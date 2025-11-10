namespace Greenwaytech.PiholeApiClient.Model.Pihole;

public record PiholeTeleportExport
{
    public required byte[]? Data { get; set; }
    public required string Contentype { get; set; } //= "application/zip";
    public required string contentDisposition { get; set; }// = "attachment; filename=pihole-teleport-export.zip";

}
