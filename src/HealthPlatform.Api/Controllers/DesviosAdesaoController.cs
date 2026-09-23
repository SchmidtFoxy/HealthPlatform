using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/desvios-adesao")]
public sealed class DesviosAdesaoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        Guid pacienteId,
        [FromQuery] int dias = 30,
        CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 7, 180);

        var paciente = await db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });

        var desde = DateTime.UtcNow.AddDays(-dias);
        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == EventoDesvioAdesaoService.TipoRegistro &&
                        x.DataHoraUtc >= desde)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(250)
            .ToListAsync(ct);

        var eventos = registros.Select(EventoDesvioAdesaoService.Ler).Where(x => x is not null).Cast<EventoDesvioAdesaoLeitura>().ToArray();
        var ultimos7 = DateTime.UtcNow.AddDays(-7);
        var recorrencias = eventos
            .GroupBy(x => new { x.Categoria, x.Tipo })
            .Select(g => new
            {
                g.Key.Categoria,
                g.Key.Tipo,
                Total = g.Count(),
                UltimoEventoUtc = g.Max(x => x.DataHoraUtc),
                Prioridade = g.Max(x => x.Prioridade),
                Recorrente = g.Count() >= 2
            })
            .OrderByDescending(x => x.Prioridade)
            .ThenByDescending(x => x.Total)
            .ToArray();

        return Ok(new
        {
            pacienteId,
            paciente = paciente.Nome,
            dias,
            total = eventos.Length,
            ultimos7Dias = eventos.Count(x => x.DataHoraUtc >= ultimos7),
            prioridadeMaior = eventos.Length == 0 ? 0 : eventos.Max(x => x.Prioridade),
            recorrencias = recorrencias.Where(x => x.Recorrente).ToArray(),
            categorias = eventos.GroupBy(x => x.Categoria).Select(g => new { categoria = g.Key, total = g.Count() }).OrderByDescending(x => x.total),
            eventos = eventos.Select(x => new
            {
                x.Id,
                x.DataHoraUtc,
                x.Categoria,
                x.Tipo,
                x.Titulo,
                x.Detalhe,
                x.Prioridade,
                x.Contexto
            })
        });
    }
}
