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
    private readonly SemaphoreSlim _authLock = new(1, 1);

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
        // Quick check without lock
        if (_cachedSession?.IsValid() == true)
        {
            _logger.LogDebug("Using cached Pi-hole session (SID: {Sid}, expires in {RemainingSeconds}s)", 
                _cachedSession.Sid?[..3] + "...", 
                (_cachedSession.PiholeAuthResponseTimeStamp.AddSeconds(_cachedSession.Validity ?? 0) - DateTimeOffset.UtcNow).TotalSeconds);
            return _cachedSession;
        }

        // Lock for authentication
        await _authLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock (another thread might have refreshed it)
            if (_cachedSession?.IsValid() == true)
            {
                _logger.LogDebug("Using cached Pi-hole session acquired after lock (SID: {Sid})", _cachedSession.Sid?[..8] + "...");
                return _cachedSession;
            }

            _logger.LogInformation("Authenticating with Pi-hole API at {BaseUrl}", _config.ApiBaseUrl);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth")
            {
                Content = new StringContent($"{{\"password\":\"{_config.ApiKey}\"}}", Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to authenticate with Pi-hole API. Status: {StatusCode}", response.StatusCode);
                return new() { Valid = false, PiholeAuthResponseTimeStamp = DateTimeOffset.UtcNow };
            }

            var json = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<PiholeAuthResponse>(json);
            if (authResponse?.Session == null)
            {
                _logger.LogError("Invalid session response from Pi-hole API");
                return new() { Valid = false, PiholeAuthResponseTimeStamp = DateTimeOffset.UtcNow };
            }

            _cachedSession = authResponse.GetPiholeApiSession();
            _logger.LogInformation("Successfully authenticated with Pi-hole. Session valid for {Validity} seconds (SID: {Sid})", 
                _cachedSession.Validity, 
                _cachedSession.Sid?[..3] + "...");
            return _cachedSession;
        }
        finally
        {
            _authLock.Release();
        }
    }

    Task<PiholeApiSession> IPiholeSessionProvider.GetValidSessionAsync()
    {
        return GetValidSessionAsync();
    }

    public void Dispose()
    {
        if (_cachedSession == null || !_cachedSession.IsValid())
            return;
        
        _authLock.Wait();
        try
        {
            if (_cachedSession == null || !_cachedSession.IsValid())
                return;

            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth");
            request.Headers.Add("sid", _cachedSession.Sid);
            _httpClient.Send(request);
            _cachedSession = null;
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cachedSession == null || !_cachedSession.IsValid())
            return;
        
        await _authLock.WaitAsync();
        try
        {
            if (_cachedSession == null || !_cachedSession.IsValid())
                return;

            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/auth");
            request.Headers.Add("sid", _cachedSession.Sid);
            await _httpClient.SendAsync(request);
            _cachedSession = null;
        }
        finally
        {
            _authLock.Release();
        }
    }
}
