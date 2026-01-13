
Add-Type -AssemblyName System.Drawing

function Resize-Image {
    param (
        [string]$ProcessFile,
        [int]$Width,
        [int]$Height
    )

    try {
        $image = [System.Drawing.Image]::FromFile($ProcessFile)
        $bmp = new-object System.Drawing.Bitmap $Width, $Height
        $graph = [System.Drawing.Graphics]::FromImage($bmp)
        $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graph.DrawImage($image, 0, 0, $Width, $Height)
        $image.Dispose()
        return $bmp
    }
    catch {
        Write-Host "Error resizing $ProcessFile : $_"
        return $null
    }
}

$files = @("Assets\about_v2.png", "Assets\model_check.png", "Assets\missing_tag.png")

foreach ($file in $files) {
    $path = Join-Path "E:\My\BIM'ism\Revit Pluging" $file
    if (Test-Path $path) {
        Write-Host "Processing $file..."
        $newBmp = Resize-Image -ProcessFile $path -Width 96 -Height 96
        if ($newBmp) {
            $newBmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
            $newBmp.Dispose()
            Write-Host "Resized $file to 96x96."
        }
    } else {
        Write-Host "File not found: $path"
    }
}
