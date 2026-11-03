using System;
using System.IO;
using System.Linq;
using Alice.Combat;
using Alice.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// M2 combat on the prototype: Alice's health, thrust and hit reaction on her prefab, and the card
    /// soldier as data, sprite set and prefab. Adds only what is missing and never replaces values someone
    /// tuned. Called by <see cref="AlicePrototypeBuilder"/>, which also places the soldier in the room.
    /// </summary>
    public static class AliceCombatSetup
    {
        private const string Root = "Assets/_Alice";
        private const string AlicePrefabPath = Root + "/Prefabs/Characters/Alice.prefab";
        private const string AliceCombatPath = Root + "/Data/Player/AliceCombat.asset";
        private const string TilePath = Root + "/Art/Prototype/GreyTile.png";

        private const string SoldierFrames = Root + "/Art/Characters/Enemies/CardSoldier";
        private const string SoldierSettingsPath = Root + "/Data/Enemies/CardSoldier.asset";
        private const string SoldierSpritesPath = Root + "/Data/Enemies/CardSoldierSprites.asset";
        private const string SoldierPrefabPath = Root + "/Prefabs/Enemies/CardSoldier.prefab";

        // Frames are copied from Art/Reviews/Alice/Revision04 by soldier-frames.ps1 onto an 80x64 canvas
        // whose feet stand two pixels above the bottom edge, leaving room for the blood pool.
        private const int SoldierGroundOffsetPixels = 2;
        private const int SoldierWindupFrames = 2;
        private const int SoldierActiveFrames = 4;

        private const string SpriteChildName = "Sprite";
        private const string ThrustMarkerName = "ThrustMarker";
        private const int EnemySortingOrder = 9;
        private const int ThrustMarkerSortingOrder = 12;

        /// <summary>Where the prototype room gets its first soldier: open floor after the fan gap, facing the way Alice comes.</summary>
        public static readonly Vector2 PrototypeSoldierPosition = new Vector2(78f, 0f);

        public static void EnsureAssets()
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/Enemies");
            AliceProjectSetup.EnsureFolder(Root + "/Prefabs/Enemies");

            PlayerCombatSettings aliceCombat = EnsureAsset<PlayerCombatSettings>(AliceCombatPath);
            EnsureAliceCombat(aliceCombat);

            MeleeEnemySettings soldier = EnsureAsset<MeleeEnemySettings>(SoldierSettingsPath);
            EnemySpriteSet sprites = EnsureSoldierSprites();
            if (sprites != null)
            {
                EnsureSoldierPrefab(soldier, sprites);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Puts one soldier into the prototype room unless the room already has an enemy.</summary>
        public static void EnsureSoldierInScene(Scene scene, Transform gameplay)
        {
            if (gameplay.GetComponentInChildren<MeleeEnemy>(true) != null)
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierPrefabPath);
            if (prefab == null)
            {
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(gameplay, false);
            instance.transform.position = PrototypeSoldierPosition;
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void EnsureAliceCombat(PlayerCombatSettings settings)
        {
            if (!File.Exists(AlicePrefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(AlicePrefabPath);
            try
            {
                bool changed = false;
                if (!root.TryGetComponent(out Health health))
                {
                    health = root.AddComponent<Health>();
                    changed = true;
                }

                if (!root.TryGetComponent(out PlayerAttack _))
                {
                    root.AddComponent<PlayerAttack>().Bind(settings, CreateThrustMarker(root.transform));
                    changed = true;
                }

                if (!root.TryGetComponent(out PlayerDamageReaction _))
                {
                    root.AddComponent<PlayerDamageReaction>().Settings = settings;
                    changed = true;
                }

                // The grey-box needle only stood in for missing thrust frames.
                Transform marker = root.transform.Find(ThrustMarkerName);
                if (marker != null && root.TryGetComponent(out PlayerSpriteView view) && view.HasThrustFrames)
                {
                    root.GetComponent<PlayerAttack>().Bind(settings, null);
                    UnityEngine.Object.DestroyImmediate(marker.gameObject);
                    changed = true;
                }

                Transform sprite = root.transform.Find(SpriteChildName);
                if (!root.TryGetComponent(out DamageFlash _) && sprite != null)
                {
                    root.AddComponent<DamageFlash>().Bind(health, sprite.GetComponent<SpriteRenderer>(), true);
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, AlicePrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SpriteRenderer CreateThrustMarker(Transform parent)
        {
            // Grey-box needle for the active frames until Alice has thrust art.
            GameObject marker = new GameObject(ThrustMarkerName);
            marker.transform.SetParent(parent, false);
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.color = new Color(0.86f, 0.88f, 0.95f);
            renderer.sortingOrder = ThrustMarkerSortingOrder;
            renderer.enabled = false;
            return renderer;
        }

        private static EnemySpriteSet EnsureSoldierSprites()
        {
            EnemySpriteSet sprites = EnsureAsset<EnemySpriteSet>(SoldierSpritesPath);
            if (sprites.Idle != null)
            {
                return sprites;
            }

            Sprite idle = LoadFrames("card_soldier_idle_").FirstOrDefault();
            Sprite[] walk = LoadFrames("card_soldier_walk_");
            Sprite[] attack = LoadFrames("card_soldier_attack_");
            Sprite[] death = LoadFrames("card_soldier_death_");
            if (idle == null || attack.Length <= SoldierWindupFrames + SoldierActiveFrames || death.Length == 0)
            {
                Debug.LogWarning($"Card soldier frames not found in {SoldierFrames}; run soldier-frames.ps1 from Art/Reviews/Alice/Revision04.");
                return null;
            }

            // Generated thrust: draw back, four frames of the extended spear, return to guard.
            sprites.Assign(idle, walk,
                attack.Take(SoldierWindupFrames).ToArray(),
                attack.Skip(SoldierWindupFrames).Take(SoldierActiveFrames).ToArray(),
                attack.Skip(SoldierWindupFrames + SoldierActiveFrames).ToArray(),
                death, SoldierGroundOffsetPixels);
            EditorUtility.SetDirty(sprites);
            return sprites;
        }

        private static Sprite[] LoadFrames(string prefix)
        {
            return AssetDatabase.FindAssets("t:Sprite " + prefix, new[] { SoldierFrames })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
        }

        private static void EnsureSoldierPrefab(MeleeEnemySettings settings, EnemySpriteSet sprites)
        {
            if (File.Exists(SoldierPrefabPath))
            {
                return;
            }

            GameObject root = new GameObject("CardSoldier");
            try
            {
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = settings.BodySize;
                collider.offset = new Vector2(0f, settings.BodySize.y * 0.5f);
                Health health = root.AddComponent<Health>();

                MeleeEnemy enemy = root.AddComponent<MeleeEnemy>();
                enemy.Settings = settings;
                enemy.InitialFacing = -1;

                GameObject child = new GameObject(SpriteChildName);
                child.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites.Idle;
                renderer.flipX = true;
                renderer.sortingOrder = EnemySortingOrder;

                root.AddComponent<MeleeEnemyView>().Bind(enemy, sprites, renderer);
                root.AddComponent<DamageFlash>().Bind(health, renderer, false);
                PrefabUtility.SaveAsPrefabAsset(root, SoldierPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
