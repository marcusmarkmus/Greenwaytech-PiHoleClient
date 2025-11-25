using Greenwaytech.PiholeApiClient.Model.App;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Actions;

/// <summary>
/// Client for Pi-hole action operations like gravity update, DNS restart, and cache flushing.
/// </summary>
public interface IActionsClient
{
    /// <summary>
    /// Trigger a gravity database update on the Pi-hole.
    /// This updates the blocklist database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of the gravity update</returns>
    Task<PiholeClientApiResponse<string>> UpdateGravityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restart the DNS service (FTL) on the Pi-hole.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of the DNS restart</returns>
    Task<PiholeClientApiResponse<string>> RestartDnsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Flush (clear) the Pi-hole query logs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of flushing logs</returns>
    Task<PiholeClientApiResponse<string>> FlushLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Flush (clear) the ARP cache on the Pi-hole.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response indicating success or failure of flushing ARP cache</returns>
    [Obsolete("The ARP cache flush action is deprecated and may be removed in future versions of Pi-hole.")]
    Task<PiholeClientApiResponse<string>> FlushArpCacheAsync(CancellationToken cancellationToken = default);
}
