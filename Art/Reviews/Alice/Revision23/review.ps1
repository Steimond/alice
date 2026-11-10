# Review sheet of one clip: every rendered frame enlarged and numbered; a red frame marks a frame the forearm check
# flags, a yellow one a hand correction from overrides.json; a cyan dot marks the grip point.
param([Parameter(Mandatory)][string]$Clip, [Parameter(Mandatory)][string]$Out, [string]$Dir = '', [int]$S = 3, [int]$Cols = 8,
      [int]$Top = 4, [int]$Hgt = 76, [string]$Report = '')
Add-Type -AssemblyName System.Drawing
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Dir) { $Dir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Alice\Trial" }
$rep = @{}
if (-not $Report) { $Report = Join-Path $here 'grip-report.csv' }
foreach ($l in Get-Content $Report | Select-Object -Skip 1) { $f = $l.Split(','); $rep[$f[0]] = $f }
$files = @(Get-ChildItem $Dir -Filter "alice_trial_$($Clip)_*.png" | Where-Object { $_.BaseName -match "^alice_trial_$($Clip)_\d+$" } | Sort-Object Name)
$rows = [math]::Ceiling($files.Count / $Cols)
$cw = 128 * $S; $ch = $Hgt * $S
$b = New-Object System.Drawing.Bitmap ([int]($cw * [math]::Min($Cols, $files.Count))), ([int]($ch * $rows))
$g = [System.Drawing.Graphics]::FromImage($b); $g.Clear([System.Drawing.Color]::FromArgb(58, 58, 64))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$font = New-Object System.Drawing.Font 'Consolas', 14
for ($i = 0; $i -lt $files.Count; $i++) {
    $im = [System.Drawing.Bitmap]::FromFile($files[$i].FullName)
    $x = ($i % $Cols) * $cw; $y = [math]::Floor($i / $Cols) * $ch
    $g.DrawImage($im, (New-Object System.Drawing.Rectangle $x, $y, $cw, $ch), (New-Object System.Drawing.Rectangle 0, $Top, 128, $Hgt), 'Pixel')
    $r = $rep[$files[$i].Name]
    $col = [System.Drawing.Color]::FromArgb(90, 90, 100); $w = 1
    if ($r -and $r[8] -match 'override') { $col = [System.Drawing.Color]::Gold; $w = 3 }
    elseif ($r -and $r[8] -match 'suspect') { $col = [System.Drawing.Color]::Red; $w = 3 }
    $g.DrawRectangle((New-Object System.Drawing.Pen $col, $w), $x + 1, $y + 1, $cw - 3, $ch - 3)
    $g.DrawString(("{0:D2}" -f $i), $font, [System.Drawing.Brushes]::White, $x + 4, $y + 2)
    if ($r -and $r[1]) {
        $gx = $x + [double]$r[1] * $S; $gy = $y + ([double]$r[2] - $Top) * $S
        $g.FillEllipse([System.Drawing.Brushes]::Cyan, [float]($gx - 2), [float]($gy - 2), 4, 4)
    }
    $im.Dispose()
}
$b.Save($Out)
