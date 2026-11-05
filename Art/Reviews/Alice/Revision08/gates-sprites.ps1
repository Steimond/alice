# Generated gate props into game sprites: hard alpha and a crop to the drawn pixels, so the bottom-centre pivot
# (AliceCharacterArtPostprocessor) sits on the prop's real foot. The bridge keeps its full 12-tile width.
param(
    [string]$OutDir = "C:\Projects\alice\Assets\_Alice\Art\World\Gates"
)

Add-Type -AssemblyName System.Drawing
$map = [ordered]@{
    'fan'              = 'gate_pickup_A01'
    'mushroom-piece'   = 'gate_pickup_A02'
    'grin'             = 'gate_pickup_A03'
    'watch'            = 'gate_pickup_A04'
    'mushroom-marker'  = 'gate_mushroom'
    'little-door'      = 'gate_little_door'
    'cheshire-post-02' = 'gate_cheshire'
    'clock-post-02'    = 'gate_clock'
    'bridge'           = 'gate_bridge'
    'blade'            = 'gate_blade'
    'guillotine-post'  = 'gate_post'
}
$keepWidth = @('bridge')

New-Item -ItemType Directory -Force $OutDir | Out-Null
foreach ($name in $map.Keys)
{
    $source = New-Object System.Drawing.Bitmap (Join-Path $PSScriptRoot "raw\$name.png")
    $minX = $source.Width; $minY = $source.Height; $maxX = -1; $maxY = -1
    for ($y = 0; $y -lt $source.Height; $y++)
    {
        for ($x = 0; $x -lt $source.Width; $x++)
        {
            if ($source.GetPixel($x, $y).A -ge 128)
            {
                $minX = [Math]::Min($minX, $x); $maxX = [Math]::Max($maxX, $x)
                $minY = [Math]::Min($minY, $y); $maxY = [Math]::Max($maxY, $y)
            }
        }
    }

    if ($keepWidth -contains $name)
    {
        $minX = 0; $maxX = $source.Width - 1
    }

    $w = $maxX - $minX + 1; $h = $maxY - $minY + 1
    $result = New-Object System.Drawing.Bitmap $w, $h
    $translucent = 0
    for ($y = 0; $y -lt $h; $y++)
    {
        for ($x = 0; $x -lt $w; $x++)
        {
            $c = $source.GetPixel($x + $minX, $y + $minY)
            if ($c.A -gt 0 -and $c.A -lt 255) { $translucent++ }
            if ($c.A -ge 128)
            {
                $result.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
            }
        }
    }

    $source.Dispose()
    $target = Join-Path $OutDir "$($map[$name]).png"
    $result.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
    $result.Dispose()
    "{0,-18} -> {1,-18} {2}x{3}, translucent fixed: {4}" -f $name, $map[$name], $w, $h, $translucent
}
