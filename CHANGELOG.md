## v0.10.6 r1 — alinhamento das sentinelas da timeline
- Explicita no HistoricoProgressaoService que a timeline nasce de **eventos realmente registrados** e persistidos.
- Explicita a trava **nao ranqueia eventos** com a mesma formulacao usada pela suíte de compatibilidade.
- Nenhuma alteracao funcional, de schema ou de versao semantica.

## v0.10.6 — Histórico de Progressões & Linha do Tempo Esportiva
- organiza eventos de progressão realmente registrados em uma timeline longitudinal;
- exibe aplicação, eixo, profissional, duração em observação, encerramento e observações;
- associa a interpretação longitudinal atual somente ao evento temporal correspondente;
- eventos antigos sem leitura comparável permanecem como histórico registrado, sem fabricar resposta retrospectiva;
- não ranqueia progressões e não cria score de melhor/pior resposta;
- sem nova migration: schema permanece 38/38.

## v0.10.5 — Interpretação Longitudinal da Resposta
- integra o comparativo pré/pós, o evento registrado e a reavaliação supervisionada;
- classifica o padrão observado como DadosInsuficientes, EmFormacao, Estavel, Favoravel, Misto ou Atencao;
- preserva prontidão, recuperação, dor, carga e volume como eixos independentes;
- não usa score composto e não transforma melhora temporal em causalidade;
- sinais clínicos de atenção prevalecem sobre leitura isolada de performance/carga;
- padrão favorável não libera nova progressão automática;
- sem nova migration: schema permanece 38/38.

## v0.10.4 — Comparativo Pré/Pós-Progressão
- usa o evento real da v0.10.3 como marco temporal;
- compara até 7 dias antes e depois em recuperação, dor, prontidão, carga interna e volume estimado;
- distingue janela em formação, dados insuficientes e janela comparável;
- associação temporal não é tratada como causalidade;
- não cria nova progressão, score causal ou prescrição automática;
- sem nova migration: schema permanece 38/38.


## v0.10.3 r3 — alinhamento das sentinelas PREPARAR 38/38
- Corrige smoke tests legados que ainda esperavam denominador `/37` nos upgrades v0.5.1–v0.6.4.
- Atualiza as verificações de total atual do PREPARAR para `38/38`, preservando a ordem histórica dos upgrades.
- Nenhuma regra funcional, migration ou versão semântica foi alterada.

## v0.10.3 r1 — migration EF do evento de progressão

- Corrige `PendingModelChangesWarning` do EF Core 10 no `PREPARAR.ps1`.
- O setup agora gera a migration incremental `V0103EventosProgressaoSupervisionada` antes do `database update` em bases existentes.
- O `ModelSnapshot` passa a ser atualizado pelo próprio `dotnet-ef`, mantendo o SQL `v0.10.3_eventos_progressao_supervisionada.sql` como proteção idempotente da etapa 38/38.
- Todas as etapas do preparo agora anunciam o total 38/38.

## v0.10.3 — Registro de Evento de Progressão
- adiciona marco temporal explícito para mudanças realmente aplicadas por profissional;
- novo endpoint para registrar/listar/encerrar eventos supervisionados;
- nova tabela EventosProgressaoSupervisionada e upgrade 38/38;
- home do atleta e profissional passa a distinguir sugestão de mudança realmente registrada;
- nenhuma causalidade é inferida automaticamente a partir do evento.


## v0.10.3 r2 — proteção contra mesclagem de fontes antigas
- PREPARAR.ps1 valida e restaura automaticamente os dois controllers da Home antes do build quando detectar o cabeçalho corrompido da r0.
- Inclui cópias canônicas em scripts/recovery e o utilitário CORRIGIR-FONTES-v0.10.3.ps1 para reparo manual determinístico.
- Nenhuma alteração funcional, de schema ou de versão semântica.


## v0.10.3 r1 — correção de compilação das Homes

- Corrigidos os cabeçalhos `using` de `MeuPortalPacienteController` e `PortalPacienteController`, corrompidos durante a integração da reavaliação de progressão.
- `reavaliacaoProgressao` agora é passado explicitamente ao `PortalPacienteHomeResponse` nas duas Homes.
- Sem alteração de schema, migration ou regra funcional da v0.10.3.
# Changelog

## v0.10.3 — Reavaliação da Progressão & Decisão de Continuidade
- transforma o monitoramento em decisão transparente: Manter, Revisar, Encerrar ou AguardarDados;
- encerramento significa apenas finalizar a janela atual de observação, sem inferir sucesso causal;
- revisão clínica prevalece sobre performance favorável;
- falta de dado não vira falha, cobrança ou progressão automática;
- integração nos portais do atleta e profissional, sem nova migration.

## v0.10.1 — Monitoramento de Resposta à Progressão
- adiciona leitura transparente de recuperação, carga e performance ao redor do plano supervisionado;
- estados Estável, Observar, Revisar e DadosInsuficientes, sem score opaco;
- revisão clínica prevalece sobre qualquer novo avanço;
- não atribui causalidade sem registro temporal da mudança e não prescreve nova progressão;
- integração nos portais do atleta e profissional, sem nova migration.

﻿﻿# v0.10.0 — Plano de Progressão Supervisionada

- adiciona `PlanoProgressaoSupervisionadaService`;
- integra atleta e profissional nas duas Homes;
- estrutura critérios transparentes para discussão de uma única progressão;
- preserva bloqueios de recuperação, carga e dados insuficientes;
- não cria meta, carga, volume, calorias ou medicação automaticamente;
- sem migration; schema permanece 37/37;
- suíte ampliada para 920 verificações.

# v0.9.9 — Decisão Assistida de Progressão

- adiciona `DecisaoProgressaoService` depois da Janela de Progressão;
- organiza até 3 opções transparentes e sugere apenas um eixo para discussão por vez;
- considera prioridade do ciclo e perfil/objetivo esportivo sem produzir prescrição automática;
- janela fechada, recuperação/carga em revisão ou dados insuficientes continuam bloqueando nova exigência;
- não define carga, volume, calorias, medicação nem cria meta automaticamente;
- integrado às Homes do atleta e profissional sem migration; schema permanece 37/37.

# v0.9.8 — Janela de Progressão & Critério de Avanço

- Nova camada `JanelaProgressaoService` com critérios transparentes e independentes para considerar uma progressão.
- Expõe base comportamental, ausência de oscilação, recuperação e carga recente sem condensar os eixos em score opaco.
- Recuperação/carga em atenção alta bloqueiam progressão; dados insuficientes viram observação, nunca reprovação.
- Uma janela aberta apenas autoriza considerar uma progressão pequena; não cria meta, aumenta carga ou altera plano automaticamente.
- Integrado às Homes do atleta e profissional sem nova migration; schema permanece 37/37.

# v0.9.7 — Reentrada Gradual de Desafio

- avalia quando a rotina em manutencao tem espaco para um unico novo desafio;
- retomada, habitos oscilando ou consolidando bloqueiam aumento prematuro de exigencia;
- elegibilidade nao cria meta automaticamente, nao altera prescricao e nao cria score opaco;
- cards equivalentes para atleta e profissional;
- sem migration nova; schema permanece 37/37.

# Changelog

## v0.9.7-r1 — Hotfix de build local

- `PREPARAR.ps1` encerra automaticamente uma instância local antiga da `HealthPlatform.Api` antes do `dotnet build`.
- Evita falhas `MSB3021/MSB3027` causadas por DLLs bloqueadas pelo processo da API ainda em execução.
- Não altera schema, versão semântica ou regras funcionais da v0.9.7.

## v0.9.7 — Encerramento do Ciclo de Hábito & Transição para Manutenção

- adiciona leitura de encerramento do foco de hábito;
- diferencia foco em curso, consolidando, pronto para manutenção, manutenção e proteção;
- foco encerrado sai do destaque sem disparar automaticamente outra meta;
- proteção de retomada continua acima de qualquer transição de foco;
- sem XP negativo, punição de streak, score opaco ou alteração automática de prescrição;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 888 verificações.

# v0.9.5 — Revisao do Proximo Foco & Ciclo de Habito

- revisa o foco atual em Continuar, Consolidar, Manutencao ou Proteger;
- usa a tendencia semanal correspondente ao eixo do foco;
- impede troca precoce de foco e aumento automatico de exigencia;
- integra atleta e profissional sem migration nova.

# Changelog

## v0.9.4 — Manutencao Sustentavel & Proximo Foco

- adiciona um proximo foco unico para a rotina, priorizando eixos oscilando antes dos que ainda consolidam;
- habitos estaveis passam a ficar explicitamente em modo de manutencao, sem aumento automatico de cobranca;
- protecao de retomada/recuperacao tem precedencia sobre qualquer novo foco;
- integra a mesma leitura nas Homes do atleta e do profissional;
- nao cria score opaco, nao pune XP/streak e nao altera prescricao automaticamente;
- schema permanece 37/37, sem migration nova;
- suite ampliada para 872 verificacoes.

# v0.9.3 — Estabilidade de Hábitos & Consolidação da Rotina

- Nova leitura de estabilidade por comportamento: estável, consolidando, oscilando ou sem dados.
- Eixos transparentes: consistência, dias ativos, nutrição, hidratação e contexto de recuperação.
- Hábitos estáveis não geram cobrança extra; oscilações recebem próxima ação simples e sustentável.
- Sem score opaco, XP negativo, punição de streak ou alteração automática de prescrição.
- Sem migration; schema permanece 37/37.

# v0.9.2 — Proteção da Retomada & Continuidade

- adiciona `ProtecaoRetomadaService` para reconhecer retomada recente por streak curto, aumento de dias ativos e consistência ainda em consolidação;
- protege descanso/redução de ritmo coerentes com recuperação contra falso rótulo de recaída;
- limita a leitura a até 3 sinais e não cria score/probabilidade de recaída;
- adiciona cards para atleta e profissional;
- sem XP negativo, punição de streak ou compensação;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 856 verificações.

## v0.9.1 — Plano de Reconexão & Retomada Sustentável
- Converte sinais do Radar de Adesão em até 3 passos pequenos e auditáveis de retomada.
- Protege descanso/redução de ritmo quando há contexto de recuperação.
- Usa o roteiro do dia e prioridades já existentes; não cria prescrição paralela.
- Sem XP negativo, punição de streak, treino dobrado ou restrição compensatória.
- Integração atleta + profissional.
- Sem migration; schema permanece 37/37.
- Suíte ampliada para 848 verificações.


### v0.9.0 r1 — correção de compilação do Radar de Adesão

- corrige `RadarAdesaoService` para usar `PortalHidratacaoContextualResponse.MetaMl`, nome real do contrato de hidratação;
- adiciona sentinela estática para impedir novo desalinhamento entre o radar e o DTO de hidratação;
- sem alteração de schema, migration ou regra funcional do radar.
# HealthPlatform v0.9.0 — Radar de Adesão & Continuidade do Plano

- Novo radar de adesão/continuidade com sinais auditáveis.
- Consistência, ritmo semanal, nutrição e hidratação como eixos separados.
- Contexto de recuperação impede que descanso adequado seja interpretado como falha.
- Sem probabilidade de abandono, score opaco, XP negativo ou punição.
- Integração atleta + profissional.
- Sem migration; schema permanece 37/37.
- Suíte ampliada para 840 verificações.

# HealthPlatform v0.8.9 — Tendência Semanal & Comparativo de Semanas

- compara a semana atual com o mesmo número de dias da semana anterior;
- compara ritmo de treinos, dias ativos, prontidão média, carga interna estimada, adesão nutricional registrada e hidratação registrada;
- semanas parciais não são comparadas injustamente com semanas completas;
- carga/frequência maiores não são rotuladas automaticamente como melhores;
- sem score semanal composto, diagnóstico ou alteração automática de prescrição;
- mesma leitura para atleta e profissional;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 832 verificações.

# HealthPlatform v0.8.8 — Resumo Semanal & Fechamento da Semana

- Nova síntese semanal derivada do planejamento, recuperação, carga, nutrição, hidratação e execução diária.
- Estados transparentes: Executando, Equilibrada, Observar e Revisar.
- Sem score único, sem diagnóstico e sem alteração automática de prescrição.
- Integração na Home do atleta e no prontuário profissional.
- Sem migration nova; schema permanece 37/37.
- Suíte ampliada para 824 verificações.

# HealthPlatform v0.8.7 — Planejamento Semanal Adaptativo

- organiza até 5 focos da semana usando prioridades, metas, checkpoint e Estratégia do Dia;
- recuperação/carga têm precedência sobre perseguir volume semanal;
- mostra treinos concluídos/restantes apenas como referência da meta do ciclo;
- não cria exercícios, não redistribui carga e não altera prescrição automaticamente;
- sem migration; schema permanece 37/37;
- suíte ampliada para 816 verificações.

# Changelog

## v0.8.7 — Ações Prioritárias do Ciclo
- Consolida tendência por objetivo, checkpoint, recuperação, carga, nutrição e hidratação em até 3 prioridades semanais.
- Recuperação e equilíbrio de carga têm precedência sobre metas de volume/performance quando há sinais de atenção.
- Cada prioridade informa motivo e ação, sem score opaco e sem alterar prescrição automaticamente.
- Cards equivalentes para atleta e profissional.
- Sem migration nova; schema permanece 37/37.
- Suíte ampliada para 808 verificações.

# HealthPlatform v0.8.5 — Tendência por Objetivo do Ciclo

- Nova leitura contextual por perfil esportivo (hipertrofia, força/performance, emagrecimento, corrida/condicionamento, qualidade de vida ou personalizado).
- Prioriza eixos relevantes ao objetivo sem criar score único.
- Peso continua descritivo e só ganha direção quando existe peso-alvo definido pelo profissional.
- Integração nas Homes do atleta e do profissional.
- Sem migration nova; schema permanece 37/37.
- Suíte ampliada para 800 verificações.

# v0.8.5 — Comparativo de Ciclos & Tendência de Longo Prazo

- compara até quatro ciclos esportivos usando taxas semanais, não apenas totais brutos;
- apresenta treinos/semana, check-ins/semana, prontidão média e variação de peso por ciclo;
- compara o ciclo mais recente com o anterior sem criar score composto;
- variação de peso permanece descritiva, sem classificação automática de bom/ruim;
- mesma leitura para atleta e profissional;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 792 verificações.

## v0.8.5-r1

- Corrige sentinelas correntes do TESTAR.ps1 para esperar API e identidade MVP Preview v0.8.5.
- Mantém validações históricas da v0.8.2 intactas.

# v0.8.5 — Fechamento de Ciclo & Relatório de Evolução

- Adiciona `RelatorioCicloService` como síntese longitudinal do ciclo ativo.
- Consolida volume realizado, metas mensuráveis, evolução multidimensional e checkpoint.
- Introduz estados `EmCurso`, `Evoluindo`, `Revisar` e `ProntoParaFechamento`.
- Adiciona cards de relatório na Home do atleta e no prontuário profissional.
- Mantém cada dimensão auditável, sem score clínico composto.
- Não cria automaticamente próximo ciclo e não altera prescrição.
- Mantém schema 37/37, sem migration nova.
- Amplia a suíte para 784 verificações.

---

# v0.8.2 — Revisão de Ciclo & Checkpoint de Progresso

- Adicionado `CheckpointCicloService`.
- Novo contrato `PortalCheckpointCicloResponse` com evidências transparentes.
- Integração nas Homes do atleta e profissional.
- Estados de acompanhamento: EmCurso, Evoluindo, Revisar e Consolidar.
- Nenhum score composto opaco e nenhuma alteração automática da prescrição.
- Sem migration; PREPARAR permanece 37/37.
- TESTAR ampliado para 776 verificações e preservado em UTF-8 BOM.

# v0.8.1 — Metas do Ciclo & Progresso por Objetivo

- Acompanha metas mensuráveis já definidas no ciclo esportivo: treinos semanais, consistência e peso-alvo.
- Treinos da semana usam apenas sessões concluídas dentro da semana atual do ciclo.
- Peso-alvo usa distância absoluta ao alvo, funcionando tanto para perda quanto para ganho de peso.
- Objetivo textual do profissional permanece informativo e não é convertido em score opaco.
- Home do atleta e prontuário profissional recebem o mesmo acompanhamento.
- Progresso não altera automaticamente carga, dieta ou prescrição.
- Sem migration; PREPARAR permanece 37/37.
- Suíte ampliada para 768 verificações.

## v0.8.0 r11 — compatibilidade dos testes de volume

- Corrige sentinelas históricas `[429/600]`, `[430/600]` e `[431/600]` para validar a estrutura do painel de volume sem depender de rótulos acentuados sujeitos a diferenças de encoding no PowerShell.
- Nenhuma alteração funcional, de schema ou de versão semântica.


### v0.8.0-r8 — compatibilidade de testes dos gráficos de check-in
- Ajustada a sentinela `[372/600]` para validar os campos estruturais `adesaoAlimentacaoPercentual` e `adesaoTreinoPercentual`, evitando falsos negativos de encoding em rótulos acentuados.
- Nenhuma alteração funcional, de schema ou de versão semântica.


### v0.8.0 r6 — compatibilidade dos testes de gráficos de hábitos
- Corrige a sentinela `[314/600]` para validar `hpHabitCharts`, `hpLineChart`, `sonoHorasMedia` e `aguaLitrosDia` sem depender de literais acentuados no PowerShell.
- Nenhuma alteração funcional, de schema ou de versão semântica.
## v0.8.0-r2 — Hotfix de login local

- Em `Development`, o bootstrap do admin demo agora zera `AccessFailedCount` e remove `LockoutEnd` antes de validar/sincronizar a senha.
- Corrige `401` persistente após repetidas tentativas de smoke test com credenciais antigas.
- A alteração é restrita ao fluxo local de desenvolvimento; produção continua respeitando o lockout normal do Identity.

## v0.8.0-r1 — Hotfix de credencial local

- O runner local agora força `Seed__AdminEmail` e `Seed__AdminPassword` a partir do `appsettings.json` antes de iniciar a API.
- Evita `401` no `TESTAR.ps1` quando uma variável de ambiente antiga `Seed__AdminPassword` sobrescrevia a credencial local esperada.
- Sem mudança de schema ou versão semântica: `VERSION.txt` permanece `0.8.0`.

# v0.8.0 — Painel de Evolução Esportiva

- Consolida consistência, recuperação, carga, performance, nutrição, hidratação e ciclo em um painel longitudinal.
- Mantém cada indicador separado, com valor, referência, tendência e explicação; não cria score esportivo opaco.
- Integra o painel à Home do atleta e ao prontuário profissional.
- Sem migration; PREPARAR permanece 37/37.
- Suíte ampliada para 760 verificações.


### v0.7.9-r1 — correção de sentinela do MVP Preview
- Corrige o teste legado `[463/600]`, que ainda esperava `MVP Preview • v0.7.8` apesar da UI corrente já estar em `v0.7.9`.
- Nenhuma alteração de schema ou regra funcional.

# v0.7.9 — Hidratação Contextual & Balanço do Dia

- Resume meta hídrica ativa, consumo registrado e progresso do dia.
- Usa duração e RPE do treino apenas como contexto, sem aumentar automaticamente a meta.
- Integra hidratação ao Coach Diário e às Homes do atleta e do profissional.
- Reutiliza metas/registros existentes; sem migration nova.
- PREPARAR permanece 37/37; suíte ampliada para 752 verificações.

﻿
### v0.7.8 r1 — alinhamento de sentinelas de versão
- Corrige TESTAR.ps1 para esperar API 0.7.8 no healthcheck.
- Alinha Swagger, banner de login, log de bootstrap e scripts/run.ps1 com 0.7.8.
- Sem alteração de schema ou regra funcional de adesão nutricional.
# v0.7.8 — Adesão Nutricional Contextual

- Nova síntese diária de adesão às refeições do plano ativo.
- Paciente registra refeição como Realizada, Adaptada ou Não realizada.
- Registros idempotentes por refeição/dia no Diário; nenhuma migration nova.
- Coach Diário considera ausência/padrão de registros sem prescrever compensações.
- Home do atleta e prontuário profissional recebem card nutricional.
- PREPARAR permanece 37/37.
- Suíte ampliada para 744 verificações.

# v0.7.7 — Plano de Recuperação Contextual

- Adiciona síntese diária de recuperação baseada em prontidão, dor localizada, tendência de recuperação, carga, estratégia e execução.
- Prioriza proteção de região dolorosa, sono, consolidação de carga, hidratação e encerramento do dia quando aplicável.
- Não diagnostica lesão, não prescreve tratamento e não altera automaticamente a prescrição profissional.
- Integra a leitura na Home do atleta e no prontuário profissional.
- Sem migration nova; schema permanece 37/37.


### v0.7.6 r2 — correção de consistência do resumo de dor
- Alinha a variável interna `impactoMaximoTreino7` ao contrato `ImpactoMaximoTreino7`, preservando a mesma regra de cálculo.
- Corrige a validação estática `[722/728]` sem alterar schema, API ou comportamento clínico.
# v0.7.6 — Dor por Região Corporal

- Registro de região, lado, intensidade e impacto no treino usando o Diário existente.
- Resumo longitudinal de 7 dias no portal do atleta e prontuário profissional.
- Coach Diário passa a priorizar dor localizada relevante sem diagnosticar lesão.
- Sem migration nova; schema permanece 37/37.

# HealthPlatform v0.7.5 — Seed Ana v7 & Variáveis Automáticas PowerShell

- Hotfix r1: corrige as sentinelas [697] e [705] da suíte que ainda procuravam o banner legado v6 em vez do seed corrente v7.

- Corrige o seed da Ana para não sobrescrever `$HOME`, variável automática somente leitura do PowerShell.
- O resumo final agora usa `$homeResumo`.
- Adiciona proteção de regressão para `HOME`, `PID`, `PROFILE`, `HOST` e `PSHOME`.
- Mantém os 56 dias de histórico, avaliações corporais, carga, performance, gamificação e Coach Diário.
- Sem alteração de schema; PREPARAR permanece 37/37.
- Suíte ampliada para 720 verificações.

# v0.7.4 — Seed Ana v6 & Validação PowerShell
- Corrigido falso positivo da validação numérica: textos como `7d` dentro de strings não são mais tratados como sufixos de código.
- Resumo do seed agora usa `7 dias` explicitamente.

- Corrigido `POPULAR-ANA-RIBEIRO.ps1`: o literal `2.5m` era sintaxe C# e no PowerShell era interpretado como comando inexistente.
- Ajuste do deload agora usa `[decimal]2.5`.
- Seed da Ana atualizado para v6 mantendo 56 dias, Coach, carga, performance, prontidão, metas e avaliações corporais.
- Adicionadas verificações contra sufixos numéricos incompatíveis em scripts PowerShell.
- Sem migration nova; schema da v0.6.4 permanece suficiente.
- API/UI/VERSION atualizados para 0.7.4.
- Suíte ampliada para 712 verificações.

# v0.7.2 — Seed Resiliente & Diagnóstico de Dados

- Corrige o seed pesado da Ana Ribeiro que podia abortar com `400 Bad Request` sem revelar o endpoint ou a resposta da API.
- `Api()` agora inclui método, rota e corpo de erro retornado pelo backend.
- Metas demonstrativas passaram a ser opcionais: falha em uma meta não impede a população das demais camadas.
- Histórico de metas é protegido contra recurso ausente e falha individual.
- Seed acumula avisos não fatais e mostra um resumo ao final.
- Mantém URL local 5180, 56 dias de histórico e proteção contra `$PID`.
- `VERSION.txt`, API, Swagger e UI atualizados para 0.7.2.
- Sem migration nova; `PREPARAR` permanece 37/37.
- Suíte: 696 verificações.

# v0.7.1 — Coach Diário & Prioridades Contextuais

- Adiciona `CoachDiarioService` como camada derivada e explicável.
- Combina prontidão, estratégia, recuperação, carga, performance, execução e ciclo.
- Prioriza até três ações do dia com motivo e ação recomendada.
- Inclui travas explícitas contra diagnóstico, prescrição paralela e progressão automática de carga.
- Exibe síntese para atleta e leitura equivalente no prontuário profissional.
- Mantém o schema em 37/37; nenhuma migration nova.
- Amplia a suíte de 672 para 680 verificações.
- Atualiza `POPULAR-ANA-RIBEIRO.ps1` para a linha 0.7.x.

# v0.6.9-r5

- Corrige `VERSION.txt` para a versao semantica `0.6.9`, preservando revisoes apenas em logs/changelog.
- Mantem o runner local fixado em `http://localhost:5180` e a sincronizacao de credencial local.

# v0.6.9-r4

- Corrige o runner local para fixar explicitamente `http://localhost:5180` mesmo usando `--no-launch-profile`.
- Mantém Development explícito e sincronização da credencial local do admin.
- Evita divergência entre a porta real da API e TESTAR/DEMO/POPULAR.

## v0.6.9-r3 — correção de bootstrap local

- Corrige sincronização da senha do admin também no fluxo Development.
- RODAR.ps1 força Development explicitamente e evita instância dotnet antiga na porta 5180.
- Mantém a versão funcional/API em 0.6.9 e a suíte em 672 verificações.

## v0.6.9-r1 — Hotfix da linha de base + seed Ana Ribeiro

- Corrige a linha de base de carga para três janelas semanais explícitas anteriores aos 7 dias atuais.
- Alinha `RelacaoCargaComBase` entre motor, contrato e teste 667/672.
- Adiciona `POPULAR-ANA-RIBEIRO.ps1` para gerar histórico esportivo rico e idempotente da paciente demo.

# v0.6.9 — Carga de Treino & Equilíbrio

- Adicionado `CargaTreinoService` com carga interna estimada por sessão (`duração × RPE`).
- Comparação dos últimos 7 dias com a média semanal das 3 semanas anteriores.
- Classificação observacional: DadosInsuficientes, ConstruindoBase, AbaixoDaBase, Coerente, AcimaDaBase e Revisar.
- Combinação contextual com prontidão, dor e recuperação recentes.
- Cards específicos para atleta e profissional, com linguagem não diagnóstica.
- Nenhuma migration: leitura calculada sobre execuções e prontidão já persistidas.
- Suíte ampliada para 672 verificações.

# v0.6.8 — Performance & PRs

- Linha de performance derivada diretamente das execuções reais de treino.
- Melhor carga por exercício, melhor volume estimado e evolução desde a primeira marca registrada.
- Detecção conservadora de novos PRs: exige marca anterior e considera PR recente apenas nos últimos 7 dias.
- Contexto de prontidão exibido no dia da melhor marca quando houver check-in correspondente.
- Comparação de volume estimado dos últimos 28 dias contra os 28 dias anteriores.
- Home do atleta com evolução esportiva e lembrete explícito de que PR não é meta diária.
- Prontuário profissional com performance, volume, PRs e contexto de recuperação.
- Sem nova migration: dados são calculados a partir do histórico já existente.
- PREPARAR permanece 37/37.
- Suíte ampliada para 664 verificações.

# v0.6.7 — Tendências de Recuperação & Alertas Inteligentes

- Nova leitura longitudinal de recuperação comparando os últimos 7 dias com os 7 dias anteriores.
- Combina prontidão, sono, dor, recuperação, energia, frequência de treinos e sessões com RPE 8+.
- Sinais transparentes para dor elevada, sono baixo, recuperação reduzida, carga intensa recente e queda de prontidão.
- Classificação conservadora: Dados Insuficientes, Estável, Melhorando, Observar ou Atenção.
- Home do atleta mostra tendência de recuperação sem linguagem diagnóstica.
- Prontuário profissional mostra os sinais e justificativas usados pelo motor.
- Nenhum alerta altera automaticamente a prescrição; decisão clínica permanece com o profissional.
- Sem mudança de schema; PREPARAR permanece 37/37.
- Suíte ampliada para 656 verificações.

# v0.6.6 — Execução Guiada

- Roteiro de Hoje derivado de dados reais do acompanhamento.
- Progresso diário com prontidão, treino, metas/hidratação e fechamento.
- Fechamento do dia com percepção 0–10 e reflexão breve.
- +20 XP apenas no primeiro fechamento do dia; atualização posterior não duplica XP.
- Leitura profissional da execução diária.
- Sem alteração de schema: reutiliza registros e ledger já existentes.

# v0.6.5 — Estratégia do Dia

- Adiciona `EstrategiaDiariaService`, combinando prontidão, ciclo esportivo e planos ativos.
- Cria orientação de intensidade, RPE, carga e volume com regra de segurança: nunca aumentar automaticamente acima da prescrição.
- Adiciona estratégia nutricional e hidratação contextual sem alterar quantidades prescritas.
- Home do atleta e prontuário profissional exibem a Estratégia do Dia.
- Sem mudança de schema; PREPARAR permanece 37/37.
- Suíte ampliada para 640 verificações.

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

### v0.7.5 r2 — correção da sentinela de schema
- Corrige o teste 718/720 para validar a etapa 37/37 em `scripts/setup.ps1`, que é onde o PREPARAR delega os upgrades.
- Nenhuma alteração de schema, migration ou comportamento funcional.

### v0.8.0 r3 — compatibilidade do teste de handoff
- A sentinela histórica do handoff clínico deixou de depender do literal acentuado `RESUMO CLÍNICO`, evitando falso negativo de encoding no Windows PowerShell.
- A validação continua exigindo a função `hpClinicalSummaryText` e os blocos estruturais `AGENDA` e `ACOMPANHAMENTO`.
- Sem alteração de schema ou versão semântica.

### v0.8.0 r4 — Compatibilidade do teste de inserção visual
- Corrige a sentinela `[293/600]` para validar a estrutura real da inserção rápida de refeição sem depender de literal acentuado.
- Preserva `openMealLibraryInsertForm`, endpoint de inserção, retorno à aba de alimentação e fechamento do modal.
- Nenhuma alteração de schema ou regra funcional.

### v0.8.0 r5 — compatibilidade do teste de inserção visual de sessão
- Corrige a sentinela `[305/600]` para validar a estrutura funcional da inserção de sessão de treino sem depender de literal acentuado no toast.
- Mantém a versão funcional `0.8.0`, schema e funcionalidades inalterados.

### v0.8.0 r7
- Corrigida sentinela historica do formulario de fase nutricional para validar a estrutura do formulario sem depender do literal acentuado "Manutencao".

- r9: teste [385/600] de destaques de fases passou a validar campos estruturais sem depender de strings acentuadas.

### v0.8.0 r12 — compatibilidade dos testes de progressão
- Corrige sentinelas históricas [444-446] para validar estrutura real do painel profissional, gráficos/recordes e portal do paciente.
- Remove dependência de rótulos acentuados nas validações estáticas de progressão por exercício.
- Sem alteração de schema, API funcional ou versão semântica.

- r13: sentinelas [458-460] de sinais de progressao agora validam estrutura/funcoes/campos em vez de literais acentuados.

### v0.8.0 r14 — compatibilidade de encoding dos testes
- `TESTAR.ps1` passa a ser distribuído em UTF-8 com BOM para Windows PowerShell 5.1 interpretar corretamente literais Unicode (acentos e `•`).
- Corrige a causa sistêmica dos falsos negativos em sentinelas visuais/identidade sem alterar regras de negócio, schema ou versão funcional.


### v0.8.8-r1 — Correção das sentinelas de versão
- TESTAR.ps1 alinhado à versão corrente 0.8.8 em VERSION.txt, HP_MVP_VERSION e /api/health.
- Testes históricos e schema 37/37 preservados.

### v0.8.9 r1 — correção de compilação da tendência semanal
- Corrige o cálculo de hidratação comparativa filtrando apenas registros com valor e materializando `decimal` não anulável antes do `Average`.
- Elimina o erro CS1503 em `TendenciaSemanalService` sem alterar regra de negócio, schema ou versão semântica.
- r1: alinhada a trava textual do encerramento de hábito para explicitar que o sistema **não gera nova meta automática** ao consolidar um foco.
## v0.9.8 r1 — correção da sentinela inicial de versão

- Corrige o healthcheck inicial do `TESTAR.ps1`, que ainda exigia `0.9.7` apesar da API e do pacote já estarem em `0.9.8`.
- Preserva as referências históricas de compatibilidade da v0.9.7.
- Mantém `VERSION.txt` em `0.9.8`, sem alteração de schema ou migration.

