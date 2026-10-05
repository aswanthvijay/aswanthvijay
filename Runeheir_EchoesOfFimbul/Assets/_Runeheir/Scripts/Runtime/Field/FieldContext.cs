using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Field
{
    /// <summary>Static facts about the loaded map (save point, bounds) for items like Raven Feather / Wind Rune Shard.</summary>
    public static class FieldContext
    {
        public static string MapId { get; private set; }

        public static string MapName { get; private set; }

        public static Vector3 SavePoint { get; private set; }

        public static Bounds Bounds { get; private set; }

        public static void Set(string mapId, string mapName, Vector3 savePoint, Bounds bounds)
        {
            MapId = mapId;
            MapName = mapName;
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
