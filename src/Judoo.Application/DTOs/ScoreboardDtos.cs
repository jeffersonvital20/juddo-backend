using Judoo.Domain.Entities;
using Judoo.Domain.Enums;

namespace Judoo.Application.DTOs;

public record AtletaDto(
    Guid Id,
    string Nome,
    string? RegistroFederacao,
    string? NomeAcademia,
    string? SiglaAcademia
);

public record PlacarLutaDto(
    Guid LutaId,
    int IpponBranco,
    int WazaAriBranco,
    int YukoBranco,
    int ShidoBranco,
    int IpponAzul,
    int WazaAriAzul,
    int YukoAzul,
    int ShidoAzul,
    bool OsaekomiAtivo,
    string? OsaekomiCor,
    int OsaekomiTempoSegundos,
    int CronometroSegundos,
    bool EmGoldenScore,
    bool CronometroRodando,
    bool SonoMama,
    DateTime UltimaAtualizacao
)
{
    public static PlacarLutaDto FromDomain(PlacarLuta p) => new(
        p.LutaId,
        p.IpponBranco,
        p.WazaAriBranco,
        p.YukoBranco,
        p.ShidoBranco,
        p.IpponAzul,
        p.WazaAriAzul,
        p.YukoAzul,
        p.ShidoAzul,
        p.OsaekomiAtivo,
        p.OsaekomiCor?.ToString(),
        p.OsaekomiTempoSegundos,
        p.CronometroSegundos,
        p.EmGoldenScore,
        p.CronometroRodando,
        p.SonoMama,
        p.UltimaAtualizacao
    );
}

public record LutaDetalhesDto(
    Guid Id,
    Guid ChaveId,
    Guid? TatameId,
    AtletaDto? AtletaBranco,
    AtletaDto? AtletaAzul,
    PlacarLutaDto? Placar,
    string Status,
    string? TipoVitoria,
    bool EmGoldenScore,
    int TempoLutaSegundos,
    int Fase,
    int OrdemNaChave,
    Guid? VencedorId = null
);

public record TatameStatusDto(
    Guid Id,
    int Numero,
    string Nome,
    string Status,
    Guid? LutaAtualId,
    LutaDetalhesDto? LutaAtual
);
