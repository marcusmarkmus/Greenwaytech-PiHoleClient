namespace Greenwaytech.PiholeApiClient.Model.App.Request;

public record EnsureLocalDnsRecordResponse
{
    public bool Success { get; init; } 
    public required DataOperation DataOperation { get; set; }
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of DNS records removed (for removal operations)
    /// </summary>
    public int? RemovedCount { get; init; }
    
    /// <summary>
    /// List of IP addresses that were removed or are in conflict
    /// </summary>
    public List<string>? RemovedIpAddresses { get; init; }
    
    /// <summary>
    /// List of conflicting IP addresses (when trying to add a domain that already exists with different IP)
    /// </summary>
    public List<string>? ConflictingIpAddresses { get; init; }
    
    /// <summary>
    /// List of domains that were removed (for IP-based removal)
    /// </summary>
    public List<string>? RemovedDomains { get; init; }
}
