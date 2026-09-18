namespace Warehouse.Shared;

/// <summary>
/// Orden de carga importada desde el sistema externo (web service o BD, aún por decidir).
/// Es la fuente común para los dos flujos: verificación de recepción y preparación de pedido.
/// </summary>
public class LoadOrder
{
    public Guid Id { get; set; }

    /// <summary>Referencia de la orden en el programa origen.</summary>
    public string ExternalRef { get; set; } = "";

    public string? Customer { get; set; }
    public DateOnly? OrderDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<LoadOrderLine> Lines { get; set; } = new();
}

/// <summary>Línea de detalle de una orden de carga.</summary>
public class LoadOrderLine
{
    public Guid Id { get; set; }
    public Guid LoadOrderId { get; set; }

    public string? ExternalRef { get; set; }
    public string? ProductCode { get; set; }
    public string? Barcode { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}

// ---------------------------------------------------------------------------
// Flujo 1 — Verificación de recepción
// ---------------------------------------------------------------------------

/// <summary>
/// Sesión de verificación: se comprueba que el pedido recibido es correcto,
/// marcando cada línea OK/NOK, con observaciones y dos firmas (supervisor y transportista).
/// </summary>
public class VerificationSession
{
    public Guid Id { get; set; }
    public Guid LoadOrderId { get; set; }
    public SessionStatus Status { get; set; }

    public string? FinalObservation { get; set; }

    public string? SupervisorName { get; set; }
    public string? SupervisorSignature { get; set; }   // PNG en base64 (data URL)
    public string? CarrierName { get; set; }
    public string? CarrierSignature { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public LoadOrder? Order { get; set; }
    public List<VerificationLine> Lines { get; set; } = new();
}

public class VerificationLine
{
    public Guid Id { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid LoadOrderLineId { get; set; }

    public LineCheckResult Result { get; set; }
    public string? Observation { get; set; }

    // Copia de datos de la línea de origen para pintar el detalle sin joins extra.
    public string? ProductCode { get; set; }
    public string? Barcode { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}

// ---------------------------------------------------------------------------
// Flujo 2 — Preparación de pedido (picking / carga con códigos de barras)
// ---------------------------------------------------------------------------

/// <summary>
/// Sesión de preparación: dada una orden de carga se prepara el pedido escaneando
/// códigos de barras. Cada escaneo válido incrementa la cantidad cargada.
/// </summary>
public class PickingSession
{
    public Guid Id { get; set; }
    public Guid LoadOrderId { get; set; }
    public SessionStatus Status { get; set; }

    public string? Observation { get; set; }
    public string? PreparerName { get; set; }
    public string? PreparerSignature { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public LoadOrder? Order { get; set; }
    public List<PickingLine> Lines { get; set; } = new();
}

public class PickingLine
{
    public Guid Id { get; set; }
    public Guid PickingSessionId { get; set; }
    public Guid LoadOrderLineId { get; set; }

    public string? Barcode { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal LoadedQuantity { get; set; }
    public PickingLineStatus Status { get; set; }
    public string? Observation { get; set; }

    public string? ProductCode { get; set; }
    public string Description { get; set; } = "";
    public string? Unit { get; set; }
}
