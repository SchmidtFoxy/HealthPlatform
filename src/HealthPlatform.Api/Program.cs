using System.Text;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthPlatform API", Version = "v0.3.40" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o JWT obtido em /api/auth/login"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var connectionString = DatabaseConnectionResolver.Resolve(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddIdentityCore<Usuario>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole<Guid>>()
.AddSignInManager()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Secao Jwt nao configurada.");

if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    throw new InvalidOperationException("Jwt:Key deve possuir pelo menos 32 bytes.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Todo controller existente com [Authorize] continua sendo area PROFISSIONAL.
    // O paciente recebe apenas a policy PatientOnly nos endpoints feitos para ele.
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(
            TipoUsuario.Admin.ToString(),
            TipoUsuario.Medico.ToString(),
            TipoUsuario.Nutricionista.ToString(),
            TipoUsuario.Personal.ToString(),
            TipoUsuario.Secretaria.ToString())
        .Build();

    options.AddPolicy("PatientOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(TipoUsuario.Paciente.ToString()));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SPA web do MVP. Rotas desconhecidas fora de /api caem no index.
app.MapFallbackToFile("index.html");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(
        db,
        users,
        roles,
        builder.Configuration["Seed:OrganizationName"] ?? "Clinica Demo",
        builder.Configuration["Seed:AdminEmail"],
        builder.Configuration["Seed:AdminPassword"]);
}
else if (builder.Configuration.GetValue<bool>("DemoBootstrap:Enabled"))
{
    // Caminho propositalmente simples para o MVP hospedado.
    // Em um banco Render NOVO e vazio, cria o schema atual diretamente.
    // Nao substitui a estrategia definitiva de migrations da futura producao.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    await db.Database.EnsureCreatedAsync();

    var adminEmail = builder.Configuration["Seed:AdminEmail"];
    var adminPassword = builder.Configuration["Seed:AdminPassword"];

    await DbSeeder.SeedAsync(
        db,
        users,
        roles,
        builder.Configuration["Seed:OrganizationName"] ?? "Clinica Demo MVP",
        adminEmail,
        adminPassword);

    if (builder.Configuration.GetValue<bool>("DemoBootstrap:SyncAdminPassword") &&
        !string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        var admin = await users.FindByEmailAsync(adminEmail);

        if (admin is null)
            throw new InvalidOperationException("Admin demo nao encontrado apos o seed.");

        if (!await users.CheckPasswordAsync(admin, adminPassword))
        {
            var resetToken = await users.GeneratePasswordResetTokenAsync(admin);
            var resetResult = await users.ResetPasswordAsync(admin, resetToken, adminPassword);

            if (!resetResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Nao foi possivel sincronizar a senha do admin demo: " +
                    string.Join("; ", resetResult.Errors.Select(x => x.Description)));
            }

            Console.WriteLine("Senha do admin demo sincronizada com Seed__AdminPassword.");
        }
    }
}

app.Run();
