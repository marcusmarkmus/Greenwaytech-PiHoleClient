using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.App.Request;
using Greenwaytech.PiholeApiClient.Model.App.Response;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Config;
public interface IPiholeConfigClient
{
    /// <summary>
    /// Ensures a local DNS record exists in the Pi-hole configuration, adding it if not present.
    /// By default, prevents creating duplicate domains pointing to different IPs.
    /// </summary>
    /// <param name="localDnsRecordRequest">DNS record to ensure exists</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating if record was created, already existed, or has conflicts</returns>
    Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> EnsureLocalDnsRecord(
        LocalDnsRecordRequest localDnsRecordRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current Pi-hole configuration.
    /// </summary>
    /// <param name="detailed"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PiholeClientApiResponse<PiholeGetConfigResponse>> GetPiholeConfigAsync(bool detailed = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provided a PiholePatchConfigRequest object, patch the Pi-hole configuration.
    /// Only provided settings will be changed, unspecified settings will remain unchanged.
    /// !IMPORTANT!: Lists/arrays provided in the config will REPLACE existing lists/arrays, not append to them.
    /// Do a GetPiholeConfigAsync first to retrieve existing lists/arrays if you want to append to them.
    /// </summary>
    /// <param name="patchRequest"></param>
    /// <param name="restartServices">Restart FTL after a change </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PiholeClientApiResponse<PiholeGetConfigResponse>> PatchPiholeConfigAsync(PiholePatchConfigRequest patchRequest, bool restartServices = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific DNS record (exact match of IP and domain).
    /// Use this when you want to remove only one specific record and there might be duplicates.
    /// </summary>
    /// <param name="localDnsRecordRequest">The exact DNS record to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating if the specific record was removed</returns>
    Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> RemoveLocalDnsRecord(
        LocalDnsRecordRequest localDnsRecordRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all DNS records for a specific domain (regardless of IP address).
    /// </summary>
    /// <param name="domain">Domain name to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating how many records were removed</returns>
    Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> RemoveLocalDnsRecordsByDomain(
        string domain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all DNS records pointing to a specific IP address (all domains for that IP).
    /// </summary>
    /// <param name="ipAddress">IP address to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating how many records were removed</returns>
    Task<PiholeClientApiResponse<EnsureLocalDnsRecordResponse>> RemoveLocalDnsRecordsByIp(
        string ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the current local DNS configuration and returns the result of the validation operation.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the validation operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a response with a tuple indicating whether the configuration is valid and an error message if validation fails.</returns>
    Task<PiholeClientApiResponse<(bool Valid, string ErrorMessage)>> ValidateLocalDnsConfig(
        CancellationToken cancellationToken = default);
}
