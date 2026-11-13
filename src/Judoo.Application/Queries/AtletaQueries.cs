using Judoo.Application.DTOs;
using Judoo.Domain.Repositories;
using MediatR;

namespace Judoo.Application.Queries;

// 1. Obter Atletas
public record ObterAtletasQuery(Guid? CampeonatoId = null) : IRequest<IEnumerable<AtletaCadastroDto>>;

public class ObterAtletasQueryHandler : IRequestHandler<ObterAtletasQuery, IEnumerable<AtletaCadastroDto>>
{
    private readonly IAtletaRepository _atletaRepo;

    public ObterAtletasQueryHandler(IAtletaRepository atletaRepo)
    {
        _atletaRepo = atletaRepo;
    }

    public async Task<IEnumerable<AtletaCadastroDto>> Handle(ObterAtletasQuery request, CancellationToken cancellationToken)
    {
        var atletas = await _atletaRepo.ObterTodosAsync(request.CampeonatoId, cancellationToken);
        return atletas.Select(a => new AtletaCadastroDto(
            a.Id,
            a.AcademiaId,
            a.Nome,
            a.RegistroFederacao,
            a.DataNascimento,
            a.Sexo,
            a.FotoUrl,
            a.NomeAcademia,
            a.SiglaAcademia
        ));
    }
}

// 2. Obter Academias
public record ObterAcademiasQuery() : IRequest<IEnumerable<AcademiaDto>>;

public class ObterAcademiasQueryHandler : IRequestHandler<ObterAcademiasQuery, IEnumerable<AcademiaDto>>
{
    private readonly IAtletaRepository _atletaRepo;

    public ObterAcademiasQueryHandler(IAtletaRepository atletaRepo)
    {
        _atletaRepo = atletaRepo;
    }

    public async Task<IEnumerable<AcademiaDto>> Handle(ObterAcademiasQuery request, CancellationToken cancellationToken)
    {
        var academias = await _atletaRepo.ObterAcademiasAsync(cancellationToken);
        return academias.Select(ac => new AcademiaDto(
            ac.Id,
            ac.Nome,
            ac.Sigla,
            ac.Cidade,
            ac.Estado
        ));
    }
}
