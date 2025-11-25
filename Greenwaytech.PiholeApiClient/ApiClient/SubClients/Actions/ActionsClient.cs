using Greenwaytech.PiholeApiClient.Model.App;
using Microsoft.Extensions.Logging;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Actions;

public class ActionsClient : IActionsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public ActionsClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Trigger a gravity database update on the Pi-hole.
    /// This updates the blocklist database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of the gravity update</returns>
    public async Task<PiholeClientApiResponse<string>> UpdateGravityAsync(CancellationToken cancellationToken = default)
    {
        var response = new PiholeClientApiResponse<string>
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/action/gravity");
        request.Headers.Add("Accept", "application/json");

        var responseMessage = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (responseMessage.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully triggered gravity update");
            response.IsSuccess = true;
            response.Data = responseContent;
            return response;
        }

        _logger.LogError("Failed to trigger gravity update");
        response.ErrorMessage = string.Concat("Failed to trigger gravity update - ", responseContent);
        return response;
    }

    /// <summary>
    /// Restart the DNS service (FTL) on the Pi-hole.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of the DNS restart</returns>
    public async Task<PiholeClientApiResponse<string>> RestartDnsAsync(CancellationToken cancellationToken = default)
    {
        var response = new PiholeClientApiResponse<string>
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/action/restartdns");
        request.Headers.Add("Accept", "application/json");

        var responseMessage = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (responseMessage.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully restarted DNS service");
            response.IsSuccess = true;
            response.Data = responseContent;
            return response;
        }

        _logger.LogError("Failed to restart DNS service");
        response.ErrorMessage = string.Concat("Failed to restart DNS service - ", responseContent);
        return response;
    }

    /// <summary>
    /// Flush (clear) the Pi-hole query logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of flushing logs</returns>
    public async Task<PiholeClientApiResponse<string>> FlushLogsAsync(CancellationToken cancellationToken = default)
    {
        var response = new PiholeClientApiResponse<string>
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/action/flush/logs");
        request.Headers.Add("Accept", "application/json");

        var responseMessage = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (responseMessage.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully flushed query logs");
            response.IsSuccess = true;
            response.Data = responseContent;
            return response;
        }

        _logger.LogError("Failed to flush query logs");
        response.ErrorMessage = string.Concat("Failed to flush query logs - ", responseContent);
        return response;
    }

    /// <summary>
    /// Flush (clear) the ARP cache on the Pi-hole.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of flushing ARP cache</returns>
    [Obsolete("The ARP cache flush action is deprecated and may be removed in future versions of Pi-hole.")]
    public async Task<PiholeClientApiResponse<string>> FlushArpCacheAsync(CancellationToken cancellationToken = default)
    {
        var response = new PiholeClientApiResponse<string>
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/action/flush/arp");
        request.Headers.Add("Accept", "application/json");

        var responseMessage = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (responseMessage.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully flushed ARP cache");
            response.IsSuccess = true;
            response.Data = responseContent;
            return response;
        }

        _logger.LogError("Failed to flush ARP cache");
        response.ErrorMessage = string.Concat("Failed to flush ARP cache - ", responseContent);
        return response;
    }
}
