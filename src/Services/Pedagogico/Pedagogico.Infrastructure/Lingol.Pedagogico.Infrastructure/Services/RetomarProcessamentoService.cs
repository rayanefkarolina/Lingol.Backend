using System.Collections.Concurrent;
using System.Threading.Channels;
using Lingol.Pedagogico.Application.Abstractions;
using Lingol.Pedagogico.Application.Queue;
using Lingol.Pedagogico.Domain.Entities;
using Lingol.Pedagogico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lingol.Pedagogico.Infrastructure.Services;

/// <summary>
/// Rede de segurança das duas filas in-process.
///
/// As filas vivem na memória do processo. Se o servidor reinicia — e em
/// hospedagem gratuita isso acontece sozinho, por hibernação ou deploy — tudo
/// que estava enfileirado some, e a atividade fica presa em "Processando" para
/// sempre, sem ninguém para destravá-la.
///
/// Este serviço varre o banco e devolve para a fila o que ficou pelo caminho:
/// na subida (onde nada pode estar realmente em andamento) e, depois, de tempos
/// em tempos, para pegar também o que travou com o processo vivo.
///
/// Pressupõe UMA instância da API, que é como o deploy está descrito em
/// docs/publicando.md. Com duas ou mais, a varredura de inicialização precisaria
/// de um tempo de carência para não reenfileirar o que a outra instância está
/// processando naquele instante.
/// </summary>
public class RetomarProcessamentoService : BackgroundService
{
    /// <summary>Tempo parado a partir do qual a varredura periódica age.</summary>
    private static readonly TimeSpan ParadoPorTempoDemais = TimeSpan.FromMinutes(15);

    /// <summary>De quanto em quanto tempo a varredura periódica roda.</summary>
    private static readonly TimeSpan IntervaloDaVarredura = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Quantas vezes a mesma atividade pode ser reenfileirada nesta execução do
    /// processo antes de desistir. Evita repetir para sempre algo que falha
    /// sempre (uma chave de IA inválida, por exemplo).
    /// </summary>
    private const int MaxTentativasPorAtividade = 3;

    private readonly IServiceProvider _serviceProvider;
    private readonly ChannelWriter<GerarAtividadeQueueItem> _geracaoWriter;
    private readonly ChannelWriter<CorrigirEntregaQueueItem> _correcaoWriter;
    private readonly ILogger<RetomarProcessamentoService> _logger;

    private readonly ConcurrentDictionary<Guid, int> _tentativas = new();

    public RetomarProcessamentoService(
        IServiceProvider serviceProvider,
        ChannelWriter<GerarAtividadeQueueItem> geracaoWriter,
        ChannelWriter<CorrigirEntregaQueueItem> correcaoWriter,
        ILogger<RetomarProcessamentoService> logger)
    {
        _serviceProvider = serviceProvider;
        _geracaoWriter = geracaoWriter;
        _correcaoWriter = correcaoWriter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Na subida nada pode estar em andamento de verdade: tudo que estiver
        // "Processando" é sobra de uma execução anterior. Daí a carência zero.
        await VarrerComSegurancaAsync(TimeSpan.Zero, "inicialização", stoppingToken);

        using var relogio = new PeriodicTimer(IntervaloDaVarredura);

        while (await SeguroEsperarAsync(relogio, stoppingToken))
        {
            await VarrerComSegurancaAsync(ParadoPorTempoDemais, "rotina", stoppingToken);
        }
    }

    private static async Task<bool> SeguroEsperarAsync(PeriodicTimer relogio, CancellationToken ct)
    {
        try
        {
            return await relogio.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// A varredura nunca pode derrubar a API. Na hospedagem gratuita o banco
    /// costuma estar pausado na primeira consulta do dia, e falhar aqui deixaria
    /// o serviço inteiro fora do ar.
    /// </summary>
    private async Task VarrerComSegurancaAsync(TimeSpan carencia, string origem, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PedagogicoDbContext>();
            var cadastro = scope.ServiceProvider.GetRequiredService<ICadastroClient>();

            var geracoes = await RetomarGeracoesAsync(db, cadastro, carencia, ct);
            var correcoes = await RetomarCorrecoesAsync(db, carencia, ct);

            if (geracoes > 0 || correcoes > 0)
            {
                _logger.LogInformation(
                    "Varredura de {Origem}: {Geracoes} atividade(s) e {Correcoes} correção(ões) devolvidas à fila",
                    origem, geracoes, correcoes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Varredura de {Origem} falhou; será tentada de novo no próximo ciclo", origem);
        }
    }

    private async Task<int> RetomarGeracoesAsync(
        PedagogicoDbContext db,
        ICadastroClient cadastro,
        TimeSpan carencia,
        CancellationToken ct)
    {
        var limite = DateTime.UtcNow - carencia;

        var paradas = await db.Atividades
            .Where(a => (a.Status == StatusAtividade.Pendente || a.Status == StatusAtividade.Processando)
                        && a.DataCriacao <= limite)
            .OrderBy(a => a.DataCriacao)
            .ToListAsync(ct);

        var reenfileiradas = 0;

        foreach (var atividade in paradas)
        {
            var tentativa = _tentativas.AddOrUpdate(atividade.Id, 1, (_, n) => n + 1);

            if (tentativa > MaxTentativasPorAtividade)
            {
                atividade.MudarStatusParaErro(
                    "A geração foi interrompida várias vezes. Gere a atividade novamente.");
                _logger.LogWarning(
                    "Atividade {AtividadeId} desistida após {Tentativas} tentativas de retomada",
                    atividade.Id, tentativa - 1);
                continue;
            }

            var item = await RemontarPedidoAsync(atividade, cadastro, ct);

            if (item is null)
            {
                _logger.LogWarning(
                    "Atividade {AtividadeId} não pôde ser remontada; fica como está para a próxima varredura",
                    atividade.Id);
                continue;
            }

            atividade.MudarStatusParaPendente();
            await _geracaoWriter.WriteAsync(item, ct);
            reenfileiradas++;

            _logger.LogInformation(
                "Atividade {AtividadeId} devolvida à fila (tentativa {Tentativa})",
                atividade.Id, tentativa);
        }

        if (paradas.Count > 0)
            await db.SaveChangesAsync(ct);

        return reenfileiradas;
    }

    /// <summary>
    /// Usa o pedido original guardado na atividade. Para atividades criadas
    /// antes desse campo existir, remonta o que dá a partir das colunas — o foco
    /// da revisão, que não tem coluna própria, se perde, então a revisão volta a
    /// ser uma atividade normal sobre o mesmo assunto.
    /// </summary>
    private async Task<GerarAtividadeQueueItem?> RemontarPedidoAsync(
        Atividade atividade,
        ICadastroClient cadastro,
        CancellationToken ct)
    {
        var salvo = GerarAtividadeQueueItem.Desserializar(atividade.PayloadGeracaoJson);

        if (salvo is not null)
        {
            salvo.EnfileiradoEm = DateTime.UtcNow;
            return salvo;
        }

        var turma = await cadastro.ObterTurmaAsync(atividade.TurmaId, ct);

        if (turma is null)
        {
            _logger.LogWarning(
                "Turma {TurmaId} não respondeu; atividade {AtividadeId} não pôde ser remontada",
                atividade.TurmaId, atividade.Id);
            return null;
        }

        return new GerarAtividadeQueueItem
        {
            AtividadeId = atividade.Id,
            TurmaId = atividade.TurmaId,
            Livro = atividade.Livro,
            CapituloOuAssunto = atividade.CapituloOuAssunto,
            Materia = atividade.Materia,
            Ano = turma.Ano,
            NumQuestoes = atividade.NumQuestoes,
            Modo = atividade.Modo,
            Tema = atividade.Tema
        };
    }

    private async Task<int> RetomarCorrecoesAsync(
        PedagogicoDbContext db,
        TimeSpan carencia,
        CancellationToken ct)
    {
        var limite = DateTime.UtcNow - carencia;

        var pendentes = await db.RespostasAluno
            .Where(r => !r.CorrecaoProcessada && r.DataEnvio <= limite)
            .OrderBy(r => r.DataEnvio)
            .ToListAsync(ct);

        foreach (var entrega in pendentes)
        {
            await _correcaoWriter.WriteAsync(new CorrigirEntregaQueueItem
            {
                RespostaAlunoId = entrega.Id,
                AtividadeId = entrega.AtividadeId,
                TurmaId = entrega.TurmaId,
                AlunoId = entrega.AlunoId
            }, ct);

            _logger.LogInformation(
                "Diagnóstico da entrega {RespostaAlunoId} devolvido à fila", entrega.Id);
        }

        return pendentes.Count;
    }
}
