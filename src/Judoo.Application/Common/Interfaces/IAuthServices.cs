using Judoo.Domain.Entities;

namespace Judoo.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verify(string senha, string senhaHash);
}

public interface IJwtTokenService
{
    string GerarToken(Usuario usuario);
}
