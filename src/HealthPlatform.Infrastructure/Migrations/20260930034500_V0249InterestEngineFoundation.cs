using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260930034500_V0249InterestEngineFoundation")]
public partial class V0249InterestEngineFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InteressesExplorePaciente",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                Codigo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Intencao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Origem = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InteressesExplorePaciente", x => x.Id);
                table.ForeignKey(
                    name: "FK_InteressesExplorePaciente_Organizacoes_OrganizacaoId",
                    column: x => x.OrganizacaoId,
                    principalTable: "Organizacoes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_InteressesExplorePaciente_Pacientes_PacienteId",
                    column: x => x.PacienteId,
                    principalTable: "Pacientes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InteressesExplorePaciente_OrganizacaoId_Intencao",
            table: "InteressesExplorePaciente",
            columns: new[] { "OrganizacaoId", "Intencao" });

        migrationBuilder.CreateIndex(
            name: "IX_InteressesExplorePaciente_PacienteId_Codigo",
            table: "InteressesExplorePaciente",
            columns: new[] { "PacienteId", "Codigo" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InteressesExplorePaciente");
    }
}
