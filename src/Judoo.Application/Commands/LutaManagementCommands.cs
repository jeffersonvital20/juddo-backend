using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using Judoo.Domain.Services;
using MediatR;

namespace Judoo.Application.Commands;

// 1. Criar Luta Manualmente
public record CriarLutaManualCommand(
    Guid ChaveId,
    Guid? TatameId,
    Guid AtletaBrancoId,
    Guid AtletaAzulId,
    int Fase = 1,
    int OrdemNaChave = 1,
    int? OrdemNoTatame = null
) : IRequest<LutaDetalhesDto>;

public class CriarLutaManualCommandHandler : IRequestHandler<CriarLutaManualCommand, LutaDetalhesDto>
{
    private readonly ILutaRepository _lutaRepo;
    private readonly IMediator _mediator;

    public CriarLutaManualCommandHandler(ILutaRepository lutaRepo, IMediator mediator)
    {
        _lutaRepo = lutaRepo;
        _mediator = mediator;
    }

    public async Task<LutaDetalhesDto> Handle(CriarLutaManualCommand request, CancellationToken cancellationToken)
    {
        var luta = new Luta
        {
            Id = Guid.NewGuid(),
            ChaveId = request.ChaveId,
            TatameId = request.TatameId,
            AtletaBrancoId = request.AtletaBrancoId,
            AtletaAzulId = request.AtletaAzulId,
            Fase = request.Fase,
            OrdemNaChave = request.OrdemNaChave,
            OrdemNoTatame = request.OrdemNoTatame,
            Status = StatusLuta.Agendada,
            TempoLutaSegundos = 240,
            CriadaEm = DateTime.UtcNow
        };

        await _lutaRepo.CriarLutaAsync(luta, cancellationToken);

        var lutaCarregada = await _mediator.Send(new Queries.ObterLutaDetalhesQuery(luta.Id), cancellationToken);
        return lutaCarregada!;
    }
}

// 2. Alterar Status da Luta (Agendada, Chamada, EmAndamento, Finalizada)
public record AlterarStatusLutaCommand(
    Guid LutaId,
    StatusLuta NovoStatus,
    Guid? VencedorId = null,
    TipoVitoria? TipoVitoria = null
) : IRequest<bool>;

public class AlterarStatusLutaCommandHandler : IRequestHandler<AlterarStatusLutaCommand, bool>
{
    private readonly ILutaRepository _lutaRepo;
    private readonly ITatameNotificationService _notificationService;

    public AlterarStatusLutaCommandHandler(ILutaRepository lutaRepo, ITatameNotificationService notificationService)
    {
        _lutaRepo = lutaRepo;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(AlterarStatusLutaCommand request, CancellationToken cancellationToken)
    {
        await _lutaRepo.AtualizarStatusLutaAsync(request.LutaId, request.NovoStatus, request.VencedorId, request.TipoVitoria, cancellationToken);

        var luta = await _lutaRepo.ObterPorIdAsync(request.LutaId, cancellationToken);
        if (luta != null && luta.TatameId.HasValue)
        {
            if (request.NovoStatus == StatusLuta.EmAndamento)
            {
                await _notificationService.NotificarLutaIniciadaAsync(luta.TatameId.Value, luta, cancellationToken);
            }
        }

        if (request.NovoStatus == StatusLuta.Finalizada && request.VencedorId.HasValue && request.TipoVitoria.HasValue && luta != null && luta.TatameId.HasValue)
        {
            var corVencedor = request.VencedorId == luta.AtletaBrancoId ? CorJudogi.Branco : CorJudogi.Azul;
            await _notificationService.NotificarLutaFinalizadaAsync(luta.TatameId.Value, luta.Id, corVencedor, request.TipoVitoria.Value, cancellationToken);
        }

        return true;
    }
}

// 3. Atribuir Tatame à Luta
public record AtribuirTatameLutaCommand(
    Guid LutaId,
    Guid? TatameId,
    int? OrdemNoTatame
) : IRequest<bool>;

public class AtribuirTatameLutaCommandHandler : IRequestHandler<AtribuirTatameLutaCommand, bool>
{
    private readonly ILutaRepository _lutaRepo;

    public AtribuirTatameLutaCommandHandler(ILutaRepository lutaRepo)
    {
        _lutaRepo = lutaRepo;
    }

    public async Task<bool> Handle(AtribuirTatameLutaCommand request, CancellationToken cancellationToken)
    {
        await _lutaRepo.AtribuirTatameAsync(request.LutaId, request.TatameId, request.OrdemNoTatame, cancellationToken);
        return true;
    }
}

// 4. Obter Todas as Lutas
public record ObterTodasLutasQuery(Guid? CampeonatoId = null) : IRequest<IEnumerable<LutaDetalhesDto>>;

public class ObterTodasLutasQueryHandler : IRequestHandler<ObterTodasLutasQuery, IEnumerable<LutaDetalhesDto>>
{
    private readonly ILutaRepository _lutaRepo;
    private readonly IPlacarLutaRepository _placarRepo;

    public ObterTodasLutasQueryHandler(ILutaRepository lutaRepo, IPlacarLutaRepository placarRepo)
    {
        _lutaRepo = lutaRepo;
        _placarRepo = placarRepo;
    }

    public async Task<IEnumerable<LutaDetalhesDto>> Handle(ObterTodasLutasQuery request, CancellationToken cancellationToken)
    {
        var lutas = await _lutaRepo.ObterTodasAsync(request.CampeonatoId, cancellationToken);
        var result = new List<LutaDetalhesDto>();

        foreach (var l in lutas)
        {
            var placar = await _placarRepo.ObterPorLutaIdAsync(l.Id, cancellationToken);
            var bDto = l.AtletaBranco != null ? new AtletaDto(l.AtletaBranco.Id, l.AtletaBranco.Nome, l.AtletaBranco.RegistroFederacao, l.AtletaBranco.NomeAcademia, l.AtletaBranco.SiglaAcademia) : null;
            var aDto = l.AtletaAzul != null ? new AtletaDto(l.AtletaAzul.Id, l.AtletaAzul.Nome, l.AtletaAzul.RegistroFederacao, l.AtletaAzul.NomeAcademia, l.AtletaAzul.SiglaAcademia) : null;

            result.Add(new LutaDetalhesDto(
                l.Id,
                l.ChaveId,
                l.TatameId,
                bDto,
                aDto,
                placar != null ? PlacarLutaDto.FromDomain(placar) : null,
                l.Status.ToString(),
                l.TipoVitoria?.ToString(),
                l.EmGoldenScore,
                l.TempoLutaSegundos,
                l.Fase,
                l.OrdemNaChave,
                l.VencedorId
            ));
        }

        return result;
    }
}

// 5. Montar Grade de Lutas da Categoria
public record GerarGradeLutasCommand(
    Guid CampeonatoId,
    Guid CategoriaId,
    Guid? TatameId = null
) : IRequest<IEnumerable<LutaDetalhesDto>>;

public class GerarGradeLutasCommandHandler : IRequestHandler<GerarGradeLutasCommand, IEnumerable<LutaDetalhesDto>>
{
    private readonly ICampeonatoRepository _campeonatoRepo;
    private readonly ITatameRepository _tatameRepo;
    private readonly ILutaRepository _lutaRepo;
    private readonly IMediator _mediator;

    public GerarGradeLutasCommandHandler(
        ICampeonatoRepository campeonatoRepo,
        ITatameRepository tatameRepo,
        ILutaRepository lutaRepo,
        IMediator mediator)
    {
        _campeonatoRepo = campeonatoRepo;
        _tatameRepo = tatameRepo;
        _lutaRepo = lutaRepo;
        _mediator = mediator;
    }

    public async Task<IEnumerable<LutaDetalhesDto>> Handle(GerarGradeLutasCommand request, CancellationToken cancellationToken)
    {
        var chave = await _campeonatoRepo.ObterOuCriarChaveAsync(request.CampeonatoId, request.CategoriaId, cancellationToken);
        if (chave == null)
            throw new InvalidOperationException("Não foi possível obter ou criar a chave da categoria.");

        var lutasExistentes = (await _lutaRepo.ObterLutasPorChaveAsync(chave.Id, cancellationToken)).ToList();
        if (lutasExistentes.Count > 0)
        {
            var existentesDto = new List<LutaDetalhesDto>();
            foreach (var le in lutasExistentes)
            {
                var dto = await _mediator.Send(new Queries.ObterLutaDetalhesQuery(le.Id), cancellationToken);
                if (dto != null) existentesDto.Add(dto);
            }
            return existentesDto;
        }

        var atletasInscritos = (await _campeonatoRepo.ObterPesagensPorCategoriaAsync(request.CampeonatoId, request.CategoriaId, cancellationToken))
            .Where(p => !string.Equals(p.Status, "REPROVADO", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.AtletaId)
            .Distinct()
            .ToList();

        if (atletasInscritos.Count < 2)
            throw new InvalidOperationException("São necessários ao menos 2 atletas inscritos na categoria para montar a grade de lutas.");

        var tatames = (await _tatameRepo.ObterTodosPorCampeonatoAsync(request.CampeonatoId, cancellationToken)).ToList();
        var totalFases = Math.Max(1, (int)Math.Ceiling(Math.Log2(atletasInscritos.Count)));
        int lutasPrimeiraFase = 1 << (totalFases - 1); // Ex: 4 atletas -> totalFases=2 -> 2 semifinais
        int byes = (1 << totalFases) - atletasInscritos.Count;
        int ordemNoTatameGlobal = 1;

        var lutasPrimeiraRodada = new List<Luta>();
        int idxAtleta = 0;

        // 1. Cria as lutas da fase inicial (Ex: Oitavas=4, Quartas=3, Semifinal=2, Final=1)
        for (int m = 1; m <= lutasPrimeiraFase; m++)
        {
            Guid? tatameDestino = request.TatameId
                ?? (tatames.Count > 0 ? tatames[(ordemNoTatameGlobal - 1) % tatames.Count].Id : null);

            Guid? brancoId = idxAtleta < atletasInscritos.Count ? atletasInscritos[idxAtleta++] : null;
            Guid? azulId = null;

            // Se ainda há lutas completas para preencher (sem BYE neste confronto), aloca o segundo atleta
            if (m <= (lutasPrimeiraFase - byes) && idxAtleta < atletasInscritos.Count)
            {
                azulId = atletasInscritos[idxAtleta++];
            }

            var luta = new Luta
            {
                Id = Guid.NewGuid(),
                ChaveId = chave.Id,
                TatameId = tatameDestino,
                AtletaBrancoId = brancoId,
                AtletaAzulId = azulId,
                Fase = totalFases,
                OrdemNaChave = m,
                OrdemNoTatame = ordemNoTatameGlobal++,
                Status = StatusLuta.Agendada,
                TempoLutaSegundos = 240,
                CriadaEm = DateTime.UtcNow
            };

            await _lutaRepo.CriarLutaAsync(luta, cancellationToken);
            lutasPrimeiraRodada.Add(luta);
        }

        // 2. Cria todas as fases seguintes da Copa até a Grande Final (Fase 1)
        for (int f = totalFases - 1; f >= 1; f--)
        {
            int qtdLutasFase = 1 << (f - 1);
            for (int m = 1; m <= qtdLutasFase; m++)
            {
                Guid? tatameDestino = request.TatameId
                    ?? (tatames.Count > 0 ? tatames[(ordemNoTatameGlobal - 1) % tatames.Count].Id : null);

                var lutaFaseSeguinte = new Luta
                {
                    Id = Guid.NewGuid(),
                    ChaveId = chave.Id,
                    TatameId = tatameDestino,
                    AtletaBrancoId = null,
                    AtletaAzulId = null,
                    Fase = f,
                    OrdemNaChave = m,
                    OrdemNoTatame = ordemNoTatameGlobal++,
                    Status = StatusLuta.Agendada,
                    TempoLutaSegundos = 240,
                    CriadaEm = DateTime.UtcNow
                };

                await _lutaRepo.CriarLutaAsync(lutaFaseSeguinte, cancellationToken);
            }
        }

        // 3. Processa os BYEs da primeira fase (atletas sem oponente avançam automaticamente para a próxima fase)
        foreach (var lutaInicial in lutasPrimeiraRodada)
        {
            if (lutaInicial.AtletaBrancoId.HasValue && !lutaInicial.AtletaAzulId.HasValue)
            {
                await _lutaRepo.AtualizarStatusLutaAsync(
                    lutaInicial.Id,
                    StatusLuta.Finalizada,
                    lutaInicial.AtletaBrancoId,
                    TipoVitoria.WO,
                    cancellationToken);
            }
        }

        await _campeonatoRepo.AtualizarChaveGeradaAsync(chave.Id, totalFases, true, cancellationToken);

        var todasDaChave = await _lutaRepo.ObterLutasPorChaveAsync(chave.Id, cancellationToken);
        var criadas = new List<LutaDetalhesDto>();
        foreach (var lc in todasDaChave)
        {
            var detalhe = await _mediator.Send(new Queries.ObterLutaDetalhesQuery(lc.Id), cancellationToken);
            if (detalhe != null) criadas.Add(detalhe);
        }

        return criadas;
    }
}

// 6. Definir / Iniciar Luta Atual no Tatame
public record DefinirLutaAtualTatameCommand(
    Guid TatameId,
    Guid? LutaId,
    bool IniciarLuta = false
) : IRequest<bool>;

public class DefinirLutaAtualTatameCommandHandler : IRequestHandler<DefinirLutaAtualTatameCommand, bool>
{
    private readonly ITatameRepository _tatameRepo;
    private readonly ILutaRepository _lutaRepo;
    private readonly ITatameNotificationService _notificationService;

    public DefinirLutaAtualTatameCommandHandler(
        ITatameRepository tatameRepo,
        ILutaRepository lutaRepo,
        ITatameNotificationService notificationService)
    {
        _tatameRepo = tatameRepo;
        _lutaRepo = lutaRepo;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(DefinirLutaAtualTatameCommand request, CancellationToken cancellationToken)
    {
        var tatame = await _tatameRepo.ObterPorIdAsync(request.TatameId, cancellationToken);
        if (tatame == null) return false;

        Luta? luta = null;
        if (request.LutaId.HasValue)
        {
            luta = await _lutaRepo.ObterPorIdAsync(request.LutaId.Value, cancellationToken);
            if (luta == null) return false;
        }

        var statusTatame = request.LutaId.HasValue && request.IniciarLuta
            ? StatusTatame.EmLuta
            : StatusTatame.Disponivel;

        await _tatameRepo.AtualizarLutaAtualAsync(request.TatameId, request.LutaId, statusTatame, cancellationToken);

        if (luta != null && request.IniciarLuta)
        {
            await _lutaRepo.AtualizarStatusLutaAsync(luta.Id, StatusLuta.EmAndamento, null, null, cancellationToken);
            await _notificationService.NotificarLutaIniciadaAsync(request.TatameId, luta, cancellationToken);
        }

        return true;
    }
}
