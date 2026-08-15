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

        return Ok(new PortalPacienteHomeResponse(
            dia, paciente, proximaConsulta, evolucao, plano,
            metas, metas.Count, metasConcluidas, percentualMetas,
            registros, exames));
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
