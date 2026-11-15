using System.Data;
using Dapper;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using Judoo.Infrastructure.Data;

namespace Judoo.Infrastructure.Repositories;

public class PlacarLutaRepository : IPlacarLutaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PlacarLutaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PlacarLuta?> ObterPorLutaIdAsync(Guid lutaId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                luta_id AS LutaId,
                ippon_branco AS IpponBranco,
                wazaari_branco AS WazaAriBranco,
                COALESCE(yuko_branco, 0) AS YukoBranco,
                shido_branco AS ShidoBranco,
                ippon_azul AS IpponAzul,
                wazaari_azul AS WazaAriAzul,
                COALESCE(yuko_azul, 0) AS YukoAzul,
                shido_azul AS ShidoAzul,
                osaekomi_ativo AS OsaekomiAtivo,
                osaekomi_cor AS OsaekomiCorString,
                osaekomi_tempo_segundos AS OsaekomiTempoSegundos,
                cronometro_segundos AS CronometroSegundos,
                em_golden_score AS EmGoldenScore,
                cronometro_rodando AS CronometroRodando,
                sono_mama AS SonoMama,
                ultima_atualizacao AS UltimaAtualizacao
            FROM placar_lutas
            WHERE luta_id = @LutaId;";

        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(sql, new { LutaId = lutaId }, cancellationToken: ct));
        if (row == null) return null;

        var placar = new PlacarLuta(lutaId);
        CorJudogi? corOsae = null;
        string? rawCor = (string?)row.osaekomicorstring;
        if (!string.IsNullOrEmpty(rawCor) && Enum.TryParse<CorJudogi>(rawCor, true, out var parsedCor))
        {
            corOsae = parsedCor;
        }

        placar.SetRawScores(
            (int)row.ipponbranco,
            (int)row.wazaaribranco,
            (int)row.shidobranco,
            (int)row.ipponazul,
            (int)row.wazaariazul,
            (int)row.shidoazul,
            (bool)row.osaekomiativo,
            corOsae,
            (int)row.osaekomitemposegundos,
            (int)row.cronometrosegundos,
            (bool)row.emgoldenscore,
            (bool)row.cronometrorodando,
            (bool)row.sonomama,
            (int)row.yukobranco,
            (int)row.yukoazul
        );

        return placar;
    }

    public async Task SalvarOuAtualizarAsync(PlacarLuta placar, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO placar_lutas (
                luta_id,
                ippon_branco,
                wazaari_branco,
                yuko_branco,
                shido_branco,
                ippon_azul,
                wazaari_azul,
                yuko_azul,
                shido_azul,
                osaekomi_ativo,
                osaekomi_cor,
                osaekomi_tempo_segundos,
                cronometro_segundos,
                em_golden_score,
                cronometro_rodando,
                sono_mama,
                ultima_atualizacao
            ) VALUES (
                @LutaId,
                @IpponBranco,
                @WazaAriBranco,
                @YukoBranco,
                @ShidoBranco,
                @IpponAzul,
                @WazaAriAzul,
                @YukoAzul,
                @ShidoAzul,
                @OsaekomiAtivo,
                @OsaekomiCor,
                @OsaekomiTempoSegundos,
                @CronometroSegundos,
                @EmGoldenScore,
                @CronometroRodando,
                @SonoMama,
                @UltimaAtualizacao
            )
            ON CONFLICT (luta_id) DO UPDATE SET
                ippon_branco = EXCLUDED.ippon_branco,
                wazaari_branco = EXCLUDED.wazaari_branco,
                yuko_branco = EXCLUDED.yuko_branco,
                shido_branco = EXCLUDED.shido_branco,
                ippon_azul = EXCLUDED.ippon_azul,
                wazaari_azul = EXCLUDED.wazaari_azul,
                yuko_azul = EXCLUDED.yuko_azul,
                shido_azul = EXCLUDED.shido_azul,
                osaekomi_ativo = EXCLUDED.osaekomi_ativo,
                osaekomi_cor = EXCLUDED.osaekomi_cor,
                osaekomi_tempo_segundos = EXCLUDED.osaekomi_tempo_segundos,
                cronometro_segundos = EXCLUDED.cronometro_segundos,
                em_golden_score = EXCLUDED.em_golden_score,
                cronometro_rodando = EXCLUDED.cronometro_rodando,
                sono_mama = EXCLUDED.sono_mama,
                ultima_atualizacao = EXCLUDED.ultima_atualizacao;";

        string? corJudogiStr = placar.OsaekomiCor?.ToString();

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            placar.LutaId,
            placar.IpponBranco,
            placar.WazaAriBranco,
            placar.YukoBranco,
            placar.ShidoBranco,
            placar.IpponAzul,
            placar.WazaAriAzul,
            placar.YukoAzul,
            placar.ShidoAzul,
            placar.OsaekomiAtivo,
            OsaekomiCor = corJudogiStr,
            placar.OsaekomiTempoSegundos,
            placar.CronometroSegundos,
            placar.EmGoldenScore,
            placar.CronometroRodando,
            placar.SonoMama,
            UltimaAtualizacao = DateTime.UtcNow
        }, cancellationToken: ct));
    }
}
