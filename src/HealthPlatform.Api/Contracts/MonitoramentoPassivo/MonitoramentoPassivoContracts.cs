namespace HealthPlatform.Api.Contracts.MonitoramentoPassivo;

public sealed record MonitoramentoPassivoSinalRequest(
    string Fonte,
    string Metrica,
    DateTime InicioUtc,
    DateTime? FimUtc,
    decimal Valor,
    string? Unidade,
    string? IdExterno,
    string? Qualidade);

public sealed record MonitoramentoPassivoImportacaoRequest(
    IReadOnlyList<MonitoramentoPassivoSinalRequest> Sinais);

public sealed record MonitoramentoPassivoImportacaoResponse(
    int Recebidos,
    int Importados,
    int IgnoradosDuplicados,
    int Rejeitados,
    IReadOnlyList<string> FontesReconhecidas);

public sealed record MonitoramentoPassivoMetricaResponse(
    string Metrica,
    string Rotulo,
    string Unidade,
    int Amostras,
    decimal? UltimoValor,
    DateTime? UltimaLeituraUtc,
    decimal? Media,
    decimal? Total,
    decimal? VariacaoPercentual7Dias);

public sealed record MonitoramentoPassivoFonteResponse(
    string Fonte,
    string Rotulo,
    int Amostras,
    DateTime? UltimaSincronizacaoUtc,
    bool ComDadosRecentes);

public sealed record MonitoramentoPassivoResumoResponse(
    Guid PacienteId,
    int Dias,
    int TotalAmostras,
    DateTime? UltimaSincronizacaoUtc,
    IReadOnlyList<MonitoramentoPassivoFonteResponse> Fontes,
    IReadOnlyList<MonitoramentoPassivoMetricaResponse> Metricas,
    string MensagemSeguranca);
