using System.Collections.Generic;
using UnityEngine;

namespace Alice.Core
{
    /// <summary>A place Alice rests at (GDD section 4), addressed by a stable ID such as R_P01.</summary>
    public interface IRestSpot
    {
        string RestId { get; }

        /// <summary>Where Alice's feet go when she sits, and where she wakes after a defeat.</summary>
        Vector2 SeatPosition { get; }

        /// <summary>+1 faces right.</summary>
        int Facing { get; }
    }

    /// <summary>The rest spots of the loaded rooms, so a save can put Alice back at the one it names.</summary>
    public static class RestSpots
    {
        private static readonly List<IRestSpot> Spots = new List<IRestSpot>();

        public static void Register(IRestSpot spot)
        {
            if (!Spots.Contains(spot))
            {
                Spots.Add(spot);
            }
        }

        public static void Unregister(IRestSpot spot)
        {
            Spots.Remove(spot);
        }

        public static IRestSpot Find(string restId)
        {
            if (string.IsNullOrEmpty(restId))
            {
                return null;
            }

            foreach (IRestSpot spot in Spots)
            {
                if (spot.RestId == restId)
                {
                    return spot;
                }
            }

            return null;
        }
    }
}
