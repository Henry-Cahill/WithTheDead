using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The types the map's passes hand each other: shared by the revamp's parallel passes, so kept apart from any one
    // pass and frozen while they're written.
    public static partial class KentuckyMap
    {
        /// <summary>How much of a site is built: Full near survivors (and the fixed square), Lite for far streamed chunks.</summary>
        internal enum Detail { Full, Lite }

        /// <summary>Where two or more street pieces meet (site-local).</summary>
        internal sealed class Junction
        {
            public Vector2 At;
            /// <summary>The streets' layer (bridges/tunnels only meet their own layer).</summary>
            public int Layer;
            public readonly List<JunctionArm> Arms = new List<JunctionArm>();
        }

        internal struct JunctionArm
        {
            public Street Street;
            /// <summary>Index in Street.Pts of the junction point.</summary>
            public int Vertex;
            /// <summary>Unit, pointing away from the junction along the street.</summary>
            public Vector2 Dir;
            /// <summary>Half the carriageway width (Street.Data.width / 2, min 1.5).</summary>
            public float HalfWidth;
        }

        /// <summary>A building the exteriors pass made enterable, for the interiors pass to furnish. Site-local metres;
        /// Parent is the site's root (it moves with a streamed chunk).</summary>
        internal sealed class EnterableBuilding
        {
            public Footprint Footprint;
            public Transform Parent;
            /// <summary>Floor to floor.</summary>
            public float StoreyHeight;
            /// <summary>Storeys with floors (1 = ground only).</summary>
            public int Storeys;
            public float WallThickness;
            public int Seed;
            public readonly List<Doorway> Doors = new List<Doorway>();
            /// <summary>Its windows on every storey (<see cref="WindowSpan.Storey"/>), as built.</summary>
            public readonly List<WindowSpan> Windows = new List<WindowSpan>();
            /// <summary>Its floors above and below the ground one and the stairwell between them (null: the ground floor only).</summary>
            public LevelLayout Levels;
            /// <summary>The glass standing in each storey's working windows (<see cref="BuildingShell.Panes"/>, as
            /// <see cref="CutawayPass"/> emitted it; index the storey), switched off while that floor's real windows are in
            /// (<see cref="WindowStreamer"/>); null when it has none.</summary>
            public Renderer[] Panes;
            /// <summary>Which storeys have their real windows in now (index the storey; null till one has): their stand-in glass
            /// stays off.</summary>
            public bool[] Furnished;
            /// <summary>Its front door's yard fittings, as the exteriors pass placed them (null without a front door): made to work
            /// as the starter home's when its ground floor is furnished.</summary>
            public YardSpots Yard;
        }

        /// <summary>Where a building's front-door fittings are (site-local): the door and its step, the wall mailbox, the
        /// house-number plate.</summary>
        internal sealed class YardSpots
        {
            /// <summary>The front door's middle on the outline and the way out through it; the top of the step, porch or ground
            /// before it.</summary>
            public Vector2 Door, Outward;
            public float Step;
            /// <summary>The wall mailbox's front, at its height; false <see cref="HasMailbox"/> when it has none.</summary>
            public Vector3 Mailbox;
            public bool HasMailbox;
            /// <summary>The house-number plate on the wall's face and the way it faces, and the number on it (null: no plate).</summary>
            public Vector3 Plate, PlateNormal;
            public string Number;
        }

        internal struct Doorway { public int Edge; public Vector2 At; public Vector2 Outward; public float Width; }

        /// <summary>An opening along edge Pts[Edge] -> Pts[(Edge+1)%n], From/To metres from the edge's start; Sill/Head above
        /// the storey's floor. <see cref="Working"/>: a real opening through the wall with its fixed sash in the wall, its
        /// working parts (<see cref="RaidWindow"/>) fitted near the survivors once its floor is furnished. <see cref="Clear"/>: a
        /// real opening too, with clear glass fixed in it (a shop front, a church's, a band, one that doesn't open), every other
        /// window of a building with a floor at it; <see cref="Plain"/> one of a tall building's upper floors', its glass and no
        /// more (no casing or dressing inside). Else glass drawn on the wall.</summary>
        internal struct WindowSpan { public int Edge; public int Storey; public float From, To, Sill, Head; public bool Working, Clear, Plain; }

        /// <summary>Room kinds shared by the interiors and loot code.</summary>
        internal static class RoomKinds
        {
            public const string Living = "living", Kitchen = "kitchen", Bathroom = "bathroom", Bedroom = "bedroom", Hall = "hall",
                Sales = "sales", Stock = "stock", Office = "office", Classroom = "classroom", Nave = "nave", Workshop = "workshop",
                Garage = "garage", Storage = "storage", Ward = "ward", Cells = "cells", Kitchenette = "kitchenette", Open = "open";
        }

        /// <summary>
        /// Where one building's visible geometry goes, so the isometric camera can show roofs and upper storeys from
        /// outside and cut away, Project Zomboid-style, the building the survivor is in and whatever stands between
        /// the camera and them. Get one with <see cref="Site.Shell"/>. On a Full site each building gets meshes of its
        /// own, which <see cref="CutawayPass"/> emits and <see cref="MapCutaway"/> hides and shows; on a Lite site and
        /// for scenery past the edge the meshes are the site's shared batches, never cut away. Site-local metres, like
        /// every batch.
        /// </summary>
        internal sealed class BuildingShell
        {
            public Footprint Footprint;
            /// <summary>Top of the ground storey's walls (set by the exteriors pass).</summary>
            public float WallTop;
            /// <summary>The building's highest point: ridge, steeple, parapet (set by the exteriors pass).</summary>
            public float Top;
            /// <summary>True when <see cref="Walls"/> and <see cref="Upper"/> are the site's shared batches.</summary>
            public bool Batched;
            /// <summary><see cref="Upper"/> split by storey ahead of the cutaway pass's emitting it, a slice at a time
            /// (<see cref="Levels.UpperSplit"/>); null till then, and after.</summary>
            public RaidMesh[] UpperBands;
            /// <summary>The ground storey's outside walls and what hangs on them (doors, windows, trim, signs, awnings),
            /// by the way their outward face looks (see <see cref="Facing"/>): the cutaway lowers the faces turned to
            /// the camera.</summary>
            public readonly RaidMesh[] Walls = new RaidMesh[4];
            /// <summary>Everything above the ground storey's walls: upper storeys, ceilings, roofs, eaves, chimneys,
            /// roof snow and icicles.</summary>
            public RaidMesh Upper;
            /// <summary>A building with floors above or below the ground one (<see cref="EnterableBuilding.Levels"/>): each
            /// level's floor, stairs and stairwell walls (index level - <see cref="LevelLayout.Lowest"/>), which the cutaway
            /// hides with the level; null for the others.</summary>
            public RaidMesh[] Structure;
            /// <summary>The stairwell's own walls on each level (as <see cref="Structure"/>), objects of their own with
            /// their colliders, which the isometric camera cuts away as it does the rooms' partitions; null for the others.</summary>
            public GameObject[] Stairwells;
            /// <summary>Its levels, when it has any (as <see cref="Structure"/>).</summary>
            public LevelLayout Levels;
            /// <summary>Glass standing in each storey's working windows (<see cref="WindowSpan.Working"/>; index the storey) while
            /// that floor's real windows aren't in (they're fitted near the survivors, <see cref="WindowStreamer"/>), so an
            /// opening never shows empty (see <see cref="EnterableBuilding.Panes"/>); null when there are none.</summary>
            public RaidMesh[] Panes;

            /// <summary>0 when an outward face looks north (+z), 1 east (+x), 2 south, 3 west.</summary>
            public static int Facing(Vector2 outward)
                => Mathf.Abs(outward.y) >= Mathf.Abs(outward.x) ? (outward.y >= 0f ? 0 : 2) : (outward.x >= 0f ? 1 : 3);

            /// <summary>The wall mesh for a face looking along <paramref name="outward"/>.</summary>
            public RaidMesh WallsFacing(Vector2 outward) => Walls[Facing(outward)];
        }
    }
}
