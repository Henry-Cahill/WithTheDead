using Unity.AI.Navigation;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Holds the world's <see cref="NavMeshSurface"/> so gameplay can rebuild the baked
    /// NavMesh when a baked-in obstacle (a cover block, a world container, a floor of the
    /// starter home) is demolished. The surface is scoped to the World subtree, so a rebuild
    /// ignores the root-level player and zombies (which must never bake in as static obstacles).
    /// </summary>
    public static class WorldNavMesh
    {
        private static NavMeshSurface s_Surface;

        public static void Register(NavMeshSurface surface) => s_Surface = surface;

        /// <summary>True when <paramref name="t"/> lies under the surface, so the bake takes in its geometry
        /// (unless a <see cref="NavMeshModifier"/> keeps it out) and removing it calls for a <see cref="Rebuild"/>.</summary>
        public static bool Collects(Transform t) => s_Surface != null && t != null && t.IsChildOf(s_Surface.transform);

        /// <summary>Re-bakes the world NavMesh. Call after removing a baked-in obstacle so the freed
        /// space becomes walkable again. Cheap for our small arena; only triggered by demolition.</summary>
        public static void Rebuild()
        {
            if (s_Surface != null) s_Surface.BuildNavMesh();
        }
    }
}
