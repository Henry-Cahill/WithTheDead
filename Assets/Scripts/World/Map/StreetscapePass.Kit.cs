using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What the streetscape's steps share while one site is built: the site's context (how built-up a spot is, where the
    // roads meet), where each kind of thing is drawn (inside the playable square with colliders, past it as scenery), the
    // site's overhead wires, its sign atlas, the utility poles the steps hang things on, and the looks they share.
    internal static partial class StreetscapePass
    {
        #region Looks
        internal static readonly Color Snow = new Color(0.86f, 0.89f, 0.95f);
        internal static readonly Color WireBlack = new Color(0.07f, 0.07f, 0.075f);
        internal static readonly Color[] PoleWoods =
        {
            new Color(0.27f, 0.21f, 0.16f), new Color(0.33f, 0.26f, 0.19f), new Color(0.42f, 0.37f, 0.32f),
        };

        internal static Surf Matte(Color c) => DecorPalette.Matte(c);
        internal static Surf Gloss(Color c) => DecorPalette.Gloss(c);
        internal static Surf Metal(Color c) => DecorPalette.Metal(c);
        internal static Surf SnowSurf => DecorPalette.Matte(Snow);
        internal static Surf Galvanised => DecorPalette.Metal(new Color(0.60f, 0.62f, 0.64f));
        internal static Surf DarkMetal => DecorPalette.Matte(new Color(0.15f, 0.16f, 0.17f));
        internal static readonly Color LamppostColour = new Color(0.48f, 0.38f, 0.30f); // weather‑worn wooden‑paint
        private static Color StreetPoleHue(Street s)
        {
            // Use the stable hash of the street’s id to pick one of three subtle tints.
            int h = (int)(s.Data.id % 3);
            return h switch
            {
                0 => new Color(0.45f, 0.35f, 0.28f), // darker
                1 => new Color(0.52f, 0.40f, 0.33f), // mid‑tone (default)
                _ => new Color(0.58f, 0.44f, 0.38f), // lighter
            };
        }
        internal static Surf LamppostSurf => DecorPalette.Matte(LamppostColour);
        internal static Surf Concrete => DecorPalette.Matte(new Color(0.60f, 0.59f, 0.56f));
        #endregion

        /// <summary>The streetscape's view of one site while it's built.</summary>
        internal sealed class Ctx
        {
            public readonly Site Site;
            /// <summary>Full detail: everything. Lite: only the landmarks seen from afar.</summary>
            public readonly bool Full;
            /// <summary>The playable square's half side.</summary>
            public readonly float Half;
            /// <summary>How far the site's data reaches: the square itself for a streamed piece, the scenery ring's outer
            /// edge for a fixed map.</summary>
            public readonly float Reach;
            /// <summary>The site's middle in the world (x, z): world-anchored choices agree across streamed pieces.</summary>
            public readonly Vector2 Origin;
            public readonly Density Density;
            public readonly WireBatch Wires = new WireBatch();
            public readonly List<Pole> Poles = new List<Pole>();
            /// <summary>The street lights standing on their own, planned before they're drawn.</summary>
            public readonly List<LampPlan> Lamps = new List<LampPlan>();
            /// <summary>Every street light planned so far (on poles too), for keeping the next ones apart.</summary>
            public readonly List<LampPlan> AllLamps = new List<LampPlan>();
            /// <summary>The burning lamps drawn so far, made <see cref="StreetLamp"/>s together at the end (nearest the
            /// site's middle first, for the edit-mode lights' cap).</summary>
            public readonly List<Burning> Burning = new List<Burning>();
            /// <summary>Where two or more roads meet, with their road arms only.</summary>
            public readonly List<Crossing> Crossings = new List<Crossing>();
            private NameAtlas _names;
            private RoadGrid _roads;
            private BuildingGrid _blocks;
            private Dictionary<Footprint, EnterableBuilding> _enterables;

            public Ctx(Site site)
            {
                Site = site;
                Full = site.Detail == Detail.Full;
                Half = site.Half;
                Reach = site.Chunked ? site.Half : site.Half + SceneryReach;
                Origin = new Vector2(site.WorldOffset.x, site.WorldOffset.z);
                Density = new Density(site.Area, Reach);
                if (Full) FindCrossings();
            }

            /// <summary>The site's sign atlas (street names, route shields, painted names), made the first time it's asked
            /// for.</summary>
            public NameAtlas Names => _names ??= new NameAtlas(Site);

            /// <summary>The carriageways as the roads drew them, for testing spots the occupancy grid can't (the square's
            /// edge, the scenery ring).</summary>
            public RoadGrid Roads => _roads ??= new RoadGrid(Site.Area);

            /// <summary>The buildings' outlines (a streamed piece's neighbours' too), for keeping wires off the roofs.</summary>
            public BuildingGrid Blocks => _blocks ??= new BuildingGrid(Site.Area);

            /// <summary>The enterable building on a footprint (its storeys), or null.</summary>
            public EnterableBuilding EnterableOf(Footprint f)
            {
                if (_enterables == null)
                {
                    _enterables = new Dictionary<Footprint, EnterableBuilding>(Site.Enterables.Count);
                    foreach (EnterableBuilding e in Site.Enterables)
                        if (e.Footprint != null) _enterables[e.Footprint] = e;
                }
                return f != null && _enterables.TryGetValue(f, out EnterableBuilding b) ? b : null;
            }

            /// <summary>True where a roadside thing may stand: inside the square free of everything and of every road's
            /// surface (<see cref="Site.FreeBeside"/>); at the edge or past it, off the carriageways as drawn and out of
            /// the buildings.</summary>
            public bool Roadside(Vector2 p, float radius)
            {
                if (Inside(p) && Mathf.Abs(p.x) < Half - radius - 0.05f && Mathf.Abs(p.y) < Half - radius - 0.05f)
                    return Site.FreeBeside(p, radius);
                return Roads.Clearance(p, radius + 1f) >= radius && !Blocks.Contains(p, radius);
            }

            /// <summary>True inside the playable square, where things get colliders and claim their spot.</summary>
            public bool Inside(Vector2 p) => Mathf.Abs(p.x) < Half && Mathf.Abs(p.y) < Half;

            /// <summary>True within the site's data (the square, or the scenery ring past it).</summary>
            public bool Within(Vector2 p) => Mathf.Abs(p.x) < Reach && Mathf.Abs(p.y) < Reach;

            /// <summary>The batch for something standing on the ground at <paramref name="p"/>.</summary>
            public RaidMesh Low(Vector2 p) => Inside(p) ? Site.Town.Out.Solid(To3(p, 0f)) : Site.Town.Far.Solid(To3(p, 0f));

            /// <summary>The batch for what's above head height at <paramref name="p"/> (pole tops, wires' fittings, lamp
            /// heads, signal heads): shown from the isometric camera, never baked into the NavMesh.</summary>
            public RaidMesh High(Vector2 p) => Inside(p) ? Site.Roofs.At(To3(p, 0f)) : Site.Town.Far.Solid(To3(p, 0f));

            /// <summary>A solid box (a pole, a post, a box on the sidewalk), inside the playable square only.</summary>
            public void Collider(Vector2 p, float y0, Vector3 size, float yaw)
            {
                if (!Inside(p)) return;
                Site.Town.PropCollider(new Vector3(p.x, y0 + size.y * 0.5f, p.y), size, yaw);
            }

            /// <summary>A small prop's collider, turned with it (the map's prop colliders are merged into one mesh, so a turned
            /// box costs no more than a square one).</summary>
            public void Block(Vector2 p, Vector3 size, float yaw) => Collider(p, 0f, size, yaw);

            /// <summary>A world-anchored key for a spot (to half a metre), so a thing two streamed pieces both build comes
            /// out the same in each.</summary>
            public long KeyOf(Vector2 local)
            {
                Vector2 w = local + Origin;
                return ((long)Mathf.RoundToInt(w.x * 2f) << 32) ^ (uint)Mathf.RoundToInt(w.y * 2f);
            }

            /// <summary>A world-anchored number in [0, 1) for a spot and a salt.</summary>
            public float Unit(Vector2 local, int salt) => MapStyle.Unit(KeyOf(local), salt);

            // A crossing is where three or more road arms meet: two pieces meeting end to end (a road changing its name,
            // or its mapped pieces simply joining) are no junction to sign or light.
            /// <summary>Detects junctions where three or more distinct road arms intersect, recording their arms and ranks.</summary>
            private void FindCrossings()
            {
                // Prepare a reusable list to track distinct street names at this junction.
                var names = new List<string>(6);
                // Examine each junction the map defines.
                foreach (Junction j in Site.Junctions)
                {
                    // Initialise a Crossing record for this junction.
                    var c = new Crossing { At = j.At, Junction = j };
                    // Reset the street‑name list for this new crossing.
                    names.Clear();
                    // Iterate over each arm of the junction to collect valid road arms.
                    for (int i = 0; i < j.Arms.Count; i++)
                    {
                        JunctionArm a = j.Arms[i];
                        // Determine the road's importance; skip non‑roads.
                        int rank = RoadRank(a.Street);
                        if (rank < 0) continue;
                        // Record this arm and its index for later sign/traffic‑light placement.
                        c.Arms.Add(a);
                        c.ArmIndex.Add(i);
                        // Track distinct street identifiers so we only accept true junctions (multiple road names).
                        string key = NameKey(a.Street);
                        if (!names.Contains(key)) names.Add(key);
                        // Keep the highest road rank seen for this crossing (used for signage priority).
                        if (rank > c.TopRank) c.TopRank = rank;
                    }
                    // After collecting arms, ensure this is a real junction (>=3 arms and >=2 distinct street names).
                    if (c.Arms.Count < 3 || names.Count < 2) continue;
                    Crossings.Add(c);
                }
            }

            /// <summary>The crossing nearest <paramref name="p"/> within <paramref name="within"/> metres, or null.</summary>
            public Crossing NearestCrossing(Vector2 p, float within)
            {
                // Initialise the best candidate as none and set the maximum squared distance we’ll accept.
                Crossing best = null;
                float bestD = within * within;
                // For each known crossing compute squared distance and keep the nearest within the limit.
                foreach (Crossing c in Crossings)
                {
                    float d = (c.At - p).sqrMagnitude;
                    // Skip if this crossing is farther than the current best.
                    if (d >= bestD) continue;
                    bestD = d;
                    best = c;
                }
                return best;
            }

            /// <summary>Hands out what's built per site and can't be batched: the wires and the sign atlas.</summary>
            public IEnumerable<string> Finish()
            {
                // after all road geometry has been placed, add sidewalk lamps.
                BuildSidewalkLamps(this);
                foreach (string step in Wires.Emit(Site)) yield return step;
                _names?.Finish();
            }
        }

        /// <summary>A place where roads meet (only their road arms).</summary>
        internal sealed class Crossing
        {
            public Vector2 At;
            public Junction Junction;
            public readonly List<JunctionArm> Arms = new List<JunctionArm>();
            /// <summary>Each arm's index in <see cref="Junction"/>'s own arms (for <see cref="RoadPass.ArmSetback"/> and
            /// <see cref="RoadPass.StopLine"/>).</summary>
            public readonly List<int> ArmIndex = new List<int>();
            public int TopRank = -1;
            /// <summary>Set once something controls it (signals, stop signs), so nothing else does.</summary>
            public bool Controlled;
            /// <summary>Set once its street-name blades are up.</summary>
            public bool Named;

            /// <summary>How far from the junction's point arm <paramref name="k"/> (in <see cref="Arms"/>) leaves the
            /// junction's paved patch, as the roads shaped it.</summary>
            public float Setback(int k) => RoadPass.ArmSetback(Junction, ArmIndex[k]);

            /// <summary>The side (+1 left, -1 right of its street's own direction) of arm <paramref name="k"/>'s street
            /// that is on the right of traffic coming into the junction along it.</summary>
            public int ApproachRight(int k)
            {
                JunctionArm a = Arms[k];
                List<Vector2> pts = a.Street.Pts;
                bool forward = a.Vertex + 1 < pts.Count && Vector2.Dot(pts[a.Vertex + 1] - pts[a.Vertex], a.Dir) > 0f;
                return forward ? 1 : -1;
            }
        }

        /// <summary>A burning street light, drawn, waiting to be made a <see cref="StreetLamp"/>.</summary>
        internal struct Burning
        {
            public LampPlan Lamp;
            public Vector2 Foot;
            public float PoleHeight;
            public Vector3 Head;
        }

        /// <summary>A utility pole (or a tower) the steps hang things on.</summary>
        internal sealed class Pole
        {
            public Vector2 At;
            /// <summary>Across the line at the pole (unit): the crossarm's way.</summary>
            public Vector2 Across;
            /// <summary>From the pole towards the carriageway it stands beside (zero for poles off the roads).</summary>
            public Vector2 ToRoad;
            public float Height;
            public bool ThreePhase, Edge, Terminal, Transformer, Telephone = true, Secondary = true;
            /// <summary>The line it belongs to (poles of one line share it).</summary>
            public int Line;
            public Street Street;
            public long Key;
            /// <summary>A street light on its arm, or null.</summary>
            public LampPlan Lamp;
            /// <summary>Its guy wire's way (unit, on the ground), or zero.</summary>
            public Vector2 Guy;
        }

        /// <summary>The kinds of street light.</summary>
        internal enum LampKind { CobraOnPole, Davit, Acorn, Farm, Wood }

        /// <summary>A street light to draw: where, which way it reaches, how it looks and whether it still burns.</summary>
        internal sealed class LampPlan
        {
            public LampKind Kind;
            public Vector2 At;
            /// <summary>The way the arm reaches (towards the road), unit.</summary>
            public Vector2 Reach;
            public bool Lit, Flicker, Mercury;
            public long Key;
            /// <summary>The pole's height (lamps standing on their own).</summary>
            public float Height;
        }

        #region Streets
        /// <summary>How much a street counts as a road (links as their road; living streets as residential): 6 motorway
        /// or trunk ... 2 residential or unclassified, 1 service road or track; -1 for anything that isn't a road.</summary>
        internal static int RoadRank(Street s)
        {
            switch (s.Data.kind)
            {
                case "motorway": case "trunk": case "motorway_link": case "trunk_link": return 6;
                case "primary": case "primary_link": return 5;
                case "secondary": case "secondary_link": return 4;
                case "tertiary": case "tertiary_link": return 3;
                case "unclassified": case "residential": case "living_street": return 2;
                case "service": case "track": return 1;
                default: return -1;
            }
        }

        /// <summary>Half the carriageway's width as the roads drew it (the wider side, to the kerb face or the edge of the
        /// shoulder); for a street the road pass didn't analyse, as it would have drawn it.</summary>
        internal static float HalfWidth(Street s) => Mathf.Max(RoadPass.HalfWidth(s), MappedHalf(s));

        /// <summary>How far the carriageway reaches on one side of a street (+1 left of its points' order, -1 right).</summary>
        internal static float EdgeOf(Street s, int side) => Mathf.Max(RoadPass.EdgeOffset(s, side), MappedHalf(s));

        /// <summary>Where the roadside things (poles, signs, lamps, hydrants) stand on one side of a street, from its line:
        /// on the sidewalk behind the kerb, on the verge, or past an unkerbed edge (<see cref="RoadPass.RoadsideOffset"/>),
        /// never on the carriageway.</summary>
        internal static float Roadside(Street s, int side) => Mathf.Max(RoadPass.RoadsideOffset(s, side), EdgeOf(s, side) + 0.55f);

        /// <summary>Half the carriageway from the street's data alone, as the roads draw a street they know nothing more of
        /// (6 m when the width isn't mapped; shoulders on the highways). The same in every streamed piece.</summary>
        internal static float MappedHalf(Street s)
        {
            string k = s.Data.kind;
            float w = s.Data.width > 0.5f ? s.Data.width : 6f;
            float half = Mathf.Clamp(w, k == "track" ? 2.6f : 3.2f, 30f) * 0.5f;
            switch (k)
            {
                case "motorway": return half + 2.4f;
                case "trunk": return half + 1.5f;
                case "motorway_link": case "trunk_link": return half + 1.0f;
                default: return half;
            }
        }

        /// <summary>How far past the carriageway's edge the roadside things stand when only the data says where it is
        /// (where a streamed piece's line meets its neighbour's, both must agree).</summary>
        internal static float Verge(Street s)
        {
            switch (s.Data.kind)
            {
                case "residential": case "living_street": return 0.75f;
                case "unclassified": return 1.5f;
                case "tertiary": return 1.0f;
                case "secondary": case "primary": return 1.2f;
                case "trunk": case "motorway": return 3.0f;
                default: return 0.8f;
            }
        }

        /// <summary>A street's name, or its id when it has none: the pieces of one road share it.</summary>
        internal static string NameKey(Street s) => !string.IsNullOrEmpty(s.Data.name) ? s.Data.name : "#" + s.Data.id;

        /// <summary>A stable number from a street's name (or its id), for choices every piece of the road shares.</summary>
        internal static long NameId(Street s) => !string.IsNullOrEmpty(s.Data.name) ? DecorTextures.StableHash(s.Data.name) : s.Data.id;

        /// <summary>The point on <paramref name="pts"/> nearest <paramref name="p"/>, the direction of its segment and how
        /// far it is.</summary>
        internal static float Nearest(List<Vector2> pts, Vector2 p, out Vector2 on, out Vector2 dir)
        {
            // Initialise best distance to a huge value; default output point is first vertex.
            float best = float.MaxValue;
            on = pts.Count > 0 ? pts[0] : p;
            dir = Vector2.right;
            // Scan each segment of the polyline.
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float len2 = ab.sqrMagnitude;
                // Skip degenerate zero‑length segments.
                if (len2 < 1e-6f) continue;
                // Project point onto segment, clamped to segment extents.
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                Vector2 q = a + ab * t;
                float d = (q - p).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                on = q;
                dir = ab / Mathf.Sqrt(len2);
            }
            return Mathf.Sqrt(best);
        }

        /// <summary>The road (rank 1 and up, no tunnels) whose surface is nearest <paramref name="p"/> within
        /// <paramref name="within"/>.</summary>
        internal static Street NearestRoad(Ctx ctx, Vector2 p, float within, out Vector2 on, out Vector2 dir, int minRank = 1)
            => ctx.Roads.Nearest(p, within, Mathf.Max(1, minRank), out on, out dir);

        /// <summary>Degrees of yaw for a thing whose front (+Z) faces <paramref name="facing"/> (x east, z north).</summary>
        internal static float Yaw(Vector2 facing) => Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;

        internal static Vector2 Left(Vector2 d) => new Vector2(-d.y, d.x);
        #endregion

        internal static void BuildSidewalkLamps(Ctx ctx)
        {
            const float spacing = 12f; // metres between poles
            const float poleHeight = 2.4f; // metres tall
            const float poleRadius = 0.075f; // thin wooden post
            const float lampHeadRadius = 0.12f; // small glass head
            const float lampHeadHeight = 0.15f; // sits on top of the pole
            foreach (Street s in ctx.Site.Area.Streets)
            {
                if (RoadRank(s) > 2) continue; // skip higher‑class roads
                // use the deterministic hue for this street
                Color poleCol = StreetPoleHue(s);
                Surf poleSurf = DecorPalette.Matte(poleCol);
                // pre‑cache material for efficiency (the first call creates it)

                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i];
                    Vector2 b = s.Pts[i + 1];
                    Vector2 seg = b - a;
                    float segLen = seg.magnitude;
                    if (segLen < 0.5f) continue; // ignore tiny fragments
                    // direction of the street (unit)
                    Vector2 dir = seg / segLen;
                    // right‑hand side of travel (street’s forward direction is from a→b)
                    Vector2 right = Left(dir); // Left() gives a 90° CCW turn, so this is right‑hand side
                    // start point for lamp placement on this segment
                    float offset = 0f;
                    // walk along the segment, dropping a lamp every ‘spacing’
                    while (offset < segLen)
                    {
                        Vector2 onSeg = a + dir * offset;
                        // distance from road centre to the kerb face (use Roadside() helper)
                        float kerb = Roadside(s, 1); // side = +1 is the right sidewalk
                        // final world position: centre of the sidewalk, a few centimetres out from the kerb
                        Vector2 pos2 = onSeg + right * (kerb + 0.15f);
                        // build pole geometry (a thin box)
                        Vector3 polePos = new Vector3(pos2.x, poleHeight * 0.5f, pos2.y);
                        ctx.Low(pos2).Box(polePos, new Vector3(poleRadius * 2f, poleHeight, poleRadius * 2f), poleSurf);
                        // add the lamp head (a small sphere‑ish box just above the pole)
                        Vector3 headPos = new Vector3(pos2.x, poleHeight + lampHeadHeight * 0.5f, pos2.y);
                        ctx.High(pos2).Box(headPos, new Vector3(lampHeadRadius * 2f, lampHeadHeight, lampHeadRadius * 2f), Gloss(new Color(0.95f, 0.92f, 0.70f)));
                        // advance to next placement point
                        offset += spacing;
                    }
                }
            }
        }


        /// <summary>
        /// How built-up the site is around a spot: the footprints of its buildings (and its neighbours' that reach in) summed
        /// on a coarse grid, by kind. Downtown is where shops and offices crowd the street; town where houses stand close.
        /// </summary>
        internal sealed class Density
        {
            private const float Cell = 16f;
            private readonly int _n;
            private readonly float _half;
            private readonly float[] _commercial, _built;
            private readonly int[] _homes, _count;

            public Density(Area area, float reach)
            {
                _half = reach;
                _n = Mathf.Max(1, Mathf.CeilToInt(reach * 2f / Cell));
                _commercial = new float[_n * _n];
                _built = new float[_n * _n];
                _homes = new int[_n * _n];
                _count = new int[_n * _n];
                foreach (Footprint f in area.Buildings) Add(f);
                foreach (Footprint f in area.Foreign) Add(f);
            }

            private void Add(Footprint f)
            {
                int i = Index(f.Middle);
                if (i < 0) return;
                _built[i] += f.Size;
                _count[i]++;
                switch (f.Data.kind)
                {
                    case "commercial": case "retail": case "office": case "civic": case "hotel": case "hospital":
                        _commercial[i] += f.Size;
                        break;
                    case "apartments":
                        _commercial[i] += f.Size * 0.5f;
                        _homes[i]++;
                        break;
                    case "house": case "trailer":
                        _homes[i]++;
                        break;
                }
            }

            private int Index(Vector2 p)
            {
                int x = Mathf.FloorToInt((p.x + _half) / Cell), z = Mathf.FloorToInt((p.y + _half) / Cell);
                if (x < 0 || z < 0 || x >= _n || z >= _n) return -1;
                return z * _n + x;
            }

            /// <summary>Sums within <paramref name="radius"/> of <paramref name="p"/>: shop and office floor area, all
            /// built area, homes and buildings.</summary>
            public void Sum(Vector2 p, float radius, out float commercial, out float built, out int homes, out int count)
            {
                // Initialise result accumulators for the requested radius.
                commercial = built = 0f;
                homes = count = 0;
                // Determine cell range covering the circle (rounded up to whole cells).
                int r = Mathf.CeilToInt(radius / Cell);
                int cx = Mathf.FloorToInt((p.x + _half) / Cell), cz = Mathf.FloorToInt((p.y + _half) / Cell);
                // Use squared radius with a half‑cell padding to avoid edge cases.
                float r2 = (radius + Cell * 0.5f) * (radius + Cell * 0.5f);
                for (int z = cz - r; z <= cz + r; z++)
                {
                    if (z < 0 || z >= _n) continue;
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        if (x < 0 || x >= _n) continue;
                        var c = new Vector2(-_half + (x + 0.5f) * Cell, -_half + (z + 0.5f) * Cell);
                        // Skip cells whose centre lies outside the circle.
                        if ((c - p).sqrMagnitude > r2) continue;
                        int i = z * _n + x;
                        commercial += _commercial[i];
                        built += _built[i];
                        homes += _homes[i];
                        count += _count[i];
                    }
                }
            }

            /// <summary>A small town's (or a city's) commercial core: shops and offices crowding the street.</summary>
            public bool Downtown(Vector2 p)
            {
                Sum(p, 45f, out float commercial, out float built, out _, out int count);
                return commercial >= 1100f && count >= 4 && commercial >= built * 0.45f;
            }

            /// <summary>The middle of a city, where the wires run underground.</summary>
            public bool CityCore(Vector2 p)
            {
                Sum(p, 60f, out float commercial, out _, out _, out _);
                return commercial >= 9000f;
            }

            /// <summary>Built up: a town street rather than a country road.</summary>
            public bool Town(Vector2 p)
            {
                Sum(p, 60f, out _, out float built, out _, out int count);
                return count >= 5 || built >= 1500f;
            }

            /// <summary>Homes within <paramref name="radius"/>.</summary>
            public int Homes(Vector2 p, float radius)
            {
                Sum(p, radius, out _, out _, out int homes, out _);
                return homes;
            }
        }

        /// <summary>
        /// The site's carriageways as the roads drew them (tunnels aside; bridges in), on a coarse grid, for testing the
        /// spots the occupancy grid can't: the square's edge and the scenery ring past it.
        /// </summary>
        internal sealed class RoadGrid
        {
            private const float Cell = 16f;
            private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();
            private readonly List<Vector2> _a = new List<Vector2>(), _b = new List<Vector2>();
            private readonly List<float> _half = new List<float>(), _mapped = new List<float>();
            private readonly List<Street> _street = new List<Street>();
            private readonly List<int> _rank = new List<int>();

            public RoadGrid(Area area)
            {
                foreach (Street s in area.Streets)
                {
                    int rank = RoadRank(s);
                    if (s.Data.tunnel || s.Pts.Count < 2 || rank < 1) continue;
                    float half = HalfWidth(s), mapped = MappedHalf(s);
                    for (int i = 0; i + 1 < s.Pts.Count; i++) Add(s, rank, s.Pts[i], s.Pts[i + 1], half, mapped);
                }
            }

            private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

            private void Add(Street s, int rank, Vector2 a, Vector2 b, float half, float mapped)
            {
                if ((b - a).sqrMagnitude < 1e-6f) return;
                int index = _a.Count;
                _a.Add(a);
                _b.Add(b);
                _half.Add(half);
                _mapped.Add(mapped);
                _street.Add(s);
                _rank.Add(rank);
                int x0 = Mathf.FloorToInt((Mathf.Min(a.x, b.x) - half) / Cell), x1 = Mathf.FloorToInt((Mathf.Max(a.x, b.x) + half) / Cell);
                int z0 = Mathf.FloorToInt((Mathf.Min(a.y, b.y) - half) / Cell), z1 = Mathf.FloorToInt((Mathf.Max(a.y, b.y) + half) / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        long k = Key(x, z);
                        if (!_cells.TryGetValue(k, out List<int> list)) _cells[k] = list = new List<int>(4);
                        list.Add(index);
                    }
            }

            /// <summary>How far <paramref name="p"/> is from the nearest carriageway's surface (negative on it), up to
            /// <paramref name="within"/>; with <paramref name="mapped"/>, from the carriageways as the data alone has them
            /// (the same in every streamed piece, whatever its roads made of them).</summary>
            public float Clearance(Vector2 p, float within, bool mapped = false)
            {
                float best = within;
                int x0 = Mathf.FloorToInt((p.x - within) / Cell), x1 = Mathf.FloorToInt((p.x + within) / Cell);
                int z0 = Mathf.FloorToInt((p.y - within) / Cell), z1 = Mathf.FloorToInt((p.y + within) / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!_cells.TryGetValue(Key(x, z), out List<int> list)) continue;
                        foreach (int i in list)
                        {
                            float d = MapGeometry.DistanceToSegment(p, _a[i], _b[i]) - (mapped ? _mapped[i] : _half[i]);
                            if (d < best) best = d;
                        }
                    }
                return best;
            }

            /// <summary>The road of rank <paramref name="minRank"/> and up whose surface is nearest <paramref name="p"/>
            /// within <paramref name="within"/> metres (or null): the nearest point on its line and its segment's way.</summary>
            public Street Nearest(Vector2 p, float within, int minRank, out Vector2 on, out Vector2 dir)
            {
                Street best = null;
                on = p;
                dir = Vector2.right;
                float bestD = within;
                int bestIndex = int.MaxValue;
                int x0 = Mathf.FloorToInt((p.x - within) / Cell), x1 = Mathf.FloorToInt((p.x + within) / Cell);
                int z0 = Mathf.FloorToInt((p.y - within) / Cell), z1 = Mathf.FloorToInt((p.y + within) / Cell);
                for (int z = z0; z <= z1; z++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!_cells.TryGetValue(Key(x, z), out List<int> list)) continue;
                        foreach (int i in list)
                        {
                            if (_rank[i] < minRank) continue;
                            Vector2 a = _a[i], ab = _b[i] - a;
                            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                            Vector2 q = a + ab * t;
                            float d = (q - p).magnitude - _half[i];
                            // Ties go to the street listed first, as a plain scan of the streets would find it.
                            if (d > bestD || (d == bestD && i >= bestIndex)) continue;
                            bestD = d;
                            bestIndex = i;
                            best = _street[i];
                            on = q;
                            dir = ab.normalized;
                        }
                    }
                return best;
            }
        }

        /// <summary>
        /// The site's buildings (and, on a streamed piece, its neighbours' that reach in) on a coarse grid: is a spot inside
        /// one, does a wire's line pass over one.
        /// </summary>
        internal sealed class BuildingGrid
        {
            private const float Cell = 16f;
            private readonly Dictionary<long, List<Footprint>> _cells = new Dictionary<long, List<Footprint>>();
            private readonly HashSet<Footprint> _seen = new HashSet<Footprint>();

            public BuildingGrid(Area area)
            {
                foreach (Footprint f in area.Buildings) Add(f);
                foreach (Footprint f in area.Foreign) Add(f);
            }

            private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

            private void Add(Footprint f)
            {
                if (f.Pts.Count < 3) return;
                Rect b = f.Bounds;
                for (int z = Mathf.FloorToInt(b.yMin / Cell); z <= Mathf.FloorToInt(b.yMax / Cell); z++)
                    for (int x = Mathf.FloorToInt(b.xMin / Cell); x <= Mathf.FloorToInt(b.xMax / Cell); x++)
                    {
                        long k = Key(x, z);
                        if (!_cells.TryGetValue(k, out List<Footprint> list)) _cells[k] = list = new List<Footprint>(4);
                        list.Add(f);
                    }
            }

            /// <summary>True when <paramref name="p"/> is inside a building, or within <paramref name="pad"/> of its walls.</summary>
            public bool Contains(Vector2 p, float pad)
            {
                for (int z = Mathf.FloorToInt((p.y - pad) / Cell); z <= Mathf.FloorToInt((p.y + pad) / Cell); z++)
                    for (int x = Mathf.FloorToInt((p.x - pad) / Cell); x <= Mathf.FloorToInt((p.x + pad) / Cell); x++)
                    {
                        if (!_cells.TryGetValue(Key(x, z), out List<Footprint> list)) continue;
                        foreach (Footprint f in list)
                        {
                            Rect b = f.Bounds;
                            if (p.x < b.xMin - pad || p.x > b.xMax + pad || p.y < b.yMin - pad || p.y > b.yMax + pad) continue;
                            if (MapGeometry.Contains(f.Pts, p)) return true;
                            if (pad > 0f && MapGeometry.DistanceToEdge(f.Pts, p) < pad) return true;
                        }
                    }
                return false;
            }

            /// <summary>True when the straight line from <paramref name="a"/> to <paramref name="b"/> passes over a building
            /// (or within <paramref name="pad"/> of its walls, where its eaves are), <paramref name="except"/> aside.</summary>
            public bool Crosses(Vector2 a, Vector2 b, float pad, Footprint except = null)
            {
                _seen.Clear();
                float x0 = Mathf.Min(a.x, b.x) - pad, x1 = Mathf.Max(a.x, b.x) + pad;
                float z0 = Mathf.Min(a.y, b.y) - pad, z1 = Mathf.Max(a.y, b.y) + pad;
                for (int z = Mathf.FloorToInt(z0 / Cell); z <= Mathf.FloorToInt(z1 / Cell); z++)
                    for (int x = Mathf.FloorToInt(x0 / Cell); x <= Mathf.FloorToInt(x1 / Cell); x++)
                    {
                        if (!_cells.TryGetValue(Key(x, z), out List<Footprint> list)) continue;
                        foreach (Footprint f in list)
                        {
                            if (f == except || !_seen.Add(f)) continue;
                            Rect r = f.Bounds;
                            if (r.xMax < x0 || r.xMin > x1 || r.yMax < z0 || r.yMin > z1) continue;
                            if (SegmentMeets(f.Pts, a, b, pad)) return true;
                        }
                    }
                return false;
            }

            /// <summary>True when the segment from <paramref name="a"/> to <paramref name="b"/> enters the polygon or comes
            /// within <paramref name="pad"/> of its outline.</summary>
            internal static bool Meets(List<Vector2> poly, Vector2 a, Vector2 b, float pad) => SegmentMeets(poly, a, b, pad);

            // The segment a-b inside the polygon or within pad of its outline.
            private static bool SegmentMeets(List<Vector2> poly, Vector2 a, Vector2 b, float pad)
            {
                if (MapGeometry.Contains(poly, a) || MapGeometry.Contains(poly, b)) return true;
                if (pad > 0f && (MapGeometry.DistanceToEdge(poly, a) < pad || MapGeometry.DistanceToEdge(poly, b) < pad)) return true;
                int n = poly.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector2 c = poly[i], d = poly[(i + 1) % n];
                    if (SegmentsCross(a, b, c, d)) return true;
                    if (pad > 0f && MapGeometry.DistanceToSegment(c, a, b) < pad) return true;
                }
                return false;
            }

            private static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                float d1 = Cross(b - a, c - a), d2 = Cross(b - a, d - a), d3 = Cross(d - c, a - c), d4 = Cross(d - c, b - c);
                return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
            }

            private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
        }

        /// <summary>
        /// The site's overhead wires: sagging three-sided strands, a few hundred vertices a span, gathered into one mesh per
        /// 64 m cell (inside the square under the overhead root, which moves with a streamed piece and is never baked;
        /// past it with the scenery). One palette material, so a whole town's wires are a handful of draws.
        /// <para>The vegetation pass reads the wires back from these meshes to keep the trees' crowns off them, so their
        /// layout is a contract: readable meshes on objects named "Wires" directly under the overhead root, emitted before
        /// the vegetation pass runs; each strand a run of three-vertex rings in order, no two rings of one strand more than
        /// 5 m apart.</para>
        /// </summary>
        internal sealed class WireBatch
        {
            private const float Cell = 64f;

            /// <summary>Collects geometry for a single wire‑mesh cell: vertices, normals, UVs and triangle indices.</summary>
            private sealed class Bucket
            {
                public readonly List<Vector3> V = new List<Vector3>(512);
                public readonly List<Vector3> N = new List<Vector3>(512);
                public readonly List<Vector2> Uv = new List<Vector2>(512);
                public readonly List<int> T = new List<int>(1024);
                public Vector3 Centre;
                public bool Far;
            }

            private readonly Dictionary<long, Bucket> _cells = new Dictionary<long, Bucket>();
            private readonly List<Bucket> _order = new List<Bucket>();
            private Material _material;

            public int Count { get; private set; }

            /// <summary>A wire from <paramref name="a"/> to <paramref name="b"/> hanging <paramref name="sag"/> metres at its
            /// middle; <paramref name="far"/> for scenery.</summary>
            public void Add(Vector3 a, Vector3 b, float sag, float radius, Color colour, bool far)
            {
                Vector3 ab = b - a;
                float len = ab.magnitude;
                if (len < 0.05f) return;
                int segs = Mathf.Clamp(Mathf.CeilToInt(len / 5f), 2, 80);
                Surf s = DecorPalette.Matte(colour);
                _material = s.Mat;
                Bucket bucket = BucketAt((a + b) * 0.5f, far);
                Vector3 side = Vector3.Cross(ab, Vector3.up);
                side = side.sqrMagnitude > 1e-6f ? side.normalized : Vector3.right;
                int first = bucket.V.Count;
                for (int k = 0; k <= segs; k++)
                {
                    float t = k / (float)segs;
                    Vector3 p = a + ab * t - Vector3.up * (sag * 4f * t * (1f - t));
                    // The strand's way here (the parabola's slope), for the ring of three vertices round it.
                    Vector3 along = ab - Vector3.up * (sag * 4f * (1f - 2f * t));
                    Vector3 up = Vector3.Cross(side, along).normalized;
                    for (int j = 0; j < 3; j++)
                    {
                        float ang = (90f + j * 120f) * Mathf.Deg2Rad;
                        Vector3 n = side * Mathf.Cos(ang) + up * Mathf.Sin(ang);
                        bucket.V.Add(p + n * radius - bucket.Centre);
                        bucket.N.Add(n);
                        bucket.Uv.Add(s.Uv);
                    }
                }
                for (int k = 0; k < segs; k++)
                    for (int j = 0; j < 3; j++)
                    {
                        int a0 = first + k * 3 + j, a1 = first + k * 3 + (j + 1) % 3;
                        int b0 = a0 + 3, b1 = a1 + 3;
                        bucket.T.Add(a0); bucket.T.Add(b0); bucket.T.Add(b1);
                        bucket.T.Add(a0); bucket.T.Add(b1); bucket.T.Add(a1);
                    }
                Count++;
            }

            /// <summary>A wire that hangs from <paramref name="a"/> and levels out at <paramref name="b"/> (half a span: where
            /// a line leaves the data for a neighbouring region, both sides meet level at the same height).</summary>
            public void AddHalf(Vector3 a, Vector3 b, float radius, Color colour, bool far)
            {
                Vector3 ab = b - a;
                float len = ab.magnitude;
                if (len < 0.05f) return;
                int segs = Mathf.Clamp(Mathf.CeilToInt(len / 5f), 2, 80);
                var pts = new Vector3[segs + 1];
                for (int k = 0; k <= segs; k++)
                {
                    float t = k / (float)segs;
                    Vector3 p = new Vector3(a.x + ab.x * t, 0f, a.z + ab.z * t);
                    p.y = b.y + (a.y - b.y) * (1f - t) * (1f - t);
                    pts[k] = p;
                }
                for (int k = 0; k < segs; k++) Add(pts[k], pts[k + 1], 0f, radius, colour, far);
            }

            /// <summary>Gets or creates the bucket for the cell containing point p, separating far scenery.</summary>
            private Bucket BucketAt(Vector3 p, bool far)
            {
                int x = Mathf.FloorToInt(p.x / Cell), z = Mathf.FloorToInt(p.z / Cell);
                long key = ((long)x << 32) ^ (uint)z ^ (far ? 1L << 62 : 0L);
                if (_cells.TryGetValue(key, out Bucket b)) return b;
                b = new Bucket { Centre = new Vector3((x + 0.5f) * Cell, 0f, (z + 0.5f) * Cell), Far = far };
                _cells[key] = b;
                _order.Add(b);
                return b;
            }

            // Emit meshes for each bucket that actually contains geometry.
            public IEnumerable<string> Emit(Site site)
            {
                foreach (Bucket b in _order)
                {
                    // Signal Unity to step the world if needed; this yields a step name for the batch system.
                    if (site.StepDue) yield return "wires";
                    // Skip empty buckets – nothing to draw.
                    if (b.V.Count == 0) continue;
                    var mesh = new Mesh { name = "Wires" };
                    // Register the mesh so it can be cleaned up later.
                    RaidMesh.CreatedMeshes?.Add(mesh);
                    // Use 32‑bit indices for large meshes.
                    if (b.V.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                    mesh.SetVertices(b.V);
                    mesh.SetNormals(b.N);
                    mesh.SetUVs(0, b.Uv);
                    mesh.SetTriangles(b.T, 0, true);
                    var go = new GameObject("Wires");
                    // Attach to the appropriate parent (far scenery vs overhead).
                    go.transform.SetParent(b.Far ? site.Town.Scenery : site.Town.Overhead, false);
                    go.transform.localPosition = b.Centre;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = _material;
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.lightProbeUsage = LightProbeUsage.Off;
                    r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
                // Cleanup temporary buckets for the next frame.
                _cells.Clear();
                _order.Clear();
            }
        }
    }
}
