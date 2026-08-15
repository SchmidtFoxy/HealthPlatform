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
public sealed class FasesTreinoController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record CriarFaseTreinoRequest(
        string Nome,
        string Tipo,
        string? Objetivo,
        DateOnly DataInicio,
        DateOnly? DataFim,
        Guid? PlanoTreinoId,
        decimal? MetaPesoKg,
        int? MetaAdesaoPercentual,
        int? DuracaoMinimaDias,
        string? CriterioTransicao,
        string? Observacoes);

    public sealed record AtualizarFaseTreinoRequest(
        string Nome,
        string Tipo,
        string? Objetivo,
        DateOnly DataInicio,
        DateOnly? DataFim,
        Guid? PlanoTreinoId,
        string Status,
        decimal? MetaPesoKg,
        int? MetaAdesaoPercentual,
        int? DuracaoMinimaDias,
        string? CriterioTransicao,
        string? Observacoes);

    public sealed record ReordenarFaseTreinoRequest(Guid FaseId, int Ordem);
    public sealed record ReordenarFasesTreinoRequest(IReadOnlyCollection<ReordenarFaseTreinoRequest> Fases);

    [HttpGet("api/pacientes/{pacienteId:guid}/fases-treino")]
    public async Task<IActionResult> Listar(Guid pacienteId, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await db.FasesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .Include(x => x.Profissional)
            .Include(x => x.PlanoTreino)
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.DataInicio)
            .ToListAsync(ct);

        return Ok(itens.Select(x => ToResponse(x)).ToList());
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/fases-treino")]
    public async Task<IActionResult> Criar(Guid pacienteId, CriarFaseTreinoRequest request, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var erro = Validar(
            request.Nome, request.Tipo, request.DataInicio, request.DataFim, "Planejada",
            request.MetaPesoKg, request.MetaAdesaoPercentual, request.DuracaoMinimaDias, request.CriterioTransicao);
        if (erro is not null)
            return BadRequest(new { message = erro });

        if (!await PlanoValido(request.PlanoTreinoId, pacienteId, ct))
            return BadRequest(new { message = "Plano de treino vinculado nao pertence ao paciente/organizacao." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        var maiorOrdem = await db.FasesTreino
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => (int?)x.Ordem)
            .MaxAsync(ct) ?? 0;

        var fase = new FaseTreino
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            PlanoTreinoId = request.PlanoTreinoId,
            Nome = request.Nome.Trim(),
            Tipo = NormalizarTipo(request.Tipo),
            Objetivo = Limpar(request.Objetivo),
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Ordem = maiorOrdem + 1,
            Status = "Planejada",
            Observacoes = Limpar(request.Observacoes),
            MetaPesoKg = request.MetaPesoKg,
            MetaAdesaoPercentual = request.MetaAdesaoPercentual,
            DuracaoMinimaDias = request.DuracaoMinimaDias,
            CriterioTransicao = Limitar(request.CriterioTransicao, 1000)
        };

        db.FasesTreino.Add(fase);
        Auditar("CREATE", fase, null, Snapshot(fase));
        await db.SaveChangesAsync(ct);

        await db.Entry(fase).Reference(x => x.Profissional).LoadAsync(ct);
        if (fase.PlanoTreinoId.HasValue)
            await db.Entry(fase).Reference(x => x.PlanoTreino).LoadAsync(ct);

        return Ok(ToResponse(fase));
    }

    [HttpPut("api/fases-treino/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, AtualizarFaseTreinoRequest request, CancellationToken ct = default)
    {
        var fase = await db.FasesTreino
            .Include(x => x.Profissional)
            .Include(x => x.PlanoTreino)
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (fase is null)
            return NotFound(new { message = "Fase de treino nao encontrada." });

        var erro = Validar(
            request.Nome, request.Tipo, request.DataInicio, request.DataFim, request.Status,
            request.MetaPesoKg, request.MetaAdesaoPercentual, request.DuracaoMinimaDias, request.CriterioTransicao);
        if (erro is not null)
            return BadRequest(new { message = erro });

        if (!await PlanoValido(request.PlanoTreinoId, fase.PacienteId, ct))
            return BadRequest(new { message = "Plano de treino vinculado nao pertence ao paciente/organizacao." });

        var antes = Snapshot(fase);
        fase.Nome = request.Nome.Trim();
        fase.Tipo = NormalizarTipo(request.Tipo);
        fase.Objetivo = Limpar(request.Objetivo);
        fase.DataInicio = request.DataInicio;
        fase.DataFim = request.DataFim;
        fase.PlanoTreinoId = request.PlanoTreinoId;
        fase.Status = NormalizarStatus(request.Status);
        fase.Observacoes = Limpar(request.Observacoes);
        fase.MetaPesoKg = request.MetaPesoKg;
        fase.MetaAdesaoPercentual = request.MetaAdesaoPercentual;
        fase.DuracaoMinimaDias = request.DuracaoMinimaDias;
        fase.CriterioTransicao = Limitar(request.CriterioTransicao, 1000);
        fase.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("UPDATE", fase, antes, Snapshot(fase));
        await db.SaveChangesAsync(ct);

        return Ok(ToResponse(fase));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/fases-treino/reordenar")]
    public async Task<IActionResult> Reordenar(Guid pacienteId, ReordenarFasesTreinoRequest request, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        if (request.Fases is null || request.Fases.Count == 0)
            return BadRequest(new { message = "Informe as fases para reordenacao." });

        if (request.Fases.Select(x => x.FaseId).Distinct().Count() != request.Fases.Count)
            return BadRequest(new { message = "Fase repetida na reordenacao." });

        if (request.Fases.Any(x => x.Ordem <= 0))
            return BadRequest(new { message = "A ordem deve ser maior que zero." });

        var fases = await db.FasesTreino
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .ToListAsync(ct);

        var idsExistentes = fases.Select(x => x.Id).OrderBy(x => x).ToArray();
        var idsRecebidos = request.Fases.Select(x => x.FaseId).OrderBy(x => x).ToArray();

        if (!idsExistentes.SequenceEqual(idsRecebidos))
            return BadRequest(new { message = "A reordenacao deve incluir exatamente todas as fases do paciente." });

        if (request.Fases.GroupBy(x => x.Ordem).Any(g => g.Count() > 1))
            return BadRequest(new { message = "Cada fase deve possuir uma ordem unica." });

        foreach (var item in request.Fases)
        {
            var fase = fases.First(x => x.Id == item.FaseId);
            fase.Ordem = item.Ordem;
            fase.UpdatedAtUtc = DateTime.UtcNow;
        }

        AuditarGenerico("REORDER", pacienteId.ToString(), request.Fases);
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Fases de treino reordenadas." });
    }

    [HttpDelete("api/fases-treino/{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct = default)
    {
        var fase = await db.FasesTreino.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (fase is null)
            return NotFound(new { message = "Fase de treino nao encontrada." });

        if (fase.Status == "EmAndamento")
            return Conflict(new { message = "Finalize ou altere o status da fase em andamento antes de excluir." });

        var antes = Snapshot(fase);
        db.FasesTreino.Remove(fase);
        Auditar("DELETE", fase, antes, null);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private async Task<bool> PlanoValido(Guid? planoId, Guid pacienteId, CancellationToken ct)
    {
        if (!planoId.HasValue)
            return true;

        return await db.PlanosTreino.AnyAsync(x =>
            x.Id == planoId.Value &&
            x.PacienteId == pacienteId &&
            x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private static string? Validar(
        string nome,
        string tipo,
        DateOnly inicio,
        DateOnly? fim,
        string status,
        decimal? metaPesoKg,
        int? metaAdesaoPercentual,
        int? duracaoMinimaDias,
        string? criterioTransicao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return "Nome da fase e obrigatorio.";
        if (string.IsNullOrWhiteSpace(tipo))
            return "Tipo da fase e obrigatorio.";
        if (fim.HasValue && fim.Value < inicio)
            return "Data final nao pode ser anterior a data inicial.";

        if (metaPesoKg.HasValue && (metaPesoKg.Value < 20m || metaPesoKg.Value > 400m))
            return "Meta de peso deve ficar entre 20 e 400 kg.";

        if (metaAdesaoPercentual.HasValue && (metaAdesaoPercentual.Value < 0 || metaAdesaoPercentual.Value > 100))
            return "Meta de adesao deve ficar entre 0 e 100%.";

        if (duracaoMinimaDias.HasValue && (duracaoMinimaDias.Value < 1 || duracaoMinimaDias.Value > 3650))
            return "Duracao minima deve ficar entre 1 e 3650 dias.";

        if (!string.IsNullOrWhiteSpace(criterioTransicao) && criterioTransicao.Trim().Length > 1000)
            return "Criterio de transicao deve possuir no maximo 1000 caracteres.";

        var normalizado = NormalizarStatus(status);
        if (normalizado is not ("Planejada" or "EmAndamento" or "Concluida" or "Cancelada"))
            return "Status permitido: Planejada, EmAndamento, Concluida ou Cancelada.";

        return null;
    }

    private static string NormalizarTipo(string tipo)
    {
        var valor = tipo.Trim();
        return valor.Length > 50 ? valor[..50] : valor;
    }

    private static string NormalizarStatus(string status)
    {
        var s = (status ?? string.Empty).Trim().ToLowerInvariant();
        return s switch
        {
            "planejada" => "Planejada",
            "emandamento" or "em andamento" => "EmAndamento",
            "concluida" or "concluída" => "Concluida",
            "cancelada" => "Cancelada",
            _ => status?.Trim() ?? string.Empty
        };
    }

    private static string? Limitar(string? valor, int maximo)
    {
        var limpo = Limpar(valor);
        if (limpo is null) return null;
        return limpo.Length <= maximo ? limpo : limpo[..maximo];
    }

    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private object ToResponse(FaseTreino x) => new
    {
        x.Id,
        x.PacienteId,
        x.ProfissionalId,
        profissionalNome = x.Profissional?.Nome,
        x.PlanoTreinoId,
        planoNome = x.PlanoTreino?.Nome,
        planoVersao = x.PlanoTreino?.Versao,
        x.Nome,
        x.Tipo,
        x.Objetivo,
        x.DataInicio,
        x.DataFim,
        x.Ordem,
        x.Status,
        x.Observacoes,
        x.MetaPesoKg,
        x.MetaAdesaoPercentual,
        x.DuracaoMinimaDias,
        x.CriterioTransicao,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    };

    private static object Snapshot(FaseTreino x) => new
    {
        x.Id, x.PacienteId, x.ProfissionalId, x.PlanoTreinoId, x.Nome, x.Tipo,
        x.Objetivo, x.DataInicio, x.DataFim, x.Ordem, x.Status, x.Observacoes,
        x.MetaPesoKg, x.MetaAdesaoPercentual, x.DuracaoMinimaDias, x.CriterioTransicao
    };

    private void Auditar(string acao, FaseTreino fase, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(FaseTreino),
            EntidadeId = fase.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private void AuditarGenerico(string acao, string entidadeId, object dados)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(FaseTreino),
            EntidadeId = entidadeId,
            DadosNovosJson = JsonSerializer.Serialize(dados),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }
}
