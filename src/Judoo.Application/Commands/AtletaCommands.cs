using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Repositories;
using MediatR;

namespace Judoo.Application.Commands;

// 1. Cadastrar Atleta e Opcionalmente Inscrever
public record CadastrarAtletaCommand(
    Guid AcademiaId,
    string Nome,
    string? RegistroFederacao,
    DateTime DataNascimento,
    string Sexo,
    string? FotoUrl,
    Guid? CampeonatoId = null,
    Guid? CategoriaId = null,
    decimal? PesoAferidoKg = null
) : IRequest<AtletaCadastroDto>;

public class CadastrarAtletaCommandHandler : IRequestHandler<CadastrarAtletaCommand, AtletaCadastroDto>
{
    private readonly IAtletaRepository _atletaRepo;

    public CadastrarAtletaCommandHandler(IAtletaRepository atletaRepo)
    {
        _atletaRepo = atletaRepo;
    }

    public async Task<AtletaCadastroDto> Handle(CadastrarAtletaCommand request, CancellationToken cancellationToken)
    {
        var atleta = new Atleta
        {
            Id = Guid.NewGuid(),
            AcademiaId = request.AcademiaId,
            Nome = request.Nome,
            RegistroFederacao = request.RegistroFederacao,
            DataNascimento = request.DataNascimento,
            Sexo = request.Sexo,
            FotoUrl = request.FotoUrl,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        await _atletaRepo.CriarAsync(atleta, cancellationToken);

        // Se informou campeonato e categoria, já cria o registro de inscrição/pesagem
        if (request.CampeonatoId.HasValue && request.CategoriaId.HasValue)
        {
            var inscricao = new Pesagem
            {
                Id = Guid.NewGuid(),
                CampeonatoId = request.CampeonatoId.Value,
                AtletaId = atleta.Id,
                CategoriaId = request.CategoriaId.Value,
                PesoAferidoKg = request.PesoAferidoKg ?? 0m,
                Status = request.PesoAferidoKg.HasValue && request.PesoAferidoKg.Value > 0 ? "APROVADO" : "PENDENTE",
                DataPesagem = DateTime.UtcNow,
                Observacoes = "Inscrição online realizada"
            };
            await _atletaRepo.InscreverEmCampeonatoAsync(inscricao, cancellationToken);
        }

        var atletaSalvo = await _atletaRepo.ObterPorIdAsync(atleta.Id, cancellationToken);

        return new AtletaCadastroDto(
            atleta.Id,
            atleta.AcademiaId,
            atleta.Nome,
            atleta.RegistroFederacao,
            atleta.DataNascimento,
            atleta.Sexo,
            atleta.FotoUrl,
            atletaSalvo?.NomeAcademia,
            atletaSalvo?.SiglaAcademia
        );
    }
}

// 2. Inscrever Atleta em Campeonato
public record InscreverAtletaCommand(
    Guid CampeonatoId,
    Guid AtletaId,
    Guid CategoriaId,
    decimal? PesoKg = null
) : IRequest<bool>;

public class InscreverAtletaCommandHandler : IRequestHandler<InscreverAtletaCommand, bool>
{
    private readonly IAtletaRepository _atletaRepo;

    public InscreverAtletaCommandHandler(IAtletaRepository atletaRepo)
    {
        _atletaRepo = atletaRepo;
    }

    public async Task<bool> Handle(InscreverAtletaCommand request, CancellationToken cancellationToken)
    {
        var inscricao = new Pesagem
        {
            Id = Guid.NewGuid(),
            CampeonatoId = request.CampeonatoId,
            AtletaId = request.AtletaId,
            CategoriaId = request.CategoriaId,
            PesoAferidoKg = request.PesoKg ?? 0m,
            Status = request.PesoKg.HasValue && request.PesoKg.Value > 0 ? "APROVADO" : "PENDENTE",
            DataPesagem = DateTime.UtcNow
        };

        await _atletaRepo.InscreverEmCampeonatoAsync(inscricao, cancellationToken);
        return true;
    }
}

// 3. Cadastrar Academia
public record CadastrarAcademiaCommand(
    string Nome,
    string Sigla,
    string? Cidade,
    string? Estado
) : IRequest<AcademiaDto>;

public class CadastrarAcademiaCommandHandler : IRequestHandler<CadastrarAcademiaCommand, AcademiaDto>
{
    private readonly IAtletaRepository _atletaRepo;

    public CadastrarAcademiaCommandHandler(IAtletaRepository atletaRepo)
    {
        _atletaRepo = atletaRepo;
    }

    public async Task<AcademiaDto> Handle(CadastrarAcademiaCommand request, CancellationToken cancellationToken)
    {
        var academia = new Academia
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Sigla = request.Sigla.ToUpper(),
            Cidade = request.Cidade,
            Estado = request.Estado?.ToUpper(),
            CriadoEm = DateTime.UtcNow
        };

        await _atletaRepo.CriarAcademiaAsync(academia, cancellationToken);

        return new AcademiaDto(
            academia.Id,
            academia.Nome,
            academia.Sigla,
            academia.Cidade,
            academia.Estado
        );
    }
}
