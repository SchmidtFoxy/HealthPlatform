using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260925054500_V0205TagsSegmentation")]
public partial class V0205TagsSegmentation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TagsSegmentacao",
            table: "Pacientes",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TagsSegmentacao",
            table: "Pacientes");
    }
}
