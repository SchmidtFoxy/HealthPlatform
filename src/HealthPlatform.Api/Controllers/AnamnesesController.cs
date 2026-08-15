using System.Text.Json;
using HealthPlatform.Api.Contracts.Anamneses;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize]
public class AnamnesesController(AppDbContext db, CurrentUser currentUser, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    public sealed record EvolucaoHabitosPontoResponse(
        Guid AnamneseId,
        DateTime DataUtc,
        decimal? SonoHorasMedia,
        string? SonoQualidade,
        bool? DespertaDuranteNoite,
        int? EstresseNivel,
        string? AtividadeFisica,
        int? AtividadeFisicaDiasSemana,
        decimal? AguaLitrosDia);

    public sealed record VariacaoHabitosResponse(
        decimal? SonoHorasMedia,
        int? EstresseNivel,
        int? AtividadeFisicaDiasSemana,
        decimal? AguaLitrosDia);

    public sealed record EvolucaoHabitosResponse(
        Guid PacienteId,
        int Total,
        EvolucaoHabitosPontoResponse? Atual,
        EvolucaoHabitosPontoResponse? Anterior,
        VariacaoHabitosResponse VariacaoDesdeAnterior,
        IReadOnlyCollection<EvolucaoHabitosPontoResponse> Itens);

    [HttpGet("api/pacientes/{pacienteId:guid}/anamneses")]
    public async Task<ActionResult<IReadOnlyCollection<AnamneseResponse>>> GetByPaciente(Guid pacienteId, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var itens = await QueryCompleta()
            .Where(x => x.PacienteId == pacienteId && x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataUtc)
            .ToListAsync(ct);
        return Ok(itens.Select(ToResponse).ToList());
    }

    [HttpGet("api/pacientes/{pacienteId:guid}/evolucao-habitos")]
    public async Task<ActionResult<EvolucaoHabitosResponse>> GetEvolucaoHabitos(
        Guid pacienteId,
        [FromQuery] int limite = 24,
        CancellationToken ct = default)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        limite = Math.Clamp(limite, 2, 60);

        var itens = await db.Anamneses.AsNoTracking()
            .Where(x =>
                x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId)
            .OrderByDescending(x => x.DataUtc)
            .Take(limite)
            .Select(x => new EvolucaoHabitosPontoResponse(
                x.Id,
                x.DataUtc,
                x.SonoHorasMedia,
                x.SonoQualidade,
                x.DespertaDuranteNoite,
                x.EstresseNivel,
                x.AtividadeFisica,
                x.AtividadeFisicaDiasSemana,
                x.AguaLitrosDia))
            .ToListAsync(ct);

        itens.Reverse();

        var atual = itens.Count > 0 ? itens[^1] : null;
        var anterior = itens.Count > 1 ? itens[^2] : null;

        var variacao = new VariacaoHabitosResponse(
            Diferenca(atual?.SonoHorasMedia, anterior?.SonoHorasMedia),
            Diferenca(atual?.EstresseNivel, anterior?.EstresseNivel),
            Diferenca(atual?.AtividadeFisicaDiasSemana, anterior?.AtividadeFisicaDiasSemana),
            Diferenca(atual?.AguaLitrosDia, anterior?.AguaLitrosDia));

        return Ok(new EvolucaoHabitosResponse(
            pacienteId,
            itens.Count,
            atual,
            anterior,
            variacao,
            itens));
    }

    [HttpGet("api/anamneses/{id:guid}")]
    public async Task<ActionResult<AnamneseResponse>> GetById(Guid id, CancellationToken ct)
    {
        var item = await QueryCompleta().FirstOrDefaultAsync(x =>
            x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        return item is null ? NotFound(new { message = "Anamnese nao encontrada." }) : Ok(ToResponse(item));
    }

    [HttpPost("api/pacientes/{pacienteId:guid}/anamneses")]
    public async Task<ActionResult<AnamneseResponse>> Create(Guid pacienteId, UpsertAnamneseRequest request, CancellationToken ct)
    {
        if (!await PacienteExiste(pacienteId, ct))
            return NotFound(new { message = "Paciente nao encontrado." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null)
            return Conflict(new { message = "Cadastre seu perfil profissional antes de registrar anamneses." });

        var erro = await ValidarRequest(pacienteId, request, null, profissional.Id, ct);
        if (erro is not null) return erro;

        var item = new Anamnese { PacienteId = pacienteId, ProfissionalId = profissional.Id };
        Aplicar(item, request);
        db.Anamneses.Add(item);

        var erroRespostas = await SincronizarRespostas(item, request.RespostasPersonalizadas, profissional.Id, ct);
        if (erroRespostas is not null) return erroRespostas;

        AdicionarAuditoria("CREATE", item, null, Snapshot(item));
        await db.SaveChangesAsync(ct);

        var criado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(criado));
    }

    [HttpPut("api/anamneses/{id:guid}")]
    public async Task<ActionResult<AnamneseResponse>> Update(Guid id, UpsertAnamneseRequest request, CancellationToken ct)
    {
        var item = await db.Anamneses.Include(x => x.RespostasPersonalizadas)
            .FirstOrDefaultAsync(x => x.Id == id && x.Paciente.OrganizacaoId == currentUser.OrganizationId, ct);
        if (item is null) return NotFound(new { message = "Anamnese nao encontrada." });

        var profissional = await GetProfissionalAtual(ct);
        if (profissional is null || item.ProfissionalId != profissional.Id) return Forbid();

        var erro = await ValidarRequest(item.PacienteId, request, item.Id, profissional.Id, ct);
        if (erro is not null) return erro;

        var antes = Snapshot(item);
        Aplicar(item, request);
        var erroRespostas = await SincronizarRespostas(item, request.RespostasPersonalizadas, profissional.Id, ct);
        if (erroRespostas is not null) return erroRespostas;

        AdicionarAuditoria("UPDATE", item, antes, Snapshot(item));
        await db.SaveChangesAsync(ct);
        var atualizado = await QueryCompleta().FirstAsync(x => x.Id == item.Id, ct);
        return Ok(ToResponse(atualizado));
    }

    private IQueryable<Anamnese> QueryCompleta() => db.Anamneses.AsNoTracking()
        .Include(x => x.Profissional)
        .Include(x => x.RespostasPersonalizadas)
            .ThenInclude(x => x.PerguntaAnamnese);

    private async Task<ActionResult?> ValidarRequest(Guid pacienteId, UpsertAnamneseRequest request, Guid? anamneseAtualId, Guid profissionalId, CancellationToken ct)
    {
        if (request.SonoHorasMedia < 0 || request.SonoHorasMedia > 24)
            return BadRequest(new { message = "Horas medias de sono devem estar entre 0 e 24." });
        if (request.EstresseNivel < 0 || request.EstresseNivel > 10)
            return BadRequest(new { message = "Nivel de estresse deve estar entre 0 e 10." });
        if (request.AtividadeFisicaDiasSemana < 0 || request.AtividadeFisicaDiasSemana > 7)
            return BadRequest(new { message = "Dias de atividade fisica devem estar entre 0 e 7." });
        if (request.AguaLitrosDia < 0 || request.AguaLitrosDia > 30)
            return BadRequest(new { message = "Consumo diario de agua deve estar entre 0 e 30 litros." });

        if (request.ConsultaId.HasValue)
        {
            var consultaValida = await db.Consultas.AnyAsync(x =>
                x.Id == request.ConsultaId && x.PacienteId == pacienteId &&
                x.Paciente.OrganizacaoId == currentUser.OrganizationId && x.ProfissionalId == profissionalId, ct);
            if (!consultaValida)
                return BadRequest(new { message = "A consulta informada nao pertence ao paciente/profissional atual." });

            var ocupada = await db.Anamneses.AnyAsync(x =>
                x.ConsultaId == request.ConsultaId && (!anamneseAtualId.HasValue || x.Id != anamneseAtualId), ct);
            if (ocupada)
                return Conflict(new { message = "Esta consulta ja possui uma anamnese vinculada." });
        }
        return null;
    }

    private async Task<ActionResult?> SincronizarRespostas(Anamnese item, IReadOnlyCollection<RespostaAnamneseRequest>? respostas, Guid profissionalId, CancellationToken ct)
    {
        if (respostas is null) return null;
        var duplicadas = respostas.GroupBy(x => x.PerguntaId).Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        if (duplicadas.Length > 0)
            return BadRequest(new { message = "Cada pergunta personalizada pode possuir apenas uma resposta." });

        var ids = respostas.Select(x => x.PerguntaId).Distinct().ToArray();
        var validas = await db.PerguntasAnamnese.Where(x => ids.Contains(x.Id) && x.OrganizacaoId == currentUser.OrganizationId &&
            x.ProfissionalId == profissionalId && x.Ativa).Select(x => x.Id).ToListAsync(ct);
        if (validas.Count != ids.Length)
            return BadRequest(new { message = "Uma ou mais perguntas personalizadas sao invalidas ou estao inativas." });

        db.RespostasAnamnesePersonalizadas.RemoveRange(item.RespostasPersonalizadas);
        item.RespostasPersonalizadas.Clear();
        foreach (var resposta in respostas)
            item.RespostasPersonalizadas.Add(new RespostaAnamnesePersonalizada
            {
                AnamneseId = item.Id,
                PerguntaAnamneseId = resposta.PerguntaId,
                Resposta = Normalizar(resposta.Resposta)
            });
        return null;
    }

    private static void Aplicar(Anamnese x, UpsertAnamneseRequest r)
    {
        x.ConsultaId = r.ConsultaId;
        x.DataUtc = (r.DataUtc ?? DateTime.UtcNow).ToUniversalTime();
        x.ObjetivoAcompanhamento = Normalizar(r.ObjetivoAcompanhamento);
        x.HistoricoDoencas = Normalizar(r.HistoricoDoencas);
        x.HistoricoFamiliar = Normalizar(r.HistoricoFamiliar);
        x.Cirurgias = Normalizar(r.Cirurgias);
        x.Alergias = Normalizar(r.Alergias);
        x.Medicamentos = Normalizar(r.Medicamentos);
        x.Suplementos = Normalizar(r.Suplementos);
        x.Tabagismo = Normalizar(r.Tabagismo);
        x.Etilismo = Normalizar(r.Etilismo);
        x.SonoHorasMedia = r.SonoHorasMedia;
        x.SonoQualidade = Normalizar(r.SonoQualidade);
        x.DespertaDuranteNoite = r.DespertaDuranteNoite;
        x.EstresseNivel = r.EstresseNivel;
        x.AtividadeFisica = Normalizar(r.AtividadeFisica);
        x.AtividadeFisicaDiasSemana = r.AtividadeFisicaDiasSemana;
        x.HabitoIntestinal = Normalizar(r.HabitoIntestinal);
        x.AguaLitrosDia = r.AguaLitrosDia;
        x.Observacoes = Normalizar(r.Observacoes);
    }

    private async Task<bool> PacienteExiste(Guid id, CancellationToken ct) =>
        await db.Pacientes.AnyAsync(x => x.Id == id && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private async Task<Profissional?> GetProfissionalAtual(CancellationToken ct) =>
        await db.Profissionais.FirstOrDefaultAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

    private void AdicionarAuditoria(string acao, Anamnese item, object? antes, object? depois) =>
        db.AuditLogs.Add(new AuditLog
        {
            OrganizacaoId = currentUser.OrganizationId,
            UsuarioId = currentUser.UserId,
            Acao = acao,
            Entidade = nameof(Anamnese),
            EntidadeId = item.Id.ToString(),
            DadosAnterioresJson = antes is null ? null : JsonSerializer.Serialize(antes),
            DadosNovosJson = depois is null ? null : JsonSerializer.Serialize(depois),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

    private static object Snapshot(Anamnese x) => new
    {
        x.Id, x.PacienteId, x.ProfissionalId, x.ConsultaId, x.DataUtc, x.ObjetivoAcompanhamento,
        x.HistoricoDoencas, x.HistoricoFamiliar, x.Cirurgias, x.Alergias, x.Medicamentos, x.Suplementos,
        x.Tabagismo, x.Etilismo, x.SonoHorasMedia, x.SonoQualidade, x.DespertaDuranteNoite, x.EstresseNivel,
        x.AtividadeFisica, x.AtividadeFisicaDiasSemana, x.HabitoIntestinal, x.AguaLitrosDia, x.Observacoes
    };

    private static AnamneseResponse ToResponse(Anamnese x) => new(
        x.Id, x.PacienteId, x.ProfissionalId, x.Profissional.Nome, x.ConsultaId, x.DataUtc,
        x.ObjetivoAcompanhamento, x.HistoricoDoencas, x.HistoricoFamiliar, x.Cirurgias, x.Alergias,
        x.Medicamentos, x.Suplementos, x.Tabagismo, x.Etilismo, x.SonoHorasMedia, x.SonoQualidade,
        x.DespertaDuranteNoite, x.EstresseNivel, x.AtividadeFisica, x.AtividadeFisicaDiasSemana,
        x.HabitoIntestinal, x.AguaLitrosDia, x.Observacoes,
        x.RespostasPersonalizadas.OrderBy(r => r.PerguntaAnamnese.Ordem).Select(r =>
            new RespostaAnamneseResponse(r.PerguntaAnamneseId, r.PerguntaAnamnese.Texto, r.PerguntaAnamnese.TipoResposta, r.Resposta)).ToList(),
        x.CreatedAtUtc, x.UpdatedAtUtc);

    private static decimal? Diferenca(decimal? atual, decimal? anterior) =>
        atual.HasValue && anterior.HasValue
            ? Math.Round(atual.Value - anterior.Value, 2)
            : null;

    private static int? Diferenca(int? atual, int? anterior) =>
        atual.HasValue && anterior.HasValue
            ? atual.Value - anterior.Value
            : null;

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
