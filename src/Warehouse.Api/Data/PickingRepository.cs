using Dapper;
using Warehouse.Shared;

namespace Warehouse.Api.Data;

public class PickingRepository
{
    private readonly DbConnectionFactory _factory;
    private readonly OrderRepository _orders;

    public PickingRepository(DbConnectionFactory factory, OrderRepository orders)
    {
        _factory = factory;
        _orders = orders;
    }

    public async Task<PickingSession> StartAsync(Guid loadOrderId, CancellationToken ct = default)
    {
        var order = await _orders.GetAsync(loadOrderId, ct)
            ?? throw new KeyNotFoundException("La orden no existe o no está importada.");

        using var conn = await _factory.OpenAsync(ct);
        using var tx = conn.BeginTransaction();

        var sessionId = Guid.NewGuid();
        await conn.ExecuteAsync(
            "insert into picking_sessions (id, load_order_id, status) values (@id, @loadOrderId, 0)",
            new { id = sessionId, loadOrderId }, tx);

        foreach (var l in order.Lines)
        {
            await conn.ExecuteAsync(
                "insert into picking_lines (id, picking_session_id, load_order_line_id, barcode, " +
                "required_quantity, loaded_quantity, status, product_code, description, unit) " +
                "values (@Id, @SessionId, @LineId, @Barcode, @Required, 0, 0, @ProductCode, @Description, @Unit)",
                new
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    LineId = l.Id,
                    l.Barcode,
                    Required = l.Quantity,
                    l.ProductCode,
                    l.Description,
                    l.Unit
                }, tx);
        }

        tx.Commit();
        return (await GetAsync(sessionId, ct))!;
    }

    public async Task<PickingSession?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var session = await conn.QuerySingleOrDefaultAsync<PickingSession>(
            "select id, load_order_id, status, observation, preparer_name, preparer_signature, " +
            "created_at, completed_at from picking_sessions where id = @id", new { id });
        if (session is null) return null;

        session.Lines = await LoadLinesAsync(id, ct);
        session.Order = await _orders.GetAsync(session.LoadOrderId, ct);
        return session;
    }

    private async Task<List<PickingLine>> LoadLinesAsync(Guid sessionId, CancellationToken ct)
    {
        using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<PickingLine>(
            "select id, picking_session_id, load_order_line_id, barcode, required_quantity, loaded_quantity, " +
            "status, observation, product_code, description, unit " +
            "from picking_lines where picking_session_id = @sessionId order by description",
            new { sessionId })).ToList();
    }

    /// <summary>
    /// Procesa un escaneo: localiza la línea por código de barras, incrementa la cantidad cargada
    /// y actualiza su estado. Avisa si el código no pertenece al pedido o si ya estaba completo.
    /// </summary>
    public async Task<ScanResponse> ScanAsync(Guid sessionId, ScanRequest req, CancellationToken ct = default)
    {
        var barcode = req.Barcode?.Trim() ?? "";
        var qty = req.Quantity <= 0 ? 1 : req.Quantity;

        using var conn = await _factory.OpenAsync(ct);
        using var tx = conn.BeginTransaction();

        var line = await conn.QuerySingleOrDefaultAsync<PickingLine>(
            "select id, picking_session_id, load_order_line_id, barcode, required_quantity, loaded_quantity, " +
            "status, observation, product_code, description, unit " +
            "from picking_lines where picking_session_id = @sessionId and barcode = @barcode " +
            "for update",
            new { sessionId, barcode }, tx);

        if (line is null)
        {
            tx.Rollback();
            return new ScanResponse(ScanOutcome.NotInOrder,
                $"El código {barcode} no pertenece a este pedido.", null, await AllLoadedAsync(sessionId, ct));
        }

        if (line.LoadedQuantity >= line.RequiredQuantity)
        {
            // Ya completa: registramos exceso pero no bloqueamos.
            var over = line.LoadedQuantity + qty;
            await conn.ExecuteAsync(
                "update picking_lines set loaded_quantity = @over, status = @status where id = @id",
                new { over, status = (int)PickingLineStatus.Over, id = line.Id }, tx);
            tx.Commit();

            line.LoadedQuantity = over;
            line.Status = PickingLineStatus.Over;
            return new ScanResponse(ScanOutcome.OverQuantity,
                $"«{line.Description}» ya estaba completa. Exceso registrado.", line, await AllLoadedAsync(sessionId, ct));
        }

        var newQty = line.LoadedQuantity + qty;
        var newStatus = newQty >= line.RequiredQuantity ? PickingLineStatus.Loaded : PickingLineStatus.Partial;
        await conn.ExecuteAsync(
            "update picking_lines set loaded_quantity = @newQty, status = @status where id = @id",
            new { newQty, status = (int)newStatus, id = line.Id }, tx);
        tx.Commit();

        line.LoadedQuantity = newQty;
        line.Status = newStatus;
        var msg = newStatus == PickingLineStatus.Loaded
            ? $"«{line.Description}» completada ({newQty:0.##}/{line.RequiredQuantity:0.##})."
            : $"«{line.Description}» {newQty:0.##}/{line.RequiredQuantity:0.##}.";
        return new ScanResponse(ScanOutcome.Matched, msg, line, await AllLoadedAsync(sessionId, ct));
    }

    public async Task<bool> UpdateLineAsync(Guid sessionId, Guid lineId, UpdatePickingLineRequest req, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var line = await conn.QuerySingleOrDefaultAsync<PickingLine>(
            "select required_quantity from picking_lines where id = @lineId and picking_session_id = @sessionId",
            new { lineId, sessionId });
        if (line is null) return false;

        var status = req.LoadedQuantity <= 0 ? PickingLineStatus.Pending
            : req.LoadedQuantity < line.RequiredQuantity ? PickingLineStatus.Partial
            : req.LoadedQuantity > line.RequiredQuantity ? PickingLineStatus.Over
            : PickingLineStatus.Loaded;

        var rows = await conn.ExecuteAsync(
            "update picking_lines set loaded_quantity = @LoadedQuantity, observation = @Observation, status = @status " +
            "where id = @lineId and picking_session_id = @sessionId",
            new { req.LoadedQuantity, req.Observation, status = (int)status, lineId, sessionId });
        return rows > 0;
    }

    public async Task<bool> CompleteAsync(Guid sessionId, CompletePickingRequest req, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.ExecuteAsync(
            "update picking_sessions set status = 1, observation = @Observation, preparer_name = @PreparerName, " +
            "preparer_signature = @PreparerSignature, completed_at = now() where id = @sessionId and status = 0",
            new { req.Observation, req.PreparerName, req.PreparerSignature, sessionId });
        return rows > 0;
    }

    private async Task<bool> AllLoadedAsync(Guid sessionId, CancellationToken ct)
    {
        using var conn = await _factory.OpenAsync(ct);
        var pending = await conn.ExecuteScalarAsync<int>(
            "select count(*) from picking_lines where picking_session_id = @sessionId and loaded_quantity < required_quantity",
            new { sessionId });
        return pending == 0;
    }
}
