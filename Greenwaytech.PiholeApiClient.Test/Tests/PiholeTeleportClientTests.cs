using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Tests for Pi-hole Teleport client functionality including backup and restore operations.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PiholeTeleportClientTests
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

    [Test, Order(50)]
    public async Task TeleportPull_ShouldGetFile()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();

        // Act
        var result = await cut.Teleport.PullPiholeTeleportFile();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
    }

    [Test, Order(51)]
    [RequiresBaselineRestore]
    public async Task Teleport_Import_ShouldSucceed()
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

    private IPiholeApiClientService GeneratePiholeApiClientService()
    {
        return PiholeTestContainerFixture.ServiceProvider.GetRequiredService<IPiholeApiClientService>();
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
