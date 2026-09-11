using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Services;

public static class GamificacaoService
{
    public static async Task RegistrarEventoAsync(AppDbContext db, Guid organizacaoId, Guid pacienteId,
        DateOnly data, string fonte, Guid fonteId, int pontos, string motivo, string? adequacao, CancellationToken ct)
    {
        if (pontos <= 0) return;
        var existe = await db.EventosXp.AnyAsync(x => x.PacienteId == pacienteId && x.Fonte == fonte && x.FonteId == fonteId, ct);
        if (existe) return;
        db.EventosXp.Add(new EventoXp
        {
            OrganizacaoId = organizacaoId, PacienteId = pacienteId, Data = data, Fonte = fonte,
            FonteId = fonteId, Pontos = pontos, Motivo = motivo, Adequacao = adequacao
        });
    }

    public static (int Pontos, string Adequacao, string Motivo) CalcularXpTreino(string? recomendacao, int? esforco)
    {
        if (!esforco.HasValue) return (70, "SemRpe", "Treino concluido; informe o esforco percebido para personalizar o XP.");
        static int NivelEsforco(int rpe) => rpe <= 3 ? 0 : rpe <= 5 ? 1 : rpe <= 8 ? 2 : 3;
        static int NivelRecomendacao(string? r) => r switch { "Recuperacao" => 0, "Leve" => 1, "Pesado" => 3, _ => 2 };
        if (string.IsNullOrWhiteSpace(recomendacao)) return (70, "SemCheckIn", "Treino concluido sem prontidao registrada; XP neutro.");
        var delta = NivelEsforco(esforco.Value) - NivelRecomendacao(recomendacao);
        if (delta == 0) return (100, "Ideal", "Treino alinhado a prontidao do dia.");
        if (Math.Abs(delta) == 1) return (75, delta > 0 ? "Acima" : "Abaixo", "Treino proximo da intensidade sugerida.");
        if (delta >= 2) return (35, "Excesso", "Treino muito acima da intensidade sugerida; XP reduzido para nao premiar sobrecarga.");
        return (60, "Conservador", "Treino abaixo da intensidade sugerida, preservando consistencia sem punicao severa.");
    }

    public static async Task<PortalGamificacaoResponse> MontarResumoAsync(AppDbContext db, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking().Where(x => x.Id == pacienteId)
            .Select(x => new { x.Id, x.OrganizacaoId }).FirstAsync(ct);
        await AtualizarDesafiosEConquistasAsync(db, paciente.OrganizacaoId, pacienteId, dia, ct);
        await db.SaveChangesAsync(ct);

        var eventos = await db.EventosXp.AsNoTracking().Where(x => x.PacienteId == pacienteId).ToListAsync(ct);
        var totalXp = eventos.Sum(x => x.Pontos);
        var nivel = totalXp / 500 + 1;
        var xpNivel = totalXp % 500;
        var desde = dia.AddDays(-13);
        var diasAtivos = eventos.Where(x => x.Data >= desde && x.Data <= dia).Select(x => x.Data).Distinct().ToHashSet();
        var consistencia = Math.Min(100, diasAtivos.Count * 10);
        var referencia = dia;
        if (!diasAtivos.Contains(referencia) && diasAtivos.Contains(dia.AddDays(-1))) referencia = dia.AddDays(-1);
        var streak = 0;
        while (diasAtivos.Contains(referencia.AddDays(-streak))) streak++;
        var hojeXp = eventos.Where(x => x.Data == dia).Sum(x => x.Pontos);
        var recentes = eventos.OrderByDescending(x => x.CreatedAtUtc).Take(5)
            .Select(x => new PortalEventoXpResponse(x.Id, x.Data, x.Fonte, x.Pontos, x.Motivo, x.Adequacao)).ToList();
        var semanaInicio = InicioSemana(dia);
        var desafios = await db.DesafiosSemanaisPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.SemanaInicio == semanaInicio)
            .OrderBy(x => x.Codigo)
            .Select(x => new PortalDesafioSemanalResponse(x.Id, x.Codigo, x.Titulo, x.Descricao, x.Meta, x.Progresso, x.RecompensaXp, x.ConcluidoEmUtc != null))
            .ToListAsync(ct);
        var conquistas = await db.ConquistasPaciente.AsNoTracking().Where(x => x.PacienteId == pacienteId)
            .OrderByDescending(x => x.CreatedAtUtc).Take(4)
            .Select(x => new PortalConquistaResponse(x.Id, x.Codigo, x.Titulo, x.Descricao, x.Icone, x.DataConquista, x.RecompensaXp))
            .ToListAsync(ct);
        return new PortalGamificacaoResponse(totalXp, nivel, xpNivel, 500, consistencia, streak, hojeXp, diasAtivos.Count, recentes, desafios, conquistas);
    }

    private static DateOnly InicioSemana(DateOnly dia)
    {
        var deslocamento = ((int)dia.DayOfWeek + 6) % 7;
        return dia.AddDays(-deslocamento);
    }

    private static async Task AtualizarDesafiosEConquistasAsync(AppDbContext db, Guid organizacaoId, Guid pacienteId, DateOnly dia, CancellationToken ct)
    {
        var semanaInicio = InicioSemana(dia);
        var metaTreinosCiclo = await db.CiclosEsportivosPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Status == "Ativo" && x.DataInicio <= dia && x.DataFim >= dia)
            .Select(x => x.MetaTreinosSemanais).FirstOrDefaultAsync(ct);
        var metaTreinosSemana = metaTreinosCiclo ?? 3;
        var modelos = new[]
        {
            (Codigo: "treinos-3", Titulo: "Ritmo do ciclo", Descricao: $"Complete {metaTreinosSemana} treinos nesta semana.", Tipo: "Treinos", Meta: metaTreinosSemana, Xp: 180),
            (Codigo: "checkins-5", Titulo: "Ouça seu corpo", Descricao: "Faça 5 check-ins de prontidão na semana.", Tipo: "Prontidao", Meta: 5, Xp: 140),
            (Codigo: "dias-5", Titulo: "Consistência sustentável", Descricao: "Tenha atividade saudável registrada em 5 dias da semana.", Tipo: "DiasAtivos", Meta: 5, Xp: 160)
        };
        var existentes = await db.DesafiosSemanaisPaciente.Where(x => x.PacienteId == pacienteId && x.SemanaInicio == semanaInicio).ToListAsync(ct);
        foreach (var modelo in modelos)
        {
            var existente = existentes.FirstOrDefault(x => x.Codigo == modelo.Codigo);
            if (existente is not null)
            {
                if (existente.ConcluidoEmUtc is null) { existente.Meta = modelo.Meta; existente.Descricao = modelo.Descricao; existente.Titulo = modelo.Titulo; }
                continue;
            }
            var novo = new DesafioSemanalPaciente
            {
                OrganizacaoId = organizacaoId, PacienteId = pacienteId, SemanaInicio = semanaInicio, Codigo = modelo.Codigo,
                Titulo = modelo.Titulo, Descricao = modelo.Descricao, TipoMetrica = modelo.Tipo, Meta = modelo.Meta, RecompensaXp = modelo.Xp
            };
            db.DesafiosSemanaisPaciente.Add(novo);
            existentes.Add(novo);
        }

        var inicioUtc = semanaInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = dia.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var treinos = await db.ExecucoesTreino.CountAsync(x => x.PacienteId == pacienteId && x.Status == "Concluido" && x.DataHoraInicioUtc >= inicioUtc && x.DataHoraInicioUtc < fimUtc, ct);
        var checkins = await db.ProntidoesDiarias.CountAsync(x => x.PacienteId == pacienteId && x.Data >= semanaInicio && x.Data <= dia, ct);
        var diasAtivos = await db.EventosXp.Where(x => x.PacienteId == pacienteId && x.Data >= semanaInicio && x.Data <= dia)
            .Select(x => x.Data).Distinct().CountAsync(ct);

        foreach (var desafio in existentes)
        {
            desafio.Progresso = desafio.TipoMetrica switch { "Treinos" => treinos, "Prontidao" => checkins, _ => diasAtivos };
            if (desafio.ConcluidoEmUtc is null && desafio.Progresso >= desafio.Meta)
            {
                desafio.ConcluidoEmUtc = DateTime.UtcNow;
                await RegistrarEventoAsync(db, organizacaoId, pacienteId, dia, "DesafioSemanal", desafio.Id, desafio.RecompensaXp, $"Desafio concluido: {desafio.Titulo}.", "Consistencia", ct);
            }
        }

        var totalTreinos = await db.ExecucoesTreino.CountAsync(x => x.PacienteId == pacienteId && x.Status == "Concluido", ct);
        var totalCheckins = await db.ProntidoesDiarias.CountAsync(x => x.PacienteId == pacienteId, ct);
        var eventos14 = await db.EventosXp.Where(x => x.PacienteId == pacienteId && x.Data >= dia.AddDays(-13) && x.Data <= dia).Select(x => x.Data).Distinct().CountAsync(ct);
        var performance = await PerformanceEsportivaService.MontarAsync(db, pacienteId, dia, ct);
        var possuiPrHistorico = performance.Destaques.Any(x => x.Registros >= 2 && x.EvolucaoMelhorCargaPercentual > 0m);
        var candidatas = new[]
        {
            (Codigo: "primeiro-treino", Titulo: "Primeiro passo", Descricao: "Concluiu o primeiro treino registrado.", Icone: "🏁", Atingiu: totalTreinos >= 1, Xp: 80),
            (Codigo: "dez-treinos", Titulo: "Em movimento", Descricao: "Completou 10 treinos registrados.", Icone: "🏋️", Atingiu: totalTreinos >= 10, Xp: 180),
            (Codigo: "sete-checkins", Titulo: "Autoconhecimento", Descricao: "Registrou 7 check-ins de prontidão.", Icone: "🧠", Atingiu: totalCheckins >= 7, Xp: 140),
            (Codigo: "primeiro-pr", Titulo: "Nova marca", Descricao: "Superou uma melhor carga anterior em um exercício.", Icone: "🏆", Atingiu: possuiPrHistorico, Xp: 150),
            (Codigo: "consistencia-80", Titulo: "Ritmo sustentável", Descricao: "Alcançou consistência de pelo menos 80/100 em 14 dias.", Icone: "🔥", Atingiu: eventos14 >= 8, Xp: 220)
        };
        var conquistadas = await db.ConquistasPaciente.Where(x => x.PacienteId == pacienteId).Select(x => x.Codigo).ToListAsync(ct);
        foreach (var candidata in candidatas.Where(x => x.Atingiu && !conquistadas.Contains(x.Codigo)))
        {
            var conquista = new ConquistaPaciente
            {
                OrganizacaoId = organizacaoId, PacienteId = pacienteId, Codigo = candidata.Codigo, Titulo = candidata.Titulo,
                Descricao = candidata.Descricao, Icone = candidata.Icone, DataConquista = dia, RecompensaXp = candidata.Xp
            };
            db.ConquistasPaciente.Add(conquista);
            await RegistrarEventoAsync(db, organizacaoId, pacienteId, dia, "Conquista", conquista.Id, conquista.RecompensaXp, $"Conquista desbloqueada: {conquista.Titulo}.", "Marco", ct);
        }
    }

}
