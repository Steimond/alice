using System.Collections.Generic;

namespace Alice.Core
{
    /// <summary>
    /// Black over the screen asked for by any system (a room change); the screen fade shows the darkest request
    /// together with Alice's own defeat darkness.
    /// </summary>
    public static class ScreenDarkness
    {
        private static readonly Dictionary<object, float> Requests = new Dictionary<object, float>();

        public static float Current
        {
            get
            {
                float darkest = 0f;
                foreach (float value in Requests.Values)
                {
                    darkest = value > darkest ? value : darkest;
                }

                return darkest;
            }
        }

        public static void Set(object source, float darkness)
        {
            if (darkness <= 0f)
            {
                Requests.Remove(source);
            }
            else
            {
                Requests[source] = darkness > 1f ? 1f : darkness;
            }
        }
    }
}
