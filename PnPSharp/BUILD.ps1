param([switch]$Release)
$cfg = if ($Release) { "Release" } else { "Debug" }
Write-Host "Building PnPSharp ($cfg)" -ForegroundColor Cyan
dotnet restore | Out-Null
dotnet build -c $cfg | Out-Null
Write-Host "Run sample" -ForegroundColor Cyan
dotnet run --project .\samples\PnPSharp.Samples -c $cfg