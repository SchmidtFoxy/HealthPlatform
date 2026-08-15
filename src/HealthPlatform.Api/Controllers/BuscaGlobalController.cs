using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record BuscaGlobalItemResponse(
    string Tipo,
    Guid Id,
    Guid? PacienteId,
    string Titulo,
    string? Subtitulo,
    DateTime? DataUtc,
    string Destino,
    string? Severidade);

public sealed record BuscaGlobalResponse(
    string Termo,
    int Total,
    IReadOnlyCollection<BuscaGlobalItemResponse> Itens);

[ApiController]
[Authorize]
[Route("api/busca")]
public sealed class BuscaGlobalController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BuscaGlobalResponse>> Get(
        [FromQuery] string? termo,
        [FromQuery] int limite = 30,
        CancellationToken ct = default)
    {
        termo = (termo ?? "").Trim();
        limite = Math.Clamp(limite, 5, 100);

        if (termo.Length < 2)
            return Ok(new BuscaGlobalResponse(termo, 0, Array.Empty<BuscaGlobalItemResponse>()));

        var org = currentUser.OrganizationId;
        var pattern = $"%{termo}%";
        var itens = new List<BuscaGlobalItemResponse>();

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == org &&
                (EF.Functions.ILike(x.Nome, pattern) ||
                 (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                 (x.Telefone != null && EF.Functions.ILike(x.Telefone, pattern)) ||
                 (x.Cpf != null && EF.Functions.ILike(x.Cpf, pattern))))
            .OrderBy(x => x.Nome)
            .Take(limite)
            .Select(x => new { x.Id, x.Nome, x.Email, x.Telefone, x.Ativo })
            .ToListAsync(ct);

        itens.AddRange(pacientes.Select(x => new BuscaGlobalItemResponse(
            "Paciente",
            x.Id,
            x.Id,
            x.Nome,
            string.Join(" • ", new[] { x.Email, x.Telefone }.Where(v => !string.IsNullOrWhiteSpace(v))),
            null,
            "prontuario",
            x.Ativo ? null : "Inativo")));

        if (itens.Count < limite)
        {
            var pendencias = await db.PendenciasClinicas.AsNoTracking()
                .Where(x =>
                    x.OrganizacaoId == org &&
                    x.Status != "Resolvida" &&
                    (EF.Functions.ILike(x.Titulo, pattern) ||
                     (x.Descricao != null && EF.Functions.ILike(x.Descricao, pattern)) ||
                     EF.Functions.ILike(x.Paciente.Nome, pattern)))
                .OrderByDescending(x => x.Severidade == "Alta")
                .ThenBy(x => x.VencimentoUtc)
                .Take(limite - itens.Count)
                .Select(x => new
                {
                    x.Id,
                    x.PacienteId,
                    PacienteNome = x.Paciente.Nome,
                    x.Titulo,
                    x.Descricao,
                    x.VencimentoUtc,
                    x.Severidade
                })
                .ToListAsync(ct);

            itens.AddRange(pendencias.Select(x => new BuscaGlobalItemResponse(
                "Pendência",
                x.Id,
                x.PacienteId,
                x.Titulo,
                x.PacienteNome,
                x.VencimentoUtc,
                "pendencias",
                x.Severidade)));
        }

        if (itens.Count < limite)
        {
            var followups = await db.InteracoesAcompanhamento.AsNoTracking()
                .Where(x =>
                    x.OrganizacaoId == org &&
                    (EF.Functions.ILike(x.Resultado, pattern) ||
                     EF.Functions.ILike(x.Paciente.Nome, pattern)))
                .OrderByDescending(x => x.DataHoraUtc)
                .Take(limite - itens.Count)
                .Select(x => new
                {
                    x.Id,
                    x.PacienteId,
                    PacienteNome = x.Paciente.Nome,
                    x.Resultado,
                    x.Canal,
                    x.DataHoraUtc,
                    x.ProximoContatoUtc
                })
                .ToListAsync(ct);

            itens.AddRange(followups.Select(x => new BuscaGlobalItemResponse(
                "Follow-up",
                x.Id,
                x.PacienteId,
                x.PacienteNome,
                $"{x.Canal} • {x.Resultado}",
                x.ProximoContatoUtc ?? x.DataHoraUtc,
                "followups",
                null)));
        }

        if (itens.Count < limite)
        {
            var consultas = await db.Consultas.AsNoTracking()
                .Where(x =>
                    x.Paciente.OrganizacaoId == org &&
                    (EF.Functions.ILike(x.Paciente.Nome, pattern) ||
                     (x.Motivo != null && EF.Functions.ILike(x.Motivo, pattern))))
                .OrderByDescending(x => x.DataHoraUtc)
                .Take(limite - itens.Count)
                .Select(x => new
                {
                    x.Id,
                    x.PacienteId,
                    PacienteNome = x.Paciente.Nome,
                    x.Motivo,
                    x.DataHoraUtc,
                    Status = x.Status.ToString()
                })
                .ToListAsync(ct);

            itens.AddRange(consultas.Select(x => new BuscaGlobalItemResponse(
                "Consulta",
                x.Id,
                x.PacienteId,
                x.PacienteNome,
                string.Join(" • ", new[] { x.Status, x.Motivo }.Where(v => !string.IsNullOrWhiteSpace(v))),
                x.DataHoraUtc,
                "agenda",
                null)));
        }

        var final = itens
            .Take(limite)
            .ToList();

        return Ok(new BuscaGlobalResponse(termo, final.Count, final));
    }
}
