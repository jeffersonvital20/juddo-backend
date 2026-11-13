using Judoo.Application.Commands;
using Judoo.Application.DTOs;
using Judoo.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judoo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AtletasController : ControllerBase
{
    private readonly IMediator _mediator;

    public AtletasController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lista os atletas cadastrados, opcionalmente filtrados por campeonato.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<AtletaCadastroDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterAtletas([FromQuery] Guid? campeonatoId)
    {
        var result = await _mediator.Send(new ObterAtletasQuery(campeonatoId));
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo lutador no sistema e opcionalmente o inscreve em um campeonato (Admin ou Secretaria).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Secretaria")]
    [ProducesResponseType(typeof(AtletaCadastroDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CadastrarAtleta([FromBody] CadastrarAtletaCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/atletas/{result.Id}", result);
    }

    /// <summary>
    /// Inscreve um atleta em um campeonato e categoria (Admin ou Secretaria).
    /// </summary>
    [HttpPost("inscrever")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> InscreverEmCampeonato([FromBody] InscreverAtletaCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { mensagem = "Atleta inscrito com sucesso no campeonato." });
    }

    /// <summary>
    /// Lista academias e clubes de Judô cadastrados.
    /// </summary>
    [HttpGet("academias")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<AcademiaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterAcademias()
    {
        var result = await _mediator.Send(new ObterAcademiasQuery());
        return Ok(result);
    }

    /// <summary>
    /// Cadastra uma nova academia / agremiação (Admin ou Secretaria).
    /// </summary>
    [HttpPost("academias")]
    [Authorize(Roles = "Admin,Secretaria")]
    [ProducesResponseType(typeof(AcademiaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CadastrarAcademia([FromBody] CadastrarAcademiaCommand command)
    {
        var result = await _mediator.Send(command);
        return Created($"/api/atletas/academias/{result.Id}", result);
    }
}
