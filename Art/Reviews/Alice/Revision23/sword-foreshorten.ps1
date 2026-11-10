# Foreshortened sword: the blade turned toward or away from the viewer looks shorter, so a swing round the body
# reads as a circle in depth, not as a flat up-and-down (owner, 10 Nov 2026: "as if she really swings a sword").
# The hilt keeps its size (the fist holds it), only the blade from the guard on is squeezed along its length.
# Each length gets its own RotSprite set: shard/rot-f<percent>, used by render-sword.ps1 through the "len" field.
# Usage: sword-foreshorten.ps1 [-Lengths 70,45,25]
param([int[]]$Lengths = @(70, 45, 25), [int]$Guard = 9)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = [System.Drawing.Bitmap]::FromFile((Join-Path $here 'shard\sword-final.png'))
foreach ($pc in $Lengths) {
    $blade = $src.Width - $Guard
    $nb = [math]::Max(2, [int][math]::Round($blade * $pc / 100.0))
    $out = New-Object System.Drawing.Bitmap ($Guard + $nb), $src.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $src.Height; $y++) {
        for ($x = 0; $x -lt $Guard; $x++) { $out.SetPixel($x, $y, $src.GetPixel($x, $y)) }
        for ($x = 0; $x -lt $nb; $x++) {
            # Sample from the end so the dark tip and the blood near it survive the squeeze.
            $sx = $Guard + [int][math]::Floor(($x + 0.5) * $blade / $nb)
            if ($x -eq $nb - 1) { $sx = $src.Width - 1 - [int][math]::Floor(0.5 * $blade / $nb) }
            $out.SetPixel($Guard + $x, $y, $src.GetPixel([math]::Min($src.Width - 1, $sx), $y))
        }
    }
    $file = Join-Path $here ("shard\sword-f{0}.png" -f $pc)
    $out.Save($file, [System.Drawing.Imaging.ImageFormat]::Png); $out.Dispose()
    & (Join-Path $here 'sword-rotate.ps1') -In $file -GripX 3 -GripY 4 -OutDir (Join-Path $here ("shard\rot-f{0}" -f $pc)) -Steps 32
}
$src.Dispose()
