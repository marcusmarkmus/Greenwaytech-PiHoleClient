using Greenwaytech.PiholeApiClient.Model.Pihole;

namespace Greenwaytech.PiholeApiClient.Authentication;
public interface IPiholeSessionProvider
{
    Task<PiholeApiSession> GetValidSessionAsync();
}
