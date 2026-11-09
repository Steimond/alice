# One row per clip of a game frame folder (<prefix>_<clip>_NN.png), enlarged on the room grey with the floor line,
# to look at every frame of every clip at once. Usage: clip-sheet.ps1 -Dir <folder> -Out <png> [-Scale 3]
param(
    [Parameter(Mandatory)][string]$Dir,
    [Parameter(Mandatory)][string]$Out,
    [int]$Scale = 3
)

Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem $Dir -Filter *.png | Sort-Object Name
$rows = [ordered]@{}
foreach ($file in $files)
{
    $clip = $file.BaseName -replace '_\d+$', ''
    if (-not $rows.Contains($clip)) { $rows[$clip] = New-Object System.Collections.ArrayList }
    [void]$rows[$clip].Add($file.FullName)
}

$first = New-Object System.Drawing.Bitmap $files[0].FullName
$w = $first.Width * $Scale; $h = $first.Height * $Scale; $first.Dispose()
$gap = 6; $label = 110
$most = ($rows.Values | ForEach-Object { $_.Count } | Measure-Object -Maximum).Maximum
$sheet = New-Object System.Drawing.Bitmap ([int]($label + $most * ($w + $gap))), ([int]($rows.Count * ($h + $gap)))
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(58, 58, 64))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$font = New-Object System.Drawing.Font 'Consolas', 11
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(160, 90, 90)), 1
$row = 0
foreach ($clip in $rows.Keys)
{
    $y = $row * ($h + $gap)
    $g.DrawString(($clip -replace '^[^_]+_', ''), $font, [System.Drawing.Brushes]::White, 4, $y + 4)
    $col = 0
    foreach ($path in $rows[$clip])
    {
        $image = New-Object System.Drawing.Bitmap $path
        $x = $label + $col * ($w + $gap)
        $g.DrawImage($image, $x, $y, $w, $h)
        $g.DrawLine($pen, $x, $y + $h - 1, $x + $w, $y + $h - 1)
        $image.Dispose()
        $col++
    }
    $row++
}
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
