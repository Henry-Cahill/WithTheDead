using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Thin white lane markings on the roads the build selects: dashes down the middle (where no other centre line is
    /// painted: BuildStreets' DashedLine, BuildRoadMarkings) and a line just inside each edge. Along those the build
    /// also marks as lit, a row of lamp posts on the left-hand roadside, each with a warm amber light unless it's
    /// broken. (The Resources materials this was first written for never existed in the project, so it's drawn in
    /// palette colours.)
    /// </summary>
    internal static class RoadMarkingsBuilder
    {
        internal const float SideLineOffset = 0.25f; // metres in from the road edge
        private const float CentreDash = 0.8f; // length of a white centre dash (metres)
        private const float CentreGap = 0.4f; // gap between centre dashes (metres)
        // Street lamps
        private const float LampSpacing = 10f; // metres between lamp posts
        private const float LampRoadsideOffset = SideLineOffset + 0.5f; // metres out from the road edge
        private const float LampBaseSize = 0.20f; // width/depth of concrete base (metres)
        private const float LampBaseHeight = 0.10f; // height of base (metres)
        private const float LampPoleRadius = 0.05f; // half-width of pole (metres)
        private const float LampPoleHeight = 2.5f; // pole height (metres)
        private const float LampHeadSize = 0.30f; // width/depth of glass head (metres)
        private const float LampHeadHeight = 0.12f; // thickness of glass head (metres)
        /// <summary>Colour, intensity and range of the amber street-lamp light.</summary>
        internal const float LampLightIntensity = 2.5f; // warm amber intensity
        internal const float LampLightRange = 6f; // metres of visible light
        internal static readonly Color LampLightColor = new Color(1f, 0.75f, 0.5f); // period-correct amber

        private static Surf Paint => DecorPalette.Matte(new Color(0.90f, 0.90f, 0.87f));
        private static Surf LampPole => DecorPalette.Metal(new Color(0.35f, 0.36f, 0.37f));
        private static Surf LitHead => DecorPalette.Glow(LampLightColor);
        private static Surf DeadHead => DecorPalette.Glass(new Color(0.30f, 0.30f, 0.28f));

        /// <summary>
        /// Paints the lane markings on one piece of a road (its part inside the playable square), if the build selects
        /// the road for them. <paramref name="width"/> is the road's drawn width.
        /// </summary>
        internal static void BuildForStreet(KentuckyMap.Site site, KentuckyMap.Street street, List<Vector2> piece, float width, MapBuild build)
        {
            if (!build.HasRoadMarkings(street.Data.id)) return;
            float thickness = build.GetRoadMarkingThickness();
            // Wider roads already carry BuildStreets' faded centre line, and streets of two or more lanes get theirs from
            // BuildRoadMarkings: only the side lines there.
            bool centre = width < 7f && street.Data.lanes < 2;
            float edge = width * 0.5f - SideLineOffset;
            Surf paint = Paint;
            for (int i = 0; i + 1 < piece.Count; i++)
            {
                Vector2 a = piece[i], b = piece[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.05f) continue;
                Vector2 dir = (b - a) / len;
                // A frame at the segment's middle, on the asphalt, its +Z along the segment.
                Vector3 mid = KentuckyMap.To3((a + b) * 0.5f, KentuckyMap.StreetY + thickness * 0.5f);
                RaidMesh m = site.Town.Out.Flat(mid);
                m.SetFrame(mid, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg);
                if (centre)
                    for (float travelled = 0f; travelled < len; travelled += CentreDash + CentreGap)
                    {
                        float dash = Mathf.Min(CentreDash, len - travelled);
                        m.Box(new Vector3(0f, 0f, travelled + dash * 0.5f - len * 0.5f), new Vector3(0.12f, thickness, dash), paint);
                    }
                // side lines, just inside each edge
                m.Box(new Vector3(-edge, 0f, 0f), new Vector3(0.05f, thickness, len), paint);
                m.Box(new Vector3(edge, 0f, 0f), new Vector3(0.05f, thickness, len), paint);
                m.ResetFrame();
            }
        }

        /// <summary>
        /// The lamp posts along the left-hand roadside of one piece of a road the build selects for markings and marks
        /// as lit: one every 10 m where the roadside is free (<see cref="KentuckyMap.Site.FreeBeside"/>), so call it once
        /// the roads, the buildings and the other roadside props are down. About one in five is broken and dark.
        /// </summary>
        internal static void BuildStreetLamps(KentuckyMap.Site site, KentuckyMap.Street street, List<Vector2> piece, float width, MapBuild build)
        {
            if (!build.HasRoadMarkings(street.Data.id) || !build.HasStreetLamp(street.Data.id)) return;
            float top = LampBaseHeight + LampPoleHeight;
            float walked = LampSpacing * 0.5f;   // start half a spacing in from the start of the road
            for (int i = 0; i + 1 < piece.Count; i++)
            {
                Vector2 a = piece[i], b = piece[i + 1];
                float len = Vector2.Distance(a, b);
                if (len < 0.05f) continue;
                Vector2 d = (b - a) / len, left = new Vector2(-d.y, d.x);
                for (; walked <= len; walked += LampSpacing)
                {
                    // out from the road's left edge, with a small jitter for visual variance
                    float jitterX = (float)(build.Random.NextDouble() - 0.5) * 0.2f;
                    float jitterZ = (float)(build.Random.NextDouble() - 0.5) * 0.2f;
                    Vector2 at = a + d * walked + left * (width * 0.5f + LampRoadsideOffset) + new Vector2(jitterX, jitterZ);
                    if (!site.FreeBeside(at, 0.3f)) continue;
                    site.Take(at, 0.3f);
                    bool broken = build.Random.NextDouble() < 0.20;   // 20 % chance the lamp is broken (no light)

                    Vector3 at3 = KentuckyMap.To3(at, 0f);
                    RaidMesh m = site.Town.Out.Solid(at3);
                    m.SetFrame(at3, Mathf.Atan2(-left.x, -left.y) * Mathf.Rad2Deg);   // +Z towards the road
                    // concrete base, pole on top of it, glass head on top of the pole
                    m.Box(new Vector3(0f, LampBaseHeight * 0.5f, 0f), new Vector3(LampBaseSize, LampBaseHeight, LampBaseSize), DecorMaterials.Concrete);
                    m.Box(new Vector3(0f, LampBaseHeight + LampPoleHeight * 0.5f, 0f),
                        new Vector3(LampPoleRadius * 2f, LampPoleHeight, LampPoleRadius * 2f), LampPole);
                    m.Box(new Vector3(0f, top + LampHeadHeight * 0.5f, 0f), new Vector3(LampHeadSize, LampHeadHeight, LampHeadSize),
                        broken ? DeadHead : LitHead);
                    m.ResetFrame();
                    site.Town.PropCollider(at3 + Vector3.up * (top * 0.5f), new Vector3(LampBaseSize, top, LampBaseSize), 0f);
                    // The light at the underside of the head, where its bulb shows: a culled RoomLight, no shadows.
                    if (!broken)
                        site.Town.Light(site.Town.World, at3 + Vector3.up * top, LampLightColor, LampLightRange, LampLightIntensity,
                            flicker: false, shadows: false);
                }
                walked -= len;
            }
        }
    }
}