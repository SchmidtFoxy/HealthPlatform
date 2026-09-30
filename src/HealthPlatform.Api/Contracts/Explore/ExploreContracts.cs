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


public sealed record StartSportFundamentoResponse(
    string Titulo,
    string Descricao);

public sealed record StartSportModalidadeResponse(
    string Codigo,
    string Nome,
    string Descricao,
    string[] Ambientes,
    string[] Recursos,
    IReadOnlyCollection<StartSportFundamentoResponse> Fundamentos,
    string PrimeiroMarco,
    string ProximoPasso,
    string RegraDeUso);

public sealed record StartSportResponse(
    string? PlanoAtual,
    string? AtividadeRelatada,
    int? FrequenciaSemanalRelatada,
    IReadOnlyCollection<StartSportModalidadeResponse> Modalidades,
    string MensagemDeContexto,
    string RegraDeAutonomia);
