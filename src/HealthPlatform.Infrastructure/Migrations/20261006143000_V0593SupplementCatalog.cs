using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261006143000_V0593SupplementCatalog")]
public partial class V0593SupplementCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Suplementos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                Nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                NomeNormalizado = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Marca = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                MarcaNormalizada = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Forma = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                PorcaoQuantidade = table.Column<decimal>(type: "numeric", nullable: false),
                PorcaoUnidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CaloriasPorPorcao = table.Column<decimal>(type: "numeric", nullable: false),
                ProteinasGPorPorcao = table.Column<decimal>(type: "numeric", nullable: false),
                CarboidratosGPorPorcao = table.Column<decimal>(type: "numeric", nullable: false),
                GordurasGPorPorcao = table.Column<decimal>(type: "numeric", nullable: false),
                FibrasGPorPorcao = table.Column<decimal>(type: "numeric", nullable: false),
                CafeinaMgPorPorcao = table.Column<decimal>(type: "numeric", nullable: true),
                Composicao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                InstrucoesUso = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Ativo = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suplementos", x => x.Id);
                table.ForeignKey(
                    name: "FK_Suplementos_Organizacoes_OrganizacaoId",
                    column: x => x.OrganizacaoId,
                    principalTable: "Organizacoes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Suplementos_OrganizacaoId_Categoria_Ativo",
            table: "Suplementos",
            columns: new[] { "OrganizacaoId", "Categoria", "Ativo" });

        migrationBuilder.CreateIndex(
            name: "IX_Suplementos_OrganizacaoId_NomeNormalizado_MarcaNormalizada",
            table: "Suplementos",
            columns: new[] { "OrganizacaoId", "NomeNormalizado", "MarcaNormalizada" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Suplementos");
    }
}
