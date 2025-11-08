using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
public record PiholeTeleportImportResponse
{
    [JsonPropertyName("files")]
    public string[] Files { get; set; }
    [JsonPropertyName("took")]
    public float Took { get; set; }
}

