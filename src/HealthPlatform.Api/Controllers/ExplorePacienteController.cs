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
                "Referencia")
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
        new("ciclismo", "Ciclismo")
    ];

    private static string[] IntencoesExplore() =>
    [
        "QueroExperimentar",
        "QueroRetomar",
        "TenhoCuriosidade"
    ];


}
