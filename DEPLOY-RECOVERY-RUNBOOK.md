# AESYN Performance — Production Recovery Operator Runbook

## Escopo

Este runbook acompanha o fluxo seguro de produção da série v0.57.x. Ele não substitui os gates automatizados do `DEPLOY-PRODUCAO.ps1`.

## Regra absoluta

**Nenhuma migration ou substituição da aplicação pode ocorrer antes de o backup PostgreSQL estar concluído e validado.**

## Antes de aplicar

1. Confirmar `TargetVersion`, host e usuário.
2. Confirmar `PRODUCAO:<host>:<versão>`.
3. Confirmar `ATIVAR:<host>:<versão>`.
4. Confirmar `VALIDAR-MIGRATIONS:<host>:<versão>`.
5. Verificar backup com tamanho, SHA-256 e `pg_restore --list`.
6. Verificar staging sem `.git` e sem `.env*`.
7. Confirmar que migrations destrutivas continuam bloqueadas.

## Se uma migration falhar

1. Não promover a release staged.
2. Não executar restart da release alvo.
3. Ler o arquivo `migration-failure-v*.log`.
4. Conferir o backup registrado no diagnóstico.
5. Só autorizar restore com `RESTORE-BACKUP:<host>:<versão>` após confirmar que o backup pertence ao ciclo atual.

## Recovery autorizado

O script:
1. revalida o backup com `pg_restore --list`;
2. interrompe temporariamente o serviço da aplicação;
3. executa restore controlado;
4. revalida o PostgreSQL com `SELECT 1`;
5. religa o serviço;
6. executa healthcheck de recovery.

## Após recovery

Mesmo com recovery aprovado:
- a release alvo continua bloqueada;
- não reutilizar staging/ciclo falho;
- iniciar novo ciclo completo;
- gerar novo backup;
- refazer staging, safety gate, hash, migration execution e verificações.

## Evidências esperadas

O diretório `.deploy-logs/recovery-audit-v<versão>-<timestamp>/` deve conter:
- `SUMMARY.txt`;
- `BACKUP-EVIDENCE.txt`;
- `MIGRATION-EVIDENCE.txt`;
- `OPERATOR-NEXT-STEPS.txt`.

Nunca registrar conteúdo do `.env` ou credenciais nesses arquivos.
