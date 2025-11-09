using DotNet.Testcontainers.Containers;

namespace Greenwaytech.PiholeApiClient.Test.Extensions;
public static class PiholeClientTestExtensions
{
    public static string GetPiholeTestContainerBaseUrl(this IContainer container, int piholePort) =>
        $"http://{container.Hostname}:{container.GetMappedPublicPort(piholePort)}";
}
