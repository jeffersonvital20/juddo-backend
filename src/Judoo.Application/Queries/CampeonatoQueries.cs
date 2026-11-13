using Judoo.Application.DTOs;
using Judoo.Domain.Repositories;
using MediatR;

namespace Judoo.Application.Queries;

// 1. Obter Todos os Campeonatos
public record ObterCampeonatosQuery() : IRequest<IEnumerable<CampeonatoDto>>;

public class ObterCampeonatosQueryHandler : IRequestHandler<ObterCampeonatosQuery, IEnumerable<CampeonatoDto>>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public ObterCampeonatosQueryHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<IEnumerable<CampeonatoDto>> Handle(ObterCampeonatosQuery request, CancellationToken cancellationToken)
    {
        var campeonatos = await _campeonatoRepo.ObterTodosAsync(cancellationToken);
        return campeonatos.Select(CampeonatoDto.FromDomain);
    }
}

// 2. Obter Detalhes do Campeonato
public record ObterCampeonatoPorIdQuery(Guid Id) : IRequest<CampeonatoDto?>;

public class ObterCampeonatoPorIdQueryHandler : IRequestHandler<ObterCampeonatoPorIdQuery, CampeonatoDto?>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public ObterCampeonatoPorIdQueryHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<CampeonatoDto?> Handle(ObterCampeonatoPorIdQuery request, CancellationToken cancellationToken)
    {
        var campeonato = await _campeonatoRepo.ObterPorIdAsync(request.Id, cancellationToken);
        return campeonato != null ? CampeonatoDto.FromDomain(campeonato) : null;
    }
}

// 3. Obter Categorias do Campeonato
public record ObterCategoriasCampeonatoQuery(Guid CampeonatoId) : IRequest<IEnumerable<CategoriaDto>>;

public class ObterCategoriasCampeonatoQueryHandler : IRequestHandler<ObterCategoriasCampeonatoQuery, IEnumerable<CategoriaDto>>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public ObterCategoriasCampeonatoQueryHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<IEnumerable<CategoriaDto>> Handle(ObterCategoriasCampeonatoQuery request, CancellationToken cancellationToken)
    {
        var categorias = await _campeonatoRepo.ObterCategoriasAsync(request.CampeonatoId, cancellationToken);
        return categorias.Select(c => new CategoriaDto(
            c.Id,
            c.CampeonatoId,
            c.Classe,
            c.DivisaoPeso,
            c.PesoLimiteKg,
            c.Sexo,
            c.TempoLutaSegundos
        ));
    }
}

// 4. Obter Chaves do Campeonato
public record ObterChavesCampeonatoQuery(Guid CampeonatoId) : IRequest<IEnumerable<ChaveDto>>;

public class ObterChavesCampeonatoQueryHandler : IRequestHandler<ObterChavesCampeonatoQuery, IEnumerable<ChaveDto>>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public ObterChavesCampeonatoQueryHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<IEnumerable<ChaveDto>> Handle(ObterChavesCampeonatoQuery request, CancellationToken cancellationToken)
    {
        var chaves = await _campeonatoRepo.ObterChavesAsync(request.CampeonatoId, cancellationToken);
        return chaves.Select(ChaveDto.FromDomain);
    }
}
