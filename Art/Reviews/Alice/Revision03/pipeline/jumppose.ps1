param([string]$Skeleton, [string]$OutJson, [string]$OutPng, [ValidateSet('basic', 'detailed')][string]$Set = 'basic')
# Jump poses for animate_with_skeleton_v3, built from the stance skeleton's bone lengths.
# Angles are degrees from straight down, positive toward +x (forward when facing east).
# RIGHT limbs are the near ones. Air frames keep the hips at stance height so the legs tuck up;
# the landing frame drops the body until the lower ankle reaches the stance ground line.
Add-Type -AssemblyName System.Drawing
$N = 64.0
$sk = Get-Content $Skeleton -Raw | ConvertFrom-Json
$k = @{}
foreach ($p in $sk.keypoints) { $k[$p.label] = @{ x = $p.x * $N; y = $p.y * $N } }

function Dist($a, $b) { [math]::Sqrt(($a.x - $b.x) * ($a.x - $b.x) + ($a.y - $b.y) * ($a.y - $b.y)) }
function Step($from, $len, $deg) { $r = $deg * [math]::PI / 180; @{ x = $from.x + $len * [math]::Sin($r); y = $from.y + $len * [math]::Cos($r) } }

$thigh = ((Dist $k['RIGHT HIP'] $k['RIGHT KNEE']) + (Dist $k['LEFT HIP'] $k['LEFT KNEE'])) / 2
$shin = ((Dist $k['RIGHT KNEE'] $k['RIGHT LEG']) + (Dist $k['LEFT KNEE'] $k['LEFT LEG'])) / 2
$upper = ((Dist $k['RIGHT SHOULDER'] $k['RIGHT ELBOW']) + (Dist $k['LEFT SHOULDER'] $k['LEFT ELBOW'])) / 2
$fore = ((Dist $k['RIGHT ELBOW'] $k['RIGHT ARM']) + (Dist $k['LEFT ELBOW'] $k['LEFT ARM'])) / 2
$hipC = @{ x = ($k['RIGHT HIP'].x + $k['LEFT HIP'].x) / 2; y = ($k['RIGHT HIP'].y + $k['LEFT HIP'].y) / 2 }
$torso = Dist $hipC $k['NECK']
$groundAnkle = [math]::Max($k['RIGHT LEG'].y, $k['LEFT LEG'].y)
$headLabels = 'NOSE', 'LEFT EYE', 'RIGHT EYE', 'LEFT EAR', 'RIGHT EAR'

# name, near thigh/bend, far thigh/bend, near upper arm/forearm, far upper arm/forearm, lean, grounded
$poses = @(
    @('rise_a', -5, 10, -20, 30, 140, 160, 120, 150, 0, $false),
    @('rise_b', 45, 80, 15, 95, 130, 170, 110, 150, 5, $false),
    @('apex', 60, 110, 35, 115, 80, 120, -70, -40, 8, $false),
    @('fall_a', 20, 35, -10, 50, 150, 175, 165, 185, 0, $false),
    @('fall_b', 15, 25, -15, 60, 160, 185, 145, 170, -3, $false),
    @('land', 55, 100, 45, 105, 40, 90, -30, 20, 20, $true)
)
# The detailed set adds in-betweens, a fall with the arms thrown up, a two-step landing and the step off
# a ledge without a jump, so a walk-off no longer borrows the tucked apex pose.
if ($Set -eq 'detailed')
{
    $poses = @(
        @('takeoff', -15, 10, 30, 60, 150, 165, 130, 150, 5, $false),
        @('rise_fast', -5, 10, -20, 30, 140, 160, 120, 150, 0, $false),
        @('rise_mid', 25, 50, 0, 60, 135, 165, 115, 150, 3, $false),
        @('rise_slow', 45, 80, 15, 95, 130, 170, 110, 150, 5, $false),
        @('apex', 60, 110, 35, 115, 80, 120, -70, -40, 8, $false),
        @('apex_open', 40, 70, 20, 80, 100, 130, 30, 60, 4, $false),
        @('fall_start', 25, 40, 0, 45, 130, 150, 110, 140, 0, $false),
        @('fall_a', 15, 20, -10, 35, 150, 170, 140, 165, -3, $false),
        @('fall_b', 8, 30, -15, 20, 160, 175, 130, 155, -3, $false),
        @('land_impact', 60, 105, 50, 105, 30, 70, -20, 30, 25, $true),
        @('land_recover', 35, 60, 25, 60, 15, 40, -10, 20, 10, $true),
        @('step_off', 30, 15, -25, 30, 60, 90, -40, -10, 3, $false),
        @('step_drop', 20, 30, -10, 30, 100, 130, 60, 100, 0, $false)
    )
}

$frames = @()
foreach ($pose in $poses)
{
    $j = @{}
    $hip = @{ x = $hipC.x; y = $hipC.y }
    $legs = @{ RIGHT = @($pose[1], $pose[2]); LEFT = @($pose[3], $pose[4]) }
    $arms = @{ RIGHT = @($pose[5], $pose[6]); LEFT = @($pose[7], $pose[8]) }
    foreach ($side in 'RIGHT', 'LEFT')
    {
        $h = @{ x = $hip.x + ($k["$side HIP"].x - $hipC.x); y = $hip.y + ($k["$side HIP"].y - $hipC.y) }
        $knee = Step $h $thigh $legs[$side][0]
        $j["$side HIP"] = $h; $j["$side KNEE"] = $knee; $j["$side LEG"] = Step $knee $shin ($legs[$side][0] - $legs[$side][1])
    }
    $neck = Step $hip $torso (180 - $pose[9])
    $j['NECK'] = $neck
    foreach ($h in $headLabels) { $j[$h] = @{ x = $neck.x + ($k[$h].x - $k['NECK'].x); y = $neck.y + ($k[$h].y - $k['NECK'].y) } }
    foreach ($side in 'RIGHT', 'LEFT')
    {
        $sh = @{ x = $neck.x + ($k["$side SHOULDER"].x - $k['NECK'].x); y = $neck.y + ($k["$side SHOULDER"].y - $k['NECK'].y) }
        $elbow = Step $sh $upper $arms[$side][0]
        $j["$side SHOULDER"] = $sh; $j["$side ELBOW"] = $elbow; $j["$side ARM"] = Step $elbow $fore $arms[$side][1]
    }
    $dy = 0
    if ($pose[10]) { $dy = $groundAnkle - [math]::Max($j['RIGHT LEG'].y, $j['LEFT LEG'].y) }
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

$S = 6
$bmp = New-Object System.Drawing.Bitmap ([int](($N * $S + 10) * $frames.Count)), ([int]($N * $S))
$g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::FromArgb(40, 40, 44)); $g.SmoothingMode = 'AntiAlias'
$bones = @(('NECK', 'RIGHT HIP'), ('NECK', 'LEFT HIP'), ('RIGHT HIP', 'RIGHT KNEE'), ('RIGHT KNEE', 'RIGHT LEG'), ('LEFT HIP', 'LEFT KNEE'), ('LEFT KNEE', 'LEFT LEG'),
    ('RIGHT SHOULDER', 'RIGHT ELBOW'), ('RIGHT ELBOW', 'RIGHT ARM'), ('LEFT SHOULDER', 'LEFT ELBOW'), ('LEFT ELBOW', 'LEFT ARM'), ('NECK', 'NOSE'))
for ($f = 0; $f -lt $frames.Count; $f++)
{
    $m = @{}; foreach ($p in $frames[$f]) { $m[$p.label] = $p }
    $ox = $f * ($N * $S + 10)
    $g.DrawLine([System.Drawing.Pens]::Gray, $ox, $groundAnkle * $S, $ox + $N * $S, $groundAnkle * $S)
    foreach ($b in $bones)
    {
        $col = if ($b[0] -like 'RIGHT*' -or $b[1] -like 'RIGHT*') { [System.Drawing.Color]::Orange } elseif ($b[0] -like 'LEFT*' -or $b[1] -like 'LEFT*') { [System.Drawing.Color]::DeepSkyBlue } else { [System.Drawing.Color]::White }
        $pen = New-Object System.Drawing.Pen $col, 3
        $g.DrawLine($pen, [float]($ox + $m[$b[0]].x * $N * $S), [float]($m[$b[0]].y * $N * $S), [float]($ox + $m[$b[1]].x * $N * $S), [float]($m[$b[1]].y * $N * $S))
    }
    $g.DrawString($poses[$f][0], (New-Object System.Drawing.Font 'Arial', 12), [System.Drawing.Brushes]::Yellow, $ox + 4, 4)
}
$bmp.Save($OutPng, [System.Drawing.Imaging.ImageFormat]::Png)
