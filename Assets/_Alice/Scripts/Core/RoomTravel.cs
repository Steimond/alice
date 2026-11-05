using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// A place in a room where Alice can arrive: a doorway's landing or a rest chair. Addressed by a stable ID,
    /// unique across the map (such as D_M3A_E or R_M3A_01).
    /// </summary>
    public interface IArrivalSpot
    {
        string SpotId { get; }
        Vector2 ArrivalPosition { get; }

        /// <summary>+1 faces right.</summary>
        int ArrivalFacing { get; }
    }

    /// <summary>Whatever can be put down at an arrival spot: Alice, through her own components.</summary>
    public interface IRoomArrival
    {
        void ArriveAt(Vector2 position, int facing);
    }

    /// <summary>
    /// Moving between rooms (GDD section 13: one room per scene, no streaming of the whole map). A doorway, a
    /// defeat that wakes Alice in another room's chair, or a loaded save asks here; the room loader listens,
    /// swaps the room scenes in the dark and puts Alice at the spot. Without a loader (a prototype room played
    /// on its own) requests simply go unanswered.
    /// </summary>
    public static class RoomTravel
    {
        private static readonly List<IArrivalSpot> Spots = new List<IArrivalSpot>();

        public static event Action<string, string> Requested;

        /// <summary>True from a request until Alice stands in the new room.</summary>
        public static bool IsTravelling { get; set; }

        /// <summary>Set when a room scene is played directly in the Editor: the save then keeps her there.</summary>
        public static bool StartedInRoom { get; set; }

        public static bool HasListener => Requested != null;

        public static void Request(string roomId, string spotId)
        {
            Requested?.Invoke(roomId, spotId);
        }

        public static void Register(IArrivalSpot spot)
        {
            if (!Spots.Contains(spot))
            {
                Spots.Add(spot);
            }
        }

        public static void Unregister(IArrivalSpot spot)
        {
            Spots.Remove(spot);
        }

        public static IArrivalSpot FindSpot(string spotId)
        {
            if (string.IsNullOrEmpty(spotId))
            {
                return null;
            }

            foreach (IArrivalSpot spot in Spots)
            {
                if (spot.SpotId == spotId)
                {
                    return spot;
                }
            }

            return null;
        }
    }
}
