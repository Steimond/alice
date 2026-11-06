# Bundle shimmer into the game: frames 0-7 (8 repeats 0), hard alpha, one crop for all of them (the union of what
# is drawn, the sparkle above the knot included), so the bundle never shifts and the bottom-centre pivot sits on
# its bottom row.
param(
    [string]$OutDir = "C:\Projects\alice\Assets\_Alice\Art\World\Items",
    # raw\glint-small: the small bundle (6 Nov 2026); raw\glint was the first, too big to carry.
    [string]$Source = "raw\glint-small"
)

Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null
$frames = 0..7 | ForEach-Object { New-Object System.Drawing.Bitmap (Join-Path $PSScriptRoot "$Source\$_.png") }
$minX = 99; $minY = 99; $maxX = -1; $maxY = -1
foreach ($b in $frames)
{
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
}

$w = $maxX - $minX + 1; $h = $maxY - $minY + 1
for ($i = 0; $i -lt $frames.Count; $i++)
{
    $out = New-Object System.Drawing.Bitmap $w, $h
    for ($y = 0; $y -lt $h; $y++)
    {
        for ($x = 0; $x -lt $w; $x++)
        {
            $c = $frames[$i].GetPixel($x + $minX, $y + $minY)
            if ($c.A -ge 128) { $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B)) }
        }
    }
    $out.Save((Join-Path $OutDir ("item_bundle_{0:00}.png" -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
    $frames[$i].Dispose()
}
"bundle frames: {0}x{1}, crop x {2}..{3}, y {4}..{5}" -f $w, $h, $minX, $maxX, $minY, $maxY
