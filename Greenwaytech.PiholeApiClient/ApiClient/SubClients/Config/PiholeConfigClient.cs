using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Config;
public class PiholeConfigClient : IPiholeConfigClient
{ 
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    public PiholeConfigClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    /// <summary>
    /// TODO: Add documentation
    /// </summary>
    /// <param name="detailed"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<PiholeClientApiResponse<PiholeGetConfigResponse>> GetPiholeConfigAsync(bool detailed = false, CancellationToken cancellationToken = default)
    {
        var response = new PiholeClientApiResponse<PiholeGetConfigResponse>()
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
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


    public async Task<PiholeClientApiResponse<PiholeGetConfigResponse>> PatchPiholeConfigAsync(PiholePatchConfigRequest patchRequest, bool restartServices=true, CancellationToken cancellationToken = default)
    {
        var config = patchRequest ?? throw new ArgumentNullException(nameof(patchRequest));
        var response = new PiholeClientApiResponse<PiholeGetConfigResponse>()
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("restart", restartServices.ToString());

        var content = new StringContent(JsonSerializer.Serialize(config, _jsonSerializerOptions), Encoding.UTF8, "application/json");
        var responseMessage = await _httpClient.PatchAsync("/api/config", content, cancellationToken);
        if (responseMessage.IsSuccessStatusCode)
        {
            var json = responseMessage.Content.ReadAsStringAsync(cancellationToken).Result;
            var configResponse = JsonSerializer.Deserialize<PiholeGetConfigResponse>(json);
            if (configResponse is not null)
            {
                _logger.LogInformation("Successfully updated Pi-hole configuration");
                response.IsSuccess = true;
                response.Data = configResponse;
                return response;
            }
            _logger.LogWarning("Received empty configuration after update");
            response.ErrorMessage = "Received empty configuration after update";
            return response;
        }
        _logger.LogError("Failed to update Pi-hole configuration");
        response.ErrorMessage = "Failed to update Pi-hole configuration - " + await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        return response;
    }
}
