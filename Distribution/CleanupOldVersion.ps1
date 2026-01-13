# BIMism AI Agent - Complete Cleanup Script
# This removes all old installations and cached data

Write-Host "BIMism AI Agent - Cleanup Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Define paths
$revitAddinsPath = "$env:APPDATA\Autodesk\Revit\Addins\2025"
$bimismFolder = Join-Path $revitAddinsPath "BIMism"
$addinFile = Join-Path $revitAddinsPath "BIMism.addin"

Write-Host "Checking for existing installation..." -ForegroundColor Yellow

# Remove BIMism folder
if (Test-Path $bimismFolder) {
    Write-Host "Removing old BIMism folder: $bimismFolder" -ForegroundColor Yellow
    Remove-Item $bimismFolder -Recurse -Force
    Write-Host "✓ Folder removed" -ForegroundColor Green
} else {
    Write-Host "No BIMism folder found" -ForegroundColor Gray
}

# Remove .addin file
if (Test-Path $addinFile) {
    Write-Host "Removing old .addin file: $addinFile" -ForegroundColor Yellow
    Remove-Item $addinFile -Force
    Write-Host "✓ Addin file removed" -ForegroundColor Green
} else {
    Write-Host "No .addin file found" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host "You can now run the new installer: BIMism_AI_Agent_Setup_v1.9.5.exe" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
