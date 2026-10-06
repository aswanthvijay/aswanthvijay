using Runeheir.World;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Field
{
    /// <summary>Static facts about the loaded map (save point, bounds, rules) for items like Raven Feather / Wind Rune Shard.</summary>
    public static class FieldContext
    {
        public static string MapId { get; private set; }

        public static string MapName { get; private set; }

        /// <summary>The loaded map's definition (null in a hand-built scene whose map id isn't in the catalog).</summary>
        public static MapDefinition Map { get; private set; }

        /// <summary>The generated layout (null in hand-built scenes).</summary>
        public static MapLayout Layout { get; private set; }

        public static Vector3 SavePoint { get; private set; }

        public static Bounds Bounds { get; private set; }

        /// <summary>Dead and Blood Branches can be cracked here (everywhere but Vigrid Haven).</summary>
        public static bool AllowsBranches => Map == null || Map.AllowBranches;

        /// <summary>Wind Rune Shards work here (not in towns or the arena).</summary>
        public static bool AllowsRandomTeleport => Map == null || Map.AllowRandomTeleport;

        public static void Set(string mapId, string mapName, Vector3 savePoint, Bounds bounds, MapLayout layout = null)
        {
            MapId = mapId;
            MapName = mapName;
            Map = MapCatalog.Get(mapId);
            Layout = layout;
            SavePoint = savePoint;
            Bounds = bounds;
        }

        public static bool TryGetRandomPoint(out Vector3 point)
        {
            var bounds = Bounds;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    Random.Range(bounds.min.z, bounds.max.z));

                // On a generated map only real ground counts (not the water of a fjord inlet under a NavMesh edge).
                if (Layout != null && !Layout.IsWalkable(new GroundPoint(candidate.x, candidate.z)))
                {
                    continue;
                }

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    point = hit.position;
                    return true;
                }
            }

            point = SavePoint;
            return false;
        }
    }
}
