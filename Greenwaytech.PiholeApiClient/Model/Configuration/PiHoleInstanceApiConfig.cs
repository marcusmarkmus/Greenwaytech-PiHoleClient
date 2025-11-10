namespace Greenwaytech.PiholeApiClient.Model.Configuration;
public record PiHoleInstanceApiConfig : IPiHoleInstanceApiConfig
{
    public required string ApiBaseUrl { get; set; }
    public required string ApiKey { get; set; }
}
