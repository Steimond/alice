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

    // Gloss in a spray is the brightest blood tone, never a pale pink: blood is always red (owner, 9 November 2026).
    static readonly Color Glint = ColorTranslator.FromHtml("#A01511");

    // Saturated yellow or orange inside a gush (the generator's guts and fat) becomes blood as well.
    static bool IsYellowGore(Color c) { return c.R >= 0xB0 && c.G >= 0x80 && c.B < c.G * 0.75 && c.R - c.B >= 0x50; }

    static bool NearBlood(Bitmap b, int x, int y, int r)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= b.Width || ny >= b.Height) continue;
                Color n = b.GetPixel(nx, ny);
                if (n.A >= 128 && IsNeonBlood(n)) return true;
            }
        return false;
    }

    static bool IsNeonBlood(Color c) { return c.R >= 0x60 && c.G <= 0x30 && c.B <= 0x30 && c.R > c.G * 2 + 0x30; }

    // Dark red counts as well: some gushes are drawn maroon, and their gloss is still a pale streak inside.
    static bool IsBloodish(Color c) { return IsNeonBlood(c) || (c.R >= 0x20 && c.R > c.G * 3 && c.R > c.B * 3); }

    // How many of the four sides are bright blood: a gloss streak inside a gush, not the chalk beside it.
    static int BloodSides(Bitmap b, int x, int y)
    {
        int n = 0;
        int[,] d = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + d[i, 0], ny = y + d[i, 1];
            if (nx < 0 || ny < 0 || nx >= b.Width || ny >= b.Height) continue;
            Color q = b.GetPixel(nx, ny);
            if (q.A >= 128 && q.R >= 0xD0 && q.G <= 0x50) n++;
        }
        return n;
    }

    // True when a pixel has a neighbour that is neither empty, bright blood nor light: it sits on the body, not in a spray.
    static bool TouchesBody(Bitmap b, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= b.Width || ny >= b.Height) continue;
                Color q = b.GetPixel(nx, ny);
                bool blood = q.R >= 0xD0 && q.G <= 0x50;
                bool light = q.R >= 0xD0 && q.G >= 0x90;
                if (q.A >= 128 && !blood && !light) return true;
            }
        return false;
    }

    static bool NextToBlood(Bitmap b, int x, int y)
    {
        int found = 0;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= b.Width || ny >= b.Height) continue;
                Color n = b.GetPixel(nx, ny);
                if (n.A >= 128 && IsBloodish(n)) found++;
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
        double maxDistance, int minBlob, bool alignBottom, bool clearAir, bool mapPalette, bool clampBottom, bool gore, bool settle, bool settleAny)
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
                    // clampBottom: a shift down keeps what already lies on the floor row there instead of cutting it off.
                    if (clampBottom && ty >= height && y < height) ty = height - 1;
                    if (tx < 0 || ty < 0 || tx >= width || ty >= height) continue;
                    Color o;
                    if (((c.R >= 0xD0 && c.G >= 0xC0 && c.B >= 0xC0) || (gore && c.R >= 0xD0 && c.G >= 0x90 && (BloodSides(src, x, y) >= 2 || !TouchesBody(src, x, y))))
                        && NextToBlood(src, x, y))
                    {
                        o = Glint; // the gloss streak inside a spray
                    }
                    else if (gore && c.R >= 0xD0 && c.G <= 0x40 && c.B <= 0x50)
                    {
                        // gore: in a death clip the bright pink-red of a glowing eye is blood as well, and goes dark red.
                        o = Ramp[0];
                    }
                    else if (IsYellowGore(c) && NearBlood(src, x, y, 3))
                    {
                        o = c.G >= 0xD0 ? Ramp[0] : Ramp[1];
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
        if (settle) Settle(dst, settleAny);
        return dst;
    }

    // A creature lying on the floor: the bulk of its body comes down onto the floor row and the few pixels that reached
    // lower (a paw, a hem, a drop) are pressed onto it, instead of the body hovering over them. Standing frames, taller
    // than LyingHeight, are left alone: their feet are the contact.
    const int LyingHeight = 30;

    static void Settle(Bitmap b, bool any)
    {
        int w = b.Width, h = b.Height, top = -1;
        var rows = new int[h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = b.GetPixel(x, y);
                if (c.A == 0 || IsBloodish(c)) continue;
                rows[y]++;
                if (top < 0) top = y;
            }
        if (top < 0 || (!any && h - top > LyingHeight)) return;
        int most = 0;
        for (int y = 0; y < h; y++) most = Math.Max(most, rows[y]);
        int bulk = h - 1;
        while (bulk > 0 && rows[bulk] * 2 < most) bulk--;
        int down = h - 1 - bulk;
        if (down <= 0) return;
        var moved = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = b.GetPixel(x, y);
                if (c.A == 0) continue;
                int ty = Math.Min(h - 1, y + down);
                if (y + down >= h && moved.GetPixel(x, ty).A > 0 && !IsBloodish(c)) continue;
                moved.SetPixel(x, ty, c);
            }
        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.Transparent);
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.DrawImageUnscaled(moved, 0, 0);
        }
        moved.Dispose();
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
        # Per-frame nudges that hold the body still inside a loop (the run drifted by its chalk mark).
        if ($clip.frameShiftX -ne $null) { $shiftX += [int]$clip.frameShiftX[$index] }
        $shiftY = if ($clip.shiftY -ne $null) { [int]$clip.shiftY } else { 0 }
        $clearAir = [bool]$clip.clearLast -and $index -eq $last
        $clipBlob = if ($clip.minBlob) { [int]$clip.minBlob } else { $minBlob }
        $bitmap = [EnemyFrames]::Convert($source, [int]$data.width, [int]$data.height, $shiftX, $shiftY, $palette,
            $maxDistance, $clipBlob, [bool]$clip.alignBottom, $clearAir, -not [bool]$clip.keepColours, [bool]$clip.clampBottom, [bool]$clip.gore, ([bool]$clip.settle -or ($clip.settleFrom -ne $null -and $index -ge [int]$clip.settleFrom)), ($clip.settleFrom -ne $null -and $index -ge [int]$clip.settleFrom))
        # eraseRed: [[frame, x0, y0, x1, y1], ...] in canvas pixels clears red strays (ribbon scraps the generator
        # left under a body) inside the box; other colours there stay.
        if ($clip.eraseRed -ne $null)
        {
            foreach ($box in $clip.eraseRed)
            {
                if ([int]$box[0] -ne $index) { continue }
                for ($ey = [int]$box[2]; $ey -le [int]$box[4]; $ey++) { for ($ex = [int]$box[1]; $ex -le [int]$box[3]; $ex++) {
                    $p = $bitmap.GetPixel($ex, $ey)
                    if ($p.A -gt 0 -and $p.R -gt $p.G * 1.6) { $bitmap.SetPixel($ex, $ey, [System.Drawing.Color]::Transparent) } } }
            }
        }
        $name = "{0}_{1}_{2:D2}.png" -f $data.prefix, $clip.id, $index
        $bitmap.Save((Join-Path $outDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
        $index++
    }
    "{0}: {1} frames" -f $clip.id, $index
}
