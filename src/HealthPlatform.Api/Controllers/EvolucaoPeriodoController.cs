using HealthPlatform.Api.Contracts.Evolucao;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

/// <summary>Aba Evolucao da ficha: o mesmo painel que o paciente ve em Progresso.</summary>
[ApiController]
[Authorize]
public sealed class EvolucaoPeriodoController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/evolucao-periodo")]
    public async Task<ActionResult<EvolucaoPeriodoResponse>> Get(Guid pacienteId, [FromQuery] int dias = 90, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (!existe) return NotFound(new { message = "Paciente nao encontrado." });
        return Ok(await EvolucaoPeriodoService.MontarAsync(db, pacienteId, dias, paraPaciente: false, ct));
    }
}
