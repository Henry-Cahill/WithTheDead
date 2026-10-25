using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // A lot in the start square kept clear for a building of the game's own: the hub's bar, which stands in the town its
    // front door opens onto (KettleLobby). It's picked from the place's data before any piece is built, fronting a street,
    // where nothing the data draws stands (buildings, roads, fences, power lines, water and railways, graves, pitches),
    // and every piece's site marks it taken (Site.TakeLot), so nothing the passes place for themselves (trees, cars,
    // street furniture, plough piles) lands on it either.
    public static partial class KentuckyMap
    {
        /// <summary>How big a lot to keep clear: half its width, and how far it reaches in front of (towards its street,
        /// where the door is) and behind its building's middle, in metres.</summary>
        public struct LotSize
        {
            public float HalfWidth, Front, Back;
            public LotSize(float halfWidth, float front, float back) { HalfWidth = halfWidth; Front = front; Back = back; }
        }

        /// <summary>
        /// A lot kept clear in the start square, in world metres (x east, z north). Its building is laid out around its
        /// own middle with the door on its local -z side; <see cref="Yaw"/> turns it to face its street.
        /// </summary>
        public sealed class Lot
        {
            /// <summary>The building's middle, where its own plan's origin goes.</summary>
            public Vector2 Center;
            /// <summary>The building's turn about the vertical, in degrees (as a Unity rotation): its local -z faces the street.</summary>
            public float Yaw;
            public LotSize Size;
            /// <summary>The name of the street the lot fronts ("" when it has none).</summary>
            public string Street = "";

            /// <summary>A point of the building's plan (local x to the right of the door, local z back from it) in the world.</summary>
            public Vector2 ToWorld(Vector2 local)
            {
                float s = Mathf.Sin(Yaw * Mathf.Deg2Rad), c = Mathf.Cos(Yaw * Mathf.Deg2Rad);
                return Center + new Vector2(local.x * c + local.y * s, -local.x * s + local.y * c);
            }

            /// <summary>A world point in the building's plan.</summary>
            public Vector2 ToLocal(Vector2 world)
            {
                Vector2 d = world - Center;
                float s = Mathf.Sin(Yaw * Mathf.Deg2Rad), c = Mathf.Cos(Yaw * Mathf.Deg2Rad);
                return new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
            }

            /// <summary>True when a world point lies on the lot (grown by <paramref name="margin"/>).</summary>
            public bool Contains(Vector2 world, float margin = 0f)
            {
                Vector2 l = ToLocal(world);
                return l.x >= -Size.HalfWidth - margin && l.x <= Size.HalfWidth + margin && l.y >= -Size.Front - margin && l.y <= Size.Back + margin;
            }

            /// <summary>The lot's four corners in the world.</summary>
            public Vector2[] Corners() => new[]
            {
                ToWorld(new Vector2(-Size.HalfWidth, -Size.Front)), ToWorld(new Vector2(Size.HalfWidth, -Size.Front)),
                ToWorld(new Vector2(Size.HalfWidth, Size.Back)), ToWorld(new Vector2(-Size.HalfWidth, Size.Back)),
            };
        }

        /// <summary>The lot the next statewide map should keep clear near its start (asked for by the hub's bar, see
        /// <see cref="KettleLobby"/>), or null. The build that reads it clears it.</summary>
        public static LotSize? WantLot;

        /// <summary>The lot the current map keeps clear, or null (none was asked for, or none could be found).</summary>
        public static Lot ReservedLot { get; private set; }

        /// <summary>Why the last lot asked for wasn't found ("" when it was, or none was asked for).</summary>
        public static string LotWhy { get; private set; } = "";

        // Land uses that draw things of their own on the ground (graves, goals, drums and pallets, crop rows, woods) or that
        // nothing should stand on (water): a lot never overlaps them.
        private static readonly HashSet<string> s_LotBlockers = new HashSet<string>
        {
            "water", "wetland", "reservoir", "basin", "cemetery", "grave_yard", "churchyard", "pitch", "playground", "stadium",
            "track", "quarry", "industrial", "railway", "construction", "landfill", "farmland", "farmyard", "orchard",
            "vineyard", "forest", "wood", "scrub", "garden", "allotments", "golf_course",
        };

        /// <summary>Settles the start's lot from its data: the one asked for (<see cref="WantLot"/>) is picked and kept
        /// clear, or none is.</summary>
        internal static void PlanLot(Area start, float half)
        {
            ReservedLot = null;
            LotWhy = "";
            if (!WantLot.HasValue) return;
            LotSize size = WantLot.Value;
            WantLot = null;
            ReservedLot = FindLot(start, half, size);
            if (ReservedLot == null)
            {
                LotWhy = "no clear lot of " + Mathf.RoundToInt(size.HalfWidth * 2f) + " by " + Mathf.RoundToInt(size.Front + size.Back)
                    + " m fronts a street in " + start.Place;
                Debug.LogWarning("[CloneZ] " + LotWhy + ".");
                return;
            }
            Vector2 c = ReservedLot.Center;
            Debug.Log("[CloneZ] Lot kept clear at " + c.x.ToString("0.0") + ", " + c.y.ToString("0.0") + " (turned " + ReservedLot.Yaw.ToString("0")
                + "\u00b0), fronting " + (string.IsNullOrEmpty(ReservedLot.Street) ? "an unnamed street" : ReservedLot.Street) + ", "
                + c.magnitude.ToString("0") + " m from the middle of " + start.Place + ".");
        }

        /// <summary>Lets go of the lot (a map that didn't ask for one).</summary>
        internal static void ForgetLot()
        {
            ReservedLot = null;
            WantLot = null;
            LotWhy = "";
        }

        private struct LotCandidate
        {
            public Vector2 Center;
            public float Yaw, Score;
            public string Street;
        }

        /// <summary>
        /// The best lot of <paramref name="size"/> in the square: fronting a street (its door towards it, just past the
        /// kerb), clear of everything the data draws, inside the square and off its edge band, best some 40 to 90 m from
        /// the middle on a town street. Null when there's none.
        /// </summary>
        internal static Lot FindLot(Area a, float half, LotSize size)
        {
            var candidates = new List<LotCandidate>(4096);
            foreach (Street s in a.Streets)
            {
                if (s.Rank < 1 || s.Data.tunnel || s.Data.bridge) continue;
                float off = s.Width * 0.5f + 1.2f + size.Front;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 p0 = s.Pts[i], ab = s.Pts[i + 1] - p0;
                    float len = ab.magnitude;
                    if (len < 1f) continue;
                    Vector2 d = ab / len, left = new Vector2(-d.y, d.x);
                    for (float t = 1.5f; t < len; t += 3f)
                    {
                        Vector2 on = p0 + d * t;
                        for (int k = -1; k <= 1; k += 2)
                        {
                            Vector2 n = left * k;
                            Vector2 middle = on + n * off;
                            float dist = middle.magnitude;
                            if (dist < 24f || dist > half - 30f) continue;
                            // Best on a town street some 60 m out; an alley or a service road only when there's nothing better near.
                            float score = Mathf.Abs(dist - 60f) / 60f
                                + (s.Rank == 6 ? 0.35f : 0f) + (s.Rank == 1 ? 0.6f : 0f) + (s.IsTownStreet ? 0f : 0.1f)
                                - (string.IsNullOrEmpty(s.Data.name) ? 0f : 0.05f);
                            candidates.Add(new LotCandidate
                            {
                                Center = middle, Yaw = Mathf.Atan2(n.x, n.y) * Mathf.Rad2Deg, Score = score, Street = s.Data.name ?? "",
                            });
                        }
                    }
                }
            }
            candidates.Sort((x, y) => x.Score.CompareTo(y.Score));
            var lot = new Lot { Size = size };
            for (int i = 0; i < candidates.Count; i++)
            {
                lot.Center = candidates[i].Center;
                lot.Yaw = candidates[i].Yaw;
                lot.Street = candidates[i].Street;
                if (LotClear(a, lot, half)) return lot;
            }
            return null;
        }

        // True when nothing the data draws stands on the lot and it lies inside the square, off the edge band.
        private static bool LotClear(Area a, Lot lot, float half)
        {
            Vector2[] corners = lot.Corners();
            float limit = half - 14f;
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (Vector2 c in corners)
            {
                if (Mathf.Abs(c.x) > limit || Mathf.Abs(c.y) > limit) return false;
                x0 = Mathf.Min(x0, c.x); x1 = Mathf.Max(x1, c.x);
                z0 = Mathf.Min(z0, c.y); z1 = Mathf.Max(z1, c.y);
            }
            Rect box = Rect.MinMaxRect(x0, z0, x1, z1);

            foreach (Footprint f in a.Buildings)
                if (Near(f.Bounds, box, 1f) && PolygonHitsLot(f.Pts, lot, 0.8f)) return false;
            foreach (Street s in a.Streets)
                if (!s.Data.tunnel && LineHitsLot(s.Pts, lot, box, s.Width * 0.5f + 0.6f)) return false;
            foreach (Line l in a.Barriers)
                if (LineHitsLot(l.Pts, lot, box, 0.8f)) return false;
            foreach (Line l in a.Power)
                if (LineHitsLot(l.Pts, lot, box, 2f)) return false;
            foreach (Line l in a.Lines)
                if (!l.Data.tunnel && LineHitsLot(l.Pts, lot, box, Mathf.Max(3f, l.Data.width * 0.5f + 2f))) return false;
            foreach (Patch p in a.Areas)
            {
                if (p.Data == null || string.IsNullOrEmpty(p.Data.kind) || !s_LotBlockers.Contains(p.Data.kind)) continue;
                if (PolygonHitsLot(p.Pts, lot, 0.5f)) return false;
            }
            return true;
        }

        private static bool Near(Rect r, Rect box, float margin)
            => r.xMax >= box.xMin - margin && r.xMin <= box.xMax + margin && r.yMax >= box.yMin - margin && r.yMin <= box.yMax + margin;

        // A line (a street's middle, a fence, a power line) coming within <paramref name="reach"/> of the lot.
        private static bool LineHitsLot(List<Vector2> pts, Lot lot, Rect box, float reach)
        {
            if (pts == null || pts.Count == 0) return false;
            MapGeometry.Extent(pts, out float x0, out float z0, out float x1, out float z1);
            if (!Near(Rect.MinMaxRect(x0, z0, x1, z1), box, reach + 0.5f)) return false;
            Vector2 prev = lot.ToLocal(pts[0]);
            if (pts.Count == 1) return InLot(prev, lot, reach);
            for (int i = 1; i < pts.Count; i++)
            {
                Vector2 next = lot.ToLocal(pts[i]);
                if (SegmentHitsRect(prev, next, lot, reach)) return true;
                prev = next;
            }
            return false;
        }

        // A closed outline overlapping the lot (grown by <paramref name="margin"/>): an edge crossing it, or the lot inside it.
        private static bool PolygonHitsLot(List<Vector2> pts, Lot lot, float margin)
        {
            if (pts == null || pts.Count < 3) return false;
            var local = new List<Vector2>(pts.Count);
            for (int i = 0; i < pts.Count; i++) local.Add(lot.ToLocal(pts[i]));
            for (int i = 0; i < local.Count; i++)
                if (SegmentHitsRect(local[i], local[(i + 1) % local.Count], lot, margin)) return true;
            return MapGeometry.Contains(local, new Vector2(0f, (lot.Size.Back - lot.Size.Front) * 0.5f));
        }

        private static bool InLot(Vector2 local, Lot lot, float margin)
            => local.x >= -lot.Size.HalfWidth - margin && local.x <= lot.Size.HalfWidth + margin
            && local.y >= -lot.Size.Front - margin && local.y <= lot.Size.Back + margin;

        // A segment (in the lot's own coordinates) touching the lot's rectangle grown by <paramref name="margin"/>
        // (Liang-Barsky clipping).
        private static bool SegmentHitsRect(Vector2 a, Vector2 b, Lot lot, float margin)
        {
            float xMin = -lot.Size.HalfWidth - margin, xMax = lot.Size.HalfWidth + margin;
            float zMin = -lot.Size.Front - margin, zMax = lot.Size.Back + margin;
            float t0 = 0f, t1 = 1f;
            Vector2 d = b - a;
            return Clip(-d.x, a.x - xMin, ref t0, ref t1) && Clip(d.x, xMax - a.x, ref t0, ref t1)
                && Clip(-d.y, a.y - zMin, ref t0, ref t1) && Clip(d.y, zMax - a.y, ref t0, ref t1);
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Abs(p) < 1e-6f) return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }
    }
}
