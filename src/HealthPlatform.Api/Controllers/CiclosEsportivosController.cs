using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public sealed class CiclosEsportivosController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record UpsertCicloEsportivoRequest(string Nome, string PerfilEsportivo, string? Objetivo, DateOnly DataInicio, DateOnly DataFim,
        string Status, int? MetaTreinosSemanais, int? MetaConsistenciaPercentual, decimal? MetaPesoKg, Guid? FaseTreinoId, Guid? FaseNutricionalId, string? Observacoes);

    [HttpGet("api/pacientes/{pacienteId:guid}/ciclos-esportivos")]
    public async Task<IActionResult> Listar(Guid pacienteId, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var ciclos = await db.CiclosEsportivosPaciente.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataInicio).Select(x => new { x.Id, x.Nome, x.PerfilEsportivo, x.Objetivo, x.DataInicio, x.DataFim, x.Status, x.MetaTreinosSemanais, x.MetaConsistenciaPercentual, x.MetaPesoKg, x.FaseTreinoId, x.FaseNutricionalId, x.Observacoes }).ToListAsync(ct);
        return Ok(ciclos);
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/ciclos-esportivos")]
    public async Task<IActionResult> Criar(Guid pacienteId, UpsertCicloEsportivoRequest request, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        var profissional = await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (profissional is null) return Conflict(new { message = "Perfil profissional ativo nao encontrado." });
        if (!await VinculosValidos(pacienteId, request.FaseTreinoId, request.FaseNutricionalId, ct)) return BadRequest(new { message = "Fase vinculada nao pertence ao paciente/organizacao." });
        if (NormalizarStatus(request.Status) == "Ativo") await EncerrarOutrosAtivos(pacienteId, null, ct);
        var ciclo = new CicloEsportivoPaciente { OrganizacaoId = currentUser.OrganizationId, PacienteId = pacienteId, ProfissionalId = profissional.Id, Nome = request.Nome.Trim(), PerfilEsportivo = NormalizarPerfil(request.PerfilEsportivo), Objetivo = Limpar(request.Objetivo), DataInicio = request.DataInicio, DataFim = request.DataFim, Status = NormalizarStatus(request.Status), MetaTreinosSemanais = request.MetaTreinosSemanais, MetaConsistenciaPercentual = request.MetaConsistenciaPercentual, MetaPesoKg = request.MetaPesoKg, FaseTreinoId = request.FaseTreinoId, FaseNutricionalId = request.FaseNutricionalId, Observacoes = Limpar(request.Observacoes) };
        db.CiclosEsportivosPaciente.Add(ciclo); Auditar("CREATE", ciclo.Id, null, ciclo); await db.SaveChangesAsync(ct); return Ok(ciclo);
    }

    [HttpPut("api/ciclos-esportivos/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, UpsertCicloEsportivoRequest request, CancellationToken ct = default)
    {
        var ciclo = await db.CiclosEsportivosPaciente.FirstOrDefaultAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (ciclo is null) return NotFound(new { message = "Ciclo esportivo nao encontrado." });
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        if (!await VinculosValidos(ciclo.PacienteId, request.FaseTreinoId, request.FaseNutricionalId, ct)) return BadRequest(new { message = "Fase vinculada nao pertence ao paciente/organizacao." });
        if (NormalizarStatus(request.Status) == "Ativo") await EncerrarOutrosAtivos(ciclo.PacienteId, ciclo.Id, ct);
        var antes = new { ciclo.Nome, ciclo.PerfilEsportivo, ciclo.Objetivo, ciclo.DataInicio, ciclo.DataFim, ciclo.Status, ciclo.MetaTreinosSemanais, ciclo.MetaConsistenciaPercentual, ciclo.MetaPesoKg };
        ciclo.Nome=request.Nome.Trim(); ciclo.PerfilEsportivo=NormalizarPerfil(request.PerfilEsportivo); ciclo.Objetivo=Limpar(request.Objetivo); ciclo.DataInicio=request.DataInicio; ciclo.DataFim=request.DataFim; ciclo.Status=NormalizarStatus(request.Status); ciclo.MetaTreinosSemanais=request.MetaTreinosSemanais; ciclo.MetaConsistenciaPercentual=request.MetaConsistenciaPercentual; ciclo.MetaPesoKg=request.MetaPesoKg; ciclo.FaseTreinoId=request.FaseTreinoId; ciclo.FaseNutricionalId=request.FaseNutricionalId; ciclo.Observacoes=Limpar(request.Observacoes); ciclo.UpdatedAtUtc=DateTime.UtcNow;
        Auditar("UPDATE", ciclo.Id, antes, ciclo); await db.SaveChangesAsync(ct); return Ok(ciclo);
    }

    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct) => await db.Pacientes.AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private async Task<bool> VinculosValidos(Guid pacienteId, Guid? ft, Guid? fn, CancellationToken ct) =>
        (!ft.HasValue || await db.FasesTreino.AnyAsync(x => x.Id == ft && x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct)) &&
        (!fn.HasValue || await db.FasesNutricionais.AnyAsync(x => x.Id == fn && x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct));
    private async Task EncerrarOutrosAtivos(Guid pacienteId, Guid? exceto, CancellationToken ct) { var ativos=await db.CiclosEsportivosPaciente.Where(x=>x.PacienteId==pacienteId && x.OrganizacaoId==currentUser.OrganizationId && x.Status=="Ativo" && (!exceto.HasValue || x.Id!=exceto.Value)).ToListAsync(ct); foreach(var x in ativos){x.Status="Concluido";x.UpdatedAtUtc=DateTime.UtcNow;} }
    private static string? Validar(UpsertCicloEsportivoRequest r) { if(string.IsNullOrWhiteSpace(r.Nome)) return "Informe o nome do ciclo."; if(r.DataFim<r.DataInicio) return "A data final deve ser igual ou posterior a inicial."; if(r.MetaTreinosSemanais is <1 or >14) return "Meta de treinos semanais deve ficar entre 1 e 14."; if(r.MetaConsistenciaPercentual is <0 or >100) return "Meta de consistencia deve ficar entre 0 e 100."; if(r.MetaPesoKg is <=0 or >500) return "Meta de peso invalida."; return null; }
    private static string NormalizarStatus(string? v) => (v??"").Trim().ToLowerInvariant() switch { "ativo" => "Ativo", "concluido" => "Concluido", "cancelado" => "Cancelado", _ => "Planejado" };
    private static string NormalizarPerfil(string? v) { var s=(v??"").Trim(); return string.IsNullOrWhiteSpace(s)?"QualidadeDeVida":s.Length<=50?s:s[..50]; }
    private static string? Limpar(string? v) => string.IsNullOrWhiteSpace(v)?null:v.Trim();
    private void Auditar(string acao, Guid id, object? antes, object? depois) { db.AuditLogs.Add(new AuditLog { OrganizacaoId=currentUser.OrganizationId, UsuarioId=currentUser.UserId, Acao=acao, Entidade=nameof(CicloEsportivoPaciente), EntidadeId=id.ToString(), DadosAnterioresJson=antes is null?null:JsonSerializer.Serialize(antes), DadosNovosJson=depois is null?null:JsonSerializer.Serialize(depois), IpAddress=httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() }); }
}
