# Массив из двух слоёв: внутренность — крупная текстура кладки по мировым координатам
# (повтор раз в размер текстуры, а не раз в тайл), край — от Wang-тайлов, но только
# полоса шириной $Rim пикселей от пустоты (верх-плитка, обводка). Раскладка — двойная
# сетка, как в wang-preview.ps1.
param(
    [Parameter(Mandatory)] [string] $Sheet,
    [Parameter(Mandatory)] [string] $Meta,
    [Parameter(Mandatory)] [string] $Texture,
    [Parameter(Mandatory)] [string] $Out,
    [string] $MapFile = '',
    [string] $Figure = '',
    [int] $FigureCellX = 6,
    [int] $Rim = 4,
    [int] $Scale = 3,
    [string] $Back = '#2b2f38',
    # Плитка 2x2 самой текстуры — проверка, видны ли швы повтора.
    [string] $SeamOut = ''
)
Add-Type -AssemblyName System.Drawing
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $MapFile) { $MapFile = Join-Path $scriptDir 'testmap.txt' }
if (-not $Figure) { $Figure = Join-Path $scriptDir '..\Revision03\pipeline\stand\stand-noneedle.png' }

$tilesetInfo = Get-Content $Meta -Raw | ConvertFrom-Json
$sheetImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Sheet))
$textureImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Texture))
[int] $tileSize = $tilesetInfo.tileset_data.tiles[0].bounding_box.width
$lookup = @{}
foreach ($tile in $tilesetInfo.tileset_data.tiles) {
    $c = $tile.corners
    $key = '{0}{1}{2}{3}' -f [int]($c.NW -eq 'lower'), [int]($c.NE -eq 'lower'), [int]($c.SW -eq 'lower'), [int]($c.SE -eq 'lower')
    $lookup[$key] = $tile.bounding_box
}

if ($SeamOut) {
    $tw = $textureImage.Width; $th = $textureImage.Height
    $seam = New-Object System.Drawing.Bitmap ([int]($tw * 2 * $Scale)), ([int]($th * 2 * $Scale))
    $sg = [System.Drawing.Graphics]::FromImage($seam)
    $sg.InterpolationMode = 'NearestNeighbor'; $sg.PixelOffsetMode = 'Half'
    for ($i = 0; $i -lt 2; $i++) { for ($j = 0; $j -lt 2; $j++) {
        $sg.DrawImage($textureImage, [int]($i * $tw * $Scale), [int]($j * $th * $Scale), [int]($tw * $Scale), [int]($th * $Scale))
    } }
    $sg.Dispose(); $seam.Save($SeamOut, [System.Drawing.Imaging.ImageFormat]::Png)
}

$rows = Get-Content $MapFile | Where-Object { $_ -ne '' }
[int] $height = $rows.Count
[int] $width = ($rows | Measure-Object -Property Length -Maximum).Maximum
function Solid([int] $x, [int] $y) {
    if ($y -lt 0 -or $y -ge $height) { return $true }
    $row = $rows[$y]
    if ($x -lt 0 -or $x -ge $row.Length) { return $true }
    return $row[$x] -eq '#'
}

# Для каждого Wang-тайла — расстояние каждого пикселя до ближайшего прозрачного
# (по квадрату Чебышёва, только внутри тайла). Полностью заполненный тайл прозрачных
# не имеет — у него вся площадь внутренняя.
$rimMask = @{}
foreach ($key in $lookup.Keys) {
    $bb = $lookup[$key]
    $mask = New-Object 'bool[,]' $tileSize, $tileSize
    for ($py = 0; $py -lt $tileSize; $py++) {
        for ($px = 0; $px -lt $tileSize; $px++) {
            $near = $false
            for ($dy = -$Rim; $dy -le $Rim -and -not $near; $dy++) {
                for ($dx = -$Rim; $dx -le $Rim; $dx++) {
                    $qx = $px + $dx; $qy = $py + $dy
                    if ($qx -lt 0 -or $qy -lt 0 -or $qx -ge $tileSize -or $qy -ge $tileSize) { continue }
                    if ($sheetImage.GetPixel($bb.x + $qx, $bb.y + $qy).A -eq 0) { $near = $true; break }
                }
            }
            $mask[$px, $py] = $near
        }
    }
    $rimMask[$key] = $mask
}

[int] $cw = $width * $tileSize; [int] $ch = $height * $tileSize
$canvas = New-Object System.Drawing.Bitmap $cw, $ch
$backColor = [System.Drawing.ColorTranslator]::FromHtml($Back)
for ($y = 0; $y -lt $ch; $y++) { for ($x = 0; $x -lt $cw; $x++) { $canvas.SetPixel($x, $y, $backColor) } }

$half = [int]($tileSize / 2)
for ($ty = 0; $ty -le $height; $ty++) {
    for ($tx = 0; $tx -le $width; $tx++) {
        $key = '{0}{1}{2}{3}' -f [int](Solid ($tx - 1) ($ty - 1)), [int](Solid $tx ($ty - 1)), [int](Solid ($tx - 1) $ty), [int](Solid $tx $ty)
        if ($key -eq '0000') { continue }
        $bb = $lookup[$key]; $mask = $rimMask[$key]
        for ($py = 0; $py -lt $tileSize; $py++) {
            $wy = $ty * $tileSize - $half + $py
            if ($wy -lt 0 -or $wy -ge $ch) { continue }
            for ($px = 0; $px -lt $tileSize; $px++) {
                $wx = $tx * $tileSize - $half + $px
                if ($wx -lt 0 -or $wx -ge $cw) { continue }
                $src = $sheetImage.GetPixel($bb.x + $px, $bb.y + $py)
                if ($src.A -eq 0) { continue }
                if ($mask[$px, $py]) {
                    $canvas.SetPixel($wx, $wy, $src)
                } else {
                    $canvas.SetPixel($wx, $wy, $textureImage.GetPixel($wx % $textureImage.Width, $wy % $textureImage.Height))
                }
            }
        }
    }
}

if (Test-Path $Figure) {
    $figureImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Figure))
    $bottom = 0
    for ($py = $figureImage.Height - 1; $py -ge 0 -and $bottom -eq 0; $py--) {
        for ($px = 0; $px -lt $figureImage.Width; $px++) { if ($figureImage.GetPixel($px, $py).A -gt 0) { $bottom = $py; break } }
    }
    $floorY = 0
    for ($y = 0; $y -lt $height; $y++) { if ((Solid $FigureCellX $y) -and -not (Solid $FigureCellX ($y - 1))) { $floorY = $y; break } }
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $g.DrawImage($figureImage, [int]($FigureCellX * $tileSize + $half - $figureImage.Width / 2), [int]($floorY * $tileSize - $bottom - 1), $figureImage.Width, $figureImage.Height)
    $g.Dispose()
}

$big = New-Object System.Drawing.Bitmap ([int]($cw * $Scale)), ([int]($ch * $Scale))
$bg = [System.Drawing.Graphics]::FromImage($big)
$bg.InterpolationMode = 'NearestNeighbor'; $bg.PixelOffsetMode = 'Half'
$bg.DrawImage($canvas, 0, 0, $big.Width, $big.Height)
$bg.Dispose()
$big.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"saved $Out"
