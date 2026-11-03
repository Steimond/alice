using System;
using System.IO;
using Alice.Abilities;
using Alice.Core;
using Alice.Player;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Builds the M1 grey traversal room: Alice prefab, movement settings, pixel-perfect camera
    /// and a measured obstacle course. One tile is one unit; the grid lines on blocks are a ruler.
    /// M2 adds Alice's combat components and a card soldier after the fan gap.
    /// </summary>
    public static class AlicePrototypeBuilder
    {
        private const string Root = "Assets/_Alice";
        private const string ScenePath = Root + "/Scenes/Prototype_Traversal.unity";
        private const string TilePath = Root + "/Art/Prototype/GreyTile.png";
        private const string SettingsPath = Root + "/Data/Player/AliceMovement.asset";
        private const string PrefabPath = Root + "/Prefabs/Characters/Alice.prefab";
        private const string RoomName = "GreyRoom";
        private const float OneWayThickness = 0.5f;

        private static readonly Rect RoomBounds = new Rect(-4f, -10f, 116f, 32f);
        private static readonly Vector2 SpawnPosition = new Vector2(2f, 0f);

        private static readonly Color SolidColor = new Color(0.42f, 0.42f, 0.45f);
        private static readonly Color OneWayColor = new Color(0.62f, 0.58f, 0.5f);
        private static readonly Color HazardColor = new Color(0.6f, 0.08f, 0.08f, 0.6f);
        private static readonly Color PickupColor = new Color(0.95f, 0.8f, 0.2f);
        private static readonly Color BackdropColor = new Color(0.1f, 0.09f, 0.12f);

        private enum BlockKind
        {
            Decoration,
            Solid,
            OneWay,
            Hazard,
            Pickup
        }

        [MenuItem("Alice/Prototype/Build Traversal Room")]
        public static void BuildFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Build(false);
            }
        }

        [MenuItem("Alice/Prototype/Rebuild Grey Room Geometry")]
        public static void RebuildRoomFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Build(true);
            }
        }

        // Batch entry point: -executeMethod Alice.Editor.AlicePrototypeBuilder.BuildBatch
        public static void BuildBatch()
        {
            Build(false);
            Debug.Log("ALICE_PROTOTYPE_COMPLETE");
        }

        // Batch entry point after a change to level limits: -executeMethod Alice.Editor.AlicePrototypeBuilder.RebuildRoomBatch
        public static void RebuildRoomBatch()
        {
            Build(true);
            Debug.Log("ALICE_PROTOTYPE_COMPLETE");
        }

        /// <summary>Creates only what is missing; <paramref name="rebuildRoom"/> replaces the GreyRoom geometry.</summary>
        private static void Build(bool rebuildRoom)
        {
            AliceProjectSetup.EnsureFolder(Root + "/Art/Prototype");
            AliceProjectSetup.EnsureFolder(Root + "/Data/Player");

            Sprite tile = EnsureTileSprite();
            PlayerMovementSettings settings = EnsureSettings();
            GameObject prefab = EnsureAlicePrefab(tile, settings);
            AlicePlayerSpritesSetup.Ensure();
            AliceCombatSetup.EnsureAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject world = FindRoot(scene, "World");
            GameObject gameplay = FindRoot(scene, "Gameplay");
            Camera camera = FindRoot(scene, "Main Camera").GetComponent<Camera>();

            Transform room = world.transform.Find(RoomName);
            if (room != null && rebuildRoom)
            {
                UnityEngine.Object.DestroyImmediate(room.gameObject);
                room = null;
            }

            if (room == null)
            {
                BuildRoom(world.transform, tile);
            }

            Transform spawn = gameplay.transform.Find("PlayerSpawn");
            if (spawn.position == Vector3.zero)
            {
                spawn.position = SpawnPosition;
            }

            PlayerMotor player = gameplay.GetComponentInChildren<PlayerMotor>(true);
            if (player == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(gameplay.transform, false);
                instance.transform.position = spawn.position;
                player = instance.GetComponent<PlayerMotor>();
            }

            ConfigureCamera(camera, player.transform);
            AliceCombatSetup.EnsureSoldierInScene(scene, gameplay.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new IOException("Could not save scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject found = Array.Find(scene.GetRootGameObjects(), root => root.name == name);
            if (found == null)
            {
                throw new InvalidOperationException($"Scene {scene.path} has no root object '{name}'.");
            }

            return found;
        }

        private static Sprite EnsureTileSprite()
        {
            if (!File.Exists(TilePath))
            {
                // White tile with a one-pixel grey line on the left and bottom edges:
                // tiled blocks then show a single-pixel grid of whole tiles.
                Texture2D texture = new Texture2D(WorldMetrics.TilePixels, WorldMetrics.TilePixels, TextureFormat.RGBA32, false);
                try
                {
                    Color line = new Color(0.78f, 0.78f, 0.78f);
                    for (int y = 0; y < WorldMetrics.TilePixels; y++)
                    {
                        for (int x = 0; x < WorldMetrics.TilePixels; x++)
                        {
                            texture.SetPixel(x, y, x == 0 || y == 0 ? line : Color.white);
                        }
                    }

                    File.WriteAllBytes(TilePath, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                AssetDatabase.ImportAsset(TilePath);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TilePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = WorldMetrics.PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                TextureImporterSettings textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);
        }

        private static PlayerMovementSettings EnsureSettings()
        {
            PlayerMovementSettings settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            return settings;
        }

        private static GameObject EnsureAlicePrefab(Sprite tile, PlayerMovementSettings settings)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("Alice");
            try
            {
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.size = TraversalMetrics.StandingSize;
                collider.offset = new Vector2(0f, TraversalMetrics.StandingSize.y * 0.5f);

                root.AddComponent<PlayerInputReader>();
                root.AddComponent<AbilityInventory>();
                PlayerMotor motor = root.AddComponent<PlayerMotor>();
                motor.Settings = settings;
                root.AddComponent<PlayerRespawn>();

                SpriteRenderer bodyRenderer = CreateTiledRenderer("Body", root.transform, tile, TraversalMetrics.StandingSize, 10);
                bodyRenderer.transform.localPosition = new Vector3(0f, TraversalMetrics.StandingSize.y * 0.5f, 0f);
                SpriteRenderer marker = CreateTiledRenderer("FacingMarker", root.transform, tile, new Vector2(0.25f, 0.25f), 11);
                marker.color = new Color(0.55f, 0.1f, 0.12f);

                root.AddComponent<PlayerPlaceholderView>().Bind(motor, bodyRenderer, marker.transform);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureCamera(Camera camera, Transform player)
        {
            if (!camera.TryGetComponent(out PixelPerfectCamera pixelPerfect))
            {
                pixelPerfect = camera.gameObject.AddComponent<PixelPerfectCamera>();
                pixelPerfect.assetsPPU = WorldMetrics.PixelsPerUnit;
                pixelPerfect.refResolutionX = WorldMetrics.ReferenceWidth;
                pixelPerfect.refResolutionY = WorldMetrics.ReferenceHeight;
                pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            }

            if (!camera.TryGetComponent(out CameraFollow2D follow))
            {
                follow = camera.gameObject.AddComponent<CameraFollow2D>();
                follow.Bounds = RoomBounds;
            }

            if (follow.Target == null)
            {
                follow.Target = player;
                camera.transform.position = new Vector3(player.position.x, player.position.y + 2.5f, -10f);
            }
        }

        private static void BuildRoom(Transform world, Sprite tile)
        {
            Transform room = new GameObject(RoomName).transform;
            room.SetParent(world, false);

            Block(room, tile, "Backdrop", -4f, -10f, 112f, 22f, BlockKind.Decoration);

            // Frame.
            Block(room, tile, "WallLeft", -4f, -6f, -2f, 22f, BlockKind.Solid);
            Block(room, tile, "WallRight", 110f, -6f, 112f, 22f, BlockKind.Solid);
            Block(room, tile, "Ceiling", -4f, 20f, 112f, 22f, BlockKind.Solid);
            Block(room, tile, "PitHazard", -2f, -10f, 110f, -6.5f, BlockKind.Hazard);

            // The course measures the level limits from TraversalMetrics; everything sits on whole tiles.
            const int jumpLedge = TraversalMetrics.MaxJumpLedge;
            const int jumpGap = TraversalMetrics.MaxJumpGap;

            // Ledges 2..limit, plus one tile above the limit that a ground jump cannot clear.
            Block(room, tile, "Floor_Start", -2f, -6f, 30f, 0f, BlockKind.Solid);
            Block(room, tile, "Ledge_H2", 6f, 0f, 9f, 2f, BlockKind.Solid);
            Block(room, tile, "Ledge_H3", 12f, 0f, 15f, 3f, BlockKind.Solid);
            Block(room, tile, $"Ledge_H{jumpLedge}", 18f, 0f, 21f, jumpLedge, BlockKind.Solid);
            Block(room, tile, $"Ledge_H{jumpLedge + 1}", 24f, 0f, 27f, jumpLedge + 1, BlockKind.Solid);

            // Gaps of 3, 4 and the jump limit, then the fan gap.
            Block(room, tile, "Pad_AfterGap3", 33f, -6f, 39f, 0f, BlockKind.Solid);
            Block(room, tile, "Pad_AfterGap4", 43f, -6f, 49f, 0f, BlockKind.Solid);
            Block(room, tile, $"Pad_AfterGap{jumpGap}", 49f + jumpGap, -6f, 55f + jumpGap, 0f, BlockKind.Solid);
            Block(room, tile, "Pickup_Fan_A01", 51.5f + jumpGap, 0.5f, 52.5f + jumpGap, 1.5f, BlockKind.Pickup);
            float fanGapEnd = 55f + jumpGap + TraversalMetrics.MaxFanGap;
            Block(room, tile, $"Floor_AfterGap{TraversalMetrics.MaxFanGap}", fanGapEnd, -6f, 94f, 0f, BlockKind.Solid);

            // One-way platforms one ledge limit apart; Down + Jump drops through.
            for (int level = 1; level <= 3; level++)
            {
                float top = level * jumpLedge;
                Block(room, tile, $"OneWay_{top}", 70f, top - OneWayThickness, 75f, top, BlockKind.OneWay);
            }

            // Passage of slide height, 8 tiles long: standing does not fit.
            Block(room, tile, "SlideTunnel_Ceiling", 82f, TraversalMetrics.SlidePassageHeight, 90f, 5f, BlockKind.Solid);

            // Wall at the fan ledge limit: ground jump plus fan jump.
            Block(room, tile, $"FanWall_H{TraversalMetrics.MaxFanLedge}", 94f, -6f, 110f, TraversalMetrics.MaxFanLedge,
                BlockKind.Solid);
        }

        private static void Block(Transform parent, Sprite tile, string name,
            float xMin, float yMin, float xMax, float yMax, BlockKind kind)
        {
            Vector2 size = new Vector2(xMax - xMin, yMax - yMin);
            int order = kind == BlockKind.Decoration ? -100 : kind == BlockKind.Hazard ? 5 : 0;
            SpriteRenderer renderer = CreateTiledRenderer(name, parent, tile, size, order);
            renderer.transform.position = new Vector3(xMin + size.x * 0.5f, yMin + size.y * 0.5f, 0f);
            renderer.color = kind switch
            {
                BlockKind.Decoration => BackdropColor,
                BlockKind.OneWay => OneWayColor,
                BlockKind.Hazard => HazardColor,
                BlockKind.Pickup => PickupColor,
                _ => SolidColor
            };

            if (kind == BlockKind.Decoration)
            {
                return;
            }

            GameObject block = renderer.gameObject;
            BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
            collider.size = size;
            switch (kind)
            {
                case BlockKind.OneWay:
                    collider.usedByEffector = true;
                    PlatformEffector2D effector = block.AddComponent<PlatformEffector2D>();
                    effector.useOneWay = true;
                    effector.surfaceArc = 170f;
                    break;
                case BlockKind.Hazard:
                    collider.isTrigger = true;
                    block.AddComponent<FallHazard>();
                    break;
                case BlockKind.Pickup:
                    collider.isTrigger = true;
                    block.AddComponent<AbilityPickup>().AbilityId = AbilityIds.Fan;
                    break;
            }
        }

        private static SpriteRenderer CreateTiledRenderer(string name, Transform parent, Sprite tile, Vector2 size, int order)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = tile;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
