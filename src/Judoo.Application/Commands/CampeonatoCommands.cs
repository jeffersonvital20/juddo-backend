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

// 3. Criar Categoria do Campeonato
public record CriarCategoriaCommand(
    Guid CampeonatoId,
    string Classe,
    string DivisaoPeso,
    decimal PesoLimiteKg,
    string Sexo,
    int TempoLutaSegundos = 240
) : IRequest<CategoriaDto>;

public class CriarCategoriaCommandHandler : IRequestHandler<CriarCategoriaCommand, CategoriaDto>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public CriarCategoriaCommandHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<CategoriaDto> Handle(CriarCategoriaCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Classe))
            throw new ArgumentException("A classe da categoria é obrigatória.");
        if (string.IsNullOrWhiteSpace(request.DivisaoPeso))
            throw new ArgumentException("A divisão de peso é obrigatória.");
        if (request.PesoLimiteKg <= 0)
            throw new ArgumentException("O peso limite deve ser maior que zero.");

        var categoria = new Categoria
        {
            Id = Guid.NewGuid(),
            CampeonatoId = request.CampeonatoId,
            Classe = request.Classe.Trim(),
            DivisaoPeso = request.DivisaoPeso.Trim(),
            PesoLimiteKg = request.PesoLimiteKg,
            Sexo = string.IsNullOrWhiteSpace(request.Sexo) ? "MASCULINO" : request.Sexo.Trim().ToUpperInvariant(),
            TempoLutaSegundos = request.TempoLutaSegundos > 0 ? request.TempoLutaSegundos : 240,
            CriadoEm = DateTime.UtcNow
        };

        await _campeonatoRepo.CriarCategoriaAsync(categoria, cancellationToken);
        await _campeonatoRepo.ObterOuCriarChaveAsync(request.CampeonatoId, categoria.Id, cancellationToken);

        return new CategoriaDto(
            categoria.Id,
            categoria.CampeonatoId,
            categoria.Classe,
            categoria.DivisaoPeso,
            categoria.PesoLimiteKg,
            categoria.Sexo,
            categoria.TempoLutaSegundos
        );
    }
}

// 4. Atualizar Categoria do Campeonato
public record AtualizarCategoriaCommand(
    Guid CategoriaId,
    string Classe,
    string DivisaoPeso,
    decimal PesoLimiteKg,
    string Sexo,
    int TempoLutaSegundos = 240
) : IRequest<CategoriaDto?>;

public class AtualizarCategoriaCommandHandler : IRequestHandler<AtualizarCategoriaCommand, CategoriaDto?>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public AtualizarCategoriaCommandHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<CategoriaDto?> Handle(AtualizarCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await _campeonatoRepo.ObterCategoriaPorIdAsync(request.CategoriaId, cancellationToken);
        if (categoria == null) return null;

        categoria.Classe = request.Classe.Trim();
        categoria.DivisaoPeso = request.DivisaoPeso.Trim();
        categoria.PesoLimiteKg = request.PesoLimiteKg;
        categoria.Sexo = string.IsNullOrWhiteSpace(request.Sexo) ? categoria.Sexo : request.Sexo.Trim().ToUpperInvariant();
        categoria.TempoLutaSegundos = request.TempoLutaSegundos > 0 ? request.TempoLutaSegundos : categoria.TempoLutaSegundos;

        await _campeonatoRepo.AtualizarCategoriaAsync(categoria, cancellationToken);

        return new CategoriaDto(
            categoria.Id,
            categoria.CampeonatoId,
            categoria.Classe,
            categoria.DivisaoPeso,
            categoria.PesoLimiteKg,
            categoria.Sexo,
            categoria.TempoLutaSegundos
        );
    }
}

// 5. Remover Categoria do Campeonato
public record RemoverCategoriaCommand(Guid CategoriaId) : IRequest<bool>;

public class RemoverCategoriaCommandHandler : IRequestHandler<RemoverCategoriaCommand, bool>
{
    private readonly ICampeonatoRepository _campeonatoRepo;

    public RemoverCategoriaCommandHandler(ICampeonatoRepository campeonatoRepo)
    {
        _campeonatoRepo = campeonatoRepo;
    }

    public async Task<bool> Handle(RemoverCategoriaCommand request, CancellationToken cancellationToken)
    {
        var categoria = await _campeonatoRepo.ObterCategoriaPorIdAsync(request.CategoriaId, cancellationToken);
        if (categoria == null) return false;

        await _campeonatoRepo.RemoverCategoriaAsync(request.CategoriaId, cancellationToken);
        return true;
    }
}
