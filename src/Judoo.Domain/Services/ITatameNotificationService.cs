using Judoo.Domain.Entities;
using Judoo.Domain.Enums;

namespace Judoo.Domain.Services;

public interface ITatameNotificationService
{
    Task NotificarPlacarAtualizadoAsync(Guid tatameId, PlacarLuta placar, CancellationToken ct = default);
    Task NotificarLutaIniciadaAsync(Guid tatameId, Luta luta, CancellationToken ct = default);
    Task NotificarLutaFinalizadaAsync(Guid tatameId, Guid lutaId, CorJudogi? vencedor, TipoVitoria tipoVitoria, CancellationToken ct = default);
    Task NotificarCronometroAsync(Guid tatameId, int tempoSegundos, bool rodando, bool goldenScore, CancellationToken ct = default);
}
