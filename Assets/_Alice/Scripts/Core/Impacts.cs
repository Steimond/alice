using System;

namespace Alice.Core
{
    /// <summary>
    /// A blow that should be felt: whoever lands or takes it raises an impact with a strength in screen
    /// pixels, and the camera shakes. The camera does not need to know who fights whom.
    /// </summary>
    public static class Impacts
    {
        public static event Action<float> Happened;

        public static void Raise(float pixels)
        {
            if (pixels > 0f)
            {
                Happened?.Invoke(pixels);
            }
        }
    }
}
