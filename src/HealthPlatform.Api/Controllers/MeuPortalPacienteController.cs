using System.Text.Json;
using HealthPlatform.Api.Contracts.Diario;
using HealthPlatform.Api.Contracts.Metas;
using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Policy = "PatientOnly")]
[Route("api/portal/me")]
public sealed class MeuPortalPacienteController(
    AppDbContext db,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("home")]
    public async Task<ActionResult<PortalPacienteHomeResponse>> Home(
        [FromQuery] DateOnly? data,
        CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Nao existe paciente ativo vinculado a este acesso." });

        return await MontarHome(pacienteId.Value, data, ct);
    }

    [HttpPost("prontidao")]
    public async Task<ActionResult<PortalProntidaoDiariaResponse>> RegistrarProntidao(
        RegistrarProntidaoDiariaRequest request,
        CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.Data > hoje.AddDays(1))
            return BadRequest(new { message = "Nao e permitido registrar prontidao em data futura." });

        if (request.SonoHoras < 0m || request.SonoHoras > 16m)
            return BadRequest(new { message = "Sono deve estar entre 0 e 16 horas." });

        if (!EscalaValida(request.SonoQualidade) || !EscalaValida(request.EnergiaNivel) ||
            !EscalaValida(request.DorNivel) || !EscalaValida(request.DisposicaoNivel) ||
            !EscalaValida(request.RecuperacaoNivel))
            return BadRequest(new { message = "As escalas da prontidao devem estar entre 0 e 10." });

        var ultimoTreino = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value &&
                        x.Status == "Concluido" &&
                        x.DataHoraInicioUtc <= DateTime.UtcNow)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .Select(x => new { x.DataHoraInicioUtc, x.EsforcoPercebido })
            .FirstOrDefaultAsync(ct);

        decimal? horasDesdeUltimoTreino = ultimoTreino is null
            ? null
            : Math.Round((decimal)(DateTime.UtcNow - ultimoTreino.DataHoraInicioUtc).TotalHours, 2);

        var calculo = CalcularProntidao(
            request.SonoHoras, request.SonoQualidade, request.EnergiaNivel, request.DorNivel,
            request.DisposicaoNivel, request.RecuperacaoNivel, horasDesdeUltimoTreino,
            ultimoTreino?.EsforcoPercebido);

        var item = await db.ProntidoesDiarias
            .FirstOrDefaultAsync(x => x.PacienteId == pacienteId.Value && x.Data == request.Data, ct);

        object? antes = item is null ? null : new
        {
            item.SonoHoras, item.SonoQualidade, item.EnergiaNivel, item.DorNivel,
            item.DisposicaoNivel, item.RecuperacaoNivel, item.Score, item.RecomendacaoTreino
        };

        if (item is null)
        {
            item = new ProntidaoDiaria
            {
                OrganizacaoId = currentUser.OrganizationId,
                PacienteId = pacienteId.Value,
                Data = request.Data,
                Origem = "Paciente"
            };
            db.ProntidoesDiarias.Add(item);
        }

        item.SonoHoras = request.SonoHoras;
        item.SonoQualidade = request.SonoQualidade;
        item.EnergiaNivel = request.EnergiaNivel;
        item.DorNivel = request.DorNivel;
        item.DisposicaoNivel = request.DisposicaoNivel;
        item.RecuperacaoNivel = request.RecuperacaoNivel;
        item.HorasDesdeUltimoTreino = horasDesdeUltimoTreino;
        item.EsforcoUltimoTreino = ultimoTreino?.EsforcoPercebido;
        item.Score = calculo.Score;
        item.RecomendacaoTreino = calculo.Recomendacao;
        item.MotivoRecomendacao = calculo.Motivo;
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar(antes is null ? "CREATE" : "UPDATE", nameof(ProntidaoDiaria), item.Id, antes, new
        {
            item.Data, item.SonoHoras, item.SonoQualidade, item.EnergiaNivel, item.DorNivel,
            item.DisposicaoNivel, item.RecuperacaoNivel, item.HorasDesdeUltimoTreino,
            item.EsforcoUltimoTreino, item.Score, item.RecomendacaoTreino
        });

        if (antes is null)
            await GamificacaoService.RegistrarEventoAsync(db, currentUser.OrganizationId, pacienteId.Value,
                item.Data, "Prontidao", item.Id, 30, "Check-in de prontidao concluido.", "Autocuidado", ct);

        await db.SaveChangesAsync(ct);
        return Ok(MapearProntidao(item));
    }


    [HttpPost("dor-corporal")]
    public async Task<ActionResult<PortalDorCorporalResumoResponse>> RegistrarDorCorporal(
        RegistrarDorCorporalRequest request, CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue) return NotFound(new { message = "Paciente vinculado nao encontrado." });
        if (request.Data > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
            return BadRequest(new { message = "Nao e permitido registrar dor em data futura." });
        if (string.IsNullOrWhiteSpace(request.Regiao) || request.Regiao.Trim().Length > 80)
            return BadRequest(new { message = "Informe uma regiao corporal valida." });
        if (request.Intensidade < 0 || request.Intensidade > 10 || request.ImpactoTreino < 0 || request.ImpactoTreino > 10)
            return BadRequest(new { message = "Intensidade e impacto no treino devem estar entre 0 e 10." });

        var inicio = DateTime.SpecifyKind(request.Data.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fim = inicio.AddDays(1);
        var candidatos = await db.RegistrosDiarioPaciente
            .Where(x => x.PacienteId == pacienteId.Value && x.Tipo == "DorCorporal" && x.DataHoraUtc >= inicio && x.DataHoraUtc < fim)
            .ToListAsync(ct);
        var regiao = request.Regiao.Trim();
        var lado = Limpar(request.Lado);
        var item = candidatos.FirstOrDefault(x => DorCorporalService.Corresponde(x.Descricao, regiao, lado));
        var novo = item is null;
        if (item is null)
        {
            item = new RegistroDiarioPaciente { PacienteId = pacienteId.Value, Tipo = "DorCorporal", DataHoraUtc = inicio.AddHours(12) };
            db.RegistrosDiarioPaciente.Add(item);
        }
        item.Escala = request.Intensidade;
        item.ValorNumerico = request.ImpactoTreino;
        item.Unidade = "impacto-treino-0-10";
        item.Descricao = DorCorporalService.Serializar(regiao, lado, request.ImpactoTreino, Limpar(request.Observacao));
        item.UpdatedAtUtc = DateTime.UtcNow;
        Auditar(novo ? "CREATE" : "UPDATE", "DorCorporal", item.Id, null,
            new { request.Data, Regiao = regiao, Lado = lado, request.Intensidade, request.ImpactoTreino });
        await db.SaveChangesAsync(ct);
        return Ok(await DorCorporalService.MontarAsync(db, pacienteId.Value, request.Data, ct));
    }


    [HttpPost("refeicoes/{refeicaoId:guid}/adesao")]
    public async Task<ActionResult<PortalAdesaoNutricionalResponse>> RegistrarAdesaoRefeicao(
        Guid refeicaoId, RegistrarAdesaoRefeicaoRequest request, CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue) return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var status = AdesaoNutricionalService.NormalizarStatus(request.Status);
        if (string.IsNullOrWhiteSpace(status))
            return BadRequest(new { message = "Status deve ser Realizada, Adaptada ou NaoRealizada." });

        var dia = DateOnly.FromDateTime(DateTime.UtcNow);
        var refeicaoValida = await db.RefeicoesPlanoAlimentar.AsNoTracking().AnyAsync(x =>
            x.Id == refeicaoId && x.PlanoAlimentar.PacienteId == pacienteId.Value &&
            x.PlanoAlimentar.Status == "Ativo" && x.PlanoAlimentar.DataInicio <= dia &&
            (!x.PlanoAlimentar.DataFim.HasValue || x.PlanoAlimentar.DataFim.Value >= dia), ct);
        if (!refeicaoValida) return NotFound(new { message = "Refeicao ativa nao encontrada no plano do paciente." });

        var inicio = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fim = inicio.AddDays(1);
        var candidatos = await db.RegistrosDiarioPaciente
            .Where(x => x.PacienteId == pacienteId.Value && x.Tipo == "AdesaoRefeicao" &&
                        x.DataHoraUtc >= inicio && x.DataHoraUtc < fim).ToListAsync(ct);
        var item = candidatos.FirstOrDefault(x => AdesaoNutricionalService.Corresponde(x.Descricao, refeicaoId));
        var novo = item is null;
        if (item is null)
        {
            item = new RegistroDiarioPaciente { PacienteId = pacienteId.Value, Tipo = "AdesaoRefeicao", DataHoraUtc = DateTime.UtcNow };
            db.RegistrosDiarioPaciente.Add(item);
        }
        item.Descricao = AdesaoNutricionalService.Serializar(refeicaoId, status, request.Observacao);
        item.ValorNumerico = status switch { "Realizada" => 100m, "Adaptada" => 80m, _ => 0m };
        item.Unidade = "adequacao-plano-percentual";
        item.Escala = null;
        item.UpdatedAtUtc = DateTime.UtcNow;
        Auditar(novo ? "CREATE" : "UPDATE", "AdesaoRefeicao", item.Id, null, new { refeicaoId, status });
        await db.SaveChangesAsync(ct);

        return Ok(await AdesaoNutricionalService.MontarAsync(db, pacienteId.Value, dia, ct));
    }

    [HttpPost("fechamento-dia")]
    public async Task<IActionResult> FecharDia(FecharDiaRequest request, CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });
        if (request.PercepcaoDoDia < 0 || request.PercepcaoDoDia > 10)
            return BadRequest(new { message = "Percepcao do dia deve estar entre 0 e 10." });

        var dia = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fim = inicio.AddDays(1);
        var item = await db.RegistrosDiarioPaciente.FirstOrDefaultAsync(x =>
            x.PacienteId == pacienteId.Value && x.Tipo == "FechamentoDia" &&
            x.DataHoraUtc >= inicio && x.DataHoraUtc < fim, ct);
        var novo = item is null;
        if (item is null)
        {
            item = new RegistroDiarioPaciente { PacienteId = pacienteId.Value, Tipo = "FechamentoDia", DataHoraUtc = DateTime.UtcNow };
            db.RegistrosDiarioPaciente.Add(item);
        }
        item.Escala = request.PercepcaoDoDia;
        item.Descricao = Limpar(request.Resumo);
        item.Unidade = null;
        item.ValorNumerico = null;
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar(novo ? "CREATE" : "UPDATE", nameof(RegistroDiarioPaciente), item.Id, null,
            new { item.Tipo, item.Escala, item.Descricao, Data = dia });
        if (novo)
            await GamificacaoService.RegistrarEventoAsync(db, currentUser.OrganizationId, pacienteId.Value,
                dia, "FechamentoDia", item.Id, 20, "Dia revisado com consciência.", "Reflexao", ct);
        await db.SaveChangesAsync(ct);
        return Ok(new { data = dia, percepcaoDoDia = item.Escala, resumo = item.Descricao, xp = novo ? 20 : 0 });
    }

    [HttpPost("diario")]
    public async Task<ActionResult<RegistroDiarioResponse>> RegistrarDiario(
        UpsertRegistroDiarioRequest request,
        CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        if (string.IsNullOrWhiteSpace(request.Tipo))
            return BadRequest(new { message = "Tipo do registro e obrigatorio." });

        if (request.Escala.HasValue && (request.Escala < 0 || request.Escala > 10))
            return BadRequest(new { message = "Escala deve estar entre 0 e 10." });

        if (string.IsNullOrWhiteSpace(request.Descricao) &&
            !request.ValorNumerico.HasValue &&
            !request.Escala.HasValue &&
            string.IsNullOrWhiteSpace(request.ImagemUrl))
            return BadRequest(new { message = "Informe ao menos descricao, valor, escala ou imagem." });

        var item = new RegistroDiarioPaciente
        {
            PacienteId = pacienteId.Value,
            DataHoraUtc = request.DataHoraUtc.ToUniversalTime(),
            Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "Observacao" : request.Tipo.Trim(),
            Descricao = Limpar(request.Descricao),
            ValorNumerico = request.ValorNumerico,
            Unidade = Limpar(request.Unidade),
            Escala = request.Escala,
            ImagemUrl = Limpar(request.ImagemUrl)
        };

        db.RegistrosDiarioPaciente.Add(item);
        Auditar("CREATE", nameof(RegistroDiarioPaciente), item.Id, null, new
        {
            item.PacienteId,
            item.DataHoraUtc,
            item.Tipo,
            item.Descricao,
            item.ValorNumerico,
            item.Unidade,
            item.Escala
        });

        await db.SaveChangesAsync(ct);

        return Ok(new RegistroDiarioResponse(
            item.Id, item.PacienteId, item.DataHoraUtc, item.Tipo, item.Descricao,
            item.ValorNumerico, item.Unidade, item.Escala, item.ImagemUrl,
            item.CreatedAtUtc, item.UpdatedAtUtc));
    }

    [HttpPost("metas/{metaId:guid}/registro")]
    public async Task<ActionResult<RegistroMetaResponse>> RegistrarMeta(
        Guid metaId,
        RegistrarMetaRequest request,
        CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var meta = await db.MetasPaciente
            .FirstOrDefaultAsync(x =>
                x.Id == metaId &&
                x.PacienteId == pacienteId.Value &&
                x.Status == "Ativa", ct);

        if (meta is null)
            return NotFound(new { message = "Meta ativa nao encontrada." });

        if (request.Data < meta.DataInicio ||
            (meta.DataFim.HasValue && request.Data > meta.DataFim.Value))
            return BadRequest(new { message = "Data fora do periodo da meta." });

        if (request.Valor is null && request.Concluida is null)
            return BadRequest(new { message = "Informe valor ou concluida." });

        var registro = await db.RegistrosMetas
            .FirstOrDefaultAsync(x => x.MetaPacienteId == meta.Id && x.Data == request.Data, ct);

        var jaConcluida = registro?.Concluida == true;
        object? antes = registro is null
            ? null
            : new { registro.Valor, registro.Concluida, registro.Observacao };

        if (registro is null)
        {
            registro = new RegistroMeta { MetaPacienteId = meta.Id, Data = request.Data };
            db.RegistrosMetas.Add(registro);
        }

        registro.Valor = request.Valor;
        registro.Concluida = request.Concluida ??
            (meta.ValorObjetivo.HasValue && request.Valor.HasValue
                ? request.Valor.Value >= meta.ValorObjetivo.Value
                : null);
        registro.Observacao = Limpar(request.Observacao);

        Auditar(antes is null ? "CREATE" : "UPDATE", nameof(RegistroMeta), registro.Id, antes,
            new { registro.MetaPacienteId, registro.Data, registro.Valor, registro.Concluida, registro.Observacao });

        if (!jaConcluida && registro.Concluida == true)
            await GamificacaoService.RegistrarEventoAsync(db, currentUser.OrganizationId, pacienteId.Value,
                registro.Data, "MetaDiaria", registro.Id, 25, $"Meta concluida: {meta.Nome}.", "Consistencia", ct);

        await db.SaveChangesAsync(ct);

        return Ok(new RegistroMetaResponse(
            registro.Id, registro.Data, registro.Valor, registro.Concluida,
            registro.Observacao, registro.CreatedAtUtc));
    }


    [HttpGet("plano")]
    public async Task<IActionResult> MeuPlano(CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var dia = DateOnly.FromDateTime(DateTime.UtcNow);

        var plano = await db.PlanosAlimentares.AsNoTracking()
            .Include(x => x.Profissional)
            .Include(x => x.Refeicoes)
                .ThenInclude(x => x.Itens)
                    .ThenInclude(x => x.Alimento)
            .Include(x => x.Refeicoes)
                .ThenInclude(x => x.Itens)
                    .ThenInclude(x => x.Substituicoes)
                        .ThenInclude(x => x.Alimento)
            .Where(x => x.PacienteId == pacienteId.Value &&
                        x.Status == "Ativo" &&
                        x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (plano is null)
            return Ok(new { plano = (object?)null });

        static object Nutrientes(Alimento a, decimal gramas)
        {
            var fator = gramas / 100m;
            return new
            {
                calorias = Math.Round(a.CaloriasPor100g * fator, 1),
                proteinas = Math.Round(a.ProteinasPor100g * fator, 1),
                carboidratos = Math.Round(a.CarboidratosPor100g * fator, 1),
                gorduras = Math.Round(a.GordurasPor100g * fator, 1),
                fibras = Math.Round(a.FibrasPor100g * fator, 1)
            };
        }

        var refeicoes = plano.Refeicoes
            .OrderBy(x => x.Ordem)
            .Select(r => new
            {
                r.Id,
                r.Nome,
                r.Horario,
                r.Ordem,
                r.Observacoes,
                itens = r.Itens.Select(i => new
                {
                    i.Id,
                    alimento = i.Alimento.Nome,
                    i.Quantidade,
                    i.Unidade,
                    i.QuantidadeGramas,
                    i.Observacao,
                    nutrientes = Nutrientes(i.Alimento, i.QuantidadeGramas),
                    substituicoes = i.Substituicoes.Select(sub => new
                    {
                        sub.Id,
                        alimento = sub.Alimento.Nome,
                        sub.Quantidade,
                        sub.Unidade,
                        sub.QuantidadeGramas,
                        sub.Observacao,
                        nutrientes = Nutrientes(sub.Alimento, sub.QuantidadeGramas)
                    }).ToList()
                }).ToList()
            }).ToList();

        var itensPlano = plano.Refeicoes.SelectMany(x => x.Itens).ToList();
        var totalCalorias = itensPlano.Sum(i => i.Alimento.CaloriasPor100g * i.QuantidadeGramas / 100m);
        var totalProteinas = itensPlano.Sum(i => i.Alimento.ProteinasPor100g * i.QuantidadeGramas / 100m);
        var totalCarboidratos = itensPlano.Sum(i => i.Alimento.CarboidratosPor100g * i.QuantidadeGramas / 100m);
        var totalGorduras = itensPlano.Sum(i => i.Alimento.GordurasPor100g * i.QuantidadeGramas / 100m);
        var totalFibras = itensPlano.Sum(i => i.Alimento.FibrasPor100g * i.QuantidadeGramas / 100m);

        return Ok(new
        {
            plano = new
            {
                plano.Id,
                plano.Nome,
                plano.DataInicio,
                plano.DataFim,
                plano.Status,
                plano.Observacoes,
                profissional = plano.Profissional.Nome,
                refeicoes,
                totais = new
                {
                    calorias = Math.Round(totalCalorias, 1),
                    proteinas = Math.Round(totalProteinas, 1),
                    carboidratos = Math.Round(totalCarboidratos, 1),
                    gorduras = Math.Round(totalGorduras, 1),
                    fibras = Math.Round(totalFibras, 1)
                }
            }
        });
    }

    [HttpGet("metas")]
    public async Task<IActionResult> MinhasMetas([FromQuery] int dias = 30, CancellationToken ct = default)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        dias = Math.Clamp(dias, 7, 180);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = hoje.AddDays(-(dias - 1));

        var metas = await db.MetasPaciente.AsNoTracking()
            .Include(x => x.Registros.Where(r => r.Data >= inicio && r.Data <= hoje))
            .Where(x => x.PacienteId == pacienteId.Value)
            .OrderByDescending(x => x.Status == "Ativa")
            .ThenBy(x => x.Nome)
            .ToListAsync(ct);

        return Ok(new
        {
            inicio,
            fim = hoje,
            metas = metas.Select(m =>
            {
                var regs = m.Registros.OrderByDescending(x => x.Data).ToList();
                var concluidos = regs.Count(x => x.Concluida == true);
                return new
                {
                    m.Id,
                    m.Nome,
                    m.Tipo,
                    m.ValorObjetivo,
                    m.Unidade,
                    m.Frequencia,
                    m.DataInicio,
                    m.DataFim,
                    m.Status,
                    m.Observacoes,
                    registros = regs.Select(r => new
                    {
                        r.Id,
                        r.Data,
                        r.Valor,
                        r.Concluida,
                        r.Observacao
                    }),
                    resumo = new
                    {
                        registros = regs.Count,
                        concluidos,
                        percentualConclusao = regs.Count == 0
                            ? 0m
                            : Math.Round((decimal)concluidos / regs.Count * 100m, 1)
                    }
                };
            })
        });
    }

    [HttpGet("diario")]
    public async Task<IActionResult> MeuDiario(
        [FromQuery] DateOnly? de,
        [FromQuery] DateOnly? ate,
        [FromQuery] string? tipo,
        CancellationToken ct)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFim = ate ?? hoje;
        var dataInicio = de ?? dataFim.AddDays(-29);

        if (dataInicio > dataFim)
            return BadRequest(new { message = "Periodo do diario invalido." });

        var inicioUtc = DateTime.SpecifyKind(dataInicio.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = DateTime.SpecifyKind(dataFim.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var query = db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value &&
                        x.DataHoraUtc >= inicioUtc &&
                        x.DataHoraUtc < fimUtc);

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(x => x.Tipo == tipo.Trim());

        var itens = await query
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new
            {
                x.Id,
                x.DataHoraUtc,
                x.Tipo,
                x.Descricao,
                x.ValorNumerico,
                x.Unidade,
                x.Escala,
                x.ImagemUrl
            })
            .ToListAsync(ct);

        return Ok(new { de = dataInicio, ate = dataFim, total = itens.Count, itens });
    }

    [HttpGet("evolucao")]
    public async Task<IActionResult> MinhaEvolucao([FromQuery] int limite = 24, CancellationToken ct = default)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        limite = Math.Clamp(limite, 2, 100);

        var itens = await db.Avaliacoes.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value)
            .OrderByDescending(x => x.DataUtc)
            .Take(limite)
            .Select(x => new
            {
                x.Id,
                x.DataUtc,
                x.PesoKg,
                x.AlturaM,
                x.PercentualGordura,
                x.MassaMagraKg,
                x.MassaGordaKg,
                x.CinturaCm,
                x.AbdomenCm,
                x.QuadrilCm,
                x.PressaoSistolica,
                x.PressaoDiastolica,
                x.FrequenciaCardiaca
            })
            .ToListAsync(ct);

        var ordenados = itens.OrderBy(x => x.DataUtc).Select(x => new
        {
            x.Id,
            x.DataUtc,
            x.PesoKg,
            x.AlturaM,
            imc = x.PesoKg.HasValue && x.AlturaM.HasValue && x.AlturaM.Value > 0
                ? Math.Round(x.PesoKg.Value / (x.AlturaM.Value * x.AlturaM.Value), 2)
                : (decimal?)null,
            x.PercentualGordura,
            x.MassaMagraKg,
            x.MassaGordaKg,
            x.CinturaCm,
            x.AbdomenCm,
            x.QuadrilCm,
            x.PressaoSistolica,
            x.PressaoDiastolica,
            x.FrequenciaCardiaca
        }).ToList();

        return Ok(new { total = ordenados.Count, itens = ordenados });
    }

    [HttpGet("exames")]
    public async Task<IActionResult> MeusExames([FromQuery] int limite = 20, CancellationToken ct = default)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        limite = Math.Clamp(limite, 1, 100);

        var exames = await db.ExamesLaboratoriais.AsNoTracking()
            .Include(x => x.Profissional)
            .Include(x => x.Resultados)
                .ThenInclude(x => x.MarcadorLaboratorial)
            .Where(x => x.PacienteId == pacienteId.Value)
            .OrderByDescending(x => x.DataColetaUtc)
            .Take(limite)
            .ToListAsync(ct);

        return Ok(new
        {
            total = exames.Count,
            exames = exames.Select(e => new
            {
                e.Id,
                e.DataColetaUtc,
                e.Laboratorio,
                e.Observacoes,
                profissional = e.Profissional.Nome,
                resultados = e.Resultados.OrderBy(r => r.MarcadorLaboratorial.Nome).Select(r => new
                {
                    r.Id,
                    marcador = r.MarcadorLaboratorial.Nome,
                    r.ValorNumerico,
                    r.ValorTexto,
                    r.Unidade,
                    r.ReferenciaMinima,
                    r.ReferenciaMaxima,
                    r.ReferenciaTexto,
                    r.Observacao,
                    classificacao = Classificar(r.ValorNumerico, r.ReferenciaMinima, r.ReferenciaMaxima)
                })
            })
        });
    }

    [HttpGet("jornada")]
    public async Task<IActionResult> MinhaJornada([FromQuery] int dias = 180, CancellationToken ct = default)
    {
        var pacienteId = await MeuPacienteId(ct);
        if (!pacienteId.HasValue)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        dias = Math.Clamp(dias, 30, 365);
        var desde = DateTime.UtcNow.AddDays(-dias);
        var itens = new List<PortalJornadaItemResponse>();

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.DataHoraUtc >= desde)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new { x.Id, x.DataHoraUtc, x.Motivo, x.Status, Profissional = x.Profissional.Nome })
            .ToListAsync(ct);
        itens.AddRange(consultas.Select(x => new PortalJornadaItemResponse(
            "consulta", x.Id, x.DataHoraUtc, "Consulta", x.Motivo ?? $"Consulta {x.Status}", x.Profissional, "calendario")));

        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.DataUtc >= desde)
            .OrderByDescending(x => x.DataUtc)
            .Select(x => new { x.Id, x.DataUtc, x.PesoKg, x.PercentualGordura })
            .ToListAsync(ct);
        itens.AddRange(avaliacoes.Select(x => new PortalJornadaItemResponse(
            "avaliacao", x.Id, x.DataUtc, "Avaliacao corporal",
            x.PesoKg.HasValue ? $"Peso registrado: {x.PesoKg:0.##} kg" : "Nova avaliacao registrada", null, "evolucao")));

        var exames = await db.ExamesLaboratoriais.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.DataColetaUtc >= desde)
            .OrderByDescending(x => x.DataColetaUtc)
            .Select(x => new { x.Id, x.DataColetaUtc, x.Laboratorio, Resultados = x.Resultados.Count })
            .ToListAsync(ct);
        itens.AddRange(exames.Select(x => new PortalJornadaItemResponse(
            "exame", x.Id, x.DataColetaUtc, "Exame laboratorial",
            x.Resultados == 1 ? "1 marcador registrado" : $"{x.Resultados} marcadores registrados", x.Laboratorio, "exames")));

        var metas = await db.MetasPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.CreatedAtUtc >= desde)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.CreatedAtUtc, x.Nome, x.Status, Profissional = x.Profissional.Nome })
            .ToListAsync(ct);
        itens.AddRange(metas.Select(x => new PortalJornadaItemResponse(
            "meta", x.Id, x.CreatedAtUtc, "Nova meta", x.Nome, x.Profissional, "metas")));

        var diario = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.DataHoraUtc >= desde)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(100)
            .Select(x => new { x.Id, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico, x.Unidade })
            .ToListAsync(ct);
        itens.AddRange(diario.Select(x => new PortalJornadaItemResponse(
            "diario", x.Id, x.DataHoraUtc, x.Tipo,
            x.Descricao ?? (x.ValorNumerico.HasValue ? $"{x.ValorNumerico:0.##} {x.Unidade}".Trim() : "Registro realizado"), null, "diario")));

        var checkins = await db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.DataUtc >= desde)
            .OrderByDescending(x => x.DataUtc)
            .Select(x => new { x.Id, x.DataUtc, x.PesoKg, x.PercepcaoEvolucaoNivel })
            .ToListAsync(ct);
        itens.AddRange(checkins.Select(x => new PortalJornadaItemResponse(
            "checkin", x.Id, x.DataUtc, "Check-in",
            x.PesoKg.HasValue ? $"Peso: {x.PesoKg:0.##} kg" : "Check-in de acompanhamento enviado", null, "evolucao")));

        var treinos = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.DataHoraInicioUtc >= desde)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .Select(x => new { x.Id, x.DataHoraInicioUtc, Sessao = x.SessaoTreino.Nome, x.DuracaoMinutos })
            .ToListAsync(ct);
        itens.AddRange(treinos.Select(x => new PortalJornadaItemResponse(
            "treino", x.Id, x.DataHoraInicioUtc, "Treino realizado",
            string.IsNullOrWhiteSpace(x.Sessao) ? "Sessao concluida" : x.Sessao,
            x.DuracaoMinutos.HasValue ? $"{x.DuracaoMinutos} min" : null, "treino")));

        var solicitacoes = await db.SolicitacoesClinicas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId.Value && x.CreatedAtUtc >= desde)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.CreatedAtUtc, x.Titulo, x.Status, Profissional = x.Profissional.Nome })
            .ToListAsync(ct);
        itens.AddRange(solicitacoes.Select(x => new PortalJornadaItemResponse(
            "solicitacao", x.Id, x.CreatedAtUtc, "Solicitacao clinica", x.Titulo, $"{x.Profissional} · {x.Status}", "solicitacoes")));

        var ordenados = itens.OrderByDescending(x => x.DataUtc).Take(250).ToList();
        return Ok(new
        {
            periodoDias = dias,
            total = ordenados.Count,
            itens = ordenados
        });
    }

    private async Task<Guid?> MeuPacienteId(CancellationToken ct)
        => await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<ActionResult<PortalPacienteHomeResponse>> MontarHome(
        Guid pacienteId,
        DateOnly? data,
        CancellationToken ct)
    {
        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioUtc = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = inicioUtc.AddDays(1);

        var paciente = await db.Pacientes.AsNoTracking()
            .Where(x => x.Id == pacienteId &&
                        x.OrganizacaoId == currentUser.OrganizationId &&
                        x.UsuarioId == currentUser.UserId &&
                        x.Ativo)
            .Select(x => new PortalPacienteResumoResponse(x.Id, x.Nome, x.DataNascimento, x.Sexo))
            .FirstOrDefaultAsync(ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente nao encontrado." });

        var agoraUtc = DateTime.UtcNow;
        var proximaConsulta = await db.Consultas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId &&
                        x.DataHoraUtc >= agoraUtc &&
                        x.Status != StatusConsulta.Cancelada &&
                        x.Status != StatusConsulta.Faltou)
            .OrderBy(x => x.DataHoraUtc)
            .Select(x => new PortalProximaConsultaResponse(
                x.Id, x.DataHoraUtc, x.Status.ToString(), x.Profissional.Nome, x.Motivo))
            .FirstOrDefaultAsync(ct);

        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId)
            .OrderByDescending(x => x.DataUtc)
            .Take(2)
            .Select(x => new { x.DataUtc, x.PesoKg, x.AlturaM, x.PercentualGordura, x.CinturaCm })
            .ToListAsync(ct);

        var atual = avaliacoes.ElementAtOrDefault(0);
        var anterior = avaliacoes.ElementAtOrDefault(1);
        decimal? imc = null;
        if (atual?.PesoKg is not null && atual.AlturaM is not null && atual.AlturaM.Value > 0)
            imc = Math.Round(atual.PesoKg.Value / (atual.AlturaM.Value * atual.AlturaM.Value), 2);

        decimal? variacaoPeso = null;
        if (atual?.PesoKg is not null && anterior?.PesoKg is not null)
            variacaoPeso = Math.Round(atual.PesoKg.Value - anterior.PesoKg.Value, 2);

        var evolucao = new PortalEvolucaoCorporalResponse(
            atual?.DataUtc, atual?.PesoKg, anterior?.PesoKg, variacaoPeso,
            imc, atual?.PercentualGordura, atual?.CinturaCm);

        var planoEntity = await db.PlanosAlimentares.AsNoTracking()
            .Include(x => x.Profissional)
            .Include(x => x.Refeicoes).ThenInclude(x => x.Itens)
            .Where(x => x.PacienteId == pacienteId &&
                        x.Status == "Ativo" &&
                        x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        PortalPlanoAtualResponse? plano = null;
        if (planoEntity is not null)
        {
            var refeicoes = planoEntity.Refeicoes.OrderBy(x => x.Ordem)
                .Select(x => new PortalRefeicaoResponse(
                    x.Id, x.Nome, x.Horario, x.Ordem, x.Itens.Count))
                .ToList();

            plano = new PortalPlanoAtualResponse(
                planoEntity.Id, planoEntity.Nome, planoEntity.DataInicio,
                planoEntity.DataFim, planoEntity.Profissional.Nome,
                refeicoes.Count, refeicoes);
        }

        var metasEntity = await db.MetasPaciente.AsNoTracking()
            .Include(x => x.Registros)
            .Where(x => x.PacienteId == pacienteId &&
                        x.Status == "Ativa" &&
                        x.DataInicio <= dia &&
                        (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        var metas = metasEntity.Select(x =>
        {
            var registro = x.Registros.FirstOrDefault(r => r.Data == dia);
            decimal? progresso = null;
            if (registro?.Concluida == true) progresso = 100m;
            else if (x.ValorObjetivo.HasValue && x.ValorObjetivo.Value > 0 && registro?.Valor is not null)
                progresso = Math.Round(
                    Math.Clamp(registro.Valor.Value / x.ValorObjetivo.Value * 100m, 0m, 100m), 1);

            return new PortalMetaHojeResponse(
                x.Id, x.Nome, x.Tipo, x.ValorObjetivo, x.Unidade,
                registro?.Valor, registro?.Concluida, progresso);
        }).ToList();

        var metasConcluidas = metas.Count(x => x.Concluida == true);
        var percentualMetas = metas.Count == 0
            ? 0m
            : Math.Round((decimal)metasConcluidas / metas.Count * 100m, 1);

        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId &&
                        x.DataHoraUtc >= inicioUtc &&
                        x.DataHoraUtc < fimUtc)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new PortalRegistroDiarioResponse(
                x.Id, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico,
                x.Unidade, x.Escala, x.ImagemUrl))
            .ToListAsync(ct);

        var resultadosRecentes = await db.ResultadosExamesLaboratoriais.AsNoTracking()
            .Where(x => x.ExameLaboratorial.PacienteId == pacienteId)
            .OrderByDescending(x => x.ExameLaboratorial.DataColetaUtc)
            .ThenBy(x => x.MarcadorLaboratorial.Nome)
            .Take(8)
            .Select(x => new
            {
                x.Id,
                x.ExameLaboratorialId,
                x.ExameLaboratorial.DataColetaUtc,
                Marcador = x.MarcadorLaboratorial.Nome,
                x.ValorNumerico,
                x.ValorTexto,
                x.Unidade,
                x.ReferenciaMinima,
                x.ReferenciaMaxima
            })
            .ToListAsync(ct);

        var exames = resultadosRecentes.Select(x => new PortalExameRecenteResponse(
            x.Id, x.ExameLaboratorialId, x.DataColetaUtc, x.Marcador,
            x.ValorNumerico, x.ValorTexto, x.Unidade,
            Classificar(x.ValorNumerico, x.ReferenciaMinima, x.ReferenciaMaxima)))
            .ToList();

        var prontidaoEntity = await db.ProntidoesDiarias.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PacienteId == pacienteId && x.Data == dia, ct);
        var prontidao = prontidaoEntity is null ? null : MapearProntidao(prontidaoEntity);
        var dorCorporal = await DorCorporalService.MontarAsync(db, pacienteId, dia, ct);
        var gamificacao = await GamificacaoService.MontarResumoAsync(db, pacienteId, dia, ct);
        var ciclo = await CicloEsportivoService.MontarAtualAsync(db, pacienteId, dia, ct);
        var metasDoCiclo = await MetasCicloService.MontarAsync(db, pacienteId, dia, ciclo, gamificacao, ct);
        var estrategiaDoDia = await EstrategiaDiariaService.MontarAsync(db, pacienteId, dia, prontidao, ct);
        var tendenciaRecuperacao = await TendenciaRecuperacaoService.MontarAsync(db, pacienteId, dia, ct);
        var cargaTreino = await CargaTreinoService.MontarAsync(db, pacienteId, dia, ct);
        var performance = await PerformanceEsportivaService.MontarAsync(db, pacienteId, dia, ct);
        var execucaoDoDia = await ExecucaoGuiadaService.MontarAsync(db, pacienteId, dia, estrategiaDoDia, ct);
        var planoRecuperacao = PlanoRecuperacaoService.Montar(prontidao, dorCorporal, tendenciaRecuperacao, cargaTreino, estrategiaDoDia, execucaoDoDia);
        var adesaoNutricional = await AdesaoNutricionalService.MontarAsync(db, pacienteId, dia, ct);
        var hidratacaoContextual = await HidratacaoContextualService.MontarAsync(db, pacienteId, dia, ct);
        var evolucaoEsportiva = EvolucaoEsportivaService.Montar(gamificacao, ciclo, tendenciaRecuperacao, cargaTreino, performance, adesaoNutricional, hidratacaoContextual);
        var checkpointDoCiclo = CheckpointCicloService.Montar(ciclo, metasDoCiclo, evolucaoEsportiva);
        var relatorioDoCiclo = RelatorioCicloService.Montar(ciclo, metasDoCiclo, checkpointDoCiclo, evolucaoEsportiva);
        var comparativoDeCiclos = await ComparativoCiclosService.MontarAsync(db, pacienteId, dia, ct);
        var tendenciaDoObjetivo = TendenciaObjetivoCicloService.Montar(ciclo, metasDoCiclo, evolucaoEsportiva, performance, cargaTreino, tendenciaRecuperacao, adesaoNutricional);
        var acoesPrioritariasDoCiclo = AcoesPrioritariasCicloService.Montar(ciclo, tendenciaDoObjetivo, checkpointDoCiclo, tendenciaRecuperacao, cargaTreino, adesaoNutricional, hidratacaoContextual);
        var planejamentoSemanal = PlanejamentoSemanalService.Montar(ciclo, metasDoCiclo, acoesPrioritariasDoCiclo, checkpointDoCiclo, estrategiaDoDia);
        var resumoSemanal = ResumoSemanalService.Montar(dia, planejamentoSemanal, acoesPrioritariasDoCiclo, tendenciaRecuperacao, cargaTreino, adesaoNutricional, hidratacaoContextual, execucaoDoDia);
        var tendenciaSemanal = await TendenciaSemanalService.MontarAsync(db, pacienteId, dia, ct);
        var radarAdesao = RadarAdesaoService.Montar(gamificacao, tendenciaSemanal, planejamentoSemanal, tendenciaRecuperacao, cargaTreino, adesaoNutricional, hidratacaoContextual);
        var planoReconexao = PlanoReconexaoService.Montar(radarAdesao, planejamentoSemanal, acoesPrioritariasDoCiclo, execucaoDoDia);
        var protecaoRetomada = ProtecaoRetomadaService.Montar(radarAdesao, planoReconexao, tendenciaSemanal, gamificacao);
        var estabilidadeHabitos = EstabilidadeHabitosService.Montar(gamificacao, tendenciaSemanal, radarAdesao, protecaoRetomada);
        var proximoFocoHabito = ProximoFocoHabitoService.Montar(estabilidadeHabitos, protecaoRetomada, radarAdesao);
        var revisaoFocoHabito = RevisaoFocoHabitoService.Montar(proximoFocoHabito, estabilidadeHabitos, protecaoRetomada, tendenciaSemanal);
        var encerramentoCicloHabito = EncerramentoCicloHabitoService.Montar(revisaoFocoHabito, estabilidadeHabitos, protecaoRetomada);
        var reentradaDesafio = ReentradaDesafioService.Montar(encerramentoCicloHabito, estabilidadeHabitos, protecaoRetomada);
        var janelaProgressao = JanelaProgressaoService.Montar(reentradaDesafio, estabilidadeHabitos, tendenciaRecuperacao, cargaTreino);
        var decisaoProgressao = DecisaoProgressaoService.Montar(janelaProgressao, ciclo, acoesPrioritariasDoCiclo, tendenciaDoObjetivo);
        var planoProgressaoSupervisionada = PlanoProgressaoSupervisionadaService.Montar(decisaoProgressao, janelaProgressao);
        var monitoramentoRespostaProgressao = MonitoramentoRespostaProgressaoService.Montar(planoProgressaoSupervisionada, tendenciaRecuperacao, cargaTreino, performance);
        var reavaliacaoProgressao = ReavaliacaoProgressaoService.Montar(planoProgressaoSupervisionada, monitoramentoRespostaProgressao);
        var registroProgressao = await RegistroProgressaoService.MontarAsync(db, currentUser.OrganizationId, pacienteId, ct);
        var comparativoProgressao = await ComparativoProgressaoService.MontarAsync(db, currentUser.OrganizationId, pacienteId, dia, ct);
        var interpretacaoLongitudinalProgressao = InterpretacaoLongitudinalProgressaoService.Montar(registroProgressao, comparativoProgressao, reavaliacaoProgressao);
        var historicoProgressoes = await HistoricoProgressaoService.MontarAsync(db, currentUser.OrganizationId, pacienteId, interpretacaoLongitudinalProgressao, dia, ct);
        var comparacaoProgressoes = ComparacaoProgressoesService.Montar(historicoProgressoes);
        var toleranciaProgressao = ToleranciaProgressaoService.Montar(historicoProgressoes);
        var perfilRespostaAtleta = PerfilRespostaAtletaService.Montar(toleranciaProgressao, interpretacaoLongitudinalProgressao, estabilidadeHabitos, tendenciaRecuperacao, cargaTreino, performance);
        var painelMedicinaEsporte = PainelMedicinaEsporteService.Montar(prontidao, dorCorporal, tendenciaRecuperacao, cargaTreino, performance, adesaoNutricional, hidratacaoContextual, ciclo, registroProgressao, perfilRespostaAtleta);
        var alertasClinicoEsportivos = AlertasClinicoEsportivosService.Montar(painelMedicinaEsporte);
        var mapaCorporalLongitudinal = await MapaCorporalLongitudinalService.MontarAsync(db, pacienteId, dia, ct);
        var disponibilidadeTreino = DisponibilidadeTreinoService.Montar(prontidao, dorCorporal, tendenciaRecuperacao, cargaTreino, estrategiaDoDia);
        var sessaoPlanejadaExecutada = await SessaoPlanejadaExecutadaService.MontarAsync(db, pacienteId, dia, estrategiaDoDia, disponibilidadeTreino, ct);
        var coachDiario = CoachDiarioService.Montar(prontidao, dorCorporal, estrategiaDoDia, tendenciaRecuperacao, cargaTreino, performance, execucaoDoDia, ciclo, adesaoNutricional, hidratacaoContextual);

        return Ok(new PortalPacienteHomeResponse(
            dia, paciente, proximaConsulta, prontidao, dorCorporal, gamificacao, ciclo, metasDoCiclo, checkpointDoCiclo, relatorioDoCiclo, comparativoDeCiclos, tendenciaDoObjetivo, acoesPrioritariasDoCiclo, planejamentoSemanal, resumoSemanal, tendenciaSemanal, radarAdesao, planoReconexao, protecaoRetomada, estabilidadeHabitos, proximoFocoHabito, revisaoFocoHabito, encerramentoCicloHabito, reentradaDesafio, janelaProgressao, decisaoProgressao, planoProgressaoSupervisionada, monitoramentoRespostaProgressao, reavaliacaoProgressao, registroProgressao, comparativoProgressao, interpretacaoLongitudinalProgressao, historicoProgressoes, comparacaoProgressoes, toleranciaProgressao, perfilRespostaAtleta, painelMedicinaEsporte, alertasClinicoEsportivos, mapaCorporalLongitudinal, disponibilidadeTreino, sessaoPlanejadaExecutada, estrategiaDoDia, tendenciaRecuperacao, cargaTreino, performance, evolucaoEsportiva, planoRecuperacao, adesaoNutricional, hidratacaoContextual, coachDiario, execucaoDoDia, evolucao, plano,
            metas, metas.Count, metasConcluidas, percentualMetas,
            registros, exames));
    }

    private static PortalProntidaoDiariaResponse MapearProntidao(ProntidaoDiaria x)
        => new(x.Id, x.Data, x.SonoHoras, x.SonoQualidade, x.EnergiaNivel, x.DorNivel,
            x.DisposicaoNivel, x.RecuperacaoNivel, x.HorasDesdeUltimoTreino,
            x.EsforcoUltimoTreino, x.Score, x.RecomendacaoTreino, x.MotivoRecomendacao);

    private static bool EscalaValida(int? valor)
        => !valor.HasValue || (valor.Value >= 0 && valor.Value <= 10);

    private static (int Score, string Recomendacao, string Motivo) CalcularProntidao(
        decimal sonoHoras, int? sonoQualidade, int energia, int dor, int disposicao,
        int recuperacao, decimal? horasDesdeUltimoTreino, int? esforcoUltimoTreino)
    {
        static decimal NotaSonoHoras(decimal horas)
        {
            if (horas >= 7.5m && horas <= 9.5m) return 100m;
            if (horas > 9.5m) return 90m;
            if (horas >= 7m) return 90m;
            if (horas >= 6.5m) return 80m;
            if (horas >= 6m) return 70m;
            if (horas >= 5.5m) return 55m;
            if (horas >= 5m) return 40m;
            return 25m;
        }

        var sono = NotaSonoHoras(sonoHoras);
        if (sonoQualidade.HasValue) sono = (sono + sonoQualidade.Value * 10m) / 2m;

        var score = sono * 0.25m + energia * 10m * 0.20m + (10 - dor) * 10m * 0.20m +
                    disposicao * 10m * 0.15m + recuperacao * 10m * 0.20m;

        var cargaRecente = false;
        if (horasDesdeUltimoTreino.HasValue && esforcoUltimoTreino.HasValue)
        {
            if (horasDesdeUltimoTreino <= 18m && esforcoUltimoTreino >= 8) { score -= 10m; cargaRecente = true; }
            else if (horasDesdeUltimoTreino <= 24m && esforcoUltimoTreino >= 7) { score -= 6m; cargaRecente = true; }
            else if (horasDesdeUltimoTreino <= 36m && esforcoUltimoTreino >= 8) { score -= 4m; cargaRecente = true; }
        }

        if (dor >= 8) score = Math.Min(score, 45m);
        if (recuperacao <= 3) score = Math.Min(score, 50m);
        if (sonoHoras < 4.5m) score = Math.Min(score, 45m);
        var final = (int)Math.Round(Math.Clamp(score, 0m, 100m));

        string recomendacao;
        if (final >= 90 && dor <= 2 && energia >= 8 && recuperacao >= 8 && !cargaRecente) recomendacao = "Pesado";
        else if (final >= 75) recomendacao = "Normal";
        else if (final >= 55) recomendacao = "Leve";
        else recomendacao = "Recuperacao";

        var motivos = new List<string>();
        if (sonoHoras < 6m) motivos.Add("sono abaixo do ideal");
        if (energia <= 5) motivos.Add("energia reduzida");
        if (dor >= 5) motivos.Add("dor elevada");
        if (recuperacao <= 5) motivos.Add("recuperacao incompleta");
        if (cargaRecente) motivos.Add("carga recente alta");
        if (motivos.Count == 0) motivos.Add("marcadores do dia favoraveis");

        var acao = recomendacao switch
        {
            "Pesado" => "Alta intensidade e uma opcao, desde que esteja prevista no plano.",
            "Normal" => "Boa prontidao para cumprir o treino planejado.",
            "Leve" => "Prefira reduzir volume ou intensidade e preservar a tecnica.",
            _ => "Priorize recuperacao ativa, mobilidade ou descanso conforme o plano."
        };

        return (final, recomendacao, $"{acao} Sinais considerados: {string.Join(", ", motivos)}.");
    }

    private void Auditar(string acao, string entidade, Guid id, object? antes, object? depois)
        => db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

    private static string Classificar(decimal? valor, decimal? minimo, decimal? maximo)
    {
        if (!valor.HasValue || (!minimo.HasValue && !maximo.HasValue))
            return "SemReferenciaNumerica";
        if (minimo.HasValue && valor.Value < minimo.Value) return "Baixo";
        if (maximo.HasValue && valor.Value > maximo.Value) return "Alto";
        return "DentroDaReferencia";
    }

    private static string? Limpar(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
