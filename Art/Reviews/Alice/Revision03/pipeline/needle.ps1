# Draws Alice's needle onto cleaned 128x80 frames so that it turns with her body (owner's note, 4 Nov
# 2026: a needle glued to the hand at a fixed length reads as a weightless stick).
#
# Per frame, in canvas pixels (JSON array, one entry per frame):
#   {"hx", "hy", "deg"}                      the old way: 32 px from the hand at an angle (0 = +x, CCW)
#   {"hx", "hy", "theta", "cx", "smear"?}    a turning blow: the point lies on a flat ellipse round the
#                                             body (cx = body centre, theta 0 = forward, 90 = toward the
#                                             camera, 180 = back, 270 = away). Toward or away from the camera
#                                             the needle shortens by itself; in the far half it is drawn only
#                                             where nothing of Alice is in front of it.
#   "smear": theta the point came from; a pale streak follows the point's path back to it (decreasing
#   theta, the direction Alice turns), so the swing reads between frames.
# Any frame may also carry:
#   "arc": [cx, cy, r, from, to, dir]  a swing trail round a shoulder from angle 'from' to 'to' (degrees,
#                                   0 = +x, counter-clockwise); dir +1 = angle rising, otherwise falling;
#                                   drawn only where Alice is not
#   "streak": n                     speed lines n pixels long behind the hand, along the needle, for a thrust
param(
    [Parameter(Mandatory)] [string]$FramesDir,
    [Parameter(Mandatory)] [string]$OutDir,
    [Parameter(Mandatory)] [string]$Json,
    [double]$Radius = 55,        # point's distance from the body centre when it points straight forward
    [double]$Flatness = 4,       # half-height of the ellipse: how far toward/away tilts the point down/up
    [int]$NeedleLength = 32
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null

$steelDark = [System.Drawing.Color]::FromArgb(255, 38, 40, 48)
$steelMid = [System.Drawing.Color]::FromArgb(255, 132, 140, 152)
$steelLight = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
$smearOuter = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
$smearInner = [System.Drawing.Color]::FromArgb(255, 132, 140, 152)

$specs = Get-Content $Json -Raw | ConvertFrom-Json
$files = Get-ChildItem $FramesDir -Filter *.png | Sort-Object Name

function Rad($deg) { $deg * [math]::PI / 180 }
function Point($cx, $cy, $theta, $radius) {
    $a = Rad $theta
    @{ x = $cx + $radius * [math]::Cos($a); y = $cy + $Flatness * ($radius / $Radius) * [math]::Sin($a) }
}

for ($i = 0; $i -lt $files.Count; $i++) {
    $src = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $img = [System.Drawing.Bitmap]::new($src); $src.Dispose()
    # What Alice covers, before anything is added: the far half is drawn only outside it.
    $body = New-Object 'bool[,]' 128, 80
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) { $body[$x, $y] = $img.GetPixel($x, $y).A -ge 128 } }
    function Put($x, $y, $color, [bool]$behind) {
        $px = [int][math]::Round($x); $py = [int][math]::Round($y)
        if ($px -lt 0 -or $py -lt 0 -or $px -ge 128 -or $py -ge 79) { return }
        if ($behind -and $body[$px, $py]) { return }
        $img.SetPixel($px, $py, $color)
    }

    $s = $specs[$i]
    $hx = [double]$s.hx; $hy = [double]$s.hy
    if ($null -ne $s.theta) {
        $theta = [double]$s.theta
        $behind = [math]::Sin((Rad $theta)) -lt -0.2
        if ($null -ne $s.smear) {
            # The streak runs from the earlier point down to this one, a little inside the point's path.
            $from = [double]$s.smear; $to = $theta
            while ($from -le $to) { $from += 360 }
            for ($t = $to; $t -le $from; $t += 1.5) {
                $far = [math]::Sin((Rad $t)) -lt -0.2
                $p1 = Point $s.cx $hy $t ($Radius - 1); $p2 = Point $s.cx $hy $t ($Radius - 3)
                Put $p1.x $p1.y $smearOuter $far
                if ([int]$t % 3 -eq 0) { Put $p2.x ($p2.y + 1) $smearInner $far }
            }
        }
        $tip = Point $s.cx $hy $theta $Radius
        $dx = $tip.x - $hx; $dy = $tip.y - $hy
        $len = [math]::Min($NeedleLength, [math]::Sqrt($dx * $dx + $dy * $dy))
        $ux = if ($len -gt 0) { $dx / [math]::Sqrt($dx * $dx + $dy * $dy) } else { 0 }
        $uy = if ($len -gt 0) { $dy / [math]::Sqrt($dx * $dx + $dy * $dy) } else { 0 }
    }
    else {
        $behind = $false
        $a = Rad ([double]$s.deg)
        $ux = [math]::Cos($a); $uy = - [math]::Sin($a); $len = $NeedleLength
    }

    if ($null -ne $s.arc) {
        $acx = [double]$s.arc[0]; $acy = [double]$s.arc[1]; $ar = [double]$s.arc[2]
        $from = [double]$s.arc[3]; $to = [double]$s.arc[4]
        # Sixth value: +1 when the swing turns counter-clockwise (angle rising), else it falls.
        $rising = $s.arc.Count -ge 6 -and [double]$s.arc[5] -gt 0
        if ($rising) { while ($to -lt $from) { $to += 360 }; $lo = $from; $hi = $to }
        else { while ($from -le $to) { $from += 360 }; $lo = $to; $hi = $from }
        for ($t = $lo; $t -le $hi; $t += 0.75) {
            $a = Rad $t
            Put ($acx + $ar * [math]::Cos($a)) ($acy - $ar * [math]::Sin($a)) $smearOuter $true
            Put ($acx + ($ar - 1) * [math]::Cos($a)) ($acy - ($ar - 1) * [math]::Sin($a)) $smearOuter $true
            if ([int]$t % 3 -eq 0) { Put ($acx + ($ar - 3) * [math]::Cos($a)) ($acy - ($ar - 3) * [math]::Sin($a)) $smearInner $true }
        }
    }
    if ($null -ne $s.streak) {
        # Two broken lines just above and below the shaft, trailing behind the fist.
        for ($t = 2; $t -le [double]$s.streak; $t += 1) {
            if ($t % 4 -eq 3) { continue }
            Put ($hx - $t * $ux) ($hy - $t * $uy - 2) $smearOuter $true
            if ($t % 2 -eq 0) { Put ($hx - ($t + 2) * $ux) ($hy - ($t + 2) * $uy + 2) $smearInner $true }
        }
    }

    # A one-pixel shaft from a few pixels inside the fist to the point; the eye shows only at full length.
    $sx = $hx - 3 * $ux; $sy = $hy - 3 * $uy
    for ($t = 0; $t -le $len; $t += 0.5) {
        $col = if ($t -lt 3) { $steelDark } elseif ($t -gt $len - 8) { $steelMid } else { $steelLight }
        Put ($sx + $t * $ux) ($sy + $t * $uy) $col $behind
    }
    if ($len -ge $NeedleLength - 1 -and -not $behind) {
        $ex = [int][math]::Round($sx + $ux); $ey = [int][math]::Round($sy + $uy)
        foreach ($d in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1))) { Put ($ex + $d[0]) ($ey + $d[1]) $steelDark $false }
        if ($ex -ge 0 -and $ey -ge 0 -and $ex -lt 128 -and $ey -lt 80) { $img.SetPixel($ex, $ey, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)) }
    }

    $img.Save((Join-Path $OutDir $files[$i].Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $img.Dispose()
}
