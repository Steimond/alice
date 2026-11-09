namespace Alice.World
{
    /// <summary>
    /// What a point on the map shows (GDD 0.34): only what Alice has found — where she is, the resting chairs, the
    /// bundles still lying where she saw them — and the player's own marks. Gates, lifts and hazards stay off the
    /// map. New kinds go at the end: saves and the atlas store the number.
    /// </summary>
    public enum MapIconKind
    {
        Alice = 0,
        Rest = 1,
        Item = 9,
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
