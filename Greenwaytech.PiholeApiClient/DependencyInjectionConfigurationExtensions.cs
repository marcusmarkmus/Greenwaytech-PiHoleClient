using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Authentication;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenwaytech.PiholeApiClient;
public static class DependencyInjectionConfigurationExtensions
{
    public const string HttpClientPiholeApiClientName = "GreenwayTech.PiholeApiClientHttpClient";

    /// <summary>
    /// Marker service to detect duplicate registrations of AddPiholeApiClient.
    /// </summary>
    private sealed class PiholeApiClientRegistrationMarker;

    /// <summary>
    /// Adds and configures the Pi-hole API client services and related dependencies to the specified service
    /// collection.
    /// Note: If multiple or dynamic Pi-hole instances need to be supported, consider using <see cref="AddPiholeApiClientFactory"/> instead.
    /// </summary>
    /// <remarks>
    /// This method registers the required HTTP clients and authentication handlers for interacting
    /// with the Pi-hole API. It should be called during application startup as part of service configuration.
    /// The session provider is registered as a singleton to enable proper session caching across requests.
    /// <para>
    /// <b>WARNING:</b> This method should only be called once per service collection. Calling it multiple times
    /// will throw an <see cref="InvalidOperationException"/>. The Pi-hole API client uses internal locking
    /// to prevent race conditions during configuration mutations, but this only works correctly when a single
    /// client instance is used per Pi-hole server. For multiple Pi-hole instances, use <see cref="AddPiholeApiClientFactory"/> instead.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to which the Pi-hole API client services will be added.</param>
    /// <param name="configureOptions">A delegate that configures the options for the Pi-hole API client instance. Cannot be null.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if AddPiholeApiClient has already been called on this service collection.</exception>
    public static IServiceCollection AddPiholeApiClient(this IServiceCollection services, Action<PiHoleInstanceApiConfig> configureOptions)
    {
        // Detect duplicate registrations - this prevents race conditions from multiple client instances
        if (services.Any(sd => sd.ServiceType == typeof(PiholeApiClientRegistrationMarker)))
        {
            throw new InvalidOperationException(
                "AddPiholeApiClient has already been called on this service collection. " +
                "This method should only be called once to ensure thread-safe configuration mutations. " +
                "For multiple Pi-hole instances, use AddPiholeApiClientFactory instead.");
        }

        // Add marker to detect future duplicate registrations
        services.AddSingleton<PiholeApiClientRegistrationMarker>();

        services.Configure(configureOptions);
        
        // Register session provider as singleton to enable session caching
        services.AddSingleton<IPiholeSessionProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var logger = sp.GetRequiredService<ILogger<PiholeSessionProvider>>();
            var options = sp.GetRequiredService<IOptions<PiHoleInstanceApiConfig>>();
            httpClient.BaseAddress = new Uri(options.Value.ApiBaseUrl);
            return new PiholeSessionProvider(httpClient, logger, options);
        });
        
        services.AddTransient<PiholeAuthHandler>();
        
        // Register named HttpClient for the Pi-hole API client
        services.AddHttpClient(HttpClientPiholeApiClientName)
            .AddHttpMessageHandler<PiholeAuthHandler>();
        
        // Register API client as SINGLETON to ensure the internal lock is shared across all callers.
        // This is critical for thread-safe configuration mutations.
        services.AddSingleton<IPiholeApiClientService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(HttpClientPiholeApiClientName);
            var logger = sp.GetRequiredService<ILogger<PiholeApiClientService>>();
            var options = sp.GetRequiredService<IOptions<PiHoleInstanceApiConfig>>();
            httpClient.BaseAddress = new Uri(options.Value.ApiBaseUrl);
            return new PiholeApiClientService(httpClient, logger, options);
        });

        return services;
    }

    /// <summary>
    /// Adds the Pi-hole API client factory and related services to the specified service collection.
    /// </summary>
    /// <remarks>This method registers all required services for using <see cref="IPiholeClientFactory"/> and
    /// related Pi-hole API client components. Call this method during application startup to enable dependency
    /// injection of Pi-hole API clients.
    /// The factory pattern supports multiple Pi-hole instances with per-instance session caching.
    /// Please do not register any named HttpClients with the name defined in <see cref="HttpClientPiholeApiClientName"/>
    /// to avoid conflicts!
    /// </remarks>
    /// <param name="services">The service collection to which the Pi-hole API client factory and its dependencies will be added.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to support method chaining.</returns>
    public static IServiceCollection AddPiholeApiClientFactory(this IServiceCollection services)
    {
        services.AddSingleton<IPiholeClientFactory, PiholeClientFactory>();
        return services;
    }
}
