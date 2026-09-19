#!/usr/bin/env bash
#
# Exporta la BD de almacén a db/warehouse_export.sql (esquema + datos, portable).
#
# Uso:
#   db/export-db.sh
#
# Usa el pg_dump del contenedor (Postgres 16), así no depende de la versión del
# cliente psql/pg_dump del host (el server es 16 y el host puede tener otro).
# Config opcional por entorno: WAREHOUSE_DB_CONTAINER, POSTGRES_DB, POSTGRES_USER.
set -euo pipefail

CONTAINER="${WAREHOUSE_DB_CONTAINER:-warehouse-db}"
DB="${POSTGRES_DB:-warehouse}"
DB_USER="${POSTGRES_USER:-warehouse}"

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT="$DIR/warehouse_export.sql"

if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; then
  echo "ERROR: el contenedor '$CONTAINER' no está corriendo. Levántalo con 'docker compose up -d'." >&2
  exit 1
fi

echo "Exportando '$DB' desde '$CONTAINER' -> $OUT"
docker exec "$CONTAINER" pg_dump -U "$DB_USER" -d "$DB" \
  --no-owner --no-privileges --clean --if-exists > "$OUT"

echo "OK: $(wc -l < "$OUT") líneas, $(du -h "$OUT" | cut -f1)."
