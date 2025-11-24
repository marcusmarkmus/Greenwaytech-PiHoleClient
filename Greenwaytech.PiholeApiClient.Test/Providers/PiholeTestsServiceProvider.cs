using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Greenwaytech.PiholeApiClient.Test.Providers;

internal class PiholeTestsServiceProvider
{
   
    public void Initialize()
    {
        // Initialization logic can be added here if needed
    }
    public PiholeTestsServiceProvider()
    {
        
    }
    public static IPiholeApiClientService GetPiholeApiClientService(PiHoleInstanceApiConfig config)
    {
        var services = new ServiceCollection();
        
        services.AddPiholeApiClient(options =>
        {
            options.ApiKey = config.ApiKey;
            options.ApiBaseUrl = config.ApiBaseUrl;
        });
        var serviceProvider = services.BuildServiceProvider();
        var clientService = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        return clientService;
    }
    public static IPiholeClientFactory GetPiholeClientFactory()
    {
        var services = new ServiceCollection();
     

        var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IPiholeClientFactory>();
        return clientFactory;
    }

    internal static IPiholeApiClientService GetPiholeApiClientService(IOptions<PiHoleInstanceApiConfig> config) 
        => config is null
            ? throw new ArgumentNullException(nameof(config))
            : GetPiholeApiClientService(config.Value);
}
