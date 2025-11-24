namespace Greenwaytech.PiholeApiClient.Model.App.Response;

public record LocalDnsRecordRequest
{
    public required string Domain { get; init; }
    public required string IpAddress { get; init; }
    public LocalDnsRecordRequest()
    {
        
    }
    public LocalDnsRecordRequest(string domain, string ipAddress)
    {
        Domain = domain;
        IpAddress = ipAddress;
    }
}
