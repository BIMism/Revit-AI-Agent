# Build and Deploy Script for Revit AI Agent
# Run this after making changes to deploy to Revit

Write-Host "Building RevitAIAgent..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Deploying to Revit..." -ForegroundColor Green
    
    $outputDir = "bin\Debug\net8.0-windows"
    $revitAddinsDir = "$env:AppData\Autodesk\Revit\Addins\2025"
    
    # Copy DLL
    Copy-Item "$outputDir\RevitAIAgent.dll" "$revitAddinsDir\RevitAIAgent.dll" -Force
    Write-Host "  ✓ Copied DLL" -ForegroundColor Green
    
    # Copy all PNG icons
    Copy-Item "$outputDir\*.png" "$revitAddinsDir\" -Force
    Write-Host "  ✓ Copied icons" -ForegroundColor Green
    
    Write-Host "`nDeployment complete! Restart Revit to see changes." -ForegroundColor Yellow
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}
