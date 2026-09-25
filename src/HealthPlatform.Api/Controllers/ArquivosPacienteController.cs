using System.Collections.Concurrent;
using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Policy = "AuthenticatedOnly")]
public sealed class ArquivosPacienteController(
    AppDbContext db,
    CurrentUser currentUser,
    IWebHostEnvironment environment,
    IPushNotificationService push) : ControllerBase
{
    private const long LimiteBytes = 15 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);

    [HttpGet("api/pacientes/{pacienteId:guid}/arquivos")]
    public async Task<IActionResult> ListarProfissional(Guid pacienteId, [FromQuery] string? q, [FromQuery] string? categoria, CancellationToken ct)
    {
        if (!await PodeAcessarComoProfissional(pacienteId, ct)) return Forbid();
        return Ok(Filtrar(await LerIndice(pacienteId, ct), q, categoria));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/arquivos/me")]
    public async Task<IActionResult> ListarPaciente([FromQuery] string? q, [FromQuery] string? categoria, CancellationToken ct)
    {
        var paciente = await MeuPaciente(ct);
        if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });
        return Ok(Filtrar(await LerIndice(paciente.Id, ct), q, categoria));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/arquivos/upload")]
    [RequestSizeLimit(LimiteBytes)]
    public async Task<IActionResult> UploadProfissional(Guid pacienteId, IFormFile file, [FromForm] string? categoria, [FromForm] string? descricao, [FromForm] string? tags, CancellationToken ct)
    {
        if (!await PodeAcessarComoProfissional(pacienteId, ct)) return Forbid();
        return await SalvarArquivo(pacienteId, file, categoria, descricao, tags, "Profissional", ct);
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpPost("api/arquivos/me/upload")]
    [RequestSizeLimit(LimiteBytes)]
    public async Task<IActionResult> UploadPaciente(IFormFile file, [FromForm] string? categoria, [FromForm] string? descricao, [FromForm] string? tags, CancellationToken ct)
    {
        var paciente = await MeuPaciente(ct);
        if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });
        return await SalvarArquivo(paciente.Id, file, categoria, descricao, tags, "Paciente", ct);
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/arquivos/{arquivoId:guid}/download")]
    public async Task<IActionResult> DownloadProfissional(Guid pacienteId, Guid arquivoId, CancellationToken ct)
    {
        if (!await PodeAcessarComoProfissional(pacienteId, ct)) return Forbid();
        return await Baixar(pacienteId, arquivoId, ct);
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/arquivos/me/{arquivoId:guid}/download")]
    public async Task<IActionResult> DownloadPaciente(Guid arquivoId, CancellationToken ct)
    {
        var paciente = await MeuPaciente(ct);
        if (paciente is null) return NotFound();
        return await Baixar(paciente.Id, arquivoId, ct);
    }

    [HttpDelete("api/pacientes/{pacienteId:guid}/arquivos/{arquivoId:guid}")]
    public async Task<IActionResult> RemoverProfissional(Guid pacienteId, Guid arquivoId, CancellationToken ct)
    {
        if (!await PodeAcessarComoProfissional(pacienteId, ct)) return Forbid();
        return await Remover(pacienteId, arquivoId, "Profissional", ct);
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpDelete("api/arquivos/me/{arquivoId:guid}")]
    public async Task<IActionResult> RemoverPaciente(Guid arquivoId, CancellationToken ct)
    {
        var paciente = await MeuPaciente(ct);
        if (paciente is null) return NotFound();
        return await Remover(paciente.Id, arquivoId, "Paciente", ct);
    }

    private async Task<IActionResult> SalvarArquivo(Guid pacienteId, IFormFile file, string? categoria, string? descricao, string? tags, string origem, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Selecione um arquivo." });
        if (file.Length > LimiteBytes) return BadRequest(new { message = "O arquivo deve ter no maximo 15 MB." });

        var contentType = NormalizarContentType(file.ContentType);
        var extensao = ExtensaoPermitida(contentType);
        if (extensao is null) return BadRequest(new { message = "Formato invalido. Use PDF, JPEG, PNG ou WebP." });
        if (!await AssinaturaValida(file, contentType, ct)) return BadRequest(new { message = "O conteudo do arquivo nao corresponde ao formato informado." });

        var id = Guid.NewGuid();
        var dir = DiretorioPaciente(pacienteId);
        Directory.CreateDirectory(dir);
        var nomeFisico = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{id:N}{extensao}";
        var target = Path.Combine(dir, nomeFisico);
        await using (var stream = System.IO.File.Create(target)) await file.CopyToAsync(stream, ct);

        var item = new ArquivoPacienteIndexItem
        {
            Id = id,
            Nome = Path.GetFileName(file.FileName),
            NomeFisico = nomeFisico,
            ContentType = contentType,
            TamanhoBytes = file.Length,
            Categoria = NormalizarCategoria(categoria),
            Descricao = Limitar(descricao, 500),
            Tags = NormalizarTags(tags),
            DataUtc = DateTime.UtcNow,
            Origem = origem,
            CriadoPorUsuarioId = currentUser.UserId
        };

        var gate = LockPaciente(pacienteId);
        await gate.WaitAsync(ct);
        try
        {
            var indice = await LerIndiceSemLock(pacienteId, ct);
            indice.Add(item);
            await GravarIndiceSemLock(pacienteId, indice, ct);
        }
        finally { gate.Release(); }

        await Auditar("UPLOAD", pacienteId, item, ct);
        await NotificarNovoArquivo(pacienteId, item, origem, ct);
        return Ok(ToResponse(pacienteId, item));
    }

    private async Task<IActionResult> Baixar(Guid pacienteId, Guid arquivoId, CancellationToken ct)
    {
        var item = (await LerIndice(pacienteId, ct)).FirstOrDefault(x => x.Id == arquivoId && !x.Removido);
        if (item is null) return NotFound();
        var path = Path.Combine(DiretorioPaciente(pacienteId), item.NomeFisico);
        if (!System.IO.File.Exists(path)) return NotFound(new { message = "Arquivo fisico nao encontrado." });
        await Auditar("DOWNLOAD", pacienteId, item, ct);
        return PhysicalFile(path, item.ContentType, item.Nome, enableRangeProcessing: true);
    }

    private async Task<IActionResult> Remover(Guid pacienteId, Guid arquivoId, string origem, CancellationToken ct)
    {
        ArquivoPacienteIndexItem? item;
        var gate = LockPaciente(pacienteId);
        await gate.WaitAsync(ct);
        try
        {
            var indice = await LerIndiceSemLock(pacienteId, ct);
            item = indice.FirstOrDefault(x => x.Id == arquivoId && !x.Removido);
            if (item is null) return NotFound();
            item.Removido = true;
            item.RemovidoEmUtc = DateTime.UtcNow;
            item.RemovidoPorUsuarioId = currentUser.UserId;
            item.RemovidoPorOrigem = origem;
            await GravarIndiceSemLock(pacienteId, indice, ct);
        }
        finally { gate.Release(); }

        await Auditar("REMOVE", pacienteId, item!, ct);
        return NoContent();
    }

    private async Task NotificarNovoArquivo(Guid pacienteId, ArquivoPacienteIndexItem item, string origem, CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .Where(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => new { x.Nome, x.UsuarioId })
            .FirstOrDefaultAsync(ct);

        if (paciente is null) return;

        if (string.Equals(origem, "Profissional", StringComparison.OrdinalIgnoreCase))
        {
            if (paciente.UsuarioId is Guid pacienteUsuarioId && pacienteUsuarioId != currentUser.UserId)
            {
                await push.EnviarAsync(
                    pacienteUsuarioId,
                    "lembrete",
                    "Novo arquivo no seu acompanhamento",
                    $"{item.Nome} foi adicionado à sua biblioteca AESYN.",
                    "arquivos",
                    ct);
            }
            return;
        }

        // O modelo atual permite que profissionais ativos da organização acompanhem os pacientes
        // da mesma organização. Enquanto não houver vínculo explícito paciente↔profissional,
        // notificamos os profissionais ativos e evitamos eco para o próprio usuário que enviou.
        var profissionais = await db.Profissionais.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo && x.UsuarioId != currentUser.UserId)
            .Select(x => x.UsuarioId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var usuarioId in profissionais)
        {
            await push.EnviarAsync(
                usuarioId,
                "lembrete",
                $"Novo arquivo de {paciente.Nome}",
                $"{item.Nome} foi enviado pelo paciente e está disponível para revisão.",
                "pacientes",
                ct);
        }
    }

    private async Task Auditar(string acao, Guid pacienteId, ArquivoPacienteIndexItem item, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = $"PATIENT_FILE_{acao}",
            Entidade = "ArquivoPaciente",
            EntidadeId = item.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new { pacienteId, item.Nome, item.Categoria, item.TamanhoBytes, item.Tags }, JsonOptions),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> PodeAcessarComoProfissional(Guid pacienteId, CancellationToken ct)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking().AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (!pacienteExiste) return false;
        return await db.Profissionais.AsNoTracking().AnyAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    }

    private Task<Paciente?> MeuPaciente(CancellationToken ct) => db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x =>
        x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private async Task<List<ArquivoPacienteIndexItem>> LerIndice(Guid pacienteId, CancellationToken ct)
    {
        var gate = LockPaciente(pacienteId);
        await gate.WaitAsync(ct);
        try { return await LerIndiceSemLock(pacienteId, ct); }
        finally { gate.Release(); }
    }

    private async Task<List<ArquivoPacienteIndexItem>> LerIndiceSemLock(Guid pacienteId, CancellationToken ct)
    {
        var path = CaminhoIndice(pacienteId);
        if (!System.IO.File.Exists(path)) return [];
        try
        {
            await using var stream = System.IO.File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<List<ArquivoPacienteIndexItem>>(stream, JsonOptions, ct) ?? [];
        }
        catch (JsonException) { return []; }
    }

    private async Task GravarIndiceSemLock(Guid pacienteId, List<ArquivoPacienteIndexItem> itens, CancellationToken ct)
    {
        Directory.CreateDirectory(DiretorioPaciente(pacienteId));
        var path = CaminhoIndice(pacienteId);
        var temp = path + ".tmp";
        await using (var stream = System.IO.File.Create(temp)) await JsonSerializer.SerializeAsync(stream, itens, JsonOptions, ct);
        System.IO.File.Move(temp, path, true);
    }

    private object[] Filtrar(IEnumerable<ArquivoPacienteIndexItem> itens, string? q, string? categoria)
    {
        var query = itens.Where(x => !x.Removido);
        if (!string.IsNullOrWhiteSpace(categoria)) query = query.Where(x => x.Categoria.Equals(categoria.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var termo = q.Trim();
            query = query.Where(x => x.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                (x.Descricao?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.Tags.Any(t => t.Contains(termo, StringComparison.OrdinalIgnoreCase)) ||
                x.Categoria.Contains(termo, StringComparison.OrdinalIgnoreCase));
        }
        return query.OrderByDescending(x => x.DataUtc).Select(x => ToResponse(Guid.Empty, x)).ToArray();
    }

    private object ToResponse(Guid pacienteId, ArquivoPacienteIndexItem x) => new
    {
        x.Id, x.Nome, x.ContentType, x.TamanhoBytes, x.Categoria, x.Descricao, x.Tags, x.DataUtc, x.Origem,
        downloadUrl = pacienteId == Guid.Empty ? (string?)null : $"/api/pacientes/{pacienteId}/arquivos/{x.Id}/download",
        chatReferencia = $"Arquivo AESYN: {x.Nome} • {x.Categoria} • ID {x.Id}"
    };

    private string DiretorioPaciente(Guid pacienteId) => Path.Combine(environment.ContentRootPath, "App_Data", "patient-files", currentUser.OrganizationId.ToString("N"), pacienteId.ToString("N"));
    private string CaminhoIndice(Guid pacienteId) => Path.Combine(DiretorioPaciente(pacienteId), "index.json");
    private SemaphoreSlim LockPaciente(Guid pacienteId) => Locks.GetOrAdd($"{currentUser.OrganizationId:N}:{pacienteId:N}", _ => new SemaphoreSlim(1, 1));

    private static string NormalizarContentType(string valor) => valor.Split(';', 2)[0].Trim().ToLowerInvariant();
    private static string? ExtensaoPermitida(string contentType) => contentType switch
    {
        "application/pdf" => ".pdf", "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", _ => null
    };
    private static string NormalizarCategoria(string? valor)
    {
        var v = (valor ?? "Outro").Trim();
        var permitidas = new[] { "Exame", "Laudo", "Foto", "Receita", "Documento", "Outro" };
        return permitidas.FirstOrDefault(x => x.Equals(v, StringComparison.OrdinalIgnoreCase)) ?? "Outro";
    }
    private static string? Limitar(string? valor, int limite)
    {
        var v = valor?.Trim();
        return string.IsNullOrWhiteSpace(v) ? null : v.Length <= limite ? v : v[..limite];
    }
    private static string[] NormalizarTags(string? tags) => (tags ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.Length > 40 ? x[..40] : x).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray();

    private static async Task<bool> AssinaturaValida(IFormFile file, string contentType, CancellationToken ct)
    {
        var buffer = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
        return contentType switch
        {
            "application/pdf" => read >= 5 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46 && buffer[4] == 0x2D,
            "image/jpeg" => read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            "image/png" => read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 && buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A,
            "image/webp" => read >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50,
            _ => false
        };
    }

    private sealed class ArquivoPacienteIndexItem
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string NomeFisico { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long TamanhoBytes { get; set; }
        public string Categoria { get; set; } = "Outro";
        public string? Descricao { get; set; }
        public string[] Tags { get; set; } = [];
        public DateTime DataUtc { get; set; }
        public string Origem { get; set; } = string.Empty;
        public Guid CriadoPorUsuarioId { get; set; }
        public bool Removido { get; set; }
        public DateTime? RemovidoEmUtc { get; set; }
        public Guid? RemovidoPorUsuarioId { get; set; }
        public string? RemovidoPorOrigem { get; set; }
    }
}
