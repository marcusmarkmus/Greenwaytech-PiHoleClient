using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.App.Request;
using Greenwaytech.PiholeApiClient.Model.App.Response;
using Greenwaytech.PiholeApiClient.Model.Pihole;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Greenwaytech.PiholeApiClient.Extensions;

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

    // Cache common header values to avoid repeated allocations
    private const string TrueString = "True";
    private const string FalseString = "False";

    public PiholeConfigClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get the current Pi-hole configuration.
    /// </summary>
    /// <param name="detailed">Include detailed configuration information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the Pi-hole configuration</returns>
    public async Task<PiholeClientApiResponse<PiholeGetConfigResponse>> GetPiholeConfigAsync(bool detailed = false, CancellationToken cancellationToken = default)
    {
        var response = new PiholeClientApiResponse<PiholeGetConfigResponse>()
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };

        // Use HttpRequestMessage for thread-safe header management
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("detailed", detailed ? TrueString : FalseString);

        var responseMessage = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Cache content to avoid reading stream twice
        var responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (responseMessage.IsSuccessStatusCode)
        {
            var configResponse = JsonSerializer.Deserialize<PiholeGetConfigResponse>(responseContent);
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
        response.ErrorMessage = string.Concat("Failed to pull Pi-hole configuration - ", responseContent);
        return response;
    }

    /// <summary>
    /// Provided a PiholePatchConfigRequest object, patch the Pi-hole configuration.
    /// Only provided settings will be changed, unspecified settings will remain unchanged.
    /// !IMPORTANT!: Lists/arrays provided in the config will REPLACE existing lists/arrays, not append to them.
    /// Do a GetPiholeConfigAsync first to retrieve existing lists/arrays if you want to append to them.
    /// </summary>
    /// <param name="patchRequest">Configuration patch request</param>
    /// <param name="restartServices">Restart FTL after a change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the updated Pi-hole configuration</returns>
    public async Task<PiholeClientApiResponse<PiholeGetConfigResponse>> PatchPiholeConfigAsync(PiholePatchConfigRequest patchRequest, bool restartServices=true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchRequest);

        var response = new PiholeClientApiResponse<PiholeGetConfigResponse>()
        {
            IsSuccess = false,
            Data = null,
            ErrorMessage = string.Empty
        };

        var jsonContent = JsonSerializer.Serialize(patchRequest, _jsonSerializerOptions);
        
        // Use HttpRequestMessage for thread-safe header management
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/config");
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("restart", restartServices ? TrueString : FalseString);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var responseMessage = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Cache content to avoid reading stream twice - FIXED: removed .Result blocking call
        var responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (responseMessage.IsSuccessStatusCode)
        {
            var configResponse = JsonSerializer.Deserialize<PiholeGetConfigResponse>(responseContent);
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
        response.ErrorMessage = string.Concat("Failed to update Pi-hole configuration - ", responseContent);
        return response;
    }

    /// <summary>
    /// Ensures a local DNS record exists in the Pi-hole configuration, adding it if not present.
    /// By default, prevents creating duplicate domains pointing to different IPs.
    /// </summary>
    /// <param name="localDnsRecordRequest">DNS record to ensure exists</param>
    /// <param name="allowDuplicateDomains">If true, allows same domain to point to multiple IPs (not recommended)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating if record was created, already existed, or has conflicts</returns>
    public async Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> EnsureLocalDnsRecord(
        LocalDnsRecordRequest localDnsRecordRequest,
        bool allowDuplicateDomains = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localDnsRecordRequest);

        // Validate request using extension method
        var validationResult = localDnsRecordRequest.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult.ErrorMessage!.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        // Get current configuration
        var currentConfigResponse = await GetPiholeConfigAsync(detailed: true, cancellationToken).ConfigureAwait(false);
        if (!currentConfigResponse.IsSuccess || currentConfigResponse.Data is null)
        {
            _logger.LogError("Unable to retrieve current Pi-hole configuration to ensure local DNS record.");
            return "Unable to retrieve current Pi-hole configuration".ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        var currentRecords = currentConfigResponse.Data.Config?.Dns?.Hosts;

        // Try to add the record using extension method with conflict detection
        var addResult = currentRecords.TryAddRecord(
            localDnsRecordRequest.IpAddress, 
            localDnsRecordRequest.Domain,
            allowDuplicateDomains);

        // Handle conflicts
        if (addResult.HasConflict)
        {
            _logger.LogWarning("DNS record conflict detected for domain {Domain}. Existing IPs: {IPs}", 
                localDnsRecordRequest.Domain, 
                string.Join(", ", addResult.ConflictingIpAddresses ?? []));
            
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = false,
                ErrorMessage = addResult.Message,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = addResult.Message,
                    DataOperation = DataOperation.Conflict,
                    ConflictingIpAddresses = addResult.ConflictingIpAddresses
                }
            };
        }

        // If record already existed, return early
        if (addResult.AlreadyExists)
        {
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = true,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = addResult.Message,
                    DataOperation = DataOperation.AlreadyExists
                }
            };
        }

        // Patch the configuration with the updated records
        var patchRequest = new PiholePatchConfigRequest
        {
            Config = new PiholeConfigModel
            {
                Dns = new Dns
                {
                    Hosts = [.. addResult.UpdatedRecords]
                }
            }
        };

        var patchResponse = await PatchPiholeConfigAsync(patchRequest, restartServices: true, cancellationToken).ConfigureAwait(false);

        if (!patchResponse.IsSuccess)
        {
            var errorMsg = string.Concat("Failed to patch Pi-hole configuration to add local DNS record: ", patchResponse.ErrorMessage);
            return errorMsg.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
        {
            IsSuccess = true,
            Data = new EnsureLocalDnsRecordResponse
            {
                Message = addResult.Message,
                DataOperation = DataOperation.Created
            }
        };
    }

    /// <summary>
    /// Removes all DNS records for a specific domain (regardless of IP address).
    /// </summary>
    /// <param name="domain">Domain name to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating how many records were removed</returns>
    public async Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> RemoveLocalDnsRecordsByDomain(
        string domain, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return "Domain cannot be null or empty".ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        // Get current configuration
        var currentConfigResponse = await GetPiholeConfigAsync(detailed: true, cancellationToken).ConfigureAwait(false);
        if (!currentConfigResponse.IsSuccess || currentConfigResponse.Data is null)
        {
            _logger.LogError("Unable to retrieve current Pi-hole configuration to remove local DNS record.");
            return "Unable to retrieve current Pi-hole configuration".ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        var currentRecords = currentConfigResponse.Data.Config?.Dns?.Hosts;

        // Check if there are no records
        if (currentRecords.RecordCount() == 0)
        {
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = true,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = "No DNS records exist",
                    DataOperation = DataOperation.AlreadyExists
                }
            };
        }

        // Try to remove records by domain
        var removeResult = currentRecords.TryRemoveRecordsByDomain(domain);

        // If no records were removed, return early
        if (!removeResult.WasRemoved)
        {
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = true,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = removeResult.Message,
                    DataOperation = DataOperation.AlreadyExists
                }
            };
        }

        // Patch the configuration with the updated records
        var patchRequest = new PiholePatchConfigRequest
        {
            Config = new PiholeConfigModel
            {
                Dns = new Dns
                {
                    Hosts = removeResult.UpdatedRecords
                }
            }
        };

        var patchResponse = await PatchPiholeConfigAsync(patchRequest, restartServices: true, cancellationToken).ConfigureAwait(false);

        if (!patchResponse.IsSuccess)
        {
            var errorMsg = string.Concat("Failed to patch Pi-hole configuration to remove local DNS record: ", patchResponse.ErrorMessage);
            return errorMsg.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
        {
            IsSuccess = true,
            Data = new EnsureLocalDnsRecordResponse
            {
                Message = removeResult.Message,
                DataOperation = DataOperation.Deleted,
                RemovedCount = removeResult.RemovedCount,
                RemovedIpAddresses = removeResult.RemovedIpAddresses
            }
        };
    }

    /// <summary>
    /// Removes all DNS records pointing to a specific IP address (all domains for that IP).
    /// </summary>
    /// <param name="ipAddress">IP address to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating how many records were removed</returns>
    public async Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> RemoveLocalDnsRecordsByIp(
        string ipAddress, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return "IP address cannot be null or empty".ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        // Get current configuration
        var currentConfigResponse = await GetPiholeConfigAsync(detailed: true, cancellationToken).ConfigureAwait(false);
        if (!currentConfigResponse.IsSuccess || currentConfigResponse.Data is null)
        {
            _logger.LogError("Unable to retrieve current Pi-hole configuration to remove local DNS records by IP.");
            return "Unable to retrieve current Pi-hole configuration".ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        var currentRecords = currentConfigResponse.Data.Config?.Dns?.Hosts;

        // Check if there are no records
        if (currentRecords.RecordCount() == 0)
        {
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = true,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = "No DNS records exist",
                    DataOperation = DataOperation.AlreadyExists
                }
            };
        }

        // Try to remove records by IP
        var removeResult = currentRecords.TryRemoveRecordsByIp(ipAddress);

        // If no records were removed, return early
        if (!removeResult.WasRemoved)
        {
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = true,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = removeResult.Message,
                    DataOperation = DataOperation.AlreadyExists
                }
            };
        }

        // Patch the configuration with the updated records
        var patchRequest = new PiholePatchConfigRequest
        {
            Config = new PiholeConfigModel
            {
                Dns = new Dns
                {
                    Hosts = removeResult.UpdatedRecords
                }
            }
        };

        var patchResponse = await PatchPiholeConfigAsync(patchRequest, restartServices: true, cancellationToken).ConfigureAwait(false);

        if (!patchResponse.IsSuccess)
        {
            var errorMsg = string.Concat("Failed to patch Pi-hole configuration to remove local DNS records by IP: ", patchResponse.ErrorMessage);
            return errorMsg.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
        {
            IsSuccess = true,
            Data = new EnsureLocalDnsRecordResponse
            {
                Message = removeResult.Message,
                DataOperation = DataOperation.Deleted,
                RemovedCount = removeResult.RemovedCount,
                RemovedDomains = removeResult.RemovedDomains
            }
        };
    }

    /// <summary>
    /// Removes a specific DNS record (exact match of IP and domain).
    /// Use this when you want to remove only one specific record and there might be duplicates.
    /// </summary>
    /// <param name="localDnsRecordRequest">The exact DNS record to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating if the specific record was removed</returns>
    public async Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> RemoveLocalDnsRecord(
        LocalDnsRecordRequest localDnsRecordRequest, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localDnsRecordRequest);

        // Validate request
        var validationResult = localDnsRecordRequest.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult.ErrorMessage!.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        // Get current configuration
        var currentConfigResponse = await GetPiholeConfigAsync(detailed: true, cancellationToken).ConfigureAwait(false);
        if (!currentConfigResponse.IsSuccess || currentConfigResponse.Data is null)
        {
            _logger.LogError("Unable to retrieve current Pi-hole configuration to remove local DNS record.");
            return "Unable to retrieve current Pi-hole configuration".ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        var currentRecords = currentConfigResponse.Data.Config?.Dns?.Hosts;

        // Check if there are no records
        if (currentRecords.RecordCount() == 0)
        {
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = true,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = "No DNS records exist",
                    DataOperation = DataOperation.AlreadyExists
                }
            };
        }

        // Try to remove the specific record
        var removeResult = currentRecords.TryRemoveSpecificRecord(
            localDnsRecordRequest.IpAddress, 
            localDnsRecordRequest.Domain);

        // If record wasn't found, return early
        if (!removeResult.WasRemoved)
        {
            return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
            {
                IsSuccess = true,
                Data = new EnsureLocalDnsRecordResponse
                {
                    Message = removeResult.Message,
                    DataOperation = DataOperation.AlreadyExists
                }
            };
        }

        // Patch the configuration with the updated records
        var patchRequest = new PiholePatchConfigRequest
        {
            Config = new PiholeConfigModel
            {
                Dns = new Dns
                {
                    Hosts = removeResult.UpdatedRecords
                }
            }
        };

        var patchResponse = await PatchPiholeConfigAsync(patchRequest, restartServices: true, cancellationToken).ConfigureAwait(false);

        if (!patchResponse.IsSuccess)
        {
            var errorMsg = string.Concat("Failed to patch Pi-hole configuration to remove local DNS record: ", patchResponse.ErrorMessage);
            return errorMsg.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
        {
            IsSuccess = true,
            Data = new EnsureLocalDnsRecordResponse
            {
                Message = removeResult.Message,
                DataOperation = DataOperation.Deleted
            }
        };
    }
}
