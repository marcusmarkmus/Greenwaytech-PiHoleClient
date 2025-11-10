using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole;

public record PiholeApiSession
{
    [JsonPropertyName("valid")]
    public bool? Valid { get; set; }
    [JsonPropertyName("totp")]
    public bool? Totp { get; set; }
    [JsonPropertyName("sid")]
    public string? Sid { get; set; }
    [JsonPropertyName("csrf")]
    public string? Csrf { get; set; }
    [JsonPropertyName("validity")]
    public int? Validity { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    public required DateTimeOffset PiholeAuthResponseTimeStamp { get; set; }

    public bool IsValid() 
        => Valid == true && !string.IsNullOrEmpty(Sid) && Validity > 0 && PiholeAuthResponseTimeStamp.AddSeconds(Validity ?? 0) > DateTimeOffset.UtcNow;
}

