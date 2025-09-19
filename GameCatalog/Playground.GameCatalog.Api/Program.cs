using OpenAI;
using Playground.GameCatalog.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<Playground.GameCatalog.Models.GameCatalogContext>("gameCatalogDb");

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

builder.Services.AddSingleton(sp =>
{
    return new OpenAIClient(builder.Configuration["OpenAI:ApiKey"]);
});

// Register OpenApiService (initialization only)
builder.Services.AddSingleton<OpenAIService>();

var app = builder.Build();

app.MapGrpcService<CatalogService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();
