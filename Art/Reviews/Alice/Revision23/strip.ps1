# Numbered strip of a raw generation folder, enlarged without smoothing, for picking a variant by eye.
param([Parameter(Mandatory)][string]$Dir, [Parameter(Mandatory)][string]$Out, [int]$S = 3, [int]$Cols = 9)
Add-Type -AssemblyName System.Drawing
$files = @(Get-ChildItem $Dir -Filter *.png | Sort-Object Name)
$first = [System.Drawing.Bitmap]::FromFile($files[0].FullName); $w = $first.Width; $h = $first.Height; $first.Dispose()
$cols = [math]::Min($Cols, $files.Count); $rows = [math]::Ceiling($files.Count / $cols)
$b = New-Object System.Drawing.Bitmap ([int]($w * $S * $cols)), ([int]($h * $S * $rows))
$g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(58, 58, 64))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$font = New-Object System.Drawing.Font 'Consolas', 11
for ($i = 0; $i -lt $files.Count; $i++) {
    $im = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $x = ($i % $cols) * $w * $S; $y = [math]::Floor($i / $cols) * $h * $S
    $g.DrawImage($im, (New-Object System.Drawing.Rectangle $x, $y, ($w * $S), ($h * $S)), (New-Object System.Drawing.Rectangle 0, 0, $w, $h), 'Pixel')
    $g.DrawRectangle((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(90, 90, 100)), 1), $x, $y, $w * $S - 1, $h * $S - 1)
    $g.DrawString($files[$i].BaseName, $font, [System.Drawing.Brushes]::White, $x + 3, $y + 2)
    $im.Dispose()
}
$b.Save($Out)
