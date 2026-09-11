# v0.5.8 — Protocolos de Acompanhamento / Connected Care

A v0.5.8 transforma o monitoramento guiado em acompanhamento personalizado por paciente.

## O que entrou
- profissional configura quais métricas cada paciente deve acompanhar;
- frequência: diária, dias da semana, semanal ou sob demanda;
- horário, unidade e instruções personalizadas;
- portal do paciente passa a priorizar somente os registros definidos no protocolo quando houver configuração ativa;
- resumo do prontuário mostra o protocolo ativo junto ao monitoramento remoto;
- ativação/desativação auditada e isolada por organização;
- nova tabela `ProtocolosAcompanhamento`;
- PREPARAR passa para 32 etapas e possui migration incremental v0.5.8 para instalações existentes.

## Validação esperada
- `PREPARAR.ps1`: 32/32
- `TESTAR.ps1`: 580/580

Baseline anterior: v0.5.7 validada com 570/570 testes.
