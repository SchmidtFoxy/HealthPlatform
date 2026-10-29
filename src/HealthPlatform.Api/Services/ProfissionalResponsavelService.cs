using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

/// <summary>Origem de quem foi apontado como responsavel pelo paciente.</summary>
public static class OrigemResponsavel
{
    public const string Definido = "Definido";
    public const string UltimaConsulta = "UltimaConsulta";
    public const string UltimoPlano = "UltimoPlano";
    public const string Conversa = "Conversa";
    public const string UnicoProfissional = "UnicoProfissional";
    public const string SemResponsavel = "SemResponsavel";

    public static string Rotulo(string origem) => origem switch
    {
        Definido => "Definido pela administração",
        UltimaConsulta => "Pela consulta mais recente",
        UltimoPlano => "Pelo plano de treino ou dieta mais recente",
        Conversa => "Pelo último profissional que respondeu no chat",
        UnicoProfissional => "Único profissional da equipe do paciente",
        _ => "Sem profissional responsável"
    };
}

public sealed record ResponsavelPaciente(Guid PacienteId, Profissional? Profissional, string Origem)
{
    public string OrigemRotulo => OrigemResponsavel.Rotulo(Origem);
}

/// <summary>
/// Quem acompanha o paciente e recebe as mensagens dele. Ordem: o profissional definido no
/// paciente; a consulta mais recente que nao foi cancelada nem faltou; quem fez o plano de
/// treino ou dieta mais recente; o ultimo profissional que respondeu no chat; o unico profissional da equipe. So contam profissionais da equipe do paciente. Fora disso o paciente fica sem responsavel,
/// em vez de cair em alguem por ordem alfabetica.
/// </summary>
public sealed class ProfissionalResponsavelService(AppDbContext db)
{
    public async Task<ResponsavelPaciente> ResolverAsync(Guid organizacaoId, Guid pacienteId, CancellationToken ct) =>
        (await ResolverEmLoteAsync(organizacaoId, [pacienteId], ct))[pacienteId];

    public async Task<IReadOnlyDictionary<Guid, ResponsavelPaciente>> ResolverEmLoteAsync(
        Guid organizacaoId, IReadOnlyCollection<Guid> pacienteIds, CancellationToken ct)
    {
        var ids = pacienteIds.Distinct().ToArray();
        var resultado = new Dictionary<Guid, ResponsavelPaciente>();
        if (ids.Length == 0) return resultado;

        var profissionais = await db.Profissionais.AsNoTracking()
            .Where(x => x.OrganizacaoId == organizacaoId && x.Ativo)
            .ToDictionaryAsync(x => x.Id, ct);

        // So quem esta na equipe do paciente enxerga ele; so essas pessoas podem receber as mensagens.
        var equipe = (await db.ProfissionaisPacientes.AsNoTracking()
                .Where(x => x.OrganizacaoId == organizacaoId && ids.Contains(x.PacienteId))
                .Select(x => new { x.PacienteId, x.ProfissionalId })
                .ToListAsync(ct))
            .Select(x => (x.PacienteId, x.ProfissionalId))
            .ToHashSet();
        bool NaEquipe(Guid pacienteId, Guid profissionalId) => equipe.Contains((pacienteId, profissionalId));

        var definidos = await db.Pacientes.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.OrganizacaoId == organizacaoId && ids.Contains(x.Id) && x.ProfissionalResponsavelId.HasValue)
            .Select(x => new { x.Id, ProfissionalId = x.ProfissionalResponsavelId!.Value })
            .ToDictionaryAsync(x => x.Id, x => x.ProfissionalId, ct);

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId) && x.Paciente.OrganizacaoId == organizacaoId &&
                x.Status != StatusConsulta.Cancelada && x.Status != StatusConsulta.Faltou)
            .Select(x => new { x.PacienteId, x.ProfissionalId, x.DataHoraUtc })
            .ToListAsync(ct);
        var porConsulta = consultas
            .Where(x => profissionais.ContainsKey(x.ProfissionalId) && NaEquipe(x.PacienteId, x.ProfissionalId))
            .GroupBy(x => x.PacienteId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.DataHoraUtc).First().ProfissionalId);

        var planosTreino = await db.PlanosTreino.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.ProfissionalId, x.CreatedAtUtc })
            .ToListAsync(ct);
        var planosDieta = await db.PlanosAlimentares.AsNoTracking()
            .Where(x => ids.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, x.ProfissionalId, x.CreatedAtUtc })
            .ToListAsync(ct);
        var porPlano = planosTreino.Concat(planosDieta)
            .Where(x => profissionais.ContainsKey(x.ProfissionalId) && NaEquipe(x.PacienteId, x.ProfissionalId))
            .GroupBy(x => x.PacienteId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAtUtc).First().ProfissionalId);

        var conversas = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == organizacaoId && ids.Contains(x.PacienteId) &&
                x.Canal.StartsWith("Chat:") && x.Resultado == "Profissional")
            .GroupBy(x => new { x.PacienteId, x.ProfissionalId })
            .Select(g => new { g.Key.PacienteId, g.Key.ProfissionalId, Ultima = g.Max(x => x.DataHoraUtc) })
            .ToListAsync(ct);
        var porConversa = conversas
            .Where(x => profissionais.ContainsKey(x.ProfissionalId) && NaEquipe(x.PacienteId, x.ProfissionalId))
            .GroupBy(x => x.PacienteId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Ultima).First().ProfissionalId);

        var porEquipe = equipe
            .Where(x => profissionais.ContainsKey(x.ProfissionalId))
            .GroupBy(x => x.PacienteId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ProfissionalId).ToList());

        foreach (var id in ids)
        {
            if (definidos.TryGetValue(id, out var definidoId) && profissionais.TryGetValue(definidoId, out var definido))
                resultado[id] = new(id, definido, OrigemResponsavel.Definido);
            else if (porConsulta.TryGetValue(id, out var consultaId))
                resultado[id] = new(id, profissionais[consultaId], OrigemResponsavel.UltimaConsulta);
            else if (porPlano.TryGetValue(id, out var planoId))
                resultado[id] = new(id, profissionais[planoId], OrigemResponsavel.UltimoPlano);
            else if (porConversa.TryGetValue(id, out var conversaId))
                resultado[id] = new(id, profissionais[conversaId], OrigemResponsavel.Conversa);
            else if (porEquipe.TryGetValue(id, out var daEquipe) && daEquipe.Count == 1)
                resultado[id] = new(id, profissionais[daEquipe[0]], OrigemResponsavel.UnicoProfissional);
            else
                resultado[id] = new(id, null, OrigemResponsavel.SemResponsavel);
        }

        return resultado;
    }

    /// <summary>Pacientes ativos da organizacao cujo responsavel hoje e este profissional.</summary>
    public async Task<Guid[]> PacientesDoProfissionalAsync(Guid organizacaoId, Guid profissionalId, CancellationToken ct)
    {
        var candidatos = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == organizacaoId && x.Ativo)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var responsaveis = await ResolverEmLoteAsync(organizacaoId, candidatos, ct);
        return responsaveis.Values.Where(x => x.Profissional?.Id == profissionalId).Select(x => x.PacienteId).ToArray();
    }
}
