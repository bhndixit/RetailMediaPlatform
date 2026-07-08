using Microsoft.Extensions.DependencyInjection;
using RetailMedia.Infrastructure.Analytics;
using RetailMedia.Infrastructure.Caching;
using RetailMedia.Infrastructure.Messaging;
using RetailMedia.Infrastructure.Tenancy;
using RetailMedia.Messaging.Abstractions;
using RetailMedia.Web.Abstractions;
using RetailMedia.Web.Tenancy;

namespace RetailMedia.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<IAnalyticsStore, InMemoryAnalyticsStore>();
        services.AddSingleton<ICacheProvider, InMemoryCache>();
        services.AddSingleton<ITenantProvider, ConfigurationTenantProvider>();

        return services;
    }
}
