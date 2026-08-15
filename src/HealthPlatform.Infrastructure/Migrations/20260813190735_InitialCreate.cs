using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Acao = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Entidade = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EntidadeId = table.Column<string>(type: "text", nullable: true),
                    DadosAnterioresJson = table.Column<string>(type: "text", nullable: true),
                    DadosNovosJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfisAcesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisAcesso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TipoUsuario = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alimentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CaloriasPor100g = table.Column<decimal>(type: "numeric", nullable: false),
                    ProteinasPor100g = table.Column<decimal>(type: "numeric", nullable: false),
                    CarboidratosPor100g = table.Column<decimal>(type: "numeric", nullable: false),
                    GordurasPor100g = table.Column<decimal>(type: "numeric", nullable: false),
                    FibrasPor100g = table.Column<decimal>(type: "numeric", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alimentos_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exercicios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    GrupoMuscular = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Equipamento = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercicios_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarcadoresLaboratoriais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnidadePadrao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarcadoresLaboratoriais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarcadoresLaboratoriais_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificacoesInternas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Prioridade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OrigemTipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    OrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrigemChave = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    DataEventoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LidaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacoesInternas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacoesInternas_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Profissao = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pacientes_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RegistroProfissional = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Especialidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profissionais_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerfisAcessoClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisAcessoClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfisAcessoClaims_PerfisAcesso_RoleId",
                        column: x => x.RoleId,
                        principalTable: "PerfisAcesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosClaims_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UsuariosLogins_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosPerfisAcesso",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosPerfisAcesso", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UsuariosPerfisAcesso_PerfisAcesso_RoleId",
                        column: x => x.RoleId,
                        principalTable: "PerfisAcesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosPerfisAcesso_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UsuariosTokens_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosDiarioPaciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHoraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    ValorNumerico = table.Column<decimal>(type: "numeric", nullable: true),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Escala = table.Column<int>(type: "integer", nullable: true),
                    ImagemUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosDiarioPaciente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosDiarioPaciente_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RevisoesFases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dominio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FaseNome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FaseDestinoId = table.Column<Guid>(type: "uuid", nullable: true),
                    FaseDestinoNome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Decisao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Justificativa = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DataUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusAntes = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StatusDepois = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CriteriosConfigurados = table.Column<int>(type: "integer", nullable: false),
                    CriteriosAtendidos = table.Column<int>(type: "integer", nullable: false),
                    ObjetivosProntosParaRevisao = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideCriterios = table.Column<bool>(type: "boolean", nullable: false),
                    CriterioProfissional = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SnapshotIndicadoresJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisoesFases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisoesFases_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Consultas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHoraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    QueixaPrincipal = table.Column<string>(type: "text", nullable: true),
                    Evolucao = table.Column<string>(type: "text", nullable: true),
                    Conduta = table.Column<string>(type: "text", nullable: true),
                    Orientacoes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consultas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consultas_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamesLaboratoriais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataColetaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Laboratorio = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamesLaboratoriais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamesLaboratoriais_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamesLaboratoriais_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InteracoesAcompanhamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHoraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Canal = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Resultado = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ProximoContatoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteracoesAcompanhamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InteracoesAcompanhamento_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InteracoesAcompanhamento_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InteracoesAcompanhamento_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MetasPaciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ValorObjetivo = table.Column<decimal>(type: "numeric", nullable: true),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Frequencia = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasPaciente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetasPaciente_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetasPaciente_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelosPlanosAlimentares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ConteudoJson = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosPlanosAlimentares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelosPlanosAlimentares_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelosPlanosAlimentares_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelosPlanosTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ConteudoJson = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosPlanosTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelosPlanosTreino_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelosPlanosTreino_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelosRefeicoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ConteudoJson = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosRefeicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelosRefeicoes_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelosRefeicoes_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelosSessoesTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ConteudoJson = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosSessoesTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelosSessoesTreino_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelosSessoesTreino_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerguntasAnamnese",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TipoResposta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OpcoesJson = table.Column<string>(type: "text", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerguntasAnamnese", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerguntasAnamnese_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerguntasAnamnese_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanosAlimentares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    PlanoOrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    AjustePercentual = table.Column<decimal>(type: "numeric", nullable: false),
                    MetaCalorias = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaProteinasG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaCarboidratosG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaGordurasG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaFibrasG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosAlimentares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosAlimentares_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanosAlimentares_PlanosAlimentares_PlanoOrigemId",
                        column: x => x.PlanoOrigemId,
                        principalTable: "PlanosAlimentares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanosAlimentares_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanosTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    PlanoOrigemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Versao = table.Column<int>(type: "integer", nullable: false),
                    AjusteCargaPercentual = table.Column<decimal>(type: "numeric", nullable: false),
                    AjusteSeries = table.Column<int>(type: "integer", nullable: false),
                    AjusteRepeticoes = table.Column<int>(type: "integer", nullable: false),
                    AjusteDescansoSegundos = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanosTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanosTreino_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanosTreino_PlanosTreino_PlanoOrigemId",
                        column: x => x.PlanoOrigemId,
                        principalTable: "PlanosTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanosTreino_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RelatoriosClinicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataFimUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataGeracaoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    ConclusaoMedica = table.Column<string>(type: "text", nullable: true),
                    VersaoTemplate = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ConteudoJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatoriosClinicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatoriosClinicos_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelatoriosClinicos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Anamneses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultaId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ObjetivoAcompanhamento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HistoricoDoencas = table.Column<string>(type: "text", nullable: true),
                    HistoricoFamiliar = table.Column<string>(type: "text", nullable: true),
                    Cirurgias = table.Column<string>(type: "text", nullable: true),
                    Alergias = table.Column<string>(type: "text", nullable: true),
                    Medicamentos = table.Column<string>(type: "text", nullable: true),
                    Suplementos = table.Column<string>(type: "text", nullable: true),
                    Tabagismo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Etilismo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SonoHorasMedia = table.Column<decimal>(type: "numeric", nullable: true),
                    SonoQualidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DespertaDuranteNoite = table.Column<bool>(type: "boolean", nullable: true),
                    EstresseNivel = table.Column<int>(type: "integer", nullable: true),
                    AtividadeFisica = table.Column<string>(type: "text", nullable: true),
                    AtividadeFisicaDiasSemana = table.Column<int>(type: "integer", nullable: true),
                    HabitoIntestinal = table.Column<string>(type: "text", nullable: true),
                    AguaLitrosDia = table.Column<decimal>(type: "numeric", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anamneses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Anamneses_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Anamneses_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Anamneses_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Avaliacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultaId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PesoKg = table.Column<decimal>(type: "numeric", nullable: true),
                    AlturaM = table.Column<decimal>(type: "numeric", nullable: true),
                    PercentualGordura = table.Column<decimal>(type: "numeric", nullable: true),
                    MassaMagraKg = table.Column<decimal>(type: "numeric", nullable: true),
                    MassaGordaKg = table.Column<decimal>(type: "numeric", nullable: true),
                    CinturaCm = table.Column<decimal>(type: "numeric", nullable: true),
                    AbdomenCm = table.Column<decimal>(type: "numeric", nullable: true),
                    QuadrilCm = table.Column<decimal>(type: "numeric", nullable: true),
                    PressaoSistolica = table.Column<int>(type: "integer", nullable: true),
                    PressaoDiastolica = table.Column<int>(type: "integer", nullable: true),
                    FrequenciaCardiaca = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avaliacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvolucoesClinicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultaId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataHoraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Subjetivo = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    Objetivo = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    Avaliacao = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    Plano = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvolucoesClinicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvolucoesClinicas_Consultas_ConsultaId",
                        column: x => x.ConsultaId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EvolucoesClinicas_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvolucoesClinicas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvolucoesClinicas_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PendenciasClinicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrigemCodigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Severidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ValorReferencia = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AcaoSugerida = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    VencimentoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VistaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdiadaAteUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvidaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Resolucao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConsultaRetornoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendenciasClinicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendenciasClinicas_Consultas_ConsultaRetornoId",
                        column: x => x.ConsultaRetornoId,
                        principalTable: "Consultas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PendenciasClinicas_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendenciasClinicas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendenciasClinicas_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultadosExamesLaboratoriais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExameLaboratorialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarcadorLaboratorialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValorNumerico = table.Column<decimal>(type: "numeric", nullable: true),
                    ValorTexto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Unidade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReferenciaMinima = table.Column<decimal>(type: "numeric", nullable: true),
                    ReferenciaMaxima = table.Column<decimal>(type: "numeric", nullable: true),
                    ReferenciaTexto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosExamesLaboratoriais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadosExamesLaboratoriais_ExamesLaboratoriais_ExameLabo~",
                        column: x => x.ExameLaboratorialId,
                        principalTable: "ExamesLaboratoriais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultadosExamesLaboratoriais_MarcadoresLaboratoriais_Marca~",
                        column: x => x.MarcadorLaboratorialId,
                        principalTable: "MarcadoresLaboratoriais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosMetas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaPacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric", nullable: true),
                    Concluida = table.Column<bool>(type: "boolean", nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosMetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosMetas_MetasPaciente_MetaPacienteId",
                        column: x => x.MetaPacienteId,
                        principalTable: "MetasPaciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FasesNutricionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoAlimentarId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    MetaPesoKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    MetaAdesaoPercentual = table.Column<int>(type: "integer", nullable: true),
                    DuracaoMinimaDias = table.Column<int>(type: "integer", nullable: true),
                    CriterioTransicao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FasesNutricionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FasesNutricionais_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FasesNutricionais_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FasesNutricionais_PlanosAlimentares_PlanoAlimentarId",
                        column: x => x.PlanoAlimentarId,
                        principalTable: "PlanosAlimentares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FasesNutricionais_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefeicoesPlanoAlimentar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoAlimentarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Horario = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    MetaCalorias = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaProteinasG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaCarboidratosG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaGordurasG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MetaFibrasG = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefeicoesPlanoAlimentar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefeicoesPlanoAlimentar_PlanosAlimentares_PlanoAlimentarId",
                        column: x => x.PlanoAlimentarId,
                        principalTable: "PlanosAlimentares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FasesTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoTreinoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    MetaPesoKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    MetaAdesaoPercentual = table.Column<int>(type: "integer", nullable: true),
                    DuracaoMinimaDias = table.Column<int>(type: "integer", nullable: true),
                    CriterioTransicao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FasesTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FasesTreino_Organizacoes_OrganizacaoId",
                        column: x => x.OrganizacaoId,
                        principalTable: "Organizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FasesTreino_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FasesTreino_PlanosTreino_PlanoTreinoId",
                        column: x => x.PlanoTreinoId,
                        principalTable: "PlanosTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FasesTreino_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessoesTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoTreinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DiasSemana = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesTreino_PlanosTreino_PlanoTreinoId",
                        column: x => x.PlanoTreinoId,
                        principalTable: "PlanosTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RespostasAnamnesePersonalizadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnamneseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerguntaAnamneseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Resposta = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostasAnamnesePersonalizadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespostasAnamnesePersonalizadas_Anamneses_AnamneseId",
                        column: x => x.AnamneseId,
                        principalTable: "Anamneses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespostasAnamnesePersonalizadas_PerguntasAnamnese_PerguntaA~",
                        column: x => x.PerguntaAnamneseId,
                        principalTable: "PerguntasAnamnese",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensRefeicaoPlano",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefeicaoPlanoAlimentarId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlimentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    QuantidadeGramas = table.Column<decimal>(type: "numeric", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensRefeicaoPlano", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensRefeicaoPlano_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensRefeicaoPlano_RefeicoesPlanoAlimentar_RefeicaoPlanoAli~",
                        column: x => x.RefeicaoPlanoAlimentarId,
                        principalTable: "RefeicoesPlanoAlimentar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheckInsAcompanhamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    FaseNutricionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    FaseTreinoId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegistradoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PesoKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    AdesaoAlimentacaoPercentual = table.Column<int>(type: "integer", nullable: true),
                    AdesaoTreinoPercentual = table.Column<int>(type: "integer", nullable: true),
                    FomeNivel = table.Column<int>(type: "integer", nullable: true),
                    EnergiaNivel = table.Column<int>(type: "integer", nullable: true),
                    SonoNivel = table.Column<int>(type: "integer", nullable: true),
                    PercepcaoEvolucaoNivel = table.Column<int>(type: "integer", nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Origem = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInsAcompanhamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInsAcompanhamento_FasesNutricionais_FaseNutricionalId",
                        column: x => x.FaseNutricionalId,
                        principalTable: "FasesNutricionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CheckInsAcompanhamento_FasesTreino_FaseTreinoId",
                        column: x => x.FaseTreinoId,
                        principalTable: "FasesTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CheckInsAcompanhamento_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecucoesTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanoTreinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessaoTreinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHoraInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataHoraFimUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DuracaoMinutos = table.Column<int>(type: "integer", nullable: true),
                    EsforcoPercebido = table.Column<int>(type: "integer", nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucoesTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecucoesTreino_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecucoesTreino_PlanosTreino_PlanoTreinoId",
                        column: x => x.PlanoTreinoId,
                        principalTable: "PlanosTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecucoesTreino_SessoesTreino_SessaoTreinoId",
                        column: x => x.SessaoTreinoId,
                        principalTable: "SessoesTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessaoTreinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Series = table.Column<int>(type: "integer", nullable: false),
                    Repeticoes = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Carga = table.Column<decimal>(type: "numeric", nullable: true),
                    UnidadeCarga = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DescansoSegundos = table.Column<int>(type: "integer", nullable: true),
                    TempoSegundos = table.Column<int>(type: "integer", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensTreino_Exercicios_ExercicioId",
                        column: x => x.ExercicioId,
                        principalTable: "Exercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensTreino_SessoesTreino_SessaoTreinoId",
                        column: x => x.SessaoTreinoId,
                        principalTable: "SessoesTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubstituicoesItensRefeicao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemRefeicaoPlanoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlimentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    QuantidadeGramas = table.Column<decimal>(type: "numeric", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstituicoesItensRefeicao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubstituicoesItensRefeicao_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubstituicoesItensRefeicao_ItensRefeicaoPlano_ItemRefeicaoP~",
                        column: x => x.ItemRefeicaoPlanoId,
                        principalTable: "ItensRefeicaoPlano",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecucoesItensTreino",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecucaoTreinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemTreinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesRealizadas = table.Column<int>(type: "integer", nullable: true),
                    RepeticoesRealizadas = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CargaRealizada = table.Column<decimal>(type: "numeric", nullable: true),
                    UnidadeCarga = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EsforcoPercebido = table.Column<int>(type: "integer", nullable: true),
                    Concluido = table.Column<bool>(type: "boolean", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucoesItensTreino", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecucoesItensTreino_ExecucoesTreino_ExecucaoTreinoId",
                        column: x => x.ExecucaoTreinoId,
                        principalTable: "ExecucoesTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecucoesItensTreino_ItensTreino_ItemTreinoId",
                        column: x => x.ItemTreinoId,
                        principalTable: "ItensTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_OrganizacaoId_NomeNormalizado",
                table: "Alimentos",
                columns: new[] { "OrganizacaoId", "NomeNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Anamneses_ConsultaId",
                table: "Anamneses",
                column: "ConsultaId",
                unique: true,
                filter: "\"ConsultaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Anamneses_PacienteId_DataUtc",
                table: "Anamneses",
                columns: new[] { "PacienteId", "DataUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Anamneses_ProfissionalId",
                table: "Anamneses",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_ConsultaId",
                table: "Avaliacoes",
                column: "ConsultaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_PacienteId",
                table: "Avaliacoes",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInsAcompanhamento_FaseNutricionalId",
                table: "CheckInsAcompanhamento",
                column: "FaseNutricionalId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInsAcompanhamento_FaseTreinoId",
                table: "CheckInsAcompanhamento",
                column: "FaseTreinoId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInsAcompanhamento_OrganizacaoId_DataUtc",
                table: "CheckInsAcompanhamento",
                columns: new[] { "OrganizacaoId", "DataUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInsAcompanhamento_PacienteId_DataUtc",
                table: "CheckInsAcompanhamento",
                columns: new[] { "PacienteId", "DataUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_PacienteId",
                table: "Consultas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_ProfissionalId_DataHoraUtc",
                table: "Consultas",
                columns: new[] { "ProfissionalId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvolucoesClinicas_ConsultaId",
                table: "EvolucoesClinicas",
                column: "ConsultaId");

            migrationBuilder.CreateIndex(
                name: "IX_EvolucoesClinicas_OrganizacaoId",
                table: "EvolucoesClinicas",
                column: "OrganizacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_EvolucoesClinicas_PacienteId_DataHoraUtc",
                table: "EvolucoesClinicas",
                columns: new[] { "PacienteId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EvolucoesClinicas_ProfissionalId_DataHoraUtc",
                table: "EvolucoesClinicas",
                columns: new[] { "ProfissionalId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamesLaboratoriais_PacienteId_DataColetaUtc",
                table: "ExamesLaboratoriais",
                columns: new[] { "PacienteId", "DataColetaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamesLaboratoriais_ProfissionalId",
                table: "ExamesLaboratoriais",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesItensTreino_ExecucaoTreinoId",
                table: "ExecucoesItensTreino",
                column: "ExecucaoTreinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesItensTreino_ItemTreinoId",
                table: "ExecucoesItensTreino",
                column: "ItemTreinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesTreino_PacienteId_DataHoraInicioUtc",
                table: "ExecucoesTreino",
                columns: new[] { "PacienteId", "DataHoraInicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesTreino_PlanoTreinoId",
                table: "ExecucoesTreino",
                column: "PlanoTreinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecucoesTreino_SessaoTreinoId",
                table: "ExecucoesTreino",
                column: "SessaoTreinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercicios_OrganizacaoId_Nome",
                table: "Exercicios",
                columns: new[] { "OrganizacaoId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_FasesNutricionais_OrganizacaoId_Status",
                table: "FasesNutricionais",
                columns: new[] { "OrganizacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FasesNutricionais_PacienteId_Ordem",
                table: "FasesNutricionais",
                columns: new[] { "PacienteId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_FasesNutricionais_PlanoAlimentarId",
                table: "FasesNutricionais",
                column: "PlanoAlimentarId");

            migrationBuilder.CreateIndex(
                name: "IX_FasesNutricionais_ProfissionalId",
                table: "FasesNutricionais",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_FasesTreino_OrganizacaoId_Status",
                table: "FasesTreino",
                columns: new[] { "OrganizacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FasesTreino_PacienteId_Ordem",
                table: "FasesTreino",
                columns: new[] { "PacienteId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_FasesTreino_PlanoTreinoId",
                table: "FasesTreino",
                column: "PlanoTreinoId");

            migrationBuilder.CreateIndex(
                name: "IX_FasesTreino_ProfissionalId",
                table: "FasesTreino",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_InteracoesAcompanhamento_OrganizacaoId_ProximoContatoUtc",
                table: "InteracoesAcompanhamento",
                columns: new[] { "OrganizacaoId", "ProximoContatoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InteracoesAcompanhamento_PacienteId_DataHoraUtc",
                table: "InteracoesAcompanhamento",
                columns: new[] { "PacienteId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InteracoesAcompanhamento_ProfissionalId_DataHoraUtc",
                table: "InteracoesAcompanhamento",
                columns: new[] { "ProfissionalId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ItensRefeicaoPlano_AlimentoId",
                table: "ItensRefeicaoPlano",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensRefeicaoPlano_RefeicaoPlanoAlimentarId",
                table: "ItensRefeicaoPlano",
                column: "RefeicaoPlanoAlimentarId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensTreino_ExercicioId",
                table: "ItensTreino",
                column: "ExercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensTreino_SessaoTreinoId_Ordem",
                table: "ItensTreino",
                columns: new[] { "SessaoTreinoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_MarcadoresLaboratoriais_OrganizacaoId_NomeNormalizado",
                table: "MarcadoresLaboratoriais",
                columns: new[] { "OrganizacaoId", "NomeNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetasPaciente_PacienteId_Status_DataInicio",
                table: "MetasPaciente",
                columns: new[] { "PacienteId", "Status", "DataInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_MetasPaciente_ProfissionalId",
                table: "MetasPaciente",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosPlanosAlimentares_OrganizacaoId_Ativo_Nome",
                table: "ModelosPlanosAlimentares",
                columns: new[] { "OrganizacaoId", "Ativo", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosPlanosAlimentares_ProfissionalId",
                table: "ModelosPlanosAlimentares",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosPlanosTreino_OrganizacaoId_Ativo_Nome",
                table: "ModelosPlanosTreino",
                columns: new[] { "OrganizacaoId", "Ativo", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosPlanosTreino_ProfissionalId",
                table: "ModelosPlanosTreino",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosRefeicoes_OrganizacaoId_Ativo_Nome",
                table: "ModelosRefeicoes",
                columns: new[] { "OrganizacaoId", "Ativo", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosRefeicoes_OrganizacaoId_Categoria",
                table: "ModelosRefeicoes",
                columns: new[] { "OrganizacaoId", "Categoria" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosRefeicoes_ProfissionalId",
                table: "ModelosRefeicoes",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosSessoesTreino_OrganizacaoId_Ativo_Nome",
                table: "ModelosSessoesTreino",
                columns: new[] { "OrganizacaoId", "Ativo", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosSessoesTreino_OrganizacaoId_Categoria",
                table: "ModelosSessoesTreino",
                columns: new[] { "OrganizacaoId", "Categoria" });

            migrationBuilder.CreateIndex(
                name: "IX_ModelosSessoesTreino_ProfissionalId",
                table: "ModelosSessoesTreino",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesInternas_OrganizacaoId_UsuarioId_OrigemChave",
                table: "NotificacoesInternas",
                columns: new[] { "OrganizacaoId", "UsuarioId", "OrigemChave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesInternas_UsuarioId_Ativa_LidaEmUtc",
                table: "NotificacoesInternas",
                columns: new[] { "UsuarioId", "Ativa", "LidaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesInternas_UsuarioId_DataEventoUtc",
                table: "NotificacoesInternas",
                columns: new[] { "UsuarioId", "DataEventoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Organizacoes_Cnpj",
                table: "Organizacoes",
                column: "Cnpj",
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_OrganizacaoId_Cpf",
                table: "Pacientes",
                columns: new[] { "OrganizacaoId", "Cpf" },
                unique: true,
                filter: "\"Cpf\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PendenciasClinicas_ConsultaRetornoId",
                table: "PendenciasClinicas",
                column: "ConsultaRetornoId");

            migrationBuilder.CreateIndex(
                name: "IX_PendenciasClinicas_OrganizacaoId_Status_VencimentoUtc",
                table: "PendenciasClinicas",
                columns: new[] { "OrganizacaoId", "Status", "VencimentoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PendenciasClinicas_PacienteId_OrigemCodigo",
                table: "PendenciasClinicas",
                columns: new[] { "PacienteId", "OrigemCodigo" });

            migrationBuilder.CreateIndex(
                name: "IX_PendenciasClinicas_PacienteId_Status",
                table: "PendenciasClinicas",
                columns: new[] { "PacienteId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PendenciasClinicas_ProfissionalId",
                table: "PendenciasClinicas",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "PerfisAcesso",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfisAcessoClaims_RoleId",
                table: "PerfisAcessoClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PerguntasAnamnese_OrganizacaoId_ProfissionalId_Ordem",
                table: "PerguntasAnamnese",
                columns: new[] { "OrganizacaoId", "ProfissionalId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_PerguntasAnamnese_ProfissionalId",
                table: "PerguntasAnamnese",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosAlimentares_PacienteId_DataInicio",
                table: "PlanosAlimentares",
                columns: new[] { "PacienteId", "DataInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosAlimentares_PlanoOrigemId",
                table: "PlanosAlimentares",
                column: "PlanoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosAlimentares_ProfissionalId",
                table: "PlanosAlimentares",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosTreino_PacienteId_Status_DataInicio",
                table: "PlanosTreino",
                columns: new[] { "PacienteId", "Status", "DataInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanosTreino_PlanoOrigemId",
                table: "PlanosTreino",
                column: "PlanoOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanosTreino_ProfissionalId",
                table: "PlanosTreino",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Profissionais_OrganizacaoId_RegistroProfissional",
                table: "Profissionais",
                columns: new[] { "OrganizacaoId", "RegistroProfissional" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefeicoesPlanoAlimentar_PlanoAlimentarId_Ordem",
                table: "RefeicoesPlanoAlimentar",
                columns: new[] { "PlanoAlimentarId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDiarioPaciente_PacienteId_DataHoraUtc",
                table: "RegistrosDiarioPaciente",
                columns: new[] { "PacienteId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosMetas_MetaPacienteId_Data",
                table: "RegistrosMetas",
                columns: new[] { "MetaPacienteId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelatoriosClinicos_PacienteId_DataGeracaoUtc",
                table: "RelatoriosClinicos",
                columns: new[] { "PacienteId", "DataGeracaoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RelatoriosClinicos_ProfissionalId",
                table: "RelatoriosClinicos",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostasAnamnesePersonalizadas_AnamneseId_PerguntaAnamnese~",
                table: "RespostasAnamnesePersonalizadas",
                columns: new[] { "AnamneseId", "PerguntaAnamneseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespostasAnamnesePersonalizadas_PerguntaAnamneseId",
                table: "RespostasAnamnesePersonalizadas",
                column: "PerguntaAnamneseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosExamesLaboratoriais_ExameLaboratorialId_MarcadorL~",
                table: "ResultadosExamesLaboratoriais",
                columns: new[] { "ExameLaboratorialId", "MarcadorLaboratorialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosExamesLaboratoriais_MarcadorLaboratorialId",
                table: "ResultadosExamesLaboratoriais",
                column: "MarcadorLaboratorialId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisoesFases_FaseId",
                table: "RevisoesFases",
                column: "FaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisoesFases_OrganizacaoId_Dominio_DataUtc",
                table: "RevisoesFases",
                columns: new[] { "OrganizacaoId", "Dominio", "DataUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisoesFases_PacienteId_DataUtc",
                table: "RevisoesFases",
                columns: new[] { "PacienteId", "DataUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesTreino_PlanoTreinoId_Ordem",
                table: "SessoesTreino",
                columns: new[] { "PlanoTreinoId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_SubstituicoesItensRefeicao_AlimentoId",
                table: "SubstituicoesItensRefeicao",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SubstituicoesItensRefeicao_ItemRefeicaoPlanoId",
                table: "SubstituicoesItensRefeicao",
                column: "ItemRefeicaoPlanoId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Usuarios",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_OrganizacaoId_Email",
                table: "Usuarios",
                columns: new[] { "OrganizacaoId", "Email" });

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Usuarios",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosClaims_UserId",
                table: "UsuariosClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosLogins_UserId",
                table: "UsuariosLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPerfisAcesso_RoleId",
                table: "UsuariosPerfisAcesso",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Avaliacoes");

            migrationBuilder.DropTable(
                name: "CheckInsAcompanhamento");

            migrationBuilder.DropTable(
                name: "EvolucoesClinicas");

            migrationBuilder.DropTable(
                name: "ExecucoesItensTreino");

            migrationBuilder.DropTable(
                name: "InteracoesAcompanhamento");

            migrationBuilder.DropTable(
                name: "ModelosPlanosAlimentares");

            migrationBuilder.DropTable(
                name: "ModelosPlanosTreino");

            migrationBuilder.DropTable(
                name: "ModelosRefeicoes");

            migrationBuilder.DropTable(
                name: "ModelosSessoesTreino");

            migrationBuilder.DropTable(
                name: "NotificacoesInternas");

            migrationBuilder.DropTable(
                name: "PendenciasClinicas");

            migrationBuilder.DropTable(
                name: "PerfisAcessoClaims");

            migrationBuilder.DropTable(
                name: "RegistrosDiarioPaciente");

            migrationBuilder.DropTable(
                name: "RegistrosMetas");

            migrationBuilder.DropTable(
                name: "RelatoriosClinicos");

            migrationBuilder.DropTable(
                name: "RespostasAnamnesePersonalizadas");

            migrationBuilder.DropTable(
                name: "ResultadosExamesLaboratoriais");

            migrationBuilder.DropTable(
                name: "RevisoesFases");

            migrationBuilder.DropTable(
                name: "SubstituicoesItensRefeicao");

            migrationBuilder.DropTable(
                name: "UsuariosClaims");

            migrationBuilder.DropTable(
                name: "UsuariosLogins");

            migrationBuilder.DropTable(
                name: "UsuariosPerfisAcesso");

            migrationBuilder.DropTable(
                name: "UsuariosTokens");

            migrationBuilder.DropTable(
                name: "FasesNutricionais");

            migrationBuilder.DropTable(
                name: "FasesTreino");

            migrationBuilder.DropTable(
                name: "ExecucoesTreino");

            migrationBuilder.DropTable(
                name: "ItensTreino");

            migrationBuilder.DropTable(
                name: "MetasPaciente");

            migrationBuilder.DropTable(
                name: "Anamneses");

            migrationBuilder.DropTable(
                name: "PerguntasAnamnese");

            migrationBuilder.DropTable(
                name: "ExamesLaboratoriais");

            migrationBuilder.DropTable(
                name: "MarcadoresLaboratoriais");

            migrationBuilder.DropTable(
                name: "ItensRefeicaoPlano");

            migrationBuilder.DropTable(
                name: "PerfisAcesso");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Exercicios");

            migrationBuilder.DropTable(
                name: "SessoesTreino");

            migrationBuilder.DropTable(
                name: "Consultas");

            migrationBuilder.DropTable(
                name: "Alimentos");

            migrationBuilder.DropTable(
                name: "RefeicoesPlanoAlimentar");

            migrationBuilder.DropTable(
                name: "PlanosTreino");

            migrationBuilder.DropTable(
                name: "PlanosAlimentares");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Profissionais");

            migrationBuilder.DropTable(
                name: "Organizacoes");
        }
    }
}
