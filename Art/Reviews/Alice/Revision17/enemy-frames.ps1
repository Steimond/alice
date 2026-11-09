# Brings raw PixMiniMax frames of one enemy into the game: one canvas and pivot for every clip, hard alpha,
# colours mapped to the enemy's look plus the blood ramp (off-palette motion arcs dropped), neon blood remapped
# to the blood palette (Revision04/blood-palette.ps1), small detached crumbs removed. Writes
# <OutDir>/<prefix>_<clip>_NN.png; existing names are overwritten so their GUIDs stay.
# Usage: powershell -ExecutionPolicy Bypass -File enemy-frames.ps1 -Spec moth.json
param(
    [Parameter(Mandatory)][string]$Spec
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;

public static class EnemyFrames
{
    // Brightest to darkest, the blood palette of gore-execution-flash.png (Revision04/blood-palette.ps1).
    static readonly int[] RampMin = { 0xE6, 0xC8, 0xA8, 0x90, 0x78, 0x00 };
    static readonly Color[] Ramp = {
        ColorTranslator.FromHtml("#A01511"), ColorTranslator.FromHtml("#900C08"), ColorTranslator.FromHtml("#820A07"),
        ColorTranslator.FromHtml("#730403"), ColorTranslator.FromHtml("#650202"), ColorTranslator.FromHtml("#570201") };

    static readonly Color Glint = ColorTranslator.FromHtml("#CC6351");

    static bool IsNeonBlood(Color c) { return c.R >= 0x60 && c.G <= 0x30 && c.B <= 0x30 && c.R > c.G * 2 + 0x30; }

    static bool NextToBlood(Bitmap b, int x, int y)
    {
        int found = 0;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= b.Width || ny >= b.Height) continue;
                Color n = b.GetPixel(nx, ny);
                if (n.A >= 128 && IsNeonBlood(n)) found++;
            }
        return found >= 2;
    }

    public static List<Color> Palette(string path)
    {
        var set = new HashSet<int>();
        var list = new List<Color>();
        using (var b = new Bitmap(path))
        {
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                {
                    Color c = b.GetPixel(x, y);
                    if (c.A >= 128 && set.Add(c.ToArgb())) list.Add(Color.FromArgb(255, c));
                }
        }
        foreach (Color r in Ramp) list.Add(r);
        return list;
    }

    static double Distance(Color a, Color b)
    {
        double dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return Math.Sqrt(dr * dr * 0.3 + dg * dg * 0.59 + db * db * 0.11);
    }

    // shiftX/shiftY place the source on the target canvas; alignBottom drops the drawing onto the bottom row.
    public static Bitmap Convert(string path, int width, int height, int shiftX, int shiftY, List<Color> palette,
        double maxDistance, int minBlob, bool alignBottom, bool clearAir, bool mapPalette)
    {
        var dst = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var src = new Bitmap(path))
        {
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    if (c.A < 128) continue;
                    int tx = x + shiftX, ty = y + shiftY;
                    if (tx < 0 || ty < 0 || tx >= width || ty >= height) continue;
                    Color o;
                    if (c.R >= 0xD0 && c.G >= 0xA0 && c.B >= 0xA0 && NextToBlood(src, x, y))
                    {
                        o = Glint; // the gloss streak inside a spray
                    }
                    else if (IsNeonBlood(c))
                    {
                        int i = 0;
                        while (c.R < RampMin[i]) i++;
                        o = Ramp[i];
                    }
                    else
                    {
                        Color best = c; double bestD = double.MaxValue;
                        foreach (Color p in palette) { double d = Distance(c, p); if (d < bestD) { bestD = d; best = p; } }
                        if (bestD > maxDistance) continue; // an off-palette motion arc
                        o = mapPalette ? best : Color.FromArgb(255, c);
                    }
                    dst.SetPixel(tx, ty, o);
                }
        }

        RemoveCrumbs(dst, minBlob, clearAir);
        if (alignBottom)
        {
            int bottom = -1;
            for (int y = height - 1; y >= 0 && bottom < 0; y--)
                for (int x = 0; x < width; x++) if (dst.GetPixel(x, y).A > 0) { bottom = y; break; }
            if (bottom >= 0 && bottom < height - 1)
            {
                int down = height - 1 - bottom;
                var moved = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for (int y = 0; y <= bottom; y++)
                    for (int x = 0; x < width; x++) { Color c = dst.GetPixel(x, y); if (c.A > 0) moved.SetPixel(x, y + down, c); }
                dst.Dispose();
                dst = moved;
            }
        }
        return dst;
    }

    // Detached pieces smaller than minBlob go; with clearAir every piece but the largest goes unless it lies on
    // the floor rows (a pool or a spilled clothespin), so nothing hangs in the air on a held last frame.
    static void RemoveCrumbs(Bitmap b, int minBlob, bool clearAir)
    {
        int w = b.Width, h = b.Height;
        var label = new int[w, h];
        var sizes = new List<int> { 0 };
        var lowest = new List<int> { 0 };
        int next = 0;
        var stack = new Stack<Point>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (b.GetPixel(x, y).A == 0 || label[x, y] != 0) continue;
                next++; sizes.Add(0); lowest.Add(0);
                stack.Push(new Point(x, y)); label[x, y] = next;
                while (stack.Count > 0)
                {
                    Point p = stack.Pop();
                    sizes[next]++;
                    if (p.Y > lowest[next]) lowest[next] = p.Y;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = p.X + dx, ny = p.Y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h || label[nx, ny] != 0 || b.GetPixel(nx, ny).A == 0) continue;
                            label[nx, ny] = next; stack.Push(new Point(nx, ny));
                        }
                }
            }
        int largest = 1;
        for (int i = 1; i <= next; i++) if (sizes[i] > sizes[largest]) largest = i;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int l = label[x, y];
                if (l == 0 || l == largest) continue;
                bool onFloor = lowest[l] >= h - 3;
                if (sizes[l] < minBlob || (clearAir && !onFloor)) b.SetPixel(x, y, Color.Transparent);
            }
    }
}
'@

$specPath = (Resolve-Path $Spec).Path
$root = Split-Path $specPath
$data = Get-Content $specPath -Raw | ConvertFrom-Json
$palette = [EnemyFrames]::Palette((Join-Path $root $data.palette))
$outDir = Join-Path $root $data.outDir
New-Item -ItemType Directory -Force $outDir | Out-Null
$maxDistance = if ($data.maxDistance) { [double]$data.maxDistance } else { 60.0 }
$minBlob = if ($data.minBlob) { [int]$data.minBlob } else { 4 }

foreach ($clip in $data.clips)
{
    $index = 0
    $last = $clip.frames.Count - 1
    foreach ($frame in $clip.frames)
    {
        $source = Join-Path $root ("{0}/{1:D2}.png" -f $clip.dir, [int]$frame)
        $shiftX = if ($clip.shiftX -ne $null) { [int]$clip.shiftX } else { 0 }
        $shiftY = if ($clip.shiftY -ne $null) { [int]$clip.shiftY } else { 0 }
        $clearAir = [bool]$clip.clearLast -and $index -eq $last
        $clipBlob = if ($clip.minBlob) { [int]$clip.minBlob } else { $minBlob }
        $bitmap = [EnemyFrames]::Convert($source, [int]$data.width, [int]$data.height, $shiftX, $shiftY, $palette,
            $maxDistance, $clipBlob, [bool]$clip.alignBottom, $clearAir, -not [bool]$clip.keepColours)
        $name = "{0}_{1}_{2:D2}.png" -f $data.prefix, $clip.id, $index
        $bitmap.Save((Join-Path $outDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
        $index++
    }
    "{0}: {1} frames" -f $clip.id, $index
}
