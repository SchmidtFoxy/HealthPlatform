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
    IReadOnlyCollection<InteresseExploreResponse> InteressesDeclarados,
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


public sealed record BeginnerJourneyEtapaResponse(
    int Ordem,
    string Titulo,
    string ObjetivoEducacional,
    string EvidenciaDeFamiliaridade,
    string ProximoQuando);

public sealed record BeginnerJourneyResponse(
    string ModalidadeCodigo,
    string ModalidadeNome,
    string Titulo,
    string Descricao,
    IReadOnlyCollection<BeginnerJourneyEtapaResponse> Etapas,
    string RegraDeProgressao,
    string RegraDeAutonomia);

public sealed record BeginnerJourneysResponse(
    IReadOnlyCollection<BeginnerJourneyResponse> Jornadas,
    string Fonte,
    string RegraGeral);


public sealed record HomeWorkoutMovimentoResponse(
    Guid Id,
    string Nome,
    string? GrupoMuscular,
    string? Equipamento,
    string? Descricao,
    string MotivoDaCompatibilidade);

public sealed record HomeWorkoutResponse(
    string Espaco,
    string Recurso,
    string Preferencia,
    string[] EspacosDisponiveis,
    string[] RecursosDisponiveis,
    string[] PreferenciasDisponiveis,
    IReadOnlyCollection<HomeWorkoutMovimentoResponse> Movimentos,
    string Fonte,
    string RegraDeUso);


public sealed record QuickMovementPossibilidadeResponse(
    Guid Id,
    string Nome,
    string? GrupoMuscular,
    string? Equipamento,
    string? Descricao,
    string MotivoDaCompatibilidade);

public sealed record QuickMovementResponse(
    string Janela,
    string Contexto,
    string Preferencia,
    string[] JanelasDisponiveis,
    string[] ContextosDisponiveis,
    string[] PreferenciasDisponiveis,
    IReadOnlyCollection<QuickMovementPossibilidadeResponse> Possibilidades,
    string Fonte,
    string RegraDeUso);


public sealed record TravelModePossibilidadeResponse(
    Guid Id,
    string Nome,
    string? GrupoMuscular,
    string? Equipamento,
    string? Descricao,
    string MotivoDaCompatibilidade);

public sealed record TravelModeResponse(
    string? PlanoAtual,
    string Hospedagem,
    string Recurso,
    string Rotina,
    string[] HospedagensDisponiveis,
    string[] RecursosDisponiveis,
    string[] RotinasDisponiveis,
    IReadOnlyCollection<TravelModePossibilidadeResponse> Possibilidades,
    string Fonte,
    string RegraDeUso);


public sealed record OutdoorModePossibilidadeResponse(
    Guid Id,
    string Nome,
    string? GrupoMuscular,
    string? Equipamento,
    string? Descricao,
    string MotivoDaCompatibilidade);

public sealed record OutdoorModeResponse(
    string Ambiente,
    string Recurso,
    string Interesse,
    string[] AmbientesDisponiveis,
    string[] RecursosDisponiveis,
    string[] InteressesDisponiveis,
    IReadOnlyCollection<OutdoorModePossibilidadeResponse> Possibilidades,
    string Fonte,
    string RegraDeUso);


public sealed record LearnFundamentalsItemResponse(
    string Codigo,
    string Titulo,
    string Explicacao,
    string OQueObservar,
    string ErroComum,
    string ProximoPasso);

public sealed record LearnFundamentalsResponse(
    string Modalidade,
    string Capacidade,
    string[] ModalidadesDisponiveis,
    string[] CapacidadesDisponiveis,
    IReadOnlyCollection<LearnFundamentalsItemResponse> Fundamentos,
    string Fonte,
    string RegraDeUso);


public sealed record ExploreStarterPackSessaoResponse(
    Guid Id,
    string Nome,
    string? Categoria,
    string? Descricao);

public sealed record ExploreStarterPackResponse(
    string Codigo,
    string Nome,
    string ModalidadeCodigo,
    string ModalidadeNome,
    string ObjetivoCodigo,
    string ObjetivoNome,
    IReadOnlyCollection<string> Capacidades,
    IReadOnlyCollection<ExploreStarterPackSessaoResponse> Sessoes,
    bool PossuiReferencias,
    string EstadoEditorial);

public sealed record ExploreStarterPacksResponse(
    string? Modalidade,
    string? Objetivo,
    string[] ModalidadesDisponiveis,
    IReadOnlyCollection<ExploreStarterPackResponse> Packs,
    string Fonte,
    string RegraDeUso);


public sealed record InteresseExploreResponse(
    Guid Id,
    string Codigo,
    string Nome,
    string Intencao,
    string Origem,
    DateTime DeclaradoEmUtc);

public sealed record InteresseExploreOpcaoResponse(
    string Codigo,
    string Nome);

public sealed record InteresseExploreCatalogoResponse(
    IReadOnlyCollection<InteresseExploreResponse> Interesses,
    IReadOnlyCollection<InteresseExploreOpcaoResponse> ModalidadesDisponiveis,
    string[] IntencoesDisponiveis,
    string RegraDeUso);

public sealed record AtualizarInteressesExploreRequest(
    IReadOnlyCollection<AtualizarInteresseExploreItemRequest> Interesses);

public sealed record AtualizarInteresseExploreItemRequest(
    string Codigo,
    string Intencao);


public sealed record SportsExpansionICapacidadeResponse(
    string Codigo,
    string Nome);

public sealed record SportsExpansionIObjetivoResponse(
    string Codigo,
    string Nome,
    IReadOnlyCollection<SportsExpansionICapacidadeResponse> Capacidades);

public sealed record SportsExpansionIModalidadeResponse(
    string Codigo,
    string Nome,
    string Descricao,
    string[] Ambientes,
    string[] Recursos,
    IReadOnlyCollection<SportsExpansionIObjetivoResponse> Objetivos);

public sealed record SportsExpansionIResponse(
    IReadOnlyCollection<SportsExpansionIModalidadeResponse> Modalidades,
    string CadeiaEstrutural,
    string RegraDeUso);


public sealed record FootballFutsalFundamentoResponse(
    string Codigo,
    string Nome,
    string Descricao,
    string OQueObservar);

public sealed record FootballFutsalCapacidadeResponse(
    string Codigo,
    string Nome,
    string Contexto,
    string DiferencaDaOutraModalidade);

public sealed record FootballFutsalSessaoReferenciaResponse(
    Guid Id,
    string Nome,
    string? Categoria,
    string? Descricao,
    string MotivoDaReferencia);

public sealed record FootballFutsalModalidadeResponse(
    string Codigo,
    string Nome,
    string AmbientePrincipal,
    string Dinamica,
    string[] Recursos,
    IReadOnlyCollection<FootballFutsalFundamentoResponse> Fundamentos,
    IReadOnlyCollection<FootballFutsalCapacidadeResponse> Capacidades,
    IReadOnlyCollection<FootballFutsalSessaoReferenciaResponse> SessoesReferencia,
    string DiferencaChave);

public sealed record FootballFutsalResponse(
    IReadOnlyCollection<FootballFutsalModalidadeResponse> Modalidades,
    string Fonte,
    string RegraDeUso);


public sealed record BasketballVolleyballFundamentoResponse(
    string Codigo,
    string Nome,
    string Descricao,
    string OQueObservar);

public sealed record BasketballVolleyballCapacidadeResponse(
    string Codigo,
    string Nome,
    string Contexto,
    string DiferencaDaOutraModalidade);

public sealed record BasketballVolleyballSessaoReferenciaResponse(
    Guid Id,
    string Nome,
    string? Categoria,
    string? Descricao,
    string MotivoDaReferencia);

public sealed record BasketballVolleyballModalidadeResponse(
    string Codigo,
    string Nome,
    string AmbientePrincipal,
    string Dinamica,
    string[] Recursos,
    IReadOnlyCollection<BasketballVolleyballFundamentoResponse> Fundamentos,
    IReadOnlyCollection<BasketballVolleyballCapacidadeResponse> Capacidades,
    IReadOnlyCollection<BasketballVolleyballSessaoReferenciaResponse> SessoesReferencia,
    string DiferencaChave);

public sealed record BasketballVolleyballResponse(
    IReadOnlyCollection<BasketballVolleyballModalidadeResponse> Modalidades,
    string Fonte,
    string RegraDeUso);


public sealed record TennisBeachTennisFundamentoResponse(
    string Codigo,
    string Nome,
    string Descricao,
    string OQueObservar);

public sealed record TennisBeachTennisCapacidadeResponse(
    string Codigo,
    string Nome,
    string Contexto,
    string DiferencaDaOutraModalidade);

public sealed record TennisBeachTennisSessaoReferenciaResponse(
    Guid Id,
    string Nome,
    string? Categoria,
    string? Descricao,
    string MotivoDaReferencia);

public sealed record TennisBeachTennisModalidadeResponse(
    string Codigo,
    string Nome,
    string AmbientePrincipal,
    string Dinamica,
    string[] Recursos,
    IReadOnlyCollection<TennisBeachTennisFundamentoResponse> Fundamentos,
    IReadOnlyCollection<TennisBeachTennisCapacidadeResponse> Capacidades,
    IReadOnlyCollection<TennisBeachTennisSessaoReferenciaResponse> SessoesReferencia,
    string DiferencaChave);

public sealed record TennisBeachTennisResponse(
    IReadOnlyCollection<TennisBeachTennisModalidadeResponse> Modalidades,
    string Fonte,
    string RegraDeUso);

public sealed record SportsExpansionIICapacidadeResponse(
    string Codigo,
    string Nome);

public sealed record SportsExpansionIIObjetivoResponse(
    string Codigo,
    string Nome,
    IReadOnlyCollection<SportsExpansionIICapacidadeResponse> Capacidades);

public sealed record SportsExpansionIIModalidadeResponse(
    string Codigo,
    string Nome,
    string Descricao,
    string[] Ambientes,
    string[] Recursos,
    IReadOnlyCollection<SportsExpansionIIObjetivoResponse> Objetivos);

public sealed record SportsExpansionIIResponse(
    IReadOnlyCollection<SportsExpansionIIModalidadeResponse> Modalidades,
    string CadeiaEstrutural,
    string RegraDeUso);

public sealed record SwimmingFundamentoResponse(
    string Codigo,
    string Nome,
    string Descricao,
    string OQueObservar);

public sealed record SwimmingEstiloResponse(
    string Codigo,
    string Nome,
    string Caracteristica,
    string FocoTecnico);

public sealed record SwimmingCapacidadeResponse(
    string Codigo,
    string Nome,
    string Contexto);

public sealed record SwimmingSessaoReferenciaResponse(
    Guid Id,
    string Nome,
    string? Categoria,
    string? Descricao,
    string MotivoDaReferencia);

public sealed record SwimmingResponse(
    IReadOnlyCollection<SwimmingEstiloResponse> Estilos,
    IReadOnlyCollection<SwimmingFundamentoResponse> Fundamentos,
    IReadOnlyCollection<SwimmingCapacidadeResponse> Capacidades,
    IReadOnlyCollection<SwimmingSessaoReferenciaResponse> SessoesReferencia,
    string Fonte,
    string RegraDeUso);
