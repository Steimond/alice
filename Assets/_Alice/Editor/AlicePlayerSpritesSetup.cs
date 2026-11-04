using System;
using System.IO;
using System.Linq;
using Alice.Combat;
using Alice.Player;
using UnityEditor;
using UnityEngine;

namespace Alice.Editor
{
    /// <summary>
    /// Puts Alice's trial sprites on the prefab: a sprite set asset and a <see cref="PlayerSpriteView"/>
    /// over the grey-box body. Fills only empty slots and never replaces sprites someone assigned.
    /// </summary>
    public static class AlicePlayerSpritesSetup
    {
        private const string Root = "Assets/_Alice";
        private const string FramesFolder = Root + "/Art/Characters/Alice/Trial";
        private const string IdlePath = FramesFolder + "/alice_trial_idle_00.png";
        private const string RunPrefix = "alice_trial_run_";
        private const string JumpPrefix = "alice_trial_jump_";
        private const string FallPrefix = "alice_trial_fall_";
        private const string LandPrefix = "alice_trial_land_";
        private const string StepOffPrefix = "alice_trial_stepoff_";
        private const string ThrustPrefix = "alice_trial_thrust_";
        private const string SlashPrefix = "alice_trial_slash_";
        private const string AirSlashPrefix = "alice_trial_airslash_";
        private const string CounterPrefix = "alice_trial_counter_";
        private const string ParryPrefix = "alice_trial_parry_";

        // The combat moves are 12-frame animations with anticipation and follow-through (owner, 4 Nov 2026).
        // Frames per phase: wind-up, the blow, recovery.
        private static readonly int[] ThrustSplit = { 3, 3, 6 };
        private static readonly int[] SlashSplit = { 4, 3, 5 };
        private static readonly int[] AirSlashSplit = { 5, 3, 4 };
        // The counter's long recovery holds the buried needle before wrenching it out.
        private static readonly int[] CounterSplit = { 1, 3, 8 };
        // Matches the soldier's tear: the stab lands as its card is pierced, the rip as it splits.
        private static readonly int[] CardExecutionSplit = { 3, 3, 8 };
        // Guard, deflect, hold, lower.
        private static readonly int[] ParrySplit = { 4, 3, 2, 3 };
        // Alice's side of the card soldier's execution; the ID matches MeleeEnemySettings and AliceCombat.
        private const string CardExecutionPrefix = "alice_trial_exec_card_";
        private const string CardExecutionId = "card_tear";
        private const string HurtPrefix = "alice_trial_hurt_";
        private const string DefeatPrefix = "alice_trial_defeat_";
        private const string WakePrefix = "alice_trial_wake_";
        private const string SaltsPrefix = "alice_trial_salts_";
        private const string SlidePrefix = "alice_trial_slide_";
        private const string SpinPrefix = "alice_trial_spin_";
        // Jump frames from the generation: takeoff, three rise frames, apex tuck, apex opening, fall start.
        private const int JumpFrameCount = 7;
        private const string SpriteSetPath = Root + "/Data/Player/AliceSprites.asset";
        private const string PrefabPath = Root + "/Prefabs/Characters/Alice.prefab";
        private const string SpriteChildName = "Sprite";
        private const int SpriteSortingOrder = 10;

        [MenuItem("Alice/Prototype/Attach Alice Sprites")]
        public static void EnsureFromMenu()
        {
            Ensure();
        }

        // Batch entry point: -executeMethod Alice.Editor.AlicePlayerSpritesSetup.EnsureBatch
        public static void EnsureBatch()
        {
            Ensure();
            Debug.Log("ALICE_SPRITES_COMPLETE");
        }

        public static void Ensure()
        {
            AliceProjectSetup.EnsureFolder(Root + "/Data/Player");
            PlayerSpriteSet sprites = EnsureSpriteSet();
            if (sprites == null || !File.Exists(PrefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (AttachView(root, sprites))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        private static PlayerSpriteSet EnsureSpriteSet()
        {
            PlayerSpriteSet sprites = AssetDatabase.LoadAssetAtPath<PlayerSpriteSet>(SpriteSetPath);
            if (sprites == null)
            {
                sprites = ScriptableObject.CreateInstance<PlayerSpriteSet>();
                AssetDatabase.CreateAsset(sprites, SpriteSetPath);
            }

            if (sprites.Idle == null && (sprites.Run == null || sprites.Run.Length == 0))
            {
                Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(IdlePath);
                Sprite[] run = LoadFrames(RunPrefix);
                if (idle == null || run.Length == 0)
                {
                    Debug.LogWarning($"Alice trial sprites not found in {FramesFolder}; the sprite set stays empty.");
                    return null;
                }

                sprites.Assign(idle, run);
                EditorUtility.SetDirty(sprites);
            }

            if ((sprites.JumpRise == null || sprites.JumpRise.Length == 0)
                && (sprites.JumpApex == null || sprites.JumpApex.Length == 0))
            {
                Sprite[] jump = LoadFrames(JumpPrefix);
                Sprite[] fall = LoadFrames(FallPrefix);
                if (jump.Length == JumpFrameCount && fall.Length > 0)
                {
                    sprites.AssignJump(jump.Take(4).ToArray(), new[] { jump[4], jump[5] }, jump[6], fall,
                        LoadFrames(LandPrefix), LoadFrames(StepOffPrefix));
                    EditorUtility.SetDirty(sprites);
                }
            }

            // Reassigned whenever the frames on disk no longer match what the set holds.
            Sprite[] thrust = LoadFrames(ThrustPrefix);
            if (NeedsStrike(thrust, ThrustSplit, sprites.StrikePhase(PlayerStrike.Thrust, AttackPhase.Windup),
                    sprites.StrikePhase(PlayerStrike.Thrust, AttackPhase.Active),
                    sprites.StrikePhase(PlayerStrike.Thrust, AttackPhase.Recovery)))
            {
                StrikeFrames split = Split(thrust, ThrustSplit);
                sprites.AssignThrust(split.Phase(AttackPhase.Windup), split.Phase(AttackPhase.Active),
                    split.Phase(AttackPhase.Recovery));
                EditorUtility.SetDirty(sprites);
            }

            Sprite[] slash = LoadFrames(SlashPrefix);
            Sprite[] airSlash = LoadFrames(AirSlashPrefix);
            if (NeedsStrike(slash, SlashSplit, sprites.Slash) || NeedsStrike(airSlash, AirSlashSplit, sprites.AirSlash))
            {
                sprites.AssignSlashes(Split(slash, SlashSplit), Split(airSlash, AirSlashSplit));
                EditorUtility.SetDirty(sprites);
            }

            Sprite[] counter = LoadFrames(CounterPrefix);
            if (NeedsStrike(counter, CounterSplit, sprites.Counter))
            {
                sprites.AssignCounter(Split(counter, CounterSplit));
                EditorUtility.SetDirty(sprites);
            }

            Sprite[] parry = LoadFrames(ParryPrefix);
            if (parry.Length == ParrySplit.Sum() && (!sprites.HasParry || sprites.Parry.Length != parry.Length))
            {
                sprites.AssignParry(parry, ParrySplit[0], ParrySplit[1], ParrySplit[2], ParrySplit[3]);
                EditorUtility.SetDirty(sprites);
            }

            // Reach and grab | the lunge into the heart | the pull, the rip, stepping back, standing.
            Sprite[] execution = LoadFrames(CardExecutionPrefix);
            if (NeedsStrike(execution, CardExecutionSplit, sprites.ExecutionPhases(CardExecutionId)))
            {
                sprites.AssignExecution(CardExecutionId, Split(execution, CardExecutionSplit));
                EditorUtility.SetDirty(sprites);
            }

            if (sprites.Hurt == null || sprites.Hurt.Length == 0)
            {
                Sprite[] hurt = LoadFrames(HurtPrefix);
                Sprite[] defeat = LoadFrames(DefeatPrefix);
                Sprite[] wake = LoadFrames(WakePrefix);
                if (hurt.Length > 0 && defeat.Length > 0 && wake.Length > 0)
                {
                    sprites.AssignReactions(hurt, defeat, wake);
                    EditorUtility.SetDirty(sprites);
                }
            }

            if (!sprites.HasSlide)
            {
                // Drop, two frames on the back, getting up.
                Sprite[] slide = LoadFrames(SlidePrefix);
                if (slide.Length == 4)
                {
                    sprites.AssignSlide(slide[0], new[] { slide[1], slide[2] }, slide[3]);
                    EditorUtility.SetDirty(sprites);
                }
            }

            // A real turn drawn frame by frame, 16 frames: sinking and coiling | the whip round through the back,
            // the far side and the front into the lunge | overbalancing and straightening up.
            // Reassigned whenever the number of frames on disk changes.
            Sprite[] spin = LoadFrames(SpinPrefix);
            StrikeFrames spinPhases = sprites.TechniquePhases(TechniqueIds.SpinStrike);
            int assignedSpin = (spinPhases.Phase(AttackPhase.Windup)?.Length ?? 0)
                + (spinPhases.Phase(AttackPhase.Active)?.Length ?? 0) + (spinPhases.Phase(AttackPhase.Recovery)?.Length ?? 0);
            if (spin.Length == 16 && assignedSpin != spin.Length)
            {
                sprites.AssignTechnique(TechniqueIds.SpinStrike, new StrikeFrames(spin.Take(4).ToArray(),
                    spin.Skip(4).Take(8).ToArray(), spin.Skip(12).ToArray()));
                EditorUtility.SetDirty(sprites);
            }

            if (sprites.Salts == null || sprites.Salts.Length == 0)
            {
                Sprite[] salts = LoadFrames(SaltsPrefix);
                if (salts.Length > 0)
                {
                    sprites.AssignSalts(salts);
                    EditorUtility.SetDirty(sprites);
                }
            }

            return sprites;
        }

        /// <summary>Wind-up, blow and recovery frames, in order, by the given counts.</summary>
        private static StrikeFrames Split(Sprite[] frames, int[] split)
        {
            return new StrikeFrames(frames.Take(split[0]).ToArray(), frames.Skip(split[0]).Take(split[1]).ToArray(),
                frames.Skip(split[0] + split[1]).Take(split[2]).ToArray());
        }

        private static bool NeedsStrike(Sprite[] onDisk, int[] split, StrikeFrames current)
        {
            return NeedsStrike(onDisk, split, current.Phase(AttackPhase.Windup), current.Phase(AttackPhase.Active),
                current.Phase(AttackPhase.Recovery));
        }

        /// <summary>True when the frames on disk are complete and differ from what the set holds.</summary>
        private static bool NeedsStrike(Sprite[] onDisk, int[] split, Sprite[] windup, Sprite[] active, Sprite[] recovery)
        {
            if (onDisk.Length != split.Sum())
            {
                return false;
            }

            Sprite[] held = (windup ?? new Sprite[0]).Concat(active ?? new Sprite[0]).Concat(recovery ?? new Sprite[0]).ToArray();
            return !held.SequenceEqual(onDisk) || (windup?.Length ?? 0) != split[0] || (active?.Length ?? 0) != split[1];
        }

        private static Sprite[] LoadFrames(string prefix)
        {
            return AssetDatabase.FindAssets("t:Sprite " + prefix, new[] { FramesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
        }

        /// <summary>Returns true when the prefab changed.</summary>
        private static bool AttachView(GameObject root, PlayerSpriteSet sprites)
        {
            if (root.GetComponent<PlayerSpriteView>() != null)
            {
                return false;
            }

            PlayerMotor motor = root.GetComponent<PlayerMotor>();
            Transform body = root.transform.Find("Body");
            Transform marker = root.transform.Find("FacingMarker");

            GameObject child = new GameObject(SpriteChildName);
            child.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites.Idle;
            renderer.sortingOrder = SpriteSortingOrder;

            root.AddComponent<PlayerSpriteView>().Bind(motor, sprites, renderer,
                body != null ? body.GetComponent<SpriteRenderer>() : null,
                marker != null ? marker.GetComponent<SpriteRenderer>() : null);
            return true;
        }
    }
}
