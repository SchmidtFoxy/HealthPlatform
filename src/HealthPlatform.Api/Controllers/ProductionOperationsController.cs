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
