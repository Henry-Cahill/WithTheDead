using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // Parking lots after the plough: the asphalt (the fill), the snow shoved off the lot in a ragged white band along its
    // edges, and the stalls: 90-degree stalls in rows along the lot's long side, laid out on its whole outline the way a
    // lot is striped (centred across it, double rows back to back between aisles of 6 to 7.2 m, a single row along one
    // curb when there's room for just that), their faded lines half buried, a concrete wheel stop at the head of most; and
    // the snow the plough couldn't reach round the parked cars lying packed in the stall rows while the aisles it ran
    // down show black. The parked cars stand in the stalls (VehiclePass parks in Stalls), and every piece of a lot a seam
    // cuts lays them out alike. Seen from afar (Lite pieces, a fixed map's scenery ring) a lot keeps the packed rows only.
    internal static partial class GroundPass
    {
        private const float StallWidth = 2.7f, StallDepth = 5.4f, AisleMin = 6.0f, AisleMax = 7.2f;
        private const float LotMargin = 1.2f;   // in from the lot's edge: the plowed snow lies there
        private const float MarkY = DecalY + 0.0005f;   // paint on a lot, over its plowed edge
        private const int MaxStalls = 1500;

        // Working lists for laying lots out (main thread only).
        private static readonly List<(float at, float face)> s_Rows = new List<(float at, float face)>(16);
        private static readonly float[] s_Along = new float[4], s_Amount = new float[4];

        /// <summary>
        /// Generates ground features for parking lots: draws the plowed edge band and, depending on detail level,
        /// creates stall rows or their packed‑snow representation.
        /// </summary>
        private static IEnumerable<string> Lots(Ground g)
        {
            bool any = false;
            // Track whether any parking lot was processed; used later to exit early if none found
            foreach (Owner o in g.Owners)
                // Iterate over owners in the main ground square; only consider visible parking lots.
                if (o.Kind == "parking" && o.Visible > 20f)
                {
                    PlowedEdge(g, o, g.Square, g.Site.Town.Out);
                    any = true;
                    // Yield a step token if the engine expects incremental processing.
                    if (g.Site.StepDue) yield return "lots";
                }
            for (int b = 0; b < g.Ring.Count; b++)
                // Process parking lots that lie in outer ring bands (farther from the player).
                foreach (Owner o in g.Ring[b])
                    if (o.Kind == "parking" && o.Visible > 20f)
                    {
                        PlowedEdge(g, o, RingBand(g, b), g.Site.Town.Far);
                        // Draw the packed‑snow representation of stall rows for distant lots.
                        PackedRows(g, o, RingBand(g, b));
                    }
            // No parking lots processed; stop further work.
            if (!any) yield break;
            foreach (Owner o in g.Owners)
            {
                if (o.Kind != "parking" || o.Visible <= 20f) continue;
                // For detailed view, generate stall rows; otherwise just packed‑snow rows.
                if (g.Lite) PackedRows(g, o, g.Square);
                else
                    foreach (string step in StallRows(g, o)) yield return step;
            }
        }

        // The snow the plow pushed off the lot: a ragged band along its mapped edges (the road pass draws over it where
        // a street or a drive comes in).
        /// <summary>
        /// Draws the ragged snow band left by the plow along the parking lot's perimeter.
        /// Handles both closed polygons (no explicit border start) and open shapes.
        /// </summary>
        private static void PlowedEdge(Ground g, Owner o, Rect domain, OutdoorBatches into)
        {
            List<Vector2> p = o.Pts;
            int n = p.Count, start = -1;
            // Find the first edge that lies on the lot's border within the domain; start will be its index.
            // If none found the polygon is considered closed.
            for (int k = 0; k < n && start < 0; k++)
                if (OnBorder(p[k], p[(k + 1) % n], domain)) start = k;
            // True when the lot's outline forms a closed loop.
            bool closed = start < 0;
            // Edge width varies with map style; SaltLotEdge adds subtle random variation.
            float width = 1.0f + 0.8f * MapStyle.Unit(o.Id, SaltLotEdge);
            var chain = new List<Vector2>();
            var normals = new List<Vector2>();
            // Iterate over each edge, starting just after the found border edge, wrapping around.
            for (int s = 0; s <= n; s++)
            {
                int k = (start + 1 + s) % n;
                // Collect consecutive border edges into a chain for processing.
                if (s < n && !OnBorder(p[k], p[(k + 1) % n], domain))
                {
                    if (chain.Count == 0) chain.Add(p[k]);
                    Densify(chain, p[(k + 1) % n], 2f);
                    continue;
                }
                // When we reach a non‑border edge (or loop end) and have a chain, compute normals and inner offsets.
                if (chain.Count >= 2)
                {
                    Normals(chain, normals);
                    var inner = new float[chain.Count];
                    for (int i = 0; i < chain.Count; i++)
                        inner[i] = width * (0.5f + 0.9f * WorldNoise(g.World(chain[i]), 4f, SaltLotEdge)) * Taper(chain, i, domain, closed);
                    // Emit the final strip geometry for this border segment.
                    Strip(into, chain, normals, inner, null, DecalY, SnowSurf);
                }
                chain.Clear();
            }
        }

        #region The stalls
        // The lot's stall rows on its whole outline (on a streamed piece, the outline as mapped in its region, so pieces
        // agree): each stall that fits inside it and, inside the square, clear of the streets, buildings and fences on
        // it, drawn by the piece holding its middle; each line between two stalls by the piece holding the line's middle.
        // Yields between rows when the step has had its time.
        private static IEnumerable<string> StallRows(Ground g, Owner o)
        {
            if (g.Stalls.Count >= MaxStalls) yield break;
            List<Vector2> outline = g.Whole(o) ?? o.Pts;
            if (outline.Count < 3 || Mathf.Abs(MapGeometry.SignedArea(outline)) < 90f) yield break;
            var rows = new List<(float at, float face)>(8);
            if (!Layout(o, outline, out Vector2 u, out Vector2 v, rows, out int across, out float x0)) yield break;

            System.Random lotRng = MapStyle.Rng(o.Id, SaltStall);
            bool yellow = lotRng.NextDouble() < 0.45;
            Surf paint = DecorPalette.Matte(yellow ? new Color(0.80f, 0.68f, 0.30f) : new Color(0.84f, 0.85f, 0.82f));
            Surf stop = DecorPalette.Matte(new Color(0.58f, 0.57f, 0.54f));
            Surf cap = DecorPalette.Matte(new Color(0.84f, 0.89f, 0.97f));
            float yaw = Mathf.Atan2(u.x, u.y) * Mathf.Rad2Deg;   // along the rows
            var fits = new bool[across];
            for (int r = 0; r < rows.Count; r++)
            {
                if (r > 0 && g.Site.StepDue) yield return "lots";
                float rowAt = rows[r].at, face = rows[r].face;
                Vector2 nose = v * face;   // cars nose in from the aisle towards the row's back
                float noseYaw = Mathf.Atan2(nose.x, nose.y) * Mathf.Rad2Deg;
                bool anyHere = false;
                for (int j = 0; j < across; j++)
                {
                    fits[j] = StallFits(g, o, outline, Middle(u, v, x0 + (j + 0.5f) * StallWidth, rowAt), u, v);
                    anyHere |= fits[j];
                }
                if (!anyHere) continue;
                for (int j = 0; j <= across; j++)
                {
                    if (j > 0 && g.Site.StepDue) yield return "lots";
                    bool here = j < across && fits[j], before = j > 0 && fits[j - 1];
                    // A line between two stalls, or at the end of a run.
                    Vector2 lineAt = Middle(u, v, x0 + j * StallWidth, rowAt);
                    if ((here || before) && g.Square.Contains(lineAt)) PaintLine(g, lineAt, v, yaw, paint, StallKey(o.Id, j, r, 1));
                    if (!here) continue;
                    Vector2 c = Middle(u, v, x0 + (j + 0.5f) * StallWidth, rowAt);
                    if (!g.Square.Contains(c)) continue;
                    long key = StallKey(o.Id, j, r, 0);
                    float snow = StallSnow(key);
                    float left = before ? (StallSnow(StallKey(o.Id, j - 1, r, 0)) + snow) * 0.5f : 0f;
                    float right = j + 1 < across && fits[j + 1] ? (StallSnow(StallKey(o.Id, j + 1, r, 0)) + snow) * 0.5f : 0f;
                    g.Stalls.Add(new ParkingStall { At = c, Yaw = noseYaw, Width = StallWidth, Depth = StallDepth, Lot = o.Id });
                    PackedSnow(g, c, u, nose, left, snow, right);
                    if (MapStyle.Unit(key, SaltStop) < 0.6f)
                    {
                        Vector2 at = c + nose * (StallDepth * 0.5f - 0.55f);
                        Vector3 s = To3(at, 0.06f);
                        RaidMesh mesh = g.Site.Town.Out.Solid(s);
                        mesh.Box(s, new Vector3(1.8f, 0.12f, 0.16f), yaw + 90f, stop);
                        mesh.Box(To3(at, 0.125f), new Vector3(1.78f, 0.015f, 0.17f), yaw + 90f, cap);
                    }
                    if (g.Stalls.Count >= MaxStalls) yield break;
                }
            }
        }

        private static Vector2 Middle(Vector2 u, Vector2 v, float along, float across) => u * along + v * across;

        // How a lot is striped, from its whole outline: the rows' way (u along them, v across, from the lot's long axis in
        // its data, alike everywhere), each row's line across the lot and which way its cars face (double rows round each
        // aisle, then a single row on its aisle if there's room left, the aisles widened from 6 m towards 7.2 m with what's
        // left over, all of it centred between the margins), and the stalls along every row (from x0, centred too).
        // False for a lot too small to stripe.
        private static bool Layout(Owner o, List<Vector2> outline, out Vector2 u, out Vector2 v, List<(float at, float face)> rows,
            out int across, out float x0)
        {
            u = o.Axis;
            v = new Vector2(-u.y, u.x);
            rows.Clear();
            across = 0;
            x0 = 0f;
            float u0 = float.MaxValue, u1 = float.MinValue, v0 = float.MaxValue, v1 = float.MinValue;
            foreach (Vector2 p in outline)
            {
                float pu = Vector2.Dot(p, u), pv = Vector2.Dot(p, v);
                u0 = Mathf.Min(u0, pu); u1 = Mathf.Max(u1, pu);
                v0 = Mathf.Min(v0, pv); v1 = Mathf.Max(v1, pv);
            }
            float usable = v1 - v0 - LotMargin * 2f;
            int modules = Mathf.FloorToInt(usable / (StallDepth * 2f + AisleMin));
            float spare = usable - modules * (StallDepth * 2f + AisleMin);
            bool single = spare >= StallDepth + AisleMin;
            if (single) spare -= StallDepth + AisleMin;
            int aisles = modules + (single ? 1 : 0);
            if (aisles == 0) return false;
            float aisle = Mathf.Min(AisleMax, AisleMin + spare / aisles), module = StallDepth * 2f + aisle;
            float used = modules * module + (single ? StallDepth + aisle : 0f);
            float y = (v0 + v1) * 0.5f - used * 0.5f;
            for (int m = 0; m < modules; m++)
            {
                rows.Add((y + StallDepth * 0.5f, -1f));   // its back on the row before's (or the curb), its aisle above
                rows.Add((y + module - StallDepth * 0.5f, 1f));
                y += module;
            }
            if (single) rows.Add((y + StallDepth * 0.5f, -1f));
            across = Mathf.FloorToInt((u1 - u0 - LotMargin * 2f) / StallWidth);
            x0 = (u0 + u1) * 0.5f - across * StallWidth * 0.5f;
            return across >= 1;
        }

        // A lot seen from afar (a Lite piece's, or in a fixed map's scenery ring): no lines, stops or stalls for the cars,
        // just the packed snow of its stall rows as the full lot has it, one band down each run of stalls whose middles
        // the domain holds (so pieces meet at a stall's edge), fading out where the run ends at the lot's edge.
        private static void PackedRows(Ground g, Owner o, Rect domain)
        {
            List<Vector2> outline = g.Whole(o) ?? o.Pts;
            if (outline.Count < 3 || Mathf.Abs(MapGeometry.SignedArea(outline)) < 90f) return;
            List<(float at, float face)> rows = s_Rows;
            if (!Layout(o, outline, out Vector2 u, out Vector2 v, rows, out int across, out float x0)) return;
            float amount = 0.38f + 0.2f * MapStyle.Unit(o.Id, SaltStall + 3);
            for (int r = 0; r < rows.Count; r++)
            {
                float rowAt = rows[r].at;
                Vector2 back = v * rows[r].face;
                int start = -1;
                for (int j = 0; j <= across; j++)
                {
                    Vector2 c = Middle(u, v, x0 + (j + 0.5f) * StallWidth, rowAt);
                    if (j < across && domain.Contains(c) && InStall(outline, c, u, v))
                    {
                        if (start < 0) start = j;
                        continue;
                    }
                    if (start < 0) continue;
                    // The run start .. j - 1: carrying on past either end (into the next piece) at full depth.
                    bool onL = start > 0 && InStall(outline, Middle(u, v, x0 + (start - 0.5f) * StallWidth, rowAt), u, v);
                    bool onR = j < across && InStall(outline, c, u, v);
                    float length = (j - start) * StallWidth, hu = StallWidth * 0.5f;
                    int n = 0;
                    s_Along[n] = 0f; s_Amount[n++] = onL ? amount : 0f;
                    s_Along[n] = Mathf.Min(hu, length * 0.5f); s_Amount[n++] = amount;
                    if (length - hu > hu + 0.01f)
                    {
                        s_Along[n] = length - hu;
                        s_Amount[n++] = amount;
                    }
                    s_Along[n] = length; s_Amount[n++] = onR ? amount : 0f;
                    PackedBand(g, Middle(u, v, x0 + start * StallWidth, rowAt), u, back, s_Along, s_Amount, n);
                    start = -1;
                }
            }
        }

        // A stall's corners inside the lot's outline (as StallFits has it, without what stands on the lot).
        private static bool InStall(List<Vector2> outline, Vector2 c, Vector2 u, Vector2 v)
        {
            Vector2 du = u * (StallWidth * 0.45f), dv = v * (StallDepth * 0.45f);
            return Inside(outline, c - du - dv) && Inside(outline, c + du - dv) && Inside(outline, c - du + dv) && Inside(outline, c + du + dv);
        }

        private static long StallKey(long lot, long j, long row, int what)
            => unchecked(((lot * 73856093L) ^ (j * 19349663L) ^ (row * 83492791L)) * 4 + what);

        // A stall whose corners (and middle) are inside the lot and, inside the square, on the lot's own ground and clear
        // of everything built later.
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

        // How much snow lies packed in a stall: thin where a car came and went, deeper where one stood through the storm.
        private static float StallSnow(long key)
        {
            float roll = MapStyle.Unit(key, SaltStall + 1), k = MapStyle.Unit(key, SaltStall + 2);
            return roll < 0.2f ? 0.1f + 0.15f * k : roll < 0.7f ? 0.32f + 0.2f * k : 0.52f + 0.22f * k;
        }

        // The snow left in a stall, drawn see-through with the snow's tone over the lines: the plough ran down the aisles,
        // so it lies from the row's back (where it meets the next row's) feathering out towards the aisle, running on from
        // stall to stall (the amounts at the stall's sides are its neighbours' means, nothing at the end of a run).
        private static void PackedSnow(Ground g, Vector2 c, Vector2 u, Vector2 back, float left, float mid, float right)
        {
            float hu = StallWidth * 0.5f;
            s_Along[0] = 0f; s_Amount[0] = left;
            s_Along[1] = hu; s_Amount[1] = mid;
            s_Along[2] = StallWidth; s_Amount[2] = right;
            PackedBand(g, c - u * hu, u, back, s_Along, s_Amount, 3);
        }

        // Packed snow down a stretch of a stall row: from the row's back feathering out to the aisle, through columns at
        // distances `along` (along u) from `from` on the row's middle line, with the amounts there.
        private static void PackedBand(Ground g, Vector2 from, Vector2 u, Vector2 back, float[] along, float[] amount, int columns)
        {
            const float feather = 1.6f;
            float hv = StallDepth * 0.5f;
            List<Vector3> verts = g.ToneVerts;
            List<Vector2> uvs = g.ToneUvs;
            int b0 = verts.Count;
            // Each column three points across the row: the back, the feather's start and the aisle's edge.
            for (int col = 0; col < columns; col++)
            {
                Vector2 at = from + u * along[col];
                verts.Add(To3(at + back * hv, ToneY));
                uvs.Add(PackedUv(amount[col]));
                verts.Add(To3(at + back * (hv - (StallDepth - feather)), ToneY));
                uvs.Add(PackedUv(amount[col] * 0.9f));
                verts.Add(To3(at - back * hv, ToneY));
                uvs.Add(PackedUv(0f));
            }
            // Corner order for ToneQuad is anticlockwise seen from above; which way round depends on the row's facing.
            bool flip = Cross2(u, back) < 0f;
            for (int col = 0; col + 1 < columns; col++)
                for (int line = 0; line < 2; line++)
                {
                    int p00 = b0 + col * 3 + line, p10 = b0 + (col + 1) * 3 + line, p11 = p10 + 1, p01 = p00 + 1;
                    if (flip) ToneQuad(g, p00, p10, p11, p01);
                    else ToneQuad(g, p00, p01, p11, p10);
                }
        }

        private static float Cross2(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        // A quad of the tone's extra geometry, its corners anticlockwise seen from above.
        private static void ToneQuad(Ground g, int a, int b, int c, int d)
        {
            List<int> t = g.ToneTris;
            t.Add(a); t.Add(d); t.Add(c);   // clockwise seen from above: the face looks up
            t.Add(a); t.Add(c); t.Add(b);
        }
        #endregion
    }
}
