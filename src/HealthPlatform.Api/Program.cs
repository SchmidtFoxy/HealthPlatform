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
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthPlatform API", Version = "v0.10.0" });
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
    var adminEmail = builder.Configuration["Seed:AdminEmail"];
    var adminPassword = builder.Configuration["Seed:AdminPassword"];

    await DbSeeder.SeedAsync(
        db,
        users,
        roles,
        builder.Configuration["Seed:OrganizationName"] ?? "Clinica Demo",
        adminEmail,
        adminPassword);

    // Ambiente local: mantem a credencial demo alinhada ao appsettings.Development.json.
    // Isso evita 401 ao reaproveitar um banco criado por revisoes anteriores.
    if (builder.Configuration.GetValue<bool>("DemoBootstrap:SyncAdminPassword") &&
        !string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        var admin = await users.FindByEmailAsync(adminEmail);

        if (admin is null)
            throw new InvalidOperationException("Admin demo nao encontrado apos o seed local.");

        // A conta de administracao local e uma credencial de desenvolvimento/teste.
        // Tentativas de smoke test com senha antiga podem acionar o lockout do Identity;
        // limpe esse estado antes de validar/sincronizar a senha esperada pelo TESTAR.ps1.
        var resetAccessResult = await users.ResetAccessFailedCountAsync(admin);
        if (!resetAccessResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Nao foi possivel zerar falhas de acesso do admin demo local: " +
                string.Join("; ", resetAccessResult.Errors.Select(x => x.Description)));
        }

        var unlockResult = await users.SetLockoutEndDateAsync(admin, null);
        if (!unlockResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Nao foi possivel desbloquear o admin demo local: " +
                string.Join("; ", unlockResult.Errors.Select(x => x.Description)));
        }

        if (!await users.CheckPasswordAsync(admin, adminPassword))
        {
            var resetToken = await users.GeneratePasswordResetTokenAsync(admin);
            var resetResult = await users.ResetPasswordAsync(admin, resetToken, adminPassword);

            if (!resetResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Nao foi possivel sincronizar a senha do admin demo local: " +
                    string.Join("; ", resetResult.Errors.Select(x => x.Description)));
            }

            Console.WriteLine("[v0.10.0] Admin local desbloqueado e senha sincronizada com Seed:AdminPassword.");
        }
        else
        {
            Console.WriteLine("[v0.10.0] Admin local desbloqueado; credencial ja esta sincronizada.");
        }
    }
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

    // Compatibilidade incremental do MVP hospedado: EnsureCreated nao altera um banco ja existente.
    // A v0.5.1 introduz SolicitacoesClinicas e precisa garantir a tabela tambem em demos Render preservadas.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "SolicitacoesClinicas" (
            "Id" uuid NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NULL,
            "OrganizacaoId" uuid NOT NULL,
            "PacienteId" uuid NOT NULL,
            "ProfissionalId" uuid NOT NULL,
            "Tipo" character varying(60) NOT NULL,
            "Titulo" character varying(300) NOT NULL,
            "Descricao" character varying(3000) NULL,
            "DataLimiteUtc" timestamp with time zone NULL,
            "Status" character varying(30) NOT NULL DEFAULT 'Pendente',
            "RespostaPaciente" character varying(4000) NULL,
            "LinkResposta" character varying(1000) NULL,
            "RespondidaEmUtc" timestamp with time zone NULL,
            "RevisadaEmUtc" timestamp with time zone NULL,
            "ObservacaoRevisao" character varying(3000) NULL,
            CONSTRAINT "PK_SolicitacoesClinicas" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_SolicitacoesClinicas_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_SolicitacoesClinicas_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_SolicitacoesClinicas_OrganizacaoId_Status_DataLimiteUtc" ON "SolicitacoesClinicas" ("OrganizacaoId", "Status", "DataLimiteUtc");
        CREATE INDEX IF NOT EXISTS "IX_SolicitacoesClinicas_PacienteId_Status" ON "SolicitacoesClinicas" ("PacienteId", "Status");
        CREATE INDEX IF NOT EXISTS "IX_SolicitacoesClinicas_ProfissionalId" ON "SolicitacoesClinicas" ("ProfissionalId");
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "ProntidoesDiarias" (
            "Id" uuid NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NULL,
            "OrganizacaoId" uuid NOT NULL,
            "PacienteId" uuid NOT NULL,
            "Data" date NOT NULL,
            "SonoHoras" numeric(4,2) NOT NULL,
            "SonoQualidade" integer NULL,
            "EnergiaNivel" integer NOT NULL,
            "DorNivel" integer NOT NULL,
            "DisposicaoNivel" integer NOT NULL,
            "RecuperacaoNivel" integer NOT NULL,
            "HorasDesdeUltimoTreino" numeric(6,2) NULL,
            "EsforcoUltimoTreino" integer NULL,
            "Score" integer NOT NULL,
            "RecomendacaoTreino" character varying(30) NOT NULL,
            "MotivoRecomendacao" character varying(1200) NULL,
            "Origem" character varying(30) NOT NULL DEFAULT 'Paciente',
            CONSTRAINT "PK_ProntidoesDiarias" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_ProntidoesDiarias_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProntidoesDiarias_PacienteId_Data" ON "ProntidoesDiarias" ("PacienteId", "Data");
        CREATE INDEX IF NOT EXISTS "IX_ProntidoesDiarias_OrganizacaoId_Data" ON "ProntidoesDiarias" ("OrganizacaoId", "Data");
        """);

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
