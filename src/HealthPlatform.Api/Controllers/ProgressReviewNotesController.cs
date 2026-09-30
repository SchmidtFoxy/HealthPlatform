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

        nota.Conteudo = MontarPayloadFollowUp(
            item,
            request.ContextoRelacionado,
            request.HorizonteRevisao,
            request.Responsavel,
            request.ObservacaoFollowUp);
        nota.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("PROGRESS_REVIEW_FOLLOW_UP_UPDATED", nota, antes, Snapshot(nota));

        await db.SaveChangesAsync(cancellationToken);
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
        string? ObservacaoFollowUp);

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
            NormalizarOpcional(observacaoFollowUp, 2000));

        return JsonSerializer.Serialize(payload);
    }

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
            nota.AutorUsuarioId,
            nota.AutorNome,
            nota.CreatedAtUtc,
            nota.UpdatedAtUtc,
            nota.Arquivada);
    }

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
