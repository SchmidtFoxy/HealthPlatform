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
public sealed class ProgramasTreinoController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record ProgramaDiaRequest(string Dia, Guid? ModeloPlanoTreinoId, string? Observacoes);
    public sealed record ProgramaFaseRequest(string Nome, int Semanas, int Ordem, string? Observacoes, IReadOnlyCollection<ProgramaDiaRequest> Dias);
    public sealed record ProgramaConteudo(string? Observacoes, IReadOnlyCollection<ProgramaFaseRequest> Fases);
    public sealed record SalvarProgramaRequest(string Nome, string? Objetivo, string? Descricao, string? Observacoes, bool Ativo, IReadOnlyCollection<ProgramaFaseRequest> Fases);

    [HttpGet("api/programas-treino")]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInativos=false,[FromQuery] string? busca=null,CancellationToken ct=default)
    {
        var query=db.ProgramasTreinoModelo.AsNoTracking().Where(x=>x.OrganizacaoId==currentUser.OrganizationId);
        if(!incluirInativos) query=query.Where(x=>x.Ativo);
        if(!string.IsNullOrWhiteSpace(busca)) { var termo=$"%{busca.Trim()}%"; query=query.Where(x=>EF.Functions.ILike(x.Nome,termo)||(x.Objetivo!=null&&EF.Functions.ILike(x.Objetivo,termo))||(x.Descricao!=null&&EF.Functions.ILike(x.Descricao,termo))); }
        var itens=await query.Include(x=>x.Profissional).OrderByDescending(x=>x.Ativo).ThenByDescending(x=>x.UpdatedAtUtc??x.CreatedAtUtc).ToListAsync(ct);
        return Ok(itens.Select(x=>ToResponse(x)).ToList());
    }

    [HttpGet("api/programas-treino/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id,CancellationToken ct=default)
    {
        var item=await db.ProgramasTreinoModelo.AsNoTracking().Include(x=>x.Profissional).FirstOrDefaultAsync(x=>x.Id==id&&x.OrganizacaoId==currentUser.OrganizationId,ct);
        return item is null?NotFound(new{message="Programa de treino nao encontrado."}):Ok(ToResponse(item));
    }

    [HttpPost("api/programas-treino")]
    public async Task<IActionResult> Criar(SalvarProgramaRequest request,CancellationToken ct=default)
    {
        var erro=await Validar(request,ct); if(erro is not null) return BadRequest(new{message=erro});
        var profissional=await GetProfissionalAtual(ct); if(profissional is null) return Conflict(new{message="Perfil profissional ativo nao encontrado."});
        var conteudo=new ProgramaConteudo(Limpar(request.Observacoes),NormalizarFases(request.Fases));
        var item=new ProgramaTreinoModelo{OrganizacaoId=currentUser.OrganizationId,ProfissionalId=profissional.Id,Nome=request.Nome.Trim(),Objetivo=Limpar(request.Objetivo),Descricao=Limpar(request.Descricao),ConteudoJson=JsonSerializer.Serialize(conteudo),Ativo=request.Ativo};
        db.ProgramasTreinoModelo.Add(item); Auditar("CREATE",item,null,Resumo(item,conteudo)); await db.SaveChangesAsync(ct); return Ok(ToResponse(item,profissional.Nome));
    }

    [HttpPut("api/programas-treino/{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id,SalvarProgramaRequest request,CancellationToken ct=default)
    {
        var item=await db.ProgramasTreinoModelo.Include(x=>x.Profissional).FirstOrDefaultAsync(x=>x.Id==id&&x.OrganizacaoId==currentUser.OrganizationId,ct);
        if(item is null) return NotFound(new{message="Programa de treino nao encontrado."});
        var erro=await Validar(request,ct); if(erro is not null) return BadRequest(new{message=erro});
        var antes=new{item.Nome,item.Objetivo,item.Descricao,item.Ativo,item.ConteudoJson};
        var conteudo=new ProgramaConteudo(Limpar(request.Observacoes),NormalizarFases(request.Fases));
        item.Nome=request.Nome.Trim(); item.Objetivo=Limpar(request.Objetivo); item.Descricao=Limpar(request.Descricao); item.ConteudoJson=JsonSerializer.Serialize(conteudo); item.Ativo=request.Ativo; item.UpdatedAtUtc=DateTime.UtcNow;
        Auditar("UPDATE",item,antes,Resumo(item,conteudo)); await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }

    [HttpPost("api/programas-treino/{id:guid}/duplicar")]
    public async Task<IActionResult> Duplicar(Guid id,CancellationToken ct=default)
    {
        var origem=await db.ProgramasTreinoModelo.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id&&x.OrganizacaoId==currentUser.OrganizationId,ct);
        if(origem is null) return NotFound(new{message="Programa de treino nao encontrado."});
        var profissional=await GetProfissionalAtual(ct); if(profissional is null) return Conflict(new{message="Perfil profissional ativo nao encontrado."});
        var copia=new ProgramaTreinoModelo{OrganizacaoId=currentUser.OrganizationId,ProfissionalId=profissional.Id,Nome=$"{origem.Nome} - copia",Objetivo=origem.Objetivo,Descricao=origem.Descricao,ConteudoJson=origem.ConteudoJson,Ativo=true};
        db.ProgramasTreinoModelo.Add(copia); Auditar("DUPLICATE",copia,null,new{OrigemId=origem.Id,copia.Nome}); await db.SaveChangesAsync(ct); return Ok(ToResponse(copia,profissional.Nome));
    }

    [HttpDelete("api/programas-treino/{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id,CancellationToken ct=default)
    {
        var item=await db.ProgramasTreinoModelo.FirstOrDefaultAsync(x=>x.Id==id&&x.OrganizacaoId==currentUser.OrganizationId,ct);
        if(item is null) return NotFound(new{message="Programa de treino nao encontrado."});
        Auditar("DELETE",item,new{item.Nome,item.Objetivo,item.Descricao,item.ConteudoJson},null); db.ProgramasTreinoModelo.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<string?> Validar(SalvarProgramaRequest request,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(request.Nome)) return "Nome do programa e obrigatorio.";
        if(request.Fases is null||request.Fases.Count==0) return "Adicione pelo menos uma fase ao programa.";
        if(request.Fases.Any(x=>string.IsNullOrWhiteSpace(x.Nome))) return "Todas as fases precisam de nome.";
        if(request.Fases.Any(x=>x.Semanas<=0||x.Semanas>52)) return "A duracao de cada fase deve ficar entre 1 e 52 semanas.";
        var ids=request.Fases.SelectMany(x=>x.Dias??Array.Empty<ProgramaDiaRequest>()).Where(x=>x.ModeloPlanoTreinoId.HasValue).Select(x=>x.ModeloPlanoTreinoId!.Value).Distinct().ToArray();
        if(ids.Length==0) return "Associe pelo menos um treino-modelo ao programa.";
        var validos=await db.ModelosPlanosTreino.AsNoTracking().Where(x=>ids.Contains(x.Id)&&x.OrganizacaoId==currentUser.OrganizationId&&x.Ativo).Select(x=>x.Id).ToListAsync(ct);
        if(ids.Except(validos).Any()) return "O programa possui treinos-modelo inativos ou indisponiveis.";
        return null;
    }

    private static IReadOnlyCollection<ProgramaFaseRequest> NormalizarFases(IReadOnlyCollection<ProgramaFaseRequest> fases)=>fases.OrderBy(x=>x.Ordem).Select((f,i)=>new ProgramaFaseRequest(f.Nome.Trim(),Math.Clamp(f.Semanas,1,52),i+1,Limpar(f.Observacoes),(f.Dias??Array.Empty<ProgramaDiaRequest>()).Select(d=>new ProgramaDiaRequest(d.Dia.Trim(),d.ModeloPlanoTreinoId,Limpar(d.Observacoes))).ToList())).ToList();
    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct)=>await db.Profissionais.FirstOrDefaultAsync(x=>x.UsuarioId==currentUser.UserId&&x.OrganizacaoId==currentUser.OrganizationId&&x.Ativo,ct);
    private object ToResponse(ProgramaTreinoModelo x,string? profissionalNome=null)
    {
        ProgramaConteudo? conteudo=null; try{conteudo=JsonSerializer.Deserialize<ProgramaConteudo>(x.ConteudoJson);}catch{}
        var fases=conteudo?.Fases??Array.Empty<ProgramaFaseRequest>();
        var ids=fases.SelectMany(f=>f.Dias??Array.Empty<ProgramaDiaRequest>()).Where(d=>d.ModeloPlanoTreinoId.HasValue).Select(d=>d.ModeloPlanoTreinoId!.Value).Distinct().ToArray();
        return new{x.Id,x.Nome,x.Objetivo,x.Descricao,observacoes=conteudo?.Observacoes,x.Ativo,x.ProfissionalId,profissionalNome=profissionalNome??x.Profissional?.Nome,fases,semanas=fases.Sum(f=>f.Semanas),sessoes=fases.Sum(f=>f.Dias.Count(d=>d.ModeloPlanoTreinoId.HasValue)),modelosDistintos=ids.Length,x.CreatedAtUtc,x.UpdatedAtUtc};
    }
    private static object Resumo(ProgramaTreinoModelo p,ProgramaConteudo c)=>new{p.Nome,p.Objetivo,p.Descricao,p.Ativo,Fases=c.Fases.Count,Semanas=c.Fases.Sum(x=>x.Semanas),Sessoes=c.Fases.Sum(x=>x.Dias.Count(d=>d.ModeloPlanoTreinoId.HasValue))};
    private void Auditar(string acao,ProgramaTreinoModelo p,object? antes,object? depois)=>db.AuditLogs.Add(new AuditLog{OrganizacaoId=currentUser.OrganizationId,UsuarioId=currentUser.UserId,Acao=acao,Entidade=nameof(ProgramaTreinoModelo),EntidadeId=p.Id.ToString(),DadosAnterioresJson=antes is null?null:JsonSerializer.Serialize(antes),DadosNovosJson=depois is null?null:JsonSerializer.Serialize(depois),IpAddress=httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()});
    private static string? Limpar(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
