using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Warehouse.Client;
using Warehouse.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base de la API REST (configurable en wwwroot/appsettings.json → ApiBaseUrl).
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBase) });

builder.Services.AddScoped<WarehouseApi>();

await builder.Build().RunAsync();
