# Small Alice frames into the game: the palette of the big stance, hard alpha, specks dropped (bits not joined to
# the body, which PixMiniMax scatters on jumps), then onto Alice's 128x80 canvas with the feet on the bottom row and
# the body on the same centre line as her full-size frames. Airborne frames sit on the bottom row too: the jump
# height comes from physics, not from the picture.
param(
    [string]$OutDir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial",
    [int]$MinSpeck = 6
)

Add-Type -AssemblyName System.Drawing
$root = $PSScriptRoot
$paletteSource = New-Object System.Drawing.Bitmap (Join-Path $root "..\Revision03\pipeline\stand\stand-noneedle.png")
$palette = @{}
for ($y = 0; $y -lt $paletteSource.Height; $y++)
{
    for ($x = 0; $x -lt $paletteSource.Width; $x++)
    {
        $c = $paletteSource.GetPixel($x, $y)
        if ($c.A -ge 128) { $palette[$c.ToArgb()] = $c }
    }
}
$paletteSource.Dispose()
$colours = @($palette.Values)
$nearest = @{}

function Nearest([System.Drawing.Color]$c)
{
    $key = $c.ToArgb() -bor 0xFF000000
    if ($nearest.ContainsKey($key)) { return $nearest[$key] }
    $best = $null; $bestDistance = [double]::MaxValue
    foreach ($p in $colours)
    {
        $dr = $c.R - $p.R; $dg = $c.G - $p.G; $db = $c.B - $p.B
        $d = 2 * $dr * $dr + 4 * $dg * $dg + 3 * $db * $db
        if ($d -lt $bestDistance) { $bestDistance = $d; $best = $p }
    }
    $nearest[$key] = $best
    return $best
}

# Returns a 2D array of colours (or $null) with specks removed.
function Clean([string]$path)
{
    $b = New-Object System.Drawing.Bitmap $path
    $w = $b.Width; $h = $b.Height
    $grid = New-Object 'object[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++)
    {
        for ($x = 0; $x -lt $w; $x++)
        {
            $c = $b.GetPixel($x, $y)
            if ($c.A -ge 128) { $grid[$x, $y] = Nearest $c }
        }
    }
    $b.Dispose()

    $label = New-Object 'int[,]' $w, $h
    $sizes = @{ }
    $next = 0
    for ($y = 0; $y -lt $h; $y++)
    {
        for ($x = 0; $x -lt $w; $x++)
        {
            if ($null -eq $grid[$x, $y] -or $label[$x, $y] -ne 0) { continue }
            $next++
            $stack = New-Object System.Collections.Stack
            $stack.Push(@($x, $y)); $label[$x, $y] = $next; $count = 0
            while ($stack.Count -gt 0)
            {
                $p = $stack.Pop(); $count++
                for ($dy = -1; $dy -le 1; $dy++)
                {
                    for ($dx = -1; $dx -le 1; $dx++)
                    {
                        $nx = $p[0] + $dx; $ny = $p[1] + $dy
                        if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $w -or $ny -ge $h) { continue }
                        if ($null -ne $grid[$nx, $ny] -and $label[$nx, $ny] -eq 0)
                        {
                            $label[$nx, $ny] = $next; $stack.Push(@($nx, $ny))
                        }
                    }
                }
            }
            $sizes[$next] = $count
        }
    }

    # Only the body stays: PixMiniMax also draws thin detached streaks (a needle-like line on the jump) that are
    # longer than any speck, so everything but the largest piece goes.
    $body = ($sizes.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key
    $dropped = 0
    for ($y = 0; $y -lt $h; $y++)
    {
        for ($x = 0; $x -lt $w; $x++)
        {
            if ($null -ne $grid[$x, $y] -and $label[$x, $y] -ne $body) { $grid[$x, $y] = $null; $dropped++ }
        }
    }

    return @{ Grid = $grid; W = $w; H = $h; Dropped = $dropped }
}

# Horizontal centre of the stance body on the 128-wide canvas, from the full-size stance (x 16..37 of 64, +36).
$centreX = 62.5

function Place($frame, [string]$name, [double]$bodyCentre)
{
    $grid = $frame.Grid
    $maxY = -1
    for ($y = 0; $y -lt $frame.H; $y++)
    {
        for ($x = 0; $x -lt $frame.W; $x++) { if ($null -ne $grid[$x, $y]) { $maxY = $y } }
    }
    $dx = [int][Math]::Round($centreX - $bodyCentre)
    $dy = 79 - $maxY
    $out = New-Object System.Drawing.Bitmap 128, 80
    for ($y = 0; $y -lt $frame.H; $y++)
    {
        for ($x = 0; $x -lt $frame.W; $x++)
        {
            $c = $grid[$x, $y]
            if ($null -eq $c) { continue }
            $tx = $x + $dx; $ty = $y + $dy
            if ($tx -ge 0 -and $tx -lt 128 -and $ty -ge 0 -and $ty -lt 80) { $out.SetPixel($tx, $ty, $c) }
        }
    }
    $out.Save((Join-Path $OutDir "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    "{0}: specks dropped {1}" -f $name, $frame.Dropped
}

# The small stance body spans x 14..30 on the 48 canvas; every small frame keeps that centre so nothing slides.
$smallCentre = 22
$sets = [ordered]@{
    'alice_trial_small_idle_'  = @('breath', 1, 2, 3, 4, 5, 6, 7, 8)
    'alice_trial_small_run_'   = @('runloop', 1, 2, 3, 4, 5, 6, 7, 8)
    'alice_trial_small_rise_'  = @('jump', 2, 3)
    'alice_trial_small_apex_'  = @('jump', 4, 5)
    'alice_trial_small_fall_'  = @('jump', 6)
    'alice_trial_small_land_'  = @('jump', 7)
}
foreach ($prefix in $sets.Keys)
{
    $spec = $sets[$prefix]
    for ($i = 1; $i -lt $spec.Count; $i++)
    {
        $frame = Clean (Join-Path $root "raw\$($spec[0])\$($spec[$i]).png")
        Place $frame ("{0}{1:00}" -f $prefix, ($i - 1)) $smallCentre
    }
}

# The shrink is on the full-size 64 canvas: the same centre as every full-size frame (26.5 + 36).
for ($i = 1; $i -le 8; $i++)
{
    $frame = Clean (Join-Path $root "raw\shrink\$i.png")
    Place $frame ("alice_trial_shrink_{0:00}" -f ($i - 1)) 26.5
}
