using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V064CiclosEsportivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CiclosEsportivosPaciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    FaseTreinoId = table.Column<Guid>(type: "uuid", nullable: true),
                    FaseNutricionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PerfilEsportivo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MetaTreinosSemanais = table.Column<int>(type: "integer", nullable: true),
                    MetaConsistenciaPercentual = table.Column<int>(type: "integer", nullable: true),
                    MetaPesoKg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CiclosEsportivosPaciente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CiclosEsportivosPaciente_FasesNutricionais_FaseNutricionalId",
                        column: x => x.FaseNutricionalId,
                        principalTable: "FasesNutricionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CiclosEsportivosPaciente_FasesTreino_FaseTreinoId",
                        column: x => x.FaseTreinoId,
                        principalTable: "FasesTreino",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CiclosEsportivosPaciente_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CiclosEsportivosPaciente_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CiclosEsportivosPaciente_FaseNutricionalId",
                table: "CiclosEsportivosPaciente",
                column: "FaseNutricionalId");

            migrationBuilder.CreateIndex(
                name: "IX_CiclosEsportivosPaciente_FaseTreinoId",
                table: "CiclosEsportivosPaciente",
                column: "FaseTreinoId");

            migrationBuilder.CreateIndex(
                name: "IX_CiclosEsportivosPaciente_OrganizacaoId_Status",
                table: "CiclosEsportivosPaciente",
                columns: new[] { "OrganizacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CiclosEsportivosPaciente_PacienteId_Periodo",
                table: "CiclosEsportivosPaciente",
                columns: new[] { "PacienteId", "DataInicio", "DataFim" });

            migrationBuilder.CreateIndex(
                name: "IX_CiclosEsportivosPaciente_ProfissionalId",
                table: "CiclosEsportivosPaciente",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CiclosEsportivosPaciente");
        }
    }
}
