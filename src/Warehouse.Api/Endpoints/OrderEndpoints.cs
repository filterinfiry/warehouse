using Warehouse.Api.Data;

namespace Warehouse.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/orders").WithTags("Órdenes");

        // Listado combinado: disponibles en el origen externo + ya importadas.
        g.MapGet("/", async (OrderRepository repo, CancellationToken ct) =>
            Results.Ok(await repo.ListAvailableAsync(ct)));

        // Detalle de una orden importada.
        g.MapGet("/{id:guid}", async (Guid id, OrderRepository repo, CancellationToken ct) =>
        {
            var order = await repo.GetAsync(id, ct);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        // Importa una orden del origen externo a la BD local (idempotente).
        g.MapPost("/import/{externalRef}", async (string externalRef, OrderRepository repo, CancellationToken ct) =>
        {
            try
            {
                var order = await repo.ImportAsync(externalRef, ct);
                return Results.Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });
    }
}
