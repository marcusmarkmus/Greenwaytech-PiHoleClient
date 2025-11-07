using Greenwaytech.PiholeApiClient;
using Greenwaytech.PiholeApiClient.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Use extension method for Pi-hole client registration and options
        services.AddPiholeApiClient(options =>
        {
            var section = context.Configuration.GetSection("PiHoleInstanceApiConfig");
            options.ApiBaseUrl = section.GetValue<string>("ApiBaseUrl") ?? throw new ArgumentNullException(nameof(options.ApiBaseUrl));
            options.ApiKey = section.GetValue<string>("ApiKey") ?? throw new ArgumentNullException(nameof(options.ApiKey));
        });
    })
    .Build();

var piholeClient = host.Services.GetRequiredService<IPiholeApiClientService>();
Console.WriteLine($"Pi-hole client resolved: {piholeClient.GetType().Name}");

Console.WriteLine("Pulling Pi-hole teleport file...");
var result = await piholeClient.PullPiholeTeleportFile();
Console.WriteLine($"Received teleport file with content type: {result.Contentype}, size: {result.Data?.Length ?? 0} bytes");
var filePath = Path.Combine(Directory.GetCurrentDirectory(), "pihole_teleport.zip");
Console.WriteLine($"Save file to {filePath}? (y/N)?");
if (Console.ReadLine()?.Trim().ToLower() == "y")
{
    await File.WriteAllBytesAsync(filePath, result.Data ?? Array.Empty<byte>());
    Console.WriteLine("File saved.");
}
else
{
    Console.WriteLine("File not saved.");
}

await host.RunAsync();

