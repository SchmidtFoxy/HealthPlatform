# HealthPlatform v0.7.5 — Seed Ana v7 & Proteção de Variáveis PowerShell

> **v0.7.5:** corrige o populador pesado da Ana Ribeiro para PowerShell nativo. O literal decimal de C# `2.5m` foi removido e substituído por cast explícito `[decimal]2.5`, evitando que o PowerShell tente executar `2.5m` como comando. A versão também adiciona validações de regressão para literais numéricos incompatíveis em scripts `.ps1`.

## Destaques da v0.7.5

- `POPULAR-ANA-RIBEIRO.ps1` atualizado para **v7**.
- Corrigido o deload do histórico de treino: `[decimal]2.5` em vez de `2.5m`.
- Revisão do seed para impedir sufixos numéricos de C# em PowerShell.
- Diagnóstico resiliente e seed de 56 dias preservados.
- Sem alteração de schema; `PREPARAR` continua 37/37.
- Suíte ampliada para 712 verificações.


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
