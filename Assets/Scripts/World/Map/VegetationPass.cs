using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's trees and bushes, Kentucky in deep winter: bare oaks, maples, tulip poplars and hackberries,
    /// white-limbed sycamores along the creeks, dark red cedars crowding the fence rows, old fields and cemeteries, white
    /// pines and blue spruces in the yards (<see cref="TreeKit"/>). They come from the map where it has them (mapped
    /// trees, tree rows, woods, scrub, wetlands, parks, cemeteries, school grounds, pastures) and, since it mostly
    /// doesn't, from what a place is like: every home's yard trees and foundation bushes and a farmstead's windbreak
    /// (VegetationPass.Yards), the big trees along the town's residential streets, cedar fence rows along the country
    /// roads and the fields' edges, trees along the creeks, and the region's own woods over open country (thin in the
    /// Bluegrass, closing in towards the eastern mountains; see <see cref="NaturePlan.Wooded"/>), cut by the cleared
    /// strips under the power lines.
    /// Woods, rows and street trees stand on an absolute lattice (<see cref="NaturePlan"/>), so every piece of a
    /// streamed map grows the same trees and woods run on unbroken across the seams. Inside the playable square each tree
    /// gets a trunk collider, takes its spot (so it keeps off porches, doorsteps, driveways, poles and everything else
    /// standing) and is fitted to its room: its limbs cut back short of the nearest wall and overhead wire, a conifer
    /// sized so its skirt keeps off the walls, nothing growing up into a wire; a conifer leaves a well in the snow under it
    /// (<see cref="GroundPass.TreeWell"/>). Past a fixed map's edge they're cheap scenery; a Lite site draws each as a
    /// trunk under one mass of crown, without colliders. Before the trees, the driveways' cars are planned
    /// (<see cref="VehiclePass"/>) so the yard trees leave them room.
    /// </summary>
    internal static partial class VegetationPass
    {
        private const float WoodsCell = 6f, WoodsCellLite = 9f;   // the lattice the woods and grounds are sown on

        // Salts for the trees' random choices (MapStyle).
        private const int SaltKind = 0x7EE0, SaltShape = 0x7EE1, SaltYaw = 0x7EE2, SaltHeight = 0x7EE3, SaltSow = 0x7EE4,
            SaltUnder = 0x7EE5, SaltJitter = 0x7EE7, SaltGap = 0x7EE8, SaltField = 0x7EE9, SaltCreek = 0x7EEA,
            SaltRoadRow = 0x7EEB, SaltOldField = 0x7EEC, SaltCemetery = 0x7EED;

        /// <summary>A share of kinds of tree, drawn from by a number in [0, 1).</summary>
        private sealed class Mix
        {
            private readonly TreeKind[] _kinds;
            private readonly float[] _upTo;

            public Mix(params (TreeKind kind, float weight)[] entries)
            {
                _kinds = new TreeKind[entries.Length];
                _upTo = new float[entries.Length];
                float total = 0f;
                for (int i = 0; i < entries.Length; i++) total += entries[i].weight;
                float sum = 0f;
                for (int i = 0; i < entries.Length; i++)
                {
                    sum += entries[i].weight / total;
                    _kinds[i] = entries[i].kind;
                    _upTo[i] = sum;
                }
            }

            public TreeKind Pick(float u)
            {
                for (int i = 0; i < _upTo.Length; i++)
                    if (u < _upTo[i]) return _kinds[i];
                return _kinds[_kinds.Length - 1];
            }
        }

        private static readonly Mix ForestMix = new Mix((TreeKind.Oak, 0.30f), (TreeKind.Maple, 0.18f), (TreeKind.Tulip, 0.16f),
            (TreeKind.Hackberry, 0.10f), (TreeKind.Cedar, 0.13f), (TreeKind.WhitePine, 0.03f), (TreeKind.ScrubPine, 0.04f),
            (TreeKind.Dogwood, 0.04f), (TreeKind.Sycamore, 0.02f));
        private static readonly Mix MountainMix = new Mix((TreeKind.Oak, 0.27f), (TreeKind.Maple, 0.15f), (TreeKind.Tulip, 0.21f),
            (TreeKind.Hackberry, 0.04f), (TreeKind.Cedar, 0.04f), (TreeKind.WhitePine, 0.09f), (TreeKind.ScrubPine, 0.13f),
            (TreeKind.Dogwood, 0.05f), (TreeKind.Sycamore, 0.02f));
        private static readonly Mix EdgeMix = new Mix((TreeKind.Cedar, 0.42f), (TreeKind.Hackberry, 0.18f), (TreeKind.Oak, 0.14f),
            (TreeKind.Maple, 0.08f), (TreeKind.ScrubPine, 0.10f), (TreeKind.Dogwood, 0.08f));
        private static readonly Mix WetMix = new Mix((TreeKind.Sycamore, 0.42f), (TreeKind.Maple, 0.28f), (TreeKind.Hackberry, 0.2f),
            (TreeKind.Oak, 0.1f));
        private static readonly Mix GroundsMix = new Mix((TreeKind.Oak, 0.28f), (TreeKind.Maple, 0.24f), (TreeKind.Sycamore, 0.06f),
            (TreeKind.Tulip, 0.08f), (TreeKind.WhitePine, 0.13f), (TreeKind.Spruce, 0.06f), (TreeKind.Pear, 0.07f), (TreeKind.Hackberry, 0.08f));
        private static readonly Mix CemeteryMix = new Mix((TreeKind.Cedar, 0.42f), (TreeKind.Oak, 0.25f), (TreeKind.Maple, 0.13f),
            (TreeKind.Spruce, 0.08f), (TreeKind.WhitePine, 0.08f), (TreeKind.Dogwood, 0.04f));
        private static readonly Mix RowMix = new Mix((TreeKind.Cedar, 0.50f), (TreeKind.Hackberry, 0.22f), (TreeKind.Oak, 0.12f),
            (TreeKind.Maple, 0.06f), (TreeKind.ScrubPine, 0.04f), (TreeKind.Dogwood, 0.06f));
        private static readonly Mix PastureMix = new Mix((TreeKind.Oak, 0.6f), (TreeKind.Hackberry, 0.15f), (TreeKind.Maple, 0.1f),
            (TreeKind.Cedar, 0.15f));
        private static readonly Mix OldFieldMix = new Mix((TreeKind.Cedar, 0.66f), (TreeKind.ScrubPine, 0.14f), (TreeKind.Dogwood, 0.1f),
            (TreeKind.Hackberry, 0.1f));
        private static readonly Mix StreetMix = new Mix((TreeKind.Maple, 0.45f), (TreeKind.Oak, 0.24f), (TreeKind.Pear, 0.15f),
            (TreeKind.Hackberry, 0.1f), (TreeKind.Sycamore, 0.06f));

        /// <summary>One site's planting: where it stands, what's been planted and the scratch lists.</summary>
        private sealed class Planting
        {
            public readonly Site Site;
            public readonly NaturePlan Plan;
            public readonly bool Lite;
            public readonly int Max, MaxRing;
            public int Count, Ring;
            public string Diag = "";
            public readonly List<NaturePlan.CellPoint> Cells = new List<NaturePlan.CellPoint>(512);
            public readonly List<NaturePlan.Station> Stations = new List<NaturePlan.Station>(128);
            // The scenery ring's trees, by 2.5 m cell (the ring has no occupancy grid).
            private readonly HashSet<long> _ring = new HashSet<long>();

            public Planting(Site site, NaturePlan plan)
            {
                Site = site;
                Plan = plan;
                Lite = plan.Lite;
                Max = Mathf.Max(520, Mathf.RoundToInt(2600f * site.AreaShare));
                MaxRing = site.Chunked ? 0 : 1600;
            }

            public bool RingFree(Vector2 p)
            {
                int cx = Mathf.FloorToInt(p.x / 2.5f), cz = Mathf.FloorToInt(p.y / 2.5f);
                for (int z = cz - 1; z <= cz + 1; z++)
                    for (int x = cx - 1; x <= cx + 1; x++)
                        if (_ring.Contains(NaturePlan.Combine(x, z))) return false;
                return true;
            }

            public void RingTake(Vector2 p) => _ring.Add(NaturePlan.Combine(Mathf.FloorToInt(p.x / 2.5f), Mathf.FloorToInt(p.y / 2.5f)));

            // The trees planted inside the square, by metre cell, as the occupancy grid has them: what the yards test where
            // the grid can't tell a tree from the house's own bounding box.
            private readonly HashSet<long> _trees = new HashSet<long>();

            public bool TreeFree(Vector2 at, float room)
            {
                int r = Mathf.CeilToInt(room), cx = Mathf.FloorToInt(at.x), cz = Mathf.FloorToInt(at.y);
                for (int z = cz - r; z <= cz + r; z++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (_trees.Contains(NaturePlan.Combine(x, z))) return false;
                return true;
            }

            public void TreeTake(Vector2 at, float room)
            {
                int r = Mathf.CeilToInt(room), cx = Mathf.FloorToInt(at.x), cz = Mathf.FloorToInt(at.y);
                for (int z = cz - r; z <= cz + r; z++)
                    for (int x = cx - r; x <= cx + r; x++)
                        _trees.Add(NaturePlan.Combine(x, z));
            }
        }

        internal static IEnumerable<string> Build(Site site)
        {
            NaturePlan plan = NaturePlan.For(site);
            yield return "nature plan";
            if (!plan.Lite)
            {
                VehiclePass.PlanDriveways(site, plan);
                yield return "driveway plan";
            }
            var p = new Planting(site, plan);
            int before = p.Count;
            foreach (string step in MappedTrees(p)) yield return step;
            foreach (string step in TreeRows(p)) yield return step;
            Diag(p, "mapped", ref before);
            yield return "mapped trees";
            foreach (string step in Yards(p)) yield return step;
            Diag(p, "yards", ref before);
            foreach (string step in Grounds(p)) yield return step;
            CemeteryTrees(p);
            Diag(p, "grounds", ref before);
            yield return "grounds trees";
            foreach (string step in StreetTrees(p)) yield return step;
            Diag(p, "street", ref before);
            yield return "street trees";
            foreach (string step in Creeks(p)) yield return step;
            foreach (string step in RoadRows(p)) yield return step;
            foreach (string step in FieldEdges(p)) yield return step;
            Diag(p, "rows", ref before);
            yield return "fence rows";
            foreach (string step in Sweep(p)) yield return step;
            Diag(p, "sweep", ref before);
            if (!site.Chunked) Debug.Log("[NatureDiag] trees " + p.Count + " (ring " + p.Ring + "): " + p.Diag);
        }

        // Temporary: what each source planted, for tuning.
        private static void Diag(Planting p, string what, ref int before)
        {
            p.Diag += what + " " + (p.Count - before) + ", ";
            before = p.Count;
        }

        #region Planting
        /// <summary>
        /// Plants a tree at <paramref name="at"/> when there's room: inside the playable square with a trunk collider (on a
        /// Full site), its spot taken and the tree fitted to its room (see <see cref="Fit"/>); in a fixed map's scenery ring
        /// as cheap scenery. <paramref name="room"/> is the clear radius it wants; <paramref name="checkGrid"/> false when
        /// the caller has checked the spot itself. Never under a mapped power line.
        /// </summary>
        private static bool Plant(Planting p, Vector2 at, TreeKind kind, float height, long key, float room, TreeTier tier, bool checkGrid = true)
        {
            Site site = p.Site;
            if (p.Plan.UnderPowerLine(at)) return false;
            float yaw = MapStyle.Unit(key, SaltYaw) * 360f;
            if (p.Plan.InSquare(at, 0f))
            {
                if (p.Count >= p.Max) return false;
                if (checkGrid && !site.Free(at, room)) return false;
                var fit = default(TreeRoom);
                if (p.Lite) tier = TreeTier.Blob;
                else if (!Fit(p.Plan, at, kind, yaw, ref height, ref fit)) return false;
                Vector3 at3 = To3(at, 0f);
                RaidMesh m = site.Town.Out.Solid(at3);
                m.SetFrame(at3, yaw);
                TreeKit.Tree(m, kind, height, MapStyle.Rng(key, SaltShape), tier, fit);
                m.ResetFrame();
                if (!p.Lite)
                {
                    float solid = TreeKit.SolidRadius(kind, height) * 2f;
                    site.Town.PropCollider(at3 + Vector3.up * 1.5f, new Vector3(solid, 3f, solid), 0f);
                    if (tier == TreeTier.Full && TreeKit.Conifer(kind)) GroundPass.TreeWell(site, at, TreeKit.Reach(kind, height) * 0.8f);
                }
                site.Take(at, room);
                p.TreeTake(at, room);
                p.Count++;
                site.Trees++;
                return true;
            }
            if (p.Ring >= p.MaxRing || !p.Plan.InRing(at) || !p.RingFree(at)) return false;
            if (p.Plan.RoadClearance(at, 2f) < 1.2f || p.Plan.WallDistance(at, room + 1f) < room) return false;
            Land land = p.Plan.LandAt(at);
            if (land == Land.Water || land == Land.Lot || land == Land.Pitch) return false;
            Vector3 far = To3(at, 0f);
            RaidMesh fm = site.Town.Far.Solid(far);
            fm.SetFrame(far, yaw);
            TreeKit.Tree(fm, kind, height, MapStyle.Rng(key, SaltShape), TreeTier.Lite);
            fm.ResetFrame();
            p.RingTake(at);
            p.Ring++;
            site.Trees++;
            return true;
        }

        /// <summary>
        /// Fits a tree inside the square to its room: a conifer cut down until its skirt keeps off the nearest wall and
        /// it tops out short of the wires hanging near it; a hardwood topped below a wire hanging straight over its trunk,
        /// and its limbs cut back short of the nearest wall and wire otherwise (<paramref name="room"/>, in the tree's own
        /// frame, turned <paramref name="yaw"/>). False when what's left isn't worth planting.
        /// </summary>
        private static bool Fit(NaturePlan plan, Vector2 at, TreeKind kind, float yaw, ref float height, ref TreeRoom room)
        {
            bool conifer = TreeKit.Conifer(kind);
            float reach = TreeKit.Reach(kind, height) + 0.5f;
            if (plan.NearestWall(at, reach, out Vector2 wallDir, out float wall))
            {
                if (conifer) height = Mathf.Min(height, (wall - 0.35f) / TreeKit.Skirt(kind));
                else room.Add(Azimuth(wallDir) - yaw, wall);
            }
            if (plan.WireNear(at, reach, height + 0.5f, out Vector2 wireDir, out float wire, out float wireY))
            {
                if (conifer) height = Mathf.Min(height, wireY - 0.6f + wire / TreeKit.Skirt(kind));
                else if (wire < 1.8f) height = Mathf.Min(height, wireY - 0.8f);
                else room.Add(Azimuth(wireDir) - yaw, wire);
            }
            return height >= TreeKit.Shortest(kind);
        }

        /// <summary>A plan direction's bearing: degrees clockwise from +z.</summary>
        private static float Azimuth(Vector2 dir) => Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

        /// <summary>A bush at <paramref name="at"/> (inside the square, on a Full site only): no collider, a small spot taken.</summary>
        private static bool Bush(Planting p, Vector2 at, ShrubKind kind, float size, long key, bool checkGrid = true)
        {
            if (p.Lite || !p.Plan.InSquare(at, 0.5f)) return false;
            if (checkGrid && !p.Site.Free(at, 0.5f)) return false;
            Vector3 at3 = To3(at, 0f);
            RaidMesh m = p.Site.Town.Out.Solid(at3);
            m.SetFrame(at3, MapStyle.Unit(key, SaltYaw) * 360f);
            TreeKit.Shrub(m, kind, size, MapStyle.Rng(key, SaltShape));
            m.ResetFrame();
            p.Site.Take(at, 0.5f);
            return true;
        }

        /// <summary>A height for a tree of the kind, its own.</summary>
        private static float HeightOf(TreeKind kind, long key, float scale = 1f) => TreeKit.Height(kind, MapStyle.Rng(key, SaltHeight)) * scale;
        #endregion

        #region Mapped trees and tree rows
        // Every tree mapped as a point, where it stands (unless it stands on a road, in a building or in the water).
        private static IEnumerable<string> MappedTrees(Planting p)
        {
            foreach (Node n in p.Site.Area.Nodes)
            {
                if (n.Data.kind != "tree") continue;
                long key = n.Data.id;
                if (p.Plan.RoadClearance(n.At, 1f) < 0.3f || p.Plan.WallDistance(n.At, 1f) < 0.6f) continue;
                Land land = p.Plan.LandAt(n.At);
                if (land == Land.Water || land == Land.Lot) continue;
                TreeKind kind = Named(n.Data.name, out TreeKind named) ? named
                    : p.Plan.Urban(n.At) > 0.05f ? StreetMix.Pick(MapStyle.Unit(key, SaltKind)) : GroundsMix.Pick(MapStyle.Unit(key, SaltKind));
                if (p.Plan.InSquare(n.At, 0f) && (!p.Site.FreeBeside(n.At, 0.4f) || p.Plan.NearDoor(n.At, 1.6f))) continue;
                Plant(p, n.At, kind, HeightOf(kind, key), key, 0.8f, TreeTier.Full, false);
                if (p.Site.StepDue) yield return "mapped trees";
            }
        }

        // The odd named tree says what it is.
        private static bool Named(string name, out TreeKind kind)
        {
            kind = TreeKind.Oak;
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            if (n.Contains("oak")) kind = TreeKind.Oak;
            else if (n.Contains("maple")) kind = TreeKind.Maple;
            else if (n.Contains("sycamore")) kind = TreeKind.Sycamore;
            else if (n.Contains("cedar")) kind = TreeKind.Cedar;
            else if (n.Contains("spruce")) kind = TreeKind.Spruce;
            else if (n.Contains("pine")) kind = TreeKind.WhitePine;
            else if (n.Contains("poplar")) kind = TreeKind.Tulip;
            else return false;
            return true;
        }

        // The mapped tree rows: a tree every five or six metres, a little off the line, mostly cedars.
        private static IEnumerable<string> TreeRows(Planting p)
        {
            foreach (Line l in p.Site.Area.Barriers)
            {
                if (l.Data.kind != "tree_row") continue;
                p.Plan.Stations(l.Pts, 5.5f, p.Stations);
                foreach (NaturePlan.Station s in p.Stations)
                {
                    long key = NaturePlan.Combine(l.Data.id, s.K);
                    Vector2 side = new Vector2(-s.Dir.y, s.Dir.x) * ((MapStyle.Unit(key, SaltJitter) - 0.5f) * 1.2f);
                    Vector2 at = s.At + side + s.Dir * ((MapStyle.Unit(key, SaltJitter + 1) - 0.5f) * 1.5f);
                    TreeKind kind = RowMix.Pick(MapStyle.Unit(key, SaltKind));
                    Plant(p, at, kind, HeightOf(kind, key, 0.9f), key, 0.9f, TreeTier.Full);
                }
                if (p.Site.StepDue) yield return "tree rows";
            }
        }
        #endregion

        #region The country: creeks, roadside and field fence rows
        // Sycamores, maples and hackberries along both banks of the creeks and rivers, every eight metres or so.
        private static IEnumerable<string> Creeks(Planting p)
        {
            foreach (Line l in p.Site.Area.Lines)
            {
                if (l.IsRailway || l.Data.tunnel) continue;
                float half = Mathf.Clamp(l.Data.width, 1f, 60f) * 0.5f;
                p.Plan.Stations(l.Pts, 8f, p.Stations);
                foreach (NaturePlan.Station s in p.Stations)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        long key = NaturePlan.Combine(NaturePlan.Combine(l.Data.id, s.K), side);
                        float urban = p.Plan.Urban(s.At);
                        if (MapStyle.Unit(key, SaltCreek) > (urban > 0.12f ? 0.35f : 0.6f)) continue;
                        Vector2 normal = new Vector2(-s.Dir.y, s.Dir.x) * side;
                        Vector2 at = s.At + normal * (half + 1.5f + MapStyle.Unit(key, SaltJitter) * 3f)
                            + s.Dir * ((MapStyle.Unit(key, SaltJitter + 1) - 0.5f) * 4f);
                        if (MapStyle.Unit(key, SaltUnder) < 0.18f)
                        {
                            Bush(p, at, ShrubKind.Thicket, 1.2f + MapStyle.Unit(key, SaltUnder + 1), key);
                            continue;
                        }
                        TreeKind kind = WetMix.Pick(MapStyle.Unit(key, SaltKind));
                        Plant(p, at, kind, HeightOf(kind, key), key, 1.0f, TreeTier.Full);
                    }
                if (p.Site.StepDue) yield return "creek trees";
            }
        }

        // Out of town the country roads run between fence rows: cedars, hackberries and the odd oak a few metres past the
        // ditch, in runs with gaps between them, now on one side, now on both.
        private static IEnumerable<string> RoadRows(Planting p)
        {
            foreach (Street st in p.Site.Area.Streets)
            {
                if (st.Data.tunnel || st.Data.bridge || st.Rank < 2 || st.Rank > 5 || NaturePlan.FootKind(st.Data.kind)) continue;
                p.Plan.Stations(st.Pts, 6.5f, p.Stations);
                foreach (NaturePlan.Station s in p.Stations)
                {
                    if (p.Plan.Urban(s.At) > 0.035f) continue;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        long key = NaturePlan.Combine(NaturePlan.Combine(st.Data.id, s.K), side);
                        Vector2 normal = new Vector2(-s.Dir.y, s.Dir.x) * side;
                        Vector2 at = s.At + normal * (RoadPass.EdgeOffset(st, side) + 4.5f + MapStyle.Unit(key, SaltJitter) * 3f)
                            + s.Dir * ((MapStyle.Unit(key, SaltJitter + 1) - 0.5f) * 2f);
                        // Runs: where the noise along the road says so, and not everywhere even there.
                        if (p.Plan.NoiseAt(at, 140.0, SaltRoadRow + side) < 0.5f || MapStyle.Unit(key, SaltGap) < 0.18f) continue;
                        Land land = p.Plan.LandAt(at);
                        if (land != Land.None && land != Land.Farmland && land != Land.Meadow && land != Land.Grass) continue;
                        if (p.Plan.WallDistance(at, 8f) < 6f) continue;
                        if (MapStyle.Unit(key, SaltUnder) < 0.2f)
                        {
                            Bush(p, at, ShrubKind.Thicket, 1.1f + MapStyle.Unit(key, SaltUnder + 1) * 0.8f, key);
                            continue;
                        }
                        TreeKind kind = RowMix.Pick(MapStyle.Unit(key, SaltKind));
                        Plant(p, at, kind, HeightOf(kind, key, 0.85f), key, 0.9f, TreeTier.Full);
                    }
                    if (p.Site.StepDue) yield return "fence rows";
                }
            }
        }

        // The fields' edges grow fence rows: every edge of a field, pasture or farmyard that isn't a cut made by the map's
        // regions or the square, in runs with gaps. Fields that share an edge sow the same trees on it (the spot is the
        // key), so it gets one row.
        private static IEnumerable<string> FieldEdges(Planting p)
        {
            var edge = new List<Vector2>(2) { Vector2.zero, Vector2.zero };
            foreach (Patch pa in p.Site.Area.Areas)
            {
                Land land = NaturePlan.LandOf(pa.Data.kind);
                if (land != Land.Farmland && land != Land.Meadow && land != Land.Farmyard) continue;
                List<Vector2> pts = pa.Pts;
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
                    if (p.Plan.CutVertex(a) && p.Plan.CutVertex(b) && OnOneLine(a, b)) continue;
                    edge[0] = a;
                    edge[1] = b;
                    p.Plan.Stations(edge, 6f, p.Stations);
                    foreach (NaturePlan.Station s in p.Stations)
                    {
                        bool alongX = (s.K & 1) == 0;
                        long across = (long)System.Math.Round((alongX ? p.Plan.AbsZ(s.At.y) : p.Plan.AbsX(s.At.x)) / 3.0);
                        long key = NaturePlan.Combine(s.K, across);
                        if (p.Plan.NoiseAt(s.At, 120.0, SaltField) < 0.42f || MapStyle.Unit(key, SaltGap) < 0.25f) continue;
                        if (p.Plan.Urban(s.At) > 0.05f || p.Plan.RoadClearance(s.At, 4f) < 3f) continue;
                        Vector2 at = s.At + new Vector2(-s.Dir.y, s.Dir.x) * ((MapStyle.Unit(key, SaltJitter) - 0.5f) * 1.4f);
                        TreeKind kind = RowMix.Pick(MapStyle.Unit(key, SaltKind));
                        Plant(p, at, kind, HeightOf(kind, key, 0.85f), key, 0.9f, TreeTier.Full);
                    }
                    if (p.Site.StepDue) yield return "fence rows";
                }
            }
        }

        // Both ends on one line along x or z: an edge of a cut, not a hedge.
        private static bool OnOneLine(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) < 0.05f || Mathf.Abs(a.y - b.y) < 0.05f;
        #endregion

        #region The sweep: woods, grounds and open country
        /// <summary>
        /// Walks the site's lattice once, cell by cell, sowing what the land there grows: the mapped woods thick (with
        /// thickets, logs, stumps and stones under them, cedars crowding their edges and sycamores near water), scrub and
        /// wetland thinner, parks, cemeteries, school grounds, golf courses and lawns with big scattered trees, a lone
        /// oak in a pasture now and then, the region's own woods over unmapped open country away from the houses, and red
        /// cedars coming up in old fields.
        /// </summary>
        private static IEnumerable<string> Sweep(Planting p)
        {
            NaturePlan plan = p.Plan;
            float cell = p.Lite ? WoodsCellLite : WoodsCell;
            float thin = (cell * cell) / (WoodsCell * WoodsCell);   // a coarser lattice sows each cell more often
            float reach = plan.Site.Chunked ? plan.Half : plan.Reach;
            Mix forest = plan.Woodland > 0.6f ? MountainMix : ForestMix;
            const float band = 40f;
            int rows = Mathf.CeilToInt(reach * 2f / band);
            for (int row = 0; row < rows; row++)
            {
                float z0 = -reach + row * band, z1 = Mathf.Min(reach, z0 + band);
                plan.Cells(Rect.MinMaxRect(-reach, z0, reach, z1), cell, 0.85f, SaltSow, p.Cells);
                foreach (NaturePlan.CellPoint c in p.Cells)
                {
                    if (c.At.y >= z1 && z1 < reach) continue;   // the next band's
                    Sow(p, c, forest, thin);
                    if (p.Site.StepDue) yield return "woods";
                }
                yield return "woods";
            }
        }

        private static void Sow(Planting p, NaturePlan.CellPoint c, Mix forest, float thin)
        {
            NaturePlan plan = p.Plan;
            Vector2 at = c.At;
            long key = c.Key;
            float u = MapStyle.Unit(key, SaltSow + 7);
            Land land = plan.LandAt(at);
            switch (land)
            {
                case Land.Forest:
                    Woods(p, at, key, forest, 0.95f, true);
                    return;
                case Land.Scrub:
                    if (u < 0.3f * thin) Bush(p, at, ShrubKind.Thicket, 1.2f + MapStyle.Unit(key, SaltUnder), key);
                    else if (u < 0.62f * thin) Grow(p, at, key, OldFieldMix, 0.8f, 0.9f);
                    return;
                case Land.Wetland:
                    if (u < 0.22f * thin) Bush(p, at, ShrubKind.Thicket, 1.4f + MapStyle.Unit(key, SaltUnder), key);
                    else if (u < 0.45f * thin) Grow(p, at, key, WetMix, 1f, 1.0f);
                    return;
                case Land.Orchard:
                    if (u < 0.75f * thin) Plant(p, at, TreeKind.Pear, HeightOf(TreeKind.Pear, key, 0.75f), key, 1.0f, TreeTier.Full);
                    return;
                case Land.Park:
                    if (u < 0.13f * thin) Grow(p, at, key, GroundsMix, 1f, 2.0f);
                    return;
                case Land.Cemetery:
                    // Inside a Full site's square the cemetery's own plots get its trees (CemeteryTrees).
                    if (u < 0.17f * thin && (p.Lite || !plan.InSquare(at, 0f))) Grow(p, at, key, CemeteryMix, 1f, 1.6f);
                    return;
                case Land.School:
                case Land.Grass:
                    if (u < 0.045f * thin) Grow(p, at, key, GroundsMix, 1f, 2.0f);
                    return;
                case Land.Golf:
                    if (u < 0.14f * thin && plan.NoiseAt(at, 45.0, SaltSow + 3) > 0.55f) Grow(p, at, key, GroundsMix, 1f, 2.0f);
                    return;
                case Land.Meadow:
                case Land.Farmyard:
                    if (u < (land == Land.Farmyard ? 0.06f : 0.012f) * thin) Grow(p, at, key, PastureMix, 1.05f, 2.2f);
                    return;
                case Land.None:
                    if (plan.Wooded(at, out float depth))
                    {
                        if (plan.WallDistance(at, 30f) < 26f || plan.RoadClearance(at, 6f, true) < 3.5f) return;
                        Woods(p, at, key, depth < 0.05f ? EdgeMix : forest, 0.9f, false);
                        return;
                    }
                    if (u < 0.09f * thin && plan.Urban(at) < 0.025f && plan.NoiseAt(at, 260.0, SaltOldField) > 0.66f
                        && plan.WallDistance(at, 16f) > 14f && plan.RoadClearance(at, 6f, true) > 4f)
                        Grow(p, at, key, OldFieldMix, 0.9f, 1.0f);
                    return;
                default:
                    return;
            }
        }

        // A tree from a mix (scaled in height), with the room it wants.
        private static void Grow(Planting p, Vector2 at, long key, Mix mix, float scale, float room)
        {
            TreeKind kind = mix.Pick(MapStyle.Unit(key, SaltKind));
            Plant(p, at, kind, HeightOf(kind, key, scale), key, room, TreeTier.Full);
        }

        // One cell of woods: a tree most times (sycamores and maples near water, cedars at the edge), and under the trees
        // now and then a thicket, a fallen log, a stump or a limestone outcrop.
        private static void Woods(Planting p, Vector2 at, long key, Mix mix, float chance, bool mapped)
        {
            float u = MapStyle.Unit(key, SaltSow + 7);
            if (u >= chance) return;
            if (mapped && IsEdge(p.Plan, at, Land.Forest)) mix = EdgeMix;
            if (p.Plan.WaterDistance(at, 16f) < 12f) mix = WetMix;
            TreeKind kind = mix.Pick(MapStyle.Unit(key, SaltKind));
            Plant(p, at, kind, HeightOf(kind, key, 1.05f), key, 0.8f, TreeTier.Wood);
            if (p.Lite) return;
            float under = MapStyle.Unit(key, SaltUnder);
            Vector2 aside = at + new Vector2(MapStyle.Unit(key, SaltUnder + 1) - 0.5f, MapStyle.Unit(key, SaltUnder + 2) - 0.5f) * 4f;
            if (under < 0.16f) Bush(p, aside, ShrubKind.Thicket, 1.0f + MapStyle.Unit(key, SaltUnder + 3) * 1.2f, key + 1);
            else if (under < 0.205f) Deadwood(p, aside, key, 0);
            else if (under < 0.225f) Deadwood(p, aside, key, 1);
            else if (under < 0.24f) Deadwood(p, aside, key, 2);
        }

        // Within five metres of land that isn't the same.
        private static bool IsEdge(NaturePlan plan, Vector2 at, Land land)
            => plan.LandAt(at + new Vector2(5f, 0f)) != land || plan.LandAt(at - new Vector2(5f, 0f)) != land
            || plan.LandAt(at + new Vector2(0f, 5f)) != land || plan.LandAt(at - new Vector2(0f, 5f)) != land;

        // A fallen log (0), a stump (1) or a limestone outcrop (2) on the woods' floor: no collider, a little spot taken.
        private static void Deadwood(Planting p, Vector2 at, long key, int what)
        {
            if (!p.Plan.InSquare(at, 1f) || !p.Site.Free(at, 0.8f)) return;
            System.Random rng = MapStyle.Rng(key, SaltShape + 9);
            Vector3 at3 = To3(at, 0f);
            RaidMesh m = p.Site.Town.Out.Solid(at3);
            m.SetFrame(at3, MapStyle.Unit(key, SaltYaw + 9) * 360f);
            if (what == 0) TreeKit.Log(m, 2.5f + (float)rng.NextDouble() * 3.5f, 0.18f + (float)rng.NextDouble() * 0.16f, rng);
            else if (what == 1) TreeKit.Stump(m, 0.22f + (float)rng.NextDouble() * 0.2f, rng);
            else TreeKit.Rock(m, 0.6f + (float)rng.NextDouble() * 0.9f, rng);
            m.ResetFrame();
            p.Site.Take(at, what == 0 ? 1.5f : 0.6f);
        }
        #endregion
    }
}
