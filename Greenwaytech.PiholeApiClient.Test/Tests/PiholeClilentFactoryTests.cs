using DotNet.Testcontainers.Containers;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Providers;
using Greenwaytech.PiholeApiClient.Test.Extensions;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class PiholeClilentFactoryTests
{
    private string _baseUrl => "http://localhost:8080";
    private string _baseUrl2 => "http://localhost:8081";

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {

    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
    
    }

    [Test]
    public void CreateClient_WithValidConfig_ReturnsClient()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = PiholeTestInstanceProvider.PiholePassword };

        // Act
        var client = factory.CreateClient(config);

        // Assert
        Assert.That(client, Is.Not.Null);
        Assert.That(client.Config, Is.Not.Null);
        Assert.That(client.Teleport, Is.Not.Null);
    }

    [Test]
    public void CreateClient_WithMissingApiBaseUrl_ThrowsArgumentException()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config = new PiHoleInstanceApiConfig { ApiBaseUrl = "", ApiKey = PiholeTestInstanceProvider.PiholePassword };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => factory.CreateClient(config));
        Assert.That(ex?.ParamName, Is.EqualTo("ApiBaseUrl"));
    }

    [Test]
    public void CreateClient_WithMissingApiKey_ThrowsArgumentException()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => factory.CreateClient(config));
        Assert.That(ex?.ParamName, Is.EqualTo("ApiKey"));
    }

    [Test]
    public void CreateClient_WithValidPiholeNode_ReturnsClient()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var node = new PiholeNode
        {
            Name = "TestNode",
            Config = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = PiholeTestInstanceProvider.PiholePassword },
            Description = "Test node"
        };

        // Act
        var client = factory.CreateClient(node);

        // Assert
        Assert.That(client, Is.Not.Null);
        Assert.That(client.Config, Is.Not.Null);
        Assert.That(client.Teleport, Is.Not.Null);
    }

    [Test]
    public void CreateClient_ReturnedClient_CanCallConfigAndTeleport()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = PiholeTestInstanceProvider.PiholePassword };

        // Act
        var client = factory.CreateClient(config);

        // Assert
        Assert.That(client.Config, Is.Not.Null);
        Assert.That(client.Teleport, Is.Not.Null);
    }

    #region Client Caching Tests

    [Test]
    public void CreateClient_SameConfig_ReturnsSameInstance()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "test-api-key" };

        // Act
        var client1 = factory.CreateClient(config);
        var client2 = factory.CreateClient(config);

        // Assert - Should return the SAME cached instance
        Assert.That(client2, Is.SameAs(client1), 
            "CreateClient should return the same cached instance for the same config");
    }

    [Test]
    public void CreateClient_DifferentBaseUrl_ReturnsDifferentInstances()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config1 = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "test-api-key" };
        var config2 = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl2, ApiKey = "test-api-key" };

        // Act
        var client1 = factory.CreateClient(config1);
        var client2 = factory.CreateClient(config2);

        // Assert - Should return DIFFERENT instances for different Pi-holes
        Assert.That(client2, Is.Not.SameAs(client1), 
            "CreateClient should return different instances for different base URLs");
    }

    [Test]
    public void CreateClient_DifferentApiKey_ReturnsDifferentInstances()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config1 = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "api-key-1" };
        var config2 = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "api-key-2" };

        // Act
        var client1 = factory.CreateClient(config1);
        var client2 = factory.CreateClient(config2);

        // Assert - Different API keys should be treated as different instances
        Assert.That(client2, Is.Not.SameAs(client1), 
            "CreateClient should return different instances for different API keys");
    }

    [Test]
    public void CreateClient_UrlWithTrailingSlash_ReturnsSameInstanceAsWithout()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config1 = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "test-api-key" };
        var config2 = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl + "/", ApiKey = "test-api-key" };

        // Act
        var client1 = factory.CreateClient(config1);
        var client2 = factory.CreateClient(config2);

        // Assert - Trailing slash should be normalized
        Assert.That(client2, Is.SameAs(client1), 
            "CreateClient should normalize URLs and return same instance regardless of trailing slash");
    }

    [Test]
    public async Task CreateClient_ConcurrentCalls_ReturnsSameInstance()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        var factory = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "test-api-key" };

        // Act - Fire multiple concurrent CreateClient calls
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => factory.CreateClient(config)))
            .ToArray();
        
        var clients = await Task.WhenAll(tasks);

        // Assert - All should return the SAME instance
        var firstClient = clients[0];
        Assert.That(clients.All(c => ReferenceEquals(c, firstClient)), Is.True,
            "All concurrent CreateClient calls should return the same cached instance");
    }

    [Test]
    public void CreateClient_MultipleFactoryInstances_EachHasOwnCache()
    {
        // Arrange
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
        var factory1 = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var factory2 = new PiholeClientFactory(loggerFactory, httpClientFactory);
        var config = new PiHoleInstanceApiConfig { ApiBaseUrl = _baseUrl, ApiKey = "test-api-key" };

        // Act
        var clientFromFactory1 = factory1.CreateClient(config);
        var clientFromFactory2 = factory2.CreateClient(config);

        // Assert - Different factory instances have their own cache
        // This is expected behavior - the factory itself should be a singleton in DI
        Assert.That(clientFromFactory2, Is.Not.SameAs(clientFromFactory1), 
            "Different factory instances should have separate caches");
    }

    #endregion
}
