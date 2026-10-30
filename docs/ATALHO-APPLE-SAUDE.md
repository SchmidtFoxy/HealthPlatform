# Atalho "AESYN Saúde" (piloto Apple Watch / Apple Saúde)

O PWA não consegue ler o app Saúde do iPhone. Quem lê é um atalho do app **Atalhos** (Shortcuts), que já vem no iPhone. O atalho lê o resumo do dia anterior e envia para o Aesyn com o código pessoal do paciente.

O atalho é montado **uma vez só**, num iPhone da equipe. Depois ele é compartilhado por link do iCloud, e cada paciente instala pelo link e cola o próprio código.

## O que o servidor recebe

`POST https://aesyn.com.br/api/integracoes/apple-health/atalho`

- Cabeçalho `X-Aesyn-Token` com o código do paciente (ou o campo `token` no corpo).
- Corpo JSON, todos os campos opcionais (precisa de pelo menos um valor):

| Campo | O que é | Unidade |
|---|---|---|
| `data` | Dia dos dados (`aaaa-MM-dd`). Se faltar, vale "ontem". Aceita até 30 dias atrás. | |
| `passos` | Passos do dia | passos |
| `energiaAtiva` | Energia ativa do dia | kcal |
| `distanciaKm` | Distância caminhada + corrida | km |
| `minutosAtivos` | Minutos de exercício (anel verde) | min |
| `fcRepouso` | Frequência cardíaca em repouso | bpm |
| `hrv` | Variabilidade da FC (média do dia) | ms |
| `sonoMinutos` ou `sonoHoras` | Sono da noite | min ou h |

Os números podem ir como número ou texto ("8.234", "5,3", "58 bpm"). Reenviar o mesmo dia substitui os valores anteriores desse dia, então rodar o atalho duas vezes não duplica nada.

A resposta tem `mensagem`, por exemplo `Aesyn recebeu 29/10: passos: 8234 · FC repouso (bpm): 58`. Código inválido ou desconectado volta 401.

## Montar o atalho (uns 15 minutos, no iPhone)

Abra o app **Atalhos** › **+** e dê o nome **AESYN Saúde**. Adicione as ações nesta ordem:

1. **Texto**: escreva `COLE-O-CODIGO`. Depois ative em **Detalhes do atalho › Perguntas de importação** uma pergunta para esse texto: "Cole o código do Aesyn (Perfil › Apple Watch e Saúde)". Assim cada paciente cola o próprio código ao instalar.
2. **Definir variável**: `Token` = Texto.
3. **Data**: Data Atual. Depois **Ajustar data**: Subtrair 1 dia.
4. **Formatar data**: Personalizado `yyyy-MM-dd`. **Definir variável** `DiaTexto`.
5. **Obter datas da entrada** usando `DiaTexto`. **Definir variável** `InicioOntem` (ontem à 0h).
6. **Formatar data** da Data Atual com `yyyy-MM-dd`, depois **Obter datas da entrada**. **Definir variável** `InicioHoje`.
7. Para cada métrica que soma no dia (Passos, Energia Ativa, Distância de Caminhada e Corrida, Minutos de Exercício):
   - **Encontrar amostras de saúde** onde Tipo é *(a métrica)* e Data de Início está entre `InicioOntem` e `InicioHoje`. **Agrupar por: Dia** (isso evita contar iPhone e Watch em dobro).
   - **Calcular estatísticas**: Soma. **Definir variável** (`Passos`, `Energia`, `Distancia`, `Exercicio`).
8. FC de repouso: **Encontrar amostras de saúde** com Tipo Frequência Cardíaca em Repouso, mesmo intervalo, Ordenar por Data de Início (mais recente primeiro), Limite 1. **Definir variável** `FCRepouso`.
9. HRV: **Encontrar amostras de saúde** com Tipo Variabilidade da Frequência Cardíaca, mesmo intervalo. **Calcular estatísticas**: Média. **Definir variável** `HRV`.
10. **Obter conteúdo do URL**:
    - URL: `https://aesyn.com.br/api/integracoes/apple-health/atalho`
    - Método: **POST**
    - Cabeçalhos: `X-Aesyn-Token` = `Token`
    - Corpo da solicitação: **JSON**, com os campos `data` (Texto = `DiaTexto`), `passos`, `energiaAtiva`, `distanciaKm`, `minutosAtivos`, `fcRepouso`, `hrv` (tipo **Número**, cada um com a variável correspondente).
11. **Obter valor do dicionário** `mensagem` e **Mostrar notificação** com ele (ajuda a ver se deu certo).

Rode uma vez com o seu próprio código. O iPhone pede permissão para ler o Saúde: permita todos os tipos. A notificação deve mostrar "Aesyn recebeu …", e os números aparecem em Progresso › Sinais conectados.

O sono ficou de fora desta primeira versão porque a Análise do Sono vem em fases (núcleo, profundo, REM) e somar as durações no Atalhos é trabalhoso. O servidor já aceita `sonoMinutos`/`sonoHoras` para quando isso for montado.

## Compartilhar com os pacientes

No atalho: **Compartilhar › Copiar link do iCloud**. Coloque o link em `AppleHealth:AtalhoUrl` no `appsettings.json` (ou mande para o Claude colocar) e ele aparece no botão "Abrir o atalho no iPhone" do perfil do paciente.

## O que o paciente faz

1. No Aesyn: **Perfil › Apple Watch e Saúde › Conectar**, e copia o código.
2. Toca em "Abrir o atalho no iPhone", depois em "Adicionar atalho", e cola o código.
3. No app Atalhos: **Automação › Nova automação › Hora do Dia** (por exemplo 9h), **Executar imediatamente**, atalho **AESYN Saúde**.

## Limites conhecidos do piloto

- O iPhone protege os dados do Saúde com o aparelho bloqueado. Se a automação rodar com o celular bloqueado, a leitura pode vir vazia. Por isso a sugestão é um horário em que o paciente costuma usar o celular.
- Só envia o dia anterior. Se um dia falhar, ele não é reenviado sozinho (dá para rodar o atalho mudando o `data`).
- Desconectar no perfil invalida o código na hora. Gerar um novo código também invalida o anterior.
