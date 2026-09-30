using HealthPlatform.Api.Contracts.PerformancePassport;
using HealthPlatform.Infrastructure.Data;

namespace HealthPlatform.Api.Services;

public static class AthletePerformancePassportService
{
    public static async Task<AthletePerformancePassportResponse> MontarAsync(
        AppDbContext db, Guid pacienteId, CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var performance = await PerformanceEsportivaService.MontarAsync(db, pacienteId, hoje, ct);

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

        var dominios = new AthletePerformancePassportDomainResponse[]
        {
            new(
                "cargas",
                "Cargas",
                melhoresMarcas.Length > 0 ? "ComDados" : "SemDados",
                "Execuções de treino concluídas",
                melhoresMarcas.Length,
                "Melhores cargas são comparadas apenas dentro do mesmo exercício e da mesma unidade."),
            new(
                "recordes",
                "Recordes",
                melhoresMarcas.Length > 0 ? "ComDados" : "SemDados",
                "Melhores cargas registradas",
                performance.PrsRecentes,
                "PR significa melhor carga efetivamente registrada; o AESYN não estima 1RM."),
            new(
                "tempos",
                "Tempos",
                "SemFonteEstruturada",
                "Foundation",
                0,
                "Duração de sessão não é convertida automaticamente em recorde de tempo."),
            new(
                "provas",
                "Provas",
                "SemFonteEstruturada",
                "Foundation",
                0,
                "Resultados de prova ainda não possuem contrato dedicado no Performance Passport."),
            new(
                "testes",
                "Testes",
                "SemFonteEstruturada",
                "Foundation",
                0,
                "Testes de performance ainda não possuem contrato dedicado no Performance Passport."),
            new(
                "habilidades",
                "Habilidades",
                "SemFonteEstruturada",
                "Foundation",
                0,
                "Habilidades não são inferidas a partir de carga, volume ou frequência de treino."),
            new(
                "marcos",
                "Marcos",
                melhoresMarcas.Length > 0 ? "BaseDisponivel" : "SemDados",
                "Melhores marcas registradas",
                melhoresMarcas.Length,
                "A Foundation usa melhores marcas como base de marcos, sem criar conquistas retroativas.")
        };

        var estado = melhoresMarcas.Length > 0 ? "BaseDePerformance" : "BaseEmConstrucao";

        return new AthletePerformancePassportResponse(
            "v0.28.0",
            performance.DiasObservados,
            performance.TreinosPeriodo,
            performance.PrsRecentes,
            estado,
            dominios,
            melhoresMarcas,
            "Athlete Performance Passport consolida somente registros existentes. Não estima 1RM, não fabrica recordes, não certifica habilidade e não substitui avaliação profissional.");
    }
}
