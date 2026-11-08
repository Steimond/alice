namespace Alice.World
{
    /// <summary>
    /// What a point on the map shows (GDD section 10). Gates differ by shape and symbol, not only by colour.
    /// New kinds go at the end: saves and the atlas store the number.
    /// </summary>
    public enum MapIconKind
    {
        Alice = 0,
        Rest = 1,
        ShortcutDoor = 2,
        Lift = 3,
        GateFan = 4,
        GateMushroom = 5,
        GateCheshire = 6,
        GateClock = 7,
        LostDecrees = 8,
        MarkPin = 20,
        MarkCross = 21,
        MarkButton = 22,
        MarkKnot = 23
    }

    public static class MapIconKinds
    {
        /// <summary>The marks a player can leave, in the order the palette of marks cycles through them.</summary>
        public static readonly MapIconKind[] PlayerMarks =
        {
            MapIconKind.MarkPin,
            MapIconKind.MarkCross,
            MapIconKind.MarkButton,
            MapIconKind.MarkKnot
        };
    }
}
