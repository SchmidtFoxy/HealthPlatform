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
public sealed class ModelosPlanosTreinoController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record SalvarModeloTreinoRequest(string Nome, string? Descricao);
    public sealed record AtualizarModeloTreinoRequest(string Nome, string? Descricao, bool Ativo);
    public sealed record CriarTreinoDeModeloRequest(
        string Nome,
        string? Objetivo,
        DateOnly DataInicio,
        DateOnly? DataFim,
        string? Observacoes);

    public sealed record TemplateItemTreino(
        Guid ExercicioId,
        int Ordem,
        int Series,
        string Repeticoes,
        decimal? Carga,
        string? UnidadeCarga,
        int? DescansoSegundos,
        int? TempoSegundos,
        string? Observacoes);

    public sealed record TemplateSessaoTreino(
        string Nome,
        string? DiasSemana,
        int Ordem,
        string? Observacoes,
        IReadOnlyCollection<TemplateItemTreino> Itens);

    public sealed record TemplateTreinoConteudo(
        string? ObjetivoOriginal,
        string? ObservacoesOriginais,
        IReadOnlyCollection<TemplateSessaoTreino> Sessoes);

    [HttpGet("api/modelos-planos-treino")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        CancellationToken ct = default)
    {
        var query = db.ModelosPlanosTreino.AsNoTracking()
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

    [HttpPost("api/treinos/{planoId:guid}/salvar-como-modelo")]
    public async Task<IActionResult> SalvarComoModelo(
        Guid planoId,
        SalvarModeloTreinoRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var plano = await db.PlanosTreino.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Sessoes).ThenInclude(x => x.Itens)
            .FirstOrDefaultAsync(x =>
                x.Id == planoId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (plano is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        var conteudo = new TemplateTreinoConteudo(
            plano.Objetivo,
            plano.Observacoes,
            plano.Sessoes.OrderBy(x => x.Ordem).Select(s =>
                new TemplateSessaoTreino(
                    s.Nome,
                    s.DiasSemana,
                    s.Ordem,
                    s.Observacoes,
                    s.Itens.OrderBy(x => x.Ordem).Select(i =>
                        new TemplateItemTreino(
                            i.ExercicioId,
                            i.Ordem,
                            i.Series,
                            i.Repeticoes,
                            i.Carga,
                            i.UnidadeCarga,
                            i.DescansoSegundos,
                            i.TempoSegundos,
                            i.Observacoes)).ToList())).ToList());

        var modelo = new ModeloPlanoTreino
        {
            OrganizacaoId = currentUser.OrganizationId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            Descricao = Limpar(request.Descricao),
            ConteudoJson = JsonSerializer.Serialize(conteudo),
            Ativo = true
        };

        db.ModelosPlanosTreino.Add(modelo);
        Auditar("CREATE", modelo, null, new
        {
            modelo.Nome,
            modelo.Descricao,
            OrigemPlanoId = plano.Id,
            Sessoes = conteudo.Sessoes.Count,
            Exercicios = conteudo.Sessoes.Sum(x => x.Itens.Count)
        });

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(modelo, profissional.Nome));
    }

    [HttpPut("api/modelos-planos-treino/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        AtualizarModeloTreinoRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome do modelo e obrigatorio." });

        var modelo = await db.ModelosPlanosTreino
            .Include(x => x.Profissional)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo de treino nao encontrado." });

        var antes = new { modelo.Nome, modelo.Descricao, modelo.Ativo };
        modelo.Nome = request.Nome.Trim();
        modelo.Descricao = Limpar(request.Descricao);
        modelo.Ativo = request.Ativo;
        modelo.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("UPDATE", modelo, antes, new { modelo.Nome, modelo.Descricao, modelo.Ativo });
        await db.SaveChangesAsync(ct);

        return Ok(ToResponse(modelo));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/treinos/criar-de-modelo/{modeloId:guid}")]
    public async Task<IActionResult> CriarTreinoDeModelo(
        Guid pacienteId,
        Guid modeloId,
        CriarTreinoDeModeloRequest request,
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

        var modelo = await db.ModelosPlanosTreino.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == modeloId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (modelo is null)
            return NotFound(new { message = "Modelo de treino nao encontrado ou inativo." });

        var conteudo = JsonSerializer.Deserialize<TemplateTreinoConteudo>(modelo.ConteudoJson);
        if (conteudo is null || conteudo.Sessoes.Count == 0)
            return BadRequest(new { message = "Modelo de treino sem conteudo valido." });

        var idsExercicios = conteudo.Sessoes
            .SelectMany(x => x.Itens)
            .Select(x => x.ExercicioId)
            .Distinct()
            .ToArray();

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
                message = "O modelo possui exercicios inativos ou indisponiveis. Atualize o catalogo ou crie um novo modelo.",
                exerciciosInvalidos = invalidos
            });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

        var plano = new PlanoTreino
        {
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            Objetivo = Limpar(request.Objetivo) ?? conteudo.ObjetivoOriginal,
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Status = "Ativo",
            Observacoes = Limpar(request.Observacoes) ?? conteudo.ObservacoesOriginais,
            Versao = 1,
            AjusteCargaPercentual = 0m,
            AjusteSeries = 0,
            AjusteRepeticoes = 0,
            AjusteDescansoSegundos = 0
        };

        foreach (var s in conteudo.Sessoes.OrderBy(x => x.Ordem))
        {
            var sessao = new SessaoTreino
            {
                PlanoTreinoId = plano.Id,
                Nome = s.Nome,
                DiasSemana = s.DiasSemana,
                Ordem = s.Ordem,
                Observacoes = s.Observacoes
            };

            foreach (var i in s.Itens.OrderBy(x => x.Ordem))
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

            plano.Sessoes.Add(sessao);
        }

        db.PlanosTreino.Add(plano);
        Auditar("CREATE_FROM_TEMPLATE", modelo, null, new
        {
            ModeloId = modelo.Id,
            PacienteId = pacienteId,
            PlanoId = plano.Id,
            plano.Nome,
            Sessoes = plano.Sessoes.Count,
            Exercicios = plano.Sessoes.Sum(x => x.Itens.Count)
        });

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            plano.Id,
            plano.PacienteId,
            plano.Nome,
            plano.Objetivo,
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

    private object ToResponse(ModeloPlanoTreino x, string? profissionalNome = null)
    {
        TemplateTreinoConteudo? conteudo = null;
        try { conteudo = JsonSerializer.Deserialize<TemplateTreinoConteudo>(x.ConteudoJson); } catch { }

        return new
        {
            x.Id,
            x.Nome,
            x.Descricao,
            x.Ativo,
            x.ProfissionalId,
            profissionalNome = profissionalNome ?? x.Profissional?.Nome,
            sessoes = conteudo?.Sessoes.Count ?? 0,
            exercicios = conteudo?.Sessoes.Sum(s => s.Itens.Count) ?? 0,
            objetivo = conteudo?.ObjetivoOriginal,
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        };
    }

    private void Auditar(string acao, ModeloPlanoTreino modelo, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(ModeloPlanoTreino),
            EntidadeId = modelo.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }

    private static string? Limpar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
