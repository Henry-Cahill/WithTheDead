using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;
using static CloneZ.StreetscapePass;

namespace CloneZ
{
    /// <summary>
    /// What stands out over a Kentucky town: its water tower on its legs with the town's name round the tank (or a tall
    /// standpipe, or a newer pedestal sphere), the radio masts striped red and white with their guys and blinking beacons,
    /// the flags in front of the courthouse, the post office, the schools and the fire and police stations (some at
    /// half-staff), the monuments and the green-and-gold historical markers. Water towers and masts are mapped (no town
    /// gets a made-up one) and drawn on Lite sites too, plainer; the flagpoles are mapped or put in front of the civic
    /// buildings that are mapped as such.
    /// </summary>
    internal static class Landmarks
    {
        private const int SaltTower = 0x7A7E, SaltMast = 0x3A57, SaltFlag = 0xF1A6, SaltMemorial = 0x3E30;
        private static readonly Color[] TankPaints =
        {
            new Color(0.86f, 0.87f, 0.86f), new Color(0.66f, 0.78f, 0.86f), new Color(0.72f, 0.74f, 0.76f), new Color(0.70f, 0.80f, 0.70f),
        };
        private static readonly Color AviationOrange = new Color(0.86f, 0.30f, 0.10f);
        private static readonly Color Granite = new Color(0.55f, 0.55f, 0.53f);
        private static readonly Color Bronze = new Color(0.33f, 0.46f, 0.40f);
        private static readonly Color Aluminium = new Color(0.78f, 0.79f, 0.80f);

        /// <summary>The landmarks of a full site: water towers, masts, flagpoles and memorials.</summary>
        internal static void Build(Ctx ctx)
        {
            var flags = new List<Vector2>();
            foreach (Node n in ctx.Site.Area.Nodes)
            {
                switch (n.Data.kind)
                {
                    case "water_tower": WaterTower(ctx, n); break;
                    case "mast": Mast(ctx, n); break;
                    case "flagpole":
                        if (ctx.Inside(n.At) && (ctx.Site.Free(n.At, 0.4f) || ctx.Site.FreeBeside(n.At, 0.4f)))
                        {
                            Flagpole(ctx, n.At, Wind(ctx, n.At), 10f, MapStyle.Unit(n.Data.id, SaltFlag) < 0.4f);
                            flags.Add(n.At);
                        }
                        break;
                    case "memorial": Memorial(ctx, n); break;
                }
            }
            CivicFlags(ctx, flags);
        }

        /// <summary>A Lite site's landmarks: water towers and masts, plainer.</summary>
        internal static void Lite(Ctx ctx)
        {
            foreach (Node n in ctx.Site.Area.Nodes)
            {
                if (n.Data.kind == "water_tower") WaterTower(ctx, n);
                else if (n.Data.kind == "mast") Mast(ctx, n);
            }
        }

        // The way the flags stream: the wind out of the north-west, give or take, the same over a whole town.
        private static Vector2 Wind(Ctx ctx, Vector2 at)
        {
            Vector2 w = at + ctx.Origin;
            long cell = ((long)Mathf.FloorToInt(w.x / 2000f) << 32) ^ (uint)Mathf.FloorToInt(w.y / 2000f);
            float a = (135f + 40f * (MapStyle.Unit(cell, SaltFlag + 1) - 0.5f)) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a));
        }

        #region Water towers
        // Only what stands below this goes into the walkable world (and its colliders); the rest is drawn overhead, so the
        // NavMesh never finds a floor on a tank's roof.
        private const float LowPart = 3f;

        private static void WaterTower(Ctx ctx, Node n)
        {
            if (!ctx.Within(n.At)) return;
            bool inside = ctx.Inside(n.At);
            if (inside && !ctx.Site.Free(n.At, 2f) && !ctx.Site.FreeBeside(n.At, 2f)) return;
            long id = n.Data.id;
            Color paint = TankPaints[MapStyle.Pick(id, SaltTower, TankPaints.Length)];
            Surf tank = Gloss(paint), dark = Gloss(paint * 0.82f);
            float style = MapStyle.Unit(id, SaltTower + 1);
            Vector3 foot = To3(n.At, 0f);
            RaidMesh low = ctx.Low(n.At), high = ctx.High(n.At);
            float yaw = n.Data.dir >= 0 ? n.Data.dir : MapStyle.Unit(id, SaltTower + 2) * 360f;
            low.SetFrame(foot, yaw);
            high.SetFrame(foot, yaw);
            float bandY, radius, top;
            if (style < 0.6f)
            {
                // The classic: a tank on six legs, braced, round a central riser, a catwalk at its waist.
                const int legs = 6;
                bandY = 31f;
                radius = 5.6f;
                float legTop = bandY - 1.5f;
                for (int i = 0; i < legs; i++)
                {
                    float a = i * Mathf.PI * 2f / legs;
                    var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    Vector3 f0 = dir * 6.8f, f1 = dir * 4.9f + new Vector3(0f, legTop, 0f);
                    Vector3 knee = Vector3.Lerp(f0, f1, LowPart / legTop);
                    low.Rod(f0, knee, 0.42f, dark, 6);
                    high.Rod(knee, f1, 0.42f, dark, 6);
                    low.Box(f0 + new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.4f, 0.9f), Concrete);
                    if (inside)
                        ctx.Site.Town.PropCollider(foot + Quaternion.Euler(0f, yaw, 0f) * f0 + new Vector3(0f, 1.5f, 0f),
                            new Vector3(0.6f, 3f, 0.6f), 0f);
                    if (!ctx.Full) continue;
                    float b = (i + 1) * Mathf.PI * 2f / legs;
                    var next = new Vector3(Mathf.Sin(b), 0f, Mathf.Cos(b));
                    for (int k = 0; k < 2; k++)
                    {
                        float y0 = 4f + k * 11f, y1 = y0 + 11f;
                        Vector3 p0 = Vector3.Lerp(dir * 6.8f, dir * 4.9f, y0 / legTop) + new Vector3(0f, y0, 0f);
                        Vector3 p1 = Vector3.Lerp(dir * 6.8f, dir * 4.9f, y1 / legTop) + new Vector3(0f, y1, 0f);
                        Vector3 q0 = Vector3.Lerp(next * 6.8f, next * 4.9f, y0 / legTop) + new Vector3(0f, y0, 0f);
                        Vector3 q1 = Vector3.Lerp(next * 6.8f, next * 4.9f, y1 / legTop) + new Vector3(0f, y1, 0f);
                        high.Rod(p0, q1, 0.08f, dark);
                        high.Rod(q0, p1, 0.08f, dark);
                        high.Rod(p1, q1, 0.18f, dark);
                    }
                }
                low.Cylinder(Vector3.zero, 0.9f, LowPart, 10, dark);
                high.Cylinder(new Vector3(0f, LowPart, 0f), 0.9f, bandY - 3f - LowPart, 10, dark);
                if (inside) ctx.Site.Town.PropCollider(foot + new Vector3(0f, 1.5f, 0f), new Vector3(1.8f, 3f, 1.8f), 0f);
                high.Ellipsoid(new Vector3(0f, bandY, 0f), new Vector3(radius * 2f, 7f, radius * 2f), tank, 16, 8);
                high.Cylinder(new Vector3(0f, bandY, 0f), radius, 4.2f, 16, tank);
                top = bandY + 4.2f;
                high.Cylinder(new Vector3(0f, top, 0f), radius + 0.05f, 2.6f, 16, SnowSurf, Quaternion.identity, 0.06f);
                if (ctx.Full)
                    for (int i = 0; i < 16; i++)
                    {
                        float a = i * Mathf.PI * 2f / 16f;
                        var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                        high.Box(dir * (radius + 0.45f) + new Vector3(0f, bandY - 0.05f, 0f), new Vector3(2.3f, 0.08f, 0.8f), Quaternion.LookRotation(dir), dark);
                        high.Box(dir * (radius + 0.82f) + new Vector3(0f, bandY + 1.0f, 0f), new Vector3(2.3f, 0.06f, 0.06f), Quaternion.LookRotation(dir), dark);
                    }
                top += 2.6f;
            }
            else if (style < 0.85f)
            {
                // A standpipe: one tall drum with a shallow roof.
                bandY = 22f;
                radius = 5.2f;
                low.Cylinder(Vector3.zero, radius, LowPart, 18, tank);
                high.Cylinder(new Vector3(0f, LowPart, 0f), radius, 30f - LowPart, 18, tank);
                if (inside) ctx.Site.Town.PropCollider(foot + new Vector3(0f, 1.5f, 0f), new Vector3(radius * 1.6f, 3f, radius * 1.6f), 0f);
                high.Cylinder(new Vector3(0f, 30f, 0f), radius + 0.05f, 1.6f, 18, SnowSurf, Quaternion.identity, 0.1f);
                top = 31.6f;
            }
            else
            {
                // A newer pedestal sphere: one fluted column flaring at its foot, a round tank on top.
                bandY = 33f;
                radius = 6.4f;
                low.Cylinder(Vector3.zero, 3.2f, LowPart, 12, dark, Quaternion.identity, 0.6f);
                high.Cylinder(new Vector3(0f, LowPart, 0f), 1.9f, 26f, 12, tank, Quaternion.identity, 0.9f);
                if (inside) ctx.Site.Town.PropCollider(foot + new Vector3(0f, 1.5f, 0f), new Vector3(5f, 3f, 5f), 0f);
                high.Ellipsoid(new Vector3(0f, bandY, 0f), new Vector3(radius * 2f, 10f, radius * 2f), tank, 16, 9);
                high.Ellipsoid(new Vector3(0f, bandY + 4.3f, 0f), new Vector3(radius * 1.2f, 1.6f, radius * 1.2f), SnowSurf, 12, 4);
                top = bandY + 5.2f;
            }
            high.Cylinder(new Vector3(0f, top, 0f), 0.06f, 1.2f, 6, dark);
            high.Ellipsoid(new Vector3(0f, top + 1.3f, 0f), new Vector3(0.45f, 0.45f, 0.45f), new Surf(Blinkers.Beacon, Vector2.zero, 0f), 8, 5);
            low.ResetFrame();
            high.ResetFrame();
            if (ctx.Full) TowerName(ctx, n, foot, bandY + (style < 0.6f ? 2.1f : 0f), radius, yaw);
            if (inside) ctx.Site.Take(n.At, 7.5f);
        }

        // The town's name painted round the tank in big letters, on two sides: the tower's own name when it's mapped with
        // one, else the nearest town's.
        private static void TowerName(Ctx ctx, Node n, Vector3 foot, float y, float radius, float yaw)
        {
            string name = TowerText(n.Data.name);
            if (string.IsNullOrEmpty(name)) name = TownNear(ctx, n.At);
            if (string.IsNullOrEmpty(name)) return;
            if (!ctx.Names.Lettering(name, SignBlack, out Rect uv, out float aspect)) return;
            float w = Mathf.Min(radius * 1.75f, 2.0f * aspect), h = w / aspect;
            RaidMesh m = ctx.High(new Vector2(foot.x, foot.z));
            // In flat panels following the tank's curve (a metre or so each), on two sides.
            int panels = Mathf.Max(3, Mathf.CeilToInt(w / 1.1f));
            float step = w / panels / radius * Mathf.Rad2Deg;
            for (int side = 0; side < 2; side++)
                for (int k = 0; k < panels; k++)
                {
                    Quaternion q = Quaternion.Euler(0f, yaw + side * 180f + ((panels - 1) * 0.5f - k) * step, 0f);
                    Vector3 nrm = q * Vector3.forward;
                    // Panel k (left to right as seen from outside) shows its share of the name.
                    var part = new Rect(uv.xMin + uv.width * k / panels, uv.yMin, uv.width / panels, uv.height);
                    m.Upright(foot + nrm * (radius + 0.06f) + new Vector3(0f, y, 0f), nrm, w / panels * 1.02f, h, ctx.Names.Surf, part);
                }
        }

        private static string s_PlacesFolder;
        private static MapPlace[] s_Places;

        // The nearest town (300 people or more) within 4 km of a spot, from the build's places: the start square knows its
        // own; a streamed piece looks it up, the same whenever it's built.
        private static string TownNear(Ctx ctx, Vector2 local)
        {
            Site site = ctx.Site;
            if (!site.Chunked) return site.Area.Place;
            try
            {
                MapPlace start = site.Build.FindPlace(site.Area.Place);
                if (start == null) return null;
                if (s_PlacesFolder != site.Build.Folder)
                {
                    string path = System.IO.Path.Combine(site.Build.Folder, "places.json");
                    MapPlaces file = System.IO.File.Exists(path) ? JsonUtility.FromJson<MapPlaces>(System.IO.File.ReadAllText(path)) : null;
                    s_Places = file != null && file.places != null ? file.places : new MapPlace[0];
                    s_PlacesFolder = site.Build.Folder;
                }
                double gx = start.x + site.WorldOffset.x + local.x, gz = start.z + site.WorldOffset.z + local.y;
                MapPlace best = null;
                double bestD = 4000.0 * 4000.0;
                foreach (MapPlace p in s_Places)
                {
                    if (MapGameplay.Population(p) < 300) continue;
                    double dx = p.x - gx, dz = p.z - gz, d = dx * dx + dz * dz;
                    if (d < bestD) { bestD = d; best = p; }
                }
                return best != null ? best.name : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static string TowerText(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string t = name;
            foreach (string w in new[] { "Water Tower", "Water Tank", "Elevated Tank", "Standpipe", "Storage Tank", "Tank", "Tower" })
            {
                int i = t.IndexOf(w, System.StringComparison.OrdinalIgnoreCase);
                if (i >= 0) t = t.Remove(i, w.Length);
            }
            t = t.Trim(' ', '-', ',');
            return t.Length >= 3 && t.Length <= 18 ? t : null;
        }
        #endregion

        #region Masts
        private static void Mast(Ctx ctx, Node n)
        {
            if (!ctx.Within(n.At)) return;
            bool inside = ctx.Inside(n.At);
            if (inside && !ctx.Site.Free(n.At, 1f) && !ctx.Site.FreeBeside(n.At, 1f)) return;
            string name = n.Data.name ?? "";
            long id = n.Data.id;
            bool cell = Has(name, "verizon", "at&t", "cingular", "sprint", "t-mobile", "cellular", "wireless", "alltel", "cell");
            bool broadcast = !cell && (Has(name, "radio", "broadcast", " tv", "-tv", "-fm", "-am", " fm", " am") || name.StartsWith("W", System.StringComparison.Ordinal));
            float h = cell ? 42f + 12f * MapStyle.Unit(id, SaltMast) : broadcast ? 95f + 60f * MapStyle.Unit(id, SaltMast) : 48f + 30f * MapStyle.Unit(id, SaltMast);
            Vector3 foot = To3(n.At, 0f);
            RaidMesh low = ctx.Low(n.At), high = ctx.High(n.At);
            Surf orange = Gloss(AviationOrange), white = Gloss(new Color(0.88f, 0.88f, 0.86f));
            Surf beacon = new Surf(Blinkers.Beacon, Vector2.zero, 0f);
            if (cell)
            {
                // A monopole, tapering; only its foot in the walkable world.
                float r3 = 0.75f * (1f - 0.5f * LowPart / h);
                low.Cylinder(foot, 0.75f, LowPart, 10, Galvanised, Quaternion.identity, r3 / 0.75f);
                high.Cylinder(foot + new Vector3(0f, LowPart, 0f), r3, h - LowPart, 10, Galvanised, Quaternion.identity, 0.375f / r3);
                for (int i = 0; i < 3; i++)
                {
                    Quaternion q = Quaternion.Euler(0f, i * 120f, 0f);
                    Vector3 d = q * Vector3.forward;
                    high.Box(foot + d * 1.2f + new Vector3(0f, h - 1.2f, 0f), new Vector3(2.6f, 0.15f, 0.15f), q, Galvanised);
                    for (int k = -1; k <= 1; k++)
                        high.Box(foot + d * 1.25f + q * new Vector3(k * 0.9f, h - 1.2f, 0f), new Vector3(0.3f, 1.4f, 0.12f), q, white);
                }
                high.Ellipsoid(foot + new Vector3(0f, h + 0.3f, 0f), new Vector3(0.4f, 0.4f, 0.4f), beacon, 8, 5);
                if (inside) ctx.Site.Town.PropCollider(foot + new Vector3(0f, 1.5f, 0f), new Vector3(1.5f, 3f, 1.5f), 0f);
                if (inside) ctx.Site.Take(n.At, 1.5f);
                return;
            }
            // A guyed lattice mast: three legs banded orange and white, zigzag bracing on each face.
            const float face = 0.9f;
            float r = face / Mathf.Sqrt(3f);
            float yaw = MapStyle.Unit(id, SaltMast + 1) * 120f;
            var legs = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                float a = (yaw + i * 120f) * Mathf.Deg2Rad;
                legs[i] = new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
            }
            const int bands = 7;
            for (int b = 0; b < bands; b++)
            {
                float y0 = h * b / bands, y1 = h * (b + 1) / bands;
                Surf s = b % 2 == 0 ? orange : white;
                for (int i = 0; i < 3; i++)
                {
                    if (b == 0) low.Rod(foot + legs[i], foot + legs[i] + new Vector3(0f, LowPart, 0f), 0.07f, s);
                    high.Rod(foot + legs[i] + new Vector3(0f, Mathf.Max(y0, LowPart), 0f), foot + legs[i] + new Vector3(0f, y1, 0f), 0.07f, s);
                }
            }
            if (ctx.Full)
            {
                for (float y = 0f; y + 3f <= h; y += 3f)
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 a = foot + legs[i] + new Vector3(0f, y, 0f), b = foot + legs[(i + 1) % 3] + new Vector3(0f, y + 3f, 0f);
                        high.Rod(a, b, 0.03f, (int)(y / h * bands) % 2 == 0 ? orange : white);
                    }
                // The guys: three anchors, three levels each.
                for (int i = 0; i < 3; i++)
                {
                    float a = (yaw + 60f + i * 120f) * Mathf.Deg2Rad;
                    var d = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                    Vector2 anchor = n.At + d * (h * 0.55f);
                    for (int k = 1; k <= 3; k++)
                        ctx.Wires.Add(foot + new Vector3(0f, h * k * 0.3f, 0f), To3(anchor, 0.1f), 0.2f + h * 0.004f, 0.012f,
                            new Color(0.45f, 0.46f, 0.47f), !ctx.Inside(anchor));
                    if (ctx.Within(anchor)) ctx.Low(anchor).Box(To3(anchor, 0.15f), new Vector3(1.2f, 0.3f, 1.2f), Concrete);
                }
            }
            high.Ellipsoid(foot + new Vector3(0f, h + 0.3f, 0f), new Vector3(0.45f, 0.45f, 0.45f), beacon, 8, 5);
            high.Ellipsoid(foot + new Vector3(0f, h * 0.5f, 0f) + legs[0] * 1.6f, new Vector3(0.35f, 0.35f, 0.35f), beacon, 8, 5);
            low.Box(foot + new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.5f, 1.6f), Concrete);
            if (inside)
            {
                ctx.Site.Town.PropCollider(foot + new Vector3(0f, 1.5f, 0f), new Vector3(1.6f, 3f, 1.6f), 0f);
                ctx.Site.Take(n.At, 1.5f);
            }
        }

        private static bool Has(string s, params string[] words)
        {
            foreach (string w in words)
                if (s.IndexOf(w, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        #endregion

        #region Flags
        // A flagpole in front of the civic buildings mapped as such, between the building and its road.
        private static void CivicFlags(Ctx ctx, List<Vector2> flags)
        {
            foreach (Spot p in ctx.Site.Area.Points)
            {
                string k = p.Data.kind;
                bool big = k == "courthouse" || k == "school" || k == "townhall";
                if (!big && k != "post_office" && k != "police" && k != "fire_station" && k != "library") continue;
                if (!ctx.Inside(p.At)) continue;
                bool near = false;
                foreach (Vector2 f in flags) if ((f - p.At).sqrMagnitude < 60f * 60f) near = true;
                if (near) continue;
                Street s = NearestRoad(ctx, p.At, 70f, out Vector2 on, out _, 2);
                if (s == null) continue;
                Footprint b = null;
                foreach (Footprint f in ctx.Site.Area.Buildings)
                    if (f.Bounds.Contains(p.At) && MapGeometry.Contains(f.Pts, p.At)) { b = f; break; }
                Vector2 from = b != null ? b.Middle : p.At;
                Vector2 toRoad = (on - from).normalized;
                // Out from the building's front wall, a few metres short of the road.
                float wall = b != null ? Mathf.Max(b.Bounds.width, b.Bounds.height) * 0.35f : 0f;
                float room = Vector2.Distance(from, on) - wall - HalfWidth(s);
                if (room < 3f) continue;
                Vector2 at = from + toRoad * (wall + Mathf.Min(room * 0.5f, 5f));
                bool ok = false;
                for (int t = 0; t < 5 && !ok; t++)
                {
                    Vector2 q = at + Left(toRoad) * ((t + 1) / 2 * 2.5f * (t % 2 == 0 ? 1f : -1f));
                    if (ctx.Inside(q) && (ctx.Site.Free(q, 0.5f) || ctx.Site.FreeBeside(q, 0.5f)) && !InBuilding(ctx, q)) { at = q; ok = true; }
                }
                if (!ok) continue;
                flags.Add(at);
                Flagpole(ctx, at, Wind(ctx, at), big ? 12f : 10f, MapStyle.Unit(p.Data.id, SaltFlag) < 0.4f);
            }
        }

        private static bool InBuilding(Ctx ctx, Vector2 p)
        {
            foreach (Footprint f in ctx.Site.Area.Buildings)
                if (f.Bounds.Contains(p) && MapGeometry.Contains(f.Pts, p)) return true;
            return false;
        }

        // An aluminium flagpole on its base with a gold ball, the flag streaming downwind (at half-staff for the dead).
        private static void Flagpole(Ctx ctx, Vector2 at, Vector2 wind, float h, bool halfStaff)
        {
            Vector3 foot = To3(at, 0f);
            RaidMesh low = ctx.Low(at), high = ctx.High(at);
            low.Cylinder(foot, 0.45f, 0.22f, 10, Concrete);
            low.Cylinder(foot + new Vector3(0f, 0.22f, 0f), 0.42f, 0.03f, 10, SnowSurf);
            low.Cylinder(foot + new Vector3(0f, 0.22f, 0f), 0.08f, h, 8, Metal(Aluminium), Quaternion.identity, 0.5f);
            high.Ellipsoid(foot + new Vector3(0f, h + 0.3f, 0f), new Vector3(0.16f, 0.16f, 0.16f), Gloss(new Color(0.85f, 0.68f, 0.25f)), 8, 5);
            float fw = h >= 12f ? 1.8f : 1.5f, fh = fw * 0.62f;
            float top = halfStaff ? h * 0.58f : h - 0.1f;
            const float droop = 30f;
            Vector3 down = Vector3.down;
            Vector3 fly = (To3(wind, 0f) * Mathf.Cos(droop * Mathf.Deg2Rad) + down * Mathf.Sin(droop * Mathf.Deg2Rad)) * fw;
            Vector3 hoist = foot + new Vector3(0f, top, 0f) + To3(wind, 0f) * 0.09f;
            Vector3 centre = hoist + fly * 0.5f + down * (fh * 0.5f);
            Rect uv = SignArt.Cell("flag");
            high.QuadUV(centre, fly * 0.5f, -down * (fh * 0.5f), SignArt.Surf, uv);
            high.QuadUV(centre, -fly * 0.5f, -down * (fh * 0.5f), SignArt.Surf, new Rect(uv.xMax, uv.yMin, -uv.width, uv.height));
            ctx.Wires.Add(foot + new Vector3(0f, 1.2f, 0f) + To3(wind, 0f) * 0.1f, foot + new Vector3(0f, h, 0f) + To3(wind, 0f) * 0.1f,
                0.05f, 0.006f, new Color(0.85f, 0.85f, 0.82f), !ctx.Inside(at));
            ctx.Collider(at, 0f, new Vector3(0.9f, 1f, 0.9f), 0f);
            ctx.Site.Take(at, 0.5f);
        }
        #endregion

        #region Memorials
        private static void Memorial(Ctx ctx, Node n)
        {
            if (!ctx.Inside(n.At)) return;
            string name = n.Data.name ?? "";
            if (Has(name, "wall", "bench", "tree", "garden")) return;
            bool marker = Has(name, "marker", "historic");
            float r = marker ? 0.4f : 1.4f;
            if (!ctx.Site.Free(n.At, r) && !ctx.Site.FreeBeside(n.At, r)) return;
            Street s = NearestRoad(ctx, n.At, 40f, out Vector2 on, out _);
            Vector2 facing = s != null && (on - n.At).sqrMagnitude > 0.01f ? (on - n.At).normalized : Vector2.up;
            if (n.Data.dir >= 0)
            {
                float a = n.Data.dir * Mathf.Deg2Rad;
                facing = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            }
            Vector3 foot = To3(n.At, 0f);
            RaidMesh m = ctx.Low(n.At);
            if (marker)
            {
                // A Kentucky historical highway marker: green and gold on a black post, the same both sides.
                m.Box(foot + new Vector3(0f, 1.0f, 0f), new Vector3(0.1f, 2.0f, 0.1f), Matte(new Color(0.05f, 0.05f, 0.05f)));
                Vector3 c = foot + new Vector3(0f, 2.15f, 0f);
                float yaw = Yaw(facing);
                m.Box(c, new Vector3(1.05f, 0.8f, 0.04f), yaw, Gloss(new Color(0.06f, 0.24f, 0.14f)));
                Vector3 nrm = To3(facing, 0f);
                m.Upright(c + nrm * 0.021f, nrm, 1.0f, 0.75f, SignArt.Surf, SignArt.Cell("marker"));
                m.Upright(c - nrm * 0.021f, -nrm, 1.0f, 0.75f, SignArt.Surf, SignArt.Cell("marker"));
                m.Box(c + new Vector3(0f, 0.42f, 0f), new Vector3(1.0f, 0.03f, 0.05f), yaw, SnowSurf);
                ctx.Collider(n.At, 0f, new Vector3(0.12f, 2f, 0.12f), 0f);
                ctx.Site.Take(n.At, 0.4f);
                return;
            }
            Surf stone = Matte(Granite);
            m.SetFrame(foot, Yaw(facing));
            m.Box(new Vector3(0f, 0.2f, 0f), new Vector3(2.2f, 0.4f, 2.2f), stone);
            m.Box(new Vector3(0f, 0.415f, 0f), new Vector3(2.1f, 0.03f, 2.1f), SnowSurf);
            m.Box(new Vector3(0f, 1.1f, 0f), new Vector3(1.3f, 1.4f, 1.3f), stone);
            m.Box(new Vector3(0f, 1.815f, 0f), new Vector3(1.25f, 0.03f, 1.25f), SnowSurf);
            if (Has(name, "statue", "soldier", "confederate", "union", "veteran"))
            {
                // A soldier in bronze on a tall die.
                Surf bronze = Gloss(Bronze);
                m.Box(new Vector3(0f, 2.5f, 0f), new Vector3(0.95f, 1.4f, 0.95f), stone);
                float y = 3.2f;
                m.Box(new Vector3(-0.12f, y + 0.45f, 0f), new Vector3(0.16f, 0.9f, 0.2f), bronze);
                m.Box(new Vector3(0.12f, y + 0.45f, 0f), new Vector3(0.16f, 0.9f, 0.2f), bronze);
                m.Box(new Vector3(0f, y + 1.25f, 0f), new Vector3(0.48f, 0.75f, 0.28f), bronze);
                m.Ellipsoid(new Vector3(0f, y + 1.8f, 0f), new Vector3(0.24f, 0.28f, 0.24f), bronze, 8, 5);
                m.Box(new Vector3(0f, y + 1.98f, 0f), new Vector3(0.3f, 0.08f, 0.3f), bronze);
                m.Box(new Vector3(0.3f, y + 1.1f, 0.05f), new Vector3(0.12f, 0.7f, 0.12f), bronze);
                m.Box(new Vector3(-0.32f, y + 1.3f, 0.12f), new Vector3(0.05f, 1.3f, 0.05f), bronze);
                m.Box(new Vector3(0f, y + 2.03f, 0f), new Vector3(0.26f, 0.04f, 0.26f), SnowSurf);
            }
            else
            {
                // An obelisk.
                Quaternion square = Quaternion.Euler(0f, 45f, 0f);
                m.Cylinder(new Vector3(0f, 1.8f, 0f), 0.52f, 5.2f, 4, stone, square, 0.55f);
                m.Cylinder(new Vector3(0f, 7.0f, 0f), 0.29f, 0.6f, 4, stone, square, 0.02f);
            }
            m.ResetFrame();
            ctx.Block(n.At, new Vector3(2.2f, 2f, 2.2f), Yaw(facing));
            ctx.Site.Take(n.At, 1.4f);
        }
        #endregion
    }
}
