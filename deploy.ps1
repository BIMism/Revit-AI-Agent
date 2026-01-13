# Build and Deploy Script for Revit AI Agent
# Run this after making changes to deploy to Revit

Write-Host "Building RevitAIAgent..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Deploying to Revit..." -ForegroundColor Green
    
    $outputDir = "bin\Debug\net8.0-windows"
    $revitAddinsDir = "$env:AppData\Autodesk\Revit\Addins\2025"
    $bimismDir = "$revitAddinsDir\BIMism"

    # Create BIMism subfolder
    if (-not (Test-Path $bimismDir)) {
        New-Item -ItemType Directory -Force -Path $bimismDir | Out-Null
    }
    
    # Copy Manifest to Root
    Copy-Item "BIMism.addin" "$revitAddinsDir\BIMism.addin" -Force
    Write-Host "  ✓ Copied Manifest to Root" -ForegroundColor Green

    # Copy DLL to Subfolder
    Copy-Item "$outputDir\RevitAIAgent.dll" "$bimismDir\RevitAIAgent.dll" -Force
    Write-Host "  ✓ Copied DLL to BIMism" -ForegroundColor Green
    
    # Copy all PNG icons and Assets to Subfolder (Assets folder logic)
    $assetsDir = "$bimismDir\Assets"
    if (-not (Test-Path $assetsDir)) {
        New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
    }
    Copy-Item "$outputDir\Assets\*.png" "$assetsDir\" -Force
    Write-Host "  ✓ Copied Assets to BIMism\Assets" -ForegroundColor Green
    
    Write-Host "`nDeployment complete! Restart Revit to see changes." -ForegroundColor Yellow
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}
