using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V058ProtocolosAcompanhamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProtocolosAcompanhamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Frequencia = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HorarioLocal = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    DiasSemana = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Instrucoes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolosAcompanhamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProtocolosAcompanhamento_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProtocolosAcompanhamento_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolosAcompanhamento_OrganizacaoId_PacienteId_Ativo",
                table: "ProtocolosAcompanhamento",
                columns: new[] { "OrganizacaoId", "PacienteId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolosAcompanhamento_PacienteId_Tipo_Ativo",
                table: "ProtocolosAcompanhamento",
                columns: new[] { "PacienteId", "Tipo", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolosAcompanhamento_ProfissionalId",
                table: "ProtocolosAcompanhamento",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProtocolosAcompanhamento");
        }
    }
}
