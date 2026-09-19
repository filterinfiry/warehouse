# Base de datos — export / import

BD de desarrollo: Postgres 16 en Docker (`docker compose up -d`), en `localhost:5433`
(`warehouse` / `warehouse`). Los scripts operan **a través del contenedor** para no
depender de la versión del cliente psql del host.

## Ficheros
- `001_...` (en `../src/Warehouse.Api/Migrations`) — esquema, aplicado por el migrador de la API.
- `seed.sql` — datos de ejemplo iniciales.
- `warehouse_export.sql` — **snapshot exportado** (esquema + datos) para llevar la BD a otra máquina.
- `export-db.sh` / `import-db.sh` — generar y restaurar ese snapshot.

## Exportar (desde esta máquina)
```bash
db/export-db.sh          # regenera db/warehouse_export.sql desde la BD local
```

## Importar en otra base de datos
```bash
# 1) en la otra máquina: clona el repo y levanta la BD
docker compose up -d

# 2a) restaurar en el contenedor local (warehouse-db)
db/import-db.sh

# 2b) restaurar en una BD remota / gestionada (Neon, RDS, otro server...)
db/import-db.sh postgresql://usuario:password@host:5432/basededatos
```

El dump se genera con `--clean --if-exists`, así que se puede reimportar encima de
una BD ya existente sin borrarla a mano. La API ya apunta a `localhost:5433/warehouse`
(`../src/Warehouse.Api/appsettings.json`), así que arranca contra la BD restaurada sin tocar nada.
