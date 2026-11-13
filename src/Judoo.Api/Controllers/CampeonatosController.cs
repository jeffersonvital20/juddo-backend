using Judoo.Application.Commands;
using Judoo.Application.DTOs;
using Judoo.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judoo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampeonatosController : ControllerBase
{
    private readonly IMediator _mediator;

    public CampeonatosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record GerarGradeRequest(Guid CategoriaId, Guid? TatameId = null);

    /// <summary>
    /// Lista todos os campeonatos cadastrados no sistema.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CampeonatoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterTodos()
    {
        var result = await _mediator.Send(new ObterCampeonatosQuery());
        return Ok(result);
    }

    /// <summary>
    /// Obtém detalhes de um campeonato por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CampeonatoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _mediator.Send(new ObterCampeonatoPorIdQuery(id));
        if (result == null) return NotFound(new { mensagem = "Campeonato não encontrado." });
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo campeonato com categorias oficiais e tatames (somente Admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CampeonatoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarCampeonatoCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
    }

    /// <summary>
    /// Altera o status do campeonato (EM_ANDAMENTO, FINALIZADO) (somente Admin).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] string status)
    {
        await _mediator.Send(new AtualizarStatusCampeonatoCommand(id, status));
        return Ok(new { mensagem = $"Status do campeonato alterado para {status}." });
    }

    /// <summary>
    /// Lista as categorias de peso vinculadas ao campeonato.
    /// </summary>
    [HttpGet("{id:guid}/categorias")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterCategorias(Guid id)
    {
        var result = await _mediator.Send(new ObterCategoriasCampeonatoQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Lista as chaves do campeonato por categoria.
    /// </summary>
    [HttpGet("{id:guid}/chaves")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ChaveDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterChaves(Guid id)
    {
        var result = await _mediator.Send(new ObterChavesCampeonatoQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Monta automaticamente a grade de lutas de uma categoria com base nos lutadores inscritos (Admin ou Secretaria).
    /// </summary>
    [HttpPost("{id:guid}/chaves/gerar")]
    [Authorize(Roles = "Admin,Secretaria")]
    [ProducesResponseType(typeof(IEnumerable<LutaDetalhesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GerarGrade(Guid id, [FromBody] GerarGradeRequest req)
    {
        try
        {
            var result = await _mediator.Send(new GerarGradeLutasCommand(id, req.CategoriaId, req.TatameId));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }
}
