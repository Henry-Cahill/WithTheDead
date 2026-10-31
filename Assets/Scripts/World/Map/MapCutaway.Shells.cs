using UnityEngine;

namespace CloneZ
{
    // Handing out the shells while the buildings are drawn, and the record each building's own shell leaves for the
    // runtime (see MapCutaway.cs).
    internal static partial class MapCutaway
    {
        /// <summary>Footprints smaller than this (m²) share the site's batches: too small to stand in, or to hide anyone.</summary>
        internal const float MinOwnShell = 4f;
        /// <summary>Footprints larger than <see cref="MinOwnShell"/> but smaller than this get their own wall mesh (so they can be cut away) while still using the shared roof batch.</summary>
        internal const float OwnWallThreshold = 20f;

        /// <summary>Off: every building draws into the site's shared batches and nothing is ever cut (the harness's
        /// "before" counts).</summary>
        internal static bool OwnShells = true;

        /// <summary>A building's shell on <paramref name="site"/> (see <see cref="KentuckyMap.Site.Shell"/>). On a Full site
        /// a building inside the square gets meshes of its own, which <see cref="CutawayPass"/> emits; everything else
        /// (Lite sites, scenery past the edge, the tiniest sheds) draws into the site's shared batches and is never cut.</summary>
        internal static KentuckyMap.BuildingShell ShellFor(KentuckyMap.Site site, KentuckyMap.Footprint footprint, bool scenery)
        {
            var shell = new KentuckyMap.BuildingShell { Footprint = footprint };
            if (!OwnShells || scenery || site.Detail != KentuckyMap.Detail.Full)
            {
                // everything stays batched – unchanged from original
                shell.Batched = true;
                Vector3 middle = KentuckyMap.To3(footprint.Middle, 0f);
                RaidMesh walls = scenery ? site.Town.Far.Solid(middle) : site.Town.Out.Solid(middle);
                for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = walls;
                shell.Upper = site.Roofs.At(middle);
            }
            else if (footprint.Size < MinOwnShell)
            {
                // still too tiny – fully batched (original behaviour for tiny sheds)
                shell.Batched = true;
                Vector3 middle = KentuckyMap.To3(footprint.Middle, 0f);
                RaidMesh walls = scenery ? site.Town.Far.Solid(middle) : site.Town.Out.Solid(middle);
                for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = walls;
                shell.Upper = site.Roofs.At(middle);
            }
            else if (footprint.Size < OwnWallThreshold)
            {
                // own wall mesh only – roof stays shared
                var walls = new RaidMesh();
                for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = walls;
                shell.Upper = site.Roofs.At(KentuckyMap.To3(footprint.Middle, 0f)); // shared roof batch
            }
            else
            {
                // original path – own walls *and* own roof
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
            /// <summary>When the roof reaches past the walls (eaves, a porch roof, an awning, a sign): the box in plan round the
            /// footprint and everything above it, site-local (from the upper part's vertices as drawn, no mesh read back).</summary>
            public Rect LocalCover;
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
            public System.Collections.Generic.List<Renderer> DoorNoShadow;
            public int DoorCut = -1;

            // A building with floors above or below the ground one (MapCutaway.Levels.cs): its levels; its upper part's
            // storeys apart from the roof (Bands[0] what stands between the ground storey and level 1's floor, Bands[k] storey
            // k) and each storey's walls as a building of their own to the stand-ins (BandWalls[k], cut by facing while the
            // survivor is on that floor); each level's floors and stairs and its stairwell's walls (index level - Lowest).
            public KentuckyMap.LevelLayout Levels;
            public MeshRenderer[] Bands;
            public Mesh[] BandMeshes;
            public Building[] BandWalls;
            public Renderer[] Structure, Stairwells;
            /// <summary>For a storey's walls standing in as a building (see <see cref="BandWalls"/>): the floor its cut walls'
            /// stubs stand on (0 for a building's own walls, which stand on its base).</summary>
            public float StubBase;
            /// <summary>The level the survivor is on in it while they're in it (<see cref="int.MinValue"/> when they aren't),
            /// the level everything over which is lifted now (<see cref="int.MaxValue"/>: nothing is) and the facings of that
            /// floor's walls lowered; when the hidden parts were last made sure of.</summary>
            public int Level = int.MinValue, Shown = int.MaxValue, ShownFacing;
            public float Reassert;
        }
    }
}
