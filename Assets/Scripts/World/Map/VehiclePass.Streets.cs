using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The cars along the kerbs, in the parking lots and out on the highways, the school buses, the cruisers and the cars
    // at the pumps.
    internal static partial class VehiclePass
    {
        private const float StallWidth = 2.7f, StallDepth = 5.4f, RowPeriod = 18f;   // parking rows: two back to back, a 7.2 m aisle
        private const int SaltKerb = 0xCA10, SaltLane = 0xCA11, SaltLot = 0xCA12, SaltWreck = 0xCA13, SaltCrash = 0xCA14, SaltBus = 0xCA15,
            SaltForecourt = 0xCA16;

        #region Kerbs
        /// <summary>
        /// Cars parallel-parked along the kerbs, on stations 6.6 m apart: nose to tail downtown and along the shopping
        /// streets (both sides where the street is wide enough), now and then along the residential streets, hardly ever out
        /// of town; never near a junction, a crossing, a hydrant or a bus stop, a bend, a driveway or on a bridge. Now and
        /// then a car was left slewed across a lane in town, its door open, and at the odd junction two never cleared up
        /// after a fender-bender.
        /// </summary>
        private static void KerbCars(Parking c)
        {
            NaturePlan plan = c.Plan;
            var bends = new List<Vector2>(16);
            foreach (Street st in c.Site.Area.Streets)
            {
                if (st.Data.tunnel || st.Data.bridge || st.Rank >= 6 || NaturePlan.FootKind(st.Data.kind)) continue;
                if (st.Rank == 0 && st.Data.kind != "living_street") continue;
                if (st.Width < 5.6f || (st.Data.kind != null && st.Data.kind.EndsWith("_link"))) continue;
                bends.Clear();
                foreach (Vector2 v in st.Pts)
                    if (!plan.CutVertex(v)) bends.Add(v);
                int only = (st.Data.id & 1) == 0 ? 1 : -1;
                plan.Stations(st.Pts, 6.6f, c.Stations);
                foreach (NaturePlan.Station s in c.Stations)
                {
                    if (!plan.InSquare(s.At, 0f) || Near(bends, s.At, 4f) || Near(c.Junctions, s.At, 11f) || Near(c.NoParking, s.At, 5f)) continue;
                    Vector2 right = new Vector2(s.Dir.y, -s.Dir.x);
                    float bizL = plan.Business(s.At - right * (st.Width * 0.5f + 9f)), bizR = plan.Business(s.At + right * (st.Width * 0.5f + 9f));
                    bool both = st.Width >= 8.4f || (Mathf.Max(bizL, bizR) > 0.06f && st.Width >= 7f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        if (!both && side != only) continue;
                        long key = NaturePlan.Combine(NaturePlan.Combine(st.Data.id, s.K), side + 3);
                        Vector2 n = right * side;
                        Vector2 front = s.At + n * (st.Width * 0.5f + 9f);
                        if (MapStyle.Unit(key, SaltKerb) >= KerbChance(st, side > 0 ? bizR : bizL, plan.Urban(front), plan, front)) continue;
                        Vector2 at = s.At + n * (st.Width * 0.5f - 1.15f + (MapStyle.Unit(key, SaltJitter) - 0.5f) * 0.25f)
                            + s.Dir * ((MapStyle.Unit(key, SaltJitter + 1) - 0.5f) * 0.8f);
                        if (plan.IsKept(at, s.Dir, 2.5f, 1.0f)) continue;
                        Vector2 heading = st.Data.oneway || side > 0 ? s.Dir : -s.Dir;
                        float yaw = Yaw(heading) + (MapStyle.Unit(key, SaltJitter + 2) - 0.5f) * 4f;
                        CarBody body = BodyFor(Where.Street, MapStyle.Unit(key, SaltBody));
                        Park(c, at, yaw, LookFor(key, body, side > 0 ? 0.5f : 0.55f), key, Quaternion.identity);
                    }
                    Abandoned(c, st, s);
                }
            }
            Fenderbenders(c);
        }

        // How likely a kerbside station has a car: shops and offices across the sidewalk fill the kerb, homes see one now
        // and then, the open road hardly ever; main roads only park in front of the shops, alleys a little behind them.
        private static float KerbChance(Street st, float business, float urban, NaturePlan plan, Vector2 front)
        {
            if (business > 0.06f) return st.Rank == 1 ? 0.18f : 0.62f;
            if (st.Rank >= 4) return 0f;
            if (urban > 0.035f) return st.Rank == 1 ? 0.04f : st.Width >= 7f ? 0.17f : 0.1f;
            return st.Rank == 1 ? 0f : 0.015f;
        }

        // One station in sixty on a town street: a car left in the lane, slewed, door hanging open (never within 30 m of
        // the square's edge, where the exits are).
        private static void Abandoned(Parking c, Street st, NaturePlan.Station s)
        {
            if (st.Rank < 2 || !c.Plan.InSquare(s.At, 30f) || c.Plan.Urban(s.At) < 0.03f) return;
            long key = NaturePlan.Combine(NaturePlan.Combine(st.Data.id, s.K), 11);
            if (MapStyle.Unit(key, SaltLane) >= 0.016f) return;
            Vector2 right = new Vector2(s.Dir.y, -s.Dir.x);
            float u = MapStyle.Unit(key, SaltLane + 1);
            Vector2 at = s.At + right * ((u - 0.5f) * Mathf.Min(2.4f, st.Width * 0.3f));
            float yaw = Yaw(u < 0.5f ? -s.Dir : s.Dir) + (MapStyle.Unit(key, SaltLane + 2) < 0.5f ? 1f : -1f) * (15f + MapStyle.Unit(key, SaltLane + 3) * 30f);
            CarWear wear = MapStyle.Unit(key, SaltWear) < 0.65f ? CarWear.DoorOpen : CarWear.Crashed | CarWear.DoorOpen;
            Park(c, at, yaw, LookFor(key, BodyFor(Where.Street, MapStyle.Unit(key, SaltBody)), 0.5f, wear), key, Quaternion.identity);
        }

        // At one town junction in a dozen two cars never moved after a fender-bender: one slewed across the box, the other
        // nosed into its side.
        private static void Fenderbenders(Parking c)
        {
            NaturePlan plan = c.Plan;
            foreach (Junction j in c.Site.Junctions)
            {
                if (j.Arms.Count < 3 || !plan.InSquare(j.At, 30f) || plan.Urban(j.At) < 0.04f) continue;
                int rank = 0;
                foreach (JunctionArm a in j.Arms) rank = Mathf.Max(rank, a.Street.Rank);
                if (rank < 3 || rank >= 6) continue;
                long key = NaturePlan.Combine((long)System.Math.Round(plan.AbsX(j.At.x)), (long)System.Math.Round(plan.AbsZ(j.At.y)));
                if (MapStyle.Unit(key, SaltCrash) >= 0.08f) continue;
                JunctionArm arm = j.Arms[MapStyle.Pick(key, SaltCrash + 1, j.Arms.Count)];
                Vector2 right = new Vector2(arm.Dir.y, -arm.Dir.x);
                Vector2 a0 = j.At + arm.Dir * 2.2f + right * 1.3f;
                float yawA = Yaw(-arm.Dir) + 20f + MapStyle.Unit(key, SaltCrash + 2) * 25f;
                long keyA = NaturePlan.Combine(key, 1), keyB = NaturePlan.Combine(key, 2);
                if (!Park(c, a0, yawA, LookFor(keyA, BodyFor(Where.Street, MapStyle.Unit(keyA, SaltBody)), 0.45f, CarWear.DoorOpen), keyA, Quaternion.identity))
                    continue;
                Vector2 side = Axis(yawA + 90f);
                Vector2 b0 = a0 + side * 3.3f + Axis(yawA) * 0.6f;
                Park(c, b0, Yaw(-side) + (MapStyle.Unit(key, SaltCrash + 3) - 0.5f) * 20f,
                    LookFor(keyB, BodyFor(Where.Street, MapStyle.Unit(keyB, SaltBody)), 0.45f, CarWear.Crashed), keyB, Quaternion.identity);
            }
        }

        private static bool Near(List<Vector2> points, Vector2 p, float within)
        {
            float w2 = within * within;
            for (int i = 0; i < points.Count; i++)
                if ((points[i] - p).sqrMagnitude < w2) return true;
            return false;
        }
        #endregion

        #region Parking lots
        /// <summary>
        /// The parking lots' cars, nosed into stalls in rows two deep between aisles: the rows run along the lot's longest
        /// side (a lot cut by a seam picks its way by its id, so both pieces agree) on the absolute lattice; the lot fills a
        /// quarter to three fifths full, fuller by the stores. The plough piles in the corners (planned first, see
        /// <see cref="SnowPass"/>) and the walls are kept clear.
        /// </summary>
        private static void LotCars(Parking c)
        {
            NaturePlan plan = c.Plan;
            SnowPass.PlanPiles(c.Site, plan);
            float half = plan.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Patch pa in c.Site.Area.Areas)
            {
                if (pa.Data.kind != "parking") continue;
                List<Vector2> poly = MapGeometry.ClipPolygon(pa.Pts, square);
                if (poly.Count < 3 || Mathf.Abs(MapGeometry.SignedArea(poly)) < 60f) continue;
                Vector2 u = LotAxis(plan, pa, poly), v = new Vector2(-u.y, u.x);
                double uMin = double.MaxValue, uMax = double.MinValue, vMin = double.MaxValue, vMax = double.MinValue;
                foreach (Vector2 q in poly)
                {
                    double ax = plan.AbsX(q.x), az = plan.AbsZ(q.y);
                    double qu = ax * u.x + az * u.y, qv = ax * v.x + az * v.y;
                    uMin = System.Math.Min(uMin, qu);
                    uMax = System.Math.Max(uMax, qu);
                    vMin = System.Math.Min(vMin, qv);
                    vMax = System.Math.Max(vMax, qv);
                }
                long id = pa.Data.id;
                float fill = 0.25f + 0.35f * MapStyle.Unit(id, SaltLot);
                long r0 = (long)System.Math.Floor(vMin / RowPeriod) - 1, r1 = (long)System.Math.Ceiling(vMax / RowPeriod) + 1;
                long j0 = (long)System.Math.Floor(uMin / StallWidth) - 1, j1 = (long)System.Math.Ceiling(uMax / StallWidth) + 1;
                for (long r = r0; r <= r1; r++)
                    for (int which = 0; which < 2; which++)
                    {
                        double rowV = r * RowPeriod + (which == 0 ? StallDepth * 0.5f : RowPeriod - StallDepth * 0.5f);
                        Vector2 nose = which == 0 ? -v : v;   // away from the aisle between the rows
                        for (long j = j0; j <= j1; j++)
                        {
                            double rowU = j * StallWidth + StallWidth * 0.5;
                            Vector2 at = plan.Local(rowU * u.x + rowV * v.x, rowU * u.y + rowV * v.y);
                            if (!plan.InSquare(at, 1f) || !Stall(poly, at, u, v)) continue;
                            long key = NaturePlan.Combine(NaturePlan.Combine(id, j), r * 2 + which);
                            float busy = fill + (plan.Business(at) > 0.04f ? 0.15f : 0f);
                            if (MapStyle.Unit(key, SaltLot + 1) >= busy) continue;
                            if (plan.WallDistance(at, 3f) < 2.6f || plan.IsKept(at, 1.6f)) continue;
                            Vector2 heading = MapStyle.Unit(key, SaltLot + 2) < 0.12f ? -nose : nose;
                            at += u * ((MapStyle.Unit(key, SaltJitter) - 0.5f) * 0.3f) + v * ((MapStyle.Unit(key, SaltJitter + 1) - 0.5f) * 0.4f);
                            float yaw = Yaw(heading) + (MapStyle.Unit(key, SaltJitter + 2) - 0.5f) * 8f;
                            Park(c, at, yaw, LookFor(key, BodyFor(Where.Street, MapStyle.Unit(key, SaltBody)), 0.55f), key, Quaternion.identity);
                        }
                    }
            }
        }

        // A stall whose four corners are inside the lot.
        private static bool Stall(List<Vector2> poly, Vector2 at, Vector2 u, Vector2 v)
        {
            Vector2 a = u * (StallWidth * 0.45f), b = v * (StallDepth * 0.45f);
            return MapGeometry.Contains(poly, at + a + b) && MapGeometry.Contains(poly, at + a - b)
                && MapGeometry.Contains(poly, at - a + b) && MapGeometry.Contains(poly, at - a - b);
        }

        /// <summary>The way a lot's rows run: along its longest side of its own; for a lot cut by the square's or a
        /// region's edge, that side's direction folded to a quarter turn and then turned or not by the lot's id, so the
        /// pieces either side of the cut agree.</summary>
        private static Vector2 LotAxis(NaturePlan plan, Patch pa, List<Vector2> poly)
        {
            float best = 0f;
            Vector2 dir = Vector2.right;
            bool cut = false;
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
                float len = Vector2.Distance(a, b);
                if (len < 0.5f) continue;
                if (plan.CutVertex(a) && plan.CutVertex(b) && (Mathf.Abs(a.x - b.x) < 0.05f || Mathf.Abs(a.y - b.y) < 0.05f))
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
                if (MapStyle.Unit(pa.Data.id, SaltLot + 3) < 0.5f) angle += 90f;
            }
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }
        #endregion

        #region The highways
        /// <summary>
        /// Out of town, along the highways and main roads, what's left of the people who tried to leave: a car on the
        /// shoulder with its door hanging open or a tyre flat, one run off into the ditch, a short jam in the lane, a burnt-out
        /// shell; one every few hundred metres, on stations 30 m apart.
        /// </summary>
        private static void RoadWrecks(Parking c)
        {
            NaturePlan plan = c.Plan;
            foreach (Street st in c.Site.Area.Streets)
            {
                if (st.Data.tunnel || st.Data.bridge || st.Rank < 4 || NaturePlan.FootKind(st.Data.kind)) continue;
                float rate = st.Rank >= 6 ? 0.06f : st.Rank == 5 ? 0.035f : 0.022f;
                plan.Stations(st.Pts, 30f, c.Stations);
                foreach (NaturePlan.Station s in c.Stations)
                {
                    if (!plan.InSquare(s.At, 2f) || plan.Urban(s.At) > 0.04f) continue;
                    long key = NaturePlan.Combine(NaturePlan.Combine(st.Data.id, s.K), 21);
                    if (MapStyle.Unit(key, SaltWreck) >= rate) continue;
                    Wreck(c, st, s, key);
                }
            }
        }

        private static void Wreck(Parking c, Street st, NaturePlan.Station s, long key)
        {
            float what = MapStyle.Unit(key, SaltWreck + 1);
            Vector2 right = new Vector2(s.Dir.y, -s.Dir.x);
            int side = st.Data.oneway || MapStyle.Unit(key, SaltWreck + 2) < 0.5f ? 1 : -1;
            Vector2 n = right * side, heading = st.Data.oneway || side > 0 ? s.Dir : -s.Dir;
            float u(int k) => MapStyle.Unit(key, SaltWreck + 3 + k);
            CarBody body = BodyFor(Where.Highway, MapStyle.Unit(key, SaltBody));
            if (what < 0.45f)
            {
                Vector2 at = s.At + n * (st.Width * 0.5f - 0.5f);
                CarWear wear = (u(0) < 0.5f ? CarWear.DoorOpen : CarWear.None) | (u(1) < 0.25f ? CarWear.Flat : CarWear.None);
                Park(c, at, Yaw(heading) + (u(2) - 0.5f) * 14f, LookFor(key, body, 0.6f, wear), key, Quaternion.identity);
            }
            else if (what < 0.72f)
            {
                Vector2 at = s.At + n * (st.Width * 0.5f + 2.2f + u(0) * 1.5f);
                float slew = side * (20f + u(1) * 45f);
                Quaternion tilt = Quaternion.Euler((u(2) - 0.5f) * 12f, 0f, (u(3) < 0.5f ? 1f : -1f) * (5f + u(4) * 6f));
                CarWear wear = CarWear.Crashed | (u(5) < 0.4f ? CarWear.DoorOpen : CarWear.None);
                Park(c, at, Yaw(heading) + slew, LookFor(key, body, 0.65f, wear), key, tilt);
            }
            else if (what < 0.88f)
            {
                int cars = 3 + MapStyle.Pick(key, SaltWreck + 9, 3);
                float back = 0f;
                for (int k = 0; k < cars; k++)
                {
                    long ck = NaturePlan.Combine(key, k);
                    CarBody b = k == 1 && u(6) < 0.25f ? CarBody.BoxTruck : BodyFor(Where.Highway, MapStyle.Unit(ck, SaltBody));
                    float len = CarKit.Size(b).z;
                    back += len * 0.5f;
                    Vector2 at = s.At - heading * back + n * (st.Width * 0.25f + (MapStyle.Unit(ck, SaltJitter) - 0.5f) * 0.6f);
                    back += len * 0.5f + 1.2f + MapStyle.Unit(ck, SaltJitter + 1) * 1.8f;
                    CarWear wear = MapStyle.Unit(ck, SaltWear) < 0.4f ? CarWear.DoorOpen : CarWear.None;
                    Park(c, at, Yaw(heading) + (MapStyle.Unit(ck, SaltJitter + 2) - 0.5f) * 12f, LookFor(ck, b, 0.6f, wear), ck, Quaternion.identity);
                }
            }
            else
            {
                Vector2 at = s.At + n * (st.Width * 0.5f - 0.3f);
                Park(c, at, Yaw(heading) + (u(0) - 0.5f) * 50f, LookFor(key, body, 0.5f, CarWear.Burnt), key, Quaternion.identity);
            }
        }
        #endregion

        #region Buses, cruisers and forecourts
        private static void Specials(Parking c)
        {
            foreach (Footprint f in c.Site.Area.Buildings)
                if ((f.Data.kind == "school" || f.Data.use == "school") && c.Plan.InSquare(f.Middle, 0f)) Buses(c, f);
            foreach (Spot s in c.Site.Area.Points)
            {
                if (!c.Plan.InSquare(s.At, 0f)) continue;
                if (s.Data.kind == "police") Cruiser(c, s);
                else if (s.Data.kind == "fuel") Forecourt(c, s);
            }
        }

        // Two to four school buses side by side, noses to the school's longest wall that has room in front of it.
        private static void Buses(Parking c, Footprint school)
        {
            NaturePlan plan = c.Plan;
            long id = school.Data.id;
            int want = 2 + MapStyle.Pick(id, SaltBus, 3);
            List<Vector2> pts = school.Pts;
            var order = new List<(float len, int i)>(pts.Count);
            for (int i = 0; i < pts.Count; i++) order.Add((Vector2.Distance(pts[i], pts[(i + 1) % pts.Count]), i));
            order.Sort((a, b) => b.len != a.len ? b.len.CompareTo(a.len) : a.i.CompareTo(b.i));
            Vector3 size = CarKit.Size(CarBody.Bus);
            foreach ((float len, int i) in order)
            {
                if (len < 3.4f * 2f) break;
                Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                Vector2 along = (b - a) / len, outward = new Vector2(along.y, -along.x);
                Vector2 mid = (a + b) * 0.5f + outward * (2.5f + size.z * 0.5f);
                for (int n = Mathf.Min(want, Mathf.FloorToInt(len / 3.4f)); n >= 2; n--)
                {
                    bool fits = true;
                    for (int k = 0; k < n && fits; k++)
                        fits = BusFits(c, school, mid + along * ((k - (n - 1) * 0.5f) * 3.4f), outward, size);
                    if (!fits) continue;
                    for (int k = 0; k < n; k++)
                    {
                        long key = NaturePlan.Combine(id, 500 + k);
                        Park(c, mid + along * ((k - (n - 1) * 0.5f) * 3.4f), Yaw(-outward) + (MapStyle.Unit(key, SaltJitter) - 0.5f) * 3f,
                            LookFor(key, CarBody.Bus, 0.6f), key, Quaternion.identity);
                    }
                    return;
                }
            }
        }

        private static bool BusFits(Parking c, Footprint school, Vector2 at, Vector2 axis, Vector3 size)
        {
            NaturePlan plan = c.Plan;
            float hl = size.z * 0.5f;
            if (!plan.InSquare(at, hl + 0.5f) || plan.IsKept(at, axis, hl, size.x * 0.5f) || c.Overlaps(at, axis, hl + 0.3f, size.x * 0.5f + 0.3f))
                return false;
            Rect box = school.Bounds;
            for (int k = -2; k <= 2; k++)
            {
                Vector2 q = at + axis * (k * hl * 0.45f);
                if (plan.WallDistance(q, 2f) < 1.4f || plan.RoadClearance(q, 2f) < 0.4f) return false;
                Land land = plan.LandAt(q);
                if (land == Land.Water || land == Land.Pitch) return false;
                bool byBox = q.x > box.xMin - 1.5f && q.x < box.xMax + 1.5f && q.y > box.yMin - 1.5f && q.y < box.yMax + 1.5f;
                if (!byBox && !c.Site.FreeBeside(q, 1.3f)) return false;
            }
            return true;
        }

        // A cruiser at the kerb in front of the police station.
        private static void Cruiser(Parking c, Spot s)
        {
            if (!c.Plan.NearestRoad(s.At, 45f, 2, out NaturePlan.RoadSeg seg, out float dist, out Vector2 foot) || dist < 0.5f) return;
            Vector2 dir = (seg.B - seg.A).normalized, toward = (s.At - foot) / dist;
            Vector2 right = new Vector2(dir.y, -dir.x);
            float side = Vector2.Dot(right, toward) >= 0f ? 1f : -1f;
            Vector2 heading = side > 0f ? dir : -dir;
            long key = NaturePlan.Combine(s.Data.id, 77);
            for (int k = 0; k < 5; k++)
            {
                float along = (k % 2 == 0 ? 1f : -1f) * ((k + 1) / 2) * 6.6f;
                Vector2 at = foot + dir * along + right * side * (seg.Half - 1.15f);
                if (Near(c.Junctions, at, 9f)) continue;
                if (Park(c, at, Yaw(heading), LookFor(key, CarBody.Cruiser, 0.35f), key, Quaternion.identity)) return;
            }
        }

        // One or two cars left on a gas station's forecourt, clear of where its pumps stand.
        private static void Forecourt(Parking c, Spot s)
        {
            NaturePlan plan = c.Plan;
            int want = 1 + MapStyle.Pick(s.Data.id, SaltForecourt, 2), placed = 0;
            for (int k = 0; k < 12 && placed < want; k++)
            {
                long key = NaturePlan.Combine(s.Data.id, 600 + k);
                float angle = (k * 30f + MapStyle.Unit(s.Data.id, SaltForecourt + 1) * 30f) * Mathf.Deg2Rad;
                Vector2 at = s.At + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (6.5f + (k % 3) * 2f);
                float yaw = MapStyle.Unit(key, SaltForecourt + 2) * 360f;
                Vector2 axis = Axis(yaw);
                if (!plan.InSquare(at, 3f) || plan.IsKept(at, axis, 2.6f, 1.0f)) continue;
                bool ok = true;
                for (int j = -1; j <= 1 && ok; j++)
                {
                    Vector2 q = at + axis * (j * 1.7f);
                    ok = plan.RoadClearance(q, 2f) > 0.6f && plan.WallDistance(q, 2f) > 1.4f && c.Site.FreeBeside(q, 1.0f);
                }
                if (!ok) continue;
                CarWear wear = MapStyle.Unit(key, SaltWear) < 0.5f ? CarWear.DoorOpen : CarWear.None;
                if (Park(c, at, yaw, LookFor(key, BodyFor(Where.Street, MapStyle.Unit(key, SaltBody)), 0.45f, wear), key, Quaternion.identity)) placed++;
            }
        }
        #endregion
    }
}
