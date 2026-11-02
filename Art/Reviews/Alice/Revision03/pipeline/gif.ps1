param([string]$Dir, [string]$Out, [int]$Scale = 4, [int]$DelayCs = 10, [int]$Skip = 0)
Add-Type -AssemblyName PresentationCore, WindowsBase, System.Drawing
$files = Get-ChildItem $Dir -Filter *.png | Sort-Object Name
if ($Skip -gt 0) { $files = $files[0..($files.Count - 1 - $Skip)] }
$enc = New-Object System.Windows.Media.Imaging.GifBitmapEncoder
foreach ($f in $files)
{
    $src = [System.Drawing.Bitmap]::FromFile($f.FullName)
    $big = New-Object System.Drawing.Bitmap ($src.Width * $Scale), ($src.Height * $Scale)
    $g = [System.Drawing.Graphics]::FromImage($big)
    $g.Clear([System.Drawing.Color]::FromArgb(60, 60, 64))
    $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $g.DrawImage($src, 0, 0, $big.Width, $big.Height)
    $ms = New-Object System.IO.MemoryStream
    $big.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png); $ms.Position = 0
    $frame = [System.Windows.Media.Imaging.BitmapFrame]::Create($ms, 'None', 'OnLoad')
    $meta = New-Object System.Windows.Media.Imaging.BitmapMetadata 'gif'
    $meta.SetQuery('/grctlext/Delay', [uint16]$DelayCs)
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($frame, $null, $meta, $null))
    $g.Dispose(); $big.Dispose(); $src.Dispose()
}
$mem = New-Object System.IO.MemoryStream
$enc.Save($mem)
$bytes = $mem.ToArray()
# WPF writes no NETSCAPE2.0 loop block; insert it after the global colour table so the GIF repeats.
$flags = $bytes[10]
$gct = if ($flags -band 0x80) { 3 * [math]::Pow(2, ($flags -band 7) + 1) } else { 0 }
$pos = 13 + [int]$gct
$loop = [byte[]](0x21, 0xFF, 0x0B) + [System.Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0') + [byte[]](0x03, 0x01, 0x00, 0x00, 0x00)
$result = $bytes[0..($pos - 1)] + $loop + $bytes[$pos..($bytes.Length - 1)]
$result = [byte[]]$result
# WPF also drops the frame delay from the metadata; write it into every graphic control extension.
for ($i = 0; $i -lt $result.Length - 6; $i++)
{
    if ($result[$i] -eq 0x21 -and $result[$i + 1] -eq 0xF9 -and $result[$i + 2] -eq 4)
    {
        $result[$i + 4] = [byte]($DelayCs -band 0xFF); $result[$i + 5] = [byte]($DelayCs -shr 8)
    }
}
[System.IO.File]::WriteAllBytes($Out, $result)
"frames: $($files.Count), bytes: $($result.Length)"
