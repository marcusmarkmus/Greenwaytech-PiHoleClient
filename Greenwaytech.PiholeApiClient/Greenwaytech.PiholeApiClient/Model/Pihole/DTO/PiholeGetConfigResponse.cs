using Greenwaytech.PiholeApiClient.Model.Pihole;
using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

public record PiholeGetConfigResponse
{
    [JsonPropertyName("config")]
    public PiholeConfigModel? Config { get; init; }
    [JsonPropertyName("took")]
    public float? Took { get; init; }
}
