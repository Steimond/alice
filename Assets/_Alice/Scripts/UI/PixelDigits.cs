using UnityEngine;

namespace Alice.UI
{
    /// <summary>
    /// A 5x7 pixel font for the digits 0-9 with a one-pixel dark outline baked in, so HUD numbers sit on
    /// the pixel grid and stay readable over any background without a font asset. Tinting the image
    /// colours the ink; the outline stays dark. Digits are not words, so nothing here needs localization.
    /// </summary>
    public static class PixelDigits
    {
        private const int InkWidth = 5;
        private const int InkHeight = 7;
        public const int Width = InkWidth + 2;
        public const int Height = InkHeight + 2;

        // Rows top to bottom, '#' = ink.
        private static readonly string[] Glyphs =
        {
            ".###." + "#...#" + "#..##" + "#.#.#" + "##..#" + "#...#" + ".###.",
            "..#.." + ".##.." + "..#.." + "..#.." + "..#.." + "..#.." + ".###.",
            ".###." + "#...#" + "....#" + "...#." + "..#.." + ".#..." + "#####",
            "####." + "....#" + "....#" + ".###." + "....#" + "....#" + "####.",
            "...#." + "..##." + ".#.#." + "#..#." + "#####" + "...#." + "...#.",
            "#####" + "#...." + "####." + "....#" + "....#" + "#...#" + ".###.",
            ".###." + "#...." + "#...." + "####." + "#...#" + "#...#" + ".###.",
            "#####" + "....#" + "...#." + "..#.." + ".#..." + ".#..." + ".#...",
            ".###." + "#...#" + "#...#" + ".###." + "#...#" + "#...#" + ".###.",
            ".###." + "#...#" + "#...#" + ".####" + "....#" + "....#" + ".###."
        };

        private static readonly Color Outline = new Color(0.04f, 0.03f, 0.05f, 1f);

        private static Sprite[] _sprites;

        public static Sprite For(int digit)
        {
            // With domain reload off, the cache outlives Play mode while Unity destroys the sprites made in
            // it; a destroyed sprite draws as a plain light square, so rebuild when that happened.
            if (_sprites == null || _sprites[0] == null)
            {
                Build();
            }

            return _sprites[Mathf.Clamp(digit, 0, 9)];
        }

        private static bool IsInk(int digit, int x, int y)
        {
            // x, y in the outlined cell, y up; the glyph rows run top to bottom.
            int gx = x - 1;
            int gy = InkHeight - 1 - (y - 1);
            return gx >= 0 && gx < InkWidth && gy >= 0 && gy < InkHeight && Glyphs[digit][gy * InkWidth + gx] == '#';
        }

        private static void Build()
        {
            Texture2D texture = new Texture2D(Width * 10, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "PixelDigits"
            };
            _sprites = new Sprite[10];
            for (int digit = 0; digit < 10; digit++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Color color = Color.clear;
                        if (IsInk(digit, x, y))
                        {
                            color = Color.white;
                        }
                        else if (TouchesInk(digit, x, y))
                        {
                            color = Outline;
                        }

                        texture.SetPixel(digit * Width + x, y, color);
                    }
                }

                _sprites[digit] = Sprite.Create(texture, new Rect(digit * Width, 0, Width, Height), Vector2.zero, 1f);
            }

            texture.Apply();
        }

        private static bool TouchesInk(int digit, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx != 0 || dy != 0) && IsInk(digit, x + dx, y + dy))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
