# Informe del proyecto — Gestión de Almacén

_Última actualización: 20-oct-2026_

## 1. Qué es

Aplicación para gestionar el almacén con dos flujos de trabajo:

1. **Verificación de recepción** — dada una orden de carga, se comprueba línea a línea
   (OK / Mal), con observaciones por línea y final, y **doble firma** (supervisor y
   transportista).
2. **Preparación de pedido (picking)** — dada una orden, se prepara escaneando **códigos
   de barras**; cada escaneo válido marca cargado, avisa si el código no pertenece al
   pedido, controla el progreso por línea, admite observaciones y firma del preparador.
   Se va guardando en base de datos conforme se carga.

## 2. Arquitectura

| Capa       | Tecnología                        | Proyecto               |
|------------|-----------------------------------|------------------------|
| Cliente    | Blazor WebAssembly (PWA)          | `src/Warehouse.Client` |
| Servidor   | ASP.NET Core Minimal API + Dapper | `src/Warehouse.Api`    |
| Compartido | DTOs y modelos de dominio         | `src/Warehouse.Shared` |
| Datos      | PostgreSQL 16                     | `src/Warehouse.Api/Migrations` |

- .NET 8. Migrador propio (sin dependencias): aplica los `.sql` de `Migrations` que no
  estén en la tabla `schema_migrations`.
- Firma por `<canvas>` + JS interop; PNG en base64.

## 3. Estado actual

| Área | Estado |
|------|--------|
| Esqueleto de la app (API + cliente + dominio) | ✅ Completo, compila (0 errores) |
| Módulo verificación de recepción | ✅ Implementado |
| Módulo preparación de pedido (picking) | ✅ Implementado |
| Base de datos (esquema + seed + scripts) | ✅ Lista; ver sección 5 |
| Adaptador del origen externo (web service) | ✅ Montado y probado, **dormido** (usa mock hasta configurar la API) |
| **API origen real** | ⏳ **Pendiente**: aún no existe la API; falta URL, auth y respuesta real |
| Despliegue en producción | ⏳ Pendiente |

## 4. Origen de datos externo (web service)

Las órdenes de carga vienen de **otro programa** vía **web service (HTTP/JSON)**.
Encapsulado tras `IExternalOrderSource`, con dos implementaciones:

- `HttpExternalOrderSource` — adaptador real, configurable por la sección `ExternalSource`
  de `appsettings.json`. Probado (deserialización, mapeo y 404→null).
- `MockExternalOrderSource` — datos de ejemplo en memoria, activo por defecto.

Selección automática: si `ExternalSource:BaseUrl` tiene valor se usa el adaptador HTTP;
si está vacío, el mock.

📄 Contrato que debe cumplir la API (endpoints, campos, tipos, auth, ejemplos):
**[`API-CONTRACT.md`](API-CONTRACT.md)**.

### 4.1. Pasos para activarla cuando exista la API

1. **Rellenar `ExternalSource`** en `src/Warehouse.Api/appsettings.json`:
   ```jsonc
   "ExternalSource": {
     "BaseUrl": "https://tu-api.ejemplo/api",   // ← interruptor: con valor usa la API
     "ListPath": "orders",
     "OrderPath": "orders/{ref}",
     "TimeoutSeconds": 30,
     "Auth": {
       "Type": "ApiKey",            // None | ApiKey | Bearer
       "HeaderName": "X-Api-Key",   // solo si Type=ApiKey
       "Value": "LA_CLAVE_O_TOKEN"  // clave (ApiKey) o token (Bearer)
     }
   }
   ```
   > No subir la clave a Git: en producción usar variable de entorno
   > (`ExternalSource__Auth__Value=...`) o el `appsettings` del servidor.

2. **Solo si los nombres de campo del JSON NO coinciden** con el contrato: en
   `src/Warehouse.Api/External/HttpExternalOrderSource.cs`, DTOs `Api*` (marcados con ⚠),
   añadir a cada propiedad que difiera su nombre real:
   ```csharp
   [System.Text.Json.Serialization.JsonPropertyName("numero_pedido")]
   public string Ref { get; set; } = "";
   ```
   Si coinciden (aunque cambie mayús/minús), no hay que tocar nada.

3. **Reiniciar la API** (lee la config al arrancar):
   `dotnet run --project src/Warehouse.Api`

4. **Comprobar:** `GET /api/orders` debe devolver las órdenes de la API real (no las de
   ejemplo); importar una con `POST /api/orders/import/{ref}` y verificar que trae las
   líneas con su `barcode`. Los errores quedan en el log de la API.

5. **Marcha atrás:** vaciar `BaseUrl` (`""`) y reiniciar → vuelve al mock, sin tocar código.

> Lo normal es **solo el paso 1 + reiniciar**. El paso 2 únicamente si los nombres difieren.

## 5. Base de datos

- Desarrollo: Postgres 16 en Docker (`docker compose up -d`), en `localhost:5433`
  (`warehouse` / `warehouse`).
- **Crear desde cero en otra máquina (sin Docker):** `db/create-database.sql`
  (rol + BD + esquema + datos de ejemplo, idempotente):
  `psql -U postgres -f db/create-database.sql`.
- **Llevar los datos actuales:** snapshot `db/warehouse_export.sql` + `db/import-db.sh`.
- Datos de ejemplo: `db/seed.sql` (6 órdenes con códigos EAN-13 reales; idempotente).

## 6. Pendiente

- **Conseguir y enchufar la API real** (sección 4.1): URL, autenticación y una respuesta
  de ejemplo del listado y de una orden con líneas.
- **Despliegue** en el servidor de destino.
