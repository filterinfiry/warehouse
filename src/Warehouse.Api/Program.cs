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

// Origen externo de órdenes (web service o BD, aún por decidir).
// Cambiar aquí la implementación cuando se decida el origen real.
builder.Services.AddSingleton<IExternalOrderSource, MockExternalOrderSource>();

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
