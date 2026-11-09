# Builds the main-menu logo (GDD 0.35): the generated "Alice" lettering on a dark label plate with a thin gold
# frame, and the owner's hidden line (8 Nov 2026) written into the plate's flat fill a few levels lighter, so it
# shows only when brightness or contrast is pushed up. No hand drawing: the plate and the hidden letters are
# drawn here from a small 5x7 pixel font.
param(
    [string]$Letters = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "logo-b.png"),
    [string]$Out = (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "menu_logo.png"),
    [string]$Hidden = "Alisa K. 25.01.22",
    [int[]]$Delta = @(1, 1, 2),  # how much lighter the hidden letters are than the plate, R G B: about one level of brightness
    [string]$PreviewOut = ""    # optional: the same logo with the hidden line pushed to full contrast, for checking
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$font# Case matters ('A' and 'a' are different glyphs), so not a PowerShell hash literal.
$font = New-Object 'System.Collections.Generic.Dictionary[string,string[]]' ([StringComparer]::Ordinal)
$font['A'] = @('.###.', '#...#', '#...#', '#####', '#...#', '#...#', '#...#')
$font['K'] = @('#...#', '#..#.', '#.#..', '##...', '#.#..', '#..#.', '#...#')
$font['l'] = @('.##..', '..#..', '..#..', '..#..', '..#..', '..#..', '.###.')
$font['i'] = @('..#..', '.....', '.##..', '..#..', '..#..', '..#..', '.###.')
$font['s'] = @('.....', '.....', '.####', '#....', '.###.', '....#', '####.')
$font['a'] = @('.....', '.....', '.###.', '....#', '.####', '#...#', '.####')
$font['.'] = @('.....', '.....', '.....', '.....', '.....', '.##..', '.##..')
$font['0'] = @('.###.', '#...#', '#..##', '#.#.#', '##..#', '#...#', '.###.')
$font['1'] = @('..#..', '.##..', '..#..', '..#..', '..#..', '..#..', '.###.')
$font['2'] = @('.###.', '#...#', '....#', '...#.', '..#..', '.#...', '#####')
$font['5'] = @('#####', '#....', '####.', '....#', '....#', '#...#', '.###.')
$font[' '] = @('.....', '.....', '.....', '.....', '.....', '.....', '.....')
foreach ($ch in $Hidden.ToCharArray()) { if (-not $font.ContainsKey([string]$ch)) { throw "No glyph for '$ch'." } }

$src = [System.Drawing.Bitmap]::FromFile($Letters)
# Lettering bounds after a hard alpha cut.
$x0 = $src.Width; $y0 = $src.Height; $x1 = -1; $y1 = -1
for ($y = 0; $y -lt $src.Height; $y++) { for ($x = 0; $x -lt $src.Width; $x++) {
    if ($src.GetPixel($x, $y).A -ge 128) { if ($x -lt $x0) { $x0 = $x }; if ($x -gt $x1) { $x1 = $x }; if ($y -lt $y0) { $y0 = $y }; if ($y -gt $y1) { $y1 = $y } }
} }
$lw = $x1 - $x0 + 1; $lh = $y1 - $y0 + 1

$pad = 10; $hiddenBand = 13
$textWidth = $Hidden.Length * 6 - 1
$w = [math]::Max($lw, $textWidth) + 2 * $pad
$h = $lh + $pad + 4 + $hiddenBand
$logo = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

$fill = [System.Drawing.Color]::FromArgb(255, 14, 12, 22)
$edge = [System.Drawing.Color]::FromArgb(255, 9, 7, 13)
$gold = [System.Drawing.Color]::FromArgb(255, 150, 118, 70)
$goldDark = [System.Drawing.Color]::FromArgb(255, 82, 62, 38)
$secret = [System.Drawing.Color]::FromArgb(255, 14 + $Delta[0], 12 + $Delta[1], 22 + $Delta[2])
$corner = 4

for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
    # Notched corners, like a cut paper label.
    $dx = [math]::Min($x, $w - 1 - $x); $dy = [math]::Min($y, $h - 1 - $y)
    if ($dx + $dy -lt $corner) { continue }
    $c = $fill
    if ($dx + $dy -eq $corner -or $dx -eq 0 -or $dy -eq 0) { $c = $edge }
    elseif ($dx + $dy -eq $corner + 1 -or $dx -eq 1 -or $dy -eq 1) { $c = $goldDark }
    elseif ($dx + $dy -eq $corner + 2 -or $dx -eq 2 -or $dy -eq 2) { $c = $gold }
    $logo.SetPixel($x, $y, $c)
} }

# The hidden line, centred in the band under the lettering, on flat fill only.
$tx = [int](($w - $textWidth) / 2); $ty = $pad + $lh + 3
$i = 0
foreach ($ch in $Hidden.ToCharArray()) {
    $glyph = $font[[string]$ch]
    for ($gy = 0; $gy -lt 7; $gy++) { for ($gx = 0; $gx -lt 5; $gx++) {
        if ($glyph[$gy][$gx] -eq '#') { $logo.SetPixel($tx + $i * 6 + $gx, $ty + $gy, $secret) }
    } }
    $i++
}

# The lettering on top, hard alpha.
$lx = [int](($w - $lw) / 2) - $x0; $ly = $pad - $y0
for ($y = $y0; $y -le $y1; $y++) { for ($x = $x0; $x -le $x1; $x++) {
    $p = $src.GetPixel($x, $y)
    if ($p.A -ge 128) { $logo.SetPixel($x + $lx, $y + $ly, [System.Drawing.Color]::FromArgb(255, $p.R, $p.G, $p.B)) }
} }
$src.Dispose()
$logo.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"logo {0}x{1}, lettering {2}x{3}, hidden line at ({4},{5}) {6}x7, colour +{7}" -f $w, $h, $lw, $lh, $tx, $ty, $textWidth, ($Delta -join ",")

if ($PreviewOut) {
    # Brightness and contrast pushed hard, as a curious player would: levels 0..21 stretched to 0..255.
    $check = New-Object System.Drawing.Bitmap $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        $p = $logo.GetPixel($x, $y)
        if ($p.A -eq 0) { continue }
        $r = [math]::Min(255, $p.R * 12); $g = [math]::Min(255, $p.G * 12); $b = [math]::Min(255, $p.B * 12)
        $check.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $r, $g, $b))
    } }
    $check.Save($PreviewOut, [System.Drawing.Imaging.ImageFormat]::Png); $check.Dispose()
}
$logo.Dispose()

