using Warehouse.Shared;

namespace Warehouse.Api.External;

/// <summary>
/// Fuente de órdenes de carga del programa origen. La decisión web service vs. lectura
/// directa de BD queda encapsulada aquí: basta con implementar esta interfaz y registrarla.
/// </summary>
public interface IExternalOrderSource
{
    /// <summary>Lista las órdenes disponibles para importar.</summary>
    Task<IReadOnlyList<OrderSummary>> ListAsync(CancellationToken ct = default);

    /// <summary>Obtiene una orden completa (con líneas) por su referencia externa.</summary>
    Task<LoadOrder?> FetchAsync(string externalRef, CancellationToken ct = default);
}
