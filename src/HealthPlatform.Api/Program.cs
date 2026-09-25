using System.Security.Claims;
using System.Text;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Email;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Infrastructure.Data;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthPlatform API", Version = "v0.19.39" });
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

// v0.19.26 - tokens publicos de conta devem expirar rapidamente.
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(30));

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<ITransactionalEmailService, SmtpTransactionalEmailService>();

// v0.19.39 - Web Push usa VAPID; em Development permanece desligado por padrao.
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection(PushOptions.SectionName));
builder.Services.AddScoped<IPushNotificationService, WebPushNotificationService>();

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

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue("sub");
                var organizationValue = context.Principal?.FindFirstValue("organization_id");
                var stamp = context.Principal?.FindFirstValue("security_stamp");

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("Sessao invalida.");
                    return;
                }

                var usuario = await userManager.FindByIdAsync(userId.ToString());
                if (usuario is null || !usuario.Ativo ||
                    !Guid.TryParse(organizationValue, out var organizationId) ||
                    usuario.OrganizacaoId != organizationId ||
                    string.IsNullOrWhiteSpace(stamp) ||
                    !string.Equals(usuario.SecurityStamp, stamp, StringComparison.Ordinal))
                {
                    context.Fail("Sessao expirada ou revogada.");
                    return;
                }

                if (context.Principal?.HasClaim("impersonation", "true") == true)
                {
                    var impersonatorValue = context.Principal.FindFirstValue("impersonator_id");
                    var impersonatorStamp = context.Principal.FindFirstValue("impersonator_security_stamp");
                    if (!Guid.TryParse(impersonatorValue, out var impersonatorId))
                    {
                        context.Fail("Sessao de simulacao invalida.");
                        return;
                    }
                    var impersonator = await userManager.FindByIdAsync(impersonatorId.ToString());
                    if (impersonator is null || !impersonator.Ativo ||
                        impersonator.OrganizacaoId != organizationId ||
                        impersonator.TipoUsuario != TipoUsuario.Admin ||
                        string.IsNullOrWhiteSpace(impersonatorStamp) ||
                        !string.Equals(impersonator.SecurityStamp, impersonatorStamp, StringComparison.Ordinal))
                    {
                        context.Fail("Administrador de origem nao esta mais autorizado.");
                    }
                }
            },
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted) return;
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Sua sessao expirou ou nao e valida. Entre novamente."
                });
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted) return;
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Voce nao tem permissao para executar esta acao."
                });
            }
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

    options.AddPolicy("AuthenticatedOnly", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("PatientOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(TipoUsuario.Paciente.ToString()));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Nginx termina TLS na borda da VPS; a API precisa reconstruir o esquema HTTPS
    // antes de HSTS/redirects, evitando loops e URLs incorretas atras do reverse proxy.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
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

// Deve executar antes de HTTPS/HSTS para respeitar X-Forwarded-Proto do Nginx.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();

// v0.19.37 — a impersonacao administrativa e deliberadamente somente leitura.
// Isso evita que uma acao real seja atribuida ao profissional simulado no AuditLog.
app.Use(async (context, next) =>
{
    var impersonating = context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim("impersonation", "true");
    var isFinalize = context.Request.Path.StartsWithSegments("/api/impersonacao/finalizar");
    var isRead = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method);

    if (impersonating && !isFinalize && !isRead)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Modo de simulacao e somente leitura. Saia da simulacao para executar alteracoes reais."
        });
        return;
    }

    await next();
});

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
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "EventosProgressaoSupervisionada" (
            "Id" uuid NOT NULL,
            "OrganizacaoId" uuid NOT NULL,
            "PacienteId" uuid NOT NULL,
            "ProfissionalId" uuid NOT NULL,
            "CicloEsportivoPacienteId" uuid NULL,
            "Eixo" character varying(40) NOT NULL,
            "Descricao" character varying(1000) NOT NULL,
            "DataAplicacaoUtc" timestamp with time zone NOT NULL,
            "Status" character varying(30) NOT NULL DEFAULT 'EmObservacao',
            "Observacoes" character varying(1600) NULL,
            "EncerradoEmUtc" timestamp with time zone NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NULL,
            CONSTRAINT "PK_EventosProgressaoSupervisionada" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_EventosProgressaoSupervisionada_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_EventosProgressaoSupervisionada_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_EventosProgressaoSupervisionada_Ciclos_CicloId" FOREIGN KEY ("CicloEsportivoPacienteId") REFERENCES "CiclosEsportivosPaciente" ("Id") ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_EventosProgressaoSupervisionada_PacienteId_DataAplicacaoUtc" ON "EventosProgressaoSupervisionada" ("PacienteId", "DataAplicacaoUtc");
        CREATE INDEX IF NOT EXISTS "IX_EventosProgressaoSupervisionada_OrganizacaoId_Status" ON "EventosProgressaoSupervisionada" ("OrganizacaoId", "Status");
        """);

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

            Console.WriteLine("[v0.10.3] Admin local desbloqueado e senha sincronizada com Seed:AdminPassword.");
        }
        else
        {
            Console.WriteLine("[v0.10.3] Admin local desbloqueado; credencial ja esta sincronizada.");
        }
    }
}
else if (builder.Configuration.GetValue<bool>("DemoBootstrap:Enabled"))
{
    // Caminho propositalmente simples para o MVP hospedado.
    // Em um banco NOVO e vazio, cria o schema atual diretamente.
    // Nao substitui a estrategia definitiva de migrations da futura producao.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    await db.Database.EnsureCreatedAsync();

    // Compatibilidade incremental do MVP hospedado: EnsureCreated nao altera um banco ja existente.
    // A v0.5.1 introduz SolicitacoesClinicas e precisa garantir a tabela tambem em bancos de demonstracao preservados.
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

    // Compatibilidade com bancos demo preservados de versoes anteriores.
    // EnsureCreatedAsync cria o schema completo apenas quando o banco e novo/vazio;
    // em bancos existentes, garantimos aqui os upgrades esportivos/gamificacao em ordem.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "ProtocolosAcompanhamento" (
            "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
            "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "ProfissionalId" uuid NOT NULL,
            "Tipo" character varying(60) NOT NULL, "Titulo" character varying(180) NOT NULL, "Unidade" character varying(40) NULL,
            "Frequencia" character varying(30) NOT NULL DEFAULT 'Diario', "HorarioLocal" character varying(5) NULL,
            "DiasSemana" character varying(80) NULL, "Instrucoes" character varying(1200) NULL, "Ativo" boolean NOT NULL DEFAULT TRUE,
            CONSTRAINT "PK_ProtocolosAcompanhamento" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_ProtocolosAcompanhamento_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_ProtocolosAcompanhamento_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_ProtocolosAcompanhamento_OrganizacaoId_PacienteId_Ativo" ON "ProtocolosAcompanhamento" ("OrganizacaoId", "PacienteId", "Ativo");
        CREATE INDEX IF NOT EXISTS "IX_ProtocolosAcompanhamento_PacienteId_Tipo_Ativo" ON "ProtocolosAcompanhamento" ("PacienteId", "Tipo", "Ativo");

        CREATE TABLE IF NOT EXISTS "MedicamentosPaciente" (
            "Id" uuid NOT NULL, "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "ProfissionalId" uuid NOT NULL,
            "Nome" varchar(220) NOT NULL, "Dose" numeric(12,3) NULL, "Unidade" varchar(40) NULL, "Via" varchar(80) NULL,
            "Frequencia" varchar(40) NOT NULL DEFAULT 'Diario', "HorariosLocais" varchar(200) NULL,
            "DataInicio" date NOT NULL, "DataFim" date NULL, "Orientacao" varchar(1600) NULL, "Ativo" boolean NOT NULL DEFAULT TRUE,
            "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
            CONSTRAINT "PK_MedicamentosPaciente" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_MedicamentosPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_MedicamentosPaciente_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_MedicamentosPaciente_OrganizacaoId_PacienteId_Ativo" ON "MedicamentosPaciente" ("OrganizacaoId", "PacienteId", "Ativo");

        CREATE TABLE IF NOT EXISTS "RegistrosMedicamentos" (
            "Id" uuid NOT NULL, "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "MedicamentoId" uuid NOT NULL,
            "DataHoraPrevistaUtc" timestamp with time zone NOT NULL, "DataHoraTomadaUtc" timestamp with time zone NULL,
            "Status" varchar(30) NOT NULL, "Observacao" varchar(1000) NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
            CONSTRAINT "PK_RegistrosMedicamentos" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_RegistrosMedicamentos_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_RegistrosMedicamentos_MedicamentosPaciente_MedicamentoId" FOREIGN KEY ("MedicamentoId") REFERENCES "MedicamentosPaciente" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_RegistrosMedicamentos_OrganizacaoId_PacienteId_DataHoraPrevistaUtc" ON "RegistrosMedicamentos" ("OrganizacaoId", "PacienteId", "DataHoraPrevistaUtc");
        CREATE INDEX IF NOT EXISTS "IX_RegistrosMedicamentos_MedicamentoId_DataHoraPrevistaUtc" ON "RegistrosMedicamentos" ("MedicamentoId", "DataHoraPrevistaUtc");

        CREATE TABLE IF NOT EXISTS "EventosXp" (
            "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
            "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "Data" date NOT NULL,
            "Fonte" character varying(40) NOT NULL, "FonteId" uuid NOT NULL, "Pontos" integer NOT NULL,
            "Motivo" character varying(500) NOT NULL, "Adequacao" character varying(30) NULL,
            CONSTRAINT "PK_EventosXp" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_EventosXp_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_EventosXp_PacienteId_Fonte_FonteId" ON "EventosXp" ("PacienteId", "Fonte", "FonteId");
        CREATE INDEX IF NOT EXISTS "IX_EventosXp_OrganizacaoId_Data" ON "EventosXp" ("OrganizacaoId", "Data");

        CREATE TABLE IF NOT EXISTS "DesafiosSemanaisPaciente" (
            "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
            "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "SemanaInicio" date NOT NULL,
            "Codigo" character varying(60) NOT NULL, "Titulo" character varying(160) NOT NULL, "Descricao" character varying(500) NOT NULL,
            "TipoMetrica" character varying(40) NOT NULL, "Meta" integer NOT NULL, "Progresso" integer NOT NULL, "RecompensaXp" integer NOT NULL,
            "ConcluidoEmUtc" timestamp with time zone NULL,
            CONSTRAINT "PK_DesafiosSemanaisPaciente" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_DesafiosSemanaisPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_DesafiosSemanaisPaciente_PacienteId_SemanaInicio_Codigo" ON "DesafiosSemanaisPaciente" ("PacienteId", "SemanaInicio", "Codigo");
        CREATE INDEX IF NOT EXISTS "IX_DesafiosSemanaisPaciente_OrganizacaoId_SemanaInicio" ON "DesafiosSemanaisPaciente" ("OrganizacaoId", "SemanaInicio");

        CREATE TABLE IF NOT EXISTS "ConquistasPaciente" (
            "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
            "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "Codigo" character varying(60) NOT NULL,
            "Titulo" character varying(160) NOT NULL, "Descricao" character varying(500) NOT NULL, "Icone" character varying(20) NOT NULL,
            "DataConquista" date NOT NULL, "RecompensaXp" integer NOT NULL,
            CONSTRAINT "PK_ConquistasPaciente" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_ConquistasPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_ConquistasPaciente_PacienteId_Codigo" ON "ConquistasPaciente" ("PacienteId", "Codigo");
        CREATE INDEX IF NOT EXISTS "IX_ConquistasPaciente_OrganizacaoId_DataConquista" ON "ConquistasPaciente" ("OrganizacaoId", "DataConquista");

        CREATE TABLE IF NOT EXISTS "CiclosEsportivosPaciente" (
            "Id" uuid NOT NULL, "CreatedAtUtc" timestamp with time zone NOT NULL, "UpdatedAtUtc" timestamp with time zone NULL,
            "OrganizacaoId" uuid NOT NULL, "PacienteId" uuid NOT NULL, "ProfissionalId" uuid NOT NULL,
            "FaseTreinoId" uuid NULL, "FaseNutricionalId" uuid NULL, "Nome" character varying(160) NOT NULL,
            "PerfilEsportivo" character varying(50) NOT NULL, "Objetivo" character varying(800) NULL,
            "DataInicio" date NOT NULL, "DataFim" date NOT NULL, "Status" character varying(30) NOT NULL,
            "MetaTreinosSemanais" integer NULL, "MetaConsistenciaPercentual" integer NULL, "MetaPesoKg" numeric(8,2) NULL,
            "Observacoes" character varying(1200) NULL,
            CONSTRAINT "PK_CiclosEsportivosPaciente" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_CiclosEsportivosPaciente_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_CiclosEsportivosPaciente_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_CiclosEsportivosPaciente_FasesTreino_FaseTreinoId" FOREIGN KEY ("FaseTreinoId") REFERENCES "FasesTreino" ("Id") ON DELETE SET NULL,
            CONSTRAINT "FK_CiclosEsportivosPaciente_FasesNutricionais_FaseNutricionalId" FOREIGN KEY ("FaseNutricionalId") REFERENCES "FasesNutricionais" ("Id") ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_PacienteId_Periodo" ON "CiclosEsportivosPaciente" ("PacienteId", "DataInicio", "DataFim");
        CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_OrganizacaoId_Status" ON "CiclosEsportivosPaciente" ("OrganizacaoId", "Status");
        CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_ProfissionalId" ON "CiclosEsportivosPaciente" ("ProfissionalId");
        CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_FaseTreinoId" ON "CiclosEsportivosPaciente" ("FaseTreinoId");
        CREATE INDEX IF NOT EXISTS "IX_CiclosEsportivosPaciente_FaseNutricionalId" ON "CiclosEsportivosPaciente" ("FaseNutricionalId");
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

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "EventosProgressaoSupervisionada" (
            "Id" uuid NOT NULL,
            "OrganizacaoId" uuid NOT NULL,
            "PacienteId" uuid NOT NULL,
            "ProfissionalId" uuid NOT NULL,
            "CicloEsportivoPacienteId" uuid NULL,
            "Eixo" character varying(40) NOT NULL,
            "Descricao" character varying(1000) NOT NULL,
            "DataAplicacaoUtc" timestamp with time zone NOT NULL,
            "Status" character varying(30) NOT NULL DEFAULT 'EmObservacao',
            "Observacoes" character varying(1600) NULL,
            "EncerradoEmUtc" timestamp with time zone NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NULL,
            CONSTRAINT "PK_EventosProgressaoSupervisionada" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_EventosProgressaoSupervisionada_Pacientes_PacienteId" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_EventosProgressaoSupervisionada_Profissionais_ProfissionalId" FOREIGN KEY ("ProfissionalId") REFERENCES "Profissionais" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_EventosProgressaoSupervisionada_Ciclos_CicloId" FOREIGN KEY ("CicloEsportivoPacienteId") REFERENCES "CiclosEsportivosPaciente" ("Id") ON DELETE SET NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_EventosProgressaoSupervisionada_PacienteId_DataAplicacaoUtc" ON "EventosProgressaoSupervisionada" ("PacienteId", "DataAplicacaoUtc");
        CREATE INDEX IF NOT EXISTS "IX_EventosProgressaoSupervisionada_OrganizacaoId_Status" ON "EventosProgressaoSupervisionada" ("OrganizacaoId", "Status");
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
