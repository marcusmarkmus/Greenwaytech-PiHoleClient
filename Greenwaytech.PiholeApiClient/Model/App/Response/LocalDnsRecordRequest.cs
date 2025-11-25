namespace Greenwaytech.PiholeApiClient.Model.App.Response;

/// <summary>
/// Represents a request to create or update a local DNS record on a Pi-hole server.
/// </summary>
public record LocalDnsRecordRequest
{
    public required string Domain { get; init; }
    public required string IpAddress { get; init; }
    /// <summary>
    /// This will overwrite ANY existing record for the given domain, 
    /// i.e. the only ip this domain will resolve to is the one provided here.
    /// </summary>
    public bool OverwriteExisting { get; init; } = false;
    public LocalDnsRecordRequest()
    {
        
    }
    public LocalDnsRecordRequest(string domain, string ipAddress, bool overwriteExisting=false)
    {
        Domain = domain;
        IpAddress = ipAddress;
        OverwriteExisting = overwriteExisting;
    }
}
