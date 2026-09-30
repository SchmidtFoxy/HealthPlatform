## v0.27.5 — Periodization 3.0

A camada de Workout Intelligence passa a organizar o planejamento temporal já registrado, sem criar periodização automática.

### Entregas
- **Microciclo:** semana corrente calculada a partir da data de início do plano.
- **Mesociclo:** fase de treino ativa ou correspondente à data atual.
- **Bloco:** plano de treino vigente como contexto macro.
- **Deload:** identifica fases explicitamente registradas como deload, sem sugerir ou aplicar uma semana de descarga.
- fases ordenadas com tipo, status, duração, objetivo e critério profissional de transição;
- integração ao endpoint e painel de Workout Intelligence 3.0;
- reutiliza `PlanoTreino` + `FaseTreino`; **não cria migration ou tabela nova**.

### Guardrail
Periodization 3.0 **não cria microciclo, mesociclo, bloco, deload ou transição automaticamente**. A camada apenas organiza planejamento já registrado pelo profissional.

**Próxima fase:** `v0.28.0 — Athlete Performance Passport Foundation`.

---

## v0.27.4 — Progression & Regression 3.0

Transforma o histórico de prescrição × execução em **sinais explicáveis de revisão**, sem aplicar mudanças automaticamente.

### O que entra nesta versão
- **RevisarProgressao:** evidências repetidas como RIR acima do alvo, carga ou séries acima do prescrito.
- **RevisarRegressao:** evidências repetidas como RIR abaixo do alvo, carga ou séries abaixo do prescrito.
- **SinaisMistos:** progressão e regressão aparecem juntas e exigem leitura profissional.
- **HistoricoInsuficiente:** menos de duas execuções comparáveis não gera sugestão de direção.
- Cada sinal mostra as **evidências usadas**, a sugestão textual e a regra de revisão.

### Guardrail
Progression & Regression 3.0 **não altera automaticamente carga, volume, exercício, RIR, cadência, técnica ou prescrição**. Toda sugestão é explicável e depende de revisão profissional.

**Próxima etapa:** `v0.27.5 — Periodization 3.0`.

---

## v0.27.0 — Workout Intelligence 3.0 Foundation

Inicia a fase **Workout Intelligence 3.0** sobre os dados de treino que o AESYN já possui, sem criar schema prematuramente e sem automatizar decisões profissionais.

### O que entra nesta versão
- **Mapa de cobertura estruturada:** séries, repetições, carga, descanso, tempo e RPE.
- **Lacunas explícitas:** RIR, Cadência, técnicas avançadas e periodização 3.0 aparecem como próximas camadas, sem serem inferidas de texto livre.
- **Prescrito vs realizado — fundação:** resumo descritivo de plano atual e execuções recentes, preservando janelas e contexto.
- **API profissional e do paciente:** leitura da mesma base com isolamento por organização e vínculo do paciente.
- **Painel profissional:** card mobile-first na aba Treino mostrando cobertura, séries prescritas/realizadas e próximos passos.

### Guardrail
Workout Intelligence 3.0 Foundation **não prescreve progressão, regressão, carga, volume, RPE/RIR, descanso, cadência, técnica ou periodização automaticamente**. A leitura é descritiva e serve para apoiar revisão profissional.

**Próxima etapa:** `v0.27.1 — Prescription Variables 3.0`.

---

## v0.26.6 — Recreational Sports 2.0

Aprofunda os esportes recreativos da Sports Expansion II sem assumir uma modalidade única. O Explore passa a organizar **Jogos de quadra, Parque e área livre, Praia e areia e Lazer social**, conectando esses contextos a coordenação, reação/adaptação, capacidade geral de movimento, equilíbrio/estabilidade, agilidade e resistência conforme o contexto.

As referências continuam vindo de `ModelosSessoesTreino` existentes. Quando não há conteúdo adequado, o AESYN preserva a lacuna em vez de inventar uma sessão.

Recreational Sports 2.0 é descoberta e contexto. O sistema **não escolhe automaticamente uma prática e não prescreve duração, carga, volume, intensidade, aptidão ou retorno ao esporte**.

**Próxima etapa:** `v0.27.0 — Workout Intelligence 3.0 Foundation`.

---

## v0.26.5 — Trekking 2.0

O AESYN Explore agora aprofunda **Trekking** organizando contextos de terreno e capacidades físicas de suporte sem montar rota nem transformar descoberta em prescrição.

### O que entra nesta versão
- **Contextos de terreno:** Subida, Descida, Terreno irregular e Deslocamento prolongado.
- **Resistência prolongada:** suporte a esforços maiores sem definir distância, duração ou pace.
- **Estabilidade de membros inferiores:** tornozelo, joelho e quadril diante de terreno variável.
- **Base física:** core/postura, cadeia posterior/panturrilha e transporte de carga.
- **Gestão de esforço:** contexto para consistência sem zonas ou intensidade automática.
- **Referências reais:** reaproveitamento editorial de `ModelosSessoesTreino` quando houver conteúdo relacionado.

### Guardrail
Trekking 2.0 é contexto e descoberta. O sistema **não monta rota e não prescreve automaticamente distância, ganho de elevação, pace, duração, peso de mochila, carga, volume, intensidade, aptidão ou retorno ao esporte**.

**Próxima etapa:** `v0.26.6 — Recreational Sports 2.0`.

## v0.26.4 — Rowing 2.0

O AESYN Explore agora aprofunda **Remo** com uma leitura didática da sequência **Catch → Drive → Finish → Recovery**, conectando técnica geral a capacidades de suporte e a sessões-modelo já existentes.

### O que entra nesta versão
- **Fases da remada:** Catch, Drive, Finish e Recovery.
- **Potência coordenada:** pernas, tronco e braços trabalhando em sequência.
- **Base física:** cadeia posterior, core/postura, ombro/escápula, Resistência específica e ritmo/coordenação.
- **Referências reais:** reaproveitamento editorial de `ModelosSessoesTreino` quando houver conteúdo relacionado.

### Guardrail
Rowing 2.0 é contexto e descoberta. O sistema **não prescreve automaticamente cadência, stroke rate, distância, split/500 m, potência, carga, volume, intensidade, aptidão ou retorno ao esporte**.

**Próxima etapa:** `v0.26.5 — Trekking 2.0`.

## v0.26.3 — Martial Arts 2.0

- Aprofunda Artes Marciais sem eleger uma luta específica como padrão.
- Organiza Base e postura, deslocamento, distância/espaço, rotação/transferência de força e reação/coordenação.
- Expõe capacidades de equilíbrio, core, ombro/escápula, quadril, potência/agilidade e Condicionamento de suporte.
- Referencia `ModelosSessoesTreino` já existentes quando houver correspondência editorial.
- Não ensina combate nem prescreve golpes, rounds, contato, carga, volume, intensidade, aptidão, retorno ao contato ou liberação clínica automaticamente.
- Não cria migration ou tabela nova.



## v0.26.1 — Swimming 2.0

- Aprofunda Natação com Crawl, Costas, Peito e Borboleta.
- Organiza respiração/alinhamento, braçada/propulsão, pernada e saída/virada.
- Expõe capacidades de resistência aquática, ombro/escápula, core/alinhamento e mobilidade útil.
- Referencia `ModelosSessoesTreino` já existentes quando houver correspondência editorial.
- Não prescreve metragem, séries, ritmo, volume, intensidade, águas abertas, aptidão ou retorno à água automaticamente.
- Não cria migration ou tabela nova.

# AESYN Performance

> **Athlete & Human Performance** — acompanhamento humano, saúde e performance centrados em medicina do esporte.

## Visão do produto

O AESYN está evoluindo de um sistema que registra dados de treino, nutrição e acompanhamento para uma **plataforma longitudinal de acompanhamento humano e performance**. A pessoa é o centro: corpo, movimento, esportes, recuperação, nutrição, comportamento, objetivos, rotina e contexto profissional devem convergir para uma experiência coerente.

O objetivo não é substituir médicos, nutricionistas, treinadores ou outros profissionais. O AESYN deve **aumentar autonomia com contexto**, reduzir atrito, organizar informação e ajudar o profissional a perceber o que merece atenção.








































### Sports Expansion II Foundation

A `v0.26.0` abre a segunda expansão esportiva do AESYN com **Natação, Triathlon, Artes Marciais, Remo, Trekking e esportes recreativos**. As modalidades entram na biblioteca profissional, no Explore e no Interest Engine usando a mesma cadeia `Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão`.

A fundação organiza ambientes, recursos, fundamentos e capacidades sem declarar aptidão, escolher automaticamente uma arte marcial, montar rota, definir distância, volume, intensidade ou retorno ao esporte. A próxima etapa aprofunda **Natação** como modalidade específica.

### Tennis & Beach Tennis 2.0

A `v0.25.3` fecha a primeira expansão esportiva aprofundando **Tênis** e **Beach Tennis** como modalidades de raquete com contextos próprios. O Tênis considera quique, tipo de quadra, forehand/backhand, saque, aceleração, frenagem e mudança de direção; o Beach Tennis enfatiza jogo sem quique, areia, voleios, smash, reação e estabilidade em superfície deformável.

O Explore relaciona `ModelosSessoesTreino` existentes quando houver correspondência editorial, sem criar sessão artificial. A plataforma não define carga, volume, intensidade, aptidão ou retorno ao esporte automaticamente.

### Basketball & Volleyball 2.0

A `v0.25.2` aprofunda **Basquete** e **Vôlei** como contextos de quadra distintos. O Basquete enfatiza drible, passe, arremesso, aceleração e deslocamento lateral; o Vôlei destaca recepção, levantamento, ataque/saque, salto/aterrissagem e ações repetidas acima da cabeça.

O atleta também vê referências reais de `ModelosSessoesTreino` quando houver correspondência editorial. A plataforma explica diferenças esportivas sem criar protocolo, salto-alvo, intensidade ou retorno ao esporte automaticamente.

### Football & Futsal 2.0

A `v0.25.1` aprofunda **Futebol** e **Futsal** como contextos esportivos diferentes. Campo e quadra passam a ter explicações próprias de dinâmica, fundamentos técnicos, capacidades físicas e recursos.

O endpoint do atleta também relaciona `ModelosSessoesTreino` existentes por sinais editoriais, sem copiar ou atribuir essas sessões. A comparação deixa explícito que maior espaço no futebol e maior densidade de ações no futsal mudam o contexto, mas não geram intensidade, volume ou retorno ao esporte automaticamente.

### Sports Expansion I Foundation

A `v0.25.0` abre a expansão esportiva de **Futebol, Futsal, Basquete, Vôlei, Tênis e Beach Tennis**. Essas modalidades entram na mesma taxonomia da biblioteca profissional, com ambientes, equipamentos, objetivos e capacidades específicas.

No Explore, o atleta pode conhecer a estrutura dessas modalidades e também declará-las como interesse. A fundação organiza conhecimento e cobertura editorial; não declara aptidão, não inicia sessão e não converte uma modalidade escolhida em prescrição.

### Interest Engine Foundation

A `v0.24.9` cria uma fonte explícita de **interesses declarados pelo atleta**. O sistema passa a separar três contextos que antes podiam se confundir: atividade já relatada, plano profissional e aquilo que a pessoa quer experimentar/retomar/conhecer.

Os interesses são persistidos por paciente e organização, podem ser atualizados pelo próprio atleta e alimentam o bloco **QUERO FAZER** da bússola do Explore. O AESYN não infere preferência clínica a partir de atividade passada, adesão, prontidão ou plano.

### Sports Starter Packs 2.0

A `v0.24.8` leva Starter Packs ao AESYN Explore como **referências navegáveis por modalidade e objetivo**. Os packs reutilizam `ModelosSessoesTreino` já existentes e mostram capacidades e sessões-modelo relacionadas.

O atleta pode explorar a estrutura, mas o sistema não copia sessão, não atribui plano, não inicia treino e não publica prescrição automaticamente. Packs sem sessão relacionada aparecem como lacuna editorial, em vez de ganhar conteúdo artificial.

### Learn Fundamentals 2.0

A `v0.24.7` transforma **Aprender fundamentos** em uma experiência educacional navegável por modalidade e capacidade. Cada fundamento explica o conceito, o que observar, um erro comum e o próximo passo de aprendizagem.

O conteúdo é educacional e contextual. Não define séries, repetições, carga, pace, volume, intensidade, aptidão clínica ou progressão automática.

### Outdoor Mode 2.0

A `v0.24.6` transforma **Quero ir para fora** em um explorador contextual por ambiente externo, recurso e interesse. O AESYN consulta o catálogo `Exercicios` e mostra possibilidades reais para Rua, Parque, Praça, Trilha leve e Área externa livre.

Outdoor Mode não define rota, distância, pace, carga, volume, duração ou intensidade. O ambiente ajuda a organizar possibilidades; não vira prescrição automática.

### Travel Mode 2.0

A `v0.24.5` adiciona **Estou viajando** ao AESYN Explore. A pessoa informa o tipo de hospedagem/espaço, os recursos disponíveis e a rotina temporária; o sistema reaproveita o catálogo `Exercicios` para mostrar possibilidades compatíveis.

Quando existe plano ativo, ele permanece visível como referência. Travel Mode não transforma viagem em deload automático, não substitui o plano profissional e não fabrica uma ficha específica de viagem.

### Quick Movement 2.0

A `v0.24.4` transforma **Tenho pouco tempo** em um explorador contextual. A pessoa escolhe uma janela disponível, o contexto e a preferência; o AESYN consulta `Exercicios` e organiza possibilidades reais do catálogo.

A janela de tempo é apenas contexto de exploração. **Curto não significa intenso**: o sistema não define duração exata da execução, séries, repetições, carga, volume ou intensidade.

### Home Workout 2.0

A `v0.24.3` transforma **Mover em casa** em um explorador contextual. O paciente escolhe espaço disponível, recurso e preferência de movimento; o AESYN consulta o catálogo profissional `Exercicios` e mostra possibilidades compatíveis com aquele recorte.

A lista não é uma ficha: não define séries, repetições, carga, duração, intensidade ou progressão. Se nenhum movimento real do catálogo combinar com o contexto, a interface mostra a lacuna em vez de inventar conteúdo.

### Beginner Journeys 2.0

A `v0.24.2` transforma as modalidades do Start a Sport em **jornadas educacionais de familiaridade**. Cada jornada possui etapas com objetivo educacional, evidência de familiaridade e uma explicação de quando faz sentido seguir aprendendo.

As jornadas não registram “aprovação”, não alteram treino, não avançam carga/volume e não funcionam como liberação clínica. São uma estrutura para a pessoa entender melhor uma modalidade antes de decisões de treinamento.

### Start a Sport 2.0

A `v0.24.1` transforma **Começar um esporte** em uma jornada navegável. O paciente pode explorar Caminhada, Corrida, Ciclismo, Calistenia e Musculação, entendendo ambientes, recursos, fundamentos, primeiro marco e próximo passo de aprendizado.

A jornada é educacional: não define carga, pace, volume, zona, séries, repetições ou aptidão clínica. Quando existe um plano ativo, a própria API deixa explícito que Explore é complementar e não substitui a prescrição atual.

### AESYN Explore Foundation

A `v0.24.0` inicia a fase de autonomia guiada do atleta. Explore conecta o plano ativo, o ciclo esportivo, a atividade relatada e caminhos de descoberta como **Começar um esporte**, **Mover em casa**, **Tenho pouco tempo**, **Quero ir para fora** e **Aprender fundamentos**.

A experiência é construída sobre três perguntas: **Preciso fazer**, **Quero fazer** e **Posso fazer hoje**. Explore não altera prescrição, não libera atividade clinicamente contraindicada e não define intensidade automaticamente.

### Movement Library Mobile & Accessibility 2.0

A `v0.23.8` fecha a ergonomia da biblioteca profissional para telas pequenas e navegação assistiva. A bancada ganha foco visível, `aria-label`, região `aria-live`, navegação por setas/Home/End, suporte a `Escape`, alvos de toque maiores e foco contextual após navegação interna.

No mobile, filtros, mapas, cards e áreas de conteúdo passam a respeitar `safe-area`, larguras pequenas e rolagem horizontal controlada. A interface também respeita `prefers-reduced-motion` e `prefers-contrast`.

### Professional Movement Library 2.0

A `v0.23.7` consolida a fase profissional da biblioteca em uma única bancada de curadoria. Taxonomia, cobertura, Starter Packs, catálogo de exercícios e sessões reutilizáveis passam a ter navegação direta dentro da mesma experiência.

A tela mantém visível a cadeia `Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão` e reforça a separação entre **curadoria** e **prescrição**: organizar a biblioteca não publica nem altera automaticamente o plano de qualquer paciente.

### Movement Templates & Starter Packs Foundation

A `v0.23.6` organiza `ModelosSessoesTreino` existentes em **Starter Packs profissionais** derivados da taxonomia de modalidade, objetivo e capacidade. Cada pack é apenas um agrupamento de referências: nenhuma sessão é clonada, nenhum plano é criado e nada é publicado automaticamente para pacientes.

O endpoint `/api/biblioteca-movimento/starter-packs` mostra packs com sessões existentes e também packs ainda sem sessão, tornando a lacuna editorial visível antes da fase AESYN Explore.

### Movement Library Coverage & Quality 2.0

A `v0.23.5` torna a biblioteca **auditável editorialmente**. O próprio `GET /api/biblioteca-movimento` passa a informar cobertura por modalidade e no recorte filtrado: capacidades com sessão, capacidades com exercício, movimentos relacionados, movimentos com descrição e movimentos com mídia.

As lacunas são mostradas de forma explícita e viram uma lista de prioridades editoriais para o profissional. O sistema não atribui um “score clínico” à biblioteca e não preenche automaticamente conteúdo ausente; cobertura significa apenas presença real nas fontes existentes.

### Movement Instructions & Media Foundation

A `v0.23.4` torna os movimentos da biblioteca mais ensináveis sem criar uma segunda ficha de exercício. Ao tocar em um movimento, a biblioteca busca `GET /api/biblioteca-movimento/exercicios/{id}` e apresenta a **descrição/instrução cadastrada** e a referência de **vídeo (`VideoUrl`)** do próprio registro `Exercicios`.

Quando descrição ou vídeo ainda não existem, a interface mostra explicitamente a ausência do conteúdo em vez de inventar instruções. O profissional pode abrir o catálogo de exercícios para completar a fonte original.

### Movement Progression & Regression Foundation

A `v0.23.3` completa a primeira taxonomia da biblioteca com uma camada explícita de **progressão e regressão**. Cada capacidade passa a mostrar eixos estruturais como controle, volume, carga externa, duração/densidade, velocidade e amplitude quando aplicáveis.

Esses eixos não alteram prescrição, carga ou exercício automaticamente. Eles servem como metadados de navegação para o profissional. Quando houver paciente vinculado, a decisão pode ser sustentada pelo histórico real já disponível em `ProgressaoExerciciosTreinoController`, que compara cargas apenas dentro do mesmo exercício e unidade.

### Movement Session Model 2.0

A `v0.23.2` preenche a camada **Sessão** da taxonomia esportiva sem criar uma biblioteca paralela. Cada capacidade passa a exibir sessões reutilizáveis encontradas em `ModelosSessoesTreino`, a mesma fonte usada pelo Workout Builder.

A relação entre modalidade/objetivo/capacidade e sessão é organizada pela taxonomia e pelo conteúdo descritivo dos modelos existentes. Clicar em uma sessão leva à biblioteca profissional de treinos/sessões; nenhum modelo é copiado ou recriado automaticamente.

### Movement Taxonomy & Filters 2.0

A `v0.23.1` transforma a fundação da biblioteca em navegação estruturada. O profissional pode combinar filtros de **modalidade, objetivo, capacidade, ambiente e equipamento**, além de fazer busca textual dentro do resultado visível.

Os filtros estruturais são aplicados pelo endpoint `/api/biblioteca-movimento`; a busca textual continua instantânea no frontend. O catálogo `Exercicios` permanece a única fonte dos movimentos vinculados.

### Sports & Movement Library Foundation

A `v0.23.0` inicia a fase de biblioteca esportiva com a taxonomia `Modalidade → Objetivo → Capacidade → Sessão → Exercício → Progressão`.

A fundação começa com **Musculação, Caminhada, Corrida, Calistenia, Mobilidade, Condicionamento e Ciclismo**. A taxonomia não cria uma segunda tabela de exercícios: o endpoint `/api/biblioteca-movimento` referencia exclusivamente o catálogo profissional já existente em `Exercicios`.

Isso permite ampliar modalidades sem quebrar o Workout Builder e sem duplicar movimentos. A biblioteca organiza possibilidades; seleção, progressão e prescrição continuam dependentes do contexto individual e do julgamento profissional.

### Daily Premium UX 2.0

A `v0.22.9` fecha a fase `v0.22.x` refinando a experiência do Daily sem criar novas regras clínicas. Os blocos de visão do dia, contexto, plano, ações, fluxo rápido, reflexão e histórico passam a compartilhar hierarquia visual, espaçamento, foco, toque e estados mais consistentes.

A experiência mobile ganha áreas tocáveis mínimas, feedback de interação, foco visível por teclado, `aria-label`, `aria-live`, suporte a `prefers-reduced-motion` e menor densidade visual. O bloco de 30 segundos recebe destaque como foco operacional do dia.

### Professional Daily Signals 2.0

A `v0.22.8` leva sinais recentes do Daily para o **Professional Command Center**. A fila não usa um score clínico novo: ela reaproveita a recomendação de prontidão já existente, o fechamento do dia e a ausência de check-in recente.

As regras são explícitas: `Recuperacao` entra como **Revisar hoje**; `Leve` ou fechamento com percepção `≤ 4/10` entram como **Observar**; ausência de check-in por 3 ou mais dias entra como **Contexto pendente**. Cada paciente mostra o motivo e os fatores brutos disponíveis.

A fila é operacional e explicável. Não representa diagnóstico, risco clínico, prioridade médica automática ou substituição da avaliação profissional.

### Daily History 2.0

A `v0.22.7` cria uma linha do tempo diária real dos últimos dias, reunindo **Morning Check-in, treino executado e Evening Reflection** a partir dos registros persistidos.

O endpoint `/api/portal/me/historico-diario` consulta as fontes já existentes (`ProntidoesDiarias`, `ExecucoesTreino` e `RegistrosDiarioPaciente`) e não cria tabela paralela. Dias sem informação permanecem explicitamente como lacunas, sem serem classificados como fracasso, baixa adesão ou risco.

### Evening Reflection 2.0

A `v0.22.6` evolui o fechamento diário existente para uma reflexão guiada e leve. Em menos de um minuto, a pessoa registra percepção final do dia, quanto conseguiu executar, dificuldade percebida, energia final e um aprendizado simples.

A implementação reaproveita `/api/portal/me/fechamento-dia` e a fonte de verdade existente. Os novos elementos são consolidados no resumo do fechamento, preservando compatibilidade sem criar tabela ou endpoint paralelo.

A reflexão não pune baixa execução nem interpreta cansaço como falha. O objetivo é aumentar qualidade do contexto longitudinal para a própria pessoa e para o profissional.

### Daily 30 Seconds Experience 2.0

A `v0.22.5` consolida o fluxo diário essencial em uma experiência curta: **check-in → contexto → plano → próxima ação**. O objetivo é permitir que a pessoa entenda o dia e escolha o próximo passo em aproximadamente 30 segundos, principalmente no mobile.

A experiência reutiliza Morning Check-in, Daily Readiness Context, Today's Plan e Action Hub. O AESYN orienta sem julgar, sem penalizar dias ruins e sem transformar o fluxo em diagnóstico ou obrigação.

### Action Hub 2.0

A `v0.22.4` centraliza as **ações mais úteis do dia** em um único ponto. O paciente consegue acessar rapidamente treino, alimentação, check-in, hidratação, peso e contexto diário sem precisar procurar cada função em telas diferentes.

O hub é contextual: ações sem plano disponível aparecem desabilitadas, e o sistema não cria novas obrigações. O objetivo é reduzir atrito operacional, especialmente no mobile.

### Today's Plan 2.0

A `v0.22.3` transforma o contexto diário em uma visão prática do **plano de hoje**. O AESYN reúne treino ativo, plano nutricional, objetivo principal, disponibilidade, logística, recursos e readiness para mostrar o que já está planejado e como abordar o dia.

O sistema não inventa prescrição quando não existe plano e não altera intensidade automaticamente. O contexto diário serve para apoiar execução, autocuidado e conversa com o profissional.

### Daily Readiness Context 2.0

A `v0.22.2` transforma o Morning Check-in em uma leitura diária **explicável**. Sono, energia, dor, disposição e recuperação aparecem como fatores separados, com indicação descritiva de contexto favorável, intermediário ou de atenção.

O AESYN mostra **por que** o dia está sendo lido daquela forma, sem criar um score clínico opaco. Um único fator não determina conduta, treino ou diagnóstico; a leitura existe para apoiar conversa, autocuidado e decisão profissional contextualizada.

### Morning Check-in 2.0

A `v0.22.1` evolui o check-in diário do paciente para uma experiência mobile-first de aproximadamente **30 segundos**. O fluxo organiza sono, qualidade do sono, energia, dor, disposição e recuperação percebida com escalas claras e feedback visual imediato.

O check-in reaproveita o contrato de prontidão já existente, sem criar uma segunda fonte de verdade. As respostas alimentam a prontidão diária e o contexto longitudinal disponível ao profissional. Dias ruins continuam sendo informação útil; não existe incentivo para “agradar” o sistema.

### AESYN Daily Foundation

A `v0.22.0` inaugura o **AESYN Daily**, camada diária que conecta quatro perguntas simples: **Preciso fazer**, **Quero fazer**, **Posso fazer hoje** e **Como estou**.

A fundação reaproveita plano profissional, objetivos, identidade esportiva, contexto de vida e sinais recentes. O Daily organiza o dia; não prescreve sozinho, não diagnostica e não substitui orientação profissional.

### Human Profile Synthesis 2.0

A `v0.21.4` fecha a fundação do Human Profile com uma **síntese profissional compacta** de seis dimensões: objetivos, identidade esportiva, corpo, plano atual, contexto de vida e sinais recentes.

A síntese serve como ponto de partida para consulta e acompanhamento. Ela mantém caráter explicável e descritivo: não gera diagnóstico, não classifica risco e não substitui a abertura das fontes originais quando o profissional precisa de detalhe.

### Life Context Foundation

A `v0.21.3` adiciona ao Human Profile uma camada de **contexto de vida**. O AESYN passa a reunir rotina, disponibilidade, logística, equipamentos, preferências e limitações registradas para responder melhor à tríade **Preciso fazer / Quero fazer / Posso fazer hoje**.

Essa camada é descritiva e contextual. Ela não prescreve automaticamente e não interpreta ausência de informação como ausência de restrição.

### Sports Identity Foundation

A `v0.21.2` acrescenta ao Human Profile uma leitura de **identidade esportiva**. O AESYN passa a reunir modalidades atuais, nível de experiência, frequência, contexto preferido, histórico esportivo e relação com objetivos ativos.

A identidade esportiva é longitudinal e contextual: ela não é tratada como um rótulo fixo, não presume aptidão e não infere nível competitivo quando isso não estiver registrado.

### Multi-Goal Engine

A `v0.21.1` amplia o Human Profile com um **mapa de objetivos simultâneos**. O motor reúne objetivos já existentes no ciclo atual, treino ativo, nutrição ativa e avaliação/intake; remove duplicidades sem perder as fontes originais e apresenta uma hierarquia operacional simples: **Principal / Complementar / Acompanhado**.

A hierarquia serve para organizar contexto, não para produzir prioridade clínica. O motor também classifica objetivos em domínios descritivos como composição corporal, performance, movimento e saúde/rotina, sempre de forma explicável e sem diagnóstico.

### Human Profile Foundation

A `v0.21.0` inaugura o **Human Profile** como camada longitudinal da pessoa. A visão profissional passa a reunir, em um único bloco, identidade, objetivos já registrados, esporte/movimento, rotina/recuperação e contexto/limitações presentes na anamnese e nos planos existentes.

A fundação não cria uma segunda fonte de verdade: ela agrega dados já existentes e deixa explícito quando uma dimensão ainda não possui contexto. O indicador **dimensões com contexto** mede somente completude informacional, não saúde, risco ou qualidade clínica.

A próxima etapa é o **Multi-Goal Engine**, para permitir objetivos simultâneos com prioridade e acompanhamento explícitos.

### Period Comparison 2.0

O profissional pode alternar entre **7 dias**, **30 dias** e **início × atual**. A comparação reaproveita dados já carregados de treino, diário, tendência semanal e avaliações corporais; não cria uma fonte paralela nem infere significado clínico a partir da direção numérica.

### North Star

> **O AESYN precisa conhecer o atleta melhor a cada dia — com consentimento, contexto e utilidade.**

Para o atleta, o produto deve responder principalmente:

- **Como estou hoje?**
- **O que preciso fazer agora?**
- **Estou evoluindo?**
- **Existe algo que meu profissional quer que eu observe?**
- **Quero me movimentar: o que consigo fazer hoje?**

Para o profissional:

- **Quem precisa de mim hoje?**
- **Por quê?**
- **Como essa pessoa evoluiu desde a última revisão?**
- **O que mudou em treino, nutrição, recuperação, adesão ou sintomas relatados?**

## Pilares do AESYN

- **Human Profile** — a pessoa como unidade central, e não um conjunto de tabelas.
- **AESYN Daily** — o que fazer e observar hoje.
- **AESYN Professional** — acompanhamento profissional e fila de atenção.
- **AESYN Training** — prescrição, execução, progressão e performance.
- **AESYN Nutrition** — nutrição esportiva, planejamento e flexibilidade.
- **AESYN Recovery** — sono, fadiga, desconforto, prontidão e recuperação.
- **AESYN Explore** — esportes, movimento, treinos em casa e jornadas para começar do zero.
- **AESYN Progress** — tornar a evolução visível e compreensível.
- **AESYN Intelligence** — transformar histórico em contexto, tendências e padrões explicáveis, sem inventar diagnóstico.

## Autonomia guiada

O AESYN deve diferenciar três contextos:

1. **Preciso fazer** — o que foi orientado/prescrito pelo profissional.
2. **Quero fazer** — interesses, esportes e atividades que a pessoa deseja praticar.
3. **Posso fazer hoje** — tempo disponível, local, equipamento, experiência e condição relatada.

Essa separação permite que o produto ajude alguém a começar uma corrida, experimentar calistenia, aprender fundamentos de um esporte ou treinar em casa sem misturar exploração voluntária com uma prescrição profissional.

## Modelo de operação do desenvolvimento

- Revisões locais `-r1`, `-r2`, etc. não são commitadas individualmente; commit/push ocorre apenas quando a versão funcional passa integralmente no `TESTAR.ps1`.

O desenvolvimento é incremental e parte **sempre do estado real mais recente do projeto**. Arquivos existentes não devem ser substituídos por versões genéricas quando puderem ser evoluídos preservando histórico, decisões e gates já construídos.

### Fluxo local oficial

```text
PREPARAR.ps1 → RODAR.ps1 → TESTAR.ps1
```

- `PREPARAR.ps1`: valida ambiente, dependências, banco, migrations, build e pré-condições.
- `RODAR.ps1`: inicia a aplicação local.
- `TESTAR.ps1`: executa os gates funcionais e de produto sem usar produção como laboratório.

### Documentação viva obrigatória

Toda nova versão deve revisar e atualizar, quando aplicável:

- `README.md` — visão, operação, estado atual e capacidades relevantes;
- `ROADMAP.md` — concluído, etapa atual, próxima etapa, replanejamentos e futuro;
- `CHANGELOG.md` — o que mudou;
- `VERSION.txt` e versões públicas da aplicação;
- `PREPARAR.ps1`;
- `RODAR.ps1`;
- `TESTAR.ps1`.

Uma versão não está completa apenas porque o código funciona: **produto, testes e documentação precisam concordar sobre o estado do AESYN**.

## Estado atual

- **Versão funcional:** `v0.26.1 — Swimming 2.0`
- **Fase:** Professional Core / transição para acompanhamento humano longitudinal
- **Próxima versão planejada:** `v0.21.0 — Human Profile Foundation`
- **Baseline aprovada:** `v0.20.9 — Clinical & Sports Snapshot 2.0` aprovada pelo AESYN Product Gate e commitada como versão funcional estável.

### O que a v0.20.10 entrega

> Comparar períodos com janela explícita, sem transformar variação numérica em conclusão clínica.

O **Period Comparison 2.0** permite alternar entre **7 dias**, **30 dias** e **início × atual**. A janela semanal reutiliza a tendência longitudinal já existente; a janela de 30 dias compara execuções de treino, minutos, esforço médio e registros de diário com os 30 dias anteriores; e a comparação de início usa a primeira e a avaliação corporal mais recente. Quando não existe base suficiente, o AESYN informa isso explicitamente.

### O que a v0.20.6 entrega

> Ao abrir um paciente, o profissional entende em poucos segundos como aquela pessoa está.

O **Patient Overview 2.0** adiciona um cockpit no topo da visão geral com objetivo atual, status, tags, prontidão, treino recente, adesão, peso/tendência, planos vigentes, último evento longitudinal e uma próxima revisão operacional explicável. Os sinais servem para orientar a navegação; não diagnosticam, prescrevem ou alteram conduta automaticamente.

### O que a v0.20.7 entrega

> Explicar **por que** uma pessoa entrou em atenção e levar o profissional diretamente ao dado que merece revisão.

O **Attention Reasons 2.0** transforma os sinais da Central de Atenção em motivos estruturados e deduplicados. Cada motivo informa classificação (prioridade, observação ou operacional), origem, período e contexto, com navegação direta para treino, alimentação, diário, timeline, pendências ou follow-ups. A classificação organiza trabalho; não cria diagnóstico nem altera conduta automaticamente.


### O que a v0.20.9 entrega

O prontuário profissional agora inclui o **Clinical & Sports Snapshot 2.0**, uma síntese compacta e navegável de corpo, performance, recuperação, nutrição e adesão ao protocolo. A visão reaproveita os dados longitudinais já carregados pelo prontuário, mostra tendência corporal quando existe avaliação anterior, carga de treino e recuperação de 7 dias, comparação semanal quando o backend possui base suficiente e os acontecimentos mais recentes da timeline. Cada bloco leva ao domínio de origem, preservando explicabilidade e decisão profissional. Não há diagnóstico, estimativa automática de risco ou alteração de prescrição.

### O que a v0.20.8 entrega

> Diminuir a distância entre perceber um problema e executar a próxima ação profissional adequada.

O **Professional Action Center 2.0** reúne, no mesmo contexto, chat, solicitação explícita de check-in, nota interna, revisão de treino, revisão nutricional, follow-up, pendência e agendamento de retorno. O Action Center aparece tanto na Central de Atenção quanto no Patient Overview, sempre preservando o prontuário completo e exigindo ação consciente do profissional.

### Objetivo da v0.20.9

> Transformar dados clínicos e esportivos dispersos em um snapshot longitudinal curto, comparável e útil durante revisão e consulta.

A próxima etapa será o **Clinical & Sports Snapshot 2.0**, aproximando corpo, recuperação, treinamento, nutrição e contexto longitudinal em uma leitura profissional compacta.

## Roadmap resumido rumo ao 1.0

| Faixa | Direção |
|---|---|
| `0.20.x` | Professional Core Completion |
| `0.21.x` | Human Profile |
| `0.22.x` | AESYN Daily |
| `0.23.x` | Sports & Movement Library |
| `0.24.x` | AESYN Explore / Movement Discovery |
| `0.25–0.26.x` | Expansão de modalidades esportivas |
| `0.27.x` | Workout Intelligence 3.0 |
| `0.28.x` | Athlete Performance Passport |
| `0.29.x` | Progress Intelligence |
| `0.30.x` | Sports Nutrition 3.0 |
| `0.31.x` | Recovery Intelligence |
| `0.32.x` | Human Timeline |
| `0.33.x` | AESYN Connect |
| `0.34.x` | Professional Attention Intelligence |
| `0.35.x` | Consultation Mode |
| `0.36.x` | Goal Intelligence |
| `0.37–0.38.x` | Progression, gamificação positiva e desafios |
| `0.39–0.41.x` | Experiência adaptativa, padrões e insights |
| `0.42–0.45.x` | Return to Sport, calendário, journal e círculos de apoio |
| `0.46–0.48.x` | Premium UX, mobile native feel e notificações |
| `0.49–0.55.x` | Segurança, escala, equipes, colaboração, onboarding e acessibilidade |
| `0.56–0.59.x` | Product Readiness |
| `0.60.x` | AESYN Beta / jornadas completas |
| `0.61–0.64.x` | Validação real + feedback + product analytics |
| `0.65–0.69.x` | Fundação comercial e portabilidade de dados |
| `0.70–0.89.x` | Faixa deliberadamente adaptativa, guiada pelo uso real |
| `0.90–0.99.x` | Feature freeze e hardening para release |
| `1.0.0` | Produto comercial estável |

O detalhamento e os replanejamentos pertencem ao [`ROADMAP.md`](ROADMAP.md).

---

# Histórico de versões e capacidades

## v0.20.8 — Professional Action Center 2.0

A Central de Atenção e o Patient Overview passam a oferecer uma camada única de ações rápidas. O profissional pode abrir chat, solicitar check-in, registrar nota interna, revisar treino ou nutrição, registrar follow-up, criar pendência ou agendar retorno sem procurar cada ferramenta em módulos diferentes. Toda ação continua explícita, contextual e dependente de decisão profissional.

## v0.20.7 — Attention Reasons 2.0

A Central de Atenção passa a explicar cada motivo de forma estruturada, separando prioridade, observação e pendência operacional. Os sinais mostram origem, período, detalhe e destino de navegação, reduzindo a necessidade de procurar contexto em várias telas. Motivos equivalentes são deduplicados e a configuração profissional da fila continua preservada.

## v0.20.6 — Patient Overview 2.0

A visão profissional do paciente ganha um cockpit de leitura rápida que reutiliza os dados longitudinais já carregados pelo prontuário. Objetivo, status, tags, prontidão, treino, adesão, peso, planos vigentes, último evento e sinais operacionais aparecem antes do detalhamento clínico. A interface é responsiva, compatível com dark mode e mantém julgamento clínico e decisão de conduta com o profissional.

## v0.20.5 — Tags & Segmentation

A carteira profissional agora suporta tags personalizadas por paciente, edição pela lista ou pelo perfil e filtro direto por tag. As alterações são auditadas, normalizadas e deduplicadas, permitindo segmentar grupos operacionais sem modificar dados clínicos ou prescrições.

## v0.20.3 — Patient Status Management

A carteira profissional agora diferencia pacientes ativos, pausados, aguardando avaliação e encerrados. A mudança é administrativa, auditada e não apaga histórico clínico.

## v0.20.2 — Patient Attention Queue 2.0

A Central de Atenção agora possui uma fila profissional configurável, construída sobre os sinais já consolidados do sistema. O profissional pode ajustar o score mínimo, incluir ou ocultar casos de observação e priorizar follow-ups vencidos sem alterar automaticamente qualquer conduta clínica.

## v0.20.1 — Patient Search & Advanced Filters

A carteira profissional agora possui busca e filtros avançados por status, responsável, aderência, interação, próxima revisão e marcadores operacionais, com ordenação e layout responsivo.

## v0.20.0 — Professional Dashboard 2.0

A fase `Professional Workspace 2.0` começa com um dashboard executivo único para o profissional: consultas do dia, pacientes em atenção, pendências abertas, mensagens não lidas, follow-ups e revisões clínicas aparecem em uma leitura, com atalhos para agir sem navegar por vários módulos.

**Fluxo local:** `PREPARAR.ps1` → `RODAR.ps1` → `TESTAR.ps1`. Testes funcionais de desenvolvimento permanecem locais e não usam a VPS de produção.

## v0.19.44 — Product Flow Gate / Lista 03 Closure

A v0.19.44 fecha a Lista 03 como fase de estabilização do produto. O `TESTAR.ps1` passa a conferir transversalmente se os fluxos essenciais de paciente e profissional continuam conectados, enquanto `docs/PRODUCT-FLOW-GATE.md` define a homologação manual ponta a ponta que deve ser executada com dados de teste antes de produção comercial.

O smoke test continua **não destrutivo** e exclusivamente local; ele não usa a VPS de produção para criar pacientes, treinos, dietas, check-ins ou mensagens. A etapa originalmente planejada como `v0.20.6 — Shared Care / Multiple Professionals` foi posteriormente replanejada para uma fase mais madura de colaboração profissional; o histórico desta versão é preservado aqui.

**Fluxo local:** `PREPARAR.ps1` → `RODAR.ps1` → `TESTAR.ps1`.

## v0.19.43 — Audit, Privacy & Consent

A versão 0.19.43 adiciona governança de privacidade à conta: aceites versionados de Termos e Política de Privacidade, painel de privacidade, auditoria consultável pelo próprio usuário, solicitação de exclusão para análise e desativação segura da conta. A migration `V01943AuditPrivacyConsent` persiste o histórico mínimo necessário sem apagar registros clínicos silenciosamente. A documentação em `docs/` é operacional e deve passar por revisão jurídica antes da operação comercial.

## v0.19.42 — Offline & Poor Connection Resilience

O AESYN agora diferencia **sem internet**, **conexão instável** e falhas reais do aplicativo. A interface exibe um banner persistente, preserva rascunhos explicitamente marcados durante a sessão, oferece retry/reconexão e permite fila offline somente para ações declaradas como seguras e deduplicáveis. A API continua fora do cache do Service Worker.

O chat já preserva o texto digitado via `sessionStorage`; marcações de notificação podem ser retomadas após reconexão; ações duplicadas podem ser protegidas por chave com `hpRunOnceV01942`. Nenhuma migration foi necessária.

**Fluxo local:** `PREPARAR.ps1` → `RODAR.ps1` → `TESTAR.ps1`. Desenvolvimento continua sem executar testes funcionais contra a VPS de produção.

## v0.19.41 — UX State System

O frontend passa a ter um contrato visual reutilizável para **loading/skeleton, vazio, erro, retry, sucesso, toast e confirmação**. O objetivo é reduzir estados improvisados e fazer telas novas seguirem o mesmo comportamento no desktop, mobile e dark mode.

A confirmação própria `hpConfirm` também prepara a retirada gradual de `window.confirm` dos fluxos críticos, enquanto `hpRunAction` padroniza botões em processamento. Nenhuma migration foi necessária.

**Fluxo local:** `PREPARAR.ps1` → `RODAR.ps1` → `TESTAR.ps1`. Desenvolvimento continua sem executar testes funcionais contra a VPS de produção.

### v0.19.35 — Chat Reliability & Context Foundation

- mensagens não lidas e badge no chat;
- abertura da conversa marca notificações correspondentes como lidas;
- status Enviada / Entregue / Lida para mensagens próprias;
- retry com preservação do texto digitado;
- referências estruturadas para arquivo, treino, exercício, refeição e exame;
- integração da biblioteca de arquivos com o contexto do chat;
- isolamento paciente/profissional/organização preservado;
- sem migration nova.

## v0.19.34 — Patient Files Mobile 2.0

A biblioteca de arquivos do paciente agora aceita exames, laudos, fotos e PDFs tanto pelo profissional quanto pelo próprio paciente. O upload foi pensado para celular (câmera, galeria ou seletor de arquivos), com metadados pesquisáveis, download autenticado, remoção auditada e referência direta no chat. Os binários ficam em `App_Data/patient-files`; na VPS, mantenha esse diretório em volume persistente.

## v0.19.33-r1 — Workout CRUD & Multi-Plan Completion

Fecha o CRUD profissional de treino e o suporte explícito a múltiplos planos por paciente. Planos podem ser criados, editados, duplicados, arquivados, reativados ou retirados da rotina ativa sem apagar histórico. O portal do paciente passa a exibir todos os planos ativos e permite iniciar livremente qualquer sessão publicada.

## v0.19.32 — Professional Nutrition Access Completion

A área profissional de Nutrição agora expõe o fluxo completo do que já existia no backend: criar e editar dietas, refeições, alimentos, substituições/equivalências, metas, modelos, atribuição, duplicação/progressão, arquivamento com preservação de histórico e revisão/publicação.

O catálogo de alimentos também pode ser mantido visualmente sem depender de Swagger.

**Fluxo local:** `PREPARAR.ps1` → `RODAR.ps1` → `TESTAR.ps1`. Desenvolvimento continua sem executar testes funcionais contra a VPS de produção.

## v0.19.31 — Athlete Data Hub

A área do paciente agora separa a rotina diária das métricas técnicas. Use **Dados para Atletas** para consultar performance, recuperação, corpo, consistência e tendências sem sobrecarregar a Home.

## v0.19.30 — Patient Home Cleanup

- Home do paciente orientada à pergunta **“o que eu preciso fazer hoje?”**.
- Novo **Hoje em um olhar** com Check-in, Treino, Alimentação, Hidratação, Mensagens e Pendências.
- Redução de cards duplicados e indicadores técnicos na primeira leitura.
- Análises esportivas e gamificação continuam disponíveis de forma recolhida até a área dedicada **Dados para Atletas** da v0.19.31.
- Layout mobile-first e dark mode preservados.
- Sem migration nesta versão.

**Fluxo local:** `PREPARAR.ps1` → `RODAR.ps1` → `TESTAR.ps1`. Testes funcionais permanecem exclusivamente locais.

## v0.19.28 — Settings & User Profile

Esta versão consolida a conta do usuário dentro das Configurações: dados básicos, alteração de senha, foto de perfil reduzida no navegador, preferência de tema, preferências de notificações e resumo da sessão atual. As preferências passam a ser persistidas no servidor por usuário e as alterações sensíveis permanecem auditadas sem armazenar senha ou conteúdo da foto.

**Fluxo local:** `PREPARAR.ps1` → `RODAR.ps1` → `TESTAR.ps1`. O `TESTAR.ps1` continua isolado de produção/VPS.

## v0.19.27 — Session & Authorization Hardening

- Corrige o 403 do chat do paciente separando corretamente policies de paciente e profissional.
- Adiciona renovação de JWT enquanto a sessão está ativa, logout com revogação por SecurityStamp e tratamento consistente de 401/403.
- Ativa lockout de 5 tentativas / 15 minutos também para contas legadas ao autenticar.
- Restringe a conversa profissional ao profissional responsável resolvido para o paciente, impedindo leitura por GUID de outro profissional da organização.
- Mantém testes de desenvolvimento exclusivamente locais, sem atingir a VPS de produção.

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

## v0.26.2 — Triathlon 2.0

O Explore esportivo aprofunda Triathlon em três disciplinas (natação, ciclismo e corrida), duas transições (T1/T2), capacidades de suporte e referências a sessões-modelo já cadastradas. A camada é educacional/editorial: não gera distância, pace, potência, zonas, volume, intensidade, estratégia nutricional, aptidão ou retorno ao esporte automaticamente.

## v0.27.1 — Prescription Variables 3.0

A prescrição de treino passa a tratar **RIR-alvo**, **Cadência** e **Técnica avançada** como variáveis estruturadas do exercício, deixando de depender de observações livres. A execução também pode registrar RIR realizado, cadência realizada e técnica executada para comparação longitudinal.

- RIR-alvo: inteiro de 0 a 10 por item prescrito;
- Cadência: texto curto preservado sem interpretação automática (ex.: `3-1-1-0`);
- Técnica avançada: estratégia explicitamente prescrita pelo profissional;
- execução: `RirRealizado`, `CadenciaRealizada` e `TecnicaExecutada`;
- Workout Intelligence passa a reconhecer RIR, cadência e técnica como dimensões estruturadas.

**Guardrail:** Prescription Variables 3.0 não automatiza progressão e não prescreve ou altera automaticamente qualquer variável. A decisão continua sob responsabilidade profissional.

Próxima etapa: **v0.27.2 — Prescribed vs Performed 3.0**.


## v0.27.2 — Prescribed vs Performed 3.0

A camada de Workout Intelligence passa a comparar, por exercício, a prescrição vigente com a última execução concluída disponível na janela selecionada.

**Entregue na v0.27.2:**
- comparação de Séries, Repetições, Carga, RIR, Cadência e Técnica;
- estado `SemExecucaoNoPeriodo`, `SemDiferencaRegistrada` ou `DiferencasRegistradas`;
- contagem de execuções comparáveis por item;
- visual profissional Prescrito × realizado dentro do Workout Intelligence;
- diferença registrada é contexto, não score de adesão nem julgamento de execução;
- nenhuma mudança automática de carga, volume, RIR, cadência, técnica ou prescrição.

Próxima etapa: **v0.27.3 — Advanced Techniques 3.0**.


## v0.27.3 — Advanced Techniques 3.0

Técnicas avançadas deixam de depender apenas de texto livre e passam a possuir **código estruturado + parâmetros opcionais**, preservando o campo legado para compatibilidade.

**Catálogo inicial:** Drop set, Bi-set, Rest-pause, Cluster, Myo-reps, Isometria, Pré-exaustão e Tempo controlado.

**Entregue na v0.27.3:**
- endpoint `GET /api/treinos/tecnicas-avancadas`;
- `TecnicaAvancadaCodigo` e `TecnicaAvancadaParametros` na prescrição;
- `TecnicaExecutadaCodigo` e `TecnicaExecutadaParametros` na execução;
- Workout Builder com seleção estruturada e parâmetros;
- compatibilidade com `TecnicaAvancada`/`TecnicaExecutada` legados;
- Workout Intelligence compara código estruturado antes do texto legado;
- o AESYN não escolhe, combina ou aplica técnica automaticamente.

Próxima etapa: **v0.27.4 — Progression & Regression 3.0**.

---

## v0.28.0 — Athlete Performance Passport Foundation

A primeira versão do **Athlete Performance Passport** cria uma visão longitudinal de performance sem inventar dados. A Foundation reaproveita registros de treino que já existem e consolida melhores cargas por exercício, PRs recentes e um mapa explícito de cobertura para **cargas, recordes, tempos, provas, testes, habilidades e marcos**.

Quando um domínio ainda não possui fonte estruturada, a API retorna `SemFonteEstruturada` em vez de inferir desempenho. Melhor carga é sempre comparada dentro do mesmo exercício e unidade.

### Guardrail
O Athlete Performance Passport **não estima 1RM, não fabrica recordes, não certifica habilidade e não substitui avaliação profissional**.

**Próxima etapa:** `v0.28.1 — Performance Records 2.0`.

---

## v0.28.1 — Performance Records 2.0

O Athlete Performance Passport passa a separar formalmente **recordes observados** de **marcas derivadas**.

### Entregue
- `MelhorCarga` como recorde `Observado`;
- `MelhorVolumeEstimado` como marca `Derivado`;
- comparação sempre limitada ao mesmo exercício e à mesma unidade;
- data da marca, recência e quantidade de registros comparáveis;
- evolução percentual da melhor carga em relação ao primeiro registro comparável;
- endpoints dedicados de records para profissional e paciente;
- compatibilidade com o contrato Foundation preservada.

### Guardrail
Performance Records 2.0 **não mistura unidades, não estima 1RM, não transforma volume derivado em carga observada e não fabrica recordes**.

**Próxima etapa:** `v0.28.2 — Timed Performance 2.0`.

---

## v0.28.2 — Timed Performance 2.0

O Athlete Performance Passport passa a reconhecer **tempo real de sessões concluídas** como uma dimensão própria, sem confundir duração com qualidade de performance.

### Entregue
- duração registrada da execução como fonte preferencial;
- fallback calculado por `DataHoraInicioUtc` × `DataHoraFimUtc` quando a duração não foi persistida;
- agrupamento somente pela mesma `SessaoTreino`;
- duração mais recente, menor, maior e média;
- quantidade de registros comparáveis;
- origem explícita da duração mais recente;
- endpoint profissional e endpoint do paciente;
- coleção `Tempos` adicionada ao Performance Passport.

### Guardrail
Timed Performance 2.0 **não considera automaticamente a menor duração como melhor performance, maior intensidade, melhor condicionamento, pace ou resultado de prova**. A camada descreve tempo real dentro da mesma sessão.

**Próxima etapa:** `v0.28.3 — Competition & Test Results 2.0`.

---

## v0.28.3 — Competition & Test Results 2.0

O Athlete Performance Passport passa a reconhecer **provas/competições** e **testes/avaliações** a partir de registros supervisionados já existentes em `EventosProgressaoSupervisionada`.

### Entregue
- classificação `ProvaCompeticao` apenas quando eixo/descrição traz indicação explícita de prova, competição, campeonato, torneio ou corrida;
- classificação `TesteAvaliacao` apenas quando há indicação explícita de teste, avaliação, benchmark ou protocolo;
- data, status, eixo, descrição, observações e ciclo esportivo vinculável;
- endpoint profissional e endpoint do paciente;
- coleção `Resultados` no passaporte;
- domínios `provas` e `testes` deixam de ser permanentemente `SemFonteEstruturada` quando existe base supervisionada compatível.

### Guardrail
Competition & Test Results 2.0 **não inventa colocação, tempo, distância, nota, aprovação, recorde ou melhora** quando esses dados não existem de forma estruturada. Um registro supervisionado é contexto, não um resultado numérico presumido.

**Próxima etapa:** `v0.28.4 — Skills & Milestones 2.0`.

---

## v0.28.4 — Skills & Milestones 2.0

O Athlete Performance Passport passa a reconhecer **habilidades** e **marcos** somente quando eles já existem como registros supervisionados explicitamente documentados.

### Entregue
- `HabilidadeRegistrada` para eventos que citam explicitamente habilidade, técnica, competência, skill ou fundamento;
- `MarcoRegistrado` para eventos que citam explicitamente marco, milestone, conquista, meta/objetivo atingido ou recorde pessoal;
- data, status, eixo, descrição, observações e ciclo esportivo;
- endpoint profissional e endpoint do paciente;
- coleção `HabilidadesMarcos` no passaporte;
- atualização dos domínios `habilidades` e `marcos`.

### Guardrail
Skills & Milestones 2.0 **não certifica domínio técnico, não cria conquista automática e não converte recordes, cargas, tempos, volume ou frequência em habilidade ou marco**. O sistema apenas organiza o que foi explicitamente registrado em contexto supervisionado.

**Próxima etapa:** `v0.28.5 — Performance Evolution 2.0`.

---

## v0.28.5 — Performance Evolution 2.0

O Athlete Performance Passport passa a apresentar **início × atual** somente quando há pelo menos dois registros comparáveis dentro da mesma referência.

### Entregue
- evolução de carga dentro do mesmo exercício e unidade;
- evolução temporal dentro da mesma sessão;
- valor inicial, valor atual, variação absoluta e percentual;
- datas inicial e atual;
- quantidade de registros comparáveis;
- resumo das demais dimensões do passaporte;
- endpoint profissional e endpoint do paciente;
- coleção `Evolucao` integrada ao passaporte.

### Guardrail
Performance Evolution 2.0 **não gera score, ranking, prognóstico, tendência clínica ou recomendação automática**. Variação de carga não equivale a força máxima; variação de duração não equivale automaticamente a melhora ou piora de performance.

**Próxima fase:** `v0.29.0 — Progress Intelligence Foundation`.

---

## v0.29.0 — Progress Intelligence Foundation

Inicia a nova camada de inteligência longitudinal sobre o Athlete Performance Passport.

### Entregue
- sinais descritivos derivados apenas de comparações já existentes;
- direção textual `AcimaDoInicial`, `AbaixoDoInicial` ou `EstavelNoPeriodo`;
- carga e tempo permanecem domínios separados;
- evidência e limite interpretativo por sinal;
- quantidade de registros comparáveis;
- resumo de registros supervisionados;
- endpoints profissional e paciente;
- integração ao passaporte e ao workspace profissional.

### Guardrail
Progress Intelligence Foundation **não gera score, ranking, diagnóstico, prognóstico, recomendação automática ou julgamento clínico**. A direção apenas descreve a relação matemática entre o primeiro e o registro atual dentro de uma base comparável.

**Próxima etapa:** `v0.29.1 — Progress Signal Context 2.0`.

---

## v0.29.1 — Progress Signal Context 2.0

Expande a Progress Intelligence Foundation com contexto observacional para cada sinal.

### Entregue
- recência: `Recente`, `Intermediaria` ou `Antiga`;
- dias desde o último registro;
- dias cobertos pela comparação;
- quantidade de registros comparáveis;
- densidade observacional: `BaseMinima`, `BaseCurta` ou `BaseMaisDensa`;
- origem da evidência;
- contexto de leitura textual por sinal;
- endpoints profissional e paciente;
- integração ao Athlete Performance Passport e ao workspace profissional.

### Guardrail
Recência, cobertura e densidade descrevem **disponibilidade de dados**. Não representam qualidade do atleta, confiança clínica, força da evidência científica, certeza, score, ranking, diagnóstico, prognóstico ou recomendação automática.

**Próxima etapa:** `v0.29.2 — Multi-Signal Timeline 2.0`.

---

## v0.29.2 — Multi-Signal Timeline 2.0

Organiza os pontos observados de performance numa única linha do tempo cronológica.

### Entregue
- eventos `InicioComparavel` e `RegistroAtual`;
- domínio, referência, medida, valor e unidade;
- data/hora do registro;
- recência herdada do contexto;
- origem da evidência;
- quantidade de registros comparáveis;
- ordenação cronológica entre carga e tempo;
- endpoints profissional e paciente;
- integração ao Athlete Performance Passport e workspace profissional.

### Guardrail
A timeline usa **somente pontos observados**. Não cria pontos intermediários, não interpola dados, não projeta tendência e não interpreta proximidade temporal como relação causal.

**Próxima etapa:** `v0.29.3 — Progress Evidence Windows 2.0`.

---

## v0.29.3 — Progress Evidence Windows 2.0

Organiza os eventos observados em janelas temporais explícitas.

### Entregue
- janela `Recente30d`;
- janela `Intermediaria90d`;
- janela `Ampla180d`;
- período inicial/final de cada janela;
- total de eventos observados;
- separação entre eventos de carga e tempo;
- quantidade de referências distintas;
- cobertura descritiva: `SemEventosObservados`, `EventoIsolado`, `CoberturaCurta` ou `CoberturaMaisAmpla`;
- endpoints profissional e paciente;
- integração ao Athlete Performance Passport.

### Guardrail
Mais registros representam **maior cobertura documental**, não maior confiança clínica, melhor desempenho, prognóstico ou recomendação automática.

**Próxima etapa:** `v0.29.4 — Cross-Signal Observation Map 2.0`.

---

## v0.29.4 — Cross-Signal Observation Map 2.0

Adiciona um mapa diário de coobservação entre os sinais já existentes.

### Entregue
- agrupamento por data observada;
- quantidade total de eventos no dia;
- eventos de carga e tempo separados;
- quantidade de referências distintas;
- lista de domínios observados;
- lista de referências observadas;
- classificação `CoobservacaoNoMesmoDia` ou `ObservacaoIsoladaNoDia`;
- endpoints profissional e paciente;
- integração ao Athlete Performance Passport.

### Guardrail
Coobservação no mesmo dia significa apenas **coincidência documental temporal**. Não representa correlação, causalidade, influência, resposta fisiológica, tendência ou efeito de uma variável sobre outra.

**Próxima etapa:** `v0.29.5 — Progress Observation Summary 2.0`.

---

## v0.29.5 — Progress Observation Summary 2.0

Consolida as camadas observacionais anteriores em um resumo único.

### Entregue
- total de sinais descritivos;
- total de contextos disponíveis;
- total de eventos da timeline;
- total de janelas temporais;
- total de dias observados no mapa;
- dias com múltiplos domínios;
- dias com múltiplas referências;
- referências distintas na timeline;
- cobertura geral: `SemCoberturaObservacional`, `CoberturaMinima`, `CoberturaParcial` ou `CoberturaMaisAmpla`;
- endpoints profissional e paciente;
- integração ao Athlete Performance Passport.

### Guardrail
Cobertura mais ampla significa apenas **mais dados observados disponíveis**. Não significa melhor desempenho, maior confiança clínica, maior certeza, prognóstico favorável ou necessidade de intervenção.

**Próxima etapa:** `v0.29.6 — Progress Intelligence Closure 2.0`.

---

## v0.29.6 — Progress Intelligence Closure 2.0

Fecha formalmente a fundação observacional construída na linha 0.29.x.

### Entregue
- fechamento estrutural das 6 camadas de inteligência de progresso;
- contagem de componentes esperados e disponíveis;
- lista de componentes presentes;
- lista de componentes ausentes;
- estado `EstruturaObservacionalCompleta` ou `EstruturaObservacionalParcial`;
- endpoints profissional e paciente;
- integração ao Athlete Performance Passport.

### Guardrail
`EstruturaObservacionalCompleta` significa apenas que os componentes técnicos previstos estão presentes. Não significa melhor desempenho, maior qualidade clínica, maior certeza, prognóstico favorável ou recomendação automática.

**Próxima etapa:** `v0.30.0 — próxima fase funcional do ROADMAP`.

---

## v0.30.0 — Progress Review Workspace Foundation

Abre a linha 0.30.x transformando a fundação observacional em um workspace único de revisão.

### Entregue
- `ProgressReviewWorkspaceResponse`;
- seis seções de revisão;
- estado de preparação estrutural;
- contagem de seções disponíveis;
- foundation, contexto, timeline, janelas, mapa e summary organizados em uma visão;
- endpoints profissional e paciente;
- integração ao Athlete Performance Passport.

### Guardrail
`PreparacaoEstruturalCompleta` significa apenas que os componentes necessários para a revisão estão disponíveis. Não significa decisão clínica pronta, diagnóstico, prognóstico, recomendação ou maior certeza.

**Próxima etapa:** `v0.30.1 — Progress Review Notes Foundation`.

---

## v0.30.1 — Progress Review Notes Foundation

Adiciona a estrutura-base de observações profissionais vinculadas ao workspace de revisão.

### Campos estruturados
- `dado-observado`;
- `interpretacao-profissional`;
- `ponto-atencao`;
- `hipotese-acompanhamento`;
- `proximo-item-revisar`.

### Entregue
- contrato explícito de campo de nota;
- fundação integrada ao Athlete Performance Passport;
- endpoint profissional;
- endpoint do portal do paciente em modo somente leitura;
- UI de estrutura de notas;
- separação explícita entre observação e interpretação;
- `PersistenciaDisponivel = false` nesta fundação.

### Guardrail
A estrutura não transforma observação profissional em diagnóstico, prognóstico, prescrição ou recomendação automática. Persistência própria será introduzida somente após estabilização do modelo.

**Próxima etapa:** `v0.30.2 — Progress Review Notes Persistence`.

---

## v0.30.2 — Progress Review Notes Persistence

Habilita persistência real das notas estruturadas de revisão de progresso reutilizando a infraestrutura consolidada de `NotaInternaProfissional`.

### Persistência
- vínculo com organização e paciente;
- autor (`AutorUsuarioId` e `AutorNome`);
- `CreatedAtUtc` e `UpdatedAtUtc`;
- arquivamento lógico;
- auditoria de criação, edição e arquivamento;
- conteúdo privado da equipe profissional.

### Campos estruturados
As notas continuam limitadas aos cinco campos definidos na v0.30.1:
- `dado-observado`;
- `interpretacao-profissional`;
- `ponto-atencao`;
- `hipotese-acompanhamento`;
- `proximo-item-revisar`.

### API profissional
- `GET /api/pacientes/{pacienteId}/performance/progress-review-notes`;
- `POST /api/pacientes/{pacienteId}/performance/progress-review-notes`;
- `PUT /api/pacientes/{pacienteId}/performance/progress-review-notes/{id}`;
- `DELETE /api/pacientes/{pacienteId}/performance/progress-review-notes/{id}`.

### Arquitetura
Nenhuma tabela duplicada foi criada. A feature reutiliza `NotasInternasProfissionais` e `AuditLogs`, mantendo compatibilidade com o módulo de notas internas existente.

**Próxima etapa:** `v0.30.3 — Progress Review History & Filters`.

---

## v0.30.3 — Progress Review History & Filters

Adiciona histórico filtrável das notas profissionais de revisão de progresso.

### Filtros
- campo estruturado;
- autor;
- período inicial/final;
- incluir ou ocultar arquivadas;
- ordenação ascendente ou descendente.

### API
`GET /api/pacientes/{pacienteId}/performance/progress-review-notes/history`

Parâmetros opcionais:
- `campo`;
- `autorUsuarioId`;
- `deUtc`;
- `ateUtc`;
- `incluirArquivadas`;
- `ordenacao=asc|desc`.

### UI
A modal **Revisão de progresso** agora possui filtros antes da listagem de notas, preservando criação, edição e arquivamento.

### Privacidade
Histórico e filtros continuam restritos à equipe profissional. O portal do paciente não recebe endpoint de histórico dessas notas.

**Próxima etapa:** `v0.30.4 — Progress Review Context Links`.

---

## v0.30.4 — Progress Review Context Links

Permite vincular cada nota profissional de revisão ao contexto observacional que motivou o registro.

### Contextos suportados
- `foundation`;
- `context`;
- `timeline`;
- `window`;
- `observation-map`;
- `summary`.

### Persistência
O vínculo é opcional e reutiliza a própria `Categoria` de `NotaInternaProfissional`, com namespace controlado `ProgressReview:`. Nenhuma tabela ou migration adicional foi necessária.

### Regras
- nota pode permanecer sem contexto;
- contexto exige tipo e referência;
- referência é limitada a 120 caracteres;
- filtros por campo continuam funcionando com notas antigas e contextualizadas;
- a UI permite selecionar tipo e informar referência;
- o histórico mostra o contexto salvo.

### Guardrail
O vínculo contextual é apenas uma referência documental. Não duplica o conteúdo clínico, não cria causalidade, não confirma hipótese e não transforma o contexto em conclusão profissional automática.

**Próxima etapa:** `v0.30.5 — Progress Review Context Navigation`.

---

## v0.30.5 — Progress Review Context Navigation

Transforma os vínculos contextuais das notas profissionais em navegação direta para a seção observacional correspondente no workspace.

### Mapeamento
- `foundation` → Progress Intelligence Foundation;
- `context` → Progress Signal Context;
- `timeline` → Multi-Signal Timeline;
- `window` → Progress Evidence Windows;
- `observation-map` → Cross-Signal Observation Map;
- `summary` → Progress Observation Summary.

### Entregue
- `NavegacaoDestino` nas notas persistidas;
- endpoint `context-navigation/{tipo}`;
- botão **Abrir contexto** nas notas contextualizadas;
- scroll suave até a seção observacional;
- destaque temporário da seção alvo;
- fallback quando a seção não estiver disponível.

### Guardrail
A navegação apenas abre a seção relacionada. Ela não interpreta `ContextoReferencia`, não seleciona automaticamente um achado clínico e não transforma a referência documental em conclusão.

**Próxima etapa:** `v0.30.6 — Progress Review Context Focus`.

---

## v0.30.6 — Progress Review Context Focus

Evolui a navegação contextual para focar um item observacional específico somente quando a referência registrada na nota possui correspondência única e inequívoca dentro da seção.

### Estratégia
- itens observacionais recebem `data-progress-context-ref`;
- a referência da nota é normalizada para comparação;
- referências no formato `dominio::referencia` também consideram a parte após `::`;
- somente uma correspondência exata única recebe foco;
- zero correspondências ou múltiplas correspondências mantêm fallback para a seção inteira.

### Contextos focáveis
- Foundation: `referencia`;
- Context: `referencia`;
- Timeline: `referencia`;
- Evidence Window: `janela`;
- Observation Map: `data`;
- Summary: referência estável `summary`.

### UX
O item localizado recebe scroll centralizado e destaque temporário. Em caso ambíguo, o sistema informa que não houve correspondência única e abre apenas a seção.

### Guardrail
O sistema não usa similaridade semântica, fuzzy match ou inferência para escolher registros. Foco específico só ocorre diante de correspondência textual normalizada única.

**Próxima etapa:** `v0.30.7 — Progress Review Context Capture`.

---

## v0.30.7 — Progress Review Context Capture

Permite iniciar uma nova nota profissional diretamente a partir de um item observacional já exibido no workspace.

### Fluxo
1. o item observacional expõe `data-progress-context-type` e `data-progress-context-ref`;
2. aparece a ação **Criar nota deste contexto**;
3. a modal Progress Review Notes é aberta;
4. `ContextoTipo` e `ContextoReferencia` são preenchidos automaticamente;
5. o profissional escolhe o campo estruturado e escreve a observação.

### Contextos
- `foundation`;
- `context`;
- `timeline`;
- `window`;
- `observation-map`;
- `summary`.

### Guardrail
A captura copia somente os identificadores técnicos de contexto. Não copia texto clínico, não cria interpretação, não escolhe campo da nota e não salva automaticamente.

**Próxima etapa:** `v0.30.8 — Progress Review Context Capture Confirmation`.

---

## v0.30.8 — Progress Review Context Capture Confirmation

Adiciona uma etapa visual explícita de confirmação do vínculo contextual antes do salvamento da nota.

### Confirmação
Após capturar ou selecionar um contexto, a modal exibe:
- origem do vínculo: capturado ou selecionado manualmente;
- tipo de contexto;
- referência contextual;
- aviso de que a persistência ocorrerá apenas no salvamento.

### Ações
- **Trocar contexto**: move o foco para o seletor e passa o vínculo para edição manual;
- **Remover vínculo**: limpa tipo e referência;
- alterações manuais atualizam a confirmação em tempo real;
- ao limpar o formulário, o estado de confirmação também é reiniciado.

### Guardrail
Capturar um contexto não significa confirmar seu conteúdo clínico. O sistema não salva automaticamente, não escolhe o campo estruturado e não redige a observação profissional.

**Próxima etapa:** `v0.30.9 — Progress Review Context Integrity`.

---

## v0.30.9 — Progress Review Context Integrity

Adiciona validação estrutural explícita para o vínculo contextual antes do envio da nota.

### Regras
- tipo e referência vazios: válido, nota sem vínculo contextual;
- tipo sem referência: inválido;
- referência sem tipo: inválido;
- tipo deve pertencer aos seis contextos suportados;
- referência limitada a 120 caracteres;
- `|` é reservado pelo namespace interno e não pode fazer parte da referência.

### Dupla validação
- frontend bloqueia o submit e mostra feedback local;
- backend expõe `context-integrity` e reutiliza a mesma regra estrutural antes de persistir;
- o backend permanece como autoridade final.

### Guardrail
A integridade valida apenas forma e coerência estrutural. Não determina significado clínico, causalidade, relevância, diagnóstico ou pertinência da observação profissional.

**Próxima etapa:** `v0.30.10 — Progress Review Context Integrity UX`.

---

## v0.30.10 — Progress Review Context Integrity UX

Melhora a experiência de correção dos vínculos contextuais inválidos no formulário de notas profissionais.

### UX
- campos inválidos recebem `aria-invalid`;
- campo responsável recebe marcador técnico `data-progress-context-field-invalid-v03010`;
- botão **Corrigir vínculo** move o foco para o campo responsável;
- submit fica desabilitado quando a estrutura contextual está inválida;
- estado `Pronto para salvar` aparece quando o vínculo está válido;
- ausência completa de contexto continua sendo uma condição válida.

### Comportamento
O estado visual acompanha alterações de tipo e referência em tempo real. Quando o vínculo volta a ser válido, os marcadores de erro são removidos e o botão de salvar é reabilitado.

### Guardrail
A UX apenas orienta correção estrutural. Não sugere qual contexto usar, não interpreta referência, não modifica a observação e não toma decisão clínica.

**Próxima etapa:** `v0.30.11 — Progress Review Context Integrity Accessibility`.

