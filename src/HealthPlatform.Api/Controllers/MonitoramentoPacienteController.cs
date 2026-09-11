using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
public sealed class MonitoramentoPacienteController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    private static readonly string[] TiposMonitorados =
    [
        "Peso", "PressaoSistolica", "PressaoDiastolica", "Glicemia",
        "FrequenciaCardiaca", "Saturacao", "Temperatura", "Sono", "Dor", "Energia"
    ];

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/monitoramento")]
    public async Task<IActionResult> Profissional(Guid pacienteId, [FromQuery] int dias = 7, CancellationToken ct = default)
    {
        var profissionalExiste = await db.Profissionais.AsNoTracking().AnyAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (!profissionalExiste) return Forbid();

        var pacienteExiste = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (!pacienteExiste) return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await MontarResumo(pacienteId, dias, ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/monitoramento")]
    public async Task<IActionResult> Paciente([FromQuery] int dias = 7, CancellationToken ct = default)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
        if (!pacienteId.HasValue) return NotFound(new { message = "Paciente vinculado nao encontrado." });

        return Ok(await MontarResumo(pacienteId.Value, dias, ct));
    }

    private async Task<object> MontarResumo(Guid pacienteId, int dias, CancellationToken ct)
    {
        dias = Math.Clamp(dias, 1, 90);
        var desdeUtc = DateTime.UtcNow.AddDays(-dias);
        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.DataHoraUtc >= desdeUtc && TiposMonitorados.Contains(x.Tipo))
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new { x.Id, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico, x.Unidade, x.Escala })
            .ToListAsync(ct);

        var metricas = TiposMonitorados.Select(tipo =>
        {
            var itens = registros.Where(x => x.Tipo == tipo).ToList();
            var ultimo = itens.FirstOrDefault();
            var numericos = itens.Where(x => x.ValorNumerico.HasValue).Select(x => x.ValorNumerico!.Value).ToList();
            var escalas = itens.Where(x => x.Escala.HasValue).Select(x => (decimal)x.Escala!.Value).ToList();
            return new
            {
                tipo,
                total = itens.Count,
                ultimo,
                media = numericos.Count > 0 ? Math.Round(numericos.Average(), 1) : escalas.Count > 0 ? Math.Round(escalas.Average(), 1) : (decimal?)null,
                minimo = numericos.Count > 0 ? numericos.Min() : (decimal?)null,
                maximo = numericos.Count > 0 ? numericos.Max() : (decimal?)null
            };
        }).ToList();

        return new { pacienteId, dias, desdeUtc, totalRegistros = registros.Count, metricas };
    }
}
