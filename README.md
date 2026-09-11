# HealthPlatform v0.7.2 — Seed Resiliente & Diagnóstico de Dados

> **v0.7.2:** consolida a linha 0.7.x e endurece o seed-vitrine da Ana Ribeiro. Requisições 4xx/5xx agora informam método, endpoint e corpo devolvido pela API; metas demonstrativas são tratadas como enriquecimento opcional, para que uma falha isolada não impeça prontidão, treinos, diário, avaliações, performance e Coach de serem populados.

## Destaques da v0.7.2

- `POPULAR-ANA-RIBEIRO.ps1` v4;
- diagnóstico detalhado de erros HTTP do seed;
- falhas não fatais são acumuladas e resumidas no fim;
- criação/histórico de metas não derruba todo o cenário demonstrativo;
- mantém 56 dias de histórico esportivo, carga, PRs, prontidão e avaliações;
- Coach Diário e toda a linha v0.7.1 preservados;
- nenhuma alteração de schema: `PREPARAR` permanece 37/37;
- suíte ampliada para 696 verificações.


## Princípios

- não diagnostica;
- não cria prescrição paralela;
- não aumenta carga ou volume automaticamente;
- explica quais dados motivaram cada prioridade;
- atleta e profissional enxergam a mesma base de decisão em linguagens adequadas;
- nenhuma migration nova: `PREPARAR` permanece 37/37.

## Validação

A suíte local possui 680 verificações, preservando todas as camadas anteriores.

Execute:

```powershell
.\PREPARAR.ps1
.\RODAR.ps1
.\TESTAR.ps1
```
