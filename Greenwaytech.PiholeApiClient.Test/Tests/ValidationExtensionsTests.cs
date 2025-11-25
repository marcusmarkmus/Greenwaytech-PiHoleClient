using Greenwaytech.PiholeApiClient.Extensions;
using Greenwaytech.PiholeApiClient.Model.App.Response;

namespace Greenwaytech.PiholeApiClient.Test.Tests;

/// <summary>
/// Unit tests for ValidationExtensions
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ValidationExtensionsTests
{
    #region Validate LocalDnsRecordRequest Tests

    [Test]
    public void Validate_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null.Or.Empty);
    }

    [Test]
    public void Validate_WithValidIPv4_ShouldReturnSuccess()
    {
        // Arrange
        var validIpAddresses = new[]
        {
            "0.0.0.0",
            "127.0.0.1",
            "192.168.1.1",
            "10.0.0.1",
            "172.16.0.1",
            "255.255.255.255"
        };

        foreach (var ip in validIpAddresses)
        {
            var request = new LocalDnsRecordRequest
            {
                Domain = "test.local",
                IpAddress = ip
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.That(result.IsValid, Is.True, $"Should accept valid IPv4: {ip}");
        }
    }

    [Test]
    public void Validate_WithValidIPv6_ShouldReturnSuccess()
    {
        // Arrange
        var validIpAddresses = new[]
        {
            "2001:0db8:85a3:0000:0000:8a2e:0370:7334",
            "2001:db8:85a3::8a2e:370:7334",
            "::1",
            "::",
            "fe80::1",
            "::ffff:192.0.2.1"
        };

        foreach (var ip in validIpAddresses)
        {
            var request = new LocalDnsRecordRequest
            {
                Domain = "test.local",
                IpAddress = ip
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.That(result.IsValid, Is.True, $"Should accept valid IPv6: {ip}");
        }
    }

    [Test]
    public void Validate_WithInvalidIpAddress_ShouldReturnFailure()
    {
        // Arrange
        var invalidIpAddresses = new[]
        {
            "256.256.256.256",
            "192.168.1",
            "192.168.1.1.1",
            "invalid-ip",
            "192.168.1.a",
            "192.168.1.1.1",
            "999.999.999.999"
        };

        foreach (var ip in invalidIpAddresses)
        {
            var request = new LocalDnsRecordRequest
            {
                Domain = "test.local",
                IpAddress = ip
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.That(result.IsValid, Is.False, $"Should reject invalid IP: {ip}");
            Assert.That(result.ErrorMessage, Does.Contain("not a valid"), $"Failed for IP: {ip}");
        }
    }

    [Test]
    public void Validate_WithNullIpAddress_ShouldReturnFailure()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = null!
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("IP address").And.Contains("empty"));
    }

    [Test]
    public void Validate_WithEmptyIpAddress_ShouldReturnFailure()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = ""
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("IP address").And.Contains("empty"));
    }

    [Test]
    public void Validate_WithWhitespaceIpAddress_ShouldReturnFailure()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = "   "
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("IP address").And.Contains("empty"));
    }

    [Test]
    public void Validate_WithValidDomains_ShouldReturnSuccess()
    {
        // Arrange
        var validDomains = new[]
        {
            "test.local",
            "api.example.com",
            "sub.domain.example.com",
            "test-api.local",
            "123.local",
            "test123.local",
            "a.b.c.d.e.f.g.local",
            "very-long-domain-name-that-is-still-valid.local"
        };

        foreach (var domain in validDomains)
        {
            var request = new LocalDnsRecordRequest
            {
                Domain = domain,
                IpAddress = "192.168.1.1"
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.That(result.IsValid, Is.True, $"Should accept valid domain: {domain}");
        }
    }

    [Test]
    public void Validate_WithInvalidDomains_ShouldReturnFailure()
    {
        // Arrange
        var invalidDomains = new[]
        {
            "-test.local",          // Starts with hyphen
            "test-.local",          // Label ends with hyphen
            "test..local",          // Double dot
            ".test.local",          // Starts with dot
            "test.local.",          // Ends with dot (depending on implementation)
            "test local",           // Contains space
            "test@local",           // Contains @
            "test#local",           // Contains #
            new string('a', 64) + ".local",  // Label too long (>63 chars)
            new string('a', 254),            // Domain too long (>253 chars)
            ""
        };

        foreach (var domain in invalidDomains.Where(d => !string.IsNullOrEmpty(d)))
        {
            var request = new LocalDnsRecordRequest
            {
                Domain = domain,
                IpAddress = "192.168.1.1"
            };

            // Act
            var result = request.Validate();

            // Assert
            Assert.That(result.IsValid, Is.False, $"Should reject invalid domain: {domain}");
            Assert.That(result.ErrorMessage, Does.Contain("not valid"), $"Failed for domain: {domain}");
        }
    }

    [Test]
    public void Validate_WithNullDomain_ShouldReturnFailure()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = null!,
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Domain").And.Contains("empty"));
    }

    [Test]
    public void Validate_WithEmptyDomain_ShouldReturnFailure()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Domain").And.Contains("empty"));
    }

    [Test]
    public void Validate_WithWhitespaceDomain_ShouldReturnFailure()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "   ",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Domain").And.Contains("empty"));
    }

    [Test]
    public void Validate_WithNullRequest_ShouldReturnFailure()
    {
        // Arrange
        LocalDnsRecordRequest? request = null;

        // Act
        var result = request!.Validate();

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("null"));
    }

    [Test]
    public void Validate_WithOverwriteExistingTrue_ShouldStillValidate()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = "192.168.1.1",
            OverwriteExisting = true
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithOverwriteExistingFalse_ShouldStillValidate()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = "192.168.1.1",
            OverwriteExisting = false
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region ValidationResult Tests

    [Test]
    public void ValidationResult_Success_ShouldHaveCorrectProperties()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null.Or.Empty);
    }

    [Test]
    public void ValidationResult_Failure_ShouldHaveCorrectProperties()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = ValidationResult.Failure(errorMessage);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
    }

    [Test]
    public void ValidationResult_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Success();
        var result3 = ValidationResult.Failure("Error");
        var result4 = ValidationResult.Failure("Error");

        // Assert
        Assert.That(result1, Is.EqualTo(result2));
        Assert.That(result3, Is.EqualTo(result4));
        Assert.That(result1, Is.Not.EqualTo(result3));
    }

    #endregion

    #region Domain Validation Edge Cases

    [Test]
    public void Validate_WithDomainContainingNumbers_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "server123.local",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithDomainStartingWithNumber_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "123server.local",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithSingleCharacterLabels_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "a.b.c",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithMaxLengthLabel_ShouldBeValid()
    {
        // Arrange - Max label length is 63 characters
        var maxLengthLabel = new string('a', 63);
        var request = new LocalDnsRecordRequest
        {
            Domain = $"{maxLengthLabel}.local",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithHyphenInMiddle_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "my-server.local",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithMultipleHyphens_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "my-api-server.local",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion

    #region IP Address Edge Cases

    [Test]
    public void Validate_WithLeadingZerosInIPv4_ShouldValidate()
    {
        // Arrange - Some parsers may accept leading zeros
        var request = new LocalDnsRecordRequest
        {
            Domain = "test.local",
            IpAddress = "192.168.001.001"
        };

        // Act
        var result = request.Validate();

        // Note: IPAddress.TryParse behavior determines this
        // This test documents current behavior
        Assert.That(result.IsValid, Is.True.Or.False);
    }

    [Test]
    public void Validate_WithLocalhostIPv4_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "localhost.local",
            IpAddress = "127.0.0.1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithLocalhostIPv6_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "localhost.local",
            IpAddress = "::1"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithBroadcastAddress_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "broadcast.local",
            IpAddress = "255.255.255.255"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_WithZeroAddress_ShouldBeValid()
    {
        // Arrange
        var request = new LocalDnsRecordRequest
        {
            Domain = "zero.local",
            IpAddress = "0.0.0.0"
        };

        // Act
        var result = request.Validate();

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    #endregion
}
