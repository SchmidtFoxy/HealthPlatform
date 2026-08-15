using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record FilaFollowUpItemResponse(
    Guid PacienteId,
    string PacienteNome,
    string? Telefone,
    string? Email,
    DateTime UltimoContatoUtc,
    string UltimoCanal,
    string UltimoResultado,
    DateTime ProximoContatoUtc,
    int DiasAtraso,
    string Faixa,
    string Prioridade,
    int ContatosUltimos30Dias);

public sealed record FilaFollowUpResumoResponse(
    int Total,
    int Vencidos,
    int Hoje,
    int Proximos7Dias,
    int AltaPrioridade,
    IReadOnlyCollection<FilaFollowUpItemResponse> Itens);

[ApiController]
[Authorize]
[Route("api/followups/fila")]
public sealed class FilaFollowUpController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FilaFollowUpResumoResponse>> Get(
        [FromQuery] string? faixa = "abertos",
        [FromQuery] string? busca = null,
        CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var hoje = DateOnly.FromDateTime(agora);
        var limite30 = agora.AddDays(-30);
        var limite7 = agora.AddDays(7);

        var profissional = await db.Profissionais.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (profissional is null)
            return Forbid();

        var interacoes = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.ProfissionalId == profissional.Id)
            .Select(x => new
            {
                x.Id,
                x.PacienteId,
                PacienteNome = x.Paciente.Nome,
                x.Paciente.Telefone,
                x.Paciente.Email,
                x.DataHoraUtc,
                x.Canal,
                x.Resultado,
                x.ProximoContatoUtc
            })
            .ToListAsync(ct);

        var agrupados = interacoes
            .GroupBy(x => new { x.PacienteId, x.PacienteNome, x.Telefone, x.Email })
            .Select(g =>
            {
                var ultimo = g.OrderByDescending(x => x.DataHoraUtc).First();
                var proximo = g
                    .Where(x => x.ProximoContatoUtc.HasValue)
                    .OrderByDescending(x => x.DataHoraUtc)
                    .Select(x => x.ProximoContatoUtc)
                    .FirstOrDefault();

                if (!proximo.HasValue)
                    return null;

                var data = proximo.Value;
                var dataDia = DateOnly.FromDateTime(data);
                var atraso = dataDia < hoje ? hoje.DayNumber - dataDia.DayNumber : 0;
                var faixaItem = dataDia < hoje ? "Vencido"
                    : dataDia == hoje ? "Hoje"
                    : data <= limite7 ? "Proximos7Dias"
                    : "Futuro";

                var prioridade = atraso >= 7 ? "Alta"
                    : atraso > 0 ? "Media"
                    : faixaItem == "Hoje" ? "Media"
                    : "Normal";

                return new FilaFollowUpItemResponse(
                    g.Key.PacienteId,
                    g.Key.PacienteNome,
                    g.Key.Telefone,
                    g.Key.Email,
                    ultimo.DataHoraUtc,
                    ultimo.Canal,
                    ultimo.Resultado,
                    data,
                    atraso,
                    faixaItem,
                    prioridade,
                    g.Count(x => x.DataHoraUtc >= limite30));
            })
            .Where(x => x is not null)
            .Cast<FilaFollowUpItemResponse>()
            .ToList();

        IEnumerable<FilaFollowUpItemResponse> query = agrupados;

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(x =>
                x.PacienteNome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                (x.Telefone?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Email?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        query = (faixa ?? "abertos").Trim().ToLowerInvariant() switch
        {
            "vencidos" => query.Where(x => x.Faixa == "Vencido"),
            "hoje" => query.Where(x => x.Faixa == "Hoje"),
            "7dias" => query.Where(x => x.Faixa == "Proximos7Dias"),
            "futuro" => query.Where(x => x.Faixa == "Futuro"),
            "todos" => query,
            _ => query.Where(x => x.Faixa != "Futuro")
        };

        var final = query
            .OrderByDescending(x => x.DiasAtraso)
            .ThenBy(x => x.ProximoContatoUtc)
            .ThenBy(x => x.PacienteNome)
            .ToList();

        return Ok(new FilaFollowUpResumoResponse(
            agrupados.Count,
            agrupados.Count(x => x.Faixa == "Vencido"),
            agrupados.Count(x => x.Faixa == "Hoje"),
            agrupados.Count(x => x.Faixa == "Proximos7Dias"),
            agrupados.Count(x => x.Prioridade == "Alta"),
            final));
    }
}
