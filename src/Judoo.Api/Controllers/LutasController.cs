using Judoo.Application.Commands;
using Judoo.Application.Commands.RegistrarPonto;
using Judoo.Application.DTOs;
using Judoo.Application.Queries;
using Judoo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judoo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LutasController : ControllerBase
{
    private readonly IMediator _mediator;

    public LutasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lista todas as lutas, com filtro opcional por campeonato.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<LutaDetalhesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterTodas([FromQuery] Guid? campeonatoId)
    {
        var result = await _mediator.Send(new ObterTodasLutasQuery(campeonatoId));
        return Ok(result);
    }

    /// <summary>
    /// Obtém detalhes de uma luta específica, atletas e placar atual.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LutaDetalhesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterLutaDetalhes(Guid id)
    {
        var result = await _mediator.Send(new ObterLutaDetalhesQuery(id));
        if (result == null) return NotFound(new { mensagem = "Luta não encontrada." });
        return Ok(result);
    }

    /// <summary>
    /// Consulta ultra-rápida do placar atual da luta.
    /// </summary>
    [HttpGet("{id:guid}/placar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PlacarLutaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPlacar(Guid id)
    {
        var result = await _mediator.Send(new ObterPlacarLutaQuery(id));
        if (result == null) return NotFound(new { mensagem = "Placar não encontrado." });
        return Ok(result);
    }

    /// <summary>
    /// Cria e agenda uma nova luta entre dois atletas (Admin ou Secretaria).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Secretaria")]
    [ProducesResponseType(typeof(LutaDetalhesDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarLuta([FromBody] CriarLutaManualCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(ObterLutaDetalhes), new { id = result.Id }, result);
    }

    public record AlterarStatusRequest(
        StatusLuta Status,
        Guid? VencedorId,
        TipoVitoria? TipoVitoria
    );

    /// <summary>
    /// Altera o status da luta (Admin ou Mesario).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,Mesario")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusRequest req)
    {
        await _mediator.Send(new AlterarStatusLutaCommand(id, req.Status, req.VencedorId, req.TipoVitoria));
        return Ok(new { mensagem = $"Status da luta alterado para {req.Status}." });
    }

    public record AtribuirTatameRequest(Guid? TatameId, int? OrdemNoTatame);

    /// <summary>
    /// Atribui a luta a um tatame específico e define sua ordem de combate (Admin ou Secretaria).
    /// </summary>
    [HttpPut("{id:guid}/tatame")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> AtribuirTatame(Guid id, [FromBody] AtribuirTatameRequest req)
    {
        await _mediator.Send(new AtribuirTatameLutaCommand(id, req.TatameId, req.OrdemNoTatame));
        return Ok(new { mensagem = "Tatame atribuído à luta com sucesso." });
    }

    public record PontuarRequest(
        Guid TatameId,
        CorJudogi Judogi,
        TipoPontuacao Tipo,
        int TempoLutaSegundos,
        Guid? UsuarioId
    );

    /// <summary>
    /// Registra pontuação ou falta técnica (Admin ou Mesario).
    /// </summary>
    [HttpPost("{id:guid}/pontuar")]
    [Authorize(Roles = "Admin,Mesario")]
    [ProducesResponseType(typeof(RegistrarPontoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarPonto(Guid id, [FromBody] PontuarRequest req)
    {
        var command = new RegistrarPontoCommand(
            LutaId: id,
            TatameId: req.TatameId,
            Judogi: req.Judogi,
            Tipo: req.Tipo,
            TempoLutaSegundos: req.TempoLutaSegundos,
            UsuarioId: req.UsuarioId
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    public record CronometroRequest(
        Guid TatameId,
        int CronometroSegundos,
        bool CronometroRodando,
        bool EmGoldenScore,
        bool SonoMama
    );

    /// <summary>
    /// Sincroniza o cronômetro do tatame (Admin ou Mesario).
    /// </summary>
    [HttpPost("{id:guid}/cronometro")]
    [Authorize(Roles = "Admin,Mesario")]
    [ProducesResponseType(typeof(PlacarLutaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarCronometro(Guid id, [FromBody] CronometroRequest req)
    {
        var command = new AtualizarCronometroCommand(
            id,
            req.TatameId,
            req.CronometroSegundos,
            req.CronometroRodando,
            req.EmGoldenScore,
            req.SonoMama
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    public record OsaekomiRequest(
        Guid TatameId,
        AcaoOsaekomi Acao,
        CorJudogi? Judogi,
        int Segundos
    );

    /// <summary>
    /// Controla o cronômetro de Osaekomi (Admin ou Mesario).
    /// </summary>
    [HttpPost("{id:guid}/osaekomi")]
    [Authorize(Roles = "Admin,Mesario")]
    [ProducesResponseType(typeof(PlacarLutaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ControlarOsaekomi(Guid id, [FromBody] OsaekomiRequest req)
    {
        var command = new RegistrarOsaekomiCommand(
            id,
            req.TatameId,
            req.Acao,
            req.Judogi,
            req.Segundos
        );

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
