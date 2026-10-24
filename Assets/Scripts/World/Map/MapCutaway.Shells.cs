using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // Handing out the shells while the buildings are drawn, and the record each building's own shell leaves for the
    // runtime (see MapCutaway.cs).
    internal static partial class MapCutaway
    {
        /// <summary>Footprints smaller than this (m²) share the site's batches: too small to stand in, or to hide anyone.</summary>
        internal const float MinOwnShell = 4f;

        // The meshes handed out to the shells are kept and reused (a dense streamed piece has seventy-odd buildings, and
        // each new RaidMesh starts with a few dozen KB of lists): a site's meshes go back to the pool when the next site
        // asks for its first shell, by which time every pass of the earlier site has run.
        private const int PoolCap = 96;
        private const int PoolMaxVertices = 24000;   // a mesh grown bigger than this isn't kept
        private static readonly Stack<RaidMesh> s_Pool = new Stack<RaidMesh>(PoolCap);
        private static readonly List<RaidMesh> s_Pending = new List<RaidMesh>(128);

        /// <summary>A building's shell on <paramref name="site"/> (see <see cref="KentuckyMap.Site.Shell"/>). On a Full site
        /// a building inside the square gets meshes of its own, which <see cref="CutawayPass"/> emits; everything else
        /// (Lite sites, scenery past the edge, the tiniest sheds) draws into the site's shared batches and is never cut.</summary>
        internal static KentuckyMap.BuildingShell ShellFor(KentuckyMap.Site site, KentuckyMap.Footprint footprint, bool scenery)
        {
            if (site.Shells.Count == 0) Recycle();
            var shell = new KentuckyMap.BuildingShell { Footprint = footprint };
            if (scenery || site.Detail != KentuckyMap.Detail.Full || footprint.Size < MinOwnShell)
            {
                shell.Batched = true;
                Vector3 middle = KentuckyMap.To3(footprint.Middle, 0f);
                RaidMesh walls = scenery ? site.Town.Far.Solid(middle) : site.Town.Out.Solid(middle);
                for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = walls;
                shell.Upper = site.Roofs.At(middle);
            }
            else
            {
                // One mesh for all four faces: the runtime sorts a building's walls by facing only once it has to cut
                // some of them (see MapCutaway.Overlay.cs), so an uncut building costs two renderers.
                RaidMesh walls = Rent();
                for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = walls;
                shell.Upper = Rent();
            }
            site.Shells.Add(shell);
            return shell;
        }

        /// <summary>After <see cref="CutawayPass"/> has emitted a shell: its meshes go back to the pool (once the site is
        /// done) and the shell points at the site's shared batches (<see cref="KentuckyMap.BuildingShell.Batched"/>), so
        /// anything a later pass still draws into it shows, uncut, rather than landing in another building.</summary>
        internal static void Retire(KentuckyMap.Site site, KentuckyMap.BuildingShell shell)
        {
            Vector3 middle = KentuckyMap.To3(shell.Footprint.Middle, 0f);
            RaidMesh walls = shell.Walls[0], upper = shell.Upper;
            if (walls != null) s_Pending.Add(walls);
            if (upper != null && upper != walls) s_Pending.Add(upper);
            RaidMesh shared = site.Town.Out.Solid(middle);
            for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = shared;
            shell.Upper = site.Roofs.At(middle);
            shell.Batched = true;
        }

        private static RaidMesh Rent() => s_Pool.Count > 0 ? s_Pool.Pop() : new RaidMesh();

        private static void Recycle()
        {
            for (int i = 0; i < s_Pending.Count; i++)
            {
                RaidMesh m = s_Pending[i];
                if (m.VertexCount > PoolMaxVertices || s_Pool.Count >= PoolCap) continue;
                m.Clear();
                s_Pool.Push(m);
            }
            s_Pending.Clear();
        }

        /// <summary>
        /// One building with a shell of its own, as <see cref="CutawayPass"/> emitted it (site-local metres) and, once its
        /// site is registered, where it stands in the world and how much of it is cut away.
        /// </summary>
        internal sealed class Building
        {
            public long Id;
            public string Kind;
            /// <summary>The footprint's middle: the walls' and the roof's objects stand here (mesh space = minus this).</summary>
            public Vector2 Middle;
            /// <summary>The footprint, anticlockwise, and each edge's facing (<see cref="KentuckyMap.BuildingShell.Facing"/>).</summary>
            public Vector2[] Local;
            public byte[] EdgeGroup;
            /// <summary>Facings that have walls (bit per facing).</summary>
            public int Groups;
            /// <summary>When the roof reaches well past the walls (a porch roof, a canopy, deep eaves): its outline, as a
            /// convex octagon round everything in <see cref="Upper"/>; else null (the footprint will do).</summary>
            public Vector2[] LocalRoof;
            /// <summary>Top of the ground storey's walls and of the whole building, above its base.</summary>
            public float WallTop, Top;
            /// <summary>How thick the outside walls are (for sorting the walls by facing).</summary>
            public float WallThickness;
            public MeshRenderer Walls, Upper;

            // Registered: in the world.
            public MapCutawaySite Site;
            public Vector2[] Pts, Roof;
            public Rect Bounds, RoofBounds;
            public float Base;
            public int X0, Z0, X1, Z1;   // the spatial hash cells it's in

            // Cut away: parts (bits 0-3 the walls by facing, bit 4 the upper part) cut now, and when each last had to be.
            public int Applied;
            public readonly float[] Seen = { Never, Never, Never, Never, Never };
            public int Stamp;
            public bool Occupied, Active;

            // The walls' stand-in while some of them are cut (MapCutaway.Overlay.cs), made the first time they are.
            public MeshRenderer Overlay;
            public Mesh OverlayMesh;
            public UnityEngine.Rendering.SubMeshDescriptor[] OverlayParts;
            /// <summary>Each part slot's sub-mesh in <see cref="OverlayMesh"/>, or -1 when it's empty.</summary>
            public int[] OverlayMap;
            public int OverlayMaterials, OverlayShown = -1;
            public bool OverlayFailed;
        }
    }
}
