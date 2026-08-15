using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dados/resumo")]
public sealed class DadosResumoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var org = currentUser.OrganizationId;

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var profissionais = await db.Profissionais.AsNoTracking()
            .CountAsync(x => x.OrganizacaoId == org && x.Ativo, ct);

        var consultas = await db.Consultas.AsNoTracking()
            .CountAsync(x => pacientes.Contains(x.PacienteId), ct);

        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .CountAsync(x => pacientes.Contains(x.PacienteId), ct);

        var exames = await db.ExamesLaboratoriais.AsNoTracking()
            .CountAsync(x => pacientes.Contains(x.PacienteId), ct);

        var metas = await db.MetasPaciente.AsNoTracking()
            .CountAsync(x => pacientes.Contains(x.PacienteId), ct);

        var diario = await db.RegistrosDiarioPaciente.AsNoTracking()
            .CountAsync(x => pacientes.Contains(x.PacienteId), ct);

        var treinos = await db.PlanosTreino.AsNoTracking()
            .CountAsync(x => pacientes.Contains(x.PacienteId), ct);

        var execucoes = await db.ExecucoesTreino.AsNoTracking()
            .CountAsync(x => pacientes.Contains(x.PacienteId), ct);

        var pendencias = await db.PendenciasClinicas.AsNoTracking()
            .CountAsync(x => x.OrganizacaoId == org, ct);

        var notificacoes = await db.NotificacoesInternas.AsNoTracking()
            .CountAsync(x => x.OrganizacaoId == org && x.Ativa, ct);

        return Ok(new
        {
            organizacaoId = org,
            profissionais,
            pacientes = pacientes.Count,
            consultas,
            avaliacoes,
            exames,
            metas,
            registrosDiario = diario,
            planosTreino = treinos,
            execucoesTreino = execucoes,
            pendencias,
            notificacoes,
            geradoEmUtc = DateTime.UtcNow
        });
    }
}
