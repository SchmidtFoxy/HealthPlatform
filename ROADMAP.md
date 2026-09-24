# AESYN Performance — Roadmap Mestre

> **Hotfix v0.19.25-r2:** corrige o gate `[1908/1914]` para validar `EmailPrincipal`, `Confirmado` e `PodeSolicitarConfirmacao` no contrato `EmailAccountStatusResponse`, onde esses campos realmente são declarados, em vez de exigir seus nomes literais dentro do controller. Nenhuma alteração funcional ou de schema.

> **Hotfix v0.19.25-r1:** `TESTAR.ps1` deve permanecer em **UTF-8 com BOM** para compatibilidade com Windows PowerShell 5.1. O `PREPARAR.ps1` valida essa condição para impedir regressões de encoding.


> **Versão-base deste roadmap:** v0.19.25 (revisão de pacote atual: v0.19.25-r2) — Account & Email Foundation  
> **Propósito:** impedir que ideias, pendências e detalhes de produto sejam esquecidos durante a evolução do AESYN.

## Regra de uso deste arquivo

Este arquivo é a referência de direção do produto. Antes de iniciar uma nova versão:

1. conferir a próxima entrega neste roadmap;
2. verificar se há pendências abertas da fase atual;
3. não remover uma ideia apenas porque outra prioridade apareceu;
4. quando uma ideia mudar de posição, mover o item e registrar o motivo em vez de apagá-lo;
5. uma versão funcional só deve ser considerada concluída depois de PREPARAR → RODAR → TESTAR e do gate específico da funcionalidade;
6. correções da mesma versão usam `-r1`, `-r2`, etc.;
7. uma revisão não deve virar funcionalidade nova escondida;
8. funcionalidades clínicas devem apoiar a decisão profissional, não substituir julgamento médico/nutricional.

---

# 1. Visão do produto

O AESYN deve evoluir de uma plataforma que apenas registra treino, dieta, exames e check-ins para um **sistema operacional de acompanhamento de performance**.

A plataforma deve conectar três coisas:

- **o que foi prescrito pelo profissional**;
- **o que o paciente realmente executou**;
- **o que está acontecendo com o paciente ao longo do tempo**.

A experiência do paciente deve ser simples, mobile-first e orientada ao dia atual. A experiência do profissional deve ser rica, longitudinal e proativa.

## Princípios permanentes

- Mobile-first.
- Médico/profissional no centro da decisão clínica.
- Histórico nunca deve ser perdido por sobrescrita silenciosa.
- Auditoria para ações sensíveis.
- Estados de loading, erro, vazio e retry padronizados.
- Tema claro e escuro completos.
- PWA tratado como produto real, não como detalhe técnico.
- Segurança, isolamento entre pacientes e permissões por perfil em todas as versões.
- Dados técnicos avançados separados da Home comum do paciente.
- Alertas devem explicar por que foram gerados.
- Gamificação deve premiar adesão e consistência, não incentivar excesso.

---

# 2. Baseline já existente até v0.19.24

Este bloco é histórico e serve para evitar que o roadmap tente recriar como novidade algo que já existe.

- v0.19.0 — identidade AESYN e fundação da navegação do paciente.
- v0.19.1 — HTTPS e proxy de produção.
- v0.19.2 — limpeza da experiência de produção.
- v0.19.3 — Patient Workout Access Hub.
- v0.19.4 — técnicas avançadas no Workout Builder.
- v0.19.5 — orientação ao paciente para técnicas avançadas.
- v0.19.6 — fotos de progresso e marcação.
- v0.19.7 — calculadora de TMB e GET.
- v0.19.8 — meta de peso e alvo energético.
- v0.19.9 — metas de macronutrientes por peso corporal.
- v0.19.10 — conversor de porções e medidas.
- v0.19.11 — consistência do dark mode.
- v0.19.12 — Meal & Portion Manager 2.0.
- v0.19.13 — equivalências inteligentes de alimentos.
- v0.19.14 — calendário nutricional.
- v0.19.15 — Smart Meal Swap.
- v0.19.16 — Workout Progression Engine.
- v0.19.17 — alternativas inteligentes de exercícios.
- v0.19.18 — chat paciente ↔ profissional.
- v0.19.19 — eventos de divergência e adesão.
- v0.19.20 — Professional Attention Center.
- v0.19.21 — timeline e contexto do paciente.
- v0.19.22 — Positive Progress / AESYN XP.
- v0.19.23 — Passive Monitoring Foundation.
- v0.19.24 — Metabolic Planning UX & Safety: fluxo TMB→GET→objetivo→meta→macros, meta ativa, delta percentual, macro residual e confirmação de segurança.

Essas funcionalidades podem e devem receber versões 2.0/3.0 nas fases futuras.

---

# 3. Fase imediata — fechamento da Lista 03

A prioridade atual é transformar a base já grande do AESYN em uma experiência coesa, confiável e pronta para uso real.

## ✅ v0.19.24 — Metabolic Planning UX & Safety — CONCLUÍDA

### Problema a resolver
A calculadora atual consegue produzir TMB, GET, alvo de peso e macros corretamente, mas as quatro etapas aparecem como blocos parcialmente independentes. Isso pode fazer o profissional interpretar números conflitantes como erro do sistema.

### Entregas
- Explicar visualmente a sequência: **TMB → GET → objetivo → meta calórica → macros**.
- Exibir de forma explícita qual número está ativo como `Meta calórica do plano`.
- Diferenciar claramente:
  - TMB;
  - GET/manutenção;
  - ajuste energético;
  - alvo calórico;
  - energia resultante dos macros.
- Ao aplicar novo alvo calórico, recalcular/assistir a distribuição de macros em vez de manter silenciosamente valores incompatíveis.
- Permitir escolher qual macro ficará residual após proteína e lipídios, normalmente carboidrato, sem impedir ajuste manual.
- Mostrar delta energético em kcal **e percentual**.
- Destacar metas agressivas antes de aplicação.
- Quando o alvo ficar abaixo da TMB, exigir confirmação explícita do profissional ou política configurável da clínica.
- Não transformar TMB em “mínimo absoluto” clínico automático; tratar como sinal de revisão.
- Incluir ajuda contextual para os fatores de atividade.
- Diferenciar `peso atual`, `peso de referência dos macros` e `peso desejado`.
- Testes unitários do cálculo, não apenas testes por presença de tokens na UI.
- Cenários de teste conhecidos com valores esperados.

### Gate
O profissional deve conseguir explicar, olhando apenas para a tela, de onde surgiu cada valor e por que macros e calorias estão ou não alinhados.

**Status:** implementado na v0.19.24; validar em ambiente local pelo fluxo PREPARAR → RODAR → TESTAR antes de produção.

---

## v0.19.25 — Account & Email Foundation

- cadastro/gestão de e-mails;
- confirmação de e-mail;
- e-mail principal;
- troca segura de e-mail;
- prevenção de duplicidade;
- templates básicos de comunicação;
- rastreio de envio/falha.

### Gate
Paciente e profissional conseguem consultar o e-mail principal, solicitar confirmação e trocar o endereço de forma segura, sem duplicidade e sem expor tokens. O desenvolvimento não envia mensagens reais quando `Email:Enabled=false`.

**Status:** implementado na v0.19.25; validar localmente com PREPARAR → RODAR → TESTAR antes de qualquer deploy.

## v0.19.26 — Public Access & Password Recovery

- páginas para usuário não autenticado;
- esqueci minha senha;
- token de redefinição com expiração de 30 minutos;
- redefinição de senha;
- resposta anti-enumeração no pedido de recuperação;
- mensagens claras de sucesso/erro;
- redirecionamentos corretos entre login, recuperação e redefinição;
- auditoria de solicitação/conclusão sem persistir token ou senha.

### Gate
Usuário não autenticado consegue solicitar recuperação e redefinir a senha por link temporário; a API não revela se o e-mail existe, não envia e-mail real quando `Email:Enabled=false` e não persiste token/senha em auditoria.

**Status:** implementado na v0.19.26; validar localmente com PREPARAR → RODAR → TESTAR antes de qualquer deploy.

## v0.19.27 — Session & Authorization Hardening

- revisar 401/403;
- corrigir chat 403;
- expiração de sessão;
- renovação de autenticação enquanto o JWT atual ainda é válido;
- logout consistente com revogação imediata dos JWTs anteriores via `SecurityStamp`;
- bloqueio temporário após 5 tentativas inválidas por 15 minutos;
- revisão de permissões de paciente, profissional e administrador;
- garantir isolamento de dados entre pacientes e entre profissionais no chat.

### Gate
Paciente acessa o próprio chat sem 403; profissional continua restrito às rotas profissionais; outro profissional da mesma organização não consegue abrir a conversa usando apenas o GUID; 401 encerra a sessão local, 403 informa falta de permissão sem deslogar; logout revoga o token anterior e a sessão ativa pode ser renovada antes da expiração.

**Status:** implementado na v0.19.27; validar localmente com PREPARAR → RODAR → TESTAR antes de qualquer deploy.

## v0.19.28 — Settings & User Profile

- página de configurações;
- foto;
- dados básicos;
- alteração de senha;
- tema;
- privacidade;
- preferências de notificações;
- sessões/dispositivos ativos quando aplicável.

## v0.19.29 — AESYN App Identity & Theme Completion

- ícone do app;
- favicon;
- manifest;
- ícones PWA;
- splash/launch experience quando suportado;
- nome do aplicativo instalado;
- tema claro/escuro consistente em todas as telas;
- corrigir telas do paciente que ainda não aderiram ao dark mode;
- escolha de tema no primeiro acesso.

## v0.19.30 — Patient Home Cleanup

- corrigir o CSS do “Hoje em um olhar”;
- simplificar Home do paciente;
- Home deve responder **“o que eu preciso fazer hoje?”**;
- priorizar check-in, treino, alimentação, hidratação, mensagens e pendências;
- remover excesso de indicadores técnicos da Home;
- estados vazios, loading, erro e retry.

## v0.19.31 — Athlete Data Hub

Criar/amadurecer a área **Dados para Atletas** para retirar complexidade da Home comum.

- carga;
- volume;
- sono;
- recuperação;
- peso;
- composição corporal;
- histórico;
- performance;
- tendências;
- métricas avançadas;
- XP/streaks quando fizer sentido.

## v0.19.32 — Professional Nutrition Access Completion

- disponibilizar no acesso profissional todas as ações de nutrição que já existirem no backend/UI;
- criar plano alimentar;
- editar;
- duplicar;
- arquivar/excluir conforme regra de histórico;
- refeições;
- alimentos;
- equivalências;
- metas;
- atribuição ao paciente;
- revisão/publicação.

## v0.19.33 — Workout CRUD & Multi-Plan Completion

- CRUD completo dos treinos;
- remoção/arquivamento real;
- substituição não pode ser a única forma de retirar treino;
- paciente pode ter nenhum, um ou vários planos;
- Treino A/B/C, cardio, recuperação etc.;
- início livre quando permitido pela prescrição;
- duplicação;
- desvinculação;
- histórico/versionamento preservado.

## v0.19.34 — Patient Files Mobile 2.0

- biblioteca funcional de arquivos;
- upload pelo celular;
- câmera;
- galeria;
- PDF;
- exames;
- laudos;
- categoria;
- descrição;
- tags;
- data;
- pesquisa;
- anexar/indexar arquivo no chat com o profissional;
- permissões e auditoria de download/remoção.

## v0.19.35 — Chat Reliability & Context Foundation

- corrigir definitivamente autorização do chat;
- mensagens não lidas;
- badge;
- status enviada/entregue/lida quando tecnicamente aplicável;
- anexos;
- foto/PDF/arquivo;
- tratamento de erro/retry;
- base para contexto de treino, exercício, refeição e exame.

## v0.19.36 — Administrator Professional Impersonation

- administrador simular acesso do profissional;
- banner persistente indicando impersonação;
- botão para sair da simulação;
- auditoria de quem iniciou/finalizou;
- nenhuma impersonação silenciosa;
- impedir ações incompatíveis com a política de segurança.

## v0.19.37 — Patient First Access & Tutorials

- onboarding de primeiro acesso;
- tutorial do check-in diário;
- auxílio do primeiro treino;
- explicação básica de alimentação/plano;
- introdução ao chat;
- tutoriais podem ser refeitos depois;
- ajuda contextual por `?` ou equivalente.

## v0.19.38 — PWA Install & Mobile Ergonomics

- atalho/banner para instalar o PWA;
- instrução própria para Safari/iPhone;
- Android/Chrome;
- modo standalone;
- safe areas;
- teclado não cobrindo campos;
- touch targets;
- scroll;
- modais;
- botão voltar físico do Android;
- teste em telas pequenas e grandes.

## v0.19.39 — Push Notifications End-to-End

Paciente deve receber, conforme preferência:
- nova mensagem;
- atualização/publicação de treino;
- atualização/publicação de dieta;
- lembretes relevantes.

Profissional deve receber:
- novo check-in relevante;
- treino realizado quando configurado;
- divergência do planejado;
- dor/dificuldade reportada;
- novo arquivo/exame;
- nova mensagem.

Testar push com navegador/app fechado e PWA instalado.

## v0.19.40 — In-App Notification Center

- central interna;
- lidas/não lidas;
- badges;
- prioridade;
- histórico;
- preferências por categoria;
- deep link para origem da notificação.

## v0.19.41 — UX State System

Padronizar transversalmente:
- skeleton;
- loading;
- empty state;
- erro;
- retry;
- toast;
- confirmação;
- modal;
- sucesso;
- ações destrutivas.

## v0.19.42 — Offline & Poor Connection Resilience

- detectar ausência de internet;
- informar sem parecer falha do aplicativo;
- preservar formulários digitados;
- retry;
- reconexão;
- fila apenas para ações em que seja seguro;
- evitar duplicidade de submissões.

## v0.19.43 — Audit, Privacy & Consent

- auditoria de treino;
- dieta;
- arquivos;
- alterações do paciente;
- impersonação;
- exclusões/arquivamentos;
- aceite de termos;
- versão do termo;
- data/hora do aceite;
- política de privacidade;
- fluxo de desativação da conta;
- revisão de requisitos LGPD.

## v0.19.44 — Product Flow Gate / Lista 03 Closure

### Fluxo paciente obrigatório
primeiro acesso → tutorial → check-in → visualizar plano → escolher/iniciar treino permitido → executar → registrar alimentação → conversar → enviar arquivo → receber atualização profissional.

### Fluxo profissional obrigatório
login → localizar paciente → revisar contexto → criar/alterar treino → criar/alterar dieta → publicar → conversar → revisar arquivos/exames → receber alertas → acompanhar evolução.

### Gate
Nenhum desses fluxos pode depender de atalho de desenvolvedor, banco manual, Swagger ou intervenção técnica.

---

# 4. v0.20.x — Professional Workspace 2.0

Objetivo: transformar a área profissional em uma central de trabalho para dezenas/centenas de pacientes.

## v0.20.0 — Professional Dashboard 2.0
- visão executiva;
- pendências;
- mensagens;
- alertas;
- pacientes recentes;
- ações rápidas.

## v0.20.1 — Patient Search & Advanced Filters
- busca global;
- filtros por status;
- tags;
- responsável;
- aderência;
- última interação;
- próxima revisão.

## v0.20.2 — Patient Attention Queue 2.0
Evoluir a Central de Atenção existente:
- sem check-in;
- dor alta;
- sono ruim recorrente;
- baixa adesão;
- treino não realizado;
- alimentação divergente;
- mensagens pendentes;
- novos exames;
- prioridades configuráveis.

## v0.20.3 — Patient Status Management
- ativo;
- pausado;
- aguardando avaliação;
- encerrado;
- status administrativo futuro.

## v0.20.4 — Professional Internal Notes
- observações privadas;
- não visíveis ao paciente;
- histórico;
- autoria/data.

## v0.20.5 — Tags & Segmentation
- tags personalizadas;
- filtros;
- agrupamentos;
- buscas salvas futuramente.

## v0.20.6 — Shared Care / Multiple Professionals
- profissional principal;
- médico;
- nutricionista;
- treinador;
- permissões por relação;
- histórico de responsáveis.

## v0.20.7 — Favorites & Quick Access
- pacientes favoritos;
- recentes;
- ações rápidas.

## v0.20.8 — Templates Ownership
- template pessoal;
- template da clínica;
- compartilhado;
- arquivado;
- busca e filtros.

## v0.20.9 — Professional Workspace Gate

---

# 5. v0.21.x — Workout Intelligence 2.0

Objetivo: permitir prescrição e execução avançadas sem depender de ferramenta externa.

- Advanced Exercise Prescription.
- Biset/superset.
- Triset.
- Circuitos.
- Drop set.
- Rest-pause.
- Pirâmides.
- RPE.
- RIR.
- Cadência/tempo sob tensão.
- Tempo de descanso.
- Aquecimento específico.
- Séries preparatórias.
- Alternativas autorizadas.
- Exercício unilateral e registro por lado.
- Vídeo/demonstração.
- Execução mobile série a série.
- Cronômetro de descanso.
- Última carga/repetições/RPE visíveis durante a execução.
- Progressão de carga.
- Progressão de repetições.
- Volume total.
- PRs/melhores marcas.
- Histórico por exercício.
- Versionamento imutável de prescrições publicadas.

Sugestão de subdivisão: v0.21.0 a v0.21.20, uma capacidade relevante por versão.

---

# 6. v0.22.x — Nutrition Intelligence 2.0

Objetivo: levar nutrição ao mesmo nível de maturidade do treino.

- Food Library 2.0.
- Meal Builder 2.0.
- Diet Builder 3.0.
- templates.
- copiar refeição.
- duplicar dieta.
- horários.
- quantidades e unidades.
- alimento opcional.
- alternativas autorizadas.
- equivalências.
- favoritos.
- consumo realizado.
- planejado × consumido.
- motivo de divergência.
- adesão nutricional.
- histórico longitudinal.
- versionamento de dieta publicada.
- coerência automática/assistida entre meta calórica e macros.

---

# 7. v0.23.x — Patient Daily Experience 2.0

Objetivo: paciente comum deve entender a tela inicial em poucos segundos.

Home orientada a:
- Check-in.
- Treino do dia / treinos disponíveis.
- Alimentação.
- Hidratação.
- Mensagens.
- Pendências.

Incluir:
- prioridades do dia;
- quick actions;
- progresso diário;
- fechamento do dia;
- integração com Dados para Atletas sem poluir a Home.

---

# 8. v0.24.x — Check-in & Recovery Intelligence 2.0

Campos/sinais:
- sono e qualidade;
- energia;
- dor;
- fadiga;
- estresse;
- humor;
- motivação;
- recuperação;
- disposição;
- peso opcional;
- observação livre.

Evoluções:
- alertas de dor;
- tendências de recuperação;
- tendências de sono;
- tendência de adesão;
- fila profissional de revisão;
- comparação com baseline individual.

Se houver sinal relevante, o sistema deve registrar e sinalizar, não apenas armazenar.

---

# 9. v0.25.x — Central de Acompanhamento 2.0

Objetivo: profissional deve poder começar o dia por uma única tela.

Exemplo:
- pacientes com dor alta;
- sem treinar há vários dias;
- sono ruim recorrente;
- alimentação divergente;
- mensagens sem resposta;
- novos exames;
- queda de aderência;
- progresso positivo digno de reforço.

Recursos:
- severidade;
- prioridade;
- filtros;
- revisar;
- adiar;
- resolver;
- histórico;
- deep link para contexto.

---

# 10. v0.26.x — Communication 2.0

- Chat 2.0.
- read receipts.
- badges.
- resposta a mensagem específica.
- mensagem importante.
- anexos.
- mensagens automáticas do sistema.
- contexto de treino.
- contexto de exercício.
- contexto de refeição.
- contexto de exame.
- contexto de arquivo.

Exemplo desejado:

> Paciente está falando sobre: Agachamento livre • Treino B • Série 3/4 • última carga registrada 90 kg.

---

# 11. v0.27.x — Files, Exams & Body Evolution 2.0

- biblioteca de documentos 3.0;
- categorias e tags;
- upload mobile;
- revisão de exame;
- valores/unidades/referências;
- trends laboratoriais;
- peso;
- circunferências;
- composição corporal;
- bioimpedância;
- fotos de progresso;
- comparação lado a lado;
- frente/lado/costas;
- desenho/marcação;
- comentário profissional;
- status revisado/não revisado.

Evitar diagnóstico automático de exames.

---

# 12. v0.28.x — Clinical & Performance Timeline 2.0

Evoluir a timeline existente para visão longitudinal unificada:

avaliação → treino v1 → dieta v1 → exames → check-ins → alterações → fotos → peso → treino v2 → retorno.

Filtros:
- treino;
- nutrição;
- exames;
- composição corporal;
- comunicação;
- avaliações;
- arquivos;
- alertas.

Objetivo: compreender meses de acompanhamento sem abrir dezenas de telas.

---

# 13. v0.29.x — Adaptive Monitoring 2.0

Evoluir Passive Monitoring para detecção explicável de padrões.

- sono;
- recuperação;
- dor;
- performance;
- nutrição;
- adesão;
- engajamento;
- múltiplos sinais combinados.

Exemplo:

sono ↓ + recuperação ↓ + performance ↓ + dor ↑ → **sinal para revisão profissional**.

Regras:
- mostrar quais dados contribuíram;
- baseline individual;
- evitar diagnóstico;
- evitar mudança automática de prescrição sem regra e autorização profissional.

---

# 14. v0.30.x — Gamification 3.0

- XP 3.0;
- níveis;
- smart streaks;
- conquistas;
- metas semanais;
- desafios;
- temporadas;
- recompensas por recuperação adequada;
- treino;
- alimentação;
- hidratação;
- check-ins;
- consistência.

Não premiar volume excessivo ou comportamento potencialmente inadequado.

---

# 15. v0.31.x — Clinic & Team

- organizações/clínicas;
- membros da equipe;
- profissionais;
- recepção;
- administrador;
- papéis;
- permissões granulares;
- compartilhamento de cuidado;
- atribuição de pacientes;
- convites;
- auditoria da clínica;
- templates da organização.

---

# 16. v0.32.x — SaaS & Monetization

- planos;
- assinatura;
- trial;
- limites de pacientes/recursos;
- cobrança;
- upgrade/downgrade;
- inadimplência;
- administração SaaS;
- métricas de uso;
- métricas de receita;
- faturamento.

---

# 17. v0.33.x+ — Integrations

A ordem deve ser reavaliada quando esta fase chegar, porque APIs e mercado mudam.

Candidatas:
- Apple Health;
- Health Connect;
- Garmin;
- Strava;
- wearables;
- balanças inteligentes;
- dispositivos esportivos;
- laboratórios;
- APIs externas.

---

# 18. Backlog protegido — ideias que NÃO podem sumir

Mesmo que mudem de versão, estes itens devem permanecer rastreáveis:

- fotos comparativas com upload e desenho/marcação;
- múltiplos treinos A/B/C e início livre quando permitido;
- biset/conjugado, drop set e progressão de carga;
- chat direto paciente ↔ profissional;
- alertas por divergências do planejado;
- histórico dessas divergências;
- profissional enxergar padrões de adesão;
- Central de Acompanhamento proativa;
- “Dados para Atletas” separado da Home comum;
- onboarding/refazer tutoriais;
- upload mobile e anexos no chat;
- notificações nos dois sentidos;
- histórico/versionamento de treino e dieta;
- observações privadas do profissional;
- múltiplos profissionais por paciente;
- timelines longitudinais;
- exames e composição corporal;
- uso de câmera/galeria;
- PWA instalável;
- experiência real em iOS/Android;
- offline/conexão ruim;
- auditoria;
- LGPD/consentimentos;
- monetização SaaS;
- clínicas/equipes;
- integrações de saúde e esporte;
- gamificação voltada a consistência saudável;
- monitoramento adaptativo explicável;
- separar sinalização automática de decisão profissional.

---

# 19. Gates permanentes de toda versão

## Segurança
- autenticação;
- autorização;
- isolamento por usuário/paciente/organização;
- auditoria quando aplicável;
- nenhum segredo em código ou ZIP.

## Mobile
- iPhone;
- Android;
- navegador;
- PWA;
- layout pequeno;
- teclado;
- safe-area;
- touch.

## UX
- claro;
- escuro;
- loading;
- erro;
- vazio;
- retry;
- responsividade.

## Backend e banco
- build limpo;
- migration consistente;
- scripts idempotentes quando aplicável;
- índices;
- integridade referencial;
- sem pending model changes.

## Produção
- PREPARAR;
- RODAR;
- TESTAR;
- health endpoint;
- Docker/deploy quando aplicável;
- smoke test real.

## Histórico
- não sobrescrever prescrição publicada sem estratégia explícita;
- arquivar/versionar em vez de apagar quando houver relevância clínica.

---

# 20. Definição de “monstro” para o AESYN

O objetivo final não é ter o maior número de telas. O AESYN deve ser forte porque:

1. o paciente entende exatamente o que fazer hoje;
2. o profissional sabe exatamente quem precisa de atenção;
3. treino e nutrição possuem prescrição, execução e histórico;
4. comunicação carrega contexto;
5. dados de check-in, exames e performance se conectam no tempo;
6. alterações importantes são percebidas sem exigir busca manual;
7. o sistema explica seus alertas;
8. decisões continuam sob responsabilidade profissional;
9. a experiência mobile é excelente;
10. toda evolução é auditável e não destrói o histórico.

---

**Este arquivo deve ser atualizado, nunca substituído por uma lista menor.**


## Regra operacional — ambientes de teste

- `TESTAR.ps1` e os gates de desenvolvimento devem executar **somente em localhost/127.0.0.1**.
- A VPS de producao (`aesyn.com.br`) nao deve ser usada como alvo de testes de desenvolvimento.
- Validacao de producao deve ser separada, explicitamente acionada para deploy/healthcheck e preferencialmente somente leitura.
- Render nao faz mais parte da infraestrutura atual; arquivos, seeds e smoke tests especificos do Render sao considerados legados.
- Politica consolidada na revisao **v0.19.24-r2**.


### Revisão v0.19.24-r4 — seeds demonstrativos opcionais

- `POPULAR-ANA-RIBEIRO.ps1` não faz mais parte do pacote limpo e não é requisito para PREPARAR/TESTAR.
- Populadores e cenários demo devem ser ferramentas opcionais, nunca dependências do gate funcional do produto.
- Quando um seed demonstrativo for removido, os testes devem validar as funcionalidades reais diretamente em código/API em vez de exigir o arquivo legado.
- Esta revisão não altera a versão funcional pública `0.19.24`.

### Revisão v0.19.24-r3 — limpeza de sobreposição
- O `PREPARAR.ps1` deve remover resíduos de deploy Render preservados por extrações sobrepostas no Windows.
- O pacote oficial continua sem artefatos do Render.
- `TESTAR.ps1` permanece estritamente local e nunca deve usar a VPS/produção como alvo de testes de desenvolvimento.


### v0.19.24-r5
- Hotfix do gate de comparação calórica de macros.

### Revisão v0.19.27-r1 — sincronização global de versão no smoke test

- Corrige os gates históricos do `TESTAR.ps1` que ainda comparavam `VERSION.txt`/health com `0.19.26` após a evolução funcional para `0.19.27`.
- Todos os gates que validam a versão pública corrente agora esperam `0.19.27`; marcadores históricos de funcionalidades antigas continuam preservados.
- Não altera funcionalidade, schema, migration ou contrato da API; versão funcional permanece `0.19.27`.
- Mantém `TESTAR.ps1` em UTF-8 com BOM para compatibilidade com Windows PowerShell 5.1.
