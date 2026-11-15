using Judoo.Domain.Entities;
using Judoo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Judoo.Infrastructure.Data;

public class JudooDbContext : DbContext
{
    public JudooDbContext(DbContextOptions<JudooDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Campeonato> Campeonatos => Set<Campeonato>();
    public DbSet<Tatame> Tatames => Set<Tatame>();
    public DbSet<Academia> Academias => Set<Academia>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Atleta> Atletas => Set<Atleta>();
    public DbSet<Pesagem> Pesagens => Set<Pesagem>();
    public DbSet<Chave> Chaves => Set<Chave>();
    public DbSet<Luta> Lutas => Set<Luta>();
    public DbSet<PlacarLuta> PlacarLutas => Set<PlacarLuta>();
    public DbSet<EventoPontuacao> EventosLuta => Set<EventoPontuacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Usuarios
        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("usuarios");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            b.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255).IsRequired();
            b.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(30).IsRequired();
            b.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true);
            b.Property(x => x.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("NOW()");
        });

        // 2. Campeonatos
        modelBuilder.Entity<Campeonato>(b =>
        {
            b.ToTable("campeonatos");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
            b.Property(x => x.Local).HasColumnName("local").HasMaxLength(255).IsRequired();
            b.Property(x => x.DataInicio).HasColumnName("data_inicio").IsRequired();
            b.Property(x => x.DataFim).HasColumnName("data_fim").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("RASCUNHO");
            b.Property(x => x.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("NOW()");
        });

        // 3. Tatames
        modelBuilder.Entity<Tatame>(b =>
        {
            b.ToTable("tatames");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.CampeonatoId).HasColumnName("campeonato_id").IsRequired();
            b.Property(x => x.Numero).HasColumnName("numero").IsRequired();
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).HasDefaultValue(StatusTatame.Disponivel);
            b.Property(x => x.LutaAtualId).HasColumnName("luta_atual_id");
            b.Property(x => x.MesarioId).HasColumnName("mesario_id");
            b.Property(x => x.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("NOW()");

            b.HasOne(x => x.Campeonato)
             .WithMany(c => c.Tatames)
             .HasForeignKey(x => x.CampeonatoId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Mesario)
             .WithMany()
             .HasForeignKey(x => x.MesarioId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.LutaAtual)
             .WithMany()
             .HasForeignKey(x => x.LutaAtualId)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.CampeonatoId, x.Numero }).IsUnique();
        });

        // 4. Academias
        modelBuilder.Entity<Academia>(b =>
        {
            b.ToTable("academias");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            b.Property(x => x.Sigla).HasColumnName("sigla").HasMaxLength(10).IsRequired();
            b.Property(x => x.Cidade).HasColumnName("cidade").HasMaxLength(100);
            b.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(2);
            b.Property(x => x.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("NOW()");
        });

        // 5. Categorias
        modelBuilder.Entity<Categoria>(b =>
        {
            b.ToTable("categorias");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.CampeonatoId).HasColumnName("campeonato_id").IsRequired();
            b.Property(x => x.Classe).HasColumnName("classe").HasMaxLength(50).IsRequired();
            b.Property(x => x.DivisaoPeso).HasColumnName("divisao_peso").HasMaxLength(30).IsRequired();
            b.Property(x => x.PesoLimiteKg).HasColumnName("peso_limite_kg").HasPrecision(5, 2).IsRequired();
            b.Property(x => x.Sexo).HasColumnName("sexo").HasMaxLength(20).IsRequired();
            b.Property(x => x.TempoLutaSegundos).HasColumnName("tempo_luta_segundos").HasDefaultValue(240);
            b.Property(x => x.TempoOsaekomiWazaari).HasColumnName("tempo_osaekomi_wazaari").HasDefaultValue(10);
            b.Property(x => x.TempoOsaekomiIppon).HasColumnName("tempo_osaekomi_ippon").HasDefaultValue(20);
            b.Property(x => x.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("NOW()");

            b.HasOne(x => x.Campeonato)
             .WithMany(c => c.Categorias)
             .HasForeignKey(x => x.CampeonatoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. Atletas
        modelBuilder.Entity<Atleta>(b =>
        {
            b.ToTable("atletas");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.AcademiaId).HasColumnName("academia_id").IsRequired();
            b.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            b.Property(x => x.RegistroFederacao).HasColumnName("registro_federacao").HasMaxLength(50);
            b.Property(x => x.DataNascimento).HasColumnName("data_nascimento").IsRequired();
            b.Property(x => x.Sexo).HasColumnName("sexo").HasMaxLength(20).IsRequired();
            b.Property(x => x.FotoUrl).HasColumnName("foto_url").HasMaxLength(500);
            b.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true);
            b.Property(x => x.CriadoEm).HasColumnName("criado_em").HasDefaultValueSql("NOW()");

            b.Ignore(x => x.NomeAcademia);
            b.Ignore(x => x.SiglaAcademia);

            b.HasOne(x => x.Academia)
             .WithMany()
             .HasForeignKey(x => x.AcademiaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. Pesagens
        modelBuilder.Entity<Pesagem>(b =>
        {
            b.ToTable("pesagens");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.CampeonatoId).HasColumnName("campeonato_id").IsRequired();
            b.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
            b.Property(x => x.CategoriaId).HasColumnName("categoria_id").IsRequired();
            b.Property(x => x.PesoAferidoKg).HasColumnName("peso_aferido_kg").HasPrecision(5, 2).IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("PENDENTE");
            b.Property(x => x.DataPesagem).HasColumnName("data_pesagem").HasDefaultValueSql("NOW()");
            b.Property(x => x.AferidoPorUsuarioId).HasColumnName("aferido_por_usuario_id");
            b.Property(x => x.Observacoes).HasColumnName("observacoes");

            b.HasOne(x => x.Campeonato).WithMany().HasForeignKey(x => x.CampeonatoId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Atleta).WithMany().HasForeignKey(x => x.AtletaId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.AferidoPorUsuario).WithMany().HasForeignKey(x => x.AferidoPorUsuarioId).OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.CampeonatoId, x.AtletaId }).IsUnique();
        });

        // 8. Chaves
        modelBuilder.Entity<Chave>(b =>
        {
            b.ToTable("chaves");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.CampeonatoId).HasColumnName("campeonato_id").IsRequired();
            b.Property(x => x.CategoriaId).HasColumnName("categoria_id").IsRequired();
            b.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(30).HasDefaultValue("ELIMINATORIA_SIMPLES");
            b.Property(x => x.TotalFases).HasColumnName("total_fases").HasDefaultValue(1);
            b.Property(x => x.Gerada).HasColumnName("gerada").HasDefaultValue(false);
            b.Property(x => x.CriadaEm).HasColumnName("criada_em").HasDefaultValueSql("NOW()");

            b.HasOne(x => x.Campeonato).WithMany().HasForeignKey(x => x.CampeonatoId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Cascade);
        });

        // 9. Lutas
        modelBuilder.Entity<Luta>(b =>
        {
            b.ToTable("lutas");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ChaveId).HasColumnName("chave_id").IsRequired();
            b.Property(x => x.TatameId).HasColumnName("tatame_id");
            b.Property(x => x.AtletaBrancoId).HasColumnName("atleta_branco_id");
            b.Property(x => x.AtletaAzulId).HasColumnName("atleta_azul_id");
            b.Property(x => x.VencedorId).HasColumnName("vencedor_id");
            b.Property(x => x.Fase).HasColumnName("fase").HasDefaultValue(1);
            b.Property(x => x.OrdemNaChave).HasColumnName("ordem_na_chave").HasDefaultValue(1);
            b.Property(x => x.OrdemNoTatame).HasColumnName("ordem_no_tatame");
            b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).HasDefaultValue(StatusLuta.Agendada);
            b.Property(x => x.TipoVitoria).HasColumnName("tipo_vitoria").HasConversion<string>().HasMaxLength(40);
            b.Property(x => x.EmGoldenScore).HasColumnName("em_golden_score").HasDefaultValue(false);
            b.Property(x => x.TempoLutaSegundos).HasColumnName("tempo_luta_segundos").HasDefaultValue(0);
            b.Property(x => x.HoraInicio).HasColumnName("hora_inicio");
            b.Property(x => x.HoraFim).HasColumnName("hora_fim");
            b.Property(x => x.CriadaEm).HasColumnName("criada_em").HasDefaultValueSql("NOW()");

            b.HasOne(x => x.Chave).WithMany(c => c.Lutas).HasForeignKey(x => x.ChaveId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Tatame).WithMany().HasForeignKey(x => x.TatameId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.AtletaBranco).WithMany().HasForeignKey(x => x.AtletaBrancoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.AtletaAzul).WithMany().HasForeignKey(x => x.AtletaAzulId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Vencedor).WithMany().HasForeignKey(x => x.VencedorId).OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.TatameId, x.Status });
        });

        // 10. PlacarLutas
        modelBuilder.Entity<PlacarLuta>(b =>
        {
            b.ToTable("placar_lutas");
            b.HasKey(x => x.LutaId);
            b.Property(x => x.LutaId).HasColumnName("luta_id");
            b.Property(x => x.IpponBranco).HasColumnName("ippon_branco").HasDefaultValue(0);
            b.Property(x => x.WazaAriBranco).HasColumnName("wazaari_branco").HasDefaultValue(0);
            b.Property(x => x.YukoBranco).HasColumnName("yuko_branco").HasDefaultValue(0);
            b.Property(x => x.ShidoBranco).HasColumnName("shido_branco").HasDefaultValue(0);
            b.Property(x => x.IpponAzul).HasColumnName("ippon_azul").HasDefaultValue(0);
            b.Property(x => x.WazaAriAzul).HasColumnName("wazaari_azul").HasDefaultValue(0);
            b.Property(x => x.YukoAzul).HasColumnName("yuko_azul").HasDefaultValue(0);
            b.Property(x => x.ShidoAzul).HasColumnName("shido_azul").HasDefaultValue(0);
            b.Property(x => x.OsaekomiAtivo).HasColumnName("osaekomi_ativo").HasDefaultValue(false);
            b.Property(x => x.OsaekomiCor).HasColumnName("osaekomi_cor").HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.OsaekomiTempoSegundos).HasColumnName("osaekomi_tempo_segundos").HasDefaultValue(0);
            b.Property(x => x.CronometroSegundos).HasColumnName("cronometro_segundos").HasDefaultValue(240);
            b.Property(x => x.EmGoldenScore).HasColumnName("em_golden_score").HasDefaultValue(false);
            b.Property(x => x.CronometroRodando).HasColumnName("cronometro_rodando").HasDefaultValue(false);
            b.Property(x => x.SonoMama).HasColumnName("sono_mama").HasDefaultValue(false);
            b.Property(x => x.UltimaAtualizacao).HasColumnName("ultima_atualizacao").HasDefaultValueSql("NOW()");

            b.HasOne(x => x.Luta)
             .WithOne(l => l.Placar)
             .HasForeignKey<PlacarLuta>(x => x.LutaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // 11. EventosLuta
        modelBuilder.Entity<EventoPontuacao>(b =>
        {
            b.ToTable("eventos_luta");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.LutaId).HasColumnName("luta_id").IsRequired();
            b.Property(x => x.AtletaId).HasColumnName("atleta_id");
            b.Property(x => x.CorJudogi).HasColumnName("cor_judogi").HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(30).IsRequired();
            b.Property(x => x.TempoLutaSegundos).HasColumnName("tempo_luta_segundos").IsRequired();
            b.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(255);
            b.Property(x => x.RegistradoPorUsuarioId).HasColumnName("registrado_por_usuario_id");
            b.Property(x => x.TimestampUtc).HasColumnName("timestamp_utc").HasDefaultValueSql("NOW()");

            b.HasOne(x => x.Luta).WithMany(l => l.Eventos).HasForeignKey(x => x.LutaId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Atleta).WithMany().HasForeignKey(x => x.AtletaId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.RegistradoPorUsuario).WithMany().HasForeignKey(x => x.RegistradoPorUsuarioId).OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => new { x.LutaId, x.TimestampUtc });
        });
    }
}
