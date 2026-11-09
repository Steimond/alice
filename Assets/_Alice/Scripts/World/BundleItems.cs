using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// Whether the thing in a bundle (an ability or a pocket trinket) is already Alice's. Ability and trinket IDs
    /// never share a value, so one question covers both kinds of bundle.
    /// </summary>
    public static class BundleItems
    {
        public static bool AliceHas(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            Transform alice = PlayerPresence.Current;
            return AbilityCheck.AliceHas(itemId)
                || alice != null && alice.TryGetComponent(out ITrinketReceiver receiver) && receiver.HasTrinket(itemId);
        }
    }
}
