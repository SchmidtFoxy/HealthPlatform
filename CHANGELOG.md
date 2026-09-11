# v0.6.4 — Ciclos Esportivos

- Novo `CicloEsportivoPaciente` para organizar objetivo, perfil esportivo, período e metas do ciclo.
- Ciclos podem vincular fases de treino e nutrição e mantêm apenas um ciclo ativo por paciente via fluxo profissional.
- Home Daily Athlete mostra semana atual, progresso temporal, treinos acumulados e prontidão média do ciclo.
- Leitura profissional recebe o mesmo ciclo em formato longitudinal e clínico.
- Missão semanal de treinos passa a respeitar `MetaTreinosSemanais` do ciclo ativo, com fallback seguro para 3.
- Upgrade `V064CiclosEsportivos`, SQL idempotente e PREPARAR 37/37.
- Suíte ampliada para 632 verificações.

# v0.6.3 — Missões & Conquistas

- Desafios semanais automáticos baseados em comportamento realmente registrado: treinos, prontidão e dias ativos.
- Recompensas de desafio entram no ledger de XP com idempotência; não existe botão manual para fabricar progresso.
- Conquistas persistentes para marcos esportivos e de autocuidado, como primeiro treino, 10 treinos, 7 check-ins e consistência 80+.
- Home Daily Athlete passa a mostrar progresso das missões e conquistas recentes.
- Nova entidades `DesafioSemanalPaciente` e `ConquistaPaciente`, upgrade `V063MissoesConquistas` e SQL idempotente.
- PREPARAR passa a 36/36 e suíte sobe para 624 verificações.

# HealthPlatform Changelog

## v0.6.2 — XP & Consistência

- Ledger auditável `EventosXp`, com idempotência por paciente + fonte + registro de origem.
- XP de autocuidado para check-in de prontidão e conclusão de metas.
- XP de treino baseado na adequação entre RPE executado e recomendação diária; sobrecarga deliberada recebe menos XP.
- Níveis de 500 XP, XP do dia, streak e score de consistência baseado em frequência sustentável nos últimos 14 dias.
- Nova faixa de evolução esportiva na Home do paciente e resumo de consistência para leitura profissional.
- Upgrade incremental `V062XpConsistencia`, SQL idempotente e PREPARAR 35/35.
- Suíte ampliada para 616 verificações.


## v0.6.1 — Daily Athlete / Prontidão Diária
**Tipo:** Feature release / início da linha Sports Medicine + Daily Care

- novo check-in matinal PatientOnly com sono, qualidade do sono, energia, dor, disposição e recuperação;
- score de prontidão de 0 a 100 com ponderação fisiológica simples e transparente;
- contexto do treino anterior entra no cálculo quando houve esforço percebido alto recentemente;
- travas de segurança impedem score artificialmente alto com dor elevada, recuperação muito baixa ou sono muito curto;
- recomendação diária entre Recuperação, Leve, Normal e Pesado, sem assumir que maior intensidade é sempre melhor;
- Home do paciente passa a destacar a prontidão e permitir atualização diária rápida;
- prontuário profissional mostra o mesmo dado em leitura objetiva diária, persistido para análise longitudinal;
- nova entidade `ProntidaoDiaria`, migration incremental `V061ProntidaoDiaria` e SQL idempotente;
- PREPARAR ampliado para 34/34;
- prepara a base para XP, níveis, streaks e desafios alinhados à adequação do comportamento, não ao excesso.


## v0.6.0-r1 — Hotfix da suíte de upgrade

- Corrige validações históricas do `TESTAR.ps1` que ainda tratavam `32/32` como a contagem atual do `PREPARAR`.
- A contagem anterior era `33/33` após a inclusão do upgrade de medicamentos da v0.6.0; na v0.6.1, a contagem atual é `34/34` com o upgrade de prontidão diária.
- Preserva as checagens dos upgrades históricos v0.5.1 e v0.5.8.
- Nenhuma alteração de schema, API ou regra clínica neste hotfix.

## v0.6.0 — Medicamentos e Adesão
**Tipo:** Feature release / novo marco Connected Care

- medicamentos estruturados por paciente;
- registro de tomadas pelo portal;
- aderência de 30 dias no prontuário;
- auditoria, PatientOnly e multi-tenant;
- migration incremental `V060MedicamentosAdesao`;
- SQL idempotente `v0.6.0_medicamentos.sql`;
- PREPARAR 34/34.

# v0.5.9-r1 — Hotfix de validações do protocolo

- corrige o teste 577 para reconhecer a rota do protocolo com `offsetMinutos`, introduzida pela aderência baseada no dia local;
- corrige o teste 578 para validar o upgrade histórico correto da v0.5.8 (`v0.5.8_protocolos_acompanhamento.sql`);
- não altera banco, schema, API ou regra clínica;
- mantém o fluxo oficial `PREPARAR -> RODAR -> TESTAR`.

# v0.5.9 — Aderência aos Protocolos
**Tipo:** feature release

- adiciona cálculo de aderência aos protocolos configuráveis;
- mostra ao paciente o que estava previsto e o que já foi concluído hoje;
- mostra ao profissional aderência consolidada dos últimos 7 dias;
- respeita frequência diária, dias da semana, semanal e sob demanda;
- considera pressão concluída apenas com sistólica + diastólica;
- respeita timezone local por offset do navegador;
- sem schema ou migration nova;
- suíte ampliada de 580 para 588 verificações.


## v0.5.8-r1 - Hotfix de validacao do PREPARAR
- Corrige o teste 517/580 para reconhecer corretamente o upgrade v0.5.1 na etapa 31/32.
- Preserva a etapa 32/32 para o upgrade v0.5.8 de protocolos de acompanhamento.
- Nenhuma alteracao de schema, API, regra clinica ou dados.
# v0.5.7 — Monitoramento Guiado
**Tipo:** feature release

- adiciona registros guiados de sinais vitais e sintomas ao Hoje do paciente;
- estrutura pressão arterial em sistólica/diastólica sem criar nova tabela;
- adiciona glicemia, frequência cardíaca, saturação e temperatura aos registros rápidos;
- adiciona resumo profissional dos últimos 7 dias com último valor, média, mínimo e máximo;
- adiciona endpoints profissional e PatientOnly de monitoramento;
- reutiliza o diário existente, sem schema ou migration nova;
- suíte ampliada de 561 para 570 verificações.

# v0.5.6 — Jornada do Paciente
**Tipo:** feature release

- adiciona timeline própria no portal do paciente;
- consolida consultas, avaliações, exames, metas, diário, check-ins, treinos e solicitações;
- adiciona filtro de período e atalhos contextuais;
- mantém dados clínicos internos/ SOAP fora da visão simplificada do paciente;
- preserva `PatientOnly` e isolamento organizacional;
- sem schema novo e sem migration nova;
- suíte ampliada de 553 para 561 verificações.

# v0.5.5 — Desde a última consulta
**Tipo:** feature release

- adiciona resumo longitudinal ao prontuário profissional;
- usa a última consulta como início do período, com fallback seguro de 30 dias;
- agrega registros do paciente, treinos, check-ins e médias de adesão;
- mostra peso inicial/atual e variação quando houver avaliações suficientes;
- consolida solicitações pendentes e respostas aguardando revisão;
- sem schema novo e sem migration nova.

# Changelog


## v0.5.4 — Feature release

- Integra Solicitações Clínicas ao **Hoje** do paciente.
- Solicitações pendentes passam a contar como atividades do acompanhamento diário.
- Adiciona destaque e resposta direta para solicitações vencidas/pendentes.
- Integra respostas aguardando revisão e solicitações vencidas à **Central do Dia** profissional.
- Dashboard profissional passa a destacar solicitações para revisão.
- Sem migration/schema novo; reutiliza o schema v0.5.1.
- Suíte ampliada de 535 para 545 verificações.

## v0.5.3 — Feature release

- Adiciona Central profissional de Solicitações Clínicas.
- Consolida solicitações de todos os pacientes em uma única fila operacional.
- Adiciona busca por paciente/título/tipo e filtros por status/prazo.
- Exibe métricas de pendentes, respostas para revisão e vencidas.
- Permite revisar, cancelar e abrir o prontuário diretamente da central.
- Preserva isolamento multi-tenant e reutiliza o schema v0.5.1.
- Sem migration/schema novo.
- Suíte ampliada de 526 para 535 verificações.

## v0.5.2 — Feature release

- Integra Solicitações Clínicas ao motor de notificações internas.
- Paciente recebe lembretes contextuais com prioridade baseada no prazo.
- Profissional recebe aviso quando uma solicitação é respondida.
- Navegação contextual: paciente → Solicitações; profissional → prontuário do paciente.
- Notificações são desativadas automaticamente quando o estado deixa de exigir ação.
- Sem schema novo.
- Suíte ampliada de 518 para 526 verificações.

## v0.5.1-r1 — Hotfix de migration

- Corrige o `PREPARAR.ps1` em instalações que já possuem a migration baseline da v0.5.0.
- Gera e normaliza a migration incremental `V051SolicitacoesClinicas` antes de `database update`.
- Mantém instalações novas usando a `InitialCreate` atual, sem migration incremental redundante.
- Não suprime `PendingModelChangesWarning`, não recria o banco e não altera dados existentes.
- Mantém o upgrade SQL v0.5.1 idempotente como camada adicional de compatibilidade.

## v0.5.1 — Solicitações Clínicas
**Tipo:** feature release.

- adiciona solicitações clínicas profissional → paciente;
- adiciona resposta/conclusão pelo portal do paciente;
- adiciona revisão/cancelamento profissional;
- adiciona auditoria e isolamento multi-tenant;
- adiciona schema idempotente e etapa 31/31 no PREPARAR;
- adiciona cobertura estrutural à suíte de fumaça.

# HealthPlatform — Changelog

## v0.5.8 — Protocolos de Acompanhamento
**Tipo:** feature

- adiciona protocolo configurável por paciente;
- permite definir métrica, frequência, horário, unidade e instruções;
- paciente vê registros rápidos filtrados pelo protocolo ativo;
- profissional visualiza e gerencia protocolo junto ao monitoramento remoto;
- adiciona schema `ProtocolosAcompanhamento` e migration incremental compatível com upgrades;
- amplia smoke test para 580 verificações.


## v0.5.0 — Connected Care Stable Milestone
**Tipo:** marco de versão / promoção estável

- promove a v0.4.6, validada com 508/508 testes, como nova baseline;
- consolida a experiência Patient Today introduzida no ciclo v0.4.x;
- mantém identidade visual RS e UX responsiva para desktop, tablet e mobile;
- atualiza runtime, healthcheck, Swagger, interface e scripts para 0.5.0;
- não adiciona schema, migration, regra clínica ou mutação de dados.

## Ciclo v0.4.x — resumo real

### v0.4.0 — Connected Care / Patient Today
**Tipo:** feature

- nova experiência diária do paciente;
- registros rápidos de peso, água, sono, dor, energia e sintomas;
- integração com metas, próxima consulta, plano alimentar, exames e evolução;
- reutilização do `RegistroDiarioPaciente`, sem criar estrutura paralela.

### v0.4.0-r1
**Tipo:** hotfix de teste

- atualiza o smoke test do `POPULAR-REMOTO-RICO.ps1` para a implementação V2 atual;
- sem alteração funcional ou de banco.

### v0.4.1
**Tipo:** promoção técnica

- promoção da baseline 0.4.0-r1;
- sem funcionalidade clínica nova.

### v0.4.2
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.3
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.4
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.4-r1
**Tipo:** hotfix de teste

- corrige a validação 60/508 para não depender de texto acentuado no Windows PowerShell;
- passa a validar rota e estrutura reais do histórico de treino;
- sem alteração funcional ou de banco.

### v0.4.5
**Tipo:** promoção técnica

- promoção da baseline 0.4.4-r1 validada com 508/508;
- sem funcionalidade clínica nova.

### v0.4.6
**Tipo:** promoção técnica

- promoção da baseline 0.4.5 validada com 508/508;
- sem funcionalidade clínica nova.

## Próximo ciclo funcional

O ciclo v0.5.x será usado para evoluções reais do Connected Care, priorizando a ponte profissional ↔ paciente. As próximas entregas devem privilegiar módulos como solicitações clínicas, tarefas/acompanhamento, protocolos configuráveis e comunicação contextual, e cada release deverá declarar explicitamente o que entrou.

### v0.5.4-r1 — Hotfix de testes
- Corrige validações do TESTAR.ps1 que dependiam de textos acentuados servidos via HTTP.
- As verificações agora usam rotas, funções e seletores técnicos estáveis.
- Nenhuma alteração de schema, API, regra clínica ou dados.
