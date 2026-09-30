using System.Text.RegularExpressions;
using HealthPlatform.Api.Contracts.PerformancePassport;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class AthletePerformancePassportService
{
    public static async Task<AthletePerformancePassportResponse> MontarAsync(
        AppDbContext db,
        Guid pacienteId,
        CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var performance = await PerformanceEsportivaService.MontarAsync(db, pacienteId, hoje, ct);
        var recordes = await MontarRecordesAsync(db, pacienteId, hoje, ct);
        var tempos = await MontarTemposAsync(db, pacienteId, hoje, ct);
        var resultados = await MontarResultadosCompeticaoTesteAsync(db, pacienteId, hoje, ct);
        var habilidadesMarcos = await MontarHabilidadesMarcosAsync(db, pacienteId, hoje, ct);

        var melhoresMarcas = performance.Destaques
            .Where(x => x.MelhorCarga.HasValue)
            .Select(x => new AthletePerformancePassportRecordResponse(
                x.ExercicioId,
                x.Exercicio,
                x.GrupoMuscular,
                "MelhorCarga",
                x.MelhorCarga,
                x.UnidadeCarga,
                x.DataMelhorCarga,
                x.NovoPrRecente,
                x.Registros,
                "ExecucoesTreino"))
            .OrderByDescending(x => x.Recente)
            .ThenByDescending(x => x.DataUtc)
            .ThenBy(x => x.Exercicio)
            .ToArray();

        var recordesObservados = recordes.Count(x => x.Natureza == "Observado");

        var dominios = new AthletePerformancePassportDomainResponse[]
        {
            new(
                "cargas",
                "Cargas",
                recordesObservados > 0 ? "ComDados" : "SemDados",
                "Execuções de treino concluídas",
                recordesObservados,
                "Melhores cargas são comparadas apenas no mesmo exercício e na mesma unidade."),
            new(
                "recordes",
                "Recordes",
                recordes.Count > 0 ? "ComDados" : "SemDados",
                "Performance Records 2.0",
                recordes.Count,
                "Recordes observados e marcas derivadas são identificados separadamente."),
            new(
                "tempos",
                "Tempos",
                tempos.Count > 0 ? "ComDados" : "SemDados",
                "Timed Performance 2.0",
                tempos.Count,
                "Duração real é comparada somente dentro da mesma sessão. Menor duração não significa melhor performance automaticamente."),
            new(
                "provas",
                "Provas",
                resultados.Any(x => x.Categoria == "ProvaCompeticao") ? "ComDados" : "SemDados",
                "Competition & Test Results 2.0",
                resultados.Count(x => x.Categoria == "ProvaCompeticao"),
                "Somente eventos supervisionados com indicação explícita de prova, competição, campeonato, corrida ou torneio entram neste domínio."),
            new(
                "testes",
                "Testes",
                resultados.Any(x => x.Categoria == "TesteAvaliacao") ? "ComDados" : "SemDados",
                "Competition & Test Results 2.0",
                resultados.Count(x => x.Categoria == "TesteAvaliacao"),
                "Somente eventos supervisionados com indicação explícita de teste, avaliação, benchmark ou protocolo entram neste domínio."),
            new(
                "habilidades",
                "Habilidades",
                habilidadesMarcos.Any(x => x.Categoria == "HabilidadeRegistrada") ? "ComDados" : "SemDados",
                "Skills & Milestones 2.0",
                habilidadesMarcos.Count(x => x.Categoria == "HabilidadeRegistrada"),
                "Somente registros supervisionados explicitamente identificados como habilidade, técnica, competência ou fundamento entram neste domínio."),
            new(
                "marcos",
                "Marcos",
                habilidadesMarcos.Any(x => x.Categoria == "MarcoRegistrado") ? "ComDados" : "SemDados",
                "Skills & Milestones 2.0",
                habilidadesMarcos.Count(x => x.Categoria == "MarcoRegistrado"),
                "Marcos exigem registro supervisionado explícito; recordes, tempos e volume não viram conquista automaticamente.")
        };

        var estado = recordes.Count > 0 ? "PerformanceRecordsDisponiveis" : tempos.Count > 0 ? "TimedPerformanceDisponivel" : "BaseEmConstrucao";

        return new AthletePerformancePassportResponse(
            "v0.28.4",
            performance.DiasObservados,
            performance.TreinosPeriodo,
            performance.PrsRecentes,
            estado,
            dominios,
            melhoresMarcas,
            "Skills & Milestones 2.0 incorpora somente habilidades e marcos explicitamente registrados em contexto supervisionado. Não certifica domínio técnico, não cria conquista automática e não converte recordes, cargas ou tempos em habilidade.")
        {
            Recordes = recordes,
            Tempos = tempos,
            Resultados = resultados,
            HabilidadesMarcos = habilidadesMarcos
        };
    }

    public static async Task<IReadOnlyCollection<AthletePerformanceRecordResponse>> MontarRecordesAsync(
        AppDbContext db,
        Guid pacienteId,
        DateOnly dia,
        CancellationToken ct)
    {
        const int diasObservados = 180;
        var inicioUtc = dia.AddDays(-(diasObservados - 1)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var recenteDesdeUtc = dia.AddDays(-6).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var itens = await db.ExecucoesItensTreino.AsNoTracking()
            .Where(x =>
                x.ExecucaoTreino.PacienteId == pacienteId &&
                x.ExecucaoTreino.Status == "Concluido" &&
                x.ExecucaoTreino.DataHoraInicioUtc >= inicioUtc &&
                x.ExecucaoTreino.DataHoraInicioUtc < fimUtc &&
                x.Concluido)
            .Select(x => new
            {
                x.ItemTreino.ExercicioId,
                Exercicio = x.ItemTreino.Exercicio.Nome,
                GrupoMuscular = x.ItemTreino.Exercicio.GrupoMuscular,
                x.CargaRealizada,
                UnidadeCarga = x.UnidadeCarga ?? x.ItemTreino.UnidadeCarga,
                x.SeriesRealizadas,
                x.RepeticoesRealizadas,
                x.ExecucaoTreino.DataHoraInicioUtc
            })
            .ToListAsync(ct);

        var records = new List<AthletePerformanceRecordResponse>();

        foreach (var grupo in itens
            .Where(x => x.CargaRealizada.HasValue && x.CargaRealizada.Value > 0m)
            .GroupBy(x => new
            {
                x.ExercicioId,
                x.Exercicio,
                x.GrupoMuscular,
                Unidade = NormalizarUnidade(x.UnidadeCarga)
            }))
        {
            var comparaveis = grupo
                .OrderBy(x => x.DataHoraInicioUtc)
                .ToList();

            if (comparaveis.Count == 0)
                continue;

            var melhorCarga = comparaveis
                .OrderByDescending(x => x.CargaRealizada)
                .ThenByDescending(x => x.DataHoraInicioUtc)
                .First();

            var primeiraCarga = comparaveis.First().CargaRealizada;
            decimal? evolucaoCarga = null;
            if (primeiraCarga.HasValue && primeiraCarga.Value > 0m && melhorCarga.CargaRealizada.HasValue)
            {
                evolucaoCarga = Math.Round(
                    (melhorCarga.CargaRealizada.Value - primeiraCarga.Value) / primeiraCarga.Value * 100m,
                    1);
            }

            records.Add(new AthletePerformanceRecordResponse(
                grupo.Key.ExercicioId,
                grupo.Key.Exercicio,
                grupo.Key.GrupoMuscular,
                "MelhorCarga",
                "Observado",
                melhorCarga.CargaRealizada!.Value,
                grupo.Key.Unidade,
                melhorCarga.DataHoraInicioUtc,
                melhorCarga.DataHoraInicioUtc >= recenteDesdeUtc,
                comparaveis.Count,
                evolucaoCarga,
                "Maior carga efetivamente registrada no mesmo exercício e na mesma unidade dentro de 180 dias.",
                "ExecucoesTreino"));

            var volumes = comparaveis
                .Select(x => new
                {
                    Item = x,
                    Volume = CalcularVolumeEstimado(
                        x.CargaRealizada,
                        x.SeriesRealizadas,
                        x.RepeticoesRealizadas)
                })
                .Where(x => x.Volume.HasValue)
                .ToList();

            if (volumes.Count > 0)
            {
                var melhorVolume = volumes
                    .OrderByDescending(x => x.Volume)
                    .ThenByDescending(x => x.Item.DataHoraInicioUtc)
                    .First();

                records.Add(new AthletePerformanceRecordResponse(
                    grupo.Key.ExercicioId,
                    grupo.Key.Exercicio,
                    grupo.Key.GrupoMuscular,
                    "MelhorVolumeEstimado",
                    "Derivado",
                    melhorVolume.Volume!.Value,
                    $"{grupo.Key.Unidade}·rep",
                    melhorVolume.Item.DataHoraInicioUtc,
                    melhorVolume.Item.DataHoraInicioUtc >= recenteDesdeUtc,
                    volumes.Count,
                    null,
                    "Maior carga × repetições estimadas no mesmo exercício e unidade; é uma marca derivada, não uma carga observada.",
                    "ExecucoesTreino"));
            }
        }

        return records
            .OrderByDescending(x => x.Recente)
            .ThenBy(x => x.Natureza == "Observado" ? 0 : 1)
            .ThenByDescending(x => x.DataUtc)
            .ThenBy(x => x.Exercicio)
            .ThenBy(x => x.Tipo)
            .ToArray();
    }




    public static async Task<IReadOnlyCollection<AthleteSkillMilestoneResponse>> MontarHabilidadesMarcosAsync(
        AppDbContext db,
        Guid pacienteId,
        DateOnly dia,
        CancellationToken ct)
    {
        const int diasObservados = 365;
        var inicioUtc = dia.AddDays(-(diasObservados - 1)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var eventos = await db.EventosProgressaoSupervisionada.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.DataAplicacaoUtc >= inicioUtc &&
                x.DataAplicacaoUtc < fimUtc)
            .OrderByDescending(x => x.DataAplicacaoUtc)
            .Select(x => new
            {
                x.Id,
                x.Eixo,
                x.Descricao,
                x.DataAplicacaoUtc,
                x.Status,
                x.Observacoes,
                x.CicloEsportivoPacienteId
            })
            .ToListAsync(ct);

        return eventos
            .Select(x => new
            {
                Evento = x,
                Categoria = ClassificarHabilidadeMarco(x.Eixo, x.Descricao)
            })
            .Where(x => x.Categoria is not null)
            .Select(x => new AthleteSkillMilestoneResponse(
                x.Evento.Id,
                x.Categoria!,
                x.Evento.Eixo,
                x.Evento.Descricao,
                x.Evento.DataAplicacaoUtc,
                x.Evento.Status,
                x.Evento.Observacoes,
                x.Evento.CicloEsportivoPacienteId,
                "RegistroSupervisionado",
                "EventosProgressaoSupervisionada",
                x.Categoria == "HabilidadeRegistrada"
                    ? "O registro descreve uma habilidade supervisionada explicitamente documentada; não equivale a certificação automática de domínio técnico."
                    : "O registro descreve um marco supervisionado explicitamente documentado; não é conquista automática derivada de carga, tempo, volume ou frequência."))
            .ToArray();
    }

    private static string? ClassificarHabilidadeMarco(string? eixo, string? descricao)
    {
        var texto = $"{eixo} {descricao}".ToLowerInvariant();

        if (ContemTermoExplicito(
                texto,
                "habilidade",
                "skill",
                "técnica",
                "tecnica",
                "competência",
                "competencia",
                "fundamento"))
            return "HabilidadeRegistrada";

        if (ContemTermoExplicito(
                texto,
                "marco",
                "milestone",
                "conquista",
                "meta atingida",
                "objetivo atingido",
                "recorde pessoal"))
            return "MarcoRegistrado";

        return null;
    }

    public static async Task<IReadOnlyCollection<AthleteCompetitionTestResultResponse>> MontarResultadosCompeticaoTesteAsync(
        AppDbContext db,
        Guid pacienteId,
        DateOnly dia,
        CancellationToken ct)
    {
        const int diasObservados = 365;
        var inicioUtc = dia.AddDays(-(diasObservados - 1)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var eventos = await db.EventosProgressaoSupervisionada.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.DataAplicacaoUtc >= inicioUtc &&
                x.DataAplicacaoUtc < fimUtc)
            .OrderByDescending(x => x.DataAplicacaoUtc)
            .Select(x => new
            {
                x.Id,
                x.Eixo,
                x.Descricao,
                x.DataAplicacaoUtc,
                x.Status,
                x.Observacoes,
                x.CicloEsportivoPacienteId
            })
            .ToListAsync(ct);

        return eventos
            .Select(x => new
            {
                Evento = x,
                Categoria = ClassificarResultadoCompeticaoTeste(x.Eixo, x.Descricao)
            })
            .Where(x => x.Categoria is not null)
            .Select(x => new AthleteCompetitionTestResultResponse(
                x.Evento.Id,
                x.Categoria!,
                x.Evento.Eixo,
                x.Evento.Descricao,
                x.Evento.DataAplicacaoUtc,
                x.Evento.Status,
                x.Evento.Observacoes,
                x.Evento.CicloEsportivoPacienteId,
                "RegistroSupervisionado",
                "EventosProgressaoSupervisionada",
                "O passaporte preserva o registro supervisionado como contexto. Sem campo estruturado específico, não infere colocação, tempo, distância, nota, aprovação, recorde ou melhora."))
            .ToArray();
    }

    private static string? ClassificarResultadoCompeticaoTeste(string? eixo, string? descricao)
    {
        var texto = $"{eixo} {descricao}".ToLowerInvariant();

        if (ContemTermoExplicito(texto, "prova", "competição", "competicao", "campeonato", "torneio", "corrida"))
            return "ProvaCompeticao";

        if (ContemTermoExplicito(texto, "teste", "avaliação", "avaliacao", "benchmark", "protocolo"))
            return "TesteAvaliacao";

        return null;
    }

    private static bool ContemTermoExplicito(string texto, params string[] termos) =>
        termos.Any(termo => texto.Contains(termo, StringComparison.OrdinalIgnoreCase));

    public static async Task<IReadOnlyCollection<AthleteTimedPerformanceResponse>> MontarTemposAsync(
        AppDbContext db,
        Guid pacienteId,
        DateOnly dia,
        CancellationToken ct)
    {
        const int diasObservados = 180;
        var inicioUtc = dia.AddDays(-(diasObservados - 1)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var sessoes = await db.ExecucoesTreino.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Status == "Concluido" &&
                x.DataHoraInicioUtc >= inicioUtc &&
                x.DataHoraInicioUtc < fimUtc)
            .Select(x => new
            {
                x.SessaoTreinoId,
                Sessao = x.SessaoTreino.Nome,
                x.DataHoraInicioUtc,
                x.DataHoraFimUtc,
                x.DuracaoMinutos
            })
            .ToListAsync(ct);

        var comparaveis = sessoes
            .Select(x => new
            {
                x.SessaoTreinoId,
                x.Sessao,
                x.DataHoraInicioUtc,
                Duracao = ResolverDuracaoMinutos(x.DuracaoMinutos, x.DataHoraInicioUtc, x.DataHoraFimUtc),
                Origem = x.DuracaoMinutos.HasValue && x.DuracaoMinutos.Value > 0
                    ? "DuracaoRegistrada"
                    : "CalculadaPorTimestamps"
            })
            .Where(x => x.Duracao.HasValue && x.Duracao.Value > 0)
            .ToList();

        return comparaveis
            .GroupBy(x => new { x.SessaoTreinoId, x.Sessao })
            .Select(grupo =>
            {
                var ordenados = grupo
                    .OrderBy(x => x.DataHoraInicioUtc)
                    .ToList();
                var recente = ordenados[^1];
                var duracoes = ordenados.Select(x => x.Duracao!.Value).ToArray();

                return new AthleteTimedPerformanceResponse(
                    grupo.Key.SessaoTreinoId,
                    grupo.Key.Sessao,
                    duracoes.Length,
                    recente.Duracao!.Value,
                    duracoes.Min(),
                    duracoes.Max(),
                    Math.Round((decimal)duracoes.Average(), 1),
                    recente.DataHoraInicioUtc,
                    recente.Origem,
                    "Menor, maior e média descrevem duração da mesma sessão. Menor duração não significa melhor performance, maior intensidade ou melhor condicionamento automaticamente.");
            })
            .OrderByDescending(x => x.UltimaExecucaoUtc)
            .ThenBy(x => x.Sessao)
            .ToArray();
    }

    private static int? ResolverDuracaoMinutos(
        int? duracaoRegistrada,
        DateTime inicioUtc,
        DateTime? fimUtc)
    {
        if (duracaoRegistrada.HasValue && duracaoRegistrada.Value > 0)
            return duracaoRegistrada.Value;

        if (!fimUtc.HasValue || fimUtc.Value <= inicioUtc)
            return null;

        var minutos = (int)Math.Round((fimUtc.Value - inicioUtc).TotalMinutes, MidpointRounding.AwayFromZero);
        return minutos > 0 ? minutos : null;
    }

    private static string NormalizarUnidade(string? unidade) =>
        string.IsNullOrWhiteSpace(unidade) ? "unidade-nao-informada" : unidade.Trim().ToLowerInvariant();

    private static decimal? CalcularVolumeEstimado(decimal? carga, int? series, string? repeticoes)
    {
        if (!carga.HasValue || carga.Value <= 0m || string.IsNullOrWhiteSpace(repeticoes))
            return null;

        var numeros = Regex.Matches(repeticoes, @"\d+")
            .Select(x => int.Parse(x.Value))
            .ToArray();

        if (numeros.Length == 0)
            return null;

        int totalRepeticoes;
        if (repeticoes.Contains(',') || repeticoes.Contains(';') || repeticoes.Contains('/'))
        {
            totalRepeticoes = numeros.Sum();
        }
        else
        {
            totalRepeticoes = numeros[0] * Math.Max(1, series ?? 1);
        }

        return totalRepeticoes <= 0
            ? null
            : Math.Round(carga.Value * totalRepeticoes, 1);
    }
}
