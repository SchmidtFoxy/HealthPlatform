# HealthPlatform v0.9.6 — Proteção da Retomada & Continuidade

> v0.9.6: Encerramento do Ciclo de Hábito & Transição para Manutenção — foco consolidado sai do destaque sem abrir nova cobrança automaticamente.

A v0.9.6 protege o período logo após uma reconexão com o plano. Ela usa apenas sinais observáveis — streak curto, mudança de dias ativos, consistência e contexto de recuperação — para reconhecer uma retomada ainda frágil sem rotular recaída ou calcular probabilidade de abandono.

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

### v0.9.6 — Manutencao Sustentavel & Proximo Foco
A rotina agora diferencia habitos que ja podem ficar em manutencao de um unico eixo que merece foco. Recuperacao e retomada continuam acima de qualquer aumento de exigencia.
