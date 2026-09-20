    using System.Text.Json;
    using HealthPlatform.Api.Services;
    using HealthPlatform.Domain.Entities;
    using HealthPlatform.Infrastructure.Data;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    namespace HealthPlatform.Api.Controllers;

    [ApiController]
    [Authorize]
    public sealed class ProgramaTreinoAssignmentController(
        AppDbContext db,
        CurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) : ControllerBase
    {
        public sealed record AtribuirProgramaRequest(
            DateOnly DataInicio,
            bool ConcluirPlanosAtivos,
            bool AtivarPrimeiraFase,
            string? Observacoes);

        public sealed record ProgramaDia(
            string Dia,
            Guid? ModeloPlanoTreinoId,
            string? Observacoes);

        public sealed record ProgramaFase(
            string Nome,
            int Semanas,
            int Ordem,
            string? Observacoes,
            IReadOnlyCollection<ProgramaDia> Dias);

        public sealed record ProgramaConteudo(
            string? Observacoes,
            IReadOnlyCollection<ProgramaFase> Fases);

        public sealed record TemplateItemTreino(
            Guid ExercicioId,
            int Ordem,
            int Series,
            string Repeticoes,
            decimal? Carga,
            string? UnidadeCarga,
            int? DescansoSegundos,
            int? TempoSegundos,
            string? Observacoes);

        public sealed record TemplateSessaoTreino(
            string Nome,
            string? DiasSemana,
            int Ordem,
            string? Observacoes,
            IReadOnlyCollection<TemplateItemTreino> Itens);

        public sealed record TemplateTreinoConteudo(
            string? ObjetivoOriginal,
            string? ObservacoesOriginais,
            IReadOnlyCollection<TemplateSessaoTreino> Sessoes);

        [HttpPost("api/pacientes/{pacienteId:guid}/programas-treino/{programaId:guid}/atribuir")]
        public async Task<IActionResult> Atribuir(
            Guid pacienteId,
            Guid programaId,
            AtribuirProgramaRequest request,
            CancellationToken ct = default)
        {
            var paciente = await db.Pacientes
                .FirstOrDefaultAsync(x =>
                    x.Id == pacienteId &&
                    x.OrganizacaoId == currentUser.OrganizationId &&
                    x.Ativo, ct);

            if (paciente is null)
                return NotFound(new { message = "Paciente nao encontrado ou inativo." });

            var programa = await db.ProgramasTreinoModelo.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == programaId &&
                    x.OrganizacaoId == currentUser.OrganizationId &&
                    x.Ativo, ct);

            if (programa is null)
                return NotFound(new { message = "Programa de treino nao encontrado ou inativo." });

            ProgramaConteudo? conteudo;
            try
            {
                conteudo = JsonSerializer.Deserialize<ProgramaConteudo>(programa.ConteudoJson);
            }
            catch
            {
                conteudo = null;
            }

            if (conteudo is null || conteudo.Fases.Count == 0)
                return BadRequest(new { message = "Programa sem fases validas para publicacao." });

            var fases = conteudo.Fases
                .OrderBy(x => x.Ordem)
                .ToList();

            if (fases.Any(x => x.Semanas <= 0 || x.Semanas > 52))
                return BadRequest(new { message = "O programa possui fase com duracao invalida." });

            var idsModelos = fases
                .SelectMany(x => x.Dias ?? Array.Empty<ProgramaDia>())
                .Where(x => x.ModeloPlanoTreinoId.HasValue)
                .Select(x => x.ModeloPlanoTreinoId!.Value)
                .Distinct()
                .ToArray();

            if (idsModelos.Length == 0)
                return BadRequest(new { message = "O programa nao possui treinos-modelo vinculados." });

            var modelos = await db.ModelosPlanosTreino.AsNoTracking()
                .Where(x =>
                    idsModelos.Contains(x.Id) &&
                    x.OrganizacaoId == currentUser.OrganizationId &&
                    x.Ativo)
                .ToListAsync(ct);

            var idsEncontrados = modelos.Select(x => x.Id).ToHashSet();
            var ausentes = idsModelos.Where(x => !idsEncontrados.Contains(x)).ToArray();

            if (ausentes.Length > 0)
                return Conflict(new
                {
                    message = "O programa referencia treinos-modelo inativos ou indisponiveis.",
                    modelosIndisponiveis = ausentes
                });

            var modelosConteudo = new Dictionary<Guid, TemplateTreinoConteudo>();
            foreach (var modelo in modelos)
            {
                TemplateTreinoConteudo? template = null;
                try
                {
                    template = JsonSerializer.Deserialize<TemplateTreinoConteudo>(modelo.ConteudoJson);
                }
                catch { }

                if (template is null || template.Sessoes.Count == 0)
                    return Conflict(new
                    {
                        message = $"O treino-modelo '{modelo.Nome}' nao possui conteudo valido."
                    });

                modelosConteudo[modelo.Id] = template;
            }

            var exercicioIds = modelosConteudo.Values
                .SelectMany(x => x.Sessoes)
                .SelectMany(x => x.Itens)
                .Select(x => x.ExercicioId)
                .Distinct()
                .ToArray();

            var exerciciosValidos = await db.Exercicios.AsNoTracking()
                .Where(x =>
                    exercicioIds.Contains(x.Id) &&
                    x.OrganizacaoId == currentUser.OrganizationId &&
                    x.Ativo)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var exerciciosInvalidos = exercicioIds.Except(exerciciosValidos).ToArray();
            if (exerciciosInvalidos.Length > 0)
                return Conflict(new
                {
                    message = "Existem exercicios inativos ou indisponiveis nos treinos do programa.",
                    exerciciosInvalidos
                });

            var profissional = await db.Profissionais
                .FirstOrDefaultAsync(x =>
                    x.UsuarioId == currentUser.UserId &&
                    x.OrganizacaoId == currentUser.OrganizationId &&
                    x.Ativo, ct);

            if (profissional is null)
                return Conflict(new { message = "Perfil profissional ativo nao encontrado." });

            if (request.ConcluirPlanosAtivos)
            {
                var ativos = await db.PlanosTreino
                    .Where(x =>
                        x.PacienteId == pacienteId &&
                        x.Paciente.OrganizacaoId == currentUser.OrganizationId &&
                        x.Status == "Ativo")
                    .ToListAsync(ct);

                foreach (var ativo in ativos)
                {
                    ativo.Status = "Concluido";
                    ativo.DataFim ??= request.DataInicio.AddDays(-1);
                    ativo.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            var maiorOrdemFase = await db.FasesTreino
                .Where(x =>
                    x.PacienteId == pacienteId &&
                    x.OrganizacaoId == currentUser.OrganizationId)
                .Select(x => (int?)x.Ordem)
                .MaxAsync(ct) ?? 0;

            var dataCursor = request.DataInicio;
            var planosCriados = new List<PlanoTreino>();
            var fasesCriadas = new List<FaseTreino>();

            for (var faseIndex = 0; faseIndex < fases.Count; faseIndex++)
            {
                var fasePrograma = fases[faseIndex];
                var dias = (fasePrograma.Dias ?? Array.Empty<ProgramaDia>())
                    .Where(x => x.ModeloPlanoTreinoId.HasValue)
                    .ToList();

                if (dias.Count == 0)
                    continue;

                var faseInicio = dataCursor;
                var faseFim = faseInicio.AddDays(Math.Max(1, fasePrograma.Semanas) * 7 - 1);
                dataCursor = faseFim.AddDays(1);

                var plano = new PlanoTreino
                {
                    PacienteId = pacienteId,
                    ProfissionalId = profissional.Id,
                    Nome = $"{programa.Nome} • {fasePrograma.Nome}",
                    Objetivo = programa.Objetivo,
                    DataInicio = faseInicio,
                    DataFim = faseFim,
                    Status = faseIndex == 0 && request.AtivarPrimeiraFase ? "Ativo" : "Planejado",
                    Observacoes = CombinarObservacoes(
                        request.Observacoes,
                        conteudo.Observacoes,
                        fasePrograma.Observacoes),
                    Versao = 1,
                    AjusteCargaPercentual = 0m,
                    AjusteSeries = 0,
                    AjusteRepeticoes = 0,
                    AjusteDescansoSegundos = 0
                };

                var ordemSessao = 1;
                foreach (var dia in dias)
                {
                    var modeloId = dia.ModeloPlanoTreinoId!.Value;
                    var modelo = modelos.First(x => x.Id == modeloId);
                    var template = modelosConteudo[modeloId];

                    foreach (var sessaoTemplate in template.Sessoes.OrderBy(x => x.Ordem))
                    {
                        var sessao = new SessaoTreino
                        {
                            PlanoTreinoId = plano.Id,
                            Nome = template.Sessoes.Count == 1
                                ? modelo.Nome
                                : $"{modelo.Nome} • {sessaoTemplate.Nome}",
                            DiasSemana = dia.Dia,
                            Ordem = ordemSessao++,
                            Observacoes = CombinarObservacoes(
                                dia.Observacoes,
                                sessaoTemplate.Observacoes)
                        };

                        foreach (var itemTemplate in sessaoTemplate.Itens.OrderBy(x => x.Ordem))
                        {
                            sessao.Itens.Add(new ItemTreino
                            {
                                SessaoTreinoId = sessao.Id,
                                ExercicioId = itemTemplate.ExercicioId,
                                Ordem = itemTemplate.Ordem,
                                Series = itemTemplate.Series,
                                Repeticoes = itemTemplate.Repeticoes,
                                Carga = itemTemplate.Carga,
                                UnidadeCarga = itemTemplate.UnidadeCarga,
                                DescansoSegundos = itemTemplate.DescansoSegundos,
                                TempoSegundos = itemTemplate.TempoSegundos,
                                Observacoes = itemTemplate.Observacoes
                            });
                        }

                        plano.Sessoes.Add(sessao);
                    }
                }

                var fasePaciente = new FaseTreino
                {
                    OrganizacaoId = currentUser.OrganizationId,
                    PacienteId = pacienteId,
                    ProfissionalId = profissional.Id,
                    PlanoTreinoId = plano.Id,
                    Nome = fasePrograma.Nome,
                    Tipo = "Programa",
                    Objetivo = programa.Objetivo,
                    DataInicio = faseInicio,
                    DataFim = faseFim,
                    Ordem = maiorOrdemFase + fasesCriadas.Count + 1,
                    Status = faseIndex == 0 && request.AtivarPrimeiraFase
                        ? "EmAndamento"
                        : "Planejada",
                    Observacoes = CombinarObservacoes(
                        programa.Descricao,
                        fasePrograma.Observacoes),
                    DuracaoMinimaDias = Math.Max(1, fasePrograma.Semanas) * 7,
                    CriterioTransicao = "Revisar adesao, prontidao, resposta ao treino e criterio profissional antes da proxima fase."
                };

                planosCriados.Add(plano);
                fasesCriadas.Add(fasePaciente);
                db.PlanosTreino.Add(plano);
                db.FasesTreino.Add(fasePaciente);
            }

            if (planosCriados.Count == 0)
                return BadRequest(new { message = "Nenhuma fase publicavel foi encontrada no programa." });

            db.AuditLogs.Add(new AuditLog
            {
                OrganizacaoId = currentUser.OrganizationId,
                UsuarioId = currentUser.UserId,
                Acao = "ASSIGN_PROGRAM",
                Entidade = nameof(ProgramaTreinoModelo),
                EntidadeId = programa.Id.ToString(),
                DadosNovosJson = JsonSerializer.Serialize(new
                {
                    ProgramaId = programa.Id,
                    programa.Nome,
                    PacienteId = pacienteId,
                    PacienteNome = paciente.Nome,
                    request.DataInicio,
                    request.ConcluirPlanosAtivos,
                    request.AtivarPrimeiraFase,
                    Fases = fasesCriadas.Count,
                    Planos = planosCriados.Count
                }),
                IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            });

            await db.SaveChangesAsync(ct);

            return Ok(new
            {
                programaId = programa.Id,
                programaNome = programa.Nome,
                pacienteId,
                pacienteNome = paciente.Nome,
                dataInicio = request.DataInicio,
                dataFim = fasesCriadas.Max(x => x.DataFim),
                fasesCriadas = fasesCriadas.Select(x => new
                {
                    x.Id,
                    x.PlanoTreinoId,
                    x.Nome,
                    x.DataInicio,
                    x.DataFim,
                    x.Status,
                    x.Ordem
                }),
                planosCriados = planosCriados.Select(x => new
                {
                    x.Id,
                    x.Nome,
                    x.DataInicio,
                    x.DataFim,
                    x.Status,
                    sessoes = x.Sessoes.Count
                })
            });
        }

        private static string? CombinarObservacoes(params string?[] partes)
        {
            var valores = partes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct()
                .ToArray();

            return valores.Length == 0 ? null : string.Join(" | ", valores);
        }
    }
