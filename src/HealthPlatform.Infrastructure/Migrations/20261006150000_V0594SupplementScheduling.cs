using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006150000_V0594SupplementScheduling")]
public partial class V0594SupplementScheduling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SuplementosPlanoAlimentar",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PlanoAlimentarId = table.Column<Guid>(type: "uuid", nullable: false),
                SuplementoId = table.Column<Guid>(type: "uuid", nullable: false),
                RefeicaoPlanoAlimentarId = table.Column<Guid>(type: "uuid", nullable: true),
                QuantidadePorcoes = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                Horario = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                Contexto = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SuplementosPlanoAlimentar", x => x.Id);
                table.ForeignKey("FK_SuplementosPlanoAlimentar_PlanosAlimentares_PlanoAlimentarId", x => x.PlanoAlimentarId, "PlanosAlimentares", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_SuplementosPlanoAlimentar_RefeicoesPlanoAlimentar_RefeicaoPlanoAlimentarId", x => x.RefeicaoPlanoAlimentarId, "RefeicoesPlanoAlimentar", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_SuplementosPlanoAlimentar_Suplementos_SuplementoId", x => x.SuplementoId, "Suplementos", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_SuplementosPlanoAlimentar_PlanoAlimentarId_Horario", table: "SuplementosPlanoAlimentar", columns: new[] { "PlanoAlimentarId", "Horario" });
        migrationBuilder.CreateIndex(name: "IX_SuplementosPlanoAlimentar_RefeicaoPlanoAlimentarId", table: "SuplementosPlanoAlimentar", column: "RefeicaoPlanoAlimentarId");
        migrationBuilder.CreateIndex(name: "IX_SuplementosPlanoAlimentar_SuplementoId", table: "SuplementosPlanoAlimentar", column: "SuplementoId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "SuplementosPlanoAlimentar");
}
