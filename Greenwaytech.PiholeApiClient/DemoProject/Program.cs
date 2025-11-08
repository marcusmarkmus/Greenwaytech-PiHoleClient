using Greenwaytech.PiholeApiClient;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
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
var result = await piholeClient.Teleport.PullPiholeTeleportFile();
Console.WriteLine($"Received teleport file with content type: {result.Data?.Contentype}, size: {result.Data?.Data?.Length ?? 0} bytes");
var filePath = Path.Combine(Directory.GetCurrentDirectory(), "pihole_teleport.zip");
Console.WriteLine($"Save file to {filePath}? (y/N)?");
if (Console.ReadLine()?.Trim().ToLower() == "y")
{
    await File.WriteAllBytesAsync(filePath, result.Data?.Data ?? []);
    Console.WriteLine("File saved.");
}
else
{
    Console.WriteLine("File not saved.");
}

Console.WriteLine("Attempting to patch Pi-hole configuration...");
var patchConfig = new PiholePatchConfigRequest
{
    Config = new Greenwaytech.PiholeApiClient.Model.Pihole.PiholeConfigModel
    {
        dns = new Greenwaytech.PiholeApiClient.Model.Pihole.Dns
        {
            hosts = ["10.10.10.2 test.test"]
        }
    }
};


var patchResult = await piholeClient.Config.PatchPiholeConfigAsync(patchConfig);

Console.WriteLine("Getting Pi-hole configuration...");
var configResult = await piholeClient.Config.GetPiholeConfigAsync(detailed: true);
if (configResult.IsSuccess && configResult.Data is not null)
{
    Console.WriteLine("Successfully retrieved Pi-hole configuration.");
    Console.WriteLine("Print configuration to console? (y/N)?");
    if (Console.ReadLine()?.Trim().ToLower() != "y")
    {
        return;
    }
    Console.WriteLine("Pi-hole Configuration:");
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(configResult.Data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

}
else
{
    Console.WriteLine($"Failed to get configuration: {configResult.ErrorMessage}");
}

//await host.RunAsync();

