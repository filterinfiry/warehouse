using Warehouse.Shared;

namespace Warehouse.Api.External;

/// <summary>
/// Implementación de ejemplo con datos en memoria, para desarrollar el resto de la app
/// mientras se decide el origen real (web service o BD). Reemplazable vía DI.
/// </summary>
public class MockExternalOrderSource : IExternalOrderSource
{
    private static readonly List<LoadOrder> Orders = Build();

    public Task<IReadOnlyList<OrderSummary>> ListAsync(CancellationToken ct = default)
    {
        IReadOnlyList<OrderSummary> list = Orders
            .Select(o => new OrderSummary(o.ExternalRef, o.Customer, o.OrderDate, o.Lines.Count, false, null))
            .ToList();
        return Task.FromResult(list);
    }

    public Task<LoadOrder?> FetchAsync(string externalRef, CancellationToken ct = default)
    {
        var order = Orders.FirstOrDefault(o =>
            string.Equals(o.ExternalRef, externalRef, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(order);
    }

    private static List<LoadOrder> Build()
    {
        return new List<LoadOrder>
        {
            new()
            {
                ExternalRef = "OC-2026-0001",
                Customer = "Distribuciones García S.L.",
                OrderDate = new DateOnly(2026, 9, 18),
                Lines = new()
                {
                    Line("7501031311309", "REF-100", "Aceite oliva virgen extra 5L", 24, "ud"),
                    Line("8410179000015", "REF-101", "Harina de trigo 25kg", 10, "saco"),
                    Line("8410179000022", "REF-102", "Azúcar blanco 1kg", 120, "ud"),
                    Line("8480000123456", "REF-103", "Sal marina fina 1kg", 60, "ud"),
                }
            },
            new()
            {
                ExternalRef = "OC-2026-0002",
                Customer = "Hostelería del Sur",
                OrderDate = new DateOnly(2026, 9, 18),
                Lines = new()
                {
                    Line("8412345000019", "REF-200", "Pack agua mineral 1.5L x6", 200, "pack"),
                    Line("8412345000026", "REF-201", "Refresco cola 2L", 90, "ud"),
                    Line("8412345000033", "REF-202", "Servilletas 100ud", 300, "paquete"),
                }
            }
        };
    }

    private static LoadOrderLine Line(string barcode, string code, string desc, decimal qty, string unit) => new()
    {
        Barcode = barcode,
        ProductCode = code,
        Description = desc,
        Quantity = qty,
        Unit = unit
    };
}
