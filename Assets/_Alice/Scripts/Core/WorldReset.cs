using System;

namespace Alice.Core
{
    /// <summary>
    /// The reset a rest brings (GDD section 4): ordinary enemies back at their posts, alive and hostile.
    /// A defeat drags Alice back to the rest, so it raises the same reset. Systems listen here instead of
    /// the player reaching into them.
    /// </summary>
    public static class WorldReset
    {
        public static event Action Happened;

        public static void Raise()
        {
            Happened?.Invoke();
        }
    }
}
