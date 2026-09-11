# HealthPlatform v0.7.7 — Plano de Recuperação Contextual

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
