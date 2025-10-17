#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run tests with code coverage for PowerSharp Platform
.DESCRIPTION
    This script runs all tests with code coverage collection and optionally generates HTML reports.
    Results are stored in TestResults/ directory.
.PARAMETER Project
    Specific test project to run. If not specified, runs all tests.
.PARAMETER GenerateReport
    Generate HTML coverage report using ReportGenerator
.PARAMETER Threshold
    Minimum coverage threshold (default: 60)
.PARAMETER ThresholdType
    Type of threshold: line, branch, or method (default: line)
.PARAMETER Open
    Open HTML report in browser after generation
.EXAMPLE
    .\RUN-COVERAGE.ps1
    Run all tests with coverage
.EXAMPLE
    .\RUN-COVERAGE.ps1 -GenerateReport -Open
    Run tests, generate HTML report, and open in browser
.EXAMPLE
    .\RUN-COVERAGE.ps1 -Project "PowerSharp.Aspire.Tests"
    Run coverage for specific test project
.EXAMPLE
    .\RUN-COVERAGE.ps1 -Threshold 80 -ThresholdType line
    Run with 80% line coverage threshold
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$Project = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport,
    
    [Parameter(Mandatory=$false)]
    [int]$Threshold = 60,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("line", "branch", "method")]
    [string]$ThresholdType = "line",
    
    [Parameter(Mandatory=$false)]
    [switch]$Open
)

$ErrorActionPreference = "Stop"

# Colors for output
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"
$ColorInfo = "Cyan"

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor $ColorInfo
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $ColorSuccess
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor $ColorWarning
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $ColorError
}

# Script start
Write-Host @"

╔═══════════════════════════════════════════════════════════════╗
║           PowerSharp Platform - Code Coverage Runner         ║
╚═══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor $ColorInfo

# Ensure we're in the correct directory
$scriptDir = $PSScriptRoot
Set-Location $scriptDir
Write-Success "Working directory: $scriptDir"

# Clean previous test results
Write-Step "Cleaning previous test results..."
if (Test-Path "TestResults") {
    Remove-Item -Path "TestResults" -Recurse -Force
    Write-Success "Cleaned TestResults directory"
}

# Build test target
$testTarget = if ($Project) {
    $projectPath = Get-ChildItem -Recurse -Filter "$Project.csproj" | Select-Object -First 1
    if (-not $projectPath) {
        Write-Error "Project '$Project' not found"
        exit 1
    }
    Write-Success "Found project: $($projectPath.FullName)"
    $projectPath.FullName
} else {
    "PowerSharp.Platform.sln"
}

# Build test command
Write-Step "Running tests with coverage..."
Write-Host "  Target: $testTarget" -ForegroundColor Gray
Write-Host "  Threshold: $Threshold% ($ThresholdType)" -ForegroundColor Gray
Write-Host "  Settings: coverlet.runsettings" -ForegroundColor Gray

$testArgs = @(
    "test",
    $testTarget,
    "--configuration", "Debug",
    "--collect:`"XPlat Code Coverage`"",
    "--settings", "coverlet.runsettings",
    "--results-directory", "./TestResults",
    "--verbosity", "normal"
)

# Add threshold parameters
if ($Threshold -gt 0) {
    $testArgs += "/p:Threshold=$Threshold"
    $testArgs += "/p:ThresholdType=$ThresholdType"
    $testArgs += "/p:ThresholdStat=total"
}

# Run tests
Write-Host ""
$testProcess = Start-Process -FilePath "dotnet" -ArgumentList $testArgs -NoNewWindow -Wait -PassThru

if ($testProcess.ExitCode -eq 0) {
    Write-Success "Tests completed successfully"
} else {
    Write-Error "Tests failed or coverage threshold not met (Exit Code: $($testProcess.ExitCode))"
    if ($testProcess.ExitCode -eq 1) {
        Write-Warning "This could be due to test failures or coverage below threshold"
    }
    # Don't exit - still try to generate report
}

# Find coverage files
Write-Step "Locating coverage files..."
$coverageFiles = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse
if ($coverageFiles.Count -eq 0) {
    Write-Error "No coverage files found in TestResults/"
    exit 1
}
Write-Success "Found $($coverageFiles.Count) coverage file(s)"

# Generate HTML report if requested
if ($GenerateReport) {
    Write-Step "Generating HTML coverage report..."
    
    # Check if ReportGenerator is installed
    $reportGenInstalled = Get-Command reportgenerator -ErrorAction SilentlyContinue
    if (-not $reportGenInstalled) {
        Write-Warning "ReportGenerator not found. Installing..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install ReportGenerator"
            exit 1
        }
        Write-Success "ReportGenerator installed"
    }
    
    # Generate report
    $reportDir = Join-Path $scriptDir "TestResults" "CoverageReport"
    $reportArgs = @(
        "-reports:TestResults/**/coverage.cobertura.xml",
        "-targetdir:$reportDir",
        "-reporttypes:Html;Badges;JsonSummary",
        "-assemblyfilters:-*Tests*",
        "-title:PowerSharp Platform Code Coverage"
    )
    
    Write-Host "  Output: $reportDir" -ForegroundColor Gray
    reportgenerator @reportArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "HTML report generated successfully"
        
        # Display summary
        $summaryFile = Join-Path $reportDir "Summary.json"
        if (Test-Path $summaryFile) {
            $summary = Get-Content $summaryFile | ConvertFrom-Json
            Write-Host "`nCoverage Summary:" -ForegroundColor $ColorInfo
            Write-Host "  Line Coverage:   $($summary.summary.linecoverage)%" -ForegroundColor $(if ($summary.summary.linecoverage -ge $Threshold) { $ColorSuccess } else { $ColorWarning })
            Write-Host "  Branch Coverage: $($summary.summary.branchcoverage)%" -ForegroundColor Gray
            Write-Host "  Method Coverage: $($summary.summary.methodcoverage)%" -ForegroundColor Gray
        }
        
        # Open in browser if requested
        if ($Open) {
            $indexPath = Join-Path $reportDir "index.html"
            if (Test-Path $indexPath) {
                Write-Step "Opening coverage report in browser..."
                Start-Process $indexPath
                Write-Success "Report opened"
            }
        } else {
            Write-Host "`nTo view the report, open:" -ForegroundColor $ColorInfo
            Write-Host "  $(Join-Path $reportDir 'index.html')" -ForegroundColor Gray
        }
    } else {
        Write-Error "Failed to generate HTML report"
        exit 1
    }
}

# Summary
Write-Host @"

╔═══════════════════════════════════════════════════════════════╗
║                      Coverage Run Complete                    ║
╚═══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor $ColorSuccess

Write-Host "Coverage files location:" -ForegroundColor $ColorInfo
Write-Host "  TestResults/**/coverage.cobertura.xml" -ForegroundColor Gray

if ($GenerateReport) {
    Write-Host "`nHTML Report location:" -ForegroundColor $ColorInfo
    Write-Host "  TestResults/CoverageReport/index.html" -ForegroundColor Gray
}

Write-Host "`nNext steps:" -ForegroundColor $ColorInfo
Write-Host "  • Review coverage in VS Code with Coverage Gutters extension" -ForegroundColor Gray
Write-Host "  • Run with -GenerateReport -Open to view detailed HTML report" -ForegroundColor Gray
Write-Host "  • Use -Threshold to enforce minimum coverage levels" -ForegroundColor Gray

Write-Host ""
exit 0
