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
          // Set multiple environment variables to ensure authentication works
          .WithEnvironment(PiholeWebserverPasswordEnvVar, PiholePassword)
          .WithEnvironment("WEBPASSWORD", PiholePassword) // Legacy/alternative format
          .WithEnvironment("WEB_PASSWORD", PiholePassword) // Alternative format
          .WithWaitStrategy(
              Wait.ForUnixContainer()
                  .UntilInternalTcpPortIsAvailable(PiholePort)
                  // Wait for Pi-hole web server to be ready by checking login page
                  .UntilHttpRequestIsSucceeded(request => request
                      .ForPort(PiholePort)
                      .ForPath("/admin/login")
                      .ForStatusCode(System.Net.HttpStatusCode.OK))
          )
          .Build();
}
