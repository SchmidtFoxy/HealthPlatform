using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record UpsertEvolucaoClinicaRequest(
    Guid? ConsultaId,
    DateTime? DataHoraUtc,
    string? Subjetivo,
    string? Objetivo,
    string? Avaliacao,
    string? Plano,
    string? Observacoes);

public sealed record EvolucaoClinicaResponse(
    Guid Id,
    Guid PacienteId,
    Guid ProfissionalId,
    string ProfissionalNome,
    Guid? ConsultaId,
    DateTime? ConsultaDataHoraUtc,
    DateTime DataHoraUtc,
    string? Subjetivo,
    string? Objetivo,
    string? Avaliacao,
    string? Plano,
    string? Observacoes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

[ApiController]
[Authorize]
public sealed class EvolucoesClinicasController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/evolucoes")]
    public async Task<ActionResult<IReadOnlyCollection<EvolucaoClinicaResponse>>> Listar(
        Guid pacienteId,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await db.EvolucoesClinicas.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new EvolucaoClinicaResponse(
                x.Id,
                x.PacienteId,
                x.ProfissionalId,
                x.Profissional.Nome,
                x.ConsultaId,
                x.Consulta != null ? x.Consulta.DataHoraUtc : null,
                x.DataHoraUtc,
                x.Subjetivo,
                x.Objetivo,
                x.Avaliacao,
                x.Plano,
                x.Observacoes,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(ct);

        return Ok(itens);
    }

    [HttpGet("api/evolucoes/{id:guid}")]
    public async Task<ActionResult<EvolucaoClinicaResponse>> Obter(Guid id, CancellationToken ct = default)
    {
        var item = await db.EvolucoesClinicas.AsNoTracking()
            .Where(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new EvolucaoClinicaResponse(
                x.Id,
                x.PacienteId,
                x.ProfissionalId,
                x.Profissional.Nome,
                x.ConsultaId,
                x.Consulta != null ? x.Consulta.DataHoraUtc : null,
                x.DataHoraUtc,
                x.Subjetivo,
                x.Objetivo,
                x.Avaliacao,
                x.Plano,
                x.Observacoes,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);

        return item is null ? NotFound(new { message = "Evolucao clinica nao encontrada." }) : Ok(item);
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/evolucoes")]
    public async Task<ActionResult<EvolucaoClinicaResponse>> Criar(
        Guid pacienteId,
        UpsertEvolucaoClinicaRequest request,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var profissional = await ProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        if (!PossuiConteudo(request))
            return BadRequest(new { message = "Informe pelo menos um campo SOAP ou uma observacao." });

        if (!await ConsultaValida(request.ConsultaId, pacienteId, ct))
            return BadRequest(new { message = "Consulta vinculada invalida para este paciente." });

        var item = new EvolucaoClinica
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            ConsultaId = request.ConsultaId,
            DataHoraUtc = (request.DataHoraUtc ?? DateTime.UtcNow).ToUniversalTime(),
            Subjetivo = Limpar(request.Subjetivo),
            Objetivo = Limpar(request.Objetivo),
            Avaliacao = Limpar(request.Avaliacao),
            Plano = Limpar(request.Plano),
            Observacoes = Limpar(request.Observacoes)
        };

        db.EvolucoesClinicas.Add(item);
        AdicionarAuditoria("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obter), new { id = item.Id }, await CarregarResposta(item.Id, ct));
    }

    [HttpPut("api/evolucoes/{id:guid}")]
    public async Task<ActionResult<EvolucaoClinicaResponse>> Atualizar(
        Guid id,
        UpsertEvolucaoClinicaRequest request,
        CancellationToken ct = default)
    {
        var item = await db.EvolucoesClinicas
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Evolucao clinica nao encontrada." });

        var profissional = await ProfissionalAtual(ct);
        if (profissional is null || item.ProfissionalId != profissional.Id)
            return Forbid();

        if (!PossuiConteudo(request))
            return BadRequest(new { message = "Informe pelo menos um campo SOAP ou uma observacao." });

        if (!await ConsultaValida(request.ConsultaId, item.PacienteId, ct))
            return BadRequest(new { message = "Consulta vinculada invalida para este paciente." });

        var antes = Snapshot(item);
        item.ConsultaId = request.ConsultaId;
        item.DataHoraUtc = (request.DataHoraUtc ?? item.DataHoraUtc).ToUniversalTime();
        item.Subjetivo = Limpar(request.Subjetivo);
        item.Objetivo = Limpar(request.Objetivo);
        item.Avaliacao = Limpar(request.Avaliacao);
        item.Plano = Limpar(request.Plano);
        item.Observacoes = Limpar(request.Observacoes);

        AdicionarAuditoria("UPDATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(await CarregarResposta(item.Id, ct));
    }

    private async Task<EvolucaoClinicaResponse> CarregarResposta(Guid id, CancellationToken ct) =>
        await db.EvolucoesClinicas.AsNoTracking()
            .Where(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new EvolucaoClinicaResponse(
                x.Id,
                x.PacienteId,
                x.ProfissionalId,
                x.Profissional.Nome,
                x.ConsultaId,
                x.Consulta != null ? x.Consulta.DataHoraUtc : null,
                x.DataHoraUtc,
                x.Subjetivo,
                x.Objetivo,
                x.Avaliacao,
                x.Plano,
                x.Observacoes,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .FirstAsync(ct);

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private async Task<Profissional?> ProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private async Task<bool> ConsultaValida(Guid? consultaId, Guid pacienteId, CancellationToken ct)
    {
        if (!consultaId.HasValue) return true;
        return await db.Consultas.AsNoTracking().AnyAsync(x =>
            x.Id == consultaId.Value &&
            x.PacienteId == pacienteId &&
            x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
    }

    private void AdicionarAuditoria(string acao, EvolucaoClinica item, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(EvolucaoClinica),
            EntidadeId = item.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static object Snapshot(EvolucaoClinica x) => new
    {
        x.Id,
        x.PacienteId,
        x.ProfissionalId,
        x.ConsultaId,
        x.DataHoraUtc,
        x.Subjetivo,
        x.Objetivo,
        x.Avaliacao,
        x.Plano,
        x.Observacoes
    };

    private static bool PossuiConteudo(UpsertEvolucaoClinicaRequest request) =>
        !string.IsNullOrWhiteSpace(request.Subjetivo) ||
        !string.IsNullOrWhiteSpace(request.Objetivo) ||
        !string.IsNullOrWhiteSpace(request.Avaliacao) ||
        !string.IsNullOrWhiteSpace(request.Plano) ||
        !string.IsNullOrWhiteSpace(request.Observacoes);

    private static string? Limpar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
