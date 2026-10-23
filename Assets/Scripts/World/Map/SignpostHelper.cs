using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Builds a period‑correct road signpost: metal pole, bracket, aged wooden board and bolts.
    /// All dimensions are in metres; deterministic variation is derived from the sign position.
    /// </summary>
    internal static class SignpostHelper
    {
        private static readonly Color PoleMetalColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Surf PoleMetal = DecorPalette.Matte(PoleMetalColor);
        private static readonly Color BoardBaseColor = new Color(0.78f, 0.73f, 0.56f); // aged wood
        private static readonly Color BoltColor = new Color(0.48f, 0.48f, 0.48f);
        private static readonly Surf BoltMat = DecorPalette.Matte(BoltColor);

        public static void PlaceSignpost(OutdoorBatches into, KentuckyMap.Site site, Vector2 pos, float yaw)
        {
            // deterministic rng from position
            int seed = ((int)(pos.x * 73856093)) ^ ((int)(pos.y * 19349663));
            var rng = new System.Random(seed);
            // slight random yaw offset ±5°
            float yawOffset = (float)(rng.NextDouble() * 10.0 - 5.0);
            float finalYaw = yaw + yawOffset;

            // GameObject holder for hierarchy
            var go = new GameObject("RoadSign");
            go.transform.SetParent(site.Town.World, false);
            go.transform.localPosition = new Vector3(pos.x, 0f, pos.y);
            go.transform.localEulerAngles = new Vector3(0f, finalYaw, 0f);

            // 1. Metal pole – 0.1 m × 2.5 m × 0.1 m, centre at (0,1.25,0)
            var poleMesh = new RaidMesh();
            poleMesh.Box(new Vector3(0f, 1.25f, 0f), new Vector3(0.1f, 2.5f, 0.1f), PoleMetal);
            poleMesh.ApplyTo(go, Vector3.zero, ShadowCastingMode.On);

            // 2. Bracket – 0.2 m × 0.1 m × 0.02 m, centred at (0,1.8,0.06)
            var bracketMesh = new RaidMesh();
            bracketMesh.Box(new Vector3(0f, 1.8f, 0.06f), new Vector3(0.2f, 0.1f, 0.02f), PoleMetal);
            bracketMesh.ApplyTo(go, Vector3.zero, ShadowCastingMode.On);

            // 3. Sign board – 1.2 m × 0.5 m × 0.02 m, centre at (0,1.9,0.30)
            // colour jitter ±0.04 on each channel
            float colJitter = (float)(rng.NextDouble() * 0.08 - 0.04);
            Color boardCol = new Color(
                Mathf.Clamp01(BoardBaseColor.r + colJitter),
                Mathf.Clamp01(BoardBaseColor.g + colJitter),
                Mathf.Clamp01(BoardBaseColor.b + colJitter));
            var boardSurf = DecorPalette.Matte(boardCol);
            var boardMesh = new RaidMesh();
            boardMesh.Box(new Vector3(0f, 1.9f, 0.30f), new Vector3(1.2f, 0.5f, 0.02f), boardSurf);
            boardMesh.ApplyTo(go, Vector3.zero, ShadowCastingMode.On);

            // 4‑7. Four bolts – 0.02 m³ cubes at board corners
            Vector3[] boltOffsets = new[]
            {
                new Vector3(-0.58f, 1.64f, 0.31f), // lower‑left
                new Vector3( 0.58f, 1.64f, 0.31f), // lower‑right
                new Vector3(-0.58f, 2.16f, 0.31f), // upper‑left
                new Vector3( 0.58f, 2.16f, 0.31f)  // upper‑right
            };
            foreach (var off in boltOffsets)
            {
                var boltMesh = new RaidMesh();
                boltMesh.Box(off, new Vector3(0.02f, 0.02f, 0.02f), BoltMat);
                boltMesh.ApplyTo(go, Vector3.zero, ShadowCastingMode.On);
            }
        }
    }
}
