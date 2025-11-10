using Greenwaytech.PiholeApiClient;
using Greenwaytech.PiholeApiClient.ApiClient;
using Greenwaytech.PiholeApiClient.Model.Pihole.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


//Example usage of the Pi-hole API client with dependency injection and configuration
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        if (!File.Exists("appsettings.json"))
        {
            Console.WriteLine("appsettings.json file not found! " +
                "Please create the file with the necessary configuration for the pihole client." +
                "Use the included appsettings.sample.json as a template.");
        }
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
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
Console.WriteLine($"Save file to {filePath}? (y/n)?[n]");
var fileSaved = Console.ReadLine()?.Trim().ToLower() == "y";
if (fileSaved)
{
    await File.WriteAllBytesAsync(filePath, result.Data?.Data ?? []);
    Console.WriteLine("File saved.");
}
else
{
    Console.WriteLine("File not saved.");
}

// Overwrite the file with invalid/corrupt data for testing
await File.WriteAllBytesAsync(filePath, new byte[] { 0x00, 0xFF, 0xDE, 0xAD, 0xBE, 0xEF });

if (fileSaved)
{
    Console.WriteLine("Attempting to push Pi-hole teleport file...");
    var importRequest = new PiholeTeleportImportRequest
    {
        File = await File.ReadAllBytesAsync(filePath),
        PiholeTeleportImportSettings = new Greenwaytech.PiholeApiClient.Model.Pihole.PiholeTeleportImportSettings
        {
            Config = true,
            DhcpLeases = false,
            Gravity = new Greenwaytech.PiholeApiClient.Model.Pihole.Gravity
            {
                Group = false,
                Adlist = true,
                AdlistByGroup = false,
                Domainlist = true,
                DomainlistByGroup = false,
                Client = false,
                ClientByGroup = false
            }
        }
    };
    var pushResult = await piholeClient.Teleport.PushPiholeTeleportFile(importRequest);
    if (pushResult.IsSuccess && pushResult.Data is not null)
    {
        Console.WriteLine("Successfully pushed teleport file.");
        Console.WriteLine($"Imported config: {string.Join(",", pushResult.Data.Files)} in {pushResult.Data.Took} seconds");
    }
    else
    {
        Console.WriteLine($"Failed to push teleport file: {pushResult.ErrorMessage}");
    }
}
else
{
    Console.WriteLine("Skipping teleport file push since file was not saved.");
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
    Console.WriteLine("Print configuration to console? (y/n)[n]?");
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

//Example in a non-DI context: //TODO:
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
var piholeSection = configuration.GetSection("PiHoleInstanceApiConfig");
var piholeConfig = new Greenwaytech.PiholeApiClient.Model.Configuration.PiHoleInstanceApiConfig
{
    ApiBaseUrl = piholeSection.GetValue<string>("ApiBaseUrl") ?? throw new ArgumentNullException(nameof(piholeSection)),
    ApiKey = piholeSection.GetValue<string>("ApiKey") ?? throw new ArgumentNullException(nameof(piholeSection))
};
using var httpClient = new HttpClient { BaseAddress = new Uri(piholeConfig.ApiBaseUrl) };
var piholeApiClient = new PiholeApiClientService(httpClient, null!, Microsoft.Extensions.Options.Options.Create(piholeConfig));
Console.WriteLine($"Pi-hole client created without DI: {piholeApiClient.GetType().Name}");
