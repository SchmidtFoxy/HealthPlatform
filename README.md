# v0.3.41-r1 — Correção do teste do POPULAR remoto

Patch apenas da suíte de testes.

A v0.3.41 atualizou corretamente o banner de `POPULAR-REMOTO.ps1` para:

`HealthPlatform v0.3.41 - POPULAR RENDER DEMO`

mas o teste `[486/508]` ainda procurava a string histórica da v0.3.40.

Isso fazia a suíte interromper em 485/508 mesmo com API, banco, SPA, assets, Docker, Blueprint, bootstrap e healthcheck já validados.

A r1 atualiza somente essa expectativa estática.

Não há alteração de:
- banco/schema;
- API;
- regras de negócio;
- identidade RS;
- layout desktop/iPad/iPhone;
- scripts de deploy;
- população remota.

`VERSION.txt` permanece `0.3.41`.
`PREPARAR.ps1` permanece 30/30.
`TESTAR.ps1` permanece 508/508.


# v0.3.41 — RS Visual Identity + Mobile/Tablet UX

A v0.3.41 aplica ao MVP a identidade visual RS enviada como referência e melhora de forma ampla o uso em iPad e iPhone.

## Identidade

A interface passa a usar:
- azul-marinho profundo como cor institucional;
- marfim/branco como base;
- dourado em linhas, estados ativos e detalhes;
- monograma `RS`;
- tipografia display condensada para títulos;
- linguagem visual editorial, limpa e ligada a ciência/performance.

O produto continua se chamando HealthPlatform; `RS` funciona como assinatura visual da demonstração.

## Login

O login foi redesenhado para lembrar a linguagem dos materiais:
- fundo marfim;
- faixa vertical navy;
- linha dourada;
- formas abstratas discretas;
- títulos altos/condensados;
- copy `Ciência. Estratégia. Resultado.`.

Nenhuma imagem externa ou fonte remota é necessária.

## Profissional

- sidebar navy com seleção branca e marcador dourado;
- dashboard e cards com aparência mais editorial;
- títulos e números com hierarquia mais forte;
- prontuário com abas mais limpas;
- formulários e estados ativos padronizados.

## iPad

Entre 821 e 1180 px:
- sidebar compacta;
- conteúdo com margens menores;
- grids clínicos deixam de ficar espremidos;
- modais grandes respeitam a viewport;
- builders de treino ficam mais utilizáveis.

Em iPad retrato a navegação passa para drawer.

## iPhone

- safe areas para notch/home indicator;
- `100dvh`;
- menu profissional em drawer com backdrop;
- touch targets de pelo menos 44 px;
- modais viram bottom sheets;
- abas do prontuário ficam sticky e roláveis;
- portal do paciente ganha header compacto;
- navegação do portal usa scroll horizontal com snap;
- grids analíticos de treino são reorganizados para leitura vertical.

## Banco/API

Nenhuma alteração de schema ou regra de negócio.

Os hotfixes do Render e o script `POPULAR-REMOTO-RICO.ps1` são preservados.

`PREPARAR.ps1`: **30 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **508 etapas**.
`TESTAR-RENDER.ps1`: **12 etapas**.


# v0.3.40-r3 — Sincronização da senha do admin no Render

Correção do login profissional no ambiente demo.

## Causa

O `DbSeeder` só define a senha no momento em que cria o usuário.

Se `admin@healthplatform.local` já existia, alterar `Seed__AdminPassword` no Render não modificava a senha armazenada no ASP.NET Core Identity.

Isso fazia o healthcheck, banco, SPA e assets funcionarem normalmente, mas o login retornar HTTP 401.

## Correção

No modo `DemoBootstrap`, quando:

`DemoBootstrap__SyncAdminPassword=true`

a inicialização agora:

1. executa o seed normal;
2. localiza o admin configurado em `Seed__AdminEmail`;
3. verifica a senha atual com `CheckPasswordAsync`;
4. se ela for diferente de `Seed__AdminPassword`, gera um token interno de reset;
5. aplica `ResetPasswordAsync`.

Nenhum paciente, consulta, plano ou outro registro é apagado.

O `render.yaml` já habilita:

`DemoBootstrap__SyncAdminPassword=true`

Portanto, basta definir a senha desejada em `Seed__AdminPassword` e fazer um novo deploy.

## Versão

- `VERSION.txt`: continua `0.3.40`
- `PREPARAR.ps1`: 30/30
- `TESTAR.ps1`: 492/492
- schema novo: não


# v0.3.40-r2 — Login Render + Demo Rica

A r2 corrige uma confusão importante da tela de login no ambiente hospedado: o HTML ainda preenchia `ChangeMe_123!`, embora no Render a senha correta seja `Seed__AdminPassword`.

Agora:
- a senha não vem preenchida;
- erro de autenticação aparece dentro do formulário;
- 401 no endpoint de login não é tratado como "sessão expirada";
- a interface orienta a usar a senha configurada na demo.

Também foi criado `POPULAR-REMOTO-RICO.ps1`.

Ele mantém poucos pacientes fictícios, mas aprofunda cada prontuário com:
- consultas e evoluções SOAP;
- evolução de hábitos/anamneses;
- avaliações corporais adicionais;
- painel laboratorial ampliado;
- planos alimentares completos com cinco refeições;
- fases nutricionais e de treino;
- check-ins longitudinais;
- histórico de execuções para alimentar progressão de carga;
- cenários contrastantes de boa resposta, baixa adesão, alterações laboratoriais e performance.

Acesso de paciente preparado pelo seed rico:
- `ana.ribeiro.demo@healthplatform.local`
- `diego.alves.demo@healthplatform.local`
- senha: `PacienteDemo_123!`

Não há alteração de banco/API de domínio.

`VERSION.txt` permanece `0.3.40`.
`PREPARAR.ps1` permanece 30/30.
`TESTAR.ps1` permanece 492/492.


# v0.3.40-r1 — Render / inotify hotfix

Correção específica do deploy no Render.

O container gratuito retornou:

`System.IO.IOException: The configured user limit (128) on the number of inotify instances has been reached`

A falha acontecia dentro de `WebApplication.CreateBuilder(args)`, antes da inicialização normal da aplicação, porque o host do .NET habilita por padrão o reload de `appsettings.json` e usa `FileSystemWatcher`.

A r1 agora inicia a aplicação com:

`--hostBuilder:reloadConfigOnChange=false`

Isso impede a criação desse watcher para os arquivos de configuração no ambiente hospedado.

Também foi adicionado ao `render.yaml`:

`DOTNET_USE_POLLING_FILE_WATCHER=1`

como proteção adicional para providers físicos que eventualmente precisem monitorar arquivos no container.

Não há alteração de banco, API ou regra de negócio.

- `VERSION.txt`: continua `0.3.40`
- `PREPARAR.ps1`: continua 30/30
- `TESTAR.ps1`: continua 492/492
- schema novo: não


# v0.3.40 — Render Demo Deploy

A v0.3.40 empacota o MVP Preview validado para uma hospedagem temporária de demonstração no Render.

## Arquivos novos

- `Dockerfile`
- `.dockerignore`
- `docker-entrypoint.sh`
- `render.yaml`
- `DEPLOY-RENDER-MVP.md`
- `POPULAR-REMOTO.ps1`
- `TESTAR-RENDER.ps1`

## Render

O container publica a API em `0.0.0.0:$PORT`.

O Blueprint cria:
- Web Service Docker;
- PostgreSQL;
- health check `/api/health`;
- JWT secret gerado pelo Render;
- senha do admin solicitada no primeiro Blueprint sync.

## Banco demo

O fluxo local permanece intacto e continua usando a migration baseline estável e os upgrades históricos.

Somente quando `DemoBootstrap__Enabled=true`, a aplicação hospedada usa `EnsureCreatedAsync()` para inicializar um **banco novo e vazio** com o schema atual e então criar organização/roles/admin.

Isso é deliberadamente uma solução de MVP e não substitui a futura estratégia de migrations de produção.

## Demo remota

Depois do primeiro deploy:

`POPULAR-REMOTO.ps1` popula o Render com dados fictícios, catálogos básicos e um acesso de paciente.

`TESTAR-RENDER.ps1` executa 12 verificações remotas somente leitura.

## Banco

Não há upgrade SQL local nesta versão.

`PREPARAR.ps1`: **30 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **492 etapas**.
`TESTAR-RENDER.ps1`: **12 etapas**.


# v0.3.39 — MVP Preview + Polimento de Demonstração

A v0.3.39 congela novas funcionalidades grandes e prepara a experiência para uma rodada real de avaliação do protótipo.

## Objetivo

Esta versão é feita para ser entregue a um usuário de teste e responder principalmente:
- o que está faltando;
- o que está confuso;
- o que demora demais;
- o que quebra;
- o que existe, mas está no lugar errado.

## Experiência de demonstração

A interface agora identifica claramente o ambiente como **MVP Preview / Demo** e orienta o uso somente com dados fictícios.

O dashboard profissional ganhou uma apresentação mais clara do objetivo da versão e atalhos para:
- novo paciente;
- agenda;
- roteiro da demo.

## Roteiro interno

O botão **Roteiro da demo** abre um guia curto com os principais fluxos que vale explorar:
- cadastro/prontuário;
- consulta;
- alimentação;
- treino;
- acompanhamento;
- portal do paciente.

O guia também oferece um modelo copiável de feedback com:
- FALTOU;
- CONFUNDIU;
- DEMOROU;
- QUEBROU/BUG;
- SUGESTÃO;
- PRIORIDADE.

## Pequenos ajustes de UX

- `Esc` fecha modais, drawer ou menu lateral quando possível;
- feedback visual quando a conexão cai ou retorna;
- foco de teclado mais visível;
- estados vazios mais consistentes;
- refinamento de cliques, cards e toasts;
- Swagger removido da navegação normal da demo, sem remover o endpoint.

## Banco

Não há alteração de schema nesta versão.

`PREPARAR.ps1`: **30 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **476 etapas**.


# v0.3.38-r1 — Correção de compilação da análise de progresso

Patch de compilação da v0.3.38.

`AnalisarSerie(...)` retornava `object`, mas o pipeline LINQ precisava acessar propriedades como `revisaoSugerida`, `status` e `exercicio`. Em C#, essas propriedades não ficam disponíveis estaticamente quando o retorno é declarado como `object`.

A r1 substitui os objetos anônimos por records internos fortemente tipados:
- `AnaliseExercicioResponse`;
- `PontoAnaliseExercicio`.

O contrato JSON, as regras de estagnação/progressão/carga+RPE e a interface permanecem iguais.

Não há alteração de banco ou schema.
`VERSION.txt` permanece `0.3.38`.
`PREPARAR.ps1` permanece 30/30.
`TESTAR.ps1` permanece 462/462.


# v0.3.38 — Estagnação + Fadiga + Sinais de Progressão

A v0.3.38 transforma a série histórica de cargas da v0.3.37 em sinais objetivos de acompanhamento esportivo.

## Endpoints

Profissional:

`GET /api/pacientes/{pacienteId}/treinos/analise-progresso?dias=120`

Paciente:

`GET /api/portal/me/treinos/analise-progresso?dias=120`

## Estados

Cada exercício + unidade pode aparecer como:
- **Progredindo**;
- **Estagnação**;
- **Revisão por carga/RPE**;
- **Estável**;
- **Sem base**.

## Estagnação

A heurística exige:
- pelo menos 5 registros;
- variação da carga média recente dentro de ±2% da base anterior;
- ausência de novo recorde na janela recente.

## Revisão por carga/RPE

O sistema sinaliza revisão quando encontra:
- queda da carga média recente de pelo menos 3%;
- RPE médio recente de 8/10 ou mais;
- base histórica suficiente.

Esse estado não é diagnóstico de fadiga, lesão ou overtraining.

## Progressão

Um exercício é marcado como `Progredindo` quando:
- houve novo recorde na janela recente; ou
- a carga média recente ficou mais de 2% acima da base anterior.

## Métricas

O retorno inclui:
- média de carga anterior;
- média de carga recente;
- variação percentual;
- RPE médio recente;
- melhor carga;
- dias sem novo recorde;
- última carga;
- sinais explicativos;
- série temporal.

## Interface

O painel aparece em:
- Treinos;
- Resumo do prontuário;
- Meu treino no portal do paciente.

Os exercícios com estagnação ou revisão por carga/RPE aparecem primeiro e recebem indicação de **revisão sugerida**, nunca ajuste automático.

## Banco

Não há alteração de schema nesta versão.

`PREPARAR.ps1`: **30 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **462 etapas**.


# v0.3.37 — Progressão por Exercício + Recordes de Carga

A v0.3.37 aprofunda a leitura esportiva da v0.3.36 olhando a evolução de cada exercício ao longo das execuções.

## Endpoints

Profissional:

`GET /api/pacientes/{pacienteId}/treinos/progressao-exercicios?dias=180`

Paciente:

`GET /api/portal/me/treinos/progressao-exercicios?dias=180`

O período pode variar de 14 a 730 dias.

## Métricas por exercício

Para cada exercício + unidade de carga, o backend calcula:
- primeira carga registrada;
- última carga;
- melhor carga;
- data da melhor marca;
- delta absoluto;
- variação percentual;
- quantidade de novos recordes sucessivos;
- tendência de carga;
- último número de séries, repetições e RPE;
- série temporal das cargas.

## Unidades

`kg` e `lb` nunca são misturados na mesma série.

Se um mesmo exercício possuir registros em unidades diferentes, cada unidade gera uma série independente.

## Tendência

A tendência pode aparecer como:
- **Acima da base**;
- **Estável**;
- **Abaixo da base**;
- **Sem base**.

Com histórico suficiente, a comparação usa médias recentes; com poucos registros, compara o primeiro e o último ponto.

## Recordes

Um novo recorde é contado quando uma execução supera a maior carga anterior daquele exercício e unidade dentro do período consultado.

O primeiro registro funciona apenas como baseline e não conta como recorde.

## Sem estimativas artificiais

A versão não calcula 1RM estimado por fórmulas e não transforma repetições textuais em tonelagem.

Assim, prescrições como `8-12`, `até a falha` ou textos livres não produzem métricas matemáticas fictícias.

## Interface

No prontuário profissional, o painel aparece em:
- Treinos;
- Resumo.

No portal do paciente, aparece em:
- Meu treino.

A interface inclui:
- resumo de exercícios com carga;
- exercícios com base comparativa;
- total de novos recordes;
- maior evolução percentual;
- destaque de exercício com mais recordes;
- gráficos de carga;
- cards individuais com carga inicial, atual e melhor marca.

## Banco

Não há alteração de schema nesta versão.

`PREPARAR.ps1`: **30 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **448 etapas**.


# v0.3.36 — Volume de Treino + Distribuição por Grupo Muscular

A v0.3.36 aprofunda a análise esportiva usando a prescrição de treino e as execuções já existentes.

## Novo endpoint

`GET /api/pacientes/{pacienteId}/treinos/analise-volume?dias=30`

É possível informar também `planoId`.

Sem `planoId`, o backend usa o plano ativo mais recente; se não houver plano ativo, usa o plano mais recente disponível.

## Volume planejado

Por grupo muscular, o sistema calcula:
- séries por ciclo da ficha;
- séries semanais estimadas;
- exercícios distintos;
- participação percentual no volume semanal.

A frequência semanal é inferida a partir de `DiasSemana`, reconhecendo abreviações e nomes dos dias em português.

Quando a frequência não pode ser reconhecida, a sessão usa fallback de 1x/semana e o retorno informa `frequenciaInferida = false`.

## Volume realizado

Para as execuções concluídas do plano no período:
- séries realizadas;
- séries realizadas por grupo muscular;
- média semanal de séries realizadas;
- quantidade de execuções.

## Decisão importante

A versão **não calcula tonelagem fictícia** a partir de prescrições textuais como `8-12`, `até a falha` ou outros formatos livres.

Carga e repetição continuam disponíveis na ficha, mas esta análise usa séries como unidade comparável e confiável.

## Interface

O painel **Volume e distribuição muscular** aparece em:
- Treinos;
- Resumo do prontuário.

Ele mostra:
- total de séries planejadas por semana;
- séries realizadas no período;
- média semanal realizada;
- grupo com maior concentração relativa;
- barras por grupo muscular;
- volume e frequência de cada sessão.

## Banco

Não há alteração de schema nesta versão.

`PREPARAR.ps1`: **30 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **434 etapas**.


# v0.3.35 — Revisão de Fase + Transição Assistida

A v0.3.35 transforma o painel de prontidão da v0.3.34 em um fluxo operacional de decisão.

## Revisão

Uma fase `EmAndamento` pode receber uma revisão com uma das decisões:
- **Manter**: registra a revisão sem alterar a fase;
- **Concluir**: encerra a fase atual;
- **Avançar**: conclui a fase atual e ativa a próxima fase `Planejada` do mesmo ciclo.

Toda revisão exige justificativa.

Quando existem critérios objetivos configurados e ainda pendentes, `Concluir` ou `Avançar` exige confirmação explícita de override profissional.

## Histórico imutável

Cada decisão gera um registro em `RevisoesFases` contendo:
- domínio (Nutrição/Treino);
- fase revisada;
- eventual fase de destino;
- decisão e justificativa;
- status antes/depois;
- quantidade de critérios configurados e atendidos;
- indicação de override;
- snapshot dos indicadores usados na decisão;
- usuário e horário da revisão.

O histórico permanece independente da fase para preservar rastreabilidade mesmo se o planejamento for alterado posteriormente.

## Endpoints

- `GET /api/pacientes/{pacienteId}/revisoes-fases`
- `POST /api/fases-nutricionais/{id}/revisar`
- `POST /api/fases-treino/{id}/revisar`

## Segurança da transição

A mudança de status e o registro da revisão são executados em transação.

O sistema nunca avança automaticamente: a transição depende de uma ação explícita do profissional.

## Banco

Novo upgrade idempotente:

`scripts/sql/v0.3.35_revisoes_transicoes_fases.sql`

`PREPARAR.ps1`: **30 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **420 etapas**.


# v0.3.34 — Metas de Fase + Critérios de Transição

Cada fase nutricional e de treino pode definir meta de peso, adesão mínima, duração mínima e critério profissional de transição.

Novo endpoint: `GET /api/pacientes/{pacienteId}/status-transicao-fases`. Ele cruza as metas com os check-ins e sinaliza quando os critérios objetivos estão prontos para revisão. A conclusão da fase continua sendo decisão do profissional.

Novo SQL: `scripts/sql/v0.3.34_criterios_transicao_fases.sql`.

PREPARAR: 29 etapas. POPULAR: 13 etapas. TESTAR: 406 etapas.


# v0.3.33-r3 — Normalização UTF-8 de todas as leituras do TESTAR

Correção exclusiva do `TESTAR.ps1`.

O teste 93 revelou a segunda metade do mesmo problema de encoding: além dos assets web, diversos testes históricos liam arquivos `.cs`, `.js`, `.css`, `.sql` e scripts locais com `Get-Content` sem `-Encoding UTF8`.

No Windows PowerShell 5.1 isso pode decodificar arquivos UTF-8 usando a página ANSI do Windows e gerar falsos negativos em textos acentuados.

A r3:
- adiciona `-Encoding UTF8` às leituras locais de texto do `TESTAR.ps1`;
- mantém o próprio `TESTAR.ps1` em UTF-8 com BOM;
- torna o teste 93 estrutural, validando `PROF:PENDENCIA`, `PendenciaClinica`, `vencida`, `venceLogo`, severidade alta e janela de 24h;
- preserva as correções da r1 e r2.

Nenhuma API, frontend, banco, schema, SQL ou regra de negócio foi alterada.
`VERSION.txt` permanece `0.3.33`.


# v0.3.33-r2 — Estabilização de encoding dos assets no TESTAR

Correção exclusiva do `TESTAR.ps1`.

O Windows PowerShell 5.1 pode decodificar `app.js`/`app.css` obtidos por `Invoke-WebRequest` com encoding legado quando a resposta HTTP não traz charset explícito. Após a correção UTF-8 BOM da r1, isso expôs falsos negativos em testes históricos com textos acentuados.

A r2:
- continua requisitando `app.js` e `app.css` via HTTP para validar publicação/status 200;
- usa os arquivos locais com `Get-Content -Encoding UTF8` para validar o conteúdo;
- troca o teste 21 de uma copy acentuada para marcadores estruturais do fluxo de relatórios;
- mantém `TESTAR.ps1` em UTF-8 com BOM.

Nenhuma API, frontend, banco, schema, SQL ou regra de negócio foi alterada.
`VERSION.txt` permanece `0.3.33`.


# v0.3.33-r1 — Compatibilidade do TESTAR com Windows PowerShell 5.1

Correção exclusiva do `TESTAR.ps1`.

O teste 384 continha o caractere grego `Δ` em uma string. Em UTF-8 sem BOM, o Windows PowerShell 5.1 pode interpretar um dos bytes desse caractere como uma aspas tipográfica, provocando erro de parser antes da execução da suíte.

Correções:
- a validação passou a usar `mediaAdesaoAlimentacao`, em ASCII;
- `TESTAR.ps1` agora é gravado como UTF-8 com BOM para compatibilidade com Windows PowerShell 5.1.

Nenhuma API, regra de negócio, frontend, banco, schema ou SQL foi alterado.
`VERSION.txt` permanece `0.3.33`.


# v0.3.33 — Análise de Fases + Comparativo de Resposta

A v0.3.33 transforma os check-ins da v0.3.32 em leitura comparativa dos ciclos nutricional e de treino.

## Novo endpoint

`GET /api/pacientes/{pacienteId}/analise-fases`

Para cada fase nutricional e de treino, o backend calcula:
- quantidade de check-ins;
- peso inicial;
- peso final;
- variação de peso;
- adesão alimentar média;
- adesão ao treino média;
- fome média;
- energia média;
- sono médio;
- percepção média de evolução.

## Destaques

O endpoint também identifica automaticamente:
- melhor adesão alimentar;
- melhor adesão ao treino;
- maior redução de peso;
- maior energia média.

Esses destaques são estatísticos e baseados apenas nos registros armazenados; não representam diagnóstico ou recomendação clínica automática.

## Interface

A análise aparece em:
- Alimentação: comparação das fases nutricionais;
- Treinos: comparação dos blocos de periodização;
- Resumo: destaques consolidados dos dois ciclos.

## Banco

Não há alteração de schema nesta versão.

`PREPARAR.ps1`: **28 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **390 etapas**.


# v0.3.32 — Check-ins de Evolução + Adesão por Fase

A v0.3.32 liga o planejamento de fases ao acompanhamento real da resposta do paciente.

## Indicadores do check-in

Cada check-in pode registrar:
- peso;
- adesão ao plano alimentar (%);
- adesão ao treino (%);
- fome (0–10);
- energia (0–10);
- sono (0–10);
- percepção de evolução (0–10);
- observações.

## Fases

O check-in pode ser vinculado simultaneamente a:
- uma fase nutricional;
- uma fase de treino.

No portal do paciente, o sistema tenta identificar automaticamente as fases atuais.

## Profissional

Novo conjunto de endpoints:
- `GET /api/pacientes/{pacienteId}/check-ins`
- `POST /api/pacientes/{pacienteId}/check-ins`
- `PUT /api/check-ins/{id}`
- `DELETE /api/check-ins/{id}`

O prontuário mostra histórico e gráficos de:
- peso;
- adesão alimentar;
- adesão ao treino;
- energia.

## Paciente

No portal, a tela **Evolução** passa a incluir os próprios check-ins.

Endpoints:
- `GET /api/portal/me/check-ins`
- `POST /api/portal/me/check-ins`

## Banco

Novo upgrade idempotente:

`scripts/sql/v0.3.32_checkins_acompanhamento.sql`

`PREPARAR.ps1`: **28 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **378 etapas**.


# v0.3.31 — Ciclos de Treino + Periodização

A v0.3.31 organiza as versões de treino em blocos explícitos de periodização.

## Fases de treino

O profissional pode criar:
- Adaptação;
- Hipertrofia;
- Força;
- Deload;
- Performance;
- Condicionamento;
- Personalizada.

Cada fase possui nome, tipo, objetivo, período, status, ordem, observações e vínculo opcional com uma versão específica do plano de treino.

As fases podem ser Planejada, Em andamento, Concluída ou Cancelada, podem ser reordenadas e uma fase em andamento não pode ser apagada diretamente.

Novo upgrade:
`scripts/sql/v0.3.31_fases_treino.sql`

`PREPARAR.ps1`: **27 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **362 etapas**.


# v0.3.30 — Fases Nutricionais + Planejamento de Ciclo

A v0.3.30 transforma a sequência de planos alimentares em um ciclo nutricional explícito.

## Fases

O profissional pode criar fases como:
- Adaptação;
- Cutting;
- Manutenção;
- Refeed;
- Ganho;
- Performance;
- Personalizada.

Cada fase possui:
- nome e tipo;
- objetivo;
- início e fim;
- status;
- ordem dentro do ciclo;
- plano alimentar opcionalmente vinculado;
- observações.

## Estados

As fases podem ficar como:
- Planejada;
- Em andamento;
- Concluída;
- Cancelada.

Uma fase em andamento não pode ser excluída diretamente.

## Sequenciamento

As fases podem ser reordenadas, permitindo montar ciclos como:

Adaptação → Cutting 1 → Refeed → Cutting 2 → Manutenção.

## Plano vinculado

Uma fase pode apontar para um PlanoAlimentar específico, inclusive sua versão V1/V2/V3.

O backend valida que o plano pertence ao mesmo paciente e organização.

## Banco

Novo upgrade idempotente:

`scripts/sql/v0.3.30_fases_nutricionais.sql`

`PREPARAR.ps1`: **26 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **348 etapas**.


# v0.3.29-r2 — Robustez do smoke runtime de metas por refeição

Correção exclusiva do `TESTAR.ps1`.

O teste 321 assumia que o primeiro plano retornado possuía uma primeira refeição indexável.
Em bases onde a coleção retornada não oferece uma refeição utilizável naquele registro, o PowerShell podia produzir `$null` e falhar antes da validação real.

O smoke agora:
- percorre os planos retornados;
- procura a primeira refeição não nula;
- valida `metas` e `desvios` pela coleção de nomes de propriedades;
- pula o runtime sem criar ou alterar dados se não houver refeição utilizável.

Nenhuma API, entidade, SQL, schema ou regra de negócio foi alterada.

`VERSION.txt` permanece `0.3.29`.


# v0.3.29-r1 — Correção do teste histórico de distribuição nutricional

Correção exclusiva do `TESTAR.ps1`.

O teste 280 ainda validava a copy antiga da v0.3.25 (`Percentual do total diário prescrito`).
Na v0.3.29 o mesmo bloco foi evoluído para exibir também as metas planejadas por refeição.

O teste passa a validar a implementação atual:
- `nutritionMealDistribution`;
- `mealTargetMini`;
- copy `Prescrito no dia + metas planejadas por bloco`.

Nenhuma API, entidade, regra de negócio, SQL ou schema foi alterado.

`VERSION.txt` permanece `0.3.29`.


# v0.3.29 — Metas por Refeição + Distribuição Planejada de Macros

A v0.3.29 aprofunda a prescrição nutricional: além da meta diária do plano, cada refeição pode possuir sua própria meta.

## Metas por refeição

Cada bloco alimentar passa a aceitar:
- calorias;
- proteínas;
- carboidratos;
- gorduras;
- fibras.

A API devolve, por refeição:
- totais prescritos;
- metas;
- desvios entre prescrito e planejado.

## Distribuição automática

Novo endpoint:

`POST /api/planos-alimentares/{id}/distribuir-metas-refeicoes`

O profissional informa a porcentagem diária destinada a cada refeição. A soma precisa fechar em 100%.

A mesma proporção é aplicada às metas diárias definidas no plano.

Exemplo:
- café: 20%;
- almoço: 35%;
- lanche/pré-treino: 15%;
- jantar: 30%.

## Ajuste manual

Novo endpoint:

`PUT /api/refeicoes-plano/{refeicaoId}/metas-nutricionais`

Permite sobrescrever uma refeição individualmente depois da distribuição automática.

## Compatibilidade

As metas por refeição são preservadas em:
- criação/edição do plano;
- progressões V2/V3;
- templates completos de plano alimentar;
- biblioteca reutilizável de refeições.

## Banco

Novo upgrade idempotente:

`scripts/sql/v0.3.29_metas_por_refeicao.sql`

`PREPARAR.ps1`: **25 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **334 etapas**.


# v0.3.28 — Evolução de Hábitos + Gráficos de Anamnese

A v0.3.28 inicia a segunda etapa do roadmap de acompanhamento longitudinal: transformar dados repetidos da anamnese em tendência visual.

## Novo endpoint

`GET /api/pacientes/{pacienteId}/evolucao-habitos?limite=24`

Retorna, em ordem cronológica:
- sono médio;
- qualidade do sono;
- despertares noturnos;
- nível de estresse;
- atividade física;
- dias de atividade por semana;
- consumo diário de água.

Também retorna o registro atual, o anterior e a diferença entre ambos para os indicadores numéricos.

## Prontuário

A aba **Anamnese** passa a receber um painel longitudinal com:
- valor atual;
- diferença contra o registro anterior;
- contexto qualitativo;
- gráficos SVG nativos de sono, estresse, frequência de atividade física e hidratação.

O mesmo bloco também aparece no **Resumo** do paciente para leitura rápida.

## Banco

Não há alteração de schema nesta versão.

`PREPARAR.ps1`: **24 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **320 etapas**.


# v0.3.27 — Biblioteca de Sessões de Treino + Inserção Rápida

A v0.3.27 deixa a prescrição de treino modular, do mesmo modo que a biblioteca de refeições fez com a dieta.

## Salvar uma sessão

Cada sessão de um plano pode ser salva como bloco reutilizável.

O snapshot preserva:
- nome;
- dias da semana;
- exercícios;
- ordem;
- séries;
- repetições;
- carga e unidade;
- descanso;
- tempo;
- observações.

## Biblioteca

Novo endpoint:
`GET /api/modelos-sessoes-treino`

Suporta busca, categoria e ativos/inativos.

## Inserção rápida

Novo endpoint:
`POST /api/treinos/{planoId}/inserir-modelo-sessao/{modeloId}`

A sessão é adicionada no fim do plano atual.

Planos concluídos não aceitam inserção e todos os exercícios são revalidados contra o catálogo ativo.

## Interface

Na aba Treinos:
- cada sessão ganha **Salvar sessão**;
- entra **Biblioteca de sessões**;
- o profissional escolhe o plano de destino;
- busca Push/Pull/Pernas/Full Body etc.;
- ajusta nome/dias;
- insere em poucos cliques.

## Banco

Novo upgrade idempotente:
`scripts/sql/v0.3.27_modelos_sessoes_treino.sql`

`PREPARAR.ps1`: **24 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **308 etapas**.


# v0.3.27 — Biblioteca de Refeições + Inserção Rápida

A v0.3.27 deixa a montagem de dieta mais modular.

## Salvar uma refeição

Cada refeição de um plano alimentar pode ser salva como bloco reutilizável.

O snapshot preserva:
- nome e horário;
- alimentos;
- quantidades e gramas;
- substituições;
- observações.

O modelo pode receber categoria e descrição.

## Biblioteca

Novo endpoint:
`GET /api/modelos-refeicoes`

Suporta busca, categoria e ativos/inativos.

## Inserção rápida

Novo endpoint:
`POST /api/planos-alimentares/{planoId}/inserir-modelo-refeicao/{modeloId}`

A refeição é adicionada ao final do plano atual sem recriar o restante da dieta.

Planos concluídos não aceitam inserção.

Todos os alimentos e substituições são revalidados antes de copiar.

## Interface

Na aba alimentar:
- cada refeição ganha **Salvar refeição**;
- entra o botão **Biblioteca de refeições**;
- o profissional escolhe o plano ativo de destino;
- busca o bloco;
- ajusta nome/horário;
- insere em poucos cliques.

## Banco

Novo upgrade idempotente:
`scripts/sql/v0.3.27_modelos_refeicoes.sql`

`PREPARAR.ps1`: **23 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **296 etapas**.


# v0.3.27 — Metas Nutricionais + Distribuição de Macros por Refeição

A v0.3.27 aprofunda o construtor alimentar para uso real em consultoria nutricional.

## Metas explícitas

Cada plano pode agora possuir metas diárias de:
- calorias;
- proteínas;
- carboidratos;
- gorduras;
- fibras.

As metas podem ser informadas já na criação ou alteradas depois sem reconstruir refeições.

## Meta × prescrito

O prontuário mostra lado a lado o total calculado do plano e a meta definida, incluindo diferença de cada indicador.

Novo endpoint:
`PUT /api/planos-alimentares/{id}/metas-nutricionais`

## Análise nutricional

Novo endpoint:
`GET /api/planos-alimentares/{id}/analise-nutricional`

Retorna:
- metas;
- totais prescritos;
- desvios;
- distribuição percentual por refeição para calorias, P/C/G e fibras.

## Progressão e templates

Metas acompanham a progressão alimentar proporcionalmente e também passam a ser preservadas nos templates da v0.3.23.

## Banco

Novo upgrade idempotente:
`scripts/sql/v0.3.27_metas_nutricionais.sql`

`PREPARAR.ps1`: **22 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **284 etapas**.


# v0.3.27 — Templates de Treino + Criação Rápida

A v0.3.27 leva a reutilização de templates para as prescrições de treino.

## Salvar como modelo

Qualquer plano de treino existente pode virar um modelo reutilizável.

O snapshot preserva:
- sessões;
- dias da semana;
- exercícios;
- séries;
- repetições;
- carga e unidade;
- descanso;
- tempo;
- observações;
- objetivo.

## Usar modelo

Na aba Treinos entra o botão **Usar modelo**.

O profissional pode buscar um template, visualizar quantidade de sessões/exercícios e criar uma nova ficha para o paciente atual.

O treino criado nasce como V1 e continua compatível com a progressão de treino da v0.3.22.

## Segurança

Antes de instanciar o template, todos os exercícios são revalidados contra o catálogo ativo da organização.

## Banco

Novo upgrade idempotente:

`scripts/sql/v0.3.27_modelos_plano_treino.sql`

`PREPARAR.ps1`: **21 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **270 etapas**.


# v0.3.27 — Correção de compilação dos templates alimentares

Correção pontual no `ModelosPlanosAlimentaresController`.

O retorno da listagem usava `modelos.Select(ToResponse)`. Como `ToResponse` possui um segundo parâmetro opcional, o compilador não conseguia resolver corretamente a sobrecarga de `Enumerable.Select`.

Foi substituído por uma lambda explícita:

`modelos.Select(x => ToResponse(x)).ToList()`

Nenhum schema, SQL, migration ou regra de negócio foi alterado.

`VERSION.txt` permanece `0.3.27`.


# v0.3.27 — Templates de Plano Alimentar + Criação Rápida

A v0.3.27 melhora diretamente a escala da consultoria nutricional.

## Salvar como modelo

Qualquer plano alimentar existente pode ser transformado em template reutilizável.

O modelo preserva refeições, horários, alimentos, quantidades, gramas, substituições e observações.

## Usar modelo

Na aba Plano alimentar entra o botão **Usar modelo**.

O profissional pesquisa o catálogo de modelos, escolhe um template, define nome e datas e cria um novo plano para o paciente atual.

O novo plano nasce como V1 e pode depois usar a progressão alimentar já existente.

## Segurança do catálogo

Ao reutilizar um modelo, todos os alimentos e substituições são revalidados. Alimentos inativos ou de outra organização bloqueiam a criação, evitando planos quebrados.

## Banco

Novo upgrade idempotente:

`scripts/sql/v0.3.27_modelos_plano_alimentar.sql`

`PREPARAR.ps1`: **20 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **258 etapas**.


# v0.3.27 — Correção da referência histórica da progressão alimentar

Correção exclusiva do `TESTAR.ps1`.

O teste 233 estava procurando `v0.3.27_progressao_plano_alimentar.sql`, mas esse upgrade foi criado na v0.3.21.

Referências corretas preservadas:
- SOAP: `v0.3.15_evolucoes_clinicas.sql`;
- progressão alimentar: `v0.3.21_progressao_plano_alimentar.sql`;
- progressão de treino: `v0.3.27_progressao_treino.sql`.

Nenhuma API, entidade, regra de negócio, banco ou migration foi alterada.


# v0.3.27 — Correção do smoke histórico SOAP

Correção exclusiva do `TESTAR.ps1`.

O teste 179 misturava duas responsabilidades:
- validar o upgrade histórico SOAP, criado na v0.3.15;
- validar a contagem atual do `PREPARAR.ps1`.

A revisão passa a validar separadamente:
- `scripts/sql/v0.3.15_evolucoes_clinicas.sql`;
- `PREPARAR.ps1` atual em `19/19`;
- presença do upgrade atual `v0.3.27_progressao_treino.sql`.

Nenhuma API, entidade, regra de treino, banco ou migration foi alterada.


# v0.3.27 — Progressão de Treino + Duplicação de Ciclo

A v0.3.27 leva o versionamento para os planos de treino sem alterar o histórico das execuções realizadas.

## Progressão versionada

Cada plano passa a possuir:
- `PlanoOrigemId`;
- `Versao`;
- `AjusteCargaPercentual`;
- `AjusteSeries`;
- `AjusteRepeticoes`;
- `AjusteDescansoSegundos`.

## Simulação

`GET /api/treinos/{id}/simular-progressao`

Mostra exercícios afetados, prescrições com carga, soma de cargas atual/projetada e quantas prescrições de repetição podem ser ajustadas com segurança.

## Nova versão

`POST /api/treinos/{id}/duplicar`

Copia sessões, exercícios, ordem, observações e prescrição, aplicando ajustes em lote.

Ajustes:
- carga: -50% a +100%;
- séries: -5 a +10;
- repetições: -20 a +30;
- descanso: -300 a +600 s.

Repetições `10`, `8-12` e `8 a 12` são ajustadas. Texto complexo é preservado.

## Banco

Novo upgrade idempotente:
`scripts/sql/v0.3.27_progressao_treino.sql`

`PREPARAR.ps1`: **19 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **246 etapas**.


# v0.3.27 — Correções de UX do plano alimentar

Revisão da v0.3.27 sem alteração adicional de schema.

Correções:
- `TESTAR.ps1` passa a validar corretamente `PREPARAR 18/18`;
- botão `+ Novo plano` da aba Plano alimentar ganha binding robusto;
- `openMealPlanForm` recupera o paciente atual pelo `state.patientId` quando necessário;
- modal de criação/progressão alimentar passa a usar largura ampliada e área interna rolável;
- fechamento do modal remove a classe específica de nutrição.

O upgrade de banco continua sendo o mesmo `v0.3.27_progressao_plano_alimentar.sql`.


# v0.3.27 — Progressão do Plano Alimentar + Duplicação Inteligente

A v0.3.27 inicia o novo foco do roadmap: escalar a consultoria nutricional e de treino.

## Progressão alimentar versionada

Cada plano alimentar passa a possuir:
- `PlanoOrigemId`;
- `Versao`;
- `AjustePercentual`.

Ao criar uma progressão, o plano anterior é preservado e uma nova versão é criada.

## Simular ajuste

Novo endpoint:

`GET /api/planos-alimentares/{id}/simular-ajuste`

Permite simular:
- percentual das porções;
- calorias alvo.

O sistema projeta calorias, proteínas, carboidratos, gorduras e fibras antes de salvar.

## Duplicação inteligente

Novo endpoint:

`POST /api/planos-alimentares/{id}/duplicar`

A nova versão copia:
- refeições;
- horários;
- alimentos;
- quantidades;
- gramas;
- substituições;
- observações.

As porções são escaladas proporcionalmente.

Ajuste permitido nesta etapa: **-50% a +100%**.

O profissional pode concluir automaticamente o plano anterior ao ativar a nova versão.

## Interface

Na aba Plano alimentar cada plano ganha:
- versão;
- percentual de ajuste;
- botão **Criar progressão**;
- simulador Atual x Projetado;
- modo Percentual;
- modo Calorias alvo.

## Próximos passos do novo roadmap

Prioridade:
1. progressão e templates de dieta;
2. progressão e templates de treino;
3. ciclos/fases de dieta e treinamento;
4. gráficos de anamnese e evolução;
5. consultoria nutricional e esportiva mais profunda.

## Banco

Novo upgrade idempotente:

`scripts/sql/v0.3.27_progressao_plano_alimentar.sql`

`PREPARAR.ps1`: **18 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **234 etapas**.


# v0.3.27 — Minha Conta + Troca de Senha

A v0.3.27 fecha o primeiro ciclo de segurança de acesso da equipe.

## Minha Conta

Nova área dentro de Configurações com:
- nome;
- e-mail;
- tipo de perfil;
- data de criação da conta.

O usuário pode editar o próprio nome.

Quando existe perfil `Profissional`, o nome é sincronizado automaticamente.

## Alterar minha senha

Novo endpoint:

`POST /api/configuracoes/minha-conta/alterar-senha`

O fluxo exige:
- senha atual;
- nova senha;
- confirmação;
- mínimo de 10 caracteres;
- política de senha configurada no ASP.NET Core Identity;
- nova senha diferente da atual.

A alteração usa `UserManager.ChangePasswordAsync`.

## Auditoria segura

A troca gera evento `PASSWORD_CHANGE`.

Nenhuma senha atual ou nova é armazenada no `AuditLog`.

## Banco

Sem migration ou SQL upgrade novo.

`PREPARAR.ps1`: **17 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **222 etapas**.


# v0.3.27 — Equipe v2 + Segurança de Acesso

A v0.3.27 expande a gestão de equipe introduzida na v0.3.18.

## Busca e filtros

A tela Equipe agora permite filtrar por:
- nome ou e-mail;
- tipo de acesso;
- status ativo/inativo.

A busca usa `ILIKE` no PostgreSQL e debounce na interface.

## Redefinir senha temporária

Administradores podem gerar uma nova senha temporária para outro membro ativo da equipe.

O fluxo usa:
- `GeneratePasswordResetTokenAsync`;
- `ResetPasswordAsync`.

Proteções:
- limitado ao `OrganizacaoId` atual;
- pacientes não entram no fluxo da equipe;
- membro inativo deve ser reativado antes do reset;
- o administrador não redefine a própria senha por essa tela.

## Auditoria segura

A redefinição gera um evento `PASSWORD_RESET` no `AuditLog`.

A senha temporária **não é registrada** no log. A auditoria salva apenas que a redefinição ocorreu.

## Banco

Sem migration ou SQL upgrade novo.

`PREPARAR.ps1`: **17 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **214 etapas**.


# v0.3.27 — Equipe + Gestão de Profissionais

A v0.3.27 adiciona a primeira gestão administrativa de equipe do consultório.

## Nova tela Equipe

Disponível apenas para usuários do tipo **Admin**.

Mostra:
- membros da organização;
- e-mail;
- tipo de acesso;
- status ativo/inativo;
- registro profissional;
- especialidade;
- resumo de membros ativos, inativos e profissionais.

## Adicionar membro

O administrador pode criar acessos para:
- Administrador;
- Médico;
- Nutricionista;
- Personal;
- Secretaria.

O acesso é criado pelo ASP.NET Core Identity com uma senha temporária informada pelo administrador.

Médico, Nutricionista e Personal exigem registro profissional e recebem automaticamente um perfil `Profissional`.

## Editar membro

Permite:
- editar nome;
- trocar tipo de acesso;
- ativar/inativar;
- atualizar registro e especialidade.

A Role do Identity é sincronizada com `TipoUsuario`.

O administrador atualmente logado não pode inativar a si próprio nem remover o próprio tipo Admin.

## Auditoria

Criação e edição geram `AuditLog` com dados antes/depois.

## Multi-tenant

Todos os membros são limitados ao `OrganizacaoId` do administrador atual.

## Banco

Sem migration ou SQL upgrade novo.

`PREPARAR.ps1`: **17 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **206 etapas**.


# v0.3.27 — Handoff Clínico + Impressão do Resumo

A v0.3.27 transforma o resumo clínico consolidado em uma ferramenta prática de passagem de caso.

## Copiar handoff
Novo botão **Copiar handoff** na aba Resumo. O texto inclui agenda, evolução SOAP, avaliação corporal, anamnese, exames fora da referência, metas, treinos e pendências.

A cópia usa a API moderna de clipboard e possui fallback para navegadores sem suporte.

## Imprimir
Novo botão **Imprimir** abre uma versão limpa e própria para impressão, contendo os dados consolidados do resumo clínico.

Conteúdo textual do prontuário é escapado antes de entrar no HTML de impressão.

## Banco
Sem migration ou SQL upgrade novo.

`PREPARAR.ps1`: **17 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **196 etapas**.


# v0.3.27 — Correção da referência histórica do upgrade SOAP

Correção exclusiva do `TESTAR.ps1`.

Ao promover a versão para v0.3.27, uma substituição automática alterou a referência do upgrade SOAP de `v0.3.15_evolucoes_clinicas.sql` para `v0.3.27_evolucoes_clinicas.sql`.

O arquivo correto permanece `v0.3.15_evolucoes_clinicas.sql`, pois foi nessa versão que a tabela `EvolucoesClinicas` foi introduzida.

Nesta revisão:
- o teste 179 volta a validar o arquivo histórico correto;
- o `PREPARAR.ps1` permanece apontando para o SQL correto;
- nenhuma API, entidade, banco, schema ou regra de negócio foi alterada.


# v0.3.27 — Resumo Clínico Consolidado + Handoff

A v0.3.27 transforma a aba **Resumo** do prontuário em uma leitura rápida do estado atual do paciente.

## Novo endpoint

`GET /api/pacientes/{pacienteId}/resumo-clinico`

Consolida:
- última consulta;
- próxima consulta;
- última evolução SOAP;
- última avaliação corporal e IMC;
- resultados numéricos recentes fora da faixa de referência registrada;
- última anamnese, com alergias e medicamentos;
- metas ativas;
- treinos concluídos/registrados nos últimos 30 dias;
- pendências abertas e de alta prioridade.

## Prontuário

A aba **Resumo** recebe um painel consolidado com indicadores e blocos clínicos, sem substituir os módulos detalhados.

Há botão **Atualizar** para recarregar o prontuário e recalcular o resumo.

## Handoff

O objetivo é facilitar a leitura rápida do contexto por outro profissional autorizado da organização antes de abrir cada módulo.

O resumo é informativo e não substitui julgamento clínico.

## Banco

Sem migration ou SQL upgrade novo.

`PREPARAR.ps1`: **17 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **188 etapas**.


# v0.3.27 — Smoke SOAP sem dependência de encoding/regex

Correção exclusiva do `TESTAR.ps1`.

Os testes SOAP finais agora validam o código-fonte local com `String.Contains`, evitando falso negativo causado por regex, acentuação, bullet e transformação de conteúdo pelo `Invoke-WebRequest`.

A revisão endurece:
- `[177/180]` estrutura S/O/A/P e leitura no submit;
- `[178/180]` edição visual e PUT;
- `[180/180]` estilos SOAP.

Nenhuma API, entidade, banco, migration, CSS ou JavaScript funcional foi alterado.


# v0.3.27 — Smoke SOAP alinhado ao helper real da interface

Correção exclusiva do `TESTAR.ps1`.

O formulário SOAP é montado por meio do helper `area(label, name, ...)`, portanto os atributos finais `name=...` não aparecem literalmente no código-fonte estático do `app.js`.

O teste `[177/180]` agora valida a implementação real:
- criação dos quatro campos via `area(...)`;
- leitura de `subjetivo`, `objetivo`, `avaliacao` e `plano` no submit;
- presença de `openEvolutionForm`.

Nenhuma API, entidade, banco, migration ou comportamento funcional foi alterado.


# v0.3.27 — Correção do smoke test do formulário SOAP

Correção exclusiva do `TESTAR.ps1`.

O teste `[177/180]` validava os rótulos visuais SOAP por texto literal com acentuação. A interface real usa os campos estruturados do formulário, então o smoke podia falhar mesmo com a funcionalidade presente.

Agora o teste valida:
- `openEvolutionForm`;
- campo `subjetivo`;
- campo `objetivo`;
- campo `avaliacao`;
- campo `plano`.

Nenhuma API, entidade, interface funcional, migration ou regra de negócio foi alterada.


# v0.3.27 — Evolução Clínica SOAP + Plano de Conduta

A v0.3.27 adiciona um registro clínico estruturado independente da consulta tradicional.

## Evolução SOAP

Cada evolução possui:
- **S — Subjetivo:** relato do paciente, sintomas, percepção e adesão;
- **O — Objetivo:** achados mensuráveis e observáveis;
- **A — Avaliação:** síntese e avaliação profissional;
- **P — Plano:** conduta, orientação e próximos passos;
- observações complementares.

## Vínculo opcional com consulta

Uma evolução pode existir sozinha ou ser associada a uma consulta do mesmo paciente.

O backend valida paciente, organização e consulta antes de salvar.

## Histórico e edição

Nova aba **Evoluções** no prontuário:
- histórico em ordem cronológica reversa;
- profissional responsável;
- consulta vinculada quando existir;
- edição pelo profissional autor.

Criação e atualização geram `AuditLog` com snapshot antes/depois.

## Timeline

Evoluções SOAP passam a aparecer na timeline clínica do paciente.

## Banco

Nova tabela `EvolucoesClinicas`.

Upgrade idempotente:
`scripts/sql/v0.3.27_evolucoes_clinicas.sql`

`PREPARAR.ps1`: **17 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **180 etapas**.


# v0.3.27 — Central do Dia

Nova visão operacional para começar o dia com consultas, follow-ups, pendências e pacientes que merecem revisão em uma única tela.

## Hoje
A tela reúne:
- consultas do dia;
- follow-ups vencidos;
- follow-ups previstos para hoje;
- pendências de alta prioridade, vencidas ou vencendo;
- pacientes com pendências abertas ou sem retorno futuro.

## Timezone
A interface envia o offset do navegador e o backend converte a janela local de hoje para UTC antes de consultar a agenda.

## Ações rápidas
- abrir prontuário;
- registrar contato;
- abrir agenda;
- abrir fila de follow-up;
- abrir pendências;
- abrir carteira.

## Dashboard
Novo resumo “Hoje” com consultas, follow-ups e pendências prioritárias.

## Banco
Sem migration ou SQL upgrade novo.

`PREPARAR.ps1`: **16 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **170 etapas**.


# v0.3.27 — Correção de compilação da Busca Global

Correção pontual em `BuscaGlobalController.cs`.

A entidade `Consulta` não possui `OrganizacaoId` diretamente. O filtro multi-tenant da busca de consultas passa agora por `Consulta.Paciente.OrganizacaoId`.

Nesta revisão:
- `x.OrganizacaoId == org` foi corrigido para `x.Paciente.OrganizacaoId == org` na consulta de agendas;
- isolamento por organização permanece garantido;
- nenhuma entidade foi alterada;
- nenhuma migration/SQL upgrade foi adicionada;
- banco e dados existentes permanecem intactos;
- `PREPARAR.ps1` continua no modo rápido.


# v0.3.27 — Busca Global + Central de Ações

A v0.3.27 adiciona uma busca única para navegar rapidamente por dados operacionais do consultório.

## Busca global

Atalho:
`Ctrl + K` no Windows/Linux.
`Cmd + K` no macOS.

Também há botão no cabeçalho.

A busca consulta:
- pacientes;
- pendências abertas;
- follow-ups;
- consultas.

## Pacientes

Pesquisa por:
- nome;
- e-mail;
- telefone;
- CPF.

Ao selecionar, abre diretamente o prontuário.

## Pendências

Pesquisa por:
- título;
- descrição;
- nome do paciente.

O resultado leva à fila de pendências.

## Follow-up

Pesquisa por:
- nome do paciente;
- resultado do contato.

O resultado leva à fila de follow-up.

## Consultas

Pesquisa por:
- paciente;
- motivo.

O resultado leva à agenda.

## UX

- debounce de 220 ms para evitar requisições a cada tecla;
- mínimo de 2 caracteres;
- Enter abre o primeiro resultado;
- Esc fecha a central;
- clique fora fecha o modal;
- layout responsivo.

## Segurança

A busca respeita `OrganizacaoId` em todas as fontes.

## Banco

Sem migration/upgrade SQL novo.

`PREPARAR.ps1`: **16 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **162 etapas**.


# v0.3.27 — Correção final da copy histórica do teste 104

Correção exclusiva do `TESTAR.ps1`.

O teste `[153/154]` detectou corretamente uma última mensagem antiga no teste `[104/154]`:
`Validando versao v0.3.27 sem schema novo...`

Ela foi substituída por:
`Validando versao v0.3.27 e compatibilidade do schema...`

Nenhuma API, interface, banco, schema ou regra de negócio foi alterada.


# v0.3.27 — Correção do falso positivo no teste 153

Correção exclusiva do `TESTAR.ps1`.

O teste `[153/154]` procurava as expressões históricas `nao exige schema novo` e `sem schema novo` no arquivo inteiro. Como essas próprias expressões existiam dentro da condição do teste 153, ele sempre encontrava a si mesmo e falhava.

Nesta revisão:
- o teste localiza o início do próprio bloco `[153/154]`;
- analisa apenas o conteúdo anterior a ele;
- continua detectando mensagens históricas antigas;
- deixa de acusar falso positivo contra o próprio código;
- API, interface, banco e schema permanecem inalterados.


# v0.3.27 — Correção do teste 90 de notificações

Correção exclusiva do `TESTAR.ps1`.

O teste `[90/154]` foi convertido para validação estática, mas ainda verificava uma propriedade do antigo retorno da chamada mutável de sincronização.

Agora ele apenas confirma:
- a existência de `POST /api/notificacoes/sincronizar`;
- a regra de sincronização profissional;
- a regra de sincronização do paciente.

Nenhuma sincronização é executada pelo smoke test.

API, interface, banco e schema permanecem inalterados.


# v0.3.27 — Correção de compilação das notificações

Correção pontual no `NotificacoesController.cs`.

Na v0.3.27, a substituição da mensagem de lembrete do paciente deixou um fragmento residual da string anterior e provocou `CS1003` na compilação.

Nesta revisão:
- a mensagem do paciente fica `Consulta próxima • {ProfissionalNome}`;
- continua sem `ToLocalTime()` do servidor;
- nenhuma API, entidade ou regra de negócio foi alterada;
- nenhum schema/upgrade SQL novo foi adicionado;
- não é necessário limpar ou repopular o banco;
- o `PREPARAR.ps1` rápido permanece igual.


# v0.3.27 — Estabilização + Qualidade

Release de consolidação antes dos próximos módulos maiores.

## Insights
O dashboard soma os insights completos de cada paciente antes de limitar a lista visual aos quatro principais. Os totais Alta/Média/Baixa deixam de ser subcontados.

## Perguntas de anamnese
`GET /api/anamnese/perguntas?incluirInativas=true` permite carregar perguntas desativadas para administração e reativação.

## Notificações e horário
As mensagens internas deixam de formatar horários usando `ToLocalTime()` do servidor. Os instantes estruturados permanecem em UTC e são apresentados localmente pela interface.

## Logout e polling
Os botões de logout profissional e paciente são religados ao wrapper que encerra o polling periódico de notificações.

## Portal do paciente
O clique em notificações usa um resolvedor de navegação com fallback para as funções existentes do portal e tolera diferentes representações de `tipoUsuario`.

## Smoke test não-mutável
O `TESTAR.ps1` não chama mais `POST /api/notificacoes/sincronizar` nem usa `GET ...?sincronizar=true`. A sincronização é validada estaticamente e a listagem usa `sincronizar=false`.

## Banco
Sem migration/upgrade SQL novo.

`PREPARAR.ps1`: **16 etapas**.
`POPULAR.ps1`: **13 etapas**.
`TESTAR.ps1`: **154 etapas**.


# v0.3.27 — Correção do smoke test do relatório HTML

Correção exclusiva do `TESTAR.ps1`.

Após a correção de compilação da `v0.3.27`, o relatório HTML passou a montar o percentual de comparecimento usando a variável `taxa` no `StringBuilder`. O teste `[142/146]` ainda procurava o identificador antigo `TaxaComparecimento`.

Nesta revisão:
- o teste passa a validar o conteúdo real do relatório (`Comparecimento` + `taxa`);
- API e interface permanecem inalteradas;
- nenhum schema ou upgrade SQL novo foi adicionado;
- nenhum dado precisa ser recriado;
- o `PREPARAR.ps1` rápido permanece igual.


# v0.3.27 — Correção de compilação do relatório gerencial

Correção pontual no `GestaoExportController`.

O relatório HTML da v0.3.27 usava uma raw interpolated string com chaves CSS, causando `CS9006` e `CS1073` durante o `dotnet build`.

Nesta revisão:
- o HTML passa a ser montado com `StringBuilder`;
- CSS e dados continuam iguais;
- nenhuma entidade foi alterada;
- nenhum upgrade SQL novo foi adicionado;
- nenhum dado precisa ser recriado;
- o `PREPARAR.ps1` continua sem atualização automática do SDK/dotnet-ef.


# v0.3.27 — Relatórios Gerenciais + PREPARAR Mais Rápido

## PREPARAR sem atualização automática do .NET

Em ciclos rápidos de desenvolvimento, o `PREPARAR.ps1` não tenta mais instalar ou atualizar o SDK .NET.

Agora ele:
- confirma rapidamente que `dotnet` existe;
- executa `dotnet --version`;
- segue usando o SDK já instalado;
- verifica `dotnet-ef` sem fazer `tool update`;
- instala `dotnet-ef 10.*` somente se a ferramenta ainda não existir.

Se uma versão futura realmente exigir outro SDK, a atualização deve ser feita de forma explícita naquela versão.

Isso reduz o tempo de preparação em sucessivas rodadas de teste.

## Gestão — Exportar CSV

A tela Gestão recebe **Exportar CSV**.

O arquivo contém, por paciente:
- nome;
- e-mail;
- telefone;
- se é novo no período;
- última consulta realizada;
- próxima consulta;
- pendências abertas;
- follow-ups realizados no período;
- próximo contato previsto.

O download é autenticado com o JWT atual.

## Gestão — Relatório imprimível

Novo botão **Relatório imprimível**.

Gera HTML autenticado com:
- período;
- pacientes ativos;
- consultas realizadas;
- taxa de comparecimento;
- follow-ups;
- faltas;
- cancelamentos;
- pendências abertas;
- novos pacientes;
- listagem dos pacientes.

A página possui botão de impressão e pode ser salva como PDF pelo navegador.

## Banco

Sem migration/schema novo.

`PREPARAR.ps1` continua com **16 etapas**, porém sem atualização automática do .NET.

`POPULAR.ps1` continua com **13 etapas**.

Smoke test: **146 etapas**.


# v0.3.27 — Gestão + Indicadores Operacionais

A v0.3.27 adiciona uma visão gerencial da operação sem criar estruturas novas no banco.

## Nova tela: Gestão

Filtros de período:
- 7 dias;
- 30 dias;
- 60 dias;
- 90 dias;
- 180 dias;
- 365 dias.

## Indicadores

### Pacientes
- pacientes ativos;
- pacientes novos no período.

### Consultas
- total;
- realizadas;
- agendadas;
- canceladas;
- faltas;
- taxa de comparecimento.

A taxa considera:
`realizadas / (realizadas + faltas)`.

### Follow-up
- contatos realizados no período;
- follow-ups atualmente vencidos.

### Pendências
- abertas;
- resolvidas dentro do período.

### Engajamento
- execuções de treino;
- registros de diário;
- registros de metas.

## Visualizações

- barras por status de consulta;
- atividade semanal combinando consultas + follow-ups;
- pacientes que merecem revisão operacional.

## Dashboard

Nova seção **Resumo de gestão** com:
- comparecimento;
- follow-ups;
- pendências;
- novos pacientes.

## Banco

Sem migration/schema novo.

A versão utiliza somente dados já existentes.

`PREPARAR.ps1` continua com **16 etapas**.

`POPULAR.ps1` continua com **13 etapas**.

Smoke test: **138 etapas**.


# v0.3.27 — Fila de Follow-up + Lembretes de Contato

A v0.3.8 passou a registrar contatos e próximo contato previsto. A v0.3.27 transforma isso em uma rotina operacional.

## Nova tela: Follow-up

A fila mostra pacientes com próximo contato definido e classifica em:
- vencido;
- hoje;
- próximos 7 dias;
- futuro.

Cada item exibe:
- paciente;
- telefone/e-mail;
- próximo contato;
- último contato;
- último canal;
- último resultado;
- quantidade de contatos nos últimos 30 dias;
- dias de atraso.

## Ações

Direto na fila:
- registrar novo contato;
- abrir prontuário.

Ao registrar um contato, a fila é recarregada.

## Filtros

- vencidos + hoje + próximos 7 dias;
- vencidos;
- hoje;
- próximos 7 dias;
- futuros;
- todos;
- busca por paciente, telefone ou e-mail.

## Notificações

O sino profissional passa a gerar lembretes quando:
- o próximo follow-up está vencido;
- o próximo follow-up ocorrerá nas próximas 24 horas.

O clique leva para a nova fila de follow-up.

## Dashboard

Nova seção **Follow-ups** mostra os contatos mais próximos/vencidos e acesso à fila completa.

## Base demo

O `POPULAR.ps1` agora também cria follow-ups demonstrativos idempotentes para os cinco pacientes de cenário.

`POPULAR.ps1`: **13 etapas**.

## Banco

Sem schema novo.

A versão reutiliza `InteracoesAcompanhamento` criada na v0.3.8.

`PREPARAR.ps1` continua com **16 etapas**.

Smoke test: **130 etapas**.


# v0.3.27 — Correção do smoke test

Correção exclusiva do `TESTAR.ps1`.

A v0.3.27 adicionou o upgrade de follow-up e passou o `PREPARAR.ps1` de 15 para 16 etapas, porém o teste legado `[104/122]` ainda verificava `[15/15]`.

Nesta revisão:
- a asserção passa a validar `[16/16]`;
- nenhuma API foi alterada;
- nenhum schema foi alterado;
- nenhuma migration/upgrade novo foi adicionado;
- nenhum dado precisa ser recriado.


# v0.3.27 — Follow-up + Ações Rápidas da Carteira

A v0.3.7 identifica e prioriza pacientes. A v0.3.27 permite agir diretamente a partir dessa visão.

## Ações rápidas na Carteira

Em cada paciente:
- **Registrar contato**;
- **Agendar retorno**;
- **Criar pendência**;
- **Abrir prontuário**.

## Follow-up persistente

Nova entidade `InteracaoAcompanhamento` armazena:
- organização;
- paciente;
- profissional;
- data/hora;
- canal;
- resultado;
- observações;
- próximo contato previsto.

Canais:
- Telefone;
- WhatsApp;
- Email;
- Presencial;
- Outro.

## Carteira

Passa a exibir:
- último contato;
- próximo contato;
- número de contatos nos últimos 30 dias.

## Prontuário

A aba **Resumo** recebe uma seção **Follow-up** com:
- histórico recente;
- canal;
- profissional;
- resultado;
- observações;
- próximo contato;
- botão para registrar novo contato.

## Auditoria

Cada novo contato gera `AuditLog`.

## Banco

Nova tabela:
- `InteracoesAcompanhamento`.

Upgrade idempotente:
`scripts/sql/v0.3.27_followup.sql`

`PREPARAR.ps1`: **16 etapas**.

Smoke test: **122 etapas**.


# v0.3.27 — Carteira de Pacientes + Priorização de Acompanhamento

A base demo da v0.3.6 passou a ter vários pacientes e cenários. A v0.3.27 transforma esses dados em uma visão operacional da carteira.

## Nova tela: Carteira

Mostra cada paciente com:
- prioridade;
- motivo da prioridade;
- número de sinais;
- pendências abertas;
- treinos nos últimos 30 dias;
- registros de metas nos últimos 14 dias;
- última consulta;
- próxima consulta;
- última avaliação;
- último exame.

## Priorização

O backend calcula um score operacional considerando:
- pendências de alta prioridade;
- demais pendências abertas;
- exames mais recentes fora da referência registrada;
- variação relevante de peso;
- ausência de retorno futuro;
- dados clínicos antigos.

O score é convertido em:
- Alta;
- Média;
- Baixa;
- Estável.

A classificação serve para organização da carteira. Não é diagnóstico, prognóstico nem classificação médica de risco.

## Filtros

- busca por nome/e-mail;
- prioridade;
- ordenar por prioridade;
- nome;
- próximo retorno;
- última consulta.

## Dashboard

Nova seção **Pacientes para acompanhar** mostra os primeiros pacientes por prioridade e permite abrir o prontuário ou acessar a carteira completa.

## Endpoint

`GET /api/carteira`

Parâmetros:
- `busca`;
- `prioridade`;
- `ordenar`.

## Banco

Sem migration nova.

`PREPARAR.ps1` continua com 15 etapas.

Smoke test: **112 etapas**.


# v0.3.27 — Correção do POPULAR.ps1

Corrige a falha observada na etapa `[6/12]` quando o endpoint de avaliações retorna uma coleção vazia/nula e o PowerShell tentava executar cast direto para `[datetime]`.

A revisão:
- adiciona `As-Array` para normalizar respostas vazias;
- adiciona `Date-Key` para conversão segura de datas;
- torna idempotência de avaliações tolerante a valores nulos;
- aplica a mesma proteção às buscas de consultas, exames, metas, diário, treinos e pendências;
- não altera API, schema ou dados já criados.

Os cinco pacientes criados antes da falha são reaproveitados normalmente na próxima execução do `POPULAR.ps1`.


# v0.3.27 — Base Demo Rica + Cenários de Carteira

A v0.3.27 é uma versão de consolidação voltada a deixar o sistema cheio de dados úteis para validar dashboard, gráficos, insights, agenda, pendências, notificações e treinos.

## Novo script: `POPULAR.ps1`

O script adiciona cinco pacientes demonstrativos além do paciente demo já existente.

Ele é desenhado para ser **idempotente**: ao encontrar o mesmo paciente, consulta, avaliação, coleta, meta, diário, treino ou pendência principal, reaproveita o registro em vez de recriá-lo.

### Cenários

- **Ana Ribeiro** — boa evolução corporal, boa adesão, exames melhorando, retorno futuro e plano de treino.
- **Bruno Martins** — ganho de peso, LDL/glicemia alterados, baixa adesão, plano ativo sem execução e pendência vencida.
- **Carla Souza** — composição corporal estável, TSH acima da referência mais recente e retorno programado.
- **Diego Alves** — boa evolução, boa adesão e plano de treinamento ativo.
- **Elisa Ferreira** — baixa adesão, ausência de diário, sem retorno futuro e pendência de acompanhamento.

## Dados gerados

Para os cenários aplicáveis:
- pacientes;
- consultas passadas e futuras;
- três avaliações corporais por paciente;
- duas coletas laboratoriais por paciente;
- quatro marcadores por coleta;
- metas;
- registros de metas;
- diário;
- planos de treino;
- pendências;
- sincronização das notificações.

## Como usar

Com a API rodando:

```powershell
.\POPULAR.ps1
```

O script mostra um resumo final com:
- pacientes;
- pacientes com insights;
- quantidade de insights;
- pendências abertas;
- notificações ativas;
- notificações não lidas.

## Novo endpoint de diagnóstico de dados

`GET /api/dados/resumo`

Retorna contagens da organização atual:
- profissionais;
- pacientes;
- consultas;
- avaliações;
- exames;
- metas;
- diário;
- planos de treino;
- execuções;
- pendências;
- notificações.

## Banco

**Sem schema/migration nova.**

A v0.3.27 apenas usa as estruturas já existentes.

`PREPARAR.ps1` continua com 15 etapas.

Smoke test: **104 etapas**.


# v0.3.27 — Notificações Internas + Lembretes de Acompanhamento

A v0.3.27 adiciona uma central de notificações persistente para profissional e paciente.

## Profissional

O sino de notificações alerta sobre:

### Agenda
- consultas nas próximas 24 horas;
- prioridade maior quando faltam até 2 horas.

### Pendências
- pendência vencida;
- pendência de prioridade alta;
- pendência com vencimento nas próximas 24 horas.

As notificações apontam para:
- Agenda;
- Pendências.

## Paciente

O portal do paciente recebe sino próprio.

São gerados lembretes para:
- consultas nas próximas 24 horas;
- prioridade maior quando a consulta estiver muito próxima.

## Central de notificações

- contador de não lidas;
- drawer lateral;
- marcar uma notificação como lida ao abrir;
- marcar todas como lidas;
- atualização automática a cada 60 segundos;
- clique direciona para a área correspondente.

## Persistência e idempotência

Nova tabela:
- `NotificacoesInternas`.

Cada notificação gerada automaticamente possui uma `OrigemChave` única por organização + usuário.

Isso evita notificações duplicadas ao sincronizar repetidamente.

Quando a consulta deixa de ser futura ou a pendência deixa de estar ativa/relevante, a notificação automática correspondente é desativada.

Se uma notificação já lida mudar de prioridade/conteúdo, ela volta a aparecer como não lida.

## Banco

Upgrade idempotente:
`scripts/sql/v0.3.27_notificacoes.sql`

`PREPARAR.ps1`: **15 etapas**.

Smoke test: **98 etapas**.


# v0.3.27 — Pendências + Tratamento dos Insights

A v0.3.3 passou a detectar sinais automáticos. A v0.3.27 fecha o ciclo operacional: o profissional pode transformar um sinal em uma pendência persistente e acompanhar sua resolução.

## Nova entidade: PendenciaClinica

A pendência guarda:
- paciente;
- profissional responsável;
- código de origem do insight;
- categoria;
- severidade;
- título e descrição;
- valor de referência;
- ação sugerida;
- status;
- prazo;
- data em que foi vista;
- adiamento;
- resolução;
- consulta de retorno vinculada.

## Ciclo de vida

Status disponíveis:
- Nova;
- Vista;
- Adiada;
- Resolvida.

Ações:
- marcar como vista;
- adiar para uma data futura;
- resolver com anotação;
- agendar retorno diretamente da pendência.

## Insight -> Pendência

No prontuário, cada insight possui **+ Criar pendência**.

A criação é idempotente por paciente + `OrigemCodigo` enquanto a pendência anterior estiver aberta, evitando duplicação acidental do mesmo sinal.

## Agenda

A ação **Agendar retorno**:
- cria uma `Consulta`;
- usa status `Agendada`;
- vincula a consulta em `ConsultaRetornoId`;
- mantém auditoria.

## Nova tela profissional: Pendências

Filtros:
- Abertas;
- Adiadas;
- Resolvidas;
- Todas.

A tela permite abrir o prontuário e executar todo o ciclo de tratamento.

O dashboard também mostra um resumo das pendências abertas.

## Banco

Nova tabela:
- `PendenciasClinicas`.

Upgrade idempotente:
`scripts/sql/v0.3.27_pendencias.sql`

`PREPARAR.ps1`: **14 etapas**.

Smoke test: **88 etapas**.


# v0.3.27 — Alertas Clínicos + Insights Automáticos

O sistema passa a analisar os dados já registrados e gerar sinais automáticos de acompanhamento.

## Regras iniciais

### Exames
- resultado numérico da coleta mais recente fora da faixa mínima/máxima registrada;
- o sistema usa exclusivamente a referência salva junto ao resultado;
- o insight é um sinal de revisão, não um diagnóstico.

### Evolução corporal
- comparação das duas avaliações mais recentes com peso;
- variação a partir de 3% gera sinal;
- variação a partir de 7% recebe prioridade maior;
- a interpretação depende do objetivo e do contexto clínico.

### Retorno
- última consulta realizada há 60+ dias;
- nenhum novo atendimento futuro registrado.

### Metas
- cobertura de registros inferior a 50% nos últimos 14 dias;
- expectativa calculada conforme frequência diária, semanal ou mensal.

### Treinos
- plano ativo sem execução registrada nos últimos 14 dias;
- queda relevante em comparação às duas semanas anteriores.

## Dashboard profissional

Nova **Central de atenção**:
- quantidade total de sinais;
- prioridade alta/média/baixa;
- pacientes ordenados pela maior prioridade;
- clique abre diretamente o prontuário.

## Prontuário

A aba **Resumo** recebe:
- painel de insights do paciente;
- categoria;
- prioridade;
- descrição;
- valor de referência;
- ação sugerida.

## Segurança clínica

Os cards apresentam aviso explícito:

> Os insights são sinais automáticos baseados nos registros do sistema. Eles não representam diagnóstico, prescrição ou avaliação de urgência e devem ser interpretados pelo profissional no contexto clínico.

## Banco

**Sem migration nova.**

Os cálculos utilizam consultas, avaliações, exames, metas e execuções de treino já armazenados.

Smoke test: **78 etapas**.


# v0.3.27 — Gráficos de Evolução + Painel Analítico

Esta versão transforma os históricos já existentes em visualizações de tendência.

## Prontuário profissional

### Resumo / Avaliações
Gráficos para:
- peso;
- IMC;
- percentual de gordura;
- cintura.

### Exames
- agrupa resultados pelo marcador;
- considera resultados numéricos;
- mostra gráfico somente quando existem pelo menos duas coletas;
- preserva a unidade registrada no exame.

### Treinos
- progressão de carga por exercício;
- utiliza as cargas realmente registradas pelo paciente;
- mantém os indicadores de adesão da v0.3.1.

## Portal do paciente

### Evolução
- cards atuais;
- gráficos de peso, IMC, gordura e cintura;
- tabela histórica continua disponível.

### Exames
- tendência de marcadores laboratoriais numéricos;
- coletas e referências continuam visíveis abaixo.

### Treino
- gráfico de progressão das cargas realmente utilizadas;
- ficha e histórico de execuções permanecem disponíveis.

## Tecnologia

Os gráficos são produzidos em **SVG nativo** no `app.js`.

Não foi adicionada biblioteca JavaScript externa, npm, Node ou CDN.
A versão funciona no mesmo modelo atual de SPA servida pelo ASP.NET Core.

## Banco

**Sem migration/schema novo.**

A v0.3.27 reutiliza:
- Avaliacoes;
- ExamesLaboratoriais / ResultadosExamesLaboratoriais;
- ExecucoesTreino / ExecucoesItensTreino.

`PREPARAR.ps1` continua com 13 etapas.

Smoke test: **70 etapas**.


# v0.3.27 — Execução de Treinos + Progressão de Carga

O módulo de treino deixa de ser apenas uma ficha prescrita e passa a registrar a execução real do paciente.

## Paciente
Na seção **Treino**:
- botão `Registrar treino`;
- duração do treino;
- esforço percebido (RPE 0–10);
- séries realmente feitas;
- repetições realizadas;
- carga utilizada;
- RPE por exercício;
- marcação de exercício concluído;
- observações;
- histórico recente dos treinos concluídos.

## Profissional
Na aba **Treinos** do prontuário:
- total de treinos nos últimos 90 dias;
- minutos totais;
- esforço médio;
- exercícios com carga registrada;
- última carga;
- maior carga;
- histórico recente das execuções.

## Banco
Novas tabelas:
- `ExecucoesTreino`;
- `ExecucoesItensTreino`.

Upgrade:
`scripts/sql/v0.3.27_execucoes_treino.sql`

O `PREPARAR.ps1` agora possui **13 etapas**.

## Segurança
O registro de execução do paciente usa `PatientOnly` e resolve o paciente pelo JWT.
O paciente só pode registrar itens pertencentes à própria sessão ativa.

Smoke test: **62 etapas**.


# v0.3.27 — Treinos + Biblioteca de Exercícios

Primeira versão do módulo de treinamento físico.

## Catálogo de exercícios
- nome;
- grupo muscular;
- equipamento;
- descrição;
- link de vídeo;
- ativar/inativar;
- catálogo separado por organização;
- seis exercícios iniciais são criados de forma idempotente para organizações existentes.

## Plano de treino
Cada paciente pode ter planos contendo:
- nome e objetivo;
- vigência;
- status;
- orientações;
- múltiplos treinos/dias;
- dias da semana;
- múltiplos exercícios;
- ordem;
- séries;
- repetições;
- carga e unidade;
- descanso;
- tempo de execução;
- observações.

## Interface profissional
- nova aba **Treinos** no prontuário;
- novo item **Plano de treino** em `+ Registrar`;
- construtor visual da ficha;
- criação rápida de exercício no catálogo.

## Portal do paciente
Nova seção **Treino** com:
- plano ativo;
- profissional responsável;
- sessões/dias;
- prescrição completa;
- grupo muscular/equipamento;
- botão de vídeo quando houver URL.

O endpoint do paciente é:
`GET /api/portal/me/treino`

Ele usa exclusivamente o paciente vinculado ao JWT.

## Banco
Upgrade idempotente:
`scripts/sql/v0.3.27_treinos.sql`

O `PREPARAR.ps1` agora possui **12 etapas** e aplica o upgrade automaticamente.

Smoke test: **54 etapas**.


# v0.3.27 — Portal do Paciente Completo

A home do paciente evolui para um portal navegável com dados históricos próprios.

## Navegação do paciente
- Início;
- Plano;
- Metas;
- Diário;
- Evolução;
- Exames.

## Novos endpoints protegidos por `PatientOnly`
- `GET /api/portal/me/plano`
- `GET /api/portal/me/metas`
- `GET /api/portal/me/diario`
- `GET /api/portal/me/evolucao`
- `GET /api/portal/me/exames`

Todos resolvem o paciente exclusivamente pelo `UsuarioId` e `OrganizacaoId` presentes no JWT.

## Plano alimentar
- refeições;
- horários;
- alimentos;
- quantidades;
- substituições;
- macros de cada item;
- totais nutricionais do plano.

## Metas e diário
- histórico de 30 dias;
- progresso;
- registros concluídos;
- atualização da meta pelo próprio paciente;
- novo registro de diário pelo portal.

## Evolução e exames
- histórico cronológico das avaliações corporais;
- IMC calculado;
- peso, gordura, cintura e pressão;
- coletas laboratoriais;
- resultados;
- referências;
- classificação por faixa.

Sem migration nova.

Smoke test: **46 etapas**.


# v0.3.27 — Acesso do Paciente + Portal Separado

Principais mudanças:

- criação/renovação/revogação do acesso do paciente pelo prontuário;
- usuário `Paciente` vinculado ao `Paciente.UsuarioId`;
- ativação por convite e definição da própria senha;
- JWT de paciente com política de autorização separada;
- endpoints profissionais continuam inacessíveis para o papel `Paciente`;
- `GET /api/portal/me/home` resolve o paciente pelo token, sem `pacienteId` arbitrário;
- paciente pode registrar o próprio diário;
- paciente pode atualizar o progresso das próprias metas;
- interface própria, responsiva e diferente do painel profissional;
- sem migration nova: o campo `Paciente.UsuarioId` já existia desde o baseline;
- smoke test ampliado para 40 etapas.

## Fluxo local

Profissional:
1. Entre no sistema.
2. Abra um paciente.
3. Clique em **Acesso do paciente**.
4. Gere o convite e copie o link.

Paciente:
1. Abra o link.
2. Defina a senha.
3. Faça login com o e-mail do convite.
4. O sistema identifica `TipoUsuario = Paciente` e abre o portal dedicado.


# v0.3.27 — Correção do smoke test administrativo

O teste 32 agora valida as rotas reais usadas pela interface:
- `/api/alimentos/`;
- `/api/exames/marcadores/`;
- `/api/anamnese/perguntas/`;
- `/api/configuracoes/organizacao`;
- edição do profissional.

Nenhuma alteração de banco ou migration.


# v0.3.27

Correção: o endpoint `/api/health` agora reporta corretamente a versão `0.3.27`.


## v0.3.27 — Catálogos + Configurações

Nesta versão a administração dos catálogos passa a existir na própria interface web.

- nova área **Configurações**;
- resumo da organização, usuário e profissional logado;
- gestão visual do catálogo de alimentos;
- criação de marcadores laboratoriais;
- criação de perguntas personalizadas da anamnese;
- sem migration nova;
- smoke test ampliado para 30 etapas.

Fluxo:

```powershell
.\PREPARAR.ps1
.\RODAR.ps1
.\TESTAR.ps1
```

# HealthPlatform v0.3.27 — Edição Clínica + Agenda Operacional

Evolução da v0.2.4 validada. Esta versão torna o prontuário e a agenda mais operacionais diretamente no navegador.

## Novidades
- edição visual de consultas existentes;
- edição visual de anamneses existentes;
- edição visual de avaliações corporais existentes;
- novo `PUT /api/avaliacoes/{id}` com auditoria;
- seletor de registro quando houver múltiplos itens no prontuário;
- agenda com ações rápidas: Confirmar, Realizada, Falta e Cancelar;
- reagendamento visual com data/hora local e offset do navegador;
- auditoria já existente para atualização/status/reagendamento preservada;
- sem migration nova.

## Execução
```powershell
.\PREPARAR.ps1
.\RODAR.ps1
```
Em outro PowerShell:
```powershell
.\TESTAR.ps1
```
Acesse `http://localhost:5180`.
