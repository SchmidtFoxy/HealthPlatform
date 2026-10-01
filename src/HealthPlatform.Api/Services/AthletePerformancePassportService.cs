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
        var evolucao = await MontarEvolucaoAsync(db, pacienteId, hoje, recordes, tempos, resultados, habilidadesMarcos, ct);
        var inteligenciaProgresso = MontarInteligenciaProgresso(evolucao, resultados, habilidadesMarcos);
        var contextoSinaisProgresso = MontarContextoSinaisProgresso(inteligenciaProgresso, hoje);
        var timelineMultissinal = MontarTimelineMultissinal(evolucao, contextoSinaisProgresso);
        var janelasEvidenciaProgresso = MontarJanelasEvidenciaProgresso(timelineMultissinal, hoje);
        var mapaObservacaoCruzada = MontarMapaObservacaoCruzada(timelineMultissinal);
        var resumoObservacionalProgresso = MontarResumoObservacionalProgresso(inteligenciaProgresso, contextoSinaisProgresso, timelineMultissinal, janelasEvidenciaProgresso, mapaObservacaoCruzada);
        var fechamentoInteligenciaProgresso = MontarFechamentoInteligenciaProgresso(inteligenciaProgresso, contextoSinaisProgresso, timelineMultissinal, janelasEvidenciaProgresso, mapaObservacaoCruzada, resumoObservacionalProgresso);
        var workspaceRevisaoProgresso = MontarWorkspaceRevisaoProgresso(inteligenciaProgresso, contextoSinaisProgresso, timelineMultissinal, janelasEvidenciaProgresso, mapaObservacaoCruzada, resumoObservacionalProgresso, fechamentoInteligenciaProgresso);
        var fundacaoNotasRevisaoProgresso = MontarFundacaoNotasRevisaoProgresso(workspaceRevisaoProgresso);

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
            "v0.42.6",
            performance.DiasObservados,
            performance.TreinosPeriodo,
            performance.PrsRecentes,
            estado,
            dominios,
            melhoresMarcas,
            "Professional Review Shared Context Closure fecha a linha 0.42.x consolidando fundação, persistência, status, histórico, filtros e resumo em um estado estrutural único de prontidão do contexto profissional compartilhado.")
        {
            Recordes = recordes,
            Tempos = tempos,
            Resultados = resultados,
            HabilidadesMarcos = habilidadesMarcos,
            Evolucao = evolucao,
            InteligenciaProgresso = inteligenciaProgresso,
            ContextoSinaisProgresso = contextoSinaisProgresso,
            TimelineMultissinal = timelineMultissinal,
            JanelasEvidenciaProgresso = janelasEvidenciaProgresso,
            MapaObservacaoCruzada = mapaObservacaoCruzada,
            ResumoObservacionalProgresso = resumoObservacionalProgresso,
            FechamentoInteligenciaProgresso = fechamentoInteligenciaProgresso,
            WorkspaceRevisaoProgresso = workspaceRevisaoProgresso,
            FundacaoNotasRevisaoProgresso = fundacaoNotasRevisaoProgresso
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














    public static ProgressReviewNotesFoundationResponse MontarFundacaoNotasRevisaoProgresso(
        ProgressReviewWorkspaceResponse workspace)
    {
        var campos = new[]
        {
            new ProgressReviewNoteFieldResponse(
                "dado-observado",
                "Dado observado",
                "Observacao",
                "Registrar o fato ou evidência objetiva que motivou a revisão, sem interpretação embutida.",
                true),

            new ProgressReviewNoteFieldResponse(
                "interpretacao-profissional",
                "Interpretação profissional",
                "Interpretacao",
                "Registrar a leitura profissional de forma separada do dado observado.",
                false),

            new ProgressReviewNoteFieldResponse(
                "ponto-atencao",
                "Ponto de atenção",
                "Acompanhamento",
                "Registrar algo que merece revisão posterior sem classificá-lo automaticamente como problema.",
                false),

            new ProgressReviewNoteFieldResponse(
                "hipotese-acompanhamento",
                "Hipótese de acompanhamento",
                "Hipotese",
                "Registrar hipótese profissional explícita como hipótese, não como diagnóstico ou conclusão.",
                false),

            new ProgressReviewNoteFieldResponse(
                "proximo-item-revisar",
                "Próximo item a revisar",
                "Planejamento",
                "Registrar qual informação, sinal ou contexto deve ser revisitado posteriormente.",
                false)
        };

        return new ProgressReviewNotesFoundationResponse(
            workspace.DiasObservados,
            workspace.EstadoPreparacao,
            campos,
            campos.Length,
            true,
            "RevisaoProfissional",
            "Progress Review Notes Persistence mantém observações profissionais privadas, com autoria, timestamps e auditoria. Os campos não substituem o dado observado e não representam diagnóstico, prognóstico, prescrição ou recomendação automática.");
    }

    public static ProgressReviewWorkspaceResponse MontarWorkspaceRevisaoProgresso(
        ProgressIntelligenceFoundationResponse foundation,
        ProgressSignalContextSummaryResponse contexto,
        MultiSignalTimelineResponse timeline,
        ProgressEvidenceWindowsResponse janelas,
        CrossSignalObservationMapResponse mapa,
        ProgressObservationSummaryResponse resumo,
        ProgressIntelligenceClosureResponse fechamento)
    {
        var secoes = new[]
        {
            new ProgressReviewWorkspaceSectionResponse(
                "foundation",
                "Progress Intelligence Foundation",
                foundation is not null,
                foundation?.Sinais.Count ?? 0,
                "Sinais descritivos disponíveis para revisão."),

            new ProgressReviewWorkspaceSectionResponse(
                "context",
                "Progress Signal Context",
                contexto is not null,
                contexto?.Contextos.Count ?? 0,
                "Contexto temporal e densidade observacional."),

            new ProgressReviewWorkspaceSectionResponse(
                "timeline",
                "Multi-Signal Timeline",
                timeline is not null,
                timeline?.Eventos.Count ?? 0,
                "Eventos observados organizados cronologicamente."),

            new ProgressReviewWorkspaceSectionResponse(
                "windows",
                "Progress Evidence Windows",
                janelas is not null,
                janelas?.Janelas.Count ?? 0,
                "Cobertura documental por janelas temporais."),

            new ProgressReviewWorkspaceSectionResponse(
                "observation-map",
                "Cross-Signal Observation Map",
                mapa is not null,
                mapa?.Dias.Count ?? 0,
                "Coobservações documentais agrupadas por data."),

            new ProgressReviewWorkspaceSectionResponse(
                "summary",
                "Progress Observation Summary",
                resumo is not null,
                resumo?.EventosTimeline ?? 0,
                "Síntese operacional das camadas observacionais.")
        };

        var disponiveis = secoes.Count(x => x.Disponivel);

        var estado = fechamento.EstadoEstrutural == "EstruturaObservacionalCompleta"
            ? "PreparacaoEstruturalCompleta"
            : "PreparacaoParcial";

        return new ProgressReviewWorkspaceResponse(
            timeline?.DiasObservados ?? 180,
            estado,
            secoes,
            disponiveis,
            secoes.Length,
            "Progress Review Workspace Foundation apenas organiza dados existentes para revisão profissional. Preparação estrutural completa não significa decisão clínica pronta, maior certeza, melhor desempenho, diagnóstico, prognóstico ou recomendação automática.");
    }

    public static ProgressIntelligenceClosureResponse MontarFechamentoInteligenciaProgresso(
        ProgressIntelligenceFoundationResponse foundation,
        ProgressSignalContextSummaryResponse contexto,
        MultiSignalTimelineResponse timeline,
        ProgressEvidenceWindowsResponse janelas,
        CrossSignalObservationMapResponse mapa,
        ProgressObservationSummaryResponse resumo)
    {
        var componentes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["ProgressIntelligenceFoundation"] = foundation is not null,
            ["ProgressSignalContext"] = contexto is not null,
            ["MultiSignalTimeline"] = timeline is not null,
            ["ProgressEvidenceWindows"] = janelas is not null,
            ["CrossSignalObservationMap"] = mapa is not null,
            ["ProgressObservationSummary"] = resumo is not null
        };

        var presentes = componentes
            .Where(x => x.Value)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToArray();

        var ausentes = componentes
            .Where(x => !x.Value)
            .Select(x => x.Key)
            .OrderBy(x => x)
            .ToArray();

        var estado = ausentes.Length == 0
            ? "EstruturaObservacionalCompleta"
            : "EstruturaObservacionalParcial";

        return new ProgressIntelligenceClosureResponse(
            componentes.Count,
            presentes.Length,
            presentes,
            ausentes,
            estado,
            "Progress Intelligence Closure 2.0 descreve apenas a presença estrutural das camadas observacionais. Estrutura completa não significa melhor desempenho, maior qualidade clínica, maior certeza, prognóstico favorável ou recomendação automática.");
    }

    public static ProgressObservationSummaryResponse MontarResumoObservacionalProgresso(
        ProgressIntelligenceFoundationResponse foundation,
        ProgressSignalContextSummaryResponse contexto,
        MultiSignalTimelineResponse timeline,
        ProgressEvidenceWindowsResponse janelas,
        CrossSignalObservationMapResponse mapa)
    {
        var referenciasDistintasTimeline = timeline.Eventos
            .Select(x => $"{x.Dominio}::{x.Referencia}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var cobertura = timeline.Eventos.Count switch
        {
            0 => "SemCoberturaObservacional",
            <= 2 => "CoberturaMinima",
            <= 6 => "CoberturaParcial",
            _ => "CoberturaMaisAmpla"
        };

        return new ProgressObservationSummaryResponse(
            timeline.DiasObservados,
            foundation.Sinais.Count,
            contexto.Contextos.Count,
            timeline.Eventos.Count,
            janelas.Janelas.Count,
            mapa.Dias.Count,
            mapa.DiasComMultiplosDominios,
            mapa.DiasComMultiplasReferencias,
            referenciasDistintasTimeline,
            cobertura,
            "Progress Observation Summary 2.0 resume somente quantidade e cobertura dos dados observados. Cobertura mais ampla não significa melhor desempenho, maior confiança clínica, maior certeza, prognóstico favorável ou necessidade de intervenção.");
    }

    public static CrossSignalObservationMapResponse MontarMapaObservacaoCruzada(
        MultiSignalTimelineResponse timeline)
    {
        var dias = timeline.Eventos
            .GroupBy(x => DateOnly.FromDateTime(x.DataUtc))
            .Select(grupo =>
            {
                var eventos = grupo.ToArray();

                var dominios = eventos
                    .Select(x => x.Dominio)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToArray();

                var referencias = eventos
                    .Select(x => x.Referencia)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToArray();

                var leitura = dominios.Length > 1 || referencias.Length > 1
                    ? "CoobservacaoNoMesmoDia"
                    : "ObservacaoIsoladaNoDia";

                return new CrossSignalObservationDayResponse(
                    grupo.Key,
                    eventos.Length,
                    eventos.Count(x => x.Dominio == "Carga"),
                    eventos.Count(x => x.Dominio == "Tempo"),
                    referencias.Length,
                    dominios,
                    referencias,
                    leitura);
            })
            .OrderBy(x => x.Data)
            .ToArray();

        return new CrossSignalObservationMapResponse(
            timeline.DiasObservados,
            dias,
            dias.Count(x => x.DominiosObservados.Count > 1),
            dias.Count(x => x.ReferenciasDistintas > 1),
            "Cross-Signal Observation Map 2.0 mostra apenas coobservação documental no mesmo dia. Coocorrência temporal não significa correlação, causalidade, influência, resposta fisiológica ou efeito de uma variável sobre outra.");
    }

    public static ProgressEvidenceWindowsResponse MontarJanelasEvidenciaProgresso(
        MultiSignalTimelineResponse timeline,
        DateOnly hoje)
    {
        var fimUtc = hoje.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var definicoes = new[]
        {
            new { Nome = "Recente30d", Dias = 30 },
            new { Nome = "Intermediaria90d", Dias = 90 },
            new { Nome = "Ampla180d", Dias = 180 }
        };

        var janelas = definicoes
            .Select(def =>
            {
                var inicioUtc = fimUtc.AddDays(-def.Dias);
                var eventos = timeline.Eventos
                    .Where(x => x.DataUtc >= inicioUtc && x.DataUtc <= fimUtc)
                    .ToArray();

                var referencias = eventos
                    .Select(x => $"{x.Dominio}::{x.Referencia}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                var cobertura = eventos.Length switch
                {
                    0 => "SemEventosObservados",
                    1 => "EventoIsolado",
                    <= 4 => "CoberturaCurta",
                    _ => "CoberturaMaisAmpla"
                };

                return new ProgressEvidenceWindowItemResponse(
                    def.Nome,
                    def.Dias,
                    inicioUtc,
                    fimUtc,
                    eventos.Length,
                    eventos.Count(x => x.Dominio == "Carga"),
                    eventos.Count(x => x.Dominio == "Tempo"),
                    referencias,
                    cobertura);
            })
            .ToArray();

        return new ProgressEvidenceWindowsResponse(
            timeline.DiasObservados,
            janelas,
            "Progress Evidence Windows 2.0 conta apenas eventos observados dentro de cada janela. Mais registros significam maior cobertura documental, não maior confiança clínica, melhor desempenho, prognóstico ou recomendação.");
    }

    public static MultiSignalTimelineResponse MontarTimelineMultissinal(
        AthletePerformanceEvolutionResponse evolucao,
        ProgressSignalContextSummaryResponse contexto)
    {
        var contextoPorChave = contexto.Contextos
            .GroupBy(x => $"{x.Dominio}::{x.Referencia}")
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var eventos = new List<MultiSignalTimelineEventResponse>();

        foreach (var ponto in evolucao.Pontos)
        {
            var chave = $"{ponto.Dominio}::{ponto.Referencia}";
            contextoPorChave.TryGetValue(chave, out var contextoSinal);

            var recencia = contextoSinal?.Recencia ?? "SemContexto";
            var origem = contextoSinal?.OrigemEvidencia ?? "AthletePerformancePassport";

            eventos.Add(new MultiSignalTimelineEventResponse(
                ponto.Dominio,
                ponto.Referencia,
                ponto.DataInicialUtc,
                "InicioComparavel",
                ponto.Medida,
                ponto.ValorInicial,
                ponto.Unidade,
                ponto.RegistrosComparaveis,
                recencia,
                origem));

            eventos.Add(new MultiSignalTimelineEventResponse(
                ponto.Dominio,
                ponto.Referencia,
                ponto.DataAtualUtc,
                "RegistroAtual",
                ponto.Medida,
                ponto.ValorAtual,
                ponto.Unidade,
                ponto.RegistrosComparaveis,
                recencia,
                origem));
        }

        var ordenados = eventos
            .OrderBy(x => x.DataUtc)
            .ThenBy(x => x.Dominio)
            .ThenBy(x => x.Referencia)
            .ThenBy(x => x.Momento)
            .ToArray();

        return new MultiSignalTimelineResponse(
            evolucao.DiasObservados,
            ordenados,
            ordenados.Count(x => x.Dominio == "Carga"),
            ordenados.Count(x => x.Dominio == "Tempo"),
            ordenados.Select(x => $"{x.Dominio}::{x.Referencia}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            "Multi-Signal Timeline 2.0 exibe somente pontos observados de início e atual. Não cria pontos intermediários, não interpola dados, não projeta tendência e não transforma proximidade temporal em relação causal.");
    }

    public static ProgressSignalContextSummaryResponse MontarContextoSinaisProgresso(
        ProgressIntelligenceFoundationResponse foundation,
        DateOnly hoje)
    {
        var hojeUtc = hoje.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var contextos = foundation.Sinais
            .Select(sinal =>
            {
                var dataInicial = sinal.DataInicialUtc ?? sinal.DataAtualUtc;
                var dataAtual = sinal.DataAtualUtc ?? sinal.DataInicialUtc;

                var diasDesdeUltimo = dataAtual.HasValue
                    ? Math.Max(0, (int)(hojeUtc - dataAtual.Value.Date).TotalDays)
                    : foundation.DiasObservados;

                var diasCobertos = dataInicial.HasValue && dataAtual.HasValue
                    ? Math.Max(0, (int)(dataAtual.Value.Date - dataInicial.Value.Date).TotalDays)
                    : 0;

                var recencia = diasDesdeUltimo switch
                {
                    <= 14 => "Recente",
                    <= 45 => "Intermediaria",
                    _ => "Antiga"
                };

                var densidade = sinal.RegistrosComparaveis switch
                {
                    <= 2 => "BaseMinima",
                    <= 4 => "BaseCurta",
                    _ => "BaseMaisDensa"
                };

                var origem = sinal.Dominio switch
                {
                    "Carga" => "ExecucoesItensTreino",
                    "Tempo" => "ExecucoesTreino",
                    _ => "AthletePerformancePassport"
                };

                var contextoLeitura =
                    $"Sinal {recencia.ToLowerInvariant()} com {sinal.RegistrosComparaveis} registro(s) comparável(is) em {diasCobertos} dia(s) de cobertura. " +
                    "Recência e densidade descrevem disponibilidade de dados; não medem qualidade, capacidade, evolução clínica ou certeza.";

                return new ProgressSignalContextResponse(
                    sinal.Dominio,
                    sinal.Referencia,
                    recencia,
                    diasDesdeUltimo,
                    sinal.RegistrosComparaveis,
                    diasCobertos,
                    densidade,
                    origem,
                    contextoLeitura);
            })
            .OrderBy(x => x.Dominio)
            .ThenBy(x => x.Referencia)
            .ToArray();

        return new ProgressSignalContextSummaryResponse(
            foundation.DiasObservados,
            contextos,
            contextos.Count(x => x.Recencia == "Recente"),
            contextos.Count(x => x.Recencia == "Intermediaria"),
            contextos.Count(x => x.Recencia == "Antiga"),
            "Progress Signal Context 2.0 contextualiza recência, cobertura temporal, quantidade de registros e origem da evidência. Não produz score de confiança, ranking, diagnóstico, prognóstico ou recomendação automática.");
    }

    public static ProgressIntelligenceFoundationResponse MontarInteligenciaProgresso(
        AthletePerformanceEvolutionResponse evolucao,
        IReadOnlyCollection<AthleteCompetitionTestResultResponse> resultados,
        IReadOnlyCollection<AthleteSkillMilestoneResponse> habilidadesMarcos)
    {
        var sinais = evolucao.Pontos
            .Select(x =>
            {
                var direcao = x.VariacaoAbsoluta switch
                {
                    > 0m => "AcimaDoInicial",
                    < 0m => "AbaixoDoInicial",
                    _ => "EstavelNoPeriodo"
                };

                var evidencia = x.Dominio == "Carga"
                    ? "Comparação longitudinal da carga observada no mesmo exercício e unidade."
                    : "Comparação longitudinal da duração observada na mesma sessão.";

                var limite = x.Dominio == "Carga"
                    ? "A direção descreve somente a carga registrada. Não equivale a melhora de força máxima, técnica, segurança ou prognóstico."
                    : "A direção descreve somente duração. Menor ou maior tempo não é automaticamente melhora, piora, intensidade ou condicionamento.";

                return new ProgressIntelligenceSignalResponse(
                    x.Dominio,
                    x.Referencia,
                    x.Medida,
                    direcao,
                    x.VariacaoPercentual,
                    x.RegistrosComparaveis,
                    x.DataInicialUtc,
                    x.DataAtualUtc,
                    evidencia,
                    limite);
            })
            .OrderBy(x => x.Dominio)
            .ThenBy(x => x.Referencia)
            .ToArray();

        return new ProgressIntelligenceFoundationResponse(
            evolucao.DiasObservados,
            sinais,
            sinais.Count(x => x.Dominio == "Carga"),
            sinais.Count(x => x.Dominio == "Tempo"),
            resultados.Count + habilidadesMarcos.Count,
            "Progress Intelligence Foundation resume somente sinais descritivos baseados em comparações existentes. Não cria score, ranking, diagnóstico, prognóstico, recomendação automática ou julgamento clínico.");
    }

    public static async Task<AthletePerformanceEvolutionResponse> MontarEvolucaoAsync(
        AppDbContext db,
        Guid pacienteId,
        DateOnly dia,
        IReadOnlyCollection<AthletePerformanceRecordResponse> recordes,
        IReadOnlyCollection<AthleteTimedPerformanceResponse> tempos,
        IReadOnlyCollection<AthleteCompetitionTestResultResponse> resultados,
        IReadOnlyCollection<AthleteSkillMilestoneResponse> habilidadesMarcos,
        CancellationToken ct)
    {
        const int diasObservados = 180;
        var inicioUtc = dia.AddDays(-(diasObservados - 1)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var itens = await db.ExecucoesItensTreino.AsNoTracking()
            .Where(x =>
                x.ExecucaoTreino.PacienteId == pacienteId &&
                x.ExecucaoTreino.Status == "Concluido" &&
                x.ExecucaoTreino.DataHoraInicioUtc >= inicioUtc &&
                x.ExecucaoTreino.DataHoraInicioUtc < fimUtc &&
                x.Concluido &&
                x.CargaRealizada.HasValue &&
                x.CargaRealizada.Value > 0m)
            .Select(x => new
            {
                x.ItemTreino.ExercicioId,
                Exercicio = x.ItemTreino.Exercicio.Nome,
                Carga = x.CargaRealizada!.Value,
                Unidade = x.UnidadeCarga ?? x.ItemTreino.UnidadeCarga,
                x.ExecucaoTreino.DataHoraInicioUtc
            })
            .ToListAsync(ct);

        var pontos = new List<AthletePerformanceEvolutionPointResponse>();

        foreach (var grupo in itens.GroupBy(x => new
        {
            x.ExercicioId,
            x.Exercicio,
            Unidade = NormalizarUnidade(x.Unidade)
        }))
        {
            var ordenados = grupo.OrderBy(x => x.DataHoraInicioUtc).ToArray();
            if (ordenados.Length < 2)
                continue;

            var inicial = ordenados[0];
            var atual = ordenados[^1];
            var variacao = atual.Carga - inicial.Carga;
            decimal? percentual = inicial.Carga > 0m
                ? Math.Round(variacao / inicial.Carga * 100m, 1)
                : null;

            pontos.Add(new AthletePerformanceEvolutionPointResponse(
                "Carga",
                grupo.Key.Exercicio,
                "Carga observada",
                inicial.Carga,
                atual.Carga,
                grupo.Key.Unidade,
                Math.Round(variacao, 2),
                percentual,
                inicial.DataHoraInicioUtc,
                atual.DataHoraInicioUtc,
                ordenados.Length,
                "A variação descreve apenas a carga registrada no mesmo exercício e unidade. Não representa força máxima, qualidade técnica ou prognóstico."));
        }

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

        foreach (var grupo in sessoes.GroupBy(x => new { x.SessaoTreinoId, x.Sessao }))
        {
            var ordenados = grupo
                .Select(x => new
                {
                    x.DataHoraInicioUtc,
                    Duracao = ResolverDuracaoMinutos(x.DuracaoMinutos, x.DataHoraInicioUtc, x.DataHoraFimUtc)
                })
                .Where(x => x.Duracao.HasValue && x.Duracao.Value > 0)
                .OrderBy(x => x.DataHoraInicioUtc)
                .ToArray();

            if (ordenados.Length < 2)
                continue;

            var inicial = ordenados[0];
            var atual = ordenados[^1];
            var variacao = atual.Duracao!.Value - inicial.Duracao!.Value;
            decimal? percentual = inicial.Duracao.Value > 0
                ? Math.Round((decimal)variacao / inicial.Duracao.Value * 100m, 1)
                : null;

            pontos.Add(new AthletePerformanceEvolutionPointResponse(
                "Tempo",
                grupo.Key.Sessao,
                "Duração da sessão",
                inicial.Duracao.Value,
                atual.Duracao.Value,
                "min",
                variacao,
                percentual,
                inicial.DataHoraInicioUtc,
                atual.DataHoraInicioUtc,
                ordenados.Length,
                "A variação descreve duração da mesma sessão. Menor ou maior tempo não é classificado automaticamente como melhora, piora, intensidade ou condicionamento."));
        }

        return new AthletePerformanceEvolutionResponse(
            diasObservados,
            pontos
                .OrderBy(x => x.Dominio)
                .ThenBy(x => x.Referencia)
                .ToArray(),
            recordes.Count(x => x.Tipo == "MelhorCarga"),
            tempos.Count,
            resultados.Count,
            habilidadesMarcos.Count,
            "Performance Evolution 2.0 descreve início × atual somente em bases comparáveis. Não gera score, ranking, tendência clínica, prognóstico ou recomendação automática.");
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
