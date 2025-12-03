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

/// <summary>
/// Provides methods for managing Pi-hole configuration including DNS records and other settings.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Thread Safety:</strong> This class is thread-safe for concurrent operations within a single instance.
/// All configuration mutation methods (EnsureLocalDnsRecord, RemoveLocalDnsRecord*, etc.) use internal locking
/// to ensure that read-modify-write sequences are properly serialized, preventing race conditions and lost updates.
/// </para>
/// <para>
/// <strong>Important Limitation:</strong> Thread safety is guaranteed ONLY when using the SAME client instance.
/// If you create multiple client instances for the same Pi-hole server and perform concurrent operations across
/// those instances, race conditions may still occur because each instance has its own lock.
/// </para>
/// <para>
/// <strong>Safe Usage Patterns:</strong>
/// - Single client instance with concurrent operations ✓
/// - Scoped/Singleton client lifetime ✓
/// - Sequential operations even with multiple instances ✓
/// </para>
/// <para>
/// <strong>Unsafe Usage Patterns:</strong>
/// - Multiple transient clients in Parallel.ForEach ✗
/// - Concurrent operations across different client instances ✗
/// </para>
/// <para>
/// The library will log a warning if it detects multiple client registrations for the same Pi-hole instance.
/// For advanced multi-instance scenarios, consider using the factory pattern with proper external coordination.
/// </para>
/// </remarks>
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
    private const string TrueString = "true";
    private const string FalseString = "false";

    /// <summary>
    /// Semaphore to serialize ALL configuration mutations per Pi-hole instance.
    /// This prevents race conditions where multiple concurrent operations perform read-modify-write sequences.
    /// IMPORTANT: The entire read-modify-write cycle must be locked, not just the write operation.
    /// Example race condition without proper locking:
    ///   Thread A: GET config (v1) -> modify -> PATCH (creates v2)
    ///   Thread B: GET config (v1) -> modify -> PATCH (overwrites v2 with changes based on v1!)
    /// By locking the entire operation, we ensure:
    ///   Thread A: LOCK -> GET (v1) -> modify -> PATCH (v2) -> UNLOCK
    ///   Thread B: [waits] -> LOCK -> GET (v2) -> modify -> PATCH (v3) -> UNLOCK
    /// </summary>
    private readonly SemaphoreSlim _configMutationLock = new(1, 1);

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
    /// <para>
    /// WARNING: When performing read-modify-write operations (GET config, modify locally, PATCH config),
    /// you MUST lock the entire sequence to prevent race conditions. If you call this method directly
    /// after GetPiholeConfigAsync, consider using the higher-level methods (e.g., EnsureLocalDnsRecord)
    /// which handle locking automatically, or manually acquire _configMutationLock around your entire operation.
    /// </para>
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

        // Cache content to avoid reading stream twice
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
    /// Prevents creating duplicate domains pointing to different IPs.
    /// To overwrite existing records for the same domain, set OverwriteExisting to true in the request.
    /// This will also remove any conflicting records for that domain, and thus "clean" the configuration.
    /// <para>
    /// This operation is thread-safe. The entire read-modify-write sequence is locked to prevent race conditions.
    /// </para>
    /// </summary>
    /// <param name="localDnsRecordRequest">DNS record to ensure exists</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating if record was created, already existed, or has conflicts</returns>
    public async Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> EnsureLocalDnsRecord(
        LocalDnsRecordRequest localDnsRecordRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localDnsRecordRequest);

        // Validate request using extension method
        var validationResult = localDnsRecordRequest.Validate();
        if (!validationResult.IsValid)
        {
            return validationResult.ErrorMessage!.ToFailureResponse<EnsureLocalDnsRecordResponse>();
        }

        // Lock the entire read-modify-write sequence to prevent race conditions
        await _configMutationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Acquired config mutation lock for EnsureLocalDnsRecord: {Domain}", localDnsRecordRequest.Domain);

            // Get current configuration (inside lock to ensure we read the latest state)
            var currentConfigResponse = await GetPiholeConfigAsync(detailed: false, cancellationToken).ConfigureAwait(false);
            if (!currentConfigResponse.IsSuccess || currentConfigResponse.Data is null)
            {
                _logger.LogError("Unable to retrieve current Pi-hole configuration to ensure local DNS record.");
                return "Unable to retrieve current Pi-hole configuration".ToFailureResponse<EnsureLocalDnsRecordResponse>();
            }

            var localDnsRecords = currentConfigResponse.Data.Config?.Dns?.Hosts ?? [];
  
            // Try to add the record using extension method with conflict detection
            var addResult = localDnsRecords.TryAddRecord(
                localDnsRecordRequest.IpAddress, 
                localDnsRecordRequest.Domain,
                overWriteExisting: localDnsRecordRequest.OverwriteExisting);

            // Handle addResult - only skip patch if no changes were made
            if (!addResult.WasAdded)
            {
                // Record already exists (idempotent) or there's a conflict
                return new PiholeClientApiResponse<EnsureLocalDnsRecordResponse>
                {
                    IsSuccess = !addResult.HasConflict,
                    Data = new EnsureLocalDnsRecordResponse
                    {
                        Message = addResult.Message,
                        DataOperation = addResult.AlreadyExists ? DataOperation.AlreadyExists : DataOperation.Conflict,
                        ConflictingIpAddresses = addResult.ConflictingIpAddresses
                    }
                };
            }

            // WasAdded was true - Patch the configuration with the updated records
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

            _logger.LogInformation("Successfully ensured DNS record: {Domain} -> {IpAddress}", localDnsRecordRequest.Domain, localDnsRecordRequest.IpAddress);

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
        finally
        {
            _configMutationLock.Release();
            _logger.LogDebug("Released config mutation lock for EnsureLocalDnsRecord");
        }
    }

    /// <summary>
    /// Removes all DNS records for a specific domain (regardless of IP address).
    /// <para>
    /// This operation is thread-safe. The entire read-modify-write sequence is locked to prevent race conditions.
    /// </para>
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

        // Lock the entire read-modify-write sequence
        await _configMutationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Acquired config mutation lock for RemoveLocalDnsRecordsByDomain: {Domain}", domain);

            // Get current configuration (inside lock)
            var currentConfigResponse = await GetPiholeConfigAsync(detailed: false, cancellationToken).ConfigureAwait(false);
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
        finally
        {
            _configMutationLock.Release();
            _logger.LogDebug("Released config mutation lock for RemoveLocalDnsRecordsByDomain");
        }
    }

    /// <summary>
    /// Removes all DNS records pointing to a specific IP address (all domains for that IP).
    /// <para>
    /// This operation is thread-safe. The entire read-modify-write sequence is locked to prevent race conditions.
    /// </para>
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

        // Lock the entire read-modify-write sequence
        await _configMutationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Acquired config mutation lock for RemoveLocalDnsRecordsByIp: {IpAddress}", ipAddress);

            // Get current configuration (inside lock)
            var currentConfigResponse = await GetPiholeConfigAsync(detailed: false, cancellationToken).ConfigureAwait(false);
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
        finally
        {
            _configMutationLock.Release();
            _logger.LogDebug("Released config mutation lock for RemoveLocalDnsRecordsByIp");
        }
    }

    /// <summary>
    /// Removes a specific DNS record (exact match of IP and domain).
    /// Use this when you want to remove only one specific record and there might be duplicates.
    /// <para>
    /// This operation is thread-safe. The entire read-modify-write sequence is locked to prevent race conditions.
    /// </para>
    /// </summary>
    /// <param name="localDnsRecordRequest">The exact DNS record to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating if the specific record was removed</returns>
    public async Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> 
        RemoveLocalDnsRecord(
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

        // Lock the entire read-modify-write sequence
        await _configMutationLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Acquired config mutation lock for RemoveLocalDnsRecord: {Domain} -> {IpAddress}", 
                localDnsRecordRequest.Domain, localDnsRecordRequest.IpAddress);

            // Get current configuration (inside lock)
            var currentConfigResponse = await GetPiholeConfigAsync(detailed: false, cancellationToken).ConfigureAwait(false);
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

            var patchResponse = await PatchPiholeConfigAsync(patchRequest, restartServices: false, cancellationToken).ConfigureAwait(false);

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
        finally
        {
            _configMutationLock.Release();
            _logger.LogDebug("Released config mutation lock for RemoveLocalDnsRecord");
        }
    }


    /// <summary>
    /// Validates the current local DNS configuration and returns the result of the validation operation.
    /// This is a read-only operation and does not require locking.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the validation operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see
    /// cref="PiholeClientApiResponse{T}"/> with a tuple indicating whether the configuration is valid and an error
    /// message if validation fails.</returns>
    public async Task<PiholeClientApiResponse<(bool Valid, string ErrorMessage)>> ValidateLocalDnsConfig(
        CancellationToken cancellationToken = default)
    {
        var currentConfigResponse = await GetPiholeConfigAsync(detailed: false, cancellationToken).ConfigureAwait(false);
        if (!currentConfigResponse.IsSuccess) 
        {
            _logger.LogError("Unable to retrieve current Pi-hole configuration to validate local DNS configuration.");
            return new PiholeClientApiResponse<(bool Valid, string ErrorMessage)>
            {
                IsSuccess = false,
                ErrorMessage = "Unable to retrieve current Pi-hole configuration",
                Data = (false, "Unable to retrieve current Pi-hole configuration")
            };
        }

        var currentRecords = currentConfigResponse.Data?.Config?.Dns?.Hosts;
        var dnsValidation = currentRecords.ValidateDnsRecords();
        
        if (!dnsValidation.IsValid)
        {
            _logger.LogWarning("Local DNS configuration validation failed: {ErrorMessage}", dnsValidation.ErrorMessage);
            return new PiholeClientApiResponse<(bool Valid, string ErrorMessage)>
            {
                IsSuccess = false,
                ErrorMessage = dnsValidation.ErrorMessage ?? "DNS records validation failed",
                Data = (false, dnsValidation.ErrorMessage ?? "DNS records validation failed")
            };
        }

        _logger.LogInformation("Local DNS configuration validation succeeded.");
        return new PiholeClientApiResponse<(bool Valid, string ErrorMessage)>
        {
            IsSuccess = true,
            Data = (true, string.Empty)
        };
    }
}
