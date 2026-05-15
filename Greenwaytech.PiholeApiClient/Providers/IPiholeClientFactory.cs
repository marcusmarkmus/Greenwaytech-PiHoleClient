using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;

namespace Greenwaytech.PiholeApiClient.Providers
{
    public interface IPiholeClientFactory : IDisposable, IAsyncDisposable
    {
        IPiholeApiClientService CreateClient(IPiHoleInstanceApiConfig piHoleInstanceApiConfig);
        IPiholeApiClientService CreateClient(PiholeNode node);
    }
}