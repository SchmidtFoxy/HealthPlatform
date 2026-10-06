# AESYN Performance — ROADMAP MESTRE — LISTA 05

> Base funcional: **v0.59.11 — Messaging Presence & Delivery Polish / Lista 04 Closure**
>
> Nova série funcional: **v0.60.x — Mobile Reliability, Athlete Experience & Professional Planning**
>
> Objetivo da Lista 05: transformar pontos de atrito observados no uso real do PWA, treino, plano, busca, arquivos, interesses, chat e prescrição nutricional em uma experiência coesa, mobile-first e clinicamente útil, sem microversões artificiais.

---

## 1. Regra de fechamento da Lista 05

A Lista 05 só poderá ser marcada como **CONCLUÍDA** quando todos os 15 requisitos originais estiverem:

- implementados ou explicitamente consolidados em uma solução superior;
- cobertos por gate funcional no `TESTAR.ps1` quando houver risco de regressão;
- validados em tema claro e escuro nas superfícies tocadas;
- validados em PWA/mobile aproximadamente 360 / 390 / 430 px;
- validados em desktop quando a mesma superfície existir;
- sem overflow horizontal, tela “solta”, CTA escondido, teclado cobrindo ação ou dependência de interação exclusiva de desktop;
- documentados em `README.md`, `ROADMAP.md` e `CHANGELOG.md` quando aplicável.

Não criar versões apenas para “handoff”, “closure”, “continuity”, “summary” ou variações documentais. Cada versão funcional precisa entregar valor observável ao paciente ou profissional.

---

# 2. Ordem funcional da série v0.60.x

## v0.60.0 — PWA Session & Mobile Search Reliability

### Problemas atacados
1. erro `sessão expirada` ao entrar pelo app/PWA;
2. lupa invisível no tema claro;
3. lupa/busca mobile replicando a UX de PC (`Esc`, atalhos e interação inadequada);
4. viewport/tela podendo se mover lateralmente no app.

### Entregas
- revisar ciclo de autenticação do PWA, restauração de sessão, boot do app e transições após retomada;
- impedir que uma sessão válida seja descartada prematuramente durante carregamento/reidratação;
- diferenciar claramente sessão realmente expirada de falha transitória de carregamento;
- busca mobile com experiência própria: botão visível, sheet/modal full-width, botão fechar explícito e teclado mobile;
- `Esc` permanece como atalho progressivo no desktop, nunca requisito no mobile;
- corrigir contraste/ícone da lupa em tema claro e validar tema escuro;
- bloquear overflow horizontal global sem mascarar componentes legitimamente roláveis;
- respeitar `safe-area`, `visualViewport` e teclado virtual;
- gate automático para sessão PWA, busca mobile e ausência de overflow nas superfícies tocadas.

### Critério de aceite
O usuário abre o PWA, entra ou retoma a sessão sem “sessão expirada” falsa, consegue abrir/fechar a busca com uma mão e a tela permanece estável na largura do dispositivo.

---

## v0.60.1 — Patient Files Mobile Recovery

### Problema atacado
- erro da página **Arquivos** no mobile.

### Entregas
- reproduzir e corrigir falha real da tela Arquivos em mobile/PWA;
- revisar carregamento, vazio, erro, retry e autenticação de download/preview;
- cards responsivos para PDF, imagem, DOC/DOCX e demais tipos já suportados;
- preview/download sem navegação quebrada;
- ações com alvos de toque confortáveis;
- conteúdo longo/nome de arquivo não pode quebrar a largura;
- regressão automática do erro encontrado.

### Critério de aceite
Arquivos abre, lista, filtra/consulta, pré-visualiza ou baixa conteúdo suportado sem erro em 360/390/430 px.

---

## v0.60.2 — Mobile Navigation & Profile/More 2.0

### Problemas/melhorias atacados
- `•••` pouco expressivo;
- funções secundárias misturadas às funções principais;
- seta “voltar” usada onde semanticamente a ação é sair/fechar.

### Entregas
- substituir `•••` por botão explícito **Perfil / Mais** com ícone compreensível;
- posicionar o acesso secundário separado da navegação primária;
- manter ações mais usadas imediatamente acessíveis;
- reorganizar sheet/menu secundário por grupos: perfil, dados, histórico, configurações e ações auxiliares;
- substituir seta por **X** verde-escuro quando a ação for fechar/sair de uma experiência modal/detalhe;
- preservar seta apenas quando existir navegação hierárquica real “voltar para a tela anterior”;
- foco, `aria-label`, teclado e touch corretos;
- Touched Surface Quality Gate em todas as superfícies alteradas.

### Critério de aceite
A navegação principal fica visualmente mais limpa e o usuário distingue intuitivamente “voltar” de “fechar”.

---

## v0.60.3 — Athlete Interests Discovery 2.0

### Problema atacado
- sugestões/interesses esportivos escondidos no fim da página, com baixa descoberta.

### Entregas
- transformar interesses em superfície descoberta, não rodapé;
- criar aba/seção de **Sugestões** ou experiência equivalente de alta visibilidade;
- separar `Já faço`, `Tenho interesse`, `Sugestões para explorar`;
- busca/filtros simples por modalidade/categoria quando agregar valor;
- CTA claro para adicionar/remover interesse;
- sugestões não devem afirmar adequação clínica automática;
- mobile-first, cards compactos e carregamento progressivo.

### Critério de aceite
Um paciente novo consegue encontrar e adicionar uma modalidade sugerida sem rolar até o fim da página nem conhecer previamente onde a função está.

---

## v0.60.4 — Workout Selection Before Start

### Problema atacado
- `Iniciar treino` não deixa claro qual treino A/B/C/etc. será executado antes do início.

### Entregas
- card/lista dos treinos disponíveis antes de iniciar;
- permitir tocar no treino e abrir um resumo antes da execução;
- exibir nome, foco, exercícios, duração/volume quando disponíveis e contexto do plano;
- CTA `Iniciar este treino` somente após seleção clara;
- preservar início rápido quando houver apenas um treino elegível;
- lembrar seleção apenas quando não causar execução acidental;
- estados sem treino, treino indisponível e erro bem definidos.

### Critério de aceite
O paciente sabe inequivocamente qual treino começará antes de registrar a primeira série.

---

## v0.60.5 — Workout Execution Readability

### Problema atacado
- contraste ruim das palavras na tela de lançamento dos dados do treino.

### Entregas
- revisar contraste de labels, valores, placeholder, estados e CTAs em tema escuro/claro;
- usar branco/verde do sistema de design somente onde passar contraste e hierarquia;
- melhorar leitura de carga, repetições, descanso, RPE/RIR e demais campos existentes;
- estados concluído/em andamento/próximo não podem depender somente de cor;
- validar sob luz forte/mobile e tamanhos de fonte do sistema.

### Critério de aceite
Todos os dados essenciais de execução permanecem legíveis sem esforço nos dois temas e nos três tamanhos mobile de referência.

---

## v0.60.6 — Workout Series Blocks / Sub-series Builder

### Problema atacado
- uma prescrição precisa aceitar séries com esquemas diferentes no mesmo exercício.

### Entregas
- botão `+` junto às séries/blocos do exercício;
- permitir múltiplos blocos de séries com quantidade, repetições/faixa e demais parâmetros pertinentes por bloco;
- exemplo válido: `1 x 10–15` + `2 x 3x10` ou configuração equivalente coerente com o domínio;
- ordenar, editar e remover blocos com segurança;
- Workout Builder profissional e execução do paciente devem compartilhar a mesma interpretação;
- preservar compatibilidade com exercícios antigos de bloco único;
- migrations/versionamento de contrato se necessários;
- gates de persistência, duplicação/template e execução.

### Critério de aceite
O profissional consegue prescrever variações de séries no mesmo exercício e o paciente as executa na ordem correta sem ambiguidade.

---

## v0.60.7 — Patient Plan Compact Experience

### Problema atacado
- página Plano ocupa espaço demais e exige rolagem excessiva; balão azul desperdiça largura.

### Entregas
- reduzir altura/spacing sem sacrificar leitura;
- centralizar adequadamente a escrita no balão azul;
- aproveitar melhor largura útil à esquerda/direita;
- compactar cards repetitivos;
- destacar `agora`, `próximo` e progresso sem transformar a tela em dashboard denso;
- manter ações essenciais ao alcance;
- validar conteúdo curto e longo, fonte ampliada e 360/390/430 px.

### Critério de aceite
O paciente enxerga mais do plano com menos rolagem, sem perder clareza ou alvos de toque.

---

## v0.60.8 — Professional Chat Identity & Global Inbox Consolidation

### Requisitos atacados
- chat com todos os pacientes na sidebar do médico;
- substituir `Dr Testinho` por `Dr Raphael` nas superfícies demo pertinentes.

### Observação de baseline
A Lista 04 já entregou uma **Professional Chat Inbox** na sidebar. Esta versão não deve recriar a função; deve auditar, consolidar e corrigir qualquer superfície que ainda não exponha corretamente o inbox global ou ainda dependa do paciente previamente aberto.

### Entregas
- Chat global sempre acessível pela navegação profissional quando o perfil tiver permissão;
- lista de todos os pacientes/conversas elegíveis, inclusive sem conversa recente quando fizer sentido iniciar contato;
- busca por paciente;
- preservar não lidas, aguardando resposta e deep-link já existentes;
- remover duplicações de entrada de chat dentro do fluxo profissional;
- atualizar identidade demo `Dr Testinho` → `Dr Raphael` somente em seeds/cópias/demo pertinentes, sem alterar nomes de profissionais reais;
- regressões para navegação e identidade demo.

### Critério de aceite
O profissional abre Chat pela sidebar e alcança qualquer paciente autorizado sem primeiro navegar ao prontuário.

---

## v0.60.9 — Professional Nutrition Planning Engine 3.0

### Problema atacado
O fluxo médico de criar dieta ficou centrado em metas e perdeu a sensação de um software completo de cálculo nutricional.

### Princípio clínico
O AESYN calcula, organiza e compara dados; **não substitui julgamento médico/nutricional** e não deve prescrever automaticamente estratégia clínica.

### Entregas — Diet Builder / Biblioteca de dietas
- CTA explícita na área profissional de Nutrição: `Montar dieta modelo` e `Nova dieta`;
- permitir criar **dieta modelo sem paciente vinculado**, no mesmo espírito da biblioteca de treinos, porém com fluxo nutricional mais completo;
- permitir criar nova dieta diretamente para um paciente quando já houver contexto clínico aberto;
- biblioteca pesquisável de dietas/modelos com nome, objetivo, tags, observações e composição nutricional resumida;
- ações seguras de `usar modelo`, `duplicar`, `editar modelo`, `salvar como novo modelo` e `adaptar ao paciente`;
- ao aplicar um modelo, preservar o modelo-base e criar uma cópia editável vinculada ao paciente, evitando alterações retroativas no template;
- permitir transformar uma dieta já montada em modelo reutilizável;
- suportar revisão antes de publicar, com diferenças claras entre `modelo`, `rascunho do paciente` e `plano publicado`;
- integrar modelos ao motor nutricional desta versão: metas, calorias, macros, refeições, alimentos, porções e suplementos devem recalcular normalmente após a aplicação;
- manter versionamento/auditoria suficiente para saber qual modelo originou uma dieta do paciente quando aplicável;
- UX superior ao builder de treino atual: criação guiada, resumo nutricional sempre visível, menos cliques repetitivos e possibilidade de começar por modelo, copiar dieta existente ou começar do zero.

### Critério de aceite — Diet Builder
O profissional consegue entrar em Nutrição e escolher claramente entre `Montar dieta modelo` e `Nova dieta`, criar/editar um template reutilizável sem paciente, aplicar uma cópia a um paciente e adaptá-la sem alterar o original.

### Entregas — dados-base
- peso atual;
- altura;
- idade/data de nascimento quando disponível;
- sexo/campo fisiológico usado pela fórmula quando pertinente e consentido;
- objetivo e meta de peso já existentes quando aplicável;
- nível de atividade/contexto energético;
- possibilidade de o profissional ajustar manualmente qualquer resultado calculado.

### Entregas — cálculo energético
- TMB com fórmula identificada;
- GET/manutenção;
- ajuste energético do objetivo;
- meta calórica ativa;
- diferença absoluta e percentual entre manutenção e alvo;
- avisos informativos para valores incomuns, sem bloquear julgamento profissional quando não houver regra de segurança objetiva.

### Entregas — macronutrientes
- proteína, carboidrato e gordura em g/dia;
- suporte a cálculo por peso corporal onde já existir regra do produto;
- kcal resultantes dos macros;
- diferença `meta energética x energia dos macros`;
- ajuste manual com recálculo imediato.

### Entregas — builder nutricional ao vivo
- calorias, proteínas, carboidratos e gorduras por alimento;
- subtotal por refeição;
- total diário;
- `meta / prescrito / restante / excedido`;
- percentual da meta;
- atualização ao vivo ao adicionar, remover, trocar ou redimensionar porção;
- suplementos com macros continuam integrados aos totais conforme Lista 04;
- evitar dupla contagem ao alternar unidade/porção.

### Entregas — UX
- fluxo visual claro: `Dados → Gasto energético → Objetivo → Macros → Refeições → Revisão`;
- profissional pode navegar entre etapas sem perder rascunho;
- resumo sticky/compacto no desktop e equivalente mobile não intrusivo;
- explicar qual valor está ativo e qual é apenas referência;
- estados de fórmula indisponível/dado faltante sem inventar resultado.

### Critério de aceite
O profissional consegue partir dos dados corporais, calcular/revisar necessidades, definir metas e montar uma dieta vendo em tempo real a composição nutricional completa, mantendo controle manual em cada etapa.

---

## v0.60.10 — Lista 05 Integrated Mobile & Cross-surface Quality Pass

### Propósito
Não é uma versão de “polish infinito”. É um **gate integrado final** para capturar regressões entre soluções que foram implementadas separadamente ao longo da Lista 05.

### Entregas
- jornada PWA real: abrir app → autenticar/retomar → buscar → navegar → abrir arquivos → escolher treino → executar → consultar plano → chat;
- jornada profissional: login → chat global → paciente → treino → séries avançadas → nutrição/cálculo → dieta;
- tema claro e escuro nas superfícies da Lista 05;
- 360 / 390 / 430 px;
- desktop para superfícies profissionais;
- teclado virtual, safe-area, orientation/resize quando pertinente;
- nenhum overflow horizontal global;
- loading/empty/error/retry;
- foco e acessibilidade básica;
- nenhum nome demo incorreto;
- nenhuma regressão dos recursos da Lista 04;
- gates de regressão consolidados no `TESTAR.ps1`.

### Critério de fechamento
Se este gate passar integralmente, marcar:

> **LISTA 05 — CONCLUÍDA**

Não criar `v0.60.11+` apenas para prolongar a série. Uma nova evolução funcional exige nova prioridade real/Lista 06 ou replanejamento explícito do Product Brain.

---

# 3. Mapa dos 16 requisitos da Lista 05

| # | Requisito Lista 05 | Versão planejada |
|---|---|---|
| 1 | Lupa invisível em tema claro | v0.60.0 |
| 2 | “Sessão expirada” ao entrar pelo app | v0.60.0 |
| 3 | Erro página Arquivos mobile | v0.60.1 |
| 4 | Criar dieta sem software real de cálculo | v0.60.9 |
| 5 | Lupa mobile com UX de PC/Esc | v0.60.0 |
| 6 | Chat de todos os pacientes na sidebar médico | v0.60.8 |
| 7 | App fixo na largura / só rolagem vertical | v0.60.0 |
| 8 | Interesses esportivos com sugestões visíveis | v0.60.3 |
| 9 | Escolher treino antes de iniciar | v0.60.4 |
| 10 | Legibilidade dos dados do treino | v0.60.5 |
| 11 | Plano mais compacto / balão azul melhor aproveitado | v0.60.7 |
| 12 | Dr Testinho → Dr Raphael | v0.60.8 |
| 13 | ••• → Perfil/Mais clean | v0.60.2 |
| 14 | seta voltar → X quando for fechar/sair | v0.60.2 |
| 15 | botão + e blocos/subséries com reps diferentes | v0.60.6 |
| 16 | Diet Builder com `Montar dieta modelo` / `Nova dieta`, biblioteca reutilizável e aplicação a pacientes | v0.60.9 |

**Cobertura planejada: 16/16.**

---

# 4. Gates permanentes da Lista 05

## PWA Creamy Gate
Toda superfície mobile tocada precisa ser validada em aproximadamente 360, 390 e 430 px, incluindo teclado aberto quando houver input.

## Touched Surface Quality Gate
Toda tela alterada deve sair melhor do que entrou em hierarquia, spacing, contraste, estados, CTA, loading, vazio, erro/retry e acessibilidade básica.

## Session Reliability Gate
A aplicação não pode chamar de “sessão expirada” um boot transitório, atraso de rede ou estado ainda não reidratado.

## Search Mobile Gate
Nenhuma função crítica de busca pode exigir `Esc`, hover, atalho de teclado ou precisão de mouse.

## Horizontal Stability Gate
A shell do PWA não pode se deslocar lateralmente. Elementos com rolagem horizontal deliberada devem conter a própria rolagem.

## Nutrition Integrity Gate
Conversão de unidade/porção não pode alterar nutrientes canônicos indevidamente nem causar dupla contagem.

## Clinical Control Gate
Cálculos podem sugerir números e explicar fórmulas, mas decisão e publicação permanecem sob controle profissional.

## Workout Prescription Compatibility Gate
Novos blocos de séries devem preservar e interpretar corretamente prescrições legadas de bloco único.

## Regression Memory Gate
Cada bug reproduzido da Lista 05 que possa voltar deve virar proteção automatizada no `TESTAR.ps1` quando tecnicamente viável.

---

# 5. O que NÃO fazer nesta série

- não criar uma versão por ajuste visual minúsculo quando puder ser consolidado com a superfície funcional correspondente;
- não recriar Professional Chat Inbox já entregue na Lista 04;
- não transformar cálculo nutricional em prescrição automática;
- não “corrigir” viewport com `overflow-x:hidden` escondendo componentes quebrados sem tratar a causa;
- não trocar toda seta do produto por X indiscriminadamente;
- não tornar `Dr Raphael` hardcoded para usuários reais;
- não quebrar compatibilidade de treinos antigos ao introduzir subséries;
- não abrir `v0.60.11+` apenas para fechamento documental.

---

# 6. Sequência de prioridade

**Bloco A — confiabilidade imediata:** v0.60.0 → v0.60.1

**Bloco B — clareza mobile:** v0.60.2 → v0.60.3

**Bloco C — experiência de treino:** v0.60.4 → v0.60.6

**Bloco D — plano e comunicação:** v0.60.7 → v0.60.8

**Bloco E — nutrição profissional completa:** v0.60.9

**Bloco F — gate integrado e fechamento:** v0.60.10

---

# 7. Definição de “Lista 05 perfeita”

A Lista 05 será considerada pronta quando o AESYN atingir simultaneamente estas cinco condições:

1. **Entrar é confiável** — PWA não expira sessão falsamente e a tela não dança lateralmente.
2. **Encontrar é simples** — busca e navegação funcionam com uma mão no celular.
3. **Treinar é inequívoco** — o atleta escolhe o treino, lê os campos e executa prescrições simples ou avançadas sem dúvida.
4. **Planejar é profissional** — o médico/profissional possui um verdadeiro motor de planejamento nutricional e uma biblioteca reutilizável de dietas/modelos, não apenas metas soltas.
5. **A experiência fecha como sistema** — Arquivos, Plano, Interesses, Chat, Perfil/Mais e as jornadas cruzadas funcionam com qualidade coerente.

