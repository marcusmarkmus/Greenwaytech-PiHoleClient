using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Pihole;

namespace Greenwaytech.PiholeApiClient.ApiClient.SubClients.Teleport;
public interface ITeleportClient
{
    Task<PiholeClientApiResponse<PiholeTeleportExport>> PullPiholeTeleportFile(CancellationToken cancellationToken = default);
}
