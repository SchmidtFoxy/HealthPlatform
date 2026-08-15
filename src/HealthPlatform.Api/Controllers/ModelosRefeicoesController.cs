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
public sealed class ModelosRefeicoesController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record SalvarModeloRefeicaoRequest(
        string Nome,
        string? Categoria,
        string? Descricao);

    public sealed record AtualizarModeloRefeicaoRequest(
        string Nome,
        string? Categoria,
        string? Descricao,
        bool Ativo);

    public sealed record InserirModeloRefeicaoRequest(
        string? Nome,
        TimeOnly? Horario,
        string? Observacoes);

    public sealed record ModeloSubstituicao(
        Guid AlimentoId,
        decimal Quantidade,
        string Unidade,
        decimal QuantidadeGramas,
        string? Observacao);

    public sealed record ModeloItemRefeicao(
        Guid AlimentoId,
        decimal Quantidade,
        string Unidade,
        decimal QuantidadeGramas,
        string? Observacao,
        IReadOnlyCollection<ModeloSubstituicao> Substituicoes);

    public sealed record ModeloRefeicaoConteudo(
        string NomeOriginal,
        TimeOnly? HorarioOriginal,
        string? ObservacoesOriginais,
        decimal? MetaCalorias,
        decimal? MetaProteinasG,
        decimal? MetaCarboidratosG,
        decimal? MetaGordurasG,
        decimal? MetaFibrasG,
        IReadOnlyCollection<ModeloItemRefeicao> Itens);

    [HttpGet("api/modelos-refeicoes")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        [FromQuery] string? categoria = null,
        CancellationToken ct = default)
    {
        var query = db.ModelosRefeicoes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId);

        if (!incluirInativos)
            query = query.Where(x => x.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, termo) ||
                (x.Descricao != null && EF.Functions.ILike(x.Descricao, termo)) ||
                (x.Categoria != null && EF.Functions.ILike(x.Categoria, termo)));
        }

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var cat = categoria.Trim().ToLower();
            query = query.Where(x =>
                x.Categoria != null &&
                x.Categoria.ToLower() == cat);
        }

        var itens = await query
            .Include(x => x.Profissional)
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Nome)
            .ToListAsync(ct);

        return Ok(itens.Select(x => ToResponse(x)).ToList());
    }

    [HttpPost("api/refeicoes-plano/{refeicaoId:guid}/salvar-como-modelo")]
    public async Task<IActionResult> SalvarComoModelo(
        Guid refeicaoId,
        SalvarModeloRefeicaoRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var refeicao = await db.RefeicoesPlanoAlimentar.AsNoTracking()
            .Include(x => x.PlanoAlimentar).ThenInclude(x => x.Paciente)
            .Include(x => x.Itens).ThenInclude(x => x.Substituicoes)
            .FirstOrDefaultAsync(x =>
                x.Id == refeicaoId &&
                x.PlanoAlimentar.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (refeicao is null)
            return NotFound(new { message = "Refeicao nao encontrada." });

        if (refeicao.Itens.Count == 0)
            return BadRequest(new { message = "Nao e possivel salvar uma refeicao vazia como modelo." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        var conteudo = new ModeloRefeicaoConteudo(
            refeicao.Nome,
            refeicao.Horario,
            refeicao.Observacoes,
            refeicao.MetaCalorias,
            refeicao.MetaProteinasG,
            refeicao.MetaCarboidratosG,
            refeicao.MetaGordurasG,
            refeicao.MetaFibrasG,
            refeicao.Itens.Select(i =>
                new ModeloItemRefeicao(
                    i.AlimentoId,
                    i.Quantidade,
                    i.Unidade,
                    i.QuantidadeGramas,
                    i.Observacao,
                    i.Substituicoes.Select(s =>
                        new ModeloSubstituicao(
                            s.AlimentoId,
                            s.Quantidade,
                            s.Unidade,
                            s.QuantidadeGramas,
                            s.Observacao)).ToList())).ToList());

        var modelo = new ModeloRefeicao
        {
            OrganizacaoId = currentUser.OrganizationId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            Categoria = Limpar(request.Categoria),
            Descricao = Limpar(request.Descricao),
            ConteudoJson = JsonSerializer.Serialize(conteudo),
            Ativo = true
        };

        db.ModelosRefeicoes.Add(modelo);
        Auditar("CREATE", modelo, null, new
        {
            modelo.Nome,
            modelo.Categoria,
            OrigemRefeicaoId = refeicao.Id,
            Itens = conteudo.Itens.Count
        });

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(modelo, profissional.Nome));
    }

    [HttpPut("api/modelos-refeicoes/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        AtualizarModeloRefeicaoRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var modelo = await db.ModelosRefeicoes
            .Include(x => x.Profissional)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo de refeicao nao encontrado." });

        var antes = new
        {
            modelo.Nome,
            modelo.Categoria,
            modelo.Descricao,
            modelo.Ativo
        };

        modelo.Nome = request.Nome.Trim();
        modelo.Categoria = Limpar(request.Categoria);
        modelo.Descricao = Limpar(request.Descricao);
        modelo.Ativo = request.Ativo;
        modelo.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("UPDATE", modelo, antes, new
        {
            modelo.Nome,
            modelo.Categoria,
            modelo.Descricao,
            modelo.Ativo
        });

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(modelo));
    }

    [HttpPost("api/planos-alimentares/{planoId:guid}/inserir-modelo-refeicao/{modeloId:guid}")]
    public async Task<IActionResult> InserirNoPlano(
        Guid planoId,
        Guid modeloId,
        InserirModeloRefeicaoRequest request,
        CancellationToken ct = default)
    {
        var plano = await db.PlanosAlimentares
            .Include(x => x.Paciente)
            .Include(x => x.Refeicoes)
            .FirstOrDefaultAsync(x =>
                x.Id == planoId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (plano is null)
            return NotFound(new { message = "Plano alimentar nao encontrado." });

        if (plano.Status == "Concluido")
            return Conflict(new { message = "Nao e possivel inserir refeicoes em um plano concluido." });

        var modelo = await db.ModelosRefeicoes.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == modeloId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo de refeicao nao encontrado ou inativo." });

        var conteudo = JsonSerializer.Deserialize<ModeloRefeicaoConteudo>(modelo.ConteudoJson);
        if (conteudo is null || conteudo.Itens.Count == 0)
            return BadRequest(new { message = "Modelo de refeicao sem conteudo valido." });

        var idsAlimentos = conteudo.Itens
            .Select(x => x.AlimentoId)
            .Concat(conteudo.Itens.SelectMany(x => x.Substituicoes).Select(x => x.AlimentoId))
            .Distinct()
            .ToArray();

        var alimentosValidos = await db.Alimentos
            .Where(x =>
                idsAlimentos.Contains(x.Id) &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var invalidos = idsAlimentos.Except(alimentosValidos).ToArray();
        if (invalidos.Length > 0)
            return Conflict(new
            {
                message = "O modelo possui alimentos inativos ou indisponiveis.",
                alimentosInvalidos = invalidos
            });

        var ordem = plano.Refeicoes.Count == 0
            ? 1
            : plano.Refeicoes.Max(x => x.Ordem) + 1;

        var refeicao = new RefeicaoPlanoAlimentar
        {
            PlanoAlimentarId = plano.Id,
            Nome = Limpar(request.Nome) ?? conteudo.NomeOriginal,
            Horario = request.Horario ?? conteudo.HorarioOriginal,
            Ordem = ordem,
            Observacoes = Limpar(request.Observacoes) ?? conteudo.ObservacoesOriginais,
            MetaCalorias = conteudo.MetaCalorias,
            MetaProteinasG = conteudo.MetaProteinasG,
            MetaCarboidratosG = conteudo.MetaCarboidratosG,
            MetaGordurasG = conteudo.MetaGordurasG,
            MetaFibrasG = conteudo.MetaFibrasG
        };

        foreach (var i in conteudo.Itens)
        {
            var item = new ItemRefeicaoPlano
            {
                RefeicaoPlanoAlimentarId = refeicao.Id,
                AlimentoId = i.AlimentoId,
                Quantidade = i.Quantidade,
                Unidade = i.Unidade,
                QuantidadeGramas = i.QuantidadeGramas,
                Observacao = i.Observacao
            };

            foreach (var s in i.Substituicoes)
            {
                item.Substituicoes.Add(new SubstituicaoItemRefeicao
                {
                    ItemRefeicaoPlanoId = item.Id,
                    AlimentoId = s.AlimentoId,
                    Quantidade = s.Quantidade,
                    Unidade = s.Unidade,
                    QuantidadeGramas = s.QuantidadeGramas,
                    Observacao = s.Observacao
                });
            }

            refeicao.Itens.Add(item);
        }

        db.RefeicoesPlanoAlimentar.Add(refeicao);
        plano.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("INSERT_FROM_TEMPLATE", modelo, null, new
        {
            ModeloId = modelo.Id,
            PlanoId = plano.Id,
            RefeicaoId = refeicao.Id,
            refeicao.Nome,
            refeicao.Ordem,
            Itens = refeicao.Itens.Count
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            refeicao.Id,
            refeicao.PlanoAlimentarId,
            refeicao.Nome,
            refeicao.Horario,
            refeicao.Ordem,
            itens = refeicao.Itens.Count,
            modeloId = modelo.Id,
            modeloNome = modelo.Nome
        });
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private object ToResponse(ModeloRefeicao x, string? profissionalNome = null)
    {
        ModeloRefeicaoConteudo? conteudo = null;
        try { conteudo = JsonSerializer.Deserialize<ModeloRefeicaoConteudo>(x.ConteudoJson); } catch { }

        return new
        {
            x.Id,
            x.Nome,
            x.Categoria,
            x.Descricao,
            x.Ativo,
            x.ProfissionalId,
            profissionalNome = profissionalNome ?? x.Profissional?.Nome,
            horario = conteudo?.HorarioOriginal,
            itens = conteudo?.Itens.Count ?? 0,
            substituicoes = conteudo?.Itens.Sum(i => i.Substituicoes.Count) ?? 0,
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        };
    }

    private void Auditar(string acao, ModeloRefeicao modelo, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(ModeloRefeicao),
            EntidadeId = modelo.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static string? Limpar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
