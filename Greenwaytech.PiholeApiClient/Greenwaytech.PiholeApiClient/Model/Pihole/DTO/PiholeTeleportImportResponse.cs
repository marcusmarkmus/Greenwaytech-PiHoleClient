using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
public record PiholeTeleportImportResponse
{
    [JsonPropertyName("files")]
    public string[]? Files { get; set; }
    [JsonPropertyName("error")]
    public PiholeTeleportImportError? Error { get; set; }

    [JsonPropertyName("took")]
    public float Took { get; set; }
}

public record PiholeTeleportImportError
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("hint")]
    public string? Hint { get; set; }
    

}

