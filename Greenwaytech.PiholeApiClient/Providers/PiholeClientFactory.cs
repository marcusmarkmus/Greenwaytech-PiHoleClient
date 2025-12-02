using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Authentication;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Greenwaytech.PiholeApiClient.Providers;

public class PiholeClientFactory : IPiholeClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    
    // Cache session providers per Pi-hole instance to enable session reuse across multiple client creations
    private readonly ConcurrentDictionary<string, IPiholeSessionProvider> _sessionProviders = new();

    public PiholeClientFactory(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    public IPiholeApiClientService CreateClient(IPiHoleInstanceApiConfig piHoleInstanceApiConfig)
    {
        if (string.IsNullOrWhiteSpace(piHoleInstanceApiConfig.ApiBaseUrl))
            throw new ArgumentException("Base URL must be provided", nameof(piHoleInstanceApiConfig.ApiBaseUrl));
        if (string.IsNullOrWhiteSpace(piHoleInstanceApiConfig.ApiKey))
            throw new ArgumentException("API key must be provided", nameof(piHoleInstanceApiConfig.ApiKey));

        // Create a unique cache key for this Pi-hole instance (URL + API key combination)
        var cacheKey = $"{piHoleInstanceApiConfig.ApiBaseUrl.TrimEnd('/')}|{piHoleInstanceApiConfig.ApiKey}";
        
        // Get or create a session provider for this specific Pi-hole instance
        var sessionProvider = _sessionProviders.GetOrAdd(cacheKey, _ =>
        {
            var sessionHttpClient = _httpClientFactory.CreateClient();
            sessionHttpClient.BaseAddress = new Uri(piHoleInstanceApiConfig.ApiBaseUrl);
            var sessionLogger = _loggerFactory.CreateLogger<PiholeSessionProvider>();
            var config = new PiHoleInstanceApiConfig
            {
                ApiBaseUrl = piHoleInstanceApiConfig.ApiBaseUrl,
                ApiKey = piHoleInstanceApiConfig.ApiKey
            };
            var options = Options.Create(config);
            
            var provider = new PiholeSessionProvider(sessionHttpClient, sessionLogger, options);
            sessionLogger.LogInformation("Created new session provider for Pi-hole instance: {BaseUrl}", piHoleInstanceApiConfig.ApiBaseUrl);
            return provider;
        });

        // Create a dedicated auth handler for this client that uses the per-instance session provider
        var authHandlerLogger = _loggerFactory.CreateLogger<PiholeAuthHandler>();
        var authHandler = new PiholeAuthHandler(authHandlerLogger, sessionProvider)
        {
            InnerHandler = new HttpClientHandler()
        };

        // Create HttpClient with the auth handler in the pipeline
        var httpClient = new HttpClient(authHandler)
        {
            BaseAddress = new Uri(piHoleInstanceApiConfig.ApiBaseUrl)
        };

        var logger = _loggerFactory.CreateLogger<PiholeApiClientService>();
        var clientConfig = new PiHoleInstanceApiConfig
        {
            ApiBaseUrl = piHoleInstanceApiConfig.ApiBaseUrl,
            ApiKey = piHoleInstanceApiConfig.ApiKey
        };
        var clientOptions = Options.Create(clientConfig);

        return new PiholeApiClientService(httpClient, logger, clientOptions);
    }

    public IPiholeApiClientService CreateClient(PiholeNode node) 
        => CreateClient(node.Config);
}