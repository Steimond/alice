namespace Alice.Core
{
    /// <summary>
    /// The fixed scale of the game. Art, rooms and the camera are all built on these numbers;
    /// changing one invalidates every sprite and room, so they are constants rather than settings.
    /// </summary>
    public static class WorldMetrics
    {
        /// <summary>Sprite pixels in one world unit.</summary>
        public const int PixelsPerUnit = 16;

        /// <summary>One tile is one world unit; level geometry snaps to whole tiles.</summary>
        public const int TilePixels = 16;

        public const float TileSize = (float)TilePixels / PixelsPerUnit;

        /// <summary>Internal render resolution: x3 at 1080p, x4 at 1440p, x6 at 4K.</summary>
        public const int ReferenceWidth = 640;

        public const int ReferenceHeight = 360;

        public const float ViewWidthTiles = (float)ReferenceWidth / TilePixels;
        public const float ViewHeightTiles = (float)ReferenceHeight / TilePixels;
    }
}
