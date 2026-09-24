# v0.19.37 — Patient First Access & Tutorials

- onboarding persistente no primeiro acesso do paciente;
- tutorial guiado de Home, check-in, primeiro treino, plano alimentar e chat;
- possibilidade de fechar e continuar depois sem marcar conclusão;
- tutorial pode ser refeito pelo botão de ajuda `?` e pelo Meu perfil;
- conclusão auditada e sincronizada entre dispositivos;
- migration `V01937OnboardingPaciente`;
- sem testes de desenvolvimento contra produção.

# v0.19.36 — Administrator Professional Impersonation

- Adiciona simulação explícita de Médico, Nutricionista e Personal a partir da gestão de Equipe.
- Sessão simulada recebe claims de origem administrativa e valida também o `SecurityStamp` do administrador.
- Banner persistente informa que a plataforma está em modo de simulação e permite retornar ao acesso administrativo.
- Simulação é somente leitura: mutações reais são bloqueadas para preservar segurança e autoria de auditoria.
- Início e encerramento são registrados no AuditLog com o administrador como ator.
- Sem migration nova e sem dependência de produção nos testes locais.
- TESTAR ampliado para 2050 gates.

## v0.19.35-r2 — Patient Files/Chat Gate Semantic Sync
- Corrige o gate legado de indexação de arquivo no chat para a semântica estruturada atual.
- Substitui a exigência textual `Arquivo AESYN:` por `Arquivo compartilhado:` e valida `referenciaTipo`, `referenciaId` e `referenciaTitulo`.
- Sem alteração funcional, de schema ou migration.

## v0.19.35-r1 — Chat Persistence Gate Semantic Sync

- Corrige falso negativo do gate legado de persistência do chat.
- O chat passou a persistir `Observacoes = observacoes`, após `MontarObservacoes(mensagem, referencia)`, para suportar contexto estruturado de arquivo/treino/exercício/refeição/exame.
- O smoke test agora valida a implementação atual (`MontarObservacoes`/`SepararObservacoes`) em vez de exigir o antigo `Observacoes = mensagem`.
- Sem alteração funcional, de banco ou migration.

# v0.19.35 — Chat Reliability & Context Foundation

- Consolidada leitura de mensagens via `NotificacaoInterna.LidaEmUtc`.
- Adicionado endpoint `GET /api/chat/nao-lidas` para badges sem abrir a conversa.
- Mensagens próprias expõem status Enviada/Entregue/Lida.
- Falha de carregamento oferece retry; falha de envio preserva o texto digitado.
- Chat passa a persistir referência contextual leve para Arquivo, Treino, Exercício, Refeição e Exame sem alterar schema.
- Patient Files envia referência estruturada ao chat.
- CSS mobile/dark e badges adicionados.
- TESTAR ampliado para 2035 gates.

# v0.19.34 — Patient Files Mobile 2.0

- Cria biblioteca protegida de arquivos do paciente no acesso profissional e no portal do paciente.
- Upload mobile de PDF, JPEG, PNG e WebP, com suporte a câmera/galeria/arquivos e limite de 15 MB.
- Valida MIME e assinatura mágica antes de persistir o binário.
- Adiciona categoria, descrição, tags, data, pesquisa e filtros.
- Download passa por endpoint autenticado; os arquivos não são publicados por StaticFiles.
- Remoção é lógica no índice e upload/download/remoção geram AuditLog.
- Arquivos podem ser indexados no chat por referência contextual.
- Persistência em `App_Data/patient-files`; sem migration nova. Em produção, o diretório deve estar em volume persistente.
- Atualiza ROADMAP e smoke gate para 2020 validações.

# v0.19.33-r1 — Multi-Plan Gate Semantic Sync
- Corrige falso negativo no gate `[1998/2006]`: a UI informa `sem plano, ter um ou vários planos simultâneos`, enquanto o smoke test ainda exigia literalmente `zero, um ou vários`.
- Sincroniza o gate com a implementação real sem reintroduzir texto artificial na interface.
- Nenhuma alteração funcional, de schema, migration ou contrato da API; versão pública permanece `0.19.33`.

# v0.19.33 — Workout CRUD & Multi-Plan Completion

- CRUD profissional de treino fechado com arquivamento e reativação explícitos.
- Desvinculação segura da rotina ativa preservando histórico e auditoria.
- Duplicação rápida de plano mantendo a linhagem/versionamento.
- Zero, um ou vários planos de treino por paciente suportados na UI.
- Portal do paciente lista todos os planos ativos e permite iniciar qualquer sessão liberada.
- Sem alteração de schema/migration.

# v0.19.32 — Professional Nutrition Access Completion

- Completa o acesso profissional aos recursos nutricionais diretamente na área do paciente.
- Corrige o Nutrition Builder para **editar** planos existentes via `PUT`, em vez de sempre criar um novo plano.
- Mantém criação de dietas, refeições, metas nutricionais e substituições/equivalências no mesmo fluxo.
- Adiciona gerenciamento visual do catálogo de alimentos: criar, editar, desativar e reativar.
- Expõe duplicação/progressão, modelos reutilizáveis, atribuição, arquivamento sem apagar histórico e revisão/publicação.
- Mantém versionamento/histórico e não cria migration nova.
- Atualiza `ROADMAP.md` e amplia o smoke gate para **1994** validações.

# v0.19.31 — Athlete Data Hub

- Cria a área dedicada **Dados para Atletas**, separando métricas técnicas da Home diária.
- Reúne prontidão, carga/volume, performance, recuperação, peso, composição corporal, consistência, XP/streak e contexto do ciclo quando disponíveis.
- Adiciona tendência corporal baseada no histórico de avaliações e atalhos para check-ins, treino, evolução e chat.
- Adiciona acesso pela Home, navegação secundária e menu Mais do paciente.
- Inclui responsividade e dark mode próprios, sem migration nem alteração de schema.
- Mantém a regra de que métricas são contexto longitudinal e não substituem decisão profissional.

# v0.19.30-r3 — Patient Home Legacy Action Hub Cleanup

- Corrige falso negativo do gate histórico `[1161/1168]`, que ainda exigia o antigo `mobile-now-hub`/Action Hub removido pela v0.19.30.
- Atualiza os gates históricos para validar o bloco atual **Hoje em um olhar**, suas seis ações essenciais, prioridade contextual, responsividade e acessibilidade de movimento.
- Atualiza também os gates posteriores de hierarquia/adaptação que ainda citavam `mobile-now-hub`, evitando novas falhas em cascata.
- Remove listeners e CSS mortos de `mobileNowPrimary`, `mobileNowWater`, `mobileNowMore` e `.mobile-now-*`.
- Nenhuma alteração de schema, API clínica ou versão pública; permanece `0.19.30`.

# v0.19.30-r1 — Daily Athlete Gate Compatibility Hotfix

- Corrige falso negativo do gate histórico `[605/696]` após a simplificação intencional da Home do paciente na v0.19.30.
- O teste não exige mais o antigo texto `DAILY ATHLETE • PRONTIDÃO` nem o botão removido `dailyReadinessButton`.
- O gate passa a validar a integração atual do check-in pela Home nova: `hpAthleteHome2` → `athleteHome2Body` → `openDailyReadiness(readiness)`.
- Remove o listener morto de `dailyReadinessButton` do `app.js`.
- Nenhuma alteração de schema, API clínica ou versão pública; permanece `0.19.30`.

# v0.19.30 — Patient Home Cleanup

- Reorganiza a Home do paciente em torno da pergunta “o que eu preciso fazer hoje?”.
- Novo bloco **Hoje em um olhar** com Check-in, Treino, Alimentação, Hidratação, Mensagens e Pendências.
- Remove da primeira leitura blocos duplicados de prontidão, resumo e ação imediata que competiam pela atenção.
- Mantém análises esportivas e gamificação recolhidas, preservando funcionalidade sem poluir a Home comum.
- Melhora CSS mobile e dark mode do resumo diário.
- Preserva loading, erro/retry globais do portal e navegação para cada ação essencial.
- Sem alteração de schema/migration.

# v0.19.28 — Settings & User Profile

- Consolida a página de Configurações como central da conta do usuário.
- Persiste tema e preferências de notificações por usuário.
- Adiciona foto de perfil com compressão local para 256x256 antes do envio; JPEG/PNG/WebP aceitos e payload limitado.
- Adiciona resumo da sessão atual (IP, user-agent e expiração do JWT).
- Mantém alteração de nome e senha já existentes.
- Auditoria registra atualização/remoção da foto sem persistir o conteúdo da imagem.
- Migration `V01928ConfiguracoesPerfilUsuario` adiciona apenas preferências de conta ao Identity user.
- Interface responsiva para desktop/mobile e integração com o tema já existente.
- Versão pública: `0.19.28`.
- Gate final esperado: `1949/1949`.

# v0.19.27 — Session & Authorization Hardening

- Corrige a causa estrutural do `403` no chat do paciente: o controller agora usa `AuthenticatedOnly`, com `PatientOnly` nas rotas `/me` e policy profissional nas rotas por paciente.
- Adiciona isolamento de conversa: profissional só acessa o chat quando é o profissional resolvido para aquele paciente.
- JWT passa a carregar `security_stamp`; a autenticação valida usuário ativo, organização e stamp em toda requisição autenticada.
- `POST /api/auth/logout` rotaciona o `SecurityStamp`, revogando imediatamente os tokens emitidos anteriormente.
- `POST /api/auth/renovar` renova a sessão enquanto o token atual permanece válido; o frontend agenda renovação cinco minutos antes da expiração.
- 401 e 403 passam a retornar JSON padronizado; o frontend desloga apenas em 401 e preserva a sessão em 403.
- Lockout de 5 falhas / 15 minutos é ativado também para contas antigas no login, e novas contas passam a nascer com `LockoutEnabled=true`.
- Mantém desenvolvimento/testes isolados de produção/VPS.
- Versão pública: `0.19.27`.

# v0.19.26 — Public Access & Password Recovery

- Adiciona fluxo público de recuperação de senha em `/recuperar-senha` e redefinição em `/redefinir-senha`.
- Implementa endpoints anônimos `POST /api/auth/password/esqueci` e `POST /api/auth/password/redefinir`.
- Usa tokens nativos do ASP.NET Core Identity com expiração configurada em 30 minutos e encoding Base64Url para links.
- Resposta do pedido de recuperação é deliberadamente genérica para evitar enumeração de contas.
- Adiciona template transacional de redefinição e mantém SMTP desabilitado por padrão no desenvolvimento.
- Audita solicitação/entrega e conclusão sem armazenar token ou senha.
- Adiciona páginas públicas responsivas, estados de sucesso/erro e retorno correto ao login.
- Mantém testes de desenvolvimento isolados de produção/VPS.
- Versão pública: `0.19.26`.

# v0.19.25-r1 — Windows PowerShell UTF-8 BOM Hotfix

- Corrige `TESTAR.ps1` salvo em UTF-8 sem BOM, que fazia o Windows PowerShell 5.1 interpretar caracteres como `→`, acentos e símbolos como ANSI/CP1252 e gerar erros de parser.
- `TESTAR.ps1` volta a ser distribuído em UTF-8 com BOM.
- `PREPARAR.ps1` passa a bloquear futuras regressões verificando o BOM antes do build/teste.
- Nenhuma regra funcional de Account & Email Foundation foi alterada.

## v0.19.25 — Account & Email Foundation

- Adiciona API de gestão do e-mail principal da própria conta para pacientes e profissionais autenticados.
- Implementa confirmação de e-mail e troca segura de endereço com tokens do ASP.NET Core Identity.
- Exige senha atual antes de solicitar troca e bloqueia e-mails duplicados.
- Mantém `UserName` e e-mail do cadastro de paciente sincronizados dentro de transação.
- Adiciona templates básicos de comunicação para confirmação e troca de e-mail.
- Adiciona SMTP configurável por ambiente, desabilitado por padrão no desenvolvimento.
- Registra envio, indisponibilidade/falha, confirmação e troca no `AuditLog`, sem persistir tokens.
- Cria policy `AuthenticatedOnly` para recursos de conta que devem funcionar também para pacientes.
- Amplia o `TESTAR.ps1` até 1914 gates sem realizar chamadas de produção nem disparos SMTP.
- Versão pública: `0.19.25`.

## v0.19.24-r5 — Hotfix Macro Calorie Gate

- Corrige falso negativo do `TESTAR.ps1` no gate de comparação energética dos macros.
- O teste agora valida o texto realmente renderizado pela interface (`Energia dos macros vs. meta ativa:`), além da fórmula de delta e da classe `macro-target-balance`.
- Nenhuma regra metabólica ou funcionalidade foi alterada; permanece a versão funcional `0.19.24`.

## v0.19.24-r4 — Hotfix seeds demonstrativos opcionais

- Remove a dependencia obrigatoria do `TESTAR.ps1` em `POPULAR-ANA-RIBEIRO.ps1`, arquivo demonstrativo que ja nao faz parte do pacote limpo.
- Mantem as validacoes do Coach Diario, schema, versao e funcionalidades reais independentes de seeds de demonstracao.
- Se o seed Ana Ribeiro existir manualmente, suas validacoes legadas ainda podem rodar; se estiver ausente, o gate registra que o seed foi aposentado e segue normalmente.
- Consolida a regra de arquitetura: `PREPARAR.ps1` e `TESTAR.ps1` nao podem depender de populadores/demo removidos do produto.
- Mantem `VERSION.txt` em `0.19.24`; `-r4` identifica apenas revisao corretiva do pacote.

## v0.19.24-r3 — Hotfix limpeza de sobreposição / Render legado

- `PREPARAR.ps1` agora remove explicitamente artefatos legados do Render que podem sobreviver quando um ZIP novo é extraído por cima de uma pasta antiga no Windows.
- A limpeza é restrita aos arquivos conhecidos: `render.yaml`, `DEPLOY-RENDER-MVP.md`, `TESTAR-RENDER.ps1`, `POPULAR-REMOTO.ps1` e `POPULAR-REMOTO-RICO.ps1`.
- `TESTAR.ps1` valida que nenhum desses artefatos permaneceu e orienta executar `PREPARAR.ps1` caso uma sobreposição antiga seja detectada.
- Mantida a regra: testes de desenvolvimento somente em `localhost`/`127.0.0.1`; produção na VPS não é ambiente de teste funcional.

﻿
## v0.19.24-r2 — Hotfix VPS / isolamento de testes
- Remove dependencia dos gates antigos de Render (`POPULAR-REMOTO*`, `TESTAR-RENDER`, `DEPLOY-RENDER-MVP`, `render.yaml`).
- `TESTAR.ps1` agora bloqueia explicitamente BaseUrl que nao seja localhost/127.0.0.1.
- Gates verificam que nenhuma chamada HTTP de desenvolvimento aponta para AESYN em producao/VPS.
- Deploy atual documentado como Docker + VPS + Nginx/HTTPS.

## v0.19.24 — Metabolic Planning UX & Safety


## v0.19.24-r1 — Hotfix TESTAR / pacote limpo

- Remove dependências residuais do `TESTAR.ps1` em `POPULAR.ps1`, arquivo legado já ausente do projeto limpo.
- Mantém `VERSION.txt` em `0.19.24`; `-r1` identifica somente a revisão corretiva do pacote.
- Preserva os gates de follow-up verificando os assets funcionais atuais, sem exigir seed demo removido.

- Conecta visualmente TMB, GET, objetivo, meta calórica e distribuição de macros.
- Exibe a meta calórica ativa e delta dos macros em kcal e percentual.
- Adiciona conciliação assistida por macro residual (carboidrato, lipídio ou proteína), mantendo edição manual.
- Metas abaixo da TMB ou com ritmo >1% do peso/semana exigem confirmação profissional explícita; alvo abaixo de 1200 kcal permanece bloqueado para aplicação automática.
- Adiciona ajuda do fator de atividade e gate com cenários matemáticos conhecidos (84 kg, 171 cm, 27 anos).
- Versão pública: `0.19.24`.

## v0.19.23 — Passive Monitoring Foundation

- Adiciona pipeline normalizado de monitoramento passivo usando a trilha já existente de registros do paciente, sem migration nova.
- Prepara fontes `AppleHealth`, `HealthConnect` e `Garmin` para adaptadores futuros.
- Suporta passos, sono em minutos, FC de repouso, HRV, energia ativa, distância e minutos ativos.
- Implementa validação temporal, limite de lote e deduplicação por ID externo ou fonte+métrica+horário.
- Adiciona endpoints para importação pelo paciente, resumo do paciente, leitura profissional e descoberta das fontes suportadas.
- Exibe resumo no hub Saúde do paciente e na visão geral profissional, com dark mode e responsividade.
- Mantém separação entre dado observado e interpretação: não gera diagnóstico, prescrição ou alteração automática de conduta.
- Versão pública: `0.19.23`.

## v0.19.22-r3 — Positive Progress / AESYN XP — PowerShell Encoding Gate Fix

- Corrige o gate da política de XP positivo para não depender de frases acentuadas lidas de arquivos C# UTF-8 sem BOM pelo Windows PowerShell 5.
- Mantém a política funcional existente intacta; o teste passa a validar tokens ASCII estáveis (`XP negativo`, `progresso`, `excesso de treino`).
- Nenhuma alteração funcional, de banco ou de versão pública.

## v0.19.22-r2 — Positive Progress / AESYN XP — Gate Fix

- Corrige o gate do `TESTAR.ps1` que procurava propriedades PascalCase do DTO dentro do serviço.
- O serviço agora é validado pelos identificadores reais `xpSemana`, `diasComProgresso7` e `faltamXp`.
- As propriedades públicas `XpSemana`, `DiasComProgresso7` e `XpAteProximoNivel` passam a ser validadas no arquivo de contracts, onde de fato são declaradas.
- Nenhuma alteração funcional, de banco ou de versão pública.

## v0.19.22-r1 — Correção do gate Positive Progress

- Corrige strings do `TESTAR.ps1` que usavam escape `\"` inválido no Windows PowerShell.
- Mantém a versão funcional v0.19.22 e todas as funcionalidades de Positive Progress / AESYN XP.

## 0.19.22 - Positive Progress / AESYN XP
- Adiciona resumo de progresso positivo com XP semanal, dias com progresso, avanço de nível e fontes de XP.
- Reforça a regra de XP cumulativo: não existe XP negativo e semanas leves não apagam progresso.
- Adiciona cards dedicados para paciente e profissional, com dark mode e responsividade.
- Mantém descanso/recuperação protegidos contra mecânicas de compensação.

# v0.19.21 — Patient Timeline & Context

- Evolui a timeline profissional para uma leitura longitudinal integrada de contexto.
- Conecta prontuário, corpo, exames, nutrição, treino, check-ins, chat, metas, rotina e eventos de adesão em uma sequência única.
- Adiciona categorias, fonte, prioridade e chave de contexto aos eventos da timeline.
- Inclui execuções de treino, planos de treino, check-ins, mensagens do acompanhamento e desvios de adesão na timeline.
- Agrupa eventos por dia e destaca quando dimensões diferentes ocorreram no mesmo período, sem inferir causalidade clínica.
- Adiciona filtros por dimensão e indicadores dos últimos 7 dias, sinais de atenção e dias com contexto.
- Mantém isolamento por organização e não exige migration nova.

# v0.19.20 — Professional Attention Center

- Evolui a Central do Dia com uma camada de priorização profissional baseada em sinais realmente acionáveis.
- Agrega eventos de desvio/adesão dos últimos 14 dias e destaca recorrências em nutrição e treino.
- Cruza recorrências com pendências de alta prioridade e follow-ups vencidos para reduzir ruído e evitar alertas isolados sem contexto.
- Classifica os pacientes em Observação, Atenção ou Prioridade e mostra os principais sinais que justificam a revisão.
- Inclui resumo no dashboard e acesso direto ao perfil do paciente.
- Mantém limite explícito: a central organiza atenção, não diagnostica e não modifica prescrições automaticamente.
- Sem nova migration.

# v0.19.19 — Deviation & Adherence Events

- Registra eventos estruturados quando a execução difere do plano, sem transformar cada ocorrência em notificação.
- Nutrição: captura refeições adaptadas e não realizadas, incluindo o contexto informado pelo paciente.
- Treino: captura substituições de exercícios e itens não concluídos durante a execução.
- Adiciona API profissional de histórico, recorrência e prioridade para alimentar a futura Central de Atenção.
- Perfil profissional ganha resumo de desvios dos últimos 30 dias, com recorrências e separação por nutrição/treino.
- Não altera automaticamente treino, dieta ou conduta profissional.
- Sem migration nova: utiliza o registro diário já existente como trilha estruturada e auditável.


## v0.19.18-r1 — correção de compilação do Patient Professional Chat
- Corrige `ChatAcompanhamentoController`: `Consulta` não possui `OrganizacaoId`.
- O filtro de segurança por organização passa a usar `Consulta.Paciente.OrganizacaoId`, mantendo isolamento organizacional sem alterar schema.
- Nenhuma migration nova; versão funcional permanece `0.19.18`.
## v0.19.18 — Patient Professional Chat
- Chat direto paciente ↔ profissional dentro da plataforma.
- Contextos: geral, nutrição, treino, exames, medicamentos/suplementos e recuperação.
- Notificação automática ao destinatário a cada nova mensagem.
- SLA informativo de até 24h úteis e aviso de não emergência.
- Reutiliza InteracaoAcompanhamento e NotificacaoInterna; sem migration nova.

# v0.19.17 — Smart Exercise Alternatives

- Alternativas de exercício no fluxo de execução do paciente.
- Ranking limitado ao mesmo grupo muscular e catálogo da organização.
- Contextos para equipamento ocupado, falta de equipamento e preferência.
- Dor/desconforto bloqueia troca automática e orienta revisão profissional.
- Adaptação fica registrada de forma estruturada no histórico da execução.
- Sem migration nesta versão.

## v0.19.16 — Workout Progression Engine
- Cruza regras de progressão configuradas com histórico real de carga, repetições, RPE e tendência.
- Classifica exercícios em candidato à progressão, manter/observar ou construindo base.
- Exibe orientação contextual ao paciente e painel de revisão ao profissional.
- Não aplica aumento automaticamente; decisão permanece profissional.

## v0.19.15-r3 — Smart Meal Swap gate hardening

- Corrige o gate do Smart Meal Swap para validar `SimilaridadePercentual` no contrato de resposta, onde a propriedade realmente é declarada.
- Mantém a versão funcional v0.19.15 e não altera comportamento da API.

## v0.19.15 — Smart Meal Swap
- Paciente pode pedir uma troca para uma refeição já prescrita e nutricionalmente próxima.
- Alternativas são ranqueadas por proximidade de calorias e macronutrientes.
- A troca é registrada como adaptação com motivo, preservando o plano original para revisão profissional.
- Assets web agora usam cache busting por versão para evitar CSS/JS antigos após atualização.
- Reforço adicional das superfícies do tema escuro profissional.


## v0.19.14-r1 - Migration snapshot + dark surface hardening
- Corrige o snapshot/model target da migration `V01914CalendarioNutricional`, eliminando `PendingModelChangesWarning` no `database update`.
- Adiciona o Designer da migration e mantém snapshot sincronizado com `ProgramacaoNutricionalDia`.
- Completa o tema escuro em Central de Atenção, Carteira, Pacientes, Perfil, Pendências, Follow-up e Gestão Operacional.
- Remove superfícies brancas residuais no dark mode sem alterar a identidade do tema claro.
# v0.19.14 — Nutrition Calendar

- Calendário nutricional mensal por paciente, com persistência no PostgreSQL.
- Cada dia pode apontar para uma versão de plano alimentar e um contexto (padrão, treino, descanso, viagem ou flexível).
- Cópia de semana para a semana seguinte, preservando os cardápios configurados.
- Troca de dois dias programados sem alterar os planos originais.
- Navegação mensal e edição rápida diretamente na aba Alimentação do paciente.
- Nova migration `V01914CalendarioNutricional`.

# v0.19.13 — Smart Food Equivalences

- Adiciona busca de alternativas alimentares equivalentes diretamente no Nutrition Builder.
- Ranqueia opções por proximidade de calorias e macronutrientes, com ajuste automático da porção em gramas.
- Prioriza alimentos da mesma categoria quando disponível e mostra até seis alternativas.
- A aplicação é sempre explícita pelo profissional e permanece editável antes de salvar/publicar o plano.
- Interface responsiva compatível com os temas claro e escuro.


## v0.19.12-r1 — Meal & Portion Manager 2.0
- Ajusta os rótulos de duplicação no Nutrition Builder para “Duplicar refeição” e “Duplicar alimento”, alinhando a interface ao gate funcional e deixando a ação mais clara em produção.
# v0.19.12 — Meal & Portion Manager 2.0

- Nutrição: porções inteligentes, medidas domésticas e equivalência em gramas integradas ao Builder atual.
- Nutrição: escala rápida de porções por alimento/refeição.
- Nutrição: ordenar refeições, distribuir metas e salvar refeições existentes como modelos reutilizáveis.
- Produção: versão pública atualizada para 0.19.12.

# v0.19.11 — Dark UI Consistency

- Padroniza a Carteira de Pacientes no tema escuro premium do AESYN.
- Remove superfícies brancas residuais em cards, métricas e badges de prioridade.
- Reforça contraste de tipografia, bordas e ações em grafite/preto com verde apenas como acento.
- Mantém o tema claro intacto e não altera schema.

## 0.19.10 - Food Portions & Measure Converter
- Nutrition Builder passa a aceitar gramas, ml, unidade, fatia, colher, xícara, concha e porção.
- Conversão bidirecional entre quantidade e gramas usando equivalência editável por medida.
- Cálculo nutricional continua baseado em gramas, preservando precisão e permitindo prescrição em medidas mais naturais para cada alimento.
- Substituições alimentares recebem o mesmo conversor de medidas.
- Sem alteração de schema/migration.

## v0.19.9 — Macro Targets by Body Weight

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
- Novo slot profissional de fotos de evolução corporal dentro do perfil do paciente.
- Upload autenticado de JPEG/PNG/WebP com limite de 8 MB, validação de assinatura e segregação por organização/paciente.
- Arquivos ficam fora do `wwwroot` e só são lidos por endpoint autenticado, evitando exposição pública das fotos clínicas.
- Galeria longitudinal com posição (frente, costas e laterais), data e observação.
- Comparação lado a lado entre duas datas.
- Ferramenta de desenho sobre a imagem; a anotação é salva como cópia e preserva a foto original.
- Persistência clínica reutiliza o Diário do Paciente, sem migration nova nesta etapa.
- Remove versão histórica visível do Command Center e detalhe de infraestrutura da mensagem de login.

## v0.19.5 — Patient Advanced Technique Guidance
- Leva BISET, DROP set e progressão de carga para a experiência de execução do paciente.
- Interpreta os marcadores estruturados gerados pelo Workout Builder sem nova migration.
- BISET passa a exibir agrupamento e orientação de execução conjugada.
- DROP exibe percentual, etapas e carga de referência calculada quando há carga prescrita.
- Progressão exibe gatilho profissional e próxima carga de referência quando aplicável.
- Identidade visual das técnicas segue o dark theme premium do AESYN.
- Nenhuma alteração de schema; baseline permanece 39/39.

## v0.19.4 — Advanced Workout Techniques Builder

- Adiciona BISET/conjugado, DROP set e Progressão de carga ao Workout Builder profissional.
- BISET pode agregar automaticamente um segundo exercício ao mesmo grupo.
- DROP permite redução percentual e múltiplas etapas.
- Progressão aceita percentual alvo e regra de aplicação.
- Preserva as técnicas no JSON do treino-modelo e gera orientação legível ao criar ficha do paciente.
- Remove referências visíveis de versão do cabeçalho do Workout Builder visitado nesta entrega.
- Sem alteração de schema.

## v0.19.3 — Patient Workout Access Hub

- Paciente passa a visualizar todos os treinos/sessões liberados da ficha em atalhos A, B, C...
- Qualquer treino disponível pode ser iniciado diretamente pelo paciente, sem ficar preso apenas ao treino sugerido para o dia.
- Mantidos prontidão diária, recomendação contextual e prescrição profissional como referência, sem bloquear a escolha de sessão liberada.
- Hub responsivo para uso mobile-first e integrado ao tema claro/escuro AESYN.
- Sem alteração de schema ou migration.

# v0.19.2 — Production Experience Cleanup

## v0.19.2-r1 — Production Experience Cleanup hardening
- Corrige gates históricos que ainda exigiam textos de MVP/demo já removidos da interface.
- Remove badges visíveis “Demo” das áreas profissional e do paciente.
- Converte a Central de Ajuda para linguagem operacional de produção.
- Remove referência visível a “MVP local” do convite do paciente.
- Mantém versão funcional pública em 0.19.2, sem alteração de schema.

- Remove linguagem visível de demo/MVP Preview da home e do shell autenticado.
- Substitui o roteiro de demonstração por uma Central de Ajuda AESYN adequada ao uso em produção.
- Remove versão técnica da superfície pública de login; a versão permanece disponível no health endpoint e nos artefatos operacionais.
- Mantém identidade AESYN, tema premium e hardening HTTPS da v0.19.1.
- Sem alteração de schema.

## v0.19.1 — Production HTTPS & Proxy Hardening

- Adiciona processamento explícito de `X-Forwarded-For` e `X-Forwarded-Proto` antes do pipeline HTTPS.
- Habilita HSTS em ambientes fora de Development.
- Mantém HTTPS redirection sem criar loop atrás do Nginx/Render quando o proxy informa o esquema original.
- Atualiza Swagger, Health, Preview e versão pública para 0.19.1.
- Adiciona `PRODUCAO-HTTPS.md` com configuração de Nginx e validação operacional.
- Sem alteração de schema/banco de dados.


## v0.19.0-r5 — AESYN Organic Graphite Home
- Refinamento visual da home pública com base grafite/preta e petróleo restrito a profundidade.
- Verde AESYN passa a atuar como cor de acento em vez de dominar o plano de fundo.
- Símbolo central preservado com tratamento mais orgânico, camadas radiais e linhas sutis.
- Cards de pilares e cabeçalho público migrados para superfícies neutras escuras.
- Card de login suavizado para off-white, reduzindo contraste agressivo com o fundo.


### v0.19.0-r2
- Corrige o suporte semântico de tema com `color-scheme: light/dark`, alinhando controles nativos do navegador ao tema AESYN.
- Mantém a versão funcional pública em 0.19.0; trata-se apenas de revisão do pacote.
## 0.19.0 — AESYN Identity & Patient Navigation Foundation

- Nova identidade visual AESYN em verde-petróleo e menta, com monograma atualizado.
- Escolha de tema claro/escuro no primeiro login, persistida no navegador e alterável depois.
- Navegação principal do paciente reorganizada para Hoje, Treino, Plano e Saúde.
- Novo hub Saúde com Exames laboratoriais, Monitoramento, Recuperação e Como estou.
- Dark mode reforçado para superfícies legadas do portal e área profissional.
- Fundação visual preparada para a próxima fase de treino, nutrição, fotos e integrações.
- Sem mudança de schema.

## 0.18.16 — Workout Builder Delete + Weekly Patient Plan

- Remoção funcional e touch-friendly de exercícios no builder.
- Seleção do treino do dia usando os dias prescritos.
- Visão semanal completa do plano no portal do paciente.
- Navegação do resumo semanal para cada ficha de treino.
- Mobile UX reforçada.
- Sem mudança de schema.

## 0.18.15 — Nutrition Review & Publish 2.0

- Revisão nutricional antes da publicação.
- Histórico e seleção de versões.
- Comparativo prescrito × meta para kcal/macros/fibra.
- Revisão de refeições e orientações.
- Publicação explícita com histórico preservado.
- Integração direta na aba Nutrição.
- Sem mudança de schema.

## 0.18.14 — Nutrition Templates 2.0

- Biblioteca de modelos de dieta/refeição refeita para uso profissional.
- Correção do erro de abertura de Modelos em Nutrição.
- Busca, filtros, categorias e ordenação.
- Atribuição/inserção, edição e ativação/desativação.
- KPIs e estados de erro/empty state.
- Sem mudança de schema.

## 0.18.13 — Nutrition Builder Large Catalog

- Otimização do Nutrition Builder para catálogos de milhares de alimentos.
- Busca inteligente e normalizada com limite de resultados.
- Totais nutricionais por refeição em tempo real.
- Estruturas rápidas de 3–6 refeições.
- Comparação de fibras incluída nas metas.
- Melhorias de responsividade.
- Sem mudança de schema.


## 0.18.12 — Professional Workout & Nutrition Flow Hardening

- Correção do botão de novo treino no paciente.
- Responsividade reforçada do Workout Builder.
- Fluxo explícito para adicionar Treino B/C/D ao mesmo plano.
- Ações de editar/agregar treino preservando sessões já salvas.
- Navegação profissional de Programas de treino endurecida com retry.
- Fluxo de Nutrição reorganizado para montar dieta e acessar modelos.
- Sem mudança de schema.
# v0.18.6 — Exercise Library 2.0 + Standalone Workout Studio

- Novo catálogo profissional de exercícios com busca, filtros, CRUD e ativação/inativação.
- Endpoint de catálogo-base adiciona dezenas de exercícios sem duplicar nomes existentes.
- Treinos-modelo agora podem nascer e ser editados sem qualquer paciente vinculado.
- Builder standalone suporta múltiplas sessões, séries, repetições, carga, descanso, tempo e observações.
- Professional Prescription Workspace reposicionado como AESYN Professional Workout Studio.
- A atribuição ao paciente continua explícita e cria uma cópia independente do modelo.
- Sem alteração de schema ou migration.


### v0.18.5-r3 — Smoke test current-version gate fix
- Corrige gates históricos do `TESTAR.ps1` que ainda comparavam `VERSION.txt` com `0.18.4`.
- Preserva tokens históricos de funcionalidades anteriores; apenas a versão corrente esperada passa a ser `0.18.5`.
- Sem alteração funcional, de schema ou migration.



### v0.18.5-r2 — Windows PowerShell UTF-8 parser hardening
- `TESTAR.ps1` agora e distribuido com UTF-8 BOM para compatibilidade com Windows PowerShell 5.1 e literais acentuados/setas usados pelos gates historicos.
- Nenhuma alteracao funcional, de schema ou migration.

## v0.18.5-r1 — Exams Timeline & Trends (test gate fix)
- Corrige gates legados do `TESTAR.ps1` que ainda esperavam `VERSION.txt = 0.18.4`.
- Atualiza a validação do MVP Preview para a versão empacotada `0.18.5`.
- Sem alteração funcional, de banco ou de schema.
# v0.18.4 — AESYN Exams Core

- Nova experiência laboratorial dentro do Patient Performance Profile.
- KPIs de coletas, marcadores, resultados numéricos e itens para revisão.
- Histórico expansível por coleta e marcador.
- Tendências laboratoriais nativas para marcadores com duas ou mais coletas.
- Guardrail explícito: organização e tendência apoiam revisão profissional, sem diagnóstico automático.
- Responsividade desktop/tablet/mobile.
- Sem alteração de schema; baseline permanece 38/38.

# Changelog

## 0.18.3 — Evolution Dashboard

- adiciona dashboard longitudinal de evolução dentro do Patient Performance Profile;
- integra avaliação corporal, treino, adesão, prontidão, hidratação e exames em uma leitura única;
- adiciona KPIs, gráficos, contexto laboratorial e timeline de marcos;
- mantém schema 38/38 e não adiciona migration.

## v0.18.0-r2 — Correção de gate legado do Prescription Workspace
- Corrige o teste histórico de acesso ao Professional Prescription Workspace para validar a rota e o carregador reais, sem depender do título antigo da interface.
- Preserva o rebranding AESYN Performance e todas as regras de negócio da v0.18.0.
- Sem migration e sem alteração de schema.

## v0.18.0 — AESYN Performance Foundation

- Reposiciona a experiência visual como **AESYN Performance** sem renomear a arquitetura interna HealthPlatform.
- Aplica a identidade visual AESYN: verde petróleo `#0B3B36`, esmeralda `#19C98B`, off-white `#F6F8F7`, grafite `#111817` e cinza frio `#A7B2AF`.
- Redesenha login e branding do profissional/paciente para saúde e performance em sincronia.
- Reorganiza a linguagem principal para Performance, Acompanhamento e Treino & Nutrição.
- Mantém backend, banco, APIs e features da v0.17.9 sem alteração de schema.
- Prepara a interface para a próxima fase: Performance Dashboard, Performance Profile, evolução e exames.

## v0.17.9-r1 — Smoke Test Workspace Label Fix

- Corrige falso negativo no check histórico do Professional Prescription Workspace.
- O smoke test passa a validar o rótulo atual `Revisar & publicar` em vez do legado `Ajustar e publicar`.
- Nenhuma alteração funcional, de API, schema ou migrations.

## v0.17.9 — Review, Publish & Versioning
- revisão conjunta do plano de treino e plano alimentar do paciente;
- histórico visual de versões com status;
- publicação explícita de uma versão, inativando a anterior sem apagar histórico;
- acesso pelo prontuário e integração com builders existentes;
- sem migration: schema permanece 38/38.

## v0.17.8 — Patient Preferences & Smart Adaptation
- adiciona Smart Adaptation ao Recommendation Tuning Workspace;
- cruza preferências do rascunho com rotina/hidratação já registradas na anamnese;
- oferece ajustes explicados e aplicáveis um a um, sem publicação automática;
- cobre rotina apertada, disponibilidade semanal, hidratação progressiva, restrições alimentares, retorno ao exercício e qualidade de vida;
- adiciona acesso "Preferências & adaptação" no prontuário;
- preserva schema 38/38 e os builders/bibliotecas existentes.

## v0.17.7 — Diet & Meal Library
- Centraliza modelos de dietas e refeições em uma biblioteca profissional.
- Permite busca, filtro, edição, ativação/desativação e atribuição.
- Dietas atribuídas geram cópias independentes para personalização.
- Refeições podem ser inseridas em planos ativos do paciente.
- Integração com Professional Prescription Workspace e Nutrition Builder 2.0.
- Schema preservado em 38/38.

# Changelog

## v0.17.6 — Nutrition Builder 2.0

- Adiciona edição completa do plano alimentar existente no mesmo builder de criação.
- Adiciona busca de alimentos sem desmontar o rascunho atual.
- Adiciona duplicação rápida de refeições e itens.
- Mostra meta × prescrito e total estimado enquanto o profissional monta o plano.
- Integra o rascunho da Recommendation Tuning como contexto opcional.
- Mantém schema 38/38 e todos os endpoints existentes.

## v0.17.4 — Workout Builder 2.0
- adiciona edição completa de planos de treino existentes usando o PUT já disponível na API;
- troca dias livres em texto por seletor estruturado de dias da semana;
- adiciona busca local por nome/grupo/equipamento no catálogo de exercícios;
- cadastrar exercício novo atualiza o construtor sem perder o rascunho;
- permite duplicar sessões e exercícios para acelerar prescrições semelhantes;
- leva o contexto do Recommendation Tuning para dentro do builder como referência profissional;
- preserva schema 38/38 e não cria migration.

# v0.17.3 — Recommendation Tuning Workspace

- adiciona workspace de ajuste fino da recomendação inicial;
- permite editar água, sessões de força, duração, refeições, condicionamento, direção nutricional e preferências;
- mostra comparação **Sugerido × Ajustado** em tempo real;
- inclui atalhos conservadores para simplificar rotina, priorizar recuperação ou restaurar a sugestão original;
- mantém ajustes como rascunho e preserva decisão/publicação profissional;
- prepara continuidade dos ajustes para Workout Builder 2.0 e Nutrition Builder 2.0;
- mantém schema 38/38 e nenhuma migration nova;
- amplia smoke test para 1496 verificações.

# v0.17.2 — Treatment Goal & Initial Recommendation

- seleção de objetivo: hipertrofia, perda de peso, recomposição, condicionamento, qualidade de vida, manutenção e retorno ao exercício;
- rascunho inicial editável de hidratação, frequência de treino e direção nutricional;
- disponibilidade semanal, duração, refeições/dia, nível e preferências/restrições entram no contexto;
- hidratação usa uma faixa-base de protótipo (30–35 ml/kg) somente como rascunho, com ajuste rápido de ±250 ml;
- nada é publicado automaticamente; o profissional mantém a decisão final;
- mantém schema 38/38 e nenhuma migration nova.

# v0.17.1 — Patient Intake & Body Assessment

- Adiciona onboarding profissional guiado após o cadastro do paciente.
- Separa cadastro essencial de avaliação corporal para não bloquear o primeiro atendimento.
- Reutiliza Avaliações existentes para dados de antropometria/composição corporal e principais campos de bioimpedância já suportados.
- Permite pular e retomar a avaliação pelo prontuário.
- Nenhuma migration nova; schema permanece 38/38.
- Mantém a decisão profissional como requisito para as próximas sugestões assistidas.

# HealthPlatform v0.17.0 — Professional Prescription Workspace

- inaugura a fase de experiência profissional de prescrição;
- adiciona `Prescrições` à navegação profissional com um workspace dedicado a avaliação → objetivo → prescrição → revisão;
- consolida contadores das bibliotecas existentes de planos de treino, sessões, planos alimentares e refeições;
- permite continuar a prescrição a partir de pacientes recentes e adiciona atalho `Prescrição` no prontuário;
- explicita o fluxo futuro de avaliação corporal/bioimpedância, objetivo terapêutico, preferências e sugestão assistida sempre sujeita à revisão profissional;
- corrige a colisão JavaScript entre salvar refeição como modelo e salvar plano alimentar como modelo (`openSaveMealTemplate` duplicada);
- mantém schema 38/38 e não cria migration.

## v0.16.4 — Weekly Challenges
- Novo resumo contextual de desafios da semana na Home do atleta.
- Reutiliza `gamificacao.desafiosSemana`, prontidão e contexto semanal.
- Recuperação planejada e hábitos básicos podem representar progresso semanal.
- Sem compensação de treino perdido e sem bônus por intensidade extra.
- Schema preservado em 38/38; nenhuma migration nova.

## v0.16.3-r1 - Smoke Test Current Version Fix

- Corrige referencias de versao corrente ainda presas em `0.16.2` no `TESTAR.ps1`.
- Healthcheck, MVP Preview e verificacoes correntes do HealthController passam a validar `0.16.3`.
- Preserva marcadores historicos como `HP_DYNAMIC_MISSIONS='v0.16.2'`.
- Nenhuma alteracao funcional, de schema ou de migration.

# Changelog

## v0.16.3 — Achievement Families
- Agrupa `gamificacao.conquistasRecentes` em cinco famílias comportamentais: Consistência, Recuperação, Execução, Evolução e Hábitos.
- Adiciona uma leitura de repertório para mostrar que tipo de comportamento está sendo reconhecido, sem criar um segundo sistema de badges.
- Mantém recuperação planejada e hábitos como progresso legítimo e explicita que intensidade extra não torna uma família superior.
- Preserva `HP_DYNAMIC_MISSIONS='v0.16.2'`, schema `38/38` e todos os endpoints/regras existentes.
- Amplia o smoke test para 1448 verificações e mantém compatibilidade com Windows PowerShell 5.1.

# v0.16.2 — Dynamic Missions

- Missões da gamificação passam a ser priorizadas dinamicamente com base em prontidão, estratégia do dia, foco gamificado e contexto semanal.
- Dias de recuperação/leve priorizam comportamentos protetivos e não exigem treino para preservar progresso.
- A prioridade visual muda sem duplicar o sistema de missões nem criar uma nova fonte de verdade.
- Gamificação responsável reforçada: excesso e intensidade isolada não geram mérito adicional.
- Sem migration nova; schema permanece 38/38.


## v0.16.1-r1 — MVP Preview Smoke Test Fix
- Corrige falso negativo no check `[463/600]`: identidade do MVP Preview agora valida a versão corrente `v0.16.1`.
- Preserva `HP_ATHLETE_LEVEL_SYSTEM='v0.16.0'` como marcador histórico e não altera schema, API ou regras esportivas.

# Changelog


## v0.16.1 — Streak Intelligence
- transforma streak em leitura de qualidade da sequência, combinando consistência, dias ativos, missões e contexto de recuperação;
- diferencia sequência protegida, sustentável, em construção e retomada inteligente;
- deixa explícito que recuperação planejada preserva a qualidade da adesão e não exige treino para manter streak;
- evita premiar excesso ou transformar sequência em dívida de atividade;
- reutiliza os dados atuais de gamificação e planejamento, sem endpoint, migration ou schema novo.

## v0.16.0 — Athlete Level System
- inaugura a fase Gamificação 3.0 com uma leitura de nível baseada no XP e na consistência já existentes;
- apresenta identidades de progressão: Fundação, Ritmo, Consistente, Performance sustentável e Referência;
- mostra progresso no nível, XP restante, consistência, streak, eventos alinhados e sinais de excesso;
- reforça explicitamente que treino pesado não vale mais XP apenas por ser pesado e que recuperação adequada também é progresso;
- reutiliza `gamificacao` e `gamificacao2`, sem endpoint, tabela ou migration nova;
- mantém schema `38/38` e amplia o smoke test para `1424/1424`.

## v0.15.6 — Athlete Progress Story
- transforma a evolução esportiva longitudinal já existente em uma narrativa curta e acionável para o atleta;
- destaca até três sinais entre consistência, recuperação, carga, performance, nutrição, hidratação e ciclo;
- apresenta um “próximo capítulo” sem prescrição automática e com proteção explícita da referência profissional;
- integra a história de evolução à Home e leva aos detalhes completos de evolução;
- mantém schema 38/38 e não adiciona migration.

## v0.15.5 — Weekly Review

- adiciona `hpWeeklyReview(d)` à Home do atleta;
- sintetiza treinos/meta semanal, consistência, missões e carga recente;
- diferencia semana protegida, consolidada e em andamento;
- apresenta foco contextual para a próxima semana com proteção explícita contra prescrição automática;
- reutiliza `planejamentoSemanal`, `resumoSemanal`, `tendenciaSemanal`, `gamificacao` e `cargaIndividualizada`;
- CTA abre os cards semanais detalhados já existentes;
- mantém schema `38/38`, sem migration nova;
- smoke test ampliado para `1408/1408`.

## v0.15.4 — End-of-Day Flow

- Nova camada de encerramento diário no portal do atleta.
- Revisão consolidada de corpo, sessão, hidratação e execução antes do fechamento.
- CTA reutiliza `/api/portal/me/fechamento-dia`; nenhuma API ou migration nova.
- Mensagem de segurança evita pressão para completar ou compensar pendências.
- Smoke test ampliado para 1400 verificações.


## v0.15.3-r1 - Smoke Test Schema Check Fix

- Corrige falso negativo no teste 1391/1392 causado por `$migrationCount` nao inicializado.
- A validacao de ausencia de migration agora confere o PREPARAR 38/38 e a inexistencia de `v0.15.3_recovery_day_flow.sql`, seguindo o mesmo padrao da v0.15.2.
- Nenhuma alteracao funcional, de API, schema ou migration.

## v0.15.3 — Recovery Day Flow

- Novo fluxo de recuperação diária no portal do atleta.
- Contexto, plano de recuperação e reavaliação ficam reunidos antes da ficha de treino quando a prontidão pede recuperação/leve.
- Recuperação planejada passa a ser apresentada como execução correta do plano, sem incentivo a compensação de carga.
- Integração somente com dados e ações existentes; schema permanece 38/38 e não há migration nova.
- Smoke test ampliado para 1392 verificações.

## v0.15.2 — Training Day Flow
- Integra a tela de treino em uma jornada diária de três etapas: Contexto → Executar → Fechar.
- Reutiliza prontidão e estratégia do dia para contextualizar a sessão prescrita, sem alterar a prescrição profissional.
- CTA adapta-se ao estado real do atleta: fazer check-in, iniciar treino ou revisar a sessão concluída no dia.
- O fechamento mantém duração, RPE geral e detalhes executados no mesmo fluxo, alimentando o histórico esportivo existente.
- Sinais de recuperação/treino leve ficam explícitos antes da execução, reforçando segurança e coerência com o plano.
- Adiciona navegação visual da execução e responsividade específica para 720px/390px.
- Sem endpoint novo, sem migration nova; schema permanece 38/38.
- Smoke test ampliado para 1384 verificações.

## v0.15.1 — Morning Check-in
- Cria uma experiência matinal dedicada na Athlete Home 2.0 sem duplicar dados ou regras de prontidão.
- O card matinal informa se o check-in está pendente ou concluído e resume os cinco sinais principais: sono, energia, dor, disposição e recuperação.
- A ação de registrar/atualizar reutiliza o fluxo `openDailyReadiness` e o endpoint existente `/api/portal/me/prontidao`.
- O formulário de prontidão ganha uma introdução visual curta com os cinco fatores, mantendo o preenchimento em menos de 1 minuto.
- Responsividade específica para 720px e 390px, com fatores em trilho horizontal no mobile e suporte a `prefers-reduced-motion`.
- Sem endpoint novo, sem migration nova; schema permanece 38/38.
- Smoke test ampliado para 1376 verificações.

## v0.15.0-r2 — Smoke Test CSS Token Fix

- Corrige falso negativo no teste `[1365/1368]` da responsividade da Athlete Home 2.0.
- A validação passa a conferir `display:grid` e `grid-template-columns` como tokens independentes, sem exigir que sejam adjacentes no CSS minificado.
- Corrige também a mensagem de erro do badge MVP Preview para mencionar `v0.15.0`.
- Sem alterações funcionais, schema ou migrations.

# v0.15.0 — Athlete Home 2.0

- inaugura a fase Athlete Experience 1.0;
- adiciona uma camada operacional no topo da Home do atleta;
- reúne “Como estou”, “O que fazer agora”, “Como vai o dia” e “Como estou evoluindo”;
- reutiliza prontidão, estratégia do dia, hidratação, execução, gamificação e ciclo;
- cards levam diretamente ao check-in, treino, registros e evolução;
- mantém schema 38/38 e não adiciona migration.


## v0.14.9-r1 - PowerShell 5.1 smoke-test parser fix

- Corrige quoting do bloco `Mobile Accessibility & Polish` no `TESTAR.ps1`.
- Mantem a versao funcional `0.14.9`, schema `38/38` e 1360 verificacoes.
- Preserva UTF-8 com BOM para compatibilidade com Windows PowerShell 5.1.
# v0.14.9 — Mobile Accessibility & Polish

- Fecha a série mobile 0.14.x com acessibilidade e acabamento transversal.
- Adiciona skip link para o conteúdo principal e foco visível consistente.
- Marca a seção atual com `aria-current=page` e anuncia mudanças de área em região `aria-live`.
- Garante alvos mínimos de toque de 44 px nas principais ações mobile.
- Adiciona tratamento para `prefers-contrast: more` e consolida `prefers-reduced-motion`.
- Melhora wrapping de conteúdo em telas estreitas e semântica de navegação do atleta.
- Preserva schema 38/38, endpoints e regras clínico-esportivas.
- Smoke test ampliado para 1360 verificações.

# v0.14.8 — Athlete Profile Mobile

- adiciona **Meu perfil** ao menu mobile do atleta;
- consolida identidade esportiva, nivel, XP, streak e consistencia;
- mostra ciclo atual, objetivo, semana, progresso e meta semanal;
- resume ultima avaliacao corporal e dados basicos do atleta;
- adiciona layouts responsivos para 720 px e 390 px;
- preserva schema 38/38 e as regras clinico-esportivas existentes;
- amplia o smoke test para 1352 verificacoes.

# v0.14.7 — Mobile Modals & Bottom Sheets

- Consolida modais mobile como bottom sheets com safe-area, alvos de toque e entrada visual consistente.
- Fecha overlays ao tocar no backdrop sem conflitar com o conteúdo interno.
- Adiciona `role=dialog`, `aria-modal`, foco inicial e contenção de Tab dentro do modal.
- Restaura o foco anterior ao fechar e bloqueia scroll/overscroll do fundo enquanto o overlay está aberto.
- Mantém ações persistentes e respeita `prefers-reduced-motion`.
- Sem migration nova; schema permanece 38/38.

# v0.14.6 — Mobile Charts & Progress

- Evolui gráficos SVG existentes para leitura mobile-first sem criar nova dependência visual.
- Adiciona resumo Mín./Atual/Máx., intervalo temporal, tendência explícita e descrição acessível.
- Aumenta legibilidade e alvos dos pontos em telas pequenas, preservando rolagem vertical.
- Mantém endpoints, regras esportivas e schema 38/38 sem migration nova.
- Smoke test passa a 1336 verificações.

# v0.14.5 — Mobile Feedback States

- Consolida estados de carregamento, erro, vazio, sucesso e envio no portal mobile.
- Erros de carregamento ganham estado persistente com ação `Tentar novamente`, evitando telas presas em loading.
- Registros rápidos do atleta usam `aria-busy`, spinner e bloqueio de envio duplicado enquanto salvam.
- Empty states recebem semântica acessível e superfície visual consistente.
- Loading states passam a usar `role=status` e `aria-live=polite`.
- Mantém schema 38/38 e não altera endpoints ou regras esportivas.
- Smoke test ampliado para 1328 verificações.

# v0.14.4 — Mobile Forms & Inputs

- Consolida a etapa de formulários da série mobile 0.14.x.
- Form grids do portal do atleta passam a uma coluna em telas pequenas, incluindo layouts `two`, `three` e `span-2`.
- Inputs, selects, textareas, checkbox, radio e range recebem dimensões e estados mobile consistentes.
- Ações de formulários em sheets/modais ficam persistentes e respeitam safe-area.
- Enhancement progressivo adiciona `inputmode`, `aria-required`, `enterkeyhint` e reposicionamento do campo focado para o teclado virtual.
- Preserva regras esportivas, endpoints e persistência; sem migration nova, schema 38/38.
- Smoke test ampliado para 1320 verificações.

### v0.14.3-r4 — Rich Athlete Demo Seed Audit

- Mantem a versao funcional 0.14.3 e o schema 38/38.
- Corrige a janela historica das metas: os 56 dias agora ficam integralmente dentro do periodo configurado.
- Estende o historico de treino ate a semana atual para alimentar corretamente carga interna recente, linha de base e PRs.
- Adiciona auditoria final com contagem persistida de treinos, metas, diario e avaliacoes.
- Adiciona diagnostico explicito de carga e performance/PR para diferenciar falta de dados de comportamento esperado.
- Revisao de tooling/demo; nenhuma regra funcional, endpoint ou migration nova.

### v0.14.3-r3 — Rich Athlete Demo Seed Fix

- Torna o seed ASCII-safe e envia JSON explicitamente em UTF-8 para compatibilidade com Windows PowerShell 5.1.
- Reaproveita o atleta Lucatti Demo e os dados existentes de forma idempotente.
- Corrige o payload de treino que impedia a etapa de historico esportivo de continuar.
- Revisao de tooling/demo; nenhuma migration nova.

### v0.14.3-r2 — Rich Athlete Demo Seed

- Mantem a versao funcional 0.14.3 e o schema 38/38.
- Adiciona `POPULAR-LUCATTI-DEMO-RICO.ps1`, um cenario esportivo ficticio de 56 dias para avaliar a experiencia mobile com dados longitudinais.
- O seed cobre prontidao, metas, hidratacao, peso, treino executado, RPE, progressao, ciclo esportivo, avaliacoes corporais, XP/missoes e contexto do Coach Diario.
- Adiciona `VALIDAR-SEED-DEMO-RICO.ps1`, que usa o parser nativo do PowerShell para impedir regressao de sintaxe como `$key:` antes da execucao do seed.
- Revisao de tooling/demo; nenhuma regra funcional, endpoint ou migration nova.


### v0.14.3-r1 — Render Schema Compatibility

- Corrige bootstrap de bancos Render preservados de versoes antigas.
- `DemoBootstrap` agora garante Protocolos, Medicamentos, XP, Missoes/Conquistas e Ciclos Esportivos antes de `EventosProgressaoSupervisionada`.
- Evita `42P01 relation "CiclosEsportivosPaciente" does not exist` durante deploy sem apagar dados existentes.
- Versao funcional permanece 0.14.3; sem migration nova.

# v0.14.3 — Mobile Data Views

- Refina listas, históricos e tabelas do portal do atleta para uso mobile-first.
- Converte visualmente linhas densas em superfícies compactas no celular, reduzindo scroll horizontal e colunas comprimidas.
- Padroniza medidas, exames, diário, follow-ups, check-ins, metas e histórico de sessões.
- Preserva desktop e regras esportivas; nenhuma migration nova, schema 38/38.
- Smoke test ampliado para 1312 verificações.

# v0.14.2 — Mobile Content Hierarchy

- Consolida a hierarquia visual do conteúdo no portal do atleta.
- Padroniza títulos de cards, textos auxiliares, labels e campos mobile.
- Ajusta espaçamento entre seções e densidade para leitura rápida no celular.
- Melhora contraste de placeholders e superfícies de formulário.
- Mantém regras esportivas e schema 38/38 sem migration.
- Smoke test ampliado para 1304 verificações.

# v0.14.2 — Mobile Navigation Shell

- Refina o shell permanente do portal do atleta no celular.
- App bar fica mais compacta e reduz elementos de identidade redundantes no mobile.
- Busca, notificações e saída recebem alvos de toque consistentes.
- Dock inferior ganha estado ativo mais limpo, sem indicador solto, e integra visualmente o botão Mais.
- Safe-area passa a considerar topo, laterais e base do aparelho.
- Mantém `prefers-reduced-motion` e tratamento para telas abaixo de 390 px.
- Sem migration nova; schema permanece 38/38.
- Smoke test ampliado para 1296 verificações.

# v0.14.0 — Mobile UI Foundation

- Inicia oficialmente a fase 0.14.x de refinamento estrutural do front mobile.
- Centraliza tokens de canvas, superfícies, texto, contraste, foco, raio e sombra do portal do atleta.
- Padroniza títulos, cards, formulários e alvos de toque no mobile.
- Adiciona foco visível acessível, `touch-action: manipulation`, viewport `100dvh` e tratamento consistente de safe-area.
- Reduz divergências visuais entre telas sem alterar lógica clínica, API ou persistência.
- Sem migration nova; schema permanece 38/38.
- Smoke test ampliado para 1288 verificações.

# v0.13.23 — Mobile Session Review

- Histórico recente de treino passa a abrir uma revisão de sessão no portal do atleta.
- Resumo mostra duração, RPE geral, exercícios concluídos, cargas/repetições registradas e observação da sessão.
- Reutiliza exclusivamente os dados já retornados por `treinos/historico`; nenhuma persistência ou endpoint novo.
- Mobile recebe bottom sheet com safe-area e ação opcional para abrir o Quick Log.
- Reforça que revisão do executado não altera a prescrição profissional.
- Sem migration; schema permanece 38/38.
- Smoke test ampliado para 1280 verificações.

# v0.13.22-r1 — Quick Log Hub (Smoke Fix)

- Corrige a validação `[1270/1272]` para comparar os textos do Quick Log sem diferenciar maiúsculas/minúsculas.
- Nenhuma alteração funcional, visual, de API, banco ou schema.

# v0.13.22 — Quick Log Hub

- Registro rápido global acessível acima do dock mobile, sem depender da Home.
- Bottom sheet com Água, Energia, Dor e Sono, reaproveitando o fluxo existente de diário.
- FAB respeita safe-area, telas estreitas e `prefers-reduced-motion`.
- Não cria persistência, score ou regra clínica paralela.
- Sem migration; schema permanece 38/38.
- Smoke test ampliado para 1272 verificações.

# v0.13.21 — Today Brief

- Consolida prontidão, treino e hidratação em um resumo mobile único.
- Reduz duplicidade visual da Home sem remover compatibilidade estrutural dos componentes anteriores.
- Ações rápidas reaproveitam check-in, treino e registro de água existentes.
- Estados do componente derivam apenas de contexto esportivo já calculado.
- Sem migration; schema permanece 38/38.
- Smoke test ampliado para 1264 verificações.

# v0.13.20 — Hydration Pace

- Home mobile ganha um resumo compacto do ritmo de hidratação do dia.
- Reutiliza meta, consumo, progresso e estado de `hidratacaoContextual`.
- Mostra consumido, restante e percentual sem recalcular necessidade hídrica.
- Ação rápida reaproveita o registro de água existente.
- Meta atingida encerra a pressão por volume extra.
- Sem migration; schema permanece 38/38.
- Smoke test ampliado para 1256 verificações.

# v0.13.20 — Daily Closure

- Home mobile ganha um fechamento diário compacto logo após a Daily Athlete Timeline.
- Reutiliza o roteiro diário e o fechamento já existentes, sem persistência paralela.
- Mostra progresso, concluídos/pendentes e estado fechado sem exigir perfeição.
- Ação abre o fluxo existente de fechamento/revisão do dia.
- Sem migration; schema permanece 38/38.
- Smoke test ampliado para 1240 verificações.

# v0.13.16 — Body Context Brief

- Nova leitura rápida mobile antes do Insight Rail.
- Sintetiza semana, carga e recuperação em uma frase contextual.
- Estados steady/observe/review derivados exclusivamente de dados já existentes.
- Check-in pendente aparece como dado faltante, sem inferência de prontidão.
- Sem migration; schema permanece 38/38.
- Smoke test ampliado para 1224 verificações.

# v0.13.15 — Mobile Insight Rail

## UX mobile
- Agrupa Weekly Athlete Rhythm, Training Load Snapshot e Recovery Pulse em uma faixa horizontal de insights no celular.
- Reduz scroll vertical e mantém ações do dia, timeline e prontidão como hierarquia principal.
- Adiciona scroll-snap, ocultação de scrollbar e largura confortável para uso com o polegar.
- Desktop mantém os componentes em sua apresentação completa.

## Segurança e compatibilidade
- Nenhuma migration nova; schema permanece 38/38.
- Nenhuma regra clínica, score, prescrição ou persistência alterada.
- TESTAR.ps1 permanece UTF-8 com BOM para Windows PowerShell 5.1.

# v0.13.15 — Mobile Insight Rail

- Home mobile ganha resumo compacto da carga dos últimos 7 dias;
- comparação usa a mediana histórica individual já existente;
- estado visual reaproveita a classificação de carga individualizada do backend;
- continuidade para a análise completa sem criar fluxo paralelo;
- nenhuma regra clínica nova, nenhuma migration; schema 38/38.

# v0.13.13 — Weekly Athlete Rhythm

- Home mobile passa a mostrar o ritmo da semana em um card compacto.
- Progresso usa meta de treinos, sessões concluídas e restantes já calculadas.
- Estados de recuperação/revisão protegem o atleta de cobrança por volume.
- Semana concluída é tratada como consolidada, sem sugerir volume extra.
- Atalho abre planejamento/resumo semanal detalhado existente.
- Sem migration; schema permanece 38/38.
- Smoke test ampliado para 1200 verificações.

# v0.13.12 — Daily Athlete Timeline

- Home mobile passa a apresentar a sequência diária Check-in → Treino → Recuperação.
- A etapa atual é destacada a partir de dados já existentes de prontidão e resposta à sessão.
- Cada etapa funciona como atalho para check-in, treino ou leitura pós-sessão.
- Não cria prescrição, score, dado clínico ou fluxo persistente novo.
- Ajustes específicos para telas até 720px e 390px, com suporte a `prefers-reduced-motion`.
- Sem alteração de schema; permanece 38/38.
- Smoke test ampliado para 1192 verificações.

## v0.13.11-r4 — Recovery Pulse / smoke test demo encoding-safe

- Corrige a validação `[464/600]` para não depender de frases completas com caracteres acentuados.
- O aviso da demo passa a ser validado por sua estrutura (`dev-note`) e por fragmentos estáveis do conteúdo.
- Nenhuma alteração em API, banco, Recovery Pulse ou interface.

### v0.13.11-r2 — correção da identidade do MVP Preview no smoke test
- Corrige a validação `[463/600]`, que ainda exigia o texto histórico `MVP • DEMO`.
- O teste agora valida o badge atual `mvp-brand-badge compact">Demo`, compatível com o HTML corrente.
- Nenhuma alteração funcional em Recovery Pulse, API, banco ou interface.

# v0.13.11 — Recovery Pulse
### v0.13.11-r1 — correção de sentinelas do MVP Preview
- Torna a validação de identidade do `TESTAR.ps1` resistente a diferenças de encoding do caractere separador `•` no PowerShell/Windows.
- A identidade continua exigindo `MVP Preview`, `v0.13.11`, badge de demo e `loginMessage`, mas sem depender de uma única string Unicode literal.
- Nenhuma regra clínica, API, banco ou interface foi alterada.


- Traz a resposta da última sessão para a Home mobile, sem exigir abertura das análises avançadas.
- Conecta treino, prontidão do dia seguinte, recuperação percebida e dor em um resumo compacto.
- Diferencia resposta estável, observação e necessidade de revisão sem criar diagnóstico ou nova prescrição.
- Permite abrir a leitura completa ou fazer o check-in quando a prontidão ainda está pendente.
- Mantém schema 38/38, sem migration nova.
- Smoke test ampliado para 1184 verificações.

# v0.13.10 — Post-Workout Mobile Loop

- Fecha o fluxo de treino no celular com um resumo pós-sessão próprio, em vez de retornar silenciosamente à lista.
- Mostra duração e RPE registrados sem criar interpretação clínica adicional.
- Oferece próximas ações diretas: registrar água ou voltar para Hoje.
- Resumo pós-treino usa bottom sheet mobile, safe-area e alvos de toque maiores.
- Mantém schema 38/38 e não adiciona migration.
- Smoke test ampliado para 1176 verificações.

# v0.13.9 — Mobile Action Hub

- Home mobile ganha uma área **Agora** para destacar a próxima ação útil do dia.
- Sem prontidão registrada, o check-in matinal vira a ação principal.
- Com prontidão disponível, o treino do dia passa a ser a ação principal.
- Atalhos diretos para água e registros rápidos reduzem navegação pelo menu.
- Hub recebe tratamento específico para telas estreitas e respeita redução de movimento.
- Sem migration nova; schema permanece 38/38.
- Smoke test ampliado para 1168 verificações.

# v0.13.8 — Mobile Home Hierarchy

- Home do atleta ganha o bloco **Em 30 segundos**, reunindo prontidão, treino, hidratação e streak.
- Hierarquia mobile refinada para leitura imediata do estado esportivo diário.
- Card de prontidão compactado no celular, com fatores em rail horizontal e CTA preservado.
- Nenhuma migration nova; schema permanece 38/38.

## v0.13.5 — Empty States & Mobile Polish

- Reestrutura estados vazios do portal do atleta para parecerem estados intencionais de aplicativo, e não telas incompletas.
- Corrige contraste do cabeçalho mobile de páginas como Alimentação, garantindo título e subtítulo legíveis sobre a superfície escura.
- Adiciona estado vazio orientativo para plano alimentar ainda não publicado, com contexto e retorno rápido para Hoje.
- Ajusta altura, espaçamento e ocupação vertical de telas sem conteúdo no celular.
- Mantém schema 38/38 e não adiciona migration.
- Smoke test ampliado para 1136 verificações.

## v0.13.4-r2 — Mobile Typography Polish

- Corrige a tipografia dos títulos de página do portal do atleta no celular.
- Remove fonte condensada e caixa alta forçada de nomes de planos/telas.
- Melhora quebra de linha, legibilidade e escala tipográfica em 720 px e 420 px.
- Sem alteração de regra de negócio, API, banco ou versão funcional.


## v0.13.4-r1 — Workout Execution Mobile Polish
- Refina a tela de execução de treino no celular com contraste consistente entre labels, campos e cartões.
- Reorganiza séries, repetições, carga e RPE em uma grade 2x2 mais legível.
- Transforma o controle "Feito" em alvo de toque adequado para mobile.
- Corrige o rodapé fixo de conclusão para não encobrir a observação geral.
- Mantém regras de negócio, API, schema e versão funcional 0.13.4 inalterados.

## v0.13.4 — Mobile Daily Experience

- Reestrutura a Home do atleta para priorizar o uso diário em celular, reduzindo o comportamento de dashboard desktop.
- Dock inferior passa a ter cinco destinos essenciais: Hoje, Treino, Plano, Evolução e Mais.
- Destinos secundários ficam em uma bottom sheet acessível por polegar, sem navegação horizontal infinita.
- Progresso/missões e análises esportivas usam divulgação progressiva no mobile, reduzindo scroll e carga cognitiva.
- Inclui snapshot compacto do plano do dia antes das análises profundas.
- Registro rápido vira um rail horizontal com alvos maiores de toque.
- Mantém toda a lógica clínica/esportiva da v0.13.3 e schema 38/38, sem migration.
- Smoke test ampliado para 1128 verificações.

﻿
## v0.13.2-r2 — Correção do smoke test + Sport Performance UI

- Corrige a asserção de proteção contextual do Foco Gamificado do Dia para o texto real com acentuação.
- Reduz predominância de branco com canvas azul-acinzentado e superfícies em camadas.
- Reforça contraste e identidade visual de medicina esportiva/performance.
- Melhora responsividade de grids, cards, métricas, prontidão, gamificação, tabelas e navegação entre 1180px, 900px, 680px e 480px.
- Mantém versão funcional 0.13.2, schema 38/38 e regras de negócio intactas.


## v0.13.2-r1 — Refinamento visual e correção do smoke test
- Corrige o healthcheck inicial do TESTAR.ps1 para esperar corretamente a API 0.13.2.
- Unifica a identidade visual entre Hoje, prontidão, gamificação, foco diário e área profissional.
- Reforça hierarquia visual mobile, superfícies, espaçamento, navegação e destaque dos cards esportivos sem alterar regras de negócio ou schema.
## v0.13.2 — Foco Gamificado do Dia

- transforma o contexto esportivo e as missões semanais em uma única prioridade gamificada legível no celular;
- em dias protegidos, recuperação/revisão/retorno gradual prevalecem e nenhuma missão vira cobrança;
- em dias compatíveis, uma missão incompleta vira referência de consistência, nunca obrigação;
- semana já concluída não gera meta extra apenas para buscar XP;
- não cria treino, não altera meta semanal, não concede XP por si só e não autoriza compensação ou progressão automática;
- integra a leitura nas Homes do atleta e do profissional;
- preserva schema 38/38, sem migration nova.

## v0.13.1 — Missões Contextuais 2.0

- contextualiza as missões semanais com prontidão, recuperação planejada, resposta à sessão e retorno gradual;
- missões incompletas podem ficar `SemPressaoHoje` quando recuperação/revisão deve prevalecer;
- preserva progresso e recompensa já registrados, sem apagar desafio, XP ou streak;
- proíbe compensação de sessão, urgência artificial para fechar a semana e aumento de carga para buscar recompensa;
- integra a mesma leitura nas Homes do atleta e do profissional;
- preserva schema 38/38, sem migration nova.

## v0.13.0 — Gamificacao 2.0

- inaugura uma camada de gamificacao orientada a adequacao, consistencia, recuperacao planejada e retorno gradual;
- explicita quando XP recente veio de comportamento alinhado ou de excesso;
- descanso planejado e adaptacao coerente contam como progresso contextual;
- XP/streak nao viram obrigacao de treinar nem justificativa para aumentar carga;
- nao cria XP negativo, score de sofrimento ou recompensa por intensidade bruta;
- integra a leitura nas Homes do atleta e do profissional;
- preserva schema 38/38, sem migration nova.

## v0.12.5 — Relatório Esportivo Profissional

- adiciona `RelatorioEsportivoProfissionalService`;
- consolida evidências esportivas em seções rastreáveis;
- destaca pontos de atenção e pontos estáveis sem score global;
- integra relatório nas Homes do atleta e do profissional;
- preserva schema 38/38, sem migration nova;
- amplia a suíte de fumaça para 1088 verificações.

## v0.12.4 — Retorno Gradual após Pausa/Dor

- Nova leitura de retorno gradual ancorada em sessões realmente executadas e registros corporais persistidos.
- Detecta pausas observáveis de 7+ dias no histórico recente sem presumir lesão ou causa.
- Integra readiness contextual, resposta à sessão, carga individualizada e mapa corporal longitudinal.
- Estados: SemRetornoAtivo, DadosInsuficientes, RetornoEmPreparacao, RetornoEmCurso e RevisarAntesRetorno.
- Progressão permanece supervisionada: um passo por vez, sem compensação de sessões perdidas e sem aumento automático de carga.
- Sem migration nova; schema permanece 38/38.

## v0.12.3 — Readiness Contextual ao Treino do Dia

- relaciona a prontidao do dia com a sessao realmente prevista e sua demanda de RPE;
- classifica a demanda em recuperacao, leve, moderada ou exigente sem criar uma nova prescricao;
- a mesma prontidao pode ser compativel com uma sessao leve e exigir adaptacao diante de uma sessao exigente;
- deload/recuperacao planejada e disponibilidade do dia prevalecem sobre motivacao ou score isolado;
- ausencia de sessao ou check-in nao gera treino artificial nem aumento de exigencia;
- nao e liberacao medica, nao diagnostica lesao e nao altera treino automaticamente;
- integra atleta e profissional sem migration nova; schema permanece 38/38;
- suite de fumaca ampliada para 1072 verificacoes.

# HealthPlatform v0.12.2 — Deload & Recuperação Planejada

- Diferencia redução deliberada de carga de queda involuntária de adesão.
- Reconhece deload, descarga, recuperação/regenerativo e taper somente quando a intenção está explícita no bloco configurado pelo profissional.
- Contextualiza Radar de Adesão, carga individualizada e resposta à sessão durante a janela planejada.
- Não cria deload automaticamente, não prescreve carga, não compensa carga perdida e não diagnostica fadiga/lesão.
- Sem migration nova; schema permanece 38/38.
- Suíte funcional ampliada para 1064 verificações.

## v0.12.1 — Blocos de Treinamento / Mesociclos
- Consolida a `FaseTreino` vinculada ao ciclo como bloco/mesociclo configurado pelo profissional.
- Exibe período, semana atual, objetivo, sessões executadas, duração, RPE, carga interna e critério de transição.
- Contextualiza com carga individualizada e resposta recente à sessão.
- Não cria periodização, deload, carga ideal ou aumento de volume automaticamente.
- Sem migration nova; schema permanece 38/38.

## v0.12.0 — Motor de Carga Esportiva Individualizado

- usa ate 56 dias de sessoes reais para construir referencia semanal do proprio atleta;
- compara a carga atual de 7 dias com mediana e quartis descritivos das semanas ativas anteriores;
- exige historico minimo antes de formar uma referencia individual;
- recuperacao e resposta a sessao prevalecem sobre uma leitura isolada da posicao estatistica da carga;
- abaixo/dentro/acima do historico recente sao descricoes, nao zonas biologicas de seguranca;
- nao calcula risco de lesao, carga ideal ou prescricao automatica;
- integra atleta e profissional sem migration nova; schema permanece 38/38;
- suite de fumaca ampliada para 1048 verificacoes.

## v0.11.5 — Resposta a Sessao

- acompanha a resposta observada apos a ultima sessao concluida usando o check-in do dia seguinte como referencia principal;
- separa prontidao, recuperacao percebida, dor, disposicao e registros de dor localizada pos-sessao;
- evita usar check-in pre-treino do mesmo dia como se fosse resposta posterior;
- estados SemSessao, AguardandoResposta, RespostaEstavel, Observar e Revisar;
- associacao temporal nao implica causalidade e resposta estavel nao autoriza nova progressao;
- nao cria score de sucesso, diagnostico ou prescricao de nova carga;
- integra atleta e profissional sem migration nova; schema permanece 38/38;
- suite de fumaca ampliada para 1040 verificacoes.

## v0.11.4 — Sessao Planejada x Sessao Executada

- compara a estrategia/sessao planejada com a ultima execucao concluida do dia;
- preserva RPE, series e volume estimado como eixos separados, sem score unico;
- usa observacoes da execucao como motivo apenas quando realmente registradas;
- reconhece adaptacao coerente com recuperacao/dor/carga sem tratar reducao como baixa adesao;
- execucao acima da referencia nao recebe premio automatico;
- nao gera compensacao, aumento de carga ou prescricao automatica;
- integra a leitura nas Homes do atleta e do profissional, sem migration nova;
- suite de fumaca ampliada para 1032 verificacoes.

## v0.11.3 — Disponibilidade para Treino

- separa disposicao/motivacao de compatibilidade contextual com a sessao planejada;
- cruza prontidao do dia, dor localizada, recuperacao recente e carga de treino sem criar score opaco;
- introduz estados `DadosInsuficientes`, `RecuperacaoPrioritaria`, `Adaptar` e `CompativelComPlanejado`;
- sinais de recuperacao/dor/carga prevalecem sobre motivacao isolada;
- nao produz liberacao medica, diagnostico ou alteracao automatica do treino;
- integra a leitura nas Homes do atleta e do profissional, sem migration nova;
- suite de fumaca ampliada para 1024 verificacoes.


## v0.11.2 — Mapa Corporal Longitudinal

- amplia dor localizada de 7 dias para uma leitura longitudinal de 56 dias, sem substituir o resumo diário existente;
- agrupa ocorrências por região + lateralidade e mantém frequência, intensidade e impacto funcional separados;
- compara presença em 28 dias recentes vs. 28 dias anteriores (`MaisPresente`, `MenosPresente`, `SemMudanca` ou `HistoricoCurto`);
- considera recorrência apenas como frequência observada (3+ dias), sem diagnóstico, probabilidade de lesão ou causalidade automática;
- integra a visão nas Homes do atleta e do profissional, sem migration nova;
- suíte de fumaça ampliada para 1016 verificações.

# v0.11.1 — Alertas Clinico-Esportivos Transparentes

- Adiciona alertas explicaveis a partir dos oito eixos ja consolidados no Painel de Medicina do Esporte.
- Cada alerta expoe os sinais e origens que motivaram o destaque, sem score oculto.
- Combinacoes recuperacao + dor, carga + recuperacao, dor + prontidao e progressao + sinais concomitantes recebem prioridade contextual.
- Sinal isolado permanece em acompanhamento e nao recebe gravidade artificial.
- Alertas nao produzem diagnostico, nao estimam probabilidade de lesao e nao realizam prescricao automatica.
- Integrado as Homes do atleta e do profissional, sem nova migration; schema permanece 38/38.

﻿
## v0.11.0 r2 — reparo resiliente do Painel de Medicina do Esporte
- O `PREPARAR.ps1` agora valida os guardrails clinicos do `PainelMedicinaEsporteService.cs` antes do build.
- Se uma extracao por sobreposicao preservar uma copia antiga, o arquivo e restaurado automaticamente a partir de `scripts/recovery`.
- Inclui `CORRIGIR-FONTES-v0.11.0.ps1` para reparo manual opcional.
- Sem mudanca funcional, de schema ou de versao semantica.

# v0.11.0 — Painel de Medicina do Esporte

- Consolida prontidao, dor, recuperacao, carga, performance, adesao e progressao em uma visao profissional unica.
- Mantem cada eixo rastreavel aos dados-fonte e nao cria score geral de saude/performance.
- Recuperacao e dor tem prioridade sobre performance isolada na definicao da principal area de atencao.
- Leva contexto do ciclo esportivo e perfil longitudinal para a interpretacao, sem diagnostico automatico.
- Adiciona cards de sintese para profissional e atleta, reduzindo a dependencia de leitura card a card.
- Sem nova migration; schema permanece 38/38.

# v0.10.9 — Perfil de Resposta do Atleta

- Consolida tolerancia individual, resposta longitudinal atual e estabilidade de habitos em um retrato longitudinal unico e revisavel.
- Mantem padroes por eixo separados; nao cria nota geral de responsividade.
- Diferencia padrao historico de estado longitudinal atual para evitar extrapolacao causal.
- Usa recuperacao, carga, performance e estabilidade comportamental apenas como contexto atual.
- Nao rotula o atleta de forma permanente, nao calcula previsao de lesao e nao autoriza nova progressao automaticamente.
- Integrado as Homes do atleta e do profissional sem nova migration; schema permanece 38/38.

# v0.10.8 — Tolerância Individual à Progressão

- Descreve padrões recorrentes do próprio atleta por eixo, usando eventos realmente registrados.
- Exige recorrência no mesmo eixo; um evento isolado não define tolerância individual.
- Mantém duração de observação e estados de resposta como evidências separadas.
- Pode sinalizar estabilidade recorrente, padrão variável ou necessidade de contexto sem score de risco.
- Não calcula probabilidade de lesão, não define dose/carga e não autoriza nova progressão automaticamente.
- Integrado às Homes do atleta e do profissional sem nova migration; schema permanece 38/38.

# v0.10.7 — Comparação entre Progressões

- Compara eventos realmente registrados apenas dentro do mesmo eixo esportivo.
- Mostra sequência, duração de observação, status e estado de resposta de até quatro eventos recentes por eixo.
- Eixos com somente um evento permanecem documentados sem comparação artificial.
- Não cria ranking de melhor/pior progressão, score de sucesso ou inferência causal.
- Não autoriza nova progressão automaticamente e não altera carga, volume, nutrição ou medicação.
- Integrado às Homes do atleta e do profissional sem nova migration; schema permanece 38/38.

## v0.10.6 r1 — alinhamento das sentinelas da timeline
- Explicita no HistoricoProgressaoService que a timeline nasce de **eventos realmente registrados** e persistidos.
- Explicita a trava **nao ranqueia eventos** com a mesma formulacao usada pela suíte de compatibilidade.
- Nenhuma alteracao funcional, de schema ou de versao semantica.

## v0.10.6 — Histórico de Progressões & Linha do Tempo Esportiva
- organiza eventos de progressão realmente registrados em uma timeline longitudinal;
- exibe aplicação, eixo, profissional, duração em observação, encerramento e observações;
- associa a interpretação longitudinal atual somente ao evento temporal correspondente;
- eventos antigos sem leitura comparável permanecem como histórico registrado, sem fabricar resposta retrospectiva;
- não ranqueia progressões e não cria score de melhor/pior resposta;
- sem nova migration: schema permanece 38/38.

## v0.10.5 — Interpretação Longitudinal da Resposta
- integra o comparativo pré/pós, o evento registrado e a reavaliação supervisionada;
- classifica o padrão observado como DadosInsuficientes, EmFormacao, Estavel, Favoravel, Misto ou Atencao;
- preserva prontidão, recuperação, dor, carga e volume como eixos independentes;
- não usa score composto e não transforma melhora temporal em causalidade;
- sinais clínicos de atenção prevalecem sobre leitura isolada de performance/carga;
- padrão favorável não libera nova progressão automática;
- sem nova migration: schema permanece 38/38.

## v0.10.4 — Comparativo Pré/Pós-Progressão
- usa o evento real da v0.10.3 como marco temporal;
- compara até 7 dias antes e depois em recuperação, dor, prontidão, carga interna e volume estimado;
- distingue janela em formação, dados insuficientes e janela comparável;
- associação temporal não é tratada como causalidade;
- não cria nova progressão, score causal ou prescrição automática;
- sem nova migration: schema permanece 38/38.


## v0.10.3 r3 — alinhamento das sentinelas PREPARAR 38/38
- Corrige smoke tests legados que ainda esperavam denominador `/37` nos upgrades v0.5.1–v0.6.4.
- Atualiza as verificações de total atual do PREPARAR para `38/38`, preservando a ordem histórica dos upgrades.
- Nenhuma regra funcional, migration ou versão semântica foi alterada.

## v0.10.3 r1 — migration EF do evento de progressão

- Corrige `PendingModelChangesWarning` do EF Core 10 no `PREPARAR.ps1`.
- O setup agora gera a migration incremental `V0103EventosProgressaoSupervisionada` antes do `database update` em bases existentes.
- O `ModelSnapshot` passa a ser atualizado pelo próprio `dotnet-ef`, mantendo o SQL `v0.10.3_eventos_progressao_supervisionada.sql` como proteção idempotente da etapa 38/38.
- Todas as etapas do preparo agora anunciam o total 38/38.

## v0.10.3 — Registro de Evento de Progressão
- adiciona marco temporal explícito para mudanças realmente aplicadas por profissional;
- novo endpoint para registrar/listar/encerrar eventos supervisionados;
- nova tabela EventosProgressaoSupervisionada e upgrade 38/38;
- home do atleta e profissional passa a distinguir sugestão de mudança realmente registrada;
- nenhuma causalidade é inferida automaticamente a partir do evento.


## v0.10.3 r2 — proteção contra mesclagem de fontes antigas
- PREPARAR.ps1 valida e restaura automaticamente os dois controllers da Home antes do build quando detectar o cabeçalho corrompido da r0.
- Inclui cópias canônicas em scripts/recovery e o utilitário CORRIGIR-FONTES-v0.10.3.ps1 para reparo manual determinístico.
- Nenhuma alteração funcional, de schema ou de versão semântica.


## v0.10.3 r1 — correção de compilação das Homes

- Corrigidos os cabeçalhos `using` de `MeuPortalPacienteController` e `PortalPacienteController`, corrompidos durante a integração da reavaliação de progressão.
- `reavaliacaoProgressao` agora é passado explicitamente ao `PortalPacienteHomeResponse` nas duas Homes.
- Sem alteração de schema, migration ou regra funcional da v0.10.3.
# Changelog

## v0.10.3 — Reavaliação da Progressão & Decisão de Continuidade
- transforma o monitoramento em decisão transparente: Manter, Revisar, Encerrar ou AguardarDados;
- encerramento significa apenas finalizar a janela atual de observação, sem inferir sucesso causal;
- revisão clínica prevalece sobre performance favorável;
- falta de dado não vira falha, cobrança ou progressão automática;
- integração nos portais do atleta e profissional, sem nova migration.

## v0.10.1 — Monitoramento de Resposta à Progressão
- adiciona leitura transparente de recuperação, carga e performance ao redor do plano supervisionado;
- estados Estável, Observar, Revisar e DadosInsuficientes, sem score opaco;
- revisão clínica prevalece sobre qualquer novo avanço;
- não atribui causalidade sem registro temporal da mudança e não prescreve nova progressão;
- integração nos portais do atleta e profissional, sem nova migration.

﻿﻿# v0.10.0 — Plano de Progressão Supervisionada

- adiciona `PlanoProgressaoSupervisionadaService`;
- integra atleta e profissional nas duas Homes;
- estrutura critérios transparentes para discussão de uma única progressão;
- preserva bloqueios de recuperação, carga e dados insuficientes;
- não cria meta, carga, volume, calorias ou medicação automaticamente;
- sem migration; schema permanece 37/37;
- suíte ampliada para 920 verificações.

# v0.9.9 — Decisão Assistida de Progressão

- adiciona `DecisaoProgressaoService` depois da Janela de Progressão;
- organiza até 3 opções transparentes e sugere apenas um eixo para discussão por vez;
- considera prioridade do ciclo e perfil/objetivo esportivo sem produzir prescrição automática;
- janela fechada, recuperação/carga em revisão ou dados insuficientes continuam bloqueando nova exigência;
- não define carga, volume, calorias, medicação nem cria meta automaticamente;
- integrado às Homes do atleta e profissional sem migration; schema permanece 37/37.

# v0.9.8 — Janela de Progressão & Critério de Avanço

- Nova camada `JanelaProgressaoService` com critérios transparentes e independentes para considerar uma progressão.
- Expõe base comportamental, ausência de oscilação, recuperação e carga recente sem condensar os eixos em score opaco.
- Recuperação/carga em atenção alta bloqueiam progressão; dados insuficientes viram observação, nunca reprovação.
- Uma janela aberta apenas autoriza considerar uma progressão pequena; não cria meta, aumenta carga ou altera plano automaticamente.
- Integrado às Homes do atleta e profissional sem nova migration; schema permanece 37/37.

# v0.9.7 — Reentrada Gradual de Desafio

- avalia quando a rotina em manutencao tem espaco para um unico novo desafio;
- retomada, habitos oscilando ou consolidando bloqueiam aumento prematuro de exigencia;
- elegibilidade nao cria meta automaticamente, nao altera prescricao e nao cria score opaco;
- cards equivalentes para atleta e profissional;
- sem migration nova; schema permanece 37/37.

# Changelog

## v0.9.7-r1 — Hotfix de build local

- `PREPARAR.ps1` encerra automaticamente uma instância local antiga da `HealthPlatform.Api` antes do `dotnet build`.
- Evita falhas `MSB3021/MSB3027` causadas por DLLs bloqueadas pelo processo da API ainda em execução.
- Não altera schema, versão semântica ou regras funcionais da v0.9.7.

## v0.9.7 — Encerramento do Ciclo de Hábito & Transição para Manutenção

- adiciona leitura de encerramento do foco de hábito;
- diferencia foco em curso, consolidando, pronto para manutenção, manutenção e proteção;
- foco encerrado sai do destaque sem disparar automaticamente outra meta;
- proteção de retomada continua acima de qualquer transição de foco;
- sem XP negativo, punição de streak, score opaco ou alteração automática de prescrição;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 888 verificações.

# v0.9.5 — Revisao do Proximo Foco & Ciclo de Habito

- revisa o foco atual em Continuar, Consolidar, Manutencao ou Proteger;
- usa a tendencia semanal correspondente ao eixo do foco;
- impede troca precoce de foco e aumento automatico de exigencia;
- integra atleta e profissional sem migration nova.

# Changelog

## v0.9.4 — Manutencao Sustentavel & Proximo Foco

- adiciona um proximo foco unico para a rotina, priorizando eixos oscilando antes dos que ainda consolidam;
- habitos estaveis passam a ficar explicitamente em modo de manutencao, sem aumento automatico de cobranca;
- protecao de retomada/recuperacao tem precedencia sobre qualquer novo foco;
- integra a mesma leitura nas Homes do atleta e do profissional;
- nao cria score opaco, nao pune XP/streak e nao altera prescricao automaticamente;
- schema permanece 37/37, sem migration nova;
- suite ampliada para 872 verificacoes.

# v0.9.3 — Estabilidade de Hábitos & Consolidação da Rotina

- Nova leitura de estabilidade por comportamento: estável, consolidando, oscilando ou sem dados.
- Eixos transparentes: consistência, dias ativos, nutrição, hidratação e contexto de recuperação.
- Hábitos estáveis não geram cobrança extra; oscilações recebem próxima ação simples e sustentável.
- Sem score opaco, XP negativo, punição de streak ou alteração automática de prescrição.
- Sem migration; schema permanece 37/37.

# v0.9.2 — Proteção da Retomada & Continuidade

- adiciona `ProtecaoRetomadaService` para reconhecer retomada recente por streak curto, aumento de dias ativos e consistência ainda em consolidação;
- protege descanso/redução de ritmo coerentes com recuperação contra falso rótulo de recaída;
- limita a leitura a até 3 sinais e não cria score/probabilidade de recaída;
- adiciona cards para atleta e profissional;
- sem XP negativo, punição de streak ou compensação;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 856 verificações.

## v0.9.1 — Plano de Reconexão & Retomada Sustentável
- Converte sinais do Radar de Adesão em até 3 passos pequenos e auditáveis de retomada.
- Protege descanso/redução de ritmo quando há contexto de recuperação.
- Usa o roteiro do dia e prioridades já existentes; não cria prescrição paralela.
- Sem XP negativo, punição de streak, treino dobrado ou restrição compensatória.
- Integração atleta + profissional.
- Sem migration; schema permanece 37/37.
- Suíte ampliada para 848 verificações.


### v0.9.0 r1 — correção de compilação do Radar de Adesão

- corrige `RadarAdesaoService` para usar `PortalHidratacaoContextualResponse.MetaMl`, nome real do contrato de hidratação;
- adiciona sentinela estática para impedir novo desalinhamento entre o radar e o DTO de hidratação;
- sem alteração de schema, migration ou regra funcional do radar.
# HealthPlatform v0.9.0 — Radar de Adesão & Continuidade do Plano

- Novo radar de adesão/continuidade com sinais auditáveis.
- Consistência, ritmo semanal, nutrição e hidratação como eixos separados.
- Contexto de recuperação impede que descanso adequado seja interpretado como falha.
- Sem probabilidade de abandono, score opaco, XP negativo ou punição.
- Integração atleta + profissional.
- Sem migration; schema permanece 37/37.
- Suíte ampliada para 840 verificações.

# HealthPlatform v0.8.9 — Tendência Semanal & Comparativo de Semanas

- compara a semana atual com o mesmo número de dias da semana anterior;
- compara ritmo de treinos, dias ativos, prontidão média, carga interna estimada, adesão nutricional registrada e hidratação registrada;
- semanas parciais não são comparadas injustamente com semanas completas;
- carga/frequência maiores não são rotuladas automaticamente como melhores;
- sem score semanal composto, diagnóstico ou alteração automática de prescrição;
- mesma leitura para atleta e profissional;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 832 verificações.

# HealthPlatform v0.8.8 — Resumo Semanal & Fechamento da Semana

- Nova síntese semanal derivada do planejamento, recuperação, carga, nutrição, hidratação e execução diária.
- Estados transparentes: Executando, Equilibrada, Observar e Revisar.
- Sem score único, sem diagnóstico e sem alteração automática de prescrição.
- Integração na Home do atleta e no prontuário profissional.
- Sem migration nova; schema permanece 37/37.
- Suíte ampliada para 824 verificações.

# HealthPlatform v0.8.7 — Planejamento Semanal Adaptativo

- organiza até 5 focos da semana usando prioridades, metas, checkpoint e Estratégia do Dia;
- recuperação/carga têm precedência sobre perseguir volume semanal;
- mostra treinos concluídos/restantes apenas como referência da meta do ciclo;
- não cria exercícios, não redistribui carga e não altera prescrição automaticamente;
- sem migration; schema permanece 37/37;
- suíte ampliada para 816 verificações.

# Changelog

## v0.8.7 — Ações Prioritárias do Ciclo
- Consolida tendência por objetivo, checkpoint, recuperação, carga, nutrição e hidratação em até 3 prioridades semanais.
- Recuperação e equilíbrio de carga têm precedência sobre metas de volume/performance quando há sinais de atenção.
- Cada prioridade informa motivo e ação, sem score opaco e sem alterar prescrição automaticamente.
- Cards equivalentes para atleta e profissional.
- Sem migration nova; schema permanece 37/37.
- Suíte ampliada para 808 verificações.

# HealthPlatform v0.8.5 — Tendência por Objetivo do Ciclo

- Nova leitura contextual por perfil esportivo (hipertrofia, força/performance, emagrecimento, corrida/condicionamento, qualidade de vida ou personalizado).
- Prioriza eixos relevantes ao objetivo sem criar score único.
- Peso continua descritivo e só ganha direção quando existe peso-alvo definido pelo profissional.
- Integração nas Homes do atleta e do profissional.
- Sem migration nova; schema permanece 37/37.
- Suíte ampliada para 800 verificações.

# v0.8.5 — Comparativo de Ciclos & Tendência de Longo Prazo

- compara até quatro ciclos esportivos usando taxas semanais, não apenas totais brutos;
- apresenta treinos/semana, check-ins/semana, prontidão média e variação de peso por ciclo;
- compara o ciclo mais recente com o anterior sem criar score composto;
- variação de peso permanece descritiva, sem classificação automática de bom/ruim;
- mesma leitura para atleta e profissional;
- sem migration nova; schema permanece 37/37;
- suíte ampliada para 792 verificações.

## v0.8.5-r1

- Corrige sentinelas correntes do TESTAR.ps1 para esperar API e identidade MVP Preview v0.8.5.
- Mantém validações históricas da v0.8.2 intactas.

# v0.8.5 — Fechamento de Ciclo & Relatório de Evolução

- Adiciona `RelatorioCicloService` como síntese longitudinal do ciclo ativo.
- Consolida volume realizado, metas mensuráveis, evolução multidimensional e checkpoint.
- Introduz estados `EmCurso`, `Evoluindo`, `Revisar` e `ProntoParaFechamento`.
- Adiciona cards de relatório na Home do atleta e no prontuário profissional.
- Mantém cada dimensão auditável, sem score clínico composto.
- Não cria automaticamente próximo ciclo e não altera prescrição.
- Mantém schema 37/37, sem migration nova.
- Amplia a suíte para 784 verificações.

---

# v0.8.2 — Revisão de Ciclo & Checkpoint de Progresso

- Adicionado `CheckpointCicloService`.
- Novo contrato `PortalCheckpointCicloResponse` com evidências transparentes.
- Integração nas Homes do atleta e profissional.
- Estados de acompanhamento: EmCurso, Evoluindo, Revisar e Consolidar.
- Nenhum score composto opaco e nenhuma alteração automática da prescrição.
- Sem migration; PREPARAR permanece 37/37.
- TESTAR ampliado para 776 verificações e preservado em UTF-8 BOM.

# v0.8.1 — Metas do Ciclo & Progresso por Objetivo

- Acompanha metas mensuráveis já definidas no ciclo esportivo: treinos semanais, consistência e peso-alvo.
- Treinos da semana usam apenas sessões concluídas dentro da semana atual do ciclo.
- Peso-alvo usa distância absoluta ao alvo, funcionando tanto para perda quanto para ganho de peso.
- Objetivo textual do profissional permanece informativo e não é convertido em score opaco.
- Home do atleta e prontuário profissional recebem o mesmo acompanhamento.
- Progresso não altera automaticamente carga, dieta ou prescrição.
- Sem migration; PREPARAR permanece 37/37.
- Suíte ampliada para 768 verificações.

## v0.8.0 r11 — compatibilidade dos testes de volume

- Corrige sentinelas históricas `[429/600]`, `[430/600]` e `[431/600]` para validar a estrutura do painel de volume sem depender de rótulos acentuados sujeitos a diferenças de encoding no PowerShell.
- Nenhuma alteração funcional, de schema ou de versão semântica.


### v0.8.0-r8 — compatibilidade de testes dos gráficos de check-in
- Ajustada a sentinela `[372/600]` para validar os campos estruturais `adesaoAlimentacaoPercentual` e `adesaoTreinoPercentual`, evitando falsos negativos de encoding em rótulos acentuados.
- Nenhuma alteração funcional, de schema ou de versão semântica.


### v0.8.0 r6 — compatibilidade dos testes de gráficos de hábitos
- Corrige a sentinela `[314/600]` para validar `hpHabitCharts`, `hpLineChart`, `sonoHorasMedia` e `aguaLitrosDia` sem depender de literais acentuados no PowerShell.
- Nenhuma alteração funcional, de schema ou de versão semântica.
## v0.8.0-r2 — Hotfix de login local

- Em `Development`, o bootstrap do admin demo agora zera `AccessFailedCount` e remove `LockoutEnd` antes de validar/sincronizar a senha.
- Corrige `401` persistente após repetidas tentativas de smoke test com credenciais antigas.
- A alteração é restrita ao fluxo local de desenvolvimento; produção continua respeitando o lockout normal do Identity.

## v0.8.0-r1 — Hotfix de credencial local

- O runner local agora força `Seed__AdminEmail` e `Seed__AdminPassword` a partir do `appsettings.json` antes de iniciar a API.
- Evita `401` no `TESTAR.ps1` quando uma variável de ambiente antiga `Seed__AdminPassword` sobrescrevia a credencial local esperada.
- Sem mudança de schema ou versão semântica: `VERSION.txt` permanece `0.8.0`.

# v0.8.0 — Painel de Evolução Esportiva

- Consolida consistência, recuperação, carga, performance, nutrição, hidratação e ciclo em um painel longitudinal.
- Mantém cada indicador separado, com valor, referência, tendência e explicação; não cria score esportivo opaco.
- Integra o painel à Home do atleta e ao prontuário profissional.
- Sem migration; PREPARAR permanece 37/37.
- Suíte ampliada para 760 verificações.


### v0.7.9-r1 — correção de sentinela do MVP Preview
- Corrige o teste legado `[463/600]`, que ainda esperava `MVP Preview • v0.7.8` apesar da UI corrente já estar em `v0.7.9`.
- Nenhuma alteração de schema ou regra funcional.

# v0.7.9 — Hidratação Contextual & Balanço do Dia

- Resume meta hídrica ativa, consumo registrado e progresso do dia.
- Usa duração e RPE do treino apenas como contexto, sem aumentar automaticamente a meta.
- Integra hidratação ao Coach Diário e às Homes do atleta e do profissional.
- Reutiliza metas/registros existentes; sem migration nova.
- PREPARAR permanece 37/37; suíte ampliada para 752 verificações.

﻿
### v0.7.8 r1 — alinhamento de sentinelas de versão
- Corrige TESTAR.ps1 para esperar API 0.7.8 no healthcheck.
- Alinha Swagger, banner de login, log de bootstrap e scripts/run.ps1 com 0.7.8.
- Sem alteração de schema ou regra funcional de adesão nutricional.
# v0.7.8 — Adesão Nutricional Contextual

- Nova síntese diária de adesão às refeições do plano ativo.
- Paciente registra refeição como Realizada, Adaptada ou Não realizada.
- Registros idempotentes por refeição/dia no Diário; nenhuma migration nova.
- Coach Diário considera ausência/padrão de registros sem prescrever compensações.
- Home do atleta e prontuário profissional recebem card nutricional.
- PREPARAR permanece 37/37.
- Suíte ampliada para 744 verificações.

# v0.7.7 — Plano de Recuperação Contextual

- Adiciona síntese diária de recuperação baseada em prontidão, dor localizada, tendência de recuperação, carga, estratégia e execução.
- Prioriza proteção de região dolorosa, sono, consolidação de carga, hidratação e encerramento do dia quando aplicável.
- Não diagnostica lesão, não prescreve tratamento e não altera automaticamente a prescrição profissional.
- Integra a leitura na Home do atleta e no prontuário profissional.
- Sem migration nova; schema permanece 37/37.


### v0.7.6 r2 — correção de consistência do resumo de dor
- Alinha a variável interna `impactoMaximoTreino7` ao contrato `ImpactoMaximoTreino7`, preservando a mesma regra de cálculo.
- Corrige a validação estática `[722/728]` sem alterar schema, API ou comportamento clínico.
# v0.7.6 — Dor por Região Corporal

- Registro de região, lado, intensidade e impacto no treino usando o Diário existente.
- Resumo longitudinal de 7 dias no portal do atleta e prontuário profissional.
- Coach Diário passa a priorizar dor localizada relevante sem diagnosticar lesão.
- Sem migration nova; schema permanece 37/37.

# HealthPlatform v0.7.5 — Seed Ana v7 & Variáveis Automáticas PowerShell

- Hotfix r1: corrige as sentinelas [697] e [705] da suíte que ainda procuravam o banner legado v6 em vez do seed corrente v7.

- Corrige o seed da Ana para não sobrescrever `$HOME`, variável automática somente leitura do PowerShell.
- O resumo final agora usa `$homeResumo`.
- Adiciona proteção de regressão para `HOME`, `PID`, `PROFILE`, `HOST` e `PSHOME`.
- Mantém os 56 dias de histórico, avaliações corporais, carga, performance, gamificação e Coach Diário.
- Sem alteração de schema; PREPARAR permanece 37/37.
- Suíte ampliada para 720 verificações.

# v0.7.4 — Seed Ana v6 & Validação PowerShell
- Corrigido falso positivo da validação numérica: textos como `7d` dentro de strings não são mais tratados como sufixos de código.
- Resumo do seed agora usa `7 dias` explicitamente.

- Corrigido `POPULAR-ANA-RIBEIRO.ps1`: o literal `2.5m` era sintaxe C# e no PowerShell era interpretado como comando inexistente.
- Ajuste do deload agora usa `[decimal]2.5`.
- Seed da Ana atualizado para v6 mantendo 56 dias, Coach, carga, performance, prontidão, metas e avaliações corporais.
- Adicionadas verificações contra sufixos numéricos incompatíveis em scripts PowerShell.
- Sem migration nova; schema da v0.6.4 permanece suficiente.
- API/UI/VERSION atualizados para 0.7.4.
- Suíte ampliada para 712 verificações.

# v0.7.2 — Seed Resiliente & Diagnóstico de Dados

- Corrige o seed pesado da Ana Ribeiro que podia abortar com `400 Bad Request` sem revelar o endpoint ou a resposta da API.
- `Api()` agora inclui método, rota e corpo de erro retornado pelo backend.
- Metas demonstrativas passaram a ser opcionais: falha em uma meta não impede a população das demais camadas.
- Histórico de metas é protegido contra recurso ausente e falha individual.
- Seed acumula avisos não fatais e mostra um resumo ao final.
- Mantém URL local 5180, 56 dias de histórico e proteção contra `$PID`.
- `VERSION.txt`, API, Swagger e UI atualizados para 0.7.2.
- Sem migration nova; `PREPARAR` permanece 37/37.
- Suíte: 696 verificações.

# v0.7.1 — Coach Diário & Prioridades Contextuais

- Adiciona `CoachDiarioService` como camada derivada e explicável.
- Combina prontidão, estratégia, recuperação, carga, performance, execução e ciclo.
- Prioriza até três ações do dia com motivo e ação recomendada.
- Inclui travas explícitas contra diagnóstico, prescrição paralela e progressão automática de carga.
- Exibe síntese para atleta e leitura equivalente no prontuário profissional.
- Mantém o schema em 37/37; nenhuma migration nova.
- Amplia a suíte de 672 para 680 verificações.
- Atualiza `POPULAR-ANA-RIBEIRO.ps1` para a linha 0.7.x.

# v0.6.9-r5

- Corrige `VERSION.txt` para a versao semantica `0.6.9`, preservando revisoes apenas em logs/changelog.
- Mantem o runner local fixado em `http://localhost:5180` e a sincronizacao de credencial local.

# v0.6.9-r4

- Corrige o runner local para fixar explicitamente `http://localhost:5180` mesmo usando `--no-launch-profile`.
- Mantém Development explícito e sincronização da credencial local do admin.
- Evita divergência entre a porta real da API e TESTAR/DEMO/POPULAR.

## v0.6.9-r3 — correção de bootstrap local

- Corrige sincronização da senha do admin também no fluxo Development.
- RODAR.ps1 força Development explicitamente e evita instância dotnet antiga na porta 5180.
- Mantém a versão funcional/API em 0.6.9 e a suíte em 672 verificações.

## v0.6.9-r1 — Hotfix da linha de base + seed Ana Ribeiro

- Corrige a linha de base de carga para três janelas semanais explícitas anteriores aos 7 dias atuais.
- Alinha `RelacaoCargaComBase` entre motor, contrato e teste 667/672.
- Adiciona `POPULAR-ANA-RIBEIRO.ps1` para gerar histórico esportivo rico e idempotente da paciente demo.

# v0.6.9 — Carga de Treino & Equilíbrio

- Adicionado `CargaTreinoService` com carga interna estimada por sessão (`duração × RPE`).
- Comparação dos últimos 7 dias com a média semanal das 3 semanas anteriores.
- Classificação observacional: DadosInsuficientes, ConstruindoBase, AbaixoDaBase, Coerente, AcimaDaBase e Revisar.
- Combinação contextual com prontidão, dor e recuperação recentes.
- Cards específicos para atleta e profissional, com linguagem não diagnóstica.
- Nenhuma migration: leitura calculada sobre execuções e prontidão já persistidas.
- Suíte ampliada para 672 verificações.

# v0.6.8 — Performance & PRs

- Linha de performance derivada diretamente das execuções reais de treino.
- Melhor carga por exercício, melhor volume estimado e evolução desde a primeira marca registrada.
- Detecção conservadora de novos PRs: exige marca anterior e considera PR recente apenas nos últimos 7 dias.
- Contexto de prontidão exibido no dia da melhor marca quando houver check-in correspondente.
- Comparação de volume estimado dos últimos 28 dias contra os 28 dias anteriores.
- Home do atleta com evolução esportiva e lembrete explícito de que PR não é meta diária.
- Prontuário profissional com performance, volume, PRs e contexto de recuperação.
- Sem nova migration: dados são calculados a partir do histórico já existente.
- PREPARAR permanece 37/37.
- Suíte ampliada para 664 verificações.

# v0.6.7 — Tendências de Recuperação & Alertas Inteligentes

- Nova leitura longitudinal de recuperação comparando os últimos 7 dias com os 7 dias anteriores.
- Combina prontidão, sono, dor, recuperação, energia, frequência de treinos e sessões com RPE 8+.
- Sinais transparentes para dor elevada, sono baixo, recuperação reduzida, carga intensa recente e queda de prontidão.
- Classificação conservadora: Dados Insuficientes, Estável, Melhorando, Observar ou Atenção.
- Home do atleta mostra tendência de recuperação sem linguagem diagnóstica.
- Prontuário profissional mostra os sinais e justificativas usados pelo motor.
- Nenhum alerta altera automaticamente a prescrição; decisão clínica permanece com o profissional.
- Sem mudança de schema; PREPARAR permanece 37/37.
- Suíte ampliada para 656 verificações.

# v0.6.6 — Execução Guiada

- Roteiro de Hoje derivado de dados reais do acompanhamento.
- Progresso diário com prontidão, treino, metas/hidratação e fechamento.
- Fechamento do dia com percepção 0–10 e reflexão breve.
- +20 XP apenas no primeiro fechamento do dia; atualização posterior não duplica XP.
- Leitura profissional da execução diária.
- Sem alteração de schema: reutiliza registros e ledger já existentes.

# v0.6.5 — Estratégia do Dia

- Adiciona `EstrategiaDiariaService`, combinando prontidão, ciclo esportivo e planos ativos.
- Cria orientação de intensidade, RPE, carga e volume com regra de segurança: nunca aumentar automaticamente acima da prescrição.
- Adiciona estratégia nutricional e hidratação contextual sem alterar quantidades prescritas.
- Home do atleta e prontuário profissional exibem a Estratégia do Dia.
- Sem mudança de schema; PREPARAR permanece 37/37.
- Suíte ampliada para 640 verificações.

# v0.6.4 — Ciclos Esportivos

- Novo `CicloEsportivoPaciente` para organizar objetivo, perfil esportivo, período e metas do ciclo.
- Ciclos podem vincular fases de treino e nutrição e mantêm apenas um ciclo ativo por paciente via fluxo profissional.
- Home Daily Athlete mostra semana atual, progresso temporal, treinos acumulados e prontidão média do ciclo.
- Leitura profissional recebe o mesmo ciclo em formato longitudinal e clínico.
- Missão semanal de treinos passa a respeitar `MetaTreinosSemanais` do ciclo ativo, com fallback seguro para 3.
- Upgrade `V064CiclosEsportivos`, SQL idempotente e PREPARAR 37/37.
- Suíte ampliada para 632 verificações.

# v0.6.3 — Missões & Conquistas

- Desafios semanais automáticos baseados em comportamento realmente registrado: treinos, prontidão e dias ativos.
- Recompensas de desafio entram no ledger de XP com idempotência; não existe botão manual para fabricar progresso.
- Conquistas persistentes para marcos esportivos e de autocuidado, como primeiro treino, 10 treinos, 7 check-ins e consistência 80+.
- Home Daily Athlete passa a mostrar progresso das missões e conquistas recentes.
- Nova entidades `DesafioSemanalPaciente` e `ConquistaPaciente`, upgrade `V063MissoesConquistas` e SQL idempotente.
- PREPARAR passa a 36/36 e suíte sobe para 624 verificações.

# HealthPlatform Changelog

## v0.6.2 — XP & Consistência

- Ledger auditável `EventosXp`, com idempotência por paciente + fonte + registro de origem.
- XP de autocuidado para check-in de prontidão e conclusão de metas.
- XP de treino baseado na adequação entre RPE executado e recomendação diária; sobrecarga deliberada recebe menos XP.
- Níveis de 500 XP, XP do dia, streak e score de consistência baseado em frequência sustentável nos últimos 14 dias.
- Nova faixa de evolução esportiva na Home do paciente e resumo de consistência para leitura profissional.
- Upgrade incremental `V062XpConsistencia`, SQL idempotente e PREPARAR 35/35.
- Suíte ampliada para 616 verificações.


## v0.6.1 — Daily Athlete / Prontidão Diária
**Tipo:** Feature release / início da linha Sports Medicine + Daily Care

- novo check-in matinal PatientOnly com sono, qualidade do sono, energia, dor, disposição e recuperação;
- score de prontidão de 0 a 100 com ponderação fisiológica simples e transparente;
- contexto do treino anterior entra no cálculo quando houve esforço percebido alto recentemente;
- travas de segurança impedem score artificialmente alto com dor elevada, recuperação muito baixa ou sono muito curto;
- recomendação diária entre Recuperação, Leve, Normal e Pesado, sem assumir que maior intensidade é sempre melhor;
- Home do paciente passa a destacar a prontidão e permitir atualização diária rápida;
- prontuário profissional mostra o mesmo dado em leitura objetiva diária, persistido para análise longitudinal;
- nova entidade `ProntidaoDiaria`, migration incremental `V061ProntidaoDiaria` e SQL idempotente;
- PREPARAR ampliado para 34/34;
- prepara a base para XP, níveis, streaks e desafios alinhados à adequação do comportamento, não ao excesso.


## v0.6.0-r1 — Hotfix da suíte de upgrade

- Corrige validações históricas do `TESTAR.ps1` que ainda tratavam `32/32` como a contagem atual do `PREPARAR`.
- A contagem anterior era `33/33` após a inclusão do upgrade de medicamentos da v0.6.0; na v0.6.1, a contagem atual é `34/34` com o upgrade de prontidão diária.
- Preserva as checagens dos upgrades históricos v0.5.1 e v0.5.8.
- Nenhuma alteração de schema, API ou regra clínica neste hotfix.

## v0.6.0 — Medicamentos e Adesão
**Tipo:** Feature release / novo marco Connected Care

- medicamentos estruturados por paciente;
- registro de tomadas pelo portal;
- aderência de 30 dias no prontuário;
- auditoria, PatientOnly e multi-tenant;
- migration incremental `V060MedicamentosAdesao`;
- SQL idempotente `v0.6.0_medicamentos.sql`;
- PREPARAR 34/34.

# v0.5.9-r1 — Hotfix de validações do protocolo

- corrige o teste 577 para reconhecer a rota do protocolo com `offsetMinutos`, introduzida pela aderência baseada no dia local;
- corrige o teste 578 para validar o upgrade histórico correto da v0.5.8 (`v0.5.8_protocolos_acompanhamento.sql`);
- não altera banco, schema, API ou regra clínica;
- mantém o fluxo oficial `PREPARAR -> RODAR -> TESTAR`.

# v0.5.9 — Aderência aos Protocolos
**Tipo:** feature release

- adiciona cálculo de aderência aos protocolos configuráveis;
- mostra ao paciente o que estava previsto e o que já foi concluído hoje;
- mostra ao profissional aderência consolidada dos últimos 7 dias;
- respeita frequência diária, dias da semana, semanal e sob demanda;
- considera pressão concluída apenas com sistólica + diastólica;
- respeita timezone local por offset do navegador;
- sem schema ou migration nova;
- suíte ampliada de 580 para 588 verificações.


## v0.5.8-r1 - Hotfix de validacao do PREPARAR
- Corrige o teste 517/580 para reconhecer corretamente o upgrade v0.5.1 na etapa 31/32.
- Preserva a etapa 32/32 para o upgrade v0.5.8 de protocolos de acompanhamento.
- Nenhuma alteracao de schema, API, regra clinica ou dados.
# v0.5.7 — Monitoramento Guiado
**Tipo:** feature release

- adiciona registros guiados de sinais vitais e sintomas ao Hoje do paciente;
- estrutura pressão arterial em sistólica/diastólica sem criar nova tabela;
- adiciona glicemia, frequência cardíaca, saturação e temperatura aos registros rápidos;
- adiciona resumo profissional dos últimos 7 dias com último valor, média, mínimo e máximo;
- adiciona endpoints profissional e PatientOnly de monitoramento;
- reutiliza o diário existente, sem schema ou migration nova;
- suíte ampliada de 561 para 570 verificações.

# v0.5.6 — Jornada do Paciente
**Tipo:** feature release

- adiciona timeline própria no portal do paciente;
- consolida consultas, avaliações, exames, metas, diário, check-ins, treinos e solicitações;
- adiciona filtro de período e atalhos contextuais;
- mantém dados clínicos internos/ SOAP fora da visão simplificada do paciente;
- preserva `PatientOnly` e isolamento organizacional;
- sem schema novo e sem migration nova;
- suíte ampliada de 553 para 561 verificações.

# v0.5.5 — Desde a última consulta
**Tipo:** feature release

- adiciona resumo longitudinal ao prontuário profissional;
- usa a última consulta como início do período, com fallback seguro de 30 dias;
- agrega registros do paciente, treinos, check-ins e médias de adesão;
- mostra peso inicial/atual e variação quando houver avaliações suficientes;
- consolida solicitações pendentes e respostas aguardando revisão;
- sem schema novo e sem migration nova.

# Changelog


## v0.5.4 — Feature release

- Integra Solicitações Clínicas ao **Hoje** do paciente.
- Solicitações pendentes passam a contar como atividades do acompanhamento diário.
- Adiciona destaque e resposta direta para solicitações vencidas/pendentes.
- Integra respostas aguardando revisão e solicitações vencidas à **Central do Dia** profissional.
- Dashboard profissional passa a destacar solicitações para revisão.
- Sem migration/schema novo; reutiliza o schema v0.5.1.
- Suíte ampliada de 535 para 545 verificações.

## v0.5.3 — Feature release

- Adiciona Central profissional de Solicitações Clínicas.
- Consolida solicitações de todos os pacientes em uma única fila operacional.
- Adiciona busca por paciente/título/tipo e filtros por status/prazo.
- Exibe métricas de pendentes, respostas para revisão e vencidas.
- Permite revisar, cancelar e abrir o prontuário diretamente da central.
- Preserva isolamento multi-tenant e reutiliza o schema v0.5.1.
- Sem migration/schema novo.
- Suíte ampliada de 526 para 535 verificações.

## v0.5.2 — Feature release

- Integra Solicitações Clínicas ao motor de notificações internas.
- Paciente recebe lembretes contextuais com prioridade baseada no prazo.
- Profissional recebe aviso quando uma solicitação é respondida.
- Navegação contextual: paciente → Solicitações; profissional → prontuário do paciente.
- Notificações são desativadas automaticamente quando o estado deixa de exigir ação.
- Sem schema novo.
- Suíte ampliada de 518 para 526 verificações.

## v0.5.1-r1 — Hotfix de migration

- Corrige o `PREPARAR.ps1` em instalações que já possuem a migration baseline da v0.5.0.
- Gera e normaliza a migration incremental `V051SolicitacoesClinicas` antes de `database update`.
- Mantém instalações novas usando a `InitialCreate` atual, sem migration incremental redundante.
- Não suprime `PendingModelChangesWarning`, não recria o banco e não altera dados existentes.
- Mantém o upgrade SQL v0.5.1 idempotente como camada adicional de compatibilidade.

## v0.5.1 — Solicitações Clínicas
**Tipo:** feature release.

- adiciona solicitações clínicas profissional → paciente;
- adiciona resposta/conclusão pelo portal do paciente;
- adiciona revisão/cancelamento profissional;
- adiciona auditoria e isolamento multi-tenant;
- adiciona schema idempotente e etapa 31/31 no PREPARAR;
- adiciona cobertura estrutural à suíte de fumaça.

# HealthPlatform — Changelog

## v0.5.8 — Protocolos de Acompanhamento
**Tipo:** feature

- adiciona protocolo configurável por paciente;
- permite definir métrica, frequência, horário, unidade e instruções;
- paciente vê registros rápidos filtrados pelo protocolo ativo;
- profissional visualiza e gerencia protocolo junto ao monitoramento remoto;
- adiciona schema `ProtocolosAcompanhamento` e migration incremental compatível com upgrades;
- amplia smoke test para 580 verificações.


## v0.5.0 — Connected Care Stable Milestone
**Tipo:** marco de versão / promoção estável

- promove a v0.4.6, validada com 508/508 testes, como nova baseline;
- consolida a experiência Patient Today introduzida no ciclo v0.4.x;
- mantém identidade visual RS e UX responsiva para desktop, tablet e mobile;
- atualiza runtime, healthcheck, Swagger, interface e scripts para 0.5.0;
- não adiciona schema, migration, regra clínica ou mutação de dados.

## Ciclo v0.4.x — resumo real

### v0.4.0 — Connected Care / Patient Today
**Tipo:** feature

- nova experiência diária do paciente;
- registros rápidos de peso, água, sono, dor, energia e sintomas;
- integração com metas, próxima consulta, plano alimentar, exames e evolução;
- reutilização do `RegistroDiarioPaciente`, sem criar estrutura paralela.

### v0.4.0-r1
**Tipo:** hotfix de teste

- atualiza o smoke test do `POPULAR-REMOTO-RICO.ps1` para a implementação V2 atual;
- sem alteração funcional ou de banco.

### v0.4.1
**Tipo:** promoção técnica

- promoção da baseline 0.4.0-r1;
- sem funcionalidade clínica nova.

### v0.4.2
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.3
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.4
**Tipo:** promoção técnica

- promoção da baseline validada;
- sem funcionalidade clínica nova.

### v0.4.4-r1
**Tipo:** hotfix de teste

- corrige a validação 60/508 para não depender de texto acentuado no Windows PowerShell;
- passa a validar rota e estrutura reais do histórico de treino;
- sem alteração funcional ou de banco.

### v0.4.5
**Tipo:** promoção técnica

- promoção da baseline 0.4.4-r1 validada com 508/508;
- sem funcionalidade clínica nova.

### v0.4.6
**Tipo:** promoção técnica

- promoção da baseline 0.4.5 validada com 508/508;
- sem funcionalidade clínica nova.

## Próximo ciclo funcional

O ciclo v0.5.x será usado para evoluções reais do Connected Care, priorizando a ponte profissional ↔ paciente. As próximas entregas devem privilegiar módulos como solicitações clínicas, tarefas/acompanhamento, protocolos configuráveis e comunicação contextual, e cada release deverá declarar explicitamente o que entrou.

### v0.5.4-r1 — Hotfix de testes
- Corrige validações do TESTAR.ps1 que dependiam de textos acentuados servidos via HTTP.
- As verificações agora usam rotas, funções e seletores técnicos estáveis.
- Nenhuma alteração de schema, API, regra clínica ou dados.

### v0.7.5 r2 — correção da sentinela de schema
- Corrige o teste 718/720 para validar a etapa 37/37 em `scripts/setup.ps1`, que é onde o PREPARAR delega os upgrades.
- Nenhuma alteração de schema, migration ou comportamento funcional.

### v0.8.0 r3 — compatibilidade do teste de handoff
- A sentinela histórica do handoff clínico deixou de depender do literal acentuado `RESUMO CLÍNICO`, evitando falso negativo de encoding no Windows PowerShell.
- A validação continua exigindo a função `hpClinicalSummaryText` e os blocos estruturais `AGENDA` e `ACOMPANHAMENTO`.
- Sem alteração de schema ou versão semântica.

### v0.8.0 r4 — Compatibilidade do teste de inserção visual
- Corrige a sentinela `[293/600]` para validar a estrutura real da inserção rápida de refeição sem depender de literal acentuado.
- Preserva `openMealLibraryInsertForm`, endpoint de inserção, retorno à aba de alimentação e fechamento do modal.
- Nenhuma alteração de schema ou regra funcional.

### v0.8.0 r5 — compatibilidade do teste de inserção visual de sessão
- Corrige a sentinela `[305/600]` para validar a estrutura funcional da inserção de sessão de treino sem depender de literal acentuado no toast.
- Mantém a versão funcional `0.8.0`, schema e funcionalidades inalterados.

### v0.8.0 r7
- Corrigida sentinela historica do formulario de fase nutricional para validar a estrutura do formulario sem depender do literal acentuado "Manutencao".

- r9: teste [385/600] de destaques de fases passou a validar campos estruturais sem depender de strings acentuadas.

### v0.8.0 r12 — compatibilidade dos testes de progressão
- Corrige sentinelas históricas [444-446] para validar estrutura real do painel profissional, gráficos/recordes e portal do paciente.
- Remove dependência de rótulos acentuados nas validações estáticas de progressão por exercício.
- Sem alteração de schema, API funcional ou versão semântica.

- r13: sentinelas [458-460] de sinais de progressao agora validam estrutura/funcoes/campos em vez de literais acentuados.

### v0.8.0 r14 — compatibilidade de encoding dos testes
- `TESTAR.ps1` passa a ser distribuído em UTF-8 com BOM para Windows PowerShell 5.1 interpretar corretamente literais Unicode (acentos e `•`).
- Corrige a causa sistêmica dos falsos negativos em sentinelas visuais/identidade sem alterar regras de negócio, schema ou versão funcional.


### v0.8.8-r1 — Correção das sentinelas de versão
- TESTAR.ps1 alinhado à versão corrente 0.8.8 em VERSION.txt, HP_MVP_VERSION e /api/health.
- Testes históricos e schema 37/37 preservados.

### v0.8.9 r1 — correção de compilação da tendência semanal
- Corrige o cálculo de hidratação comparativa filtrando apenas registros com valor e materializando `decimal` não anulável antes do `Average`.
- Elimina o erro CS1503 em `TendenciaSemanalService` sem alterar regra de negócio, schema ou versão semântica.
- r1: alinhada a trava textual do encerramento de hábito para explicitar que o sistema **não gera nova meta automática** ao consolidar um foco.
## v0.9.8 r1 — correção da sentinela inicial de versão

- Corrige o healthcheck inicial do `TESTAR.ps1`, que ainda exigia `0.9.7` apesar da API e do pacote já estarem em `0.9.8`.
- Preserva as referências históricas de compatibilidade da v0.9.7.
- Mantém `VERSION.txt` em `0.9.8`, sem alteração de schema ou migration.


### v0.11.0 r1 — alinhamento das travas clínicas do painel
- Explicita no `PainelMedicinaEsporteService` as sentinelas `nao produzir diagnostico`, `previsao de lesao` e `prescricao automatica`, preservando a lógica funcional e a segurança clínica já existentes.

## v0.13.3 — Mobile First Experience
- Reestruturação mobile-first do portal do atleta, priorizando uso diário por celular.
- Navegação principal do paciente convertida em dock inferior com swipe horizontal e safe-area para iOS/Android.
- Cabeçalho mobile mais compacto e contrastado, com identidade visual de performance.
- Cards, métricas, prontidão, gamificação e ações rápidas reorganizados para leitura e toque em telas pequenas.
- Tabelas passam a se comportar como cards no mobile, reduzindo dependência de rolagem horizontal.
- Formulários em coluna única, controles com alvo mínimo de 44px e inputs de 16px para evitar zoom automático no iOS.
- Ajustes específicos para telas abaixo de 420px.
- Sem alteração de schema; permanece 38/38.


## v0.13.6 — Mobile Interaction Experience
- Transforma os formulários rápidos do portal do atleta em bottom sheets no celular, com hierarquia e alcance de polegar.
- Mantém ações Cancelar/Salvar fixas acima da safe-area durante a rolagem.
- Check-in de prontidão passa a usar sliders 0–10 com valor visível para reduzir digitação repetitiva.
- Registros rápidos de escala (dor/energia) reutilizam o mesmo controle de toque.
- Inputs mobile usam altura mínima de 50px e fonte de 16px para evitar zoom automático no iOS.
- Sem alteração de schema; permanece 38/38.


## v0.13.7 — Mobile Feedback Experience
- Padroniza feedback de sucesso e erro com componente visual mais legível e acessível.
- Portal do atleta passa a exibir loading contextual por área, em vez de skeleton genérico.
- Transições curtas entre áreas do dock melhoram continuidade sem transformar a navegação em animação decorativa.
- Toasts mobile respeitam o dock inferior e a safe-area do dispositivo.
- Adiciona resposta tátil visual aos principais alvos de toque e suporte a `prefers-reduced-motion`.
- Sem alteração de schema; permanece 38/38.

### v0.13.11-r3 — correção do smoke test de aviso da demo
- Corrige a validação `[464/600]` para comparar o aviso de demonstração sem diferenciar maiúsculas/minúsculas.
- Nenhuma alteração funcional, visual, de API, banco ou regra esportiva.

### v0.13.11-r5 — Smoke test UTF-8 seguro
- `TESTAR.ps1` salvo como UTF-8 com BOM para compatibilidade com Windows PowerShell 5.1.
- Evita falsos negativos em sentinelas com acentos, incluindo roteiro da demo, prontidão, recuperação e evolução.
- Nenhuma alteração de API, regra de negócio, schema ou interface.


## v0.13.17 — Consistency Compass
- Adiciona uma bússola de consistência esportiva compacta na Home mobile.
- Reutiliza consistência, streak, dias ativos e missões contextuais já existentes.
- Dias protegidos por recuperação/revisão não geram pressão para treinar ou preservar sequência.
- Acesso direto ao bloco de Progresso e missões.
- Sem migration nova; schema permanece 38/38.


## v0.13.20 — Adaptive Mobile Home
- A Home mobile passa a adaptar prioridade conforme quatro estágios do dia: check-in, treino, recuperação e dia fechado.
- Antes do check-in, informações secundárias são reduzidas para destacar prontidão e contexto inicial.
- Pós-treino, recuperação e fechamento ganham prioridade visual.
- Com o dia fechado, chamadas de ação repetitivas são ocultadas e a Home assume um estado mais calmo.
- A adaptação reutiliza prontidão, resposta à sessão e execução do dia já existentes; sem nova regra clínica, score ou migration.
- Schema permanece 38/38.

## v0.14.3-r3 - Rich Athlete Demo Seed robustness
- Corrige metas ja existentes cujo periodo nao cobria os 56 dias de historico do seed: o script agora atualiza `DataInicio`/`DataFim` antes de registrar o historico.
- Torna `POPULAR-LUCATTI-DEMO-RICO.ps1` ASCII-safe para Windows PowerShell 5.1 e envia JSON explicitamente em UTF-8.
- Remove caracteres especiais dos payloads de treino demonstrativo para evitar falha de desserializacao em ambientes Windows PowerShell legados.
- Mantem `VERSION.txt = 0.14.3`; nenhuma alteracao de schema ou funcionalidade de produto.


## v0.14.4-r1 — PowerShell 5.1 Test Runner Compatibility
- `TESTAR.ps1` regravado em UTF-8 com BOM para compatibilidade com Windows PowerShell 5.1.
- Corrige ParserError causado por caracteres Unicode presentes nos tokens de validacao (ex.: seta `→`).
- Nenhuma alteracao funcional, de API, banco ou schema. `VERSION.txt` permanece `0.14.4`.

### v0.14.5-r1 - PowerShell 5.1 / current-version smoke-test fix
- Corrige validacoes do `TESTAR.ps1` que ainda comparavam a versao corrente com `0.14.4` apos o bump funcional para `0.14.5`.
- Preserva validacoes historicas e nomes de artefatos de releases anteriores.
- Mantem `TESTAR.ps1` em UTF-8 com BOM para Windows PowerShell 5.1.
- Sem alteracao de schema, API ou regra funcional.


## v0.14.7-r1 — Smoke Test MVP Preview Fix
- Corrige asserts do TESTAR.ps1 que ainda buscavam `v0.14.6` no rótulo corrente do MVP Preview.
- Preserva marcadores históricos como `HP_MOBILE_CHARTS_PROGRESS='v0.14.6'`.
- Sem alteração de schema, migrations, endpoints ou regras funcionais.


### v0.15.0-r1 — Smoke Test Current Version Fix
- Corrige o healthcheck inicial do `TESTAR.ps1` para aceitar a versão corrente `0.15.0`.
- Atualiza a validação de identidade do `MVP Preview` para `v0.15.0`, preservando todos os marcadores históricos da linha `0.14.x`.
- Nenhuma alteração funcional, de schema ou de migration.
## v0.16.5 — Seasonal Progress
- Fecha a primeira sequência da Gamificação 3.0 com uma leitura longitudinal do ciclo esportivo.
- Reutiliza `cicloEsportivoAtual`, `gamificacao`, `resumoSemanal` e `evolucaoEsportiva`; não cria fonte paralela.
- Organiza a temporada em Fundação, Construção, Consolidação e Fechamento do ciclo.
- Mostra semana/total, progresso temporal, treinos do ciclo, prontidão média, consistência e objetivo do ciclo.
- Mantém recuperação planejada como parte válida do progresso sazonal e evita premiar picos isolados de intensidade.
- Schema permanece 38/38, sem migration nova.
- Smoke test ampliado para 1464 verificações.



## v0.16.5-r1 - MVP Preview smoke test fix
- Corrige a validacao do badge MVP Preview para esperar v0.16.5.
- Nenhuma alteracao funcional, de schema ou de dominio.


### v0.17.1-r1 — Smoke Test Intake Label Fix
- Corrige falso negativo do smoke test herdado da v0.17.0: o fluxo profissional agora valida o rótulo atual “Avaliação inicial” em vez do antigo “Avaliação e contexto”.
- Nenhuma alteração funcional, de API, schema ou migration.

## v0.17.1-r2 - Smoke Test Patient Intake Selector Fix
- Corrige falso negativo no check 1477/1480 do `TESTAR.ps1`.
- O seletor JavaScript `$('#patientIntake')` era validado dentro de aspas duplas do PowerShell, fazendo `$(` ser interpretado como subexpressao.
- A validacao agora usa quoting literal compativel com Windows PowerShell 5.1.
- Sem alteracoes funcionais, de schema, migrations ou API.


### v0.17.2-r1 — Smoke Test Workspace Label Fix
- Corrige falso negativo herdado do Professional Prescription Workspace: o passo 2 passou de 'Direção do tratamento' para 'Direção + sugestão inicial'.
- Nenhuma alteração funcional, de schema ou de API.


## v0.17.4-r1 — Smoke Test MVP Preview Fix
- Normaliza os asserts históricos do MVP Preview para a versão corrente v0.17.4.
- Nenhuma alteração funcional, de schema ou migration.

### v0.18.0-r1 — Correção do gate visual legado
- Atualiza os gates [493/600] a [498/600] do `TESTAR.ps1` para validar AESYN em vez da identidade RS anterior.
- Mantém `VERSION.txt` em `0.18.0`; não altera schema nem comportamento funcional.


## v0.18.2 — Patient Performance Profile
- Reestruturação do prontuário profissional como Performance Profile AESYN.
- Snapshot integrado de prontidão, corpo, treino, nutrição, check-ins, exames e hidratação.
- Navegação contextual por dimensão de acompanhamento.
- Ações de prescrição e registro priorizadas; ações secundárias agrupadas.
- Nova faixa de contexto integrado na visão geral.
- Layout responsivo dedicado.
- Schema preservado em 38/38; nenhuma migration adicionada.


## v0.18.1 — Professional Performance Dashboard

- **r1:** corrige o gate histórico `[468/600]` do `TESTAR.ps1` para validar o novo Professional Performance Dashboard AESYN em vez do hero legado do MVP Preview.
- Nova Home profissional AESYN com command center de acompanhamento e performance.
- KPIs de pacientes ativos, consultas do dia, pacientes em foco e retornos pendentes.
- Integra `api/insights/dashboard` à visão profissional para priorização de sinais existentes.
- Visão de operação com agenda, próximos atendimentos e pacientes recentes.
- Guardrail explícito: insights organizam contexto para revisão profissional e não substituem avaliação médica.
- Responsividade específica do dashboard AESYN.
- Sem migration nova; schema permanece 38/38.


## v0.18.5 — Exams Timeline & Trends
- Evolui o Exams Core com comparação neutra entre a última medição e a anterior para marcadores numéricos.
- Adiciona delta absoluto/percentual, direção numérica (subiu, desceu, sem variação) e data da medição sem classificar a direção como melhora/piora clínica.
- Adiciona comparação contextual entre as duas últimas coletas e linha do tempo laboratorial por coleta.
- Mantém o Exams Core v0.18.4 como base e adiciona guardrail explícito de interpretação profissional.
- Layout responsivo dedicado para desktop, tablet e mobile.
- Schema permanece 38/38; nenhuma migration nova.
- Smoke test ampliado para 1592 verificações.

### v0.18.6-r1 — estabilização do smoke test
- Corrige gate legado `[463/600]` do MVP Preview para validar `v0.18.6` em vez de `v0.18.5`.
- Sem alteração funcional, de schema ou migration.

### v0.18.6-r2 — estabilização do smoke test
- Atualiza o gate histórico da Workout Library para reconhecer a identidade atual `AESYN • STANDALONE WORKOUT LIBRARY • v0.18.6`.
- Preserva o marcador funcional `HP_WORKOUT_LIBRARY_ASSIGNMENT='v0.17.5'` e todos os comportamentos legados de atribuição.
- Nenhuma alteração de schema, migration ou regra de negócio.


## 0.18.7 — Workout Builder 3.0
- Presets rápidos: hipertrofia, força, resistência e técnica/retorno.
- Busca contextual de exercícios dentro de cada item do treino.
- Filtros por grupo muscular e equipamento no builder.
- Duplicação e reordenação de exercícios.
- Duplicação e reordenação de sessões completas.
- Resumo vivo de sessões, exercícios e séries prescritas.
- Duplicação completa de treino-modelo sem paciente.
- Workout Library atualizada para o fluxo 3.0.
- Layout responsivo aprimorado.
- Schema 38/38 preservado; nenhuma migration nova.


### v0.18.7-r1 — estabilização do smoke test
- Corrige o healthcheck inicial do `TESTAR.ps1` para esperar a versão corrente `0.18.7`.
- Nenhuma alteração funcional, de schema ou migration.


### v0.18.7-r2 — estabilização de versionamento do smoke test
- Atualiza gates legados de `VERSION.txt`, health endpoint e MVP Preview para a versão corrente `0.18.7`.
- Preserva marcadores históricos de nascimento das features da v0.18.6.
- Nenhuma alteração funcional, de schema ou migration.


### v0.18.7-r3 — estabilização do gate Standalone Workout Builder
- Atualiza o gate legado que procurava `Sem paciente vinculado`.
- O teste passa a validar a copy atual `O modelo permanece independente de paciente.` do Workout Builder 3.0.
- Nenhuma alteração funcional, de schema ou migration.


## 0.18.8 — Workout Template Library 2.0
- Catálogo profissional de treinos-modelo.
- Filtro por objetivo, status e favoritos.
- Ordenação por nome, favoritos, sessões, exercícios e atualização recente.
- Favoritos persistidos localmente no navegador.
- Chips rápidos por objetivo.
- Quick Template Preview com sessões, exercícios, séries, reps, carga e descanso.
- Preview integra a Exercise Library para resolver nomes e metadados dos exercícios.
- Fluxo de atribuição continua gerando cópia independente por paciente.
- Schema 38/38 preservado; nenhuma migration nova.


### v0.18.8-r1 — estabilização do gate MVP Preview
- Atualiza o gate legado `[463/600]` para reconhecer a identidade corrente da AESYN em `v0.18.8`.
- Nenhuma alteração funcional, de schema ou migration.


### v0.18.8-r2 — estabilização do gate Workout Library
- Atualiza o gate histórico `[1505/1512]` para reconhecer a identidade atual `AESYN • WORKOUT TEMPLATE LIBRARY 2.0 • v0.18.8`.
- Preserva o fluxo de biblioteca, atribuição e cópia independente por paciente.
- Nenhuma alteração funcional, de schema ou migration.


## 0.18.9 — Program Builder 1.0
- Biblioteca profissional de programas de treino.
- Programas independentes de paciente.
- Composição por treinos-modelo existentes.
- Múltiplas fases por programa.
- Duração em semanas por fase.
- Agenda semanal com slots de Segunda a Domingo.
- Duplicação e reordenação de fases/dias.
- Resumo de semanas, fases, sessões e modelos distintos.
- Preview completo do ciclo.
- Busca, filtro por objetivo e ordenação de programas.
- Duplicação, exclusão, exportação e importação JSON.
- Acesso direto pelo Professional Workout Studio e pela Workout Template Library.
- Persistência local nesta primeira fase; schema 38/38 preservado e sem migration nova.


### v0.18.9-r1 — estabilização do gate MVP Preview
- Atualiza o gate legado `[463/600]` para reconhecer a identidade corrente da AESYN em `v0.18.9`.
- Nenhuma alteração funcional, de schema ou migration.


## 0.18.10 — Server-side Program Catalog
- Nova entidade `ProgramaTreinoModelo`.
- CRUD completo em `/api/programas-treino`.
- Program Builder passa a persistir no servidor.
- Migração assistida do localStorage legado.
- Migration EF incremental e SQL idempotente.
- Setup ampliado para `39/39`.


### v0.18.10-r1 — estabilização do gate MVP Preview
- Atualiza o gate legado `[463/600]` para reconhecer corretamente a identidade AESYN em `v0.18.10`.
- Nenhuma alteração funcional, de schema ou migration.


### v0.18.10-r2 — correção da migration EF
- Corrige a geração de `V01810ProgramasTreinoModelo`: o comando não usa mais `--no-build`.
- A causa era a migration sendo calculada a partir da DLL compilada antes da nova entidade, o que podia gerar migration vazia e causar `PendingModelChangesWarning` em `[6/38]`.
- O setup agora detecta e remove uma migration v0.18.10 vazia antes de regenerá-la.
- Nenhuma nova funcionalidade; versão funcional permanece `0.18.10`.


### v0.18.10-r3 — sincronização forçada do ModelSnapshot
- Corrige definitivamente o `PendingModelChangesWarning` da v0.18.10.
- Se `V01810ProgramasTreinoModelo` existir mas ainda não estiver aplicada, o setup a remove via `dotnet ef migrations remove --force` e gera novamente.
- Isso restaura e recria o `AppDbContextModelSnapshot` a partir do modelo atual, evitando reaproveitar uma migration/snapshot inconsistente de tentativas anteriores.
- Se a migration já estiver aplicada, ela é preservada.


### v0.18.10-r4 — snapshot EF fix definitivo
- Remove a geração dinâmica de `V01810ProgramasTreinoModelo`.
- `AppDbContextModelSnapshot` passa a incluir `ProgramaTreinoModelo`.
- A criação física da tabela continua pelo SQL idempotente `[39/39]`.
- O setup limpa qualquer migration experimental v0.18.10 deixada por r1/r2/r3 e remove `bin/obj` relacionados para impedir que a migration antiga sobreviva no assembly.
- Corrige tanto `PendingModelChangesWarning` quanto `The name 'V01810ProgramasTreinoModelo' is used by an existing migration`.


### v0.18.10-r5 — restore após limpeza de obj/bin
- Corrige `NETSDK1004` no `[5/38]`.
- Como a r4 remove `obj/` e `bin/` para limpar migrations antigas, o setup agora executa `dotnet restore` imediatamente após a limpeza.
- Adiciona verificação defensiva antes da recompilação para garantir que `project.assets.json` exista.
- Nenhuma alteração funcional; versão permanece `0.18.10`.


### v0.18.10-r6 — estabilização do gate de preview de programas
- Atualiza o gate histórico `[1629/1634]` para reconhecer o preview server-side atual.
- Substitui a expectativa de copy `Planejamento mestre` por `Fonte: servidor AESYN`.
- Nenhuma alteração funcional, de schema ou migration.


## 0.18.11 — Program Assignment & Publish
- Novo fluxo profissional de atribuição de programa a paciente.
- Picker de paciente diretamente no preview do programa.
- Review & Publish antes da materialização.
- Publicação cria um plano por fase e uma fase longitudinal correspondente.
- Treinos-modelo são copiados com sessões, exercícios, carga, repetições e descansos.
- Dia do programa é aplicado em `DiasSemana`.
- Validação de templates/exercícios ativos antes da publicação.
- Opção de concluir planos ativos anteriores.
- Primeira fase pode iniciar como `Ativo` / `EmAndamento`.
- Programa mestre permanece independente das cópias clínicas publicadas.
- Tela de sucesso com resumo e acesso direto ao paciente.
- Sem migration nova; baseline permanece 39/39.


### v0.18.11-r1 — correção de compilação do Assignment Controller
- Remove caractere `\` espúrio no início de `ProgramaTreinoAssignmentController.cs`.
- Corrige `CS1056: Caractere inesperado '\'` no `[3/38] Compilando...`.
- Nenhuma alteração funcional, de schema ou migration.

## v0.19.0-r3 — AESYN Brand Reference Alignment
- Home/login alinhada ao template visual AESYN enviado.
- Símbolo oficial aplicado a partir da referência visual enviada.
- Paleta petróleo/mint refinada.
- Tema escuro corrigido para superfícies petróleo, incluindo cards do dashboard.

### v0.19.0-r4 — Premium Neutral Dark Theme
- Refina o tema escuro para base quase preta/grafite, com verde AESYN reservado para destaques e estados.
- Remove superfícies brancas remanescentes no dashboard profissional em dark mode.
- Reequilibra contraste de tipografia, bordas, inputs, tabelas, KPIs e cards legados.
- Mantém a versão funcional pública 0.19.0; revisão visual sem alteração de schema.

## v0.19.4-r1 — Smoke Gate Production Label Fix
- Corrige o gate histórico do Workout Builder 2.0 para validar marcadores estruturais, sem exigir o rótulo visual legado `WORKOUT BUILDER 2.0 • v0.17.4`.
- Mantém a interface de produção sem referências visíveis de versões históricas.
- Nenhuma alteração funcional ou de schema.


### v0.19.25-r2 — correção do gate de contrato de e-mail
- Corrige falso negativo no gate `[1908/1914]`.
- `EmailPrincipal`, `Confirmado` e `PodeSolicitarConfirmacao` pertencem ao contrato `EmailAccountStatusResponse`, não ao texto do `ContaEmailController`.
- O gate agora valida controller e contrato separadamente, de acordo com a responsabilidade real de cada arquivo.
- Nenhuma alteração funcional, de schema ou migration; versão funcional permanece `0.19.25`.


### v0.19.27-r1 — sincronização de versão no TESTAR
- Corrige falso negativo já no gate `[1/600]`: a API anunciava corretamente `0.19.27`, mas diversos gates históricos ainda exigiam `0.19.26`.
- Atualiza todas as comparações da versão pública corrente no `TESTAR.ps1` para `0.19.27`.
- Preserva as validações funcionais históricas e a versão funcional `0.19.27`.
- Nenhuma alteração de schema, migration ou comportamento de produção.


## v0.19.29 — AESYN App Identity & Theme Completion
- Adiciona `manifest.webmanifest` com identidade AESYN Performance e modo standalone.
- Gera ícones 32/180/192/512 e variantes maskable a partir do símbolo oficial já versionado no projeto.
- Adiciona favicon, Apple Touch Icon, meta tags mobile/PWA e theme-color dinâmico.
- Adiciona launch experience para execução instalada quando suportada pelo navegador.
- Consolida variáveis e overrides do dark mode para login, portal do paciente, modais, formulários e componentes legados.
- Corrige o primeiro acesso para respeitar preferência já persistida e salva a escolha inicial na conta quando autenticado.
- Sem alteração de schema ou migration.


## v0.19.29-r1 — Version Foundation Gate Sync Hotfix
- Corrige falso negativo no gate `[1719/1728]` (`AESYN Identity & Patient Navigation Foundation`).
- O gate legado ainda exigia `HP_MVP_VERSION='0.19.28'`; agora valida a versão pública corrente `0.19.29`.
- Mantém intactas as validações históricas de tema, navegação e Patient Health Hub.
- Nenhuma alteração funcional, de schema ou migration; versão funcional permanece `0.19.29`.


## v0.19.30-r2 — Daily Summary Legacy Gate Cleanup
- Corrige os gates históricos `[1153/1160]` que ainda exigiam `mobile-home-glance` e `glance-chip`, removidos intencionalmente na nova Home da v0.19.30.
- Passa a validar `patient-today-overview`, as seis ações essenciais e o layout responsivo atual.
- Atualiza o gate adaptativo para não depender do seletor legado de recovery.
- Remove CSS morto do antigo resumo mobile sem alterar o fluxo atual do paciente.
- Nenhuma alteração de schema, migration ou versão funcional; permanece `0.19.30`.

## v0.19.30-r4 — Patient Home Readiness Gate Cleanup
- Corrige gates históricos que ainda exigiam `daily-readiness-card` no `app.js` após a simplificação intencional da Home.
- A prontidão/check-in passa a ser validada pelo card atual `athleteHome2Body` dentro de `patient-today-overview`.
- Atualiza também o gate de compactação para a estrutura mobile atual, sem reintroduzir componentes legados.
- Sem alteração funcional, migration ou schema.
