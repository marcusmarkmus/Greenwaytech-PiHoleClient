using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.App.Request;
using Greenwaytech.PiholeApiClient.Model.App.Response;
using Greenwaytech.PiholeApiClient.Test.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Tests for Pi-hole Config client functionality including configuration management and DNS record operations.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PiholeConfigClientTests
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

    [Test, Order(1)]
    public async Task Config_ShouldGetConfig()
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

    [Test, Order(2)]
    [RequiresBaselineRestore]
    public async Task Config_PatchLocalDnsRecordAndReadBack_ShouldSucceed()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    #region DNS Record Management Tests

    [Test, Order(10)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_EnsureLocalDnsRecord_ShouldAddNewRecord()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(11)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_EnsureLocalDnsRecord_ShouldReturnAlreadyExistsForDuplicate()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(12)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_EnsureLocalDnsRecord_ShouldDetectConflictForSameDomainDifferentIP()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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
    }

    [Test, Order(13)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_EnsureLocalDnsRecord_ShouldAllowMultipleDomainsForSameIP()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(14)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_RemoveLocalDnsRecord_ShouldRemoveSpecificRecord()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(15)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_RemoveLocalDnsRecordsByDomain_ShouldRemoveAllRecordsForDomain()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(16)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_RemoveLocalDnsRecordsByIp_ShouldRemoveAllDomainsForIP()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(17)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_RemoveNonExistent_ShouldReturnNotFound()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        var nonExistentDomain = $"nonexistent-{Guid.NewGuid():N}.local";

        // Act
        var result = await cut.Config.RemoveLocalDnsRecordsByDomain(nonExistentDomain);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        Assert.That(result.Data?.Message, Does.Contain("not found").Or.Contain("No DNS records"));
    }

    [Test, Order(18)]
    public async Task DnsRecord_EnsureLocalDnsRecord_ShouldValidateIpAddress()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(19)]
    public async Task DnsRecord_EnsureLocalDnsRecord_ShouldValidateDomain()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        var request = new LocalDnsRecordRequest
        {
            Domain = "",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await cut.Config.EnsureLocalDnsRecord(request);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Domain").And.Contain("empty"));
    }

    [Test, Order(20)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_CompleteWorkflow_AddConflictResolveRemove()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(21)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_EnsureLocalDnsRecord_WithOverwriteExisting_ShouldReplaceRecord()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(22)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_EnsureLocalDnsRecord_WithIPv6_ShouldWork()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(23)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_EnsureLocalDnsRecord_IdempotencyTest()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(24)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_RemoveSpecificRecord_WithMultipleDuplicates_ShouldRemoveOnlyOne()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        var testDomain = $"duplicate-{Guid.NewGuid():N}.local";
        var testIp = "192.168.100.40";
        
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

    [Test, Order(25)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_ValidateLocalDnsConfig_WithValidConfig_ShouldReturnValid()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(26)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_CaseInsensitiveDomainHandling()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(27)]
    public async Task DnsRecord_RemoveFromEmptyConfig_ShouldHandleGracefully()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        var nonExistentDomain = $"never-existed-{Guid.NewGuid():N}.local";

        // Act: Try to remove from potentially empty list
        var removeResult = await cut.Config.RemoveLocalDnsRecordsByDomain(nonExistentDomain);

        // Assert: Should not fail, just return "not found"
        Assert.That(removeResult.IsSuccess, Is.True);
        Assert.That(removeResult.Data?.DataOperation, Is.EqualTo(DataOperation.AlreadyExists));
        Assert.That(removeResult.Data?.RemovedCount, Is.Null.Or.EqualTo(0));
    }

    [Test, Order(28)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_LargeBatchOperations_ShouldHandleMultipleRecords()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
        var testIpBase = "192.168.101";
        var domains = new List<string>();
        var recordCount = 10;

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

    [Test, Order(29)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_SpecialCharactersInDomain_ShouldValidate()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    [Test, Order(30)]
    [RequiresBaselineRestore]
    public async Task DnsRecord_ConflictWithOverwriteFalse_ShouldProvideConflictDetails()
    {
        // Arrange
        var cut = GeneratePiholeApiClientService();
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

    private IPiholeApiClientService GeneratePiholeApiClientService()
    {
        return PiholeTestContainerFixture.ServiceProvider.GetRequiredService<IPiholeApiClientService>();
    }
}
