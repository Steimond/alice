using Alice.World;
using UnityEngine;

namespace Alice.UI
{
    /// <summary>
    /// Draws the revealed rooms as a tailor's pattern sheet (GDD 0.33): dark paper with a faint grid, room interiors
    /// filled with their zone's tint, walls traced in chalk, catwalks as a dashed line, hazards as red dots. Passages
    /// stay open in the chalk line, also towards rooms not yet revealed, so the player sees where a way continues.
    /// The texture covers the atlas bounds plus a margin; one texel row per <paramref name="scale"/> pixels per tile.
    /// </summary>
    public static class MapPainter
    {
        /// <summary>Paper around the rooms, in tiles, unless the caller asks for more.</summary>
        public const int Margin = 6;

        private static readonly Color32 Paper = new Color32(21, 22, 31, 255);

        public static Color PaperColor => Paper;
        private static readonly Color32 Grid = new Color32(30, 32, 44, 255);
        private static readonly Color32 Chalk = new Color32(217, 210, 192, 255);
        private static readonly Color32 Dash = new Color32(190, 160, 110, 255);
        private static readonly Color32 Blood = new Color32(170, 50, 50, 255);
        private const int GridStep = 4;

        /// <summary>The world tile at the texture's bottom-left corner.</summary>
        public static Vector2Int Origin(MapAtlas atlas, int margin = Margin)
        {
            RectInt bounds = atlas.Bounds;
            return new Vector2Int(bounds.xMin - margin, bounds.yMin - margin);
        }

        public static Vector2Int SizeInTiles(MapAtlas atlas, int margin = Margin)
        {
            RectInt bounds = atlas.Bounds;
            return new Vector2Int(bounds.width + 2 * margin, bounds.height + 2 * margin);
        }

        public static Texture2D Paint(MapAtlas atlas, MapState state, string currentRoomId, int scale, Texture2D reuse = null,
            int margin = Margin)
        {
            Vector2Int origin = Origin(atlas, margin);
            Vector2Int tiles = SizeInTiles(atlas, margin);
            int width = tiles.x * scale;
            int height = tiles.y * scale;
            Texture2D texture = reuse;
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(texture);
                    }
                    else
                    {
                        Object.DestroyImmediate(texture);
                    }
                }

                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "Map"
                };
            }

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool line = (x / scale) % GridStep == 0 && x % scale == 0 || (y / scale) % GridStep == 0 && y % scale == 0;
                    pixels[y * width + x] = line ? Grid : Paper;
                }
            }

            foreach (MapAtlas.RoomEntry room in atlas.Rooms)
            {
                if (state.IsRevealed(room.Id))
                {
                    PaintRoom(atlas, room, room.Id == currentRoomId, origin, scale, pixels, width);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static void PaintRoom(MapAtlas atlas, MapAtlas.RoomEntry room, bool current, Vector2Int origin, int scale,
            Color32[] pixels, int width)
        {
            MapAtlas.ZoneEntry zone = atlas.FindZone(room.ZoneId);
            Color tint = zone != null ? zone.Tint : new Color(0.22f, 0.3f, 0.36f);
            Color32 fill = (Color)tint * (current ? 1f : 0.7f);
            fill.a = 255;
            for (int wy = room.Rect.yMin; wy < room.Rect.yMax; wy++)
            {
                for (int wx = room.Rect.xMin; wx < room.Rect.xMax; wx++)
                {
                    char cell = room.CellAt(wx, wy);
                    if (cell == MapAtlas.Rock)
                    {
                        continue;
                    }

                    int px0 = (wx - origin.x) * scale;
                    int py0 = (wy - origin.y) * scale;
                    for (int py = 0; py < scale; py++)
                    {
                        for (int px = 0; px < scale; px++)
                        {
                            Color32 color = fill;
                            if (cell == MapAtlas.Catwalk && py == scale - 1 && (px + px0 / scale) % 2 == 0)
                            {
                                color = Dash;
                            }
                            else if (cell == MapAtlas.Hazard && (px + py) % 2 == 0)
                            {
                                color = Blood;
                            }

                            pixels[(py0 + py) * width + px0 + px] = color;
                        }
                    }

                    // Chalk on every side that faces rock, in this room or in the neighbour beyond its edge.
                    if (IsRockAt(atlas, room, wx - 1, wy))
                    {
                        Line(pixels, width, px0, py0, 1, scale);
                    }

                    if (IsRockAt(atlas, room, wx + 1, wy))
                    {
                        Line(pixels, width, px0 + scale - 1, py0, 1, scale);
                    }

                    if (IsRockAt(atlas, room, wx, wy - 1))
                    {
                        Line(pixels, width, px0, py0, scale, 1);
                    }

                    if (IsRockAt(atlas, room, wx, wy + 1))
                    {
                        Line(pixels, width, px0, py0 + scale - 1, scale, 1);
                    }
                }
            }
        }

        private static bool IsRockAt(MapAtlas atlas, MapAtlas.RoomEntry room, int wx, int wy)
        {
            if (room.Rect.Contains(new Vector2Int(wx, wy)))
            {
                return room.CellAt(wx, wy) == MapAtlas.Rock;
            }

            foreach (MapAtlas.RoomEntry other in atlas.Rooms)
            {
                if (other != room && other.Rect.Contains(new Vector2Int(wx, wy)))
                {
                    return other.CellAt(wx, wy) == MapAtlas.Rock;
                }
            }

            // Open cells reach a room's edge only at passages; one with no room beyond yet (a zone still to come)
            // stays open on the map too.
            return false;
        }

        private static void Line(Color32[] pixels, int width, int x0, int y0, int w, int h)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    pixels[y * width + x] = Chalk;
                }
            }
        }
    }
}
