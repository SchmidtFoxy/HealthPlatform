using HealthPlatform.Api.Contracts.Explore;
using HealthPlatform.Api.Services;
using HealthPlatform.Domain.Entities;
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

        var interessesDeclarados = await db.InteressesExplorePaciente.AsNoTracking()
            .Where(x => x.PacienteId == paciente.Id &&
                        x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Nome)
            .Select(x => new InteresseExploreResponse(
                x.Id,
                x.Codigo,
                x.Nome,
                x.Intencao,
                x.Origem,
                x.CreatedAtUtc))
            .ToArrayAsync(ct);

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
                "travel-mode",
                "Estou viajando",
                "Adapte a exploração ao espaço, aos recursos e à rotina temporária da viagem.",
                ["Viagem", "Rotina temporária"],
                ["Mobilidade", "Calistenia", "Caminhada", "Condicionamento"],
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
                "Educacao"),
            new ExploreCaminhoResponse(
                "sports-starter-packs",
                "Explorar Starter Packs",
                "Veja agrupamentos de referência por modalidade e objetivo usando sessões-modelo já existentes.",
                ["Explorar", "Referências"],
                ["Musculação", "Corrida", "Calistenia", "Mobilidade", "Ciclismo"],
                "Referencia"),
            new ExploreCaminhoResponse(
                "sports-expansion-i",
                "Esportes de quadra e campo",
                "Explore fundamentos e capacidades de Futebol, Futsal, Basquete, Vôlei, Tênis e Beach Tennis.",
                ["Campo", "Quadra", "Areia", "Raquete", "Coletivos"],
                ["Futebol", "Futsal", "Basquete", "Vôlei", "Tênis", "Beach Tennis"],
                "Esportes"),
            new ExploreCaminhoResponse(
                "sports-expansion-ii",
                "Água, combate e aventura",
                "Explore fundamentos e capacidades de Natação, Triathlon, Artes Marciais, Remo, Trekking e práticas recreativas.",
                ["Piscina", "Águas abertas", "Tatame", "Remo", "Trilha", "Recreativo"],
                ["Natação", "Triathlon", "Artes Marciais", "Remo", "Trekking", "Recreativos"],
                "Esportes")
        };

        return Ok(new ExploreFoundationResponse(
            plano,
            ciclo?.Nome,
            ciclo?.Objetivo,
            anamnese?.AtividadeFisica,
            anamnese?.AtividadeFisicaDiasSemana,
            caminhos,
            interessesDeclarados,
            "Explore organiza possibilidades. Ele nao substitui o plano profissional, nao libera atividade clinicamente contraindicada e nao prescreve intensidade automaticamente."));
    }
    [HttpGet("start-a-sport")]
    public async Task<ActionResult<StartSportResponse>> StartASport(CancellationToken ct = default)
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

        var anamnese = await db.Anamneses.AsNoTracking()
            .Where(x => x.PacienteId == paciente.Id)
            .OrderByDescending(x => x.DataUtc)
            .Select(x => new
            {
                x.AtividadeFisica,
                x.AtividadeFisicaDiasSemana
            })
            .FirstOrDefaultAsync(ct);

        var modalidades = new[]
        {
            new StartSportModalidadeResponse(
                "caminhada",
                "Caminhada",
                "Uma porta de entrada simples para criar familiaridade com movimento contínuo e rotina ativa.",
                ["Rua", "Parque", "Esteira"],
                ["Tênis confortável", "Rota segura"],
                [
                    new("Ritmo confortável", "Aprender a caminhar em um ritmo que permita perceber o ambiente e manter controle da respiração."),
                    new("Postura e passada", "Observar postura, apoio dos pés e uma passada natural sem buscar velocidade máxima."),
                    new("Regularidade", "Construir familiaridade com sair, caminhar e voltar antes de pensar em distância ou performance.")
                ],
                "Completar uma experiência de caminhada consciente e confortável.",
                "Explorar rotas, ambientes e preferências antes de aumentar exigência.",
                "Explore apresenta fundamentos; duração, ritmo e progressão continuam dependentes do seu contexto e de orientação profissional quando aplicável."),
            new StartSportModalidadeResponse(
                "corrida",
                "Corrida",
                "Conheça a lógica básica da corrida antes de pensar em pace, volume ou performance.",
                ["Rua", "Parque", "Pista", "Esteira"],
                ["Tênis adequado", "Espaço seguro"],
                [
                    new("Caminhar e correr", "Entender que começar pode envolver alternância entre caminhada e corrida sem obrigação de correr continuamente."),
                    new("Cadência natural", "Priorizar passos naturais e confortáveis, sem perseguir um número universal de cadência."),
                    new("Percepção de esforço", "Aprender a perceber como o corpo responde, sem usar um único sinal isolado como autorização para aumentar intensidade.")
                ],
                "Conhecer a sensação básica da corrida sem transformar o primeiro contato em teste.",
                "Aprender sobre técnica, ambiente e progressão antes de buscar metas de tempo.",
                "Explore não define pace, volume, zona ou intensidade de treino automaticamente."),
            new StartSportModalidadeResponse(
                "ciclismo",
                "Ciclismo",
                "Descubra os fundamentos do pedal com atenção a controle, ambiente e equipamento.",
                ["Ciclovia", "Parque", "Indoor"],
                ["Bicicleta ajustada", "Capacete", "Ambiente seguro"],
                [
                    new("Controle da bicicleta", "Familiarizar-se com frenagem, direção e mudanças de trajetória antes de buscar velocidade."),
                    new("Cadência confortável", "Perceber um giro controlado sem obrigação de perseguir cadência ou potência específicas."),
                    new("Segurança no ambiente", "Entender rota, sinalização e convivência com outros usuários antes de ampliar distância.")
                ],
                "Realizar um primeiro contato focado em controle e segurança.",
                "Explorar terreno, posição e familiaridade com a bicicleta antes de pensar em performance.",
                "Explore não define potência, cadência, distância ou intensidade automaticamente."),
            new StartSportModalidadeResponse(
                "calistenia",
                "Calistenia",
                "Use o próprio corpo para conhecer padrões básicos de força, controle e estabilidade.",
                ["Casa", "Parque", "Academia"],
                ["Peso corporal", "Barra ou apoio opcional"],
                [
                    new("Empurrar", "Conhecer variações de apoio e controle em movimentos de empurrar."),
                    new("Puxar", "Entender opções de puxada conforme os recursos disponíveis."),
                    new("Agachar e estabilizar", "Explorar padrões simples de membros inferiores e controle do tronco.")
                ],
                "Reconhecer quais padrões básicos são familiares e quais ainda precisam de orientação.",
                "Explorar regressões, apoios e técnica antes de adicionar complexidade.",
                "Explore não escolhe automaticamente exercício, volume, progressão ou regressão para o paciente."),
            new StartSportModalidadeResponse(
                "musculacao",
                "Musculação",
                "Entenda a estrutura de um treino resistido e os principais padrões antes de pensar em carga.",
                ["Academia", "Studio"],
                ["Máquinas ou pesos livres", "Ambiente orientado"],
                [
                    new("Padrões de movimento", "Reconhecer empurrar, puxar, agachar, dobrar quadril e estabilizar."),
                    new("Execução antes da carga", "Aprender a lógica do exercício e do equipamento antes de perseguir peso."),
                    new("Séries e repetições", "Entender a linguagem básica de uma sessão sem interpretar uma quantidade genérica como prescrição individual.")
                ],
                "Compreender a estrutura de uma sessão e identificar equipamentos básicos.",
                "Abrir o catálogo com um profissional e aprender os movimentos que fazem sentido para seu plano.",
                "Explore ensina conceitos, mas não define carga, séries, repetições ou técnicas para você automaticamente.")
        };

        var mensagem = string.IsNullOrWhiteSpace(plano)
            ? "Você não possui um plano de treino ativo nesta leitura. Explore pode ajudar a conhecer possibilidades, mas conhecer uma modalidade não equivale a receber uma prescrição."
            : $"Seu plano ativo é '{plano}'. Explore é complementar: conhecer outra modalidade não substitui o plano atual.";

        return Ok(new StartSportResponse(
            plano,
            anamnese?.AtividadeFisica,
            anamnese?.AtividadeFisicaDiasSemana,
            modalidades,
            mensagem,
            "Começar um esporte significa aprender e experimentar com contexto. O AESYN não declara aptidão clínica, não libera retorno ao esporte e não prescreve intensidade automaticamente."));
    }


    [HttpGet("beginner-journeys")]
    public async Task<ActionResult<BeginnerJourneysResponse>> BeginnerJourneys(
        [FromQuery] string? modalidade = null,
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var jornadas = new[]
        {
            new BeginnerJourneyResponse(
                "caminhada",
                "Caminhada",
                "Da curiosidade à familiaridade com caminhar",
                "Uma sequência educacional para entender ambiente, ritmo confortável e regularidade antes de pensar em distância.",
                [
                    new(1, "Conhecer o ambiente", "Perceber rota, superfície, segurança e recursos do local.", "Consigo identificar onde caminhar com conforto e segurança.", "Quando o ambiente deixou de ser uma dúvida."),
                    new(2, "Perceber o próprio ritmo", "Distinguir caminhar confortável de caminhar apressado.", "Consigo reconhecer um ritmo confortável sem perseguir velocidade.", "Quando consigo repetir a experiência com controle."),
                    new(3, "Criar familiaridade", "Entender preparação, saída, caminhada e retorno como uma rotina.", "Consigo organizar uma experiência simples sem depender de improviso.", "Quando a rotina já é compreensível e previsível.")
                ],
                "As etapas são marcos de familiaridade, não metas obrigatórias de distância, duração ou velocidade.",
                "Avançar de etapa não autoriza aumento automático de intensidade e não substitui orientação profissional."),
            new BeginnerJourneyResponse(
                "corrida",
                "Corrida",
                "Aprender a lógica da corrida antes de buscar pace",
                "Uma jornada para entender alternância, percepção de esforço e ambiente sem transformar o início em teste.",
                [
                    new(1, "Caminhar e correr", "Entender que alternar caminhada e corrida pode fazer parte do aprendizado.", "Sei diferenciar a sensação de caminhar da sensação de correr sem obrigação de continuidade.", "Quando a alternância deixou de parecer falha ou improviso."),
                    new(2, "Perceber esforço", "Reconhecer que velocidade e esforço não são a mesma coisa em todas as pessoas.", "Consigo descrever como me senti sem depender apenas de pace.", "Quando consigo observar a resposta do corpo com clareza."),
                    new(3, "Entender progressão", "Aprender que frequência, duração e velocidade são variáveis diferentes.", "Sei que aumentar uma variável não significa que todas devem aumentar.", "Quando consigo conversar sobre progressão sem usar apenas distância ou tempo.")
                ],
                "A jornada ensina conceitos; não define pace, zona, volume semanal ou frequência de treino.",
                "Nenhuma etapa declara aptidão clínica ou libera progressão de corrida."),
            new BeginnerJourneyResponse(
                "ciclismo",
                "Ciclismo",
                "Do controle da bicicleta à leitura do ambiente",
                "Uma jornada educacional sobre equipamento, controle, terreno e convivência no espaço.",
                [
                    new(1, "Conhecer a bicicleta", "Entender ajuste básico, freios, direção e pontos de contato.", "Sei identificar os controles essenciais antes de pedalar.", "Quando operar a bicicleta deixou de exigir tentativa e erro."),
                    new(2, "Controlar o movimento", "Aprender frenagem, curvas e mudanças de direção em ambiente previsível.", "Consigo explicar como reduzir velocidade e mudar direção com controle.", "Quando tenho familiaridade com controle básico."),
                    new(3, "Ler o ambiente", "Observar rota, sinalização, superfície e outros usuários.", "Consigo identificar riscos ambientais básicos antes de sair.", "Quando consigo planejar uma experiência simples com segurança contextual.")
                ],
                "A jornada não prescreve potência, cadência, distância ou terreno.",
                "Familiaridade com a bicicleta não equivale a liberação clínica ou domínio técnico completo."),
            new BeginnerJourneyResponse(
                "calistenia",
                "Calistenia",
                "Conhecer padrões básicos usando o próprio corpo",
                "Uma jornada para entender apoio, controle e padrões de movimento antes de buscar complexidade.",
                [
                    new(1, "Reconhecer padrões", "Diferenciar empurrar, puxar, agachar e estabilizar.", "Consigo identificar qual padrão um movimento representa.", "Quando os padrões deixam de parecer exercícios isolados."),
                    new(2, "Entender apoio e alavanca", "Perceber como apoio e posição mudam a exigência de um movimento.", "Consigo entender por que duas variações do mesmo padrão podem ser diferentes.", "Quando regressão deixa de significar 'falhar'."),
                    new(3, "Priorizar controle", "Entender execução consistente antes de complexidade.", "Consigo reconhecer que qualidade e controle vêm antes de variações mais difíceis.", "Quando consigo discutir progressão sem depender de desafio máximo.")
                ],
                "A jornada não escolhe automaticamente exercício, repetição, série ou progressão.",
                "Variações de apoio são informação educacional, não prescrição individual."),
            new BeginnerJourneyResponse(
                "musculacao",
                "Musculação",
                "Entender o treino resistido antes de perseguir carga",
                "Uma jornada para conhecer padrões, equipamentos e linguagem básica de uma sessão.",
                [
                    new(1, "Conhecer a linguagem", "Entender exercício, série, repetição, descanso e carga como conceitos distintos.", "Consigo ler uma ficha simples e saber o que cada campo significa.", "Quando a estrutura de uma sessão deixou de ser confusa."),
                    new(2, "Conhecer equipamentos", "Identificar máquinas, pesos livres e pontos básicos de ajuste.", "Consigo reconhecer equipamentos sem assumir que sei executá-los sozinho.", "Quando consigo pedir orientação de forma mais específica."),
                    new(3, "Execução antes da carga", "Entender que carga é uma variável e não o objetivo isolado do exercício.", "Consigo diferenciar aprender um movimento de testar força.", "Quando consigo priorizar técnica e contexto antes de aumentar peso.")
                ],
                "A jornada não define carga, séries, repetições, descanso ou técnica avançada.",
                "Conhecimento da linguagem da musculação não substitui supervisão quando ela for necessária.")
        };

        if (!string.IsNullOrWhiteSpace(modalidade))
        {
            var filtro = modalidade.Trim();
            jornadas = jornadas
                .Where(x =>
                    x.ModalidadeCodigo.Equals(filtro, StringComparison.OrdinalIgnoreCase) ||
                    x.ModalidadeNome.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return Ok(new BeginnerJourneysResponse(
            jornadas,
            "AESYN Explore / Start a Sport",
            "Beginner Journeys organiza aprendizagem por familiaridade. Não registra conclusão clínica, não progride treino e não publica prescrição automaticamente."));
    }


    [HttpGet("home-workout")]
    public async Task<ActionResult<HomeWorkoutResponse>> HomeWorkout(
        [FromQuery] string? espaco = null,
        [FromQuery] string? recurso = null,
        [FromQuery] string? preferencia = null,
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var espacos = new[] { "Pouco espaço", "Sala ou quarto", "Quintal ou área externa" };
        var recursos = new[] { "Sem equipamento", "Faixa ou elástico", "Halter", "Banco ou cadeira" };
        var preferencias = new[] { "Força", "Mobilidade", "Condicionamento", "Corpo inteiro" };

        var espacoAtual = NormalizarOpcao(espaco, espacos, "Sala ou quarto");
        var recursoAtual = NormalizarOpcao(recurso, recursos, "Sem equipamento");
        var preferenciaAtual = NormalizarOpcao(preferencia, preferencias, "Corpo inteiro");

        var exercicios = await db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.GrupoMuscular,
                x.Equipamento,
                x.Descricao
            })
            .ToListAsync(ct);

        var candidatos = exercicios
            .Select(x => new
            {
                Exercicio = x,
                Texto = $"{x.Nome} {x.GrupoMuscular} {x.Equipamento} {x.Descricao}".ToLowerInvariant()
            })
            .Where(x => CompativelComCasa(x.Texto, recursoAtual, preferenciaAtual))
            .Take(16)
            .Select(x => new HomeWorkoutMovimentoResponse(
                x.Exercicio.Id,
                x.Exercicio.Nome,
                x.Exercicio.GrupoMuscular,
                x.Exercicio.Equipamento,
                x.Exercicio.Descricao,
                MotivoCompatibilidade(x.Texto, espacoAtual, recursoAtual, preferenciaAtual)))
            .ToArray();

        return Ok(new HomeWorkoutResponse(
            espacoAtual,
            recursoAtual,
            preferenciaAtual,
            espacos,
            recursos,
            preferencias,
            candidatos,
            "Exercicios",
            "Home Workout organiza possibilidades do catalogo profissional por contexto de casa. Nao monta ficha, nao define series, repeticoes, carga, intensidade ou progressao automaticamente."));
    }

    private static string NormalizarOpcao(string? valor, string[] opcoes, string padrao)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return padrao;

        var encontrada = opcoes.FirstOrDefault(x =>
            x.Equals(valor.Trim(), StringComparison.OrdinalIgnoreCase));

        return encontrada ?? padrao;
    }

    private static bool CompativelComCasa(string texto, string recurso, string preferencia)
    {
        var recursoOk = recurso switch
        {
            "Sem equipamento" => !texto.Contains("barra") &&
                                !texto.Contains("máquina") &&
                                !texto.Contains("maquina") &&
                                !texto.Contains("cabo") &&
                                !texto.Contains("polia"),
            "Faixa ou elástico" => texto.Contains("elástico") ||
                                  texto.Contains("elastico") ||
                                  texto.Contains("faixa") ||
                                  texto.Contains("peso corporal"),
            "Halter" => texto.Contains("halter") || texto.Contains("dumbbell"),
            "Banco ou cadeira" => texto.Contains("banco") ||
                                 texto.Contains("cadeira") ||
                                 texto.Contains("peso corporal"),
            _ => true
        };

        var preferenciaOk = preferencia switch
        {
            "Força" => texto.Contains("força") ||
                       texto.Contains("forca") ||
                       texto.Contains("agach") ||
                       texto.Contains("flex") ||
                       texto.Contains("remada") ||
                       texto.Contains("press"),
            "Mobilidade" => texto.Contains("mobil") ||
                            texto.Contains("along") ||
                            texto.Contains("quadril") ||
                            texto.Contains("tornozelo") ||
                            texto.Contains("ombro"),
            "Condicionamento" => texto.Contains("cardio") ||
                                 texto.Contains("condicion") ||
                                 texto.Contains("polichinelo") ||
                                 texto.Contains("corrida") ||
                                 texto.Contains("salt"),
            "Corpo inteiro" => true,
            _ => true
        };

        return recursoOk && preferenciaOk;
    }

    private static string MotivoCompatibilidade(
        string texto,
        string espaco,
        string recurso,
        string preferencia)
    {
        var partes = new List<string>
        {
            $"Contexto: {espaco}",
            $"Recurso: {recurso}",
            $"Preferencia: {preferencia}"
        };

        if (texto.Contains("peso corporal"))
            partes.Add("catalogado como peso corporal");
        else if (texto.Contains("halter"))
            partes.Add("catalogado com halter");
        else if (texto.Contains("elástico") || texto.Contains("elastico"))
            partes.Add("catalogado com elástico/faixa");

        return string.Join(" • ", partes);
    }


    [HttpGet("quick-movement")]
    public async Task<ActionResult<QuickMovementResponse>> QuickMovement(
        [FromQuery] string? janela = null,
        [FromQuery] string? contexto = null,
        [FromQuery] string? preferencia = null,
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var janelas = new[] { "Até 5 min", "5 a 10 min", "10 a 20 min" };
        var contextos = new[] { "Em casa", "No trabalho", "Ao ar livre", "Qualquer lugar" };
        var preferencias = new[] { "Mobilidade", "Ativação", "Força leve", "Movimento geral" };

        var janelaAtual = NormalizarOpcao(janela, janelas, "5 a 10 min");
        var contextoAtual = NormalizarOpcao(contexto, contextos, "Qualquer lugar");
        var preferenciaAtual = NormalizarOpcao(preferencia, preferencias, "Movimento geral");

        var exercicios = await db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.GrupoMuscular,
                x.Equipamento,
                x.Descricao
            })
            .ToListAsync(ct);

        var possibilidades = exercicios
            .Select(x => new
            {
                Exercicio = x,
                Texto = $"{x.Nome} {x.GrupoMuscular} {x.Equipamento} {x.Descricao}".ToLowerInvariant()
            })
            .Where(x => CompativelComMovimentoRapido(x.Texto, contextoAtual, preferenciaAtual))
            .Take(12)
            .Select(x => new QuickMovementPossibilidadeResponse(
                x.Exercicio.Id,
                x.Exercicio.Nome,
                x.Exercicio.GrupoMuscular,
                x.Exercicio.Equipamento,
                x.Exercicio.Descricao,
                MotivoMovimentoRapido(x.Texto, janelaAtual, contextoAtual, preferenciaAtual)))
            .ToArray();

        return Ok(new QuickMovementResponse(
            janelaAtual,
            contextoAtual,
            preferenciaAtual,
            janelas,
            contextos,
            preferencias,
            possibilidades,
            "Exercicios",
            "Quick Movement organiza possibilidades curtas pelo contexto escolhido. A janela de tempo e apenas um filtro de exploracao: nao define intensidade, series, repeticoes, volume ou treino pronto."));
    }

    private static bool CompativelComMovimentoRapido(
        string texto,
        string contexto,
        string preferencia)
    {
        var contextoOk = contexto switch
        {
            "Em casa" => !texto.Contains("máquina") &&
                         !texto.Contains("maquina") &&
                         !texto.Contains("polia"),
            "No trabalho" => !texto.Contains("barra") &&
                             !texto.Contains("halter") &&
                             !texto.Contains("máquina") &&
                             !texto.Contains("maquina") &&
                             !texto.Contains("polia"),
            "Ao ar livre" => !texto.Contains("máquina") &&
                             !texto.Contains("maquina") &&
                             !texto.Contains("polia"),
            "Qualquer lugar" => true,
            _ => true
        };

        var preferenciaOk = preferencia switch
        {
            "Mobilidade" => texto.Contains("mobil") ||
                            texto.Contains("along") ||
                            texto.Contains("quadril") ||
                            texto.Contains("tornozelo") ||
                            texto.Contains("ombro"),
            "Ativação" => texto.Contains("ativ") ||
                          texto.Contains("aquec") ||
                          texto.Contains("mobil") ||
                          texto.Contains("core") ||
                          texto.Contains("estabil"),
            "Força leve" => texto.Contains("agach") ||
                            texto.Contains("flex") ||
                            texto.Contains("remada") ||
                            texto.Contains("peso corporal") ||
                            texto.Contains("isometr"),
            "Movimento geral" => true,
            _ => true
        };

        return contextoOk && preferenciaOk;
    }

    private static string MotivoMovimentoRapido(
        string texto,
        string janela,
        string contexto,
        string preferencia)
    {
        var partes = new List<string>
        {
            $"Janela escolhida: {janela}",
            $"Contexto: {contexto}",
            $"Preferencia: {preferencia}"
        };

        if (texto.Contains("mobil"))
            partes.Add("catalogado com indicio de mobilidade");
        else if (texto.Contains("peso corporal"))
            partes.Add("catalogado como peso corporal");
        else if (texto.Contains("core") || texto.Contains("estabil"))
            partes.Add("catalogado com indicio de estabilizacao");

        return string.Join(" • ", partes);
    }


    [HttpGet("travel-mode")]
    public async Task<ActionResult<TravelModeResponse>> TravelMode(
        [FromQuery] string? hospedagem = null,
        [FromQuery] string? recurso = null,
        [FromQuery] string? rotina = null,
        CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
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

        var hospedagens = new[] { "Quarto pequeno", "Hotel com academia", "Casa ou apartamento", "Sem local definido" };
        var recursos = new[] { "Sem equipamento", "Peso corporal", "Faixa ou elástico", "Halter", "Academia disponível" };
        var rotinas = new[] { "Agenda apertada", "Horário flexível", "Muitos deslocamentos", "Dia imprevisível" };

        var hospedagemAtual = NormalizarOpcao(hospedagem, hospedagens, "Sem local definido");
        var recursoAtual = NormalizarOpcao(recurso, recursos, "Sem equipamento");
        var rotinaAtual = NormalizarOpcao(rotina, rotinas, "Dia imprevisível");

        var exercicios = await db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.GrupoMuscular,
                x.Equipamento,
                x.Descricao
            })
            .ToListAsync(ct);

        var possibilidades = exercicios
            .Select(x => new
            {
                Exercicio = x,
                Texto = $"{x.Nome} {x.GrupoMuscular} {x.Equipamento} {x.Descricao}".ToLowerInvariant()
            })
            .Where(x => CompativelComViagem(x.Texto, hospedagemAtual, recursoAtual))
            .Take(14)
            .Select(x => new TravelModePossibilidadeResponse(
                x.Exercicio.Id,
                x.Exercicio.Nome,
                x.Exercicio.GrupoMuscular,
                x.Exercicio.Equipamento,
                x.Exercicio.Descricao,
                MotivoViagem(x.Texto, hospedagemAtual, recursoAtual, rotinaAtual)))
            .ToArray();

        return Ok(new TravelModeResponse(
            plano,
            hospedagemAtual,
            recursoAtual,
            rotinaAtual,
            hospedagens,
            recursos,
            rotinas,
            possibilidades,
            "Exercicios",
            "Travel Mode organiza possibilidades para um contexto temporario. Ele nao substitui o plano profissional, nao transforma viagem em deload automatico e nao cria treino, carga, volume ou intensidade automaticamente."));
    }

    private static bool CompativelComViagem(
        string texto,
        string hospedagem,
        string recurso)
    {
        var hospedagemOk = hospedagem switch
        {
            "Quarto pequeno" => !texto.Contains("barra") &&
                               !texto.Contains("máquina") &&
                               !texto.Contains("maquina") &&
                               !texto.Contains("polia") &&
                               !texto.Contains("corrida"),
            "Hotel com academia" => true,
            "Casa ou apartamento" => !texto.Contains("máquina") &&
                                     !texto.Contains("maquina") &&
                                     !texto.Contains("polia"),
            "Sem local definido" => !texto.Contains("máquina") &&
                                    !texto.Contains("maquina") &&
                                    !texto.Contains("polia"),
            _ => true
        };

        var recursoOk = recurso switch
        {
            "Sem equipamento" => !texto.Contains("halter") &&
                                !texto.Contains("barra") &&
                                !texto.Contains("máquina") &&
                                !texto.Contains("maquina") &&
                                !texto.Contains("polia") &&
                                !texto.Contains("elástico") &&
                                !texto.Contains("elastico"),
            "Peso corporal" => texto.Contains("peso corporal") ||
                               texto.Contains("agach") ||
                               texto.Contains("flex") ||
                               texto.Contains("prancha") ||
                               texto.Contains("mobil"),
            "Faixa ou elástico" => texto.Contains("elástico") ||
                                  texto.Contains("elastico") ||
                                  texto.Contains("faixa") ||
                                  texto.Contains("peso corporal"),
            "Halter" => texto.Contains("halter") || texto.Contains("dumbbell"),
            "Academia disponível" => true,
            _ => true
        };

        return hospedagemOk && recursoOk;
    }

    private static string MotivoViagem(
        string texto,
        string hospedagem,
        string recurso,
        string rotina)
    {
        var partes = new List<string>
        {
            $"Hospedagem: {hospedagem}",
            $"Recurso: {recurso}",
            $"Rotina temporaria: {rotina}"
        };

        if (texto.Contains("peso corporal"))
            partes.Add("catalogado como peso corporal");
        else if (texto.Contains("mobil"))
            partes.Add("catalogado com indicio de mobilidade");
        else if (texto.Contains("halter"))
            partes.Add("catalogado com halter");

        return string.Join(" • ", partes);
    }


    [HttpGet("outdoor-mode")]
    public async Task<ActionResult<OutdoorModeResponse>> OutdoorMode(
        [FromQuery] string? ambiente = null,
        [FromQuery] string? recurso = null,
        [FromQuery] string? interesse = null,
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var ambientes = new[] { "Rua", "Parque", "Praça", "Trilha leve", "Área externa livre" };
        var recursos = new[] { "Sem equipamento", "Banco", "Barra fixa", "Bicicleta", "Escada ou inclinação" };
        var interesses = new[] { "Caminhar", "Correr", "Mobilidade", "Força com peso corporal", "Condicionamento geral" };

        var ambienteAtual = NormalizarOpcao(ambiente, ambientes, "Parque");
        var recursoAtual = NormalizarOpcao(recurso, recursos, "Sem equipamento");
        var interesseAtual = NormalizarOpcao(interesse, interesses, "Condicionamento geral");

        var exercicios = await db.Exercicios.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Nome)
            .Select(x => new
            {
                x.Id,
                x.Nome,
                x.GrupoMuscular,
                x.Equipamento,
                x.Descricao
            })
            .ToListAsync(ct);

        var possibilidades = exercicios
            .Select(x => new
            {
                Exercicio = x,
                Texto = $"{x.Nome} {x.GrupoMuscular} {x.Equipamento} {x.Descricao}".ToLowerInvariant()
            })
            .Where(x => CompativelComOutdoor(x.Texto, recursoAtual, interesseAtual))
            .Take(14)
            .Select(x => new OutdoorModePossibilidadeResponse(
                x.Exercicio.Id,
                x.Exercicio.Nome,
                x.Exercicio.GrupoMuscular,
                x.Exercicio.Equipamento,
                x.Exercicio.Descricao,
                MotivoOutdoor(x.Texto, ambienteAtual, recursoAtual, interesseAtual)))
            .ToArray();

        return Ok(new OutdoorModeResponse(
            ambienteAtual,
            recursoAtual,
            interesseAtual,
            ambientes,
            recursos,
            interesses,
            possibilidades,
            "Exercicios",
            "Outdoor Mode organiza possibilidades pelo ambiente externo informado. Nao define rota, distancia, pace, carga, volume, duracao ou intensidade automaticamente."));
    }

    private static bool CompativelComOutdoor(
        string texto,
        string recurso,
        string interesse)
    {
        var recursoOk = recurso switch
        {
            "Sem equipamento" => !texto.Contains("máquina") &&
                                !texto.Contains("maquina") &&
                                !texto.Contains("polia") &&
                                !texto.Contains("halter"),
            "Banco" => texto.Contains("banco") ||
                       texto.Contains("peso corporal") ||
                       texto.Contains("agach") ||
                       texto.Contains("flex"),
            "Barra fixa" => texto.Contains("barra") ||
                            texto.Contains("pux") ||
                            texto.Contains("remada"),
            "Bicicleta" => texto.Contains("cicl") ||
                           texto.Contains("bike") ||
                           texto.Contains("bicic"),
            "Escada ou inclinação" => texto.Contains("subida") ||
                                      texto.Contains("escada") ||
                                      texto.Contains("corrida") ||
                                      texto.Contains("caminhada") ||
                                      texto.Contains("agach"),
            _ => true
        };

        var interesseOk = interesse switch
        {
            "Caminhar" => texto.Contains("caminh"),
            "Correr" => texto.Contains("corrida") || texto.Contains("correr"),
            "Mobilidade" => texto.Contains("mobil") ||
                            texto.Contains("along") ||
                            texto.Contains("quadril") ||
                            texto.Contains("tornozelo") ||
                            texto.Contains("ombro"),
            "Força com peso corporal" => texto.Contains("peso corporal") ||
                                         texto.Contains("agach") ||
                                         texto.Contains("flex") ||
                                         texto.Contains("prancha") ||
                                         texto.Contains("pux"),
            "Condicionamento geral" => true,
            _ => true
        };

        return recursoOk && interesseOk;
    }

    private static string MotivoOutdoor(
        string texto,
        string ambiente,
        string recurso,
        string interesse)
    {
        var partes = new List<string>
        {
            $"Ambiente: {ambiente}",
            $"Recurso: {recurso}",
            $"Interesse: {interesse}"
        };

        if (texto.Contains("caminh"))
            partes.Add("catalogado com indicio de caminhada");
        else if (texto.Contains("corrida"))
            partes.Add("catalogado com indicio de corrida");
        else if (texto.Contains("peso corporal"))
            partes.Add("catalogado como peso corporal");
        else if (texto.Contains("mobil"))
            partes.Add("catalogado com indicio de mobilidade");

        return string.Join(" • ", partes);
    }


    [HttpGet("learn-fundamentals")]
    public async Task<ActionResult<LearnFundamentalsResponse>> LearnFundamentals(
        [FromQuery] string? modalidade = null,
        [FromQuery] string? capacidade = null,
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var modalidades = new[] { "Musculação", "Corrida", "Calistenia", "Mobilidade", "Ciclismo" };
        var capacidadesPorModalidade = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Musculação"] = ["Padrões de movimento", "Controle", "Força"],
            ["Corrida"] = ["Técnica básica", "Percepção de esforço", "Ritmo"],
            ["Calistenia"] = ["Apoio", "Alavanca", "Estabilidade"],
            ["Mobilidade"] = ["Amplitude", "Controle", "Respiração"],
            ["Ciclismo"] = ["Controle", "Cadência", "Ambiente"]
        };

        var modalidadeAtual = NormalizarOpcao(modalidade, modalidades, "Musculação");
        var capacidades = capacidadesPorModalidade[modalidadeAtual];
        var capacidadeAtual = NormalizarOpcao(capacidade, capacidades, capacidades[0]);

        var fundamentos = MontarFundamentos(modalidadeAtual, capacidadeAtual);

        return Ok(new LearnFundamentalsResponse(
            modalidadeAtual,
            capacidadeAtual,
            modalidades,
            capacidades,
            fundamentos,
            "AESYN Explore / Sports & Movement Library",
            "Learn Fundamentals organiza conhecimento educacional. Nao define series, repeticoes, carga, pace, volume, intensidade, aptidao clinica ou progressao automatica."));
    }

    private static LearnFundamentalsItemResponse[] MontarFundamentos(
        string modalidade,
        string capacidade)
    {
        if (modalidade == "Musculação" && capacidade == "Padrões de movimento")
            return
            [
                new("empurrar", "Empurrar", "Mover uma resistência para longe do corpo em diferentes ângulos.", "Observe estabilidade, trajetória e controle.", "Transformar carga alta em objetivo principal antes de entender o movimento.", "Reconhecer variações de empurrar no catálogo profissional."),
                new("puxar", "Puxar", "Trazer uma resistência em direção ao corpo ou controlar o retorno.", "Observe posição do tronco e trajetória dos braços.", "Confundir maior peso com melhor execução.", "Identificar puxadas horizontais e verticais."),
                new("agachar", "Agachar", "Organizar tornozelo, joelho, quadril e tronco em um padrão de sentar e levantar.", "Observe equilíbrio, controle e amplitude confortável.", "Forçar uma amplitude universal para todas as pessoas.", "Comparar variações e recursos disponíveis.")
            ];

        if (modalidade == "Corrida")
            return
            [
                new("alternancia", "Caminhar e correr", "Alternar pode fazer parte do aprendizado sem representar falha.", "Observe como muda a percepção de esforço.", "Achar que começar exige correr continuamente.", "Entender diferença entre aprender e testar performance."),
                new("esforco", "Percepção de esforço", "Velocidade e esforço não são equivalentes para todas as pessoas.", "Observe respiração, controle e percepção subjetiva.", "Usar pace isolado como autorização para aumentar intensidade.", "Aprender como contexto muda a resposta."),
                new("ambiente", "Ambiente e superfície", "Terreno e ambiente alteram exigência e experiência.", "Observe superfície, inclinação, espaço e segurança.", "Tratar todos os percursos como equivalentes.", "Explorar contexto antes de pensar em progressão.")
            ];

        if (modalidade == "Calistenia")
            return
            [
                new("apoio", "Pontos de apoio", "Mãos, pés, banco, barra ou chão mudam a exigência do movimento.", "Observe estabilidade e conforto do apoio.", "Interpretar apoio maior como 'pior versão'.", "Entender como apoio muda a alavanca."),
                new("alavanca", "Alavanca", "A posição do corpo pode tornar o mesmo padrão mais ou menos exigente.", "Observe distância entre apoios e centro de massa.", "Avançar para variações difíceis só pela aparência.", "Comparar variações do mesmo padrão."),
                new("controle", "Controle corporal", "Estabilidade e trajetória importam antes da complexidade.", "Observe capacidade de repetir com controle.", "Buscar dificuldade máxima como marcador de progresso.", "Explorar consistência antes da progressão.")
            ];

        if (modalidade == "Mobilidade")
            return
            [
                new("amplitude", "Amplitude", "Amplitude é específica do movimento, da pessoa e do contexto.", "Observe onde o movimento permanece controlado.", "Forçar amplitude apenas porque outra pessoa alcança.", "Distinguir amplitude disponível de amplitude útil."),
                new("controle", "Controle na amplitude", "Chegar mais longe não significa controlar melhor.", "Observe estabilidade ao entrar e sair da posição.", "Usar desconforto como prova de eficácia.", "Explorar movimento com controle."),
                new("respiracao", "Respiração", "A respiração pode ajudar a perceber tensão e controle.", "Observe se consegue respirar sem prender o ar.", "Usar uma técnica respiratória como regra universal.", "Relacionar respiração ao próprio contexto.")
            ];

        if (modalidade == "Ciclismo")
            return
            [
                new("controle", "Controle da bicicleta", "Frenagem, direção e estabilidade vêm antes de velocidade.", "Observe segurança para iniciar, frear e mudar direção.", "Aumentar velocidade antes de dominar controle.", "Reconhecer os controles essenciais."),
                new("cadencia", "Cadência", "Cadência descreve frequência de pedalada, não um alvo universal.", "Observe sensação e controle do giro.", "Perseguir um número fixo sem contexto.", "Entender cadência como variável, não prescrição."),
                new("ambiente", "Leitura do ambiente", "Rota, superfície e outros usuários mudam a experiência.", "Observe sinalização, terreno e previsibilidade.", "Tratar indoor e rua como contextos equivalentes.", "Explorar segurança contextual antes de performance.")
            ];

        return
        [
            new("conceito", capacidade, $"Entender o que significa {capacidade.ToLowerInvariant()} dentro de {modalidade}.", "Observe como o conceito aparece no movimento.", "Transformar um conceito educacional em regra automática.", "Explorar exemplos no catálogo profissional."),
            new("contexto", "Contexto", "O mesmo fundamento pode mudar conforme pessoa, ambiente e recurso.", "Observe diferenças entre variações.", "Assumir que existe uma única execução universal.", "Comparar possibilidades com orientação quando necessário."),
            new("familiaridade", "Familiaridade", "Aprender o conceito antes de buscar intensidade ou performance.", "Observe se consegue explicar o fundamento com suas palavras.", "Confundir conhecer com estar apto ou prescrito.", "Usar o conhecimento para conversar melhor com o profissional.")
        ];
    }


    [HttpGet("starter-packs")]
    public async Task<ActionResult<ExploreStarterPacksResponse>> ExploreStarterPacks(
        [FromQuery] string? modalidade = null,
        [FromQuery] string? objetivo = null,
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

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

        var definicoes = new[]
        {
            new
            {
                Codigo = "musculacao",
                Nome = "Musculação",
                Objetivos = new[]
                {
                    new { Codigo = "forca", Nome = "Força", Capacidades = new[] { "Padrões de movimento", "Controle", "Força" }, Termos = new[] { "força", "forca", "muscul", "resist", "empurr", "pux", "agach" } },
                    new { Codigo = "hipertrofia", Nome = "Hipertrofia", Capacidades = new[] { "Tensão", "Volume", "Execução" }, Termos = new[] { "hipertrof", "muscul", "volume", "resist", "força", "forca" } }
                }
            },
            new
            {
                Codigo = "corrida",
                Nome = "Corrida",
                Objetivos = new[]
                {
                    new { Codigo = "iniciacao", Nome = "Iniciação", Capacidades = new[] { "Técnica básica", "Percepção de esforço", "Ritmo" }, Termos = new[] { "corrida", "correr", "caminh", "run", "trote" } },
                    new { Codigo = "base", Nome = "Base", Capacidades = new[] { "Ritmo", "Continuidade", "Controle" }, Termos = new[] { "corrida", "base", "ritmo", "cardio", "aerob" } }
                }
            },
            new
            {
                Codigo = "calistenia",
                Nome = "Calistenia",
                Objetivos = new[]
                {
                    new { Codigo = "fundamentos", Nome = "Fundamentos", Capacidades = new[] { "Apoio", "Alavanca", "Estabilidade" }, Termos = new[] { "calisten", "peso corporal", "flex", "prancha", "barra", "agach" } }
                }
            },
            new
            {
                Codigo = "mobilidade",
                Nome = "Mobilidade",
                Objetivos = new[]
                {
                    new { Codigo = "controle", Nome = "Controle de movimento", Capacidades = new[] { "Amplitude", "Controle", "Respiração" }, Termos = new[] { "mobil", "along", "quadril", "tornozelo", "ombro" } }
                }
            },
            new
            {
                Codigo = "ciclismo",
                Nome = "Ciclismo",
                Objetivos = new[]
                {
                    new { Codigo = "iniciacao", Nome = "Iniciação", Capacidades = new[] { "Controle", "Cadência", "Ambiente" }, Termos = new[] { "cicl", "bike", "bicic", "pedal", "cadencia", "cadência" } }
                }
            }
        };

        var modalidadeFiltro = modalidade?.Trim();
        var objetivoFiltro = objetivo?.Trim();

        var defs = definicoes.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(modalidadeFiltro))
        {
            defs = defs.Where(x =>
                x.Codigo.Equals(modalidadeFiltro, StringComparison.OrdinalIgnoreCase) ||
                x.Nome.Contains(modalidadeFiltro, StringComparison.OrdinalIgnoreCase));
        }

        var packs = new List<ExploreStarterPackResponse>();

        foreach (var mod in defs)
        {
            var objetivos = mod.Objetivos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(objetivoFiltro))
            {
                objetivos = objetivos.Where(x =>
                    x.Codigo.Equals(objetivoFiltro, StringComparison.OrdinalIgnoreCase) ||
                    x.Nome.Contains(objetivoFiltro, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var obj in objetivos)
            {
                var sessoes = modelosSessao
                    .Where(modelo =>
                    {
                        var texto = $"{modelo.Nome} {modelo.Categoria} {modelo.Descricao}".ToLowerInvariant();
                        return obj.Termos.Any(termo =>
                            texto.Contains(termo.ToLowerInvariant(), StringComparison.Ordinal));
                    })
                    .Take(8)
                    .Select(x => new ExploreStarterPackSessaoResponse(
                        x.Id,
                        x.Nome,
                        x.Categoria,
                        x.Descricao))
                    .ToArray();

                var possuiReferencias = sessoes.Length > 0;

                packs.Add(new ExploreStarterPackResponse(
                    $"{mod.Codigo}:{obj.Codigo}",
                    $"{mod.Nome} • {obj.Nome}",
                    mod.Codigo,
                    mod.Nome,
                    obj.Codigo,
                    obj.Nome,
                    obj.Capacidades,
                    sessoes,
                    possuiReferencias,
                    possuiReferencias
                        ? $"{sessoes.Length} sessão(ões)-modelo existente(s) relacionada(s)."
                        : "Ainda não há sessão-modelo relacionada; o pack permanece como referência editorial."));
            }
        }

        return Ok(new ExploreStarterPacksResponse(
            modalidadeFiltro,
            objetivoFiltro,
            definicoes.Select(x => x.Nome).ToArray(),
            packs,
            "ModelosSessoesTreino",
            "Sports Starter Packs sao referencias exploraveis. Nao copiam sessoes, nao atribuem plano, nao iniciam treino e nao publicam prescricao automaticamente."));
    }


    [HttpGet("interesses")]
    public async Task<ActionResult<InteresseExploreCatalogoResponse>> GetInteresses(
        CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.AsNoTracking()
            .Where(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var interesses = await db.InteressesExplorePaciente.AsNoTracking()
            .Where(x =>
                x.PacienteId == paciente.Id &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Nome)
            .Select(x => new InteresseExploreResponse(
                x.Id,
                x.Codigo,
                x.Nome,
                x.Intencao,
                x.Origem,
                x.CreatedAtUtc))
            .ToArrayAsync(ct);

        return Ok(new InteresseExploreCatalogoResponse(
            interesses,
            CatalogoInteressesExplore(),
            IntencoesExplore(),
            "Interesses sao declarados pelo atleta. Atividade relatada, plano profissional e interesse sao contextos diferentes; o AESYN nao infere preferencia clinica."));
    }

    [HttpPut("interesses")]
    public async Task<ActionResult<InteresseExploreCatalogoResponse>> PutInteresses(
        AtualizarInteressesExploreRequest request,
        CancellationToken ct = default)
    {
        var paciente = await db.Pacientes
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (paciente is null)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var catalogo = CatalogoInteressesExplore();
        var porCodigo = catalogo.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);
        var intencoes = IntencoesExplore();

        var recebidos = (request.Interesses ?? Array.Empty<AtualizarInteresseExploreItemRequest>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Codigo))
            .GroupBy(x => x.Codigo.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Last())
            .ToArray();

        if (recebidos.Length > 12)
            return BadRequest(new { message = "Informe no maximo 12 interesses." });

        foreach (var item in recebidos)
        {
            if (!porCodigo.ContainsKey(item.Codigo.Trim()))
                return BadRequest(new { message = $"Modalidade de interesse invalida: {item.Codigo}" });

            if (!intencoes.Contains(item.Intencao, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Intencao invalida para {item.Codigo}." });
        }

        var atuais = await db.InteressesExplorePaciente
            .Where(x =>
                x.PacienteId == paciente.Id &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .ToListAsync(ct);

        var codigosRecebidos = recebidos
            .Select(x => x.Codigo.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var remover = atuais
            .Where(x => !codigosRecebidos.Contains(x.Codigo))
            .ToArray();

        if (remover.Length > 0)
            db.InteressesExplorePaciente.RemoveRange(remover);

        foreach (var item in recebidos)
        {
            var codigo = item.Codigo.Trim();
            var opcao = porCodigo[codigo];
            var intencao = intencoes.First(x =>
                x.Equals(item.Intencao, StringComparison.OrdinalIgnoreCase));

            var existente = atuais.FirstOrDefault(x =>
                x.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

            if (existente is null)
            {
                db.InteressesExplorePaciente.Add(new InteresseExplorePaciente
                {
                    OrganizacaoId = currentUser.OrganizationId,
                    PacienteId = paciente.Id,
                    Codigo = opcao.Codigo,
                    Nome = opcao.Nome,
                    Intencao = intencao,
                    Origem = "DeclaradoPeloAtleta"
                });
            }
            else
            {
                existente.Nome = opcao.Nome;
                existente.Intencao = intencao;
                existente.Origem = "DeclaradoPeloAtleta";
                existente.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);

        var salvos = await db.InteressesExplorePaciente.AsNoTracking()
            .Where(x =>
                x.PacienteId == paciente.Id &&
                x.OrganizacaoId == currentUser.OrganizationId)
            .OrderBy(x => x.Nome)
            .Select(x => new InteresseExploreResponse(
                x.Id,
                x.Codigo,
                x.Nome,
                x.Intencao,
                x.Origem,
                x.CreatedAtUtc))
            .ToArrayAsync(ct);

        return Ok(new InteresseExploreCatalogoResponse(
            salvos,
            catalogo,
            intencoes,
            "Interesses atualizados por declaracao explicita do atleta. Nenhum interesse altera plano, intensidade, aptidao ou conduta profissional automaticamente."));
    }

    private static InteresseExploreOpcaoResponse[] CatalogoInteressesExplore() =>
    [
        new("musculacao", "Musculação"),
        new("caminhada", "Caminhada"),
        new("corrida", "Corrida"),
        new("calistenia", "Calistenia"),
        new("mobilidade", "Mobilidade"),
        new("condicionamento", "Condicionamento"),
        new("ciclismo", "Ciclismo"),
        new("futebol", "Futebol"),
        new("futsal", "Futsal"),
        new("basquete", "Basquete"),
        new("volei", "Vôlei"),
        new("tenis", "Tênis"),
        new("beach-tennis", "Beach Tennis"),
        new("natacao", "Natação"),
        new("triathlon", "Triathlon"),
        new("artes-marciais", "Artes Marciais"),
        new("remo", "Remo"),
        new("trekking", "Trekking"),
        new("recreativos", "Esportes recreativos")
    ];

    private static string[] IntencoesExplore() =>
    [
        "QueroExperimentar",
        "QueroRetomar",
        "TenhoCuriosidade"
    ];


    [HttpGet("sports-expansion-i")]
    public async Task<ActionResult<SportsExpansionIResponse>> SportsExpansionI(
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var modalidades = new SportsExpansionIModalidadeResponse[]
        {
            new(
                "futebol",
                "Futebol",
                "Fundamentos técnicos, deslocamentos multidirecionais e preparação física específica de campo.",
                ["Campo", "Society", "Gramado"],
                ["Bola", "Cones", "Mini-barreiras", "Gol"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("passe-controle", "Passe e controle"),
                        new("conducao", "Condução"),
                        new("finalizacao", "Finalização")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("aceleracao", "Aceleração"),
                        new("mudanca-direcao", "Mudança de direção"),
                        new("resistencia-especifica", "Resistência específica")
                    ])
                ]),
            new(
                "futsal",
                "Futsal",
                "Fundamentos de bola em espaço reduzido, acelerações curtas e repetição de esforços.",
                ["Quadra", "Ginásio"],
                ["Bola de futsal", "Cones", "Gol"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("passe-controle", "Passe e controle"),
                        new("conducao", "Condução e drible"),
                        new("finalizacao", "Finalização")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("aceleracao", "Aceleração curta"),
                        new("agilidade", "Agilidade"),
                        new("repeticao-esforcos", "Repetição de esforços")
                    ])
                ]),
            new(
                "basquete",
                "Basquete",
                "Fundamentos de bola, arremesso, saltos e deslocamentos de quadra.",
                ["Quadra", "Ginásio"],
                ["Bola", "Cesta", "Cones"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("drible", "Drible"),
                        new("passe", "Passe"),
                        new("arremesso", "Arremesso")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("salto", "Salto"),
                        new("deslocamento-lateral", "Deslocamento lateral"),
                        new("aceleracao", "Aceleração")
                    ])
                ]),
            new(
                "volei",
                "Vôlei",
                "Recepção, levantamento, ataque, saque, bloqueio e preparação para saltos.",
                ["Quadra", "Ginásio", "Areia"],
                ["Bola", "Rede", "Cones"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("recepcao", "Recepção"),
                        new("levantamento", "Levantamento"),
                        new("ataque", "Ataque e saque")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("salto", "Salto e aterrissagem"),
                        new("deslocamento", "Deslocamento de quadra"),
                        new("ombro", "Capacidade de ombro")
                    ])
                ]),
            new(
                "tenis",
                "Tênis",
                "Golpes de base, saque, posicionamento e preparação multidirecional.",
                ["Quadra rápida", "Saibro", "Grama"],
                ["Raquete", "Bola", "Rede", "Cones"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("golpes-base", "Golpes de base"),
                        new("saque", "Saque"),
                        new("posicionamento", "Posicionamento")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("agilidade", "Agilidade multidirecional"),
                        new("aceleracao", "Aceleração curta"),
                        new("ombro-tronco", "Ombro e tronco")
                    ])
                ]),
            new(
                "beach-tennis",
                "Beach Tennis",
                "Voleios, saque, posicionamento em dupla e deslocamento na areia.",
                ["Areia", "Quadra de beach tennis"],
                ["Raquete", "Bola", "Rede", "Cones"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("voleio", "Voleio"),
                        new("saque", "Saque"),
                        new("posicionamento", "Posicionamento em dupla")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("deslocamento-areia", "Deslocamento na areia"),
                        new("potencia", "Potência"),
                        new("ombro-tronco", "Ombro e tronco")
                    ])
                ])
        };

        return Ok(new SportsExpansionIResponse(
            modalidades,
            "Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão",
            "Sports Expansion I amplia a taxonomia e a descoberta. A estrutura nao declara aptidao, nao inicia treino e nao prescreve intensidade automaticamente."));
    }


    [HttpGet("football-futsal")]
    public async Task<ActionResult<FootballFutsalResponse>> FootballFutsal(
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var modelos = await db.ModelosSessoesTreino.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
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
        var futebolRefs = modelos
            .Where(x =>
            {
                var texto = $"{x.Nome} {x.Categoria} {x.Descricao}".ToLowerInvariant();
                return new[] { "futebol", "society", "campo", "bola", "passe", "chute", "sprint" }
                    .Any(t => texto.Contains(t, StringComparison.Ordinal));
            })
            .Take(6)
            .Select(x => new FootballFutsalSessaoReferenciaResponse(
                x.Id,
                x.Nome,
                x.Categoria,
                x.Descricao,
                "Sessão-modelo existente com termos relacionados a futebol/campo."))
            .ToArray();

        var futsalRefs = modelos
            .Where(x =>
            {
                var texto = $"{x.Nome} {x.Categoria} {x.Descricao}".ToLowerInvariant();
                return new[] { "futsal", "quadra", "bola", "passe", "chute", "agilidade" }
                    .Any(t => texto.Contains(t, StringComparison.Ordinal));
            })
            .Take(6)
            .Select(x => new FootballFutsalSessaoReferenciaResponse(
                x.Id,
                x.Nome,
                x.Categoria,
                x.Descricao,
                "Sessão-modelo existente com termos relacionados a futsal/quadra."))
            .ToArray();

        var modalidades = new[]
        {
            new FootballFutsalModalidadeResponse(
                "futebol",
                "Futebol",
                "Campo / society",
                "Mais espaço por jogador, deslocamentos mais longos e maior presença de acelerações em campo aberto.",
                ["Bola", "Cones", "Mini-barreiras", "Gol", "Campo ou society"],
                [
                    new("passe-controle", "Passe e controle", "Receber, orientar e dar continuidade à jogada em diferentes distâncias.", "Observe direção do primeiro toque, apoio e leitura antes do passe."),
                    new("conducao", "Condução", "Transportar a bola mantendo controle enquanto o espaço e a pressão mudam.", "Observe proximidade da bola, visão do ambiente e mudança de direção."),
                    new("finalizacao", "Finalização", "Entender contato com a bola, direção e contexto antes de buscar força máxima.", "Observe equilíbrio, pé de apoio e escolha do momento.")
                ],
                [
                    new("aceleracao", "Aceleração", "Saídas e retomadas de velocidade em distâncias variáveis.", "No futebol, o espaço tende a permitir acelerações mais longas do que no futsal."),
                    new("mudanca-direcao", "Mudança de direção", "Frear, reorganizar apoio e mudar direção em resposta à jogada.", "No campo, a mudança pode acontecer após deslocamentos mais longos."),
                    new("resistencia-especifica", "Resistência específica", "Alternância de corrida, aceleração, recuperação e ações técnicas durante a partida.", "O campo amplia distâncias totais e duração das ações locomotoras.")
                ],
                futebolRefs,
                "Campo amplia espaço e distância; isso muda leitura, deslocamento e preparação física."),
            new FootballFutsalModalidadeResponse(
                "futsal",
                "Futsal",
                "Quadra / ginásio",
                "Espaço reduzido, mais ações por minuto, acelerações curtas e decisões técnicas sob pressão mais frequente.",
                ["Bola de futsal", "Cones", "Gol", "Quadra"],
                [
                    new("passe-controle", "Passe e controle", "Receber e soltar a bola com pouco espaço e tempo de decisão.", "Observe orientação corporal, primeiro toque e velocidade da circulação."),
                    new("conducao-drible", "Condução e drible", "Controlar a bola em espaço curto e usar mudanças rápidas de direção.", "Observe proximidade da bola e capacidade de proteger a posse."),
                    new("finalizacao", "Finalização", "Finalizar com pouco tempo e ângulo reduzido, sem transformar potência em único objetivo.", "Observe preparação curta, equilíbrio e leitura do goleiro.")
                ],
                [
                    new("aceleracao-curta", "Aceleração curta", "Saídas rápidas em poucos metros após mudança de posse ou espaço.", "No futsal, acelerações são tipicamente mais curtas e frequentes."),
                    new("agilidade", "Agilidade", "Mudanças frequentes de direção e resposta a estímulos em espaço reduzido.", "A quadra aumenta a densidade de mudanças e decisões."),
                    new("repeticao-esforcos", "Repetição de esforços", "Sequência de ações intensas curtas intercaladas por pausas e participação técnica.", "O futsal concentra mais ações por unidade de espaço e tempo.")
                ],
                futsalRefs,
                "Quadra reduz espaço e tempo; isso aumenta frequência de decisão, mudança de direção e repetição de ações.")
        };

        return Ok(new FootballFutsalResponse(
            modalidades,
            "Sports Expansion I + ModelosSessoesTreino",
            "Football & Futsal 2.0 explica diferenças e referencia conteúdo existente. Não escolhe posição, não monta treino, não define carga, volume, intensidade ou retorno ao esporte."));
    }


    [HttpGet("basketball-volleyball")]
    public async Task<ActionResult<BasketballVolleyballResponse>> BasketballVolleyball(
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var modelos = await db.ModelosSessoesTreino.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
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

        var basqueteRefs = modelos
            .Where(x =>
            {
                var texto = $"{x.Nome} {x.Categoria} {x.Descricao}".ToLowerInvariant();
                return new[] { "basquete", "basket", "drible", "arremesso", "cesta", "salto", "lateral", "quadra" }
                    .Any(t => texto.Contains(t, StringComparison.Ordinal));
            })
            .Take(6)
            .Select(x => new BasketballVolleyballSessaoReferenciaResponse(
                x.Id,
                x.Nome,
                x.Categoria,
                x.Descricao,
                "Sessão-modelo existente com termos relacionados a basquete/quadra."))
            .ToArray();

        var voleiRefs = modelos
            .Where(x =>
            {
                var texto = $"{x.Nome} {x.Categoria} {x.Descricao}".ToLowerInvariant();
                return new[] { "volei", "vôlei", "volley", "manchete", "toque", "saque", "bloqueio", "salto", "quadra" }
                    .Any(t => texto.Contains(t, StringComparison.Ordinal));
            })
            .Take(6)
            .Select(x => new BasketballVolleyballSessaoReferenciaResponse(
                x.Id,
                x.Nome,
                x.Categoria,
                x.Descricao,
                "Sessão-modelo existente com termos relacionados a vôlei/quadra."))
            .ToArray();

        var modalidades = new[]
        {
            new BasketballVolleyballModalidadeResponse(
                "basquete",
                "Basquete",
                "Quadra / ginásio",
                "Ações com bola em deslocamento, mudanças rápidas de direção, acelerações, saltos e finalização em cesta.",
                ["Bola", "Cesta", "Cones", "Quadra"],
                [
                    new("drible", "Drible", "Controlar a bola enquanto o corpo muda direção, velocidade e relação com o defensor.", "Observe altura do drible, proteção da bola e visão do ambiente."),
                    new("passe", "Passe", "Transferir a bola com precisão e tempo adequado para manter continuidade da jogada.", "Observe base, direção e leitura do companheiro."),
                    new("arremesso", "Arremesso", "Organizar equilíbrio, alinhamento e coordenação para finalizar na cesta.", "Observe base, trajetória e controle antes de buscar distância.")
                ],
                [
                    new("salto", "Salto", "Aparece em rebotes, arremessos, bloqueios e disputas próximas à cesta.", "No basquete, o salto frequentemente se combina com deslocamento e contato espacial."),
                    new("deslocamento-lateral", "Deslocamento lateral", "Importante para defesa, contenção e reposicionamento sem perder equilíbrio.", "É mais contínuo e reativo em ações defensivas do que no vôlei."),
                    new("aceleracao", "Aceleração", "Saídas curtas para transição, contra-ataque e criação de espaço.", "O basquete combina aceleração com domínio de bola e mudanças de direção.")
                ],
                basqueteRefs,
                "Basquete mistura deslocamento contínuo com bola, contato espacial e finalizações em movimento."),
            new BasketballVolleyballModalidadeResponse(
                "volei",
                "Vôlei",
                "Quadra / ginásio",
                "Ações separadas pela rede, com recepção, levantamento, ataque, saque, bloqueio e ciclos repetidos de salto e aterrissagem.",
                ["Bola", "Rede", "Cones", "Quadra"],
                [
                    new("recepcao", "Recepção", "Controlar a primeira bola para criar continuidade e organização da jogada.", "Observe base, plataforma de contato e direção da bola."),
                    new("levantamento", "Levantamento", "Organizar a bola para o ataque com controle de direção e altura.", "Observe posicionamento sob a bola e precisão do toque."),
                    new("ataque-saque", "Ataque e saque", "Aplicar coordenação de tronco, braço e timing para enviar a bola com intenção.", "Observe sequência do movimento, contato e equilíbrio após a ação.")
                ],
                [
                    new("salto-aterrissagem", "Salto e aterrissagem", "Aparece em ataque, bloqueio e saque, exigindo repetição com controle de retorno ao solo.", "No vôlei, saltos verticais e aterrissagens repetidas têm papel mais central."),
                    new("deslocamento-quadra", "Deslocamento de quadra", "Ajustes curtos de posição para cobertura, recepção, bloqueio e aproximação.", "O deslocamento responde à trajetória da bola e ao sistema de jogo."),
                    new("ombro", "Capacidade de ombro", "Saque e ataque envolvem repetição de ações acima da cabeça.", "No vôlei, a demanda de ombro aparece de forma mais repetida em gestos aéreos.")
                ],
                voleiRefs,
                "Vôlei separa as equipes pela rede e concentra recepção, ações aéreas e aterrissagens repetidas.")
        };

        return Ok(new BasketballVolleyballResponse(
            modalidades,
            "Sports Expansion I + ModelosSessoesTreino",
            "Basketball & Volleyball 2.0 compara contextos e referencia conteúdo existente. Não define posição, salto-alvo, carga, volume, intensidade ou retorno ao esporte automaticamente."));
    }



    [HttpGet("tennis-beach-tennis")]
    public async Task<ActionResult<TennisBeachTennisResponse>> TennisBeachTennis(
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var modelos = await db.ModelosSessoesTreino.AsNoTracking()
            .Where(x =>
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo)
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

        var tenisRefs = modelos
            .Where(x =>
            {
                var texto = $"{x.Nome} {x.Categoria} {x.Descricao}".ToLowerInvariant();
                return new[] { "tênis", "tenis", "tennis", "raquete", "saque", "forehand", "backhand", "quadra", "lateral", "agilidade" }
                    .Any(t => texto.Contains(t, StringComparison.Ordinal));
            })
            .Take(6)
            .Select(x => new TennisBeachTennisSessaoReferenciaResponse(
                x.Id,
                x.Nome,
                x.Categoria,
                x.Descricao,
                "Sessão-modelo existente com termos relacionados a tênis, raquete ou deslocamento de quadra."))
            .ToArray();

        var beachRefs = modelos
            .Where(x =>
            {
                var texto = $"{x.Nome} {x.Categoria} {x.Descricao}".ToLowerInvariant();
                return new[] { "beach tennis", "beach-tennis", "areia", "raquete", "saque", "voleio", "smash", "lateral", "agilidade" }
                    .Any(t => texto.Contains(t, StringComparison.Ordinal));
            })
            .Take(6)
            .Select(x => new TennisBeachTennisSessaoReferenciaResponse(
                x.Id,
                x.Nome,
                x.Categoria,
                x.Descricao,
                "Sessão-modelo existente com termos relacionados a beach tennis, areia, raquete ou deslocamento."))
            .ToArray();

        var modalidades = new[]
        {
            new TennisBeachTennisModalidadeResponse(
                "tenis",
                "Tênis",
                "Quadra — piso duro, saibro ou grama",
                "Trocas com quique, variação de profundidade, acelerações, frenagens e reposicionamento conforme bola e superfície.",
                ["Raquete", "Bolas", "Rede", "Quadra"],
                [
                    new("saque", "Saque", "Iniciar o ponto combinando lançamento, rotação do tronco, ação do ombro e transferência de força.", "Observe consistência do lançamento, equilíbrio e coordenação antes de buscar velocidade."),
                    new("forehand-backhand", "Forehand e backhand", "Organizar preparação, contato e recuperação para golpes dos dois lados do corpo.", "Observe distância da bola, base, rotação e retorno à posição."),
                    new("posicionamento", "Posicionamento", "Ajustar distância, base e recuperação conforme profundidade, direção e ritmo da troca.", "Observe split step, leitura do quique e reposicionamento após o golpe.")
                ],
                [
                    new("aceleracao-frenagem", "Aceleração e frenagem", "Arranques curtos e desaceleração para chegar equilibrado à bola.", "Na quadra rígida ou saibro, aderência e deslizamento mudam a estratégia de apoio."),
                    new("mudanca-direcao", "Mudança de direção", "Reorganizar apoios para responder a bolas abertas, curtas ou profundas.", "O quique amplia a necessidade de leitura da trajetória antes do contato."),
                    new("rotacao-transferencia", "Rotação e transferência de força", "Integrar pernas, tronco e membro superior nos golpes e no saque.", "A cadeia de força acontece com base mais estável do que na areia.")
                ],
                tenisRefs,
                "O quique e a superfície da quadra alteram tempo de leitura, apoio, frenagem e construção do ponto."),
            new TennisBeachTennisModalidadeResponse(
                "beach-tennis",
                "Beach Tennis",
                "Areia / quadra de beach tennis",
                "Jogo sem quique, com deslocamento na areia, ações de rede, voleios, saque e bolas acima da cabeça em ritmo contínuo.",
                ["Raquete de beach tennis", "Bolas", "Rede", "Quadra de areia"],
                [
                    new("saque", "Saque", "Iniciar o ponto com controle de lançamento, coordenação e contato acima da cabeça.", "Observe estabilidade na areia, trajetória do lançamento e equilíbrio após o contato."),
                    new("voleio", "Voleio e controle sem quique", "Responder à bola no ar com preparação curta, direção e controle de raquete.", "Observe leitura precoce, contato à frente e recuperação rápida."),
                    new("smash-lob", "Smash e leitura de lob", "Organizar deslocamento, ajuste sob a bola e ação acima da cabeça.", "Observe reposicionamento na areia e controle do tronco antes da potência.")
                ],
                [
                    new("deslocamento-areia", "Deslocamento na areia", "Acelerar, frear e ajustar posição sobre uma superfície deformável.", "A areia aumenta custo locomotor e reduz resposta elástica do apoio em relação à quadra."),
                    new("reacao-posicionamento", "Reação e posicionamento", "Responder cedo porque a bola não pode tocar o chão.", "Sem quique, leitura e primeiro passo ganham ainda mais importância."),
                    new("ombro-tronco", "Ombro e tronco", "Saque, smash e ações altas repetem gestos acima da cabeça.", "A demanda aérea se combina com base instável e deslocamento na areia.")
                ],
                beachRefs,
                "A ausência de quique e a areia mudam completamente tempo de reação, deslocamento e estabilidade.")
        };

        return Ok(new TennisBeachTennisResponse(
            modalidades,
            "Sports Expansion I + ModelosSessoesTreino",
            "Tennis & Beach Tennis 2.0 compara superfícies, fundamentos e capacidades usando referências existentes. Não define carga, volume, intensidade, aptidão ou retorno ao esporte automaticamente."));
    }


    [HttpGet("swimming")]
    public async Task<ActionResult<SwimmingResponse>> Swimming(
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x => x.UsuarioId == currentUser.UserId && x.OrganizacaoId == currentUser.OrganizationId && x.Ativo, ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var modelos = await db.ModelosSessoesTreino.AsNoTracking()
            .Where(x => x.OrganizacaoId == currentUser.OrganizationId && x.Ativo)
            .OrderBy(x => x.Categoria).ThenBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, x.Categoria, x.Descricao })
            .ToListAsync(ct);

        var referencias = modelos
            .Where(x =>
            {
                var texto = $"{x.Nome} {x.Categoria} {x.Descricao}".ToLowerInvariant();
                return new[] { "natação", "natacao", "nado", "crawl", "piscina", "aquático", "aquatico", "ombro", "escápula", "escapula", "core", "cardio" }
                    .Any(t => texto.Contains(t, StringComparison.Ordinal));
            })
            .Take(8)
            .Select(x => new SwimmingSessaoReferenciaResponse(
                x.Id, x.Nome, x.Categoria, x.Descricao,
                "Sessão-modelo existente com termos relacionados a natação, técnica aquática ou capacidades de suporte."))
            .ToArray();

        var estilos = new SwimmingEstiloResponse[]
        {
            new("crawl", "Crawl", "Nado alternado com rotação longitudinal e respiração coordenada.", "Alinhamento, entrada da mão, apoio aquático, rotação e timing respiratório."),
            new("costas", "Costas", "Nado alternado em posição dorsal com rotação do tronco.", "Linha corporal, rotação, entrada da mão e continuidade da braçada."),
            new("peito", "Peito", "Nado simultâneo com ciclo coordenado de braços, respiração e pernada.", "Timing entre puxada, respiração, recuperação, pernada e deslize."),
            new("borboleta", "Borboleta", "Nado simultâneo com ondulação corporal e ação coordenada dos braços.", "Ritmo da ondulação, estabilidade do tronco, recuperação dos braços e respiração.")
        };

        var fundamentos = new SwimmingFundamentoResponse[]
        {
            new("respiracao-alinhamento", "Respiração e alinhamento", "Organizar posição corporal e respiração sem interromper excessivamente a continuidade do nado.", "Observe cabeça, quadril, linha corporal e se a respiração desorganiza o movimento."),
            new("bracada-propulsao", "Braçada e propulsão", "Criar apoio e deslocamento com trajetória eficiente dos membros superiores.", "Observe entrada, apoio, direção da força e recuperação sem impor um modelo único."),
            new("pernada", "Pernada", "Contribuir para equilíbrio, propulsão e sustentação conforme o estilo.", "Observe coordenação com tronco e braços, amplitude e continuidade."),
            new("saida-virada", "Saída e virada", "Organizar transições de borda e retomada do nado com controle técnico.", "Observe aproximação, orientação, impulsão e retomada antes de buscar velocidade.")
        };

        var capacidades = new SwimmingCapacidadeResponse[]
        {
            new("resistencia-aquatica", "Resistência aquática", "Sustentar técnica e organização do movimento ao longo do tempo na água."),
            new("ombro-escapula", "Ombro e escápula", "Dar suporte ao volume repetido de ações de membros superiores e controle escapular."),
            new("core-alinhamento", "Core e alinhamento", "Manter transmissão de força e posição corporal eficiente no meio aquático."),
            new("mobilidade", "Mobilidade útil", "Permitir posições necessárias ao gesto sem transformar amplitude isolada em objetivo automático.")
        };

        return Ok(new SwimmingResponse(
            estilos, fundamentos, capacidades, referencias,
            "Sports Expansion II + ModelosSessoesTreino",
            "Swimming 2.0 organiza estilos, fundamentos e capacidades e referencia conteúdo existente. Não prescreve metragem, séries, ritmo, volume, intensidade, águas abertas, aptidão ou retorno à água automaticamente."));
    }


    [HttpGet("sports-expansion-ii")]
    public async Task<ActionResult<SportsExpansionIIResponse>> SportsExpansionII(
        CancellationToken ct = default)
    {
        var pacienteExiste = await db.Pacientes.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentUser.UserId &&
                x.OrganizacaoId == currentUser.OrganizationId &&
                x.Ativo,
                ct);

        if (!pacienteExiste)
            return NotFound(new { message = "Paciente vinculado nao encontrado." });

        var modalidades = new SportsExpansionIIModalidadeResponse[]
        {
            new(
                "natacao",
                "Natação",
                "Modalidade aquática com técnica de nado, respiração, eficiência propulsiva e resistência específica.",
                ["Piscina", "Águas abertas"],
                ["Piscina", "Óculos", "Touca", "Prancha opcional"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("respiracao", "Respiração e alinhamento"),
                        new("bracada", "Braçada e propulsão"),
                        new("virada", "Saída e virada")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("resistencia-aquatica", "Resistência aquática"),
                        new("ombro-escapula", "Ombro e escápula"),
                        new("core", "Estabilidade de tronco")
                    ])
                ]),
            new(
                "triathlon",
                "Triathlon",
                "Integra natação, ciclismo e corrida, com transições e gestão de demandas entre modalidades.",
                ["Piscina", "Águas abertas", "Rua", "Ciclovia", "Transição"],
                ["Equipamento de natação", "Bicicleta", "Capacete", "Tênis"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("natacao", "Fundamentos de natação"),
                        new("ciclismo", "Controle e eficiência no ciclismo"),
                        new("corrida", "Técnica de corrida e transição")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("resistencia-multimodal", "Resistência multimodal"),
                        new("transicoes", "Transições"),
                        new("durabilidade", "Durabilidade de movimento")
                    ])
                ]),
            new(
                "artes-marciais",
                "Artes Marciais",
                "Família de modalidades de combate com base, deslocamento, coordenação, potência e controle técnico.",
                ["Tatame", "Dojo", "Academia", "Ringue"],
                ["Espaço seguro", "Aparadores opcionais", "Equipamento específico da modalidade"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("base-guarda", "Base e guarda"),
                        new("deslocamento", "Deslocamento"),
                        new("tecnica", "Técnica específica")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("potencia", "Potência"),
                        new("agilidade", "Agilidade e reação"),
                        new("condicionamento", "Condicionamento específico")
                    ])
                ]),
            new(
                "remo",
                "Remo",
                "Movimento cíclico de puxada e extensão coordenada, praticado na água ou em ergômetro.",
                ["Água", "Raia", "Indoor"],
                ["Barco e remo", "Remoergômetro"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("sequencia", "Sequência pernas–tronco–braços"),
                        new("retorno", "Retorno e recuperação"),
                        new("ritmo", "Ritmo técnico")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("cadeia-posterior", "Cadeia posterior"),
                        new("resistencia", "Resistência"),
                        new("core", "Estabilidade de tronco")
                    ])
                ]),
            new(
                "trekking",
                "Trekking",
                "Deslocamento prolongado em trilhas com variação de terreno, inclinação, apoio e contexto ambiental.",
                ["Trilha", "Montanha", "Parque", "Estrada de terra"],
                ["Calçado adequado", "Mochila", "Água", "Bastões opcionais"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("passada", "Passada em terreno irregular"),
                        new("subida-descida", "Subida e descida"),
                        new("orientacao", "Leitura de terreno e segurança")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("resistencia", "Resistência locomotora"),
                        new("forca-pernas", "Força de membros inferiores"),
                        new("equilibrio", "Equilíbrio e estabilidade")
                    ])
                ]),
            new(
                "recreativos",
                "Esportes recreativos",
                "Práticas de lazer e movimento que podem variar por ambiente, regras e recursos disponíveis.",
                ["Parque", "Praia", "Clube", "Quadra", "Área livre"],
                ["Recursos variáveis conforme a prática"],
                [
                    new("fundamentos", "Fundamentos", [
                        new("regras-basicas", "Regras e segurança básicas"),
                        new("coordenacao", "Coordenação"),
                        new("exploracao", "Exploração do movimento")
                    ]),
                    new("preparacao", "Preparação física", [
                        new("movimento-geral", "Capacidade geral de movimento"),
                        new("reacao", "Reação e adaptação"),
                        new("resistencia", "Resistência conforme o contexto")
                    ])
                ])
        };

        return Ok(new SportsExpansionIIResponse(
            modalidades,
            "Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão",
            "Sports Expansion II amplia a taxonomia e a descoberta. A estrutura não declara aptidão, não escolhe modalidade de combate, não define distância, volume, intensidade, rota ou retorno ao esporte automaticamente."));
    }

}
