using System.IO;
using System.Linq;
using Alice.Player;
using Alice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// The first pocket trinkets (GDD 0.27): three trinket assets, the trinket component on Alice's prefab and one
    /// cache per trinket in the M3 test rooms. Idempotent: existing assets keep their tuning, a room that already
    /// has the cache of a trinket is left alone.
    /// </summary>
    public static class AliceTrinketSetup
    {
        private const string Root = "Assets/_Alice";
        private const string DataFolder = Root + "/Data/Trinkets";
        private const string IconFolder = Root + "/Art/UI/Trinkets";
        private const string AlicePrefabPath = Root + "/Prefabs/Characters/Alice.prefab";
        private const string RoomsFolder = Root + "/Scenes/M3";
        private const int CacheSortingOrder = 2;

        private sealed class Spec
        {
            public string Id;
            public string Asset;
            public float DamageTaken = 1f;
            public float ParryBonus;
            public int ExecutionHeal;
            public string Room;
            public Vector2 Position;
        }

        // Where each lies: the thimble on the ledge only the fan reaches, the button past the Cheshire bridge,
        // the sweet on the way back.
        private static readonly Spec[] Specs =
        {
            new Spec { Id = "T01", Asset = "T01_Thimble", DamageTaken = 0.8f, Room = "M3_C", Position = new Vector2(16f, 6f) },
            new Spec { Id = "T02", Asset = "T02_Button", ParryBonus = 0.05f, Room = "M3_E", Position = new Vector2(12f, 0f) },
            new Spec { Id = "T03", Asset = "T03_CandiedSweet", ExecutionHeal = 15, Room = "M3_G", Position = new Vector2(0f, 0f) }
        };

        [MenuItem("Alice/Prototype/Set Up Trinkets")]
        public static void SetUpFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Trinkets: leave Play mode first.");
                return;
            }

            TrinketDefinition[] catalog = EnsureAssets();
            EnsureOnAlice(catalog);
            string previous = SceneManager.GetActiveScene().path;
            foreach (Spec spec in Specs)
            {
                string path = $"{RoomsFolder}/{spec.Room}.unity";
                if (!File.Exists(path))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (PlaceCache(scene, spec, catalog.First(trinket => trinket.Id == spec.Id)))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            {
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
        }

        private static TrinketDefinition[] EnsureAssets()
        {
            AliceProjectSetup.EnsureFolder(DataFolder);
            return Specs.Select(spec =>
            {
                string path = $"{DataFolder}/{spec.Asset}.asset";
                TrinketDefinition trinket = AssetDatabase.LoadAssetAtPath<TrinketDefinition>(path);
                Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/trinket_{spec.Id}.png");
                if (trinket == null)
                {
                    trinket = ScriptableObject.CreateInstance<TrinketDefinition>();
                    trinket.Configure(spec.Id, icon, spec.DamageTaken, spec.ParryBonus, spec.ExecutionHeal);
                    AssetDatabase.CreateAsset(trinket, path);
                }
                else if (trinket.Icon == null && icon != null)
                {
                    trinket.Configure(trinket.Id, icon, trinket.DamageTakenMultiplier, trinket.ParryWindowBonus,
                        trinket.ExecutionHeal);
                    EditorUtility.SetDirty(trinket);
                }

                return trinket;
            }).ToArray();
        }

        private static void EnsureOnAlice(TrinketDefinition[] catalog)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(AlicePrefabPath);
            try
            {
                if (!root.TryGetComponent(out PlayerTrinkets trinkets))
                {
                    trinkets = root.AddComponent<PlayerTrinkets>();
                }

                // Trinkets added by hand stay; the first ones are always there.
                trinkets.Catalog = (trinkets.Catalog ?? new TrinketDefinition[0]).Where(trinket => trinket != null)
                    .Union(catalog).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root, AlicePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool PlaceCache(Scene scene, Spec spec, TrinketDefinition trinket)
        {
            Transform gameplay = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentsInChildren<TrinketCache>(true).Any(cache => cache.TrinketId == spec.Id))
                {
                    return false;
                }

                // Caches live with the other gameplay objects of the room: where the builder put the doorways.
                RoomDoorway doorway = root.GetComponentInChildren<RoomDoorway>(true);
                if (gameplay == null && doorway != null)
                {
                    gameplay = doorway.transform.parent;
                }
            }

            GameObject cacheObject = new GameObject("Trinket_" + spec.Id);
            SceneManager.MoveGameObjectToScene(cacheObject, scene);
            if (gameplay != null)
            {
                cacheObject.transform.SetParent(gameplay, false);
            }

            cacheObject.transform.position = spec.Position + new Vector2(0f, 0.75f);
            BoxCollider2D trigger = cacheObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2f, 2f);
            cacheObject.AddComponent<TrinketCache>().TrinketId = spec.Id;

            // A stand-in until AliceItemSetup turns it into a bundle on the floor (GDD 0.28).
            GameObject art = new GameObject("Art");
            art.transform.SetParent(cacheObject.transform, false);
            art.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = trinket.Icon;
            renderer.sortingOrder = CacheSortingOrder;
            return true;
        }
    }
}
