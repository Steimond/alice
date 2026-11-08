# Per frame: where the head sits (best match of the stance head), the lowest and highest drawn rows,
# colour count. Used to judge a run cycle's bob before and after cleanup: the head should follow the
# designed wave and never jump more than 2 px between neighbouring frames.
param(
    [Parameter(Mandatory)][string]$Dir,
    [string]$Reference = "C:\Projects\alice\Art\Reviews\Alice\Revision03\pipeline\stand\stand-noneedle.png",
    [int[]]$Head = @(22, 3, 40, 23),   # head box in the 64x64 reference
    [int]$RefOffsetX = 0, [int]$RefOffsetY = 0   # where the reference box lands in these frames at zero shift
)
Add-Type -AssemblyName System.Drawing
function Load($path)
{
    $b = [System.Drawing.Bitmap]::FromFile($path); $w = $b.Width; $h = $b.Height
    $a = New-Object 'int[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { $c = $b.GetPixel($x, $y); $a[$x, $y] = if ($c.A -ge 128) { $c.ToArgb() } else { 0 } } }
    $b.Dispose(); return , $a
}
$ref = Load $Reference
$prev = $null
foreach ($f in Get-ChildItem $Dir -Filter *.png | Sort-Object Name)
{
    $a = Load $f.FullName; $w = $a.GetLength(0); $h = $a.GetLength(1)
    $best = [double]::MaxValue; $bx = 0; $by = 0
    for ($dy = -6; $dy -le 14; $dy++) { for ($dx = -6; $dx -le 14; $dx++) {
        $s = 0
        for ($y = $Head[1]; $y -le $Head[3]; $y++) { for ($x = $Head[0]; $x -le $Head[2]; $x++) {
            $r = $ref[$x, $y]; $xx = $x + $dx + $RefOffsetX; $yy = $y + $dy + $RefOffsetY
            $c = if ($xx -ge 0 -and $yy -ge 0 -and $xx -lt $w -and $yy -lt $h) { $a[$xx, $yy] } else { 0 }
            if (($r -eq 0) -ne ($c -eq 0)) { $s += 3 } elseif ($r -ne 0 -and $r -ne $c) { $s += 1 }
        } }
        if ($s -lt $best) { $best = $s; $bx = $dx; $by = $dy }
    } }
    $top = -1; $low = -1; $cols = @{}
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { if ($a[$x, $y] -ne 0) { if ($top -lt 0) { $top = $y }; $low = $y; $cols[$a[$x, $y]] = 1 } } }
    $jump = if ($prev) { "  dHead {0},{1}" -f ($bx - $prev[0]), ($by - $prev[1]) } else { "" }
    "{0}: head {1},{2} (miss {3})  top {4} low {5} colours {6}{7}" -f $f.Name, $bx, $by, $best, $top, $low, $cols.Count, $jump
    $prev = @($bx, $by)
}
