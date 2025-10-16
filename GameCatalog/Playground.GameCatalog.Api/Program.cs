using OpenAI;
using Playground.GameCatalog.Api.Services;
using Playground.GameCatalog.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<GameCatalogContext>("gameCatalogDb");

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

var openAIClient = new OpenAIClient(builder.Configuration["OpenAI:ApiKey"]);

builder.Services.AddSingleton(openAIClient);

//builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<ResponseService>();

var app = builder.Build();

app.MapGrpcService<CatalogService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();
