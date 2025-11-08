using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Pihole;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
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

    public async Task<PiholeClientApiResponse<PiholeTeleportImportResponse>> PushPiholeTeleportFile(PiholeTeleportImportRequest importRequest, CancellationToken cancellationToken = default)
    {
        var apiResponse = new PiholeClientApiResponse<PiholeTeleportImportResponse>
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var requestContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(importRequest.File);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        requestContent.Add(fileContent, "file", "teleport.zip");
        if (importRequest.PiholeTeleportImportSettings != null)
        {
            var importSettingsJson = System.Text.Json.JsonSerializer.Serialize(importRequest.PiholeTeleportImportSettings);
            var importSettingsContent = new StringContent(importSettingsJson, System.Text.Encoding.UTF8, "application/json");
            requestContent.Add(importSettingsContent, "import");
        }
        var response = await _httpClient.PostAsync("/api/teleporter", requestContent, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseData = System.Text.Json.JsonSerializer.Deserialize<PiholeTeleportImportResponse>(responseContent);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to push Pi-hole teleport file");
            apiResponse.ErrorMessage = "Failed to push Pi-hole teleport file - " + responseContent;
            return apiResponse;
        }

        apiResponse.IsSuccess = true;
        apiResponse.Data = responseData;
        return apiResponse;
    }
}
