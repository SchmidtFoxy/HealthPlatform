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
    [HttpGet("todos")]
    public async Task<IActionResult> Todos(CancellationToken ct)
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
        var planos = await db.PlanosTreino.AsNoTracking()
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
            .ToListAsync(ct);

        return Ok(new
        {
            total = planos.Count,
            permiteInicioLivre = true,
            planos = planos.Select(plano => new
            {
                plano.Id,
                plano.Nome,
                plano.Objetivo,
                plano.DataInicio,
                plano.DataFim,
                plano.Status,
                plano.Observacoes,
                plano.Versao,
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
            })
        });
    }

    [HttpGet("alternativas/{itemTreinoId:guid}")]
    public async Task<IActionResult> Alternativas(Guid itemTreinoId, [FromQuery] string? motivo, CancellationToken ct)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var item = await db.ItensTreino.AsNoTracking()
            .Include(x => x.Exercicio)
            .Include(x => x.SessaoTreino).ThenInclude(x => x.PlanoTreino)
            .FirstOrDefaultAsync(x => x.Id == itemTreinoId &&
                x.SessaoTreino.PlanoTreino.PacienteId == pacienteId.Value &&
                x.SessaoTreino.PlanoTreino.Status == "Ativo", ct);

        if (item is null)
            return NotFound(new { message = "Exercicio prescrito nao encontrado no plano ativo." });

        var motivoNormalizado = (motivo ?? string.Empty).Trim();
        if (motivoNormalizado.Equals("DorDesconforto", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                itemTreinoId,
                exercicioOriginal = item.Exercicio.Nome,
                bloqueadoPorSeguranca = true,
                mensagem = "Dor ou desconforto merece revisao profissional. Interrompa o exercicio se necessario e converse com seu profissional antes de substituir por conta propria.",
                alternativas = Array.Empty<object>()
            });
        }

        var catalogo = await db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo && x.Id != item.ExercicioId)
            .Select(x => new { x.Id, x.Nome, x.GrupoMuscular, x.Equipamento, x.Descricao, x.VideoUrl })
            .ToListAsync(ct);

        static bool Eq(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) &&
            string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

        var grupo = item.Exercicio.GrupoMuscular;
        var equipamento = item.Exercicio.Equipamento;
        var alternativas = catalogo
            .Where(x => Eq(x.GrupoMuscular, grupo))
            .Select(x =>
            {
                var score = 70;
                if (Eq(x.Equipamento, equipamento)) score += 18;
                if (motivoNormalizado.Equals("EquipamentoOcupado", StringComparison.OrdinalIgnoreCase) && !Eq(x.Equipamento, equipamento)) score += 12;
                if (motivoNormalizado.Equals("SemEquipamento", StringComparison.OrdinalIgnoreCase))
                {
                    var eq = (x.Equipamento ?? string.Empty).ToLowerInvariant();
                    if (eq.Contains("peso corporal") || eq.Contains("halter") || eq.Contains("elast")) score += 16;
                }
                return new
                {
                    exercicioId = x.Id,
                    x.Nome,
                    x.GrupoMuscular,
                    x.Equipamento,
                    x.Descricao,
                    x.VideoUrl,
                    compatibilidadePercentual = Math.Clamp(score, 0, 100)
                };
            })
            .OrderByDescending(x => x.compatibilidadePercentual)
            .ThenBy(x => x.Nome)
            .Take(6)
            .ToList();

        return Ok(new
        {
            itemTreinoId,
            exercicioOriginal = item.Exercicio.Nome,
            grupoMuscular = grupo,
            motivo = motivoNormalizado,
            bloqueadoPorSeguranca = false,
            mensagem = alternativas.Count == 0
                ? "Nenhuma alternativa do mesmo grupo muscular foi encontrada no catalogo profissional."
                : "Alternativas do catalogo organizadas por similaridade funcional. Preserve a tecnica e siga as orientacoes do seu profissional.",
            alternativas
        });
    }

}
