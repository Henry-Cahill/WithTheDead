using UnityEngine;
using CloneZ;

namespace CloneZ
{
    internal static class StreetLampHelper
    {
        private const float PoleHeight = 4.0f;
        private const float ArmRootY = 3.82f;   // where the arm leaves the pole
        private const float ArmTipY = 4.02f;    // the arm rises ~20 degrees to its tip, level with the pole's top
        private const float ArmReach = 0.56f;   // how far the tip reaches out from the pole, over the road
        private const float ShadeRimY = 3.87f;  // the bottom of the shade hanging under the tip

        // Helper to convert 2-D map coords to 3-D world coords (y is up).
        private static Vector3 To3(Vector2 v, float y) => new Vector3(v.x, y, v.y);

        // The yellow glass shade and its bulb glow (unlit), like the lit lenses of DecorProps.StreetLight.
        private static Surf ShadeSurf => DecorPalette.Glow(new Color(1f, 0.82f, 0.45f));
        private static Surf BulbSurf => DecorPalette.Glow(new Color(1f, 0.97f, 0.88f));
        private static Surf SnowMoundSurf => DecorPalette.Matte(DecorProps.SnowWhite);
        private static Surf IceSurf => DecorPalette.Gloss(new Color(0.80f, 0.87f, 0.94f));
        // Wooden street-sign.
        private static Surf SignSurf => DecorPalette.Matte(new Color(0.45f, 0.30f, 0.15f));
        private static Surf RustSurf => DecorPalette.Matte(new Color(0.40f, 0.30f, 0.20f));

        /// <summary>
        /// Builds a period-correct 1993-style street lamp at <paramref name="pos"/>, a spot the caller has already
        /// checked and taken: a metal base plate, a 4 m iron pole flecked with rust, an arm from near its top with a
        /// lit yellow glass shade and bright bulb at its tip, a wooden sign hanging from a bracket on the pole with an
        /// icicle under it, snow banked against the base, and a warm point light. <paramref name="yaw"/> (degrees about
        /// Y, as for the props' frames) is the way the arm reaches: local +Z points along (sin yaw, 0, cos yaw), so pass
        /// Atan2(toRoad.x, toRoad.y) to have it reach out over the road.
        /// </summary>
        public static void PlaceLamp(OutdoorBatches into, KentuckyMap.Site site, Vector2 pos, float yaw)
        {
            Surf metal = KentuckyMap.StreetLampMetal;
            var rng = new System.Random(Mathf.RoundToInt(pos.x * 10f) * 73856093 ^ Mathf.RoundToInt(pos.y * 10f) * 19349663);
            Vector3 at = To3(pos, 0f);
            RaidMesh m = into.Solid(at);
            m.SetFrame(at, yaw);   // local +Z: out over the road; +X: along it

            // Base plate, and snow banked against it on the side away from the road.
            m.Box(new Vector3(0f, 0.01f, 0f), new Vector3(0.30f, 0.02f, 0.30f), metal);
            m.Ellipsoid(new Vector3(0f, 0.02f, -0.20f), new Vector3(0.42f, 0.24f, 0.34f), SnowMoundSurf, 8, 4);

            // Iron pole, standing on the plate.
            m.Cylinder(new Vector3(0f, 0.02f, 0f), 0.04f, PoleHeight, 12, metal);

            // Arm, angled up from near the pole's top, ending in a socket.
            m.Rod(new Vector3(0f, ArmRootY, 0.02f), new Vector3(0f, ArmTipY, ArmReach), 0.035f, metal);
            m.Cylinder(new Vector3(0f, ShadeRimY + 0.12f, ArmReach), 0.05f, 0.05f, 10, metal);

            // Shade: a glowing yellow glass bell under the socket, the bright bulb showing through its mouth.
            m.Cylinder(new Vector3(0f, ShadeRimY, ArmReach), 0.15f, 0.12f, 12, ShadeSurf, Quaternion.identity, 0.3f);
            var bulb = new Vector3(0f, ShadeRimY, ArmReach);
            m.Ellipsoid(bulb, new Vector3(0.08f, 0.08f, 0.08f), BulbSurf);

            // Wooden sign hanging broadside to the road from a bracket on the pole (a tiny wobble), an icicle under it.
            m.Box(new Vector3(0.29f, 2.80f, 0f), new Vector3(0.52f, 0.025f, 0.025f), metal);
            m.Box(new Vector3(0.12f, 2.76f, 0f), new Vector3(0.01f, 0.08f, 0.01f), metal);
            m.Box(new Vector3(0.46f, 2.76f, 0f), new Vector3(0.01f, 0.08f, 0.01f), metal);
            m.Box(new Vector3(0.29f, 2.62f, 0f), new Vector3(0.46f, 0.20f, 0.025f), R(rng, -2f, 2f), SignSurf);
            m.Cylinder(new Vector3(0.29f + R(rng, -0.15f, 0.15f), 2.52f, 0f), 0.012f, R(rng, 0.07f, 0.13f), 5, IceSurf,
                Quaternion.Euler(180f, 0f, 0f), 0f);

            // Rust speckles: 3-5 small patches on the pole's surface.
            int speckCount = rng.Next(3, 6);
            for (int i = 0; i < speckCount; i++)
            {
                float h = R(rng, 0.3f, PoleHeight - 0.4f);
                float angle = R(rng, 0f, 360f);
                var onSurface = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0f, Mathf.Cos(angle * Mathf.Deg2Rad)) * 0.04f;
                m.Box(onSurface + Vector3.up * h, new Vector3(0.025f, 0.035f, 0.012f), angle, RustSurf);
            }

            Vector3 lightAt = m.ToMeshSpace(bulb);
            m.ResetFrame();
            // Collider so the player can't walk through the pole.
            site.Town.PropCollider(To3(pos, 0.02f + PoleHeight * 0.5f), new Vector3(0.2f, PoleHeight, 0.2f), 0f);

            // A warm point light at the bulb, under the world root like the street lights' (RoomLight switches it off
            // past its cull distance). No shadows: there can be hundreds of these lamps.
            site.Town.Light(site.Town.World, lightAt, KentuckyMap.StreetLampLightColor, 8f, 2.5f, flicker: true, shadows: false);
        }

        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
    }
}