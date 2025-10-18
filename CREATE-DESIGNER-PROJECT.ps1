#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates and configures the PowerSharp.AdaptiveCards Blazor Server project.

.DESCRIPTION
    This script automates the setup of the Adaptive Cards Designer integration:
    1. Creates a new Blazor Server project targeting .NET 9.0
    2. Adds the project to PowerSharp.Platform.sln
    3. Sets up git submodule for neurodance/AdaptiveCards fork
    4. Builds the TypeScript designer from source
    5. Creates all necessary JSInterop, components, and services
    6. Configures project structure and dependencies

.NOTES
    Prerequisites:
    - .NET 9.0 SDK installed
    - Node.js 18+ and npm installed
    - Git configured with GitHub access
    - PowerSharp.Platform.sln exists in current directory
    
    NPM Authentication (if required):
    - For public packages: No authentication needed
    - For private packages: Run 'npm login' before executing this script
    - For CI/CD: Set NPM_TOKEN environment variable with a granular access token
      See: https://docs.npmjs.com/creating-and-viewing-access-tokens
    - Note: npm recently deprecated classic tokens in favor of granular tokens
      See: https://github.blog/changelog/2025-09-29-strengthening-npm-security-important-changes-to-authentication-and-token-management/
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$SolutionPath = "PowerSharp.Platform.sln",
    
    [Parameter()]
    [string]$ProjectName = "PowerSharp.AdaptiveCards",
    
    [Parameter()]
    [string]$ForkUrl = "https://github.com/neurodance/AdaptiveCards.git",
    
    [Parameter()]
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Color output functions
function Write-Step {
    param([string]$Message)
    Write-Host "► $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# Verify prerequisites
Write-Step "Verifying prerequisites..."

# Check .NET SDK
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error ".NET SDK not found. Please install .NET 9.0 SDK."
    exit 1
}
Write-Success ".NET SDK version: $dotnetVersion"

# Check Node.js
$nodeVersion = node --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Node.js not found. Please install Node.js 18+."
    exit 1
}
Write-Success "Node.js version: $nodeVersion"

# Check npm
$npmVersion = npm --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "npm not found. Please install npm."
    exit 1
}
Write-Success "npm version: $npmVersion"

# Check npm authentication status (optional - only needed for private packages)
Write-Step "Checking npm authentication..."
$npmWhoami = npm whoami 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Success "npm authenticated as: $npmWhoami"
} else {
    Write-Warning "npm not authenticated (this is OK for public packages)"
    Write-Host "  If you encounter authentication errors during build, run 'npm login' first" -ForegroundColor Gray
    Write-Host "  Or set NPM_TOKEN environment variable for automation" -ForegroundColor Gray
}

# Check solution file
if (-not (Test-Path $SolutionPath)) {
    Write-Error "Solution file not found: $SolutionPath"
    exit 1
}
Write-Success "Solution file found: $SolutionPath"

# Step 1: Create Blazor Server project
Write-Step "Creating Blazor Server project: $ProjectName"

if (Test-Path $ProjectName) {
    Write-Warning "Project directory already exists: $ProjectName"
    $response = Read-Host "Delete and recreate? (y/n)"
    if ($response -eq 'y') {
        Remove-Item -Path $ProjectName -Recurse -Force
        Write-Success "Removed existing project directory"
    } else {
        Write-Warning "Skipping project creation"
        exit 0
    }
}

dotnet new blazor --name $ProjectName --framework net9.0 --interactivity Server --no-https
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create Blazor project"
    exit 1
}
Write-Success "Created Blazor Server project"

# Step 2: Add project to solution
Write-Step "Adding project to solution..."
dotnet sln $SolutionPath add "$ProjectName\$ProjectName.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to add project to solution"
    exit 1
}
Write-Success "Added project to solution"

# Step 3: Set up git submodule
Write-Step "Setting up git submodule for Adaptive Cards fork..."

$externalDir = "external"
$submodulePath = "$externalDir/AdaptiveCards"

if (-not (Test-Path $externalDir)) {
    New-Item -Path $externalDir -ItemType Directory | Out-Null
    Write-Success "Created external directory"
}

if (Test-Path $submodulePath) {
    Write-Warning "Submodule directory already exists: $submodulePath"
    Write-Warning "Skipping submodule creation"
} else {
    git submodule add $ForkUrl $submodulePath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to add git submodule"
        exit 1
    }
    Write-Success "Added git submodule"
}

# Initialize and update submodule
git submodule update --init --recursive
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update git submodule"
    exit 1
}
Write-Success "Initialized git submodule"

# Step 4: Build designer from source
if (-not $SkipBuild) {
    Write-Step "Building Adaptive Cards Designer from source..."
    
    $designerSourcePath = "$submodulePath/source/nodejs/adaptivecards-designer"
    if (-not (Test-Path $designerSourcePath)) {
        Write-Error "Designer source not found at: $designerSourcePath"
        exit 1
    }
    
    Push-Location $designerSourcePath
    
    try {
        Write-Step "Installing npm dependencies..."
        
        # Check if npm authentication is required
        $npmConfig = npm config get registry 2>$null
        Write-Host "Using npm registry: $npmConfig" -ForegroundColor Gray
        
        # Set npm to non-interactive mode to avoid auth prompts
        $env:NPM_CONFIG_UPDATE_NOTIFIER = "false"
        $env:NPM_CONFIG_FUND = "false"
        
        # Install with --no-audit and --legacy-peer-deps to avoid auth issues with public packages
        npm install --no-audit --legacy-peer-deps
        if ($LASTEXITCODE -ne 0) {
            Write-Error "npm install failed"
            Write-Host "If authentication is required, please run 'npm login' first or set up an npm token." -ForegroundColor Yellow
            Write-Host "See: https://docs.npmjs.com/creating-and-viewing-access-tokens" -ForegroundColor Yellow
            exit 1
        }
        Write-Success "npm dependencies installed"
        
        Write-Step "Building designer..."
        npm run build
        if ($LASTEXITCODE -ne 0) {
            Write-Error "npm build failed"
            exit 1
        }
        Write-Success "Designer built successfully"
        
    } finally {
        Pop-Location
    }
    
    # Copy built assets to Blazor wwwroot
    Write-Step "Copying designer assets to Blazor project..."
    
    $designerDist = "$designerSourcePath/dist"
    $blazorWwwroot = "$ProjectName/wwwroot/designer"
    
    if (-not (Test-Path $designerDist)) {
        Write-Error "Designer dist folder not found: $designerDist"
        exit 1
    }
    
    if (-not (Test-Path $blazorWwwroot)) {
        New-Item -Path $blazorWwwroot -ItemType Directory -Force | Out-Null
    }
    
    Copy-Item -Path "$designerDist/*" -Destination $blazorWwwroot -Recurse -Force
    Write-Success "Designer assets copied to wwwroot/designer"
} else {
    Write-Warning "Skipping designer build (--SkipBuild specified)"
}

# Step 5: Create JSInterop bridge
Write-Step "Creating JSInterop bridge..."

$jsInteropPath = "$ProjectName/wwwroot/js/designer-interop.js"
$jsDir = "$ProjectName/wwwroot/js"

if (-not (Test-Path $jsDir)) {
    New-Item -Path $jsDir -ItemType Directory -Force | Out-Null
}

$jsInteropContent = @'
// PowerSharp Adaptive Cards Designer JSInterop Bridge
window.PowerSharpDesigner = {
    designerInstance: null,
    dotNetRef: null,

    initialize: function(containerId, initialCard, dotNetRef) {
        this.dotNetRef = dotNetRef;
        
        // Define host containers
        const hostContainers = [
            new ACDesigner.HostContainer(
                "WebChat",
                ACDesigner.defaultHostContainers.webChat.targetVersion,
                ACDesigner.defaultHostContainers.webChat.styleSheet
            ),
            new ACDesigner.HostContainer(
                "Outlook",
                ACDesigner.defaultHostContainers.outlook.targetVersion,
                ACDesigner.defaultHostContainers.outlook.styleSheet
            ),
            new ACDesigner.HostContainer(
                "Teams",
                ACDesigner.defaultHostContainers.teams.targetVersion,
                ACDesigner.defaultHostContainers.teams.styleSheet
            )
        ];

        // Create designer instance
        this.designerInstance = new ACDesigner.CardDesigner(hostContainers);
        
        // Set up event handlers
        this.designerInstance.onCardPayloadChanged = (sender) => {
            const cardJson = this.designerInstance.getCardPayloadAsObject();
            this.dotNetRef.invokeMethodAsync('OnCardChanged', JSON.stringify(cardJson));
        };

        this.designerInstance.onCardValidated = (sender, logEntries) => {
            const errors = logEntries
                .filter(entry => entry.level === ACDesigner.CardDesigner.CardDesignerLogLevel.Error)
                .map(entry => entry.message);
            
            if (errors.length > 0) {
                this.dotNetRef.invokeMethodAsync('OnValidationError', errors);
            }
        };

        // Attach to container
        const container = document.getElementById(containerId);
        if (container) {
            this.designerInstance.attachTo(container);
        }

        // Set initial card if provided
        if (initialCard) {
            this.setCard(initialCard);
        }

        return true;
    },

    setCard: function(cardJson) {
        if (this.designerInstance) {
            try {
                const cardObject = typeof cardJson === 'string' ? JSON.parse(cardJson) : cardJson;
                this.designerInstance.setCard(cardObject);
                return true;
            } catch (error) {
                console.error('Failed to set card:', error);
                return false;
            }
        }
        return false;
    },

    getCard: function() {
        if (this.designerInstance) {
            return JSON.stringify(this.designerInstance.getCardPayloadAsObject());
        }
        return null;
    },

    dispose: function() {
        if (this.designerInstance) {
            // Cleanup if needed
            this.designerInstance = null;
        }
        this.dotNetRef = null;
    }
};
'@

Set-Content -Path $jsInteropPath -Value $jsInteropContent -Encoding UTF8
Write-Success "Created JSInterop bridge: $jsInteropPath"

# Step 6: Create Blazor component
Write-Step "Creating Blazor component..."

$componentsDir = "$ProjectName/Components/Designer"
if (-not (Test-Path $componentsDir)) {
    New-Item -Path $componentsDir -ItemType Directory -Force | Out-Null
}

$componentPath = "$componentsDir/AdaptiveCardDesigner.razor"
$componentContent = @'
@using Microsoft.JSInterop
@inject IJSRuntime JS
@implements IAsyncDisposable

<div id="@_containerId" class="adaptive-card-designer-container"></div>

@code {
    private string _containerId = $"designer-{Guid.NewGuid():N}";
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<AdaptiveCardDesigner>? _dotNetRef;

    [Parameter]
    public string? InitialCard { get; set; }

    [Parameter]
    public EventCallback<string> OnCardChanged { get; set; }

    [Parameter]
    public EventCallback<string[]> OnValidationError { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/designer-interop.js");
                _dotNetRef = DotNetObjectReference.Create(this);
                
                await _jsModule.InvokeVoidAsync(
                    "PowerSharpDesigner.initialize",
                    _containerId,
                    InitialCard,
                    _dotNetRef
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize designer: {ex.Message}");
            }
        }
    }

    [JSInvokable]
    public async Task OnCardChangedCallback(string cardJson)
    {
        await OnCardChanged.InvokeAsync(cardJson);
    }

    [JSInvokable]
    public async Task OnValidationErrorCallback(string[] errors)
    {
        await OnValidationError.InvokeAsync(errors);
    }

    public async Task<bool> SetCardAsync(string cardJson)
    {
        if (_jsModule is null) return false;
        
        try
        {
            return await _jsModule.InvokeAsync<bool>("PowerSharpDesigner.setCard", cardJson);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetCardAsync()
    {
        if (_jsModule is null) return null;
        
        try
        {
            return await _jsModule.InvokeAsync<string>("PowerSharpDesigner.getCard");
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("PowerSharpDesigner.dispose");
                await _jsModule.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _dotNetRef?.Dispose();
    }
}
'@

Set-Content -Path $componentPath -Value $componentContent -Encoding UTF8
Write-Success "Created Blazor component: $componentPath"

# Step 7: Create designer page
Write-Step "Creating designer page..."

$pagesDir = "$ProjectName/Components/Pages"
$designerPagePath = "$pagesDir/Designer.razor"

$designerPageContent = @'
@page "/designer"
@using PowerSharp.AdaptiveCards.Components.Designer
@rendermode InteractiveServer

<PageTitle>Adaptive Card Designer - PowerSharp</PageTitle>

<div class="designer-page">
    <div class="designer-header">
        <h1>Adaptive Card Designer</h1>
        <div class="designer-actions">
            <button class="btn btn-primary" @onclick="LoadSampleCard">Load Sample</button>
            <button class="btn btn-secondary" @onclick="ClearCard">Clear</button>
            <button class="btn btn-success" @onclick="SaveCard">Save Card</button>
        </div>
    </div>

    @if (_validationErrors.Any())
    {
        <div class="alert alert-danger">
            <h4>Validation Errors:</h4>
            <ul>
                @foreach (var error in _validationErrors)
                {
                    <li>@error</li>
                }
            </ul>
        </div>
    }

    <div class="designer-container">
        <AdaptiveCardDesigner 
            InitialCard="@_currentCard"
            OnCardChanged="HandleCardChanged"
            OnValidationError="HandleValidationError" />
    </div>

    @if (!string.IsNullOrEmpty(_currentCard))
    {
        <div class="card-json-preview">
            <h3>Current Card JSON:</h3>
            <pre><code>@_currentCard</code></pre>
        </div>
    }
</div>

@code {
    private string? _currentCard;
    private List<string> _validationErrors = new();

    private const string SampleCard = @"{
        ""type"": ""AdaptiveCard"",
        ""version"": ""1.5"",
        ""body"": [
            {
                ""type"": ""TextBlock"",
                ""text"": ""Welcome to PowerSharp Designer"",
                ""size"": ""Large"",
                ""weight"": ""Bolder""
            },
            {
                ""type"": ""TextBlock"",
                ""text"": ""Create your adaptive cards here"",
                ""wrap"": true
            }
        ]
    }";

    private void LoadSampleCard()
    {
        _currentCard = SampleCard;
        _validationErrors.Clear();
    }

    private void ClearCard()
    {
        _currentCard = null;
        _validationErrors.Clear();
    }

    private async Task SaveCard()
    {
        if (!string.IsNullOrEmpty(_currentCard))
        {
            // TODO: Implement save functionality (e.g., to file system, database, etc.)
            Console.WriteLine($"Saving card: {_currentCard}");
        }
    }

    private void HandleCardChanged(string cardJson)
    {
        _currentCard = cardJson;
        _validationErrors.Clear();
        StateHasChanged();
    }

    private void HandleValidationError(string[] errors)
    {
        _validationErrors = errors.ToList();
        StateHasChanged();
    }
}
'@

Set-Content -Path $designerPagePath -Value $designerPageContent -Encoding UTF8
Write-Success "Created designer page: $designerPagePath"

# Step 8: Create CSS styles
Write-Step "Creating CSS styles..."

$cssPath = "$ProjectName/wwwroot/css/designer.css"
$cssContent = @'
/* PowerSharp Adaptive Cards Designer Styles */

.designer-page {
    padding: 20px;
    height: 100vh;
    display: flex;
    flex-direction: column;
}

.designer-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    padding-bottom: 15px;
    border-bottom: 2px solid #dee2e6;
}

.designer-header h1 {
    margin: 0;
    font-size: 2rem;
    color: #333;
}

.designer-actions {
    display: flex;
    gap: 10px;
}

.designer-actions button {
    padding: 8px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    transition: all 0.2s;
}

.btn-primary {
    background-color: #0d6efd;
    color: white;
}

.btn-primary:hover {
    background-color: #0b5ed7;
}

.btn-secondary {
    background-color: #6c757d;
    color: white;
}

.btn-secondary:hover {
    background-color: #5c636a;
}

.btn-success {
    background-color: #198754;
    color: white;
}

.btn-success:hover {
    background-color: #157347;
}

.designer-container {
    flex: 1;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    overflow: hidden;
    min-height: 500px;
}

.adaptive-card-designer-container {
    width: 100%;
    height: 100%;
}

.card-json-preview {
    margin-top: 20px;
    padding: 15px;
    background-color: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 4px;
}

.card-json-preview h3 {
    margin-top: 0;
    font-size: 1.2rem;
    color: #495057;
}

.card-json-preview pre {
    background-color: #ffffff;
    padding: 15px;
    border-radius: 4px;
    overflow-x: auto;
    max-height: 300px;
}

.card-json-preview code {
    font-family: 'Courier New', monospace;
    font-size: 13px;
    color: #212529;
}

.alert {
    padding: 15px;
    margin-bottom: 20px;
    border: 1px solid transparent;
    border-radius: 4px;
}

.alert-danger {
    background-color: #f8d7da;
    border-color: #f5c2c7;
    color: #842029;
}

.alert h4 {
    margin-top: 0;
    font-size: 1.1rem;
}

.alert ul {
    margin-bottom: 0;
    padding-left: 20px;
}
'@

Set-Content -Path $cssPath -Value $cssContent -Encoding UTF8
Write-Success "Created CSS styles: $cssPath"

# Step 9: Update App.razor to include CSS and scripts
Write-Step "Updating App.razor..."

$appRazorPath = "$ProjectName/Components/App.razor"
if (Test-Path $appRazorPath) {
    $appRazorContent = Get-Content $appRazorPath -Raw
    
    # Add designer CSS link if not present
    if ($appRazorContent -notmatch 'designer\.css') {
        $appRazorContent = $appRazorContent -replace '(<link rel="stylesheet" href="app\.css"[^>]*>)', "`$1`n    <link rel=""stylesheet"" href=""css/designer.css"" />"
    }
    
    # Add designer script references if not present
    if ($appRazorContent -notmatch 'adaptivecards-designer') {
        $scriptTag = @'

    <!-- Adaptive Cards Designer Dependencies -->
    <script src="designer/adaptivecards.min.js"></script>
    <script src="designer/adaptivecards-designer.min.js"></script>
    <link rel="stylesheet" href="designer/adaptivecards-designer.min.css" />
'@
        $appRazorContent = $appRazorContent -replace '(</head>)', "$scriptTag`n`$1"
    }
    
    Set-Content -Path $appRazorPath -Value $appRazorContent -Encoding UTF8
    Write-Success "Updated App.razor with designer references"
} else {
    Write-Warning "App.razor not found at: $appRazorPath"
}

# Step 10: Add navigation link
Write-Step "Adding navigation link..."

$navMenuPath = "$ProjectName/Components/Layout/NavMenu.razor"
if (Test-Path $navMenuPath) {
    $navMenuContent = Get-Content $navMenuPath -Raw
    
    if ($navMenuContent -notmatch '/designer') {
        $navLinkTag = @'

        <div class="nav-item px-3">
            <NavLink class="nav-link" href="designer">
                <span class="bi bi-pencil-square-nav-menu" aria-hidden="true"></span> Designer
            </NavLink>
        </div>
'@
        $navMenuContent = $navMenuContent -replace '(</nav>)', "$navLinkTag`n`$1"
        Set-Content -Path $navMenuPath -Value $navMenuContent -Encoding UTF8
        Write-Success "Added designer navigation link"
    } else {
        Write-Success "Designer navigation link already exists"
    }
} else {
    Write-Warning "NavMenu.razor not found at: $navMenuPath"
}

# Step 11: Create BUILD-DESIGNER.ps1 helper script
Write-Step "Creating BUILD-DESIGNER.ps1 helper script..."

$buildScriptPath = "BUILD-DESIGNER.ps1"
$buildScriptContent = @'
#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Rebuilds the Adaptive Cards Designer and copies assets to Blazor project.

.DESCRIPTION
    This script rebuilds the TypeScript designer from the git submodule
    and copies the built assets to the PowerSharp.AdaptiveCards wwwroot folder.
    Use this when you pull updates from the upstream AdaptiveCards repository.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$SubmodulePath = "external/AdaptiveCards",
    
    [Parameter()]
    [string]$ProjectPath = "PowerSharp.AdaptiveCards"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "► Updating git submodule..." -ForegroundColor Cyan
git submodule update --remote $SubmodulePath

Write-Host "► Building designer from source..." -ForegroundColor Cyan
$designerSourcePath = "$SubmodulePath/source/nodejs/adaptivecards-designer"

Push-Location $designerSourcePath

try {
    Write-Host "  Installing dependencies..." -ForegroundColor Gray
    npm install
    
    Write-Host "  Building..." -ForegroundColor Gray
    npm run build
    
    Write-Host "✓ Build complete" -ForegroundColor Green
} finally {
    Pop-Location
}

Write-Host "► Copying assets to Blazor project..." -ForegroundColor Cyan
$designerDist = "$designerSourcePath/dist"
$blazorWwwroot = "$ProjectPath/wwwroot/designer"

if (-not (Test-Path $blazorWwwroot)) {
    New-Item -Path $blazorWwwroot -ItemType Directory -Force | Out-Null
}

Copy-Item -Path "$designerDist/*" -Destination $blazorWwwroot -Recurse -Force

Write-Host "✓ Designer assets updated successfully" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run: dotnet build $ProjectPath" -ForegroundColor Gray
Write-Host "  2. Run: dotnet run --project $ProjectPath" -ForegroundColor Gray
Write-Host "  3. Navigate to: http://localhost:5000/designer" -ForegroundColor Gray
'@

Set-Content -Path $buildScriptPath -Value $buildScriptContent -Encoding UTF8
Write-Success "Created BUILD-DESIGNER.ps1 helper script"

# Final summary
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "✓ PowerSharp.AdaptiveCards project created successfully!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Project Structure:" -ForegroundColor Cyan
Write-Host "  $ProjectName/" -ForegroundColor White
Write-Host "    ├── Components/Designer/AdaptiveCardDesigner.razor" -ForegroundColor Gray
Write-Host "    ├── Components/Pages/Designer.razor" -ForegroundColor Gray
Write-Host "    ├── wwwroot/js/designer-interop.js" -ForegroundColor Gray
Write-Host "    ├── wwwroot/css/designer.css" -ForegroundColor Gray
Write-Host "    └── wwwroot/designer/ (built assets)" -ForegroundColor Gray
Write-Host ""
Write-Host "  external/AdaptiveCards/ (git submodule)" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Build the project:" -ForegroundColor White
Write-Host "     dotnet build $ProjectName" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Run the project:" -ForegroundColor White
Write-Host "     dotnet run --project $ProjectName" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Navigate to the designer:" -ForegroundColor White
Write-Host "     http://localhost:5000/designer" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. To update designer from upstream:" -ForegroundColor White
Write-Host "     .\BUILD-DESIGNER.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "Documentation: See ADAPTIVE-CARDS-POC.md for implementation details" -ForegroundColor Cyan
Write-Host ""
