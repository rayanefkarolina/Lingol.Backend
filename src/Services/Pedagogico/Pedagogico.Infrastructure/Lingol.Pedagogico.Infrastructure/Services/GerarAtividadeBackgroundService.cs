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
/// Consome a fila in-process de geração de atividades: chama a IA, persiste as
/// questões e avisa o painel do professor via <see cref="IAtividadeNotifier"/>.
/// </summary>
public class GerarAtividadeBackgroundService : BackgroundService
{
    private readonly ChannelReader<GerarAtividadeQueueItem> _queueReader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GerarAtividadeBackgroundService> _logger;

    public GerarAtividadeBackgroundService(
        ChannelReader<GerarAtividadeQueueItem> queueReader,
        IServiceProvider serviceProvider,
        ILogger<GerarAtividadeBackgroundService> logger)
    {
        _queueReader = queueReader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GerarAtividadeBackgroundService iniciado");

        try
        {
            await foreach (var item in _queueReader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessarAsync(item, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar atividade {AtividadeId}", item.AtividadeId);
                    await RegistrarFalhaAsync(item, ex, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GerarAtividadeBackgroundService cancelado");
        }
    }

    private async Task ProcessarAsync(GerarAtividadeQueueItem item, CancellationToken ct)
    {
        _logger.LogInformation("Processando atividade {AtividadeId}", item.AtividadeId);

        using var scope = _serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<PedagogicoDbContext>();
        var iaService = scope.ServiceProvider.GetRequiredService<IIaAtividadeService>();
        var notifier = scope.ServiceProvider.GetRequiredService<IAtividadeNotifier>();

        var atividade = await db.Atividades
            .FirstOrDefaultAsync(a => a.Id == item.AtividadeId, ct);

        if (atividade is null)
        {
            _logger.LogWarning("Atividade {AtividadeId} não encontrada", item.AtividadeId);
            return;
        }

        atividade.MudarStatusParaProcessando();
        await db.SaveChangesAsync(ct);

        var questoesGeradas = await iaService.GerarAtividadeAsync(
            item.Livro,
            item.CapituloOuAssunto,
            item.Materia,
            item.Ano,
            item.NumQuestoes,
            item.Modo,
            item.Tema,
            item.PerfilAeeContexto,
            item.FocoRevisao,
            ct);

        _logger.LogInformation("IA retornou {NumQuestoes} questões", questoesGeradas.Count);

        // Gerar de novo substitui o resultado anterior em vez de somar a ele.
        // Sem isso, uma atividade reenfileirada pela varredura de recuperação
        // sairia com o dobro de questões. A troca acontece no mesmo SaveChanges
        // lá embaixo, então ou entra tudo, ou não entra nada.
        var questoesAnteriores = await db.Questoes
            .Where(q => q.AtividadeId == atividade.Id)
            .ToListAsync(ct);

        if (questoesAnteriores.Count > 0)
        {
            _logger.LogInformation(
                "Descartando {Total} questão(ões) de uma geração anterior interrompida",
                questoesAnteriores.Count);
            db.Questoes.RemoveRange(questoesAnteriores);
        }

        var ordem = 1;
        foreach (var q in questoesGeradas)
        {
            var questao = new Questao(
                atividade.Id,
                ordem++,
                q.Enunciado,
                q.Tipo,
                q.GabaritoOuCriterio,
                q.Alternativas,
                q.Explicacao,
                q.Habilidade);

            atividade.AdicionarQuestao(questao);
            db.Questoes.Add(questao);
        }

        atividade.MudarStatusParaPronta();
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Atividade {AtividadeId} pronta", item.AtividadeId);

        await notifier.NotificarAtividadeProntaAsync(
            item.TurmaId,
            item.AtividadeId,
            questoesGeradas.Count,
            ct);
    }

    private async Task RegistrarFalhaAsync(GerarAtividadeQueueItem item, Exception erro, CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<PedagogicoDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<IAtividadeNotifier>();

            var atividade = await db.Atividades
                .FirstOrDefaultAsync(a => a.Id == item.AtividadeId, ct);

            if (atividade is null)
                return;

            atividade.MudarStatusParaErro($"Erro: {erro.Message}");
            await db.SaveChangesAsync(ct);

            await notifier.NotificarErroAsync(item.TurmaId, item.AtividadeId, erro.Message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar erro da atividade {AtividadeId}", item.AtividadeId);
        }
    }
}
