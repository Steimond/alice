using System;
using System.IO;
using System.Linq;
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
        // Thrust frames from the generation: wind-up, two of the extended arm, two of the return.
        private const int ThrustFrameCount = 5;
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

            if (!sprites.HasThrust)
            {
                Sprite[] thrust = LoadFrames(ThrustPrefix);
                if (thrust.Length == ThrustFrameCount)
                {
                    sprites.AssignThrust(new[] { thrust[0] }, new[] { thrust[1], thrust[2] }, new[] { thrust[3], thrust[4] });
                    EditorUtility.SetDirty(sprites);
                }
            }

            return sprites;
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
