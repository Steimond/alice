# A contact sheet of game frames (128x80 canvas), each cropped to Alice and enlarged, named, for eyeballing a
# detail across many animations at once (the needle, owner's rule of 6 Nov 2026).
param(
    [Parameter(Mandatory)][string[]]$Names,   # file names without folder, e.g. alice_trial_fall_00
    [Parameter(Mandatory)][string]$Out,
    [string]$Dir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial",
    [int]$X0 = 24, [int]$Y0 = 16, [int]$W = 80, [int]$H = 64, [int]$Scale = 3, [int]$PerRow = 8
)

Add-Type -AssemblyName System.Drawing
$names = $Names | ForEach-Object { $_ -split ',' } | Where-Object { $_ }
$rows = [Math]::Ceiling($names.Count / $PerRow)
$cellW = $W * $Scale + 4; $cellH = $H * $Scale + 16
$sheet = New-Object System.Drawing.Bitmap ($PerRow * $cellW), ($rows * $cellH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 58, 58, 64))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font = New-Object System.Drawing.Font "Consolas", 8
for ($i = 0; $i -lt $names.Count; $i++)
{
    $ox = ($i % $PerRow) * $cellW; $oy = [Math]::Floor($i / $PerRow) * $cellH
    $file = Join-Path $Dir "$($names[$i]).png"
    if (Test-Path $file)
    {
        $b = New-Object System.Drawing.Bitmap $file
        $g.DrawImage($b, (New-Object System.Drawing.Rectangle $ox, ($oy + 14), ($W * $Scale), ($H * $Scale)), (New-Object System.Drawing.Rectangle $X0, $Y0, $W, $H), [System.Drawing.GraphicsUnit]::Pixel)
        $b.Dispose()
    }
    $g.DrawString(($names[$i] -replace 'alice_trial_', ''), $font, [System.Drawing.Brushes]::White, $ox + 2, $oy)
}
$g.Dispose()
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
