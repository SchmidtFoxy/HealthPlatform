using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260923193000_V01914CalendarioNutricional")]
public partial class V01914CalendarioNutricional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProgramacoesNutricionaisDia",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                PlanoAlimentarId = table.Column<Guid>(type: "uuid", nullable: false),
                Data = table.Column<DateOnly>(type: "date", nullable: false),
                Contexto = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Observacoes = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProgramacoesNutricionaisDia", x => x.Id);
                table.ForeignKey("FK_ProgramacoesNutricionaisDia_Pacientes_PacienteId", x => x.PacienteId, "Pacientes", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ProgramacoesNutricionaisDia_PlanosAlimentares_PlanoAlimentarId", x => x.PlanoAlimentarId, "PlanosAlimentares", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_ProgramacoesNutricionaisDia_PacienteId_Data", "ProgramacoesNutricionaisDia", new[] { "PacienteId", "Data" }, unique: true);
        migrationBuilder.CreateIndex("IX_ProgramacoesNutricionaisDia_PlanoAlimentarId", "ProgramacoesNutricionaisDia", "PlanoAlimentarId");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("ProgramacoesNutricionaisDia");
}
