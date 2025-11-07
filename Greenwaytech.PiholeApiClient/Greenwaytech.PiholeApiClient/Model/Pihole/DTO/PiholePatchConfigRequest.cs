using Greenwaytech.PiholeApiClient.Model.Pihole;
using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

public record PiholePatchConfigRequest
{
    [JsonPropertyName("config")]
    public required PiholeConfigModel Config { get; init; }
}