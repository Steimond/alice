namespace Alice.Core
{
    /// <summary>
    /// The stable ID of the room Alice is in (such as P_COMBAT), so a lost purse can remember where it lies
    /// without anyone relying on scene or object names.
    /// </summary>
    public static class RoomPresence
    {
        public static string CurrentId { get; private set; } = string.Empty;

        public static void Enter(string roomId)
        {
            CurrentId = roomId ?? string.Empty;
        }

        public static void Leave(string roomId)
        {
            if (CurrentId == roomId)
            {
                CurrentId = string.Empty;
            }
        }
    }
}
