# Review sheet (owner's rule, 6 Nov 2026: after every fix look again the way the player sees it). Each frame twice,
# enlarged on the room's dark backdrop: as it is, and with a white 1-px outline round every opaque pixel, like the
# execution highlight, which shows every stray pixel.
param(
    [Parameter(Mandatory)][string]$Files,
    [Parameter(Mandatory)][string]$Out,
    [int]$Scale = 4,
    [int]$PerRow = 4
)

Add-Type -AssemblyName System.Drawing
$paths = @(Get-ChildItem $Files | Sort-Object Name)
$first = New-Object System.Drawing.Bitmap $paths[0].FullName
$w = $first.Width; $h = $first.Height; $first.Dispose()
$cellW = 2 * $w * $Scale + 12; $cellH = $h * $Scale + 18
$rows = [Math]::Ceiling($paths.Count / $PerRow)
$sheet = New-Object System.Drawing.Bitmap ($PerRow * $cellW), ($rows * $cellH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 26, 24, 30))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font = New-Object System.Drawing.Font "Consolas", 8
for ($i = 0; $i -lt $paths.Count; $i++)
{
    $ox = ($i % $PerRow) * $cellW; $oy = [Math]::Floor($i / $PerRow) * $cellH + 14
    $b = New-Object System.Drawing.Bitmap $paths[$i].FullName
    $g.DrawImage($b, $ox, $oy, $w * $Scale, $h * $Scale)
    $outlined = New-Object System.Drawing.Bitmap $b
    $white = [System.Drawing.Color]::White
    for ($y = 0; $y -lt $h; $y++)
    {
        for ($x = 0; $x -lt $w; $x++)
        {
            if ($b.GetPixel($x, $y).A -ge 128) { continue }
            $touch = $false
            foreach ($d in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1)))
            {
                $nx = $x + $d[0]; $ny = $y + $d[1]
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $w -and $ny -lt $h -and $b.GetPixel($nx, $ny).A -ge 128) { $touch = $true }
            }
            if ($touch) { $outlined.SetPixel($x, $y, $white) }
        }
    }
    $g.DrawImage($outlined, $ox + $w * $Scale + 4, $oy, $w * $Scale, $h * $Scale)
    $outlined.Dispose(); $b.Dispose()
    $g.DrawString($paths[$i].BaseName, $font, [System.Drawing.Brushes]::White, $ox + 2, $oy - 13)
}
$g.Dispose()
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
