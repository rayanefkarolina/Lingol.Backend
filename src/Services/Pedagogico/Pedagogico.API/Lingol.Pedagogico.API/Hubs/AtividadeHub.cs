using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Lingol.Pedagogico.API.Hubs;

public class AtividadeHub : Hub
{
    private readonly ILogger<AtividadeHub> _logger;

    public AtividadeHub(ILogger<AtividadeHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinTurmaGroup(string turmaId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"turma-{turmaId}");
        _logger.LogInformation("Cliente {ConnectionId} entrou no grupo turma-{TurmaId}",
            Context.ConnectionId, turmaId);
    }

    public async Task LeaveTurmaGroup(string turmaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"turma-{turmaId}");
        _logger.LogInformation("Cliente {ConnectionId} saiu do grupo turma-{TurmaId}",
            Context.ConnectionId, turmaId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Cliente {ConnectionId} conectado", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Cliente {ConnectionId} desconectado", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}