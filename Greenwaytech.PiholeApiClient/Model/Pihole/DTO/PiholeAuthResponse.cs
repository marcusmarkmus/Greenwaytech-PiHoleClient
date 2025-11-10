using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

public record PiholeAuthResponse
{
    [JsonPropertyName("session")]
    public AuthSession? Session { get; init; }
    [JsonPropertyName("took")]
    public float? Took { get; init; }
}

public record AuthSession
{
    [JsonPropertyName("valid")]
    public bool? Valid { get; init; }
    [JsonPropertyName("totp")]
    public bool? Totp { get; init; }
    [JsonPropertyName("sid")]
    public string? Sid { get; init; }
    [JsonPropertyName("csrf")]
    public string? Csrf { get; init; }
    [JsonPropertyName("validity")]
    public int? Validity { get; init; }
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
