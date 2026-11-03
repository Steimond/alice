# Copies the chosen card soldier frames into the game on one 80x64 canvas.
# The 64x64 generations stand off-centre: the card body sits around x 24 and the feet on row 60,
# while the spear thrust and the corpse reach the right edge. Shifting by (+16, +1) centres the body
# on the sprite pivot and keeps the blood pool rows below the feet; the sprite set then lowers the
# drawing by the two pixels between the feet and the canvas bottom.
param(
    [string]$Review = $PSScriptRoot,
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\..\..\Assets\_Alice\Art\Characters\Enemies\CardSoldier'),
    # Death variant folder; the game frames keep their names, so swapping keeps the sprite GUIDs.
    [string]$Death = 'enemy-death3-blood'
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$Width = 80
$Height = 64
$OffsetX = 16
$OffsetY = 1

function Copy-Frame([string]$Source, [string]$Name) {
    $src = [System.Drawing.Bitmap]::FromFile($Source)
    $dst = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $src.Height; $y++) {
        for ($x = 0; $x -lt $src.Width; $x++) {
            $c = $src.GetPixel($x, $y)
            $tx = $x + $OffsetX; $ty = $y + $OffsetY
            # Hard alpha: the import keeps pixel art crisp only without half-transparent edges.
            if ($c.A -ge 128 -and $tx -lt $Width -and $ty -lt $Height) {
                $dst.SetPixel($tx, $ty, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
            }
        }
    }
    $dst.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $dst.Dispose(); $src.Dispose()
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
Copy-Frame (Join-Path $Review 'enemy-attack\0.png') 'card_soldier_idle_00'
# Index 0 of a PixMiniMax result is the input stand; the walk's last frame was pinned to it, so 1..8 loop.
for ($i = 1; $i -le 8; $i++) {
    $n = '{0:D2}' -f ($i - 1)
    Copy-Frame (Join-Path $Review "enemy-walk\$i.png") "card_soldier_walk_$n"
    Copy-Frame (Join-Path $Review "enemy-attack\$i.png") "card_soldier_attack_$n"
    Copy-Frame (Join-Path $Review "$Death\$i.png") "card_soldier_death_$n"
}
