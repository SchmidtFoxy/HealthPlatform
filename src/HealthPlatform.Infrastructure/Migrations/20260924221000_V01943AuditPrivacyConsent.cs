using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260924221000_V01943AuditPrivacyConsent")]
public partial class V01943AuditPrivacyConsent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "ContaDesativadaEmUtc", table: "Usuarios", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "PoliticaPrivacidadeAceitaEmUtc", table: "Usuarios", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "PoliticaPrivacidadeVersaoAceita", table: "Usuarios", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "SolicitacaoExclusaoDadosEmUtc", table: "Usuarios", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "TermosAceitosEmUtc", table: "Usuarios", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "TermosVersaoAceita", table: "Usuarios", type: "character varying(40)", maxLength: 40, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ContaDesativadaEmUtc", table: "Usuarios");
        migrationBuilder.DropColumn(name: "PoliticaPrivacidadeAceitaEmUtc", table: "Usuarios");
        migrationBuilder.DropColumn(name: "PoliticaPrivacidadeVersaoAceita", table: "Usuarios");
        migrationBuilder.DropColumn(name: "SolicitacaoExclusaoDadosEmUtc", table: "Usuarios");
        migrationBuilder.DropColumn(name: "TermosAceitosEmUtc", table: "Usuarios");
        migrationBuilder.DropColumn(name: "TermosVersaoAceita", table: "Usuarios");
    }
}
