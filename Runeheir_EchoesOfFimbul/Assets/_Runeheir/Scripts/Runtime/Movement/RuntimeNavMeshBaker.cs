using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Runeheir.Movement
{
    /// <summary>
    /// Bakes a NavMesh at load time from every collider under this object, with no package required.
    /// Ideal for prototype / procedurally generated maps. For hand-authored maps, use the AI Navigation
    /// package's NavMeshSurface and bake in the editor instead; this component then does nothing because
    /// a NavMesh already exists (see <see cref="skipIfNavMeshExists"/>).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class RuntimeNavMeshBaker : MonoBehaviour
    {
        private const int NotWalkableArea = 1;

        [SerializeField] private Vector3 boundsSize = new Vector3(200f, 40f, 200f);
        [SerializeField] private LayerMask includedLayers = ~0;
        [Tooltip("NavMesh agent type (0 = Humanoid).")]
        [SerializeField] private int agentTypeId;
        [SerializeField] private bool skipIfNavMeshExists = true;

        private static int s_runtimeBakes;

        private NavMeshDataInstance _instance;

        public static bool HasAnyNavMesh => NavMesh.CalculateTriangulation().vertices.Length > 0;

        public Bounds WorldBounds => new Bounds(transform.position, boundsSize);

        private void Awake()
        {
            // Skip only when the NavMesh came from an editor bake (NavMeshSurface), not from another runtime bake.
            if (skipIfNavMeshExists && s_runtimeBakes == 0 && HasAnyNavMesh)
            {
                return;
            }

            Bake();
        }

        public void Bake()
        {
            RemoveInstance();

            var markups = new List<NavMeshBuildMarkup>();
            foreach (var blocker in GetComponentsInChildren<NavBlocker>(true))
            {
                markups.Add(new NavMeshBuildMarkup
                {
                    root = blocker.transform,
                    overrideArea = true,
                    area = NotWalkableArea,
                });
            }

            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(transform, includedLayers, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            var settings = NavMesh.GetSettingsByID(agentTypeId);
            var localBounds = new Bounds(Vector3.zero, boundsSize);
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, localBounds, transform.position, Quaternion.identity);
            _instance = NavMesh.AddNavMeshData(data);
            if (_instance.valid)
            {
                s_runtimeBakes++;
            }

            Debug.Log($"[Runeheir] Runtime NavMesh baked from {sources.Count} colliders.");
        }

        private void OnDestroy()
        {
            RemoveInstance();
        }

        private void RemoveInstance()
        {
            if (_instance.valid)
            {
                _instance.Remove();
                s_runtimeBakes = System.Math.Max(0, s_runtimeBakes - 1);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(transform.position, boundsSize);
        }
    }
}
