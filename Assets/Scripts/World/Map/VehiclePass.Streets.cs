using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The cars along the kerbs, in the parking lots and out on the highways, the school buses, the cruisers, the cars on
    // the forecourts and the odd pair left where one ran into the other.
    internal static partial class VehiclePass
    {
        private const int SaltKerb = 0xCA10, SaltLane = 0xCA11, SaltLot = 0xCA12, SaltWreck = 0xCA13, SaltCrash = 0xCA14, SaltBus = 0xCA15,
            SaltForecourt = 0xCA16;

        #region Kerbs
        /// <summary>
        /// Cars parallel-parked along the kerbs as the roads drew them (<see cref="RoadPass.EdgeOffset"/>), on stations 6.6 m
        /// apart: nose to tail downtown and along the shopping streets (both sides where the street is wide enough), now and
        /// then along the residential streets, hardly ever out of town; never round a junction (past its paving and
        /// crosswalks), at a crossing, a hydrant or a bus stop, on a bend, across a driveway's apron or on a bridge. Now
        /// and then a car was left slewed across a lane in town, its door open. Found first, then parked, thinned evenly if
        /// there are more than the site has room for.
        /// </summary>
        private static IEnumerable<string> KerbCars(Parking c)
        {
            NaturePlan plan = c.Plan;
            var bends = new List<Vector2>(16);
            List<Candidate> found = c.Found;
            found.Clear();
            foreach (Street st in c.Site.Area.Streets)
            {
                if (st.Data.tunnel || st.Data.bridge || st.Rank >= 6 || NaturePlan.FootKind(st.Data.kind) || !RoadPass.IsCarriageway(st)) continue;
                if (st.Rank == 0 && st.Data.kind != "living_street") continue;
                if (st.Width < 5.6f || (st.Data.kind != null && st.Data.kind.EndsWith("_link"))) continue;
                // Real bends only: the vertices where the street turns by more than 12 degrees.
                bends.Clear();
                for (int i = 1; i + 1 < st.Pts.Count; i++)
                {
                    Vector2 d0 = st.Pts[i] - st.Pts[i - 1], d1 = st.Pts[i + 1] - st.Pts[i];
                    if (d0.sqrMagnitude > 1e-4f && d1.sqrMagnitude > 1e-4f && Vector2.Dot(d0.normalized, d1.normalized) < 0.978f) bends.Add(st.Pts[i]);
                }
                int only = (st.Data.id & 1) == 0 ? 1 : -1;
                plan.Stations(st.Pts, 6.6f, c.Stations);
                foreach (NaturePlan.Station s in c.Stations)
                {
                    if (!plan.InSquare(s.At, 0f) || Near(bends, s.At, 4f) || c.NoParkingAt(s.At)) continue;
                    Vector2 right = new Vector2(s.Dir.y, -s.Dir.x);
                    float bizL = plan.Business(s.At - right * (st.Width * 0.5f + 9f)), bizR = plan.Business(s.At + right * (st.Width * 0.5f + 9f));
                    bool both = st.Width >= 8.4f || (Mathf.Max(bizL, bizR) > 0.06f && st.Width >= 7f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        if (!both && side != only) continue;
                        long key = NaturePlan.Combine(NaturePlan.Combine(st.Data.id, s.K), side + 3);
                        Vector2 n = right * side;
                        Vector2 front = s.At + n * (st.Width * 0.5f + 9f);
                        if (MapStyle.Unit(key, SaltKerb) >= KerbChance(st, side > 0 ? bizR : bizL, plan.Urban(front))) continue;
                        // The kerb as the roads drew it (their sides count from the street's left).
                        float edge = RoadPass.EdgeOffset(st, -side);
                        Vector2 at = s.At + n * (edge - 1.15f + (MapStyle.Unit(key, SaltJitter) - 0.5f) * 0.25f)
                            + s.Dir * ((MapStyle.Unit(key, SaltJitter + 1) - 0.5f) * 0.8f);
                        if (edge < 2.6f || plan.IsKept(at, s.Dir, 2.5f, 1.0f)) continue;
                        Vector2 heading = st.Data.oneway || side > 0 ? s.Dir : -s.Dir;
                        found.Add(new Candidate
                        {
                            At = at, Yaw = Yaw(heading) + (MapStyle.Unit(key, SaltJitter + 2) - 0.5f) * 4f, Key = key,
                            Body = BodyFor(Where.Street, MapStyle.Unit(key, SaltBody)), Snow = side > 0 ? 0.5f : 0.55f,
                        });
                    }
                    Abandoned(c, st, s);
                }
                if (c.Site.StepDue) yield return "kerbside cars";
            }
            foreach (string step in ParkFound(c, c.Max - c.Count - 8, "kerbside cars")) yield return step;
            yield return "kerbside cars";
        }

        // How likely a kerbside station has a car: shops and offices across the sidewalk fill the kerb, homes see one now
        // and then, the open road hardly ever; main roads only park in front of the shops, alleys a little behind them.
        private static float KerbChance(Street st, float business, float urban)
        {
            if (business > 0.06f) return st.Rank == 1 ? 0.12f : 0.6f;
            if (st.Rank >= 4) return business > 0.03f ? 0.22f : 0f;
            if (urban > 0.035f) return st.Rank == 1 ? 0.04f : st.Width >= 7f ? 0.18f : 0.11f;
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
            c.Found.Add(new Candidate { At = at, Yaw = yaw, Key = key, Body = BodyFor(Where.Street, MapStyle.Unit(key, SaltBody)), Snow = 0.5f, Wear = wear });
        }

        // At one town crossroads in a dozen, a pair never cleared up after one ran into the back of the other at the stop
        // line: the first stopped in its lane short of the crosswalk, door open, the second nosed into its tail, slewed.
        private static void Crashes(Parking c)
        {
            NaturePlan plan = c.Plan;
            foreach (Junction j in c.Crossroads)
            {
                if (!plan.InSquare(j.At, 30f) || plan.Urban(j.At) < 0.04f) continue;
                int rank = 0;
                foreach (JunctionArm a in j.Arms) rank = Mathf.Max(rank, a.Street.Rank);
                if (rank < 3 || rank >= 6) continue;
                long key = NaturePlan.Combine((long)System.Math.Round(plan.AbsX(j.At.x)), (long)System.Math.Round(plan.AbsZ(j.At.y)));
                if (MapStyle.Unit(key, SaltCrash) >= 0.08f) continue;
                int arm = MapStyle.Pick(key, SaltCrash + 1, j.Arms.Count);
                JunctionArm into = j.Arms[arm];
                if (NaturePlan.FootKind(into.Street.Data.kind) || into.Street.Rank < 2) continue;
                if (!RoadPass.StopLine(j, arm, out Vector2 stop, out float _)) continue;
                long keyA = NaturePlan.Combine(key, 1), keyB = NaturePlan.Combine(key, 2);
                CarBody a0 = BodyFor(Where.Street, MapStyle.Unit(keyA, SaltBody)), b0 = BodyFor(Where.Street, MapStyle.Unit(keyB, SaltBody));
                float lenA = CarKit.Size(a0).z, lenB = CarKit.Size(b0).z;
                Vector2 atA = stop + into.Dir * (0.8f + lenA * 0.5f);
                float yawA = Yaw(-into.Dir) + (MapStyle.Unit(key, SaltCrash + 2) - 0.5f) * 8f;
                if (!Park(c, atA, yawA, LookFor(keyA, a0, 0.45f, CarWear.DoorOpen), keyA, Quaternion.identity)) continue;
                Vector2 side = new Vector2(-into.Dir.y, into.Dir.x) * ((MapStyle.Unit(key, SaltCrash + 3) - 0.5f) * 0.8f);
                Vector2 atB = atA + into.Dir * ((lenA + lenB) * 0.5f + 0.15f) + side;
                Park(c, atB, Yaw(-into.Dir) + (MapStyle.Unit(key, SaltCrash + 4) < 0.5f ? 1f : -1f) * (8f + MapStyle.Unit(key, SaltCrash + 5) * 14f),
                    LookFor(keyB, b0, 0.45f, CarWear.Crashed | (MapStyle.Unit(keyB, SaltWear) < 0.5f ? CarWear.DoorOpen : CarWear.None)), keyB, Quaternion.identity);
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
        /// The parking lots' cars, nosed into the stalls the ground pass painted (<see cref="GroundPass.Stalls"/>), a quarter
        /// to three fifths of them taken, more by the stores, one in eight backed in; none where the plough heaped its snow
        /// (planned first, see <see cref="SnowPass"/>). The small and odd-shaped lots the ground leaves unmarked get a few
        /// cars nosed up to their longest edges, when the whole lot is the site's. Thinned evenly to the site's room.
        /// </summary>
        private static IEnumerable<string> LotCars(Parking c)
        {
            NaturePlan plan = c.Plan;
            SnowPass.PlanPiles(c.Site, plan);
            List<Candidate> found = c.Found;
            found.Clear();
            IReadOnlyList<GroundPass.ParkingStall> stalls = GroundPass.Stalls(c.Site);
            var marked = new HashSet<long>();
            for (int i = 0; i < stalls.Count; i++)
            {
                GroundPass.ParkingStall s = stalls[i];
                marked.Add(s.Lot);
                long key = NaturePlan.Combine((long)System.Math.Round(plan.AbsX(s.At.x) * 10.0), (long)System.Math.Round(plan.AbsZ(s.At.y) * 10.0));
                if (MapStyle.Unit(key, SaltLot + 1) >= LotFill(plan, s.Lot, s.At)) continue;
                CarBody body = BodyFor(Where.Street, MapStyle.Unit(key, SaltBody));
                float len = CarKit.Size(body).z;
                Vector2 nose = Axis(s.Yaw), across = new Vector2(-nose.y, nose.x);
                // Pulled up to the wheel stop at the stall's head.
                Vector2 at = s.At + nose * (s.Depth * 0.5f - 0.35f - len * 0.5f) + across * ((MapStyle.Unit(key, SaltJitter) - 0.5f) * 0.3f);
                if (plan.IsKept(at, nose, len * 0.5f, 1.0f)) continue;
                bool backed = MapStyle.Unit(key, SaltLot + 2) < 0.12f;
                found.Add(new Candidate
                {
                    At = at, Yaw = s.Yaw + (backed ? 180f : 0f) + (MapStyle.Unit(key, SaltJitter + 2) - 0.5f) * 5f, Key = key, Body = body, Snow = 0.55f,
                });
            }
            foreach (Patch pa in c.Site.Area.Areas)
                if (pa.Data.kind == "parking" && !marked.Contains(pa.Data.id)) PlainLot(c, pa);
            foreach (string step in ParkFound(c, Mathf.Min(c.Max / 2, c.Max - c.Count - 8), "parking lot cars")) yield return step;
            yield return "parking lot cars";
        }

        // How full a lot is: its own share, more by the stores.
        private static float LotFill(NaturePlan plan, long lot, Vector2 at)
            => 0.25f + 0.35f * MapStyle.Unit(lot, SaltLot) + (plan.Business(at) > 0.04f ? 0.15f : 0f);

        // A lot without stalls (a church's, a store's, a garage's), whole on this site: its cars along its longest edges,
        // nosed up to them clear of the plough's snow at the edge.
        private static void PlainLot(Parking c, Patch pa)
        {
            NaturePlan plan = c.Plan;
            List<Vector2> poly = pa.Pts;
            if (poly.Count < 3) return;
            foreach (Vector2 q in poly)
                if (!plan.InSquare(q, 1f) || plan.CutVertex(q)) return;
            float signed = MapGeometry.SignedArea(poly), area = Mathf.Abs(signed);
            if (area < 50f) return;
            long id = pa.Data.id;
            int want = Mathf.Clamp(Mathf.RoundToInt(area / 30f * LotFill(plan, id, MapGeometry.Middle(poly))), 0, 8), placed = 0;
            // Its two longest edges, the longest first.
            int first = -1, second = -1;
            float l1 = 0f, l2 = 0f;
            for (int i = 0; i < poly.Count; i++)
            {
                float len = Vector2.Distance(poly[i], poly[(i + 1) % poly.Count]);
                if (len > l1)
                {
                    l2 = l1;
                    second = first;
                    l1 = len;
                    first = i;
                }
                else if (len > l2)
                {
                    l2 = len;
                    second = i;
                }
            }
            for (int pass = 0; pass < 2 && placed < want; pass++)
            {
                int e = pass == 0 ? first : second;
                if (e < 0) break;
                Vector2 a = poly[e], b = poly[(e + 1) % poly.Count];
                float l = Vector2.Distance(a, b);
                if (l < 3.5f) continue;
                Vector2 along = (b - a) / l, inward = signed > 0f ? new Vector2(-along.y, along.x) : new Vector2(along.y, -along.x);
                for (int k = 0; 1.7f + k * 2.9f <= l - 1.7f && placed < want; k++)
                {
                    long key = NaturePlan.Combine(id, e * 64 + k);
                    if (MapStyle.Unit(key, SaltLot + 3) < 0.25f) continue;
                    CarBody body = BodyFor(Where.Street, MapStyle.Unit(key, SaltBody));
                    float len = CarKit.Size(body).z;
                    Vector2 at = a + along * (1.7f + k * 2.9f) + inward * (1.6f + len * 0.5f);
                    if (!MapGeometry.Contains(poly, at + inward * (len * 0.5f)) || !MapGeometry.Contains(poly, at - inward * (len * 0.5f))) continue;
                    if (plan.IsKept(at, inward, len * 0.5f, 1.0f)) continue;
                    if (Park(c, at, Yaw(-inward) + (MapStyle.Unit(key, SaltJitter) - 0.5f) * 6f, LookFor(key, body, 0.55f), key, Quaternion.identity)) placed++;
                }
            }
        }
        #endregion

        #region The highways
        /// <summary>
        /// Out of town, along the highways and main roads, what's left of the people who tried to leave: a car on the
        /// shoulder with its door hanging open or a tyre flat, one run off into the ditch (clear of the fences, poles and
        /// signs), a short jam in the lane, a burnt-out shell; one every few hundred metres, on stations 30 m apart, none
        /// round a junction.
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
                    if (!plan.InSquare(s.At, 2f) || plan.Urban(s.At) > 0.04f || c.NoParkingAt(s.At)) continue;
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
            float edge = RoadPass.EdgeOffset(st, -side);   // the roads count their sides from the street's left
            float u(int k) => MapStyle.Unit(key, SaltWreck + 3 + k);
            CarBody body = BodyFor(Where.Highway, MapStyle.Unit(key, SaltBody));
            if (what < 0.45f)
            {
                Vector2 at = s.At + n * (edge - 0.5f);
                CarWear wear = (u(0) < 0.5f ? CarWear.DoorOpen : CarWear.None) | (u(1) < 0.25f ? CarWear.Flat : CarWear.None);
                Park(c, at, Yaw(heading) + (u(2) - 0.5f) * 14f, LookFor(key, body, 0.6f, wear), key, Quaternion.identity);
            }
            else if (what < 0.72f)
            {
                Vector2 at = s.At + n * (edge + 2.2f + u(0) * 1.5f);
                float slew = side * (20f + u(1) * 45f);
                // Off the road the ditch must be clear: no fence, pole, sign or tree in the way.
                Vector2 axis = Axis(Yaw(heading) + slew);
                for (int k = -1; k <= 1; k++)
                    if (!c.Site.FreeBeside(at + axis * (k * 1.6f), 0.9f)) return;
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
                    Vector2 at = s.At - heading * back + n * (edge * 0.5f + (MapStyle.Unit(ck, SaltJitter) - 0.5f) * 0.6f);
                    back += len * 0.5f + 1.2f + MapStyle.Unit(ck, SaltJitter + 1) * 1.8f;
                    if (c.NoParkingAt(at)) break;
                    CarWear wear = MapStyle.Unit(ck, SaltWear) < 0.4f ? CarWear.DoorOpen : CarWear.None;
                    Park(c, at, Yaw(heading) + (MapStyle.Unit(ck, SaltJitter + 2) - 0.5f) * 12f, LookFor(ck, b, 0.6f, wear), ck, Quaternion.identity);
                }
            }
            else
            {
                Vector2 at = s.At + n * (edge - 0.3f);
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
                if (plan.WallDistance(q, 2f) < 1.4f || plan.RoadClearance(q, 2f) < 0.4f || plan.NearDoor(q, 3f)) return false;
                Land land = plan.LandAt(q);
                if (land == Land.Water || land == Land.Pitch) return false;
                bool byBox = q.x > box.xMin - 1.5f && q.x < box.xMax + 1.5f && q.y > box.yMin - 1.5f && q.y < box.yMax + 1.5f;
                if (!byBox && !c.Site.FreeBeside(q, 1.3f)) return false;
            }
            return true;
        }

        // A cruiser at the kerb in front of the police station, clear of the corners and the driveways.
        private static void Cruiser(Parking c, Spot s)
        {
            if (!c.Plan.NearestRoad(s.At, 45f, 2, out NaturePlan.RoadSeg seg, out float dist, out Vector2 foot) || dist < 0.5f) return;
            Vector2 dir = (seg.B - seg.A).normalized, toward = (s.At - foot) / dist;
            Vector2 right = new Vector2(dir.y, -dir.x);
            float side = Vector2.Dot(right, toward) >= 0f ? 1f : -1f;
            Vector2 heading = side > 0f ? dir : -dir;
            float edge = RoadPass.EdgeOffset(c.Plan.StreetOf(seg), side > 0f ? -1 : 1);
            long key = NaturePlan.Combine(s.Data.id, 77);
            for (int k = 0; k < 5; k++)
            {
                float along = (k % 2 == 0 ? 1f : -1f) * ((k + 1) / 2) * 6.6f;
                Vector2 at = foot + dir * along + right * side * (edge - 1.15f);
                if (c.NoParkingAt(at) || c.Plan.IsKept(at, dir, 2.7f, 1.0f)) continue;
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
