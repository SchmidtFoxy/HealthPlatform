using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260925042000_V0203PatientStatusManagement")]
public partial class V0203PatientStatusManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "StatusAcompanhamento", table: "Pacientes", type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Ativo");
        migrationBuilder.AddColumn<string>(name: "MotivoStatusAcompanhamento", table: "Pacientes", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "StatusAcompanhamentoAlteradoEmUtc", table: "Pacientes", type: "timestamp with time zone", nullable: true);
        migrationBuilder.Sql("UPDATE \"Pacientes\" SET \"StatusAcompanhamento\" = CASE WHEN \"Ativo\" THEN 'Ativo' ELSE 'Encerrado' END, \"StatusAcompanhamentoAlteradoEmUtc\" = COALESCE(\"UpdatedAtUtc\", \"CreatedAtUtc\");");
        migrationBuilder.CreateIndex(name: "IX_Pacientes_OrganizacaoId_StatusAcompanhamento", table: "Pacientes", columns: new[] { "OrganizacaoId", "StatusAcompanhamento" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Pacientes_OrganizacaoId_StatusAcompanhamento", table: "Pacientes");
        migrationBuilder.DropColumn(name: "MotivoStatusAcompanhamento", table: "Pacientes");
        migrationBuilder.DropColumn(name: "StatusAcompanhamento", table: "Pacientes");
        migrationBuilder.DropColumn(name: "StatusAcompanhamentoAlteradoEmUtc", table: "Pacientes");
    }
}
