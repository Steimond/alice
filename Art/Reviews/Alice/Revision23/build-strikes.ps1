# Prepares the strikes drawn for the mirror-shard sword (GDD 0.44) from their PixMiniMax frames, up to clean frames
# and snapped hand points; extract-frames.ps1 lists them and render-sword.ps1 draws the sword into the game frames.
# The strikes: the second cut of the combo (rising), the finisher (two-handed overhead chop, held), the counter (flat
# sweep) and the parry (blade upright before the face, knocked back over the head). Steps per strike:
# strip-smears.ps1 -> cleanup.ps1 -SkipNeedle -> drop the frame the generator smeared into a body-coloured arc ->
# specks.ps1 -> needle-snap.ps1 on the hand JSON.
param([string]$GameDir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial")
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$alice = Join-Path $here 'alice'
$p03 = Join-Path (Split-Path -Parent $here) 'Revision03\pipeline'
$stand = Join-Path $p03 'stand\stand-noneedle.png'
$kp = Join-Path (Split-Path -Parent $here) 'Revision19\alice\kp.json'

# name | frame dropped after cleanup | game prefix | order of clean frames in the game (empty = as they are)
$strikes = @(
    # The parry redrawn in one hand (owner, 10 Nov 2026: the old one turned to the camera and the needle changed hands).
    'parryC||alice_trial_parry_|',
    # Fourth round (owner, 10 Nov 2026): strikes that sweep round the body in depth, Alice kept in profile.
    'slashD||alice_trial_slash_|',
    'finC||alice_trial_finisher_|',
    'duckA||alice_trial_exec_duck_|',
    'cutF|run-04.png|alice_trial_cut_|',
    'airB|run-05.png|alice_trial_airslash_|',
    'counterA|run-05.png|alice_trial_counter_|'
)
foreach ($s in $strikes) {
    $name, $drop, $prefix, $order = $s.Split('|')
    Push-Location $alice
    try {
        foreach ($d in "$name-stripped", "$name-clean", "$name-final") { if (Test-Path $d) { Remove-Item -Recurse -Force $d } }
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $p03 'strip-smears.ps1') -InDir "$name-raw" -OutDir "$name-stripped" -PaletteFrom $stand | Out-Null
        & powershell -NoProfile -ExecutionPolicy Bypass -Command "& '$p03\cleanup.ps1' -FramesDir '$name-stripped' -PaletteFrom '$stand' -Keypoints '$kp' -OutDir '$name-clean' -Airborne @() -SkipNeedle" | Out-Null
        if ($drop) { Remove-Item (Join-Path "$name-clean" $drop) }
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $here 'specks.ps1') -Dir "$name-clean" | Out-Null
        if (-not (Test-Path "$name-hand.json")) { "{0}: clean frames ready, no hand JSON yet" -f $name; continue }
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $p03 'needle-snap.ps1') -CleanDir "$name-clean" -Json "$name-hand.json" -OutJson "$name-hand-snapped.json" | Out-Null
        "{0}: clean frames and snapped hand JSON ready" -f $name
    }
    finally { Pop-Location }
}
