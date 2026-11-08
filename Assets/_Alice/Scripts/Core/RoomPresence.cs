using System;

namespace Alice.Core
{
    /// <summary>
    /// The stable ID of the room Alice is in (such as P_COMBAT), so a lost purse can remember where it lies
    /// without anyone relying on scene or object names. The map listens for entries to reveal rooms.
    /// </summary>
    public static class RoomPresence
    {
        public static string CurrentId { get; private set; } = string.Empty;

        public static event Action<string> Entered;

        public static void Enter(string roomId)
        {
            CurrentId = roomId ?? string.Empty;
            if (!string.IsNullOrEmpty(CurrentId))
            {
                Entered?.Invoke(CurrentId);
            }
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
