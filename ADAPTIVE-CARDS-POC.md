# Adaptive Cards Designer - Blazor Integration POC

> **Date:** October 17, 2025  
> **Phase:** Proof of Concept  
> **Fork:** https://github.com/neurodance/AdaptiveCards

---

## Architecture Decision: Blazor Server App

**Recommendation: Create `PowerSharp.AdaptiveCards` Blazor Server project in PowerSharp.sln**

### Why Blazor Server (not WebAssembly)?

1. **Better for Enterprise**: Server-side rendering, easier Azure AD/Entra ID integration
2. **Smaller payload**: TypeScript designer stays on server, only SignalR for updates
3. **Aspire Integration**: Natural fit with PowerSharp.Aspire orchestration
4. **Security**: Credentials and card storage on server, not client
5. **Performance**: No WASM download overhead

---

## Project Structure

```
PowerSharp/
├── PowerSharp.AdaptiveCards/              # NEW Blazor Server project
│   ├── PowerSharp.AdaptiveCards.csproj
│   ├── Program.cs
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   └── NavMenu.razor
│   │   ├── Pages/
│   │   │   ├── Home.razor
│   │   │   ├── Designer.razor             # Main designer page
│   │   │   └── CardLibrary.razor          # Card management
│   │   └── Shared/
│   │       ├── AdaptiveCardDesigner.razor # Designer component
│   │       └── CardPreview.razor          # Preview component
│   ├── Services/
│   │   ├── CardStorageService.cs          # Save/load cards
│   │   └── DesignerStateService.cs        # State management
│   ├── wwwroot/
│   │   ├── designer/                      # Built TypeScript designer assets
│   │   │   ├── adaptivecards-designer.min.js
│   │   │   ├── adaptivecards-designer.css
│   │   │   └── monaco-editor/ (monaco assets)
│   │   └── js/
│   │       └── designer-interop.js        # JSInterop bridge
│   └── appsettings.json
│
├── PowerSharp.Aspire.AppHost/
│   └── AppHost.cs                         # Add reference to AdaptiveCards
│
└── external/                              # NEW external dependencies
    └── AdaptiveCards/                     # Git submodule pointing to neurodance/AdaptiveCards
        └── source/nodejs/adaptivecards-designer/
```

---

## Implementation Plan

### Phase 1: Project Setup (Day 1)

**Step 1: Create Blazor Server Project**
```powershell
dotnet new blazor --name PowerSharp.AdaptiveCards --framework net9.0 --no-https false --interactivity Server
cd PowerSharp.AdaptiveCards
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Server
```

**Step 2: Add to Solution**
```powershell
dotnet sln PowerSharp.Platform.sln add PowerSharp.AdaptiveCards/PowerSharp.AdaptiveCards.csproj
```

**Step 3: Add Git Submodule for Your Fork**
```powershell
git submodule add https://github.com/neurodance/AdaptiveCards.git external/AdaptiveCards
git submodule update --init --recursive
```

**Step 4: Build Designer from Fork**
```powershell
cd external/AdaptiveCards/source/nodejs/adaptivecards-designer
npm install
npm run build

# Copy dist files to Blazor wwwroot
Copy-Item -Path "dist/*" -Destination "../../../../../PowerSharp.AdaptiveCards/wwwroot/designer/" -Recurse
```

---

### Phase 2: Basic JSInterop Bridge (Day 1-2)

**File: `wwwroot/js/designer-interop.js`**

```javascript
// PowerSharp Adaptive Cards Designer - JavaScript Interop
window.PowerSharpDesigner = {
    designer: null,
    dotNetHelper: null,
    
    // Initialize designer
    initialize: function(containerId, initialCard, dotNetRef) {
        console.log('PowerSharp Designer initializing...');
        
        this.dotNetHelper = dotNetRef;
        
        // Create designer with default Microsoft hosts
        this.designer = new ACDesigner.CardDesigner(ACDesigner.defaultMicrosoftHosts);
        
        // Set asset path for CDN resources
        this.designer.assetPath = '/designer/';
        
        // Attach to container
        const container = document.getElementById(containerId);
        this.designer.attachTo(container);
        
        // Set initial card if provided
        if (initialCard) {
            try {
                const cardJson = JSON.parse(initialCard);
                this.designer.setCard(cardJson);
            } catch (e) {
                console.error('Failed to parse initial card:', e);
            }
        }
        
        // Hook card payload changes
        this.designer.onCardPayloadChanged = (designer) => {
            const payload = JSON.stringify(designer.getCard(), null, 2);
            this.dotNetHelper.invokeMethodAsync('OnCardChanged', payload);
        };
        
        // Hook validation events
        this.designer.onCardValidated = (designer, validationEvents) => {
            const errors = validationEvents.map(e => ({
                message: e.message,
                phase: e.phase,
                source: e.source ? e.source.constructor.name : 'Unknown'
            }));
            this.dotNetHelper.invokeMethodAsync('OnCardValidated', errors);
        };
        
        console.log('PowerSharp Designer initialized successfully');
        return true;
    },
    
    // Get current card JSON
    getCard: function() {
        if (this.designer) {
            return JSON.stringify(this.designer.getCard(), null, 2);
        }
        return null;
    },
    
    // Set card JSON
    setCard: function(cardJson) {
        if (this.designer) {
            try {
                const card = JSON.parse(cardJson);
                this.designer.setCard(card);
                return true;
            } catch (e) {
                console.error('Failed to set card:', e);
                return false;
            }
        }
        return false;
    },
    
    // Set sample data
    setSampleData: function(dataJson) {
        if (this.designer) {
            try {
                const data = JSON.parse(dataJson);
                this.designer.sampleData = data;
                return true;
            } catch (e) {
                console.error('Failed to set sample data:', e);
                return false;
            }
        }
        return false;
    },
    
    // Cleanup
    dispose: function() {
        if (this.designer) {
            // Designer doesn't have explicit dispose, but we can clean references
            this.designer = null;
            this.dotNetHelper = null;
        }
    }
};
```

---

### Phase 3: Blazor Component (Day 2)

**File: `Components/Shared/AdaptiveCardDesigner.razor`**

```razor
@using Microsoft.JSInterop
@inject IJSRuntime JS
@implements IAsyncDisposable

<div class="adaptive-card-designer-wrapper">
    <div id="@_containerId" class="adaptive-card-designer-container"></div>
</div>

@code {
    private string _containerId = $"designer-{Guid.NewGuid():N}";
    private DotNetObjectReference<AdaptiveCardDesigner>? _objRef;
    private bool _initialized = false;

    [Parameter]
    public string InitialCardJson { get; set; } = DefaultCard;

    [Parameter]
    public EventCallback<string> OnCardChanged { get; set; }

    [Parameter]
    public EventCallback<ValidationError[]> OnCardValidated { get; set; }

    private const string DefaultCard = @"{
        ""type"": ""AdaptiveCard"",
        ""version"": ""1.5"",
        ""body"": [
            {
                ""type"": ""TextBlock"",
                ""text"": ""Welcome to PowerSharp Adaptive Cards Designer"",
                ""size"": ""Large"",
                ""weight"": ""Bolder""
            }
        ]
    }";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeDesigner();
        }
    }

    private async Task InitializeDesigner()
    {
        try
        {
            _objRef = DotNetObjectReference.Create(this);

            var success = await JS.InvokeAsync<bool>(
                "PowerSharpDesigner.initialize",
                _containerId,
                InitialCardJson,
                _objRef);

            _initialized = success;

            if (!success)
            {
                Console.WriteLine("Failed to initialize designer");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing designer: {ex.Message}");
        }
    }

    [JSInvokable]
    public async Task OnCardChanged(string cardJson)
    {
        if (OnCardChanged.HasDelegate)
        {
            await OnCardChanged.InvokeAsync(cardJson);
        }
    }

    [JSInvokable]
    public async Task OnCardValidated(ValidationError[] errors)
    {
        if (OnCardValidated.HasDelegate)
        {
            await OnCardValidated.InvokeAsync(errors);
        }
    }

    public async Task<string?> GetCardAsync()
    {
        if (!_initialized) return null;

        try
        {
            return await JS.InvokeAsync<string>("PowerSharpDesigner.getCard");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SetCardAsync(string cardJson)
    {
        if (!_initialized) return false;

        try
        {
            return await JS.InvokeAsync<bool>("PowerSharpDesigner.setCard", cardJson);
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_initialized)
        {
            try
            {
                await JS.InvokeVoidAsync("PowerSharpDesigner.dispose");
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _objRef?.Dispose();
    }

    public class ValidationError
    {
        public string Message { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
```

**File: `Components/Shared/AdaptiveCardDesigner.razor.css`**

```css
.adaptive-card-designer-wrapper {
    width: 100%;
    height: 100%;
    min-height: 600px;
}

.adaptive-card-designer-container {
    width: 100%;
    height: 100%;
}

/* Override designer styles if needed */
::deep .acd-designer-host {
    height: 100%;
}
```

---

### Phase 4: Designer Page (Day 2)

**File: `Components/Pages/Designer.razor`**

```razor
@page "/designer"
@page "/designer/{cardId}"
@using PowerSharp.AdaptiveCards.Services
@inject CardStorageService CardStorage
@inject NavigationManager Navigation

<PageTitle>Adaptive Card Designer - PowerSharp</PageTitle>

<div class="designer-page">
    <div class="designer-header">
        <h1>Adaptive Card Designer</h1>
        <div class="designer-actions">
            <button class="btn btn-primary" @onclick="SaveCard" disabled="@_saving">
                @if (_saving)
                {
                    <span class="spinner-border spinner-border-sm me-1"></span>
                }
                Save
            </button>
            <button class="btn btn-secondary" @onclick="NewCard">
                New Card
            </button>
            <button class="btn btn-secondary" @onclick="ExportCard">
                Export JSON
            </button>
        </div>
    </div>

    @if (_loading)
    {
        <div class="loading-container">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading designer...</span>
            </div>
        </div>
    }
    else
    {
        <AdaptiveCardDesigner @ref="_designer"
                              InitialCardJson="@_cardJson"
                              OnCardChanged="HandleCardChanged"
                              OnCardValidated="HandleCardValidated" />
    }

    @if (_validationErrors.Any())
    {
        <div class="validation-errors mt-3">
            <h5>Validation Errors</h5>
            <ul>
                @foreach (var error in _validationErrors)
                {
                    <li><strong>@error.Phase:</strong> @error.Message (@error.Source)</li>
                }
            </ul>
        </div>
    }
</div>

@code {
    [Parameter]
    public string? CardId { get; set; }

    private AdaptiveCardDesigner? _designer;
    private string _cardJson = string.Empty;
    private bool _loading = true;
    private bool _saving = false;
    private bool _hasUnsavedChanges = false;
    private List<AdaptiveCardDesigner.ValidationError> _validationErrors = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadCard();
        _loading = false;
    }

    private async Task LoadCard()
    {
        if (!string.IsNullOrEmpty(CardId))
        {
            var card = await CardStorage.GetCardAsync(CardId);
            _cardJson = card?.Json ?? string.Empty;
        }
    }

    private Task HandleCardChanged(string cardJson)
    {
        _cardJson = cardJson;
        _hasUnsavedChanges = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleCardValidated(AdaptiveCardDesigner.ValidationError[] errors)
    {
        _validationErrors = errors.ToList();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task SaveCard()
    {
        if (_designer == null) return;

        _saving = true;
        StateHasChanged();

        try
        {
            var json = await _designer.GetCardAsync();
            if (json != null)
            {
                var savedCardId = await CardStorage.SaveCardAsync(CardId, json);
                _hasUnsavedChanges = false;

                if (string.IsNullOrEmpty(CardId))
                {
                    // Redirect to URL with card ID
                    Navigation.NavigateTo($"/designer/{savedCardId}");
                }
            }
        }
        finally
        {
            _saving = false;
            StateHasChanged();
        }
    }

    private void NewCard()
    {
        Navigation.NavigateTo("/designer", forceLoad: true);
    }

    private async Task ExportCard()
    {
        if (_designer == null) return;

        var json = await _designer.GetCardAsync();
        if (json != null)
        {
            // Trigger download
            // (Implementation depends on your preferred approach)
        }
    }
}
```

---

### Phase 5: Aspire Integration (Day 3)

**Update: `PowerSharp.Aspire.AppHost/AppHost.cs`**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PowerSharp_Aspire_ApiService>("apiservice");

var webfrontend = builder.AddProject<Projects.PowerSharp_Aspire_Web>("webfrontend")
    .WithReference(apiService)
    .WithExternalHttpEndpoints();

// Add Adaptive Cards Designer
var adaptiveCardsDesigner = builder.AddProject<Projects.PowerSharp_AdaptiveCards>("adaptivecards-designer")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

---

## Build Automation Script

**File: `BUILD-DESIGNER.ps1`**

```powershell
#!/usr/bin/env pwsh
# Build Adaptive Cards Designer from fork

$ErrorActionPreference = "Stop"

Write-Host "=== Building Adaptive Cards Designer ===" -ForegroundColor Cyan

# Paths
$repoRoot = $PSScriptRoot
$designerSource = Join-Path $repoRoot "external" "AdaptiveCards" "source" "nodejs" "adaptivecards-designer"
$designerDist = Join-Path $designerSource "dist"
$blazorWwwroot = Join-Path $repoRoot "PowerSharp.AdaptiveCards" "wwwroot" "designer"

# Check if submodule exists
if (!(Test-Path $designerSource)) {
    Write-Host "Initializing AdaptiveCards submodule..." -ForegroundColor Yellow
    git submodule update --init --recursive
}

# Navigate to designer
Push-Location $designerSource

try {
    # Install dependencies
    Write-Host "Installing npm dependencies..." -ForegroundColor Cyan
    npm install

    # Build designer
    Write-Host "Building designer..." -ForegroundColor Cyan
    npm run build

    # Copy to Blazor wwwroot
    Write-Host "Copying designer assets to Blazor project..." -ForegroundColor Cyan
    if (!(Test-Path $blazorWwwroot)) {
        New-Item -ItemType Directory -Path $blazorWwwroot -Force | Out-Null
    }

    Copy-Item -Path "$designerDist/*" -Destination $blazorWwwroot -Recurse -Force

    Write-Host "✓ Designer built successfully!" -ForegroundColor Green
}
finally {
    Pop-Location
}
```

---

## Next Steps

1. **Create the Blazor project** in PowerSharp.sln
2. **Add git submodule** pointing to your fork
3. **Build designer** from fork with BUILD-DESIGNER.ps1
4. **Implement JSInterop bridge** (designer-interop.js)
5. **Create Blazor component** (AdaptiveCardDesigner.razor)
6. **Test POC** - verify designer loads and works

**Estimated Time: 2-3 days for working POC**

Would you like me to start creating these files?
