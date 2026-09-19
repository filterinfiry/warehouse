#!/usr/bin/env bash
#
# Importa db/warehouse_export.sql en OTRA base de datos.
#
# Uso:
#   db/import-db.sh                                        # -> contenedor local (warehouse-db)
#   db/import-db.sh postgresql://user:pass@host:5432/db    # -> BD remota / otra máquina
#   TARGET_URL=postgresql://... db/import-db.sh            # (igual, por entorno)
#   db/import-db.sh <url> <fichero.sql>                    # dump alternativo
#
# Para destinos remotos usa la imagen postgres:16 como cliente desechable, así no
# depende de la versión del psql del host (el dump lleva directivas de pg16).
# El dump se generó con --clean --if-exists: es idempotente sobre una BD ya creada.
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Primer argumento: si parece URL es el destino; si no, es el fichero de dump.
TARGET_URL="${TARGET_URL:-}"
DUMP=""
if [[ "${1:-}" == postgres://* || "${1:-}" == postgresql://* ]]; then
  TARGET_URL="$1"
  DUMP="${2:-}"
else
  DUMP="${1:-}"
fi
DUMP="${DUMP:-$DIR/warehouse_export.sql}"

if [[ ! -f "$DUMP" ]]; then
  echo "ERROR: no existe el dump '$DUMP'. Genera uno con db/export-db.sh." >&2
  exit 1
fi

if [[ -n "$TARGET_URL" ]]; then
  echo "Importando '$DUMP' en destino remoto (cliente postgres:16 desechable)..."
  docker run --rm -i postgres:16 \
    psql "$TARGET_URL" -v ON_ERROR_STOP=1 < "$DUMP"
else
  CONTAINER="${WAREHOUSE_DB_CONTAINER:-warehouse-db}"
  DB="${POSTGRES_DB:-warehouse}"
  DB_USER="${POSTGRES_USER:-warehouse}"
  if ! docker ps --format '{{.Names}}' | grep -qx "$CONTAINER"; then
    echo "ERROR: el contenedor '$CONTAINER' no está corriendo. Levántalo con 'docker compose up -d'." >&2
    exit 1
  fi
  echo "Importando '$DUMP' en contenedor local '$CONTAINER' ($DB)..."
  docker exec -i "$CONTAINER" psql -U "$DB_USER" -d "$DB" -v ON_ERROR_STOP=1 < "$DUMP"
fi

echo "OK: importación completada."
