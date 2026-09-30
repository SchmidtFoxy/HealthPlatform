using HealthPlatform.Api.Contracts.Explore;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Policy = "PatientOnly")]
[Route("api/portal/me/explore")]
public sealed class ExplorePacienteController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ExploreFoundationResponse>> Get(CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .Where(x => x.UsuarioId == currentUser.UserId &&
                        x.OrganizacaoId == currentUser.OrganizationId &&
                        x.Ativo)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var plano = await db.PlanosTreino.AsNoTracking()
            .Where(x => x.PacienteId == paciente.Id && x.Status == "Ativo")
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync(ct);

        var ciclo = await db.CiclosEsportivosPaciente.AsNoTracking()
            .Where(x => x.PacienteId == paciente.Id && x.Status == "Ativo")
            .OrderByDescending(x => x.DataInicio)
            .Select(x => new { x.Nome, x.Objetivo })
            .FirstOrDefaultAsync(ct);

        var anamnese = await db.Anamneses.AsNoTracking()
            .Where(x => x.PacienteId == paciente.Id)
            .OrderByDescending(x => x.DataUtc)
            .Select(x => new
            {
                x.AtividadeFisica,
                x.AtividadeFisicaDiasSemana
            })
            .FirstOrDefaultAsync(ct);

        var caminhos = new[]
        {
            new ExploreCaminhoResponse(
                "start-a-sport",
                "Começar um esporte",
                "Descubra fundamentos e caminhos iniciais sem precisar saber por onde começar.",
                ["Quero experimentar", "Estou começando"],
                ["Caminhada", "Corrida", "Ciclismo", "Calistenia"],
                "Exploracao"),
            new ExploreCaminhoResponse(
                "home-movement",
                "Mover em casa",
                "Encontre opções compatíveis com pouco espaço e recursos simples.",
                ["Casa", "Pouco equipamento"],
                ["Calistenia", "Mobilidade", "Condicionamento"],
                "Contexto"),
            new ExploreCaminhoResponse(
                "quick-movement",
                "Tenho pouco tempo",
                "Organize possibilidades curtas de movimento para dias apertados.",
                ["Pouco tempo", "Rotina corrida"],
                ["Caminhada", "Mobilidade", "Condicionamento"],
                "Contexto"),
            new ExploreCaminhoResponse(
                "outdoor",
                "Quero ir para fora",
                "Explore modalidades e movimento em ambientes externos.",
                ["Rua", "Parque", "Outdoor"],
                ["Caminhada", "Corrida", "Ciclismo"],
                "Exploracao"),
            new ExploreCaminhoResponse(
                "learn-fundamentals",
                "Aprender fundamentos",
                "Veja a base de uma modalidade antes de pensar em intensidade ou performance.",
                ["Aprender", "Iniciante"],
                ["Musculação", "Corrida", "Calistenia", "Mobilidade"],
                "Educacao")
        };

        return Ok(new ExploreFoundationResponse(
            plano,
            ciclo?.Nome,
            ciclo?.Objetivo,
            anamnese?.AtividadeFisica,
            anamnese?.AtividadeFisicaDiasSemana,
            caminhos,
            "Explore organiza possibilidades. Ele nao substitui o plano profissional, nao libera atividade clinicamente contraindicada e nao prescreve intensidade automaticamente."));
    }
}
