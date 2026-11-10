# Weapon candidates side by side: each enlarged on its own, and at game scale beside Alice's stance (same pixels per
# unit), so the silhouette is judged both up close and as the player sees it.
# Usage: compare.ps1 -Out compare.png [-Big 5] [-Game 3]
param([string]$Out = 'compare.png', [int]$Big = 5, [int]$Game = 3)
Add-Type -AssemblyName System.Drawing
$dir = Split-Path $MyInvocation.MyCommand.Path
$names = 'key-1', 'key-2', 'poker-1', 'poker-2', 'compass-1', 'compass-2', 'ripper-1', 'ripper-2'
$alice = [System.Drawing.Bitmap]::FromFile((Join-Path $dir '..\Revision03\pipeline\stand\stand-noneedle.png'))
$cell = 64 * $Big + 8
$pair = (64 + 48) * $Game + 8
$w = [Math]::Max($cell, $pair) * 4
$h = ($cell + 20) * 2 + ($pair + 20) * 2
$sheet = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(58, 58, 64))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$font = New-Object System.Drawing.Font 'Consolas', 11
$brush = [System.Drawing.Brushes]::Gainsboro
$col = [Math]::Max($cell, $pair)
for ($i = 0; $i -lt $names.Count; $i++)
{
    $img = [System.Drawing.Bitmap]::FromFile((Join-Path $dir ("weapons\" + $names[$i] + '.png')))
    $x = ($i % 4) * $col; $y = [Math]::Floor($i / 4) * ($cell + 20)
    $g.DrawString($names[$i], $font, $brush, $x + 4, $y)
    $g.DrawImage($img, (New-Object System.Drawing.Rectangle $x, ($y + 18), (64 * $Big), (64 * $Big)))
    # Game scale: Alice and the weapon at the same pixel size, the weapon beside her hand height.
    $y2 = 2 * ($cell + 20) + [Math]::Floor($i / 4) * ($pair + 20)
    $g.DrawString($names[$i], $font, $brush, $x + 4, $y2)
    $g.DrawImage($alice, (New-Object System.Drawing.Rectangle $x, ($y2 + 18), (64 * $Game), (64 * $Game)))
    $g.DrawImage($img, (New-Object System.Drawing.Rectangle ($x + 40 * $Game), ($y2 + 18), (64 * $Game), (64 * $Game)))
    $img.Dispose()
}
$sheet.Save((Join-Path $dir $Out))
$alice.Dispose()
