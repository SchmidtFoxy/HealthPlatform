using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<Usuario> users, RoleManager<IdentityRole<Guid>> roles, string organizationName, string? adminEmail, string? adminPassword)
    {
        string[] roleNames = Enum.GetNames<TipoUsuario>();
        foreach (var roleName in roleNames)
        {
            if (!await roles.RoleExistsAsync(roleName))
                await roles.CreateAsync(new IdentityRole<Guid>(roleName));
        }

        var org = await db.Organizacoes.FirstOrDefaultAsync();
        if (org is null)
        {
            org = new Organizacao { Nome = organizationName };
            db.Organizacoes.Add(org);
            await db.SaveChangesAsync();
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) return;

        var admin = await users.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new Usuario
            {
                Id = Guid.NewGuid(),
                OrganizacaoId = org.Id,
                Nome = "Administrador",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                TipoUsuario = TipoUsuario.Admin
            };

            var result = await users.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            await users.AddToRoleAsync(admin, TipoUsuario.Admin.ToString());
        }
    }
}
