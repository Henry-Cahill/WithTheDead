using UnityEngine;

namespace CloneZ
{
    // Handing out the shells while the buildings are drawn, and the record each building's own shell leaves for the
    // runtime (see MapCutaway.cs).
    internal static partial class MapCutaway
    {
        /// <summary>Footprints smaller than this (m²) share the site's batches: too small to stand in, or to hide anyone.</summary>
        internal const float MinOwnShell = 4f;

        /// <summary>Off: every building draws into the site's shared batches and nothing is ever cut (the harness's
        /// "before" counts).</summary>
        internal static bool OwnShells = true;

        /// <summary>A building's shell on <paramref name="site"/> (see <see cref="KentuckyMap.Site.Shell"/>). On a Full site
        /// a building inside the square gets meshes of its own, which <see cref="CutawayPass"/> emits; everything else
        /// (Lite sites, scenery past the edge, the tiniest sheds) draws into the site's shared batches and is never cut.</summary>
        internal static KentuckyMap.BuildingShell ShellFor(KentuckyMap.Site site, KentuckyMap.Footprint footprint, bool scenery)
        {
            var shell = new KentuckyMap.BuildingShell { Footprint = footprint };
            if (!OwnShells || scenery || site.Detail != KentuckyMap.Detail.Full || footprint.Size < MinOwnShell)
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
                // some of them (see MapCutaway.Overlay.cs), so an uncut building costs two renderers. (A RaidMesh starts
                // empty and rents its arrays from RaidMesh's shared pool, giving them back once baked.)
                var walls = new RaidMesh();
                for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = walls;
                shell.Upper = new RaidMesh();
            }
            site.Shells.Add(shell);
            return shell;
        }

        /// <summary>After <see cref="CutawayPass"/> has emitted a shell: it points at the site's shared batches from then on
        /// (<see cref="KentuckyMap.BuildingShell.Batched"/>), so anything a later pass still draws into it shows, uncut,
        /// rather than landing in a mesh that's already baked (or queued to be).</summary>
        internal static void Retire(KentuckyMap.Site site, KentuckyMap.BuildingShell shell)
        {
            Vector3 middle = KentuckyMap.To3(shell.Footprint.Middle, 0f);
            RaidMesh shared = site.Town.Out.Solid(middle);
            for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = shared;
            shell.Upper = site.Roofs.At(middle);
            shell.Batched = true;
        }

        /// <summary>
        /// One building with a shell of its own, as <see cref="CutawayPass"/> emitted it (site-local metres) and, once its
        /// site is registered, where it stands in the world and how much of it is cut away.
        /// </summary>
        internal sealed class Building
        {
            public long Id;
            public string Kind;
            /// <summary>The footprint's middle: the walls' and the upper part's objects stand here (mesh space = minus this).</summary>
            public Vector2 Middle;
            /// <summary>The footprint, anticlockwise, and each edge's facing (<see cref="KentuckyMap.BuildingShell.Facing"/>).</summary>
            public Vector2[] Local;
            public byte[] EdgeGroup;
            /// <summary>Facings that have walls (bit per facing).</summary>
            public int Groups;
            /// <summary>Top of the ground storey's walls and of the whole building, above its base: as the exteriors pass
            /// set them on the shell, then (once the meshes are baked, see Measure) what the meshes reach.</summary>
            public float WallTop, Top;
            /// <summary>The ground storey's top as the exteriors pass set it: while the storeys above are lifted, the walls are
            /// shown clipped here (MapCutaway.Overlay.cs).</summary>
            public float ClipTop;
            /// <summary>The walls reach above <see cref="ClipTop"/> (a tall window's head, the linings of a tall room): lifting
            /// the storeys above needs the walls' stand-in, or those bits would float.</summary>
            public bool ClipWalls;
            /// <summary>Set once the baked meshes' bounds have been read into <see cref="WallTop"/>, <see cref="Top"/> and
            /// <see cref="ClipWalls"/>.</summary>
            public bool Measured;
            /// <summary>How thick the outside walls are (for sorting the walls by facing).</summary>
            public float WallThickness;
            public MeshRenderer Walls, Upper;
            /// <summary>Their meshes: empty until baked when they were queued on the site's bake.</summary>
            public Mesh WallsMesh, UpperMesh;

            // Registered: in the world.
            public MapCutawaySite Site;
            public Vector2[] Pts;
            /// <summary>The footprint's bounds in the world.</summary>
            public Rect Bounds;
            /// <summary>What the building may cover in plan (the footprint and whatever reaches past its walls: eaves, porch
            /// roofs, awnings): the spatial hash cells it's in.</summary>
            public Rect Cover;
            public float Base;
            public int X0, Z0, X1, Z1;

            // The upper part's heights (MapCutaway.Heights.cs), read from its baked mesh the first time a scan meets it (a
            // slice at a time): per cell of a coarse grid in plan, the highest point of the upper part above the base in
            // HeightStep units (0 = nothing there). Null until read (or when the building has no upper part).
            public byte[] Height;
            public float HX, HZ, HCell;
            public int HW, HH;
            public bool Resolved;

            // Cut away: parts (bits 0-3 the walls by facing, bit 4 the upper part) cut now, and when each last had to be; what
            // the last scan wanted cut, and since when (MapCutaway.Work's frames) a cut has been held back for the stand-in.
            public int Applied, Wanted, HoldFrame = -1;
            public readonly float[] Seen = { Never, Never, Never, Never, Never };
            public int Stamp;
            public bool Occupied, Active;

            // The walls' stand-in while some of them are cut or the storeys above lifted (MapCutaway.Overlay.cs), made ahead
            // of need, and what the walls show now: WallsWhole, WallsNone, or the facings lowered on the stand-in.
            public MeshRenderer Overlay;
            public Mesh OverlayMesh;
            public UnityEngine.Rendering.SubMeshDescriptor[] OverlayParts;
            /// <summary>Each part slot's sub-mesh in <see cref="OverlayMesh"/>, or -1 when it's empty.</summary>
            public int[] OverlayMap;
            public int OverlayMaterials, OverlayShown = -1, WallState = WallsWhole;
            public long OverlayBytes;
            public bool OverlayFailed;
            /// <summary>Waiting in MapCutaway.Work's lists: a cut wants its stand-in now; a scan met it unread.</summary>
            public bool Urgent, ToRead;
            /// <summary>The doors in its cut walls and the renderers of theirs hidden with them (MapCutaway.Doors.cs).</summary>
            public System.Collections.Generic.List<HomeDoor> DoorObjs;
            public System.Collections.Generic.List<Renderer> Doors;
        }
    }
}
