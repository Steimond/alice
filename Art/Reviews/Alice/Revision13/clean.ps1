# Pickup frames 1..12 onto Alice's 128x80 canvas exactly like her full-size frames (x+36, y+20): the stance's
# palette, hard alpha. Colours far from the palette (the grey bundle) keep their own. Then a 4x preview with an
# 8-pixel grid, for reading hand positions for needle.ps1.
param(
    [string]$Raw = (Join-Path $PSScriptRoot "raw\pick-a"),
    [string]$Out = (Join-Path $PSScriptRoot "clean"),
    [int]$Scale = 4
)

Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null
$stand = New-Object System.Drawing.Bitmap (Join-Path $PSScriptRoot "..\Revision03\pipeline\stand\stand-noneedle.png")
$palette = @{}
for ($y = 0; $y -lt 64; $y++) { for ($x = 0; $x -lt 64; $x++) { $c = $stand.GetPixel($x, $y); if ($c.A -ge 128) { $palette[$c.ToArgb()] = $c } } }
$stand.Dispose()
$colours = @($palette.Values)

$files = Get-ChildItem $Raw -Filter *.png | Sort-Object Name | Select-Object -Skip 1
$sheet = New-Object System.Drawing.Bitmap ($files.Count * 128 * $Scale), (80 * $Scale)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 58, 58, 64))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$i = 0
foreach ($file in $files)
{
    $b = New-Object System.Drawing.Bitmap $file.FullName
    $o = New-Object System.Drawing.Bitmap 128, 80
    for ($y = 0; $y -lt 64; $y++)
    {
        for ($x = 0; $x -lt 64; $x++)
        {
            $c = $b.GetPixel($x, $y)
            if ($c.A -lt 128) { continue }
            $best = $c; $bestDistance = 1800
            foreach ($p in $colours)
            {
                $d = 2 * ($c.R - $p.R) * ($c.R - $p.R) + 4 * ($c.G - $p.G) * ($c.G - $p.G) + 3 * ($c.B - $p.B) * ($c.B - $p.B)
                if ($d -lt $bestDistance) { $bestDistance = $d; $best = $p }
            }
            $o.SetPixel($x + 36, $y + 20, [System.Drawing.Color]::FromArgb(255, $best.R, $best.G, $best.B))
        }
    }
    $b.Dispose()
    $o.Save((Join-Path $Out ("{0:00}.png" -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.DrawImage($o, $i * 128 * $Scale, 0, 128 * $Scale, 80 * $Scale)
    $o.Dispose()
    $i++
}

$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(70, 255, 255, 255))
for ($x = 0; $x -le $sheet.Width; $x += 8 * $Scale) { $g.DrawLine($pen, $x, 0, $x, $sheet.Height) }
for ($y = 0; $y -le $sheet.Height; $y += 8 * $Scale) { $g.DrawLine($pen, 0, $y, $sheet.Width, $y) }
$g.Dispose()
$sheet.Save((Join-Path $PSScriptRoot "clean-grid.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
"frames: $i"
