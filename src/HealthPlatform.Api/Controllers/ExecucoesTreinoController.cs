using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record ExecucaoItemTreinoRequest(
    Guid ItemTreinoId,
    int? SeriesRealizadas,
    string? RepeticoesRealizadas,
    decimal? CargaRealizada,
    string? UnidadeCarga,
    int? EsforcoPercebido,
    bool Concluido,
    string? Observacoes,
    Guid? ExercicioAlternativoId,
    string? MotivoAlternativa);

public sealed record RegistrarExecucaoTreinoRequest(
    Guid SessaoTreinoId,
    DateTime DataHoraInicioUtc,
    DateTime? DataHoraFimUtc,
    int? DuracaoMinutos,
    int? EsforcoPercebido,
    string? Observacoes,
    IReadOnlyCollection<ExecucaoItemTreinoRequest> Itens);

[ApiController]
[Authorize(Policy = "PatientOnly")]
[Route("api/portal/me/treinos")]
public sealed class ExecucoesTreinoPacienteController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpPost("execucoes")]
    public async Task<IActionResult> Registrar(
        RegistrarExecucaoTreinoRequest request,
        CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var sessao = await db.SessoesTreino.AsNoTracking()
            .Include(x => x.PlanoTreino)
            .Include(x => x.Itens).ThenInclude(x => x.Exercicio)
            .FirstOrDefaultAsync(x =>
                x.Id == request.SessaoTreinoId &&
                x.PlanoTreino.PacienteId == paciente.Id &&
                x.PlanoTreino.Status == "Ativo", ct);

        if (sessao is null)
            return NotFound(new { message = "Sessao de treino ativa nao encontrada." });

        if (request.EsforcoPercebido.HasValue &&
            (request.EsforcoPercebido < 0 || request.EsforcoPercebido > 10))
            return BadRequest(new { message = "Esforco percebido deve estar entre 0 e 10." });

        var validIds = sessao.Itens.Select(x => x.Id).ToHashSet();
        if (request.Itens.Any(x => !validIds.Contains(x.ItemTreinoId)))
            return BadRequest(new { message = "Existe item informado que nao pertence a esta sessao." });

        var idsAlternativos = request.Itens.Where(x => x.ExercicioAlternativoId.HasValue)
            .Select(x => x.ExercicioAlternativoId!.Value).Distinct().ToArray();
        var alternativas = idsAlternativos.Length == 0
            ? new Dictionary<Guid, Exercicio>()
            : await db.Exercicios.AsNoTracking()
                .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo && idsAlternativos.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

        foreach (var solicitado in request.Itens.Where(x => x.ExercicioAlternativoId.HasValue))
        {
            var original = sessao.Itens.First(x => x.Id == solicitado.ItemTreinoId).Exercicio;
            if (!alternativas.TryGetValue(solicitado.ExercicioAlternativoId!.Value, out var alternativa))
                return BadRequest(new { message = "Exercicio alternativo invalido ou indisponivel." });
            if (!string.IsNullOrWhiteSpace(original.GrupoMuscular) &&
                !string.Equals(original.GrupoMuscular.Trim(), alternativa.GrupoMuscular?.Trim(), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "A alternativa precisa pertencer ao mesmo grupo muscular do exercicio prescrito." });
        }

        if (request.Itens.Any(x =>
            (x.SeriesRealizadas.HasValue && x.SeriesRealizadas < 0) ||
            (x.CargaRealizada.HasValue && x.CargaRealizada < 0) ||
            (x.EsforcoPercebido.HasValue && (x.EsforcoPercebido < 0 || x.EsforcoPercebido > 10))))
            return BadRequest(new { message = "Valores de execucao invalidos." });

        var inicio = request.DataHoraInicioUtc.ToUniversalTime();
        var fim = request.DataHoraFimUtc?.ToUniversalTime();
        var duracao = request.DuracaoMinutos;
        if (!duracao.HasValue && fim.HasValue && fim.Value >= inicio)
            duracao = (int)Math.Round((fim.Value - inicio).TotalMinutes);

        var execucao = new ExecucaoTreino
        {
            PacienteId = paciente.Id,
            PlanoTreinoId = sessao.PlanoTreinoId,
            SessaoTreinoId = sessao.Id,
            DataHoraInicioUtc = inicio,
            DataHoraFimUtc = fim,
            DuracaoMinutos = duracao,
            EsforcoPercebido = request.EsforcoPercebido,
            Observacoes = Limpar(request.Observacoes),
            Status = "Concluido"
        };

        foreach (var i in request.Itens)
        {
            execucao.Itens.Add(new ExecucaoItemTreino
            {
                ItemTreinoId = i.ItemTreinoId,
                SeriesRealizadas = i.SeriesRealizadas,
                RepeticoesRealizadas = Limpar(i.RepeticoesRealizadas),
                CargaRealizada = i.CargaRealizada,
                UnidadeCarga = Limpar(i.UnidadeCarga),
                EsforcoPercebido = i.EsforcoPercebido,
                Concluido = i.Concluido,
                Observacoes = MontarObservacaoItem(i.Observacoes, i.ExercicioAlternativoId,
                    i.ExercicioAlternativoId.HasValue && alternativas.TryGetValue(i.ExercicioAlternativoId.Value, out var alternativa) ? alternativa.Nome : null,
                    i.MotivoAlternativa)
            });
        }

        db.ExecucoesTreino.Add(execucao);

        foreach (var solicitado in request.Itens)
        {
            var itemPrescrito = sessao.Itens.First(x => x.Id == solicitado.ItemTreinoId);
            if (solicitado.ExercicioAlternativoId.HasValue &&
                alternativas.TryGetValue(solicitado.ExercicioAlternativoId.Value, out var alternativaEscolhida))
            {
                await EventoDesvioAdesaoService.RegistrarAsync(
                    db, paciente.Id, inicio, "Treino", "ExercicioAlternativo", "Exercício substituído",
                    $"{itemPrescrito.Exercicio.Nome} foi substituído por {alternativaEscolhida.Nome}. Motivo: {Limpar(solicitado.MotivoAlternativa) ?? "não informado"}.",
                    $"TREINO:{execucao.Id}:{solicitado.ItemTreinoId}:ALTERNATIVA", 1,
                    new
                    {
                        execucaoId = execucao.Id,
                        itemTreinoId = solicitado.ItemTreinoId,
                        exercicioOriginalId = itemPrescrito.ExercicioId,
                        exercicioOriginal = itemPrescrito.Exercicio.Nome,
                        exercicioAlternativoId = alternativaEscolhida.Id,
                        exercicioAlternativo = alternativaEscolhida.Nome,
                        motivo = Limpar(solicitado.MotivoAlternativa)
                    }, ct);
            }

            if (!solicitado.Concluido)
            {
                await EventoDesvioAdesaoService.RegistrarAsync(
                    db, paciente.Id, inicio, "Treino", "ExercicioNaoConcluido", "Exercício não concluído",
                    $"{itemPrescrito.Exercicio.Nome} ficou sem conclusão nesta execução.",
                    $"TREINO:{execucao.Id}:{solicitado.ItemTreinoId}:NAO_CONCLUIDO", 2,
                    new { execucaoId = execucao.Id, itemTreinoId = solicitado.ItemTreinoId, exercicio = itemPrescrito.Exercicio.Nome }, ct);
            }
        }

        var dataTreino = DateOnly.FromDateTime(inicio);
        var recomendacao = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x => x.PacienteId == paciente.Id && x.Data == dataTreino)
            .Select(x => x.RecomendacaoTreino)
            .FirstOrDefaultAsync(ct);
        var xpTreino = GamificacaoService.CalcularXpTreino(recomendacao, execucao.EsforcoPercebido);
        await GamificacaoService.RegistrarEventoAsync(db, currentUser.OrganizationId, paciente.Id, dataTreino,
            "Treino", execucao.Id, xpTreino.Pontos, xpTreino.Motivo, xpTreino.Adequacao, ct);

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = "CREATE",
            Entidade = nameof(ExecucaoTreino),
            EntidadeId = execucao.Id.ToString(),
            DadosNovosJson = JsonSerializer.Serialize(new
            {
                execucao.PacienteId,
                execucao.PlanoTreinoId,
                execucao.SessaoTreinoId,
                execucao.DataHoraInicioUtc,
                execucao.DuracaoMinutos,
                execucao.EsforcoPercebido,
                Itens = request.Itens.Count
            }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        await db.SaveChangesAsync(ct);
        return Ok(new { execucao.Id, execucao.Status, execucao.DuracaoMinutos });
    }

    [HttpGet("historico")]
    public async Task<IActionResult> Historico([FromQuery] int dias = 90, CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 7, 365);

        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var desde = DateTime.UtcNow.AddDays(-dias);

        var itens = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.SessaoTreino)
            .Include(x => x.PlanoTreino)
            .Include(x => x.Itens).ThenInclude(x => x.ItemTreino).ThenInclude(x => x.Exercicio)
            .Where(x => x.PacienteId == pacienteId.Value && x.DataHoraInicioUtc >= desde)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        return Ok(new
        {
            dias,
            total = itens.Count,
            execucoes = itens.Select(ToHistory)
        });
    }

    private static object ToHistory(ExecucaoTreino x) => new
    {
        x.Id,
        x.DataHoraInicioUtc,
        x.DataHoraFimUtc,
        x.DuracaoMinutos,
        x.EsforcoPercebido,
        x.Observacoes,
        x.Status,
        plano = x.PlanoTreino.Nome,
        sessao = x.SessaoTreino.Nome,
        itens = x.Itens.Select(i => new
        {
            i.ItemTreinoId,
            exercicio = i.ItemTreino.Exercicio.Nome,
            i.SeriesRealizadas,
            i.RepeticoesRealizadas,
            i.CargaRealizada,
            i.UnidadeCarga,
            i.EsforcoPercebido,
            i.Concluido,
            i.Observacoes
        })
    };

    private static string? MontarObservacaoItem(string? observacao, Guid? exercicioAlternativoId, string? exercicioAlternativoNome, string? motivo)
    {
        var texto = Limpar(observacao);
        if (!exercicioAlternativoId.HasValue || string.IsNullOrWhiteSpace(exercicioAlternativoNome))
            return texto;

        static string Safe(string? x) => (x ?? string.Empty).Replace("|", "/").Replace("]", ")").Trim();
        var marcador = $"[AESYN_ALT_EXERCICIO:{exercicioAlternativoId.Value}|{Safe(exercicioAlternativoNome)}|{Safe(motivo)}]";
        return string.IsNullOrWhiteSpace(texto) ? marcador : $"{marcador} {texto}";
    }

    private static string? Limpar(string? x)
        => string.IsNullOrWhiteSpace(x) ? null : x.Trim();
}

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/treinos")]
public sealed class ExecucoesTreinoProfissionalController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet("historico")]
    public async Task<IActionResult> Historico(
        Guid pacienteId,
        [FromQuery] int dias = 90,
        CancellationToken ct = default)
    {
        dias = Math.Clamp(dias, 7, 365);

        var existe = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (!existe)
            return NotFound(new { message = "Paciente nao encontrado." });

        var desde = DateTime.UtcNow.AddDays(-dias);

        var itens = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.SessaoTreino)
            .Include(x => x.PlanoTreino)
            .Include(x => x.Itens).ThenInclude(x => x.ItemTreino).ThenInclude(x => x.Exercicio)
            .Where(x => x.PacienteId == pacienteId && x.DataHoraInicioUtc >= desde)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        var exercicios = itens
            .SelectMany(x => x.Itens.Select(i => new
            {
                i.ItemTreino.ExercicioId,
                Exercicio = i.ItemTreino.Exercicio.Nome,
                x.DataHoraInicioUtc,
                i.CargaRealizada,
                i.SeriesRealizadas,
                i.RepeticoesRealizadas
            }))
            .Where(x => x.CargaRealizada.HasValue)
            .GroupBy(x => new { x.ExercicioId, x.Exercicio })
            .Select(g => new
            {
                g.Key.ExercicioId,
                g.Key.Exercicio,
                ultimaCarga = g.OrderByDescending(x => x.DataHoraInicioUtc).First().CargaRealizada,
                maiorCarga = g.Max(x => x.CargaRealizada),
                registros = g.OrderBy(x => x.DataHoraInicioUtc)
                    .Select(x => new
                    {
                        x.DataHoraInicioUtc,
                        x.CargaRealizada,
                        x.SeriesRealizadas,
                        x.RepeticoesRealizadas
                    })
            })
            .OrderBy(x => x.Exercicio)
            .ToList();

        return Ok(new
        {
            dias,
            totalTreinos = itens.Count,
            minutosTotais = itens.Sum(x => x.DuracaoMinutos ?? 0),
            esforcoMedio = itens.Where(x => x.EsforcoPercebido.HasValue).Any()
                ? Math.Round(itens.Where(x => x.EsforcoPercebido.HasValue)
                    .Average(x => x.EsforcoPercebido!.Value), 1)
                : (double?)null,
            evolucaoCarga = exercicios,
            execucoes = itens.Select(x => new
            {
                x.Id,
                x.DataHoraInicioUtc,
                x.DuracaoMinutos,
                x.EsforcoPercebido,
                plano = x.PlanoTreino.Nome,
                sessao = x.SessaoTreino.Nome,
                itens = x.Itens.Select(i => new
                {
                    exercicio = i.ItemTreino.Exercicio.Nome,
                    i.SeriesRealizadas,
                    i.RepeticoesRealizadas,
                    i.CargaRealizada,
                    i.UnidadeCarga,
                    i.Concluido
                })
            })
        });
    }
}
