using Microsoft.Extensions.Diagnostics.HealthChecks;
using Playground.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();
builder.Services.AddMudServices();

var isHttps = builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https";
builder.Services.AddSingleton<GameCatalogClient>()
    .AddGrpcServiceReference<Catalog.CatalogClient>($"{(isHttps ? "https" : "http")}://gameCatalogApi", failureStatus: HealthStatus.Degraded);
    
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
