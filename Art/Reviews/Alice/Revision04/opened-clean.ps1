# Polish of the card soldier's "opened" frames, which also play while he stands stunned waiting for the execution
# (owner's note, 6 Nov 2026: bits hang in the air next to the spear, and the execution highlight outlines them).
# 1. Frame 00: the generator's swing arc right of the shaft (bright red, from (74,32) down to where it touches the
#    shaft at (62,45)) is erased: bright pixels right of the shaft in that box. The shaft itself is dark.
# 2. Every frame: detached pieces up to 12 px are erased (specks.ps1); the spear, a detached 200+ px piece, stays.
# The originals are kept in opened-orig/. Files are written in place, so their GUIDs stay.
param(
    [string]$Dir = "C:\Projects\alice\Assets\_Alice\Art\Characters\Enemies\CardSoldier"
)

Add-Type -AssemblyName System.Drawing
$backup = Join-Path $PSScriptRoot "opened-orig"
New-Item -ItemType Directory -Force $backup | Out-Null
foreach ($f in Get-ChildItem $Dir -Filter "card_soldier_opened_*.png")
{
    $kept = Join-Path $backup $f.Name
    if (-not (Test-Path $kept)) { Copy-Item $f.FullName $kept }
    # Always start from the original, so running this again gives the same result.
    Copy-Item $kept $f.FullName -Force
}

$path = Join-Path $Dir "card_soldier_opened_00.png"
$src = New-Object System.Drawing.Bitmap $path
$b = New-Object System.Drawing.Bitmap $src
$src.Dispose()
$erased = 0
for ($y = 30; $y -le 47; $y++)
{
    for ($x = 62; $x -le 76; $x++)
    {
        $c = $b.GetPixel($x, $y)
        if ($c.A -ge 128 -and $c.R -gt 170) { $b.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)); $erased++ }
    }
}
$b.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$b.Dispose()
"frame 00: swing arc, $erased px erased"

# 3. The spear head (frames 00-04, x 58+, y up to 24): pale grey 92857D and ivory DFCFC0 pixels belong to the card and
#    the shaft, not to the red head; there they read as glints out of the picture (owner's note, 6 Nov 2026). In
#    frame 00 they form a streak into the head, the rest of the swing arc. On the head's edge they are erased; inside
#    it they take the commonest colour of their neighbours.
$pale = @(
    [System.Drawing.Color]::FromArgb(255, 0x92, 0x85, 0x7D).ToArgb(),
    [System.Drawing.Color]::FromArgb(255, 0xDF, 0xCF, 0xC0).ToArgb()
)
foreach ($i in 0..4)
{
    $path = Join-Path $Dir ("card_soldier_opened_{0:00}.png" -f $i)
    $src = New-Object System.Drawing.Bitmap $path
    $b = New-Object System.Drawing.Bitmap $src
    $src.Dispose()
    $fixed = 0
    for ($y = 0; $y -le 24; $y++)
    {
        for ($x = 58; $x -lt $b.Width; $x++)
        {
            if ($pale -notcontains $b.GetPixel($x, $y).ToArgb()) { continue }
            $edge = $false; $votes = @{}
            foreach ($d in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1), @(-1, -1), @(1, 1), @(-1, 1), @(1, -1)))
            {
                $nx = $x + $d[0]; $ny = $y + $d[1]
                if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $b.Width -or $ny -ge $b.Height) { $edge = $true; continue }
                $c = $b.GetPixel($nx, $ny)
                if ($c.A -lt 128) { $edge = $true; continue }
                if ($pale -contains $c.ToArgb()) { continue }
                $votes[$c.ToArgb()] = 1 + [int]$votes[$c.ToArgb()]
            }
            if ($edge -or $votes.Count -eq 0) { $b.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)) }
            else
            {
                $best = ($votes.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key
                $b.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($best))
            }
            $fixed++
        }
    }
    $b.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $b.Dispose()
    "frame {0:00}: spear head, {1} pale px" -f $i, $fixed
}

# 4. Frames 00-01: bright red C04E3D on the middle of the shaft, where the swing arc met it; frames 02-04 have darker
#    blood there, so in motion it flashed. It takes that darker blood (7D2D26), the brightest shade the later frames
#    use on the shaft (found on the second review pass, 6 Nov 2026).
$bright = [System.Drawing.Color]::FromArgb(255, 0xC0, 0x4E, 0x3D).ToArgb()
$dark = [System.Drawing.Color]::FromArgb(255, 0x7D, 0x2D, 0x26)
foreach ($i in 0..1)
{
    $path = Join-Path $Dir ("card_soldier_opened_{0:00}.png" -f $i)
    $src = New-Object System.Drawing.Bitmap $path
    $b = New-Object System.Drawing.Bitmap $src
    $src.Dispose()
    $n = 0
    for ($y = 38; $y -le 52; $y++)
    {
        for ($x = 54; $x -le 68; $x++)
        {
            if ($b.GetPixel($x, $y).ToArgb() -eq $bright) { $b.SetPixel($x, $y, $dark); $n++ }
        }
    }
    $b.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $b.Dispose()
    "frame {0:00}: shaft blood, {1} px darkened" -f $i, $n
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "specks.ps1") -Files (Join-Path $Dir "card_soldier_opened_*.png") -MaxSize 12 -Fix
