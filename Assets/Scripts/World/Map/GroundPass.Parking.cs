using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Parking lots after the plow: the asphalt fill (packed snow polished by tyres in patches), the snow shoved to the
    // lot's edges in a ragged white band, and, laid out on the lot's whole outline so every chunk agrees, rows of
    // 90-degree stalls along the lot's long axis, their faded lines half buried, with a concrete wheel stop at the head
    // of most. The stalls are kept (Stalls) for the cars.
    internal static partial class GroundPass
    {
        private const float StallWidth = 2.7f, StallDepth = 5.4f, Aisle = 7.0f, LotMargin = 1.4f;
        private const float MarkY = DecalY + 0.0005f;   // paint on a lot, over its plowed edge
        private const int MaxStalls = 1500;

        private static void Parking(Ground g)
        {
            foreach (Owner o in g.Owners)
                if (o.Kind == "parking" && o.Visible > 20f) Lot(g, o, g.Square, g.Site.Town.Out, !g.Lite);
            for (int b = 0; b < g.Ring.Count; b++)
                foreach (Owner o in g.Ring[b])
                    if (o.Kind == "parking" && o.Visible > 20f) Lot(g, o, RingBand(g, b), g.Site.Town.Far, false);
        }

        private static void Lot(Ground g, Owner o, Rect domain, OutdoorBatches into, bool detailed)
        {
            PlowedEdge(g, o, domain, into);
            if (detailed) StallRows(g, o);
        }

        // The snow the plow pushed off the lot: a ragged band along its mapped edges (the road pass draws over it where
        // a street or a drive comes in).
        private static void PlowedEdge(Ground g, Owner o, Rect domain, OutdoorBatches into)
        {
            List<Vector2> p = o.Pts;
            int n = p.Count, start = -1;
            for (int k = 0; k < n && start < 0; k++)
                if (OnBorder(p[k], p[(k + 1) % n], domain)) start = k;
            bool closed = start < 0;
            float width = 1.0f + 0.8f * MapStyle.Unit(o.Id, SaltLotEdge);
            var chain = new List<Vector2>();
            var normals = new List<Vector2>();
            for (int s = 0; s <= n; s++)
            {
                int k = (start + 1 + s) % n;
                if (s < n && !OnBorder(p[k], p[(k + 1) % n], domain))
                {
                    if (chain.Count == 0) chain.Add(p[k]);
                    Densify(chain, p[(k + 1) % n], 2f);
                    continue;
                }
                if (chain.Count >= 2)
                {
                    Normals(chain, normals);
                    var inner = new float[chain.Count];
                    for (int i = 0; i < chain.Count; i++)
                        inner[i] = width * (0.5f + 0.9f * WorldNoise(g.World(chain[i]), 4f, SaltLotEdge)) * Taper(chain, i, domain, closed);
                    Strip(into, chain, normals, inner, null, DecalY, SnowSurf);
                }
                chain.Clear();
            }
        }

        // Rows of stalls across the lot's short side, on aisles along its long side, laid out on the lot's whole outline
        // (each chunk draws the stalls whose middle it holds). Odd-shaped and small lots stay plain.
        private static void StallRows(Ground g, Owner o)
        {
            List<Vector2> whole = g.Whole(o);
            if (whole == null || whole.Count < 3 || g.Stalls.Count >= MaxStalls) return;
            float area = Mathf.Abs(MapGeometry.SignedArea(whole));
            Box(whole, out Vector2 d, out Vector2 mid, out Vector2 half);
            if (area < 220f || area < 4f * half.x * half.y * 0.62f || half.y * 2f < StallDepth + Aisle + LotMargin * 2f) return;
            var n = new Vector2(-d.y, d.x);
            // Stall rows across n: double rows round each aisle, a single one when there's room left for just that.
            float usable = half.y * 2f - LotMargin * 2f;
            const float module = StallDepth * 2f + Aisle;
            int modules = Mathf.FloorToInt(usable / module);
            bool single = usable - modules * module >= StallDepth + Aisle;
            float used = modules * module + (single ? StallDepth + Aisle : 0f);
            float y = -used * 0.5f;
            var rows = new List<(float at, float face)>();
            for (int m = 0; m < modules; m++)
            {
                rows.Add((y + StallDepth * 0.5f, -1f));
                rows.Add((y + StallDepth + Aisle + StallDepth * 0.5f, 1f));
                y += module;
            }
            if (single) rows.Add((y + StallDepth * 0.5f, -1f));

            System.Random lotRng = MapStyle.Rng(o.Id, SaltStall);
            bool yellow = lotRng.NextDouble() < 0.45;
            Surf paint = DecorPalette.Matte(yellow ? new Color(0.78f, 0.67f, 0.33f) : new Color(0.80f, 0.81f, 0.78f));
            Surf stop = DecorPalette.Matte(new Color(0.56f, 0.55f, 0.52f));
            Surf cap = DecorPalette.Matte(new Color(0.84f, 0.89f, 0.97f));
            float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;   // along the lot
            int across = Mathf.FloorToInt((half.x * 2f - LotMargin * 2f) / StallWidth);
            float x0 = -across * StallWidth * 0.5f;
            for (int r = 0; r < rows.Count; r++)
            {
                (float rowAt, float face) = rows[r];
                bool lastValid = false;
                for (int j = 0; j <= across; j++)
                {
                    float xc = x0 + (j + 0.5f) * StallWidth;
                    Vector2 c = mid + d * xc + n * rowAt;
                    bool valid = j < across && StallFits(g, o, whole, c, d, n);
                    Vector2 lineAt = mid + d * (xc - StallWidth * 0.5f) + n * rowAt;
                    // A line between two stalls, or at the end of a run, drawn by the chunk holding its middle.
                    if ((valid || lastValid) && g.Square.Contains(lineAt))
                        PaintLine(g, lineAt, n, yaw, paint, MapStyle.Key(g.World(lineAt)));
                    lastValid = valid;
                    if (!valid || !g.Square.Contains(c)) continue;
                    long key = MapStyle.Key(g.World(c));
                    Vector2 nose = n * face;   // cars nose in from the aisle towards the row's back
                    float noseYaw = Mathf.Atan2(nose.x, nose.y) * Mathf.Rad2Deg;
                    g.Stalls.Add(new ParkingStall { At = c, Yaw = noseYaw, Width = StallWidth, Depth = StallDepth, Lot = o.Id });
                    if (MapStyle.Unit(key, SaltStop) < 0.6f)
                    {
                        Vector2 at = c + nose * (StallDepth * 0.5f - 0.55f);
                        Vector3 s = To3(at, 0.06f);
                        RaidMesh m = g.Site.Town.Out.Solid(s);
                        m.Box(s, new Vector3(1.8f, 0.12f, 0.16f), yaw + 90f, stop);
                        m.Box(To3(at, 0.125f), new Vector3(1.78f, 0.015f, 0.17f), yaw + 90f, cap);
                    }
                    if (g.Stalls.Count >= MaxStalls) return;
                }
            }
        }

        private static bool StallFits(Ground g, Owner o, List<Vector2> whole, Vector2 c, Vector2 d, Vector2 n)
        {
            for (int k = 0; k < 5; k++)
            {
                Vector2 q = k == 4 ? c : c + d * (((k & 1) == 0 ? -0.5f : 0.5f) * (StallWidth - 0.3f)) + n * (((k & 2) == 0 ? -0.5f : 0.5f) * (StallDepth - 0.3f));
                if (!Inside(whole, q)) return false;
                if (!g.Square.Contains(q)) continue;   // the next chunk's ground: as mapped
                if (!g.Shows(o, q) || g.Blocked(q)) return false;
            }
            return true;
        }

        // A stall line across the row, faded and partly buried under packed snow (one to three pieces showing).
        private static void PaintLine(Ground g, Vector2 at, Vector2 n, float yaw, in Surf paint, long key)
        {
            uint h = MapStyle.Hash(key, SaltLine);
            float len = StallDepth - 0.5f, from = -len * 0.5f;
            int pieces = 1 + (int)(h % 3);
            float piece = len / pieces;
            float lineYaw = yaw + 90f;   // the line's length (local +z) runs across the lot, along n
            for (int i = 0; i < pieces; i++)
            {
                uint hi = h >> (4 + i * 8);
                if ((hi & 7) < 2) continue;   // buried
                float a = from + i * piece + (hi >> 3 & 3) * 0.12f, b = from + (i + 1) * piece - (hi >> 5 & 3) * 0.15f;
                if (b - a < 0.25f) continue;
                Vector3 c = To3(at + n * ((a + b) * 0.5f), MarkY);
                g.Site.Town.Out.Flat(c).Flat(c, 0.1f, b - a, lineYaw, paint);
            }
        }
    }
}
