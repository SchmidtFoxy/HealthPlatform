using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030010000_V0726AlimentosFavoritos")]
public partial class V0726AlimentosFavoritos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AlimentosFavoritos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                Refeicao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                AlimentoId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AlimentosFavoritos", x => x.Id);
                table.ForeignKey("FK_AlimentosFavoritos_Alimentos_AlimentoId", x => x.AlimentoId, "Alimentos", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_AlimentosFavoritos_Profissionais_ProfissionalId", x => x.ProfissionalId, "Profissionais", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_AlimentosFavoritos_AlimentoId", table: "AlimentosFavoritos", column: "AlimentoId");
        migrationBuilder.CreateIndex(name: "IX_AlimentosFavoritos_ProfissionalId_Refeicao_AlimentoId", table: "AlimentosFavoritos", columns: new[] { "ProfissionalId", "Refeicao", "AlimentoId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AlimentosFavoritos");
    }
}
