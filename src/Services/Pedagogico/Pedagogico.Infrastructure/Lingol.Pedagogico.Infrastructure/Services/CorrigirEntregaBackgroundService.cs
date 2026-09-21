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
/// Diagnóstico pedagógico em segundo plano. O aluno já recebeu o placar na hora;
/// aqui a IA classifica as dificuldades por trás dos erros e alimenta o dashboard
/// do professor.
/// </summary>
public class CorrigirEntregaBackgroundService : BackgroundService
{
    private readonly ChannelReader<CorrigirEntregaQueueItem> _queueReader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CorrigirEntregaBackgroundService> _logger;

    public CorrigirEntregaBackgroundService(
        ChannelReader<CorrigirEntregaQueueItem> queueReader,
        IServiceProvider serviceProvider,
        ILogger<CorrigirEntregaBackgroundService> logger)
    {
        _queueReader = queueReader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CorrigirEntregaBackgroundService iniciado");

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
                    // O placar do aluno já foi persistido; uma falha aqui deixa a
                    // entrega sem diagnóstico, mas não invalida o resultado.
                    _logger.LogError(ex,
                        "Erro ao diagnosticar a entrega {RespostaAlunoId}", item.RespostaAlunoId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CorrigirEntregaBackgroundService cancelado");
        }
    }

    private async Task ProcessarAsync(CorrigirEntregaQueueItem item, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<PedagogicoDbContext>();
        var iaService = scope.ServiceProvider.GetRequiredService<IIaAtividadeService>();
        var notifier = scope.ServiceProvider.GetRequiredService<IAtividadeNotifier>();

        var entrega = await db.RespostasAluno
            .Include(r => r.Itens)
            .FirstOrDefaultAsync(r => r.Id == item.RespostaAlunoId, ct);

        if (entrega is null)
        {
            _logger.LogWarning("Entrega {RespostaAlunoId} não encontrada", item.RespostaAlunoId);
            return;
        }

        if (entrega.CorrecaoProcessada)
            return;

        var atividade = await db.Atividades
            .Include(a => a.Questoes)
            .FirstOrDefaultAsync(a => a.Id == entrega.AtividadeId, ct);

        if (atividade is null)
        {
            _logger.LogWarning("Atividade {AtividadeId} não encontrada", entrega.AtividadeId);
            return;
        }

        _logger.LogInformation(
            "Diagnosticando entrega {RespostaAlunoId} ({Acertos}/{Total})",
            entrega.Id, entrega.Acertos, entrega.TotalQuestoes);

        var resultado = await iaService.CorrigirRespostaAsync(atividade, entrega, ct);

        foreach (var d in resultado.Dificuldades)
        {
            var dificuldade = new DificuldadeAluno(
                entrega.Id,
                atividade.Id,
                atividade.TurmaId,
                entrega.AlunoId,
                d.QuestaoId,
                d.Tipo,
                d.Descricao);

            db.DificuldadesAluno.Add(dificuldade);
        }

        entrega.DefinirCorrecao(resultado.Nota, resultado.FeedbackGeral);
        entrega.MarcarCorrecaoProcessada();

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Entrega {RespostaAlunoId} diagnosticada: {Total} dificuldade(s)",
            entrega.Id, resultado.Dificuldades.Count);

        await notifier.NotificarCorrecaoProntaAsync(
            atividade.TurmaId,
            atividade.Id,
            entrega.AlunoId,
            resultado.Dificuldades.Count,
            ct);
    }
}
