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
    private const string PrefixoHandoff = "ProfessionalReviewHandoff:";
    private const string PrefixoContinuity = "ProfessionalReviewContinuity:";
    private const string PrefixoEscalation = "ProfessionalReviewEscalation:";
    private const string PrefixoCoordination = "ProfessionalReviewCoordination:";
    private const string PrefixoCollaboration = "ProfessionalReviewCollaboration:";
    private const string PrefixoSharedContext = "ProfessionalReviewSharedContext:";
    private const string PrefixoTeamAlignment = "ProfessionalReviewTeamAlignment:";
    private const string PrefixoTeamDecision = "ProfessionalReviewTeamDecision:";
    private const string PrefixoTeamOutcome = "ProfessionalReviewTeamOutcome:";
    private const string PrefixoTeamLearning = "ProfessionalReviewTeamLearning:";
    private const string PrefixoTeamInsight = "ProfessionalReviewTeamInsight:";
    private const string PrefixoTeamKnowledge = "ProfessionalReviewTeamKnowledge:";
    private const string PrefixoTeamKnowledgeApplication = "ProfessionalReviewTeamKnowledgeApplication:";
    private const string PrefixoTeamKnowledgeEffect = "ProfessionalReviewTeamKnowledgeEffect:";
    private const string PrefixoTeamKnowledgeEffectReview = "ProfessionalReviewTeamKnowledgeEffectReview:";
    private const string PrefixoTeamKnowledgeEffectDecision = "ProfessionalReviewTeamKnowledgeEffectDecision:";
    private const string PrefixoTeamKnowledgeEffectDecisionReview = "ProfessionalReviewTeamKnowledgeEffectDecisionReview:";

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

    public sealed record CriarProfessionalReviewHandoffRequest(
        string ProfissionalOrigem,
        string ProfissionalDestino,
        Guid? DelegationRelacionadaId = null,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoTransferido = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewHandoffRequest(
        string ProfissionalOrigem,
        string ProfissionalDestino,
        Guid? DelegationRelacionadaId = null,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoTransferido = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("handoff/foundation")]
    public ActionResult<ProfessionalReviewHandoffFoundationResponse> HandoffFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewHandoffFieldResponse(
                "delegation-relacionada",
                "Delegation relacionada",
                false,
                "referencia",
                "Permite relacionar o handoff a uma delegação profissional já documentada."),
            new ProfessionalReviewHandoffFieldResponse(
                "assignment-relacionada",
                "Assignment relacionada",
                false,
                "referencia",
                "Permite relacionar o handoff a uma atribuição profissional já documentada."),
            new ProfessionalReviewHandoffFieldResponse(
                "profissional-origem",
                "Profissional de origem",
                true,
                "texto",
                "Identifica documentalmente quem realiza a passagem de contexto."),
            new ProfessionalReviewHandoffFieldResponse(
                "profissional-destino",
                "Profissional de destino",
                true,
                "texto",
                "Identifica documentalmente quem recebe o contexto do handoff."),
            new ProfessionalReviewHandoffFieldResponse(
                "contexto-transferido",
                "Contexto transferido",
                false,
                "texto-longo",
                "Documenta o contexto operacional compartilhado sem caracterizar decisão clínica automática."),
            new ProfessionalReviewHandoffFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewHandoffFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes para a equipe.")
        };

        return Ok(new ProfessionalReviewHandoffFoundationResponse(
            "FundacaoHandoffDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza handoffs documentais entre profissionais e, a partir da v0.37.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewContinuityRequest(
        string ProfissionalSeguimento,
        Guid? HandoffRelacionadoId = null,
        Guid? DelegationRelacionadaId = null,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoContinuidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewContinuityRequest(
        string ProfissionalSeguimento,
        Guid? HandoffRelacionadoId = null,
        Guid? DelegationRelacionadaId = null,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoContinuidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("continuity/foundation")]
    public ActionResult<ProfessionalReviewContinuityFoundationResponse> ContinuityFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewContinuityFieldResponse(
                "handoff-relacionado",
                "Handoff relacionado",
                false,
                "referencia",
                "Permite relacionar a continuidade a um handoff profissional já documentado."),
            new ProfessionalReviewContinuityFieldResponse(
                "delegation-relacionada",
                "Delegation relacionada",
                false,
                "referencia",
                "Permite relacionar a continuidade a uma delegação profissional já documentada."),
            new ProfessionalReviewContinuityFieldResponse(
                "assignment-relacionada",
                "Assignment relacionada",
                false,
                "referencia",
                "Permite relacionar a continuidade a uma atribuição profissional já documentada."),
            new ProfessionalReviewContinuityFieldResponse(
                "profissional-seguimento",
                "Profissional de seguimento",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo acompanhamento de continuidade."),
            new ProfessionalReviewContinuityFieldResponse(
                "contexto-continuidade",
                "Contexto de continuidade",
                false,
                "texto-longo",
                "Documenta o contexto compartilhado necessário para continuidade operacional da equipe."),
            new ProfessionalReviewContinuityFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewContinuityFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes para a continuidade.")
        };

        return Ok(new ProfessionalReviewContinuityFoundationResponse(
            "FundacaoContinuityDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza continuidade documental entre profissionais e, a partir da v0.38.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewEscalationRequest(
        string ProfissionalOrigem,
        string ProfissionalDestino,
        Guid? ContinuityRelacionadaId = null,
        Guid? HandoffRelacionadoId = null,
        Guid? DelegationRelacionadaId = null,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoEscalado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewEscalationRequest(
        string ProfissionalOrigem,
        string ProfissionalDestino,
        Guid? ContinuityRelacionadaId = null,
        Guid? HandoffRelacionadoId = null,
        Guid? DelegationRelacionadaId = null,
        Guid? AssignmentRelacionadaId = null,
        string? ContextoEscalado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("escalation/foundation")]
    public ActionResult<ProfessionalReviewEscalationFoundationResponse> EscalationFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewEscalationFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o escalonamento a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewEscalationFieldResponse(
                "handoff-relacionado",
                "Handoff relacionado",
                false,
                "referencia",
                "Permite relacionar o escalonamento a um handoff profissional já documentado."),
            new ProfessionalReviewEscalationFieldResponse(
                "delegation-relacionada",
                "Delegation relacionada",
                false,
                "referencia",
                "Permite relacionar o escalonamento a uma delegação profissional já documentada."),
            new ProfessionalReviewEscalationFieldResponse(
                "assignment-relacionada",
                "Assignment relacionada",
                false,
                "referencia",
                "Permite relacionar o escalonamento a uma atribuição profissional já documentada."),
            new ProfessionalReviewEscalationFieldResponse(
                "profissional-origem",
                "Profissional de origem",
                true,
                "texto",
                "Identifica documentalmente o profissional que inicia o escalonamento."),
            new ProfessionalReviewEscalationFieldResponse(
                "profissional-destino",
                "Profissional de destino",
                true,
                "texto",
                "Identifica documentalmente o profissional para quem o contexto é escalado."),
            new ProfessionalReviewEscalationFieldResponse(
                "contexto-escalado",
                "Contexto escalado",
                false,
                "texto-longo",
                "Documenta o contexto compartilhado entre profissionais sem definir prioridade clínica."),
            new ProfessionalReviewEscalationFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewEscalationFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes ao escalonamento.")
        };

        return Ok(new ProfessionalReviewEscalationFoundationResponse(
            "FundacaoEscalationDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza escalonamento documental entre profissionais e, a partir da v0.39.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewCoordinationRequest(
        string ProfissionalCoordenador,
        Guid? AssignmentRelacionadaId = null,
        Guid? DelegationRelacionadaId = null,
        Guid? HandoffRelacionadoId = null,
        Guid? ContinuityRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        string? ContextoCoordenacao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewCoordinationRequest(
        string ProfissionalCoordenador,
        Guid? AssignmentRelacionadaId = null,
        Guid? DelegationRelacionadaId = null,
        Guid? HandoffRelacionadoId = null,
        Guid? ContinuityRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        string? ContextoCoordenacao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("coordination/foundation")]
    public ActionResult<ProfessionalReviewCoordinationFoundationResponse> CoordinationFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewCoordinationFieldResponse(
                "assignment-relacionada",
                "Assignment relacionada",
                false,
                "referencia",
                "Permite relacionar a coordenação a uma atribuição profissional já documentada."),
            new ProfessionalReviewCoordinationFieldResponse(
                "delegation-relacionada",
                "Delegation relacionada",
                false,
                "referencia",
                "Permite relacionar a coordenação a uma delegação profissional já documentada."),
            new ProfessionalReviewCoordinationFieldResponse(
                "handoff-relacionado",
                "Handoff relacionado",
                false,
                "referencia",
                "Permite relacionar a coordenação a um handoff profissional já documentado."),
            new ProfessionalReviewCoordinationFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar a coordenação a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewCoordinationFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar a coordenação a um escalonamento profissional já documentado."),
            new ProfessionalReviewCoordinationFieldResponse(
                "profissional-coordenador",
                "Profissional coordenador",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pela coordenação operacional do acompanhamento."),
            new ProfessionalReviewCoordinationFieldResponse(
                "contexto-coordenacao",
                "Contexto de coordenação",
                false,
                "texto-longo",
                "Documenta o contexto compartilhado entre profissionais sem definir prioridade clínica."),
            new ProfessionalReviewCoordinationFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewCoordinationFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes à coordenação.")
        };

        return Ok(new ProfessionalReviewCoordinationFoundationResponse(
            "FundacaoCoordinationDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza coordenação documental integrada entre Assignment, Delegation, Handoff, Continuity e Escalation e, a partir da v0.40.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewCollaborationRequest(
        string ProfissionalResponsavel,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? ProfissionaisParticipantes = null,
        string? ContextoColaboracao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewCollaborationRequest(
        string ProfissionalResponsavel,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? ProfissionaisParticipantes = null,
        string? ContextoColaboracao = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("collaboration/foundation")]
    public ActionResult<ProfessionalReviewCollaborationFoundationResponse> CollaborationFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewCollaborationFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar a colaboração a uma coordenação profissional já documentada."),
            new ProfessionalReviewCollaborationFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar a colaboração a um escalonamento profissional já documentado."),
            new ProfessionalReviewCollaborationFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar a colaboração a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewCollaborationFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pela colaboração."),
            new ProfessionalReviewCollaborationFieldResponse(
                "profissionais-participantes",
                "Profissionais participantes",
                false,
                "texto",
                "Registra os profissionais participantes da colaboração documental."),
            new ProfessionalReviewCollaborationFieldResponse(
                "contexto-colaboracao",
                "Contexto de colaboração",
                false,
                "texto-longo",
                "Documenta o contexto compartilhado entre profissionais sem definir prioridade clínica."),
            new ProfessionalReviewCollaborationFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewCollaborationFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes à colaboração.")
        };

        return Ok(new ProfessionalReviewCollaborationFoundationResponse(
            "FundacaoCollaborationDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza colaboração documental compartilhada entre Coordination, Escalation e Continuity e, a partir da v0.41.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewSharedContextRequest(
        string ProfissionalResponsavel,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoCompartilhado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewSharedContextRequest(
        string ProfissionalResponsavel,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoCompartilhado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("shared-context/foundation")]
    public ActionResult<ProfessionalReviewSharedContextFoundationResponse> SharedContextFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewSharedContextFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar o contexto compartilhado a uma colaboração profissional já documentada."),
            new ProfessionalReviewSharedContextFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar o contexto compartilhado a uma coordenação profissional já documentada."),
            new ProfessionalReviewSharedContextFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar o contexto compartilhado a um escalonamento profissional já documentado."),
            new ProfessionalReviewSharedContextFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o contexto compartilhado a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewSharedContextFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo contexto compartilhado."),
            new ProfessionalReviewSharedContextFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes do contexto compartilhado."),
            new ProfessionalReviewSharedContextFieldResponse(
                "contexto-compartilhado",
                "Contexto compartilhado",
                false,
                "texto-longo",
                "Documenta o contexto compartilhado entre profissionais sem definir prioridade clínica."),
            new ProfessionalReviewSharedContextFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewSharedContextFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes ao contexto compartilhado.")
        };

        return Ok(new ProfessionalReviewSharedContextFoundationResponse(
            "FundacaoSharedContextDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza contexto profissional compartilhado entre Collaboration, Coordination, Escalation e Continuity e, a partir da v0.42.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewTeamAlignmentRequest(
        string ProfissionalResponsavel,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ObjetivoAlinhamento = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamAlignmentRequest(
        string ProfissionalResponsavel,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ObjetivoAlinhamento = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-alignment/foundation")]
    public ActionResult<ProfessionalReviewTeamAlignmentFoundationResponse> TeamAlignmentFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar o alinhamento a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar o alinhamento a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar o alinhamento a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar o alinhamento a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o alinhamento a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo alinhamento de equipe."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes do alinhamento."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "objetivo-alinhamento",
                "Objetivo de alinhamento",
                false,
                "texto-longo",
                "Documenta o objetivo operacional do alinhamento entre profissionais sem definir prioridade clínica."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamAlignmentFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações documentais relevantes ao alinhamento da equipe.")
        };

        return Ok(new ProfessionalReviewTeamAlignmentFoundationResponse(
            "FundacaoTeamAlignmentDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza alinhamento documental entre profissionais usando Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.43.1, possui persistência auditada. Não executa condutas, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewTeamDecisionRequest(
        string ProfissionalResponsavel,
        string DecisaoDocumentada,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? RacionalJustificativa = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamDecisionRequest(
        string ProfissionalResponsavel,
        string DecisaoDocumentada,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? RacionalJustificativa = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-decision/foundation")]
    public ActionResult<ProfessionalReviewTeamDecisionFoundationResponse> TeamDecisionFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamDecisionFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um alinhamento entre profissionais já documentado."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo registro da decisão."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes da decisão documentada."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "decisao-documentada",
                "Decisão documentada",
                true,
                "texto-longo",
                "Registra a decisão profissional acordada sem executá-la automaticamente."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "racional-justificativa",
                "Racional / justificativa",
                false,
                "texto-longo",
                "Documenta o racional profissional associado à decisão sem produzir recomendação automática."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamDecisionFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais sobre a decisão da equipe.")
        };

        return Ok(new ProfessionalReviewTeamDecisionFoundationResponse(
            "FundacaoTeamDecisionDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza decisões documentadas entre profissionais usando Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.44.1, possui persistência auditada. Não executa condutas, não cria prescrição automaticamente, não transfere automaticamente responsabilidade clínica, não define prioridade clínica, não classifica risco e não substitui decisão profissional."));
    }

    public sealed record CriarProfessionalReviewTeamOutcomeRequest(
        string ProfissionalResponsavel,
        string ResultadoDocumentado,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? EvidenciaSuporte = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamOutcomeRequest(
        string ProfissionalResponsavel,
        string ResultadoDocumentado,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? EvidenciaSuporte = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-outcome/foundation")]
    public ActionResult<ProfessionalReviewTeamOutcomeFoundationResponse> TeamOutcomeFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar o resultado a uma decisão de equipe já documentada."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar o resultado a um alinhamento entre profissionais já documentado."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar o resultado a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar o resultado a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar o resultado a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar o resultado a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o resultado a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo registro do resultado."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes do acompanhamento do resultado."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "resultado-documentado",
                "Resultado documentado",
                true,
                "texto-longo",
                "Registra o resultado observado sem inferir automaticamente causalidade, prognóstico ou recomendação."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "evidencia-suporte",
                "Evidência / registro de suporte",
                false,
                "texto-longo",
                "Permite documentar evidências ou registros de suporte sem produzir interpretação clínica automática."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamOutcomeFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais sobre o resultado documentado.")
        };

        return Ok(new ProfessionalReviewTeamOutcomeFoundationResponse(
            "FundacaoTeamOutcomeDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza resultados documentados das decisões da equipe usando Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.45.1, possui persistência auditada. Não infere causalidade, não produz prognóstico ou recomendação automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamLearningRequest(
        string ProfissionalResponsavel,
        string AprendizadoDocumentado,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? EvidenciaBaseObservacional = null,
        string? Aplicabilidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamLearningRequest(
        string ProfissionalResponsavel,
        string AprendizadoDocumentado,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? EvidenciaBaseObservacional = null,
        string? Aplicabilidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-learning/foundation")]
    public ActionResult<ProfessionalReviewTeamLearningFoundationResponse> TeamLearningFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamLearningFieldResponse(
                "team-outcome-relacionado",
                "Team Outcome relacionado",
                false,
                "referencia",
                "Permite relacionar o aprendizado a um resultado de equipe já documentado."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar o aprendizado a uma decisão de equipe já documentada."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar o aprendizado a um alinhamento entre profissionais já documentado."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar o aprendizado a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar o aprendizado a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar o aprendizado a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar o aprendizado a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o aprendizado a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo registro do aprendizado."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes do aprendizado documentado."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "aprendizado-documentado",
                "Aprendizado documentado",
                true,
                "texto-longo",
                "Registra o aprendizado observado pela equipe sem transformá-lo automaticamente em evidência clínica validada."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "evidencia-base-observacional",
                "Evidência / base observacional",
                false,
                "texto-longo",
                "Permite registrar a base observacional sem inferir causalidade ou força de evidência."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "aplicabilidade",
                "Aplicabilidade",
                false,
                "texto-longo",
                "Permite documentar onde o aprendizado pode ser considerado sem gerar recomendação automática."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamLearningFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais sobre o aprendizado documentado.")
        };

        return Ok(new ProfessionalReviewTeamLearningFoundationResponse(
            "FundacaoTeamLearningDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza aprendizados documentados da equipe usando Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.46.1, possui persistência auditada. Não infere causalidade, não transforma aprendizado em evidência clínica validada, não produz prognóstico ou recomendação automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamInsightRequest(
        string ProfissionalResponsavel,
        string InsightDocumentado,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? Aplicabilidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamInsightRequest(
        string ProfissionalResponsavel,
        string InsightDocumentado,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? Aplicabilidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-insight/foundation")]
    public ActionResult<ProfessionalReviewTeamInsightFoundationResponse> TeamInsightFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamInsightFieldResponse(
                "team-learning-relacionado",
                "Team Learning relacionado",
                false,
                "referencia",
                "Permite relacionar o insight a um aprendizado de equipe já documentado."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "team-outcome-relacionado",
                "Team Outcome relacionado",
                false,
                "referencia",
                "Permite relacionar o insight a um resultado de equipe já documentado."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar o insight a uma decisão de equipe já documentada."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar o insight a um alinhamento entre profissionais já documentado."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar o insight a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar o insight a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar o insight a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar o insight a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o insight a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo registro do insight."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes da construção do insight documentado."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "insight-documentado",
                "Insight documentado",
                true,
                "texto-longo",
                "Registra o insight profissional sem transformá-lo automaticamente em evidência clínica validada."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "base-observacional-evidencia-suporte",
                "Base observacional / evidência de suporte",
                false,
                "texto-longo",
                "Permite registrar observações ou evidências de suporte sem inferir causalidade ou força de evidência."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "interpretacao-profissional",
                "Interpretação profissional",
                false,
                "texto-longo",
                "Permite registrar interpretação humana explícita sem transformá-la em conclusão clínica automática."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "aplicabilidade",
                "Aplicabilidade",
                false,
                "texto-longo",
                "Permite documentar onde o insight pode ser considerado sem gerar recomendação automática."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamInsightFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais sobre o insight documentado.")
        };

        return Ok(new ProfessionalReviewTeamInsightFoundationResponse(
            "FundacaoTeamInsightDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza insights documentados da equipe usando Team Learning, Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.47.1, possui persistência auditada. Não transforma insight em evidência clínica validada, não infere causalidade, não produz prognóstico, recomendação ou decisão terapêutica automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamKnowledgeRequest(
        string ProfissionalResponsavel,
        string ConhecimentoDocumentado,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? Aplicabilidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamKnowledgeRequest(
        string ProfissionalResponsavel,
        string ConhecimentoDocumentado,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? Aplicabilidade = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-knowledge/foundation")]
    public ActionResult<ProfessionalReviewTeamKnowledgeFoundationResponse> TeamKnowledgeFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "team-insight-relacionado",
                "Team Insight relacionado",
                false,
                "referencia",
                "Permite relacionar o conhecimento a um insight de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "team-learning-relacionado",
                "Team Learning relacionado",
                false,
                "referencia",
                "Permite relacionar o conhecimento a um aprendizado de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "team-outcome-relacionado",
                "Team Outcome relacionado",
                false,
                "referencia",
                "Permite relacionar o conhecimento a um resultado de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar o conhecimento a uma decisão de equipe já documentada."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar o conhecimento a um alinhamento entre profissionais já documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar o conhecimento a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar o conhecimento a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar o conhecimento a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar o conhecimento a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o conhecimento a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo registro do conhecimento."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes da construção do conhecimento documentado."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "conhecimento-documentado",
                "Conhecimento documentado",
                true,
                "texto-longo",
                "Registra conhecimento profissional documentado sem transformá-lo automaticamente em evidência clínica validada."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "base-observacional-evidencia-suporte",
                "Base observacional / evidência de suporte",
                false,
                "texto-longo",
                "Permite registrar observações ou evidências de suporte sem inferir causalidade ou força de evidência."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "interpretacao-profissional",
                "Interpretação profissional",
                false,
                "texto-longo",
                "Permite registrar interpretação humana explícita sem transformá-la em conclusão clínica automática."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "aplicabilidade",
                "Aplicabilidade",
                false,
                "texto-longo",
                "Permite documentar onde o conhecimento pode ser considerado sem gerar recomendação automática."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamKnowledgeFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais sobre o conhecimento documentado.")
        };

        return Ok(new ProfessionalReviewTeamKnowledgeFoundationResponse(
            "FundacaoTeamKnowledgeDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza conhecimento documentado da equipe usando Team Insight, Team Learning, Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.48.1, possui persistência auditada. Não transforma conhecimento em evidência clínica validada, não infere causalidade, não produz prognóstico, recomendação ou decisão terapêutica automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamKnowledgeApplicationRequest(
        string ProfissionalResponsavel,
        string AplicacaoDocumentada,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ObjetivoAplicacao = null,
        string? ContextoAplicacao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ResultadoEsperadoDocumentado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamKnowledgeApplicationRequest(
        string ProfissionalResponsavel,
        string AplicacaoDocumentada,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ObjetivoAplicacao = null,
        string? ContextoAplicacao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ResultadoEsperadoDocumentado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-knowledge-application/foundation")]
    public ActionResult<ProfessionalReviewTeamKnowledgeApplicationFoundationResponse> TeamKnowledgeApplicationFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "team-knowledge-relacionado",
                "Team Knowledge relacionado",
                false,
                "referencia",
                "Permite relacionar a aplicação a um conhecimento de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "team-insight-relacionado",
                "Team Insight relacionado",
                false,
                "referencia",
                "Permite relacionar a aplicação a um insight de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "team-learning-relacionado",
                "Team Learning relacionado",
                false,
                "referencia",
                "Permite relacionar a aplicação a um aprendizado de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "team-outcome-relacionado",
                "Team Outcome relacionado",
                false,
                "referencia",
                "Permite relacionar a aplicação a um resultado de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar a aplicação a uma decisão de equipe já documentada."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar a aplicação a um alinhamento entre profissionais já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar a aplicação a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar a aplicação a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar a aplicação a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar a aplicação a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar a aplicação a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo registro da aplicação."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes da aplicação documentada."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "aplicacao-documentada",
                "Aplicação documentada",
                true,
                "texto-longo",
                "Registra como o conhecimento foi considerado no contexto profissional sem executar automaticamente conduta ou prescrição."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "objetivo-aplicacao",
                "Objetivo da aplicação",
                false,
                "texto-longo",
                "Permite registrar o objetivo documental da aplicação sem convertê-lo em recomendação clínica automática."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "contexto-aplicacao",
                "Contexto de aplicação",
                false,
                "texto-longo",
                "Permite registrar o contexto em que a aplicação foi considerada."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "base-observacional-evidencia-suporte",
                "Base observacional / evidência de suporte",
                false,
                "texto-longo",
                "Permite registrar observações ou evidências de suporte sem inferir causalidade ou força de evidência."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "interpretacao-profissional",
                "Interpretação profissional",
                false,
                "texto-longo",
                "Permite registrar interpretação humana explícita sem transformá-la em conclusão clínica automática."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "resultado-esperado-documentado",
                "Resultado esperado documentado",
                false,
                "texto-longo",
                "Permite registrar resultado esperado como expectativa documental, sem prognóstico automático."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamKnowledgeApplicationFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais sobre a aplicação documentada.")
        };

        return Ok(new ProfessionalReviewTeamKnowledgeApplicationFoundationResponse(
            "FundacaoTeamKnowledgeApplicationDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza aplicação documentada do conhecimento da equipe usando Team Knowledge, Team Insight, Team Learning, Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.49.1, possui persistência auditada. Não transforma aplicação registrada em evidência clínica validada, não infere causalidade, não produz prognóstico, recomendação ou decisão terapêutica automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamKnowledgeEffectRequest(
        string ProfissionalResponsavel,
        string EfeitoObservadoDocumentado,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoObservacao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ResultadoObservadoDocumentado = null,
        string? ImpactoPercebidoDocumentado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamKnowledgeEffectRequest(
        string ProfissionalResponsavel,
        string EfeitoObservadoDocumentado,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoObservacao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ResultadoObservadoDocumentado = null,
        string? ImpactoPercebidoDocumentado = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-knowledge-effect/foundation")]
    public ActionResult<ProfessionalReviewTeamKnowledgeEffectFoundationResponse> TeamKnowledgeEffectFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "team-knowledge-application-relacionada",
                "Team Knowledge Application relacionada",
                false,
                "referencia",
                "Permite relacionar o efeito observado a uma aplicação de conhecimento já documentada."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "team-knowledge-relacionado",
                "Team Knowledge relacionado",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um conhecimento de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "team-insight-relacionado",
                "Team Insight relacionado",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um insight de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "team-learning-relacionado",
                "Team Learning relacionado",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um aprendizado de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "team-outcome-relacionado",
                "Team Outcome relacionado",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um resultado de equipe já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar o efeito observado a uma decisão de equipe já documentada."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um alinhamento profissional já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um contexto profissional compartilhado já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar o efeito observado a uma colaboração profissional já documentada."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar o efeito observado a uma coordenação profissional já documentada."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um escalonamento profissional já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar o efeito observado a um registro de continuidade profissional já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "profissional-responsavel",
                "Profissional responsável",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pelo registro do efeito observado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra profissionais participantes da observação do efeito."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "efeito-observado-documentado",
                "Efeito observado documentado",
                true,
                "texto-longo",
                "Registra o efeito percebido ou observado sem atribuir causalidade automaticamente."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "contexto-observacao",
                "Contexto da observação",
                false,
                "texto-longo",
                "Permite registrar o contexto em que o efeito foi observado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "base-observacional-evidencia-suporte",
                "Base observacional / evidência de suporte",
                false,
                "texto-longo",
                "Permite registrar observações ou evidências de suporte sem inferir causalidade ou força de evidência."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "interpretacao-profissional",
                "Interpretação profissional",
                false,
                "texto-longo",
                "Permite registrar interpretação humana explícita sem transformá-la em conclusão clínica automática."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "resultado-observado-documentado",
                "Resultado observado documentado",
                false,
                "texto-longo",
                "Permite registrar resultado observado sem convertê-lo em desfecho causal comprovado."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "impacto-percebido-documentado",
                "Impacto percebido documentado",
                false,
                "texto-longo",
                "Permite registrar impacto percebido sem inferir benefício, dano ou efeito causal automaticamente."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamKnowledgeEffectFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais sobre o efeito observado.")
        };

        return Ok(new ProfessionalReviewTeamKnowledgeEffectFoundationResponse(
            "FundacaoTeamKnowledgeEffectDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza efeitos observados após aplicações do conhecimento da equipe usando Team Knowledge Application, Team Knowledge, Team Insight, Team Learning, Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.50.1, possui persistência auditada. Não transforma efeito registrado em causalidade comprovada ou evidência clínica validada, não produz prognóstico, recomendação ou decisão terapêutica automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamKnowledgeEffectReviewRequest(
        string ProfissionalRevisor,
        string RevisaoDocumentada,
        Guid? TeamKnowledgeEffectRelacionadoId = null,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoRevisao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ConclusaoDocumental = null,
        string? NecessidadeAcompanhamentoDocumentada = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamKnowledgeEffectReviewRequest(
        string ProfissionalRevisor,
        string RevisaoDocumentada,
        Guid? TeamKnowledgeEffectRelacionadoId = null,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoRevisao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ConclusaoDocumental = null,
        string? NecessidadeAcompanhamentoDocumentada = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-knowledge-effect-review/foundation")]
    public ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewFoundationResponse> TeamKnowledgeEffectReviewFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-knowledge-effect-relacionado",
                "Team Knowledge Effect relacionado",
                false,
                "referencia",
                "Permite relacionar a revisão a um efeito observado já documentado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-knowledge-application-relacionada",
                "Team Knowledge Application relacionada",
                false,
                "referencia",
                "Permite relacionar a revisão à aplicação do conhecimento previamente documentada."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-knowledge-relacionado",
                "Team Knowledge relacionado",
                false,
                "referencia",
                "Permite relacionar a revisão ao conhecimento de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-insight-relacionado",
                "Team Insight relacionado",
                false,
                "referencia",
                "Permite relacionar a revisão a um insight de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-learning-relacionado",
                "Team Learning relacionado",
                false,
                "referencia",
                "Permite relacionar a revisão a um aprendizado de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-outcome-relacionado",
                "Team Outcome relacionado",
                false,
                "referencia",
                "Permite relacionar a revisão a um resultado de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar a revisão a uma decisão de equipe documentada."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar a revisão a um alinhamento profissional documentado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar a revisão a um contexto profissional compartilhado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar a revisão a uma colaboração profissional documentada."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar a revisão a uma coordenação profissional documentada."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar a revisão a um escalonamento profissional documentado."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar a revisão a um registro de continuidade profissional."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "profissional-revisor",
                "Profissional revisor",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pela revisão."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes da revisão."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "revisao-documentada",
                "Revisão documentada",
                true,
                "texto-longo",
                "Registra a revisão profissional sem produzir conclusão clínica automática."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "contexto-revisao",
                "Contexto da revisão",
                false,
                "texto-longo",
                "Permite documentar o contexto em que a revisão foi realizada."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "base-observacional-evidencia-suporte",
                "Base observacional / evidência de suporte",
                false,
                "texto-longo",
                "Permite registrar observações e evidências de suporte sem inferir automaticamente causalidade ou força de evidência."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "interpretacao-profissional",
                "Interpretação profissional",
                false,
                "texto-longo",
                "Permite registrar interpretação humana explícita sem transformá-la em decisão clínica automática."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "conclusao-documental",
                "Conclusão documental",
                false,
                "texto-longo",
                "Permite registrar uma conclusão documental sem convertê-la em diagnóstico, prognóstico ou decisão terapêutica automática."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "necessidade-acompanhamento-documentada",
                "Necessidade de acompanhamento documentada",
                false,
                "texto-longo",
                "Permite registrar necessidade percebida de acompanhamento sem criar urgência, prioridade ou conduta automática."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamKnowledgeEffectReviewFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais da revisão.")
        };

        return Ok(new ProfessionalReviewTeamKnowledgeEffectReviewFoundationResponse(
            "FundacaoTeamKnowledgeEffectReviewDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza revisões profissionais de efeitos observados do conhecimento da equipe usando Team Knowledge Effect, Team Knowledge Application, Team Knowledge, Team Insight, Team Learning, Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.51.1, possui persistência auditada. Não transforma revisão em validação causal ou evidência clínica validada, não produz prognóstico, recomendação ou decisão terapêutica automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamKnowledgeEffectDecisionRequest(
        string ProfissionalResponsavel,
        string DecisaoDocumentada,
        Guid? TeamKnowledgeEffectReviewRelacionadaId = null,
        Guid? TeamKnowledgeEffectRelacionadoId = null,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoDecisao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? JustificativaProfissional = null,
        string? ResultadoEsperadoDocumentado = null,
        string? NecessidadeAcompanhamentoDocumentada = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamKnowledgeEffectDecisionRequest(
        string ProfissionalResponsavel,
        string DecisaoDocumentada,
        Guid? TeamKnowledgeEffectReviewRelacionadaId = null,
        Guid? TeamKnowledgeEffectRelacionadoId = null,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoDecisao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? JustificativaProfissional = null,
        string? ResultadoEsperadoDocumentado = null,
        string? NecessidadeAcompanhamentoDocumentada = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-knowledge-effect-decision/foundation")]
    public ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionFoundationResponse> TeamKnowledgeEffectDecisionFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-knowledge-effect-review-relacionada",
                "Team Knowledge Effect Review relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a uma revisão profissional previamente documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-knowledge-effect-relacionado",
                "Team Knowledge Effect relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um efeito observado documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-knowledge-application-relacionada",
                "Team Knowledge Application relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão à aplicação do conhecimento previamente documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-knowledge-relacionado",
                "Team Knowledge relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão ao conhecimento de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-insight-relacionado",
                "Team Insight relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um insight de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-learning-relacionado",
                "Team Learning relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um aprendizado de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-outcome-relacionado",
                "Team Outcome relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um resultado de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-decision-relacionada",
                "Team Decision relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a uma decisão de equipe documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "team-alignment-relacionado",
                "Team Alignment relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um alinhamento profissional documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "shared-context-relacionado",
                "Shared Context relacionado",
                false,
                "referencia",
                "Permite relacionar a decisão a um contexto profissional compartilhado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "collaboration-relacionada",
                "Collaboration relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a uma colaboração profissional documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "coordination-relacionada",
                "Coordination relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a uma coordenação profissional documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "escalation-relacionada",
                "Escalation relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a um escalonamento profissional documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "continuity-relacionada",
                "Continuity relacionada",
                false,
                "referencia",
                "Permite relacionar a decisão a um registro de continuidade profissional."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "profissional-responsavel",
                "Profissional responsável pela decisão",
                true,
                "texto",
                "Identifica documentalmente o profissional responsável pela decisão."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "participantes",
                "Participantes",
                false,
                "texto",
                "Registra os profissionais participantes do processo decisório."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "decisao-documentada",
                "Decisão documentada",
                true,
                "texto-longo",
                "Registra a decisão profissional sem executá-la automaticamente."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "contexto-decisao",
                "Contexto da decisão",
                false,
                "texto-longo",
                "Permite documentar o contexto em que a decisão foi registrada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "base-observacional-evidencia-suporte",
                "Base observacional / evidência de suporte",
                false,
                "texto-longo",
                "Permite registrar observações e evidências de suporte sem inferir automaticamente causalidade ou força de evidência."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "justificativa-profissional",
                "Justificativa profissional",
                false,
                "texto-longo",
                "Permite registrar a justificativa humana explícita da decisão documental."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "resultado-esperado-documentado",
                "Resultado esperado documentado",
                false,
                "texto-longo",
                "Permite registrar um resultado esperado sem transformá-lo em prognóstico ou promessa de desfecho."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "necessidade-acompanhamento-documentada",
                "Necessidade de acompanhamento documentada",
                false,
                "texto-longo",
                "Permite registrar necessidade percebida de acompanhamento sem criar urgência, prioridade ou conduta automática."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "horizonte",
                "Horizonte",
                false,
                "texto",
                "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionFieldResponse(
                "observacao-profissional",
                "Observação profissional",
                false,
                "texto-longo",
                "Permite registrar observações adicionais da decisão.")
        };

        return Ok(new ProfessionalReviewTeamKnowledgeEffectDecisionFoundationResponse(
            "FundacaoTeamKnowledgeEffectDecisionDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza decisões profissionais documentadas relacionadas às revisões dos efeitos observados do conhecimento da equipe usando Team Knowledge Effect Review, Team Knowledge Effect, Team Knowledge Application, Team Knowledge, Team Insight, Team Learning, Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.52.1, possui persistência auditada. Não transforma decisão documentada em validação causal ou evidência clínica validada, não produz prognóstico, recomendação ou decisão terapêutica automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    public sealed record CriarProfessionalReviewTeamKnowledgeEffectDecisionReviewRequest(
        string ProfissionalRevisor,
        string RevisaoDocumentada,
        Guid? TeamKnowledgeEffectDecisionRelacionadaId = null,
        Guid? TeamKnowledgeEffectReviewRelacionadaId = null,
        Guid? TeamKnowledgeEffectRelacionadoId = null,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoRevisao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ConclusaoDocumental = null,
        string? NecessidadeAcompanhamentoDocumentada = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    public sealed record AtualizarProfessionalReviewTeamKnowledgeEffectDecisionReviewRequest(
        string ProfissionalRevisor,
        string RevisaoDocumentada,
        Guid? TeamKnowledgeEffectDecisionRelacionadaId = null,
        Guid? TeamKnowledgeEffectReviewRelacionadaId = null,
        Guid? TeamKnowledgeEffectRelacionadoId = null,
        Guid? TeamKnowledgeApplicationRelacionadaId = null,
        Guid? TeamKnowledgeRelacionadoId = null,
        Guid? TeamInsightRelacionadoId = null,
        Guid? TeamLearningRelacionadoId = null,
        Guid? TeamOutcomeRelacionadoId = null,
        Guid? TeamDecisionRelacionadaId = null,
        Guid? TeamAlignmentRelacionadoId = null,
        Guid? SharedContextRelacionadoId = null,
        Guid? CollaborationRelacionadaId = null,
        Guid? CoordinationRelacionadaId = null,
        Guid? EscalationRelacionadaId = null,
        Guid? ContinuityRelacionadaId = null,
        string? Participantes = null,
        string? ContextoRevisao = null,
        string? BaseObservacionalEvidenciaSuporte = null,
        string? InterpretacaoProfissional = null,
        string? ConclusaoDocumental = null,
        string? NecessidadeAcompanhamentoDocumentada = null,
        string? Horizonte = null,
        string? ObservacaoProfissional = null);

    [HttpGet("team-knowledge-effect-decision-review/foundation")]
    public ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionReviewFoundationResponse> TeamKnowledgeEffectDecisionReviewFoundation()
    {
        var campos = new[]
        {
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-knowledge-effect-decision-relacionada", "Team Knowledge Effect Decision relacionada", false, "referencia", "Permite relacionar a revisão a uma decisão profissional previamente documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-knowledge-effect-review-relacionada", "Team Knowledge Effect Review relacionada", false, "referencia", "Permite relacionar a revisão à revisão profissional do efeito observado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-knowledge-effect-relacionado", "Team Knowledge Effect relacionado", false, "referencia", "Permite relacionar a revisão ao efeito observado documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-knowledge-application-relacionada", "Team Knowledge Application relacionada", false, "referencia", "Permite relacionar a revisão à aplicação do conhecimento documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-knowledge-relacionado", "Team Knowledge relacionado", false, "referencia", "Permite relacionar a revisão ao conhecimento de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-insight-relacionado", "Team Insight relacionado", false, "referencia", "Permite relacionar a revisão a um insight de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-learning-relacionado", "Team Learning relacionado", false, "referencia", "Permite relacionar a revisão a um aprendizado de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-outcome-relacionado", "Team Outcome relacionado", false, "referencia", "Permite relacionar a revisão a um resultado de equipe documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-decision-relacionada", "Team Decision relacionada", false, "referencia", "Permite relacionar a revisão a uma decisão de equipe documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("team-alignment-relacionado", "Team Alignment relacionado", false, "referencia", "Permite relacionar a revisão a um alinhamento profissional documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("shared-context-relacionado", "Shared Context relacionado", false, "referencia", "Permite relacionar a revisão a um contexto profissional compartilhado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("collaboration-relacionada", "Collaboration relacionada", false, "referencia", "Permite relacionar a revisão a uma colaboração profissional documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("coordination-relacionada", "Coordination relacionada", false, "referencia", "Permite relacionar a revisão a uma coordenação profissional documentada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("escalation-relacionada", "Escalation relacionada", false, "referencia", "Permite relacionar a revisão a um escalonamento profissional documentado."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("continuity-relacionada", "Continuity relacionada", false, "referencia", "Permite relacionar a revisão a um registro de continuidade profissional."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("profissional-revisor", "Profissional revisor", true, "texto", "Identifica documentalmente o profissional responsável pela revisão."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("participantes", "Participantes", false, "texto", "Registra os profissionais participantes da revisão."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("revisao-documentada", "Revisão documentada", true, "texto-longo", "Registra a revisão profissional da decisão sem executar qualquer conduta automaticamente."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("contexto-revisao", "Contexto da revisão", false, "texto-longo", "Permite documentar o contexto em que a revisão foi realizada."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("base-observacional-evidencia-suporte", "Base observacional / evidência de suporte", false, "texto-longo", "Permite registrar observações e evidências de suporte sem inferir causalidade ou força de evidência automaticamente."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("interpretacao-profissional", "Interpretação profissional", false, "texto-longo", "Permite registrar interpretação humana explícita sem transformá-la em inferência automática."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("conclusao-documental", "Conclusão documental", false, "texto-longo", "Permite registrar conclusão documental sem produzir decisão terapêutica automática."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("necessidade-acompanhamento-documentada", "Necessidade de acompanhamento documentada", false, "texto-longo", "Permite registrar necessidade percebida de acompanhamento sem criar urgência, prioridade ou conduta automática."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("horizonte", "Horizonte", false, "texto", "Registra referência temporal operacional sem definir urgência clínica."),
            new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFieldResponse("observacao-profissional", "Observação profissional", false, "texto-longo", "Permite registrar observações adicionais da revisão.")
        };

        return Ok(new ProfessionalReviewTeamKnowledgeEffectDecisionReviewFoundationResponse(
            "FundacaoTeamKnowledgeEffectDecisionReviewDisponivel",
            true,
            "EquipeProfissional",
            campos,
            "A fundação organiza revisões profissionais das decisões documentadas sobre os efeitos observados do conhecimento da equipe usando Team Knowledge Effect Decision, Team Knowledge Effect Review, Team Knowledge Effect, Team Knowledge Application, Team Knowledge, Team Insight, Team Learning, Team Outcome, Team Decision, Team Alignment, Shared Context, Collaboration, Coordination, Escalation e Continuity como referências opcionais e, a partir da v0.53.1, possui persistência auditada. Não transforma revisão em validação causal ou evidência clínica validada, não produz prognóstico, recomendação ou decisão terapêutica automática, não executa conduta ou prescrição, não transfere automaticamente responsabilidade clínica e não substitui avaliação profissional."));
    }

    [HttpGet("team-knowledge-effect-decision-review")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectDecisionReviewPersistedResponse>>> ListarTeamKnowledgeEffectDecisionReviews(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecisionReview));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamKnowledgeEffectDecisionReview).ToArray());
    }

    [HttpPost("team-knowledge-effect-decision-review")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionReviewPersistedResponse>> CriarTeamKnowledgeEffectDecisionReview(
        Guid pacienteId,
        CriarProfessionalReviewTeamKnowledgeEffectDecisionReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalRevisor = NormalizarObrigatorio(request.ProfissionalRevisor, 160);
        if (profissionalRevisor is null)
            return BadRequest(new { message = "Informe o profissional revisor." });

        var revisaoDocumentada = NormalizarObrigatorio(request.RevisaoDocumentada, 3000);
        if (revisaoDocumentada is null)
            return BadRequest(new { message = "Informe a revisão documentada." });

        if (request.TeamKnowledgeEffectDecisionRelacionadaId.HasValue &&
            !await TeamKnowledgeEffectDecisionPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect Decision relacionada inválida para este paciente." });

        if (request.TeamKnowledgeEffectReviewRelacionadaId.HasValue &&
            !await TeamKnowledgeEffectReviewPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectReviewRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect Review relacionada inválida para este paciente." });

        if (request.TeamKnowledgeEffectRelacionadoId.HasValue &&
            !await TeamKnowledgeEffectPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect relacionado inválido para este paciente." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var autor = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(cancellationToken) ?? "Profissional";

        var nota = new NotaInternaProfissional
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            AutorUsuarioId = currentUser.UserId,
            AutorNome = autor,
            Categoria = PrefixoTeamKnowledgeEffectDecisionReview + "item",
            Conteudo = MontarPayloadTeamKnowledgeEffectDecisionReview(
                profissionalRevisor,
                revisaoDocumentada,
                request.TeamKnowledgeEffectDecisionRelacionadaId,
                request.TeamKnowledgeEffectReviewRelacionadaId,
                request.TeamKnowledgeEffectRelacionadoId,
                request.TeamKnowledgeApplicationRelacionadaId,
                request.TeamKnowledgeRelacionadoId,
                request.TeamInsightRelacionadoId,
                request.TeamLearningRelacionadoId,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.ContextoRevisao,
                request.BaseObservacionalEvidenciaSuporte,
                request.InterpretacaoProfissional,
                request.ConclusaoDocumental,
                request.NecessidadeAcompanhamentoDocumentada,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_REVIEW_CREATED", nota, null, Snapshot(nota));
        await db.SaveChangesAsync(cancellationToken);

        return Ok(MapearTeamKnowledgeEffectDecisionReview(nota));
    }

    [HttpPut("team-knowledge-effect-decision-review/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionReviewPersistedResponse>> AtualizarTeamKnowledgeEffectDecisionReview(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamKnowledgeEffectDecisionReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecisionReview),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalRevisor = NormalizarObrigatorio(request.ProfissionalRevisor, 160);
        if (profissionalRevisor is null)
            return BadRequest(new { message = "Informe o profissional revisor." });

        var revisaoDocumentada = NormalizarObrigatorio(request.RevisaoDocumentada, 3000);
        if (revisaoDocumentada is null)
            return BadRequest(new { message = "Informe a revisão documentada." });

        if (request.TeamKnowledgeEffectDecisionRelacionadaId.HasValue &&
            !await TeamKnowledgeEffectDecisionPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect Decision relacionada inválida para este paciente." });

        if (request.TeamKnowledgeEffectReviewRelacionadaId.HasValue &&
            !await TeamKnowledgeEffectReviewPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectReviewRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect Review relacionada inválida para este paciente." });

        if (request.TeamKnowledgeEffectRelacionadoId.HasValue &&
            !await TeamKnowledgeEffectPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect relacionado inválido para este paciente." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamKnowledgeEffectDecisionReview(nota);
        var payloadAtualizado = new TeamKnowledgeEffectDecisionReviewPayload(
            profissionalRevisor,
            revisaoDocumentada,
            request.TeamKnowledgeEffectDecisionRelacionadaId,
            request.TeamKnowledgeEffectReviewRelacionadaId,
            request.TeamKnowledgeEffectRelacionadoId,
            request.TeamKnowledgeApplicationRelacionadaId,
            request.TeamKnowledgeRelacionadoId,
            request.TeamInsightRelacionadoId,
            request.TeamLearningRelacionadoId,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.ContextoRevisao, 3000),
            NormalizarOpcional(request.BaseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(request.InterpretacaoProfissional, 3000),
            NormalizarOpcional(request.ConclusaoDocumental, 3000),
            NormalizarOpcional(request.NecessidadeAcompanhamentoDocumentada, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_REVIEW_UPDATED", nota, antes, Snapshot(nota));
        await db.SaveChangesAsync(cancellationToken);

        return Ok(MapearTeamKnowledgeEffectDecisionReview(nota));
    }

    [HttpPatch("team-knowledge-effect-decision-review/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionReviewPersistedResponse>> AtualizarStatusTeamKnowledgeEffectDecisionReview(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamKnowledgeEffectDecisionReviewStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim();

        if (!StatusTeamKnowledgeEffectDecisionReviewValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecisionReview) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamKnowledgeEffectDecisionReview(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_REVIEW_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamKnowledgeEffectDecisionReview(nota));
    }

    [HttpDelete("team-knowledge-effect-decision-review/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamKnowledgeEffectDecisionReview(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecisionReview),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;
            Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_REVIEW_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-knowledge-effect-decision/closure")]
    public ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionClosureResponse> FechamentoTeamKnowledgeEffectDecisions()
    {
        var componentes = new[]
        {
            "TeamKnowledgeEffectDecisionFoundation",
            "TeamKnowledgeEffectDecisionPersistence",
            "TeamKnowledgeEffectDecisionStatus",
            "TeamKnowledgeEffectDecisionHistory",
            "TeamKnowledgeEffectDecisionFilters",
            "TeamKnowledgeEffectDecisionSummary"
        };

        return Ok(new ProfessionalReviewTeamKnowledgeEffectDecisionClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamKnowledgeEffectDecisionCompleta",
            "O fechamento descreve apenas disponibilidade estrutural das decisões profissionais documentadas sobre os efeitos observados do conhecimento da equipe. Não transforma decisão em validação causal ou evidência clínica validada, não produz prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect-decision/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionSummaryResponse>> ResumoTeamKnowledgeEffectDecisions(
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
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamKnowledgeEffectDecision).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var registradas = itens.Count(x => !x.Arquivada && x.Status == "Registrado");
        var emRevisao = itens.Count(x => !x.Arquivada && x.Status == "EmRevisao");
        var consolidadas = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartadas = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamKnowledgeEffectDecisionProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectDecisionSummaryResponse(
            total,
            ativas,
            registradas,
            emRevisao,
            consolidadas,
            descartadas,
            arquivadas,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais das decisões profissionais sobre os efeitos observados do conhecimento da equipe. Não transforma agregações em validação causal ou evidência clínica validada, não produz prognóstico, recomendação ou decisão terapêutica, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect-decision/search")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionFiltersResponse>> FiltrarTeamKnowledgeEffectDecisions(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamKnowledgeEffectDecisionValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamKnowledgeEffectDecision)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.DecisaoDocumentada.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoDecisao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.BaseObservacionalEvidenciaSuporte?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.JustificativaProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ResultadoEsperadoDocumentado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.NecessidadeAcompanhamentoDocumentada?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectDecisionFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar decisões profissionais documentadas sobre os efeitos observados do conhecimento da equipe. Não transformam decisão registrada em validação causal ou evidência clínica validada, não produzem prognóstico, recomendação, decisão terapêutica, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect-decision")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse>>> ListarTeamKnowledgeEffectDecisions(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamKnowledgeEffectDecision).ToArray());
    }

    [HttpPost("team-knowledge-effect-decision")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse>> CriarTeamKnowledgeEffectDecision(
        Guid pacienteId,
        CriarProfessionalReviewTeamKnowledgeEffectDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável pela decisão." });

        var decisaoDocumentada = NormalizarObrigatorio(request.DecisaoDocumentada, 3000);
        if (decisaoDocumentada is null)
            return BadRequest(new { message = "Informe a decisão documentada." });

        if (request.TeamKnowledgeEffectReviewRelacionadaId.HasValue &&
            !await TeamKnowledgeEffectReviewPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectReviewRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect Review relacionada inválida para este paciente." });

        if (request.TeamKnowledgeEffectRelacionadoId.HasValue &&
            !await TeamKnowledgeEffectPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect relacionado inválido para este paciente." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var autor = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(cancellationToken) ?? "Profissional";

        var nota = new NotaInternaProfissional
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            AutorUsuarioId = currentUser.UserId,
            AutorNome = autor,
            Categoria = PrefixoTeamKnowledgeEffectDecision + "item",
            Conteudo = MontarPayloadTeamKnowledgeEffectDecision(
                profissionalResponsavel,
                decisaoDocumentada,
                request.TeamKnowledgeEffectReviewRelacionadaId,
                request.TeamKnowledgeEffectRelacionadoId,
                request.TeamKnowledgeApplicationRelacionadaId,
                request.TeamKnowledgeRelacionadoId,
                request.TeamInsightRelacionadoId,
                request.TeamLearningRelacionadoId,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.ContextoDecisao,
                request.BaseObservacionalEvidenciaSuporte,
                request.JustificativaProfissional,
                request.ResultadoEsperadoDocumentado,
                request.NecessidadeAcompanhamentoDocumentada,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_CREATED", nota, null, Snapshot(nota));
        await db.SaveChangesAsync(cancellationToken);

        return Ok(MapearTeamKnowledgeEffectDecision(nota));
    }

    [HttpPut("team-knowledge-effect-decision/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse>> AtualizarTeamKnowledgeEffectDecision(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamKnowledgeEffectDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável pela decisão." });

        var decisaoDocumentada = NormalizarObrigatorio(request.DecisaoDocumentada, 3000);
        if (decisaoDocumentada is null)
            return BadRequest(new { message = "Informe a decisão documentada." });

        if (request.TeamKnowledgeEffectReviewRelacionadaId.HasValue &&
            !await TeamKnowledgeEffectReviewPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectReviewRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect Review relacionada inválida para este paciente." });

        if (request.TeamKnowledgeEffectRelacionadoId.HasValue &&
            !await TeamKnowledgeEffectPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect relacionado inválido para este paciente." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamKnowledgeEffectDecision(nota);
        var payloadAtualizado = new TeamKnowledgeEffectDecisionPayload(
            profissionalResponsavel,
            decisaoDocumentada,
            request.TeamKnowledgeEffectReviewRelacionadaId,
            request.TeamKnowledgeEffectRelacionadoId,
            request.TeamKnowledgeApplicationRelacionadaId,
            request.TeamKnowledgeRelacionadoId,
            request.TeamInsightRelacionadoId,
            request.TeamLearningRelacionadoId,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.ContextoDecisao, 3000),
            NormalizarOpcional(request.BaseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(request.JustificativaProfissional, 3000),
            NormalizarOpcional(request.ResultadoEsperadoDocumentado, 3000),
            NormalizarOpcional(request.NecessidadeAcompanhamentoDocumentada, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_UPDATED", nota, antes, Snapshot(nota));
        await db.SaveChangesAsync(cancellationToken);

        return Ok(MapearTeamKnowledgeEffectDecision(nota));
    }

    [HttpGet("team-knowledge-effect-decision/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionHistoryResponse>> HistoricoTeamKnowledgeEffectDecision(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var decisionExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision),
                cancellationToken);

        if (!decisionExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_"));

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

            return new ProfessionalReviewTeamKnowledgeEffectDecisionHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamKnowledgeEffectDecision(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectDecisionHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais das decisões profissionais sobre os efeitos observados do conhecimento da equipe. Não transforma decisão registrada em validação causal ou evidência clínica validada, não interpreta evolução clínica, prognóstico, urgência, prioridade, risco, resultado clínico ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-knowledge-effect-decision/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse>> AtualizarStatusTeamKnowledgeEffectDecision(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamKnowledgeEffectDecisionStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim();

        if (!StatusTeamKnowledgeEffectDecisionValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamKnowledgeEffectDecision(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamKnowledgeEffectDecision(nota));
    }

    [HttpDelete("team-knowledge-effect-decision/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamKnowledgeEffectDecision(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;
            Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-knowledge-effect-review/closure")]
    public ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewClosureResponse> FechamentoTeamKnowledgeEffectReviews()
    {
        var componentes = new[]
        {
            "TeamKnowledgeEffectReviewFoundation",
            "TeamKnowledgeEffectReviewPersistence",
            "TeamKnowledgeEffectReviewStatus",
            "TeamKnowledgeEffectReviewHistory",
            "TeamKnowledgeEffectReviewFilters",
            "TeamKnowledgeEffectReviewSummary"
        };

        return Ok(new ProfessionalReviewTeamKnowledgeEffectReviewClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamKnowledgeEffectReviewCompleta",
            "O fechamento descreve apenas disponibilidade estrutural das revisões profissionais documentadas dos efeitos observados do conhecimento da equipe. Não transforma revisão em validação causal ou evidência clínica validada, não produz prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect-review/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewSummaryResponse>> ResumoTeamKnowledgeEffectReviews(
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
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamKnowledgeEffectReview).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var registradas = itens.Count(x => !x.Arquivada && x.Status == "Registrado");
        var emRevisao = itens.Count(x => !x.Arquivada && x.Status == "EmRevisao");
        var consolidadas = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartadas = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalRevisor = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalRevisor))
            .GroupBy(x => x.ProfissionalRevisor.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamKnowledgeEffectReviewProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectReviewSummaryResponse(
            total,
            ativas,
            registradas,
            emRevisao,
            consolidadas,
            descartadas,
            arquivadas,
            porProfissionalRevisor,
            "O resumo apresenta somente agregações documentais das revisões profissionais dos efeitos observados do conhecimento da equipe. Não transforma agregações em validação causal ou evidência clínica validada, não produz prognóstico, recomendação ou decisão terapêutica, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect-review/search")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewFiltersResponse>> FiltrarTeamKnowledgeEffectReviews(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalRevisor = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamKnowledgeEffectReviewValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var revisorNormalizado = NormalizarOpcional(profissionalRevisor, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamKnowledgeEffectReview)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (revisorNormalizado is null ||
                    x.ProfissionalRevisor.Contains(revisorNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalRevisor.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.RevisaoDocumentada.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoRevisao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.BaseObservacionalEvidenciaSuporte?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.InterpretacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ConclusaoDocumental?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.NecessidadeAcompanhamentoDocumentada?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectReviewFiltersResponse(
            statusNormalizado,
            revisorNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar revisões profissionais documentadas dos efeitos observados do conhecimento da equipe. Não transformam revisão em validação causal ou evidência clínica validada, não produzem prognóstico, recomendação, decisão terapêutica, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect-review")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse>>> ListarTeamKnowledgeEffectReviews(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamKnowledgeEffectReview).ToArray());
    }

    [HttpPost("team-knowledge-effect-review")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse>> CriarTeamKnowledgeEffectReview(
        Guid pacienteId,
        CriarProfessionalReviewTeamKnowledgeEffectReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalRevisor = NormalizarObrigatorio(request.ProfissionalRevisor, 160);
        if (profissionalRevisor is null)
            return BadRequest(new { message = "Informe o profissional revisor." });

        var revisaoDocumentada = NormalizarObrigatorio(request.RevisaoDocumentada, 3000);
        if (revisaoDocumentada is null)
            return BadRequest(new { message = "Informe a revisão documentada." });

        if (request.TeamKnowledgeEffectRelacionadoId.HasValue &&
            !await TeamKnowledgeEffectPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect relacionado inválido para este paciente." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamKnowledgeEffectReview + "item",
            Conteudo = MontarPayloadTeamKnowledgeEffectReview(
                profissionalRevisor,
                revisaoDocumentada,
                request.TeamKnowledgeEffectRelacionadoId,
                request.TeamKnowledgeApplicationRelacionadaId,
                request.TeamKnowledgeRelacionadoId,
                request.TeamInsightRelacionadoId,
                request.TeamLearningRelacionadoId,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.ContextoRevisao,
                request.BaseObservacionalEvidenciaSuporte,
                request.InterpretacaoProfissional,
                request.ConclusaoDocumental,
                request.NecessidadeAcompanhamentoDocumentada,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledgeEffectReview(nota));
    }

    [HttpPut("team-knowledge-effect-review/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse>> AtualizarTeamKnowledgeEffectReview(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamKnowledgeEffectReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalRevisor = NormalizarObrigatorio(request.ProfissionalRevisor, 160);
        if (profissionalRevisor is null)
            return BadRequest(new { message = "Informe o profissional revisor." });

        var revisaoDocumentada = NormalizarObrigatorio(request.RevisaoDocumentada, 3000);
        if (revisaoDocumentada is null)
            return BadRequest(new { message = "Informe a revisão documentada." });

        if (request.TeamKnowledgeEffectRelacionadoId.HasValue &&
            !await TeamKnowledgeEffectPertencePacienteAsync(pacienteId, request.TeamKnowledgeEffectRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Effect relacionado inválido para este paciente." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamKnowledgeEffectReview(nota);
        var payloadAtualizado = new TeamKnowledgeEffectReviewPayload(
            profissionalRevisor,
            revisaoDocumentada,
            request.TeamKnowledgeEffectRelacionadoId,
            request.TeamKnowledgeApplicationRelacionadaId,
            request.TeamKnowledgeRelacionadoId,
            request.TeamInsightRelacionadoId,
            request.TeamLearningRelacionadoId,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.ContextoRevisao, 3000),
            NormalizarOpcional(request.BaseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(request.InterpretacaoProfissional, 3000),
            NormalizarOpcional(request.ConclusaoDocumental, 3000),
            NormalizarOpcional(request.NecessidadeAcompanhamentoDocumentada, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledgeEffectReview(nota));
    }

    [HttpGet("team-knowledge-effect-review/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewHistoryResponse>> HistoricoTeamKnowledgeEffectReview(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var reviewExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview),
                cancellationToken);

        if (!reviewExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_"));

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

            return new ProfessionalReviewTeamKnowledgeEffectReviewHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamKnowledgeEffectReview(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectReviewHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais das revisões profissionais dos efeitos observados do conhecimento da equipe. Não transforma revisão em validação causal ou evidência clínica validada, não interpreta evolução clínica, prognóstico, urgência, prioridade, risco, resultado clínico ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-knowledge-effect-review/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse>> AtualizarStatusTeamKnowledgeEffectReview(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamKnowledgeEffectReviewStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamKnowledgeEffectReviewValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamKnowledgeEffectReview(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamKnowledgeEffectReview(nota));
    }

    [HttpDelete("team-knowledge-effect-review/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamKnowledgeEffectReview(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-knowledge-effect/closure")]
    public ActionResult<ProfessionalReviewTeamKnowledgeEffectClosureResponse> FechamentoTeamKnowledgeEffects()
    {
        var componentes = new[]
        {
            "TeamKnowledgeEffectFoundation",
            "TeamKnowledgeEffectPersistence",
            "TeamKnowledgeEffectStatus",
            "TeamKnowledgeEffectHistory",
            "TeamKnowledgeEffectFilters",
            "TeamKnowledgeEffectSummary"
        };

        return Ok(new ProfessionalReviewTeamKnowledgeEffectClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamKnowledgeEffectCompleta",
            "O fechamento descreve apenas disponibilidade estrutural dos efeitos observados documentados do conhecimento da equipe. Não transforma efeito observado em causalidade comprovada ou evidência clínica validada, não produz prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectSummaryResponse>> ResumoTeamKnowledgeEffects(
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
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamKnowledgeEffect).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var registrados = itens.Count(x => !x.Arquivada && x.Status == "Registrado");
        var emRevisao = itens.Count(x => !x.Arquivada && x.Status == "EmRevisao");
        var consolidados = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartados = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamKnowledgeEffectProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectSummaryResponse(
            total,
            ativos,
            registrados,
            emRevisao,
            consolidados,
            descartados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais dos efeitos observados registrados. Não transforma agregações em causalidade comprovada ou evidência clínica validada, não produz prognóstico, recomendação ou decisão terapêutica, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect/search")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectFiltersResponse>> FiltrarTeamKnowledgeEffects(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamKnowledgeEffectValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamKnowledgeEffect)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.EfeitoObservadoDocumentado.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoObservacao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.BaseObservacionalEvidenciaSuporte?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.InterpretacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ResultadoObservadoDocumentado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ImpactoPercebidoDocumentado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar efeitos observados documentados do conhecimento da equipe. Não transformam efeito observado em causalidade comprovada ou evidência clínica validada, não produzem prognóstico, recomendação, decisão terapêutica, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-effect")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamKnowledgeEffectPersistedResponse>>> ListarTeamKnowledgeEffects(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamKnowledgeEffect).ToArray());
    }

    [HttpPost("team-knowledge-effect")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectPersistedResponse>> CriarTeamKnowledgeEffect(
        Guid pacienteId,
        CriarProfessionalReviewTeamKnowledgeEffectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var efeitoObservadoDocumentado = NormalizarObrigatorio(request.EfeitoObservadoDocumentado, 3000);
        if (efeitoObservadoDocumentado is null)
            return BadRequest(new { message = "Informe o efeito observado documentado." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamKnowledgeEffect + "item",
            Conteudo = MontarPayloadTeamKnowledgeEffect(
                profissionalResponsavel,
                efeitoObservadoDocumentado,
                request.TeamKnowledgeApplicationRelacionadaId,
                request.TeamKnowledgeRelacionadoId,
                request.TeamInsightRelacionadoId,
                request.TeamLearningRelacionadoId,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.ContextoObservacao,
                request.BaseObservacionalEvidenciaSuporte,
                request.InterpretacaoProfissional,
                request.ResultadoObservadoDocumentado,
                request.ImpactoPercebidoDocumentado,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledgeEffect(nota));
    }

    [HttpPut("team-knowledge-effect/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectPersistedResponse>> AtualizarTeamKnowledgeEffect(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamKnowledgeEffectRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var efeitoObservadoDocumentado = NormalizarObrigatorio(request.EfeitoObservadoDocumentado, 3000);
        if (efeitoObservadoDocumentado is null)
            return BadRequest(new { message = "Informe o efeito observado documentado." });

        if (request.TeamKnowledgeApplicationRelacionadaId.HasValue &&
            !await TeamKnowledgeApplicationPertencePacienteAsync(pacienteId, request.TeamKnowledgeApplicationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge Application relacionada inválida para este paciente." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamKnowledgeEffect(nota);
        var payloadAtualizado = new TeamKnowledgeEffectPayload(
            profissionalResponsavel,
            efeitoObservadoDocumentado,
            request.TeamKnowledgeApplicationRelacionadaId,
            request.TeamKnowledgeRelacionadoId,
            request.TeamInsightRelacionadoId,
            request.TeamLearningRelacionadoId,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.ContextoObservacao, 3000),
            NormalizarOpcional(request.BaseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(request.InterpretacaoProfissional, 3000),
            NormalizarOpcional(request.ResultadoObservadoDocumentado, 3000),
            NormalizarOpcional(request.ImpactoPercebidoDocumentado, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledgeEffect(nota));
    }

    [HttpGet("team-knowledge-effect/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectHistoryResponse>> HistoricoTeamKnowledgeEffect(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamKnowledgeEffectExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect),
                cancellationToken);

        if (!teamKnowledgeEffectExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_"));

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

            return new ProfessionalReviewTeamKnowledgeEffectHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamKnowledgeEffect(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeEffectHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais dos efeitos observados do conhecimento da equipe. Não transforma efeito observado em causalidade comprovada ou evidência clínica validada, não interpreta evolução clínica, prognóstico, urgência, prioridade, risco, resultado clínico ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-knowledge-effect/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeEffectPersistedResponse>> AtualizarStatusTeamKnowledgeEffect(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamKnowledgeEffectStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamKnowledgeEffectValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamKnowledgeEffect(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamKnowledgeEffect(nota));
    }

    [HttpDelete("team-knowledge-effect/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamKnowledgeEffect(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-knowledge-application/closure")]
    public ActionResult<ProfessionalReviewTeamKnowledgeApplicationClosureResponse> FechamentoTeamKnowledgeApplications()
    {
        var componentes = new[]
        {
            "TeamKnowledgeApplicationFoundation",
            "TeamKnowledgeApplicationPersistence",
            "TeamKnowledgeApplicationStatus",
            "TeamKnowledgeApplicationHistory",
            "TeamKnowledgeApplicationFilters",
            "TeamKnowledgeApplicationSummary"
        };

        return Ok(new ProfessionalReviewTeamKnowledgeApplicationClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamKnowledgeApplicationCompleta",
            "O fechamento descreve apenas disponibilidade estrutural da aplicação documentada do conhecimento da equipe. Não transforma aplicação em evidência clínica validada, não infere causalidade, prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-application/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeApplicationSummaryResponse>> ResumoTeamKnowledgeApplications(
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
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamKnowledgeApplication).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var registrados = itens.Count(x => !x.Arquivada && x.Status == "Registrado");
        var emRevisao = itens.Count(x => !x.Arquivada && x.Status == "EmRevisao");
        var consolidados = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartados = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamKnowledgeApplicationProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeApplicationSummaryResponse(
            total,
            ativos,
            registrados,
            emRevisao,
            consolidados,
            descartados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais das aplicações registradas do conhecimento da equipe. Não transforma aplicação em evidência clínica validada, não infere causalidade, prognóstico, recomendação ou decisão terapêutica, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-application/search")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeApplicationFiltersResponse>> FiltrarTeamKnowledgeApplications(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamKnowledgeApplicationValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamKnowledgeApplication)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.AplicacaoDocumentada.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ObjetivoAplicacao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ContextoAplicacao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.BaseObservacionalEvidenciaSuporte?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.InterpretacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ResultadoEsperadoDocumentado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeApplicationFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar aplicações documentadas do conhecimento da equipe. Não transformam aplicação em evidência clínica validada, não inferem causalidade, prognóstico, recomendação, decisão terapêutica, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge-application")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamKnowledgeApplicationPersistedResponse>>> ListarTeamKnowledgeApplications(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamKnowledgeApplication).ToArray());
    }

    [HttpPost("team-knowledge-application")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeApplicationPersistedResponse>> CriarTeamKnowledgeApplication(
        Guid pacienteId,
        CriarProfessionalReviewTeamKnowledgeApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var aplicacaoDocumentada = NormalizarObrigatorio(request.AplicacaoDocumentada, 3000);
        if (aplicacaoDocumentada is null)
            return BadRequest(new { message = "Informe a aplicação documentada." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamKnowledgeApplication + "item",
            Conteudo = MontarPayloadTeamKnowledgeApplication(
                profissionalResponsavel,
                aplicacaoDocumentada,
                request.TeamKnowledgeRelacionadoId,
                request.TeamInsightRelacionadoId,
                request.TeamLearningRelacionadoId,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.ObjetivoAplicacao,
                request.ContextoAplicacao,
                request.BaseObservacionalEvidenciaSuporte,
                request.InterpretacaoProfissional,
                request.ResultadoEsperadoDocumentado,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledgeApplication(nota));
    }

    [HttpPut("team-knowledge-application/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeApplicationPersistedResponse>> AtualizarTeamKnowledgeApplication(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamKnowledgeApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var aplicacaoDocumentada = NormalizarObrigatorio(request.AplicacaoDocumentada, 3000);
        if (aplicacaoDocumentada is null)
            return BadRequest(new { message = "Informe a aplicação documentada." });

        if (request.TeamKnowledgeRelacionadoId.HasValue &&
            !await TeamKnowledgePertencePacienteAsync(pacienteId, request.TeamKnowledgeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Knowledge relacionado inválido para este paciente." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamKnowledgeApplication(nota);
        var payloadAtualizado = new TeamKnowledgeApplicationPayload(
            profissionalResponsavel,
            aplicacaoDocumentada,
            request.TeamKnowledgeRelacionadoId,
            request.TeamInsightRelacionadoId,
            request.TeamLearningRelacionadoId,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.ObjetivoAplicacao, 3000),
            NormalizarOpcional(request.ContextoAplicacao, 3000),
            NormalizarOpcional(request.BaseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(request.InterpretacaoProfissional, 3000),
            NormalizarOpcional(request.ResultadoEsperadoDocumentado, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledgeApplication(nota));
    }

    [HttpGet("team-knowledge-application/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeApplicationHistoryResponse>> HistoricoTeamKnowledgeApplication(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamKnowledgeApplicationExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication),
                cancellationToken);

        if (!teamKnowledgeApplicationExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_"));

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

            return new ProfessionalReviewTeamKnowledgeApplicationHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamKnowledgeApplication(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeApplicationHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais da aplicação do conhecimento da equipe. Não transforma aplicação em evidência clínica validada, não interpreta causalidade, evolução clínica, prognóstico, urgência, prioridade, risco, resultado clínico ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-knowledge-application/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeApplicationPersistedResponse>> AtualizarStatusTeamKnowledgeApplication(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamKnowledgeApplicationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamKnowledgeApplicationValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamKnowledgeApplication(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamKnowledgeApplication(nota));
    }

    [HttpDelete("team-knowledge-application/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamKnowledgeApplication(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-knowledge/closure")]
    public ActionResult<ProfessionalReviewTeamKnowledgeClosureResponse> FechamentoTeamKnowledges()
    {
        var componentes = new[]
        {
            "TeamKnowledgeFoundation",
            "TeamKnowledgePersistence",
            "TeamKnowledgeStatus",
            "TeamKnowledgeHistory",
            "TeamKnowledgeFilters",
            "TeamKnowledgeSummary"
        };

        return Ok(new ProfessionalReviewTeamKnowledgeClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamKnowledgeCompleta",
            "O fechamento descreve apenas disponibilidade estrutural do conhecimento documentado da equipe. Não transforma conhecimento em evidência clínica validada, não infere causalidade, prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeSummaryResponse>> ResumoTeamKnowledges(
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
                x.Categoria.StartsWith(PrefixoTeamKnowledge))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamKnowledge).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var registrados = itens.Count(x => !x.Arquivada && x.Status == "Registrado");
        var emRevisao = itens.Count(x => !x.Arquivada && x.Status == "EmRevisao");
        var consolidados = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartados = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamKnowledgeProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeSummaryResponse(
            total,
            ativos,
            registrados,
            emRevisao,
            consolidados,
            descartados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais do conhecimento registrado da equipe. Não transforma conhecimento em evidência clínica validada, não infere causalidade, prognóstico, recomendação ou decisão terapêutica, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge/search")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeFiltersResponse>> FiltrarTeamKnowledges(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamKnowledgeValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledge) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamKnowledge)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.ConhecimentoDocumentado.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.BaseObservacionalEvidenciaSuporte?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.InterpretacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Aplicabilidade?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar conhecimento documentado da equipe. Não transformam conhecimento em evidência clínica validada, não inferem causalidade, prognóstico, recomendação, decisão terapêutica, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-knowledge")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamKnowledgePersistedResponse>>> ListarTeamKnowledges(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledge));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamKnowledge).ToArray());
    }

    [HttpPost("team-knowledge")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgePersistedResponse>> CriarTeamKnowledge(
        Guid pacienteId,
        CriarProfessionalReviewTeamKnowledgeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var conhecimentoDocumentado = NormalizarObrigatorio(request.ConhecimentoDocumentado, 3000);
        if (conhecimentoDocumentado is null)
            return BadRequest(new { message = "Informe o conhecimento documentado." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamKnowledge + "item",
            Conteudo = MontarPayloadTeamKnowledge(
                profissionalResponsavel,
                conhecimentoDocumentado,
                request.TeamInsightRelacionadoId,
                request.TeamLearningRelacionadoId,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.BaseObservacionalEvidenciaSuporte,
                request.InterpretacaoProfissional,
                request.Aplicabilidade,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledge(nota));
    }

    [HttpPut("team-knowledge/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgePersistedResponse>> AtualizarTeamKnowledge(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamKnowledgeRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledge),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var conhecimentoDocumentado = NormalizarObrigatorio(request.ConhecimentoDocumentado, 3000);
        if (conhecimentoDocumentado is null)
            return BadRequest(new { message = "Informe o conhecimento documentado." });

        if (request.TeamInsightRelacionadoId.HasValue &&
            !await TeamInsightPertencePacienteAsync(pacienteId, request.TeamInsightRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Insight relacionado inválido para este paciente." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamKnowledge(nota);
        var payloadAtualizado = new TeamKnowledgePayload(
            profissionalResponsavel,
            conhecimentoDocumentado,
            request.TeamInsightRelacionadoId,
            request.TeamLearningRelacionadoId,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.BaseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(request.InterpretacaoProfissional, 3000),
            NormalizarOpcional(request.Aplicabilidade, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamKnowledge(nota));
    }

    [HttpGet("team-knowledge/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgeHistoryResponse>> HistoricoTeamKnowledge(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamKnowledgeExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledge),
                cancellationToken);

        if (!teamKnowledgeExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_"));

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

            return new ProfessionalReviewTeamKnowledgeHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamKnowledge(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamKnowledgeHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais do conhecimento da equipe. Não transforma conhecimento em evidência clínica validada, não interpreta causalidade, evolução clínica, prognóstico, urgência, prioridade, risco, resultado clínico ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-knowledge/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamKnowledgePersistedResponse>> AtualizarStatusTeamKnowledge(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamKnowledgeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamKnowledgeValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledge) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamKnowledge(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamKnowledge(nota));
    }

    [HttpDelete("team-knowledge/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamKnowledge(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledge),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-insight/closure")]
    public ActionResult<ProfessionalReviewTeamInsightClosureResponse> FechamentoTeamInsights()
    {
        var componentes = new[]
        {
            "TeamInsightFoundation",
            "TeamInsightPersistence",
            "TeamInsightStatus",
            "TeamInsightHistory",
            "TeamInsightFilters",
            "TeamInsightSummary"
        };

        return Ok(new ProfessionalReviewTeamInsightClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamInsightCompleta",
            "O fechamento descreve apenas disponibilidade estrutural dos insights documentados da equipe. Não transforma insight em evidência clínica validada, não infere causalidade, prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-insight/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamInsightSummaryResponse>> ResumoTeamInsights(
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
                x.Categoria.StartsWith(PrefixoTeamInsight))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamInsight).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var registrados = itens.Count(x => !x.Arquivada && x.Status == "Registrado");
        var emRevisao = itens.Count(x => !x.Arquivada && x.Status == "EmRevisao");
        var consolidados = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartados = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamInsightProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamInsightSummaryResponse(
            total,
            ativos,
            registrados,
            emRevisao,
            consolidados,
            descartados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais dos insights registrados da equipe. Não transforma insight em evidência clínica validada, não infere causalidade, prognóstico, recomendação ou decisão terapêutica, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-insight/search")]
    public async Task<ActionResult<ProfessionalReviewTeamInsightFiltersResponse>> FiltrarTeamInsights(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamInsightValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamInsight) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamInsight)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.InsightDocumentado.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.BaseObservacionalEvidenciaSuporte?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.InterpretacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Aplicabilidade?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamInsightFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar insights documentados da equipe. Não transformam insight em evidência clínica validada, não inferem causalidade, prognóstico, recomendação, decisão terapêutica, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-insight")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamInsightPersistedResponse>>> ListarTeamInsights(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamInsight));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamInsight).ToArray());
    }

    [HttpPost("team-insight")]
    public async Task<ActionResult<ProfessionalReviewTeamInsightPersistedResponse>> CriarTeamInsight(
        Guid pacienteId,
        CriarProfessionalReviewTeamInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var insightDocumentado = NormalizarObrigatorio(request.InsightDocumentado, 3000);
        if (insightDocumentado is null)
            return BadRequest(new { message = "Informe o insight documentado." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamInsight + "item",
            Conteudo = MontarPayloadTeamInsight(
                profissionalResponsavel,
                insightDocumentado,
                request.TeamLearningRelacionadoId,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.BaseObservacionalEvidenciaSuporte,
                request.InterpretacaoProfissional,
                request.Aplicabilidade,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_INSIGHT_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamInsight(nota));
    }

    [HttpPut("team-insight/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamInsightPersistedResponse>> AtualizarTeamInsight(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamInsightRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamInsight),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var insightDocumentado = NormalizarObrigatorio(request.InsightDocumentado, 3000);
        if (insightDocumentado is null)
            return BadRequest(new { message = "Informe o insight documentado." });

        if (request.TeamLearningRelacionadoId.HasValue &&
            !await TeamLearningPertencePacienteAsync(pacienteId, request.TeamLearningRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Learning relacionado inválido para este paciente." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamInsight(nota);
        var payloadAtualizado = new TeamInsightPayload(
            profissionalResponsavel,
            insightDocumentado,
            request.TeamLearningRelacionadoId,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.BaseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(request.InterpretacaoProfissional, 3000),
            NormalizarOpcional(request.Aplicabilidade, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_INSIGHT_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamInsight(nota));
    }

    [HttpGet("team-insight/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamInsightHistoryResponse>> HistoricoTeamInsight(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamInsightExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamInsight),
                cancellationToken);

        if (!teamInsightExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_INSIGHT_"));

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

            return new ProfessionalReviewTeamInsightHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamInsight(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamInsightHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais dos insights da equipe. Não transforma insight em evidência clínica validada, não interpreta causalidade, evolução clínica, prognóstico, urgência, prioridade, risco, resultado clínico ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-insight/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamInsightPersistedResponse>> AtualizarStatusTeamInsight(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamInsightStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamInsightValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamInsight) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamInsight(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_INSIGHT_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamInsight(nota));
    }

    [HttpDelete("team-insight/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamInsight(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamInsight),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_INSIGHT_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-learning/closure")]
    public ActionResult<ProfessionalReviewTeamLearningClosureResponse> FechamentoTeamLearnings()
    {
        var componentes = new[]
        {
            "TeamLearningFoundation",
            "TeamLearningPersistence",
            "TeamLearningStatus",
            "TeamLearningHistory",
            "TeamLearningFilters",
            "TeamLearningSummary"
        };

        return Ok(new ProfessionalReviewTeamLearningClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamLearningCompleta",
            "O fechamento descreve apenas disponibilidade estrutural dos aprendizados documentados da equipe. Não transforma aprendizado em evidência clínica validada, não infere causalidade, prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-learning/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamLearningSummaryResponse>> ResumoTeamLearnings(
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
                x.Categoria.StartsWith(PrefixoTeamLearning))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamLearning).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var registrados = itens.Count(x => !x.Arquivada && x.Status == "Registrado");
        var emRevisao = itens.Count(x => !x.Arquivada && x.Status == "EmRevisao");
        var consolidados = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartados = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamLearningProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamLearningSummaryResponse(
            total,
            ativos,
            registrados,
            emRevisao,
            consolidados,
            descartados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais dos aprendizados registrados da equipe. Não transforma aprendizado em evidência clínica validada, não infere causalidade, prognóstico ou recomendação, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-learning/search")]
    public async Task<ActionResult<ProfessionalReviewTeamLearningFiltersResponse>> FiltrarTeamLearnings(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamLearningValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamLearning) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamLearning)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.AprendizadoDocumentado.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.EvidenciaBaseObservacional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Aplicabilidade?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamLearningFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar aprendizados documentados da equipe. Não transformam aprendizado em evidência clínica validada, não inferem causalidade, prognóstico, recomendação, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-learning")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamLearningPersistedResponse>>> ListarTeamLearnings(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamLearning));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamLearning).ToArray());
    }

    [HttpPost("team-learning")]
    public async Task<ActionResult<ProfessionalReviewTeamLearningPersistedResponse>> CriarTeamLearning(
        Guid pacienteId,
        CriarProfessionalReviewTeamLearningRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var aprendizadoDocumentado = NormalizarObrigatorio(request.AprendizadoDocumentado, 3000);
        if (aprendizadoDocumentado is null)
            return BadRequest(new { message = "Informe o aprendizado documentado." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamLearning + "item",
            Conteudo = MontarPayloadTeamLearning(
                profissionalResponsavel,
                aprendizadoDocumentado,
                request.TeamOutcomeRelacionadoId,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.EvidenciaBaseObservacional,
                request.Aplicabilidade,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_LEARNING_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamLearning(nota));
    }

    [HttpPut("team-learning/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamLearningPersistedResponse>> AtualizarTeamLearning(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamLearningRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamLearning),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var aprendizadoDocumentado = NormalizarObrigatorio(request.AprendizadoDocumentado, 3000);
        if (aprendizadoDocumentado is null)
            return BadRequest(new { message = "Informe o aprendizado documentado." });

        if (request.TeamOutcomeRelacionadoId.HasValue &&
            !await TeamOutcomePertencePacienteAsync(pacienteId, request.TeamOutcomeRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Outcome relacionado inválido para este paciente." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamLearning(nota);
        var payloadAtualizado = new TeamLearningPayload(
            profissionalResponsavel,
            aprendizadoDocumentado,
            request.TeamOutcomeRelacionadoId,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.EvidenciaBaseObservacional, 3000),
            NormalizarOpcional(request.Aplicabilidade, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_LEARNING_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamLearning(nota));
    }

    [HttpGet("team-learning/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamLearningHistoryResponse>> HistoricoTeamLearning(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamLearningExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamLearning),
                cancellationToken);

        if (!teamLearningExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_LEARNING_"));

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

            return new ProfessionalReviewTeamLearningHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamLearning(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamLearningHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais dos aprendizados da equipe. Não transforma aprendizado em evidência clínica validada, não interpreta causalidade, evolução clínica, prognóstico, urgência, prioridade, risco ou resultado clínico, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-learning/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamLearningPersistedResponse>> AtualizarStatusTeamLearning(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamLearningStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamLearningValido(status))
            return BadRequest(new { message = "Status inválido. Use Registrado, EmRevisao, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamLearning) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamLearning(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_LEARNING_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamLearning(nota));
    }

    [HttpDelete("team-learning/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamLearning(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamLearning),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_LEARNING_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-outcome/closure")]
    public ActionResult<ProfessionalReviewTeamOutcomeClosureResponse> FechamentoTeamOutcomes()
    {
        var componentes = new[]
        {
            "TeamOutcomeFoundation",
            "TeamOutcomePersistence",
            "TeamOutcomeStatus",
            "TeamOutcomeHistory",
            "TeamOutcomeFilters",
            "TeamOutcomeSummary"
        };

        return Ok(new ProfessionalReviewTeamOutcomeClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamOutcomeCompleta",
            "O fechamento descreve apenas disponibilidade estrutural dos resultados documentados da equipe. Não infere causalidade, prognóstico ou recomendação, não representa score clínico, risco, urgência, prioridade ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-outcome/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamOutcomeSummaryResponse>> ResumoTeamOutcomes(
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
                x.Categoria.StartsWith(PrefixoTeamOutcome))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamOutcome).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var observados = itens.Count(x => !x.Arquivada && x.Status == "Observado");
        var emAcompanhamento = itens.Count(x => !x.Arquivada && x.Status == "EmAcompanhamento");
        var consolidados = itens.Count(x => !x.Arquivada && x.Status == "Consolidado");
        var descartados = itens.Count(x => !x.Arquivada && x.Status == "Descartado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamOutcomeProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamOutcomeSummaryResponse(
            total,
            ativos,
            observados,
            emAcompanhamento,
            consolidados,
            descartados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais dos resultados registrados da equipe. Não infere causalidade, prognóstico ou recomendação, não representa score clínico, risco, urgência ou prioridade, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-outcome/search")]
    public async Task<ActionResult<ProfessionalReviewTeamOutcomeFiltersResponse>> FiltrarTeamOutcomes(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamOutcomeValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Observado, EmAcompanhamento, Consolidado ou Descartado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamOutcome) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamOutcome)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.ResultadoDocumentado.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.EvidenciaSuporte?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamOutcomeFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar resultados documentados da equipe. Não inferem causalidade, prognóstico, recomendação, urgência, risco, prioridade clínica ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-outcome")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamOutcomePersistedResponse>>> ListarTeamOutcomes(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamOutcome));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamOutcome).ToArray());
    }

    [HttpPost("team-outcome")]
    public async Task<ActionResult<ProfessionalReviewTeamOutcomePersistedResponse>> CriarTeamOutcome(
        Guid pacienteId,
        CriarProfessionalReviewTeamOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var resultadoDocumentado = NormalizarObrigatorio(request.ResultadoDocumentado, 3000);
        if (resultadoDocumentado is null)
            return BadRequest(new { message = "Informe o resultado documentado." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamOutcome + "item",
            Conteudo = MontarPayloadTeamOutcome(
                profissionalResponsavel,
                resultadoDocumentado,
                request.TeamDecisionRelacionadaId,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.EvidenciaSuporte,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_OUTCOME_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamOutcome(nota));
    }

    [HttpPut("team-outcome/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamOutcomePersistedResponse>> AtualizarTeamOutcome(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamOutcome),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var resultadoDocumentado = NormalizarObrigatorio(request.ResultadoDocumentado, 3000);
        if (resultadoDocumentado is null)
            return BadRequest(new { message = "Informe o resultado documentado." });

        if (request.TeamDecisionRelacionadaId.HasValue &&
            !await TeamDecisionPertencePacienteAsync(pacienteId, request.TeamDecisionRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Team Decision relacionada inválida para este paciente." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamOutcome(nota);
        var payloadAtualizado = new TeamOutcomePayload(
            profissionalResponsavel,
            resultadoDocumentado,
            request.TeamDecisionRelacionadaId,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.EvidenciaSuporte, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_OUTCOME_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamOutcome(nota));
    }

    [HttpGet("team-outcome/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamOutcomeHistoryResponse>> HistoricoTeamOutcome(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamOutcomeExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamOutcome),
                cancellationToken);

        if (!teamOutcomeExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_OUTCOME_"));

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

            return new ProfessionalReviewTeamOutcomeHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamOutcome(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamOutcomeHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra apenas eventos documentais dos resultados da equipe. Não interpreta causalidade, evolução clínica, prognóstico, urgência, prioridade, risco ou resultado clínico, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-outcome/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamOutcomePersistedResponse>> AtualizarStatusTeamOutcome(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamOutcomeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamOutcomeValido(status))
            return BadRequest(new { message = "Status inválido. Use Observado, EmAcompanhamento, Consolidado ou Descartado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamOutcome) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamOutcome(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_OUTCOME_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamOutcome(nota));
    }

    [HttpDelete("team-outcome/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamOutcome(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamOutcome),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_OUTCOME_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-decision/closure")]
    public ActionResult<ProfessionalReviewTeamDecisionClosureResponse> FechamentoTeamDecisions()
    {
        var componentes = new[]
        {
            "TeamDecisionFoundation",
            "TeamDecisionPersistence",
            "TeamDecisionStatus",
            "TeamDecisionHistory",
            "TeamDecisionFilters",
            "TeamDecisionSummary"
        };

        return Ok(new ProfessionalReviewTeamDecisionClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamDecisionCompleta",
            "O fechamento descreve apenas disponibilidade estrutural das decisões documentadas da equipe. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-decision/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamDecisionSummaryResponse>> ResumoTeamDecisions(
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
                x.Categoria.StartsWith(PrefixoTeamDecision))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamDecision).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var planejadas = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidas = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var canceladas = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamDecisionProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamDecisionSummaryResponse(
            total,
            ativas,
            planejadas,
            emAndamento,
            concluidas,
            canceladas,
            arquivadas,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais das decisões registradas da equipe. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-decision/search")]
    public async Task<ActionResult<ProfessionalReviewTeamDecisionFiltersResponse>> FiltrarTeamDecisions(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamDecisionValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamDecision) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamDecision)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    x.DecisaoDocumentada.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.RacionalJustificativa?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamDecisionFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar decisões documentadas da equipe. Não classificam urgência, risco, prioridade clínica, prognóstico ou necessidade de intervenção, não executam conduta ou prescrição e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-decision")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamDecisionPersistedResponse>>> ListarTeamDecisions(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamDecision));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamDecision).ToArray());
    }

    [HttpPost("team-decision")]
    public async Task<ActionResult<ProfessionalReviewTeamDecisionPersistedResponse>> CriarTeamDecision(
        Guid pacienteId,
        CriarProfessionalReviewTeamDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var decisaoDocumentada = NormalizarObrigatorio(request.DecisaoDocumentada, 3000);
        if (decisaoDocumentada is null)
            return BadRequest(new { message = "Informe a decisão documentada." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamDecision + "item",
            Conteudo = MontarPayloadTeamDecision(
                profissionalResponsavel,
                decisaoDocumentada,
                request.TeamAlignmentRelacionadoId,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.RacionalJustificativa,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_DECISION_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamDecision(nota));
    }

    [HttpPut("team-decision/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamDecisionPersistedResponse>> AtualizarTeamDecision(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamDecision),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        var decisaoDocumentada = NormalizarObrigatorio(request.DecisaoDocumentada, 3000);
        if (decisaoDocumentada is null)
            return BadRequest(new { message = "Informe a decisão documentada." });

        if (request.TeamAlignmentRelacionadoId.HasValue &&
            !await TeamAlignmentPertencePacienteAsync(pacienteId, request.TeamAlignmentRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Team Alignment relacionado inválido para este paciente." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamDecision(nota);
        var payloadAtualizado = new TeamDecisionPayload(
            profissionalResponsavel,
            decisaoDocumentada,
            request.TeamAlignmentRelacionadoId,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.RacionalJustificativa, 3000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_DECISION_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamDecision(nota));
    }

    [HttpGet("team-decision/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamDecisionHistoryResponse>> HistoricoTeamDecision(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamDecisionExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamDecision),
                cancellationToken);

        if (!teamDecisionExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_DECISION_"));

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

            return new ProfessionalReviewTeamDecisionHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamDecision(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamDecisionHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais das decisões da equipe. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado, não executa conduta ou prescrição e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-decision/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamDecisionPersistedResponse>> AtualizarStatusTeamDecision(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamDecisionStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamDecisionValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamDecision) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamDecision(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_DECISION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamDecision(nota));
    }

    [HttpDelete("team-decision/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamDecision(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamDecision),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_DECISION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("team-alignment/closure")]
    public ActionResult<ProfessionalReviewTeamAlignmentClosureResponse> FechamentoTeamAlignments()
    {
        var componentes = new[]
        {
            "TeamAlignmentFoundation",
            "TeamAlignmentPersistence",
            "TeamAlignmentStatus",
            "TeamAlignmentHistory",
            "TeamAlignmentFilters",
            "TeamAlignmentSummary"
        };

        return Ok(new ProfessionalReviewTeamAlignmentClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaTeamAlignmentCompleta",
            "O fechamento descreve apenas disponibilidade estrutural do alinhamento entre profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-alignment/summary")]
    public async Task<ActionResult<ProfessionalReviewTeamAlignmentSummaryResponse>> ResumoTeamAlignments(
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
                x.Categoria.StartsWith(PrefixoTeamAlignment))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearTeamAlignment).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var planejados = itens.Count(x => !x.Arquivada && x.Status == "Planejado");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidos = itens.Count(x => !x.Arquivada && x.Status == "Concluido");
        var cancelados = itens.Count(x => !x.Arquivada && x.Status == "Cancelado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewTeamAlignmentProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewTeamAlignmentSummaryResponse(
            total,
            ativos,
            planejados,
            emAndamento,
            concluidos,
            cancelados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais dos alinhamentos entre profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-alignment/search")]
    public async Task<ActionResult<ProfessionalReviewTeamAlignmentFiltersResponse>> FiltrarTeamAlignments(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusTeamAlignmentValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamAlignment) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearTeamAlignment)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObjetivoAlinhamento?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewTeamAlignmentFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros documentais de alinhamento entre profissionais. Não classificam urgência, risco, prioridade clínica, prognóstico ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("team-alignment")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewTeamAlignmentPersistedResponse>>> ListarTeamAlignments(
        Guid pacienteId,
        [FromQuery] bool incluirArquivados = false,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var query = db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamAlignment));

        if (!incluirArquivados)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearTeamAlignment).ToArray());
    }

    [HttpPost("team-alignment")]
    public async Task<ActionResult<ProfessionalReviewTeamAlignmentPersistedResponse>> CriarTeamAlignment(
        Guid pacienteId,
        CriarProfessionalReviewTeamAlignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoTeamAlignment + "item",
            Conteudo = MontarPayloadTeamAlignment(
                profissionalResponsavel,
                request.SharedContextRelacionadoId,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.ObjetivoAlinhamento,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamAlignment(nota));
    }

    [HttpPut("team-alignment/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewTeamAlignmentPersistedResponse>> AtualizarTeamAlignment(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewTeamAlignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamAlignment),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        if (request.SharedContextRelacionadoId.HasValue &&
            !await SharedContextPertencePacienteAsync(pacienteId, request.SharedContextRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Shared Context relacionado inválido para este paciente." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadTeamAlignment(nota);
        var payloadAtualizado = new TeamAlignmentPayload(
            profissionalResponsavel,
            request.SharedContextRelacionadoId,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.ObjetivoAlinhamento, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearTeamAlignment(nota));
    }

    [HttpGet("team-alignment/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewTeamAlignmentHistoryResponse>> HistoricoTeamAlignment(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var teamAlignmentExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamAlignment),
                cancellationToken);

        if (!teamAlignmentExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_"));

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

            return new ProfessionalReviewTeamAlignmentHistoryItemResponse(
                log.Id,
                id,
                MapearEventoTeamAlignment(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewTeamAlignmentHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais de alinhamento entre profissionais. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("team-alignment/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewTeamAlignmentPersistedResponse>> AtualizarStatusTeamAlignment(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewTeamAlignmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusTeamAlignmentValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamAlignment) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadTeamAlignment(nota);

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
                "PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearTeamAlignment(nota));
    }

    [HttpDelete("team-alignment/{id:guid}")]
    public async Task<IActionResult> ArquivarTeamAlignment(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamAlignment),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("shared-context/closure")]
    public ActionResult<ProfessionalReviewSharedContextClosureResponse> FechamentoSharedContexts()
    {
        var componentes = new[]
        {
            "SharedContextFoundation",
            "SharedContextPersistence",
            "SharedContextStatus",
            "SharedContextHistory",
            "SharedContextFilters",
            "SharedContextSummary"
        };

        return Ok(new ProfessionalReviewSharedContextClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaSharedContextCompleta",
            "O fechamento descreve apenas disponibilidade estrutural do contexto profissional compartilhado. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("shared-context/summary")]
    public async Task<ActionResult<ProfessionalReviewSharedContextSummaryResponse>> ResumoSharedContexts(
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
                x.Categoria.StartsWith(PrefixoSharedContext))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearSharedContext).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var planejados = itens.Count(x => !x.Arquivada && x.Status == "Planejado");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidos = itens.Count(x => !x.Arquivada && x.Status == "Concluido");
        var cancelados = itens.Count(x => !x.Arquivada && x.Status == "Cancelado");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewSharedContextProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewSharedContextSummaryResponse(
            total,
            ativos,
            planejados,
            emAndamento,
            concluidos,
            cancelados,
            arquivados,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais dos contextos profissionais compartilhados. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("shared-context/search")]
    public async Task<ActionResult<ProfessionalReviewSharedContextFiltersResponse>> FiltrarSharedContexts(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivados = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusSharedContextValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoSharedContext) &&
                (incluirArquivados || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearSharedContext)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Participantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ContextoCompartilhado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewSharedContextFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivados,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros documentais de contexto profissional compartilhado. Não classificam urgência, risco, prioridade clínica, prognóstico ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("shared-context")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewSharedContextPersistedResponse>>> ListarSharedContexts(
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
                x.Categoria.StartsWith(PrefixoSharedContext));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearSharedContext).ToArray());
    }

    [HttpPost("shared-context")]
    public async Task<ActionResult<ProfessionalReviewSharedContextPersistedResponse>> CriarSharedContext(
        Guid pacienteId,
        CriarProfessionalReviewSharedContextRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoSharedContext + "item",
            Conteudo = MontarPayloadSharedContext(
                profissionalResponsavel,
                request.CollaborationRelacionadaId,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.Participantes,
                request.ContextoCompartilhado,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_SHARED_CONTEXT_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearSharedContext(nota));
    }

    [HttpPut("shared-context/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewSharedContextPersistedResponse>> AtualizarSharedContext(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewSharedContextRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoSharedContext),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        if (request.CollaborationRelacionadaId.HasValue &&
            !await CollaborationPertencePacienteAsync(pacienteId, request.CollaborationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Collaboration relacionada inválida para este paciente." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadSharedContext(nota);
        var payloadAtualizado = new SharedContextPayload(
            profissionalResponsavel,
            request.CollaborationRelacionadaId,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.Participantes, 1000),
            NormalizarOpcional(request.ContextoCompartilhado, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_SHARED_CONTEXT_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearSharedContext(nota));
    }

    [HttpGet("shared-context/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewSharedContextHistoryResponse>> HistoricoSharedContext(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var sharedContextExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoSharedContext),
                cancellationToken);

        if (!sharedContextExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_SHARED_CONTEXT_"));

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

            return new ProfessionalReviewSharedContextHistoryItemResponse(
                log.Id,
                id,
                MapearEventoSharedContext(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewSharedContextHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais de contexto profissional compartilhado. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("shared-context/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewSharedContextPersistedResponse>> AtualizarStatusSharedContext(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewSharedContextStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusSharedContextValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoSharedContext) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadSharedContext(nota);

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
                "PROFESSIONAL_REVIEW_SHARED_CONTEXT_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearSharedContext(nota));
    }

    [HttpDelete("shared-context/{id:guid}")]
    public async Task<IActionResult> ArquivarSharedContext(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoSharedContext),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_SHARED_CONTEXT_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("collaboration/closure")]
    public ActionResult<ProfessionalReviewCollaborationClosureResponse> FechamentoCollaborations()
    {
        var componentes = new[]
        {
            "CollaborationFoundation",
            "CollaborationPersistence",
            "CollaborationStatus",
            "CollaborationHistory",
            "CollaborationFilters",
            "CollaborationSummary"
        };

        return Ok(new ProfessionalReviewCollaborationClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaCollaborationCompleta",
            "O fechamento descreve apenas disponibilidade estrutural da colaboração profissional. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("collaboration/summary")]
    public async Task<ActionResult<ProfessionalReviewCollaborationSummaryResponse>> ResumoCollaborations(
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
                x.Categoria.StartsWith(PrefixoCollaboration))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearCollaboration).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var planejadas = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidas = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var canceladas = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porProfissionalResponsavel = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalResponsavel))
            .GroupBy(x => x.ProfissionalResponsavel.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewCollaborationProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewCollaborationSummaryResponse(
            total,
            ativas,
            planejadas,
            emAndamento,
            concluidas,
            canceladas,
            arquivadas,
            porProfissionalResponsavel,
            "O resumo apresenta somente agregações documentais das colaborações profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("collaboration/search")]
    public async Task<ActionResult<ProfessionalReviewCollaborationFiltersResponse>> FiltrarCollaborations(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalResponsavel = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusCollaborationValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var responsavelNormalizado = NormalizarOpcional(profissionalResponsavel, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCollaboration) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearCollaboration)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (responsavelNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(responsavelNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalResponsavel.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ProfissionaisParticipantes?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ContextoColaboracao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewCollaborationFiltersResponse(
            statusNormalizado,
            responsavelNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros documentais de colaboração profissional. Não classificam urgência, risco, prioridade clínica, prognóstico ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("collaboration")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewCollaborationPersistedResponse>>> ListarCollaborations(
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
                x.Categoria.StartsWith(PrefixoCollaboration));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearCollaboration).ToArray());
    }

    [HttpPost("collaboration")]
    public async Task<ActionResult<ProfessionalReviewCollaborationPersistedResponse>> CriarCollaboration(
        Guid pacienteId,
        CriarProfessionalReviewCollaborationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

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
            Categoria = PrefixoCollaboration + "item",
            Conteudo = MontarPayloadCollaboration(
                profissionalResponsavel,
                request.CoordinationRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContinuityRelacionadaId,
                request.ProfissionaisParticipantes,
                request.ContextoColaboracao,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_COLLABORATION_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearCollaboration(nota));
    }

    [HttpPut("collaboration/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewCollaborationPersistedResponse>> AtualizarCollaboration(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewCollaborationRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCollaboration),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalResponsavel = NormalizarObrigatorio(request.ProfissionalResponsavel, 160);
        if (profissionalResponsavel is null)
            return BadRequest(new { message = "Informe o profissional responsável." });

        if (request.CoordinationRelacionadaId.HasValue &&
            !await CoordinationPertencePacienteAsync(pacienteId, request.CoordinationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Coordination relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadCollaboration(nota);
        var payloadAtualizado = new CollaborationPayload(
            profissionalResponsavel,
            request.CoordinationRelacionadaId,
            request.EscalationRelacionadaId,
            request.ContinuityRelacionadaId,
            NormalizarOpcional(request.ProfissionaisParticipantes, 1000),
            NormalizarOpcional(request.ContextoColaboracao, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_COLLABORATION_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearCollaboration(nota));
    }

    [HttpGet("collaboration/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewCollaborationHistoryResponse>> HistoricoCollaboration(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var collaborationExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCollaboration),
                cancellationToken);

        if (!collaborationExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_COLLABORATION_"));

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

            return new ProfessionalReviewCollaborationHistoryItemResponse(
                log.Id,
                id,
                MapearEventoCollaboration(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewCollaborationHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais de colaboração profissional. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("collaboration/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewCollaborationPersistedResponse>> AtualizarStatusCollaboration(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewCollaborationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusCollaborationValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCollaboration) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadCollaboration(nota);

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
                "PROFESSIONAL_REVIEW_COLLABORATION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearCollaboration(nota));
    }

    [HttpDelete("collaboration/{id:guid}")]
    public async Task<IActionResult> ArquivarCollaboration(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCollaboration),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_COLLABORATION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("coordination/closure")]
    public ActionResult<ProfessionalReviewCoordinationClosureResponse> FechamentoCoordinations()
    {
        var componentes = new[]
        {
            "CoordinationFoundation",
            "CoordinationPersistence",
            "CoordinationStatus",
            "CoordinationHistory",
            "CoordinationFilters",
            "CoordinationSummary"
        };

        return Ok(new ProfessionalReviewCoordinationClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaCoordinationCompleta",
            "O fechamento descreve apenas disponibilidade estrutural da coordenação profissional integrada. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("coordination/summary")]
    public async Task<ActionResult<ProfessionalReviewCoordinationSummaryResponse>> ResumoCoordinations(
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
                x.Categoria.StartsWith(PrefixoCoordination))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearCoordination).ToArray();

        var total = itens.Length;
        var arquivadas = itens.Count(x => x.Arquivada);
        var ativas = itens.Count(x => !x.Arquivada);
        var planejadas = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidas = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var canceladas = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porProfissionalCoordenador = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalCoordenador))
            .GroupBy(x => x.ProfissionalCoordenador.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewCoordinationProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewCoordinationSummaryResponse(
            total,
            ativas,
            planejadas,
            emAndamento,
            concluidas,
            canceladas,
            arquivadas,
            porProfissionalCoordenador,
            "O resumo apresenta somente agregações documentais das coordenações profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("coordination/search")]
    public async Task<ActionResult<ProfessionalReviewCoordinationFiltersResponse>> FiltrarCoordinations(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalCoordenador = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusCoordinationValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var coordenadorNormalizado = NormalizarOpcional(profissionalCoordenador, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCoordination) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearCoordination)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (coordenadorNormalizado is null ||
                    x.ProfissionalCoordenador.Contains(coordenadorNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalCoordenador.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoCoordenacao?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewCoordinationFiltersResponse(
            statusNormalizado,
            coordenadorNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros documentais de coordenação profissional. Não classificam urgência, risco, prioridade clínica, prognóstico ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("coordination")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewCoordinationPersistedResponse>>> ListarCoordinations(
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
                x.Categoria.StartsWith(PrefixoCoordination));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearCoordination).ToArray());
    }

    [HttpPost("coordination")]
    public async Task<ActionResult<ProfessionalReviewCoordinationPersistedResponse>> CriarCoordination(
        Guid pacienteId,
        CriarProfessionalReviewCoordinationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalCoordenador = NormalizarObrigatorio(request.ProfissionalCoordenador, 160);
        if (profissionalCoordenador is null)
            return BadRequest(new { message = "Informe o profissional coordenador." });

        if (request.AssignmentRelacionadaId.HasValue &&
            !await AssignmentPertencePacienteAsync(pacienteId, request.AssignmentRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Assignment relacionada inválida para este paciente." });

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });

        if (request.HandoffRelacionadoId.HasValue &&
            !await HandoffPertencePacienteAsync(pacienteId, request.HandoffRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Handoff relacionado inválido para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

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
            Categoria = PrefixoCoordination + "item",
            Conteudo = MontarPayloadCoordination(
                profissionalCoordenador,
                request.AssignmentRelacionadaId,
                request.DelegationRelacionadaId,
                request.HandoffRelacionadoId,
                request.ContinuityRelacionadaId,
                request.EscalationRelacionadaId,
                request.ContextoCoordenacao,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_COORDINATION_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearCoordination(nota));
    }

    [HttpPut("coordination/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewCoordinationPersistedResponse>> AtualizarCoordination(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewCoordinationRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCoordination),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalCoordenador = NormalizarObrigatorio(request.ProfissionalCoordenador, 160);
        if (profissionalCoordenador is null)
            return BadRequest(new { message = "Informe o profissional coordenador." });

        if (request.AssignmentRelacionadaId.HasValue &&
            !await AssignmentPertencePacienteAsync(pacienteId, request.AssignmentRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Assignment relacionada inválida para este paciente." });

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });

        if (request.HandoffRelacionadoId.HasValue &&
            !await HandoffPertencePacienteAsync(pacienteId, request.HandoffRelacionadoId.Value, cancellationToken))
            return BadRequest(new { message = "Handoff relacionado inválido para este paciente." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });

        if (request.EscalationRelacionadaId.HasValue &&
            !await EscalationPertencePacienteAsync(pacienteId, request.EscalationRelacionadaId.Value, cancellationToken))
            return BadRequest(new { message = "Escalation relacionada inválida para este paciente." });

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadCoordination(nota);
        var payloadAtualizado = new CoordinationPayload(
            profissionalCoordenador,
            request.AssignmentRelacionadaId,
            request.DelegationRelacionadaId,
            request.HandoffRelacionadoId,
            request.ContinuityRelacionadaId,
            request.EscalationRelacionadaId,
            NormalizarOpcional(request.ContextoCoordenacao, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_COORDINATION_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearCoordination(nota));
    }

    [HttpGet("coordination/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewCoordinationHistoryResponse>> HistoricoCoordination(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var coordinationExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCoordination),
                cancellationToken);

        if (!coordinationExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_COORDINATION_"));

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

            return new ProfessionalReviewCoordinationHistoryItemResponse(
                log.Id,
                id,
                MapearEventoCoordination(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewCoordinationHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais de coordenação profissional. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("coordination/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewCoordinationPersistedResponse>> AtualizarStatusCoordination(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewCoordinationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusCoordinationValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCoordination) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadCoordination(nota);

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
                "PROFESSIONAL_REVIEW_COORDINATION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearCoordination(nota));
    }

    [HttpDelete("coordination/{id:guid}")]
    public async Task<IActionResult> ArquivarCoordination(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCoordination),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_COORDINATION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("escalation/closure")]
    public ActionResult<ProfessionalReviewEscalationClosureResponse> FechamentoEscalations()
    {
        var componentes = new[]
        {
            "EscalationFoundation",
            "EscalationPersistence",
            "EscalationStatus",
            "EscalationHistory",
            "EscalationFilters",
            "EscalationSummary"
        };

        return Ok(new ProfessionalReviewEscalationClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaEscalationCompleta",
            "O fechamento descreve apenas disponibilidade estrutural dos escalonamentos profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("escalation/summary")]
    public async Task<ActionResult<ProfessionalReviewEscalationSummaryResponse>> ResumoEscalations(
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
                x.Categoria.StartsWith(PrefixoEscalation))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearEscalation).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var planejados = itens.Count(x => !x.Arquivada && x.Status == "Planejado");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidos = itens.Count(x => !x.Arquivada && x.Status == "Concluido");
        var cancelados = itens.Count(x => !x.Arquivada && x.Status == "Cancelado");

        var porOrigem = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalOrigem))
            .GroupBy(x => x.ProfissionalOrigem.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewEscalationProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        var porDestino = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalDestino))
            .GroupBy(x => x.ProfissionalDestino.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewEscalationProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewEscalationSummaryResponse(
            total,
            ativos,
            planejados,
            emAndamento,
            concluidos,
            cancelados,
            arquivados,
            porOrigem,
            porDestino,
            "O resumo apresenta somente agregações documentais dos escalonamentos profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("escalation/search")]
    public async Task<ActionResult<ProfessionalReviewEscalationFiltersResponse>> FiltrarEscalations(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalOrigem = null,
        [FromQuery] string? profissionalDestino = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusEscalationValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var origemNormalizada = NormalizarOpcional(profissionalOrigem, 160);
        var destinoNormalizado = NormalizarOpcional(profissionalDestino, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoEscalation) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearEscalation)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (origemNormalizada is null ||
                    x.ProfissionalOrigem.Contains(origemNormalizada, StringComparison.OrdinalIgnoreCase)) &&
                (destinoNormalizado is null ||
                    x.ProfissionalDestino.Contains(destinoNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalOrigem.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    x.ProfissionalDestino.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoEscalado?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewEscalationFiltersResponse(
            statusNormalizado,
            origemNormalizada,
            destinoNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros documentais de escalonamento profissional. Não classificam urgência, risco, prioridade clínica, prognóstico ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("escalation")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewEscalationPersistedResponse>>> ListarEscalations(
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
                x.Categoria.StartsWith(PrefixoEscalation));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearEscalation).ToArray());
    }

    [HttpPost("escalation")]
    public async Task<ActionResult<ProfessionalReviewEscalationPersistedResponse>> CriarEscalation(
        Guid pacienteId,
        CriarProfessionalReviewEscalationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalOrigem = NormalizarObrigatorio(request.ProfissionalOrigem, 160);
        var profissionalDestino = NormalizarObrigatorio(request.ProfissionalDestino, 160);

        if (profissionalOrigem is null)
            return BadRequest(new { message = "Informe o profissional de origem." });

        if (profissionalDestino is null)
            return BadRequest(new { message = "Informe o profissional de destino." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });
        }

        if (request.HandoffRelacionadoId.HasValue &&
            !await HandoffPertencePacienteAsync(pacienteId, request.HandoffRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Handoff relacionado inválido para este paciente." });
        }

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });
        }

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
            Categoria = PrefixoEscalation + "item",
            Conteudo = MontarPayloadEscalation(
                profissionalOrigem,
                profissionalDestino,
                request.ContinuityRelacionadaId,
                request.HandoffRelacionadoId,
                request.DelegationRelacionadaId,
                request.AssignmentRelacionadaId,
                request.ContextoEscalado,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_ESCALATION_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearEscalation(nota));
    }

    [HttpPut("escalation/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewEscalationPersistedResponse>> AtualizarEscalation(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewEscalationRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoEscalation),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalOrigem = NormalizarObrigatorio(request.ProfissionalOrigem, 160);
        var profissionalDestino = NormalizarObrigatorio(request.ProfissionalDestino, 160);

        if (profissionalOrigem is null)
            return BadRequest(new { message = "Informe o profissional de origem." });

        if (profissionalDestino is null)
            return BadRequest(new { message = "Informe o profissional de destino." });

        if (request.ContinuityRelacionadaId.HasValue &&
            !await ContinuityPertencePacienteAsync(pacienteId, request.ContinuityRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Continuity relacionada inválida para este paciente." });
        }

        if (request.HandoffRelacionadoId.HasValue &&
            !await HandoffPertencePacienteAsync(pacienteId, request.HandoffRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Handoff relacionado inválido para este paciente." });
        }

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });
        }

        if (request.AssignmentRelacionadaId.HasValue &&
            !await AssignmentPertencePacienteAsync(pacienteId, request.AssignmentRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Assignment relacionada inválida para este paciente." });
        }

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadEscalation(nota);
        var payloadAtualizado = new EscalationPayload(
            profissionalOrigem,
            profissionalDestino,
            request.ContinuityRelacionadaId,
            request.HandoffRelacionadoId,
            request.DelegationRelacionadaId,
            request.AssignmentRelacionadaId,
            NormalizarOpcional(request.ContextoEscalado, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_ESCALATION_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearEscalation(nota));
    }

    [HttpGet("escalation/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewEscalationHistoryResponse>> HistoricoEscalation(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var escalationExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoEscalation),
                cancellationToken);

        if (!escalationExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_ESCALATION_"));

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

            return new ProfessionalReviewEscalationHistoryItemResponse(
                log.Id,
                id,
                MapearEventoEscalation(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewEscalationHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais de escalonamento profissional. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("escalation/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewEscalationPersistedResponse>> AtualizarStatusEscalation(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewEscalationStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusEscalationValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoEscalation) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadEscalation(nota);

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
                "PROFESSIONAL_REVIEW_ESCALATION_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearEscalation(nota));
    }

    [HttpDelete("escalation/{id:guid}")]
    public async Task<IActionResult> ArquivarEscalation(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoEscalation),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_ESCALATION_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("continuity/closure")]
    public ActionResult<ProfessionalReviewContinuityClosureResponse> FechamentoContinuities()
    {
        var componentes = new[]
        {
            "ContinuityFoundation",
            "ContinuityPersistence",
            "ContinuityStatus",
            "ContinuityHistory",
            "ContinuityFilters",
            "ContinuitySummary"
        };

        return Ok(new ProfessionalReviewContinuityClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaContinuityCompleta",
            "O fechamento descreve apenas disponibilidade estrutural da continuidade profissional. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("continuity/summary")]
    public async Task<ActionResult<ProfessionalReviewContinuitySummaryResponse>> ResumoContinuities(
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
                x.Categoria.StartsWith(PrefixoContinuity))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearContinuity).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var planejados = itens.Count(x => !x.Arquivada && x.Status == "Planejada");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidos = itens.Count(x => !x.Arquivada && x.Status == "Concluida");
        var cancelados = itens.Count(x => !x.Arquivada && x.Status == "Cancelada");

        var porProfissionalSeguimento = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalSeguimento))
            .GroupBy(x => x.ProfissionalSeguimento.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewContinuityProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewContinuitySummaryResponse(
            total,
            ativos,
            planejados,
            emAndamento,
            concluidos,
            cancelados,
            arquivados,
            porProfissionalSeguimento,
            "O resumo apresenta somente agregações documentais da continuidade profissional. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("continuity/search")]
    public async Task<ActionResult<ProfessionalReviewContinuityFiltersResponse>> FiltrarContinuities(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalSeguimento = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusContinuityValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var profissionalNormalizado = NormalizarOpcional(profissionalSeguimento, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoContinuity) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearContinuity)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (profissionalNormalizado is null ||
                    x.ProfissionalSeguimento.Contains(profissionalNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalSeguimento.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoContinuidade?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewContinuityFiltersResponse(
            statusNormalizado,
            profissionalNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar registros documentais de continuidade profissional. Não classificam urgência, risco, prioridade clínica, prognóstico ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("continuity")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewContinuityPersistedResponse>>> ListarContinuities(
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
                x.Categoria.StartsWith(PrefixoContinuity));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearContinuity).ToArray());
    }

    [HttpPost("continuity")]
    public async Task<ActionResult<ProfessionalReviewContinuityPersistedResponse>> CriarContinuity(
        Guid pacienteId,
        CriarProfessionalReviewContinuityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var profissionalSeguimento = NormalizarObrigatorio(request.ProfissionalSeguimento, 160);
        if (profissionalSeguimento is null)
            return BadRequest(new { message = "Informe o profissional de seguimento." });

        if (request.HandoffRelacionadoId.HasValue &&
            !await HandoffPertencePacienteAsync(pacienteId, request.HandoffRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Handoff relacionado inválido para este paciente." });
        }

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });
        }

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
            Categoria = PrefixoContinuity + "item",
            Conteudo = MontarPayloadContinuity(
                profissionalSeguimento,
                request.HandoffRelacionadoId,
                request.DelegationRelacionadaId,
                request.AssignmentRelacionadaId,
                request.ContextoContinuidade,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_CONTINUITY_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearContinuity(nota));
    }

    [HttpPut("continuity/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewContinuityPersistedResponse>> AtualizarContinuity(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewContinuityRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoContinuity),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var profissionalSeguimento = NormalizarObrigatorio(request.ProfissionalSeguimento, 160);
        if (profissionalSeguimento is null)
            return BadRequest(new { message = "Informe o profissional de seguimento." });

        if (request.HandoffRelacionadoId.HasValue &&
            !await HandoffPertencePacienteAsync(pacienteId, request.HandoffRelacionadoId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Handoff relacionado inválido para este paciente." });
        }

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });
        }

        if (request.AssignmentRelacionadaId.HasValue &&
            !await AssignmentPertencePacienteAsync(pacienteId, request.AssignmentRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Assignment relacionada inválida para este paciente." });
        }

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadContinuity(nota);
        var payloadAtualizado = new ContinuityPayload(
            profissionalSeguimento,
            request.HandoffRelacionadoId,
            request.DelegationRelacionadaId,
            request.AssignmentRelacionadaId,
            NormalizarOpcional(request.ContextoContinuidade, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_CONTINUITY_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearContinuity(nota));
    }

    [HttpGet("continuity/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewContinuityHistoryResponse>> HistoricoContinuity(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var continuityExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoContinuity),
                cancellationToken);

        if (!continuityExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_CONTINUITY_"));

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

            return new ProfessionalReviewContinuityHistoryItemResponse(
                log.Id,
                id,
                MapearEventoContinuity(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewContinuityHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais de continuidade profissional. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("continuity/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewContinuityPersistedResponse>> AtualizarStatusContinuity(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewContinuityStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusContinuityValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejada, EmAndamento, Concluida ou Cancelada." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoContinuity) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadContinuity(nota);

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
                "PROFESSIONAL_REVIEW_CONTINUITY_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearContinuity(nota));
    }

    [HttpDelete("continuity/{id:guid}")]
    public async Task<IActionResult> ArquivarContinuity(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoContinuity),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_CONTINUITY_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("handoff/closure")]
    public ActionResult<ProfessionalReviewHandoffClosureResponse> FechamentoHandoffs()
    {
        var componentes = new[]
        {
            "HandoffFoundation",
            "HandoffPersistence",
            "HandoffStatus",
            "HandoffHistory",
            "HandoffFilters",
            "HandoffSummary"
        };

        return Ok(new ProfessionalReviewHandoffClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaHandoffCompleta",
            "O fechamento descreve apenas disponibilidade estrutural dos handoffs profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("handoff/summary")]
    public async Task<ActionResult<ProfessionalReviewHandoffSummaryResponse>> ResumoHandoffs(
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
                x.Categoria.StartsWith(PrefixoHandoff))
            .ToListAsync(cancellationToken);

        var itens = notas.Select(MapearHandoff).ToArray();

        var total = itens.Length;
        var arquivados = itens.Count(x => x.Arquivada);
        var ativos = itens.Count(x => !x.Arquivada);
        var planejados = itens.Count(x => !x.Arquivada && x.Status == "Planejado");
        var emAndamento = itens.Count(x => !x.Arquivada && x.Status == "EmAndamento");
        var concluidos = itens.Count(x => !x.Arquivada && x.Status == "Concluido");
        var cancelados = itens.Count(x => !x.Arquivada && x.Status == "Cancelado");

        var porOrigem = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalOrigem))
            .GroupBy(x => x.ProfissionalOrigem.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewHandoffProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        var porDestino = itens
            .Where(x => !x.Arquivada && !string.IsNullOrWhiteSpace(x.ProfissionalDestino))
            .GroupBy(x => x.ProfissionalDestino.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfessionalReviewHandoffProfissionalResumoResponse(
                g.Key,
                g.Count()))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Profissional)
            .ToArray();

        return Ok(new ProfessionalReviewHandoffSummaryResponse(
            total,
            ativos,
            planejados,
            emAndamento,
            concluidos,
            cancelados,
            arquivados,
            porOrigem,
            porDestino,
            "O resumo apresenta somente agregações documentais dos handoffs profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico ou recomendação e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpGet("handoff/search")]
    public async Task<ActionResult<ProfessionalReviewHandoffFiltersResponse>> FiltrarHandoffs(
        Guid pacienteId,
        [FromQuery] string? status = null,
        [FromQuery] string? profissionalOrigem = null,
        [FromQuery] string? profissionalDestino = null,
        [FromQuery] string? horizonte = null,
        [FromQuery] string? texto = null,
        [FromQuery] bool incluirArquivadas = false,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var statusNormalizado = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (statusNormalizado is not null && !StatusHandoffValido(statusNormalizado))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var origemNormalizada = NormalizarOpcional(profissionalOrigem, 160);
        var destinoNormalizado = NormalizarOpcional(profissionalDestino, 160);
        var horizonteNormalizado = NormalizarOpcional(horizonte, 120);
        var textoNormalizado = NormalizarOpcional(texto, 240);
        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);

        var notas = await db.NotasInternasProfissionais
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoHandoff) &&
                (incluirArquivadas || !x.Arquivada))
            .ToListAsync(cancellationToken);

        var itens = notas
            .Select(MapearHandoff)
            .Where(x =>
                (statusNormalizado is null || x.Status == statusNormalizado) &&
                (origemNormalizada is null ||
                    x.ProfissionalOrigem.Contains(origemNormalizada, StringComparison.OrdinalIgnoreCase)) &&
                (destinoNormalizado is null ||
                    x.ProfissionalDestino.Contains(destinoNormalizado, StringComparison.OrdinalIgnoreCase)) &&
                (horizonteNormalizado is null ||
                    (x.Horizonte?.Contains(horizonteNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (textoNormalizado is null ||
                    x.ProfissionalOrigem.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    x.ProfissionalDestino.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContextoTransferido?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ObservacaoProfissional?.Contains(textoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)))
            .ToArray();

        itens = ordemAsc
            ? itens.OrderBy(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray()
            : itens.OrderByDescending(x => x.AtualizadoEmUtc ?? x.CriadoEmUtc).ToArray();

        return Ok(new ProfessionalReviewHandoffFiltersResponse(
            statusNormalizado,
            origemNormalizada,
            destinoNormalizado,
            horizonteNormalizado,
            textoNormalizado,
            incluirArquivadas,
            ordemAsc ? "asc" : "desc",
            itens.Length,
            itens,
            "Os filtros servem apenas para localizar handoffs profissionais documentados. Não classificam urgência, risco, prioridade clínica, resposta ao tratamento ou necessidade de intervenção e não transferem automaticamente responsabilidade clínica."));
    }

    [HttpGet("handoff")]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalReviewHandoffPersistedResponse>>> ListarHandoffs(
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
                x.Categoria.StartsWith(PrefixoHandoff));

        if (!incluirArquivadas)
            query = query.Where(x => !x.Arquivada);

        var notas = await query
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearHandoff).ToArray());
    }

    [HttpPost("handoff")]
    public async Task<ActionResult<ProfessionalReviewHandoffPersistedResponse>> CriarHandoff(
        Guid pacienteId,
        CriarProfessionalReviewHandoffRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await PacienteExiste(pacienteId, cancellationToken))
            return NotFound();

        var origem = NormalizarObrigatorio(request.ProfissionalOrigem, 160);
        var destino = NormalizarObrigatorio(request.ProfissionalDestino, 160);

        if (origem is null)
            return BadRequest(new { message = "Informe o profissional de origem." });

        if (destino is null)
            return BadRequest(new { message = "Informe o profissional de destino." });

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });
        }

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
            Categoria = PrefixoHandoff + "item",
            Conteudo = MontarPayloadHandoff(
                origem,
                destino,
                request.DelegationRelacionadaId,
                request.AssignmentRelacionadaId,
                request.ContextoTransferido,
                request.Horizonte,
                request.ObservacaoProfissional),
            Arquivada = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.NotasInternasProfissionais.Add(nota);
        Auditar("PROFESSIONAL_REVIEW_HANDOFF_CREATED", nota, null, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearHandoff(nota));
    }

    [HttpPut("handoff/{id:guid}")]
    public async Task<ActionResult<ProfessionalReviewHandoffPersistedResponse>> AtualizarHandoff(
        Guid pacienteId,
        Guid id,
        AtualizarProfessionalReviewHandoffRequest request,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoHandoff),
                cancellationToken);

        if (nota is null)
            return NotFound();

        var origem = NormalizarObrigatorio(request.ProfissionalOrigem, 160);
        var destino = NormalizarObrigatorio(request.ProfissionalDestino, 160);

        if (origem is null)
            return BadRequest(new { message = "Informe o profissional de origem." });

        if (destino is null)
            return BadRequest(new { message = "Informe o profissional de destino." });

        if (request.DelegationRelacionadaId.HasValue &&
            !await DelegationPertencePacienteAsync(pacienteId, request.DelegationRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Delegation relacionada inválida para este paciente." });
        }

        if (request.AssignmentRelacionadaId.HasValue &&
            !await AssignmentPertencePacienteAsync(pacienteId, request.AssignmentRelacionadaId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Assignment relacionada inválida para este paciente." });
        }

        var antes = Snapshot(nota);

        var payloadAtual = LerPayloadHandoff(nota);
        var payloadAtualizado = new HandoffPayload(
            origem,
            destino,
            request.DelegationRelacionadaId,
            request.AssignmentRelacionadaId,
            NormalizarOpcional(request.ContextoTransferido, 2000),
            NormalizarOpcional(request.Horizonte, 120),
            NormalizarOpcional(request.ObservacaoProfissional, 2000),
            payloadAtual.Status,
            payloadAtual.StatusAtualizadoEmUtc);

        nota.Conteudo = JsonSerializer.Serialize(payloadAtualizado);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROFESSIONAL_REVIEW_HANDOFF_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapearHandoff(nota));
    }

    [HttpGet("handoff/{id:guid}/history")]
    public async Task<ActionResult<ProfessionalReviewHandoffHistoryResponse>> HistoricoHandoff(
        Guid pacienteId,
        Guid id,
        [FromQuery] string ordenacao = "desc",
        CancellationToken cancellationToken = default)
    {
        var handoffExiste = await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoHandoff),
                cancellationToken);

        if (!handoffExiste)
            return NotFound();

        var ordemAsc = string.Equals(ordenacao, "asc", StringComparison.OrdinalIgnoreCase);
        var entityId = id.ToString();

        var query = db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Entidade == nameof(NotaInternaProfissional) &&
                x.EntidadeId == entityId &&
                x.Acao.StartsWith("PROFESSIONAL_REVIEW_HANDOFF_"));

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

            return new ProfessionalReviewHandoffHistoryItemResponse(
                log.Id,
                id,
                MapearEventoHandoff(log.Acao),
                autorNome,
                log.UsuarioId,
                log.CreatedAtUtc,
                log.DadosNovosJson ?? log.DadosAnterioresJson);
        }).ToArray();

        return Ok(new ProfessionalReviewHandoffHistoryResponse(
            id,
            itens,
            itens.Length,
            ordemAsc ? "asc" : "desc",
            "O histórico registra eventos documentais do Handoff. Não interpreta evolução clínica, causalidade, urgência, prioridade, risco ou resultado e não transfere automaticamente responsabilidade clínica."));
    }

    [HttpPatch("handoff/{id:guid}/status")]
    public async Task<ActionResult<ProfessionalReviewHandoffPersistedResponse>> AtualizarStatusHandoff(
        Guid pacienteId,
        Guid id,
        ProfessionalReviewHandoffStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? null
            : request.Status.Trim();

        if (!StatusHandoffValido(status))
            return BadRequest(new { message = "Status inválido. Use Planejado, EmAndamento, Concluido ou Cancelado." });

        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoHandoff) &&
                !x.Arquivada,
                cancellationToken);

        if (nota is null)
            return NotFound();

        var antes = Snapshot(nota);
        var payload = LerPayloadHandoff(nota);

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
                "PROFESSIONAL_REVIEW_HANDOFF_STATUS_CHANGED",
                nota,
                antes,
                Snapshot(nota));

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(MapearHandoff(nota));
    }

    [HttpDelete("handoff/{id:guid}")]
    public async Task<IActionResult> ArquivarHandoff(
        Guid pacienteId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var nota = await db.NotasInternasProfissionais
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoHandoff),
                cancellationToken);

        if (nota is null)
            return NotFound();

        if (!nota.Arquivada)
        {
            var antes = Snapshot(nota);
            nota.Arquivada = true;
            nota.UpdatedAtUtc = DateTime.UtcNow;

            Auditar("PROFESSIONAL_REVIEW_HANDOFF_ARCHIVED", nota, antes, Snapshot(nota));
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("delegation/closure")]
    public ActionResult<ProfessionalReviewDelegationClosureResponse> FechamentoDelegations()
    {
        var componentes = new[]
        {
            "DelegationFoundation",
            "DelegationPersistence",
            "DelegationStatus",
            "DelegationHistory",
            "DelegationFilters",
            "DelegationSummary"
        };

        return Ok(new ProfessionalReviewDelegationClosureResponse(
            componentes.Length,
            componentes.Length,
            componentes,
            Array.Empty<string>(),
            "EstruturaDelegationCompleta",
            "O fechamento descreve apenas disponibilidade estrutural das delegações profissionais. Não representa score clínico, risco, urgência, prioridade, prognóstico, recomendação ou decisão terapêutica e não transfere automaticamente responsabilidade clínica."));
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

    private sealed record TeamKnowledgeEffectDecisionReviewPayload(
        string ProfissionalRevisor,
        string RevisaoDocumentada,
        Guid? TeamKnowledgeEffectDecisionRelacionadaId,
        Guid? TeamKnowledgeEffectReviewRelacionadaId,
        Guid? TeamKnowledgeEffectRelacionadoId,
        Guid? TeamKnowledgeApplicationRelacionadaId,
        Guid? TeamKnowledgeRelacionadoId,
        Guid? TeamInsightRelacionadoId,
        Guid? TeamLearningRelacionadoId,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? ContextoRevisao,
        string? BaseObservacionalEvidenciaSuporte,
        string? InterpretacaoProfissional,
        string? ConclusaoDocumental,
        string? NecessidadeAcompanhamentoDocumentada,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamKnowledgeEffectDecisionReview(
        string profissionalRevisor,
        string revisaoDocumentada,
        Guid? teamKnowledgeEffectDecisionRelacionadaId,
        Guid? teamKnowledgeEffectReviewRelacionadaId,
        Guid? teamKnowledgeEffectRelacionadoId,
        Guid? teamKnowledgeApplicationRelacionadaId,
        Guid? teamKnowledgeRelacionadoId,
        Guid? teamInsightRelacionadoId,
        Guid? teamLearningRelacionadoId,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? contextoRevisao,
        string? baseObservacionalEvidenciaSuporte,
        string? interpretacaoProfissional,
        string? conclusaoDocumental,
        string? necessidadeAcompanhamentoDocumentada,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamKnowledgeEffectDecisionReviewPayload(
            profissionalRevisor,
            revisaoDocumentada,
            teamKnowledgeEffectDecisionRelacionadaId,
            teamKnowledgeEffectReviewRelacionadaId,
            teamKnowledgeEffectRelacionadoId,
            teamKnowledgeApplicationRelacionadaId,
            teamKnowledgeRelacionadoId,
            teamInsightRelacionadoId,
            teamLearningRelacionadoId,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(contextoRevisao, 3000),
            NormalizarOpcional(baseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(interpretacaoProfissional, 3000),
            NormalizarOpcional(conclusaoDocumental, 3000),
            NormalizarOpcional(necessidadeAcompanhamentoDocumentada, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamKnowledgeEffectDecisionReviewPersistedResponse MapearTeamKnowledgeEffectDecisionReview(NotaInternaProfissional nota)
    {
        TeamKnowledgeEffectDecisionReviewPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamKnowledgeEffectDecisionReviewPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamKnowledgeEffectDecisionReviewPersistedResponse(
            nota.Id,
            payload?.TeamKnowledgeEffectDecisionRelacionadaId,
            payload?.TeamKnowledgeEffectReviewRelacionadaId,
            payload?.TeamKnowledgeEffectRelacionadoId,
            payload?.TeamKnowledgeApplicationRelacionadaId,
            payload?.TeamKnowledgeRelacionadoId,
            payload?.TeamInsightRelacionadoId,
            payload?.TeamLearningRelacionadoId,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalRevisor ?? "Profissional",
            payload?.Participantes,
            payload?.RevisaoDocumentada ?? nota.Conteudo,
            payload?.ContextoRevisao,
            payload?.BaseObservacionalEvidenciaSuporte,
            payload?.InterpretacaoProfissional,
            payload?.ConclusaoDocumental,
            payload?.NecessidadeAcompanhamentoDocumentada,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static TeamKnowledgeEffectDecisionReviewPayload LerPayloadTeamKnowledgeEffectDecisionReview(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamKnowledgeEffectDecisionReviewPayload>(nota.Conteudo)
                ?? new TeamKnowledgeEffectDecisionReviewPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamKnowledgeEffectDecisionReviewPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamKnowledgeEffectDecisionReviewValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamKnowledgeEffectDecisionPayload(
        string ProfissionalResponsavel,
        string DecisaoDocumentada,
        Guid? TeamKnowledgeEffectReviewRelacionadaId,
        Guid? TeamKnowledgeEffectRelacionadoId,
        Guid? TeamKnowledgeApplicationRelacionadaId,
        Guid? TeamKnowledgeRelacionadoId,
        Guid? TeamInsightRelacionadoId,
        Guid? TeamLearningRelacionadoId,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? ContextoDecisao,
        string? BaseObservacionalEvidenciaSuporte,
        string? JustificativaProfissional,
        string? ResultadoEsperadoDocumentado,
        string? NecessidadeAcompanhamentoDocumentada,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamKnowledgeEffectDecision(
        string profissionalResponsavel,
        string decisaoDocumentada,
        Guid? teamKnowledgeEffectReviewRelacionadaId,
        Guid? teamKnowledgeEffectRelacionadoId,
        Guid? teamKnowledgeApplicationRelacionadaId,
        Guid? teamKnowledgeRelacionadoId,
        Guid? teamInsightRelacionadoId,
        Guid? teamLearningRelacionadoId,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? contextoDecisao,
        string? baseObservacionalEvidenciaSuporte,
        string? justificativaProfissional,
        string? resultadoEsperadoDocumentado,
        string? necessidadeAcompanhamentoDocumentada,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamKnowledgeEffectDecisionPayload(
            profissionalResponsavel,
            decisaoDocumentada,
            teamKnowledgeEffectReviewRelacionadaId,
            teamKnowledgeEffectRelacionadoId,
            teamKnowledgeApplicationRelacionadaId,
            teamKnowledgeRelacionadoId,
            teamInsightRelacionadoId,
            teamLearningRelacionadoId,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(contextoDecisao, 3000),
            NormalizarOpcional(baseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(justificativaProfissional, 3000),
            NormalizarOpcional(resultadoEsperadoDocumentado, 3000),
            NormalizarOpcional(necessidadeAcompanhamentoDocumentada, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse MapearTeamKnowledgeEffectDecision(NotaInternaProfissional nota)
    {
        TeamKnowledgeEffectDecisionPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamKnowledgeEffectDecisionPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamKnowledgeEffectDecisionPersistedResponse(
            nota.Id,
            payload?.TeamKnowledgeEffectReviewRelacionadaId,
            payload?.TeamKnowledgeEffectRelacionadoId,
            payload?.TeamKnowledgeApplicationRelacionadaId,
            payload?.TeamKnowledgeRelacionadoId,
            payload?.TeamInsightRelacionadoId,
            payload?.TeamLearningRelacionadoId,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.DecisaoDocumentada ?? nota.Conteudo,
            payload?.ContextoDecisao,
            payload?.BaseObservacionalEvidenciaSuporte,
            payload?.JustificativaProfissional,
            payload?.ResultadoEsperadoDocumentado,
            payload?.NecessidadeAcompanhamentoDocumentada,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamKnowledgeEffectDecision(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_DECISION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamKnowledgeEffectDecisionPayload LerPayloadTeamKnowledgeEffectDecision(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamKnowledgeEffectDecisionPayload>(nota.Conteudo)
                ?? new TeamKnowledgeEffectDecisionPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamKnowledgeEffectDecisionPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamKnowledgeEffectDecisionValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamKnowledgeEffectReviewPayload(
        string ProfissionalRevisor,
        string RevisaoDocumentada,
        Guid? TeamKnowledgeEffectRelacionadoId,
        Guid? TeamKnowledgeApplicationRelacionadaId,
        Guid? TeamKnowledgeRelacionadoId,
        Guid? TeamInsightRelacionadoId,
        Guid? TeamLearningRelacionadoId,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? ContextoRevisao,
        string? BaseObservacionalEvidenciaSuporte,
        string? InterpretacaoProfissional,
        string? ConclusaoDocumental,
        string? NecessidadeAcompanhamentoDocumentada,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamKnowledgeEffectReview(
        string profissionalRevisor,
        string revisaoDocumentada,
        Guid? teamKnowledgeEffectRelacionadoId,
        Guid? teamKnowledgeApplicationRelacionadaId,
        Guid? teamKnowledgeRelacionadoId,
        Guid? teamInsightRelacionadoId,
        Guid? teamLearningRelacionadoId,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? contextoRevisao,
        string? baseObservacionalEvidenciaSuporte,
        string? interpretacaoProfissional,
        string? conclusaoDocumental,
        string? necessidadeAcompanhamentoDocumentada,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamKnowledgeEffectReviewPayload(
            profissionalRevisor,
            revisaoDocumentada,
            teamKnowledgeEffectRelacionadoId,
            teamKnowledgeApplicationRelacionadaId,
            teamKnowledgeRelacionadoId,
            teamInsightRelacionadoId,
            teamLearningRelacionadoId,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(contextoRevisao, 3000),
            NormalizarOpcional(baseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(interpretacaoProfissional, 3000),
            NormalizarOpcional(conclusaoDocumental, 3000),
            NormalizarOpcional(necessidadeAcompanhamentoDocumentada, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse MapearTeamKnowledgeEffectReview(NotaInternaProfissional nota)
    {
        TeamKnowledgeEffectReviewPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamKnowledgeEffectReviewPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamKnowledgeEffectReviewPersistedResponse(
            nota.Id,
            payload?.TeamKnowledgeEffectRelacionadoId,
            payload?.TeamKnowledgeApplicationRelacionadaId,
            payload?.TeamKnowledgeRelacionadoId,
            payload?.TeamInsightRelacionadoId,
            payload?.TeamLearningRelacionadoId,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalRevisor ?? "Profissional",
            payload?.Participantes,
            payload?.RevisaoDocumentada ?? nota.Conteudo,
            payload?.ContextoRevisao,
            payload?.BaseObservacionalEvidenciaSuporte,
            payload?.InterpretacaoProfissional,
            payload?.ConclusaoDocumental,
            payload?.NecessidadeAcompanhamentoDocumentada,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamKnowledgeEffectReview(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_REVIEW_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamKnowledgeEffectReviewPayload LerPayloadTeamKnowledgeEffectReview(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamKnowledgeEffectReviewPayload>(nota.Conteudo)
                ?? new TeamKnowledgeEffectReviewPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamKnowledgeEffectReviewPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamKnowledgeEffectReviewValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamKnowledgeEffectPayload(
        string ProfissionalResponsavel,
        string EfeitoObservadoDocumentado,
        Guid? TeamKnowledgeApplicationRelacionadaId,
        Guid? TeamKnowledgeRelacionadoId,
        Guid? TeamInsightRelacionadoId,
        Guid? TeamLearningRelacionadoId,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? ContextoObservacao,
        string? BaseObservacionalEvidenciaSuporte,
        string? InterpretacaoProfissional,
        string? ResultadoObservadoDocumentado,
        string? ImpactoPercebidoDocumentado,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamKnowledgeEffect(
        string profissionalResponsavel,
        string efeitoObservadoDocumentado,
        Guid? teamKnowledgeApplicationRelacionadaId,
        Guid? teamKnowledgeRelacionadoId,
        Guid? teamInsightRelacionadoId,
        Guid? teamLearningRelacionadoId,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? contextoObservacao,
        string? baseObservacionalEvidenciaSuporte,
        string? interpretacaoProfissional,
        string? resultadoObservadoDocumentado,
        string? impactoPercebidoDocumentado,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamKnowledgeEffectPayload(
            profissionalResponsavel,
            efeitoObservadoDocumentado,
            teamKnowledgeApplicationRelacionadaId,
            teamKnowledgeRelacionadoId,
            teamInsightRelacionadoId,
            teamLearningRelacionadoId,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(contextoObservacao, 3000),
            NormalizarOpcional(baseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(interpretacaoProfissional, 3000),
            NormalizarOpcional(resultadoObservadoDocumentado, 3000),
            NormalizarOpcional(impactoPercebidoDocumentado, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamKnowledgeEffectPersistedResponse MapearTeamKnowledgeEffect(NotaInternaProfissional nota)
    {
        TeamKnowledgeEffectPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamKnowledgeEffectPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamKnowledgeEffectPersistedResponse(
            nota.Id,
            payload?.TeamKnowledgeApplicationRelacionadaId,
            payload?.TeamKnowledgeRelacionadoId,
            payload?.TeamInsightRelacionadoId,
            payload?.TeamLearningRelacionadoId,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.EfeitoObservadoDocumentado ?? nota.Conteudo,
            payload?.ContextoObservacao,
            payload?.BaseObservacionalEvidenciaSuporte,
            payload?.InterpretacaoProfissional,
            payload?.ResultadoObservadoDocumentado,
            payload?.ImpactoPercebidoDocumentado,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamKnowledgeEffect(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_EFFECT_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamKnowledgeEffectPayload LerPayloadTeamKnowledgeEffect(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamKnowledgeEffectPayload>(nota.Conteudo)
                ?? new TeamKnowledgeEffectPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamKnowledgeEffectPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamKnowledgeEffectValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamKnowledgeApplicationPayload(
        string ProfissionalResponsavel,
        string AplicacaoDocumentada,
        Guid? TeamKnowledgeRelacionadoId,
        Guid? TeamInsightRelacionadoId,
        Guid? TeamLearningRelacionadoId,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? ObjetivoAplicacao,
        string? ContextoAplicacao,
        string? BaseObservacionalEvidenciaSuporte,
        string? InterpretacaoProfissional,
        string? ResultadoEsperadoDocumentado,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamKnowledgeApplication(
        string profissionalResponsavel,
        string aplicacaoDocumentada,
        Guid? teamKnowledgeRelacionadoId,
        Guid? teamInsightRelacionadoId,
        Guid? teamLearningRelacionadoId,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? objetivoAplicacao,
        string? contextoAplicacao,
        string? baseObservacionalEvidenciaSuporte,
        string? interpretacaoProfissional,
        string? resultadoEsperadoDocumentado,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamKnowledgeApplicationPayload(
            profissionalResponsavel,
            aplicacaoDocumentada,
            teamKnowledgeRelacionadoId,
            teamInsightRelacionadoId,
            teamLearningRelacionadoId,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(objetivoAplicacao, 3000),
            NormalizarOpcional(contextoAplicacao, 3000),
            NormalizarOpcional(baseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(interpretacaoProfissional, 3000),
            NormalizarOpcional(resultadoEsperadoDocumentado, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamKnowledgeApplicationPersistedResponse MapearTeamKnowledgeApplication(NotaInternaProfissional nota)
    {
        TeamKnowledgeApplicationPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamKnowledgeApplicationPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamKnowledgeApplicationPersistedResponse(
            nota.Id,
            payload?.TeamKnowledgeRelacionadoId,
            payload?.TeamInsightRelacionadoId,
            payload?.TeamLearningRelacionadoId,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.AplicacaoDocumentada ?? nota.Conteudo,
            payload?.ObjetivoAplicacao,
            payload?.ContextoAplicacao,
            payload?.BaseObservacionalEvidenciaSuporte,
            payload?.InterpretacaoProfissional,
            payload?.ResultadoEsperadoDocumentado,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamKnowledgeApplication(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_APPLICATION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamKnowledgeApplicationPayload LerPayloadTeamKnowledgeApplication(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamKnowledgeApplicationPayload>(nota.Conteudo)
                ?? new TeamKnowledgeApplicationPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamKnowledgeApplicationPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamKnowledgeApplicationValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamKnowledgePayload(
        string ProfissionalResponsavel,
        string ConhecimentoDocumentado,
        Guid? TeamInsightRelacionadoId,
        Guid? TeamLearningRelacionadoId,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? BaseObservacionalEvidenciaSuporte,
        string? InterpretacaoProfissional,
        string? Aplicabilidade,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamKnowledge(
        string profissionalResponsavel,
        string conhecimentoDocumentado,
        Guid? teamInsightRelacionadoId,
        Guid? teamLearningRelacionadoId,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? baseObservacionalEvidenciaSuporte,
        string? interpretacaoProfissional,
        string? aplicabilidade,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamKnowledgePayload(
            profissionalResponsavel,
            conhecimentoDocumentado,
            teamInsightRelacionadoId,
            teamLearningRelacionadoId,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(baseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(interpretacaoProfissional, 3000),
            NormalizarOpcional(aplicabilidade, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamKnowledgePersistedResponse MapearTeamKnowledge(NotaInternaProfissional nota)
    {
        TeamKnowledgePayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamKnowledgePayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamKnowledgePersistedResponse(
            nota.Id,
            payload?.TeamInsightRelacionadoId,
            payload?.TeamLearningRelacionadoId,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.ConhecimentoDocumentado ?? nota.Conteudo,
            payload?.BaseObservacionalEvidenciaSuporte,
            payload?.InterpretacaoProfissional,
            payload?.Aplicabilidade,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamKnowledge(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_KNOWLEDGE_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamKnowledgePayload LerPayloadTeamKnowledge(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamKnowledgePayload>(nota.Conteudo)
                ?? new TeamKnowledgePayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamKnowledgePayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamKnowledgeValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamInsightPayload(
        string ProfissionalResponsavel,
        string InsightDocumentado,
        Guid? TeamLearningRelacionadoId,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? BaseObservacionalEvidenciaSuporte,
        string? InterpretacaoProfissional,
        string? Aplicabilidade,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamInsight(
        string profissionalResponsavel,
        string insightDocumentado,
        Guid? teamLearningRelacionadoId,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? baseObservacionalEvidenciaSuporte,
        string? interpretacaoProfissional,
        string? aplicabilidade,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamInsightPayload(
            profissionalResponsavel,
            insightDocumentado,
            teamLearningRelacionadoId,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(baseObservacionalEvidenciaSuporte, 3000),
            NormalizarOpcional(interpretacaoProfissional, 3000),
            NormalizarOpcional(aplicabilidade, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamInsightPersistedResponse MapearTeamInsight(NotaInternaProfissional nota)
    {
        TeamInsightPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamInsightPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamInsightPersistedResponse(
            nota.Id,
            payload?.TeamLearningRelacionadoId,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.InsightDocumentado ?? nota.Conteudo,
            payload?.BaseObservacionalEvidenciaSuporte,
            payload?.InterpretacaoProfissional,
            payload?.Aplicabilidade,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamInsight(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_INSIGHT_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_INSIGHT_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_INSIGHT_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_INSIGHT_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamInsightPayload LerPayloadTeamInsight(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamInsightPayload>(nota.Conteudo)
                ?? new TeamInsightPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamInsightPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamInsightValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamLearningPayload(
        string ProfissionalResponsavel,
        string AprendizadoDocumentado,
        Guid? TeamOutcomeRelacionadoId,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? EvidenciaBaseObservacional,
        string? Aplicabilidade,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Registrado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamLearning(
        string profissionalResponsavel,
        string aprendizadoDocumentado,
        Guid? teamOutcomeRelacionadoId,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? evidenciaBaseObservacional,
        string? aplicabilidade,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamLearningPayload(
            profissionalResponsavel,
            aprendizadoDocumentado,
            teamOutcomeRelacionadoId,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(evidenciaBaseObservacional, 3000),
            NormalizarOpcional(aplicabilidade, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Registrado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamLearningPersistedResponse MapearTeamLearning(NotaInternaProfissional nota)
    {
        TeamLearningPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamLearningPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamLearningPersistedResponse(
            nota.Id,
            payload?.TeamOutcomeRelacionadoId,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.AprendizadoDocumentado ?? nota.Conteudo,
            payload?.EvidenciaBaseObservacional,
            payload?.Aplicabilidade,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Registrado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamLearning(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_LEARNING_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_LEARNING_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_LEARNING_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_LEARNING_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamLearningPayload LerPayloadTeamLearning(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamLearningPayload>(nota.Conteudo)
                ?? new TeamLearningPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamLearningPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamLearningValido(string? status) =>
        status is "Registrado" or "EmRevisao" or "Consolidado" or "Descartado";

    private sealed record TeamOutcomePayload(
        string ProfissionalResponsavel,
        string ResultadoDocumentado,
        Guid? TeamDecisionRelacionadaId,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? EvidenciaSuporte,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Observado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamOutcome(
        string profissionalResponsavel,
        string resultadoDocumentado,
        Guid? teamDecisionRelacionadaId,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? evidenciaSuporte,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamOutcomePayload(
            profissionalResponsavel,
            resultadoDocumentado,
            teamDecisionRelacionadaId,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(evidenciaSuporte, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Observado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamOutcomePersistedResponse MapearTeamOutcome(NotaInternaProfissional nota)
    {
        TeamOutcomePayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamOutcomePayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamOutcomePersistedResponse(
            nota.Id,
            payload?.TeamDecisionRelacionadaId,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.ResultadoDocumentado ?? nota.Conteudo,
            payload?.EvidenciaSuporte,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Observado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamOutcome(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_OUTCOME_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_OUTCOME_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_OUTCOME_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_OUTCOME_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamOutcomePayload LerPayloadTeamOutcome(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamOutcomePayload>(nota.Conteudo)
                ?? new TeamOutcomePayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamOutcomePayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamOutcomeValido(string? status) =>
        status is "Observado" or "EmAcompanhamento" or "Consolidado" or "Descartado";

    private sealed record TeamDecisionPayload(
        string ProfissionalResponsavel,
        string DecisaoDocumentada,
        Guid? TeamAlignmentRelacionadoId,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? RacionalJustificativa,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamDecision(
        string profissionalResponsavel,
        string decisaoDocumentada,
        Guid? teamAlignmentRelacionadoId,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? racionalJustificativa,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamDecisionPayload(
            profissionalResponsavel,
            decisaoDocumentada,
            teamAlignmentRelacionadoId,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(racionalJustificativa, 3000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamDecisionPersistedResponse MapearTeamDecision(NotaInternaProfissional nota)
    {
        TeamDecisionPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamDecisionPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamDecisionPersistedResponse(
            nota.Id,
            payload?.TeamAlignmentRelacionadoId,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? "Profissional",
            payload?.Participantes,
            payload?.DecisaoDocumentada ?? nota.Conteudo,
            payload?.RacionalJustificativa,
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

    private static string MapearEventoTeamDecision(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_DECISION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_DECISION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_DECISION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_DECISION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamDecisionPayload LerPayloadTeamDecision(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamDecisionPayload>(nota.Conteudo)
                ?? new TeamDecisionPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamDecisionPayload("Profissional", nota.Conteudo, null, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamDecisionValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private sealed record TeamAlignmentPayload(
        string ProfissionalResponsavel,
        Guid? SharedContextRelacionadoId,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? ObjetivoAlinhamento,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadTeamAlignment(
        string profissionalResponsavel,
        Guid? sharedContextRelacionadoId,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? objetivoAlinhamento,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new TeamAlignmentPayload(
            profissionalResponsavel,
            sharedContextRelacionadoId,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(objetivoAlinhamento, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewTeamAlignmentPersistedResponse MapearTeamAlignment(NotaInternaProfissional nota)
    {
        TeamAlignmentPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<TeamAlignmentPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewTeamAlignmentPersistedResponse(
            nota.Id,
            payload?.SharedContextRelacionadoId,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? nota.Conteudo,
            payload?.Participantes,
            payload?.ObjetivoAlinhamento,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoTeamAlignment(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_TEAM_ALIGNMENT_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static TeamAlignmentPayload LerPayloadTeamAlignment(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<TeamAlignmentPayload>(nota.Conteudo)
                ?? new TeamAlignmentPayload(nota.Conteudo, null, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new TeamAlignmentPayload(nota.Conteudo, null, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusTeamAlignmentValido(string? status) =>
        status is "Planejado" or "EmAndamento" or "Concluido" or "Cancelado";

    private sealed record SharedContextPayload(
        string ProfissionalResponsavel,
        Guid? CollaborationRelacionadaId,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? Participantes,
        string? ContextoCompartilhado,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadSharedContext(
        string profissionalResponsavel,
        Guid? collaborationRelacionadaId,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? participantes,
        string? contextoCompartilhado,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new SharedContextPayload(
            profissionalResponsavel,
            collaborationRelacionadaId,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(participantes, 1000),
            NormalizarOpcional(contextoCompartilhado, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewSharedContextPersistedResponse MapearSharedContext(NotaInternaProfissional nota)
    {
        SharedContextPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<SharedContextPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewSharedContextPersistedResponse(
            nota.Id,
            payload?.CollaborationRelacionadaId,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? nota.Conteudo,
            payload?.Participantes,
            payload?.ContextoCompartilhado,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoSharedContext(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_SHARED_CONTEXT_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_SHARED_CONTEXT_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_SHARED_CONTEXT_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_SHARED_CONTEXT_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static SharedContextPayload LerPayloadSharedContext(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<SharedContextPayload>(nota.Conteudo)
                ?? new SharedContextPayload(nota.Conteudo, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new SharedContextPayload(nota.Conteudo, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusSharedContextValido(string? status) =>
        status is "Planejado" or "EmAndamento" or "Concluido" or "Cancelado";

    private sealed record CollaborationPayload(
        string ProfissionalResponsavel,
        Guid? CoordinationRelacionadaId,
        Guid? EscalationRelacionadaId,
        Guid? ContinuityRelacionadaId,
        string? ProfissionaisParticipantes,
        string? ContextoColaboracao,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadCollaboration(
        string profissionalResponsavel,
        Guid? coordinationRelacionadaId,
        Guid? escalationRelacionadaId,
        Guid? continuityRelacionadaId,
        string? profissionaisParticipantes,
        string? contextoColaboracao,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new CollaborationPayload(
            profissionalResponsavel,
            coordinationRelacionadaId,
            escalationRelacionadaId,
            continuityRelacionadaId,
            NormalizarOpcional(profissionaisParticipantes, 1000),
            NormalizarOpcional(contextoColaboracao, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewCollaborationPersistedResponse MapearCollaboration(NotaInternaProfissional nota)
    {
        CollaborationPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<CollaborationPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewCollaborationPersistedResponse(
            nota.Id,
            payload?.CoordinationRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ContinuityRelacionadaId,
            payload?.ProfissionalResponsavel ?? nota.Conteudo,
            payload?.ProfissionaisParticipantes,
            payload?.ContextoColaboracao,
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

    private static string MapearEventoCollaboration(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_COLLABORATION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_COLLABORATION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_COLLABORATION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_COLLABORATION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static CollaborationPayload LerPayloadCollaboration(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<CollaborationPayload>(nota.Conteudo)
                ?? new CollaborationPayload(nota.Conteudo, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new CollaborationPayload(nota.Conteudo, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusCollaborationValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private sealed record CoordinationPayload(
        string ProfissionalCoordenador,
        Guid? AssignmentRelacionadaId,
        Guid? DelegationRelacionadaId,
        Guid? HandoffRelacionadoId,
        Guid? ContinuityRelacionadaId,
        Guid? EscalationRelacionadaId,
        string? ContextoCoordenacao,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadCoordination(
        string profissionalCoordenador,
        Guid? assignmentRelacionadaId,
        Guid? delegationRelacionadaId,
        Guid? handoffRelacionadoId,
        Guid? continuityRelacionadaId,
        Guid? escalationRelacionadaId,
        string? contextoCoordenacao,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new CoordinationPayload(
            profissionalCoordenador,
            assignmentRelacionadaId,
            delegationRelacionadaId,
            handoffRelacionadoId,
            continuityRelacionadaId,
            escalationRelacionadaId,
            NormalizarOpcional(contextoCoordenacao, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewCoordinationPersistedResponse MapearCoordination(NotaInternaProfissional nota)
    {
        CoordinationPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<CoordinationPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewCoordinationPersistedResponse(
            nota.Id,
            payload?.AssignmentRelacionadaId,
            payload?.DelegationRelacionadaId,
            payload?.HandoffRelacionadoId,
            payload?.ContinuityRelacionadaId,
            payload?.EscalationRelacionadaId,
            payload?.ProfissionalCoordenador ?? nota.Conteudo,
            payload?.ContextoCoordenacao,
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

    private static string MapearEventoCoordination(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_COORDINATION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_COORDINATION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_COORDINATION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_COORDINATION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static CoordinationPayload LerPayloadCoordination(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<CoordinationPayload>(nota.Conteudo)
                ?? new CoordinationPayload(nota.Conteudo, null, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new CoordinationPayload(nota.Conteudo, null, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusCoordinationValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private sealed record EscalationPayload(
        string ProfissionalOrigem,
        string ProfissionalDestino,
        Guid? ContinuityRelacionadaId,
        Guid? HandoffRelacionadoId,
        Guid? DelegationRelacionadaId,
        Guid? AssignmentRelacionadaId,
        string? ContextoEscalado,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadEscalation(
        string profissionalOrigem,
        string profissionalDestino,
        Guid? continuityRelacionadaId,
        Guid? handoffRelacionadoId,
        Guid? delegationRelacionadaId,
        Guid? assignmentRelacionadaId,
        string? contextoEscalado,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new EscalationPayload(
            profissionalOrigem,
            profissionalDestino,
            continuityRelacionadaId,
            handoffRelacionadoId,
            delegationRelacionadaId,
            assignmentRelacionadaId,
            NormalizarOpcional(contextoEscalado, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewEscalationPersistedResponse MapearEscalation(NotaInternaProfissional nota)
    {
        EscalationPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<EscalationPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewEscalationPersistedResponse(
            nota.Id,
            payload?.ContinuityRelacionadaId,
            payload?.HandoffRelacionadoId,
            payload?.DelegationRelacionadaId,
            payload?.AssignmentRelacionadaId,
            payload?.ProfissionalOrigem ?? nota.Conteudo,
            payload?.ProfissionalDestino ?? string.Empty,
            payload?.ContextoEscalado,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoEscalation(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_ESCALATION_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_ESCALATION_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_ESCALATION_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_ESCALATION_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static EscalationPayload LerPayloadEscalation(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<EscalationPayload>(nota.Conteudo)
                ?? new EscalationPayload(nota.Conteudo, string.Empty, null, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new EscalationPayload(nota.Conteudo, string.Empty, null, null, null, null, null, null, null);
        }
    }

    private static bool StatusEscalationValido(string? status) =>
        status is "Planejado" or "EmAndamento" or "Concluido" or "Cancelado";

    private sealed record ContinuityPayload(
        string ProfissionalSeguimento,
        Guid? HandoffRelacionadoId,
        Guid? DelegationRelacionadaId,
        Guid? AssignmentRelacionadaId,
        string? ContextoContinuidade,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejada",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadContinuity(
        string profissionalSeguimento,
        Guid? handoffRelacionadoId,
        Guid? delegationRelacionadaId,
        Guid? assignmentRelacionadaId,
        string? contextoContinuidade,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new ContinuityPayload(
            profissionalSeguimento,
            handoffRelacionadoId,
            delegationRelacionadaId,
            assignmentRelacionadaId,
            NormalizarOpcional(contextoContinuidade, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejada",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewContinuityPersistedResponse MapearContinuity(NotaInternaProfissional nota)
    {
        ContinuityPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<ContinuityPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewContinuityPersistedResponse(
            nota.Id,
            payload?.HandoffRelacionadoId,
            payload?.DelegationRelacionadaId,
            payload?.AssignmentRelacionadaId,
            payload?.ProfissionalSeguimento ?? nota.Conteudo,
            payload?.ContextoContinuidade,
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

    private static string MapearEventoContinuity(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_CONTINUITY_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_CONTINUITY_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_CONTINUITY_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_CONTINUITY_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static ContinuityPayload LerPayloadContinuity(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<ContinuityPayload>(nota.Conteudo)
                ?? new ContinuityPayload(nota.Conteudo, null, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new ContinuityPayload(nota.Conteudo, null, null, null, null, null, null);
        }
    }

    private static bool StatusContinuityValido(string? status) =>
        status is "Planejada" or "EmAndamento" or "Concluida" or "Cancelada";

    private sealed record HandoffPayload(
        string ProfissionalOrigem,
        string ProfissionalDestino,
        Guid? DelegationRelacionadaId,
        Guid? AssignmentRelacionadaId,
        string? ContextoTransferido,
        string? Horizonte,
        string? ObservacaoProfissional,
        string Status = "Planejado",
        DateTime? StatusAtualizadoEmUtc = null);

    private static string MontarPayloadHandoff(
        string profissionalOrigem,
        string profissionalDestino,
        Guid? delegationRelacionadaId,
        Guid? assignmentRelacionadaId,
        string? contextoTransferido,
        string? horizonte,
        string? observacaoProfissional)
    {
        var payload = new HandoffPayload(
            profissionalOrigem,
            profissionalDestino,
            delegationRelacionadaId,
            assignmentRelacionadaId,
            NormalizarOpcional(contextoTransferido, 2000),
            NormalizarOpcional(horizonte, 120),
            NormalizarOpcional(observacaoProfissional, 2000),
            "Planejado",
            null);

        return JsonSerializer.Serialize(payload);
    }

    private static ProfessionalReviewHandoffPersistedResponse MapearHandoff(NotaInternaProfissional nota)
    {
        HandoffPayload? payload = null;

        try
        {
            payload = JsonSerializer.Deserialize<HandoffPayload>(nota.Conteudo);
        }
        catch (JsonException)
        {
            // Compatibilidade defensiva: conteúdo legado não deve quebrar a listagem.
        }

        return new ProfessionalReviewHandoffPersistedResponse(
            nota.Id,
            payload?.DelegationRelacionadaId,
            payload?.AssignmentRelacionadaId,
            payload?.ProfissionalOrigem ?? nota.Conteudo,
            payload?.ProfissionalDestino ?? string.Empty,
            payload?.ContextoTransferido,
            payload?.Horizonte,
            payload?.ObservacaoProfissional,
            payload?.Status ?? "Planejado",
            payload?.StatusAtualizadoEmUtc,
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

    private static string MapearEventoHandoff(string acao) =>
        acao switch
        {
            "PROFESSIONAL_REVIEW_HANDOFF_CREATED" => "Criado",
            "PROFESSIONAL_REVIEW_HANDOFF_UPDATED" => "Editado",
            "PROFESSIONAL_REVIEW_HANDOFF_STATUS_CHANGED" => "StatusAlterado",
            "PROFESSIONAL_REVIEW_HANDOFF_ARCHIVED" => "Arquivado",
            _ => "Atualizado"
        };

    private static HandoffPayload LerPayloadHandoff(NotaInternaProfissional nota)
    {
        try
        {
            return JsonSerializer.Deserialize<HandoffPayload>(nota.Conteudo)
                ?? new HandoffPayload(nota.Conteudo, string.Empty, null, null, null, null, null);
        }
        catch (JsonException)
        {
            return new HandoffPayload(nota.Conteudo, string.Empty, null, null, null, null, null);
        }
    }

    private static bool StatusHandoffValido(string? status) =>
        status is "Planejado" or "EmAndamento" or "Concluido" or "Cancelado";

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

    private async Task<bool> TeamKnowledgeEffectDecisionPertencePacienteAsync(
        Guid pacienteId,
        Guid teamKnowledgeEffectDecisionId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamKnowledgeEffectDecisionId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectDecision) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamKnowledgeEffectReviewPertencePacienteAsync(
        Guid pacienteId,
        Guid teamKnowledgeEffectReviewId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamKnowledgeEffectReviewId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffectReview) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamKnowledgeEffectPertencePacienteAsync(
        Guid pacienteId,
        Guid teamKnowledgeEffectId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamKnowledgeEffectId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeEffect) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamKnowledgeApplicationPertencePacienteAsync(
        Guid pacienteId,
        Guid teamKnowledgeApplicationId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamKnowledgeApplicationId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledgeApplication) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamKnowledgePertencePacienteAsync(
        Guid pacienteId,
        Guid teamKnowledgeId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamKnowledgeId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamKnowledge) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamInsightPertencePacienteAsync(
        Guid pacienteId,
        Guid teamInsightId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamInsightId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamInsight) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamLearningPertencePacienteAsync(
        Guid pacienteId,
        Guid teamLearningId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamLearningId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamLearning) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamOutcomePertencePacienteAsync(
        Guid pacienteId,
        Guid teamOutcomeId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamOutcomeId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamOutcome) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamDecisionPertencePacienteAsync(
        Guid pacienteId,
        Guid teamDecisionId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamDecisionId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamDecision) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> TeamAlignmentPertencePacienteAsync(
        Guid pacienteId,
        Guid teamAlignmentId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == teamAlignmentId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoTeamAlignment) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> SharedContextPertencePacienteAsync(
        Guid pacienteId,
        Guid sharedContextId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == sharedContextId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoSharedContext) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> CollaborationPertencePacienteAsync(
        Guid pacienteId,
        Guid collaborationId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == collaborationId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCollaboration) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> CoordinationPertencePacienteAsync(
        Guid pacienteId,
        Guid coordinationId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == coordinationId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoCoordination) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> EscalationPertencePacienteAsync(
        Guid pacienteId,
        Guid escalationId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == escalationId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoEscalation) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> ContinuityPertencePacienteAsync(
        Guid pacienteId,
        Guid continuityId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == continuityId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoContinuity) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> HandoffPertencePacienteAsync(
        Guid pacienteId,
        Guid handoffId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == handoffId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoHandoff) &&
                !x.Arquivada,
                cancellationToken);

    private async Task<bool> DelegationPertencePacienteAsync(
        Guid pacienteId,
        Guid delegationId,
        CancellationToken cancellationToken) =>
        await db.NotasInternasProfissionais
            .AsNoTracking()
            .AnyAsync(x =>
                x.Id == delegationId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.PacienteId == pacienteId &&
                x.Categoria.StartsWith(PrefixoDelegation) &&
                !x.Arquivada,
                cancellationToken);

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
