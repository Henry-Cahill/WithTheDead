using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The railways: a ballast bed dusted with snow, wooden ties (some under snow), two steel rails with polished heads
    // (rusty on disused lines, gone from abandoned ones); deck girders on a rail bridge; and where a track runs on a
    // carriageway, rails set flush in the road, with concrete crossing panels and a crossbuck with flashing lights on
    // each approach where it crosses one. La Grange's Main Street, for one, has its trains run down the middle.
    internal static partial class RoadPass
    {
        private const float Gauge = 1.435f;
        private const float BedTop = 0.12f, TieTop = 0.13f, RailTop = 0.27f;

        private static IEnumerable<string> Railways(Net net)
        {
            foreach (Line l in net.Site.Area.Lines)
            {
                if (!l.IsRailway || l.Data.tunnel || l.Pts.Count < 2) continue;
                foreach (List<Vector2> piece in net.Site.Chunked ? Whole(l.Pts) : MapGeometry.ClipLine(l.Pts, net.Square))
                    Railway(net, l, piece);
                if (net.Site.StepDue) yield return "railways";
            }
        }

        private static void Railway(Net net, Line l, List<Vector2> pts)
        {
            Chain line = ChainOf(pts);
            if (line.Length < 0.5f) return;
            Look look = net.Look;
            string kind = l.Data.kind;
            bool abandoned = kind == "abandoned", disused = kind == "disused", bridge = l.Data.bridge;
            int seed = (int)(MapStyle.Hash(l.Data.id, 0x7A11) & 0x3fffffff);
            List<Sample> s = net.Samples;
            SamplesOf(line, 0f, line.Length, 1f, s);
            int n = s.Count;
            if (n < 2) return;
            FitSides(n);
            // On a carriageway (a level crossing, or a street the track runs down): set into the road.
            for (int i = 0; i < n; i++) s_WalkOk[i] = OnPavement(net, s[i].P, null, -0.3f);
            for (int i = 0; i + 1 < n; i++) s_KerbOk[i] = !(s_WalkOk[i] && s_WalkOk[i + 1]);
            float half = Gauge * 0.5f;
            if (!bridge)
            {
                // The bed: a flat top and snowy shoulders.
                for (int i = 0; i + 1 < n; i++) s_Keep[i] = s_KerbOk[i];
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < n; i++)
                    {
                        s_A[i] = side * 1.45f;
                        s_B[i] = side * 2.2f;
                    }
                    Band(net, s, s_A, s_B, BedTop, 0.004f, look.Snow, null, s_Keep);
                }
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = -1.45f;
                    s_B[i] = 1.45f;
                }
                Band(net, s, s_A, s_B, BedTop, BedTop, abandoned ? look.Snow : look.Ballast, null, s_Keep);
            }
            else
            {
                // A deck girder bridge: the track on a steel deck between two plate girders, open over whatever passes
                // beneath (at grade, the map being flat).
                Underpasses(net, line, null, l, s_Under);
                for (int i = 0; i + 1 < n; i++) s_Keep[i] = s_KerbOk[i] && !Under(s_Under, (s[i].S + s[i + 1].S) * 0.5f);
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = -1.75f;
                    s_B[i] = 1.75f;
                }
                Band(net, s, s_A, s_B, BedTop, BedTop, look.Girder, null, s_Keep);
                SamplesOf(line, 0f, line.Length, 0f, net.Samples2);
                List<Sample> c = net.Samples2;
                for (int i = 0; i + 1 < c.Count; i++)
                {
                    Outside(c[i].S, c[i + 1].S, s_Under, s_Pieces);
                    foreach (Vector2 piece in s_Pieces)
                        for (int side = -1; side <= 1; side += 2)
                        {
                            Vector2 a = GirderPoint(line, side, piece.x, c[i], c[i + 1]), b = GirderPoint(line, side, piece.y, c[i], c[i + 1]);
                            Vector3 mid = To3((a + b) * 0.5f, 0.5f);
                            Beam(net.Out.Solid(mid), a, b, 0f, 1.0f, 0.22f, look.Girder);
                            Beam(net.Out.Solid(mid), a, b, 1.0f, 0.04f, 0.3f, look.ParapetCap);
                            Vector2 d = b - a;
                            if (d.sqrMagnitude > 0.01f)
                                net.Site.Town.PropCollider(mid, new Vector3(d.magnitude, 1.0f, 0.22f), -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                        }
                }
            }
            if (!abandoned && net.Full) Ties(net, line, seed, disused);
            if (!abandoned) Rails(net, line, disused);
            if (net.Full && !abandoned) LevelCrossings(net, line, s);
            net.Site.TakeLine(pts, 2.2f);
        }

        // A girder's line at arc length sv between two samples: mitred at the samples themselves.
        private static Vector2 GirderPoint(Chain line, int side, float sv, in Sample s0, in Sample s1)
        {
            float o = side * 1.95f;
            if (Mathf.Abs(sv - s0.S) < 1e-3f) return s0.P + s0.N * o;
            if (Mathf.Abs(sv - s1.S) < 1e-3f) return s1.P + s1.N * o;
            PointOn(line, sv, out Vector2 p, out Vector2 d);
            return p + Left(d) * o;
        }

        // A fixed map's railways past the edge, into the scenery batches: the bed with its snowy shoulders, the ties and
        // the two rails' heads, flat (no colliders, no crossings), so a line doesn't stop dead at the playable edge.
        /// <summary>
        /// Generates railway geometry for map edges, adding ballast, ties, and rails to scenery batches.
        /// </summary>
        private static void SceneryRail(Net net)
        {
            Look look = net.Look;
            foreach (Line l in net.Site.Area.Lines)
            {
                if (!l.IsRailway || l.Data.tunnel || l.Pts.Count < 2) continue;
                bool abandoned = l.Data.kind == "abandoned", disused = l.Data.kind == "disused";
                foreach (List<Vector2> piece in OutsidePieces(l.Pts, net.Half))
                {
                    Chain line = ChainOf(piece);
                    if (line.Length < 0.5f) continue;
                    SamplesOf(line, 0f, line.Length, 0f, net.Samples);
                    List<Sample> s = net.Samples;
                    int n = s.Count;
                    if (n < 2) continue;
                    // Ensure sample arrays are sized for the number of points on this piece.
                    Fit(n);
                    // If this is not a bridge, generate the ballast and snow‑covered shoulders on both sides of the track.
                    if (!l.Data.bridge)
                        for (int side = -1; side <= 1; side += 2)
                        {
                            // Set side‑specific offsets for the ballast geometry.
                            for (int i = 0; i < n; i++)
                            {
                                s_A[i] = side * 1.45f;
                                s_B[i] = side * 2.2f;
                                s_YA[i] = BedTop;
                                s_YB[i] = 0.004f;
                            }
                            // Create a flat band of snow on the side.
                            FarBand(net, s, look.Snow);
                        }
                    // Core rail bed (or girder) common to both sides.
                    for (int i = 0; i < n; i++)
                    {
                        s_A[i] = l.Data.bridge ? -1.75f : -1.45f;
                        s_B[i] = -s_A[i];
                        s_YA[i] = s_YB[i] = BedTop;
                    }
                    // Choose material based on bridge/abandoned/disused state.
                    FarBand(net, s, l.Data.bridge ? look.Girder : abandoned ? look.Snow : look.Ballast);
                    // No further geometry for abandoned lines.
                    if (abandoned) continue;
                    // Place wooden ties at regular intervals along the line.
                    for (float t = 0.3f; t < line.Length; t += 0.6f)
                    {
                        PointOn(line, t, out Vector2 p, out Vector2 d);
                        Vector2 across = Left(d);
                        Vector3 a = To3(p - d * 0.12f - across * 1.3f, TieTop), b = To3(p + d * 0.12f - across * 1.3f, TieTop);
                        Vector3 c = To3(p + d * 0.12f + across * 1.3f, TieTop), e = To3(p - d * 0.12f + across * 1.3f, TieTop);
                        Face(net.Site.Town.Far.Flat(To3(p, 0f)), a, b, c, e, Vector3.up, look.Tie);
                    }
                    // Add the two rail heads; choose rusted material for disused lines.
                    for (int side = -1; side <= 1; side += 2)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            s_A[i] = side * Gauge * 0.5f - 0.036f;
                            s_B[i] = side * Gauge * 0.5f + 0.036f;
                            s_YA[i] = s_YB[i] = TieTop + 0.01f;
                        }
                        FarBand(net, s, disused ? look.RailRust : look.RailHead);
                    }
                }
            }
        }

        // Ties every 0.6 m on the open stretches, about one in three under snow (most, on a disused line).
        private static void Ties(Net net, Chain line, int seed, bool disused)
        {
            Look look = net.Look;
            for (float t = 0.3f; t < line.Length; t += 0.6f)
            {
                PointOn(line, t, out Vector2 p, out Vector2 d);
                if (OnPavement(net, p, null, -0.3f)) continue;
                Vector2 across = Left(d);
                bool snowy = Noise(p, 1.75f, seed) < (disused ? 0.7f : 0.33f);
                Vector3 a = To3(p - d * 0.12f - across * 1.3f, TieTop), b = To3(p + d * 0.12f - across * 1.3f, TieTop);
                Vector3 c = To3(p + d * 0.12f + across * 1.3f, TieTop), e = To3(p - d * 0.12f + across * 1.3f, TieTop);
                Face(net.Out.Flat(To3(p, 0f)), a, b, c, e, Vector3.up, snowy ? look.TieSnow : look.Tie);
            }
        }

        // The two rails: on the open stretches a head (polished, or rust) on a web from the ties; set flush with a dark
        // flangeway beside each where the track runs in a road, with crossing panels where it only crosses one.
        /// <summary>
        /// Adds the two rails and supporting geometry on open stretches of track.
        /// </summary>
        private static void Rails(Net net, Chain line, bool disused)
        {
            Look look = net.Look;
            List<Sample> s = net.Samples;
            int n = s.Count;
            float half = Gauge * 0.5f;
            int i = 0;
            // Walk through the samples, grouping consecutive segments that are either open (road) or closed (railway fenced).
            while (i + 1 < n)
            {
                bool open = s_KerbOk[i];
                // Find the end of the current run of the same open/closed state.
                int j = i;
                while (j + 1 < n && s_KerbOk[j] == open) j++;
                float from = s[i].S, to = s[j].S;
                // Generate a dense set of samples for this stretch.
                SamplesOf(line, from, to, 0f, net.Samples2);
                List<Sample> c = net.Samples2;
                Fit(c.Count);
                // Build geometry for each side of the track.
                for (int side = -1; side <= 1; side += 2)
                {
                    float r = side * half;
                    if (open)
                    {
                        // Open stretch: place rail heads and web ties.
                        for (int k = 0; k < c.Count; k++)
                        {
                            s_A[k] = r - 0.036f;
                            s_B[k] = r + 0.036f;
                        }
                        Band(net, c, s_A, s_B, RailTop, RailTop, disused ? look.RailRust : look.RailHead, null, null, true);
                        // Add vertical faces connecting the rail head to the tie web.
                        for (int k = 0; k + 1 < c.Count; k++)
                            for (int face = -1; face <= 1; face += 2)
                            {
                                float off = r + face * 0.036f;
                                Vector2 a = c[k].P + c[k].N * off, b = c[k + 1].P + c[k + 1].N * off;
                                Vector3 a0 = To3(a, TieTop), b0 = To3(b, TieTop), b1 = To3(b, RailTop), a1 = To3(a, RailTop);
                                Face(net.Out.Solid((a0 + b1) * 0.5f), a0, b0, b1, a1, To3(c[k].N * face, 0f), disused ? look.RailRust : look.Rail);
                            }
                    }
                    else
                    {
                        // Closed stretch: draw a simple rail‑head marker (junction) and a rubber‑like base.
                        for (int k = 0; k < c.Count; k++)
                        {
                            s_A[k] = r - 0.036f;
                            s_B[k] = r + 0.036f;
                        }
                        Band(net, c, s_A, s_B, JunctionMarkY + 0.002f, JunctionMarkY + 0.002f, look.RailHead);
                        for (int k = 0; k < c.Count; k++)
                        {
                            s_A[k] = r - side * 0.036f;
                            s_B[k] = r - side * 0.11f;
                        }
                        Band(net, c, s_A, s_B, JunctionMarkY + 0.0015f, JunctionMarkY + 0.0015f, look.Rubber);
                    }
                }
                // Very short closed stretch (under 30 m) gets a crossing panel if the full map is being built.
                if (!open && to - from < 30f && net.Full)
                {
                    for (int k = 0; k < c.Count; k++)
                    {
                        s_A[k] = -1.35f;
                        s_B[k] = 1.35f;
                    }
                    Band(net, c, s_A, s_B, JunctionMarkY, JunctionMarkY, look.Panel);
                }
                i = j;
            }
        }

// A crossbuck with flashing lights on the right of each approach to every short stretch of track set into a road.
         private static void LevelCrossings(Net net, Chain line, List<Sample> s)
         {
             int n = s.Count;
             int i = 0;
             while (i + 1 < n)
             {
                 if (s_KerbOk[i])
                 {
                     i++;
                     continue;
                 }
                 int j = i;
                 while (j + 1 < n && !s_KerbOk[j]) j++;
                 float length = s[j].S - s[i].S;
                 if (length < 30f)
                 {
                     Vector2 mid = (s[i].P + s[j].P) * 0.5f;
                     PointOn(line, (s[i].S + s[j].S) * 0.5f, out Vector2 _, out Vector2 railDir);
                     if (NearestChain(net, mid, out Chain c, out float sx)) {
                         Crossbucks(net, c, sx, railDir);
                         // Build concrete crossing panel under the crossbuck
                         CrossingPanel(net, mid, railDir);
                     }
                 }
                 i = j;
             }
         }

        /// <summary>
        /// Generates a concrete crossing stripe on the road surface for level‑crossings.
        /// </summary>
        private static void CrossingPanel(Net net, Vector2 at, Vector2 railDir)
        {
            // road surface Y‑level – use the same constant the rest of the road code uses
            const float y = RoadY;
            // orientation: the panel lies across the road, i.e. its front faces the rail direction
            float angle = Mathf.Atan2(railDir.x, railDir.y) * Mathf.Rad2Deg;
            Vector3 at3 = To3(at, y);
            RaidMesh m = net.Out.Solid(at3);
            m.SetFrame(at3, angle);
            // size: about a typical road‑crossing stripe (1.2 m wide, 0.25 m deep, 2 cm thick)
            m.Box(new Vector3(0f, 0.01f, 0f), new Vector3(1.2f, 0.02f, 0.25f), net.Look.Panel);
            m.ResetFrame();
        }


        // Place crossbucks (signs with lamps) on both sides of a short level‑crossing.
        private static void Crossbucks(Net net, Chain c, float sx, Vector2 railDir)
        {
            PointOn(c, sx, out Vector2 p, out Vector2 d);
            float sin = Mathf.Abs(Cross(d, railDir));
            float reach = Mathf.Min(14f, 2.4f / Mathf.Max(0.3f, sin)) + 3.2f;
            for (int dirn = -1; dirn <= 1; dirn += 2)
            {
                // Traffic coming along +dirn meets the crossing; its right is the chain's right when dirn is +1.
                if (c.W.OneWay && (dirn < 0) == ChainRunsWithWay(c)) continue;
                float s = sx - dirn * reach;
                if (s < 0f || s > c.Length) continue;
                PointOn(c, s, out Vector2 q, out Vector2 t);
                ProfAt(c, dirn > 0 ? -1 : 1, s, out Prof side);
                float off = side.Walk ? side.Edge + KerbWidth + 0.5f : side.Edge + 1.1f;
                Vector2 at = q + Left(t) * (-dirn * off);
                if (OnPavement(net, at, null, 0.2f) || InBuilding(net, at, 0.4f)) continue;
                Crossbuck(net, at, -t * dirn);
                net.Site.Take(at, 0.6f);
            }
        }


        // True when the chain is drawn the way its first way's points run (one‑way traffic flows along it).
        private static bool ChainRunsWithWay(Chain c) => c.Parts.Count == 0 || !c.Parts[0].Rev;
        /// <summary>
        /// Creates a crossbuck sign with flashing lights for a level‑crossing.
        /// </summary>
        // Build the crossbuck post and its flashing lamps.
        // The frame is oriented so the front of the sign faces the direction of oncoming traffic.
        // Geometry consists of a base box, a central post cylinder, two coloured panels, a horizontal bar and three lamps.
        private static void Crossbuck(Net net, Vector2 at, Vector2 facing)
        {
            Look look = net.Look;
            Vector3 at3 = To3(at, 0f);
            RaidMesh m = net.Out.Solid(at3);
            m.SetFrame(at3, Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg);
            m.Box(new Vector3(0f, 0.08f, 0f), new Vector3(0.45f, 0.16f, 0.45f), look.Parapet);
            m.Cylinder(new Vector3(0f, 0.16f, 0f), 0.055f, 4.0f, 8, look.PostGrey);
            for (int k = -1; k <= 1; k += 2)
            {
                // Two angled panels (black and white) form the familiar cross shape.
                Quaternion roll = Quaternion.Euler(0f, 0f, k * 45f);
                m.Box(new Vector3(0f, 3.65f, 0.06f), new Vector3(1.26f, 0.27f, 0.02f), roll, look.CrossbuckBlack);
                m.Box(new Vector3(0f, 3.65f, 0.075f), new Vector3(1.2f, 0.21f, 0.02f), roll, look.CrossbuckWhite);
            }
            // Horizontal bar supporting the lamps.
            m.Box(new Vector3(0f, 2.6f, 0.02f), new Vector3(1.5f, 0.08f, 0.08f), look.PostGrey);
            for (int k = -1; k <= 1; k += 2)
            {
                // Each side gets a red signal lamp.
                m.CylinderX(new Vector3(k * 0.6f, 2.6f, 0.09f), 0.24f, 0.03f, 12, look.CrossbuckBlack, 90f);
                m.CylinderX(new Vector3(k * 0.6f, 2.6f, 0.12f), 0.1f, 0.04f, 10, look.LampRed, 90f);
                m.Box(new Vector3(k * 0.6f, 2.73f, 0.19f), new Vector3(0.24f, 0.02f, 0.16f), look.LampHood);
            }
            m.ResetFrame();
            net.Site.Town.PropCollider(at3 + Vector3.up * 2.1f, new Vector3(0.3f, 4.2f, 0.3f), 0f);
        }
    }
}
