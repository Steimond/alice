# Maps every opaque pixel to the nearest colour of a reference sprite's palette (weighted RGB distance), and
# makes alpha hard. Used when a small-canvas generation keeps the shapes but drifts in colour.
param(
    [Parameter(Mandatory)][string]$In,
    [Parameter(Mandatory)][string]$PaletteFrom,
    [Parameter(Mandatory)][string]$Out
)

Add-Type -AssemblyName System.Drawing
$reference = New-Object System.Drawing.Bitmap ((Resolve-Path $PaletteFrom).Path)
$palette = @{}
for ($y = 0; $y -lt $reference.Height; $y++)
{
    for ($x = 0; $x -lt $reference.Width; $x++)
    {
        $c = $reference.GetPixel($x, $y)
        if ($c.A -ge 128) { $palette[$c.ToArgb()] = $c }
    }
}
$reference.Dispose()
$colours = @($palette.Values)

$source = New-Object System.Drawing.Bitmap ((Resolve-Path $In).Path)
$result = New-Object System.Drawing.Bitmap $source.Width, $source.Height
for ($y = 0; $y -lt $source.Height; $y++)
{
    for ($x = 0; $x -lt $source.Width; $x++)
    {
        $c = $source.GetPixel($x, $y)
        if ($c.A -lt 128) { continue }
        $best = $null; $bestDistance = [double]::MaxValue
        foreach ($p in $colours)
        {
            $dr = $c.R - $p.R; $dg = $c.G - $p.G; $db = $c.B - $p.B
            $d = 2 * $dr * $dr + 4 * $dg * $dg + 3 * $db * $db
            if ($d -lt $bestDistance) { $bestDistance = $d; $best = $p }
        }
        $result.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $best.R, $best.G, $best.B))
    }
}
$source.Dispose()
$result.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$result.Dispose()
"palette $($colours.Count) colours"
