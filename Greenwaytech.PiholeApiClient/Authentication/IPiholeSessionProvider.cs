using Greenwaytech.PiholeApiClient.Model.Pihole;

namespace Greenwaytech.PiholeApiClient.Authentication;
internal interface IPiholeSessionProvider
{
    Task<PiholeApiSession> GetValidSessionAsync();
}
