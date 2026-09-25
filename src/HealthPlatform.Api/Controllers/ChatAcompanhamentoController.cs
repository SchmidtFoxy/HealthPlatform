using System.Text;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Policy = "AuthenticatedOnly")]
[Route("api/chat")]
public sealed class ChatAcompanhamentoController(
    AppDbContext db,
    CurrentUser currentUser,
    IPushNotificationService push) : ControllerBase
{
    private const string ContextMarkerPrefix = "[[AESYNCTX|";
    private static readonly HashSet<string> ContextosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Geral", "Nutricao", "Treino", "Exames", "Medicamentos", "Recuperacao"
    };

    private static readonly HashSet<string> ReferenciasPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Arquivo", "Treino", "Exercicio", "Refeicao", "Exame"
    };

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("me")]
    public async Task<IActionResult> MinhaConversa(CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var profissional = await ResolverProfissionalDoPaciente(paciente.Id, ct);
        if (profissional is null) return Ok(new { pacienteId = paciente.Id, profissional = (object?)null, mensagens = Array.Empty<object>(), naoLidas = 0 });

        await MarcarConversaComoLida(paciente.Id, profissional.Id, ct);
        var mensagens = await ListarMensagens(paciente.Id, profissional.Id, "Paciente", ct);
        return Ok(new
        {
            pacienteId = paciente.Id,
            profissional = new { profissional.Id, profissional.Nome, profissional.Especialidade },
            prazoResposta = "Resposta em ate 24h uteis",
            emergencia = "Este canal nao deve ser usado para urgencias ou emergencias.",
            naoLidas = 0,
            mensagens
        });
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpPost("me/mensagens")]
    public async Task<IActionResult> EnviarComoPaciente(ChatMensagemRequest request, CancellationToken ct)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var profissional = await ResolverProfissionalDoPaciente(paciente.Id, ct);
        if (profissional is null) return BadRequest(new { message = "Nenhum profissional ativo esta vinculado ao seu acompanhamento." });

        return await CriarMensagem(paciente, profissional, "Paciente", request, profissional.UsuarioId, ct);
    }

    [Authorize]
    [HttpGet("pacientes/{pacienteId:guid}")]
    public async Task<IActionResult> ConversaProfissional(Guid pacienteId, CancellationToken ct)
    {
        var profissional = await MeuProfissional(ct);
        if (profissional is null) return Forbid();

        var paciente = await db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (paciente is null) return NotFound();
        if (!await PodeAcompanharPaciente(profissional.Id, paciente.Id, ct)) return Forbid();

        await MarcarConversaComoLida(paciente.Id, profissional.Id, ct);
        var mensagens = await ListarMensagens(paciente.Id, profissional.Id, "Profissional", ct);
        return Ok(new
        {
            paciente = new { paciente.Id, paciente.Nome },
            profissional = new { profissional.Id, profissional.Nome, profissional.Especialidade },
            prazoResposta = "Resposta em ate 24h uteis",
            emergencia = "Este canal e destinado ao acompanhamento. Urgencias e emergencias devem usar os canais apropriados.",
            naoLidas = 0,
            mensagens
        });
    }

    [Authorize]
    [HttpPost("pacientes/{pacienteId:guid}/mensagens")]
    public async Task<IActionResult> EnviarComoProfissional(Guid pacienteId, ChatMensagemRequest request, CancellationToken ct)
    {
        var profissional = await MeuProfissional(ct);
        if (profissional is null) return Forbid();

        var paciente = await db.Pacientes.FirstOrDefaultAsync(x =>
            x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (paciente is null) return NotFound();
        if (!await PodeAcompanharPaciente(profissional.Id, paciente.Id, ct)) return Forbid();

        return await CriarMensagem(paciente, profissional, "Profissional", request, paciente.UsuarioId, ct);
    }

    [HttpGet("nao-lidas")]
    public async Task<IActionResult> NaoLidas([FromQuery] Guid? pacienteId, CancellationToken ct)
    {
        IQueryable<NotificacaoInterna> query = db.NotificacoesInternas.AsNoTracking().Where(x =>
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.UsuarioId == currentUser.UserId &&
            x.Ativa &&
            !x.LidaEmUtc.HasValue &&
            x.OrigemTipo == "ChatAcompanhamento" &&
            x.OrigemId.HasValue);

        if (pacienteId.HasValue)
        {
            var ids = await db.InteracoesAcompanhamento.AsNoTracking()
                .Where(x => x.OrganizacaoId == currentUser.OrganizationId &&
                    x.PacienteId == pacienteId.Value && x.Canal.StartsWith("Chat:"))
                .Select(x => x.Id)
                .ToListAsync(ct);
            query = query.Where(x => ids.Contains(x.OrigemId!.Value));
        }

        var total = await query.CountAsync(ct);
        return Ok(new { total, possuiNaoLidas = total > 0 });
    }

    private async Task<IActionResult> CriarMensagem(
        Paciente paciente,
        Profissional profissional,
        string autor,
        ChatMensagemRequest request,
        Guid? destinatarioUsuarioId,
        CancellationToken ct)
    {
        var mensagem = (request.Mensagem ?? string.Empty).Trim();
        if (mensagem.Length is < 1 or > 3000)
            return BadRequest(new { message = "A mensagem deve ter entre 1 e 3000 caracteres." });

        var contexto = NormalizarContexto(request.Contexto);
        var referencia = NormalizarReferencia(request);
        var observacoes = MontarObservacoes(mensagem, referencia);
        var agora = DateTime.UtcNow;
        var item = new InteracaoAcompanhamento
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = paciente.Id,
            ProfissionalId = profissional.Id,
            DataHoraUtc = agora,
            Canal = $"Chat:{contexto}",
            Resultado = autor,
            Observacoes = observacoes,
            CreatedAtUtc = agora,
            UpdatedAtUtc = agora
        };
        db.InteracoesAcompanhamento.Add(item);

        if (destinatarioUsuarioId.HasValue)
        {
            db.NotificacoesInternas.Add(new NotificacaoInterna
            {
                OrganizacaoId = currentUser.OrganizationId,
                UsuarioId = destinatarioUsuarioId.Value,
                Tipo = "Chat",
                Prioridade = "Normal",
                Titulo = autor == "Paciente" ? $"Nova mensagem de {paciente.Nome}" : $"Nova mensagem de {profissional.Nome}",
                Mensagem = $"{RotuloContexto(contexto)} • {Resumir(mensagem, 120)}",
                OrigemTipo = "ChatAcompanhamento",
                OrigemId = item.Id,
                OrigemChave = $"CHAT:{item.Id}",
                DataEventoUtc = agora,
                Link = autor == "Paciente" ? "pacientes" : "chat",
                CreatedAtUtc = agora,
                UpdatedAtUtc = agora,
                Ativa = true
            });
        }

        await db.SaveChangesAsync(ct);
        if (destinatarioUsuarioId.HasValue)
        {
            var pushTitulo = autor == "Paciente" ? $"Nova mensagem de {paciente.Nome}" : $"Nova mensagem de {profissional.Nome}";
            await push.EnviarAsync(destinatarioUsuarioId.Value, "mensagem", pushTitulo, $"{RotuloContexto(contexto)} • {Resumir(mensagem, 120)}", autor == "Paciente" ? "pacientes" : "chat", ct);
        }
        return Ok(new
        {
            item.Id,
            item.DataHoraUtc,
            autorTipo = autor,
            contexto,
            contextoRotulo = RotuloContexto(contexto),
            mensagem,
            referencia,
            status = destinatarioUsuarioId.HasValue ? "Entregue" : "Enviada"
        });
    }

    private async Task<object[]> ListarMensagens(Guid pacienteId, Guid profissionalId, string perspectiva, CancellationToken ct)
    {
        var itens = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId &&
                x.ProfissionalId == profissionalId && x.Canal.StartsWith("Chat:"))
            .OrderBy(x => x.DataHoraUtc)
            .Take(300)
            .ToListAsync(ct);

        var ids = itens.Select(x => x.Id).ToArray();
        var notificacoes = ids.Length == 0
            ? new Dictionary<Guid, DateTime?>()
            : await db.NotificacoesInternas.AsNoTracking()
                .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.OrigemTipo == "ChatAcompanhamento" &&
                    x.OrigemId.HasValue && ids.Contains(x.OrigemId.Value))
                .GroupBy(x => x.OrigemId!.Value)
                .Select(g => new { Id = g.Key, LidaEmUtc = g.Max(x => x.LidaEmUtc) })
                .ToDictionaryAsync(x => x.Id, x => x.LidaEmUtc, ct);

        return itens.Select(x =>
        {
            var meu = string.Equals(x.Resultado, perspectiva, StringComparison.OrdinalIgnoreCase);
            var (mensagem, referencia) = SepararObservacoes(x.Observacoes ?? string.Empty);
            notificacoes.TryGetValue(x.Id, out var lidaEmUtc);
            var status = meu ? (lidaEmUtc.HasValue ? "Lida" : notificacoes.ContainsKey(x.Id) ? "Entregue" : "Enviada") : "Recebida";
            return (object)new
            {
                x.Id,
                x.DataHoraUtc,
                autorTipo = x.Resultado,
                meu,
                contexto = ExtrairContexto(x.Canal),
                contextoRotulo = RotuloContexto(ExtrairContexto(x.Canal)),
                mensagem,
                referencia,
                status,
                lidaEmUtc
            };
        }).ToArray();
    }

    private async Task MarcarConversaComoLida(Guid pacienteId, Guid profissionalId, CancellationToken ct)
    {
        var ids = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.PacienteId == pacienteId &&
                x.ProfissionalId == profissionalId && x.Canal.StartsWith("Chat:"))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (ids.Count == 0) return;

        var agora = DateTime.UtcNow;
        var notificacoes = await db.NotificacoesInternas
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.UsuarioId == currentUser.UserId &&
                x.Ativa && !x.LidaEmUtc.HasValue && x.OrigemTipo == "ChatAcompanhamento" &&
                x.OrigemId.HasValue && ids.Contains(x.OrigemId.Value))
            .ToListAsync(ct);

        if (notificacoes.Count == 0) return;
        foreach (var notificacao in notificacoes)
        {
            notificacao.LidaEmUtc = agora;
            notificacao.UpdatedAtUtc = agora;
        }
        await db.SaveChangesAsync(ct);
    }

    private static ChatReferencia? NormalizarReferencia(ChatMensagemRequest request)
    {
        var tipo = (request.ReferenciaTipo ?? string.Empty).Trim();
        if (!ReferenciasPermitidas.Contains(tipo)) return null;
        var titulo = (request.ReferenciaTitulo ?? string.Empty).Trim();
        if (titulo.Length > 160) titulo = titulo[..160];
        return new ChatReferencia(tipo, request.ReferenciaId, titulo);
    }

    private static string MontarObservacoes(string mensagem, ChatReferencia? referencia)
    {
        if (referencia is null) return mensagem;
        var titulo = Convert.ToBase64String(Encoding.UTF8.GetBytes(referencia.Titulo ?? string.Empty));
        return $"{mensagem}\n{ContextMarkerPrefix}{referencia.Tipo}|{referencia.Id?.ToString() ?? string.Empty}|{titulo}]]";
    }

    private static (string Mensagem, ChatReferencia? Referencia) SepararObservacoes(string observacoes)
    {
        var indice = observacoes.LastIndexOf(ContextMarkerPrefix, StringComparison.Ordinal);
        if (indice < 0) return (observacoes, null);
        var fim = observacoes.IndexOf("]]", indice, StringComparison.Ordinal);
        if (fim < 0) return (observacoes, null);
        var payload = observacoes[(indice + ContextMarkerPrefix.Length)..fim];
        var partes = payload.Split('|', 3);
        if (partes.Length != 3 || !ReferenciasPermitidas.Contains(partes[0])) return (observacoes, null);
        Guid? id = Guid.TryParse(partes[1], out var parsed) ? parsed : null;
        string titulo;
        try { titulo = Encoding.UTF8.GetString(Convert.FromBase64String(partes[2])); }
        catch { titulo = string.Empty; }
        return (observacoes[..indice].TrimEnd(), new ChatReferencia(partes[0], id, titulo));
    }

    private async Task<bool> PodeAcompanharPaciente(Guid profissionalId, Guid pacienteId, CancellationToken ct)
    {
        var responsavel = await ResolverProfissionalDoPaciente(pacienteId, ct);
        return responsavel?.Id == profissionalId;
    }

    private async Task<Profissional?> MeuProfissional(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private async Task<Profissional?> ResolverProfissionalDoPaciente(Guid pacienteId, CancellationToken ct)
    {
        var profissionalId = await db.Consultas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => (Guid?)x.ProfissionalId)
            .FirstOrDefaultAsync(ct);

        if (profissionalId.HasValue)
        {
            var vinculado = await db.Profissionais.FirstOrDefaultAsync(x => x.Id == profissionalId.Value && x.Ativo, ct);
            if (vinculado is not null) return vinculado;
        }

        return await db.Profissionais.OrderBy(x => x.Nome).FirstOrDefaultAsync(x =>
            x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    }

    private static string NormalizarContexto(string? valor)
    {
        var contexto = string.IsNullOrWhiteSpace(valor) ? "Geral" : valor.Trim();
        return ContextosPermitidos.Contains(contexto) ? contexto : "Geral";
    }

    private static string ExtrairContexto(string canal)
    {
        var partes = canal.Split(':', 2);
        return partes.Length == 2 ? NormalizarContexto(partes[1]) : "Geral";
    }

    private static string RotuloContexto(string contexto) => contexto switch
    {
        "Nutricao" => "Nutrição",
        "Treino" => "Treino",
        "Exames" => "Exames",
        "Medicamentos" => "Medicamentos e suplementos",
        "Recuperacao" => "Recuperação",
        _ => "Acompanhamento geral"
    };

    private static string Resumir(string valor, int limite) => valor.Length <= limite ? valor : valor[..limite] + "…";
}

public sealed record ChatMensagemRequest(
    string? Mensagem,
    string? Contexto,
    string? ReferenciaTipo = null,
    Guid? ReferenciaId = null,
    string? ReferenciaTitulo = null);

public sealed record ChatReferencia(string Tipo, Guid? Id, string? Titulo);
