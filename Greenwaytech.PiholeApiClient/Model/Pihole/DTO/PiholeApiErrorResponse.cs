using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

public record PiholeApiErrorResponse
{
    [JsonPropertyName("error")]
    public Error? Error { get; set; }
    [JsonPropertyName("took")]
    public float? Took { get; set; }
}

public class Error
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("hint")]
    public string? Hint { get; set; }
}

