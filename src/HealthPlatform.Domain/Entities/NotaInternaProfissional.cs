using HealthPlatform.Domain.Common;

namespace HealthPlatform.Domain.Entities;

public class NotaInternaProfissional : BaseEntity
{
    public Guid OrganizacaoId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid AutorUsuarioId { get; set; }
    public string AutorNome { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Geral";
    public string Conteudo { get; set; } = string.Empty;
    public bool Fixada { get; set; }
    public bool Arquivada { get; set; }

    public Organizacao Organizacao { get; set; } = null!;
    public Paciente Paciente { get; set; } = null!;
}
