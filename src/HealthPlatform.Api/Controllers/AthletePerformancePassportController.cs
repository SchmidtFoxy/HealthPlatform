using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public sealed class AthletePerformancePassportController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/performance/passaporte")]
    public async Task<IActionResult> Profissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarAsync(db, pacienteId, ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/passaporte")]
    public async Task<IActionResult> Paciente(CancellationToken ct = default)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarAsync(db, pacienteId.Value, ct));
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/performance/passaporte/recordes")]
    public async Task<IActionResult> RecordesProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarRecordesAsync(
            db,
            pacienteId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/passaporte/recordes")]
    public async Task<IActionResult> RecordesPaciente(CancellationToken ct = default)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarRecordesAsync(
            db,
            pacienteId.Value,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/passaporte/tempos")]
    public async Task<IActionResult> TemposProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarTemposAsync(
            db,
            pacienteId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/passaporte/tempos")]
    public async Task<IActionResult> TemposPaciente(CancellationToken ct = default)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarTemposAsync(
            db,
            pacienteId.Value,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/passaporte/resultados")]
    public async Task<IActionResult> ResultadosProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(
            db,
            pacienteId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/passaporte/resultados")]
    public async Task<IActionResult> ResultadosPaciente(CancellationToken ct = default)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(
            db,
            pacienteId.Value,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }

}
