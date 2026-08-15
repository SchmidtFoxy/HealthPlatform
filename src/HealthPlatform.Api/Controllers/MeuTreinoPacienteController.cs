using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Policy = "PatientOnly")]
[Route("api/portal/me/treino")]
public sealed class MeuTreinoPacienteController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Atual(CancellationToken ct)
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

        var plano = await db.PlanosTreino.AsNoTracking()
            .Include(x => x.Profissional)
            .Include(x => x.Sessoes)
                .ThenInclude(x => x.Itens)
                    .ThenInclude(x => x.Exercicio)
            .Where(x =>
                x.PacienteId == pacienteId.Value &&
                x.Status == "Ativo" &&
                x.DataInicio <= hoje &&
                (!x.DataFim.HasValue || x.DataFim.Value >= hoje))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (plano is null)
            return Ok(new { plano = (object?)null });

        return Ok(new
        {
            plano = new
            {
                plano.Id,
                plano.Nome,
                plano.Objetivo,
                plano.DataInicio,
                plano.DataFim,
                plano.Status,
                plano.Observacoes,
                profissional = plano.Profissional.Nome,
                totalSessoes = plano.Sessoes.Count,
                totalExercicios = plano.Sessoes.Sum(x => x.Itens.Count),
                sessoes = plano.Sessoes.OrderBy(x => x.Ordem).Select(s => new
                {
                    s.Id,
                    s.Nome,
                    s.DiasSemana,
                    s.Ordem,
                    s.Observacoes,
                    itens = s.Itens.OrderBy(i => i.Ordem).Select(i => new
                    {
                        i.Id,
                        exercicioId = i.ExercicioId,
                        exercicio = i.Exercicio.Nome,
                        i.Exercicio.GrupoMuscular,
                        i.Exercicio.Equipamento,
                        i.Exercicio.Descricao,
                        i.Exercicio.VideoUrl,
                        i.Series,
                        i.Repeticoes,
                        i.Carga,
                        i.UnidadeCarga,
                        i.DescansoSegundos,
                        i.TempoSegundos,
                        i.Observacoes
                    })
                })
            }
        });
    }
}
