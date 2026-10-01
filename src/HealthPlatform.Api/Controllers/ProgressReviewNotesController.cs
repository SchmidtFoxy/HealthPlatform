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
    private const string PrefixoActionPlan = "ProfessionalReviewActionPlan:";
    private const string PrefixoTaskCoordination = "ProfessionalReviewTaskCoordination:";
    private const string PrefixoAssignment = "ProfessionalReviewAssignment:";
    private const string PrefixoDelegation = "ProfessionalReviewDelegation:";

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

    public sealed record CriarProfessionalReviewActionPlanRequest(
        string AcaoOperacional,
        string? ObjetivoRelacionado = null,
        string? Responsavel = null,
        string? Horizonte = null,
        Guid? CarePlanRelacionadoId = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewActionPlanRequest(
        string AcaoOperacional,
        string? ObjetivoRelacionado = null,
        string? Responsavel = null,
        string? Horizonte = null,
        Guid? CarePlanRelacionadoId = null,
        string? ObservacaoProfissional = null);

    [HttpGet("action-plan-foundation")]
    public ActionResult<ProfessionalReviewActionPlanFoundationResponse> ActionPlanFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewActionPlanFieldResponse(
                "acao-operacional",
                "Ação operacional",
                "Descreve a próxima ação profissional acompanhável.",
                true),
            new ProfessionalReviewActionPlanFieldResponse(
                "objetivo-relacionado",
                "Objetivo relacionado",
                "Relaciona a ação a um objetivo documental do plano profissional.",
                false),
            new ProfessionalReviewActionPlanFieldResponse(
                "responsavel",
                "Responsável",
                "Identifica o profissional ou papel responsável pela ação.",
                false),
            new ProfessionalReviewActionPlanFieldResponse(
                "horizonte",
                "Prazo ou horizonte",
                "Registra uma referência temporal documental para acompanhamento.",
                false),
            new ProfessionalReviewActionPlanFieldResponse(
                "care-plan-relacionado",
                "Care Plan relacionado",
                "Permite referenciar opcionalmente um Care Plan já existente.",
                false),
            new ProfessionalReviewActionPlanFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                "Permite registrar contexto adicional da ação.",
                false)
        };

        return Ok(new ProfessionalReviewActionPlanFoundationResponse(
            campos,
            campos.Length,
            "FundacaoActionPlanDisponivel",
            true,
            "EquipeProfissional",
            "A fundação do Action Plan organiza ações profissionais de forma documental. A partir da v0.33.1 possui persistência profissional auditada, sem executar ações e sem criar prescrição, prioridade, risco, diagnóstico, prognóstico ou recomendação automática."));
    }

    public sealed record CriarProfessionalReviewTaskCoordinationRequest(
        string TarefaOperacional,
        Guid? ActionPlanRelacionadoId = null,
        string? Responsavel = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTaskCoordinationRequest(
        string TarefaOperacional,
        Guid? ActionPlanRelacionadoId = null,
        string? Responsavel = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("task-coordination/foundation")]
    public ActionResult<ProfessionalReviewTaskCoordinationFoundationResponse> TaskCoordinationFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTaskCoordinationFieldResponse(
                "tarefa-operacional",
                "Tarefa operacional",
                true,
                "texto",
                "Descreve a atividade acompanhável pela equipe profissional."),
            new ProfessionalReviewTaskCoordinationFieldResponse(
                "action-plan-relacionado",
                "Action Plan relacionado",
                false,
                "referencia",
                "Permite vincular a tarefa a uma ação operacional já documentada."),
            new ProfessionalReviewTaskCoordinationFieldResponse(
                "responsavel",
                "Responsável",
                false,
                "texto",
                "Identifica quem ficará responsável pelo acompanhamento operacional."),
            new ProfessionalReviewTaskCoordinationFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra janela ou referência temporal operacional, sem definir urgência clínica."),
            new ProfessionalReviewTaskCoordinationFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar contexto operacional relevante para a equipe.")
        };

        return Ok(new ProfessionalReviewTaskCoordinationFoundationResponse(
            "FundacaoTaskCoordinationDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza tarefas acompanháveis pela equipe profissional e, a partir da v0.34.1, possui persistência auditada. Não executa condutas, não atribui prioridade clínica e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewAssignmentRequest(
        string ResponsavelPrincipal,
        Guid? TaskCoordinationRelacionadaId = null,
        string? ApoioParticipante = null,
        string? ContextoAtribuicao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewAssignmentRequest(
        string ResponsavelPrincipal,
        Guid? TaskCoordinationRelacionadaId = null,
        string? ApoioParticipante = null,
        string? ContextoAtribuicao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("assignment/foundation")]
    public ActionResult<ProfessionalReviewAssignmentFoundationResponse> AssignmentFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewAssignmentFieldResponse(
                "task-coordination-relacionada",
                "Tarefa operacional relacionada",
                false,
                "referencia",
                "Permite relacionar a atribuição a uma tarefa operacional já documentada."),
            new ProfessionalReviewAssignmentFieldResponse(
                "responsavel-principal",
                "Responsável principal",
                true,
                "texto",
                "Identifica quem assume a responsabilidade documental principal pela atribuição."),
            new ProfessionalReviewAssignmentFieldResponse(
                "apoio-participante",
                "Apoio ou participante",
                false,
                "texto",
                "Registra apoio adicional ou participante envolvido na atribuição."),
            new ProfessionalReviewAssignmentFieldResponse(
                "contexto-atribuicao",
                "Contexto da atribuição",
                false,
                "texto-longo",
                "Documenta o contexto operacional da atribuição sem caracterizar decisão clínica."),
            new ProfessionalReviewAssignmentFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewAssignmentFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes para a equipe.")
        };

        return Ok(new ProfessionalReviewAssignmentFoundationResponse(
            "FundacaoAssignmentDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza atribuições documentais da equipe profissional e, a partir da v0.35.1, possui persistência auditada. Não executa condutas, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewDelegationRequest(
        string ProfissionalDelegante,
        string ProfissionalDelegado,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoDelegacao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewDelegationRequest(
        string ProfissionalDelegante,
        string ProfissionalDelegado,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoDelegacao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("delegation/foundation")]
    public ActionResult<ProfessionalReviewDelegationFoundationResponse> DelegationFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewDelegationFieldResponse(
                "assignment-relacionada",
                "Assignment relacionada",
                false,
                "referencia",
                "Permite relacionar a delegação a uma atribuição profissional já documentada."),
            new ProfessionalReviewDelegationFieldResponse(
                "profissional-delegante",
                "Profissional delegante",
                true,
                "texto",
                "Identifica documentalmente quem realiza a delegação."),
            new ProfessionalReviewDelegationFieldResponse(
                "profissional-delegado",
                "Profissional delegado",
                true,
                "texto",
                "Identifica documentalmente quem recebe a delegação."),
            new ProfessionalReviewDelegationFieldResponse(
                "contexto-delegacao",
                "Contexto da delegação",
                false,
                "texto-longo",
                "Documenta o contexto operacional da delegação sem caracterizar decisão clínica automática."),
            new ProfessionalReviewDelegationFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewDelegationFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes para a equipe.")
        };

        return Ok(new ProfessionalReviewDelegationFoundationResponse(
            "FundacaoDelegationDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza delegações documentais da equipe profissional e, a partir da v0.36.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    [HttpGet("delegation/summary")]
    public async Task<ActionResult<ProfessionalReviewDelegationSummaryResponse>> ResumoDelegations(
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
                x.Categoria.StartsWith(PrefixoDelegation))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearDelegation).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var planejadas = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidas = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var canceladas = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porDelegante = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalDelegante))
            .GroupBy(x => x.ProfissionalDelegante.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewDelegationProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        var porDelegado = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalDelegado))
            .GroupBy(x => x.ProfissionalDelegado.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewDelegationProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewDelegationSummaryResponse(
            total,
            ativas,
            planejadas,
            emAndamento,
            concluidas,
            canceladas,
            arquivadas,
            porDelegante,
            porDelegado,
            "O resumo apresenta somente agregações documentais das delegações profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("delegation/search")]
    public async Task<ActionResult<ProfessionalReviewDelegationFiltersResponse>> FiltrarDelegations(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalDelegante = null,
        [FromQuery] string? profissionalDelegado = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusDelegationValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var deleganteNormalizado = NormalizarOpcional(profissionalDelegante, 160);
        var delegadoNormalizado = NormalizarOpcional(profissionalDelegado, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoDelegation) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearDelegation)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (deleganteNormalizado is null ||
                    x.ProfissionalDelegante.Contains(deleganteNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (delegadoNormalizado is null ||
                    x.ProfissionalDelegado.Contains(delegadoNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalDelegante.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    x.ProfissionalDelegado.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoDelegacao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewDelegationFiltersResponse(
            statusNormalizado,
            deleganteNormalizado,
            delegadoNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar delegações profissionais documentadas. Não classificam urgência, risco, prioridade clínica, resposta ao tratamento ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("delegation")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewDelegationPersistedResponse>>> ListarDelegations(
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
                x.Categoria.StartsWith(PrefixoDelegation));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearDelegation).ToArray());
    }

    [HttpPost("delegation")]
    public async Task<ActionResult<ProfessionalReviewDelegationPersistedResponse>> CriarDelegation(
        Guid pacienteId,
        CriarProfessionalReviewDelegationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var delegante = NormalizarObrigatorio(request.ProfissionalDelegante, 160);
        var delegado = NormalizarObrigatorio(request.ProfissionalDelegado, 160);

        if (delegante is null)
            return BadRequest(new { message = "Informe o profissional delegante." });

        if (delegado is null)
            return BadRequest(new { message = "Informe o profissional delegado." });

        if (request.AssignmentRelacionadaId.HasValue &&
            !await AssignmentPertencePacienteAsync(pacienteId, request.AssignmentRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Assignment relacionada inválida para este paciente." });
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
            Categoria = PrefixoDelegation + "item",
            Conteudo = MontarPayloadDelegation(
                delegante,
                delegado,
                request.AssignmentRelacionadaId,
                request.ContextoDelegacao,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_DELEGATION_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearDelegation(nota));
    }

    [HttpPut("delegation/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewDelegationPersistedResponse>> AtualizarDelegation(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewDelegationRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoDelegation),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var delegante = NormalizarObrigatorio(request.ProfissionalDelegante, 160);
        var delegado = NormalizarObrigatorio(request.ProfissionalDelegado, 160);

        if (delegante is null)
            return BadRequest(new { message = "Informe o profissional delegante." });

        if (delegado is null)
            return BadRequest(new { message = "Informe o profissional delegado." });

        if (request.AssignmentRelacionadaId.HasValue &&
            !await AssignmentPertencePacienteAsync(pacienteId, request.AssignmentRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Assignment relacionada inválida para este paciente." });
        }

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadDelegation(nota);
        var payloadAtualizado = new DelegationPayload(
            delegante,
            delegado,
            request.AssignmentRelacionadaId,
            NormalizarOpcional(request.ContextoDelegacao, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_DELEGATION_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearDelegation(nota));
    }

    [HttpGet("delegation/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewDelegationHistoryResponse>> HistoricoDelegation(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var delegationExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoDelegation),
                cancellationToken);

        if (!delegationExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_DELEGATION_"));

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

            return new ProfessionalReviewDelegationHistoryItemResponse(
                log.Id,
                id,
                MapearEventoDelegation(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewDelegationHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais da Delegation. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("delegation/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewDelegationPersistedResponse>> AtualizarStatusDelegation(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewDelegationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusDelegationValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoDelegation) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadDelegation(nota);

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
                "PROFESSIONAL_REVIEW_DELEGATION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearDelegation(nota));
    }

    [HttpDelete("delegation/{id:guid}")]
    public async Task<IActionResult> ArquivarDelegation(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoDelegation),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_DELEGATION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("assignment/closure")]
    public ActionResult<ProfessionalReviewAssignmentClosureResponse> FechamentoAssignments()
    {
        var componentes = new[]
        {
            "AssignmentFoundation",
            "AssignmentPersistence",
            "AssignmentStatus",
            "AssignmentHistory",
            "AssignmentFilters",
            "AssignmentSummary"
        };

        return Ok(new ProfessionalReviewAssignmentClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaAssignmentCompleta",
            "O fechamento descreve apenas disponibilidade estrutural das atribuições profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica."));
    }

    [HttpGet("assignment/summary")]
    public async Task<ActionResult<ProfessionalReviewAssignmentSummaryResponse>> ResumoAssignments(
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
                x.Categoria.StartsWith(PrefixoAssignment))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearAssignment).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var planejadas = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidas = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var canceladas = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porResponsavelPrincipal = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ResponsavelPrincipal))
            .GroupBy(x => x.ResponsavelPrincipal.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewAssignmentResponsavelResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.ResponsavelPrincipal)
            .ToArray();

        return Ok(new ProfessionalReviewAssignmentSummaryResponse(
            total,
            ativas,
            planejadas,
            emAndamento,
            concluidas,
            canceladas,
            arquivadas,
            porResponsavelPrincipal,
            "O resumo apresenta somente agregações documentais das atribuições profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação."));
    }

    [HttpGet("assignment/search")]
    public async Task<ActionResult<ProfessionalReviewAssignmentFiltersResponse>> FiltrarAssignments(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? responsavelPrincipal = null,
        [FromQuery] string? apoioParticipante = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusAssignmentValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var responsavelNormalizado = NormalizarOpcional(responsavelPrincipal, 160);
        var apoioNormalizado = NormalizarOpcional(apoioParticipante, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoAssignment) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearAssignment)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ResponsavelPrincipal.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (apoioNormalizado is null ||
                    (x.ApoioParticipante?.Contains(apoioNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ResponsavelPrincipal.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ApoioParticipante?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ContextoAtribuicao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewAssignmentFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            apoioNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar atribuições profissionais. Não classificam urgência, risco, prioridade clínica, resposta ao tratamento ou necessidade de intervenção."));
    }

    [HttpGet("assignment")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewAssignmentPersistedResponse>>> ListarAssignments(
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
                x.Categoria.StartsWith(PrefixoAssignment));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearAssignment).ToArray());
    }

    [HttpPost("assignment")]
    public async Task<ActionResult<ProfessionalReviewAssignmentPersistedResponse>> CriarAssignment(
        Guid pacienteId,
        CriarProfessionalReviewAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var responsavelPrincipal = NormalizarObrigatorio(request.ResponsavelPrincipal, 160);
        if (responsavelPrincipal is null)
            return BadRequest(new { message = "Informe o responsável principal." });

        if (request.TaskCoordinationRelacionadaId.HasValue &&
            !await TaskCoordinationPertencePacienteAsync(pacienteId, request.TaskCoordinationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Task Coordination relacionada inválida para este paciente." });
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
            Categoria = PrefixoAssignment + "item",
            Conteudo = MontarPayloadAssignment(
                responsavelPrincipal,
                request.TaskCoordinationRelacionadaId,
                request.ApoioParticipante,
                request.ContextoAtribuicao,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_ASSIGNMENT_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearAssignment(nota));
    }

    [HttpPut("assignment/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewAssignmentPersistedResponse>> AtualizarAssignment(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoAssignment),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var responsavelPrincipal = NormalizarObrigatorio(request.ResponsavelPrincipal, 160);
        if (responsavelPrincipal is null)
            return BadRequest(new { message = "Informe o responsável principal." });

        if (request.TaskCoordinationRelacionadaId.HasValue &&
            !await TaskCoordinationPertencePacienteAsync(pacienteId, request.TaskCoordinationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Task Coordination relacionada inválida para este paciente." });
        }

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadAssignment(nota);
        var payloadAtualizado = new AssignmentPayload(
            responsavelPrincipal,
            request.TaskCoordinationRelacionadaId,
            NormalizarOpcional(request.ApoioParticipante, 160),
            NormalizarOpcional(request.ContextoAtribuicao, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_ASSIGNMENT_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearAssignment(nota));
    }

    [HttpGet("assignment/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewAssignmentHistoryResponse>> HistoricoAssignment(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var assignmentExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoAssignment),
                cancellationToken);

        if (!assignmentExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_ASSIGNMENT_"));

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

            return new ProfessionalReviewAssignmentHistoryItemResponse(
                log.Id,
                id,
                MapearEventoAssignment(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewAssignmentHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais da Assignment. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado."));
    }

    [HttpPatch("assignment/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewAssignmentPersistedResponse>> AtualizarStatusAssignment(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewAssignmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusAssignmentValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoAssignment) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadAssignment(nota);

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
                "PROFESSIONAL_REVIEW_ASSIGNMENT_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearAssignment(nota));
    }

    [HttpDelete("assignment/{id:guid}")]
    public async Task<IActionResult> ArquivarAssignment(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoAssignment),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_ASSIGNMENT_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("task-coordination/closure")]
    public ActionResult<ProfessionalReviewTaskCoordinationClosureResponse> FechamentoTaskCoordination()
    {
        var componentes = new[]
        {
            "TaskCoordinationFoundation",
            "TaskCoordinationPersistence",
            "TaskCoordinationStatus",
            "TaskCoordinationHistory",
            "TaskCoordinationFilters",
            "TaskCoordinationSummary"
        };

        return Ok(new ProfessionalReviewTaskCoordinationClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTaskCoordinationCompleta",
            "O fechamento descreve apenas disponibilidade estrutural da Task Coordination. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica."));
    }

    [HttpGet("task-coordination/summary")]
    public async Task<ActionResult<ProfessionalReviewTaskCoordinationSummaryResponse>> ResumoTaskCoordination(
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
                x.Categoria.StartsWith(PrefixoTaskCoordination))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTaskCoordination).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var planejadas = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidas = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var canceladas = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.Responsavel))
            .GroupBy(x => x.Responsavel!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTaskCoordinationResponsavelResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Responsavel)
            .ToArray();

        return Ok(new ProfessionalReviewTaskCoordinationSummaryResponse(
            total,
            ativas,
            planejadas,
            emAndamento,
            concluidas,
            canceladas,
            arquivadas,
            porResponsavel,
            "O resumo apresenta somente agregações documentais das tarefas operacionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação."));
    }

    [HttpGet("task-coordination/search")]
    public async Task<ActionResult<ProfessionalReviewTaskCoordinationFiltersResponse>> FiltrarTaskCoordination(
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
        if (statusNormalizado is not null && !StatusTaskCoordinationValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var responsavelNormalizado = NormalizarOpcional(responsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTaskCoordination) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTaskCoordination)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    (x.Responsavel?.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.TarefaOperacional.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTaskCoordinationFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar tarefas operacionais. Não classificam urgência, risco, prioridade clínica, resposta ao tratamento ou necessidade de intervenção."));
    }

    [HttpGet("task-coordination")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTaskCoordinationPersistedResponse>>> ListarTaskCoordination(
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
                x.Categoria.StartsWith(PrefixoTaskCoordination));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTaskCoordination).ToArray());
    }

    [HttpPost("task-coordination")]
    public async Task<ActionResult<ProfessionalReviewTaskCoordinationPersistedResponse>> CriarTaskCoordination(
        Guid pacienteId,
        CriarProfessionalReviewTaskCoordinationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var tarefa = NormalizarObrigatorio(request.TarefaOperacional, 1000);
        if (tarefa is null)
            return BadRequest(new { message = "Informe a tarefa operacional." });

        if (request.ActionPlanRelacionadoId.HasValue &&
            !await ActionPlanPertencePacienteAsync(pacienteId, request.ActionPlanRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Action Plan relacionado inválido para este paciente." });
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
            Categoria = PrefixoTaskCoordination + "item",
            Conteudo = MontarPayloadTaskCoordination(
                tarefa,
                request.ActionPlanRelacionadoId,
                request.Responsavel,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TASK_COORDINATION_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTaskCoordination(nota));
    }

    [HttpPut("task-coordination/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTaskCoordinationPersistedResponse>> AtualizarTaskCoordination(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTaskCoordinationRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTaskCoordination),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var tarefa = NormalizarObrigatorio(request.TarefaOperacional, 1000);
        if (tarefa is null)
            return BadRequest(new { message = "Informe a tarefa operacional." });

        if (request.ActionPlanRelacionadoId.HasValue &&
            !await ActionPlanPertencePacienteAsync(pacienteId, request.ActionPlanRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Action Plan relacionado inválido para este paciente." });
        }

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTaskCoordination(nota);
        var payloadAtualizado = new TaskCoordinationPayload(
            tarefa,
            request.ActionPlanRelacionadoId,
            NormalizarOpcional(request.Responsavel, 160),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TASK_COORDINATION_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTaskCoordination(nota));
    }

    [HttpGet("task-coordination/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTaskCoordinationHistoryResponse>> HistoricoTaskCoordination(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var tarefaExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTaskCoordination),
                cancellationToken);

        if (!tarefaExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TASK_COORDINATION_"));

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

            return new ProfessionalReviewTaskCoordinationHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTaskCoordination(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTaskCoordinationHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais da Task Coordination. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado."));
    }

    [HttpPatch("task-coordination/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTaskCoordinationPersistedResponse>> AtualizarStatusTaskCoordination(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTaskCoordinationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTaskCoordinationValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTaskCoordination) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTaskCoordination(nota);

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
                "PROFESSIONAL_REVIEW_TASK_COORDINATION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTaskCoordination(nota));
    }

    [HttpDelete("task-coordination/{id:guid}")]
    public async Task<IActionResult> ArquivarTaskCoordination(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTaskCoordination),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TASK_COORDINATION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("action-plan/closure")]
    public ActionResult<ProfessionalReviewActionPlanClosureResponse> FechamentoActionPlan()
    {
        var componentes = new[]
        {
            "ActionPlanFoundation",
            "ActionPlanPersistence",
            "ActionPlanStatus",
            "ActionPlanHistory",
            "ActionPlanFilters",
            "ActionPlanSummary"
        };

        return Ok(new ProfessionalReviewActionPlanClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaActionPlanCompleta",
            "O fechamento descreve apenas disponibilidade estrutural do Action Plan. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica."));
    }

    [HttpGet("action-plan/summary")]
    public async Task<ActionResult<ProfessionalReviewActionPlanSummaryResponse>> ResumoActionPlan(
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
                x.Categoria.StartsWith(PrefixoActionPlan))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearActionPlan).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var planejadas = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidas = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var canceladas = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.Responsavel))
            .GroupBy(x => x.Responsavel!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewActionPlanResponsavelResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Responsavel)
            .ToArray();

        return Ok(new ProfessionalReviewActionPlanSummaryResponse(
            total,
            ativos,
            planejadas,
            emAndamento,
            concluidas,
            canceladas,
            arquivadas,
            porResponsavel,
            "O resumo apresenta somente agregações documentais do Action Plan. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação."));
    }

    [HttpGet("action-plan/search")]
    public async Task<ActionResult<ProfessionalReviewActionPlanFiltersResponse>> FiltrarActionPlan(
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
        if (statusNormalizado is not null && !StatusActionPlanValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var responsavelNormalizado = NormalizarOpcional(responsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoActionPlan) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearActionPlan)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    (x.Responsavel?.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.AcaoOperacional.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ObjetivoRelacionado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewActionPlanFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros do Action Plan. Não classificam urgência, risco, prioridade clínica, resposta ao tratamento ou necessidade de intervenção."));
    }

    [HttpGet("action-plan")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewActionPlanPersistedResponse>>> ListarActionPlan(
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
                x.Categoria.StartsWith(PrefixoActionPlan));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearActionPlan).ToArray());
    }

    [HttpPost("action-plan")]
    public async Task<ActionResult<ProfessionalReviewActionPlanPersistedResponse>> CriarActionPlan(
        Guid pacienteId,
        CriarProfessionalReviewActionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var acao = NormalizarObrigatorio(request.AcaoOperacional, 1000);
        if (acao is null)
            return BadRequest(new { message = "Informe a ação operacional." });

        if (request.CarePlanRelacionadoId.HasValue &&
            !await CarePlanPertencePacienteAsync(pacienteId, request.CarePlanRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Care Plan relacionado inválido para este paciente." });
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
            Categoria = PrefixoActionPlan + "item",
            Conteudo = MontarPayloadActionPlan(
                acao,
                request.ObjetivoRelacionado,
                request.Responsavel,
                request.Horizonte,
                request.CarePlanRelacionadoId,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_ACTION_PLAN_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearActionPlan(nota));
    }

    [HttpPut("action-plan/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewActionPlanPersistedResponse>> AtualizarActionPlan(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewActionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoActionPlan),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var acao = NormalizarObrigatorio(request.AcaoOperacional, 1000);
        if (acao is null)
            return BadRequest(new { message = "Informe a ação operacional." });

        if (request.CarePlanRelacionadoId.HasValue &&
            !await CarePlanPertencePacienteAsync(pacienteId, request.CarePlanRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Care Plan relacionado inválido para este paciente." });
        }

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadActionPlan(nota);
        var payloadAtualizado = new ActionPlanPayload(
            acao,
            NormalizarOpcional(request.ObjetivoRelacionado, 500),
            NormalizarOpcional(request.Responsavel, 160),
            NormalizarOpcional(request.Horizonte, 120),
            request.CarePlanRelacionadoId,
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_ACTION_PLAN_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearActionPlan(nota));
    }

    [HttpGet("action-plan/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewActionPlanHistoryResponse>> HistoricoActionPlan(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var actionPlanExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoActionPlan),
                cancellationToken);

        if (!actionPlanExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_ACTION_PLAN_"));

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

            return new ProfessionalReviewActionPlanHistoryItemResponse(
                log.Id,
                id,
                MapearEventoActionPlan(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewActionPlanHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais do Action Plan. Não interpreta evolução clínica, causalidade, prioridade, risco ou resultado."));
    }

    [HttpPatch("action-plan/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewActionPlanPersistedResponse>> AtualizarStatusActionPlan(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewActionPlanStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusActionPlanValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoActionPlan) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadActionPlan(nota);

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
                "PROFESSIONAL_REVIEW_ACTION_PLAN_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearActionPlan(nota));
    }

    [HttpDelete("action-plan/{id:guid}")]
    public async Task<IActionResult> ArquivarActionPlan(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoActionPlan),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_ACTION_PLAN_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("care-plan/closure")]
    public ActionResult<ProgressReviewCarePlanClosureResponse> FechamentoCarePlan()
    {
        var componentes = new[]
        {
            "CarePlanFoundation",
            "CarePlanPersistence",
            "CarePlanStatus",
            "CarePlanHistory",
            "CarePlanFilters",
            "CarePlanSummary"
        };

        return Ok(new ProgressReviewCarePlanClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaCarePlanCompleta",
            "O fechamento descreve apenas disponibilidade estrutural do Care Plan. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica."));
    }

    [HttpGet("care-plan/summary")]
    public async Task<ActionResult<ProgressReviewCarePlanSummaryResponse>> ResumoCarePlan(
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
                x.Categoria.StartsWith(PrefixoCarePlan))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearCarePlan).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var planejados = itens.Count(x => !x.Arquivada && x.Status == "Planejado");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidos = itens.Count(x => !x.Arquivada && x.Status == "Concluido");
        var cancelados = itens.Count(x => !x.Arquivada && x.Status == "Cancelado");

        var porResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.Responsavel))
            .GroupBy(x => x.Responsavel!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProgressReviewCarePlanResponsavelResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Responsavel)
            .ToArray();

        return Ok(new ProgressReviewCarePlanSummaryResponse(
            total,
            ativos,
            planejados,
            emAndamento,
            concluidos,
            cancelados,
            arquivados,
            porResponsavel,
            "O resumo apresenta somente agregações documentais do Care Plan. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação."));
    }

    [HttpGet("care-plan/search")]
    public async Task<ActionResult<ProgressReviewCarePlanFiltersResponse>> FiltrarCarePlan(
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
        if (statusNormalizado is not null && !StatusCarePlanValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var responsavelNormalizado = NormalizarOpcional(responsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCarePlan) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearCarePlan)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    (x.Responsavel?.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ObjetivoCuidado.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    x.AcaoPlanejada.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProgressReviewCarePlanFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros do Care Plan. Não classificam urgência, risco, prioridade clínica, resposta ao tratamento ou necessidade de intervenção."));
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

        var payloadAtual = LerPayloadCarePlan(nota);
        var payloadAtualizado = new CarePlanPayload(
            objetivo,
            acao,
            NormalizarOpcional(request.Responsavel, 160),
            NormalizarOpcional(request.Horizonte, 120),
            request.FollowUpRelacionadoId,
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROGRESS_REVIEW_CARE_PLAN_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearCarePlan(nota));
    }

    [HttpGet("care-plan/{id:guid}/history")]
    public async Task<ActionResult<ProgressReviewCarePlanHistoryResponse>> HistoricoCarePlan(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var carePlanExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCarePlan),
                cancellationToken);

        if (!carePlanExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROGRESS_REVIEW_CARE_PLAN_"));

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

            return new ProgressReviewCarePlanHistoryItemResponse(
                log.Id,
                id,
                MapearEventoCarePlan(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProgressReviewCarePlanHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais do Care Plan. Não interpreta evolução clínica, causalidade, prioridade, risco ou resultado."));
    }

    [HttpPatch("care-plan/{id:guid}/status")]
    public async Task<ActionResult<ProgressReviewCarePlanPersistedResponse>> AtualizarStatusCarePlan(
        Guid pacienteId,
        Guid id,
        ProgressReviewCarePlanStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusCarePlanValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCarePlan) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadCarePlan(nota);

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
                "PROGRESS_REVIEW_CARE_PLAN_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

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
        string? ObservacaoProfissional,
        string Status = "Planejado",
        DateTime? StatusAtualizadoEmUtc = null);

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
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private sealed record DelegationPayload(
        string ProfissionalDelegante,
        string ProfissionalDelegado,
        Guid? AssignmentRelacionadaId,
        string? ContextoDelegacao,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadDelegation(
        string profissionalDelegante,
        string profissionalDelegado,
        Guid? assignmentRelacionadaId,
        string? contextoDelegacao,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new DelegationPayload(
            profissionalDelegante,
            profissionalDelegado,
            assignmentRelacionadaId,
            NormalizarOpcional(contextoDelegacao, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewDelegationPersistedResponse MapearDelegation(NotaInternaProfissional nota)
    {
        DelegationPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<DelegationPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewDelegationPersistedResponse(
            nota.Id,
            payload?.AssignmentRelacionadaId,
            payload?.ProfissionalDelegante ?? nota.Conteudo,
            payload?.ProfissionalDelegado ?? string.Empty,
            payload?.ContextoDelegacao,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejada",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoDelegation(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_DELEGATION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_DELEGATION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_DELEGATION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_DELEGATION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static DelegationPayload LerPayloadDelegation(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<DelegationPayload>(nota.Conteudo)
                ?? new DelegationPayload(nota.Conteudo, string.Empty, null, null, null, null);
        }
        catch (JsonException)
        {
            return new DelegationPayload(nota.Conteudo, string.Empty, null, null, null, null);
        }
    }

    private static bool StatusDelegationValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private sealed record AssignmentPayload(
        string ResponsavelPrincipal,
        Guid? TaskCoordinationRelacionadaId,
        string? ApoioParticipante,
        string? ContextoAtribuicao,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadAssignment(
        string responsavelPrincipal,
        Guid? taskCoordinationRelacionadaId,
        string? apoioParticipante,
        string? contextoAtribuicao,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new AssignmentPayload(
            responsavelPrincipal,
            taskCoordinationRelacionadaId,
            NormalizarOpcional(apoioParticipante, 160),
            NormalizarOpcional(contextoAtribuicao, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewAssignmentPersistedResponse MapearAssignment(NotaInternaProfissional nota)
    {
        AssignmentPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<AssignmentPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewAssignmentPersistedResponse(
            nota.Id,
            payload?.TaskCoordinationRelacionadaId,
            payload?.ResponsavelPrincipal ?? nota.Conteudo,
            payload?.ApoioParticipante,
            payload?.ContextoAtribuicao,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejada",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoAssignment(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_ASSIGNMENT_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_ASSIGNMENT_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_ASSIGNMENT_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_ASSIGNMENT_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static AssignmentPayload LerPayloadAssignment(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<AssignmentPayload>(nota.Conteudo)
                ?? new AssignmentPayload(nota.Conteudo, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new AssignmentPayload(nota.Conteudo, null, null, null, null, null);
        }
    }

    private static bool StatusAssignmentValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private sealed record TaskCoordinationPayload(
        string TarefaOperacional,
        Guid? ActionPlanRelacionadoId,
        string? Responsavel,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTaskCoordination(
        string tarefaOperacional,
        Guid? actionPlanRelacionadoId,
        string? responsavel,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TaskCoordinationPayload(
            tarefaOperacional,
            actionPlanRelacionadoId,
            NormalizarOpcional(responsavel, 160),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTaskCoordinationPersistedResponse MapearTaskCoordination(NotaInternaProfissional nota)
    {
        TaskCoordinationPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TaskCoordinationPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTaskCoordinationPersistedResponse(
            nota.Id,
            payload?.TarefaOperacional ?? nota.Conteudo,
            payload?.ActionPlanRelacionadoId,
            payload?.Responsavel,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejada",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private sealed record ActionPlanPayload(
        string AcaoOperacional,
        string? ObjetivoRelacionado,
        string? Responsavel,
        string? Horizonte,
        Guid? CarePlanRelacionadoId,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadActionPlan(
        string acaoOperacional,
        string? objetivoRelacionado,
        string? responsavel,
        string? horizonte,
        Guid? carePlanRelacionadoId,
        string? observacaoProfissional)
    {
        var payload = new ActionPlanPayload(
            acaoOperacional,
            NormalizarOpcional(objetivoRelacionado, 500),
            NormalizarOpcional(responsavel, 160),
            NormalizarOpcional(horizonte, 120),
            carePlanRelacionadoId,
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewActionPlanPersistedResponse MapearActionPlan(NotaInternaProfissional nota)
    {
        ActionPlanPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<ActionPlanPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewActionPlanPersistedResponse(
            nota.Id,
            payload?.AcaoOperacional ?? nota.Conteudo,
            payload?.ObjetivoRelacionado,
            payload?.Responsavel,
            payload?.Horizonte,
            payload?.CarePlanRelacionadoId,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejada",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoActionPlan(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_ACTION_PLAN_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_ACTION_PLAN_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_ACTION_PLAN_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_ACTION_PLAN_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static ActionPlanPayload LerPayloadActionPlan(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<ActionPlanPayload>(nota.Conteudo)
                ?? new ActionPlanPayload(nota.Conteudo, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new ActionPlanPayload(nota.Conteudo, null, null, null, null, null);
        }
    }

    private static bool StatusActionPlanValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private static string MapearEventoTaskCoordination(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TASK_COORDINATION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TASK_COORDINATION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TASK_COORDINATION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TASK_COORDINATION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TaskCoordinationPayload LerPayloadTaskCoordination(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TaskCoordinationPayload>(nota.Conteudo)
                ?? new TaskCoordinationPayload(nota.Conteudo, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TaskCoordinationPayload(nota.Conteudo, null, null, null, null);
        }
    }

    private static bool StatusTaskCoordinationValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private async Task<bool> AssignmentPertencePacienteAsync(
        Guid pacienteId,
        Guid assignmentId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == assignmentId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoAssignment) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TaskCoordinationPertencePacienteAsync(
        Guid pacienteId,
        Guid taskCoordinationId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == taskCoordinationId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTaskCoordination) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> ActionPlanPertencePacienteAsync(
        Guid pacienteId,
        Guid actionPlanId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == actionPlanId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoActionPlan) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> CarePlanPertencePacienteAsync(
        Guid pacienteId,
        Guid carePlanId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == carePlanId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCarePlan) &&
                !x.Arquivada,
                cancellationToken);

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
            payload?.Status ?? "Planejado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoCarePlan(string acao) =>
        acao switch
        {
            "PROGRESS_REVIEW_CARE_PLAN_CREATED" => "Criado",
            "PROGRESS_REVIEW_CARE_PLAN_UPDATED" => "Editado",
            "PROGRESS_REVIEW_CARE_PLAN_STATUS_CHANGED" => "StatusAlterado",
            "PROGRESS_REVIEW_CARE_PLAN_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static CarePlanPayload LerPayloadCarePlan(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<CarePlanPayload>(nota.Conteudo)
                ?? new CarePlanPayload(nota.Conteudo, string.Empty, null, null, null, null);
        }
        catch (JsonException)
        {
            return new CarePlanPayload(nota.Conteudo, string.Empty, null, null, null, null);
        }
    }

    private static bool StatusCarePlanValido(string? status) =>
        status is "Planejado" or "EmAndamento" or "Concluido" or "Cancelado";

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
