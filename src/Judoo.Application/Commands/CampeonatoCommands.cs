using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using MediatR;

namespace Judoo.Application.Commands;

// 1. Criar Campeonato
public record CriarCampeonatoCommand(
    string Nome,
    string Local,
    DateTime DataInicio,
    DateTime DataFim,
    int QuantidadeTatames = 2
) : IRequest<CampeonatoDto>;

public class CriarCampeonatoCommandHandler : IRequestHandler<CriarCampeonatoCommand, CampeonatoDto>
{
    private readonly ICampeonatoRepository _campeonatoRepo;
    private readonly ITatameRepository _tatameRepo;

    public CriarCampeonatoCommandHandler(ICampeonatoRepository campeonatoRepo, ITatameRepository tatameRepo)
    {
        _campeonatoRepo = campeonatoRepo;
        _tatameRepo = tatameRepo;
    }

    public async Task<CampeonatoDto> Handle(CriarCampeonatoCommand request, CancellationToken cancellationToken)
    {
        var campeonato = new Campeonato
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Local = request.Local,
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Status = "EM_ANDAMENTO",
            CriadoEm = DateTime.UtcNow
        };

        await _campeonatoRepo.CriarAsync(campeonato, cancellationToken);

        // Criar Categorias Padrões da FIJ
        var categoriasPadrao = new[]
        {
            new Categoria { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Classe = "Sênior", DivisaoPeso = "-60kg Ligeiro", PesoLimiteKg = 60.00m, Sexo = "MASCULINO", TempoLutaSegundos = 240 },
            new Categoria { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Classe = "Sênior", DivisaoPeso = "-66kg Meio-Leve", PesoLimiteKg = 66.00m, Sexo = "MASCULINO", TempoLutaSegundos = 240 },
            new Categoria { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Classe = "Sênior", DivisaoPeso = "-73kg Leve", PesoLimiteKg = 73.00m, Sexo = "MASCULINO", TempoLutaSegundos = 240 },
            new Categoria { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Classe = "Sênior", DivisaoPeso = "-81kg Meio-Médio", PesoLimiteKg = 81.00m, Sexo = "MASCULINO", TempoLutaSegundos = 240 },
            new Categoria { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Classe = "Sênior", DivisaoPeso = "-52kg Meio-Leve", PesoLimiteKg = 52.00m, Sexo = "FEMININO", TempoLutaSegundos = 240 },
            new Categoria { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, Classe = "Sênior", DivisaoPeso = "-57kg Leve", PesoLimiteKg = 57.00m, Sexo = "FEMININO", TempoLutaSegundos = 240 },
        };

        foreach (var cat in categoriasPadrao)
        {
            await _campeonatoRepo.CriarCategoriaAsync(cat, cancellationToken);
            // Cria chave para a categoria
            await _campeonatoRepo.ObterOuCriarChaveAsync(campeonato.Id, cat.Id, cancellationToken);
        }

        // Criar Tatames Solicitados
        for (int i = 1; i <= Math.Max(1, request.QuantidadeTatames); i++)
        {
            var tatame = new Tatame
            {
                Id = Guid.NewGuid(),
                CampeonatoId = campeonato.Id,
                Numero = i,
                Nome = $"Tatame {i} - Shiai-Jo {(char)('A' + i - 1)}",
                Status = StatusTatame.Disponivel,
                CriadoEm = DateTime.UtcNow
            };
            await _tatameRepo.CriarAsync(tatame, cancellationToken);
        }

        return CampeonatoDto.FromDomain(campeonato);
    }
}

// 2. Atualizar Status do Campeonato
public record AtualizarStatusCampeonatoCommand(Guid Id, string Status) : IRequest<bool>;

public class AtualizarStatusCampeonatoCommandHandler : IRequestHandler<AtualizarStatusCampeonatoCommand, bool>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public AtualizarStatusCampeonatoCommandHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<bool> Handle(AtualizarStatusCampeonatoCommand request, CancellationToken cancellationToken)
    {
        await _campeonatoRepo.AtualizarStatusAsync(request.Id, request.Status, cancellationToken);
        return true;
    }
}
