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

        /// <summary>The room Alice was in when the game was saved; the slot screen names its zone.</summary>
        public string RoomId = string.Empty;

        /// <summary>Time played in this slot, in seconds.</summary>
        public float PlayTime;

        /// <summary>When the slot was last written, UTC, round-trip format ("o").</summary>
        public string SavedAtUtc = string.Empty;

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

        // Saves from GDD 0.33 kept whole visited rooms in "MapRooms"; that field is no longer read, so such a map
        // starts over and opens as Alice walks (GDD 0.34).

        /// <summary>The cells explored in each room, as Alice walked them.</summary>
        public List<MapRoomCellsData> MapCells = new List<MapRoomCellsData>();

        /// <summary>Rooms revealed whole by a map, by stable room ID.</summary>
        public List<string> MapWholeRooms = new List<string>();

        /// <summary>Marks the player left on the map.</summary>
        public List<MapMarkData> MapMarks = new List<MapMarkData>();
    }

    /// <summary>One room's explored cells: a bit per cell of its map rectangle, row by row, in Base64.</summary>
    [Serializable]
    public sealed class MapRoomCellsData
    {
        public string Room = string.Empty;
        public string Bits = string.Empty;
    }

    /// <summary>One player mark: its kind (MapIconKind as a number) and where it lies, in world tiles.</summary>
    [Serializable]
    public sealed class MapMarkData
    {
        public int Kind;
        public float X;
        public float Y;
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
