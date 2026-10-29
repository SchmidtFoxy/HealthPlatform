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
    string? Telefone,
    bool Ativo,
    string StatusAcompanhamento,
    string Acesso,
    DateTime? UltimoAcessoUtc,
    Guid? ProfissionalId,
    string? Profissional,
    string OrigemResponsavel,
    string OrigemResponsavelRotulo,
    IReadOnlyCollection<AdminMembroEquipeResponse> Equipe,
    DateTime CreatedAtUtc);

public sealed record AdminMembroEquipeResponse(Guid Id, string Nome, string Tipo);

public sealed record AdminEquipePacienteRequest(IReadOnlyCollection<Guid> ProfissionalIds);

/// <summary>Em lote: adiciona um profissional a equipe dos pacientes e, se pedido, tira outro (transferencia).</summary>
public sealed record AdminAtribuirLoteRequest(
    IReadOnlyCollection<Guid> PacienteIds,
    Guid ProfissionalId,
    Guid? RemoverProfissionalId,
    bool TornarResponsavel);

public sealed record AdminProfissionalResponse(
    Guid Id,
    Guid UsuarioId,
    string Nome,
    string Tipo,
    string? Especialidade,
    bool ContaAtiva,
    int Pacientes,
    int PacientesDefinidos);

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
    CurrentUser currentUser,
    ProfissionalResponsavelService responsaveis,
    AcessoPacientesService acesso,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
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
        var equipes = await EquipesAsync(org, ct);
        bool SemResponsavel(Guid id) => !equipes.ContainsKey(id);

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

        foreach (var p in pacientes.Where(x => SemResponsavel(x.Id)).OrderBy(x => x.CreatedAtUtc).Take(8))
            atencao.Add(new AdminAtencaoResponse("sem-profissional", p.Nome, "Nenhum profissional na equipe: ninguém vê este paciente", p.Id, p.UsuarioId));

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
            pacientes.Count(x => SemResponsavel(x.Id)),
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
        [FromQuery] string? responsavel = null,
        [FromQuery] string? equipe = null,
        [FromQuery] Guid? profissionalId = null,
        [FromQuery] Guid? id = null,
        [FromQuery] bool incluirInativos = false,
        [FromQuery] int pagina = 1,
        CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        const int tamanho = 30;
        pagina = Math.Max(1, pagina);
        var org = currentUser.OrganizationId;

        var query = db.Pacientes.AsNoTracking().Where(x => x.OrganizacaoId == org);
        if (id.HasValue) query = query.Where(x => x.Id == id.Value);
        else if (!incluirInativos) query = query.Where(x => x.Ativo);
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
                    p.Telefone,
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

        var porPaciente = await responsaveis.ResolverEmLoteAsync(org, linhas.Select(x => x.Id).ToArray(), ct);
        var equipes = await EquipesAsync(org, ct);
        IReadOnlyCollection<AdminMembroEquipeResponse> EquipeDe(Guid id) => equipes.TryGetValue(id, out var e) ? e : [];

        var filtradas = linhas
            .Select(x => new { Linha = x, Acesso = Acesso(x.UsuarioAtivo, x.EmailConfirmado), Responsavel = porPaciente[x.Id] })
            .Where(x => string.IsNullOrWhiteSpace(acesso) || x.Acesso == acesso)
            .Where(x => !profissionalId.HasValue || EquipeDe(x.Linha.Id).Any(m => m.Id == profissionalId))
            .Where(x => equipe != "sem" || EquipeDe(x.Linha.Id).Count == 0)
            .Where(x => responsavel switch
            {
                "sem" => x.Responsavel.Profissional is null,
                "definido" => x.Responsavel.Origem == OrigemResponsavel.Definido,
                "automatico" => x.Responsavel.Profissional is not null && x.Responsavel.Origem != OrigemResponsavel.Definido,
                _ => true
            })
            .ToList();

        var total = filtradas.Count;
        var paginaAtual = filtradas.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
        var usuarioIds = paginaAtual.Where(x => x.Linha.UsuarioId.HasValue).Select(x => x.Linha.UsuarioId!.Value).ToArray();

        var ultimosAcessos = await db.AuditLogs.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.UsuarioId != null && usuarioIds.Contains(x.UsuarioId.Value) && AcoesDeAcesso.Contains(x.Acao))
            .GroupBy(x => x.UsuarioId!.Value)
            .Select(g => new { UsuarioId = g.Key, Ultimo = g.Max(x => x.CreatedAtUtc) })
            .ToDictionaryAsync(x => x.UsuarioId, x => x.Ultimo, ct);

        var itens = paginaAtual.Select(x => new AdminPacienteResponse(
                x.Linha.Id,
                x.Linha.Nome,
                x.Linha.Email,
                x.Linha.Telefone,
                x.Linha.Ativo,
                x.Linha.StatusAcompanhamento,
                x.Acesso,
                x.Linha.UsuarioId.HasValue && ultimosAcessos.TryGetValue(x.Linha.UsuarioId.Value, out var ultimo) ? ultimo : null,
                x.Responsavel.Profissional?.Id,
                x.Responsavel.Profissional?.Nome,
                x.Responsavel.Origem,
                x.Responsavel.OrigemRotulo,
                EquipeDe(x.Linha.Id),
                x.Linha.CreatedAtUtc))
            .ToList();

        return Ok(new AdminPacientesResponse(itens, total, pagina, total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanho)));
    }

    /// <summary>Profissionais ativos com quantos pacientes estao com cada um (para atribuir e transferir).</summary>
    [HttpGet("profissionais")]
    public async Task<ActionResult<IReadOnlyCollection<AdminProfissionalResponse>>> Profissionais(CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        var org = currentUser.OrganizationId;
        var profissionais = await (
                from p in db.Profissionais.AsNoTracking()
                join u in db.Users.AsNoTracking() on p.UsuarioId equals u.Id
                where p.OrganizacaoId == org && p.Ativo
                orderby p.Nome
                select new { p.Id, p.UsuarioId, p.Nome, p.Tipo, p.Especialidade, ContaAtiva = u.Ativo })
            .ToListAsync(ct);

        var ativos = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.Ativo)
            .Select(x => new { x.Id, x.ProfissionalResponsavelId })
            .ToListAsync(ct);
        var idsAtivos = ativos.Select(x => x.Id).ToHashSet();
        var equipes = await EquipesAsync(org, ct);
        var contagem = equipes
            .Where(x => idsAtivos.Contains(x.Key))
            .SelectMany(x => x.Value)
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.Count());
        var definidos = ativos
            .Where(x => x.ProfissionalResponsavelId.HasValue)
            .GroupBy(x => x.ProfissionalResponsavelId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        return Ok(profissionais.Select(x => new AdminProfissionalResponse(
            x.Id,
            x.UsuarioId,
            x.Nome,
            x.Tipo.ToString(),
            x.Especialidade,
            x.ContaAtiva,
            contagem.GetValueOrDefault(x.Id),
            definidos.GetValueOrDefault(x.Id))).ToList());
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

    /// <summary>Define a equipe inteira do paciente: so esses profissionais veem o paciente.</summary>
    [HttpPut("pacientes/{pacienteId:guid}/equipe")]
    public async Task<IActionResult> DefinirEquipe(Guid pacienteId, AdminEquipePacienteRequest request, CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        var org = currentUser.OrganizationId;
        var paciente = await db.Pacientes.FirstOrDefaultAsync(x => x.Id == pacienteId && x.OrganizacaoId == org, ct);
        if (paciente is null) return NotFound(new { message = "Paciente nao encontrado." });

        var desejados = request.ProfissionalIds.Distinct().ToHashSet();
        var validos = await db.Profissionais.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && desejados.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (validos.Count != desejados.Count) return BadRequest(new { message = "Escolha profissionais da clinica." });

        var atuais = await db.ProfissionaisPacientes.Where(x => x.PacienteId == pacienteId).ToListAsync(ct);
        var antes = atuais.Select(x => x.ProfissionalId).ToList();
        db.ProfissionaisPacientes.RemoveRange(atuais.Where(x => !desejados.Contains(x.ProfissionalId)));
        foreach (var id in desejados.Where(id => atuais.All(x => x.ProfissionalId != id)))
            await acesso.GarantirNaEquipeAsync(org, pacienteId, id, currentUser.UserId, ct);

        // O responsavel pelas mensagens precisa estar na equipe; se saiu, o paciente volta ao automatico.
        if (paciente.ProfissionalResponsavelId.HasValue && !desejados.Contains(paciente.ProfissionalResponsavelId.Value))
            paciente.ProfissionalResponsavelId = null;
        paciente.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("SET_CARE_TEAM", pacienteId, new { profissionais = antes }, new { profissionais = desejados });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Atribui ou transfere varios pacientes de uma vez.</summary>
    [HttpPost("pacientes/atribuir")]
    public async Task<IActionResult> AtribuirLote(AdminAtribuirLoteRequest request, CancellationToken ct = default)
    {
        if (!await EhAdmin(ct)) return Forbid();

        var org = currentUser.OrganizationId;
        if (request.PacienteIds.Count == 0) return BadRequest(new { message = "Escolha ao menos um paciente." });
        if (request.RemoverProfissionalId == request.ProfissionalId) return BadRequest(new { message = "Escolha profissionais diferentes." });

        var destino = await db.Profissionais.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ProfissionalId && x.OrganizacaoId == org && x.Ativo, ct);
        if (destino is null) return BadRequest(new { message = "Escolha um profissional ativo da clinica." });

        var ids = request.PacienteIds.Distinct().ToArray();
        var pacientes = await db.Pacientes.Where(x => x.OrganizacaoId == org && ids.Contains(x.Id)).ToListAsync(ct);
        var agora = DateTime.UtcNow;
        var adicionados = 0;
        foreach (var p in pacientes)
        {
            if (await acesso.GarantirNaEquipeAsync(org, p.Id, destino.Id, currentUser.UserId, ct)) adicionados++;
            if (request.RemoverProfissionalId.HasValue)
            {
                var sai = await db.ProfissionaisPacientes
                    .Where(x => x.PacienteId == p.Id && x.ProfissionalId == request.RemoverProfissionalId.Value)
                    .ToListAsync(ct);
                db.ProfissionaisPacientes.RemoveRange(sai);
                if (p.ProfissionalResponsavelId == request.RemoverProfissionalId) p.ProfissionalResponsavelId = destino.Id;
            }
            if (request.TornarResponsavel) p.ProfissionalResponsavelId = destino.Id;
            p.UpdatedAtUtc = agora;
            Auditar(request.RemoverProfissionalId.HasValue ? "TRANSFER_CARE_TEAM" : "ADD_CARE_TEAM", p.Id, null, new
            {
                ProfissionalId = destino.Id,
                request.RemoverProfissionalId,
                request.TornarResponsavel
            });
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { pacientes = pacientes.Count, adicionados });
    }

    /// <summary>Equipe de cada paciente da organizacao: ligacoes da equipe mais o responsavel definido.</summary>
    private async Task<Dictionary<Guid, List<AdminMembroEquipeResponse>>> EquipesAsync(Guid org, CancellationToken ct)
    {
        var profissionais = await db.Profissionais.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .Select(x => new { x.Id, x.Nome, x.Tipo })
            .ToDictionaryAsync(x => x.Id, ct);
        var ligacoes = await db.ProfissionaisPacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org)
            .Select(x => new { x.PacienteId, x.ProfissionalId })
            .ToListAsync(ct);
        var responsaveisDefinidos = await db.Pacientes.AsNoTracking()
            .Where(x => x.OrganizacaoId == org && x.ProfissionalResponsavelId.HasValue)
            .Select(x => new { PacienteId = x.Id, ProfissionalId = x.ProfissionalResponsavelId!.Value })
            .ToListAsync(ct);

        return ligacoes.Concat(responsaveisDefinidos)
            .Where(x => profissionais.ContainsKey(x.ProfissionalId))
            .GroupBy(x => x.PacienteId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ProfissionalId).Distinct()
                    .Select(id => new AdminMembroEquipeResponse(id, profissionais[id].Nome, profissionais[id].Tipo.ToString()))
                    .OrderBy(x => x.Nome)
                    .ToList());
    }

    private void Auditar(string acao, Guid pacienteId, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog
    {
        OrganizacaoId = currentUser.OrganizationId,
        UsuarioId = currentUser.UserId,
        Acao = acao,
        Entidade = nameof(Paciente),
        EntidadeId = pacienteId.ToString(),
        DadosAnterioresJson = antes is null ? null : System.Text.Json.JsonSerializer.Serialize(antes),
        DadosNovosJson = depois is null ? null : System.Text.Json.JsonSerializer.Serialize(depois),
        IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
    });

    private async Task<bool> EhAdmin(CancellationToken ct) =>
        !User.HasClaim("impersonation", "true") &&
        await db.Users.AsNoTracking().AnyAsync(x =>
            x.Id == currentUser.UserId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo &&
            x.TipoUsuario == TipoUsuario.Admin, ct);
}
