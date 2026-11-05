# Inputs for small Alice: a 32x32 style crop of the stance (head and torso) and a half-size stance on a 32x32
# canvas, downsampled by the most frequent opaque colour of each 2x2 block, as a base for the generator to redraw.
param(
    [string]$Stand = "C:\Projects\alice\Art\Reviews\Alice\Revision03\pipeline\stand\stand-noneedle.png"
)

Add-Type -AssemblyName System.Drawing
$source = New-Object System.Drawing.Bitmap $Stand

$crop = New-Object System.Drawing.Bitmap 32, 32
for ($y = 0; $y -lt 32; $y++)
{
    for ($x = 0; $x -lt 32; $x++)
    {
        $crop.SetPixel($x, $y, $source.GetPixel($x + 11, $y + 6))
    }
}
$crop.Save((Join-Path $PSScriptRoot "style-crop32.png"), [System.Drawing.Imaging.ImageFormat]::Png)

# Half size; the stance's feet (row 59) land on the bottom row, centred.
$half = New-Object System.Drawing.Bitmap 32, 32
for ($y = 0; $y -lt 32; $y++)
{
    for ($x = 0; $x -lt 32; $x++)
    {
        $sx = 2 * $x - 6
        $sy = 2 * $y - 4
        $counts = @{}
        foreach ($d in @(@(0, 0), @(1, 0), @(0, 1), @(1, 1)))
        {
            $px = $sx + $d[0]; $py = $sy + $d[1]
            if ($px -lt 0 -or $py -lt 0 -or $px -ge 64 -or $py -ge 64) { continue }
            $c = $source.GetPixel($px, $py)
            if ($c.A -lt 128) { continue }
            $key = $c.ToArgb()
            $counts[$key] = 1 + [int]$counts[$key]
        }
        # A block needs two opaque pixels, so thin outlines do not double the silhouette.
        $total = ($counts.Values | Measure-Object -Sum).Sum
        if ($total -ge 2)
        {
            $best = ($counts.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key
            $half.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($best))
        }
    }
}
$half.Save((Join-Path $PSScriptRoot "stand-half32.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$source.Dispose(); $crop.Dispose(); $half.Dispose()
