using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V060MedicamentosAdesao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicamentosPaciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Dose = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Via = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Frequencia = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HorariosLocais = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Orientacao = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicamentosPaciente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicamentosPaciente_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicamentosPaciente_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosMedicamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHoraPrevistaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataHoraTomadaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosMedicamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosMedicamentos_MedicamentosPaciente_MedicamentoId",
                        column: x => x.MedicamentoId,
                        principalTable: "MedicamentosPaciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosMedicamentos_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicamentosPaciente_OrganizacaoId_PacienteId_Ativo",
                table: "MedicamentosPaciente",
                columns: new[] { "OrganizacaoId", "PacienteId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicamentosPaciente_PacienteId",
                table: "MedicamentosPaciente",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicamentosPaciente_ProfissionalId",
                table: "MedicamentosPaciente",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosMedicamentos_MedicamentoId_DataHoraPrevistaUtc",
                table: "RegistrosMedicamentos",
                columns: new[] { "MedicamentoId", "DataHoraPrevistaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosMedicamentos_OrganizacaoId_PacienteId_DataHoraPrev~",
                table: "RegistrosMedicamentos",
                columns: new[] { "OrganizacaoId", "PacienteId", "DataHoraPrevistaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosMedicamentos_PacienteId",
                table: "RegistrosMedicamentos",
                column: "PacienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosMedicamentos");

            migrationBuilder.DropTable(
                name: "MedicamentosPaciente");
        }
    }
}
