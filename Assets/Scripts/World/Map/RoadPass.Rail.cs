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
                // A deck girder bridge: the track on a steel deck between two plate girders.
                for (int i = 0; i + 1 < n; i++) s_Keep[i] = s_KerbOk[i];
                for (int i = 0; i < n; i++)
                {
                    s_A[i] = -1.75f;
                    s_B[i] = 1.75f;
                }
                Band(net, s, s_A, s_B, BedTop, BedTop, look.Girder, null, s_Keep);
                SamplesOf(line, 0f, line.Length, 0f, net.Samples2);
                List<Sample> c = net.Samples2;
                for (int i = 0; i + 1 < c.Count; i++)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 a = c[i].P + c[i].N * (side * 1.95f), b = c[i + 1].P + c[i + 1].N * (side * 1.95f);
                        Vector3 mid = To3((a + b) * 0.5f, 0.5f);
                        Beam(net.Out.Solid(mid), a, b, 0f, 1.0f, 0.22f, look.Girder);
                        Beam(net.Out.Solid(mid), a, b, 1.0f, 0.04f, 0.3f, look.ParapetCap);
                        Vector2 d = b - a;
                        if (d.sqrMagnitude > 0.01f)
                            net.Site.Town.PropCollider(mid, new Vector3(d.magnitude, 1.0f, 0.22f), -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    }
            }
            if (!abandoned && net.Full) Ties(net, line, seed, disused);
            if (!abandoned) Rails(net, line, disused);
            if (net.Full && !abandoned) LevelCrossings(net, line, s);
            net.Site.TakeLine(pts, 2.2f);
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
        private static void Rails(Net net, Chain line, bool disused)
        {
            Look look = net.Look;
            List<Sample> s = net.Samples;
            int n = s.Count;
            float half = Gauge * 0.5f;
            int i = 0;
            while (i + 1 < n)
            {
                bool open = s_KerbOk[i];
                int j = i;
                while (j + 1 < n && s_KerbOk[j] == open) j++;
                float from = s[i].S, to = s[j].S;
                SamplesOf(line, from, to, 0f, net.Samples2);
                List<Sample> c = net.Samples2;
                Fit(c.Count);
                for (int side = -1; side <= 1; side += 2)
                {
                    float r = side * half;
                    if (open)
                    {
                        for (int k = 0; k < c.Count; k++)
                        {
                            s_A[k] = r - 0.036f;
                            s_B[k] = r + 0.036f;
                        }
                        Band(net, c, s_A, s_B, RailTop, RailTop, disused ? look.RailRust : look.RailHead, null, null, true);
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
                    if (NearestChain(net, mid, out Chain c, out float sx)) Crossbucks(net, c, sx, railDir);
                }
                i = j;
            }
        }

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

        // True when the chain is drawn the way its first way's points run (one-way traffic flows along it).
        private static bool ChainRunsWithWay(Chain c) => c.Parts.Count == 0 || !c.Parts[0].Rev;

        // A white crossbuck on a grey mast, two red lamps on a crossarm below it, facing <paramref name="facing"/>.
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
                Quaternion roll = Quaternion.Euler(0f, 0f, k * 45f);
                m.Box(new Vector3(0f, 3.65f, 0.06f), new Vector3(1.26f, 0.27f, 0.02f), roll, look.CrossbuckBlack);
                m.Box(new Vector3(0f, 3.65f, 0.075f), new Vector3(1.2f, 0.21f, 0.02f), roll, look.CrossbuckWhite);
            }
            m.Box(new Vector3(0f, 2.6f, 0.02f), new Vector3(1.5f, 0.08f, 0.08f), look.PostGrey);
            for (int k = -1; k <= 1; k += 2)
            {
                m.CylinderX(new Vector3(k * 0.6f, 2.6f, 0.09f), 0.24f, 0.03f, 12, look.CrossbuckBlack, 90f);
                m.CylinderX(new Vector3(k * 0.6f, 2.6f, 0.12f), 0.1f, 0.04f, 10, look.LampRed, 90f);
                m.Box(new Vector3(k * 0.6f, 2.73f, 0.19f), new Vector3(0.24f, 0.02f, 0.16f), look.LampHood);
            }
            m.ResetFrame();
            net.Site.Town.PropCollider(at3 + Vector3.up * 2.1f, new Vector3(0.3f, 4.2f, 0.3f), 0f);
        }
    }
}
