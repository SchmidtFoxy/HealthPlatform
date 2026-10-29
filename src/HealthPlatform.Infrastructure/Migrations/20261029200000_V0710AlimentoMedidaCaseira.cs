using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261029200000_V0710AlimentoMedidaCaseira")]
public partial class V0710AlimentoMedidaCaseira : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "UnidadeNome", table: "Alimentos", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "GramasPorUnidade", table: "Alimentos", type: "numeric", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "GramasPorMl", table: "Alimentos", type: "numeric", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "UnidadeNome", table: "Alimentos");
        migrationBuilder.DropColumn(name: "GramasPorUnidade", table: "Alimentos");
        migrationBuilder.DropColumn(name: "GramasPorMl", table: "Alimentos");
    }
}
