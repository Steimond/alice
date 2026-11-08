using System.Collections.Generic;
using System.IO;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Grey rooms L01–L08 of the Lower Quarters slice from the room schema (Docs/LowerQuartersRooms.md). Geometry
    /// is painted through the Level Palette (AliceLevelGeometry) and objects come from its catalog, so every room
    /// stays editable with the palette afterwards. Rooms are created once and then left alone; the rebuild menu
    /// recreates them. Coordinates are tiles from the room's bottom-left corner, as in the schema.
    ///
    /// The rooms are places of one world (LevelDesignGuide, section 4): each has a world position, and the route is
    /// a loop on that map — east along the bottom (L01, L02), up beside L02 (L03) and through the tall L04, back
    /// west along the top (L05 above L02 and L03, L06, L07, L08) and up from L08 to H01. The lift drops from the
    /// floor of L06 through the ceiling of L02 onto the shore. AliceLevelWorld checks that the passages meet.
    ///
    /// Not built yet, because their mechanics, art or rooms do not exist: the lift ride itself (it is a one-way drop
    /// for now), steam pipes, the needle scene, Mother Crab, Dodo, H00, H01 and the side places L09–L12. Their
    /// openings are cut and show up in the biome map as passages to nowhere.
    /// </summary>
    public static class AliceLowerQuartersBuilder
    {
        private const int WallThickness = 2;
        private const int OpeningHeight = 4;
        private const float ArrivalInset = 3.5f;
        private const string ZoneId = "B01";

        private const string DoorwayPrefab = AliceLevelPaletteCatalog.Root + "/Transitions/Doorway.prefab";
        private const string ShortcutDoorPrefab = AliceLevelPaletteCatalog.Root + "/Transitions/ShortcutDoor.prefab";
        private const string ChairPrefab = AliceLevelPaletteCatalog.Root + "/Rest/RestChair.prefab";
        private const string CachePrefab = AliceLevelPaletteCatalog.Root + "/Service/DecreeCache.prefab";
        private const string FanPrefab = AliceLevelPaletteCatalog.Root + "/Items/Ability_A01.prefab";

        private sealed class Opening
        {
            public string Id;
            public int Side;
            public int Floor;
            public string TargetRoom;
            public string TargetDoorway;
        }

        // A passage through the floor or ceiling: its trigger sits in the hole, arrival is wherever Alice lands.
        private sealed class Shaft
        {
            public string Id;
            public Vector2 Trigger;
            public Vector2 Arrival;
            public int Facing;
            public string TargetRoom;
            public string TargetDoorway;
        }

        private sealed class Plan
        {
            public string Id;
            public Vector2Int World;
            public int Width;
            public int Height;
            public Vector2 Start;
            public readonly List<(LevelBrush Brush, RectInt Cells, bool Erase)> Strokes = new List<(LevelBrush, RectInt, bool)>();
            public readonly List<Opening> Openings = new List<Opening>();
            public readonly List<Shaft> Shafts = new List<Shaft>();
            public readonly List<(MapIconKind Kind, Vector2 Position)> Points = new List<(MapIconKind, Vector2)>();
            public System.Action<Scene> Furnish = scene => { };

            public Plan Solid(int x0, int y0, int x1, int y1) => Stroke(LevelBrush.Solid, x0, y0, x1, y1, false);
            public Plan OneWay(int x0, int y0, int x1, int y1) => Stroke(LevelBrush.OneWay, x0, y0, x1, y1, false);
            public Plan Hazard(int x0, int y0, int x1, int y1) => Stroke(LevelBrush.Hazard, x0, y0, x1, y1, false);
            public Plan Clear(int x0, int y0, int x1, int y1) => Stroke(LevelBrush.Solid, x0, y0, x1, y1, true);

            /// <summary>A pit with a hazard over a solid bottom row, so the room's floor edge stays closed on the map.</summary>
            public Plan Pit(int x0, int x1, int top) => Solid(x0, 0, x1, 1).Hazard(x0, 1, x1, top);

            // Exclusive ends, like the schema's ranges: Solid(0, 0, 14, 6) is x 0–14 with its top at y 6.
            private Plan Stroke(LevelBrush brush, int x0, int y0, int x1, int y1, bool erase)
            {
                Strokes.Add((brush, new RectInt(x0, y0, x1 - x0, y1 - y0), erase));
                return this;
            }

            /// <summary>An opening in the west (-1) or east (1) wall with its floor at <paramref name="floor"/>.</summary>
            public Plan Door(string id, int side, int floor, string targetRoom, string targetDoorway)
            {
                Openings.Add(new Opening { Id = id, Side = side, Floor = floor, TargetRoom = targetRoom, TargetDoorway = targetDoorway });
                return this;
            }

            /// <summary>A map icon the geometry alone does not give: a fan gate, a lift.</summary>
            public Plan Point(MapIconKind kind, float x, float y)
            {
                Points.Add((kind, new Vector2(x, y)));
                return this;
            }

            public Plan Drop(string id, Vector2 trigger, Vector2 arrival, int facing, string targetRoom, string targetDoorway)
            {
                Shafts.Add(new Shaft { Id = id, Trigger = trigger, Arrival = arrival, Facing = facing, TargetRoom = targetRoom, TargetDoorway = targetDoorway });
                return this;
            }
        }

        [MenuItem("Alice/Level/Build Lower Quarters Grey Rooms")]
        public static void BuildFromMenu()
        {
            Build(false);
        }

        [MenuItem("Alice/Level/Rebuild Lower Quarters Grey Rooms (recreate scenes)")]
        public static void RebuildFromMenu()
        {
            Build(true);
        }

        public static void Build(bool recreate)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Lower Quarters: leave Play mode first.");
                return;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogWarning("Lower Quarters: save or discard changes in " + SceneManager.GetSceneAt(i).name + " first.");
                    return;
                }
            }

            string previous = SceneManager.GetActiveScene().path;
            AliceProjectSetup.EnsureFolder(AliceLevelRoomTools.NewRoomsFolder);
            int built = 0;
            foreach (Plan plan in Plans())
            {
                string path = $"{AliceLevelRoomTools.NewRoomsFolder}/{plan.Id}.unity";
                if (File.Exists(path) && !recreate)
                {
                    continue;
                }

                BuildRoom(plan, path);
                AliceLevelRoomTools.AddToCatalog(plan.Id);
                AlicePrototypeBuilder.AddToBuildSettings(path);
                built++;
            }

            if (built > 0)
            {
                AliceMapSetup.BakeAtlas();
            }

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            Debug.Log($"Lower Quarters: built {built} grey rooms in {AliceLevelRoomTools.NewRoomsFolder}.");
        }

        private static void BuildRoom(Plan plan, string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Room room = new GameObject("Room").AddComponent<Room>();
            room.RoomId = plan.Id;
            room.StartPosition = plan.Start;
            room.WorldPosition = plan.World;
            room.ZoneId = ZoneId;

            // Outer walls, floor row and ceiling first, then the room's own strokes, then the side openings.
            int w = plan.Width;
            int h = plan.Height;
            Paint(scene, LevelBrush.BackWall, new RectInt(0, 0, w, h), false);
            Paint(scene, LevelBrush.Solid, new RectInt(0, 0, WallThickness, h), false);
            Paint(scene, LevelBrush.Solid, new RectInt(w - WallThickness, 0, WallThickness, h), false);
            Paint(scene, LevelBrush.Solid, new RectInt(0, h - WallThickness, w, WallThickness), false);
            foreach ((LevelBrush brush, RectInt cells, bool erase) in plan.Strokes)
            {
                Paint(scene, brush, cells, erase);
            }

            foreach (Opening opening in plan.Openings)
            {
                int x = opening.Side < 0 ? 0 : w - WallThickness;
                Paint(scene, LevelBrush.Solid, new RectInt(x, opening.Floor, WallThickness, OpeningHeight), true);
            }

            AliceLevelGeometry.RebuildBlocks(scene);
            AliceLevelRoomTools.FitCamera(scene);

            foreach (Opening opening in plan.Openings)
            {
                PlaceDoorway(scene, plan, opening);
            }

            foreach (Shaft shaft in plan.Shafts)
            {
                PlaceShaft(scene, shaft);
            }

            foreach ((MapIconKind kind, Vector2 position) in plan.Points)
            {
                var point = new GameObject("MapPoint_" + kind).AddComponent<MapPoint>();
                point.transform.SetParent(AliceLevelGeometry.EnsureRoot(scene, "Gameplay"), false);
                point.transform.position = position;
                point.Kind = kind;
            }

            Place(scene, CachePrefab, plan.Start);
            plan.Furnish(scene);

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException("Could not save scene: " + path);
            }
        }

        // Cells only; the blocks are regenerated once, after every stroke of the room.
        private static void Paint(Scene scene, LevelBrush brush, RectInt cells, bool erase)
        {
            AliceLevelGeometry.SetCells(scene, brush, cells, erase);
        }

        private static void PlaceDoorway(Scene scene, Plan plan, Opening opening)
        {
            float x = opening.Side < 0 ? 1f : plan.Width - 1f;
            GameObject instance = Place(scene, DoorwayPrefab, new Vector2(x, opening.Floor));
            if (instance == null)
            {
                return;
            }

            instance.name = "Doorway_" + opening.Id;
            RoomDoorway doorway = instance.GetComponent<RoomDoorway>();
            doorway.DoorwayId = opening.Id;
            int inward = -opening.Side;
            Vector2 arrival = new Vector2(x + inward * ArrivalInset, opening.Floor);
            doorway.Configure(opening.TargetRoom, opening.TargetDoorway, arrival - (Vector2)instance.transform.position, inward);
            EditorUtility.SetDirty(doorway);
        }

        private static void PlaceShaft(Scene scene, Shaft shaft)
        {
            Transform gameplay = AliceLevelGeometry.EnsureRoot(scene, "Gameplay");
            var holder = new GameObject("Shaft_" + shaft.Id);
            holder.transform.SetParent(gameplay, false);
            holder.transform.position = shaft.Trigger;
            BoxCollider2D trigger = holder.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(OpeningHeight, 1f);
            RoomDoorway doorway = holder.AddComponent<RoomDoorway>();
            doorway.DoorwayId = shaft.Id;
            doorway.Configure(shaft.TargetRoom, shaft.TargetDoorway, shaft.Arrival - shaft.Trigger, shaft.Facing);
            EditorUtility.SetDirty(doorway);
        }

        private static GameObject Place(Scene scene, string prefabPath, Vector2 floor)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("Lower Quarters: missing palette prefab " + prefabPath + " (Alice/Level/Fill Palette From Existing Objects).");
                return null;
            }

            return AliceLevelRoomTools.Place(scene, prefab, floor);
        }

        private static void Soldier(Scene scene, float x, int floor, int facing)
        {
            Transform gameplay = AliceLevelGeometry.EnsureRoot(scene, "Gameplay");
            AliceCombatSetup.PlaceSoldier(scene, gameplay, new Vector2(x, floor), facing);
        }

        private static void Door(Scene scene, float x, int floor, int latchSide)
        {
            GameObject door = Place(scene, ShortcutDoorPrefab, new Vector2(x, floor));
            if (door == null)
            {
                return;
            }

            var serialized = new SerializedObject(door.GetComponent<PermanentDoor>());
            serialized.FindProperty("_latchSide").intValue = latchSide;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // World map of the slice (tiles, bottom-left corners), also in Docs/LowerQuartersRooms.md:
        //   L01 (0, 0) 80×23    L02 (80, 0) 64×39   L03 (144, 16) 48×23   L04 (192, 16) 56×50
        //   L05 (96, 39) 96×34  L06 (64, 39) 32×23  L07 (16, 39) 48×23    L08 (-24, 39) 40×46
        //   reserved: H00 (0, 23) 64×16 over L01; L10 (64, 23) 16×16 between H00 and L02; H01 over L08.
        // Strokes must stay off the side walls (x 2 … width - 2): a one-way stroke replaces solid cells and would
        // open the wall onto the neighbour.
        private static List<Plan> Plans()
        {
            var plans = new List<Plan>();

            // L01 · Flooded corridor: the first room under control (GDD 0.31). Basics without enemies: run, jumps of
            // 2 and 3, a drop and a first 4-tile gap over deep water. The entry shaft from H00 is open in the ceiling;
            // the shelf with "Drink me" is 6 above the floor, a fan gate for later.
            plans.Add(new Plan { Id = "L01", World = new Vector2Int(0, 0), Width = 80, Height = 23, Start = new Vector2(6f, 6f) }
                .Solid(0, 0, 14, 6).Solid(14, 0, 24, 4)
                .Pit(24, 28, 2)
                .Solid(28, 0, 40, 4).Solid(40, 0, 46, 6).Solid(46, 0, 54, 9).Solid(54, 0, 68, 4).Solid(68, 0, 80, 2)
                .Solid(58, 9, 62, 10)
                .Clear(4, 21, 8, 23)
                .Door("D_L01_E", 1, 2, "L02", "D_L02_W"));

            // L02 · The Mouse's shore: deep water under a catwalk, the dry staircase up to L03, and over the water a
            // climb of catwalks to the far shore: the last gap, 6 tiles at the same height, needs the fan, and leads to
            // the ledge at the opening of L10 (world y 25, above L01). The lift from L06 comes down through the ceiling
            // over the shore (world x 86–90).
            plans.Add(new Plan { Id = "L02", World = new Vector2Int(80, 0), Width = 64, Height = 39, Start = new Vector2(5f, 2f) }
                .Solid(0, 0, 16, 2)
                .Pit(16, 36, 2)
                .OneWay(16, 4, 36, 5)
                .OneWay(26, 8, 32, 9).OneWay(18, 12, 24, 13).OneWay(26, 16, 32, 17).OneWay(18, 20, 24, 21).OneWay(12, 24, 18, 25)
                .Solid(36, 0, 40, 2).Solid(40, 0, 44, 4).Solid(44, 0, 48, 7).Solid(48, 0, 52, 11).Solid(52, 0, 56, 14)
                .Solid(56, 0, 64, 18)
                .Solid(0, 23, 6, 25).Clear(0, 25, 2, 29)
                .Clear(6, 37, 10, 39)
                .Door("D_L02_W", -1, 2, "L01", "D_L01_E")
                .Door("D_L02_E", 1, 18, "L03", "D_L03_W")
                .Point(MapIconKind.GateFan, 3f, 26.5f).Point(MapIconKind.Lift, 8f, 35f)
                .Drop("D_L02_LIFT", new Vector2(8f, 38.5f), new Vector2(8f, 2f), 1, "L06", "D_L06_LIFT"));

            // L03 · Stairs and the first niche (E04), beside L02 and under L05: the needle corner, the bolted door, the
            // first lackey alone on 20 tiles of flat floor, and the rest niche on a one-way shelf.
            plans.Add(new Plan
            {
                Id = "L03", World = new Vector2Int(144, 16), Width = 48, Height = 23, Start = new Vector2(6f, 2f),
                Furnish = scene =>
                {
                    Door(scene, 16f, 2, -1);
                    Soldier(scene, 28f, 2, -1);
                    Place(scene, ChairPrefab, new Vector2(42f, 6f));
                }
            }
                .Solid(0, 0, 48, 2)
                .OneWay(38, 5, 46, 6)
                .Solid(36, 10, 46, 21)
                .Door("D_L03_W", -1, 2, "L02", "D_L02_E")
                .Door("D_L03_E", 1, 2, "L04", "D_L04_W"));

            // L04 · Entrance to the race, the turn of the loop: in at the bottom from L03, the first collector alone,
            // then catwalks up to two exits on the west wall, one per tier of L05 (world y 41 and 55).
            plans.Add(new Plan
            {
                Id = "L04", World = new Vector2Int(192, 16), Width = 56, Height = 50, Start = new Vector2(6f, 2f),
                Furnish = scene => Soldier(scene, 35f, 2, -1)
            }
                .Solid(0, 0, 56, 2)
                .OneWay(10, 5, 18, 6).OneWay(22, 9, 30, 10).OneWay(34, 13, 42, 14).OneWay(44, 17, 52, 18).OneWay(32, 21, 40, 22)
                .Solid(0, 23, 28, 25)
                .OneWay(32, 28, 40, 29).OneWay(44, 32, 52, 33).OneWay(32, 35, 40, 36)
                .Solid(0, 37, 28, 39)
                .Door("D_L04_W", -1, 2, "L03", "D_L03_E")
                .Door("D_L04_WL", -1, 25, "L05", "D_L05_EL")
                .Door("D_L04_WH", -1, 39, "L05", "D_L05_EH"));

            // L05 · The drying track, westward above L02 and L03: two tiers in from L04; below, blood pits 3–4 wide;
            // above, platforms with gaps of 4–5. The tiers meet on the west landing at y 6. Enemies in combinations.
            plans.Add(new Plan
            {
                Id = "L05", World = new Vector2Int(96, 39), Width = 96, Height = 34, Start = new Vector2(90f, 2f),
                Furnish = scene =>
                {
                    Soldier(scene, 66f, 2, 1);
                    Soldier(scene, 44f, 16, 1);
                    Soldier(scene, 26f, 2, 1);
                }
            }
                .Solid(76, 0, 96, 2).Pit(73, 76, 2).Solid(56, 0, 73, 2).Pit(52, 56, 2)
                .Solid(36, 0, 52, 2).Pit(33, 36, 2).Solid(0, 0, 33, 2)
                .Solid(82, 14, 96, 16).Solid(68, 14, 78, 16).Solid(52, 14, 63, 16).Solid(36, 14, 48, 16).Solid(22, 14, 32, 16)
                .Solid(0, 0, 18, 6)
                .Door("D_L05_EL", 1, 2, "L04", "D_L04_WL")
                .Door("D_L05_EH", 1, 16, "L04", "D_L04_WH")
                .Door("D_L05_W", -1, 6, "L06", "D_L06_E"));

            // L06 · The refuge before the boss, above the shore of L02: a rest chair, Mother Crab's corner (later)
            // and the lift down to L02 — for now a one-way drop through a hole in the floor (world x 86–90).
            plans.Add(new Plan
            {
                Id = "L06", World = new Vector2Int(64, 39), Width = 32, Height = 23, Start = new Vector2(16f, 2f),
                Furnish = scene => Place(scene, ChairPrefab, new Vector2(12f, 2f))
            }
                .Solid(0, 0, 32, 2).Solid(26, 0, 32, 6).Clear(22, 0, 26, 2)
                .Door("D_L06_E", 1, 6, "L05", "D_L05_W")
                .Door("D_L06_W", -1, 2, "L07", "D_L07_E")
                .Point(MapIconKind.Lift, 24f, 3.5f)
                .Drop("D_L06_LIFT", new Vector2(24f, 0.5f), new Vector2(20f, 2f), -1, "L02", "D_L02_LIFT"));

            // L07 · The drive — Dodo's arena: 40 flat tiles, ceiling 12 above the floor, a one-way beam at each end.
            // The west door stands in for Dodo's key until the boss exists; its latch faces the arena's entrance.
            plans.Add(new Plan
            {
                Id = "L07", World = new Vector2Int(16, 39), Width = 48, Height = 23, Start = new Vector2(42f, 2f),
                Furnish = scene => Door(scene, 8f, 2, 1)
            }
                .Solid(0, 0, 48, 2).Solid(0, 14, 48, 23)
                .OneWay(6, 5, 12, 6).OneWay(36, 5, 42, 6)
                .Door("D_L07_E", 1, 2, "L06", "D_L06_W")
                .Door("D_L07_W", -1, 2, "L08", "D_L08_E"));

            // L08 · The fan and the upper drain (E07), west of the arena: the fan lies right before a 6-tile wall; on
            // top, a 6-tile gap; then ledges 4 apart up to the drain shaft in the ceiling (to H01, not built yet).
            plans.Add(new Plan
            {
                Id = "L08", World = new Vector2Int(-24, 39), Width = 40, Height = 46, Start = new Vector2(35f, 2f),
                Furnish = scene => Place(scene, FanPrefab, new Vector2(30f, 2f))
            }
                .Solid(0, 0, 40, 2).Solid(18, 0, 26, 8).Solid(0, 0, 12, 8).Solid(0, 0, 6, 12)
                .OneWay(8, 15, 14, 16).OneWay(2, 19, 6, 20).OneWay(8, 23, 14, 24).OneWay(2, 27, 6, 28)
                .OneWay(8, 31, 14, 32).OneWay(2, 35, 6, 36).OneWay(6, 39, 12, 40)
                .Clear(6, 44, 10, 46)
                .Point(MapIconKind.GateFan, 22f, 9.5f)
                .Door("D_L08_E", 1, 2, "L07", "D_L07_W"));

            return plans;
        }
    }
}
