using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// What the real-world map means for play: the raid's exits where its streets leave the square, the survivor's
    /// spawn on the sidewalk nearest the middle, the loot in the buildings (<see cref="Loot"/>, called by
    /// <see cref="BuildingPass"/> for every enterable building), and the points of interest: gas pumps where a gas
    /// station is mapped and signs naming the places people would look for (<see cref="BuildPoints"/>).
    /// </summary>
    internal static class MapGameplay
    {
        private const int MaxContainers = 450;   // per site
        private const int MaxSigns = 30;

        #region Exits and spawn
        /// <summary>The fixed map's exits: see <see cref="DefineExits(Area, float, RaidMapInfo)"/>.</summary>
        internal static void DefineExits(Site site) => DefineExits(site.Area, site.Half, site.Town.Info);

        /// <summary>The exits where <paramref name="area"/>'s streets cross the square of half-side
        /// <paramref name="half"/> around its middle: the best-ranked street on each side becomes an exit (two at least;
        /// a side without a street gets one through the snow). Read from the data alone, so a streamed map can place
        /// them before the ground they stand on is built.</summary>
        internal static void DefineExits(Area area, float half, RaidMapInfo info)
        {
            float edge = half - 3f;
            var square = Rect.MinMaxRect(-edge, -edge, edge, edge);
            var best = new float[4];
            var at = new Vector2[4];
            var names = new string[4];
            for (int i = 0; i < 4; i++) best[i] = float.MinValue;
            foreach (Street s in area.Streets)
            {
                if (s.Rank <= 0 || s.Data.tunnel) continue;
                foreach (List<Vector2> piece in MapGeometry.ClipLine(s.Pts, square))
                {
                    for (int end = 0; end < 2; end++)
                    {
                        Vector2 p = end == 0 ? piece[0] : piece[piece.Count - 1];
                        int side = SideOf(p, edge);
                        if (side < 0) continue;
                        float score = s.Rank * 1000f - (side == 0 || side == 2 ? Mathf.Abs(p.x) : Mathf.Abs(p.y));
                        if (score <= best[side]) continue;
                        best[side] = score;
                        at[side] = p;
                        names[side] = string.IsNullOrEmpty(s.Data.name) ? KindName(s.Data.kind) : s.Data.name;
                    }
                }
            }
            int found = 0;
            for (int i = 0; i < 4; i++) if (best[i] > float.MinValue) found++;
            for (int side = 0; side < 4; side++)
            {
                if (best[side] == float.MinValue)
                {
                    if (found >= 2) continue;
                    found++;
                    at[side] = SideMiddle(side, edge);
                    names[side] = null;
                }
                info.Exits.Add(new RaidExit
                {
                    Name = names[side] != null ? names[side] + " (" + SideName(side) + ")" : "Snowfield (" + SideName(side) + ")",
                    Position = To3(at[side], 0f), Radius = 4.5f, ExtractSeconds = 8f, Side = side,
                    Hint = names[side] != null ? "Where " + names[side] + " leaves " + area.Place
                        : "Across the snow, " + SideName(side) + " edge of " + area.Place,
                });
            }
        }

        private static int SideOf(Vector2 p, float edge)
        {
            if (Mathf.Abs(p.y - edge) < 0.01f) return 0;
            if (Mathf.Abs(p.x - edge) < 0.01f) return 1;
            if (Mathf.Abs(p.y + edge) < 0.01f) return 2;
            if (Mathf.Abs(p.x + edge) < 0.01f) return 3;
            return -1;
        }

        private static Vector2 SideMiddle(int side, float edge)
        {
            switch (side)
            {
                case 0: return new Vector2(0f, edge);
                case 1: return new Vector2(edge, 0f);
                case 2: return new Vector2(0f, -edge);
                default: return new Vector2(-edge, 0f);
            }
        }

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

        /// <summary>The fixed map's spawn: the survivor starts on the sidewalk of the street nearest the place's centre,
        /// facing along it.</summary>
        internal static void PickSpawn(Site site)
        {
            PickSpawn(site.Area, site.Half, p => site.FreeBeside(p, 0.6f), out Vector2 spot, out float yaw);
            site.Town.Info.PlayerSpawn = To3(spot, 0.6f);
            site.Town.Info.PlayerSpawnYaw = yaw;
        }

        /// <summary>The sidewalk spot nearest the middle of <paramref name="area"/> (at least 5 m inside the square of
        /// half-side <paramref name="half"/>) that <paramref name="free"/> finds clear, facing along its street.</summary>
        internal static void PickSpawn(Area area, float half, System.Func<Vector2, bool> free, out Vector2 spot, out float yaw)
        {
            spot = Vector2.zero;
            yaw = 0f;
            float bestDist = float.MaxValue;
            foreach (Street s in area.Streets)
            {
                if (s.Rank < 2 || s.Data.tunnel) continue;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    Vector2 ab = b - a;
                    if (ab.sqrMagnitude < 0.01f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(-a, ab) / ab.sqrMagnitude);
                    Vector2 d = ab.normalized;
                    Vector2 p = a + ab * t + new Vector2(-d.y, d.x) * (s.Data.width * 0.5f + 1.2f);
                    float dist = p.magnitude;
                    if (dist >= bestDist || Mathf.Abs(p.x) > half - 5f || Mathf.Abs(p.y) > half - 5f || !free(p)) continue;
                    bestDist = dist;
                    spot = p;
                    yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                }
            }
        }
        #endregion

        #region Loot
        /// <summary>A few containers to suit the building (one per 45 m² of floor, one to five), standing clear of the
        /// walls, of each other and of the door; drawn from <paramref name="rng"/>, the building's own stream.</summary>
        internal static void Loot(Site site, Footprint f, System.Random rng, Vector2 doorPoint)
        {
            ContainerType[] kinds = ContainersFor(f.Data);
            int count = Mathf.Clamp(Mathf.RoundToInt(f.Size / 45f), 1, 5);
            var placed = new List<Vector2>(count);
            for (int i = 0; i < count && site.Containers < MaxContainers; i++)
            {
                for (int attempt = 0; attempt < 14; attempt++)
                {
                    var at = new Vector2(Mathf.Lerp(f.Bounds.xMin, f.Bounds.xMax, (float)rng.NextDouble()),
                        Mathf.Lerp(f.Bounds.yMin, f.Bounds.yMax, (float)rng.NextDouble()));
                    if (!MapGeometry.Contains(f.Pts, at) || MapGeometry.DistanceToEdge(f.Pts, at) < 0.9f
                        || Vector2.Distance(at, doorPoint) < 2f || Near(placed, at, 1.3f)) continue;
                    LootContainer.Spawn(site.Loot, kinds[rng.Next(kinds.Length)], To3(at, 0f), rng.Next());
                    placed.Add(at);
                    site.Containers++;
                    break;
                }
            }
        }

        /// <summary>The kinds of container a building holds, by what it's used for or else what it is.</summary>
        internal static ContainerType[] ContainersFor(MapBuilding b)
        {
            string use = b.use ?? "";
            if (use == "police") return new[] { ContainerType.GunLocker, ContainerType.Locker, ContainerType.Desk };
            if (b.kind == "hospital" || use == "hospital" || use == "clinic" || use == "doctors" || use == "pharmacy" || use == "dentist")
                return new[] { ContainerType.MedCabinet, ContainerType.Shelf, ContainerType.Desk };
            if (use == "fire_station") return new[] { ContainerType.Locker, ContainerType.ToolBench };
            if (use.StartsWith("shop:")) return new[] { ContainerType.Shelf, ContainerType.Shelf, ContainerType.Crate, ContainerType.Pantry };
            switch (b.kind)
            {
                case "house": case "trailer": case "apartments": case "hotel":
                    return new[] { ContainerType.Wardrobe, ContainerType.Pantry, ContainerType.Fridge, ContainerType.Desk };
                case "retail": case "commercial":
                    return new[] { ContainerType.Shelf, ContainerType.Crate, ContainerType.Pantry };
                case "office": case "civic": case "school": case "church":
                    return new[] { ContainerType.Desk, ContainerType.Locker, ContainerType.Shelf };
                default:
                    return new[] { ContainerType.Crate, ContainerType.ToolBench, ContainerType.Locker };
            }
        }
        #endregion

        #region Points of interest
        /// <summary>Gas pumps where a gas station is mapped, and a sign with the real name by the places people would
        /// look for (the nearest the middle first), each at the nearest free spot.</summary>
        internal static IEnumerable<string> BuildPoints(Site site)
        {
            Points(site);
            yield return "points";
        }

        private static void Points(Site site)
        {
            var rng = new System.Random(site.Town.Rng.Next());
            var spots = new List<Spot>(site.Area.Points);
            spots.Sort((p, q) => p.At.sqrMagnitude.CompareTo(q.At.sqrMagnitude));
            int signs = 0;
            foreach (Spot s in spots)
            {
                if (s.Data.kind == "fuel")
                {
                    for (int k = -1; k <= 1; k += 2)
                    {
                        if (!NearestFree(site, s.At + new Vector2(k * 2.2f, 0f), 1.2f, out Vector2 at)) continue;
                        Vector3 at3 = To3(at, 0f);
                        RaidMesh m = site.Town.Out.Solid(at3);
                        float pumpYaw = (float)(rng.NextDouble() * 10.0 - 5.0);
                        m.SetFrame(at3, pumpYaw);
                        DecorProps.GasPump(m, rng);
                        m.ResetFrame();
                        site.Town.PropCollider(at3 + Vector3.up * 0.9f, new Vector3(1.0f, 1.8f, 1.6f), pumpYaw);
                        site.Take(at, 1.2f);
                    }
                    continue;
                }
                if (signs >= MaxSigns || string.IsNullOrEmpty(s.Data.name) || !SignColours(s.Data.kind, out Color bg, out Color fg)) continue;
                // Hand-painted: the colours a little off.
                bg.r = Mathf.Clamp01(bg.r + (float)rng.NextDouble() * 0.04f - 0.02f);
                bg.g = Mathf.Clamp01(bg.g + (float)rng.NextDouble() * 0.04f - 0.02f);
                bg.b = Mathf.Clamp01(bg.b + (float)rng.NextDouble() * 0.04f - 0.02f);
                fg.r = Mathf.Clamp01(fg.r + (float)rng.NextDouble() * 0.04f - 0.02f);
                fg.g = Mathf.Clamp01(fg.g + (float)rng.NextDouble() * 0.04f - 0.02f);
                fg.b = Mathf.Clamp01(fg.b + (float)rng.NextDouble() * 0.04f - 0.02f);
                if (!NearestFree(site, s.At, 1.4f, out Vector2 spot)) continue;
                string text = s.Data.name.ToUpperInvariant();
                if (text.Length > 24) text = text.Substring(0, 24).TrimEnd();
                Vector2 toStreet = site.Area.NearestStreetDirection(spot, 40f);
                float yaw = toStreet == Vector2.zero ? 0f : Mathf.Atan2(toStreet.x, toStreet.y) * Mathf.Rad2Deg + MapStyle.Range(rng, -5f, 5f);
                Vector3 spot3 = To3(spot, 0f);
                RaidMesh sm = site.Town.Out.Solid(spot3);
                sm.SetFrame(spot3, yaw);   // the board faces +Z: towards the street
                DecorProps.PostSign(sm, rng, text, bg, fg, 0.42f, 1.3f);
                sm.ResetFrame();
                site.Town.PropCollider(spot3 + Vector3.up * 1.1f, new Vector3(2.4f, 2.2f, 0.3f), yaw);
                site.Take(spot, 1.4f);
                signs++;
            }
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
