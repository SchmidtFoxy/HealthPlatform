using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TreinosController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor,
    IPushNotificationService push) : ControllerBase
{
    public sealed record UpsertExercicioRequest(
        string Nome,
        string? GrupoMuscular,
        string? Equipamento,
        string? Descricao,
        string? VideoUrl);

    public sealed record ItemTreinoRequest(
        Guid ExercicioId,
        int Ordem,
        int Series,
        string Repeticoes,
        decimal? Carga,
        string? UnidadeCarga,
        int? DescansoSegundos,
        int? TempoSegundos,
        string? Observacoes);

    public sealed record SessaoTreinoRequest(
        string Nome,
        string? DiasSemana,
        int Ordem,
        string? Observacoes,
        IReadOnlyCollection<ItemTreinoRequest> Itens);

    public sealed record UpsertPlanoTreinoRequest(
        string Nome,
        string? Objetivo,
        DateOnly DataInicio,
        DateOnly? DataFim,
        string? Status,
        string? Observacoes,
        IReadOnlyCollection<SessaoTreinoRequest> Sessoes);

    public sealed record DuplicarPlanoTreinoRequest(
        string Nome,
        DateOnly DataInicio,
        DateOnly? DataFim,
        decimal AjusteCargaPercentual,
        int AjusteSeries,
        int AjusteRepeticoes,
        int AjusteDescansoSegundos,
        bool ConcluirPlanoAnterior);

    public sealed record SimulacaoProgressaoTreinoResponse(
        Guid PlanoId,
        decimal AjusteCargaPercentual,
        int AjusteSeries,
        int AjusteRepeticoes,
        int AjusteDescansoSegundos,
        int Exercicios,
        int ExerciciosComCarga,
        int PrescricoesRepeticoesAjustadas,
        int PrescricoesRepeticoesPreservadas,
        decimal SomaCargasAtual,
        decimal SomaCargasProjetada);

    [HttpGet("api/exercicios")]
    public async Task<IActionResult> ListarExercicios(
        [FromQuery] bool incluirInativos = false,
        [FromQuery] string? busca = null,
        [FromQuery] string? grupoMuscular = null,
        [FromQuery] string? equipamento = null,
        CancellationToken ct = default)
    {
        var query = db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId);

        if (!incluirInativos)
            query = query.Where(x => x.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim().ToLower();
            query = query.Where(x =>
                x.Nome.ToLower().Contains(termo) ||
                (x.GrupoMuscular != null && x.GrupoMuscular.ToLower().Contains(termo)) ||
                (x.Equipamento != null && x.Equipamento.ToLower().Contains(termo)) ||
                (x.Descricao != null && x.Descricao.ToLower().Contains(termo)));
        }

        if (!string.IsNullOrWhiteSpace(grupoMuscular))
            query = query.Where(x => x.GrupoMuscular == grupoMuscular.Trim());

        if (!string.IsNullOrWhiteSpace(equipamento))
            query = query.Where(x => x.Equipamento == equipamento.Trim());

        var itens = await query.OrderBy(x => x.GrupoMuscular).ThenBy(x => x.Nome)
            .Select(x => new
            {
                x.Id, x.Nome, x.GrupoMuscular, x.Equipamento,
                x.Descricao, x.VideoUrl, x.Ativo
            })
            .ToListAsync(ct);

        return Ok(itens);
    }

    [HttpGet("api/exercicios/filtros")]
    public async Task<IActionResult> FiltrosExercicios(CancellationToken ct)
    {
        var query = db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo);

        var gruposMusculares = await query
            .Where(x => x.GrupoMuscular != null && x.GrupoMuscular != "")
            .Select(x => x.GrupoMuscular!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        var equipamentos = await query
            .Where(x => x.Equipamento != null && x.Equipamento != "")
            .Select(x => x.Equipamento!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        return Ok(new { gruposMusculares, equipamentos });
    }

    [HttpPost("api/exercicios/catalogo-base")]
    public async Task<IActionResult> PopularCatalogoBase(CancellationToken ct)
    {
        var existentes = await db.Exercicios
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => x.Nome)
            .ToListAsync(ct);
        var nomes = existentes.Select(NormalizarNomeCatalogo).ToHashSet();
        var criados = 0;
        var ignorados = 0;

        foreach (var baseItem in CatalogoBaseExercicios())
        {
            if (!nomes.Add(NormalizarNomeCatalogo(baseItem.Nome)))
            {
                ignorados++;
                continue;
            }

            var item = new Exercicio
            {
                OrganizacaoId = currentUser.OrganizationId,
                Nome = baseItem.Nome,
                GrupoMuscular = baseItem.GrupoMuscular,
                Equipamento = baseItem.Equipamento,
                Descricao = baseItem.Descricao,
                Ativo = true
            };
            db.Exercicios.Add(item);
            criados++;
        }

        Auditar("SEED_BASE_CATALOG", nameof(Exercicio), Guid.Empty, null, new { criados, ignorados });
        await db.SaveChangesAsync(ct);
        return Ok(new { criados, ignorados, totalCatalogoBase = criados + ignorados });
    }

    [HttpPost("api/exercicios")]
    public async Task<IActionResult> CriarExercicio(UpsertExercicioRequest request, CancellationToken ct)
    {
        var erro = ValidarExercicio(request);
        if (erro is not null) return BadRequest(new { message = erro });

        var nomeNormalizado = request.Nome.Trim().ToLower();
        if (await db.Exercicios.AnyAsync(x => x.OrganizacaoId == currentUser.OrganizationId && x.Nome.ToLower() == nomeNormalizado, ct))
            return Conflict(new { message = "Ja existe um exercicio com este nome no catalogo." });

        var item = new Exercicio
        {
            OrganizacaoId = currentUser.OrganizationId,
            Nome = request.Nome.Trim(),
            GrupoMuscular = Limpar(request.GrupoMuscular),
            Equipamento = Limpar(request.Equipamento),
            Descricao = Limpar(request.Descricao),
            VideoUrl = Limpar(request.VideoUrl),
            Ativo = true
        };

        db.Exercicios.Add(item);
        Auditar("CREATE", nameof(Exercicio), item.Id, null, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(Snapshot(item));
    }

    [HttpPut("api/exercicios/{id:guid}")]
    public async Task<IActionResult> AtualizarExercicio(Guid id, UpsertExercicioRequest request, CancellationToken ct)
    {
        var item = await db.Exercicios.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Exercicio nao encontrado." });

        var erro = ValidarExercicio(request);
        if (erro is not null) return BadRequest(new { message = erro });

        var antes = Snapshot(item);
        item.Nome = request.Nome.Trim();
        item.GrupoMuscular = Limpar(request.GrupoMuscular);
        item.Equipamento = Limpar(request.Equipamento);
        item.Descricao = Limpar(request.Descricao);
        item.VideoUrl = Limpar(request.VideoUrl);

        Auditar("UPDATE", nameof(Exercicio), item.Id, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        return Ok(Snapshot(item));
    }

    [HttpDelete("api/exercicios/{id:guid}")]
    public async Task<IActionResult> InativarExercicio(Guid id, CancellationToken ct)
    {
        var item = await db.Exercicios.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound();

        var antes = new { item.Ativo };
        item.Ativo = false;
        Auditar("DEACTIVATE", nameof(Exercicio), item.Id, antes, new { item.Ativo });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("api/exercicios/{id:guid}/reativar")]
    public async Task<IActionResult> ReativarExercicio(Guid id, CancellationToken ct)
    {
        var item = await db.Exercicios.FirstOrDefaultAsync(x =>
            x.Id == id && x.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound();

        var antes = new { item.Ativo };
        item.Ativo = true;
        Auditar("ACTIVATE", nameof(Exercicio), item.Id, antes, new { item.Ativo });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/treinos")]
    public async Task<IActionResult> ListarTreinos(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await QueryCompleta()
            .Where(x => x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataInicio)
            .ToListAsync(ct);

        return Ok(itens.Select(ToResponse));
    }

    [HttpGet("api/treinos/{id:guid}")]
    public async Task<IActionResult> ObterTreino(Guid id, CancellationToken ct)
    {
        var item = await QueryCompleta().FirstOrDefaultAsync(x =>
            x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null
            ? NotFound(new { message = "Plano de treino nao encontrado." })
            : Ok(ToResponse(item));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/treinos")]
    public async Task<IActionResult> CriarTreino(
        Guid pacienteId,
        UpsertPlanoTreinoRequest request,
        CancellationToken ct)
    {
        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        var erro = await ValidarTreino(pacienteId, request, ct);
        if (erro is not null) return BadRequest(new { message = erro });

        var item = new PlanoTreino
        {
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            Objetivo = Limpar(request.Objetivo),
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Status = NormalizarStatus(request.Status),
            Observacoes = Limpar(request.Observacoes)
        };

        MontarSessoes(item, request.Sessoes);
        db.PlanosTreino.Add(item);
        Auditar("CREATE", nameof(PlanoTreino), item.Id, null, SnapshotPlano(item));
        await db.SaveChangesAsync(ct);

        var criado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return Ok(ToResponse(criado));
    }

    [HttpPut("api/treinos/{id:guid}")]
    public async Task<IActionResult> AtualizarTreino(
        Guid id,
        UpsertPlanoTreinoRequest request,
        CancellationToken ct)
    {
        var item = await db.PlanosTreino
            .Include(x => x.Paciente)
            .Include(x => x.Sessoes).ThenInclude(x => x.Itens)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        var erro = await ValidarTreino(item.PacienteId, request, ct);
        if (erro is not null) return BadRequest(new { message = erro });

        var antes = SnapshotPlano(item);
        item.Nome = request.Nome.Trim();
        item.Objetivo = Limpar(request.Objetivo);
        item.DataInicio = request.DataInicio;
        item.DataFim = request.DataFim;
        item.Status = NormalizarStatus(request.Status);
        item.Observacoes = Limpar(request.Observacoes);

        db.SessoesTreino.RemoveRange(item.Sessoes);
        item.Sessoes.Clear();
        MontarSessoes(item, request.Sessoes);

        Auditar("UPDATE", nameof(PlanoTreino), item.Id, antes, SnapshotPlano(item));
        await db.SaveChangesAsync(ct);

        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return Ok(ToResponse(atualizado));
    }

    [HttpGet("api/treinos/{id:guid}/simular-progressao")]
    public async Task<IActionResult> SimularProgressao(
        Guid id,
        [FromQuery] decimal cargaPercentual = 0,
        [FromQuery] int seriesDelta = 0,
        [FromQuery] int repeticoesDelta = 0,
        [FromQuery] int descansoDeltaSegundos = 0,
        CancellationToken ct = default)
    {
        var plano = await QueryCompleta().FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (plano is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        var erro = ValidarProgressao(cargaPercentual, seriesDelta, repeticoesDelta, descansoDeltaSegundos);
        if (erro is not null)
            return BadRequest(new { message = erro });

        var itens = plano.Sessoes.SelectMany(x => x.Itens).ToList();
        var ajustadas = 0;
        var preservadas = 0;

        foreach (var item in itens)
        {
            if (repeticoesDelta == 0) continue;
            if (TentarAjustarRepeticoes(item.Repeticoes, repeticoesDelta, out _))
                ajustadas++;
            else
                preservadas++;
        }

        var somaAtual = itens.Where(x => x.Carga.HasValue).Sum(x => x.Carga!.Value);
        var fatorCarga = 1m + cargaPercentual / 100m;
        var somaProjetada = Math.Round(somaAtual * fatorCarga, 2, MidpointRounding.AwayFromZero);

        return Ok(new SimulacaoProgressaoTreinoResponse(
            plano.Id,
            cargaPercentual,
            seriesDelta,
            repeticoesDelta,
            descansoDeltaSegundos,
            itens.Count,
            itens.Count(x => x.Carga.HasValue),
            ajustadas,
            preservadas,
            Math.Round(somaAtual, 2),
            somaProjetada));
    }

    [HttpPost("api/treinos/{id:guid}/duplicar")]
    public async Task<IActionResult> DuplicarTreino(
        Guid id,
        DuplicarPlanoTreinoRequest request,
        CancellationToken ct = default)
    {
        var origem = await db.PlanosTreino
            .Include(x => x.Paciente)
            .Include(x => x.Profissional)
            .Include(x => x.Sessoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Exercicio)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (origem is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome da nova versao e obrigatorio." });

        if (request.DataFim.HasValue && request.DataFim.Value < request.DataInicio)
            return BadRequest(new { message = "Data final nao pode ser anterior a data inicial." });

        var erro = ValidarProgressao(
            request.AjusteCargaPercentual,
            request.AjusteSeries,
            request.AjusteRepeticoes,
            request.AjusteDescansoSegundos);

        if (erro is not null)
            return BadRequest(new { message = erro });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null) return Forbid();

        var raizId = origem.PlanoOrigemId ?? origem.Id;
        var maiorVersao = await db.PlanosTreino
            .Where(x =>
                x.PacienteId == origem.PacienteId &&
                (x.Id == raizId || x.PlanoOrigemId == raizId))
            .MaxAsync(x => (int?)x.Versao, ct) ?? origem.Versao;

        var novo = new PlanoTreino
        {
            PacienteId = origem.PacienteId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            Objetivo = origem.Objetivo,
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Status = "Ativo",
            Observacoes = origem.Observacoes,
            PlanoOrigemId = raizId,
            Versao = maiorVersao + 1,
            AjusteCargaPercentual = request.AjusteCargaPercentual,
            AjusteSeries = request.AjusteSeries,
            AjusteRepeticoes = request.AjusteRepeticoes,
            AjusteDescansoSegundos = request.AjusteDescansoSegundos
        };

        var fatorCarga = 1m + request.AjusteCargaPercentual / 100m;

        foreach (var sessaoOrigem in origem.Sessoes.OrderBy(x => x.Ordem))
        {
            var sessao = new SessaoTreino
            {
                PlanoTreinoId = novo.Id,
                Nome = sessaoOrigem.Nome,
                DiasSemana = sessaoOrigem.DiasSemana,
                Ordem = sessaoOrigem.Ordem,
                Observacoes = sessaoOrigem.Observacoes
            };

            foreach (var itemOrigem in sessaoOrigem.Itens.OrderBy(x => x.Ordem))
            {
                var repeticoes = itemOrigem.Repeticoes;
                if (request.AjusteRepeticoes != 0 &&
                    TentarAjustarRepeticoes(itemOrigem.Repeticoes, request.AjusteRepeticoes, out var ajustada))
                    repeticoes = ajustada;

                sessao.Itens.Add(new ItemTreino
                {
                    SessaoTreinoId = sessao.Id,
                    ExercicioId = itemOrigem.ExercicioId,
                    Ordem = itemOrigem.Ordem,
                    Series = Math.Max(1, itemOrigem.Series + request.AjusteSeries),
                    Repeticoes = repeticoes,
                    Carga = itemOrigem.Carga.HasValue
                        ? Math.Max(0m, Math.Round(itemOrigem.Carga.Value * fatorCarga, 2, MidpointRounding.AwayFromZero))
                        : null,
                    UnidadeCarga = itemOrigem.UnidadeCarga,
                    DescansoSegundos = itemOrigem.DescansoSegundos.HasValue
                        ? Math.Max(0, itemOrigem.DescansoSegundos.Value + request.AjusteDescansoSegundos)
                        : null,
                    TempoSegundos = itemOrigem.TempoSegundos,
                    Observacoes = itemOrigem.Observacoes
                });
            }

            novo.Sessoes.Add(sessao);
        }

        if (request.ConcluirPlanoAnterior)
        {
            origem.Status = "Concluido";
            origem.DataFim ??= request.DataInicio.AddDays(-1);
            origem.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.PlanosTreino.Add(novo);
        Auditar("DUPLICATE_PROGRESS", nameof(PlanoTreino), novo.Id, null, new
        {
            OrigemId = origem.Id,
            RaizId = raizId,
            novo.Versao,
            novo.AjusteCargaPercentual,
            novo.AjusteSeries,
            novo.AjusteRepeticoes,
            novo.AjusteDescansoSegundos,
            request.ConcluirPlanoAnterior
        });

        await db.SaveChangesAsync(ct);

        var criado = await QueryCompleta().FirstAsync(x => x.Id == novo.Id, ct);
        return Ok(ToResponse(criado));
    }

    [HttpPost("api/treinos/{id:guid}/status/{status}")]
    public async Task<IActionResult> StatusTreino(Guid id, string status, CancellationToken ct)
    {
        var item = await db.PlanosTreino
            .Include(x => x.Paciente)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null) return NotFound();

        var novo = NormalizarStatus(status);
        if (novo is not ("Ativo" or "Inativo" or "Concluido"))
            return BadRequest(new { message = "Status permitido: Ativo, Inativo ou Concluido." });

        var antes = new { item.Status };
        item.Status = novo;
        Auditar("STATUS", nameof(PlanoTreino), item.Id, antes, new { item.Status });
        await db.SaveChangesAsync(ct);
        if (novo == "Ativo" && item.Paciente.UsuarioId.HasValue)
            await push.EnviarAsync(item.Paciente.UsuarioId.Value, "plano", "Seu treino foi atualizado", $"{item.Nome} está disponível no AESYN.", "treinos", ct);
        return NoContent();
    }

    [HttpDelete("api/treinos/{id:guid}")]
    public async Task<IActionResult> ArquivarTreino(Guid id, CancellationToken ct)
    {
        var item = await db.PlanosTreino
            .Include(x => x.Paciente)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        if (item.Status == "Inativo")
            return NoContent();

        var antes = new { item.Status, item.DataFim };
        item.Status = "Inativo";
        item.DataFim ??= DateOnly.FromDateTime(DateTime.UtcNow);
        Auditar("ARCHIVE", nameof(PlanoTreino), item.Id, antes, new { item.Status, item.DataFim });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("api/treinos/{id:guid}/reativar")]
    public async Task<IActionResult> ReativarTreino(Guid id, CancellationToken ct)
    {
        var item = await db.PlanosTreino
            .Include(x => x.Paciente)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        var antes = new { item.Status, item.DataFim };
        item.Status = "Ativo";
        if (item.DataFim.HasValue && item.DataFim.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            item.DataFim = null;
        Auditar("REACTIVATE", nameof(PlanoTreino), item.Id, antes, new { item.Status, item.DataFim });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("api/treinos/{id:guid}/desvincular")]
    public async Task<IActionResult> DesvincularTreino(Guid id, CancellationToken ct)
    {
        var item = await db.PlanosTreino
            .Include(x => x.Paciente)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Plano de treino nao encontrado." });

        var antes = new { item.Status, item.DataFim };
        item.Status = "Inativo";
        item.DataFim ??= DateOnly.FromDateTime(DateTime.UtcNow);
        Auditar("UNLINK_ARCHIVE", nameof(PlanoTreino), item.Id, antes, new
        {
            item.Status,
            item.DataFim,
            HistoricoPreservado = true
        });
        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            message = "Plano desvinculado da rotina ativa sem apagar o historico.",
            item.Id,
            item.Status
        });
    }

    private IQueryable<PlanoTreino> QueryCompleta() => db.PlanosTreino.AsNoTracking()
        .Include(x => x.Paciente)
        .Include(x => x.Profissional)
        .Include(x => x.Sessoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Exercicio);

    private async Task<string?> ValidarTreino(
        Guid pacienteId,
        UpsertPlanoTreinoRequest request,
        CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return "Paciente nao encontrado ou inativo.";
        if (string.IsNullOrWhiteSpace(request.Nome))
            return "Nome do plano e obrigatorio.";
        if (request.DataFim.HasValue && request.DataFim.Value < request.DataInicio)
            return "Data final nao pode ser anterior a data inicial.";

        var status = NormalizarStatus(request.Status);
        if (status is not ("Ativo" or "Inativo" or "Concluido"))
            return "Status permitido: Ativo, Inativo ou Concluido.";

        if (request.Sessoes is null || request.Sessoes.Count == 0)
            return "Informe ao menos um treino/dia.";
        if (request.Sessoes.Any(x => string.IsNullOrWhiteSpace(x.Nome)))
            return "Todos os treinos devem possuir nome.";

        var itens = request.Sessoes.SelectMany(x => x.Itens ?? Array.Empty<ItemTreinoRequest>()).ToList();
        if (itens.Count == 0)
            return "Informe ao menos um exercicio.";
        if (itens.Any(x =>
            x.Series <= 0 ||
            string.IsNullOrWhiteSpace(x.Repeticoes) ||
            (x.DescansoSegundos.HasValue && x.DescansoSegundos < 0) ||
            (x.TempoSegundos.HasValue && x.TempoSegundos < 0)))
            return "Series, repeticoes, descanso e tempo possuem valores invalidos.";

        var ids = itens.Select(x => x.ExercicioId).Distinct().ToArray();
        var validos = await db.Exercicios.CountAsync(x =>
            ids.Contains(x.Id) &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (validos != ids.Length)
            return "Um ou mais exercicios nao existem ou estao inativos.";

        return null;
    }

    private void MontarSessoes(
        PlanoTreino plano,
        IReadOnlyCollection<SessaoTreinoRequest> sessoes)
    {
        foreach (var s in sessoes.OrderBy(x => x.Ordem))
        {
            var sessao = new SessaoTreino
            {
                PlanoTreinoId = plano.Id,
                Nome = s.Nome.Trim(),
                DiasSemana = Limpar(s.DiasSemana),
                Ordem = s.Ordem,
                Observacoes = Limpar(s.Observacoes)
            };

            foreach (var i in (s.Itens ?? Array.Empty<ItemTreinoRequest>()).OrderBy(x => x.Ordem))
            {
                sessao.Itens.Add(new ItemTreino
                {
                    SessaoTreinoId = sessao.Id,
                    ExercicioId = i.ExercicioId,
                    Ordem = i.Ordem,
                    Series = i.Series,
                    Repeticoes = i.Repeticoes.Trim(),
                    Carga = i.Carga,
                    UnidadeCarga = Limpar(i.UnidadeCarga),
                    DescansoSegundos = i.DescansoSegundos,
                    TempoSegundos = i.TempoSegundos,
                    Observacoes = Limpar(i.Observacoes)
                });
            }

            plano.Sessoes.Add(sessao);
        }
    }

    private object ToResponse(PlanoTreino x) => new
    {
        x.Id,
        x.PacienteId,
        x.ProfissionalId,
        profissionalNome = x.Profissional.Nome,
        x.Nome,
        x.Objetivo,
        x.DataInicio,
        x.DataFim,
        x.Status,
        x.Observacoes,
        x.PlanoOrigemId,
        x.Versao,
        x.AjusteCargaPercentual,
        x.AjusteSeries,
        x.AjusteRepeticoes,
        x.AjusteDescansoSegundos,
        sessoes = x.Sessoes.OrderBy(s => s.Ordem).Select(s => new
        {
            s.Id,
            s.Nome,
            s.DiasSemana,
            s.Ordem,
            s.Observacoes,
            itens = s.Itens.OrderBy(i => i.Ordem).Select(i => new
            {
                i.Id,
                i.ExercicioId,
                exercicioNome = i.Exercicio.Nome,
                i.Exercicio.GrupoMuscular,
                i.Exercicio.Equipamento,
                i.Exercicio.Descricao,
                i.Exercicio.VideoUrl,
                i.Ordem,
                i.Series,
                i.Repeticoes,
                i.Carga,
                i.UnidadeCarga,
                i.DescansoSegundos,
                i.TempoSegundos,
                i.Observacoes
            })
        }),
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    };

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct)
        => await db.Profissionais.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct)
        => await db.Pacientes.AnyAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private void Auditar(string acao, string entidade, Guid id, object? antes, object? depois)
        => db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

    private static string? ValidarExercicio(UpsertExercicioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return "Nome do exercicio e obrigatorio.";

        if (!string.IsNullOrWhiteSpace(request.VideoUrl) &&
            !Uri.TryCreate(request.VideoUrl, UriKind.Absolute, out _))
            return "VideoUrl deve ser uma URL absoluta.";

        return null;
    }

    private static object Snapshot(Exercicio x) => new
    {
        x.Id, x.OrganizacaoId, x.Nome, x.GrupoMuscular,
        x.Equipamento, x.Descricao, x.VideoUrl, x.Ativo
    };

    private static object SnapshotPlano(PlanoTreino x) => new
    {
        x.Id, x.PacienteId, x.ProfissionalId, x.Nome, x.Objetivo,
        x.DataInicio, x.DataFim, x.Status, x.Observacoes,
        x.PlanoOrigemId, x.Versao, x.AjusteCargaPercentual,
        x.AjusteSeries, x.AjusteRepeticoes, x.AjusteDescansoSegundos,
        sessoes = x.Sessoes.Select(s => new
        {
            s.Nome, s.DiasSemana, s.Ordem,
            itens = s.Itens.Select(i => new
            {
                i.ExercicioId, i.Ordem, i.Series, i.Repeticoes,
                i.Carga, i.UnidadeCarga, i.DescansoSegundos,
                i.TempoSegundos, i.Observacoes
            })
        })
    };

    private static string? ValidarProgressao(
        decimal cargaPercentual,
        int seriesDelta,
        int repeticoesDelta,
        int descansoDeltaSegundos)
    {
        if (cargaPercentual < -50m || cargaPercentual > 100m)
            return "O ajuste de carga deve ficar entre -50% e +100%.";
        if (seriesDelta < -5 || seriesDelta > 10)
            return "O ajuste de series deve ficar entre -5 e +10.";
        if (repeticoesDelta < -20 || repeticoesDelta > 30)
            return "O ajuste de repeticoes deve ficar entre -20 e +30.";
        if (descansoDeltaSegundos < -300 || descansoDeltaSegundos > 600)
            return "O ajuste de descanso deve ficar entre -300 e +600 segundos.";
        return null;
    }

    private static bool TentarAjustarRepeticoes(
        string original,
        int delta,
        out string ajustada)
    {
        ajustada = original;
        if (string.IsNullOrWhiteSpace(original)) return false;

        var texto = original.Trim();

        if (int.TryParse(texto, out var unico))
        {
            ajustada = Math.Max(1, unico + delta).ToString();
            return true;
        }

        var separadores = new[] { "-", "–", "—", " a " };
        foreach (var separador in separadores)
        {
            var partes = texto.Split(separador, StringSplitOptions.TrimEntries);
            if (partes.Length != 2) continue;
            if (!int.TryParse(partes[0], out var min) ||
                !int.TryParse(partes[1], out var max)) continue;

            min = Math.Max(1, min + delta);
            max = Math.Max(min, max + delta);
            ajustada = $"{min}-{max}";
            return true;
        }

        return false;
    }

    private static string NormalizarStatus(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "Ativo";
        var x = valor.Trim().ToLowerInvariant();
        return x switch
        {
            "ativo" => "Ativo",
            "inativo" => "Inativo",
            "concluido" or "concluído" => "Concluido",
            _ => valor.Trim()
        };
    }

    private static string? Limpar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private sealed record ExercicioBase(string Nome, string GrupoMuscular, string Equipamento, string Descricao);

    private static string NormalizarNomeCatalogo(string nome) => nome.Trim().ToLowerInvariant();

    private static IReadOnlyCollection<ExercicioBase> CatalogoBaseExercicios() => new[]
    {
        new ExercicioBase("Supino reto com barra", "Peitoral", "Barra", "Pressão horizontal com foco em peitoral, tríceps e estabilização escapular."),
        new ExercicioBase("Supino inclinado com halteres", "Peitoral", "Halteres", "Pressão inclinada com amplitude controlada e foco na porção clavicular do peitoral."),
        new ExercicioBase("Supino reto com halteres", "Peitoral", "Halteres", "Pressão horizontal unilateral independente, favorecendo amplitude e controle."),
        new ExercicioBase("Crucifixo na máquina", "Peitoral", "Máquina", "Adução horizontal do ombro com trajetória guiada e tensão contínua."),
        new ExercicioBase("Crossover na polia", "Peitoral", "Polia", "Adução horizontal em cabo com ajuste de altura e foco em controle."),
        new ExercicioBase("Flexão de braços", "Peitoral", "Peso corporal", "Pressão horizontal em cadeia fechada com tronco estável."),
        new ExercicioBase("Remada curvada com barra", "Costas", "Barra", "Puxada horizontal com quadril estabilizado e foco em dorsais e romboides."),
        new ExercicioBase("Remada baixa na polia", "Costas", "Polia", "Puxada horizontal guiada com controle escapular."),
        new ExercicioBase("Remada unilateral com halter", "Costas", "Halteres", "Puxada unilateral com apoio e foco em amplitude da dorsal."),
        new ExercicioBase("Puxada alta pronada", "Costas", "Polia", "Puxada vertical com pegada pronada, priorizando grande dorsal."),
        new ExercicioBase("Puxada alta neutra", "Costas", "Polia", "Puxada vertical com pegada neutra e trajetória confortável para ombros."),
        new ExercicioBase("Barra fixa pronada", "Costas", "Peso corporal", "Puxada vertical em cadeia fechada com controle corporal."),
        new ExercicioBase("Pulldown com braços estendidos", "Costas", "Polia", "Extensão de ombro com cotovelos quase fixos e foco em dorsais."),
        new ExercicioBase("Desenvolvimento militar com barra", "Ombros", "Barra", "Pressão vertical com controle de tronco e trajetória sobre a cabeça."),
        new ExercicioBase("Desenvolvimento com halteres", "Ombros", "Halteres", "Pressão vertical bilateral independente para deltoides e tríceps."),
        new ExercicioBase("Elevação lateral com halteres", "Ombros", "Halteres", "Abdução de ombro com foco em deltoide lateral e controle de amplitude."),
        new ExercicioBase("Elevação lateral na polia", "Ombros", "Polia", "Abdução com tensão contínua e ajuste fino da trajetória."),
        new ExercicioBase("Crucifixo inverso na máquina", "Ombros", "Máquina", "Abdução horizontal com foco em deltoide posterior e musculatura escapular."),
        new ExercicioBase("Face pull", "Ombros", "Polia", "Puxada alta com rotação externa e foco em deltoide posterior/escápulas."),
        new ExercicioBase("Rosca direta com barra", "Bíceps", "Barra", "Flexão de cotovelo bilateral com tronco estável."),
        new ExercicioBase("Rosca alternada com halteres", "Bíceps", "Halteres", "Flexão alternada de cotovelo com supinação controlada."),
        new ExercicioBase("Rosca martelo", "Bíceps", "Halteres", "Flexão de cotovelo em pegada neutra com ênfase braquial/braquiorradial."),
        new ExercicioBase("Rosca Scott", "Bíceps", "Máquina", "Flexão de cotovelo com braço apoiado e menor compensação de ombro."),
        new ExercicioBase("Tríceps na polia com barra", "Tríceps", "Polia", "Extensão de cotovelo com ombros estabilizados."),
        new ExercicioBase("Tríceps corda", "Tríceps", "Polia", "Extensão de cotovelo com pegada neutra e separação ao final."),
        new ExercicioBase("Tríceps francês unilateral", "Tríceps", "Halteres", "Extensão de cotovelo acima da cabeça com foco na cabeça longa."),
        new ExercicioBase("Paralelas", "Tríceps", "Peso corporal", "Pressão em cadeia fechada envolvendo tríceps, peitoral e cintura escapular."),
        new ExercicioBase("Agachamento livre", "Quadríceps", "Barra", "Agachamento multiarticular com controle de joelho, quadril e tronco."),
        new ExercicioBase("Agachamento frontal", "Quadríceps", "Barra", "Agachamento com carga anterior e maior demanda de tronco/quadríceps."),
        new ExercicioBase("Leg press 45°", "Quadríceps", "Máquina", "Extensão combinada de joelho e quadril em trajetória guiada."),
        new ExercicioBase("Cadeira extensora", "Quadríceps", "Máquina", "Extensão de joelho em cadeia aberta com amplitude ajustável."),
        new ExercicioBase("Afundo com halteres", "Quadríceps", "Halteres", "Padrão unilateral com flexão de joelho/quadril e controle do equilíbrio."),
        new ExercicioBase("Passada caminhando", "Quadríceps", "Halteres", "Avanço alternado dinâmico com demanda de estabilidade unilateral."),
        new ExercicioBase("Agachamento búlgaro", "Quadríceps", "Halteres", "Agachamento unilateral com pé posterior elevado."),
        new ExercicioBase("Levantamento terra romeno", "Posterior de coxa", "Barra", "Dobradiça de quadril com joelhos semiflexionados e foco em posteriores/glúteos."),
        new ExercicioBase("Stiff com halteres", "Posterior de coxa", "Halteres", "Dobradiça de quadril com carga bilateral independente."),
        new ExercicioBase("Mesa flexora", "Posterior de coxa", "Máquina", "Flexão de joelho em decúbito com resistência guiada."),
        new ExercicioBase("Cadeira flexora", "Posterior de coxa", "Máquina", "Flexão de joelho sentado com estabilização de quadril."),
        new ExercicioBase("Nordic curl", "Posterior de coxa", "Peso corporal", "Flexão de joelho excêntrica de alta demanda para isquiotibiais."),
        new ExercicioBase("Hip thrust com barra", "Glúteos", "Barra", "Extensão de quadril com apoio torácico e pico de contração em glúteos."),
        new ExercicioBase("Elevação pélvica", "Glúteos", "Peso corporal", "Extensão de quadril em solo para glúteos e estabilizadores."),
        new ExercicioBase("Abdução de quadril na máquina", "Glúteos", "Máquina", "Abdução de quadril guiada com foco em glúteo médio."),
        new ExercicioBase("Coice na polia", "Glúteos", "Polia", "Extensão de quadril unilateral em cabo."),
        new ExercicioBase("Step-up", "Glúteos", "Caixa", "Subida unilateral em caixa com extensão de joelho e quadril."),
        new ExercicioBase("Panturrilha em pé", "Panturrilhas", "Máquina", "Flexão plantar com joelhos estendidos e foco em gastrocnêmio."),
        new ExercicioBase("Panturrilha sentada", "Panturrilhas", "Máquina", "Flexão plantar com joelhos flexionados e maior participação do sóleo."),
        new ExercicioBase("Panturrilha no leg press", "Panturrilhas", "Máquina", "Flexão plantar em plataforma com joelhos estendidos."),
        new ExercicioBase("Prancha frontal", "Core", "Peso corporal", "Estabilização anti-extensão com alinhamento entre tronco e pelve."),
        new ExercicioBase("Prancha lateral", "Core", "Peso corporal", "Estabilização lateral com foco em oblíquos e cintura pélvica."),
        new ExercicioBase("Dead bug", "Core", "Peso corporal", "Controle lombo-pélvico com movimentação alternada de membros."),
        new ExercicioBase("Pallof press", "Core", "Polia", "Exercício anti-rotação em posição estável."),
        new ExercicioBase("Abdominal na polia", "Core", "Polia", "Flexão de tronco resistida com controle pélvico."),
        new ExercicioBase("Farmer walk", "Core", "Halteres", "Caminhada carregada para pegada, core e estabilidade global."),
        new ExercicioBase("Levantamento terra convencional", "Corpo inteiro", "Barra", "Dobradiça multiarticular para cadeia posterior, tronco e pegada."),
        new ExercicioBase("Kettlebell swing", "Corpo inteiro", "Kettlebell", "Dobradiça balística de quadril com potência e condicionamento."),
        new ExercicioBase("Goblet squat", "Quadríceps", "Kettlebell", "Agachamento com carga anterior próxima ao tronco."),
        new ExercicioBase("Thruster com halteres", "Corpo inteiro", "Halteres", "Agachamento seguido de pressão vertical em movimento integrado."),
        new ExercicioBase("Remo ergométrico", "Cardio", "Ergômetro", "Condicionamento cíclico de corpo inteiro com resistência ajustável."),
        new ExercicioBase("Bicicleta ergométrica", "Cardio", "Bicicleta", "Condicionamento cíclico de baixo impacto com controle de carga."),
        new ExercicioBase("Caminhada inclinada", "Cardio", "Esteira", "Condicionamento contínuo com inclinação ajustável e menor impacto que corrida."),
        new ExercicioBase("Corrida na esteira", "Cardio", "Esteira", "Condicionamento contínuo ou intervalado com velocidade ajustável."),
        new ExercicioBase("Escada ergométrica", "Cardio", "Máquina", "Condicionamento com padrão repetido de subida e demanda de membros inferiores."),
        new ExercicioBase("Mobilidade de tornozelo na parede", "Mobilidade", "Peso corporal", "Mobilidade de dorsiflexão com joelho avançando sobre o pé sem perder contato do calcanhar."),
        new ExercicioBase("90/90 de quadril", "Mobilidade", "Peso corporal", "Mobilidade ativa/passiva de rotação interna e externa de quadril."),
        new ExercicioBase("Rotação torácica em quatro apoios", "Mobilidade", "Peso corporal", "Mobilidade torácica com pelve estável."),
        new ExercicioBase("Alongamento de flexores do quadril", "Mobilidade", "Peso corporal", "Posição de avanço com controle pélvico para flexores do quadril."),
        new ExercicioBase("Wall slide", "Mobilidade", "Peso corporal", "Elevação de braços na parede com controle escapular e torácico."),
        new ExercicioBase("Copenhagen plank", "Adutores", "Banco", "Estabilização lateral com foco em adutores e core."),
        new ExercicioBase("Adução de quadril na máquina", "Adutores", "Máquina", "Adução de quadril guiada com amplitude controlada."),
        new ExercicioBase("Sled push", "Condicionamento", "Trenó", "Empurrar trenó com foco em potência/condicionamento e baixa fase excêntrica."),
        new ExercicioBase("Battle rope", "Condicionamento", "Corda naval", "Ondulações de corda para condicionamento e resistência de membros superiores."),
        new ExercicioBase("Box jump", "Potência", "Caixa", "Salto vertical para caixa com aterrissagem controlada."),
        new ExercicioBase("Medicine ball slam", "Potência", "Medicine ball", "Arremesso explosivo ao solo com participação global."),
        new ExercicioBase("Landmine press unilateral", "Ombros", "Landmine", "Pressão diagonal unilateral com trajetória amigável ao ombro."),
        new ExercicioBase("Remada cavalinho", "Costas", "Landmine", "Puxada horizontal com barra ancorada e foco em dorsais/romboides."),
        new ExercicioBase("Agachamento no hack", "Quadríceps", "Máquina", "Agachamento guiado com suporte de tronco e foco em membros inferiores."),
        new ExercicioBase("Glute ham raise", "Posterior de coxa", "Máquina", "Extensão/flexão combinada para posteriores e glúteos com alta demanda."),
        new ExercicioBase("Reverse fly na polia", "Ombros", "Polia", "Abdução horizontal em cabo com foco em deltoide posterior."),
        new ExercicioBase("Rosca na polia baixa", "Bíceps", "Polia", "Flexão de cotovelo com tensão contínua em cabo."),
        new ExercicioBase("Tríceps testa com barra W", "Tríceps", "Barra W", "Extensão de cotovelo deitado com foco em tríceps."),
        new ExercicioBase("Good morning", "Posterior de coxa", "Barra", "Dobradiça de quadril com barra apoiada e tronco rigidamente controlado.")
    };

}
