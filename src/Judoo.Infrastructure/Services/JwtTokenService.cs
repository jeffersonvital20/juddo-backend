using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Judoo.Application.Common.Interfaces;
using Judoo.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Judoo.Infrastructure.Services;

public static class JwtDefaults
{
    public const string DefaultKey = "JudooSuperSecretKeyForJwtAuthentication2026!";
    public const string DefaultIssuer = "Judoo.Api";
    public const string DefaultAudience = "Judoo.Clients";
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GerarToken(Usuario usuario)
    {
        var secret = _configuration["Jwt:Key"] ?? JwtDefaults.DefaultKey;
        var issuer = _configuration["Jwt:Issuer"] ?? JwtDefaults.DefaultIssuer;
        var audience = _configuration["Jwt:Audience"] ?? JwtDefaults.DefaultAudience;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
