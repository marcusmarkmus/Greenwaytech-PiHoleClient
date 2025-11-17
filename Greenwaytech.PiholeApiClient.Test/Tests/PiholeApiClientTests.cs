using DotNet.Testcontainers.Containers;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Test.Extensions;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

public class PiholeApiClientTests
{

    private IContainer _container;
    private string _baseUrl;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        //All tests use the same container instance for speed - but be aware that tests may affect each other due to shared state.
        _container = PiholeTestInstanceProvider.BuildPiholeTestContainer();

        await _container.StartAsync()
          .ConfigureAwait(false);

       _baseUrl = _container.GetPiholeTestContainerBaseUrl(PiholeTestInstanceProvider.PiholePort);

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
        Assert.That(config?.Config?.Database, Is.Not.Null); 

    }

    [Test]
    public async Task PiholeClient_Config_PatchLocalDnsRecordAndReadBack_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        var getResult = await cut.Config.GetPiholeConfigAsync();
        Assert.That(getResult.IsSuccess, Is.True, getResult.ErrorMessage);
        var originalConfig = getResult.Data?.Config;
        Assert.That(originalConfig, Is.Not.Null);

        
        var blockTTLToSet = 5;
        var localDnsRecordToAdd = "10.10.0.100 exampledomain.local";
        string[] hostsListToPatch = [.. originalConfig.Dns?.Hosts ?? [], .. new[] { localDnsRecordToAdd }];

        var patchRequest = new Model.Pihole.DTO.PiholePatchConfigRequest
        {
            Config = new Model.Pihole.PiholeConfigModel
            {
                Dns = new Model.Pihole.Dns
                {
                    BlockTTL = blockTTLToSet,
                    Hosts = hostsListToPatch
                },
            }
        };

        // Act
        var patchResult = await cut.Config.PatchPiholeConfigAsync(patchRequest);
        Assert.That(patchResult.IsSuccess, Is.True, patchResult.ErrorMessage);

        // Read back
        var getResultAfter = await cut.Config.GetPiholeConfigAsync();
        Assert.That(getResultAfter.IsSuccess, Is.True, getResultAfter.ErrorMessage);
        var updatedConfig = getResultAfter.Data?.Config;
        Assert.That(updatedConfig, Is.Not.Null);
        Assert.That(updatedConfig.Dns?.BlockTTL, Is.EqualTo(blockTTLToSet));
        Assert.That(updatedConfig.Dns?.Hosts, Does.Contain(localDnsRecordToAdd));
    }

    [Test]
    public async Task PiholeClient_Teleport_Import_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        var teleportFileStream = GetPiholeTeleportFileData();
        var knownConfigChangeInTeleportFileHosts = "10.10.10.10 testrecord.local";
        Assert.That(teleportFileStream, Is.Not.Null, "Could not get Teleportfile for testing.");
        var exportData = teleportFileStream;
        Assert.That(exportData, Is.Not.Null);
        Assert.That(exportData, Is.Not.Null.And.Not.Empty);

        // Act: Import the teleport file back
        var importRequest = new Model.Pihole.DTO.PiholeTeleportImportRequest
        {
            File = exportData,
            PiholeTeleportImportSettings = null // import all
        };
        var importResult = await cut.Teleport.PushPiholeTeleportFile(importRequest);

        // Assert
        Assert.That(importResult, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(importResult.IsSuccess, Is.True, importResult.ErrorMessage);
            Assert.That(importResult.Data, Is.Not.Null);
        }
        Assert.That(importResult.Data.Error, Is.Null, $"Import error: {importResult.Data.Error?.Message}");

        var configResult = await cut.Config.GetPiholeConfigAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(configResult.IsSuccess, Is.True, configResult.ErrorMessage);
            Assert.That(configResult.Data, Is.Not.Null);
        }
        Assert.That(configResult.Data.Config, Is.Not.Null);
        Assert.That(configResult.Data.Config.Dns?.Hosts, Does.Contain(knownConfigChangeInTeleportFileHosts), "Imported config hosts does not contain expected value from teleport file.");

    }


    private IOptions<PiHoleInstanceApiConfig> GetPiholeConfigOptions() 
        => Options.Create(new PiHoleInstanceApiConfig
    {
        ApiBaseUrl = _baseUrl,
        ApiKey = PiholeTestInstanceProvider.PiholePassword
    });

    private IPiholeApiClientService GeneratePiholeApiClientService()
    {
        var config = GetPiholeConfigOptions();
        return PiholeTestsServiceProvider.GetPiholeApiClientService(config);
    }
    

    private static byte[] GetPiholeTeleportFileData()
    {
        byte[] zipBytes;
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("Greenwaytech.PiholeApiClient.Test.TestData.TeleportExport.zip"))
        {
            if (stream is null)
                throw new FileNotFoundException("Embedded resource not found.");

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            zipBytes = memoryStream.ToArray();

        };
        return zipBytes;
    }
}
