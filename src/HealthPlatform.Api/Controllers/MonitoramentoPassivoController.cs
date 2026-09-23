using HealthPlatform.Api.Contracts.MonitoramentoPassivo;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
public sealed class MonitoramentoPassivoController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    [Authorize(Policy = "PatientOnly")]
    [HttpPost("api/portal/me/monitoramento-passivo/importar")]
    public async Task<IActionResult> Importar(
        [FromBody] MonitoramentoPassivoImportacaoRequest request,
        CancellationToken ct = default)
    {
        var pacienteId = await PacienteAtualId(ct);
        if (!pacienteId.HasValue) return NotFound(new { message = "Paciente vinculado nao encontrado." });
        if (request.Sinais is null || request.Sinais.Count == 0)
            return BadRequest(new { message = "Envie ao menos um sinal para importacao." });

        var resposta = await MonitoramentoPassivoService.ImportarAsync(db, pacienteId.Value, request, ct);
        return Ok(resposta);
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/monitoramento-passivo")]
    public async Task<IActionResult> MeuResumo([FromQuery] int dias = 14, CancellationToken ct = default)
    {
        var pacienteId = await PacienteAtualId(ct);
        if (!pacienteId.HasValue) return NotFound(new { message = "Paciente vinculado nao encontrado." });
        return Ok(await MonitoramentoPassivoService.ResumirAsync(db, pacienteId.Value, dias, ct));
    }

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/monitoramento-passivo")]
    public async Task<IActionResult> ResumoProfissional(Guid pacienteId, [FromQuery] int dias = 14, CancellationToken ct = default)
    {
        var profissionalExiste = await db.Profissionais.AsNoTracking().AnyAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (!profissionalExiste) return Forbid();

        var pacienteExiste = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (!pacienteExiste) return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await MonitoramentoPassivoService.ResumirAsync(db, pacienteId, dias, ct));
    }

    [Authorize]
    [HttpGet("api/monitoramento-passivo/fontes")]
    public IActionResult Fontes() => Ok(new
    {
        fontes = MonitoramentoPassivoService.FontesSuportadas,
        observacao = "Esta fundacao recebe sinais normalizados. Autorizacao OAuth e sincronizacao nativa de cada provedor entram nos adaptadores de integracao."
    });

    private async Task<Guid?> PacienteAtualId(CancellationToken ct) =>
        await db.Pacientes.AsNoTracking()
            .Where(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
}
