using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260925050000_V0204ProfessionalInternalNotes")]
public partial class V0204ProfessionalInternalNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotasInternasProfissionais",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                AutorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                AutorNome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Categoria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Conteudo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Fixada = table.Column<bool>(type: "boolean", nullable: false),
                Arquivada = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotasInternasProfissionais", x => x.Id);
                table.ForeignKey(name: "FK_NotasInternasProfissionais_Organizacoes_OrganizacaoId", column: x => x.OrganizacaoId, principalTable: "Organizacoes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_NotasInternasProfissionais_Pacientes_PacienteId", column: x => x.PacienteId, principalTable: "Pacientes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_NotasInternasProfissionais_OrganizacaoId_PacienteId_Fixada", table: "NotasInternasProfissionais", columns: new[] { "OrganizacaoId", "PacienteId", "Fixada" });
        migrationBuilder.CreateIndex(name: "IX_NotasInternasProfissionais_PacienteId_CreatedAtUtc", table: "NotasInternasProfissionais", columns: new[] { "PacienteId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "NotasInternasProfissionais");
}
