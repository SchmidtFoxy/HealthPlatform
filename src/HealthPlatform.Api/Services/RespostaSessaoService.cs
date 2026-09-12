using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class RespostaSessaoService
{
    public static async Task<PortalRespostaSessaoResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        // Resposta observada depois da sessao nao prova que a sessao causou a resposta.
        // O check-in do mesmo dia pode ter ocorrido antes do treino; por isso a referencia principal e o dia seguinte.
        var limiteFimUtc = DateTime.SpecifyKind(dia.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var limiteInicioUtc = limiteFimUtc.AddDays(-3);
        var sessao = await db.ExecucoesTreino.AsNoTracking()
            .Include(x => x.SessaoTreino)
            .Where(x => x.PacienteId == pacienteId && x.Status == "Concluido" &&
                        x.DataHoraInicioUtc >= limiteInicioUtc && x.DataHoraInicioUtc < limiteFimUtc)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .FirstOrDefaultAsync(ct);

        if (sessao is null)
        {
            return new PortalRespostaSessaoResponse(
                "SemSessao", "Resposta a sessao", "Nao ha sessao concluida recente para iniciar uma janela de resposta.",
                null, null, null, null, null, null, null, null, null, 0,
                Array.Empty<PortalRespostaSessaoEixoResponse>(),
                "Sem uma sessao registrada, o sistema nao cria resposta presumida.",
                "Resposta a sessao e acompanhamento temporal; nao diagnostica lesao, nao atribui causalidade e nao prescreve nova carga.");
        }

        var dataSessao = DateOnly.FromDateTime(sessao.DataHoraInicioUtc);
        var dataRespostaEsperada = dataSessao.AddDays(1);
        var checkin = await db.ProntidoesDiarias.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Data == dataRespostaEsperada && x.Data <= dia)
            .FirstOrDefaultAsync(ct);

        var fimSessao = sessao.DataHoraFimUtc ?? sessao.DataHoraInicioUtc.AddMinutes(sessao.DuracaoMinutos ?? 0);
        var fimJanelaDor = fimSessao.AddHours(36);
        if (fimJanelaDor > limiteFimUtc) fimJanelaDor = limiteFimUtc;
        var doresPos = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Tipo == "DorCorporal" &&
                        x.DataHoraUtc >= fimSessao && x.DataHoraUtc < fimJanelaDor)
            .ToListAsync(ct);

        if (checkin is null)
        {
            var resumoAguardando = dataRespostaEsperada > dia
                ? "A sessao foi concluida, mas a janela principal de resposta do dia seguinte ainda nao aconteceu."
                : "A sessao foi concluida, mas ainda nao existe check-in do dia seguinte para interpretar a resposta.";
            return new PortalRespostaSessaoResponse(
                "AguardandoResposta", "Resposta a sessao", resumoAguardando,
                sessao.DataHoraInicioUtc, sessao.SessaoTreino?.Nome, sessao.EsforcoPercebido, sessao.DuracaoMinutos,
                null, null, null, null, null, doresPos.Count,
                doresPos.Count > 0
                    ? new[] { new PortalRespostaSessaoEixoResponse("dor-localizada", "Dor localizada pos-sessao", "Observar", $"{doresPos.Count} registro(s)", "0 registros e apenas ausencia documental", "Registros feitos depois da sessao sao contexto temporal e nao demonstram causa.") }
                    : Array.Empty<PortalRespostaSessaoEixoResponse>(),
                "Aguardar o check-in posterior evita usar dados pre-treino como se fossem resposta ao treino.",
                "Nao aumentar exigencia para gerar dados; ausencia de check-in nao e resposta negativa nem falha de adesao.");
        }

        var eixos = new List<PortalRespostaSessaoEixoResponse>();
        string EstadoEscala(int valor, int atencao, int revisar, bool invertido = false)
        {
            if (invertido) return valor >= revisar ? "Revisar" : valor >= atencao ? "Observar" : "Estavel";
            return valor <= revisar ? "Revisar" : valor <= atencao ? "Observar" : "Estavel";
        }

        var estadoRecuperacao = EstadoEscala(checkin.RecuperacaoNivel, 6, 4);
        var estadoDor = EstadoEscala(checkin.DorNivel, 4, 7, true);
        var estadoDisposicao = EstadoEscala(checkin.DisposicaoNivel, 6, 4);
        var estadoProntidao = checkin.Score < 50 ? "Revisar" : checkin.Score < 65 ? "Observar" : "Estavel";
        eixos.Add(new("recuperacao", "Recuperacao percebida", estadoRecuperacao, $"{checkin.RecuperacaoNivel}/10", "Check-in do dia seguinte", "Recuperacao percebida e observada separadamente de performance."));
        eixos.Add(new("dor", "Dor geral", estadoDor, $"{checkin.DorNivel}/10", "Check-in do dia seguinte", "Dor posterior merece contexto; associacao temporal nao implica causalidade."));
        eixos.Add(new("disposicao", "Disposicao", estadoDisposicao, $"{checkin.DisposicaoNivel}/10", "Check-in do dia seguinte", "Disposicao e percepcao subjetiva e nao substitui recuperacao ou dor."));
        eixos.Add(new("prontidao", "Prontidao", estadoProntidao, $"{checkin.Score}/100", "Check-in do dia seguinte", "Prontidao resume o check-in, mas nao e score de sucesso da sessao."));
        if (doresPos.Count > 0)
            eixos.Add(new("dor-localizada", "Dor localizada pos-sessao", "Observar", $"{doresPos.Count} registro(s)", "Janela de ate 36h apos a sessao", "Mostra ocorrencias registradas no tempo; nao conclui que a sessao causou a dor."));

        var revisar = eixos.Any(x => x.Estado == "Revisar");
        var observar = eixos.Any(x => x.Estado == "Observar");
        var estado = revisar ? "Revisar" : observar ? "Observar" : "RespostaEstavel";
        var resumo = estado switch
        {
            "Revisar" => "A resposta registrada no dia seguinte tem pelo menos um eixo que merece revisao antes de nova progressao.",
            "Observar" => "A resposta pos-sessao apresenta sinais que merecem acompanhamento antes de mudar novamente a estrategia.",
            _ => "Os eixos registrados no dia seguinte estao estaveis; isso sustenta observacao, nao autoriza nova progressao automaticamente."
        };

        return new PortalRespostaSessaoResponse(
            estado, "Resposta a sessao", resumo, sessao.DataHoraInicioUtc, sessao.SessaoTreino?.Nome,
            sessao.EsforcoPercebido, sessao.DuracaoMinutos, checkin.Data, checkin.Score, checkin.RecuperacaoNivel,
            checkin.DorNivel, checkin.DisposicaoNivel, doresPos.Count, eixos,
            "A leitura compara o treino registrado com o primeiro check-in do dia seguinte e com dor localizada registrada apos a sessao. Mudanca temporal nao prova causa.",
            "Resposta estavel nao significa sucesso causal, liberacao medica ou permissao para aumentar carga; revisar contexto com o profissional quando houver sinais persistentes ou importantes.");
    }
}
