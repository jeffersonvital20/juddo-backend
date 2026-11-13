using Judoo.Domain.Entities;
using Judoo.Domain.Enums;

namespace Judoo.Domain.Repositories;

public interface ILutaRepository
{
    Task<Luta?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Luta>> ObterLutasPorTatameAsync(Guid tatameId, CancellationToken ct = default);
    Task<IEnumerable<Luta>> ObterLutasPorChaveAsync(Guid chaveId, CancellationToken ct = default);
    Task<IEnumerable<Luta>> ObterTodasAsync(Guid? campeonatoId = null, CancellationToken ct = default);
    Task CriarLutaAsync(Luta luta, CancellationToken ct = default);
    Task AtualizarStatusLutaAsync(Guid lutaId, StatusLuta status, Guid? vencedorId, TipoVitoria? tipoVitoria, CancellationToken ct = default);
    Task AtribuirTatameAsync(Guid lutaId, Guid? tatameId, int? ordemNoTatame, CancellationToken ct = default);
    Task RegistrarEventoPontuacaoAsync(EventoPontuacao evento, CancellationToken ct = default);
}

public interface IPlacarLutaRepository
{
    Task<PlacarLuta?> ObterPorLutaIdAsync(Guid lutaId, CancellationToken ct = default);
    Task SalvarOuAtualizarAsync(PlacarLuta placar, CancellationToken ct = default);
}

public interface ITatameRepository
{
    Task<Tatame?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Tatame>> ObterTodosPorCampeonatoAsync(Guid campeonatoId, CancellationToken ct = default);
    Task CriarAsync(Tatame tatame, CancellationToken ct = default);
    Task AtualizarLutaAtualAsync(Guid tatameId, Guid? lutaId, StatusTatame status, CancellationToken ct = default);
}

public interface ICampeonatoRepository
{
    Task<IEnumerable<Campeonato>> ObterTodosAsync(CancellationToken ct = default);
    Task<Campeonato?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task CriarAsync(Campeonato campeonato, CancellationToken ct = default);
    Task AtualizarStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<IEnumerable<Categoria>> ObterCategoriasAsync(Guid campeonatoId, CancellationToken ct = default);
    Task CriarCategoriaAsync(Categoria categoria, CancellationToken ct = default);
    Task<IEnumerable<Chave>> ObterChavesAsync(Guid campeonatoId, CancellationToken ct = default);
    Task<Chave?> ObterOuCriarChaveAsync(Guid campeonatoId, Guid categoriaId, CancellationToken ct = default);
    Task AtualizarChaveGeradaAsync(Guid chaveId, int totalFases, bool gerada, CancellationToken ct = default);
    Task<IEnumerable<Pesagem>> ObterPesagensPorCategoriaAsync(Guid campeonatoId, Guid categoriaId, CancellationToken ct = default);
}

public interface IAtletaRepository
{
    Task<IEnumerable<Atleta>> ObterTodosAsync(Guid? campeonatoId = null, CancellationToken ct = default);
    Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task CriarAsync(Atleta atleta, CancellationToken ct = default);
    Task<IEnumerable<Academia>> ObterAcademiasAsync(CancellationToken ct = default);
    Task CriarAcademiaAsync(Academia academia, CancellationToken ct = default);
    Task InscreverEmCampeonatoAsync(Pesagem inscricao, CancellationToken ct = default);
}

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<Usuario>> ObterTodosAsync(CancellationToken ct = default);
    Task CriarAsync(Usuario usuario, CancellationToken ct = default);
    Task AtualizarStatusAsync(Guid id, bool ativo, CancellationToken ct = default);
}
