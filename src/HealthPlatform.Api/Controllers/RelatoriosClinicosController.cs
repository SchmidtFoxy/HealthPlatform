using System.Net;
using System.Text;
using System.Text.Json;
using HealthPlatform.Api.Contracts.Relatorios;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class RelatoriosClinicosController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("api/pacientes/{pacienteId:guid}/relatorios")]
    public async Task<ActionResult<IReadOnlyCollection<RelatorioClinicoResponse>>> GetByPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var itens = await QueryCompleta().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataGeracaoUtc).ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpGet("api/relatorios/{id:guid}")]
    public async Task<ActionResult<RelatorioClinicoResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await QueryCompleta().FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Relatorio clinico nao encontrado." }) : Ok(ToResponse(item));
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/relatorios/preview")]
    public async Task<ActionResult<RelatorioClinicoConteudoResponse>> Preview(Guid pacienteId, [FromQuery] DateTime? inicioUtc, [FromQuery] DateTime? fimUtc, CancellationToken ct)
    {
        var validacao = ValidarPeriodo(inicioUtc, fimUtc); if (validacao is not null) return validacao;
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        return Ok(await MontarConteudo(pacienteId, inicioUtc, fimUtc, ct));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/relatorios")]
    public async Task<ActionResult<RelatorioClinicoResponse>> Create(Guid pacienteId, CreateRelatorioClinicoRequest request, CancellationToken ct)
    {
        var validacao = ValidarPeriodo(request.DataInicioUtc, request.DataFimUtc); if (validacao is not null) return validacao;
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Conflict(new { message = "Cadastre seu perfil profissional antes de gerar relatorios." });
        var conteudo = await MontarConteudo(pacienteId, request.DataInicioUtc, request.DataFimUtc, ct);
        var item = new RelatorioClinico
        {
            PacienteId = pacienteId, ProfissionalId = profissional.Id,
            DataInicioUtc = request.DataInicioUtc?.ToUniversalTime(), DataFimUtc = request.DataFimUtc?.ToUniversalTime(),
            DataGeracaoUtc = DateTime.UtcNow,
            Titulo = Limpar(request.Titulo) ?? $"Relatorio clinico - {conteudo.Paciente.Nome}",
            ConclusaoMedica = Limpar(request.ConclusaoMedica), VersaoTemplate = "0.1.5",
            ConteudoJson = JsonSerializer.Serialize(conteudo, JsonOptions)
        };
        db.RelatoriosClinicos.Add(item); Auditar("CREATE", item); await db.SaveChangesAsync(ct);
        var criado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(criado));
    }

    [HttpGet("api/relatorios/{id:guid}/html")]
    [Produces("text/html")]
    public async Task<IActionResult> GetHtml(Guid id, CancellationToken ct)
    {
        var item = await QueryCompleta().FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Relatorio clinico nao encontrado." });
        return Content(GerarHtml(ToResponse(item)), "text/html", Encoding.UTF8);
    }

    private async Task<RelatorioClinicoConteudoResponse> MontarConteudo(Guid pacienteId, DateTime? inicioUtc, DateTime? fimUtc, CancellationToken ct)
    {
        var inicio = inicioUtc?.ToUniversalTime(); var fim = fimUtc?.ToUniversalTime();
        var paciente = await db.Pacientes.AsNoTracking().FirstAsync(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct);
        var consultasQ = db.Consultas.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId);
        var avaliacoesQ = db.Avaliacoes.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId);
        var anamnesesQ = db.Anamneses.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId);
        var examesQ = db.ExamesLaboratoriais.AsNoTracking().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId);
        if (inicio.HasValue) { consultasQ=consultasQ.Where(x=>x.DataHoraUtc>=inicio.Value); avaliacoesQ=avaliacoesQ.Where(x=>x.DataUtc>=inicio.Value); anamnesesQ=anamnesesQ.Where(x=>x.DataUtc>=inicio.Value); examesQ=examesQ.Where(x=>x.DataColetaUtc>=inicio.Value); }
        if (fim.HasValue) { consultasQ=consultasQ.Where(x=>x.DataHoraUtc<=fim.Value); avaliacoesQ=avaliacoesQ.Where(x=>x.DataUtc<=fim.Value); anamnesesQ=anamnesesQ.Where(x=>x.DataUtc<=fim.Value); examesQ=examesQ.Where(x=>x.DataColetaUtc<=fim.Value); }
        var consultas=await consultasQ.OrderBy(x=>x.DataHoraUtc).ToListAsync(ct);
        var avaliacoes=await avaliacoesQ.OrderBy(x=>x.DataUtc).ToListAsync(ct);
        var anamnese=await anamnesesQ.OrderByDescending(x=>x.DataUtc).FirstOrDefaultAsync(ct);
        var exames=await examesQ.Include(x=>x.Resultados).ThenInclude(x=>x.MarcadorLaboratorial).OrderBy(x=>x.DataColetaUtc).ToListAsync(ct);
        var primeira=avaliacoes.FirstOrDefault(); var ultima=avaliacoes.LastOrDefault(); decimal? imcAtual=null;
        if (ultima?.PesoKg is decimal peso && ultima.AlturaM is decimal altura && altura>0) imcAtual=Math.Round(peso/(altura*altura),2);
        var todosResultados=exames.SelectMany(e=>e.Resultados.Select(r=>new RelatorioMarcadorResponse(e.Id,e.DataColetaUtc,r.MarcadorLaboratorial.Nome,r.ValorNumerico,r.ValorTexto,r.Unidade,r.ReferenciaMinima,r.ReferenciaMaxima,r.ReferenciaTexto,CalcularSituacao(r.ValorNumerico,r.ReferenciaMinima,r.ReferenciaMaxima),e.Laboratorio))).ToList();
        var recentes=todosResultados.GroupBy(x=>x.Marcador,StringComparer.OrdinalIgnoreCase).Select(g=>g.OrderByDescending(x=>x.DataColetaUtc).First()).OrderBy(x=>x.Marcador).ToList();
        var fora=recentes.Where(x=>x.Situacao is "Baixo" or "Alto").ToList();
        return new RelatorioClinicoConteudoResponse(
            new RelatorioPacienteResponse(paciente.Id,paciente.Nome,paciente.DataNascimento,paciente.Sexo,paciente.Profissao), inicio, fim,
            new RelatorioIndicadoresResponse(consultas.Count,avaliacoes.Count,exames.Count,primeira?.PesoKg,ultima?.PesoKg,Diferenca(ultima?.PesoKg,primeira?.PesoKg),primeira?.PercentualGordura,ultima?.PercentualGordura,Diferenca(ultima?.PercentualGordura,primeira?.PercentualGordura),primeira?.CinturaCm,ultima?.CinturaCm,Diferenca(ultima?.CinturaCm,primeira?.CinturaCm),imcAtual),
            anamnese is null?null:new RelatorioAnamneseResponse(anamnese.DataUtc,anamnese.ObjetivoAcompanhamento,anamnese.SonoHorasMedia,anamnese.SonoQualidade,anamnese.EstresseNivel,anamnese.AtividadeFisica,anamnese.AtividadeFisicaDiasSemana,anamnese.AguaLitrosDia,anamnese.Medicamentos,anamnese.Suplementos,anamnese.Observacoes),
            consultas.OrderByDescending(x=>x.DataHoraUtc).Take(10).Select(x=>new RelatorioConsultaResponse(x.Id,x.DataHoraUtc,x.Motivo,x.QueixaPrincipal,x.Evolucao,x.Conduta,x.Status.ToString())).ToList(), recentes, fora);
    }

    private IQueryable<RelatorioClinico> QueryCompleta()=>db.RelatoriosClinicos.AsNoTracking().Include(x=>x.Paciente).Include(x=>x.Profissional);
    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct)=>await db.Profissionais.FirstOrDefaultAsync(x=>x.UsuarioId==currentUser.UserId&&x.OrganizacaoId==currentUser.OrganizationId&&x.Ativo,ct);
    private async Task<bool> PacienteExiste(Guid id,CancellationToken ct)=>await db.Pacientes.AnyAsync(x=>x.Id==id&&x.OrganizacaoId==currentUser.OrganizationId&&x.Ativo,ct);
    private ActionResult? ValidarPeriodo(DateTime? inicio,DateTime? fim)=>inicio.HasValue&&fim.HasValue&&inicio.Value.ToUniversalTime()>fim.Value.ToUniversalTime()?BadRequest(new{message="Data inicial nao pode ser posterior a data final."}):null;
    private void Auditar(string acao,RelatorioClinico item)=>db.AuditLogs.Add(new AuditLog{OrganizacaoId=currentUser.OrganizationId,UsuarioId=currentUser.UserId,Acao=acao,Entidade=nameof(RelatorioClinico),EntidadeId=item.Id.ToString(),DadosNovosJson=JsonSerializer.Serialize(new{item.Id,item.PacienteId,item.ProfissionalId,item.Titulo,item.DataInicioUtc,item.DataFimUtc,item.DataGeracaoUtc,item.VersaoTemplate}),IpAddress=httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()});
    private static RelatorioClinicoResponse ToResponse(RelatorioClinico x){var c=JsonSerializer.Deserialize<RelatorioClinicoConteudoResponse>(x.ConteudoJson,JsonOptions)??throw new InvalidOperationException("Conteudo do relatorio invalido.");return new(x.Id,x.PacienteId,x.ProfissionalId,x.Profissional.Nome,x.Titulo,x.DataInicioUtc,x.DataFimUtc,x.DataGeracaoUtc,x.ConclusaoMedica,x.VersaoTemplate,c,x.CreatedAtUtc);}
    private static decimal? Diferenca(decimal? atual,decimal? inicial)=>atual.HasValue&&inicial.HasValue?Math.Round(atual.Value-inicial.Value,2):null;
    private static string? CalcularSituacao(decimal? valor,decimal? min,decimal? max){if(!valor.HasValue)return null;if(min.HasValue&&valor.Value<min.Value)return "Baixo";if(max.HasValue&&valor.Value>max.Value)return "Alto";return min.HasValue||max.HasValue?"DentroDaReferencia":null;}
    private static string? Limpar(string? valor)=>string.IsNullOrWhiteSpace(valor)?null:valor.Trim();

    private static string GerarHtml(RelatorioClinicoResponse r)
    {
        static string H(string? v)=>WebUtility.HtmlEncode(v??"-"); static string N(decimal? v,string unidade="")=>v.HasValue?$"{v.Value:0.##}{(string.IsNullOrWhiteSpace(unidade)?"":" "+unidade)}":"-";
        var c=r.Conteudo; var sb=new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>").Append(H(r.Titulo)).Append("</title><style>body{font-family:Arial,sans-serif;max-width:1000px;margin:40px auto;color:#222}h1,h2{margin-bottom:6px}.muted{color:#666}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.card{border:1px solid #ddd;border-radius:8px;padding:12px}table{width:100%;border-collapse:collapse;margin-top:10px}th,td{border-bottom:1px solid #ddd;padding:8px;text-align:left}th{background:#f6f6f6}@media print{body{margin:0}.no-print{display:none}}</style></head><body>");
        sb.Append("<button class='no-print' onclick='window.print()'>Imprimir / salvar em PDF</button><h1>").Append(H(r.Titulo)).Append("</h1><div class='muted'>Paciente: ").Append(H(c.Paciente.Nome)).Append(" | Gerado em ").Append(r.DataGeracaoUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm")).Append("</div>");
        sb.Append("<h2>Indicadores</h2><div class='grid'><div class='card'><b>Peso atual</b><br>").Append(N(c.Indicadores.PesoAtualKg,"kg")).Append("<br><small>Variacao: ").Append(N(c.Indicadores.VariacaoPesoKg,"kg")).Append("</small></div><div class='card'><b>Gordura corporal</b><br>").Append(N(c.Indicadores.PercentualGorduraAtual,"%")).Append("<br><small>Variacao: ").Append(N(c.Indicadores.VariacaoPercentualGordura,"p.p.")).Append("</small></div><div class='card'><b>Cintura</b><br>").Append(N(c.Indicadores.CinturaAtualCm,"cm")).Append("<br><small>Variacao: ").Append(N(c.Indicadores.VariacaoCinturaCm,"cm")).Append("</small></div></div>");
        if(c.UltimaAnamnese is not null)sb.Append("<h2>Ultima anamnese</h2><p><b>Objetivo:</b> ").Append(H(c.UltimaAnamnese.ObjetivoAcompanhamento)).Append("</p><p><b>Sono:</b> ").Append(N(c.UltimaAnamnese.SonoHorasMedia,"h")).Append(" / ").Append(H(c.UltimaAnamnese.SonoQualidade)).Append(" &nbsp; <b>Estresse:</b> ").Append(c.UltimaAnamnese.EstresseNivel?.ToString()??"-").Append("/10</p>");
        sb.Append("<h2>Exames recentes</h2><table><tr><th>Marcador</th><th>Valor</th><th>Faixa informada</th><th>Situacao</th></tr>");
        foreach(var x in c.ExamesRecentes){var valor=x.ValorNumerico.HasValue?N(x.ValorNumerico,x.Unidade??""):H(x.ValorTexto);var faixa=x.ReferenciaTexto??$"{N(x.ReferenciaMinima)} - {N(x.ReferenciaMaxima)}";sb.Append("<tr><td>").Append(H(x.Marcador)).Append("</td><td>").Append(valor).Append("</td><td>").Append(H(faixa)).Append("</td><td>").Append(H(x.Situacao)).Append("</td></tr>");}
        sb.Append("</table>"); if(!string.IsNullOrWhiteSpace(r.ConclusaoMedica))sb.Append("<h2>Conclusao do profissional</h2><p>").Append(H(r.ConclusaoMedica)).Append("</p>");
        sb.Append("<hr><div class='muted'>Profissional: ").Append(H(r.ProfissionalNome)).Append(" | Template ").Append(H(r.VersaoTemplate)).Append("</div></body></html>"); return sb.ToString();
    }
}
