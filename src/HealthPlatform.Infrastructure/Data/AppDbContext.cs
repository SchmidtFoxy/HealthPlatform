using HealthPlatform.Domain.Common;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organizacao> Organizacoes => Set<Organizacao>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Anamnese> Anamneses => Set<Anamnese>();
    public DbSet<PerguntaAnamnese> PerguntasAnamnese => Set<PerguntaAnamnese>();
    public DbSet<RespostaAnamnesePersonalizada> RespostasAnamnesePersonalizadas => Set<RespostaAnamnesePersonalizada>();
    public DbSet<MarcadorLaboratorial> MarcadoresLaboratoriais => Set<MarcadorLaboratorial>();
    public DbSet<ExameLaboratorial> ExamesLaboratoriais => Set<ExameLaboratorial>();
    public DbSet<ResultadoExameLaboratorial> ResultadosExamesLaboratoriais => Set<ResultadoExameLaboratorial>();
    public DbSet<RelatorioClinico> RelatoriosClinicos => Set<RelatorioClinico>();
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<PlanoAlimentar> PlanosAlimentares => Set<PlanoAlimentar>();
    public DbSet<FaseNutricional> FasesNutricionais => Set<FaseNutricional>();
    public DbSet<ModeloRefeicao> ModelosRefeicoes => Set<ModeloRefeicao>();
    public DbSet<ModeloPlanoAlimentar> ModelosPlanosAlimentares => Set<ModeloPlanoAlimentar>();
    public DbSet<RefeicaoPlanoAlimentar> RefeicoesPlanoAlimentar => Set<RefeicaoPlanoAlimentar>();
    public DbSet<ItemRefeicaoPlano> ItensRefeicaoPlano => Set<ItemRefeicaoPlano>();
    public DbSet<SubstituicaoItemRefeicao> SubstituicoesItensRefeicao => Set<SubstituicaoItemRefeicao>();
    public DbSet<MetaPaciente> MetasPaciente => Set<MetaPaciente>();
    public DbSet<RegistroMeta> RegistrosMetas => Set<RegistroMeta>();
    public DbSet<RegistroDiarioPaciente> RegistrosDiarioPaciente => Set<RegistroDiarioPaciente>();
    public DbSet<Exercicio> Exercicios => Set<Exercicio>();
    public DbSet<PlanoTreino> PlanosTreino => Set<PlanoTreino>();
    public DbSet<FaseTreino> FasesTreino => Set<FaseTreino>();
    public DbSet<CheckInAcompanhamento> CheckInsAcompanhamento => Set<CheckInAcompanhamento>();
    public DbSet<RevisaoFase> RevisoesFases => Set<RevisaoFase>();
    public DbSet<ModeloSessaoTreino> ModelosSessoesTreino => Set<ModeloSessaoTreino>();
    public DbSet<ModeloPlanoTreino> ModelosPlanosTreino => Set<ModeloPlanoTreino>();
    public DbSet<SessaoTreino> SessoesTreino => Set<SessaoTreino>();
    public DbSet<ItemTreino> ItensTreino => Set<ItemTreino>();
    public DbSet<ExecucaoTreino> ExecucoesTreino => Set<ExecucaoTreino>();
    public DbSet<ExecucaoItemTreino> ExecucoesItensTreino => Set<ExecucaoItemTreino>();
    public DbSet<PendenciaClinica> PendenciasClinicas => Set<PendenciaClinica>();
    public DbSet<NotificacaoInterna> NotificacoesInternas => Set<NotificacaoInterna>();
    public DbSet<InteracaoAcompanhamento> InteracoesAcompanhamento => Set<InteracaoAcompanhamento>();
    public DbSet<EvolucaoClinica> EvolucoesClinicas => Set<EvolucaoClinica>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Organizacao>(entity =>
        {
            entity.ToTable("Organizacoes");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Cnpj).HasMaxLength(18);
            entity.HasIndex(x => x.Cnpj).IsUnique().HasFilter("\"Cnpj\" IS NOT NULL");
        });

        builder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => new { x.OrganizacaoId, x.Email });
        });

        builder.Entity<IdentityRole<Guid>>().ToTable("PerfisAcesso");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UsuariosPerfisAcesso");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UsuariosClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UsuariosLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("PerfisAcessoClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UsuariosTokens");

        builder.Entity<Profissional>(entity =>
        {
            entity.ToTable("Profissionais");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.RegistroProfissional).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Especialidade).HasMaxLength(120);
            entity.HasIndex(x => new { x.OrganizacaoId, x.RegistroProfissional }).IsUnique();
            entity.HasOne(x => x.Organizacao).WithMany(x => x.Profissionais)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Paciente>(entity =>
        {
            entity.ToTable("Pacientes");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Cpf).HasMaxLength(14);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Telefone).HasMaxLength(30);
            entity.HasIndex(x => new { x.OrganizacaoId, x.Cpf }).IsUnique().HasFilter("\"Cpf\" IS NOT NULL");
            entity.HasOne(x => x.Organizacao).WithMany(x => x.Pacientes)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Consulta>(entity =>
        {
            entity.ToTable("Consultas");
            entity.HasOne(x => x.Paciente).WithMany(x => x.Consultas)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.Consultas)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ProfissionalId, x.DataHoraUtc });
        });

        builder.Entity<Avaliacao>(entity =>
        {
            entity.ToTable("Avaliacoes");
            entity.HasOne(x => x.Paciente).WithMany(x => x.Avaliacoes)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Consulta).WithOne(x => x.Avaliacao)
                .HasForeignKey<Avaliacao>(x => x.ConsultaId).OnDelete(DeleteBehavior.SetNull);
        });


        builder.Entity<Anamnese>(entity =>
        {
            entity.ToTable("Anamneses");
            entity.Property(x => x.ObjetivoAcompanhamento).HasMaxLength(2000);
            entity.Property(x => x.Tabagismo).HasMaxLength(80);
            entity.Property(x => x.Etilismo).HasMaxLength(80);
            entity.Property(x => x.SonoQualidade).HasMaxLength(80);
            entity.HasIndex(x => new { x.PacienteId, x.DataUtc });
            entity.HasIndex(x => x.ConsultaId).IsUnique().HasFilter("\"ConsultaId\" IS NOT NULL");
            entity.HasOne(x => x.Paciente).WithMany(x => x.Anamneses)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.Anamneses)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Consulta).WithOne(x => x.Anamnese)
                .HasForeignKey<Anamnese>(x => x.ConsultaId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PerguntaAnamnese>(entity =>
        {
            entity.ToTable("PerguntasAnamnese");
            entity.Property(x => x.Texto).HasMaxLength(500).IsRequired();
            entity.Property(x => x.TipoResposta).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.OrganizacaoId, x.ProfissionalId, x.Ordem });
            entity.HasOne(x => x.Organizacao).WithMany(x => x.PerguntasAnamnese)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.PerguntasAnamnese)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RespostaAnamnesePersonalizada>(entity =>
        {
            entity.ToTable("RespostasAnamnesePersonalizadas");
            entity.HasIndex(x => new { x.AnamneseId, x.PerguntaAnamneseId }).IsUnique();
            entity.HasOne(x => x.Anamnese).WithMany(x => x.RespostasPersonalizadas)
                .HasForeignKey(x => x.AnamneseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PerguntaAnamnese).WithMany(x => x.Respostas)
                .HasForeignKey(x => x.PerguntaAnamneseId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MarcadorLaboratorial>(entity =>
        {
            entity.ToTable("MarcadoresLaboratoriais");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.NomeNormalizado).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(100);
            entity.Property(x => x.UnidadePadrao).HasMaxLength(50);
            entity.HasIndex(x => new { x.OrganizacaoId, x.NomeNormalizado }).IsUnique();
            entity.HasOne(x => x.Organizacao).WithMany(x => x.MarcadoresLaboratoriais)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExameLaboratorial>(entity =>
        {
            entity.ToTable("ExamesLaboratoriais");
            entity.Property(x => x.Laboratorio).HasMaxLength(160);
            entity.HasIndex(x => new { x.PacienteId, x.DataColetaUtc });
            entity.HasOne(x => x.Paciente).WithMany(x => x.ExamesLaboratoriais)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.ExamesLaboratoriais)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ResultadoExameLaboratorial>(entity =>
        {
            entity.ToTable("ResultadosExamesLaboratoriais");
            entity.Property(x => x.ValorTexto).HasMaxLength(300);
            entity.Property(x => x.Unidade).HasMaxLength(50);
            entity.Property(x => x.ReferenciaTexto).HasMaxLength(300);
            entity.HasIndex(x => new { x.ExameLaboratorialId, x.MarcadorLaboratorialId }).IsUnique();
            entity.HasIndex(x => x.MarcadorLaboratorialId);
            entity.HasOne(x => x.ExameLaboratorial).WithMany(x => x.Resultados)
                .HasForeignKey(x => x.ExameLaboratorialId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.MarcadorLaboratorial).WithMany(x => x.Resultados)
                .HasForeignKey(x => x.MarcadorLaboratorialId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RelatorioClinico>(entity =>
        {
            entity.ToTable("RelatoriosClinicos");
            entity.Property(x => x.Titulo).HasMaxLength(220).IsRequired();
            entity.Property(x => x.VersaoTemplate).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ConteudoJson).IsRequired();
            entity.HasIndex(x => new { x.PacienteId, x.DataGeracaoUtc });
            entity.HasOne(x => x.Paciente).WithMany(x => x.RelatoriosClinicos).HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.RelatoriosClinicos).HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Alimento>(entity =>
        {
            entity.ToTable("Alimentos");
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.NomeNormalizado).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(100);
            entity.HasIndex(x => new { x.OrganizacaoId, x.NomeNormalizado }).IsUnique();
            entity.HasOne(x => x.Organizacao).WithMany(x => x.Alimentos).HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ModeloPlanoAlimentar>(entity =>
        {
            entity.ToTable("ModelosPlanosAlimentares");
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(600);
            entity.Property(x => x.ConteudoJson).IsRequired();
            entity.HasIndex(x => new { x.OrganizacaoId, x.Ativo, x.Nome });
            entity.HasOne(x => x.Organizacao).WithMany(x => x.ModelosPlanosAlimentares)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.ModelosPlanosAlimentares)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ModeloRefeicao>(entity =>
        {
            entity.ToTable("ModelosRefeicoes");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(80);
            entity.Property(x => x.Descricao).HasMaxLength(600);
            entity.Property(x => x.ConteudoJson).IsRequired();
            entity.HasIndex(x => new { x.OrganizacaoId, x.Ativo, x.Nome });
            entity.HasIndex(x => new { x.OrganizacaoId, x.Categoria });
            entity.HasOne(x => x.Organizacao).WithMany(x => x.ModelosRefeicoes)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.ModelosRefeicoes)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FaseNutricional>(entity =>
        {
            entity.ToTable("FasesNutricionais");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Objetivo).HasMaxLength(600);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Observacoes).HasMaxLength(1200);
            entity.Property(x => x.MetaPesoKg).HasPrecision(8, 2);
            entity.Property(x => x.CriterioTransicao).HasMaxLength(1000);

            entity.HasIndex(x => new { x.PacienteId, x.Ordem });
            entity.HasIndex(x => new { x.OrganizacaoId, x.Status });
            entity.HasIndex(x => x.PlanoAlimentarId);

            entity.HasOne(x => x.Organizacao)
                .WithMany(x => x.FasesNutricionais)
                .HasForeignKey(x => x.OrganizacaoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Paciente)
                .WithMany(x => x.FasesNutricionais)
                .HasForeignKey(x => x.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Profissional)
                .WithMany(x => x.FasesNutricionais)
                .HasForeignKey(x => x.ProfissionalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PlanoAlimentar)
                .WithMany(x => x.FasesNutricionais)
                .HasForeignKey(x => x.PlanoAlimentarId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PlanoAlimentar>(entity =>
        {
            entity.ToTable("PlanosAlimentares");
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.MetaCalorias).HasPrecision(10, 2);
            entity.Property(x => x.MetaProteinasG).HasPrecision(10, 2);
            entity.Property(x => x.MetaCarboidratosG).HasPrecision(10, 2);
            entity.Property(x => x.MetaGordurasG).HasPrecision(10, 2);
            entity.Property(x => x.MetaFibrasG).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.PacienteId, x.DataInicio });
            entity.HasIndex(x => x.PlanoOrigemId);
            entity.HasOne(x => x.Paciente).WithMany(x => x.PlanosAlimentares).HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.PlanosAlimentares).HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PlanoOrigem).WithMany(x => x.VersoesDerivadas).HasForeignKey(x => x.PlanoOrigemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefeicaoPlanoAlimentar>(entity =>
        {
            entity.ToTable("RefeicoesPlanoAlimentar");
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.MetaCalorias).HasPrecision(10, 2);
            entity.Property(x => x.MetaProteinasG).HasPrecision(10, 2);
            entity.Property(x => x.MetaCarboidratosG).HasPrecision(10, 2);
            entity.Property(x => x.MetaGordurasG).HasPrecision(10, 2);
            entity.Property(x => x.MetaFibrasG).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.PlanoAlimentarId, x.Ordem });
            entity.HasOne(x => x.PlanoAlimentar).WithMany(x => x.Refeicoes).HasForeignKey(x => x.PlanoAlimentarId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ItemRefeicaoPlano>(entity =>
        {
            entity.ToTable("ItensRefeicaoPlano");
            entity.Property(x => x.Unidade).HasMaxLength(40).IsRequired();
            entity.HasOne(x => x.RefeicaoPlanoAlimentar).WithMany(x => x.Itens).HasForeignKey(x => x.RefeicaoPlanoAlimentarId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Alimento).WithMany(x => x.ItensRefeicao).HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SubstituicaoItemRefeicao>(entity =>
        {
            entity.ToTable("SubstituicoesItensRefeicao");
            entity.Property(x => x.Unidade).HasMaxLength(40).IsRequired();
            entity.HasOne(x => x.ItemRefeicaoPlano).WithMany(x => x.Substituicoes).HasForeignKey(x => x.ItemRefeicaoPlanoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Alimento).WithMany(x => x.Substituicoes).HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Restrict);
        });


        builder.Entity<MetaPaciente>(entity =>
        {
            entity.ToTable("MetasPaciente");
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Unidade).HasMaxLength(40);
            entity.Property(x => x.Frequencia).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.PacienteId, x.Status, x.DataInicio });
            entity.HasOne(x => x.Paciente).WithMany(x => x.Metas).HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.Metas).HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RegistroMeta>(entity =>
        {
            entity.ToTable("RegistrosMetas");
            entity.HasIndex(x => new { x.MetaPacienteId, x.Data }).IsUnique();
            entity.HasOne(x => x.MetaPaciente).WithMany(x => x.Registros).HasForeignKey(x => x.MetaPacienteId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RegistroDiarioPaciente>(entity =>
        {
            entity.ToTable("RegistrosDiarioPaciente");
            entity.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Unidade).HasMaxLength(40);
            entity.Property(x => x.ImagemUrl).HasMaxLength(1000);
            entity.HasIndex(x => new { x.PacienteId, x.DataHoraUtc });
            entity.HasOne(x => x.Paciente).WithMany(x => x.RegistrosDiario).HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
        });


        builder.Entity<Exercicio>(entity =>
        {
            entity.ToTable("Exercicios");
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.GrupoMuscular).HasMaxLength(100);
            entity.Property(x => x.Equipamento).HasMaxLength(120);
            entity.Property(x => x.VideoUrl).HasMaxLength(1000);
            entity.HasIndex(x => new { x.OrganizacaoId, x.Nome });
            entity.HasOne(x => x.Organizacao).WithMany(x => x.Exercicios)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ModeloPlanoTreino>(entity =>
        {
            entity.ToTable("ModelosPlanosTreino");
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(600);
            entity.Property(x => x.ConteudoJson).IsRequired();
            entity.HasIndex(x => new { x.OrganizacaoId, x.Ativo, x.Nome });
            entity.HasOne(x => x.Organizacao).WithMany(x => x.ModelosPlanosTreino)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.ModelosPlanosTreino)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ModeloSessaoTreino>(entity =>
        {
            entity.ToTable("ModelosSessoesTreino");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Categoria).HasMaxLength(80);
            entity.Property(x => x.Descricao).HasMaxLength(600);
            entity.Property(x => x.ConteudoJson).IsRequired();
            entity.HasIndex(x => new { x.OrganizacaoId, x.Ativo, x.Nome });
            entity.HasIndex(x => new { x.OrganizacaoId, x.Categoria });
            entity.HasOne(x => x.Organizacao).WithMany(x => x.ModelosSessoesTreino)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.ModelosSessoesTreino)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RevisaoFase>(entity =>
        {
            entity.ToTable("RevisoesFases");
            entity.Property(x => x.Dominio).HasMaxLength(20).IsRequired();
            entity.Property(x => x.FaseNome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.FaseDestinoNome).HasMaxLength(160);
            entity.Property(x => x.Decisao).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Justificativa).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.StatusAntes).HasMaxLength(30).IsRequired();
            entity.Property(x => x.StatusDepois).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CriterioProfissional).HasMaxLength(1000);

            entity.HasIndex(x => new { x.PacienteId, x.DataUtc });
            entity.HasIndex(x => new { x.OrganizacaoId, x.Dominio, x.DataUtc });
            entity.HasIndex(x => x.FaseId);

            entity.HasOne(x => x.Paciente)
                .WithMany(x => x.RevisoesFases)
                .HasForeignKey(x => x.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CheckInAcompanhamento>(entity =>
        {
            entity.ToTable("CheckInsAcompanhamento");
            entity.Property(x => x.PesoKg).HasPrecision(8, 2);
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.Property(x => x.Origem).HasMaxLength(30).IsRequired();

            entity.HasIndex(x => new { x.PacienteId, x.DataUtc });
            entity.HasIndex(x => new { x.OrganizacaoId, x.DataUtc });
            entity.HasIndex(x => x.FaseNutricionalId);
            entity.HasIndex(x => x.FaseTreinoId);

            entity.HasOne(x => x.Paciente)
                .WithMany(x => x.CheckInsAcompanhamento)
                .HasForeignKey(x => x.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.FaseNutricional)
                .WithMany()
                .HasForeignKey(x => x.FaseNutricionalId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.FaseTreino)
                .WithMany()
                .HasForeignKey(x => x.FaseTreinoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FaseTreino>(entity =>
        {
            entity.ToTable("FasesTreino");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Tipo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Objetivo).HasMaxLength(600);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Observacoes).HasMaxLength(1200);
            entity.Property(x => x.MetaPesoKg).HasPrecision(8, 2);
            entity.Property(x => x.CriterioTransicao).HasMaxLength(1000);
            entity.HasIndex(x => new { x.PacienteId, x.Ordem });
            entity.HasIndex(x => new { x.OrganizacaoId, x.Status });
            entity.HasIndex(x => x.PlanoTreinoId);
            entity.HasOne(x => x.Organizacao).WithMany(x => x.FasesTreino)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Paciente).WithMany(x => x.FasesTreino)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.FasesTreino)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PlanoTreino).WithMany(x => x.FasesTreino)
                .HasForeignKey(x => x.PlanoTreinoId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PlanoTreino>(entity =>
        {
            entity.ToTable("PlanosTreino");
            entity.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Objetivo).HasMaxLength(500);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.PacienteId, x.Status, x.DataInicio });
            entity.HasIndex(x => x.PlanoOrigemId);
            entity.HasOne(x => x.Paciente).WithMany(x => x.PlanosTreino)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.PlanosTreino)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PlanoOrigem).WithMany(x => x.VersoesDerivadas)
                .HasForeignKey(x => x.PlanoOrigemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SessaoTreino>(entity =>
        {
            entity.ToTable("SessoesTreino");
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.DiasSemana).HasMaxLength(120);
            entity.HasIndex(x => new { x.PlanoTreinoId, x.Ordem });
            entity.HasOne(x => x.PlanoTreino).WithMany(x => x.Sessoes)
                .HasForeignKey(x => x.PlanoTreinoId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ItemTreino>(entity =>
        {
            entity.ToTable("ItensTreino");
            entity.Property(x => x.Repeticoes).HasMaxLength(50).IsRequired();
            entity.Property(x => x.UnidadeCarga).HasMaxLength(20);
            entity.HasIndex(x => new { x.SessaoTreinoId, x.Ordem });
            entity.HasOne(x => x.SessaoTreino).WithMany(x => x.Itens)
                .HasForeignKey(x => x.SessaoTreinoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Exercicio).WithMany(x => x.ItensTreino)
                .HasForeignKey(x => x.ExercicioId).OnDelete(DeleteBehavior.Restrict);
        });


        builder.Entity<ExecucaoTreino>(entity =>
        {
            entity.ToTable("ExecucoesTreino");
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.PacienteId, x.DataHoraInicioUtc });
            entity.HasIndex(x => x.SessaoTreinoId);
            entity.HasOne(x => x.Paciente).WithMany(x => x.ExecucoesTreino)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PlanoTreino).WithMany()
                .HasForeignKey(x => x.PlanoTreinoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SessaoTreino).WithMany()
                .HasForeignKey(x => x.SessaoTreinoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExecucaoItemTreino>(entity =>
        {
            entity.ToTable("ExecucoesItensTreino");
            entity.Property(x => x.RepeticoesRealizadas).HasMaxLength(80);
            entity.Property(x => x.UnidadeCarga).HasMaxLength(20);
            entity.Property(x => x.Observacoes).HasMaxLength(1000);
            entity.HasIndex(x => x.ExecucaoTreinoId);
            entity.HasIndex(x => x.ItemTreinoId);
            entity.HasOne(x => x.ExecucaoTreino).WithMany(x => x.Itens)
                .HasForeignKey(x => x.ExecucaoTreinoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ItemTreino).WithMany(x => x.Execucoes)
                .HasForeignKey(x => x.ItemTreinoId).OnDelete(DeleteBehavior.Restrict);
        });


        builder.Entity<PendenciaClinica>(entity =>
        {
            entity.ToTable("PendenciasClinicas");
            entity.Property(x => x.OrigemCodigo).HasMaxLength(100);
            entity.Property(x => x.Categoria).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Severidade).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Titulo).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(3000);
            entity.Property(x => x.ValorReferencia).HasMaxLength(300);
            entity.Property(x => x.AcaoSugerida).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Resolucao).HasMaxLength(2000);

            entity.HasIndex(x => new { x.OrganizacaoId, x.Status, x.VencimentoUtc });
            entity.HasIndex(x => new { x.PacienteId, x.Status });
            entity.HasIndex(x => new { x.PacienteId, x.OrigemCodigo });

            entity.HasOne(x => x.Organizacao).WithMany(x => x.PendenciasClinicas)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Paciente).WithMany(x => x.PendenciasClinicas)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.PendenciasClinicas)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ConsultaRetorno).WithMany()
                .HasForeignKey(x => x.ConsultaRetornoId).OnDelete(DeleteBehavior.SetNull);
        });


        builder.Entity<NotificacaoInterna>(entity =>
        {
            entity.ToTable("NotificacoesInternas");
            entity.Property(x => x.Tipo).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Prioridade).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Titulo).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Mensagem).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.OrigemTipo).HasMaxLength(80);
            entity.Property(x => x.OrigemChave).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Link).HasMaxLength(500);

            entity.HasIndex(x => new { x.OrganizacaoId, x.UsuarioId, x.OrigemChave }).IsUnique();
            entity.HasIndex(x => new { x.UsuarioId, x.Ativa, x.LidaEmUtc });
            entity.HasIndex(x => new { x.UsuarioId, x.DataEventoUtc });

            entity.HasOne(x => x.Organizacao).WithMany(x => x.NotificacoesInternas)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
        });


        builder.Entity<InteracaoAcompanhamento>(entity =>
        {
            entity.ToTable("InteracoesAcompanhamento");
            entity.Property(x => x.Canal).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Resultado).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Observacoes).HasMaxLength(3000);
            entity.HasIndex(x => new { x.PacienteId, x.DataHoraUtc });
            entity.HasIndex(x => new { x.ProfissionalId, x.DataHoraUtc });
            entity.HasIndex(x => new { x.OrganizacaoId, x.ProximoContatoUtc });

            entity.HasOne(x => x.Organizacao).WithMany(x => x.InteracoesAcompanhamento)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Paciente).WithMany(x => x.InteracoesAcompanhamento)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.InteracoesAcompanhamento)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EvolucaoClinica>(entity =>
        {
            entity.ToTable("EvolucoesClinicas");
            entity.Property(x => x.Subjetivo).HasMaxLength(6000);
            entity.Property(x => x.Objetivo).HasMaxLength(6000);
            entity.Property(x => x.Avaliacao).HasMaxLength(6000);
            entity.Property(x => x.Plano).HasMaxLength(6000);
            entity.Property(x => x.Observacoes).HasMaxLength(3000);
            entity.HasIndex(x => new { x.PacienteId, x.DataHoraUtc });
            entity.HasIndex(x => new { x.ProfissionalId, x.DataHoraUtc });
            entity.HasIndex(x => x.ConsultaId);
            entity.HasOne(x => x.Organizacao).WithMany(x => x.EvolucoesClinicas)
                .HasForeignKey(x => x.OrganizacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Paciente).WithMany(x => x.EvolucoesClinicas)
                .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profissional).WithMany(x => x.EvolucoesClinicas)
                .HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Consulta).WithMany()
                .HasForeignKey(x => x.ConsultaId).OnDelete(DeleteBehavior.SetNull);
        });


        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property(x => x.Acao).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Entidade).HasMaxLength(160).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.HasIndex(x => x.CreatedAtUtc);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
