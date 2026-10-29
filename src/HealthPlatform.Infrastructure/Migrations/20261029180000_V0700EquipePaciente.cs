using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V0700EquipePaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfissionaisPacientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfissionaisPacientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfissionaisPacientes_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfissionaisPacientes_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionaisPacientes_OrganizacaoId_ProfissionalId",
                table: "ProfissionaisPacientes",
                columns: new[] { "OrganizacaoId", "ProfissionalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionaisPacientes_PacienteId_ProfissionalId",
                table: "ProfissionaisPacientes",
                columns: new[] { "PacienteId", "ProfissionalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfissionaisPacientes_ProfissionalId",
                table: "ProfissionaisPacientes",
                column: "ProfissionalId");

            // Ninguem perde quem ja atende: cada profissional entra na equipe dos pacientes com quem
            // ja teve consulta (nao cancelada nem falta), plano, evolucao, anamnese, conversa no chat
            // ou de quem e o responsavel definido. Pacientes sem nenhum desses ficam so com o admin.
            migrationBuilder.Sql("""
                INSERT INTO "ProfissionaisPacientes" ("Id", "OrganizacaoId", "PacienteId", "ProfissionalId", "CreatedAtUtc")
                SELECT gen_random_uuid(), p."OrganizacaoId", v."PacienteId", v."ProfissionalId", now()
                FROM (
                    SELECT "PacienteId", "ProfissionalId" FROM "Consultas" WHERE "Status" NOT IN (4, 5)
                    UNION SELECT "PacienteId", "ProfissionalId" FROM "PlanosAlimentares"
                    UNION SELECT "PacienteId", "ProfissionalId" FROM "PlanosTreino"
                    UNION SELECT "PacienteId", "ProfissionalId" FROM "EvolucoesClinicas"
                    UNION SELECT "PacienteId", "ProfissionalId" FROM "Anamneses"
                    UNION SELECT "PacienteId", "ProfissionalId" FROM "InteracoesAcompanhamento"
                        WHERE "Canal" LIKE 'Chat:%' AND "Resultado" = 'Profissional'
                    UNION SELECT "Id", "ProfissionalResponsavelId" FROM "Pacientes" WHERE "ProfissionalResponsavelId" IS NOT NULL
                ) v
                JOIN "Pacientes" p ON p."Id" = v."PacienteId"
                JOIN "Profissionais" pr ON pr."Id" = v."ProfissionalId" AND pr."OrganizacaoId" = p."OrganizacaoId"
                ON CONFLICT ("PacienteId", "ProfissionalId") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfissionaisPacientes");
        }
    }
}
