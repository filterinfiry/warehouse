using Warehouse.Api.Data;
using Warehouse.Shared;

namespace Warehouse.Api.Endpoints;

public static class VerificationEndpoints
{
    public static void MapVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/verifications").WithTags("Verificación");

        g.MapPost("/", async (StartVerificationRequest req, VerificationRepository repo, CancellationToken ct) =>
        {
            try
            {
                var session = await repo.StartAsync(req.LoadOrderId, ct);
                return Results.Created($"/api/verifications/{session.Id}", session);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        g.MapGet("/{id:guid}", async (Guid id, VerificationRepository repo, CancellationToken ct) =>
        {
            var session = await repo.GetAsync(id, ct);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        g.MapPut("/{id:guid}/lines/{lineId:guid}", async (
            Guid id, Guid lineId, UpdateVerificationLineRequest req, VerificationRepository repo, CancellationToken ct) =>
        {
            var ok = await repo.UpdateLineAsync(id, lineId, req, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        g.MapPost("/{id:guid}/complete", async (
            Guid id, CompleteVerificationRequest req, VerificationRepository repo, CancellationToken ct) =>
        {
            var ok = await repo.CompleteAsync(id, req, ct);
            return ok ? Results.NoContent() : Results.BadRequest(new { error = "La sesión no existe o ya está cerrada." });
        });
    }
}
