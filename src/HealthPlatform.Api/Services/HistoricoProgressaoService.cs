using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Organiza eventos de progressao realmente registrados em uma linha do tempo longitudinal.
/// A origem sao eventos realmente registrados e persistidos em EventosProgressaoSupervisionada.
/// nao ranqueia eventos, nao presume causalidade e nao fabrica resposta retrospectiva onde nao ha dados.
/// </summary>
public static class HistoricoProgressaoService
{
    public static async Task<PortalHistoricoProgressaoResponse> MontarAsync(
        AppDbContext db, Guid organizacaoId, Guid pacienteId,
        PortalInterpretacaoLongitudinalProgressaoResponse interpretacaoAtual,
        DateOnly dia, CancellationToken ct)
    {
        var eventos = await db.EventosProgressaoSupervisionada.AsNoTracking()
            .Where(x => x.OrganizacaoId == organizacaoId && x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataAplicacaoUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .Select(x => new
            {
                x.Id, x.Eixo, x.Descricao, x.DataAplicacaoUtc, x.Status, x.EncerradoEmUtc,
                x.Observacoes, ProfissionalNome = x.Profissional.Nome, x.CicloEsportivoPacienteId
            })
            .ToListAsync(ct);

        if (eventos.Count == 0)
            return new("SemHistorico", "Linha do tempo de progressoes ainda vazia",
                "Nenhuma progressao supervisionada foi registrada para este paciente.",
                0, 0, false, Array.Empty<PortalHistoricoProgressaoItemResponse>(),
                "A linha do tempo nasce apenas de eventos realmente aplicados e registrados por profissional.",
                "Ausencia de historico nao significa ausencia de cuidado; sugestoes nao sao convertidas em eventos automaticamente.");

        var agoraUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddTicks(-1);
        var itens = eventos.Select((x, index) =>
        {
            var fim = x.EncerradoEmUtc ?? agoraUtc;
            var dias = Math.Max(0, (int)Math.Floor((fim - x.DataAplicacaoUtc).TotalDays));
            var ehEventoDaInterpretacaoAtual = index == 0 && interpretacaoAtual.DataAplicacaoUtc.HasValue &&
                Math.Abs((interpretacaoAtual.DataAplicacaoUtc.Value - x.DataAplicacaoUtc).TotalSeconds) < 1 &&
                string.Equals(interpretacaoAtual.EixoProgressao, x.Eixo, StringComparison.OrdinalIgnoreCase);

            string estadoResposta;
            string resumoResposta;
            if (ehEventoDaInterpretacaoAtual)
            {
                estadoResposta = interpretacaoAtual.Estado;
                resumoResposta = interpretacaoAtual.Resumo;
            }
            else if (x.Status == "Encerrado")
            {
                estadoResposta = "HistoricoRegistrado";
                resumoResposta = string.IsNullOrWhiteSpace(x.Observacoes)
                    ? "Evento encerrado e preservado como marco temporal; esta timeline nao inventa uma resposta retrospectiva."
                    : "Evento encerrado com observacao profissional registrada; consulte a observacao junto do restante do contexto.";
            }
            else
            {
                estadoResposta = "EmObservacao";
                resumoResposta = "Evento em observacao; a resposta deve ser interpretada com os dados longitudinais disponiveis.";
            }

            return new PortalHistoricoProgressaoItemResponse(
                x.Id, x.Eixo, x.Descricao, x.DataAplicacaoUtc, x.Status, x.EncerradoEmUtc, dias,
                x.ProfissionalNome, x.CicloEsportivoPacienteId, estadoResposta, resumoResposta, x.Observacoes);
        }).ToList();

        var encerrados = itens.Count(x => x.Status == "Encerrado");
        var possuiAtivo = itens.Any(x => x.Status == "EmObservacao");
        var estado = possuiAtivo ? "EmAcompanhamento" : "HistoricoDisponivel";
        var resumo = possuiAtivo
            ? $"{itens.Count} evento(s) registrado(s); existe uma progressao atualmente em observacao."
            : $"{itens.Count} evento(s) registrado(s), todos fora de observacao ativa.";

        return new(estado, "Historico de progressoes e linha do tempo esportiva", resumo,
            itens.Count, encerrados, possuiAtivo, itens,
            "Leia a sequencia temporal junto de ciclos, carga, recuperacao e contexto profissional. Eventos nao sao ranqueados entre si e maior mudanca nao significa melhor progressao.",
            "Linha do tempo documenta sequencia e contexto; nao prova causalidade, nao gera score comparativo e nao autoriza nova progressao automaticamente.");
    }
}
