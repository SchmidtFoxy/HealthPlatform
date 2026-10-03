using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Medico,Nutricionista,Personal")]
[Route("api/operacoes-producao")]
public sealed class ProductionOperationsController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly string _operationsDirectory;

    public ProductionOperationsController(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredDirectory = configuration["ProductionOperations:OperationsDirectory"];

        _operationsDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(environment.ContentRootPath, ".deploy-logs", "operations")
            : Path.GetFullPath(configuredDirectory);
    }

    [HttpGet("deploys")]
    public async Task<ActionResult<ProductionDeployHistoryResponse>> GetDeployHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? q = null,
        [FromQuery] string? version = null,
        [FromQuery] string? status = null,
        [FromQuery] string? mode = null,
        [FromQuery] bool? rollback = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return BadRequest(new { message = "page deve ser maior ou igual a 1." });
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            return BadRequest(new { message = $"pageSize deve ficar entre 1 e {MaxPageSize}." });
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new { message = "from nao pode ser posterior a to." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        IEnumerable<ProductionDeployHistoryItem> filtered = allItems;

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            filtered = filtered.Where(x =>
                x.Version.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.ServedVersion.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(version))
        {
            filtered = filtered.Where(x =>
                x.Version.Contains(version.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            filtered = filtered.Where(x =>
                string.Equals(x.OperationsStatus, status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(mode))
        {
            filtered = filtered.Where(x =>
                string.Equals(x.Mode, mode.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (rollback.HasValue)
        {
            filtered = filtered.Where(x => x.RollbackExecuted == rollback.Value);
        }

        if (from.HasValue)
        {
            filtered = filtered.Where(x => x.RecordedAt >= from.Value);
        }

        if (to.HasValue)
        {
            filtered = filtered.Where(x => x.RecordedAt <= to.Value);
        }

        var ordered = filtered
            .OrderByDescending(x => x.RecordedAt)
            .ThenByDescending(x => x.Version, StringComparer.Ordinal)
            .ToList();

        var totalItems = ordered.Count;
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new ProductionDeployHistoryResponse(
            page,
            pageSize,
            totalItems,
            totalPages,
            items));
    }

    [HttpGet("deploys/support-timeline/evidence-pack")]
    public async Task<ActionResult<ProductionSupportEvidencePackResponse>> GetSupportEvidencePack(
        [FromQuery] string version,
        [FromQuery] DateTimeOffset recordedAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return BadRequest(new { message = "version e obrigatoria." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var item = allItems.FirstOrDefault(x =>
            string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase) &&
            x.RecordedAt.Equals(recordedAt));

        if (item is null)
        {
            return NotFound(new { message = "Evento operacional nao encontrado no historico." });
        }

        var state = IsHealthy(item)
            ? "healthy"
            : item.RollbackExecuted
                ? "rollback"
                : item.ClosureComplete
                    ? "attention"
                    : "pending";

        static string Yn(bool value) => value ? "sim" : "nao";
        static string Safe(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Replace("\r", " ", StringComparison.Ordinal)
                       .Replace("\n", " ", StringComparison.Ordinal)
                       .Replace("|", "/", StringComparison.Ordinal);

        var gates = new[]
        {
            new ProductionSupportEventGate("backup", "Backup PostgreSQL", item.BackupValidated ? "passed" : "pending",
                item.BackupValidated ? "Backup validado antes das mutacoes." : "Backup nao confirmado neste evento."),
            new ProductionSupportEventGate("migration-safety", "Migration Safety",
                item.MigrationSafetyApproved && item.MigrationHashMatched && !item.DestructiveMigrationsAllowed ? "passed" : "attention",
                item.MigrationSafetyApproved && item.MigrationHashMatched && !item.DestructiveMigrationsAllowed
                    ? "Conjunto aprovado, hash conferido e migrations destrutivas bloqueadas."
                    : "Nem todos os invariantes de migration safety foram confirmados."),
            new ProductionSupportEventGate("promotion", "Promocao", item.PromotionApplied ? "applied" : "not-applied",
                item.PromotionApplied ? "Release promovida no ciclo." : "Evento sem promocao aplicada."),
            new ProductionSupportEventGate("runtime", "Runtime", item.RuntimeHealthy ? "passed" : "attention",
                item.RuntimeHealthy ? "Runtime respondeu saudavel." : "Runtime nao foi confirmado como saudavel."),
            new ProductionSupportEventGate("version", "Version Verification", item.VersionHealthy ? "passed" : "attention",
                item.VersionHealthy ? $"Versao servida confirmada: {Safe(item.ServedVersion)}." : "Versao servida nao foi confirmada no ciclo."),
            new ProductionSupportEventGate("rollback", "Rollback", item.RollbackExecuted ? "executed" : "not-required",
                item.RollbackExecuted ? "Rollback registrado neste ciclo." : "Rollback nao foi necessario."),
            new ProductionSupportEventGate("recovery", "Recovery Audit", item.RecoveryAuditComplete ? "passed" : "pending",
                item.RecoveryAuditComplete ? "Recovery audit concluido." : "Recovery audit nao concluido."),
            new ProductionSupportEventGate("closure", "Closure", item.ClosureComplete ? "passed" : "pending",
                item.ClosureComplete ? "Closure gate concluido." : "Closure gate pendente.")
        };

        var lines = new List<string>
        {
            "# AESYN Performance - Support Evidence Pack",
            "",
            $"Evento: {Safe(item.Version)}",
            $"RecordedAt: {item.RecordedAt:O}",
            $"Estado: {state}",
            $"Modo: {Safe(item.Mode)}",
            $"Status operacional: {Safe(item.OperationsStatus)}",
            $"Versao servida: {Safe(item.ServedVersion)}",
            "",
            "## Fatos publicos",
            $"Runtime saudavel: {Yn(item.RuntimeHealthy)}",
            $"Versao confirmada: {Yn(item.VersionHealthy)}",
            $"Rollback executado: {Yn(item.RollbackExecuted)}",
            $"Recovery audit concluido: {Yn(item.RecoveryAuditComplete)}",
            $"Closure concluido: {Yn(item.ClosureComplete)}",
            "",
            "## Gates do ciclo"
        };

        foreach (var gate in gates)
        {
            lines.Add($"- {Safe(gate.Label)} [{Safe(gate.State)}]: {Safe(gate.Description)}");
        }

        lines.Add("");
        lines.Add("## Limites e seguranca");
        lines.Add("Pacote textual somente leitura para suporte tecnico.");
        lines.Add("Nao inclui hash de backup, staging path, target/host, caminhos internos, tokens, segredos ou credenciais.");
        lines.Add("Nao executa, recomenda, autoriza ou automatiza deploy, promocao ou rollback.");

        var content = string.Join(Environment.NewLine, lines);
        var fileName = $"aesyn-support-evidence-{Safe(item.Version)}-{item.RecordedAt:yyyyMMdd-HHmmss}.md";

        return Ok(new ProductionSupportEvidencePackResponse(
            fileName,
            "text/markdown;charset=utf-8",
            content,
            item.Version,
            item.RecordedAt,
            state,
            gates.Length));
    }

    [HttpGet("deploys/support-timeline/detail")]
    public async Task<ActionResult<ProductionSupportEventDetailResponse>> GetSupportTimelineDetail(
        [FromQuery] string version,
        [FromQuery] DateTimeOffset recordedAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return BadRequest(new { message = "version e obrigatoria." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var item = allItems.FirstOrDefault(x =>
            string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase) &&
            x.RecordedAt.Equals(recordedAt));

        if (item is null)
        {
            return NotFound(new { message = "Evento operacional nao encontrado no historico." });
        }

        var state = IsHealthy(item)
            ? "healthy"
            : item.RollbackExecuted
                ? "rollback"
                : item.ClosureComplete
                    ? "attention"
                    : "pending";

        var gates = new[]
        {
            new ProductionSupportEventGate(
                "backup",
                "Backup PostgreSQL",
                item.BackupValidated ? "passed" : "pending",
                item.BackupValidated ? "Backup validado antes das mutacoes." : "Backup nao confirmado neste evento."),

            new ProductionSupportEventGate(
                "migration-safety",
                "Migration Safety",
                item.MigrationSafetyApproved && item.MigrationHashMatched && !item.DestructiveMigrationsAllowed ? "passed" : "attention",
                item.MigrationSafetyApproved && item.MigrationHashMatched && !item.DestructiveMigrationsAllowed
                    ? "Conjunto aprovado, hash conferido e migrations destrutivas bloqueadas."
                    : "Nem todos os invariantes de migration safety foram confirmados."),

            new ProductionSupportEventGate(
                "promotion",
                "Promocao",
                item.PromotionApplied ? "applied" : "not-applied",
                item.PromotionApplied ? "Release promovida no ciclo." : "Evento sem promocao aplicada."),

            new ProductionSupportEventGate(
                "runtime",
                "Runtime",
                item.RuntimeHealthy ? "passed" : "attention",
                item.RuntimeHealthy ? "Runtime respondeu saudavel." : "Runtime nao foi confirmado como saudavel."),

            new ProductionSupportEventGate(
                "version",
                "Version Verification",
                item.VersionHealthy ? "passed" : "attention",
                item.VersionHealthy
                    ? $"Versao servida confirmada: {item.ServedVersion}."
                    : "Versao servida nao foi confirmada no ciclo."),

            new ProductionSupportEventGate(
                "rollback",
                "Rollback",
                item.RollbackExecuted ? "executed" : "not-required",
                item.RollbackExecuted ? "Rollback registrado neste ciclo." : "Rollback nao foi necessario."),

            new ProductionSupportEventGate(
                "recovery",
                "Recovery Audit",
                item.RecoveryAuditComplete ? "passed" : "pending",
                item.RecoveryAuditComplete ? "Recovery audit concluido." : "Recovery audit nao concluido."),

            new ProductionSupportEventGate(
                "closure",
                "Closure",
                item.ClosureComplete ? "passed" : "pending",
                item.ClosureComplete ? "Closure gate concluido." : "Closure gate pendente.")
        };

        var eventData = new ProductionSupportEventPublicData(
            item.Version,
            item.RecordedAt,
            item.Mode,
            item.OperationsStatus,
            state,
            item.RuntimeHealthy,
            item.VersionHealthy,
            item.ServedVersion,
            item.RollbackExecuted,
            item.RecoveryAuditComplete,
            item.ClosureComplete);

        return Ok(new ProductionSupportEventDetailResponse(
            eventData,
            gates,
            "Detalhe operacional somente leitura baseado em dados publicos seguros.",
            "Nao inclui hash de backup, staging path, target/host, caminhos internos, tokens, segredos ou credenciais.",
            "O detalhe explica evidencias do ciclo e nao executa, recomenda, autoriza ou automatiza deploy, promocao ou rollback."));
    }

    [HttpGet("deploys/support-timeline")]
    public async Task<ActionResult<ProductionSupportTimelineResponse>> GetSupportTimeline(
        [FromQuery] int days = 30,
        [FromQuery] int limit = 40,
        CancellationToken cancellationToken = default)
    {
        if (days < 1 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 1 e 365." });
        }

        if (limit < 1 || limit > 100)
        {
            return BadRequest(new { message = "limit deve ficar entre 1 e 100." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderByDescending(x => x.RecordedAt)
            .Take(limit)
            .ToList();

        var events = new List<ProductionSupportTimelineItem>();

        foreach (var item in periodItems)
        {
            var state = IsHealthy(item)
                ? "healthy"
                : item.RollbackExecuted
                    ? "rollback"
                    : item.ClosureComplete
                        ? "attention"
                        : "pending";

            var title = item.RollbackExecuted
                ? $"Rollback registrado em {item.Version}"
                : string.Equals(item.Mode, "validate-only", StringComparison.OrdinalIgnoreCase)
                    ? $"Validacao operacional {item.Version}"
                    : $"Deploy operacional {item.Version}";

            var description =
                $"Modo {item.Mode}; status {item.OperationsStatus}; versao servida {item.ServedVersion}; " +
                $"runtime {(item.RuntimeHealthy ? "saudavel" : "nao confirmado")}; " +
                $"closure {(item.ClosureComplete ? "concluido" : "pendente")}.";

            events.Add(new ProductionSupportTimelineItem(
                item.RecordedAt,
                item.Version,
                item.Mode,
                state,
                title,
                description,
                item.RuntimeHealthy,
                item.VersionHealthy,
                item.RollbackExecuted,
                item.RecoveryAuditComplete,
                item.ClosureComplete));
        }

        return Ok(new ProductionSupportTimelineResponse(
            days,
            from,
            now,
            limit,
            events.Count,
            events,
            "Timeline operacional somente leitura baseada em evidencias publicas seguras.",
            "Nao inclui hash de backup, staging path, target/host, caminhos internos, tokens, segredos ou credenciais."));
    }

    [HttpGet("deploys/support-handoff")]
    public async Task<ActionResult<ProductionSupportHandoffResponse>> GetSupportHandoff(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 2 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 2 e 365." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderByDescending(x => x.RecordedAt)
            .ToList();

        var latest = periodItems.FirstOrDefault();
        var healthyDeploys = periodItems.Count(IsHealthy);
        var rollbackDeploys = periodItems.Count(x => x.RollbackExecuted);
        var healthyPercentage = Percentage(healthyDeploys, periodItems.Count);
        var currentHealthyStreak = 0;

        foreach (var item in periodItems)
        {
            if (!IsHealthy(item))
            {
                break;
            }

            currentHealthyStreak++;
        }

        var state =
            latest is null ? "no-data" :
            IsHealthy(latest) ? "healthy" :
            latest.RollbackExecuted ? "rollback" :
            "attention";

        var handoffId = $"OPS-{now:yyyyMMddHHmm}-{days}D";

        var checklist = new List<ProductionSupportChecklistItem>
        {
            new("confirm-window", "Confirmar a janela analisada e o horário do snapshot.", true),
            new("review-latest", "Revisar versão, modo, status, runtime e closure do último ciclo.", latest is not null),
            new("review-health", "Comparar quantidade de ciclos saudáveis e rollbacks dentro da janela.", periodItems.Count > 0),
            new("review-version", "Confirmar se a versão servida corresponde à última release registrada.", latest is not null && latest.VersionHealthy),
            new("review-recovery", "Verificar se recovery audit e closure estão concluídos no último ciclo.", latest is not null && latest.RecoveryAuditComplete && latest.ClosureComplete),
            new("escalate-only-with-evidence", "Escalar para infraestrutura somente com evidências públicas e sem incluir segredos ou caminhos internos.", true)
        };

        var facts = new List<ProductionSupportFact>
        {
            new("window", "Janela", $"{days} dias ({from:O} ate {now:O})"),
            new("state", "Estado", state),
            new("health", "Saude", $"{healthyDeploys}/{periodItems.Count} saudavel(is) ({healthyPercentage}%)."),
            new("rollbacks", "Rollbacks", rollbackDeploys.ToString()),
            new("streak", "Sequencia saudavel", currentHealthyStreak.ToString())
        };

        if (latest is not null)
        {
            facts.Add(new ProductionSupportFact(
                "latest",
                "Ultimo ciclo",
                $"{latest.Version} | {latest.Mode} | {latest.OperationsStatus} | servido {latest.ServedVersion} | {latest.RecordedAt:O}."));
        }

        var summary = latest is null
            ? $"AESYN Support Handoff {handoffId} | janela {days} dias | sem ciclos operacionais registrados."
            : $"AESYN Support Handoff {handoffId} | janela {days} dias | estado {state} | ultimo ciclo {latest.Version} | servido {latest.ServedVersion} | saudaveis {healthyDeploys}/{periodItems.Count} ({healthyPercentage}%) | rollbacks {rollbackDeploys} | streak {currentHealthyStreak}.";

        return Ok(new ProductionSupportHandoffResponse(
            handoffId,
            days,
            from,
            now,
            state,
            latest?.Version,
            latest?.RecordedAt,
            latest?.ServedVersion,
            summary,
            facts,
            checklist,
            "Handoff tecnico somente leitura para suporte. Nao inclui hash de backup, staging path, target/host, caminhos internos, tokens, segredos ou credenciais.",
            "O handoff organiza evidencias e checklist; nao executa, recomenda, autoriza ou automatiza deploy, promocao ou rollback."));
    }

    [HttpGet("deploys/support-context")]
    public async Task<ActionResult<ProductionSupportContextResponse>> GetSupportContext(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 2 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 2 e 365." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var midpoint = from.AddTicks((now - from).Ticks / 2);

        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderByDescending(x => x.RecordedAt)
            .ToList();

        var latest = periodItems.FirstOrDefault();
        var healthyDeploys = periodItems.Count(IsHealthy);
        var rollbackDeploys = periodItems.Count(x => x.RollbackExecuted);
        var applyDeploys = periodItems.Count(x =>
            string.Equals(x.Mode, "apply", StringComparison.OrdinalIgnoreCase));
        var validateOnlyDeploys = periodItems.Count(x =>
            string.Equals(x.Mode, "validate-only", StringComparison.OrdinalIgnoreCase));
        var healthyPercentage = Percentage(healthyDeploys, periodItems.Count);

        var currentHealthyStreak = 0;
        foreach (var item in periodItems)
        {
            if (!IsHealthy(item))
            {
                break;
            }

            currentHealthyStreak++;
        }

        var previousHalf = periodItems.Where(x => x.RecordedAt < midpoint).ToList();
        var recentHalf = periodItems.Where(x => x.RecordedAt >= midpoint).ToList();
        var previousHealthyPercentage = Percentage(previousHalf.Count(IsHealthy), previousHalf.Count);
        var recentHealthyPercentage = Percentage(recentHalf.Count(IsHealthy), recentHalf.Count);
        var healthyPercentageDelta = Math.Round(
            recentHealthyPercentage - previousHealthyPercentage,
            1,
            MidpointRounding.AwayFromZero);

        var stabilityDirection =
            healthyPercentageDelta >= 10m ? "improving" :
            healthyPercentageDelta <= -10m ? "degrading" :
            "stable";

        var lastRollback = periodItems.FirstOrDefault(x => x.RollbackExecuted);
        var lastHealthyApply = periodItems.FirstOrDefault(x =>
            IsHealthy(x) &&
            string.Equals(x.Mode, "apply", StringComparison.OrdinalIgnoreCase));

        var supportState =
            latest is null ? "no-data" :
            IsHealthy(latest) && stabilityDirection != "degrading" ? "healthy" :
            latest.RollbackExecuted ? "rollback" :
            "attention";

        ProductionSupportLatestCycle? latestCycle = latest is null
            ? null
            : new ProductionSupportLatestCycle(
                latest.Version,
                latest.RecordedAt,
                latest.Mode,
                latest.OperationsStatus,
                latest.RuntimeHealthy,
                latest.VersionHealthy,
                latest.ServedVersion,
                latest.RollbackExecuted,
                latest.RecoveryAuditComplete,
                latest.ClosureComplete);

        var signals = new ProductionSupportSignals(
            currentHealthyStreak,
            stabilityDirection,
            healthyPercentageDelta,
            lastRollback?.Version,
            lastRollback?.RecordedAt,
            lastHealthyApply?.Version,
            lastHealthyApply?.RecordedAt);

        var trends = new ProductionSupportTrends(
            periodItems.Count,
            healthyDeploys,
            healthyPercentage,
            rollbackDeploys,
            applyDeploys,
            validateOnlyDeploys,
            previousHealthyPercentage,
            recentHealthyPercentage);

        var facts = new List<ProductionSupportFact>
        {
            new("window", "Janela analisada", $"{days} dias ({from:O} ate {now:O})"),
            new("health", "Saude operacional", $"{healthyDeploys}/{periodItems.Count} ciclo(s) saudavel(is) ({healthyPercentage}%)."),
            new("modes", "Modos de execucao", $"{applyDeploys} apply / {validateOnlyDeploys} validate-only."),
            new("stability", "Estabilidade", $"{stabilityDirection}; delta {healthyPercentageDelta} p.p. entre metade recente e anterior."),
            new("streak", "Sequencia saudavel", $"{currentHealthyStreak} ciclo(s) consecutivo(s).")
        };

        if (latest is not null)
        {
            facts.Add(new ProductionSupportFact(
                "latest",
                "Ultimo ciclo",
                $"{latest.Version} em {latest.RecordedAt:O}; modo {latest.Mode}; status {latest.OperationsStatus}; versao servida {latest.ServedVersion}."));
        }

        if (lastRollback is not null)
        {
            facts.Add(new ProductionSupportFact(
                "rollback",
                "Ultimo rollback",
                $"{lastRollback.Version} em {lastRollback.RecordedAt:O}."));
        }

        return Ok(new ProductionSupportContextResponse(
            days,
            from,
            midpoint,
            now,
            supportState,
            latestCycle,
            trends,
            signals,
            facts,
            "Contexto tecnico somente leitura para suporte. Nao inclui hash de backup, staging path, target/host, caminhos internos, tokens, segredos ou credenciais.",
            "O contexto nao executa, recomenda, autoriza ou automatiza deploy, promocao ou rollback."));
    }

    [HttpGet("deploys/audit-snapshot")]
    public async Task<ActionResult<ProductionAuditSnapshotResponse>> GetAuditSnapshot(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 2 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 2 e 365." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderByDescending(x => x.RecordedAt)
            .ToList();

        var latest = periodItems.FirstOrDefault();
        var healthyDeploys = periodItems.Count(IsHealthy);
        var rollbackDeploys = periodItems.Count(x => x.RollbackExecuted);
        var healthyPercentage = Percentage(healthyDeploys, periodItems.Count);

        var currentHealthyStreak = 0;
        foreach (var item in periodItems)
        {
            if (!IsHealthy(item))
            {
                break;
            }

            currentHealthyStreak++;
        }

        var lastRollback = periodItems.FirstOrDefault(x => x.RollbackExecuted);

        var state =
            latest is null ? "no-data" :
            IsHealthy(latest) ? "healthy" :
            latest.RollbackExecuted ? "rollback" :
            "attention";

        var indicators = new ProductionAuditSnapshotIndicators(
            periodItems.Count,
            healthyDeploys,
            healthyPercentage,
            rollbackDeploys,
            currentHealthyStreak);

        ProductionAuditSnapshotLatestCycle? latestCycle = latest is null
            ? null
            : new ProductionAuditSnapshotLatestCycle(
                latest.Version,
                latest.RecordedAt,
                latest.Mode,
                latest.OperationsStatus,
                latest.RuntimeHealthy,
                latest.VersionHealthy,
                latest.ServedVersion,
                latest.RollbackExecuted,
                latest.ClosureComplete);

        var summary = latest is null
            ? $"AESYN Production Snapshot | janela {days} dias | sem ciclos operacionais registrados."
            : $"AESYN Production Snapshot | janela {days} dias | estado {state} | ultimo ciclo {latest.Version} em {latest.RecordedAt:O} | saudaveis {healthyDeploys}/{periodItems.Count} ({healthyPercentage}%) | rollbacks {rollbackDeploys} | streak {currentHealthyStreak}.";

        return Ok(new ProductionAuditSnapshotResponse(
            days,
            from,
            now,
            state,
            indicators,
            latestCycle,
            lastRollback?.Version,
            lastRollback?.RecordedAt,
            summary,
            "Snapshot tecnico somente leitura. Nao inclui hash de backup, staging path, target/host, caminhos internos, tokens, segredos ou credenciais."));
    }

    [HttpGet("deploys/reliability/export")]
    public async Task<ActionResult<ProductionReliabilityExportResponse>> GetReliabilityExport(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 2 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 2 e 365." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var midpoint = from.AddTicks((now - from).Ticks / 2);
        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderByDescending(x => x.RecordedAt)
            .ToList();

        var previousHalf = periodItems.Where(x => x.RecordedAt < midpoint).ToList();
        var recentHalf = periodItems.Where(x => x.RecordedAt >= midpoint).ToList();

        var previousHealthy = previousHalf.Count(IsHealthy);
        var recentHealthy = recentHalf.Count(IsHealthy);
        var previousHealthyPercentage = Percentage(previousHealthy, previousHalf.Count);
        var recentHealthyPercentage = Percentage(recentHealthy, recentHalf.Count);
        var healthyPercentageDelta = Math.Round(
            recentHealthyPercentage - previousHealthyPercentage,
            1,
            MidpointRounding.AwayFromZero);

        var currentHealthyStreak = 0;
        foreach (var item in periodItems)
        {
            if (!IsHealthy(item))
            {
                break;
            }

            currentHealthyStreak++;
        }

        var lastRollback = periodItems.FirstOrDefault(x => x.RollbackExecuted);
        var lastHealthyApply = periodItems.FirstOrDefault(x =>
            IsHealthy(x) &&
            string.Equals(x.Mode, "apply", StringComparison.OrdinalIgnoreCase));

        var stabilityDirection =
            healthyPercentageDelta >= 10m ? "improving" :
            healthyPercentageDelta <= -10m ? "degrading" :
            "stable";

        static string Yn(bool value) => value ? "sim" : "nao";
        static string Safe(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "-"
                : value.Replace("\r", " ", StringComparison.Ordinal)
                       .Replace("\n", " ", StringComparison.Ordinal)
                       .Replace("|", "/", StringComparison.Ordinal);

        var lines = new List<string>
        {
            "# AESYN Performance - Production Reliability Export",
            "",
            $"Gerado em: {now:O}",
            $"Janela: {from:O} ate {now:O} ({days} dias)",
            "",
            "## Sinais",
            $"Sequencia saudavel atual: {currentHealthyStreak}",
            $"Ultimo rollback: {(lastRollback is null ? "nenhum" : $"{Safe(lastRollback.Version)} em {lastRollback.RecordedAt:O}")}",
            $"Ultimo apply saudavel: {(lastHealthyApply is null ? "nenhum" : $"{Safe(lastHealthyApply.Version)} em {lastHealthyApply.RecordedAt:O}")}",
            $"Direcao de estabilidade: {stabilityDirection}",
            $"Delta saudavel: {healthyPercentageDelta} p.p.",
            "",
            "## Janelas comparativas",
            $"Anterior: {previousHealthy}/{previousHalf.Count} saudavel(is) ({previousHealthyPercentage}%) - {from:O} ate {midpoint:O}",
            $"Recente: {recentHealthy}/{recentHalf.Count} saudavel(is) ({recentHealthyPercentage}%) - {midpoint:O} ate {now:O}",
            "",
            "## Historico operacional seguro",
            "| RecordedAt | Version | Mode | Status | Runtime | VersionHealthy | ServedVersion | Rollback | Closure |",
            "| --- | --- | --- | --- | --- | --- | --- | --- | --- |"
        };

        foreach (var item in periodItems)
        {
            lines.Add(
                $"| {item.RecordedAt:O} | {Safe(item.Version)} | {Safe(item.Mode)} | {Safe(item.OperationsStatus)} | {Yn(item.RuntimeHealthy)} | {Yn(item.VersionHealthy)} | {Safe(item.ServedVersion)} | {Yn(item.RollbackExecuted)} | {Yn(item.ClosureComplete)} |");
        }

        lines.Add("");
        lines.Add("## Limites e seguranca");
        lines.Add("Este arquivo e somente leitura e serve para auditoria tecnica.");
        lines.Add("Nao inclui hash de backup, staging path, target/host, caminhos internos, tokens, segredos ou credenciais.");
        lines.Add("Nao executa, recomenda, autoriza ou automatiza deploy, promocao ou rollback.");

        var content = string.Join(Environment.NewLine, lines);
        var fileName = $"aesyn-production-reliability-{now:yyyyMMdd-HHmmss}.md";

        return Ok(new ProductionReliabilityExportResponse(
            fileName,
            "text/markdown;charset=utf-8",
            content,
            days,
            periodItems.Count,
            now));
    }

    [HttpGet("deploys/reliability/detail")]
    public async Task<ActionResult<ProductionReliabilityDetailResponse>> GetReliabilityDetail(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 2 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 2 e 365." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var midpoint = from.AddTicks((now - from).Ticks / 2);

        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderByDescending(x => x.RecordedAt)
            .ToList();

        var previousHalf = periodItems.Where(x => x.RecordedAt < midpoint).ToList();
        var recentHalf = periodItems.Where(x => x.RecordedAt >= midpoint).ToList();

        var previousHealthy = previousHalf.Count(IsHealthy);
        var recentHealthy = recentHalf.Count(IsHealthy);
        var previousHealthyPercentage = Percentage(previousHealthy, previousHalf.Count);
        var recentHealthyPercentage = Percentage(recentHealthy, recentHalf.Count);
        var healthyPercentageDelta = Math.Round(
            recentHealthyPercentage - previousHealthyPercentage,
            1,
            MidpointRounding.AwayFromZero);

        var currentHealthyStreak = 0;
        foreach (var item in periodItems)
        {
            if (!IsHealthy(item))
            {
                break;
            }

            currentHealthyStreak++;
        }

        var lastRollback = periodItems.FirstOrDefault(x => x.RollbackExecuted);
        var lastHealthyApply = periodItems.FirstOrDefault(x =>
            IsHealthy(x) &&
            string.Equals(x.Mode, "apply", StringComparison.OrdinalIgnoreCase));

        var stabilityDirection =
            healthyPercentageDelta >= 10m ? "improving" :
            healthyPercentageDelta <= -10m ? "degrading" :
            "stable";

        var explanations = new[]
        {
            new ProductionReliabilityExplanation(
                "healthy-streak",
                "Sequencia saudavel",
                $"Conta ciclos consecutivos a partir do deploy mais recente ate o primeiro ciclo que nao atende ao criterio saudavel. Resultado atual: {currentHealthyStreak}.",
                "Informativo"),

            new ProductionReliabilityExplanation(
                "last-rollback",
                "Ultimo rollback",
                lastRollback is null
                    ? "Nenhum rollback foi registrado dentro da janela selecionada."
                    : $"Rollback mais recente na janela: {lastRollback.Version} em {lastRollback.RecordedAt:O}.",
                "Informativo"),

            new ProductionReliabilityExplanation(
                "last-healthy-apply",
                "Ultimo apply saudavel",
                lastHealthyApply is null
                    ? "Nenhum ciclo apply saudavel foi encontrado dentro da janela selecionada."
                    : $"Apply saudavel mais recente: {lastHealthyApply.Version} em {lastHealthyApply.RecordedAt:O}.",
                "Informativo"),

            new ProductionReliabilityExplanation(
                "stability",
                "Direcao de estabilidade",
                $"Compara a taxa saudavel da metade recente ({recentHealthyPercentage}%) com a metade anterior ({previousHealthyPercentage}%). Delta: {healthyPercentageDelta} p.p. Limiar: >= +10 improving; <= -10 degrading; entre os limites stable.",
                "Informativo")
        };

        return Ok(new ProductionReliabilityDetailResponse(
            days,
            from,
            midpoint,
            now,
            new ProductionReliabilityWindow(
                "previous",
                from,
                midpoint,
                previousHalf.Count,
                previousHealthy,
                previousHealthyPercentage),
            new ProductionReliabilityWindow(
                "recent",
                midpoint,
                now,
                recentHalf.Count,
                recentHealthy,
                recentHealthyPercentage),
            currentHealthyStreak,
            lastRollback?.Version,
            lastRollback?.RecordedAt,
            lastHealthyApply?.Version,
            lastHealthyApply?.RecordedAt,
            healthyPercentageDelta,
            stabilityDirection,
            explanations));
    }

    [HttpGet("deploys/reliability")]
    public async Task<ActionResult<ProductionReliabilitySignalsResponse>> GetReliabilitySignals(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 2 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 2 e 365." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderByDescending(x => x.RecordedAt)
            .ToList();

        var currentHealthyStreak = 0;
        foreach (var item in periodItems)
        {
            if (!IsHealthy(item))
            {
                break;
            }

            currentHealthyStreak++;
        }

        var lastRollback = periodItems.FirstOrDefault(x => x.RollbackExecuted);
        var lastHealthyApply = periodItems.FirstOrDefault(x =>
            IsHealthy(x) &&
            string.Equals(x.Mode, "apply", StringComparison.OrdinalIgnoreCase));

        var midpoint = from.AddTicks((now - from).Ticks / 2);
        var previousHalf = periodItems.Where(x => x.RecordedAt < midpoint).ToList();
        var recentHalf = periodItems.Where(x => x.RecordedAt >= midpoint).ToList();

        var previousHealthyPercentage = Percentage(previousHalf.Count(IsHealthy), previousHalf.Count);
        var recentHealthyPercentage = Percentage(recentHalf.Count(IsHealthy), recentHalf.Count);
        var healthyPercentageDelta = Math.Round(
            recentHealthyPercentage - previousHealthyPercentage,
            1,
            MidpointRounding.AwayFromZero);

        var stabilityDirection =
            healthyPercentageDelta >= 10m ? "improving" :
            healthyPercentageDelta <= -10m ? "degrading" :
            "stable";

        var latest = periodItems.FirstOrDefault();

        return Ok(new ProductionReliabilitySignalsResponse(
            days,
            from,
            now,
            periodItems.Count,
            currentHealthyStreak,
            lastRollback?.Version,
            lastRollback?.RecordedAt,
            lastHealthyApply?.Version,
            lastHealthyApply?.RecordedAt,
            previousHealthyPercentage,
            recentHealthyPercentage,
            healthyPercentageDelta,
            stabilityDirection,
            latest?.Version,
            latest is not null && IsHealthy(latest)));
    }

    [HttpGet("deploys/trends")]
    public async Task<ActionResult<ProductionDeployTrendsResponse>> GetDeployTrends(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days < 1 || days > 365)
        {
            return BadRequest(new { message = "days deve ficar entre 1 e 365." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);
        var periodItems = allItems
            .Where(x => x.RecordedAt >= from && x.RecordedAt <= now)
            .OrderBy(x => x.RecordedAt)
            .ToList();

        var totalDeploys = periodItems.Count;
        var healthyDeploys = periodItems.Count(IsHealthy);
        var rollbackDeploys = periodItems.Count(x => x.RollbackExecuted);
        var applyDeploys = periodItems.Count(x =>
            string.Equals(x.Mode, "apply", StringComparison.OrdinalIgnoreCase));
        var validateOnlyDeploys = periodItems.Count(x =>
            string.Equals(x.Mode, "validate-only", StringComparison.OrdinalIgnoreCase));

        var healthyPercentage = Percentage(healthyDeploys, totalDeploys);
        var rollbackPercentage = Percentage(rollbackDeploys, totalDeploys);

        var daily = periodItems
            .GroupBy(x => DateOnly.FromDateTime(x.RecordedAt.UtcDateTime))
            .Select(group => new ProductionDeployTrendPoint(
                group.Key,
                group.Count(),
                group.Count(IsHealthy),
                group.Count(x => x.RollbackExecuted),
                group.Count(x => string.Equals(x.Mode, "apply", StringComparison.OrdinalIgnoreCase)),
                group.Count(x => string.Equals(x.Mode, "validate-only", StringComparison.OrdinalIgnoreCase))))
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(new ProductionDeployTrendsResponse(
            days,
            from,
            now,
            totalDeploys,
            healthyDeploys,
            healthyPercentage,
            rollbackDeploys,
            rollbackPercentage,
            applyDeploys,
            validateOnlyDeploys,
            daily));
    }

    [HttpGet("deploys/detail")]
    public async Task<ActionResult<ProductionDeployDetailResponse>> GetDeployDetail(
        [FromQuery] string version,
        [FromQuery] DateTimeOffset recordedAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return BadRequest(new { message = "version e obrigatoria." });
        }

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var item = allItems.FirstOrDefault(x =>
            string.Equals(x.Version, version, StringComparison.OrdinalIgnoreCase) &&
            x.RecordedAt.Equals(recordedAt));

        if (item is null)
        {
            return NotFound(new { message = "Release operacional nao encontrada no historico." });
        }

        var timeline = BuildTimeline(item);

        return Ok(new ProductionDeployDetailResponse(
            item,
            timeline,
            new ProductionDeployHealthSummary(
                item.RuntimeHealthy,
                item.VersionHealthy,
                item.ServedVersion,
                item.RollbackExecuted,
                item.RecoveryAuditComplete,
                item.ClosureComplete,
                item.OperationsStatus)));
    }

    [HttpGet("deploys/latest")]
    public async Task<ActionResult<ProductionDeployHistoryItem>> GetLatest(
        CancellationToken cancellationToken = default)
    {
        var latestPath = Path.Combine(_operationsDirectory, "latest-production-state.json");
        if (!System.IO.File.Exists(latestPath))
        {
            return NotFound(new { message = "Estado operacional de producao ainda nao foi materializado." });
        }

        var json = await System.IO.File.ReadAllTextAsync(latestPath, cancellationToken);
        if (!TryParseSafeItem(json, out var item))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Estado operacional de producao existe, mas nao pode ser interpretado com seguranca."
            });
        }

        return Ok(item);
    }

    private async Task<List<ProductionDeployHistoryItem>> ReadJsonLinesAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var result = new List<ProductionDeployHistoryItem>();
        if (!System.IO.File.Exists(path))
        {
            return result;
        }

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (TryParseSafeItem(line, out var item))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static bool TryParseSafeItem(string json, out ProductionDeployHistoryItem item)
    {
        item = default!;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var version = GetString(root, "version");
            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            item = new ProductionDeployHistoryItem(
                version,
                GetString(root, "mode") ?? "unknown",
                GetString(root, "target") ?? string.Empty,
                GetBoolean(root, "backupValidated"),
                GetBoolean(root, "migrationSafetyApproved"),
                GetBoolean(root, "migrationHashMatched"),
                GetBoolean(root, "destructiveMigrationsAllowed"),
                GetBoolean(root, "promotionApplied"),
                GetBoolean(root, "runtimeHealthy"),
                GetBoolean(root, "versionHealthy"),
                GetString(root, "servedVersion") ?? string.Empty,
                GetBoolean(root, "rollbackExecuted"),
                GetBoolean(root, "recoveryAuditComplete"),
                GetBoolean(root, "closureComplete"),
                GetString(root, "operationsStatus") ?? "unknown",
                GetDateTimeOffset(root, "recordedAt"));

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsHealthy(ProductionDeployHistoryItem item) =>
        string.Equals(item.OperationsStatus, "healthy", StringComparison.OrdinalIgnoreCase) &&
        item.RuntimeHealthy &&
        item.VersionHealthy &&
        !item.RollbackExecuted;

    private static decimal Percentage(int part, int total) =>
        total == 0
            ? 0m
            : Math.Round(part * 100m / total, 1, MidpointRounding.AwayFromZero);

    private static IReadOnlyList<ProductionDeployTimelineItem> BuildTimeline(
        ProductionDeployHistoryItem item)
    {
        var migrationOk =
            item.MigrationSafetyApproved &&
            item.MigrationHashMatched &&
            !item.DestructiveMigrationsAllowed;

        return new[]
        {
            new ProductionDeployTimelineItem(
                "backup",
                "Backup PostgreSQL",
                item.BackupValidated ? "passed" : "pending",
                item.BackupValidated ? "Backup validado antes das mutacoes." : "Backup nao confirmado."),

            new ProductionDeployTimelineItem(
                "migration-safety",
                "Migration Safety",
                migrationOk ? "passed" : "blocked",
                migrationOk ? "Conjunto aprovado, hash conferido e destrutivas bloqueadas." : "Migration safety nao confirmou todos os invariantes."),

            new ProductionDeployTimelineItem(
                "promotion",
                "Promocao",
                item.PromotionApplied ? "applied" : "skipped",
                item.PromotionApplied ? "Release promovida pelo fluxo atomico." : "Ciclo sem promocao real."),

            new ProductionDeployTimelineItem(
                "runtime",
                "Runtime Health",
                item.RuntimeHealthy ? "passed" : "unchecked",
                item.RuntimeHealthy ? "Runtime respondeu saudavel." : "Runtime nao foi confirmado neste ciclo."),

            new ProductionDeployTimelineItem(
                "version",
                "Version Verification",
                item.VersionHealthy ? "passed" : "unchecked",
                item.VersionHealthy ? $"Versao servida confirmada: {item.ServedVersion}." : "Versao servida nao foi confirmada."),

            new ProductionDeployTimelineItem(
                "rollback",
                "Rollback",
                item.RollbackExecuted ? "executed" : "not-required",
                item.RollbackExecuted ? "Rollback executado durante o ciclo." : "Rollback nao foi necessario."),

            new ProductionDeployTimelineItem(
                "recovery-audit",
                "Recovery Audit",
                item.RecoveryAuditComplete ? "passed" : "pending",
                item.RecoveryAuditComplete ? "Bundle de recovery audit concluido." : "Recovery audit nao concluido."),

            new ProductionDeployTimelineItem(
                "closure",
                "Closure Gate",
                item.ClosureComplete ? "passed" : "pending",
                item.ClosureComplete ? "Closure gate end-to-end concluido." : "Closure gate nao concluido.")
        };
    }

    private static string? GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool GetBoolean(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) &&
        (value.ValueKind == JsonValueKind.True ||
         (value.ValueKind == JsonValueKind.False ? false : false));

    private static DateTimeOffset GetDateTimeOffset(JsonElement root, string propertyName)
    {
        var raw = GetString(root, propertyName);
        return DateTimeOffset.TryParse(raw, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
    }
}

public sealed record ProductionSupportEvidencePackResponse(
    string FileName,
    string ContentType,
    string Content,
    string Version,
    DateTimeOffset RecordedAt,
    string State,
    int GateCount);

public sealed record ProductionSupportEventDetailResponse(
    ProductionSupportEventPublicData Event,
    IReadOnlyList<ProductionSupportEventGate> Gates,
    string SafetyNote,
    string ExcludedMetadata,
    string DecisionBoundary);

public sealed record ProductionSupportEventPublicData(
    string Version,
    DateTimeOffset RecordedAt,
    string Mode,
    string OperationsStatus,
    string State,
    bool RuntimeHealthy,
    bool VersionHealthy,
    string ServedVersion,
    bool RollbackExecuted,
    bool RecoveryAuditComplete,
    bool ClosureComplete);

public sealed record ProductionSupportEventGate(
    string Key,
    string Label,
    string State,
    string Description);

public sealed record ProductionSupportTimelineResponse(
    int Days,
    DateTimeOffset From,
    DateTimeOffset To,
    int Limit,
    int TotalItems,
    IReadOnlyList<ProductionSupportTimelineItem> Items,
    string SafetyNote,
    string ExcludedMetadata);

public sealed record ProductionSupportTimelineItem(
    DateTimeOffset RecordedAt,
    string Version,
    string Mode,
    string State,
    string Title,
    string Description,
    bool RuntimeHealthy,
    bool VersionHealthy,
    bool RollbackExecuted,
    bool RecoveryAuditComplete,
    bool ClosureComplete);

public sealed record ProductionSupportHandoffResponse(
    string HandoffId,
    int Days,
    DateTimeOffset From,
    DateTimeOffset To,
    string State,
    string? LatestVersion,
    DateTimeOffset? LatestRecordedAt,
    string? LatestServedVersion,
    string Summary,
    IReadOnlyList<ProductionSupportFact> Facts,
    IReadOnlyList<ProductionSupportChecklistItem> Checklist,
    string SafetyNote,
    string DecisionBoundary);

public sealed record ProductionSupportChecklistItem(
    string Key,
    string Label,
    bool SatisfiedByCurrentContext);

public sealed record ProductionSupportContextResponse(
    int Days,
    DateTimeOffset From,
    DateTimeOffset Midpoint,
    DateTimeOffset To,
    string State,
    ProductionSupportLatestCycle? LatestCycle,
    ProductionSupportTrends Trends,
    ProductionSupportSignals Signals,
    IReadOnlyList<ProductionSupportFact> Facts,
    string SafetyNote,
    string DecisionBoundary);

public sealed record ProductionSupportLatestCycle(
    string Version,
    DateTimeOffset RecordedAt,
    string Mode,
    string OperationsStatus,
    bool RuntimeHealthy,
    bool VersionHealthy,
    string ServedVersion,
    bool RollbackExecuted,
    bool RecoveryAuditComplete,
    bool ClosureComplete);

public sealed record ProductionSupportTrends(
    int TotalDeploys,
    int HealthyDeploys,
    decimal HealthyPercentage,
    int RollbackDeploys,
    int ApplyDeploys,
    int ValidateOnlyDeploys,
    decimal PreviousHealthyPercentage,
    decimal RecentHealthyPercentage);

public sealed record ProductionSupportSignals(
    int CurrentHealthyStreak,
    string StabilityDirection,
    decimal HealthyPercentageDelta,
    string? LastRollbackVersion,
    DateTimeOffset? LastRollbackAt,
    string? LastHealthyApplyVersion,
    DateTimeOffset? LastHealthyApplyAt);

public sealed record ProductionSupportFact(
    string Key,
    string Label,
    string Value);

public sealed record ProductionAuditSnapshotResponse(
    int Days,
    DateTimeOffset From,
    DateTimeOffset To,
    string State,
    ProductionAuditSnapshotIndicators Indicators,
    ProductionAuditSnapshotLatestCycle? LatestCycle,
    string? LastRollbackVersion,
    DateTimeOffset? LastRollbackAt,
    string Summary,
    string SafetyNote);

public sealed record ProductionAuditSnapshotIndicators(
    int TotalDeploys,
    int HealthyDeploys,
    decimal HealthyPercentage,
    int RollbackDeploys,
    int CurrentHealthyStreak);

public sealed record ProductionAuditSnapshotLatestCycle(
    string Version,
    DateTimeOffset RecordedAt,
    string Mode,
    string OperationsStatus,
    bool RuntimeHealthy,
    bool VersionHealthy,
    string ServedVersion,
    bool RollbackExecuted,
    bool ClosureComplete);

public sealed record ProductionReliabilityExportResponse(
    string FileName,
    string ContentType,
    string Content,
    int Days,
    int TotalDeploys,
    DateTimeOffset GeneratedAt);

public sealed record ProductionReliabilityDetailResponse(
    int Days,
    DateTimeOffset From,
    DateTimeOffset Midpoint,
    DateTimeOffset To,
    ProductionReliabilityWindow PreviousWindow,
    ProductionReliabilityWindow RecentWindow,
    int CurrentHealthyStreak,
    string? LastRollbackVersion,
    DateTimeOffset? LastRollbackAt,
    string? LastHealthyApplyVersion,
    DateTimeOffset? LastHealthyApplyAt,
    decimal HealthyPercentageDelta,
    string StabilityDirection,
    IReadOnlyList<ProductionReliabilityExplanation> Explanations);

public sealed record ProductionReliabilityWindow(
    string Key,
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalDeploys,
    int HealthyDeploys,
    decimal HealthyPercentage);

public sealed record ProductionReliabilityExplanation(
    string Key,
    string Label,
    string Calculation,
    string DecisionBoundary);

public sealed record ProductionReliabilitySignalsResponse(
    int Days,
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalDeploys,
    int CurrentHealthyStreak,
    string? LastRollbackVersion,
    DateTimeOffset? LastRollbackAt,
    string? LastHealthyApplyVersion,
    DateTimeOffset? LastHealthyApplyAt,
    decimal PreviousHealthyPercentage,
    decimal RecentHealthyPercentage,
    decimal HealthyPercentageDelta,
    string StabilityDirection,
    string? LatestVersion,
    bool LatestHealthy);

public sealed record ProductionDeployTrendsResponse(
    int Days,
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalDeploys,
    int HealthyDeploys,
    decimal HealthyPercentage,
    int RollbackDeploys,
    decimal RollbackPercentage,
    int ApplyDeploys,
    int ValidateOnlyDeploys,
    IReadOnlyList<ProductionDeployTrendPoint> Daily);

public sealed record ProductionDeployTrendPoint(
    DateOnly Date,
    int TotalDeploys,
    int HealthyDeploys,
    int RollbackDeploys,
    int ApplyDeploys,
    int ValidateOnlyDeploys);

public sealed record ProductionDeployDetailResponse(
    ProductionDeployHistoryItem Release,
    IReadOnlyList<ProductionDeployTimelineItem> Timeline,
    ProductionDeployHealthSummary Health);

public sealed record ProductionDeployTimelineItem(
    string Key,
    string Label,
    string State,
    string Description);

public sealed record ProductionDeployHealthSummary(
    bool RuntimeHealthy,
    bool VersionHealthy,
    string ServedVersion,
    bool RollbackExecuted,
    bool RecoveryAuditComplete,
    bool ClosureComplete,
    string OperationsStatus);

public sealed record ProductionDeployHistoryResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<ProductionDeployHistoryItem> Items);

public sealed record ProductionDeployHistoryItem(
    string Version,
    string Mode,
    string Target,
    bool BackupValidated,
    bool MigrationSafetyApproved,
    bool MigrationHashMatched,
    bool DestructiveMigrationsAllowed,
    bool PromotionApplied,
    bool RuntimeHealthy,
    bool VersionHealthy,
    string ServedVersion,
    bool RollbackExecuted,
    bool RecoveryAuditComplete,
    bool ClosureComplete,
    string OperationsStatus,
    DateTimeOffset RecordedAt);
