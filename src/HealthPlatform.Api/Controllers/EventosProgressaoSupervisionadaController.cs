using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public sealed class EventosProgressaoSupervisionadaController(
    AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record RegistrarEventoProgressaoRequest(
        string Eixo, string Descricao, DateTime? DataAplicacaoUtc, Guid? CicloEsportivoPacienteId, string? Observacoes);

    [HttpGet("api/pacientes/{pacienteId:guid}/progressao-supervisionada/eventos")]
    public async Task<IActionResult> Listar(Guid pacienteId, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var itens = await db.EventosProgressaoSupervisionada.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataAplicacaoUtc)
            .Select(x => new { x.Id, x.Eixo, x.Descricao, x.DataAplicacaoUtc, x.Status, x.EncerradoEmUtc, x.Observacoes, x.CicloEsportivoPacienteId, ProfissionalNome = x.Profissional.Nome })
            .ToListAsync(ct);
        return Ok(itens);
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/progressao-supervisionada/eventos")]
    public async Task<IActionResult> Registrar(Guid pacienteId, RegistrarEventoProgressaoRequest request, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var profissional = await ProfissionalAtual(ct);
        if (profissional is null) return Conflict(new { message = "Perfil profissional ativo nao encontrado." });
        var eixo = NormalizarEixo(request.Eixo);
        if (eixo is null) return BadRequest(new { message = "Eixo deve ser Treino, Nutricao, Recuperacao ou Consistencia." });
        if (string.IsNullOrWhiteSpace(request.Descricao) || request.Descricao.Trim().Length > 1000)
            return BadRequest(new { message = "Descreva objetivamente a mudanca aplicada (ate 1000 caracteres)." });
        if (request.Observacoes?.Trim().Length > 1600) return BadRequest(new { message = "Observacoes devem ter ate 1600 caracteres." });
        var data = request.DataAplicacaoUtc ?? DateTime.UtcNow;
        if (data.Kind == DateTimeKind.Unspecified) data = DateTime.SpecifyKind(data, DateTimeKind.Utc);
        data = data.ToUniversalTime();
        if (data > DateTime.UtcNow.AddMinutes(5)) return BadRequest(new { message = "A aplicacao nao pode estar no futuro." });
        if (request.CicloEsportivoPacienteId.HasValue && !await db.CiclosEsportivosPaciente.AnyAsync(x => x.Id == request.CicloEsportivoPacienteId && x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct))
            return BadRequest(new { message = "Ciclo esportivo informado nao pertence ao paciente." });
        if (await db.EventosProgressaoSupervisionada.AnyAsync(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId && x.Status == "EmObservacao", ct))
            return Conflict(new { message = "Ja existe uma progressao em observacao. Encerre-a antes de registrar outra mudanca." });

        var item = new EventoProgressaoSupervisionada
        {
            OrganizacaoId = currentUser.OrganizationId, PacienteId = pacienteId, ProfissionalId = profissional.Id,
            CicloEsportivoPacienteId = request.CicloEsportivoPacienteId, Eixo = eixo, Descricao = request.Descricao.Trim(),
            DataAplicacaoUtc = data, Status = "EmObservacao", Observacoes = Limpar(request.Observacoes)
        };
        db.EventosProgressaoSupervisionada.Add(item);
        Auditar("CREATE", item.Id, null, item);
        await db.SaveChangesAsync(ct);
        return Ok(new { item.Id, item.Eixo, item.Descricao, item.DataAplicacaoUtc, item.Status, item.CicloEsportivoPacienteId, item.Observacoes });
    }

    [HttpPost("api/progressao-supervisionada/eventos/{id:guid}/encerrar")]
    public async Task<IActionResult> Encerrar(Guid id, CancellationToken ct = default)
    {
        var item = await db.EventosProgressaoSupervisionada.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Evento de progressao nao encontrado." });
        if (item.Status == "Encerrado") return Ok(new { item.Id, item.Status, item.EncerradoEmUtc });
        var antes = new { item.Status, item.EncerradoEmUtc };
        item.Status = "Encerrado"; item.EncerradoEmUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        Auditar("UPDATE", item.Id, antes, new { item.Status, item.EncerradoEmUtc });
        await db.SaveChangesAsync(ct);
        return Ok(new { item.Id, item.Status, item.EncerradoEmUtc });
    }

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private async Task<Profissional?> ProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private static string? NormalizarEixo(string? valor) => (valor ?? "").Trim().ToLowerInvariant() switch
    {
        "treino" => "Treino", "nutricao" or "nutrição" => "Nutricao", "recuperacao" or "recuperação" => "Recuperacao",
        "consistencia" or "consistência" => "Consistencia", _ => null
    };
    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private void Auditar(string acao, Guid id, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog
    {
        OrganizacaoId = currentUser.OrganizationId, UsuarioId = currentUser.UserId, Acao = acao,
        Entidade = nameof(EventoProgressaoSupervisionada), EntidadeId = id.ToString(),
        DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
        DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
        IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
    });
}
