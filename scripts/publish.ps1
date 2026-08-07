# Publish the WPF app for Windows (run on a Windows machine with .NET 8 SDK).
$ErrorActionPreference = "Stop"

$out = if ($args.Count -gt 0) { $args[0] } else { "publish\win-x64" }
$rid = if ($args.Count -gt 1) { $args[1] } else { "win-x64" }

Write-Host "Publishing InwardDC ($rid) -> $out"
dotnet publish "src\InwardDC.App\InwardDC.App.csproj" `
  -c Release -r $rid `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $out

Write-Host ""
Write-Host "Published. Next steps:"
Write-Host "  1. Run installer\build-installer.ps1 to produce setup.exe"
Write-Host "  2. Or copy $out\InwardDC.exe to any 64-bit Windows machine (no .NET required)"
