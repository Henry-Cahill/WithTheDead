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
        private static void BuildAwningAndSign(Site site, Vector2 doorPt, Vector2 outward, float yaw, MapBuilding b, System.Random rng)
        {
            // ----- awning -------------------------------------------------
            // Width = 2 m, depth = 0.6 m, height above door = 1.2 m, thickness = 0.08 m
            const float awningWidth = 2.0f;
            const float awningDepth = 0.6f;
            const float awningHeightAboveDoor = 1.2f;
            const float awningThickness = 0.08f;
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
            site.Town.Out.Solid(rightPos).Box(rightPos,
                new Vector3(supportWidth, supportHeight, supportWidth), 0f, awningMat);
            site.Town.PropCollider(rightPos,
                new Vector3(supportWidth, supportHeight, supportWidth), 0f);

            // ----- horizontal slats -----
            int slatCount = 4;
            float slatThickness = 0.02f;
            float slatDepth = awningDepth * 0.9f;
            float slatStartY = awningCenter.y - awningThickness * 0.5f + slatThickness * 0.5f;
            for (int i = 0; i < slatCount; i++)
            {
                float y = slatStartY + i * (awningHeightAboveDoor / (slatCount + 1));
                Vector3 slatCenter = new Vector3(awningCenter.x, y, awningCenter.z);
                site.Overhead.At(slatCenter).Box(slatCenter,
                    new Vector3(awningWidth * 0.95f, slatThickness, slatDepth),
                    yaw, awningMat);
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
            // Board sits on top of the pole, faces the street (same yaw as door)
            Vector3 boardCenter = poleBase + new Vector3(0f, poleHeight + boardHeight * 0.5f, 0f);
            site.Town.Out.Solid(boardCenter).Box(boardCenter,
                new Vector3(boardWidth, boardHeight, boardDepth), yaw,
                DecorPalette.Matte(new Color(0.90f, 0.84f, 0.70f)));
            site.Town.PropCollider(boardCenter,
                new Vector3(boardWidth, boardHeight, boardDepth), yaw);
            // ----- painted sign area -----
            Color paintCol = new Color(0.75f, 0.20f, 0.20f); // faded red
            Vector3 paintCenter = boardCenter + new Vector3(0f, 0f, boardDepth * 0.51f);
            site.Town.Out.Solid(paintCenter).Box(paintCenter,
                new Vector3(boardWidth * 0.9f, boardHeight * 0.8f, 0.01f),
                yaw, DecorPalette.Matte(paintCol));
            site.Town.PropCollider(paintCenter,
                new Vector3(boardWidth * 0.9f, boardHeight * 0.8f, 0.01f), yaw);
            site.Take(doorPt + outward * (awningDepth + 0.7f), awningWidth * 0.5f + 0.4f);
        }
    }
}
