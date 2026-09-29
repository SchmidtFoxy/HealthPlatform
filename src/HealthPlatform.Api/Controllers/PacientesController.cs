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


    [HttpGet("pesquisa-avancada")]
    public async Task<IActionResult> PesquisaAvancada(
        [FromQuery] string? busca = null,
        [FromQuery] string? status = "Ativos",
        [FromQuery] Guid? responsavelId = null,
        [FromQuery] string? aderencia = null,
        [FromQuery] string? ultimaInteracao = null,
        [FromQuery] string? proximaRevisao = null,
        [FromQuery] string? marcador = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? ordenar = "nome",
        CancellationToken ct = default)
    {
        var agora = DateTime.UtcNow;
        var pacientesBase = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new { x.Id, x.Nome, x.Cpf, x.Email, x.Telefone, x.Profissao, x.Ativo, x.StatusAcompanhamento, x.MotivoStatusAcompanhamento, x.StatusAcompanhamentoAlteradoEmUtc, x.TagsSegmentacao })
            .ToListAsync(ct);

        var ids = pacientesBase.Select(x => x.Id).ToArray();
        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.ProfissionalId, ProfissionalNome = x.Profissional.Nome, x.DataHoraUtc })
            .ToListAsync(ct);
        var checkIns = await db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataUtc, x.AdesaoAlimentacaoPercentual, x.AdesaoTreinoPercentual })
            .ToListAsync(ct);
        var interacoes = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.DataHoraUtc, x.ProximoContatoUtc })
            .ToListAsync(ct);

        var itens = pacientesBase.Select(paciente =>
        {
            var ultimaConsulta = consultas.Where(x => x.PacienteId == paciente.Id).OrderByDescending(x => x.DataHoraUtc).FirstOrDefault();
            var ultimosCheckIns = checkIns.Where(x => x.PacienteId == paciente.Id).OrderByDescending(x => x.DataUtc).Take(4).ToArray();
            var valoresAdesao = ultimosCheckIns
                .SelectMany(x => new int?[] { x.AdesaoAlimentacaoPercentual, x.AdesaoTreinoPercentual })
                .Where(x => x.HasValue).Select(x => x!.Value).ToArray();
            decimal? adesaoMedia = valoresAdesao.Length == 0 ? null : Math.Round((decimal)valoresAdesao.Average(), 1);
            var interacoesPaciente = interacoes.Where(x => x.PacienteId == paciente.Id).ToArray();
            var ultima = interacoesPaciente.OrderByDescending(x => x.DataHoraUtc).Select(x => (DateTime?)x.DataHoraUtc).FirstOrDefault();
            var proxima = interacoesPaciente.Where(x => x.ProximoContatoUtc.HasValue && x.ProximoContatoUtc >= agora)
                .OrderBy(x => x.ProximoContatoUtc).Select(x => x.ProximoContatoUtc).FirstOrDefault();

            var marcadores = new List<string>();
            if (!ultima.HasValue || ultima.Value < agora.AddDays(-30)) marcadores.Add("SemInteracaoRecente");
            if (!proxima.HasValue) marcadores.Add("SemRevisaoAgendada");
            if (adesaoMedia.HasValue && adesaoMedia.Value < 60) marcadores.Add("BaixaAdesao");
            if (!adesaoMedia.HasValue) marcadores.Add("SemDadosAdesao");
            if (!paciente.Ativo) marcadores.Add("Inativo");

            return new PacientePesquisaAvancadaItem(
                paciente.Id, paciente.Nome, paciente.Cpf, paciente.Email, paciente.Telefone, paciente.Profissao, paciente.Ativo, paciente.StatusAcompanhamento, paciente.MotivoStatusAcompanhamento, paciente.StatusAcompanhamentoAlteradoEmUtc,
                ultimaConsulta?.ProfissionalId, ultimaConsulta?.ProfissionalNome, adesaoMedia, ultima, proxima, marcadores, LerTagsSegmentacao(paciente.TagsSegmentacao));
        }).ToList();

        IEnumerable<PacientePesquisaAvancadaItem> query = itens;
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            var digitos = SomenteDigitos(busca);
            query = query.Where(x => x.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                (x.Email?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Telefone?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Cpf?.Contains(digitos ?? termo, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        query = (status ?? "Ativos").Trim().ToLowerInvariant() switch
        {
            "ativo" => query.Where(x => x.StatusAcompanhamento == "Ativo"),
            "pausado" => query.Where(x => x.StatusAcompanhamento == "Pausado"),
            "aguardandoavaliacao" => query.Where(x => x.StatusAcompanhamento == "AguardandoAvaliacao"),
            "encerrado" => query.Where(x => x.StatusAcompanhamento == "Encerrado"),
            "inativos" => query.Where(x => !x.Ativo),
            "todos" => query,
            _ => query.Where(x => x.Ativo)
        };
        if (responsavelId.HasValue) query = query.Where(x => x.ResponsavelId == responsavelId.Value);
        if (!string.IsNullOrWhiteSpace(aderencia)) query = aderencia.Trim().ToLowerInvariant() switch
        {
            "alta" => query.Where(x => x.AdesaoMediaPercentual >= 80),
            "media" => query.Where(x => x.AdesaoMediaPercentual >= 60 && x.AdesaoMediaPercentual < 80),
            "baixa" => query.Where(x => x.AdesaoMediaPercentual < 60),
            "semdados" => query.Where(x => !x.AdesaoMediaPercentual.HasValue),
            _ => query
        };
        if (!string.IsNullOrWhiteSpace(ultimaInteracao)) query = ultimaInteracao.Trim().ToLowerInvariant() switch
        {
            "7d" => query.Where(x => x.UltimaInteracaoUtc >= agora.AddDays(-7)),
            "30d" => query.Where(x => x.UltimaInteracaoUtc >= agora.AddDays(-30)),
            "antiga" => query.Where(x => !x.UltimaInteracaoUtc.HasValue || x.UltimaInteracaoUtc < agora.AddDays(-30)),
            _ => query
        };
        if (!string.IsNullOrWhiteSpace(proximaRevisao)) query = proximaRevisao.Trim().ToLowerInvariant() switch
        {
            "7d" => query.Where(x => x.ProximaRevisaoUtc.HasValue && x.ProximaRevisaoUtc <= agora.AddDays(7)),
            "30d" => query.Where(x => x.ProximaRevisaoUtc.HasValue && x.ProximaRevisaoUtc <= agora.AddDays(30)),
            "semdata" => query.Where(x => !x.ProximaRevisaoUtc.HasValue),
            _ => query
        };
        if (!string.IsNullOrWhiteSpace(marcador) && !marcador.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Marcadores.Contains(marcador, StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(tag) && !tag.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Tags.Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase));

        query = (ordenar ?? "nome").Trim().ToLowerInvariant() switch
        {
            "aderencia" => query.OrderBy(x => x.AdesaoMediaPercentual ?? -1).ThenBy(x => x.Nome),
            "interacao" => query.OrderBy(x => x.UltimaInteracaoUtc ?? DateTime.MinValue).ThenBy(x => x.Nome),
            "revisao" => query.OrderBy(x => x.ProximaRevisaoUtc ?? DateTime.MaxValue).ThenBy(x => x.Nome),
            _ => query.OrderBy(x => x.Nome)
        };

        var responsaveis = consultas.GroupBy(x => new { x.ProfissionalId, x.ProfissionalNome })
            .Select(x => new { id = x.Key.ProfissionalId, nome = x.Key.ProfissionalNome }).OrderBy(x => x.nome).ToArray();
        var final = query.ToArray();
        var tagsDisponiveis = itens.SelectMany(x => x.Tags).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        return Ok(new { total = final.Length, itens = final, responsaveis, marcadores = new[] { "SemInteracaoRecente", "SemRevisaoAgendada", "BaixaAdesao", "SemDadosAdesao", "Inativo" }, tagsDisponiveis });
    }

    [Authorize(Roles = "Admin,Medico,Nutricionista,Personal,Secretaria")]
    [HttpPatch("{id:guid}/tags")]
    public async Task<IActionResult> AtualizarTags(Guid id, AtualizarTagsPacienteRequest request, CancellationToken ct)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });

        var tags = NormalizarTags(request.Tags);
        var antes = new { paciente.Id, Tags = LerTagsSegmentacao(paciente.TagsSegmentacao) };
        paciente.TagsSegmentacao = tags.Count == 0 ? null : JsonSerializer.Serialize(tags);
        paciente.UpdatedAtUtc = DateTime.UtcNow;
        var depois = new { paciente.Id, Tags = tags };

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "PATIENT_TAGS_CHANGED",
            Entidade = nameof(Paciente),
            EntidadeId = paciente.Id.ToString(),
            DadosAnterioresJson = JsonSerializer.Serialize(antes),
            DadosNovosJson = JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);
        return Ok(new { paciente.Id, tags });
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
            Profissao = NormalizarOpcional(request.Profissao),
            StatusAcompanhamento = "Ativo",
            StatusAcompanhamentoAlteradoEmUtc = DateTime.UtcNow
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

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<PacienteResponse>> AlterarStatus(Guid id, AlterarStatusPacienteRequest request, CancellationToken ct)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });

        var statusNormalizado = NormalizarStatusAcompanhamento(request.Status);
        if (statusNormalizado is null)
            return BadRequest(new { message = "Status invalido. Use Ativo, Pausado, AguardandoAvaliacao ou Encerrado." });

        var antes = Snapshot(paciente);
        paciente.StatusAcompanhamento = statusNormalizado;
        paciente.MotivoStatusAcompanhamento = NormalizarOpcional(request.Motivo);
        paciente.StatusAcompanhamentoAlteradoEmUtc = DateTime.UtcNow;
        paciente.Ativo = statusNormalizado != "Encerrado";
        AdicionarAuditoria("PATIENT_STATUS_CHANGED", paciente, antes, Snapshot(paciente));
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
        paciente.StatusAcompanhamento = "Ativo";
        paciente.StatusAcompanhamentoAlteradoEmUtc = DateTime.UtcNow;
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
        paciente.StatusAcompanhamento = "Encerrado";
        paciente.StatusAcompanhamentoAlteradoEmUtc = DateTime.UtcNow;
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
        x.Ativo,
        x.StatusAcompanhamento,
        x.MotivoStatusAcompanhamento,
        x.StatusAcompanhamentoAlteradoEmUtc
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
    private static string? NormalizarStatusAcompanhamento(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalizado = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return normalizado switch
        {
            "ativo" => "Ativo",
            "pausado" => "Pausado",
            "aguardandoavaliacao" => "AguardandoAvaliacao",
            "encerrado" => "Encerrado",
            _ => null
        };
    }

    private static IReadOnlyList<string> LerTagsSegmentacao(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }

    private static IReadOnlyList<string> NormalizarTags(IEnumerable<string>? tags) =>
        (tags ?? Array.Empty<string>())
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Length <= 40 ? x : x[..40])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToArray();

    private static PacienteResponse ToResponse(Paciente x) => new(
        x.Id, x.Nome, x.Cpf, x.DataNascimento, x.Sexo, x.Telefone, x.Email, x.Profissao, x.Ativo, x.StatusAcompanhamento, x.MotivoStatusAcompanhamento, x.StatusAcompanhamentoAlteradoEmUtc, LerTagsSegmentacao(x.TagsSegmentacao), x.CreatedAtUtc);
}


public sealed record PacientePesquisaAvancadaItem(
    Guid Id, string Nome, string? Cpf, string? Email, string? Telefone, string? Profissao, bool Ativo, string StatusAcompanhamento, string? MotivoStatusAcompanhamento, DateTime? StatusAcompanhamentoAlteradoEmUtc,
    Guid? ResponsavelId, string? ResponsavelNome, decimal? AdesaoMediaPercentual, DateTime? UltimaInteracaoUtc,
    DateTime? ProximaRevisaoUtc, IReadOnlyCollection<string> Marcadores, IReadOnlyCollection<string> Tags);

public sealed record AtualizarTagsPacienteRequest(IReadOnlyCollection<string>? Tags);
