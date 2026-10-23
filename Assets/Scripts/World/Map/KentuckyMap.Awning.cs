using System;
using UnityEngine;

namespace CloneZ
{
    // Builds period‑correct wooden awnings for retail façades:
// – deterministic wood colour per building
// – side support posts and horizontal slats
// – sign pole with painted board
// Partial class to isolate awning and sign logic for retail‑type buildings.
    public static partial class KentuckyMap
    {
        private static Color GetAwningColor(MapBuilding b)
        {
            // deterministic hue shift based on building id hash
            int seed = (int)b.id; // use id field
            UnityEngine.Random.InitState(seed);
            float hue = UnityEngine.Random.Range(0.07f, 0.12f); // brown range
            float sat = UnityEngine.Random.Range(0.55f, 0.65f);
            float val = UnityEngine.Random.Range(0.45f, 0.55f);
            return UnityEngine.Color.HSVToRGB(hue, sat, val);
        }

        private static Color GetBannerColor(MapBuilding b, System.Random rng)
        {
            // start from the awning colour, lift brightness a little and add a tiny random hue shift
            Color baseCol = GetAwningColor(b);
            float h, s, v;
            Color.RGBToHSV(baseCol, out h, out s, out v);
            h += (float)rng.NextDouble() * 0.02f - 0.01f; // ±0.01 hue
            v = Mathf.Clamp01(v + 0.12f); // a bit brighter for canvas
            return Color.HSVToRGB(h, s * 0.9f, v);
        }
        private static void BuildAwningAndSign(Site site, Vector2 doorPt, Vector2 outward, float yaw, MapBuilding b, System.Random rng)
        {
            // ----- awning -------------------------------------------------
            // Width = 2 m, depth = 0.6 m, height above door = 1.2 m, thickness = 0.08 m
            const float awningWidth = 2.0f;
            const float awningDepth = 0.6f;
            const float awningHeightAboveDoor = 1.2f;
            const float awningThickness = 0.08f;

        // added constants for new decorative parts
        const float BracketSize = 0.05f; // 5 cm cube for metal brackets
        const float LanternSize = 0.12f; // 12 cm cube for lantern glow
            // Center of the awning (mid‑point over the door, shifted outward)
            Vector3 awningCenter = To3(doorPt + outward * (awningDepth * 0.5f), DoorHeight + awningHeightAboveDoor);
            Vector3 awningSize = new Vector3(awningWidth, awningThickness, awningDepth);
            // Use a slightly weathered wood colour, deterministic per building
            Color awningCol = GetAwningColor(b);
            Surf awningMat = DecorMaterials.WoodFloor(awningCol);
            site.Overhead.At(awningCenter).Box(awningCenter, awningSize, yaw, awningMat);
            // ----- side support posts -----
            float supportWidth = 0.07f;
            float supportHeight = awningHeightAboveDoor + 0.5f;
            Vector3 leftOffset = Vector3.left * (awningWidth * 0.5f - supportWidth * 0.5f);
            Vector3 rightOffset = Vector3.right * (awningWidth * 0.5f - supportWidth * 0.5f);
            Vector3 leftPos = awningCenter + leftOffset;
            Vector3 rightPos = awningCenter + rightOffset;
site.Town.Out.Solid(leftPos).Box(leftPos,
                 new Vector3(supportWidth, supportHeight, supportWidth), 0f, awningMat);
            site.Town.PropCollider(leftPos,
                 new Vector3(supportWidth, supportHeight, supportWidth), 0f);
            // Thin vertical rope tie on left post (jute colour)
            var ropeColor = DecorPalette.Matte(new Color(0.545f, 0.435f, 0.255f));
            var ropeSize = new Vector3(0.02f, supportHeight, 0.02f);
            site.Town.Out.Solid(leftPos).Box(leftPos, ropeSize, 0f, ropeColor);
            site.Town.PropCollider(leftPos, ropeSize, 0f);
            site.Town.Out.Solid(rightPos).Box(rightPos,
                 new Vector3(supportWidth, supportHeight, supportWidth), 0f, awningMat);
            site.Town.PropCollider(rightPos,
                 new Vector3(supportWidth, supportHeight, supportWidth), 0f);
            // Thin vertical rope tie on right post (jute colour)
            site.Town.Out.Solid(rightPos).Box(rightPos, ropeSize, 0f, ropeColor);
            site.Town.PropCollider(rightPos, ropeSize, 0f);

            // ----- horizontal slats -----
            int slatCount = 4;
            float slatThickness = 0.02f;
            float slatDepth = awningDepth * 0.9f;
            float slatStartY = awningCenter.y - awningThickness * 0.5f + slatThickness * 0.5f;
            // ----- metal corner brackets (period‑correct iron brackets) -----
            Surf metalMat = DecorPalette.Metal(new Color(0.45f, 0.45f, 0.48f));
            float halfW = awningWidth * 0.5f;
            float halfD = awningDepth * 0.5f;
            Vector3[] corners = new Vector3[]
            {
                awningCenter + new Vector3(-halfW, awningHeightAboveDoor, -halfD),
                awningCenter + new Vector3( halfW, awningHeightAboveDoor, -halfD),
                awningCenter + new Vector3(-halfW, awningHeightAboveDoor,  halfD),
                awningCenter + new Vector3( halfW, awningHeightAboveDoor,  halfD)
            };
            foreach (var c in corners)
            {
                site.Overhead.At(c).Box(c, new Vector3(BracketSize, BracketSize, BracketSize), 0f, metalMat);
                site.Town.PropCollider(c, new Vector3(BracketSize, BracketSize, BracketSize), 0f);
            }
            // Pole: 0.05 × 0.05 × 1.0 m, board: 0.6 × 0.15 × 0.02 m
            const float poleHeight = 1.0f;
            const float poleSize = 0.05f;
            const float boardWidth = 0.6f;
            const float boardHeight = 0.15f;
            const float boardDepth = 0.02f;
            // Position the pole slightly to the side of the door (right‑hand side when facing outward)
            Vector3 right = Vector3.Cross(Vector3.up, new Vector3(outward.x, 0f, outward.y));
            Vector3 poleBase = To3(doorPt + outward * 0.2f + new Vector2(right.x, right.z) * 0.6f, 0f);
            Vector3 poleCenter = poleBase + new Vector3(0f, poleHeight * 0.5f, 0f);
            site.Town.Out.Solid(poleCenter).Box(poleCenter,
                new Vector3(poleSize, poleHeight, poleSize), 0f,
                DecorMaterials.WoodFloor(new Color(0.48f, 0.38f, 0.28f)));
            site.Town.PropCollider(poleCenter,
                new Vector3(poleSize, poleHeight, poleSize), 0f);
            // ----- tiny lantern hanging from the top of the pole -----
            Vector3 lanternPos = poleCenter + new Vector3(0f, poleHeight * 0.5f + LanternSize * 0.5f + 0.05f, 0f);
            Surf lanternMat = DecorPalette.Glow(new Color(1.00f, 0.85f, 0.60f)); // warm amber glow
            site.Town.Out.Solid(lanternPos).Box(lanternPos,
                new Vector3(LanternSize, LanternSize, LanternSize), 0f, lanternMat);
            site.Town.PropCollider(lanternPos,
                new Vector3(LanternSize, LanternSize, LanternSize), 0f);
            // ----- fabric banner draped in front of the awning -----
            float bannerWidth = awningWidth * 0.78f;
            float bannerHeight = 0.38f;
            float bannerDepth = 0.02f;
            Vector3 bannerCenter = new Vector3(
                awningCenter.x,
                awningCenter.y - awningThickness * 0.5f + bannerHeight * 0.5f,
                awningCenter.z + (awningDepth * 0.5f) - (bannerDepth * 0.5f));
            Color bannerCol = GetBannerColor(b, rng);
            Surf bannerMat = DecorPalette.Matte(bannerCol);
            site.Town.Out.Solid(bannerCenter).Box(bannerCenter,
                new Vector3(bannerWidth, bannerHeight, bannerDepth), yaw, bannerMat);
            site.Town.PropCollider(bannerCenter,
                new Vector3(bannerWidth, bannerHeight, bannerDepth), yaw);
            // original radius was awningWidth * 0.5f + 0.4f
            site.Take(doorPt + outward * (awningDepth + 0.7f),
                awningWidth * 0.5f + 0.6f); // extra margin for banner & brackets
        }
    }
}
