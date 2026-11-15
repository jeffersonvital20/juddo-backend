using Judoo.Application.Commands;
using Judoo.Application.DTOs;
using Judoo.Application.Queries;
using Judoo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judoo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsuariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsuariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record CadastrarUsuarioRequest(
        string Nome,
        string Email,
        string Senha,
        PerfilUsuario Role
    );

    public record AlterarStatusUsuarioRequest(bool Ativo);

    public record AtualizarUsuarioRequest(
        string Nome,
        string Email,
        PerfilUsuario Role,
        string? NovaSenha = null
    );

    /// <summary>
    /// Lista todos os usuários cadastrados no sistema e seus respectivos perfis.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterTodos()
    {
        var result = await _mediator.Send(new ObterUsuariosQuery());
        return Ok(result);
    }

    /// <summary>
    /// Obtém os detalhes de um usuário por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _mediator.Send(new ObterUsuarioPorIdQuery(id));
        if (result == null) return NotFound(new { mensagem = "Usuário não encontrado." });
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo usuário no sistema de acordo com o perfil escolhido (Admin, Secretaria, Mesario, Publico).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarUsuarioRequest req)
    {
        try
        {
            var result = await _mediator.Send(new RegistrarUsuarioCommand(req.Nome, req.Email, req.Senha, req.Role));
            return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Ativa ou desativa um usuário cadastrado.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusUsuarioRequest req)
    {
        var atualizado = await _mediator.Send(new AlterarStatusUsuarioCommand(id, req.Ativo));
        if (!atualizado) return NotFound(new { mensagem = "Usuário não encontrado." });
        return Ok(new { mensagem = req.Ativo ? "Usuário ativado." : "Usuário desativado." });
    }

    /// <summary>
    /// Atualiza nome, e-mail, perfil de acesso e (opcionalmente) senha de um usuário.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarUsuarioRequest req)
    {
        try
        {
            var result = await _mediator.Send(new AtualizarUsuarioCommand(id, req.Nome, req.Email, req.Role, req.NovaSenha));
            if (result == null) return NotFound(new { mensagem = "Usuário não encontrado." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Remove um usuário do sistema.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid id)
    {
        var removido = await _mediator.Send(new RemoverUsuarioCommand(id));
        if (!removido) return NotFound(new { mensagem = "Usuário não encontrado." });
        return Ok(new { mensagem = "Usuário removido com sucesso." });
    }
}
