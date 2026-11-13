using Judoo.Application.Commands;
using Judoo.Application.DTOs;
using Judoo.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judoo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TatamesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TatamesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record DefinirLutaAtualRequest(Guid? LutaId, bool IniciarLuta = false);

    /// <summary>
    /// Lista os tatames ativos de um campeonato e suas lutas em andamento.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<TatameStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterTatames([FromQuery] Guid campeonatoId)
    {
        var result = await _mediator.Send(new ObterTatamesQuery(campeonatoId));
        return Ok(result);
    }

    /// <summary>
    /// Define ou inicia a luta atual em um tatame específico (Admin ou Mesario).
    /// </summary>
    [HttpPut("{tatameId:guid}/luta-atual")]
    [Authorize(Roles = "Admin,Mesario")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinirLutaAtual(Guid tatameId, [FromBody] DefinirLutaAtualRequest req)
    {
        var sucesso = await _mediator.Send(new DefinirLutaAtualTatameCommand(tatameId, req.LutaId, req.IniciarLuta));
        if (!sucesso) return NotFound(new { mensagem = "Tatame ou luta não encontrados." });
        return Ok(new { mensagem = "Luta atual do tatame atualizada com sucesso." });
    }
}
