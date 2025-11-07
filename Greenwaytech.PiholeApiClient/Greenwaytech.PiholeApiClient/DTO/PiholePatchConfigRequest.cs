using Greenwaytech.PiholeApiClient.Model;
using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.DTO;

public record PiholePatchConfigRequest
{
    [JsonPropertyName("config")]
    public required PiholeConfigModel Config { get; init; }
}