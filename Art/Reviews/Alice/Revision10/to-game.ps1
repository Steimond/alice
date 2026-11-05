# Little door opening and Alice's bite into the game. The door frames share the closed door's crop, so the arch
# never shifts and the bottom-centre pivot stays on its sill. The bite goes onto Alice's 128x80 canvas exactly as
# her full-size frames (x+36, y+20), in the stance's palette with hard alpha.
param(
    [string]$Gates = "C:\Projects\alice\Assets\_Alice\Art\World\Gates",
    [string]$Frames = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial"
)

Add-Type -AssemblyName System.Drawing
$root = $PSScriptRoot

function Bounds([string]$path)
{
    $b = New-Object System.Drawing.Bitmap $path
    $minX = $b.Width; $minY = $b.Height; $maxX = -1; $maxY = -1
    for ($y = 0; $y -lt $b.Height; $y++)
    {
        for ($x = 0; $x -lt $b.Width; $x++)
        {
            if ($b.GetPixel($x, $y).A -ge 128)
            {
                $minX = [Math]::Min($minX, $x); $maxX = [Math]::Max($maxX, $x)
                $minY = [Math]::Min($minY, $y); $maxY = [Math]::Max($maxY, $y)
            }
        }
    }
    $b.Dispose()
    return @($minX, $minY, ($maxX - $minX + 1), ($maxY - $minY + 1))
}

$r = Bounds (Join-Path $root "raw\door-open\0.png")
for ($i = 0; $i -le 4; $i++)
{
    $b = New-Object System.Drawing.Bitmap (Join-Path $root "raw\door-open\$i.png")
    $out = New-Object System.Drawing.Bitmap $r[2], $r[3]
    for ($y = 0; $y -lt $r[3]; $y++)
    {
        for ($x = 0; $x -lt $r[2]; $x++)
        {
            $c = $b.GetPixel($x + $r[0], $y + $r[1])
            if ($c.A -ge 128) { $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B)) }
        }
    }
    $b.Dispose()
    $out.Save((Join-Path $Gates ("gate_little_door_{0:00}.png" -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}
"door: {0}x{1}" -f $r[2], $r[3]

$stand = New-Object System.Drawing.Bitmap (Join-Path $root "..\Revision03\pipeline\stand\stand-noneedle.png")
$palette = @{}
for ($y = 0; $y -lt 64; $y++) { for ($x = 0; $x -lt 64; $x++) { $c = $stand.GetPixel($x, $y); if ($c.A -ge 128) { $palette[$c.ToArgb()] = $c } } }
$stand.Dispose()
$colours = @($palette.Values)
for ($i = 1; $i -le 8; $i++)
{
    $b = New-Object System.Drawing.Bitmap (Join-Path $root "raw\bite\$i.png")
    $out = New-Object System.Drawing.Bitmap 128, 80
    for ($y = 0; $y -lt 64; $y++)
    {
        for ($x = 0; $x -lt 64; $x++)
        {
            $c = $b.GetPixel($x, $y)
            if ($c.A -lt 128) { continue }
            # The mushroom piece is the only red in her hand; it keeps its own colour.
            $best = $c; $bestDistance = 1800
            foreach ($p in $colours)
            {
                $d = 2 * ($c.R - $p.R) * ($c.R - $p.R) + 4 * ($c.G - $p.G) * ($c.G - $p.G) + 3 * ($c.B - $p.B) * ($c.B - $p.B)
                if ($d -lt $bestDistance) { $bestDistance = $d; $best = $p }
            }
            $out.SetPixel($x + 36, $y + 20, [System.Drawing.Color]::FromArgb(255, $best.R, $best.G, $best.B))
        }
    }
    $b.Dispose()
    $out.Save((Join-Path $Frames ("alice_trial_bite_{0:00}.png" -f ($i - 1))), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}
"bite: 8 frames"
