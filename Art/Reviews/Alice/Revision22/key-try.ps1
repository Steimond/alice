# Puts the key into Alice's hand on a few clean frames (no needle) to judge its size and look in play.
# Each entry: frame path, hand x, hand y, angle in degrees (0 = pointing forward, counter-clockwise).
param([string]$Out = 'key-in-hand.png', [int]$Scale = 4)
Add-Type -AssemblyName System.Drawing
$dir = Split-Path $MyInvocation.MyCommand.Path
$tries = @(
    @('..\Revision19\alice\tear-clean\f06.png', 70, 54, 247),
    @('..\Revision19\alice\duck1-clean\run-04.png', 90, 44, 0),
    @('..\Revision19\alice\moth2-clean\run-02.png', 72, 28, 112),
    @('..\Revision19\alice\duck1-clean\run-08.png', 43, 28, 135)
)
$sheet = New-Object System.Drawing.Bitmap (128 * $Scale * $tries.Count), (80 * $Scale)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(58, 58, 64))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
for ($i = 0; $i -lt $tries.Count; $i++)
{
    $t = $tries[$i]
    $frame = [System.Drawing.Bitmap]::FromFile((Join-Path $dir $t[0]))
    $step = [int]([Math]::Round($t[3] / 22.5)) % 16
    $rot = [System.Drawing.Bitmap]::FromFile((Join-Path $dir ("key\rot\key_{0:D3}.png" -f [int]($step * 22.5))))
    $canvas = New-Object System.Drawing.Bitmap 128, 80
    $cg = [System.Drawing.Graphics]::FromImage($canvas)
    # The key goes behind the fist: frame drawn over it, so the fingers close over the shaft.
    $cg.DrawImageUnscaled($rot, $t[1] - 50, $t[2] - 50)
    $cg.DrawImageUnscaled($frame, 0, 0)
    $cg.Dispose()
    $g.DrawImage($canvas, (New-Object System.Drawing.Rectangle (128 * $Scale * $i), 0, (128 * $Scale), (80 * $Scale)))
    $frame.Dispose(); $rot.Dispose(); $canvas.Dispose()
}
$sheet.Save((Join-Path $dir $Out))
