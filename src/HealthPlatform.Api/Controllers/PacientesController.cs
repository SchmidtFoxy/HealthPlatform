using System.Net.Mail;
using System.Text.Json;
using HealthPlatform.Api.Contracts.Pacientes;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes")]
public class PacientesController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PacienteListResponse>> GetAll(
        [FromQuery] string? busca,
        [FromQuery] bool incluirInativos = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 25,
        CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 100);

        var query = db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId);

        if (!incluirInativos)
            query = query.Where(x => x.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            var cpfBusca = SomenteDigitos(busca);
            query = query.Where(x =>
                x.Nome.ToLower().Contains(termo) ||
                (x.Cpf != null && (x.Cpf.Contains(termo) || (cpfBusca != null && x.Cpf.Replace(".", "").Replace("-", "").Contains(cpfBusca)))) ||
                (x.Email != null && x.Email.ToLower().Contains(termo)) ||
                (x.Telefone != null && x.Telefone.Contains(termo)));
        }

        var total = await query.CountAsync(ct);
        var itens = await query
            .OrderBy(x => x.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);

        var totalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanhoPagina);
        return Ok(new PacienteListResponse(itens, total, pagina, tamanhoPagina, totalPaginas));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PacienteResponse>> GetById(Guid id, CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

        return paciente is null ? NotFound(new { message = "Paciente nao encontrado." }) : Ok(ToResponse(paciente));
    }

    [HttpPost]
    public async Task<ActionResult<PacienteResponse>> Create(CreatePacienteRequest request, CancellationToken ct)
    {
        var validation = await ValidarAsync(request.Nome, request.Cpf, request.Email, null, ct);
        if (validation is not null) return validation;

        var paciente = new Paciente
        {
            OrganizacaoId = currentUser.OrganizationId,
            Nome = request.Nome.Trim(),
            Cpf = NormalizarCpf(request.Cpf),
            DataNascimento = request.DataNascimento,
            Sexo = NormalizarOpcional(request.Sexo),
            Telefone = NormalizarOpcional(request.Telefone),
            Email = NormalizarEmail(request.Email),
            Profissao = NormalizarOpcional(request.Profissao)
        };

        db.Pacientes.Add(paciente);
        AdicionarAuditoria("CREATE", paciente, null, Snapshot(paciente));
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = paciente.Id }, ToResponse(paciente));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PacienteResponse>> Update(Guid id, UpdatePacienteRequest request, CancellationToken ct)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(
            x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });

        var validation = await ValidarAsync(request.Nome, request.Cpf, request.Email, id, ct);
        if (validation is not null) return validation;

        var antes = Snapshot(paciente);
        paciente.Nome = request.Nome.Trim();
        paciente.Cpf = NormalizarCpf(request.Cpf);
        paciente.DataNascimento = request.DataNascimento;
        paciente.Sexo = NormalizarOpcional(request.Sexo);
        paciente.Telefone = NormalizarOpcional(request.Telefone);
        paciente.Email = NormalizarEmail(request.Email);
        paciente.Profissao = NormalizarOpcional(request.Profissao);

        AdicionarAuditoria("UPDATE", paciente, antes, Snapshot(paciente));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(paciente));
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<ActionResult<PacienteResponse>> Activate(Guid id, CancellationToken ct)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(
            x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });
        if (paciente.Ativo) return Ok(ToResponse(paciente));

        var antes = Snapshot(paciente);
        paciente.Ativo = true;
        AdicionarAuditoria("ACTIVATE", paciente, antes, Snapshot(paciente));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(paciente));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(
            x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });
        if (!paciente.Ativo) return NoContent();

        var antes = Snapshot(paciente);
        paciente.Ativo = false;
        AdicionarAuditoria("DEACTIVATE", paciente, antes, Snapshot(paciente));
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<ActionResult?> ValidarAsync(string nome, string? cpf, string? email, Guid? ignorarPacienteId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return BadRequest(new { message = "Nome e obrigatorio." });

        if (nome.Trim().Length > 160)
            return BadRequest(new { message = "Nome deve possuir no maximo 160 caracteres." });

        var cpfNormalizado = NormalizarCpf(cpf);
        if (cpfNormalizado is not null && cpfNormalizado.Length != 11)
            return BadRequest(new { message = "CPF deve possuir 11 digitos." });

        if (cpfNormalizado is not null)
        {
            var cpfExiste = await db.Pacientes.AnyAsync(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Cpf == cpfNormalizado &&
                (!ignorarPacienteId.HasValue || x.Id != ignorarPacienteId.Value), ct);

            if (cpfExiste)
                return Conflict(new { message = "Ja existe um paciente com este CPF nesta organizacao." });
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            try { _ = new MailAddress(email.Trim()); }
            catch { return BadRequest(new { message = "Email invalido." }); }
        }

        return null;
    }

    private void AdicionarAuditoria(string acao, Paciente paciente, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(Paciente),
            EntidadeId = paciente.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static object Snapshot(Paciente x) => new
    {
        x.Id,
        x.Nome,
        x.Cpf,
        x.DataNascimento,
        x.Sexo,
        x.Telefone,
        x.Email,
        x.Profissao,
        x.Ativo
    };

    private static string? SomenteDigitos(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static string? NormalizarCpf(string? cpf) => SomenteDigitos(cpf);
    private static string? NormalizarEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    private static string? NormalizarOpcional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PacienteResponse ToResponse(Paciente x) => new(
        x.Id, x.Nome, x.Cpf, x.DataNascimento, x.Sexo, x.Telefone, x.Email, x.Profissao, x.Ativo, x.CreatedAtUtc);
}
