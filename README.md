# AESYN Performance v0.18.0 — Performance Foundation

A camada de produto do HealthPlatform passa a se apresentar como **AESYN Performance**: consulta médica, acompanhamento pelo app, métricas e check-ins, treino, nutrição, exames e dashboards de evolução em uma única jornada.

A arquitetura técnica permanece `HealthPlatform` nesta fase para preservar compatibilidade de solution, assemblies, APIs, banco e deploy. A v0.18.0 é uma evolução visual e de posicionamento, sem migration nova.

**Versão funcional:** `0.18.0`  
**Schema:** `38/38` — sem migration nova.

## Pilares AESYN

- Consulta médica e avaliação profissional;
- acompanhamento contínuo pelo app;
- métricas, prontidão e check-ins;
- treino e recuperação;
- integração nutricional;
- acompanhamento de exames;
- dashboards de evolução longitudinal.

## Identidade visual

`#0B3B36` verde petróleo • `#19C98B` verde esmeralda • `#F6F8F7` off-white • `#111817` grafite • `#A7B2AF` cinza frio.

## Base preservada — v0.17.9

Fecha a fase Professional Prescription Experience com revisão conjunta de treino e alimentação, publicação explícita e histórico de versões preservado.

# HealthPlatform v0.17.9 — Patient Preferences & Smart Adaptation

Esta versão conecta preferências, rotina e anamnese ao rascunho profissional. O sistema sugere adaptações explicadas para hidratação, frequência/duração do treino e organização alimentar, sempre como rascunho revisável e nunca como prescrição/publicação automática.

## Destaques

- edição completa de planos alimentares existentes usando `PUT /api/planos-alimentares/{id}`;
- busca local de alimentos por nome/categoria dentro de cada item, sem perder o rascunho;
- duplicação rápida de refeições e alimentos;
- metas diárias visíveis junto do total prescrito durante a montagem;
- contexto do `Recommendation Tuning Workspace` reaproveitado como referência, sem prescrição automática;
- novo plano pode nascer com a quantidade de refeições do rascunho profissional;
- mantém biblioteca de refeições/modelos, progressão e análise nutricional já existentes.

## Segurança de produto

O builder organiza e acelera a prescrição, mas não publica recomendações automaticamente nem substitui a decisão profissional.


## v0.17.7 — Diet & Meal Library
- Biblioteca profissional central de planos alimentares e refeições.
- Busca e filtro por modelos ativos/inativos.
- Edição de nome, descrição/categoria e status dos modelos.
- Atribuição de dieta cria cópia independente para o paciente.
- Refeições modelo podem ser inseridas em planos alimentares ativos.
- Integração pelo Workspace de Prescrições e pela aba Alimentação do paciente.
- Sem nova migration; schema permanece 38/38.


> A revisão r1 da v0.17.9 permanece registrada apenas como histórico; a versão funcional atual é 0.18.0.
