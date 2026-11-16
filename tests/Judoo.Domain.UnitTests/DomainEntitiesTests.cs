using Judoo.Domain.Entities;
using Judoo.Domain.Enums;

namespace Judoo.Domain.UnitTests;

public class DomainEntitiesTests
{
    [Fact]
    public void Usuario_DeveInicializarComValoresPadraoEPermitirAtribuicao()
    {
        var id = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = id,
            Nome = "Sensei Tanaka",
            Email = "tanaka@judoo.com",
            SenhaHash = "hash-seguro",
            Role = PerfilUsuario.Admin,
            Ativo = true
        };

        Assert.Equal(id, usuario.Id);
        Assert.Equal("Sensei Tanaka", usuario.Nome);
        Assert.Equal("tanaka@judoo.com", usuario.Email);
        Assert.Equal("hash-seguro", usuario.SenhaHash);
        Assert.Equal(PerfilUsuario.Admin, usuario.Role);
        Assert.True(usuario.Ativo);
        Assert.True(usuario.CriadoEm <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Campeonato_Tatame_Categoria_DevemManterRelacionamentosEValoresPadrao()
    {
        var campId = Guid.NewGuid();
        var tatameId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var mesario = new Usuario { Id = Guid.NewGuid(), Nome = "Mesario 1" };

        var campeonato = new Campeonato
        {
            Id = campId,
            Nome = "Copa Estadual",
            Local = "Ginásio Central",
            DataInicio = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Utc),
            DataFim = new DateTime(2026, 11, 13, 18, 0, 0, DateTimeKind.Utc)
        };

        var tatame = new Tatame
        {
            Id = tatameId,
            CampeonatoId = campId,
            Numero = 1,
            Nome = "Tatame 1",
            MesarioId = mesario.Id,
            Mesario = mesario,
            Campeonato = campeonato
        };

        var categoria = new Categoria
        {
            Id = categoriaId,
            CampeonatoId = campId,
            Classe = "Sênior",
            DivisaoPeso = "-73kg",
            PesoLimiteKg = 73.0m,
            Sexo = "MASCULINO",
            Campeonato = campeonato
        };

        campeonato.Tatames.Add(tatame);
        campeonato.Categorias.Add(categoria);

        Assert.Equal("RASCUNHO", campeonato.Status);
        Assert.Single(campeonato.Tatames);
        Assert.Single(campeonato.Categorias);
        Assert.Equal(StatusTatame.Disponivel, tatame.Status);
        Assert.Equal(mesario.Id, tatame.MesarioId);
        Assert.Same(mesario, tatame.Mesario);
        Assert.Same(campeonato, tatame.Campeonato);
        Assert.Equal(240, categoria.TempoLutaSegundos);
        Assert.Equal(10, categoria.TempoOsaekomiWazaari);
        Assert.Equal(20, categoria.TempoOsaekomiIppon);
        Assert.Equal(73.0m, categoria.PesoLimiteKg);
    }

    [Fact]
    public void Academia_Atleta_Pesagem_DevemManterPropriedadesEValoresPadrao()
    {
        var academia = new Academia
        {
            Id = Guid.NewGuid(),
            Nome = "Kodokan Brasil",
            Sigla = "KDK",
            Cidade = "São Paulo",
            Estado = "SP"
        };

        var atleta = new Atleta
        {
            Id = Guid.NewGuid(),
            AcademiaId = academia.Id,
            Nome = "Rafael Silva",
            RegistroFederacao = "CBJ-999",
            DataNascimento = new DateTime(1998, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            Sexo = "MASCULINO",
            FotoUrl = "https://cdn.judoo/foto.png",
            Academia = academia,
            NomeAcademia = academia.Nome,
            SiglaAcademia = academia.Sigla
        };

        var pesagem = new Pesagem
        {
            Id = Guid.NewGuid(),
            CampeonatoId = Guid.NewGuid(),
            AtletaId = atleta.Id,
            CategoriaId = Guid.NewGuid(),
            PesoAferidoKg = 72.4m,
            AferidoPorUsuarioId = Guid.NewGuid(),
            Observacoes = "Dentro do peso",
            Atleta = atleta
        };

        Assert.True(atleta.Ativo);
        Assert.Same(academia, atleta.Academia);
        Assert.Equal("KDK", atleta.SiglaAcademia);
        Assert.Equal("PENDENTE", pesagem.Status);
        Assert.Equal(72.4m, pesagem.PesoAferidoKg);
        Assert.Equal("Dentro do peso", pesagem.Observacoes);
        Assert.Same(atleta, pesagem.Atleta);
    }

    [Fact]
    public void Chave_Luta_EventoPontuacao_DevemManterRelacionamentosEValoresPadrao()
    {
        var chave = new Chave
        {
            Id = Guid.NewGuid(),
            CampeonatoId = Guid.NewGuid(),
            CategoriaId = Guid.NewGuid()
        };

        var luta = new Luta
        {
            Id = Guid.NewGuid(),
            ChaveId = chave.Id,
            TatameId = Guid.NewGuid(),
            AtletaBrancoId = Guid.NewGuid(),
            AtletaAzulId = Guid.NewGuid(),
            VencedorId = null,
            OrdemNoTatame = 1,
            Chave = chave
        };

        var evento = new EventoPontuacao
        {
            Id = Guid.NewGuid(),
            LutaId = luta.Id,
            AtletaId = luta.AtletaBrancoId,
            CorJudogi = CorJudogi.Branco,
            Tipo = TipoPontuacao.Ippon,
            TempoLutaSegundos = 145,
            Descricao = "Seoi-nage",
            RegistradoPorUsuarioId = Guid.NewGuid(),
            Luta = luta
        };

        chave.Lutas.Add(luta);
        luta.Eventos.Add(evento);

        Assert.Equal("ELIMINATORIA_SIMPLES", chave.Tipo);
        Assert.Equal(1, chave.TotalFases);
        Assert.False(chave.Gerada);
        Assert.Single(chave.Lutas);

        Assert.Equal(StatusLuta.Agendada, luta.Status);
        Assert.Equal(1, luta.Fase);
        Assert.Equal(1, luta.OrdemNaChave);
        Assert.Equal(0, luta.TempoLutaSegundos);
        Assert.False(luta.EmGoldenScore);
        Assert.Single(luta.Eventos);
        Assert.Equal("Seoi-nage", evento.Descricao);
        Assert.Equal(TipoPontuacao.Ippon, evento.Tipo);
        Assert.Equal(CorJudogi.Branco, evento.CorJudogi);
    }

    [Fact]
    public void DomainEnums_DevemConterTodosOsValoresEsperados()
    {
        Assert.Equal(4, Enum.GetValues<PerfilUsuario>().Length);
        Assert.Equal(2, Enum.GetValues<CorJudogi>().Length);
        Assert.Equal(7, Enum.GetValues<TipoPontuacao>().Length); // Yuko, WazaAri, Ippon, Shido, CancelarYuko, CancelarWazaAri, CancelarShido
        Assert.Equal(6, Enum.GetValues<StatusLuta>().Length);
        Assert.Equal(8, Enum.GetValues<TipoVitoria>().Length); // Ippon, WazaAriAwaseteIppon, WazaAri, Yuko, HansokuMake, Decisao, WO, Desistencia
        Assert.Equal(4, Enum.GetValues<StatusTatame>().Length);
    }
}
