using Warehouse.Api.Data;
using Warehouse.Shared;

namespace Warehouse.Api.Endpoints;

public static class PickingEndpoints
{
    public static void MapPickingEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/pickings").WithTags("Preparación");

        g.MapPost("/", async (StartPickingRequest req, PickingRepository repo, CancellationToken ct) =>
        {
            try
            {
                var session = await repo.StartAsync(req.LoadOrderId, ct);
                return Results.Created($"/api/pickings/{session.Id}", session);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        g.MapGet("/{id:guid}", async (Guid id, PickingRepository repo, CancellationToken ct) =>
        {
            var session = await repo.GetAsync(id, ct);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        // Escaneo de código de barras: el corazón del flujo de preparación.
        g.MapPost("/{id:guid}/scan", async (Guid id, ScanRequest req, PickingRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Barcode))
                return Results.BadRequest(new { error = "Código de barras vacío." });
            var result = await repo.ScanAsync(id, req, ct);
            return Results.Ok(result);
        });

        g.MapPut("/{id:guid}/lines/{lineId:guid}", async (
            Guid id, Guid lineId, UpdatePickingLineRequest req, PickingRepository repo, CancellationToken ct) =>
        {
            var ok = await repo.UpdateLineAsync(id, lineId, req, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        g.MapPost("/{id:guid}/complete", async (
            Guid id, CompletePickingRequest req, PickingRepository repo, CancellationToken ct) =>
        {
            var ok = await repo.CompleteAsync(id, req, ct);
            return ok ? Results.NoContent() : Results.BadRequest(new { error = "La sesión no existe o ya está cerrada." });
        });
    }
}
