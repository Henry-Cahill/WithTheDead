using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The farmland and the wild land: Kentucky fields in January are snow striped by last year's rows (corn stubble,
    // soybean or tobacco ground) running along the field, stopping short of the fence for the headland, here and there
    // drifted over; hayfields and pasture show tufts (their fill). Marshes get clumps of dead reeds and cattails, and
    // mapped trees a well of thin, needle-strewn snow under their crowns.
    internal static partial class GroundPass
    {
        private enum Crop { Corn, Beans, Hay }

        private const float RowSpacing = 0.76f;   // 30-inch rows
        private const int MaxReedClumps = 1500;

        /// <summary>What a field grew last summer (from its id): corn half the time, then soybeans or tobacco, then hay.</summary>
        private static Crop CropOf(Owner o)
        {
            float u = MapStyle.Unit(o.Id, SaltCrop);
            return u < 0.5f ? Crop.Corn : u < 0.82f ? Crop.Beans : Crop.Hay;
        }

        private static void Fields(Ground g)
        {
            var spans = new List<float>();
            var cut = new List<float>();
            foreach (Owner o in g.Owners)
                if (o.Visible > 4f) FieldRows(g, o, g.Owners, g.Site.Town.Out, !g.Lite, spans, cut);
            for (int b = 0; b < g.Ring.Count; b++)
                foreach (Owner o in g.Ring[b])
                    if (o.Visible > 4f) FieldRows(g, o, g.Ring[b], g.Site.Town.Far, false, spans, cut);
            if (g.Lite) return;
            Marsh(g);
            TreeWells(g);
        }

        // Last year's crop rows on farmland (not on hay); the strips of leaf litter along an orchard's rows of trees.
        private static void FieldRows(Ground g, Owner o, List<Owner> owners, OutdoorBatches into, bool detailed, List<float> spans, List<float> cut)
        {
            if (o.Kind == "farmland")
            {
                Crop crop = CropOf(o);
                if (crop == Crop.Hay) return;
                Rows(g, o, owners, into, detailed, spans, cut, RowSpacing, crop == Crop.Corn ? 0.34f : 0.28f,
                    crop == Crop.Corn ? StubbleSurf : FurrowSurf, 2.5f + 3f * MapStyle.Unit(o.Id, SaltRows + 1), 52);
            }
            else if (o.Kind == "orchard")
                Rows(g, o, owners, into, detailed, spans, cut, 6.5f, 1.5f, ForestFloorSurf, 4f, 24);
        }

        #region Rows
        // An area's rows: lines along its long axis every <paramref name="spacing"/> metres, fixed to the world (so the
        // rows run on unbroken from chunk to chunk), cut to where the area's ground shows, short of its mapped edges by
        // a headland; detailed, each row is broken where the snow drifted over it (<paramref name="buried"/> in 256 of
        // its 6 m stretches).
        private static void Rows(Ground g, Owner o, List<Owner> owners, OutdoorBatches into, bool detailed, List<float> spans,
            List<float> cut, float spacing, float band, in Surf surf, float headland, int buried)
        {
            Vector2 d = o.Axis, n = new Vector2(-d.y, d.x);
            float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;   // a row's length (local +z) along the area
            float phase = MapStyle.Unit(o.Id, SaltRows) * spacing;
            float offN = Vector2.Dot(g.Offset, n), offD = Vector2.Dot(g.Offset, d);
            Rect b = o.Bounds;
            float s0 = float.MaxValue, s1 = float.MinValue;
            for (int c = 0; c < 4; c++)
            {
                var corner = new Vector2((c & 1) == 0 ? b.xMin : b.xMax, (c & 2) == 0 ? b.yMin : b.yMax);
                float s = Vector2.Dot(corner, n) + offN;
                s0 = Mathf.Min(s0, s);
                s1 = Mathf.Max(s1, s);
            }
            Rect domain = Domain(g, o.Bounds.center);
            int k0 = Mathf.CeilToInt((s0 - phase) / spacing), k1 = Mathf.FloorToInt((s1 - phase) / spacing);
            for (int k = k0; k <= k1; k++)
            {
                Vector2 origin = n * (k * spacing + phase - offN);
                Spans(o.Pts, origin, d, spans);
                for (int i = o.Rank + 1; i < owners.Count && spans.Count > 0; i++)
                {
                    Owner q = owners[i];
                    if (!q.Bounds.Overlaps(b)) continue;
                    Spans(q.Pts, origin, d, cut);
                    Subtract(spans, cut);
                }
                for (int i = 0; i + 1 < spans.Count; i += 2)
                {
                    float t0 = spans[i], t1 = spans[i + 1];
                    if (!OnEdge(origin + d * t0, domain)) t0 += headland;
                    if (!OnEdge(origin + d * t1, domain)) t1 -= headland;
                    if (t1 - t0 < 0.5f) continue;
                    if (!detailed)
                    {
                        RowPiece(into, origin, d, t0, t1, band, yaw, surf);
                        continue;
                    }
                    // Broken where drifts buried it: 6 m stretches (fixed to the world along the row), most showing.
                    const float stretch = 6f;
                    int c0 = Mathf.FloorToInt((t0 + offD) / stretch), c1 = Mathf.FloorToInt((t1 + offD) / stretch);
                    for (int c = c0; c <= c1; c++)
                    {
                        long key = ((long)k << 32) ^ (uint)c;
                        uint h = MapStyle.Hash(key, (int)(o.Id & 0x7fffffff) ^ SaltRowGap);
                        if ((h & 0xff) < buried) continue;
                        float a = c * stretch - offD + ((h >> 8) & 0xff) / 255f * 1.6f;
                        float e = (c + 1) * stretch - offD - ((h >> 16) & 0xff) / 255f * 1.6f;
                        a = Mathf.Max(a, t0);
                        e = Mathf.Min(e, t1);
                        if (e - a > 0.3f) RowPiece(into, origin, d, a, e, band * (0.75f + ((h >> 24) & 0xff) / 255f * 0.5f), yaw, surf);
                    }
                }
            }
        }

        private static void RowPiece(OutdoorBatches into, Vector2 origin, Vector2 d, float t0, float t1, float band, float yaw, in Surf surf)
        {
            Vector3 c = To3(origin + d * ((t0 + t1) * 0.5f), DecalY);
            into.Flat(c).Flat(c, band, t1 - t0, yaw, surf);
        }

        /// <summary>Takes the sorted, paired intervals of <paramref name="cut"/> out of those of <paramref name="spans"/>.</summary>
        private static void Subtract(List<float> spans, List<float> cut)
        {
            if (cut.Count == 0) return;
            var result = new List<float>(spans.Count + 4);
            for (int i = 0; i + 1 < spans.Count; i += 2)
            {
                float a = spans[i], b = spans[i + 1];
                for (int j = 0; j + 1 < cut.Count && a < b; j += 2)
                {
                    float c = cut[j], e = cut[j + 1];
                    if (e <= a || c >= b) continue;
                    if (c > a)
                    {
                        result.Add(a);
                        result.Add(c);
                    }
                    a = Mathf.Max(a, e);
                }
                if (a < b)
                {
                    result.Add(a);
                    result.Add(b);
                }
            }
            spans.Clear();
            spans.AddRange(result);
        }
        #endregion

        #region Marshes and trees
        // Clumps of reeds and cattails through the marshes, on a 6 m lattice fixed to the world.
        private static void Marsh(Ground g)
        {
            int placed = 0;
            const float cell = 6f;
            foreach (Owner o in g.Owners)
            {
                if (o.Kind != "wetland" || o.Visible < 10f) continue;
                Rect b = o.Bounds;
                Vector2 w0 = g.World(b.min), w1 = g.World(b.max);
                for (int j = Mathf.FloorToInt(w0.y / cell); j <= Mathf.FloorToInt(w1.y / cell) && placed < MaxReedClumps; j++)
                    for (int i = Mathf.FloorToInt(w0.x / cell); i <= Mathf.FloorToInt(w1.x / cell) && placed < MaxReedClumps; i++)
                    {
                        long key = ((long)i << 32) ^ (uint)j;
                        if (MapStyle.Unit(key, SaltReeds + 2) > 0.5f) continue;
                        System.Random rng = MapStyle.Rng(key, SaltReeds + 3);
                        Vector2 at = new Vector2((i + MapStyle.Range(rng, 0.1f, 0.9f)) * cell, (j + MapStyle.Range(rng, 0.1f, 0.9f)) * cell) - g.Offset;
                        if (!g.Square.Contains(at) || !g.Shows(o, at) || !g.Clear(at, 0.3f)) continue;
                        ReedClump(g.Site.Town.Out, at, rng, MapStyle.Range(rng, 0.9f, 1.5f));
                        placed++;
                    }
            }
        }

        // A well of thin, needle-strewn snow under every mapped tree that stands on open ground.
        private static void TreeWells(Ground g)
        {
            foreach (Node node in g.Site.Area.Nodes)
            {
                if (node.Data.kind != "tree" || !g.Square.Contains(node.At) || g.Blocked(node.At)) continue;
                Owner o = g.OwnerAt(node.At);
                if (o != null && (o.Tier == TierFeature || o.Kind == "water" || o.Kind == "forest")) continue;
                TreeWell(g.Site, node.At, 1.1f + 0.7f * MapStyle.Unit(node.Data.id, SaltWell));
            }
        }
        #endregion
    }
}
