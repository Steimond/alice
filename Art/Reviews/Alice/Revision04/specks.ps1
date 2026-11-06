# Finds detached bits in sprite frames: every 8-connected piece of opaque pixels except the largest (the body),
# with its size and box. The execution highlight outlines every opaque pixel, so a stray bit next to the spear that
# is invisible in the plain frame shows up clearly (owner's note, 6 Nov 2026). With -Fix it erases pieces smaller
# than -MaxSize and writes the frames back in place (same files, so the GUIDs stay).
param(
    [Parameter(Mandatory)][string]$Files,
    [int]$MaxSize = 12,
    [switch]$Fix
)

Add-Type -AssemblyName System.Drawing
foreach ($file in (Get-ChildItem $Files | Sort-Object Name))
{
    $src = New-Object System.Drawing.Bitmap $file.FullName
    $b = New-Object System.Drawing.Bitmap $src
    $src.Dispose()
    $w = $b.Width; $h = $b.Height
    $label = New-Object 'int[,]' $w, $h
    $sizes = @{}; $boxes = @{}; $next = 0
    for ($y = 0; $y -lt $h; $y++)
    {
        for ($x = 0; $x -lt $w; $x++)
        {
            if ($b.GetPixel($x, $y).A -lt 128 -or $label[$x, $y] -ne 0) { continue }
            $next++; $n = 0; $x0 = $x; $x1 = $x; $y0 = $y; $y1 = $y
            $stack = New-Object System.Collections.Stack; $stack.Push(@($x, $y)); $label[$x, $y] = $next
            while ($stack.Count -gt 0)
            {
                $p = $stack.Pop(); $n++
                $x0 = [Math]::Min($x0, $p[0]); $x1 = [Math]::Max($x1, $p[0]); $y0 = [Math]::Min($y0, $p[1]); $y1 = [Math]::Max($y1, $p[1])
                for ($dy = -1; $dy -le 1; $dy++)
                {
                    for ($dx = -1; $dx -le 1; $dx++)
                    {
                        $nx = $p[0] + $dx; $ny = $p[1] + $dy
                        if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $w -or $ny -ge $h -or $label[$nx, $ny] -ne 0) { continue }
                        if ($b.GetPixel($nx, $ny).A -ge 128) { $label[$nx, $ny] = $next; $stack.Push(@($nx, $ny)) }
                    }
                }
            }
            $sizes[$next] = $n; $boxes[$next] = "x$x0-$x1 y$y0-$y1"
        }
    }

    $body = ($sizes.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 1).Key
    $pieces = $sizes.Keys | Where-Object { $_ -ne $body } | Sort-Object { -$sizes[$_] }
    if (@($pieces).Count -eq 0) { $b.Dispose(); continue }
    "{0}: body {1} px; detached: {2}" -f $file.Name, $sizes[$body], (($pieces | ForEach-Object { "$($sizes[$_]) px at $($boxes[$_])" }) -join '; ')
    if ($Fix)
    {
        $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
        for ($y = 0; $y -lt $h; $y++)
        {
            for ($x = 0; $x -lt $w; $x++)
            {
                $l = $label[$x, $y]
                if ($l -ne 0 -and $l -ne $body -and $sizes[$l] -le $MaxSize) { $b.SetPixel($x, $y, $clear) }
            }
        }
        $b.Save($file.FullName, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $b.Dispose()
}
