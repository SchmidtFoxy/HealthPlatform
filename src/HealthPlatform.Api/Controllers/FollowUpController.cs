using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record RegistrarFollowUpRequest(
    DateTime? DataHoraUtc,
    string Canal,
    string Resultado,
    string? Observacoes,
    DateTime? ProximoContatoUtc);

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/followups")]
public sealed class FollowUpController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        Guid pacienteId,
        [FromQuery] int limite = 50,
        CancellationToken ct = default)
    {
        limite = Math.Clamp(limite, 1, 200);

        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(limite)
            .Select(x => new
            {
                x.Id,
                x.PacienteId,
                x.ProfissionalId,
                profissionalNome = x.Profissional.Nome,
                x.DataHoraUtc,
                x.Canal,
                x.Resultado,
                x.Observacoes,
                x.ProximoContatoUtc,
                x.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(new
        {
            total = itens.Count,
            ultimoContatoUtc = itens.Select(x => (DateTime?)x.DataHoraUtc).FirstOrDefault(),
            proximoContatoUtc = itens
                .Where(x => x.ProximoContatoUtc.HasValue && x.ProximoContatoUtc.Value >= DateTime.UtcNow)
                .OrderBy(x => x.ProximoContatoUtc)
                .Select(x => x.ProximoContatoUtc)
                .FirstOrDefault(),
            itens
        });
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(
        Guid pacienteId,
        RegistrarFollowUpRequest request,
        CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var profissional = await ProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        var canal = NormalizarCanal(request.Canal);
        if (canal is null)
            return BadRequest(new { message = "Canal permitido: Telefone, WhatsApp, Email, Presencial ou Outro." });

        if (string.IsNullOrWhiteSpace(request.Resultado))
            return BadRequest(new { message = "Resultado do contato e obrigatorio." });

        var dataHora = (request.DataHoraUtc ?? DateTime.UtcNow).ToUniversalTime();
        var proximo = request.ProximoContatoUtc?.ToUniversalTime();

        if (proximo.HasValue && proximo.Value <= dataHora)
            return BadRequest(new { message = "Proximo contato deve ocorrer depois do contato atual." });

        var item = new InteracaoAcompanhamento
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            DataHoraUtc = dataHora,
            Canal = canal,
            Resultado = request.Resultado.Trim(),
            Observacoes = Limpar(request.Observacoes),
            ProximoContatoUtc = proximo
        };

        db.InteracoesAcompanhamento.Add(item);
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "CREATE",
            Entidade = nameof(InteracaoAcompanhamento),
            EntidadeId = item.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new
            {
                item.PacienteId,
                item.ProfissionalId,
                item.DataHoraUtc,
                item.Canal,
                item.Resultado,
                item.Observacoes,
                item.ProximoContatoUtc
            }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            item.Id,
            item.PacienteId,
            item.ProfissionalId,
            profissionalNome = profissional.Nome,
            item.DataHoraUtc,
            item.Canal,
            item.Resultado,
            item.Observacoes,
            item.ProximoContatoUtc
        });
    }

    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct)
        => await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private async Task<Profissional?> ProfissionalAtual(CancellationToken ct)
        => await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private static string? NormalizarCanal(string? valor)
    {
        var x = (valor ?? "").Trim().ToLowerInvariant();
        return x switch
        {
            "telefone" => "Telefone",
            "whatsapp" or "whats" => "WhatsApp",
            "email" or "e-mail" => "Email",
            "presencial" => "Presencial",
            "outro" => "Outro",
            _ => null
        };
    }

    private static string? Limpar(string? x)
        => string.IsNullOrWhiteSpace(x) ? null : x.Trim();
}
