using Unity.AI.Navigation;

namespace CloneZ
{
    /// <summary>
    /// Holds the world's <see cref="NavMeshSurface"/> so gameplay can rebuild the baked
    /// NavMesh when a baked-in obstacle (a cover block or a world container) is demolished.
    /// The surface is scoped to the World subtree, so a rebuild ignores the root-level
    /// player and zombies (which must never bake in as static obstacles).
    /// </summary>
    public static class WorldNavMesh
    {
        private static NavMeshSurface s_Surface;

        public static void Register(NavMeshSurface surface) => s_Surface = surface;

        /// <summary>Re-bakes the world NavMesh. Call after removing a baked-in obstacle so the freed
        /// space becomes walkable again. Cheap for our small arena; only triggered by demolition.</summary>
        public static void Rebuild()
        {
            if (s_Surface != null) s_Surface.BuildNavMesh();
        }
    }
}
