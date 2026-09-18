using Dapper;
using Warehouse.Api.External;
using Warehouse.Shared;

namespace Warehouse.Api.Data;

public class OrderRepository
{
    private readonly DbConnectionFactory _factory;
    private readonly IExternalOrderSource _external;

    public OrderRepository(DbConnectionFactory factory, IExternalOrderSource external)
    {
        _factory = factory;
        _external = external;
    }

    /// <summary>Órdenes ya importadas a la BD local.</summary>
    public async Task<List<LoadOrder>> ListImportedAsync(CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var orders = (await conn.QueryAsync<LoadOrder>(
            "select id, external_ref, customer, order_date, notes, created_at from load_orders order by created_at desc"))
            .ToList();
        return orders;
    }

    public async Task<LoadOrder?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var order = await conn.QuerySingleOrDefaultAsync<LoadOrder>(
            "select id, external_ref, customer, order_date, notes, created_at from load_orders where id = @id",
            new { id });
        if (order is null) return null;

        order.Lines = (await conn.QueryAsync<LoadOrderLine>(
            "select id, load_order_id, external_ref, product_code, barcode, description, quantity, unit " +
            "from load_order_lines where load_order_id = @id order by description", new { id })).ToList();
        return order;
    }

    public async Task<LoadOrder?> GetByExternalRefAsync(string externalRef, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>(
            "select id from load_orders where external_ref = @externalRef", new { externalRef });
        return id is null ? null : await GetAsync(id.Value, ct);
    }

    /// <summary>
    /// Importa una orden del sistema externo. Idempotente: si ya existe por external_ref,
    /// devuelve la existente sin duplicar.
    /// </summary>
    public async Task<LoadOrder> ImportAsync(string externalRef, CancellationToken ct = default)
    {
        var existing = await GetByExternalRefAsync(externalRef, ct);
        if (existing is not null) return existing;

        var src = await _external.FetchAsync(externalRef, ct)
            ?? throw new KeyNotFoundException($"La orden '{externalRef}' no existe en el origen externo.");

        using var conn = await _factory.OpenAsync(ct);
        using var tx = conn.BeginTransaction();

        var orderId = Guid.NewGuid();
        await conn.ExecuteAsync(
            "insert into load_orders (id, external_ref, customer, order_date, notes) " +
            "values (@Id, @ExternalRef, @Customer, @OrderDate, @Notes)",
            new { Id = orderId, src.ExternalRef, src.Customer, src.OrderDate, src.Notes }, tx);

        foreach (var l in src.Lines)
        {
            await conn.ExecuteAsync(
                "insert into load_order_lines (id, load_order_id, external_ref, product_code, barcode, description, quantity, unit) " +
                "values (@Id, @LoadOrderId, @ExternalRef, @ProductCode, @Barcode, @Description, @Quantity, @Unit)",
                new
                {
                    Id = Guid.NewGuid(),
                    LoadOrderId = orderId,
                    l.ExternalRef,
                    l.ProductCode,
                    l.Barcode,
                    l.Description,
                    l.Quantity,
                    l.Unit
                }, tx);
        }

        tx.Commit();
        return (await GetAsync(orderId, ct))!;
    }

    /// <summary>Combina las órdenes del origen externo con las ya importadas para el listado.</summary>
    public async Task<List<OrderSummary>> ListAvailableAsync(CancellationToken ct = default)
    {
        var imported = await ListImportedAsync(ct);
        var importedByRef = imported.ToDictionary(o => o.ExternalRef, StringComparer.OrdinalIgnoreCase);
        var lineCounts = await LineCountsAsync(ct);

        var external = await _external.ListAsync(ct);
        var result = new List<OrderSummary>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in external)
        {
            seen.Add(s.ExternalRef);
            if (importedByRef.TryGetValue(s.ExternalRef, out var local))
                result.Add(s with { Imported = true, LocalId = local.Id, LineCount = s.LineCount });
            else
                result.Add(s);
        }

        // Órdenes importadas que ya no aparecen en el origen externo.
        foreach (var o in imported.Where(o => !seen.Contains(o.ExternalRef)))
            result.Add(new OrderSummary(o.ExternalRef, o.Customer, o.OrderDate,
                lineCounts.GetValueOrDefault(o.Id), true, o.Id));

        return result;
    }

    /// <summary>Número de líneas por orden importada (una sola consulta agregada).</summary>
    private async Task<Dictionary<Guid, int>> LineCountsAsync(CancellationToken ct)
    {
        using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<LineCountRow>(
            "select load_order_id, count(*)::int as cnt from load_order_lines group by load_order_id");
        return rows.ToDictionary(r => r.LoadOrderId, r => r.Cnt);
    }

    private record LineCountRow(Guid LoadOrderId, int Cnt);
}
