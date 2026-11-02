param([string]$Skeleton, [string]$OutJson, [string]$OutPng)
Add-Type -AssemblyName System.Drawing
$N = 64.0
$sk = Get-Content $Skeleton -Raw | ConvertFrom-Json
$k = @{}
foreach ($p in $sk.keypoints) { $k[$p.label] = @{ x = $p.x * $N; y = $p.y * $N } }

function Dist($a, $b) { [math]::Sqrt(($a.x - $b.x) * ($a.x - $b.x) + ($a.y - $b.y) * ($a.y - $b.y)) }
function Rad($d) { $d * [math]::PI / 180 }
# Angle from straight down; positive swings toward +x (forward when facing east).
function Step($from, $len, $deg) { @{ x = $from.x + $len * [math]::Sin((Rad $deg)); y = $from.y + $len * [math]::Cos((Rad $deg)) } }

$thigh = ((Dist $k['RIGHT HIP'] $k['RIGHT KNEE']) + (Dist $k['LEFT HIP'] $k['LEFT KNEE'])) / 2
$shin = ((Dist $k['RIGHT KNEE'] $k['RIGHT LEG']) + (Dist $k['LEFT KNEE'] $k['LEFT LEG'])) / 2
$upper = ((Dist $k['RIGHT SHOULDER'] $k['RIGHT ELBOW']) + (Dist $k['LEFT SHOULDER'] $k['LEFT ELBOW'])) / 2
$fore = ((Dist $k['RIGHT ELBOW'] $k['RIGHT ARM']) + (Dist $k['LEFT ELBOW'] $k['LEFT ARM'])) / 2
$hipC = @{ x = ($k['RIGHT HIP'].x + $k['LEFT HIP'].x) / 2; y = ($k['RIGHT HIP'].y + $k['LEFT HIP'].y) / 2 }
$torso = Dist $hipC $k['NECK']
$groundAnkle = [math]::Max($k['RIGHT LEG'].y, $k['LEFT LEG'].y)
$headLabels = 'NOSE', 'LEFT EYE', 'RIGHT EYE', 'LEFT EAR', 'RIGHT EAR'

# One leg's phase table: thigh angle and knee bend (degrees). The other leg runs 4 frames behind.
$thighA = 30, 15, -5, -30, -35, -10, 20, 40
$bendA = 10, 35, 25, 20, 70, 110, 95, 45
# Frames 3 and 7 are the airborne moments of the two strides.
$lift = 0, 0, 0, 1.5, 0, 0, 0, 1.5
$lean = 10

$frames = @()
for ($f = 0; $f -lt 8; $f++)
{
    $j = @{}
    $hip = @{ x = $hipC.x; y = $hipC.y }
    foreach ($side in 'RIGHT', 'LEFT')
    {
        $p = if ($side -eq 'RIGHT') { $f } else { ($f + 4) % 8 }
        $h = @{ x = $hip.x + ($k["$side HIP"].x - $hipC.x); y = $hip.y + ($k["$side HIP"].y - $hipC.y) }
        $knee = Step $h $thigh $thighA[$p]
        $ankle = Step $knee $shin ($thighA[$p] - $bendA[$p])
        $j["$side HIP"] = $h; $j["$side KNEE"] = $knee; $j["$side LEG"] = $ankle
    }
    # Drop the body so the lower foot touches the stance ground line; lift it on airborne frames.
    $low = [math]::Max($j['RIGHT LEG'].y, $j['LEFT LEG'].y)
    $dy = $groundAnkle - $lift[$f] - $low
    $neck = Step $hip $torso (180 - $lean)
    $j['NECK'] = $neck
    foreach ($h in $headLabels) { $j[$h] = @{ x = $neck.x + ($k[$h].x - $k['NECK'].x); y = $neck.y + ($k[$h].y - $k['NECK'].y) } }
    foreach ($side in 'RIGHT', 'LEFT')
    {
        $p = if ($side -eq 'RIGHT') { $f } else { ($f + 4) % 8 }
        $sh = @{ x = $neck.x + ($k["$side SHOULDER"].x - $k['NECK'].x); y = $neck.y + ($k["$side SHOULDER"].y - $k['NECK'].y) }
        # Arms swing against the same-side leg; forearm stays bent forward.
        $ua = - $thighA[$p] * 0.9
        $elbow = Step $sh $upper $ua
        $hand = Step $elbow $fore ($ua + 75)
        $j["$side SHOULDER"] = $sh; $j["$side ELBOW"] = $elbow; $j["$side ARM"] = $hand
    }
    $list = @()
    foreach ($label in $j.Keys)
    {
        $z = if ($label -like 'RIGHT*') { 2 } elseif ($label -like 'LEFT*') { 0 } else { 1 }
        if ($headLabels -contains $label) { $z = 1 }
        $list += [ordered]@{ label = $label; x = [math]::Round($j[$label].x / $N, 4); y = [math]::Round(($j[$label].y + $dy) / $N, 4); z_index = $z }
    }
    $frames += , $list
}
ConvertTo-Json -InputObject $frames -Depth 5 -Compress | Out-File -Encoding ascii $OutJson

# Stick-figure preview at 6x: near (right) limbs orange, far (left) limbs blue.
$S = 6
$bmp = New-Object System.Drawing.Bitmap (64 * $S * 8 + 70), (64 * $S)
$g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::FromArgb(40, 40, 44)); $g.SmoothingMode = 'AntiAlias'
$bones = @(('NECK', 'RIGHT HIP'), ('NECK', 'LEFT HIP'), ('RIGHT HIP', 'RIGHT KNEE'), ('RIGHT KNEE', 'RIGHT LEG'), ('LEFT HIP', 'LEFT KNEE'), ('LEFT KNEE', 'LEFT LEG'),
    ('RIGHT SHOULDER', 'RIGHT ELBOW'), ('RIGHT ELBOW', 'RIGHT ARM'), ('LEFT SHOULDER', 'LEFT ELBOW'), ('LEFT ELBOW', 'LEFT ARM'), ('NECK', 'NOSE'))
for ($f = 0; $f -lt 8; $f++)
{
    $m = @{}; foreach ($p in $frames[$f]) { $m[$p.label] = $p }
    $ox = $f * (64 * $S + 10)
    $g.DrawLine([System.Drawing.Pens]::Gray, $ox, $groundAnkle * $S, $ox + 64 * $S, $groundAnkle * $S)
    foreach ($b in $bones)
    {
        $col = if ($b[0] -like 'RIGHT*' -or $b[1] -like 'RIGHT*') { [System.Drawing.Color]::Orange } elseif ($b[0] -like 'LEFT*' -or $b[1] -like 'LEFT*') { [System.Drawing.Color]::DeepSkyBlue } else { [System.Drawing.Color]::White }
        $pen = New-Object System.Drawing.Pen $col, 3
        $g.DrawLine($pen, [float]($ox + $m[$b[0]].x * 64 * $S), [float]($m[$b[0]].y * 64 * $S), [float]($ox + $m[$b[1]].x * 64 * $S), [float]($m[$b[1]].y * 64 * $S))
    }
}
$bmp.Save($OutPng, [System.Drawing.Imaging.ImageFormat]::Png)
"thigh {0:N1} shin {1:N1} upper {2:N1} fore {3:N1} torso {4:N1}" -f $thigh, $shin, $upper, $fore, $torso
