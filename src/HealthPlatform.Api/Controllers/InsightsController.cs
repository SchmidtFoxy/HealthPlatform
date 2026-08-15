using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record InsightClinicoResponse(
    string Codigo,
    string Categoria,
    string Severidade,
    string Titulo,
    string Descricao,
    DateTime? DataReferenciaUtc,
    string? Valor,
    string? AcaoSugerida);

public sealed record PacienteInsightsResponse(
    Guid PacienteId,
    string PacienteNome,
    DateTime GeradoEmUtc,
    int Total,
    int Alta,
    int Media,
    int Baixa,
    IReadOnlyCollection<InsightClinicoResponse> Insights);

public sealed record DashboardInsightPacienteResponse(
    Guid PacienteId,
    string PacienteNome,
    string SeveridadeMaxima,
    int Total,
    IReadOnlyCollection<InsightClinicoResponse> Insights);

public sealed record DashboardInsightsResponse(
    DateTime GeradoEmUtc,
    int PacientesAnalisados,
    int PacientesComInsights,
    int TotalInsights,
    int Alta,
    int Media,
    int Baixa,
    IReadOnlyCollection<DashboardInsightPacienteResponse> Pacientes);

[ApiController]
[Authorize]
public sealed class InsightsController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/insights")]
    public async Task<ActionResult<PacienteInsightsResponse>> Paciente(
        Guid pacienteId,
        CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente nao encontrado ou inativo." });

        var insights = await CalcularPaciente(pacienteId, ct);
        return Ok(ToPacienteResponse(paciente.Id, paciente.Nome, insights));
    }

    [HttpGet("api/insights/dashboard")]
    public async Task<ActionResult<DashboardInsightsResponse>> Dashboard(
        [FromQuery] int limite = 12,
        CancellationToken ct = default)
    {
        limite = Math.Clamp(limite, 1, 50);

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome })
            .ToListAsync(ct);

        var calculados = new List<DashboardInsightPacienteResponse>();
        var totalInsights = 0;
        var totalAlta = 0;
        var totalMedia = 0;
        var totalBaixa = 0;

        foreach (var paciente in pacientes)
        {
            var insights = await CalcularPaciente(paciente.Id, ct);
            if (insights.Count == 0) continue;

            totalInsights += insights.Count;
            totalAlta += insights.Count(x => x.Severidade == "Alta");
            totalMedia += insights.Count(x => x.Severidade == "Media");
            totalBaixa += insights.Count(x => x.Severidade == "Baixa");

            calculados.Add(new DashboardInsightPacienteResponse(
                paciente.Id,
                paciente.Nome,
                SeveridadeMaxima(insights),
                insights.Count,
                insights
                    .OrderByDescending(x => PesoSeveridade(x.Severidade))
                    .ThenByDescending(x => x.DataReferenciaUtc)
                    .Take(4)
                    .ToList()));
        }

        var ordenados = calculados
            .OrderByDescending(x => PesoSeveridade(x.SeveridadeMaxima))
            .ThenByDescending(x => x.Total)
            .ThenBy(x => x.PacienteNome)
            .Take(limite)
            .ToList();

        return Ok(new DashboardInsightsResponse(
            DateTime.UtcNow,
            pacientes.Count,
            calculados.Count,
            totalInsights,
            totalAlta,
            totalMedia,
            totalBaixa,
            ordenados));
    }

    private async Task<List<InsightClinicoResponse>> CalcularPaciente(
        Guid pacienteId,
        CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var hoje = DateOnly.FromDateTime(agora);
        var insights = new List<InsightClinicoResponse>();

        await AvaliarExames(pacienteId, insights, ct);
        await AvaliarEvolucaoCorporal(pacienteId, insights, ct);
        await AvaliarRetorno(pacienteId, insights, agora, ct);
        await AvaliarMetas(pacienteId, insights, hoje, ct);
        await AvaliarTreinos(pacienteId, insights, agora, ct);

        return insights
            .OrderByDescending(x => PesoSeveridade(x.Severidade))
            .ThenByDescending(x => x.DataReferenciaUtc)
            .ToList();
    }

    private async Task AvaliarExames(
        Guid pacienteId,
        List<InsightClinicoResponse> insights,
        CancellationToken ct)
    {
        var ultimaColeta = await db.ExamesLaboratoriais.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataColetaUtc)
            .Select(x => new { x.Id, x.DataColetaUtc })
            .FirstOrDefaultAsync(ct);

        if (ultimaColeta is null) return;

        var resultados = await db.ResultadosExamesLaboratoriais.AsNoTracking()
            .Where(x => x.ExameLaboratorialId == ultimaColeta.Id)
            .Select(x => new
            {
                Marcador = x.MarcadorLaboratorial.Nome,
                x.ValorNumerico,
                x.Unidade,
                x.ReferenciaMinima,
                x.ReferenciaMaxima
            })
            .ToListAsync(ct);

        foreach (var r in resultados)
        {
            if (!r.ValorNumerico.HasValue) continue;

            var baixo = r.ReferenciaMinima.HasValue &&
                        r.ValorNumerico.Value < r.ReferenciaMinima.Value;
            var alto = r.ReferenciaMaxima.HasValue &&
                       r.ValorNumerico.Value > r.ReferenciaMaxima.Value;

            if (!baixo && !alto) continue;

            var direcao = baixo ? "abaixo" : "acima";
            insights.Add(new InsightClinicoResponse(
                "EXAME_FORA_REFERENCIA",
                "Exames",
                "Alta",
                $"{r.Marcador} fora da referência registrada",
                $"O resultado mais recente está {direcao} da faixa de referência informada pelo laboratório. Este sinal não representa diagnóstico.",
                ultimaColeta.DataColetaUtc,
                $"{r.ValorNumerico:0.##} {r.Unidade}".Trim(),
                "Revisar o resultado no contexto clínico e definir se há necessidade de acompanhamento."));
        }
    }

    private async Task AvaliarEvolucaoCorporal(
        Guid pacienteId,
        List<InsightClinicoResponse> insights,
        CancellationToken ct)
    {
        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.PesoKg.HasValue)
            .OrderByDescending(x => x.DataUtc)
            .Take(2)
            .Select(x => new { x.DataUtc, Peso = x.PesoKg!.Value })
            .ToListAsync(ct);

        if (avaliacoes.Count < 2 || avaliacoes[1].Peso <= 0) return;

        var atual = avaliacoes[0];
        var anterior = avaliacoes[1];
        var deltaKg = atual.Peso - anterior.Peso;
        var deltaPct = deltaKg / anterior.Peso * 100m;
        var absPct = Math.Abs(deltaPct);

        if (absPct < 3m) return;

        var severidade = absPct >= 7m ? "Alta" : "Media";
        var direcao = deltaKg > 0 ? "aumento" : "redução";

        insights.Add(new InsightClinicoResponse(
            "VARIACAO_PESO",
            "Evolução",
            severidade,
            $"Variação de peso de {Math.Abs(deltaPct):0.#}%",
            $"Foi observado {direcao} de {Math.Abs(deltaKg):0.##} kg entre as duas avaliações mais recentes. A relevância depende do objetivo e do contexto do paciente.",
            atual.DataUtc,
            $"{deltaKg:+0.##;-0.##;0} kg",
            "Revisar objetivo, intervalo entre avaliações e contexto clínico antes de interpretar a mudança."));
    }

    private async Task AvaliarRetorno(
        Guid pacienteId,
        List<InsightClinicoResponse> insights,
        DateTime agora,
        CancellationToken ct)
    {
        var ultima = await db.Consultas.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Status == StatusConsulta.Realizada)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => (DateTime?)x.DataHoraUtc)
            .FirstOrDefaultAsync(ct);

        if (!ultima.HasValue) return;

        var dias = (int)Math.Floor((agora - ultima.Value).TotalDays);
        if (dias < 60) return;

        var futura = await db.Consultas.AsNoTracking().AnyAsync(x =>
            x.PacienteId == pacienteId &&
            x.DataHoraUtc > agora &&
            x.Status != StatusConsulta.Cancelada &&
            x.Status != StatusConsulta.Faltou, ct);

        if (futura) return;

        insights.Add(new InsightClinicoResponse(
            "SEM_RETORNO",
            "Agenda",
            dias >= 90 ? "Alta" : "Media",
            $"Sem retorno agendado há {dias} dias",
            "A última consulta realizada já ocorreu há um período relevante e não há nova consulta futura registrada.",
            ultima.Value,
            $"{dias} dias",
            "Avaliar se o acompanhamento prevê retorno e, se fizer sentido, entrar em contato para reagendamento."));
    }

    private async Task AvaliarMetas(
        Guid pacienteId,
        List<InsightClinicoResponse> insights,
        DateOnly hoje,
        CancellationToken ct)
    {
        var inicio = hoje.AddDays(-13);

        var metas = await db.MetasPaciente.AsNoTracking()
            .Include(x => x.Registros.Where(r => r.Data >= inicio && r.Data <= hoje))
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Status == "Ativa" &&
                x.DataInicio <= hoje &&
                (!x.DataFim.HasValue || x.DataFim.Value >= inicio))
            .ToListAsync(ct);

        foreach (var meta in metas)
        {
            var inicioEfetivo = meta.DataInicio > inicio ? meta.DataInicio : inicio;
            var diasAtivos = Math.Max(1, hoje.DayNumber - inicioEfetivo.DayNumber + 1);

            var esperado = meta.Frequencia switch
            {
                "Diaria" => diasAtivos,
                "Semanal" => Math.Max(1, (int)Math.Ceiling(diasAtivos / 7m)),
                "Mensal" => 1,
                _ => diasAtivos
            };

            var registros = meta.Registros.Count;
            var cobertura = esperado == 0
                ? 100m
                : Math.Clamp((decimal)registros / esperado * 100m, 0m, 100m);

            if (cobertura >= 50m) continue;

            insights.Add(new InsightClinicoResponse(
                "BAIXA_ADESAO_META",
                "Metas",
                registros == 0 ? "Media" : "Baixa",
                $"Baixa frequência de registros em “{meta.Nome}”",
                $"Foram encontrados {registros} registro(s) no período, para uma expectativa aproximada de {esperado} conforme a frequência configurada.",
                DateTime.UtcNow,
                $"{Math.Round(cobertura, 0)}% de cobertura",
                "Conferir se a meta continua adequada e se o paciente está conseguindo registrar o acompanhamento."));
        }
    }

    private async Task AvaliarTreinos(
        Guid pacienteId,
        List<InsightClinicoResponse> insights,
        DateTime agora,
        CancellationToken ct)
    {
        var planoAtivo = await db.PlanosTreino.AsNoTracking().AnyAsync(x =>
            x.PacienteId == pacienteId &&
            x.Status == "Ativo" &&
            x.DataInicio <= DateOnly.FromDateTime(agora) &&
            (!x.DataFim.HasValue || x.DataFim.Value >= DateOnly.FromDateTime(agora)), ct);

        if (!planoAtivo) return;

        var inicioAtual = agora.AddDays(-14);
        var inicioAnterior = agora.AddDays(-28);

        var atual = await db.ExecucoesTreino.AsNoTracking().CountAsync(x =>
            x.PacienteId == pacienteId &&
            x.DataHoraInicioUtc >= inicioAtual &&
            x.DataHoraInicioUtc <= agora, ct);

        var anterior = await db.ExecucoesTreino.AsNoTracking().CountAsync(x =>
            x.PacienteId == pacienteId &&
            x.DataHoraInicioUtc >= inicioAnterior &&
            x.DataHoraInicioUtc < inicioAtual, ct);

        if (atual == 0)
        {
            insights.Add(new InsightClinicoResponse(
                "SEM_TREINO_RECENTE",
                "Treinos",
                "Media",
                "Plano ativo sem treino registrado nos últimos 14 dias",
                "Existe um plano de treino ativo, mas nenhuma execução foi registrada nas últimas duas semanas.",
                agora,
                "0 treinos / 14 dias",
                "Confirmar adesão ao plano e se o paciente está utilizando o registro de treinos."));
            return;
        }

        if (anterior >= 2 && atual * 2 < anterior)
        {
            insights.Add(new InsightClinicoResponse(
                "QUEDA_FREQUENCIA_TREINO",
                "Treinos",
                "Baixa",
                "Queda na frequência de treinos registrados",
                $"Foram registrados {atual} treino(s) nos últimos 14 dias contra {anterior} no período anterior.",
                agora,
                $"{anterior} → {atual}",
                "Verificar rotina, recuperação e possíveis barreiras de adesão antes de ajustar a prescrição."));
        }
    }

    private static PacienteInsightsResponse ToPacienteResponse(
        Guid id,
        string nome,
        IReadOnlyCollection<InsightClinicoResponse> insights)
        => new(
            id,
            nome,
            DateTime.UtcNow,
            insights.Count,
            insights.Count(x => x.Severidade == "Alta"),
            insights.Count(x => x.Severidade == "Media"),
            insights.Count(x => x.Severidade == "Baixa"),
            insights);

    private static string SeveridadeMaxima(
        IReadOnlyCollection<InsightClinicoResponse> insights)
        => insights.OrderByDescending(x => PesoSeveridade(x.Severidade))
            .Select(x => x.Severidade)
            .FirstOrDefault() ?? "Baixa";

    private static int PesoSeveridade(string severidade) => severidade switch
    {
        "Alta" => 3,
        "Media" => 2,
        _ => 1
    };
}
