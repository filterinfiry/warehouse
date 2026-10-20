namespace Warehouse.Api.External;

/// <summary>
/// Configuración del origen externo de órdenes (sección "ExternalSource" de appsettings).
/// Si <see cref="BaseUrl"/> está vacío, la app usa <c>MockExternalOrderSource</c>.
/// </summary>
public class ExternalSourceOptions
{
    public const string SectionName = "ExternalSource";

    /// <summary>URL base del web service. Vacío = usar el mock de ejemplo.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Ruta (relativa a BaseUrl) del listado de órdenes.</summary>
    public string ListPath { get; set; } = "orders";

    /// <summary>Ruta de una orden concreta; <c>{ref}</c> se sustituye por la referencia externa.</summary>
    public string OrderPath { get; set; } = "orders/{ref}";

    public int TimeoutSeconds { get; set; } = 30;

    public AuthOptions Auth { get; set; } = new();

    /// <summary>Hay origen real configurado (hay URL base).</summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(BaseUrl);
}

/// <summary>Autenticación del web service.</summary>
public class AuthOptions
{
    /// <summary>None | ApiKey | Bearer.</summary>
    public string Type { get; set; } = "None";

    /// <summary>Nombre de cabecera para Type=ApiKey (p.ej. "X-Api-Key").</summary>
    public string? HeaderName { get; set; } = "X-Api-Key";

    /// <summary>La clave (ApiKey) o el token (Bearer).</summary>
    public string? Value { get; set; }
}
