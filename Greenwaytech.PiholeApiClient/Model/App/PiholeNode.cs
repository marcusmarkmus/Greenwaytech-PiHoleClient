using Greenwaytech.PiholeApiClient.Model.Configuration;

namespace Greenwaytech.PiholeApiClient.Model.App;

/// <summary>
/// Represents a Pi-hole node configuration.
/// Meant to be used in scenarios with multiple Pi-hole instances.
/// </summary>
public record PiholeNode 
{
    public required string Name { get; init; } 
    public required PiHoleInstanceApiConfig Config { get; init; }
    public PiholeNodeRole Role { get; init; } = PiholeNodeRole.Standalone;
    public string Description { get; init; } = "";

}
