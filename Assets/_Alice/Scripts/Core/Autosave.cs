using System;

namespace Alice.Core
{
    /// <summary>
    /// The autosave points of GDD section 10: a rest, waking after a defeat, a new ability. Systems ask here;
    /// the save system listens and writes, so nobody reaches into it directly.
    /// </summary>
    public static class Autosave
    {
        public static event Action Requested;

        public static void Request()
        {
            Requested?.Invoke();
        }
    }
}
