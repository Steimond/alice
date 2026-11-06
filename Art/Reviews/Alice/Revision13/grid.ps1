# Enlarged crops of the clean frames with a labelled 4-pixel grid in canvas coordinates, to read hand positions.
param(
    [int]$X0 = 44, [int]$Y0 = 28, [int]$W = 48, [int]$H = 52, [int]$Scale = 5, [int]$PerRow = 6,
    [string]$Dir = (Join-Path $PSScriptRoot "clean"),
    [string]$Out = (Join-Path $PSScriptRoot "grid.png")
)

Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem $Dir -Filter *.png | Sort-Object Name
$rows = [Math]::Ceiling($files.Count / $PerRow)
$cellW = $W * $Scale + 30; $cellH = $H * $Scale + 30
$sheet = New-Object System.Drawing.Bitmap ($PerRow * $cellW), ($rows * $cellH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 40, 40, 46))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font = New-Object System.Drawing.Font "Consolas", 8
$minor = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(45, 255, 255, 255))
$major = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(110, 255, 220, 120))
$i = 0
foreach ($file in $files)
{
    $ox = ($i % $PerRow) * $cellW + 26; $oy = [Math]::Floor($i / $PerRow) * $cellH + 22
    $b = New-Object System.Drawing.Bitmap $file.FullName
    $g.DrawImage($b, (New-Object System.Drawing.Rectangle $ox, $oy, ($W * $Scale), ($H * $Scale)), (New-Object System.Drawing.Rectangle $X0, $Y0, $W, $H), [System.Drawing.GraphicsUnit]::Pixel)
    $b.Dispose()
    for ($x = 0; $x -le $W; $x += 4)
    {
        $pen = if ((($X0 + $x) % 8) -eq 0) { $major } else { $minor }
        $g.DrawLine($pen, $ox + $x * $Scale, $oy, $ox + $x * $Scale, $oy + $H * $Scale)
        if ((($X0 + $x) % 8) -eq 0) { $g.DrawString(($X0 + $x).ToString(), $font, [System.Drawing.Brushes]::Khaki, $ox + $x * $Scale - 6, $oy - 14) }
    }
    for ($y = 0; $y -le $H; $y += 4)
    {
        $pen = if ((($Y0 + $y) % 8) -eq 0) { $major } else { $minor }
        $g.DrawLine($pen, $ox, $oy + $y * $Scale, $ox + $W * $Scale, $oy + $y * $Scale)
        if ((($Y0 + $y) % 8) -eq 0) { $g.DrawString(($Y0 + $y).ToString(), $font, [System.Drawing.Brushes]::Khaki, $ox - 24, $oy + $y * $Scale - 6) }
    }
    $g.DrawString($file.BaseName, $font, [System.Drawing.Brushes]::White, $ox + 2, $oy + 2)
    $i++
}
$g.Dispose()
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
