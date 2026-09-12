using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class BlocoTreinamentoService
{
    public static async Task<PortalBlocoTreinamentoResponse> MontarAsync(
        AppDbContext db, Guid organizacaoId, Guid pacienteId, DateOnly dia,
        PortalCicloEsportivoResponse? ciclo,
        PortalCargaIndividualizadaResponse cargaIndividualizada,
        PortalRespostaSessaoResponse respostaSessao,
        CancellationToken ct)
    {
        const string seguranca = "Bloco/mesociclo e uma estrutura configurada pelo profissional: o sistema nao cria periodizacao automaticamente, nao define carga ideal e nao aumenta volume automaticamente. Deload ou recuperacao planejada devem ser intencao explicita do profissional.";

        if (ciclo is null)
            return new("SemCiclo", null, null, null, null, null, null, null, null, null, null, null, null, 0, 0, null, null, null, null,
                cargaIndividualizada.PosicaoHistorica, respostaSessao.Estado,
                "Nao existe ciclo esportivo ativo para contextualizar um bloco de treinamento.", Array.Empty<string>(), seguranca);

        // bloco configurado pelo profissional: o bloco de treinamento reutiliza a FaseTreino vinculada ao ciclo. Isso preserva a intencao profissional ja registrada
        // e evita que o sistema invente automaticamente uma periodizacao/mesociclo.
        var cicloEntity = await db.CiclosEsportivosPaciente.AsNoTracking()
            .Include(x => x.FaseTreino)
            .FirstOrDefaultAsync(x => x.Id == ciclo.Id && x.PacienteId == pacienteId && x.OrganizacaoId == organizacaoId, ct);

        var fase = cicloEntity?.FaseTreino;
        if (fase is null)
            return new("SemBlocoConfigurado", null, null, null, null, null, null, null, null, null, null, null, null, 0, 0, null, null, null, null,
                cargaIndividualizada.PosicaoHistorica, respostaSessao.Estado,
                "O ciclo esta ativo, mas ainda nao possui um bloco/fase de treino configurado pelo profissional.",
                new[] { "Sem bloco configurado: a plataforma nao cria mesociclo automaticamente." }, seguranca);

        var fim = fase.DataFim ?? ciclo.DataFim;
        if (fim < fase.DataInicio) fim = fase.DataInicio;
        var totalDias = Math.Max(1, fim.DayNumber - fase.DataInicio.DayNumber + 1);
        var totalSemanas = Math.Max(1, (int)Math.Ceiling(totalDias / 7m));
        var diasDecorridos = Math.Clamp(dia.DayNumber - fase.DataInicio.DayNumber + 1, 0, totalDias);
        var semanaAtual = dia < fase.DataInicio ? 0 : Math.Min(totalSemanas, Math.Max(1, (int)Math.Ceiling(Math.Max(1, diasDecorridos) / 7m)));
        var progresso = dia < fase.DataInicio ? 0m : Math.Round(diasDecorridos / (decimal)totalDias * 100m, 1);

        var inicioUtc = DateTime.SpecifyKind(fase.DataInicio.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimObservado = dia < fim ? dia : fim;
        var fimUtc = DateTime.SpecifyKind(fimObservado.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var execQuery = db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" && x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc);
        if (fase.PlanoTreinoId.HasValue)
            execQuery = execQuery.Where(x => x.PlanoTreinoId == fase.PlanoTreinoId.Value);

        var execucoes = await execQuery
            .Select(x => new { x.DuracaoMinutos, x.EsforcoPercebido })
            .ToListAsync(ct);
        var duracao = execucoes.Sum(x => x.DuracaoMinutos ?? 0);
        var rpes = execucoes.Where(x => x.EsforcoPercebido.HasValue).Select(x => x.EsforcoPercebido!.Value).ToList();
        decimal? rpeMedio = rpes.Count > 0 ? Math.Round((decimal)rpes.Average(), 1) : null;
        var cargas = execucoes.Where(x => x.DuracaoMinutos is > 0 && x.EsforcoPercebido is >= 1 and <= 10)
            .Select(x => (decimal)(x.DuracaoMinutos!.Value * x.EsforcoPercebido!.Value)).ToList();
        decimal? carga = cargas.Count > 0 ? Math.Round(cargas.Sum(), 1) : null;

        var estado = dia < fase.DataInicio ? "Planejado" : dia > fim ? "Encerrado" : "EmCurso";
        var sinais = new List<string>
        {
            $"Bloco configurado pelo profissional: {fase.Nome} ({fase.Tipo}).",
            $"Semana {Math.Max(0, semanaAtual)}/{totalSemanas}; {execucoes.Count} sessao(oes) concluida(s) no periodo observado."
        };
        if (!string.IsNullOrWhiteSpace(fase.Objetivo)) sinais.Add($"Objetivo registrado: {fase.Objetivo}");
        if (!string.IsNullOrWhiteSpace(fase.CriterioTransicao)) sinais.Add($"Criterio de transicao registrado: {fase.CriterioTransicao}");
        if (fase.DuracaoMinimaDias.HasValue) sinais.Add($"Duracao minima configurada: {fase.DuracaoMinimaDias.Value} dia(s).");
        sinais.Add($"Carga recente no historico individual: {cargaIndividualizada.PosicaoHistorica}.");
        sinais.Add($"Resposta recente a sessao: {respostaSessao.Estado}.");

        var resumo = estado switch
        {
            "Planejado" => "O bloco esta configurado e ainda nao iniciou. A plataforma apenas organiza a intencao profissional registrada.",
            "Encerrado" => "O periodo do bloco terminou. Os dados executados permanecem como contexto para revisao profissional.",
            _ => "Bloco em andamento com referencia temporal, execucao observada e contexto individual de carga/resposta."
        };

        return new(estado, fase.Id, fase.Nome, fase.Tipo, fase.Objetivo, fase.DataInicio, fim,
            semanaAtual, totalSemanas, progresso, fase.Ordem, fase.Status, fase.PlanoTreinoId,
            execucoes.Count, duracao, rpeMedio, carga, fase.CriterioTransicao, fase.DuracaoMinimaDias,
            cargaIndividualizada.PosicaoHistorica, respostaSessao.Estado, resumo, sinais, seguranca);
    }
}
