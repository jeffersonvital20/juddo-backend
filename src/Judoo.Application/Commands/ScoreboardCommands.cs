using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using Judoo.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Judoo.Application.Commands;

// 1. Atualizar Cronômetro
public record AtualizarCronometroCommand(
    Guid LutaId,
    Guid TatameId,
    int CronometroSegundos,
    bool CronometroRodando,
    bool EmGoldenScore,
    bool SonoMama
) : IRequest<PlacarLutaDto>;

public class AtualizarCronometroCommandHandler : IRequestHandler<AtualizarCronometroCommand, PlacarLutaDto>
{
    private readonly IPlacarLutaRepository _placarRepo;
    private readonly ITatameNotificationService _notificationService;

    public AtualizarCronometroCommandHandler(IPlacarLutaRepository placarRepo, ITatameNotificationService notificationService)
    {
        _placarRepo = placarRepo;
        _notificationService = notificationService;
    }

    public async Task<PlacarLutaDto> Handle(AtualizarCronometroCommand request, CancellationToken cancellationToken)
    {
        var placar = await _placarRepo.ObterPorLutaIdAsync(request.LutaId, cancellationToken)
                     ?? new PlacarLuta(request.LutaId);

        placar.CronometroSegundos = request.CronometroSegundos;
        placar.CronometroRodando = request.CronometroRodando;
        placar.EmGoldenScore = request.EmGoldenScore;
        placar.SonoMama = request.SonoMama;
        placar.UltimaAtualizacao = DateTime.UtcNow;

        await _placarRepo.SalvarOuAtualizarAsync(placar, cancellationToken);
        await _notificationService.NotificarCronometroAsync(request.TatameId, request.CronometroSegundos, request.CronometroRodando, request.EmGoldenScore, request.SonoMama, cancellationToken);

        return PlacarLutaDto.FromDomain(placar);
    }
}

// 2. Controle de Osaekomi (Imobilização)
public enum AcaoOsaekomi
{
    Iniciar,
    AtualizarSegundos,
    Toketa // Saída da imobilização
}

public record RegistrarOsaekomiCommand(
    Guid LutaId,
    Guid TatameId,
    AcaoOsaekomi Acao,
    CorJudogi? Judogi,
    int Segundos
) : IRequest<PlacarLutaDto>;

public class RegistrarOsaekomiCommandHandler : IRequestHandler<RegistrarOsaekomiCommand, PlacarLutaDto>
{
    private readonly IPlacarLutaRepository _placarRepo;
    private readonly ILutaRepository _lutaRepo;
    private readonly ITatameNotificationService _notificationService;

    public RegistrarOsaekomiCommandHandler(
        IPlacarLutaRepository placarRepo,
        ILutaRepository lutaRepo,
        ITatameNotificationService notificationService)
    {
        _placarRepo = placarRepo;
        _lutaRepo = lutaRepo;
        _notificationService = notificationService;
    }

    public async Task<PlacarLutaDto> Handle(RegistrarOsaekomiCommand request, CancellationToken cancellationToken)
    {
        var placar = await _placarRepo.ObterPorLutaIdAsync(request.LutaId, cancellationToken)
                     ?? new PlacarLuta(request.LutaId);

        switch (request.Acao)
        {
            case AcaoOsaekomi.Iniciar when request.Judogi.HasValue:
                placar.IniciarOsaekomi(request.Judogi.Value);
                break;

            case AcaoOsaekomi.AtualizarSegundos:
                var (lutaEncerrada, vencedor, tipoVitoria) = placar.AtualizarOsaekomi(request.Segundos);
                if (lutaEncerrada && vencedor.HasValue && tipoVitoria.HasValue)
                {
                    var luta = await _lutaRepo.ObterPorIdAsync(request.LutaId, cancellationToken);
                    Guid? vencedorId = vencedor.Value == CorJudogi.Branco ? luta?.AtletaBrancoId : luta?.AtletaAzulId;
                    await _lutaRepo.AtualizarStatusLutaAsync(request.LutaId, StatusLuta.Finalizada, vencedorId, tipoVitoria, cancellationToken);
                    await _notificationService.NotificarLutaFinalizadaAsync(request.TatameId, request.LutaId, vencedor, tipoVitoria.Value, cancellationToken);
                }
                break;

            case AcaoOsaekomi.Toketa:
                placar.Toketa();
                break;
        }

        await _placarRepo.SalvarOuAtualizarAsync(placar, cancellationToken);
        await _notificationService.NotificarPlacarAtualizadoAsync(request.TatameId, placar, cancellationToken);

        return PlacarLutaDto.FromDomain(placar);
    }
}
