using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Services;
using Judoo.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Judoo.Infrastructure.Services;

public class TatameNotificationService : ITatameNotificationService
{
    private readonly IHubContext<TatameHub, ITatameClient> _hubContext;
    private readonly ILogger<TatameNotificationService> _logger;

    public TatameNotificationService(IHubContext<TatameHub, ITatameClient> hubContext, ILogger<TatameNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotificarPlacarAtualizadoAsync(Guid tatameId, PlacarLuta placar, CancellationToken ct = default)
    {
        var groupName = $"tatame-{tatameId}";
        var dto = PlacarLutaDto.FromDomain(placar);

        _logger.LogDebug("Transmitindo PlacarAtualizado para grupo {GroupName} e arena-geral", groupName);

        // Envia para quem está ouvindo o tatame específico e para o telão geral da Arena
        await _hubContext.Clients.Group(groupName).PlacarAtualizado(dto);
        await _hubContext.Clients.Group("arena-geral").PlacarAtualizado(new { TatameId = tatameId, Placar = dto });
    }

    public async Task NotificarCronometroAsync(Guid tatameId, int tempoSegundos, bool rodando, bool goldenScore, bool sonoMama = false, CancellationToken ct = default)
    {
        var groupName = $"tatame-{tatameId}";
        var payload = new
        {
            TatameId = tatameId,
            TempoSegundos = tempoSegundos,
            Rodando = rodando,
            GoldenScore = goldenScore,
            SonoMama = sonoMama,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group(groupName).CronometroAtualizado(payload);
        await _hubContext.Clients.Group("arena-geral").CronometroAtualizado(payload);
    }

    public async Task NotificarLutaIniciadaAsync(Guid tatameId, Luta luta, CancellationToken ct = default)
    {
        var groupName = $"tatame-{tatameId}";
        await _hubContext.Clients.Group(groupName).LutaIniciada(luta);
        await _hubContext.Clients.Group("arena-geral").LutaIniciada(new { TatameId = tatameId, Luta = luta });
    }

    public async Task NotificarLutaFinalizadaAsync(Guid tatameId, Guid lutaId, CorJudogi? vencedor, TipoVitoria tipoVitoria, CancellationToken ct = default)
    {
        var groupName = $"tatame-{tatameId}";
        var payload = new
        {
            TatameId = tatameId,
            LutaId = lutaId,
            Vencedor = vencedor?.ToString(),
            TipoVitoria = tipoVitoria.ToString(),
            FinalizadoEm = DateTime.UtcNow
        };

        await _hubContext.Clients.Group(groupName).LutaFinalizada(payload);
        await _hubContext.Clients.Group("arena-geral").LutaFinalizada(payload);
    }
}
