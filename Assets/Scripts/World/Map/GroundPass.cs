using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's ground, which sets the palette of the whole map: a January snow cover over everything,
    /// cool white and gently uneven (drifts, wind-scoured crust, greyer round industry, shaded in the woods), on which
    /// the land use reads the way it would from a hill after a week of snow: farm fields striped with corn stubble or
    /// soybean rows, pasture tufts, darker woods with leaf litter showing, reed beds, frozen mud on lots and farmyards,
    /// gravel yards with pallets and drums, cemeteries with rows of headstones, playgrounds and ball fields, plowed
    /// parking lots (black aisles, packed snow in the stall rows round the parked cars, half-buried stall lines, wheel
    /// stops), plazas, gas station forecourts; and the water: ponds and lakes frozen grey-blue under drifted snow, with
    /// white rims and reedy or muddy banks, rivers running black between shelves of ice with drift ice coming down,
    /// streams and ditches as dark threads of water between ice shelves (culverts aren't drawn).
    /// </summary>
    /// <remarks>
    /// <para>Land-use outlines overlap (the world data keeps only their outer rings, so a field's woodlot or a forest's
    /// clearing is drawn over by its surroundings), so the areas are first made into one partition of the ground
    /// (<see cref="Compose"/>): where they overlap, features (parking, pitches, cemeteries...) beat cover (water,
    /// forest, fields...), which beats broad land use (residential, industrial, parks...), and within a tier the smaller
    /// area wins. Every fill is then drawn at one height, so no two fight.</para>
    /// <para>The ground's heights above the base snow, all below the railway beds (<see cref="MapStyle.LotY"/>, 8 mm),
    /// the nature pass's driveways (9 mm), the paths (10.5 mm) and the roads (<see cref="MapStyle.StreetY"/>, 12 mm, and
    /// everything the road pass draws above it):</para>
    /// <list type="table">
    /// <item><term>0</term><description>the base snow: the walkable Ground box's top (the fixed map's far snowfield
    /// lies 20 mm lower)</description></item>
    /// <item><term>1.5 mm (<see cref="FillY"/>)</term><description>the land-use fills: ice or open water, forest floor,
    /// lots, paving...</description></item>
    /// <item><term>3 mm (<see cref="DecalY"/>)</term><description>what lies on them: snow on the ice, shore rims and
    /// banks, stubble rows, the plowed snow round a lot, tree wells (other passes' ground decals belong here too);
    /// stall lines half a millimetre higher</description></item>
    /// <item><term>4 mm (<see cref="ToneY"/>)</term><description>the snow's tone: a see-through layer of drifts,
    /// scoured crust, grime and shade over everything below it but the lots and the water, and the packed snow in the
    /// lots' stall rows</description></item>
    /// <item><term>4.5, 4.8 mm (<see cref="ShelfY"/>, <see cref="FloeY"/>)</term><description>open rivers: the shelf
    /// ice along the banks; the snow on it, and the drift ice</description></item>
    /// <item><term>5, 5.5, 6 mm (<see cref="BedY"/>, <see cref="StreamIceY"/>, <see cref="StreamWaterY"/>)</term>
    /// <description>waterways: the banks of streams and ditches, their ice shelves, the open water (and the thread of it
    /// down a frozen river)</description></item>
    /// </list>
    /// <para>A streamed chunk draws exactly its own square (fills, tone and decals cut to it) and places anything that
    /// repeats (rows, headstones, stalls, drift ice) on lattices fixed to the world or to the area's whole outline, so
    /// neighbouring chunks meet without a seam. Lite chunks and the fixed map's scenery ring get the fills, the water
    /// with its shores and shelves, the field rows, the lots' plowed edges and packed stall rows and the tone, but no
    /// props, no stall lines and no small decals.</para>
    /// </remarks>
    internal static partial class GroundPass
    {
        /// <summary>The land-use fills (see the remarks).</summary>
        internal const float FillY = 0.0015f;
        /// <summary>Flat decals on the ground (see the remarks): other passes' ground decals belong at this height too.</summary>
        internal const float DecalY = 0.003f;
        /// <summary>The see-through layer of the snow's tone.</summary>
        internal const float ToneY = 0.004f;
        /// <summary>An open river's shelf ice.</summary>
        internal const float ShelfY = 0.0045f;
        /// <summary>The snow on an open river's shelf ice, and the drift ice.</summary>
        internal const float FloeY = 0.0048f;
        /// <summary>The banks of streams and ditches.</summary>
        internal const float BedY = 0.005f;
        /// <summary>The ice shelves of streams.</summary>
        internal const float StreamIceY = 0.0055f;
        /// <summary>Open water: the dark thread down a stream or a frozen river.</summary>
        internal const float StreamWaterY = 0.006f;

        private const float FarSnowY = -0.02f;   // the fixed map's snowfield past its scenery

        /// <summary>Builds all ground-related layers for a site, yielding step identifiers for progress tracking.</summary>
        internal static IEnumerable<string> Build(Site site)
        {
            // Initialise per‑site ground data.
            var g = new Ground(site);
            // Replace any previous entry for this site and store the new ground object.
            s_Sites.Remove(site);
            s_Sites.Add(site, g);
            // Warm up any cached resources needed for ground rendering.
            Warm();
            // Ensure shared geometry (e.g., meshes) is prepared.
            MakeAll();
            // The basements under the houses: the ground leaves them open (KentuckyMap.Levels).
            g.Holes = new List<Vector2[]>();
            foreach (string step in Basements(site, g.Holes)) yield return step;
            // Create the base walkable snow and distant snowfield.
            BaseSnow(site, g.Holes);
            // Partition the site into owners (areas) for later processing.
            Partition(g);
            yield return "ground";
            // Add water features.
            foreach (string step in Water(g)) yield return step;
            yield return "water";
            // Add field patterns (rows, crops, etc.).
            foreach (string step in Fields(g)) yield return step;
            yield return "fields";
            // For full‑detail sites, generate the ground mask used by later passes.
            if (!g.Lite)
            {
                g.Mask();
                yield return "ground mask";
            }
            // Add lot outlines and related geometry.
            foreach (string step in Lots(g)) yield return step;
            yield return "lots";
            // For full‑detail sites, draw the detailed ground surface.
            if (!g.Lite)
                foreach (string step in Grounds(g)) yield return step;
            // Apply the snow tone overlay.
            Tone(g);
            // Release temporary data structures.
            g.Release();
            yield return "snow tone";
        }

        #region What other passes can ask
        /// <summary>A parking stall painted on a lot: its middle (site-local), the way a car nosed into it faces (yaw in
        /// degrees as Unity turns: 0 = +z, clockwise seen from above), its width and depth, and the lot's id.</summary>
        internal struct ParkingStall
        {
            public Vector2 At;
            public float Yaw, Width, Depth;
            public long Lot;
        }

        private static readonly ConditionalWeakTable<Site, Ground> s_Sites = new ConditionalWeakTable<Site, Ground>();
        private static readonly List<ParkingStall> s_NoStalls = new List<ParkingStall>();
        private static readonly List<Vector2> s_NoClearings = new List<Vector2>();

        /// <summary>The stalls painted on the site's parking lots (Full detail only), in a fixed order: where parked cars
        /// belong. The lots themselves are taken (<see cref="Site.Take(Rect)"/>), so place cars here without asking
        /// <see cref="Site.Free"/>.</summary>
        internal static IReadOnlyList<ParkingStall> Stalls(Site site)
            => s_Sites.TryGetValue(site, out Ground g) ? g.Stalls : s_NoStalls;

        /// <summary>The middles of the bare 10 m plots the ground pass leaves among a cemetery's headstones (Full
        /// detail only), in a fixed order: where the cemetery's trees belong (the rest of it is taken by the stones).</summary>
        internal static IReadOnlyList<Vector2> Clearings(Site site)
            => s_Sites.TryGetValue(site, out Ground g) ? g.Clearings : s_NoClearings;

        /// <summary>The kind of land whose ground shows at a site-local point inside the site's square ("water",
        /// "forest", "parking"...; see <see cref="MapArea.kind"/>), or "" for plain snow.</summary>
        internal static string LandAt(Site site, Vector2 p)
        {
            if (!s_Sites.TryGetValue(site, out Ground g)) return "";
            Owner o = g.OwnerAt(p);
            return o != null ? o.Kind : "";
        }

        /// <summary>A tree well: the thinner, needle-strewn snow under a tree's crown (radius in metres), drawn with the
        /// outdoor ground at <see cref="DecalY"/>. Skipped on water, paving and lots.</summary>
        internal static void TreeWell(Site site, Vector2 at, float radius)
        {
            if (s_Sites.TryGetValue(site, out Ground g))
            {
                Owner o = g.OwnerAt(at);
                if (o != null && (o.Tier == TierFeature || o.Kind == "water")) return;
            }
            System.Random rng = MapStyle.Rng(MapStyle.Key(at), SaltWell);
            Vector3 c = To3(at, DecalY);
            site.Town.Out.Flat(c).FlatBlob(c, radius, radius * MapStyle.Range(rng, 0.8f, 1.1f), MapStyle.Range(rng, 0f, 180f),
                0.25f, rng, ForestFloorSurf);
        }

        /// <summary>The snow the ground is made of: a planar texture of the ground's own, cooler than the decor snow so
        /// that it reads white under the map's warm daylight.</summary>
        internal static Surf SnowSurf => Surface("snow", 9f, 0.28f);
        #endregion

        #region The base snow
        // Walkable snow with one collider over the playable square (like the procedural town's), and a snowfield
        // without a collider under the scenery beyond, so the horizon isn't empty. Over a basement the snow is open: a body
        // there passes through the collider (LevelStructure), and the house's own floor covers it.
        private static void BaseSnow(Site site, List<Vector2[]> holes)
        {
            float half = site.Half + 0.8f;
            var ground = new GameObject("Ground");
            ground.transform.SetParent(site.Town.World, false);
            var m = new RaidMesh();
            SnowTop(m, half, holes);
            m.ApplyTo(ground, Vector3.zero, ShadowCastingMode.Off);
            // Its sides, a metre deep, for where the next piece isn't built: open where a basement crosses one, and kept out
            // of the NavMesh (there's nothing to walk on under the snow, and a side hangs in any basement past the piece's edge
            // that it doesn't know of, under the next piece's house).
            var sides = new RaidMesh();
            SnowSides(sides, half, holes);
            var skirt = new GameObject("Sides");
            skirt.transform.SetParent(ground.transform, false);
            sides.ApplyTo(skirt, Vector3.zero, ShadowCastingMode.Off);
            skirt.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var col = ground.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.5f, 0f);
            col.size = new Vector3(half * 2f, 1f, half * 2f);
            if (site.Chunked) return;   // a streamed map's neighbouring pieces are the horizon

            float far = site.Half + SceneryReach + 40f;
            var field = new RaidMesh();
            field.Flat(new Vector3(0f, FarSnowY, 0f), far * 2f, far * 2f, 0f, SnowSurf);
            field.Emit("Snowfield", site.Town.Scenery, Vector3.zero, ShadowCastingMode.Off);
        }

        // The snow's top with the basements cut out of it; no underside, which would hang in the basements (and keep the
        // NavMesh out of them).
        private static void SnowTop(RaidMesh m, float half, List<Vector2[]> holes)
        {
            var pieces = new List<List<Vector2>>
            {
                new List<Vector2> { new Vector2(-half, -half), new Vector2(half, -half), new Vector2(half, half), new Vector2(-half, half) },
            };
            var next = new List<List<Vector2>>();
            var frame = new LevelLayout();
            if (holes != null)
                foreach (Vector2[] h in holes)
                {
                    if (!HoleFrame(h, frame, out Rect hole)) continue;
                    next.Clear();
                    foreach (List<Vector2> p in pieces)
                    {
                        if (!Overlaps(frame, p, hole)) next.Add(p);
                        else Levels.OutsideRect(frame, p, hole, next);
                    }
                    (pieces, next) = (next, pieces);
                }
            Surf snow = SnowSurf;
            foreach (List<Vector2> p in pieces)
            {
                if (MapGeometry.SignedArea(p) > 0f) p.Reverse();   // clockwise seen from above: the face looks up
                for (int i = 1; i + 1 < p.Count; i++) m.Tri(To3(p[0], 0f), To3(p[i], 0f), To3(p[i + 1], 0f), snow);
            }
        }

        // The snow's sides round the square, a metre deep, less where a basement's hole crosses one.
        private static void SnowSides(RaidMesh m, float half, List<Vector2[]> holes)
        {
            Surf snow = SnowSurf;
            SnowSide(m, new Vector2(-half, half), new Vector2(half, half), Vector3.forward, holes, snow);
            SnowSide(m, new Vector2(-half, -half), new Vector2(half, -half), Vector3.back, holes, snow);
            SnowSide(m, new Vector2(half, -half), new Vector2(half, half), Vector3.right, holes, snow);
            SnowSide(m, new Vector2(-half, -half), new Vector2(-half, half), Vector3.left, holes, snow);
        }

        private static void SnowSide(RaidMesh m, Vector2 a, Vector2 b, Vector3 outward, List<Vector2[]> holes, in Surf snow)
        {
            var cuts = new List<Vector2>(2);
            if (holes != null)
                foreach (Vector2[] h in holes)
                    if (Crossing(a, b, h, out float t0, out float t1)) cuts.Add(new Vector2(t0, t1));
            cuts.Sort((p, q) => p.x.CompareTo(q.x));
            float length = Vector2.Distance(a, b), t = 0f;
            foreach (Vector2 c in cuts)
            {
                if (c.x > t) SnowStretch(m, a, b, t, c.x, length, outward, snow);
                t = Mathf.Max(t, c.y);
            }
            if (t < 1f) SnowStretch(m, a, b, t, 1f, length, outward, snow);
        }

        private static void SnowStretch(RaidMesh m, Vector2 a, Vector2 b, float t0, float t1, float length, Vector3 outward, in Surf snow)
        {
            if ((t1 - t0) * length < 0.01f) return;
            Vector2 mid = Vector2.Lerp(a, b, (t0 + t1) * 0.5f);
            m.Upright(new Vector3(mid.x, -0.5f, mid.y), outward, (t1 - t0) * length, 1f, snow);
        }

        // The stretch t0..t1 of the segment a..b that lies in a convex quad (clipped against each of its sides), if any.
        private static bool Crossing(Vector2 a, Vector2 b, Vector2[] quad, out float t0, out float t1)
        {
            t0 = 0f;
            t1 = 1f;
            Vector2 d = b - a, mid = Vector2.zero;
            foreach (Vector2 p in quad) mid += p;
            mid /= quad.Length;
            for (int i = 0; i < quad.Length; i++)
            {
                Vector2 p = quad[i], e = quad[(i + 1) % quad.Length] - p;
                var n = new Vector2(-e.y, e.x);
                if (Vector2.Dot(mid - p, n) < 0f) n = -n;   // inward
                float num = Vector2.Dot(a - p, n), den = Vector2.Dot(d, n);
                if (Mathf.Abs(den) < 1e-9f)
                {
                    if (num < 0f) return false;
                    continue;
                }
                float t = -num / den;
                if (den > 0f) t0 = Mathf.Max(t0, t);
                else t1 = Mathf.Min(t1, t);
                if (t0 >= t1) return false;
            }
            return true;
        }
        #endregion

        #region Basements
        // The basements under the site's houses (and those of the houses its neighbours build that reach into it): their
        // corners, anticlockwise. KentuckyMap.Levels tells from the data alone, so the buildings, raised after the ground,
        // have them just where it leaves room. Each house's main block is worked out here (and kept): a step at a time.
        private static IEnumerable<string> Basements(Site site, List<Vector2[]> into)
        {
            if (site.Detail != Detail.Full) yield break;
            foreach (Footprint f in site.Area.Buildings)
            {
                Basement(f, into);
                if (site.StepDue) yield return "ground";
            }
            foreach (Footprint f in site.Area.Foreign)
            {
                Basement(f, into);
                if (site.StepDue) yield return "ground";
            }
        }

        private static void Basement(Footprint f, List<Vector2[]> into)
        {
            Vector2[] b = Levels.BasementOf(f);
            if (b != null && b.Length == 4) into.Add(b);
        }

        // A basement's rectangle in a frame of its own (its first corner, its sides' directions).
        private static bool HoleFrame(Vector2[] h, LevelLayout frame, out Rect hole)
        {
            Vector2 ex = h[1] - h[0], ez = h[3] - h[0];
            float w = ex.magnitude, d = ez.magnitude;
            hole = new Rect(0f, 0f, w, d);
            if (w < 0.5f || d < 0.5f) return false;
            frame.Origin = h[0];
            frame.Ex = ex / w;
            frame.Ez = ez / d;
            return true;
        }

        // True when a polygon's extent in the frame overlaps the rectangle.
        private static bool Overlaps(LevelLayout frame, List<Vector2> poly, Rect r)
        {
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            foreach (Vector2 p in poly)
            {
                Vector2 q = frame.ToLocal(p);
                x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x);
                z0 = Mathf.Min(z0, q.y); z1 = Mathf.Max(z1, q.y);
            }
            return x1 > r.xMin + 1e-3f && x0 < r.xMax - 1e-3f && z1 > r.yMin + 1e-3f && z0 < r.yMax - 1e-3f;
        }

        // True when the square cell (x0, z0)..(x1, z1) and the convex quad overlap (by their separating axes), less a
        // sliver at the edges.
        private static bool CellOverHole(Vector2[] h, float x0, float z0, float x1, float z1)
        {
            const float eps = 0.05f;
            float hx0 = float.MaxValue, hz0 = float.MaxValue, hx1 = float.MinValue, hz1 = float.MinValue;
            foreach (Vector2 p in h)
            {
                hx0 = Mathf.Min(hx0, p.x); hx1 = Mathf.Max(hx1, p.x);
                hz0 = Mathf.Min(hz0, p.y); hz1 = Mathf.Max(hz1, p.y);
            }
            if (hx1 <= x0 + eps || hx0 >= x1 - eps || hz1 <= z0 + eps || hz0 >= z1 - eps) return false;
            for (int i = 0; i < h.Length; i++)
            {
                Vector2 e = h[(i + 1) % h.Length] - h[i];
                var n = new Vector2(-e.y, e.x);
                float len = n.magnitude;
                if (len < 1e-5f) continue;
                n /= len;
                float a0 = float.MaxValue, a1 = float.MinValue, c0 = float.MaxValue, c1 = float.MinValue;
                foreach (Vector2 p in h)
                {
                    float s = Vector2.Dot(p, n);
                    a0 = Mathf.Min(a0, s); a1 = Mathf.Max(a1, s);
                }
                for (int k = 0; k < 4; k++)
                {
                    float s = Vector2.Dot(new Vector2((k & 1) == 0 ? x0 : x1, (k & 2) == 0 ? z0 : z1), n);
                    c0 = Mathf.Min(c0, s); c1 = Mathf.Max(c1, s);
                }
                if (a1 <= c0 + eps || c1 <= a0 + eps) return false;
            }
            return true;
        }
        #endregion

        #region The site being built
        // Salts for MapStyle's hashes, one per kind of choice.
        private const int SaltWell = 0x6E11, SaltRows = 0x6A01, SaltRowGap = 0x6A02, SaltCrop = 0x6A03, SaltIcePatch = 0x6B01,
            SaltRim = 0x6B02, SaltBank = 0x6B03, SaltChannel = 0x6B04, SaltStream = 0x6B05, SaltReeds = 0x6B06,
            SaltFloe = 0x6B07, SaltShelf = 0x6B08,
            SaltStone = 0x6C01, SaltGraves = 0x6C02, SaltPlot = 0x6C03, SaltPlay = 0x6C04, SaltPitch = 0x6C05,
            SaltYard = 0x6C06, SaltStall = 0x6D01, SaltLine = 0x6D02, SaltStop = 0x6D03, SaltLotEdge = 0x6D04,
            SaltTone = 0x6E01, SaltPlaza = 0x6E02, SaltOrchard = 0x6A04;

        /// <summary>What the ground pass knows about one site while it's built (and, for <see cref="LandAt"/> and
        /// <see cref="Stalls"/>, afterwards).</summary>
        private sealed class Ground
        {
            public readonly Site Site;
            public readonly bool Lite;
            public readonly float Half;
            public readonly Rect Square;
            /// <summary>Where the site's middle is in the world (x, z): site-local + this is the same point in every
            /// chunk that sees it.</summary>
            public readonly Vector2 Offset;
            /// <summary>The areas cut to the square, in priority order (the last one shows where they overlap).</summary>
            public List<Owner> Owners = new List<Owner>();
            /// <summary>The basements under the site's houses, their corners anticlockwise: the ground leaves them open.</summary>
            public List<Vector2[]> Holes;
            /// <summary>A fixed map's areas past its edge (the scenery ring), in the four bands round the square.</summary>
            public readonly List<List<Owner>> Ring = new List<List<Owner>>();
            public readonly List<ParkingStall> Stalls = new List<ParkingStall>();
            public readonly List<Vector2> Clearings = new List<Vector2>();
            /// <summary>See-through geometry drawn with the snow's tone (the packed snow left in the lots' stall rows):
            /// positions, coordinates in the tone's table (<see cref="ToneMaterial"/>) and triangles, clockwise seen from
            /// above.</summary>
            public readonly List<Vector3> ToneVerts = new List<Vector3>();
            public readonly List<Vector2> ToneUvs = new List<Vector2>();
            public readonly List<int> ToneTris = new List<int>();

            private bool[] _blocked;
            private int _n;
            private bool _shiftTried, _hasShift;
            private Vector2 _shift;

            public Ground(Site site)
            {
                Site = site;
                Lite = site.Detail == Detail.Lite;
                Half = site.Half;
                Square = Rect.MinMaxRect(-Half, -Half, Half, Half);
                Offset = new Vector2(site.WorldOffset.x, site.WorldOffset.z);
            }

            /// <summary>The point in world metres (the same in every chunk).</summary>
            public Vector2 World(Vector2 local) => local + Offset;

            /// <summary>The area whose ground shows at a site-local point inside the square, or null for plain snow.</summary>
            public Owner OwnerAt(Vector2 p)
            {
                List<Owner> owners = Owners;
                for (int i = owners.Count - 1; i >= 0; i--)
                {
                    Owner o = owners[i];
                    if (o.Bounds.Contains(p) && Inside(o.Pts, p)) return o;
                }
                return null;
            }

            /// <summary>True when a site-local point inside the square shows <paramref name="o"/>'s ground.</summary>
            public bool Shows(Owner o, Vector2 p)
            {
                if (!o.Bounds.Contains(p) || !Inside(o.Pts, p)) return false;
                List<Owner> owners = Owners;
                for (int i = owners.Count - 1; i > o.Rank; i--)
                {
                    Owner q = owners[i];
                    if (q.Bounds.Contains(p) && Inside(q.Pts, p)) return false;
                }
                return true;
            }

            /// <summary>Lets go of what only the build needed (the trapezoids, the ring, the mask, the tone's extras).</summary>
            public void Release()
            {
                foreach (Owner o in Owners) o.Traps.Clear();
                Ring.Clear();
                _blocked = null;
                ToneVerts.Clear();
                ToneUvs.Clear();
                ToneTris.Clear();
            }

            #region Where props can't go
            /// <summary>True when the point is inside the square, nothing the later passes build (streets with their
            /// sidewalks, railways, buildings, barriers) will stand within <paramref name="r"/> of it, and the site has it
            /// free.</summary>
            public bool Clear(Vector2 p, float r)
            {
                if (Mathf.Abs(p.x) > Half - r || Mathf.Abs(p.y) > Half - r) return false;
                if (_blocked == null) BuildMask();
                int k = Mathf.CeilToInt(r);
                int cx = Cell(p.x), cz = Cell(p.y);
                for (int z = cz - k; z <= cz + k; z++)
                    for (int x = cx - k; x <= cx + k; x++)
                        if (x >= 0 && z >= 0 && x < _n && z < _n && _blocked[z * _n + x]) return false;
                return Site.Free(p, r);
            }

            /// <summary>True when the 1 m cell under the point will have a street, a railway, a building or a barrier
            /// (or is outside the square).</summary>
            public bool Blocked(Vector2 p)
            {
                if (_blocked == null) BuildMask();
                int x = Cell(p.x), z = Cell(p.y);
                return x < 0 || z < 0 || x >= _n || z >= _n || _blocked[z * _n + x];
            }

            /// <summary>Works out where the later passes will build (see <see cref="Clear"/>), if it isn't known yet.</summary>
            public void Mask()
            {
                if (_blocked == null) BuildMask();
            }

            private int Cell(float v) => Mathf.FloorToInt(v + Half);

            /// <summary>Computes a boolean mask of cells that later passes will occupy (streets, railways, barriers, footprints), used for prop placement checks.</summary>
            private void BuildMask()
            {
                _n = Mathf.CeilToInt(Half * 2f) + 1;
                _blocked = new bool[_n * _n];
                Area a = Site.Area;
                foreach (Street s in a.Streets)
                    if (!s.Data.tunnel) MarkLine(s.Pts, s.Width * 0.5f + (s.Rank >= 2 ? 2.4f : 0.6f));
                foreach (Line l in a.Lines)
                    if (l.IsRailway && !l.Data.tunnel) MarkLine(l.Pts, 2.5f);
                foreach (Line l in a.Barriers) MarkLine(l.Pts, 0.5f);
                foreach (Footprint f in a.Buildings) MarkFootprint(f, 1.2f);
                foreach (Footprint f in a.Foreign) MarkFootprint(f, 1.2f);
            }

            private void MarkLine(List<Vector2> pts, float r)
            {
                for (int i = 0; i + 1 < pts.Count; i++) MarkCapsule(pts[i], pts[i + 1], r + 0.71f);
            }

            // Every cell whose middle lies within r of the segment: a row at a time, the span of the row inside the
            // capsule (convex, so one span: the union of what the two end discs and the band between them cover).
            private void MarkCapsule(Vector2 a, Vector2 b, float r)
            {
                float zMin = Mathf.Min(a.y, b.y) - r, zMax = Mathf.Max(a.y, b.y) + r;
                if (zMax < -Half || zMin > Half || Mathf.Max(a.x, b.x) + r < -Half || Mathf.Min(a.x, b.x) - r > Half) return;
                Vector2 d = b - a;
                float len = d.magnitude;
                Vector2 u = len > 1e-5f ? d / len : Vector2.right;
                int z0 = Mathf.Max(0, Mathf.CeilToInt(zMin + Half - 0.5f)), z1 = Mathf.Min(_n - 1, Mathf.FloorToInt(zMax + Half - 0.5f));
                for (int z = z0; z <= z1; z++)
                {
                    float zc = z + 0.5f - Half;
                    float lo = float.MaxValue, hi = float.MinValue;
                    Disc(a, zc, r, ref lo, ref hi);
                    Disc(b, zc, r, ref lo, ref hi);
                    if (len > 1e-5f)
                    {
                        // The band: |cross(p - a, u)| <= r and 0 <= dot(p - a, u) <= len, along the row z = zc.
                        float bLo = float.MinValue, bHi = float.MaxValue;
                        Clamp(u.y, -(zc - a.y) * u.x, -r, r, a.x, ref bLo, ref bHi);
                        Clamp(u.x, (zc - a.y) * u.y, 0f, len, a.x, ref bLo, ref bHi);
                        if (bLo <= bHi)
                        {
                            lo = Mathf.Min(lo, bLo);
                            hi = Mathf.Max(hi, bHi);
                        }
                    }
                    if (lo > hi) continue;
                    int x0 = Mathf.Max(0, Mathf.CeilToInt(lo + Half - 0.5f)), x1 = Mathf.Min(_n - 1, Mathf.FloorToInt(hi + Half - 0.5f));
                    for (int x = x0; x <= x1; x++) _blocked[z * _n + x] = true;
                }
            }

            /// <summary>Calculates the horizontal interval (lo, hi) of a disc intersecting the current row at height zc.</summary>
            private static void Disc(Vector2 c, float zc, float r, ref float lo, ref float hi)
            {
                float dz = zc - c.y, w2 = r * r - dz * dz;
                if (w2 < 0f) return;
                float w = Mathf.Sqrt(w2);
                lo = Mathf.Min(lo, c.x - w);
                hi = Mathf.Max(hi, c.x + w);
            }

            // Narrows [lo, hi] to the x where min <= k (x - ax) + c <= max.
            private static void Clamp(float k, float c, float min, float max, float ax, ref float lo, ref float hi)
            {
                // Skip calculations when k is effectively zero; treat the inequality as a constant check.
                if (Mathf.Abs(k) < 1e-6f)
                {
                    // If the constant term c is outside the allowed range, the interval becomes empty.
                    if (c < min || c > max)
                    {
                        lo = float.MaxValue;
                        hi = float.MinValue;
                    }
                    return;
                }
                float x0 = ax + (min - c) / k, x1 = ax + (max - c) / k;
                if (x0 > x1)
                {
                    float t = x0;
                    x0 = x1;
                    x1 = t;
                }
                // Intersect the new bounds with the existing interval.
                lo = Mathf.Max(lo, x0);
                hi = Mathf.Min(hi, x1);
            }

            // Every cell whose middle is inside the outline or within the margin of a wall: a band along each wall, and
            // the inside a row at a time between the outline's crossings (even-odd), so a big footprint costs its rows
            // and walls rather than its cells times its corners.
            private void MarkFootprint(Footprint f, float margin)
            {
                List<Vector2> p = f.Pts;
                Rect b = f.Bounds;
                if (b.xMax + margin < -Half || b.xMin - margin > Half || b.yMax + margin < -Half || b.yMin - margin > Half) return;
                for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) MarkCapsule(p[j], p[i], margin + 0.71f);
                int z0 = Mathf.Max(0, Mathf.CeilToInt(b.yMin + Half - 0.5f)), z1 = Mathf.Min(_n - 1, Mathf.FloorToInt(b.yMax + Half - 0.5f));
                List<float> xs = _crossings;
                for (int z = z0; z <= z1; z++)
                {
                    float zc = z + 0.5f - Half;
                    xs.Clear();
                    for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                    {
                        Vector2 a = p[i], c = p[j];
                        if ((a.y > zc) != (c.y > zc)) xs.Add(a.x + (zc - a.y) * (c.x - a.x) / (c.y - a.y));
                    }
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                    {
                        int x0 = Mathf.Max(0, Mathf.CeilToInt(xs[k] + Half - 0.5f)), x1 = Mathf.Min(_n - 1, Mathf.FloorToInt(xs[k + 1] + Half - 0.5f));
                        for (int x = x0; x <= x1; x++) _blocked[z * _n + x] = true;
                    }
                }
            }

            private readonly List<float> _crossings = new List<float>();
            #endregion

            #region Where an area's whole outline is
            /// <summary>The area's whole outline (within its region) in site-local metres: on a streamed chunk worked out
            /// from the data, so every chunk lays out the area's rows and stalls the same way; on a fixed map the patch
            /// itself. Null when a chunk can't tell where its region lies.</summary>
            public List<Vector2> Whole(Owner o)
            {
                if (o.WholeDone) return o.Whole;
                o.WholeDone = true;
                if (!Site.Chunked) return o.Whole = o.Patch.Pts;
                int[] pts = o.Patch.Data.pts;
                if (pts == null || pts.Length < 6 || !Shift(o, out Vector2 shift)) return null;
                var list = new List<Vector2>(pts.Length / 2);
                for (int i = 0; i + 1 < pts.Length; i += 2)
                    list.Add(new Vector2((float)(shift.x + pts[i] * 0.1), (float)(shift.y + pts[i + 1] * 0.1)));
                float size = Site.Build.RegionSize;
                o.Region = new Rect(shift.x, shift.y, size, size);
                list = MapGeometry.ClipPolygon(list, o.Region);
                return o.Whole = list.Count >= 3 ? list : null;
            }

            // Region metres + shift = site-local metres. A chunk's areas all come from its own region, whose corner lies
            // a whole number of chunks and a half from the chunk's middle, so any one point both in the data and in the
            // chunk (a node, a building's corner, a corner of the area itself) gives the shift exactly.
            /// <summary>Determines the site‑local coordinate shift aligning chunk data with the global map, based on known points.</summary>
            private bool Shift(Owner o, out Vector2 shift)
            {
                if (!_shiftTried)
                {
                    _shiftTried = true;
                    Area a = Site.Area;
                    foreach (Node n in a.Nodes)
                        if (Snap(n.At, n.Data.x, n.Data.z, out _shift)) { _hasShift = true; break; }
                    if (!_hasShift)
                        foreach (Spot s in a.Points)
                            if (Snap(s.At, s.Data.x, s.Data.z, out _shift)) { _hasShift = true; break; }
                    if (!_hasShift)
                        foreach (Footprint f in a.Buildings)
                            if (SnapAny(f.Pts, f.Data.pts, out _shift)) { _hasShift = true; break; }
                }
                if (_hasShift)
                {
                    shift = _shift;
                    return true;
                }
                return SnapAny(o.Patch.Pts, o.Patch.Data.pts, out shift);
            }

            // The first point strictly inside the square is one of the data's own (cutting adds points only on the
            // square's edges): find which.
            private bool SnapAny(List<Vector2> local, int[] data, out Vector2 shift)
            {
                shift = default;
                if (data == null) return false;
                for (int i = 0; i < local.Count; i++)
                {
                    Vector2 v = local[i];
                    if (Mathf.Abs(v.x) > Half - 0.01f || Mathf.Abs(v.y) > Half - 0.01f) continue;
                    for (int k = 0; k + 1 < data.Length; k += 2)
                        if (Snap(v, data[k], data[k + 1], out shift)) return true;
                    return false;
                }
                return false;
            }

            private bool Snap(Vector2 local, int x, int z, out Vector2 shift)
            {
                double sx = local.x - x * 0.1, sz = local.y - z * 0.1, chunk = Half * 2.0;
                double gx = System.Math.Round((sx + Half) / chunk) * chunk - Half, gz = System.Math.Round((sz + Half) / chunk) * chunk - Half;
                shift = new Vector2((float)gx, (float)gz);
                return System.Math.Abs(gx - sx) < 0.002 && System.Math.Abs(gz - sz) < 0.002;
            }
            #endregion
        }

        /// <summary>Even-odd point in polygon without the edge test (fast enough for every prop and grid point).</summary>
        private static bool Inside(List<Vector2> p, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            {
                Vector2 a = p[i], b = p[j];
                if ((a.y > q.y) != (b.y > q.y) && q.x < (b.x - a.x) * (q.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        /// <summary>The batches a fixed map draws <paramref name="p"/> into: the playable ground's inside its square, the
        /// scenery's past it.</summary>
        private static OutdoorBatches Batch(Ground g, Vector3 p)
            => Mathf.Abs(p.x) <= g.Half && Mathf.Abs(p.z) <= g.Half ? g.Site.Town.Out : g.Site.Town.Far;
        #endregion
    }
}
