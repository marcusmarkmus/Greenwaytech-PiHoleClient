using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Config;
public interface IPiholeConfigClient
{
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
}
