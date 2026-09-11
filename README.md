# v0.5.1 — Solicitações Clínicas / Connected Care

> **Hotfix v0.5.1-r1:** corrige o fluxo de migrations para bancos atualizados a partir da v0.5.0. A versão funcional da API permanece 0.5.1.

A v0.5.1 é a primeira entrega funcional após o marco estável v0.5.0 (508/508). Ela cria um fluxo bidirecional simples entre profissional e paciente para tarefas clínicas.

## O que entrou
- nova entidade `SolicitacaoClinica`, isolada por organização;
- profissional cria solicitações por paciente com tipo, título, orientação e prazo;
- histórico da solicitação acessível no fluxo clínico do paciente;
- paciente ganhou a área **Solicitações** no portal;
- paciente pode responder/concluir uma solicitação com texto e link de referência;
- profissional pode revisar a devolutiva ou cancelar uma solicitação;
- todas as mudanças de estado são auditadas;
- novo upgrade SQL idempotente `v0.5.1_solicitacoes_clinicas.sql`;
- PREPARAR passa de 30 para 31 etapas;
- suíte de fumaça passa de 508 para 518 verificações.

## Escopo deliberadamente deixado para versões seguintes
Upload físico de arquivos, notificações automáticas e protocolos recorrentes ainda não fazem parte desta entrega. O campo de link permite validar o ciclo antes de introduzir armazenamento de documentos.

---

# v0.5.0 — Connected Care: marco estável

A v0.5.0 promove a **v0.4.6**, validada com **508/508 testes de fumaça**, como novo marco estável do HealthPlatform. Esta promoção encerra o ciclo 0.4.x sem introduzir schema novo, regra clínica nova ou mutação de dados.

## O que esta versão representa
- consolidação da experiência **Connected Care / Patient Today** iniciada na v0.4.0;
- identidade visual RS e experiência mobile/tablet preservadas;
- runtime, healthcheck, Swagger, interface e scripts identificados como 0.5.0;
- suíte `TESTAR.ps1` atualizada para exigir 0.5.0;
- histórico de versões agora acompanhado também por `CHANGELOG.md`;
- baseline anterior confirmada: **v0.4.6 — 508/508**.

## Regra de versionamento daqui para frente
- **feature**: sobe versão e descreve funcionalidade entregue;
- **hotfix**: usa sufixo `-rN`;
- **promoção técnica**: será declarada explicitamente e não será apresentada como feature.


# v0.3.27 — Correção do smoke test administrativo

O teste 32 agora valida as rotas reais usadas pela interface:
- `/api/alimentos/`;
- `/api/exames/marcadores/`;
- `/api/anamnese/perguntas/`;
- `/api/configuracoes/organizacao`;
- edição do profissional.

Nenhuma alteração de banco ou migration.


# v0.3.27

Correção: o endpoint `/api/health` agora reporta corretamente a versão `0.3.27`.


## v0.3.27 — Catálogos + Configurações

Nesta versão a administração dos catálogos passa a existir na própria interface web.

- nova área **Configurações**;
- resumo da organização, usuário e profissional logado;
- gestão visual do catálogo de alimentos;
- criação de marcadores laboratoriais;
- criação de perguntas personalizadas da anamnese;
- sem migration nova;
- smoke test ampliado para 30 etapas.

Fluxo:

```powershell
.\PREPARAR.ps1
.\RODAR.ps1
.\TESTAR.ps1
```

# HealthPlatform v0.3.27 — Edição Clínica + Agenda Operacional

Evolução da v0.2.4 validada. Esta versão torna o prontuário e a agenda mais operacionais diretamente no navegador.

## Novidades
- edição visual de consultas existentes;
- edição visual de anamneses existentes;
- edição visual de avaliações corporais existentes;
- novo `PUT /api/avaliacoes/{id}` com auditoria;
- seletor de registro quando houver múltiplos itens no prontuário;
- agenda com ações rápidas: Confirmar, Realizada, Falta e Cancelar;
- reagendamento visual com data/hora local e offset do navegador;
- auditoria já existente para atualização/status/reagendamento preservada;
- sem migration nova.

## Execução
```powershell
.\PREPARAR.ps1
.\RODAR.ps1
```
Em outro PowerShell:
```powershell
.\TESTAR.ps1
```
Acesse `http://localhost:5180`.


## Hotfix v0.4.0-r1
- Corrige o smoke test local do `POPULAR-REMOTO-RICO.ps1`, atualizando as assinaturas verificadas para a implementação rica V2 atual.
- Não altera schema, API, regras clínicas ou dados.

## Hotfix v0.4.4-r1

- Corrige a validação [60/508] do `TESTAR.ps1` para não depender do texto acentuado `Histórico recente` retornado via HTTP no Windows PowerShell.
- O teste agora valida a rota real de histórico de treino, a coleção `h.execucoes` e o carregador `loadPatientWorkout`.
- Nenhuma alteração de banco, API, regra clínica ou dados.

- Baseline anterior validada: v0.4.5 (508/508 testes de fumaça).
