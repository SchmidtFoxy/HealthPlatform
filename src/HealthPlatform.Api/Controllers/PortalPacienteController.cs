using HealthPlatform.Api.Contracts.Portal;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/portal")]
public class PortalPacienteController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    [HttpGet("home")]
    public async Task<ActionResult<PortalPacienteHomeResponse>> GetHome(Guid pacienteId, [FromQuery] DateOnly? data, CancellationToken ct)
    {
        var dia = data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioUtc = DateTime.SpecifyKind(dia.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var fimUtc = inicioUtc.AddDays(1);

        var paciente = await db.Pacientes.AsNoTracking()
            .Where(x => x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .Select(x => new PortalPacienteResumoResponse(x.Id, x.Nome, x.DataNascimento, x.Sexo))
            .FirstOrDefaultAsync(ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente nao encontrado ou inativo." });

        var agoraUtc = DateTime.UtcNow;
        var proximaConsulta = await db.Consultas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                        x.DataHoraUtc >= agoraUtc && x.Status != StatusConsulta.Cancelada && x.Status != StatusConsulta.Faltou)
            .OrderBy(x => x.DataHoraUtc)
            .Select(x => new PortalProximaConsultaResponse(x.Id, x.DataHoraUtc, x.Status.ToString(), x.Profissional.Nome, x.Motivo))
            .FirstOrDefaultAsync(ct);

        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
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
            atual?.DataUtc,
            atual?.PesoKg,
            anterior?.PesoKg,
            variacaoPeso,
            imc,
            atual?.PercentualGordura,
            atual?.CinturaCm);

        var planoEntity = await db.PlanosAlimentares.AsNoTracking()
            .Include(x => x.Profissional)
            .Include(x => x.Refeicoes)
            .ThenInclude(x => x.Itens)
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                        x.Status == "Ativo" && x.DataInicio <= dia && (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderByDescending(x => x.DataInicio)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        PortalPlanoAtualResponse? plano = null;
        if (planoEntity is not null)
        {
            var refeicoes = planoEntity.Refeicoes.OrderBy(x => x.Ordem)
                .Select(x => new PortalRefeicaoResponse(x.Id, x.Nome, x.Horario, x.Ordem, x.Itens.Count))
                .ToList();
            plano = new PortalPlanoAtualResponse(planoEntity.Id, planoEntity.Nome, planoEntity.DataInicio, planoEntity.DataFim, planoEntity.Profissional.Nome, refeicoes.Count, refeicoes);
        }

        var metasEntity = await db.MetasPaciente.AsNoTracking()
            .Include(x => x.Registros)
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                        x.Status == "Ativa" && x.DataInicio <= dia && (!x.DataFim.HasValue || x.DataFim.Value >= dia))
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

        var metas = metasEntity.Select(x =>
        {
            var registro = x.Registros.FirstOrDefault(r => r.Data == dia);
            decimal? progresso = null;
            if (registro?.Concluida == true) progresso = 100m;
            else if (x.ValorObjetivo.HasValue && x.ValorObjetivo.Value > 0 && registro?.Valor is not null)
                progresso = Math.Round(Math.Clamp(registro.Valor.Value / x.ValorObjetivo.Value * 100m, 0m, 100m), 1);

            return new PortalMetaHojeResponse(x.Id, x.Nome, x.Tipo, x.ValorObjetivo, x.Unidade, registro?.Valor, registro?.Concluida, progresso);
        }).ToList();

        var metasConcluidas = metas.Count(x => x.Concluida == true);
        var percentualMetas = metas.Count == 0 ? 0m : Math.Round((decimal)metasConcluidas / metas.Count * 100m, 1);

        var registros = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.DataHoraUtc >= inicioUtc && x.DataHoraUtc < fimUtc)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new PortalRegistroDiarioResponse(x.Id, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico, x.Unidade, x.Escala, x.ImagemUrl))
            .ToListAsync(ct);

        var resultadosRecentes = await db.ResultadosExamesLaboratoriais.AsNoTracking()
            .Where(x => x.ExameLaboratorial.PacienteId == pacienteId && x.ExameLaboratorial.Paciente.OrganizacaoId == currentUser.OrganizationId)
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
            x.Id,
            x.ExameLaboratorialId,
            x.DataColetaUtc,
            x.Marcador,
            x.ValorNumerico,
            x.ValorTexto,
            x.Unidade,
            Classificar(x.ValorNumerico, x.ReferenciaMinima, x.ReferenciaMaxima)))
            .ToList();

        return Ok(new PortalPacienteHomeResponse(
            dia,
            paciente,
            proximaConsulta,
            evolucao,
            plano,
            metas,
            metas.Count,
            metasConcluidas,
            percentualMetas,
            registros,
            exames));
    }

    private static string Classificar(decimal? valor, decimal? minimo, decimal? maximo)
    {
        if (!valor.HasValue || (!minimo.HasValue && !maximo.HasValue)) return "SemReferenciaNumerica";
        if (minimo.HasValue && valor.Value < minimo.Value) return "Baixo";
        if (maximo.HasValue && valor.Value > maximo.Value) return "Alto";
        return "DentroDaReferencia";
    }
}
