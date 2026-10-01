using HealthPlatform.Api.Contracts.WorkoutIntelligence;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class WorkoutIntelligenceService
{
    public static async Task<WorkoutIntelligenceResponse> MontarAsync(AppDbContext db, Guid organizacaoId, Guid pacienteId, int dias, CancellationToken ct)
    {
        dias = Math.Clamp(dias, 7, 180);
        var plano = await db.PlanosTreino.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Sessoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Exercicio)
            .Include(x => x.FasesTreino)
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == organizacaoId)
            .OrderByDescending(x => x.Status == "Ativo")
            .ThenByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var desde = DateTime.UtcNow.AddDays(-dias);
        var execucoes = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.Paciente)
            .Include(x => x.Itens)
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == organizacaoId && x.Status == "Concluido" && x.DataHoraInicioUtc >= desde)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .ToListAsync(ct);

        var itensPlano = plano?.Sessoes.SelectMany(x => x.Itens).ToList() ?? new List<ItemTreino>();
        var itensExecucao = execucoes.SelectMany(x => x.Itens).Where(x => x.Concluido).ToList();
        var dimensoes = new List<WorkoutIntelligenceDimensionResponse>
        {
            Dimensao("series", "Séries", "Prescrição + execução", itensPlano.Count(x => x.Series > 0), itensPlano.Count, itensExecucao.Count(x => x.SeriesRealizadas.HasValue), itensExecucao.Count, "Séries prescritas e realizadas já possuem campos estruturados."),
            Dimensao("repeticoes", "Repetições", "Prescrição + execução", itensPlano.Count(x => !string.IsNullOrWhiteSpace(x.Repeticoes)), itensPlano.Count, itensExecucao.Count(x => !string.IsNullOrWhiteSpace(x.RepeticoesRealizadas)), itensExecucao.Count, "Repetições preservam a faixa/texto prescrita e o realizado sem converter automaticamente em meta."),
            Dimensao("carga", "Carga", "Prescrição + execução", itensPlano.Count(x => x.Carga.HasValue), itensPlano.Count, itensExecucao.Count(x => x.CargaRealizada.HasValue), itensExecucao.Count, "Carga e unidade podem ser comparadas descritivamente quando registradas."),
            DimensaoPrescricao("descanso", "Descanso", itensPlano.Count(x => x.DescansoSegundos.HasValue), itensPlano.Count, "Descanso está estruturado na prescrição; a execução ainda não registra descanso realizado."),
            DimensaoPrescricao("tempo", "Tempo", itensPlano.Count(x => x.TempoSegundos.HasValue), itensPlano.Count, "Tempo por item está estruturado na prescrição e não equivale a cadência automaticamente."),
            DimensaoExecucao("rpe", "RPE", execucoes.Count(x => x.EsforcoPercebido.HasValue), execucoes.Count, "RPE de sessão e esforço percebido por item contextualizam a execução."),
            Dimensao("rir", "RIR", "Prescrição + execução", itensPlano.Count(x => x.RirAlvo.HasValue), itensPlano.Count, itensExecucao.Count(x => x.RirRealizado.HasValue), itensExecucao.Count, "RIR-alvo e RIR realizado possuem campos estruturados próprios."),
            Dimensao("cadencia", "Cadência", "Prescrição + execução", itensPlano.Count(x => !string.IsNullOrWhiteSpace(x.Cadencia)), itensPlano.Count, itensExecucao.Count(x => !string.IsNullOrWhiteSpace(x.CadenciaRealizada)), itensExecucao.Count, "Cadência prescrita e realizada são preservadas como texto estruturado, sem inferência automática."),
            Dimensao("tecnicas", "Técnicas avançadas", "Prescrição + execução", itensPlano.Count(x => !string.IsNullOrWhiteSpace(x.TecnicaAvancadaCodigo) || !string.IsNullOrWhiteSpace(x.TecnicaAvancada)), itensPlano.Count, itensExecucao.Count(x => !string.IsNullOrWhiteSpace(x.TecnicaExecutadaCodigo) || !string.IsNullOrWhiteSpace(x.TecnicaExecutada)), itensExecucao.Count, "Advanced Techniques 3.0 preserva código estruturado, parâmetros e texto legado para compatibilidade."),
            DimensaoPeriodizacao(plano)
        };

        var execucaoItens = execucoes
            .SelectMany(e => e.Itens.Where(i => i.Concluido).Select(i => new { Execucao = e, Item = i }))
            .ToList();
        var comparacoes = new List<WorkoutIntelligenceComparisonResponse>();
        var sinaisProgressaoRegressao = new List<WorkoutProgressionRegressionSignalResponse>();
        if (plano is not null)
        {
            foreach (var sessao in plano.Sessoes.OrderBy(x => x.Ordem))
            {
                foreach (var item in sessao.Itens.OrderBy(x => x.Ordem))
                {
                    var historico = execucaoItens
                        .Where(x => x.Item.ItemTreinoId == item.Id)
                        .OrderByDescending(x => x.Execucao.DataHoraInicioUtc)
                        .ToList();
                    var ultimo = historico.FirstOrDefault();
                    var diferencas = ultimo is null ? new List<string>() : Comparar(item, ultimo.Item);
                    var estado = ultimo is null ? "SemExecucaoNoPeriodo" : diferencas.Count == 0 ? "SemDiferencaRegistrada" : "DiferencasRegistradas";
                    comparacoes.Add(new(
                        item.Id, sessao.Nome, item.Exercicio?.Nome ?? "Exercício", historico.Count, ultimo?.Execucao.DataHoraInicioUtc, estado,
                        item.Series, ultimo?.Item.SeriesRealizadas, item.Repeticoes, ultimo?.Item.RepeticoesRealizadas,
                        item.Carga, item.UnidadeCarga, ultimo?.Item.CargaRealizada, ultimo?.Item.UnidadeCarga,
                        item.RirAlvo, ultimo?.Item.RirRealizado, item.Cadencia, ultimo?.Item.CadenciaRealizada,
                        item.TecnicaAvancada, ultimo?.Item.TecnicaExecutada, item.TecnicaAvancadaCodigo, ultimo?.Item.TecnicaExecutadaCodigo,
                        item.TecnicaAvancadaParametros, ultimo?.Item.TecnicaExecutadaParametros, diferencas));
                    var recentes = historico.Take(3).ToList();
                    var evidenciasProgressao = new List<string>();
                    var evidenciasRegressao = new List<string>();
                    if (recentes.Count >= 2)
                    {
                        var ultimasDuas = recentes.Take(2).Select(x => x.Item).ToList();
                        if (item.RirAlvo.HasValue && ultimasDuas.All(x => x.RirRealizado.HasValue && x.RirRealizado.Value >= item.RirAlvo.Value + 2 && (!x.SeriesRealizadas.HasValue || x.SeriesRealizadas.Value >= item.Series)))
                            evidenciasProgressao.Add($"RIR ficou pelo menos 2 repetições acima do alvo nas últimas {ultimasDuas.Count} execuções comparáveis.");
                        if (item.RirAlvo.HasValue && ultimasDuas.All(x => x.RirRealizado.HasValue && x.RirRealizado.Value <= item.RirAlvo.Value - 2))
                            evidenciasRegressao.Add($"RIR ficou pelo menos 2 repetições abaixo do alvo nas últimas {ultimasDuas.Count} execuções comparáveis.");
                        if (item.Carga.HasValue && ultimasDuas.All(x => x.CargaRealizada.HasValue && x.CargaRealizada.Value > item.Carga.Value))
                            evidenciasProgressao.Add("Carga realizada ficou acima da prescrita em duas execuções consecutivas.");
                        if (item.Carga.HasValue && ultimasDuas.All(x => x.CargaRealizada.HasValue && x.CargaRealizada.Value < item.Carga.Value))
                            evidenciasRegressao.Add("Carga realizada ficou abaixo da prescrita em duas execuções consecutivas.");
                        if (item.Series > 0 && ultimasDuas.All(x => x.SeriesRealizadas.HasValue && x.SeriesRealizadas.Value > item.Series))
                            evidenciasProgressao.Add("Séries realizadas ficaram acima da prescrição em duas execuções consecutivas.");
                        if (item.Series > 0 && ultimasDuas.All(x => x.SeriesRealizadas.HasValue && x.SeriesRealizadas.Value < item.Series))
                            evidenciasRegressao.Add("Séries realizadas ficaram abaixo da prescrição em duas execuções consecutivas.");
                    }
                    var estadoSinal = recentes.Count < 2 ? "HistoricoInsuficiente" : evidenciasProgressao.Count > 0 && evidenciasRegressao.Count > 0 ? "SinaisMistos" : evidenciasProgressao.Count > 0 ? "RevisarProgressao" : evidenciasRegressao.Count > 0 ? "RevisarRegressao" : "SemSinalConsistente";
                    var direcaoSinal = estadoSinal == "RevisarProgressao" ? "Progressao" : estadoSinal == "RevisarRegressao" ? "Regressao" : "Neutro";
                    var evidenciasSinal = evidenciasProgressao.Concat(evidenciasRegressao).ToArray();
                    var sugestaoSinal = estadoSinal switch
                    {
                        "RevisarProgressao" => "Revisar se a prescrição já comporta progressão; nenhum ajuste foi aplicado.",
                        "RevisarRegressao" => "Revisar se a exigência prescrita deve ser mantida ou regredida; nenhum ajuste foi aplicado.",
                        "SinaisMistos" => "Revisar os sinais mistos antes de qualquer alteração na prescrição.",
                        "HistoricoInsuficiente" => "Aguardar mais execuções comparáveis antes de revisar progressão ou regressão.",
                        _ => "Manter observação; não há sinal repetido suficiente para sugerir revisão de progressão/regressão."
                    };
                    sinaisProgressaoRegressao.Add(new(item.Id, sessao.Nome, item.Exercicio?.Nome ?? "Exercício", estadoSinal, direcaoSinal, recentes.Count, evidenciasSinal, sugestaoSinal, "Sinal explicável para revisão profissional; nunca altera a prescrição automaticamente."));
                }
            }
        }

        var periodizacao = MontarPeriodizacao(plano);

        var rpes = execucoes.Where(x => x.EsforcoPercebido.HasValue).Select(x => (decimal)x.EsforcoPercebido!.Value).ToArray();
        var resumo = new WorkoutIntelligenceSummaryResponse(
            itensPlano.Sum(x => Math.Max(0, x.Series)),
            itensExecucao.Sum(x => Math.Max(0, x.SeriesRealizadas ?? 0)),
            rpes.Length == 0 ? null : Math.Round(rpes.Average(), 1),
            itensPlano.Count(x => x.Carga.HasValue), itensExecucao.Count(x => x.CargaRealizada.HasValue),
            itensPlano.Count(x => !string.IsNullOrWhiteSpace(x.Repeticoes)), itensExecucao.Count(x => !string.IsNullOrWhiteSpace(x.RepeticoesRealizadas)),
            comparacoes.Count(x => x.ExecucoesNoPeriodo > 0), comparacoes.Count(x => x.Estado == "DiferencasRegistradas"));

        return new WorkoutIntelligenceResponse(
            "v0.48.6", dias, plano?.Id, plano?.Nome, plano?.Status, plano?.Sessoes.Count ?? 0, itensPlano.Count, execucoes.Count, itensExecucao.Count,
            resumo, dimensoes, comparacoes, sinaisProgressaoRegressao, periodizacao,
            new[] { "Athlete Performance Passport v0.28.0 entregue; próxima camada: resultados estruturados de tempos, provas, testes, habilidades e marcos." },
            "Periodization 3.0 organiza microciclo, mesociclo, bloco e deload a partir do planejamento já registrado. Não cria periodização, deload ou alteração de prescrição automaticamente.");
    }


    private static WorkoutPeriodizationResponse MontarPeriodizacao(PlanoTreino? plano)
    {
        if (plano is null)
            return new("SemPlano", "Sem plano ativo", "Sem fase ativa", "Sem bloco ativo", null, null, null, null, null, null, false, Array.Empty<WorkoutPeriodizationPhaseResponse>(), "Periodização depende do planejamento registrado pelo profissional; o AESYN não cria ciclos automaticamente.");

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var fases = plano.FasesTreino.OrderBy(x => x.Ordem).ThenBy(x => x.DataInicio).ToList();
        var faseAtual = fases.FirstOrDefault(x => x.Status.Equals("EmCurso", StringComparison.OrdinalIgnoreCase) || x.Status.Equals("Ativa", StringComparison.OrdinalIgnoreCase))
            ?? fases.FirstOrDefault(x => x.DataInicio <= hoje && (!x.DataFim.HasValue || x.DataFim.Value >= hoje));
        var semanaPlano = SemanaDesde(plano.DataInicio, hoje);
        int? totalSemanasPlano = plano.DataFim.HasValue ? SemanasEntre(plano.DataInicio, plano.DataFim.Value) : null;
        var deloadPlanejado = fases.Any(EhDeload);
        var fasesResponse = fases.Select(x => new WorkoutPeriodizationPhaseResponse(
            x.Id, x.Nome, x.Tipo, x.Ordem, x.Status, x.DataInicio, x.DataFim,
            x.DataFim.HasValue ? SemanasEntre(x.DataInicio, x.DataFim.Value) : Math.Max(1, x.DuracaoMinimaDias.HasValue ? (int)Math.Ceiling(x.DuracaoMinimaDias.Value / 7m) : 1),
            x == faseAtual ? SemanaDesde(x.DataInicio, hoje) : null, EhDeload(x), x.Objetivo, x.CriterioTransicao)).ToArray();
        var faseAtualResponse = fasesResponse.FirstOrDefault(x => faseAtual is not null && x.Id == faseAtual.Id);
        var estado = fases.Count == 0 ? "SemFasesConfiguradas" : faseAtual is not null ? "EmCurso" : "PlanejadoSemFaseAtual";
        return new(
            estado,
            semanaPlano.HasValue ? $"Semana {semanaPlano.Value} do plano" : "Microciclo sem referência temporal",
            faseAtual?.Nome ?? "Sem fase ativa",
            plano.Nome,
            semanaPlano, totalSemanasPlano, faseAtual?.Nome, faseAtual?.Tipo, faseAtualResponse?.SemanaAtual, faseAtualResponse?.DuracaoSemanas,
            deloadPlanejado, fasesResponse,
            "Microciclo, mesociclo, bloco e deload refletem apenas planejamento profissional já registrado; nenhuma transição ou deload é aplicado automaticamente.");
    }

    private static WorkoutIntelligenceDimensionResponse DimensaoPeriodizacao(PlanoTreino? plano)
    {
        var fases = plano?.FasesTreino?.Count ?? 0;
        var estado = plano is null ? "SemDados" : fases == 0 ? "SemFaseConfigurada" : "Estruturado";
        return new("periodizacao", "Microciclo / mesociclo / bloco", estado, "Plano + fases de treino", fases, Math.Max(1, fases),
            fases > 0 ? "Periodization 3.0 usa datas, ordem, tipo, status e critérios de transição das fases já registradas." : "Plano existente, mas ainda sem fases de periodização configuradas.");
    }

    private static bool EhDeload(FaseTreino fase) =>
        fase.Tipo.Contains("deload", StringComparison.OrdinalIgnoreCase) || fase.Nome.Contains("deload", StringComparison.OrdinalIgnoreCase);

    private static int? SemanaDesde(DateOnly inicio, DateOnly referencia)
    {
        if (referencia < inicio) return null;
        return Math.Max(1, ((referencia.DayNumber - inicio.DayNumber) / 7) + 1);
    }

    private static int SemanasEntre(DateOnly inicio, DateOnly fim) => Math.Max(1, (int)Math.Ceiling((fim.DayNumber - inicio.DayNumber + 1) / 7m));

    private static List<string> Comparar(ItemTreino prescrito, ExecucaoItemTreino realizado)
    {
        var diferencas = new List<string>();
        if (realizado.SeriesRealizadas.HasValue && realizado.SeriesRealizadas.Value != prescrito.Series) diferencas.Add("Séries");
        if (!string.IsNullOrWhiteSpace(realizado.RepeticoesRealizadas) && !TextoIgual(realizado.RepeticoesRealizadas, prescrito.Repeticoes)) diferencas.Add("Repetições");
        if (prescrito.Carga.HasValue && realizado.CargaRealizada.HasValue && prescrito.Carga.Value != realizado.CargaRealizada.Value) diferencas.Add("Carga");
        if (prescrito.RirAlvo.HasValue && realizado.RirRealizado.HasValue && prescrito.RirAlvo.Value != realizado.RirRealizado.Value) diferencas.Add("RIR");
        if (!string.IsNullOrWhiteSpace(prescrito.Cadencia) && !string.IsNullOrWhiteSpace(realizado.CadenciaRealizada) && !TextoIgual(prescrito.Cadencia, realizado.CadenciaRealizada)) diferencas.Add("Cadência");
        if (!string.IsNullOrWhiteSpace(prescrito.TecnicaAvancadaCodigo) && !string.IsNullOrWhiteSpace(realizado.TecnicaExecutadaCodigo) && !TextoIgual(prescrito.TecnicaAvancadaCodigo, realizado.TecnicaExecutadaCodigo)) diferencas.Add("Técnica");
        else if (string.IsNullOrWhiteSpace(prescrito.TecnicaAvancadaCodigo) && string.IsNullOrWhiteSpace(realizado.TecnicaExecutadaCodigo) && !string.IsNullOrWhiteSpace(prescrito.TecnicaAvancada) && !string.IsNullOrWhiteSpace(realizado.TecnicaExecutada) && !TextoIgual(prescrito.TecnicaAvancada, realizado.TecnicaExecutada)) diferencas.Add("Técnica");
        if (!string.IsNullOrWhiteSpace(prescrito.TecnicaAvancadaParametros) && !string.IsNullOrWhiteSpace(realizado.TecnicaExecutadaParametros) && !TextoIgual(prescrito.TecnicaAvancadaParametros, realizado.TecnicaExecutadaParametros)) diferencas.Add("Parâmetros da técnica");
        return diferencas;
    }

    private static bool TextoIgual(string? a, string? b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static WorkoutIntelligenceDimensionResponse Dimensao(string codigo,string nome,string origem,int planejadosComDado,int totalPlanejados,int executadosComDado,int totalExecutados,string observacao)
    {
        var total = totalPlanejados + totalExecutados; var comDado = planejadosComDado + executadosComDado;
        var estado = total == 0 ? "SemDados" : comDado == total ? "Estruturado" : comDado > 0 ? "Parcial" : "SemRegistro";
        return new(codigo,nome,estado,origem,comDado,total,observacao);
    }
    private static WorkoutIntelligenceDimensionResponse DimensaoPrescricao(string codigo,string nome,int comDado,int total,string observacao)
    {
        var estado = total == 0 ? "SemDados" : comDado == total ? "EstruturadoNaPrescricao" : comDado > 0 ? "ParcialNaPrescricao" : "SemRegistro";
        return new(codigo,nome,estado,"Prescrição",comDado,total,observacao);
    }
    private static WorkoutIntelligenceDimensionResponse DimensaoExecucao(string codigo,string nome,int comDado,int total,string observacao)
    {
        var estado = total == 0 ? "SemDados" : comDado == total ? "EstruturadoNaExecucao" : comDado > 0 ? "ParcialNaExecucao" : "SemRegistro";
        return new(codigo,nome,estado,"Execução",comDado,total,observacao);
    }
    private static WorkoutIntelligenceDimensionResponse NaoEstruturada(string codigo,string nome,string observacao) => new(codigo,nome,"AindaNaoEstruturado","Roadmap v0.27.x",0,0,observacao);
}
