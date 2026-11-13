using Judoo.Domain.Enums;

namespace Judoo.Domain.Entities;

public class PlacarLuta
{
    public Guid LutaId { get; set; }
    public int IpponBranco { get; private set; }
    public int WazaAriBranco { get; private set; }
    public int ShidoBranco { get; private set; }

    public int IpponAzul { get; private set; }
    public int WazaAriAzul { get; private set; }
    public int ShidoAzul { get; private set; }

    public bool OsaekomiAtivo { get; private set; }
    public CorJudogi? OsaekomiCor { get; private set; }
    public int OsaekomiTempoSegundos { get; private set; }

    public int CronometroSegundos { get; set; }
    public bool EmGoldenScore { get; set; }
    public bool CronometroRodando { get; set; }
    public bool SonoMama { get; set; }
    public DateTime UltimaAtualizacao { get; set; } = DateTime.UtcNow;
    public Luta? Luta { get; set; }

    public PlacarLuta() { }

    public PlacarLuta(Guid lutaId, int tempoInicialSegundos = 240)
    {
        LutaId = lutaId;
        CronometroSegundos = tempoInicialSegundos;
    }

    /// <summary>
    /// Aplica uma ação de pontuação ou penalidade conforme as regras da FIJ.
    /// Retorna se a pontuação causou o encerramento da luta e o tipo de vitória resultante.
    /// </summary>
    public (bool lutaEncerrada, CorJudogi? vencedor, TipoVitoria? tipoVitoria) AplicarPontuacao(CorJudogi judogi, TipoPontuacao tipo)
    {
        UltimaAtualizacao = DateTime.UtcNow;

        switch (tipo)
        {
            case TipoPontuacao.Ippon:
                if (judogi == CorJudogi.Branco)
                {
                    IpponBranco = 1;
                    return (true, CorJudogi.Branco, TipoVitoria.Ippon);
                }
                else
                {
                    IpponAzul = 1;
                    return (true, CorJudogi.Azul, TipoVitoria.Ippon);
                }

            case TipoPontuacao.WazaAri:
                if (judogi == CorJudogi.Branco)
                {
                    WazaAriBranco++;
                    if (WazaAriBranco >= 2)
                    {
                        IpponBranco = 1;
                        return (true, CorJudogi.Branco, TipoVitoria.WazaAriAwaseteIppon);
                    }
                    if (EmGoldenScore)
                    {
                        return (true, CorJudogi.Branco, TipoVitoria.WazaAri);
                    }
                }
                else
                {
                    WazaAriAzul++;
                    if (WazaAriAzul >= 2)
                    {
                        IpponAzul = 1;
                        return (true, CorJudogi.Azul, TipoVitoria.WazaAriAwaseteIppon);
                    }
                    if (EmGoldenScore)
                    {
                        return (true, CorJudogi.Azul, TipoVitoria.WazaAri);
                    }
                }
                break;

            case TipoPontuacao.Shido:
                if (judogi == CorJudogi.Branco)
                {
                    if (ShidoBranco < 3) ShidoBranco++;
                    if (ShidoBranco >= 3)
                    {
                        return (true, CorJudogi.Azul, TipoVitoria.HansokuMake);
                    }
                    if (EmGoldenScore)
                    {
                        // No Golden Score, qualquer Shido de diferença encerra imediatamente
                        return (true, CorJudogi.Azul, TipoVitoria.HansokuMake);
                    }
                }
                else
                {
                    if (ShidoAzul < 3) ShidoAzul++;
                    if (ShidoAzul >= 3)
                    {
                        return (true, CorJudogi.Branco, TipoVitoria.HansokuMake);
                    }
                    if (EmGoldenScore)
                    {
                        return (true, CorJudogi.Branco, TipoVitoria.HansokuMake);
                    }
                }
                break;

            case TipoPontuacao.CancelarWazaAri:
                if (judogi == CorJudogi.Branco && WazaAriBranco > 0)
                {
                    WazaAriBranco--;
                    IpponBranco = 0;
                }
                else if (judogi == CorJudogi.Azul && WazaAriAzul > 0)
                {
                    WazaAriAzul--;
                    IpponAzul = 0;
                }
                break;

            case TipoPontuacao.CancelarShido:
                if (judogi == CorJudogi.Branco && ShidoBranco > 0)
                {
                    ShidoBranco--;
                }
                else if (judogi == CorJudogi.Azul && ShidoAzul > 0)
                {
                    ShidoAzul--;
                }
                break;
        }

        return (false, null, null);
    }

    public void IniciarOsaekomi(CorJudogi cor)
    {
        OsaekomiAtivo = true;
        OsaekomiCor = cor;
        OsaekomiTempoSegundos = 0;
        UltimaAtualizacao = DateTime.UtcNow;
    }

    public (bool lutaEncerrada, CorJudogi? vencedor, TipoVitoria? tipoVitoria) AtualizarOsaekomi(int segundos)
    {
        OsaekomiTempoSegundos = segundos;
        UltimaAtualizacao = DateTime.UtcNow;

        if (!OsaekomiAtivo || OsaekomiCor == null)
            return (false, null, null);

        var judogi = OsaekomiCor.Value;
        bool temWazaAri = judogi == CorJudogi.Branco ? WazaAriBranco >= 1 : WazaAriAzul >= 1;

        // Se o atleta já tiver Waza-Ari, aos 10s ele alcança o 2º Waza-ari (Awasete Ippon)
        if (temWazaAri && segundos >= 10)
        {
            return AplicarPontuacao(judogi, TipoPontuacao.WazaAri);
        }

        // Sem Waza-ari prévio: 20s é Ippon direto
        if (segundos >= 20)
        {
            return AplicarPontuacao(judogi, TipoPontuacao.Ippon);
        }

        // Aos 10s sem Waza-ari prévio: concede Waza-ari
        if (segundos == 10)
        {
            AplicarPontuacao(judogi, TipoPontuacao.WazaAri);
        }

        return (false, null, null);
    }

    public void Toketa()
    {
        OsaekomiAtivo = false;
        OsaekomiCor = null;
        OsaekomiTempoSegundos = 0;
        UltimaAtualizacao = DateTime.UtcNow;
    }

    public void SetRawScores(int ipponB, int wazaariB, int shidoB, int ipponA, int wazaariA, int shidoA, bool osaekomi, CorJudogi? corOsae, int tempoOsae, int cronometro, bool goldenScore, bool rodando, bool sonoMama)
    {
        IpponBranco = ipponB;
        WazaAriBranco = wazaariB;
        ShidoBranco = shidoB;
        IpponAzul = ipponA;
        WazaAriAzul = wazaariA;
        ShidoAzul = shidoA;
        OsaekomiAtivo = osaekomi;
        OsaekomiCor = corOsae;
        OsaekomiTempoSegundos = tempoOsae;
        CronometroSegundos = cronometro;
        EmGoldenScore = goldenScore;
        CronometroRodando = rodando;
        SonoMama = sonoMama;
        UltimaAtualizacao = DateTime.UtcNow;
    }
}
