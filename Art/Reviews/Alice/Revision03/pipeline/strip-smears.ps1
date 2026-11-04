# Removes the motion arcs PixMiniMax draws by itself on fast frames (white and peach crescents), so
# needle.ps1 can draw the needle and its own trail in the game's steel. A pixel goes when its colour is
# far from every colour of the stance (the arcs are off-palette) or when it belongs to a small blob that
# floats free of Alice. Frame 0 of a PixMiniMax result is the input stance and is skipped.
param(
    [Parameter(Mandatory)] [string]$InDir,
    [Parameter(Mandatory)] [string]$OutDir,
    [Parameter(Mandatory)] [string]$PaletteFrom,
    [double]$MaxDistance = 70,
    [int]$MinBlob = 30
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutDir | Out-Null

$pal = New-Object System.Collections.Generic.List[int[]]
$ref = [System.Drawing.Bitmap]::FromFile($PaletteFrom)
$seenColours = @{}
for ($y = 0; $y -lt $ref.Height; $y++) { for ($x = 0; $x -lt $ref.Width; $x++) {
    $c = $ref.GetPixel($x, $y)
    if ($c.A -ge 128 -and -not $seenColours.ContainsKey($c.ToArgb())) { $seenColours[$c.ToArgb()] = 1; $pal.Add(@($c.R, $c.G, $c.B)) }
} }
$ref.Dispose()

function Distance($c) {
    $best = [double]::MaxValue
    foreach ($p in $pal) { $d = [math]::Sqrt(($c.R - $p[0]) * ($c.R - $p[0]) + ($c.G - $p[1]) * ($c.G - $p[1]) + ($c.B - $p[2]) * ($c.B - $p[2])); if ($d -lt $best) { $best = $d } }
    $best
}

$files = Get-ChildItem $InDir -Filter *.png | Sort-Object Name | Select-Object -Skip 1
$index = 0
foreach ($f in $files) {
    $src = [System.Drawing.Bitmap]::FromFile($f.FullName)
    $w = $src.Width; $h = $src.Height
    $keep = New-Object 'bool[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        $c = $src.GetPixel($x, $y)
        $keep[$x, $y] = $c.A -ge 128 -and (Distance $c) -le $MaxDistance
    } }
    # Small blobs, 8-connected, that are not part of Alice.
    $seen = New-Object 'bool[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        if (-not $keep[$x, $y] -or $seen[$x, $y]) { continue }
        $blob = New-Object System.Collections.Generic.List[int[]]
        $stack = New-Object System.Collections.Generic.Stack[int[]]
        $stack.Push(@($x, $y)); $seen[$x, $y] = $true
        while ($stack.Count -gt 0) {
            $p = $stack.Pop(); $blob.Add($p)
            for ($dy = -1; $dy -le 1; $dy++) { for ($dx = -1; $dx -le 1; $dx++) {
                $nx = $p[0] + $dx; $ny = $p[1] + $dy
                if ($nx -ge 0 -and $ny -ge 0 -and $nx -lt $w -and $ny -lt $h -and $keep[$nx, $ny] -and -not $seen[$nx, $ny]) { $seen[$nx, $ny] = $true; $stack.Push(@($nx, $ny)) }
            } }
        }
        if ($blob.Count -lt $MinBlob) { foreach ($p in $blob) { $keep[$p[0], $p[1]] = $false } }
    } }
    $out = [System.Drawing.Bitmap]::new($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { if ($keep[$x, $y]) { $out.SetPixel($x, $y, $src.GetPixel($x, $y)) } } }
    $src.Dispose()
    $out.Save((Join-Path $OutDir ('run-{0:D2}.png' -f $index)), [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose(); $index++
}
