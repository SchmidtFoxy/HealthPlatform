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
public sealed class ModelosPlanosAlimentaresController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record SalvarModeloRequest(string Nome, string? Descricao);
    public sealed record AtualizarModeloRequest(string Nome, string? Descricao, bool Ativo);
    public sealed record CriarPlanoDeModeloRequest(
        string Nome,
        DateOnly DataInicio,
        DateOnly? DataFim,
        string? Observacoes);

    public sealed record TemplateSubstituicao(
        Guid AlimentoId,
        decimal Quantidade,
        string Unidade,
        decimal QuantidadeGramas,
        string? Observacao);

    public sealed record TemplateItem(
        Guid AlimentoId,
        decimal Quantidade,
        string Unidade,
        decimal QuantidadeGramas,
        string? Observacao,
        IReadOnlyCollection<TemplateSubstituicao> Substituicoes);

    public sealed record TemplateRefeicao(
        string Nome,
        TimeOnly? Horario,
        int Ordem,
        string? Observacoes,
        decimal? MetaCalorias,
        decimal? MetaProteinasG,
        decimal? MetaCarboidratosG,
        decimal? MetaGordurasG,
        decimal? MetaFibrasG,
        IReadOnlyCollection<TemplateItem> Itens);

    public sealed record TemplateConteudo(
        string? ObservacoesOriginais,
        decimal? MetaCalorias,
        decimal? MetaProteinasG,
        decimal? MetaCarboidratosG,
        decimal? MetaGordurasG,
        decimal? MetaFibrasG,
        IReadOnlyCollection<TemplateRefeicao> Refeicoes);

    [HttpGet("api/modelos-planos-alimentares")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        CancellationToken ct = default)
    {
        var query = db.ModelosPlanosAlimentares.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId);

        if (!incluirInativos)
            query = query.Where(x => x.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, termo) ||
                (x.Descricao != null && EF.Functions.ILike(x.Descricao, termo)));
        }

        var modelos = await query
            .Include(x => x.Profissional)
            .OrderByDescending(x => x.Ativo)
            .ThenBy(x => x.Nome)
            .ToListAsync(ct);

        return Ok(modelos.Select(x => ToResponse(x)).ToList());
    }

    [HttpPost("api/planos-alimentares/{planoId:guid}/salvar-como-modelo")]
    public async Task<IActionResult> SalvarComoModelo(
        Guid planoId,
        SalvarModeloRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var plano = await db.PlanosAlimentares.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Refeicoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Substituicoes)
            .FirstOrDefaultAsync(x =>
                x.Id == planoId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (plano is null)
            return NotFound(new { message = "Plano alimentar nao encontrado." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        var conteudo = new TemplateConteudo(
            plano.Observacoes,
            plano.MetaCalorias,
            plano.MetaProteinasG,
            plano.MetaCarboidratosG,
            plano.MetaGordurasG,
            plano.MetaFibrasG,
            plano.Refeicoes.OrderBy(x => x.Ordem).Select(r =>
                new TemplateRefeicao(
                    r.Nome,
                    r.Horario,
                    r.Ordem,
                    r.Observacoes,
                    r.MetaCalorias,
                    r.MetaProteinasG,
                    r.MetaCarboidratosG,
                    r.MetaGordurasG,
                    r.MetaFibrasG,
                    r.Itens.Select(i =>
                        new TemplateItem(
                            i.AlimentoId,
                            i.Quantidade,
                            i.Unidade,
                            i.QuantidadeGramas,
                            i.Observacao,
                            i.Substituicoes.Select(s =>
                                new TemplateSubstituicao(
                                    s.AlimentoId,
                                    s.Quantidade,
                                    s.Unidade,
                                    s.QuantidadeGramas,
                                    s.Observacao)).ToList())).ToList())).ToList());

        var modelo = new ModeloPlanoAlimentar
        {
            OrganizacaoId = currentUser.OrganizationId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            Descricao = Limpar(request.Descricao),
            ConteudoJson = JsonSerializer.Serialize(conteudo),
            Ativo = true
        };

        db.ModelosPlanosAlimentares.Add(modelo);
        Auditar("CREATE", modelo, null, new
        {
            modelo.Nome,
            modelo.Descricao,
            OrigemPlanoId = plano.Id,
            Refeicoes = conteudo.Refeicoes.Count
        });

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(modelo, profissional.Nome));
    }

    [HttpPut("api/modelos-planos-alimentares/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        AtualizarModeloRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var modelo = await db.ModelosPlanosAlimentares
            .Include(x => x.Profissional)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo nao encontrado." });

        var antes = new { modelo.Nome, modelo.Descricao, modelo.Ativo };
        modelo.Nome = request.Nome.Trim();
        modelo.Descricao = Limpar(request.Descricao);
        modelo.Ativo = request.Ativo;
        modelo.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("UPDATE", modelo, antes, new { modelo.Nome, modelo.Descricao, modelo.Ativo });
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(modelo));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/planos-alimentares/criar-de-modelo/{modeloId:guid}")]
    public async Task<IActionResult> CriarPlanoDeModelo(
        Guid pacienteId,
        Guid modeloId,
        CriarPlanoDeModeloRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do plano e obrigatorio." });

        if (request.DataFim.HasValue && request.DataFim.Value < request.DataInicio)
            return BadRequest(new { message = "Data final nao pode ser anterior a data inicial." });

        var pacienteExiste = await db.Pacientes.AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente nao encontrado ou inativo." });

        var modelo = await db.ModelosPlanosAlimentares.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == modeloId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo alimentar nao encontrado ou inativo." });

        var conteudo = JsonSerializer.Deserialize<TemplateConteudo>(modelo.ConteudoJson);
        if (conteudo is null || conteudo.Refeicoes.Count == 0)
            return BadRequest(new { message = "Modelo alimentar sem conteudo valido." });

        var idsAlimentos = conteudo.Refeicoes
            .SelectMany(x => x.Itens)
            .Select(x => x.AlimentoId)
            .Concat(conteudo.Refeicoes.SelectMany(x => x.Itens)
                .SelectMany(x => x.Substituicoes)
                .Select(x => x.AlimentoId))
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
                message = "O modelo possui alimentos inativos ou indisponiveis. Atualize o catalogo ou crie um novo modelo.",
                alimentosInvalidos = invalidos
            });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        var plano = new PlanoAlimentar
        {
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Status = "Ativo",
            Observacoes = Limpar(request.Observacoes) ?? conteudo.ObservacoesOriginais,
            Versao = 1,
            AjustePercentual = 0m,
            MetaCalorias = conteudo.MetaCalorias,
            MetaProteinasG = conteudo.MetaProteinasG,
            MetaCarboidratosG = conteudo.MetaCarboidratosG,
            MetaGordurasG = conteudo.MetaGordurasG,
            MetaFibrasG = conteudo.MetaFibrasG
        };

        foreach (var r in conteudo.Refeicoes.OrderBy(x => x.Ordem))
        {
            var refeicao = new RefeicaoPlanoAlimentar
            {
                PlanoAlimentarId = plano.Id,
                Nome = r.Nome,
                Horario = r.Horario,
                Ordem = r.Ordem,
                Observacoes = r.Observacoes,
                MetaCalorias = r.MetaCalorias,
                MetaProteinasG = r.MetaProteinasG,
                MetaCarboidratosG = r.MetaCarboidratosG,
                MetaGordurasG = r.MetaGordurasG,
                MetaFibrasG = r.MetaFibrasG
            };

            foreach (var i in r.Itens)
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

            plano.Refeicoes.Add(refeicao);
        }

        db.PlanosAlimentares.Add(plano);
        Auditar("CREATE_FROM_TEMPLATE", modelo, null, new
        {
            ModeloId = modelo.Id,
            PacienteId = pacienteId,
            PlanoId = plano.Id,
            plano.Nome,
            Refeicoes = plano.Refeicoes.Count
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            plano.Id,
            plano.PacienteId,
            plano.Nome,
            plano.DataInicio,
            plano.DataFim,
            plano.Status,
            plano.Versao,
            modeloId = modelo.Id,
            modeloNome = modelo.Nome
        });
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private object ToResponse(ModeloPlanoAlimentar x, string? profissionalNome = null)
    {
        TemplateConteudo? conteudo = null;
        try { conteudo = JsonSerializer.Deserialize<TemplateConteudo>(x.ConteudoJson); } catch { }

        return new
        {
            x.Id,
            x.Nome,
            x.Descricao,
            x.Ativo,
            x.ProfissionalId,
            profissionalNome = profissionalNome ?? x.Profissional?.Nome,
            refeicoes = conteudo?.Refeicoes.Count ?? 0,
            itens = conteudo?.Refeicoes.Sum(r => r.Itens.Count) ?? 0,
            metaCalorias = conteudo?.MetaCalorias,
            metaProteinasG = conteudo?.MetaProteinasG,
            metaCarboidratosG = conteudo?.MetaCarboidratosG,
            metaGordurasG = conteudo?.MetaGordurasG,
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        };
    }

    private void Auditar(string acao, ModeloPlanoAlimentar modelo, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(ModeloPlanoAlimentar),
            EntidadeId = modelo.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static string? Limpar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
