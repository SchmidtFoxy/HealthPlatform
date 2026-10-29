using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261029170000_V0700ProfissionalResponsavel")]
public partial class V0700ProfissionalResponsavel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ProfissionalResponsavelId",
            table: "Pacientes",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Pacientes_ProfissionalResponsavelId",
            table: "Pacientes",
            column: "ProfissionalResponsavelId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Pacientes_ProfissionalResponsavelId",
            table: "Pacientes");

        migrationBuilder.DropColumn(
            name: "ProfissionalResponsavelId",
            table: "Pacientes");
    }
}
