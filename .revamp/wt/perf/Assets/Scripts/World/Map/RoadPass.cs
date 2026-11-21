using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's roads in a Kentucky January: carriageways meeting in real junctions (one paved patch per
    /// junction, kerb returns round its corners, stop bars and crosswalks on its approaches), main roads ploughed to wet
    /// asphalt with slush between the wheel tracks, side streets and alleys under packed snow with tyre tracks, farm
    /// tracks as two ruts, low berms of ploughed snow along the edges (broken at junctions, crosswalks and driveways),
    /// kerbs, gutters and sidewalks where a town has them, driveway aprons, faded and half-buried markings, trodden
    /// paths, bridges with parapets and guard rails, median barriers, and the railways with their level crossings.
    /// Inside the playable square everything is built at the site's <see cref="Site.Detail"/> (Lite: the surfaces
    /// only); on a fixed map the roads carry on past the edge as scenery in the same colours.
    /// </summary>
    /// <remarks>
    /// Heights above the snow (y = 0, the ground's top), lowest first: trodden paths <see cref="PathY"/>; carriageways
    /// <see cref="RoadY"/> (a fraction of a millimetre higher for bigger roads, so overlapping ribbons never fight);
    /// their slush, tyre tracks and gutters <see cref="DetailY"/>; lane lines <see cref="MarkY"/>, slush over them
    /// <see cref="BuryY"/>; junction patches <see cref="PatchY"/> (they cover whatever the ribbons left under them, so a
    /// neighbouring chunk's ribbon running into a junction it can't see is harmless), their slush
    /// <see cref="PatchDetailY"/>; stop bars, crosswalks and level-crossing panels <see cref="JunctionMarkY"/>; kerb
    /// tops and sidewalks <see cref="KerbTop"/> (no colliders: the survivor walks on the ground collider, a few
    /// centimetres under the paving); berms, parapets, rails and crossbucks stand on top. The parts live in
    /// RoadPass.Network (what each street is, junction geometry), RoadPass.Streets (carriageways, kerbs, sidewalks,
    /// markings, berms, driveways, scenery), RoadPass.Paths, RoadPass.Rail, RoadPass.Bridges and RoadPass.Textures.
    /// The helpers at the top (<see cref="HalfWidth"/>, <see cref="EdgeOffset"/>, <see cref="SidewalkSides"/>,
    /// <see cref="KerbOffset"/>, <see cref="RoadsideOffset"/>, <see cref="ArmSetback"/>, <see cref="StopLine"/>...)
    /// tell the later passes where things stand.
    /// </remarks>
    internal static partial class RoadPass
    {
        /// <summary>Trodden paths through the snow.</summary>
        internal const float PathY = 0.0105f;
        /// <summary>The carriageways' surface (<see cref="MapStyle.StreetY"/>).</summary>
        internal const float RoadY = MapStyle.StreetY;
        /// <summary>Slush, tyre tracks and gutters on the carriageways.</summary>
        internal const float DetailY = RoadY + 0.0025f;
        /// <summary>Lane lines.</summary>
        internal const float MarkY = RoadY + 0.005f;
        /// <summary>Slush lying over the lines.</summary>
        internal const float BuryY = RoadY + 0.0065f;
        /// <summary>Junction patches: above everything a ribbon carries.</summary>
        internal const float PatchY = RoadY + 0.009f;
        /// <summary>Slush on the junction patches.</summary>
        internal const float PatchDetailY = RoadY + 0.0115f;
        /// <summary>Stop bars, crosswalks, level-crossing panels.</summary>
        internal const float JunctionMarkY = RoadY + 0.014f;
        /// <summary>Kerb tops and sidewalks.</summary>
        internal const float KerbTop = 0.10f;
        /// <summary>The kerb stone's top, from the kerb face back.</summary>
        internal const float KerbWidth = 0.16f;

        private const float StepFull = 2.5f;   // metres between the samples of a ribbon's jittered edges

        /// <summary>The roads' analysis of each street piece the last road pass built (the helpers below read it).</summary>
        private static readonly ConditionalWeakTable<Street, Way> s_Ways = new ConditionalWeakTable<Street, Way>();
        /// <summary>Each junction's arms' setbacks, as the last road pass built it.</summary>
        private static readonly ConditionalWeakTable<Junction, float[]> s_Setbacks = new ConditionalWeakTable<Junction, float[]>();

        internal static IEnumerable<string> Build(Site site)
        {
            var net = new Net(site);
            Analyse(net);
            yield return "road network";
            Junctions(net);
            PlanCuts(net);
            yield return "junctions";
            foreach (string step in Carriageways(net)) yield return step;
            yield return "streets";
            foreach (string step in SidesAndCorners(net)) yield return step;
            yield return "sidewalks";
            if (net.Full)
            {
                foreach (string step in Berms(net)) yield return step;
                DrawDriveways(net);
                yield return "berms and driveways";
                foreach (string step in Markings(net)) yield return step;
                yield return "road markings";
            }
            Paths(net);
            yield return "paths";
            Railways(net);
            yield return "railways";
            Bridges(net);
            yield return "bridges";
            if (!site.Chunked)
            {
                Scenery(net);
                yield return "scenery roads";
            }
            Publish(net);
        }

        #region Helpers for the later passes
        /// <summary>Half the carriageway as the roads drew it (the wider side, from the street's line to its kerb face or
        /// edge), or half the mapped width when the road pass hasn't seen the street.</summary>
        internal static float HalfWidth(Street s)
            => s_Ways.TryGetValue(s, out Way w) ? Mathf.Max(w.L.Edge, w.R.Edge) : s.Width * 0.5f;

        /// <summary>How far the carriageway reaches from the street's line on one side: <paramref name="side"/> +1 is left
        /// of the street's direction (its points' order), -1 right. Kerb face or plain edge.</summary>
        internal static float EdgeOffset(Street s, int side)
            => s_Ways.TryGetValue(s, out Way w) ? (side >= 0 ? w.L : w.R).Edge : s.Width * 0.5f;

        /// <summary>Which sides have a sidewalk: 0 none, 1 left, 2 right, 3 both (of the street's direction).</summary>
        internal static int SidewalkSides(Street s)
            => s_Ways.TryGetValue(s, out Way w) ? (w.L.Walk ? 1 : 0) | (w.R.Walk ? 2 : 0) : 0;

        /// <summary>From the street's line to the back of the kerb on one side (where a sidewalk or verge starts), or to
        /// the carriageway's edge where there's no kerb.</summary>
        internal static float KerbOffset(Street s, int side)
        {
            if (!s_Ways.TryGetValue(s, out Way w)) return s.Width * 0.5f;
            Side d = side >= 0 ? w.L : w.R;
            return d.Kerb ? d.Edge + KerbWidth : d.Edge;
        }

        /// <summary>From the street's line to the outer edge of its sidewalk on one side, or 0 when it has none.</summary>
        internal static float SidewalkOuter(Street s, int side)
            => s_Ways.TryGetValue(s, out Way w) && (side >= 0 ? w.L : w.R).Walk ? (side >= 0 ? w.L : w.R).WalkOut : 0f;

        /// <summary>Where roadside things (poles, signs, hydrants, mailboxes) stand on one side, from the street's line: on
        /// the sidewalk 0.6 m behind the kerb, or on the verge where the sidewalk is set back, or 1.2 m past an unkerbed
        /// edge (beyond the plough's berm).</summary>
        internal static float RoadsideOffset(Street s, int side)
        {
            if (!s_Ways.TryGetValue(s, out Way w)) return s.Width * 0.5f + 1.2f;
            Side d = side >= 0 ? w.L : w.R;
            if (!d.Kerb) return d.Edge + (w.Cls <= Cls.Trunk ? 2.6f : 1.2f);
            if (d.Walk && d.WalkIn > d.Edge + KerbWidth + 1.2f) return (d.Edge + KerbWidth + d.WalkIn) * 0.5f;
            return d.Edge + KerbWidth + 0.45f;
        }

        /// <summary>True where the street was ploughed down to the asphalt (main roads, downtown); false under packed snow,
        /// ruts or trodden paths.</summary>
        internal static bool Ploughed(Street s) => s_Ways.TryGetValue(s, out Way w) && w.Winter == Winter.Ploughed;

        /// <summary>True for a street the roads drew as a vehicle road (not a path).</summary>
        internal static bool IsCarriageway(Street s) => s_Ways.TryGetValue(s, out Way w) ? w.Carriage : s.Rank > 0;

        /// <summary>How far from the junction's point arm <paramref name="arm"/> (an index into
        /// <see cref="Junction.Arms"/>) leaves the junction's paved patch: where its own carriageway, kerbs and sidewalks
        /// start. Half the widest road there when the road pass didn't shape the junction.</summary>
        internal static float ArmSetback(Junction j, int arm)
        {
            if (s_Setbacks.TryGetValue(j, out float[] t) && arm >= 0 && arm < t.Length && t[arm] > 0f) return t[arm];
            float widest = 0f;
            foreach (JunctionArm a in j.Arms) widest = Mathf.Max(widest, a.HalfWidth);
            return widest + 1f;
        }

        /// <summary>The stop line on arm <paramref name="arm"/>'s approach: <paramref name="at"/> is the middle of the
        /// approach lane's stop bar (traffic coming in keeps right, so it's left of the arm's direction away from the
        /// junction), <paramref name="distance"/> its distance from the junction's point. False for an arm traffic can't
        /// come in by (a one-way street leaving the junction).</summary>
        internal static bool StopLine(Junction j, int arm, out Vector2 at, out float distance)
        {
            at = j.At;
            distance = 0f;
            if (arm < 0 || arm >= j.Arms.Count) return false;
            JunctionArm a = j.Arms[arm];
            bool forward = a.Vertex + 1 < a.Street.Pts.Count && Vector2.Dot(a.Street.Pts[a.Vertex + 1] - a.Street.Pts[a.Vertex], a.Dir) > 0f;
            if (a.Street.Data.oneway && forward) return false;
            distance = ArmSetback(j, arm) + 1.2f;
            var left = new Vector2(-a.Dir.y, a.Dir.x);
            float across = a.Street.Data.oneway ? 0f : EdgeAcross(a.Street, forward ? 1 : -1) * 0.5f;
            at = j.At + a.Dir * distance + left * across;
            return true;
        }

        // Half the approach lane's reach from the line, for the stop line's middle.
        private static float EdgeAcross(Street s, int side) => EdgeOffset(s, side);
        #endregion

        #region What the roads are drawn with (fresh every build: the palette and materials can be remade between builds)
        private sealed class Look
        {
            public Surf Asphalt, Concrete, Packed, Sidewalk, Snow, Trodden, TroddenLine, Paved;
            public Surf Kerb, KerbFace, Gutter, Slush, SlushLight, Wet, Track, TrackGravel, Rut, BermGrime;
            public Surf Yellow, White, Panel, Rubber, Apron, Crack, Tar, Pothole;
            public Surf Ballast, Tie, TieSnow, Rail, RailHead, RailRust, Girder;
            public Surf Parapet, ParapetCap, GuardRail, GuardPost, CrossbuckWhite, CrossbuckBlack, PostGrey, LampRed, LampHood;

            public Look()
            {
                Asphalt = DecorMaterials.Planar(RoadTextures.WetAsphalt(), 14f, 0.42f);
                Concrete = DecorMaterials.Planar(RoadTextures.RoadConcrete(), 7f, 0.25f);
                Packed = DecorMaterials.Planar(RoadTextures.PackedSnow(), 9f, 0.22f);
                Sidewalk = DecorMaterials.Planar(RoadTextures.Sidewalk(), 3f, 0.12f);
                Snow = DecorMaterials.Snow;
                Trodden = DecorPalette.Matte(new Color(0.76f, 0.78f, 0.83f));
                TroddenLine = DecorPalette.Matte(new Color(0.66f, 0.68f, 0.74f));
                Paved = DecorMaterials.Planar(RoadTextures.Sidewalk(), 3f, 0.12f);
                Kerb = DecorPalette.Matte(new Color(0.74f, 0.74f, 0.72f));
                KerbFace = DecorPalette.Matte(new Color(0.56f, 0.56f, 0.55f));
                Gutter = DecorPalette.Matte(new Color(0.47f, 0.46f, 0.44f));
                Slush = DecorPalette.Matte(new Color(0.55f, 0.53f, 0.50f));
                SlushLight = DecorPalette.Matte(new Color(0.68f, 0.67f, 0.66f));
                Wet = DecorPalette.Gloss(new Color(0.13f, 0.13f, 0.14f));
                Track = DecorPalette.Matte(new Color(0.42f, 0.40f, 0.38f));
                TrackGravel = DecorPalette.Matte(new Color(0.46f, 0.39f, 0.31f));
                Rut = DecorPalette.Matte(new Color(0.50f, 0.45f, 0.40f));
                BermGrime = DecorPalette.Matte(new Color(0.66f, 0.66f, 0.66f));
                Yellow = DecorPalette.Matte(new Color(0.70f, 0.58f, 0.24f));
                White = DecorPalette.Matte(new Color(0.78f, 0.78f, 0.76f));
                Panel = DecorPalette.Matte(new Color(0.55f, 0.55f, 0.53f));
                Rubber = DecorPalette.Matte(new Color(0.20f, 0.20f, 0.21f));
                Apron = DecorMaterials.Planar(RoadTextures.Sidewalk(), 3f, 0.12f);
                Crack = DecorPalette.Matte(new Color(0.075f, 0.075f, 0.08f));
                Tar = DecorPalette.Gloss(new Color(0.035f, 0.035f, 0.04f));
                Pothole = DecorPalette.Gloss(new Color(0.10f, 0.11f, 0.12f));
                Ballast = DecorMaterials.Planar(RoadTextures.SnowyBallast(), 3f, 0.08f);
                Tie = DecorPalette.Matte(new Color(0.25f, 0.20f, 0.16f));
                TieSnow = DecorPalette.Matte(new Color(0.80f, 0.82f, 0.87f));
                Rail = DecorPalette.Metal(new Color(0.30f, 0.27f, 0.25f));
                RailHead = DecorPalette.Metal(new Color(0.62f, 0.62f, 0.63f));
                RailRust = DecorPalette.Matte(new Color(0.40f, 0.24f, 0.16f));
                Girder = DecorPalette.Metal(new Color(0.24f, 0.25f, 0.27f));
                Parapet = DecorPalette.Matte(new Color(0.66f, 0.65f, 0.62f));
                ParapetCap = DecorPalette.Matte(new Color(0.86f, 0.89f, 0.95f));
                GuardRail = DecorPalette.Metal(new Color(0.66f, 0.67f, 0.68f));
                GuardPost = DecorPalette.Matte(new Color(0.36f, 0.29f, 0.22f));
                CrossbuckWhite = DecorPalette.Matte(new Color(0.88f, 0.88f, 0.86f));
                CrossbuckBlack = DecorPalette.Matte(new Color(0.08f, 0.08f, 0.08f));
                PostGrey = DecorPalette.Metal(new Color(0.58f, 0.58f, 0.58f));
                LampRed = DecorPalette.Gloss(new Color(0.36f, 0.05f, 0.04f));
                LampHood = DecorPalette.Matte(new Color(0.06f, 0.06f, 0.06f));
            }
        }
        #endregion

        #region Drawing primitives
        /// <summary>A four-cornered face, <paramref name="a"/> to <paramref name="d"/> round its edge, turned to face
        /// <paramref name="toward"/>: one quad when it's a parallelogram (4 vertices), else two triangles.</summary>
        private static void Face(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 toward, in Surf s)
        {
            if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(d)) return;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), toward) < 0f)
            {
                Vector3 t = b;
                b = d;
                d = t;
            }
            if ((a + c - b - d).sqrMagnitude < 1e-8f) m.Quad((a + c) * 0.5f, (d - a) * 0.5f, (b - a) * 0.5f, s);
            else
            {
                m.Tri(a, b, c, s);
                m.Tri(a, c, d, s);
            }
        }

        /// <summary>A triangle turned to face <paramref name="toward"/>.</summary>
        private static void Tri(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 toward, in Surf s)
        {
            if (!Finite(a) || !Finite(b) || !Finite(c)) return;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), toward) < 0f) m.Tri(a, c, b, s);
            else m.Tri(a, b, c, s);
        }

        private static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
            && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

        /// <summary>A face-up polygon (any winding, star-shaped from <paramref name="hub"/>) fanned from its hub; slivers
        /// that turn over (a polygon folding on itself) are left out.</summary>
        private static void Fan(OutdoorBatches into, Vector2 hub, List<Vector2> ring, float y, in Surf s)
        {
            if (ring.Count < 2) return;
            float area = MapGeometry.SignedArea(ring);
            if (float.IsNaN(area) || !Finite(hub)) return;
            Vector3 h = To3(hub, y);
            RaidMesh m = into.Flat(h);
            for (int i = 0; i < ring.Count; i++)
            {
                Vector2 p = ring[i], q = ring[(i + 1) % ring.Count];
                float turn = (p.x - hub.x) * (q.y - hub.y) - (p.y - hub.y) * (q.x - hub.x);
                if (area >= 0f ? turn <= 1e-5f : turn >= -1e-5f) continue;
                if (area >= 0f) m.Tri(h, To3(q, y), To3(p, y), s);
                else m.Tri(h, To3(p, y), To3(q, y), s);
            }
        }

        /// <summary>A box standing on the ground along a plan segment (its length along it), for walls, rails, posts.</summary>
        private static void Beam(RaidMesh m, Vector2 a, Vector2 b, float y0, float height, float thick, in Surf s)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            float yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            m.Box(To3((a + b) * 0.5f, y0 + height * 0.5f), new Vector3(len, height, thick), yaw, s);
        }

        private static Vector2 Left(Vector2 d) => new Vector2(-d.y, d.x);

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        #endregion

        #region Noise that two chunks agree on
        /// <summary>Smooth value noise in [0, 1] over the ground, from site-local metres, the same at a world point in
        /// whichever chunk draws it (<see cref="RaidMesh.PlanarOrigin"/> is the chunk's place on the world grid, wrapped
        /// every 252 m; <paramref name="cell"/> must divide 252, so the wrap never shows).</summary>
        private static float Noise(Vector2 local, float cell, int seed)
        {
            Vector3 o = RaidMesh.PlanarOrigin;
            int period = Mathf.Max(1, Mathf.RoundToInt(252f / cell));
            float x = (local.x + o.x) / cell, z = (local.y + o.z) / cell;
            int xi = Mathf.FloorToInt(x), zi = Mathf.FloorToInt(z);
            float fx = x - xi, fz = z - zi;
            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);
            float a = Lattice(xi, zi, period, seed), b = Lattice(xi + 1, zi, period, seed);
            float c = Lattice(xi, zi + 1, period, seed), d = Lattice(xi + 1, zi + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }

        private static float Lattice(int x, int z, int period, int seed)
        {
            x %= period;
            if (x < 0) x += period;
            z %= period;
            if (z < 0) z += period;
            return MapStyle.Unit(((long)x << 20) ^ z, seed);
        }
        #endregion

        // Hands the analysis to the helpers above.
        private static void Publish(Net net)
        {
            foreach (Way w in net.Ways)
            {
                if (w.S == null) continue;
                if (s_Ways.TryGetValue(w.S, out Way old))
                {
                    if (old == w || old.Length >= w.Length) continue;
                    s_Ways.Remove(w.S);
                }
                s_Ways.Add(w.S, w);
            }
            if (net.Joints.Count == 0) return;
            var byPoint = new Dictionary<long, Joint>(net.Joints.Count);
            foreach (Joint j in net.Joints) byPoint[SnapKey(j.At)] = j;
            foreach (Junction pub in net.Site.Junctions)
            {
                if (!byPoint.TryGetValue(SnapKey(pub.At), out Joint j) || !j.HasPatch) continue;
                var t = new float[pub.Arms.Count];
                for (int i = 0; i < pub.Arms.Count; i++)
                {
                    JunctionArm pa = pub.Arms[i];
                    foreach (Arm a in j.Arms)
                        if (a.W.S == pa.Street && Vector2.Dot(a.Dir, pa.Dir) > 0.9f)
                        {
                            t[i] = a.Through ? Mathf.Max(a.TanL, a.TanR) : a.Trim;
                            break;
                        }
                }
                s_Setbacks.Remove(pub);
                s_Setbacks.Add(pub, t);
            }
        }
    }
}
