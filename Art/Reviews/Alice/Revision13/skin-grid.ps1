# Clean frames enlarged around the hands, skin pixels tinted, with a 2-pixel grid labelled every 4: for telling
# the fist from the bare forearm by eye.
param(
    # Comma-separated: powershell -File passes a list as one string.
    [string]$FrameList = "4,5,6,7,8,9,10,11,12",
    [string]$Dir = (Join-Path $PSScriptRoot "clean-seq"),
    [string]$Out = (Join-Path $PSScriptRoot "skin-grid.png"),
    [int]$X0 = 56, [int]$Y0 = 38, [int]$W = 28, [int]$H = 42, [int]$Scale = 10, [int]$PerRow = 5
)

Add-Type -AssemblyName System.Drawing
$Frames = @($FrameList.Split(",") | ForEach-Object { [int]$_ })
function IsSkin([System.Drawing.Color]$c) { return $c.A -ge 128 -and ($c.R - $c.B) -gt 55 -and $c.R -gt 150 -and $c.G -gt 100 }

$rows = [Math]::Ceiling($Frames.Count / $PerRow)
$cellW = $W * $Scale + 34; $cellH = $H * $Scale + 34
$sheet = New-Object System.Drawing.Bitmap ($PerRow * $cellW), ($rows * $cellH)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 30, 30, 36))
$font = New-Object System.Drawing.Font "Consolas", 9
$minor = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(40, 255, 255, 255))
$major = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(120, 255, 220, 120))
for ($k = 0; $k -lt $Frames.Count; $k++)
{
    $i = $Frames[$k]
    $ox = ($k % $PerRow) * $cellW + 30; $oy = [Math]::Floor($k / $PerRow) * $cellH + 26
    $b = New-Object System.Drawing.Bitmap (Join-Path $Dir ("{0:00}.png" -f $i))
    for ($y = 0; $y -lt $H; $y++)
    {
        for ($x = 0; $x -lt $W; $x++)
        {
            $c = $b.GetPixel($x + $X0, $y + $Y0)
            if ($c.A -lt 128) { continue }
            if (IsSkin $c) { $c = [System.Drawing.Color]::FromArgb(255, [Math]::Min(255, $c.R + 40), [Math]::Max(0, $c.G - 90), [Math]::Max(0, $c.B - 90)) }
            $brush = New-Object System.Drawing.SolidBrush $c
            $g.FillRectangle($brush, $ox + $x * $Scale, $oy + $y * $Scale, $Scale, $Scale)
            $brush.Dispose()
        }
    }
    $b.Dispose()
    for ($x = 0; $x -le $W; $x += 2)
    {
        $pen = if ((($X0 + $x) % 4) -eq 0) { $major } else { $minor }
        $g.DrawLine($pen, $ox + $x * $Scale, $oy, $ox + $x * $Scale, $oy + $H * $Scale)
        if ((($X0 + $x) % 4) -eq 0) { $g.DrawString(($X0 + $x).ToString(), $font, [System.Drawing.Brushes]::Khaki, $ox + $x * $Scale - 7, $oy - 16) }
    }
    for ($y = 0; $y -le $H; $y += 2)
    {
        $pen = if ((($Y0 + $y) % 4) -eq 0) { $major } else { $minor }
        $g.DrawLine($pen, $ox, $oy + $y * $Scale, $ox + $W * $Scale, $oy + $y * $Scale)
        if ((($Y0 + $y) % 4) -eq 0) { $g.DrawString(($Y0 + $y).ToString(), $font, [System.Drawing.Brushes]::Khaki, $ox - 26, $oy + $y * $Scale - 7) }
    }
    $g.DrawString("frame $i", $font, [System.Drawing.Brushes]::White, $ox + 2, $oy + 2)
}
$g.Dispose()
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
