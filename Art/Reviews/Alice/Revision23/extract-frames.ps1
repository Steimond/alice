# Builds frames.json for render-sword.ps1: for every Alice game frame, a clean source frame without any weapon and
# where the sword goes. Run once after the art changes; corrections go to overrides.json, not here.
#   1. The game frames as they were with the needle are kept once in needle-backup/.
#   2. Sets with clean frames and a hand JSON: each backup frame is matched to its needle output by identical
#      content; the source is the clean frame of the same name, the spec is its JSON entry.
#   3. The strikes drawn for the sword (build-strikes.ps1): clean frames in game order with their hand JSON.
#   4. Everything else with a needle: needle-swap.ps1 erases the needle and reports where it was.
# Frames with no weapon at all (small Alice, shrink, bite) are not listed.
param([string]$GameDir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial")
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$alice = Split-Path -Parent $here
$p03 = Join-Path $alice 'Revision03\pipeline'
$backup = Join-Path $here 'needle-backup'
$cleanAll = Join-Path $here 'frames\clean'
if (-not (Test-Path $backup)) {
    New-Item -ItemType Directory $backup | Out-Null
    Copy-Item (Join-Path $GameDir 'alice_trial_*.png') $backup
}
if (Test-Path $cleanAll) { Remove-Item -Recurse -Force $cleanAll }
New-Item -ItemType Directory -Force $cleanAll | Out-Null
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $here 'fork-erase.ps1') | Out-Null
$md5 = [System.Security.Cryptography.MD5]::Create()
function HashOf([string]$p) { [BitConverter]::ToString($md5.ComputeHash([IO.File]::ReadAllBytes($p))) }
function Entry($name, $src, $spec, [bool]$lock) {
    $e = [ordered]@{ name = $name; src = $src }
    foreach ($p in $spec.PSObject.Properties) { $e[$p.Name] = $p.Value }
    if ($lock) { $e.lock = $true }
    $e
}

# name | clean frames | snapped hand JSON | needle output that went into the game | lock (angles used as drawn)
$sets = @(
    "spinC|$p03\clean-spinC|$p03\spinC-needle-snapped.json|$p03\final-spinC-refit|",
    "execA|$here\alice\execA-clean-fixed|$p03\execA-needle-snapped.json|$p03\final-execA-refit|",
    "fidget4|$p03\clean-fidget4|$p03\fidget4-needle-snapped.json|$p03\final-fidget4-refit|",
    # Running: the blade trails behind the swinging fist as drawn, so it never flips between frames.
    "run|$alice\Revision15\run\clean-a|$alice\Revision15\run\run2-needle-snapped.json|$alice\Revision15\run\final-a|lock",
    "moth2|$alice\Revision19\alice\moth2-clean|$alice\Revision19\alice\moth2-needle-snapped.json|$alice\Revision19\alice\moth2-final|",
    "tear|$alice\Revision19\alice\tear-clean|$alice\Revision19\alice\tear-needle-snapped.json|$alice\Revision19\alice\tear-final|",
    "sheet|$alice\Revision18\sheet-c-clean|$alice\Revision18\sheet-c-needle-snapped.json|$alice\Revision18\sheet-c-final|",
    "rat|$alice\Revision21\alice\rat2-cut|$here\alice\rat-hand-snapped.json|$alice\Revision21\alice\rat2-final|"
)
$out = New-Object System.Collections.ArrayList
$done = @{}
foreach ($set in $sets) {
    $name, $clean, $json, $final, $lock = $set.Split('|')
    $files = @(Get-ChildItem $clean -Filter *.png | Sort-Object Name | ForEach-Object Name)
    $specs = Get-Content $json -Raw | ConvertFrom-Json
    $byHash = @{}
    foreach ($f in Get-ChildItem $final -Filter *.png) { $byHash[(HashOf $f.FullName)] = $f.Name }
    $n = 0
    foreach ($b in Get-ChildItem $backup -Filter *.png | Sort-Object Name) {
        if ($done.ContainsKey($b.Name)) { continue }
        $src = $byHash[(HashOf $b.FullName)]
        if ($null -eq $src) { continue }
        $i = [array]::IndexOf($files, $src)
        [void]$out.Add((Entry $b.Name (Join-Path $clean $src) $specs[$i] ($lock -eq 'lock')))
        $done[$b.Name] = 1; $n++
    }
    "{0}: {1} frames" -f $name, $n
}

# The strikes drawn for the sword: name | game prefix | order of clean frames (empty = as they are).
$strikes = @(
    # Fourth round: swings round the body in depth (foreshortened blade, trail across the body), Alice in profile.
    'slashD|alice_trial_slash_|',
    'finC|alice_trial_finisher_|00,01,02,04,06,07,09,09,10,11,12,13,14,14,15,15',
    # The duck redrawn without turning round (owner: the sword went to the far hand); the wrench lands on frame 7 as before.
    'duckA|alice_trial_exec_duck_|00,01,02,03,04,05,06,11,11,12,12,12,13,14,15,15',
    'parryC|alice_trial_parry_|',
    'cutF|alice_trial_cut_|',
    'airB|alice_trial_airslash_|00,01,02,03,04,06,07,07,08,09,10,11',
    'counterA|alice_trial_counter_|00,02,03,04,06,07,08,09,09,10,11,11'
)
foreach ($s in $strikes) {
    $name, $prefix, $order = $s.Split('|')
    $clean = Join-Path $here "alice\$name-clean"
    $files = @(Get-ChildItem $clean -Filter *.png | Sort-Object Name | ForEach-Object Name)
    $specs = Get-Content (Join-Path $here "alice\$name-hand-snapped.json") -Raw | ConvertFrom-Json
    $pick = if ($order) { $order.Split(',') | ForEach-Object { "run-$_.png" } } else { $files }
    $i = 0
    foreach ($f in $pick) {
        $game = "{0}{1:D2}.png" -f $prefix, $i
        [void]$out.Add((Entry $game (Join-Path $clean $f) $specs[[array]::IndexOf($files, $f)] $false))
        $done[$game] = 1; $i++
    }
    "{0}: {1} frames" -f $name, $i
}

# The rest: erase the needle in the game frame itself and take its place for the sword.
$skip = '^alice_trial_(small_|shrink_|bite_|thrust_)'
$swapIn = Join-Path $here 'frames\swap-in'
if (Test-Path $swapIn) { Remove-Item -Recurse -Force $swapIn }
New-Item -ItemType Directory $swapIn | Out-Null
foreach ($b in Get-ChildItem $backup -Filter *.png) {
    if ($done.ContainsKey($b.Name) -or $b.Name -match $skip) { continue }
    Copy-Item $b.FullName $swapIn
}
$specFile = Join-Path $here 'frames\swap-spec.json'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $here 'needle-swap.ps1') -Files $swapIn -OutDir (Join-Path $here 'frames\swap-out') `
    -CleanOut $cleanAll -SpecOut $specFile `
    -StuckMatch '^alice_trial_rest_(0[4-9]|[12][0-9])\.png$' -StuckX 77 -StuckY 49 -StuckLength 30 `
    -EraseMatch '^alice_trial_pickup_' -BehindMatch '^alice_trial_jump_0[0-3]\.png$' | Out-Null
$swapSpecs = Get-Content $specFile -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($e in $swapSpecs) {
    $spec = [pscustomobject]@{}
    foreach ($p in $e.PSObject.Properties) { if ($p.Name -ne 'name') { $spec | Add-Member $p.Name $p.Value } }
    # The swap already chose the grip; the forearm check may still turn a reverse grip.
    [void]$out.Add((Entry $e.name (Join-Path $cleanAll $e.name) $spec $false))
}
"swapped: {0} frames" -f $swapSpecs.Count

$sorted = @($out | Sort-Object { $_.name })
ConvertTo-Json $sorted -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $here 'frames.json')
"frames.json: {0} frames" -f $sorted.Count
