using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Model.Pihole;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Greenwaytech.PiholeApiClient.Api;

public interface IPiholeApiClientService
{
    Task<PiholeClientApiResponse<PiholeGetConfigResponse>> GetPiholeConfigAsync(bool detailed = false, CancellationToken cancellationToken = default);
    Task<PiholeClientApiResponse<PiholeTeleportExport>> PullPiholeTeleportFile(CancellationToken cancellationToken = default);
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

    public async Task<PiholeClientApiResponse<PiholeGetConfigResponse>> GetPiholeConfigAsync(bool detailed = false, CancellationToken cancellationToken = default)
    {
        var response = new PiholeClientApiResponse<PiholeGetConfigResponse>() 
        { IsSuccess=false,
            Data=null,
            ErrorMessage=string.Empty
        };
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("detailed", detailed.ToString());
       
        var responseMessage = await _httpClient.GetAsync("/api/config", cancellationToken);

        if (responseMessage.IsSuccessStatusCode)
        {
            var json = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            var configResponse = JsonSerializer.Deserialize<PiholeGetConfigResponse>(json);
            if (configResponse is not null)
            {
                _logger.LogInformation("Successfully pulled Pi-hole configuration");
                response.IsSuccess = true;
                response.Data = configResponse;
                return response;
            }
            _logger.LogWarning("Received empty configuration");
           response.ErrorMessage = "Received empty configuration";
           return response;
        }
        _logger.LogError("Failed to pull Pi-hole configuration");
        response.ErrorMessage = "Failed to pull Pi-hole configuration - " + await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        return response;
    }

    public async Task<PiholeClientApiResponse<PiholeTeleportExport>> PullPiholeTeleportFile(CancellationToken cancellationToken = default)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/zip");
        var response = new PiholeClientApiResponse<PiholeTeleportExport>
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };
        var responseMessage = await _httpClient.GetAsync("/api/teleporter", cancellationToken);
        if (!responseMessage.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to pull Pi-hole teleport file");
            response.ErrorMessage = "Failed to pull Pi-hole teleport file - " + await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
        var content = await responseMessage.Content.ReadAsByteArrayAsync(cancellationToken);
        if (content.Length == 0)
        {
            _logger.LogWarning("Received empty configuration");
            response.ErrorMessage = "Received empty configuration";
            return response;
        }
        _logger.LogInformation("Successfully pulled configuration ");
        response.IsSuccess = true;
        response.Data = new PiholeTeleportExport
        {
            Data = content,
            Contentype = string.Join(";", responseMessage.Content.Headers.GetValues("content-type")),
            contentDisposition = string.Join(";", responseMessage.Content.Headers.GetValues("content-disposition"))
        };
        return response;

    }

}


