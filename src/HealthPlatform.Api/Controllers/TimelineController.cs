using HealthPlatform.Api.Contracts.Timeline;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes/{pacienteId:guid}/timeline")]
public class TimelineController(AppDbContext db, CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TimelineItemResponse>>> Get(Guid pacienteId, CancellationToken ct)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente nao encontrado." });

        var consultas = await db.Consultas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new
            {
                x.Id,
                x.DataHoraUtc,
                x.Motivo,
                x.QueixaPrincipal,
                x.Evolucao,
                x.Conduta,
                x.Status,
                ProfissionalNome = x.Profissional.Nome
            })
            .ToListAsync(ct);

        var avaliacoes = await db.Avaliacoes.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new
            {
                x.Id,
                x.ConsultaId,
                x.DataUtc,
                x.PesoKg,
                x.AlturaM,
                x.PercentualGordura,
                x.CinturaCm,
                x.PressaoSistolica,
                x.PressaoDiastolica
            })
            .ToListAsync(ct);

        var anamneses = await db.Anamneses.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new
            {
                x.Id,
                x.ConsultaId,
                x.DataUtc,
                x.ObjetivoAcompanhamento,
                x.SonoHorasMedia,
                x.EstresseNivel,
                x.AguaLitrosDia,
                ProfissionalNome = x.Profissional.Nome
            })
            .ToListAsync(ct);

        var evolucoes = await db.EvolucoesClinicas.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new
            {
                x.Id,
                x.ConsultaId,
                x.DataHoraUtc,
                x.Subjetivo,
                x.Objetivo,
                x.Avaliacao,
                x.Plano,
                x.Observacoes,
                ProfissionalNome = x.Profissional.Nome
            })
            .ToListAsync(ct);

        var exames = await db.ExamesLaboratoriais.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new
            {
                x.Id,
                x.DataColetaUtc,
                x.Laboratorio,
                x.Observacoes,
                ProfissionalNome = x.Profissional.Nome,
                Resultados = x.Resultados.Select(r => new
                {
                    Marcador = r.MarcadorLaboratorial.Nome,
                    r.ValorNumerico,
                    r.ValorTexto,
                    r.Unidade
                }).ToList()
            })
            .ToListAsync(ct);

        var relatorios = await db.RelatoriosClinicos.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new { x.Id, x.DataGeracaoUtc, x.Titulo, x.DataInicioUtc, x.DataFimUtc, x.VersaoTemplate, ProfissionalNome = x.Profissional.Nome })
            .ToListAsync(ct);

        var planos = await db.PlanosAlimentares.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new { x.Id, x.CreatedAtUtc, x.Nome, x.DataInicio, x.DataFim, x.Status, ProfissionalNome = x.Profissional.Nome, Refeicoes = x.Refeicoes.Count })
            .ToListAsync(ct);

        var metas = await db.MetasPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .Select(x => new { x.Id, x.CreatedAtUtc, x.Nome, x.Tipo, x.ValorObjetivo, x.Unidade, x.Frequencia, x.Status, ProfissionalNome = x.Profissional.Nome })
            .ToListAsync(ct);

        var diario = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(50)
            .Select(x => new { x.Id, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico, x.Unidade, x.Escala })
            .ToListAsync(ct);

        var timeline = new List<TimelineItemResponse>();

        timeline.AddRange(consultas.Select(x => new TimelineItemResponse(
            "consulta",
            x.Id,
            x.DataHoraUtc,
            $"Consulta - {x.Status}",
            x.Motivo ?? x.QueixaPrincipal,
            new
            {
                profissional = x.ProfissionalNome,
                motivo = x.Motivo,
                queixaPrincipal = x.QueixaPrincipal,
                evolucao = x.Evolucao,
                conduta = x.Conduta,
                status = x.Status.ToString()
            })));

        timeline.AddRange(avaliacoes.Select(x =>
        {
            decimal? imc = null;
            if (x.PesoKg.HasValue && x.AlturaM.HasValue && x.AlturaM.Value > 0)
                imc = Math.Round(x.PesoKg.Value / (x.AlturaM.Value * x.AlturaM.Value), 2);

            return new TimelineItemResponse(
                "avaliacao",
                x.Id,
                x.DataUtc,
                "Avaliacao corporal",
                x.PesoKg.HasValue ? $"Peso: {x.PesoKg:0.##} kg" : "Avaliacao registrada",
                new
                {
                    consultaId = x.ConsultaId,
                    pesoKg = x.PesoKg,
                    alturaM = x.AlturaM,
                    imc,
                    percentualGordura = x.PercentualGordura,
                    cinturaCm = x.CinturaCm,
                    pressao = x.PressaoSistolica.HasValue || x.PressaoDiastolica.HasValue
                        ? $"{x.PressaoSistolica}/{x.PressaoDiastolica}"
                        : null
                });
        }));

        timeline.AddRange(anamneses.Select(x => new TimelineItemResponse(
            "anamnese",
            x.Id,
            x.DataUtc,
            "Anamnese",
            x.ObjetivoAcompanhamento ?? "Anamnese registrada",
            new
            {
                consultaId = x.ConsultaId,
                profissional = x.ProfissionalNome,
                objetivo = x.ObjetivoAcompanhamento,
                sonoHorasMedia = x.SonoHorasMedia,
                estresseNivel = x.EstresseNivel,
                aguaLitrosDia = x.AguaLitrosDia
            })));

        timeline.AddRange(evolucoes.Select(x => new TimelineItemResponse(
            "evolucao_clinica",
            x.Id,
            x.DataHoraUtc,
            "Evolucao clinica SOAP",
            x.Avaliacao ?? x.Plano ?? x.Subjetivo ?? "Evolucao registrada",
            new
            {
                consultaId = x.ConsultaId,
                profissional = x.ProfissionalNome,
                subjetivo = x.Subjetivo,
                objetivo = x.Objetivo,
                avaliacao = x.Avaliacao,
                plano = x.Plano,
                observacoes = x.Observacoes
            })));

        timeline.AddRange(exames.Select(x => new TimelineItemResponse(
            "exame",
            x.Id,
            x.DataColetaUtc,
            "Exames laboratoriais",
            x.Resultados.Count == 1 ? "1 marcador registrado" : $"{x.Resultados.Count} marcadores registrados",
            new
            {
                profissional = x.ProfissionalNome,
                laboratorio = x.Laboratorio,
                observacoes = x.Observacoes,
                resultados = x.Resultados
            })));

        timeline.AddRange(relatorios.Select(x => new TimelineItemResponse(
            "relatorio", x.Id, x.DataGeracaoUtc, x.Titulo, "Snapshot clinico gerado",
            new { profissional = x.ProfissionalNome, periodoInicioUtc = x.DataInicioUtc, periodoFimUtc = x.DataFimUtc, versaoTemplate = x.VersaoTemplate })));

        timeline.AddRange(planos.Select(x => new TimelineItemResponse(
            "plano_alimentar", x.Id, x.CreatedAtUtc, x.Nome, $"Plano alimentar {x.Status.ToLowerInvariant()}",
            new { profissional = x.ProfissionalNome, dataInicio = x.DataInicio, dataFim = x.DataFim, status = x.Status, refeicoes = x.Refeicoes })));


        timeline.AddRange(metas.Select(x => new TimelineItemResponse(
            "meta", x.Id, x.CreatedAtUtc, x.Nome, $"Meta {x.Status.ToLowerInvariant()}",
            new { profissional = x.ProfissionalNome, tipo = x.Tipo, valorObjetivo = x.ValorObjetivo, unidade = x.Unidade, frequencia = x.Frequencia, status = x.Status })));

        timeline.AddRange(diario.Select(x => new TimelineItemResponse(
            "registro_diario", x.Id, x.DataHoraUtc, $"Diario - {x.Tipo}", x.Descricao,
            new { tipo = x.Tipo, descricao = x.Descricao, valor = x.ValorNumerico, unidade = x.Unidade, escala = x.Escala })));

        return Ok(timeline.OrderByDescending(x => x.DataUtc).ToList());
    }
}
