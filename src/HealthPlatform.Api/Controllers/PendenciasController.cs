using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record CriarPendenciaRequest(
    string? OrigemCodigo,
    string? Categoria,
    string? Severidade,
    string Titulo,
    string? Descricao,
    string? ValorReferencia,
    string? AcaoSugerida,
    DateTime? VencimentoUtc);

public sealed record AdiarPendenciaRequest(DateTime AdiadaAteUtc, string? Observacao);
public sealed record ResolverPendenciaRequest(string? Resolucao);
public sealed record CriarRetornoPendenciaRequest(DateTime DataHoraUtc, string? Motivo);

[ApiController]
[Authorize]
public sealed class PendenciasController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pendencias")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? status = null,
        [FromQuery] int limite = 100,
        CancellationToken ct = default)
    {
        limite = Math.Clamp(limite, 1, 300);
        var agora = DateTime.UtcNow;

        var query = db.PendenciasClinicas.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId);

        if (string.IsNullOrWhiteSpace(status) || status.Equals("abertas", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x =>
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= agora));
        }
        else if (!status.Equals("todas", StringComparison.OrdinalIgnoreCase))
        {
            var normalizado = NormalizarStatus(status);
            query = query.Where(x => x.Status == normalizado);
        }

        var itens = await query
            .OrderByDescending(x => x.Severidade == "Alta")
            .ThenByDescending(x => x.Severidade == "Media")
            .ThenBy(x => x.VencimentoUtc ?? DateTime.MaxValue)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(limite)
            .Select(x => new
            {
                x.Id,
                x.PacienteId,
                pacienteNome = x.Paciente.Nome,
                x.ProfissionalId,
                profissionalNome = x.Profissional.Nome,
                x.OrigemCodigo,
                x.Categoria,
                x.Severidade,
                x.Titulo,
                x.Descricao,
                x.ValorReferencia,
                x.AcaoSugerida,
                x.Status,
                x.VencimentoUtc,
                x.VistaEmUtc,
                x.AdiadaAteUtc,
                x.ResolvidaEmUtc,
                x.Resolucao,
                x.ConsultaRetornoId,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(new
        {
            total = itens.Count,
            novas = itens.Count(x => x.Status == "Nova"),
            vistas = itens.Count(x => x.Status == "Vista"),
            adiadas = itens.Count(x => x.Status == "Adiada"),
            resolvidas = itens.Count(x => x.Status == "Resolvida"),
            itens
        });
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/pendencias")]
    public async Task<IActionResult> ListarPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await db.PendenciasClinicas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId &&
                        x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Status == "Resolvida")
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id, x.OrigemCodigo, x.Categoria, x.Severidade,
                x.Titulo, x.Descricao, x.ValorReferencia, x.AcaoSugerida,
                x.Status, x.VencimentoUtc, x.VistaEmUtc, x.AdiadaAteUtc,
                x.ResolvidaEmUtc, x.Resolucao, x.ConsultaRetornoId,
                x.CreatedAtUtc, x.UpdatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(itens);
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/pendencias")]
    public async Task<IActionResult> Criar(
        Guid pacienteId,
        CriarPendenciaRequest request,
        CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var profissional = await ProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        if (string.IsNullOrWhiteSpace(request.Titulo))
            return BadRequest(new { message = "Titulo da pendencia e obrigatorio." });

        var severidade = NormalizarSeveridade(request.Severidade);
        var origem = Limpar(request.OrigemCodigo);

        if (origem is not null)
        {
            var existente = await db.PendenciasClinicas.FirstOrDefaultAsync(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.OrigemCodigo == origem &&
                x.Status != "Resolvida", ct);

            if (existente is not null)
                return Ok(ToResponse(existente));
        }

        var item = new PendenciaClinica
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            OrigemCodigo = origem,
            Categoria = Limpar(request.Categoria) ?? "Acompanhamento",
            Severidade = severidade,
            Titulo = request.Titulo.Trim(),
            Descricao = Limpar(request.Descricao),
            ValorReferencia = Limpar(request.ValorReferencia),
            AcaoSugerida = Limpar(request.AcaoSugerida),
            Status = "Nova",
            VencimentoUtc = request.VencimentoUtc?.ToUniversalTime()
        };

        db.PendenciasClinicas.Add(item);
        Auditar("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);

        return Ok(ToResponse(item));
    }

    [HttpPut("api/pendencias/{id:guid}/vista")]
    public async Task<IActionResult> MarcarVista(Guid id, CancellationToken ct)
    {
        var item = await Obter(id, ct);
        if (item is null) return NotFound();

        var antes = Snapshot(item);
        item.Status = "Vista";
        item.VistaEmUtc ??= DateTime.UtcNow;
        item.AdiadaAteUtc = null;
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("VIEW", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpPut("api/pendencias/{id:guid}/adiar")]
    public async Task<IActionResult> Adiar(
        Guid id,
        AdiarPendenciaRequest request,
        CancellationToken ct)
    {
        var item = await Obter(id, ct);
        if (item is null) return NotFound();

        var quando = request.AdiadaAteUtc.ToUniversalTime();
        if (quando <= DateTime.UtcNow)
            return BadRequest(new { message = "A data de adiamento deve estar no futuro." });

        var antes = Snapshot(item);
        item.Status = "Adiada";
        item.VistaEmUtc ??= DateTime.UtcNow;
        item.AdiadaAteUtc = quando;
        if (!string.IsNullOrWhiteSpace(request.Observacao))
            item.Resolucao = $"Adiada: {request.Observacao.Trim()}";
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("SNOOZE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpPut("api/pendencias/{id:guid}/resolver")]
    public async Task<IActionResult> Resolver(
        Guid id,
        ResolverPendenciaRequest request,
        CancellationToken ct)
    {
        var item = await Obter(id, ct);
        if (item is null) return NotFound();

        var antes = Snapshot(item);
        item.Status = "Resolvida";
        item.VistaEmUtc ??= DateTime.UtcNow;
        item.ResolvidaEmUtc = DateTime.UtcNow;
        item.AdiadaAteUtc = null;
        item.Resolucao = Limpar(request.Resolucao);
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("RESOLVE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [HttpPost("api/pendencias/{id:guid}/retorno")]
    public async Task<IActionResult> CriarRetorno(
        Guid id,
        CriarRetornoPendenciaRequest request,
        CancellationToken ct)
    {
        var item = await Obter(id, ct);
        if (item is null) return NotFound();

        var profissional = await ProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        var data = request.DataHoraUtc.ToUniversalTime();
        if (data <= DateTime.UtcNow)
            return BadRequest(new { message = "O retorno precisa estar no futuro." });

        var consulta = new Consulta
        {
            PacienteId = item.PacienteId,
            ProfissionalId = profissional.Id,
            DataHoraUtc = data,
            Motivo = Limpar(request.Motivo) ?? $"Retorno: {item.Titulo}",
            Status = StatusConsulta.Agendada
        };

        var antes = Snapshot(item);
        db.Consultas.Add(consulta);

        item.ConsultaRetornoId = consulta.Id;
        item.Status = "Vista";
        item.VistaEmUtc ??= DateTime.UtcNow;
        item.VencimentoUtc = data;
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("CREATE_RETURN", item, antes, new
        {
            Pendencia = Snapshot(item),
            ConsultaId = consulta.Id,
            consulta.DataHoraUtc,
            consulta.Motivo
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            pendencia = ToResponse(item),
            consulta = new
            {
                consulta.Id,
                consulta.PacienteId,
                consulta.ProfissionalId,
                consulta.DataHoraUtc,
                consulta.Motivo,
                status = consulta.Status.ToString()
            }
        });
    }

    private async Task<PendenciaClinica?> Obter(Guid id, CancellationToken ct)
        => await db.PendenciasClinicas.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct)
        => await db.Pacientes.AnyAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private async Task<Profissional?> ProfissionalAtual(CancellationToken ct)
        => await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private void Auditar(string acao, PendenciaClinica item, object? antes, object? depois)
        => db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(PendenciaClinica),
            EntidadeId = item.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

    private static object Snapshot(PendenciaClinica x) => new
    {
        x.Id, x.OrganizacaoId, x.PacienteId, x.ProfissionalId,
        x.OrigemCodigo, x.Categoria, x.Severidade, x.Titulo,
        x.Descricao, x.ValorReferencia, x.AcaoSugerida,
        x.Status, x.VencimentoUtc, x.VistaEmUtc,
        x.AdiadaAteUtc, x.ResolvidaEmUtc, x.Resolucao,
        x.ConsultaRetornoId
    };

    private static object ToResponse(PendenciaClinica x) => new
    {
        x.Id, x.PacienteId, x.ProfissionalId,
        x.OrigemCodigo, x.Categoria, x.Severidade, x.Titulo,
        x.Descricao, x.ValorReferencia, x.AcaoSugerida,
        x.Status, x.VencimentoUtc, x.VistaEmUtc,
        x.AdiadaAteUtc, x.ResolvidaEmUtc, x.Resolucao,
        x.ConsultaRetornoId, x.CreatedAtUtc, x.UpdatedAtUtc
    };

    private static string NormalizarStatus(string? valor)
    {
        var x = (valor ?? "").Trim().ToLowerInvariant();
        return x switch
        {
            "nova" => "Nova",
            "vista" => "Vista",
            "adiada" => "Adiada",
            "resolvida" => "Resolvida",
            _ => valor?.Trim() ?? ""
        };
    }

    private static string NormalizarSeveridade(string? valor)
    {
        var x = (valor ?? "").Trim().ToLowerInvariant();
        return x switch
        {
            "alta" => "Alta",
            "baixa" => "Baixa",
            _ => "Media"
        };
    }

    private static string? Limpar(string? x)
        => string.IsNullOrWhiteSpace(x) ? null : x.Trim();
}
