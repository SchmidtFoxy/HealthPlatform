# HealthPlatform v0.17.6 — Nutrition Builder 2.0

**Versão funcional:** `0.17.6`  
**Schema:** `38/38` — sem migration nova.

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
