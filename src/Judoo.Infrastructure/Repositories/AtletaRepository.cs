using Dapper;
using Judoo.Domain.Entities;
using Judoo.Domain.Repositories;
using Judoo.Infrastructure.Data;

namespace Judoo.Infrastructure.Repositories;

public class AtletaRepository : IAtletaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AtletaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Atleta>> ObterTodosAsync(Guid? campeonatoId = null, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql;
        if (campeonatoId.HasValue)
        {
            sql = @"
                SELECT 
                    a.id, a.academia_id AS AcademiaId, a.nome, a.registro_federacao AS RegistroFederacao, 
                    a.data_nascimento AS DataNascimento, a.sexo, a.foto_url AS FotoUrl, a.ativo, a.criado_em AS CriadoEm,
                    ac.nome AS NomeAcademia, ac.sigla AS SiglaAcademia
                FROM atletas a
                JOIN academias ac ON a.academia_id = ac.id
                JOIN pesagens p ON p.atleta_id = a.id
                WHERE p.campeonato_id = @CampeonatoId AND a.ativo = TRUE
                ORDER BY a.nome ASC;";
            return await conn.QueryAsync<Atleta>(new CommandDefinition(sql, new { CampeonatoId = campeonatoId }, cancellationToken: ct));
        }

        sql = @"
            SELECT 
                a.id, a.academia_id AS AcademiaId, a.nome, a.registro_federacao AS RegistroFederacao, 
                a.data_nascimento AS DataNascimento, a.sexo, a.foto_url AS FotoUrl, a.ativo, a.criado_em AS CriadoEm,
                ac.nome AS NomeAcademia, ac.sigla AS SiglaAcademia
            FROM atletas a
            LEFT JOIN academias ac ON a.academia_id = ac.id
            WHERE a.ativo = TRUE
            ORDER BY a.nome ASC;";

        return await conn.QueryAsync<Atleta>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                a.id, a.academia_id AS AcademiaId, a.nome, a.registro_federacao AS RegistroFederacao, 
                a.data_nascimento AS DataNascimento, a.sexo, a.foto_url AS FotoUrl, a.ativo, a.criado_em AS CriadoEm,
                ac.nome AS NomeAcademia, ac.sigla AS SiglaAcademia
            FROM atletas a
            LEFT JOIN academias ac ON a.academia_id = ac.id
            WHERE a.id = @Id;";

        return await conn.QuerySingleOrDefaultAsync<Atleta>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task CriarAsync(Atleta atleta, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO atletas (
                id, academia_id, nome, registro_federacao, data_nascimento, sexo, foto_url, ativo, criado_em
            ) VALUES (
                @Id, @AcademiaId, @Nome, @RegistroFederacao, @DataNascimento, @Sexo, @FotoUrl, @Ativo, @CriadoEm
            );";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            atleta.Id,
            atleta.AcademiaId,
            atleta.Nome,
            atleta.RegistroFederacao,
            atleta.DataNascimento,
            Sexo = atleta.Sexo.ToUpper(),
            atleta.FotoUrl,
            atleta.Ativo,
            atleta.CriadoEm
        }, cancellationToken: ct));
    }

    public async Task<IEnumerable<Academia>> ObterAcademiasAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id, nome, sigla, cidade, estado, criado_em AS CriadoEm
            FROM academias
            ORDER BY nome ASC;";

        return await conn.QueryAsync<Academia>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task CriarAcademiaAsync(Academia academia, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO academias (id, nome, sigla, cidade, estado, criado_em)
            VALUES (@Id, @Nome, @Sigla, @Cidade, @Estado, @CriadoEm);";

        await conn.ExecuteAsync(new CommandDefinition(sql, academia, cancellationToken: ct));
    }

    public async Task InscreverEmCampeonatoAsync(Pesagem inscricao, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO pesagens (
                id, campeonato_id, atleta_id, categoria_id, peso_aferido_kg, status, data_pesagem, observacoes
            ) VALUES (
                @Id, @CampeonatoId, @AtletaId, @CategoriaId, @PesoAferidoKg, @Status, @DataPesagem, @Observacoes
            )
            ON CONFLICT (campeonato_id, atleta_id) DO UPDATE SET
                categoria_id = EXCLUDED.categoria_id,
                peso_aferido_kg = EXCLUDED.peso_aferido_kg,
                status = EXCLUDED.status,
                data_pesagem = EXCLUDED.data_pesagem,
                observacoes = EXCLUDED.observacoes;";

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            inscricao.Id,
            inscricao.CampeonatoId,
            inscricao.AtletaId,
            inscricao.CategoriaId,
            inscricao.PesoAferidoKg,
            Status = inscricao.Status.ToUpper(),
            inscricao.DataPesagem,
            inscricao.Observacoes
        }, cancellationToken: ct));
    }
}
