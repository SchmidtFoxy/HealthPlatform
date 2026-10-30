using System.Text;

namespace HealthPlatform.Api.Services;

public sealed record ChatReferencia(string Tipo, Guid? Id, string? Titulo);

/// <summary>
/// A mensagem do chat fica no campo Observacoes da interacao, com marcadores no fim:
/// "texto\n[[AESYNRESP|id]]\n[[AESYNCTX|tipo|id|titulo-base64]]". Sem coluna nova no banco.
/// </summary>
public static class ChatMarcadores
{
    private const string ReferenciaPrefixo = "[[AESYNCTX|";
    private const string RespostaPrefixo = "[[AESYNRESP|";

    public static readonly HashSet<string> ReferenciasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Arquivo", "Treino", "Exercicio", "Refeicao", "Exame"
    };

    public static string Montar(string mensagem, ChatReferencia? referencia, Guid? respostaAId)
    {
        var sb = new StringBuilder(mensagem);
        if (respostaAId.HasValue) sb.Append('\n').Append(RespostaPrefixo).Append(respostaAId.Value).Append("]]");
        if (referencia is not null)
        {
            var titulo = Convert.ToBase64String(Encoding.UTF8.GetBytes(referencia.Titulo ?? string.Empty));
            sb.Append('\n').Append(ReferenciaPrefixo).Append(referencia.Tipo).Append('|')
                .Append(referencia.Id?.ToString() ?? string.Empty).Append('|').Append(titulo).Append("]]");
        }
        return sb.ToString();
    }

    public static (string Mensagem, ChatReferencia? Referencia, Guid? RespostaAId) Separar(string? observacoes)
    {
        var texto = observacoes ?? string.Empty;
        ChatReferencia? referencia = null;
        Guid? respostaAId = null;

        var indice = texto.LastIndexOf(ReferenciaPrefixo, StringComparison.Ordinal);
        var fim = indice < 0 ? -1 : texto.IndexOf("]]", indice, StringComparison.Ordinal);
        if (fim > indice)
        {
            var partes = texto[(indice + ReferenciaPrefixo.Length)..fim].Split('|', 3);
            if (partes.Length == 3 && ReferenciasPermitidas.Contains(partes[0]))
            {
                Guid? id = Guid.TryParse(partes[1], out var parsed) ? parsed : null;
                string titulo;
                try { titulo = Encoding.UTF8.GetString(Convert.FromBase64String(partes[2])); }
                catch { titulo = string.Empty; }
                referencia = new ChatReferencia(partes[0], id, titulo);
                texto = texto[..indice].TrimEnd();
            }
        }

        indice = texto.LastIndexOf(RespostaPrefixo, StringComparison.Ordinal);
        fim = indice < 0 ? -1 : texto.IndexOf("]]", indice, StringComparison.Ordinal);
        if (fim > indice && Guid.TryParse(texto[(indice + RespostaPrefixo.Length)..fim], out var resposta))
        {
            respostaAId = resposta;
            texto = texto[..indice].TrimEnd();
        }

        return (texto, referencia, respostaAId);
    }

    /// <summary>Texto curto para listas e citacoes: foto vira "📷 Foto", arquivo vira o nome.</summary>
    public static string Resumo(string mensagem, ChatReferencia? referencia, int limite)
    {
        var texto = mensagem;
        if (referencia?.Tipo == "Arquivo" && (mensagem == "Foto" || mensagem.StartsWith("Enviei o arquivo", StringComparison.Ordinal)))
            texto = mensagem == "Foto" ? "📷 Foto" : $"📎 {referencia.Titulo}";
        texto = texto.Replace('\n', ' ');
        return texto.Length <= limite ? texto : texto[..limite] + "…";
    }
}
