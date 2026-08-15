using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
public sealed class AnaliseProgressoTreinoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    private sealed record RegistroExercicio(
        Guid ExercicioId,
        string Exercicio,
        string GrupoMuscular,
        DateTime DataUtc,
        decimal Carga,
        string Unidade,
        int? Series,
        string? Repeticoes,
        int? Rpe);

    private sealed record PontoAnaliseExercicio(
        DateTime dataUtc,
        decimal carga,
        int? Series,
        string? Repeticoes,
        int? rpe);

    private sealed record AnaliseExercicioResponse(
        Guid exercicioId,
        string exercicio,
        string grupoMuscular,
        string unidade,
        int registros,
        string status,
        bool revisaoSugerida,
        decimal? mediaCargaAnterior,
        decimal mediaCargaRecente,
        decimal? variacaoRecentePercentual,
        decimal? mediaRpeRecente,
        decimal maiorCarga,
        DateTime maiorCargaUtc,
        int diasSemRecorde,
        decimal ultimaCarga,
        DateTime ultimaCargaUtc,
        int? ultimoRpe,
        IReadOnlyCollection<string> sinais,
        IReadOnlyCollection<PontoAnaliseExercicio> pontos);

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/treinos/analise-progresso")]
    public async Task<IActionResult> Profissional(
        Guid pacienteId,
        [FromQuery] int dias = 120,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        return Ok(await Montar(pacienteId, dias, ct));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/treinos/analise-progresso")]
    public async Task<IActionResult> Paciente(
        [FromQuery] int dias = 120,
        CancellationToken ct = default)
    {
        var pacienteId = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        return Ok(await Montar(pacienteId.Value, dias, ct));
    }

    private async Task<object> Montar(Guid pacienteId, int dias, CancellationToken ct)
    {
        dias = Math.Clamp(dias, 21, 365);
        var desde = DateTime.UtcNow.AddDays(-dias);

        var execucoes = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Itens)
                .ThenInclude(x => x.ItemTreino)
                .ThenInclude(x => x.Exercicio)
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                x.DataHoraInicioUtc >= desde &&
                x.Status == "Concluido")
            .OrderBy(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        var registros = execucoes
            .SelectMany(execucao => execucao.Itens
                .Where(item =>
                    item.Concluido &&
                    item.CargaRealizada.HasValue &&
                    item.CargaRealizada.Value >= 0m)
                .Select(item => new RegistroExercicio(
                    item.ItemTreino.ExercicioId,
                    item.ItemTreino.Exercicio.Nome,
                    Grupo(item.ItemTreino.Exercicio.GrupoMuscular),
                    execucao.DataHoraInicioUtc,
                    item.CargaRealizada!.Value,
                    NormalizarUnidade(item.UnidadeCarga ?? item.ItemTreino.UnidadeCarga),
                    item.SeriesRealizadas,
                    Limpar(item.RepeticoesRealizadas),
                    item.EsforcoPercebido)))
            .OrderBy(x => x.DataUtc)
            .ToList();

        var analises = registros
            .GroupBy(x => new { x.ExercicioId, x.Exercicio, x.GrupoMuscular, x.Unidade })
            .Select(g => AnalisarSerie(
                g.Key.ExercicioId,
                g.Key.Exercicio,
                g.Key.GrupoMuscular,
                g.Key.Unidade,
                g.OrderBy(x => x.DataUtc).ToList()))
            .OrderByDescending(x => x.revisaoSugerida)
            .ThenBy(x => OrdemStatus(x.status))
            .ThenBy(x => x.exercicio)
            .ToList();

        var revisar = analises.Where(x => x.revisaoSugerida).ToList();
        var progredindo = analises.Where(x => x.status == "Progredindo").ToList();

        return new
        {
            pacienteId,
            dias,
            desdeUtc = desde,
            resumo = new
            {
                execucoesPeriodo = execucoes.Count,
                seriesAnalisadas = analises.Count,
                progredindo = analises.Count(x => x.status == "Progredindo"),
                estagnacao = analises.Count(x => x.status == "Estagnacao"),
                possivelFadiga = analises.Count(x => x.status == "PossivelFadiga"),
                estavel = analises.Count(x => x.status == "Estavel"),
                semBase = analises.Count(x => x.status == "SemBase"),
                revisaoSugerida = revisar.Count
            },
            destaques = new
            {
                revisar = revisar.Take(5).Select(x => new
                {
                    x.exercicioId,
                    x.exercicio,
                    x.grupoMuscular,
                    x.unidade,
                    x.status,
                    x.variacaoRecentePercentual,
                    x.mediaRpeRecente,
                    x.diasSemRecorde
                }).ToList(),
                progredindo = progredindo.Take(5).Select(x => new
                {
                    x.exercicioId,
                    x.exercicio,
                    x.grupoMuscular,
                    x.unidade,
                    x.variacaoRecentePercentual,
                    x.maiorCarga,
                    x.diasSemRecorde
                }).ToList()
            },
            exercicios = analises,
            observacao = "Os sinais sao heuristicas de acompanhamento esportivo baseadas em carga e RPE registrados. Nao representam diagnostico de fadiga, lesao ou overtraining e nao prescrevem aumento de carga automaticamente."
        };
    }

    private static AnaliseExercicioResponse AnalisarSerie(
        Guid exercicioId,
        string exercicio,
        string grupoMuscular,
        string unidade,
        IReadOnlyList<RegistroExercicio> pontos)
    {
        var ultimo = pontos.Last();
        var maior = pontos.Max(x => x.Carga);
        var ultimoRecorde = pontos
            .Where(x => x.Carga == maior)
            .OrderByDescending(x => x.DataUtc)
            .First();

        var diasSemRecorde = Math.Max(0, (DateTime.UtcNow.Date - ultimoRecorde.DataUtc.Date).Days);

        if (pontos.Count < 3)
        {
            return new AnaliseExercicioResponse(
                exercicioId,
                exercicio,
                grupoMuscular,
                unidade,
                pontos.Count,
                "SemBase",
                false,
                null,
                Math.Round(pontos.Average(x => x.Carga), 2),
                null,
                MediaRpe(pontos),
                maior,
                ultimoRecorde.DataUtc,
                diasSemRecorde,
                ultimo.Carga,
                ultimo.DataUtc,
                ultimo.Rpe,
                new[] { "Menos de 3 registros com carga." },
                Pontos(pontos));
        }

        var recentes = pontos.TakeLast(Math.Min(3, pontos.Count)).ToList();
        var anterioresDisponiveis = pontos.Take(Math.Max(0, pontos.Count - recentes.Count)).ToList();
        var anteriores = anterioresDisponiveis.TakeLast(Math.Min(3, anterioresDisponiveis.Count)).ToList();

        var mediaRecente = Math.Round(recentes.Average(x => x.Carga), 2);
        var mediaAnterior = anteriores.Count > 0
            ? Math.Round(anteriores.Average(x => x.Carga), 2)
            : Math.Round(pontos.First().Carga, 2);

        decimal? variacao = mediaAnterior > 0m
            ? Math.Round((mediaRecente - mediaAnterior) * 100m / mediaAnterior, 1)
            : null;

        var mediaRpe = MediaRpe(recentes);
        var recordeNaJanelaRecente = recentes.Any(x =>
            x.Carga == maior &&
            x.DataUtc == ultimoRecorde.DataUtc);

        var possivelFadiga =
            pontos.Count >= 4 &&
            variacao.HasValue &&
            variacao.Value <= -3m &&
            mediaRpe.HasValue &&
            mediaRpe.Value >= 8m;

        var estagnacao =
            pontos.Count >= 5 &&
            variacao.HasValue &&
            Math.Abs(variacao.Value) <= 2m &&
            !recordeNaJanelaRecente;

        var progredindo =
            recordeNaJanelaRecente ||
            (variacao.HasValue && variacao.Value > 2m);

        string status;
        if (possivelFadiga)
            status = "PossivelFadiga";
        else if (estagnacao)
            status = "Estagnacao";
        else if (progredindo)
            status = "Progredindo";
        else
            status = "Estavel";

        var sinais = new List<string>();

        if (variacao.HasValue)
            sinais.Add($"Carga media recente {variacao.Value:+0.0;-0.0;0.0}% versus a base anterior.");

        if (mediaRpe.HasValue)
            sinais.Add($"RPE medio recente {mediaRpe.Value:0.0}/10.");

        if (recordeNaJanelaRecente)
            sinais.Add("Melhor carga do periodo apareceu na janela recente.");

        if (estagnacao)
            sinais.Add("Carga recente permaneceu dentro de +/-2% da base, sem novo recorde recente.");

        if (possivelFadiga)
            sinais.Add("Queda de carga >=3% combinada com RPE medio recente >=8/10.");

        return new AnaliseExercicioResponse(
            exercicioId,
            exercicio,
            grupoMuscular,
            unidade,
            pontos.Count,
            status,
            status is "Estagnacao" or "PossivelFadiga",
            mediaAnterior,
            mediaRecente,
            variacao,
            mediaRpe,
            maior,
            ultimoRecorde.DataUtc,
            diasSemRecorde,
            ultimo.Carga,
            ultimo.DataUtc,
            ultimo.Rpe,
            sinais,
            Pontos(pontos));
    }

    private static IReadOnlyCollection<PontoAnaliseExercicio> Pontos(
        IReadOnlyList<RegistroExercicio> pontos) =>
        pontos.Select(x => new PontoAnaliseExercicio(
            x.DataUtc,
            x.Carga,
            x.Series,
            x.Repeticoes,
            x.Rpe))
        .ToList();

    private static decimal? MediaRpe(IEnumerable<RegistroExercicio> pontos)
    {
        var valores = pontos.Where(x => x.Rpe.HasValue).Select(x => x.Rpe!.Value).ToList();
        return valores.Count == 0 ? null : Math.Round((decimal)valores.Average(), 1);
    }

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private static int OrdemStatus(string status) =>
        status switch
        {
            "PossivelFadiga" => 0,
            "Estagnacao" => 1,
            "Progredindo" => 2,
            "Estavel" => 3,
            _ => 4
        };

    private static string NormalizarUnidade(string? unidade)
    {
        if (string.IsNullOrWhiteSpace(unidade))
            return "sem unidade";

        var valor = unidade.Trim().ToLowerInvariant();
        return valor switch
        {
            "kg" or "kgs" or "quilo" or "quilos" => "kg",
            "lb" or "lbs" or "libra" or "libras" => "lb",
            _ => unidade.Trim()
        };
    }

    private static string Grupo(string? grupo) =>
        string.IsNullOrWhiteSpace(grupo) ? "Nao informado" : grupo.Trim();

    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
