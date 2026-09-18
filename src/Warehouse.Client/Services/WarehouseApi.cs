using System.Net.Http.Json;
using Warehouse.Shared;

namespace Warehouse.Client.Services;

/// <summary>Cliente tipado sobre la API REST de almacén.</summary>
public class WarehouseApi
{
    private readonly HttpClient _http;

    public WarehouseApi(HttpClient http) => _http = http;

    // --- Órdenes ---------------------------------------------------------

    public async Task<List<OrderSummary>> ListOrdersAsync() =>
        await _http.GetFromJsonAsync<List<OrderSummary>>("/api/orders") ?? new();

    public async Task<LoadOrder?> GetOrderAsync(Guid id) =>
        await _http.GetFromJsonAsync<LoadOrder>($"/api/orders/{id}");

    public async Task<LoadOrder?> ImportOrderAsync(string externalRef)
    {
        var resp = await _http.PostAsync($"/api/orders/import/{Uri.EscapeDataString(externalRef)}", null);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<LoadOrder>() : null;
    }

    // --- Verificación ----------------------------------------------------

    public async Task<VerificationSession?> StartVerificationAsync(Guid loadOrderId)
    {
        var resp = await _http.PostAsJsonAsync("/api/verifications", new StartVerificationRequest(loadOrderId));
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<VerificationSession>() : null;
    }

    public async Task<VerificationSession?> GetVerificationAsync(Guid id) =>
        await _http.GetFromJsonAsync<VerificationSession>($"/api/verifications/{id}");

    public async Task<bool> UpdateVerificationLineAsync(Guid id, Guid lineId, LineCheckResult result, string? observation)
    {
        var resp = await _http.PutAsJsonAsync($"/api/verifications/{id}/lines/{lineId}",
            new UpdateVerificationLineRequest(result, observation));
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CompleteVerificationAsync(Guid id, CompleteVerificationRequest req)
    {
        var resp = await _http.PostAsJsonAsync($"/api/verifications/{id}/complete", req);
        return resp.IsSuccessStatusCode;
    }

    // --- Preparación -----------------------------------------------------

    public async Task<PickingSession?> StartPickingAsync(Guid loadOrderId)
    {
        var resp = await _http.PostAsJsonAsync("/api/pickings", new StartPickingRequest(loadOrderId));
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<PickingSession>() : null;
    }

    public async Task<PickingSession?> GetPickingAsync(Guid id) =>
        await _http.GetFromJsonAsync<PickingSession>($"/api/pickings/{id}");

    public async Task<ScanResponse?> ScanAsync(Guid id, string barcode, decimal qty = 1)
    {
        var resp = await _http.PostAsJsonAsync($"/api/pickings/{id}/scan", new ScanRequest(barcode, qty));
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<ScanResponse>() : null;
    }

    public async Task<bool> UpdatePickingLineAsync(Guid id, Guid lineId, decimal loadedQty, string? observation)
    {
        var resp = await _http.PutAsJsonAsync($"/api/pickings/{id}/lines/{lineId}",
            new UpdatePickingLineRequest(loadedQty, observation));
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CompletePickingAsync(Guid id, CompletePickingRequest req)
    {
        var resp = await _http.PostAsJsonAsync($"/api/pickings/{id}/complete", req);
        return resp.IsSuccessStatusCode;
    }
}
