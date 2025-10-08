using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Playground.GameCatalog.Api;
using Playground.GameCatalog.Web.Services;

namespace Playground.GameCatalog.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameCatalogServices(this IServiceCollection services)
    {
        services.AddSingleton<GameCatalogClient>();
        services.AddGrpcServiceReference<Catalog.CatalogClient>($"https://gameCatalogApi", failureStatus: HealthStatus.Degraded);

        return services;
    }
}