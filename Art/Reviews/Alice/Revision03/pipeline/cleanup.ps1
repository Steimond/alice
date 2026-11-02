param(
    [string]$FramesDir,      # generated 64x64 frames, sorted by name
    [string]$PaletteFrom,    # sprite whose colours become the fixed palette
    [string]$Keypoints,      # JSON: one 18-joint skeleton per frame, fractions of 64
    [string]$OutDir,
    [int[]]$Airborne = @(3, 7),
    [int]$AirLift = 2,           # pixels the airborne frames rise above the contact frames
    [double]$NeedleAngle = 205,  # degrees, 0 = +x; 205 = trailing back and slightly down
    [double]$NeedleSwing = 0.6,  # share of the upper-arm swing passed on to the needle
    [int]$NeedleLength = 32
)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null

# Fixed palette: every opaque colour of the reference sprite plus three needle steels.
$pal = New-Object System.Collections.Generic.List[System.Drawing.Color]
$seen = @{}
$ref = [System.Drawing.Bitmap]::FromFile($PaletteFrom)
for ($y = 0; $y -lt $ref.Height; $y++) { for ($x = 0; $x -lt $ref.Width; $x++) {
    $p = $ref.GetPixel($x, $y)
    if ($p.A -ge 128 -and -not $seen.ContainsKey($p.ToArgb())) { $seen[$p.ToArgb()] = 1; $pal.Add([System.Drawing.Color]::FromArgb(255, $p.R, $p.G, $p.B)) }
} }
# Skin tones: the reference face, minus the dark eye and outline pixels.
$skin = @{}
for ($y = 15; $y -le 19; $y++) { for ($x = 33; $x -le 36; $x++) {
    $p = $ref.GetPixel($x, $y)
    if ($p.A -ge 128 -and ($p.R + $p.G + $p.B) -gt 360) { $skin[([System.Drawing.Color]::FromArgb(255, $p.R, $p.G, $p.B)).ToArgb()] = 1 }
} }
$ref.Dispose()
$steelDark = [System.Drawing.Color]::FromArgb(255, 38, 40, 48)
$steelMid = [System.Drawing.Color]::FromArgb(255, 132, 140, 152)
$steelLight = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
foreach ($c in $steelDark, $steelMid, $steelLight) { $pal.Add($c) }

$cache = @{}
function Nearest($c)
{
    $key = $c.ToArgb()
    if ($cache.ContainsKey($key)) { return $cache[$key] }
    $best = $null; $bd = [double]::MaxValue
    foreach ($q in $pal)
    {
        # Weighted RGB distance; good enough for snapping near-identical tones.
        $dr = $c.R - $q.R; $dg = $c.G - $q.G; $db = $c.B - $q.B
        $d = 2 * $dr * $dr + 4 * $dg * $dg + 3 * $db * $db
        if ($d -lt $bd) { $bd = $d; $best = $q }
    }
    $cache[$key] = $best
    return $best
}

$kp = Get-Content $Keypoints -Raw | ConvertFrom-Json
$files = Get-ChildItem $FramesDir -Filter *.png | Sort-Object Name

# Ground: contact frames get their lowest pixel on the stance ground row (59); airborne frames keep the average shift.
$bottoms = @()
foreach ($f in $files)
{
    $b = [System.Drawing.Bitmap]::FromFile($f.FullName); $low = -1
    for ($y = 0; $y -lt $b.Height; $y++) { for ($x = 0; $x -lt $b.Width; $x++) { if ($b.GetPixel($x, $y).A -ge 128) { $low = $y } } }
    $bottoms += $low; $b.Dispose()
}
$shifts = @()
for ($i = 0; $i -lt $files.Count; $i++) { $shifts += if ($Airborne -contains $i) { $null } else { 59 - $bottoms[$i] } }
$contact = @($shifts | Where-Object { $_ -ne $null })
$avg = [math]::Round(($contact | Measure-Object -Average).Average)

# 128x80 canvas, pivot bottom centre: stance ground row 59 -> row 79, hip centre -> column 64.
$hipX = 0.44 * 64
$ox = [int][math]::Round(64 - $hipX); $oy = 20

$report = @()
for ($i = 0; $i -lt $files.Count; $i++)
{
    $src = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $dy = if ($Airborne -contains $i) { $avg - $AirLift } else { $shifts[$i] }
    $out = New-Object System.Drawing.Bitmap 128, 80, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt 64; $y++) { for ($x = 0; $x -lt 64; $x++) {
        $p = $src.GetPixel($x, $y)
        if ($p.A -lt 128) { continue }
        $tx = $x + $ox; $ty = $y + $dy + $oy
        if ($tx -ge 0 -and $tx -lt 128 -and $ty -ge 0 -and $ty -lt 80) { $out.SetPixel($tx, $ty, (Nearest $p)) }
    } }
    $src.Dispose()

    # Needle on the near hand. The generator puts the hand only roughly where the skeleton asked,
    # so snap to the nearest skin-coloured pixel around the requested wrist.
    $hand = $kp[$i] | Where-Object { $_.label -eq 'RIGHT ARM' }
    $hx = $hand.x * 64 + $ox; $hy = $hand.y * 64 + $dy + $oy
    $bestD = [double]::MaxValue; $snap = $null
    for ($y = [int]$hy - 6; $y -le [int]$hy + 6; $y++) { for ($x = [int]$hx - 6; $x -le [int]$hx + 6; $x++) {
        if ($x -lt 0 -or $y -lt 0 -or $x -ge 128 -or $y -ge 80) { continue }
        if ($skin.ContainsKey($out.GetPixel($x, $y).ToArgb()))
        {
            $d = ($x - $hx) * ($x - $hx) + ($y - $hy) * ($y - $hy)
            if ($d -lt $bestD) { $bestD = $d; $snap = @($x, $y) }
        }
    } }
    if ($snap) { $hx = $snap[0]; $hy = $snap[1] }
    # The fist turns with the arm: a forward swing tips the trailing point down, a back swing lifts it.
    $sh = $kp[$i] | Where-Object { $_.label -eq 'RIGHT SHOULDER' }
    $el = $kp[$i] | Where-Object { $_.label -eq 'RIGHT ELBOW' }
    $swing = [math]::Atan2(($el.x - $sh.x), ($el.y - $sh.y)) * 180 / [math]::PI
    $deg = $NeedleAngle + $NeedleSwing * $swing
    # Keep the point above the ground row.
    while ($hy - ($NeedleLength - 4) * [math]::Sin($deg * [math]::PI / 180) -gt 78 -and $deg -gt 180) { $deg -= 2 }
    $a = $deg * [math]::PI / 180
    $ux = [math]::Cos($a); $uy = - [math]::Sin($a)
    $sx = $hx - 4 * $ux; $sy = $hy - 4 * $uy
    $drawn = @{}
    for ($t = 0; $t -le $NeedleLength; $t += 0.5)
    {
        $px = [int][math]::Round($sx + $t * $ux); $py = [int][math]::Round($sy + $t * $uy)
        $key = "$px,$py"
        if ($drawn.ContainsKey($key) -or $px -lt 0 -or $px -ge 128 -or $py -lt 0 -or $py -ge 80) { continue }
        $drawn[$key] = 1
        # A one-pixel shaft: a needle, not a blade. Highlight in the middle, duller toward the point.
        $col = if ($t -lt 3) { $steelDark } elseif ($t -gt $NeedleLength - 8) { $steelMid } else { $steelLight }
        # Over the ivory apron a light shaft disappears; drop it one or two steps darker there.
        $under = $out.GetPixel($px, $py)
        if ($under.A -ge 128 -and (0.3 * $under.R + 0.59 * $under.G + 0.11 * $under.B) -gt 150) { $col = if ($col -eq $steelLight) { $steelMid } else { $steelDark } }
        $out.SetPixel($px, $py, $col)
    }
    # Eye: a one-pixel hole outlined in dark steel at the blunt end.
    $ex = [int][math]::Round($sx + 1 * $ux); $ey = [int][math]::Round($sy + 1 * $uy)
    foreach ($d in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1))) { $qx = $ex + $d[0]; $qy = $ey + $d[1]; if ($qx -ge 0 -and $qx -lt 128 -and $qy -ge 0 -and $qy -lt 80) { $out.SetPixel($qx, $qy, $steelDark) } }
    $out.SetPixel($ex, $ey, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))

    $name = Join-Path $OutDir ("run-{0:D2}.png" -f $i)
    $out.Save($name, [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    $report += [pscustomobject]@{ frame = $i; rawBottom = $bottoms[$i]; shift = $dy; handX = [math]::Round($hx, 1); handY = [math]::Round($hy, 1); needleDeg = [math]::Round($deg) }
}
$report | Format-Table -AutoSize
"palette: $($pal.Count) colours (reference + 3 steel)"
