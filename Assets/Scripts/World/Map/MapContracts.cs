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
            public readonly List<WindowSpan> Windows = new List<WindowSpan>();
        }

        internal struct Doorway { public int Edge; public Vector2 At; public Vector2 Outward; public float Width; }

        /// <summary>An opening along edge Pts[Edge] -> Pts[(Edge+1)%n], From/To metres from the edge's start; Sill/Head above
        /// the storey's floor.</summary>
        internal struct WindowSpan { public int Edge; public int Storey; public float From, To, Sill, Head; }

        /// <summary>Room kinds shared by the interiors and loot code.</summary>
        internal static class RoomKinds
        {
            public const string Living = "living", Kitchen = "kitchen", Bathroom = "bathroom", Bedroom = "bedroom", Hall = "hall",
                Sales = "sales", Stock = "stock", Office = "office", Classroom = "classroom", Nave = "nave", Workshop = "workshop",
                Garage = "garage", Storage = "storage", Ward = "ward", Cells = "cells", Kitchenette = "kitchenette", Open = "open";
        }
    }
}
