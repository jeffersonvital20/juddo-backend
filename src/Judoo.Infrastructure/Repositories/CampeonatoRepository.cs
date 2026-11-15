using Dapper;
using Judoo.Domain.Entities;
using Judoo.Domain.Repositories;
using Judoo.Infrastructure.Data;

namespace Judoo.Infrastructure.Repositories;

public class CampeonatoRepository : ICampeonatoRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CampeonatoRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Campeonato>> ObterTodosAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                id, nome, local, data_inicio AS DataInicio, data_fim AS DataFim, status, criado_em AS CriadoEm
            FROM campeonatos
            ORDER BY data_inicio DESC, criado_em DESC;";

        return await conn.QueryAsync<Campeonato>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<Campeonato?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                id, nome, local, data_inicio AS DataInicio, data_fim AS DataFim, status, criado_em AS CriadoEm
            FROM campeonatos
            WHERE id = @Id;";

        return await conn.QuerySingleOrDefaultAsync<Campeonato>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task CriarAsync(Campeonato campeonato, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO campeonatos (
                id, nome, local, data_inicio, data_fim, status, criado_em
            ) VALUES (
                @Id, @Nome, @Local, @DataInicio, @DataFim, @Status, @CriadoEm
            );";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            campeonato.Id,
            campeonato.Nome,
            campeonato.Local,
            campeonato.DataInicio,
            campeonato.DataFim,
            Status = campeonato.Status.ToUpper(),
            campeonato.CriadoEm
        }, cancellationToken: ct));
    }

    public async Task AtualizarStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE campeonatos
            SET status = @Status
            WHERE id = @Id;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            Status = status.ToUpper()
        }, cancellationToken: ct));
    }

    public async Task<IEnumerable<Categoria>> ObterCategoriasAsync(Guid campeonatoId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                id, campeonato_id AS CampeonatoId, classe, divisao_peso AS DivisaoPeso, 
                peso_limite_kg AS PesoLimiteKg, sexo, tempo_luta_segundos AS TempoLutaSegundos, 
                tempo_osaekomi_wazaari AS TempoOsaekomiWazaari, tempo_osaekomi_ippon AS TempoOsaekomiIppon, 
                criado_em AS CriadoEm
            FROM categorias
            WHERE campeonato_id = @CampeonatoId
            ORDER BY classe, sexo, peso_limite_kg;";

        return await conn.QueryAsync<Categoria>(new CommandDefinition(sql, new { CampeonatoId = campeonatoId }, cancellationToken: ct));
    }

    public async Task<Categoria?> ObterCategoriaPorIdAsync(Guid categoriaId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                id, campeonato_id AS CampeonatoId, classe, divisao_peso AS DivisaoPeso, 
                peso_limite_kg AS PesoLimiteKg, sexo, tempo_luta_segundos AS TempoLutaSegundos, 
                tempo_osaekomi_wazaari AS TempoOsaekomiWazaari, tempo_osaekomi_ippon AS TempoOsaekomiIppon, 
                criado_em AS CriadoEm
            FROM categorias
            WHERE id = @Id;";

        return await conn.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(sql, new { Id = categoriaId }, cancellationToken: ct));
    }

    public async Task CriarCategoriaAsync(Categoria categoria, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO categorias (
                id, campeonato_id, classe, divisao_peso, peso_limite_kg, sexo, tempo_luta_segundos, tempo_osaekomi_wazaari, tempo_osaekomi_ippon, criado_em
            ) VALUES (
                @Id, @CampeonatoId, @Classe, @DivisaoPeso, @PesoLimiteKg, @Sexo, @TempoLutaSegundos, @TempoOsaekomiWazaari, @TempoOsaekomiIppon, @CriadoEm
            );";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            categoria.Id,
            categoria.CampeonatoId,
            categoria.Classe,
            categoria.DivisaoPeso,
            categoria.PesoLimiteKg,
            Sexo = categoria.Sexo.ToUpper(),
            categoria.TempoLutaSegundos,
            categoria.TempoOsaekomiWazaari,
            categoria.TempoOsaekomiIppon,
            categoria.CriadoEm
        }, cancellationToken: ct));
    }

    public async Task AtualizarCategoriaAsync(Categoria categoria, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE categorias
            SET classe = @Classe,
                divisao_peso = @DivisaoPeso,
                peso_limite_kg = @PesoLimiteKg,
                sexo = @Sexo,
                tempo_luta_segundos = @TempoLutaSegundos,
                tempo_osaekomi_wazaari = @TempoOsaekomiWazaari,
                tempo_osaekomi_ippon = @TempoOsaekomiIppon
            WHERE id = @Id;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            categoria.Id,
            categoria.Classe,
            categoria.DivisaoPeso,
            categoria.PesoLimiteKg,
            Sexo = categoria.Sexo.ToUpper(),
            categoria.TempoLutaSegundos,
            categoria.TempoOsaekomiWazaari,
            categoria.TempoOsaekomiIppon
        }, cancellationToken: ct));
    }

    public async Task RemoverCategoriaAsync(Guid categoriaId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            DELETE FROM pesagens WHERE categoria_id = @Id;
            DELETE FROM placar_lutas WHERE luta_id IN (SELECT id FROM lutas WHERE chave_id IN (SELECT id FROM chaves WHERE categoria_id = @Id));
            DELETE FROM eventos_pontuacao WHERE luta_id IN (SELECT id FROM lutas WHERE chave_id IN (SELECT id FROM chaves WHERE categoria_id = @Id));
            DELETE FROM lutas WHERE chave_id IN (SELECT id FROM chaves WHERE categoria_id = @Id);
            DELETE FROM chaves WHERE categoria_id = @Id;
            DELETE FROM categorias WHERE id = @Id;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = categoriaId }, cancellationToken: ct));
    }

    public async Task<IEnumerable<Chave>> ObterChavesAsync(Guid campeonatoId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, campeonato_id AS CampeonatoId, categoria_id AS CategoriaId, tipo, total_fases AS TotalFases, gerada, criada_em AS CriadaEm
            FROM chaves
            WHERE campeonato_id = @CampeonatoId
            ORDER BY criada_em ASC;";

        return await conn.QueryAsync<Chave>(new CommandDefinition(sql, new { CampeonatoId = campeonatoId }, cancellationToken: ct));
    }

    public async Task<Chave?> ObterOuCriarChaveAsync(Guid campeonatoId, Guid categoriaId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string selectSql = @"
            SELECT id, campeonato_id AS CampeonatoId, categoria_id AS CategoriaId, tipo, total_fases AS TotalFases, gerada, criada_em AS CriadaEm
            FROM chaves
            WHERE campeonato_id = @CampeonatoId AND categoria_id = @CategoriaId;";

        var chave = await conn.QuerySingleOrDefaultAsync<Chave>(new CommandDefinition(selectSql, new { CampeonatoId = campeonatoId, CategoriaId = categoriaId }, cancellationToken: ct));
        if (chave != null) return chave;

        var novaChave = new Chave
        {
            Id = Guid.NewGuid(),
            CampeonatoId = campeonatoId,
            CategoriaId = categoriaId,
            Tipo = "ELIMINATORIA_SIMPLES",
            TotalFases = 2,
            Gerada = true,
            CriadaEm = DateTime.UtcNow
        };

        const string insertSql = @"
            INSERT INTO chaves (id, campeonato_id, categoria_id, tipo, total_fases, gerada, criada_em)
            VALUES (@Id, @CampeonatoId, @CategoriaId, @Tipo, @TotalFases, @Gerada, @CriadaEm);";

        await conn.ExecuteAsync(new CommandDefinition(insertSql, new
        {
            novaChave.Id,
            novaChave.CampeonatoId,
            novaChave.CategoriaId,
            Tipo = novaChave.Tipo.ToUpper(),
            novaChave.TotalFases,
            novaChave.Gerada,
            novaChave.CriadaEm
        }, cancellationToken: ct));

        return novaChave;
    }

    public async Task AtualizarChaveGeradaAsync(Guid chaveId, int totalFases, bool gerada, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE chaves
            SET total_fases = @TotalFases,
                gerada = @Gerada
            WHERE id = @ChaveId;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { ChaveId = chaveId, TotalFases = totalFases, Gerada = gerada }, cancellationToken: ct));
    }

    public async Task<IEnumerable<Pesagem>> ObterPesagensPorCategoriaAsync(Guid campeonatoId, Guid categoriaId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, campeonato_id AS CampeonatoId, atleta_id AS AtletaId, categoria_id AS CategoriaId,
                   peso_aferido_kg AS PesoAferidoKg, status, data_pesagem AS DataPesagem,
                   aferido_por_usuario_id AS AferidoPorUsuarioId, observacoes
            FROM pesagens
            WHERE campeonato_id = @CampeonatoId AND categoria_id = @CategoriaId
            ORDER BY data_pesagem ASC;";

        return await conn.QueryAsync<Pesagem>(new CommandDefinition(sql, new { CampeonatoId = campeonatoId, CategoriaId = categoriaId }, cancellationToken: ct));
    }
}
