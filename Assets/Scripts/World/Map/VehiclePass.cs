using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's cars: left parked at the kerb of the residential streets, service roads and tertiary roads,
    /// a few of them wrecks slewed across the road, in the faded paints of the late eighties. They're built in
    /// <see cref="StreetscapePass"/>'s turn (<see cref="ParkedCars"/>), after the roadside snow piles and before the
    /// trees, where they've always claimed their spots; this pass's own turn has nothing left to do.
    /// </summary>
    internal static class VehiclePass
    {
        private const int MaxCars = 70;

        private static readonly Color[] CarPaints =
        {
            new Color(0.48f, 0.10f, 0.09f), new Color(0.18f, 0.26f, 0.42f), new Color(0.62f, 0.58f, 0.46f),
            new Color(0.30f, 0.22f, 0.14f), new Color(0.22f, 0.32f, 0.26f), new Color(0.55f, 0.55f, 0.56f),
            new Color(0.80f, 0.80f, 0.78f), new Color(0.12f, 0.12f, 0.13f),
        };

        internal static IEnumerable<string> Build(Site site)
        {
            yield break;
        }

        /// <summary>A chance of a car every 18 m along the right-hand kerb, at least 6 m from the next one and 4 m in from
        /// the square's edge; bridges and tunnels have none. Each takes its spot, but doesn't wait for a free one: the
        /// cars are parked where they were left.</summary>
        internal static void ParkedCars(Site site)
        {
            var rng = new System.Random(site.Town.Rng.Next());
            var placed = new List<Vector2>();
            foreach (Street s in site.Area.Streets)
            {
                string k = s.Data.kind;
                bool service = k == "service";
                if (!service && k != "residential" && k != "unclassified" && k != "living_street" && k != "tertiary") continue;
                if (s.Data.tunnel || s.Data.bridge) continue;
                float walked = MapStyle.Range(rng, 8f, 26f);
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
                        float off = wreck ? MapStyle.Range(rng, -0.25f, 0.25f) * s.Data.width : s.Data.width * 0.5f - 1.05f;
                        Vector2 at = a + d * walked + right * off;
                        if (Mathf.Abs(at.x) > site.Half - 4f || Mathf.Abs(at.y) > site.Half - 4f || Near(placed, at, 6f)) continue;
                        float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg
                            + (wreck ? MapStyle.Range(rng, -40f, 40f) : rng.NextDouble() < 0.15 ? 180f : 0f);
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

        private static bool Near(List<Vector2> spots, Vector2 p, float within)
        {
            for (int i = 0; i < spots.Count; i++)
                if (Vector2.Distance(spots[i], p) < within) return true;
            return false;
        }
    }
}
