# Переупорядочивает угловой Wang-тайлсет PixelLab в постоянный порядок для Unity:
# тайл с углами NW, NE, SW, SE (1 — массив) лежит под номером NW*8 + NE*4 + SW*2 + SE
# в сетке 4x4 слева направо, сверху вниз. Заодно убирает одиночные пиксели чужого
# цвета (насыщенно-синие точки, которые генератор оставил на краю).
param(
    [Parameter(Mandatory)] [string] $Sheet,
    [Parameter(Mandatory)] [string] $Meta,
    [Parameter(Mandatory)] [string] $Out
)
Add-Type -AssemblyName System.Drawing
$tilesetInfo = Get-Content $Meta -Raw | ConvertFrom-Json
$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Sheet))
[int] $tileSize = $tilesetInfo.tileset_data.tiles[0].bounding_box.width
$canvas = New-Object System.Drawing.Bitmap ([int]($tileSize * 4)), ([int]($tileSize * 4))
$removed = 0
foreach ($tile in $tilesetInfo.tileset_data.tiles) {
    $c = $tile.corners
    [int] $index = 8 * [int]($c.NW -eq 'lower') + 4 * [int]($c.NE -eq 'lower') + 2 * [int]($c.SW -eq 'lower') + [int]($c.SE -eq 'lower')
    $bb = $tile.bounding_box
    [int] $ox = ($index % 4) * $tileSize
    [int] $oy = [Math]::Floor($index / 4) * $tileSize
    for ($y = 0; $y -lt $tileSize; $y++) {
        for ($x = 0; $x -lt $tileSize; $x++) {
            $p = $source.GetPixel($bb.x + $x, $bb.y + $y)
            if ($p.A -gt 0 -and $p.A -lt 255) { $p = [System.Drawing.Color]::FromArgb(255, $p.R, $p.G, $p.B) }
            if ($p.A -gt 0 -and $p.B -gt $p.R + 80 -and $p.B -gt $p.G + 25) {
                $p = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
                $removed++
            }
            $canvas.SetPixel($ox + $x, $oy + $y, $p)
        }
    }
}
$canvas.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"saved $Out, removed $removed stray pixels"
