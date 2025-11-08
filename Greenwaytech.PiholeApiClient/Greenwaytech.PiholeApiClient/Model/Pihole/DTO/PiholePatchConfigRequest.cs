using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;

public record PiholePatchConfigRequest
{
    /// <summary>
    /// Add the configuration changes here.
    /// Will not change unspecified settings, only patch the provided ones.
    /// </summary>
    [JsonPropertyName("config")]
    public required PiholeConfigModel Config { get; init; }
}