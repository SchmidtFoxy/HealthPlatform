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


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/passaporte/habilidades-marcos")]
    public async Task<IActionResult> HabilidadesMarcosProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(
            db,
            pacienteId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/passaporte/habilidades-marcos")]
    public async Task<IActionResult> HabilidadesMarcosPaciente(CancellationToken ct = default)
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

        return Ok(await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(
            db,
            pacienteId.Value,
            DateOnly.FromDateTime(DateTime.UtcNow),
            ct));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/passaporte/evolucao")]
    public async Task<IActionResult> EvolucaoProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);

        return Ok(await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/passaporte/evolucao")]
    public async Task<IActionResult> EvolucaoPaciente(CancellationToken ct = default)
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

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId.Value, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId.Value, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId.Value, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId.Value, hoje, ct);

        return Ok(await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId.Value, hoje, recordes, tempos, resultados, habilidadesMarcos, ct));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/progress-intelligence")]
    public async Task<IActionResult> ProgressIntelligenceProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);

        return Ok(AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/progress-intelligence")]
    public async Task<IActionResult> ProgressIntelligencePaciente(CancellationToken ct = default)
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

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId.Value, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId.Value, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId.Value, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId.Value, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId.Value, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);

        return Ok(AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/progress-intelligence/context")]
    public async Task<IActionResult> ProgressSignalContextProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);

        return Ok(AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/progress-intelligence/context")]
    public async Task<IActionResult> ProgressSignalContextPaciente(CancellationToken ct = default)
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

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId.Value, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId.Value, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId.Value, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId.Value, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId.Value, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);

        return Ok(AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/progress-intelligence/timeline")]
    public async Task<IActionResult> MultiSignalTimelineProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);

        return Ok(AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/progress-intelligence/timeline")]
    public async Task<IActionResult> MultiSignalTimelinePaciente(CancellationToken ct = default)
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

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId.Value, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId.Value, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId.Value, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId.Value, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId.Value, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);

        return Ok(AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/progress-intelligence/windows")]
    public async Task<IActionResult> ProgressEvidenceWindowsProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);
        var timeline = AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto);

        return Ok(AthletePerformancePassportService.MontarJanelasEvidenciaProgresso(timeline, hoje));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/progress-intelligence/windows")]
    public async Task<IActionResult> ProgressEvidenceWindowsPaciente(CancellationToken ct = default)
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

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId.Value, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId.Value, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId.Value, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId.Value, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId.Value, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);
        var timeline = AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto);

        return Ok(AthletePerformancePassportService.MontarJanelasEvidenciaProgresso(timeline, hoje));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/progress-intelligence/observation-map")]
    public async Task<IActionResult> CrossSignalObservationMapProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);
        var timeline = AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto);

        return Ok(AthletePerformancePassportService.MontarMapaObservacaoCruzada(timeline));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/progress-intelligence/observation-map")]
    public async Task<IActionResult> CrossSignalObservationMapPaciente(CancellationToken ct = default)
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

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId.Value, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId.Value, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId.Value, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId.Value, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId.Value, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);
        var timeline = AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto);

        return Ok(AthletePerformancePassportService.MontarMapaObservacaoCruzada(timeline));
    }


    [HttpGet("api/pacientes/{pacienteId:guid}/performance/progress-intelligence/summary")]
    public async Task<IActionResult> ProgressObservationSummaryProfissional(Guid pacienteId, CancellationToken ct = default)
    {
        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);
        var timeline = AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto);
        var janelas = AthletePerformancePassportService.MontarJanelasEvidenciaProgresso(timeline, hoje);
        var mapa = AthletePerformancePassportService.MontarMapaObservacaoCruzada(timeline);

        return Ok(AthletePerformancePassportService.MontarResumoObservacionalProgresso(
            foundation, contexto, timeline, janelas, mapa));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/performance/progress-intelligence/summary")]
    public async Task<IActionResult> ProgressObservationSummaryPaciente(CancellationToken ct = default)
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

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var recordes = await AthletePerformancePassportService.MontarRecordesAsync(db, pacienteId.Value, hoje, ct);
        var tempos = await AthletePerformancePassportService.MontarTemposAsync(db, pacienteId.Value, hoje, ct);
        var resultados = await AthletePerformancePassportService.MontarResultadosCompeticaoTesteAsync(db, pacienteId.Value, hoje, ct);
        var habilidadesMarcos = await AthletePerformancePassportService.MontarHabilidadesMarcosAsync(db, pacienteId.Value, hoje, ct);
        var evolucao = await AthletePerformancePassportService.MontarEvolucaoAsync(
            db, pacienteId.Value, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var foundation = AthletePerformancePassportService.MontarInteligenciaProgresso(
            evolucao, resultados, habilidadesMarcos);
        var contexto = AthletePerformancePassportService.MontarContextoSinaisProgresso(foundation, hoje);
        var timeline = AthletePerformancePassportService.MontarTimelineMultissinal(evolucao, contexto);
        var janelas = AthletePerformancePassportService.MontarJanelasEvidenciaProgresso(timeline, hoje);
        var mapa = AthletePerformancePassportService.MontarMapaObservacaoCruzada(timeline);

        return Ok(AthletePerformancePassportService.MontarResumoObservacionalProgresso(
            foundation, contexto, timeline, janelas, mapa));
    }

}
