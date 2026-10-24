using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Builds a period-correct road signpost: metal pole, bracket, aged wooden board and bolts.
    /// All dimensions are in metres; deterministic variation is derived from the sign position.
    /// </summary>
    internal static class SignpostHelper
    {
        private static readonly Color PoleMetalColor = new Color(0.55f, 0.55f, 0.55f);
        private static Surf PoleMetal => DecorPalette.Matte(PoleMetalColor);
        private static readonly Color BoardBaseColor = new Color(0.78f, 0.73f, 0.56f); // aged wood
        private static readonly Color BoltColor = new Color(0.48f, 0.48f, 0.48f);
        private static Surf BoltMat => DecorPalette.Matte(BoltColor);

        // The four bolts, at the board's corners on its front face.
        private static readonly Vector3[] BoltOffsets =
        {
            new Vector3(-0.56f, 1.68f, 0.095f), new Vector3(0.56f, 1.68f, 0.095f),
            new Vector3(-0.56f, 2.12f, 0.095f), new Vector3(0.56f, 2.12f, 0.095f),
        };

        /// <summary>
        /// Builds a signpost at <paramref name="pos"/>, a roadside spot the caller has checked and taken. The board faces
        /// <paramref name="yaw"/>, give or take 5 degrees (degrees about Y: local +Z points along (sin yaw, 0, cos yaw)).
        /// </summary>
        public static void PlaceSignpost(OutdoorBatches into, KentuckyMap.Site site, Vector2 pos, float yaw)
        {
            // deterministic rng from position
            var rng = new System.Random(Mathf.RoundToInt(pos.x * 10f) * 73856093 ^ Mathf.RoundToInt(pos.y * 10f) * 19349663);
            // slight random yaw offset ±5°
            float finalYaw = yaw + (float)(rng.NextDouble() * 10.0 - 5.0);
            Vector3 at = KentuckyMap.To3(pos, 0f);
            RaidMesh m = into.Solid(at);
            m.SetFrame(at, finalYaw);

            // 1. Metal pole – 0.1 m × 2.5 m × 0.1 m
            m.Box(new Vector3(0f, 1.25f, 0f), new Vector3(0.1f, 2.5f, 0.1f), PoleMetal);
            // 2. Bracket – a plate on the pole's front
            m.Box(new Vector3(0f, 1.8f, 0.06f), new Vector3(0.2f, 0.1f, 0.02f), PoleMetal);
            // 3. Sign board – 1.2 m × 0.5 m × 0.02 m against the bracket, colour jitter ±0.04 on each channel
            float colJitter = (float)(rng.NextDouble() * 0.08 - 0.04);
            Color boardCol = new Color(
                Mathf.Clamp01(BoardBaseColor.r + colJitter),
                Mathf.Clamp01(BoardBaseColor.g + colJitter),
                Mathf.Clamp01(BoardBaseColor.b + colJitter));
            m.Box(new Vector3(0f, 1.9f, 0.08f), new Vector3(1.2f, 0.5f, 0.02f), DecorPalette.Matte(boardCol));
            // 4-7. Four bolts – 0.02 m cubes
            foreach (Vector3 off in BoltOffsets) m.Box(off, new Vector3(0.02f, 0.02f, 0.02f), BoltMat);
            m.ResetFrame();
            // Collider so the player can't walk through the pole.
            site.Town.PropCollider(at + Vector3.up * 1.25f, new Vector3(0.1f, 2.5f, 0.1f), 0f);
        }
    }
}