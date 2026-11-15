using Judoo.Application.Common.Interfaces;
using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Domain.Repositories;
using MediatR;

namespace Judoo.Application.Commands;

public record LoginCommand(string Email, string Senha) : IRequest<UsuarioAuthDto?>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, UsuarioAuthDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUsuarioRepository usuarioRepo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _usuarioRepo = usuarioRepo;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<UsuarioAuthDto?> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
            return null;

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _usuarioRepo.ObterPorEmailAsync(email, cancellationToken);
        if (usuario == null || !usuario.Ativo)
            return null;

        if (!_passwordHasher.Verify(request.Senha, usuario.SenhaHash))
            return null;

        var token = _jwtTokenService.GerarToken(usuario);

        return new UsuarioAuthDto(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Role.ToString().ToUpper(),
            token
        );
    }
}

public record RegistrarUsuarioCommand(
    string Nome,
    string Email,
    string Senha,
    PerfilUsuario Role
) : IRequest<UsuarioDto>;

public class RegistrarUsuarioCommandHandler : IRequestHandler<RegistrarUsuarioCommand, UsuarioDto>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IPasswordHasher _passwordHasher;

    public RegistrarUsuarioCommandHandler(IUsuarioRepository usuarioRepo, IPasswordHasher passwordHasher)
    {
        _usuarioRepo = usuarioRepo;
        _passwordHasher = passwordHasher;
    }

    public async Task<UsuarioDto> Handle(RegistrarUsuarioCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new ArgumentException("O nome do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("O e-mail do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 4)
            throw new ArgumentException("A senha deve conter ao menos 4 caracteres.");

        if (!Enum.IsDefined(typeof(PerfilUsuario), request.Role))
            throw new ArgumentException("Perfil de usuário inválido.");

        var emailNormalizado = request.Email.Trim().ToLowerInvariant();
        var existente = await _usuarioRepo.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (existente != null)
            throw new InvalidOperationException("Já existe um usuário cadastrado com este e-mail.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Email = emailNormalizado,
            SenhaHash = _passwordHasher.Hash(request.Senha),
            Role = request.Role,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };

        await _usuarioRepo.CriarAsync(usuario, cancellationToken);
        return UsuarioDto.FromDomain(usuario);
    }
}

public record AlterarStatusUsuarioCommand(Guid Id, bool Ativo) : IRequest<bool>;

public class AlterarStatusUsuarioCommandHandler : IRequestHandler<AlterarStatusUsuarioCommand, bool>
{
    private readonly IUsuarioRepository _usuarioRepo;

    public AlterarStatusUsuarioCommandHandler(IUsuarioRepository usuarioRepo)
    {
        _usuarioRepo = usuarioRepo;
    }

    public async Task<bool> Handle(AlterarStatusUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.ObterPorIdAsync(request.Id, cancellationToken);
        if (usuario == null) return false;

        await _usuarioRepo.AtualizarStatusAsync(request.Id, request.Ativo, cancellationToken);
        return true;
    }
}

public record AtualizarUsuarioCommand(
    Guid Id,
    string Nome,
    string Email,
    PerfilUsuario Role,
    string? NovaSenha = null
) : IRequest<UsuarioDto?>;

public class AtualizarUsuarioCommandHandler : IRequestHandler<AtualizarUsuarioCommand, UsuarioDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IPasswordHasher _passwordHasher;

    public AtualizarUsuarioCommandHandler(IUsuarioRepository usuarioRepo, IPasswordHasher passwordHasher)
    {
        _usuarioRepo = usuarioRepo;
        _passwordHasher = passwordHasher;
    }

    public async Task<UsuarioDto?> Handle(AtualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.ObterPorIdAsync(request.Id, cancellationToken);
        if (usuario == null) return null;

        if (!string.IsNullOrWhiteSpace(request.Nome))
            usuario.Nome = request.Nome.Trim();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailNormalizado = request.Email.Trim().ToLowerInvariant();
            var existente = await _usuarioRepo.ObterPorEmailAsync(emailNormalizado, cancellationToken);
            if (existente != null && existente.Id != usuario.Id)
                throw new InvalidOperationException("Já existe outro usuário cadastrado com este e-mail.");
            usuario.Email = emailNormalizado;
        }

        if (Enum.IsDefined(typeof(PerfilUsuario), request.Role))
            usuario.Role = request.Role;

        if (!string.IsNullOrWhiteSpace(request.NovaSenha))
        {
            if (request.NovaSenha.Length < 4)
                throw new ArgumentException("A nova senha deve conter ao menos 4 caracteres.");
            usuario.SenhaHash = _passwordHasher.Hash(request.NovaSenha);
        }

        await _usuarioRepo.AtualizarAsync(usuario, cancellationToken);
        return UsuarioDto.FromDomain(usuario);
    }
}

public record RemoverUsuarioCommand(Guid Id) : IRequest<bool>;

public class RemoverUsuarioCommandHandler : IRequestHandler<RemoverUsuarioCommand, bool>
{
    private readonly IUsuarioRepository _usuarioRepo;

    public RemoverUsuarioCommandHandler(IUsuarioRepository usuarioRepo)
    {
        _usuarioRepo = usuarioRepo;
    }

    public async Task<bool> Handle(RemoverUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.ObterPorIdAsync(request.Id, cancellationToken);
        if (usuario == null) return false;

        await _usuarioRepo.RemoverAsync(request.Id, cancellationToken);
        return true;
    }
}
