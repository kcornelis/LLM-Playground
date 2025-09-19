using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Playground.GameCatalog.Models;

namespace Playground.GameCatalog.Migrations;

public class GameCatalogDbInitializer(
    IServiceProvider _serviceProvider,
    IHostEnvironment _hostEnvironment,
    IHostApplicationLifetime _hostApplicationLifetime) : BackgroundService
{
    private readonly ActivitySource _activitySource = new(_hostEnvironment.ApplicationName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity(_hostEnvironment.ApplicationName, ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameCatalogContext>();

            await EnsureDatabaseAsync(dbContext, cancellationToken);
            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedDataAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        _hostApplicationLifetime.StopApplication();
    }

    private static async Task EnsureDatabaseAsync(GameCatalogContext dbContext, CancellationToken cancellationToken)
    {
        var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Create the database if it does not exist.
            // Do this first so there is then a database to start a transaction against.
            if (!await dbCreator.ExistsAsync(cancellationToken))
            {
                await dbCreator.CreateAsync(cancellationToken);
            }
        });
    }

    private static async Task RunMigrationAsync(GameCatalogContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task SeedDataAsync(GameCatalogContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Get all SQL files in the DataSeeding folder
            var dataSeedingDir = Path.Combine(AppContext.BaseDirectory, "DataSeeding");
            var sqlFiles = Directory.GetFiles(dataSeedingDir, "*.sql");

            foreach (var sqlFilePath in sqlFiles)
            {
                var sql = await File.ReadAllTextAsync(sqlFilePath, Encoding.Default, cancellationToken);
                await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }
}