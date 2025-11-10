using Greenwaytech.PiholeApiClient.ApiClient.SubClients;
using Greenwaytech.PiholeApiClient.ApiClient.SubClients.Config;
using Greenwaytech.PiholeApiClient.ApiClient.SubClients.Teleport;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Model.Pihole;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

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


    public PiholeApiClientService(HttpClient httpClient, ILogger<PiholeApiClientService> logger, IOptions<PiHoleInstanceApiConfig> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _piholeInstanceApiConfig = options.Value;
        _httpClient.BaseAddress = new Uri(_piholeInstanceApiConfig.ApiBaseUrl);
        Teleport = new TeleportClient(_httpClient, _logger);
        Config = new PiholeConfigClient(_httpClient, _logger);
    }
}


