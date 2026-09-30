using HealthPlatform.Api.Contracts.Dashboard;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Medico,Nutricionista,Personal")]
[Route("api/profissional/sinais-diarios")]
public sealed class SinaisDiariosProfissionalController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfessionalDailySignalsResponse>> Get(
        [FromQuery] int dias = 7,
        CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 3, 14);

        var profissionalExiste = await db.Profissionais.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        var admin = User.IsInRole("Admin");
        if (!profissionalExiste && !admin)
            return Forbid();

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = hoje.AddDays(-(dias - 1));
        var inicioUtc = DateTime.SpecifyKind(inicio.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var amanhaUtc = DateTime.SpecifyKind(hoje.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => new { x.Id, x.Nome })
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        var ids = pacientes.Select(x => x.Id).ToArray();

        var prontidoes = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                ids.Contains(x.PacienteId) &&
                x.Data >= inicio &&
                x.Data <= hoje)
            .OrderByDescending(x => x.Data)
            .Select(x => new
            {
                x.PacienteId,
                x.Data,
                x.Score,
                x.RecomendacaoTreino,
                x.MotivoRecomendacao,
                x.SonoHoras,
                x.EnergiaNivel,
                x.DorNivel,
                x.RecuperacaoNivel
            })
            .ToListAsync(ct);

        var fechamentos = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x =>
                ids.Contains(x.PacienteId) &&
                x.Tipo == "FechamentoDia" &&
                x.DataHoraUtc >= inicioUtc &&
                x.DataHoraUtc < amanhaUtc)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new
            {
                x.PacienteId,
                x.DataHoraUtc,
                x.Escala
            })
            .ToListAsync(ct);

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x =>
                ids.Contains(x.PacienteId) &&
                x.Status == "Concluido" &&
                x.DataHoraInicioUtc >= inicioUtc &&
                x.DataHoraInicioUtc < amanhaUtc)
            .Select(x => new
            {
                x.PacienteId,
                x.DataHoraInicioUtc
            })
            .ToListAsync(ct);

        var sinais = new List<ProfessionalDailySignalItemResponse>();

        foreach (var paciente in pacientes)
        {
            var prontidao = prontidoes
                .Where(x => x.PacienteId == paciente.Id)
                .OrderByDescending(x => x.Data)
                .FirstOrDefault();

            var fechamento = fechamentos
                .Where(x => x.PacienteId == paciente.Id)
                .OrderByDescending(x => x.DataHoraUtc)
                .FirstOrDefault();

            var treinosPaciente = treinos
                .Where(x => x.PacienteId == paciente.Id)
                .OrderByDescending(x => x.DataHoraInicioUtc)
                .ToArray();

            int? diasSemCheckIn = prontidao is null
                ? null
                : Math.Max(0, hoje.DayNumber - prontidao.Data.DayNumber);

            var razoes = new List<string>();
            var prioridade = 0;

            if (prontidao is not null && prontidao.Data == hoje)
            {
                if (string.Equals(prontidao.RecomendacaoTreino, "Recuperacao", StringComparison.OrdinalIgnoreCase))
                {
                    prioridade = Math.Max(prioridade, 3);
                    razoes.Add("Prontidao de hoje recomenda recuperacao segundo a regra ja existente.");
                }
                else if (string.Equals(prontidao.RecomendacaoTreino, "Leve", StringComparison.OrdinalIgnoreCase))
                {
                    prioridade = Math.Max(prioridade, 2);
                    razoes.Add("Prontidao de hoje recomenda carga leve segundo a regra ja existente.");
                }
            }

            if (fechamento?.Escala is <= 4)
            {
                prioridade = Math.Max(prioridade, 2);
                razoes.Add($"Ultimo fechamento registrou percepcao {fechamento.Escala}/10.");
            }

            if (prontidao is null || diasSemCheckIn >= 3)
            {
                prioridade = Math.Max(prioridade, 1);
                razoes.Add(prontidao is null
                    ? $"Sem check-in de prontidao nos ultimos {dias} dias."
                    : $"{diasSemCheckIn} dia(s) sem check-in de prontidao.");
            }

            if (prioridade == 0)
                continue;

            var nivel = prioridade switch
            {
                3 => "RevisarHoje",
                2 => "Observar",
                _ => "ContextoPendente"
            };

            sinais.Add(new ProfessionalDailySignalItemResponse(
                paciente.Id,
                paciente.Nome,
                nivel,
                razoes,
                prontidao?.Data,
                diasSemCheckIn,
                prontidao?.Score,
                prontidao?.RecomendacaoTreino,
                prontidao?.MotivoRecomendacao,
                prontidao?.SonoHoras,
                prontidao?.EnergiaNivel,
                prontidao?.DorNivel,
                prontidao?.RecuperacaoNivel,
                fechamento is null ? null : DateOnly.FromDateTime(fechamento.DataHoraUtc),
                fechamento?.Escala,
                treinosPaciente.Length,
                treinosPaciente.FirstOrDefault()?.DataHoraInicioUtc));
        }

        var ordenados = sinais
            .OrderBy(x => x.Nivel == "RevisarHoje" ? 0 : x.Nivel == "Observar" ? 1 : 2)
            .ThenByDescending(x => x.UltimoCheckInData)
            .ThenBy(x => x.PacienteNome)
            .ToArray();

        return Ok(new ProfessionalDailySignalsResponse(
            hoje,
            dias,
            pacientes.Count,
            ordenados.Length,
            ordenados.Count(x => x.Nivel == "RevisarHoje"),
            ordenados.Count(x => x.Nivel == "Observar"),
            ordenados.Count(x => x.Nivel == "ContextoPendente"),
            ordenados,
            "Fila operacional explicavel: Recuperacao = revisar hoje; Leve ou fechamento <= 4/10 = observar; sem check-in por 3+ dias = contexto pendente. Nao e diagnostico, risco clinico ou decisao automatica."));
    }
}
