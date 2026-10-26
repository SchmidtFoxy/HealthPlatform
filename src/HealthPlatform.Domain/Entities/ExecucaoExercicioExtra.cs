using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

/// <summary>Exercicio que o paciente acrescentou por conta propria durante o treino, fora da prescricao.</summary>
public class ExecucaoExercicioExtra : BaseEntity
{
    public Guid ExecucaoTreinoId { get; set; }
    public Guid ExercicioId { get; set; }
    public int Ordem { get; set; }
    public int? SeriesRealizadas { get; set; }
    public string? RepeticoesRealizadas { get; set; }
    public decimal? CargaRealizada { get; set; }
    public string? UnidadeCarga { get; set; }
    public string? Observacoes { get; set; }

    public ExecucaoTreino ExecucaoTreino { get; set; } = null!;
    public Exercicio Exercicio { get; set; } = null!;
}
