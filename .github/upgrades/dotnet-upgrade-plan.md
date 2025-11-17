# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade DemoProject\DemoProject.csproj
4. Upgrade Greenwaytech.PiholeApiClient.Test\Greenwaytech.PiholeApiClient.Test.csproj
5. Run unit tests to validate upgrade in the projects listed below:
  - Greenwaytech.PiholeApiClient.Test\Greenwaytech.PiholeApiClient.Test.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|
| Greenwaytech.PiholeApiClient\Greenwaytech.PiholeApiClient.csproj | Explicitly excluded         |

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                        | Current Version | New Version | Description                                   |
|:------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.Extensions.Hosting        |   9.0.10        |  10.0.0-rc.2.25502.107 | Recommended for .NET 10.0                     |
| Microsoft.Extensions.Logging        |   9.0.10        |  10.0.0-rc.2.25502.107 | Recommended for .NET 10.0                     |
| Microsoft.Extensions.Logging.Console|   9.0.10        |  10.0.0-rc.2.25502.107 | Recommended for .NET 10.0                     |
| Microsoft.Extensions.Options        |   9.0.10        |  10.0.0-rc.2.25502.107 | Recommended for .NET 10.0                     |

### Project upgrade details

#### DemoProject modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.Extensions.Hosting should be updated from `9.0.10` to `10.0.0-rc.2.25502.107` (*recommended for .NET 10.0*)

#### Greenwaytech.PiholeApiClient.Test modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.Extensions.Logging should be updated from `9.0.10` to `10.0.0-rc.2.25502.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Logging.Console should be updated from `9.0.10` to `10.0.0-rc.2.25502.107` (*recommended for .NET 10.0*)
  - Microsoft.Extensions.Options should be updated from `9.0.10` to `10.0.0-rc.2.25502.107` (*recommended for .NET 10.0*)
