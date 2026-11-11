# Prepares the raw PixMiniMax frames of the turning combo (owner, 10 Nov 2026: three ordinary strikes - two flat
# sweeps to and fro and a finisher out of a full turn; Alice may turn any way she likes, the sword only never changes
# hands) up to clean frames on the game canvas: strip-smears.ps1 -> cleanup.ps1 -SkipNeedle -> specks.ps1.
# The sword is drawn afterwards by render-combo.ps1 from combo.json.
# fallLoopA was generated from a 64 x 64 window cut at x = 32 of the game canvas, while cleanup.ps1 puts frames at
# x + 36: after this script its clean frames were moved 4 px left once, by hand (README, fourth round). Do not
# rebuild that set without moving it again.
param([string[]]$Sets = @('s1D', 's1in'))
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$reviews = Split-Path -Parent $here
$p03 = Join-Path $reviews 'Revision03\pipeline'
$stand = Join-Path $p03 'stand\stand-noneedle.png'
$kp = Join-Path $reviews 'Revision19\alice\kp.json'
$specks = Join-Path $reviews 'Revision23\specks.ps1'
Push-Location (Join-Path $here 'alice')
try {
    foreach ($name in $Sets) {
        if (-not (Test-Path "$name-raw")) { "{0}: no raw frames" -f $name; continue }
        foreach ($d in "$name-stripped", "$name-clean") { if (Test-Path $d) { Remove-Item -Recurse -Force $d } }
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $p03 'strip-smears.ps1') -InDir "$name-raw" -OutDir "$name-stripped" -PaletteFrom $stand | Out-Null
        & powershell -NoProfile -ExecutionPolicy Bypass -Command "& '$p03\cleanup.ps1' -FramesDir '$name-stripped' -PaletteFrom '$stand' -Keypoints '$kp' -OutDir '$name-clean' -Airborne @() -SkipNeedle" | Out-Null
        & powershell -NoProfile -ExecutionPolicy Bypass -File $specks -Dir "$name-clean" | Out-Null
        "{0}: {1} clean frames" -f $name, @(Get-ChildItem "$name-clean" -Filter *.png).Count
    }
}
finally { Pop-Location }
