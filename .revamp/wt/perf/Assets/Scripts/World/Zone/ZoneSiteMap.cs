using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>What lies on one metre of a raid map, as far as the zone overlays care.</summary>
    [Flags]
    internal enum SiteCell : byte
    {
        None = 0,
        /// <summary>A building's footprint (plus a margin for its walk and steps).</summary>
        Building = 1,
        /// <summary>Roads, sidewalks, lots, drives and walks: anything paved over the snow.</summary>
        Paved = 2,
        /// <summary>Something solid stands here (a wreck, a dumpster, a barrier, a tree).</summary>
        Solid = 4,
        /// <summary>Kept clear for play: exits, the survivor's start.</summary>
        Reserved = 8,
        /// <summary>Claimed by a structure or clearing the overlays placed.</summary>
        Taken = 16,
        /// <summary>A gas cloud or other hazard.</summary>
        Hazard = 32,
        /// <summary>The band along the map edge (fences, the cordon).</summary>
        Edge = 64,
    }

    /// <summary>
    /// A one-metre occupancy grid of a freshly built raid map, read back from the world itself (the town's own
    /// keep-out list dies with its builder): building roots, every paved ground decal, every solid collider, the
    /// exits and the survivor's start. The zone overlays ask it for clear ground and mark what they take, so the
    /// woods, the cordon and the set pieces never land on a street, in a house or on each other. Everything it
    /// reads is deterministic for the raid seed, so every peer finds the same spots.
    /// </summary>
    internal sealed class ZoneSiteMap
    {
        public const float CellSize = 1f;

        /// <summary>Never build over these.</summary>
        public const SiteCell Hard = SiteCell.Building | SiteCell.Solid | SiteCell.Reserved | SiteCell.Taken | SiteCell.Hazard;
        /// <summary>Open, natural ground only (no paving, not the edge band).</summary>
        public const SiteCell NotOpen = Hard | SiteCell.Paved | SiteCell.Edge;

        /// <summary>Playable half extent of the map.</summary>
        public readonly float Half;
        private readonly float _min;
        private readonly int _n;
        private readonly SiteCell[] _cells;

        public ZoneSiteMap(float half)
        {
            Half = Mathf.Max(10f, half);
            float ext = Half + 2f;
            _min = -ext;
            _n = Mathf.CeilToInt(ext * 2f / CellSize);
            _cells = new SiteCell[_n * _n];
        }

        #region Cells
        private int Ix(float x) => Mathf.Clamp(Mathf.FloorToInt((x - _min) / CellSize), 0, _n - 1);

        /// <summary>The flags of the cell holding <paramref name="p"/>.</summary>
        public SiteCell At(Vector3 p) => _cells[Ix(p.z) * _n + Ix(p.x)];

        /// <summary>Sets <paramref name="flag"/> on every cell an XZ rect overlaps.</summary>
        public void Mark(Rect r, SiteCell flag)
        {
            int x0 = Ix(r.xMin), x1 = Ix(r.xMax - 0.001f), z0 = Ix(r.yMin), z1 = Ix(r.yMax - 0.001f);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++) _cells[z * _n + x] |= flag;
        }

        /// <summary>Sets <paramref name="flag"/> on every cell whose centre lies within <paramref name="radius"/> of <paramref name="c"/>.</summary>
        public void MarkCircle(Vector3 c, float radius, SiteCell flag)
        {
            float r2 = radius * radius;
            int x0 = Ix(c.x - radius), x1 = Ix(c.x + radius), z0 = Ix(c.z - radius), z1 = Ix(c.z + radius);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float cx = _min + (x + 0.5f) * CellSize - c.x, cz = _min + (z + 0.5f) * CellSize - c.z;
                    if (cx * cx + cz * cz <= r2) _cells[z * _n + x] |= flag;
                }
        }

        /// <summary>True when any cell an XZ rect overlaps carries a flag in <paramref name="mask"/> (or the rect leaves the map).</summary>
        public bool Any(Rect r, SiteCell mask)
        {
            if (r.xMin < -Half || r.xMax > Half || r.yMin < -Half || r.yMax > Half) return true;
            int x0 = Ix(r.xMin), x1 = Ix(r.xMax - 0.001f), z0 = Ix(r.yMin), z1 = Ix(r.yMax - 0.001f);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                    if ((_cells[z * _n + x] & mask) != 0) return true;
            return false;
        }

        /// <summary>True when any cell within <paramref name="radius"/> of <paramref name="c"/> carries a flag in
        /// <paramref name="mask"/> (or the circle leaves the map).</summary>
        public bool Any(Vector3 c, float radius, SiteCell mask)
        {
            if (Mathf.Abs(c.x) + radius > Half || Mathf.Abs(c.z) + radius > Half) return true;
            float r2 = radius * radius;
            int x0 = Ix(c.x - radius), x1 = Ix(c.x + radius), z0 = Ix(c.z - radius), z1 = Ix(c.z + radius);
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float cx = _min + (x + 0.5f) * CellSize - c.x, cz = _min + (z + 0.5f) * CellSize - c.z;
                    if (cx * cx + cz * cz <= r2 && (_cells[z * _n + x] & mask) != 0) return true;
                }
            return false;
        }

        /// <summary>The share (0..1) of cells within <paramref name="radius"/> carrying a flag in <paramref name="mask"/>.</summary>
        public float Coverage(Vector3 c, float radius, SiteCell mask)
        {
            float r2 = radius * radius;
            int x0 = Ix(c.x - radius), x1 = Ix(c.x + radius), z0 = Ix(c.z - radius), z1 = Ix(c.z + radius);
            int hit = 0, all = 0;
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float cx = _min + (x + 0.5f) * CellSize - c.x, cz = _min + (z + 0.5f) * CellSize - c.z;
                    if (cx * cx + cz * cz > r2) continue;
                    all++;
                    if ((_cells[z * _n + x] & mask) != 0) hit++;
                }
            return all > 0 ? (float)hit / all : 0f;
        }
        #endregion

        #region Placement
        /// <summary>An XZ rect of the given full size centred on a point, turned a quarter at a time with <paramref name="yaw"/>.</summary>
        public static Rect Footprint(Vector3 c, Vector2 size, float yaw)
        {
            bool swap = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 45f || Mathf.Abs(Mathf.DeltaAngle(yaw, 270f)) < 45f;
            float hx = (swap ? size.y : size.x) * 0.5f, hz = (swap ? size.x : size.y) * 0.5f;
            return Rect.MinMaxRect(c.x - hx, c.z - hz, c.x + hx, c.z + hz);
        }

        public static Rect Grow(Rect r, float m) => Rect.MinMaxRect(r.xMin - m, r.yMin - m, r.xMax + m, r.yMax + m);

        /// <summary>
        /// Looks for a clear rect of <paramref name="size"/> (with <paramref name="margin"/> round it) free of every flag
        /// in <paramref name="mask"/>, at least <paramref name="edge"/> metres inside the map edge. Random candidates are
        /// scored by <paramref name="score"/> (higher is better; negative rejects) and the best of the first few valid
        /// ones wins. When none of the <paramref name="tries"/> lands clear (a crowded town leaves only a few lots
        /// open), every placement on a one-metre lattice is tried instead, so a spot that exists is always found.
        /// </summary>
        public bool TryFind(System.Random rng, Vector2 size, float yaw, float margin, SiteCell mask, float edge,
            Func<Vector3, float> score, int tries, out Vector3 found)
        {
            found = Vector3.zero;
            float best = float.NegativeInfinity;
            int valid = 0;
            float reach = Half - edge - Mathf.Max(size.x, size.y) * 0.5f;
            if (reach <= 1f) return false;
            for (int i = 0; i < tries && valid < 14; i++)
            {
                var p = new Vector3(Lerp(rng, -reach, reach), 0f, Lerp(rng, -reach, reach));
                Rect r = Grow(Footprint(p, size, yaw), margin);
                if (Any(r, mask)) continue;
                float s = score != null ? score(p) : (float)rng.NextDouble();
                if (s < 0f) continue;
                valid++;
                if (s > best)
                {
                    best = s;
                    found = p;
                }
            }
            return valid > 0 || Scan(rng, size, yaw, margin, mask, reach, score, out found);
        }

        // The exhaustive pass behind TryFind: a summed-area table of the masked cells makes each rect test four lookups.
        private bool Scan(System.Random rng, Vector2 size, float yaw, float margin, SiteCell mask, float reach,
            Func<Vector3, float> score, out Vector3 found)
        {
            found = Vector3.zero;
            int w = _n + 1;
            var sat = new int[w * w];
            for (int z = 0; z < _n; z++)
                for (int x = 0; x < _n; x++)
                    sat[(z + 1) * w + x + 1] = ((_cells[z * _n + x] & mask) != 0 ? 1 : 0)
                        + sat[z * w + x + 1] + sat[(z + 1) * w + x] - sat[z * w + x];

            float best = float.NegativeInfinity;
            bool any = false;
            int steps = Mathf.FloorToInt(reach * 2f / CellSize);
            for (int iz = 0; iz <= steps; iz++)
                for (int ix = 0; ix <= steps; ix++)
                {
                    var p = new Vector3(-reach + ix * CellSize, 0f, -reach + iz * CellSize);
                    Rect r = Grow(Footprint(p, size, yaw), margin);
                    if (r.xMin < -Half || r.xMax > Half || r.yMin < -Half || r.yMax > Half) continue;
                    int x0 = Ix(r.xMin), x1 = Ix(r.xMax - 0.001f), z0 = Ix(r.yMin), z1 = Ix(r.yMax - 0.001f);
                    if (sat[(z1 + 1) * w + x1 + 1] - sat[z0 * w + x1 + 1] - sat[(z1 + 1) * w + x0] + sat[z0 * w + x0] != 0) continue;
                    float s = score != null ? score(p) : (float)rng.NextDouble();
                    if (s < 0f || s <= best) continue;
                    best = s;
                    found = p;
                    any = true;
                }
            return any;
        }

        /// <summary>Marks a structure's footprint as taken.</summary>
        public void Claim(Vector3 c, Vector2 size, float yaw, float margin = 0f) => Mark(Grow(Footprint(c, size, yaw), margin), SiteCell.Taken);

        private static float Lerp(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        #endregion

        #region Reading the built world
        /// <summary>Reads a freshly built raid map back into a grid (see the class summary).</summary>
        public static ZoneSiteMap Analyse(Transform world, RaidMapInfo map)
        {
            var m = new ZoneSiteMap(map != null ? map.HalfSize : CloneZConfig.ArenaHalfSize);
            const float edgeBand = 2.5f;
            float h = m.Half;
            m.Mark(Rect.MinMaxRect(-h - 2f, -h - 2f, -h + edgeBand, h + 2f), SiteCell.Edge);
            m.Mark(Rect.MinMaxRect(h - edgeBand, -h - 2f, h + 2f, h + 2f), SiteCell.Edge);
            m.Mark(Rect.MinMaxRect(-h - 2f, -h - 2f, h + 2f, -h + edgeBand), SiteCell.Edge);
            m.Mark(Rect.MinMaxRect(-h - 2f, h - edgeBand, h + 2f, h + 2f), SiteCell.Edge);
            if (world == null) return m;

            Physics.SyncTransforms();

            // Buildings: every "Bldg_" root under the world, from the bounds of everything it holds.
            foreach (Transform child in world)
            {
                if (child == null || !child.name.StartsWith("Bldg_", StringComparison.Ordinal)) continue;
                if (TryBounds(child, out Bounds b)) m.Mark(Grow(ToRect(b), 1.5f), SiteCell.Building);
            }

            // Paving: the upward faces of the flat ground meshes standing a little proud of the snow. The outdoor
            // batches carry the odd standing scrap with the paving (a bin bag, a bottle), so faces are judged one at a
            // time; a mesh is passed over whole only when it lies above the paving band or is too small to be paving.
            var filters = new List<MeshFilter>(64);
            world.GetComponentsInChildren(true, filters);
            for (int i = 0; i < filters.Count; i++)
            {
                MeshFilter mf = filters[i];
                if (mf == null || mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) continue;
                Bounds rb = mr.bounds;
                if (rb.min.y > PaveTop || (rb.size.x < PaveMinSpan && rb.size.z < PaveMinSpan)) continue;
                RasterizeFlat(m, mf);
            }

            // Solids: every collider standing up off the ground.
            var colliders = new List<Collider>(512);
            world.GetComponentsInChildren(true, colliders);
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider c = colliders[i];
                if (c == null || !c.enabled || c.isTrigger) continue;
                Bounds b = c.bounds;
                if (b.max.y < 0.12f) continue;
                if (b.size.x > 60f && b.size.z > 60f) continue;   // the ground slab
                m.Mark(Grow(ToRect(b), 0.4f), SiteCell.Solid);
            }

            if (map != null)
            {
                foreach (RaidExit e in map.Exits) m.MarkCircle(e.Position, e.Radius + 5f, SiteCell.Reserved);
                m.MarkCircle(map.PlayerSpawn, 5f, SiteCell.Reserved);
            }
            for (int i = 0; i < ToxicZone.All.Count; i++)
            {
                ToxicZone z = ToxicZone.All[i];
                if (z != null) m.MarkCircle(z.transform.position, z.Radius + 1.5f, SiteCell.Hazard);
            }
            return m;
        }

        private static bool TryBounds(Transform root, out Bounds b)
        {
            b = default;
            bool any = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.isTrigger) continue;
                if (!any) { b = c.bounds; any = true; }
                else b.Encapsulate(c.bounds);
            }
            return any;
        }

        private static Rect ToRect(Bounds b) => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);

        // Marks the cells under each upward-facing triangle in the paving band: above the bare snow (snow patches sit
        // lower), below anything standing (a bag's top, a step). Scraps (litter, leaves, slush) are no paving; paving
        // comes in long pieces.
        private static void RasterizeFlat(ZoneSiteMap m, MeshFilter mf)
        {
            Mesh mesh = mf.sharedMesh;
            if (!mesh.isReadable) return;
            Transform tr = mf.transform;
            Vector3[] v = TransformVertices(tr, mesh.vertices);
            int[] t = mesh.triangles;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                if (a.y < PaveBottom || b.y < PaveBottom || c.y < PaveBottom) continue;
                if (a.y > PaveTop || b.y > PaveTop || c.y > PaveTop) continue;
                Vector3 n = Vector3.Cross(b - a, c - a);
                float len = n.magnitude;
                if (len < 1e-6f || Mathf.Abs(n.y / len) < 0.7f) continue;
                float x0 = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), x1 = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
                float z0 = Mathf.Min(a.z, Mathf.Min(b.z, c.z)), z1 = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
                if (x1 - x0 < PaveMinSpan && z1 - z0 < PaveMinSpan) continue;
                m.Mark(Rect.MinMaxRect(x0, z0, x1, z1), SiteCell.Paved);
            }
        }

        private const float PaveBottom = 0.008f, PaveTop = 0.3f, PaveMinSpan = 1f;

        // Returns a new array with each vertex transformed to world space; the original mesh stays untouched.
        private static Vector3[] TransformVertices(Transform tr, Vector3[] src)
        {
            var dst = new Vector3[src.Length];
            for (int i = 0; i < src.Length; i++) dst[i] = tr.TransformPoint(src[i]);
            return dst;
        }
        #endregion
    }
}
