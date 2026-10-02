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

        var historyPath = Path.Combine(_operationsDirectory, "production-deploy-history.jsonl");
        var allItems = await ReadJsonLinesAsync(historyPath, cancellationToken);

        var ordered = allItems
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
