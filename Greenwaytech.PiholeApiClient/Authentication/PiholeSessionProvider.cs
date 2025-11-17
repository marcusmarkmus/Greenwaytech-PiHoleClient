using Greenwaytech.PiholeApiClient.Extensions;
using Greenwaytech.PiholeApiClient.Logging;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Model.Pihole;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Greenwaytech.PiholeApiClient.Authentication;
internal class PiholeSessionProvider : IPiholeSessionProvider
{

    private readonly HttpClient _httpClient;
    private readonly ILogger<PiholeSessionProvider> _logger;
    private readonly PiHoleInstanceApiConfig _config;
    private PiholeApiSession? _cachedSession;

    [ActivatorUtilitiesConstructor]
    public PiholeSessionProvider(HttpClient httpClient, ILogger<PiholeSessionProvider> logger, IOptions<PiHoleInstanceApiConfig> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = options.Value;
        _httpClient.BaseAddress = new Uri(_config.ApiBaseUrl);
    }
    /// <summary>
    /// Overload for non-DI contexts that uses a console logger by default.
    /// </summary>
    public PiholeSessionProvider(HttpClient httpClient, IOptions<PiHoleInstanceApiConfig> options)
        : this(httpClient, new ConsoleLogger<PiholeSessionProvider>(), options)
    {
    }

    internal async Task<PiholeApiSession> GetValidSessionAsync()
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
            return new() { Valid = false, PiholeAuthResponseTimeStamp = DateTimeOffset.UtcNow };
        }

        var json = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<PiholeAuthResponse>(json);
        if (authResponse?.Session == null)
        {
            _logger.LogError("Invalid session response from Pi-hole");
            return new() { Valid = false, PiholeAuthResponseTimeStamp = DateTimeOffset.UtcNow };
        }

        _cachedSession = authResponse.GetPiholeApiSession();
        return _cachedSession;
    }

    Task<PiholeApiSession> IPiholeSessionProvider.GetValidSessionAsync()
    {
        return GetValidSessionAsync();
    }
}
