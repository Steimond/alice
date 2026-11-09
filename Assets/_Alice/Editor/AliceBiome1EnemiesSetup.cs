using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Alice.Combat;
using Alice.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alice.Editor
{
    /// <summary>
    /// The enemies of biome 1 beside the card soldier (GDD section 9): the clothes moth EN11 (flying diver), the race
    /// rat EN02 (charger), the duck washerwoman EN12 (thrower) and the laundry sheet EN13 (ambusher). Builds their
    /// settings, clip sets from the frames in Art/Characters/Enemies/&lt;Type&gt;, prefabs and palette entries, and
    /// gives Alice the hold the sheet needs. Repeatable: assets that exist keep their tuning; clip sets only take in
    /// clips they lack or whose frame count changed on disk; prefabs are made once.
    /// </summary>
    public static class AliceBiome1EnemiesSetup
    {
        private const string Root = "Assets/_Alice";
        private const string ArtRoot = Root + "/Art/Characters/Enemies";
        private const string DataRoot = Root + "/Data/Enemies";
        private const string PrefabRoot = Root + "/Prefabs/Enemies";
        private const string PaletteRoot = Root + "/Prefabs/Level/Enemies";
        private const string AlicePrefabPath = Root + "/Prefabs/Characters/Alice.prefab";
        private const string ClothespinPath = PrefabRoot + "/Clothespin.prefab";
        private const string TestScenePath = Root + "/Scenes/Prototype_Enemies.unity";
        private const string ArenaScenePath = Root + "/Scenes/Prototype_Combat.unity";
        private const string SpriteChildName = "Sprite";
        private const int EnemySortingOrder = 9;
        // The sheet hangs in front of Alice: she walks behind the cloth before it drops on her.
        private const int SheetSortingOrder = 12;
        // The clothespin's sprite is 24 px square on a bottom-centre pivot; lowered by half, it spins about its middle.
        private const float ClothespinCentre = 12f / 16f;

        private sealed class ClipSpec
        {
            public string Id;
            public float FrameTime;
            public bool Loop;

            public ClipSpec(string id, float frameTime, bool loop = false)
            {
                Id = id;
                FrameTime = frameTime;
                Loop = loop;
            }
        }

        private sealed class TypeSpec
        {
            public string Name;
            public string Prefix;
            public int SortingOrder = EnemySortingOrder;
            public ClipSpec[] Clips;
            public Func<EnemySettings> CreateSettings;
            public Func<GameObject, EnemySettings, EnemyBase> AddBehaviour;
            public ExecutionSpec Execution;
            /// <summary>Alice's frames of the execution split into wind-up, killing blow and finish.</summary>
            public int[] ExecutionSplit;
        }

        private static readonly TypeSpec[] Types =
        {
            new TypeSpec
            {
                Name = "Moth", Prefix = "moth",
                Clips = new[]
                {
                    new ClipSpec("hover", 0.08f, true), new ClipSpec("windup", 0.1f), new ClipSpec("dive", 0.1f),
                    new ClipSpec("fall", 0.07f), new ClipSpec("grounded", 0.1f), new ClipSpec("rise", 0.1f),
                    new ClipSpec("deathair", 0.07f), new ClipSpec("death", 0.12f), new ClipSpec("deathfloor", 0.08f),
                    new ClipSpec("stunned", 0.1f, true), new ClipSpec("death_execution", 0.1f)
                },
                // Pinned to the floor like a specimen: raise, kneel, one blow down through the body with the fist at knee
                // height (owner: the arm should not reach so deep), a long press, rise.
                Execution = new ExecutionSpec("moth_pin", 0.4f, 0.1f, 1f, 2.2f),
                ExecutionSplit = new[] { 3, 2, 11 },
                CreateSettings = () => Create<FlyingDiverSettings>("EN11", new Vector2(2.2f, 2f), 3, 0, 10f, 3f, 8f, 12, 4f),
                AddBehaviour = (root, settings) =>
                {
                    FlyingDiver enemy = root.AddComponent<FlyingDiver>();
                    enemy.Settings = (FlyingDiverSettings)settings;
                    return enemy;
                }
            },
            new TypeSpec
            {
                Name = "Racer", Prefix = "racer",
                Clips = new[]
                {
                    new ClipSpec("idle", 0.12f, true), new ClipSpec("walk", 0.1f, true), new ClipSpec("run", 0.055f, true),
                    new ClipSpec("windup", 0.1f),
                    new ClipSpec("skid", 0.1f), new ClipSpec("daze", 0.1f), new ClipSpec("hurt", 0.1f),
                    new ClipSpec("death", 0.09f), new ClipSpec("stunned", 0.1f, true), new ClipSpec("death_execution", 0.1f)
                },
                // Kneeling at its head (owner: more blood, more to it): the needle into the nape twice, then twisted and
                // ripped out back over her shoulder; the rat bursts at each blow and fountains at the rip.
                Execution = new ExecutionSpec("rat_nape", 0.35f, 0.1f, 1.75f, 3f),
                ExecutionSplit = new[] { 2, 1, 12 },
                CreateSettings = () => Create<ChargerSettings>("EN02", new Vector2(3.2f, 2.2f), 5, 10, 10f, 2f, 9f, 20, 6f),
                AddBehaviour = (root, settings) =>
                {
                    Charger enemy = root.AddComponent<Charger>();
                    enemy.Settings = (ChargerSettings)settings;
                    return enemy;
                }
            },
            new TypeSpec
            {
                Name = "Duck", Prefix = "duck",
                Clips = new[]
                {
                    // The walk advances by distance; Thrower counts 0.1 s per walk frame.
                    new ClipSpec("idle", 0.12f, true), new ClipSpec("walk", 0.1f, true), new ClipSpec("throw", 0.1f),
                    new ClipSpec("hurt", 0.1f), new ClipSpec("death", 0.09f), new ClipSpec("stunned", 0.1f, true),
                    new ClipSpec("death_execution", 0.1f)
                },
                // The beak torn off (owner, 9 November 2026): the needle driven into its root at face height, then wrenched
                // back over her shoulder; the beak flies, blood gushes from the face. The ID stays from the first version.
                Execution = new ExecutionSpec("duck_throat", 0.45f, 0.15f, 1.1f, 2.6f),
                ExecutionSplit = new[] { 3, 2, 11 },
                CreateSettings = () => Create<ThrowerSettings>("EN12", new Vector2(1.4f, 3.4f), 3, 10, 11f, 3f, 6f, 15, 5f),
                AddBehaviour = (root, settings) =>
                {
                    Thrower enemy = root.AddComponent<Thrower>();
                    enemy.Settings = (ThrowerSettings)settings;
                    return enemy;
                }
            },
            new TypeSpec
            {
                Name = "Sheet", Prefix = "sheet", SortingOrder = SheetSortingOrder,
                Clips = new[]
                {
                    new ClipSpec("hang", 0.14f, true), new ClipSpec("tremble", 0.1f), new ClipSpec("drop", 0.1f),
                    new ClipSpec("wrap", 0.1f, true),
                    new ClipSpec("death", 0.1f), new ClipSpec("cut", 0.09f), new ClipSpec("torn", 0.07f)
                },
                // Cloth, not a fighter: one blow ends it, so it is never stunned or executed (owner, 9 November 2026).
                CreateSettings = () => Create<AmbusherSettings>("EN13", new Vector2(2.2f, 4.6f), 1, 0, 6f, 5f, 0f, 12, 4f),
                AddBehaviour = (root, settings) =>
                {
                    Ambusher enemy = root.AddComponent<Ambusher>();
                    enemy.Settings = (AmbusherSettings)settings;
                    return enemy;
                }
            }
        };

        [MenuItem("Alice/Prototype/Set Up Biome 1 Enemies")]
        public static void SetUpFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Biome 1 enemies: leave Play mode first.");
                return;
            }

            SetUp();
        }

        public static void SetUp()
        {
            AliceProjectSetup.EnsureFolder(DataRoot);
            AliceProjectSetup.EnsureFolder(PrefabRoot);
            AliceProjectSetup.EnsureFolder(PaletteRoot);
            EnsureAliceRestraint();
            EnsureTearFree();

            foreach (TypeSpec type in Types)
            {
                EnemySettings settings = EnsureSettings(type);
                EnemyClipSet clips = EnsureClips(type);
                string prefabPath = $"{PrefabRoot}/{type.Name}.prefab";
                EnsurePrefab(type, settings, clips, prefabPath);
                EnsureExecution(type, settings);
                EnsureExecutionOutline(prefabPath);
                EnsurePaletteVariant(prefabPath, type.Name);
            }

            EnsureClothespin();
            EnsureLaundryProps();
            AssetDatabase.SaveAssets();
            Debug.Log("Biome 1 enemies set up: Moth, Racer, Duck, Sheet.");
        }

        private static T Create<T>(string archetypeId, Vector2 body, int health, int contactDamage, float sightRange,
            float sightHeight, float leash, int decrees, float confidence) where T : EnemySettings
        {
            T settings = ScriptableObject.CreateInstance<T>();
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_archetypeId").stringValue = archetypeId;
            serialized.FindProperty("_bodySize").vector2Value = body;
            serialized.FindProperty("_maxHealth").intValue = health;
            serialized.FindProperty("_contactDamage").intValue = contactDamage;
            serialized.FindProperty("_sightRange").floatValue = sightRange;
            serialized.FindProperty("_sightHeight").floatValue = sightHeight;
            serialized.FindProperty("_leashDistance").floatValue = leash;
            serialized.FindProperty("_decreeReward").intValue = decrees;
            serialized.FindProperty("_confidenceReward").floatValue = confidence;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static EnemySettings EnsureSettings(TypeSpec type)
        {
            string path = $"{DataRoot}/{type.Name}.asset";
            EnemySettings settings = AssetDatabase.LoadAssetAtPath<EnemySettings>(path);
            if (settings != null)
            {
                return settings;
            }

            settings = type.CreateSettings();
            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }

        private static EnemyClipSet EnsureClips(TypeSpec type)
        {
            string path = $"{DataRoot}/{type.Name}Clips.asset";
            EnemyClipSet set = AssetDatabase.LoadAssetAtPath<EnemyClipSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<EnemyClipSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            var clips = new List<EnemyClipSet.Clip>(set.Clips ?? new EnemyClipSet.Clip[0]);
            bool changed = false;
            foreach (ClipSpec spec in type.Clips)
            {
                Sprite[] frames = LoadFrames($"{ArtRoot}/{type.Name}", $"{type.Prefix}_{spec.Id}_");
                if (frames.Length == 0)
                {
                    Debug.LogWarning($"{type.Name}: no frames for clip '{spec.Id}'.");
                    continue;
                }

                int index = clips.FindIndex(clip => clip.Id == spec.Id);
                if (index < 0)
                {
                    clips.Add(new EnemyClipSet.Clip(spec.Id, frames, spec.FrameTime, spec.Loop));
                    changed = true;
                }
                else if (!frames.SequenceEqual(clips[index].Frames ?? new Sprite[0]))
                {
                    // Redrawn frames come in; the tuned timing stays unless the clip got a different length.
                    EnemyClipSet.Clip old = clips[index];
                    float frameTime = old.Frames != null && old.Frames.Length == frames.Length ? old.FrameTime : spec.FrameTime;
                    clips[index] = new EnemyClipSet.Clip(spec.Id, frames, frameTime, old.Loop);
                    changed = true;
                }
            }

            if (changed)
            {
                set.Assign(clips.ToArray());
                EditorUtility.SetDirty(set);
            }

            return set;
        }

        private static Sprite[] LoadFrames(string folder, string prefix)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return new Sprite[0];
            }

            return AssetDatabase.FindAssets("t:Sprite " + prefix, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal)
                    && char.IsDigit(Path.GetFileNameWithoutExtension(path)[prefix.Length]))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
        }

        private static void EnsurePrefab(TypeSpec type, EnemySettings settings, EnemyClipSet clips, string path)
        {
            if (File.Exists(path))
            {
                return;
            }

            GameObject root = new GameObject(type.Name);
            try
            {
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = settings.BodySize;
                collider.offset = new Vector2(0f, settings.BodySize.y * 0.5f);
                Health health = root.AddComponent<Health>();
                EnemyBase enemy = type.AddBehaviour(root, settings);
                enemy.InitialFacing = -1;

                GameObject child = new GameObject(SpriteChildName);
                child.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = type.SortingOrder;
                if (clips.Clips.Length > 0)
                {
                    renderer.sprite = clips.Clips[0].Frames[0];
                }

                renderer.flipX = true;
                root.AddComponent<EnemyClipView>().Bind(enemy, clips, renderer);
                root.AddComponent<DamageFlash>().Bind(health, renderer, false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private const string AliceCombatPath = Root + "/Data/Player/AliceCombat.asset";
        private const string AliceSpritesPath = Root + "/Data/Player/AliceSprites.asset";
        private const string AliceFrames = Root + "/Art/Characters/Alice/Trial";
        private const string SilhouetteShaderPath = Root + "/Shaders/SpriteSilhouette.shader";

        /// <summary>
        /// The type's execution on both sides: its ID and length in the enemy's data (only while unset, so tuning
        /// stays), Alice's timing in AliceCombat (only when missing) and her frames alice_trial_exec_&lt;prefix&gt;_NN.
        /// </summary>
        private static void EnsureExecution(TypeSpec type, EnemySettings settings)
        {
            ExecutionSpec spec = type.Execution;
            if (string.IsNullOrEmpty(spec.Id))
            {
                return;
            }

            if (string.IsNullOrEmpty(settings.ExecutionId))
            {
                MeleeAttackTimeline timeline = spec.Timeline;
                var serialized = new SerializedObject(settings);
                serialized.FindProperty("_executionId").stringValue = spec.Id;
                serialized.FindProperty("_executionTime").floatValue = timeline.Duration;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var combat = AssetDatabase.LoadAssetAtPath<PlayerCombatSettings>(AliceCombatPath);
            if (combat != null && combat.AddExecutionIfMissing(spec))
            {
                EditorUtility.SetDirty(combat);
            }

            var sprites = AssetDatabase.LoadAssetAtPath<PlayerSpriteSet>(AliceSpritesPath);
            Sprite[] frames = LoadFrames(AliceFrames, $"alice_trial_exec_{type.Prefix}_");
            if (sprites == null || frames.Length != type.ExecutionSplit.Sum())
            {
                return;
            }

            StrikeFrames held = sprites.ExecutionPhases(spec.Id);
            Sprite[] current = (held.Phase(AttackPhase.Windup) ?? new Sprite[0])
                .Concat(held.Phase(AttackPhase.Active) ?? new Sprite[0])
                .Concat(held.Phase(AttackPhase.Recovery) ?? new Sprite[0]).ToArray();
            if (current.SequenceEqual(frames))
            {
                return;
            }

            int[] split = type.ExecutionSplit;
            sprites.AssignExecution(spec.Id, new StrikeFrames(frames.Take(split[0]).ToArray(),
                frames.Skip(split[0]).Take(split[1]).ToArray(), frames.Skip(split[0] + split[1]).ToArray()));
            EditorUtility.SetDirty(sprites);
        }

        /// <summary>The pulsing outline that offers the execution, on prefabs made before it existed.</summary>
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
                if (root.TryGetComponent(out ExecutionOutline _) || !root.TryGetComponent(out EnemyBase enemy))
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

        private const string LaundryArt = Root + "/Art/World/Laundry";
        private const string PropsRoot = Root + "/Prefabs/Level/Props";
        // Behind the sheets, whose pegs sit on it, and in front of Alice, who passes under it.
        private const int RopeSortingOrder = 11;

        /// <summary>
        /// Dressing for laundry rooms in the palette: a clothesline of any length between two hooks, and a harmless
        /// hanging sheet (the sheet enemy's sway) for the ambusher to hide among.
        /// </summary>
        private static void EnsureLaundryProps()
        {
            AliceProjectSetup.EnsureFolder(PropsRoot);
            string linePath = PropsRoot + "/Clothesline.prefab";
            Sprite rope = AssetDatabase.LoadAssetAtPath<Sprite>(LaundryArt + "/clothesline_rope.png");
            Sprite hook = AssetDatabase.LoadAssetAtPath<Sprite>(LaundryArt + "/clothesline_hook.png");
            if (!File.Exists(linePath) && rope != null && hook != null)
            {
                GameObject root = new GameObject("Clothesline");
                try
                {
                    SpriteRenderer Part(string name, Sprite sprite)
                    {
                        var child = new GameObject(name);
                        child.transform.SetParent(root.transform, false);
                        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                        renderer.sprite = sprite;
                        renderer.sortingOrder = RopeSortingOrder;
                        return renderer;
                    }

                    SpriteRenderer line = Part("Rope", rope);
                    SpriteRenderer left = Part("HookLeft", hook);
                    SpriteRenderer right = Part("HookRight", hook);
                    root.AddComponent<Alice.World.Clothesline>().Bind(line, left, right);
                    PrefabUtility.SaveAsPrefabAsset(root, linePath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            string sheetPath = PropsRoot + "/HangingSheet.prefab";
            Sprite[] sway = LoadFrames($"{ArtRoot}/Sheet", "sheet_hang_");
            if (!File.Exists(sheetPath) && sway.Length > 0)
            {
                GameObject root = new GameObject("HangingSheet");
                try
                {
                    SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                    renderer.sprite = sway[0];
                    renderer.sortingOrder = SheetSortingOrder;
                    root.AddComponent<Alice.World.SpriteLoop>().Bind(sway, 0.14f);
                    PrefabUtility.SaveAsPrefabAsset(root, sheetPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static void EnsurePaletteVariant(string prefabPath, string name)
        {
            string path = $"{PaletteRoot}/{name}.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (source == null || File.Exists(path))
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.position = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        /// <summary>The thrown clothespin, and the duck's settings pointing at it.</summary>
        private static void EnsureClothespin()
        {
            if (!File.Exists(ClothespinPath))
            {
                Sprite[] spin = LoadFrames($"{ArtRoot}/Duck", "duck_pin_");
                GameObject root = new GameObject("Clothespin");
                try
                {
                    GameObject child = new GameObject(SpriteChildName);
                    child.transform.SetParent(root.transform, false);
                    child.transform.localPosition = new Vector3(0f, -ClothespinCentre, 0f);
                    SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = EnemySortingOrder + 1;
                    renderer.sprite = spin.FirstOrDefault();
                    root.AddComponent<ArcProjectile>().Bind(renderer, spin);
                    PrefabUtility.SaveAsPrefabAsset(root, ClothespinPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            var duck = AssetDatabase.LoadAssetAtPath<ThrowerSettings>($"{DataRoot}/Duck.asset");
            var pin = AssetDatabase.LoadAssetAtPath<GameObject>(ClothespinPath);
            if (duck != null && duck.Projectile == null && pin != null)
            {
                duck.Projectile = pin.GetComponent<ArcProjectile>();
                EditorUtility.SetDirty(duck);
            }
        }

        /// <summary>Alice ripping the sheet apart as she struggles free (alice_trial_tearfree_NN), once drawn.</summary>
        private static void EnsureTearFree()
        {
            var sprites = AssetDatabase.LoadAssetAtPath<PlayerSpriteSet>(AliceSpritesPath);
            Sprite[] frames = LoadFrames(AliceFrames, "alice_trial_tearfree_");
            if (sprites == null || frames.Length == 0 || (sprites.TearFree ?? new Sprite[0]).SequenceEqual(frames))
            {
                return;
            }

            sprites.AssignTearFree(frames);
            EditorUtility.SetDirty(sprites);
        }

        private static void EnsureAliceRestraint()
        {
            if (!File.Exists(AlicePrefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(AlicePrefabPath);
            try
            {
                if (!root.TryGetComponent(out PlayerRestraint _))
                {
                    root.AddComponent<PlayerRestraint>();
                    PrefabUtility.SaveAsPrefabAsset(root, AlicePrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// A test hall for the four new enemies, one bay each, copied from the combat arena: moth, rat, duck and
        /// sheet left to right with room to meet each on its own (GDD section 9: new enemies first appear alone).
        /// </summary>
        [MenuItem("Alice/Prototype/Build Enemies Test Hall")]
        public static void BuildTestHall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Enemies test hall: leave Play mode first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SetUp();
            if (!File.Exists(TestScenePath) && !AssetDatabase.CopyAsset(ArenaScenePath, TestScenePath))
            {
                Debug.LogError("Could not copy the combat arena to " + TestScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            GameObject gameplay = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Gameplay");
            if (gameplay == null)
            {
                Debug.LogError("Enemies test hall: no Gameplay root in the copied arena.");
                return;
            }

            // The arena's two soldiers stay: the moth's bay is at the left, the sheet's at the right.
            if (gameplay.GetComponentInChildren<EnemyBase>(true) == null)
            {
                Place(scene, gameplay.transform, "Moth", new Vector2(-16f, 8f), 1);
                Place(scene, gameplay.transform, "Racer", new Vector2(-13f, 0f), 1);
                Place(scene, gameplay.transform, "Duck", new Vector2(12f, 0f), -1);
                Place(scene, gameplay.transform, "Sheet", new Vector2(17f, 0f), -1);
                foreach (MeleeEnemy soldier in gameplay.GetComponentsInChildren<MeleeEnemy>(true))
                {
                    UnityEngine.Object.DestroyImmediate(soldier.gameObject);
                }
            }

            // The sheet hides among laundry: a line over its bay, one harmless sheet beside it.
            if (gameplay.GetComponentInChildren<Alice.World.Clothesline>(true) == null)
            {
                var line = AssetDatabase.LoadAssetAtPath<GameObject>(PropsRoot + "/Clothesline.prefab");
                var decoy = AssetDatabase.LoadAssetAtPath<GameObject>(PropsRoot + "/HangingSheet.prefab");
                if (line != null && decoy != null)
                {
                    var lineInstance = (GameObject)PrefabUtility.InstantiatePrefab(line, scene);
                    lineInstance.transform.SetParent(gameplay.transform, false);
                    lineInstance.transform.position = new Vector2(15f, SheetBelowRope);
                    lineInstance.GetComponent<Alice.World.Clothesline>().Length = 8;
                    var decoyInstance = (GameObject)PrefabUtility.InstantiatePrefab(decoy, scene);
                    decoyInstance.transform.SetParent(gameplay.transform, false);
                    decoyInstance.transform.position = new Vector2(13.5f, 0f);
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        // A sheet's pegs sit on the rope when the rope's pivot is this far above the sheet's (67 px; the sheet frames
        // sit 1 px lower so that what falls from it lies on the floor).
        private const float SheetBelowRope = 67f / 16f;

        private static void Place(Scene scene, Transform parent, string name, Vector2 position, int facing)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{name}.prefab");
            if (prefab == null)
            {
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.GetComponent<EnemyBase>().InitialFacing = facing;
            instance.GetComponentInChildren<SpriteRenderer>().flipX = facing < 0;
        }
    }
}
