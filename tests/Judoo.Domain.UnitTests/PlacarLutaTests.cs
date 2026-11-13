using Judoo.Domain.Entities;
using Judoo.Domain.Enums;

namespace Judoo.Domain.UnitTests;

public class PlacarLutaTests
{
    [Fact]
    public void Construtor_Padrao_E_ComLutaId_DevemInicializarValoresEsperados()
    {
        var placarVazio = new PlacarLuta();
        Assert.Equal(Guid.Empty, placarVazio.LutaId);
        Assert.Equal(0, placarVazio.CronometroSegundos);

        var lutaId = Guid.NewGuid();
        var luta = new Luta { Id = lutaId };
        var placar = new PlacarLuta(lutaId, 180)
        {
            CronometroRodando = true,
            SonoMama = true,
            EmGoldenScore = false,
            Luta = luta
        };

        Assert.Equal(lutaId, placar.LutaId);
        Assert.Equal(180, placar.CronometroSegundos);
        Assert.True(placar.CronometroRodando);
        Assert.True(placar.SonoMama);
        Assert.False(placar.EmGoldenScore);
        Assert.Same(luta, placar.Luta);
        Assert.Equal(0, placar.IpponBranco);
        Assert.Equal(0, placar.WazaAriBranco);
        Assert.Equal(0, placar.ShidoBranco);
        Assert.Equal(0, placar.IpponAzul);
        Assert.Equal(0, placar.WazaAriAzul);
        Assert.Equal(0, placar.ShidoAzul);
        Assert.False(placar.OsaekomiAtivo);
        Assert.Null(placar.OsaekomiCor);
        Assert.Equal(0, placar.OsaekomiTempoSegundos);
    }

    [Theory]
    [InlineData(CorJudogi.Branco)]
    [InlineData(CorJudogi.Azul)]
    public void AplicarPontuacao_Ippon_DeveEncerrarLutaComVitoriaPorIppon(CorJudogi cor)
    {
        var placar = new PlacarLuta(Guid.NewGuid());

        var (encerrada, vencedor, tipo) = placar.AplicarPontuacao(cor, TipoPontuacao.Ippon);

        Assert.True(encerrada);
        Assert.Equal(cor, vencedor);
        Assert.Equal(TipoVitoria.Ippon, tipo);

        if (cor == CorJudogi.Branco)
        {
            Assert.Equal(1, placar.IpponBranco);
            Assert.Equal(0, placar.IpponAzul);
        }
        else
        {
            Assert.Equal(1, placar.IpponAzul);
            Assert.Equal(0, placar.IpponBranco);
        }
    }

    [Theory]
    [InlineData(CorJudogi.Branco)]
    [InlineData(CorJudogi.Azul)]
    public void AplicarPontuacao_WazaAri_PrimeiroNaoEncerra_SegundoEncerraComoAwaseteIppon(CorJudogi cor)
    {
        var placar = new PlacarLuta(Guid.NewGuid());

        var primeiro = placar.AplicarPontuacao(cor, TipoPontuacao.WazaAri);
        Assert.False(primeiro.lutaEncerrada);
        Assert.Null(primeiro.vencedor);
        Assert.Null(primeiro.tipoVitoria);

        var segundo = placar.AplicarPontuacao(cor, TipoPontuacao.WazaAri);
        Assert.True(segundo.lutaEncerrada);
        Assert.Equal(cor, segundo.vencedor);
        Assert.Equal(TipoVitoria.WazaAriAwaseteIppon, segundo.tipoVitoria);

        if (cor == CorJudogi.Branco)
        {
            Assert.Equal(2, placar.WazaAriBranco);
            Assert.Equal(1, placar.IpponBranco);
        }
        else
        {
            Assert.Equal(2, placar.WazaAriAzul);
            Assert.Equal(1, placar.IpponAzul);
        }
    }

    [Theory]
    [InlineData(CorJudogi.Branco)]
    [InlineData(CorJudogi.Azul)]
    public void AplicarPontuacao_WazaAriNoGoldenScore_DeveEncerrarImediatamente(CorJudogi cor)
    {
        var placar = new PlacarLuta(Guid.NewGuid())
        {
            EmGoldenScore = true
        };

        var (encerrada, vencedor, tipo) = placar.AplicarPontuacao(cor, TipoPontuacao.WazaAri);

        Assert.True(encerrada);
        Assert.Equal(cor, vencedor);
        Assert.Equal(TipoVitoria.WazaAri, tipo);
    }

    [Theory]
    [InlineData(CorJudogi.Branco, CorJudogi.Azul)]
    [InlineData(CorJudogi.Azul, CorJudogi.Branco)]
    public void AplicarPontuacao_Shido_TresPenalidadesResultamEmHansokuMakeParaOponente(CorJudogi penalizado, CorJudogi vencedorEsperado)
    {
        var placar = new PlacarLuta(Guid.NewGuid());

        var s1 = placar.AplicarPontuacao(penalizado, TipoPontuacao.Shido);
        var s2 = placar.AplicarPontuacao(penalizado, TipoPontuacao.Shido);
        var s3 = placar.AplicarPontuacao(penalizado, TipoPontuacao.Shido);
        var s4 = placar.AplicarPontuacao(penalizado, TipoPontuacao.Shido);

        Assert.False(s1.lutaEncerrada);
        Assert.False(s2.lutaEncerrada);
        Assert.True(s3.lutaEncerrada);
        Assert.Equal(vencedorEsperado, s3.vencedor);
        Assert.Equal(TipoVitoria.HansokuMake, s3.tipoVitoria);

        // Limite máximo de 3 shidos mantido
        Assert.True(s4.lutaEncerrada);
        int shidosFinal = penalizado == CorJudogi.Branco ? placar.ShidoBranco : placar.ShidoAzul;
        Assert.Equal(3, shidosFinal);
    }

    [Theory]
    [InlineData(CorJudogi.Branco, CorJudogi.Azul)]
    [InlineData(CorJudogi.Azul, CorJudogi.Branco)]
    public void AplicarPontuacao_ShidoNoGoldenScore_DeveEncerrarImediatamenteComoHansokuMake(CorJudogi penalizado, CorJudogi vencedorEsperado)
    {
        var placar = new PlacarLuta(Guid.NewGuid())
        {
            EmGoldenScore = true
        };

        var (encerrada, vencedor, tipo) = placar.AplicarPontuacao(penalizado, TipoPontuacao.Shido);

        Assert.True(encerrada);
        Assert.Equal(vencedorEsperado, vencedor);
        Assert.Equal(TipoVitoria.HansokuMake, tipo);
    }

    [Theory]
    [InlineData(CorJudogi.Branco)]
    [InlineData(CorJudogi.Azul)]
    public void AplicarPontuacao_CancelarWazaAri_DeveDecrementarELimparIpponQuandoAplicavel(CorJudogi cor)
    {
        var placar = new PlacarLuta(Guid.NewGuid());

        // Cancelamento com zero não deve negativar
        var zeroResult = placar.AplicarPontuacao(cor, TipoPontuacao.CancelarWazaAri);
        Assert.False(zeroResult.lutaEncerrada);

        placar.AplicarPontuacao(cor, TipoPontuacao.WazaAri);
        placar.AplicarPontuacao(cor, TipoPontuacao.WazaAri);

        placar.AplicarPontuacao(cor, TipoPontuacao.CancelarWazaAri);

        if (cor == CorJudogi.Branco)
        {
            Assert.Equal(1, placar.WazaAriBranco);
            Assert.Equal(0, placar.IpponBranco);
        }
        else
        {
            Assert.Equal(1, placar.WazaAriAzul);
            Assert.Equal(0, placar.IpponAzul);
        }
    }

    [Theory]
    [InlineData(CorJudogi.Branco)]
    [InlineData(CorJudogi.Azul)]
    public void AplicarPontuacao_CancelarShido_DeveDecrementarQuandoMaiorQueZero(CorJudogi cor)
    {
        var placar = new PlacarLuta(Guid.NewGuid());

        // Cancelamento com zero não deve negativar
        var zeroResult = placar.AplicarPontuacao(cor, TipoPontuacao.CancelarShido);
        Assert.False(zeroResult.lutaEncerrada);

        placar.AplicarPontuacao(cor, TipoPontuacao.Shido);
        placar.AplicarPontuacao(cor, TipoPontuacao.Shido);
        placar.AplicarPontuacao(cor, TipoPontuacao.CancelarShido);

        int shidos = cor == CorJudogi.Branco ? placar.ShidoBranco : placar.ShidoAzul;
        Assert.Equal(1, shidos);
    }

    [Fact]
    public void AtualizarOsaekomi_QuandoInativo_DeveAtualizarTempoSemPontuar()
    {
        var placar = new PlacarLuta(Guid.NewGuid());

        var (encerrada, vencedor, tipo) = placar.AtualizarOsaekomi(15);

        Assert.False(encerrada);
        Assert.Null(vencedor);
        Assert.Null(tipo);
        Assert.Equal(15, placar.OsaekomiTempoSegundos);
    }

    [Theory]
    [InlineData(CorJudogi.Branco)]
    [InlineData(CorJudogi.Azul)]
    public void Osaekomi_ProgressaoCompleta_10sWazaAri_20sIppon_E_Toketa(CorJudogi cor)
    {
        var placar = new PlacarLuta(Guid.NewGuid());
        placar.IniciarOsaekomi(cor);

        Assert.True(placar.OsaekomiAtivo);
        Assert.Equal(cor, placar.OsaekomiCor);
        Assert.Equal(0, placar.OsaekomiTempoSegundos);

        // 9s não pontua
        var t9 = placar.AtualizarOsaekomi(9);
        Assert.False(t9.lutaEncerrada);
        Assert.Equal(0, cor == CorJudogi.Branco ? placar.WazaAriBranco : placar.WazaAriAzul);

        // 10s sem Waza-ari prévio concede Waza-ari e continua
        var t10 = placar.AtualizarOsaekomi(10);
        Assert.False(t10.lutaEncerrada);
        Assert.Equal(1, cor == CorJudogi.Branco ? placar.WazaAriBranco : placar.WazaAriAzul);

        // Toketa interrompe imobilização sem perder o Waza-ari conquistado
        placar.Toketa();
        Assert.False(placar.OsaekomiAtivo);
        Assert.Null(placar.OsaekomiCor);
        Assert.Equal(0, placar.OsaekomiTempoSegundos);

        // Nova imobilização com Waza-ari prévio encerra aos 10s com Awasete Ippon
        placar.IniciarOsaekomi(cor);
        var t10Segundo = placar.AtualizarOsaekomi(10);
        Assert.True(t10Segundo.lutaEncerrada);
        Assert.Equal(cor, t10Segundo.vencedor);
        Assert.Equal(TipoVitoria.WazaAriAwaseteIppon, t10Segundo.tipoVitoria);
    }

    [Theory]
    [InlineData(CorJudogi.Branco)]
    [InlineData(CorJudogi.Azul)]
    public void AtualizarOsaekomi_20SegundosSemWazaAriPrevio_DeveConcederIpponDireto(CorJudogi cor)
    {
        var placar = new PlacarLuta(Guid.NewGuid());
        placar.IniciarOsaekomi(cor);

        var t15 = placar.AtualizarOsaekomi(15);
        Assert.False(t15.lutaEncerrada);

        var t20 = placar.AtualizarOsaekomi(20);
        Assert.True(t20.lutaEncerrada);
        Assert.Equal(cor, t20.vencedor);
        Assert.Equal(TipoVitoria.Ippon, t20.tipoVitoria);
    }

    [Fact]
    public void SetRawScores_DeveHidratarTodosOsCamposDoPlacar()
    {
        var placar = new PlacarLuta(Guid.NewGuid());

        placar.SetRawScores(
            ipponB: 1,
            wazaariB: 2,
            shidoB: 1,
            ipponA: 0,
            wazaariA: 1,
            shidoA: 2,
            osaekomi: true,
            corOsae: CorJudogi.Azul,
            tempoOsae: 12,
            cronometro: 95,
            goldenScore: true,
            rodando: true,
            sonoMama: true
        );

        Assert.Equal(1, placar.IpponBranco);
        Assert.Equal(2, placar.WazaAriBranco);
        Assert.Equal(1, placar.ShidoBranco);
        Assert.Equal(0, placar.IpponAzul);
        Assert.Equal(1, placar.WazaAriAzul);
        Assert.Equal(2, placar.ShidoAzul);
        Assert.True(placar.OsaekomiAtivo);
        Assert.Equal(CorJudogi.Azul, placar.OsaekomiCor);
        Assert.Equal(12, placar.OsaekomiTempoSegundos);
        Assert.Equal(95, placar.CronometroSegundos);
        Assert.True(placar.EmGoldenScore);
        Assert.True(placar.CronometroRodando);
        Assert.True(placar.SonoMama);
    }
}
