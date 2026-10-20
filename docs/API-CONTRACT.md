# Contrato del web service de órdenes de carga

Documento para quien implemente el **web service origen** del que *warehouse* importa las
órdenes de carga. La app consume este servicio a través de `HttpExternalOrderSource`
(`src/Warehouse.Api/External/`), configurado en la sección `ExternalSource` de
`src/Warehouse.Api/appsettings.json`.

- Formato: **JSON** sobre HTTP.
- Los nombres de campo son **insensibles a mayúsculas/minúsculas** y se pueden ajustar en
  un único sitio (los DTOs `Api*` de `HttpExternalOrderSource.cs`), así que no tienen por
  qué ser idénticos; esta es la forma por defecto.
- Fechas en formato ISO `YYYY-MM-DD`.
- Cantidades numéricas, admiten decimales (ej. `12.5`).

## Configuración en la app

```jsonc
"ExternalSource": {
  "BaseUrl": "https://origen.ejemplo/api",   // vacío = usar el mock de ejemplo
  "ListPath": "orders",                       // ruta del listado
  "OrderPath": "orders/{ref}",                // ruta del detalle; {ref} = referencia
  "TimeoutSeconds": 30,
  "Auth": {
    "Type": "None",                           // None | ApiKey | Bearer
    "HeaderName": "X-Api-Key",                // solo para ApiKey
    "Value": ""                               // la clave (ApiKey) o el token (Bearer)
  }
}
```

---

## 1) Listado de órdenes

```
GET {BaseUrl}/{ListPath}        (por defecto: GET /orders)
```

Devuelve un **array** de órdenes disponibles para importar:

| Campo       | Tipo              | Obligatorio   | Descripción |
|-------------|-------------------|---------------|-------------|
| `ref`       | string            | **Sí** (único) | Referencia de la orden; clave para pedir el detalle |
| `customer`  | string            | No            | Nombre del cliente |
| `date`      | string `YYYY-MM-DD` | No          | Fecha de la orden |
| `lineCount` | entero            | No            | Nº de líneas (informativo) |

**Ejemplo:**

```json
[
  { "ref": "OC-2026-1001", "customer": "Distribuciones García S.L.", "date": "2026-09-18", "lineCount": 6 },
  { "ref": "OC-2026-1002", "customer": "Hostelería del Sur",        "date": "2026-09-18", "lineCount": 5 }
]
```

---

## 2) Detalle de una orden

```
GET {BaseUrl}/{OrderPath}       (por defecto: GET /orders/{ref})
```

`{ref}` se sustituye por la referencia de la orden (URL-encoded). Devuelve un **objeto**
con sus líneas. Si la orden no existe, responder **HTTP 404**.

**Cabecera de la orden:**

| Campo      | Tipo              | Obligatorio | Descripción |
|------------|-------------------|-------------|-------------|
| `ref`      | string            | **Sí**      | Referencia de la orden |
| `customer` | string            | No          | Cliente |
| `date`     | string `YYYY-MM-DD` | No        | Fecha |
| `notes`    | string            | No          | Observaciones de la orden |
| `lines`    | array             | **Sí**      | Líneas de la orden (ver abajo) |

**Cada elemento de `lines`:**

| Campo         | Tipo              | Obligatorio      | Descripción |
|---------------|-------------------|------------------|-------------|
| `barcode`     | string            | **Sí (clave)**   | **Código de barras** del producto. Es lo que se escanea en la preparación; sin él el picking no funciona |
| `quantity`    | número (decimal)  | **Sí**           | Cantidad pedida (cantidad requerida en verificación y picking) |
| `description` | string            | **Sí**           | Descripción del producto (se muestra) |
| `productCode` | string            | No               | Código / referencia interna del producto |
| `ref`         | string            | No               | Referencia de la línea en el origen |
| `unit`        | string            | No               | Unidad (ud, caja, saco…) |

**Ejemplo:**

```json
{
  "ref": "OC-2026-1001",
  "customer": "Distribuciones García S.L.",
  "date": "2026-09-18",
  "notes": "Entregar en frío",
  "lines": [
    { "ref": "L1", "productCode": "REF-100", "barcode": "7501031311309", "description": "Aceite oliva virgen extra 5L", "quantity": 24, "unit": "ud" },
    { "ref": "L2", "productCode": "REF-101", "barcode": "8410179000015", "description": "Harina de trigo 25kg",        "quantity": 10, "unit": "saco" }
  ]
}
```

---

## Mínimo imprescindible

Si la API solo pudiera entregar lo esencial, necesita **sí o sí**:

- **`ref`** de la orden (único) y la lista de **`lines`**.
- En cada línea: **`barcode`** + **`quantity`** + **`description`**.

El resto (`customer`, `date`, `notes`, `productCode`, `unit`, `lineCount`) es recomendable
pero opcional: la app funciona sin ellos.

## Notas

- Un `barcode` debe identificar de forma fiable el producto de la línea (es con lo que se
  valida el escaneo en la preparación de pedido).
- Autenticación admitida por el cliente: **sin auth**, **API key** en cabecera
  (p. ej. `X-Api-Key: <clave>`) o **Bearer token** (`Authorization: Bearer <token>`).
- Errores: cualquier código distinto de 2xx (salvo 404 en el detalle, que significa
  "orden no encontrada") se trata como fallo y se registra en el log de la API.
