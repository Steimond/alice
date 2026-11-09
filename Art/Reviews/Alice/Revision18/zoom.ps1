# Frames of a folder enlarged, several per row, cropped to a box: zoom.ps1 -Dir d -Out o.png [-X 32 -Y 16 -W 72 -H 64 -Scale 4 -PerRow 8]
param([string]$Dir, [string]$Out, [int]$X = 32, [int]$Y = 16, [int]$W = 72, [int]$H = 64, [int]$Scale = 4, [int]$PerRow = 8)
Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem $Dir -Filter *.png | Sort-Object Name
$rows = [math]::Ceiling($files.Count / $PerRow)
$sheet = New-Object System.Drawing.Bitmap ([int]($PerRow * ($W * $Scale + 4))), ([int]($rows * ($H * $Scale + 4)))
$g = [System.Drawing.Graphics]::FromImage($sheet); $g.Clear([System.Drawing.Color]::FromArgb(58, 58, 64))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
for ($i = 0; $i -lt $files.Count; $i++) {
    $img = New-Object System.Drawing.Bitmap $files[$i].FullName
    $dest = New-Object System.Drawing.Rectangle (($i % $PerRow) * ($W * $Scale + 4)), ([math]::Floor($i / $PerRow) * ($H * $Scale + 4)), ($W * $Scale), ($H * $Scale)
    $g.DrawImage($img, $dest, (New-Object System.Drawing.Rectangle $X, $Y, $W, $H), [System.Drawing.GraphicsUnit]::Pixel)
    $img.Dispose()
}
$sheet.Save((Join-Path (Get-Location) $Out))
