# Build the Inno Setup installer. Requires iscc (Inno Setup 6) on PATH and a
# published build in publish\win-x64 (run scripts\publish.ps1 first).
$ErrorActionPreference = "Stop"

if (-not (Get-Command iscc -ErrorAction SilentlyContinue)) {
    Write-Error "iscc not found. Install Inno Setup 6 and add it to PATH."
}

if (-not (Test-Path "publish\win-x64\InwardDC.exe")) {
    Write-Error "publish\win-x64\InwardDC.exe not found. Run scripts\publish.ps1 first."
}

iscc installer\installer.iss
Write-Host ""
Write-Host "Installer written to installer\output."
