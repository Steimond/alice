# Lists skin blobs per clean frame (bounding box, centre, size): the face, the hands and forearms, for needle points.
param([string]$Dir)
Add-Type -AssemblyName System.Drawing
function IsSkin($c) { $c.A -ge 128 -and ($c.R - $c.B) -gt 55 -and $c.R -gt 150 -and $c.G -gt 100 }
foreach ($f in Get-ChildItem $Dir -Filter *.png | Sort-Object Name) {
    $b = New-Object System.Drawing.Bitmap $f.FullName
    $w = $b.Width; $h = $b.Height; $lab = New-Object 'int[,]' $w, $h; $n = 0; $out = @()
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        if ($lab[$x, $y] -ne 0 -or -not (IsSkin $b.GetPixel($x, $y))) { continue }
        $n++; $st = New-Object System.Collections.Stack; $st.Push(@($x, $y)); $lab[$x, $y] = $n
        $minx = $x; $maxx = $x; $miny = $y; $maxy = $y; $cnt = 0; $sx = 0; $sy = 0
        while ($st.Count) { $p = $st.Pop(); $cnt++; $sx += $p[0]; $sy += $p[1]
            if ($p[0] -lt $minx) { $minx = $p[0] }; if ($p[0] -gt $maxx) { $maxx = $p[0] }; if ($p[1] -lt $miny) { $miny = $p[1] }; if ($p[1] -gt $maxy) { $maxy = $p[1] }
            foreach ($d in @(@(1,0),@(-1,0),@(0,1),@(0,-1),@(1,1),@(-1,-1),@(1,-1),@(-1,1))) { $nx = $p[0] + $d[0]; $ny = $p[1] + $d[1]
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $w -and $ny -lt $h -and $lab[$nx, $ny] -eq 0 -and (IsSkin $b.GetPixel($nx, $ny))) { $lab[$nx, $ny] = $n; $st.Push(@($nx, $ny)) } } }
        if ($cnt -ge 3) { $out += "[$minx-$maxx,$miny-$maxy c($([int]($sx/$cnt)),$([int]($sy/$cnt))) n$cnt]" }
    } }
    "$($f.Name): " + ($out -join ' ')
    $b.Dispose()
}
