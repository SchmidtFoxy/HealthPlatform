using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V063MissoesConquistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConquistasPaciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Icone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DataConquista = table.Column<DateOnly>(type: "date", nullable: false),
                    RecompensaXp = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConquistasPaciente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConquistasPaciente_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DesafiosSemanaisPaciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemanaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TipoMetrica = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Meta = table.Column<int>(type: "integer", nullable: false),
                    Progresso = table.Column<int>(type: "integer", nullable: false),
                    RecompensaXp = table.Column<int>(type: "integer", nullable: false),
                    ConcluidoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesafiosSemanaisPaciente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesafiosSemanaisPaciente_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConquistasPaciente_OrganizacaoId_DataConquista",
                table: "ConquistasPaciente",
                columns: new[] { "OrganizacaoId", "DataConquista" });

            migrationBuilder.CreateIndex(
                name: "IX_ConquistasPaciente_PacienteId_Codigo",
                table: "ConquistasPaciente",
                columns: new[] { "PacienteId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesafiosSemanaisPaciente_OrganizacaoId_SemanaInicio",
                table: "DesafiosSemanaisPaciente",
                columns: new[] { "OrganizacaoId", "SemanaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_DesafiosSemanaisPaciente_PacienteId_SemanaInicio_Codigo",
                table: "DesafiosSemanaisPaciente",
                columns: new[] { "PacienteId", "SemanaInicio", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConquistasPaciente");

            migrationBuilder.DropTable(
                name: "DesafiosSemanaisPaciente");
        }
    }
}
