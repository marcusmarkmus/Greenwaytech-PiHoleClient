using Greenwaytech.PiholeApiClient.Model;
using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.DTO;

public record PiholeGetConfigResponse
{
    [JsonPropertyName("config")]
    public PiholeConfigModel? Config { get; init; }
    [JsonPropertyName("took")]
    public float? Took { get; init; }
}
