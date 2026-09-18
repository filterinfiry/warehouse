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

### Origen de datos externo (pendiente de decidir)

Las órdenes de carga provienen de **otro programa**. El medio (web service o lectura
directa de BD) aún **no está decidido**, así que queda encapsulado tras la interfaz
`IExternalOrderSource`. Ahora mismo hay una implementación de ejemplo
(`MockExternalOrderSource`) con datos en memoria. Para conectar el origen real basta con
implementar la interfaz y cambiar el registro en `Program.cs`.

## Puesta en marcha (desarrollo)

```bash
# 1. Base de datos
docker compose up -d

# 2. API (aplica migraciones al arrancar) -> https://localhost:7000/swagger
dotnet run --project src/Warehouse.Api

# 3. Cliente Blazor -> https://localhost:7100
dotnet run --project src/Warehouse.Client
```

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
