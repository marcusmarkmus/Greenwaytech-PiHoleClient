using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;

namespace Greenwaytech.PiholeApiClient.Providers
{
    public interface IPiholeClientFactory
    {
        IPiholeApiClientService CreateClient(IPiHoleInstanceApiConfig piHoleInstanceApiConfig);
        IPiholeApiClientService CreateClient(PiholeNode node);
    }
}