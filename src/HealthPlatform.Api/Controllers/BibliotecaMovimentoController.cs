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
        [FromQuery] string? objetivo = null,
        [FromQuery] string? capacidade = null,
        [FromQuery] string? ambiente = null,
        [FromQuery] string? equipamento = null,
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

        var modelosSessao = await db.ModelosSessoesTreino.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.Categoria,
                x.Descricao
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

        if (!string.IsNullOrWhiteSpace(ambiente))
        {
            var filtroAmbiente = ambiente.Trim();
            defs = defs
                .Where(x => x.Ambientes.Any(a =>
                    a.Contains(filtroAmbiente, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(equipamento))
        {
            var filtroEquipamento = equipamento.Trim();
            defs = defs
                .Where(x =>
                    x.EquipamentosComuns.Any(e =>
                        e.Contains(filtroEquipamento, StringComparison.OrdinalIgnoreCase)) ||
                    exercicios.Any(ex =>
                        !string.IsNullOrWhiteSpace(ex.Equipamento) &&
                        ex.Equipamento.Contains(filtroEquipamento, StringComparison.OrdinalIgnoreCase) &&
                        Corresponde(ex.Nome, ex.GrupoMuscular, ex.Equipamento, ex.Descricao, x.Termos, [])))
                .ToArray();
        }

        var modalidades = defs.Select(mod =>
        {
            var objetivosDef = mod.Objetivos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(objetivo))
            {
                var filtroObjetivo = objetivo.Trim();
                objetivosDef = objetivosDef.Where(x =>
                    x.Codigo.Equals(filtroObjetivo, StringComparison.OrdinalIgnoreCase) ||
                    x.Nome.Contains(filtroObjetivo, StringComparison.OrdinalIgnoreCase));
            }

            var objetivos = objetivosDef.Select(obj =>
            {
                var capacidadesDef = obj.Capacidades.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(capacidade))
                {
                    var filtroCapacidade = capacidade.Trim();
                    capacidadesDef = capacidadesDef.Where(x =>
                        x.Codigo.Equals(filtroCapacidade, StringComparison.OrdinalIgnoreCase) ||
                        x.Nome.Contains(filtroCapacidade, StringComparison.OrdinalIgnoreCase));
                }

                var capacidades = capacidadesDef.Select(cap =>
                {
                    var vinculados = exercicios
                        .Where(ex =>
                            Corresponde(ex.Nome, ex.GrupoMuscular, ex.Equipamento, ex.Descricao, mod.Termos, cap.Termos) &&
                            (string.IsNullOrWhiteSpace(equipamento) ||
                             (!string.IsNullOrWhiteSpace(ex.Equipamento) &&
                              ex.Equipamento.Contains(equipamento.Trim(), StringComparison.OrdinalIgnoreCase))))
                        .Take(40)
                        .Select(ex => new BibliotecaMovimentoExercicioResponse(
                            ex.Id,
                            ex.Nome,
                            ex.GrupoMuscular,
                            ex.Equipamento,
                            ex.Descricao,
                            ex.VideoUrl))
                        .ToArray();

                    var sessoes = modelosSessao
                        .Where(modelo => CorrespondeSessao(
                            modelo.Nome,
                            modelo.Categoria,
                            modelo.Descricao,
                            mod.Termos,
                            obj.Nome,
                            cap.Termos))
                        .Take(20)
                        .Select(modelo => new BibliotecaMovimentoSessaoResponse(
                            modelo.Id,
                            modelo.Nome,
                            modelo.Categoria,
                            modelo.Descricao))
                        .ToArray();

                    var progressaoRegressao = CatalogoProgressao(cap.Codigo, cap.Nome);

                    return new BibliotecaMovimentoCapacidadeResponse(
                        cap.Codigo,
                        cap.Nome,
                        sessoes,
                        vinculados,
                        progressaoRegressao);
                }).ToArray();

                return new BibliotecaMovimentoObjetivoResponse(
                    obj.Codigo,
                    obj.Nome,
                    capacidades);
            })
            .Where(x => string.IsNullOrWhiteSpace(capacidade) || x.Capacidades.Count > 0)
            .ToArray();

            return new BibliotecaMovimentoModalidadeResponse(
                mod.Codigo,
                mod.Nome,
                mod.Descricao,
                mod.Ambientes,
                mod.EquipamentosComuns,
                objetivos);
        })
        .Where(x => string.IsNullOrWhiteSpace(objetivo) || x.Objetivos.Count > 0)
        .ToArray();

        return Ok(new BibliotecaMovimentoResponse(
            "Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão",
            modalidades.Length,
            exercicios.Count,
            modelosSessao.Count,
            modalidades,
            "Os exercícios vêm exclusivamente do catálogo profissional existente (`Exercicios`). A taxonomia organiza referências e não duplica movimentos.",
            "As sessões vêm exclusivamente de `ModelosSessoesTreino`, a mesma biblioteca reutilizável do Workout Builder. A taxonomia apenas organiza referências.",
            "A biblioteca descreve possibilidades de movimento. Seleção, progressão, regressão e prescrição continuam dependentes de contexto individual e julgamento profissional."));
    }

    private static IReadOnlyCollection<BibliotecaMovimentoProgressaoResponse> CatalogoProgressao(
        string capacidadeCodigo,
        string capacidadeNome)
    {
        var itens = new List<BibliotecaMovimentoProgressaoResponse>
        {
            new(
                "Controle",
                "Aumentar gradualmente amplitude, estabilidade ou exigencia tecnica quando a execucao estiver consistente.",
                "Reduzir amplitude, apoio, velocidade ou complexidade para recuperar controle do movimento.",
                "Usar como orientacao profissional; a biblioteca nao promove automaticamente o exercicio."),
            new(
                "Volume",
                "Aumentar series, repeticoes, tempo ou distancia de forma planejada.",
                "Reduzir series, repeticoes, tempo ou distancia preservando o objetivo da sessao.",
                "Alteracoes de volume devem acontecer na prescricao ou no planejamento profissional."),
            new(
                "Carga externa",
                "Aumentar carga somente quando houver tecnica consistente e contexto para progressao.",
                "Reduzir carga ou usar variante com menor resistencia quando o contexto pedir menor exigencia.",
                "Comparar carga somente dentro do mesmo exercicio e unidade; sem estimativa automatica de 1RM.")
        };

        var codigo = capacidadeCodigo.ToLowerInvariant();
        var nome = capacidadeNome.ToLowerInvariant();

        if (codigo.Contains("aerob") || nome.Contains("aerob") ||
            codigo.Contains("cardio") || nome.Contains("cardio") ||
            codigo.Contains("resistencia") || nome.Contains("resist"))
        {
            itens.Add(new(
                "Duracao e densidade",
                "Aumentar gradualmente duracao, distancia ou densidade de trabalho conforme planejamento.",
                "Reduzir duracao, distancia ou densidade mantendo uma dose executavel.",
                "A biblioteca nao define zonas, ritmos ou cargas internas automaticamente."));
        }

        if (codigo.Contains("veloc") || nome.Contains("veloc") ||
            codigo.Contains("potenc") || nome.Contains("potenc"))
        {
            itens.Add(new(
                "Velocidade",
                "Aumentar velocidade ou exigencia explosiva somente com tecnica e recuperacao adequadas.",
                "Reduzir velocidade, impacto ou complexidade para manter qualidade de movimento.",
                "Nao inferir prontidao esportiva, risco ou retorno ao esporte somente por esta taxonomia."));
        }

        if (codigo.Contains("mobil") || nome.Contains("mobil") ||
            codigo.Contains("quadril") || codigo.Contains("tornozelo") || codigo.Contains("ombro"))
        {
            itens.Add(new(
                "Amplitude",
                "Ampliar gradualmente a amplitude ativa quando houver controle e tolerancia.",
                "Trabalhar em amplitude menor e confortavel, com mais apoio ou controle.",
                "Dor, limitacao clinica ou retorno de lesao exigem avaliacao profissional; a biblioteca nao diagnostica."));
        }

        return itens;
    }

    private static bool CorrespondeSessao(
        string nome,
        string? categoria,
        string? descricao,
        IReadOnlyCollection<string> termosModalidade,
        string objetivo,
        IReadOnlyCollection<string> termosCapacidade)
    {
        var texto = $"{nome} {categoria} {descricao}".ToLowerInvariant();
        var objetivoNormalizado = objetivo.ToLowerInvariant();

        var capacidadeCombina = termosCapacidade.Any(x =>
            texto.Contains(x.ToLowerInvariant()));

        var modalidadeCombina = termosModalidade.Any(x =>
            texto.Contains(x.ToLowerInvariant()));

        var objetivoCombina = texto.Contains(objetivoNormalizado);

        // Sessões genéricas de treino resistido e mobilidade podem ser
        // reutilizadas quando o texto do modelo descreve a capacidade.
        return capacidadeCombina ||
               (modalidadeCombina && objetivoCombina) ||
               (modalidadeCombina && termosModalidade.Contains("forca")) ||
               (modalidadeCombina && termosModalidade.Contains("mobilidade"));
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
