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

    public record SalvarCategoriaRequest(
        string Classe,
        string DivisaoPeso,
        decimal PesoLimiteKg,
        string Sexo,
        int TempoLutaSegundos = 240
    );

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
    /// Cadastra um novo campeonato com categorias oficiais e tatames (Admin ou Secretaria).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Secretaria")]
    [ProducesResponseType(typeof(CampeonatoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarCampeonatoCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
    }

    /// <summary>
    /// Altera o status do campeonato (EM_ANDAMENTO, FINALIZADO) (Admin ou Secretaria).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,Secretaria")]
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
    /// Cadastra uma nova categoria de peso para o campeonato (somente Admin).
    /// </summary>
    [HttpPost("{id:guid}/categorias")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarCategoria(Guid id, [FromBody] SalvarCategoriaRequest req)
    {
        try
        {
            var result = await _mediator.Send(new CriarCategoriaCommand(
                id,
                req.Classe,
                req.DivisaoPeso,
                req.PesoLimiteKg,
                req.Sexo,
                req.TempoLutaSegundos
            ));
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza os dados de uma categoria de peso existente (somente Admin).
    /// </summary>
    [HttpPut("{id:guid}/categorias/{categoriaId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarCategoria(Guid id, Guid categoriaId, [FromBody] SalvarCategoriaRequest req)
    {
        var result = await _mediator.Send(new AtualizarCategoriaCommand(
            categoriaId,
            req.Classe,
            req.DivisaoPeso,
            req.PesoLimiteKg,
            req.Sexo,
            req.TempoLutaSegundos
        ));
        if (result == null) return NotFound(new { mensagem = "Categoria não encontrada." });
        return Ok(result);
    }

    /// <summary>
    /// Remove uma categoria de peso do campeonato (somente Admin).
    /// </summary>
    [HttpDelete("{id:guid}/categorias/{categoriaId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverCategoria(Guid id, Guid categoriaId)
    {
        var removido = await _mediator.Send(new RemoverCategoriaCommand(categoriaId));
        if (!removido) return NotFound(new { mensagem = "Categoria não encontrada." });
        return Ok(new { mensagem = "Categoria removida com sucesso." });
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
