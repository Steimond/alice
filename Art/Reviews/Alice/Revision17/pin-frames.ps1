# The thrown clothespin: hard alpha, cropped to a square around its middle, then four quarter turns so the spin
# stays on the pixel grid. Writes duck_pin_00..03.png next to the duck frames.
param([string]$In = "duck/pin.png", [string]$OutDir = "../../../../Assets/_Alice/Art/Characters/Enemies/Duck")
Add-Type -AssemblyName System.Drawing
$src = New-Object System.Drawing.Bitmap ((Resolve-Path $In).Path)
$size = 24
$square = New-Object System.Drawing.Bitmap $size, $size
for ($y = 0; $y -lt $src.Height; $y++) { for ($x = 0; $x -lt $src.Width; $x++) {
    $c = $src.GetPixel($x, $y)
    if ($c.A -ge 128) { $square.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c)) }
} }
$turns = @([System.Drawing.RotateFlipType]::RotateNoneFlipNone, [System.Drawing.RotateFlipType]::Rotate90FlipNone,
    [System.Drawing.RotateFlipType]::Rotate180FlipNone, [System.Drawing.RotateFlipType]::Rotate270FlipNone)
for ($i = 0; $i -lt 4; $i++) {
    $copy = $square.Clone()
    $copy.RotateFlip($turns[$i])
    $copy.Save((Join-Path (Resolve-Path $OutDir) ("duck_pin_{0:D2}.png" -f $i)), [System.Drawing.Imaging.ImageFormat]::Png)
    $copy.Dispose()
}
