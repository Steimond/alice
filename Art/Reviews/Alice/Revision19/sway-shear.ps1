# A seamless sway for a hanging cloth, from one still frame: each row shifts sideways by a whole number of pixels,
# nothing at the pegs and most at the hem, with a wave running down the cloth. PixMiniMax drew horizontal seams
# into every sway it made of the laundry sheet (Revision17 and three tries in Revision19), and a shear cannot.
# Usage: sway-shear.ps1 -In look.png -OutDir dir [-Frames 8] [-Top 10] [-Amplitude 2.2] [-Wave 0.09]
# Writes 00..(Frames-1).png; frame 0 equals the input, so the loop starts and ends on the still sheet.
param(
    [Parameter(Mandatory)][string]$In,
    [Parameter(Mandatory)][string]$OutDir,
    [int]$Frames = 8,
    [int]$Top = 10,            # rows above this (pegs and the top hem) never move
    [double]$Amplitude = 2.2,  # pixels at the hem at the widest swing
    [double]$Wave = 0.09       # radians of phase lag per row: the swing reaches the hem later than the top
)

Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null
$src = New-Object System.Drawing.Bitmap (Resolve-Path $In).Path
$w = $src.Width; $h = $src.Height
for ($f = 0; $f -lt $Frames; $f++)
{
    $phase = 2 * [math]::PI * $f / $Frames
    $out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $h; $y++)
    {
        $t = if ($y -le $Top) { 0.0 } else { [math]::Pow(($y - $Top) / ($h - 1 - $Top), 1.4) }
        # sin(phase - lag) - sin(-lag) keeps frame 0 exactly the still frame on every row.
        $lag = ($y - $Top) * $Wave
        $shift = [int][math]::Round($Amplitude * $t * ([math]::Sin($phase - $lag) - [math]::Sin(-$lag)) * 0.5)
        for ($x = 0; $x -lt $w; $x++)
        {
            $sx = $x - $shift
            if ($sx -lt 0 -or $sx -ge $w) { continue }
            $c = $src.GetPixel($sx, $y)
            if ($c.A -ge 128) { $out.SetPixel($x, $y, $c) }
        }
    }
    $out.Save((Join-Path (Resolve-Path $OutDir).Path ("{0:D2}.png" -f $f)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}
$src.Dispose()
"{0} frames" -f $Frames
