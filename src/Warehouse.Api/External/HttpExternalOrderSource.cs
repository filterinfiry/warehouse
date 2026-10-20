using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Warehouse.Shared;

namespace Warehouse.Api.External;

/// <summary>
/// Origen de órdenes vía WEB SERVICE (HTTP/JSON). ESQUELETO LISTO PARA RELLENAR:
/// la fontanería (HttpClient, auth, rutas, opciones) está completa; lo ÚNICO que habrá
/// que ajustar cuando exista la API real son los nombres de campo del JSON —ver los DTO
/// <c>Api*</c> al final— para que casen con lo que devuelva el web service.
///
/// Mientras BaseUrl esté vacío en appsettings, la app NO usa esta clase (usa el mock).
/// </summary>
public class HttpExternalOrderSource : IExternalOrderSource
{
    private readonly HttpClient _http;
    private readonly ExternalSourceOptions _opt;
    private readonly ILogger<HttpExternalOrderSource> _log;

    // Tolerante a mayúsculas/minúsculas en los nombres de campo del JSON.
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public HttpExternalOrderSource(
        HttpClient http, IOptions<ExternalSourceOptions> opt, ILogger<HttpExternalOrderSource> log)
    {
        _http = http;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<OrderSummary>> ListAsync(CancellationToken ct = default)
    {
        var path = _opt.ListPath.TrimStart('/');
        var items = await GetJsonAsync<List<ApiOrderSummary>>(path, ct) ?? new();

        return items
            .Select(s => new OrderSummary(s.Ref, s.Customer, s.Date, s.LineCount, Imported: false, LocalId: null))
            .ToList();
    }

    public async Task<LoadOrder?> FetchAsync(string externalRef, CancellationToken ct = default)
    {
        var path = _opt.OrderPath
            .TrimStart('/')
            .Replace("{ref}", Uri.EscapeDataString(externalRef));

        var o = await GetJsonAsync<ApiOrder>(path, ct, allowNotFound: true);
        if (o is null) return null;

        return new LoadOrder
        {
            ExternalRef = o.Ref,
            Customer = o.Customer,
            OrderDate = o.Date,
            Notes = o.Notes,
            Lines = o.Lines.Select(l => new LoadOrderLine
            {
                ExternalRef = l.Ref,
                ProductCode = l.ProductCode,
                Barcode = l.Barcode,
                Description = l.Description ?? "",
                Quantity = l.Quantity,
                Unit = l.Unit
            }).ToList()
        };
    }

    private async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct, bool allowNotFound = false)
    {
        using var resp = await _http.GetAsync(path, ct);

        if (allowNotFound && resp.StatusCode == HttpStatusCode.NotFound)
            return default;

        if (!resp.IsSuccessStatusCode)
        {
            var body = await SafeBody(resp, ct);
            _log.LogError("Origen externo {Path} devolvió {Code}. {Body}", path, (int)resp.StatusCode, body);
            throw new HttpRequestException($"El origen externo respondió {(int)resp.StatusCode} en '{path}'.");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, Json, ct);
    }

    private static async Task<string> SafeBody(HttpResponseMessage resp, CancellationToken ct)
    {
        try { return (await resp.Content.ReadAsStringAsync(ct)).Trim(); }
        catch { return ""; }
    }

    // -------------------------------------------------------------------------
    //  DTOs del JSON de la API. ⚠ AJUSTAR estos nombres cuando exista la API real
    //  (o añadir [JsonPropertyName("...")] a cada propiedad si no coinciden).
    //  Forma asumida de momento:
    //    GET {ListPath}          -> [ { ref, customer, date, lineCount }, ... ]
    //    GET {OrderPath}  ({ref}) -> { ref, customer, date, notes, lines: [ { ref,
    //                                 productCode, barcode, description, quantity, unit } ] }
    // -------------------------------------------------------------------------
    private sealed class ApiOrderSummary
    {
        public string Ref { get; set; } = "";
        public string? Customer { get; set; }
        public DateOnly? Date { get; set; }
        public int LineCount { get; set; }
    }

    private sealed class ApiOrder
    {
        public string Ref { get; set; } = "";
        public string? Customer { get; set; }
        public DateOnly? Date { get; set; }
        public string? Notes { get; set; }
        public List<ApiOrderLine> Lines { get; set; } = new();
    }

    private sealed class ApiOrderLine
    {
        public string? Ref { get; set; }
        public string? ProductCode { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }
}
