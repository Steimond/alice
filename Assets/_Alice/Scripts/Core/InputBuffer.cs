namespace Alice.Core
{
    /// <summary>Time-window checks for buffered button presses.</summary>
    public static class InputBuffer
    {
        /// <summary>True when the press happened after the last consumed one and is still inside the window.</summary>
        public static bool IsFresh(float pressedAt, float consumedAt, float now, float window)
        {
            return pressedAt > consumedAt && now - pressedAt <= window;
        }
    }
}
