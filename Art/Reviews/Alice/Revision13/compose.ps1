# The real bundle into the pickup frames (guide 4.7: the generator draws the gesture with empty hands, the
# object is composed by script). From the grab on it lies under her hand, then rides in her hands to her chest;
# her fingers are drawn back over it so she holds it. Frames before the grab show none: the world bundle is
# still on the floor then. Output: the game frames alice_trial_pickup_NN.
param(
    [string]$In = (Join-Path $PSScriptRoot "needle-seq2"),
    [string]$Bundle = "C:\Projects\alice\Assets\_Alice\Art\World\Items\item_bundle_00.png",
    [string]$Out = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial"
)

Add-Type -AssemblyName System.Drawing
# Frames that show the bundle (from the grab on) and the middle of her hand(s) there. The middle of the bundle goes
# exactly there in every frame (owner's rule, 6 Nov 2026), only never below the floor. The points are read from
# skin-grid.png (skin tinted, 2-px grid): the fists are the front, upper end of the skin strip. Measuring all skin
# near the hand does not work: her forearms are bare, and the middle of that skin is the forearm, near the elbow
# (owner saw the bundle on her elbow).
$hands = @{
    4  = @(75, 78)
    5  = @(75, 77)
    6  = @(75, 77)
    7  = @(75, 72)
    8  = @(74, 57)
    9  = @(72, 47)
    10 = @(70, 43)
    11 = @(70, 43)
    12 = @(72, 46)
}

# Frames where the bundle still lies on the floor under her hand.
$floorFrames = @(4, 5, 6)

$bundleImage = New-Object System.Drawing.Bitmap $Bundle
# The bundle body: its opaque box gives its middle and its bottom.
$minX = 99; $maxX = -1; $minY = 99; $maxY = -1
for ($y = 0; $y -lt $bundleImage.Height; $y++) { for ($x = 0; $x -lt $bundleImage.Width; $x++) { if ($bundleImage.GetPixel($x, $y).A -ge 128) { $minX = [Math]::Min($minX, $x); $maxX = [Math]::Max($maxX, $x); $minY = [Math]::Min($minY, $y); $maxY = [Math]::Max($maxY, $y) } } }
$bodyX = ($minX + $maxX) / 2.0
$bodyY = ($minY + $maxY) / 2.0


function IsSkin([System.Drawing.Color]$c)
{
    return $c.A -ge 128 -and ($c.R - $c.B) -gt 60 -and $c.R -gt 150 -and $c.G -gt 100
}

$steel = @(
    [System.Drawing.Color]::FromArgb(255, 38, 40, 48).ToArgb(),
    [System.Drawing.Color]::FromArgb(255, 132, 140, 152).ToArgb(),
    [System.Drawing.Color]::FromArgb(255, 214, 220, 226).ToArgb()
)

# Her arms in front of the bundle, whole: every skin patch that reaches into the hands' neighbourhood, followed to
# its end (hand, bare forearm up to the sleeve), plus every dark opaque pixel touching it (its outline). A box round
# the hand alone cut the forearm off: the arm looked torn, the elbow vanished, the box's cut edge showed as a black
# stripe (owner's note, 6 Nov 2026).
function ArmMask($frame, [int]$hx, [int]$hy)
{
    $reach = 6
    $mask = New-Object 'bool[,]' 128, 80
    $skin = New-Object 'bool[,]' 128, 80
    for ($y0 = [Math]::Max(0, $hy - $reach); $y0 -le [Math]::Min(79, $hy + $reach); $y0++)
    {
        for ($x0 = [Math]::Max(0, $hx - $reach); $x0 -le [Math]::Min(127, $hx + $reach); $x0++)
        {
            if ($skin[$x0, $y0] -or -not (IsSkin $frame.GetPixel($x0, $y0))) { continue }
            $stack = New-Object System.Collections.Stack; $stack.Push(@($x0, $y0))
            while ($stack.Count -gt 0)
            {
                $q = $stack.Pop(); $x = $q[0]; $y = $q[1]
                if ($x -lt 0 -or $y -lt 0 -or $x -ge 128 -or $y -ge 80 -or $skin[$x, $y]) { continue }
                if (-not (IsSkin $frame.GetPixel($x, $y))) { continue }
                $skin[$x, $y] = $true; $mask[$x, $y] = $true
                foreach ($d in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1), @(-1, -1), @(1, 1), @(-1, 1), @(1, -1))) { $stack.Push(@(($x + $d[0]), ($y + $d[1]))) }
            }
        }
    }

    for ($y = 0; $y -lt 80; $y++)
    {
        for ($x = 0; $x -lt 128; $x++)
        {
            if (-not $skin[$x, $y]) { continue }
            foreach ($d in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1), @(-1, -1), @(1, 1), @(-1, 1), @(1, -1)))
            {
                $nx = $x + $d[0]; $ny = $y + $d[1]
                if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge 128 -or $ny -ge 80) { continue }
                $c = $frame.GetPixel($nx, $ny)
                if ($c.A -ge 128 -and ($c.R + $c.G + $c.B) -lt 330) { $mask[$nx, $ny] = $true }
            }
        }
    }
    # The comma keeps PowerShell from unrolling the 2D array into a flat list on return.
    return ,$mask
}

$files = Get-ChildItem $In -Filter *.png | Sort-Object Name
for ($i = 0; $i -lt $files.Count; $i++)
{
    $frame = New-Object System.Drawing.Bitmap $files[$i].FullName
    $canvas = New-Object System.Drawing.Bitmap 128, 80
    for ($y = 0; $y -lt 80; $y++) { for ($x = 0; $x -lt 128; $x++) { $canvas.SetPixel($x, $y, $frame.GetPixel($x, $y)) } }
    if ($hands.ContainsKey($i))
    {
        $hx = $hands[$i][0]; $hy = $hands[$i][1]
        $dx = [int][Math]::Round($hx - $bodyX)
        $dy = [int][Math]::Round($hy - $bodyY)
        $dy = [Math]::Min($dy, 79 - $maxY)
        # Layers. On the floor the bundle lies behind all of her (her hand reaches over it, her toes are in front
        # of it). Carried, it is in front of her dress but behind her arms, and the needle stays in front.
        $onFloor = $floorFrames -contains $i
        $arm = ArmMask $frame $hx $hy
        for ($y = 0; $y -lt $bundleImage.Height; $y++)
        {
            for ($x = 0; $x -lt $bundleImage.Width; $x++)
            {
                $c = $bundleImage.GetPixel($x, $y)
                $tx = $x + $dx; $ty = $y + $dy
                if ($c.A -lt 128 -or $tx -lt 0 -or $tx -ge 128 -or $ty -lt 0 -or $ty -ge 80) { continue }
                $her = $frame.GetPixel($tx, $ty)
                $front = $her.A -ge 128 -and ($onFloor -or $arm[$tx, $ty] -or $steel -contains $her.ToArgb())
                if (-not $front) { $canvas.SetPixel($tx, $ty, $c) }
            }
        }
    }
    $frame.Dispose()
    $canvas.Save((Join-Path $Out ("alice_trial_pickup_{0:00}.png" -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $canvas.Dispose()
}
$bundleImage.Dispose()
"pickup frames: $($files.Count)"
