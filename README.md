# AESYN Performance — v0.18.6

## Exercise Library 2.0 + Standalone Workout Studio

Esta versão desloca o foco para a produção profissional de treinos: catálogo amplo de exercícios, base inicial curada e treino-modelo totalmente independente de paciente.

- catálogo profissional pesquisável e filtrável;
- população opcional de uma base inicial com dezenas de exercícios;
- CRUD e ativação/inativação preservando histórico;
- criação e edição de treinos-modelo sem paciente;
- sessões e prescrições completas dentro do modelo;
- atribuição posterior continua criando cópia independente para o paciente;
- sem migration nova; schema 38/38 preservado.


## v0.18.7 — Workout Builder 3.0
A fase profissional de treino passa a priorizar velocidade de produção e reuso. O builder standalone agora oferece presets de prescrição, busca contextual por exercício/grupo/equipamento, duplicação e ordenação de exercícios e sessões, resumo dinâmico de volume prescrito e duplicação completa de modelos. Continua sem exigir paciente vinculado e sem migration nova.


## v0.18.8 — Workout Template Library 2.0
A biblioteca de treino passa a funcionar como catálogo profissional: filtros por objetivo e status, favoritos locais, ordenação por tamanho/recência, chips de objetivo e preview completo da composição do treino antes de editar ou atribuir. O fluxo continua independente de paciente e atribuições continuam criando cópias independentes.


## v0.18.9 — Program Builder 1.0
O Workout Studio passa a permitir montar programas independentes de paciente usando treinos-modelo como blocos. Cada programa pode ter múltiplas fases, duração em semanas e agenda semanal própria. Nesta primeira versão, programas são persistidos no navegador e podem ser exportados/importados em JSON, mantendo o schema 38/38 intacto.


## v0.18.10 — Server-side Program Catalog
Programas profissionais deixam de depender do localStorage e passam a ser persistidos no banco por organização/profissional. A versão adiciona CRUD server-side, duplicação, importação JSON via API e migração assistida dos programas locais da v0.18.9.


## v0.18.13 — Nutrition Builder Large Catalog

- Nutrition Builder preparado para catálogos com milhares de alimentos.
- Busca normalizada sem acentos, por múltiplos termos, nome e categoria.
- Ranking prioriza correspondência exata, prefixo e aliases PT-BR.
- Cada seletor mostra no máximo 120 resultados filtrados, evitando renderizar 10 mil opções em cada linha.
- Busca com debounce para reduzir custo de renderização.
- Totais ao vivo por refeição e total diário.
- Metas agora com comparação também de fibras.
- Estruturas rápidas de 3, 4, 5 ou 6 refeições.
- Layout mobile reforçado.
- Sem alteração de schema; baseline permanece 39/39.

## v0.18.12 — Professional Workout & Nutrition Flow Hardening

- Corrige `+ Novo treino/plano` no prontuário: o Workout Builder agora abre o modal diretamente.
- Corrige overflow e espaçamento do Workout Builder em desktop, tablet e celular.
- Reforça o acesso a Programas de treino com estado de erro e retry.
- Reorganiza as ações profissionais de Treino e Nutrição no paciente.
- Adiciona `+ Adicionar treino` a um plano existente para criar Treino B/C/D sem remontar o plano.
- Mantém `Editar / agregar` para ajustar sessões e exercícios já salvos.
- Expõe `+ Montar dieta`, modelos de dieta e modelos de refeição diretamente no paciente.
- Sem alteração de schema; baseline de banco permanece 39/39.

## v0.18.11 — Program Assignment & Publish
O catálogo profissional de programas passa a ter um fluxo de publicação para pacientes. A partir do preview do programa, o profissional escolhe o paciente, revisa início/opções e publica. Cada fase do programa é materializada em um `PlanoTreino` e uma `FaseTreino`, usando os treinos-modelo como fonte, sem alterar o programa mestre. Não há migration nova; a versão reutiliza o schema 39/39.
