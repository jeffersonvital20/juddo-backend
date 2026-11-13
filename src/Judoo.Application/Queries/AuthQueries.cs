using Judoo.Application.DTOs;
using Judoo.Domain.Repositories;
using MediatR;

namespace Judoo.Application.Queries;

public record ObterUsuariosQuery() : IRequest<IEnumerable<UsuarioDto>>;

public class ObterUsuariosQueryHandler : IRequestHandler<ObterUsuariosQuery, IEnumerable<UsuarioDto>>
{
    private readonly IUsuarioRepository _usuarioRepo;

    public ObterUsuariosQueryHandler(IUsuarioRepository usuarioRepo)
    {
        _usuarioRepo = usuarioRepo;
    }

    public async Task<IEnumerable<UsuarioDto>> Handle(ObterUsuariosQuery request, CancellationToken cancellationToken)
    {
        var usuarios = await _usuarioRepo.ObterTodosAsync(cancellationToken);
        return usuarios.Select(UsuarioDto.FromDomain);
    }
}

public record ObterUsuarioPorIdQuery(Guid Id) : IRequest<UsuarioDto?>;

public class ObterUsuarioPorIdQueryHandler : IRequestHandler<ObterUsuarioPorIdQuery, UsuarioDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;

    public ObterUsuarioPorIdQueryHandler(IUsuarioRepository usuarioRepo)
    {
        _usuarioRepo = usuarioRepo;
    }

    public async Task<UsuarioDto?> Handle(ObterUsuarioPorIdQuery request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.ObterPorIdAsync(request.Id, cancellationToken);
        return usuario != null ? UsuarioDto.FromDomain(usuario) : null;
    }
}
