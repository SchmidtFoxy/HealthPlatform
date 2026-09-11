using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V0103EventosProgressaoSupervisionada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosProgressaoSupervisionada",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CicloEsportivoPacienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Eixo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DataAplicacaoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(1600)", maxLength: 1600, nullable: true),
                    EncerradoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosProgressaoSupervisionada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosProgressaoSupervisionada_CiclosEsportivosPaciente_Ci~",
                        column: x => x.CicloEsportivoPacienteId,
                        principalTable: "CiclosEsportivosPaciente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventosProgressaoSupervisionada_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosProgressaoSupervisionada_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosProgressaoSupervisionada_CicloEsportivoPacienteId",
                table: "EventosProgressaoSupervisionada",
                column: "CicloEsportivoPacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosProgressaoSupervisionada_OrganizacaoId_Status",
                table: "EventosProgressaoSupervisionada",
                columns: new[] { "OrganizacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosProgressaoSupervisionada_PacienteId_DataAplicacaoUtc",
                table: "EventosProgressaoSupervisionada",
                columns: new[] { "PacienteId", "DataAplicacaoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosProgressaoSupervisionada_ProfissionalId",
                table: "EventosProgressaoSupervisionada",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosProgressaoSupervisionada");
        }
    }
}
