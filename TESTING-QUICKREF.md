# PowerSharp Testing - Quick Reference

## 🚀 Quick Commands

### Run All Tests
```powershell
dotnet test PowerSharp.Platform.sln -c Debug
```

### Run with Coverage (Simple)
```powershell
.\RUN-COVERAGE.ps1
```

### Run with HTML Report
```powershell
.\RUN-COVERAGE.ps1 -GenerateReport -Open
```

### Run Specific Project
```powershell
.\RUN-COVERAGE.ps1 -Project "PowerSharp.Aspire.Tests"
```

### Enforce Threshold
```powershell
.\RUN-COVERAGE.ps1 -Threshold 80 -ThresholdType line
```

## 📊 Test Projects

| Project | Tests | Purpose |
|---------|-------|---------|
| **PowerSharp.Aspire.Tests** | 17 | Distributed app integration |
| **PnPSharp.Tests** | TBD | Unit tests with mocking |
| **PnPSharp.IntegrationTests** | TBD | Live SharePoint/Graph |

## 🎯 Coverage Thresholds

| Level | Line | Branch | Method |
|-------|------|--------|--------|
| **Minimum** (CI enforced) | 60% | 50% | 55% |
| **Target** | 80% | 70% | 75% |

## 📁 Key Files

- `coverlet.runsettings` - Coverage configuration
- `Directory.Build.props` - SourceLink, deterministic builds
- `.coveragerc` - Documentation, best practices
- `RUN-COVERAGE.ps1` - Automation script
- `.github/workflows/dotnet-ci.yml` - CI/CD pipeline
- `TESTING-INFRASTRUCTURE.md` - Comprehensive guide

## 🔧 VS Code Integration

1. Install: `Coverage Gutters` extension
2. Run: `.\RUN-COVERAGE.ps1`
3. Open any `.cs` file
4. Click "Watch" in status bar
5. See inline coverage (green/red)

## ⚙️ CI/CD

- **Triggers:** Push to main/develop, PRs
- **Enforces:** 60% minimum line coverage
- **Artifacts:** Coverage reports, test results (30 days)
- **View:** GitHub Actions tab

## 📦 Package Versions

- xUnit: **2.9.2**
- xunit.analyzers: **1.16.0**
- coverlet.collector: **6.0.2**
- Aspire.Hosting.Testing: **9.5.1**

## 🎓 Best Practices

✅ Run coverage before pushing  
✅ Aim for 80% line coverage  
✅ Test critical business logic  
✅ Use xUnit [Fact] and [Theory]  
✅ Proper async/await patterns  
✅ Descriptive test names  

## 🐛 Troubleshooting

**No coverage files?**
- Verify coverlet.collector in test projects
- Check runsettings path

**Tests fail in Aspire.Tests?**
- Docker must be running
- Or skip with: `dotnet test --filter "FullyQualifiedName!~Aspire"`

**Build errors?**
- Clean: `dotnet clean PowerSharp.Platform.sln`
- Rebuild: `dotnet build PowerSharp.Platform.sln`

---

**Quick Start:** `.\RUN-COVERAGE.ps1 -GenerateReport -Open`
