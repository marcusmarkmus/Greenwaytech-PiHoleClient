using Greenwaytech.PiholeApiClient.DTO;
using Greenwaytech.PiholeApiClient.Extensions;
using Greenwaytech.PiholeApiClient.Model;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Greenwaytech.PiholeApiClient.Authentication;
public class PiholeSessionProvider : IPiholeSessionProvider
{

    private readonly HttpClient _httpClient;
    private readonly ILogger<PiholeSessionProvider> _logger;
    private readonly PiHoleInstanceApiConfig _config;
    private PiholeApiSession? _cachedSession;

    public PiholeSessionProvider(HttpClient httpClient, ILogger<PiholeSessionProvider> logger, IOptions<PiHoleInstanceApiConfig> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = options.Value;
        _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl);
    }
    public async Task<PiholeApiSession> GetValidSessionAsync()
    {

        if (_cachedSession?.IsValid() == true)
        {
            _logger.LogInformation("Using cached Pi-hole session");
            return _cachedSession;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth")
        {
            Content = new StringContent($"{{\"password\":\"{_config.ApiKey}\"}}", Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to authenticate with Pi-hole API");
            return new() { valid = false, PiholeAuthResponseTimeStamp = DateTimeOffset.UtcNow };
        }

        var json = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<PiholeAuthResponse>(json);
        if (authResponse?.session == null)
        {
            _logger.LogError("Invalid session response from Pi-hole");
            return new() { valid = false, PiholeAuthResponseTimeStamp = DateTimeOffset.UtcNow };
        }

        _cachedSession = authResponse.GetPiholeApiSession();
        return _cachedSession;
    }
}
