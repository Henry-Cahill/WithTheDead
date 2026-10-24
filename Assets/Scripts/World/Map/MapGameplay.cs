using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// What the real-world map means for play. As the last pass of every site (<see cref="Build"/>) it notes how thick
    /// the Zone is there (<see cref="DensityAt"/>, for <see cref="ZombieSpawner"/> on the streamed map), publishes the
    /// real landmarks people navigate by (MapGameplay.Landmarks: the courthouse, the churches, the water tower... on
    /// the Zone map and in the descriptions of places) and dresses the points of interest (gas pumps, the named
    /// places' signs). It lays out the raid's exits where the main roads leave the start square, named by their routes
    /// ("US 31E north") and where they go, and the survivor's spawn on a downtown sidewalk clear of the junctions
    /// (<see cref="KentuckyMap.Build"/>, <see cref="KentuckyMap.PlanStart"/>); and it decides the loot of every
    /// building by what it really was (MapGameplay.Purpose, MapGameplay.Loot: <see cref="ContainersFor(MapBuilding, string)"/>
    /// for the interiors, <see cref="Loot"/> for buildings without rooms). Everything random comes from
    /// <see cref="MapStyle"/>, so it's the same in every build of a piece.
    /// </summary>
    internal static partial class MapGameplay
    {
        private const int MaxContainers = 450;   // per site
        private const int MaxSigns = 30;          // per 400 m square
        // Salts for this pass's own random choices (MapStyle).
        private const int SaltPumps = 0x6A50, SaltSign = 0x6A51, SaltExitSign = 0x6A52;

        internal static IEnumerable<string> Build(Site site)
        {
            RecordDensity(site);
            Landmarks(site);
            yield return "danger and landmarks";
            if (site.Detail == Detail.Full)
            {
                Points(site);
                yield return "points";
            }
            if (!site.Chunked) TallyLoot(site);
        }

        #region Exits
        /// <summary>One way out of the start square, as planned from its streets.</summary>
        private sealed class ExitPlan
        {
            public Vector2 At, Out;
            public int Side;
            public float Score, RoadHalf;
            public Street Street;
            public string Route, Name, Hint, Destination;
            public float Miles;
        }

        /// <summary>The fixed map's exits (see <see cref="DefineExits(Area, float, RaidMapInfo)"/>), with a guide sign
        /// at each road out.</summary>
        internal static void DefineExits(Site site)
        {
            List<ExitPlan> plans = PlanExits(site.Area, site.Half);
            Publish(plans, site.Area, site.Town.Info);
            foreach (ExitPlan e in plans)
            {
                if (e.Street == null || site.Detail != Detail.Full) continue;
                if (!GuideSign(site.Town.Out.Solid(To3(e.At, 0f)), e, p => site.FreeBeside(p, 0.7f), out Vector2 spot, out float yaw, out float boardW))
                    continue;
                site.Town.PropCollider(To3(spot, 1.6f), new Vector3(boardW * 0.7f, 3.2f, 0.25f), yaw);
                site.Take(spot, 0.8f);
            }
        }

        /// <summary>
        /// The exits where <paramref name="area"/>'s roads leave the square of half-side <paramref name="half"/> around
        /// its middle: on each side the best road out (the most important, a numbered route first, the one heading
        /// most squarely out), named by its route and direction ("US 31E north") and hinting where it goes (the biggest
        /// place ahead: "North 3rd Street out of Bardstown, towards Louisville, 34 miles"); a second on a side that has
        /// another main road well apart; a side no road leaves gets one across the snow. Read from the data alone, so
        /// the streamed map places them before the ground they stand on is built (it gets its guide signs then too,
        /// beside the road, clear of the start's props as their colliders have them).
        /// </summary>
        internal static void DefineExits(Area area, float half, RaidMapInfo info)
        {
            List<ExitPlan> plans = PlanExits(area, half);
            Publish(plans, area, info);
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            if (streamer == null || streamer.World == null) return;
            Physics.SyncTransforms();
            int mask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
            System.Func<Vector2, bool> free = p => OffRoads(area, p, 0.7f)
                && !Physics.CheckBox(To3(p, 1.6f), new Vector3(1.6f, 1.4f, 1.6f), Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
            var signs = new RaidMesh();
            var posts = new List<(Vector2 at, float yaw, float width)>();
            foreach (ExitPlan e in plans)
                if (e.Street != null && GuideSign(signs, e, free, out Vector2 spot, out float yaw, out float boardW)) posts.Add((spot, yaw, boardW));
            GameObject go = signs.Emit("ExitSigns", streamer.World, Vector3.zero);
            if (go == null) return;
            DecorPalette.Apply();
            foreach ((Vector2 at, float yaw, float width) in posts)
            {
                var c = new GameObject("ExitSignCollider");
                c.transform.SetParent(go.transform, false);
                c.transform.SetPositionAndRotation(To3(at, 1.6f), Quaternion.Euler(0f, yaw, 0f));
                c.AddComponent<BoxCollider>().size = new Vector3(width * 0.7f, 3.2f, 0.25f);
            }
        }

        // True when a disc of radius r at p is clear of every street's carriageway (the start's data, site-local).
        private static bool OffRoads(Area area, Vector2 p, float r)
        {
            foreach (Street s in area.Streets)
            {
                if (s.Data.tunnel) continue;
                float reach = s.Width * 0.5f + r, reach2 = reach * reach;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], ab = s.Pts[i + 1] - a;
                    float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                    if ((a + ab * t - p).sqrMagnitude < reach2) return false;
                }
            }
            return true;
        }

        private static void Publish(List<ExitPlan> plans, Area area, RaidMapInfo info)
        {
            foreach (ExitPlan e in plans)
                info.Exits.Add(new RaidExit
                {
                    Name = e.Name, Hint = e.Hint, Position = To3(e.At, 0f), Radius = 4.5f, ExtractSeconds = 8f, Side = e.Side,
                });
            var line = new System.Text.StringBuilder("[CloneZ] Map gameplay at " + area.Place + ": exits ");
            for (int i = 0; i < plans.Count; i++) line.Append(i > 0 ? "; " : "").Append(plans[i].Name).Append(" (").Append(plans[i].Hint).Append(')');
            line.Append(". ").Append(LandmarkSummary(8)).Append(". The Zone's density at the middle ")
                .Append(DensityAt(Vector3.zero).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                .Append(", loot richness ").Append(LootRichnessAt(Vector3.zero).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append('.');
            Debug.Log(line.ToString());
            // The harness's builds list every landmark with its rank, to check what the map publishes.
            if (!Application.isPlaying) Debug.Log("[CloneZ] Map landmarks at " + area.Place + ": " + LandmarkSummary(int.MaxValue, true));
        }

        private static List<ExitPlan> PlanExits(Area area, float half)
        {
            float edge = half - 3f;
            var square = Rect.MinMaxRect(-edge, -edge, edge, edge);
            var found = new List<ExitPlan>();
            foreach (Street s in area.Streets)
            {
                if (s.Rank <= 0 || s.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                {
                    if (piece.Count < 2) continue;
                    for (int end = 0; end < 2; end++)
                    {
                        Vector2 p = end == 0 ? piece[0] : piece[piece.Count - 1];
                        int side = SideOf(p, edge);
                        if (side < 0) continue;
                        Vector2 inner = end == 0 ? piece[1] : piece[piece.Count - 2];
                        Vector2 dir = p - inner;
                        if (dir.sqrMagnitude < 1e-4f) continue;
                        dir.Normalize();
                        float squarely = Vector2.Dot(dir, SideNormal(side));
                        if (squarely < 0.2f) continue;   // running along the edge, not out of town
                        string route = FirstRoute(s.Data.route);
                        float score = s.Rank * 1000f + (route != null ? 400f : 0f) + squarely * 150f
                            - (side == 0 || side == 2 ? Mathf.Abs(p.x) : Mathf.Abs(p.y)) * 0.5f;
                        found.Add(new ExitPlan
                        {
                            At = p, Out = dir, Side = side, Score = score, Street = s, Route = route,
                            RoadHalf = Mathf.Max(1.5f, s.Width * 0.5f),
                        });
                    }
                }
            }
            found.Sort((a, b) => b.Score.CompareTo(a.Score));

            var exits = new List<ExitPlan>(6);
            for (int side = 0; side < 4; side++)
                foreach (ExitPlan e in found)
                    if (e.Side == side) { exits.Add(e); break; }
            // A second road out on a side: a main road well apart from the exits already there.
            foreach (ExitPlan e in found)
            {
                if (exits.Count >= 6) break;
                if (e.Street.Rank < 4 || exits.Contains(e)) continue;
                bool apart = true;
                foreach (ExitPlan x in exits) apart &= Vector2.Distance(x.At, e.At) > 120f;
                if (apart) exits.Add(e);
            }
            // A side with no road out: across the snow.
            for (int side = 0; side < 4; side++)
            {
                bool has = false;
                foreach (ExitPlan e in exits) has |= e.Side == side;
                if (!has) exits.Add(new ExitPlan { At = SideMiddle(side, edge), Out = SideNormal(side), Side = side });
            }
            exits.Sort((a, b) => a.Side != b.Side ? a.Side.CompareTo(b.Side) : Clockwise(a).CompareTo(Clockwise(b)));
            Name(exits, area);
            return exits;
        }

        // Where along its side an exit is, clockwise (so exits list north, east, south, west, each side in order).
        private static float Clockwise(ExitPlan e) => e.Side == 0 ? e.At.x : e.Side == 1 ? -e.At.y : e.Side == 2 ? -e.At.x : e.At.y;

        // Names the exits by route and direction (each by a route no other exit uses, when the roads allow it: a road
        // carrying US 31E and US 150 north and one carrying US 31E and US 62 west become "US 31E north" and "US 62
        // west"), with where the road goes; the snowfields by their side.
        private static void Name(List<ExitPlan> exits, Area area)
        {
            AssignRoutes(exits);
            var around = new List<KeyValuePair<MapPlace, Vector3>>();
            foreach (ExitPlan e in exits)
            {
                string dir = SideName(e.Side);
                if (e.Street == null)
                {
                    e.Name = "Snowfield (" + dir + ")";
                    e.Hint = "Across the snow, " + dir + " edge of " + area.Place;
                    continue;
                }
                string route = e.Route;
                string street = StreetName(e.Street);
                // "South 3rd Street" leaving south needs no "(south)".
                bool says = street != null && street.StartsWith(dir + " ", System.StringComparison.OrdinalIgnoreCase);
                e.Name = route != null ? route + " " + dir : says ? street : street + " (" + dir + ")";
                Destination(e, around);
                string way = street == route || street == null ? (route ?? "The road") : street;
                e.Hint = way + " out of " + area.Place
                    + (e.Destination != null ? ", towards " + e.Destination + ", " + Mathf.Max(1, Mathf.RoundToInt(e.Miles)) + " miles" : "");
            }
        }

        // The route each road out goes by: as many different ones as can be, each road's first-listed route when it
        // makes no difference (a search over the few exits and their few routes).
        private static void AssignRoutes(List<ExitPlan> exits)
        {
            var options = new List<string>[exits.Count];
            for (int i = 0; i < exits.Count; i++)
            {
                options[i] = new List<string>();
                if (exits[i].Street != null) foreach (string r in Routes(exits[i].Street.Data.route)) options[i].Add(r);
            }
            var pick = new int[exits.Count];
            var best = new int[exits.Count];
            int bestScore = int.MinValue;
            var used = new Dictionary<string, int>();
            void Search(int i, int score)
            {
                if (i == exits.Count)
                {
                    if (score > bestScore) { bestScore = score; System.Array.Copy(pick, best, pick.Length); }
                    return;
                }
                if (options[i].Count == 0) { pick[i] = -1; Search(i + 1, score); return; }
                for (int k = 0; k < options[i].Count && k < 4; k++)
                {
                    string r = options[i][k];
                    used.TryGetValue(r, out int n);
                    pick[i] = k;
                    used[r] = n + 1;
                    Search(i + 1, score + (n == 0 ? 10 : 0) - k);
                    used[r] = n;
                }
            }
            Search(0, 0);
            for (int i = 0; i < exits.Count; i++) exits[i].Route = best[i] >= 0 && options[i].Count > 0 ? options[i][best[i]] : null;
        }

        // The biggest place the road heads for: within 40° of its way out and 100 km, the larger and nearer the better.
        private static void Destination(ExitPlan e, List<KeyValuePair<MapPlace, Vector3>> around)
        {
            Vector2 heading = (e.Out + SideNormal(e.Side)).normalized;
            Vector3 from = To3(e.At, 0f);
            PlacesAround(from, 100000f, 1500, around);
            float best = float.MinValue;
            foreach (KeyValuePair<MapPlace, Vector3> kv in around)
            {
                var to = new Vector2(kv.Value.x - from.x, kv.Value.z - from.z);
                float d = to.magnitude;
                if (d < 4000f) continue;
                float angle = Vector2.Angle(heading, to);
                if (angle > 40f) continue;
                float score = 2f * Mathf.Log10(Population(kv.Key)) - d / 25000f - angle / 30f;
                if (score <= best) continue;
                best = score;
                e.Destination = kv.Key.name;
                e.Miles = d / 1609.34f;
            }
        }

        private static string StreetName(Street s)
        {
            if (!string.IsNullOrEmpty(s.Data.name) && s.Data.name != s.Data.route) return s.Data.name;
            return KindName(s.Data.kind);
        }

        /// <summary>The routes a road carries ("US 31E;US 150" → US 31E, US 150).</summary>
        private static IEnumerable<string> Routes(string route)
        {
            if (string.IsNullOrEmpty(route)) yield break;
            foreach (string r in route.Split(';'))
            {
                string t = r.Trim();
                if (t.Length > 0) yield return t;
            }
        }

        private static string FirstRoute(string route)
        {
            foreach (string r in Routes(route)) return r;
            return null;
        }

        private static int SideOf(Vector2 p, float edge)
        {
            if (Mathf.Abs(p.y - edge) < 0.01f) return 0;
            if (Mathf.Abs(p.x - edge) < 0.01f) return 1;
            if (Mathf.Abs(p.y + edge) < 0.01f) return 2;
            if (Mathf.Abs(p.x + edge) < 0.01f) return 3;
            return -1;
        }

        private static Vector2 SideNormal(int side) => side == 0 ? Vector2.up : side == 1 ? Vector2.right : side == 2 ? Vector2.down : Vector2.left;

        private static Vector2 SideMiddle(int side, float edge) => SideNormal(side) * edge;

        private static string SideName(int side) => side == 0 ? "north" : side == 1 ? "east" : side == 2 ? "south" : "west";

        private static string KindName(string kind)
        {
            switch (kind)
            {
                case "motorway": case "trunk": return "The highway";
                case "primary": case "secondary": return "The state road";
                case "track": return "A farm track";
                case "service": return "A service road";
                default: return "A side street";
            }
        }

        // A green guide sign beside the road out, facing the traffic leaving town: the route and its way above, where it
        // goes and how far below (miles, as Kentucky's were). Drawn into <paramref name="m"/> in the site's (or the
        // world's) metres at the first spot <paramref name="free"/> allows (beside the road, a little back from the
        // edge); false when there's none. Where it stands, its turn and its board's width come back for its collider.
        private static bool GuideSign(RaidMesh m, ExitPlan e, System.Func<Vector2, bool> free, out Vector2 spot, out float yaw, out float boardW)
        {
            var right = new Vector2(e.Out.y, -e.Out.x);
            spot = Vector2.zero;
            yaw = 0f;
            boardW = 0f;
            bool ok = false;
            for (int tryAt = 0; tryAt < 8 && !ok; tryAt++)
            {
                float back = 7f + (tryAt >> 1) * 4f;
                float sideways = (tryAt & 1) == 0 ? 1f : -1f;
                spot = e.At - e.Out * back + right * sideways * (e.RoadHalf + 2.2f);
                ok = free == null || free(spot);
            }
            if (!ok) return false;
            var rng = MapStyle.Rng(MapStyle.Key(e.At), SaltExitSign);
            string top = e.Route != null ? (e.Name ?? e.Route).ToUpperInvariant() : SideName(e.Side).ToUpperInvariant();
            string bottom = e.Destination != null
                ? e.Destination.ToUpperInvariant() + "  " + Mathf.Max(1, Mathf.RoundToInt(e.Miles))
                : (e.Street != null ? StreetName(e.Street).ToUpperInvariant() : "");
            if (bottom.Length > 26) bottom = bottom.Substring(0, 26).TrimEnd();
            var green = new Color(0.10f, 0.36f, 0.20f);
            var white = new Color(0.93f, 0.94f, 0.92f);
            Vector3 at = To3(spot, 0f);
            // The board faces back into town, a little askew.
            yaw = Mathf.Atan2(-e.Out.x, -e.Out.y) * Mathf.Rad2Deg + MapStyle.Range(rng, -4f, 4f);
            m.SetFrame(at, yaw);
            Surf low = DecorMaterials.Sign(bottom.Length > 0 ? bottom : top, green, white, out float lowAspect);
            Surf high = DecorMaterials.Sign(top, green, white, out float highAspect);
            float lowH = 0.62f, highH = 0.5f;
            float lowW = Mathf.Min(4.2f, lowH * lowAspect), highW = Mathf.Min(3.6f, highH * highAspect);
            boardW = Mathf.Max(lowW, highW) + 0.3f;
            var post = DecorPalette.Matte(new Color(0.42f, 0.43f, 0.44f));
            for (int k = -1; k <= 1; k += 2)
                m.Box(new Vector3(k * boardW * 0.32f, 1.6f, -0.06f), new Vector3(0.09f, 3.2f, 0.09f), post);
            var backing = DecorPalette.Matte(green * 0.85f);
            m.Box(new Vector3(0f, 2.55f, 0f), new Vector3(boardW, 1.45f, 0.05f), backing);
            m.Upright(new Vector3(0f, 2.98f, 0.027f), Vector3.forward, highW, highH, high, new Rect(0f, 0f, 1f, 1f));
            m.Upright(new Vector3(0f, 2.28f, 0.027f), Vector3.forward, lowW, lowH, low, new Rect(0f, 0f, 1f, 1f));
            m.Box(new Vector3(0f, 3.31f, 0f), new Vector3(boardW, 0.06f, 0.1f), DecorPalette.Matte(new Color(0.94f, 0.95f, 0.97f)));
            m.ResetFrame();
            return true;
        }
        #endregion

        #region Spawn
        /// <summary>The fixed map's spawn (see <see cref="PickSpawn(Area, float, System.Func{Vector2, bool}, out Vector2, out float)"/>).</summary>
        internal static void PickSpawn(Site site)
        {
            PickSpawn(site.Area, site.Half, p => site.FreeBeside(p, 0.6f), out Vector2 spot, out float yaw);
            site.Town.Info.PlayerSpawn = To3(spot, 0.6f);
            site.Town.Info.PlayerSpawnYaw = yaw;
        }

        /// <summary>
        /// Where the survivor starts: on a sidewalk of a town street near the middle of <paramref name="area"/> (20 to 70 m
        /// out is best), never within 18 m of a junction or on the square's edge band, where <paramref name="free"/> finds
        /// it clear, preferring a street with named places along it; facing along the street towards the middle (so the
        /// opening frame looks downtown). Falls back to the sidewalk spot nearest the middle, then the middle itself.
        /// </summary>
        internal static void PickSpawn(Area area, float half, System.Func<Vector2, bool> free, out Vector2 spot, out float yaw)
        {
            List<Vector2> junctions = JunctionPoints(area);
            var interest = new List<Vector2>();
            foreach (Spot s in area.Points) if (!string.IsNullOrEmpty(s.Data.name)) interest.Add(s.At);
            foreach (Footprint f in area.Buildings) if (!string.IsNullOrEmpty(f.Data.name) || !string.IsNullOrEmpty(f.Data.use)) interest.Add(f.Middle);

            spot = Vector2.zero;
            yaw = 0f;
            float best = float.MinValue, nearest = float.MaxValue;
            Vector2 fallback = Vector2.zero;
            float fallbackYaw = 0f;
            bool any = false, anyFallback = false;
            foreach (Street s in area.Streets)
            {
                if (s.Rank < 2 || s.Rank > 5 || s.Data.tunnel || s.Data.bridge) continue;
                float off = s.Width * 0.5f + 1.6f;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], ab = s.Pts[i + 1] - a;
                    float len = ab.magnitude;
                    if (len < 0.5f) continue;
                    Vector2 d = ab / len, left = new Vector2(-d.y, d.x);
                    for (float t = 1.5f; t < len; t += 3f)
                    {
                        Vector2 on = a + d * t;
                        for (int k = -1; k <= 1; k += 2)
                        {
                            Vector2 p = on + left * (k * off);
                            float dist = p.magnitude;
                            if (Mathf.Abs(p.x) > half - 15f || Mathf.Abs(p.y) > half - 15f) continue;
                            float toJunction = NearestDistance(junctions, p);
                            if (toJunction < 6f) continue;
                            if (!free(p)) continue;
                            Vector2 facing = Vector2.Dot(d, -p) >= 0f ? d : -d;
                            float facingYaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;
                            if (dist < nearest)
                            {
                                nearest = dist;
                                fallback = p;
                                fallbackYaw = facingYaw;
                                anyFallback = true;
                            }
                            if (toJunction < 18f) continue;
                            float score = -Mathf.Abs(dist - 40f) / 40f + Mathf.Min(toJunction, 40f) / 60f
                                + Mathf.Min(1f, 0.2f * CountWithin(interest, p, 35f))
                                + (s.Rank <= 4 ? 0.15f : 0f);
                            if (score <= best) continue;
                            best = score;
                            spot = p;
                            yaw = facingYaw;
                            any = true;
                        }
                    }
                }
            }
            if (!any && anyFallback)
            {
                spot = fallback;
                yaw = fallbackYaw;
            }
        }

        /// <summary>Where streets meet: points two or more streets (by id) share, or where three pieces end together.</summary>
        private static List<Vector2> JunctionPoints(Area area)
        {
            var at = new Dictionary<long, (Vector2 p, long street, int streets, int ends)>();
            foreach (Street s in area.Streets)
            {
                if (s.Rank <= 0 || s.Data.tunnel) continue;
                for (int i = 0; i < s.Pts.Count; i++)
                {
                    Vector2 p = s.Pts[i];
                    long key = MapStyle.Key(p);
                    bool end = i == 0 || i == s.Pts.Count - 1;
                    if (!at.TryGetValue(key, out var v)) at[key] = (p, s.Data.id, 1, end ? 1 : 0);
                    else at[key] = (v.p, v.street, v.streets + (v.street != s.Data.id ? 1 : 0), v.ends + (end ? 1 : 0));
                }
            }
            var list = new List<Vector2>();
            foreach (var v in at.Values) if (v.streets >= 2 || v.ends >= 3) list.Add(v.p);
            return list;
        }

        private static float NearestDistance(List<Vector2> points, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Count; i++) best = Mathf.Min(best, (points[i] - p).sqrMagnitude);
            return Mathf.Sqrt(best);
        }

        private static int CountWithin(List<Vector2> points, Vector2 p, float within)
        {
            int n = 0;
            float w2 = within * within;
            for (int i = 0; i < points.Count; i++) if ((points[i] - p).sqrMagnitude < w2) n++;
            return n;
        }
        #endregion

        #region Points of interest
        // Gas pumps where a gas station is mapped without the canopy that brings its own, and a sign with the real name
        // by the places people would look for (the nearest the middle first), each at the nearest free spot towards the
        // street. Each from its point's own random stream.
        private static void Points(Site site)
        {
            var spots = new List<Spot>(site.Area.Points);
            spots.Sort((p, q) => p.At.sqrMagnitude.CompareTo(q.At.sqrMagnitude));
            int signs = 0, maxSigns = Mathf.Max(3, Mathf.RoundToInt(MaxSigns * site.AreaShare));
            foreach (Spot s in spots)
            {
                System.Random rng = MapStyle.Rng(s.Data.id, s.Data.kind == "fuel" ? SaltPumps : SaltSign);
                if (s.Data.kind == "fuel")
                {
                    GasSign(site, s, rng);
                    if (HasCanopy(site, s.At)) continue;
                    Vector2 along = site.Area.NearestStreetDirection(s.At, 40f);
                    Vector2 across = along == Vector2.zero ? Vector2.right : new Vector2(-along.y, along.x);
                    float pumpYaw = along == Vector2.zero ? 0f : Mathf.Atan2(along.x, along.y) * Mathf.Rad2Deg + 90f;
                    for (int k = -1; k <= 1; k += 2)
                    {
                        if (!NearestFree(site, s.At + across * (k * 2.2f), 1.2f, out Vector2 at)) continue;
                        Vector3 at3 = To3(at, 0f);
                        RaidMesh m = site.Town.Out.Solid(at3);
                        float yaw = pumpYaw + MapStyle.Range(rng, -4f, 4f);
                        m.SetFrame(at3, yaw);
                        DecorProps.GasPump(m, rng);
                        m.ResetFrame();
                        site.Town.PropCollider(at3 + Vector3.up * 0.9f, new Vector3(1.0f, 1.8f, 1.6f), yaw);
                        site.Take(at, 1.2f);
                    }
                    continue;
                }
                if (signs >= maxSigns || string.IsNullOrEmpty(s.Data.name) || !SignColours(s.Data.kind, out Color bg, out Color fg)) continue;
                // Hand-painted: the colours a little off.
                bg.r = Mathf.Clamp01(bg.r + MapStyle.Range(rng, -0.02f, 0.02f));
                bg.g = Mathf.Clamp01(bg.g + MapStyle.Range(rng, -0.02f, 0.02f));
                bg.b = Mathf.Clamp01(bg.b + MapStyle.Range(rng, -0.02f, 0.02f));
                fg.r = Mathf.Clamp01(fg.r + MapStyle.Range(rng, -0.02f, 0.02f));
                fg.g = Mathf.Clamp01(fg.g + MapStyle.Range(rng, -0.02f, 0.02f));
                fg.b = Mathf.Clamp01(fg.b + MapStyle.Range(rng, -0.02f, 0.02f));
                // Between the place and its street: a step from the point towards the curb.
                Vector2 toStreet = site.Area.NearestStreetDirection(s.At, 40f);
                Vector2 near = toStreet == Vector2.zero ? s.At : s.At + toStreet * 3f;
                if (!NearestFree(site, near, 1.4f, out Vector2 spot)) continue;
                string text = s.Data.name.ToUpperInvariant();
                if (text.Length > 24) text = text.Substring(0, 24).TrimEnd();
                Vector2 facing = site.Area.NearestStreetDirection(spot, 40f);
                float signYaw = facing == Vector2.zero ? 0f : Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg + MapStyle.Range(rng, -5f, 5f);
                Vector3 spot3 = To3(spot, 0f);
                RaidMesh sm = site.Town.Out.Solid(spot3);
                sm.SetFrame(spot3, signYaw);   // the board faces +Z: towards the street
                DecorProps.PostSign(sm, rng, text, bg, fg, 0.42f, 1.3f);
                sm.ResetFrame();
                site.Town.PropCollider(spot3 + Vector3.up * 1.1f, new Vector3(2.4f, 2.2f, 0.3f), signYaw);
                site.Take(spot, 1.4f);
                signs++;
            }
        }

        // A gas station's pylon sign at the curb, the tallest thing on its block: the station's name (or just GAS) and
        // January 1993's price, seen both ways down the road.
        private static void GasSign(Site site, Spot s, System.Random rng)
        {
            Vector2 toStreet = site.Area.NearestStreetDirection(s.At, 40f);
            if (toStreet == Vector2.zero) return;
            Vector2 spot = Vector2.zero;
            bool found = false;
            for (float k = 2f; k <= 30f; k += 1.5f)
            {
                Vector2 p = s.At + toStreet * k;
                if (site.FreeBeside(p, 0.7f)) { spot = p; found = true; }
                else if (found) break;
            }
            if (!found) return;
            string name = string.IsNullOrEmpty(s.Data.name) ? "GAS" : s.Data.name.ToUpperInvariant();
            if (name.Length > 16) name = name.Substring(0, 16).TrimEnd();
            bool named = name != "GAS";
            var red = new Color(0.64f, 0.12f, 0.10f);
            var white = new Color(0.93f, 0.92f, 0.88f);
            Surf nameSurf = DecorMaterials.Sign(name, named ? red : white, named ? white : red, out float nameAspect);
            Surf priceSurf = DecorMaterials.Sign("REGULAR 1.07", white, new Color(0.10f, 0.10f, 0.12f), out float priceAspect);
            float nameH = 0.9f, priceH = 0.45f;
            float nameW = Mathf.Min(3.2f, nameH * nameAspect), priceW = Mathf.Min(2.6f, priceH * priceAspect);
            float boardW = Mathf.Max(nameW, priceW) + 0.3f;
            Vector3 at = To3(spot, 0f);
            float yaw = Mathf.Atan2(toStreet.x, toStreet.y) * Mathf.Rad2Deg + 90f + MapStyle.Range(rng, -3f, 3f);
            RaidMesh m = site.Town.Out.Solid(at);
            m.SetFrame(at, yaw);
            var steel = DecorPalette.Metal(new Color(0.46f, 0.47f, 0.48f));
            m.Box(new Vector3(0f, 2.9f, 0f), new Vector3(0.22f, 5.8f, 0.22f), steel);
            m.Box(new Vector3(0f, 5.25f, 0f), new Vector3(boardW, 1.15f, 0.24f), DecorPalette.Matte(named ? red * 0.8f : white * 0.9f));
            m.Box(new Vector3(0f, 4.27f, 0f), new Vector3(boardW, 0.62f, 0.18f), DecorPalette.Matte(white * 0.85f));
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 n = k > 0 ? Vector3.forward : Vector3.back;
                m.Upright(new Vector3(0f, 5.25f, k * 0.125f), n, nameW, nameH, nameSurf, new Rect(0f, 0f, 1f, 1f));
                m.Upright(new Vector3(0f, 4.27f, k * 0.095f), n, priceW, priceH, priceSurf, new Rect(0f, 0f, 1f, 1f));
            }
            m.Box(new Vector3(0f, 5.87f, 0f), new Vector3(boardW + 0.02f, 0.08f, 0.3f), DecorPalette.Matte(new Color(0.94f, 0.95f, 0.97f)));
            m.ResetFrame();
            site.Town.PropCollider(at + Vector3.up * 1.5f, new Vector3(0.4f, 3f, 0.4f), yaw);
            site.Take(spot, 0.8f);
        }

        // A canopy (or a building used as a gas station) within 30 m: the building pass puts the pumps under it.
        private static bool HasCanopy(Site site, Vector2 at)
        {
            foreach (Footprint f in site.Area.Buildings)
            {
                if (f.Data.kind != "canopy" && f.Data.use != "fuel") continue;
                Rect r = f.Bounds;
                if (at.x > r.xMin - 30f && at.x < r.xMax + 30f && at.y > r.yMin - 30f && at.y < r.yMax + 30f) return true;
            }
            return false;
        }

        /// <summary>The free spot nearest <paramref name="near"/>, within 12 m, for something of radius
        /// <paramref name="radius"/>.</summary>
        private static bool NearestFree(Site site, Vector2 near, float radius, out Vector2 at)
        {
            for (float r = 0f; r <= 12f; r += 2f)
            {
                int steps = r == 0f ? 1 : 10;
                for (int i = 0; i < steps; i++)
                {
                    float angle = i * Mathf.PI * 2f / steps;
                    at = near + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                    if (site.Free(at, radius)) return true;
                }
            }
            at = near;
            return false;
        }

        private static bool SignColours(string kind, out Color bg, out Color fg)
        {
            bg = new Color(0.86f, 0.84f, 0.78f);
            fg = new Color(0.12f, 0.12f, 0.12f);
            switch (kind)
            {
                case "police": case "fire_station": case "townhall": case "courthouse": case "post_office": case "library":
                case "community_centre": case "prison":
                    bg = new Color(0.16f, 0.22f, 0.36f);
                    fg = new Color(0.90f, 0.90f, 0.86f);
                    return true;
                case "hospital": case "clinic": case "doctors": case "pharmacy": case "dentist": case "veterinary":
                    fg = new Color(0.62f, 0.10f, 0.08f);
                    return true;
                case "school":
                    bg = new Color(0.18f, 0.30f, 0.22f);
                    fg = new Color(0.90f, 0.88f, 0.80f);
                    return true;
                case "place_of_worship":
                    bg = new Color(0.30f, 0.22f, 0.16f);
                    fg = new Color(0.90f, 0.86f, 0.76f);
                    return true;
                case "bank": case "restaurant": case "fast_food": case "cafe": case "bar": case "pub": case "car_repair":
                    return true;
                default:
                    return kind != null && kind.StartsWith("shop:");
            }
        }
        #endregion

        private static bool Near(List<Vector2> spots, Vector2 p, float within)
        {
            for (int i = 0; i < spots.Count; i++)
                if (Vector2.Distance(spots[i], p) < within) return true;
            return false;
        }
    }
}
