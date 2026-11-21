using System.Collections.Generic;
using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The real-world map's trees: pines and bare hardwoods where the land is wooded (thickest in the forests, thinning
    /// out through orchards, scrub, parks and yards), each with a collider, clear of everything already standing.
    /// </summary>
    internal static class VegetationPass
    {
        private const int MaxTrees = 1200;

        internal static IEnumerable<string> Build(Site site)
        {
            Trees(site);
            yield return "trees";
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

        // As many trees as the land's density asks for, at random free spots in each area inside the square (four tries a
        // tree); seven in ten of a forest's are pines, three in ten elsewhere.
        private static void Trees(Site site)
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
                MapGeometry.Extent(inside, out float x0, out float z0, out float x1, out float z1);
                for (int planted = 0, tries = 0; planted < want && tries < want * 4; tries++)
                {
                    var at = new Vector2(MapStyle.Range(rng, x0, x1), MapStyle.Range(rng, z0, z1));
                    if (!MapGeometry.Contains(inside, at) || !site.Free(at, 1.3f)) continue;
                    bool pine = rng.NextDouble() < (p.Data.kind == "forest" ? 0.7 : 0.3);
                    float h = pine ? MapStyle.Range(rng, 7f, 13f) : MapStyle.Range(rng, 5f, 9f);
                    Vector3 at3 = To3(at, 0f);
                    RaidMesh m = site.Town.Out.Solid(at3);
                    m.SetFrame(at3, MapStyle.Range(rng, 0f, 360f));
                    if (pine) DecorProps.Pine(m, rng, h);
                    else DecorProps.BareTree(m, rng, h);
                    m.ResetFrame();
                    float s = 0.9f + (float)rng.NextDouble() * 0.2f;
                    site.Town.PropCollider(at3 + Vector3.up * 1.5f, new Vector3(0.45f, 3f, 0.45f) * s, 0f);
                    site.Take(at, 1.3f);
                    site.Trees++;
                    planted++;
                }
            }
        }
    }
}
