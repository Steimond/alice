using System.Collections.Generic;
using System.IO;
using Alice.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Bakes Lower Quarters art over the grey blocks of the open scene (Art/Reviews/Alice/Revision14).
    /// The blocks keep their colliders and stay the only source of geometry; their grey renderers are hidden and
    /// three baked sprites take over: the back wall, the masonry and the one-way catwalks.
    ///
    /// The masonry is two layers. Its inside is one large seamless texture sampled by world pixel, so it repeats
    /// every 8 tiles instead of every tile. Its rim (top surface and outline) comes from a corner Wang tileset laid
    /// on the dual grid: each art tile is shifted by half a tile and picks the tile that matches its four
    /// surrounding cells, which puts the drawn edge exactly on the cell edge the collider uses.
    ///
    /// Running it again rebakes the same files and reuses the scene objects, so it is safe to repeat.
    /// </summary>
    public static class AliceRoomArtBaker
    {
        private const string SourceFolder = "Assets/_Alice/Art/World/LowerQuarters/Source";
        private const string BakedFolder = "Assets/_Alice/Art/World/LowerQuarters/Baked";
        private const string FillPath = SourceFolder + "/lq_fill_masonry.png";
        private const string TileRimPath = SourceFolder + "/lq_rim_tile.png";
        private const string SlabRimPath = SourceFolder + "/lq_rim_slab.png";
        private const string PlankRimPath = SourceFolder + "/lq_rim_plank.png";
        private const string TraversalScenePath = "Assets/_Alice/Scenes/Prototype_Traversal.unity";
        private const string TestScenePath = "Assets/_Alice/Scenes/Prototype_LowerQuarters.unity";
        private const string ArtRootName = "LowerQuartersArt";

        // Pixels from the empty side that still come from the Wang tile: the pale cap and the outline are 3-4 px.
        private const int RimWidth = 4;
        private const int DecorationOrder = -100;
        private const int BackWallOrder = -100;
        private const int MasonryOrder = 0;
        private const int CatwalkOrder = 1;
        // The back wall reuses the masonry texture, darker and cooler, so walkable surfaces stay the brightest.
        private static readonly Color BackWallShade = new Color(0.26f, 0.3f, 0.36f, 1f);
        // Shifts the back wall's copy of the texture so its large stones never line up with the masonry's.
        private static readonly Vector2Int BackWallTextureOffset = new Vector2Int(61, 37);

        private enum CellKind
        {
            Solid,
            OneWay,
            BackWall
        }

        [MenuItem("Alice/Art/Build Lower Quarters Test Scene")]
        public static void BuildTestSceneFromMenu()
        {
            if (!LeavePlayModeFirst())
            {
                return;
            }

            // Opening the test scene replaces the open ones, so never do it over unsaved work.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogWarning("Room art: save or discard changes in " + SceneManager.GetSceneAt(i).name + " first.");
                    return;
                }
            }

            if (!File.Exists(TestScenePath) && !AssetDatabase.CopyAsset(TraversalScenePath, TestScenePath))
            {
                throw new IOException("Could not create scene: " + TestScenePath);
            }

            Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            Bake(scene, TileRimPath);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new IOException("Could not save scene: " + TestScenePath);
            }
        }

        [MenuItem("Alice/Art/Bake Lower Quarters Art (Laundry Tiles)")]
        public static void BakeTilesFromMenu()
        {
            BakeOpenScene(TileRimPath);
        }

        [MenuItem("Alice/Art/Bake Lower Quarters Art (Wet Slabs)")]
        public static void BakeSlabsFromMenu()
        {
            BakeOpenScene(SlabRimPath);
        }

        /// <summary>Bakes the given scene without saving it; files are named after the scene.</summary>
        public static void BakeScene(Scene scene, bool wetSlabs)
        {
            Bake(scene, wetSlabs ? SlabRimPath : TileRimPath);
        }

        public static string BakedPathPrefix(Scene scene)
        {
            return BakedFolder + "/" + scene.name;
        }

        private static void BakeOpenScene(string rimPath)
        {
            if (!LeavePlayModeFirst())
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            Bake(scene, rimPath);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool LeavePlayModeFirst()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Room art: leave Play mode first.");
                return false;
            }

            return true;
        }

        private static void Bake(Scene scene, string rimPath)
        {
            Sprite greyTile = AlicePrototypeBuilder.EnsureTileSprite();
            var blocks = new List<(SpriteRenderer renderer, CellKind kind)>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sprite == greyTile && TryClassify(renderer, out CellKind kind))
                    {
                        blocks.Add((renderer, kind));
                    }
                }
            }

            if (blocks.Count == 0)
            {
                Debug.LogWarning("Room art: no grey blocks in " + scene.name + ".");
                return;
            }

            RectInt bounds = CellBounds(blocks);
            var cells = new Dictionary<CellKind, bool[,]>
            {
                [CellKind.Solid] = new bool[bounds.width, bounds.height],
                [CellKind.OneWay] = new bool[bounds.width, bounds.height],
                [CellKind.BackWall] = new bool[bounds.width, bounds.height]
            };
            foreach ((SpriteRenderer renderer, CellKind kind) in blocks)
            {
                Mark(cells[kind], bounds, BlockRect(renderer));
            }

            Texture2D fill = LoadPixels(FillPath);
            Texture2D rim = LoadPixels(rimPath);
            Texture2D plank = LoadPixels(PlankRimPath);

            Directory.CreateDirectory(BakedFolder);
            string prefix = BakedFolder + "/" + scene.name;
            Sprite backWall = Save(BakeBackWall(cells[CellKind.BackWall], bounds, fill), prefix + "_backwall.png");
            Sprite masonry = Save(BakeWang(cells[CellKind.Solid], bounds, rim, fill), prefix + "_masonry.png");
            Sprite catwalks = Save(BakeWang(cells[CellKind.OneWay], bounds, plank, null), prefix + "_catwalks.png");

            Transform parent = blocks[0].renderer.transform.parent;
            Transform artRoot = parent.Find(ArtRootName);
            if (artRoot == null)
            {
                artRoot = new GameObject(ArtRootName).transform;
                artRoot.SetParent(parent, false);
            }

            Vector3 origin = new Vector3(bounds.xMin, bounds.yMin, 0f);
            Place(artRoot, "BackWall", backWall, BackWallOrder, origin);
            Place(artRoot, "Masonry", masonry, MasonryOrder, origin);
            Place(artRoot, "Catwalks", catwalks, CatwalkOrder, origin);

            foreach ((SpriteRenderer renderer, CellKind _) in blocks)
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }

            Debug.Log($"Room art: baked {blocks.Count} blocks of {scene.name}, {bounds.width}x{bounds.height} tiles.");
        }

        // Only the builder's blocks: solid and one-way blocks carry their collider, the back wall is the one
        // collider-less grey renderer at the decoration order. Hazards (triggers) and Alice's grey body stay as they are.
        private static bool TryClassify(SpriteRenderer renderer, out CellKind kind)
        {
            kind = CellKind.Solid;
            if (renderer.drawMode != SpriteDrawMode.Tiled)
            {
                return false;
            }

            var collider = renderer.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                kind = CellKind.BackWall;
                return renderer.sortingOrder == DecorationOrder;
            }

            if (collider.isTrigger)
            {
                return false;
            }

            kind = renderer.GetComponent<PlatformEffector2D>() != null ? CellKind.OneWay : CellKind.Solid;
            return true;
        }

        private static Rect BlockRect(SpriteRenderer renderer)
        {
            Vector2 size = renderer.size;
            Vector2 centre = renderer.transform.position;
            return new Rect(centre - size * 0.5f, size);
        }

        private static RectInt CellBounds(List<(SpriteRenderer renderer, CellKind kind)> blocks)
        {
            Rect total = BlockRect(blocks[0].renderer);
            foreach ((SpriteRenderer renderer, CellKind _) in blocks)
            {
                Rect rect = BlockRect(renderer);
                total = Rect.MinMaxRect(Mathf.Min(total.xMin, rect.xMin), Mathf.Min(total.yMin, rect.yMin),
                    Mathf.Max(total.xMax, rect.xMax), Mathf.Max(total.yMax, rect.yMax));
            }

            int xMin = Mathf.FloorToInt(total.xMin + 0.01f);
            int yMin = Mathf.FloorToInt(total.yMin + 0.01f);
            int xMax = Mathf.CeilToInt(total.xMax - 0.01f);
            int yMax = Mathf.CeilToInt(total.yMax - 0.01f);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        // A cell belongs to a block when its centre is inside it; blocks are built on whole tiles.
        private static void Mark(bool[,] grid, RectInt bounds, Rect rect)
        {
            for (int x = 0; x < bounds.width; x++)
            {
                for (int y = 0; y < bounds.height; y++)
                {
                    Vector2 centre = new Vector2(bounds.xMin + x + 0.5f, bounds.yMin + y + 0.5f);
                    if (rect.Contains(centre))
                    {
                        grid[x, y] = true;
                    }
                }
            }
        }

        // Outside the room each edge cell continues, so outer walls run off the image without an outline and an
        // opening at the very edge (a doorway) stays open instead of being drawn shut.
        private static bool At(bool[,] grid, int x, int y)
        {
            x = Mathf.Clamp(x, 0, grid.GetLength(0) - 1);
            y = Mathf.Clamp(y, 0, grid.GetLength(1) - 1);
            return grid[x, y];
        }

        private static Texture2D BakeWang(bool[,] grid, RectInt bounds, Texture2D sheet, Texture2D fill)
        {
            const int tile = WorldMetrics.TilePixels;
            const int half = tile / 2;
            int width = bounds.width * tile;
            int height = bounds.height * tile;
            var pixels = new Color32[width * height];
            bool[][,] rimMasks = RimMasks(sheet);

            // Dual grid: art tile (tx, ty) is centred on the corner shared by cells tx-1..tx and ty-1..ty
            // (y up, as in the scene). Its NW corner is the upper-left cell.
            for (int tx = 0; tx <= bounds.width; tx++)
            {
                for (int ty = 0; ty <= bounds.height; ty++)
                {
                    int nw = At(grid, tx - 1, ty) ? 8 : 0;
                    int ne = At(grid, tx, ty) ? 4 : 0;
                    int sw = At(grid, tx - 1, ty - 1) ? 2 : 0;
                    int se = At(grid, tx, ty - 1) ? 1 : 0;
                    int index = nw | ne | sw | se;
                    if (index == 0)
                    {
                        continue;
                    }

                    for (int px = 0; px < tile; px++)
                    {
                        for (int py = 0; py < tile; py++)
                        {
                            int wx = tx * tile - half + px;
                            int wy = ty * tile - half + py;
                            if (wx < 0 || wy < 0 || wx >= width || wy >= height)
                            {
                                continue;
                            }

                            Color32 source = SheetPixel(sheet, index, px, py);
                            if (source.a == 0)
                            {
                                continue;
                            }

                            if (fill != null && !rimMasks[index][px, py])
                            {
                                source = FillPixel(fill, bounds.xMin * tile + wx, bounds.yMin * tile + wy);
                            }

                            source.a = 255;
                            pixels[wy * width + wx] = source;
                        }
                    }
                }
            }

            return MakeTexture(width, height, pixels);
        }

        private static Texture2D BakeBackWall(bool[,] grid, RectInt bounds, Texture2D fill)
        {
            const int tile = WorldMetrics.TilePixels;
            int width = bounds.width * tile;
            int height = bounds.height * tile;
            var pixels = new Color32[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!grid[x / tile, y / tile])
                    {
                        continue;
                    }

                    Color texel = FillPixel(fill, bounds.xMin * tile + x + BackWallTextureOffset.x,
                        bounds.yMin * tile + y + BackWallTextureOffset.y);
                    Color shaded = texel * BackWallShade;
                    shaded.a = 1f;
                    pixels[y * width + x] = shaded;
                }
            }

            return MakeTexture(width, height, pixels);
        }

        // Tile index i sits at column i % 4, row i / 4 counted from the top of the packed sheet (pack-wang.ps1);
        // px/py count from the tile's bottom-left like the texture itself.
        private static Color32 SheetPixel(Texture2D sheet, int index, int px, int py)
        {
            const int tile = WorldMetrics.TilePixels;
            int column = index % 4;
            int rowFromTop = index / 4;
            int x = column * tile + px;
            int y = sheet.height - (rowFromTop + 1) * tile + py;
            return sheet.GetPixel(x, y);
        }

        private static Color32 FillPixel(Texture2D fill, int worldX, int worldY)
        {
            int x = ((worldX % fill.width) + fill.width) % fill.width;
            int y = ((worldY % fill.height) + fill.height) % fill.height;
            return fill.GetPixel(x, y);
        }

        // For each tile, which pixels lie within RimWidth of an empty pixel of the same tile: those are kept from
        // the tileset, the rest is the texture. A tile with no empty pixels is all inside.
        private static bool[][,] RimMasks(Texture2D sheet)
        {
            const int tile = WorldMetrics.TilePixels;
            var masks = new bool[16][,];
            for (int index = 0; index < 16; index++)
            {
                var mask = new bool[tile, tile];
                for (int px = 0; px < tile; px++)
                {
                    for (int py = 0; py < tile; py++)
                    {
                        mask[px, py] = NearEmpty(sheet, index, px, py);
                    }
                }

                masks[index] = mask;
            }

            return masks;
        }

        private static bool NearEmpty(Texture2D sheet, int index, int px, int py)
        {
            const int tile = WorldMetrics.TilePixels;
            for (int dx = -RimWidth; dx <= RimWidth; dx++)
            {
                for (int dy = -RimWidth; dy <= RimWidth; dy++)
                {
                    int qx = px + dx;
                    int qy = py + dy;
                    if (qx < 0 || qy < 0 || qx >= tile || qy >= tile)
                    {
                        continue;
                    }

                    if (SheetPixel(sheet, index, qx, qy).a == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Reads the PNG bytes directly, so the source files need no special import settings.
        private static Texture2D LoadPixels(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                throw new IOException("Could not read " + path);
            }

            return texture;
        }

        private static Texture2D MakeTexture(int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Sprite Save(Texture2D texture, string path)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = WorldMetrics.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 8192;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Place(Transform artRoot, string name, Sprite sprite, int order, Vector3 origin)
        {
            Transform child = artRoot.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(artRoot, false);
            }

            // The image's bottom-left corner goes on the room's bottom-left cell whatever pivot the import ended
            // up with (an import postprocessor once forced bottom-centre and shifted every room by half its width).
            Vector2 pivot = sprite.pivot / sprite.pixelsPerUnit;
            child.position = origin + (Vector3)pivot;
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            EditorUtility.SetDirty(renderer);
        }
    }
}
