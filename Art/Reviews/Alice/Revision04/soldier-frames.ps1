# Copies the chosen card soldier frames into the game on one 80x64 canvas.
# The 64x64 generations stand off-centre: the card body sits around x 24 and the feet on row 60,
# while the spear thrust and the corpse reach the right edge. Shifting by (+16, +1) centres the body
# on the sprite pivot and keeps the blood pool rows below the feet; the sprite set then lowers the
# drawing by the two pixels between the feet and the canvas bottom.
param(
    [string]$Review = $PSScriptRoot,
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\..\..\Assets\_Alice\Art\Characters\Enemies\CardSoldier'),
    # Death variant folder; the game frames keep their names, so swapping keeps the sprite GUIDs.
    [string]$Death = 'enemy-death3-blood',
    # Only rewrite the thrust frames.
    [switch]$AttackOnly
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$Width = 80
$Height = 64
$OffsetX = 16
$OffsetY = 1

function Copy-Frame([string]$Source, [string]$Name, [int]$CanvasWidth = $Width, [int]$ShiftX = $OffsetX) {
    $src = [System.Drawing.Bitmap]::FromFile($Source)
    $dst = [System.Drawing.Bitmap]::new($CanvasWidth, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $src.Height; $y++) {
        for ($x = 0; $x -lt $src.Width; $x++) {
            $c = $src.GetPixel($x, $y)
            $tx = $x + $ShiftX; $ty = $y + $OffsetY
            # Hard alpha: the import keeps pixel art crisp only without half-transparent edges.
            if ($c.A -ge 128 -and $tx -ge 0 -and $tx -lt $CanvasWidth -and $ty -lt $Height) {
                $dst.SetPixel($tx, $ty, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
            }
        }
    }
    $dst.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $dst.Dispose(); $src.Dispose()
}

New-Item -ItemType Directory -Force $OutDir | Out-Null

# The weighted thrust (4 Nov 2026, enemy-attack4): generated on a 128x64 canvas with the stance moved
# 48 px right, so the drawn-back shaft and the full thrust both fit. In the game it sits on a 144x64 canvas
# with the card centred like every other frame. The generator stepped the body 28 px back while cocking the
# spear; the windup frames are pulled forward so the card leaves its collider by only about half a tile.
$attackShift = @{ 1 = 0; 2 = 16; 3 = 20; 4 = 20; 5 = 20; 6 = 20; 7 = 10; 8 = 0; 9 = 0; 10 = 0; 11 = 0 }
for ($i = 1; $i -le 11; $i++) {
    Copy-Frame (Join-Path $Review ("enemy-attack4\{0:D2}.png" -f $i)) ('card_soldier_attack_{0:D2}' -f ($i - 1)) 144 $attackShift[$i]
}
if ($AttackOnly) { return }
Copy-Frame (Join-Path $Review 'enemy-attack\0.png') 'card_soldier_idle_00'
# Index 0 of a PixMiniMax result is the input stand; the walk's last frame was pinned to it, so 1..8 loop.
for ($i = 1; $i -le 8; $i++) {
    $n = '{0:D2}' -f ($i - 1)
    Copy-Frame (Join-Path $Review "enemy-walk\$i.png") "card_soldier_walk_$n"
    Copy-Frame (Join-Path $Review "$Death\$i.png") "card_soldier_death_$n"
}

# Reactions by killing blow (GDD 0.18). The generator drew Alice's needle into the first two stab frames
# and a white blade smear into the first beheading frame, so those are skipped.
for ($i = 1; $i -le 4; $i++) { Copy-Frame (Join-Path $Review "enemy-hurt-blood\$i.png") ('card_soldier_hurt_{0:D2}' -f ($i - 1)) }
for ($i = 3; $i -le 12; $i++) { Copy-Frame (Join-Path $Review "enemy-stab-death-blood\$i.png") ('card_soldier_stab_{0:D2}' -f ($i - 3)) }
for ($i = 2; $i -le 8; $i++) { Copy-Frame (Join-Path $Review "enemy-behead-blood\$i.png") ('card_soldier_behead_{0:D2}' -f ($i - 2)) }

# Parry and counter. The opening's first frame carries a white blade arc, and the execution's first two
# show the needle still flying in, so they are skipped; from frame 3 the needle sits in the heart.
for ($i = 2; $i -le 8; $i++) { Copy-Frame (Join-Path $Review "enemy-opened-blood\$i.png") ('card_soldier_opened_{0:D2}' -f ($i - 2)) }
for ($i = 3; $i -le 12; $i++) { Copy-Frame (Join-Path $Review "enemy-execution-blood\$i.png") ('card_soldier_execution_{0:D2}' -f ($i - 3)) }

# Execution by tearing the card (GDD 0.18), played in sync with Alice's 1.3 s execution: three held stance
# frames cover her step and grab, so the needle reaches the heart as her blow lands and the card tears as
# she pulls her hands apart.
$tear = @(0, 0, 0) + (1..12)
for ($k = 0; $k -lt $tear.Count; $k++) { Copy-Frame (Join-Path $Review ("enemy-tear-blood\{0}.png" -f $tear[$k])) ('card_soldier_tear_{0:D2}' -f $k) }
