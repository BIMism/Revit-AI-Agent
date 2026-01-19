
$appData = $env:APPDATA
$revitAddins = "$appData\Autodesk\Revit\Addins"

# Target Versions
$years = @("2024", "2025")

foreach ($year in $years) {
    $targetDir = "$revitAddins\$year"
    if (Test-Path $targetDir) {
        Write-Host "Installing for Revit $year..."
        
        # 1. Copy .addin file
        Copy-Item -Path "BIMism.addin" -Destination "$targetDir\BIMism.addin" -Force
        
        # 2. Create BIMism folder
        $bimismDir = "$targetDir\BIMism"
        if (!(Test-Path $bimismDir)) { New-Item -ItemType Directory -Path $bimismDir }
        
        # 3. Copy Binaries
        $sourceBin = "bin\Release\net8.0-windows"
        Copy-Item -Path "$sourceBin\RevitAIAgent.dll" -Destination "$bimismDir\RevitAIAgent.dll" -Force
        Copy-Item -Path "$sourceBin\Newtonsoft.Json.dll" -Destination "$bimismDir\Newtonsoft.Json.dll" -Force
        
        # 4. Copy Assets (Knowledge)
        $assetsDir = "$bimismDir\Assets"
        if (!(Test-Path $assetsDir)) { New-Item -ItemType Directory -Path $assetsDir }
        Copy-Item -Path "Assets\RevitKnowledge.json" -Destination "$assetsDir\RevitKnowledge.json" -Force
        
        Write-Host "✅ installed successfully for $year"
    }
}
Write-Host "DONE. Please restart Revit."
