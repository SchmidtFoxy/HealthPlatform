# HealthPlatform v0.17.3 — Recommendation Tuning Workspace

A fase profissional agora permite transformar a sugestão inicial em um **rascunho realmente ajustável** antes de entrar nos builders. O profissional compara sugerido × ajustado e modifica hidratação, sessões de força, duração, refeições, condicionamento, direção nutricional e preferências em um único workspace.

- Atalhos conservadores: **Simplificar rotina**, **Priorizar recuperação** e **Restaurar sugerido**.
- Alterações permanecem como rascunho; nenhuma publicação automática.
- Ajustes ficam disponíveis em memória do workspace para continuidade nos builders seguintes.
- Schema preservado em **38/38**; nenhuma migration nova.
- Smoke test: **1496 verificações**.
- Compatível com Windows PowerShell 5.1 (`TESTAR.ps1` em UTF-8 com BOM).

---

# HealthPlatform v0.17.2 — Treatment Goal & Initial Recommendation

A fase profissional conecta avaliação corporal a objetivo terapêutico e gera um rascunho inicial editável de hidratação, frequência de treino e direção nutricional. A sugestão é uma heurística de protótipo para revisão profissional e nunca é publicada automaticamente.

# HealthPlatform — v0.17.1 Seasonal Progress

> Versão funcional atual: **v0.16.5 — Seasonal Progress**.

A Gamificação 3.0 fecha sua primeira sequência com uma leitura de **progresso sazonal** baseada no ciclo esportivo já existente. O atleta passa a enxergar semana atual, percentual temporal do ciclo, treinos acumulados, prontidão média, consistência e objetivo do ciclo dentro de uma narrativa simples de Fundação → Construção → Consolidação → Fechamento.

A leitura não cria meta paralela nem premia picos isolados de intensidade. Semanas de recuperação/proteção continuam fazendo parte do progresso do ciclo, reforçando que continuidade e resposta ao corpo valem mais do que “forçar” a temporada.

- Schema: **38/38**.
- Migration nova: **nenhuma**.
- Smoke test: **1464 verificações**.
- Compatível com Windows PowerShell 5.1 (`TESTAR.ps1` em UTF-8 com BOM).

---

- Compatível com Windows PowerShell 5.1 (`TESTAR.ps1` em UTF-8 com BOM).

---

# HealthPlatform v0.15.4 — End-of-Day Flow

## v0.15.6 — Weekly Review

A experiência do atleta agora fecha também o ciclo semanal. A Home reúne treino/meta, consistência, missões e carga recente em uma síntese curta, interpreta o estado da semana e sugere um foco contextual para a próxima semana sem substituir o plano profissional. O botão **Revisar semana completa** abre as análises semanais já existentes (planejamento, resumo e tendência), evitando duplicação de dados ou regras. Esta versão não altera o schema (38/38) nem adiciona migration.


> Versão funcional atual: **v0.15.4 — End-of-Day Flow**.

## v0.15.4 — End-of-Day Flow

- fecha a experiência diária do atleta em uma leitura única de corpo, sessão, hidratação e roteiro;
- reutiliza `execucaoDoDia`, `prontidaoDiaria`, `respostaSessao` e `hidratacaoContextual`;
- usa o endpoint de fechamento já existente, sem criar backend ou estado paralelo;
- deixa explícito que encerrar o dia não exige completar pendências nem compensar carga;
- mantém o fechamento editável para correção da percepção final;
- schema preservado em 38/38.

---

# HealthPlatform v0.15.3 — Recovery Day Flow

> Versão funcional atual: **v0.15.3 — Recovery Day Flow**.

## v0.15.3 — Recovery Day Flow

- Organiza dias de recuperação em **Contexto → Recuperar → Reavaliar**.
- Ativa o fluxo somente quando a prontidão/intensidade contextual indica `Recuperacao` ou `Leve`.
- Reutiliza `prontidaoDiaria`, `planoRecuperacao` e `execucaoDoDia`; não cria endpoint ou estado paralelo.
- Permite registrar rapidamente como o atleta está pelo Quick Log, sem pressão para compensar carga.
- Mantém o Training Day Flow disponível como referência da prescrição profissional.


O treino do atleta agora funciona como uma jornada contínua: o sistema usa o Morning Check-in e a prontidão como contexto, mostra a sessão prescrita, permite iniciar a execução e fecha o ciclo com duração, RPE e revisão. O fluxo não cria prescrição automática: a prontidão contextualiza a decisão e a ficha profissional continua sendo a referência.

- Schema: **38/38**.
- Migration nova: **nenhuma**.
- Smoke test: **1384 verificações**.
- Compatível com Windows PowerShell 5.1 (`TESTAR.ps1` em UTF-8 com BOM).

## v0.15.0 — Mobile Accessibility & Polish

Fecha a série mobile 0.14.x com uma camada transversal de acessibilidade e acabamento: skip link, foco visível consistente, `aria-current` na navegação, região viva para anunciar troca de seção, alvos mínimos de toque, suporte a `prefers-contrast` e reforço de `prefers-reduced-motion`. A entrega não altera schema, endpoints ou regras clínico-esportivas.


## v0.14.8 — Athlete Profile Mobile

O portal do atleta ganhou uma area **Meu perfil** otimizada para celular. Ela reune identidade, nivel/XP, consistencia, streak, prontidao media do ciclo, objetivo e progresso do ciclo esportivo e o ultimo contexto corporal. A entrega reutiliza os dados ja existentes do portal e nao altera schema ou regras clinico-esportivas.

# HealthPlatform v0.14.8 — Mobile Modals & Bottom Sheets

A v0.14.7 continua a consolidação mobile: modais e ações rápidas passam a se comportar como bottom sheets consistentes, com backdrop clicável, foco acessível, navegação por teclado, bloqueio de scroll do fundo e respeito à safe-area. Não altera schema nem regras esportivas.

# HealthPlatform v0.14.6 — Mobile Charts & Progress

> Versão funcional atual: **v0.14.6 — Mobile Charts & Progress**.


## v0.14.6 — Mobile Charts & Progress

- Reorganiza gráficos de evolução do atleta para leitura mobile-first em uma coluna.
- Cada gráfico passa a mostrar resumo compacto de mínimo, atual e máximo antes da visualização.
- Tendência e intervalo temporal ficam explícitos sem depender apenas da leitura visual da linha.
- Pontos do SVG ganham alvo visual maior no celular e os gráficos preservam rolagem vertical por toque.
- Adiciona descrição textual acessível com quantidade de registros, valores inicial/atual, mínimo/máximo e direção da tendência.
- Reduz densidade visual em telas estreitas, ocultando rodapé redundante quando o resumo mobile já está visível.
- Preserva os mesmos dados, endpoints e regras esportivas; sem migration nova, schema 38/38.
- Smoke test ampliado para 1336 verificações.


## v0.14.5 — Mobile Feedback States

- Consolida feedbacks de carregamento, erro, vazio, sucesso e submissão no portal mobile do atleta.
- Erros de carregamento deixam de manter a tela presa no spinner e passam a oferecer `Tentar novamente`.
- Registros rápidos do atleta mostram estado `Salvando...`, `aria-busy` e bloqueio contra envio duplicado.
- Empty states do portal recebem superfície, semântica `status` e leitura mobile consistente.
- Loading states passam a anunciar progresso com `role=status` / `aria-live=polite`.
- Preserva regras esportivas, endpoints e persistência; sem migration nova, schema 38/38.
- Smoke test ampliado para 1328 verificações.

## v0.14.4 — Mobile Forms & Inputs

- Converte formulários do portal do atleta para fluxo mobile de uma coluna, evitando campos comprimidos.
- Padroniza inputs, selects e textareas com alvos de toque de 50–52 px, foco visível e estados disabled/erro.
- Amplia checkbox, radio e range para interação por toque.
- Mantém ações de salvar/cancelar acessíveis em sheets/modais e respeita safe-area.
- Aplica `inputmode` contextual a campos numéricos, email, telefone e URL, inclusive em formulários renderizados dinamicamente.
- Ao focar um campo no celular, reposiciona o controle para reduzir sobreposição pelo teclado virtual.
- Nenhuma migration nova; schema permanece 38/38.
- Smoke test ampliado para 1320 verificações.

# HealthPlatform v0.14.3 — Mobile Data Views

> Versão funcional atual: **v0.14.3 — Mobile Data Views**.

## v0.14.3 — Mobile Data Views

- Reestrutura tabelas, históricos e listas densas do portal do atleta para leitura mobile-first.
- Linhas de tabela passam a se comportar como cartões compactos em telas pequenas, evitando desktop comprimido.
- Padroniza históricos de diário, follow-up, check-ins, metas, medidas e exames.
- Remove dependência de scroll horizontal nas principais visualizações do atleta e melhora leitura em até 390 px.
- Nenhuma migration nova; schema permanece 38/38.
- Smoke test ampliado para 1312 verificações.

## Revisao v0.14.3-r4 — Rich Athlete Demo Seed Audit

A baseline funcional continua em **v0.14.3 — Mobile Data Views**. A revisao r4 fecha o tooling de avaliacao com um atleta ficticio rico em historico: corrige a janela retroativa das metas, leva os treinos ate a semana atual e imprime auditoria/diagnostico de carga e performance ao final.

Validacao de sintaxe antes do seed:

```powershell
.\VALIDAR-SEED-DEMO-RICO.ps1
```

Execucao local (API em `http://localhost:5180`):

```powershell
.\POPULAR-LUCATTI-DEMO-RICO.ps1 -SenhaAdmin 'SUA_SENHA_ADMIN'
```

Execucao contra Render ou outra API:

```powershell
.\POPULAR-LUCATTI-DEMO-RICO.ps1 -BaseUrl 'https://SEU-SERVICO.onrender.com' -SenhaAdmin 'SUA_SENHA_ADMIN'
```

O cenario e ficticio e serve exclusivamente para validacao funcional/visual.


# HealthPlatform v0.14.2 — Mobile Content Hierarchy

> Versão funcional atual: **v0.14.2 — Mobile Content Hierarchy**.

## v0.14.2 — Mobile Content Hierarchy

- Padroniza hierarquia de títulos, subtítulos e eyebrows no portal mobile.
- Unifica espaçamento vertical e padding de cards para reduzir sensação de conteúdo empilhado.
- Normaliza labels, inputs, selects e textareas com contraste e dimensões consistentes.
- Melhora ações de formulário e leitura em telas de até 390 px.
- Nenhuma migration nova; schema permanece 38/38.
- Smoke test ampliado para 1304 verificações.

## v0.14.2 — Mobile Navigation Shell

Segunda etapa da série 0.14.x de refinamento mobile. Aplica a fundação visual ao shell permanente do portal do atleta: app bar compacta, dock inferior com estado ativo coerente, ação Mais integrada, safe-area lateral/superior/inferior e controles globais preparados para toque. Não altera regras clínicas, endpoints ou schema.

# HealthPlatform v0.14.0 — Mobile UI Foundation

> Versão funcional atual: **v0.14.0 — Mobile UI Foundation**.

## v0.14.0 — Mobile UI Foundation

Marco de entrada da série 0.14.x, dedicada ao refinamento do front mobile. Esta versão centraliza tokens visuais do portal do atleta e padroniza canvas, superfícies, tipografia, cards, formulários, foco acessível, alvos de toque, safe-area e viewport dinâmico. Não altera regras clínicas, endpoints ou schema.

# HealthPlatform v0.13.23 — Mobile Session Review

> Versão funcional atual: **v0.13.23 — Mobile Session Review**.

## v0.13.23 — Mobile Session Review

- histórico recente de treino do atleta passa a ser interativo no celular;
- toque em uma sessão abre revisão compacta com duração, RPE, exercícios executados e observação;
- detalhes são lidos do histórico já existente, sem endpoint ou persistência paralela;
- resumo deixa explícito que o registro não altera a prescrição profissional;
- ação opcional abre o Quick Log para registrar como o atleta está após revisar a sessão;
- bottom sheet respeita safe-area e telas estreitas;
- sem migration nova; schema permanece 38/38;
- smoke test ampliado para 1280 verificações.

# HealthPlatform v0.13.22 — Quick Log Hub

> Versão funcional atual: **v0.13.22 — Quick Log Hub**.

## v0.13.22 — Quick Log Hub

- adiciona um botão flutuante de registro rápido disponível em qualquer área do portal do atleta no celular;
- reúne Água, Energia, Dor e Sono em uma bottom sheet para uso com uma mão;
- reaproveita `openQuickPatientRecord` e a persistência já existente, sem fluxo paralelo;
- respeita safe-area, telas estreitas e `prefers-reduced-motion`;
- sem migration nova; schema permanece 38/38;
- smoke test ampliado para 1272 verificações.


## v0.13.21 — Today Brief

- Home mobile consolida prontidão, treino e hidratação em uma única leitura rápida.
- Ações diretas reutilizam check-in, área de treino e registro de água existentes.
- Resumos antigos permanecem no DOM por compatibilidade, mas deixam de competir visualmente no celular.
- Estados pendente, pronto e recuperação usam apenas dados já calculados.
- Nenhuma regra clínica, score, prescrição ou migration nova; schema permanece 38/38.
- Smoke test ampliado para 1264 verificações.

# HealthPlatform v0.13.20 — Hydration Pace

> Versão funcional atual: **v0.13.20 — Hydration Pace**.

## v0.13.20 — Hydration Pace

- adiciona leitura compacta de ritmo de hidratação na Home mobile;
- mostra água registrada, restante da meta e progresso percentual usando `hidratacaoContextual`;
- ação `+` abre o registro rápido de água já existente;
- meta continua vindo exclusivamente do plano profissional, sem aumento automático por treino/RPE;
- estado de meta concluída não incentiva consumo extra;
- sem migration nova; schema permanece 38/38.

# HealthPlatform v0.13.20 — Daily Closure

> Versão funcional atual: **v0.13.20 — Daily Closure**.

## v0.13.20 — Daily Closure

- traz o fechamento do dia para a hierarquia principal da Home mobile;
- reutiliza `execucaoDoDia`, progresso, concluídos, pendentes e fechamento já persistido;
- reforça “execute o plano, não persiga perfeição”, sem transformar 100% em obrigação;
- permite fechar ou revisar o dia sem abrir o bloco de análises avançadas;
- não cria score, regra clínica, XP, prescrição ou migration; schema permanece 38/38.

## v0.13.17 — Consistency Compass

- adiciona uma bússola compacta de consistência na Home mobile do atleta;
- reutiliza score de consistência, streak, dias ativos e missões já existentes;
- diferencia rotina sustentada, consistência em construção e retomada simples;
- em recuperação/revisão, reforça que respeitar o plano também é consistência e não exige treino para preservar streak;
- abre Progresso e missões diretamente, sem criar score clínico, XP novo ou prescrição;
- não altera schema, mantendo 38/38.

## v0.13.16 — Body Context Brief

- adiciona uma leitura rápida antes do trilho de insights no portal do atleta;
- sintetiza semana, carga e recuperação usando somente estados já calculados;
- diferencia contexto estável, observação e revisão sem criar score ou prescrição;
- mantém a leitura curta e mobile-first, com detalhes preservados no Insight Rail;
- não altera schema, mantendo 38/38.



- entrega uma única prioridade diária derivada da Gamificação 2.0 e das Missões Contextuais 2.0;
- reduz ruído no mobile sem transformar gamificação em prescrição;
- protege dias de recuperação, revisão e retorno gradual;
- não inventa treino, não cria XP automático e não exige compensação para manter streak;
- mantém schema 38/38.

## v0.13.1 — Missões Contextuais 2.0

- contextualiza as missões semanais com prontidão, recuperação planejada, resposta à sessão e retorno gradual;
- missões incompletas podem ficar `SemPressaoHoje` quando recuperação/revisão deve prevalecer;
- preserva progresso e recompensa já registrados, sem apagar desafio, XP ou streak;
- proíbe compensação de sessão, urgência artificial para fechar a semana e aumento de carga para buscar recompensa;
- integra a mesma leitura nas Homes do atleta e do profissional;
- preserva schema 38/38, sem migration nova.

## v0.13.0 — Gamificacao 2.0

- inaugura uma camada de gamificacao orientada a adequacao, consistencia, recuperacao planejada e retorno gradual;
- explicita quando XP recente veio de comportamento alinhado ou de excesso;
- descanso planejado e adaptacao coerente contam como progresso contextual;
- XP/streak nao viram obrigacao de treinar nem justificativa para aumentar carga;
- nao cria XP negativo, score de sofrimento ou recompensa por intensidade bruta;
- integra a leitura nas Homes do atleta e do profissional;
- preserva schema 38/38, sem migration nova.

## v0.12.5 — Relatório Esportivo Profissional

A v0.12.5 consolida os principais eixos da medicina do esporte em um relatório longitudinal rastreável para revisão profissional. O relatório organiza ciclo/bloco, carga e recuperação, dor, readiness, sessão planejada versus executada, resposta pós-sessão, progressão supervisionada, retorno gradual e suporte diário, preservando as evidências de origem.

O relatório não cria score geral do atleta, não diagnostica, não estima risco de lesão, não prova causalidade e não prescreve automaticamente. Pontos de atenção têm prioridade clínica-esportiva sobre sinais isolados de performance favorável.

## v0.12.4 — Retorno Gradual após Pausa/Dor

A plataforma diferencia períodos deliberados de redução de carga, configurados pelo profissional no bloco/mesociclo, de quedas involuntárias de adesão. A leitura é contextual e não prescritiva.

# HealthPlatform v0.12.1 — Blocos de Treinamento / Mesociclos

A v0.12.1 organiza a FaseTreino já configurada pelo profissional como bloco/mesociclo contextual do ciclo esportivo. Mostra período, semana, objetivo, execução real, RPE/carga observada e critérios de transição sem criar periodização ou progressão automática.

## v0.12.1 — Reavaliação da Progressão & Decisão de Continuidade
Converte o monitoramento de resposta em uma decisão supervisionada de manter, revisar, encerrar a observação atual ou aguardar mais dados, sem inferir causalidade nem iniciar nova progressão automaticamente.

## v0.10.1 — Monitoramento de Resposta à Progressão
Acompanha recuperação, carga e performance ao redor do plano supervisionado sem inferir causalidade nem autorizar nova progressão automática.

# HealthPlatform v0.10.1 — Plano de Progressão Supervisionada

> Marco v0.10.1: depois de abrir a janela e organizar a decisão de progressão, a plataforma agora estrutura a discussão profissional sem transformar elegibilidade em prescrição automática.

A camada de progressão supervisionada mostra um único eixo em discussão, critérios transparentes e uma regra de reavaliação baseada na resposta observada. O sistema não define carga, volume, calorias, medicação ou meta automaticamente.

## Princípios
- uma mudança por vez;
- outros eixos permanecem estáveis durante observação;
- janela/decisão anteriores continuam soberanas;
- falta de dados, recuperação ou carga em revisão bloqueiam nova exigência;
- reavaliação ocorre após observar resposta, sem prazo automático imposto.

# HealthPlatform v0.9.9 — Janela de Progressão & Critério de Avanço

> v0.9.9: a plataforma agora explicita quando existe contexto suficiente para discutir uma progressão e quando recuperação, carga, oscilação ou falta de dados pedem manutenção.

A v0.9.9 continua a linha de medicina do esporte e autocuidado diário: avanço não é recompensa automática por aderir bem. A janela usa critérios separados e visíveis, sem score opaco, e nunca aumenta carga, volume, meta nutricional ou medicação por conta própria.


## Princípios
- retomada em curso é inferida por sinais transparentes, não por score oculto;
- no máximo 3 sinais/prioridades de proteção;
- redução de ritmo coerente com recuperação não vira recaída;
- não remove XP, não pune streak e não usa compensação;
- nenhuma alteração automática de treino, nutrição ou medicação;
- sem migration nova; `PREPARAR` permanece 37/37;
- suíte: 856 verificações.

# HealthPlatform v0.9.1 — Plano de Reconexão & Retomada Sustentável

A v0.9.1 transforma sinais do Radar de Adesão em até três passos pequenos de retomada, sem compensação, punição ou prescrição paralela. Recuperação adequada continua acima da pressão por metas.

## Princípios
- no máximo 3 passos de reconexão;
- usa sinais já existentes do radar, roteiro do dia e prioridades do ciclo;
- descanso coerente com recuperação permanece protegido;
- sem XP negativo, punição de streak ou compensação;
- não dobra treino, não restringe alimentação e não altera prescrição automaticamente;
- sem migration nova; `PREPARAR` permanece 37/37;
- suíte: 848 verificações.

# HealthPlatform v0.9.0 — Radar de Adesão & Continuidade do Plano

A v0.9.0 inaugura a linha 0.9.x com um radar de continuidade do plano. Ele combina consistência, ritmo semanal, nutrição e hidratação para reconhecer oscilações de adesão sem rotular o paciente e sem confundir recuperação planejada com abandono.

## Princípios
- sem score/probabilidade de abandono;
- descanso coerente com recuperação não vira falha;
- sem XP negativo ou punição;
- prioridades pequenas e sustentáveis para reconectar com o plano;
- nenhuma alteração automática de treino, nutrição ou medicação.

## Estrutura
- `RadarAdesaoService`;
- contratos `PortalRadarAdesao*`;
- cards para atleta e profissional;
- sem migration nova; `PREPARAR` permanece 37/37;
- suíte: 840 verificações.

# HealthPlatform v0.8.9 — Tendência Semanal & Comparativo de Semanas

A v0.8.9 acrescenta comparação longitudinal justa entre a semana atual e o mesmo intervalo da semana anterior, mantendo cada eixo separado e auditável.

## v0.8.8 — Resumo Semanal & Fechamento da Semana

A síntese semanal reúne treino, recuperação, carga, nutrição, hidratação e execução em eixos separados, sem score clínico único.

# HealthPlatform v0.8.8 — Ações Prioritárias do Ciclo

A v0.8.8 transforma os sinais já calculados pelo HealthPlatform em até três prioridades semanais claras, colocando recuperação e equilíbrio de carga acima de metas de volume/performance quando necessário. A camada organiza; não cria prescrição nova.

## Destaques

- até 3 prioridades por semana, com ordem, nível, motivo e ação;
- recuperação e carga têm precedência explícita quando há sinais de atenção;
- usa tendência por objetivo, checkpoint, recuperação, carga, nutrição e hidratação;
- fallback saudável: executar o plano com consistência quando não há sinal relevante;
- mesma leitura para atleta e profissional;
- sem score opaco, diagnóstico ou alteração automática de treino/nutrição/medicação;
- nenhuma migration nova: schema continua 37/37;
- suíte ampliada para 808 verificações.

---


# HealthPlatform v0.8.2 — Revisão de Ciclo & Checkpoint de Progresso

A v0.8.2 transforma o ciclo esportivo em uma revisão longitudinal transparente. O checkpoint cruza avanço temporal, metas mensuráveis e o Painel de Evolução Esportiva sem produzir score clínico opaco e sem alterar a prescrição automaticamente.

## Destaques
- Checkpoint do ciclo para atleta e profissional.
- Estado EmCurso, Evoluindo, Revisar ou Consolidar.
- Semana atual, total de semanas e progresso temporal.
- Comparação explícita entre progresso médio das metas e tempo transcorrido.
- Evidências multidimensionais vindas do Painel de Evolução.
- Trava: não diagnostica, não prescreve e não altera treino/alimentação/medicação.
- Sem migration nova; schema permanece 37/37.
- Suíte ampliada para 776 verificações.


## v0.8.8 — Ações Prioritárias do Ciclo
A Home ordena até três prioridades semanais usando sinais já existentes, colocando recuperação e carga acima de metas de volume/performance quando necessário. A camada é explicativa e não altera prescrição automaticamente.

### v0.9.7 — Manutencao Sustentavel & Proximo Foco
A rotina agora diferencia habitos que ja podem ficar em manutencao de um unico eixo que merece foco. Recuperacao e retomada continuam acima de qualquer aumento de exigencia.

### v0.13.5 — Mobile Daily Experience

A experiência do atleta no celular passa a seguir uma hierarquia diária: primeiro estado/foco/check-in/ações rápidas; depois progresso e análises sob demanda. O dock inferior foi simplificado para cinco destinos e a navegação secundária passa a abrir em bottom sheet.

### v0.13.3 — Mobile First Experience
O portal do atleta passa a adotar mobile-first de forma estrutural: dock inferior, safe-area, hierarquia compacta, cards adaptativos, métricas roláveis, tabelas em cards e formulários otimizados para toque. O foco é transformar o uso diário em celular na experiência principal, e não em uma versão reduzida do desktop.

### v0.13.5 — Empty States & Mobile Polish

A experiência mobile agora trata estados vazios como parte real do produto. Cabeçalhos de página ganham contraste consistente no celular e telas sem dados — começando pelo plano alimentar — passam a explicar o estado, orientar o usuário e oferecer uma ação útil, sem parecerem quebradas ou inacabadas.



### v0.13.6 — Mobile Interaction Experience
O portal do atleta passa a tratar registros e check-ins como interações nativas de celular: bottom sheets, ações fixas, safe-area e controles 0–10 por slider. A versão mantém schema 38/38 e não altera regras clínicas.


### v0.13.8 — Mobile Feedback Experience
A experiência mobile do atleta agora comunica melhor o que está acontecendo: carregamentos são contextuais, sucesso/erro usam feedback padronizado, a troca entre áreas tem transição curta e os avisos ficam acima do dock/safe-area. Nenhuma migration foi adicionada; o schema permanece 38/38.


## v0.13.8 — Mobile Home Hierarchy
A Home mobile prioriza leitura em poucos segundos: prontidão, intensidade do treino, hidratação e streak aparecem em um resumo compacto antes das análises detalhadas.


## v0.13.9 — Mobile Action Hub

A Home do atleta passa a destacar a próxima ação útil em um bloco **Agora**. O objetivo é reduzir procura por funções: check-in quando a prontidão ainda não foi registrada, treino quando o contexto do dia já está disponível, além de atalhos rápidos para hidratação e outros registros. A versão mantém o schema 38/38 e não altera regras clínicas.


## v0.13.10 — Post-Workout Mobile Loop

No portal do atleta, concluir um treino agora abre um resumo pós-sessão mobile com duração, RPE e próximas ações rápidas. O fluxo não cria recomendação clínica nova: apenas confirma o que foi registrado e facilita registrar hidratação ou retornar à tela Hoje.

## v0.13.11 — Recovery Pulse

A Home aproxima a resposta à última sessão da rotina diária, apresentando prontidão, recuperação e dor em um card compacto, sem inferir causalidade nem liberar progressão automaticamente.



## v0.13.15 — Training Load Snapshot

- nova leitura compacta de carga recente na Home mobile;
- compara carga interna dos últimos 7 dias com a mediana pessoal já calculada;
- reaproveita estado, posição histórica e leitura da carga individualizada, sem criar score novo;
- diferencia visualmente contexto estável, observação e revisão;
- acesso direto à análise completa de carga;
- nenhuma migration nova; schema permanece 38/38.

## v0.13.13 — Weekly Athlete Rhythm

- adiciona um resumo compacto da semana na Home mobile do atleta;
- reutiliza `planejamentoSemanal` e `resumoSemanal`, sem criar score ou prescrição paralela;
- mostra treinos concluídos/meta e sessões restantes;
- diferencia semana em andamento, consolidada e contexto de proteção/recuperação;
- permite abrir o contexto semanal detalhado já existente;
- mantém schema **38/38**, sem migration nova.

## v0.13.12 — Daily Athlete Timeline

A Home mobile passa a organizar o dia esportivo em **Check-in → Treino → Recuperação**. A etapa atual é destacada a partir de dados já existentes e cada estágio funciona como atalho para a ação correspondente. Não há novo score, prescrição, persistência ou migration; o schema permanece 38/38.

## v0.13.15 — Mobile Insight Rail

- reorganiza os insights esportivos secundários da Home em um trilho horizontal no celular;
- Weekly Athlete Rhythm, Training Load Snapshot e Recovery Pulse continuam disponíveis sem dominar o scroll vertical;
- preserva a prioridade visual de Hoje, AGORA, Daily Athlete Timeline e prontidão;
- mantém os cards completos no desktop e não altera regra clínica, persistência ou schema;
- adiciona scroll-snap, alvos confortáveis e tratamento específico para telas estreitas.



### v0.13.20 — Adaptive Mobile Home
No portal do atleta, a Home mobile agora muda sua hierarquia de acordo com o estágio real do dia: antes do check-in, após o check-in, depois do treino e após o fechamento. A mudança é de UX e reaproveita dados esportivos já existentes, sem prescrição automática ou alteração de schema.


> Render: a revisao v0.14.3-r1 adiciona compatibilidade idempotente para bancos demo persistidos de versoes antigas, preservando os dados existentes.

### Revisao v0.14.3-r3 - seed rico
Se uma tentativa anterior do `POPULAR-LUCATTI-DEMO-RICO.ps1` criou as metas mas falhou durante o treino, nao e necessario limpar o banco. A revisao r3 corrige o periodo das metas existentes e retoma a populacao de forma idempotente.


## v0.14.4-r1 — PowerShell 5.1 Test Runner Compatibility
- `TESTAR.ps1` regravado em UTF-8 com BOM para compatibilidade com Windows PowerShell 5.1.
- Corrige ParserError causado por caracteres Unicode presentes nos tokens de validacao (ex.: seta `→`).
- Nenhuma alteracao funcional, de API, banco ou schema. `VERSION.txt` permanece `0.14.4`.

> Hotfix v0.14.5-r1: o `TESTAR.ps1` foi ajustado para reconhecer `0.14.5` como versao corrente no healthcheck e nas validacoes equivalentes, preservando compatibilidade com Windows PowerShell 5.1.

### v0.14.7-r1 — Smoke Test MVP Preview Fix
Corrige as verificações do TESTAR.ps1 que ainda esperavam o rótulo visual v0.14.6 no MVP Preview. A aplicação permanece funcionalmente em v0.14.7; nenhuma regra, endpoint, migration ou schema foi alterado.


### Hotfix v0.14.9-r1
O `TESTAR.ps1` teve o quoting do token `[aria-current="page"]` corrigido para sintaxe nativa do PowerShell 5.1. Nenhuma funcionalidade, endpoint, migration ou regra esportiva foi alterada.


## v0.15.0 — Athlete Home 2.0
A Home do atleta passa a reunir quatro perguntas operacionais em uma única camada: como estou, o que fazer agora, como vai o dia e como estou evoluindo. A implementação reutiliza prontidão, estratégia, hidratação, execução, gamificação e ciclo esportivo já existentes, sem migration nova.


### v0.15.0-r1 — Smoke Test Current Version Fix
- Corrige o healthcheck inicial do `TESTAR.ps1` para aceitar a versão corrente `0.15.0`.
- Atualiza a validação de identidade do `MVP Preview` para `v0.15.0`, preservando todos os marcadores históricos da linha `0.14.x`.
- Nenhuma alteração funcional, de schema ou de migration.


## v0.15.6 — Athlete Progress Story
A Home do atleta passa a resumir a evolução longitudinal em uma narrativa humana, usando os indicadores já calculados pela Evolução Esportiva. Não há novo schema nem prescrição automática.

> Hotfix r1: corrigido o smoke test legado de identidade do MVP Preview para reconhecer a versão corrente v0.16.1.


## v0.16.2 — Dynamic Missions
A camada de missões agora reorganiza as missões contextuais já existentes conforme prontidão, estratégia do dia e recuperação. O objetivo é mostrar primeiro a ação mais útil para o momento, sem premiar intensidade desnecessária e sem substituir o plano profissional. Schema preservado em 38/38.


## v0.16.3 — Achievement Families
Conquistas recentes passam a ser organizadas por famílias de comportamento (Consistência, Recuperação, Execução, Evolução e Hábitos), usando a gamificação existente sem alterar schema ou criar badges paralelos.
> Revisao v0.16.3-r1: corrige apenas referencias de versao corrente no smoke test para `0.16.3`; sem mudanca funcional ou de schema.


## v0.16.4 — Weekly Challenges

- adiciona desafio semanal contextual sobre `gamificacao.desafiosSemana`;
- resume progresso semanal sem criar uma segunda fonte de missões;
- protege semanas de recuperação e reconhece adesão útil;
- não exige compensação de treino perdido nem recompensa intensidade extra;
- mantém schema 38/38 e não adiciona migration.

## v0.16.5-r1 - MVP Preview smoke test fix
- Corrige a validacao do badge MVP Preview para esperar v0.16.5.
- Nenhuma alteracao funcional, de schema ou de dominio.

## v0.17.0 — Professional Prescription Workspace

A fase profissional passa a tratar prescrição como um fluxo contínuo: avaliação/contexto → objetivo → treino/alimentação → revisão e publicação. O workspace reutiliza as bibliotecas e APIs já existentes; sugestões automatizadas ainda não são publicadas automaticamente e serão introduzidas em etapas posteriores sempre como apoio à decisão do profissional.

O onboarding planejado separa cadastro essencial de uma avaliação esportiva/metabólica retomável, permitindo registrar bioimpedância/composição corporal, rotina, preferências, disponibilidade e objetivo antes de gerar uma proposta editável de hidratação, treino e alimentação.


## v0.17.1 — Patient Intake & Body Assessment

- Cadastro essencial permanece rápido e independente da bioimpedância.
- Novo onboarding profissional retomável para avaliação inicial.
- Composição corporal usa o endpoint existente de Avaliações: peso, altura, gordura, massa magra/gorda, cintura, abdômen, quadril, pressão e frequência cardíaca.
- Bioimpedância/composição corporal é opcional: o paciente pode ser criado mesmo sem esses dados.
- Após criar um paciente, o profissional pode completar a avaliação imediatamente ou fazer depois pelo prontuário.
- Próxima etapa planejada: objetivo do tratamento + recomendação inicial assistida para revisão profissional.


### v0.17.1-r1 — Smoke Test Intake Label Fix
- Corrige falso negativo do smoke test herdado da v0.17.0: o fluxo profissional agora valida o rótulo atual “Avaliação inicial” em vez do antigo “Avaliação e contexto”.
- Nenhuma alteração funcional, de API, schema ou migration.

### Hotfix v0.17.1-r2
Corrige apenas a validacao do acesso ao Patient Intake no `TESTAR.ps1` para Windows PowerShell 5.1. A implementacao do prontuario ja estava correta; o teste interpretava `$(` como subexpressao em vez de texto literal.


> Hotfix r1: corrige somente a validação do rótulo do passo de objetivo/sugestão no smoke test.
