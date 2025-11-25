# Greenwaytech-PiHoleClient
a .net client library for interacting with the Pi-Hole API - Work in progress!

# Example Usage

## Configuration
Add the  [Greenwaytech.PiholeApiClient nuget package](https://www.nuget.org/packages/Greenwaytech.PiholeApiClient/)  in your preferred way.
 
Appsettings.json configuration example:
```json
{
  "PiHoleInstanceApiConfig": {
    "ApiBaseUrl": "http://localhost",
    "ApiKey": "<your_api_key>"
  }
}

### Dependency Injection
```
DI registration example, single client:

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
//Usually just inject IPiholeApiClientService in your service.

```
DI registration example, factory (multiple clients):

```csharp
var hostMultiplePiholeNodes = Host.CreateDefaultBuilder(args)
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
        services.AddPiholeApiClientFactory();
        var section = context.Configuration.GetSection("PiHoleInstanceApiConfig");
        
        var ApiBaseUrl = section.GetValue<string>("ApiBaseUrl") ?? throw new Exception(nameof(PiHoleInstanceApiConfig.ApiBaseUrl) + " not found in configsettings");
        var ApiKey = section.GetValue<string>("ApiKey") ?? throw new Exception(nameof(PiHoleInstanceApiConfig.ApiKey) + " not found in configsettings");
        var options = new PiHoleInstanceApiConfig() { ApiBaseUrl = ApiBaseUrl, ApiKey = ApiKey};
        services.AddTransient(_ => Options.Create(options));
    })
    .Build();

var piholeClientFactory = hostMultiplePiholeNodes.Services.GetRequiredService<Greenwaytech.PiholeApiClient.Providers.IPiholeClientFactory>();
var piholeConfigFactoryExample = hostMultiplePiholeNodes.Services.GetRequiredService<IOptions<PiHoleInstanceApiConfig>>().Value;
var piholeClientFromFactory = piholeClientFactory.CreateClient(piholeConfigFactoryExample);
Console.WriteLine($"Pi-hole client resolved from factory: {piholeClientFromFactory.GetType().Name}");
//Usually you would just inject IPiholeClientFactory in your service and create clients there.

```


### Non-Di example usage:
```csharp
//todo!
```

# Current Features
## Teleport
Pull and push Teleport data to/from Pi-hole instances.
Todo: examplecode
## Config
Pull and push configuration settings to/from Pi-hole instances.
Todo: excample code
## Actions
Perform actions
todo: example code
# Next planned features
## Get messages from pihole
Todo
## Abstract parts of the big config object
For example methods for "add local dns setting" that will handle everything behind the scenes of pathing the config object

