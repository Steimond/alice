param([string]$Dir, [string]$Out, [int]$Scale = 8)
Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem $Dir -Filter *.png | Sort-Object Name
$all = @{}
$rows = @()
$first = [System.Drawing.Bitmap]::FromFile($files[0].FullName)
$w = $first.Width; $h = $first.Height; $first.Dispose()
$strip = New-Object System.Drawing.Bitmap (($w * $Scale + 8) * $files.Count), ($h * $Scale)
$g = [System.Drawing.Graphics]::FromImage($strip)
$g.Clear([System.Drawing.Color]::FromArgb(60, 60, 64))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 80, 80)), 1
for ($i = 0; $i -lt $files.Count; $i++)
{
    $b = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $c = @{}; $semi = 0; $minx = 999; $miny = 999; $maxx = -1; $maxy = -1
    for ($y = 0; $y -lt $b.Height; $y++) { for ($x = 0; $x -lt $b.Width; $x++) {
        $p = $b.GetPixel($x, $y)
        if ($p.A -gt 0)
        {
            if ($p.A -lt 255) { $semi++ }
            $c[$p.ToArgb()] = 1; $all[$p.ToArgb()] = 1
            if ($x -lt $minx) { $minx = $x }; if ($x -gt $maxx) { $maxx = $x }
            if ($y -lt $miny) { $miny = $y }; if ($y -gt $maxy) { $maxy = $y }
        }
    } }
    $rows += [pscustomobject]@{ frame = $i; colors = $c.Count; semi = $semi; bbox = "$($maxx - $minx + 1)x$($maxy - $miny + 1)"; top = $miny; bottom = $maxy; left = $minx; right = $maxx }
    $x0 = $i * ($w * $Scale + 8)
    $g.DrawImage($b, $x0, 0, $w * $Scale, $h * $Scale)
    $b.Dispose()
}
# Ground line of frame 0, to see vertical drift across frames.
$gy = ($rows[0].bottom + 1) * $Scale
$g.DrawLine($pen, 0, $gy, $strip.Width, $gy)
$strip.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$rows | Format-Table -AutoSize
"unique colours across all frames: $($all.Count)"
