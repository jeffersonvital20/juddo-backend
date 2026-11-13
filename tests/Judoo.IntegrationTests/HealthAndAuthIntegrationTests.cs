using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Judoo.Application.Commands;
using Judoo.Application.DTOs;
using Judoo.Domain.Enums;
using Judoo.IntegrationTests.Fixtures;

namespace Judoo.IntegrationTests;

public class HealthAndAuthIntegrationTests : IClassFixture<JudooApiFactory>
{
    private readonly HttpClient _client;

    public HealthAndAuthIntegrationTests(JudooApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> LoginAsAsync(string email, string senha = "123456")
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = senha });
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<UsuarioAuthDto>())!;
        return auth.Token;
    }

    [Fact]
    public async Task HealthEndpoint_DeveRetornar200Healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task SwaggerJson_DeveEstarDisponivelEmDevelopment()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Judoo API", body);
    }

    [Theory]
    [InlineData("admin@judoo.com", "123456", "ADMIN")]
    [InlineData("secretaria@judoo.com", "123456", "SECRETARIA")]
    [InlineData("mesario1@judoo.com", "123456", "MESARIO")]
    [InlineData("publico@judoo.com", "123456", "PUBLICO")]
    public async Task AuthLogin_DeveRetornarJwtEPerfilCorretoParaUsuariosSeed(string email, string senha, string expectedRole)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = senha });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<UsuarioAuthDto>();
        Assert.NotNull(dto);
        Assert.Equal(expectedRole, dto!.Role);
        Assert.Contains(".", dto.Token);
    }

    [Fact]
    public async Task AuthLogin_CredenciaisInvalidas_DeveRetornar401()
    {
        var errSenha = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "admin@judoo.com", Senha = "errada" });
        Assert.Equal(HttpStatusCode.Unauthorized, errSenha.StatusCode);

        var errUser = await _client.PostAsJsonAsync("/api/auth/login", new { Email = "naoexiste@judoo.com", Senha = "123456" });
        Assert.Equal(HttpStatusCode.Unauthorized, errUser.StatusCode);
    }

    [Fact]
    public async Task CadastroDeUsuariosPorPerfil_E_PermissoesRBAC_DevemSerRespeitados()
    {
        // 1. Anônimo tentando criar campeonato ou usuário -> 401 Unauthorized
        var anonCamp = await _client.PostAsJsonAsync("/api/campeonatos", new CriarCampeonatoCommand("Teste", "SP", DateTime.UtcNow, DateTime.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized, anonCamp.StatusCode);

        var anonUsuarios = await _client.GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Unauthorized, anonUsuarios.StatusCode);

        // 2. Auto-cadastro público -> sempre cria perfil PUBLICO e permite login
        var emailPub = $"torcedor.{Guid.NewGuid():N}@judoo.com";
        var regPubResp = await _client.PostAsJsonAsync("/api/auth/registrar", new { Nome = "Torcedor", Email = emailPub, Senha = "senha123" });
        Assert.Equal(HttpStatusCode.Created, regPubResp.StatusCode);
        var pubDto = await regPubResp.Content.ReadFromJsonAsync<UsuarioDto>();
        Assert.Equal("PUBLICO", pubDto!.Role);

        // Duplicidade -> 409 Conflict; senha curta -> 400 BadRequest
        var dupPubResp = await _client.PostAsJsonAsync("/api/auth/registrar", new { Nome = "Torcedor", Email = emailPub, Senha = "senha123" });
        Assert.Equal(HttpStatusCode.Conflict, dupPubResp.StatusCode);

        var badPubResp = await _client.PostAsJsonAsync("/api/auth/registrar", new { Nome = "X", Email = "x@judoo.com", Senha = "12" });
        Assert.Equal(HttpStatusCode.BadRequest, badPubResp.StatusCode);

        // 3. Autenticar como Secretaria e tentar criar campeonato ou usuário -> 403 Forbidden
        var secToken = await LoginAsAsync("secretaria@judoo.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secToken);

        var secCamp = await _client.PostAsJsonAsync("/api/campeonatos", new CriarCampeonatoCommand("Teste", "SP", DateTime.UtcNow, DateTime.UtcNow));
        Assert.Equal(HttpStatusCode.Forbidden, secCamp.StatusCode);

        var secUser = await _client.PostAsJsonAsync("/api/usuarios", new { Nome = "Novo", Email = "novo@judoo.com", Senha = "123456", Role = PerfilUsuario.Mesario });
        Assert.Equal(HttpStatusCode.Forbidden, secUser.StatusCode);

        // 4. Autenticar como Admin -> cadastrar novo Mesario, listar, obter por ID e desativar
        var adminToken = await LoginAsAsync("admin@judoo.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var emailMesario = $"mesario.{Guid.NewGuid():N}@judoo.com";
        var criarMesarioResp = await _client.PostAsJsonAsync("/api/usuarios", new
        {
            Nome = "Mesário Novo",
            Email = emailMesario,
            Senha = "segredo123",
            Role = PerfilUsuario.Mesario
        });
        Assert.Equal(HttpStatusCode.Created, criarMesarioResp.StatusCode);
        var mesarioDto = (await criarMesarioResp.Content.ReadFromJsonAsync<UsuarioDto>())!;
        Assert.Equal("MESARIO", mesarioDto.Role);

        var usuarios = await _client.GetFromJsonAsync<List<UsuarioDto>>("/api/usuarios");
        Assert.Contains(usuarios!, u => u.Id == mesarioDto.Id);

        var porId = await _client.GetFromJsonAsync<UsuarioDto>($"/api/usuarios/{mesarioDto.Id}");
        Assert.Equal(emailMesario, porId!.Email);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/usuarios/{Guid.NewGuid()}")).StatusCode);

        // Login do novo mesário antes de desativar funciona
        var novoMesarioToken = await LoginAsAsync(emailMesario, "segredo123");
        Assert.False(string.IsNullOrWhiteSpace(novoMesarioToken));

        // Admin desativa o usuário -> novo login deve retornar 401
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var desativarResp = await _client.PutAsJsonAsync($"/api/usuarios/{mesarioDto.Id}/status", new { Ativo = false });
        Assert.Equal(HttpStatusCode.OK, desativarResp.StatusCode);

        var loginDesativado = await _client.PostAsJsonAsync("/api/auth/login", new { Email = emailMesario, Senha = "segredo123" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginDesativado.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
