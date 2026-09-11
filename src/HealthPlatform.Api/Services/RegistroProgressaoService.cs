using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>
/// Exibe apenas eventos explicitamente registrados por profissional. Nao presume que uma sugestao foi aplicada.
/// </summary>
public static class RegistroProgressaoService
{
    public static async Task<PortalRegistroProgressaoResponse> MontarAsync(
        AppDbContext db, Guid organizacaoId, Guid pacienteId, CancellationToken ct)
    {
        var evento = await db.EventosProgressaoSupervisionada.AsNoTracking()
            .Where(x => x.OrganizacaoId == organizacaoId && x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataAplicacaoUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new PortalEventoProgressaoSupervisionadaResponse(
                x.Id, x.Eixo, x.Descricao, x.DataAplicacaoUtc, x.Status, x.EncerradoEmUtc, x.Observacoes,
                x.Profissional.Nome, x.CicloEsportivoPacienteId))
            .FirstOrDefaultAsync(ct);

        if (evento is null)
            return new("SemRegistro", "Nenhuma progressao aplicada registrada",
                "As camadas de decisao continuam sendo orientativas ate que um profissional registre explicitamente uma mudanca aplicada.",
                false, null,
                "Ausencia de evento nao significa ausencia de cuidado e o sistema nao presume aplicacao, dose, carga ou causalidade.");

        var ativo = evento.Status == "EmObservacao";
        return new(ativo ? "EmObservacao" : "Encerrado",
            ativo ? "Progressao registrada em observacao" : "Ultima progressao registrada encerrada",
            ativo
                ? "Existe uma referencia temporal real para acompanhar a resposta a partir da mudanca registrada."
                : "O ultimo evento possui inicio e encerramento registrados e permanece disponivel como contexto longitudinal.",
            ativo, evento,
            "O registro documenta o que foi aplicado por profissional; nao prova causalidade e nao autoriza nova progressao automatica.");
    }
}
