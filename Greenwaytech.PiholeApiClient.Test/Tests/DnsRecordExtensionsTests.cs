using Greenwaytech.PiholeApiClient.Extensions;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Unit tests for DnsRecordExtensions
/// </summary>
[TestFixture]
public class DnsRecordExtensionsTests
{
    #region FormatDnsRecord Tests

    [Test]
    public void FormatDnsRecord_ShouldFormatCorrectly()
    {
        // Arrange
        var ip = "192.168.1.1";
        var domain = "test.local";

        // Act
        var result = DnsRecordExtensions.FormatDnsRecord(ip, domain);

        // Assert
        Assert.That(result, Is.EqualTo("192.168.1.1 test.local"));
    }

    [Test]
    public void FormatDnsRecord_WithIPv6_ShouldFormatCorrectly()
    {
        // Arrange
        var ip = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
        var domain = "test.local";

        // Act
        var result = DnsRecordExtensions.FormatDnsRecord(ip, domain);

        // Assert
        Assert.That(result, Is.EqualTo("2001:0db8:85a3:0000:0000:8a2e:0370:7334 test.local"));
    }

    #endregion

    #region ParseDnsRecord Tests

    [Test]
    public void ParseDnsRecord_WithValidRecord_ShouldParse()
    {
        // Arrange
        var record = "192.168.1.1 test.local";

        // Act
        var result = DnsRecordExtensions.ParseDnsRecord(record);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.ipAddress, Is.EqualTo("192.168.1.1"));
        Assert.That(result.Value.domain, Is.EqualTo("test.local"));
    }

    [Test]
    public void ParseDnsRecord_WithExtraSpaces_ShouldParse()
    {
        // Arrange
        var record = "  192.168.1.1   test.local  ";

        // Act
        var result = DnsRecordExtensions.ParseDnsRecord(record);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.ipAddress, Is.EqualTo("192.168.1.1"));
        Assert.That(result.Value.domain, Is.EqualTo("test.local"));
    }

    [Test]
    public void ParseDnsRecord_WithNullOrEmpty_ShouldReturnNull()
    {
        // Act & Assert
        Assert.That(DnsRecordExtensions.ParseDnsRecord(null!), Is.Null);
        Assert.That(DnsRecordExtensions.ParseDnsRecord(""), Is.Null);
        Assert.That(DnsRecordExtensions.ParseDnsRecord("   "), Is.Null);
    }

    [Test]
    public void ParseDnsRecord_WithInvalidFormat_ShouldReturnNull()
    {
        // Arrange
        var invalidRecords = new[]
        {
            "192.168.1.1",              // Only IP
            "test.local",               // Only domain
            "192.168.1.1 test.local extra", // Too many parts
            ""
        };

        // Act & Assert
        foreach (var record in invalidRecords)
        {
            var result = DnsRecordExtensions.ParseDnsRecord(record);
            Assert.That(result, Is.Null, $"Expected null for: {record}");
        }
    }

    [Test]
    public void ParseDnsRecord_WithIPv6_ShouldParse()
    {
        // Arrange
        var record = "2001:0db8:85a3::8a2e:0370:7334 test.local";

        // Act
        var result = DnsRecordExtensions.ParseDnsRecord(record);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.ipAddress, Is.EqualTo("2001:0db8:85a3::8a2e:0370:7334"));
        Assert.That(result.Value.domain, Is.EqualTo("test.local"));
    }

    #endregion

    #region ContainsRecord Tests

    [Test]
    public void ContainsRecord_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local", "192.168.1.2 api.local" };

        // Act
        var result = records.ContainsRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsRecord_WhenNotExists_ShouldReturnFalse()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local", "192.168.1.2 api.local" };

        // Act
        var result = records.ContainsRecord("192.168.1.3", "other.local");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ContainsRecord_CaseInsensitive_ShouldReturnTrue()
    {
        // Arrange
        var records = new[] { "192.168.1.1 Test.Local" };

        // Act
        var result = records.ContainsRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsRecord_WithNullRecords_ShouldReturnFalse()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.ContainsRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ContainsRecord_WithEmptyRecords_ShouldReturnFalse()
    {
        // Arrange
        var records = Array.Empty<string>();

        // Act
        var result = records.ContainsRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region FindRecordsByDomain Tests

    [Test]
    public void FindRecordsByDomain_WithMultipleMatches_ShouldReturnAllIps()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 test.local",
            "192.168.1.3 other.local"
        };

        // Act
        var result = records.FindRecordsByDomain("test.local");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Does.Contain("192.168.1.1"));
        Assert.That(result, Does.Contain("192.168.1.2"));
    }

    [Test]
    public void FindRecordsByDomain_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var result = records.FindRecordsByDomain("nonexistent.local");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindRecordsByDomain_CaseInsensitive_ShouldReturnMatches()
    {
        // Arrange
        var records = new[] { "192.168.1.1 Test.Local" };

        // Act
        var result = records.FindRecordsByDomain("test.local");

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("192.168.1.1"));
    }

    [Test]
    public void FindRecordsByDomain_WithNullRecords_ShouldReturnEmpty()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.FindRecordsByDomain("test.local");

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region FindRecordsByIp Tests

    [Test]
    public void FindRecordsByIp_WithMultipleMatches_ShouldReturnAllDomains()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test1.local",
            "192.168.1.1 test2.local",
            "192.168.1.2 other.local"
        };

        // Act
        var result = records.FindRecordsByIp("192.168.1.1");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Does.Contain("test1.local"));
        Assert.That(result, Does.Contain("test2.local"));
    }

    [Test]
    public void FindRecordsByIp_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var result = records.FindRecordsByIp("192.168.1.2");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindRecordsByIp_CaseInsensitive_ShouldReturnMatches()
    {
        // Arrange
        var records = new[] { "192.168.1.1 Test.Local" };

        // Act
        var result = records.FindRecordsByIp("192.168.1.1");

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("Test.Local"));
    }

    [Test]
    public void FindRecordsByIp_WithNullRecords_ShouldReturnEmpty()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.FindRecordsByIp("192.168.1.1");

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region HasDomainConflict Tests

    [Test]
    public void HasDomainConflict_WithMultipleIps_ShouldReturnTrue()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 test.local"
        };

        // Act
        var (hasConflict, ipAddresses) = records.HasDomainConflict("test.local");

        // Assert
        Assert.That(hasConflict, Is.True);
        Assert.That(ipAddresses, Has.Count.EqualTo(2));
        Assert.That(ipAddresses, Does.Contain("192.168.1.1"));
        Assert.That(ipAddresses, Does.Contain("192.168.1.2"));
    }

    [Test]
    public void HasDomainConflict_WithSingleIp_ShouldReturnFalse()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var (hasConflict, ipAddresses) = records.HasDomainConflict("test.local");

        // Assert
        Assert.That(hasConflict, Is.False);
        Assert.That(ipAddresses, Has.Count.EqualTo(1));
    }

    [Test]
    public void HasDomainConflict_WithNoDomain_ShouldReturnFalse()
    {
        // Arrange
        var records = new[] { "192.168.1.1 other.local" };

        // Act
        var (hasConflict, ipAddresses) = records.HasDomainConflict("test.local");

        // Assert
        Assert.That(hasConflict, Is.False);
        Assert.That(ipAddresses, Is.Empty);
    }

    #endregion

    #region TryAddRecord Tests

    [Test]
    public void TryAddRecord_NewRecord_ShouldAddSuccessfully()
    {
        // Arrange
        var records = new[] { "192.168.1.1 existing.local" };

        // Act
        var result = records.TryAddRecord("192.168.1.2", "new.local");

        // Assert
        Assert.That(result.WasAdded, Is.True);
        Assert.That(result.UpdatedRecords, Has.Count.EqualTo(2));
        Assert.That(result.UpdatedRecords, Does.Contain("192.168.1.2 new.local"));
        Assert.That(result.Message, Does.Contain("added successfully"));
    }

    [Test]
    public void TryAddRecord_DuplicateRecord_ShouldReturnAlreadyExists()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var result = records.TryAddRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result.WasAdded, Is.False);
        Assert.That(result.AlreadyExists, Is.True);
        Assert.That(result.UpdatedRecords, Is.Empty);
        Assert.That(result.Message, Does.Contain("already exists"));
    }

    [Test]
    public void TryAddRecord_ConflictingDomain_ShouldReturnConflict()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var result = records.TryAddRecord("192.168.1.2", "test.local", overWriteExisting: false);

        // Assert
        Assert.That(result.WasAdded, Is.False);
        Assert.That(result.HasConflict, Is.True);
        Assert.That(result.ConflictingIpAddresses, Has.Count.EqualTo(1));
        Assert.That(result.ConflictingIpAddresses![0], Is.EqualTo("192.168.1.1"));
        Assert.That(result.Message, Does.Contain("already points to"));
    }

    [Test]
    public void TryAddRecord_ConflictingDomainWithOverwrite_ShouldReplaceRecord()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local", "192.168.1.3 other.local" };

        // Act
        var result = records.TryAddRecord("192.168.1.2", "test.local", overWriteExisting: true);

        // Assert
        Assert.That(result.WasAdded, Is.True);
        Assert.That(result.UpdatedRecords, Has.Count.EqualTo(2));
        Assert.That(result.UpdatedRecords, Does.Contain("192.168.1.2 test.local"));
        Assert.That(result.UpdatedRecords, Does.Not.Contain("192.168.1.1 test.local"));
        Assert.That(result.Message, Does.Contain("replaced"));
    }

    [Test]
    public void TryAddRecord_MultipleConflictsWithOverwrite_ShouldReplaceAll()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 test.local",
            "192.168.1.3 other.local"
        };

        // Act
        var result = records.TryAddRecord("192.168.1.10", "test.local", overWriteExisting: true);

        // Assert
        Assert.That(result.WasAdded, Is.True);
        Assert.That(result.UpdatedRecords, Has.Count.EqualTo(2)); // other.local + new record
        Assert.That(result.UpdatedRecords, Does.Contain("192.168.1.10 test.local"));
        Assert.That(result.UpdatedRecords, Does.Contain("192.168.1.3 other.local"));
        Assert.That(result.Message, Does.Contain("replaced 2"));
    }

    [Test]
    public void TryAddRecord_WithNullRecords_ShouldCreateNew()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.TryAddRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result.WasAdded, Is.True);
        Assert.That(result.UpdatedRecords, Has.Count.EqualTo(1));
        Assert.That(result.UpdatedRecords[0], Is.EqualTo("192.168.1.1 test.local"));
    }

    [Test]
    public void TryAddRecord_MultipleDomainsSameIp_ShouldAllowAll()
    {
        // Arrange
        var records = new[] { "192.168.1.1 domain1.local" };

        // Act
        var result1 = records.TryAddRecord("192.168.1.1", "domain2.local");
        var result2 = result1.UpdatedRecords.TryAddRecord("192.168.1.1", "domain3.local");

        // Assert
        Assert.That(result1.WasAdded, Is.True);
        Assert.That(result2.WasAdded, Is.True);
        Assert.That(result2.UpdatedRecords, Has.Count.EqualTo(3));
    }

    #endregion

    #region TryRemoveRecordsByDomain Tests

    [Test]
    public void TryRemoveRecordsByDomain_WithExistingDomain_ShouldRemove()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 other.local"
        };

        // Act
        var result = records.TryRemoveRecordsByDomain("test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(1));
        Assert.That(result.RemovedIpAddresses, Does.Contain("192.168.1.1"));
        Assert.That(result.UpdatedRecords, Has.Length.EqualTo(1));
        Assert.That(result.UpdatedRecords[0], Is.EqualTo("192.168.1.2 other.local"));
    }

    [Test]
    public void TryRemoveRecordsByDomain_WithMultipleRecords_ShouldRemoveAll()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 test.local",
            "192.168.1.3 other.local"
        };

        // Act
        var result = records.TryRemoveRecordsByDomain("test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(2));
        Assert.That(result.RemovedIpAddresses, Has.Count.EqualTo(2));
        Assert.That(result.UpdatedRecords, Has.Length.EqualTo(1));
    }

    [Test]
    public void TryRemoveRecordsByDomain_WithNonExistentDomain_ShouldReturnNotRemoved()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var result = records.TryRemoveRecordsByDomain("nonexistent.local");

        // Assert
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.RemovedCount, Is.EqualTo(0));
        Assert.That(result.Message, Does.Contain("not found"));
    }

    [Test]
    public void TryRemoveRecordsByDomain_CaseInsensitive_ShouldRemove()
    {
        // Arrange
        var records = new[] { "192.168.1.1 Test.Local" };

        // Act
        var result = records.TryRemoveRecordsByDomain("test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(1));
    }

    [Test]
    public void TryRemoveRecordsByDomain_WithNullRecords_ShouldReturnNotRemoved()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.TryRemoveRecordsByDomain("test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.UpdatedRecords, Is.Empty);
    }

    #endregion

    #region TryRemoveRecordsByIp Tests

    [Test]
    public void TryRemoveRecordsByIp_WithExistingIp_ShouldRemove()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 other.local"
        };

        // Act
        var result = records.TryRemoveRecordsByIp("192.168.1.1");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(1));
        Assert.That(result.RemovedDomains, Does.Contain("test.local"));
        Assert.That(result.UpdatedRecords, Has.Length.EqualTo(1));
    }

    [Test]
    public void TryRemoveRecordsByIp_WithMultipleRecords_ShouldRemoveAll()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test1.local",
            "192.168.1.1 test2.local",
            "192.168.1.2 other.local"
        };

        // Act
        var result = records.TryRemoveRecordsByIp("192.168.1.1");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(2));
        Assert.That(result.RemovedDomains, Has.Count.EqualTo(2));
        Assert.That(result.UpdatedRecords, Has.Length.EqualTo(1));
    }

    [Test]
    public void TryRemoveRecordsByIp_WithNonExistentIp_ShouldReturnNotRemoved()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var result = records.TryRemoveRecordsByIp("192.168.1.2");

        // Assert
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.RemovedCount, Is.EqualTo(0));
    }

    [Test]
    public void TryRemoveRecordsByIp_CaseInsensitive_ShouldRemove()
    {
        // Arrange
        var records = new[] { "192.168.1.1 Test.Local" };

        // Act
        var result = records.TryRemoveRecordsByIp("192.168.1.1");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.RemovedDomains, Does.Contain("Test.Local"));
    }

    [Test]
    public void TryRemoveRecordsByIp_WithNullRecords_ShouldReturnNotRemoved()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.TryRemoveRecordsByIp("192.168.1.1");

        // Assert
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.UpdatedRecords, Is.Empty);
    }

    #endregion

    #region TryRemoveSpecificRecord Tests

    [Test]
    public void TryRemoveSpecificRecord_WithExactMatch_ShouldRemove()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 other.local"
        };

        // Act
        var result = records.TryRemoveSpecificRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.RemovedCount, Is.EqualTo(1));
        Assert.That(result.UpdatedRecords, Has.Length.EqualTo(1));
        Assert.That(result.UpdatedRecords[0], Is.EqualTo("192.168.1.2 other.local"));
    }

    [Test]
    public void TryRemoveSpecificRecord_WithNoMatch_ShouldReturnNotRemoved()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local" };

        // Act
        var result = records.TryRemoveSpecificRecord("192.168.1.2", "test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.RemovedCount, Is.EqualTo(0));
        Assert.That(result.Message, Does.Contain("not found"));
    }

    [Test]
    public void TryRemoveSpecificRecord_OnlyRemovesExactMatch_NotOtherRecords()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 test.local", // Same domain, different IP
            "192.168.1.1 other.local" // Same IP, different domain
        };

        // Act
        var result = records.TryRemoveSpecificRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
        Assert.That(result.UpdatedRecords, Has.Length.EqualTo(2));
        Assert.That(result.UpdatedRecords, Does.Contain("192.168.1.2 test.local"));
        Assert.That(result.UpdatedRecords, Does.Contain("192.168.1.1 other.local"));
    }

    [Test]
    public void TryRemoveSpecificRecord_CaseInsensitive_ShouldRemove()
    {
        // Arrange
        var records = new[] { "192.168.1.1 Test.Local" };

        // Act
        var result = records.TryRemoveSpecificRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.True);
    }

    [Test]
    public void TryRemoveSpecificRecord_WithNullRecords_ShouldReturnNotRemoved()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.TryRemoveSpecificRecord("192.168.1.1", "test.local");

        // Assert
        Assert.That(result.WasRemoved, Is.False);
        Assert.That(result.UpdatedRecords, Is.Empty);
    }

    #endregion

    #region RecordCount Tests

    [Test]
    public void RecordCount_WithRecords_ShouldReturnCount()
    {
        // Arrange
        var records = new[] { "192.168.1.1 test.local", "192.168.1.2 other.local" };

        // Act
        var count = records.RecordCount();

        // Assert
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void RecordCount_WithEmptyRecords_ShouldReturnZero()
    {
        // Arrange
        var records = Array.Empty<string>();

        // Act
        var count = records.RecordCount();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void RecordCount_WithNullRecords_ShouldReturnZero()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var count = records.RecordCount();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    #endregion

    #region ValidateDnsRecords Tests

    [Test]
    public void ValidateDnsRecords_WithValidRecords_ShouldReturnSuccess()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 other.local"
        };

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null.Or.Empty);
    }

    [Test]
    public void ValidateDnsRecords_WithDuplicates_ShouldReturnFailure()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.1 test.local", // Duplicate
            "192.168.1.2 other.local"
        };

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Duplicate"));
    }

    [Test]
    public void ValidateDnsRecords_WithDomainConflicts_ShouldReturnFailure()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 test.local", // Same domain, different IP
            "192.168.1.3 other.local"
        };

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Domain conflicts"));
        Assert.That(result.ErrorMessage, Does.Contain("test.local"));
    }

    [Test]
    public void ValidateDnsRecords_WithMultipleDomainConflicts_ShouldListAll()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.2 test.local",
            "192.168.1.3 api.local",
            "192.168.1.4 api.local"
        };

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("test.local"));
        Assert.That(result.ErrorMessage, Does.Contain("api.local"));
    }

    [Test]
    public void ValidateDnsRecords_WithNullRecords_ShouldReturnSuccess()
    {
        // Arrange
        IEnumerable<string>? records = null;

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateDnsRecords_WithEmptyRecords_ShouldReturnSuccess()
    {
        // Arrange
        var records = Array.Empty<string>();

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateDnsRecords_MultipleDomainsSameIp_ShouldBeValid()
    {
        // Arrange - This is valid: multiple domains can point to the same IP
        var records = new[]
        {
            "192.168.1.1 test1.local",
            "192.168.1.1 test2.local",
            "192.168.1.1 test3.local"
        };

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateDnsRecords_CaseInsensitiveDuplicates_ShouldDetect()
    {
        // Arrange
        var records = new[]
        {
            "192.168.1.1 test.local",
            "192.168.1.1 Test.Local" // Case-insensitive duplicate
        };

        // Act
        var result = records.ValidateDnsRecords();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Duplicate"));
    }

    #endregion
}
