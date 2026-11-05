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
    /// Puts the generated gate art (Art/Reviews/Alice/Revision08) on the M3 gates in place of the tinted grey
    /// placeholders. Gates are found by their components, never by object names. The placeholder renderer sits
    /// at the trigger's centre, while the sprites pivot on their foot, so each prop's art goes on a child placed on
    /// the floor. Running it again only refreshes the sprites; a gate whose sprite file is missing keeps its
    /// placeholder.
    /// </summary>
    public static class AliceGateArtSetup
    {
        private const string ArtFolder = "Assets/_Alice/Art/World/Gates";
        private const string RoomsFolder = "Assets/_Alice/Scenes/M3";
        private const int PostSortingOrder = 3;

        // Where the prop's foot is, relative to the gate object the builder made (see AliceRoomsBuilder).
        private static readonly Vector2 PickupFoot = new Vector2(0f, -0.5f);      // object at floor + 0.75: hovers
        private static readonly Vector2 MarkerFoot = new Vector2(0f, -0.5f);      // object at floor + 0.5
        private static readonly Vector2 LittleDoorFoot = new Vector2(0f, -0.75f); // object at floor + 0.75
        private static readonly Vector2 BladeFoot = new Vector2(0f, -2.5f);       // blade centre, 5 tiles high
        // The bridge deck is 12 px (0.75 tile) thick; its top meets the collider's top, 0.25 above the object.
        private const float BridgeDeckHeight = 12f / 16f;
        // The uprights overlap the blade's edges by a pixel, so it reads as running in their grooves yet stays
        // in plain sight.
        private const float PostOffset = 1.125f;

        [MenuItem("Alice/Prototype/Apply Gate Art to M3 Rooms")]
        public static void ApplyToRoomsFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Gate art: leave Play mode first.");
                return;
            }

            string previous = SceneManager.GetActiveScene().path;
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { RoomsFolder }).Select(AssetDatabase.GUIDToAssetPath))
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (Apply(scene))
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

        /// <summary>Dresses every gate in the scene; true when something changed.</summary>
        public static bool Apply(Scene scene)
        {
            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (AbilityPickup pickup in root.GetComponentsInChildren<AbilityPickup>(true))
                {
                    changed |= Dress(pickup.gameObject, Load("gate_pickup_" + pickup.AbilityId), PickupFoot) != null;
                }

                foreach (ClockMarker marker in root.GetComponentsInChildren<ClockMarker>(true))
                {
                    changed |= Dress(marker.gameObject, Load("gate_clock"), MarkerFoot) != null;
                }

                foreach (RoomDoorway doorway in root.GetComponentsInChildren<RoomDoorway>(true).Where(d => d.RequiresSmall))
                {
                    changed |= DressLittleDoor(doorway);
                }

                foreach (CheshireBridge bridge in root.GetComponentsInChildren<CheshireBridge>(true))
                {
                    changed |= DressBridge(bridge);
                }

                foreach (ClockGuillotine guillotine in root.GetComponentsInChildren<ClockGuillotine>(true))
                {
                    changed |= DressGuillotine(guillotine);
                }
            }

            return changed;
        }

        private static bool DressLittleDoor(RoomDoorway doorway)
        {
            // The builder hangs the placeholder on a child of the doorway trigger.
            Transform art = doorway.transform.Cast<Transform>().FirstOrDefault(child =>
                child.GetComponent<SpriteRenderer>() != null || child.GetComponentInChildren<SpriteRenderer>(true) != null);
            Sprite[] frames = LoadFrames("gate_little_door_");
            if (art == null || frames.Length == 0)
            {
                return false;
            }

            SpriteRenderer renderer = Dress(art.gameObject, frames[0], LittleDoorFoot);
            if (doorway.TryGetComponent(out LittleDoor door))
            {
                // The door draws its own frames: shut, swinging open, standing open.
                door.Bind(renderer, frames);
                EditorUtility.SetDirty(door);
            }

            return true;
        }

        private static Sprite[] LoadFrames(string prefix)
        {
            return AssetDatabase.FindAssets(prefix + " t:Sprite", new[] { ArtFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileName(path).StartsWith(prefix, System.StringComparison.Ordinal))
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
        }

        private static bool DressBridge(CheshireBridge bridge)
        {
            Sprite sprite = Load("gate_bridge");
            Collider2D surface = bridge.GetComponent<Collider2D>();
            if (sprite == null || surface == null)
            {
                return false;
            }

            float top = surface.offset.y + ((BoxCollider2D)surface).size.y * 0.5f;
            SpriteRenderer renderer = Dress(bridge.gameObject, sprite, new Vector2(0f, top - BridgeDeckHeight));
            if (renderer == null)
            {
                return false;
            }

            // The bridge fades its renderer in and out, so it must now drive the art child.
            bridge.Bind(surface, renderer);
            EditorUtility.SetDirty(bridge);
            return true;
        }

        private static bool DressGuillotine(ClockGuillotine guillotine)
        {
            Rigidbody2D blade = guillotine.GetComponentInChildren<Rigidbody2D>(true);
            Sprite post = Load("gate_post");
            bool changed = blade != null && Dress(blade.gameObject, Load("gate_blade"), BladeFoot) != null;
            if (post == null)
            {
                return changed;
            }

            SpriteRenderer[] posts = guillotine.transform.Cast<Transform>()
                .Select(child => child.GetComponent<SpriteRenderer>())
                .Where(renderer => renderer != null && renderer.sprite == post)
                .ToArray();
            for (int i = posts.Length; i < 2; i++)
            {
                GameObject upright = new GameObject("Upright");
                upright.transform.SetParent(guillotine.transform, false);
                upright.AddComponent<SpriteRenderer>();
            }

            posts = guillotine.transform.Cast<Transform>()
                .Select(child => child.GetComponent<SpriteRenderer>())
                .Where(renderer => renderer != null && (renderer.sprite == post || renderer.sprite == null))
                .Take(2)
                .ToArray();
            for (int i = 0; i < posts.Length; i++)
            {
                int side = i == 0 ? -1 : 1;
                posts[i].sprite = post;
                posts[i].flipX = side > 0;
                posts[i].sortingOrder = PostSortingOrder;
                posts[i].transform.localPosition = new Vector3(side * PostOffset, 0f, 0f);
            }

            return true;
        }

        /// <summary>
        /// Swaps the placeholder on <paramref name="target"/> for a child holding the sprite with its foot at
        /// <paramref name="foot"/>; returns the art renderer, or null when there is no sprite.
        /// </summary>
        private static SpriteRenderer Dress(GameObject target, Sprite sprite, Vector2 foot)
        {
            if (sprite == null)
            {
                return null;
            }

            int order = 0;
            if (target.TryGetComponent(out SpriteRenderer placeholder))
            {
                order = placeholder.sortingOrder;
                Object.DestroyImmediate(placeholder);
            }

            SpriteRenderer art = target.transform.Cast<Transform>()
                .Select(child => child.GetComponent<SpriteRenderer>())
                .FirstOrDefault(renderer => renderer != null);
            if (art == null)
            {
                GameObject child = new GameObject("Art");
                child.transform.SetParent(target.transform, false);
                art = child.AddComponent<SpriteRenderer>();
                art.sortingOrder = order;
            }

            art.sprite = sprite;
            art.drawMode = SpriteDrawMode.Simple;
            art.transform.localPosition = foot;
            EditorUtility.SetDirty(art);
            return art;
        }

        private static Sprite Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
        }
    }
}
