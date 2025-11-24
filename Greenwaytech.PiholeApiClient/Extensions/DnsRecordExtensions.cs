namespace Greenwaytech.PiholeApiClient.Extensions;

/// <summary>
/// Extension methods for working with DNS host records
/// </summary>
internal static class DnsRecordExtensions
{
    /// <summary>
    /// Formats a DNS record in the Pi-hole format: "IP domain"
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <param name="domain">Domain name</param>
    /// <returns>Formatted DNS record string</returns>
    internal static string FormatDnsRecord(string ipAddress, string domain)
    {
        return string.Concat(ipAddress, " ", domain);
    }

    /// <summary>
    /// Parses a DNS record string into IP and domain components
    /// </summary>
    /// <param name="record">DNS record string in format "IP domain"</param>
    /// <returns>Tuple of (ipAddress, domain) or null if parsing fails</returns>
    internal static (string ipAddress, string domain)? ParseDnsRecord(string record)
    {
        if (string.IsNullOrWhiteSpace(record))
            return null;

        var parts = record.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return null;

        return (parts[0], parts[1]);
    }

    /// <summary>
    /// Checks if a DNS record already exists in the collection
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="ipAddress">IP address to search for</param>
    /// <param name="domain">Domain name to search for</param>
    /// <returns>True if the record exists, false otherwise</returns>
    internal static bool ContainsRecord(this IEnumerable<string>? records, string ipAddress, string domain)
    {
        if (records is null)
            return false;

        var expectedRecord = FormatDnsRecord(ipAddress, domain);
        
        return records.Any(record => 
            string.Equals(record, expectedRecord, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds all DNS records for a specific domain
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="domain">Domain name to search for</param>
    /// <returns>List of IP addresses associated with the domain</returns>
    internal static List<string> FindRecordsByDomain(this IEnumerable<string>? records, string domain)
    {
        if (records is null)
            return [];

        var ipAddresses = new List<string>();
        
        foreach (var record in records)
        {
            var parsed = ParseDnsRecord(record);
            if (parsed.HasValue && 
                string.Equals(parsed.Value.domain, domain, StringComparison.OrdinalIgnoreCase))
            {
                ipAddresses.Add(parsed.Value.ipAddress);
            }
        }

        return ipAddresses;
    }

    /// <summary>
    /// Finds all DNS records for a specific IP address
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="ipAddress">IP address to search for</param>
    /// <returns>List of domains associated with the IP address</returns>
    internal static List<string> FindRecordsByIp(this IEnumerable<string>? records, string ipAddress)
    {
        if (records is null)
            return [];

        var domains = new List<string>();
        
        foreach (var record in records)
        {
            var parsed = ParseDnsRecord(record);
            if (parsed.HasValue && 
                string.Equals(parsed.Value.ipAddress, ipAddress, StringComparison.OrdinalIgnoreCase))
            {
                domains.Add(parsed.Value.domain);
            }
        }

        return domains;
    }

    /// <summary>
    /// Checks if a domain has conflicting records (same domain pointing to different IPs)
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="domain">Domain name to check</param>
    /// <returns>Tuple indicating if conflicts exist and the list of conflicting IP addresses</returns>
    internal static (bool hasConflict, List<string> ipAddresses) HasDomainConflict(
        this IEnumerable<string>? records, 
        string domain)
    {
        var ips = records.FindRecordsByDomain(domain);
        return (ips.Count > 1, ips);
    }

    /// <summary>
    /// Adds a DNS record to the collection if it doesn't already exist.
    /// Prevents creating duplicate domains with different IPs unless allowDuplicateDomains is true.
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="domain">Domain name</param>
    /// <param name="overWriteExisting">If true, replaces existing records for the domain</param>
    /// <returns>Result with status, message, and updated records</returns>
    internal static DnsRecordAddResult TryAddRecord(
        this IEnumerable<string>? records, 
        string ipAddress, 
        string domain,
        bool overWriteExisting = false)
    {
        var recordList = records is not null 
            ? new List<string>(records) 
            : [];

        // Check if exact record already exists - no action needed
        if (recordList.ContainsRecord(ipAddress, domain))
        {
            return new DnsRecordAddResult
            {
                WasAdded = false,
                AlreadyExists = true,
                UpdatedRecords = recordList, // Return current list unchanged
                Message = $"DNS record '{ipAddress} {domain}' already exists"
            };
        }

        // Check for existing records with the same domain
        var existingIps = recordList.FindRecordsByDomain(domain);
        
        // No conflicts - just add the record
        if (existingIps.Count == 0)
        {
            recordList.Add(FormatDnsRecord(ipAddress, domain));
            return new DnsRecordAddResult
            {
                WasAdded = true,
                UpdatedRecords = recordList,
                Message = $"DNS record '{ipAddress} {domain}' added successfully"
            };
        }

        // Conflict exists - check if overwrite is allowed
        if (!overWriteExisting)
        {
            return new DnsRecordAddResult
            {
                WasAdded = false,
                HasConflict = true,
                ConflictingIpAddresses = existingIps,
                UpdatedRecords = recordList, // Return current list unchanged
                Message = $"Domain '{domain}' already points to {string.Join(", ", existingIps)}. " +
                          $"Remove existing record(s) first or set overWriteExisting=true."
            };
        }

        // Overwrite allowed - remove old records and add new one
        recordList.RemoveAll(record =>
        {
            var parsed = ParseDnsRecord(record);
            return parsed.HasValue && 
                   string.Equals(parsed.Value.domain, domain, StringComparison.OrdinalIgnoreCase);
        });
        
        recordList.Add(FormatDnsRecord(ipAddress, domain));
        
        return new DnsRecordAddResult
        {
            WasAdded = true,
            UpdatedRecords = recordList,
            Message = $"DNS record '{ipAddress} {domain}' added successfully, " +
                      $"replaced {existingIps.Count} existing record(s) for domain '{domain}'"
        };
    }

    /// <summary>
    /// Removes DNS records by domain name (removes ALL records for that domain)
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="domain">Domain name to remove</param>
    /// <returns>Result with removal status and updated records</returns>
    internal static DnsRecordRemoveResult TryRemoveRecordsByDomain(
        this IEnumerable<string>? records, 
        string domain)
    {
        if (records is null)
            return new DnsRecordRemoveResult { WasRemoved = false, UpdatedRecords = [] };

        var recordsArray = records.ToArray();
        var removedIpAddresses = new List<string>();
        
        // Find all IPs associated with this domain before removal
        foreach (var record in recordsArray)
        {
            var parsed = ParseDnsRecord(record);
            if (parsed.HasValue && 
                string.Equals(parsed.Value.domain, domain, StringComparison.OrdinalIgnoreCase))
            {
                removedIpAddresses.Add(parsed.Value.ipAddress);
            }
        }

        // Remove all records matching the domain
        var updatedRecords = recordsArray
            .Where(record =>
            {
                var parsed = ParseDnsRecord(record);
                return !parsed.HasValue || 
                       !string.Equals(parsed.Value.domain, domain, StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        var wasRemoved = updatedRecords.Length < recordsArray.Length;
        
        return new DnsRecordRemoveResult
        {
            WasRemoved = wasRemoved,
            RemovedCount = recordsArray.Length - updatedRecords.Length,
            RemovedIpAddresses = removedIpAddresses,
            UpdatedRecords = updatedRecords,
            Message = wasRemoved 
                ? $"Removed {removedIpAddresses.Count} DNS record(s) for domain '{domain}'" 
                : $"No DNS records found for domain '{domain}'"
        };
    }

    /// <summary>
    /// Removes DNS records by IP address (removes ALL domains pointing to that IP)
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="ipAddress">IP address to remove</param>
    /// <returns>Result with removal status and updated records</returns>
    internal static DnsRecordRemoveResult TryRemoveRecordsByIp(
        this IEnumerable<string>? records, 
        string ipAddress)
    {
        if (records is null)
            return new DnsRecordRemoveResult { WasRemoved = false, UpdatedRecords = [] };

        var recordsArray = records.ToArray();
        var removedDomains = new List<string>();
        
        // Find all domains associated with this IP before removal
        foreach (var record in recordsArray)
        {
            var parsed = ParseDnsRecord(record);
            if (parsed.HasValue && 
                string.Equals(parsed.Value.ipAddress, ipAddress, StringComparison.OrdinalIgnoreCase))
            {
                removedDomains.Add(parsed.Value.domain);
            }
        }

        // Remove all records matching the IP
        var updatedRecords = recordsArray
            .Where(record =>
            {
                var parsed = ParseDnsRecord(record);
                return !parsed.HasValue || 
                       !string.Equals(parsed.Value.ipAddress, ipAddress, StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        var wasRemoved = updatedRecords.Length < recordsArray.Length;
        
        return new DnsRecordRemoveResult
        {
            WasRemoved = wasRemoved,
            RemovedCount = recordsArray.Length - updatedRecords.Length,
            RemovedDomains = removedDomains,
            UpdatedRecords = updatedRecords,
            Message = wasRemoved 
                ? $"Removed {removedDomains.Count} DNS record(s) with IP '{ipAddress}'" 
                : $"No DNS records found with IP '{ipAddress}'"
        };
    }

    /// <summary>
    /// Removes a specific DNS record (exact match of IP and domain)
    /// </summary>
    /// <param name="records">Collection of DNS records</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="domain">Domain name</param>
    /// <returns>Result with removal status and updated records</returns>
    internal static DnsRecordRemoveResult TryRemoveSpecificRecord(
        this IEnumerable<string>? records, 
        string ipAddress,
        string domain)
    {
        if (records is null)
            return new DnsRecordRemoveResult { WasRemoved = false, UpdatedRecords = [] };

        var recordsArray = records.ToArray();
        var targetRecord = FormatDnsRecord(ipAddress, domain);
        
        // Remove only the exact matching record
        var updatedRecords = recordsArray
            .Where(record => !string.Equals(record, targetRecord, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var wasRemoved = updatedRecords.Length < recordsArray.Length;
        
        return new DnsRecordRemoveResult
        {
            WasRemoved = wasRemoved,
            RemovedCount = wasRemoved ? 1 : 0,
            RemovedIpAddresses = wasRemoved ? [ipAddress] : [],
            RemovedDomains = wasRemoved ? [domain] : [],
            UpdatedRecords = updatedRecords,
            Message = wasRemoved 
                ? $"Removed DNS record '{ipAddress} {domain}'" 
                : $"DNS record '{ipAddress} {domain}' not found"
        };
    }

    /// <summary>
    /// Gets the count of DNS records in the collection
    /// </summary>
    internal static int RecordCount(this IEnumerable<string>? records)
    {
        return records?.Count() ?? 0;
    }

    /// <summary>
    /// Validates DNS records for duplicates and conflicts
    /// </summary>
    /// <param name="records">Collection of DNS records to validate</param>
    /// <returns>Validation result indicating if records are valid</returns>
    internal static ValidationResult ValidateDnsRecords(this IEnumerable<string>? records)
    {
        var errorMessageSb = new System.Text.StringBuilder();
        if (records is null)
        {
            return ValidationResult.Success();
        }

        var recordsArray = records.ToArray();
        if (recordsArray.Length == 0)
        {
            return ValidationResult.Success();
        }

        // Check for exact duplicates
        var duplicates = recordsArray
            .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            errorMessageSb.AppendLine($"Duplicate DNS records found: {string.Join(", ", duplicates)}");
        }

        // Check for domain conflicts (same domain pointing to different IPs)
        var domainConflicts = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        //collect IPs per domain - if more than one IP per domain, it's a conflict/bad config
        foreach (var record in recordsArray)
        {
            var parsed = ParseDnsRecord(record);
            if (parsed.HasValue)
            {
                var (ip, domain) = parsed.Value;
                
                if (!domainConflicts.TryGetValue(domain, out List<string>? domainConflictInstance))
                {
                    domainConflictInstance = [];
                    domainConflicts[domain] = domainConflictInstance;
                }
                
                if (!domainConflictInstance.Contains(ip, StringComparer.OrdinalIgnoreCase))
                {
                    domainConflictInstance.Add(ip);
                }
            }
        }

        // Find domains with more than one associated IP
        var conflicts = domainConflicts
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp => $"{kvp.Key} -> [{string.Join(", ", kvp.Value)}]")
            .ToList();

        if (conflicts.Count > 0)
        {
            errorMessageSb.AppendLine($"Domain conflicts found (same domain pointing to multiple IPs): {string.Join("; ", conflicts)}");
        }
        if(errorMessageSb.Length > 0)
        {
            return ValidationResult.Failure(errorMessageSb.ToString());
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Result of adding a DNS record
/// </summary>
internal record DnsRecordAddResult
{
    public bool WasAdded { get; init; }
    public bool AlreadyExists { get; init; }
    public bool HasConflict { get; init; }
    public List<string>? ConflictingIpAddresses { get; init; }
    public List<string> UpdatedRecords { get; init; } = [];
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Result of removing DNS record(s)
/// </summary>
internal record DnsRecordRemoveResult
{
    public bool WasRemoved { get; init; }
    public int RemovedCount { get; init; }
    public List<string> RemovedIpAddresses { get; init; } = [];
    public List<string> RemovedDomains { get; init; } = [];
    public string[] UpdatedRecords { get; init; } = [];
    public string Message { get; init; } = string.Empty;
}
