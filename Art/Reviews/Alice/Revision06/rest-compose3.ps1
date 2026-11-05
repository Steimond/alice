# Rest at the armchair, owner's layout of 4 Nov 2026 (third version). The props are separate sprites that
# each stand on the floor: the armchair facing the viewer, a giant upright spool of red thread, a giant spool of
# blue thread lying on its side, and in front of them, nearer the camera, a low square pincushion with needles
# as long as Alice's. Alice stabs her needle into it with a reverse grip, like a knife, and leaves it standing
# beside the others, then turns to the viewer and sits down in the chair.
# Alice's frames hold only Alice and her needle, on her usual 128x80 canvas (pivot bottom centre = where she
# stands when the rest starts). The cushion is drawn in front of her, so it hides the buried part of the needle.
param(
    [string]$Dir = $PSScriptRoot,
    [string]$AliceDir = (Join-Path $PSScriptRoot 'rest3-clean'),
    [string]$OutDir = (Join-Path $PSScriptRoot 'rest3-frames'),
    [string]$PropsDir = (Join-Path $PSScriptRoot 'rest3-props'),
    [string]$PreviewDir = (Join-Path $PSScriptRoot 'rest3-preview'),
    # The fist stops at the belt on this frame; from here on her needle stands in the cushion.
    [int]$StuckFrom = 4,
    # Per frame [shiftX, lift] for Alice; the last pair repeats. She drifts onto the seat and is lifted onto it
    # as her feet leave the floor (frames 13-15 are the generated in-betweens).
    [string]$Moves = '0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;-3,-2;-5,-5;-7,-7;-9,-9'
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
foreach ($folder in $OutDir, $PropsDir, $PreviewDir) { New-Item -ItemType Directory -Force $folder | Out-Null }

$steelDark = [System.Drawing.Color]::FromArgb(255, 38, 40, 48)
$steelMid = [System.Drawing.Color]::FromArgb(255, 132, 140, 152)
$steelLight = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
$clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)

# The needle as needle.ps1 draws it; rows below $maxY are left out (buried in the cushion).
function Needle($img, [double]$hx, [double]$hy, [double]$deg, [int]$maxY = 79) {
    $a = $deg * [math]::PI / 180; $ux = [math]::Cos($a); $uy = - [math]::Sin($a)
    $sx = $hx - 3 * $ux; $sy = $hy - 3 * $uy; $len = 32
    for ($t = 0; $t -le $len; $t += 0.5) {
        $col = if ($t -lt 3) { $steelDark } elseif ($t -gt $len - 8) { $steelMid } else { $steelLight }
        $px = [int][math]::Round($sx + $t * $ux); $py = [int][math]::Round($sy + $t * $uy)
        if ($px -ge 0 -and $py -ge 0 -and $px -lt $img.Width -and $py -lt [Math]::Min($maxY + 1, $img.Height)) { $img.SetPixel($px, $py, $col) }
    }
    $ex = [int][math]::Round($sx + $ux); $ey = [int][math]::Round($sy + $uy)
    foreach ($o in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1))) {
        $px = $ex + $o[0]; $py = $ey + $o[1]
        if ($px -ge 0 -and $py -ge 0 -and $px -lt $img.Width -and $py -lt $img.Height) { $img.SetPixel($px, $py, $steelDark) }
    }
    if ($ex -ge 0 -and $ey -ge 0 -and $ex -lt $img.Width -and $ey -lt $img.Height) { $img.SetPixel($ex, $ey, $clear) }
}

function Copy-Png($src, $dst) { Copy-Item (Join-Path $Dir $src) (Join-Path $PropsDir $dst) -Force }

# --- props --------------------------------------------------------------------------------------------
Copy-Png 'chair-front-01.png' 'rest_chair.png'
Copy-Png 'spool-red.png' 'rest_spool_red.png'
Copy-Png 'spool-blue.png' 'rest_spool_blue.png'
$cushion = [System.Drawing.Bitmap]::FromFile((Join-Path $Dir 'cushion-low-01.png'))
$pin = New-Object System.Drawing.Bitmap 32, 32, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($pin); $g.DrawImageUnscaled($cushion, 0, 0); $g.Dispose(); $cushion.Dispose()
# Two needles of Alice's size already stand in it, buried below row 19 of the cushion's top.
Needle $pin 20 3 266 19
Needle $pin 26 4 278 20
$pin.Save((Join-Path $PropsDir 'rest_cushion.png'), [System.Drawing.Imaging.ImageFormat]::Png)

# Where each prop's bottom centre stands, in pixels from Alice's pivot (feet centre); sorting order.
$layout = @(
    @{ File = 'rest_chair.png'; X = -6; Order = 4 },
    @{ File = 'rest_spool_red.png'; X = 40; Order = 5 },
    @{ File = 'rest_spool_blue.png'; X = 58; Order = 6 },
    @{ File = 'rest_cushion.png'; X = 21; Order = 12 }
)
$layout | ConvertTo-Json | Set-Content (Join-Path $PropsDir 'layout.json') -Encoding ascii

# --- Alice's frames -----------------------------------------------------------------------------------
$hand = @{ 0 = @(70, 57, 216); 1 = @(73, 44, 272); 2 = @(73, 44, 271); 3 = @(73, 44, 271) }
$stuck = @(77, 52, 270)
$steps = @($Moves.Split(';') | ForEach-Object { $q = $_.Split(','); , @([int]$q[0], [int]$q[1]) })
$files = Get-ChildItem $AliceDir -Filter *.png | Sort-Object Name
for ($i = 0; $i -lt $files.Count; $i++) {
    $m = if ($i -lt $steps.Count) { $steps[$i] } else { $steps[$steps.Count - 1] }
    $alice = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $out = New-Object System.Drawing.Bitmap 128, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    if ($i -ge $StuckFrom) { Needle $out $stuck[0] $stuck[1] $stuck[2] }
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) {
        $c = $alice.GetPixel($x, $y); $tx = $x + $m[0]; $ty = $y + $m[1]
        if ($c.A -ge 128 -and $tx -ge 0 -and $ty -ge 0 -and $tx -lt 128 -and $ty -lt 80) { $out.SetPixel($tx, $ty, $c) }
    } }
    $alice.Dispose()
    # The needle in her fist is in front of her; once stuck, her fist closes over its top until she lets go.
    if ($hand.ContainsKey($i)) { $h = $hand[$i]; Needle $out $h[0] $h[1] $h[2] }
    $out.Save((Join-Path $OutDir ('rest-{0:D2}.png' -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}

# --- preview of the whole scene, sorted as in the game -------------------------------------------------
$frames = Get-ChildItem $OutDir -Filter 'rest-*.png' | Sort-Object Name
foreach ($f in $frames) {
    $scene = New-Object System.Drawing.Bitmap 160, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($scene)
    $items = @($layout | ForEach-Object { $_ }) + @(@{ File = $f.FullName; X = 0; Order = 10 })
    foreach ($item in ($items | Sort-Object { $_.Order })) {
        $path = if ([IO.Path]::IsPathRooted($item.File)) { $item.File } else { Join-Path $PropsDir $item.File }
        $bmp = [System.Drawing.Bitmap]::FromFile($path)
        $left = 64 + 16 + $item.X - [int]($bmp.Width / 2); $top = 80 - $bmp.Height
        $g.DrawImageUnscaled($bmp, $left, $top); $bmp.Dispose()
    }
    $g.Dispose()
    $scene.Save((Join-Path $PreviewDir $f.Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $scene.Dispose()
}
