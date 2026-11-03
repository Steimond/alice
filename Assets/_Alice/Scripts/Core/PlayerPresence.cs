using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// Where Alice is, for enemies and other systems that react to her. Rooms spawn enemies without
    /// scene references to the player, so they look her up here instead of by name or tag.
    /// </summary>
    public static class PlayerPresence
    {
        public static Transform Current { get; private set; }

        public static void Register(Transform player)
        {
            Current = player;
        }

        public static void Unregister(Transform player)
        {
            if (Current == player)
            {
                Current = null;
            }
        }
    }
}
