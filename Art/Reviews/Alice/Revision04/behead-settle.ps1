# Ends the card soldier's beheading with blood that comes to rest (owner's note, 4 Nov 2026: the last
# frame froze the spray in mid-air). A PixMiniMax continuation of the last frame shows the spray falling;
# its frames 1-3 go through the shared blood palette, and a final still frame is frame 3 with every small
# blob that floats free of the floor removed. The frames are appended after card_soldier_behead_06.
param(
    [string]$Review = $PSScriptRoot,
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\..\..\Assets\_Alice\Art\Characters\Enemies\CardSoldier')
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$raw = Join-Path $Review 'enemy-behead-settle'
$pick = Join-Path $Review 'enemy-behead-settle-pick'
$blood = Join-Path $Review 'enemy-behead-settle-blood'
New-Item -ItemType Directory -Force $pick, $blood | Out-Null
foreach ($i in 1..3) { Copy-Item -Force (Join-Path $raw "$i.png") (Join-Path $pick "$i.png") }
& (Join-Path $Review 'blood-palette.ps1') -InDir $pick -OutDir $blood

# Final frame: keep only what touches the floor band or is large; airborne droplets go.
$src = [System.Drawing.Bitmap]::FromFile((Join-Path $blood '3.png'))
$w = $src.Width; $h = $src.Height
$seen = New-Object 'bool[,]' $w, $h
$bottom = 0
for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { if ($src.GetPixel($x, $y).A -ge 128) { $bottom = $y } } }
$still = [System.Drawing.Bitmap]::new($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt $h; $y++) {
    for ($x = 0; $x -lt $w; $x++) {
        if ($seen[$x, $y] -or $src.GetPixel($x, $y).A -lt 128) { continue }
        $blob = New-Object System.Collections.Generic.List[int[]]
        $stack = New-Object System.Collections.Generic.Stack[int[]]
        $stack.Push(@($x, $y)); $seen[$x, $y] = $true
        $grounded = $false
        while ($stack.Count -gt 0) {
            $p = $stack.Pop(); $blob.Add($p)
            if ($p[1] -ge $bottom - 3) { $grounded = $true }
            foreach ($d in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
                $nx = $p[0] + $d[0]; $ny = $p[1] + $d[1]
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $w -and $ny -lt $h -and -not $seen[$nx, $ny] -and $src.GetPixel($nx, $ny).A -ge 128) {
                    $seen[$nx, $ny] = $true; $stack.Push(@($nx, $ny))
                }
            }
        }
        if ($grounded -or $blob.Count -ge 40) {
            foreach ($p in $blob) { $still.SetPixel($p[0], $p[1], $src.GetPixel($p[0], $p[1])) }
        }
    }
}
$still.Save((Join-Path $blood '4.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$still.Dispose(); $src.Dispose()

# Same 80x64 canvas and offset as soldier-frames.ps1.
foreach ($i in 1..4) {
    $s = [System.Drawing.Bitmap]::FromFile((Join-Path $blood "$i.png"))
    $d = [System.Drawing.Bitmap]::new(80, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $s.Height; $y++) { for ($x = 0; $x -lt $s.Width; $x++) {
        $c = $s.GetPixel($x, $y); $tx = $x + 16; $ty = $y + 1
        if ($c.A -ge 128 -and $tx -lt 80 -and $ty -lt 64) { $d.SetPixel($tx, $ty, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B)) }
    } }
    $d.Save((Join-Path $OutDir ('card_soldier_behead_{0:D2}.png' -f (6 + $i))), [System.Drawing.Imaging.ImageFormat]::Png)
    $d.Dispose(); $s.Dispose()
}

# The continuation thins the pool out; the blood should stay on the floor, so the last two frames get
# the floor band of the old final frame (its lowest four rows) wherever they are empty.
$pool = [System.Drawing.Bitmap]::FromFile((Join-Path $OutDir 'card_soldier_behead_06.png'))
$floor = 0
for ($y = 0; $y -lt $pool.Height; $y++) { for ($x = 0; $x -lt $pool.Width; $x++) { if ($pool.GetPixel($x, $y).A -ge 128) { $floor = $y } } }
foreach ($n in 9, 10) {
    $path = Join-Path $OutDir ('card_soldier_behead_{0:D2}.png' -f $n)
    $f = [System.Drawing.Bitmap]::FromFile($path); $c = [System.Drawing.Bitmap]::new($f); $f.Dispose()
    for ($y = $floor - 3; $y -le $floor; $y++) { for ($x = 0; $x -lt $c.Width; $x++) {
        $p = $pool.GetPixel($x, $y)
        if ($p.A -ge 128 -and $c.GetPixel($x, $y).A -lt 128) { $c.SetPixel($x, $y, $p) }
    } }
    $c.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $c.Dispose()
}
$pool.Dispose()
