using Unity.AI.Navigation;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Holds the world's <see cref="NavMeshSurface"/> so gameplay can rebuild the baked
    /// NavMesh when a baked-in obstacle (a cover block, a world container, a floor of the
    /// starter home) is demolished. The surface is scoped to the World subtree, so a rebuild
    /// ignores the root-level player and zombies (which must never bake in as static obstacles).
    /// A streamed map (<see cref="KentuckyStreamer"/>) has no surface: it bakes the World subtree
    /// around the survivor itself and takes the rebuilds through <see cref="RegisterStreamed"/>.
    /// </summary>
    public static class WorldNavMesh
    {
        private static NavMeshSurface s_Surface;
        private static Transform s_Root;
        private static System.Action s_Rebuild;

        public static void Register(NavMeshSurface surface)
        {
            s_Surface = surface;
            s_Root = surface != null ? surface.transform : null;
            s_Rebuild = null;
        }

        /// <summary>A NavMesh baked from the <paramref name="root"/> subtree by its owner, which
        /// <paramref name="rebuild"/> asks to bake again.</summary>
        public static void RegisterStreamed(Transform root, System.Action rebuild)
        {
            s_Surface = null;
            s_Root = root;
            s_Rebuild = rebuild;
        }

        /// <summary>True when <paramref name="t"/> lies under the surface, so the bake takes in its geometry
        /// (unless a <see cref="NavMeshModifier"/> keeps it out) and removing it calls for a <see cref="Rebuild"/>.</summary>
        public static bool Collects(Transform t) => s_Root != null && t != null && t.IsChildOf(s_Root);

        /// <summary>Re-bakes the world NavMesh. Call after removing a baked-in obstacle so the freed
        /// space becomes walkable again. Cheap for our small arena; only triggered by demolition.
        /// On a streamed map the bake around the survivor is redone in the background.</summary>
        public static void Rebuild()
        {
            if (s_Rebuild != null) s_Rebuild();
            else if (s_Surface != null) s_Surface.BuildNavMesh();
        }
    }
}
