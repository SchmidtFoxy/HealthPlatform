using System.Text.Json;
using HealthPlatform.Api.Contracts.PerformancePassport;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Medico,Nutricionista,Personal,Secretaria")]
[Route("api/pacientes/{pacienteId:guid}/performance/progress-review-notes")]
public class ProgressReviewNotesController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private const string PrefixoCategoria = "ProgressReview:";
    private const string PrefixoFollowUp = "ProgressReviewFollowUp:";
    private const string PrefixoCarePlan = "ProgressReviewCarePlan:";

    private static readonly IReadOnlyDictionary<string, string> Campos =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dado-observado"] = "Dado observado",
            ["interpretacao-profissional"] = "Interpretação profissional",
            ["ponto-atencao"] = "Ponto de atenção",
            ["hipotese-acompanhamento"] = "Hipótese de acompanhamento",
            ["proximo-item-revisar"] = "Próximo item a revisar"
        };

    private static readonly IReadOnlyDictionary<string, (string Rotulo, string Descricao)> Contextos =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["foundation"] = ("Foundation", "Sinal descritivo da Progress Intelligence Foundation."),
            ["context"] = ("Context", "Contexto temporal e densidade observacional."),
            ["timeline"] = ("Timeline", "Evento observado da Multi-Signal Timeline."),
            ["window"] = ("Evidence Window", "Janela temporal de evidência."),
            ["observation-map"] = ("Observation Map", "Coobservação documental por data."),
            ["summary"] = ("Summary", "Resumo observacional do progresso.")
        };

    private static readonly IReadOnlyDictionary<string, (string Destino, string Seletor)> NavegacaoContextos =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["foundation"] = ("Progress Intelligence Foundation", "[data-progress-intelligence-v0290]"),
            ["context"] = ("Progress Signal Context", "[data-progress-signal-context-v0291]"),
            ["timeline"] = ("Multi-Signal Timeline", "[data-multi-signal-timeline-v0292]"),
            ["window"] = ("Progress Evidence Windows", "[data-progress-evidence-windows-v0293]"),
            ["observation-map"] = ("Cross-Signal Observation Map", "[data-cross-signal-observation-map-v0294]"),
            ["summary"] = ("Progress Observation Summary", "[data-progress-observation-summary-v0295]")
        };

    public sealed record CriarProgressReviewNoteRequest(
        string Campo,
        string Conteudo,
        string? ContextoTipo = null,
        string? ContextoReferencia = null);

    public sealed record AtualizarProgressReviewNoteRequest(
        string Campo,
        string Conteudo,
        string? ContextoTipo = null,
        string? ContextoReferencia = null);

    [HttpGet("context-navigation/{tipo}")]
    public ActionResult<ProgressReviewContextNavigationResponse> ContextNavigation(string tipo)
    {
        var normalizado = tipo?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!Contextos.TryGetValue(normalizado, out var contexto) ||
            !NavegacaoContextos.TryGetValue(normalizado, out var navegacao))
            return NotFound(new { message = "Contexto de revisao nao encontrado." });

        return Ok(new ProgressReviewContextNavigationResponse(
            normalizado,
            contexto.Rotulo,
            navegacao.Destino,
            navegacao.Seletor,
            "O destino de navegação aponta somente para a seção observacional relacionada. A referência da nota permanece descritiva e não seleciona automaticamente um dado clínico específico."));
    }

    public sealed record CriarProgressReviewFollowUpRequest(
        string ItemAcompanhar,
        string? ContextoRelacionado = null,
        string? HorizonteRevisao = null,
        string? Responsavel = null,
        string? ObservacaoFollowUp = null);

    public sealed record AtualizarProgressReviewFollowUpRequest(
        string ItemAcompanhar,
        string? ContextoRelacionado = null,
        string? HorizonteRevisao = null,
        string? Responsavel = null,
        string? ObservacaoFollowUp = null);

    [HttpGet("follow-up-foundation")]
    public ActionResult<ProgressReviewFollowUpFoundationResponse> FollowUpFoundation()
    {
        var campos = new[]
        {
            new ProgressReviewFollowUpFieldResponse(
                "item-acompanhar",
                "Item a acompanhar",
                "Descreve objetivamente o item que deverá ser revisto em acompanhamento posterior.",
                true),
            new ProgressReviewFollowUpFieldResponse(
                "contexto-relacionado",
                "Contexto relacionado",
                "Permite referenciar o contexto observacional associado ao item de acompanhamento, sem copiar dados clínicos.",
                false),
            new ProgressReviewFollowUpFieldResponse(
                "horizonte-revisao",
                "Horizonte de revisão",
                "Registra uma referência temporal descritiva para nova revisão, sem gerar prazo clínico automaticamente.",
                false),
            new ProgressReviewFollowUpFieldResponse(
                "responsavel",
                "Responsável",
                "Identifica o profissional ou papel que deverá revisar o item quando aplicável.",
                false),
            new ProgressReviewFollowUpFieldResponse(
                "observacao-follow-up",
                "Observação de acompanhamento",
                "Espaço para orientação documental do próximo acompanhamento, sem transformar o registro em recomendação automática.",
                false)
        };

        return Ok(new ProgressReviewFollowUpFoundationResponse(
            campos,
            campos.Length,
            "FundacaoEstruturalDisponivel",
            true,
            "EquipeProfissional",
            "A fundação de follow-up organiza próximos itens a acompanhar. A partir da v0.31.1 possui persistência profissional auditada, sem criar decisão clínica, alerta automático, diagnóstico, prognóstico ou recomendação."));
    }

    public sealed record CriarProgressReviewCarePlanRequest(
        string ObjetivoCuidado,
        string AcaoPlanejada,
        string? Responsavel = null,
        string? Horizonte = null,
        Guid? FollowUpRelacionadoId = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProgressReviewCarePlanRequest(
        string ObjetivoCuidado,
        string AcaoPlanejada,
        string? Responsavel = null,
        string? Horizonte = null,
        Guid? FollowUpRelacionadoId = null,
        string? ObservacaoProfissional = null);

    [HttpGet("care-plan-foundation")]
    public ActionResult<ProgressReviewCarePlanFoundationResponse> CarePlanFoundation()
    {
        var campos = new[]
        {
            new ProgressReviewCarePlanFieldResponse(
                "objetivo-cuidado",
                "Objetivo do próximo cuidado",
                "Registra o objetivo documental que orientará o próximo passo profissional.",
                true),
            new ProgressReviewCarePlanFieldResponse(
                "acao-planejada",
                "Ação planejada",
                "Descreve a ação profissional prevista, sem executar ou prescrever automaticamente.",
                true),
            new ProgressReviewCarePlanFieldResponse(
                "responsavel",
                "Responsável",
                "Identifica o profissional ou papel responsável pelo próximo cuidado.",
                false),
            new ProgressReviewCarePlanFieldResponse(
                "horizonte",
                "Prazo ou horizonte",
                "Registra uma referência temporal documental, sem calcular prazo clínico.",
                false),
            new ProgressReviewCarePlanFieldResponse(
                "follow-up-relacionado",
                "Follow-up relacionado",
                "Permite referenciar opcionalmente um item de acompanhamento já existente.",
                false),
            new ProgressReviewCarePlanFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                "Permite registrar contexto adicional do plano de cuidado de forma documental.",
                false)
        };

        return Ok(new ProgressReviewCarePlanFoundationResponse(
            campos,
            campos.Length,
            "FundacaoCarePlanDisponivel",
            true,
            "EquipeProfissional",
            "A fundação do Care Plan organiza próximos cuidados de forma documental. A partir da v0.32.1 possui persistência profissional auditada, sem executar ações e sem criar prescrição, prioridade, diagnóstico, prognóstico ou recomendação automática."));
    }

    [HttpGet("care-plan")]
    public async Task<ActionResult<IReadOnlyCollection<ProgressReviewCarePlanPersistedResponse>>> ListarCarePlan(
        Guid pacienteId,
        [FromQuery] bool incluirArquivadas = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCarePlan));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearCarePlan).ToArray());
    }

    [HttpPost("care-plan")]
    public async Task<ActionResult<ProgressReviewCarePlanPersistedResponse>> CriarCarePlan(
        Guid pacienteId,
        CriarProgressReviewCarePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var objetivo = NormalizarObrigatorio(request.ObjetivoCuidado, 500);
        var acao = NormalizarObrigatorio(request.AcaoPlanejada, 1000);

        if (objetivo is null)
            return BadRequest(new { message = "Informe o objetivo do próximo cuidado." });

        if (acao is null)
            return BadRequest(new { message = "Informe a ação planejada." });

        if (request.FollowUpRelacionadoId.HasValue &&
            !await FollowUpPertencePacienteAsync(pacienteId, request.FollowUpRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Follow-up relacionado inválido para este paciente." });
        }

        var autor = await db.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(cancellationToken) ?? "Profissional";

        var nota = new NotaInternaProfissional
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            AutorUsuarioId = currentUser.UserId,
            AutorNome = autor,
            Categoria = PrefixoCarePlan + "item",
            Conteudo = MontarPayloadCarePlan(
                objetivo,
                acao,
                request.Responsavel,
                request.Horizonte,
                request.FollowUpRelacionadoId,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROGRESS_REVIEW_CARE_PLAN_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearCarePlan(nota));
    }

    [HttpPut("care-plan/{id:guid}")]
    public async Task<ActionResult<ProgressReviewCarePlanPersistedResponse>> AtualizarCarePlan(
        Guid pacienteId,
        Guid id,
        AtualizarProgressReviewCarePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCarePlan),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var objetivo = NormalizarObrigatorio(request.ObjetivoCuidado, 500);
        var acao = NormalizarObrigatorio(request.AcaoPlanejada, 1000);

        if (objetivo is null)
            return BadRequest(new { message = "Informe o objetivo do próximo cuidado." });

        if (acao is null)
            return BadRequest(new { message = "Informe a ação planejada." });

        if (request.FollowUpRelacionadoId.HasValue &&
            !await FollowUpPertencePacienteAsync(pacienteId, request.FollowUpRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Follow-up relacionado inválido para este paciente." });
        }

        var antes = Snapshot(nota);

        nota.Conteudo = MontarPayloadCarePlan(
            objetivo,
            acao,
            request.Responsavel,
            request.Horizonte,
            request.FollowUpRelacionadoId,
            request.ObservacaoProfissional);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROGRESS_REVIEW_CARE_PLAN_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearCarePlan(nota));
    }

    [HttpDelete("care-plan/{id:guid}")]
    public async Task<IActionResult> ArquivarCarePlan(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCarePlan),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROGRESS_REVIEW_CARE_PLAN_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("follow-up/closure")]
    public ActionResult<ProgressReviewFollowUpClosureResponse> FechamentoFollowUp()
    {
        var componentes = new[]
        {
            "FollowUpFoundation",
            "FollowUpPersistence",
            "FollowUpStatus",
            "FollowUpHistory",
            "FollowUpFilters",
            "FollowUpSummary"
        };

        return Ok(new ProgressReviewFollowUpClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaFollowUpCompleta",
            "O fechamento descreve apenas disponibilidade estrutural das capacidades de acompanhamento. Não representa score clínico, prioridade, risco, prognóstico, recomendação ou decisão terapêutica."));
    }

    [HttpGet("follow-up/summary")]
    public async Task<ActionResult<ProgressReviewFollowUpSummaryResponse>> ResumoFollowUp(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearFollowUp).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var abertos = itens.Count(x => !x.Arquivada && x.Status == "Aberto");
        var revisados = itens.Count(x => !x.Arquivada && x.Status == "Revisado");
        var encerrados = itens.Count(x => !x.Arquivada && x.Status == "Encerrado");

        var porResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.Responsavel))
            .GroupBy(x => x.Responsavel!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProgressReviewFollowUpResponsavelResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Responsavel)
            .ToArray();

        return Ok(new ProgressReviewFollowUpSummaryResponse(
            total,
            ativos,
            abertos,
            revisados,
            encerrados,
            arquivados,
            porResponsavel,
            "O resumo apresenta somente agregações documentais dos registros de acompanhamento. Não representa score clínico, risco, prioridade, prognóstico ou recomendação."));
    }

    [HttpGet("follow-up/search")]
    public async Task<ActionResult<ProgressReviewFollowUpFiltersResponse>> FiltrarFollowUp(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? responsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusFollowUpValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Aberto, Revisado ou Encerrado." });

        var responsavelNormalizado = NormalizarOpcional(responsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearFollowUp)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    (x.Responsavel?.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (horizonteNormalizado is null ||
                    (x.HorizonteRevisao?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ItemAcompanhar.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoRelacionado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoFollowUp?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProgressReviewFollowUpFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros de acompanhamento. Não classificam prioridade clínica, gravidade, resposta ao tratamento ou necessidade de intervenção."));
    }

    [HttpGet("follow-up")]
    public async Task<ActionResult<IReadOnlyCollection<ProgressReviewFollowUpPersistedResponse>>> ListarFollowUp(
        Guid pacienteId,
        [FromQuery] bool incluirArquivadas = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearFollowUp).ToArray());
    }

    [HttpPost("follow-up")]
    public async Task<ActionResult<ProgressReviewFollowUpPersistedResponse>> CriarFollowUp(
        Guid pacienteId,
        CriarProgressReviewFollowUpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var item = NormalizarObrigatorio(request.ItemAcompanhar, 500);
        if (item is null)
            return BadRequest(new { message = "Informe o item a acompanhar." });

        var payload = MontarPayloadFollowUp(
            item,
            request.ContextoRelacionado,
            request.HorizonteRevisao,
            request.Responsavel,
            request.ObservacaoFollowUp);

        var autor = await db.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(cancellationToken) ?? "Profissional";

        var nota = new NotaInternaProfissional
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            AutorUsuarioId = currentUser.UserId,
            AutorNome = autor,
            Categoria = PrefixoFollowUp + "item",
            Conteudo = payload,
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROGRESS_REVIEW_FOLLOW_UP_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearFollowUp(nota));
    }

    [HttpPut("follow-up/{id:guid}")]
    public async Task<ActionResult<ProgressReviewFollowUpPersistedResponse>> AtualizarFollowUp(
        Guid pacienteId,
        Guid id,
        AtualizarProgressReviewFollowUpRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var item = NormalizarObrigatorio(request.ItemAcompanhar, 500);
        if (item is null)
            return BadRequest(new { message = "Informe o item a acompanhar." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadFollowUp(nota);
        var payloadAtualizado = new FollowUpPayload(
            item,
            NormalizarOpcional(request.ContextoRelacionado, 240),
            NormalizarOpcional(request.HorizonteRevisao, 120),
            NormalizarOpcional(request.Responsavel, 160),
            NormalizarOpcional(request.ObservacaoFollowUp, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROGRESS_REVIEW_FOLLOW_UP_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearFollowUp(nota));
    }

    [HttpGet("follow-up/{id:guid}/history")]
    public async Task<ActionResult<ProgressReviewFollowUpHistoryResponse>> HistoricoFollowUp(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var followUpExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp),
                cancellationToken);

        if (!followUpExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROGRESS_REVIEW_FOLLOW_UP_"));

        query = ordemAsc
            ? query.OrderBy(x => x.CreatedAtUtc)
            : query.OrderByDescending(x => x.CreatedAtUtc);

        var logs = await query.ToListAsync(cancellationToken);

        var usuarioIds = logs
            .Where(x => x.UsuarioId.HasValue)
            .Select(x => x.UsuarioId!.Value)
            .Distinct()
            .ToArray();

        var usuarios = await db.Users
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                usuarioIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome })
            .ToDictionaryAsync(x => x.Id, x => x.Nome, cancellationToken);

        var itens = logs.Select(log =>
        {
            var autorNome = log.UsuarioId.HasValue &&
                            usuarios.TryGetValue(log.UsuarioId.Value, out var nome)
                ? nome
                : "Sistema";

            return new ProgressReviewFollowUpHistoryItemResponse(
                log.Id,
                id,
                MapearEventoFollowUp(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProgressReviewFollowUpHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais de acompanhamento. Não interpreta evolução clínica, causalidade, prioridade ou resultado."));
    }

    [HttpPatch("follow-up/{id:guid}/status")]
    public async Task<ActionResult<ProgressReviewFollowUpPersistedResponse>> AtualizarStatusFollowUp(
        Guid pacienteId,
        Guid id,
        ProgressReviewFollowUpStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusFollowUpValido(status))
            return BadRequest(new { message = "Status inválido. Use Aberto, Revisado ou Encerrado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadFollowUp(nota);

        if (payload.Status != status)
        {
            var atualizado = payload with
            {
                Status = status!,
                StatusAtualizadoEmUtc = DateTime.UtcNow
            };

            nota.Conteudo = JsonSerializer.Serialize(atualizado);
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar(
                "PROGRESS_REVIEW_FOLLOW_UP_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearFollowUp(nota));
    }

    [HttpDelete("follow-up/{id:guid}")]
    public async Task<IActionResult> ArquivarFollowUp(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROGRESS_REVIEW_FOLLOW_UP_ARCHIVED", nota, antes: null, depois: Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("context-closure")]
    public ActionResult<ProgressReviewContextClosureResponse> ContextClosure()
    {
        var componentes = new[]
        {
            "ContextLinks",
            "ContextNavigation",
            "ContextFocus",
            "ContextCapture",
            "CaptureConfirmation",
            "ContextIntegrity",
            "IntegrityUx",
            "IntegrityAccessibility"
        };

        return Ok(new ProgressReviewContextClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaContextualCompleta",
            "O fechamento descreve apenas presença estrutural das capacidades de revisão contextual. Não representa qualidade clínica, certeza, prognóstico, recomendação ou adequação profissional."));
    }

    [HttpGet("context-integrity")]
    public ActionResult<ProgressReviewContextIntegrityResponse> ContextIntegrity(
        [FromQuery] string? tipo = null,
        [FromQuery] string? referencia = null)
    {
        var resultado = ValidarIntegridadeContexto(tipo, referencia);
        return Ok(resultado);
    }

    [HttpGet("context-options")]
    public ActionResult<ProgressReviewContextLinksResponse> ContextOptions()
    {
        var opcoes = Contextos
            .Select(x => new ProgressReviewContextOptionResponse(
                x.Key,
                x.Value.Rotulo,
                x.Value.Descricao))
            .OrderBy(x => x.Tipo)
            .ToArray();

        return Ok(new ProgressReviewContextLinksResponse(
            opcoes,
            "O vínculo contextual apenas aponta para a camada observacional relacionada à nota. Não copia o conteúdo da camada, não cria causalidade e não transforma contexto em conclusão clínica."));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProgressReviewPersistedNoteResponse>>> Listar(
        Guid pacienteId,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                !x.Arquivada &&
                x.Categoria.StartsWith(PrefixoCategoria))
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(ct);

        return Ok(notas.Select(Mapear).ToArray());
    }


    [HttpGet("history")]
    public async Task<ActionResult<ProgressReviewHistoryResponse>> Historico(
        Guid pacienteId,
        [FromQuery] string? campo = null,
        [FromQuery] Guid? autorUsuarioId = null,
        [FromQuery] DateTime? deUtc = null,
        [FromQuery] DateTime? ateUtc = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        string? campoNormalizado = null;
        if (!string.IsNullOrWhiteSpace(campo))
        {
            if (!TryNormalizarCampo(campo, out var valido))
                return BadRequest(new { message = "Campo de revisao invalido." });

            campoNormalizado = valido;
        }

        if (deUtc.HasValue && ateUtc.HasValue && deUtc.Value > ateUtc.Value)
            return BadRequest(new { message = "Periodo invalido: deUtc deve ser anterior ou igual a ateUtc." });

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCategoria));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        if (campoNormalizado is not null)
        {
            var categoria = PrefixoCategoria + campoNormalizado;
            var categoriaContextual = categoria + "|";
            query = query.Where(x => x.Categoria == categoria || x.Categoria.StartsWith(categoriaContextual));
        }

        if (autorUsuarioId.HasValue)
            query = query.Where(x => x.AutorUsuarioId == autorUsuarioId.Value);

        if (deUtc.HasValue)
            query = query.Where(x => (x.UpdatedAtUtc ?? x.CreatedAtUtc) >= deUtc.Value);

        if (ateUtc.HasValue)
            query = query.Where(x => (x.UpdatedAtUtc ?? x.CreatedAtUtc) <= ateUtc.Value);

        var desc = !string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        query = desc
            ? query.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            : query.OrderBy(x => x.UpdatedAtUtc ?? x.CreatedAtUtc);

        var notas = await query.ToListAsync(ct);

        return Ok(new ProgressReviewHistoryResponse(
            notas.Select(Mapear).ToArray(),
            notas.Count,
            campoNormalizado,
            autorUsuarioId,
            deUtc,
            ateUtc,
            incluirArquivadas,
            desc ? "desc" : "asc"));
    }

    [HttpPost]
    public async Task<ActionResult<ProgressReviewPersistedNoteResponse>> Criar(
        Guid pacienteId,
        CriarProgressReviewNoteRequest request,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        if (!TryNormalizarCampo(request.Campo, out var campo))
            return BadRequest(new { message = "Campo de revisao invalido." });

        var conteudo = LimparConteudo(request.Conteudo);
        if (conteudo is null)
            return BadRequest(new { message = "Informe o conteudo da nota de revisao." });

        var autor = await db.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(ct) ?? "Profissional";

        if (!TryNormalizarContexto(request.ContextoTipo, request.ContextoReferencia, out var contextoTipo, out var contextoReferencia, out var contextoErro))
            return BadRequest(new { message = contextoErro });

        var nota = new NotaInternaProfissional
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            AutorUsuarioId = currentUser.UserId,
            AutorNome = autor,
            Categoria = MontarCategoria(campo, contextoTipo, contextoReferencia),
            Conteudo = conteudo,
            Fixada = false,
            Arquivada = false
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROGRESS_REVIEW_NOTE_CREATED", nota, null, Snapshot(nota));
        await db.SaveChangesAsync(ct);

        return Created(
            $"/api/pacientes/{pacienteId}/performance/progress-review-notes/{nota.Id}",
            Mapear(nota));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProgressReviewPersistedNoteResponse>> Atualizar(
        Guid pacienteId,
        Guid id,
        AtualizarProgressReviewNoteRequest request,
        CancellationToken ct = default)
    {
        var nota = await Obter(pacienteId, id, ct);
        if (nota is null)
            return NotFound(new { message = "Nota de revisao nao encontrada." });

        if (!TryNormalizarCampo(request.Campo, out var campo))
            return BadRequest(new { message = "Campo de revisao invalido." });

        var conteudo = LimparConteudo(request.Conteudo);
        if (conteudo is null)
            return BadRequest(new { message = "Informe o conteudo da nota de revisao." });

        if (!TryNormalizarContexto(request.ContextoTipo, request.ContextoReferencia, out var contextoTipo, out var contextoReferencia, out var contextoErro))
            return BadRequest(new { message = contextoErro });

        var antes = Snapshot(nota);
        nota.Categoria = MontarCategoria(campo, contextoTipo, contextoReferencia);
        nota.Conteudo = conteudo;
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROGRESS_REVIEW_NOTE_UPDATED", nota, antes, Snapshot(nota));
        await db.SaveChangesAsync(ct);

        return Ok(Mapear(nota));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Arquivar(
        Guid pacienteId,
        Guid id,
        CancellationToken ct = default)
    {
        var nota = await Obter(pacienteId, id, ct);
        if (nota is null)
            return NotFound(new { message = "Nota de revisao nao encontrada." });

        var antes = Snapshot(nota);
        nota.Arquivada = true;
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROGRESS_REVIEW_NOTE_ARCHIVED", nota, antes, Snapshot(nota));
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private Task<NotaInternaProfissional?> Obter(Guid pacienteId, Guid id, CancellationToken ct) =>
        db.NotasInternasProfissionais.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.PacienteId == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            !x.Arquivada &&
            x.Categoria.StartsWith(PrefixoCategoria), ct);

    private static bool TryNormalizarCampo(string? value, out string campo)
    {
        campo = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return Campos.ContainsKey(campo);
    }

    private static string? LimparConteudo(string? value)
    {
        var normalizado = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizado))
            return null;

        return normalizado.Length <= 4000
            ? normalizado
            : normalizado[..4000];
    }

    private sealed record FollowUpPayload(
        string ItemAcompanhar,
        string? ContextoRelacionado,
        string? HorizonteRevisao,
        string? Responsavel,
        string? ObservacaoFollowUp,
        string Status = "Aberto",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadFollowUp(
        string itemAcompanhar,
        string? contextoRelacionado,
        string? horizonteRevisao,
        string? responsavel,
        string? observacaoFollowUp)
    {
        var payload = new FollowUpPayload(
            itemAcompanhar,
            NormalizarOpcional(contextoRelacionado, 240),
            NormalizarOpcional(horizonteRevisao, 120),
            NormalizarOpcional(responsavel, 160),
            NormalizarOpcional(observacaoFollowUp, 2000),
            "Aberto",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private sealed record CarePlanPayload(
        string ObjetivoCuidado,
        string AcaoPlanejada,
        string? Responsavel,
        string? Horizonte,
        Guid? FollowUpRelacionadoId,
        string? ObservacaoProfissional);

    private static string MontarPayloadCarePlan(
        string objetivoCuidado,
        string acaoPlanejada,
        string? responsavel,
        string? horizonte,
        Guid? followUpRelacionadoId,
        string? observacaoProfissional)
    {
        var payload = new CarePlanPayload(
            objetivoCuidado,
            acaoPlanejada,
            NormalizarOpcional(responsavel, 160),
            NormalizarOpcional(horizonte, 120),
            followUpRelacionadoId,
            NormalizarOpcional(observacaoProfissional, 2000));

        return JsonSerializer.Serialize(payload);
    }

    private static ProgressReviewCarePlanPersistedResponse MapearCarePlan(NotaInternaProfissional nota)
    {
        CarePlanPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<CarePlanPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProgressReviewCarePlanPersistedResponse(
            nota.Id,
            payload?.ObjetivoCuidado ?? nota.Conteudo,
            payload?.AcaoPlanejada ?? string.Empty,
            payload?.Responsavel,
            payload?.Horizonte,
            payload?.FollowUpRelacionadoId,
            payload?.ObservacaoProfissional,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private async Task<bool> FollowUpPertencePacienteAsync(
        Guid pacienteId,
        Guid followUpId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == followUpId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoFollowUp) &&
                !x.Arquivada,
                cancellationToken);

    private static ProgressReviewFollowUpPersistedResponse MapearFollowUp(NotaInternaProfissional nota)
    {
        FollowUpPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<FollowUpPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado nunca deve quebrar a listagem.
        }

        return new ProgressReviewFollowUpPersistedResponse(
            nota.Id,
            payload?.ItemAcompanhar ?? nota.Conteudo,
            payload?.ContextoRelacionado,
            payload?.HorizonteRevisao,
            payload?.Responsavel,
            payload?.ObservacaoFollowUp,
            payload?.Status ?? "Aberto",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoFollowUp(string acao) =>
        acao switch
        {
            "PROGRESS_REVIEW_FOLLOW_UP_CREATED" => "Criado",
            "PROGRESS_REVIEW_FOLLOW_UP_UPDATED" => "Editado",
            "PROGRESS_REVIEW_FOLLOW_UP_STATUS_CHANGED" => "StatusAlterado",
            "PROGRESS_REVIEW_FOLLOW_UP_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static FollowUpPayload LerPayloadFollowUp(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<FollowUpPayload>(nota.Conteudo)
                ?? new FollowUpPayload(nota.Conteudo, null, null, null, null);
        }
        catch (JsonException)
        {
            return new FollowUpPayload(nota.Conteudo, null, null, null, null);
        }
    }

    private static bool StatusFollowUpValido(string? status) =>
        status is "Aberto" or "Revisado" or "Encerrado";

    private static string? NormalizarObrigatorio(string? valor, int limite)
    {
        var normalizado = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        if (normalizado is null)
            return null;

        return normalizado.Length <= limite
            ? normalizado
            : normalizado[..limite];
    }

    private static string? NormalizarOpcional(string? valor, int limite)
    {
        var normalizado = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        if (normalizado is null)
            return null;

        return normalizado.Length <= limite
            ? normalizado
            : normalizado[..limite];
    }

    private static ProgressReviewPersistedNoteResponse Mapear(NotaInternaProfissional nota)
    {
        var (campo, contextoTipo, contextoReferencia) = LerCategoria(nota.Categoria);

        var rotulo = Campos.TryGetValue(campo, out var nome)
            ? nome
            : campo;

        var navegacaoDestino = contextoTipo is not null &&
            NavegacaoContextos.TryGetValue(contextoTipo, out var navegacao)
                ? navegacao.Seletor
                : null;

        return new ProgressReviewPersistedNoteResponse(
            nota.Id,
            campo,
            rotulo,
            nota.Conteudo,
            contextoTipo,
            contextoReferencia,
            navegacaoDestino,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc);
    }

    private static string MontarCategoria(string campo, string? contextoTipo, string? contextoReferencia)
    {
        var categoria = PrefixoCategoria + campo;
        if (string.IsNullOrWhiteSpace(contextoTipo))
            return categoria;

        return $"{categoria}|{contextoTipo}|{contextoReferencia}";
    }

    private static (string Campo, string? ContextoTipo, string? ContextoReferencia) LerCategoria(string categoria)
    {
        var valor = categoria.StartsWith(PrefixoCategoria, StringComparison.OrdinalIgnoreCase)
            ? categoria[PrefixoCategoria.Length..]
            : categoria;

        var partes = valor.Split('|', 3);
        return (
            partes.ElementAtOrDefault(0) ?? valor,
            partes.ElementAtOrDefault(1),
            partes.ElementAtOrDefault(2));
    }

    private static ProgressReviewContextIntegrityResponse ValidarIntegridadeContexto(
        string? tipo,
        string? referencia)
    {
        var tipoNormalizado = string.IsNullOrWhiteSpace(tipo)
            ? null
            : tipo.Trim().ToLowerInvariant();

        var referenciaNormalizada = string.IsNullOrWhiteSpace(referencia)
            ? null
            : referencia.Trim();

        var erros = new List<string>();

        if (tipoNormalizado is null && referenciaNormalizada is null)
        {
            return new ProgressReviewContextIntegrityResponse(
                true,
                "SemVinculoContextual",
                null,
                null,
                Array.Empty<string>(),
                "Ausência de contexto é válida. Integridade estrutural não interpreta conteúdo clínico.");
        }

        if (tipoNormalizado is null)
            erros.Add("Informe o tipo do contexto antes da referência.");

        if (tipoNormalizado is not null && !Contextos.ContainsKey(tipoNormalizado))
            erros.Add("Tipo de contexto de revisão inválido.");

        if (referenciaNormalizada is null)
            erros.Add("Informe a referência do contexto selecionado.");

        if (referenciaNormalizada is not null && referenciaNormalizada.Length > 120)
            erros.Add("A referência contextual deve possuir no máximo 120 caracteres.");

        if (referenciaNormalizada is not null && referenciaNormalizada.Contains('|'))
            erros.Add("A referência contextual não pode conter o caractere reservado |.");

        return new ProgressReviewContextIntegrityResponse(
            erros.Count == 0,
            erros.Count == 0 ? "VinculoContextualValido" : "VinculoContextualInvalido",
            tipoNormalizado,
            referenciaNormalizada,
            erros,
            "A validação verifica somente coerência estrutural entre tipo e referência. Não valida significado clínico, causalidade ou pertinência profissional.");
    }

    private static bool TryNormalizarContexto(
        string? tipo,
        string? referencia,
        out string? tipoNormalizado,
        out string? referenciaNormalizada,
        out string? erro)
    {
        var integridade = ValidarIntegridadeContexto(tipo, referencia);

        tipoNormalizado = integridade.Tipo;
        referenciaNormalizada = integridade.Referencia;
        erro = integridade.Erros.FirstOrDefault();

        if (!integridade.Valido)
            return false;

        if (referenciaNormalizada is not null)
            referenciaNormalizada = referenciaNormalizada.Trim();

        return true;
    }

    private static object Snapshot(NotaInternaProfissional nota) => new
    {
        nota.Id,
        nota.PacienteId,
        nota.AutorUsuarioId,
        nota.AutorNome,
        nota.Categoria,
        nota.Conteudo,
        nota.Arquivada,
        nota.CreatedAtUtc,
        nota.UpdatedAtUtc
    };

    private void Auditar(string acao, NotaInternaProfissional nota, object? antes, object? depois) =>
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(NotaInternaProfissional),
            EntidadeId = nota.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
}
