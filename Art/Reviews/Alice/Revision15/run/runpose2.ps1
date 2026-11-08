# Run cycle skeletons for animate_with_skeleton_v3, second take (8 Nov 2026).
# The first take (Revision03/pipeline/runpose.ps1) dropped the body until the lower foot touched the ground,
# so the head height followed the leg angles and jumped 3-4 px between frames. Here the pelvis follows an
# explicit smooth bob and the legs reach their feet by two-bone IK:
#   - a planted foot slides back exactly one game step (RunFrameDistance 0.4375 u = 7 px) per frame,
#     so the drawing does not skate against the floor;
#   - the swinging foot follows a smooth loop behind and under the body;
#   - the arms swing against the legs on a sine, so they never jump either.
param(
    [string]$Skeleton = "C:\Projects\alice\Art\Reviews\Alice\Revision03\pipeline\stand-skeleton.json",
    [string]$OutJson = "run2-keypoints.json",
    [string]$OutPng = "run2-skeleton-preview.png",
    [double]$Lean = 12,
    [double]$Step = 7
)
Add-Type -AssemblyName System.Drawing
$N = 64.0
$sk = Get-Content $Skeleton -Raw | ConvertFrom-Json
$k = @{}
foreach ($p in $sk.keypoints) { $k[$p.label] = @{ x = $p.x * $N; y = $p.y * $N } }

function Dist($a, $b) { [math]::Sqrt(($a.x - $b.x) * ($a.x - $b.x) + ($a.y - $b.y) * ($a.y - $b.y)) }
function Rad($d) { $d * [math]::PI / 180 }
# Angle from straight down; positive swings toward +x (forward when facing east).
function Ray($from, $len, $deg) { @{ x = $from.x + $len * [math]::Sin((Rad $deg)); y = $from.y + $len * [math]::Cos((Rad $deg)) } }

$thigh = ((Dist $k['RIGHT HIP'] $k['RIGHT KNEE']) + (Dist $k['LEFT HIP'] $k['LEFT KNEE'])) / 2
$shin = ((Dist $k['RIGHT KNEE'] $k['RIGHT LEG']) + (Dist $k['LEFT KNEE'] $k['LEFT LEG'])) / 2
$upper = ((Dist $k['RIGHT SHOULDER'] $k['RIGHT ELBOW']) + (Dist $k['LEFT SHOULDER'] $k['LEFT ELBOW'])) / 2
$fore = ((Dist $k['RIGHT ELBOW'] $k['RIGHT ARM']) + (Dist $k['LEFT ELBOW'] $k['LEFT ARM'])) / 2
$hipC = @{ x = ($k['RIGHT HIP'].x + $k['LEFT HIP'].x) / 2; y = ($k['RIGHT HIP'].y + $k['LEFT HIP'].y) / 2 }
$torso = Dist $hipC $k['NECK']
$ground = [math]::Max($k['RIGHT LEG'].y, $k['LEFT LEG'].y)
$headLabels = 'NOSE', 'LEFT EYE', 'RIGHT EYE', 'LEFT EAR', 'RIGHT EAR'

# One leg's phase table: ankle x relative to the hip and height above the ground (pixels).
# 0 contact, 1 down, 2 passing, 3 toe-off (both feet up), 4-7 swing through. The other leg is 4 frames behind.
$footX = 9, (9 - $Step), (9 - 2 * $Step), -12, -14, -7, 3, 10
$footH = 0, 0, 0, 2, 7, 10, 7, 3
# Pelvis bob (down positive): contact mid, down lowest, passing mid, flight highest.
$bob = 0, 1, 0, -1.5, 0, 1, 0, -1.5
# Arm swing on a sine against the same-side leg; the near arm is back when the near foot plants.
$armAmp = 38

# Crouch: low enough that the leg reaches the contact foot with a little bend left.
$reach = 0.96 * ($thigh + $shin)
$drop = 0
for ($p = 0; $p -lt 3; $p++)
{
    $v = [math]::Sqrt([math]::Max(0, $reach * $reach - $footX[$p] * $footX[$p]))
    $need = ($ground - $hipC.y) - $v - $bob[$p]
    if ($need -gt $drop) { $drop = $need }
}

function Leg($hip, $foot)
{
    $dx = $foot.x - $hip.x; $dy = $foot.y - $hip.y
    $d = [math]::Sqrt($dx * $dx + $dy * $dy)
    $ux = $dx / $d; $uy = $dy / $d
    if ($d -ge $thigh + $shin - 0.01)
    {
        $knee = @{ x = $hip.x + $ux * $thigh; y = $hip.y + $uy * $thigh }
        return @{ knee = $knee; ankle = @{ x = $hip.x + $ux * ($thigh + $shin); y = $hip.y + $uy * ($thigh + $shin) } }
    }
    $a = [math]::Acos(($thigh * $thigh + $d * $d - $shin * $shin) / (2 * $thigh * $d))
    # Two candidates; the knee always bends forward (+x).
    $best = $null
    foreach ($s in 1, -1)
    {
        $c = [math]::Cos($s * $a); $sn = [math]::Sin($s * $a)
        $kx = $hip.x + $thigh * ($ux * $c - $uy * $sn); $ky = $hip.y + $thigh * ($ux * $sn + $uy * $c)
        if ($best -eq $null -or $kx -gt $best.x) { $best = @{ x = $kx; y = $ky } }
    }
    return @{ knee = $best; ankle = $foot }
}

$frames = @()
$report = @()
for ($f = 0; $f -lt 8; $f++)
{
    $j = @{}
    $hip = @{ x = $hipC.x; y = $hipC.y + $drop + $bob[$f] }
    foreach ($side in 'RIGHT', 'LEFT')
    {
        $p = if ($side -eq 'RIGHT') { $f } else { ($f + 4) % 8 }
        $h = @{ x = $hip.x + ($k["$side HIP"].x - $hipC.x); y = $hip.y + ($k["$side HIP"].y - $hipC.y) }
        $foot = @{ x = $hipC.x + $footX[$p]; y = $ground - $footH[$p] }
        $leg = Leg $h $foot
        $j["$side HIP"] = $h; $j["$side KNEE"] = $leg.knee; $j["$side LEG"] = $leg.ankle
    }
    $neck = Ray $hip $torso (180 - $Lean)
    $j['NECK'] = $neck
    foreach ($h in $headLabels) { $j[$h] = @{ x = $neck.x + ($k[$h].x - $k['NECK'].x); y = $neck.y + ($k[$h].y - $k['NECK'].y) } }
    foreach ($side in 'RIGHT', 'LEFT')
    {
        $p = if ($side -eq 'RIGHT') { $f } else { ($f + 4) % 8 }
        $sh = @{ x = $neck.x + ($k["$side SHOULDER"].x - $k['NECK'].x); y = $neck.y + ($k["$side SHOULDER"].y - $k['NECK'].y) }
        # Phase 0 (own foot planted ahead) -> arm swung back; phase 4 -> forward.
        $ua = - $armAmp * [math]::Cos((Rad ($p * 45)))
        $elbow = Ray $sh $upper $ua
        $hand = Ray $elbow $fore ($ua + 80)
        $j["$side SHOULDER"] = $sh; $j["$side ELBOW"] = $elbow; $j["$side ARM"] = $hand
    }
    $list = @()
    foreach ($label in $j.Keys)
    {
        $z = if ($label -like 'RIGHT*') { 2 } elseif ($label -like 'LEFT*') { 0 } else { 1 }
        if ($headLabels -contains $label) { $z = 1 }
        $list += [ordered]@{ label = $label; x = [math]::Round($j[$label].x / $N, 4); y = [math]::Round($j[$label].y / $N, 4); z_index = $z }
    }
    $frames += , $list
    $report += "f{0}: nose y {1:N2}  R ankle ({2:N1},{3:N1})  L ankle ({4:N1},{5:N1})" -f $f, $j['NOSE'].y, $j['RIGHT LEG'].x, $j['RIGHT LEG'].y, $j['LEFT LEG'].x, $j['LEFT LEG'].y
}
ConvertTo-Json -InputObject $frames -Depth 5 -Compress | Out-File -Encoding ascii $OutJson

# Stick-figure preview at 6x: near (right) limbs orange, far (left) limbs blue; ground and stance head lines.
$S = 6
$bmp = New-Object System.Drawing.Bitmap (64 * $S * 8 + 70), (64 * $S)
$g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::FromArgb(40, 40, 44)); $g.SmoothingMode = 'AntiAlias'
$bones = @(('NECK', 'RIGHT HIP'), ('NECK', 'LEFT HIP'), ('RIGHT HIP', 'RIGHT KNEE'), ('RIGHT KNEE', 'RIGHT LEG'), ('LEFT HIP', 'LEFT KNEE'), ('LEFT KNEE', 'LEFT LEG'),
    ('RIGHT SHOULDER', 'RIGHT ELBOW'), ('RIGHT ELBOW', 'RIGHT ARM'), ('LEFT SHOULDER', 'LEFT ELBOW'), ('LEFT ELBOW', 'LEFT ARM'), ('NECK', 'NOSE'))
for ($f = 0; $f -lt 8; $f++)
{
    $m = @{}; foreach ($p in $frames[$f]) { $m[$p.label] = $p }
    $ox = $f * (64 * $S + 10)
    $g.DrawLine([System.Drawing.Pens]::Gray, $ox, $ground * $S, $ox + 64 * $S, $ground * $S)
    $g.DrawLine([System.Drawing.Pens]::DimGray, $ox, $k['NOSE'].y * $S, $ox + 64 * $S, $k['NOSE'].y * $S)
    foreach ($b in $bones)
    {
        $col = if ($b[0] -like 'RIGHT*' -or $b[1] -like 'RIGHT*') { [System.Drawing.Color]::Orange } elseif ($b[0] -like 'LEFT*' -or $b[1] -like 'LEFT*') { [System.Drawing.Color]::DeepSkyBlue } else { [System.Drawing.Color]::White }
        $pen = New-Object System.Drawing.Pen $col, 3
        $g.DrawLine($pen, [float]($ox + $m[$b[0]].x * 64 * $S), [float]($m[$b[0]].y * 64 * $S), [float]($ox + $m[$b[1]].x * 64 * $S), [float]($m[$b[1]].y * 64 * $S))
    }
}
$bmp.Save((Join-Path (Get-Location) $OutPng), [System.Drawing.Imaging.ImageFormat]::Png)
"thigh {0:N1} shin {1:N1} upper {2:N1} fore {3:N1} torso {4:N1} drop {5:N2}" -f $thigh, $shin, $upper, $fore, $torso, $drop
$report
