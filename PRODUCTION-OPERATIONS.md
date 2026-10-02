# AESYN Performance — Production Operations Foundation

## Objetivo

A série v0.58.x começa com uma camada operacional sobre o pipeline seguro consolidado na v0.57.x.

## Estado operacional

Cada ciclo concluído gera:

- `.deploy-logs/operations/latest-production-state.json`
- `.deploy-logs/operations/production-deploy-history.jsonl`

O arquivo `latest-production-state.json` representa o estado operacional mais recente.

O arquivo `production-deploy-history.jsonl` mantém uma entrada por ciclo, permitindo histórico cronológico sem substituir registros anteriores.

## Sinais registrados

- versão alvo;
- modo `apply` ou `validate-only`;
- host alvo;
- backup validado e SHA-256;
- staging ativo;
- migration safety;
- hash de migrations;
- bloqueio destrutivo;
- promoção;
- saúde do runtime;
- verificação de versão;
- versão servida;
- rollback;
- recovery audit;
- closure gate;
- status operacional.

## Status

`healthy` exige runtime saudável e versão validada.

`validated` indica que o ciclo foi executado em modo de validação, sem ativação real.

## Segurança

A camada operacional não substitui nem enfraquece os gates da v0.57.x.

**Nenhuma migration ou substituição da aplicação pode ocorrer antes de o backup PostgreSQL estar concluído e validado.**

`DestructiveMigrationsAllowed=false` continua obrigatório.

Nenhum conteúdo de `.env`, senha ou segredo deve ser persistido nos arquivos operacionais.
