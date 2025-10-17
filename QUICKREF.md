# 🚀 PowerSharp - Quick Reference Card

## Instant Status Check
```powershell
# See what I should do next
cat .copilot-context.md | Select-String "Immediate Next Steps" -Context 0,15

# Current git state
git status --short

# Last 5 commits
git log --oneline -5
```

## Common Commands

### Build
```powershell
dotnet build PowerSharp.Platform.sln -c Debug
```

### Test
```powershell
dotnet test PowerSharp.Platform.sln -c Debug
```

### Clean Build
```powershell
dotnet clean PowerSharp.Platform.sln
dotnet build PowerSharp.Platform.sln -c Debug
```

### Git Cleanup (First Time Setup)
```powershell
.\Cleanup-Git.ps1
```

## File Locations

| File | Purpose |
|------|---------|
| `.copilot-context.md` | **START HERE** - Session context & status |
| `README.md` | Platform documentation |
| `INTEGRATION_STATUS.md` | Integration tracking |
| `PowerSharp.Platform.sln` | Main solution |
| `Cleanup-Git.ps1` | Git cleanup script |

## Project Structure

```
PowerSharp/
├── PowerSharp.Core/          # Middleware (2 projects)
├── PowerSharp.Platform/      # Foundation (1 project)
├── PowerSharp.Aspire/        # Cloud-native (5 projects)
└── PnPSharp/                 # PnP + PowerShell (5 projects)
    ├── src/PnPSharp/
    ├── samples/
    └── tests/
```

## Need Help?

1. Read `.copilot-context.md` first
2. Check `README.md` for detailed docs
3. Review `INTEGRATION_STATUS.md` for integration details

---
**Always start new sessions by reading `.copilot-context.md`**
