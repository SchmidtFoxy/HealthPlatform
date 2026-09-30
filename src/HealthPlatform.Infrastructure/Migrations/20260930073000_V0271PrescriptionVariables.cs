using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(HealthPlatform.Infrastructure.Data.AppDbContext))]
[Migration("20260930073000_V0271PrescriptionVariables")]
public partial class V0271PrescriptionVariables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "RirAlvo", table: "ItensTreino", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Cadencia", table: "ItensTreino", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TecnicaAvancada", table: "ItensTreino", type: "character varying(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<int>(name: "RirRealizado", table: "ExecucoesItensTreino", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CadenciaRealizada", table: "ExecucoesItensTreino", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TecnicaExecutada", table: "ExecucoesItensTreino", type: "character varying(80)", maxLength: 80, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RirAlvo", table: "ItensTreino");
        migrationBuilder.DropColumn(name: "Cadencia", table: "ItensTreino");
        migrationBuilder.DropColumn(name: "TecnicaAvancada", table: "ItensTreino");
        migrationBuilder.DropColumn(name: "RirRealizado", table: "ExecucoesItensTreino");
        migrationBuilder.DropColumn(name: "CadenciaRealizada", table: "ExecucoesItensTreino");
        migrationBuilder.DropColumn(name: "TecnicaExecutada", table: "ExecucoesItensTreino");
    }
}
