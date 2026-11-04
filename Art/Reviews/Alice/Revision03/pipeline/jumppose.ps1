param([string]$Skeleton, [string]$OutJson, [string]$OutPng, [ValidateSet('basic', 'detailed', 'thrust', 'slash', 'parry', 'parry-variants', 'execution-card', 'hurt-defeat', 'salts')][string]$Set = 'basic')
# Jump and attack poses for animate_with_skeleton_v3, built from the stance skeleton's bone lengths.
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
# Needle thrust as a short fencing lunge: the near fist draws back to the hip, then the near arm shoots
# straight forward at shoulder height while the front shin stays upright and the back leg straightens.
# The needle itself is drawn later by cleanup.ps1, so the hands stay empty here.
if ($Set -eq 'thrust')
{
    $poses = @(
        @('wind', 15, 20, -15, 10, -55, 40, -15, 25, -6, $true),
        @('thrust', 45, 45, -35, 0, 88, 90, -55, -80, 14, $true),
        @('thrust_hold', 48, 48, -38, 0, 90, 92, -60, -85, 17, $true),
        @('recover_a', 30, 30, -25, 5, 55, 75, -35, -40, 8, $true),
        @('recover_b', 15, 15, -10, 10, 15, 35, -10, 10, 2, $true)
    )
}
# Second hit of the combo and the air attack: the near arm rises up and back, then sweeps forward and down
# in a wide arc. The ground set keeps a short stance; the air set tucks the legs like the jump apex.
if ($Set -eq 'slash')
{
    $poses = @(
        @('g_wind', 20, 20, -15, 5, 195, 215, -20, 20, -4, $true),
        @('g_sweep_a', 30, 30, -20, 5, 130, 110, -40, -20, 6, $true),
        @('g_sweep_b', 35, 35, -25, 5, 80, 65, -50, -40, 12, $true),
        @('g_follow', 35, 35, -25, 5, 40, 20, -45, -30, 14, $true),
        @('g_recover', 20, 20, -15, 10, 20, 35, -15, 10, 5, $true),
        @('a_wind', 60, 110, 35, 115, 195, 215, -30, 10, -4, $false),
        @('a_sweep_a', 55, 100, 30, 110, 130, 110, -50, -30, 6, $false),
        @('a_sweep_b', 50, 95, 30, 105, 80, 65, -60, -45, 10, $false),
        @('a_follow', 45, 90, 25, 100, 40, 20, -55, -35, 12, $false),
        @('a_recover', 40, 80, 20, 90, 20, 35, -30, 0, 6, $false)
    )
}
# Parry: the near arm raises the needle across the body in front, flings the blow up and away,
# holds high for a moment and lowers. The weight stays back on the rear leg.
if ($Set -eq 'parry')
{
    $poses = @(
        @('p_guard', 15, 20, -15, 10, 100, 150, -20, 20, -3, $true),
        @('p_deflect', 20, 25, -20, 10, 150, 190, -30, 0, -8, $true),
        @('p_hold', 15, 20, -15, 10, 120, 170, -25, 10, -5, $true),
        @('p_recover', 15, 15, -10, 10, 30, 50, -10, 10, 0, $true)
    )
}
# Three parry ideas for the owner to compare, four frames each (guard, catch, beat, lower):
# A high beat — needle upright before the face, the blow knocked upward;
# B low beat — needle point down and forward, the blow swept down and aside;
# C step back — weight back, needle across the chest, then a push forward.
if ($Set -eq 'parry-variants')
{
    $poses = @(
        @('a_guard', 25, 30, -20, 10, 70, 125, -25, 15, -4, $true),
        @('a_catch', 25, 30, -20, 10, 80, 140, -30, 10, -7, $true),
        @('a_beat', 20, 25, -20, 10, 120, 170, -35, 0, -9, $true),
        @('a_lower', 15, 15, -10, 10, 40, 60, -15, 10, -2, $true),
        @('b_guard', 35, 45, -25, 5, 45, 70, -30, -10, 8, $true),
        @('b_catch', 35, 45, -25, 5, 40, 55, -35, -15, 10, $true),
        @('b_beat', 30, 40, -25, 5, 15, -20, -40, -20, 6, $true),
        @('b_lower', 15, 15, -10, 10, 25, 40, -15, 10, 2, $true),
        @('c_guard', 10, 20, -25, 5, 75, 100, -20, 30, -8, $true),
        @('c_catch', 8, 20, -28, 5, 70, 95, -25, 30, -12, $true),
        @('c_beat', 30, 35, -30, 10, 88, 90, -40, -30, 8, $true),
        @('c_lower', 15, 15, -10, 10, 30, 50, -15, 10, 0, $true)
    )
}
# Execution of the card soldier (GDD 0.18): the free far hand grabs the card's edge, the near hand drives
# the needle into the heart, then the hands pull apart — far up, near down — tearing the card in two,
# and Alice steps back. The soldier's own frames are generated separately and played in sync.
if ($Set -eq 'execution-card')
{
    $poses = @(
        @('x_step', 35, 35, -25, 5, -40, 30, 70, 85, 8, $true),
        @('x_grab', 35, 40, -30, 5, -60, 40, 75, 95, 10, $true),
        @('x_stab', 45, 45, -35, 0, 88, 90, 75, 100, 14, $true),
        @('x_hold', 45, 45, -35, 0, 85, 95, 80, 105, 12, $true),
        @('x_rip_a', 40, 40, -30, 5, 55, 35, 130, 160, 6, $true),
        @('x_rip_b', 35, 35, -30, 5, 30, 10, 165, 190, 2, $true),
        @('x_back', 15, 20, -20, 10, 25, 35, 40, 60, -3, $true),
        @('x_recover', 15, 15, -10, 10, 15, 35, -10, 10, 0, $true)
    )
}
# Hurt, defeat and waking (GDD section 4). Hurt: the blow from the front throws the chest back, the knees
# give, she steadies. Defeat: arms flung up, knees buckle, one knee, both knees slumping, a fall forward and
# lying face down. Waking at the rest: a sharp gasp on her knees, rising, standing.
if ($Set -eq 'hurt-defeat')
{
    $poses = @(
        @('h_jolt', 10, 25, -20, 15, -40, -10, -60, -30, -14, $true),
        @('h_recoil', 20, 40, -15, 30, 20, 50, -30, 0, -8, $true),
        @('h_recover', 15, 20, -15, 10, 15, 35, -10, 10, -2, $true),
        @('d_hit', 5, 20, -25, 10, 140, 165, 110, 140, -20, $true),
        @('d_buckle', 30, 70, 0, 70, 40, 60, 20, 40, -5, $true),
        @('d_kneel', 80, 80, 5, 95, 10, 20, 0, 10, 5, $true),
        @('d_slump', 20, 100, 10, 95, 30, 40, 15, 30, 35, $true),
        @('d_fall', 30, 110, 20, 105, 70, 80, 60, 70, 65, $true),
        @('d_prone', -80, 0, -85, 5, 85, 95, 80, 90, 88, $true),
        @('w_gasp', 10, 95, 5, 95, 20, 30, 10, 20, -12, $true),
        @('w_rise', 70, 75, 15, 60, 30, 50, 10, 30, 12, $true),
        @('w_stand', 15, 15, -10, 10, 15, 35, -10, 10, 0, $true)
    )
}
# Smelling salts (GDD 0.20): the far hand takes the vial up under the nose, a sniff, the head jerks back,
# the hand lowers. The near hand keeps the needle low so the script can draw it as usual.
if ($Set -eq 'salts')
{
    $poses = @(
        @('s_reach', 15, 15, -10, 10, 15, 35, 40, 90, 0, $true),
        @('s_raise', 15, 15, -10, 10, 15, 35, 80, 160, 1, $true),
        @('s_sniff', 15, 15, -10, 10, 15, 35, 90, 200, 4, $true),
        @('s_jolt', 15, 20, -15, 15, 10, 30, 70, 170, -12, $true),
        @('s_lower', 15, 15, -10, 10, 15, 35, 20, 40, -2, $true)
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
