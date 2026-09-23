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
    public async Task<ActionResult<IReadOnlyCollection<TimelineItemResponse>>> Get(Guid pacienteId, [FromQuery] int limite = 120, CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking().AnyAsync(x =>
            x.Id == pacienteId && x.OrganizacaoId == currentUser.OrganizationId, ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente nao encontrado." });

        limite = Math.Clamp(limite, 30, 250);

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
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                        x.Tipo != EventoDesvioAdesaoService.TipoRegistro)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(limite)
            .Select(x => new { x.Id, x.DataHoraUtc, x.Tipo, x.Descricao, x.ValorNumerico, x.Unidade, x.Escala })
            .ToListAsync(ct);

        var execucoesTreino = await db.ExecucoesTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataHoraInicioUtc)
            .Take(limite)
            .Select(x => new
            {
                x.Id,
                x.DataHoraInicioUtc,
                x.DataHoraFimUtc,
                x.DuracaoMinutos,
                x.EsforcoPercebido,
                x.Status,
                x.Observacoes,
                Plano = x.PlanoTreino.Nome,
                Sessao = x.SessaoTreino.Nome,
                Itens = x.Itens.Count,
                ItensConcluidos = x.Itens.Count(i => i.Concluido)
            })
            .ToListAsync(ct);

        var checkins = await db.CheckInsAcompanhamento.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataUtc)
            .Take(limite)
            .Select(x => new
            {
                x.Id, x.DataUtc, x.PesoKg, x.AdesaoAlimentacaoPercentual, x.AdesaoTreinoPercentual,
                x.FomeNivel, x.EnergiaNivel, x.SonoNivel, x.PercepcaoEvolucaoNivel, x.Observacoes, x.Origem
            })
            .ToListAsync(ct);

        var mensagens = await db.InteracoesAcompanhamento.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.OrganizacaoId == currentUser.OrganizationId && x.Canal.StartsWith("Chat:"))
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(limite)
            .Select(x => new { x.Id, x.DataHoraUtc, x.Canal, x.Resultado, x.Observacoes, Profissional = x.Profissional.Nome })
            .ToListAsync(ct);

        var registrosDesvio = await db.RegistrosDiarioPaciente.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                        x.Tipo == EventoDesvioAdesaoService.TipoRegistro)
            .OrderByDescending(x => x.DataHoraUtc)
            .Take(limite)
            .ToListAsync(ct);
        var desvios = registrosDesvio
            .Select(EventoDesvioAdesaoService.Ler)
            .Where(x => x is not null)
            .Cast<EventoDesvioAdesaoLeitura>()
            .ToArray();

        var planosTreino = await db.PlanosTreino.AsNoTracking()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limite)
            .Select(x => new { x.Id, x.CreatedAtUtc, x.Nome, x.Objetivo, x.Status, x.Versao, Profissional = x.Profissional.Nome, Sessoes = x.Sessoes.Count })
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
            }, "Clinico", 0, $"consulta:{x.Id}", "Consulta")));

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
                }, "Corpo", 0, $"avaliacao:{x.Id}", "Avaliacao");
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
            }, "Clinico", 0, $"anamnese:{x.Id}", "Anamnese")));

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
            }, "Clinico", 0, $"evolucao:{x.Id}", "Evolucao")));

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
            }, "Exames", 0, $"exame:{x.Id}", "Exames")));

        timeline.AddRange(relatorios.Select(x => new TimelineItemResponse(
            "relatorio", x.Id, x.DataGeracaoUtc, x.Titulo, "Snapshot clinico gerado",
            new { profissional = x.ProfissionalNome, periodoInicioUtc = x.DataInicioUtc, periodoFimUtc = x.DataFimUtc, versaoTemplate = x.VersaoTemplate },
            "Clinico", 0, $"relatorio:{x.Id}", "Relatorio")));

        timeline.AddRange(planos.Select(x => new TimelineItemResponse(
            "plano_alimentar", x.Id, x.CreatedAtUtc, x.Nome, $"Plano alimentar {x.Status.ToLowerInvariant()}",
            new { profissional = x.ProfissionalNome, dataInicio = x.DataInicio, dataFim = x.DataFim, status = x.Status, refeicoes = x.Refeicoes },
            "Nutricao", 0, $"plano-alimentar:{x.Id}", "Plano alimentar")));


        timeline.AddRange(metas.Select(x => new TimelineItemResponse(
            "meta", x.Id, x.CreatedAtUtc, x.Nome, $"Meta {x.Status.ToLowerInvariant()}",
            new { profissional = x.ProfissionalNome, tipo = x.Tipo, valorObjetivo = x.ValorObjetivo, unidade = x.Unidade, frequencia = x.Frequencia, status = x.Status },
            "Metas", 0, $"meta:{x.Id}", "Metas")));

        timeline.AddRange(diario.Select(x => new TimelineItemResponse(
            "registro_diario", x.Id, x.DataHoraUtc, $"Diario - {x.Tipo}", x.Descricao,
            new { tipo = x.Tipo, descricao = x.Descricao, valor = x.ValorNumerico, unidade = x.Unidade, escala = x.Escala },
            "Rotina", 0, $"diario:{x.Id}", "Diario")));

        timeline.AddRange(planosTreino.Select(x => new TimelineItemResponse(
            "plano_treino", x.Id, x.CreatedAtUtc, x.Nome, $"Plano de treino {x.Status.ToLowerInvariant()} • versão {x.Versao}",
            new { profissional = x.Profissional, objetivo = x.Objetivo, status = x.Status, versao = x.Versao, sessoes = x.Sessoes },
            "Treino", 0, $"plano-treino:{x.Id}", "Plano de treino")));

        timeline.AddRange(execucoesTreino.Select(x => new TimelineItemResponse(
            "treino_executado", x.Id, x.DataHoraInicioUtc, x.Sessao,
            $"{x.Status} • {x.ItensConcluidos}/{x.Itens} exercício(s)" + (x.DuracaoMinutos.HasValue ? $" • {x.DuracaoMinutos} min" : string.Empty),
            new { plano = x.Plano, sessao = x.Sessao, x.DataHoraFimUtc, x.DuracaoMinutos, x.EsforcoPercebido, x.Status, x.Observacoes, x.Itens, x.ItensConcluidos },
            "Treino", string.Equals(x.Status, "Concluido", StringComparison.OrdinalIgnoreCase) ? 0 : 1, $"treino:{x.Id}", "Execucao de treino")));

        timeline.AddRange(checkins.Select(x => new TimelineItemResponse(
            "checkin", x.Id, x.DataUtc, "Check-in de acompanhamento",
            $"Energia {x.EnergiaNivel?.ToString() ?? "—"}/10 • Sono {x.SonoNivel?.ToString() ?? "—"}/10" +
            (x.AdesaoTreinoPercentual.HasValue ? $" • treino {x.AdesaoTreinoPercentual}%" : string.Empty),
            new { x.PesoKg, x.AdesaoAlimentacaoPercentual, x.AdesaoTreinoPercentual, x.FomeNivel, x.EnergiaNivel, x.SonoNivel, x.PercepcaoEvolucaoNivel, x.Observacoes, x.Origem },
            "CheckIn", 0, $"checkin:{x.Id}", "Check-in")));

        timeline.AddRange(mensagens.Select(x =>
        {
            var contexto = x.Canal.StartsWith("Chat:", StringComparison.OrdinalIgnoreCase) ? x.Canal[5..] : "Geral";
            var resumo = string.IsNullOrWhiteSpace(x.Observacoes) ? "Mensagem registrada" :
                (x.Observacoes!.Length <= 140 ? x.Observacoes : x.Observacoes[..140] + "…");
            return new TimelineItemResponse(
                "chat", x.Id, x.DataHoraUtc, $"Mensagem • {contexto}", resumo,
                new { autor = x.Resultado, contexto, mensagem = x.Observacoes, profissional = x.Profissional },
                "Comunicacao", 0, $"chat:{x.Id}", "Chat");
        }));

        timeline.AddRange(desvios.Select(x => new TimelineItemResponse(
            "desvio_adesao", x.Id, x.DataHoraUtc, x.Titulo, x.Detalhe,
            new { x.Categoria, x.Tipo, x.Detalhe, x.OrigemChave, x.Contexto },
            x.Categoria, x.Prioridade, x.OrigemChave, "Adesao")));

        return Ok(timeline
            .OrderByDescending(x => x.DataUtc)
            .Take(limite)
            .ToList());
    }
}
