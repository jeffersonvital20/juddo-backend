using Dapper;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using Judoo.Infrastructure.Data;

namespace Judoo.Infrastructure.Repositories;

public class LutaRepository : ILutaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LutaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Luta?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                l.id, l.chave_id AS ChaveId, l.tatame_id AS TatameId, 
                l.atleta_branco_id AS AtletaBrancoId, l.atleta_azul_id AS AtletaAzulId, 
                l.vencedor_id AS VencedorId, l.fase, l.ordem_na_chave AS OrdemNaChave, 
                l.ordem_no_tatame AS OrdemNoTatame, l.status, l.tipo_vitoria AS TipoVitoria, 
                l.em_golden_score AS EmGoldenScore, l.tempo_luta_segundos AS TempoLutaSegundos, 
                l.hora_inicio AS HoraInicio, l.hora_fim AS HoraFim, l.criada_em AS CriadaEm,
                ab.id, ab.nome, ab.registro_federacao AS RegistroFederacao, ab.data_nascimento AS DataNascimento,
                ab.sexo, ab.foto_url AS FotoUrl, ab.ativo, acb.nome AS NomeAcademia, acb.sigla AS SiglaAcademia,
                az.id, az.nome, az.registro_federacao AS RegistroFederacao, az.data_nascimento AS DataNascimento,
                az.sexo, az.foto_url AS FotoUrl, az.ativo, acz.nome AS NomeAcademia, acz.sigla AS SiglaAcademia
            FROM lutas l
            LEFT JOIN atletas ab ON l.atleta_branco_id = ab.id
            LEFT JOIN academias acb ON ab.academia_id = acb.id
            LEFT JOIN atletas az ON l.atleta_azul_id = az.id
            LEFT JOIN academias acz ON az.academia_id = acz.id
            WHERE l.id = @Id;";

        var result = await conn.QueryAsync<Luta, Atleta, Atleta, Luta>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct),
            (luta, atletaBranco, atletaAzul) =>
            {
                luta.AtletaBranco = atletaBranco;
                luta.AtletaAzul = atletaAzul;
                return luta;
            },
            splitOn: "id,id"
        );

        return result.FirstOrDefault();
    }

    public async Task<IEnumerable<Luta>> ObterLutasPorTatameAsync(Guid tatameId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                l.id, l.chave_id AS ChaveId, l.tatame_id AS TatameId, 
                l.atleta_branco_id AS AtletaBrancoId, l.atleta_azul_id AS AtletaAzulId, 
                l.vencedor_id AS VencedorId, l.fase, l.ordem_na_chave AS OrdemNaChave, 
                l.ordem_no_tatame AS OrdemNoTatame, l.status, l.tipo_vitoria AS TipoVitoria, 
                l.em_golden_score AS EmGoldenScore, l.tempo_luta_segundos AS TempoLutaSegundos, 
                l.hora_inicio AS HoraInicio, l.hora_fim AS HoraFim, l.criada_em AS CriadaEm,
                ab.id, ab.nome, ab.registro_federacao AS RegistroFederacao, acb.nome AS NomeAcademia, acb.sigla AS SiglaAcademia,
                az.id, az.nome, az.registro_federacao AS RegistroFederacao, acz.nome AS NomeAcademia, acz.sigla AS SiglaAcademia
            FROM lutas l
            LEFT JOIN atletas ab ON l.atleta_branco_id = ab.id
            LEFT JOIN academias acb ON ab.academia_id = acb.id
            LEFT JOIN atletas az ON l.atleta_azul_id = az.id
            LEFT JOIN academias acz ON az.academia_id = acz.id
            WHERE l.tatame_id = @TatameId
            ORDER BY l.ordem_no_tatame ASC, l.criada_em ASC;";

        return await conn.QueryAsync<Luta, Atleta, Atleta, Luta>(
            new CommandDefinition(sql, new { TatameId = tatameId }, cancellationToken: ct),
            (luta, atletaBranco, atletaAzul) =>
            {
                luta.AtletaBranco = atletaBranco;
                luta.AtletaAzul = atletaAzul;
                return luta;
            },
            splitOn: "id,id"
        );
    }

    public async Task<IEnumerable<Luta>> ObterLutasPorChaveAsync(Guid chaveId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                l.id, l.chave_id AS ChaveId, l.tatame_id AS TatameId, 
                l.atleta_branco_id AS AtletaBrancoId, l.atleta_azul_id AS AtletaAzulId, 
                l.vencedor_id AS VencedorId, l.fase, l.ordem_na_chave AS OrdemNaChave, 
                l.ordem_no_tatame AS OrdemNoTatame, l.status, l.tipo_vitoria AS TipoVitoria, 
                l.em_golden_score AS EmGoldenScore, l.tempo_luta_segundos AS TempoLutaSegundos, 
                l.hora_inicio AS HoraInicio, l.hora_fim AS HoraFim, l.criada_em AS CriadaEm,
                ab.id, ab.nome, ab.registro_federacao AS RegistroFederacao, acb.nome AS NomeAcademia, acb.sigla AS SiglaAcademia,
                az.id, az.nome, az.registro_federacao AS RegistroFederacao, acz.nome AS NomeAcademia, acz.sigla AS SiglaAcademia
            FROM lutas l
            LEFT JOIN atletas ab ON l.atleta_branco_id = ab.id
            LEFT JOIN academias acb ON ab.academia_id = acb.id
            LEFT JOIN atletas az ON l.atleta_azul_id = az.id
            LEFT JOIN academias acz ON az.academia_id = acz.id
            WHERE l.chave_id = @ChaveId
            ORDER BY l.fase DESC, l.ordem_na_chave ASC;";

        return await conn.QueryAsync<Luta, Atleta, Atleta, Luta>(
            new CommandDefinition(sql, new { ChaveId = chaveId }, cancellationToken: ct),
            (luta, atletaBranco, atletaAzul) =>
            {
                luta.AtletaBranco = atletaBranco;
                luta.AtletaAzul = atletaAzul;
                return luta;
            },
            splitOn: "id,id"
        );
    }

    public async Task<IEnumerable<Luta>> ObterTodasAsync(Guid? campeonatoId = null, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            SELECT 
                l.id, l.chave_id AS ChaveId, l.tatame_id AS TatameId, 
                l.atleta_branco_id AS AtletaBrancoId, l.atleta_azul_id AS AtletaAzulId, 
                l.vencedor_id AS VencedorId, l.fase, l.ordem_na_chave AS OrdemNaChave, 
                l.ordem_no_tatame AS OrdemNoTatame, l.status, l.tipo_vitoria AS TipoVitoria, 
                l.em_golden_score AS EmGoldenScore, l.tempo_luta_segundos AS TempoLutaSegundos, 
                l.hora_inicio AS HoraInicio, l.hora_fim AS HoraFim, l.criada_em AS CriadaEm,
                ab.id, ab.nome, ab.registro_federacao AS RegistroFederacao, acb.nome AS NomeAcademia, acb.sigla AS SiglaAcademia,
                az.id, az.nome, az.registro_federacao AS RegistroFederacao, acz.nome AS NomeAcademia, acz.sigla AS SiglaAcademia
            FROM lutas l
            LEFT JOIN chaves c ON l.chave_id = c.id
            LEFT JOIN atletas ab ON l.atleta_branco_id = ab.id
            LEFT JOIN academias acb ON ab.academia_id = acb.id
            LEFT JOIN atletas az ON l.atleta_azul_id = az.id
            LEFT JOIN academias acz ON az.academia_id = acz.id
            WHERE (@CampeonatoId IS NULL OR c.campeonato_id = @CampeonatoId)
            ORDER BY l.criada_em DESC;";

        return await conn.QueryAsync<Luta, Atleta, Atleta, Luta>(
            new CommandDefinition(sql, new { CampeonatoId = campeonatoId }, cancellationToken: ct),
            (luta, atletaBranco, atletaAzul) =>
            {
                luta.AtletaBranco = atletaBranco;
                luta.AtletaAzul = atletaAzul;
                return luta;
            },
            splitOn: "id,id"
        );
    }

    public async Task CriarLutaAsync(Luta luta, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO lutas (
                id, chave_id, tatame_id, atleta_branco_id, atleta_azul_id, fase, ordem_na_chave, ordem_no_tatame, status, em_golden_score, tempo_luta_segundos, criada_em
            ) VALUES (
                @Id, @ChaveId, @TatameId, @AtletaBrancoId, @AtletaAzulId, @Fase, @OrdemNaChave, @OrdemNoTatame, @Status, @EmGoldenScore, @TempoLutaSegundos, @CriadaEm
            );

            INSERT INTO placar_lutas (
                luta_id, ippon_branco, wazaari_branco, yuko_branco, shido_branco, ippon_azul, wazaari_azul, yuko_azul, shido_azul, cronometro_segundos, em_golden_score, cronometro_rodando, sono_mama, ultima_atualizacao
            ) VALUES (
                @Id, 0, 0, 0, 0, 0, 0, 0, 0, 240, FALSE, FALSE, FALSE, NOW()
            ) ON CONFLICT (luta_id) DO NOTHING;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            luta.Id,
            luta.ChaveId,
            luta.TatameId,
            luta.AtletaBrancoId,
            luta.AtletaAzulId,
            luta.Fase,
            luta.OrdemNaChave,
            luta.OrdemNoTatame,
            Status = luta.Status.ToString(),
            luta.EmGoldenScore,
            luta.TempoLutaSegundos,
            luta.CriadaEm
        }, cancellationToken: ct));
    }

    public async Task AtualizarStatusLutaAsync(Guid lutaId, StatusLuta status, Guid? vencedorId, TipoVitoria? tipoVitoria, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE lutas
            SET 
                status = @Status,
                vencedor_id = @VencedorId,
                tipo_vitoria = @TipoVitoria,
                hora_inicio = CASE WHEN @Status = 'EmAndamento' AND hora_inicio IS NULL THEN NOW() ELSE hora_inicio END,
                hora_fim = CASE WHEN @Status = 'Finalizada' THEN NOW() ELSE hora_fim END
            WHERE id = @LutaId;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            LutaId = lutaId,
            Status = status.ToString(),
            VencedorId = vencedorId,
            TipoVitoria = tipoVitoria?.ToString()
        }, cancellationToken: ct));

        if (status == StatusLuta.Finalizada && vencedorId.HasValue)
        {
            await AvancarVencedorCopaAsync(lutaId, ct);
        }
    }

    public async Task AvancarVencedorCopaAsync(Guid lutaFinalizadaId, CancellationToken ct = default)
    {
        var lutaAtual = await ObterPorIdAsync(lutaFinalizadaId, ct);
        if (lutaAtual == null || !lutaAtual.VencedorId.HasValue || lutaAtual.Fase <= 1)
            return;

        int proximaFase = lutaAtual.Fase - 1;
        int proximaOrdem = (lutaAtual.OrdemNaChave + 1) / 2;
        bool slotBranco = (lutaAtual.OrdemNaChave % 2) == 1;

        using var conn = _connectionFactory.CreateConnection();
        const string findSql = @"
            SELECT id, atleta_branco_id AS AtletaBrancoId, atleta_azul_id AS AtletaAzulId, status AS Status
            FROM lutas
            WHERE chave_id = @ChaveId AND fase = @ProximaFase AND ordem_na_chave = @ProximaOrdem
            LIMIT 1;";

        var lutaSeguinte = await conn.QuerySingleOrDefaultAsync<dynamic>(
            new CommandDefinition(findSql, new
            {
                lutaAtual.ChaveId,
                ProximaFase = proximaFase,
                ProximaOrdem = proximaOrdem
            }, cancellationToken: ct));

        if (lutaSeguinte != null)
        {
            Guid lutaSeguinteId = (Guid)lutaSeguinte.id;
            string updateSql = slotBranco
                ? "UPDATE lutas SET atleta_branco_id = @VencedorId WHERE id = @Id;"
                : "UPDATE lutas SET atleta_azul_id = @VencedorId WHERE id = @Id;";

            await conn.ExecuteAsync(new CommandDefinition(updateSql, new
            {
                Id = lutaSeguinteId,
                VencedorId = lutaAtual.VencedorId
            }, cancellationToken: ct));

            // Verifica se a luta irmão (que alimenta o outro lado deste confronto) era um BYE vazio (sem atletas)
            int ordemIrma = slotBranco ? lutaAtual.OrdemNaChave + 1 : lutaAtual.OrdemNaChave - 1;
            const string irmaSql = @"
                SELECT id, atleta_branco_id AS AtletaBrancoId, atleta_azul_id AS AtletaAzulId, vencedor_id AS VencedorId, status AS Status
                FROM lutas
                WHERE chave_id = @ChaveId AND fase = @Fase AND ordem_na_chave = @OrdemIrma
                LIMIT 1;";

            var lutaIrma = await conn.QuerySingleOrDefaultAsync<dynamic>(
                new CommandDefinition(irmaSql, new
                {
                    lutaAtual.ChaveId,
                    lutaAtual.Fase,
                    OrdemIrma = ordemIrma
                }, cancellationToken: ct));

            bool irmaInexistenteOuVazia = lutaIrma == null ||
                (lutaIrma.atletabrancoid == null && lutaIrma.atletaazulid == null && lutaIrma.vencedorid == null);

            if (irmaInexistenteOuVazia && proximaFase > 1)
            {
                // Avança automaticamente por WO/BYE se não houver adversário do outro lado da chave
                await AtualizarStatusLutaAsync(lutaSeguinteId, StatusLuta.Finalizada, lutaAtual.VencedorId, TipoVitoria.WO, ct);
            }
        }
        else
        {
            // Se a luta da próxima fase ainda não existia no banco, cria automaticamente
            var novaLuta = new Luta
            {
                Id = Guid.NewGuid(),
                ChaveId = lutaAtual.ChaveId,
                TatameId = lutaAtual.TatameId,
                AtletaBrancoId = slotBranco ? lutaAtual.VencedorId : null,
                AtletaAzulId = slotBranco ? null : lutaAtual.VencedorId,
                Fase = proximaFase,
                OrdemNaChave = proximaOrdem,
                OrdemNoTatame = proximaOrdem,
                Status = StatusLuta.Agendada,
                TempoLutaSegundos = lutaAtual.TempoLutaSegundos > 0 ? lutaAtual.TempoLutaSegundos : 240,
                CriadaEm = DateTime.UtcNow
            };
            await CriarLutaAsync(novaLuta, ct);
        }
    }

    public async Task AtribuirTatameAsync(Guid lutaId, Guid? tatameId, int? ordemNoTatame, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE lutas
            SET 
                tatame_id = @TatameId,
                ordem_no_tatame = @OrdemNoTatame
            WHERE id = @LutaId;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            LutaId = lutaId,
            TatameId = tatameId,
            OrdemNoTatame = ordemNoTatame
        }, cancellationToken: ct));
    }

    public async Task RegistrarEventoPontuacaoAsync(EventoPontuacao evento, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO eventos_luta (
                id, luta_id, atleta_id, cor_judogi, tipo, tempo_luta_segundos, descricao, registrado_por_usuario_id, timestamp_utc
            ) VALUES (
                @Id, @LutaId, @AtletaId, @CorJudogi, @Tipo, @TempoLutaSegundos, @Descricao, @RegistradoPorUsuarioId, @TimestampUtc
            );";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            evento.Id,
            evento.LutaId,
            evento.AtletaId,
            CorJudogi = evento.CorJudogi.ToString(),
            Tipo = evento.Tipo.ToString(),
            evento.TempoLutaSegundos,
            evento.Descricao,
            evento.RegistradoPorUsuarioId,
            evento.TimestampUtc
        }, cancellationToken: ct));
    }
}
