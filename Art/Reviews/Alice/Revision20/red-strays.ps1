# Lists small red pieces apart from the largest red piece (the ribbon) in each frame: ribbon scraps and red specks
# the generator left on a body. Usage: red-strays.ps1 -Dir <frames> -Filter racer_run_* [-MaxSize 12]
param([string]$Dir, [string]$Filter = '*.png', [int]$MaxSize = 12)
Add-Type -AssemblyName System.Drawing
foreach ($file in Get-ChildItem $Dir -Filter $Filter | Sort-Object Name)
{
    $b = [System.Drawing.Bitmap]::FromFile($file.FullName)
    $w = $b.Width; $h = $b.Height
    $red = New-Object 'bool[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        $c = $b.GetPixel($x, $y); $red[$x, $y] = $c.A -gt 0 -and $c.R -gt $c.G * 2.5 -and $c.R -gt 0x50 -and $c.R -lt 0xC0 } }
    $seen = New-Object 'bool[,]' $w, $h
    $pieces = @()
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        if (-not $red[$x, $y] -or $seen[$x, $y]) { continue }
        $stack = New-Object System.Collections.Stack; $stack.Push(@($x, $y)); $seen[$x, $y] = $true
        $px = @()
        while ($stack.Count -gt 0) {
            $p = $stack.Pop(); $px += , $p
            foreach ($d in @(@(-1,0),@(1,0),@(0,-1),@(0,1),@(-1,-1),@(1,1),@(-1,1),@(1,-1))) {
                $nx = $p[0] + $d[0]; $ny = $p[1] + $d[1]
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $w -and $ny -lt $h -and $red[$nx, $ny] -and -not $seen[$nx, $ny]) {
                    $seen[$nx, $ny] = $true; $stack.Push(@($nx, $ny)) } } }
        $xs = $px | ForEach-Object { $_[0] }; $ys = $px | ForEach-Object { $_[1] }
        $pieces += [pscustomobject]@{ N = $px.Count; X0 = ($xs | Measure-Object -Minimum).Minimum; X1 = ($xs | Measure-Object -Maximum).Maximum
            Y0 = ($ys | Measure-Object -Minimum).Minimum; Y1 = ($ys | Measure-Object -Maximum).Maximum } } }
    $b.Dispose()
    $main = $pieces | Sort-Object N -Descending | Select-Object -First 1
    $small = $pieces | Where-Object { $_ -ne $main -and $_.N -le $MaxSize }
    if ($small) { "{0}: main {1}px at {2}..{3},{4}..{5}; strays: {6}" -f $file.BaseName, $main.N, $main.X0, $main.X1, $main.Y0, $main.Y1,
        (($small | ForEach-Object { "{0}px [{1},{2},{3},{4}]" -f $_.N, $_.X0, $_.Y0, $_.X1, $_.Y1 }) -join '; ') }
}

