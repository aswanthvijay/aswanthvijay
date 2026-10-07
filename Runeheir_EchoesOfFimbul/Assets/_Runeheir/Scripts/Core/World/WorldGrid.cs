namespace Runeheir.World
{
    /// <summary>
    /// Where each map sits in an online realm (Phase 6). The realm server builds every map that has players into one
    /// scene, so maps get their own square of the world, <see cref="Spacing"/> meters apart, in catalog order. Clients
    /// build their map at the same spot, so positions mean the same thing everywhere. Offline play keeps every map at
    /// the origin. Saved positions are always map-local (relative to the map's origin).
    /// </summary>
    public static class WorldGrid
    {
        /// <summary>Far wider than any map (the largest is 190 m), so maps never see or touch each other.</summary>
        public const float Spacing = 1000f;

        public const int Columns = 4;

        /// <summary>The map's world offset (X, Z). Unknown maps sit at the origin.</summary>
        public static GroundPoint Origin(string mapId)
        {
            int index = IndexOf(mapId);
            if (index < 0)
            {
                return new GroundPoint(0f, 0f);
            }

            return new GroundPoint(index % Columns * Spacing, index / Columns * Spacing);
        }

        /// <summary>The map whose square contains (<paramref name="x"/>, <paramref name="z"/>), or null.</summary>
        public static MapDefinition MapAt(float x, float z)
        {
            foreach (var map in MapCatalog.All)
            {
                var origin = Origin(map.Id);
                float half = Spacing * 0.5f;
                if (x >= origin.X - half && x < origin.X + half && z >= origin.Z - half && z < origin.Z + half)
                {
                    return map;
                }
            }

            return null;
        }

        private static int IndexOf(string mapId)
        {
            var all = MapCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i].Id, mapId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
