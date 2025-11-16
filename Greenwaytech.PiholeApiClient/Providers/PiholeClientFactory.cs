using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Greenwaytech.PiholeApiClient.Providers;

public class PiholeClientFactory : IPiholeClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

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

        var httpClient = _httpClientFactory.CreateClient(DependencyInjectionConfigurationExtensions.HttpClientPiholeApiClientName);
        httpClient.BaseAddress = new Uri(piHoleInstanceApiConfig.ApiBaseUrl);
        var logger = _loggerFactory.CreateLogger<PiholeApiClientService>();
        var config = new PiHoleInstanceApiConfig
        {
            ApiBaseUrl = piHoleInstanceApiConfig.ApiBaseUrl,
            ApiKey = piHoleInstanceApiConfig.ApiKey
        };
        var options = Microsoft.Extensions.Options.Options.Create(config);
        return new PiholeApiClientService(httpClient, logger, options);
    }
    public IPiholeApiClientService CreateClient(PiholeNode node) 
        => CreateClient(node.Config);

}