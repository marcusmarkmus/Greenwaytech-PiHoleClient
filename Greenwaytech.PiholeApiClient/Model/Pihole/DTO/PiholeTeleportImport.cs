using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
public record PiholeTeleportImportRequest
{
    /// <summary>
    /// The Pi-hole Teleport file as a byte array, use an exported file from /api/teleporter
    /// </summary>
    [JsonPropertyName("file")]
    public required byte[] File { get; init; }

    /// <summary>
    /// If omitted, all files will be imported.
    /// </summary>
    [JsonPropertyName("import")]
    public PiholeTeleportImportSettings? PiholeTeleportImportSettings { get; init; } = new PiholeTeleportImportSettings();

}
