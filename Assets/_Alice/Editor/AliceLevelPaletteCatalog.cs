using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// The objects of the Level Palette. A category is a folder under Prefabs/Level and its entries are the
    /// prefabs in it, so a new prop or enemy appears in the palette as soon as its prefab is dropped there.
    ///
    /// The first fill takes what already exists: variants of the chair, the soldier and the decree pile, and the
    /// gate, door and item objects the M3 builders assembled (with their generated art), copied out of the M3
    /// rooms. It only creates prefabs that are missing, so hand edits to the palette prefabs survive.
    /// </summary>
    public static class AliceLevelPaletteCatalog
    {
        public const string Root = "Assets/_Alice/Prefabs/Level";
        private const string RoomsFolder = "Assets/_Alice/Scenes/M3";

        public sealed class Category
        {
            public string Folder;
            public string Label;
            public List<GameObject> Prefabs = new List<GameObject>();
        }

        // Folder name and the label the palette shows. Folders not listed here still appear, under their name.
        private static readonly (string Folder, string Label)[] KnownCategories =
        {
            ("Rest", "Отдых"),
            ("Transitions", "Переходы"),
            ("Gates", "Затворы"),
            ("Items", "Находки"),
            ("Enemies", "Враги"),
            ("Props", "Реквизит"),
            ("Service", "Служебное")
        };

        public static List<Category> Load()
        {
            var categories = new List<Category>();
            if (!AssetDatabase.IsValidFolder(Root))
            {
                return categories;
            }

            IEnumerable<string> folders = AssetDatabase.GetSubFolders(Root).Select(Path.GetFileName);
            List<string> ordered = KnownCategories.Select(known => known.Folder).Where(folders.Contains)
                .Concat(folders.Where(folder => KnownCategories.All(known => known.Folder != folder))).ToList();
            foreach (string folder in ordered)
            {
                var category = new Category
                {
                    Folder = folder,
                    Label = KnownCategories.FirstOrDefault(known => known.Folder == folder).Label ?? folder
                };
                category.Prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { Root + "/" + folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .OrderBy(path => path)
                    .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                    .Where(prefab => prefab != null)
                    .ToList();
                categories.Add(category);
            }

            return categories;
        }

        [MenuItem("Alice/Level/Fill Palette From Existing Objects")]
        public static void FillFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Level palette: leave Play mode first.");
                return;
            }

            Fill();
        }

        public static void Fill()
        {
            foreach ((string folder, string _) in KnownCategories)
            {
                AliceProjectSetup.EnsureFolder(Root + "/" + folder);
            }

            int created = 0;
            created += Variant("Assets/_Alice/Prefabs/World/RestChair.prefab", "Rest/RestChair");
            created += Variant("Assets/_Alice/Prefabs/Enemies/CardSoldier.prefab", "Enemies/CardSoldier");
            created += Variant("Assets/_Alice/Prefabs/World/DecreeCache.prefab", "Service/DecreeCache");
            created += CopyFromM3Rooms();
            AssetDatabase.SaveAssets();
            Debug.Log($"Level palette: {created} new prefabs in {Root}.");
        }

        private static int Variant(string sourcePath, string target)
        {
            string path = $"{Root}/{target}.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null || File.Exists(path))
            {
                return 0;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.position = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return 1;
        }

        // Opens the M3 rooms additively and copies one finished object of each kind. Rooms that were already open
        // are left open; the others are closed again without saving, so the rooms themselves never change.
        private static int CopyFromM3Rooms()
        {
            int created = 0;
            string[] paths = AssetDatabase.FindAssets("t:Scene", new[] { RoomsFolder })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
            foreach (string path in paths)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                if (!wasOpen)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                try
                {
                    created += CopyKinds(scene);
                }
                finally
                {
                    if (!wasOpen)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            return created;
        }

        private static int CopyKinds(Scene scene)
        {
            int created = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (RoomDoorway doorway in root.GetComponentsInChildren<RoomDoorway>(true))
                {
                    created += doorway.RequiresSmall
                        ? Copy(doorway.gameObject, "Transitions/LittleDoorway")
                        : Copy(doorway.gameObject, "Transitions/Doorway");
                }

                foreach (PermanentDoor door in root.GetComponentsInChildren<PermanentDoor>(true))
                {
                    created += Copy(door.gameObject, "Transitions/ShortcutDoor");
                }

                foreach (CheshireBridge bridge in root.GetComponentsInChildren<CheshireBridge>(true))
                {
                    created += Copy(bridge.gameObject, "Gates/CheshireBridge");
                }

                foreach (ClockGuillotine guillotine in root.GetComponentsInChildren<ClockGuillotine>(true))
                {
                    created += Copy(guillotine.gameObject, "Gates/Guillotine");
                }

                foreach (ClockMarker marker in root.GetComponentsInChildren<ClockMarker>(true))
                {
                    created += Copy(marker.gameObject, "Gates/ClockMarker");
                }

                foreach (AbilityPickup pickup in root.GetComponentsInChildren<AbilityPickup>(true))
                {
                    created += Copy(pickup.gameObject, "Items/Ability_" + pickup.AbilityId);
                }

                foreach (TrinketCache cache in root.GetComponentsInChildren<TrinketCache>(true))
                {
                    created += Copy(cache.gameObject, "Items/Trinket_" + cache.TrinketId);
                }
            }

            return created;
        }

        // The copy forgets where it stood and whom it led to: placing it gives it a position and a fresh ID, and
        // the doorway's target is the designer's to fill in.
        private static int Copy(GameObject source, string target)
        {
            string path = $"{Root}/{target}.prefab";
            if (File.Exists(path))
            {
                return 0;
            }

            GameObject copy = Object.Instantiate(source);
            copy.name = Path.GetFileName(target);
            copy.transform.position = Vector3.zero;
            if (copy.TryGetComponent(out RoomDoorway doorway))
            {
                var serialized = new SerializedObject(doorway);
                serialized.FindProperty("_targetRoomId").stringValue = string.Empty;
                serialized.FindProperty("_targetDoorwayId").stringValue = string.Empty;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            if (copy.TryGetComponent(out ClockMarker marker))
            {
                var serialized = new SerializedObject(marker);
                serialized.FindProperty("_mechanism").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(copy, path);
            Object.DestroyImmediate(copy);
            return 1;
        }
    }
}
