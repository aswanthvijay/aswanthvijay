using UnityEngine;

namespace Runeheir.Movement
{
    /// <summary>
    /// Marks a collider as an obstacle for <see cref="RuntimeNavMeshBaker"/>: its footprint is carved
    /// out of the NavMesh (trees, rocks, runestones) instead of becoming a walkable island on top.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NavBlocker : MonoBehaviour
    {
    }
}
