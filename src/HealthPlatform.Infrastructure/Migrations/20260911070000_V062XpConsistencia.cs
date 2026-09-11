using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V062XpConsistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosXp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Fonte = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FonteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Pontos = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Adequacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosXp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosXp_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosXp_OrganizacaoId_Data",
                table: "EventosXp",
                columns: new[] { "OrganizacaoId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosXp_PacienteId_Fonte_FonteId",
                table: "EventosXp",
                columns: new[] { "PacienteId", "Fonte", "FonteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosXp");
        }
    }
}
