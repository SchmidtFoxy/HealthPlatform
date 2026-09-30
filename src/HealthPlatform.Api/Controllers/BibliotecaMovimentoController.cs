using HealthPlatform.Api.Contracts.BibliotecaMovimento;
using HealthPlatform.Api.Services;
using HealthPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Medico,Nutricionista,Personal")]
[Route("api/biblioteca-movimento")]
public sealed class BibliotecaMovimentoController(
    AppDbContext db,
    CurrentUser currentUser) : ControllerBase
{
    private sealed record CapacidadeDef(string Codigo, string Nome, string[] Termos);
    private sealed record ObjetivoDef(string Codigo, string Nome, CapacidadeDef[] Capacidades);
    private sealed record ModalidadeDef(
        string Codigo,
        string Nome,
        string Descricao,
        string[] Ambientes,
        string[] EquipamentosComuns,
        string[] Termos,
        ObjetivoDef[] Objetivos);

    [HttpGet]
    public async Task<ActionResult<BibliotecaMovimentoResponse>> Get(
        [FromQuery] string? modalidade = null,
        CancellationToken ct = default)
    {
        var exercicios = await db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.GrupoMuscular,
                x.Equipamento,
                x.Descricao,
                x.VideoUrl
            })
            .ToListAsync(ct);

        var defs = CatalogoFundacao();

        if (!string.IsNullOrWhiteSpace(modalidade))
        {
            var codigo = modalidade.Trim().ToLowerInvariant();
            defs = defs
                .Where(x =>
                    x.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase) ||
                    x.Nome.Contains(modalidade.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var modalidades = defs.Select(mod =>
        {
            var objetivos = mod.Objetivos.Select(obj =>
            {
                var capacidades = obj.Capacidades.Select(cap =>
                {
                    var vinculados = exercicios
                        .Where(ex => Corresponde(ex.Nome, ex.GrupoMuscular, ex.Equipamento, ex.Descricao, mod.Termos, cap.Termos))
                        .Take(40)
                        .Select(ex => new BibliotecaMovimentoExercicioResponse(
                            ex.Id,
                            ex.Nome,
                            ex.GrupoMuscular,
                            ex.Equipamento,
                            ex.Descricao,
                            ex.VideoUrl))
                        .ToArray();

                    return new BibliotecaMovimentoCapacidadeResponse(
                        cap.Codigo,
                        cap.Nome,
                        vinculados);
                }).ToArray();

                return new BibliotecaMovimentoObjetivoResponse(
                    obj.Codigo,
                    obj.Nome,
                    capacidades);
            }).ToArray();

            return new BibliotecaMovimentoModalidadeResponse(
                mod.Codigo,
                mod.Nome,
                mod.Descricao,
                mod.Ambientes,
                mod.EquipamentosComuns,
                objetivos);
        }).ToArray();

        return Ok(new BibliotecaMovimentoResponse(
            "Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão",
            modalidades.Length,
            exercicios.Count,
            modalidades,
            "Os exercícios vêm exclusivamente do catálogo profissional existente (`Exercicios`). A taxonomia organiza referências e não duplica movimentos.",
            "A biblioteca descreve possibilidades de movimento. Seleção, progressão, regressão e prescrição continuam dependentes de contexto individual e julgamento profissional."));
    }

    private static bool Corresponde(
        string nome,
        string? grupo,
        string? equipamento,
        string? descricao,
        IReadOnlyCollection<string> termosModalidade,
        IReadOnlyCollection<string> termosCapacidade)
    {
        var texto = $"{nome} {grupo} {equipamento} {descricao}".ToLowerInvariant();

        var capacidadeCombina = termosCapacidade.Count == 0 ||
            termosCapacidade.Any(x => texto.Contains(x.ToLowerInvariant()));

        if (!capacidadeCombina)
            return false;

        if (termosModalidade.Count == 0)
            return true;

        var modalidadeCombina = termosModalidade.Any(x => texto.Contains(x.ToLowerInvariant()));

        // Musculação e mobilidade podem reutilizar movimentos genéricos do catálogo,
        // pois o exercício continua sendo a mesma fonte de verdade profissional.
        return modalidadeCombina ||
               termosModalidade.Contains("forca") ||
               termosModalidade.Contains("mobilidade");
    }

    private static ModalidadeDef[] CatalogoFundacao() =>
    [
        new(
            "musculacao",
            "Musculação",
            "Treino resistido com pesos, máquinas, cabos e peso corporal.",
            ["Academia", "Casa"],
            ["Barra", "Halteres", "Máquinas", "Cabos", "Peso corporal"],
            ["forca", "musculacao", "halter", "barra", "maquina", "cabo"],
            [
                new("hipertrofia", "Hipertrofia", [
                    new("forca", "Força", ["supino", "agach", "terra", "remada", "desenvolvimento", "leg press"]),
                    new("volume", "Volume muscular", ["rosca", "triceps", "extens", "flex", "elevação", "crucifixo"])
                ]),
                new("forca", "Força", [
                    new("forca-maxima", "Força máxima", ["supino", "agach", "terra", "remada", "desenvolvimento"])
                ])
            ]),
        new(
            "caminhada",
            "Caminhada",
            "Movimento cíclico acessível para saúde, condicionamento e rotina diária.",
            ["Rua", "Parque", "Esteira"],
            ["Tênis", "Esteira opcional"],
            ["caminhada", "esteira"],
            [
                new("saude", "Saúde e consistência", [
                    new("aerobica", "Capacidade aeróbica", ["caminhada", "esteira"])
                ])
            ]),
        new(
            "corrida",
            "Corrida",
            "Desenvolvimento de base aeróbica, técnica e tolerância progressiva à corrida.",
            ["Rua", "Pista", "Esteira"],
            ["Tênis", "Esteira opcional"],
            ["corrida", "correr", "esteira", "sprint"],
            [
                new("base", "Base aeróbica", [
                    new("aerobica", "Capacidade aeróbica", ["corrida", "esteira"])
                ]),
                new("performance", "Performance", [
                    new("velocidade", "Velocidade", ["sprint", "tiro", "corrida"]),
                    new("potencia", "Potência", ["salto", "sprint"])
                ])
            ]),
        new(
            "calistenia",
            "Calistenia",
            "Força e controle corporal usando o próprio corpo e apoios simples.",
            ["Casa", "Parque", "Academia"],
            ["Peso corporal", "Barra fixa", "Paralelas"],
            ["peso corporal", "barra fixa", "flexao", "flexão", "paralela"],
            [
                new("forca-corporal", "Força corporal", [
                    new("empurrar", "Empurrar", ["flexao", "flexão", "paralela"]),
                    new("puxar", "Puxar", ["barra fixa", "remada"]),
                    new("core", "Controle de tronco", ["prancha", "abdominal", "core"])
                ])
            ]),
        new(
            "mobilidade",
            "Mobilidade",
            "Movimentos voltados a amplitude, controle articular e preparação para atividade.",
            ["Casa", "Academia", "Campo", "Quadra"],
            ["Peso corporal", "Faixa elástica", "Rolo"],
            ["mobilidade"],
            [
                new("movimento", "Qualidade de movimento", [
                    new("quadril", "Mobilidade de quadril", ["quadril", "gluteo", "glúteo"]),
                    new("tornozelo", "Mobilidade de tornozelo", ["tornozelo", "panturrilha"]),
                    new("ombro", "Mobilidade de ombro", ["ombro", "escap", "manguito"])
                ])
            ]),
        new(
            "condicionamento",
            "Condicionamento",
            "Sessões gerais para capacidade cardiorrespiratória, resistência e trabalho global.",
            ["Academia", "Casa", "Outdoor"],
            ["Peso corporal", "Bike", "Remo", "Corda"],
            ["cardio", "burpee", "corda", "bike", "remo"],
            [
                new("resistencia", "Resistência", [
                    new("cardiorrespiratoria", "Cardiorrespiratória", ["cardio", "bike", "remo", "corda"]),
                    new("resistencia-muscular", "Resistência muscular", ["burpee", "agach", "flexao", "flexão"])
                ])
            ]),
        new(
            "ciclismo",
            "Ciclismo",
            "Pedalada indoor ou outdoor para base aeróbica, resistência e performance.",
            ["Rua", "Ciclovia", "Indoor"],
            ["Bicicleta", "Bike ergométrica"],
            ["bike", "bicicleta", "ciclismo"],
            [
                new("base", "Base aeróbica", [
                    new("aerobica", "Capacidade aeróbica", ["bike", "bicicleta"])
                ]),
                new("performance", "Performance", [
                    new("potencia", "Potência", ["bike", "bicicleta", "sprint"])
                ])
            ])
    ];
}
