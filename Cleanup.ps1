$years = @("2023", "2024", "2025", "2026")
foreach ($year in $years) {
    $path = "$env:APPDATA\Autodesk\Revit\Addins\$year"
    if (Test-Path $path) {
        $addin = "$path\BIMism.addin"
        $folder = "$path\BIMism"
        
        if (Test-Path $addin) { 
            try { Remove-Item $addin -Force; Write-Host "Removed $addin" } catch { Write-Host "Failed to remove $addin" }
        }
        if (Test-Path $folder) { 
            try { Remove-Item $folder -Recurse -Force; Write-Host "Removed $folder" } catch { Write-Host "Failed to remove $folder" }
        }
    }
}
Write-Host "Cleanup process finished."
