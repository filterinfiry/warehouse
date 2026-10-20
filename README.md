# Gestión de Almacén

Aplicación de gestión de almacén con dos módulos:

1. **Verificación de recepción** — Dada una orden de carga, se comprueba que el pedido
   recibido es correcto. Se marca cada línea de detalle como OK / Mal, se pueden anotar
   observaciones por línea y una observación final, y se recogen dos firmas: la de la
   persona que supervisa y la de quien trae el pedido.

2. **Preparación de pedido** — Dada una orden de carga, se prepara el pedido trabajando
   con **códigos de barras**. Conforme se cargan los productos se escanean y se van dando
   por cargados; si se escanea algo que no pertenece al pedido, avisa. Al terminar se firma
   y se cierra. Se pueden anotar observaciones y todo se va guardando en base de datos.

## Arquitectura

| Capa       | Tecnología                          | Proyecto              |
|------------|-------------------------------------|-----------------------|
| Cliente    | Blazor WebAssembly (PWA)            | `src/Warehouse.Client`|
| Servidor   | ASP.NET Core Minimal API + Dapper   | `src/Warehouse.Api`   |
| Compartido | DTOs y modelos de dominio           | `src/Warehouse.Shared`|
| Datos      | PostgreSQL                          | migraciones en `src/Warehouse.Api/Migrations` |

### Origen de datos externo (web service)

Las órdenes de carga provienen de **otro programa**, vía **web service (HTTP/JSON)**.
Queda encapsulado tras la interfaz `IExternalOrderSource`, con dos implementaciones:

- `HttpExternalOrderSource` — adaptador real, configurable por la sección `ExternalSource`
  de `src/Warehouse.Api/appsettings.json` (`BaseUrl`, `ListPath`, `OrderPath`, `Auth`).
- `MockExternalOrderSource` — datos de ejemplo en memoria, para desarrollar sin la API.

La selección es automática: si `ExternalSource:BaseUrl` está configurado se usa el
adaptador HTTP; si está vacío, se usa el mock. **Mientras no haya API, sigue el mock.**

📄 **El contrato que debe cumplir el web service** (endpoints, campos, tipos, auth y
ejemplos JSON) está en **[`docs/API-CONTRACT.md`](docs/API-CONTRACT.md)**.

Resumen de los dos endpoints esperados:

| Método | Ruta (por defecto)   | Devuelve |
|--------|----------------------|----------|
| GET    | `/orders`            | Array de órdenes `{ ref, customer, date, lineCount }` |
| GET    | `/orders/{ref}`      | Orden con `lines[]` (cada línea: `barcode`, `quantity`, `description`, …); 404 si no existe |

Mínimo imprescindible: `ref` de la orden + `lines`, y en cada línea `barcode` + `quantity`
+ `description`. Para activarlo cuando exista la API: rellenar la config `ExternalSource`
y, si los nombres de campo difieren, ajustar los DTOs `Api*` de `HttpExternalOrderSource.cs`.

## Puesta en marcha (desarrollo)

```bash
# 1. Base de datos (PostgreSQL en el puerto 5433, aislado del postgres del sistema)
docker compose up -d

# 2. API (aplica migraciones al arrancar) -> https://localhost:7000/swagger
dotnet run --project src/Warehouse.Api

# 3. (opcional) Datos de ejemplo para pruebas funcionales
docker exec -i warehouse-db psql -U warehouse -d warehouse < db/seed.sql

# 4. Cliente Blazor -> https://localhost:7100
dotnet run --project src/Warehouse.Client
```

### Datos de ejemplo

`db/seed.sql` inserta 6 órdenes de carga con sus líneas (31 en total), con códigos
de barras EAN-13 reales para poder probar el escaneo tecleándolos. Es idempotente
(se puede relanzar) y solo afecta a las órdenes de ejemplo (`external_ref` con prefijo
`OC-2026-1`). No es una migración: no se aplica en producción.

La cadena de conexión está en `src/Warehouse.Api/appsettings.json` (`ConnectionStrings:Postgres`)
y la URL de la API que consume el cliente en `src/Warehouse.Client/wwwroot/appsettings.json`
(`ApiBaseUrl`).

## Migraciones

Migrador propio y sin dependencias: al arrancar la API ejecuta en orden los scripts
`.sql` de `src/Warehouse.Api/Migrations` que no estén registrados en la tabla
`schema_migrations`. Para añadir cambios de esquema, crea un nuevo archivo
`0NN_descripcion.sql`.

## API REST

| Método | Ruta                                     | Descripción                                  |
|--------|------------------------------------------|----------------------------------------------|
| GET    | `/api/orders`                            | Órdenes disponibles (externas + importadas)  |
| GET    | `/api/orders/{id}`                       | Detalle de una orden importada               |
| POST   | `/api/orders/import/{externalRef}`       | Importa una orden del origen externo         |
| POST   | `/api/verifications`                     | Inicia una verificación                      |
| GET    | `/api/verifications/{id}`                | Estado de la verificación                    |
| PUT    | `/api/verifications/{id}/lines/{lineId}` | Marca OK/Mal + observación de una línea      |
| POST   | `/api/verifications/{id}/complete`       | Observación final + firmas + cierre          |
| POST   | `/api/pickings`                          | Inicia una preparación                       |
| GET    | `/api/pickings/{id}`                     | Estado de la preparación                     |
| POST   | `/api/pickings/{id}/scan`                | Escanea un código de barras                  |
| PUT    | `/api/pickings/{id}/lines/{lineId}`      | Ajuste manual de cantidad/observación        |
| POST   | `/api/pickings/{id}/complete`            | Observación + firma + cierre                 |
