using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Judoo.Application.Commands;
using Judoo.Application.DTOs;
using Judoo.IntegrationTests.Fixtures;

namespace Judoo.IntegrationTests;

public class CampeonatosAndAtletasIntegrationTests : IClassFixture<JudooApiFactory>
{
    private readonly HttpClient _client;

    public CampeonatosAndAtletasIntegrationTests(JudooApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AutenticarAsync(string email)
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = "123456" });
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<UsuarioAuthDto>())!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }

    [Fact]
    public async Task FluxoCompleto_Campeonato_Academia_Atleta_Inscricao_E_Grade_DeveFuncionarDePontaAPonta()
    {
        // 1. Admin cria Campeonato
        await AutenticarAsync("admin@judoo.com");

        var criarCampCmd = new CriarCampeonatoCommand(
            Nome: "Campeonato Brasileiro Sênior 2026",
            Local: "Ginásio Ibirapuera - SP",
            DataInicio: new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc),
            DataFim: new DateTime(2026, 11, 21, 18, 0, 0, DateTimeKind.Utc),
            QuantidadeTatames: 3
        );

        var createCampResp = await _client.PostAsJsonAsync("/api/campeonatos", criarCampCmd);
        Assert.Equal(HttpStatusCode.Created, createCampResp.StatusCode);

        var campeonato = await createCampResp.Content.ReadFromJsonAsync<CampeonatoDto>();
        Assert.NotNull(campeonato);
        Assert.Equal("Campeonato Brasileiro Sênior 2026", campeonato!.Nome);
        Assert.Equal("EM_ANDAMENTO", campeonato.Status);

        // 2. Listar todos, obter por ID e atualizar status
        var listResp = await _client.GetFromJsonAsync<List<CampeonatoDto>>("/api/campeonatos");
        Assert.Contains(listResp!, c => c.Id == campeonato.Id);

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/campeonatos/{campeonato.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/campeonatos/{Guid.NewGuid()}")).StatusCode);

        var statusResp = await _client.PutAsJsonAsync($"/api/campeonatos/{campeonato.Id}/status", "FINALIZADO");
        Assert.Equal(HttpStatusCode.OK, statusResp.StatusCode);

        var updatedCamp = await _client.GetFromJsonAsync<CampeonatoDto>($"/api/campeonatos/{campeonato.Id}");
        Assert.Equal("FINALIZADO", updatedCamp!.Status);

        // 3. Obter Categorias e Chaves criadas automaticamente
        var categorias = await _client.GetFromJsonAsync<List<CategoriaDto>>($"/api/campeonatos/{campeonato.Id}/categorias");
        Assert.NotEmpty(categorias!);
        var categoriaLeve = categorias![0];

        var chaves = await _client.GetFromJsonAsync<List<ChaveDto>>($"/api/campeonatos/{campeonato.Id}/chaves");
        Assert.Equal(categorias.Count, chaves!.Count);

        // 4. Secretaria cadastra Academia, Atletas e monta a Grade
        await AutenticarAsync("secretaria@judoo.com");

        var academiaResp = await _client.PostAsJsonAsync("/api/atletas/academias", new CadastrarAcademiaCommand(
            Nome: "Sogipa Judô",
            Sigla: "sog",
            Cidade: "Porto Alegre",
            Estado: "rs"
        ));
        Assert.Equal(HttpStatusCode.Created, academiaResp.StatusCode);

        var academia = (await academiaResp.Content.ReadFromJsonAsync<AcademiaDto>())!;
        Assert.Equal("SOG", academia.Sigla);
        Assert.Equal("RS", academia.Estado);

        var academias = await _client.GetFromJsonAsync<List<AcademiaDto>>("/api/atletas/academias");
        Assert.Contains(academias!, a => a.Id == academia.Id);

        var atletaComInscricaoResp = await _client.PostAsJsonAsync("/api/atletas", new CadastrarAtletaCommand(
            AcademiaId: academia.Id,
            Nome: "Mayra Aguiar",
            RegistroFederacao: "CBJ-101",
            DataNascimento: new DateTime(1991, 8, 3, 0, 0, 0, DateTimeKind.Utc),
            Sexo: "FEMININO",
            FotoUrl: null,
            CampeonatoId: campeonato.Id,
            CategoriaId: categoriaLeve.Id,
            PesoAferidoKg: 77.5m
        ));
        Assert.Equal(HttpStatusCode.Created, atletaComInscricaoResp.StatusCode);

        // Menos de 2 atletas -> gerar grade deve retornar 400 BadRequest
        var gradeInsuficiente = await _client.PostAsJsonAsync($"/api/campeonatos/{campeonato.Id}/chaves/gerar", new { CategoriaId = categoriaLeve.Id });
        Assert.Equal(HttpStatusCode.BadRequest, gradeInsuficiente.StatusCode);

        var atletaSemInscricaoResp = await _client.PostAsJsonAsync("/api/atletas", new CadastrarAtletaCommand(
            AcademiaId: academia.Id,
            Nome: "Daniel Cargnin",
            RegistroFederacao: "CBJ-202",
            DataNascimento: new DateTime(1997, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            Sexo: "MASCULINO",
            FotoUrl: null
        ));
        var atletaSem = (await atletaSemInscricaoResp.Content.ReadFromJsonAsync<AtletaCadastroDto>())!;

        var inscreverResp = await _client.PostAsJsonAsync("/api/atletas/inscrever", new InscreverAtletaCommand(
            CampeonatoId: campeonato.Id,
            AtletaId: atletaSem.Id,
            CategoriaId: categoriaLeve.Id,
            PesoKg: 72.8m
        ));
        Assert.Equal(HttpStatusCode.OK, inscreverResp.StatusCode);

        var inscritos = await _client.GetFromJsonAsync<List<AtletaCadastroDto>>($"/api/atletas?campeonatoId={campeonato.Id}");
        Assert.Equal(2, inscritos!.Count);

        // 5. Gerar grade automática de lutas para a categoria
        var gradeResp = await _client.PostAsJsonAsync($"/api/campeonatos/{campeonato.Id}/chaves/gerar", new { CategoriaId = categoriaLeve.Id });
        Assert.Equal(HttpStatusCode.OK, gradeResp.StatusCode);
        var lutasGeradas = (await gradeResp.Content.ReadFromJsonAsync<List<LutaDetalhesDto>>())!;
        Assert.Single(lutasGeradas);
        Assert.NotNull(lutasGeradas[0].TatameId);
    }
}
