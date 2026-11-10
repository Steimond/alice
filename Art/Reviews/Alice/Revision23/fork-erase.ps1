# The card execution's clean frames carry a thin white fork above Alice's fist from frame 3 to 8: a stray object the
# generator drew (owner's rule: obvious defects are fixed at once). Copies the clean frames and erases every non-skin
# pixel in the box above the fist on those frames.
param([string]$In = (Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) 'Revision03\pipeline\clean-execA14'),
      [string]$Out = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'alice\execA-clean-fixed'))
Add-Type -AssemblyName System.Drawing
if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
New-Item -ItemType Directory $Out | Out-Null
foreach ($f in Get-ChildItem $In -Filter *.png | Sort-Object Name) {
    $src = [System.Drawing.Bitmap]::FromFile($f.FullName); $b = [System.Drawing.Bitmap]::new($src); $src.Dispose()
    $i = [int]($f.BaseName -replace '\D', '')
    # Frame 8 raises the other hand into the top of the box, so its box starts lower.
    $box = if ($i -ge 3 -and $i -le 7) { @(85, 30, 94, 43) } elseif ($i -eq 8) { @(83, 37, 90, 43) } else { $null }
    if ($box) {
        for ($y = $box[1]; $y -le $box[3]; $y++) { for ($x = $box[0]; $x -le $box[2]; $x++) {
            $p = $b.GetPixel($x, $y)
            $grey = [math]::Abs($p.R - $p.B) -lt 30
            # The fork is grey, white and its dark outline; hair and skin beside it are warm and stay.
            if ($p.A -ge 128 -and $grey -and ($x -ge 86 -or $y -ge 35)) { $b.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)) } } }
    }
    $b.Save((Join-Path $Out $f.Name), [System.Drawing.Imaging.ImageFormat]::Png); $b.Dispose()
}
# The fork's warm tip is left floating: loose specks go too.
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'specks.ps1') -Dir $Out
