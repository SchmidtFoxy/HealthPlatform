using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261026220000_V0659TreinoExerciciosExtras")]
public partial class V0659TreinoExerciciosExtras : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExecucoesExerciciosExtras",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExecucaoTreinoId = table.Column<Guid>(type: "uuid", nullable: false),
                ExercicioId = table.Column<Guid>(type: "uuid", nullable: false),
                Ordem = table.Column<int>(type: "integer", nullable: false),
                SeriesRealizadas = table.Column<int>(type: "integer", nullable: true),
                RepeticoesRealizadas = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                CargaRealizada = table.Column<decimal>(type: "numeric", nullable: true),
                UnidadeCarga = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                Observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExecucoesExerciciosExtras", x => x.Id);
                table.ForeignKey("FK_ExecucoesExerciciosExtras_ExecucoesTreino_ExecucaoTreinoId", x => x.ExecucaoTreinoId, "ExecucoesTreino", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ExecucoesExerciciosExtras_Exercicios_ExercicioId", x => x.ExercicioId, "Exercicios", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_ExecucoesExerciciosExtras_ExecucaoTreinoId", table: "ExecucoesExerciciosExtras", column: "ExecucaoTreinoId");
        migrationBuilder.CreateIndex(name: "IX_ExecucoesExerciciosExtras_ExercicioId", table: "ExecucoesExerciciosExtras", column: "ExercicioId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ExecucoesExerciciosExtras");
    }
}
