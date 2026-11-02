param([string]$FramesDir, [string]$Reference, [string]$OutDir,
    [int[]]$Head = @(22, 3, 40, 23),   # x0, y0, x1, y1 of the head in the reference, used for matching
    [int[]]$Face = @(30, 11, 37, 18))  # x0, y0, x1, y1 of the face patch copied over
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $OutDir | Out-Null
$ref = [System.Drawing.Bitmap]::FromFile($Reference)
function Px($b, $x, $y) { if ($x -lt 0 -or $y -lt 0 -or $x -ge $b.Width -or $y -ge $b.Height) { return [System.Drawing.Color]::Transparent }; $b.GetPixel($x, $y) }
foreach ($f in Get-ChildItem $FramesDir -Filter *.png | Sort-Object Name)
{
    $b = [System.Drawing.Bitmap]::FromFile($f.FullName)
    $best = $null; $bestScore = [double]::MaxValue
    for ($dy = -2; $dy -le 8; $dy++) { for ($dx = -4; $dx -le 8; $dx++) {
        $score = 0
        for ($y = $Head[1]; $y -le $Head[3]; $y++) { for ($x = $Head[0]; $x -le $Head[2]; $x++) {
            $r = $ref.GetPixel($x, $y); $c = Px $b ($x + $dx) ($y + $dy)
            if (($r.A -ge 128) -ne ($c.A -ge 128)) { $score += 20000; continue }
            if ($r.A -ge 128) { $score += ($r.R - $c.R) * ($r.R - $c.R) + ($r.G - $c.G) * ($r.G - $c.G) + ($r.B - $c.B) * ($r.B - $c.B) }
        } }
        if ($score -lt $bestScore) { $bestScore = $score; $best = @($dx, $dy) }
    } }
    $out = New-Object System.Drawing.Bitmap $b
    $b.Dispose()
    for ($y = $Face[1]; $y -le $Face[3]; $y++) { for ($x = $Face[0]; $x -le $Face[2]; $x++) {
        $r = $ref.GetPixel($x, $y)
        if ($r.A -ge 128) { $out.SetPixel($x + $best[0], $y + $best[1], $r) }
    } }
    $out.Save((Join-Path $OutDir $f.Name), [System.Drawing.Imaging.ImageFormat]::Png); $out.Dispose()
    "{0}: offset {1},{2} score {3}" -f $f.Name, $best[0], $best[1], $bestScore
}
$ref.Dispose()
