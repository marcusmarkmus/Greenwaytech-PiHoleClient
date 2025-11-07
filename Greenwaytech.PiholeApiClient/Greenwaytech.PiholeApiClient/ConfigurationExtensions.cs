using Greenwaytech.PiholeApiClient.Api;
using Greenwaytech.PiholeApiClient.Authentication;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Greenwaytech.PiholeApiClient;
public static class ConfigurationExtensions
{
    public static IServiceCollection AddPiholeApiClient(this IServiceCollection services, Action<PiHoleInstanceApiConfig> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddTransient<PiholeAuthHandler>();

        services.AddHttpClient<IPiholeSessionProvider, PiholeSessionProvider>();

        services.AddHttpClient<IPiholeApiClientService, PiholeApiClientService>()
            .AddHttpMessageHandler<PiholeAuthHandler>();

        return services;
    }
}
