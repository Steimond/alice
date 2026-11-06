using System.IO;
using System.Linq;
using Alice.Player;
using Alice.UI;
using Alice.World;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// Items as bundles (GDD 0.28): the Items localization table with every item's name in Russian and English, the
    /// item catalog (icon and name by ID), the pixel font, the pickup window on Alice, and the shimmering bundle in
    /// place of every ability pickup and trinket cache of the M3 rooms. Idempotent: names already in the table and
    /// entries already in the catalog are kept as edited.
    /// </summary>
    public static class AliceItemSetup
    {
        private const string Root = "Assets/_Alice";
        private const string TableName = "Items";
        private const string TableFolder = Root + "/Data/Localization";
        private const string CatalogPath = Root + "/Data/UI/ItemCatalog.asset";
        private const string HudSkinPath = Root + "/Data/UI/HudSkin.asset";
        private const string FontPath = Root + "/Art/Fonts/FusionPixel/FusionPixel12-Latin.otf";
        private const string BundleFolder = Root + "/Art/World/Items";
        private const string BundlePrefix = "item_bundle_";
        private const string AlicePrefabPath = Root + "/Prefabs/Characters/Alice.prefab";
        private const string RoomsFolder = Root + "/Scenes/M3";
        private const int FontSize = 12;
        private const float BundleFrameTime = 0.12f;
        private const int BundleSortingOrder = 2;
        private const int StillFrames = 14;
        private static readonly Vector2 BundleReach = new Vector2(2f, 2f);

        // ID, icon, Russian and English name. The names are working ones, like the items (GDD sections 5 and 10).
        private static readonly (string Id, string Icon, string Ru, string En)[] Items =
        {
            ("A01", Root + "/Art/World/Gates/gate_pickup_A01.png", "Веер Кролика", "White Rabbit's Fan"),
            ("A02", Root + "/Art/World/Gates/gate_pickup_A02.png", "Кусочек гриба", "Piece of Mushroom"),
            ("A03", Root + "/Art/World/Gates/gate_pickup_A03.png", "Чеширский шаг", "Cheshire Step"),
            ("A04", Root + "/Art/World/Gates/gate_pickup_A04.png", "Часы отсрочки", "Watch of Delay"),
            ("T01", Root + "/Art/UI/Trinkets/trinket_T01.png", "Напёрсток", "Thimble"),
            ("T02", Root + "/Art/UI/Trinkets/trinket_T02.png", "Пуговица", "Button"),
            ("T03", Root + "/Art/UI/Trinkets/trinket_T03.png", "Засахаренная конфетка", "Candied Sweet")
        };

        [MenuItem("Alice/Prototype/Set Up Items As Bundles")]
        public static void SetUpFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Items: leave Play mode first.");
                return;
            }

            EnsureTable();
            ItemCatalog catalog = EnsureCatalog();
            Font font = EnsureFont();
            EnsurePopup(catalog, font);
            DressRooms();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureTable()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(TableName, TableFolder);
            foreach (StringTable table in collection.StringTables)
            {
                string code = table.LocaleIdentifier.Code;
                foreach (var item in Items)
                {
                    if (table.GetEntry(item.Id) != null)
                    {
                        continue;
                    }

                    // Russian is the source language; every other locale starts from English.
                    table.AddEntry(item.Id, code == "ru" ? item.Ru : item.En);
                    EditorUtility.SetDirty(table);
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
        }

        private static ItemCatalog EnsureCatalog()
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/UI");
            ItemCatalog catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var entries = (catalog.Entries ?? new ItemCatalog.Entry[0]).Where(entry => entry != null).ToList();
            foreach (var item in Items)
            {
                if (entries.Any(entry => entry.Id == item.Id))
                {
                    continue;
                }

                entries.Add(new ItemCatalog.Entry
                {
                    Id = item.Id,
                    Icon = AssetDatabase.LoadAssetAtPath<Sprite>(item.Icon),
                    Name = new LocalizedString(TableName, item.Id)
                });
            }

            catalog.Entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>The pixel font at its native size, hinted raster: no smoothing between pixels.</summary>
        private static Font EnsureFont()
        {
            if (AssetImporter.GetAtPath(FontPath) is TrueTypeFontImporter importer
                && (importer.fontSize != FontSize || importer.fontRenderingMode != FontRenderingMode.HintedRaster))
            {
                importer.fontSize = FontSize;
                importer.fontRenderingMode = FontRenderingMode.HintedRaster;
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        }

        private static void EnsurePopup(ItemCatalog catalog, Font font)
        {
            HudSkin skin = AssetDatabase.LoadAssetAtPath<HudSkin>(HudSkinPath);
            GameObject root = PrefabUtility.LoadPrefabContents(AlicePrefabPath);
            try
            {
                if (!root.TryGetComponent(out ItemPopup popup))
                {
                    popup = root.AddComponent<ItemPopup>();
                }

                popup.Bind(skin, catalog, font);
                PrefabUtility.SaveAsPrefabAsset(root, AlicePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void DressRooms()
        {
            Sprite[] frames = AssetDatabase.FindAssets(BundlePrefix + " t:Sprite", new[] { BundleFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(BundlePrefix, System.StringComparison.Ordinal))
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
            if (frames.Length == 0)
            {
                Debug.LogWarning($"Items: no bundle frames in {BundleFolder}.");
                return;
            }

            string previous = SceneManager.GetActiveScene().path;
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { RoomsFolder }).Select(AssetDatabase.GUIDToAssetPath))
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (AbilityPickup pickup in root.GetComponentsInChildren<AbilityPickup>(true))
                    {
                        changed |= DressBundle(pickup.gameObject, frames);
                    }

                    foreach (TrinketCache cache in root.GetComponentsInChildren<TrinketCache>(true))
                    {
                        changed |= DressBundle(cache.gameObject, frames);
                    }
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// The item's art child becomes the bundle lying on the floor (the item object stands 0.75 above it, at the
        /// centre of its reach) with the shimmer loop.
        /// </summary>
        private static bool DressBundle(GameObject item, Sprite[] frames)
        {
            if (item.TryGetComponent(out SpriteRenderer placeholder))
            {
                Object.DestroyImmediate(placeholder);
            }

            // Interact reaches every bundle from the same distance.
            if (item.TryGetComponent(out BoxCollider2D reach) && reach.size != BundleReach)
            {
                reach.size = BundleReach;
                EditorUtility.SetDirty(reach);
            }

            SpriteRenderer art = item.transform.Cast<Transform>().Select(child => child.GetComponent<SpriteRenderer>())
                .FirstOrDefault(renderer => renderer != null);
            if (art == null)
            {
                GameObject child = new GameObject("Art");
                child.transform.SetParent(item.transform, false);
                art = child.AddComponent<SpriteRenderer>();
            }

            art.sprite = frames[0];
            art.drawMode = SpriteDrawMode.Simple;
            art.sortingOrder = BundleSortingOrder;
            art.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            if (!art.TryGetComponent(out SpriteLoop loop))
            {
                loop = art.gameObject.AddComponent<SpriteLoop>();
            }

            // The glint passes, then the bundle lies still for a while: a hint, not a blinking lamp.
            loop.Bind(frames.Concat(Enumerable.Repeat(frames[0], StillFrames)).ToArray(), BundleFrameTime);
            EditorUtility.SetDirty(art);
            EditorUtility.SetDirty(loop);
            return true;
        }
    }
}
