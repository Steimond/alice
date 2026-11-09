# Erases a weapon the generator drew into an enemy animation (a dark stake driven into the rat's nape), by its own
# colour: the columns where the stake colour stands in a bar are cleared from the top down to the stake's lowest
# pixel (stake colour, its dark outline and any other non-blood pixel on the stake's own columns), bright blood is
# kept, and holes left inside the body are filled from the pixel on their left when the body continues on both sides.
# Usage: rod-erase.ps1 -InDir raw -OutDir clean -Colour 4b1a19 [-MinBar 8]
param([string]$InDir, [string]$OutDir, [string]$Colour = '4b1a19', [int]$MinBar = 8)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null
$target = 'ff' + $Colour.ToLower()
foreach ($file in Get-ChildItem $InDir -Filter *.png | Sort-Object Name)
{
    $b = [System.Drawing.Bitmap]::FromFile($file.FullName)
    $w = $b.Width; $h = $b.Height
    $count = @{}; $low = -1
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        if ($b.GetPixel($x, $y).Name -eq $target) { $count[$x] = 1 + $count[$x]; if ($y -gt $low) { $low = $y } } } }
    $bar = @($count.Keys | Where-Object { $count[$_] -ge $MinBar } | Sort-Object)
    $out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { $out.SetPixel($x, $y, $b.GetPixel($x, $y)) } }
    $erased = New-Object 'bool[,]' $w, $h
    if ($bar.Count -gt 0)
    {
        $x0 = [Math]::Max(0, $bar[0] - 2); $x1 = [Math]::Min($w - 1, $bar[-1] + 2)
        for ($y = 0; $y -le $low; $y++) { for ($x = $x0; $x -le $x1; $x++) {
            $c = $b.GetPixel($x, $y)
            if ($c.A -lt 128) { continue }
            $bright = $c.R -ge 0xD0 -and $c.G -le 0x50
            $dark = (0.3 * $c.R + 0.59 * $c.G + 0.11 * $c.B) -lt 0x30
            $onBar = $x -ge $bar[0] -and $x -le $bar[-1]
            if ($c.Name -eq $target -or $dark -or ($onBar -and -not $bright)) {
                $out.SetPixel($x, $y, [System.Drawing.Color]::Transparent); $erased[$x, $y] = $true } } }
        # Mend the body where the stake went in: a gap with body on both sides takes the colour on its left.
        for ($y = 0; $y -le $low; $y++) {
            for ($x = $x0; $x -le $x1; $x++) {
                if (-not $erased[$x, $y]) { continue }
                $left = $null; $right = $false
                for ($k = 1; $k -le 6 -and $null -eq $left; $k++) { $p = $out.GetPixel([Math]::Max(0, $x - $k), $y); if ($p.A -ge 128) { $left = $p } }
                for ($k = 1; $k -le 6; $k++) { $p = $out.GetPixel([Math]::Min($w - 1, $x + $k), $y); if ($p.A -ge 128 -and -not $erased[[Math]::Min($w - 1, $x + $k), $y]) { $right = $true; break } }
                if ($null -ne $left -and $right) { $out.SetPixel($x, $y, $left) } } }
        "{0}: stake x {1}..{2}, down to {3}" -f $file.Name, $bar[0], $bar[-1], $low
    }
    $out.Save((Join-Path (Resolve-Path $OutDir).Path $file.Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose(); $b.Dispose()
}
