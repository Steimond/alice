# Turns the key sprite into a set of rotations for pasting into Alice's frames by hand points, as the needle is.
# Rotation is done RotSprite-style: the sprite is enlarged 8 times, turned with nearest sampling, and every 8 x 8
# block is brought back to one pixel by its most frequent colour, so lines stay whole instead of breaking up.
# The grip (where the fist closes) is the turning point; it lands on the same canvas pixel in every rotation.
# Usage: key-rotate.ps1 -In key.png -GripX 13 -GripY 6 -OutDir rot [-Steps 16]
param([string]$In, [int]$GripX, [int]$GripY, [string]$OutDir, [int]$Steps = 16)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null
$src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $In).Path)
$w = $src.Width; $h = $src.Height
$k = 8
$size = 2 * [int][Math]::Ceiling([Math]::Sqrt($w * $w + $h * $h)) + 4
$c = [int]($size / 2)
for ($s = 0; $s -lt $Steps; $s++)
{
    $deg = 360.0 * $s / $Steps
    $a = -$deg * [Math]::PI / 180.0  # counter-clockwise on screen (y down)
    $cos = [Math]::Cos($a); $sin = [Math]::Sin($a)
    $out = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($oy = 0; $oy -lt $size; $oy++) { for ($ox = 0; $ox -lt $size; $ox++) {
        $votes = @{}; $empty = 0
        for ($sy = 0; $sy -lt $k; $sy++) { for ($sx = 0; $sx -lt $k; $sx++) {
            # Sub-sample centre relative to the grip, turned back into the source.
            $dx = ($ox - $c) + ($sx + 0.5) / $k - 0.5; $dy = ($oy - $c) + ($sy + 0.5) / $k - 0.5
            $rx = $cos * $dx + $sin * $dy; $ry = -$sin * $dx + $cos * $dy
            $px = [int][Math]::Floor($rx + $GripX + 0.5); $py = [int][Math]::Floor($ry + $GripY + 0.5)
            if ($px -lt 0 -or $py -lt 0 -or $px -ge $w -or $py -ge $h) { $empty++; continue }
            $p = $src.GetPixel($px, $py)
            if ($p.A -lt 128) { $empty++; continue }
            $key = $p.ToArgb(); $votes[$key] = 1 + $votes[$key] } }
        if ($votes.Count -eq 0 -or $empty -gt ($k * $k) / 2) { continue }
        $best = ($votes.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Name
        $out.SetPixel($ox, $oy, [System.Drawing.Color]::FromArgb([int]$best)) } }
    $out.Save((Join-Path (Resolve-Path $OutDir).Path ("sword_{0:D3}.png" -f [int]$deg)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}
$src.Dispose()
"{0} rotations, canvas {1}, grip at {2},{2}" -f $Steps, $size, $c
