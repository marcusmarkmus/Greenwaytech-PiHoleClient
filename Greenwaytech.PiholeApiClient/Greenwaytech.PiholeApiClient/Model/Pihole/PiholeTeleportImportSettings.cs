using System.Text.Json.Serialization;

namespace Greenwaytech.PiholeApiClient.Model.Pihole;

public record PiholeTeleportImportSettings
{
    [JsonPropertyName("config")]
    public bool Config { get; set; } = true;
    [JsonPropertyName("dhcp_leases")]
    public bool DhcpLeases { get; set; } = true;
    [JsonPropertyName("gravity")]
    public Gravity Gravity { get; set; } = new Gravity();
}

public class Gravity
{
    [JsonPropertyName("group")]
    public bool Group { get; set; } = true;
    [JsonPropertyName("adlist")]
    public bool Adlist { get; set; } = true;
    [JsonPropertyName("adlist_by_group")]
    public bool AdlistByGroup { get; set; } = true;
    [JsonPropertyName("domainlist")]
    public bool Domainlist { get; set; } = true;
    [JsonPropertyName("domainlist_by_group")]
    public bool DomainlistByGroup { get; set; } = true;
    [JsonPropertyName("client")]
    public bool Client { get; set; } = true;
    [JsonPropertyName("client_by_group")]
    public bool ClientByGroup { get; set; } = true;
}
