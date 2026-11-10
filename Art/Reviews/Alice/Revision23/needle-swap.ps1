# Replaces the needle with the mirror-shard sword directly in finished frames, for animations whose needle was drawn
# inside a longer pipeline (cleanup.ps1, compositions) and cannot be redrawn from clean frames and a JSON.
# Per frame: the needle is the largest group of the three needle steels (8-connected); it is erased and every erased
# pixel takes the most common colour of its non-steel neighbours (or stays empty when most neighbours are empty),
# so the dress under the old one-pixel shaft closes up. The shaft's direction is its principal axis; the end that
# touches skin is the fist. The sword is pasted with its grip 3 px inside the fist along the shaft (where needle.ps1
# began the shaft), then the fist is drawn back over the hilt. A needle that touches no skin (stuck in the cushion
# while Alice rests) becomes a sword stuck the same way: its top end is the hilt, and the blade is cut where the
# needle's point was, so the cushion in front still hides the buried part.
# Usage: needle-swap.ps1 -Files "<dir or wildcard>" -OutDir <dir>
param(
    [Parameter(Mandatory)] [string]$Files,
    [Parameter(Mandatory)] [string]$OutDir,
    [string]$RotDir = '',
    [int]$Steps = 32,
    [double]$Reach = 33,
    [string]$StuckMatch = "",   # regex of frame names whose needle is stuck in something, not held
    [int]$StuckX = -1,          # a stuck sword stands still: its grip (hilt top) and visible length, when given
    [int]$StuckY = -1,
    [double]$StuckLength = 0,
    [string]$EraseMatch = "",   # regex of frame names where the needle is only erased: no sword (picking up the gift)
    [string]$BehindMatch = "",  # regex of frames whose fist is behind her head or body although skin is near (jump)
    [string]$CleanOut = "",     # with -SpecOut: also save each frame with the needle erased and no sword here
    [string]$SpecOut = "",      # JSON list of what was found per frame (grip, angle, behind, cut), for render-sword.ps1
    [double]$FistRadius = 2.5
)
Add-Type -AssemblyName System.Drawing
if (-not $RotDir) { $RotDir = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "shard\rot" }
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null
$steel = @{}
foreach ($c in @(@(38, 40, 48), @(132, 140, 152), @(214, 220, 226))) { $steel[[System.Drawing.Color]::FromArgb(255, $c[0], $c[1], $c[2]).ToArgb()] = 1 }

$rots = @()
for ($s = 0; $s -lt $Steps; $s++) {
    $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $RotDir ("sword_{0:D3}.png" -f [int](360.0 * $s / $Steps))))
    $c = [int]($bmp.Width / 2)
    $px = New-Object System.Collections.ArrayList
    for ($y = 0; $y -lt $bmp.Height; $y++) { for ($x = 0; $x -lt $bmp.Width; $x++) {
        $p = $bmp.GetPixel($x, $y)
        if ($p.A -ge 128) { [void]$px.Add(@(($x - $c), ($y - $c), $p)) } } }
    $bmp.Dispose()
    $rots += , $px
}
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

$specList = New-Object System.Collections.ArrayList
if ($CleanOut) { New-Item -ItemType Directory -Force $CleanOut | Out-Null }
$paths = if (Test-Path $Files -PathType Container) { Get-ChildItem $Files -Filter *.png } else { Get-ChildItem $Files }
foreach ($path in ($paths | Sort-Object Name)) {
    $src = [System.Drawing.Bitmap]::FromFile($path.FullName)
    $img = [System.Drawing.Bitmap]::new($src); $src.Dispose()
    $W = $img.Width; $H = $img.Height
    $isSteel = New-Object 'bool[,]' $W, $H
    for ($y = 0; $y -lt $H; $y++) { for ($x = 0; $x -lt $W; $x++) { $isSteel[$x, $y] = $steel.ContainsKey($img.GetPixel($x, $y).ToArgb()) } }

    # Largest 8-connected group of steel = the needle.
    $seen = New-Object 'bool[,]' $W, $H
    $best = $null
    for ($y = 0; $y -lt $H; $y++) { for ($x = 0; $x -lt $W; $x++) {
        if (-not $isSteel[$x, $y] -or $seen[$x, $y]) { continue }
        $group = New-Object System.Collections.ArrayList
        $stack = New-Object System.Collections.Stack
        $stack.Push(@($x, $y)); $seen[$x, $y] = $true
        while ($stack.Count -gt 0) {
            $p = $stack.Pop(); [void]$group.Add($p)
            for ($dy = -1; $dy -le 1; $dy++) { for ($dx = -1; $dx -le 1; $dx++) {
                $nx = $p[0] + $dx; $ny = $p[1] + $dy
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $W -and $ny -lt $H -and $isSteel[$nx, $ny] -and -not $seen[$nx, $ny]) { $seen[$nx, $ny] = $true; $stack.Push(@($nx, $ny)) } } }
        }
        if ($null -eq $best -or $group.Count -gt $best.Count) { $best = $group }
    } }
    if (($null -eq $best -or $best.Count -lt 6) -and $StuckX -ge 0 -and $path.Name -match $StuckMatch) {
        # Hidden needle in a stuck frame: the fixed stuck sword is still drawn (a stand-in line, nothing erased).
        $best = New-Object System.Collections.ArrayList; for ($k = 0; $k -le $StuckLength; $k++) { [void]$best.Add(@($StuckX, ($StuckY + $k))) }
    }
    if ($null -eq $best -or $best.Count -lt 6) { "NO NEEDLE {0}" -f $path.Name; if ($CleanOut) { $img.Save((Join-Path $CleanOut $path.Name)); [void]$specList.Add([ordered]@{ name = $path.Name; none = $true }) }; $img.Save((Join-Path $OutDir $path.Name)); $img.Dispose(); continue }

    # Principal axis.
    $mx = 0; $my = 0; foreach ($p in $best) { $mx += $p[0]; $my += $p[1] }; $mx /= $best.Count; $my /= $best.Count
    $sxx = 0; $syy = 0; $sxy = 0
    foreach ($p in $best) { $dx = $p[0] - $mx; $dy = $p[1] - $my; $sxx += $dx * $dx; $syy += $dy * $dy; $sxy += $dx * $dy }
    $ang = 0.5 * [math]::Atan2(2 * $sxy, $sxx - $syy)
    $ux = [math]::Cos($ang); $uy = [math]::Sin($ang)
    $lo = 1e9; $hi = -1e9
    foreach ($p in $best) { $t = ($p[0] - $mx) * $ux + ($p[1] - $my) * $uy; if ($t -lt $lo) { $lo = $t }; if ($t -gt $hi) { $hi = $t } }

    # Which end is the fist: the end with more skin around it; with no skin at all, the upper end.
    function SkinNear($cx, $cy) {
        $n = 0
        for ($dy = -2; $dy -le 2; $dy++) { for ($dx = -2; $dx -le 2; $dx++) {
            $x = [int][math]::Round($cx) + $dx; $y = [int][math]::Round($cy) + $dy
            if ($x -ge 0 -and $y -ge 0 -and $x -lt $W -and $y -lt $H -and (IsSkin $img.GetPixel($x, $y))) { $n++ } } }
        $n
    }
    $ax = $mx + $lo * $ux; $ay = $my + $lo * $uy; $bx = $mx + $hi * $ux; $by = $my + $hi * $uy
    $sa = SkinNear $ax $ay; $sb = SkinNear $bx $by
    $stuck = ($sa + $sb) -eq 0
    # needle.ps1 and cleanup.ps1 shade the shaft dark at the fist and mid-grey toward the point: that tells the ends
    # apart better than skin, since light hair passes for skin.
    $darkT = 0; $darkN = 0; $midT = 0; $midN = 0
    foreach ($p in $best) {
        $c = $img.GetPixel($p[0], $p[1]); $t = ($p[0] - $mx) * $ux + ($p[1] - $my) * $uy
        if ($c.R -eq 38) { $darkT += $t; $darkN++ } elseif ($c.R -eq 132) { $midT += $t; $midN++ }
    }
    if ($darkN -gt 0 -and $midN -gt 0) { $fistAtA = ($darkT / $darkN) -lt ($midT / $midN); $stuck = $false }
    elseif ($stuck) { $fistAtA = $ay -lt $by }
    else { $fistAtA = $sa -ge $sb }
    if ($StuckMatch -and $path.Name -match $StuckMatch) { $stuck = $true; $fistAtA = $ay -lt $by }
    if ($fistAtA) { $fx = $ax; $fy = $ay; $dirx = $ux; $diry = $uy } else { $fx = $bx; $fy = $by; $dirx = -$ux; $diry = -$uy }
    $length = $hi - $lo

    # Erase the needle; fill from neighbours, a few passes inward.
    $hole = New-Object 'bool[,]' $W, $H
    foreach ($p in $best) { if ($isSteel[$p[0], $p[1]]) { $hole[$p[0], $p[1]] = $true } }
    for ($pass = 0; $pass -lt 4; $pass++) {
        $fills = @()
        foreach ($p in $best) {
            if (-not $hole[$p[0], $p[1]]) { continue }
            $votes = @{}; $empty = 0; $known = 0
            foreach ($d in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1), @(-1, -1), @(1, 1), @(-1, 1), @(1, -1))) {
                $nx = $p[0] + $d[0]; $ny = $p[1] + $d[1]
                if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $W -or $ny -ge $H -or $hole[$nx, $ny]) { continue }
                $c = $img.GetPixel($nx, $ny); $known++
                if ($c.A -lt 128) { $empty++ } else { $k = $c.ToArgb(); $votes[$k] = 1 + $votes[$k] }
            }
            if ($known -lt 2 -and $pass -lt 3) { continue }
            $opaque = $known - $empty
            if ($empty -ge $opaque) { $fills += , @($p[0], $p[1], [System.Drawing.Color]::FromArgb(0, 0, 0, 0)) }
            else { $k = ($votes.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Name; $fills += , @($p[0], $p[1], [System.Drawing.Color]::FromArgb([int]$k)) }
        }
        foreach ($f in $fills) { $img.SetPixel($f[0], $f[1], $f[2]); $hole[$f[0], $f[1]] = $false }
    }
    $clean = [System.Drawing.Bitmap]::new($img)
    if ($EraseMatch -and $path.Name -match $EraseMatch) {
        "{0} erased, no sword" -f $path.Name
        if ($CleanOut) { $img.Save((Join-Path $CleanOut $path.Name)); [void]$specList.Add([ordered]@{ name = $path.Name; none = $true }) }
        $img.Save((Join-Path $OutDir $path.Name), [System.Drawing.Imaging.ImageFormat]::Png)
        $img.Dispose(); $clean.Dispose(); continue
    }

    # Sword: grip 3 px inside the fist (needle.ps1 started the shaft 3 px behind the hand point).
    $gx = $fx + 3 * $dirx; $gy = $fy + 3 * $diry
    if ($stuck) { $gx = $fx; $gy = $fy }
    if ($stuck -and $StuckX -ge 0) { $gx = $StuckX; $gy = $StuckY; $length = $StuckLength }
    # A fist with no skin near it is hidden behind the skirt or the body (falling): then the sword goes behind Alice
    # and only its blade shows past her outline, as the needle did. It keeps the needle's angle there: turned to the
    # ordinary hold, the hilt would stick out of her back like a stray hand.
    $skin = 0; $near = 0
    for ($y = [int]($gy - 4); $y -le [int]($gy + 4); $y++) { for ($x = [int]($gx - 4); $x -le [int]($gx + 4); $x++) {
        if ($x -lt 0 -or $y -lt 0 -or $x -ge $W -or $y -ge $H -or -not (IsSkin $clean.GetPixel($x, $y))) { continue }
        $d = [math]::Sqrt(($x - $gx) * ($x - $gx) + ($y - $gy) * ($y - $gy))
        if ($d -le 2.01) { $skin++ }
        if ($d -le 4.01) { $near++ } } }
    $behind = (-not $stuck -and $near -eq 0) -or ($stuck -and $StuckX -ge 0) -or ($BehindMatch -and $path.Name -match $BehindMatch)
    $deg = [math]::Atan2(-$diry, $dirx) * 180 / [math]::PI
    if (-not $stuck) {
        $held = if ($behind) { $deg } else { Normal-Grip $deg }
        $deg = Fit-Canvas $held $gy $Reach
        $dirx = [math]::Cos($deg * [math]::PI / 180); $diry = -[math]::Sin($deg * [math]::PI / 180)
        # The needle's shaft began right at her outline; the wrapped hilt reaches 7 px toward the point, so a hidden
        # fist holds the sword that much deeper behind her and only the blade comes out.
        if ($behind) { $gx -= 7 * $dirx; $gy -= 7 * $diry }
    }
    $step = [int][math]::Round((($deg % 360) + 360) % 360 / (360.0 / $Steps)) % $Steps
    $cut = if ($stuck) { $length + 1 } else { 1e9 }
    foreach ($p in $rots[$step]) {
        $along = $p[0] * $dirx + $p[1] * $diry
        if ($along -gt $cut) { continue }
        $x = [int][math]::Round($gx + $p[0]); $y = [int][math]::Round($gy + $p[1])
        if ($x -lt 0 -or $y -lt 0 -or $x -ge $W -or $y -ge $H) { continue }
        if ($behind -and $clean.GetPixel($x, $y).A -ge 128) { continue }
        $img.SetPixel($x, $y, $p[2])
    }
    if (-not $stuck -and -not $behind) {
        for ($y = [int]($gy - 3); $y -le [int]($gy + 3); $y++) { for ($x = [int]($gx - 3); $x -le [int]($gx + 3); $x++) {
            if ($x -lt 0 -or $y -lt 0 -or $x -ge $W -or $y -ge $H) { continue }
            $c = $clean.GetPixel($x, $y)
            $d = [math]::Sqrt(($x - $gx) * ($x - $gx) + ($y - $gy) * ($y - $gy))
            if ($d -le $FistRadius -and (IsSkin $c)) { $img.SetPixel($x, $y, $c) } } }
    }
    $note = if ($stuck) { 'stuck' } elseif ($behind) { 'behind' } elseif ($skin -eq 0) { 'DETACHED' } else { '' }
    "{0} {1:N0} deg grip ({2:N0},{3:N0}) len {5:N0} {4}" -f $path.Name, $deg, $gx, $gy, $note, $length
    if ($CleanOut) {
        $clean.Save((Join-Path $CleanOut $path.Name), [System.Drawing.Imaging.ImageFormat]::Png)
        $e = [ordered]@{ name = $path.Name; hx = [math]::Round($gx, 1); hy = [math]::Round($gy, 1); deg = [math]::Round($deg, 1) }
        if ($behind) { $e.behind = $true; $e.lock = $true }
        if ($stuck) { $e.cut = [math]::Round($cut, 1); $e.lock = $true }
        [void]$specList.Add($e)
    }
    $img.Save((Join-Path $OutDir $path.Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $img.Dispose(); $clean.Dispose()
}
if ($SpecOut) { ConvertTo-Json @($specList) -Depth 4 | Set-Content -Encoding UTF8 $SpecOut }
