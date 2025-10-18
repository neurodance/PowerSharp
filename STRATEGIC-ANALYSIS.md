# PowerSharp Platform - Strategic Analysis: Upstream Sync & Adaptive Cards Designer

> **Date:** October 17, 2025  
> **Author:** GitHub Copilot  
> **Status:** Strategic Planning Document

---

## Part 1: Upstream Dependency Sync Strategy

### Current OSS Dependencies

**PowerSharp.Core:**
- `AdaptiveCards` v3.1.0 (NuGet package, MIT license)

**PnPSharp:**
- `PnP.Core` v1.12.0 (NuGet package, MIT license)
- `PnP.Core.Auth` v1.12.0 (NuGet package, MIT license)
- `PnP.Framework` v1.14.0 (NuGet package, MIT license)
- `Microsoft.Graph` v5.52.0 (NuGet package, MIT license)

### Sync Strategy Recommendations

#### Scenario A: NuGet Packages (Current Approach - RECOMMENDED)

**Status:** ✅ **This is your current approach and is OPTIMAL**

**How it works:**
- All dependencies are consumed as NuGet packages
- No forking or submodules required
- Clean separation between your code and upstream

**Update Process:**
```powershell
# Check for updates
dotnet list package --outdated

# Update specific package
dotnet add package PnP.Core --version 1.13.0

# Update all packages in solution
dotnet list package --outdated | Select-String ">" | ForEach-Object {
    # Parse and update each package
}
```

**Pros:**
- ✅ Zero maintenance overhead for upstream sync
- ✅ Semantic versioning - control when to update
- ✅ Clean builds, no source code conflicts
- ✅ Can target specific versions for stability
- ✅ Easy to test new versions in isolation

**Cons:**
- ❌ Cannot modify upstream code directly
- ❌ Must wait for NuGet releases for bug fixes
- ❌ Limited customization options

**Verdict:** **Keep this approach for PnP SDKs and Microsoft.Graph**

---

#### Scenario B: Git Submodules (For Future OSS with Custom Mods)

**When to use:**
- You need to make custom modifications to upstream
- Upstream doesn't accept your PRs
- You want to track upstream closely but maintain local changes

**Implementation:**
```powershell
# Add submodule
git submodule add https://github.com/microsoft/AdaptiveCards.git external/AdaptiveCards
git submodule update --init --recursive

# Update from upstream
cd external/AdaptiveCards
git fetch origin
git merge origin/main
cd ../..
git add external/AdaptiveCards
git commit -m "Update AdaptiveCards submodule"
```

**Directory Structure:**
```
PowerSharp/
├── external/
│   ├── AdaptiveCards/        # Git submodule
│   └── CustomLibrary/         # Another submodule
├── PowerSharp.Core/
└── PowerSharp.Platform.sln
```

**Pros:**
- ✅ Full source access for modifications
- ✅ Can track specific upstream commits
- ✅ Easy to sync with upstream main branch
- ✅ Maintains git history from upstream

**Cons:**
- ❌ Complex merge conflicts when customizing
- ❌ Requires team understanding of submodules
- ❌ Build complexity (multiple build systems)
- ❌ CI/CD must handle recursive checkouts

**Verdict:** **Use only if you MUST customize upstream code**

---

#### Scenario C: Forked Repository (For Heavy Customization)

**When to use:**
- Extensive modifications to upstream
- Maintaining proprietary extensions
- Want to contribute back via PRs from your fork

**GitHub Fork Strategy:**
```
Upstream: microsoft/AdaptiveCards (main)
   ↓ fork
Your Fork: neurodance/AdaptiveCards (main + custom-features branch)
   ↓ reference
PowerSharp: Uses forked NuGet or submodule
```

**Sync Process:**
```powershell
# In your fork
git remote add upstream https://github.com/microsoft/AdaptiveCards.git
git fetch upstream
git checkout main
git merge upstream/main
git push origin main

# Rebase custom features
git checkout custom-features
git rebase main
git push origin custom-features --force-with-lease
```

**Pros:**
- ✅ Full control over codebase
- ✅ Can publish custom NuGet packages
- ✅ Easy to contribute back via PRs
- ✅ Clear separation: main = upstream, branches = custom

**Cons:**
- ❌ Maintenance burden for keeping fork updated
- ❌ Rebase/merge complexity with custom features
- ❌ Must publish own NuGet packages
- ❌ Team confusion about which repo to use

**Verdict:** **Use for AdaptiveCards Designer (see Part 2)**

---

### Recommended Strategy by Dependency Type

| Dependency Type | Strategy | Rationale |
|----------------|----------|-----------|
| **PnP SDKs** | NuGet packages | Stable APIs, regular releases, no customization needed |
| **Microsoft.Graph** | NuGet packages | Official SDK, well-maintained, breaking changes rare |
| **AdaptiveCards (rendering)** | NuGet packages | Stable rendering library, no customization needed |
| **AdaptiveCards Designer** | Fork + Custom Build | Heavy customization planned (see Part 2) |
| **Future low-code tools** | Evaluate case-by-case | Default to NuGet if available |

---

### Automation: Update Check Script

Create `CHECK-UPDATES.ps1`:

```powershell
#!/usr/bin/env pwsh
# Check for NuGet package updates across solution

$ErrorActionPreference = "Stop"

Write-Host "=== PowerSharp Platform - Package Update Check ===" -ForegroundColor Cyan

# Get outdated packages
$outdated = dotnet list PowerSharp.Platform.sln package --outdated --format json | ConvertFrom-Json

$hasUpdates = $false

foreach ($project in $outdated.projects) {
    $projectName = $project.path
    $frameworks = $project.frameworks
    
    foreach ($framework in $frameworks) {
        $packages = $framework.topLevelPackages
        
        foreach ($pkg in $packages) {
            if ($pkg.requestedVersion -ne $pkg.latestVersion) {
                $hasUpdates = $true
                Write-Host "`n📦 $($pkg.id)" -ForegroundColor Yellow
                Write-Host "   Project: $projectName" -ForegroundColor Gray
                Write-Host "   Current: $($pkg.requestedVersion) → Latest: $($pkg.latestVersion)" -ForegroundColor Gray
                
                # Check for breaking changes (major version bump)
                $currentMajor = ($pkg.requestedVersion -split '\.')[0]
                $latestMajor = ($pkg.latestVersion -split '\.')[0]
                
                if ($currentMajor -ne $latestMajor) {
                    Write-Host "   ⚠️  MAJOR VERSION CHANGE - Review breaking changes!" -ForegroundColor Red
                }
            }
        }
    }
}

if (-not $hasUpdates) {
    Write-Host "`n✅ All packages are up to date!" -ForegroundColor Green
}

Write-Host "`nTo update a package:" -ForegroundColor Cyan
Write-Host "  dotnet add <project> package <PackageName> --version <Version>" -ForegroundColor Gray
```

---

## Part 2: Adaptive Cards Designer Analysis

### Executive Summary

**Goal 1 (Use existing source):** ✅ **FEASIBLE** - MIT licensed, well-structured TypeScript  
**Goal 2 (Port to Blazor/C#):** ⚠️ **COMPLEX BUT ACHIEVABLE** with caveats

### Current Designer Architecture

**Technology Stack:**
- **Language:** TypeScript
- **Build:** Webpack 5
- **Editor:** Monaco Editor (VS Code editor component)
- **Renderer:** AdaptiveCards JavaScript SDK
- **Templating:** AdaptiveCards Templating Engine
- **UI Framework:** None (vanilla TypeScript + CSS)
- **License:** MIT (Microsoft)

**Key Components:**

1. **card-designer.ts** (~2000 LOC)
   - Main orchestrator
   - Manages toolbar, panels, editors
   - Undo/redo stack
   - Event coordination

2. **card-designer-surface.ts** (~600 LOC)
   - Card rendering surface
   - Drag-and-drop handling
   - Designer "peers" (visual overlays on elements)

3. **designer-peers.ts**
   - Visual representation of card elements
   - Property sheets
   - Element manipulation

4. **Monaco Editor Integration**
   - JSON editor with schema validation
   - Data editor
   - Host data editor

5. **Tool Palette**
   - Draggable element palette
   - Custom palette items support

6. **Container System**
   - Multiple host containers (Teams, Outlook, WebChat, etc.)
   - Theme support
   - Device emulation

**Repository Structure:**
```
microsoft/AdaptiveCards/source/nodejs/
├── adaptivecards-designer/        # Core designer library (MIT)
├── adaptivecards-designer-app/    # Standalone app wrapper
├── adaptivecards/                 # Rendering SDK
├── adaptivecards-controls/        # UI controls
├── adaptivecards-templating/      # Templating engine
└── adaptivecards-react/           # React bindings (relevant!)
```

---

### Assessment: Using Existing TypeScript Source

#### ✅ Pros - Why This Works Well

1. **Clean MIT License**
   - Full permission to fork, modify, redistribute
   - Can use commercially in SaaS
   - Must retain copyright notice

2. **Well-Architected**
   - Modular design with clear separation
   - Extension points for customization
   - Documented in CONTRIBUTING.md

3. **Active Maintenance**
   - Microsoft continues development
   - Regular security updates
   - Community contributions

4. **Rich Feature Set**
   - Monaco Editor integration (industry-standard)
   - Multiple host containers
   - Theme support
   - Device emulation
   - Sample catalog
   - Image-to-card (Pic2Card)
   - JSON Schema import

5. **Extensibility Hooks**
   - Custom palette items
   - Custom host containers
   - Custom peer commands
   - Custom toolbox commands

#### Strategy: Fork + Extend Approach

**Recommended Path:**
1. Fork `microsoft/AdaptiveCards` to `neurodance/AdaptiveCards`
2. Create branch: `powersharp-customizations`
3. Build custom features in your branch
4. Periodically sync main branch from upstream
5. Publish custom build as part of PowerSharp Platform

**Custom Features to Add:**
- PowerSharp-specific palette items
- Integration with PowerSharp.Core observability
- Custom export formats (PowerShell scripts, C# code gen)
- Advanced templating UI for M365/Power Platform scenarios
- Versioning and collaboration features

---

### Assessment: Porting to Blazor/C#

#### 🔄 Feasibility Analysis

**Complexity: HIGH (8/10)**

**Estimated Effort: 8-12 weeks for MVP**

#### Why Blazor is Attractive

1. **Unified Stack**
   - Same language as PowerSharp Platform (C#)
   - Fits well with Aspire architecture
   - Can share models with backend

2. **Server-Side Rendering**
   - Better for authenticated enterprise scenarios
   - Easier to integrate with Azure AD/Entra ID
   - Natural fit for M365/Power Platform integration

3. **Component Model**
   - Clean separation of concerns
   - Easier testability than vanilla TypeScript
   - Reusable components across PowerSharp suite

4. **Debugging**
   - C# debugging tools in VS/VS Code
   - Better than TypeScript debugging experience

#### Challenges & Complexity

**1. Monaco Editor Integration (DIFFICULT)**

**Current:** TypeScript wrapper around Monaco
**Blazor:** Need JSInterop or Blazor Monaco component

**Options:**
- Use existing: `BlazorMonaco` NuGet package (good but incomplete)
- Write custom JSInterop wrapper (2-3 weeks effort)
- **Recommendation:** Use BlazorMonaco + extend as needed

```csharp
// Blazor Monaco example
<MonacoEditor @ref="_editor"
              Id="card-editor"
              ConstructionOptions="EditorConstructionOptions"
              OnDidChangeModelContent="OnEditorChanged"
              CssClass="acd-code-editor" />

@code {
    private MonacoEditor _editor;
    
    private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = "json",
            Value = _cardJson,
            Theme = "vs-dark"
        };
    }
}
```

**2. Drag-and-Drop (MODERATE)**

**Current:** Vanilla JS drag-and-drop
**Blazor:** Blazor's drag-and-drop is newer, less mature

**Options:**
- Native Blazor drag-and-drop (limited browser support)
- JSInterop with dragula.js or similar
- Custom implementation with mouse events
- **Recommendation:** JSInterop + proven library

**3. Card Rendering Surface (MODERATE)**

**Current:** Custom TypeScript implementation
**Blazor:** Need to render AdaptiveCards in Blazor

**Challenge:** No official AdaptiveCards Blazor renderer exists

**Solutions:**
- Use AdaptiveCards JavaScript renderer + IJSRuntime
- Build C# renderer from AdaptiveCards.NET (exists but incomplete)
- Hybrid: JavaScript renderer, C# designer logic
- **Recommendation:** JavaScript renderer via JSInterop (pragmatic)

**4. State Management (EASY)**

**Current:** TypeScript classes managing state
**Blazor:** Can use Blazor's built-in state management

**Advantage:** Blazor is better here - reactive updates, less manual DOM manipulation

**5. Webpack/Build System (MODERATE)**

**Current:** Webpack for TypeScript bundling
**Blazor:** MSBuild + Razor compilation

**Note:** Simpler build in Blazor, but lose some TypeScript ecosystem tooling

---

### Honest Assessment: Should You Port to Blazor?

#### ❌ Arguments AGAINST Porting

1. **Massive Effort**
   - 2000+ lines of complex TypeScript
   - Monaco integration is tricky
   - Drag-and-drop requires custom work
   - 8-12 weeks minimum for feature parity
   - Ongoing maintenance of port vs. contributing to upstream

2. **JavaScript Ecosystem**
   - Monaco Editor is inherently JavaScript
   - AdaptiveCards renderer is JavaScript (no mature C# equivalent)
   - Would need JSInterop anyway - defeats some purposes

3. **Maintenance Burden**
   - Upstream improvements won't automatically flow to your port
   - Must manually port new features from TypeScript
   - Bug fixes in upstream won't help you

4. **TypeScript Works**
   - Current designer is production-ready
   - Well-tested, well-documented
   - Active community support

#### ✅ Arguments FOR Porting (or Hybrid Approach)

1. **Deep Integration**
   - Easier to integrate with PowerSharp.Core
   - Can share authentication with Aspire stack
   - Unified logging/telemetry with your observability

2. **Custom Workflow**
   - Build M365-specific designer workflows
   - Power Platform integration (Power Automate exports, etc.)
   - Enterprise features (collaboration, versioning, approval)

3. **C# Developers**
   - Your team might be more productive in C#
   - Easier to onboard .NET developers than TypeScript specialists

4. **Long-term Vision**
   - If PowerSharp will have many designer surfaces, unified architecture helps
   - Can build component library for other designers

---

### Recommended Hybrid Approach: "Blazor Shell + TypeScript Core"

**Best of Both Worlds:**

```
┌─────────────────────────────────────────────────┐
│ Blazor Application (PowerSharp.Aspire.Web)     │
│  ├── Authentication (Azure AD/Entra)           │
│  ├── Navigation & Chrome                       │
│  ├── Project Management UI                     │
│  ├── Saving/Loading (SQL/Cosmos DB)            │
│  └── Collaboration Features                    │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ Embedded TypeScript Designer               │ │
│  │  (Fork of microsoft/AdaptiveCards)         │ │
│  │  ├── Card editing surface                  │ │
│  │  ├── Monaco editors                        │ │
│  │  ├── Tool palette                          │ │
│  │  └── Property sheets                       │ │
│  └─────────────────▲──────────────────────────┘ │
│                    │                            │
│         JavaScript Interop (IJSRuntime)        │
└────────────────────┼───────────────────────────┘
                     │
            ┌────────▼────────┐
            │  Custom Events  │
            │  • Card Changed │
            │  • Save Request │
            │  • Export       │
            └─────────────────┘
```

**Implementation:**

1. **Blazor Components:**
```csharp
// PowerSharp.Aspire.Web/Components/AdaptiveCardDesigner.razor
@inject IJSRuntime JS

<div id="designer-container" class="adaptive-card-designer"></div>

@code {
    [Parameter]
    public string InitialCardJson { get; set; }
    
    [Parameter]
    public EventCallback<string> OnCardChanged { get; set; }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize TypeScript designer
            await JS.InvokeVoidAsync("PowerSharp.Designer.initialize", 
                "designer-container", 
                InitialCardJson,
                DotNetObjectReference.Create(this));
        }
    }
    
    [JSInvokable]
    public async Task NotifyCardChanged(string cardJson)
    {
        await OnCardChanged.InvokeAsync(cardJson);
    }
}
```

2. **Custom TypeScript Extensions:**
```typescript
// wwwroot/js/designer-extensions.ts
export namespace PowerSharp.Designer {
    let designer: ACDesigner.CardDesigner;
    let dotNetHelper: any;
    
    export function initialize(containerId: string, cardJson: string, helper: any) {
        dotNetHelper = helper;
        designer = new ACDesigner.CardDesigner(defaultMicrosoftHosts);
        designer.attachTo(document.getElementById(containerId));
        designer.setCard(JSON.parse(cardJson));
        
        // Hook card changes
        designer.onCardPayloadChanged = (payload) => {
            dotNetHelper.invokeMethodAsync('NotifyCardChanged', payload);
        };
        
        // Add PowerSharp-specific palette items
        designer.customPaletteItems = [
            createPowerSharpActionSet(),
            createM365ConnectorCard(),
            // ... more custom items
        ];
    }
}
```

**Benefits:**
- ✅ Keep proven TypeScript designer
- ✅ Wrap in Blazor for enterprise features
- ✅ Easy to sync with upstream (just TypeScript changes)
- ✅ Leverage Blazor for authentication, storage, collaboration
- ✅ Faster to market (weeks vs. months)

---

### Recommendation Matrix

| Scenario | Approach | Effort | Risk |
|----------|----------|--------|------|
| **Quick Win** | Fork TypeScript + Host in Aspire | 2-3 weeks | Low |
| **Hybrid** | Blazor Shell + TypeScript Core | 4-6 weeks | Low-Med |
| **Full Port** | Complete Blazor rewrite | 12-16 weeks | High |

---

## Final Recommendations

### 1. Upstream Sync Strategy

**For existing dependencies (PnP, Graph):**
- ✅ **Keep using NuGet packages** - zero maintenance, easy updates
- ✅ Create `CHECK-UPDATES.ps1` script for automated update checks
- ✅ Review and update quarterly (or when security updates released)

**For Adaptive Cards Designer:**
- ✅ **Fork to neurodance/AdaptiveCards**
- ✅ Create `powersharp-customizations` branch
- ✅ Sync main branch with upstream monthly
- ✅ Keep custom features isolated in separate branch

### 2. Adaptive Cards Designer Approach

**Phase 1 (MVP - 4 weeks):**
- Fork microsoft/AdaptiveCards to neurodance
- Build PowerSharp-specific extensions in TypeScript
- Host in Blazor component via Razor/JavaScript interop
- Add basic save/load to SQL/Cosmos DB

**Phase 2 (Enhanced - 6 weeks):**
- Build Blazor wrapper UI for project management
- Add Azure AD authentication
- Implement versioning & collaboration features
- Custom palette for M365/Power Platform scenarios

**Phase 3 (Future - Optional):**
- Evaluate selective Blazor port of components
- Build C# code generation from cards
- PowerShell script export
- Advanced templating UI

**DON'T:** Attempt full port to Blazor initially - too risky, too slow

### 3. Licensing & OSS Strategy

**Open Source (MIT):**
- PowerSharp Platform core
- Custom Adaptive Cards extensions
- Tooling & samples

**Proprietary SaaS:**
- Enterprise collaboration features
- M365/Power Platform integrations
- Advanced workflow automation
- SLA-backed support

**This is LEGAL and COMMON** with MIT-licensed code - many companies do this (GitHub, HashiCorp, etc.)

---

## Next Steps

1. **Create fork:** `neurodance/AdaptiveCards`
2. **Proof of concept:** Host TypeScript designer in Blazor component (1 week)
3. **Evaluate:** Does hybrid approach meet your needs?
4. **Decision point:** Continue hybrid or consider selective port

Would you like me to:
1. Create the `CHECK-UPDATES.ps1` script?
2. Build a POC Blazor component that hosts the TypeScript designer?
3. Create a detailed technical design for the hybrid approach?
