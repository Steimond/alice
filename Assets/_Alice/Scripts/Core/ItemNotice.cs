using System;

namespace Alice.Core
{
    /// <summary>
    /// A found item (GDD 0.28): every item in the world is a bundle, and opening it shows what was inside, its icon
    /// and name in the middle of the screen. The world announces the find by the item's stable ID; the interface
    /// knows how to show it.
    /// </summary>
    public static class ItemNotice
    {
        public static event Action<string> Found;

        public static void Announce(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                Found?.Invoke(itemId);
            }
        }
    }
}
