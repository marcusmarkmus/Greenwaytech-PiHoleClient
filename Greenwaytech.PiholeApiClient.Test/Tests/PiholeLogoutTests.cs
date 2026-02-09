using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Tests for correctly logging out of the Pi-hole API.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PiholeLogoutTests
{
    [SetUp]
    public async Task Setup()
    {
        await PiholeTestContainerFixture.RestoreBaselineIfNeeded();
    }

    [TearDown]
    public async Task Teardown()
    {
        await PiholeTestContainerFixture.CleanupAfterModifyingTest();
    }

    [Test, Order(100)]
    public async Task DisposeServiceProvider_ShouldTerminateSession()
    {
        // Arrange
        PiHoleInstanceApiConfig config = PiholeTestContainerFixture.ServiceProvider.GetRequiredService<IOptions<PiHoleInstanceApiConfig>>().Value;

        // Act/Assert
        PiholeGetConfigResponse response = await EnsureRequestWorks(config);
        
        for (int i = 0; i < response.Config!.Webserver!.Api!.Max_sessions; i++)
            await EnsureRequestWorks(config);
    }
    
    private static async Task<PiholeGetConfigResponse> EnsureRequestWorks(PiHoleInstanceApiConfig config)
    {
        await using ServiceProvider serviceProvider = new ServiceCollection()
            .AddPiholeApiClient(options =>
            {
                options.ApiKey = config.ApiKey;
                options.ApiBaseUrl = config.ApiBaseUrl;
            })
            .BuildServiceProvider();
        var clientService = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        PiholeClientApiResponse<PiholeGetConfigResponse> response = await clientService.Config.GetPiholeConfigAsync();
        Assert.That(response.IsSuccess, Is.True);
        return response.Data!;
    }
}
