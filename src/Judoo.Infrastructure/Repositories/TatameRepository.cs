using Dapper;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using Judoo.Infrastructure.Data;

namespace Judoo.Infrastructure.Repositories;

public class TatameRepository : ITatameRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TatameRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Tatame?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                id, campeonato_id AS CampeonatoId, numero, nome, 
                status, luta_atual_id AS LutaAtualId, mesario_id AS MesarioId, criado_em AS CriadoEm
            FROM tatames
            WHERE id = @Id;";

        return await conn.QuerySingleOrDefaultAsync<Tatame>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IEnumerable<Tatame>> ObterTodosPorCampeonatoAsync(Guid campeonatoId, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                id, campeonato_id AS CampeonatoId, numero, nome, 
                status, luta_atual_id AS LutaAtualId, mesario_id AS MesarioId, criado_em AS CriadoEm
            FROM tatames
            WHERE campeonato_id = @CampeonatoId
            ORDER BY numero ASC;";

        return await conn.QueryAsync<Tatame>(new CommandDefinition(sql, new { CampeonatoId = campeonatoId }, cancellationToken: ct));
    }

    public async Task CriarAsync(Tatame tatame, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO tatames (id, campeonato_id, numero, nome, status, criado_em)
            VALUES (@Id, @CampeonatoId, @Numero, @Nome, @Status, @CriadoEm);";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            tatame.Id,
            tatame.CampeonatoId,
            tatame.Numero,
            tatame.Nome,
            Status = tatame.Status.ToString(),
            tatame.CriadoEm
        }, cancellationToken: ct));
    }

    public async Task AtualizarLutaAtualAsync(Guid tatameId, Guid? lutaId, StatusTatame status, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE tatames
            SET 
                luta_atual_id = @LutaId,
                status = @Status
            WHERE id = @TatameId;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            TatameId = tatameId,
            LutaId = lutaId,
            Status = status.ToString()
        }, cancellationToken: ct));
    }
}
