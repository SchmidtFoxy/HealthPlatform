namespace HealthPlatform.Api.Contracts.Pacientes;

public sealed record PacienteListResponse(
    IReadOnlyCollection<PacienteResponse> Itens,
    int Total,
    int Pagina,
    int TamanhoPagina,
    int TotalPaginas);
