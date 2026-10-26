#!/usr/bin/env bash
# Deploy de producao executado NA VPS (chamado pelo workflow "Deploy producao").
# Uso: remote-deploy.sh <pacote.tar.gz> <versao> <banco> <servico-app> <migrations.txt>
# Fluxo: confere migrations -> backup validado do Postgres -> nova pasta da release
# (preservando compose.production.yml, .env, persistent e SQLs manuais) -> build -> troca
# -> healthcheck com a versao esperada. Se a saude falhar, volta a pasta anterior sozinho.
set -euo pipefail

PACOTE="$1"; VERSAO="$2"; BANCO="$3"; SERVICO="$4"; MIGRATIONS="$5"
ATIVO=/opt/healthplatform
BACKUPS=/opt/healthplatform-backups
COMPOSE=compose.production.yml
TS=$(date -u +%Y%m%d-%H%M%S)
NOVO="/opt/healthplatform-release-$VERSAO-$TS"
ANTERIOR="/opt/healthplatform-prev-$TS"

log() { echo "[deploy $(date -u +%H:%M:%S)] $*"; }
falha() { echo "::error::$*"; exit 1; }

cd "$ATIVO"
[ -f "$COMPOSE" ] || falha "$ATIVO/$COMPOSE nao encontrado."
dc() { docker compose -f "$ATIVO/$COMPOSE" "$@"; }
psqlq() { dc exec -T postgres psql -U healthplatform -d "$BANCO" -Atc "$1"; }

log "Versao no ar: $(curl -fsS localhost:10000/api/health || echo indisponivel)"

# 1. Producao nao aplica migrations sozinha: so seguimos se o banco ja tem todas as do pacote.
APLICADAS=$(psqlq 'select "MigrationId" from "__EFMigrationsHistory"' 2>/dev/null || true)
FALTANDO=$(grep -vxF -f <(printf '%s\n' "$APLICADAS") "$MIGRATIONS" || true)
if [ -n "$FALTANDO" ]; then
  falha "Banco sem as migrations: $(echo $FALTANDO). Nada foi alterado; aplique-as antes do deploy."
fi
log "Migrations conferidas: banco em dia com o pacote."

# 2. Backup validado antes de qualquer mudanca.
mkdir -p "$BACKUPS"
DUMP="$BACKUPS/deploy-v$VERSAO-$TS.dump"
dc exec -T postgres pg_dump -Fc -U healthplatform -d "$BANCO" > "$DUMP"
ENTRADAS=$(dc exec -T postgres pg_restore --list < "$DUMP" | grep -c ';' || true)
[ "$ENTRADAS" -gt 10 ] || falha "Backup invalido ($ENTRADAS entradas). Nada foi alterado."
log "Backup ok: $DUMP ($(du -h "$DUMP" | cut -f1), $ENTRADAS entradas)."
ls -1t "$BACKUPS"/deploy-*.dump 2>/dev/null | tail -n +8 | xargs -r rm -f

# 3. Monta a release em pasta nova, com a configuracao de producao preservada.
mkdir -p "$NOVO"
tar -xzf "$PACOTE" -C "$NOVO"
for f in "$COMPOSE" .env migrate-*.sql; do
  [ -e "$ATIVO/$f" ] && cp -a "$ATIVO/$f" "$NOVO/"
done
[ -f "$NOVO/VERSION.txt" ] && grep -qx "$VERSAO" "$NOVO/VERSION.txt" || falha "Pacote nao corresponde a v$VERSAO."
chown -R --reference="$ATIVO" "$NOVO"

# Guarda a imagem em uso para o rollback ser so uma troca de tag, sem rebuild.
IMAGEM=$(docker inspect --format '{{.Config.Image}}' "$(dc ps -q "$SERVICO")")
docker tag "$IMAGEM" "$IMAGEM-rollback"

log "Build da imagem (o site segue no ar durante o build)..."
docker compose -f "$NOVO/$COMPOSE" --project-directory "$NOVO" build "$SERVICO"

# 4. Troca: a pasta ativa vira backup e a nova assume o mesmo caminho.
trocar_para() { # $1 = pasta que deve virar a ativa, $2 = para onde vai a atual
  mv "$ATIVO/persistent" "$1/persistent"
  mv "$ATIVO" "$2"
  mv "$1" "$ATIVO"
}
trocar_para "$NOVO" "$ANTERIOR"
log "Subindo v$VERSAO..."
dc up -d --no-deps "$SERVICO"

saudavel() {
  for _ in $(seq 1 30); do
    if curl -fsS localhost:10000/api/health 2>/dev/null | grep -q "\"version\":\"$VERSAO\""; then return 0; fi
    sleep 4
  done
  return 1
}

if saudavel; then
  log "v$VERSAO no ar e saudavel."
  ls -1dt /opt/healthplatform-prev-* 2>/dev/null | tail -n +4 | xargs -r rm -rf
  exit 0
fi

# 5. Rollback automatico do codigo (o banco nao muda neste fluxo, entao nao precisa restaurar).
echo "::error::v$VERSAO nao respondeu saudavel. Voltando a versao anterior."
dc logs --tail 60 "$SERVICO" || true
FALHA="/opt/healthplatform-failed-$VERSAO-$TS"
trocar_para "$ANTERIOR" "$FALHA"
docker tag "$IMAGEM-rollback" "$IMAGEM"
dc up -d --no-deps --no-build --force-recreate "$SERVICO"
for _ in $(seq 1 30); do curl -fsS localhost:10000/api/health >/dev/null 2>&1 && break; sleep 4; done
log "Versao restaurada: $(curl -fsS localhost:10000/api/health || echo indisponivel)"
exit 1
