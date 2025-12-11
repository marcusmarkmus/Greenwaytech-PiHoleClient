using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Tests for dependency injection registration and thread-safety guarantees.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class DependencyInjectionTests
{
    #region AddPiholeApiClient Tests

    [Test]
    public void AddPiholeApiClient_CalledOnce_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => services.AddPiholeApiClient(config =>
        {
            config.ApiBaseUrl = "http://localhost:8080";
            config.ApiKey = "test-api-key";
        }));
    }

    [Test]
    public void AddPiholeApiClient_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddPiholeApiClient(config =>
        {
            config.ApiBaseUrl = "http://localhost:8080";
            config.ApiKey = "test-api-key";
        });

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddPiholeApiClient(config =>
        {
            config.ApiBaseUrl = "http://localhost:8081";
            config.ApiKey = "different-api-key";
        }));

        Assert.That(ex.Message, Does.Contain("already been called"));
        Assert.That(ex.Message, Does.Contain("AddPiholeApiClientFactory"));
    }

    [Test]
    public void AddPiholeApiClient_ResolvesAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddPiholeApiClient(config =>
        {
            config.ApiBaseUrl = "http://localhost:8080";
            config.ApiKey = "test-api-key";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var client1 = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        var client2 = serviceProvider.GetRequiredService<IPiholeApiClientService>();

        // Assert - Should be the SAME instance (singleton)
        Assert.That(client2, Is.SameAs(client1),
            "IPiholeApiClientService should be registered as singleton");
    }

    [Test]
    public async Task AddPiholeApiClient_ConcurrentResolutions_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddPiholeApiClient(config =>
        {
            config.ApiBaseUrl = "http://localhost:8080";
            config.ApiKey = "test-api-key";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act - Resolve concurrently
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => serviceProvider.GetRequiredService<IPiholeApiClientService>()))
            .ToArray();

        var clients = await Task.WhenAll(tasks);

        // Assert - All should be the same instance
        var firstClient = clients[0];
        Assert.That(clients.All(c => ReferenceEquals(c, firstClient)), Is.True,
            "All concurrent resolutions should return the same singleton instance");
    }

    #endregion

    #region AddPiholeApiClientFactory Tests

    [Test]
    public void AddPiholeApiClientFactory_ResolvesFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddPiholeApiClientFactory();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory = serviceProvider.GetRequiredService<IPiholeClientFactory>();

        // Assert
        Assert.That(factory, Is.Not.Null);
        Assert.That(factory, Is.InstanceOf<PiholeClientFactory>());
    }

    [Test]
    public void AddPiholeApiClientFactory_FactoryIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddPiholeApiClientFactory();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory1 = serviceProvider.GetRequiredService<IPiholeClientFactory>();
        var factory2 = serviceProvider.GetRequiredService<IPiholeClientFactory>();

        // Assert - Factory should be singleton
        Assert.That(factory2, Is.SameAs(factory1),
            "IPiholeClientFactory should be registered as singleton");
    }

    [Test]
    public void AddPiholeApiClientFactory_CreatedClientsAreCached()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddPiholeApiClientFactory();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IPiholeClientFactory>();

        var config = new PiHoleInstanceApiConfig
        {
            ApiBaseUrl = "http://localhost:8080",
            ApiKey = "test-api-key"
        };

        // Act
        var client1 = factory.CreateClient(config);
        var client2 = factory.CreateClient(config);

        // Assert - Same client instance returned
        Assert.That(client2, Is.SameAs(client1),
            "Factory should cache and return the same client for the same config");
    }

    [Test]
    public void AddPiholeApiClientFactory_DifferentConfigs_DifferentClients()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddPiholeApiClientFactory();

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IPiholeClientFactory>();

        var config1 = new PiHoleInstanceApiConfig
        {
            ApiBaseUrl = "http://pihole1.local",
            ApiKey = "api-key-1"
        };
        var config2 = new PiHoleInstanceApiConfig
        {
            ApiBaseUrl = "http://pihole2.local",
            ApiKey = "api-key-2"
        };

        // Act
        var client1 = factory.CreateClient(config1);
        var client2 = factory.CreateClient(config2);

        // Assert - Different clients for different configs
        Assert.That(client2, Is.Not.SameAs(client1),
            "Factory should return different clients for different configs");
    }

    #endregion

    #region Mixed Registration Tests

    [Test]
    public void AddPiholeApiClient_ThenAddPiholeApiClientFactory_BothWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        
        // Both can be registered (for different use cases)
        services.AddPiholeApiClient(config =>
        {
            config.ApiBaseUrl = "http://primary.local";
            config.ApiKey = "primary-key";
        });
        services.AddPiholeApiClientFactory();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var directClient = serviceProvider.GetRequiredService<IPiholeApiClientService>();
        var factory = serviceProvider.GetRequiredService<IPiholeClientFactory>();
        var factoryClient = factory.CreateClient(new PiHoleInstanceApiConfig
        {
            ApiBaseUrl = "http://secondary.local",
            ApiKey = "secondary-key"
        });

        // Assert - Both should work
        Assert.That(directClient, Is.Not.Null);
        Assert.That(factory, Is.Not.Null);
        Assert.That(factoryClient, Is.Not.Null);
        Assert.That(factoryClient, Is.Not.SameAs(directClient),
            "Factory-created client should be different from directly registered client");
    }

    #endregion
}
