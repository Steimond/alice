using System.Collections;
using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// What every bundle does once its item is Alice's (GDD 0.28): Alice bends for it, the bundle stays on the floor
    /// until her hand takes it, then it is gone and the find is announced. Shared by ability bundles and trinket
    /// bundles, so they look and feel the same.
    /// </summary>
    internal static class BundleOpening
    {
        // The item object stands at the centre of its reach; the bundle art lies this far below, on the floor.
        private const float FootBelow = 0.75f;

        public static void Begin(MonoBehaviour bundle, GameObject alice, string itemId)
        {
            Vector2 foot = (Vector2)bundle.transform.position + Vector2.down * FootBelow;
            float grab = alice.TryGetComponent(out IItemPicker picker) ? picker.BeginPickUp(foot) : 0f;
            bundle.StartCoroutine(Open(bundle.gameObject, itemId, grab));
        }

        private static IEnumerator Open(GameObject bundle, string itemId, float grab)
        {
            if (grab > 0f)
            {
                yield return new WaitForSeconds(grab);
            }

            bundle.SetActive(false);
            ItemNotice.Announce(itemId);
        }
    }
}
