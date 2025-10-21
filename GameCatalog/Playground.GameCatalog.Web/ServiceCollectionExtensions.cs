using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Playground.GameCatalog.Api;

namespace Playground.GameCatalog.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameCatalogServices(this IServiceCollection services)
    {
        services.AddGrpcServiceReference<Catalog.CatalogClient>($"https://gameCatalogApi", failureStatus: HealthStatus.Degraded);
        services.AddGrpcServiceReference<Chat.ChatClient>($"https://gameCatalogApi", failureStatus: HealthStatus.Degraded);

        return services;
    }
}