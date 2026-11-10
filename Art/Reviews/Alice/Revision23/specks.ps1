# Removes loose specks (8-connected opaque groups smaller than -MinSize) left by the generator's smear arcs.
param([Parameter(Mandatory)][string]$Dir, [int]$MinSize = 10)
Add-Type -AssemblyName System.Drawing
foreach ($f in Get-ChildItem $Dir -Filter *.png | Sort-Object Name) {
    $src = [System.Drawing.Bitmap]::FromFile($f.FullName); $b = [System.Drawing.Bitmap]::new($src); $src.Dispose()
    $w = $b.Width; $h = $b.Height; $seen = New-Object 'bool[,]' $w, $h; $removed = 0
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        if ($seen[$x, $y] -or $b.GetPixel($x, $y).A -lt 128) { continue }
        $g = New-Object System.Collections.ArrayList; $st = New-Object System.Collections.Stack; $st.Push(@($x, $y)); $seen[$x, $y] = $true
        while ($st.Count) { $p = $st.Pop(); [void]$g.Add($p)
            for ($dy = -1; $dy -le 1; $dy++) { for ($dx = -1; $dx -le 1; $dx++) { $nx = $p[0] + $dx; $ny = $p[1] + $dy
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $w -and $ny -lt $h -and -not $seen[$nx, $ny] -and $b.GetPixel($nx, $ny).A -ge 128) { $seen[$nx, $ny] = $true; $st.Push(@($nx, $ny)) } } } }
        if ($g.Count -lt $MinSize) { foreach ($p in $g) { $b.SetPixel($p[0], $p[1], [System.Drawing.Color]::FromArgb(0, 0, 0, 0)) }; $removed += $g.Count }
    } }
    if ($removed) { "{0}: {1} px" -f $f.Name, $removed; $b.Save($f.FullName, [System.Drawing.Imaging.ImageFormat]::Png) }
    $b.Dispose()
}
