using Dapper;
using Judoo.Domain.Entities;
using Judoo.Domain.Repositories;
using Judoo.Infrastructure.Data;

namespace Judoo.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UsuarioRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, nome, email, senha_hash AS SenhaHash, role, ativo, criado_em AS CriadoEm
            FROM usuarios
            WHERE id = @Id;";

        return await conn.QuerySingleOrDefaultAsync<Usuario>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, nome, email, senha_hash AS SenhaHash, role, ativo, criado_em AS CriadoEm
            FROM usuarios
            WHERE LOWER(email) = LOWER(@Email);";

        return await conn.QuerySingleOrDefaultAsync<Usuario>(new CommandDefinition(sql, new { Email = email }, cancellationToken: ct));
    }

    public async Task<IEnumerable<Usuario>> ObterTodosAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, nome, email, senha_hash AS SenhaHash, role, ativo, criado_em AS CriadoEm
            FROM usuarios
            ORDER BY nome ASC;";

        return await conn.QueryAsync<Usuario>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task CriarAsync(Usuario usuario, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO usuarios (id, nome, email, senha_hash, role, ativo, criado_em)
            VALUES (@Id, @Nome, @Email, @SenhaHash, @Role, @Ativo, @CriadoEm);";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.SenhaHash,
            Role = usuario.Role.ToString(),
            usuario.Ativo,
            usuario.CriadoEm
        }, cancellationToken: ct));
    }

    public async Task AtualizarAsync(Usuario usuario, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE usuarios
            SET nome = @Nome,
                email = @Email,
                senha_hash = @SenhaHash,
                role = @Role,
                ativo = @Ativo
            WHERE id = @Id;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.SenhaHash,
            Role = usuario.Role.ToString(),
            usuario.Ativo
        }, cancellationToken: ct));
    }

    public async Task AtualizarStatusAsync(Guid id, bool ativo, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE usuarios
            SET ativo = @Ativo
            WHERE id = @Id;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Ativo = ativo }, cancellationToken: ct));
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE pesagens SET aferido_por_usuario_id = NULL WHERE aferido_por_usuario_id = @Id;
            UPDATE tatames SET mesario_id = NULL WHERE mesario_id = @Id;
            UPDATE eventos_pontuacao SET registrado_por_usuario_id = NULL WHERE registrado_por_usuario_id = @Id;
            DELETE FROM usuarios WHERE id = @Id;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }
}
