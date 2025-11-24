using Greenwaytech.PiholeApiClient.Model.App;
using Greenwaytech.PiholeApiClient.Model.App.Response;
using System.Net;

namespace Greenwaytech.PiholeApiClient.Extensions;

/// <summary>
/// Extension methods for validating request objects
/// </summary>
internal static class ValidationExtensions
{
    /// <summary>
    /// Validates a LocalDnsRecordRequest and returns validation result
    /// </summary>
    /// <param name="request">The request to validate</param>
    /// <returns>Validation result with success status and error message if invalid</returns>
    internal static ValidationResult Validate(this LocalDnsRecordRequest request)
    {
        if (request is null)
        {
            return ValidationResult.Failure("Request cannot be null");
        }

        if (string.IsNullOrWhiteSpace(request.Domain))
        {
            return ValidationResult.Failure("Domain cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(request.IpAddress))
        {
            return ValidationResult.Failure("IP address cannot be null or empty");
        }

        // Optional: Validate IP address format
        if (!IsValidIpAddress(request.IpAddress))
        {
            return ValidationResult.Failure($"IP address '{request.IpAddress}' is not a valid IPv4 or IPv6 address");
        }

        // Optional: Validate domain format
        if (!IsValidDomain(request.Domain))
        {
            return ValidationResult.Failure($"Domain '{request.Domain}' is not valid");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates if a string is a valid IP address (IPv4 or IPv6)
    /// </summary>
    private static bool IsValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

    /// <summary>
    /// Validates if a string is a valid domain name
    /// </summary>
    private static bool IsValidDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain) || domain.Length > 253)
            return false;

        // Basic domain validation - allows alphanumeric, hyphens, dots
        // Each label must start/end with alphanumeric, can contain hyphens
        var labels = domain.Split('.');
        
        foreach (var label in labels)
        {
            if (string.IsNullOrWhiteSpace(label) || label.Length > 63)
                return false;

            // Must start and end with alphanumeric
            if (!char.IsLetterOrDigit(label[0]) || !char.IsLetterOrDigit(label[^1]))
                return false;

            // Can only contain alphanumeric and hyphens
            foreach (var c in label)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                    return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Represents the result of a validation operation
/// </summary>
internal readonly record struct ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
