using Judoo.Application.Commands;
using Judoo.Application.DTOs;
using Judoo.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Judoo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record LoginRequest(string Email, string Senha);
    public record RegistrarPublicoRequest(string Nome, string Email, string Senha);

    /// <summary>
    /// Autentica o usuário cadastrado e retorna o JWT com as permissões do perfil.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UsuarioAuthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _mediator.Send(new LoginCommand(req.Email, req.Senha));
        if (result == null) return Unauthorized(new { mensagem = "Credenciais inválidas ou usuário inativo." });
        return Ok(result);
    }

    /// <summary>
    /// Auto-cadastro público de usuário com perfil Publico.
    /// Para cadastrar Admin, Secretaria ou Mesario, utilize POST /api/usuarios autenticado como Admin.
    /// </summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarPublico([FromBody] RegistrarPublicoRequest req)
    {
        try
        {
            var usuario = await _mediator.Send(new RegistrarUsuarioCommand(req.Nome, req.Email, req.Senha, PerfilUsuario.Publico));
            return Created($"/api/usuarios/{usuario.Id}", usuario);
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
}
