using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record AdminContagemResponse(string Rotulo, int Valor);

public sealed record AdminAtencaoResponse(
    string Tipo,
    string Titulo,
    string Detalhe,
    Guid? PacienteId,
    Guid? UsuarioId);

public sealed record AdminVisaoGeralResponse(
    int Dias,
    int EquipeAtiva,
    int EquipeInativa,
    IReadOnlyCollection<AdminContagemResponse> EquipePorFuncao,
    int PacientesAtivos,
    int PacientesNovos,
    int PacientesComAcesso,
    int ConvitesPendentes,
    int PacientesSemAcesso,
    int PacientesSemProfissional,
    int PessoasQueAcessaram,
    int ConsultasPeriodo,
    int ConsultasRealizadas,
    int PendenciasAbertas,
    int AcoesRegistradas,
    IReadOnlyCollection<AdminAtencaoResponse> Atencao);

public sealed record AdminPacienteResponse(
    Guid Id,
    string Nome,
    string? Email,
    bool Ativo,
    string StatusAcompanhamento,
    string Acesso,
    DateTime? UltimoAcessoUtc,
    string? Profissional,
    DateTime CreatedAtUtc);

public sealed record AdminPacientesResponse(
    IReadOnlyCollection<AdminPacienteResponse> Itens,
    int Total,
    int Pagina,
    int TotalPaginas);

public sealed record AdminAtividadeResponse(
    Guid Id,
    DateTime QuandoUtc,
    Guid? UsuarioId,
    string? Usuario,
    string? FuncaoUsuario,
    string Acao,
    string Entidade,
    string? EntidadeId,
    string? Alvo);

public sealed record AdminAtividadePaginaResponse(
    IReadOnlyCollection<AdminAtividadeResponse> Itens,
    bool TemMais);

public sealed record AdminAcessoEquipeResponse(Guid UsuarioId, DateTime? UltimoAcessoUtc);

/// <summary>
/// Painel do administrador: visao da plataforma (equipe, pacientes, acessos e auditoria),
/// sem entrar em dados clinicos dos pacientes.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    /// <summary>Acoes de login e uso que nao entram na lista de atividade.</summary>
    internal static readonly string[] AcoesDeAcesso = ["LOGIN", "ACCESS"];

    private const int DiasSemAcessoEquipe = 30;

    [HttpGet("visao-geral")]
    public async Task<ActionResult<AdminVisaoGeralResponse>> VisaoGeral(
        [FromQuery] int dias = 30,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        dias = Math.Clamp(dias, 7, 365);
        var agora = DateTime.UtcNow;
        var inicio = agora.AddDays(-dias);
        var org = currentUser.OrganizationId;

        var equipe = await db.Users.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.TipoUsuario != TipoUsuario.Paciente)
            .Select(x => new { x.Id, x.Nome, x.TipoUsuario, x.Ativo, x.CreatedAtUtc })
            .ToListAsync(ct);

        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.Ativo)
            .Select(x => new { x.Id, x.Nome, x.UsuarioId, x.CreatedAtUtc })
            .ToListAsync(ct);

        var usuariosPacientes = await db.Users.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.TipoUsuario == TipoUsuario.Paciente)
            .Select(x => new { x.Id, x.Ativo, x.EmailConfirmed })
            .ToDictionaryAsync(x => x.Id, ct);

        var ultimosAcessos = await UltimosAcessosAsync(org, ct);
        var comProfissional = await PacientesComProfissionalAsync(org, ct);

        string Acesso(Guid? usuarioId)
        {
            if (!usuarioId.HasValue || !usuariosPacientes.TryGetValue(usuarioId.Value, out var u)) return "sem-acesso";
            if (u.Ativo) return "ativo";
            return u.EmailConfirmed ? "bloqueado" : "convite";
        }

        var acessoPorPaciente = pacientes.ToDictionary(x => x.Id, x => Acesso(x.UsuarioId));

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => x.Paciente.OrganizacaoId == org && x.DataHoraUtc >= inicio && x.DataHoraUtc <= agora)
            .Select(x => x.Status)
            .ToListAsync(ct);

        var pendenciasAbertas = await db.PendenciasClinicas.AsNoTracking()
            .CountAsync(x =>
                x.OrganizacaoId == org &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" || x.AdiadaAteUtc == null || x.AdiadaAteUtc <= agora), ct);

        var acoes = await db.AuditLogs.AsNoTracking()
            .CountAsync(x => x.OrganizacaoId == org && x.CreatedAtUtc >= inicio && !AcoesDeAcesso.Contains(x.Acao), ct);

        var pessoasQueAcessaram = ultimosAcessos.Count(x => x.Value >= inicio);

        var atencao = new List<AdminAtencaoResponse>();

        foreach (var p in pacientes.Where(x => acessoPorPaciente[x.Id] == "convite").OrderBy(x => x.CreatedAtUtc).Take(8))
            atencao.Add(new AdminAtencaoResponse("convite", p.Nome, "Convite enviado e ainda não ativado", p.Id, p.UsuarioId));

        foreach (var p in pacientes.Where(x => !comProfissional.Contains(x.Id)).OrderBy(x => x.CreatedAtUtc).Take(8))
            atencao.Add(new AdminAtencaoResponse("sem-profissional", p.Nome, "Sem consulta nem plano com algum profissional", p.Id, p.UsuarioId));

        // O registro de acessos comecou com o painel do admin: antes disso, ninguem conta como sumido.
        var inicioRegistro = ultimosAcessos.Count == 0
            ? agora
            : await db.AuditLogs.AsNoTracking()
                .Where(x => x.OrganizacaoId == org && AcoesDeAcesso.Contains(x.Acao))
                .MinAsync(x => x.CreatedAtUtc, ct);

        foreach (var m in equipe.Where(x => x.Ativo && x.Id != currentUser.UserId).OrderBy(x => x.Nome))
        {
            ultimosAcessos.TryGetValue(m.Id, out var ultimo);
            var referencia = ultimo ?? (m.CreatedAtUtc > inicioRegistro ? m.CreatedAtUtc : inicioRegistro);
            if (referencia >= agora.AddDays(-DiasSemAcessoEquipe)) continue;
            atencao.Add(new AdminAtencaoResponse(
                "equipe-sem-acesso",
                m.Nome,
                ultimo.HasValue ? $"Sem entrar há {(int)(agora - ultimo.Value).TotalDays} dias" : $"Sem acesso registrado há mais de {DiasSemAcessoEquipe} dias",
                null,
                m.Id));
        }

        return Ok(new AdminVisaoGeralResponse(
            dias,
            equipe.Count(x => x.Ativo),
            equipe.Count(x => !x.Ativo),
            equipe.Where(x => x.Ativo)
                .GroupBy(x => x.TipoUsuario)
                .OrderBy(x => x.Key)
                .Select(x => new AdminContagemResponse(x.Key.ToString(), x.Count()))
                .ToList(),
            pacientes.Count,
            pacientes.Count(x => x.CreatedAtUtc >= inicio),
            acessoPorPaciente.Values.Count(x => x == "ativo"),
            acessoPorPaciente.Values.Count(x => x == "convite"),
            acessoPorPaciente.Values.Count(x => x == "sem-acesso"),
            pacientes.Count(x => !comProfissional.Contains(x.Id)),
            pessoasQueAcessaram,
            consultas.Count,
            consultas.Count(x => x == StatusConsulta.Realizada),
            pendenciasAbertas,
            acoes,
            atencao));
    }

    [HttpGet("pacientes")]
    public async Task<ActionResult<AdminPacientesResponse>> Pacientes(
        [FromQuery] string? busca = null,
        [FromQuery] string? acesso = null,
        [FromQuery] bool incluirInativos = false,
        [FromQuery] int pagina = 1,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        const int tamanho = 30;
        pagina = Math.Max(1, pagina);
        var org = currentUser.OrganizationId;

        var query = db.Pacientes.AsNoTracking().Where(x => x.OrganizacaoId == org);
        if (!incluirInativos) query = query.Where(x => x.Ativo);
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Nome, termo) || (x.Email != null && EF.Functions.ILike(x.Email, termo)));
        }

        var linhas = await (
                from p in query
                join u in db.Users.AsNoTracking() on p.UsuarioId equals (Guid?)u.Id into us
                from u in us.DefaultIfEmpty()
                select new
                {
                    p.Id,
                    p.Nome,
                    p.Email,
                    p.Ativo,
                    p.StatusAcompanhamento,
                    p.UsuarioId,
                    p.CreatedAtUtc,
                    UsuarioAtivo = (bool?)u.Ativo,
                    EmailConfirmado = (bool?)u.EmailConfirmed
                })
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        static string Acesso(bool? ativo, bool? confirmado) =>
            ativo is null ? "sem-acesso" : ativo.Value ? "ativo" : confirmado == true ? "bloqueado" : "convite";

        var filtradas = linhas
            .Select(x => new { Linha = x, Acesso = Acesso(x.UsuarioAtivo, x.EmailConfirmado) })
            .Where(x => string.IsNullOrWhiteSpace(acesso) || x.Acesso == acesso)
            .ToList();

        var total = filtradas.Count;
        var pagina_ = filtradas.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
        var ids = pagina_.Select(x => x.Linha.Id).ToArray();
        var usuarioIds = pagina_.Where(x => x.Linha.UsuarioId.HasValue).Select(x => x.Linha.UsuarioId!.Value).ToArray();

        var ultimosAcessos = await db.AuditLogs.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.UsuarioId != null && usuarioIds.Contains(x.UsuarioId.Value) && AcoesDeAcesso.Contains(x.Acao))
            .GroupBy(x => x.UsuarioId!.Value)
            .Select(g => new { UsuarioId = g.Key, Ultimo = g.Max(x => x.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.UsuarioId, x => x.Ultimo, ct);

        var profissionais = await ProfissionalMaisRecenteAsync(ids, ct);

        var itens = pagina_.Select(x => new AdminPacienteResponse(
                x.Linha.Id,
                x.Linha.Nome,
                x.Linha.Email,
                x.Linha.Ativo,
                x.Linha.StatusAcompanhamento,
                x.Acesso,
                x.Linha.UsuarioId.HasValue && ultimosAcessos.TryGetValue(x.Linha.UsuarioId.Value, out var ultimo) ? ultimo : null,
                profissionais.GetValueOrDefault(x.Linha.Id),
                x.Linha.CreatedAtUtc))
            .ToList();

        return Ok(new AdminPacientesResponse(itens, total, pagina, total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanho)));
    }

    [HttpGet("equipe/acessos")]
    public async Task<ActionResult<IReadOnlyCollection<AdminAcessoEquipeResponse>>> AcessosEquipe(CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        var org = currentUser.OrganizationId;
        var equipe = await db.Users.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.TipoUsuario != TipoUsuario.Paciente)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var ultimos = await UltimosAcessosAsync(org, ct);

        return Ok(equipe.Select(id => new AdminAcessoEquipeResponse(id, ultimos.TryGetValue(id, out var u) ? u : null)).ToList());
    }

    [HttpGet("atividade")]
    public async Task<ActionResult<AdminAtividadePaginaResponse>> Atividade(
        [FromQuery] Guid? usuarioId = null,
        [FromQuery] string? area = null,
        [FromQuery] bool incluirAcessos = false,
        [FromQuery] DateTime? antesDeUtc = null,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        const int tamanho = 40;
        var org = currentUser.OrganizationId;

        var query = db.AuditLogs.AsNoTracking().Where(x => x.OrganizacaoId == org);
        if (!incluirAcessos) query = query.Where(x => !AcoesDeAcesso.Contains(x.Acao));
        if (usuarioId.HasValue) query = query.Where(x => x.UsuarioId == usuarioId);
        if (antesDeUtc.HasValue) query = query.Where(x => x.CreatedAtUtc < antesDeUtc.Value);
        query = area switch
        {
            "equipe" => query.Where(x => x.Entidade == "UsuarioEquipe" || x.Acao.StartsWith("IMPERSONATION")),
            "pacientes" => query.Where(x => x.Entidade == nameof(Paciente)),
            "contas" => query.Where(x => AcoesDeAcesso.Contains(x.Acao) || x.Entidade == "Usuario" || x.Entidade == "Sessao"),
            _ => query
        };

        var logs = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(tamanho + 1)
            .Select(x => new { x.Id, x.CreatedAtUtc, x.UsuarioId, x.Acao, x.Entidade, x.EntidadeId })
            .ToListAsync(ct);

        var temMais = logs.Count > tamanho;
        if (temMais) logs.RemoveAt(logs.Count - 1);

        var autores = logs.Where(x => x.UsuarioId.HasValue).Select(x => x.UsuarioId!.Value).Distinct().ToArray();
        var alvosIds = logs
            .Select(x => Guid.TryParse(x.EntidadeId, out var id) ? id : (Guid?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();
        var pessoasIds = autores.Concat(alvosIds).Distinct().ToArray();

        var usuarios = await db.Users.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && pessoasIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome, x.TipoUsuario })
            .ToDictionaryAsync(x => x.Id, ct);
        var pacientes = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && alvosIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Nome })
            .ToDictionaryAsync(x => x.Id, x => x.Nome, ct);

        string? Alvo(string? entidadeId)
        {
            if (!Guid.TryParse(entidadeId, out var id)) return null;
            if (pacientes.TryGetValue(id, out var nomePaciente)) return nomePaciente;
            return usuarios.TryGetValue(id, out var u) ? u.Nome : null;
        }

        var itens = logs.Select(x =>
        {
            var autor = x.UsuarioId.HasValue && usuarios.TryGetValue(x.UsuarioId.Value, out var u) ? u : null;
            return new AdminAtividadeResponse(
                x.Id,
                x.CreatedAtUtc,
                x.UsuarioId,
                autor?.Nome,
                autor?.TipoUsuario.ToString(),
                x.Acao,
                x.Entidade,
                x.EntidadeId,
                Alvo(x.EntidadeId));
        }).ToList();

        return Ok(new AdminAtividadePaginaResponse(itens, temMais));
    }

    private async Task<Dictionary<Guid, DateTime?>> UltimosAcessosAsync(Guid org, CancellationToken ct) =>
        await db.AuditLogs.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.UsuarioId != null && AcoesDeAcesso.Contains(x.Acao))
            .GroupBy(x => x.UsuarioId!.Value)
            .Select(g => new { UsuarioId = g.Key, Ultimo = g.Max(x => x.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.UsuarioId, x => (DateTime?)x.Ultimo, ct);

    /// <summary>Pacientes que ja tiveram consulta ou plano com algum profissional.</summary>
    private async Task<HashSet<Guid>> PacientesComProfissionalAsync(Guid org, CancellationToken ct)
    {
        var porConsulta = await db.Consultas.AsNoTracking()
            .Where(x => x.Paciente.OrganizacaoId == org)
            .Select(x => x.PacienteId)
            .Distinct()
            .ToListAsync(ct);
        var porDieta = await db.PlanosAlimentares.AsNoTracking()
            .Where(x => x.Paciente.OrganizacaoId == org)
            .Select(x => x.PacienteId)
            .Distinct()
            .ToListAsync(ct);
        var porTreino = await db.PlanosTreino.AsNoTracking()
            .Where(x => x.Paciente.OrganizacaoId == org)
            .Select(x => x.PacienteId)
            .Distinct()
            .ToListAsync(ct);
        return porConsulta.Concat(porDieta).Concat(porTreino).ToHashSet();
    }

    /// <summary>Nome do profissional com o vinculo mais recente (consulta ou plano) de cada paciente.</summary>
    private async Task<Dictionary<Guid, string>> ProfissionalMaisRecenteAsync(Guid[] pacienteIds, CancellationToken ct)
    {
        if (pacienteIds.Length == 0) return [];

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => pacienteIds.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, Quando = x.DataHoraUtc, x.Profissional.Nome })
            .ToListAsync(ct);
        var dietas = await db.PlanosAlimentares.AsNoTracking()
            .Where(x => pacienteIds.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, Quando = x.CreatedAtUtc, x.Profissional.Nome })
            .ToListAsync(ct);
        var treinos = await db.PlanosTreino.AsNoTracking()
            .Where(x => pacienteIds.Contains(x.PacienteId))
            .Select(x => new { x.PacienteId, Quando = x.CreatedAtUtc, x.Profissional.Nome })
            .ToListAsync(ct);

        return consultas.Concat(dietas).Concat(treinos)
            .Where(x => x.Quando <= DateTime.UtcNow.AddDays(1))
            .GroupBy(x => x.PacienteId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Quando).First().Nome);
    }

    private async Task<bool> EhAdmin(CancellationToken ct) =>
        !User.HasClaim("impersonation", "true") &&
        await db.Users.AsNoTracking().AnyAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo &&
            x.TipoUsuario == TipoUsuario.Admin, ct);
}
