using System.Text.Json;
using HealthPlatform.Api.Contracts.Exames;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class ExamesLaboratoriaisController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/exames")]
    public async Task<ActionResult<IReadOnlyCollection<ExameLaboratorialResponse>>> GetByPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var itens = await QueryCompleta()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataColetaUtc).ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpGet("api/exames/{id:guid}")]
    public async Task<ActionResult<ExameLaboratorialResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await QueryCompleta().FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Exame laboratorial nao encontrado." }) : Ok(ToResponse(item));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/exames")]
    public async Task<ActionResult<ExameLaboratorialResponse>> Create(Guid pacienteId, UpsertExameLaboratorialRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Conflict(new { message = "Cadastre seu perfil profissional antes de registrar exames." });
        var validacao = await Validar(request, ct);
        if (validacao is not null) return validacao;

        var item = new ExameLaboratorial
        {
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            DataColetaUtc = (request.DataColetaUtc ?? DateTime.UtcNow).ToUniversalTime(),
            Laboratorio = Limpar(request.Laboratorio),
            Observacoes = Limpar(request.Observacoes)
        };
        await SincronizarResultados(item, request.Resultados, ct);
        db.ExamesLaboratoriais.Add(item);
        Auditar("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);
        var criado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(criado));
    }

    [HttpPut("api/exames/{id:guid}")]
    public async Task<ActionResult<ExameLaboratorialResponse>> Update(Guid id, UpsertExameLaboratorialRequest request, CancellationToken ct)
    {
        var item = await db.ExamesLaboratoriais.Include(x => x.Resultados).ThenInclude(x => x.MarcadorLaboratorial)
            .FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Exame laboratorial nao encontrado." });
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null || item.ProfissionalId != profissional.Id) return Forbid();
        var validacao = await Validar(request, ct);
        if (validacao is not null) return validacao;

        var antes = Snapshot(item);
        item.DataColetaUtc = (request.DataColetaUtc ?? item.DataColetaUtc).ToUniversalTime();
        item.Laboratorio = Limpar(request.Laboratorio);
        item.Observacoes = Limpar(request.Observacoes);
        db.ResultadosExamesLaboratoriais.RemoveRange(item.Resultados);
        item.Resultados.Clear();
        await SincronizarResultados(item, request.Resultados, ct);
        Auditar("UPDATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return Ok(ToResponse(atualizado));
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/exames/evolucao/{marcadorId:guid}")]
    public async Task<ActionResult<EvolucaoMarcadorResponse>> GetEvolucao(Guid pacienteId, Guid marcadorId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var marcador = await db.MarcadoresLaboratoriais.AsNoTracking().FirstOrDefaultAsync(x => x.Id == marcadorId && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (marcador is null) return NotFound(new { message = "Marcador laboratorial nao encontrado." });

        var resultados = await db.ResultadosExamesLaboratoriais.AsNoTracking()
            .Where(x => x.MarcadorLaboratorialId == marcadorId && x.ValorNumerico != null &&
                        x.ExameLaboratorial.PacienteId == pacienteId &&
                        x.ExameLaboratorial.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.ExameLaboratorial.DataColetaUtc)
            .Select(x => new { x.ExameLaboratorialId, x.ExameLaboratorial.DataColetaUtc, Valor = x.ValorNumerico!.Value, x.Unidade, x.ReferenciaMinima, x.ReferenciaMaxima, x.ExameLaboratorial.Laboratorio })
            .ToListAsync(ct);

        return Ok(new EvolucaoMarcadorResponse(marcador.Id, marcador.Nome, marcador.UnidadePadrao,
            resultados.Select(x => new EvolucaoMarcadorPontoResponse(x.ExameLaboratorialId, x.DataColetaUtc, x.Valor, x.Unidade,
                x.ReferenciaMinima, x.ReferenciaMaxima, CalcularSituacao(x.Valor, x.ReferenciaMinima, x.ReferenciaMaxima), x.Laboratorio)).ToList()));
    }

    private IQueryable<ExameLaboratorial> QueryCompleta() => db.ExamesLaboratoriais.AsNoTracking()
        .Include(x => x.Profissional)
        .Include(x => x.Resultados).ThenInclude(x => x.MarcadorLaboratorial);

    private async Task<ActionResult?> Validar(UpsertExameLaboratorialRequest request, CancellationToken ct)
    {
        if (request.Resultados is null || request.Resultados.Count == 0)
            return BadRequest(new { message = "Informe pelo menos um resultado de exame." });
        var ids = request.Resultados.Select(x => x.MarcadorId).ToArray();
        if (ids.Any(x => x == Guid.Empty)) return BadRequest(new { message = "Todos os resultados devem possuir marcadorId." });
        if (ids.Distinct().Count() != ids.Length) return BadRequest(new { message = "Um marcador pode aparecer apenas uma vez por coleta." });
        if (request.Resultados.Any(x => !x.ValorNumerico.HasValue && string.IsNullOrWhiteSpace(x.ValorTexto)))
            return BadRequest(new { message = "Cada resultado deve informar valorNumerico ou valorTexto." });
        if (request.Resultados.Any(x => x.ReferenciaMinima.HasValue && x.ReferenciaMaxima.HasValue && x.ReferenciaMinima > x.ReferenciaMaxima))
            return BadRequest(new { message = "Referencia minima nao pode ser maior que a maxima." });

        var validos = await db.MarcadoresLaboratoriais.CountAsync(x => ids.Contains(x.Id) && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (validos != ids.Length) return BadRequest(new { message = "Um ou mais marcadores nao existem, pertencem a outra organizacao ou estao inativos." });
        return null;
    }

    private async Task SincronizarResultados(ExameLaboratorial exame, IEnumerable<ResultadoExameRequest> requests, CancellationToken ct)
    {
        var ids = requests.Select(x => x.MarcadorId).Distinct().ToArray();
        var marcadores = await db.MarcadoresLaboratoriais.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var r in requests)
        {
            var marcador = marcadores[r.MarcadorId];
            exame.Resultados.Add(new ResultadoExameLaboratorial
            {
                ExameLaboratorialId = exame.Id,
                MarcadorLaboratorialId = r.MarcadorId,
                ValorNumerico = r.ValorNumerico,
                ValorTexto = Limpar(r.ValorTexto),
                Unidade = Limpar(r.Unidade) ?? marcador.UnidadePadrao,
                ReferenciaMinima = r.ReferenciaMinima,
                ReferenciaMaxima = r.ReferenciaMaxima,
                ReferenciaTexto = Limpar(r.ReferenciaTexto),
                Observacao = Limpar(r.Observacao),
                MarcadorLaboratorial = marcador
            });
        }
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) => await db.Profissionais
        .FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct) => await db.Pacientes.AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private void Auditar(string acao, ExameLaboratorial item, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog
    {
        OrganizacaoId = currentUser.OrganizationId,
        UsuarioId = currentUser.UserId,
        Acao = acao,
        Entidade = nameof(ExameLaboratorial),
        EntidadeId = item.Id.ToString(),
        DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
        DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
        IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
    });

    private static object Snapshot(ExameLaboratorial x) => new
    {
        x.Id, x.PacienteId, x.ProfissionalId, x.DataColetaUtc, x.Laboratorio, x.Observacoes,
        Resultados = x.Resultados.Select(r => new { r.MarcadorLaboratorialId, r.ValorNumerico, r.ValorTexto, r.Unidade, r.ReferenciaMinima, r.ReferenciaMaxima, r.ReferenciaTexto, r.Observacao }).ToList()
    };

    private static ExameLaboratorialResponse ToResponse(ExameLaboratorial x) => new(
        x.Id, x.PacienteId, x.ProfissionalId, x.Profissional.Nome, x.DataColetaUtc, x.Laboratorio, x.Observacoes,
        x.Resultados.OrderBy(r => r.MarcadorLaboratorial.Categoria).ThenBy(r => r.MarcadorLaboratorial.Nome).Select(ToResultadoResponse).ToList(),
        x.CreatedAtUtc, x.UpdatedAtUtc);

    private static ResultadoExameResponse ToResultadoResponse(ResultadoExameLaboratorial x) => new(
        x.Id, x.MarcadorLaboratorialId, x.MarcadorLaboratorial.Nome, x.MarcadorLaboratorial.Categoria,
        x.ValorNumerico, x.ValorTexto, x.Unidade, x.ReferenciaMinima, x.ReferenciaMaxima, x.ReferenciaTexto,
        x.ValorNumerico.HasValue ? CalcularSituacao(x.ValorNumerico.Value, x.ReferenciaMinima, x.ReferenciaMaxima) : null,
        x.Observacao);

    private static string? CalcularSituacao(decimal valor, decimal? min, decimal? max)
    {
        if (min.HasValue && valor < min.Value) return "Baixo";
        if (max.HasValue && valor > max.Value) return "Alto";
        if (min.HasValue || max.HasValue) return "DentroDaReferencia";
        return null;
    }

    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
