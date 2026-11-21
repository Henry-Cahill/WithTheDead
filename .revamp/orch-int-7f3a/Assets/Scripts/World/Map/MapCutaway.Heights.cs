using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CloneZ
{
    // Each building's upper part as heights over a coarse grid in plan, read from its baked mesh the first time a scan
    // meets the building. Whether a roof stands between the camera and the survivor is then told by the roof's own shape
    // (a gable's eaves are low and its ridge high, a steeple is narrow, a porch roof low and shallow, a back wing lower
    // than the front), not by a box round its highest point, so roofs don't vanish round a survivor they don't hide.
    // Until a building's meshes are baked (a streamed piece's bake) and read, its footprint stands in, up to its top.
    internal static partial class MapCutaway
    {
        /// <summary>Metres per unit of a height cell.</summary>
        internal const float HeightStep = 0.25f;
        private const float MinHeightCell = 0.5f;
        private const int MaxHeightCells = 80;   // along a side
        /// <summary>Cells a slice of the reading touches between looks at the time (a triangle counts as 8 more).</summary>
        private const int CellsBetweenLooks = 1024;
        /// <summary>How far past its footprint a building is assumed to reach when what it covers isn't known (eaves, porch
        /// roofs, awnings, signs): the spatial hash cells it's put in.</summary>
        private const float CoverMargin = 3f;
        /// <summary>A little round what a building is known to cover (the heights grid rounds out to whole cells).</summary>
        private const float CoverSlack = 0.6f;
        /// <summary>The roof test starts this far in front of the survivor: past their own radius and half a height cell,
        /// so the building at their back never counts.</summary>
        private const float RoofFrom = 0.45f;

        /// <summary>Off: roofs are tested by the footprint up to the building's top even once read (the harness compares).</summary>
        internal static bool UseHeights = true;

        private static readonly List<Vector3> s_HV = new List<Vector3>(8192);
        private static readonly List<int> s_HI = new List<int>(16384);
        private static readonly List<int> s_HRead = new List<int>(8192);
        private static readonly List<Building> s_Rehash = new List<Building>(8);
        private static bool s_HeightWarned;

        /// <summary>Buildings whose heights were read so far, the time that took in all and the longest slice (ms), and the
        /// heights' memory (bytes).</summary>
        internal static int Resolves;
        internal static double ResolveMs, ResolveSliceMaxMs;
        internal static long HeightBytes;

        /// <summary>The heights being read, a slice at a time.</summary>
        private sealed class HeightJob
        {
            public Building B;
            public int Tri;
            public byte[] Field;
            public int W, H;
            public float X0, Z0, Cell;
            public Vector3 Shift;
        }

        private static readonly HeightJob s_Height = new HeightJob();

        /// <summary>Reads how high a building's baked walls and upper part reach (their bounds: cheap), and whether its walls
        /// reach above its ground storey's top. False while they aren't baked yet.</summary>
        private static bool Measure(Building b)
        {
            if (b.Measured) return true;
            Mesh wm = b.WallsMesh, um = b.UpperMesh;
            if (b.Walls != null && (wm == null || wm.vertexCount == 0)) return false;
            if (b.Upper != null && (um == null || um.vertexCount == 0)) return false;
            float walls = wm != null ? wm.bounds.max.y : 0f;
            b.ClipWalls = b.Groups != 0 && b.ClipTop >= MinClip && walls > b.ClipTop + 0.02f;
            // The walls the sight line is tested against are what shows of them once the storeys above are lifted.
            if (b.Groups != 0) b.WallTop = b.ClipWalls ? b.ClipTop : walls;
            b.Top = Mathf.Max(b.Top, Mathf.Max(walls, um != null ? um.bounds.max.y : 0f));
            b.Measured = true;
            if (b.Upper == null || um == null || !um.isReadable) b.Resolved = true;   // nothing to read: the footprint stands in
            if (b.Site != null) b.Site.MaxTop = Mathf.Max(b.Site.MaxTop, b.Base + b.Top);
            s_MaxTop = Mathf.Max(s_MaxTop, b.Base + b.Top);
            return true;
        }

        /// <summary>Reads every registered building's meshes now (the harness; the game reads them a slice a frame as scans
        /// meet them, or before); returns how many roofs were read.</summary>
        internal static int ResolveAll()
        {
            int n = 0;
            for (int i = 0; i < s_Sites.Count; i++)
            {
                List<Building> list = s_Sites[i].Buildings;
                for (int k = 0; k < list.Count; k++)
                    if (ResolveOne(list[k])) n++;
            }
            return n;
        }

        /// <summary>Measures building <paramref name="b"/> and reads its roof's heights now, whatever the time (the harness);
        /// true when its heights were read.</summary>
        internal static bool ResolveOne(Building b)
        {
            if (b.Site == null || !Measure(b) || b.Resolved) return false;
            if (!ReferenceEquals(s_Height.B, b))
            {
                AbortHeightJob();
                if (!StartHeightJob(b)) return false;
            }
            StepHeightJob(long.MaxValue);
            return b.Height != null;
        }

        private static void AbortHeightJob()
        {
            s_Height.B = null;
            s_Height.Field = null;
        }

        // Starts reading b's heights: false when there's nothing to read (or it isn't baked yet).
        private static bool StartHeightJob(Building b)
        {
            if (b.Site == null || b.Resolved || !Measure(b) || b.Resolved) return false;
            Mesh um = b.UpperMesh;
            long started = Stopwatch.GetTimestamp();
            try
            {
                Bounds mb = um.bounds;   // mesh space: x, z from the object (the footprint's middle), y from the base
                Vector3 o = b.Upper.transform.position;
                float x0 = Mathf.Min(o.x + mb.min.x, b.Bounds.xMin), x1 = Mathf.Max(o.x + mb.max.x, b.Bounds.xMax);
                float z0 = Mathf.Min(o.z + mb.min.z, b.Bounds.yMin), z1 = Mathf.Max(o.z + mb.max.z, b.Bounds.yMax);
                float cell = Mathf.Max(MinHeightCell, Mathf.Max(x1 - x0, z1 - z0) / MaxHeightCells);
                if (!(cell < 1e4f)) throw new System.InvalidOperationException("its bounds aren't finite");
                HeightJob job = s_Height;
                job.B = b;
                job.Tri = 0;
                job.Cell = cell;
                job.W = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / cell));
                job.H = Mathf.Max(1, Mathf.CeilToInt((z1 - z0) / cell));
                job.X0 = x0;
                job.Z0 = z0;
                job.Field = new byte[job.W * job.H];
                job.Shift = new Vector3(o.x - x0, o.y - b.Base, o.z - z0);   // mesh space to the grid's (x, z from its corner)
                s_HV.Clear();
                um.GetVertices(s_HV);
                s_HI.Clear();
                for (int sm = 0; sm < um.subMeshCount; sm++)
                {
                    if (um.GetTopology(sm) != MeshTopology.Triangles) continue;
                    s_HRead.Clear();
                    um.GetIndices(s_HRead, sm, true);
                    s_HI.AddRange(s_HRead);
                }
                return true;
            }
            catch (System.Exception e)
            {
                HeightFailed(b, e);
                return false;
            }
            finally
            {
                Took(started, false);
            }
        }

        private static void HeightFailed(Building b, System.Exception e)
        {
            AbortHeightJob();
            b.Height = null;
            b.Resolved = true;
            if (!s_HeightWarned) Debug.LogWarning("[MapCutaway] couldn't read the roof of building " + b.Id + "; its footprint stands in: " + e);
            s_HeightWarned = true;
        }

        // Runs the heights being read until they're done or `deadline` (stopwatch ticks) passes: true when done.
        private static bool StepHeightJob(long deadline)
        {
            HeightJob job = s_Height;
            Building b = job.B;
            if (b == null) return true;
            if (b.Site == null)
            {
                AbortHeightJob();
                return true;
            }
            long started = Stopwatch.GetTimestamp();
            try
            {
                byte[] field = job.Field;
                int w = job.W, h = job.H;
                float cell = job.Cell;
                Vector3 shift = job.Shift;
                // The time is looked at every so many cells touched (a roof's big faces cover hundreds each), not triangles.
                int work = 0;
                for (; job.Tri + 2 < s_HI.Count; job.Tri += 3)
                {
                    if (work >= CellsBetweenLooks)
                    {
                        work = 0;
                        if (Stopwatch.GetTimestamp() > deadline) return false;
                    }
                    work += 8 + Raster(field, w, h, cell, s_HV[s_HI[job.Tri]] + shift, s_HV[s_HI[job.Tri + 1]] + shift, s_HV[s_HI[job.Tri + 2]] + shift);
                }
                if (b.Height != null) HeightBytes -= b.Height.Length;
                b.Height = field;
                b.HX = job.X0;
                b.HZ = job.Z0;
                b.HCell = cell;
                b.HW = w;
                b.HH = h;
                HeightBytes += field.Length;
                b.Resolved = true;
                Resolves++;
                if (b.HX < b.Cover.xMin || b.HZ < b.Cover.yMin || b.HX + b.HW * cell > b.Cover.xMax || b.HZ + b.HH * cell > b.Cover.yMax)
                {
                    s_Rehash.Add(b);   // it reaches past the cells it's in
                    Rehash();
                }
                AbortHeightJob();
                return true;
            }
            catch (System.Exception e)
            {
                HeightFailed(b, e);
                return true;
            }
            finally
            {
                Took(started, deadline != long.MaxValue);
            }
        }

        private static void Took(long started, bool slice)
        {
            double ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            ResolveMs += ms;
            if (slice && ms > ResolveSliceMaxMs) ResolveSliceMaxMs = ms;
        }

        // Raises the cells under triangle a-b-c (grid space: x, z from the grid's corner, y above the base) to the
        // triangle's height over each: its highest point in that cell, or a little more, never less. Returns how many cells
        // it looked at (its cost).
        private static int Raster(byte[] field, int w, int h, float cell, Vector3 a, Vector3 b, Vector3 c)
        {
            float top = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            if (top <= 0f) return 0;
            float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z)), maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
            int i0 = Mathf.Clamp(Mathf.FloorToInt(minX / cell), 0, w - 1), i1 = Mathf.Clamp(Mathf.FloorToInt(maxX / cell), 0, w - 1);
            int j0 = Mathf.Clamp(Mathf.FloorToInt(minZ / cell), 0, h - 1), j1 = Mathf.Clamp(Mathf.FloorToInt(maxZ / cell), 0, h - 1);
            if (i0 == i1 && j0 == j1)
            {
                Raise(field, j0 * w + i0, top);
                return 1;
            }
            float area = (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);   // twice the signed area in plan
            float span = Mathf.Max(maxX - minX, maxZ - minZ);
            if (Mathf.Abs(area) <= 0.02f * span * span) return Upright(field, w, h, cell, a, b, c);
            // A sloping or level face: each cell it touches gets the face's plane at the cell's highest corner, capped at
            // the face's own top.
            float inv = 1f / area;
            float gx = ((b.y - a.y) * (c.z - a.z) - (b.z - a.z) * (c.y - a.y)) * inv;
            float gz = ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * inv;
            float half = cell * 0.5f, sign = area > 0f ? 1f : -1f;
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                {
                    float cx = (i + 0.5f) * cell, cz = (j + 0.5f) * cell;
                    if (!Touches(a, b, cx, cz, half, sign) || !Touches(b, c, cx, cz, half, sign) || !Touches(c, a, cx, cz, half, sign)) continue;
                    float hx = gx >= 0f ? cx + half : cx - half, hz = gz >= 0f ? cz + half : cz - half;
                    Raise(field, j * w + i, Mathf.Min(top, a.y + gx * (hx - a.x) + gz * (hz - a.z)));
                }
            return (i1 - i0 + 1) * (j1 - j0 + 1);
        }

        // Whether the square cell centred (cx, cz), half-size `half`, reaches the inner side of the triangle's edge p-q
        // (the triangle's plan area has the sign `sign`): with the bounding box, the separating axis test.
        private static bool Touches(Vector3 p, Vector3 q, float cx, float cz, float half, float sign)
        {
            float ex = q.x - p.x, ez = q.z - p.z;
            float e = ex * (cz - p.z) - ez * (cx - p.x);
            return sign * e + half * (Mathf.Abs(ex) + Mathf.Abs(ez)) >= 0f;
        }

        // An upright triangle (a wall, a gable end, a chimney's or a steeple's side) stands on a line in plan: along it,
        // every half cell, the cell there gets the triangle's top at that point. Returns the points looked at.
        private static int Upright(byte[] field, int w, int h, float cell, Vector3 a, Vector3 b, Vector3 c)
        {
            // The line runs between the two corners farthest apart in plan.
            Vector3 p = a, q = b;
            float best = Plan2(a, b), d = Plan2(b, c);
            if (d > best) { p = b; q = c; best = d; }
            d = Plan2(c, a);
            if (d > best) { p = c; q = a; best = d; }
            float len = Mathf.Sqrt(best);
            if (len < 1e-4f)
            {
                Raise(field, Index(a.x, a.z, w, h, cell), Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
                return 1;
            }
            float dx = (q.x - p.x) / len, dz = (q.z - p.z) / len;
            float ta = (a.x - p.x) * dx + (a.z - p.z) * dz, tb = (b.x - p.x) * dx + (b.z - p.z) * dz, tc = (c.x - p.x) * dx + (c.z - p.z) * dz;
            int n = Mathf.Max(1, Mathf.CeilToInt(len / (cell * 0.5f)));
            for (int k = 0; k <= n; k++)
            {
                float t = len * k / n;
                float y = Mathf.Max(Across(a, ta, b, tb, t), Mathf.Max(Across(b, tb, c, tc, t), Across(c, tc, a, ta, t)));
                if (y > float.MinValue) Raise(field, Index(p.x + dx * t, p.z + dz * t, w, h, cell), y);
            }
            return n + 1;
        }

        // The height where edge u-v (at u-v's positions tu, tv along the line) passes t, or MinValue when it doesn't.
        private static float Across(Vector3 u, float tu, Vector3 v, float tv, float t)
        {
            float lo = Mathf.Min(tu, tv), hi = Mathf.Max(tu, tv);
            if (t < lo - 1e-4f || t > hi + 1e-4f) return float.MinValue;
            if (hi - lo < 1e-5f) return Mathf.Max(u.y, v.y);
            return Mathf.Lerp(u.y, v.y, Mathf.Clamp01((t - tu) / (tv - tu)));
        }

        private static float Plan2(Vector3 a, Vector3 b)
        {
            float x = b.x - a.x, z = b.z - a.z;
            return x * x + z * z;
        }

        private static int Index(float x, float z, int w, int h, float cell)
            => Mathf.Clamp(Mathf.FloorToInt(z / cell), 0, h - 1) * w + Mathf.Clamp(Mathf.FloorToInt(x / cell), 0, w - 1);

        private static void Raise(byte[] field, int at, float y)
        {
            if (y <= 0f) return;
            int v = Mathf.Min(255, Mathf.CeilToInt(y / HeightStep));
            if (v > field[at]) field[at] = (byte)v;
        }

        // True when the upper part rises above the sight line from a survivor at s (plan, feet at y0) toward the camera
        // somewhere in the band `from` to `to` metres along it (`back`) and `half` either side (`side`). `lift` metres
        // less of the line's run count, so a building already cut stays cut a little longer.
        private static bool HeightHides(Building b, Vector2 s, float y0, Vector2 back, Vector2 side, float tan, float from, float to,
            float half, float lift)
        {
            byte[] field = b.Height;
            float cell = b.HCell, step = cell * 0.5f, up = y0 - b.Base;
            int lanes = Mathf.Max(1, Mathf.CeilToInt(2f * half / step));
            for (float u = from; u <= to + 1e-4f; u += step)
            {
                float line = up + Mathf.Max(0f, u - lift) * tan;   // the sight line's height above the base here
                if (line >= b.Top) return false;   // nothing of the building reaches it from here on
                int need = line <= 0f ? 0 : Mathf.Min(255, Mathf.FloorToInt(line / HeightStep));
                Vector2 m = s + back * u;
                for (int k = 0; k <= lanes; k++)
                {
                    Vector2 p = m + side * (half * (2f * k / lanes - 1f));
                    int i = Mathf.FloorToInt((p.x - b.HX) / cell), j = Mathf.FloorToInt((p.y - b.HZ) / cell);
                    if (i < 0 || j < 0 || i >= b.HW || j >= b.HH) continue;
                    if (field[j * b.HW + i] > need) return true;
                }
            }
            return false;
        }

        // Whether the building's upper part hides the survivor (not inside it): by its heights once read, else by its
        // footprint up to its top.
        private static bool RoofHides(Building b, Vector2 s, float y0, Vector2 back, Vector2 side, float tan, float reach, float far, float wide)
        {
            float top = Mathf.Min((b.Base + b.Top - y0) / tan + far, reach);
            if (b.Height != null && UseHeights)
                return top > RoofFrom && (HeightHides(b, s, y0, back, side, tan, RoofFrom, Mathf.Min(top, ViewStart), BodyHalfWidth + wide, far)
                    || (top > ViewStart && HeightHides(b, s, y0, back, side, tan, ViewStart, top, ViewHalfWidth + far, far)));
            return top > -Behind && (Crosses(b.Pts, s, back, side, -Behind, top, BodyHalfWidth + wide)
                || (top > ViewStart && Crosses(b.Pts, s, back, side, ViewStart, top, ViewHalfWidth + far)));
        }

        // Puts a building in the spatial hash cells its cover touches.
        private static void Hash(Building b)
        {
            b.X0 = Cell(b.Cover.xMin);
            b.X1 = Cell(b.Cover.xMax);
            b.Z0 = Cell(b.Cover.yMin);
            b.Z1 = Cell(b.Cover.yMax);
            for (int z = b.Z0; z <= b.Z1; z++)
                for (int x = b.X0; x <= b.X1; x++)
                {
                    long key = Key(x, z);
                    if (!s_Cells.TryGetValue(key, out List<Building> cell))
                        s_Cells[key] = cell = s_FreeCells.Count > 0 ? s_FreeCells.Pop() : new List<Building>(8);
                    cell.Add(b);
                }
        }

        private static void Unhash(Building b)
        {
            for (int z = b.Z0; z <= b.Z1; z++)
                for (int x = b.X0; x <= b.X1; x++)
                {
                    long key = Key(x, z);
                    if (!s_Cells.TryGetValue(key, out List<Building> cell)) continue;
                    cell.Remove(b);
                    if (cell.Count > 0) continue;
                    s_Cells.Remove(key);
                    s_FreeCells.Push(cell);
                }
        }

        // The buildings whose upper part turned out to reach past the cells they were in, put in the right ones.
        private static void Rehash()
        {
            for (int i = 0; i < s_Rehash.Count; i++)
            {
                Building b = s_Rehash[i];
                if (b.Site == null || b.Height == null) continue;
                Unhash(b);
                b.Cover = Rect.MinMaxRect(Mathf.Min(b.Cover.xMin, b.HX), Mathf.Min(b.Cover.yMin, b.HZ),
                    Mathf.Max(b.Cover.xMax, b.HX + b.HW * b.HCell), Mathf.Max(b.Cover.yMax, b.HZ + b.HH * b.HCell));
                Hash(b);
            }
            s_Rehash.Clear();
        }
    }
}
