# AESYN Performance — Roadmap Mestre

> **Hotfix local v0.22.1-r2:** alinha o gate histórico do guia do Morning Check-in à UX atual. O teste deixa de exigir a cópia antiga `Leva menos de 1 minuto.` e passa a aceitar a mensagem atual de aproximadamente 30 segundos. Revisão local; sem commit individual.

> **Hotfix local v0.22.1-r1:** alinha o gate histórico de prontidão mobile ao renderer `hpMorningScaleFieldV0221` introduzido no Morning Check-in 2.0. O gate deixa de exigir `patientScaleField(...)`, implementação interna substituída, e continua validando as cinco escalas de sono, energia, dor, disposição e recuperação. Revisão local; sem commit individual.

> **Hotfix local v0.21.0-r7:** alinha o gate histórico da `v0.20.10 — Period Comparison 2.0` aos helpers realmente existentes no frontend. Remove a exigência do símbolo inexistente `hpPeriodWindowV02010` e valida as implementações reais de janela semanal, 30 dias, início × atual e estatísticas de período. Revisão local; sem commit individual.

> **Hotfix local v0.21.0-r6:** substitui contagens frágeis baseadas em `Select-String(...).Matches.Count` por `[regex]::Matches(...).Count` no `TESTAR.ps1`, evitando `PropertyNotFoundStrict` quando não há `MatchInfo` compatível. Revisão local; sem commit individual.

> **Hotfix local v0.21.0-r5:** normaliza `$renderLegado` como array no gate histórico de deploy, evitando falha de `.Count` quando o PowerShell retorna um único item. Revisão local; sem commit individual.

> **Hotfix local v0.21.0-r4:** torna o gate histórico de nutrição tolerante a respostas sem a propriedade opcional `refeicoes`. O teste valida a existência da propriedade antes de enumerá-la, tratando ausência como coleção vazia sob `Set-StrictMode`. Revisão local; sem commit individual.

> **Hotfix local v0.21.0-r3:** corrige o uso da variável reservada `$PID` no `TESTAR.ps1`. Todos os identificadores locais de paciente usados pelos gates passam a usar `$pacienteIdSmoke`, evitando conflito com a variável automática somente leitura do PowerShell. Revisão local; sem commit individual.

> **Hotfix local v0.21.0-r2:** restaura compatibilidade dos gates históricos que ainda usam `$pacientes`, criando um alias para a listagem paginada corrente `$lista`. Evita alterar dezenas de gates legados e mantém uma única resposta de API como fonte do smoke test. Revisão local; sem commit individual.

> **Hotfix local v0.21.0-r1:** promove corretamente a identidade funcional corrente para `0.21.0` nos gates globais e transforma o gate da `v0.20.10 — Period Comparison 2.0` em gate histórico de feature. Isso elimina acoplamento de versões antigas ao `VERSION.txt`, healthcheck e identidade pública atuais. Revisão local; sem commit individual.

> **Hotfix local v0.20.10-r1:** normaliza como array o resultado da busca por migrations experimentais v0.18.10 em `scripts/setup.ps1`, evitando falha de `.Count` no Windows PowerShell quando o pipeline retorna exatamente um objeto. Revisão local; sem commit individual. Nenhuma alteração funcional, de schema, API ou interface.

> **Hotfix local v0.20.9-r5:** alinha o gate CSS histórico do `Professional Action Center 2.0` aos seletores que existem na implementação real. Revisões locais não geram commit/push; somente a versão funcional aprovada integralmente é versionada no Git. Nenhuma alteração funcional, de schema, API ou interface.

> **Hotfix v0.20.9-r4:** alinha o gate histórico do `Professional Action Center 2.0` ao rótulo real da interface (`Nota interna`) em vez de exigir o texto inexistente `Registrar nota`. Nenhuma alteração funcional, de schema, API ou interface.

> **Hotfix v0.20.9-r3:** corrige o gate histórico do `Professional Action Center 2.0`, removendo a exigência de um token de versão inexistente (`HP_ACTION_CENTER_V0208`) e mantendo validações semânticas da UI, ações, CSS, chat e documentação. Nenhuma alteração funcional, de schema, API ou interface.

> **Hotfix v0.20.9-r2:** estabiliza os gates históricos de `v0.20.7` e `v0.20.8`. Gates de versões concluídas passam a validar somente a presença e documentação de suas features, sem exigir `VERSION.txt`, Swagger, healthcheck ou cache da versão funcional corrente. A validação de identidade da versão atual permanece responsabilidade exclusiva do gate atual. Nenhuma alteração funcional, de schema, API ou interface.

> **Hotfix v0.20.9-r1:** corrige o bloco de gates da v0.20.9 em `TESTAR.ps1` que havia sido persistido com sequências `\\n` literais em vez de quebras de linha reais. Nenhuma alteração funcional, de schema, API ou interface.

> **Hotfix v0.20.8-r1:** desacopla o gate histórico de identidade/PWA da versão específica de cache. O teste passa a validar a presença versionada de manifest, favicon e apple-touch-icon sem exigir `?v=0.20.7`, evitando regressão a cada nova versão pública. Nenhuma alteração funcional, de schema ou de versão pública.

> **Hotfix v0.19.30-r3:** remove dependências restantes do antigo Mobile Action Hub (`mobile-now-hub`/`mobileNow*`) nos gates históricos e no frontend. Os testes passam a validar `Hoje em um olhar` e suas ações atuais; CSS/listeners mortos foram removidos. Nenhuma alteração funcional ou de schema.

> **Hotfix v0.19.30-r2:** remove a dependência dos gates históricos de `mobile-home-glance`/`glance-chip`, componentes aposentados pela nova Home `Hoje em um olhar`. Os testes agora validam as seis ações essenciais e o grid responsivo atual; CSS morto do resumo legado também foi removido. Nenhuma alteração funcional ou de schema.

> **Hotfix v0.19.30-r1:** sincroniza o gate histórico `Home Daily Athlete` com a Home simplificada da v0.19.30. O check-in continua funcional pelo card `athleteHome2Body`; foram removidas apenas dependências de teste e listener morto do antigo `dailyReadinessButton`. Nenhuma alteração funcional ou de schema.

> **Hotfix v0.19.29-r1:** sincroniza o gate histórico `AESYN Identity & Patient Navigation Foundation` com a versão pública corrente `0.19.29`. O gate ainda procurava `HP_MVP_VERSION='0.19.28'`, embora a aplicação já anunciasse corretamente `0.19.29`. Nenhuma alteração funcional ou de schema.

> **Hotfix v0.19.25-r2:** corrige o gate `[1908/1914]` para validar `EmailPrincipal`, `Confirmado` e `PodeSolicitarConfirmacao` no contrato `EmailAccountStatusResponse`, onde esses campos realmente são declarados, em vez de exigir seus nomes literais dentro do controller. Nenhuma alteração funcional ou de schema.

> **Hotfix v0.19.25-r1:** `TESTAR.ps1` deve permanecer em **UTF-8 com BOM** para compatibilidade com Windows PowerShell 5.1. O `PREPARAR.ps1` valida essa condição para impedir regressões de encoding.


> **Versão-base deste roadmap:** v0.22.6 — Evening Reflection 2.0

> Hotfix `v0.19.44-r1`: sincronizados os gates de versão corrente do `TESTAR.ps1` para `0.19.44`, sem alteração funcional.
> Hotfix `v0.19.44-r3`: sincronizado o gate de roadmap do fechamento da Lista 03 com o status concluído da v0.19.44 e com a próxima fase v0.20.0, sem alteração funcional.
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
9. `README.md`, `ROADMAP.md`, `CHANGELOG.md`, `PREPARAR.ps1`, `RODAR.ps1` e `TESTAR.ps1` fazem parte da entrega e devem acompanhar a evolução.
10. o roadmap é vivo: quando a realidade ensinar algo melhor, replanejar explicitamente sem apagar o histórico.

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


## Direção mestre definida em 2026-09-29

O AESYN passa a ser tratado como uma **plataforma de acompanhamento humano, saúde e performance centrada em medicina do esporte**. A evolução deixa de ser orientada principalmente por CRUDs e passa a ser orientada por **momentos de produto e necessidades humanas observáveis**.

### North Star

> **O AESYN precisa conhecer o atleta melhor a cada dia — com consentimento, contexto e utilidade.**

### Pilares

- Human Profile;
- AESYN Daily;
- AESYN Professional;
- AESYN Training;
- AESYN Nutrition;
- AESYN Recovery;
- AESYN Explore;
- AESYN Progress;
- AESYN Intelligence.

### Momentos de produto que orientam decisões

- **AESYN Morning:** entender o dia em segundos.
- **AESYN Training:** executar, registrar e compreender a evolução da sessão.
- **AESYN Progress:** perceber mudanças no corpo e performance.
- **Professional Morning:** saber quem merece atenção e por quê.
- **Patient Review:** compreender semanas de evolução rapidamente.
- **Consultation:** chegar à consulta com contexto longitudinal organizado.
- **Explore:** conseguir começar uma atividade ou esporte sem depender de já conhecer o caminho.

### Regra de autonomia guiada

O produto deve separar claramente:

- **Preciso fazer:** plano/orientação profissional.
- **Quero fazer:** interesses pessoais e modalidades desejadas.
- **Posso fazer hoje:** contexto real de tempo, local, equipamento, experiência e condição relatada.

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

- página de configurações consolidada;
- foto de perfil com redução local e armazenamento por conta;
- dados básicos;
- alteração de senha;
- preferência de tema persistida;
- área de privacidade e segurança com sessão atual;
- preferências de notificações;
- IP, user-agent e expiração da sessão atual;
- logout permanece global por `SecurityStamp`; inventário multi-dispositivo fica reservado para quando houver sessões persistidas por dispositivo.

### Gate
Conta consegue atualizar nome, senha, foto, tema e notificações; a imagem é reduzida antes do envio e não vai para auditoria; a tela informa a sessão atual sem criar dependência da VPS/produção.

**Status:** implementado na v0.19.28; validar localmente com PREPARAR → RODAR → TESTAR antes de qualquer deploy.

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

### Gate
Manifest e ícones válidos; identidade instalada como **AESYN Performance**; modo standalone com launch experience; theme-color acompanha claro/escuro; primeiro acesso permite escolher tema e persiste a preferência; superfícies principais do paciente, login, modais e componentes legados permanecem legíveis em dark mode.

**Status:** implementado na v0.19.29; validar localmente com PREPARAR → RODAR → TESTAR antes de qualquer deploy.

## v0.19.30 — Patient Home Cleanup

- corrigir o CSS do “Hoje em um olhar”;
- simplificar Home do paciente;
- Home deve responder **“o que eu preciso fazer hoje?”**;
- priorizar check-in, treino, alimentação, hidratação, mensagens e pendências;
- remover excesso de indicadores técnicos da Home;
- estados vazios, loading, erro e retry.

**Status:** implementado na v0.19.30; validar localmente com PREPARAR → RODAR → TESTAR antes de qualquer deploy. A Home principal agora concentra seis ações essenciais e retira blocos duplicados da primeira leitura; análises esportivas permanecem recolhidas até a evolução dedicada da v0.19.31.

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

**Status:** implementado na v0.19.31. A Home diária permanece simples e a nova área Dados para Atletas concentra métricas técnicas, tendência corporal, recuperação, carga/performance, composição corporal e consistência, com leitura responsável e atalhos para o contexto original.

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

**Status:** implementado na v0.19.32. O acesso profissional de Nutrição agora expõe criação e edição real de dietas, refeições, substituições/equivalências, metas nutricionais, catálogo CRUD de alimentos, modelos reutilizáveis, atribuição, duplicação/progressão, arquivamento sem apagar histórico e fluxo de revisão/publicação. Sem migration nova.

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

**Status:** implementado na v0.19.33. O profissional pode criar, editar, duplicar, arquivar, reativar e desvincular um plano da rotina ativa sem apagar o histórico. A relação um-para-muitos já existente passa a ser exposta de forma explícita: o paciente pode ter zero, um ou vários planos simultâneos (por exemplo força, cardio e recuperação), e o portal lista todos os planos ativos com início livre das sessões publicadas. Sem migration nova.

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

**Status:** implementado na v0.19.34. A biblioteca de arquivos passa a existir tanto no perfil profissional quanto no portal do paciente, com upload mobile de PDF/JPEG/PNG/WebP, câmera/galeria, limite e validação por assinatura do arquivo, categoria, descrição, tags, pesquisa, download autenticado, remoção lógica e auditoria de upload/download/remoção. O arquivo pode ser referenciado no chat por uma mensagem contextual sem tornar o binário público. Nesta fase os binários e o índice ficam em `App_Data/patient-files`, seguindo a mesma estratégia local protegida já usada pelas fotos de evolução; produção deve manter esse diretório em volume persistente. Sem migration nova.

## v0.19.35 — Chat Reliability & Context Foundation

- corrigir definitivamente autorização do chat;
- mensagens não lidas;
- badge;
- status enviada/entregue/lida quando tecnicamente aplicável;
- anexos;
- foto/PDF/arquivo;
- tratamento de erro/retry;
- base para contexto de treino, exercício, refeição e exame.

**Status:** implementado na v0.19.35. O chat reutiliza as notificações internas como confirmação de entrega/leitura, expõe contador de não lidas e badge, marca a conversa como lida ao abri-la, preserva a mensagem em falhas de envio e oferece retry em falhas de carregamento. Referências contextuais leves passam a suportar Arquivo, Treino, Exercício, Refeição e Exame; a biblioteca de arquivos já envia referência estruturada e o chat renderiza o contexto separadamente do texto. A autorização continua isolada por paciente, profissional e organização. Sem migration nova.

## v0.19.36 — Administrator Professional Impersonation

- administrador simular acesso do profissional;
- banner persistente indicando impersonação;
- botão para sair da simulação;
- auditoria de quem iniciou/finalizou;
- nenhuma impersonação silenciosa;
- impedir ações incompatíveis com a política de segurança.

**Status:** implementado na v0.19.36. O administrador pode iniciar a simulação pela tela Equipe apenas para Médico, Nutricionista ou Personal ativos da mesma organização. O token carrega identidade e `SecurityStamp` do administrador de origem, o banner permanece visível em toda a sessão e a saída devolve uma sessão administrativa nova. A simulação é deliberadamente **somente leitura**: requisições de mutação são bloqueadas para impedir alterações reais e auditorias atribuídas indevidamente ao profissional simulado. Início e fim são registrados no AuditLog com o administrador como ator. Sem migration nova.

## v0.19.37 — Patient First Access & Tutorials

- onboarding de primeiro acesso;
- tutorial do check-in diário;
- auxílio do primeiro treino;
- explicação básica de alimentação/plano;
- introdução ao chat;
- tutoriais podem ser refeitos depois;
- ajuda contextual por `?` ou equivalente.

**Status:** implementado na v0.19.37. O primeiro acesso do paciente consulta estado persistente na conta e abre uma trilha guiada com Home, check-in, primeiro treino, plano alimentar e chat. A conclusão é persistida em `Usuarios.OnboardingPacienteConcluidoEmUtc` e auditada. O guia pode ser fechado sem marcar conclusão, pode ser refeito pelo botão `?` ou pelo Meu perfil e inclui atalhos práticos para experimentar as áreas apresentadas.

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

**Status:** implementado na v0.19.38. O AESYN captura `beforeinstallprompt` no Chrome/Android, oferece CTA no acesso público e no menu móvel, orienta Safari/iPhone e detecta modo standalone. Service Worker cacheia apenas shell/assets estáticos, excluindo `/api/`. `visualViewport`, safe areas, touch targets de 44px, modais responsivos ao teclado e histórico próprio do portal melhoram a ergonomia mobile e o botão físico Voltar do Android. Desenvolvimento continua isolado de produção.

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

**Status:** implementado na v0.19.39. O AESYN registra inscrições Web Push por dispositivo usando `UsuariosTokens`/provider `AESYN.WebPush`, respeita preferências por categoria, usa VAPID somente quando habilitado e mantém Development sem tráfego push real. O Service Worker recebe push com o PWA/navegador fechado, exibe notificação e trata clique/deep link. Chat, check-in do paciente, treino concluído/divergente, novos arquivos e publicação de treino/dieta disparam push transacional. Chaves VAPID permanecem exclusivamente em segredo de ambiente na VPS.

**Hotfix v0.19.39-r1:** corrige a ausência do método `NotificarNovoArquivo` no pacote inicial da v0.19.39, restaurando compilação e o disparo de push após upload de arquivo sem alterar banco ou versão funcional.

## v0.19.40 — In-App Notification Center

- central interna;
- lidas/não lidas;
- badges;
- prioridade;
- histórico;
- preferências por categoria;
- deep link para origem da notificação.

**Status:** implementado na v0.19.40. A central interna agora permite alternar entre notificações ativas, não lidas e histórico (incluindo itens encerrados pela origem), filtrar por prioridade e categoria, consultar resumo por severidade, marcar itens como não lidos e manter badge global coerente. Preferências de mensagens, atualizações do plano, lembretes e check-ins podem ser ajustadas dentro da própria central usando as configurações já persistidas na conta. Deep links do paciente passam a cobrir também chat, solicitações e arquivos, enquanto o profissional continua sendo levado para agenda, pendências, pacientes ou origem específica. Nenhuma migration foi necessária.

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

**Status:** implementado na v0.19.41. Foi criado um sistema visual reutilizável para estados de carregamento, vazio, erro, sucesso e retry, com componentes acessíveis e responsivos. O loading global passou a usar o mesmo contrato visual; ações assíncronas ganharam helper com estado pendente; confirmações críticas receberam modal próprio, preparado para substituir `window.confirm` gradualmente sem quebrar fluxos existentes; dark mode, mobile e `prefers-reduced-motion` foram cobertos. A versão não altera schema.

## v0.19.42 — Offline & Poor Connection Resilience

- detectar ausência de internet;
- informar sem parecer falha do aplicativo;
- preservar formulários digitados;
- retry;
- reconexão;
- fila apenas para ações em que seja seguro;
- evitar duplicidade de submissões.

**Status:** implementado na v0.19.42. O frontend passa a distinguir offline de conexão lenta, apresenta banner persistente sem tratar oscilação de rede como falha interna e normaliza erros de rede nas chamadas da API. Rascunhos explicitamente marcados com `data-hp-draft` são preservados apenas em `sessionStorage` — evitando persistência permanente de conteúdo sensível — e o chat já usa esse mecanismo. Ações idempotentes/seguras podem optar explicitamente por fila offline deduplicada; a leitura de notificações usa esse contrato e é sincronizada na reconexão. O sistema também bloqueia duplicidade por chave com `hpRunOnceV01942`, mantém retry explícito e não armazena respostas de `/api/` no Service Worker. Nenhuma migration foi necessária.

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

**Status:** implementado na v0.19.43. A conta passa a registrar versão e data/hora de aceite dos Termos de Uso e da Política de Privacidade, com documentos operacionais versionados e histórico em AuditLog. Configurações ganhou painel de privacidade, consulta da própria trilha de auditoria, solicitação de exclusão para análise e desativação de conta protegida por senha + confirmação explícita. A desativação revoga inscrições Web Push e o SecurityStamp, sem apagar silenciosamente histórico clínico. Foi adicionada a migration `V01943AuditPrivacyConsent` e o checklist `docs/LGPD-PRIVACY-CHECKLIST.md`, que explicita pendências jurídicas/operacionais antes da produção comercial. O smoke gate também passa a cobrar auditoria nos fluxos críticos de treino, nutrição, arquivos, pacientes, impersonação e arquivamentos.

## ✅ v0.19.44 — Product Flow Gate / Lista 03 Closure — CONCLUÍDA

### Fluxo paciente obrigatório
primeiro acesso → tutorial → check-in → visualizar plano → escolher/iniciar treino permitido → executar → registrar alimentação → conversar → enviar arquivo → receber atualização profissional.

### Fluxo profissional obrigatório
login → localizar paciente → revisar contexto → criar/alterar treino → criar/alterar dieta → publicar → conversar → revisar arquivos/exames → receber alertas → acompanhar evolução.

### Gate
Nenhum desses fluxos pode depender de atalho de desenvolvedor, banco manual, Swagger ou intervenção técnica.

**Status:** implementado na v0.19.44. O smoke gate passa a verificar transversalmente que os pontos de entrada do paciente e do profissional continuam conectados às funcionalidades construídas entre v0.19.24 e v0.19.43. A homologação manual ponta a ponta está documentada em `docs/PRODUCT-FLOW-GATE.md`, e o encerramento técnico da Lista 03 em `docs/LISTA-03-CLOSURE.md`. O `TESTAR.ps1` permanece não destrutivo; por isso, operações que necessariamente criam dados são verificadas estruturalmente no smoke test e executadas manualmente na homologação controlada antes de produção comercial.

### Próxima fase
**v0.20.0 — Professional Dashboard 2.0.** A partir daqui, novas funcionalidades entram na fase Professional Workspace 2.0; bugs encontrados no fechamento usam `v0.19.44-rN` até o gate ficar verde.

---

# 4. v0.20.x — Professional Workspace 2.0

Objetivo: transformar a área profissional em uma central de trabalho para dezenas/centenas de pacientes.

## ✅ v0.20.0 — Professional Dashboard 2.0 — CONCLUÍDA
- visão executiva unificada no topo do dashboard;
- consultas do dia, pacientes em atenção, pendências, mensagens não lidas, follow-ups e revisões clínicas;
- pacientes recentes preservados;
- ações rápidas para novo paciente, Treino & Nutrição, Agenda, Pendências e Central do Dia;
- dados consolidados a partir de Central do Dia, Pendências e Notificações, sem criar fonte paralela;
- responsivo e compatível com dark mode;
- sem migration nova.

**Próxima etapa:** `v0.20.2 — Patient Attention Queue 2.0`.

## ✅ v0.20.1 — Patient Search & Advanced Filters — CONCLUÍDA
- busca global por nome, CPF, e-mail e telefone;
- filtros por status;
- marcadores operacionais calculados (tags personalizadas seguem na v0.20.5);
- responsável derivado da consulta mais recente;
- aderência baseada nos check-ins recentes;
- última interação;
- próxima revisão/contato;
- ordenação profissional e UX responsiva.

## ✅ v0.20.2 — Patient Attention Queue 2.0 — CONCLUÍDA
- fila profissional dedicada sobre a Central de Atenção existente;
- classificação visual por prioridade, atenção e observação;
- motivos derivados dos sinais já consolidados: check-in irregular, dor, sono, adesão, treino, alimentação, pendências e follow-up;
- score mínimo configurável;
- opção de incluir/excluir observações;
- opção de priorizar follow-up vencido;
- preferências locais por navegador, sem alterar conduta ou dados clínicos;
- navegação direta para o paciente;
- responsivo e sem migration nova.

**Próxima etapa:** `v0.20.3 — Patient Status Management`.

## ✅ v0.20.4 — Professional Internal Notes — CONCLUÍDA

- Adiciona notas internas privadas da equipe profissional por paciente.
- Registra autor, categoria, conteúdo, fixação, edição e arquivamento lógico.
- Mantém as notas fora do portal do paciente e protege a API por papéis da equipe.
- Todas as mutações geram AuditLog (`INTERNAL_NOTE_CREATED`, `INTERNAL_NOTE_UPDATED`, `INTERNAL_NOTE_PIN_CHANGED`, `INTERNAL_NOTE_ARCHIVED`).
- Migration `V0204ProfessionalInternalNotes` cria persistência e índices por organização/paciente.

**Próxima etapa:** `v0.20.5 — Tags & Segmentation`.

## ✅ v0.20.3 — Patient Status Management — CONCLUÍDA
- status persistente: ativo, pausado, aguardando avaliação e encerrado;
- motivo/contexto opcional da alteração;
- auditoria de mudança de status;
- filtros e identificação visual na carteira;
- acesso à alteração pela lista e pelo perfil do paciente;
- preservação de histórico clínico e compatibilidade com o campo legado `Ativo`;
- migration e índice por organização/status.

**Próxima etapa:** `v0.20.4 — Professional Internal Notes`.

## ✅ v0.20.5 — Tags & Segmentation — CONCLUÍDA
- tags personalizadas por paciente;
- persistência no prontuário administrativo do paciente;
- edição direta pela carteira e pelo perfil profissional;
- filtro por tag na busca avançada;
- agrupamentos operacionais derivados das tags cadastradas;
- auditoria de alterações (`PATIENT_TAGS_CHANGED`);
- limite de 20 tags por paciente, com normalização e deduplicação;
- UX responsiva e compatível com dark mode;
- buscas salvas permanecem planejadas para etapa futura.

**Próxima etapa:** `v0.20.6 — Patient Overview 2.0`.

## ✅ v0.20.6 — Patient Overview 2.0 — CONCLUÍDA

**Objetivo humano:** ao abrir um paciente, o profissional deve compreender em poucos segundos como aquela pessoa está.

Entregue:
- cockpit de leitura rápida antes do detalhamento do prontuário;
- objetivo atual, status e tags do paciente;
- prontidão diária, treino dos últimos 30 dias, adesão e peso/tendência;
- identificação de treino e plano nutricional vigentes;
- último evento longitudinal;
- motivo operacional de revisão com navegação para o domínio correspondente;
- estados seguros para dados ausentes;
- responsividade mobile e dark mode;
- linguagem explícita de segurança: síntese não diagnostica, não prescreve e não altera planos;
- reaproveitamento dos dados já carregados, sem criar endpoint ou persistência paralelos.

O gate anterior passou com `2190/2190`, removendo o bloqueio operacional antes desta evolução.

**Próxima etapa:** `v0.20.7 — Attention Reasons 2.0`.

## ✅ v0.20.7 — Attention Reasons 2.0 — CONCLUÍDA

**Objetivo humano:** o profissional entende imediatamente por que uma pessoa entrou em atenção, sem precisar caçar contexto em diversas telas.

Entregue:
- motivos estruturados retornados pela Central do Dia;
- deduplicação de sinais equivalentes;
- separação entre prioridade, observação e pendência operacional;
- origem, período e detalhe visíveis em cada motivo;
- navegação direta para treino, alimentação, diário, timeline, pendências ou follow-ups;
- fallback seguro para sinais legados;
- preservação dos filtros configuráveis da Patient Attention Queue 2.0;
- responsividade e dark mode;
- nenhuma inferência diagnóstica ou alteração automática de conduta.

**Próxima etapa:** `v0.20.8 — Professional Action Center 2.0`.

## ✅ v0.20.8 — Professional Action Center 2.0 — CONCLUÍDA

**Objetivo humano:** depois de entender o motivo de atenção, o profissional consegue executar a próxima ação adequada com o mínimo de atrito.

Entregue:
- Action Center contextual a partir da Central de Atenção;
- Action Center disponível também no Patient Overview;
- abertura direta do chat profissional;
- solicitação explícita de check-in enviada pelo chat após confirmação;
- acesso às notas internas profissionais;
- revisão direta de treino e nutrição;
- registro de follow-up;
- criação de pendência;
- agendamento de retorno;
- acesso preservado ao prontuário completo;
- nenhuma automação de conduta clínica;
- nenhuma migration nova.

**Próxima etapa:** `v0.20.9 — Clinical & Sports Snapshot 2.0`.

## ✅ v0.20.9 — Clinical & Sports Snapshot 2.0 — CONCLUÍDA

**Objetivo humano:** permitir que o profissional enxergue corpo, performance, recuperação, nutrição e contexto longitudinal em uma leitura compacta antes de aprofundar o prontuário.

Entregue:
- snapshot clínico-esportivo curto e explicável;
- corpo com peso, composição disponível e tendência vs avaliação anterior;
- performance com sessões dos últimos 30 dias e carga interna recente;
- recuperação com prontidão/tendência e sono médio quando disponível;
- adesão nutricional e adesão ao protocolo;
- comparação semanal quando já existe base longitudinal;
- últimos eventos da timeline;
- navegação direta para o domínio de origem;
- nenhuma migration, diagnóstico ou prescrição automatizada.

**Próxima etapa:** `v0.20.10 — Period Comparison 2.0`.

## ✅ v0.20.10 — Period Comparison 2.0 — CONCLUÍDA

**Objetivo humano:** permitir que o profissional compare períodos sem interpretar gráficos isolados ou perder contexto temporal.

Escopo-alvo:
- semana atual vs anterior;
- 30 dias atuais vs 30 dias anteriores quando houver base;
- início vs atual em indicadores corporais disponíveis;
- treino, recuperação, adesão e corpo em comparações separadas;
- diferenças absolutas e percentuais apenas quando matematicamente adequadas;
- origem e janela temporal explícitas;
- dados insuficientes tratados sem inferências artificiais.

**Entregue:** comparação semanal existente, janela móvel de 30 dias vs 30 dias anteriores usando execuções/diário, primeira avaliação vs atual para indicadores corporais disponíveis, datas das janelas explícitas, deltas absolutos e percentuais quando matematicamente adequados e estados de base insuficiente sem inferência.

**Próxima etapa:** `v0.21.0 — Human Profile Foundation`.

---

# 5. v0.21.x — Human Profile

## ✅ v0.21.0 — Human Profile Foundation — CONCLUÍDA

A pessoa passa a ser a unidade central do domínio longitudinal.

- Multi-Goal Engine;
- Sports Profile;
- Lifestyle Context;
- Physical Capabilities;
- Limitations & Restrictions;
- visão profissional;
- visão do atleta.

## ✅ v0.21.1 — Multi-Goal Engine — CONCLUÍDA

**Entregue na v0.21.1:**
- consolidação de objetivos simultâneos de ciclo, treino, nutrição e intake;
- deduplicação textual mantendo todas as fontes;
- hierarquia operacional Principal / Complementar / Acompanhado;
- domínio descritivo do objetivo;
- ausência de objetivo tratada explicitamente;
- sem score clínico, sem diagnóstico e sem inferência de prioridade médica;
- integração direta ao Human Profile sem nova fonte de verdade.

**Próxima etapa:** `v0.21.2 — Sports Identity Foundation`.

## ✅ v0.21.2 — Sports Identity Foundation — CONCLUÍDA

Estruturar identidade esportiva da pessoa: modalidades atuais e históricas, nível de experiência, frequência, contexto preferido e relação com objetivos ativos.


**Entregue na v0.21.2:**
- modalidades atuais consolidadas;
- nível de experiência registrado;
- frequência semanal;
- contexto preferido de prática;
- histórico esportivo já existente;
- relação explícita com objetivos ativos;
- completude contextual sem score de aptidão;
- identidade esportiva tratada como longitudinal e mutável.

**Próxima etapa:** `v0.21.3 — Life Context Foundation`.


## ✅ v0.21.3 — Life Context Foundation — CONCLUÍDA

**Entregue na v0.21.3:**
- rotina e contexto profissional já registrados;
- disponibilidade para treino/prática;
- logística e local;
- equipamentos/recursos;
- preferências de contexto;
- restrições e limitações registradas;
- leitura Preciso fazer / Quero fazer / Posso fazer hoje;
- sem prescrição automática e sem inferência clínica.

**Próxima etapa:** `v0.21.4 — Human Profile Synthesis 2.0`.

## ✅ v0.21.4 — Human Profile Synthesis 2.0 — CONCLUÍDA

Consolidar objetivos, identidade esportiva, contexto de vida, corpo e acompanhamento em uma síntese profissional compacta e acionável.



**Entregue na v0.21.4:**
- síntese profissional em seis dimensões;
- objetivos e prioridade operacional;
- identidade esportiva;
- medidas corporais recentes;
- planos ativos;
- contexto de vida;
- sinais recentes disponíveis;
- orientação para consulta/acompanhamento;
- sem diagnóstico, score de risco ou fonte paralela.

**Próxima etapa:** `v0.22.0 — AESYN Daily Foundation`.


# 6. v0.22.x — AESYN Daily

## ✅ v0.22.0 — AESYN Daily Foundation — CONCLUÍDA

**Entregue na v0.22.0:**
- visão diária em quatro dimensões;
- Preciso fazer;
- Quero fazer;
- Posso fazer hoje;
- Como estou;
- próxima ação contextual;
- integração com Human Profile Synthesis;
- sem diagnóstico ou prescrição automática.

**Próxima etapa:** `v0.22.1 — Morning Check-in 2.0`.

## ✅ v0.22.1 — Morning Check-in 2.0 — CONCLUÍDA

Evoluir a coleta rápida de sono, energia, dor/desconforto, estresse, humor e disponibilidade para alimentar o Daily com contexto do próprio dia.


- Today 3.0;
- Morning Check-in;
- Daily Readiness contextual;
- Today's Plan;
- Action Hub;
- experiência de 30 segundos;
- Evening Reflection;
- histórico diário;
- sinais úteis ao profissional;
- premium UX.


**Entregue na v0.22.1:**
- fluxo mobile-first de aproximadamente 30 segundos;
- sono em horas;
- qualidade do sono;
- energia;
- dor corporal;
- disposição;
- recuperação percebida;
- escalas com contexto visual;
- atualização do check-in do mesmo dia;
- reaproveitamento do contrato de prontidão existente;
- linguagem sem julgamento e sem diagnóstico.

**Próxima etapa:** `v0.22.2 — Daily Readiness Context 2.0`.


## ✅ v0.22.2 — Daily Readiness Context 2.0 — CONCLUÍDA

**Entregue na v0.22.2:**
- leitura explicável dos fatores do check-in;
- horas e qualidade do sono;
- energia;
- dor corporal;
- disposição;
- recuperação;
- fatores favoráveis e fatores de atenção;
- resumo contextual sem score clínico opaco;
- nenhuma decisão automática de treino/conduta.

**Próxima etapa:** `v0.22.3 — Today's Plan 2.0`.

## ✅ v0.22.3 — Today's Plan 2.0 — CONCLUÍDA

Transformar plano profissional, readiness e contexto do dia em uma visão prática do que está planejado para hoje, preservando autonomia e decisão profissional.



**Entregue na v0.22.3:**
- plano de hoje consolidado;
- treino ativo;
- nutrição ativa;
- objetivo principal;
- disponibilidade, contexto e recursos;
- integração com readiness;
- orientação contextual de execução;
- nenhum ajuste automático de prescrição.

**Próxima etapa:** `v0.22.4 — Action Hub 2.0`.


## ✅ v0.22.4 — Action Hub 2.0 — CONCLUÍDA

**Entregue na v0.22.4:**
- hub de ações do dia;
- iniciar treino;
- abrir alimentação;
- fazer/atualizar check-in;
- registrar hidratação;
- registrar peso;
- rever contexto/readiness;
- ações indisponíveis explicitamente desabilitadas;
- foco mobile-first e redução de navegação.

**Próxima etapa:** `v0.22.5 — Daily 30 Seconds Experience 2.0`.

## ✅ v0.22.5 — Daily 30 Seconds Experience 2.0 — CONCLUÍDA

Consolidar o fluxo diário essencial em uma experiência curta, clara e progressiva, priorizando check-in, contexto, plano e próxima ação em aproximadamente 30 segundos.



**Entregue na v0.22.5:**
- fluxo check-in → contexto → plano → próxima ação;
- sequência visual de quatro etapas;
- estado concluído/atual/disponível;
- próximo passo destacado;
- integração com readiness e Action Hub;
- ergonomia mobile-first;
- experiência orientada a aproximadamente 30 segundos;
- sem penalidade, julgamento ou diagnóstico.

**Próxima etapa:** `v0.22.6 — Evening Reflection 2.0`.


## ✅ v0.22.6 — Evening Reflection 2.0 — CONCLUÍDA

**Entregue na v0.22.6:**
- reflexão curta de fim do dia;
- percepção final 0–10;
- execução percebida do plano;
- dificuldade percebida;
- energia ao término;
- aprendizado do dia;
- observação opcional;
- reaproveitamento do fechamento diário existente;
- linguagem sem punição ou julgamento.

**Próxima etapa:** `v0.22.7 — Daily History 2.0`.

## ⏭ v0.22.7 — Daily History 2.0

Criar uma leitura histórica coerente dos dias recentes, conectando check-in, plano, execução e reflexão para mostrar padrões sem transformar histórico em score de valor pessoal.


# 7. v0.23.x — Sports & Movement Library

Estrutura-alvo: `Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão`.

Fundação: musculação, caminhada, corrida, calistenia, mobilidade, condicionamento e ciclismo.

Inclui taxonomia, instruções, mídia, progressões, regressões, equipamentos, ambientes, biblioteca AESYN, biblioteca profissional e templates.

# 8. v0.24.x — AESYN Explore

Área de autonomia guiada para quem quer se movimentar e ainda não sabe por onde começar.

- Start a Sport;
- Beginner Journeys;
- Home Workout;
- Quick Movement;
- Travel Mode;
- Outdoor Mode;
- Learn Fundamentals;
- Sports Starter Packs;
- Interest Engine.

# 9. v0.25.x — Sports Expansion I

Futebol, futsal, basquete, vôlei, tênis e beach tennis com fundamentos, capacidades, sessões e preparação física específica.

# 10. v0.26.x — Sports Expansion II

Natação, triathlon, artes marciais, remo, trekking, modalidades recreativas e expansão guiada pelo uso real.

# 11. v0.27.x — Workout Intelligence 3.0

- séries, repetições, carga, RPE/RIR, descanso, tempo e cadência;
- técnicas avançadas;
- progressão/regressão;
- microciclo, mesociclo, bloco e deload;
- templates;
- prescrito vs realizado.

# 12. v0.28.x — Athlete Performance Passport

Recordes, cargas, tempos, provas, testes, habilidades, marcos e evolução de performance.

# 13. v0.29.x — Progress Intelligence

Peso, medidas, composição corporal, fotos, força, cardio, mobilidade, consistência, recordes e Progress Story.

# 14. v0.30.x — Sports Nutrition 3.0

Calorias, macros, refeições, porções, equivalências, substituições, alternativas, contexto esportivo e publicação/versionamento profissional.

# 15. v0.31.x — Recovery Intelligence

Sono, dor/desconforto, fadiga, readiness, estresse percebido e recuperação pós-treino, com relações explicáveis entre os próprios dados da pessoa.

# 16. v0.32.x — Human Timeline

Linha temporal única e coerente dos eventos relevantes do humano.

# 17. v0.33.x — AESYN Connect

Chat, anexos, contexto de treino/refeição, busca, histórico, confiabilidade e auditoria.

# 18. v0.34.x — Professional Attention Intelligence

Pergunta central: **quem precisa de mim hoje, e por quê?**

Sinais possíveis: queda de adesão, ausência, prontidão baixa recorrente, desconforto recorrente, divergência de plano, mensagem pendente, estagnação e interrupção de treino.

# 19. v0.35.x — Consultation Mode

Resumo longitudinal desde a última consulta: corpo, sono, treino, adesão, alimentação, desconfortos, mensagens, mudanças e recordes.

# 20. v0.36.x — Goal Intelligence

Múltiplos objetivos, prioridade, prazo, checkpoints, progresso, marcos e histórico.

# 21. v0.37.x — AESYN Progression

Gamificação positiva: XP, níveis, streaks, conquistas, milestones e metas semanais, sem punição por imperfeição.

# 22. v0.38.x — Challenges & Exploration

Desafios pessoais, de movimento, consistência, mobilidade e exploração esportiva.

# 23. v0.39.x — Adaptive Athlete Experience

Experiência contextual conforme horário, plano, estado atual, atividades concluídas e histórico recente.

# 24. v0.40.x — Human Patterns

Detecção de padrões explicáveis dentro do histórico da própria pessoa, sem diagnóstico automatizado.

# 25. v0.41.x — AESYN Insights

Insights rastreáveis sobre consistência, sono, carga, adesão, progressão e tendências.

# 26. v0.42.x — Return to Sport

Jornada orientada por profissional para pausa, limitação, progressão, checkpoints e retorno.

# 27. v0.43.x — Sports Calendar

Provas, jogos, campeonatos, viagens, avaliações, blocos de treino e deloads.

# 28. v0.44.x — Athlete Journal

Registro subjetivo livre conectado à timeline.

# 29. v0.45.x — Support Circles

Pequenos círculos opcionais de apoio, sem feed infinito.

# 30. v0.46.x — Design System 2.0

Tipografia, componentes, hierarquia, animações, skeletons, estados, contraste e acessibilidade.

# 31. v0.47.x — Mobile Native Feel

PWA com navegação, gestos, cache e performance de aplicativo.

# 32. v0.48.x — Smart Notifications

Notificações relevantes, configuráveis e sem spam.

# 33. v0.49.x — Trust & Security

Autenticação, autorização, sessão, auditoria, rate limiting, arquivos, consentimento, isolamento, backup, retenção e LGPD.

# 34. v0.50.x — Performance & Scale

Queries, índices, caching, jobs, paginação, imagens, compressão e observabilidade.

# 35. v0.51.x — Clinics & Teams

Profissional individual, clínica, equipe, papéis e permissões.

# 36. v0.52.x — Professional Collaboration / Shared Care

O antigo `v0.20.6 — Shared Care / Multiple Professionals` é **replanejado para cá**. A colaboração multiprofissional só entra depois de Human Profile, timeline e regras de permissão estarem maduras.

# 37. v0.53.x — Practice Intelligence

Analytics operacional para o profissional e sua equipe.

# 38. v0.54.x — First 5 Minutes

Onboarding excepcional de atleta e profissional.

# 39. v0.55.x — Accessibility & Inclusion

Leitor de tela, teclado, contraste, redução de movimento, linguagem clara e diferentes níveis de familiaridade digital.

# 40. v0.56.x–v0.59.x — Product Readiness

- Error Experience;
- Data Reliability;
- Backup & Disaster Recovery;
- Observability.

# 41. v0.60.x — AESYN Beta

Pausa deliberada de grandes features para jornadas completas: atleta novo, atleta recorrente, profissional, consulta, viagem, descoberta de esporte, desconforto, retorno e recuperação de falhas.

Classificação de pendências: P0 quebra sistema; P1 prejudica jornada; P2 UX; P3 acabamento.

# 42. v0.61.x–v0.64.x — Real World Program

Validação com uso real, feedback profissional, feedback de atletas e analytics de fricção respeitando privacidade.

# 43. v0.65.x–v0.69.x — Commercial Foundation & Data Portability

SaaS, branding profissional, convites, ciclo de conta e exportação dos próprios dados.

# 44. v0.70.x–v0.89.x — Learning Roadmap

Faixa deliberadamente aberta para aprendizados reais: novos esportes, wearables, Apple Health, Health Connect, integrações, relatórios, visualizações, performance e acessibilidade.

# 45. v0.90.x–v0.99.x — Release Candidate

- Feature Freeze;
- Athlete Journey Hardening;
- Professional Journey Hardening;
- Security Hardening;
- Performance Hardening;
- Mobile Hardening;
- Data Integrity Hardening;
- Accessibility Hardening;
- Production Simulation;
- Release Candidate.

# 46. AESYN 1.0


**Entregue na v0.21.0:**
- camada Human Profile no resumo profissional;
- identidade e status;
- objetivos existentes agregados de ciclo, anamnese e planos;
- esporte/movimento e frequência registrada;
- sono, estresse e hidratação;
- contexto clínico já registrado;
- completude informacional por dimensões, sem score clínico;
- navegação para as fontes originais;
- nenhuma duplicação de schema ou fonte de verdade.

**Próxima etapa:** `v0.21.1 — Multi-Goal Engine`.



**Entregue na v0.21.2:**
- modalidades atuais consolidadas;
- nível de experiência registrado;
- frequência semanal;
- contexto preferido de prática;
- histórico esportivo já existente;
- relação explícita com objetivos ativos;
- completude contextual sem score de aptidão;
- identidade esportiva tratada como longitudinal e mutável.

**Próxima etapa:** `v0.21.3 — Life Context Foundation`.



**Entregue na v0.22.1:**
- fluxo mobile-first de aproximadamente 30 segundos;
- sono em horas;
- qualidade do sono;
- energia;
- dor corporal;
- disposição;
- recuperação percebida;
- escalas com contexto visual;
- atualização do check-in do mesmo dia;
- reaproveitamento do contrato de prontidão existente;
- linguagem sem julgamento e sem diagnóstico.

**Próxima etapa:** `v0.22.2 — Daily Readiness Context 2.0`.



**Entregue na v0.22.3:**
- plano de hoje consolidado;
- treino ativo;
- nutrição ativa;
- objetivo principal;
- disponibilidade, contexto e recursos;
- integração com readiness;
- orientação contextual de execução;
- nenhum ajuste automático de prescrição.

**Próxima etapa:** `v0.22.4 — Action Hub 2.0`.



**Entregue na v0.22.5:**
- fluxo check-in → contexto → plano → próxima ação;
- sequência visual de quatro etapas;
- estado concluído/atual/disponível;
- próximo passo destacado;
- integração com readiness e Action Hub;
- ergonomia mobile-first;
- experiência orientada a aproximadamente 30 segundos;
- sem penalidade, julgamento ou diagnóstico.

**Próxima etapa:** `v0.22.6 — Evening Reflection 2.0`.


## Promessa ao atleta

- sei o que fazer hoje;
- consigo cuidar melhor de mim;
- consigo experimentar novas formas de me movimentar;
- consigo enxergar minha evolução;
- meu profissional consegue me acompanhar.

## Promessa ao profissional

- sei quem precisa de mim;
- entendo como meu paciente evolui;
- consigo prescrever e acompanhar profundamente;
- tenho contexto para decisões melhores;
- o sistema reduz trabalho em vez de criar trabalho.

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

### Hotfix v0.19.30-r4 — Patient Home Readiness Gate Cleanup
- Gate legado de `daily-readiness-card` aposentado.
- O check-in/prontidão da Home é representado pelo card essencial `athleteHome2Body` em `patient-today-overview`.
- Testes devem acompanhar a arquitetura atual da Home e não forçar componentes removidos por versões posteriores.

### Revisão v0.19.33-r1 — Multi-Plan Gate Semantic Sync
- Corrige falso negativo no gate `[1998/2006]` da interface profissional de múltiplos planos.
- A UI atual comunica corretamente que o paciente pode ficar **sem plano, ter um ou vários planos simultâneos**; o teste ainda exigia literalmente `zero, um ou vários`.
- O gate passa a validar a mensagem efetivamente exibida sem alterar a funcionalidade, o contrato da API, schema ou migrations.
- Mantém como obrigatórios o card/grid multi-plan e a indicação explícita de suporte a múltiplos planos.
- Versão funcional pública permanece `0.19.33`.



### Hotfix v0.19.35-r1 — Chat Persistence Gate Semantic Sync
- Gate histórico sincronizado com a persistência contextual introduzida na v0.19.35.
- `Observacoes` continua persistindo a mensagem, agora por meio de `MontarObservacoes`, que acrescenta referência estruturada quando houver contexto.
- Nenhuma regressão funcional ou mudança de schema.


### Hotfix v0.19.35-r2 — Patient Files/Chat Gate Semantic Sync
- Corrige falso negativo do gate histórico `[2015/2020]`, que ainda exigia o texto legado `Arquivo AESYN:`.
- A integração atual usa `Arquivo compartilhado:` e referência estruturada (`referenciaTipo`, `referenciaId`, `referenciaTitulo`) introduzida na v0.19.35.
- O teste passa a validar o contrato atual sem reintroduzir texto legado na interface.
- Nenhuma alteração funcional, schema ou migration; versão funcional pública permanece `0.19.35`.


## Revisões corretivas da base atual
- **v0.19.40-r1 — Notification Patient Link Gate Semantic Sync:** corrige gate legado de navegação de notificações do paciente para validar o resolvedor atual (`hpResolvePatientNotificationLink`) sem ressuscitar implementação antiga.

### Hotfix v0.19.41-r1 — UX State Gate Semantic Sync
- Gate do UX State System alinhado à implementação real: composição dinâmica no `app.js` e classes concretas no `app.css`.
- Nenhuma funcionalidade foi removida ou reintroduzida apenas para satisfazer teste textual.

### Hotfix v0.19.44-r2
- Sincronização residual dos gates legados de versão e identidade PWA para a versão pública 0.19.44.
- Mantém a Lista 03 em fase de fechamento sem reintroduzir comportamento antigo.
