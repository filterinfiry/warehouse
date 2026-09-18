namespace Warehouse.Shared;

// Peticiones y respuestas de la API (contratos cliente <-> servidor).

/// <summary>Resumen de una orden disponible en el sistema externo o ya importada.</summary>
public record OrderSummary(
    string ExternalRef,
    string? Customer,
    DateOnly? OrderDate,
    int LineCount,
    bool Imported,
    Guid? LocalId);

public record StartVerificationRequest(Guid LoadOrderId);

public record UpdateVerificationLineRequest(LineCheckResult Result, string? Observation);

public record CompleteVerificationRequest(
    string? FinalObservation,
    string? SupervisorName,
    string? SupervisorSignature,
    string? CarrierName,
    string? CarrierSignature);

public record StartPickingRequest(Guid LoadOrderId);

public record ScanRequest(string Barcode, decimal Quantity = 1);

/// <summary>Resultado de un escaneo, con la línea afectada (si la hay) y el estado global.</summary>
public record ScanResponse(
    ScanOutcome Outcome,
    string Message,
    PickingLine? Line,
    bool AllLoaded);

public record UpdatePickingLineRequest(decimal LoadedQuantity, string? Observation);

public record CompletePickingRequest(
    string? Observation,
    string? PreparerName,
    string? PreparerSignature);
