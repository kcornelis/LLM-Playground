using MudBlazor.Services;
using Playground.GameCatalog.Web;
using Playground.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();
builder.Services.AddMudServices();
builder.Services.AddGameCatalogServices();

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
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Playground.GameCatalog.Web.ServiceCollectionExtensions).Assembly);

app.MapDefaultEndpoints();

app.Run();
