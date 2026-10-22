using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    public static partial class RaidMapBuilder
    {
        private static readonly Color Kerb = new Color(0.62f, 0.62f, 0.60f);
        private static readonly Color LineYellow = new Color(0.62f, 0.52f, 0.22f);
        private static readonly Color LineWhite = new Color(0.72f, 0.72f, 0.70f);
        private static readonly Color PackedSnow = new Color(0.50f, 0.53f, 0.58f);
        private static readonly Color WetTrack = new Color(0.11f, 0.11f, 0.12f);
        private static readonly Color FenceWood = new Color(0.45f, 0.30f, 0.20f);

        private struct RoadSpec
        {
            public bool AlongX;
            public float Coord, From, To, Width;
        }

        private static readonly List<RoadSpec> s_Roads = new List<RoadSpec>();

        private static void BuildStreets(RaidTown town)
        {
            s_Roads.Clear();
            // Main Street (north–south) and Depot Road (east–west) cross at the centre of town.
            Road(town, false, 0f, -GroundHalf, GroundHalf, 8f, -6.5f, 6.5f, true);
            Road(town, true, 0f, -GroundHalf, GroundHalf, 8f, -6.5f, 6.5f, false);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Paved(town, Rect.MinMaxRect(sx > 0 ? 4f : -6.5f, sz > 0 ? 4f : -6.5f, sx > 0 ? 6.5f : -4f, sz > 0 ? 6.5f : -4f), 0.025f, DecorMaterials.Concrete);

            // The crossing: faded zebra stripes on all four approaches.
            for (int arm = 0; arm < 4; arm++)
            {
                for (int i = 0; i < 6; i++)
                {
                    float t = -3.2f + i * 1.28f;
                    Vector3 p;
                    float w, d;
                    switch (arm)
                    {
                        case 0: p = new Vector3(t, 0.02f, 5.4f); w = 0.6f; d = 1.8f; break;
                        case 1: p = new Vector3(t, 0.02f, -5.4f); w = 0.6f; d = 1.8f; break;
                        case 2: p = new Vector3(5.4f, 0.02f, t); w = 1.8f; d = 0.6f; break;
                        default: p = new Vector3(-5.4f, 0.02f, t); w = 1.8f; d = 0.6f; break;
                    }
                    if (town.Rng.NextDouble() < 0.25) continue;
                    town.Out.Flat(p).Flat(p, w, d, 0f, DecorPalette.Matte(LineWhite * 0.9f));
                }
            }
        }

        /// <summary>An axis-aligned street: asphalt, a faded double centre line, kerbs, sidewalks, banks of
        /// plowed snow and tyre tracks. Kerbs, sidewalks and lines skip the crossing between
        /// <paramref name="skipA"/> and <paramref name="skipB"/>; the asphalt skips it too unless
        /// <paramref name="ownsCrossing"/>.</summary>
        private static void Road(RaidTown town, bool alongX, float coord, float from, float to, float width,
            float skipA, float skipB, bool ownsCrossing)
        {
            s_Roads.Add(new RoadSpec { AlongX = alongX, Coord = coord, From = from, To = to, Width = width });
            System.Random rng = town.Rng;
            float hw = width * 0.5f;
            Surf asphalt = DecorMaterials.Asphalt;
            Surf concrete = DecorMaterials.Concrete;

            for (float a = from; a < to - 0.01f; a += 10f)
            {
                float b = Mathf.Min(to, a + 10f);
                if (!ownsCrossing && b > -hw && a < hw)
                {
                    // Draw full‑width asphalt up to the crossing start
                    if (a < -hw) Slab(town, alongX, coord, a, -hw, -hw, hw, 0.012f, asphalt);
                    // Draw full‑width asphalt after the crossing end
                    if (b > hw) Slab(town, alongX, coord, hw, b, -hw, hw, 0.012f, asphalt);
                    // Split the crossing segment across the road centre
                    float crossA = Mathf.Max(a, -hw);
                    float crossB = Mathf.Min(b, hw);
                    if (crossB > crossA)
                    {
                        Slab(town, alongX, coord, crossA, crossB, -hw, 0f, 0.012f, asphalt);
                        Slab(town, alongX, coord, crossA, crossB, 0f, hw, 0.012f, asphalt);
                    }
                    continue;
                }
                Slab(town, alongX, coord, a, b, -hw, hw, 0.012f, asphalt);
                // Occasionally add a dark oil‑spill patch (~5 % chance per segment)
                if (rng.NextDouble() < 0.05)
                {
                    float spillSize = R(rng, 0.2f, 0.4f); // radius ≈ 0.2–0.4 m
                    float spillPos = R(rng, a, b);
                    Vector3 center = alongX ? new Vector3(spillPos, 0.015f, coord) : new Vector3(coord, 0.015f, spillPos);
                    // dark matte surface to look like a thin oil film on the road
                    town.Out.Flat(center).FlatBlob(center, spillSize, spillSize, R(rng, 0f, 360f), 0.015f, rng, DecorPalette.Matte(new Color(0.05f, 0.05f, 0.07f)));
                }
                // Occasionally draw faint edge‑line markings on each side of the lane (25 % chance)
                if (rng.NextDouble() < 0.25)
                {
                    float edge = hw - 0.06f; // 6 cm inset from the kerb
                    Strip(town, alongX, coord - edge, a, b, 0.04f, 0.006f,
                        DecorPalette.Matte(LineWhite * 0.7f));
                    Strip(town, alongX, coord + edge, a, b, 0.04f, 0.006f,
                        DecorPalette.Matte(LineWhite * 0.7f));
                }
// Add subtle road‑crack details with a 30 % chance (1‑3 short dark strips)
                    if (rng.NextDouble() < 0.3)
                    {
                        int crackCount = rng.Next(1, 4); // 1 to 3 cracks
                        for (int ci = 0; ci < crackCount; ci++)
                        {
                            float cStart = R(rng, a, b - 0.5f);
                            float cLen = R(rng, 0.4f, 1.2f);
                            float off = R(rng, -hw + 0.2f, hw - 0.2f);
                            float across = coord + off;
                            Strip(town, alongX, across, cStart, Mathf.Min(b, cStart + cLen), 0.02f, 0.001f,
                                DecorPalette.Matte(new Color(0.15f, 0.15f, 0.15f)));
            }

                    }
                    // Add occasional potholes with a 15 % chance per segment
                    if (rng.NextDouble() < 0.15)
                    {
                        float pLen = R(rng, 0.5f, 1.5f);
                        float pStart = R(rng, a, b - pLen);
                        float off = R(rng, -hw + 0.3f, hw - 0.3f);
                        float across = coord + off;
                        // Darker strip to simulate a pothole depression
                        Strip(town, alongX, across, pStart, pStart + pLen, 0.12f, 0.001f,
                            DecorPalette.Matte(new Color(0.1f, 0.1f, 0.1f)));
                    }
            }

            for (int side = -1; side <= 1; side += 2)
            {
                for (float a = from; a < to - 0.01f; a += 10f)
                {
                    float b = Mathf.Min(to, a + 10f);
                    if (b > skipA && a < skipB)
                    {
                        if (a < skipA) SidewalkRun(town, alongX, coord, side, hw, a, skipA, concrete, rng);
                        if (b > skipB) SidewalkRun(town, alongX, coord, side, hw, skipB, b, concrete, rng);
                        continue;
                    }
                    SidewalkRun(town, alongX, coord, side, hw, a, b, concrete, rng);
                }
            }

            // Faded double yellow centre line, broken up by wear.
            for (float a = from + 1f; a < to - 2f; a += 4.5f)
            {
                if (a + 3f > skipA - 2f && a < skipB + 2f) continue;
                if (rng.NextDouble() < 0.3) continue;
                float len = R(rng, 1.5f, 3f);
                Surf yellow = DecorPalette.Matte(LineYellow * R(rng, 0.8f, 1.05f));
                for (int k = -1; k <= 1; k += 2)
                    Strip(town, alongX, coord + k * 0.14f, a, a + len, 0.1f, 0.016f, yellow);
            }

            // Tyre tracks down each lane: packed pale ruts or wet dark ones, wobbling a little.
            for (int lane = -1; lane <= 1; lane += 2)
            {
                float laneC = coord + lane * hw * 0.5f;
                Color c = rng.NextDouble() < 0.5 ? PackedSnow : WetTrack;
                for (float a = from + 0.5f; a < to - 1f; a += R(rng, 3f, 7f))
                {
                    float len = R(rng, 2f, 6f);
                    float wob = R(rng, -0.25f, 0.25f);
                    Surf track = DecorPalette.Matte(c * R(rng, 0.9f, 1.1f));
                    for (int k = -1; k <= 1; k += 2)
                        Strip(town, alongX, laneC + wob + k * 0.78f, a, Mathf.Min(to - 0.5f, a + len), 0.24f, 0.015f, track);
                }
            }
        }

        private static void SidewalkRun(RaidTown town, bool alongX, float coord, int side, float hw, float a, float b, in Surf concrete, System.Random rng)
        {
            Slab(town, alongX, coord, a, b, side > 0 ? hw : -hw - 0.25f, side > 0 ? hw + 0.25f : -hw, 0.05f, DecorPalette.Matte(Kerb));
            // ----- Snow‑fence along the road‑side of the sidewalk -----
            float fenceAcross = coord + (side > 0 ? hw + 0.12f : -hw - 0.12f);
            for (float fp = a + 0.4f; fp < b - 0.4f; fp += R(rng, 0.7f, 1.0f))
            {
                // vertical post (box)
                Vector3 postCenter = alongX ? new Vector3(fp, 0.35f, fenceAcross) : new Vector3(fenceAcross, 0.35f, fp);
                town.Out.Solid(postCenter).Box(postCenter, new Vector3(0.05f, 0.70f, 0.05f), DecorPalette.Matte(FenceWood));
                // horizontal cross‑bar (box)
                Vector3 barCenter = alongX ? new Vector3(fp, 0.65f, fenceAcross) : new Vector3(fenceAcross, 0.65f, fp);
                Vector3 barSize = alongX ? new Vector3(0.30f, 0.02f, 0.05f) : new Vector3(0.05f, 0.02f, 0.30f);
                town.Out.Solid(barCenter).Box(barCenter, barSize, DecorPalette.Matte(FenceWood));
                // top cap (small box)
                Vector3 capCenter = alongX ? new Vector3(fp, 0.78f, fenceAcross) : new Vector3(fenceAcross, 0.78f, fp);
                town.Out.Solid(capCenter).Box(capCenter, new Vector3(0.07f, 0.02f, 0.07f), DecorPalette.Matte(FenceWood));
                // occasional lantern (metal pole + glowing lantern)
                if (rng.NextDouble() < 0.2)
                {
                    // metal pole (cylinder)
                    Vector3 poleBase = alongX ? new Vector3(fp, 0.78f, fenceAcross) : new Vector3(fenceAcross, 0.78f, fp);
                    town.Out.Solid(poleBase).Cylinder(poleBase, 0.02f, 0.40f, 8,
                        DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f)));
                    // small metal foot plate at the bottom of the pole
                    Vector3 footCenter = poleBase + new Vector3(0f, -0.01f, 0f);
                    town.Out.Solid(footCenter).Cylinder(footCenter, 0.04f, 0.02f, 12,
                        DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f)));
                    // three rivet studs on the pole
                    for (int i = 0; i < 3; i++)
                    {
                        float height = 0.15f + i * 0.10f;
                        float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                        Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.018f, height, Mathf.Sin(angle) * 0.018f);
                        Vector3 rivetPos = poleBase + offset;
                        town.Out.Solid(rivetPos).Cylinder(rivetPos, 0.006f, 0.008f, 6,
                            DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f)));
                    }
                    // lantern – 50 % chance of glass‑dome style, otherwise simple box (preserve old look)
                    Vector3 lanternCenter = alongX ? new Vector3(fp, 1.25f, fenceAcross) : new Vector3(fenceAcross, 1.25f, fp);
                    if (rng.NextDouble() < 0.5)
                    {
                        // glass dome
                        Vector3 domeCenter = lanternCenter;
                        town.Out.Solid(domeCenter).Sphere(domeCenter, 0.09f,
                            DecorPalette.Glass(new Color(0.80f, 0.90f, 1.00f, 0.5f)));
                        // inner candle – small emissive box
                        Vector3 candleCenter = lanternCenter + new Vector3(0f, -0.04f, 0f);
                        town.Out.Solid(candleCenter).Box(candleCenter,
                            new Vector3(0.04f, 0.06f, 0.04f),
                            DecorPalette.Glow(new Color(1.0f, 0.90f, 0.60f)));
                        // thin metal rim around dome
                        town.Out.Solid(domeCenter).Cylinder(domeCenter, 0.095f, 0.004f, 12,
                            DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f)));
                    }
                    else
                    {
                        // fallback to original simple lantern box (preserve legacy appearance)
                        town.Out.Solid(lanternCenter).Box(lanternCenter,
                            new Vector3(0.12f, 0.12f, 0.12f),
                            DecorPalette.Glow(new Color(1.0f, 0.85f, 0.60f)));
                    }
                    // thin rope (cylinder) from the post cap to the lantern
                    Vector3 ropeStart = capCenter;
                    Vector3 ropeEnd = lanternCenter;
                    Vector3 ropeMid = (ropeStart + ropeEnd) * 0.5f;
                    float ropeLen = Vector3.Distance(ropeStart, ropeEnd);
                    // The Cylinder overload aligns to world up, so we use the centred version with height = ropeLen
                    town.Out.Solid(ropeMid).Cylinder(ropeMid, 0.008f, ropeLen, 8,
                        DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f)));

                }
            }
            // NEW: occasional mailbox (≈15 % chance per sidewalk segment)
            if (b - a >= 0.6f && rng.NextDouble() < 0.15)
            }
            if (rng.NextDouble() < 0.2)
            {
                float rustStart = R(rng, a, b - R(rng, 0.2f, 0.4f));
                float rustLen = R(rng, 0.2f, 0.4f);
                float across = coord + (side > 0 ? hw + 0.125f : -hw - 0.125f);
                // Slightly elevated, thin rectangle to simulate rust stain
                Slab(town, alongX, coord, rustStart, rustStart + rustLen,
                    side > 0 ? hw + 0.02f : -hw - 0.27f,
                    side > 0 ? hw + 0.07f : -hw - 0.22f,
                    0.051f,
                    DecorPalette.Matte(new Color(0.55f, 0.35f, 0.20f)));
            }
            // Add subtle metal edging strip along the kerb (30 % chance)
            if (rng.NextDouble() < 0.3)
            {
                float across = coord + (side > 0 ? hw + 0.125f : -hw - 0.125f);
                Strip(town, alongX, across, a, b, 0.02f, 0.06f, DecorPalette.Matte(new Color(0.20f, 0.20f, 0.20f)));
            }
            float s0 = side > 0 ? hw + 0.25f : -hw - 2.5f, s1 = side > 0 ? hw + 2.5f : -hw - 0.25f;
            Slab(town, alongX, coord, a, b, s0, s1, 0.025f, concrete);
            // Occasionally add a small translucent ice patch on the sidewalk (20% chance)
            if (rng.NextDouble() < 0.2)
            {
                float iceSize = R(rng, 0.15f, 0.3f);
                float icePos = R(rng, a, b);
                Vector3 iceCenter = alongX ? new Vector3(icePos, 0.03f, coord + (s0 + s1) * 0.5f) : new Vector3(coord + (s0 + s1) * 0.5f, 0.03f, icePos);
                // Use a light bluish glass material to look like ice; size controls radius X and Z
                town.Out.Flat(iceCenter).FlatBlob(iceCenter, iceSize, iceSize, R(rng, 0f, 360f), 0.2f, rng, DecorPalette.Glass(new Color(0.7f, 0.85f, 1f)));
            }
            if (rng.NextDouble() < 0.6)
            {
                float t = R(rng, a, b);
                Vector3 p = alongX ? new Vector3(t, 0.03f, coord + (s0 + s1) * 0.5f) : new Vector3(coord + (s0 + s1) * 0.5f, 0.03f, t);
                town.Out.Flat(p).FlatBlob(p, R(rng, 0.6f, 1.6f), R(rng, 0.5f, 1.1f), R(rng, 0f, 360f), 0.4f, rng, DecorPalette.Matte(DecorProps.SnowShade));
            }
        }

        /// <summary>Creates a period‑correct wooden mailbox (post, box, metal latch, tiny flag) on a sidewalk.</summary>
        private static void CreateMailbox(RaidTown town, bool alongX, float posAlong, float across)
        {
            // post – 0.05 m × 0.75 m × 0.05 m
            Vector3 postCenter = alongX
                ? new Vector3(posAlong, 0.38f, across) // y = half height
                : new Vector3(across, 0.38f, posAlong);
            town.Out.Solid(postCenter).Box(postCenter,
                new Vector3(0.05f, 0.75f, 0.05f),
                DecorPalette.Matte(FenceWood));

            // mailbox body – 0.30 m × 0.20 m × 0.15 m, placed 0.05 m in front of post
            Vector3 bodyOffset = alongX ? new Vector3(0f, 0.20f, 0.08f) : new Vector3(0.08f, 0.20f, 0f);
            Vector3 bodyCenter = postCenter + bodyOffset;
            town.Out.Solid(bodyCenter).Box(bodyCenter,
                new Vector3(0.30f, 0.20f, 0.15f),
                DecorPalette.Matte(FenceWood));

            // metal latch – thin cylinder on front edge of box
            Vector3 latchPos = bodyCenter + (alongX ? new Vector3(0f, 0.10f, 0.08f) : new Vector3(0.08f, 0.10f, 0f));
            town.Out.Solid(latchPos).Cylinder(latchPos, 0.015f, 0.02f, 12,
                DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f)));

            // small flag pole – thin cylinder on top right of box
            Vector3 flagPoleBase = bodyCenter + (alongX ? new Vector3(0.15f, 0.20f, 0f) : new Vector3(0f, 0.20f, 0.15f));
            town.Out.Solid(flagPoleBase).Cylinder(flagPoleBase, 0.008f, 0.30f, 8,
                DecorPalette.Metal(new Color(0.55f, 0.55f, 0.55f)));

            // flag – tiny box at pole top (faded red‑brown cloth)
            Vector3 flagCenter = flagPoleBase + (alongX ? new Vector3(0f, 0.30f, 0f) : new Vector3(0f, 0.30f, 0f));
            town.Out.Solid(flagCenter).Box(flagCenter,
                new Vector3(0.12f, 0.02f, 0.02f),
                DecorPalette.Matte(new Color(0.68f, 0.48f, 0.30f)));
        }




        /// <summary>A flat rectangle laid along a road: <paramref name="a"/>..<paramref name="b"/> along it,
        /// <paramref name="c0"/>..<paramref name="c1"/> across (relative to the road's centre line).</summary>
        private static void Slab(RaidTown town, bool alongX, float coord, float a, float b, float c0, float c1, float y, in Surf s)
        {
            Vector3 center = alongX
                ? new Vector3((a + b) * 0.5f, y, coord + (c0 + c1) * 0.5f)
                : new Vector3(coord + (c0 + c1) * 0.5f, y, (a + b) * 0.5f);
            float len = b - a, wid = c1 - c0;
            if (alongX) town.Out.Flat(center).Flat(center, len, wid, 0f, s);
            else town.Out.Flat(center).Flat(center, wid, len, 0f, s);
        }

        private static void Strip(RaidTown town, bool alongX, float across, float a, float b, float width, float y, in Surf s)
        {
            if (b <= a) return;
            Vector3 c = alongX ? new Vector3((a + b) * 0.5f, y, across) : new Vector3(across, y, (a + b) * 0.5f);
            if (alongX) town.Out.Flat(c).Flat(c, b - a, width, 0f, s);
            else town.Out.Flat(c).Flat(c, width, b - a, 0f, s);
        }

        /// <summary>A flat paved patch (driveway, lot, path, forecourt) at height <paramref name="y"/>.</summary>
        private static void Paved(RaidTown town, Rect r, float y, in Surf s)
        {
            for (float x = r.xMin; x < r.xMax - 0.01f; x += 12f)
                for (float z = r.yMin; z < r.yMax - 0.01f; z += 12f)
                {
                    float x1 = Mathf.Min(r.xMax, x + 12f), z1 = Mathf.Min(r.yMax, z + 12f);
                    var c = new Vector3((x + x1) * 0.5f, y, (z + z1) * 0.5f);
                    town.Out.Flat(c).Flat(c, x1 - x, z1 - z, 0f, s);
                }
        }

        /// <summary>True when the XZ rect overlaps a street or its sidewalks.</summary>
        private static bool OnRoad(Rect r)
        {
            foreach (RoadSpec rd in s_Roads)
            {
                float hw = rd.Width * 0.5f + 2.6f;
                Rect road = rd.AlongX
                    ? Rect.MinMaxRect(rd.From, rd.Coord - hw, rd.To, rd.Coord + hw)
                    : Rect.MinMaxRect(rd.Coord - hw, rd.From, rd.Coord + hw, rd.To);
                if (road.Overlaps(r)) return true;
            }
            return false;
        }
    }
}
