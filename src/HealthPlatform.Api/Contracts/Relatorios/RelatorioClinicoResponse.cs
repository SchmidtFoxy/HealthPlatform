namespace HealthPlatform.Api.Contracts.Relatorios;

public sealed record RelatorioClinicoResponse(
    Guid Id, Guid PacienteId, Guid ProfissionalId, string ProfissionalNome, string Titulo,
    DateTime? DataInicioUtc, DateTime? DataFimUtc, DateTime DataGeracaoUtc,
    string? ConclusaoMedica, string VersaoTemplate, RelatorioClinicoConteudoResponse Conteudo, DateTime CreatedAtUtc);

public sealed record RelatorioClinicoConteudoResponse(
    RelatorioPacienteResponse Paciente, DateTime? PeriodoInicioUtc, DateTime? PeriodoFimUtc,
    RelatorioIndicadoresResponse Indicadores, RelatorioAnamneseResponse? UltimaAnamnese,
    IReadOnlyCollection<RelatorioConsultaResponse> ConsultasRecentes,
    IReadOnlyCollection<RelatorioMarcadorResponse> ExamesRecentes,
    IReadOnlyCollection<RelatorioMarcadorResponse> ResultadosForaDaFaixaInformada);

public sealed record RelatorioPacienteResponse(Guid Id, string Nome, DateOnly? DataNascimento, string? Sexo, string? Profissao);

public sealed record RelatorioIndicadoresResponse(
    int Consultas, int Avaliacoes, int Exames,
    decimal? PesoInicialKg, decimal? PesoAtualKg, decimal? VariacaoPesoKg,
    decimal? PercentualGorduraInicial, decimal? PercentualGorduraAtual, decimal? VariacaoPercentualGordura,
    decimal? CinturaInicialCm, decimal? CinturaAtualCm, decimal? VariacaoCinturaCm, decimal? ImcAtual);

public sealed record RelatorioAnamneseResponse(
    DateTime DataUtc, string? ObjetivoAcompanhamento, decimal? SonoHorasMedia, string? SonoQualidade,
    int? EstresseNivel, string? AtividadeFisica, int? AtividadeFisicaDiasSemana, decimal? AguaLitrosDia,
    string? Medicamentos, string? Suplementos, string? Observacoes);

public sealed record RelatorioConsultaResponse(
    Guid Id, DateTime DataHoraUtc, string? Motivo, string? QueixaPrincipal, string? Evolucao, string? Conduta, string Status);

public sealed record RelatorioMarcadorResponse(
    Guid ExameId, DateTime DataColetaUtc, string Marcador, decimal? ValorNumerico, string? ValorTexto,
    string? Unidade, decimal? ReferenciaMinima, decimal? ReferenciaMaxima, string? ReferenciaTexto,
    string? Situacao, string? Laboratorio);
