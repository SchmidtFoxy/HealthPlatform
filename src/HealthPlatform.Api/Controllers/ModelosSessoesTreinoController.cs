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
public sealed class ModelosSessoesTreinoController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record SalvarModeloSessaoRequest(
        string Nome,
        string? Categoria,
        string? Descricao);

    public sealed record AtualizarModeloSessaoRequest(
        string Nome,
        string? Categoria,
        string? Descricao,
        bool Ativo);

    public sealed record InserirModeloSessaoRequest(
        string? Nome,
        string? DiasSemana,
        string? Observacoes);

    public sealed record ModeloItemSessao(
        Guid ExercicioId,
        int Ordem,
        int Series,
        string Repeticoes,
        decimal? Carga,
        string? UnidadeCarga,
        int? DescansoSegundos,
        int? TempoSegundos,
        string? Observacoes);

    public sealed record ModeloSessaoConteudo(
        string NomeOriginal,
        string? DiasSemanaOriginal,
        string? ObservacoesOriginais,
        IReadOnlyCollection<ModeloItemSessao> Itens);

    [HttpGet("api/modelos-sessoes-treino")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        [FromQuery] string? categoria = null,
        CancellationToken ct = default)
    {
        var query = db.ModelosSessoesTreino.AsNoTracking()
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

        var modelos = await query
            .Include(x => x.Profissional)
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Nome)
            .ToListAsync(ct);

        return Ok(modelos.Select(x => ToResponse(x)).ToList());
    }

    [HttpPost("api/sessoes-treino/{sessaoId:guid}/salvar-como-modelo")]
    public async Task<IActionResult> SalvarComoModelo(
        Guid sessaoId,
        SalvarModeloSessaoRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var sessao = await db.SessoesTreino.AsNoTracking()
            .Include(x => x.PlanoTreino).ThenInclude(x => x.Paciente)
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x =>
                x.Id == sessaoId &&
                x.PlanoTreino.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (sessao is null)
            return NotFound(new { message = "Sessao de treino nao encontrada." });

        if (sessao.Itens.Count == 0)
            return BadRequest(new { message = "Nao e possivel salvar uma sessao vazia como modelo." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        var conteudo = new ModeloSessaoConteudo(
            sessao.Nome,
            sessao.DiasSemana,
            sessao.Observacoes,
            sessao.Itens.OrderBy(x => x.Ordem).Select(i =>
                new ModeloItemSessao(
                    i.ExercicioId,
                    i.Ordem,
                    i.Series,
                    i.Repeticoes,
                    i.Carga,
                    i.UnidadeCarga,
                    i.DescansoSegundos,
                    i.TempoSegundos,
                    i.Observacoes)).ToList());

        var modelo = new ModeloSessaoTreino
        {
            OrganizacaoId = currentUser.OrganizationId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            Categoria = Limpar(request.Categoria),
            Descricao = Limpar(request.Descricao),
            ConteudoJson = JsonSerializer.Serialize(conteudo),
            Ativo = true
        };

        db.ModelosSessoesTreino.Add(modelo);
        Auditar("CREATE", modelo, null, new
        {
            modelo.Nome,
            modelo.Categoria,
            OrigemSessaoId = sessao.Id,
            Exercicios = conteudo.Itens.Count
        });

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(modelo, profissional.Nome));
    }

    [HttpPut("api/modelos-sessoes-treino/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        AtualizarModeloSessaoRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var modelo = await db.ModelosSessoesTreino
            .Include(x => x.Profissional)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo de sessao nao encontrado." });

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

    [HttpPost("api/treinos/{planoId:guid}/inserir-modelo-sessao/{modeloId:guid}")]
    public async Task<IActionResult> InserirNoPlano(
        Guid planoId,
        Guid modeloId,
        InserirModeloSessaoRequest request,
        CancellationToken ct = default)
    {
        var plano = await db.PlanosTreino
            .Include(x => x.Paciente)
            .Include(x => x.Sessoes)
            .FirstOrDefaultAsync(x =>
                x.Id == planoId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (plano is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        if (plano.Status == "Concluido")
            return Conflict(new { message = "Nao e possivel inserir sessoes em um plano concluido." });

        var modelo = await db.ModelosSessoesTreino.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == modeloId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo de sessao nao encontrado ou inativo." });

        var conteudo = JsonSerializer.Deserialize<ModeloSessaoConteudo>(modelo.ConteudoJson);
        if (conteudo is null || conteudo.Itens.Count == 0)
            return BadRequest(new { message = "Modelo de sessao sem conteudo valido." });

        var idsExercicios = conteudo.Itens.Select(x => x.ExercicioId).Distinct().ToArray();

        var exerciciosValidos = await db.Exercicios
            .Where(x =>
                idsExercicios.Contains(x.Id) &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var invalidos = idsExercicios.Except(exerciciosValidos).ToArray();
        if (invalidos.Length > 0)
            return Conflict(new
            {
                message = "O modelo possui exercicios inativos ou indisponiveis.",
                exerciciosInvalidos = invalidos
            });

        var ordem = plano.Sessoes.Count == 0
            ? 1
            : plano.Sessoes.Max(x => x.Ordem) + 1;

        var sessao = new SessaoTreino
        {
            PlanoTreinoId = plano.Id,
            Nome = Limpar(request.Nome) ?? conteudo.NomeOriginal,
            DiasSemana = Limpar(request.DiasSemana) ?? conteudo.DiasSemanaOriginal,
            Ordem = ordem,
            Observacoes = Limpar(request.Observacoes) ?? conteudo.ObservacoesOriginais
        };

        foreach (var i in conteudo.Itens.OrderBy(x => x.Ordem))
        {
            sessao.Itens.Add(new ItemTreino
            {
                SessaoTreinoId = sessao.Id,
                ExercicioId = i.ExercicioId,
                Ordem = i.Ordem,
                Series = i.Series,
                Repeticoes = i.Repeticoes,
                Carga = i.Carga,
                UnidadeCarga = i.UnidadeCarga,
                DescansoSegundos = i.DescansoSegundos,
                TempoSegundos = i.TempoSegundos,
                Observacoes = i.Observacoes
            });
        }

        db.SessoesTreino.Add(sessao);
        plano.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("INSERT_FROM_TEMPLATE", modelo, null, new
        {
            ModeloId = modelo.Id,
            PlanoId = plano.Id,
            SessaoId = sessao.Id,
            sessao.Nome,
            sessao.Ordem,
            Exercicios = sessao.Itens.Count
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            sessao.Id,
            sessao.PlanoTreinoId,
            sessao.Nome,
            sessao.DiasSemana,
            sessao.Ordem,
            exercicios = sessao.Itens.Count,
            modeloId = modelo.Id,
            modeloNome = modelo.Nome
        });
    }

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private object ToResponse(ModeloSessaoTreino x, string? profissionalNome = null)
    {
        ModeloSessaoConteudo? conteudo = null;
        try { conteudo = JsonSerializer.Deserialize<ModeloSessaoConteudo>(x.ConteudoJson); } catch { }

        return new
        {
            x.Id,
            x.Nome,
            x.Categoria,
            x.Descricao,
            x.Ativo,
            x.ProfissionalId,
            profissionalNome = profissionalNome ?? x.Profissional?.Nome,
            diasSemana = conteudo?.DiasSemanaOriginal,
            exercicios = conteudo?.Itens.Count ?? 0,
            comCarga = conteudo?.Itens.Count(i => i.Carga.HasValue) ?? 0,
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        };
    }

    private void Auditar(string acao, ModeloSessaoTreino modelo, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(ModeloSessaoTreino),
            EntidadeId = modelo.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static string? Limpar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
