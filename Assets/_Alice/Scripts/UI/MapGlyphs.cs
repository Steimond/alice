using System.Collections.Generic;
using Alice.World;
using UnityEngine;

namespace Alice.UI
{
    /// <summary>
    /// The map's icons as 7×7 pixel patterns, drawn by code so the map needs no hand-made art; a MapSkin can give a
    /// sprite instead. A bundle lying in the world is a tied box (GDD 0.34).
    /// </summary>
    public static class MapGlyphs
    {
        public const int Size = 7;

        private static readonly Dictionary<MapIconKind, string[]> Patterns = new Dictionary<MapIconKind, string[]>
        {
            [MapIconKind.Alice] = new[] { "...X...", "..XXX..", ".XXXXX.", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." },
            [MapIconKind.Rest] = new[] { "X......", "X......", "X......", "XXXXXXX", "X.....X", "X.....X", "......." },
            [MapIconKind.Item] = new[] { "..X.X..", "...X...", "XXXXXXX", "X..X..X", "XXXXXXX", "X..X..X", "XXXXXXX" },
            [MapIconKind.MarkPin] = new[] { "..XX...", ".XXXX..", ".XXXX..", "..XX...", "...X...", "...X...", "...X..." },
            [MapIconKind.MarkCross] = new[] { "X.....X", ".X...X.", "..X.X..", "...X...", "..X.X..", ".X...X.", "X.....X" },
            [MapIconKind.MarkButton] = new[] { ".XXXXX.", "XXXXXXX", "XX.X.XX", "XXXXXXX", "XX.X.XX", "XXXXXXX", ".XXXXX." },
            [MapIconKind.MarkKnot] = new[] { "..XXX..", ".X...X.", ".X...X.", "..X.X..", "...X...", "..X.X..", ".X...X." }
        };

        private static readonly Dictionary<MapIconKind, Color32> Colors = new Dictionary<MapIconKind, Color32>
        {
            [MapIconKind.Alice] = new Color32(242, 230, 200, 255),
            [MapIconKind.Rest] = new Color32(224, 160, 80, 255),
            [MapIconKind.Item] = new Color32(232, 202, 112, 255)
        };

        private static readonly Color32 MarkColor = new Color32(232, 224, 204, 255);
        private static readonly Color32 Outline = new Color32(16, 16, 22, 255);
        private static readonly Dictionary<MapIconKind, Texture2D> Cache = new Dictionary<MapIconKind, Texture2D>();

        public static Color32 ColorOf(MapIconKind kind)
        {
            return Colors.TryGetValue(kind, out Color32 color) ? color : MarkColor;
        }

        /// <summary>The icon with a one-pixel dark outline, 9×9, point-filtered; made once per kind.</summary>
        public static Texture2D Texture(MapIconKind kind)
        {
            if (Cache.TryGetValue(kind, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            string[] pattern = Patterns.TryGetValue(kind, out string[] found) ? found : Patterns[MapIconKind.MarkPin];
            const int full = Size + 2;
            var pixels = new Color32[full * full];
            Color32 color = ColorOf(kind);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (pattern[Size - 1 - y][x] != 'X')
                    {
                        continue;
                    }

                    // Outline first around the pixel, the pixel itself on top.
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int index = (y + 1 + dy) * full + x + 1 + dx;
                            if (pixels[index].a == 0)
                            {
                                pixels[index] = Outline;
                            }
                        }
                    }
                }
            }

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (pattern[Size - 1 - y][x] == 'X')
                    {
                        pixels[(y + 1) * full + x + 1] = color;
                    }
                }
            }

            var texture = new Texture2D(full, full, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "MapGlyph_" + kind
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            Cache[kind] = texture;
            return texture;
        }
    }
}
