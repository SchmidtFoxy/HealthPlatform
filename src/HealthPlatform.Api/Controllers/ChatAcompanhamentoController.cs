using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
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
    CurrentUser currentUser) : ControllerBase
{
    private static readonly HashSet<string> ContextosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Geral", "Nutricao", "Treino", "Exames", "Medicamentos", "Recuperacao"
    };

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("me")]
    public async Task<IActionResult> MinhaConversa(CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x =>
            x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (paciente is null) return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var profissional = await ResolverProfissionalDoPaciente(paciente.Id, ct);
        if (profissional is null) return Ok(new { pacienteId = paciente.Id, profissional = (object?)null, mensagens = Array.Empty<object>() });

        var mensagens = await ListarMensagens(paciente.Id, profissional.Id, "Paciente", ct);
        return Ok(new
        {
            pacienteId = paciente.Id,
            profissional = new { profissional.Id, profissional.Nome, profissional.Especialidade },
            prazoResposta = "Resposta em ate 24h uteis",
            emergencia = "Este canal nao deve ser usado para urgencias ou emergencias.",
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

        var mensagens = await ListarMensagens(paciente.Id, profissional.Id, "Profissional", ct);
        return Ok(new
        {
            paciente = new { paciente.Id, paciente.Nome },
            profissional = new { profissional.Id, profissional.Nome, profissional.Especialidade },
            prazoResposta = "Resposta em ate 24h uteis",
            emergencia = "Este canal e destinado ao acompanhamento. Urgencias e emergencias devem usar os canais apropriados.",
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
        var agora = DateTime.UtcNow;
        var item = new InteracaoAcompanhamento
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = paciente.Id,
            ProfissionalId = profissional.Id,
            DataHoraUtc = agora,
            Canal = $"Chat:{contexto}",
            Resultado = autor,
            Observacoes = mensagem,
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
        return Ok(new
        {
            item.Id,
            item.DataHoraUtc,
            autorTipo = autor,
            contexto,
            contextoRotulo = RotuloContexto(contexto),
            mensagem
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

        return itens.Select(x => (object)new
        {
            x.Id,
            x.DataHoraUtc,
            autorTipo = x.Resultado,
            meu = string.Equals(x.Resultado, perspectiva, StringComparison.OrdinalIgnoreCase),
            contexto = ExtrairContexto(x.Canal),
            contextoRotulo = RotuloContexto(ExtrairContexto(x.Canal)),
            mensagem = x.Observacoes ?? string.Empty
        }).ToArray();
    }


    private async Task<bool> PodeAcompanharPaciente(Guid profissionalId, Guid pacienteId, CancellationToken ct)
    {
        // O profissional do chat e o mesmo resolvido para o paciente. Isso impede que
        // outro profissional da mesma organizacao use apenas o GUID para ler a conversa.
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

public sealed record ChatMensagemRequest(string? Mensagem, string? Contexto);
