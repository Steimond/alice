# Snaps the hand points of a needle JSON onto the hand actually drawn in each clean frame (owner's rule, 6 Nov 2026:
# the needle always comes out of the visible hand; eyeballed points miss by a few pixels and the needle floats).
# For every frame the point moves to the skin pixel nearest it (within -Radius): a point already on the fist stays,
# one in the air next to it lands on it. The fist's centre or its far edge were tried and rejected: with bare
# forearms both slide up the arm toward the elbow. Other fields of the entry are kept. Frames with no skin near the
# point are reported and left as they are: look at them by eye (a hand in deep shadow may not read as skin).
param(
    [Parameter(Mandatory)][string]$CleanDir,
    [Parameter(Mandatory)][string]$Json,
    [Parameter(Mandatory)][string]$OutJson,
    [int]$Radius = 7
)

Add-Type -AssemblyName System.Drawing

function IsSkin([System.Drawing.Color]$c)
{
    return $c.A -ge 128 -and ($c.R - $c.B) -gt 55 -and $c.R -gt 150 -and $c.G -gt 100
}

$specs = Get-Content $Json -Raw | ConvertFrom-Json
$files = Get-ChildItem $CleanDir -Filter *.png | Sort-Object Name
if ($files.Count -ne $specs.Count) { throw "Frames ($($files.Count)) and JSON entries ($($specs.Count)) differ." }

for ($i = 0; $i -lt $files.Count; $i++)
{
    $s = $specs[$i]
    $b = New-Object System.Drawing.Bitmap $files[$i].FullName
    # needle.ps1 entries carry hx/hy on the 128x80 canvas; cleanup.ps1 overrides carry x/y on the 64x64 source.
    $source = $null -eq $s.hx
    $hx = if ($source) { [int]$s.x } else { [int]$s.hx }
    $hy = if ($source) { [int]$s.y } else { [int]$s.hy }
    $best = $null; $bestD = [double]::MaxValue
    for ($y = [Math]::Max(0, $hy - $Radius); $y -le [Math]::Min($b.Height - 1, $hy + $Radius); $y++)
    {
        for ($x = [Math]::Max(0, $hx - $Radius); $x -le [Math]::Min($b.Width - 1, $hx + $Radius); $x++)
        {
            if (-not (IsSkin $b.GetPixel($x, $y))) { continue }
            $d = ($x - $hx) * ($x - $hx) + ($y - $hy) * ($y - $hy)
            if ($d -lt $bestD) { $bestD = $d; $best = @($x, $y) }
        }
    }
    $b.Dispose()

    if ($null -eq $best)
    {
        "{0}: NO SKIN near ({1},{2}) - check by eye" -f $files[$i].Name, $hx, $hy
        continue
    }

    if ($best[0] -ne $hx -or $best[1] -ne $hy) { "{0}: ({1},{2}) -> ({3},{4})" -f $files[$i].Name, $hx, $hy, $best[0], $best[1] }
    if ($source) { $s.x = $best[0]; $s.y = $best[1] } else { $s.hx = $best[0]; $s.hy = $best[1] }
}

# One entry per line, as the hand-written files are.
$lines = $specs | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 5 }
Set-Content -Path $OutJson -Value ("[`n" + ($lines -join ",`n") + "`n]") -Encoding UTF8
