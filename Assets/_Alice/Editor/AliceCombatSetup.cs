using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alice.Combat;
using Alice.Core;
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

        private const string SoldierFrames = Root + "/Art/Characters/Enemies/CardSoldier";
        private const string SoldierSettingsPath = Root + "/Data/Enemies/CardSoldier.asset";
        private const string SoldierSpritesPath = Root + "/Data/Enemies/CardSoldierSprites.asset";
        private const string SoldierPrefabPath = Root + "/Prefabs/Enemies/CardSoldier.prefab";
        private const string SilhouetteShaderPath = Root + "/Shaders/SpriteSilhouette.shader";
        private const string HudSkinPath = Root + "/Data/UI/HudSkin.asset";
        private const string HudIcons = Root + "/Art/UI/Hud";

        // Frames are copied from Art/Reviews/Alice/Revision04 by soldier-frames.ps1 onto an 80x64 canvas
        // whose feet stand two pixels above the bottom edge, leaving room for the blood pool.
        private const int SoldierGroundOffsetPixels = 2;
        // The weighted thrust (4 Nov 2026): lower the spear, draw it back to the chest and hold the cocked
        // pose, then the launch; the blow itself is the extended spear; then the spear comes back upright.
        // Its frames are 144 px wide so the drawn-back shaft and the full thrust both fit.
        private const int SoldierWindupFrames = 7;
        private const int SoldierActiveFrames = 2;
        private const float SoldierStabFrameTime = 0.13f;
        private const float SoldierBeheadFrameTime = 0.09f;
        private const float SoldierExecutionFrameTime = 0.1f;
        private const float SoldierTearDuration = 1.3f;

        private const string SpriteChildName = "Sprite";
        private const string ThrustMarkerName = "ThrustMarker";
        private const int EnemySortingOrder = 9;

        /// <summary>Where the prototype room gets its first soldier: open floor after the fan gap, facing the way Alice comes.</summary>
        public static readonly Vector2 PrototypeSoldierPosition = new Vector2(78f, 0f);

        /// <summary>Adds what is missing to Alice, the enemies and their data without opening any scene.</summary>
        [MenuItem("Alice/Prototype/Attach Combat Components")]
        public static void EnsureAssetsFromMenu()
        {
            AlicePlayerSpritesSetup.Ensure();
            EnsureAssets();
            AssetDatabase.SaveAssets();
        }

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
                EnsureExecutionOutline(SoldierPrefabPath);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Puts one soldier into the prototype room unless the room already has an enemy.</summary>
        public static void EnsureSoldierInScene(Scene scene, Transform gameplay)
        {
            if (gameplay.GetComponentInChildren<MeleeEnemy>(true) == null)
            {
                PlaceSoldier(scene, gameplay, PrototypeSoldierPosition, -1);
            }
        }

        /// <summary>Puts a card soldier from the prefab at a post; <paramref name="facing"/> +1 faces right.</summary>
        public static void PlaceSoldier(Scene scene, Transform gameplay, Vector2 position, int facing)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoldierPrefabPath);
            if (prefab == null)
            {
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(gameplay, false);
            instance.transform.position = position;
            instance.GetComponent<MeleeEnemy>().InitialFacing = facing;
            instance.GetComponentInChildren<SpriteRenderer>().flipX = facing < 0;
        }

        /// <summary>The HUD skin, with icons from Art/UI/Hud picked up by name once they exist.</summary>
        private static Alice.UI.HudSkin EnsureHudSkin()
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/UI");
            Alice.UI.HudSkin skin = EnsureAsset<Alice.UI.HudSkin>(HudSkinPath);
            if (skin.SaltFull == null || skin.SaltEmpty == null || skin.Decree == null)
            {
                // Unity objects: an explicit null check, not ??, which misses destroyed references.
                Sprite saltFull = skin.SaltFull != null ? skin.SaltFull : LoadHudIcon("hud_salt_full");
                Sprite saltEmpty = skin.SaltEmpty != null ? skin.SaltEmpty : LoadHudIcon("hud_salt_empty");
                Sprite decree = skin.Decree != null ? skin.Decree : LoadHudIcon("hud_decree");
                if (saltFull != skin.SaltFull || saltEmpty != skin.SaltEmpty || decree != skin.Decree)
                {
                    skin.AssignIcons(saltFull, saltEmpty, decree);
                    EditorUtility.SetDirty(skin);
                }
            }

            if (skin.TechniqueIconFor(TechniqueIds.SpinStrike) == null)
            {
                Sprite spin = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/UI/Rest/technique_spin_strike.png");
                if (spin != null)
                {
                    skin.AssignTechniqueIcon(TechniqueIds.SpinStrike, spin);
                    EditorUtility.SetDirty(skin);
                }
            }

            return skin;
        }

        private static Sprite LoadHudIcon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(HudIcons + "/" + name + ".png");
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
                    root.AddComponent<PlayerAttack>().Bind(settings);
                    changed = true;
                }

                if (!root.TryGetComponent(out PlayerParry _))
                {
                    root.AddComponent<PlayerParry>().Settings = settings;
                    changed = true;
                }

                if (!root.TryGetComponent(out PlayerDamageReaction _))
                {
                    root.AddComponent<PlayerDamageReaction>().Settings = settings;
                    changed = true;
                }

                if (!root.TryGetComponent(out Alice.UI.ScreenFade _))
                {
                    root.AddComponent<Alice.UI.ScreenFade>();
                    changed = true;
                }

                if (!root.TryGetComponent(out PlayerSalts _))
                {
                    root.AddComponent<PlayerSalts>().Settings = settings;
                    changed = true;
                }

                if (!root.TryGetComponent(out PlayerSpoils _))
                {
                    root.AddComponent<PlayerSpoils>().Settings = settings;
                    changed = true;
                }

                if (!root.TryGetComponent(out PlayerLoadout _))
                {
                    root.AddComponent<PlayerLoadout>();
                    changed = true;
                }

                Alice.UI.HudSkin skin = EnsureHudSkin();
                if (!root.TryGetComponent(out Alice.UI.Hud hud))
                {
                    hud = root.AddComponent<Alice.UI.Hud>();
                    changed = true;
                }

                if (hud.Skin == null)
                {
                    hud.Skin = skin;
                    changed = true;
                }

                // Leftover grey-box needle from before Alice had attack frames.
                Transform marker = root.transform.Find(ThrustMarkerName);
                if (marker != null)
                {
                    UnityEngine.Object.DestroyImmediate(marker.gameObject);
                    changed = true;
                }

                // Rest in the armchair and its technique menu (M3).
                if (!root.TryGetComponent(out PlayerRest rest))
                {
                    rest = root.AddComponent<PlayerRest>();
                    changed = true;
                }

                if (rest.Settings == null)
                {
                    rest.Settings = settings;
                    changed = true;
                }

                // Interact on doors and, later, levers and people (M3).
                if (!root.TryGetComponent(out PlayerInteractor _))
                {
                    root.AddComponent<PlayerInteractor>();
                    changed = true;
                }

                // The mushroom (A02) makes her small at marked passages.
                if (!root.TryGetComponent(out PlayerSize _))
                {
                    root.AddComponent<PlayerSize>();
                    changed = true;
                }

                if (!root.TryGetComponent(out Alice.UI.RestMenu menu))
                {
                    menu = root.AddComponent<Alice.UI.RestMenu>();
                    changed = true;
                }

                if (menu.Skin == null)
                {
                    menu.Skin = skin;
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

        private static EnemySpriteSet EnsureSoldierSprites()
        {
            EnemySpriteSet sprites = EnsureAsset<EnemySpriteSet>(SoldierSpritesPath);
            EnsureSoldierReactions(sprites);
            if (sprites.Idle != null)
            {
                EnsureSoldierAttack(sprites);
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

            // Generated thrust: draw back and hold, the extended spear, return to guard.
            sprites.Assign(idle, walk,
                attack.Take(SoldierWindupFrames).ToArray(),
                attack.Skip(SoldierWindupFrames).Take(SoldierActiveFrames).ToArray(),
                attack.Skip(SoldierWindupFrames + SoldierActiveFrames).ToArray(),
                death, SoldierGroundOffsetPixels);
            EditorUtility.SetDirty(sprites);
            return sprites;
        }

        /// <summary>Re-splits the thrust when the frames on disk changed in number (a redrawn attack).</summary>
        private static void EnsureSoldierAttack(EnemySpriteSet sprites)
        {
            Sprite[] attack = LoadFrames("card_soldier_attack_");
            int assigned = Count(sprites.AttackWindup) + Count(sprites.AttackActive) + Count(sprites.AttackRecovery);
            bool sameSplit = Count(sprites.AttackWindup) == SoldierWindupFrames && Count(sprites.AttackActive) == SoldierActiveFrames;
            if (attack.Length <= SoldierWindupFrames + SoldierActiveFrames || (assigned == attack.Length && sameSplit))
            {
                return;
            }

            sprites.AssignAttack(attack.Take(SoldierWindupFrames).ToArray(),
                attack.Skip(SoldierWindupFrames).Take(SoldierActiveFrames).ToArray(),
                attack.Skip(SoldierWindupFrames + SoldierActiveFrames).ToArray());
            EditorUtility.SetDirty(sprites);
        }

        private static int Count(Sprite[] frames)
        {
            return frames == null ? 0 : frames.Length;
        }

        /// <summary>
        /// Flinch, the parried opening and deaths by killing blow; the slash death is the default set above.
        /// Fills each piece only while it is still empty, so frames added later arrive without touching tuned ones.
        /// </summary>
        private static void EnsureSoldierReactions(EnemySpriteSet sprites)
        {
            Sprite[] hurt = IsEmpty(sprites.Hurt) ? LoadFrames("card_soldier_hurt_") : sprites.Hurt;
            Sprite[] opened = IsEmpty(sprites.Opened) ? LoadFrames("card_soldier_opened_") : sprites.Opened;
            var variants = new List<EnemySpriteSet.DeathVariant>(sprites.DeathVariants ?? new EnemySpriteSet.DeathVariant[0]);

            // The stab death is the slow one: the soldier writhes on its knees before it falls.
            bool grown = AddVariant(variants, HitKind.Thrust, "card_soldier_stab_", SoldierStabFrameTime);
            grown |= AddVariant(variants, HitKind.Spin, "card_soldier_behead_", SoldierBeheadFrameTime);
            grown |= AddVariant(variants, HitKind.Counter, "card_soldier_execution_", SoldierExecutionFrameTime);
            // The execution death spans Alice's whole card_tear execution, so both sides end together.
            grown |= AddVariant(variants, HitKind.Execution, "card_soldier_tear_", 0f, SoldierTearDuration);

            bool changed = grown || hurt != sprites.Hurt || opened != sprites.Opened;
            if (changed)
            {
                sprites.AssignReactions(hurt, opened, variants.ToArray());
                EditorUtility.SetDirty(sprites);
            }
        }

        /// <summary>
        /// Adds a missing death, or takes in frames appended to an existing one (e.g. the blood settling
        /// after a beheading), keeping its tuned frame time. Returns true when the list changed.
        /// </summary>
        private static bool AddVariant(List<EnemySpriteSet.DeathVariant> variants, HitKind kind, string prefix, float frameTime,
            float totalTime = 0f)
        {
            Sprite[] frames = LoadFrames(prefix);
            int index = variants.FindIndex(variant => variant.Kind == kind);
            if (index >= 0)
            {
                EnemySpriteSet.DeathVariant existing = variants[index];
                if (frames.Length <= (existing.Frames?.Length ?? 0))
                {
                    return false;
                }

                variants[index] = new EnemySpriteSet.DeathVariant(kind, frames, existing.FrameTime);
                return true;
            }

            if (frames.Length == 0)
            {
                return false;
            }

            if (totalTime > 0f)
            {
                frameTime = totalTime / frames.Length;
            }

            variants.Add(new EnemySpriteSet.DeathVariant(kind, frames, frameTime));
            return true;
        }

        private static bool IsEmpty(Sprite[] frames)
        {
            return frames == null || frames.Length == 0;
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

        /// <summary>Adds the outline that marks an enemy open to execution, on prefabs made before it existed.</summary>
        private static void EnsureExecutionOutline(string prefabPath)
        {
            Shader silhouette = AssetDatabase.LoadAssetAtPath<Shader>(SilhouetteShaderPath);
            if (silhouette == null || !File.Exists(prefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.TryGetComponent(out ExecutionOutline _) || !root.TryGetComponent(out MeleeEnemy enemy))
                {
                    return;
                }

                Transform sprite = root.transform.Find(SpriteChildName);
                root.AddComponent<ExecutionOutline>().Bind(enemy, sprite.GetComponent<SpriteRenderer>(), silhouette);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
