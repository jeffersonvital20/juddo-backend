using Judoo.Domain.Entities;
using Judoo.Domain.Enums;

namespace Judoo.Application.DTOs;

public record CampeonatoDto(
    Guid Id,
    string Nome,
    string Local,
    DateTime DataInicio,
    DateTime DataFim,
    string Status,
    DateTime CriadoEm
)
{
    public static CampeonatoDto FromDomain(Campeonato c) => new(
        c.Id,
        c.Nome,
        c.Local,
        c.DataInicio,
        c.DataFim,
        c.Status,
        c.CriadoEm
    );
}

public record CategoriaDto(
    Guid Id,
    Guid CampeonatoId,
    string Classe,
    string DivisaoPeso,
    decimal PesoLimiteKg,
    string Sexo,
    int TempoLutaSegundos
);

public record AcademiaDto(
    Guid Id,
    string Nome,
    string Sigla,
    string? Cidade,
    string? Estado
);

public record AtletaCadastroDto(
    Guid Id,
    Guid AcademiaId,
    string Nome,
    string? RegistroFederacao,
    DateTime DataNascimento,
    string Sexo,
    string? FotoUrl,
    string? NomeAcademia,
    string? SiglaAcademia
);

public record UsuarioAuthDto(
    Guid Id,
    string Nome,
    string Email,
    string Role,
    string Token
);

public record UsuarioDto(
    Guid Id,
    string Nome,
    string Email,
    string Role,
    bool Ativo,
    DateTime CriadoEm
)
{
    public static UsuarioDto FromDomain(Usuario u) => new(
        u.Id,
        u.Nome,
        u.Email,
        u.Role.ToString().ToUpper(),
        u.Ativo,
        u.CriadoEm
    );
}

public record ChaveDto(
    Guid Id,
    Guid CampeonatoId,
    Guid CategoriaId,
    string Tipo,
    int TotalFases,
    bool Gerada,
    DateTime CriadaEm
)
{
    public static ChaveDto FromDomain(Chave c) => new(
        c.Id,
        c.CampeonatoId,
        c.CategoriaId,
        c.Tipo,
        c.TotalFases,
        c.Gerada,
        c.CriadaEm
    );
}
