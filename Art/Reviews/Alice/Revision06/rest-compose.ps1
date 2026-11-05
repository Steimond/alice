# Builds Alice's rest frames in the armchair on the 128x80 Alice canvas (pivot bottom centre).
# The chair is never animated by the generator: it is drawn here from chair-01.png on every frame, so it
# matches the standalone chair sprite pixel for pixel. Layers: the whole chair behind Alice, Alice, and,
# once she has sat down, the near armrest and front of the chair in front of her.
param(
    [string]$Dir = $PSScriptRoot,
    [string]$AliceDir = (Join-Path $PSScriptRoot 'sit2-clean'),
    [string]$OutDir = (Join-Path $PSScriptRoot 'rest-frames'),
    # Alice is 8 px further right than the chair in raw pixels: her back rests against the backrest.
    [int]$AliceOffsetX = 8,
    # The chair's raw legs end on row 60, Alice's feet on 59: one row up puts both on the canvas floor.
    [int]$ChairShiftY = -1,
    # Near-armrest layer: chair pixels right of this raw column and below this raw row.
    [int]$FrontFromX = 27,
    [int]$FrontFromY = 30,
    # From this frame on she is inside the chair (front layer drawn, lifted onto the seat).
    [int]$SeatedFrom = 5,
    # Per-frame lift in pixels (negative = up): her hips meet the seat cushion instead of the floor.
    [int[]]$Lift = @(0, 0, 0, -1, -3, -6, -8)
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null

# cleanup.ps1 places raw Alice pixels at +36 x, +20 y (+ her per-frame ground shift, 0 for these frames).
$ox = 36 - $AliceOffsetX; $oy = 20 + $ChairShiftY
$chairSrc = [System.Drawing.Bitmap]::FromFile((Join-Path $Dir 'chair-01.png'))
$back = New-Object System.Drawing.Bitmap 128, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$front = New-Object System.Drawing.Bitmap 128, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt 64; $y++) { for ($x = 0; $x -lt 64; $x++) {
    $c = $chairSrc.GetPixel($x, $y)
    if ($c.A -lt 128) { continue }
    $solid = [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B)
    $tx = $x + $ox; $ty = $y + $oy
    if ($tx -lt 0 -or $ty -lt 0 -or $tx -ge 128 -or $ty -ge 80) { continue }
    $back.SetPixel($tx, $ty, $solid)
    if ($x -ge $FrontFromX -and $y -ge $FrontFromY) { $front.SetPixel($tx, $ty, $solid) }
} }
$chairSrc.Dispose()
$back.Save((Join-Path $OutDir 'chair.png'), [System.Drawing.Imaging.ImageFormat]::Png)

$files = Get-ChildItem $AliceDir -Filter *.png | Sort-Object Name
for ($i = 0; $i -lt $files.Count; $i++) {
    $alice = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $up = if ($i -lt $Lift.Count) { $Lift[$i] } else { $Lift[$Lift.Count - 1] }
    $out = New-Object System.Drawing.Bitmap 128, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($out)
    $g.DrawImageUnscaled($back, 0, 0)
    $g.Dispose()
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) {
        $c = $alice.GetPixel($x, $y)
        $ty = $y + $up
        if ($c.A -ge 128 -and $ty -ge 0 -and $ty -lt 80) { $out.SetPixel($x, $ty, $c) }
    } }
    $alice.Dispose()
    if ($i -ge $SeatedFrom) {
        for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) {
            $c = $front.GetPixel($x, $y)
            if ($c.A -ge 128) { $out.SetPixel($x, $y, $c) }
        } }
    }
    $out.Save((Join-Path $OutDir ('sit-{0:D2}.png' -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}
$back.Dispose(); $front.Dispose()
