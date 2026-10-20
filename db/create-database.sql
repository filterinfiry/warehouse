-- =============================================================================
--  Crea la base de datos de "Gestión de Almacén" DESDE CERO en otra máquina.
--
--  Hace, de forma IDEMPOTENTE (se puede relanzar sin romper nada):
--    1) el rol de la aplicación   -> warehouse / warehouse
--    2) la base de datos          -> warehouse  (propiedad de ese rol)
--    3) el esquema                -> reutiliza ../src/Warehouse.Api/Migrations/001_init.sql
--    4) lo registra en schema_migrations (para que el migrador de la API no lo repita)
--    5) datos de ejemplo          -> reutiliza seed.sql  (opcional, ver abajo)
--
--  REQUISITOS: psql, y ejecutarlo como SUPERUSUARIO (p.ej. el rol 'postgres'),
--  porque crea rol y base de datos.
--
--  USO (desde la raíz del repo):
--    Linux/Mac:   psql -U postgres -f db/create-database.sql
--    Windows:     psql -U postgres -f db\create-database.sql
--    Docker:      docker exec -i warehouse-db psql -U warehouse -d postgres < db/create-database.sql
--
--  Para crearla VACÍA (sin datos de ejemplo), comenta la última línea (\ir seed.sql).
--
--  La API ya apunta a Host=localhost;Port=5433;Database=warehouse;Username=warehouse;
--  Password=warehouse (src/Warehouse.Api/appsettings.json). Si tu Postgres usa otro
--  puerto/credenciales, ajústalo ahí.
-- =============================================================================

-- 1) Rol de la aplicación (solo si no existe) ---------------------------------
SELECT 'CREATE ROLE warehouse LOGIN PASSWORD ''warehouse'''
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'warehouse')
\gexec

-- 2) Base de datos (solo si no existe). CREATE DATABASE no admite IF NOT EXISTS,
--    por eso se genera con \gexec condicionado. --------------------------------
SELECT 'CREATE DATABASE warehouse OWNER warehouse'
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'warehouse')
\gexec

-- 3) A partir de aquí trabajamos DENTRO de la base de datos warehouse ---------
\connect warehouse

-- Los objetos se crean como propiedad del rol de la aplicación, de modo que el
-- login 'warehouse' tenga todos los permisos sin grants adicionales.
SET ROLE warehouse;

-- 4) Esquema (idempotente: usa create table if not exists) --------------------
\echo '>> Aplicando esquema (001_init.sql)...'
\ir ../src/Warehouse.Api/Migrations/001_init.sql

-- Registrar la migración para que el migrador de la API no la vuelva a aplicar.
CREATE TABLE IF NOT EXISTS schema_migrations (
    id         text PRIMARY KEY,
    applied_at timestamptz NOT NULL DEFAULT now()
);
INSERT INTO schema_migrations (id) VALUES ('001_init.sql')
ON CONFLICT (id) DO NOTHING;

-- 5) Datos de ejemplo (opcional). Comenta la línea siguiente para dejarla vacía.
\echo '>> Cargando datos de ejemplo (seed.sql)...'
\ir seed.sql

RESET ROLE;
\echo '>> Base de datos "warehouse" lista.'
