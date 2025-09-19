using Microsoft.EntityFrameworkCore;
using Playground.GameCatalog.Migrations;
using Playground.GameCatalog.Models;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<GameCatalogDbInitializer>();

builder.AddServiceDefaults();

builder.Services.AddDbContextPool<GameCatalogContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GameCatalogDb"), sqlOptions =>
        sqlOptions.MigrationsAssembly("Playground.GameCatalog.Migrations")
    ));
builder.EnrichSqlServerDbContext<GameCatalogContext>();

var app = builder.Build();

app.Run();