using System.Globalization;
using System.Text;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gestao/export")]
public sealed class GestaoExportController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet("csv")]
    public async Task<IActionResult> Csv(
        [FromQuery] int dias = 30,
        CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 7, 365);
        var fim = DateTime.UtcNow;
        var inicio = fim.AddDays(-dias);
        var org = currentUser.OrganizationId;

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.Ativo)
            .Select(x => new { x.Id, x.Nome, x.Email, x.Telefone, x.CreatedAtUtc })
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        var ids = pacientes.Select(x => x.Id).ToArray();

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.Status })
            .ToListAsync(ct);

        var pendencias = await db.PendenciasClinicas.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .Select(x => new { x.PacienteId, x.Status, x.AdiadaAteUtc })
            .ToListAsync(ct);

        var followups = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.ProximoContatoUtc })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Paciente;Email;Telefone;NovoNoPeriodo;UltimaConsulta;ProximaConsulta;PendenciasAbertas;FollowUpsNoPeriodo;ProximoContato");

        foreach (var p in pacientes)
        {
            var cons = consultas.Where(x => x.PacienteId == p.Id).ToList();

            var ultima = cons
                .Where(x => x.Status == StatusConsulta.Realizada)
                .OrderByDescending(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();

            var proxima = cons
                .Where(x =>
                    x.DataHoraUtc > fim &&
                    x.Status != StatusConsulta.Cancelada &&
                    x.Status != StatusConsulta.Faltou &&
                    x.Status != StatusConsulta.Realizada)
                .OrderBy(x => x.DataHoraUtc)
                .Select(x => (DateTime?)x.DataHoraUtc)
                .FirstOrDefault();

            var abertas = pendencias.Count(x =>
                x.PacienteId == p.Id &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= fim));

            var follow = followups.Where(x => x.PacienteId == p.Id).ToList();
            var followPeriodo = follow.Count(x => x.DataHoraUtc >= inicio && x.DataHoraUtc <= fim);
            var proximoContato = follow
                .Where(x => x.ProximoContatoUtc.HasValue && x.ProximoContatoUtc.Value >= fim)
                .OrderBy(x => x.ProximoContatoUtc)
                .Select(x => x.ProximoContatoUtc)
                .FirstOrDefault();

            sb.AppendLine(string.Join(";",
                CsvCell(p.Nome),
                CsvCell(p.Email),
                CsvCell(p.Telefone),
                p.CreatedAtUtc >= inicio && p.CreatedAtUtc <= fim ? "Sim" : "Nao",
                DateCell(ultima),
                DateCell(proxima),
                abertas.ToString(CultureInfo.InvariantCulture),
                followPeriodo.ToString(CultureInfo.InvariantCulture),
                DateCell(proximoContato)));
        }

        var bom = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, bytes, 0, bom.Length);
        Buffer.BlockCopy(body, 0, bytes, bom.Length, body.Length);

        return File(
            bytes,
            "text/csv; charset=utf-8",
            $"healthplatform-gestao-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet("html")]
    public async Task<IActionResult> Html(
        [FromQuery] int dias = 30,
        CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 7, 365);
        var fim = DateTime.UtcNow;
        var inicio = fim.AddDays(-dias);
        var org = currentUser.OrganizationId;

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.Ativo)
            .ToListAsync(ct);

        var ids = pacientes.Select(x => x.Id).ToArray();

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId) &&
                        x.DataHoraUtc >= inicio &&
                        x.DataHoraUtc <= fim)
            .ToListAsync(ct);

        var realizadas = consultas.Count(x => x.Status == StatusConsulta.Realizada);
        var faltas = consultas.Count(x => x.Status == StatusConsulta.Faltou);
        var canceladas = consultas.Count(x => x.Status == StatusConsulta.Cancelada);
        var baseComparecimento = realizadas + faltas;
        var taxa = baseComparecimento == 0 ? 0m :
            Math.Round((decimal)realizadas / baseComparecimento * 100m, 1);

        var followups = await db.InteracoesAcompanhamento.AsNoTracking()
            .CountAsync(x => x.OrganizacaoId == org &&
                             x.DataHoraUtc >= inicio &&
                             x.DataHoraUtc <= fim, ct);

        var pendAbertas = await db.PendenciasClinicas.AsNoTracking()
            .CountAsync(x => x.OrganizacaoId == org &&
                             x.Status != "Resolvida" &&
                             (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= fim), ct);

        var rows = string.Join(
            Environment.NewLine,
            pacientes
                .OrderBy(x => x.Nome)
                .Select(p =>
                    "<tr><td>" + Html(p.Nome) +
                    "</td><td>" + Html(p.Email) +
                    "</td><td>" + Html(p.Telefone) +
                    "</td></tr>"));

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"pt-BR\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<title>HealthPlatform - Relatorio Gerencial</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Arial,sans-serif;color:#1e293b;margin:36px;background:#fff}");
        html.AppendLine("h1{margin-bottom:4px}.muted{color:#64748b}");
        html.AppendLine(".grid{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:24px 0}");
        html.AppendLine(".card{border:1px solid #dbe2ea;border-radius:12px;padding:14px}");
        html.AppendLine(".card strong{display:block;font-size:24px;margin-bottom:4px}");
        html.AppendLine("table{width:100%;border-collapse:collapse;margin-top:20px}");
        html.AppendLine("th,td{border-bottom:1px solid #e2e8f0;padding:9px;text-align:left;font-size:13px}");
        html.AppendLine("th{background:#f8fafc}");
        html.AppendLine("@media print{body{margin:16px}.no-print{display:none}}");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<button class=\"no-print\" onclick=\"window.print()\">Imprimir</button>");
        html.AppendLine("<h1>Relatorio gerencial HealthPlatform</h1>");
        html.AppendLine(
            "<div class=\"muted\">Periodo: " +
            inicio.ToLocalTime().ToString("dd/MM/yyyy") +
            " a " +
            fim.ToLocalTime().ToString("dd/MM/yyyy") +
            " (" +
            dias +
            " dias)</div>");
        html.AppendLine("<div class=\"grid\">");
        html.AppendLine($"<div class=\"card\"><strong>{pacientes.Count}</strong>Pacientes ativos</div>");
        html.AppendLine($"<div class=\"card\"><strong>{realizadas}</strong>Consultas realizadas</div>");
        html.AppendLine($"<div class=\"card\"><strong>{taxa}%</strong>Comparecimento</div>");
        html.AppendLine($"<div class=\"card\"><strong>{followups}</strong>Follow-ups</div>");
        html.AppendLine($"<div class=\"card\"><strong>{faltas}</strong>Faltas</div>");
        html.AppendLine($"<div class=\"card\"><strong>{canceladas}</strong>Cancelamentos</div>");
        html.AppendLine($"<div class=\"card\"><strong>{pendAbertas}</strong>Pendencias abertas</div>");
        html.AppendLine(
            $"<div class=\"card\"><strong>{pacientes.Count(x => x.CreatedAtUtc >= inicio && x.CreatedAtUtc <= fim)}</strong>Novos pacientes</div>");
        html.AppendLine("</div>");
        html.AppendLine("<table>");
        html.AppendLine("<thead><tr><th>Paciente</th><th>Email</th><th>Telefone</th></tr></thead>");
        html.AppendLine("<tbody>");
        html.AppendLine(rows);
        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return Content(html.ToString(), "text/html; charset=utf-8");
    }

    private static string CsvCell(string? value)
    {
        var text = (value ?? "").Replace("\"", "\"\"");
        return $"\"{text}\"";
    }

    private static string DateCell(DateTime? value)
        => value.HasValue ? value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "";

    private static string Html(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? "");
}
