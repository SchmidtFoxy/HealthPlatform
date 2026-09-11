# HealthPlatform v0.8.1 — Metas do Ciclo & Progresso por Objetivo

A v0.8.1 transforma as metas já configuradas no ciclo esportivo em acompanhamento objetivo e auditável, sem criar score único e sem alterar automaticamente a prescrição profissional.

## Destaques da v0.8.1

- progresso semanal de treinos contra a meta do ciclo;
- consistência atual comparada à meta profissional;
- acompanhamento de peso-alvo com suporte a ganho ou perda de peso;
- objetivo textual do ciclo preservado como orientação, sem score artificial;
- mesma leitura para atleta e profissional;
- sem migration: PREPARAR permanece 37/37;
- suíte ampliada para 768 verificações.

# HealthPlatform v0.8.0 — Painel de Evolução Esportiva

A linha 0.8.x começa consolidando a experiência esportiva em uma leitura longitudinal única, sem reduzir saúde ou performance a um score composto. O painel apresenta consistência, recuperação, carga, performance, nutrição, hidratação e ciclo com suas próprias evidências.

## Destaques da v0.8.0

- novo `EvolucaoEsportivaService`;
- síntese transparente de múltiplas dimensões do ciclo;
- estados `Evoluindo`, `Estavel`, `Observar` e `DadosInsuficientes`;
- mesma leitura-base para atleta e profissional;
- nenhuma alteração automática de treino/nutrição;
- sem migration: dados são derivados dos módulos existentes;
- PREPARAR permanece 37/37;
- suíte ampliada para 760 verificações.

# HealthPlatform v0.7.9 — Hidratação Contextual & Balanço do Dia

A linha esportiva agora contextualiza a hidratação do dia a partir da meta profissional, consumo registrado e treino, sem redefinir necessidade hídrica automaticamente.

# HealthPlatform v0.7.8 — Adesão Nutricional Contextual

> A v0.7.8 transforma o plano alimentar ativo em acompanhamento diário de execução: cada refeição pode ser registrada como realizada, adaptada ou não realizada, sem alterar automaticamente calorias, macros ou prescrição.

## Destaques da v0.7.8

- acompanhamento das refeições planejadas do dia;
- status Realizada / Adaptada / Não realizada;
- adequação calculada apenas sobre refeições já registradas;
- linguagem não punitiva: foco em padrão sustentável, não perfeição;
- integração com Coach Diário;
- visão equivalente para profissional;
- sem migration: registros reutilizam o Diário do paciente;
- PREPARAR permanece 37/37.


> A v0.7.7 transforma prontidão, dor localizada, tendência de recuperação, carga de treino e execução do dia em prioridades claras de recuperação, sem diagnóstico automático e sem alterar a prescrição profissional.

## Destaques da v0.7.7

- novo `PlanoRecuperacaoService` derivado dos dados esportivos já existentes;
- estados `Protecao`, `Recuperacao` e `Equilibrio`;
- proteção de região corporal quando dor/impacto no treino estão altos;
- prioridade de recuperação quando prontidão/tendência pedem cautela;
- orientação de consolidação quando a carga exige revisão;
- atenção à média recente de sono;
- hidratação e encerramento do dia incorporados ao plano quando pertinentes;
- card específico na Home do atleta;
- leitura equivalente no prontuário profissional;
- mensagens explícitas de segurança: sem diagnóstico de lesão, sem prescrição de tratamento e sem alteração automática de treino/nutrição/medicação;
- seed da Ana Ribeiro v7 preservado e atualizado para o banner da v0.7.7;
- nenhuma migration nova: `PREPARAR` permanece `37/37`;
- suíte ampliada para **736 verificações**.

## Princípios

- medicina do esporte orientada a comportamento sustentável;
- a prescrição do profissional continua sendo a fonte de verdade;
- mais carga não é automaticamente melhor;
- dor e recuperação contextualizam a execução, não geram diagnóstico automático;
- gamificação nunca deve ser motivo para ignorar sinais do corpo.

## Validação local

```powershell
.\PREPARAR.ps1
.\RODAR.ps1
.\TESTAR.ps1
```

Alvo esperado: **736/736**.
