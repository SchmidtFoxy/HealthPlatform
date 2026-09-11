using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record CriarSolicitacaoClinicaRequest(
    string? Tipo,
    string Titulo,
    string? Descricao,
    DateTime? DataLimiteUtc);

public sealed record ResponderSolicitacaoClinicaRequest(string? Resposta, string? LinkResposta);
public sealed record RevisarSolicitacaoClinicaRequest(string? Observacao);

[ApiController]
public sealed class SolicitacoesClinicasController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [Authorize]
    [HttpGet("api/solicitacoes")]
    public async Task<IActionResult> FilaProfissional(
        [FromQuery] string? status,
        [FromQuery] string? busca,
        [FromQuery] string? prazo,
        CancellationToken ct)
    {
        if (await ProfissionalAtual(ct) is null) return Forbid();

        var agora = DateTime.UtcNow;
        var baseQuery = db.SolicitacoesClinicas.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId);

        var pendentes = await baseQuery.CountAsync(x => x.Status == "Pendente", ct);
        var aguardandoRevisao = await baseQuery.CountAsync(x => x.Status == "Enviada", ct);
        var vencidas = await baseQuery.CountAsync(x => x.Status == "Pendente" && x.DataLimiteUtc.HasValue && x.DataLimiteUtc < agora, ct);

        var query = baseQuery;
        var statusLimpo = Limpar(status);
        if (statusLimpo is not null && !statusLimpo.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Status == statusLimpo);

        var prazoLimpo = Limpar(prazo);
        if (prazoLimpo?.Equals("Vencidas", StringComparison.OrdinalIgnoreCase) == true)
            query = query.Where(x => x.Status == "Pendente" && x.DataLimiteUtc.HasValue && x.DataLimiteUtc < agora);
        else if (prazoLimpo?.Equals("Proximas24h", StringComparison.OrdinalIgnoreCase) == true)
        {
            var limite = agora.AddHours(24);
            query = query.Where(x => x.Status == "Pendente" && x.DataLimiteUtc.HasValue && x.DataLimiteUtc >= agora && x.DataLimiteUtc <= limite);
        }

        var termo = Limpar(busca);
        if (termo is not null)
        {
            var like = $"%{termo}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Paciente.Nome, like) ||
                EF.Functions.ILike(x.Titulo, like) ||
                EF.Functions.ILike(x.Tipo, like));
        }

        var itens = await query
            .OrderBy(x => x.Status == "Enviada" ? 0 : x.Status == "Pendente" ? 1 : 2)
            .ThenBy(x => x.DataLimiteUtc ?? DateTime.MaxValue)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .Select(x => new
            {
                x.Id, x.PacienteId, pacienteNome = x.Paciente.Nome,
                x.ProfissionalId, profissionalNome = x.Profissional.Nome,
                x.Tipo, x.Titulo, x.Descricao, x.DataLimiteUtc, x.Status,
                x.RespostaPaciente, x.LinkResposta, x.RespondidaEmUtc,
                x.RevisadaEmUtc, x.ObservacaoRevisao, x.CreatedAtUtc, x.UpdatedAtUtc
            })
            .Take(250)
            .ToListAsync(ct);

        return Ok(new
        {
            resumo = new { pendentes, aguardandoRevisao, vencidas, total = pendentes + aguardandoRevisao },
            itens
        });
    }

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/solicitacoes")]
    public async Task<IActionResult> ListarDoPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await db.SolicitacoesClinicas.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId)
            .OrderBy(x => x.Status == "Pendente" ? 0 : x.Status == "Enviada" ? 1 : 2)
            .ThenBy(x => x.DataLimiteUtc ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id, x.PacienteId, x.ProfissionalId,
                profissionalNome = x.Profissional.Nome,
                x.Tipo, x.Titulo, x.Descricao, x.DataLimiteUtc, x.Status,
                x.RespostaPaciente, x.LinkResposta, x.RespondidaEmUtc,
                x.RevisadaEmUtc, x.ObservacaoRevisao, x.CreatedAtUtc, x.UpdatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(itens);
    }

    [Authorize]
    [HttpPost("api/pacientes/{pacienteId:guid}/solicitacoes")]
    public async Task<IActionResult> Criar(Guid pacienteId, CriarSolicitacaoClinicaRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var profissional = await ProfissionalAtual(ct);
        if (profissional is null) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Titulo))
            return BadRequest(new { message = "Titulo da solicitacao e obrigatorio." });
        if (request.Titulo.Trim().Length > 300)
            return BadRequest(new { message = "Titulo deve ter no maximo 300 caracteres." });
        if ((request.Tipo?.Trim().Length ?? 0) > 60 || (request.Descricao?.Trim().Length ?? 0) > 3000)
            return BadRequest(new { message = "Conteudo da solicitacao excede o limite permitido." });

        var item = new SolicitacaoClinica
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            Tipo = Limpar(request.Tipo) ?? "Acompanhamento",
            Titulo = request.Titulo.Trim(),
            Descricao = Limpar(request.Descricao),
            DataLimiteUtc = request.DataLimiteUtc?.ToUniversalTime(),
            Status = "Pendente"
        };

        db.SolicitacoesClinicas.Add(item);
        Auditar("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [Authorize]
    [HttpPut("api/solicitacoes/{id:guid}/revisar")]
    public async Task<IActionResult> Revisar(Guid id, RevisarSolicitacaoClinicaRequest request, CancellationToken ct)
    {
        var item = await ObterProfissional(id, ct);
        if (item is null) return NotFound();
        if (item.Status != "Enviada")
            return BadRequest(new { message = "Somente solicitacoes enviadas pelo paciente podem ser revisadas." });

        var antes = Snapshot(item);
        item.Status = "Revisada";
        item.RevisadaEmUtc = DateTime.UtcNow;
        item.ObservacaoRevisao = Limpar(request.Observacao);
        Auditar("REVIEW", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [Authorize]
    [HttpPut("api/solicitacoes/{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        var item = await ObterProfissional(id, ct);
        if (item is null) return NotFound();
        if (item.Status is "Revisada" or "Cancelada")
            return BadRequest(new { message = "Solicitacao ja encerrada." });

        var antes = Snapshot(item);
        item.Status = "Cancelada";
        Auditar("CANCEL", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/solicitacoes")]
    public async Task<IActionResult> MinhasSolicitacoes(CancellationToken ct)
    {
        var paciente = await MeuPaciente(ct);
        if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var itens = await db.SolicitacoesClinicas.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == paciente.Id)
            .OrderBy(x => x.Status == "Pendente" ? 0 : x.Status == "Enviada" ? 1 : 2)
            .ThenBy(x => x.DataLimiteUtc ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id, x.Tipo, x.Titulo, x.Descricao, x.DataLimiteUtc, x.Status,
                profissionalNome = x.Profissional.Nome,
                x.RespostaPaciente, x.LinkResposta, x.RespondidaEmUtc,
                x.RevisadaEmUtc, x.ObservacaoRevisao, x.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(new
        {
            pendentes = itens.Count(x => x.Status == "Pendente"),
            aguardandoRevisao = itens.Count(x => x.Status == "Enviada"),
            itens
        });
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpPost("api/portal/me/solicitacoes/{id:guid}/responder")]
    public async Task<IActionResult> Responder(Guid id, ResponderSolicitacaoClinicaRequest request, CancellationToken ct)
    {
        var paciente = await MeuPaciente(ct);
        if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var item = await db.SolicitacoesClinicas.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == paciente.Id, ct);
        if (item is null) return NotFound();
        if (item.Status != "Pendente")
            return BadRequest(new { message = "Esta solicitacao nao esta mais pendente." });

        var resposta = Limpar(request.Resposta);
        var link = Limpar(request.LinkResposta);
        if ((resposta?.Length ?? 0) > 4000 || (link?.Length ?? 0) > 1000)
            return BadRequest(new { message = "Resposta excede o limite permitido." });
        if (link is not null && (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            return BadRequest(new { message = "O link deve usar http ou https." });
        if (resposta is null && link is null)
            return BadRequest(new { message = "Informe uma resposta ou um link/documento de referencia." });

        var antes = Snapshot(item);
        item.RespostaPaciente = resposta;
        item.LinkResposta = link;
        item.RespondidaEmUtc = DateTime.UtcNow;
        item.Status = "Enviada";
        Auditar("PATIENT_RESPONSE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    private async Task<SolicitacaoClinica?> ObterProfissional(Guid id, CancellationToken ct)
    {
        if (await ProfissionalAtual(ct) is null) return null;
        return await db.SolicitacoesClinicas.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
    }

    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct)
        => await db.Pacientes.AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private async Task<Profissional?> ProfissionalAtual(CancellationToken ct)
        => await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private async Task<Paciente?> MeuPaciente(CancellationToken ct)
        => await db.Pacientes.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private void Auditar(string acao, SolicitacaoClinica item, object? antes, object? depois)
        => db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(SolicitacaoClinica),
            EntidadeId = item.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

    private static object Snapshot(SolicitacaoClinica x) => new
    {
        x.Id, x.OrganizacaoId, x.PacienteId, x.ProfissionalId,
        x.Tipo, x.Titulo, x.Descricao, x.DataLimiteUtc, x.Status,
        x.RespostaPaciente, x.LinkResposta, x.RespondidaEmUtc,
        x.RevisadaEmUtc, x.ObservacaoRevisao
    };

    private static object ToResponse(SolicitacaoClinica x) => new
    {
        x.Id, x.PacienteId, x.ProfissionalId, x.Tipo, x.Titulo, x.Descricao,
        x.DataLimiteUtc, x.Status, x.RespostaPaciente, x.LinkResposta,
        x.RespondidaEmUtc, x.RevisadaEmUtc, x.ObservacaoRevisao,
        x.CreatedAtUtc, x.UpdatedAtUtc
    };

    private static string? Limpar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
