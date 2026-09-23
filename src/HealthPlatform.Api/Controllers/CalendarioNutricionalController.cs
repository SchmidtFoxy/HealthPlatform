using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class CalendarioNutricionalController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/calendario-nutricional")]
    public async Task<ActionResult<IReadOnlyCollection<ProgramacaoNutricionalResponse>>> Get(
        Guid pacienteId, [FromQuery] DateOnly inicio, [FromQuery] DateOnly fim, CancellationToken ct)
    {
        if (fim < inicio || fim.DayNumber - inicio.DayNumber > 93)
            return BadRequest(new { message = "Informe um período válido de até 94 dias." });
        if (!await db.Pacientes.AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct))
            return NotFound(new { message = "Paciente não encontrado." });

        var itens = await db.ProgramacoesNutricionaisDia.AsNoTracking()
            .Include(x => x.PlanoAlimentar)
            .Where(x => x.PacienteId == pacienteId && x.Data >= inicio && x.Data <= fim && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Data).ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpPut("api/pacientes/{pacienteId:guid}/calendario-nutricional/{data}")]
    public async Task<ActionResult<ProgramacaoNutricionalResponse>> Put(Guid pacienteId, DateOnly data, UpsertProgramacaoNutricionalRequest request, CancellationToken ct)
    {
        var pacienteOk = await db.Pacientes.AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (!pacienteOk) return NotFound(new { message = "Paciente não encontrado." });
        var plano = await db.PlanosAlimentares.FirstOrDefaultAsync(x => x.Id == request.PlanoAlimentarId && x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (plano is null) return BadRequest(new { message = "O plano alimentar selecionado não pertence a este paciente." });

        var item = await db.ProgramacoesNutricionaisDia.FirstOrDefaultAsync(x => x.PacienteId == pacienteId && x.Data == data, ct);
        if (item is null)
        {
            item = new ProgramacaoNutricionalDia { PacienteId = pacienteId, Data = data };
            db.ProgramacoesNutricionaisDia.Add(item);
        }
        item.PlanoAlimentarId = plano.Id;
        item.Contexto = NormalizarContexto(request.Contexto);
        item.Observacoes = Limpar(request.Observacoes, 800);
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await db.Entry(item).Reference(x => x.PlanoAlimentar).LoadAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpDelete("api/pacientes/{pacienteId:guid}/calendario-nutricional/{data}")]
    public async Task<IActionResult> Delete(Guid pacienteId, DateOnly data, CancellationToken ct)
    {
        var item = await db.ProgramacoesNutricionaisDia.FirstOrDefaultAsync(x => x.PacienteId == pacienteId && x.Data == data && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NoContent();
        db.ProgramacoesNutricionaisDia.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/calendario-nutricional/copiar-semana")]
    public async Task<ActionResult<IReadOnlyCollection<ProgramacaoNutricionalResponse>>> CopiarSemana(Guid pacienteId, CopiarSemanaNutricionalRequest request, CancellationToken ct)
    {
        if (!await db.Pacientes.AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct))
            return NotFound(new { message = "Paciente não encontrado." });
        var origemInicio = InicioSemana(request.DataReferencia);
        var destinoInicio = origemInicio.AddDays(7);
        var origemFim = origemInicio.AddDays(6);
        var origem = await db.ProgramacoesNutricionaisDia.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Data >= origemInicio && x.Data <= origemFim).ToListAsync(ct);
        if (origem.Count == 0) return BadRequest(new { message = "A semana de origem ainda não possui dias programados." });
        var destinoFim = destinoInicio.AddDays(6);
        var existentes = await db.ProgramacoesNutricionaisDia.Where(x => x.PacienteId == pacienteId && x.Data >= destinoInicio && x.Data <= destinoFim).ToListAsync(ct);
        if (!request.Sobrescrever && existentes.Count > 0) return Conflict(new { message = "A próxima semana já possui programação. Marque sobrescrever para substituí-la." });
        if (request.Sobrescrever) db.ProgramacoesNutricionaisDia.RemoveRange(existentes);
        foreach (var x in origem)
            db.ProgramacoesNutricionaisDia.Add(new ProgramacaoNutricionalDia { PacienteId = pacienteId, PlanoAlimentarId = x.PlanoAlimentarId, Data = x.Data.AddDays(7), Contexto = x.Contexto, Observacoes = x.Observacoes });
        await db.SaveChangesAsync(ct);
        var result = await db.ProgramacoesNutricionaisDia.AsNoTracking().Include(x => x.PlanoAlimentar).Where(x => x.PacienteId == pacienteId && x.Data >= destinoInicio && x.Data <= destinoFim).OrderBy(x => x.Data).ToListAsync(ct);
        return Ok(result.Select(ToResponse).ToList());
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/calendario-nutricional/trocar-dias")]
    public async Task<IActionResult> TrocarDias(Guid pacienteId, TrocarDiasNutricionaisRequest request, CancellationToken ct)
    {
        var itens = await db.ProgramacoesNutricionaisDia.Where(x => x.PacienteId == pacienteId && (x.Data == request.DataA || x.Data == request.DataB) && x.Paciente.OrganizacaoId == currentUser.OrganizationId).ToListAsync(ct);
        if (itens.Count != 2) return BadRequest(new { message = "Os dois dias precisam estar programados para realizar a troca." });
        var a = itens.First(x => x.Data == request.DataA); var b = itens.First(x => x.Data == request.DataB);
        (a.PlanoAlimentarId, b.PlanoAlimentarId) = (b.PlanoAlimentarId, a.PlanoAlimentarId);
        (a.Contexto, b.Contexto) = (b.Contexto, a.Contexto);
        (a.Observacoes, b.Observacoes) = (b.Observacoes, a.Observacoes);
        a.UpdatedAtUtc = b.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static DateOnly InicioSemana(DateOnly data)
    {
        var delta = ((int)data.DayOfWeek + 6) % 7;
        return data.AddDays(-delta);
    }
    private static string NormalizarContexto(string? value) => value?.Trim() switch
    {
        "Treino" => "Treino", "Descanso" => "Descanso", "Viagem" => "Viagem", "Flexivel" => "Flexivel", _ => "Padrao"
    };
    private static string? Limpar(string? value, int max) { var t = value?.Trim(); return string.IsNullOrWhiteSpace(t) ? null : t[..Math.Min(t.Length, max)]; }
    private static ProgramacaoNutricionalResponse ToResponse(ProgramacaoNutricionalDia x) => new(x.Id, x.Data, x.PlanoAlimentarId, x.PlanoAlimentar.Nome, x.PlanoAlimentar.Versao, x.PlanoAlimentar.MetaCalorias, x.Contexto, x.Observacoes);
}

public sealed record UpsertProgramacaoNutricionalRequest(Guid PlanoAlimentarId, string? Contexto, string? Observacoes);
public sealed record CopiarSemanaNutricionalRequest(DateOnly DataReferencia, bool Sobrescrever = false);
public sealed record TrocarDiasNutricionaisRequest(DateOnly DataA, DateOnly DataB);
public sealed record ProgramacaoNutricionalResponse(Guid Id, DateOnly Data, Guid PlanoAlimentarId, string PlanoNome, int PlanoVersao, decimal? MetaCalorias, string Contexto, string? Observacoes);
