using System;
using System.IO;
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
    /// M3 rest and save in a prototype room: the armchair and decree-pile prefabs, and a menu that gives the
    /// open scene its room ID, an armchair near Alice's start, the pile that shows a lost purse and the save.
    /// Idempotent: what is already in the scene is kept as it is.
    /// </summary>
    public static class AliceRestSetup
    {
        private const string Root = "Assets/_Alice";
        private const string ChairPrefabPath = Root + "/Prefabs/World/RestChair.prefab";
        private const string CachePrefabPath = Root + "/Prefabs/World/DecreeCache.prefab";
        private const string RestArtFolder = Root + "/Art/World/Rest";

        // Bottom centre of each prop, in pixels from where Alice stands; Alice herself sorts at 10.
        private static readonly RestProp[] RestProps =
        {
            new RestProp("Chair", "rest_chair.png", -6, 4),
            new RestProp("SpoolRed", "rest_spool_red.png", 40, 5),
            new RestProp("SpoolBlue", "rest_spool_blue.png", 58, 6),
            new RestProp("Cushion", "rest_cushion.png", 21, 12)
        };

        private readonly struct RestProp
        {
            public RestProp(string name, string file, int pixelsX, int order)
            {
                Name = name;
                File = file;
                PixelsX = pixelsX;
                Order = order;
            }

            public string Name { get; }
            public string File { get; }
            public int PixelsX { get; }
            public int Order { get; }
        }
        private const string DaisyFolder = Root + "/Art/World/Decrees";
        private const int DaisyFrames = 8;
        private const float DaisyFrameTime = 0.18f;
        private const int PileSortingOrder = 8;
        // Close to the start, out of the sight of the arena's soldiers (9 tiles from their posts 12 tiles away).
        private const float ChairDistanceFromStart = 1f;

        [MenuItem("Alice/Prototype/Add Rest To Open Scene")]
        public static void AddToOpenSceneFromMenu()
        {
            AddTo(SceneManager.GetActiveScene());
        }

        public static void AddTo(Scene scene)
        {
            GameObject chairPrefab = EnsureChairPrefab();
            GameObject cachePrefab = EnsureCachePrefab();
            string suffix = SuffixFor(scene.name);

            Room room = UnityEngine.Object.FindFirstObjectByType<Room>();
            if (room == null)
            {
                GameObject roomObject = new GameObject("Room");
                SceneManager.MoveGameObjectToScene(roomObject, scene);
                room = roomObject.AddComponent<Room>();
                room.RoomId = "P_" + suffix;
            }

            if (UnityEngine.Object.FindFirstObjectByType<SaveGame>() == null)
            {
                GameObject save = new GameObject("SaveGame");
                SceneManager.MoveGameObjectToScene(save, scene);
                save.AddComponent<SaveGame>();
            }

            if (UnityEngine.Object.FindFirstObjectByType<DecreeCache>() == null && cachePrefab != null)
            {
                PrefabUtility.InstantiatePrefab(cachePrefab, scene);
            }

            if (UnityEngine.Object.FindFirstObjectByType<RestPoint>() == null && chairPrefab != null)
            {
                PlayerMotor alice = UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();
                Vector3 start = alice != null ? alice.transform.position : Vector3.zero;
                GameObject chair = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, scene);
                // Feet on the floor Alice starts on, whole pixels apart from her.
                chair.transform.position = new Vector3(Mathf.Round(start.x - ChairDistanceFromStart), start.y, 0f);
                chair.GetComponent<RestPoint>().RestId = "R_" + suffix + "_01";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new IOException("Could not save scene: " + scene.path);
            }
        }

        private static string SuffixFor(string sceneName)
        {
            string name = sceneName.StartsWith("Prototype_", StringComparison.Ordinal)
                ? sceneName.Substring("Prototype_".Length)
                : sceneName;
            return name.ToUpperInvariant();
        }

        /// <summary>
        /// The rest: the armchair facing the viewer, two giant spools and, nearer the camera, the pincushion with
        /// needles as long as Alice's (owner's layout of 4 Nov 2026). Each is its own sprite standing on the
        /// floor; offsets are whole pixels from the point where Alice stands to rest, matching her rest frames.
        /// The cushion sorts in front of her, so it hides the buried part of her needle. An older prefab without
        /// the cushion is rebuilt under the same path, keeping its GUID and the scene instances.
        /// </summary>
        internal static GameObject EnsureChairPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ChairPrefabPath);
            if (existing != null && existing.transform.Find("Cushion") != null)
            {
                return existing;
            }

            AliceProjectSetup.EnsureFolder(Root + "/Prefabs/World");
            GameObject root = new GameObject("RestChair");
            try
            {
                BoxCollider2D zone = root.AddComponent<BoxCollider2D>();
                zone.isTrigger = true;
                zone.size = new Vector2(3f, 3f);
                zone.offset = new Vector2(0f, 1.5f);
                root.AddComponent<RestPoint>();
                foreach (RestProp prop in RestProps)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{RestArtFolder}/{prop.File}");
                    if (sprite == null)
                    {
                        Debug.LogWarning($"Rest sprite missing: {RestArtFolder}/{prop.File}");
                        return existing;
                    }

                    GameObject art = new GameObject(prop.Name);
                    art.transform.SetParent(root.transform, false);
                    art.transform.localPosition = new Vector3(prop.PixelsX / (float)Alice.Core.WorldMetrics.PixelsPerUnit, 0f, 0f);
                    SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = prop.Order;
                }

                return PrefabUtility.SaveAsPrefabAsset(root, ChairPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// The lost purse shows as a daisy whose petals are rolled decrees, swaying where Alice fell (owner's
        /// choice of 4 Nov 2026). An older prefab without the sway is rebuilt.
        /// </summary>
        internal static GameObject EnsureCachePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CachePrefabPath);
            if (existing != null && existing.GetComponentInChildren<SpriteLoop>(true) != null)
            {
                return existing;
            }

            Sprite[] frames = new Sprite[DaisyFrames];
            for (int index = 0; index < DaisyFrames; index++)
            {
                frames[index] = AssetDatabase.LoadAssetAtPath<Sprite>($"{DaisyFolder}/decree_daisy_{index:D2}.png");
                if (frames[index] == null)
                {
                    Debug.LogWarning("Decree daisy frame missing in " + DaisyFolder);
                    return existing;
                }
            }

            AliceProjectSetup.EnsureFolder(Root + "/Prefabs/World");
            GameObject root = new GameObject("DecreeCache");
            try
            {
                DecreeCache cache = root.AddComponent<DecreeCache>();
                GameObject pile = new GameObject("Daisy");
                pile.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = pile.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingOrder = PileSortingOrder;
                pile.AddComponent<SpriteLoop>().Bind(frames, DaisyFrameTime);
                pile.SetActive(false);
                SerializedObject serialized = new SerializedObject(cache);
                serialized.FindProperty("_visual").objectReferenceValue = pile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, CachePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
