using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The buildings raised where the plan puts them, and their lots: the office's plaza and staff lot, the food mart's
    // forecourt under its canopy, the depot's yard, the Guard post's parade ground and motor pool, the quarantine compound,
    // the clinic's lot, Veterans Park, and the houses' yards with their walks, drives and fences.
    public static partial class RaidMapBuilder
    {
        private sealed partial class Builder
        {
            private RaidBuilding _office, _mart, _depot, _post, _tent, _clinic;
            private readonly RaidBuilding[] _houses = new RaidBuilding[HouseLots.Length];

            public void Buildings()
            {
                _office = Raise(RaidLayouts.Office(_town, OfficeAt, OfficeTurns));
                _mart = Raise(RaidLayouts.GasStation(_town, MartAt, MartTurns));
                _depot = Raise(RaidLayouts.Warehouse(_town, DepotAt, DepotTurns));
                _post = Raise(RaidLayouts.ArmyPost(_town, PostAt, PostTurns));
                _tent = Raise(RaidLayouts.QuarantineTent(_town, TentAt, TentTurns));
                _clinic = Raise(RaidLayouts.Clinic(_town, ClinicAt, ClinicTurns));
                // Create each house lot building and store for later placement
                for (int i = 0; i < HouseLots.Length; i++)
                {
                    HouseLot lot = HouseLots[i];
                    _houses[i] = Raise(RaidLayouts.House(_town, lot.Name, lot.Center, lot.Turns, lot.Mirror, lot.Small));
                }

                // Build the county office and its staff lot
                Office();
                // Build the gas‑station food mart forecourt
                FoodMart();
                // Build the warehouse depot and its yard
                Depot();
                // Build the National Guard post and its motor pool
                GuardPost();
                // Build the quarantine tent and surrounding compound
                Quarantine();
                // Build the clinic and its parking lot
                Clinic();
                // Build Veterans Park with plaza, benches, trees and lamps
                Park();
                // Place each house building onto its lot and add yard features
                for (int i = 0; i < HouseLots.Length; i++) House(HouseLots[i], _houses[i]);
                // Add sidewalks and yards for both sides of Elm and Oak streets
                Yards(ElmSt, +1);
                Yards(ElmSt, -1);
                Yards(OakSt, +1);
                Yards(OakSt, -1);
            }

            private RaidBuilding Raise(RaidBuilding b)
            {
                _town.Buildings.Add(b);
                Reserve(Grow(b.Footprint, 1.2f));
                b.ExteriorDoorZones(_town.Keepout);
                return b;
            }

            // The outside doors on one side of a building (0 south, 1 west, 2 north, 3 east), each as the middle of its
            // doorway on the wall's outer face.
            private static List<Vector3> Doors(RaidBuilding b, int side)
            {
                var zones = new List<Rect>();
                b.ExteriorDoorZones(zones);
                Rect f = b.Footprint;
                var doors = new List<Vector3>();
                foreach (Rect z in zones)
                {
                    Vector2 c = z.center;
                    int s = c.y < f.yMin ? 0 : c.x < f.xMin ? 1 : c.y > f.yMax ? 2 : c.x > f.xMax ? 3 : -1;
                    if (s != side) continue;
                    doors.Add(s == 0 ? new Vector3(c.x, 0f, f.yMin) : s == 2 ? new Vector3(c.x, 0f, f.yMax)
                        : s == 1 ? new Vector3(f.xMin, 0f, c.y) : new Vector3(f.xMax, 0f, c.y));
                }
                return doors;
            }

            /// <summary>A walk <paramref name="width"/> m wide from a door on side <paramref name="side"/> of its building
            /// straight out to <paramref name="to"/> (a z for a north or south door, an x for an east or west one).</summary>
            private void Walk(Vector3 door, int side, float to, in Surf surf, float width = 1.4f)
            {
                float h = width * 0.5f;
                Rect r = side == 0 || side == 2
                    ? Rect.MinMaxRect(door.x - h, Mathf.Min(door.z, to), door.x + h, Mathf.Max(door.z, to))
                    : Rect.MinMaxRect(Mathf.Min(door.x, to), door.z - h, Mathf.Max(door.x, to), door.z + h);
                Pave(r, PathY, surf);
            }

            /// <summary>Painted bays along one edge of a lot: <paramref name="count"/> bays <paramref name="width"/> m wide
            /// from <paramref name="start"/> along the edge, <paramref name="depth"/> m deep in from it. Returns each bay's
            /// middle.</summary>
            private List<Vector3> Bays(Vector3 start, Vector3 along, Vector3 inward, int count, float width, float depth)
            {
                Surf white = DecorPalette.Matte(LineWhite);
                var middles = new List<Vector3>();
                for (int i = 0; i <= count; i++)
                {
                    Vector3 c = start + along * (i * width) + inward * (depth * 0.5f);
                    c.y = MarkY;
                    float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
                    _town.Out.Flat(c).Flat(c, 0.1f, depth - 0.2f, yaw, white);
                    if (i < count) middles.Add(start + along * ((i + 0.5f) * width) + inward * (depth * 0.5f));
                }
                return middles;
            }

            /// <summary>Two or three bags slumped round <paramref name="at"/>, to tear open and go through.</summary>
            private void Bags(Vector3 at)
            {
                int n = 2 + _rng.Next(2);
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = at + new Vector3(R(_rng, -0.5f, 0.5f), 0f, R(_rng, -0.35f, 0.35f));
                    DecorProps.TrashBag(_town.Out.Solid(p), _rng, p);
                }
                TrashBagsProp(_town, at);
                Reserve(Around(at, 0.8f, 0.6f));
            }

            private void Heap(Vector3 at, float w, float d, float h)
                => Prop(_town, at, R(_rng, 0f, 180f), new Vector3(w * 0.8f, h, d * 0.8f), true, (m, r) => DecorProps.SnowPile(m, r, Vector3.zero, w, d, h, 0f));

            private void Sign(Vector3 at, float yaw, string text, Color bg, Color fg, float height, float bottom)
                => Prop(_town, at, yaw, new Vector3(height * 6f, bottom + height, 0.3f), true, (m, r) => DecorProps.PostSign(m, r, text, bg, fg, height, bottom));

            private void Flagpole(Vector3 at, float height)
                => Prop(_town, at, 0f, new Vector3(0.5f, height, 0.5f), true, (m, r) => DecorProps.Flagpole(m, height));

            /// <summary>A tree with its collider: a bare maple, or a spruce (smaller in the yards, taller in the woods by the
            /// fence when <paramref name="tall"/>).</summary>
            private void Tree(Vector3 at, bool pine, bool tall = false)
            {
                float h = pine ? (tall ? R(_rng, 7f, 10f) : R(_rng, 5f, 7f)) : R(_rng, 5f, 7.5f);
                Prop(_town, at, R(_rng, 0f, 360f), new Vector3(0.6f, 3f, 0.6f), true, (m, r) =>
                {
                    if (pine) DecorProps.Pine(m, r, h);
                    else DecorProps.BareTree(m, r, h);
                });
            }

            #region Civic lots
            // The county office: a concrete plaza from Depot Rd to its glass front doors with the flag and the county's sign,
            // a walk from Main St to its side door, the staff lot to the east, the dumpster out back.
            private void Office()
            {
                RaidBuilding b = _office;
                Rect f = b.Footprint;
                Surf concrete = DecorMaterials.Concrete;
                foreach (Vector3 d in Doors(b, 0))
                {
                    Pave(Rect.MinMaxRect(d.x - 3.4f, DepotRd.Outer, d.x + 3.4f, f.yMin), PathY, concrete);
                    Flagpole(new Vector3(d.x - 4.6f, 0f, DepotRd.Outer + 2f), 8f);
                }
                Sign(new Vector3(f.xMin + 4.5f, 0f, DepotRd.Outer + 1.6f), 180f, "COUNTY OFFICES", new Color(0.14f, 0.20f, 0.32f), new Color(0.88f, 0.86f, 0.78f), 0.4f, 0.9f);
                foreach (Vector3 d in Doors(b, 1)) Walk(d, 1, MainSt.Outer, concrete);
                foreach (Vector3 d in Doors(b, 2))
                {
                    Pave(Rect.MinMaxRect(d.x - 1.2f, f.yMax, d.x + 1.2f, f.yMax + 1.6f), PathY, concrete);
                    DumpsterProp(_town, new Vector3(d.x - 4.4f, 0f, f.yMax + 1.6f), 0f);
                    Bags(new Vector3(d.x - 7f, 0f, f.yMax + 1.5f));
                }

                // The staff lot: one row of bays along its east side, the entrance off Depot Rd, a heap of plowed snow.
                Rect lot = Rect.MinMaxRect(f.xMax + 1.9f, DepotRd.Outer, f.xMax + 16.4f, f.yMax);
                Pave(lot, LotY, DecorMaterials.DryAsphalt);
                Reserve(lot);
                DropKerb(DepotRd, +1, lot.xMin + 1f, lot.xMin + 8f);
                List<Vector3> bays = Bays(new Vector3(lot.xMax - 0.2f, 0f, lot.yMin + 1.4f), Vector3.forward, Vector3.left, 6, 2.7f, 5f);
                ParkIn(bays, 2, 90f);
                Heap(new Vector3(lot.xMin + 1.4f, 0f, lot.yMax - 1.2f), 2.6f, 2.2f, 1.0f);
            }

            /// <summary>Parks <paramref name="cars"/> cars in different bays, nose in (<paramref name="yaw"/> faces the bay's
            /// end).</summary>
            private void ParkIn(List<Vector3> bays, int cars, float yaw)
            {
                var free = new List<Vector3>(bays);
                for (int i = 0; i < cars && free.Count > 0; i++)
                {
                    int k = _rng.Next(free.Count);
                    ParkedCar(_town, free[k], yaw + R(_rng, -2f, 2f), FamilyCar(_rng), Paint(_rng));
                    free.RemoveAt(k);
                }
            }

            // The gas-station food mart: the forecourt between Main St and the shop with two pump islands under the canopy
            // (its roof shows only from inside views), the price sign, a pickup at a pump, the dumpster round the back.
            private void FoodMart()
            {
                RaidBuilding b = _mart;
                Rect f = b.Footprint;
                Rect court = Rect.MinMaxRect(MainSt.Outer, f.yMin - 4.4f, f.xMin, f.yMax + 4.4f);
                Pave(court, LotY, DecorMaterials.Concrete);
                Reserve(court);
                DropKerb(MainSt, +1, court.yMin + 1f, court.yMin + 7.5f);
                DropKerb(MainSt, +1, court.yMax - 7.5f, court.yMax - 1f);

                float islandX = (court.xMin + court.xMax) * 0.5f + 0.4f;
                float midZ = (f.yMin + f.yMax) * 0.5f;
                foreach (float dz in new[] { -3f, 3f }) PumpProp(_town, new Vector3(islandX, 0f, midZ + dz), 0f);
                Canopy(Rect.MinMaxRect(islandX - 2.4f, midZ - 6.5f, islandX + 2.4f, midZ + 6.5f));
                ParkedCar(_town, new Vector3(islandX - 2.1f, 0f, midZ + 3f), 180f + R(_rng, -2f, 2f), DecorProps.CarKind.Pickup, Paint(_rng));
                Prop(_town, new Vector3(court.xMin + 0.8f, 0f, court.yMin + 0.9f), 270f, new Vector3(1.6f, 3.1f, 0.3f), true,
                    (m, r) => DecorProps.PostSign(m, r, "GAS 1.09", new Color(0.86f, 0.84f, 0.78f), new Color(0.62f, 0.10f, 0.08f), 0.5f, 2.6f));

                foreach (Vector3 d in Doors(b, 3))
                {
                    Pave(Rect.MinMaxRect(f.xMax, d.z - 1.2f, f.xMax + 1.6f, d.z + 1.2f), PathY, DecorMaterials.Concrete);
                    DumpsterProp(_town, new Vector3(f.xMax + 1.8f, 0f, d.z - 6.5f), 90f);
                    Bags(new Vector3(f.xMax + 1.7f, 0f, d.z - 3.9f));
                }
            }

            // The canopy over the pumps: four pillars on hazard-striped feet, the roof under the overhead root with a thin
            // collider (so the shoulder camera stays below it), and its lamps, one or two still lit.
            private void Canopy(Rect r)
            {
                Surf pillar = DecorPalette.Matte(new Color(0.80f, 0.80f, 0.78f));
                for (int i = 0; i < 4; i++)
                {
                    var p = new Vector3(i % 2 == 0 ? r.xMin + 0.4f : r.xMax - 0.4f, 0f, i < 2 ? r.yMin + 0.4f : r.yMax - 0.4f);
                    Prop(_town, p, 0f, new Vector3(0.35f, 4.2f, 0.35f), true, (m, rr) =>
                    {
                        m.Box(new Vector3(0f, 2.1f, 0f), new Vector3(0.32f, 4.2f, 0.32f), pillar);
                        m.Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.4f, 0.6f, 0.4f), DecorMaterials.Hazard);
                    });
                }
                var roof = new RaidMesh();
                var rc = new Vector3(r.center.x, 4.35f, r.center.y);
                var size = new Vector3(r.width, 0.3f, r.height);
                roof.Box(rc, size, DecorPalette.Matte(new Color(0.82f, 0.82f, 0.80f)));
                roof.Box(rc + new Vector3(0f, -0.1f, 0f), size + new Vector3(0.1f, -0.18f, 0.1f), DecorPalette.Matte(new Color(0.66f, 0.12f, 0.10f)));
                roof.Box(rc + new Vector3(0f, 0.2f, 0f), size - new Vector3(0.3f, 0.22f, 0.3f), DecorPalette.Matte(DecorProps.SnowWhite));
                int lamps = 3, lit = 1 + _rng.Next(2);
                for (int i = 0; i < lamps; i++)
                {
                    bool on = i < lit;
                    var lp = new Vector3(rc.x, 4.19f, r.yMin + r.height * (i + 0.5f) / lamps);
                    roof.Box(lp, new Vector3(0.5f, 0.02f, 1.0f), on ? DecorPalette.Glow(new Color(0.95f, 0.92f, 0.82f)) : DecorPalette.Matte(new Color(0.5f, 0.5f, 0.48f)));
                    if (on) _town.Light(_mart.Root, lp - Vector3.up * 0.16f, new Color(0.92f, 0.94f, 1f), 9f, 1.6f, false, i == 0);
                }
                GameObject canopy = roof.Emit("Canopy", _town.Overhead, rc);
                if (canopy != null) CeilingVisibility.Register(canopy.GetComponent<MeshRenderer>());
                _town.OverheadCollider(rc, size, Quaternion.identity);
            }

            // The depot: its yard before the loading doors, the drive down to Depot Rd, a pickup, pallets waiting by the rail
            // line, the dumpster and the plowed snow.
            private void Depot()
            {
                RaidBuilding b = _depot;
                Rect f = b.Footprint;
                Rect yard = Rect.MinMaxRect(f.xMin - 3.9f, f.yMin - 13.4f, RailX - 2.4f, f.yMin);
                Pave(yard, LotY, DecorMaterials.DryAsphalt);
                Reserve(yard);
                Rect drive = Rect.MinMaxRect(f.center.x - 3f, DepotRd.Outer, f.center.x + 5f, yard.yMin);
                Pave(drive, LotY, DecorMaterials.DryAsphalt);
                Reserve(drive);
                DropKerb(DepotRd, +1, drive.xMin, drive.xMax);
                ParkedCar(_town, new Vector3(f.xMin + 2f, 0f, yard.yMin + 5f), R(_rng, -2f, 2f), DecorProps.CarKind.Pickup, Paint(_rng));
                for (int i = 0; i < 2; i++)
                {
                    var p = new Vector3(yard.xMax - 1.2f, 0f, yard.yMin + 2f + i * 2.2f);
                    float yaw = i * 90f;
                    Prop(_town, p, 0f, new Vector3(1.2f, 1.2f, 1.0f), true, (m, r) => FurnitureBuilder.Pallet(m, r, Vector3.zero, yaw, true));
                }
                DumpsterProp(_town, new Vector3(yard.xMin + 1.6f, 0f, f.yMin - 2f), 0f);
                Heap(new Vector3(yard.xMin + 1.3f, 0f, yard.yMin + 1.3f), 2.6f, 2.2f, 1.2f);
            }

            // The National Guard post: the gravel parade ground off Main St with the flag, sandbags either side of the door,
            // the unit's truck and its sign; the motor pool behind with two more trucks and the fuel drums; the dumpster.
            private void GuardPost()
            {
                RaidBuilding b = _post;
                Rect f = b.Footprint;
                Rect yard = Rect.MinMaxRect(MainSt.Outer, f.yMin - 2.4f, f.xMin, f.yMax + 1.9f);
                Pave(yard, LotY, DecorMaterials.Gravel);
                DropKerb(MainSt, +1, yard.yMin + 0.6f, yard.yMin + 5.6f);
                foreach (Vector3 d in Doors(b, 1))
                    for (int s = -1; s <= 1; s += 2)
                        Prop(_town, new Vector3(d.x - 1.2f, 0f, d.z + s * 2.9f), 90f, new Vector3(2.4f, 0.7f, 0.5f), true, (m, r) => DecorProps.SandbagWall(m, r, 2.4f, 4));
                Flagpole(new Vector3(yard.xMin + 2.6f, 0f, yard.yMax - 1.6f), 7f);
                Sign(new Vector3(yard.xMin + 0.9f, 0f, yard.center.y - 1.5f), 270f, "NATIONAL GUARD", new Color(0.20f, 0.26f, 0.16f), new Color(0.88f, 0.86f, 0.74f), 0.42f, 1.2f);
                ParkedCar(_town, new Vector3(yard.xMin + 3.6f, 0f, yard.yMin + 3.6f), R(_rng, -2f, 2f), DecorProps.CarKind.ArmyTruck, Color.white);
                Reserve(yard);
                foreach (Vector3 d in Doors(b, 3)) Pave(Rect.MinMaxRect(f.xMax, d.z - 1.2f, f.xMax + 1.6f, d.z + 1.2f), PathY, DecorMaterials.Concrete);
                DumpsterProp(_town, new Vector3(f.xMax + 2f, 0f, f.yMin + 3.5f), 90f);

                // The motor pool south of the post, its gate on Main St; a gravel path to the post's side door.
                Rect pool = Rect.MinMaxRect(MainSt.Outer, f.yMin - 16.4f, f.xMax + 2.9f, f.yMin - 3.9f);
                Pave(pool, LotY, DecorMaterials.Gravel);
                DropKerb(MainSt, +1, pool.yMin + 1f, pool.yMax - 1f);
                foreach (Vector3 d in Doors(b, 0)) Walk(d, 0, pool.yMax, DecorMaterials.Gravel, 1.6f);
                for (int i = 0; i < 2; i++)
                    ParkedCar(_town, new Vector3(pool.center.x - 2f, 0f, pool.yMax - 3.4f - i * 4.6f), 90f + R(_rng, -1.5f, 1.5f), DecorProps.CarKind.ArmyTruck, Color.white);
                Color olive = new Color(0.28f, 0.31f, 0.20f);
                for (int i = 0; i < 3; i++)
                    Prop(_town, new Vector3(pool.xMax - 3.2f + i * 0.8f, 0f, pool.yMin + 0.9f), 0f, new Vector3(0.62f, 0.9f, 0.62f), true,
                        (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, olive, true));
                Reserve(pool);
            }

            // The quarantine compound behind the Guard post: a chain-link fence with its gate facing the post, the tent in the
            // middle of the toxic cloud, sandbags at the flap, the generator, a row of drums and the warning lamp at the gate.
            private void Quarantine()
            {
                Rect c = Compound;
                Vector3 flap = Doors(_tent, 1).Count > 0 ? Doors(_tent, 1)[0] : new Vector3(TentAt.x - 4.6f, 0f, TentAt.y);
                float gate0 = flap.z - 2.5f, gate1 = flap.z + 2.5f;
                // Counter-clockwise, so each run's snow banks up outside the compound.
                Fence(new Vector3(c.xMin, 0f, c.yMax), new Vector3(c.xMin, 0f, gate1), FenceKind.ChainLink, default);
                Fence(new Vector3(c.xMin, 0f, gate0), new Vector3(c.xMin, 0f, c.yMin), FenceKind.ChainLink, default);
                Fence(new Vector3(c.xMin, 0f, c.yMin), new Vector3(c.xMax, 0f, c.yMin), FenceKind.ChainLink, default);
                Fence(new Vector3(c.xMax, 0f, c.yMin), new Vector3(c.xMax, 0f, c.yMax), FenceKind.ChainLink, default);
                Fence(new Vector3(c.xMax, 0f, c.yMax), new Vector3(c.xMin, 0f, c.yMax), FenceKind.ChainLink, default);
                Reserve(Rect.MinMaxRect(c.xMin - 3f, gate0, c.xMin + 1f, gate1));
                Reserve(Rect.MinMaxRect(c.xMin, gate0, flap.x, gate1));

                ToxicZone.Spawn(_town.Scenery, new Vector3(TentAt.x, 0f, TentAt.y), 12f);
                for (int s = -1; s <= 1; s += 2)
                {
                    Prop(_town, new Vector3(c.xMin - 1f, 0f, flap.z + s * 3.6f), 270f, new Vector3(0.7f, 2f, 0.3f), true, (m, r) => DecorProps.HazardSign(m, r));
                    Prop(_town, new Vector3(flap.x - 2.2f, 0f, flap.z + s * 2.4f), 90f, new Vector3(1.8f, 0.7f, 0.5f), true, (m, r) => DecorProps.SandbagWall(m, r, 1.8f, 4));
                }
                GeneratorProp(_town, new Vector3(c.xMin + 4f, 0f, c.yMin + 3f), 0f);
                Color hazard = new Color(0.66f, 0.54f, 0.12f);
                for (int i = 0; i < 4; i++)
                    Prop(_town, new Vector3(c.xMax - 9f + i * 0.8f, 0f, c.yMin + 1.1f), 0f, new Vector3(0.62f, 0.9f, 0.62f), true,
                        (m, r) => FurnitureBuilder.Barrel(m, r, Vector3.zero, hazard, true));

                // A caged warning lamp on a post by the gate, burning red-orange.
                var lamp = new Vector3(c.xMin - 1f, 0f, gate1 + 0.6f);
                Prop(_town, lamp, 0f, new Vector3(0.2f, 2.4f, 0.2f), true, (m, r) =>
                {
                    Surf post = DecorPalette.Metal(new Color(0.30f, 0.32f, 0.28f));
                    m.Box(new Vector3(0f, 1.2f, 0f), new Vector3(0.08f, 2.4f, 0.08f), post);
                    m.Box(new Vector3(0f, 2.42f, 0.12f), new Vector3(0.05f, 0.05f, 0.3f), post);
                    m.Box(new Vector3(0f, 2.48f, 0.24f), new Vector3(0.2f, 0.06f, 0.2f), DecorPalette.Matte(new Color(0.15f, 0.15f, 0.14f)));
                    m.Box(new Vector3(0f, 2.36f, 0.24f), new Vector3(0.14f, 0.14f, 0.14f), DecorPalette.Glow(new Color(1f, 0.45f, 0.25f)));
                });
                _town.Light(_tent.Root, lamp + new Vector3(0f, 2.3f, 0.24f), new Color(1f, 0.45f, 0.25f), 6f, 1.4f, true, false);
            }

            // The clinic: the walk from Depot Rd to its front door and its sign; the patients' lot behind, off Oak St, with
            // the back door's path, the dumpster and the plowed snow.
            private void Clinic()
            {
                RaidBuilding b = _clinic;
                Rect f = b.Footprint;
                foreach (Vector3 d in Doors(b, 2)) Walk(d, 2, -DepotRd.Outer, DecorMaterials.Concrete, 1.8f);
                Sign(new Vector3(f.xMin + 3.5f, 0f, -DepotRd.Outer - 1.4f), 0f, "CLINIC", new Color(0.86f, 0.86f, 0.82f), new Color(0.12f, 0.36f, 0.22f), 0.45f, 0.9f);

                Rect lot = Rect.MinMaxRect(f.xMin, OakSt.Coord + OakSt.Outer, f.xMax, f.yMin - 1.6f);
                Pave(lot, LotY, DecorMaterials.DryAsphalt);
                Reserve(lot);
                DropKerb(OakSt, +1, lot.center.x - 3.5f, lot.center.x + 3.5f);
                List<Vector3> bays = Bays(new Vector3(lot.xMin + 1.2f, 0f, lot.yMax - 0.2f), Vector3.right, Vector3.back, 5, 2.7f, 5f);
                ParkIn(bays, 2, 0f);
                foreach (Vector3 d in Doors(b, 0)) Walk(d, 0, lot.yMax, DecorMaterials.Concrete);
                DumpsterProp(_town, new Vector3(lot.xMax - 1.7f, 0f, lot.yMax - 2.2f), 90f);
                Heap(new Vector3(lot.xMin + 1.3f, 0f, lot.yMin + 1.3f), 2.6f, 2.2f, 1.1f);
            }

            // Veterans Park on the crossroads' north-west corner: a cross of paths meeting at a plaza round the war memorial,
            // benches facing it, a tree in each quarter, two lamps and the park's sign.
            private void Park()
            {
                Rect p = ParkLot;
                var mid = new Vector3(p.center.x, 0f, p.center.y);
                Surf concrete = DecorMaterials.Concrete;
                Pave(Rect.MinMaxRect(p.xMin, mid.z - 0.8f, p.xMax, mid.z + 0.8f), PathY, concrete);
                Pave(Rect.MinMaxRect(mid.x - 0.8f, p.yMin, mid.x + 0.8f, p.yMax), PathY, concrete);
                Pave(Around(mid, 3.5f, 3.5f), PlazaY, concrete);
                Prop(_town, mid, 180f, new Vector3(2.6f, 5.4f, 2.6f), true, (m, r) => DecorProps.Obelisk(m));
                Reserve(Around(mid, 3.6f, 3.6f));
                for (int i = 0; i < 4; i++)
                {
                    float sx = i % 2 == 0 ? -1f : 1f, sz = i < 2 ? -1f : 1f;
                    var at = mid + new Vector3(sx * 4.7f, 0f, sz * 4.7f);
                    float yaw = Mathf.Atan2(-sx, -sz) * Mathf.Rad2Deg;
                    Prop(_town, at, yaw, new Vector3(1.8f, 0.9f, 0.6f), true, (m, r) => {
                        DecorProps.Bench(m);
                        AddBenchHardware(m, at);
                    });
                    if (i == 0 || i == 3)
                        Prop(_town, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(1.4f, 0f, 0f), 0f, new Vector3(0.5f, 1f, 0.5f), true, (m, r) => DecorProps.LitterBin(m));
                    Tree(new Vector3(mid.x + sx * (p.width * 0.28f), 0f, mid.z + sz * (p.height * 0.3f)), i == 1 || i == 2);
                }
                ParkLamp(new Vector3(mid.x - 1.6f, 0f, p.yMin + 2.2f));
                ParkLamp(new Vector3(mid.x + 1.6f, 0f, p.yMax - 2.2f));
                Sign(new Vector3(p.xMax - 3.2f, 0f, p.yMin + 1.4f), 180f, "VETERANS PARK", new Color(0.18f, 0.28f, 0.20f), new Color(0.88f, 0.86f, 0.74f), 0.36f, 0.6f);
            }

            private void ParkLamp(Vector3 at)
            {
                bool lit = Chance(_rng, 0.6);
                RaidMesh m = _town.Out.Solid(at);
                m.SetFrame(at, 0f);
                Vector3 globe = m.ToMeshSpace(DecorProps.ParkLamp(m, lit));
                m.ResetFrame();
                _town.PropCollider(at + Vector3.up * 1.9f, new Vector3(0.3f, 3.8f, 0.3f), 0f);
                Reserve(Around(at, 0.5f, 0.5f));
                if (lit) _town.Light(_town.World, globe, new Color(1f, 0.82f, 0.58f), 9f, 1.5f, false, false);
            }
            #endregion

            #region Houses
            // A house's front yard, fitted like the starter home's: the front walk out to the sidewalk, the mailbox by it with
            // the house's post in it, the welcome mat on the step, the number by the door and the outside tap on the side wall
            // (all of them working); the drive along the kitchen side, most with the family car on it; a tree in the yard.
            private void House(in HouseLot lot, RaidBuilding b)
            {
                float doorLx = lot.Small ? -1.5f : -3f;
                Vector3 door = b.W(doorLx, 0f, -b.HalfD - RaidBuilding.Half);
                Pave(Rect.MinMaxRect(door.x - 0.7f, Mathf.Min(door.z, lot.SidewalkZ), door.x + 0.7f, Mathf.Max(door.z, lot.SidewalkZ)), PathY, DecorMaterials.Concrete);
                Vector3 box = b.W(doorLx + 1.3f, 0f, -b.HalfD - RaidBuilding.Half - Setback + 0.7f);
                Reserve(Around(box, 0.3f, 0.3f));
                b.YardMailbox(box, RaidHomeLiving.Post(_rng));
                b.FrontDoormat(doorLx);
                b.HouseNumber(doorLx, HouseNumberOf(lot));
                b.OutsideTap(b.HalfD > 4.75f ? 0.6f : 0.3f);   // on the side wall, between its two windows

                Rect drive = Rect.MinMaxRect(lot.DriveX - DriveHalf, Mathf.Min(lot.SidewalkZ, lot.Center.y), lot.DriveX + DriveHalf, Mathf.Max(lot.SidewalkZ, lot.Center.y));
                Pave(drive, LotY, DecorMaterials.DryAsphalt);
                DropKerb(lot.Street, lot.Side, drive.xMin, drive.xMax);
                if (Chance(_rng, 0.7))
                    ParkedCar(_town, new Vector3(lot.DriveX, 0f, lot.FrontZ + lot.Side * 0.6f), (lot.Side > 0 ? 0f : 180f) + R(_rng, -2f, 2f), FamilyCar(_rng), Paint(_rng));
                Reserve(drive);

                if (Chance(_rng, 0.6))
                {
                    Vector3 t = b.W(-b.HalfW + 1.2f, 0f, -b.HalfD - RaidBuilding.Half - Setback * 0.5f);
                    if (Free(Around(t, 0.8f, 0.8f))) Tree(t, false);
                }
            }

            // The lots along one side of a residential street: the bounds between neighbours, a picket fence along the front
            // of some (gapped for the walk and the drive), and the backyards fenced off from each other and from Depot Rd.
            private void Yards(Street street, int side)
            {
                List<int> row = Row(street, side);
                if (row.Count == 0) return;
                var bounds = new float[row.Count + 1];
                for (int i = 0; i <= row.Count; i++)
                {
                    if (i == 0) bounds[i] = WestOf(HouseLots[row[0]]) - 2.5f;
                    else if (i == row.Count) bounds[i] = EastOf(HouseLots[row[i - 1]]) + 2.5f;
                    else bounds[i] = (EastOf(HouseLots[row[i - 1]]) + WestOf(HouseLots[row[i]])) * 0.5f;
                }
                float limit = RowLimitEast(street, side);
                // A corner lot on Main St keeps its side yard open to the sidewalk (no fence along the main road).
                bool openToMain = bounds[row.Count] >= limit && limit >= -MainSt.Outer - 0.31f;
                bounds[row.Count] = Mathf.Min(bounds[row.Count], limit);

                // Behind the backs: Depot Rd (a board fence along its sidewalk) or the map's own fence.
                bool onDepotRd = (side > 0) == (street.Coord < 0f);
                float rear = onDepotRd ? Mathf.Sign(street.Coord) * (DepotRd.Outer + 0.8f) : side * (EdgeLine - 1.2f);
                float backLine = side > 0 ? float.MinValue : float.MaxValue;
                foreach (int i in row) backLine = side > 0 ? Mathf.Max(backLine, HouseLots[i].BackZ) : Mathf.Min(backLine, HouseLots[i].BackZ);
                backLine += side * 0.8f;

                Color boards = new Color(0.50f, 0.40f, 0.30f);
                if (onDepotRd) Fence(new Vector3(bounds[0], 0f, rear), new Vector3(bounds[row.Count], 0f, rear), FenceKind.Privacy, boards);
                for (int i = 0; i <= row.Count; i++)
                    if (!(i == row.Count && openToMain)) Fence(new Vector3(bounds[i], 0f, backLine), new Vector3(bounds[i], 0f, rear), FenceKind.Privacy, boards);

                for (int k = 0; k < row.Count; k++)
                {
                    HouseLot lot = HouseLots[row[k]];
                    if (!Chance(_rng, 0.5)) continue;
                    Color paint = Chance(_rng, 0.6) ? new Color(0.88f, 0.88f, 0.84f) : new Color(0.56f, 0.46f, 0.34f);
                    float z = lot.SidewalkZ + side * 0.35f;
                    Vector3 door = _houses[row[k]].W(lot.Small ? -1.5f : -3f, 0f, 0f);
                    var runs = new List<Span> { new Span { A = bounds[k] + 0.3f, B = bounds[k + 1] - 0.3f } };
                    Cut(runs, door.x - 0.9f, door.x + 0.9f, false);
                    Cut(runs, lot.DriveX - DriveHalf - 0.2f, lot.DriveX + DriveHalf + 0.2f, false);
                    foreach (Span r in runs)
                        if (r.B - r.A > 0.6f) Fence(new Vector3(side > 0 ? r.B : r.A, 0f, z), new Vector3(side > 0 ? r.A : r.B, 0f, z), FenceKind.Picket, paint);
                }

                // A tree in most backyards: mostly bare maples, the odd small spruce.
                for (int k = 0; k < row.Count; k++)
                {
                    if (!Chance(_rng, 0.65)) continue;
                    float x = Mathf.Lerp(bounds[k], bounds[k + 1], R(_rng, 0.3f, 0.7f));
                    float z = Mathf.Lerp(backLine, rear, R(_rng, 0.35f, 0.65f));
                    var t = new Vector3(x, 0f, z);
                    if (Free(Around(t, 0.8f, 0.8f))) Tree(t, Chance(_rng, 0.25));
                }
            }

            private static float WestOf(in HouseLot lot) => lot.X - lot.HalfW - RaidBuilding.Half - (lot.DriveEast ? 0f : 1f + DriveHalf * 2f);
            private static float EastOf(in HouseLot lot) => lot.X + lot.HalfW + RaidBuilding.Half + (lot.DriveEast ? 1f + DriveHalf * 2f : 0f);

            // How far east a row's last lot may run: to Main St's sidewalk, or short of the park or the clinic on the corner.
            private float RowLimitEast(Street street, int side)
            {
                if (street == ElmSt && side < 0) return ParkLot.xMin - 0.6f;
                if (street == OakSt && side > 0) return _clinic.Footprint.xMin - 0.6f;
                return -MainSt.Outer - 0.3f;
            }
            #endregion
            // Slightly darker wood tone for bench wear – used for leg‑caps and back‑rest slats.
            private static readonly Color BenchWood = new Color(0.55f, 0.48f, 0.38f);

        /// <summary>Adds period‑correct hardware to a bench mesh: four leg‑caps, three back‑rest slats and a subtle wood‑tone overlay.</summary>
        private void AddBenchHardware(RaidMesh m, Vector3 localOrigin)
        {
            // leg‑caps – tiny boxes at each corner of the bench seat
            var capSize = new Vector3(0.07f, 0.07f, 0.07f);
            var offsets = new[]
            {
                new Vector3(-0.85f, 0.45f, -0.25f), // front‑left
                new Vector3( 0.85f, 0.45f, -0.25f), // front‑right
                new Vector3(-0.85f, 0.45f,  0.25f), // back‑left
                new Vector3( 0.85f, 0.45f,  0.25f) // back‑right
            };
            foreach (var o in offsets) m.Box(localOrigin + o, capSize, DecorPalette.Matte(BenchWood));

            // back‑rest – three horizontal slats spaced vertically
            var slatSize = new Vector3(1.6f, 0.04f, 0.04f);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.75f + i * 0.12f; // 0.75 m at low, then +0.12 m each
                m.Box(localOrigin + new Vector3(0f, y, 0.28f), slatSize, DecorPalette.Matte(BenchWood));
            }

            // subtle wood‑tone overlay on the seat for wear
            var seatOverlay = new Vector3(1.5f, 0.02f, 0.4f);
            m.Box(localOrigin + new Vector3(0f, 0.48f, 0f), seatOverlay, DecorPalette.Matte(BenchWood));
        }
        }
    }
}
