using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Api.Services.Push;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record DefinirResponsavelRequest(Guid? ProfissionalId, bool RepassarAvisos = true);

/// <summary>
/// Diagnostico do vinculo paciente-profissional para o admin: quem recebe as mensagens,
/// por que, para quem os avisos foram e se o profissional consegue ser avisado no celular.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/pacientes/{pacienteId:guid}")]
public sealed class AdminVinculoPacienteController(
    AppDbContext db,
    CurrentUser currentUser,
    ProfissionalResponsavelService responsaveis,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("vinculo")]
    public async Task<IActionResult> Vinculo(Guid pacienteId, CancellationToken ct)
    {
        var org = currentUser.OrganizationId;
        var paciente = await db.Pacientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pacienteId && x.OrganizacaoId == org, ct);
        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });

        var responsavel = await responsaveis.ResolverAsync(org, pacienteId, ct);
        var resp = responsavel.Profissional;

        var contaPaciente = paciente.UsuarioId.HasValue
            ? await db.Users.AsNoTracking().Where(x => x.Id == paciente.UsuarioId.Value)
                .Select(x => new { x.Email, x.Ativo, x.NotificarMensagens }).FirstOrDefaultAsync(ct)
            : null;

        var profissionais = await db.Profissionais.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .OrderByDescending(x => x.Ativo).ThenBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, x.Especialidade, Tipo = x.Tipo.ToString(), x.Ativo, x.UsuarioId })
            .ToListAsync(ct);
        var usuarioIds = profissionais.Select(x => x.UsuarioId).ToArray();
        var contas = await db.Users.AsNoTracking()
            .Where(x => usuarioIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome, x.Email, x.Ativo, x.NotificarMensagens })
            .ToDictionaryAsync(x => x.Id, ct);
        var dispositivos = await db.Set<IdentityUserToken<Guid>>().AsNoTracking()
            .Where(x => usuarioIds.Contains(x.UserId) && x.LoginProvider == WebPushNotificationService.LoginProvider)
            .GroupBy(x => x.UserId)
            .Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);

        var ultimaConsulta = await db.Consultas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new { x.DataHoraUtc, Status = x.Status.ToString(), x.ProfissionalId, ProfissionalNome = x.Profissional.Nome })
            .FirstOrDefaultAsync(ct);

        var mensagens = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.PacienteId == pacienteId && x.Canal.StartsWith("Chat:"))
            .Select(x => new { x.Id, x.ProfissionalId, x.Resultado, x.DataHoraUtc })
            .ToListAsync(ct);
        var ultimaDoPaciente = mensagens.Where(x => x.Resultado == "Paciente").Select(x => (DateTime?)x.DataHoraUtc).Max();
        var ultimaDaEquipe = mensagens.Where(x => x.Resultado == "Profissional").Select(x => (DateTime?)x.DataHoraUtc).Max();
        var aguardandoResposta = ultimaDoPaciente.HasValue && (!ultimaDaEquipe.HasValue || ultimaDoPaciente > ultimaDaEquipe);
        var nomes = profissionais.ToDictionary(x => x.Id, x => x.Nome);
        var porProfissional = mensagens.GroupBy(x => x.ProfissionalId)
            .Select(g => new
            {
                profissionalId = g.Key,
                nome = nomes.GetValueOrDefault(g.Key, "Profissional removido"),
                doPaciente = g.Count(x => x.Resultado == "Paciente"),
                daEquipe = g.Count(x => x.Resultado == "Profissional"),
                ultimaEmUtc = g.Max(x => x.DataHoraUtc),
                ehResponsavel = g.Key == resp?.Id
            })
            .OrderByDescending(x => x.ultimaEmUtc)
            .ToArray();

        // Avisos das mensagens que o paciente mandou (os da equipe vao para o proprio paciente).
        var idsDoPaciente = mensagens.Where(x => x.Resultado == "Paciente").Select(x => x.Id).ToArray();
        var avisos = await db.NotificacoesInternas.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.OrigemTipo == "ChatAcompanhamento" && x.OrigemId.HasValue && idsDoPaciente.Contains(x.OrigemId.Value))
            .Select(x => new { x.UsuarioId, x.LidaEmUtc, x.DataEventoUtc, x.Ativa })
            .ToListAsync(ct);
        var avisadosIds = avisos.Select(x => x.UsuarioId).Distinct().ToArray();
        var avisados = await db.Users.AsNoTracking().Where(x => avisadosIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome, x.Email }).ToDictionaryAsync(x => x.Id, ct);
        var avisosPorUsuario = avisos.GroupBy(x => x.UsuarioId)
            .Select(g => new
            {
                usuarioId = g.Key,
                nome = avisados.TryGetValue(g.Key, out var u) ? u.Nome : "Conta removida",
                email = avisados.TryGetValue(g.Key, out var e) ? e.Email : null,
                total = g.Count(),
                lidos = g.Count(x => x.LidaEmUtc.HasValue),
                naoLidos = g.Count(x => x.Ativa && !x.LidaEmUtc.HasValue),
                ultimoEmUtc = g.Max(x => x.DataEventoUtc),
                ehResponsavel = g.Key == resp?.UsuarioId
            })
            .OrderByDescending(x => x.ultimoEmUtc)
            .ToArray();

        var contaResp = resp is not null ? contas.GetValueOrDefault(resp.UsuarioId) : null;
        var dispositivosResp = resp is not null ? dispositivos.GetValueOrDefault(resp.UsuarioId) : 0;

        var problemas = new List<object>();
        void Problema(string nivel, string texto) => problemas.Add(new { nivel, texto });
        if (!paciente.Ativo) Problema("erro", "O paciente está inativo: ele não consegue usar o chat.");
        if (contaPaciente is null) Problema("atencao", "O paciente ainda não tem acesso ao app (convite não ativado).");
        else if (!contaPaciente.Ativo) Problema("erro", "A conta de acesso do paciente está desativada.");
        if (resp is null)
            Problema("erro", "Ninguém recebe as mensagens deste paciente. Defina o profissional responsável.");
        else
        {
            if (responsavel.Origem != OrigemResponsavel.Definido)
                Problema("atencao", $"O responsável foi deduzido ({OrigemResponsavel.Rotulo(responsavel.Origem).ToLowerInvariant()}). Confirme e defina para fixar.");
            if (contaResp is null || !contaResp.Ativo) Problema("erro", $"A conta de acesso de {resp.Nome} está desativada ou não existe: os avisos não chegam.");
            else
            {
                if (!contaResp.NotificarMensagens) Problema("atencao", $"{resp.Nome} desligou os avisos de mensagem no perfil: não recebe push, só vê no app.");
                if (dispositivosResp == 0) Problema("atencao", $"{resp.Nome} não ativou as notificações no celular: só vê as mensagens abrindo o app.");
            }
        }
        var foraDoResponsavel = porProfissional.Where(x => !x.ehResponsavel && x.doPaciente > 0).ToArray();
        if (foraDoResponsavel.Length > 0)
            Problema("atencao", $"{foraDoResponsavel.Sum(x => x.doPaciente)} mensagem(ns) do paciente foram para {string.Join(", ", foraDoResponsavel.Select(x => x.nome))}. O responsável atual vê o histórico inteiro.");
        var naoLidosOutros = avisosPorUsuario.Where(x => !x.ehResponsavel && x.naoLidos > 0).Sum(x => x.naoLidos);
        if (naoLidosOutros > 0)
            Problema("atencao", $"{naoLidosOutros} aviso(s) de mensagem não lidos estão com outra pessoa. Ao definir o responsável, eles podem ser repassados.");
        if (aguardandoResposta && ultimaDoPaciente < DateTime.UtcNow.AddHours(-24))
            Problema("atencao", "O paciente espera resposta há mais de 24 horas.");

        return Ok(new
        {
            paciente = new
            {
                paciente.Id,
                paciente.Nome,
                paciente.Email,
                paciente.Ativo,
                temAcesso = contaPaciente is not null,
                acessoAtivo = contaPaciente?.Ativo ?? false,
                emailAcesso = contaPaciente?.Email
            },
            responsavel = new
            {
                origem = responsavel.Origem,
                origemRotulo = responsavel.OrigemRotulo,
                definidoManualmente = responsavel.Origem == OrigemResponsavel.Definido,
                profissional = resp is null ? null : new
                {
                    resp.Id,
                    resp.Nome,
                    resp.Especialidade,
                    tipo = resp.Tipo.ToString(),
                    email = contaResp?.Email,
                    contaAtiva = contaResp?.Ativo ?? false,
                    notificarMensagens = contaResp?.NotificarMensagens ?? false,
                    dispositivosPush = dispositivosResp
                }
            },
            ultimaConsulta,
            conversa = new
            {
                total = mensagens.Count,
                doPaciente = idsDoPaciente.Length,
                ultimaDoPacienteEmUtc = ultimaDoPaciente,
                ultimaDaEquipeEmUtc = ultimaDaEquipe,
                aguardandoResposta,
                porProfissional
            },
            avisos = avisosPorUsuario,
            problemas,
            profissionais = profissionais.Select(x => new
            {
                x.Id,
                x.Nome,
                x.Especialidade,
                x.Tipo,
                x.Ativo,
                contaAtiva = contas.TryGetValue(x.UsuarioId, out var c) && c.Ativo,
                dispositivosPush = dispositivos.GetValueOrDefault(x.UsuarioId)
            })
        });
    }

    [HttpPut("profissional-responsavel")]
    public async Task<IActionResult> DefinirResponsavel(Guid pacienteId, DefinirResponsavelRequest request, CancellationToken ct)
    {
        var org = currentUser.OrganizationId;
        var paciente = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == pacienteId && x.OrganizacaoId == org, ct);
        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });

        Profissional? novo = null;
        if (request.ProfissionalId.HasValue)
        {
            novo = await db.Profissionais.AsNoTracking().FirstOrDefaultAsync(x =>
                x.Id == request.ProfissionalId.Value && x.OrganizacaoId == org && x.Ativo, ct);
            if (novo is null) return BadRequest(new { message = "Escolha um profissional ativo da clinica." });
        }

        var anterior = await responsaveis.ResolverAsync(org, pacienteId, ct);
        var antes = new { paciente.ProfissionalResponsavelId, efetivo = anterior.Profissional?.Id, anterior.Origem };
        var agora = DateTime.UtcNow;
        paciente.ProfissionalResponsavelId = novo?.Id;
        paciente.UpdatedAtUtc = agora;

        var repassados = 0;
        if (novo is not null && request.RepassarAvisos)
            repassados = await RepassarAvisosNaoLidos(paciente, novo.UsuarioId, agora, ct);

        // O aviso de "paciente sem responsavel" deixa de valer quando alguem assume.
        if (novo is not null)
        {
            var chave = $"CHAT-SEM-RESPONSAVEL:{paciente.Id}";
            var avisosSemResponsavel = await db.NotificacoesInternas
                .Where(x => x.OrganizacaoId == org && x.OrigemChave == chave && !x.LidaEmUtc.HasValue)
                .ToListAsync(ct);
            foreach (var aviso in avisosSemResponsavel)
            {
                aviso.LidaEmUtc = agora;
                aviso.UpdatedAtUtc = agora;
            }
        }

        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = org,
            UsuarioId = currentUser.UserId,
            Acao = novo is null ? "RemoverProfissionalResponsavel" : "DefinirProfissionalResponsavel",
            Entidade = "Paciente",
            EntidadeId = paciente.Id.ToString(),
            DadosAnterioresJson = JsonSerializer.Serialize(antes),
            DadosNovosJson = JsonSerializer.Serialize(new { ProfissionalResponsavelId = novo?.Id, avisosRepassados = repassados }),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync(ct);

        return await Vinculo(pacienteId, ct) is OkObjectResult ok
            ? Ok(new { avisosRepassados = repassados, vinculo = ok.Value })
            : Ok(new { avisosRepassados = repassados });
    }

    /// <summary>Avisos ainda nao lidos das mensagens do paciente passam para o novo responsavel.</summary>
    private async Task<int> RepassarAvisosNaoLidos(Paciente paciente, Guid novoUsuarioId, DateTime agora, CancellationToken ct)
    {
        var org = currentUser.OrganizationId;
        var idsDoPaciente = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.PacienteId == paciente.Id && x.Canal.StartsWith("Chat:") && x.Resultado == "Paciente")
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (idsDoPaciente.Count == 0) return 0;

        var avisos = await db.NotificacoesInternas
            .Where(x => x.OrganizacaoId == org && x.OrigemTipo == "ChatAcompanhamento" && x.OrigemId.HasValue &&
                idsDoPaciente.Contains(x.OrigemId.Value) && x.UsuarioId != novoUsuarioId &&
                (!paciente.UsuarioId.HasValue || x.UsuarioId != paciente.UsuarioId.Value) &&
                x.Ativa && !x.LidaEmUtc.HasValue)
            .ToListAsync(ct);
        var jaTem = (await db.NotificacoesInternas.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.UsuarioId == novoUsuarioId && x.OrigemTipo == "ChatAcompanhamento" &&
                x.OrigemId.HasValue && idsDoPaciente.Contains(x.OrigemId.Value))
            .Select(x => x.OrigemChave)
            .ToListAsync(ct)).ToHashSet();

        var repassados = 0;
        foreach (var aviso in avisos)
        {
            if (jaTem.Add(aviso.OrigemChave))
            {
                aviso.UsuarioId = novoUsuarioId;
                repassados++;
            }
            else
            {
                aviso.LidaEmUtc = agora;
            }
            aviso.UpdatedAtUtc = agora;
        }
        return repassados;
    }
}
