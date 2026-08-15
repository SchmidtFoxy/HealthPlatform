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
public sealed class RevisoesFasesController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record RevisarFaseRequest(
        string Decisao,
        string Justificativa,
        bool ConfirmarMesmoSemCriterios = false);

    private sealed record AvaliacaoCriterios(
        int Configurados,
        int Atendidos,
        bool Prontos,
        decimal? PesoAtualKg,
        decimal? AdesaoMediaPercentual,
        int DiasDecorridos,
        string SnapshotJson);

    [HttpGet("api/pacientes/{pacienteId:guid}/revisoes-fases")]
    public async Task<IActionResult> Listar(
        Guid pacienteId,
        [FromQuery] string? dominio = null,
        [FromQuery] int limite = 50,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        limite = Math.Clamp(limite, 1, 100);
        var dominioNormalizado = string.IsNullOrWhiteSpace(dominio) ? null : NormalizarDominio(dominio);

        if (!string.IsNullOrWhiteSpace(dominio) && dominioNormalizado is null)
            return BadRequest(new { message = "Dominio permitido: Nutricao ou Treino." });

        var query = db.RevisoesFases.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId);

        if (dominioNormalizado is not null)
            query = query.Where(x => x.Dominio == dominioNormalizado);

        var itens = await query
            .OrderByDescending(x => x.DataUtc)
            .Take(limite)
            .ToListAsync(ct);

        var usuariosIds = itens.Select(x => x.RevisadoPorUsuarioId).Distinct().ToList();
        var usuarios = await db.Users.AsNoTracking()
            .Where(x => usuariosIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome })
            .ToDictionaryAsync(x => x.Id, x => x.Nome, ct);

        return Ok(new
        {
            total = itens.Count,
            itens = itens.Select(x => new
            {
                x.Id,
                x.PacienteId,
                x.Dominio,
                x.FaseId,
                x.FaseNome,
                x.FaseDestinoId,
                x.FaseDestinoNome,
                x.Decisao,
                x.Justificativa,
                x.DataUtc,
                x.StatusAntes,
                x.StatusDepois,
                x.CriteriosConfigurados,
                x.CriteriosAtendidos,
                x.ObjetivosProntosParaRevisao,
                x.OverrideCriterios,
                x.CriterioProfissional,
                revisadoPorNome = usuarios.TryGetValue(x.RevisadoPorUsuarioId, out var nome) ? nome : null
            }).ToList()
        });
    }

    [HttpPost("api/fases-nutricionais/{id:guid}/revisar")]
    public async Task<IActionResult> RevisarNutricional(
        Guid id,
        RevisarFaseRequest request,
        CancellationToken ct = default)
    {
        var fase = await db.FasesNutricionais.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (fase is null)
            return NotFound(new { message = "Fase nutricional nao encontrada." });

        if (fase.Status != "EmAndamento")
            return Conflict(new { message = "Somente uma fase EmAndamento pode receber revisao operacional." });

        var decisao = NormalizarDecisao(request.Decisao);
        var erro = ValidarRevisao(decisao, request.Justificativa);
        if (erro is not null)
            return BadRequest(new { message = erro });

        var avaliacao = await AvaliarCriterios(
            fase.PacienteId,
            fase.Id,
            true,
            fase.DataInicio,
            fase.MetaPesoKg,
            fase.MetaAdesaoPercentual,
            fase.DuracaoMinimaDias,
            fase.CriterioTransicao,
            ct);

        if (ExigeOverride(decisao, avaliacao) && !request.ConfirmarMesmoSemCriterios)
        {
            return Conflict(new
            {
                message = "Existem criterios objetivos pendentes. Confirme explicitamente a decisao profissional para prosseguir.",
                avaliacao.Configurados,
                avaliacao.Atendidos,
                avaliacao.Prontos
            });
        }

        FaseNutricional? proxima = null;
        if (decisao == "Avancar")
        {
            proxima = await db.FasesNutricionais
                .Where(x =>
                    x.PacienteId == fase.PacienteId &&
                    x.OrganizacaoId == currentUser.OrganizationId &&
                    x.Ordem > fase.Ordem &&
                    x.Status == "Planejada")
                .OrderBy(x => x.Ordem)
                .FirstOrDefaultAsync(ct);

            if (proxima is null)
                return Conflict(new { message = "Nao existe proxima fase nutricional Planejada para avancar." });

            var outraAtiva = await db.FasesNutricionais.AnyAsync(x =>
                x.PacienteId == fase.PacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Status == "EmAndamento" &&
                x.Id != fase.Id, ct);

            if (outraAtiva)
                return Conflict(new { message = "Ja existe outra fase nutricional EmAndamento." });
        }

        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var statusAntes = fase.Status;
        if (decisao is "Concluir" or "Avancar")
        {
            fase.Status = "Concluida";
            fase.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (proxima is not null)
        {
            proxima.Status = "EmAndamento";
            proxima.UpdatedAtUtc = DateTime.UtcNow;
        }

        var revisao = CriarRevisao(
            fase.PacienteId,
            "Nutricao",
            fase.Id,
            fase.Nome,
            proxima?.Id,
            proxima?.Nome,
            decisao!,
            request.Justificativa,
            statusAntes,
            fase.Status,
            fase.CriterioTransicao,
            avaliacao,
            ExigeOverride(decisao, avaliacao) && request.ConfirmarMesmoSemCriterios);

        db.RevisoesFases.Add(revisao);
        AuditarRevisao(revisao);

        if (statusAntes != fase.Status)
            AuditarFase("FaseNutricional", fase.Id, "REVIEW_STATUS", statusAntes, fase.Status);

        if (proxima is not null)
            AuditarFase("FaseNutricional", proxima.Id, "REVIEW_ACTIVATE_NEXT", "Planejada", "EmAndamento");

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        return Ok(new
        {
            message = MensagemDecisao(decisao!, proxima?.Nome),
            revisaoId = revisao.Id,
            decisao,
            statusFase = fase.Status,
            proximaFaseId = proxima?.Id,
            proximaFaseNome = proxima?.Nome,
            criteriosConfigurados = avaliacao.Configurados,
            criteriosAtendidos = avaliacao.Atendidos,
            objetivosProntosParaRevisao = avaliacao.Prontos,
            overrideCriterios = revisao.OverrideCriterios
        });
    }

    [HttpPost("api/fases-treino/{id:guid}/revisar")]
    public async Task<IActionResult> RevisarTreino(
        Guid id,
        RevisarFaseRequest request,
        CancellationToken ct = default)
    {
        var fase = await db.FasesTreino.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (fase is null)
            return NotFound(new { message = "Fase de treino nao encontrada." });

        if (fase.Status != "EmAndamento")
            return Conflict(new { message = "Somente uma fase EmAndamento pode receber revisao operacional." });

        var decisao = NormalizarDecisao(request.Decisao);
        var erro = ValidarRevisao(decisao, request.Justificativa);
        if (erro is not null)
            return BadRequest(new { message = erro });

        var avaliacao = await AvaliarCriterios(
            fase.PacienteId,
            fase.Id,
            false,
            fase.DataInicio,
            fase.MetaPesoKg,
            fase.MetaAdesaoPercentual,
            fase.DuracaoMinimaDias,
            fase.CriterioTransicao,
            ct);

        if (ExigeOverride(decisao, avaliacao) && !request.ConfirmarMesmoSemCriterios)
        {
            return Conflict(new
            {
                message = "Existem criterios objetivos pendentes. Confirme explicitamente a decisao profissional para prosseguir.",
                avaliacao.Configurados,
                avaliacao.Atendidos,
                avaliacao.Prontos
            });
        }

        FaseTreino? proxima = null;
        if (decisao == "Avancar")
        {
            proxima = await db.FasesTreino
                .Where(x =>
                    x.PacienteId == fase.PacienteId &&
                    x.OrganizacaoId == currentUser.OrganizationId &&
                    x.Ordem > fase.Ordem &&
                    x.Status == "Planejada")
                .OrderBy(x => x.Ordem)
                .FirstOrDefaultAsync(ct);

            if (proxima is null)
                return Conflict(new { message = "Nao existe proxima fase de treino Planejada para avancar." });

            var outraAtiva = await db.FasesTreino.AnyAsync(x =>
                x.PacienteId == fase.PacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Status == "EmAndamento" &&
                x.Id != fase.Id, ct);

            if (outraAtiva)
                return Conflict(new { message = "Ja existe outra fase de treino EmAndamento." });
        }

        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var statusAntes = fase.Status;
        if (decisao is "Concluir" or "Avancar")
        {
            fase.Status = "Concluida";
            fase.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (proxima is not null)
        {
            proxima.Status = "EmAndamento";
            proxima.UpdatedAtUtc = DateTime.UtcNow;
        }

        var revisao = CriarRevisao(
            fase.PacienteId,
            "Treino",
            fase.Id,
            fase.Nome,
            proxima?.Id,
            proxima?.Nome,
            decisao!,
            request.Justificativa,
            statusAntes,
            fase.Status,
            fase.CriterioTransicao,
            avaliacao,
            ExigeOverride(decisao, avaliacao) && request.ConfirmarMesmoSemCriterios);

        db.RevisoesFases.Add(revisao);
        AuditarRevisao(revisao);

        if (statusAntes != fase.Status)
            AuditarFase("FaseTreino", fase.Id, "REVIEW_STATUS", statusAntes, fase.Status);

        if (proxima is not null)
            AuditarFase("FaseTreino", proxima.Id, "REVIEW_ACTIVATE_NEXT", "Planejada", "EmAndamento");

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        return Ok(new
        {
            message = MensagemDecisao(decisao!, proxima?.Nome),
            revisaoId = revisao.Id,
            decisao,
            statusFase = fase.Status,
            proximaFaseId = proxima?.Id,
            proximaFaseNome = proxima?.Nome,
            criteriosConfigurados = avaliacao.Configurados,
            criteriosAtendidos = avaliacao.Atendidos,
            objetivosProntosParaRevisao = avaliacao.Prontos,
            overrideCriterios = revisao.OverrideCriterios
        });
    }

    private async Task<AvaliacaoCriterios> AvaliarCriterios(
        Guid pacienteId,
        Guid faseId,
        bool nutricao,
        DateOnly dataInicio,
        decimal? metaPesoKg,
        int? metaAdesaoPercentual,
        int? duracaoMinimaDias,
        string? criterioProfissional,
        CancellationToken ct)
    {
        var query = db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId);

        query = nutricao
            ? query.Where(x => x.FaseNutricionalId == faseId)
            : query.Where(x => x.FaseTreinoId == faseId);

        var itens = await query.OrderBy(x => x.DataUtc).ToListAsync(ct);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dias = Math.Max(0, hoje.DayNumber - dataInicio.DayNumber + 1);
        var peso = itens.LastOrDefault(x => x.PesoKg.HasValue)?.PesoKg;
        var adesao = nutricao
            ? Media(itens.Select(x => x.AdesaoAlimentacaoPercentual))
            : Media(itens.Select(x => x.AdesaoTreinoPercentual));

        var configurados = 0;
        var atendidos = 0;

        if (duracaoMinimaDias.HasValue)
        {
            configurados++;
            if (dias >= duracaoMinimaDias.Value) atendidos++;
        }

        if (metaAdesaoPercentual.HasValue)
        {
            configurados++;
            if (adesao.HasValue && adesao.Value >= metaAdesaoPercentual.Value) atendidos++;
        }

        if (metaPesoKg.HasValue)
        {
            configurados++;
            if (peso.HasValue && Math.Abs(peso.Value - metaPesoKg.Value) <= 0.5m) atendidos++;
        }

        var prontos = configurados > 0 && configurados == atendidos;
        var snapshot = JsonSerializer.Serialize(new
        {
            diasDecorridos = dias,
            pesoAtualKg = peso,
            adesaoMediaPercentual = adesao,
            metaPesoKg,
            metaAdesaoPercentual,
            duracaoMinimaDias,
            criterioProfissional,
            criteriosConfigurados = configurados,
            criteriosAtendidos = atendidos,
            objetivosProntosParaRevisao = prontos
        });

        return new AvaliacaoCriterios(configurados, atendidos, prontos, peso, adesao, dias, snapshot);
    }

    private RevisaoFase CriarRevisao(
        Guid pacienteId,
        string dominio,
        Guid faseId,
        string faseNome,
        Guid? faseDestinoId,
        string? faseDestinoNome,
        string decisao,
        string justificativa,
        string statusAntes,
        string statusDepois,
        string? criterioProfissional,
        AvaliacaoCriterios avaliacao,
        bool overrideCriterios) =>
        new()
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            RevisadoPorUsuarioId = currentUser.UserId,
            Dominio = dominio,
            FaseId = faseId,
            FaseNome = faseNome,
            FaseDestinoId = faseDestinoId,
            FaseDestinoNome = faseDestinoNome,
            Decisao = decisao,
            Justificativa = justificativa.Trim(),
            DataUtc = DateTime.UtcNow,
            StatusAntes = statusAntes,
            StatusDepois = statusDepois,
            CriteriosConfigurados = avaliacao.Configurados,
            CriteriosAtendidos = avaliacao.Atendidos,
            ObjetivosProntosParaRevisao = avaliacao.Prontos,
            OverrideCriterios = overrideCriterios,
            CriterioProfissional = string.IsNullOrWhiteSpace(criterioProfissional) ? null : criterioProfissional.Trim(),
            SnapshotIndicadoresJson = avaliacao.SnapshotJson
        };

    private static bool ExigeOverride(string? decisao, AvaliacaoCriterios avaliacao) =>
        decisao is "Concluir" or "Avancar" &&
        avaliacao.Configurados > 0 &&
        !avaliacao.Prontos;

    private static string? ValidarRevisao(string? decisao, string justificativa)
    {
        if (decisao is null)
            return "Decisao permitida: Manter, Concluir ou Avancar.";

        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length < 5)
            return "Informe uma justificativa com pelo menos 5 caracteres.";

        if (justificativa.Trim().Length > 2000)
            return "A justificativa deve possuir no maximo 2000 caracteres.";

        return null;
    }

    private static string? NormalizarDecisao(string? decisao)
    {
        var valor = (decisao ?? string.Empty).Trim().ToLowerInvariant();
        return valor switch
        {
            "manter" => "Manter",
            "concluir" => "Concluir",
            "avancar" or "avançar" => "Avancar",
            _ => null
        };
    }

    private static string? NormalizarDominio(string? dominio)
    {
        var valor = (dominio ?? string.Empty).Trim().ToLowerInvariant();
        return valor switch
        {
            "nutricao" or "nutrição" => "Nutricao",
            "treino" => "Treino",
            _ => null
        };
    }

    private static decimal? Media(IEnumerable<int?> valores)
    {
        var itens = valores.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return itens.Count == 0 ? null : Math.Round((decimal)itens.Average(), 1);
    }

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private static string MensagemDecisao(string decisao, string? proximaNome) =>
        decisao switch
        {
            "Manter" => "Revisao registrada. A fase permanece em andamento.",
            "Concluir" => "Revisao registrada e fase concluida.",
            "Avancar" => $"Revisao registrada. Fase concluida e proxima fase ativada: {proximaNome}.",
            _ => "Revisao registrada."
        };

    private void AuditarRevisao(RevisaoFase revisao)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "REVIEW_CREATE",
            Entidade = nameof(RevisaoFase),
            EntidadeId = revisao.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new
            {
                revisao.Dominio,
                revisao.FaseId,
                revisao.FaseDestinoId,
                revisao.Decisao,
                revisao.CriteriosConfigurados,
                revisao.CriteriosAtendidos,
                revisao.OverrideCriterios
            }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private void AuditarFase(string entidade, Guid id, string acao, string antes, string depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = id.ToString(),
            DadosAnterioresJson = JsonSerializer.Serialize(new { Status = antes }),
            DadosNovosJson = JsonSerializer.Serialize(new { Status = depois }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }
}
