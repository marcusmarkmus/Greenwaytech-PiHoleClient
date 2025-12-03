using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Greenwaytech.PiholeApiClient.Providers;

/// <summary>
/// Tracks registrations of Pi-hole client instances to detect potential misconfiguration
/// where multiple clients are registered for the same Pi-hole instance.
/// This helps developers avoid race conditions in scenarios with multiple transient client resolutions.
/// </summary>
internal class PiholeClientRegistrationTracker
{
    private readonly ConcurrentDictionary<string, int> _registrationCounts = new();
    private readonly ILogger<PiholeClientRegistrationTracker> _logger;

    public PiholeClientRegistrationTracker(ILogger<PiholeClientRegistrationTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a client registration and logs a warning if multiple clients are registered
    /// for the same Pi-hole instance.
    /// </summary>
    /// <param name="instanceKey">Unique identifier for the Pi-hole instance (typically base URL)</param>
    public void RecordRegistration(string instanceKey)
    {
        if (string.IsNullOrWhiteSpace(instanceKey))
            return;

        var normalizedKey = NormalizeKey(instanceKey);
        var count = _registrationCounts.AddOrUpdate(normalizedKey, 1, (_, current) => current + 1);

        if (count > 1)
        {
            _logger.LogWarning(
                "Multiple Pi-hole clients ({Count}) have been registered/resolved for the same instance '{InstanceKey}'. " +
                "Concurrent operations across different client instances may lead to race conditions. " +
                "Consider: (1) Using a singleton client lifetime, (2) Avoiding parallel operations with transient clients, " +
                "or (3) Using AddPiholeApiClientFactory() for multi-instance scenarios with proper coordination.",
                count,
                instanceKey);
        }
    }

    /// <summary>
    /// Gets the current registration count for diagnostics
    /// </summary>
    internal int GetRegistrationCount(string instanceKey)
    {
        var normalizedKey = NormalizeKey(instanceKey);
        return _registrationCounts.TryGetValue(normalizedKey, out var count) ? count : 0;
    }

    private static string NormalizeKey(string key)
    {
        return key.TrimEnd('/').ToLowerInvariant();
    }
}
