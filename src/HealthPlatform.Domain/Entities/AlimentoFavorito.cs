using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

/// <summary>Alimento que o profissional costuma usar numa refeicao (ex.: cafe da manha). Base do "montar pela meta".</summary>
public class AlimentoFavorito : BaseEntity
{
    public Guid ProfissionalId { get; set; }
    /// <summary>Nome da refeicao normalizado (maiusculas, sem acento), ex.: "CAFE DA MANHA".</summary>
    public string Refeicao { get; set; } = string.Empty;
    public Guid AlimentoId { get; set; }

    public Profissional Profissional { get; set; } = null!;
    public Alimento Alimento { get; set; } = null!;
}
