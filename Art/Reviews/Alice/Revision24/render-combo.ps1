# Draws the mirror-shard sword and its slash into the clean frames of the turning combo (combo.json).
#
# combo.json: { "<clip>": { "prefix": "alice_trial_slash_", "frames": [ { src, hx, hy, deg, ... } ] } }
#   src     clean frame without a weapon, relative to alice/
#   hx, hy  centre of the fist that holds the sword (game canvas 128 x 80)
#   deg     blade direction: 0 = right, counter-clockwise, 90 = up
#   behind  true: the sword hand is on the far side of the body - sword and slash are drawn only outside Alice
#   none    true: no sword in this frame
#   erase   [[x0, y0, x1, y1], ...] boxes cleared before drawing (a streak the generator left)
#   slash   { cx, cy, r, ry, from, to, width, fade } - the swing as a thick crescent. The blade tip runs along an
#           ellipse (centre cx, cy; half-width r; half-height ry: a flat circle seen a little from above) from angle
#           "from" to "to" (0 = right, 90 = the far side, 270 = the side nearest the viewer). The crescent is thin
#           at "from" and "width" px thick at "to", where the blade is. fade 1 = the frame after the blow: shorter,
#           thinner, darker. The half nearest the viewer is drawn over Alice, the far half is hidden by her.
#           from > to runs the swing clockwise (a backhand, a chop from above). front true: the swing lies in the
#           plane of the picture (a chop), all of it is drawn over Alice.
#
# What differs from Revision23/render-sword.ps1 (owner, 10 Nov 2026: "the fist is huge and merged with the hilt"):
# the whole hand - the skin and its dark outline - is put back over the sword, so the hilt sits inside the fist and
# only the pommel and the blade show; nothing is foreshortened, the blade always keeps its full length.
param(
    [string]$Spec = '',
    [string]$Only = '',          # regex on clip names
    [string]$OutDir = '',        # default: out/ next to the script
    [string]$GameDir = '',       # when given, frames are also written there under prefix + NN.png
    [int]$Steps = 32,
    [double]$FistRadius = 3.2
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Spec) { $Spec = Join-Path $here 'combo.json' }
if (-not $OutDir) { $OutDir = Join-Path $here 'out' }
$rotDir = Join-Path (Split-Path -Parent $here) 'Revision23\shard\rot'

$slashCore = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
$slashBody = [System.Drawing.Color]::FromArgb(255, 214, 220, 226)
$slashEdge = [System.Drawing.Color]::FromArgb(255, 150, 160, 176)
$slashDim = [System.Drawing.Color]::FromArgb(255, 110, 120, 138)

function Rad($deg) { $deg * [math]::PI / 180 }
function IsSkin([System.Drawing.Color]$c) { $c.A -ge 128 -and ($c.R - $c.B) -gt 50 -and $c.R -gt 140 -and $c.G -gt 95 }
function IsDark([System.Drawing.Color]$c) { $c.A -ge 128 -and ($c.R + $c.G + $c.B) -lt 150 }

$rots = @()
for ($s = 0; $s -lt $Steps; $s++) {
    $file = [System.Drawing.Bitmap]::FromFile((Join-Path $rotDir ("sword_{0:D3}.png" -f [int](360.0 * $s / $Steps))))
    $c = [int]($file.Width / 2)
    $px = New-Object System.Collections.ArrayList
    for ($y = 0; $y -lt $file.Height; $y++) { for ($x = 0; $x -lt $file.Width; $x++) {
        $p = $file.GetPixel($x, $y)
        if ($p.A -ge 128) { [void]$px.Add(@(($x - $c), ($y - $c), $p)) } } }
    $file.Dispose()
    $rots += , $px
}

$all = Get-Content $Spec -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($clip in $all.PSObject.Properties) {
    if ($clip.Name -like '_*') { continue }
    if ($Only -and $clip.Name -notmatch $Only) { continue }
    $target = Join-Path $OutDir $clip.Name
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    New-Item -ItemType Directory -Force $target | Out-Null
    $index = 0
    foreach ($s in $clip.Value.frames) {
        $file = [System.Drawing.Bitmap]::FromFile((Join-Path $here ("alice\" + $s.src)))
        $img = [System.Drawing.Bitmap]::new($file); $file.Dispose()
        if ($null -ne $s.erase) {
            foreach ($box in $s.erase) {
                for ($y = [int]$box[1]; $y -le [int]$box[3]; $y++) { for ($x = [int]$box[0]; $x -le [int]$box[2]; $x++) {
                    $img.SetPixel($x, $y, [System.Drawing.Color]::Transparent) } }
            }
        }
        $clean = [System.Drawing.Bitmap]::new($img)
        $name = "{0}{1:D2}.png" -f $clip.Value.prefix, $index
        $index++
        if (-not $s.none) {
            $behind = [bool]$s.behind
            $hx = [int][math]::Floor([double]$s.hx + 0.5); $hy = [int][math]::Floor([double]$s.hy + 0.5)

            if ($null -ne $s.slash) {
                $sl = $s.slash
                $from = [double]$sl.from; $to = [double]$sl.to
                $fade = [bool]$sl.fade
                $width = [double]$sl.width
                if ($fade) { $from = $from + ($to - $from) * 0.4; $width = $width * 0.55 }
                # The outer edge follows the tip of the blade; the thickness is measured on the screen, toward the
                # centre of the swing, so the crescent stays fat where the blade passes nearest the viewer.
                $depthAt = @{}
                $front = [bool]$sl.front
                for ($t = [math]::Min($from, $to); $t -le [math]::Max($from, $to); $t += 0.4) {
                    $k = ($t - $from) / ($to - $from)          # 0 at the tail, 1 at the blade
                    $thick = $width * [math]::Sin([math]::PI * [math]::Pow($k, 1.3))
                    if ($thick -lt 0.8) { continue }
                    $ox = [double]$sl.cx + [double]$sl.r * [math]::Cos((Rad $t))
                    $oy = [double]$sl.cy - [double]$sl.ry * [math]::Sin((Rad $t))
                    $ux = [double]$sl.cx - $ox; $uy = [double]$sl.cy - $oy
                    $ul = [math]::Sqrt($ux * $ux + $uy * $uy); $ux /= $ul; $uy /= $ul
                    $far = (-not $front) -and [math]::Sin((Rad $t)) -gt 0.15
                    for ($d = 0; $d -le $thick; $d += 0.5) {
                        $x = [int][math]::Round($ox + $ux * $d); $y = [int][math]::Round($oy + $uy * $d)
                        if ($x -lt 0 -or $y -lt 0 -or $x -ge 128 -or $y -ge 80) { continue }
                        if (($far -or $behind) -and $clean.GetPixel($x, $y).A -ge 128) { continue }
                        $depth = $d / $thick                    # 0 outer edge, 1 inner edge
                        $key = "$x,$y"
                        if (-not $depthAt.ContainsKey($key) -or $depthAt[$key] -gt $depth) { $depthAt[$key] = $depth }
                    }
                }
                foreach ($key in $depthAt.Keys) {
                    $x, $y = $key.Split(',') | ForEach-Object { [int]$_ }
                    $depth = $depthAt[$key]
                    $colour = if ($fade) { if ($depth -lt 0.5) { $slashEdge } elseif ((($x + $y) % 2) -eq 0) { $slashDim } else { $null } }
                              elseif ($depth -lt 0.4) { $slashCore } elseif ($depth -lt 0.8) { $slashBody }
                              elseif ((($x + $y) % 2) -eq 0) { $slashEdge } else { $null }
                    if ($null -ne $colour) { $img.SetPixel($x, $y, $colour) }
                }
            }

            $deg = [double]$s.deg
            $step = [int][math]::Round((($deg % 360) + 360) % 360 / (360.0 / $Steps)) % $Steps
            foreach ($p in $rots[$step]) {
                $px = $hx + $p[0]; $py = $hy + $p[1]
                if ($px -lt 0 -or $py -lt 0 -or $px -ge 128 -or $py -ge 80) { continue }
                if ($behind -and $clean.GetPixel($px, $py).A -ge 128) { continue }
                $img.SetPixel($px, $py, $p[2])
            }
            if (-not $behind) {
                # The hand goes back over the hilt whole: skin near the grip and the dark outline that touches it.
                for ($y = $hy - 4; $y -le $hy + 4; $y++) { for ($x = $hx - 4; $x -le $hx + 4; $x++) {
                    if ($x -lt 1 -or $y -lt 1 -or $x -ge 127 -or $y -ge 79) { continue }
                    $d = [math]::Sqrt(($x - $hx) * ($x - $hx) + ($y - $hy) * ($y - $hy))
                    if ($d -gt $FistRadius + 1) { continue }
                    $c = $clean.GetPixel($x, $y)
                    if ((IsSkin $c) -and $d -le $FistRadius) { $img.SetPixel($x, $y, $c); continue }
                    if (IsDark $c) {
                        $touch = $false
                        foreach ($o in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
                            $nx = $x + $o[0]; $ny = $y + $o[1]
                            $dn = [math]::Sqrt(($nx - $hx) * ($nx - $hx) + ($ny - $hy) * ($ny - $hy))
                            if ($dn -le $FistRadius -and (IsSkin $clean.GetPixel($nx, $ny))) { $touch = $true }
                        }
                        if ($touch) { $img.SetPixel($x, $y, $c) }
                    }
                } }
            }
        }
        $img.Save((Join-Path $target $name), [System.Drawing.Imaging.ImageFormat]::Png)
        if ($GameDir) { $img.Save((Join-Path $GameDir $name), [System.Drawing.Imaging.ImageFormat]::Png) }
        $img.Dispose(); $clean.Dispose()
    }
    "{0}: {1} frames -> {2}" -f $clip.Name, $index, $target
}
