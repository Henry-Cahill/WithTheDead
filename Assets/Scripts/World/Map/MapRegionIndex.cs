using System;
using System.Collections.Generic;

namespace CloneZ
{
    /// <summary>
    /// One region of the world store's build (<see cref="MapRegion"/>), sorted by the pieces (chunks) the streamed
    /// map is built in: for every chunk of the region, the streets, lines, land-use areas and points that reach into
    /// it, the buildings it owns (the chunk holding the middle of a building's outline builds it whole, as the
    /// world builder gives each building to the region its middle is in) and the buildings of its neighbours that
    /// only reach into it. Plain data, built off the main thread (see <see cref="KentuckyStreamer"/>).
    /// </summary>
    internal sealed class MapRegionIndex
    {
        /// <summary>What one chunk reads from the region: indices into the region's lists.</summary>
        internal sealed class Bucket
        {
            public readonly List<int> Streets = new List<int>();
            public readonly List<int> Lines = new List<int>();
            public readonly List<int> Owned = new List<int>();
            public readonly List<int> Foreign = new List<int>();
            public readonly List<int> Areas = new List<int>();
            public readonly List<int> Pois = new List<int>();
        }

        private static readonly Bucket s_Empty = new Bucket();

        public readonly int Rx, Rz;
        /// <summary>The side of a region in metres (8,192).</summary>
        public readonly int Size;
        /// <summary>The side of a chunk in metres; a whole number of chunks fit a region.</summary>
        public readonly int ChunkSize;
        public readonly int ChunksPerSide;
        /// <summary>The region's data, or null for a region the build doesn't have (open snow).</summary>
        public readonly MapRegion Data;
        private readonly Dictionary<int, Bucket> _buckets = new Dictionary<int, Bucket>();

        public MapRegionIndex(MapRegion data, int rx, int rz, int regionSize, int chunkSize)
        {
            Data = data;
            Rx = rx;
            Rz = rz;
            Size = regionSize;
            ChunkSize = chunkSize;
            ChunksPerSide = Math.Max(1, regionSize / chunkSize);
            if (data == null) return;
            if (data.streets != null)
                for (int i = 0; i < data.streets.Length; i++) AlongLine(data.streets[i].pts, i, b => b.Streets);
            if (data.lines != null)
                for (int i = 0; i < data.lines.Length; i++) AlongLine(data.lines[i].pts, i, b => b.Lines);
            if (data.areas != null)
                for (int i = 0; i < data.areas.Length; i++) OverBox(data.areas[i].pts, i, -1, b => b.Areas);
            if (data.buildings != null)
                for (int i = 0; i < data.buildings.Length; i++)
                {
                    int[] pts = data.buildings[i].pts;
                    if (pts == null || pts.Length < 6) continue;
                    double sx = 0, sz = 0;
                    int n = pts.Length / 2;
                    for (int k = 0; k + 1 < pts.Length; k += 2) { sx += pts[k]; sz += pts[k + 1]; }
                    int owner = Cell(ChunkOf(sx / n), ChunkOf(sz / n));
                    Get(owner).Owned.Add(i);
                    OverBox(pts, i, owner, b => b.Foreign);
                }
            if (data.pois != null)
                for (int i = 0; i < data.pois.Length; i++)
                {
                    MapPoi p = data.pois[i];
                    int limit = Size * 10;
                    if (p.x < 0 || p.z < 0 || p.x >= limit || p.z >= limit) continue;
                    Get(Cell(ChunkOf(p.x), ChunkOf(p.z))).Pois.Add(i);
                }
        }

        /// <summary>What global chunk (<paramref name="cx"/>, <paramref name="cz"/>) reads from this region.</summary>
        public Bucket Get(int cx, int cz)
        {
            int lx = cx - Rx * ChunksPerSide, lz = cz - Rz * ChunksPerSide;
            if (lx < 0 || lz < 0 || lx >= ChunksPerSide || lz >= ChunksPerSide) return s_Empty;
            return _buckets.TryGetValue(lz * ChunksPerSide + lx, out Bucket b) ? b : s_Empty;
        }

        // The region-local chunk a point (decimetres from the region's corner) falls in, kept inside the region.
        private int ChunkOf(double dm)
        {
            int c = (int)Math.Floor(dm / (ChunkSize * 10.0));
            return c < 0 ? 0 : c >= ChunksPerSide ? ChunksPerSide - 1 : c;
        }

        private static int Floor(int dm, int cellDm) => dm >= 0 ? dm / cellDm : -((-dm + cellDm - 1) / cellDm);

        private int Cell(int lx, int lz) => lz * ChunksPerSide + lx;

        private Bucket Get(int cell)
        {
            if (!_buckets.TryGetValue(cell, out Bucket b)) _buckets[cell] = b = new Bucket();
            return b;
        }

        // A line goes in the chunks each of its segments' boxes touch (a long road crossing the region doesn't land in
        // every chunk its overall box covers).
        private void AlongLine(int[] pts, int index, Func<Bucket, List<int>> list)
        {
            if (pts == null || pts.Length < 4) return;
            int cellDm = ChunkSize * 10, last = ChunksPerSide - 1;
            for (int k = 0; k + 3 < pts.Length; k += 2)
            {
                int x0 = Math.Min(pts[k], pts[k + 2]), x1 = Math.Max(pts[k], pts[k + 2]);
                int z0 = Math.Min(pts[k + 1], pts[k + 3]), z1 = Math.Max(pts[k + 1], pts[k + 3]);
                int cx0 = Floor(x0, cellDm), cx1 = Floor(x1, cellDm), cz0 = Floor(z0, cellDm), cz1 = Floor(z1, cellDm);
                if (cx1 < 0 || cz1 < 0 || cx0 > last || cz0 > last) continue;   // in the margin past the region's edge
                for (int cz = Math.Max(0, cz0); cz <= Math.Min(last, cz1); cz++)
                    for (int cx = Math.Max(0, cx0); cx <= Math.Min(last, cx1); cx++)
                    {
                        List<int> into = list(Get(Cell(cx, cz)));
                        if (into.Count == 0 || into[into.Count - 1] != index) into.Add(index);
                    }
            }
        }

        // An outline goes in every chunk its box touches (an area's inside matters, not just its edge), except
        // <paramref name="skip"/>.
        private void OverBox(int[] pts, int index, int skip, Func<Bucket, List<int>> list)
        {
            if (pts == null || pts.Length < 6) return;
            int x0 = int.MaxValue, z0 = int.MaxValue, x1 = int.MinValue, z1 = int.MinValue;
            for (int k = 0; k + 1 < pts.Length; k += 2)
            {
                x0 = Math.Min(x0, pts[k]); x1 = Math.Max(x1, pts[k]);
                z0 = Math.Min(z0, pts[k + 1]); z1 = Math.Max(z1, pts[k + 1]);
            }
            int cellDm = ChunkSize * 10, last = ChunksPerSide - 1;
            int cx0 = Floor(x0, cellDm), cx1 = Floor(x1, cellDm), cz0 = Floor(z0, cellDm), cz1 = Floor(z1, cellDm);
            if (cx1 < 0 || cz1 < 0 || cx0 > last || cz0 > last) return;
            for (int cz = Math.Max(0, cz0); cz <= Math.Min(last, cz1); cz++)
                for (int cx = Math.Max(0, cx0); cx <= Math.Min(last, cx1); cx++)
                {
                    int cell = Cell(cx, cz);
                    if (cell != skip) list(Get(cell)).Add(index);
                }
        }
    }
}
