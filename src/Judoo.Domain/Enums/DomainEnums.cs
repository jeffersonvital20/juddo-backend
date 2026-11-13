namespace Judoo.Domain.Enums;

public enum PerfilUsuario
{
    Admin,
    Secretaria,
    Mesario,
    Publico
}

public enum CorJudogi
{
    Branco,
    Azul
}

public enum TipoPontuacao
{
    WazaAri,
    Ippon,
    Shido,
    CancelarWazaAri,
    CancelarShido
}

public enum TipoVitoria
{
    Ippon,
    WazaAriAwaseteIppon,
    WazaAri,
    HansokuMake,
    Decisao,
    WO,
    Desistencia
}

public enum StatusLuta
{
    Agendada,
    Chamada,
    EmAndamento,
    Pausada,
    Finalizada,
    Cancelada
}

public enum StatusTatame
{
    Disponivel,
    EmLuta,
    Pausado,
    Manutencao
}
