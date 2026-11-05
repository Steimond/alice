using System;
using System.Collections.Generic;

namespace Alice.Core
{
    /// <summary>
    /// Permanent changes to the world (GDD section 4): opened doors, pulled levers, later collected unique
    /// finds. A rest and a defeat never undo them; the save keeps them. Stable string IDs such as door:D_M3A_W.
    /// </summary>
    public static class WorldFlags
    {
        private static readonly HashSet<string> Set = new HashSet<string>();

        public static event Action<string> Raised;

        public static IEnumerable<string> All => Set;

        public static bool Has(string flag)
        {
            return !string.IsNullOrEmpty(flag) && Set.Contains(flag);
        }

        public static void Raise(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && Set.Add(flag))
            {
                Raised?.Invoke(flag);
            }
        }

        /// <summary>A save is loaded or a new game starts: the flags become exactly these.</summary>
        public static void Replace(IEnumerable<string> flags)
        {
            Set.Clear();
            if (flags == null)
            {
                return;
            }

            foreach (string flag in flags)
            {
                if (!string.IsNullOrEmpty(flag))
                {
                    Set.Add(flag);
                }
            }
        }
    }
}
