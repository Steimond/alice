using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alice.Abilities;
using Alice.Player;
using Alice.Save;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// The M3 test map (GDD section 15: four kinds of spatial gate on a test route). Bootstrap holds Alice, the
    /// camera, the save and the room loader; each room is its own scene. Seven grey rooms in a chain that returns
    /// to the start: A the hub with the armchair; B a card soldier and the fan; C a 6-tile ledge only the fan
    /// climbs; D the mushroom and a little door; E the Cheshire step and a pit wider than any jump with a vanished
    /// bridge; F the clock and a guillotine too quick to run under; G the way back to A's shortcut door, which only
    /// opens from that side. Room scenes are created once and then left alone, so hand edits survive; the rebuild
    /// menu recreates them from scratch.
    /// </summary>
    public static class AliceRoomsBuilder
    {
        private const string Root = "Assets/_Alice";
        private const string BootstrapPath = Root + "/Scenes/Bootstrap.unity";
        private const string RoomsFolder = Root + "/Scenes/M3";
        private const string CatalogPath = Root + "/Data/World/Rooms.asset";
        private const string DoorArtFolder = Root + "/Art/World/Doors";
        private const string DoorFramePrefix = "door_";
        private const int DoorSortingOrder = 3;
        private const int PropSortingOrder = 2;

        // Room geometry in tiles: floor top at y 0, walls at ±20, openings 5 tiles high.
        private const float HalfWidth = 20f;
        private const float WallThickness = 4f;
        private const float OpeningHeight = 5f;
        private const float CeilingY = 14f;

        private static readonly Color PickupColor = new Color(0.95f, 0.8f, 0.2f);
        private static readonly Color SmallDoorColor = new Color(0.45f, 0.3f, 0.2f);
        private static readonly Color CheshireColor = new Color(0.6f, 0.4f, 0.85f);
        private static readonly Color ClockColor = new Color(0.85f, 0.75f, 0.45f);
        private static readonly Color BladeColor = new Color(0.62f, 0.64f, 0.7f);

        private sealed class Exit
        {
            public string Target;
            public string TargetDoorway;
            public float Floor;
            public bool Small;
        }

        private sealed class RoomPlan
        {
            public string Id;
            public Exit West;
            public Exit East;
            public System.Action<Context> Furnish = context => { };
        }

        private sealed class Context
        {
            public Scene Scene;
            public Transform World;
            public Transform Gameplay;
            public Sprite Tile;
            public GameObject Chair;
            public string RoomId;
        }

        [MenuItem("Alice/Prototype/Build M3 Rooms")]
        public static void BuildFromMenu()
        {
            RunKeepingScene(() => Build(false));
        }

        [MenuItem("Alice/Prototype/Rebuild M3 Rooms (recreate scenes)")]
        public static void RebuildFromMenu()
        {
            RunKeepingScene(() => Build(true));
        }

        private static void RunKeepingScene(System.Action action)
        {
            string previous = SceneManager.GetActiveScene().path;
            action();
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        public static void Build(bool recreate)
        {
            Sprite tile = AlicePrototypeBuilder.EnsureTileSprite();
            PlayerMovementSettings movement = AlicePrototypeBuilder.EnsureSettings();
            GameObject alicePrefab = AlicePrototypeBuilder.EnsureAlicePrefab(tile, movement);
            AlicePlayerSpritesSetup.Ensure();
            AliceCombatSetup.EnsureAssets();
            GameObject chair = AliceRestSetup.EnsureChairPrefab();
            GameObject cache = AliceRestSetup.EnsureCachePrefab();
            AliceProjectSetup.EnsureFolder(RoomsFolder);

            List<RoomPlan> plans = Plans();
            if (recreate)
            {
                foreach (string stale in AssetDatabase.FindAssets("t:Scene", new[] { RoomsFolder }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    AssetDatabase.DeleteAsset(stale);
                }
            }

            foreach (RoomPlan plan in plans)
            {
                string path = $"{RoomsFolder}/{plan.Id}.unity";
                if (!File.Exists(path))
                {
                    BuildRoom(plan, path, tile, cache, chair);
                }

                AlicePrototypeBuilder.AddToBuildSettings(path);
            }

            RoomCatalog catalog = EnsureCatalog(plans);
            EnsureBootstrap(alicePrefab, catalog);
            AssetDatabase.SaveAssets();
        }

        private static List<RoomPlan> Plans()
        {
            Exit To(string room, string doorway, float floor = 0f, bool small = false) =>
                new Exit { Target = room, TargetDoorway = doorway, Floor = floor, Small = small };

            return new List<RoomPlan>
            {
                new RoomPlan
                {
                    Id = "M3_A", West = To("M3_G", "D_M3G_E"), East = To("M3_B", "D_M3B_W"),
                    Furnish = c =>
                    {
                        PlaceChair(c, new Vector2(6f, 0f), "R_M3A_01");
                        PlaceDoor(c, "D_M3A_W", new Vector2(-17f, 0f), -1);
                    }
                },
                new RoomPlan
                {
                    Id = "M3_B", West = To("M3_A", "D_M3A_E"), East = To("M3_C", "D_M3C_W"),
                    Furnish = c =>
                    {
                        AliceCombatSetup.PlaceSoldier(c.Scene, c.Gameplay, new Vector2(2f, 0f), -1);
                        PlacePickup(c, AbilityIds.Fan, new Vector2(15f, 0f));
                    }
                },
                new RoomPlan
                {
                    // A ledge of 6 tiles under the east exit: only the fan's second jump reaches it.
                    Id = "M3_C", West = To("M3_B", "D_M3B_E"), East = To("M3_D", "D_M3D_W", 6f),
                    Furnish = c => Solid(c, "FanLedge", 12f, 0f, HalfWidth, 6f)
                },
                new RoomPlan
                {
                    // The way on is a little door low in the east wall: a bite of the mushroom in front of it.
                    Id = "M3_D", West = To("M3_C", "D_M3C_E", 6f), East = To("M3_E", "D_M3E_W", 0f, true),
                    Furnish = c => PlacePickup(c, AbilityIds.Mushroom, new Vector2(-8f, 0f))
                },
                new RoomPlan
                {
                    // A pit 12 tiles wide, beyond even the fan; the vanished bridge spans it.
                    Id = "M3_E", West = To("M3_D", "D_M3D_E", 0f, true), East = To("M3_F", "D_M3F_W"),
                    Furnish = c =>
                    {
                        PlacePickup(c, AbilityIds.CheshireStep, new Vector2(-12f, 0f));
                        PlacePitAndBridge(c, -6f, 6f);
                    }
                },
                new RoomPlan
                {
                    Id = "M3_F", West = To("M3_E", "D_M3E_E"), East = To("M3_G", "D_M3G_W"),
                    Furnish = c =>
                    {
                        PlacePickup(c, AbilityIds.Clock, new Vector2(-12f, 0f));
                        PlaceGuillotine(c, new Vector2(4f, 0f));
                    }
                },
                new RoomPlan
                {
                    Id = "M3_G", West = To("M3_F", "D_M3F_E"), East = To("M3_A", "D_M3A_W")
                }
            };
        }

        private static void BuildRoom(RoomPlan plan, string path, Sprite tile, GameObject cache, GameObject chair)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Room room = new GameObject("Room").AddComponent<Room>();
            room.RoomId = plan.Id;
            room.ControlsCamera = true;
            room.CameraBounds = new Rect(-HalfWidth - WallThickness, -4f, (HalfWidth + WallThickness) * 2f, 24f);
            room.StartPosition = new Vector2(2f, 0f);

            Context context = new Context
            {
                Scene = scene,
                World = new GameObject("World").transform,
                Gameplay = new GameObject("Gameplay").transform,
                Tile = tile,
                Chair = chair,
                RoomId = plan.Id
            };

            float outer = HalfWidth + WallThickness;
            Block(context, "Backdrop", -outer, 0f, outer, CeilingY, AlicePrototypeBuilder.BlockKind.Decoration);
            Block(context, "Ceiling", -outer, CeilingY, outer, CeilingY + 2f, AlicePrototypeBuilder.BlockKind.Solid);
            if (plan.Id != "M3_E")
            {
                Solid(context, "Floor", -outer, -2f, outer, 0f);
            }

            BuildWall(context, plan.West, -1, outer);
            BuildWall(context, plan.East, 1, outer);

            string letter = plan.Id.Substring(plan.Id.Length - 1);
            PlaceDoorway(context, $"D_M3{letter}_W", -1, plan.West);
            PlaceDoorway(context, $"D_M3{letter}_E", 1, plan.East);
            if (cache != null)
            {
                GameObject pile = (GameObject)PrefabUtility.InstantiatePrefab(cache, scene);
                pile.transform.SetParent(context.Gameplay, false);
            }

            plan.Furnish(context);
            AliceGateArtSetup.Apply(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException("Could not save scene: " + path);
            }
        }

        /// <summary>The wall on one side: an opening at the exit's floor, or none but a little door painted low on it.</summary>
        private static void BuildWall(Context context, Exit exit, int side, float outer)
        {
            float inner = HalfWidth * side;
            float far = outer * side;
            float xMin = Mathf.Min(inner, far);
            float xMax = Mathf.Max(inner, far);
            if (exit.Small)
            {
                Solid(context, "Wall" + Side(side), xMin, 0f, xMax, CeilingY);
                return;
            }

            if (exit.Floor > 0f)
            {
                Solid(context, "WallBelow" + Side(side), xMin, 0f, xMax, exit.Floor);
            }

            Solid(context, "Wall" + Side(side), xMin, exit.Floor + OpeningHeight, xMax, CeilingY);
        }

        private static string Side(int side)
        {
            return side < 0 ? "West" : "East";
        }

        /// <summary>
        /// The doorway trigger. An open exit lands an arriving Alice 3.5 tiles inside, facing in; a little door is a
        /// low trigger against the wall that takes her only while small, and lands her 2 tiles inside. In front of
        /// it lies the reach where Interact bites the mushroom and opens the door.
        /// </summary>
        private static void PlaceDoorway(Context context, string id, int side, Exit exit)
        {
            GameObject doorway = new GameObject("Doorway_" + id);
            doorway.transform.SetParent(context.Gameplay, false);
            BoxCollider2D trigger = doorway.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            RoomDoorway component = doorway.AddComponent<RoomDoorway>();
            component.DoorwayId = id;
            int inward = -side;
            if (exit.Small)
            {
                const float doorHeight = 1.5f;
                doorway.transform.position = new Vector3(side * (HalfWidth - 0.5f), doorHeight * 0.5f, 0f);
                trigger.size = new Vector2(1.2f, doorHeight);
                component.RequiresSmall = true;
                component.Configure(exit.Target, exit.TargetDoorway, new Vector2(inward * 2f, -doorHeight * 0.5f), inward);
                SpriteRenderer art = Visual(context, "LittleDoor_" + id, doorway.transform.position, new Vector2(1f, doorHeight),
                    SmallDoorColor, PropSortingOrder);
                art.transform.SetParent(doorway.transform, true);
                AddLittleDoor(component, LittleDoorId(context.RoomId, exit.Target), inward);
                return;
            }

            doorway.transform.position = new Vector3(side * (HalfWidth + 2.5f), exit.Floor + OpeningHeight * 0.5f, 0f);
            trigger.size = new Vector2(1f, OpeningHeight);
            component.Configure(exit.Target, exit.TargetDoorway, new Vector2(inward * 3.5f, -OpeningHeight * 0.5f), inward);
        }

        /// <summary>One ID for both sides of a passage, so opening the door from one room opens it in the other.</summary>
        public static string LittleDoorId(string roomId, string otherRoomId)
        {
            return "LD_" + string.Join("_", new[] { roomId, otherRoomId }.OrderBy(id => id, System.StringComparer.Ordinal));
        }

        /// <summary>
        /// The little door's latch on a small doorway: its reach is a trigger in front of the door, clear of the
        /// doorway trigger itself, so she shrinks in the room and walks through on her own.
        /// </summary>
        public static void AddLittleDoor(RoomDoorway doorway, string doorId, int inward)
        {
            if (!doorway.TryGetComponent(out LittleDoor door))
            {
                door = doorway.gameObject.AddComponent<LittleDoor>();
            }

            door.DoorId = doorId;
            bool hasReach = doorway.GetComponentsInChildren<BoxCollider2D>(true).Any(box => box.gameObject != doorway.gameObject);
            if (!hasReach)
            {
                GameObject reach = new GameObject("LittleDoorReach");
                reach.transform.SetParent(doorway.transform, false);
                reach.transform.localPosition = new Vector3(inward * 2.5f, 0.25f, 0f);
                BoxCollider2D trigger = reach.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(3f, 2f);
            }
        }

        private static void PlaceChair(Context context, Vector2 position, string restId)
        {
            if (context.Chair == null)
            {
                return;
            }

            GameObject rest = (GameObject)PrefabUtility.InstantiatePrefab(context.Chair, context.Scene);
            rest.transform.SetParent(context.Gameplay, false);
            rest.transform.position = position;
            rest.GetComponent<RestPoint>().RestId = restId;
        }

        /// <summary>A permanent ability lying on the floor; touching it unlocks the ability for good.</summary>
        private static void PlacePickup(Context context, string abilityId, Vector2 position)
        {
            SpriteRenderer art = Visual(context, "Pickup_" + abilityId, position + new Vector2(0f, 0.75f), new Vector2(1f, 1f),
                PickupColor, PropSortingOrder);
            art.transform.SetParent(context.Gameplay, true);
            BoxCollider2D trigger = art.gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one;
            art.gameObject.AddComponent<AbilityPickup>().AbilityId = abilityId;
        }

        /// <summary>
        /// A pit between two floors, with a hazard below that returns Alice to the bank, and the Cheshire bridge
        /// across it: a shimmering outline that turns solid near Alice once she has the Cheshire Step.
        /// </summary>
        private static void PlacePitAndBridge(Context context, float pitMin, float pitMax)
        {
            float outer = HalfWidth + WallThickness;
            Solid(context, "FloorWest", -outer, -2f, pitMin, 0f);
            Solid(context, "FloorEast", pitMax, -2f, outer, 0f);
            Block(context, "Pit", pitMin, -6f, pitMax, -3f, AlicePrototypeBuilder.BlockKind.Hazard);

            SpriteRenderer surface = Visual(context, "CheshireBridge", new Vector2((pitMin + pitMax) * 0.5f, -0.25f),
                new Vector2(pitMax - pitMin, 0.5f), CheshireColor, PropSortingOrder);
            surface.transform.SetParent(context.Gameplay, true);
            BoxCollider2D body = surface.gameObject.AddComponent<BoxCollider2D>();
            body.size = new Vector2(pitMax - pitMin, 0.5f);
            surface.gameObject.AddComponent<CheshireBridge>().Bind(body, surface);
        }

        /// <summary>A blade that drops across the passage; the clock marker before it.</summary>
        private static void PlaceGuillotine(Context context, Vector2 position)
        {
            GameObject frame = new GameObject("Guillotine");
            frame.transform.SetParent(context.Gameplay, false);
            frame.transform.position = position;
            SpriteRenderer blade = Visual(context, "Blade", position + new Vector2(0f, OpeningHeight * 0.5f),
                new Vector2(1f, OpeningHeight), BladeColor, PropSortingOrder);
            blade.transform.SetParent(frame.transform, true);
            Rigidbody2D body = blade.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            BoxCollider2D solid = blade.gameObject.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1f, OpeningHeight);
            ClockGuillotine mechanism = frame.AddComponent<ClockGuillotine>();
            mechanism.Bind(blade.transform, solid);

            SpriteRenderer mark = Visual(context, "ClockMarker", position + new Vector2(-3f, 0.5f), new Vector2(1f, 1f),
                ClockColor, PropSortingOrder);
            mark.transform.SetParent(context.Gameplay, true);
            BoxCollider2D reach = mark.gameObject.AddComponent<BoxCollider2D>();
            reach.isTrigger = true;
            reach.size = new Vector2(3f, 3f);
            reach.offset = new Vector2(0f, 1f);
            mark.gameObject.AddComponent<ClockMarker>().Bind(mechanism, 5f);
        }

        /// <summary>
        /// A shortcut door: a solid body one tile wide across the passage, a trigger around it where Interact
        /// reaches the latch, and the door art (generated frames when present, else a brown block).
        /// </summary>
        private static void PlaceDoor(Context context, string id, Vector2 position, int latchSide)
        {
            GameObject door = new GameObject("Door_" + id);
            door.transform.SetParent(context.Gameplay, false);
            door.transform.position = position;
            BoxCollider2D reach = door.AddComponent<BoxCollider2D>();
            reach.isTrigger = true;
            reach.size = new Vector2(5f, 4f);
            reach.offset = new Vector2(0f, 2f);

            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(door.transform, false);
            BoxCollider2D solid = bodyObject.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1f, OpeningHeight);
            solid.offset = new Vector2(0f, OpeningHeight * 0.5f);

            GameObject art = new GameObject("Art");
            art.transform.SetParent(door.transform, false);
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = DoorSortingOrder;
            Sprite[] frames = LoadDoorFrames();
            if (frames.Length == 0)
            {
                renderer.sprite = context.Tile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(1f, OpeningHeight);
                art.transform.localPosition = new Vector3(0f, OpeningHeight * 0.5f, 0f);
                renderer.color = SmallDoorColor;
            }
            else
            {
                renderer.sprite = frames[0];
            }

            PermanentDoor component = door.AddComponent<PermanentDoor>();
            component.DoorId = id;
            component.Bind(latchSide, solid, renderer, frames);
        }

        private static void Solid(Context context, string name, float xMin, float yMin, float xMax, float yMax)
        {
            Block(context, name, xMin, yMin, xMax, yMax, AlicePrototypeBuilder.BlockKind.Solid);
        }

        private static void Block(Context context, string name, float xMin, float yMin, float xMax, float yMax,
            AlicePrototypeBuilder.BlockKind kind)
        {
            AlicePrototypeBuilder.Block(context.World, context.Tile, name, xMin, yMin, xMax, yMax, kind);
        }

        /// <summary>A working placeholder: the grey tile tinted, until the props are drawn.</summary>
        private static SpriteRenderer Visual(Context context, string name, Vector2 center, Vector2 size, Color color, int order)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = center;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = context.Tile;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Sprite[] LoadDoorFrames()
        {
            if (!AssetDatabase.IsValidFolder(DoorArtFolder))
            {
                return new Sprite[0];
            }

            return AssetDatabase.FindAssets(DoorFramePrefix + " t:Sprite", new[] { DoorArtFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(DoorFramePrefix))
                .OrderBy(path => path)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
        }

        private static RoomCatalog EnsureCatalog(List<RoomPlan> plans)
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/World");
            RoomCatalog catalog = AssetDatabase.LoadAssetAtPath<RoomCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RoomCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            // Every planned room is listed; rooms added by hand stay.
            List<RoomCatalog.Entry> entries = (catalog.Rooms ?? new RoomCatalog.Entry[0]).ToList();
            bool changed = false;
            foreach (RoomPlan plan in plans)
            {
                if (entries.All(entry => entry.RoomId != plan.Id))
                {
                    entries.Add(new RoomCatalog.Entry(plan.Id, plan.Id, string.Empty));
                    changed = true;
                }
            }

            if (changed)
            {
                catalog.Rooms = entries.ToArray();
                EditorUtility.SetDirty(catalog);
            }

            return catalog;
        }

        /// <summary>Alice, the camera following her, the save and the room loader live in Bootstrap.</summary>
        private static void EnsureBootstrap(GameObject alicePrefab, RoomCatalog catalog)
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            GameObject systems = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Systems");
            if (systems == null)
            {
                systems = new GameObject("Systems");
            }

            if (!systems.TryGetComponent(out SaveGame _))
            {
                systems.AddComponent<SaveGame>();
            }

            if (!systems.TryGetComponent(out RoomLoader loader))
            {
                loader = systems.AddComponent<RoomLoader>();
            }

            if (loader.Catalog == null)
            {
                loader.Catalog = catalog;
            }

            PlayerMotor alice = scene.GetRootGameObjects()
                .Select(root => root.GetComponentInChildren<PlayerMotor>(true)).FirstOrDefault(found => found != null);
            if (alice == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(alicePrefab, scene);
                instance.transform.position = Vector3.zero;
                alice = instance.GetComponent<PlayerMotor>();
            }

            Camera camera = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<Camera>(true))
                .FirstOrDefault(found => found != null);
            if (camera != null)
            {
                AlicePrototypeBuilder.ConfigureCamera(camera, alice.transform);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new IOException("Could not save scene: " + BootstrapPath);
            }
        }
    }
}
