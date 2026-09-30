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
            Dimensao("tecnicas", "Técnicas avançadas", "Prescrição + execução", itensPlano.Count(x => !string.IsNullOrWhiteSpace(x.TecnicaAvancada)), itensPlano.Count, itensExecucao.Count(x => !string.IsNullOrWhiteSpace(x.TecnicaExecutada)), itensExecucao.Count, "Técnica avançada prescrita e técnica executada ficam explícitas para comparação descritiva."),
            NaoEstruturada("periodizacao", "Microciclo / mesociclo / bloco", "Fases e programas existem, mas a periodização 3.0 ainda será consolidada como camada própria.")
        };

        var execucaoItens = execucoes
            .SelectMany(e => e.Itens.Where(i => i.Concluido).Select(i => new { Execucao = e, Item = i }))
            .ToList();
        var comparacoes = new List<WorkoutIntelligenceComparisonResponse>();
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
                        item.TecnicaAvancada, ultimo?.Item.TecnicaExecutada, diferencas));
                }
            }
        }

        var rpes = execucoes.Where(x => x.EsforcoPercebido.HasValue).Select(x => (decimal)x.EsforcoPercebido!.Value).ToArray();
        var resumo = new WorkoutIntelligenceSummaryResponse(
            itensPlano.Sum(x => Math.Max(0, x.Series)),
            itensExecucao.Sum(x => Math.Max(0, x.SeriesRealizadas ?? 0)),
            rpes.Length == 0 ? null : Math.Round(rpes.Average(), 1),
            itensPlano.Count(x => x.Carga.HasValue), itensExecucao.Count(x => x.CargaRealizada.HasValue),
            itensPlano.Count(x => !string.IsNullOrWhiteSpace(x.Repeticoes)), itensExecucao.Count(x => !string.IsNullOrWhiteSpace(x.RepeticoesRealizadas)),
            comparacoes.Count(x => x.ExecucoesNoPeriodo > 0), comparacoes.Count(x => x.Estado == "DiferencasRegistradas"));

        return new WorkoutIntelligenceResponse(
            "v0.27.2", dias, plano?.Id, plano?.Nome, plano?.Status, plano?.Sessoes.Count ?? 0, itensPlano.Count, execucoes.Count, itensExecucao.Count,
            resumo, dimensoes, comparacoes,
            new[] { "Técnicas avançadas 3.0 como contrato reutilizável.", "Progressão/regressão explicável, sempre revisada pelo profissional.", "Microciclo, mesociclo, bloco e deload sobre histórico preservado." },
            "Prescribed vs Performed 3.0 compara somente dados registrados. Diferença não significa erro, baixa adesão ou necessidade de ajuste; o AESYN não altera automaticamente carga, volume, RIR, cadência, técnica ou prescrição.");
    }

    private static List<string> Comparar(ItemTreino prescrito, ExecucaoItemTreino realizado)
    {
        var diferencas = new List<string>();
        if (realizado.SeriesRealizadas.HasValue && realizado.SeriesRealizadas.Value != prescrito.Series) diferencas.Add("Séries");
        if (!string.IsNullOrWhiteSpace(realizado.RepeticoesRealizadas) && !TextoIgual(realizado.RepeticoesRealizadas, prescrito.Repeticoes)) diferencas.Add("Repetições");
        if (prescrito.Carga.HasValue && realizado.CargaRealizada.HasValue && prescrito.Carga.Value != realizado.CargaRealizada.Value) diferencas.Add("Carga");
        if (prescrito.RirAlvo.HasValue && realizado.RirRealizado.HasValue && prescrito.RirAlvo.Value != realizado.RirRealizado.Value) diferencas.Add("RIR");
        if (!string.IsNullOrWhiteSpace(prescrito.Cadencia) && !string.IsNullOrWhiteSpace(realizado.CadenciaRealizada) && !TextoIgual(prescrito.Cadencia, realizado.CadenciaRealizada)) diferencas.Add("Cadência");
        if (!string.IsNullOrWhiteSpace(prescrito.TecnicaAvancada) && !string.IsNullOrWhiteSpace(realizado.TecnicaExecutada) && !TextoIgual(prescrito.TecnicaAvancada, realizado.TecnicaExecutada)) diferencas.Add("Técnica");
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
