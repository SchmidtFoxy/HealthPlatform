using System.Security.Claims;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Quem ve qual paciente. Medico, nutricionista e personal so enxergam os pacientes da propria
/// equipe (ProfissionaisPacientes) ou de quem sao o responsavel definido. Admin e secretaria veem
/// todos; o paciente ve so os proprios dados pelos endpoints dele.
/// </summary>
public sealed class AcessoPacientesService(AppDbContext db)
{
    public static readonly string[] PapeisRestritos =
    [
        nameof(TipoUsuario.Medico),
        nameof(TipoUsuario.Nutricionista),
        nameof(TipoUsuario.Personal)
    ];

    public static bool Restrito(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && PapeisRestritos.Any(user.IsInRole);

    /// <summary>Profissional da requisicao quando ela e restrita (preenchido pelo middleware).</summary>
    public Guid? ProfissionalAtualId { get; private set; }

    public async Task<List<Guid>> PacientesDoProfissionalAsync(Guid organizacaoId, Guid profissionalId, CancellationToken ct)
    {
        var equipe = await db.ProfissionaisPacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == organizacaoId && x.ProfissionalId == profissionalId)
            .Select(x => x.PacienteId)
            .ToListAsync(ct);
        var responsavel = await db.Pacientes.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.OrganizacaoId == organizacaoId && x.ProfissionalResponsavelId == profissionalId)
            .Select(x => x.Id)
            .ToListAsync(ct);
        return equipe.Concat(responsavel).Distinct().ToList();
    }

    /// <summary>Liga o profissional a equipe do paciente (sem duplicar). Nao salva: quem chama salva.</summary>
    public async Task<bool> GarantirNaEquipeAsync(Guid organizacaoId, Guid pacienteId, Guid profissionalId, Guid? criadoPor, CancellationToken ct)
    {
        var existe = await db.ProfissionaisPacientes.AnyAsync(x => x.PacienteId == pacienteId && x.ProfissionalId == profissionalId, ct) ||
            db.ProfissionaisPacientes.Local.Any(x => x.PacienteId == pacienteId && x.ProfissionalId == profissionalId);
        if (existe) return false;
        db.ProfissionaisPacientes.Add(new ProfissionalPaciente
        {
            OrganizacaoId = organizacaoId,
            PacienteId = pacienteId,
            ProfissionalId = profissionalId,
            CriadoPorUsuarioId = criadoPor
        });
        return true;
    }

    /// <summary>Aplica o limite na requisicao: chamado logo depois da autenticacao.</summary>
    public async Task AplicarAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (!Restrito(user)) return;

        var usuarioValor = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        var orgValor = user.FindFirstValue("organization_id");
        if (!Guid.TryParse(usuarioValor, out var usuarioId) || !Guid.TryParse(orgValor, out var org))
        {
            db.PacientesVisiveis = [];
            return;
        }

        var profissionalId = await db.Profissionais.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.OrganizacaoId == org)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
        ProfissionalAtualId = profissionalId;
        db.PacientesVisiveis = profissionalId.HasValue
            ? await PacientesDoProfissionalAsync(org, profissionalId.Value, ct)
            : [];
    }
}

/// <summary>
/// Rede de seguranca para rotas com {pacienteId} que nao passam pelo banco antes de responder
/// (arquivos em disco, por exemplo): paciente fora da equipe responde 404.
/// </summary>
public sealed class FiltroAcessoPaciente(AppDbContext db) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (db.PacientesVisiveis is null) return;
        if (!context.RouteData.Values.TryGetValue("pacienteId", out var valor)) return;
        if (Guid.TryParse(valor?.ToString(), out var pacienteId) && db.PacientesVisiveis.Contains(pacienteId)) return;
        context.Result = new NotFoundObjectResult(new { message = "Paciente nao encontrado." });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
