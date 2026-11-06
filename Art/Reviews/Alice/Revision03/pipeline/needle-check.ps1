# Checks Alice's needle in finished frames (owner's rule, 6 Nov 2026: the needle always comes out of the visible
# hand). The needle is found by its three steel colours (needle.ps1, cleanup.ps1); a frame passes when some steel
# pixel touches skin (8-neighbourhood), i.e. the shaft starts in the fist. Frames with no needle are listed apart:
# for most animations that is a fault too.
# Usage: needle-check.ps1 -Files "C:\...\alice_trial_pickup_*.png"   (a wildcard or a folder)
param(
    [Parameter(Mandatory)][string]$Files
)

Add-Type -AssemblyName System.Drawing
$steel = @(
    [System.Drawing.Color]::FromArgb(255, 38, 40, 48).ToArgb(),
    [System.Drawing.Color]::FromArgb(255, 132, 140, 152).ToArgb(),
    [System.Drawing.Color]::FromArgb(255, 214, 220, 226).ToArgb()
)

function IsSkin([System.Drawing.Color]$c)
{
    # Warm and light: face and hands. Hair is warm too but never touches the needle's start in a fist.
    return $c.A -ge 128 -and ($c.R - $c.B) -gt 55 -and $c.R -gt 170 -and $c.G -gt 120
}

$paths = if (Test-Path $Files -PathType Container) { Get-ChildItem $Files -Filter *.png } else { Get-ChildItem $Files }
$bad = 0; $none = 0
foreach ($path in ($paths | Sort-Object Name))
{
    $b = New-Object System.Drawing.Bitmap $path.FullName
    $count = 0; $touch = 0
    for ($y = 0; $y -lt $b.Height; $y++)
    {
        for ($x = 0; $x -lt $b.Width; $x++)
        {
            if ($steel -notcontains $b.GetPixel($x, $y).ToArgb()) { continue }
            $count++
            for ($dy = -1; $dy -le 1; $dy++)
            {
                for ($dx = -1; $dx -le 1; $dx++)
                {
                    $nx = $x + $dx; $ny = $y + $dy
                    if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $b.Width -and $ny -lt $b.Height -and (IsSkin $b.GetPixel($nx, $ny))) { $touch++ }
                }
            }
        }
    }
    $b.Dispose()
    if ($count -eq 0) { "NO NEEDLE  {0}" -f $path.Name; $none++ }
    elseif ($touch -eq 0) { "DETACHED   {0}  ({1} steel px, none touching skin)" -f $path.Name, $count; $bad++ }
}
"checked {0}: detached {1}, without needle {2}" -f @($paths).Count, $bad, $none
