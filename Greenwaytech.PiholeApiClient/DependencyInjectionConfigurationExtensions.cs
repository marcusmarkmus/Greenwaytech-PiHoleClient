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
    /// Adds and configures the Pi-hole API client services and related dependencies to the specified service
    /// collection.
    /// Note: If multiple or dynamic Pi-hole instances need to be supported, consider using <see cref="AddPiholeApiClientFactory"/> instead.
    /// </summary>
    /// <remarks>
    /// This method registers the required HTTP clients and authentication handlers for interacting
    /// with the Pi-hole API. It should be called during application startup as part of service configuration.
    /// The session provider is registered as a singleton to enable proper session caching across requests.
    /// <para>
    /// <strong>Thread Safety:</strong> Each client instance is thread-safe for concurrent operations.
    /// However, if you resolve multiple transient client instances for the same Pi-hole server and perform
    /// concurrent operations across those instances, race conditions may occur. The library will log a warning
    /// if this pattern is detected.
    /// </para>
    /// <para>
    /// <strong>Best Practices:</strong>
    /// - Use a singleton or scoped lifetime when possible
    /// - Avoid resolving multiple clients in parallel loops (e.g., Parallel.ForEach with transient resolution)
    /// - For multi-instance scenarios, use <see cref="AddPiholeApiClientFactory"/> instead
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to which the Pi-hole API client services will be added.</param>
    /// <param name="configureOptions">A delegate that configures the options for the Pi-hole API client instance. Cannot be null.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddPiholeApiClient(this IServiceCollection services, Action<PiHoleInstanceApiConfig> configureOptions)
    {
        services.Configure(configureOptions);
        
        // Register registration tracker as singleton to detect multiple client instances
        services.AddSingleton<PiholeClientRegistrationTracker>();
        
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
        
        // Register named HttpClient with auth handler (not typed - to avoid double registration)
        services.AddHttpClient(nameof(PiholeApiClientService))
            .AddHttpMessageHandler<PiholeAuthHandler>();
        
        // Single registration for IPiholeApiClientService with tracking
        services.AddTransient<IPiholeApiClientService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(PiholeApiClientService));
            var logger = sp.GetRequiredService<ILogger<PiholeApiClientService>>();
            var options = sp.GetRequiredService<IOptions<PiHoleInstanceApiConfig>>();
            var tracker = sp.GetRequiredService<PiholeClientRegistrationTracker>();
            
            // Track this registration and warn if multiple clients for same instance
            tracker.RecordRegistration(options.Value.ApiBaseUrl);
            
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
