# AESYN Performance — LISTA 05 / Série v0.60.x

> Base: **v0.59.11 — Lista 04 concluída e implantada**
>
> Série: **v0.60.x — Mobile Reliability, Athlete Experience & Professional Planning**
>
> Regra: a Lista 05 só fecha quando os requisitos estiverem implementados ou consolidados em solução superior, com regressão automatizada quando viável, tema claro/escuro, mobile 360/390/430 e desktop nas superfícies compartilhadas.

## Escopo consolidado — 16 requisitos

### Correções
1. lupa da Home visível no tema claro e validada em mobile/tablet/desktop;
2. eliminar falso “sessão expirada” ao entrar/retomar pelo PWA;
3. recuperar a página Arquivos no mobile;
4. transformar criação de dieta profissional em ferramenta real de cálculo de energia/nutrientes usando dados corporais e metas;
5. busca/lupa mobile com UX própria, sem depender de `Esc`/atalhos desktop.

### Melhorias
6. Chat global profissional acessível pela sidebar para todos os pacientes autorizados;
7. shell PWA estável, sem deslocamento horizontal, com rolagem vertical natural;
8. interesses esportivos mais descobríveis, com sugestões/aba dedicada;
9. antes de iniciar treino, permitir abrir e escolher claramente qual treino será executado;
10. melhorar contraste/legibilidade no registro da execução do treino;
11. compactar e reorganizar a página Plano, reduzindo rolagem e aproveitando largura;
12. identidade de demonstração/chat: “Dr Raphael” no lugar de “Dr Testinho”;
13. substituir `•••` por Perfil/Mais e separar funções secundárias das mais usadas;
14. usar `X` para fechar/sair quando a ação não for navegação hierárquica;
15. botão `+` em séries para blocos/subséries com prescrições distintas de repetições;
16. Nutrição profissional com **“Montar dieta modelo”** e **“Nova dieta”**, biblioteca reutilizável, duplicação/adaptação para paciente e integração ao Nutrition Planning Engine.

## Ordem funcional

### ✅ v0.60.0 — PWA Session & Mobile Search Reliability — IMPLEMENTADA
- renovação de sessão é tentada antes de expulsar o usuário após 401;
- falha transitória de renovação não destrói a sessão local imediatamente;
- boot do PWA valida/renova sessão próxima do vencimento antes da jornada;
- remoção do logout duplicado no carregamento inicial do portal do paciente;
- Service Worker passa a usar cache `aesyn-static-v0.60.0` e assets `app.css/app.js` da versão corrente;
- busca mobile ganha `X` explícito, input mobile, altura baseada em `visualViewport`, safe areas e resultados roláveis;
- `Esc` continua apenas como conveniência desktop;
- lupa ganha contraste explícito em tema claro;
- shell bloqueia deslocamento horizontal global sem remover scroll interno legítimo;
- gates da regressão adicionados a PREPARAR/TESTAR.
**Próxima etapa:** `v0.60.1 — Patient Files Mobile Recovery`.

### v0.60.1 — Patient Files Mobile Recovery
Corrigir a jornada Arquivos no paciente em 360/390/430 px: listagem, upload, câmera/galeria, preview/download, erro/retry, permissões e teclado/modal.

### v0.60.2 — Mobile Navigation & Profile/More 2.0
Perfil/Mais limpo no topo, funções secundárias separadas, sem `•••`; X para fechar/sair e seta apenas para voltar hierarquicamente.

### v0.60.3 — Athlete Interests Discovery 2.0
Interesses esportivos descobríveis por aba/sugestões, sem depender de conteúdo no fim da página.

### v0.60.4 — Workout Selection Before Start
Abrir detalhes e selecionar treino A/B/C/etc. antes de iniciar; CTA só inicia o treino escolhido.

### v0.60.5 — Workout Execution Readability
Contraste, hierarquia, campos e feedback de execução mais legíveis em claro/escuro e mobile.

### v0.60.6 — Workout Series Blocks / Sub-series Builder
`+` para blocos/subséries com repetições diferentes, preservando ordem, técnicas avançadas e leitura do paciente.

### v0.60.7 — Patient Plan Compact Experience
Plano mais compacto, texto centralizado nos destaques, melhor uso da largura e menos rolagem sem esconder informação.

### v0.60.8 — Professional Chat Identity & Global Inbox Consolidation
Consolidar Chat da sidebar para todos os pacientes autorizados; identidade de demo “Dr Raphael”; badges, busca e fila de resposta coerentes.

### v0.60.9 — Professional Nutrition Planning Engine 3.0 + Diet Model Builder
Peso, altura e dados corporais; TMB/GET; objetivo; alvo energético; macros; kcal; totais por refeição/dia; meta × prescrito × restante/excedido; controle manual profissional.
Adicionar entradas **Montar dieta modelo** e **Nova dieta**:
- modelos sem paciente;
- criar do zero;
- duplicar, versionar e adaptar;
- aplicar ao paciente sem alterar o modelo original;
- transformar dieta existente em modelo;
- alimentos, porções, refeições e suplementos reutilizáveis;
- recálculo ao vivo durante adaptação.

### v0.60.10 — Lista 05 Integrated Quality Pass & Closure
Passagem integrada real das jornadas tocadas, regressões, claro/escuro, 360/390/430/desktop, acessibilidade, empty/loading/error/retry e fechamento explícito da Lista 05.
Não criar cadeia artificial de “closure/handoff/summary” depois desse ponto.
