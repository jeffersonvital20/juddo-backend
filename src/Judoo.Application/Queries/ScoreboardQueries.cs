using Judoo.Application.DTOs;
using Judoo.Domain.Repositories;
using MediatR;

namespace Judoo.Application.Queries;

// 1. Obter Placar de uma Luta
public record ObterPlacarLutaQuery(Guid LutaId) : IRequest<PlacarLutaDto?>;

public class ObterPlacarLutaQueryHandler : IRequestHandler<ObterPlacarLutaQuery, PlacarLutaDto?>
{
    private readonly IPlacarLutaRepository _placarRepo;

    public ObterPlacarLutaQueryHandler(IPlacarLutaRepository placarRepo)
    {
        _placarRepo = placarRepo;
    }

    public async Task<PlacarLutaDto?> Handle(ObterPlacarLutaQuery request, CancellationToken cancellationToken)
    {
        var placar = await _placarRepo.ObterPorLutaIdAsync(request.LutaId, cancellationToken);
        return placar != null ? PlacarLutaDto.FromDomain(placar) : null;
    }
}

// 2. Obter Detalhes da Luta
public record ObterLutaDetalhesQuery(Guid LutaId) : IRequest<LutaDetalhesDto?>;

public class ObterLutaDetalhesQueryHandler : IRequestHandler<ObterLutaDetalhesQuery, LutaDetalhesDto?>
{
    private readonly ILutaRepository _lutaRepo;
    private readonly IPlacarLutaRepository _placarRepo;

    public ObterLutaDetalhesQueryHandler(ILutaRepository lutaRepo, IPlacarLutaRepository placarRepo)
    {
        _lutaRepo = lutaRepo;
        _placarRepo = placarRepo;
    }

    public async Task<LutaDetalhesDto?> Handle(ObterLutaDetalhesQuery request, CancellationToken cancellationToken)
    {
        var luta = await _lutaRepo.ObterPorIdAsync(request.LutaId, cancellationToken);
        if (luta == null) return null;

        var placar = await _placarRepo.ObterPorLutaIdAsync(request.LutaId, cancellationToken);

        var atletaBrancoDto = luta.AtletaBranco != null
            ? new AtletaDto(luta.AtletaBranco.Id, luta.AtletaBranco.Nome, luta.AtletaBranco.RegistroFederacao, luta.AtletaBranco.NomeAcademia, luta.AtletaBranco.SiglaAcademia)
            : null;

        var atletaAzulDto = luta.AtletaAzul != null
            ? new AtletaDto(luta.AtletaAzul.Id, luta.AtletaAzul.Nome, luta.AtletaAzul.RegistroFederacao, luta.AtletaAzul.NomeAcademia, luta.AtletaAzul.SiglaAcademia)
            : null;

        return new LutaDetalhesDto(
            luta.Id,
            luta.ChaveId,
            luta.TatameId,
            atletaBrancoDto,
            atletaAzulDto,
            placar != null ? PlacarLutaDto.FromDomain(placar) : null,
            luta.Status.ToString(),
            luta.TipoVitoria?.ToString(),
            luta.EmGoldenScore,
            luta.TempoLutaSegundos,
            luta.Fase,
            luta.OrdemNaChave
        );
    }
}

// 3. Obter Tatames do Campeonato
public record ObterTatamesQuery(Guid CampeonatoId) : IRequest<IEnumerable<TatameStatusDto>>;

public class ObterTatamesQueryHandler : IRequestHandler<ObterTatamesQuery, IEnumerable<TatameStatusDto>>
{
    private readonly ITatameRepository _tatameRepo;
    private readonly IMediator _mediator;

    public ObterTatamesQueryHandler(ITatameRepository tatameRepo, IMediator mediator)
    {
        _tatameRepo = tatameRepo;
        _mediator = mediator;
    }

    public async Task<IEnumerable<TatameStatusDto>> Handle(ObterTatamesQuery request, CancellationToken cancellationToken)
    {
        var tatames = await _tatameRepo.ObterTodosPorCampeonatoAsync(request.CampeonatoId, cancellationToken);
        var result = new List<TatameStatusDto>();

        foreach (var t in tatames)
        {
            LutaDetalhesDto? lutaAtual = null;
            if (t.LutaAtualId.HasValue)
            {
                lutaAtual = await _mediator.Send(new ObterLutaDetalhesQuery(t.LutaAtualId.Value), cancellationToken);
            }

            result.Add(new TatameStatusDto(
                t.Id,
                t.Numero,
                t.Nome,
                t.Status.ToString(),
                t.LutaAtualId,
                lutaAtual
            ));
        }

        return result;
    }
}
