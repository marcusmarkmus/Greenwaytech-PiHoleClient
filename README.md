# Greenwaytech-PiHoleClient
a .net client library for interacting with the Pi-Hole API - WIP

# Example Usage
Appsettings.json configuration example:
```json
{
  "PiHoleInstanceApiConfig": {
    "ApiBaseUrl": "http://localhost",
    "ApiKey": "<your_api_key>"
  }
}
```
DI registration example:

```csharp
//Example usage of the Pi-hole API client with dependency injection and configuration
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

```

# Features
## Teleport
Pull and push Teleport data to/from Pi-hole instances.

## Config
Pull and push configuration settings to/from Pi-hole instances.