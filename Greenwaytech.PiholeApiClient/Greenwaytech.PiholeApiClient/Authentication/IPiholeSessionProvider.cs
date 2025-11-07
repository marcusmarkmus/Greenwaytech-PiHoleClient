using Greenwaytech.PiholeApiClient.Model;

namespace Greenwaytech.PiholeApiClient.Authentication;
public interface IPiholeSessionProvider
{
    Task<PiholeApiSession> GetValidSessionAsync();
}
