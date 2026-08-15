using System.Text.Json;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
public sealed class CheckInsAcompanhamentoController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record UpsertCheckInRequest(
        DateTime DataUtc,
        decimal? PesoKg,
        int? AdesaoAlimentacaoPercentual,
        int? AdesaoTreinoPercentual,
        int? FomeNivel,
        int? EnergiaNivel,
        int? SonoNivel,
        int? PercepcaoEvolucaoNivel,
        Guid? FaseNutricionalId,
        Guid? FaseTreinoId,
        string? Observacoes);

    public sealed record MeuCheckInRequest(
        decimal? PesoKg,
        int? AdesaoAlimentacaoPercentual,
        int? AdesaoTreinoPercentual,
        int? FomeNivel,
        int? EnergiaNivel,
        int? SonoNivel,
        int? PercepcaoEvolucaoNivel,
        string? Observacoes);

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/status-transicao-fases")]
    public async Task<IActionResult> StatusTransicaoFases(Guid pacienteId, CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var checkIns = await db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.DataUtc).ToListAsync(ct);
        var nutricao = await db.FasesNutricionais.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Ordem).ToListAsync(ct);
        var treino = await db.FasesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Ordem).ToListAsync(ct);

        return Ok(new
        {
            pacienteId, geradoEmUtc = DateTime.UtcNow,
            nutricao = nutricao.Select(f => MontarStatusTransicao(f.Id, f.Nome, f.Tipo, f.Status, f.DataInicio, f.DataFim, f.MetaPesoKg, f.MetaAdesaoPercentual, f.DuracaoMinimaDias, f.CriterioTransicao, checkIns.Where(x => x.FaseNutricionalId == f.Id).ToList(), true)).ToList(),
            treino = treino.Select(f => MontarStatusTransicao(f.Id, f.Nome, f.Tipo, f.Status, f.DataInicio, f.DataFim, f.MetaPesoKg, f.MetaAdesaoPercentual, f.DuracaoMinimaDias, f.CriterioTransicao, checkIns.Where(x => x.FaseTreinoId == f.Id).ToList(), false)).ToList()
        });
    }

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/analise-fases")]
    public async Task<IActionResult> AnaliseFases(
        Guid pacienteId,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var checkIns = await db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.DataUtc)
            .ToListAsync(ct);

        var fasesNutricionais = await db.FasesNutricionais.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Ordem)
            .ToListAsync(ct);

        var fasesTreino = await db.FasesTreino.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Ordem)
            .ToListAsync(ct);

        var nutricao = fasesNutricionais.Select(fase =>
            MontarAnaliseFase(
                fase.Id,
                fase.Nome,
                fase.Tipo,
                fase.Status,
                fase.DataInicio,
                fase.DataFim,
                checkIns.Where(x => x.FaseNutricionalId == fase.Id).ToList(),
                "Nutricao")).ToList();

        var treino = fasesTreino.Select(fase =>
            MontarAnaliseFase(
                fase.Id,
                fase.Nome,
                fase.Tipo,
                fase.Status,
                fase.DataInicio,
                fase.DataFim,
                checkIns.Where(x => x.FaseTreinoId == fase.Id).ToList(),
                "Treino")).ToList();

        return Ok(new
        {
            pacienteId,
            geradoEmUtc = DateTime.UtcNow,
            totalCheckIns = checkIns.Count,
            nutricao,
            treino,
            destaques = new
            {
                melhorAdesaoAlimentar = MelhorFase(nutricao, x => x.MediaAdesaoAlimentacao),
                melhorAdesaoTreino = MelhorFase(treino, x => x.MediaAdesaoTreino),
                maiorReducaoPeso = MaiorReducaoPeso(nutricao.Concat(treino).ToList()),
                maiorEnergiaMedia = MelhorFase(nutricao.Concat(treino).ToList(), x => x.MediaEnergia)
            }
        });
    }

    [Authorize]
    [HttpGet("api/pacientes/{pacienteId:guid}/check-ins")]
    public async Task<IActionResult> Listar(
        Guid pacienteId,
        [FromQuery] int limite = 30,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        limite = Math.Clamp(limite, 1, 100);

        var itens = await QueryPaciente(pacienteId)
            .OrderByDescending(x => x.DataUtc)
            .Take(limite)
            .ToListAsync(ct);

        itens.Reverse();
        return Ok(MontarHistorico(pacienteId, itens));
    }

    [Authorize]
    [HttpPost("api/pacientes/{pacienteId:guid}/check-ins")]
    public async Task<IActionResult> Criar(
        Guid pacienteId,
        UpsertCheckInRequest request,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var erro = Validar(request.PesoKg, request.AdesaoAlimentacaoPercentual, request.AdesaoTreinoPercentual,
            request.FomeNivel, request.EnergiaNivel, request.SonoNivel, request.PercepcaoEvolucaoNivel, request.Observacoes);
        if (erro is not null)
            return BadRequest(new { message = erro });

        if (!await FasesValidas(pacienteId, request.FaseNutricionalId, request.FaseTreinoId, ct))
            return BadRequest(new { message = "Uma das fases informadas nao pertence ao paciente/organizacao." });

        var item = new CheckInAcompanhamento
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId,
            FaseNutricionalId = request.FaseNutricionalId,
            FaseTreinoId = request.FaseTreinoId,
            RegistradoPorUsuarioId = currentUser.UserId,
            DataUtc = request.DataUtc.ToUniversalTime(),
            PesoKg = request.PesoKg,
            AdesaoAlimentacaoPercentual = request.AdesaoAlimentacaoPercentual,
            AdesaoTreinoPercentual = request.AdesaoTreinoPercentual,
            FomeNivel = request.FomeNivel,
            EnergiaNivel = request.EnergiaNivel,
            SonoNivel = request.SonoNivel,
            PercepcaoEvolucaoNivel = request.PercepcaoEvolucaoNivel,
            Observacoes = Limpar(request.Observacoes),
            Origem = "Profissional"
        };

        db.CheckInsAcompanhamento.Add(item);
        Auditar("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);

        var salvo = await QueryPaciente(pacienteId).FirstAsync(x => x.Id == item.Id, ct);
        return Ok(ToResponse(salvo));
    }

    [Authorize]
    [HttpPut("api/check-ins/{id:guid}")]
    public async Task<IActionResult> Atualizar(
        Guid id,
        UpsertCheckInRequest request,
        CancellationToken ct = default)
    {
        var item = await db.CheckInsAcompanhamento
            .Include(x => x.Paciente)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Check-in nao encontrado." });

        var erro = Validar(request.PesoKg, request.AdesaoAlimentacaoPercentual, request.AdesaoTreinoPercentual,
            request.FomeNivel, request.EnergiaNivel, request.SonoNivel, request.PercepcaoEvolucaoNivel, request.Observacoes);
        if (erro is not null)
            return BadRequest(new { message = erro });

        if (!await FasesValidas(item.PacienteId, request.FaseNutricionalId, request.FaseTreinoId, ct))
            return BadRequest(new { message = "Uma das fases informadas nao pertence ao paciente/organizacao." });

        var antes = Snapshot(item);
        item.FaseNutricionalId = request.FaseNutricionalId;
        item.FaseTreinoId = request.FaseTreinoId;
        item.DataUtc = request.DataUtc.ToUniversalTime();
        item.PesoKg = request.PesoKg;
        item.AdesaoAlimentacaoPercentual = request.AdesaoAlimentacaoPercentual;
        item.AdesaoTreinoPercentual = request.AdesaoTreinoPercentual;
        item.FomeNivel = request.FomeNivel;
        item.EnergiaNivel = request.EnergiaNivel;
        item.SonoNivel = request.SonoNivel;
        item.PercepcaoEvolucaoNivel = request.PercepcaoEvolucaoNivel;
        item.Observacoes = Limpar(request.Observacoes);
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("UPDATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);

        var atualizado = await QueryPaciente(item.PacienteId).FirstAsync(x => x.Id == item.Id, ct);
        return Ok(ToResponse(atualizado));
    }

    [Authorize]
    [HttpDelete("api/check-ins/{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct = default)
    {
        var item = await db.CheckInsAcompanhamento.FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Check-in nao encontrado." });

        Auditar("DELETE", item, Snapshot(item), null);
        db.CheckInsAcompanhamento.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpGet("api/portal/me/check-ins")]
    public async Task<IActionResult> MeusCheckIns(
        [FromQuery] int limite = 30,
        CancellationToken ct = default)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        limite = Math.Clamp(limite, 1, 100);
        var itens = await QueryPaciente(pacienteId.Value)
            .OrderByDescending(x => x.DataUtc)
            .Take(limite)
            .ToListAsync(ct);

        itens.Reverse();
        return Ok(MontarHistorico(pacienteId.Value, itens));
    }

    [Authorize(Policy = "PatientOnly")]
    [HttpPost("api/portal/me/check-ins")]
    public async Task<IActionResult> RegistrarMeuCheckIn(
        MeuCheckInRequest request,
        CancellationToken ct = default)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var erro = Validar(request.PesoKg, request.AdesaoAlimentacaoPercentual, request.AdesaoTreinoPercentual,
            request.FomeNivel, request.EnergiaNivel, request.SonoNivel, request.PercepcaoEvolucaoNivel, request.Observacoes);
        if (erro is not null)
            return BadRequest(new { message = erro });

        var faseNutricionalId = await FaseNutricionalAtual(pacienteId.Value, ct);
        var faseTreinoId = await FaseTreinoAtual(pacienteId.Value, ct);

        var item = new CheckInAcompanhamento
        {
            OrganizacaoId = currentUser.OrganizationId,
            PacienteId = pacienteId.Value,
            FaseNutricionalId = faseNutricionalId,
            FaseTreinoId = faseTreinoId,
            RegistradoPorUsuarioId = currentUser.UserId,
            DataUtc = DateTime.UtcNow,
            PesoKg = request.PesoKg,
            AdesaoAlimentacaoPercentual = request.AdesaoAlimentacaoPercentual,
            AdesaoTreinoPercentual = request.AdesaoTreinoPercentual,
            FomeNivel = request.FomeNivel,
            EnergiaNivel = request.EnergiaNivel,
            SonoNivel = request.SonoNivel,
            PercepcaoEvolucaoNivel = request.PercepcaoEvolucaoNivel,
            Observacoes = Limpar(request.Observacoes),
            Origem = "Paciente"
        };

        db.CheckInsAcompanhamento.Add(item);
        Auditar("CREATE_SELF", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);

        var salvo = await QueryPaciente(pacienteId.Value).FirstAsync(x => x.Id == item.Id, ct);
        return Ok(ToResponse(salvo));
    }

    private sealed record CriterioTransicaoStatus(string Codigo, string Rotulo, bool Configurado, bool? Atendido, string? Detalhe);
    private sealed record StatusTransicaoFaseResponse(Guid FaseId, string Nome, string Tipo, string Status, DateOnly DataInicio, DateOnly? DataFim, int DiasDecorridos, int CheckIns, decimal? PesoAtualKg, decimal? AdesaoMediaPercentual, int CriteriosObjetivosConfigurados, int CriteriosObjetivosAtendidos, bool ObjetivosProntosParaRevisao, bool RequerAvaliacaoProfissional, string? CriterioTransicao, IReadOnlyCollection<CriterioTransicaoStatus> Criterios);

    private static StatusTransicaoFaseResponse MontarStatusTransicao(Guid faseId, string nome, string tipo, string status, DateOnly dataInicio, DateOnly? dataFim, decimal? metaPesoKg, int? metaAdesaoPercentual, int? duracaoMinimaDias, string? criterioTransicao, IReadOnlyCollection<CheckInAcompanhamento> checkIns, bool usarAdesaoAlimentar)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var diasDecorridos = Math.Max(0, hoje.DayNumber - dataInicio.DayNumber + 1);
        var ordenados = checkIns.OrderBy(x => x.DataUtc).ToList();
        var pesoAtual = ordenados.LastOrDefault(x => x.PesoKg.HasValue)?.PesoKg;
        var adesaoMedia = usarAdesaoAlimentar ? Media(ordenados.Select(x => x.AdesaoAlimentacaoPercentual)) : Media(ordenados.Select(x => x.AdesaoTreinoPercentual));
        var detalheAdesao = metaAdesaoPercentual.HasValue ? (adesaoMedia.HasValue ? $"{adesaoMedia.Value:0.#}% / meta {metaAdesaoPercentual.Value}%" : $"sem dados / meta {metaAdesaoPercentual.Value}%") : null;
        var detalhePeso = metaPesoKg.HasValue ? (pesoAtual.HasValue ? $"{pesoAtual.Value:0.0} kg / meta {metaPesoKg.Value:0.0} kg" : $"sem peso / meta {metaPesoKg.Value:0.0} kg") : null;
        var criterios = new List<CriterioTransicaoStatus>
        {
            new("duracao_minima", "Duracao minima", duracaoMinimaDias.HasValue, duracaoMinimaDias.HasValue ? diasDecorridos >= duracaoMinimaDias.Value : null, duracaoMinimaDias.HasValue ? $"{diasDecorridos}/{duracaoMinimaDias.Value} dias" : null),
            new("adesao_minima", "Adesao minima", metaAdesaoPercentual.HasValue, metaAdesaoPercentual.HasValue && adesaoMedia.HasValue ? adesaoMedia.Value >= metaAdesaoPercentual.Value : metaAdesaoPercentual.HasValue ? false : null, detalheAdesao),
            new("meta_peso", "Meta de peso", metaPesoKg.HasValue, metaPesoKg.HasValue && pesoAtual.HasValue ? Math.Abs(pesoAtual.Value - metaPesoKg.Value) <= 0.5m : metaPesoKg.HasValue ? false : null, detalhePeso)
        };
        var configurados = criterios.Count(x => x.Configurado);
        var atendidos = criterios.Count(x => x.Configurado && x.Atendido == true);
        return new StatusTransicaoFaseResponse(faseId, nome, tipo, status, dataInicio, dataFim, diasDecorridos, ordenados.Count, pesoAtual, adesaoMedia, configurados, atendidos, configurados > 0 && atendidos == configurados, !string.IsNullOrWhiteSpace(criterioTransicao), string.IsNullOrWhiteSpace(criterioTransicao) ? null : criterioTransicao.Trim(), criterios);
    }

    private sealed record AnaliseFaseResumo(
        Guid FaseId,
        string Nome,
        string Tipo,
        string Status,
        DateOnly DataInicio,
        DateOnly? DataFim,
        string Dominio,
        int CheckIns,
        DateTime? PrimeiroCheckInUtc,
        DateTime? UltimoCheckInUtc,
        decimal? PesoInicialKg,
        decimal? PesoFinalKg,
        decimal? VariacaoPesoKg,
        decimal? MediaAdesaoAlimentacao,
        decimal? MediaAdesaoTreino,
        decimal? MediaFome,
        decimal? MediaEnergia,
        decimal? MediaSono,
        decimal? MediaPercepcaoEvolucao);

    private static AnaliseFaseResumo MontarAnaliseFase(
        Guid faseId,
        string nome,
        string tipo,
        string status,
        DateOnly dataInicio,
        DateOnly? dataFim,
        IReadOnlyCollection<CheckInAcompanhamento> checkIns,
        string dominio)
    {
        var itens = checkIns.OrderBy(x => x.DataUtc).ToList();
        var pesos = itens.Where(x => x.PesoKg.HasValue).ToList();
        var pesoInicial = pesos.FirstOrDefault()?.PesoKg;
        var pesoFinal = pesos.LastOrDefault()?.PesoKg;

        return new AnaliseFaseResumo(
            faseId,
            nome,
            tipo,
            status,
            dataInicio,
            dataFim,
            dominio,
            itens.Count,
            itens.FirstOrDefault()?.DataUtc,
            itens.LastOrDefault()?.DataUtc,
            pesoInicial,
            pesoFinal,
            Diferenca(pesoFinal, pesoInicial),
            Media(itens.Select(x => x.AdesaoAlimentacaoPercentual)),
            Media(itens.Select(x => x.AdesaoTreinoPercentual)),
            Media(itens.Select(x => x.FomeNivel)),
            Media(itens.Select(x => x.EnergiaNivel)),
            Media(itens.Select(x => x.SonoNivel)),
            Media(itens.Select(x => x.PercepcaoEvolucaoNivel)));
    }

    private static decimal? Media(IEnumerable<int?> valores)
    {
        var itens = valores.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return itens.Count == 0 ? null : Math.Round((decimal)itens.Average(), 1);
    }

    private static object? MelhorFase(
        IEnumerable<AnaliseFaseResumo> fases,
        Func<AnaliseFaseResumo, decimal?> seletor)
    {
        var fase = fases
            .Where(x => x.CheckIns > 0 && seletor(x).HasValue)
            .OrderByDescending(x => seletor(x))
            .ThenByDescending(x => x.CheckIns)
            .FirstOrDefault();

        if (fase is null)
            return null;

        return new
        {
            fase.FaseId,
            fase.Nome,
            fase.Tipo,
            fase.Dominio,
            valor = seletor(fase),
            fase.CheckIns
        };
    }

    private static object? MaiorReducaoPeso(IReadOnlyCollection<AnaliseFaseResumo> fases)
    {
        var fase = fases
            .Where(x => x.VariacaoPesoKg.HasValue && x.CheckIns > 0)
            .OrderBy(x => x.VariacaoPesoKg)
            .ThenByDescending(x => x.CheckIns)
            .FirstOrDefault();

        if (fase is null)
            return null;

        return new
        {
            fase.FaseId,
            fase.Nome,
            fase.Tipo,
            fase.Dominio,
            valor = fase.VariacaoPesoKg,
            fase.CheckIns
        };
    }

    private IQueryable<CheckInAcompanhamento> QueryPaciente(Guid pacienteId) =>
        db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .Include(x => x.FaseNutricional)
            .Include(x => x.FaseTreino);

    private object MontarHistorico(Guid pacienteId, IReadOnlyCollection<CheckInAcompanhamento> itens)
    {
        var ordenados = itens.OrderBy(x => x.DataUtc).ToList();
        var atual = ordenados.LastOrDefault();
        var anterior = ordenados.Count > 1 ? ordenados[^2] : null;

        return new
        {
            pacienteId,
            total = ordenados.Count,
            atual = atual is null ? null : ToResponse(atual),
            variacao = new
            {
                pesoKg = Diferenca(atual?.PesoKg, anterior?.PesoKg),
                adesaoAlimentacao = Diferenca(atual?.AdesaoAlimentacaoPercentual, anterior?.AdesaoAlimentacaoPercentual),
                adesaoTreino = Diferenca(atual?.AdesaoTreinoPercentual, anterior?.AdesaoTreinoPercentual),
                energia = Diferenca(atual?.EnergiaNivel, anterior?.EnergiaNivel)
            },
            itens = ordenados.Select(ToResponse).ToList()
        };
    }

    private object ToResponse(CheckInAcompanhamento x) => new
    {
        x.Id,
        x.PacienteId,
        x.DataUtc,
        x.PesoKg,
        x.AdesaoAlimentacaoPercentual,
        x.AdesaoTreinoPercentual,
        x.FomeNivel,
        x.EnergiaNivel,
        x.SonoNivel,
        x.PercepcaoEvolucaoNivel,
        x.Observacoes,
        x.Origem,
        x.FaseNutricionalId,
        faseNutricionalNome = x.FaseNutricional?.Nome,
        x.FaseTreinoId,
        faseTreinoNome = x.FaseTreino?.Nome,
        x.CreatedAtUtc,
        x.UpdatedAtUtc
    };

    private async Task<bool> PacienteExiste(Guid pacienteId, CancellationToken ct) =>
        await db.Pacientes.AnyAsync(x =>
            x.Id == pacienteId &&
            x.OrganizacaoId == currentUser.OrganizationId &&
            x.Ativo, ct);

    private async Task<Guid?> MeuPacienteId(CancellationToken ct) =>
        await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<bool> FasesValidas(
        Guid pacienteId,
        Guid? faseNutricionalId,
        Guid? faseTreinoId,
        CancellationToken ct)
    {
        if (faseNutricionalId.HasValue)
        {
            var ok = await db.FasesNutricionais.AnyAsync(x =>
                x.Id == faseNutricionalId.Value &&
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);
            if (!ok) return false;
        }

        if (faseTreinoId.HasValue)
        {
            var ok = await db.FasesTreino.AnyAsync(x =>
                x.Id == faseTreinoId.Value &&
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId, ct);
            if (!ok) return false;
        }

        return true;
    }

    private async Task<Guid?> FaseNutricionalAtual(Guid pacienteId, CancellationToken ct)
    {
        var agora = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.FasesNutricionais.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                (x.Status == "EmAndamento" ||
                 (x.DataInicio <= agora && (!x.DataFim.HasValue || x.DataFim.Value >= agora))))
            .OrderByDescending(x => x.Status == "EmAndamento")
            .ThenByDescending(x => x.DataInicio)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<Guid?> FaseTreinoAtual(Guid pacienteId, CancellationToken ct)
    {
        var agora = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.FasesTreino.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                (x.Status == "EmAndamento" ||
                 (x.DataInicio <= agora && (!x.DataFim.HasValue || x.DataFim.Value >= agora))))
            .OrderByDescending(x => x.Status == "EmAndamento")
            .ThenByDescending(x => x.DataInicio)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static string? Validar(
        decimal? peso,
        int? adesaoAlimentacao,
        int? adesaoTreino,
        int? fome,
        int? energia,
        int? sono,
        int? percepcao,
        string? observacoes)
    {
        if (peso.HasValue && (peso.Value < 20m || peso.Value > 400m))
            return "Peso deve ficar entre 20 e 400 kg.";

        if (!PercentualValido(adesaoAlimentacao) || !PercentualValido(adesaoTreino))
            return "Adesao deve ficar entre 0 e 100%.";

        if (!EscalaValida(fome) || !EscalaValida(energia) || !EscalaValida(sono) || !EscalaValida(percepcao))
            return "Fome, energia, sono e percepcao de evolucao devem ficar entre 0 e 10.";

        if (!peso.HasValue && !adesaoAlimentacao.HasValue && !adesaoTreino.HasValue &&
            !fome.HasValue && !energia.HasValue && !sono.HasValue && !percepcao.HasValue &&
            string.IsNullOrWhiteSpace(observacoes))
            return "Informe ao menos um indicador ou observacao.";

        return null;
    }

    private static bool PercentualValido(int? valor) =>
        !valor.HasValue || (valor.Value >= 0 && valor.Value <= 100);

    private static bool EscalaValida(int? valor) =>
        !valor.HasValue || (valor.Value >= 0 && valor.Value <= 10);

    private static decimal? Diferenca(decimal? atual, decimal? anterior) =>
        atual.HasValue && anterior.HasValue ? Math.Round(atual.Value - anterior.Value, 2) : null;

    private static int? Diferenca(int? atual, int? anterior) =>
        atual.HasValue && anterior.HasValue ? atual.Value - anterior.Value : null;

    private static string? Limpar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object Snapshot(CheckInAcompanhamento x) => new
    {
        x.Id,
        x.PacienteId,
        x.FaseNutricionalId,
        x.FaseTreinoId,
        x.RegistradoPorUsuarioId,
        x.DataUtc,
        x.PesoKg,
        x.AdesaoAlimentacaoPercentual,
        x.AdesaoTreinoPercentual,
        x.FomeNivel,
        x.EnergiaNivel,
        x.SonoNivel,
        x.PercepcaoEvolucaoNivel,
        x.Observacoes,
        x.Origem
    };

    private void Auditar(string acao, CheckInAcompanhamento item, object? antes, object? depois)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(CheckInAcompanhamento),
            EntidadeId = item.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
    }
}
