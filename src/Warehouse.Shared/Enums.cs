namespace Warehouse.Shared;

/// <summary>Estado general de una sesión (verificación o preparación).</summary>
public enum SessionStatus
{
    InProgress = 0,
    Completed = 1,
    Cancelled = 2
}

/// <summary>Resultado del chequeo de una línea en la verificación de recepción.</summary>
public enum LineCheckResult
{
    Pending = 0,
    Ok = 1,
    Nok = 2
}

/// <summary>Estado de carga de una línea durante la preparación del pedido.</summary>
public enum PickingLineStatus
{
    Pending = 0,
    Partial = 1,
    Loaded = 2,
    Over = 3
}

/// <summary>Resultado de escanear un código de barras contra una sesión de preparación.</summary>
public enum ScanOutcome
{
    /// <summary>El código corresponde a una línea del pedido y se ha incrementado la carga.</summary>
    Matched = 0,
    /// <summary>La línea ya estaba completa; se ha marcado exceso.</summary>
    OverQuantity = 1,
    /// <summary>El código no pertenece a ninguna línea del pedido.</summary>
    NotInOrder = 2
}
