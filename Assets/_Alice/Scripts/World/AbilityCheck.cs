using Alice.Abilities;
using Alice.Core;
using UnityEngine;

namespace Alice.World
{
    /// <summary>Helpers for the marked places where an ability works (GDD section 5).</summary>
    internal static class AbilityCheck
    {
        public static bool AliceHas(string abilityId)
        {
            Transform alice = PlayerPresence.Current;
            return alice != null && alice.TryGetComponent(out AbilityInventory inventory) && inventory.Has(abilityId);
        }
    }
}
