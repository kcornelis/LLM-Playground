using OpenAI;
using Playground.GameCatalog.Api.Services;
using Playground.GameCatalog.Models;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<GameCatalogContext>("gameCatalogDb");

var openai = builder.AddOpenAIClient("openai", settings =>
{
    settings.Key = builder.Configuration["OpenAI:ApiKey"];
});
openai.AddChatClient("gpt-4o-mini")
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-large");

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

var openAIClient = new OpenAIClient(builder.Configuration["OpenAI:ApiKey"]);

builder.Services.AddSingleton(openAIClient);

builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<ResponseService>();

var app = builder.Build();

app.MapGrpcService<CatalogService>();
app.MapGrpcService<ChatService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();
