using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V061ProntidaoDiaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProntidoesDiarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    SonoHoras = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    SonoQualidade = table.Column<int>(type: "integer", nullable: true),
                    EnergiaNivel = table.Column<int>(type: "integer", nullable: false),
                    DorNivel = table.Column<int>(type: "integer", nullable: false),
                    DisposicaoNivel = table.Column<int>(type: "integer", nullable: false),
                    RecuperacaoNivel = table.Column<int>(type: "integer", nullable: false),
                    HorasDesdeUltimoTreino = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    EsforcoUltimoTreino = table.Column<int>(type: "integer", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    RecomendacaoTreino = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MotivoRecomendacao = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    Origem = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProntidoesDiarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProntidoesDiarias_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProntidoesDiarias_OrganizacaoId_Data",
                table: "ProntidoesDiarias",
                columns: new[] { "OrganizacaoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_ProntidoesDiarias_PacienteId_Data",
                table: "ProntidoesDiarias",
                columns: new[] { "PacienteId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProntidoesDiarias");
        }
    }
}
