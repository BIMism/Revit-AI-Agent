
$source = "e:\My\BIM'ism\Revit Pluging\bin\Release\net8.0-windows"
$destination = "e:\My\BIM'ism\Revit Pluging\BIMism_Setup.zip"
if (Test-Path $destination) { Remove-Item $destination }
Compress-Archive -Path "$source\*" -DestinationPath $destination
Write-Host "Zipped to $destination"
