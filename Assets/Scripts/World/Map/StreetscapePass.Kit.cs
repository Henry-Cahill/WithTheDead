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
            /// <summary>Real lights handed out so far, and the site's cap.</summary>
            public int Lights, MaxLights;
            /// <summary>Where two or more roads meet, with their road arms only.</summary>
            public readonly List<Crossing> Crossings = new List<Crossing>();
            private NameAtlas _names;

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

            /// <summary>A world-anchored key for a spot (to half a metre), so a thing two streamed pieces both build comes
            /// out the same in each.</summary>
            public long KeyOf(Vector2 local)
            {
                Vector2 w = local + Origin;
                return ((long)Mathf.RoundToInt(w.x * 2f) << 32) ^ (uint)Mathf.RoundToInt(w.y * 2f);
            }

            /// <summary>A world-anchored number in [0, 1) for a spot and a salt.</summary>
            public float Unit(Vector2 local, int salt) => MapStyle.Unit(KeyOf(local), salt);

            private void FindCrossings()
            {
                var names = new List<string>(6);
                foreach (Junction j in Site.Junctions)
                {
                    var c = new Crossing { At = j.At, Junction = j };
                    names.Clear();
                    foreach (JunctionArm a in j.Arms)
                    {
                        int rank = RoadRank(a.Street);
                        if (rank < 0) continue;
                        c.Arms.Add(a);
                        string key = NameKey(a.Street);
                        if (!names.Contains(key)) names.Add(key);
                        if (rank > c.TopRank) c.TopRank = rank;
                    }
                    if (c.Arms.Count < 2 || (names.Count < 2 && c.Arms.Count < 3)) continue;
                    Crossings.Add(c);
                }
            }

            /// <summary>The crossing nearest <paramref name="p"/> within <paramref name="within"/> metres, or null.</summary>
            public Crossing NearestCrossing(Vector2 p, float within)
            {
                Crossing best = null;
                float bestD = within * within;
                foreach (Crossing c in Crossings)
                {
                    float d = (c.At - p).sqrMagnitude;
                    if (d >= bestD) continue;
                    bestD = d;
                    best = c;
                }
                return best;
            }

            /// <summary>Hands out what's built per site and can't be batched: the wires and the sign atlas.</summary>
            public void Finish()
            {
                Wires.Emit(Site);
                _names?.Finish();
            }
        }

        /// <summary>A place where roads meet (only their road arms).</summary>
        internal sealed class Crossing
        {
            public Vector2 At;
            public Junction Junction;
            public readonly List<JunctionArm> Arms = new List<JunctionArm>();
            public int TopRank = -1;
            /// <summary>Set once something controls it (signals, stop signs), so nothing else does.</summary>
            public bool Controlled;
            /// <summary>Set once its street-name blades are up.</summary>
            public bool Named;
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
            string k = s.Data.kind;
            if (string.IsNullOrEmpty(k)) return -1;
            if (k.EndsWith("_link")) k = k.Substring(0, k.Length - 5);
            switch (k)
            {
                case "motorway": case "trunk": return 6;
                case "primary": return 5;
                case "secondary": return 4;
                case "tertiary": return 3;
                case "unclassified": case "residential": case "living_street": return 2;
                case "service": case "track": return 1;
                default: return -1;
            }
        }

        /// <summary>Half the carriageway's width, as the roads are drawn.</summary>
        internal static float HalfWidth(Street s) => Mathf.Max(1.5f, s.Width * 0.5f);

        /// <summary>How far past the carriageway's edge the roadside things stand (poles, signs, lamps).</summary>
        internal static float Verge(Street s)
        {
            switch (s.Data.kind)
            {
                case "residential": case "living_street": return 0.65f;
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
            float best = float.MaxValue;
            on = pts.Count > 0 ? pts[0] : p;
            dir = Vector2.right;
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                Vector2 a = pts[i], ab = pts[i + 1] - a;
                float len2 = ab.sqrMagnitude;
                if (len2 < 1e-6f) continue;
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

        /// <summary>The road (rank 1 and up, no tunnels) nearest <paramref name="p"/> within <paramref name="within"/>.</summary>
        internal static Street NearestRoad(Site site, Vector2 p, float within, out Vector2 on, out Vector2 dir, int minRank = 1)
        {
            Street best = null;
            on = p;
            dir = Vector2.right;
            float bestD = within;
            foreach (Street s in site.Area.Streets)
            {
                if (s.Data.tunnel || RoadRank(s) < minRank) continue;
                float d = Nearest(s.Pts, p, out Vector2 q, out Vector2 sd) - HalfWidth(s);
                if (d >= bestD) continue;
                bestD = d;
                best = s;
                on = q;
                dir = sd;
            }
            return best;
        }

        /// <summary>Degrees of yaw for a thing whose front (+Z) faces <paramref name="facing"/> (x east, z north).</summary>
        internal static float Yaw(Vector2 facing) => Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;

        internal static Vector2 Left(Vector2 d) => new Vector2(-d.y, d.x);
        #endregion

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
                commercial = built = 0f;
                homes = count = 0;
                int r = Mathf.CeilToInt(radius / Cell);
                int cx = Mathf.FloorToInt((p.x + _half) / Cell), cz = Mathf.FloorToInt((p.y + _half) / Cell);
                float r2 = (radius + Cell * 0.5f) * (radius + Cell * 0.5f);
                for (int z = cz - r; z <= cz + r; z++)
                {
                    if (z < 0 || z >= _n) continue;
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        if (x < 0 || x >= _n) continue;
                        var c = new Vector2(-_half + (x + 0.5f) * Cell, -_half + (z + 0.5f) * Cell);
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
        /// The site's overhead wires: sagging three-sided strands, a few hundred vertices a span, gathered into one mesh per
        /// 64 m cell (inside the square under the overhead root, which moves with a streamed piece and is never baked;
        /// past it with the scenery). One palette material, so a whole town's wires are a handful of draws.
        /// </summary>
        internal sealed class WireBatch
        {
            private const float Cell = 64f;

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
                int segs = Mathf.Clamp(Mathf.CeilToInt(len / 5f), 2, 20);
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
                int segs = Mathf.Clamp(Mathf.CeilToInt(len / 5f), 2, 20);
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

            public void Emit(Site site)
            {
                foreach (Bucket b in _order)
                {
                    if (b.V.Count == 0) continue;
                    var mesh = new Mesh { name = "Wires" };
                    RaidMesh.CreatedMeshes?.Add(mesh);
                    if (b.V.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                    mesh.SetVertices(b.V);
                    mesh.SetNormals(b.N);
                    mesh.SetUVs(0, b.Uv);
                    mesh.SetTriangles(b.T, 0, true);
                    var go = new GameObject("Wires");
                    go.transform.SetParent(b.Far ? site.Town.Scenery : site.Town.Overhead, false);
                    go.transform.localPosition = b.Centre;
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = _material;
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.lightProbeUsage = LightProbeUsage.Off;
                    r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
                _cells.Clear();
                _order.Clear();
            }
        }
    }
}
