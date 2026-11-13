using Judoo.Domain.Enums;

namespace Judoo.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PerfilUsuario Role { get; set; } = PerfilUsuario.Publico;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class Academia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class Campeonato
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Status { get; set; } = "RASCUNHO";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Tatame> Tatames { get; set; } = new List<Tatame>();
    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
}

public class Tatame
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampeonatoId { get; set; }
    public int Numero { get; set; }
    public string Nome { get; set; } = string.Empty;
    public StatusTatame Status { get; set; } = StatusTatame.Disponivel;
    public Guid? LutaAtualId { get; set; }
    public Guid? MesarioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Campeonato? Campeonato { get; set; }
    public Usuario? Mesario { get; set; }
    public Luta? LutaAtual { get; set; }
}

public class Categoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampeonatoId { get; set; }
    public string Classe { get; set; } = string.Empty;
    public string DivisaoPeso { get; set; } = string.Empty;
    public decimal PesoLimiteKg { get; set; }
    public string Sexo { get; set; } = "MASCULINO";
    public int TempoLutaSegundos { get; set; } = 240;
    public int TempoOsaekomiWazaari { get; set; } = 10;
    public int TempoOsaekomiIppon { get; set; } = 20;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Campeonato? Campeonato { get; set; }
}

public class Atleta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademiaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? RegistroFederacao { get; set; }
    public DateTime DataNascimento { get; set; }
    public string Sexo { get; set; } = "MASCULINO";
    public string? FotoUrl { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Academia? Academia { get; set; }
    public string? NomeAcademia { get; set; }
    public string? SiglaAcademia { get; set; }
}

public class Pesagem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampeonatoId { get; set; }
    public Guid AtletaId { get; set; }
    public Guid CategoriaId { get; set; }
    public decimal PesoAferidoKg { get; set; }
    public string Status { get; set; } = "PENDENTE";
    public DateTime DataPesagem { get; set; } = DateTime.UtcNow;
    public Guid? AferidoPorUsuarioId { get; set; }
    public string? Observacoes { get; set; }

    public Campeonato? Campeonato { get; set; }
    public Atleta? Atleta { get; set; }
    public Categoria? Categoria { get; set; }
    public Usuario? AferidoPorUsuario { get; set; }
}

public class Chave
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampeonatoId { get; set; }
    public Guid CategoriaId { get; set; }
    public string Tipo { get; set; } = "ELIMINATORIA_SIMPLES";
    public int TotalFases { get; set; } = 1;
    public bool Gerada { get; set; } = false;
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    public Campeonato? Campeonato { get; set; }
    public Categoria? Categoria { get; set; }
    public ICollection<Luta> Lutas { get; set; } = new List<Luta>();
}

public class Luta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChaveId { get; set; }
    public Guid? TatameId { get; set; }
    public Guid? AtletaBrancoId { get; set; }
    public Guid? AtletaAzulId { get; set; }
    public Guid? VencedorId { get; set; }
    public int Fase { get; set; } = 1;
    public int OrdemNaChave { get; set; } = 1;
    public int? OrdemNoTatame { get; set; }
    public StatusLuta Status { get; set; } = StatusLuta.Agendada;
    public TipoVitoria? TipoVitoria { get; set; }
    public bool EmGoldenScore { get; set; }
    public int TempoLutaSegundos { get; set; }
    public DateTime? HoraInicio { get; set; }
    public DateTime? HoraFim { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public Chave? Chave { get; set; }
    public Tatame? Tatame { get; set; }
    public Atleta? AtletaBranco { get; set; }
    public Atleta? AtletaAzul { get; set; }
    public Atleta? Vencedor { get; set; }
    public PlacarLuta? Placar { get; set; }
    public ICollection<EventoPontuacao> Eventos { get; set; } = new List<EventoPontuacao>();
}

public class EventoPontuacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LutaId { get; set; }
    public Guid? AtletaId { get; set; }
    public CorJudogi CorJudogi { get; set; }
    public TipoPontuacao Tipo { get; set; }
    public int TempoLutaSegundos { get; set; }
    public string? Descricao { get; set; }
    public Guid? RegistradoPorUsuarioId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public Luta? Luta { get; set; }
    public Atleta? Atleta { get; set; }
    public Usuario? RegistradoPorUsuario { get; set; }
}
