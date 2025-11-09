using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Authentication;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Test.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenwaytech.PiholeApiClient.Test;

public class PiholeApiClientTests
{
    private const string PiholeImage = "pihole/pihole:latest";
    private const string PiholePassword = "testpassword";
    private const string PiholeWebserverPasswordEnvVar = "FTLCONF_webserver_api_password";
    private const int PiholePort = 80;
    private IContainer _container;
    private string _baseUrl;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        _container = BuildPiholeTestContainer();

        await _container.StartAsync()
          .ConfigureAwait(false);

       _baseUrl = _container.GetPiholeTestContainerBaseUrl(PiholePort);

    }

    

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    [Test]
    public async Task PiholeClient_TeleportPull_ShouldGetFile()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act
        var result = await cut.Teleport.PullPiholeTeleportFile();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
    }

    [Test]
    public async Task PiholeClient_Config_ShouldGetConfig()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        // Act
        var result = await cut.Config.GetPiholeConfigAsync();
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        var config = result.Data;
        Assert.That(config?.Config, Is.Not.Null);
        Assert.That(config?.Config?.database, Is.Not.Null); //Should never be null on fresh container?

    }

    //todo: more tests,
    // - Upload Teleport file
    // - write config, and read for assertion

    private IOptions<PiHoleInstanceApiConfig> GetPiholeConfigOptions() 
        => Options.Create(new PiHoleInstanceApiConfig
    {
        ApiBaseUrl = _baseUrl,
        ApiKey = PiholePassword
    });
    private PiholeAuthHandler CreatePiholeAuthHandler()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var config = GetPiholeConfigOptions();
        var sessionProvider = new PiholeSessionProvider(new HttpClient { BaseAddress = new Uri(_baseUrl) }, loggerFactory.CreateLogger<PiholeSessionProvider>(), config);
        return new PiholeAuthHandler(loggerFactory.CreateLogger<PiholeAuthHandler>(), sessionProvider)
        {
            InnerHandler = new HttpClientHandler()
        };
    }
    private PiholeApiClientService GeneratePiholeApiClientService()
    {
        var config = GetPiholeConfigOptions();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<PiholeApiClientService>();
        var httpClient = new HttpClient(CreatePiholeAuthHandler()) { BaseAddress = new Uri(config.Value.ApiBaseUrl) };
        return new PiholeApiClientService(httpClient, logger, config);
    }
    private static IContainer BuildPiholeTestContainer()
        => new ContainerBuilder()
          .WithImage(PiholeImage)
          .WithPortBinding(PiholePort, true)
          .WithEnvironment(PiholeWebserverPasswordEnvVar, PiholePassword)
          .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(PiholePort))
          .Build();
}
