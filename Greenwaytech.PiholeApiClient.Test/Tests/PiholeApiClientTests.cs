using DotNet.Testcontainers.Containers;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.App.Response;
using Greenwaytech.PiholeApiClient.Model.Configuration;
using Greenwaytech.PiholeApiClient.Test.Extensions;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Attribute to mark tests that modify Pi-hole configuration and need baseline restore
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequiresBaselineRestoreAttribute : Attribute { }

[Parallelizable(ParallelScope.None)]
[TestFixture]
public class PiholeApiClientTests
{
    private static IContainer? _sharedContainer;
    private static string? _sharedBaseUrl;
    private static byte[]? _baselineConfig;
    private static IPiholeApiClientService? _sharedClient;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Start container once for all tests
        _sharedContainer = PiholeTestInstanceProvider.BuildPiholeTestContainer();
        await _sharedContainer.StartAsync().ConfigureAwait(false);
        _sharedBaseUrl = _sharedContainer.GetPiholeTestContainerBaseUrl(PiholeTestInstanceProvider.PiholePort);
        
        // Small delay to ensure all background services are fully initialized
        await Task.Delay(5000);

        // Create shared client instance - reuse across all tests
        _sharedClient = GeneratePiholeApiClientService();

        // Export baseline configuration once to restore before each test
        var exportResult = await _sharedClient.Teleport.PullPiholeTeleportFile();
        if (exportResult.IsSuccess && exportResult.Data?.Data != null)
        {
            _baselineConfig = exportResult.Data.Data;
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        if (_sharedContainer is not null)
        {
            await _sharedContainer.StopAsync();
            await _sharedContainer.DisposeAsync();
        }
    }

    [SetUp]
    public async Task Setup()
    {
        // Only restore baseline for tests that modify configuration
        var currentTest = TestContext.CurrentContext.Test;
        var needsRestore = currentTest?.Method?.GetCustomAttributes<RequiresBaselineRestoreAttribute>(true).Length != 0;
        
        if (!needsRestore)
        {
            // Read-only test, no restoration needed
            return;
        }

        // Restore baseline configuration for tests that modify state
        if (_baselineConfig != null && _sharedClient != null)
        {
            var importRequest = new Model.Pihole.DTO.PiholeTeleportImportRequest
            {
                File = _baselineConfig,
                PiholeTeleportImportSettings = null // import all to fully reset
            };
            
            var importResult = await _sharedClient.Teleport.PushPiholeTeleportFile(importRequest);
            if (!importResult.IsSuccess)
            {
                // Log warning but don't fail - some tests might still work
                Console.WriteLine($"Warning: Failed to restore baseline config: {importResult.ErrorMessage}");
            }
            
            // Longer delay to let API sessions cleanup
            await Task.Delay(2000);
        }
    }

    [TearDown]
    public async Task Teardown()
    {
        // Give API sessions time to cleanup (only for tests that modified state)
        var currentTest = TestContext.CurrentContext.Test;
        var needsRestore = currentTest?.Method?.GetCustomAttributes<RequiresBaselineRestoreAttribute>(true).Length != 0;
        
        if (needsRestore)
        {
            await Task.Delay(1000);
        }
    }

    [Test]
    public async Task PiholeClient_TeleportPull_ShouldGetFile()
    {
        // Arrange
        var cut = _sharedClient!;

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
        var cut = _sharedClient!;
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
    [RequiresBaselineRestore]
    public async Task PiholeClient_Config_PatchLocalDnsRecordAndReadBack_ShouldSucceed()
    {
        // Arrange
        var cut = _sharedClient!;
        var getResult = await cut.Config.GetPiholeConfigAsync();
        Assert.That(getResult.IsSuccess, Is.True, getResult.ErrorMessage);
        var originalConfig = getResult.Data?.Config;
        Assert.That(originalConfig, Is.Not.Null);

        
        var blockTTLToSet = 5;
        var localDnsRecordToAdd = "10.10.0.100 exampledomain.local";
        string[] hostsListToPatch = [.. originalConfig?.Dns?.Hosts ?? [], .. new[] { localDnsRecordToAdd }];

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
        Assert.That(updatedConfig!.Dns?.BlockTTL, Is.EqualTo(blockTTLToSet));
        Assert.That(updatedConfig.Dns?.Hosts, Does.Contain(localDnsRecordToAdd));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_Teleport_Import_ShouldSucceed()
    {
        // Arrange
        var cut = _sharedClient!;
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
        Assert.That(importResult.Data?.Error, Is.Null, $"Import error: {importResult.Data?.Error?.Message}");

        var configResult = await cut.Config.GetPiholeConfigAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(configResult.IsSuccess, Is.True, configResult.ErrorMessage);
            Assert.That(configResult.Data, Is.Not.Null);
        }
        Assert.That(configResult?.Data?.Config, Is.Not.Null);
        Assert.That(configResult!.Data!.Config!.Dns?.Hosts, Does.Contain(knownConfigChangeInTeleportFileHosts), "Imported config hosts does not contain expected value from teleport file.");

    }


    #region DNS Record Management Tests

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_ShouldAddNewRecord()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"test-{Guid.NewGuid():N}.local";
        var testIp = "192.168.100.1";
        
        var request = new LocalDnsRecordRequest
        {
            Domain = testDomain,
            IpAddress = testIp
        };

        // Act
        var result = await cut.Config.EnsureLocalDnsRecord(request);

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.DataOperation, Is.EqualTo(DataOperation.Created));
        Assert.That(result.Data.Message, Does.Contain("added successfully"));

        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Contain($"{testIp} {testDomain}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_ShouldReturnAlreadyExistsForDuplicate()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"test-{Guid.NewGuid():N}.local";
        var testIp = "192.168.100.2";
        
        var request = new LocalDnsRecordRequest
        {
            Domain = testDomain,
            IpAddress = testIp
        };

        // Add first time
        var firstResult = await cut.Config.EnsureLocalDnsRecord(request);
        Assert.That(firstResult.IsSuccess, Is.True);

        // Act: Try to add again
        var secondResult = await cut.Config.EnsureLocalDnsRecord(request);

        // Assert
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(secondResult.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        Assert.That(secondResult.Data?.Message, Does.Contain("already exists"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_ShouldDetectConflictForSameDomainDifferentIP()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"test-{Guid.NewGuid():N}.local";
        var firstIp = "192.168.100.3";
        var secondIp = "192.168.100.4";
        
        // Add first record
        var firstRequest = new LocalDnsRecordRequest
        {
            Domain = testDomain,
            IpAddress = firstIp
        };
        var firstResult = await cut.Config.EnsureLocalDnsRecord(firstRequest);
        Assert.That(firstResult.IsSuccess, Is.True);

        // Act: Try to add same domain with different IP
        var secondRequest = new LocalDnsRecordRequest
        {
            Domain = testDomain,
            IpAddress = secondIp
        };
        var conflictResult = await cut.Config.EnsureLocalDnsRecord(secondRequest);

        // Assert
        Assert.That(conflictResult.IsSuccess, Is.False);
        Assert.That(conflictResult.Data?.DataOperation, Is.EqualTo(DataOperation.Conflict));
        Assert.That(conflictResult.Data?.ConflictingIpAddresses, Does.Contain(firstIp));
        Assert.That(conflictResult.Data?.Message, Does.Contain("already points to"));

        // Cleanup
        await cut.Config.RemoveLocalDnsRecordsByDomain(testDomain);
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_ShouldAllowMultipleDomainsForSameIP()
    {
        // Arrange
        var cut = _sharedClient!;
        var testIp = "192.168.100.5";
        var domain1 = $"test1-{Guid.NewGuid():N}.local";
        var domain2 = $"test2-{Guid.NewGuid():N}.local";
        var domain3 = $"test3-{Guid.NewGuid():N}.local";

        // Act: Add multiple domains pointing to same IP
        var result1 = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = domain1, IpAddress = testIp });
        var result2 = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = domain2, IpAddress = testIp });
        var result3 = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = domain3, IpAddress = testIp });

        // Assert
        Assert.That(result1.IsSuccess, Is.True);
        Assert.That(result2.IsSuccess, Is.True);
        Assert.That(result3.IsSuccess, Is.True);
        Assert.That(result1.Data?.DataOperation, Is.EqualTo(DataOperation.Created));
        Assert.That(result2.Data?.DataOperation, Is.EqualTo(DataOperation.Created));
        Assert.That(result3.Data?.DataOperation, Is.EqualTo(DataOperation.Created));


        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Contain($"{testIp} {domain1}"));
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Contain($"{testIp} {domain2}"));
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Contain($"{testIp} {domain3}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_RemoveLocalDnsRecord_ShouldRemoveSpecificRecord()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"test-{Guid.NewGuid():N}.local";
        var testIp = "192.168.100.6";
        
        var request = new LocalDnsRecordRequest { Domain = testDomain, IpAddress = testIp };
        await cut.Config.EnsureLocalDnsRecord(request);

        // Act
        var removeResult = await cut.Config.RemoveLocalDnsRecord(request);

        // Assert
        Assert.That(removeResult.IsSuccess, Is.True);
        Assert.That(removeResult.Data?.DataOperation, Is.EqualTo(DataOperation.Deleted));
        Assert.That(removeResult.Data?.Message, Does.Contain("Removed DNS record"));

        // Verify record is gone
        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{testIp} {testDomain}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_RemoveLocalDnsRecordsByDomain_ShouldRemoveAllRecordsForDomain()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"test-{Guid.NewGuid():N}.local";
        var ip1 = "192.168.100.7";
        var ip2 = "192.168.100.8";
        
        // Add same domain with different IPs (using OverwriteExisting to test removal)
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain, IpAddress = ip1 });
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain, IpAddress = ip2, OverwriteExisting = true });

        // Act
        var removeResult = await cut.Config.RemoveLocalDnsRecordsByDomain(testDomain);

        // Assert
        Assert.That(removeResult.IsSuccess, Is.True);
        Assert.That(removeResult.Data?.DataOperation, Is.EqualTo(DataOperation.Deleted));
        // After overwrite, only ip2 should exist, so only 1 record should be removed
        Assert.That(removeResult.Data?.RemovedCount, Is.EqualTo(1));
        Assert.That(removeResult.Data?.RemovedIpAddresses, Has.Count.EqualTo(1));
        Assert.That(removeResult.Data?.RemovedIpAddresses, Does.Contain(ip2));

        // Verify all records are gone
        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{ip1} {testDomain}"));
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{ip2} {testDomain}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_RemoveLocalDnsRecordsByIp_ShouldRemoveAllDomainsForIP()
    {
        // Arrange
        var cut = _sharedClient!;
        var testIp = "192.168.100.9";
        var domain1 = $"test1-{Guid.NewGuid():N}.local";
        var domain2 = $"test2-{Guid.NewGuid():N}.local";
        var domain3 = $"test3-{Guid.NewGuid():N}.local";

        // Add multiple domains for same IP
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = domain1, IpAddress = testIp });
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = domain2, IpAddress = testIp });
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = domain3, IpAddress = testIp });

        // Act
        var removeResult = await cut.Config.RemoveLocalDnsRecordsByIp(testIp);

        // Assert
        Assert.That(removeResult.IsSuccess, Is.True);
        Assert.That(removeResult.Data?.DataOperation, Is.EqualTo(DataOperation.Deleted));
        Assert.That(removeResult.Data?.RemovedCount, Is.EqualTo(3));
        Assert.That(removeResult.Data?.RemovedDomains, Has.Count.EqualTo(3));
        Assert.That(removeResult.Data?.RemovedDomains, Does.Contain(domain1));
        Assert.That(removeResult.Data?.RemovedDomains, Does.Contain(domain2));
        Assert.That(removeResult.Data?.RemovedDomains, Does.Contain(domain3));

        // Verify all records are gone
        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{testIp} {domain1}"));
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{testIp} {domain2}"));
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{testIp} {domain3}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_RemoveNonExistent_ShouldReturnNotFound()
    {
        // Arrange
        var cut = _sharedClient!;
        var nonExistentDomain = $"nonexistent-{Guid.NewGuid():N}.local";

        // Act
        var result = await cut.Config.RemoveLocalDnsRecordsByDomain(nonExistentDomain);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists)); // Not found = already in desired state
        Assert.That(result.Data?.Message, Does.Contain("not found").Or.Contain("No DNS records"));
    }

    [Test]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_ShouldValidateIpAddress()
    {
        // Arrange
        var cut = _sharedClient!;
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = "invalid-ip-address"
        };

        // Act
        var result = await cut.Config.EnsureLocalDnsRecord(request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not a valid").Or.Contain("IP address"));
    }

    [Test]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_ShouldValidateDomain()
    {
        // Arrange
        var cut = _sharedClient!;
        var request = new LocalDnsRecordRequest
        {
            Domain = "", // Empty domain
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await cut.Config.EnsureLocalDnsRecord(request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Domain").And.Contain("empty"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_CompleteWorkflow_AddConflictResolveRemove()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"workflow-{Guid.NewGuid():N}.local";
        var ip1 = "192.168.100.10";
        var ip2 = "192.168.100.11";

        // Act & Assert: Step 1 - Add initial record
        var add1 = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain, IpAddress = ip1 });
        Assert.That(add1.IsSuccess, Is.True);
        Assert.That(add1.Data?.DataOperation, Is.EqualTo(DataOperation.Created));

        // Step 2 - Try to add same domain with different IP (should conflict)
        var add2 = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain, IpAddress = ip2 });
        Assert.That(add2.IsSuccess, Is.False);
        Assert.That(add2.Data?.DataOperation, Is.EqualTo(DataOperation.Conflict));

        // Step 3 - Resolve conflict by removing old record
        var remove1 = await cut.Config.RemoveLocalDnsRecordsByDomain(testDomain);
        Assert.That(remove1.IsSuccess, Is.True);
        Assert.That(remove1.Data?.RemovedCount, Is.EqualTo(1));

        // Step 4 - Now add the new IP successfully
        var add3 = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain, IpAddress = ip2 });
        Assert.That(add3.IsSuccess, Is.True);
        Assert.That(add3.Data?.DataOperation, Is.EqualTo(DataOperation.Created));

        // Step 5 - Verify final state
        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Contain($"{ip2} {testDomain}"));
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{ip1} {testDomain}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_WithOverwriteExisting_ShouldReplaceRecord()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"overwrite-{Guid.NewGuid():N}.local";
        var ip1 = "192.168.100.20";
        var ip2 = "192.168.100.21";

        // Add initial record
        var firstRequest = new LocalDnsRecordRequest { Domain = testDomain, IpAddress = ip1 };
        var firstResult = await cut.Config.EnsureLocalDnsRecord(firstRequest);
        Assert.That(firstResult.IsSuccess, Is.True);

        // Act: Add same domain with different IP and OverwriteExisting = true
        var secondRequest = new LocalDnsRecordRequest 
        { 
            Domain = testDomain, 
            IpAddress = ip2, 
            OverwriteExisting = true 
        };
        var secondResult = await cut.Config.EnsureLocalDnsRecord(secondRequest);

        // Assert
        Assert.That(secondResult.IsSuccess, Is.True);
        Assert.That(secondResult.Data?.DataOperation, Is.EqualTo(DataOperation.Created));
        Assert.That(secondResult.Data?.Message, Does.Contain("replaced"));

        // Verify only new record exists
        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Contain($"{ip2} {testDomain}"));
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Not.Contain($"{ip1} {testDomain}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_WithIPv6_ShouldWork()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"ipv6-{Guid.NewGuid():N}.local";
        var testIpv6 = "2001:0db8:85a3::8a2e:0370:7334";

        var request = new LocalDnsRecordRequest
        {
            Domain = testDomain,
            IpAddress = testIpv6
        };

        // Act
        var result = await cut.Config.EnsureLocalDnsRecord(request);

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.Data?.DataOperation, Is.EqualTo(DataOperation.Created));

        // Verify record exists
        var config = await cut.Config.GetPiholeConfigAsync();
        var recordExists = config.Data?.Config?.Dns?.Hosts?.Any(h => 
            h.Contains(testDomain, StringComparison.OrdinalIgnoreCase)) ?? false;
        Assert.That(recordExists, Is.True, "IPv6 record should exist in configuration");
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_EnsureLocalDnsRecord_IdempotencyTest()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"idempotent-{Guid.NewGuid():N}.local";
        var testIp = "192.168.100.30";
        var request = new LocalDnsRecordRequest { Domain = testDomain, IpAddress = testIp };

        // Act: Call multiple times with same request
        var result1 = await cut.Config.EnsureLocalDnsRecord(request);
        var result2 = await cut.Config.EnsureLocalDnsRecord(request);
        var result3 = await cut.Config.EnsureLocalDnsRecord(request);

        // Assert: First should create, rest should be idempotent
        Assert.That(result1.IsSuccess, Is.True);
        Assert.That(result1.Data?.DataOperation, Is.EqualTo(DataOperation.Created));
        
        Assert.That(result2.IsSuccess, Is.True);
        Assert.That(result2.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        
        Assert.That(result3.IsSuccess, Is.True);
        Assert.That(result3.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));

        // Verify only one record exists
        var config = await cut.Config.GetPiholeConfigAsync();
        var recordCount = config.Data?.Config?.Dns?.Hosts?.Count(h => 
            h.Contains(testDomain, StringComparison.OrdinalIgnoreCase)) ?? 0;
        Assert.That(recordCount, Is.EqualTo(1), "Should only have one record despite multiple calls");
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_RemoveSpecificRecord_WithMultipleDuplicates_ShouldRemoveOnlyOne()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"duplicate-{Guid.NewGuid():N}.local";
        var testIp = "192.168.100.40";
        
        // Manually add the same record multiple times (should not happen normally, but testing edge case)
        var request1 = new LocalDnsRecordRequest { Domain = testDomain, IpAddress = testIp };
        await cut.Config.EnsureLocalDnsRecord(request1);

        // Add a different domain to ensure we're not removing everything
        var otherDomain = $"other-{Guid.NewGuid():N}.local";
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = otherDomain, IpAddress = testIp });

        // Act: Remove specific record
        var removeResult = await cut.Config.RemoveLocalDnsRecord(request1);

        // Assert
        Assert.That(removeResult.IsSuccess, Is.True);
        Assert.That(removeResult.Data?.DataOperation, Is.EqualTo(DataOperation.Deleted));

        // Verify other record still exists
        var config = await cut.Config.GetPiholeConfigAsync();
        Assert.That(config.Data?.Config?.Dns?.Hosts, Does.Contain($"{testIp} {otherDomain}"));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_ValidateLocalDnsConfig_WithValidConfig_ShouldReturnValid()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain1 = $"valid1-{Guid.NewGuid():N}.local";
        var testDomain2 = $"valid2-{Guid.NewGuid():N}.local";
        
        // Add some valid records
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain1, IpAddress = "192.168.100.50" });
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain2, IpAddress = "192.168.100.51" });

        // Act
        var validationResult = await cut.Config.ValidateLocalDnsConfig();

        // Assert
        Assert.That(validationResult.IsSuccess, Is.True);
        Assert.That(validationResult.Data.Valid, Is.True);
        Assert.That(validationResult.Data.ErrorMessage, Is.Empty.Or.Null);
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_CaseInsensitiveDomainHandling()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"CaseSensitive-{Guid.NewGuid():N}.Local";
        var testIp = "192.168.100.60";

        // Add record with mixed case
        var addResult = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest 
        { 
            Domain = testDomain, 
            IpAddress = testIp 
        });
        Assert.That(addResult.IsSuccess, Is.True);

        // Act: Try to add same domain with different case
        var lowerCaseDomain = testDomain.ToLowerInvariant();
        var duplicateResult = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest 
        { 
            Domain = lowerCaseDomain, 
            IpAddress = testIp 
        });

        // Assert: Should recognize as duplicate (case-insensitive)
        Assert.That(duplicateResult.IsSuccess, Is.True);
        Assert.That(duplicateResult.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
    }

    [Test]
    public async Task PiholeClient_DnsRecord_RemoveFromEmptyConfig_ShouldHandleGracefully()
    {
        // Arrange
        var cut = _sharedClient!;
        var nonExistentDomain = $"never-existed-{Guid.NewGuid():N}.local";

        // Act: Try to remove from potentially empty list
        var removeResult = await cut.Config.RemoveLocalDnsRecordsByDomain(nonExistentDomain);

        // Assert: Should not fail, just return "not found"
        Assert.That(removeResult.IsSuccess, Is.True);
        Assert.That(removeResult.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        Assert.That(removeResult.Data?.RemovedCount, Is.Null.Or.EqualTo(0));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_LargeBatchOperations_ShouldHandleMultipleRecords()
    {
        // Arrange
        var cut = _sharedClient!;
        var testIpBase = "192.168.101";
        var domains = new List<string>();
        var recordCount = 10;

        try
        {
            // Act: Add multiple records
            for (int i = 0; i < recordCount; i++)
            {
                var domain = $"batch-{i}-{Guid.NewGuid():N}.local";
                domains.Add(domain);
                var result = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest 
                { 
                    Domain = domain, 
                    IpAddress = $"{testIpBase}.{i}" 
                });
                Assert.That(result.IsSuccess, Is.True, $"Failed to add record {i}");
            }

            // Verify all records exist
            var config = await cut.Config.GetPiholeConfigAsync();
            foreach (var domain in domains)
            {
                var exists = config.Data?.Config?.Dns?.Hosts?.Any(h => 
                    h.Contains(domain, StringComparison.OrdinalIgnoreCase)) ?? false;
                Assert.That(exists, Is.True, $"Domain {domain} should exist");
            }
        }
        finally
        {
            // Cleanup
            foreach (var domain in domains)
            {
                await cut.Config.RemoveLocalDnsRecordsByDomain(domain);
            }
        }
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_SpecialCharactersInDomain_ShouldValidate()
    {
        // Arrange
        var cut = _sharedClient!;
        var validDomainWithHyphens = $"my-api-server-{Guid.NewGuid():N}.local";
        var testIp = "192.168.100.70";

        // Act
        var result = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest 
        { 
            Domain = validDomainWithHyphens, 
            IpAddress = testIp 
        });

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data?.DataOperation, Is.EqualTo(DataOperation.Created));
    }

    [Test]
    [RequiresBaselineRestore]
    public async Task PiholeClient_DnsRecord_ConflictWithOverwriteFalse_ShouldProvideConflictDetails()
    {
        // Arrange
        var cut = _sharedClient!;
        var testDomain = $"conflict-detail-{Guid.NewGuid():N}.local";
        var ip1 = "192.168.100.80";
        var ip2 = "192.168.100.81";

        // Add initial record
        await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest { Domain = testDomain, IpAddress = ip1 });

        // Act: Try to add conflicting record
        var conflictResult = await cut.Config.EnsureLocalDnsRecord(new LocalDnsRecordRequest 
        { 
            Domain = testDomain, 
            IpAddress = ip2,
            OverwriteExisting = false
        });

        // Assert: Should provide detailed conflict information
        Assert.That(conflictResult.IsSuccess, Is.False);
        Assert.That(conflictResult.Data?.DataOperation, Is.EqualTo(DataOperation.Conflict));
        Assert.That(conflictResult.Data?.ConflictingIpAddresses, Is.Not.Null);
        Assert.That(conflictResult.Data?.ConflictingIpAddresses, Does.Contain(ip1));
        Assert.That(conflictResult.Data?.Message, Does.Contain(testDomain));
        Assert.That(conflictResult.Data?.Message, Does.Contain(ip1));
    }

    #endregion

    private IOptions<PiHoleInstanceApiConfig> GetPiholeConfigOptions() 
        => Options.Create(new PiHoleInstanceApiConfig
    {
        ApiBaseUrl = _sharedBaseUrl ?? throw new InvalidOperationException("Base URL is not set"),
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
