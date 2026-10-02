# AESYN Performance — Deploy Seguro v0.57.x Closure Checklist

## Objetivo

Formalizar o fechamento da série v0.57.x antes da evolução para v0.58.x.

## Gates obrigatórios

- [x] alvo e versão confirmados;
- [x] acesso remoto sem senha hardcoded;
- [x] backup PostgreSQL criado antes de qualquer mutação;
- [x] integridade do backup validada;
- [x] configuração de produção preservada;
- [x] pacote enviado para staging isolado;
- [x] pre-activation gates aprovados;
- [x] snapshot e rollback plan preparados;
- [x] migrations classificadas por safety gate;
- [x] migrations destrutivas bloqueadas;
- [x] hash do conjunto revalidado antes da execução;
- [x] migrations não destrutivas executadas somente quando `-Aplicar`;
- [x] falha de migration bloqueia promoção/restart;
- [x] recovery exige confirmação explícita;
- [x] promoção atômica reversível;
- [x] restart controlado do serviço;
- [x] healthcheck e versão servida verificadas;
- [x] rollback automático da aplicação;
- [x] audit bundle e operator runbook materializados;
- [x] closure evidence `END-TO-END-CLOSURE.txt`.

## Regra absoluta

**Nenhuma migration ou substituição da aplicação pode ocorrer antes de o backup PostgreSQL estar concluído e validado.**

## Resultado

A série v0.57.x somente é considerada fechada quando `Test-EndToEndClosureGate` retorna `Complete=true`.

O fechamento não autoriza migrations destrutivas. `DestructiveMigrationsAllowed=false` continua obrigatório.

## Próxima série

`v0.58.x` pode iniciar somente após aprovação funcional da v0.57.11.
