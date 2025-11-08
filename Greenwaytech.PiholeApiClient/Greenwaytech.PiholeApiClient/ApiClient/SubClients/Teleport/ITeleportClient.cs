using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Pihole;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Teleport;

/// <summary>
/// Use for Pi-hole backup operations.
/// </summary>
public interface ITeleportClient
{
    Task<PiholeClientApiResponse<PiholeTeleportExport>> PullPiholeTeleportFile(CancellationToken cancellationToken = default);

    /// <summary>
    /// Push a Pi-hole teleport file to the server.
    /// NOTE: Be careful if using this to sync settings between Pi-hole instances, 
    /// as it will overwrite settings that in most cases should be different between instances. 
    /// Use <see cref="PiholeTeleportImportSettings"/> to limit what is imported.
    /// Use the Config client for syncing configuration between replicas.
    /// </summary>
    Task<PiholeClientApiResponse<PiholeTeleportImportResponse>> PushPiholeTeleportFile(PiholeTeleportImportRequest importRequest, CancellationToken cancellationToken = default);
}
