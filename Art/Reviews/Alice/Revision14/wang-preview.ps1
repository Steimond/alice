# Раскладывает угловой Wang-тайлсет PixelLab (4x4, 16 тайлов) по тестовой карте
# в двойной сетке: карта задаётся клетками, тайл рисуется со сдвигом на полтайла
# и берёт углы из четырёх соседних клеток. Так граница массива проходит ровно
# по границе клеток, а коллизию можно строить по тем же клеткам.
param(
    [Parameter(Mandatory)] [string] $Sheet,
    [Parameter(Mandatory)] [string] $Meta,
    [Parameter(Mandatory)] [string] $Out,
    [string] $MapFile = '',
    [string] $Figure = '',
    [int] $FigureCellX = 6,
    [int] $Scale = 3,
    [string] $Back = '#2b2f38',
    # Папка с вариантами заливки 16x16: ими заменяется полностью внутренний тайл.
    [string] $FillDir = ''
)
Add-Type -AssemblyName System.Drawing
# В PowerShell 5.1 $PSScriptRoot пуст в значениях по умолчанию param().
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $MapFile) { $MapFile = Join-Path $scriptDir 'testmap.txt' }
if (-not $Figure) { $Figure = Join-Path $scriptDir '..\Revision03\pipeline\stand\stand-noneedle.png' }

$tilesetInfo = Get-Content $Meta -Raw | ConvertFrom-Json
$sheetImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Sheet))
[int] $tileSize = $tilesetInfo.tileset_data.tiles[0].bounding_box.width
$lookup = @{}
foreach ($tile in $tilesetInfo.tileset_data.tiles) {
    $c = $tile.corners
    $key = '{0}{1}{2}{3}' -f [int]($c.NW -eq 'lower'), [int]($c.NE -eq 'lower'), [int]($c.SW -eq 'lower'), [int]($c.SE -eq 'lower')
    $lookup[$key] = $tile.bounding_box
}

$fills = @()
if ($FillDir) {
    $fills = @(Get-ChildItem $FillDir -Filter *.png | Sort-Object Name | ForEach-Object { [System.Drawing.Bitmap]::FromFile($_.FullName) })
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

$canvas = New-Object System.Drawing.Bitmap ($width * $tileSize), ($height * $tileSize)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.Clear([System.Drawing.ColorTranslator]::FromHtml($Back))
$g.InterpolationMode = 'NearestNeighbor'
$g.PixelOffsetMode = 'Half'
# Тайл (x, y) двойной сетки лежит на пересечении клеток x-1..x и y-1..y.
for ($y = 0; $y -le $height; $y++) {
    for ($x = 0; $x -le $width; $x++) {
        $key = '{0}{1}{2}{3}' -f [int](Solid ($x - 1) ($y - 1)), [int](Solid $x ($y - 1)), [int](Solid ($x - 1) $y), [int](Solid $x $y)
        if ($key -eq '0000') { continue }
        $bb = $lookup[$key]
        $dest = New-Object System.Drawing.Rectangle (($x * $tileSize) - $tileSize / 2), (($y * $tileSize) - $tileSize / 2), $tileSize, $tileSize
        if ($key -eq '1111' -and $fills.Count -gt 0) {
            # Детерминированный «шум» по координате, чтобы раскладка не менялась от запуска к запуску.
            $hash = (($x * 73856093) -bxor ($y * 19349663)) -band 0x7fffffff
            $g.DrawImage($fills[$hash % $fills.Count], $dest, (New-Object System.Drawing.Rectangle 0, 0, $tileSize, $tileSize), 'Pixel')
            continue
        }
        $src = New-Object System.Drawing.Rectangle $bb.x, $bb.y, $tileSize, $tileSize
        $g.DrawImage($sheetImage, $dest, $src, 'Pixel')
    }
}

# Фигура Алисы для масштаба: ставится ногами на первый пол под колонкой FigureCellX.
if (Test-Path $Figure) {
    $figureImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Figure))
    $bottom = 0
    for ($py = $figureImage.Height - 1; $py -ge 0 -and $bottom -eq 0; $py--) {
        for ($px = 0; $px -lt $figureImage.Width; $px++) {
            if ($figureImage.GetPixel($px, $py).A -gt 0) { $bottom = $py; break }
        }
    }
    $floorY = 0
    for ($y = 0; $y -lt $height; $y++) {
        if ((Solid $FigureCellX $y) -and -not (Solid $FigureCellX ($y - 1))) { $floorY = $y; break }
    }
    $fx = $FigureCellX * $tileSize + $tileSize / 2 - $figureImage.Width / 2
    $fy = $floorY * $tileSize - $bottom - 1
    $g.DrawImage($figureImage, [int]$fx, [int]$fy, $figureImage.Width, $figureImage.Height)
}
$g.Dispose()

$big = New-Object System.Drawing.Bitmap ($canvas.Width * $Scale), ($canvas.Height * $Scale)
$bg = [System.Drawing.Graphics]::FromImage($big)
$bg.InterpolationMode = 'NearestNeighbor'
$bg.PixelOffsetMode = 'Half'
$bg.DrawImage($canvas, 0, 0, $big.Width, $big.Height)
$bg.Dispose()
$big.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"saved $Out ($($canvas.Width)x$($canvas.Height) px, x$Scale)"
