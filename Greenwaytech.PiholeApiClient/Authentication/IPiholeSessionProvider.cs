using Greenwaytech.PiholeApiClient.Model.Pihole;

namespace Greenwaytech.PiholeApiClient.Authentication;
internal interface IPiholeSessionProvider : IDisposable, IAsyncDisposable
{
    Task<PiholeApiSession> GetValidSessionAsync();
}
