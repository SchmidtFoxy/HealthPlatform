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


}
