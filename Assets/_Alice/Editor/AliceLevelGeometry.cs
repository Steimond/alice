using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Alice.Editor
{
    /// <summary>What a painted cell is. The order is the palette's button order.</summary>
    public enum LevelBrush
    {
        Solid,
        OneWay,
        Hazard,
        BackWall
    }

    /// <summary>
    /// Room geometry painted cell by cell (Level Palette). The painted cells live in one hidden Tilemap per kind
    /// under World/LevelGrid: they are only the data, with Unity's undo and serialization for free. From them the
    /// same grey blocks the builders make are regenerated under World/Geometry, so collisions, Alice's motor and the
    /// art baker keep working on blocks exactly as before. Solid, hazard and back-wall cells merge into rectangles;
    /// one-way cells become one block per horizontal run, because dropping through ignores a whole collider and
    /// a shared one would drop Alice through every catwalk below as well.
    /// </summary>
    public static class AliceLevelGeometry
    {
        private const string TilesFolder = "Assets/_Alice/Art/Prototype/LevelTiles";
        private const string WorldName = "World";
        private const string GridName = "LevelGrid";
        private const string GeometryName = "Geometry";

        private static readonly Color[] TileColors =
        {
            new Color(0.55f, 0.57f, 0.62f),
            new Color(0.72f, 0.6f, 0.4f),
            new Color(0.75f, 0.2f, 0.2f),
            new Color(0.3f, 0.32f, 0.36f)
        };

        public static IEnumerable<LevelBrush> Brushes => (LevelBrush[])System.Enum.GetValues(typeof(LevelBrush));

        public static Color BrushColor(LevelBrush brush)
        {
            return TileColors[(int)brush];
        }

        /// <summary>Sets or clears a rectangle of cells (inclusive) and regenerates the blocks, as one undo step.</summary>
        public static void Paint(Scene scene, LevelBrush brush, RectInt cells, bool erase)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Tilemap layer = EnsureLayer(scene, brush);
            Undo.RegisterCompleteObjectUndo(layer, erase ? "Erase level cells" : "Paint level cells");
            TileBase tile = erase ? null : EnsureTile(brush);
            for (int x = cells.xMin; x < cells.xMax; x++)
            {
                for (int y = cells.yMin; y < cells.yMax; y++)
                {
                    layer.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            // Painting solid ground over a catwalk or the back wall replaces it rather than stacking two kinds.
            if (!erase && brush != LevelBrush.BackWall)
            {
                foreach (LevelBrush other in Brushes.Where(other => other != brush && other != LevelBrush.BackWall))
                {
                    Tilemap otherLayer = FindLayer(scene, other);
                    if (otherLayer == null)
                    {
                        continue;
                    }

                    Undo.RegisterCompleteObjectUndo(otherLayer, "Paint level cells");
                    for (int x = cells.xMin; x < cells.xMax; x++)
                    {
                        for (int y = cells.yMin; y < cells.yMax; y++)
                        {
                            otherLayer.SetTile(new Vector3Int(x, y, 0), null);
                        }
                    }
                }
            }

            RebuildBlocks(scene);
            Undo.CollapseUndoOperations(group);
        }

        /// <summary>Recreates World/Geometry from the painted cells. Blocks placed by the builders elsewhere stay.</summary>
        public static void RebuildBlocks(Scene scene)
        {
            Transform world = EnsureRoot(scene, WorldName);
            Transform geometry = world.Find(GeometryName);
            if (geometry != null)
            {
                Undo.DestroyObjectImmediate(geometry.gameObject);
            }

            geometry = new GameObject(GeometryName).transform;
            Undo.RegisterCreatedObjectUndo(geometry.gameObject, "Rebuild level blocks");
            geometry.SetParent(world, false);

            Sprite grey = AlicePrototypeBuilder.EnsureTileSprite();
            foreach (LevelBrush brush in Brushes)
            {
                Tilemap layer = FindLayer(scene, brush);
                if (layer == null)
                {
                    continue;
                }

                foreach (RectInt rect in Rectangles(layer, brush == LevelBrush.OneWay))
                {
                    string name = $"{brush}_{rect.xMin}_{rect.yMin}_{rect.width}x{rect.height}";
                    AlicePrototypeBuilder.Block(geometry, grey, name, rect.xMin, rect.yMin, rect.xMax, rect.yMax, Kind(brush));
                }
            }

            EditorUtility.SetDirty(geometry.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>The rectangle around every painted solid cell, in world units; false when nothing is painted.</summary>
        public static bool TryGetSolidBounds(Scene scene, out Rect bounds)
        {
            bounds = default;
            Tilemap layer = FindLayer(scene, LevelBrush.Solid);
            if (layer == null)
            {
                return false;
            }

            layer.CompressBounds();
            BoundsInt cells = layer.cellBounds;
            if (cells.size.x <= 0 || cells.size.y <= 0)
            {
                return false;
            }

            bounds = new Rect(cells.xMin, cells.yMin, cells.size.x, cells.size.y);
            return true;
        }

        public static Tilemap FindLayer(Scene scene, LevelBrush brush)
        {
            Transform world = FindRoot(scene, WorldName);
            Transform grid = world != null ? world.Find(GridName) : null;
            Transform layer = grid != null ? grid.Find(brush.ToString()) : null;
            return layer != null ? layer.GetComponent<Tilemap>() : null;
        }

        public static Transform EnsureRoot(Scene scene, string name)
        {
            Transform found = FindRoot(scene, name);
            if (found != null)
            {
                return found;
            }

            var root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create " + name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root.transform;
        }

        public static Transform FindRoot(Scene scene, string name)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == name);
            return root != null ? root.transform : null;
        }

        private static Tilemap EnsureLayer(Scene scene, LevelBrush brush)
        {
            Tilemap existing = FindLayer(scene, brush);
            if (existing != null)
            {
                return existing;
            }

            Transform world = EnsureRoot(scene, WorldName);
            Transform grid = world.Find(GridName);
            if (grid == null)
            {
                var gridObject = new GameObject(GridName, typeof(Grid));
                Undo.RegisterCreatedObjectUndo(gridObject, "Create level grid");
                gridObject.transform.SetParent(world, false);
                grid = gridObject.transform;
            }

            // Data only: the regenerated blocks (and later the baked art) are what the room shows.
            var layerObject = new GameObject(brush.ToString(), typeof(Tilemap), typeof(TilemapRenderer));
            Undo.RegisterCreatedObjectUndo(layerObject, "Create level layer");
            layerObject.transform.SetParent(grid, false);
            layerObject.GetComponent<TilemapRenderer>().enabled = false;
            return layerObject.GetComponent<Tilemap>();
        }

        private static TileBase EnsureTile(LevelBrush brush)
        {
            string path = $"{TilesFolder}/Level{brush}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null)
            {
                return tile;
            }

            AliceProjectSetup.EnsureFolder(TilesFolder);
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AlicePrototypeBuilder.EnsureTileSprite();
            tile.color = BrushColor(brush);
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static AlicePrototypeBuilder.BlockKind Kind(LevelBrush brush)
        {
            return brush switch
            {
                LevelBrush.OneWay => AlicePrototypeBuilder.BlockKind.OneWay,
                LevelBrush.Hazard => AlicePrototypeBuilder.BlockKind.Hazard,
                LevelBrush.BackWall => AlicePrototypeBuilder.BlockKind.Decoration,
                _ => AlicePrototypeBuilder.BlockKind.Solid
            };
        }

        // Greedy cover: the widest run from each free cell, grown upward while the rows above match it exactly.
        private static List<RectInt> Rectangles(Tilemap layer, bool rowsOnly)
        {
            var result = new List<RectInt>();
            layer.CompressBounds();
            BoundsInt bounds = layer.cellBounds;
            int width = bounds.size.x;
            int height = bounds.size.y;
            if (width <= 0 || height <= 0)
            {
                return result;
            }

            var filled = new bool[width, height];
            foreach (Vector3Int position in bounds.allPositionsWithin)
            {
                if (layer.HasTile(position))
                {
                    filled[position.x - bounds.xMin, position.y - bounds.yMin] = true;
                }
            }

            var used = new bool[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!filled[x, y] || used[x, y])
                    {
                        continue;
                    }

                    int run = 1;
                    while (x + run < width && filled[x + run, y] && !used[x + run, y])
                    {
                        run++;
                    }

                    int rows = 1;
                    while (!rowsOnly && y + rows < height && RowMatches(filled, used, x, y + rows, run))
                    {
                        rows++;
                    }

                    for (int dx = 0; dx < run; dx++)
                    {
                        for (int dy = 0; dy < rows; dy++)
                        {
                            used[x + dx, y + dy] = true;
                        }
                    }

                    result.Add(new RectInt(bounds.xMin + x, bounds.yMin + y, run, rows));
                }
            }

            return result;
        }

        private static bool RowMatches(bool[,] filled, bool[,] used, int x, int y, int run)
        {
            for (int dx = 0; dx < run; dx++)
            {
                if (!filled[x + dx, y] || used[x + dx, y])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
