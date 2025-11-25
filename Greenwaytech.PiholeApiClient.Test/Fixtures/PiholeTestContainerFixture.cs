using DotNet.Testcontainers.Containers;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Test.Extensions;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Shared test fixture that manages a single Pi-hole test container for all test classes.
/// This ensures faster test execution by starting the container only once.
/// </summary>
[SetUpFixture]
public class PiholeTestContainerFixture
{
    /// <summary>
    /// Shared container instance used by all test classes.
    /// </summary>
    public static IContainer Container { get; private set; } = null!;

    /// <summary>
    /// Base URL for the Pi-hole test container API.
    /// </summary>
    public static string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Shared service provider for all tests to ensure authentication session is maintained.
    /// </summary>
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// Starts the Pi-hole test container once before any tests run.
    /// </summary>
    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // All tests use the same container instance for speed - but be aware that tests may affect each other due to shared state.
        Container = PiholeTestInstanceProvider.BuildPiholeTestContainer();

        await Container.StartAsync()
          .ConfigureAwait(false);

        BaseUrl = Container.GetPiholeTestContainerBaseUrl(PiholeTestInstanceProvider.PiholePort);

        // Create shared service provider with authentication
        var services = new ServiceCollection();
        services.AddPiholeApiClient(options =>
        {
            options.ApiKey = PiholeTestInstanceProvider.PiholePassword;
            options.ApiBaseUrl = BaseUrl;
        });
        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Stops and disposes the Pi-hole test container after all tests complete.
    /// </summary>
    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (Container is not null)
        {
            await Container.StopAsync();
            await Container.DisposeAsync();
        }
    }
}
