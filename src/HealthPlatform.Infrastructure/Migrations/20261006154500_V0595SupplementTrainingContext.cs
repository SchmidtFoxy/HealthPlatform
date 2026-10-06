using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006154500_V0595SupplementTrainingContext")]
public partial class V0595SupplementTrainingContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "SessaoTreinoId", table: "SuplementosPlanoAlimentar", type: "uuid", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_SuplementosPlanoAlimentar_SessaoTreinoId", table: "SuplementosPlanoAlimentar", column: "SessaoTreinoId");
        migrationBuilder.AddForeignKey(name: "FK_SuplementosPlanoAlimentar_SessoesTreino_SessaoTreinoId", table: "SuplementosPlanoAlimentar", column: "SessaoTreinoId", principalTable: "SessoesTreino", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_SuplementosPlanoAlimentar_SessoesTreino_SessaoTreinoId", table: "SuplementosPlanoAlimentar");
        migrationBuilder.DropIndex(name: "IX_SuplementosPlanoAlimentar_SessaoTreinoId", table: "SuplementosPlanoAlimentar");
        migrationBuilder.DropColumn(name: "SessaoTreinoId", table: "SuplementosPlanoAlimentar");
    }
}
