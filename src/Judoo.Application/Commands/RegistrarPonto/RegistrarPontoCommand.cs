using FluentValidation;
using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using Judoo.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Judoo.Application.Commands.RegistrarPonto;

public record RegistrarPontoCommand(
    Guid LutaId,
    Guid TatameId,
    CorJudogi Judogi,
    TipoPontuacao Tipo,
    int TempoLutaSegundos,
    Guid? UsuarioId = null
) : IRequest<RegistrarPontoResult>;

public record RegistrarPontoResult(
    bool Sucesso,
    PlacarLutaDto? Placar,
    bool LutaEncerrada,
    CorJudogi? Vencedor,
    TipoVitoria? TipoVitoria,
    string? Mensagem
);

public class RegistrarPontoCommandValidator : AbstractValidator<RegistrarPontoCommand>
{
    public RegistrarPontoCommandValidator()
    {
        RuleFor(x => x.LutaId).NotEmpty().WithMessage("O ID da luta é obrigatório.");
        RuleFor(x => x.TatameId).NotEmpty().WithMessage("O ID do tatame é obrigatório.");
        RuleFor(x => x.TempoLutaSegundos).GreaterThanOrEqualTo(0).WithMessage("Tempo da luta não pode ser negativo.");
    }
}

public class RegistrarPontoCommandHandler : IRequestHandler<RegistrarPontoCommand, RegistrarPontoResult>
{
    private readonly ILutaRepository _lutaRepository;
    private readonly IPlacarLutaRepository _placarRepository;
    private readonly ITatameNotificationService _notificationService;
    private readonly ILogger<RegistrarPontoCommandHandler> _logger;

    public RegistrarPontoCommandHandler(
        ILutaRepository lutaRepository,
        IPlacarLutaRepository placarRepository,
        ITatameNotificationService notificationService,
        ILogger<RegistrarPontoCommandHandler> logger)
    {
        _lutaRepository = lutaRepository;
        _placarRepository = placarRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<RegistrarPontoResult> Handle(RegistrarPontoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registrando ponto para a Luta {LutaId}: {Tipo} para {Judogi}",
            request.LutaId, request.Tipo, request.Judogi);

        // 1. Obter placar atual
        var placar = await _placarRepository.ObterPorLutaIdAsync(request.LutaId, cancellationToken);
        if (placar == null)
        {
            placar = new PlacarLuta(request.LutaId);
        }

        // Mantém o tempo atual da luta sincronizado com o enviado pela mesa
        if (request.TempoLutaSegundos >= 0)
        {
            placar.CronometroSegundos = request.TempoLutaSegundos;
        }

        // 2. Aplicar regras da FIJ via Domínio
        var (lutaEncerrada, vencedor, tipoVitoria) = placar.AplicarPontuacao(request.Judogi, request.Tipo);

        // 3. Persistir placar atualizado com alta performance (Dapper)
        await _placarRepository.SalvarOuAtualizarAsync(placar, cancellationToken);

        // 4. Registrar evento na trilha de auditoria (Event Sourcing)
        var evento = new EventoPontuacao
        {
            LutaId = request.LutaId,
            CorJudogi = request.Judogi,
            Tipo = request.Tipo,
            TempoLutaSegundos = request.TempoLutaSegundos,
            RegistradoPorUsuarioId = request.UsuarioId,
            Descricao = $"{request.Tipo} computado para judogi {request.Judogi}"
        };
        await _lutaRepository.RegistrarEventoPontuacaoAsync(evento, cancellationToken);

        // 5. Se a luta encerrou por Ippon, Hansoku-make ou Golden Score, atualiza a luta
        if (lutaEncerrada && vencedor.HasValue && tipoVitoria.HasValue)
        {
            var luta = await _lutaRepository.ObterPorIdAsync(request.LutaId, cancellationToken);
            Guid? vencedorId = vencedor.Value == CorJudogi.Branco ? luta?.AtletaBrancoId : luta?.AtletaAzulId;

            await _lutaRepository.AtualizarStatusLutaAsync(request.LutaId, StatusLuta.Finalizada, vencedorId, tipoVitoria, cancellationToken);
            await _notificationService.NotificarLutaFinalizadaAsync(request.TatameId, request.LutaId, vencedor, tipoVitoria.Value, cancellationToken);
        }

        // 6. Broadcast em tempo real para os clientes WebSocket (Mesa, Telão, Web Admin)
        await _notificationService.NotificarPlacarAtualizadoAsync(request.TatameId, placar, cancellationToken);

        return new RegistrarPontoResult(
            Sucesso: true,
            Placar: PlacarLutaDto.FromDomain(placar),
            LutaEncerrada: lutaEncerrada,
            Vencedor: vencedor,
            TipoVitoria: tipoVitoria,
            Mensagem: lutaEncerrada 
                ? $"Luta encerrada! Vitória de {vencedor} por {tipoVitoria}."
                : $"{request.Tipo} registrado com sucesso para {request.Judogi}."
        );
    }
}
