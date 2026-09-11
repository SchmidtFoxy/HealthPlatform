using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V051SolicitacoesClinicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitacoesClinicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    DataLimiteUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RespostaPaciente = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    LinkResposta = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RespondidaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevisadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ObservacaoRevisao = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesClinicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesClinicas_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacoesClinicas_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesClinicas_OrganizacaoId_Status_DataLimiteUtc",
                table: "SolicitacoesClinicas",
                columns: new[] { "OrganizacaoId", "Status", "DataLimiteUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesClinicas_PacienteId_Status",
                table: "SolicitacoesClinicas",
                columns: new[] { "PacienteId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesClinicas_ProfissionalId",
                table: "SolicitacoesClinicas",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitacoesClinicas");
        }
    }
}
