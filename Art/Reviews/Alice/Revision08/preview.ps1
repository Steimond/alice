# Enlarged previews on the room's grey backdrop, nearest-neighbour, so the art is judged against what it stands on.
param(
    # Comma-separated: powershell -File passes an array as one string.
    [Parameter(Mandatory)][string]$Files,
    [Parameter(Mandatory)][string]$Out,
    [int]$Scale = 4,
    [int]$Gap = 8
)

Add-Type -AssemblyName System.Drawing
$images = @($Files.Split(',') | ForEach-Object { New-Object System.Drawing.Bitmap ((Resolve-Path $_).Path) })
$width = $Gap
$tallest = 0
foreach ($image in $images)
{
    $width += $image.Width * $Scale + $Gap
    $tallest = [Math]::Max($tallest, $image.Height)
}

$height = $tallest * $Scale + $Gap * 2
$sheet = New-Object System.Drawing.Bitmap ([int]$width), ([int]$height)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(255, 58, 58, 64))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$x = $Gap
foreach ($image in $images)
{
    $y = $height - $Gap - $image.Height * $Scale
    $g.DrawImage($image, $x, $y, $image.Width * $Scale, $image.Height * $Scale)
    $x += $image.Width * $Scale + $Gap
    $image.Dispose()
}

$g.Dispose()
$sheet.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
