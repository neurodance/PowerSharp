# Git Cleanup Script for PowerSharp Platform
# This script removes build artifacts from git tracking and commits the integration

Write-Host "PowerSharp Platform - Git Cleanup & Integration" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan

# Step 1: Untrack build artifacts
Write-Host "`n[1/5] Removing build artifacts from git tracking..." -ForegroundColor Yellow

$foldersToUntrack = @(
    "PowerSharp.Aspire/PowerSharp.Aspire.ApiService/bin"
    "PowerSharp.Aspire/PowerSharp.Aspire.ApiService/obj"
    "PowerSharp.Aspire/PowerSharp.Aspire.AppHost/bin"
    "PowerSharp.Aspire/PowerSharp.Aspire.AppHost/obj"
    "PowerSharp.Aspire/PowerSharp.Aspire.ServiceDefaults/bin"
    "PowerSharp.Aspire/PowerSharp.Aspire.ServiceDefaults/obj"
    "PowerSharp.Aspire/PowerSharp.Aspire.Tests/bin"
    "PowerSharp.Aspire/PowerSharp.Aspire.Tests/obj"
    "PowerSharp.Aspire/PowerSharp.Aspire.Web/bin"
    "PowerSharp.Aspire/PowerSharp.Aspire.Web/obj"
    "PowerSharp.Core/bin"
    "PowerSharp.Core/obj"
    "PowerSharp.Platform/bin"
    "PowerSharp.Platform/obj"
)

foreach ($folder in $foldersToUntrack) {
    if (Test-Path $folder) {
        Write-Host "  Untracking: $folder" -ForegroundColor Gray
        git rm -r --cached $folder 2>&1 | Out-Null
    }
}

# Step 2: Stage documentation updates
Write-Host "`n[2/5] Staging documentation updates..." -ForegroundColor Yellow
git add .gitignore
git add README.md
git add INTEGRATION_STATUS.md
git add .copilot-context.md
git add QUICKREF.md
git add Cleanup-Git.ps1

# Step 3: Stage PnPSharp integration
Write-Host "`n[3/5] Staging PnPSharp integration..." -ForegroundColor Yellow
git add PnPSharp/

# Step 4: Commit changes
Write-Host "`n[4/5] Creating commits..." -ForegroundColor Yellow

# Commit the untracking of build artifacts
Write-Host "  Committing: Remove build artifacts from git tracking..." -ForegroundColor Gray
git commit -m "Remove build artifacts (bin/obj) from git tracking

Updated .gitignore to exclude build artifacts going forward.
These files should not be tracked in version control."

# Commit the PnPSharp integration
Write-Host "  Committing: Integrate PnPSharp into PowerSharp Platform..." -ForegroundColor Gray
git commit -m "Integrate PnPSharp projects into PowerSharp Platform

- Added PnPSharp library, samples, and tests to PowerSharp.Platform.sln
- Organized with solution folders for clarity
- Maintained separate PnPSharp/ folder structure
- All 13 projects building in unified solution"

# Commit the documentation
Write-Host "  Committing: Update documentation..." -ForegroundColor Gray
git commit -m "Update documentation for unified PowerSharp Platform

- Created comprehensive platform-level README.md
- Added INTEGRATION_STATUS.md tracking integration progress
- Documented all projects, build processes, and known issues
- Preserved PnPSharp-specific documentation"

# Step 5: Summary
Write-Host "`n[5/5] Summary" -ForegroundColor Yellow
Write-Host "=" * 60 -ForegroundColor Cyan

$status = git status --short
if ($status) {
    Write-Host "`nRemaining uncommitted changes:" -ForegroundColor Yellow
    git status --short
} else {
    Write-Host "`n✅ All changes committed successfully!" -ForegroundColor Green
}

Write-Host "`nGit log (last 5 commits):" -ForegroundColor Yellow
git log --oneline -5

Write-Host "`n" -ForegroundColor Cyan
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review the commits: git log" -ForegroundColor Gray
Write-Host "  2. Push to remote: git push origin main" -ForegroundColor Gray
Write-Host "  3. Verify build: dotnet build PowerSharp.Platform.sln" -ForegroundColor Gray
Write-Host ""
