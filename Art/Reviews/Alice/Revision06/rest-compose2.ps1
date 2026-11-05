# Builds Alice's rest frames at the armchair facing the viewer (owner's layout of 4 Nov 2026): a giant thread
# spool beside the chair carries a pincushion; Alice stabs her needle into it, turns to us and sits down.
# Canvas 160x80, pivot bottom centre = where Alice stands when the rest starts. The props are never animated
# by the generator: they come from single images here, so they match the standalone rest sprite exactly.
# Layers, back to front: spool, chair, the needle stuck in the cushion, the cushion, Alice, the needle in
# her hand.
param(
    [string]$Dir = $PSScriptRoot,
    [string]$AliceDir = (Join-Path $PSScriptRoot 'rest2-clean'),
    [string]$OutDir = (Join-Path $PSScriptRoot 'rest2-frames'),
    [int]$Width = 160,
    # cleanup.ps1 put raw Alice at +36 x on a 128 canvas; here raw Alice sits at +54, centred on 80.
    [int]$AliceShiftX = 18,
    [int[]]$ChairAt = @(47, 16),     # raw chair pixel -> canvas offset
    [int[]]$SpoolAt = @(90, 17),     # raw spool pixel -> canvas offset
    [int[]]$CushionAt = @(95, 12),   # raw cushion pixel -> canvas offset: it sits on the spool top (row 40)
    # Frame from which the needle stays in the cushion.
    [int]$StuckFrom = 3,
    # Per frame: [shiftX, lift] for Alice; the last pair repeats. She drifts onto the seat centre and is lifted
    # onto the high seat as her feet leave the floor (frames 13-15 are the generated in-betweens).
    [string]$Moves = '0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;0,0;-2,-2;-3,-5;-5,-7;-6,-9'
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null

$steelDark = [System.Drawing.Color]::FromArgb(255, 38, 40, 48)
$steelMid = [System.Drawing.Color]::FromArgb(255, 132, 140, 152)
$steelLight = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
$clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)

function Layer([string]$file, [int[]]$at) {
    $src = [System.Drawing.Bitmap]::FromFile((Join-Path $Dir $file))
    $out = New-Object System.Drawing.Bitmap $Width, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $src.Height; $y++) { for ($x = 0; $x -lt $src.Width; $x++) {
        $c = $src.GetPixel($x, $y); $tx = $x + $at[0]; $ty = $y + $at[1]
        if ($c.A -ge 128 -and $tx -ge 0 -and $ty -ge 0 -and $tx -lt $Width -and $ty -lt 80) {
            $out.SetPixel($tx, $ty, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
        }
    } }
    $src.Dispose()
    return $out
}

function Over($dst, $src) {
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt $Width; $x++) {
        $c = $src.GetPixel($x, $y); if ($c.A -ge 128) { $dst.SetPixel($x, $y, $c) }
    } }
}

# The needle as needle.ps1 draws it: a one-pixel shaft from a few pixels inside the fist, the eye at the fist.
function Needle($img, [double]$hx, [double]$hy, [double]$deg, [int]$length = 32) {
    $a = $deg * [math]::PI / 180; $ux = [math]::Cos($a); $uy = - [math]::Sin($a)
    $sx = $hx - 3 * $ux; $sy = $hy - 3 * $uy
    for ($t = 0; $t -le $length; $t += 0.5) {
        $col = if ($t -lt 3) { $steelDark } elseif ($t -gt $length - 8) { $steelMid } else { $steelLight }
        $px = [int][math]::Round($sx + $t * $ux); $py = [int][math]::Round($sy + $t * $uy)
        if ($px -ge 0 -and $py -ge 0 -and $px -lt $Width -and $py -lt 79) { $img.SetPixel($px, $py, $col) }
    }
    $ex = [int][math]::Round($sx + $ux); $ey = [int][math]::Round($sy + $uy)
    foreach ($o in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1))) {
        $px = $ex + $o[0]; $py = $ey + $o[1]
        if ($px -ge 0 -and $py -ge 0 -and $px -lt $Width -and $py -lt 79) { $img.SetPixel($px, $py, $steelDark) }
    }
    if ($ex -ge 0 -and $ey -ge 0 -and $ex -lt $Width -and $ey -lt 80) { $img.SetPixel($ex, $ey, $clear) }
}

$spec = Get-Content (Join-Path $Dir 'rest2-needle.json') -Raw | ConvertFrom-Json
$stuck = $spec.stuck
$steps = @($Moves.Split(';') | ForEach-Object { $p = $_.Split(','); , @([int]$p[0], [int]$p[1]) })

$back = Layer 'spools-bare.png' $SpoolAt
Over $back (Layer 'chair-front-01.png' $ChairAt)
$cushion = Layer 'pincushion.png' $CushionAt

# The standalone rest sprite: props only, the cushion without Alice's needle.
$props = New-Object System.Drawing.Bitmap $Width, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
Over $props $back; Over $props $cushion
$props.Save((Join-Path $OutDir 'props.png'), [System.Drawing.Imaging.ImageFormat]::Png); $props.Dispose()

$files = Get-ChildItem $AliceDir -Filter *.png | Sort-Object Name
for ($i = 0; $i -lt $files.Count; $i++) {
    $out = New-Object System.Drawing.Bitmap $Width, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    Over $out $back
    if ($i -ge $StuckFrom) { Needle $out $stuck.hx $stuck.hy $stuck.deg $stuck.len }
    Over $out $cushion
    $m = if ($i -lt $steps.Count) { $steps[$i] } else { $steps[$steps.Count - 1] }
    $alice = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) {
        $c = $alice.GetPixel($x, $y); $tx = $x + $AliceShiftX + $m[0]; $ty = $y + $m[1]
        if ($c.A -ge 128 -and $tx -ge 0 -and $ty -ge 0 -and $tx -lt $Width -and $ty -lt 80) { $out.SetPixel($tx, $ty, $c) }
    } }
    $alice.Dispose()
    $h = $spec.hand | Where-Object { $_.frame -eq $i }
    if ($h) { Needle $out $h.hx $h.hy $h.deg }
    $out.Save((Join-Path $OutDir ('rest-{0:D2}.png' -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}
$back.Dispose(); $cushion.Dispose()
