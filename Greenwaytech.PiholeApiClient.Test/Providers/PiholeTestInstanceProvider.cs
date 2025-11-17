using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Greenwaytech.PiholeApiClient.Test.Providers;

public static class PiholeTestInstanceProvider
{
    private const string PiholeImage = "pihole/pihole:latest";
    public const string PiholePassword = "testpassword";
    public const string PiholeWebserverPasswordEnvVar = "FTLCONF_webserver_api_password";
    public const int PiholePort = 80;

    public static IContainer BuildPiholeTestContainer()
        => new ContainerBuilder()
          .WithImage(PiholeImage)
          .WithPortBinding(PiholePort, true)
          .WithEnvironment(PiholeWebserverPasswordEnvVar, PiholePassword)
          .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(PiholePort))
          .Build();


}
