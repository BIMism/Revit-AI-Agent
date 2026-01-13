Add-Type -AssemblyName System.Drawing
function Resize-Image {
    param ([string]$RelativePath, [int]$Width, [int]$Height)
    try {
        $fullPath = Join-Path (Get-Location) $RelativePath
        $image = [System.Drawing.Image]::FromFile($fullPath)
        $bmp = new-object System.Drawing.Bitmap $Width, $Height
        $graph = [System.Drawing.Graphics]::FromImage($bmp)
        $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graph.DrawImage($image, 0, 0, $Width, $Height)
        $image.Dispose()
        $bmp.Save($fullPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        return $true
    } catch { return $false }
}
Resize-Image -RelativePath 'Assets\costing.png' -Width 32 -Height 32
