# Remaps the neon pure-red blood PixMiniMax draws onto the deep blood palette of the
# Pro Flash execution scene (gore-execution-flash.png), keeping its light-to-dark steps.
# The soldier's own reds (hood, heart, limbs) carry noticeable green and blue, so the
# near-zero G/B test leaves them alone.
param(
    [Parameter(Mandatory)] [string]$InDir,
    [Parameter(Mandatory)] [string]$OutDir
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

# Brightest to darkest, sampled from gore-execution-flash.png.
$ramp = @(
    @{ Min = 0xE6; Color = '#A01511' },
    @{ Min = 0xC8; Color = '#900C08' },
    @{ Min = 0xA8; Color = '#820A07' },
    @{ Min = 0x90; Color = '#730403' },
    @{ Min = 0x78; Color = '#650202' },
    @{ Min = 0x00; Color = '#570201' }
)
$glint = [System.Drawing.ColorTranslator]::FromHtml('#CC6351')

function Test-Blood($c) { $c.A -ge 128 -and $c.R -ge 0x60 -and $c.G -le 0x22 -and $c.B -le 0x22 }

New-Item -ItemType Directory -Force $OutDir | Out-Null
foreach ($file in Get-ChildItem $InDir -Filter *.png) {
    $src = [System.Drawing.Bitmap]::FromFile($file.FullName)
    $dst = $src.Clone()
    for ($y = 0; $y -lt $src.Height; $y++) {
        for ($x = 0; $x -lt $src.Width; $x++) {
            $c = $src.GetPixel($x, $y)
            if (Test-Blood $c) {
                $step = $ramp | Where-Object { $c.R -ge $_.Min } | Select-Object -First 1
                $dst.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($c.A, [System.Drawing.ColorTranslator]::FromHtml($step.Color)))
                continue
            }
            # Gloss streaks are near-white pixels inside the spray; the white card body
            # is not surrounded by blood, so it keeps its colour.
            if ($c.A -ge 128 -and $c.R -ge 0xD0 -and $c.G -ge 0xA0 -and $c.B -ge 0xA0) {
                $around = 0
                foreach ($o in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1))) {
                    $nx = $x + $o[0]; $ny = $y + $o[1]
                    if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $src.Width -and $ny -lt $src.Height -and (Test-Blood $src.GetPixel($nx, $ny))) { $around++ }
                }
                if ($around -ge 2) { $dst.SetPixel($x, $y, $glint) }
            }
        }
    }
    $dst.Save((Join-Path $OutDir $file.Name))
    $dst.Dispose(); $src.Dispose()
}
