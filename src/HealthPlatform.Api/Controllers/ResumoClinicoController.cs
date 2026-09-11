using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Enums;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

public sealed record ResumoClinicoConsultaResponse(
    Guid Id,
    DateTime DataHoraUtc,
    string Status,
    string? Motivo);

public sealed record ResumoClinicoSoapResponse(
    Guid Id,
    DateTime DataHoraUtc,
    string ProfissionalNome,
    string? Subjetivo,
    string? Objetivo,
    string? Avaliacao,
    string? Plano);

public sealed record ResumoClinicoCorporalResponse(
    DateTime DataUtc,
    decimal? PesoKg,
    decimal? AlturaM,
    decimal? Imc,
    decimal? PercentualGordura,
    decimal? CinturaCm);

public sealed record ResumoClinicoExameResponse(
    string Marcador,
    decimal ValorNumerico,
    string? Unidade,
    string Classificacao,
    DateTime DataColetaUtc);

public sealed record ResumoClinicoAnamneseResponse(
    Guid Id,
    DateTime DataUtc,
    string? ObjetivoAcompanhamento,
    string? Alergias,
    string? Medicamentos,
    string? Suplementos,
    decimal? SonoHorasMedia,
    string? SonoQualidade,
    int? EstresseNivel);


public sealed record ResumoDesdeUltimaConsultaResponse(
    DateTime PeriodoInicioUtc,
    string BasePeriodo,
    int DiasAcompanhados,
    int RegistrosDiario,
    int TreinosRealizados,
    int CheckInsRealizados,
    decimal? AdesaoAlimentacaoMedia,
    decimal? AdesaoTreinoMedia,
    decimal? PesoInicialKg,
    decimal? PesoAtualKg,
    decimal? VariacaoPesoKg,
    int SolicitacoesPendentes,
    int SolicitacoesParaRevisao);

public sealed record ResumoClinicoResponse(
    Guid PacienteId,
    string PacienteNome,
    DateTime GeradoEmUtc,
    ResumoClinicoConsultaResponse? UltimaConsulta,
    ResumoClinicoConsultaResponse? ProximaConsulta,
    ResumoClinicoSoapResponse? UltimaEvolucao,
    ResumoClinicoCorporalResponse? UltimaAvaliacao,
    IReadOnlyCollection<ResumoClinicoExameResponse> ExamesAlterados,
    ResumoClinicoAnamneseResponse? UltimaAnamnese,
    int MetasAtivas,
    int TreinosUltimos30Dias,
    int PendenciasAbertas,
    int PendenciasAltaPrioridade,
    ResumoDesdeUltimaConsultaResponse DesdeUltimaConsulta);

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/resumo-clinico")]
public sealed class ResumoClinicoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResumoClinicoResponse>> Get(
        Guid pacienteId,
        CancellationToken ct = default)
    {
        var org = currentUser.OrganizationId;
        var agora = DateTime.UtcNow;

        var paciente = await db.Pacientes.AsNoTracking()
            .Where(x => x.Id == pacienteId && x.OrganizacaoId == org)
            .Select(x => new { x.Id, x.Nome })
            .FirstOrDefaultAsync(ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente nao encontrado." });

        var ultimaConsultaEntity = await db.Consultas.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org &&
                x.DataHoraUtc <= agora &&
                x.Status != StatusConsulta.Cancelada)
            .OrderByDescending(x => x.DataHoraUtc)
            .FirstOrDefaultAsync(ct);

        var proximaConsultaEntity = await db.Consultas.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org &&
                x.DataHoraUtc > agora &&
                x.Status != StatusConsulta.Cancelada &&
                x.Status != StatusConsulta.Faltou &&
                x.Status != StatusConsulta.Realizada)
            .OrderBy(x => x.DataHoraUtc)
            .FirstOrDefaultAsync(ct);

        ResumoClinicoConsultaResponse? ultimaConsulta = ultimaConsultaEntity is null
            ? null
            : new(ultimaConsultaEntity.Id, ultimaConsultaEntity.DataHoraUtc,
                ultimaConsultaEntity.Status.ToString(), ultimaConsultaEntity.Motivo);

        ResumoClinicoConsultaResponse? proximaConsulta = proximaConsultaEntity is null
            ? null
            : new(proximaConsultaEntity.Id, proximaConsultaEntity.DataHoraUtc,
                proximaConsultaEntity.Status.ToString(), proximaConsultaEntity.Motivo);

        var ultimaEvolucao = await db.EvolucoesClinicas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == org)
            .OrderByDescending(x => x.DataHoraUtc)
            .Select(x => new ResumoClinicoSoapResponse(
                x.Id,
                x.DataHoraUtc,
                x.Profissional.Nome,
                x.Subjetivo,
                x.Objetivo,
                x.Avaliacao,
                x.Plano))
            .FirstOrDefaultAsync(ct);

        var avaliacaoEntity = await db.Avaliacoes.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org)
            .OrderByDescending(x => x.DataUtc)
            .FirstOrDefaultAsync(ct);

        ResumoClinicoCorporalResponse? ultimaAvaliacao = null;
        if (avaliacaoEntity is not null)
        {
            decimal? imc = null;
            if (avaliacaoEntity.PesoKg.HasValue &&
                avaliacaoEntity.AlturaM.HasValue &&
                avaliacaoEntity.AlturaM.Value > 0)
            {
                imc = Math.Round(
                    avaliacaoEntity.PesoKg.Value /
                    (avaliacaoEntity.AlturaM.Value * avaliacaoEntity.AlturaM.Value), 2);
            }

            ultimaAvaliacao = new ResumoClinicoCorporalResponse(
                avaliacaoEntity.DataUtc,
                avaliacaoEntity.PesoKg,
                avaliacaoEntity.AlturaM,
                imc,
                avaliacaoEntity.PercentualGordura,
                avaliacaoEntity.CinturaCm);
        }

        var anamneseEntity = await db.Anamneses.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org)
            .OrderByDescending(x => x.DataUtc)
            .FirstOrDefaultAsync(ct);

        ResumoClinicoAnamneseResponse? ultimaAnamnese = anamneseEntity is null
            ? null
            : new(
                anamneseEntity.Id,
                anamneseEntity.DataUtc,
                anamneseEntity.ObjetivoAcompanhamento,
                anamneseEntity.Alergias,
                anamneseEntity.Medicamentos,
                anamneseEntity.Suplementos,
                anamneseEntity.SonoHorasMedia,
                anamneseEntity.SonoQualidade,
                anamneseEntity.EstresseNivel);

        var resultadosRecentes = await db.ResultadosExamesLaboratoriais.AsNoTracking()
            .Where(x =>
                x.ExameLaboratorial.PacienteId == pacienteId &&
                x.ExameLaboratorial.Paciente.OrganizacaoId == org &&
                x.ValorNumerico.HasValue)
            .OrderByDescending(x => x.ExameLaboratorial.DataColetaUtc)
            .Take(60)
            .Select(x => new
            {
                Marcador = x.MarcadorLaboratorial.Nome,
                Valor = x.ValorNumerico!.Value,
                x.Unidade,
                x.ReferenciaMinima,
                x.ReferenciaMaxima,
                x.ExameLaboratorial.DataColetaUtc
            })
            .ToListAsync(ct);

        var examesAlterados = resultadosRecentes
            .Where(x =>
                (x.ReferenciaMinima.HasValue && x.Valor < x.ReferenciaMinima.Value) ||
                (x.ReferenciaMaxima.HasValue && x.Valor > x.ReferenciaMaxima.Value))
            .Take(10)
            .Select(x => new ResumoClinicoExameResponse(
                x.Marcador,
                x.Valor,
                x.Unidade,
                x.ReferenciaMinima.HasValue && x.Valor < x.ReferenciaMinima.Value ? "Abaixo" : "Acima",
                x.DataColetaUtc))
            .ToList();

        var metasAtivas = await db.MetasPaciente.AsNoTracking()
            .CountAsync(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org &&
                x.Status == "Ativa", ct);

        var desde30 = agora.AddDays(-30);
        var treinosUltimos30Dias = await db.ExecucoesTreino.AsNoTracking()
            .CountAsync(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org &&
                x.DataHoraInicioUtc >= desde30, ct);

        var pendencias = await db.PendenciasClinicas.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == org &&
                x.Status != "Resolvida" &&
                (x.Status != "Adiada" ||
                 !x.AdiadaAteUtc.HasValue ||
                 x.AdiadaAteUtc <= agora))
            .Select(x => new { x.Severidade })
            .ToListAsync(ct);

        var periodoInicio = ultimaConsultaEntity?.DataHoraUtc ?? agora.AddDays(-30);
        var basePeriodo = ultimaConsultaEntity is null ? "Ultimos30Dias" : "UltimaConsulta";
        var diasAcompanhados = Math.Max(0, (int)Math.Ceiling((agora - periodoInicio).TotalDays));

        var registrosDesdeConsulta = await db.RegistrosDiarioPaciente.AsNoTracking()
            .CountAsync(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org &&
                x.DataHoraUtc >= periodoInicio, ct);

        var treinosDesdeConsulta = await db.ExecucoesTreino.AsNoTracking()
            .CountAsync(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org &&
                x.DataHoraInicioUtc >= periodoInicio &&
                x.Status == "Concluido", ct);

        var checkInsPeriodo = await db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.OrganizacaoId == org &&
                x.DataUtc >= periodoInicio)
            .Select(x => new
            {
                x.AdesaoAlimentacaoPercentual,
                x.AdesaoTreinoPercentual
            })
            .ToListAsync(ct);

        var adesaoAlimentacao = checkInsPeriodo
            .Where(x => x.AdesaoAlimentacaoPercentual.HasValue)
            .Select(x => (decimal)x.AdesaoAlimentacaoPercentual!.Value)
            .ToList();
        var adesaoTreino = checkInsPeriodo
            .Where(x => x.AdesaoTreinoPercentual.HasValue)
            .Select(x => (decimal)x.AdesaoTreinoPercentual!.Value)
            .ToList();

        var pesosPeriodo = await db.Avaliacoes.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == org &&
                x.DataUtc >= periodoInicio &&
                x.PesoKg.HasValue)
            .OrderBy(x => x.DataUtc)
            .Select(x => x.PesoKg!.Value)
            .ToListAsync(ct);

        decimal? pesoInicial = pesosPeriodo.Count > 0 ? pesosPeriodo[0] : null;
        decimal? pesoAtual = pesosPeriodo.Count > 0 ? pesosPeriodo[^1] : null;
        decimal? variacaoPeso = pesoInicial.HasValue && pesoAtual.HasValue
            ? Math.Round(pesoAtual.Value - pesoInicial.Value, 2)
            : null;

        var solicitacoesPeriodo = await db.SolicitacoesClinicas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == org)
            .Select(x => x.Status)
            .ToListAsync(ct);

        var desdeUltimaConsulta = new ResumoDesdeUltimaConsultaResponse(
            periodoInicio,
            basePeriodo,
            diasAcompanhados,
            registrosDesdeConsulta,
            treinosDesdeConsulta,
            checkInsPeriodo.Count,
            adesaoAlimentacao.Count > 0 ? Math.Round(adesaoAlimentacao.Average(), 1) : null,
            adesaoTreino.Count > 0 ? Math.Round(adesaoTreino.Average(), 1) : null,
            pesoInicial,
            pesoAtual,
            variacaoPeso,
            solicitacoesPeriodo.Count(x => x == "Pendente"),
            solicitacoesPeriodo.Count(x => x == "Enviada"));

        return Ok(new ResumoClinicoResponse(
            paciente.Id,
            paciente.Nome,
            agora,
            ultimaConsulta,
            proximaConsulta,
            ultimaEvolucao,
            ultimaAvaliacao,
            examesAlterados,
            ultimaAnamnese,
            metasAtivas,
            treinosUltimos30Dias,
            pendencias.Count,
            pendencias.Count(x => x.Severidade == "Alta"),
            desdeUltimaConsulta));
    }
}
