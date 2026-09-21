using Lingol.Pedagogico.API.Hubs;
using Lingol.Pedagogico.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Lingol.Pedagogico.API.Services;

/// <summary>
/// Implementação de <see cref="IAtividadeNotifier"/> sobre o SignalR.
/// Fica na API para que a camada de Infrastructure não conheça o Hub.
/// </summary>
public class SignalRAtividadeNotifier : IAtividadeNotifier
{
    private readonly IHubContext<AtividadeHub> _hubContext;

    public SignalRAtividadeNotifier(IHubContext<AtividadeHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotificarAtividadeProntaAsync(
        Guid turmaId,
        Guid atividadeId,
        int numQuestoes,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group($"turma-{turmaId}")
            .SendAsync(
                "AtividadeGerada",
                new
                {
                    atividadeId,
                    status = "Pronta",
                    numQuestoes,
                    timestamp = DateTime.UtcNow
                },
                cancellationToken);
    }

    public Task NotificarCorrecaoProntaAsync(
        Guid turmaId,
        Guid atividadeId,
        Guid alunoId,
        int dificuldadesIdentificadas,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group($"turma-{turmaId}")
            .SendAsync(
                "CorrecaoPronta",
                new
                {
                    atividadeId,
                    alunoId,
                    dificuldadesIdentificadas,
                    timestamp = DateTime.UtcNow
                },
                cancellationToken);
    }

    public Task NotificarErroAsync(
        Guid turmaId,
        Guid atividadeId,
        string erro,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group($"turma-{turmaId}")
            .SendAsync(
                "AtividadeErro",
                new { atividadeId, erro, timestamp = DateTime.UtcNow },
                cancellationToken);
    }
}
