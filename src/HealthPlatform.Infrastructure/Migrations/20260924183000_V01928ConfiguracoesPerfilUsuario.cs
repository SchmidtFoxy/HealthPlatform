using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthPlatform.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260924183000_V01928ConfiguracoesPerfilUsuario")]
public partial class V01928ConfiguracoesPerfilUsuario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "FotoPerfilDataUrl", table: "Usuarios", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "TemaPreferido", table: "Usuarios", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "light");
        migrationBuilder.AddColumn<bool>(name: "NotificarMensagens", table: "Usuarios", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>(name: "NotificarAtualizacoesPlano", table: "Usuarios", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>(name: "NotificarLembretes", table: "Usuarios", type: "boolean", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>(name: "NotificarCheckIns", table: "Usuarios", type: "boolean", nullable: false, defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FotoPerfilDataUrl", table: "Usuarios");
        migrationBuilder.DropColumn(name: "TemaPreferido", table: "Usuarios");
        migrationBuilder.DropColumn(name: "NotificarMensagens", table: "Usuarios");
        migrationBuilder.DropColumn(name: "NotificarAtualizacoesPlano", table: "Usuarios");
        migrationBuilder.DropColumn(name: "NotificarLembretes", table: "Usuarios");
        migrationBuilder.DropColumn(name: "NotificarCheckIns", table: "Usuarios");
    }
}
