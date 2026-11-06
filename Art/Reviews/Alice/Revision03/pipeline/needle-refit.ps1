# Refits the needle of finished Alice animations to her fists (owner's rule, 6 Nov 2026): for each set, the hand
# points of its JSON are snapped (needle-snap.ps1), the needle is drawn again on the clean frames (needle.ps1), and
# every new frame replaces the game frame that the old output was copied to (found by identical content, so names
# and GUIDs stay). The old output folder is left as it was; the new one is <final>-refit.
param(
    [string[]]$Sets = @(
        "fidget4|clean-fidget4|fidget4-needle.json|final-fidget4",
        "parry3|clean-parry3|parry3-needle.json|final-parry3",
        "slash2|clean-slash2|slash2-needle.json|final-slash2",
        "air2|clean-air2|air2-needle.json|final-air2",
        "thrust2|clean-thrust2|thrust2-needle.json|final-thrust2",
        "counter2|clean-counter2|counter2-needle.json|final-counter2",
        "spinC|clean-spinC|spinC-needle.json|final-spinC",
        "execA|clean-execA14|execA-needle.json|final-execA"
    ),
    [string]$GameDir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial"
)

$here = $PSScriptRoot
$md5 = [System.Security.Cryptography.MD5]::Create()
function Hash([string]$path) { [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($path))) }

$game = @{}
foreach ($f in Get-ChildItem $GameDir -Filter "alice_trial_*.png") { $game[(Hash $f.FullName)] = $f.FullName }

foreach ($set in $Sets)
{
    $name, $clean, $json, $final = $set.Split('|')
    $snapped = Join-Path $here ($json -replace '\.json$', '-snapped.json')
    $refit = Join-Path $here "$final-refit"
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $here "needle-snap.ps1") -CleanDir (Join-Path $here $clean) -Json (Join-Path $here $json) -OutJson $snapped | Out-Null
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $here "needle.ps1") -FramesDir (Join-Path $here $clean) -OutDir $refit -Json $snapped | Out-Null
    $copied = 0; $missing = 0
    foreach ($old in Get-ChildItem (Join-Path $here $final) -Filter *.png)
    {
        $target = $game[(Hash $old.FullName)]
        if ($null -eq $target) { $missing++; continue }
        Copy-Item (Join-Path $refit $old.Name) $target -Force
        $copied++
    }
    "{0}: {1} game frames replaced, {2} old frames not in the game" -f $name, $copied, $missing
}
