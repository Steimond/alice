# Draws the mirror-shard sword into every Alice game frame from one list (frames.json, made by extract-frames.ps1)
# plus hand-made corrections (overrides.json), so any single frame can be fixed without touching the rest.
#
# frames.json: [{ name, src, hx, hy, deg | theta+cx(+smear), arc?, behind?, cut?, none?, lock? }]
#   name    game frame (alice_trial_*.png), written to -GameDir
#   src     clean frame without any weapon
#   hx, hy  where the fist closes on the hilt
#   deg     blade direction (0 = forward, counter-clockwise); theta/cx/smear and arc as in needle.ps1
#   behind  the fist is hidden: the sword is drawn only outside Alice
#   cut     only this much of the blade from the grip is drawn (stuck in the cushion)
#   none    no sword in this frame
#   lock    the angle is used as given: no grip check
# overrides.json: { "<name>": { any of the fields above } } - replaces those fields for that frame.
#
# Grip (owner, 10 Nov 2026: no reverse grip anywhere). Calm angles left from the needle, back and down, turn forward
# and down. The fist's skin blob gives the forearm direction (elbow -> fist; the sleeves are short); in an ordinary
# grip the blade leaves the fist on the thumb side, between -35 and +150 degrees to the forearm. Frames outside are
# flagged in grip-report.csv for a look by eye and a correction in overrides.json.
param(
    [string]$Frames = '',
    [string]$Overrides = '',
    [string]$GameDir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial",
    [string]$Report = '',
    [string]$Only = '',          # regex: render only these frame names (for trying a correction)
    [string]$OutDir = '',        # write here instead of the game folder
    # Clips that are no longer drawn here: the turning combo is rendered by Revision24/render-combo.ps1.
    [string]$Skip = '^alice_trial_(slash|cut|finisher)_\d+\.png$',
    [int]$Steps = 32,
    [double]$Reach = 33,
    [double]$Radius = 55,
    [double]$Flatness = 4,
    [double]$NeedleLength = 32,
    [double]$FistRadius = 2.5,
    # Frames where the hand only holds the sword: the stance, its fidget, running, landing, stepping off, waking, the
    # salts, and the stances that open and close the executions.
    [string]$CalmMatch = '^alice_trial_(idle|fidget|run|land|stepoff|jump|wake|salts)_\d+\.png$|^alice_trial_exec_\w+_(00|1[2-5])\.png$'
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Frames) { $Frames = Join-Path $here 'frames.json' }
if (-not $Overrides) { $Overrides = Join-Path $here 'overrides.json' }
if (-not $Report) { $Report = Join-Path $here 'grip-report.csv' }
$target = if ($OutDir) { $OutDir } else { $GameDir }
New-Item -ItemType Directory -Force $target | Out-Null

$smearOuter = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
$smearInner = [System.Drawing.Color]::FromArgb(255, 150, 160, 176)
$scale = $Reach / $NeedleLength
function Load-Rots([string]$dir) {
    $set = @()
    for ($s = 0; $s -lt $Steps; $s++) {
        $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $here ("shard\{0}\sword_{1:D3}.png" -f $dir, [int](360.0 * $s / $Steps))))
        $c = [int]($bmp.Width / 2)
        $px = New-Object System.Collections.ArrayList
        for ($y = 0; $y -lt $bmp.Height; $y++) { for ($x = 0; $x -lt $bmp.Width; $x++) {
            $p = $bmp.GetPixel($x, $y)
            if ($p.A -ge 128) { [void]$px.Add(@(($x - $c), ($y - $c), $p)) } } }
        $bmp.Dispose()
        $set += , $px
    }
    , $set
}
# Foreshortened sets (sword-foreshorten.ps1): a blade turned toward or away from the viewer, field "len" (0..1).
$rotSets = [ordered]@{ '100' = (Load-Rots 'rot') }
foreach ($pc in 70, 45, 25) { if (Test-Path (Join-Path $here "shard\rot-f$pc")) { $rotSets["$pc"] = Load-Rots "rot-f$pc" } }
function Pick-Set([double]$len) {
    $best = '100'; $bd = 9
    foreach ($k in $rotSets.Keys) { $d = [math]::Abs([double]$k / 100 - $len); if ($d -lt $bd) { $bd = $d; $best = $k } }
    $best
}

function Rad($deg) { $deg * [math]::PI / 180 }
function Norm($deg) { while ($deg -gt 180) { $deg -= 360 }; while ($deg -le -180) { $deg += 360 }; $deg }
function IsSkin([System.Drawing.Color]$c) { $c.A -ge 128 -and ($c.R - $c.B) -gt 55 -and $c.R -gt 170 -and $c.G -gt 120 }
function IsSkinLoose([System.Drawing.Color]$c) { $c.A -ge 128 -and ($c.R - $c.B) -gt 50 -and $c.R -gt 140 -and $c.G -gt 95 }
function Fit-Canvas([double]$deg, [double]$gy, [double]$reach) {
    for ($guard = 0; $guard -lt 45; $guard++) {
        $deg = Norm $deg
        $tipY = $gy - $reach * [math]::Sin((Rad $deg))
        if ($tipY -ge 1 -and $tipY -le 77) { break }
        $t = if ([math]::Cos((Rad $deg)) -ge 0) { 0 } elseif ($deg -gt 0) { 180 } else { -180 }
        $deg += [math]::Sign($t - $deg) * 2
    }
    $deg
}
# Forearm direction from the skin blob at the fist, limited to 14 px round it; $null when the fist shows too little.
function Forearm([System.Drawing.Bitmap]$b, [double]$hx, [double]$hy) {
    $sx = -1; $sy = -1; $bd = 99
    for ($y = [int]$hy - 3; $y -le [int]$hy + 3; $y++) { for ($x = [int]$hx - 3; $x -le [int]$hx + 3; $x++) {
        if ($x -lt 0 -or $y -lt 0 -or $x -ge 128 -or $y -ge 80 -or -not (IsSkinLoose $b.GetPixel($x, $y))) { continue }
        $d = [math]::Abs($x - $hx) + [math]::Abs($y - $hy); if ($d -lt $bd) { $bd = $d; $sx = $x; $sy = $y } } }
    if ($sx -lt 0) { return $null }
    $seen = @{}; $st = New-Object System.Collections.Stack; $st.Push(@($sx, $sy)); $seen["$sx,$sy"] = 1
    $far = 0; $ex = $sx; $ey = $sy; $n = 0
    while ($st.Count) {
        $p = $st.Pop(); $n++
        $d = [math]::Sqrt(($p[0] - $hx) * ($p[0] - $hx) + ($p[1] - $hy) * ($p[1] - $hy))
        if ($d -gt $far) { $far = $d; $ex = $p[0]; $ey = $p[1] }
        foreach ($o in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1), @(1, 1), @(-1, -1), @(1, -1), @(-1, 1))) {
            $nx = $p[0] + $o[0]; $ny = $p[1] + $o[1]
            if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge 128 -or $ny -ge 80 -or $seen.ContainsKey("$nx,$ny")) { continue }
            if ([math]::Sqrt(($nx - $hx) * ($nx - $hx) + ($ny - $hy) * ($ny - $hy)) -gt 14) { continue }
            if (-not (IsSkinLoose $b.GetPixel($nx, $ny))) { continue }
            $seen["$nx,$ny"] = 1; $st.Push(@($nx, $ny)) } }
    if ($far -lt 5) { return $null }
    [math]::Atan2(-($hy - $ey), $hx - $ex) * 180 / [math]::PI
}

$list = Get-Content $Frames -Raw -Encoding UTF8 | ConvertFrom-Json
$over = @{}
if (Test-Path $Overrides) {
    $o = Get-Content $Overrides -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($p in $o.PSObject.Properties) { if ($p.Name -notlike '_*') { $over[$p.Name] = $p.Value } }
}
$rows = New-Object System.Collections.ArrayList
foreach ($entry in $list) {
    if ($Only -and $entry.name -notmatch $Only) { continue }
    if ($Skip -and $entry.name -match $Skip) { continue }
    $s = [ordered]@{}
    foreach ($p in $entry.PSObject.Properties) { $s[$p.Name] = $p.Value }
    $fixedBy = ''
    if ($over.ContainsKey($entry.name)) {
        foreach ($p in $over[$entry.name].PSObject.Properties) { $s[$p.Name] = $p.Value }
        # A correction that sets an angle replaces a theta turn of the generated entry.
        if ($null -ne $over[$entry.name].deg) { $s.Remove('theta'); $s.Remove('smear') }
        $fixedBy = 'override'
    }
    $src = [System.Drawing.Bitmap]::FromFile($s.src)
    $img = [System.Drawing.Bitmap]::new($src); $src.Dispose()
    $clean = [System.Drawing.Bitmap]::new($img)
    if ($s.none) {
        $img.Save((Join-Path $target $s.name), [System.Drawing.Imaging.ImageFormat]::Png); $img.Dispose(); $clean.Dispose()
        [void]$rows.Add(('{0},,,,,,,none,{1}' -f $s.name, $fixedBy)); continue
    }
    $body = New-Object 'bool[,]' 128, 80
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) { $body[$x, $y] = $img.GetPixel($x, $y).A -ge 128 } }
    function Put($x, $y, $color, [bool]$hide) {
        $px = [int][math]::Round($x); $py = [int][math]::Round($y)
        if ($px -lt 0 -or $py -lt 0 -or $px -ge 128 -or $py -ge 80) { return }
        if ($hide -and $body[$px, $py]) { return }
        $img.SetPixel($px, $py, $color)
    }
    # Whole pixels: a .5 grip with banker's rounding would merge every other row of the blade into stripes.
    $hx = [math]::Floor([double]$s.hx + 0.5); $hy = [math]::Floor([double]$s.hy + 0.5)
    $reachNow = $Reach; $behind = [bool]$s.behind; $lock = [bool]$s.lock
    if ($null -ne $s.theta) {
        $theta = [double]$s.theta; $lock = $true
        if ([math]::Sin((Rad $theta)) -lt -0.2) { $behind = $true }
        $tx = $s.cx + $Radius * [math]::Cos((Rad $theta)); $ty = $hy + $Flatness * [math]::Sin((Rad $theta))
        $dx = $tx - $hx; $dy = $ty - $hy
        $reachNow = $Reach * [math]::Min(1, [math]::Sqrt($dx * $dx + $dy * $dy) / $NeedleLength)
        $deg = [math]::Atan2(-$dy, $dx) * 180 / [math]::PI
        if ($null -ne $s.smear) {
            $from = [double]$s.smear; $to = $theta
            while ($from -le $to) { $from += 360 }
            $r = $Radius + ($Reach - $NeedleLength)
            for ($t = $to; $t -le $from; $t += 1.2) {
                $farSide = [math]::Sin((Rad $t)) -lt -0.2
                Put ($s.cx + ($r - 1) * [math]::Cos((Rad $t))) ($hy + $Flatness * [math]::Sin((Rad $t))) $smearOuter $farSide
                if ([int]$t % 3 -eq 0) { Put ($s.cx + ($r - 4) * [math]::Cos((Rad $t))) ($hy + 1 + $Flatness * [math]::Sin((Rad $t))) $smearInner $farSide }
            }
        }
    }
    else { $deg = [double]$s.deg }
    $deg = Norm $deg
    $degIn = $deg
    $arm = Forearm $clean $hx $hy
    $rel = $null
    if ($null -ne $arm) { $rel = Norm ($deg - $arm) }
    if (-not $lock -and -not $behind) {
        # The needle hung back and down (about 205-255 degrees): a sword held so reads as a knife in a reverse grip.
        # It turns forward and down, the ordinary hold; wind-ups drawn back (below 203 degrees) stay.
        $d = (($deg % 360) + 360) % 360
        if ($d -ge 203 -and $d -le 255) { $deg = Norm (540 - $d); $fixedBy = if ($fixedBy) { "$fixedBy+carry" } else { 'carry' } }
    }
    # A calm hand holds the blade forward and a little up, about 15 degrees (owner, 10 Nov 2026: hanging forward and
    # down it read as too slack); the swing of a run keeps half its range round that angle.
    if (-not $behind -and $s.name -match $CalmMatch -and $deg -ge -80 -and $deg -le -5) {
        $deg = 15 + ($deg + 36) * 0.5
        $fixedBy = if ($fixedBy) { "$fixedBy+calm" } else { 'calm' }
    }
    # The forearm check only flags: its blob reading is not sure enough to turn blades by itself (it lifted calm
    # blades in the stance), so flagged frames are judged by eye and corrected in overrides.json.
    $relOut = if ($null -ne $arm) { Norm ($deg - $arm) } else { $null }
    if (-not $lock -and -not $behind -and $null -ne $relOut -and ($relOut -lt -35 -or $relOut -gt 150)) {
        $fixedBy = if ($fixedBy) { "$fixedBy+suspect" } else { 'suspect' }
    }
    # A stuck sword stands upright in the cushion, which hides its end: it is not turned to clear the floor.
    $cut = if ($null -ne $s.cut) { [double]$s.cut } else { 1e9 }
    if ($null -eq $s.cut) { $deg = Fit-Canvas $deg $hy $reachNow }

    if ($null -ne $s.arc) {
        $acx = [double]$s.arc[0]; $acy = [double]$s.arc[1]; $ar = [double]$s.arc[2] * $scale
        $from = [double]$s.arc[3]; $to = [double]$s.arc[4]
        $rising = $s.arc.Count -ge 6 -and [double]$s.arc[5] -gt 0
        if ($rising) { while ($to -lt $from) { $to += 360 }; $lo = $from; $hi = $to }
        else { while ($from -le $to) { $from += 360 }; $lo = $to; $hi = $from }
        if ($hi - $lo -ge 30) {
            for ($t = $lo; $t -le $hi; $t += 0.6) {
                $a = Rad $t
                Put ($acx + $ar * [math]::Cos($a)) ($acy - $ar * [math]::Sin($a)) $smearOuter $true
                Put ($acx + ($ar - 1) * [math]::Cos($a)) ($acy - ($ar - 1) * [math]::Sin($a)) $smearOuter $true
                if ([int]$t % 3 -eq 0) { Put ($acx + ($ar - 4) * [math]::Cos($a)) ($acy - ($ar - 4) * [math]::Sin($a)) $smearInner $true }
            }
        }
    }

    # A swing round the body: a tilted ellipse (cx, cy, rx, ry, tilt, from, to[, far]), drawn from "from" to "to" in
    # its own frame (t = 0 along the tilt, 90 above it). A circle seen a little from above: its upper half (sin t > 0)
    # lies beyond Alice and is hidden by her body, the lower half passes in front of her; far = -1 swaps the halves
    # (a blade brought over the top toward the viewer).
    if ($null -ne $s.ell) {
        $e = @($s.ell | ForEach-Object { [double]$_ })
        $from = $e[5]; $to = $e[6]
        $farSign = if ($e.Count -ge 8) { $e[7] } else { 1 }
        if ($to -lt $from) { $tmp = $from; $from = $to; $to = $tmp }
        $ct = [math]::Cos((Rad $e[4])); $st = [math]::Sin((Rad $e[4]))
        for ($t = $from; $t -le $to; $t += 0.8) {
            $far = [math]::Sin((Rad $t)) * $farSign -gt 0
            foreach ($ring in @(@(0, $smearOuter), @(1, $smearOuter), @(4, $smearInner))) {
                if ($ring[0] -eq 4 -and [int]$t % 3 -ne 0) { continue }
                $lx = ($e[2] - $ring[0]) * [math]::Cos((Rad $t)); $ly = ([math]::Max(1, $e[3] - $ring[0] * $e[3] / $e[2])) * [math]::Sin((Rad $t))
                Put ($e[0] + $lx * $ct - $ly * $st) ($e[1] - ($lx * $st + $ly * $ct)) $ring[1] $far
            }
        }
    }

    $len = if ($null -ne $s.len) { [double]$s.len } else { 1 }
    $setKey = Pick-Set $len
    $rots = $rotSets[$setKey]
    $reachNow = $reachNow * [double]$setKey / 100
    $step = [int][math]::Round((($deg % 360) + 360) % 360 / (360.0 / $Steps)) % $Steps
    foreach ($p in $rots[$step]) {
        $along = $p[0] * [math]::Cos((Rad $deg)) - $p[1] * [math]::Sin((Rad $deg))
        if ($along -gt [math]::Min($reachNow + 1, $cut)) { continue }
        Put ($hx + $p[0]) ($hy + $p[1]) $p[2] $behind
    }
    $skin = 0
    for ($y = [int]($hy - 3); $y -le [int]($hy + 3); $y++) { for ($x = [int]($hx - 3); $x -le [int]($hx + 3); $x++) {
        if ($x -lt 0 -or $y -lt 0 -or $x -ge 128 -or $y -ge 80) { continue }
        $d = [math]::Sqrt(($x - $hx) * ($x - $hx) + ($y - $hy) * ($y - $hy))
        $c = $clean.GetPixel($x, $y)
        if ((IsSkin $c) -and $d -le 2.01) { $skin++ }
        if ($d -le $FistRadius -and (IsSkin $c) -and -not $behind) { $img.SetPixel($x, $y, $c) } } }
    $state = if ($behind) { 'behind' } elseif ($skin -eq 0) { 'DETACHED' } else { '' }
    [void]$rows.Add(('{0},{1},{2},{3:N0},{4},{5},{6:N0},{7},{8}' -f $s.name, $hx, $hy, $degIn,
        $(if ($null -ne $arm) { [math]::Round($arm) } else { '' }), $(if ($null -ne $rel) { [math]::Round($rel) } else { '' }), $deg, $state, $fixedBy))
    $img.Save((Join-Path $target $s.name), [System.Drawing.Imaging.ImageFormat]::Png)
    $img.Dispose(); $clean.Dispose()
}
if (-not $Only) {
    @('name,hx,hy,deg_in,forearm,rel,deg_out,state,fixed') + $rows | Set-Content -Encoding UTF8 $Report
}
"{0} frames rendered, {1} flagged by the forearm check, {2} overrides" -f $rows.Count, @($rows | Where-Object { $_ -match 'suspect' }).Count, $over.Count
