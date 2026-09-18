using Dapper;
using Warehouse.Shared;

namespace Warehouse.Api.Data;

public class VerificationRepository
{
    private readonly DbConnectionFactory _factory;
    private readonly OrderRepository _orders;

    public VerificationRepository(DbConnectionFactory factory, OrderRepository orders)
    {
        _factory = factory;
        _orders = orders;
    }

    /// <summary>Crea una sesión de verificación a partir de una orden importada, copiando sus líneas.</summary>
    public async Task<VerificationSession> StartAsync(Guid loadOrderId, CancellationToken ct = default)
    {
        var order = await _orders.GetAsync(loadOrderId, ct)
            ?? throw new KeyNotFoundException("La orden no existe o no está importada.");

        using var conn = await _factory.OpenAsync(ct);
        using var tx = conn.BeginTransaction();

        var sessionId = Guid.NewGuid();
        await conn.ExecuteAsync(
            "insert into verification_sessions (id, load_order_id, status) values (@id, @loadOrderId, 0)",
            new { id = sessionId, loadOrderId }, tx);

        foreach (var l in order.Lines)
        {
            await conn.ExecuteAsync(
                "insert into verification_lines (id, verification_session_id, load_order_line_id, result, " +
                "product_code, barcode, description, quantity, unit) " +
                "values (@Id, @SessionId, @LineId, 0, @ProductCode, @Barcode, @Description, @Quantity, @Unit)",
                new
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    LineId = l.Id,
                    l.ProductCode,
                    l.Barcode,
                    l.Description,
                    l.Quantity,
                    l.Unit
                }, tx);
        }

        tx.Commit();
        return (await GetAsync(sessionId, ct))!;
    }

    public async Task<VerificationSession?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var session = await conn.QuerySingleOrDefaultAsync<VerificationSession>(
            "select id, load_order_id, status, final_observation, supervisor_name, supervisor_signature, " +
            "carrier_name, carrier_signature, created_at, completed_at from verification_sessions where id = @id",
            new { id });
        if (session is null) return null;

        session.Lines = (await conn.QueryAsync<VerificationLine>(
            "select id, verification_session_id, load_order_line_id, result, observation, " +
            "product_code, barcode, description, quantity, unit " +
            "from verification_lines where verification_session_id = @id order by description", new { id })).ToList();

        session.Order = await _orders.GetAsync(session.LoadOrderId, ct);
        return session;
    }

    public async Task<bool> UpdateLineAsync(Guid sessionId, Guid lineId, UpdateVerificationLineRequest req, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.ExecuteAsync(
            "update verification_lines set result = @result, observation = @observation " +
            "where id = @lineId and verification_session_id = @sessionId",
            new { result = (int)req.Result, req.Observation, lineId, sessionId });
        return rows > 0;
    }

    public async Task<bool> CompleteAsync(Guid sessionId, CompleteVerificationRequest req, CancellationToken ct = default)
    {
        using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.ExecuteAsync(
            "update verification_sessions set status = 1, final_observation = @FinalObservation, " +
            "supervisor_name = @SupervisorName, supervisor_signature = @SupervisorSignature, " +
            "carrier_name = @CarrierName, carrier_signature = @CarrierSignature, completed_at = now() " +
            "where id = @sessionId and status = 0",
            new
            {
                req.FinalObservation,
                req.SupervisorName,
                req.SupervisorSignature,
                req.CarrierName,
                req.CarrierSignature,
                sessionId
            });
        return rows > 0;
    }
}
