using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class FotosEvolucaoController(AppDbContext db, CurrentUser currentUser, IWebHostEnvironment environment) : ControllerBase
{
    private const long LimiteBytes = 8 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> Extensoes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    [HttpPost("api/pacientes/{pacienteId:guid}/fotos-evolucao/upload")]
    [RequestSizeLimit(LimiteBytes)]
    public async Task<IActionResult> Upload(Guid pacienteId, IFormFile file, CancellationToken ct)
    {
        if (!await PacienteAutorizado(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        if (file is null || file.Length == 0) return BadRequest(new { message = "Selecione uma imagem." });
        if (file.Length > LimiteBytes) return BadRequest(new { message = "A imagem deve ter no maximo 8 MB." });
        if (!Extensoes.TryGetValue(file.ContentType, out var extensao)) return BadRequest(new { message = "Formato invalido. Use JPEG, PNG ou WebP." });
        if (!await AssinaturaValida(file, file.ContentType, ct)) return BadRequest(new { message = "O conteudo do arquivo nao corresponde a uma imagem valida." });

        var targetDir = DiretorioPaciente(pacienteId);
        Directory.CreateDirectory(targetDir);
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extensao}";
        var target = Path.Combine(targetDir, fileName);
        await using (var stream = System.IO.File.Create(target)) await file.CopyToAsync(stream, ct);

        // A URL aponta para endpoint autenticado; fotos clinicas nao ficam expostas pelo StaticFiles.
        var url = $"/api/pacientes/{pacienteId}/fotos-evolucao/{fileName}";
        return Ok(new { url, fileName, file.Length, contentType = file.ContentType });
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/fotos-evolucao/{fileName}")]
    public async Task<IActionResult> Get(Guid pacienteId, string fileName, CancellationToken ct)
    {
        if (!await PacienteAutorizado(pacienteId, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName) return BadRequest();
        var extensao = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extensao switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
        if (contentType is null) return BadRequest();
        var path = Path.Combine(DiretorioPaciente(pacienteId), fileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    private Task<bool> PacienteAutorizado(Guid pacienteId, CancellationToken ct) =>
        db.Pacientes.AsNoTracking().AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private string DiretorioPaciente(Guid pacienteId) => Path.Combine(
        environment.ContentRootPath,
        "App_Data",
        "evolucao",
        currentUser.OrganizationId.ToString("N"),
        pacienteId.ToString("N"));

    private static async Task<bool> AssinaturaValida(IFormFile file, string contentType, CancellationToken ct)
    {
        var buffer = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
        if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            return read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
        if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 && buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A;
        if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
            return read >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50;
        return false;
    }
}
