# PnPSharp Copilot Instructions

## Project Overview
PnPSharp is a .NET 9 library that provides **two distinct integration paths** for SharePoint/M365 automation:
1. **Direct SDK wrappers** (`PnPSharp.PnP.*`) - Fast, typed access via PnP.Core SDK, PnP.Framework, and Microsoft Graph
2. **In-process PowerShell host** (`PnPSharp.PowerShell.*`) - Runspace pool to invoke any PowerShell cmdlet, including PnP.PowerShell module

**Critical Design Principle**: Prefer direct SDK wrappers for performance; use PowerShell host **only when cmdlets are required**.

## Architecture
```
src/PnPSharp/              # Core library
├── PowerShell/            # In-process PS7 hosting (runspace pool)
│   ├── IPowerShellHostService.cs (mockable interface)
│   └── PowerShellHostService.cs  (runspace pool impl)
└── PnP/                   # Direct SDK wrappers
    ├── PnPContextFactory.cs      (auth: cert-based app-only, device code)
    └── SharePointOperations.cs   (typed operations)
```

See `ARCHITECTURE.md` for the Mermaid diagram and design rationale.

## Key Technologies & Patterns

### PowerShell Hosting
- Uses `Microsoft.PowerShell.SDK` 7.5.0 to host PS7 in-process
- **Runspace pool** (default 1-5) provides thread-safe, reusable execution contexts
- Auto-imports `PnP.PowerShell` module in each runspace (configurable via `PowerShellHostOptions`)
- Errors from `ps.Streams.Error` are thrown as `InvalidOperationException` with concatenated messages
- **Always implement `IPowerShellHostService`** interface for testability with Moq

### Authentication Patterns
Two paths in `PnPContextFactory`:
- `CreateAppOnlyAsync()`: Certificate-based (prod automation - secure!)
- `CreateInteractiveAsync()`: Device code flow (dev/testing only)

### Testing Strategy
- **Unit tests** (`tests/PnPSharp.Tests`): Mock `IPowerShellHostService` with Moq - runs in CI
- **Integration tests** (`tests/PnPSharp.IntegrationTests`): Opt-in via `<DefineConstants>ENABLE_INTEGRATION_TESTS</DefineConstants>` + env vars (`PNP_TEST_SITE_URL`, `PNP_TEST_CLIENT_ID`) - **NOT in CI**
- Coverage via `coverlet.collector` using `XPlat Code Coverage` format

## Development Workflows

### Building
```powershell
# Recommended: use BUILD.ps1 helper
pwsh .\BUILD.ps1              # Debug build + run sample
pwsh .\BUILD.ps1 -Release     # Release build

# Or manually:
dotnet build -c Debug
dotnet run --project .\samples\PnPSharp.Samples
```

### Testing
```powershell
# Unit tests only (runs in CI):
dotnet test .\tests\PnPSharp.Tests -c Debug --collect:"XPlat Code Coverage"

# Integration tests (opt-in):
# 1. Uncomment <DefineConstants>ENABLE_INTEGRATION_TESTS</DefineConstants> in .csproj
# 2. Set env vars: PNP_TEST_SITE_URL, PNP_TEST_CLIENT_ID
# 3. Run: dotnet test .\tests\PnPSharp.IntegrationTests -c Debug
```

### CI Pipeline
`.github/workflows/dotnet-ci.yml` runs on `windows-latest`:
- .NET 9 SDK
- Builds solution in `Release` mode
- Runs **only** `PnPSharp.Tests` (not integration tests)
- Uploads Cobertura coverage + trx results

## Project-Specific Conventions

### C# Standards
- **Target**: `net9.0` (enforced in `Directory.Build.props`)
- **Language**: `LangVersion=preview`, `Nullable=enable`, `ImplicitUsings=enable`
- Use `ConfigureAwait(false)` for all library `await` calls (see `PowerShellHostService.InvokeAsync`)

### Dependency Management
All package versions specified in individual `.csproj` files (no central package management):
- PowerShell SDK: 7.5.0
- PnP.Core/Auth: 1.12.0
- Microsoft.Graph: 5.52.0
- xUnit: 2.9.2
- See `NuGet.PACKAGES.md` for full list

### Code Organization
- **Interfaces before implementations**: Always define `I*Service` interface (e.g., `IPowerShellHostService`) for mockability
- **Options pattern**: Use `*Options` classes (e.g., `PowerShellHostOptions`) with `init` properties for immutable configuration
- **Static factories**: Use static classes for factory methods (e.g., `PnPContextFactory.CreateAppOnlyAsync()`)

### Error Handling
- PowerShell errors: Check `ps.Streams.Error.Count` and concatenate messages into `InvalidOperationException`
- Async operations: Use `Task.Factory.FromAsync()` for PS `BeginInvoke/EndInvoke` pattern

## Common Tasks

### Adding New PowerShell Functionality
1. Add method to `IPowerShellHostService` interface
2. Implement in `PowerShellHostService` using `_pool` runspace
3. Add unit test mocking the interface with Moq
4. Add smoke test in `PowerShellHostServiceSmokeTests.cs` if needed

### Adding New PnP SDK Wrappers
1. Add static method to `PnP/SharePointOperations.cs` (or new static class)
2. Use `PnPContext` parameter and typed PnP.Core models
3. Example: `await ctx.Web.Lists.AddAsync(title, ListTemplateType.GenericList)`

### Updating Dependencies
- Check `NuGet.PACKAGES.md` for current versions
- Update individual `.csproj` files (no centralized version props)
- Verify PowerShell SDK compatibility: https://learn.microsoft.com/powershell/scripting/dev-cross-plat/choosing-the-right-nuget-package

## Critical Knowledge

### Why Runspace Pool?
- Thread-safe reuse across async operations
- Avoids repeated module import overhead (PnP.PowerShell is slow to load)
- Configured via `MinRunspaces`/`MaxRunspaces` in options

### Module Import Behavior
`PowerShellHostService` auto-imports `PnP.PowerShell` via `InitialSessionState.ImportPSModule()` during pool creation. Users must pre-install:
```powershell
Install-Module PnP.PowerShell -Scope CurrentUser -AllowClobber -Force
```

### VS Code vs Visual Studio
- **VS Code**: Use `BUILD.ps1` or manual `dotnet` commands
- **VS 2022**: Open `PnPSharp.sln`, F5 to run sample project
- Both supported; Windows 11 dev environment required (PowerShell SDK dependency)

## Integrating into Existing Solutions

### Directory.Build.props Conflict Warning
PnPSharp includes a `Directory.Build.props` at its root that enforces `net9.0`, `LangVersion=preview`, `Nullable=enable`, and `ImplicitUsings=enable`. MSBuild cascades these settings from root to leaf directories.

**If your solution already has `Directory.Build.props`**, you'll get merged settings that may conflict.

**Resolution strategies**:
1. **Rename and import** (recommended): `Rename-Item Directory.Build.props Directory.PnPSharp.props`, then add `<Import Project="..\..\Directory.PnPSharp.props" />` to `src/PnPSharp/PnPSharp.csproj`
2. **Conditional import**: In your solution's props, add `<Import Project="$(MSBuildThisFileDirectory)\PnPSharp\Directory.Build.props" Condition="..." />` targeting only PnPSharp projects
3. **Move settings**: Transfer properties directly into `src/PnPSharp/PnPSharp.csproj` and delete `Directory.Build.props`

### Integration Approaches

**Option 1: Full Integration** (Active development/testing)
Add all projects to your solution using solution folders for organization:
```
YourSolution.sln
├── (Your existing projects)
└── 📁 PnPSharp
    ├── PnPSharp (library)              ← src/PnPSharp/PnPSharp.csproj
    ├── 📁 Samples
    │   └── PnPSharp.Samples            ← samples/PnPSharp.Samples/PnPSharp.Samples.csproj
    ├── 📁 Tests
    │   ├── PnPSharp.Tests              ← tests/PnPSharp.Tests/PnPSharp.Tests.csproj
    │   └── PnPSharp.IntegrationTests   ← tests/PnPSharp.IntegrationTests/PnPSharp.IntegrationTests.csproj
    └── 📁 Documentation (Solution Items: README.md, ARCHITECTURE.md, NuGet.PACKAGES.md)
```
Benefits: Unified Test Explorer, single build, samples available for reference, easy to modify PnPSharp alongside your code.

**Option 2: Library Only** (Production use)
Add only `src/PnPSharp/PnPSharp.csproj` to your solution. Minimal clutter, faster builds, but no immediate access to samples/tests.

**Option 3: Git Submodule** (Long-term maintenance)
```powershell
cd YourSolutionDirectory
git submodule add <PnPSharp-repo-url> lib/PnPSharp
```
Then add projects as in Option 1. Enables independent updates and version tracking.

### Adding to VS2022 Solution (Step-by-Step)
```
1. Handle Directory.Build.props conflict first (see strategies above)
2. Verify .NET 9 SDK is installed (Tools → Options → Environment)
3. Create solution folders: Right-click solution → Add → New Solution Folder → "PnPSharp"
4. Add projects: Right-click "PnPSharp" folder → Add → Existing Project
   - Core: src/PnPSharp/PnPSharp.csproj
   - Optional: samples/PnPSharp.Samples/PnPSharp.Samples.csproj (subfolder "Samples")
   - Optional: tests/PnPSharp.Tests/PnPSharp.Tests.csproj (subfolder "Tests")
   - Optional: tests/PnPSharp.IntegrationTests/PnPSharp.IntegrationTests.csproj (subfolder "Tests")
5. Add documentation: Right-click solution → Add → Existing Item → select README.md, ARCHITECTURE.md, etc.
6. Add ProjectReference in consuming projects:
   <ProjectReference Include="..\PnPSharp\src\PnPSharp\PnPSharp.csproj" />
7. Build solution to verify no conflicts
```

### Cross-Platform Compatibility
`Microsoft.PowerShell.SDK` 7.5.0 is **Windows-only**. If your solution targets Linux/macOS, PnPSharp projects will fail to build on non-Windows platforms. Consider:
- Solution configurations that exclude PnPSharp on non-Windows builds
- Conditional `<ProjectReference>` with platform guards
- Using only the direct SDK wrappers (PnP.Core) in multi-platform scenarios
