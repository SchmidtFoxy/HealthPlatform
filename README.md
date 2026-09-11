# v0.5.5 — Solicitações no Hoje / Connected Care

A v0.5.4 integra Solicitações Clínicas ao fluxo diário, reduzindo a dependência de telas isoladas.

## O que entrou

- O paciente passa a ver solicitações pendentes diretamente na tela **Hoje**.
- Solicitações pendentes entram no progresso diário do paciente.
- Solicitações vencidas ganham destaque e ação direta **Responder**.
- A Central do Dia profissional passa a mostrar respostas aguardando revisão e solicitações vencidas.
- O dashboard profissional passa a destacar o volume de solicitações para revisão.
- Atalhos levam diretamente para a Central de Solicitações.
- Reutiliza o schema de `SolicitacoesClinicas` da v0.5.1; **sem migration nova**.
- Mantém isolamento multi-tenant e as regras de autorização existentes.

## Baseline

A v0.5.3 foi validada pelo usuário com **535/535 testes** e é a baseline desta release.

## Validação esperada

- `PREPARAR.ps1`: 31/31.
- `TESTAR.ps1`: 545/545.


## v0.5.5 — Desde a última consulta
- resumo longitudinal no prontuário desde a última consulta (fallback de 30 dias);
- consolidação de registros do diário, treinos e check-ins;
- médias de adesão alimentar e ao treino;
- variação de peso no período;
- solicitações pendentes e aguardando revisão no mesmo contexto;
- sem alteração de schema ou migration nova.
