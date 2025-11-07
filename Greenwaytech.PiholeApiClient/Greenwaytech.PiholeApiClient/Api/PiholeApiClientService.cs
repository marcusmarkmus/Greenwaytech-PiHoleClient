using Greenwaytech.PiholeApiClient.DTO;
using Greenwaytech.PiholeApiClient.Extensions;
using Greenwaytech.PiholeApiClient.Model;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Model.Teleport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;

namespace Greenwaytech.PiholeApiClient.Api;

public interface IPiholeApiClientService
{
    public Task<PiholeTeleportExport> PullPiholeTeleportFile();
}

public class PiholeApiClientService : IPiholeApiClientService
{
    private readonly HttpClient _httpClient;
    private PiholeApiSession PiholeAuthSessionsCache { get; set; }
    private readonly ILogger<PiholeApiClientService> _logger;
    private readonly PiHoleInstanceApiConfig _piholeInstanceApiConfig;
    public PiholeApiClientService(HttpClient httpClient, ILogger<PiholeApiClientService> logger, IOptions<PiHoleInstanceApiConfig> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _piholeInstanceApiConfig = options.Value;
        _httpClient.BaseAddress = new Uri(_piholeInstanceApiConfig.ApiBaseUrl);
    }

    public async Task<PiholeConfigModel> GetPiholeConfigAsync(CancellationToken cancellationToken = default)
    {
        
        throw new NotImplementedException();
        return new PiholeConfigModel();
    }

    public async Task<PiholeTeleportExport> PullPiholeTeleportFile()
    {
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/zip");
        var response = await _httpClient.GetAsync("/api/teleporter");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to pull Pi-hole teleport file");
            return new PiholeTeleportExport
            {
                Data = null,
                Contentype = string.Empty,
                contentDisposition = string.Empty
            };
        }
        var content = await response.Content.ReadAsByteArrayAsync();
        if (content.Length == 0)
        {
            _logger.LogWarning("Received empty configuration");
            throw new InvalidOperationException("Received empty configuration");
        }
        _logger.LogInformation("Successfully pulled configuration ");
        return new()
        {
            Data = content,
            Contentype = string.Join(";", response.Content.Headers.GetValues("content-type")),
            contentDisposition = string.Join(";", response.Content.Headers.GetValues("content-disposition"))
        };

    }
}


