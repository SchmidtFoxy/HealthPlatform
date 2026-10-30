using HealthPlatform.Api.Contracts.Metas;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record RegistrarAguaRequest(DateOnly Data, int Ml, int? Fuso);
public sealed record LembreteAguaRequest(bool Ativo, int? Fuso);

// Agua da tela Hoje (v0.74.0). A data e o fuso vem do aparelho, para o "hoje" bater com o do paciente.
public sealed partial class MeuPortalPacienteController
{
    [HttpGet("agua")]
    public async Task<IActionResult> MinhaAgua([FromQuery] DateOnly? data, [FromQuery] int? fuso, CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var f = AguaPacienteService.FusoValido(fuso);
        var hoje = DiaDoAparelho(data, f);
        return Ok(await MontarAgua(pacienteId.Value, hoje, f, ct));
    }

    [HttpPost("agua")]
    public async Task<IActionResult> RegistrarAgua(RegistrarAguaRequest request, CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });
        if (request.Ml == 0 || Math.Abs(request.Ml) > 3000)
            return BadRequest(new { message = "Informe entre 1 ml e 3 L." });

        var f = AguaPacienteService.FusoValido(request.Fuso);
        var dia = DiaDoAparelho(request.Data, f);
        var meta = await AguaPacienteService.MetaAtivaAsync(db, pacienteId.Value, dia, ct);

        if (meta is not null)
        {
            var atual = await db.RegistrosMetas.AsNoTracking()
                .Where(x => x.MetaPacienteId == meta.Id && x.Data == dia)
                .Select(x => x.Valor)
                .FirstOrDefaultAsync(ct);
            var totalMl = Math.Max(0, AguaPacienteService.ParaMl(atual ?? 0, meta.Unidade) + request.Ml);
            var valor = Math.Round(AguaPacienteService.DeMl(totalMl, meta.Unidade), 3);
            var r = await RegistrarMeta(meta.Id, new RegistrarMetaRequest(dia, valor, null, null), ct);
            // RegistrarMeta devolve Ok(registro) quando da certo; so repassa o erro (400/404).
            if (r.Result is { } erro and not OkObjectResult) return erro;
        }
        else if (request.Ml > 0)
        {
            var item = new RegistroDiarioPaciente
            {
                PacienteId = pacienteId.Value,
                DataHoraUtc = DateTime.UtcNow,
                Tipo = "Hidratacao",
                Descricao = "Água",
                ValorNumerico = request.Ml / 1000m,
                Unidade = "L"
            };
            db.RegistrosDiarioPaciente.Add(item);
            Auditar("CREATE", nameof(RegistroDiarioPaciente), item.Id, null, new { item.PacienteId, item.DataHoraUtc, item.Tipo, item.ValorNumerico, item.Unidade });
            await db.SaveChangesAsync(ct);
        }
        else
        {
            // Desfazer sem meta: tira dos registros mais recentes do dia (apaga ou diminui).
            var deUtc = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddMinutes(f);
            var ateUtc = deUtc.AddDays(1);
            var itens = await db.RegistrosDiarioPaciente
                .Where(x => x.PacienteId == pacienteId.Value && x.Tipo == "Hidratacao" && x.ValorNumerico != null &&
                            x.DataHoraUtc >= deUtc && x.DataHoraUtc < ateUtc)
                .OrderByDescending(x => x.DataHoraUtc)
                .ToListAsync(ct);
            decimal falta = -request.Ml;
            foreach (var item in itens)
            {
                if (falta <= 0) break;
                var ml = (item.Unidade ?? string.Empty).Trim().ToLowerInvariant() == "ml" ? item.ValorNumerico!.Value : item.ValorNumerico!.Value * 1000m;
                if (ml <= falta)
                {
                    db.RegistrosDiarioPaciente.Remove(item);
                    Auditar("DELETE", nameof(RegistroDiarioPaciente), item.Id, new { item.ValorNumerico, item.Unidade }, null);
                    falta -= ml;
                }
                else
                {
                    var antes = new { item.ValorNumerico, item.Unidade };
                    item.ValorNumerico = (ml - falta) / 1000m;
                    item.Unidade = "L";
                    Auditar("UPDATE", nameof(RegistroDiarioPaciente), item.Id, antes, new { item.ValorNumerico, item.Unidade });
                    falta = 0;
                }
            }
            await db.SaveChangesAsync(ct);
        }

        return Ok(await MontarAgua(pacienteId.Value, dia, f, ct));
    }

    [HttpPut("agua/lembrete")]
    public async Task<IActionResult> LembreteAgua(LembreteAguaRequest request, CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        await AguaPacienteService.SalvarLembreteAsync(db, currentUser.UserId, request.Ativo, AguaPacienteService.FusoValido(request.Fuso), ct);
        var aparelhos = await db.Set<IdentityUserToken<Guid>>().AsNoTracking()
            .CountAsync(x => x.UserId == currentUser.UserId && x.LoginProvider == WebPushNotificationService.LoginProvider, ct);
        return Ok(new { ativo = request.Ativo, aparelhos });
    }

    private static DateOnly DiaDoAparelho(DateOnly? data, int fuso)
    {
        // Aceita a data do aparelho so se estiver a um dia do relogio do servidor.
        var calculado = AguaPacienteService.DiaLocal(DateTime.UtcNow, fuso);
        return data is { } d && Math.Abs(d.DayNumber - calculado.DayNumber) <= 1 ? d : calculado;
    }

    private async Task<AguaPacienteService.ResumoAgua> MontarAgua(Guid pacienteId, DateOnly hoje, int fuso, CancellationToken ct)
        => await AguaPacienteService.MontarAsync(db, pacienteId, currentUser.UserId, hoje, fuso, ct);
}
