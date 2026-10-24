using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Parking lots after the plough: the asphalt (the fill), the snow shoved off the lot in a ragged white band along its
    // edges, and the stalls: 90-degree stalls in rows, their faded lines half buried, a concrete wheel stop at the head
    // of most, and the snow the plough couldn't reach between the parked cars lying packed in the stall rows while the
    // aisles it ran down show black. The stalls lie on the parked cars' lattice (VehiclePass.LotCars: on the state's
    // grid, rows along the lot's long side, two 5.4 m stalls back to back every 18 m round a 7.2 m aisle, stalls 2.7 m
    // wide), so the cars stand in them and every piece of a lot that a seam cuts lays them out alike. They're kept
    // (Stalls) for anyone else who parks.
    internal static partial class GroundPass
    {
        private const float StallWidth = 2.7f, StallDepth = 5.4f, RowPeriod = 18f;
        private const int SaltCarLot = 0xCA12 + 3;   // VehiclePass's SaltLot + 3: which way a cut lot's rows run
        private const float MarkY = DecalY + 0.0005f;   // paint on a lot, over its plowed edge
        private const int MaxStalls = 1500;

        private static readonly Dictionary<string, (double x, double z, bool found)> s_PlaceGrid
            = new Dictionary<string, (double, double, bool)>();

        private static void Lots(Ground g)
        {
            bool any = false;
            foreach (Owner o in g.Owners)
                if (o.Kind == "parking" && o.Visible > 20f)
                {
                    PlowedEdge(g, o, g.Square, g.Site.Town.Out);
                    any = true;
                }
            for (int b = 0; b < g.Ring.Count; b++)
                foreach (Owner o in g.Ring[b])
                    if (o.Kind == "parking" && o.Visible > 20f) PlowedEdge(g, o, RingBand(g, b), g.Site.Town.Far);
            if (!any || g.Lite) return;
            GridOrigin(g.Site, out double ox, out double oz);
            foreach (Owner o in g.Owners)
                if (o.Kind == "parking" && o.Visible > 20f) StallRows(g, o, ox, oz);
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

        #region The stalls
        // The lot's stall rows on the cars' lattice: each stall that fits inside the lot (on a streamed piece, inside the
        // lot's whole outline, so pieces agree) and clear of the streets, buildings and fences on it, drawn by the piece
        // holding its middle; each line between two stalls by the piece holding the line's middle.
        private static void StallRows(Ground g, Owner o, double ox, double oz)
        {
            List<Vector2> poly = o.Pts;   // the lot as the square cuts it: what the cars' lattice is laid on
            if (g.Stalls.Count >= MaxStalls || Mathf.Abs(MapGeometry.SignedArea(poly)) < 60f) return;
            List<Vector2> outline = g.Site.Chunked ? g.Whole(o) ?? poly : poly;
            Vector2 u = CarLotAxis(g, o.Patch, poly, ox, oz), v = new Vector2(-u.y, u.x);
            // The lattice cells that can matter: the lot's bounds cut to the square (and a stall round it), in the
            // lattice's frame on the state's grid.
            Rect b = MapGeometry.Bounds(outline);
            float m = StallDepth;
            Rect r = Rect.MinMaxRect(Mathf.Max(b.xMin, -g.Half - m), Mathf.Max(b.yMin, -g.Half - m), Mathf.Min(b.xMax, g.Half + m), Mathf.Min(b.yMax, g.Half + m));
            if (r.width <= 0f || r.height <= 0f) return;
            double uMin = double.MaxValue, uMax = double.MinValue, vMin = double.MaxValue, vMax = double.MinValue;
            for (int c = 0; c < 4; c++)
            {
                double ax = ox + ((c & 1) == 0 ? r.xMin : r.xMax), az = oz + ((c & 2) == 0 ? r.yMin : r.yMax);
                double qu = ax * u.x + az * u.y, qv = ax * v.x + az * v.y;
                uMin = System.Math.Min(uMin, qu);
                uMax = System.Math.Max(uMax, qu);
                vMin = System.Math.Min(vMin, qv);
                vMax = System.Math.Max(vMax, qv);
            }
            long r0 = (long)System.Math.Floor(vMin / RowPeriod) - 1, r1 = (long)System.Math.Ceiling(vMax / RowPeriod) + 1;
            long j0 = (long)System.Math.Floor(uMin / StallWidth) - 1, j1 = (long)System.Math.Ceiling(uMax / StallWidth) + 1;

            System.Random lotRng = MapStyle.Rng(o.Id, SaltStall);
            bool yellow = lotRng.NextDouble() < 0.45;
            Surf paint = DecorPalette.Matte(yellow ? new Color(0.80f, 0.68f, 0.30f) : new Color(0.84f, 0.85f, 0.82f));
            Surf stop = DecorPalette.Matte(new Color(0.58f, 0.57f, 0.54f));
            Surf cap = DecorPalette.Matte(new Color(0.84f, 0.89f, 0.97f));
            float yaw = Mathf.Atan2(u.x, u.y) * Mathf.Rad2Deg;   // along the rows
            for (long row = r0; row <= r1; row++)
                for (int which = 0; which < 2; which++)
                {
                    double rowV = row * RowPeriod + (which == 0 ? StallDepth * 0.5 : RowPeriod - StallDepth * 0.5);
                    Vector2 nose = which == 0 ? -v : v;   // cars nose in from the aisle towards the row's back
                    float noseYaw = Mathf.Atan2(nose.x, nose.y) * Mathf.Rad2Deg;
                    bool last = false;
                    for (long j = j0; j <= j1 + 1; j++)
                    {
                        double rowU = j * StallWidth + StallWidth * 0.5;
                        Vector2 c = Local(rowU * u.x + rowV * v.x, rowU * u.y + rowV * v.y, ox, oz);
                        bool fits = j <= j1 && StallFits(g, o, outline, c, u, v);
                        Vector2 lineAt = c - u * (StallWidth * 0.5f);
                        // A line between two stalls, or at the end of a run.
                        if ((fits || last) && g.Square.Contains(lineAt))
                            PaintLine(g, lineAt, v, yaw, paint, StallKey(o.Id, j, row * 2 + which, 1));
                        last = fits;
                        if (!fits || !g.Square.Contains(c)) continue;
                        long key = StallKey(o.Id, j, row * 2 + which, 0);
                        g.Stalls.Add(new ParkingStall { At = c, Yaw = noseYaw, Width = StallWidth, Depth = StallDepth, Lot = o.Id });
                        PackedSnow(g, c, u, v, nose, key);
                        if (MapStyle.Unit(key, SaltStop) < 0.6f)
                        {
                            Vector2 at = c + nose * (StallDepth * 0.5f - 0.55f);
                            Vector3 s = To3(at, 0.06f);
                            RaidMesh mesh = g.Site.Town.Out.Solid(s);
                            mesh.Box(s, new Vector3(1.8f, 0.12f, 0.16f), yaw + 90f, stop);
                            mesh.Box(To3(at, 0.125f), new Vector3(1.78f, 0.015f, 0.17f), yaw + 90f, cap);
                        }
                        if (g.Stalls.Count >= MaxStalls) return;
                    }
                }
        }

        private static long StallKey(long lot, long j, long row, int what)
            => unchecked(((lot * 73856093L) ^ (j * 19349663L) ^ (row * 83492791L)) * 4 + what);

        private static Vector2 Local(double ax, double az, double ox, double oz) => new Vector2((float)(ax - ox), (float)(az - oz));

        // A stall whose corners (and middle) are inside the lot and, inside the square, on the lot's own ground and clear
        // of everything built later. The corners as the cars' lattice tests them (VehiclePass.Stall).
        private static bool StallFits(Ground g, Owner o, List<Vector2> outline, Vector2 c, Vector2 u, Vector2 v)
        {
            Vector2 du = u * (StallWidth * 0.45f), dv = v * (StallDepth * 0.45f);
            for (int k = 0; k < 5; k++)
            {
                Vector2 q = k == 4 ? c : c + ((k & 1) == 0 ? -du : du) + ((k & 2) == 0 ? -dv : dv);
                if (!Inside(outline, q)) return false;
                if (!g.Square.Contains(q)) continue;   // the next piece's ground: as mapped
                if (!g.Shows(o, q) || g.Blocked(q)) return false;
            }
            return true;
        }

        // A stall line across the row, faded and here and there buried under packed snow (one or two pieces showing).
        private static void PaintLine(Ground g, Vector2 at, Vector2 across, float yaw, in Surf paint, long key)
        {
            uint h = MapStyle.Hash(key, SaltLine);
            float len = StallDepth - 0.5f, from = -len * 0.5f;
            int pieces = 1 + (int)(h % 2);
            float piece = len / pieces;
            float lineYaw = yaw + 90f;   // the line's length (local +z) runs across the rows, along the stall's depth
            for (int i = 0; i < pieces; i++)
            {
                uint hi = h >> (4 + i * 8);
                if ((hi & 7) == 0) continue;   // buried
                float a = from + i * piece + (hi >> 3 & 3) * 0.1f, b = from + (i + 1) * piece - (hi >> 5 & 3) * 0.12f;
                if (b - a < 0.25f) continue;
                Vector3 c = To3(at + across * ((a + b) * 0.5f), MarkY);
                g.Site.Town.Out.Flat(c).Flat(c, 0.1f, b - a, lineYaw, paint);
            }
        }

        // The snow left in a stall: packed thin where a car came and went, deeper where one stood through the storm,
        // feathered towards the aisle; drawn see-through with the snow's tone, over the lines.
        private static void PackedSnow(Ground g, Vector2 c, Vector2 u, Vector2 v, Vector2 nose, long key)
        {
            float roll = MapStyle.Unit(key, SaltStall + 1);
            float amount = roll < 0.18f ? MapStyle.Range(MapStyle.Rng(key, SaltStall + 2), 0.08f, 0.25f)
                : roll < 0.7f ? MapStyle.Range(MapStyle.Rng(key, SaltStall + 2), 0.38f, 0.6f)
                : MapStyle.Range(MapStyle.Rng(key, SaltStall + 2), 0.62f, 0.85f);
            float hu = StallWidth * 0.5f, hv = StallDepth * 0.5f;
            // Inner edges: in a little from the stall's sides, less at its back (the next row's snow meets it), more
            // towards the aisle the plough ran down.
            float side = 0.3f, back = 0.15f, aisle = 0.7f;
            bool backIsPlus = Vector2.Dot(nose, v) > 0f;
            float vLo = -hv + (backIsPlus ? aisle : back), vHi = hv - (backIsPlus ? back : aisle);
            var outer = new Vector2[4];
            var inner = new Vector2[4];
            for (int k = 0; k < 4; k++)
            {
                float su = k == 0 || k == 3 ? -1f : 1f, sv = k < 2 ? -1f : 1f;   // anticlockwise: (-,-) (+,-) (+,+) (-,+)
                outer[k] = c + u * (su * hu) + v * (sv * hv);
                inner[k] = c + u * (su * (hu - side)) + v * (sv < 0f ? vLo : vHi);
            }
            List<Vector3> verts = g.ToneVerts;
            List<Vector2> uvs = g.ToneUvs;
            int b0 = verts.Count;
            for (int k = 0; k < 4; k++)
            {
                verts.Add(To3(outer[k], ToneY));
                uvs.Add(PackedUv(0f));
            }
            for (int k = 0; k < 4; k++)
            {
                verts.Add(To3(inner[k], ToneY));
                uvs.Add(PackedUv(amount));
            }
            ToneQuad(g, b0 + 4, b0 + 5, b0 + 6, b0 + 7);
            for (int k = 0; k < 4; k++)
            {
                int k1 = (k + 1) & 3;
                ToneQuad(g, b0 + k, b0 + k1, b0 + 4 + k1, b0 + 4 + k);
            }
        }

        // A quad of the tone's extra geometry, its corners anticlockwise seen from above.
        private static void ToneQuad(Ground g, int a, int b, int c, int d)
        {
            List<int> t = g.ToneTris;
            t.Add(a); t.Add(d); t.Add(c);   // clockwise seen from above: the face looks up
            t.Add(a); t.Add(c); t.Add(b);
        }
        #endregion

        #region The cars' lattice
        // Where the site's middle is on the state's grid (EPSG:3088 metres), as the nature passes have it: its place's
        // grid point plus the site's offset from it.
        private static void GridOrigin(Site site, out double x, out double z)
        {
            x = site.WorldOffset.x;
            z = site.WorldOffset.z;
            if (site.Build == null || string.IsNullOrEmpty(site.Area.Place)) return;
            string key = site.Build.Id + "|" + site.Area.Place;
            if (!s_PlaceGrid.TryGetValue(key, out (double x, double z, bool found) p))
            {
                MapPlace place = null;
                try
                {
                    place = site.Build.FindPlace(site.Area.Place);
                }
                catch (System.Exception)
                {
                    place = null;
                }
                p = place != null ? (place.x, place.z, true) : (0d, 0d, false);
                s_PlaceGrid[key] = p;
            }
            if (!p.found) return;
            x += p.x;
            z += p.z;
        }

        // The way a lot's stall rows run, as the cars have it (VehiclePass.LotAxis): along the longest side of the lot as
        // the square cuts it; for a lot the square's or a region's edge cuts, that side's direction folded to a quarter
        // turn and turned or not by the lot's id, so the pieces on either side of the cut agree.
        private static Vector2 CarLotAxis(Ground g, Patch pa, List<Vector2> poly, double ox, double oz)
        {
            float best = 0f;
            Vector2 dir = Vector2.right;
            bool cut = false;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
                float len = Vector2.Distance(a, b);
                if (len < 0.5f) continue;
                if (CutVertex(g, a, ox, oz) && CutVertex(g, b, ox, oz) && (Mathf.Abs(a.x - b.x) < 0.05f || Mathf.Abs(a.y - b.y) < 0.05f))
                {
                    cut = true;
                    continue;
                }
                if (len <= best) continue;
                best = len;
                dir = (b - a) / len;
            }
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (cut)
            {
                angle = Mathf.Repeat(angle, 90f);
                if (MapStyle.Unit(pa.Data.id, SaltCarLot) < 0.5f) angle += 90f;
            }
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        private static bool CutVertex(Ground g, Vector2 p, double ox, double oz)
        {
            float half = g.Half;
            if (Mathf.Abs(Mathf.Abs(p.x) - half) < 0.02f || Mathf.Abs(Mathf.Abs(p.y) - half) < 0.02f) return true;
            float reach = half + SceneryReach;
            if (!g.Site.Chunked && (Mathf.Abs(Mathf.Abs(p.x) - reach) < 0.02f || Mathf.Abs(Mathf.Abs(p.y) - reach) < 0.02f)) return true;
            int size = g.Site.Build != null ? g.Site.Build.RegionSize : 8192;
            double rx = (ox + p.x) / size, rz = (oz + p.y) / size;
            return System.Math.Abs(rx - System.Math.Round(rx)) * size < 0.02 || System.Math.Abs(rz - System.Math.Round(rz)) * size < 0.02;
        }
        #endregion
    }
}
