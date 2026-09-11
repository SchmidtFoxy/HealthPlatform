using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record SalvarProtocoloItemRequest(
    string Tipo, string Titulo, string? Unidade, string Frequencia,
    string? HorarioLocal, string? DiasSemana, string? Instrucoes, bool Ativo = true);

[ApiController]
public sealed class ProtocolosAcompanhamentoController(
    AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Peso", "Pressao", "Glicemia", "FrequenciaCardiaca", "Saturacao",
        "Temperatura", "Agua", "Sono", "Dor", "Energia", "Sintoma"
    };
    private static readonly HashSet<string> FrequenciasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    { "Diario", "DiasSemana", "Semanal", "SobDemanda" };

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/protocolo-acompanhamento")]
    public async Task<IActionResult> Listar(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var entidades = await Query(pacienteId).Include(x => x.Profissional).ToListAsync(ct);
        var itens = entidades.Select(x => ToResponse(x, x.Profissional.Nome)).ToList();
        return Ok(new { pacienteId, ativos = entidades.Count(x => x.Ativo), itens });
    }

    [Authorize]
    [HttpPost("api/pacientes/{pacienteId:guid}/protocolo-acompanhamento")]
    public async Task<IActionResult> Criar(Guid pacienteId, SalvarProtocoloItemRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var profissional = await ProfissionalAtual(ct);
        if (profissional is null) return Forbid();
        var erro = Validar(request); if (erro is not null) return BadRequest(new { message = erro });
        var item = new ProtocoloAcompanhamentoItem { OrganizacaoId=currentUser.OrganizationId, PacienteId=pacienteId, ProfissionalId=profissional.Id };
        Aplicar(item, request);
        db.ProtocolosAcompanhamento.Add(item);
        Auditar("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(item));
    }

    [Authorize]
    [HttpPut("api/protocolos-acompanhamento/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, SalvarProtocoloItemRequest request, CancellationToken ct)
    {
        if (await ProfissionalAtual(ct) is null) return Forbid();
        var item = await db.ProtocolosAcompanhamento.FirstOrDefaultAsync(x => x.Id==id && x.OrganizacaoId==currentUser.OrganizationId, ct);
        if (item is null) return NotFound();
        var erro=Validar(request); if (erro is not null) return BadRequest(new { message=erro });
        var antes=Snapshot(item); Aplicar(item,request); Auditar("UPDATE",item,antes,Snapshot(item));
        await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }

    [Authorize]
    [HttpDelete("api/protocolos-acompanhamento/{id:guid}")]
    public async Task<IActionResult> Desativar(Guid id, CancellationToken ct)
    {
        if (await ProfissionalAtual(ct) is null) return Forbid();
        var item=await db.ProtocolosAcompanhamento.FirstOrDefaultAsync(x=>x.Id==id && x.OrganizacaoId==currentUser.OrganizationId,ct);
        if(item is null)return NotFound(); var antes=Snapshot(item); item.Ativo=false; Auditar("DEACTIVATE",item,antes,Snapshot(item));
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Policy="PatientOnly")]
    [HttpGet("api/portal/me/protocolo-acompanhamento")]
    public async Task<IActionResult> MeuProtocolo(CancellationToken ct)
    {
        var pacienteId=await db.Pacientes.AsNoTracking().Where(x=>x.UsuarioId==currentUser.UserId && x.OrganizacaoId==currentUser.OrganizationId && x.Ativo).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync(ct);
        if(!pacienteId.HasValue)return NotFound(new { message="Paciente vinculado nao encontrado." });
        var entidades=await Query(pacienteId.Value).Where(x=>x.Ativo).Include(x=>x.Profissional).ToListAsync(ct);
        var itens=entidades.Select(x=>ToResponse(x,x.Profissional.Nome)).ToList();
        return Ok(new { pacienteId=pacienteId.Value, configurado=itens.Count>0, itens });
    }

    private IQueryable<ProtocoloAcompanhamentoItem> Query(Guid pacienteId) => db.ProtocolosAcompanhamento.AsNoTracking()
        .Where(x=>x.OrganizacaoId==currentUser.OrganizationId && x.PacienteId==pacienteId)
        .OrderByDescending(x=>x.Ativo).ThenBy(x=>x.Titulo);

    private async Task<bool> PacienteExiste(Guid id,CancellationToken ct)=>await db.Pacientes.AsNoTracking().AnyAsync(x=>x.Id==id&&x.OrganizacaoId==currentUser.OrganizationId&&x.Ativo,ct);
    private async Task<Profissional?> ProfissionalAtual(CancellationToken ct)=>await db.Profissionais.FirstOrDefaultAsync(x=>x.UsuarioId==currentUser.UserId&&x.OrganizacaoId==currentUser.OrganizationId&&x.Ativo,ct);
    private static string? Validar(SalvarProtocoloItemRequest r){ if(!TiposPermitidos.Contains(r.Tipo?.Trim()??""))return "Tipo de monitoramento invalido."; if(string.IsNullOrWhiteSpace(r.Titulo)||r.Titulo.Trim().Length>180)return "Titulo invalido."; if(!FrequenciasPermitidas.Contains(r.Frequencia?.Trim()??""))return "Frequencia invalida."; if((r.Instrucoes?.Length??0)>1200)return "Instrucoes excedem 1200 caracteres."; if(r.HorarioLocal is not null && !System.Text.RegularExpressions.Regex.IsMatch(r.HorarioLocal,@"^([01]\d|2[0-3]):[0-5]\d$"))return "Horario deve usar HH:mm."; return null; }
    private static void Aplicar(ProtocoloAcompanhamentoItem x,SalvarProtocoloItemRequest r){x.Tipo=r.Tipo.Trim();x.Titulo=r.Titulo.Trim();x.Unidade=Limpar(r.Unidade);x.Frequencia=r.Frequencia.Trim();x.HorarioLocal=Limpar(r.HorarioLocal);x.DiasSemana=Limpar(r.DiasSemana);x.Instrucoes=Limpar(r.Instrucoes);x.Ativo=r.Ativo;}
    private static string? Limpar(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
    private static object Snapshot(ProtocoloAcompanhamentoItem x)=>new{x.Id,x.PacienteId,x.ProfissionalId,x.Tipo,x.Titulo,x.Unidade,x.Frequencia,x.HorarioLocal,x.DiasSemana,x.Instrucoes,x.Ativo};
    private static object ToResponse(ProtocoloAcompanhamentoItem x,string? profissionalNome=null)=>new{x.Id,x.PacienteId,x.ProfissionalId,profissionalNome,x.Tipo,x.Titulo,x.Unidade,x.Frequencia,x.HorarioLocal,x.DiasSemana,x.Instrucoes,x.Ativo,x.CreatedAtUtc,x.UpdatedAtUtc};
    private void Auditar(string acao,ProtocoloAcompanhamentoItem x,object? antes,object? depois)=>db.AuditLogs.Add(new AuditLog{OrganizacaoId=currentUser.OrganizationId,UsuarioId=currentUser.UserId,Acao=acao,Entidade=nameof(ProtocoloAcompanhamentoItem),EntidadeId=x.Id.ToString(),DadosAnterioresJson=antes is null?null:JsonSerializer.Serialize(antes),DadosNovosJson=depois is null?null:JsonSerializer.Serialize(depois),IpAddress=httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()});
}
