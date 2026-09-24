## v0.19.26 — Public Access & Password Recovery

- Fluxo público de recuperação de senha com resposta anti-enumeração.
- Link temporário de redefinição com token Identity e expiração de 30 minutos.
- Páginas `/recuperar-senha`, `/redefinir-senha` e retorno para `/entrar`.
- SMTP continua desabilitado por padrão em desenvolvimento; testes permanecem locais.
- Roadmap detalhado: consulte `ROADMAP.md`.

## v0.19.25 — Account & Email Foundation

Esta versão cria a fundação segura de e-mail da conta AESYN sem disparar testes contra produção.

- e-mail principal e status de confirmação;
- confirmação usando tokens do ASP.NET Core Identity;
- troca de e-mail protegida pela senha atual e prevenção de duplicidade;
- sincronização do e-mail do paciente vinculado;
- templates transacionais de confirmação e troca;
- SMTP configurável por ambiente e **desabilitado por padrão**;
- auditoria de envio, falha, confirmação e troca sem persistir tokens sensíveis;
- policy `AuthenticatedOnly` para recursos pertencentes à própria conta, inclusive paciente;
- `TESTAR.ps1` valida a fundação apenas por código/configuração local, sem enviar e-mail real.

> Para produção, configure `Email__Enabled=true` e as demais variáveis `Email__*` somente no ambiente da VPS. Não grave senha SMTP no repositório.

## v0.19.24 — Metabolic Planning UX & Safety

- Clarifica o fluxo TMB → GET → objetivo → meta calórica → macros.
- Mostra a meta calórica ativa do plano e compara macros em kcal e percentual.
- Adiciona ajuste assistido por macro residual, preservando decisão e edição profissional.
- Exige confirmação explícita para aplicar metas abaixo da TMB e/ou com ritmo agressivo; valores extremamente baixos continuam bloqueados.
- Acrescenta ajuda contextual do fator de atividade e testes numéricos conhecidos.
- ROADMAP.md permanece como fonte de direção funcional e não deve ser reduzido ao concluir versões.

## v0.19.23 — Passive Monitoring Foundation

Cria a camada normalizada para receber sinais de Apple Health, Health Connect e Garmin sem acoplar o produto a um provedor específico. A fundação aceita passos, sono, frequência cardíaca de repouso, HRV, energia ativa, distância e minutos ativos, com deduplicação por origem, resumo longitudinal e visualização para paciente/profissional. Os conectores OAuth/nativos continuam como adaptadores futuros; nenhum dado passivo gera diagnóstico ou mudança automática de conduta.

## v0.19.22 — Positive Progress / AESYN XP

Evolui a gamificação para um modelo explicitamente positivo: XP acumulado nunca diminui, semanas mais leves não apagam progresso e descanso planejado não deve gerar compensação. O portal mostra XP dos últimos 7 dias, dias com progresso, avanço de nível, consistência e principais fontes de XP.

## v0.19.21 — Patient Timeline & Context

Timeline longitudinal profissional que reúne prontuário, corpo, exames, nutrição, treino, check-ins, chat, metas, rotina e eventos de adesão. Os eventos são agrupados por dia, filtráveis por dimensão e apresentados com contexto cruzado sem inferir causalidade clínica.
## v0.19.17 — Workout Progression Engine

Progressão assistida por histórico de execução e regras configuradas pelo profissional, com visão para paciente e profissional. O sistema não altera a prescrição automaticamente.

## v0.19.15 — Nutrition Calendar

- Gerenciador profissional de refeições e porções no Nutrition Builder.
- Medidas domésticas com conversão bidirecional para gramas.
- Ajustes rápidos de porção por alimento e escala da refeição inteira.
- Reordenação de refeições e distribuição automática das metas diárias.
- Refeições persistidas podem ser salvas na biblioteca profissional como modelo.
- Sem migration de banco nesta versão.

## v0.19.11 — Dark UI Consistency

- Adiciona planejamento profissional de proteína, lipídios e carboidratos em g/kg de peso corporal.
- Usa como referências configuráveis as faixas 2–3 g/kg de proteína, 0,8–1,2 g/kg de lipídios e 3–7 g/kg de carboidratos, sem tratá-las como prescrição universal.
- Converte automaticamente g/kg em gramas por dia e estima o valor energético dos macros com 4/4/9 kcal por grama.
- Compara a energia dos macros com a meta calórica e destaca diferenças para revisão.
- Permite aplicar as metas ao plano alimentar com um clique, preservando edição manual pelo profissional.

## v0.19.8 — Weight Goal & Calorie Target Planner

- Adiciona objetivo de perder, manter ou ganhar peso com peso-alvo e prazo em semanas.
- Calcula ajuste energético e alvo calórico diário teórico a partir do GET, mantendo aplicação manual/editável pelo profissional.
- Exibe ritmo estimado, alertas para metas agressivas e aviso quando o alvo fica abaixo da TMB estimada.
- Usa aproximação explícita de 7.700 kcal/kg somente para planejamento, sem tratar a projeção como garantia clínica.

## v0.19.7 — Metabolic Energy Calculator

- Adiciona cálculo profissional de TMB e GET dentro do Nutrition Builder.
- Usa Mifflin-St Jeor e disponibiliza Katch-McArdle quando há massa magra registrada.
- Pré-preenche peso/altura pela avaliação mais recente e idade/sexo pelo cadastro do paciente.
- Inclui fatores de atividade configuráveis e permite aplicar o GET como meta calórica editável.
- Mantém o resultado como estimativa de apoio: a decisão final continua sob revisão do profissional.
- Remove rótulo visual legado de versão do Nutrition Builder.

## v0.19.6 — Patient Progress Photos & Markup

O perfil profissional do paciente passa a oferecer galeria de evolução visual, upload protegido, comparação entre datas e marcação gráfica não destrutiva sobre cópias das fotos.

> Produção: os arquivos são gravados em `App_Data/evolucao`. Em deploy com container, monte `App_Data` em volume persistente antes de usar o recurso com dados reais.

## v0.19.5 — Advanced Workout Techniques Builder

- Workout Builder profissional passa a oferecer técnica avançada por exercício: Normal, BISET/conjugado, DROP set ou Progressão de carga.
- BISET cria um par de exercício conjugado e mantém um identificador de grupo na prescrição.
- DROP permite configurar percentual de redução de carga e quantidade de etapas adicionais.
- Progressão de carga permite definir percentual alvo e regra/gatilho clínico-esportivo.
- Técnicas são persistidas de forma estruturada nos treinos-modelo e convertidas em orientação legível ao atribuir o modelo ao paciente.
- Mantém o banco atual sem migration nesta etapa; a estrutura avançada vive no conteúdo versionado dos modelos.
- Interface continua mobile-first e adequada ao tema escuro premium AESYN.

## v0.19.3 — Patient Workout Access Hub

- HTTPS preparado para reverse proxy Nginx na VPS com `X-Forwarded-Proto`.
- HSTS habilitado fora de Development.
- Guia operacional em `PRODUCAO-HTTPS.md`.
- Sem alteração de schema.

# AESYN Performance — v0.19.0

## AESYN Identity & Patient Navigation Foundation

A v0.19.0 inicia a nova fase do AESYN com identidade verde-petróleo, escolha de tema claro/escuro e uma navegação do paciente mais orientada ao cuidado integrado: Hoje, Treino, Plano e Saúde. O novo hub Saúde reúne acesso a exames, monitoramento, recuperação e registros de como o paciente está.

- nova identidade visual AESYN;
- tema claro/escuro persistente;
- novo hub Saúde;
- navegação mobile-first reorganizada;
- sem migration nova; schema 39/39 preservado.


## v0.18.7 — Workout Builder 3.0
A fase profissional de treino passa a priorizar velocidade de produção e reuso. O builder standalone agora oferece presets de prescrição, busca contextual por exercício/grupo/equipamento, duplicação e ordenação de exercícios e sessões, resumo dinâmico de volume prescrito e duplicação completa de modelos. Continua sem exigir paciente vinculado e sem migration nova.


## v0.18.8 — Workout Template Library 2.0
A biblioteca de treino passa a funcionar como catálogo profissional: filtros por objetivo e status, favoritos locais, ordenação por tamanho/recência, chips de objetivo e preview completo da composição do treino antes de editar ou atribuir. O fluxo continua independente de paciente e atribuições continuam criando cópias independentes.


## v0.18.9 — Program Builder 1.0
O Workout Studio passa a permitir montar programas independentes de paciente usando treinos-modelo como blocos. Cada programa pode ter múltiplas fases, duração em semanas e agenda semanal própria. Nesta primeira versão, programas são persistidos no navegador e podem ser exportados/importados em JSON, mantendo o schema 38/38 intacto.


## v0.18.10 — Server-side Program Catalog
Programas profissionais deixam de depender do localStorage e passam a ser persistidos no banco por organização/profissional. A versão adiciona CRUD server-side, duplicação, importação JSON via API e migração assistida dos programas locais da v0.18.9.


## v0.18.16 — Workout Builder Delete + Weekly Patient Plan

- Corrige a remoção de exercícios no Workout Builder.
- Troca o pequeno `×` por uma ação explícita `Remover exercício`, com área de toque adequada no mobile.
- Mantém duplicação e remoção independentes por exercício.
- Treino do dia agora tenta respeitar `diasSemana` antes de usar fallback.
- Adiciona `Treinos da semana` no portal do paciente com todas as sessões do plano.
- Destaque visual para a sessão de hoje sem esconder a programação completa.
- Cards semanais levam diretamente à ficha completa da sessão.
- Melhora leitura e execução da ficha no celular.
- Sem alteração de schema; baseline permanece 39/39.

## v0.18.15 — Nutrition Review & Publish 2.0

- Novo fluxo profissional específico para revisar e publicar planos alimentares.
- Entrada `Revisar & publicar` diretamente na aba Nutrição do paciente.
- Histórico de versões com plano ativo destacado.
- Revisão de kcal, proteína, carboidrato, gordura e fibra contra as metas.
- Revisão das refeições, horários, itens e orientações antes da publicação.
- Publicação explícita preservando versões anteriores no histórico.
- Atalhos para editar a versão e abrir modelos de dieta.
- Retry em caso de falha e layout responsivo.
- Sem alteração de schema; baseline permanece 39/39.

## v0.18.14 — Nutrition Templates 2.0

- Corrige o erro de abertura da biblioteca de modelos nutricionais causado por handlers de botões inexistentes.
- Biblioteca profissional única para modelos de dieta e modelos de refeição.
- KPIs de dietas, refeições, blocos e itens.
- Busca normalizada por nome, descrição, categoria e profissional.
- Filtros por status e categoria.
- Ordenação por nome, quantidade de itens ou refeições.
- Atribuição de dieta ao paciente e inserção de refeição em plano existente.
- Edição, ativação e desativação de modelos.
- Empty state, retry e atualização da biblioteca.
- CTA para montar nova dieta quando aberto dentro do paciente.
- Sem alteração de schema; baseline permanece 39/39.

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


## Roadmap do produto

A direção de evolução, pendências protegidas e gates de produto estão documentados em [`ROADMAP.md`](ROADMAP.md). Antes de iniciar uma nova versão funcional, confira esse arquivo para evitar perda de escopo ou ideias.
