var builder = DistributedApplication.CreateBuilder(args);

var parameter = builder.AddParameter("openai-api-key");

var sqlServer = builder.AddSqlServer("Playground")
                    .WithVolume(target: "/var/opt/mssql")
                    .WithLifetime(ContainerLifetime.Persistent);

var gameCatalogDb = sqlServer.AddDatabase("gameCatalogDb", "GameCatalog");

var gameCatalogMigrations = builder.AddProject<Projects.Playground_GameCatalog_Migrations>("gameCatalogMigrations")
    .WithReference(gameCatalogDb)
    .WaitFor(gameCatalogDb);

var gameCatalogApi = builder.AddProject<Projects.Playground_GameCatalog_Api>("gameCatalogApi")
    .WithEnvironment("OpenAI__ApiKey", parameter)
    .WithReference(gameCatalogDb)
    .WaitFor(gameCatalogMigrations);

builder.AddProject<Projects.Playground_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(gameCatalogMigrations)
    .WaitFor(gameCatalogMigrations)
    .WithReference(gameCatalogApi)
    .WaitFor(gameCatalogApi);

builder.Build().Run();
