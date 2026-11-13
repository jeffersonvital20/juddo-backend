using System.Security.Claims;
using Judoo.Application.Commands.RegistrarPonto;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.Infrastructure.Data;
using Judoo.Infrastructure.Hubs;
using Judoo.Infrastructure.Repositories;
using Judoo.Infrastructure.Services;
using Judoo.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Judoo.IntegrationTests;

public class InfrastructureAndRepositoriesIntegrationTests
{
    [Fact]
    public void RegistrarPontoCommandValidator_DeveValidarCamposCorretamente()
    {
        var validator = new RegistrarPontoCommandValidator();

        var invalido = validator.Validate(new RegistrarPontoCommand(Guid.Empty, Guid.Empty, CorJudogi.Branco, TipoPontuacao.Ippon, -5));
        Assert.False(invalido.IsValid);
        Assert.Contains(invalido.Errors, e => e.PropertyName == nameof(RegistrarPontoCommand.LutaId));
        Assert.Contains(invalido.Errors, e => e.PropertyName == nameof(RegistrarPontoCommand.TatameId));
        Assert.Contains(invalido.Errors, e => e.PropertyName == nameof(RegistrarPontoCommand.TempoLutaSegundos));

        var valido = validator.Validate(new RegistrarPontoCommand(Guid.NewGuid(), Guid.NewGuid(), CorJudogi.Branco, TipoPontuacao.Ippon, 120));
        Assert.True(valido.IsValid);
    }

    [Fact]
    public async Task JudooDbContext_EJodooDbContextFactory_EDbConnectionFactory_DevemConstruirModeloECriarConexao()
    {
        var factory = new JudooDbContextFactory();
        using var designContext = factory.CreateDbContext(Array.Empty<string>());
        Assert.NotNull(designContext.Model.FindEntityType(typeof(Campeonato)));

        var options = new DbContextOptionsBuilder<JudooDbContext>()
            .UseInMemoryDatabase($"EfModelTest_{Guid.NewGuid()}")
            .Options;

        await using var db = new JudooDbContext(options);

        var camp = new Campeonato { Id = Guid.NewGuid(), Nome = "Copa EF", Local = "SP", DataInicio = DateTime.UtcNow, DataFim = DateTime.UtcNow.AddDays(1) };
        var acad = new Academia { Id = Guid.NewGuid(), Nome = "Acad EF", Sigla = "AEF", Cidade = "SP", Estado = "SP" };
        var usuario = new Usuario { Id = Guid.NewGuid(), Nome = "Admin EF", Email = "ef@judoo.com", SenhaHash = "hash", Role = PerfilUsuario.Admin };
        var cat = new Categoria { Id = Guid.NewGuid(), CampeonatoId = camp.Id, Classe = "Sênior", DivisaoPeso = "-73kg", PesoLimiteKg = 73m, Sexo = "MASCULINO" };
        var tatame = new Tatame { Id = Guid.NewGuid(), CampeonatoId = camp.Id, Numero = 1, Nome = "Tatame EF" };
        var atleta = new Atleta { Id = Guid.NewGuid(), AcademiaId = acad.Id, Nome = "Atleta EF", DataNascimento = DateTime.UtcNow.AddYears(-20), Sexo = "MASCULINO" };
        var pesagem = new Pesagem { Id = Guid.NewGuid(), CampeonatoId = camp.Id, AtletaId = atleta.Id, CategoriaId = cat.Id, PesoAferidoKg = 72m };
        var chave = new Chave { Id = Guid.NewGuid(), CampeonatoId = camp.Id, CategoriaId = cat.Id };
        var luta = new Luta { Id = Guid.NewGuid(), ChaveId = chave.Id, TatameId = tatame.Id, AtletaBrancoId = atleta.Id };
        var placar = new PlacarLuta(luta.Id, 240);
        var evento = new EventoPontuacao { Id = Guid.NewGuid(), LutaId = luta.Id, CorJudogi = CorJudogi.Branco, Tipo = TipoPontuacao.Ippon, TempoLutaSegundos = 120 };

        db.Campeonatos.Add(camp);
        db.Academias.Add(acad);
        db.Usuarios.Add(usuario);
        db.Categorias.Add(cat);
        db.Tatames.Add(tatame);
        db.Atletas.Add(atleta);
        db.Pesagens.Add(pesagem);
        db.Chaves.Add(chave);
        db.Lutas.Add(luta);
        db.PlacarLutas.Add(placar);
        db.EventosLuta.Add(evento);

        await db.SaveChangesAsync();

        Assert.Equal(1, await db.Campeonatos.CountAsync());
        Assert.Equal(1, await db.Usuarios.CountAsync());
        Assert.Equal(1, await db.Tatames.CountAsync());
        Assert.Equal(1, await db.Academias.CountAsync());
        Assert.Equal(1, await db.Categorias.CountAsync());
        Assert.Equal(1, await db.Atletas.CountAsync());
        Assert.Equal(1, await db.Pesagens.CountAsync());
        Assert.Equal(1, await db.Chaves.CountAsync());
        Assert.Equal(1, await db.Lutas.CountAsync());
        Assert.Equal(1, await db.PlacarLutas.CountAsync());
        Assert.Equal(1, await db.EventosLuta.CountAsync());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=judoo_db;Username=judoo;Password=secret"
            })
            .Build();
        var connFactory = new DbConnectionFactory(config);
        using var npgsqlConn = connFactory.CreateConnection();
        Assert.Contains("judoo_db", npgsqlConn.ConnectionString);
    }

    [Fact]
    public async Task DapperRepositories_DevemGerarComandosSqlEParametrosCorretos()
    {
        var campId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var atletaId = Guid.NewGuid();
        var acadId = Guid.NewGuid();
        var tatameId = Guid.NewGuid();
        var chaveId = Guid.NewGuid();
        var lutaId = Guid.NewGuid();

        bool retornarChaveExistente = true;
        bool retornarPlacarComOsaekomi = true;

        var connFactory = new RecordingDbConnectionFactory(sql =>
        {
            static KeyValuePair<string, object?> Col(string k, object? v) => new(k, v);

            if (sql.Contains("FROM chaves"))
            {
                if (!retornarChaveExistente) return Array.Empty<IReadOnlyList<KeyValuePair<string, object?>>>();
                return new[]
                {
                    new[]
                    {
                        Col("id", chaveId),
                        Col("CampeonatoId", campId),
                        Col("CategoriaId", catId),
                        Col("tipo", "ELIMINATORIA_SIMPLES"),
                        Col("TotalFases", 2),
                        Col("gerada", true),
                        Col("CriadaEm", DateTime.UtcNow)
                    }
                };
            }

            if (sql.Contains("FROM placar_lutas"))
            {
                if (!retornarPlacarComOsaekomi) return Array.Empty<IReadOnlyList<KeyValuePair<string, object?>>>();
                return new[]
                {
                    new[]
                    {
                        Col("LutaId", lutaId),
                        Col("ipponbranco", 1),
                        Col("wazaaribranco", 1),
                        Col("shidobranco", 0),
                        Col("ipponazul", 0),
                        Col("wazaariazul", 1),
                        Col("shidoazul", 2),
                        Col("osaekomiativo", true),
                        Col("osaekomicorstring", "AZUL"),
                        Col("osaekomitemposegundos", 12),
                        Col("cronometrosegundos", 150),
                        Col("emgoldenscore", true),
                        Col("cronometrorodando", false),
                        Col("sonomama", true),
                        Col("ultimaatualizacao", DateTime.UtcNow)
                    }
                };
            }

            if (sql.Contains("FROM lutas l"))
            {
                return new[]
                {
                    new[]
                    {
                        Col("id", lutaId),
                        Col("ChaveId", chaveId),
                        Col("TatameId", tatameId),
                        Col("AtletaBrancoId", atletaId),
                        Col("AtletaAzulId", atletaId),
                        Col("VencedorId", null),
                        Col("fase", 1),
                        Col("OrdemNaChave", 1),
                        Col("OrdemNoTatame", 1),
                        Col("status", StatusLuta.Agendada),
                        Col("TipoVitoria", null),
                        Col("EmGoldenScore", false),
                        Col("TempoLutaSegundos", 240),
                        Col("HoraInicio", null),
                        Col("HoraFim", null),
                        Col("CriadaEm", DateTime.UtcNow),
                        Col("id", atletaId),
                        Col("nome", "Judoca Branco"),
                        Col("RegistroFederacao", "CBJ-1"),
                        Col("NomeAcademia", "Kodokan"),
                        Col("SiglaAcademia", "KDK"),
                        Col("id", atletaId),
                        Col("nome", "Judoca Azul"),
                        Col("RegistroFederacao", "CBJ-2"),
                        Col("NomeAcademia", "Sogipa"),
                        Col("SiglaAcademia", "SOG")
                    }
                };
            }

            return Array.Empty<IReadOnlyList<KeyValuePair<string, object?>>>();
        });

        // CampeonatoRepository
        var campRepo = new CampeonatoRepository(connFactory);
        await campRepo.CriarAsync(new Campeonato { Id = campId, Nome = "Copa Dapper", Local = "RJ", DataInicio = DateTime.UtcNow, DataFim = DateTime.UtcNow });
        await campRepo.AtualizarStatusAsync(campId, "em_andamento");
        await campRepo.CriarCategoriaAsync(new Categoria { Id = catId, CampeonatoId = campId, Classe = "Sênior", DivisaoPeso = "-81kg", PesoLimiteKg = 81m, Sexo = "masculino" });
        await campRepo.ObterTodosAsync();
        await campRepo.ObterPorIdAsync(campId);
        await campRepo.ObterCategoriasAsync(campId);
        await campRepo.ObterChavesAsync(campId);
        await campRepo.AtualizarChaveGeradaAsync(chaveId, 2, true);
        await campRepo.ObterPesagensPorCategoriaAsync(campId, catId);
        var chaveExistente = await campRepo.ObterOuCriarChaveAsync(campId, catId);
        Assert.NotNull(chaveExistente);
        retornarChaveExistente = false;
        var chaveCriada = await campRepo.ObterOuCriarChaveAsync(campId, catId);
        Assert.NotNull(chaveCriada);

        // UsuarioRepository + PasswordHasher + JwtTokenService
        var hasher = new PasswordHasher();
        var senhaHash = hasher.Hash("SenhaForte!123");
        Assert.True(hasher.Verify("SenhaForte!123", senhaHash));
        Assert.False(hasher.Verify("Errada", senhaHash));
        Assert.False(hasher.Verify("x", "formato-invalido"));
        Assert.False(hasher.Verify("x", "pbkdf2$1000$@@@$@@@"));

        var jwtConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtDefaults.DefaultKey,
                ["Jwt:Issuer"] = JwtDefaults.DefaultIssuer,
                ["Jwt:Audience"] = JwtDefaults.DefaultAudience
            })
            .Build();
        var jwtService = new JwtTokenService(jwtConfig);
        var userEntity = new Usuario { Id = Guid.NewGuid(), Nome = "Admin Test", Email = "u@judoo.com", SenhaHash = senhaHash, Role = PerfilUsuario.Admin };
        var jwt = jwtService.GerarToken(userEntity);
        Assert.Contains(".", jwt);

        var usuarioRepo = new UsuarioRepository(connFactory);
        await usuarioRepo.CriarAsync(userEntity);
        await usuarioRepo.ObterPorIdAsync(userEntity.Id);
        await usuarioRepo.ObterPorEmailAsync(userEntity.Email);
        await usuarioRepo.ObterTodosAsync();
        await usuarioRepo.AtualizarStatusAsync(userEntity.Id, false);

        // AtletaRepository
        var atletaRepo = new AtletaRepository(connFactory);
        await atletaRepo.CriarAcademiaAsync(new Academia { Id = acadId, Nome = "Acad Dapper", Sigla = "AD", Cidade = "BH", Estado = "MG" });
        await atletaRepo.ObterAcademiasAsync();
        await atletaRepo.CriarAsync(new Atleta { Id = atletaId, AcademiaId = acadId, Nome = "Judoca Dapper", DataNascimento = DateTime.UtcNow.AddYears(-20), Sexo = "feminino" });
        await atletaRepo.ObterTodosAsync();
        await atletaRepo.ObterTodosAsync(campId);
        await atletaRepo.ObterPorIdAsync(atletaId);
        await atletaRepo.InscreverEmCampeonatoAsync(new Pesagem { Id = Guid.NewGuid(), CampeonatoId = campId, AtletaId = atletaId, CategoriaId = catId, PesoAferidoKg = 80m, Status = "aprovado" });

        // TatameRepository
        var tatameRepo = new TatameRepository(connFactory);
        await tatameRepo.CriarAsync(new Tatame { Id = tatameId, CampeonatoId = campId, Numero = 1, Nome = "Tatame 1" });
        await tatameRepo.ObterPorIdAsync(tatameId);
        await tatameRepo.ObterTodosPorCampeonatoAsync(campId);
        await tatameRepo.AtualizarLutaAtualAsync(tatameId, lutaId, StatusTatame.EmLuta);

        // PlacarLutaRepository
        var placarRepo = new PlacarLutaRepository(connFactory);
        var placarHidratado = await placarRepo.ObterPorLutaIdAsync(lutaId);
        Assert.NotNull(placarHidratado);
        Assert.Equal(CorJudogi.Azul, placarHidratado!.OsaekomiCor);
        Assert.True(placarHidratado.EmGoldenScore);
        retornarPlacarComOsaekomi = false;
        Assert.Null(await placarRepo.ObterPorLutaIdAsync(lutaId));
        await placarRepo.SalvarOuAtualizarAsync(placarHidratado);

        // LutaRepository
        var lutaRepo = new LutaRepository(connFactory);
        await lutaRepo.CriarLutaAsync(new Luta { Id = lutaId, ChaveId = chaveId, TatameId = tatameId, AtletaBrancoId = atletaId, AtletaAzulId = atletaId });
        await lutaRepo.AtualizarStatusLutaAsync(lutaId, StatusLuta.Finalizada, atletaId, TipoVitoria.Ippon);
        await lutaRepo.AtribuirTatameAsync(lutaId, tatameId, 2);
        await lutaRepo.RegistrarEventoPontuacaoAsync(new EventoPontuacao { Id = Guid.NewGuid(), LutaId = lutaId, AtletaId = atletaId, CorJudogi = CorJudogi.Branco, Tipo = TipoPontuacao.Ippon, TempoLutaSegundos = 100 });
        await lutaRepo.ObterPorIdAsync(lutaId);
        await lutaRepo.ObterLutasPorTatameAsync(tatameId);
        await lutaRepo.ObterLutasPorChaveAsync(chaveId);
        await lutaRepo.ObterTodasAsync(campId);

        Assert.True(connFactory.ExecutedCommands.Count >= 24);
    }

    [Fact]
    public async Task TatameHub_ETatameNotificationService_DevemGerenciarGruposEDispararNotificacoes()
    {
        var hub = new TatameHub(NullLogger<TatameHub>.Instance);
        var groupManager = new FakeGroupManager();
        var callerContext = new FakeHubCallerContext("conn-123");

        hub.Groups = groupManager;
        hub.Context = callerContext;

        var tatameId = Guid.NewGuid();
        await hub.OnConnectedAsync();
        await hub.EntrarNoTatame(tatameId.ToString());
        await hub.EntrarNaArena();
        await hub.SairDoTatame(tatameId.ToString());
        await hub.OnDisconnectedAsync(null);

        Assert.Contains($"conn-123->tatame-{tatameId}", groupManager.Added);
        Assert.Contains("conn-123->arena-geral", groupManager.Added);
        Assert.Contains($"conn-123->tatame-{tatameId}", groupManager.Removed);

        var clientProxy = new FakeTatameClientProxy();
        var hubContext = new FakeHubContext(clientProxy);
        var notificationService = new TatameNotificationService(hubContext, NullLogger<TatameNotificationService>.Instance);

        var placar = new PlacarLuta(Guid.NewGuid(), 240);
        var luta = new Luta { Id = placar.LutaId, TatameId = tatameId };

        await notificationService.NotificarPlacarAtualizadoAsync(tatameId, placar);
        await notificationService.NotificarCronometroAsync(tatameId, 200, true, false);
        await notificationService.NotificarLutaIniciadaAsync(tatameId, luta);
        await notificationService.NotificarLutaFinalizadaAsync(tatameId, luta.Id, CorJudogi.Branco, TipoVitoria.Ippon);

        Assert.Equal(2, clientProxy.PlacarCount);
        Assert.Equal(2, clientProxy.CronometroCount);
        Assert.Equal(2, clientProxy.LutaIniciadaCount);
        Assert.Equal(2, clientProxy.LutaFinalizadaCount);
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public List<string> Added { get; } = new();
        public List<string> Removed { get; } = new();

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Added.Add($"{connectionId}->{groupName}");
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Removed.Add($"{connectionId}->{groupName}");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        public FakeHubCallerContext(string connectionId) => ConnectionId = connectionId;
        public override string ConnectionId { get; }
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => null;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    private sealed class FakeTatameClientProxy : ITatameClient
    {
        public int PlacarCount { get; private set; }
        public int CronometroCount { get; private set; }
        public int LutaIniciadaCount { get; private set; }
        public int LutaFinalizadaCount { get; private set; }

        public Task PlacarAtualizado(object placar) { PlacarCount++; return Task.CompletedTask; }
        public Task CronometroAtualizado(object cronometro) { CronometroCount++; return Task.CompletedTask; }
        public Task LutaIniciada(object luta) { LutaIniciadaCount++; return Task.CompletedTask; }
        public Task LutaFinalizada(object resultado) { LutaFinalizadaCount++; return Task.CompletedTask; }
        public Task NotificacaoGeral(string mensagem) => Task.CompletedTask;
    }

    private sealed class FakeHubContext : IHubContext<TatameHub, ITatameClient>
    {
        private readonly FakeHubClients _clients;
        public FakeHubContext(ITatameClient proxy) => _clients = new FakeHubClients(proxy);
        public IHubClients<ITatameClient> Clients => _clients;
        public IGroupManager Groups => new FakeGroupManager();
    }

    private sealed class FakeHubClients : IHubClients<ITatameClient>
    {
        private readonly ITatameClient _proxy;
        public FakeHubClients(ITatameClient proxy) => _proxy = proxy;
        public ITatameClient All => _proxy;
        public ITatameClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public ITatameClient Client(string connectionId) => _proxy;
        public ITatameClient Clients(IReadOnlyList<string> connectionIds) => _proxy;
        public ITatameClient Group(string groupName) => _proxy;
        public ITatameClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public ITatameClient Groups(IReadOnlyList<string> groupNames) => _proxy;
        public ITatameClient User(string userId) => _proxy;
        public ITatameClient Users(IReadOnlyList<string> userIds) => _proxy;
    }
}
