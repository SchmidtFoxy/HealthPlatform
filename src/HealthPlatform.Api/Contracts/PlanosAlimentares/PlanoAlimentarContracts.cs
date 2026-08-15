namespace HealthPlatform.Api.Contracts.PlanosAlimentares;

public record SubstituicaoPlanoRequest(Guid AlimentoId, decimal Quantidade, string Unidade, decimal QuantidadeGramas, string? Observacao);
public record ItemRefeicaoPlanoRequest(Guid AlimentoId, decimal Quantidade, string Unidade, decimal QuantidadeGramas, string? Observacao, IReadOnlyCollection<SubstituicaoPlanoRequest>? Substituicoes);
public record RefeicaoPlanoRequest(
    string Nome,
    TimeOnly? Horario,
    int Ordem,
    string? Observacoes,
    decimal? MetaCalorias,
    decimal? MetaProteinasG,
    decimal? MetaCarboidratosG,
    decimal? MetaGordurasG,
    decimal? MetaFibrasG,
    IReadOnlyCollection<ItemRefeicaoPlanoRequest> Itens);
public record UpsertPlanoAlimentarRequest(
    string Nome,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string Status,
    string? Observacoes,
    decimal? MetaCalorias,
    decimal? MetaProteinasG,
    decimal? MetaCarboidratosG,
    decimal? MetaGordurasG,
    decimal? MetaFibrasG,
    IReadOnlyCollection<RefeicaoPlanoRequest> Refeicoes);

public record AtualizarMetasNutricionaisRequest(
    decimal? MetaCalorias,
    decimal? MetaProteinasG,
    decimal? MetaCarboidratosG,
    decimal? MetaGordurasG,
    decimal? MetaFibrasG);


public record AtualizarMetasRefeicaoRequest(
    decimal? MetaCalorias,
    decimal? MetaProteinasG,
    decimal? MetaCarboidratosG,
    decimal? MetaGordurasG,
    decimal? MetaFibrasG);

public record DistribuicaoMetaRefeicaoRequest(Guid RefeicaoId, decimal Percentual);
public record DistribuirMetasRefeicoesRequest(IReadOnlyCollection<DistribuicaoMetaRefeicaoRequest> Refeicoes);

public record DuplicarPlanoAlimentarRequest(string Nome, DateOnly DataInicio, DateOnly? DataFim, decimal? AjustePercentual, decimal? CaloriasAlvo, bool ConcluirPlanoAnterior);
public record SimulacaoAjustePlanoResponse(Guid PlanoId, decimal AjustePercentual, decimal Fator, TotaisNutricionaisResponse TotaisAtuais, TotaisNutricionaisResponse TotaisProjetados, int ItensAfetados);

public record TotaisNutricionaisResponse(decimal Calorias, decimal ProteinasG, decimal CarboidratosG, decimal GordurasG, decimal FibrasG);
public record MetasNutricionaisResponse(decimal? Calorias, decimal? ProteinasG, decimal? CarboidratosG, decimal? GordurasG, decimal? FibrasG);
public record DesviosNutricionaisResponse(decimal? Calorias, decimal? ProteinasG, decimal? CarboidratosG, decimal? GordurasG, decimal? FibrasG);
public record DistribuicaoNutricionalResponse(decimal CaloriasPercentual, decimal ProteinasPercentual, decimal CarboidratosPercentual, decimal GordurasPercentual, decimal FibrasPercentual);
public record DistribuicaoRefeicaoResponse(
    Guid RefeicaoId,
    string Nome,
    TimeOnly? Horario,
    TotaisNutricionaisResponse Totais,
    MetasNutricionaisResponse Metas,
    DesviosNutricionaisResponse Desvios,
    DistribuicaoNutricionalResponse PercentuaisDoDia);
public record AnalisePlanoAlimentarResponse(Guid PlanoId, MetasNutricionaisResponse Metas, TotaisNutricionaisResponse Prescrito, DesviosNutricionaisResponse Desvios, IReadOnlyCollection<DistribuicaoRefeicaoResponse> Refeicoes);
public record SubstituicaoPlanoResponse(Guid Id, Guid AlimentoId, string AlimentoNome, decimal Quantidade, string Unidade, decimal QuantidadeGramas, string? Observacao, TotaisNutricionaisResponse Totais);
public record ItemRefeicaoPlanoResponse(Guid Id, Guid AlimentoId, string AlimentoNome, decimal Quantidade, string Unidade, decimal QuantidadeGramas, string? Observacao, TotaisNutricionaisResponse Totais, IReadOnlyCollection<SubstituicaoPlanoResponse> Substituicoes);
public record RefeicaoPlanoResponse(
    Guid Id,
    string Nome,
    TimeOnly? Horario,
    int Ordem,
    string? Observacoes,
    MetasNutricionaisResponse Metas,
    DesviosNutricionaisResponse Desvios,
    TotaisNutricionaisResponse Totais,
    IReadOnlyCollection<ItemRefeicaoPlanoResponse> Itens);
public record PlanoAlimentarResponse(Guid Id, Guid PacienteId, Guid ProfissionalId, string ProfissionalNome, string Nome, DateOnly DataInicio, DateOnly? DataFim, string Status, string? Observacoes, Guid? PlanoOrigemId, int Versao, decimal AjustePercentual, decimal? MetaCalorias, decimal? MetaProteinasG, decimal? MetaCarboidratosG, decimal? MetaGordurasG, decimal? MetaFibrasG, TotaisNutricionaisResponse TotaisDiarios, IReadOnlyCollection<RefeicaoPlanoResponse> Refeicoes, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
