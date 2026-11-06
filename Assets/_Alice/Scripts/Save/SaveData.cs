using System;
using System.Collections.Generic;

namespace Alice.Save
{
    /// <summary>
    /// What a save keeps (GDD section 10), addressed by stable IDs only. Fields grow with the game; the
    /// schema version tells an old file from a new one.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchema = 1;

        public int Schema = CurrentSchema;

        /// <summary>The rest Alice wakes at; empty before the first rest.</summary>
        public string RestId = string.Empty;

        /// <summary>The room that chair stands in, so a loaded game starts there.</summary>
        public string RestRoomId = string.Empty;

        /// <summary>Permanent changes to the world: opened doors, pulled levers.</summary>
        public List<string> Flags = new List<string>();

        public int Decrees;
        public float Confidence;
        public DecreeLossData Loss = new DecreeLossData();
        public string EquippedTechnique = string.Empty;
        public List<string> KnownTechniques = new List<string>();
        public List<string> Abilities = new List<string>();
        public List<string> Trinkets = new List<string>();
        public List<string> WornTrinkets = new List<string>();
        // 0 in saves written before pockets existed: read as the starting count.
        public int Pockets;
    }

    /// <summary>The single lost purse: where it lies and how much is in it.</summary>
    [Serializable]
    public sealed class DecreeLossData
    {
        public bool Exists;
        public string RoomId = string.Empty;
        public float X;
        public float Y;
        public int Amount;
    }
}
