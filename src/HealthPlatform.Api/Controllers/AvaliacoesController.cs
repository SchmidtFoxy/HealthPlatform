using System.Text.Json;
using HealthPlatform.Api.Contracts.Avaliacoes;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class AvaliacoesController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/avaliacoes")]
    public async Task<ActionResult<IReadOnlyCollection<AvaliacaoResponse>>> GetByPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataUtc)
            .ToListAsync(ct);

        return Ok(avaliacoes.Select(ToResponse).ToList());
    }

    [HttpGet("api/avaliacoes/{id:guid}")]
    public async Task<ActionResult<AvaliacaoResponse>> GetById(Guid id, CancellationToken ct)
    {
        var avaliacao = await db.Avaliacoes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        return avaliacao is null ? NotFound(new { message = "Avaliacao nao encontrada." }) : Ok(ToResponse(avaliacao));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/avaliacoes")]
    public async Task<ActionResult<AvaliacaoResponse>> Create(Guid pacienteId, CreateAvaliacaoRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        if (request.ConsultaId.HasValue)
        {
            var consultaValida = await db.Consultas.AnyAsync(x =>
                x.Id == request.ConsultaId.Value &&
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

            if (!consultaValida)
                return BadRequest(new { message = "A consulta informada nao pertence a este paciente/organizacao." });

            var jaPossui = await db.Avaliacoes.AnyAsync(x => x.ConsultaId == request.ConsultaId.Value, ct);
            if (jaPossui)
                return Conflict(new { message = "Esta consulta ja possui uma avaliacao vinculada." });
        }

        var validation = Validar(request);
        if (validation is not null) return validation;

        var avaliacao = new Avaliacao
        {
            PacienteId = pacienteId,
            ConsultaId = request.ConsultaId,
            DataUtc = (request.DataUtc ?? DateTime.UtcNow).ToUniversalTime(),
            PesoKg = request.PesoKg,
            AlturaM = request.AlturaM,
            PercentualGordura = request.PercentualGordura,
            MassaMagraKg = request.MassaMagraKg,
            MassaGordaKg = request.MassaGordaKg,
            CinturaCm = request.CinturaCm,
            AbdomenCm = request.AbdomenCm,
            QuadrilCm = request.QuadrilCm,
            PressaoSistolica = request.PressaoSistolica,
            PressaoDiastolica = request.PressaoDiastolica,
            FrequenciaCardiaca = request.FrequenciaCardiaca
        };

        db.Avaliacoes.Add(avaliacao);
        AdicionarAuditoria("CREATE", avaliacao, null, Snapshot(avaliacao));
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = avaliacao.Id }, ToResponse(avaliacao));
    }

    [HttpPut("api/avaliacoes/{id:guid}")]
    public async Task<ActionResult<AvaliacaoResponse>> Update(Guid id, CreateAvaliacaoRequest request, CancellationToken ct)
    {
        var avaliacao = await db.Avaliacoes
            .Include(x => x.Paciente)
            .FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (avaliacao is null)
            return NotFound(new { message = "Avaliacao nao encontrada." });

        if (request.ConsultaId.HasValue)
        {
            var consultaValida = await db.Consultas.AnyAsync(x =>
                x.Id == request.ConsultaId.Value &&
                x.PacienteId == avaliacao.PacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

            if (!consultaValida)
                return BadRequest(new { message = "A consulta informada nao pertence a este paciente/organizacao." });

            var jaPossui = await db.Avaliacoes.AnyAsync(x => x.ConsultaId == request.ConsultaId.Value && x.Id != id, ct);
            if (jaPossui)
                return Conflict(new { message = "Esta consulta ja possui uma avaliacao vinculada." });
        }

        var validation = Validar(request);
        if (validation is not null) return validation;

        var antes = Snapshot(avaliacao);
        avaliacao.ConsultaId = request.ConsultaId;
        avaliacao.DataUtc = (request.DataUtc ?? avaliacao.DataUtc).ToUniversalTime();
        avaliacao.PesoKg = request.PesoKg;
        avaliacao.AlturaM = request.AlturaM;
        avaliacao.PercentualGordura = request.PercentualGordura;
        avaliacao.MassaMagraKg = request.MassaMagraKg;
        avaliacao.MassaGordaKg = request.MassaGordaKg;
        avaliacao.CinturaCm = request.CinturaCm;
        avaliacao.AbdomenCm = request.AbdomenCm;
        avaliacao.QuadrilCm = request.QuadrilCm;
        avaliacao.PressaoSistolica = request.PressaoSistolica;
        avaliacao.PressaoDiastolica = request.PressaoDiastolica;
        avaliacao.FrequenciaCardiaca = request.FrequenciaCardiaca;
        avaliacao.UpdatedAtUtc = DateTime.UtcNow;

        AdicionarAuditoria("UPDATE", avaliacao, antes, Snapshot(avaliacao));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(avaliacao));
    }

    private ActionResult? Validar(CreateAvaliacaoRequest request)
    {
        if (request.PesoKg <= 0) return BadRequest(new { message = "Peso deve ser maior que zero." });
        if (request.AlturaM <= 0 || request.AlturaM > 3) return BadRequest(new { message = "Altura deve estar entre 0 e 3 metros." });
        if (request.PercentualGordura < 0 || request.PercentualGordura > 100) return BadRequest(new { message = "Percentual de gordura deve estar entre 0 e 100." });
        if (request.PressaoSistolica <= 0 || request.PressaoDiastolica <= 0) return BadRequest(new { message = "Pressao arterial deve ser maior que zero quando informada." });
        if (request.FrequenciaCardiaca <= 0) return BadRequest(new { message = "Frequencia cardiaca deve ser maior que zero quando informada." });
        return null;
    }

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AnyAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private void AdicionarAuditoria(string acao, Avaliacao avaliacao, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(Avaliacao),
            EntidadeId = avaliacao.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static object Snapshot(Avaliacao x) => new
    {
        x.Id,
        x.PacienteId,
        x.ConsultaId,
        x.DataUtc,
        x.PesoKg,
        x.AlturaM,
        x.PercentualGordura,
        x.MassaMagraKg,
        x.MassaGordaKg,
        x.CinturaCm,
        x.AbdomenCm,
        x.QuadrilCm,
        x.PressaoSistolica,
        x.PressaoDiastolica,
        x.FrequenciaCardiaca
    };

    private static AvaliacaoResponse ToResponse(Avaliacao x)
    {
        decimal? imc = null;
        if (x.PesoKg.HasValue && x.AlturaM.HasValue && x.AlturaM.Value > 0)
            imc = Math.Round(x.PesoKg.Value / (x.AlturaM.Value * x.AlturaM.Value), 2);

        return new AvaliacaoResponse(
            x.Id, x.PacienteId, x.ConsultaId, x.DataUtc,
            x.PesoKg, x.AlturaM, imc, x.PercentualGordura,
            x.MassaMagraKg, x.MassaGordaKg, x.CinturaCm,
            x.AbdomenCm, x.QuadrilCm, x.PressaoSistolica,
            x.PressaoDiastolica, x.FrequenciaCardiaca, x.CreatedAtUtc);
    }
}
