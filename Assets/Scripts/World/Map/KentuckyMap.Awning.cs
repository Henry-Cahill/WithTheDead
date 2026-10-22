using System;
using UnityEngine;

namespace CloneZ
{
    // Partial class to isolate awning and sign logic for retail‑type buildings.
    public static partial class KentuckyMap
    {
        // Adds a simple wooden awning over the front door and a small wooden sign‑board.
        // Used for retail‑type buildings (kind "retail" or use starting with "shop:").
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
            Color awningCol = new Color(0.55f, 0.45f, 0.30f);
            Surf awningMat = DecorMaterials.WoodFloor(awningCol);
            site.Overhead.At(awningCenter).Box(awningCenter, awningSize, yaw, awningMat);
            // No collider – awning is overhead only

            // ----- sign post -----------------------------------------------
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
            // Reserve space so other props don’t intersect
            site.Take(doorPt + outward * (awningDepth + 0.5f), awningWidth * 0.5f + 0.2f);
        }
    }
}
