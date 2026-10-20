using Dapper;
using Warehouse.Api.Data;
using Warehouse.Api.Endpoints;
using Warehouse.Api.External;

var builder = WebApplication.CreateBuilder(args);

// Dapper: mapear columnas snake_case a propiedades PascalCase.
DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Datos
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<VerificationRepository>();
builder.Services.AddScoped<PickingRepository>();

// Origen externo de órdenes. Decidido: WEB SERVICE (HTTP/JSON).
// Si "ExternalSource:BaseUrl" está configurado -> HttpExternalOrderSource; si no -> mock.
builder.Services.Configure<ExternalSourceOptions>(
    builder.Configuration.GetSection(ExternalSourceOptions.SectionName));

var externalOptions = builder.Configuration
    .GetSection(ExternalSourceOptions.SectionName).Get<ExternalSourceOptions>() ?? new();

if (externalOptions.Enabled)
{
    builder.Services.AddHttpClient<IExternalOrderSource, HttpExternalOrderSource>(client =>
    {
        var baseUrl = externalOptions.BaseUrl!.EndsWith('/') ? externalOptions.BaseUrl! : externalOptions.BaseUrl + "/";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(externalOptions.TimeoutSeconds);

        var auth = externalOptions.Auth;
        if (!string.IsNullOrWhiteSpace(auth.Value))
        {
            if (auth.Type.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Value);
            }
            else if (auth.Type.Equals("ApiKey", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(auth.HeaderName))
            {
                client.DefaultRequestHeaders.Add(auth.HeaderName, auth.Value);
            }
        }
    });
}
else
{
    builder.Services.AddSingleton<IExternalOrderSource, MockExternalOrderSource>();
}

// CORS para el cliente Blazor WASM en desarrollo.
const string CorsPolicy = "wasm";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>())
     .AllowAnyHeader()
     .AllowAnyMethod()));

var app = builder.Build();

// Migraciones al arrancar.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    try
    {
        await DbInitializer.RunAsync(scope.ServiceProvider, app.Environment, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fallo al aplicar migraciones. La API arranca igualmente; revisa la BD.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);

app.MapOrderEndpoints();
app.MapVerificationEndpoints();
app.MapPickingEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok", ts = DateTimeOffset.UtcNow }))
   .WithTags("Infra");

app.Run();
