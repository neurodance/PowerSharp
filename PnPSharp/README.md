# PnPSharp (.NET 9) – Unified PnP + In‑Process PowerShell Hosting

A .NET 9 class library that:
- Wraps **PnP Core SDK / PnP Framework / Microsoft Graph** for high‑performance, typed access
- Hosts **PowerShell 7** in‑process with a **runspace pool** to invoke any cmdlet (including `PnP.PowerShell`)

Windows 11 dev, works in **Visual Studio 2022** and **VS Code**.

## Repository layout
```
src/PnPSharp/                       # Library (PowerShell host + PnP wrappers)
samples/PnPSharp.Samples/           # Console sample
tests/PnPSharp.Tests/               # xUnit unit tests (mockable interface)
tests/PnPSharp.IntegrationTests/    # Opt-in, interactive device-code tests
.github/workflows/dotnet-ci.yml     # CI: Windows runner, .NET 9, unit tests + coverage
Directory.Build.props               # net9.0 for all projects
ARCHITECTURE.md                     # Diagram + design notes
NuGet.PACKAGES.md                   # Package list
BUILD.ps1                           # Build/run helper
PnPSharp.sln
```

## Quick start (Visual Studio 2022)
1. Open `PnPSharp.sln`
2. Restore/build → set **PnPSharp.Samples** as startup → **F5**

## Quick start (VS Code)
```powershell
pwsh ./BUILD.ps1
# or explicitly:
dotnet restore
dotnet build -c Debug
dotnet run --project .\samples\PnPSharp.Samples
dotnet test -c Debug --collect:"XPlat Code Coverage"
```

## Auto‑importing the PnP.PowerShell module
`PowerShellHostService` **auto-imports** `PnP.PowerShell` in each runspace by default. Install the module once:
```powershell
Install-Module PnP.PowerShell -Scope CurrentUser -AllowClobber -Force
Get-Module -ListAvailable PnP.PowerShell
```
If your module is in a non-standard path, set:
```csharp
var opts = new PnPSharp.PowerShell.PowerShellHostOptions {
  PnPModulePath = @"C:\Program Files\PowerShell\7\Modules\PnP.PowerShell"
};
using var host = new PnPSharp.PowerShell.PowerShellHostService(opts);
```

## How to consume the library
- Add a **ProjectReference** to `src/PnPSharp/PnPSharp.csproj`, or reference the built DLL.
- Prefer **direct SDK** wrappers (`PnPSharp.PnP.*`) for performance; use the PowerShell host only when cmdlets are required.

## Running tests
**Visual Studio**: Test → Run All Tests  
**CLI**:
```powershell
dotnet test -c Debug --collect:"XPlat Code Coverage"
```

## Integration tests (opt-in)
Disabled by default:
1. In `tests/PnPSharp.IntegrationTests/PnPSharp.IntegrationTests.csproj`, uncomment:
   ```xml
   <DefineConstants>ENABLE_INTEGRATION_TESTS</DefineConstants>
   ```
2. Set:
   ```powershell
   $env:PNP_TEST_SITE_URL = "https://contoso.sharepoint.com/sites/demo"
   $env:PNP_TEST_CLIENT_ID = "00000000-0000-0000-0000-000000000000"
   ```
3. Run:
   ```powershell
   dotnet test tests/PnPSharp.IntegrationTests -c Debug
   ```

## CI (GitHub Actions)
Workflow at `.github/workflows/dotnet-ci.yml` runs on **windows-latest**, installs **.NET 9**, builds, runs **unit tests** with coverage, and uploads artifacts.

## Security & performance
- Use **certificate-based app-only** auth for automation; keep secrets in Cert Store or Key Vault.
- Reuse the **runspace pool**; prefer direct SDK calls for data operations.

## References
- PowerShell NuGet package guidance: https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/choosing-the-right-nuget-package?view=powershell-7.5
- PowerShell from C# in-process (2025): https://codecube.net/2025/7/powershell-from-csharp-updated/
- PnP Core SDK/PowerShell relationship: https://github.com/pnp/pnpcore/discussions/1036
- Runspaces from C#: https://stackoverflow.com/questions/21264112/how-to-call-powershell-cmdlets-from-c-sharp-in-visual-studio