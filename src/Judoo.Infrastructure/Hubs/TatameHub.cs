using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Judoo.Infrastructure.Hubs;

public interface ITatameClient
{
    Task PlacarAtualizado(object placar);
    Task CronometroAtualizado(object cronometro);
    Task LutaIniciada(object luta);
    Task LutaFinalizada(object resultado);
    Task NotificacaoGeral(string mensagem);
}

public class TatameHub : Hub<ITatameClient>
{
    private readonly ILogger<TatameHub> _logger;

    public TatameHub(ILogger<TatameHub> logger)
    {
        _logger = logger;
    }

    public async Task EntrarNoTatame(string tatameId)
    {
        var groupName = $"tatame-{tatameId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Cliente {ConnectionId} entrou no canal do tatame {TatameId}", Context.ConnectionId, tatameId);
    }

    public async Task SairDoTatame(string tatameId)
    {
        var groupName = $"tatame-{tatameId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Cliente {ConnectionId} saiu do canal do tatame {TatameId}", Context.ConnectionId, tatameId);
    }

    public async Task EntrarNaArena()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "arena-geral");
        _logger.LogInformation("Cliente {ConnectionId} conectado ao telão geral da Arena", Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Conexão WebSocket SignalR estabelecida: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Conexão WebSocket SignalR desconectada: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
