using System.Collections.Concurrent;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;

namespace Judoo.IntegrationTests.Fixtures;

public class InMemoryTestStore
{
    public ConcurrentDictionary<Guid, Campeonato> Campeonatos { get; } = new();
    public ConcurrentDictionary<Guid, Categoria> Categorias { get; } = new();
    public ConcurrentDictionary<Guid, Tatame> Tatames { get; } = new();
    public ConcurrentDictionary<Guid, Chave> Chaves { get; } = new();
    public ConcurrentDictionary<Guid, Academia> Academias { get; } = new();
    public ConcurrentDictionary<Guid, Atleta> Atletas { get; } = new();
    public ConcurrentDictionary<Guid, Pesagem> Pesagens { get; } = new();
    public ConcurrentDictionary<Guid, Luta> Lutas { get; } = new();
    public ConcurrentDictionary<Guid, PlacarLuta> Placares { get; } = new();
    public ConcurrentDictionary<Guid, Usuario> Usuarios { get; } = new();
    public ConcurrentBag<EventoPontuacao> Eventos { get; } = new();
}

public class InMemoryUsuarioRepository : IUsuarioRepository
{
    private readonly InMemoryTestStore _store;

    public InMemoryUsuarioRepository(InMemoryTestStore store) => _store = store;

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.Usuarios.TryGetValue(id, out var u) ? u : null);

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_store.Usuarios.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<Usuario>> ObterTodosAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Usuario>>(_store.Usuarios.Values.OrderBy(u => u.Nome).ToList());

    public Task CriarAsync(Usuario usuario, CancellationToken ct = default)
    {
        _store.Usuarios[usuario.Id] = usuario;
        return Task.CompletedTask;
    }

    public Task AtualizarStatusAsync(Guid id, bool ativo, CancellationToken ct = default)
    {
        if (_store.Usuarios.TryGetValue(id, out var u))
        {
            u.Ativo = ativo;
        }
        return Task.CompletedTask;
    }
}

public class InMemoryCampeonatoRepository : ICampeonatoRepository
{
    private readonly InMemoryTestStore _store;

    public InMemoryCampeonatoRepository(InMemoryTestStore store) => _store = store;

    public Task<IEnumerable<Campeonato>> ObterTodosAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Campeonato>>(_store.Campeonatos.Values.OrderByDescending(c => c.DataInicio).ToList());

    public Task<Campeonato?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.Campeonatos.TryGetValue(id, out var c) ? c : null);

    public Task CriarAsync(Campeonato campeonato, CancellationToken ct = default)
    {
        _store.Campeonatos[campeonato.Id] = campeonato;
        return Task.CompletedTask;
    }

    public Task AtualizarStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        if (_store.Campeonatos.TryGetValue(id, out var c))
        {
            c.Status = status;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Categoria>> ObterCategoriasAsync(Guid campeonatoId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Categoria>>(_store.Categorias.Values.Where(c => c.CampeonatoId == campeonatoId).ToList());

    public Task CriarCategoriaAsync(Categoria categoria, CancellationToken ct = default)
    {
        _store.Categorias[categoria.Id] = categoria;
        return Task.CompletedTask;
    }

    public Task<Chave?> ObterOuCriarChaveAsync(Guid campeonatoId, Guid categoriaId, CancellationToken ct = default)
    {
        var existing = _store.Chaves.Values.FirstOrDefault(c => c.CampeonatoId == campeonatoId && c.CategoriaId == categoriaId);
        if (existing != null) return Task.FromResult<Chave?>(existing);

        var nova = new Chave
        {
            Id = Guid.NewGuid(),
            CampeonatoId = campeonatoId,
            CategoriaId = categoriaId,
            Tipo = "ELIMINATORIA_SIMPLES",
            TotalFases = 2,
            Gerada = true,
            CriadaEm = DateTime.UtcNow
        };
        _store.Chaves[nova.Id] = nova;
        return Task.FromResult<Chave?>(nova);
    }

    public Task<IEnumerable<Chave>> ObterChavesAsync(Guid campeonatoId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Chave>>(_store.Chaves.Values.Where(c => c.CampeonatoId == campeonatoId).ToList());

    public Task AtualizarChaveGeradaAsync(Guid chaveId, int totalFases, bool gerada, CancellationToken ct = default)
    {
        if (_store.Chaves.TryGetValue(chaveId, out var chave))
        {
            chave.TotalFases = totalFases;
            chave.Gerada = gerada;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Pesagem>> ObterPesagensPorCategoriaAsync(Guid campeonatoId, Guid categoriaId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Pesagem>>(
            _store.Pesagens.Values
                .Where(p => p.CampeonatoId == campeonatoId && p.CategoriaId == categoriaId)
                .OrderBy(p => p.DataPesagem)
                .ToList());
}

public class InMemoryTatameRepository : ITatameRepository
{
    private readonly InMemoryTestStore _store;

    public InMemoryTatameRepository(InMemoryTestStore store) => _store = store;

    public Task<Tatame?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.Tatames.TryGetValue(id, out var t) ? t : null);

    public Task<IEnumerable<Tatame>> ObterTodosPorCampeonatoAsync(Guid campeonatoId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Tatame>>(_store.Tatames.Values.Where(t => t.CampeonatoId == campeonatoId).OrderBy(t => t.Numero).ToList());

    public Task CriarAsync(Tatame tatame, CancellationToken ct = default)
    {
        _store.Tatames[tatame.Id] = tatame;
        return Task.CompletedTask;
    }

    public Task AtualizarLutaAtualAsync(Guid tatameId, Guid? lutaId, StatusTatame status, CancellationToken ct = default)
    {
        if (_store.Tatames.TryGetValue(tatameId, out var t))
        {
            t.LutaAtualId = lutaId;
            t.Status = status;
        }
        return Task.CompletedTask;
    }
}

public class InMemoryAtletaRepository : IAtletaRepository
{
    private readonly InMemoryTestStore _store;

    public InMemoryAtletaRepository(InMemoryTestStore store) => _store = store;

    public Task<IEnumerable<Atleta>> ObterTodosAsync(Guid? campeonatoId = null, CancellationToken ct = default)
    {
        var atletas = _store.Atletas.Values.Where(a => a.Ativo).AsEnumerable();
        if (campeonatoId.HasValue)
        {
            var inscritos = _store.Pesagens.Values
                .Where(p => p.CampeonatoId == campeonatoId.Value)
                .Select(p => p.AtletaId)
                .ToHashSet();
            atletas = atletas.Where(a => inscritos.Contains(a.Id));
        }

        return Task.FromResult<IEnumerable<Atleta>>(atletas.Select(HydrateAcademia).OrderBy(a => a.Nome).ToList());
    }

    public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_store.Atletas.TryGetValue(id, out var atleta)) return Task.FromResult<Atleta?>(null);
        return Task.FromResult<Atleta?>(HydrateAcademia(atleta));
    }

    public Task CriarAsync(Atleta atleta, CancellationToken ct = default)
    {
        _store.Atletas[atleta.Id] = atleta;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Academia>> ObterAcademiasAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Academia>>(_store.Academias.Values.OrderBy(a => a.Nome).ToList());

    public Task CriarAcademiaAsync(Academia academia, CancellationToken ct = default)
    {
        _store.Academias[academia.Id] = academia;
        return Task.CompletedTask;
    }

    public Task InscreverEmCampeonatoAsync(Pesagem inscricao, CancellationToken ct = default)
    {
        _store.Pesagens[inscricao.Id] = inscricao;
        return Task.CompletedTask;
    }

    private Atleta HydrateAcademia(Atleta atleta)
    {
        if (_store.Academias.TryGetValue(atleta.AcademiaId, out var academia))
        {
            atleta.NomeAcademia = academia.Nome;
            atleta.SiglaAcademia = academia.Sigla;
        }
        return atleta;
    }
}

public class InMemoryLutaRepository : ILutaRepository
{
    private readonly InMemoryTestStore _store;

    public InMemoryLutaRepository(InMemoryTestStore store) => _store = store;

    public Task<Luta?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_store.Lutas.TryGetValue(id, out var luta)) return Task.FromResult<Luta?>(null);
        return Task.FromResult<Luta?>(HydrateAtletas(luta));
    }

    public Task<IEnumerable<Luta>> ObterLutasPorTatameAsync(Guid tatameId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Luta>>(_store.Lutas.Values.Where(l => l.TatameId == tatameId).Select(HydrateAtletas).ToList());

    public Task<IEnumerable<Luta>> ObterLutasPorChaveAsync(Guid chaveId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Luta>>(_store.Lutas.Values.Where(l => l.ChaveId == chaveId).Select(HydrateAtletas).ToList());

    public Task<IEnumerable<Luta>> ObterTodasAsync(Guid? campeonatoId = null, CancellationToken ct = default)
    {
        var lutas = _store.Lutas.Values.AsEnumerable();
        if (campeonatoId.HasValue)
        {
            var chavesCamp = _store.Chaves.Values
                .Where(c => c.CampeonatoId == campeonatoId.Value)
                .Select(c => c.Id)
                .ToHashSet();
            lutas = lutas.Where(l => chavesCamp.Contains(l.ChaveId));
        }

        return Task.FromResult<IEnumerable<Luta>>(lutas.Select(HydrateAtletas).ToList());
    }

    public Task CriarLutaAsync(Luta luta, CancellationToken ct = default)
    {
        _store.Lutas[luta.Id] = luta;
        _store.Placares.TryAdd(luta.Id, new PlacarLuta(luta.Id, luta.TempoLutaSegundos));
        return Task.CompletedTask;
    }

    public Task AtualizarStatusLutaAsync(Guid lutaId, StatusLuta status, Guid? vencedorId, TipoVitoria? tipoVitoria, CancellationToken ct = default)
    {
        if (_store.Lutas.TryGetValue(lutaId, out var luta))
        {
            luta.Status = status;
            luta.VencedorId = vencedorId;
            luta.TipoVitoria = tipoVitoria;
        }
        return Task.CompletedTask;
    }

    public Task AtribuirTatameAsync(Guid lutaId, Guid? tatameId, int? ordemNoTatame, CancellationToken ct = default)
    {
        if (_store.Lutas.TryGetValue(lutaId, out var luta))
        {
            luta.TatameId = tatameId;
            luta.OrdemNoTatame = ordemNoTatame;
        }
        return Task.CompletedTask;
    }

    public Task RegistrarEventoPontuacaoAsync(EventoPontuacao evento, CancellationToken ct = default)
    {
        _store.Eventos.Add(evento);
        return Task.CompletedTask;
    }

    private Luta HydrateAtletas(Luta luta)
    {
        if (luta.AtletaBrancoId.HasValue && _store.Atletas.TryGetValue(luta.AtletaBrancoId.Value, out var ab))
        {
            if (_store.Academias.TryGetValue(ab.AcademiaId, out var acb))
            {
                ab.NomeAcademia = acb.Nome;
                ab.SiglaAcademia = acb.Sigla;
            }
            luta.AtletaBranco = ab;
        }

        if (luta.AtletaAzulId.HasValue && _store.Atletas.TryGetValue(luta.AtletaAzulId.Value, out var az))
        {
            if (_store.Academias.TryGetValue(az.AcademiaId, out var acz))
            {
                az.NomeAcademia = acz.Nome;
                az.SiglaAcademia = acz.Sigla;
            }
            luta.AtletaAzul = az;
        }

        return luta;
    }
}

public class InMemoryPlacarLutaRepository : IPlacarLutaRepository
{
    private readonly InMemoryTestStore _store;

    public InMemoryPlacarLutaRepository(InMemoryTestStore store) => _store = store;

    public Task<PlacarLuta?> ObterPorLutaIdAsync(Guid lutaId, CancellationToken ct = default)
        => Task.FromResult(_store.Placares.TryGetValue(lutaId, out var p) ? p : null);

    public Task SalvarOuAtualizarAsync(PlacarLuta placar, CancellationToken ct = default)
    {
        _store.Placares[placar.LutaId] = placar;
        return Task.CompletedTask;
    }
}
