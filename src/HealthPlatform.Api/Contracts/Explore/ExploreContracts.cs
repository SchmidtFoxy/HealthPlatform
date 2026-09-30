namespace HealthPlatform.Api.Contracts.Explore;

public sealed record ExploreCaminhoResponse(
    string Codigo,
    string Titulo,
    string Descricao,
    string[] Contextos,
    string[] Modalidades,
    string Tipo);

public sealed record ExploreFoundationResponse(
    string? PlanoAtual,
    string? CicloAtual,
    string? ObjetivoAtual,
    string? AtividadeRelatada,
    int? FrequenciaSemanalRelatada,
    IReadOnlyCollection<ExploreCaminhoResponse> Caminhos,
    string RegraDeAutonomia);
