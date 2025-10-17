# PowerSharp Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![VS2022](https://img.shields.io/badge/Visual%20Studio-2022-5C2D91)](https://visualstudio.microsoft.com/)
[![VS Code](https://img.shields.io/badge/VS%20Code-Compatible-007ACC)](https://code.visualstudio.com/)
[![Code Coverage](https://img.shields.io/badge/coverage-run%20tests-blue)](TestResults/CoverageReport/index.html)
[![Tests](https://img.shields.io/badge/tests-xUnit-green)](https://xunit.net/)

**PowerSharp Platform** is a comprehensive suite of tools for supporting an integrated approach to development with **C#**, **PowerShell 7+**, **TypeScript**, **M365**, the **MS Power Platform**, and **Azure**. It aims to simplify the journey between low-code, pro-code, and vibe-coding to speed up time-to-deployment using established Microsoft technologies.

The platform leverages Microsoft development tools where possible, extends them intelligently where needed, and adds to them judiciously from non-Microsoft sources.

---

## 📦 Repository Structure

This monorepo contains multiple integrated projects:

```
PowerSharp/
├── PowerSharp.Core/           # Core middleware & observability for Microsoft Agent Framework
├── PowerSharp.Platform/       # Platform foundation library
├── PowerSharp.Aspire/         # .NET Aspire cloud-native orchestration
│   ├── AppHost/              # Orchestrator app host
│   ├── ApiService/           # Backend API service
│   ├── Web/                  # Blazor web frontend
│   ├── ServiceDefaults/      # Shared service configuration
│   └── Tests/                # Integration tests
├── PnPSharp/                  # Unified PnP & PowerShell hosting library
│   ├── src/PnPSharp/         # Main library
│   ├── samples/              # Console samples
│   └── tests/                # Unit & integration tests
├── PowerSharp.Platform.sln    # Main solution file
└── README.md                  # This file
```

---

## 🚀 Quick Start

### Prerequisites

- **Windows 11** (primary development platform)
- **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** (or later)
- **[PowerShell 7.5+](https://github.com/PowerShell/PowerShell/releases)**
- **[Visual Studio 2022 (17.14+)](https://visualstudio.microsoft.com/)** or **[VS Code](https://code.visualstudio.com/)** with C# extension

### For PnP Development

If you plan to use the **PnPSharp** library with `PnP.PowerShell` cmdlets:

```powershell
# Install PnP.PowerShell module globally
Install-Module PnP.PowerShell -Scope CurrentUser -AllowClobber -Force

# Verify installation
Get-Module -ListAvailable PnP.PowerShell
```

---

## 🛠️ Building the Solution

### Visual Studio 2022

1. Open `PowerSharp.Platform.sln` in Visual Studio 2022
2. Build → **Build Solution** (or press `Ctrl+Shift+B`)
3. Run the startup project of your choice

### VS Code / Command Line

```powershell
# Navigate to repository root
cd c:\_source\repos\PowerSharp

# Restore NuGet packages
dotnet restore PowerSharp.Platform.sln

# Build the solution
dotnet build PowerSharp.Platform.sln -c Debug

# Run tests
dotnet test PowerSharp.Platform.sln -c Debug --no-build
```

---

## 📚 Project Details

### PowerSharp.Core

**Production observability for Microsoft Agent Framework.** Provides middleware implementing:

- **Evidence Accumulation** – Collect and correlate observability signals
- **Resource Allocation** – Track computational resource usage
- **Feedback Loops** – Adaptive Cards integration for user/system feedback

**Technologies:**
- OpenTelemetry for distributed tracing
- Adaptive Cards for UI feedback
- System.CommandLine for CLI tooling

**Key Files:**
- `PowerSharpMiddleware.cs` – Core middleware implementation
- `ResearchAgent.cs` – Example agent with observability
- `ConvertCommand.cs` – CLI command definitions

### PowerSharp.Platform

Foundation library providing shared infrastructure and common services across the platform.

### PowerSharp.Aspire

**.NET Aspire** cloud-native application orchestration stack with:

- **AppHost** – Service orchestration and configuration
- **ApiService** – RESTful backend API
- **Web** – Blazor frontend application
- **ServiceDefaults** – Shared service configuration (logging, health checks, OpenTelemetry)
- **Tests** – Integration and functional tests

**Key Features:**
- Service discovery and configuration
- Distributed application model
- Built-in observability with OpenTelemetry
- Health checks and resilience patterns

### PnPSharp

**Unified .NET 9 library** for accessing PnP SDKs with optional in-process PowerShell 7.x hosting.

**Core Capabilities:**
- Wraps **PnP Core SDK**, **PnP Framework**, and **Microsoft Graph** for high-performance typed access
- Hosts **PowerShell 7** in-process with a **runspace pool** to invoke any cmdlet (including `PnP.PowerShell`)
- Pluggable authentication (device code, certificate-based app-only, interactive)
- Comprehensive unit and integration test coverage

**Architecture:**

```
┌──────────────────────────────────────┐
│  .NET 9 Apps (Web/Worker/Console)   │
└──────────────┬───────────────────────┘
               │
      ┌────────┴────────┐
      │   Direct SDK    │   Cmdlets
      ▼                 ▼
┌─────────────┐   ┌─────────────────────┐
│ PnP Wrappers│   │ PowerShellHostService│
│ (Core/Graph)│   │   (Runspace Pool)   │
└──────┬──────┘   └──────────┬──────────┘
       │                     │
       ▼                     ▼
┌─────────────┐       ┌──────────────┐
│ SharePoint  │       │ PowerShell 7 │
│   & Graph   │       │    Cmdlets   │
└─────────────┘       └──────────────┘
```

**Design Principle:** Prefer direct SDK usage for performance; host PowerShell only when cmdlets are required; reuse runspaces; support pluggable auth.

**Documentation:**
- [`PnPSharp/README.md`](PnPSharp/README.md) – Detailed usage and API docs
- [`PnPSharp/ARCHITECTURE.md`](PnPSharp/ARCHITECTURE.md) – Architecture diagrams
- [`PnPSharp/NuGet.PACKAGES.md`](PnPSharp/NuGet.PACKAGES.md) – Package dependencies
- [`PnPSharp/BUILD.ps1`](PnPSharp/BUILD.ps1) – Build automation script

**PnPSharp Quick Example:**

```csharp
using PnPSharp.PowerShell;

// Create PowerShell host with PnP.PowerShell auto-imported
var opts = new PowerShellHostOptions { 
    MinRunspaces = 1, 
    MaxRunspaces = 5 
};

using var host = new PowerShellHostService(opts);

// Invoke cmdlet
var results = await host.InvokeCmdletAsync("Get-PnPWeb", new Dictionary<string, object?> {
    ["Url"] = "https://contoso.sharepoint.com/sites/demo"
});

foreach (var result in results) {
    Console.WriteLine(result.ToString());
}
```

**Running PnPSharp Tests:**

```powershell
# Unit tests (no external dependencies)
dotnet test PnPSharp/tests/PnPSharp.Tests -c Debug

# Integration tests (requires setup - see PnPSharp/README.md)
# Opt-in by uncommenting ENABLE_INTEGRATION_TESTS in .csproj
dotnet test PnPSharp/tests/PnPSharp.IntegrationTests -c Debug
```

---

## 🧪 Testing

### Test Framework

PowerSharp Platform uses **xUnit** for all test projects with comprehensive code coverage tracking:

- **Test Framework**: xUnit 2.9.2
- **Code Coverage**: coverlet.collector 6.0.2
- **Test Quality**: xUnit.analyzers 1.16.0
- **Distributed App Testing**: Aspire.Hosting.Testing 9.5.1

### Run All Tests

```powershell
# Run all tests in the solution
dotnet test PowerSharp.Platform.sln -c Debug

# Run tests with code coverage
.\RUN-COVERAGE.ps1

# Run tests with HTML coverage report
.\RUN-COVERAGE.ps1 -GenerateReport -Open

# Run tests with coverage threshold enforcement
.\RUN-COVERAGE.ps1 -Threshold 80 -ThresholdType line
```

### Run Specific Test Projects

```powershell
# Run Aspire integration tests
dotnet test PowerSharp.Aspire\PowerSharp.Aspire.Tests -c Debug

# Run PnPSharp unit tests
dotnet test PnPSharp/tests/PnPSharp.Tests -c Debug

# Run PnPSharp integration tests (requires live credentials)
dotnet test PnPSharp/tests/PnPSharp.IntegrationTests -c Debug
```

### Test Projects

- **PowerSharp.Aspire.Tests** (17 tests) – Aspire distributed app integration tests
  - WebTests: 4 tests (web frontend, health checks)
  - ApiServiceTests: 5 tests (weather API validation)
  - AppHostTests: 3 tests (orchestration, resources)
  - WeatherApiClientTests: 5 tests (client behavior, temperature conversion)
  
- **PnPSharp.Tests** – Unit tests with mocking for PnP SDK wrappers

- **PnPSharp.IntegrationTests** – Live SharePoint/Graph tests (opt-in, requires credentials)

### Code Coverage

Code coverage is configured with aspirational thresholds:

- **Global Minimum**: 60% line coverage (enforced in CI/CD)
- **Target Thresholds**: 
  - Line Coverage: 80%
  - Branch Coverage: 70%
  - Method Coverage: 75%

**Coverage Configuration Files:**
- `coverlet.runsettings` – Coverage collection settings
- `.coveragerc` – Documentation and best practices
- `RUN-COVERAGE.ps1` – Automated coverage runner script
- `Directory.Build.props` – SourceLink and deterministic builds

**View Coverage in VS Code:**
1. Install **Coverage Gutters** extension (`ryanluker.vscode-coverage-gutters`)
2. Run tests with coverage: `.\RUN-COVERAGE.ps1`
3. Open any source file
4. Click "Watch" in status bar to enable coverage display

---

## 🔐 Security & Authentication

### PnPSharp Authentication

PnPSharp supports multiple authentication modes:

1. **Interactive (Device Code)** – For development and user-delegated scenarios
2. **Certificate-Based App-Only** – For production automation
3. **Client Credentials** – For service-to-service scenarios

**Best Practices:**
- Use **certificate-based app-only** auth for automation workloads
- Store secrets in **Azure Key Vault** or **Windows Credential Manager**
- Never commit credentials to source control
- Reuse authentication contexts and runspace pools for performance

### Environment Variables for Integration Tests

```powershell
# Set for PnPSharp integration tests
$env:PNP_TEST_SITE_URL = "https://contoso.sharepoint.com/sites/demo"
$env:PNP_TEST_CLIENT_ID = "00000000-0000-0000-0000-000000000000"
$env:PNP_TEST_TENANT_ID = "contoso.onmicrosoft.com"
```

---

## 🏗️ Development Workflow

### Recommended IDE Setup

#### Visual Studio 2022
- Open `PowerSharp.Platform.sln`
- Set desired startup project (e.g., `PowerSharp.Aspire.AppHost` for Aspire, `PnPSharp.Samples` for PnP)
- Use **Test Explorer** for running tests
- Enable code coverage in Test Settings

#### VS Code
1. Install recommended extensions:
   - C# Dev Kit
   - .NET Install Tool
   - PowerShell
2. Open workspace folder
3. Use integrated terminal for `dotnet` commands
4. Use Test Explorer for running tests

### Solution Configuration

The `PowerSharp.Platform.sln` includes:
- Solution folders for logical organization
- Nested PnPSharp projects (library, samples, tests)
- Aspire cloud-native stack
- Core platform libraries

All projects target **.NET 9.0** via `Directory.Build.props` or individual project settings.

---

## 📖 Documentation

- **[PnPSharp Documentation](PnPSharp/README.md)** – Detailed PnP & PowerShell hosting guide
- **[Architecture Diagrams](PnPSharp/ARCHITECTURE.md)** – Visual architecture overview
- **[License](LICENSE)** – MIT License

### Key References

- [.NET 9 Documentation](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- [PnP Core SDK](https://pnp.github.io/pnpcore/)
- [PnP PowerShell](https://pnp.github.io/powershell/)
- [Microsoft Graph SDK](https://learn.microsoft.com/graph/sdks/sdks-overview)
- [PowerShell SDK for .NET](https://learn.microsoft.com/powershell/scripting/dev-cross-plat/choosing-the-right-nuget-package)

---

## 🤝 Contributing

This is currently a private development repository. For questions or collaboration inquiries, please contact the repository owner.

### Branching Strategy

- `main` – Stable, production-ready code
- Feature branches – Use descriptive names (`feature/pnp-caching`, `fix/aspire-build`)

### Code Style

- Follow standard C# conventions
- Enable nullable reference types
- Use `var` judiciously
- Add XML documentation comments for public APIs
- Keep methods focused and testable

---

## 🐛 Known Issues & Troubleshooting

### Visual Studio IDE Crashes

If Visual Studio 2022 crashes during development, use **VS Code** as an alternative:

```powershell
code c:\_source\repos\PowerSharp
```

All functionality is accessible via the command line and VS Code extensions.

### File Lock Errors During Build

If you encounter "Cannot open for writing" errors:

1. Close all running applications
2. Stop any background build processes
3. Clean the solution: `dotnet clean PowerSharp.Platform.sln`
4. Rebuild: `dotnet build PowerSharp.Platform.sln`

### PnP.PowerShell Module Not Found

Ensure the module is installed and visible:

```powershell
Get-Module -ListAvailable PnP.PowerShell
```

If not found, install it:

```powershell
Install-Module PnP.PowerShell -Scope CurrentUser -AllowClobber -Force
```

For custom module paths, configure `PowerShellHostOptions.PnPModulePath`.

### Integration Tests Not Running

Integration tests are **disabled by default**. To enable:

1. Edit `PnPSharp/tests/PnPSharp.IntegrationTests/PnPSharp.IntegrationTests.csproj`
2. Uncomment: `<DefineConstants>ENABLE_INTEGRATION_TESTS</DefineConstants>`
3. Set required environment variables (see [Security & Authentication](#-security--authentication))

---

## 📋 Roadmap

- [ ] Complete PowerSharp.Core observability middleware
- [ ] Expand PnPSharp with additional PnP Framework wrappers
- [ ] Add Aspire Azure deployment templates
- [ ] Publish NuGet packages for stable components
- [ ] CI/CD pipeline with GitHub Actions
- [ ] Comprehensive sample gallery

---

## 📄 License

This project is licensed under the **MIT License** – see the [LICENSE](LICENSE) file for details.

Copyright © 2024 neurodance

---

## 🙏 Acknowledgments

- **[PnP Community](https://pnp.github.io/)** – For exceptional SharePoint and Microsoft 365 tooling
- **[.NET Foundation](https://dotnetfoundation.org/)** – For the .NET platform
- **Microsoft** – For the comprehensive developer ecosystem

---

**Built with ❤️ for the Microsoft 365 and Azure developer community.**
