using System.Text.Json;
using HealthPlatform.Api.Contracts.PlanosAlimentares;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class PlanosAlimentaresController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpGet("api/pacientes/{pacienteId:guid}/planos-alimentares")]
    public async Task<ActionResult<IReadOnlyCollection<PlanoAlimentarResponse>>> GetByPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return NotFound(new { message = "Paciente nao encontrado." });
        var itens = await QueryCompleta().Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId).OrderByDescending(x => x.DataInicio).ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpGet("api/planos-alimentares/{id:guid}")]
    public async Task<ActionResult<PlanoAlimentarResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await QueryCompleta().FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Plano alimentar nao encontrado." }) : Ok(ToResponse(item));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/planos-alimentares")]
    public async Task<ActionResult<PlanoAlimentarResponse>> Create(Guid pacienteId, UpsertPlanoAlimentarRequest request, CancellationToken ct)
    {
        var erro = await Validar(pacienteId, request, ct); if (erro is not null) return BadRequest(new { message = erro });
        var profissional = await GetProfissionalAtual(ct); if (profissional is null) return Conflict(new { message = "Cadastre seu perfil profissional antes de criar planos alimentares." });
        var item = new PlanoAlimentar
        {
            PacienteId = pacienteId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Status = NormalizarStatus(request.Status),
            Observacoes = Limpar(request.Observacoes),
            MetaCalorias = request.MetaCalorias,
            MetaProteinasG = request.MetaProteinasG,
            MetaCarboidratosG = request.MetaCarboidratosG,
            MetaGordurasG = request.MetaGordurasG,
            MetaFibrasG = request.MetaFibrasG
        };
        await MontarRefeicoes(item, request.Refeicoes, ct); db.PlanosAlimentares.Add(item); Auditar("CREATE", item, null, Snapshot(item)); await db.SaveChangesAsync(ct);
        var criado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct); return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(criado));
    }

    [HttpPut("api/planos-alimentares/{id:guid}")]
    public async Task<ActionResult<PlanoAlimentarResponse>> Update(Guid id, UpsertPlanoAlimentarRequest request, CancellationToken ct)
    {
        var item = await QueryCompletaTracking().FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Plano alimentar nao encontrado." });
        var erro = await Validar(item.PacienteId, request, ct); if (erro is not null) return BadRequest(new { message = erro });
        var antes = Snapshot(item);
        item.Nome = request.Nome.Trim();
        item.DataInicio = request.DataInicio;
        item.DataFim = request.DataFim;
        item.Status = NormalizarStatus(request.Status);
        item.Observacoes = Limpar(request.Observacoes);
        item.MetaCalorias = request.MetaCalorias;
        item.MetaProteinasG = request.MetaProteinasG;
        item.MetaCarboidratosG = request.MetaCarboidratosG;
        item.MetaGordurasG = request.MetaGordurasG;
        item.MetaFibrasG = request.MetaFibrasG;
        item.UpdatedAtUtc = DateTime.UtcNow;
        db.RefeicoesPlanoAlimentar.RemoveRange(item.Refeicoes); item.Refeicoes.Clear(); await MontarRefeicoes(item, request.Refeicoes, ct); Auditar("UPDATE", item, antes, Snapshot(item)); await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == id, ct); return Ok(ToResponse(atualizado));
    }

    [HttpPut("api/planos-alimentares/{id:guid}/metas-nutricionais")]
    public async Task<ActionResult<PlanoAlimentarResponse>> AtualizarMetasNutricionais(
        Guid id,
        AtualizarMetasNutricionaisRequest request,
        CancellationToken ct = default)
    {
        var item = await QueryCompletaTracking().FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Plano alimentar nao encontrado." });

        var erro = ValidarMetas(
            request.MetaCalorias,
            request.MetaProteinasG,
            request.MetaCarboidratosG,
            request.MetaGordurasG,
            request.MetaFibrasG);

        if (erro is not null)
            return BadRequest(new { message = erro });

        var antes = new
        {
            item.MetaCalorias,
            item.MetaProteinasG,
            item.MetaCarboidratosG,
            item.MetaGordurasG,
            item.MetaFibrasG
        };

        item.MetaCalorias = request.MetaCalorias;
        item.MetaProteinasG = request.MetaProteinasG;
        item.MetaCarboidratosG = request.MetaCarboidratosG;
        item.MetaGordurasG = request.MetaGordurasG;
        item.MetaFibrasG = request.MetaFibrasG;
        item.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("NUTRITION_TARGETS", item, antes, new
        {
            item.MetaCalorias,
            item.MetaProteinasG,
            item.MetaCarboidratosG,
            item.MetaGordurasG,
            item.MetaFibrasG
        });

        await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == id, ct);
        return Ok(ToResponse(atualizado));
    }

    [HttpPut("api/refeicoes-plano/{refeicaoId:guid}/metas-nutricionais")]
    public async Task<ActionResult<PlanoAlimentarResponse>> AtualizarMetasRefeicao(
        Guid refeicaoId,
        AtualizarMetasRefeicaoRequest request,
        CancellationToken ct = default)
    {
        var plano = await QueryCompletaTracking().FirstOrDefaultAsync(x =>
            x.Refeicoes.Any(r => r.Id == refeicaoId) &&
            x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (plano is null)
            return NotFound(new { message = "Refeicao nao encontrada." });

        var erro = ValidarMetas(
            request.MetaCalorias,
            request.MetaProteinasG,
            request.MetaCarboidratosG,
            request.MetaGordurasG,
            request.MetaFibrasG);

        if (erro is not null)
            return BadRequest(new { message = erro });

        var refeicao = plano.Refeicoes.First(x => x.Id == refeicaoId);
        var antes = new
        {
            RefeicaoId = refeicao.Id,
            refeicao.MetaCalorias,
            refeicao.MetaProteinasG,
            refeicao.MetaCarboidratosG,
            refeicao.MetaGordurasG,
            refeicao.MetaFibrasG
        };

        refeicao.MetaCalorias = request.MetaCalorias;
        refeicao.MetaProteinasG = request.MetaProteinasG;
        refeicao.MetaCarboidratosG = request.MetaCarboidratosG;
        refeicao.MetaGordurasG = request.MetaGordurasG;
        refeicao.MetaFibrasG = request.MetaFibrasG;
        refeicao.UpdatedAtUtc = DateTime.UtcNow;
        plano.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("MEAL_NUTRITION_TARGETS", plano, antes, new
        {
            RefeicaoId = refeicao.Id,
            refeicao.MetaCalorias,
            refeicao.MetaProteinasG,
            refeicao.MetaCarboidratosG,
            refeicao.MetaGordurasG,
            refeicao.MetaFibrasG
        });

        await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == plano.Id, ct);
        return Ok(ToResponse(atualizado));
    }

    [HttpPost("api/planos-alimentares/{id:guid}/distribuir-metas-refeicoes")]
    public async Task<ActionResult<PlanoAlimentarResponse>> DistribuirMetasRefeicoes(
        Guid id,
        DistribuirMetasRefeicoesRequest request,
        CancellationToken ct = default)
    {
        var plano = await QueryCompletaTracking().FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (plano is null)
            return NotFound(new { message = "Plano alimentar nao encontrado." });

        if (request.Refeicoes is null || request.Refeicoes.Count == 0)
            return BadRequest(new { message = "Informe a distribuicao das refeicoes." });

        if (request.Refeicoes.Select(x => x.RefeicaoId).Distinct().Count() != request.Refeicoes.Count)
            return BadRequest(new { message = "Cada refeicao deve aparecer apenas uma vez na distribuicao." });

        var idsPlano = plano.Refeicoes.Select(x => x.Id).OrderBy(x => x).ToArray();
        var idsRequest = request.Refeicoes.Select(x => x.RefeicaoId).OrderBy(x => x).ToArray();

        if (!idsPlano.SequenceEqual(idsRequest))
            return BadRequest(new { message = "A distribuicao deve incluir exatamente todas as refeicoes do plano." });

        if (request.Refeicoes.Any(x => x.Percentual < 0m || x.Percentual > 100m))
            return BadRequest(new { message = "Percentuais por refeicao devem ficar entre 0 e 100." });

        var soma = request.Refeicoes.Sum(x => x.Percentual);
        if (Math.Abs(soma - 100m) > 0.1m)
            return BadRequest(new { message = $"A soma dos percentuais deve ser 100%. Soma atual: {soma:0.##}%." });

        if (!TemMetaPlano(plano))
            return BadRequest(new { message = "Defina ao menos uma meta nutricional diaria no plano antes de distribuir por refeicao." });

        var antes = plano.Refeicoes.OrderBy(x => x.Ordem).Select(x => new
        {
            x.Id,
            x.MetaCalorias,
            x.MetaProteinasG,
            x.MetaCarboidratosG,
            x.MetaGordurasG,
            x.MetaFibrasG
        }).ToList();

        foreach (var distribuicao in request.Refeicoes)
        {
            var refeicao = plano.Refeicoes.First(x => x.Id == distribuicao.RefeicaoId);
            var fator = distribuicao.Percentual / 100m;
            refeicao.MetaCalorias = PercentualMeta(plano.MetaCalorias, fator);
            refeicao.MetaProteinasG = PercentualMeta(plano.MetaProteinasG, fator);
            refeicao.MetaCarboidratosG = PercentualMeta(plano.MetaCarboidratosG, fator);
            refeicao.MetaGordurasG = PercentualMeta(plano.MetaGordurasG, fator);
            refeicao.MetaFibrasG = PercentualMeta(plano.MetaFibrasG, fator);
            refeicao.UpdatedAtUtc = DateTime.UtcNow;
        }

        plano.UpdatedAtUtc = DateTime.UtcNow;

        Auditar("MEAL_TARGET_DISTRIBUTION", plano, antes, request.Refeicoes);
        await db.SaveChangesAsync(ct);

        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == plano.Id, ct);
        return Ok(ToResponse(atualizado));
    }

    [HttpGet("api/planos-alimentares/{id:guid}/analise-nutricional")]
    public async Task<ActionResult<AnalisePlanoAlimentarResponse>> AnaliseNutricional(
        Guid id,
        CancellationToken ct = default)
    {
        var item = await QueryCompleta().FirstOrDefaultAsync(x =>
            x.Id == id &&
            x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (item is null)
            return NotFound(new { message = "Plano alimentar nao encontrado." });

        var response = ToResponse(item);
        var total = response.TotaisDiarios;

        var distribuicao = response.Refeicoes.Select(r =>
            new DistribuicaoRefeicaoResponse(
                r.Id,
                r.Nome,
                r.Horario,
                r.Totais,
                r.Metas,
                r.Desvios,
                new DistribuicaoNutricionalResponse(
                    Percentual(r.Totais.Calorias, total.Calorias),
                    Percentual(r.Totais.ProteinasG, total.ProteinasG),
                    Percentual(r.Totais.CarboidratosG, total.CarboidratosG),
                    Percentual(r.Totais.GordurasG, total.GordurasG),
                    Percentual(r.Totais.FibrasG, total.FibrasG))))
            .ToList();

        return Ok(new AnalisePlanoAlimentarResponse(
            item.Id,
            new MetasNutricionaisResponse(
                item.MetaCalorias,
                item.MetaProteinasG,
                item.MetaCarboidratosG,
                item.MetaGordurasG,
                item.MetaFibrasG),
            total,
            new DesviosNutricionaisResponse(
                Desvio(total.Calorias, item.MetaCalorias),
                Desvio(total.ProteinasG, item.MetaProteinasG),
                Desvio(total.CarboidratosG, item.MetaCarboidratosG),
                Desvio(total.GordurasG, item.MetaGordurasG),
                Desvio(total.FibrasG, item.MetaFibrasG)),
            distribuicao));
    }

    [HttpGet("api/planos-alimentares/{id:guid}/simular-ajuste")]
    public async Task<ActionResult<SimulacaoAjustePlanoResponse>> SimularAjuste(
        Guid id,
        [FromQuery] decimal? percentual = null,
        [FromQuery] decimal? caloriasAlvo = null,
        CancellationToken ct = default)
    {
        var origem = await QueryCompleta()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (origem is null)
            return NotFound(new { message = "Plano alimentar nao encontrado." });

        var atual = ToResponse(origem).TotaisDiarios;
        var erro = ResolverAjuste(atual.Calorias, percentual, caloriasAlvo, out var ajuste, out var fator);
        if (erro is not null)
            return BadRequest(new { message = erro });

        return Ok(new SimulacaoAjustePlanoResponse(
            origem.Id,
            ajuste,
            fator,
            atual,
            EscalarTotais(atual, fator),
            origem.Refeicoes.Sum(x => x.Itens.Count)));
    }

    [HttpPost("api/planos-alimentares/{id:guid}/duplicar")]
    public async Task<ActionResult<PlanoAlimentarResponse>> Duplicar(
        Guid id,
        DuplicarPlanoAlimentarRequest request,
        CancellationToken ct = default)
    {
        var origem = await QueryCompletaTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);

        if (origem is null)
            return NotFound(new { message = "Plano alimentar nao encontrado." });

        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { message = "Nome da nova versao e obrigatorio." });

        if (request.DataFim.HasValue && request.DataFim.Value < request.DataInicio)
            return BadRequest(new { message = "Data final nao pode ser anterior a data inicial." });

        var atual = ToResponse(origem).TotaisDiarios;
        var erro = ResolverAjuste(
            atual.Calorias,
            request.AjustePercentual,
            request.CaloriasAlvo,
            out var ajuste,
            out var fator);

        if (erro is not null)
            return BadRequest(new { message = erro });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Cadastre seu perfil profissional antes de criar uma progressao alimentar." });

        var raizId = origem.PlanoOrigemId ?? origem.Id;
        var maiorVersao = await db.PlanosAlimentares
            .Where(x =>
                x.PacienteId == origem.PacienteId &&
                (x.Id == raizId || x.PlanoOrigemId == raizId))
            .MaxAsync(x => (int?)x.Versao, ct) ?? origem.Versao;

        var novo = new PlanoAlimentar
        {
            PacienteId = origem.PacienteId,
            ProfissionalId = profissional.Id,
            Nome = request.Nome.Trim(),
            DataInicio = request.DataInicio,
            DataFim = request.DataFim,
            Status = "Ativo",
            Observacoes = origem.Observacoes,
            PlanoOrigemId = raizId,
            Versao = maiorVersao + 1,
            AjustePercentual = ajuste,
            MetaCalorias = request.CaloriasAlvo ?? EscalarNullable(origem.MetaCalorias, fator),
            MetaProteinasG = EscalarNullable(origem.MetaProteinasG, fator),
            MetaCarboidratosG = EscalarNullable(origem.MetaCarboidratosG, fator),
            MetaGordurasG = EscalarNullable(origem.MetaGordurasG, fator),
            MetaFibrasG = EscalarNullable(origem.MetaFibrasG, fator)
        };

        foreach (var refeicaoOrigem in origem.Refeicoes.OrderBy(x => x.Ordem))
        {
            var refeicao = new RefeicaoPlanoAlimentar
            {
                PlanoAlimentarId = novo.Id,
                Nome = refeicaoOrigem.Nome,
                Horario = refeicaoOrigem.Horario,
                Ordem = refeicaoOrigem.Ordem,
                Observacoes = refeicaoOrigem.Observacoes,
                MetaCalorias = EscalarNullable(refeicaoOrigem.MetaCalorias, fator),
                MetaProteinasG = EscalarNullable(refeicaoOrigem.MetaProteinasG, fator),
                MetaCarboidratosG = EscalarNullable(refeicaoOrigem.MetaCarboidratosG, fator),
                MetaGordurasG = EscalarNullable(refeicaoOrigem.MetaGordurasG, fator),
                MetaFibrasG = EscalarNullable(refeicaoOrigem.MetaFibrasG, fator)
            };

            foreach (var itemOrigem in refeicaoOrigem.Itens)
            {
                var item = new ItemRefeicaoPlano
                {
                    RefeicaoPlanoAlimentarId = refeicao.Id,
                    AlimentoId = itemOrigem.AlimentoId,
                    Quantidade = EscalarQuantidade(itemOrigem.Quantidade, fator),
                    Unidade = itemOrigem.Unidade,
                    QuantidadeGramas = EscalarQuantidade(itemOrigem.QuantidadeGramas, fator),
                    Observacao = itemOrigem.Observacao
                };

                foreach (var subOrigem in itemOrigem.Substituicoes)
                {
                    item.Substituicoes.Add(new SubstituicaoItemRefeicao
                    {
                        ItemRefeicaoPlanoId = item.Id,
                        AlimentoId = subOrigem.AlimentoId,
                        Quantidade = EscalarQuantidade(subOrigem.Quantidade, fator),
                        Unidade = subOrigem.Unidade,
                        QuantidadeGramas = EscalarQuantidade(subOrigem.QuantidadeGramas, fator),
                        Observacao = subOrigem.Observacao
                    });
                }

                refeicao.Itens.Add(item);
            }

            novo.Refeicoes.Add(refeicao);
        }

        if (request.ConcluirPlanoAnterior)
        {
            origem.Status = "Concluido";
            origem.DataFim ??= request.DataInicio.AddDays(-1);
            origem.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.PlanosAlimentares.Add(novo);
        Auditar("DUPLICATE_SCALE", novo, null, new
        {
            OrigemId = origem.Id,
            RaizId = raizId,
            novo.Versao,
            novo.AjustePercentual,
            CaloriasOriginais = atual.Calorias,
            CaloriasProjetadas = EscalarTotais(atual, fator).Calorias,
            request.ConcluirPlanoAnterior
        });

        await db.SaveChangesAsync(ct);

        var criado = await QueryCompleta().FirstAsync(x => x.Id == novo.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = novo.Id }, ToResponse(criado));
    }

    [HttpPost("api/planos-alimentares/{id:guid}/status/{status}")]
    public async Task<ActionResult<PlanoAlimentarResponse>> SetStatus(Guid id, string status, CancellationToken ct)
    {
        var item = await db.PlanosAlimentares.Include(x => x.Paciente).FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Plano alimentar nao encontrado." });
        var novo = NormalizarStatus(status); if (novo is not ("Ativo" or "Inativo" or "Concluido")) return BadRequest(new { message = "Status permitido: Ativo, Inativo ou Concluido." });
        var antes = new { item.Status }; item.Status = novo; item.UpdatedAtUtc = DateTime.UtcNow; Auditar("STATUS", item, antes, new { item.Status }); await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == id, ct); return Ok(ToResponse(atualizado));
    }

    private async Task<string?> Validar(Guid pacienteId, UpsertPlanoAlimentarRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct)) return "Paciente nao encontrado ou inativo.";
        if (string.IsNullOrWhiteSpace(request.Nome)) return "Nome do plano e obrigatorio.";
        if (request.DataFim.HasValue && request.DataFim.Value < request.DataInicio) return "Data final nao pode ser anterior a data inicial.";
        var status = NormalizarStatus(request.Status); if (status is not ("Ativo" or "Inativo" or "Concluido")) return "Status permitido: Ativo, Inativo ou Concluido.";
        var erroMetas = ValidarMetas(request.MetaCalorias, request.MetaProteinasG, request.MetaCarboidratosG, request.MetaGordurasG, request.MetaFibrasG);
        if (erroMetas is not null) return erroMetas;
        if (request.Refeicoes is null || request.Refeicoes.Count == 0) return "Informe ao menos uma refeicao.";
        if (request.Refeicoes.Any(x => string.IsNullOrWhiteSpace(x.Nome))) return "Todas as refeicoes devem possuir nome.";
        foreach (var refeicao in request.Refeicoes)
        {
            var erroMetaRefeicao = ValidarMetas(
                refeicao.MetaCalorias,
                refeicao.MetaProteinasG,
                refeicao.MetaCarboidratosG,
                refeicao.MetaGordurasG,
                refeicao.MetaFibrasG);
            if (erroMetaRefeicao is not null)
                return $"Refeicao '{refeicao.Nome}': {erroMetaRefeicao}";
        }
        var itens = request.Refeicoes.SelectMany(x => x.Itens ?? Array.Empty<ItemRefeicaoPlanoRequest>()).ToList();
        if (itens.Count == 0) return "Informe ao menos um alimento no plano.";
        if (itens.Any(x => x.Quantidade <= 0 || x.QuantidadeGramas <= 0 || string.IsNullOrWhiteSpace(x.Unidade))) return "Quantidade, unidade e quantidadeGramas devem ser validas em todos os itens.";
        var subs = itens.SelectMany(x => x.Substituicoes ?? Array.Empty<SubstituicaoPlanoRequest>()).ToList();
        if (subs.Any(x => x.Quantidade <= 0 || x.QuantidadeGramas <= 0 || string.IsNullOrWhiteSpace(x.Unidade))) return "Substituicoes devem possuir quantidade, unidade e quantidadeGramas validas.";
        var ids = itens.Select(x => x.AlimentoId).Concat(subs.Select(x => x.AlimentoId)).Distinct().ToArray();
        var validos = await db.Alimentos.CountAsync(x => ids.Contains(x.Id) && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
        if (validos != ids.Length) return "Um ou mais alimentos nao existem, pertencem a outra organizacao ou estao inativos.";
        return null;
    }

    private async Task MontarRefeicoes(PlanoAlimentar plano, IReadOnlyCollection<RefeicaoPlanoRequest> requests, CancellationToken ct)
    {
        var ids = requests.SelectMany(x => x.Itens).Select(x => x.AlimentoId).Concat(requests.SelectMany(x => x.Itens).SelectMany(x => x.Substituicoes ?? Array.Empty<SubstituicaoPlanoRequest>()).Select(x => x.AlimentoId)).Distinct().ToArray();
        var alimentos = await db.Alimentos.Where(x => ids.Contains(x.Id) && x.OrganizacaoId == currentUser.OrganizationId).ToDictionaryAsync(x => x.Id, ct);
        foreach (var r in requests.OrderBy(x => x.Ordem))
        {
            var refeicao = new RefeicaoPlanoAlimentar
            {
                PlanoAlimentarId = plano.Id,
                Nome = r.Nome.Trim(),
                Horario = r.Horario,
                Ordem = r.Ordem,
                Observacoes = Limpar(r.Observacoes),
                MetaCalorias = r.MetaCalorias,
                MetaProteinasG = r.MetaProteinasG,
                MetaCarboidratosG = r.MetaCarboidratosG,
                MetaGordurasG = r.MetaGordurasG,
                MetaFibrasG = r.MetaFibrasG
            };
            foreach (var i in r.Itens)
            {
                var item = new ItemRefeicaoPlano { RefeicaoPlanoAlimentarId = refeicao.Id, AlimentoId = i.AlimentoId, Alimento = alimentos[i.AlimentoId], Quantidade = i.Quantidade, Unidade = i.Unidade.Trim(), QuantidadeGramas = i.QuantidadeGramas, Observacao = Limpar(i.Observacao) };
                foreach (var s in i.Substituicoes ?? Array.Empty<SubstituicaoPlanoRequest>()) item.Substituicoes.Add(new SubstituicaoItemRefeicao { ItemRefeicaoPlanoId = item.Id, AlimentoId = s.AlimentoId, Alimento = alimentos[s.AlimentoId], Quantidade = s.Quantidade, Unidade = s.Unidade.Trim(), QuantidadeGramas = s.QuantidadeGramas, Observacao = Limpar(s.Observacao) });
                refeicao.Itens.Add(item);
            }
            plano.Refeicoes.Add(refeicao);
        }
    }

    private IQueryable<PlanoAlimentar> QueryCompleta() => db.PlanosAlimentares.AsNoTracking().Include(x => x.Paciente).Include(x => x.Profissional).Include(x => x.Refeicoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Alimento).Include(x => x.Refeicoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Substituicoes).ThenInclude(x => x.Alimento);
    private IQueryable<PlanoAlimentar> QueryCompletaTracking() => db.PlanosAlimentares.Include(x => x.Paciente).Include(x => x.Profissional).Include(x => x.Refeicoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Alimento).Include(x => x.Refeicoes).ThenInclude(x => x.Itens).ThenInclude(x => x.Substituicoes).ThenInclude(x => x.Alimento);
    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) => await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct) => await db.Pacientes.AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);
    private void Auditar(string acao, PlanoAlimentar item, object? antes, object? depois) => db.AuditLogs.Add(new AuditLog { OrganizacaoId = currentUser.OrganizationId, UsuarioId = currentUser.UserId, Acao = acao, Entidade = nameof(PlanoAlimentar), EntidadeId = item.Id.ToString(), DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes), DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois), IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() });
    private static object Snapshot(PlanoAlimentar x) => new { x.Id, x.PacienteId, x.ProfissionalId, x.Nome, x.DataInicio, x.DataFim, x.Status, x.PlanoOrigemId, x.Versao, x.AjustePercentual, x.MetaCalorias, x.MetaProteinasG, x.MetaCarboidratosG, x.MetaGordurasG, x.MetaFibrasG, Refeicoes = x.Refeicoes.Select(r => new { r.Nome, r.Horario, r.Ordem, r.MetaCalorias, r.MetaProteinasG, r.MetaCarboidratosG, r.MetaGordurasG, r.MetaFibrasG, Itens = r.Itens.Select(i => new { i.AlimentoId, i.Quantidade, i.Unidade, i.QuantidadeGramas, Substituicoes = i.Substituicoes.Select(s => new { s.AlimentoId, s.Quantidade, s.Unidade, s.QuantidadeGramas }) }) }) };
    private static PlanoAlimentarResponse ToResponse(PlanoAlimentar x)
    {
        var refeicoes = x.Refeicoes.OrderBy(r => r.Ordem).Select(r =>
        {
            var itens = r.Itens.Select(i =>
            {
                var total = Calcular(i.Alimento, i.QuantidadeGramas);
                var subs = i.Substituicoes.Select(s => new SubstituicaoPlanoResponse(
                    s.Id, s.AlimentoId, s.Alimento.Nome, s.Quantidade, s.Unidade,
                    s.QuantidadeGramas, s.Observacao, Calcular(s.Alimento, s.QuantidadeGramas))).ToList();
                return new ItemRefeicaoPlanoResponse(
                    i.Id, i.AlimentoId, i.Alimento.Nome, i.Quantidade, i.Unidade,
                    i.QuantidadeGramas, i.Observacao, total, subs);
            }).ToList();

            var totalRefeicao = Somar(itens.Select(i => i.Totais));
            var metas = new MetasNutricionaisResponse(
                r.MetaCalorias,
                r.MetaProteinasG,
                r.MetaCarboidratosG,
                r.MetaGordurasG,
                r.MetaFibrasG);
            var desvios = new DesviosNutricionaisResponse(
                Desvio(totalRefeicao.Calorias, r.MetaCalorias),
                Desvio(totalRefeicao.ProteinasG, r.MetaProteinasG),
                Desvio(totalRefeicao.CarboidratosG, r.MetaCarboidratosG),
                Desvio(totalRefeicao.GordurasG, r.MetaGordurasG),
                Desvio(totalRefeicao.FibrasG, r.MetaFibrasG));

            return new RefeicaoPlanoResponse(
                r.Id, r.Nome, r.Horario, r.Ordem, r.Observacoes,
                metas, desvios, totalRefeicao, itens);
        }).ToList();
        return new PlanoAlimentarResponse(x.Id, x.PacienteId, x.ProfissionalId, x.Profissional.Nome, x.Nome, x.DataInicio, x.DataFim, x.Status, x.Observacoes, x.PlanoOrigemId, x.Versao, x.AjustePercentual, x.MetaCalorias, x.MetaProteinasG, x.MetaCarboidratosG, x.MetaGordurasG, x.MetaFibrasG, Somar(refeicoes.Select(r => r.Totais)), refeicoes, x.CreatedAtUtc, x.UpdatedAtUtc);
    }
    private static TotaisNutricionaisResponse Calcular(Alimento a, decimal gramas) { var f = gramas / 100m; return new(Math.Round(a.CaloriasPor100g * f, 2), Math.Round(a.ProteinasPor100g * f, 2), Math.Round(a.CarboidratosPor100g * f, 2), Math.Round(a.GordurasPor100g * f, 2), Math.Round(a.FibrasPor100g * f, 2)); }
    private static TotaisNutricionaisResponse Somar(IEnumerable<TotaisNutricionaisResponse> t) => new(Math.Round(t.Sum(x => x.Calorias), 2), Math.Round(t.Sum(x => x.ProteinasG), 2), Math.Round(t.Sum(x => x.CarboidratosG), 2), Math.Round(t.Sum(x => x.GordurasG), 2), Math.Round(t.Sum(x => x.FibrasG), 2));
    private static string? ValidarMetas(
        decimal? calorias,
        decimal? proteinas,
        decimal? carboidratos,
        decimal? gorduras,
        decimal? fibras)
    {
        if (calorias.HasValue && calorias.Value <= 0)
            return "Meta de calorias deve ser maior que zero.";
        if (proteinas.HasValue && proteinas.Value < 0)
            return "Meta de proteinas nao pode ser negativa.";
        if (carboidratos.HasValue && carboidratos.Value < 0)
            return "Meta de carboidratos nao pode ser negativa.";
        if (gorduras.HasValue && gorduras.Value < 0)
            return "Meta de gorduras nao pode ser negativa.";
        if (fibras.HasValue && fibras.Value < 0)
            return "Meta de fibras nao pode ser negativa.";
        return null;
    }

    private static decimal? Desvio(decimal prescrito, decimal? meta) =>
        meta.HasValue ? Math.Round(prescrito - meta.Value, 2) : null;

    private static decimal Percentual(decimal parte, decimal total) =>
        total <= 0 ? 0m : Math.Round((parte / total) * 100m, 1);

    private static decimal? EscalarNullable(decimal? valor, decimal fator) =>
        valor.HasValue ? EscalarQuantidade(valor.Value, fator) : null;

    private static bool TemMetaPlano(PlanoAlimentar plano) =>
        plano.MetaCalorias.HasValue ||
        plano.MetaProteinasG.HasValue ||
        plano.MetaCarboidratosG.HasValue ||
        plano.MetaGordurasG.HasValue ||
        plano.MetaFibrasG.HasValue;

    private static decimal? PercentualMeta(decimal? meta, decimal fator) =>
        meta.HasValue
            ? Math.Round(meta.Value * fator, 2, MidpointRounding.AwayFromZero)
            : null;

    private static string? ResolverAjuste(
        decimal caloriasAtuais,
        decimal? percentual,
        decimal? caloriasAlvo,
        out decimal ajustePercentual,
        out decimal fator)
    {
        ajustePercentual = 0m;
        fator = 1m;

        if (percentual.HasValue && caloriasAlvo.HasValue)
            return "Informe ajustePercentual ou caloriasAlvo, nao os dois.";

        if (caloriasAlvo.HasValue)
        {
            if (caloriasAtuais <= 0)
                return "Nao e possivel calcular meta calorica porque o plano atual nao possui calorias calculaveis.";
            if (caloriasAlvo.Value <= 0)
                return "Calorias alvo devem ser maiores que zero.";

            fator = caloriasAlvo.Value / caloriasAtuais;
            ajustePercentual = Math.Round((fator - 1m) * 100m, 2);
        }
        else
        {
            ajustePercentual = percentual ?? 0m;
            fator = 1m + (ajustePercentual / 100m);
        }

        if (ajustePercentual < -50m || ajustePercentual > 100m)
            return "O ajuste permitido nesta versao deve ficar entre -50% e +100%.";

        if (fator <= 0)
            return "O fator de ajuste deve ser maior que zero.";

        return null;
    }

    private static decimal EscalarQuantidade(decimal valor, decimal fator) =>
        Math.Round(valor * fator, 2, MidpointRounding.AwayFromZero);

    private static TotaisNutricionaisResponse EscalarTotais(
        TotaisNutricionaisResponse atual,
        decimal fator) => new(
            EscalarQuantidade(atual.Calorias, fator),
            EscalarQuantidade(atual.ProteinasG, fator),
            EscalarQuantidade(atual.CarboidratosG, fator),
            EscalarQuantidade(atual.GordurasG, fator),
            EscalarQuantidade(atual.FibrasG, fator));

    private static string NormalizarStatus(string? s) => string.IsNullOrWhiteSpace(s) ? "Ativo" : char.ToUpperInvariant(s.Trim()[0]) + s.Trim()[1..].ToLowerInvariant();
    private static string? Limpar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
