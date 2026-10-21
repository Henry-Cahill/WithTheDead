using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The real-world map's props: street lights along the town's streets (most dead, a few still burning near the
    // centre), parked and wrecked cars along residential streets, trees in the mapped woods, parks and yards, gas pumps
    // at the mapped gas stations, and signs naming the mapped places (the hospital, the school, the post office...).
    public static partial class KentuckyMap
    {
        private const int MaxPoles = 90;
        private const int LitPoles = 10;
        private const int MaxCars = 70;
        private const int MaxTrees = 1200;
        private const int MaxSigns = 30;

        private static readonly Color[] CarPaints =
        {
            new Color(0.48f, 0.10f, 0.09f), new Color(0.18f, 0.26f, 0.42f), new Color(0.62f, 0.58f, 0.46f),
            new Color(0.30f, 0.22f, 0.14f), new Color(0.22f, 0.32f, 0.26f), new Color(0.55f, 0.55f, 0.56f),
            new Color(0.80f, 0.80f, 0.78f), new Color(0.12f, 0.12f, 0.13f),
        };

        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        private static bool TownStreet(string kind) => kind == "primary" || kind == "secondary" || kind == "tertiary"
            || kind == "residential" || kind == "unclassified" || kind == "living_street";

        // A pole every 34 m on alternate sides of the town's streets, its arm over the road. The ones nearest the
        // centre still burn sodium-orange; the rest went dark with the grid.
        private static void BuildStreetLights(Site site)
        {
            var poles = new List<(Vector2 at, float yaw)>();
            foreach (Street s in site.Area.Streets)
            {
                if (!TownStreet(s.Data.kind) || s.Data.tunnel || s.Data.bridge) continue;
                float side = (s.Data.id & 1) == 0 ? 1f : -1f, walked = 17f;
                for (int i = 0; i + 1 < s.Pts.Count; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.05f) continue;
                    Vector2 d = (b - a) / len, n = new Vector2(-d.y, d.x);
                    for (; walked <= len; walked += 34f, side = -side)
                    {
                        Vector2 at = a + d * walked + n * ((s.Data.width * 0.5f + 1.1f) * side);
                        if (!site.Free(at, 0.8f)) continue;
                        Vector2 toRoad = -n * side;
                        poles.Add((at, Mathf.Atan2(toRoad.x, toRoad.y) * Mathf.Rad2Deg));
                        site.Take(at, 0.8f);
                    }
                    walked -= len;
                }
            }
            poles.Sort((p, q) => p.at.sqrMagnitude.CompareTo(q.at.sqrMagnitude));
            var rng = new System.Random(site.Town.Rng.Next());
            int litCount = 0;
            for (int i = 0; i < poles.Count && i < MaxPoles; i++)
            {
                float lean = rng.NextDouble() < 0.05 ? R(rng, 8f, 14f) : 0f;
                bool lit = litCount < LitPoles && lean == 0f && rng.NextDouble() < 0.8;
                Vector3 at = To3(poles[i].at, 0f);
                RaidMesh m = site.Town.Out.Solid(at);
                m.SetFrame(at, poles[i].yaw);
                Vector3 lamp = m.ToMeshSpace(DecorProps.StreetLight(m, rng, lit, lean));
                m.ResetFrame();
                site.Town.PropCollider(at + Vector3.up * 2.75f, new Vector3(0.3f, 5.5f, 0.3f), poles[i].yaw);
                if (lit) site.Town.Light(site.Town.World, lamp, new Color(1f, 0.72f, 0.42f), 13f, 2.2f, rng.NextDouble() < 0.25, i < 3);
                if (lit) litCount++;
            }
        }

        // Cars left along the kerb of residential and service streets; now and then one stopped askew in the road.
        private static void BuildParkedCars(Site site)
        {
            var rng = new System.Random(site.Town.Rng.Next());
            var placed = new List<Vector2>();
            foreach (Street s in site.Area.Streets)
            {
                string k = s.Data.kind;
                bool service = k == "service";
                if (!service && k != "residential" && k != "unclassified" && k != "living_street" && k != "tertiary") continue;
                if (s.Data.tunnel || s.Data.bridge) continue;
                float walked = R(rng, 8f, 26f);
                for (int i = 0; i + 1 < s.Pts.Count && placed.Count < MaxCars; i++)
                {
                    Vector2 a = s.Pts[i], b = s.Pts[i + 1];
                    float len = Vector2.Distance(a, b);
                    if (len < 0.05f) continue;
                    Vector2 d = (b - a) / len, right = new Vector2(d.y, -d.x);
                    for (; walked <= len && placed.Count < MaxCars; walked += 18f)
                    {
                        if (rng.NextDouble() >= (service ? 0.15 : 0.22)) continue;
                        bool wreck = rng.NextDouble() < 0.12;
                        float off = wreck ? R(rng, -0.25f, 0.25f) * s.Data.width : s.Data.width * 0.5f - 1.05f;
                        Vector2 at = a + d * walked + right * off;
                        if (Mathf.Abs(at.x) > site.Half - 4f || Mathf.Abs(at.y) > site.Half - 4f) continue;
                        if (placed.Exists(q => Vector2.Distance(q, at) < 6f)) continue;
                        float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg + (wreck ? R(rng, -40f, 40f) : rng.NextDouble() < 0.15 ? 180f : 0f);
                        DecorProps.CarKind kind = rng.NextDouble() < 0.3 ? DecorProps.CarKind.Pickup
                            : rng.NextDouble() < 0.3 ? DecorProps.CarKind.Wagon : DecorProps.CarKind.Sedan;
                        Vector3 at3 = To3(at, 0f);
                        RaidMesh m = site.Town.Out.Solid(at3);
                        m.SetFrame(at3, yaw);
                        Color paint = CarPaints[rng.Next(CarPaints.Length)];
                paint.r = Mathf.Clamp01(paint.r + (float)rng.NextDouble() * 0.06f - 0.03f);
                paint.g = Mathf.Clamp01(paint.g + (float)rng.NextDouble() * 0.06f - 0.03f);
                paint.b = Mathf.Clamp01(paint.b + (float)rng.NextDouble() * 0.06f - 0.03f);
                DecorProps.Car(m, rng, kind, paint, out Vector3 size);
                        m.ResetFrame();
                        site.Town.PropCollider(at3 + Vector3.up * (size.y * 0.5f), size, yaw);
                        site.Take(at, 2.4f);
                        placed.Add(at);
                    }
                    walked -= len;
                }
            }
        }

        /// <summary>Trees per square metre for a kind of land, or 0 for none.</summary>
        private static float TreeDensity(string kind)
        {
            switch (kind)
            {
                case "forest": return 1f / 40f;
                case "orchard": return 1f / 30f;
                case "scrub": return 1f / 120f;
                case "wetland": return 1f / 150f;
                case "park": return 1f / 220f;
                case "cemetery": return 1f / 260f;
                case "golf": return 1f / 300f;
                case "residential": return 1f / 600f;
                case "meadow": case "grass": return 1f / 900f;
                default: return 0f;
            }
        }

        // Pines and bare hardwoods where the land is wooded, with colliders, clear of everything already standing.
        private static void BuildTrees(Site site)
        {
            var rng = new System.Random(site.Town.Rng.Next());
            float half = site.Half;
            var square = Rect.MinMaxRect(-half, -half, half, half);
            foreach (Patch p in site.Area.Areas)
            {
                float density = TreeDensity(p.Data.kind);
                if (density <= 0f || site.Trees >= MaxTrees) continue;
                List<Vector2> inside = MapGeometry.ClipPolygon(p.Pts, square);
                if (inside.Count < 3) continue;
                int want = Mathf.Min(Mathf.RoundToInt(Mathf.Abs(MapGeometry.SignedArea(inside)) * density), MaxTrees - site.Trees);
                float x0 = Min(inside, true), x1 = Max(inside, true), z0 = Min(inside, false), z1 = Max(inside, false);
                for (int planted = 0, tries = 0; planted < want && tries < want * 4; tries++)
                {
                    var at = new Vector2(R(rng, x0, x1), R(rng, z0, z1));
                    if (!MapGeometry.Contains(inside, at) || !site.Free(at, 1.3f)) continue;
                    bool pine = rng.NextDouble() < (p.Data.kind == "forest" ? 0.7 : 0.3);
                    float h = pine ? R(rng, 7f, 13f) : R(rng, 5f, 9f);
                    Vector3 at3 = To3(at, 0f);
                    RaidMesh m = site.Town.Out.Solid(at3);
                    m.SetFrame(at3, R(rng, 0f, 360f));
                    if (pine) DecorProps.Pine(m, rng, h);
                    else DecorProps.BareTree(m, rng, h);
                    m.ResetFrame();
                    site.Town.PropCollider(at3 + Vector3.up * 1.5f, new Vector3(0.45f, 3f, 0.45f), 0f);
                    site.Take(at, 1.3f);
                    site.Trees++;
                    planted++;
                }
            }
        }

        // Gas pumps where a gas station is mapped, and a sign with the real name by the places people would look for.
        private static void BuildPoints(Site site)
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
                // Slight random perturbation of sign background colour for hand‑painted variation
                bg.r = Mathf.Clamp01(bg.r + (float)rng.NextDouble() * 0.04f - 0.02f);
                bg.g = Mathf.Clamp01(bg.g + (float)rng.NextDouble() * 0.04f - 0.02f);
                bg.b = Mathf.Clamp01(bg.b + (float)rng.NextDouble() * 0.04f - 0.02f);
                fg.r = Mathf.Clamp01(fg.r + (float)rng.NextDouble() * 0.04f - 0.02f);
                fg.g = Mathf.Clamp01(fg.g + (float)rng.NextDouble() * 0.04f - 0.02f);
                fg.b = Mathf.Clamp01(fg.b + (float)rng.NextDouble() * 0.04f - 0.02f);
                if (!NearestFree(site, s.At, 1.4f, out Vector2 spot)) continue;
                string text = s.Data.name.ToUpperInvariant();
                if (text.Length > 24) text = text.Substring(0, 24).TrimEnd();
                Vector2 toStreet = NearestStreetDirection(site, spot, 40f);
                float yaw = toStreet == Vector2.zero ? 0f : Mathf.Atan2(toStreet.x, toStreet.y) * Mathf.Rad2Deg + R(rng, -5f, 5f);
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
    }
}
