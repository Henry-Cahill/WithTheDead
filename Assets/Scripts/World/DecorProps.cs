using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Outdoor props and loose debris for the raid map: wrecked cars, bare trees and pines, dumpsters,
    /// sandbag walls, concrete barriers, street lights, gas pumps, mailboxes, signs, snow piles, trash
    /// bags, and the litter strewn over floors and streets (papers, cardboard, cans, bottles, leaves,
    /// stains, glass). Drawn into a <see cref="RaidMesh"/> around the local origin, front facing +Z.
    /// </summary>
    internal static class DecorProps
    {
        private static Surf M(Color c) => DecorPalette.Matte(c);
        private static Surf G(Color c) => DecorPalette.Gloss(c);
        private static Surf Mt(Color c) => DecorPalette.Metal(c);
        private static Surf Gs(Color c) => DecorPalette.Glass(c);
        private static Surf Gl(Color c) => DecorPalette.Glow(c);
        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        private static bool Chance(System.Random rng, double p) => rng.NextDouble() < p;
        private static Color V(Color c, System.Random rng, float amount = 0.08f) => DecorPalette.Vary(c, amount, rng);
        private static Quaternion Yaw(float d) => Quaternion.Euler(0f, d, 0f);

        public static readonly Color SnowWhite = new Color(0.86f, 0.89f, 0.95f);
        public static readonly Color SnowShade = new Color(0.72f, 0.77f, 0.86f);
        private static readonly Color Tyre = new Color(0.07f, 0.07f, 0.07f);

        #region Vehicles
        public enum CarKind { Sedan, Wagon, Police, ArmyTruck, Pickup }

        /// <summary>A wrecked car (~4.4 × 1.8, front at +Z): body, cabin with dark or broken glass,
        /// bumpers, lights, wheels (one maybe flat), rust and a blanket of snow. Its footprint for a
        /// collider is returned in <paramref name="size"/>.</summary>
        public static void Car(RaidMesh m, System.Random rng, CarKind kind, Color paint, out Vector3 size)
        {
            float len = kind == CarKind.ArmyTruck ? 5.6f : (kind == CarKind.Wagon ? 4.7f : 4.4f);
            float wid = kind == CarKind.ArmyTruck ? 2.2f : 1.78f;
            size = new Vector3(wid, kind == CarKind.ArmyTruck ? 2.5f : 1.4f, len);
            Color c = V(paint, rng, 0.06f);
            Color glass = new Color(0.07f, 0.09f, 0.10f);
            Color chrome = new Color(0.55f, 0.56f, 0.56f);
            float wheelR = kind == CarKind.ArmyTruck ? 0.48f : 0.33f;
            float bodyY = wheelR + 0.02f;

            // Wheels (a flat tyre squashes one corner).
            int flat = Chance(rng, 0.4) ? rng.Next(4) : -1;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (wid * 0.5f - 0.14f);
                float z = (i < 2 ? 1f : -1f) * (len * 0.5f - (kind == CarKind.ArmyTruck ? 1.0f : 0.85f));
                float r = i == flat ? wheelR * 0.8f : wheelR;
                m.CylinderX(new Vector3(x, r, z), r, 0.24f, 12, M(Tyre));
                m.CylinderX(new Vector3(x + Mathf.Sign(x) * 0.121f, r, z), r * 0.55f, 0.01f, 10, Mt(chrome * 0.8f));
            }

            if (kind == CarKind.ArmyTruck)
            {
                Color o = V(new Color(0.28f, 0.31f, 0.20f), rng, 0.05f);
                m.Box(new Vector3(0f, bodyY + 0.3f, 0f), new Vector3(wid - 0.1f, 0.3f, len), M(o * 0.6f));
                m.Box(new Vector3(0f, bodyY + 0.95f, len * 0.5f - 0.8f), new Vector3(wid, 1.0f, 1.4f), M(o));
                m.Box(new Vector3(0f, bodyY + 1.75f, len * 0.5f - 0.95f), new Vector3(wid - 0.1f, 0.6f, 1.1f), M(o));
                m.Box(new Vector3(0f, bodyY + 1.75f, len * 0.5f - 0.39f), new Vector3(wid - 0.3f, 0.45f, 0.02f), Gs(glass));
                m.Box(new Vector3(0f, bodyY + 0.75f, len * 0.5f - 0.08f), new Vector3(wid - 0.2f, 0.5f, 0.06f), M(new Color(0.1f, 0.1f, 0.1f)));
                // Canvas-covered bed.
                m.Box(new Vector3(0f, bodyY + 0.6f, -0.75f), new Vector3(wid, 0.5f, 3.5f), M(o * 0.9f));
                m.Box(new Vector3(0f, bodyY + 1.45f, -0.75f), new Vector3(wid - 0.04f, 1.2f, 3.5f), M(V(new Color(0.33f, 0.34f, 0.24f), rng, 0.05f)));
                m.Box(new Vector3(0f, bodyY + 2.07f, -0.75f), new Vector3(wid - 0.3f, 0.06f, 3.4f), M(SnowWhite));
                m.Box(new Vector3(0f, bodyY + 2.07f, len * 0.5f - 0.95f), new Vector3(wid - 0.3f, 0.05f, 1.0f), M(SnowWhite));
                m.Box(new Vector3(0f, bodyY + 1.47f, len * 0.5f - 0.75f), new Vector3(wid - 0.3f, 0.05f, 0.8f), M(SnowWhite));
                m.Box(new Vector3(0.4f, bodyY + 0.95f, len * 0.5f - 0.09f), new Vector3(0.5f, 0.12f, 0.01f), M(new Color(0.85f, 0.85f, 0.8f)));
                return;
            }

            // Lower body, hood and trunk.
            m.Box(new Vector3(0f, bodyY + 0.3f, 0f), new Vector3(wid, 0.56f, len - 0.1f), G(c));
            m.Box(new Vector3(0f, bodyY + 0.06f, len * 0.5f - 0.04f), new Vector3(wid - 0.02f, 0.14f, 0.1f), Mt(chrome));
            m.Box(new Vector3(0f, bodyY + 0.06f, -len * 0.5f + 0.04f), new Vector3(wid - 0.02f, 0.14f, 0.1f), Mt(chrome));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * (wid * 0.5f - 0.22f), bodyY + 0.34f, len * 0.5f - 0.045f), new Vector3(0.32f, 0.14f, 0.02f), Gs(new Color(0.75f, 0.74f, 0.66f)));
                m.Box(new Vector3(s * (wid * 0.5f - 0.2f), bodyY + 0.36f, -len * 0.5f + 0.045f), new Vector3(0.3f, 0.12f, 0.02f), Gs(new Color(0.5f, 0.08f, 0.06f)));
            }

            // Cabin: pillars over a glass band; wagons run the roof to the back.
            float cabLen = kind == CarKind.Wagon ? 2.9f : (kind == CarKind.Pickup ? 1.5f : 2.2f);
            float cabZ = kind == CarKind.Wagon ? -0.55f : (kind == CarKind.Pickup ? 0.35f : -0.2f);
            float cabY = bodyY + 0.58f;
            m.Box(new Vector3(0f, cabY + 0.22f, cabZ), new Vector3(wid - 0.18f, 0.44f, cabLen), Gs(glass));
            m.Box(new Vector3(0f, cabY + 0.47f, cabZ), new Vector3(wid - 0.14f, 0.06f, cabLen + 0.04f), G(c));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * (wid * 0.5f - 0.1f), cabY + 0.22f, cabZ + cabLen * 0.5f - 0.04f), new Vector3(0.06f, 0.46f, 0.08f), G(c));
                m.Box(new Vector3(s * (wid * 0.5f - 0.1f), cabY + 0.22f, cabZ - cabLen * 0.5f + 0.04f), new Vector3(0.06f, 0.46f, 0.08f), G(c));
                m.Box(new Vector3(s * (wid * 0.5f - 0.1f), cabY + 0.22f, cabZ), new Vector3(0.06f, 0.46f, 0.06f), G(c));
            }
            if (kind == CarKind.Pickup)
            {
                m.Box(new Vector3(0f, bodyY + 0.62f, -1.2f), new Vector3(wid, 0.1f, 1.9f), G(c * 0.9f));
                m.Box(new Vector3(0f, bodyY + 0.6f, -1.2f), new Vector3(wid - 0.2f, 0.02f, 1.8f), M(new Color(0.1f, 0.1f, 0.1f)));
            }
            if (kind == CarKind.Police)
            {
                m.Box(new Vector3(0f, cabY + 0.55f, cabZ + 0.2f), new Vector3(1.0f, 0.1f, 0.22f), M(new Color(0.1f, 0.1f, 0.1f)));
                m.Box(new Vector3(-0.28f, cabY + 0.62f, cabZ + 0.2f), new Vector3(0.38f, 0.08f, 0.18f), Gs(new Color(0.55f, 0.08f, 0.08f)));
                m.Box(new Vector3(0.28f, cabY + 0.62f, cabZ + 0.2f), new Vector3(0.38f, 0.08f, 0.18f), Gs(new Color(0.08f, 0.15f, 0.55f)));
                m.Box(new Vector3(wid * 0.5f + 0.003f, bodyY + 0.35f, 0f), new Vector3(0.004f, 0.3f, 1.8f), M(new Color(0.85f, 0.85f, 0.82f)));
                m.Box(new Vector3(-wid * 0.5f - 0.003f, bodyY + 0.35f, 0f), new Vector3(0.004f, 0.3f, 1.8f), M(new Color(0.85f, 0.85f, 0.82f)));
            }

            // Rust blooms on the doors.
            int rust = rng.Next(0, 4);
            for (int i = 0; i < rust; i++)
            {
                float s = Chance(rng, 0.5) ? 1f : -1f;
                m.Blob(new Vector3(s * (wid * 0.5f + 0.004f), bodyY + R(rng, 0.1f, 0.45f), R(rng, -len * 0.4f, len * 0.4f)),
                    new Vector3(0f, 0f, s), Vector3.up, R(rng, 0.12f, 0.3f), R(rng, 0.08f, 0.18f), 9, 0.4f, rng, M(new Color(0.34f, 0.18f, 0.09f)));
            }
            // Broken glass: a pale crazed patch on the windscreen.
            if (Chance(rng, 0.5))
                m.Blob(new Vector3(R(rng, -0.4f, 0.4f), cabY + 0.25f, cabZ + cabLen * 0.5f + 0.004f), new Vector3(-1f, 0f, 0f), Vector3.up, 0.25f, 0.16f, 10, 0.5f, rng, Gs(new Color(0.42f, 0.46f, 0.48f)));

            // Snow on the roof, hood and trunk.
            m.Box(new Vector3(0f, cabY + 0.53f, cabZ), new Vector3(wid - 0.3f, 0.07f, cabLen - 0.2f), M(SnowWhite));
            m.Box(new Vector3(0f, bodyY + 0.6f, len * 0.5f - 0.6f), new Vector3(wid - 0.3f, 0.05f, 0.9f), M(SnowWhite));
            if (kind != CarKind.Wagon)
                m.Box(new Vector3(0f, bodyY + 0.6f, -len * 0.5f + 0.45f), new Vector3(wid - 0.3f, 0.05f, 0.6f), M(SnowShade));
        }
        #endregion

        #region Trees and snow
        /// <summary>A bare winter tree: tapered trunk, upswept limbs and twigs (grey-brown).</summary>
        public static void BareTree(RaidMesh m, System.Random rng, float height)
        {
            Color bark = V(new Color(0.20f, 0.17f, 0.14f), rng, 0.08f);
            float r0 = R(rng, 0.16f, 0.26f);
            float trunkH = height * R(rng, 0.55f, 0.7f);
            m.Cylinder(Vector3.zero, r0 * 1.3f, 0.3f, 7, M(bark), Quaternion.identity, 0.8f);
            m.Cylinder(Vector3.zero, r0, trunkH, 7, M(bark), Quaternion.identity, 0.45f);
            int limbs = rng.Next(4, 7);
            for (int i = 0; i < limbs; i++)
            {
                float y = trunkH * R(rng, 0.45f, 0.98f);
                float a = R(rng, 0f, Mathf.PI * 2f);
                float up = R(rng, 0.6f, 1.4f);
                var dir = new Vector3(Mathf.Sin(a), up, Mathf.Cos(a)).normalized;
                float l = height * R(rng, 0.25f, 0.42f);
                Vector3 s = new Vector3(0f, y, 0f);
                Vector3 e = s + dir * l;
                m.Rod(s, e, r0 * 0.5f, M(bark), 5);
                int twigs = rng.Next(2, 4);
                for (int t = 0; t < twigs; t++)
                {
                    Vector3 ts = Vector3.Lerp(s, e, R(rng, 0.5f, 1f));
                    float ta = a + R(rng, -1.2f, 1.2f);
                    var td = new Vector3(Mathf.Sin(ta), R(rng, 0.5f, 1.6f), Mathf.Cos(ta)).normalized;
                    m.Rod(ts, ts + td * l * R(rng, 0.35f, 0.6f), r0 * 0.2f, M(bark));
                }
                if (Chance(rng, 0.4)) m.Rod(s + dir * 0.05f + Vector3.up * 0.04f, s + dir * l * 0.6f + Vector3.up * 0.04f, r0 * 0.35f, M(SnowWhite));
            }
            m.Rod(new Vector3(0f, trunkH * 0.95f, 0f), new Vector3(R(rng, -0.4f, 0.4f), height, R(rng, -0.4f, 0.4f)), r0 * 0.35f, M(bark), 5);
        }

        /// <summary>A snow-laden pine: dark stacked cones with pale tips (scenery past the fence).</summary>
        public static void Pine(RaidMesh m, System.Random rng, float height)
        {
            Color needles = V(new Color(0.09f, 0.15f, 0.12f), rng, 0.1f);
            m.Cylinder(Vector3.zero, 0.16f, height * 0.3f, 6, M(new Color(0.18f, 0.13f, 0.1f)));
            int tiers = 4;
            for (int i = 0; i < tiers; i++)
            {
                float y = height * (0.18f + i * 0.19f);
                float r = height * (0.3f - i * 0.06f);
                m.Cylinder(new Vector3(0f, y, 0f), r, height * 0.34f, 8, M(needles), Quaternion.identity, 0.05f);
                m.Cylinder(new Vector3(0f, y + height * 0.2f, 0f), r * 0.42f, height * 0.12f, 8, M(SnowShade), Quaternion.identity, 0.1f);
            }
        }

        /// <summary>A heaped snow pile / drift of the given footprint and height.</summary>
        public static void SnowPile(RaidMesh m, System.Random rng, Vector3 at, float w, float d, float h, float yaw)
        {
            Quaternion q = Yaw(yaw);
            int lumps = rng.Next(2, 5);
            for (int i = 0; i < lumps; i++)
            {
                var off = new Vector3(R(rng, -w * 0.25f, w * 0.25f), 0f, R(rng, -d * 0.25f, d * 0.25f));
                float k = R(rng, 0.6f, 1f);
                m.Ellipsoid(at + q * off, new Vector3(w * k, h * 2f * k, d * k), q * Yaw(R(rng, -20f, 20f)), M(Chance(rng, 0.5) ? SnowWhite : SnowShade), 7, 3);
            }
        }

        /// <summary>Snow drifted against a wall: one smooth bank along <paramref name="length"/> metres of it, deepest in the
        /// middle and tapering to the ends. <paramref name="face"/> is the middle of the stretch on the wall's outer face (y =
        /// ground), <paramref name="along"/> runs along the wall and <paramref name="outward"/> points away from it. The bank's
        /// back stays inside the wall, so none of it can show through into the room.</summary>
        public static void WallDrift(RaidMesh m, System.Random rng, Vector3 face, Vector3 along, Vector3 outward, float length, float depth, float height)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(length / 0.9f));
            float step = length / n;
            Quaternion facing = Quaternion.LookRotation(outward);
            for (int i = 0; i < n; i++)
            {
                float t = -length * 0.5f + step * (i + 0.5f);
                float full = Mathf.Sin(Mathf.PI * (i + 0.5f) / n);
                float dep = depth * (0.55f + 0.45f * full);
                float hgt = height * (0.45f + 0.55f * full);
                Vector3 c = face + along * t + outward * (dep * 0.5f - 0.06f);
                m.Ellipsoid(c, new Vector3(step * 2.2f, hgt * 2f, dep), facing, M(SnowWhite), 8, 3);
            }
        }
        #endregion

        #region Street furniture
        /// <summary>A green steel dumpster with one lid propped open, snow on the other.</summary>
        public static void Dumpster(RaidMesh m, System.Random rng)
        {
            Color c = V(new Color(0.16f, 0.30f, 0.20f), rng, 0.08f);
            m.Box(new Vector3(0f, 0.65f, 0f), new Vector3(1.9f, 1.0f, 1.1f), G(c));
            m.Box(new Vector3(0f, 0.65f, 0.56f), new Vector3(1.8f, 0.9f, 0.02f), M(c * 0.85f));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.98f, 0.5f, 0f), new Vector3(0.08f, 0.16f, 1.0f), M(c * 0.6f));
                m.Cylinder(new Vector3(s * 0.75f, 0f, 0.42f), 0.08f, 0.15f, 6, M(Tyre));
                m.Cylinder(new Vector3(s * 0.75f, 0f, -0.42f), 0.08f, 0.15f, 6, M(Tyre));
            }
            m.Box(new Vector3(-0.47f, 1.17f, 0f), new Vector3(0.92f, 0.04f, 1.12f), M(new Color(0.12f, 0.12f, 0.12f)));
            m.Box(new Vector3(-0.47f, 1.22f, 0f), new Vector3(0.8f, 0.06f, 1.0f), M(SnowWhite));
            Quaternion open = Quaternion.Euler(-70f, 0f, 0f);
            m.Box(new Vector3(0.47f, 1.15f, -0.55f) + open * new Vector3(0f, 0f, 0.56f), new Vector3(0.92f, 0.04f, 1.12f), open, M(new Color(0.12f, 0.12f, 0.12f)));
            m.Ellipsoid(new Vector3(0.45f, 1.12f, 0.1f), new Vector3(0.6f, 0.35f, 0.6f), G(new Color(0.06f, 0.06f, 0.07f)), 8, 4);
        }

        /// <summary>A sandbag wall (length along X) of staggered courses, snow on top.</summary>
        public static void SandbagWall(RaidMesh m, System.Random rng, float len, int courses)
        {
            Color bag = new Color(0.50f, 0.45f, 0.32f);
            const float bw = 0.56f, bh = 0.17f;
            for (int c = 0; c < courses; c++)
            {
                float off = (c & 1) == 0 ? 0f : bw * 0.5f;
                int n = Mathf.Max(1, Mathf.FloorToInt((len - off) / bw));
                for (int i = 0; i < n; i++)
                {
                    float x = -len * 0.5f + off + bw * (i + 0.5f);
                    m.Ellipsoid(new Vector3(x, bh * 0.5f + c * bh * 0.92f, R(rng, -0.03f, 0.03f)), new Vector3(bw * 1.04f, bh * 1.25f, 0.38f), Yaw(R(rng, -6f, 6f)), M(V(bag, rng, 0.1f)), 6, 3);
                }
            }
            m.Box(new Vector3(0f, courses * bh * 0.92f + 0.04f, 0f), new Vector3(len - 0.2f, 0.06f, 0.3f), M(SnowWhite));
        }

        /// <summary>A concrete jersey barrier (length along X), optionally with hazard striping.</summary>
        public static void JerseyBarrier(RaidMesh m, System.Random rng, float len, bool striped)
        {
            Color c = V(new Color(0.60f, 0.60f, 0.56f), rng, 0.05f);
            m.Box(new Vector3(0f, 0.13f, 0f), new Vector3(len, 0.26f, 0.6f), M(c));
            m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(len, 0.4f, 0.36f), M(c));
            m.Box(new Vector3(0f, 0.74f, 0f), new Vector3(len, 0.18f, 0.22f), M(c * 1.03f));
            if (striped) m.Box(new Vector3(0f, 0.5f, 0f), new Vector3(len * 0.98f, 0.16f, 0.37f), DecorMaterials.Hazard);
            m.Box(new Vector3(0f, 0.85f, 0f), new Vector3(len - 0.1f, 0.04f, 0.2f), M(SnowWhite));
        }

        /// <summary>A wooden sawhorse barricade with striped boards.</summary>
        public static void Sawhorse(RaidMesh m, System.Random rng, float len)
        {
            Color leg = new Color(0.55f, 0.45f, 0.30f);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * (len * 0.5f - 0.1f), 0.5f, 0.15f), new Vector3(0.06f, 1.05f, 0.06f), Quaternion.Euler(15f, 0f, 0f), M(leg));
                m.Box(new Vector3(s * (len * 0.5f - 0.1f), 0.5f, -0.15f), new Vector3(0.06f, 1.05f, 0.06f), Quaternion.Euler(-15f, 0f, 0f), M(leg));
            }
            m.Box(new Vector3(0f, 0.95f, 0f), new Vector3(len, 0.2f, 0.04f), DecorMaterials.Hazard);
            m.Box(new Vector3(0f, 0.6f, 0f), new Vector3(len, 0.16f, 0.04f), DecorMaterials.Hazard);
        }

        /// <summary>A cobra-head street light on a 5.5 m pole. Returns the lamp position.</summary>
        public static Vector3 StreetLight(RaidMesh m, System.Random rng, bool lit, float lean)
        {
            Color pole = new Color(0.32f, 0.33f, 0.34f);
            Quaternion tilt = Quaternion.Euler(lean, 0f, 0f);
            m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(0.35f, 0.1f, 0.35f), M(new Color(0.45f, 0.45f, 0.42f)));
            m.Cylinder(Vector3.zero, 0.09f, 5.5f, 8, Mt(pole), tilt, 0.6f);
            Vector3 top = tilt * new Vector3(0f, 5.45f, 0f);
            Vector3 head = top + tilt * new Vector3(0f, 0.15f, 1.5f);
            m.Rod(top, head, 0.07f, Mt(pole));
            m.Box(head + tilt * new Vector3(0f, 0f, 0.2f), new Vector3(0.3f, 0.14f, 0.6f), tilt, M(new Color(0.24f, 0.25f, 0.26f)));
            m.Box(head + tilt * new Vector3(0f, -0.075f, 0.2f), new Vector3(0.24f, 0.01f, 0.48f), tilt, lit ? Gl(new Color(1f, 0.74f, 0.42f)) : Gs(new Color(0.3f, 0.3f, 0.28f)));
            m.Box(head + tilt * new Vector3(0f, 0.08f, 0.2f), new Vector3(0.26f, 0.03f, 0.5f), tilt, M(SnowWhite));
            return head + tilt * new Vector3(0f, -0.2f, 0.2f);
        }

        /// <summary>A 1990s gas pump on its concrete island.</summary>
        public static void GasPump(RaidMesh m, System.Random rng)
        {
            m.Box(new Vector3(0f, 0.08f, 0f), new Vector3(1.0f, 0.16f, 1.6f), M(new Color(0.55f, 0.55f, 0.52f)));
            m.Box(new Vector3(0f, 0.08f, 0f), new Vector3(1.02f, 0.06f, 1.62f), DecorMaterials.Hazard);
            Color body = new Color(0.78f, 0.76f, 0.72f), red = new Color(0.66f, 0.12f, 0.10f);
            m.Box(new Vector3(0f, 0.9f, 0f), new Vector3(0.5f, 1.5f, 0.8f), G(body));
            m.Box(new Vector3(0f, 1.5f, 0f), new Vector3(0.52f, 0.3f, 0.82f), G(red));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.255f, 1.18f, 0f), new Vector3(0.01f, 0.24f, 0.5f), Gs(new Color(0.08f, 0.1f, 0.1f)));
                m.Box(new Vector3(s * 0.258f, 1.2f, 0.1f), new Vector3(0.006f, 0.05f, 0.2f), M(new Color(0.4f, 0.3f, 0.15f)));
                m.Box(new Vector3(s * 0.28f, 0.95f, -0.25f), new Vector3(0.06f, 0.18f, 0.08f), M(new Color(0.1f, 0.1f, 0.1f)));
                m.Rod(new Vector3(s * 0.28f, 0.9f, -0.25f), new Vector3(s * 0.32f, 0.2f, -0.1f), 0.03f, M(new Color(0.08f, 0.08f, 0.08f)));
            }
            m.Box(new Vector3(0f, 1.67f, 0f), new Vector3(0.4f, 0.04f, 0.7f), M(SnowWhite));
        }

        /// <summary>A roadside mailbox on a post, or a blue public mail drop box.</summary>
        public static void Mailbox(RaidMesh m, System.Random rng, bool publicBox)
        {
            if (publicBox)
            {
                Color blue = new Color(0.10f, 0.18f, 0.38f);
                for (int i = 0; i < 4; i++) m.Box(new Vector3(i % 2 == 0 ? -0.2f : 0.2f, 0.1f, i < 2 ? -0.2f : 0.2f), new Vector3(0.05f, 0.2f, 0.05f), M(blue * 0.7f));
                m.Box(new Vector3(0f, 0.65f, 0f), new Vector3(0.5f, 0.9f, 0.5f), G(blue));
                m.CylinderX(new Vector3(0f, 1.1f, 0f), 0.25f, 0.5f, 10, G(blue), 90f);
                m.Box(new Vector3(0f, 0.95f, 0.26f), new Vector3(0.3f, 0.06f, 0.04f), Mt(new Color(0.6f, 0.6f, 0.6f)));
                m.Box(new Vector3(0f, 1.36f, 0f), new Vector3(0.35f, 0.04f, 0.45f), M(SnowWhite));
                return;
            }
            Color post = new Color(0.36f, 0.26f, 0.16f);
            m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(0.09f, 1.1f, 0.09f), M(post));
            Color box = V(Chance(rng, 0.5) ? new Color(0.25f, 0.25f, 0.27f) : new Color(0.55f, 0.55f, 0.56f), rng, 0.1f);
            m.Box(new Vector3(0f, 1.18f, 0.05f), new Vector3(0.22f, 0.16f, 0.48f), Mt(box));
            m.CylinderX(new Vector3(0f, 1.26f, 0.05f), 0.11f, 0.48f, 8, Mt(box), 90f);
            m.Box(new Vector3(0.12f, 1.3f, -0.05f), new Vector3(0.02f, 0.18f, 0.04f), M(new Color(0.7f, 0.12f, 0.1f)));
            m.Box(new Vector3(0f, 1.38f, 0.05f), new Vector3(0.12f, 0.03f, 0.4f), M(SnowWhite));
        }

        /// <summary>A sign board on two posts, facing +Z, its lower edge at <paramref name="bottom"/>.</summary>
        public static void PostSign(RaidMesh m, System.Random rng, string text, Color bg, Color fg, float height, float bottom)
        {
            Surf s = DecorMaterials.Sign(text, bg, fg, out float aspect);
            float w = height * aspect;
            Color post = new Color(0.35f, 0.36f, 0.37f);
            m.Box(new Vector3(-w * 0.4f, (bottom + height) * 0.5f, -0.04f), new Vector3(0.07f, bottom + height, 0.07f), Mt(post));
            m.Box(new Vector3(w * 0.4f, (bottom + height) * 0.5f, -0.04f), new Vector3(0.07f, bottom + height, 0.07f), Mt(post));
            m.Box(new Vector3(0f, bottom + height * 0.5f, 0f), new Vector3(w + 0.06f, height + 0.06f, 0.04f), M(new Color(0.2f, 0.2f, 0.2f)));
            m.Upright(new Vector3(0f, bottom + height * 0.5f, 0.021f), Vector3.forward, w, height, s, new Rect(0f, 0f, 1f, 1f));
            m.Box(new Vector3(0f, bottom + height + 0.05f, 0f), new Vector3(w, 0.04f, 0.06f), M(SnowWhite));
        }

        /// <summary>A hazard placard on a post, facing +Z.</summary>
        public static void HazardSign(RaidMesh m, System.Random rng)
        {
            m.Box(new Vector3(0f, 0.8f, -0.03f), new Vector3(0.07f, 1.6f, 0.07f), Mt(new Color(0.35f, 0.36f, 0.37f)));
            m.Box(new Vector3(0f, 1.3f, 0f), new Vector3(0.66f, 0.66f, 0.03f), M(new Color(0.2f, 0.2f, 0.2f)));
            m.Upright(new Vector3(0f, 1.3f, 0.017f), Vector3.forward, 0.62f, 0.62f, DecorMaterials.HazardSign, new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>A fire hydrant.</summary>
        public static void Hydrant(RaidMesh m, System.Random rng)
        {
            Color red = V(new Color(0.62f, 0.12f, 0.08f), rng, 0.08f);
            m.Cylinder(Vector3.zero, 0.13f, 0.08f, 8, G(red * 0.8f));
            m.Cylinder(new Vector3(0f, 0.08f, 0f), 0.1f, 0.5f, 8, G(red));
            m.Ellipsoid(new Vector3(0f, 0.6f, 0f), new Vector3(0.22f, 0.16f, 0.22f), G(red), 8, 4);
            m.CylinderX(new Vector3(0f, 0.42f, 0f), 0.05f, 0.34f, 6, G(red));
            m.Ellipsoid(new Vector3(0f, 0.68f, 0f), new Vector3(0.18f, 0.08f, 0.18f), M(SnowWhite), 8, 3);
        }

        /// <summary>A portable generator with a frame and exhaust.</summary>
        public static void Generator(RaidMesh m, System.Random rng)
        {
            Color c = new Color(0.30f, 0.33f, 0.22f);
            m.Box(new Vector3(0f, 0.35f, 0f), new Vector3(1.0f, 0.55f, 0.6f), M(c));
            m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(1.1f, 0.1f, 0.7f), M(c * 0.6f));
            m.Box(new Vector3(0.35f, 0.4f, 0.31f), new Vector3(0.2f, 0.25f, 0.02f), M(new Color(0.15f, 0.15f, 0.15f)));
            m.Cylinder(new Vector3(-0.35f, 0.62f, -0.15f), 0.04f, 0.4f, 6, Mt(new Color(0.3f, 0.28f, 0.25f)));
            m.Box(new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 0.04f, 0.5f), M(SnowWhite));
        }

        /// <summary>A black trash bag slumped on the ground: a sagging body, a second lobe and the tied neck.</summary>
        public static void TrashBag(RaidMesh m, System.Random rng, Vector3 at)
        {
            float s = R(rng, 0.85f, 1.15f);
            Quaternion q = Yaw(R(rng, 0f, 360f));
            Surf plastic = G(new Color(0.07f, 0.08f, 0.08f));
            m.Ellipsoid(at + new Vector3(0f, 0.14f * s, 0f), new Vector3(0.62f, 0.32f, 0.5f) * s, q, plastic, 8, 4);
            m.Ellipsoid(at + q * new Vector3(0.12f, 0.24f, 0.03f) * s, new Vector3(0.4f, 0.26f, 0.36f) * s, q, plastic, 7, 3);
            m.Ellipsoid(at + q * new Vector3(0.12f, 0.4f, 0.03f) * s, new Vector3(0.08f, 0.12f, 0.08f) * s, plastic, 6, 3);
        }
        #endregion

        #region Town furniture
        // The Introduction town's own pieces (RaidMapBuilder): its park, its street signs and its fences.

        /// <summary>A park bench, 1.8 m long and facing +Z: cast concrete ends, a slatted wooden seat and back, snow on the seat.</summary>
        public static void Bench(RaidMesh m)
        {
            Surf concrete = M(new Color(0.58f, 0.58f, 0.55f));
            Surf wood = M(new Color(0.42f, 0.30f, 0.20f));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.78f, 0.22f, 0f), new Vector3(0.12f, 0.44f, 0.5f), concrete);
                m.Box(new Vector3(s * 0.78f, 0.62f, -0.2f), new Vector3(0.12f, 0.36f, 0.1f), concrete);
            }
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(0f, 0.46f, 0.16f - i * 0.15f), new Vector3(1.8f, 0.04f, 0.12f), wood);
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(0f, 0.62f + i * 0.16f, -0.24f), new Vector3(1.8f, 0.11f, 0.04f), wood);
            m.Box(new Vector3(0f, 0.49f, 0.02f), new Vector3(1.5f, 0.03f, 0.36f), M(SnowWhite));
        }

        /// <summary>A park lamp: a black post on a fluted base, a frosted globe on top (glowing if <paramref name="lit"/>).
        /// Returns the globe's centre.</summary>
        public static Vector3 ParkLamp(RaidMesh m, bool lit)
        {
            Surf iron = Mt(new Color(0.12f, 0.12f, 0.13f));
            m.Cylinder(Vector3.zero, 0.16f, 0.5f, 8, iron, Quaternion.identity, 0.6f);
            m.Cylinder(new Vector3(0f, 0.5f, 0f), 0.06f, 2.9f, 8, iron);
            m.Cylinder(new Vector3(0f, 3.35f, 0f), 0.12f, 0.08f, 8, iron);
            var globe = new Vector3(0f, 3.62f, 0f);
            m.Ellipsoid(globe, new Vector3(0.44f, 0.5f, 0.44f), lit ? Gl(new Color(1f, 0.86f, 0.62f)) : Gs(new Color(0.62f, 0.64f, 0.66f)), 10, 6);
            m.Cylinder(new Vector3(0f, 3.86f, 0f), 0.07f, 0.06f, 8, iron);
            return globe;
        }

        /// <summary>A flagpole <paramref name="height"/> m tall on a concrete footing, the flag hanging limp from the top.</summary>
        public static void Flagpole(RaidMesh m, float height)
        {
            m.Box(new Vector3(0f, 0.12f, 0f), new Vector3(0.8f, 0.24f, 0.8f), DecorMaterials.Concrete);
            m.Cylinder(new Vector3(0f, 0.24f, 0f), 0.06f, height - 0.24f, 8, Mt(new Color(0.72f, 0.72f, 0.70f)), Quaternion.identity, 0.6f);
            m.Sphere(new Vector3(0f, height + 0.04f, 0f), 0.12f, Mt(new Color(0.70f, 0.60f, 0.32f)));
            // Old Glory, furled by the still cold: a field of stripes with the blue canton at the hoist.
            float top = height - 0.2f;
            for (int i = 0; i < 7; i++)
                m.Box(new Vector3(0.07f, top - 0.08f - i * 0.16f, 0.48f), new Vector3(0.03f, 0.16f, 0.92f),
                    M(i % 2 == 0 ? new Color(0.62f, 0.12f, 0.12f) : new Color(0.86f, 0.85f, 0.80f)));
            m.Box(new Vector3(0.08f, top - 0.36f, 0.22f), new Vector3(0.035f, 0.56f, 0.4f), M(new Color(0.14f, 0.18f, 0.36f)));
        }

        /// <summary>A granite war memorial, front to +Z: an obelisk on a stepped base, a bronze plaque on the front step.
        /// Snow lies on every ledge.</summary>
        public static void Obelisk(RaidMesh m)
        {
            Color granite = new Color(0.50f, 0.50f, 0.49f);
            m.Box(new Vector3(0f, 0.15f, 0f), new Vector3(2.6f, 0.3f, 2.6f), M(granite * 0.9f));
            m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(1.7f, 0.5f, 1.7f), M(granite));
            m.Box(new Vector3(0f, 2.8f, 0f), new Vector3(0.8f, 4.0f, 0.8f), Quaternion.identity, M(granite * 1.05f));
            m.Cylinder(new Vector3(0f, 4.8f, 0f), 0.57f, 0.55f, 4, M(granite * 1.05f), Quaternion.Euler(0f, 45f, 0f), 0f);
            m.Box(new Vector3(0f, 0.6f, 0.86f), new Vector3(0.9f, 0.34f, 0.03f), Mt(new Color(0.40f, 0.30f, 0.16f)));
            m.Box(new Vector3(0f, 0.32f, 0f), new Vector3(2.5f, 0.04f, 2.5f), M(SnowWhite));
            m.Box(new Vector3(0f, 0.82f, 0f), new Vector3(1.6f, 0.05f, 1.6f), M(SnowWhite));
        }

        /// <summary>A STOP sign on its post, the face to +Z.</summary>
        public static void StopSign(RaidMesh m)
        {
            m.Box(new Vector3(0f, 1.15f, -0.05f), new Vector3(0.06f, 2.3f, 0.06f), Mt(new Color(0.55f, 0.56f, 0.57f)));
            // An eight-sided disc spun so its top edge is flat, then tipped to face +Z.
            Quaternion face = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 22.5f, 0f);
            m.Cylinder(new Vector3(0f, 2.05f, -0.01f), 0.4f, 0.02f, 8, M(new Color(0.86f, 0.86f, 0.84f)), face);
            m.Cylinder(new Vector3(0f, 2.05f, 0.01f), 0.37f, 0.01f, 8, M(new Color(0.66f, 0.10f, 0.09f)), face);
            Surf text = DecorMaterials.Sign("STOP", new Color(0.66f, 0.10f, 0.09f), new Color(0.92f, 0.92f, 0.90f), out float aspect);
            m.Upright(new Vector3(0f, 2.05f, 0.022f), Vector3.forward, 0.18f * aspect, 0.18f, text, new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>A road barricade where a street ends, <paramref name="len"/> m long along X and facing +Z: two rails
        /// striped orange and white on square posts, a DEAD END plate on top.</summary>
        public static void Barricade(RaidMesh m, float len)
        {
            Surf post = M(new Color(0.80f, 0.80f, 0.78f));
            int bays = Mathf.Max(1, Mathf.RoundToInt(len / 2.4f));
            for (int i = 0; i <= bays; i++)
                m.Box(new Vector3(-len * 0.5f + 0.15f + (len - 0.3f) * i / bays, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.1f), post);
            m.Box(new Vector3(0f, 1.0f, 0.06f), new Vector3(len, 0.22f, 0.03f), DecorMaterials.Hazard);
            m.Box(new Vector3(0f, 0.6f, 0.06f), new Vector3(len, 0.22f, 0.03f), DecorMaterials.Hazard);
            Surf plate = DecorMaterials.Sign("DEAD END", new Color(0.86f, 0.70f, 0.14f), new Color(0.10f, 0.10f, 0.10f), out float aspect);
            m.Box(new Vector3(0f, 1.42f, 0.04f), new Vector3(0.3f * aspect + 0.04f, 0.34f, 0.02f), M(new Color(0.2f, 0.2f, 0.2f)));
            m.Upright(new Vector3(0f, 1.42f, 0.052f), Vector3.forward, 0.3f * aspect, 0.3f, plate, new Rect(0f, 0f, 1f, 1f));
            m.Box(new Vector3(0f, 1.13f, 0.06f), new Vector3(len - 0.1f, 0.04f, 0.06f), M(SnowWhite));
        }

        /// <summary>A run of picket fence <paramref name="len"/> m long along X, centred on the origin: posts, two rails and
        /// pointed pickets, snow along the top rail.</summary>
        public static void PicketFence(RaidMesh m, float len, Color paint)
        {
            if (len < 0.2f) return;
            Surf s = M(paint);
            int posts = Mathf.Max(1, Mathf.CeilToInt(len / 2.2f));
            for (int i = 0; i <= posts; i++)
                m.Box(new Vector3(-len * 0.5f + len * i / posts, 0.5f, 0f), new Vector3(0.09f, 1.0f, 0.09f), s);
            m.Box(new Vector3(0f, 0.3f, -0.05f), new Vector3(len, 0.07f, 0.03f), s);
            m.Box(new Vector3(0f, 0.75f, -0.05f), new Vector3(len, 0.07f, 0.03f), s);
            int pickets = Mathf.Max(1, Mathf.FloorToInt(len / 0.16f));
            float step = len / pickets;
            for (int i = 0; i < pickets; i++)
            {
                float x = -len * 0.5f + step * (i + 0.5f);
                m.Box(new Vector3(x, 0.44f, -0.08f), new Vector3(0.08f, 0.82f, 0.02f), s);
                m.Box(new Vector3(x, 0.88f, -0.08f), new Vector3(0.056f, 0.056f, 0.02f), Quaternion.Euler(0f, 0f, 45f), s);
            }
            m.Box(new Vector3(0f, 0.8f, -0.05f), new Vector3(len, 0.03f, 0.06f), M(SnowWhite));
        }

        /// <summary>A run of board privacy fence <paramref name="len"/> m long along X, centred on the origin: posts every
        /// 2.4 m, close boards 1.7 m high, a capping rail with snow on it.</summary>
        public static void PrivacyFence(RaidMesh m, float len, Color wood)
        {
            if (len < 0.2f) return;
            int posts = Mathf.Max(1, Mathf.CeilToInt(len / 2.4f));
            for (int i = 0; i <= posts; i++)
                m.Box(new Vector3(-len * 0.5f + len * i / posts, 0.9f, 0.04f), new Vector3(0.1f, 1.8f, 0.1f), M(wood * 0.8f));
            m.Box(new Vector3(0f, 0.86f, 0f), new Vector3(len, 1.7f, 0.04f), DecorMaterials.Planks(wood));
            m.Box(new Vector3(0f, 1.74f, 0f), new Vector3(len + 0.04f, 0.06f, 0.1f), M(wood * 0.85f));
            m.Box(new Vector3(0f, 1.79f, 0f), new Vector3(len, 0.04f, 0.09f), M(SnowWhite));
        }

        /// <summary>A run of chain-link fence <paramref name="len"/> m long along X, centred on the origin: galvanised posts
        /// every 3 m, a top rail, the wires, and the snow banked along its outer side (−Z).</summary>
        public static void ChainLinkFence(RaidMesh m, float len, float height)
        {
            if (len < 0.2f) return;
            Surf steel = Mt(new Color(0.56f, 0.58f, 0.60f));
            int posts = Mathf.Max(1, Mathf.CeilToInt(len / 3f));
            for (int i = 0; i <= posts; i++)
                m.Cylinder(new Vector3(-len * 0.5f + len * i / posts, 0f, 0f), 0.045f, height, 6, steel);
            m.Box(new Vector3(0f, height - 0.03f, 0f), new Vector3(len, 0.05f, 0.05f), steel);
            m.Box(new Vector3(0f, 0.08f, 0f), new Vector3(len, 0.03f, 0.03f), steel);
            Surf wire = Mt(new Color(0.46f, 0.48f, 0.50f));
            for (float y = 0.32f; y < height - 0.15f; y += 0.24f)
                m.Box(new Vector3(0f, y, 0f), new Vector3(len, 0.012f, 0.012f), wire);
            m.Box(new Vector3(0f, height + 0.005f, 0f), new Vector3(len, 0.03f, 0.05f), M(SnowWhite));
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 2.5f));
            for (int i = 0; i < n; i++)
            {
                float x = -len * 0.5f + len * (i + 0.5f) / n;
                m.Ellipsoid(new Vector3(x, 0f, -0.45f), new Vector3(len / n * 1.5f, 0.7f, 1.1f), M(SnowWhite), 8, 3);
            }
        }

        /// <summary>A public litter bin: a green steel drum on a post, a lid with snow on it.</summary>
        public static void LitterBin(RaidMesh m)
        {
            Surf green = G(new Color(0.16f, 0.26f, 0.20f));
            m.Cylinder(Vector3.zero, 0.24f, 0.9f, 10, green);
            m.Cylinder(new Vector3(0f, 0.9f, 0f), 0.27f, 0.06f, 10, Mt(new Color(0.20f, 0.22f, 0.22f)));
            m.Cylinder(new Vector3(0f, 0.96f, 0f), 0.22f, 0.04f, 10, M(SnowWhite));
        }
        #endregion

        #region Litter
        private static readonly Rect Full = new Rect(0f, 0f, 1f, 1f);

        /// <summary>One piece of floor litter at <paramref name="at"/> (y = the surface height): papers,
        /// cardboard, cans, bottles, leaves, cigarette ends, glass or plaster crumbs.</summary>
        public static void Litter(RaidMesh m, System.Random rng, Vector3 at, bool outdoors)
        {
            float yaw = R(rng, 0f, 360f);
            int k = rng.Next(outdoors ? 9 : 10);
            switch (k)
            {
                case 0:
                case 1:
                case 2:
                    m.FlatUV(at + Vector3.up * R(rng, 0.004f, 0.012f), 0.21f, 0.28f, yaw, DecorMaterials.Paper(rng.Next(3)), Full);
                    if (Chance(rng, 0.4)) m.FlatUV(at + new Vector3(R(rng, -0.15f, 0.15f), 0.014f, R(rng, -0.15f, 0.15f)), 0.21f, 0.28f, yaw + R(rng, -40f, 40f), DecorMaterials.Paper(rng.Next(3)), Full);
                    break;
                case 3:
                    m.Flat(at + Vector3.up * 0.006f, R(rng, 0.2f, 0.45f), R(rng, 0.15f, 0.35f), yaw, M(V(FurnitureBuilder.Cardboard, rng, 0.12f)));
                    break;
                case 4:
                {
                    Color can = new[] { new Color(0.65f, 0.12f, 0.1f), new Color(0.6f, 0.6f, 0.62f), new Color(0.15f, 0.3f, 0.55f), new Color(0.75f, 0.65f, 0.2f) }[rng.Next(4)];
                    m.CylinderX(at + Vector3.up * 0.033f, 0.033f, 0.12f, 6, Mt(can), yaw);
                    break;
                }
                case 5:
                {
                    Color glass = Chance(rng, 0.5) ? new Color(0.20f, 0.32f, 0.18f) : new Color(0.36f, 0.22f, 0.10f);
                    if (Chance(rng, 0.7)) m.CylinderX(at + Vector3.up * 0.035f, 0.035f, 0.24f, 6, Gs(glass), yaw);
                    else m.Cylinder(at, 0.035f, 0.24f, 6, Gs(glass), Quaternion.identity, 0.4f);
                    break;
                }
                case 6:
                {
                    int n = rng.Next(3, 8);
                    for (int i = 0; i < n; i++)
                    {
                        var p = at + new Vector3(R(rng, -0.3f, 0.3f), R(rng, 0.004f, 0.01f), R(rng, -0.3f, 0.3f));
                        Color leaf = V(new Color(0.34f, 0.24f, 0.12f), rng, 0.2f);
                        float a = R(rng, 0f, Mathf.PI * 2f);
                        m.Diamond(p, new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a)) * 0.04f, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.07f, M(leaf));
                    }
                    break;
                }
                case 7:
                {
                    float sx = R(rng, 0.25f, 0.7f), sz = R(rng, 0.2f, 0.55f);
                    if (!outdoors) { sx *= 0.65f; sz *= 0.65f; }
                    m.FlatBlob(at + Vector3.up * 0.003f, sx, sz, yaw, 0.35f, rng, M(outdoors ? new Color(0.36f, 0.37f, 0.40f) : new Color(0.24f, 0.20f, 0.15f)));
                    break;
                }
                case 8:
                    if (outdoors) { TrashBag(m, rng, at); break; }
                    for (int i = 0; i < 5; i++)
                        m.Box(at + new Vector3(R(rng, -0.25f, 0.25f), 0.012f, R(rng, -0.25f, 0.25f)), new Vector3(R(rng, 0.03f, 0.1f), 0.024f, R(rng, 0.03f, 0.08f)), Yaw(R(rng, 0f, 90f)), M(new Color(0.72f, 0.70f, 0.64f)));
                    break;
                default:
                    for (int i = 0; i < 4; i++)
                    {
                        var p = at + new Vector3(R(rng, -0.25f, 0.25f), 0.006f, R(rng, -0.25f, 0.25f));
                        float a = R(rng, 0f, Mathf.PI * 2f);
                        m.Diamond(p, new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a)) * R(rng, 0.03f, 0.07f), new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * R(rng, 0.04f, 0.1f), Gs(new Color(0.45f, 0.52f, 0.54f)));
                    }
                    break;
            }
        }
        #endregion
    }
}
