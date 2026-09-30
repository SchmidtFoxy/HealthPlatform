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


}
