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
}
