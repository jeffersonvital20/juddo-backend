using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Judoo.Application.Commands;
using Judoo.Application.Commands.RegistrarPonto;
using Judoo.Application.DTOs;
using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Judoo.IntegrationTests.Fixtures;

namespace Judoo.IntegrationTests;

public class LutasAndTatamesIntegrationTests : IClassFixture<JudooApiFactory>
{
    private readonly JudooApiFactory _factory;
    private readonly HttpClient _client;

    public LutasAndTatamesIntegrationTests(JudooApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task AutenticarAsync(string email)
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = "123456" });
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<UsuarioAuthDto>())!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }

    private async Task<(CampeonatoDto camp, TatameStatusDto tatame, Chave chave, AtletaCadastroDto branco, AtletaCadastroDto azul)> CriarCenarioBaseAsync()
    {
        await AutenticarAsync("admin@judoo.com");

        var campResp = await _client.PostAsJsonAsync("/api/campeonatos", new CriarCampeonatoCommand(
            Nome: $"Open Judô {Guid.NewGuid():N}",
            Local: "Arena Carioca",
            DataInicio: DateTime.UtcNow,
            DataFim: DateTime.UtcNow.AddDays(1),
            QuantidadeTatames: 2
        ));
        var camp = (await campResp.Content.ReadFromJsonAsync<CampeonatoDto>())!;

        var categorias = (await _client.GetFromJsonAsync<List<CategoriaDto>>($"/api/campeonatos/{camp.Id}/categorias"))!;
        var tatames = (await _client.GetFromJsonAsync<List<TatameStatusDto>>($"/api/tatames?campeonatoId={camp.Id}"))!;
        Assert.Equal(2, tatames.Count);

        var chave = new Chave
        {
            Id = Guid.NewGuid(),
            CampeonatoId = camp.Id,
            CategoriaId = categorias[0].Id,
            Tipo = "ELIMINATORIA_SIMPLES",
            TotalFases = 2,
            Gerada = true
        };
        _factory.Store.Chaves[chave.Id] = chave;

        await AutenticarAsync("secretaria@judoo.com");

        var acadResp = await _client.PostAsJsonAsync("/api/atletas/academias", new CadastrarAcademiaCommand("Pinheiros", "ECP", "São Paulo", "SP"));
        var acad = (await acadResp.Content.ReadFromJsonAsync<AcademiaDto>())!;

        var brancoResp = await _client.PostAsJsonAsync("/api/atletas", new CadastrarAtletaCommand(acad.Id, "Atleta Branco", "R1", DateTime.UtcNow.AddYears(-22), "MASCULINO", null));
        var azulResp = await _client.PostAsJsonAsync("/api/atletas", new CadastrarAtletaCommand(acad.Id, "Atleta Azul", "R2", DateTime.UtcNow.AddYears(-23), "MASCULINO", null));

        var branco = (await brancoResp.Content.ReadFromJsonAsync<AtletaCadastroDto>())!;
        var azul = (await azulResp.Content.ReadFromJsonAsync<AtletaCadastroDto>())!;

        return (camp, tatames[0], chave, branco, azul);
    }

    [Fact]
    public async Task FluxoLuta_PontuacaoFIJ_Cronometro_Osaekomi_E_Tatames_DevemIntegrarCorretamente()
    {
        var (camp, tatame, chave, branco, azul) = await CriarCenarioBaseAsync();

        // 1. Secretaria cria Luta Manual e atribui ao tatame
        var createLutaResp = await _client.PostAsJsonAsync("/api/lutas", new CriarLutaManualCommand(
            ChaveId: chave.Id,
            TatameId: tatame.Id,
            AtletaBrancoId: branco.Id,
            AtletaAzulId: azul.Id,
            Fase: 2,
            OrdemNaChave: 1,
            OrdemNoTatame: 1
        ));
        Assert.Equal(HttpStatusCode.Created, createLutaResp.StatusCode);
        var luta = (await createLutaResp.Content.ReadFromJsonAsync<LutaDetalhesDto>())!;
        Assert.Equal("Agendada", luta.Status);

        var atribResp = await _client.PutAsJsonAsync($"/api/lutas/{luta.Id}/tatame", new { TatameId = tatame.Id, OrdemNoTatame = 1 });
        Assert.Equal(HttpStatusCode.OK, atribResp.StatusCode);

        // Secretaria não pode pontuar -> 403 Forbidden
        var secPontuar = await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/pontuar", new
        {
            TatameId = tatame.Id,
            Judogi = CorJudogi.Branco,
            Tipo = TipoPontuacao.WazaAri,
            TempoLutaSegundos = 100
        });
        Assert.Equal(HttpStatusCode.Forbidden, secPontuar.StatusCode);

        // 2. Mesário assume o tatame e inicia a luta via PUT /api/tatames/{tatameId}/luta-atual
        await AutenticarAsync("mesario1@judoo.com");

        var definirLutaAtualResp = await _client.PutAsJsonAsync($"/api/tatames/{tatame.Id}/luta-atual", new
        {
            LutaId = luta.Id,
            IniciarLuta = true
        });
        Assert.Equal(HttpStatusCode.OK, definirLutaAtualResp.StatusCode);

        var tatameInexistenteResp = await _client.PutAsJsonAsync($"/api/tatames/{Guid.NewGuid()}/luta-atual", new
        {
            LutaId = luta.Id,
            IniciarLuta = false
        });
        Assert.Equal(HttpStatusCode.NotFound, tatameInexistenteResp.StatusCode);

        var tatamesAtualizados = await _client.GetFromJsonAsync<List<TatameStatusDto>>($"/api/tatames?campeonatoId={camp.Id}");
        Assert.Contains(tatamesAtualizados!, t => t.Id == tatame.Id && t.LutaAtualId == luta.Id && t.LutaAtual != null);

        // Consultas públicas
        var lutasCamp = await _client.GetFromJsonAsync<List<LutaDetalhesDto>>($"/api/lutas?campeonatoId={camp.Id}");
        Assert.Contains(lutasCamp!, l => l.Id == luta.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/lutas/{Guid.NewGuid()}")).StatusCode);

        // 3. Alterar status manualmente + Cronômetro
        var statusEmAndamentoResp = await _client.PutAsJsonAsync($"/api/lutas/{luta.Id}/status", new
        {
            Status = StatusLuta.EmAndamento,
            VencedorId = (Guid?)null,
            TipoVitoria = (TipoVitoria?)null
        });
        Assert.Equal(HttpStatusCode.OK, statusEmAndamentoResp.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/lutas/{luta.Id}/placar")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/lutas/{Guid.NewGuid()}/placar")).StatusCode);

        var cronoResp = await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/cronometro", new
        {
            TatameId = tatame.Id,
            CronometroSegundos = 210,
            CronometroRodando = true,
            EmGoldenScore = false,
            SonoMama = false
        });
        Assert.Equal(HttpStatusCode.OK, cronoResp.StatusCode);

        // 4. Osaekomi (Iniciar -> 10s Waza-ari -> Toketa)
        await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/osaekomi", new
        {
            TatameId = tatame.Id,
            Acao = AcaoOsaekomi.Iniciar,
            Judogi = CorJudogi.Branco,
            Segundos = 0
        });

        var osaeTick10 = await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/osaekomi", new
        {
            TatameId = tatame.Id,
            Acao = AcaoOsaekomi.AtualizarSegundos,
            Judogi = CorJudogi.Branco,
            Segundos = 10
        });
        Assert.Equal(1, (await osaeTick10.Content.ReadFromJsonAsync<PlacarLutaDto>())!.WazaAriBranco);

        await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/osaekomi", new
        {
            TatameId = tatame.Id,
            Acao = AcaoOsaekomi.Toketa,
            Judogi = (CorJudogi?)null,
            Segundos = 0
        });

        // 5. Pontuar Shido Azul e 2º Waza-ari Branco -> Awasete Ippon
        await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/pontuar", new
        {
            TatameId = tatame.Id,
            Judogi = CorJudogi.Azul,
            Tipo = TipoPontuacao.Shido,
            TempoLutaSegundos = 140,
            UsuarioId = (Guid?)null
        });

        var pontuarAwasete = await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/pontuar", new
        {
            TatameId = tatame.Id,
            Judogi = CorJudogi.Branco,
            Tipo = TipoPontuacao.WazaAri,
            TempoLutaSegundos = 155,
            UsuarioId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.OK, pontuarAwasete.StatusCode);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var pontoResult = await pontuarAwasete.Content.ReadFromJsonAsync<RegistrarPontoResult>(jsonOptions);
        Assert.True(pontoResult!.Sucesso);
        Assert.True(pontoResult.LutaEncerrada);
        Assert.Equal(CorJudogi.Branco, pontoResult.Vencedor);
        Assert.Equal(TipoVitoria.WazaAriAwaseteIppon, pontoResult.TipoVitoria);

        var lutaFinalizada = await _client.GetFromJsonAsync<LutaDetalhesDto>($"/api/lutas/{luta.Id}");
        Assert.Equal("Finalizada", lutaFinalizada!.Status);
        Assert.Equal("WazaAriAwaseteIppon", lutaFinalizada.TipoVitoria);
    }

    [Fact]
    public async Task Osaekomi_QueEncerraLutaNoTick_DeveFinalizarLutaENotificar()
    {
        var (_, tatame, chave, branco, azul) = await CriarCenarioBaseAsync();

        var createLutaResp = await _client.PostAsJsonAsync("/api/lutas", new CriarLutaManualCommand(
            ChaveId: chave.Id,
            TatameId: tatame.Id,
            AtletaBrancoId: branco.Id,
            AtletaAzulId: azul.Id
        ));
        var luta = (await createLutaResp.Content.ReadFromJsonAsync<LutaDetalhesDto>())!;

        await AutenticarAsync("mesario1@judoo.com");

        await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/osaekomi", new
        {
            TatameId = tatame.Id,
            Acao = AcaoOsaekomi.Iniciar,
            Judogi = CorJudogi.Azul,
            Segundos = 0
        });

        var tick20 = await _client.PostAsJsonAsync($"/api/lutas/{luta.Id}/osaekomi", new
        {
            TatameId = tatame.Id,
            Acao = AcaoOsaekomi.AtualizarSegundos,
            Judogi = CorJudogi.Azul,
            Segundos = 20
        });
        Assert.Equal(HttpStatusCode.OK, tick20.StatusCode);

        var lutaDepois = await _client.GetFromJsonAsync<LutaDetalhesDto>($"/api/lutas/{luta.Id}");
        Assert.Equal("Finalizada", lutaDepois!.Status);
        Assert.Equal("Ippon", lutaDepois.TipoVitoria);
        Assert.Equal(1, lutaDepois.Placar!.IpponAzul);
    }
}
