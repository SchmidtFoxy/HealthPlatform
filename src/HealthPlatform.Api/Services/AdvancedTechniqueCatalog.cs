namespace HealthPlatform.Api.Services;

public sealed record AdvancedTechniqueDefinition(
    string Codigo,
    string Nome,
    string Descricao,
    IReadOnlyCollection<string> ParametrosSugeridos);

public static class AdvancedTechniqueCatalog
{
    private static readonly IReadOnlyCollection<AdvancedTechniqueDefinition> Itens = new[]
    {
        new AdvancedTechniqueDefinition("drop-set", "Drop set", "Redução planejada de carga dentro do mesmo exercício, quando prescrita pelo profissional.", new[] { "reducoes", "percentual-ou-carga", "repeticoes-por-etapa" }),
        new AdvancedTechniqueDefinition("bi-set", "Bi-set", "Dois exercícios executados em sequência conforme a prescrição profissional.", new[] { "exercicio-pareado", "ordem", "descanso-apos-par" }),
        new AdvancedTechniqueDefinition("rest-pause", "Rest-pause", "Pausas curtas intra-série registradas como parte da técnica prescrita.", new[] { "pausa-segundos", "blocos", "repeticoes-por-bloco" }),
        new AdvancedTechniqueDefinition("cluster", "Cluster", "Série dividida em pequenos blocos com pausas internas planejadas.", new[] { "blocos", "repeticoes-por-bloco", "pausa-segundos" }),
        new AdvancedTechniqueDefinition("myo-reps", "Myo-reps", "Série de ativação seguida por mini-séries conforme definição profissional.", new[] { "ativacao", "mini-series", "pausa-segundos" }),
        new AdvancedTechniqueDefinition("isometria", "Isometria", "Trecho isométrico estruturado dentro do exercício.", new[] { "duracao-segundos", "posicao" }),
        new AdvancedTechniqueDefinition("pre-exaustao", "Pré-exaustão", "Exercício preparatório encadeado antes do movimento principal.", new[] { "exercicio-pareado", "ordem", "descanso" }),
        new AdvancedTechniqueDefinition("tempo-controlado", "Tempo controlado", "Execução com intenção de tempo/cadência definida pelo profissional.", new[] { "cadencia", "fase-enfatizada" })
    };

    public static IReadOnlyCollection<AdvancedTechniqueDefinition> Listar() => Itens;

    public static string? NormalizarCodigo(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var normalizado = codigo.Trim().ToLowerInvariant();
        return Itens.Any(x => x.Codigo == normalizado) ? normalizado : null;
    }

    public static AdvancedTechniqueDefinition? Encontrar(string? codigo)
    {
        var normalizado = NormalizarCodigo(codigo);
        return normalizado is null ? null : Itens.First(x => x.Codigo == normalizado);
    }
}
