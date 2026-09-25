# AESYN Performance — Product Flow Gate v0.19.44

Este documento fecha a **Lista 03** como gate de produto. Ele não substitui testes clínicos, validação jurídica nem homologação em produção.

## Regra do gate

O fluxo precisa ser executável pela interface normal do AESYN, sem Swagger, SQL manual, alteração direta do banco, atalhos de desenvolvedor ou chamadas feitas à mão.

O `TESTAR.ps1` valida a cobertura estrutural desses fluxos sem criar ou alterar registros. Antes de um deploy comercial, execute também a homologação manual abaixo em ambiente controlado com dados de teste.

---

## Fluxo obrigatório — paciente

1. **Primeiro acesso e tutorial**
   - Entrar pelo login normal.
   - Receber a trilha de primeiro acesso quando ainda não concluída.
   - Conseguir adiar e refazer o tutorial depois.

2. **Check-in diário**
   - Abrir o check-in pela Home.
   - Registrar sono, energia, dor, disposição e recuperação.
   - Ver o contexto atualizado na experiência diária.

3. **Plano e treino**
   - Visualizar os planos de treino ativos.
   - Ter zero, um ou vários planos sem quebrar a experiência.
   - Escolher uma sessão publicada e iniciar pela própria interface.
   - Concluir a execução e preservar histórico.

4. **Alimentação**
   - Visualizar o plano alimentar atual.
   - Registrar execução/adesão da alimentação pelo fluxo disponível.
   - Usar substituições quando liberadas pelo plano.

5. **Comunicação**
   - Abrir o chat com o profissional.
   - Enviar mensagem sem perder o texto em falha transitória.
   - Ver mensagens e contexto associados ao acompanhamento.

6. **Arquivos**
   - Enviar arquivo pelo celular ou navegador.
   - Abrir a biblioteca protegida.
   - Referenciar um arquivo diretamente no chat.

7. **Atualizações e notificações**
   - Receber atualização dentro da Central de Notificações.
   - Quando Web Push estiver configurado e autorizado, receber push fora do app.
   - Conseguir navegar da notificação para a origem relevante.

---

## Fluxo obrigatório — profissional

1. **Login e localização do paciente**
   - Entrar pelo acesso profissional.
   - Encontrar o paciente sem usar identificadores técnicos.

2. **Revisão de contexto**
   - Abrir perfil integrado.
   - Consultar prontidão, treino, nutrição, exames, arquivos, timeline e sinais de atenção.

3. **Treino**
   - Criar e editar plano.
   - Manter múltiplos planos quando necessário.
   - Duplicar, arquivar, reativar e desvincular sem destruir histórico.
   - Publicar/ativar para o paciente.

4. **Nutrição**
   - Criar e editar plano alimentar.
   - Manter refeições, alimentos, metas e equivalências.
   - Duplicar/progredir e arquivar preservando histórico.
   - Revisar e publicar.

5. **Comunicação e documentos**
   - Conversar com o paciente.
   - Abrir arquivos e exames no próprio perfil.
   - Referenciar arquivo no chat.

6. **Acompanhamento**
   - Receber alertas/notificações.
   - Consultar timeline integrada.
   - Navegar para evolução e dados relevantes sem depender de ferramentas técnicas.

---

## Homologação manual antes de produção comercial

Use contas e dados de teste. Marque cada item somente depois de executar o fluxo real no navegador/PWA.

### Paciente
- [ ] Primeiro acesso em celular.
- [ ] Tutorial completo e opção de refazer.
- [ ] Check-in registrado pela interface.
- [ ] Treino publicado visível e sessão iniciada/concluída.
- [ ] Plano alimentar visualizado e registro de adesão realizado.
- [ ] Chat enviado/recebido.
- [ ] Upload de PDF e foto pelo celular.
- [ ] Arquivo referenciado no chat.
- [ ] Central interna recebendo atualização.
- [ ] Push real testado com app fechado, quando VAPID estiver configurado.

### Profissional
- [ ] Localizar paciente por UI.
- [ ] Revisar contexto longitudinal.
- [ ] Criar/editar/publicar treino.
- [ ] Criar/editar/publicar dieta.
- [ ] Chat profissional funcionando.
- [ ] Abrir arquivos/exames.
- [ ] Receber alerta de atividade do paciente.
- [ ] Consultar evolução/timeline.

## Critério de encerramento

A Lista 03 é considerada tecnicamente fechada quando:

- `PREPARAR.ps1` passa;
- aplicação inicia por `RODAR.ps1`;
- `TESTAR.ps1` passa integralmente;
- nenhum passo obrigatório depende de Swagger/SQL/manual técnico;
- pendências específicas de produção ficam registradas e não são confundidas com validação local.
