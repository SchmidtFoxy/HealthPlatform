using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

public partial class V0273AdvancedTechniques : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "TecnicaAvancadaCodigo", table: "ItensTreino", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TecnicaAvancadaParametros", table: "ItensTreino", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TecnicaExecutadaCodigo", table: "ExecucoesItensTreino", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TecnicaExecutadaParametros", table: "ExecucoesItensTreino", type: "character varying(500)", maxLength: 500, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TecnicaAvancadaCodigo", table: "ItensTreino");
        migrationBuilder.DropColumn(name: "TecnicaAvancadaParametros", table: "ItensTreino");
        migrationBuilder.DropColumn(name: "TecnicaExecutadaCodigo", table: "ExecucoesItensTreino");
        migrationBuilder.DropColumn(name: "TecnicaExecutadaParametros", table: "ExecucoesItensTreino");
    }
}
