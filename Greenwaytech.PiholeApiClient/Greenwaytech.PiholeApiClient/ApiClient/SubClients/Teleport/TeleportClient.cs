using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Pihole;
using Microsoft.Extensions.Logging;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Teleport;
public class TeleportClient : ITeleportClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public TeleportClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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

    public Task<PiholeClientApiResponse<PiholeTeleportExport>> PushPiholeTeleportFile(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
