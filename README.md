# HealthPlatform v0.13.2 — Foco Gamificado do Dia

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

### v0.13.4 — Mobile Daily Experience

A experiência do atleta no celular passa a seguir uma hierarquia diária: primeiro estado/foco/check-in/ações rápidas; depois progresso e análises sob demanda. O dock inferior foi simplificado para cinco destinos e a navegação secundária passa a abrir em bottom sheet.

### v0.13.3 — Mobile First Experience
O portal do atleta passa a adotar mobile-first de forma estrutural: dock inferior, safe-area, hierarquia compacta, cards adaptativos, métricas roláveis, tabelas em cards e formulários otimizados para toque. O foco é transformar o uso diário em celular na experiência principal, e não em uma versão reduzida do desktop.
