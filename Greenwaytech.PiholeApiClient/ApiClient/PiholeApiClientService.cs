using Greenwaytech.PiholeApiClient.ApiClient.SubClients.Config;
using Greenwaytech.PiholeApiClient.ApiClient.SubClients.Teleport;
using Greenwaytech.PiholeApiClient.Logging;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenwaytech.PiholeApiClient.ApiClient;

public interface IPiholeApiClientService
{

    public ITeleportClient Teleport{ get; }
    public IPiholeConfigClient Config { get; }
}

public class PiholeApiClientService : IPiholeApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PiholeApiClientService> _logger;
    private readonly PiHoleInstanceApiConfig _piholeInstanceApiConfig;
    public ITeleportClient Teleport { get; }
    public IPiholeConfigClient Config { get; }

    [ActivatorUtilitiesConstructor]
    public PiholeApiClientService(HttpClient httpClient, ILogger<PiholeApiClientService> logger, IOptions<PiHoleInstanceApiConfig> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _piholeInstanceApiConfig = options.Value;
        _httpClient.BaseAddress = new Uri(_piholeInstanceApiConfig.ApiBaseUrl);
        Teleport = new TeleportClient(_httpClient, _logger);
        Config = new PiholeConfigClient(_httpClient, _logger);
    }

    /// <summary>
    /// Overload for non-DI contexts that uses a console logger by default.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="options"></param>
    public PiholeApiClientService(HttpClient httpClient, IOptions<PiHoleInstanceApiConfig> options)
        : this(httpClient, new ConsoleLogger<PiholeApiClientService>(), options)
    {
    }
}


