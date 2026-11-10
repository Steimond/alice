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
        private const string IdlePrefix = "alice_trial_idle_";
        private const string IdleFidgetPrefix = "alice_trial_fidget_";
        private const string RestPrefix = "alice_trial_rest_";
        private const int RestDozeFrames = 4;
        private const string IdlePath = FramesFolder + "/" + IdlePrefix + "00.png";
        private const string RunPrefix = "alice_trial_run_";
        private const string JumpPrefix = "alice_trial_jump_";
        private const string FallPrefix = "alice_trial_fall_";
        private const string LandPrefix = "alice_trial_land_";
        private const string StepOffPrefix = "alice_trial_stepoff_";
        private const string SlashPrefix = "alice_trial_slash_";
        private const string CutPrefix = "alice_trial_cut_";
        private const string FinisherPrefix = "alice_trial_finisher_";
        private const string AirSlashPrefix = "alice_trial_airslash_";
        private const string CounterPrefix = "alice_trial_counter_";
        private const string ParryPrefix = "alice_trial_parry_";

        // The combat moves are 12-frame animations with anticipation and follow-through (owner, 4 Nov 2026).
        // Frames per phase: wind-up, the blow, recovery.
        private static readonly int[] SlashSplit = { 4, 3, 5 };
        // Sword combo (GDD 0.44): the rising cut, then the finisher with its long drawn-back hold over the head,
        // the chop into the floor and a slow rise.
        private static readonly int[] CutSplit = { 4, 3, 4 };
        private static readonly int[] FinisherSplit = { 5, 3, 8 };
        private static readonly int[] AirSlashSplit = { 5, 3, 4 };
        // Coil, the flat sweep and the held extension, the return.
        private static readonly int[] CounterSplit = { 3, 3, 6 };
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
        private const string SmallIdlePrefix = "alice_trial_small_idle_";
        private const string SmallRunPrefix = "alice_trial_small_run_";
        private const string SmallRisePrefix = "alice_trial_small_rise_";
        private const string SmallApexPrefix = "alice_trial_small_apex_";
        private const string SmallFallPrefix = "alice_trial_small_fall_";
        private const string SmallLandPrefix = "alice_trial_small_land_";
        private const string ShrinkPrefix = "alice_trial_shrink_";
        private const string BitePrefix = "alice_trial_bite_";
        private const string PickupPrefix = "alice_trial_pickup_";
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
            Sprite[] cut = LoadFrames(CutPrefix);
            Sprite[] finisher = LoadFrames(FinisherPrefix);
            if (NeedsStrike(cut, CutSplit, sprites.Cut) || NeedsStrike(finisher, FinisherSplit, sprites.Finisher))
            {
                sprites.AssignCombo(Split(cut, CutSplit), Split(finisher, FinisherSplit));
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

            Sprite[] breath = LoadFrames(IdlePrefix);
            if (breath.Length > 1 && (sprites.IdleBreath == null || sprites.IdleBreath.Length != breath.Length))
            {
                sprites.AssignIdleBreath(breath);
                EditorUtility.SetDirty(sprites);
            }

            // Rest: the whole sit into the armchair, then a doze that nods the head back and forth over the
            // last frames, where she has already settled.
            Sprite[] sit = LoadFrames(RestPrefix);
            if (sit.Length > RestDozeFrames && (sprites.RestSit == null || sprites.RestSit.Length != sit.Length))
            {
                Sprite[] settled = sit.Skip(sit.Length - RestDozeFrames).ToArray();
                Sprite[] doze = settled.Reverse().Concat(settled.Skip(1).Take(RestDozeFrames - 2)).ToArray();
                sprites.AssignRest(sit, doze);
                EditorUtility.SetDirty(sprites);
            }

            // Small Alice (mushroom): assigned once the frames on disk exist and the set has none.
            if (!sprites.HasSmall)
            {
                Sprite[] smallIdle = LoadFrames(SmallIdlePrefix);
                Sprite[] smallRun = LoadFrames(SmallRunPrefix);
                Sprite[] smallRise = LoadFrames(SmallRisePrefix);
                Sprite[] smallApex = LoadFrames(SmallApexPrefix);
                Sprite[] smallFall = LoadFrames(SmallFallPrefix);
                if (smallIdle.Length > 0 && smallRun.Length > 0 && smallRise.Length > 0 && smallApex.Length > 0
                    && smallFall.Length > 0)
                {
                    sprites.AssignSmall(smallIdle, smallRun, smallRise, smallApex, smallFall, LoadFrames(SmallLandPrefix),
                        LoadFrames(ShrinkPrefix));
                    EditorUtility.SetDirty(sprites);
                }
            }

            Sprite[] pickup = LoadFrames(PickupPrefix);
            if (pickup.Length > 0 && (sprites.Pickup == null || sprites.Pickup.Length != pickup.Length))
            {
                sprites.AssignPickup(pickup);
                EditorUtility.SetDirty(sprites);
            }

            Sprite[] bite = LoadFrames(BitePrefix);
            if (bite.Length > 0 && (sprites.Bite == null || sprites.Bite.Length != bite.Length))
            {
                sprites.AssignBite(bite);
                EditorUtility.SetDirty(sprites);
            }

            Sprite[] fidget = LoadFrames(IdleFidgetPrefix);
            if (fidget.Length > 0 && (sprites.IdleFidget == null || sprites.IdleFidget.Length != fidget.Length))
            {
                sprites.AssignIdleFidget(fidget);
                EditorUtility.SetDirty(sprites);
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
