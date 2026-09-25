using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificacoes")]
public sealed class NotificacoesController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] bool sincronizar = true,
        [FromQuery] bool incluirInativas = false,
        [FromQuery] bool? lida = null,
        [FromQuery] string? prioridade = null,
        [FromQuery] string? tipo = null,
        [FromQuery] int limite = 50,
        CancellationToken ct = default)
    {
        limite = Math.Clamp(limite, 1, 200);

        if (sincronizar)
            await SincronizarInterno(ct);

        var baseQuery = db.NotificacoesInternas.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.UsuarioId == currentUser.UserId);

        if (!incluirInativas)
            baseQuery = baseQuery.Where(x => x.Ativa);

        var resumoQuery = baseQuery;

        if (lida.HasValue)
            baseQuery = lida.Value
                ? baseQuery.Where(x => x.LidaEmUtc.HasValue)
                : baseQuery.Where(x => !x.LidaEmUtc.HasValue);

        prioridade = NormalizarFiltro(prioridade);
        if (prioridade is not null)
            baseQuery = baseQuery.Where(x => x.Prioridade == prioridade);

        tipo = NormalizarFiltro(tipo);
        if (tipo is not null)
            baseQuery = baseQuery.Where(x => x.Tipo == tipo);

        var tiposDisponiveis = await resumoQuery
            .Select(x => x.Tipo)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        var resumo = await resumoQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                total = g.Count(),
                naoLidas = g.Count(x => !x.LidaEmUtc.HasValue),
                altas = g.Count(x => x.Prioridade == "Alta"),
                medias = g.Count(x => x.Prioridade == "Media"),
                encerradas = g.Count(x => !x.Ativa)
            })
            .FirstOrDefaultAsync(ct);

        var itens = await baseQuery
            .OrderBy(x => x.LidaEmUtc.HasValue)
            .ThenByDescending(x => x.Prioridade == "Alta")
            .ThenByDescending(x => x.Prioridade == "Media")
            .ThenByDescending(x => x.DataEventoUtc ?? x.CreatedAtUtc)
            .Take(limite)
            .Select(x => new
            {
                x.Id,
                x.Tipo,
                x.Prioridade,
                x.Titulo,
                x.Mensagem,
                x.OrigemTipo,
                x.OrigemId,
                x.OrigemChave,
                x.DataEventoUtc,
                x.Link,
                x.LidaEmUtc,
                lida = x.LidaEmUtc.HasValue,
                x.Ativa,
                x.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(new
        {
            total = itens.Count,
            naoLidas = resumo?.naoLidas ?? 0,
            resumo = new
            {
                total = resumo?.total ?? 0,
                naoLidas = resumo?.naoLidas ?? 0,
                altas = resumo?.altas ?? 0,
                medias = resumo?.medias ?? 0,
                encerradas = resumo?.encerradas ?? 0
            },
            tiposDisponiveis,
            filtros = new { incluirInativas, lida, prioridade, tipo, limite },
            itens
        });
    }

    private static string? NormalizarFiltro(string? valor)
    {
        var normalizado = (valor ?? string.Empty).Trim();
        return normalizado.Length == 0 ? null : normalizado;
    }

    [HttpPost("sincronizar")]
    public async Task<IActionResult> Sincronizar(CancellationToken ct)
    {
        var geradas = await SincronizarInterno(ct);
        return Ok(new { sincronizadoEmUtc = DateTime.UtcNow, geradasOuAtualizadas = geradas });
    }

    [HttpPut("{id:guid}/lida")]
    public async Task<IActionResult> MarcarLida(Guid id, CancellationToken ct)
    {
        var item = await db.NotificacoesInternas.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.UsuarioId == currentUser.UserId, ct);

        if (item is null) return NotFound();

        item.LidaEmUtc ??= DateTime.UtcNow;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("{id:guid}/nao-lida")]
    public async Task<IActionResult> MarcarNaoLida(Guid id, CancellationToken ct)
    {
        var item = await db.NotificacoesInternas.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.UsuarioId == currentUser.UserId, ct);

        if (item is null) return NotFound();

        item.LidaEmUtc = null;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("ler-todas")]
    public async Task<IActionResult> LerTodas(CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var itens = await db.NotificacoesInternas
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.UsuarioId == currentUser.UserId &&
                x.Ativa &&
                !x.LidaEmUtc.HasValue)
            .ToListAsync(ct);

        foreach (var item in itens)
        {
            item.LidaEmUtc = agora;
            item.UpdatedAtUtc = agora;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { marcadas = itens.Count });
    }

    private async Task<int> SincronizarInterno(CancellationToken ct)
    {
        var agora = DateTime.UtcNow;
        var validas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var geradas = 0;

        var usuario = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (usuario is null)
            return 0;

        if (usuario.TipoUsuario == TipoUsuario.Paciente)
        {
            geradas += await SincronizarPaciente(validas, agora, ct);
        }
        else
        {
            geradas += await SincronizarProfissional(validas, agora, ct);
        }

        var antigas = await db.NotificacoesInternas
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.UsuarioId == currentUser.UserId &&
                x.Ativa &&
                (x.OrigemTipo == "Consulta" || x.OrigemTipo == "PendenciaClinica" || x.OrigemTipo == "InteracaoAcompanhamento" || x.OrigemTipo == "SolicitacaoClinica"))
            .ToListAsync(ct);

        foreach (var antiga in antigas)
        {
            if (validas.Contains(antiga.OrigemChave)) continue;
            antiga.Ativa = false;
            antiga.UpdatedAtUtc = agora;
        }

        await db.SaveChangesAsync(ct);
        return geradas;
    }

    private async Task<int> SincronizarProfissional(
        HashSet<string> validas,
        DateTime agora,
        CancellationToken ct)
    {
        var profissional = await db.Profissionais.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (profissional is null)
            return 0;

        var count = 0;
        var limiteConsulta = agora.AddHours(24);

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x =>
                x.ProfissionalId == profissional.Id &&
                x.DataHoraUtc >= agora &&
                x.DataHoraUtc <= limiteConsulta &&
                x.Status != StatusConsulta.Cancelada &&
                x.Status != StatusConsulta.Faltou &&
                x.Status != StatusConsulta.Realizada)
            .OrderBy(x => x.DataHoraUtc)
            .Select(x => new
            {
                x.Id,
                x.DataHoraUtc,
                PacienteNome = x.Paciente.Nome,
                x.Motivo
            })
            .ToListAsync(ct);

        foreach (var c in consultas)
        {
            var chave = $"PROF:CONSULTA:{c.Id}";
            validas.Add(chave);

            var horas = (c.DataHoraUtc - agora).TotalHours;
            var prioridade = horas <= 2 ? "Alta" : horas <= 8 ? "Media" : "Normal";
            var titulo = horas <= 2
                ? $"Consulta em breve: {c.PacienteNome}"
                : $"Consulta nas próximas 24h: {c.PacienteNome}";

            count += await Upsert(
                chave,
                "Agenda",
                prioridade,
                titulo,
                $"Consulta próxima • {c.Motivo ?? "Atendimento"}",
                "Consulta",
                c.Id,
                c.DataHoraUtc,
                "agenda",
                ct);
        }

        var pendencias = await db.PendenciasClinicas.AsNoTracking()
            .Where(x =>
                x.ProfissionalId == profissional.Id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || !x.AdiadaAteUtc.HasValue || x.AdiadaAteUtc <= agora))
            .OrderByDescending(x => x.Severidade == "Alta")
            .ThenBy(x => x.VencimentoUtc ?? DateTime.MaxValue)
            .Select(x => new
            {
                x.Id,
                x.PacienteId,
                PacienteNome = x.Paciente.Nome,
                x.Titulo,
                x.Severidade,
                x.Status,
                x.VencimentoUtc
            })
            .ToListAsync(ct);

        foreach (var p in pendencias)
        {
            var vencida = p.VencimentoUtc.HasValue && p.VencimentoUtc.Value < agora;
            var venceLogo = p.VencimentoUtc.HasValue &&
                            p.VencimentoUtc.Value >= agora &&
                            p.VencimentoUtc.Value <= agora.AddHours(24);

            if (!vencida && !venceLogo && p.Severidade != "Alta")
                continue;

            var chave = $"PROF:PENDENCIA:{p.Id}";
            validas.Add(chave);

            var titulo = vencida
                ? $"Pendência vencida: {p.PacienteNome}"
                : p.Severidade == "Alta"
                    ? $"Pendência de alta prioridade: {p.PacienteNome}"
                    : $"Pendência vence em breve: {p.PacienteNome}";

            var mensagem = p.VencimentoUtc.HasValue
                ? $"{p.Titulo} • prazo registrado; confira o horário local na pendência."
                : p.Titulo;

            count += await Upsert(
                chave,
                "Pendencia",
                vencida || p.Severidade == "Alta" ? "Alta" : "Media",
                titulo,
                mensagem,
                "PendenciaClinica",
                p.Id,
                p.VencimentoUtc,
                "pendencias",
                ct);
        }

        var contatos = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x =>
                x.ProfissionalId == profissional.Id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.ProximoContatoUtc.HasValue &&
                x.ProximoContatoUtc.Value <= agora.AddHours(24))
            .OrderBy(x => x.ProximoContatoUtc)
            .Select(x => new
            {
                x.Id,
                x.PacienteId,
                PacienteNome = x.Paciente.Nome,
                x.ProximoContatoUtc,
                x.Resultado
            })
            .ToListAsync(ct);

        var ultimosPorPaciente = contatos
            .GroupBy(x => x.PacienteId)
            .Select(g => g.OrderByDescending(x => x.Id).First())
            .ToList();

        foreach (var f in ultimosPorPaciente)
        {
            if (!f.ProximoContatoUtc.HasValue) continue;

            var chave = $"PROF:FOLLOWUP:{f.PacienteId}";
            validas.Add(chave);

            var vencido = f.ProximoContatoUtc.Value < agora;
            var titulo = vencido
                ? $"Follow-up vencido: {f.PacienteNome}"
                : $"Follow-up previsto: {f.PacienteNome}";

            var mensagem = $"Follow-up • {f.Resultado}";

            count += await Upsert(
                chave,
                "FollowUp",
                vencido ? "Alta" : "Media",
                titulo,
                mensagem,
                "InteracaoAcompanhamento",
                f.Id,
                f.ProximoContatoUtc,
                "followups",
                ct);
        }


        var solicitacoesRespondidas = await db.SolicitacoesClinicas.AsNoTracking()
            .Where(x =>
                x.ProfissionalId == profissional.Id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Status == "Enviada")
            .OrderByDescending(x => x.RespondidaEmUtc ?? x.UpdatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.PacienteId,
                PacienteNome = x.Paciente.Nome,
                x.Titulo,
                x.Tipo,
                x.RespondidaEmUtc
            })
            .ToListAsync(ct);

        foreach (var r in solicitacoesRespondidas)
        {
            var chave = $"PROF:SOLICITACAO:{r.Id}";
            validas.Add(chave);

            count += await Upsert(
                chave,
                "Solicitacao",
                "Media",
                $"Resposta recebida: {r.PacienteNome}",
                $"{r.Tipo} • {r.Titulo}",
                "SolicitacaoClinica",
                r.Id,
                r.RespondidaEmUtc,
                $"paciente:{r.PacienteId}",
                ct);
        }

        return count;
    }

    private async Task<int> SincronizarPaciente(
        HashSet<string> validas,
        DateTime agora,
        CancellationToken ct)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo, ct);

        if (paciente is null)
            return 0;

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x =>
                x.PacienteId == paciente.Id &&
                x.DataHoraUtc >= agora &&
                x.DataHoraUtc <= agora.AddHours(24) &&
                x.Status != StatusConsulta.Cancelada &&
                x.Status != StatusConsulta.Faltou &&
                x.Status != StatusConsulta.Realizada)
            .OrderBy(x => x.DataHoraUtc)
            .Select(x => new
            {
                x.Id,
                x.DataHoraUtc,
                ProfissionalNome = x.Profissional.Nome,
                x.Motivo
            })
            .ToListAsync(ct);

        var count = 0;

        foreach (var c in consultas)
        {
            var chave = $"PAC:CONSULTA:{c.Id}";
            validas.Add(chave);

            var horas = (c.DataHoraUtc - agora).TotalHours;
            var prioridade = horas <= 2 ? "Alta" : "Normal";

            count += await Upsert(
                chave,
                "Consulta",
                prioridade,
                horas <= 2 ? "Sua consulta é em breve" : "Lembrete de consulta",
                $"Consulta próxima • {c.ProfissionalNome}",
                "Consulta",
                c.Id,
                c.DataHoraUtc,
                "inicio",
                ct);
        }


        var solicitacoesPendentes = await db.SolicitacoesClinicas.AsNoTracking()
            .Where(x =>
                x.PacienteId == paciente.Id &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Status == "Pendente")
            .OrderBy(x => x.DataLimiteUtc ?? DateTime.MaxValue)
            .Select(x => new
            {
                x.Id,
                x.Tipo,
                x.Titulo,
                x.DataLimiteUtc,
                ProfissionalNome = x.Profissional.Nome
            })
            .ToListAsync(ct);

        foreach (var r in solicitacoesPendentes)
        {
            var chave = $"PAC:SOLICITACAO:{r.Id}";
            validas.Add(chave);

            var vencida = r.DataLimiteUtc.HasValue && r.DataLimiteUtc.Value < agora;
            var venceEm24h = r.DataLimiteUtc.HasValue &&
                             r.DataLimiteUtc.Value >= agora &&
                             r.DataLimiteUtc.Value <= agora.AddHours(24);
            var prioridade = vencida ? "Alta" : venceEm24h ? "Media" : "Normal";
            var titulo = vencida
                ? $"Solicitação atrasada: {r.Titulo}"
                : venceEm24h
                    ? $"Solicitação vence em breve: {r.Titulo}"
                    : $"Nova solicitação: {r.Titulo}";

            count += await Upsert(
                chave,
                "Solicitacao",
                prioridade,
                titulo,
                $"{r.Tipo} • solicitado por {r.ProfissionalNome}",
                "SolicitacaoClinica",
                r.Id,
                r.DataLimiteUtc,
                "solicitacoes",
                ct);
        }

        return count;
    }

    private async Task<int> Upsert(
        string chave,
        string tipo,
        string prioridade,
        string titulo,
        string mensagem,
        string origemTipo,
        Guid origemId,
        DateTime? dataEventoUtc,
        string link,
        CancellationToken ct)
    {
        var item = await db.NotificacoesInternas.FirstOrDefaultAsync(x =>
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.UsuarioId == currentUser.UserId &&
            x.OrigemChave == chave, ct);

        if (item is null)
        {
            item = new NotificacaoInterna
            {
                OrganizacaoId = currentUser.OrganizationId,
                UsuarioId = currentUser.UserId,
                OrigemChave = chave
            };
            db.NotificacoesInternas.Add(item);
        }

        var mudou = item.Titulo != titulo ||
                    item.Mensagem != mensagem ||
                    item.Prioridade != prioridade ||
                    !item.Ativa;

        item.Tipo = tipo;
        item.Prioridade = prioridade;
        item.Titulo = titulo;
        item.Mensagem = mensagem;
        item.OrigemTipo = origemTipo;
        item.OrigemId = origemId;
        item.DataEventoUtc = dataEventoUtc;
        item.Link = link;
        item.Ativa = true;
        item.UpdatedAtUtc = DateTime.UtcNow;

        if (mudou && item.LidaEmUtc.HasValue)
            item.LidaEmUtc = null;

        return mudou ? 1 : 0;
    }
}
