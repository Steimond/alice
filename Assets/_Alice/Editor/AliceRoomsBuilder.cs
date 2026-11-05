using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// The M3 test map: Bootstrap holds Alice, the camera, the save and the room loader; each room is its own
    /// scene. Three grey rooms in a ring — A with the armchair, B with a card soldier, C a passage — and a
    /// shortcut door in A that only opens from C's side, to prove that a permanent door survives rests,
    /// defeats and reloads. Room scenes are created once and then left alone, so hand edits survive.
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

        // Room geometry in tiles: floor top at y 0, walls at ±20 with an opening 5 tiles high at the floor.
        private const float HalfWidth = 20f;
        private const float WallThickness = 4f;
        private const float OpeningHeight = 5f;
        private const float CeilingY = 14f;

        private sealed class RoomPlan
        {
            public string Id;
            public string Scene;
            public string WestTarget;
            public string EastTarget;
            public string WestTargetDoorway;
            public string EastTargetDoorway;
            public System.Action<Scene, Transform> Furnish;
        }

        [MenuItem("Alice/Prototype/Build M3 Rooms")]
        public static void BuildFromMenu()
        {
            string previous = SceneManager.GetActiveScene().path;
            Build();
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        public static void Build()
        {
            Sprite tile = AlicePrototypeBuilder.EnsureTileSprite();
            PlayerMovementSettings movement = AlicePrototypeBuilder.EnsureSettings();
            GameObject alicePrefab = AlicePrototypeBuilder.EnsureAlicePrefab(tile, movement);
            AlicePlayerSpritesSetup.Ensure();
            AliceCombatSetup.EnsureAssets();
            GameObject chair = AliceRestSetup.EnsureChairPrefab();
            GameObject cache = AliceRestSetup.EnsureCachePrefab();
            AliceProjectSetup.EnsureFolder(RoomsFolder);

            List<RoomPlan> plans = new List<RoomPlan>
            {
                new RoomPlan
                {
                    Id = "M3_A", Scene = "M3_A", WestTarget = "M3_C", WestTargetDoorway = "D_M3C_E",
                    EastTarget = "M3_B", EastTargetDoorway = "D_M3B_W",
                    Furnish = (scene, gameplay) =>
                    {
                        if (chair != null)
                        {
                            GameObject rest = (GameObject)PrefabUtility.InstantiatePrefab(chair, scene);
                            rest.transform.SetParent(gameplay, false);
                            rest.transform.position = new Vector3(6f, 0f, 0f);
                            rest.GetComponent<RestPoint>().RestId = "R_M3A_01";
                        }

                        PlaceDoor(gameplay, tile, "D_M3A_W", new Vector2(-17f, 0f), -1);
                    }
                },
                new RoomPlan
                {
                    Id = "M3_B", Scene = "M3_B", WestTarget = "M3_A", WestTargetDoorway = "D_M3A_E",
                    EastTarget = "M3_C", EastTargetDoorway = "D_M3C_W",
                    Furnish = (scene, gameplay) => AliceCombatSetup.PlaceSoldier(scene, gameplay, new Vector2(6f, 0f), -1)
                },
                new RoomPlan
                {
                    Id = "M3_C", Scene = "M3_C", WestTarget = "M3_B", WestTargetDoorway = "D_M3B_E",
                    EastTarget = "M3_A", EastTargetDoorway = "D_M3A_W",
                    Furnish = (scene, gameplay) => { }
                }
            };

            foreach (RoomPlan plan in plans)
            {
                string path = $"{RoomsFolder}/{plan.Scene}.unity";
                if (!File.Exists(path))
                {
                    BuildRoom(plan, path, tile, cache);
                }

                AlicePrototypeBuilder.AddToBuildSettings(path);
            }

            RoomCatalog catalog = EnsureCatalog(plans);
            EnsureBootstrap(alicePrefab, catalog);
            AssetDatabase.SaveAssets();
        }

        private static void BuildRoom(RoomPlan plan, string path, Sprite tile, GameObject cache)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject roomObject = new GameObject("Room");
            Room room = roomObject.AddComponent<Room>();
            room.RoomId = plan.Id;
            room.ControlsCamera = true;
            room.CameraBounds = new Rect(-HalfWidth - WallThickness, -4f, (HalfWidth + WallThickness) * 2f, 24f);
            room.StartPosition = new Vector2(2f, 0f);

            Transform world = new GameObject("World").transform;
            Transform gameplay = new GameObject("Gameplay").transform;
            float outer = HalfWidth + WallThickness;
            AlicePrototypeBuilder.Block(world, tile, "Backdrop", -outer, 0f, outer, CeilingY,
                AlicePrototypeBuilder.BlockKind.Decoration);
            AlicePrototypeBuilder.Block(world, tile, "Floor", -outer, -2f, outer, 0f, AlicePrototypeBuilder.BlockKind.Solid);
            AlicePrototypeBuilder.Block(world, tile, "Ceiling", -outer, CeilingY, outer, CeilingY + 2f,
                AlicePrototypeBuilder.BlockKind.Solid);
            AlicePrototypeBuilder.Block(world, tile, "WallWest", -outer, OpeningHeight, -HalfWidth, CeilingY,
                AlicePrototypeBuilder.BlockKind.Solid);
            AlicePrototypeBuilder.Block(world, tile, "WallEast", HalfWidth, OpeningHeight, outer, CeilingY,
                AlicePrototypeBuilder.BlockKind.Solid);

            string letter = plan.Id.Substring(plan.Id.Length - 1);
            PlaceDoorway(gameplay, $"D_M3{letter}_W", -HalfWidth - 2.5f, plan.WestTarget, plan.WestTargetDoorway, 1);
            PlaceDoorway(gameplay, $"D_M3{letter}_E", HalfWidth + 2.5f, plan.EastTarget, plan.EastTargetDoorway, -1);
            if (cache != null)
            {
                GameObject pile = (GameObject)PrefabUtility.InstantiatePrefab(cache, scene);
                pile.transform.SetParent(gameplay, false);
            }

            plan.Furnish(scene, gameplay);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException("Could not save scene: " + path);
            }
        }

        /// <summary>A trigger in the wall opening; arriving through it lands Alice 3.5 tiles inside, facing in.</summary>
        private static void PlaceDoorway(Transform parent, string id, float x, string targetRoom, string targetDoorway,
            int inward)
        {
            GameObject doorway = new GameObject("Doorway_" + id);
            doorway.transform.SetParent(parent, false);
            doorway.transform.position = new Vector3(x, OpeningHeight * 0.5f, 0f);
            BoxCollider2D trigger = doorway.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1f, OpeningHeight);
            RoomDoorway component = doorway.AddComponent<RoomDoorway>();
            component.DoorwayId = id;
            component.Configure(targetRoom, targetDoorway, new Vector2(inward * 3.5f, -OpeningHeight * 0.5f), inward);
        }

        /// <summary>
        /// A shortcut door: a solid body one tile wide across the passage, a trigger around it where Interact
        /// reaches the latch, and the door art (generated frames when present, else a grey block).
        /// </summary>
        private static void PlaceDoor(Transform parent, Sprite tile, string id, Vector2 position, int latchSide)
        {
            GameObject door = new GameObject("Door_" + id);
            door.transform.SetParent(parent, false);
            door.transform.position = position;
            BoxCollider2D reach = door.AddComponent<BoxCollider2D>();
            reach.isTrigger = true;
            reach.size = new Vector2(5f, 4f);
            reach.offset = new Vector2(0f, 2f);

            GameObject body = new GameObject("Body");
            body.transform.SetParent(door.transform, false);
            BoxCollider2D solid = body.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1f, OpeningHeight);
            solid.offset = new Vector2(0f, OpeningHeight * 0.5f);

            GameObject art = new GameObject("Art");
            art.transform.SetParent(door.transform, false);
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = DoorSortingOrder;
            Sprite[] frames = LoadDoorFrames();
            if (frames.Length == 0)
            {
                renderer.sprite = tile;
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = new Vector2(1f, OpeningHeight);
                art.transform.localPosition = new Vector3(0f, OpeningHeight * 0.5f, 0f);
                renderer.color = new Color(0.35f, 0.22f, 0.15f);
            }
            else
            {
                renderer.sprite = frames[0];
            }

            PermanentDoor component = door.AddComponent<PermanentDoor>();
            component.DoorId = id;
            component.Bind(latchSide, solid, renderer, frames);
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

            if (catalog.Rooms == null || catalog.Rooms.Length == 0)
            {
                catalog.Rooms = plans.Select(plan => new RoomCatalog.Entry(plan.Id, plan.Scene, string.Empty)).ToArray();
                EditorUtility.SetDirty(catalog);
            }

            return catalog;
        }

        /// <summary>Alice, the camera following her, the save and the room loader live in Bootstrap.</summary>
        private static void EnsureBootstrap(GameObject alicePrefab, RoomCatalog catalog)
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject systems = roots.FirstOrDefault(root => root.name == "Systems") ?? new GameObject("Systems");
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
