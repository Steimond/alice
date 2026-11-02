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
    /// over the grey-box body. Adds only what is missing and never replaces sprites someone assigned.
    /// </summary>
    public static class AlicePlayerSpritesSetup
    {
        private const string Root = "Assets/_Alice";
        private const string FramesFolder = Root + "/Art/Characters/Alice/Trial";
        private const string IdlePath = FramesFolder + "/alice_trial_idle_00.png";
        private const string RunPrefix = "alice_trial_run_";
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

            if (sprites.Idle != null || (sprites.Run != null && sprites.Run.Length > 0))
            {
                return sprites;
            }

            Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(IdlePath);
            Sprite[] run = AssetDatabase.FindAssets("t:Sprite " + RunPrefix, new[] { FramesFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(RunPrefix, StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
            if (idle == null || run.Length == 0)
            {
                Debug.LogWarning($"Alice trial sprites not found in {FramesFolder}; the sprite set stays empty.");
                return null;
            }

            sprites.Assign(idle, run);
            EditorUtility.SetDirty(sprites);
            return sprites;
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
