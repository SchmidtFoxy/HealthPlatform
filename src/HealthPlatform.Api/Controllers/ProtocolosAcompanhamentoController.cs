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
    public async Task<IActionResult> Listar(Guid pacienteId, [FromQuery] int offsetMinutos = 0, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var entidades = await Query(pacienteId).Include(x => x.Profissional).ToListAsync(ct);
        var itens = entidades.Select(x => ToResponse(x, x.Profissional.Nome)).ToList();
        var aderencia = await CalcularAderencia(pacienteId, entidades.Where(x => x.Ativo).ToList(), offsetMinutos, 7, ct);
        return Ok(new { pacienteId, ativos = entidades.Count(x => x.Ativo), itens, aderencia });
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
    public async Task<IActionResult> MeuProtocolo([FromQuery] int offsetMinutos = 0, CancellationToken ct = default)
    {
        var pacienteId=await db.Pacientes.AsNoTracking().Where(x=>x.UsuarioId==currentUser.UserId && x.OrganizacaoId==currentUser.OrganizationId && x.Ativo).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync(ct);
        if(!pacienteId.HasValue)return NotFound(new { message="Paciente vinculado nao encontrado." });
        var entidades=await Query(pacienteId.Value).Where(x=>x.Ativo).Include(x=>x.Profissional).ToListAsync(ct);
        var itens=entidades.Select(x=>ToResponse(x,x.Profissional.Nome)).ToList();
        var aderencia=await CalcularAderencia(pacienteId.Value, entidades, offsetMinutos, 7, ct);
        return Ok(new { pacienteId=pacienteId.Value, configurado=itens.Count>0, itens, aderencia });
    }

    private async Task<object> CalcularAderencia(Guid pacienteId, IReadOnlyCollection<ProtocoloAcompanhamentoItem> protocolo, int offsetMinutos, int dias, CancellationToken ct)
    {
        offsetMinutos = Math.Clamp(offsetMinutos, -840, 840);
        dias = Math.Clamp(dias, 1, 30);
        var hojeLocal = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(offsetMinutos));
        var inicioLocal = hojeLocal.AddDays(-(dias - 1));
        var inicioUtc = DateTime.SpecifyKind(inicioLocal.ToDateTime(TimeOnly.MinValue).AddMinutes(-offsetMinutos), DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(hojeLocal.AddDays(1).ToDateTime(TimeOnly.MinValue).AddMinutes(-offsetMinutos), DateTimeKind.Utc);
        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.DataHoraUtc >= inicioUtc && x.DataHoraUtc < fimUtc)
            .Select(x => new { x.DataHoraUtc, x.Tipo })
            .ToListAsync(ct);

        var previstos = 0;
        var concluidos = 0;
        var hojePrevistos = 0;
        var hojeConcluidos = 0;
        var statusHoje = new List<object>();

        foreach (var diaOffset in Enumerable.Range(0, dias))
        {
            var dia = inicioLocal.AddDays(diaOffset);
            foreach (var item in protocolo.Where(x => x.Ativo && DeveExecutarNoDia(x, dia, offsetMinutos)))
            {
                previstos++;
                var concluido = RegistroCumpre(item.Tipo, registros.Where(r => DateOnly.FromDateTime(r.DataHoraUtc.AddMinutes(offsetMinutos)) == dia).Select(r => r.Tipo));
                if (concluido) concluidos++;
                if (dia == hojeLocal)
                {
                    hojePrevistos++;
                    if (concluido) hojeConcluidos++;
                    statusHoje.Add(new { item.Id, item.Tipo, item.Titulo, item.HorarioLocal, concluido });
                }
            }
        }

        return new
        {
            dias,
            previstos,
            concluidos,
            percentual = previstos == 0 ? (int?)null : (int)Math.Round(concluidos * 100m / previstos),
            hoje = new { previstos = hojePrevistos, concluidos = hojeConcluidos, percentual = hojePrevistos == 0 ? (int?)null : (int)Math.Round(hojeConcluidos * 100m / hojePrevistos), itens = statusHoje }
        };
    }

    private static bool DeveExecutarNoDia(ProtocoloAcompanhamentoItem item, DateOnly dia, int offsetMinutos)
    {
        if (item.Frequencia.Equals("SobDemanda", StringComparison.OrdinalIgnoreCase)) return false;
        if (item.Frequencia.Equals("Diario", StringComparison.OrdinalIgnoreCase)) return true;
        if (item.Frequencia.Equals("Semanal", StringComparison.OrdinalIgnoreCase)) return dia.DayOfWeek == item.CreatedAtUtc.AddMinutes(offsetMinutos).DayOfWeek;
        if (!item.Frequencia.Equals("DiasSemana", StringComparison.OrdinalIgnoreCase)) return false;
        var token = dia.DayOfWeek switch
        {
            DayOfWeek.Monday => "Seg", DayOfWeek.Tuesday => "Ter", DayOfWeek.Wednesday => "Qua",
            DayOfWeek.Thursday => "Qui", DayOfWeek.Friday => "Sex", DayOfWeek.Saturday => "Sab", _ => "Dom"
        };
        return (item.DiasSemana ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(x => x.StartsWith(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool RegistroCumpre(string tipoProtocolo, IEnumerable<string> tiposRegistrados)
    {
        var tipos = tiposRegistrados.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (tipoProtocolo.Equals("Pressao", StringComparison.OrdinalIgnoreCase))
            return tipos.Contains("PressaoSistolica") && tipos.Contains("PressaoDiastolica");
        if (tipoProtocolo.Equals("Sintoma", StringComparison.OrdinalIgnoreCase))
            return tipos.Contains("Sintoma") || tipos.Contains("Observacao");
        return tipos.Contains(tipoProtocolo);
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
