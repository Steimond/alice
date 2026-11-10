# Draws Alice's mirror-shard sword (GDD 0.44) onto cleaned 128x80 frames by the same per-frame JSON as needle.ps1,
# so every animation that had the needle gets the sword without a new generation. The sword is one reference sprite
# (shard/sword.png) turned in 32 steps by sword-rotate.ps1 (RotSprite), grip on the canvas centre; per frame the
# nearest rotation is pasted with its grip on the hand point. The fist is then drawn back over the hilt (skin pixels
# of the clean frame near the hand), so the fingers close round the rag-wrapped base.
#
# JSON entries, as in needle.ps1:
#   {"hx","hy","deg"}                    the blade points at 'deg' (0 = forward, counter-clockwise)
#   {"hx","hy","theta","cx","smear"?}    a turning blow: direction from the hand to a point on a flat ellipse round
#                                        the body; toward or away from the camera the blade is cut shorter; in the
#                                        far half it is drawn only where nothing of Alice is in front of it
#   "arc", "smear", "streak"             swing trails, drawn as in needle.ps1 but scaled to the sword's reach
# A frame whose hand point has no skin within 2 px is reported as DETACHED (snap the JSON with needle-snap.ps1).
param(
    [Parameter(Mandatory)] [string]$FramesDir,
    [Parameter(Mandatory)] [string]$OutDir,
    [Parameter(Mandatory)] [string]$Json,
    [string]$RotDir = '',
    [int]$Steps = 32,
    [double]$Reach = 33,        # grip to point, px
    [double]$Radius = 55,       # needle.ps1 ellipse for turning blows
    [double]$Flatness = 4,
    [double]$NeedleLength = 32, # the needle the JSON was made for: foreshortening is relative to it
    [switch]$Trail,              # running: the blade trails behind the swinging fist as drawn, never turned forward
    [double]$FistRadius = 2.5
)
Add-Type -AssemblyName System.Drawing
if (-not $RotDir) { $RotDir = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "shard\rot" }
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null

$smearOuter = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
$smearInner = [System.Drawing.Color]::FromArgb(255, 150, 160, 176)
$scale = $Reach / $NeedleLength

# Rotations as pixel lists relative to the grip.
$rots = @()
for ($s = 0; $s -lt $Steps; $s++) {
    $deg = 360.0 * $s / $Steps
    $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $RotDir ("sword_{0:D3}.png" -f [int]$deg)))
    $c = [int]($bmp.Width / 2)
    $px = New-Object System.Collections.ArrayList
    for ($y = 0; $y -lt $bmp.Height; $y++) { for ($x = 0; $x -lt $bmp.Width; $x++) {
        $p = $bmp.GetPixel($x, $y)
        if ($p.A -ge 128) { [void]$px.Add(@(($x - $c), ($y - $c), $p)) } } }
    $bmp.Dispose()
    $rots += , $px
}

$specs = Get-Content $Json -Raw | ConvertFrom-Json
$files = Get-ChildItem $FramesDir -Filter *.png | Sort-Object Name
if ($specs.Count -ne $files.Count) { throw "JSON has $($specs.Count) entries, $FramesDir has $($files.Count) frames" }

function Rad($deg) { $deg * [math]::PI / 180 }
function IsSkin([System.Drawing.Color]$c) { $c.A -ge 128 -and ($c.R - $c.B) -gt 55 -and $c.R -gt 170 -and $c.G -gt 120 }
# The needle hung back and down from the fist (about 205-235 degrees); a sword held that way reads as a knife in a
# reverse grip (owner, 10 Nov 2026). Such carry angles are mirrored to forward and down, the ordinary hold. Wind-ups
# that draw the blade back (below 203 degrees) are left as they are.
function Normal-Grip([double]$deg) {
    $d = (($deg % 360) + 360) % 360
    if ($d -ge 203 -and $d -le 240) { return 540 - $d }
    $deg
}
# The sword is longer than the needle: hanging low it would go through the floor, raised high it would leave the
# 80 px canvas. Turn it toward the horizontal on its own side until the point stays within rows 1..77.
function Fit-Canvas([double]$deg, [double]$gy, [double]$reach) {
    for ($guard = 0; $guard -lt 45; $guard++) {
        while ($deg -gt 180) { $deg -= 360 }; while ($deg -le -180) { $deg += 360 }
        $tipY = $gy - $reach * [math]::Sin($deg * [math]::PI / 180)
        if ($tipY -ge 1 -and $tipY -le 77) { break }
        $target = if ([math]::Cos($deg * [math]::PI / 180) -ge 0) { 0 } elseif ($deg -gt 0) { 180 } else { -180 }
        $deg += [math]::Sign($target - $deg) * 2
    }
    $deg
}

for ($i = 0; $i -lt $files.Count; $i++) {
    $src = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $img = [System.Drawing.Bitmap]::new($src); $src.Dispose()
    $clean = [System.Drawing.Bitmap]::new($img)
    $body = New-Object 'bool[,]' 128, 80
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) { $body[$x, $y] = $img.GetPixel($x, $y).A -ge 128 } }
    function Put($x, $y, $color, [bool]$behind) {
        $px = [int][math]::Round($x); $py = [int][math]::Round($y)
        if ($px -lt 0 -or $py -lt 0 -or $px -ge 128 -or $py -ge 80) { return }
        if ($behind -and $body[$px, $py]) { return }
        $img.SetPixel($px, $py, $color)
    }

    $s = $specs[$i]
    $hx = [double]$s.hx; $hy = [double]$s.hy
    $reachNow = $Reach; $behind = $false
    if ($null -ne $s.theta) {
        $theta = [double]$s.theta
        $behind = [math]::Sin((Rad $theta)) -lt -0.2
        $tx = $s.cx + $Radius * [math]::Cos((Rad $theta)); $ty = $hy + $Flatness * [math]::Sin((Rad $theta))
        $dx = $tx - $hx; $dy = $ty - $hy
        $dist = [math]::Sqrt($dx * $dx + $dy * $dy)
        $reachNow = $Reach * [math]::Min(1, $dist / $NeedleLength)
        $deg = [math]::Atan2(-$dy, $dx) * 180 / [math]::PI
        if ($null -ne $s.smear) {
            $from = [double]$s.smear; $to = $theta
            while ($from -le $to) { $from += 360 }
            for ($t = $to; $t -le $from; $t += 1.2) {
                $far = [math]::Sin((Rad $t)) -lt -0.2
                $r = $Radius + ($Reach - $NeedleLength)
                Put ($s.cx + ($r - 1) * [math]::Cos((Rad $t))) ($hy + $Flatness * [math]::Sin((Rad $t))) $smearOuter $far
                if ([int]$t % 3 -eq 0) { Put ($s.cx + ($r - 4) * [math]::Cos((Rad $t))) ($hy + 1 + $Flatness * [math]::Sin((Rad $t))) $smearInner $far }
            }
        }
    }
    else { $deg = [double]$s.deg }
    $rawDeg = $deg
    $deg = Fit-Canvas $(if ($Trail) { $deg } else { Normal-Grip $deg }) $hy $reachNow

    if ($null -ne $s.arc) {
        $acx = [double]$s.arc[0]; $acy = [double]$s.arc[1]; $ar = [double]$s.arc[2] * $scale
        $from = [double]$s.arc[3]; $to = [double]$s.arc[4]
        $rising = $s.arc.Count -ge 6 -and [double]$s.arc[5] -gt 0
        if ($rising) { while ($to -lt $from) { $to += 360 }; $lo = $from; $hi = $to }
        else { while ($from -le $to) { $from += 360 }; $lo = $to; $hi = $from }
        # A few degrees of trail read as a stray scratch next to the sword, not as a swing.
        if ($hi - $lo -lt 30) { $lo = $hi + 1 }
        for ($t = $lo; $t -le $hi; $t += 0.6) {
            $a = Rad $t
            Put ($acx + $ar * [math]::Cos($a)) ($acy - $ar * [math]::Sin($a)) $smearOuter $true
            Put ($acx + ($ar - 1) * [math]::Cos($a)) ($acy - ($ar - 1) * [math]::Sin($a)) $smearOuter $true
            if ([int]$t % 3 -eq 0) { Put ($acx + ($ar - 4) * [math]::Cos($a)) ($acy - ($ar - 4) * [math]::Sin($a)) $smearInner $true }
        }
    }

    # A fist hidden behind the skirt or body (no skin within 4 px): the sword goes behind Alice.
    $near = 0
    for ($y = [int]($hy - 4); $y -le [int]($hy + 4); $y++) { for ($x = [int]($hx - 4); $x -le [int]($hx + 4); $x++) {
        if ($x -lt 0 -or $y -lt 0 -or $x -ge 128 -or $y -ge 80) { continue }
        if ((IsSkin $clean.GetPixel($x, $y)) -and [math]::Sqrt(($x - $hx) * ($x - $hx) + ($y - $hy) * ($y - $hy)) -le 4.01) { $near++ } } }
    # Hidden fist: keep the drawn angle, so the hilt stays inside her outline instead of sticking out like a hand.
    if ($near -eq 0) { $behind = $true; $deg = Fit-Canvas $rawDeg $hy $reachNow }

    $step = [int][math]::Round((($deg % 360) + 360) % 360 / (360.0 / $Steps)) % $Steps
    foreach ($p in $rots[$step]) {
        # Shortened (toward/away from the camera): drop the part of the blade beyond the shortened reach.
        $along = $p[0] * [math]::Cos((Rad $deg)) - $p[1] * [math]::Sin((Rad $deg))
        if ($along -gt $reachNow + 1) { continue }
        Put ($hx + $p[0]) ($hy + $p[1]) $p[2] $behind
    }

    # Fingers over the hilt; and the attachment check.
    $skin = 0
    for ($y = [int]($hy - 3); $y -le [int]($hy + 3); $y++) { for ($x = [int]($hx - 3); $x -le [int]($hx + 3); $x++) {
        if ($x -lt 0 -or $y -lt 0 -or $x -ge 128 -or $y -ge 80) { continue }
        $d = [math]::Sqrt(($x - $hx) * ($x - $hx) + ($y - $hy) * ($y - $hy))
        $c = $clean.GetPixel($x, $y)
        if ((IsSkin $c) -and $d -le 2.01) { $skin++ }
        if ($d -le $FistRadius -and (IsSkin $c) -and -not $behind) { $img.SetPixel($x, $y, $c) } } }
    if ($skin -eq 0 -and -not $behind) { "DETACHED {0} hand ({1},{2})" -f $files[$i].Name, $hx, $hy }

    $img.Save((Join-Path $OutDir $files[$i].Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $img.Dispose(); $clean.Dispose()
}
"{0}: {1} frames" -f (Split-Path $OutDir -Leaf), $files.Count
