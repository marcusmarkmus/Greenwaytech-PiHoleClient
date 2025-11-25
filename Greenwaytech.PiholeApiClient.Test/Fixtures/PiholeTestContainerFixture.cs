using DotNet.Testcontainers.Containers;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Test.Extensions;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Attribute to mark tests that modify Pi-hole configuration and need baseline restore
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequiresBaselineRestoreAttribute : Attribute { }

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
    /// Baseline configuration exported at startup, used to restore state between tests
    /// </summary>
    private static byte[]? _baselineConfig;

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

        // Small delay to ensure all background services are fully initialized
        await Task.Delay(5000);

        // Create shared service provider with authentication
        var services = new ServiceCollection();
        services.AddPiholeApiClient(options =>
        {
            options.ApiKey = PiholeTestInstanceProvider.PiholePassword;
            options.ApiBaseUrl = BaseUrl;
        });
        ServiceProvider = services.BuildServiceProvider();

        // Export baseline configuration to restore between tests
        var client = ServiceProvider.GetRequiredService<IPiholeApiClientService>();
        var exportResult = await client.Teleport.PullPiholeTeleportFile();
        if (exportResult.IsSuccess && exportResult.Data?.Data != null)
        {
            _baselineConfig = exportResult.Data.Data;
            Console.WriteLine("Baseline configuration exported successfully");
        }
        else
        {
            Console.WriteLine($"Warning: Failed to export baseline config: {exportResult.ErrorMessage}");
        }
    }

    /// <summary>
    /// Restores baseline configuration before tests that modify Pi-hole state.
    /// Call this from [SetUp] in test classes for tests marked with [RequiresBaselineRestore].
    /// </summary>
    public static async Task RestoreBaselineIfNeeded()
    {
        // Check if current test needs baseline restore
        var currentTest = TestContext.CurrentContext.Test;
        var needsRestore = currentTest?.Method?.GetCustomAttributes<RequiresBaselineRestoreAttribute>(true).Length > 0;

        if (!needsRestore)
        {
            // Read-only test, no restoration needed
            return;
        }

        // Restore baseline configuration for tests that modify state
        if (_baselineConfig != null)
        {
            var client = ServiceProvider.GetRequiredService<IPiholeApiClientService>();
            var importRequest = new Model.Pihole.DTO.PiholeTeleportImportRequest
            {
                File = _baselineConfig,
                PiholeTeleportImportSettings = null // import all to fully reset
            };

            var importResult = await client.Teleport.PushPiholeTeleportFile(importRequest);
            if (!importResult.IsSuccess)
            {
                // Log warning but don't fail - some tests might still work
                Console.WriteLine($"Warning: Failed to restore baseline config: {importResult.ErrorMessage}");
            }

            // Delay to let API sessions cleanup
            await Task.Delay(2000);
        }
    }

    /// <summary>
    /// Cleanup delay after tests that modified configuration.
    /// Call this from [TearDown] in test classes for tests marked with [RequiresBaselineRestore].
    /// </summary>
    public static async Task CleanupAfterModifyingTest()
    {
        var currentTest = TestContext.CurrentContext.Test;
        var needsRestore = currentTest?.Method?.GetCustomAttributes<RequiresBaselineRestoreAttribute>(true).Length > 0;

        if (needsRestore)
        {
            // Give API sessions time to cleanup
            await Task.Delay(1000);
        }
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
