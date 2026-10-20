using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        #region The starter home's living room and bedroom
        private static readonly Color LivingBay = new Color(0.06f, 0.045f, 0.035f);       // the dark behind a drawer
        private static readonly Color LivingDrawerInside = new Color(0.70f, 0.58f, 0.42f); // unvarnished deal
        private static readonly Color BedSheet = new Color(0.80f, 0.78f, 0.72f);

        /// <summary>Height of the middle of a <see cref="DresserBody"/> drawer (row 0 the top one).</summary>
        public static float DresserDrawerY(int row) => 0.62f - row * 0.23f;

        /// <summary>A chest of drawers (1.0 wide, 0.46 deep, the top at 0.825; front +Z) without its drawers: the carcass
        /// with a dark bay behind each of the three, a plinth, the top, and a keepsake or two on the top's right-hand end
        /// (local x 0.16..0.46 — the left end is kept clear for a portable TV). Returns the wood colour for the drawers
        /// (<see cref="DresserDrawer"/>).</summary>
        public static Color DresserBody(RaidMesh m, System.Random rng, Color wood)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(0f, 0.41f, -0.03f), new Vector3(1.0f, 0.78f, 0.44f), M(c));
            m.Box(new Vector3(0f, 0.81f, -0.01f), new Vector3(1.04f, 0.03f, 0.5f), G(c * 1.05f));
            // Small brass corner brackets on the top of the dresser
            if (rng.NextDouble() < 0.75) m.Box(new Vector3(-0.5f, 0.825f, -0.24f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            if (rng.NextDouble() < 0.75) m.Box(new Vector3(0.5f, 0.825f, -0.24f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            if (rng.NextDouble() < 0.75) m.Box(new Vector3(-0.5f, 0.825f, 0.24f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            if (rng.NextDouble() < 0.75) m.Box(new Vector3(0.5f, 0.825f, 0.24f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            m.Box(new Vector3(0f, 0.012f, 0.16f), new Vector3(0.98f, 0.024f, 0.06f), M(c * 0.8f));
            for (int row = 0; row < 3; row++)
                m.Box(new Vector3(0f, DresserDrawerY(row), 0.1915f), new Vector3(0.92f, 0.19f, 0.003f), M(LivingBay));

            int items = rng.Next(1, 3);
            for (int i = 0; i < items; i++)
            {
                var at = new Vector3(i == 0 ? 0.36f : 0.22f, 0.825f, R(rng, -0.12f, 0.06f));
                Quaternion q = Yaw(R(rng, -25f, 20f));
                switch (rng.Next(4))
                {
                    case 0:
                    {
                        // A framed photograph propped on its stand.
                        Quaternion lean = q * Quaternion.Euler(-12f, 0f, 0f);
                        m.Box(at + lean * new Vector3(0f, 0.09f, 0f), new Vector3(0.13f, 0.17f, 0.014f), lean, M(new Color(0.20f, 0.13f, 0.08f)));
                        m.Box(at + lean * new Vector3(0f, 0.09f, 0.008f), new Vector3(0.095f, 0.125f, 0.004f), lean, M(V(new Color(0.62f, 0.52f, 0.40f), rng, 0.1f)));
                        break;
                    }
                    case 1:
                        // A lacquered keepsake box.
                        m.Box(at + new Vector3(0f, 0.035f, 0f), new Vector3(0.14f, 0.07f, 0.1f), q, G(V(new Color(0.42f, 0.12f, 0.10f), rng, 0.1f)));
                        m.Box(at + new Vector3(0f, 0.072f, 0f), new Vector3(0.145f, 0.008f, 0.105f), q, Mt(Brass));
                        break;
                    case 2:
                        // A scent bottle and a hairbrush.
                        m.Cylinder(at + q * new Vector3(-0.03f, 0f, 0.03f), 0.024f, 0.08f, 8, Gs(new Color(0.70f, 0.55f, 0.62f)));
                        m.Cylinder(at + q * new Vector3(-0.03f, 0.08f, 0.03f), 0.01f, 0.02f, 6, Mt(Brass));
                        m.Box(at + q * new Vector3(0.04f, 0.015f, -0.02f), new Vector3(0.045f, 0.03f, 0.17f), q, G(new Color(0.30f, 0.20f, 0.12f)));
                        break;
                    default:
                        // A folded jumper.
                        m.Box(at + new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.06f, 0.22f), q, M(V(new Color(0.42f, 0.46f, 0.58f), rng, 0.3f)));
                        break;
                }
            }
            return c;
        }

        /// <summary>One drawer of <see cref="DresserBody"/> (row 0 the top), drawn shut: its front with two brass knobs,
        /// and the open box behind it that shows once it's pulled out.</summary>
        public static void DresserDrawer(RaidMesh m, int row, Color wood)
        {
            float y = DresserDrawerY(row);
            m.Box(new Vector3(0f, y, 0.205f), new Vector3(0.94f, 0.2f, 0.02f), M(wood * 1.1f));
            m.Box(new Vector3(-0.25f, y, 0.222f), new Vector3(0.04f, 0.04f, 0.016f), Mt(Brass));
            m.Box(new Vector3(0.25f, y, 0.222f), new Vector3(0.04f, 0.04f, 0.016f), Mt(Brass));
            m.Box(new Vector3(0f, y + 0.1f, 0.215f), new Vector3(0.94f, 0.008f, 0.004f), Mt(Brass));
            LivingDrawerBox(m, new Vector3(0f, y - 0.085f, 0.195f), 0.9f, 0.15f, 0.36f, wood);
        }

        /// <summary>A bedside table (0.45 wide, 0.4 deep, 0.6 high; front +Z) without its drawer: the body with a dark bay
        /// for the drawer, an open cubby below it with a few paperbacks, an alarm clock, and the bedside lamp with its
        /// fabric shade. Returns the lamp's bulb (local); <paramref name="body"/> is the wood colour for the drawer.</summary>
        public static Vector3 BedsideTable(RaidMesh m, System.Random rng, Color wood, out Color body)
        {
            Color c = V(wood, rng, 0.06f);
            body = c;
            m.Box(new Vector3(0f, 0.3f, -0.025f), new Vector3(0.45f, 0.6f, 0.39f), M(c));
            m.Box(new Vector3(0f, 0.4f, 0.1715f), new Vector3(0.37f, 0.14f, 0.003f), M(LivingBay));
            m.Box(new Vector3(0f, 0.17f, 0.1715f), new Vector3(0.37f, 0.22f, 0.003f), M(LivingBay));
            int spines = rng.Next(2, 5);
            float x = -0.16f;
            for (int i = 0; i < spines; i++)
            {
                float w = R(rng, 0.025f, 0.045f), h = R(rng, 0.13f, 0.18f);
                m.Box(new Vector3(x + w * 0.5f, 0.06f + h * 0.5f, 0.174f), new Vector3(w, h, 0.004f), M(V(new Color(0.45f, 0.25f, 0.20f), rng, 0.4f)));
                x += w + 0.004f;
            }

            // The alarm clock, angled toward the pillow.
            Quaternion q = Yaw(-20f);
            var clock = new Vector3(0.15f, 0.6f, 0.07f);
            m.Box(clock + q * new Vector3(0f, 0.045f, 0f), new Vector3(0.1f, 0.09f, 0.05f), q, G(V(new Color(0.55f, 0.12f, 0.10f), rng, 0.1f)));
            m.Box(clock + q * new Vector3(0f, 0.05f, 0.026f), new Vector3(0.07f, 0.06f, 0.004f), q, M(new Color(0.92f, 0.90f, 0.82f)));
            m.Box(clock + q * new Vector3(0f, 0.052f, 0.029f), new Vector3(0.004f, 0.025f, 0.002f), q, M(Black));

            // The lamp, as on every bedside table in town.
            m.Cylinder(new Vector3(0f, 0.6f, -0.04f), 0.07f, 0.04f, 8, G(new Color(0.6f, 0.5f, 0.35f)));
            m.Cylinder(new Vector3(0f, 0.64f, -0.04f), 0.02f, 0.22f, 6, Mt(Brass));
            m.Cylinder(new Vector3(0f, 0.84f, -0.04f), 0.15f, 0.2f, 10, M(new Color(0.78f, 0.70f, 0.55f)), Quaternion.identity, 0.65f, false);
            // Tiny brass switch knob on the shade
            m.Cylinder(new Vector3(0.07f, 0.84f, -0.04f), 0.01f, 0.03f, 6, Mt(Brass));
            return new Vector3(0f, 0.9f, -0.04f);
        }

        /// <summary>The drawer of <see cref="BedsideTable"/>, drawn shut: its front and knob and the box behind.</summary>
        public static void BedsideDrawer(RaidMesh m, Color wood)
        {
            m.Box(new Vector3(0f, 0.4f, 0.185f), new Vector3(0.39f, 0.16f, 0.02f), M(wood * 1.1f));
            m.Box(new Vector3(0f, 0.4f, 0.2f), new Vector3(0.04f, 0.03f, 0.02f), Mt(Brass));
            LivingDrawerBox(m, new Vector3(0f, 0.335f, 0.175f), 0.35f, 0.11f, 0.3f, wood);
        }

        // The open-topped box of a drawer, hanging back from the inside of its front: front is the bottom's front edge.
        private static void LivingDrawerBox(RaidMesh m, Vector3 front, float w, float h, float d, Color wood)
        {
            Color side = wood * 0.95f;
            float zc = front.z - d * 0.5f;
            m.Box(new Vector3(front.x, front.y + 0.006f, zc), new Vector3(w - 0.02f, 0.012f, d), M(LivingDrawerInside));
            m.Box(new Vector3(front.x - w * 0.5f + 0.006f, front.y + h * 0.5f, zc), new Vector3(0.012f, h, d), M(side));
            m.Box(new Vector3(front.x + w * 0.5f - 0.006f, front.y + h * 0.5f, zc), new Vector3(0.012f, h, d), M(side));
            m.Box(new Vector3(front.x, front.y + h * 0.5f, front.z - d + 0.006f), new Vector3(w, h, 0.012f), M(side));
        }

        /// <summary>A deep fabric armchair (0.95 wide, 0.88 deep, 0.92 high; front +Z): a low seat cushion (top at 0.5),
        /// a padded back leaning back a little with a lace antimacassar over its top, and rolled arms.</summary>
        public static void Armchair(RaidMesh m, System.Random rng, Color fabric)
        {
            Color c = V(fabric, rng, 0.06f);
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3((i % 2 == 0 ? -1f : 1f) * 0.39f, 0.04f, (i < 2 ? -1f : 1f) * 0.34f), new Vector3(0.05f, 0.08f, 0.05f), M(Walnut));
            m.Box(new Vector3(0f, 0.24f, -0.02f), new Vector3(0.95f, 0.32f, 0.82f), M(c * 0.9f));
            m.Box(new Vector3(0f, 0.445f, 0.06f), new Vector3(0.55f, 0.11f, 0.62f), M(V(c, rng, 0.04f)));
            m.Box(new Vector3(0f, 0.66f, -0.31f), new Vector3(0.59f, 0.52f, 0.2f), Quaternion.Euler(-10f, 0f, 0f), M(c));
            m.Box(new Vector3(0f, 0.7f, -0.19f), new Vector3(0.53f, 0.4f, 0.09f), Quaternion.Euler(-12f, 0f, 0f), M(V(c, rng, 0.04f)));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.375f, 0.51f, -0.02f), new Vector3(0.2f, 0.22f, 0.82f), M(c * 0.95f));
                m.CylinderX(new Vector3(s * 0.375f, 0.62f, -0.02f), 0.1f, 0.82f, 8, M(c * 0.95f), 90f);
            }
            m.Box(new Vector3(0f, 0.925f, -0.355f), new Vector3(0.34f, 0.012f, 0.16f), Quaternion.Euler(-10f, 0f, 0f), M(new Color(0.92f, 0.90f, 0.84f)));
        }

        /// <summary>A small portable CRT television (0.44 wide, 0.4 deep, 0.4 high to its handle, rabbit ears above;
        /// front +Z) standing at the origin: a cabinet with the tube bulging out behind, a dark screen in its bezel, a
        /// channel dial, a volume knob and a speaker grille down the right, a carrying handle and a pair of telescopic
        /// aerials. Returns the screen's centre (local, on the glass); <paramref name="screen"/> is its size.</summary>
        public static Vector3 PortableTv(RaidMesh m, System.Random rng, out Vector2 screen)
        {
            bool wood = Chance(rng, 0.4);
            Color cab = wood ? V(new Color(0.36f, 0.23f, 0.13f), rng, 0.06f) : V(new Color(0.15f, 0.15f, 0.16f), rng, 0.05f);
            Color face = wood ? new Color(0.60f, 0.60f, 0.58f) : new Color(0.24f, 0.24f, 0.25f);
            Color black = new Color(0.05f, 0.05f, 0.06f);

            for (int i = 0; i < 4; i++)
                m.Box(new Vector3((i % 2 == 0 ? -1f : 1f) * 0.17f, 0.008f, (i < 2 ? -0.02f : 0.12f)), new Vector3(0.04f, 0.016f, 0.04f), M(black));
            m.Box(new Vector3(0f, 0.19f, 0.06f), new Vector3(0.44f, 0.34f, 0.22f), wood ? M(cab) : G(cab));
            m.Box(new Vector3(0f, 0.18f, -0.10f), new Vector3(0.36f, 0.27f, 0.10f), G(cab * 0.92f));
            m.Box(new Vector3(0f, 0.17f, -0.18f), new Vector3(0.22f, 0.17f, 0.06f), G(cab * 0.85f));
            m.Box(new Vector3(0f, 0.19f, 0.171f), new Vector3(0.42f, 0.32f, 0.004f), M(face));

            var centre = new Vector3(-0.045f, 0.2f, 0.1775f);
            screen = new Vector2(0.27f, 0.21f);
            m.Box(new Vector3(centre.x, centre.y, 0.1745f), new Vector3(0.31f, 0.25f, 0.004f), M(black));
            m.Box(new Vector3(centre.x, centre.y, 0.1755f), new Vector3(screen.x, screen.y, 0.004f), Gs(ScreenOff));

            const float cx = 0.155f;
            m.Cylinder(new Vector3(cx, 0.27f, 0.173f), 0.032f, 0.022f, 10, M(black), Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(cx, 0.27f, 0.197f), new Vector3(0.008f, 0.05f, 0.006f), M(face * 1.3f));
            m.Cylinder(new Vector3(cx, 0.19f, 0.173f), 0.02f, 0.018f, 8, M(black), Quaternion.Euler(90f, 0f, 0f));
            for (int i = 0; i < 5; i++)
                m.Box(new Vector3(cx, 0.06f + i * 0.018f, 0.1745f), new Vector3(0.07f, 0.007f, 0.004f), M(black));
            m.Box(new Vector3(centre.x, 0.055f, 0.1745f), new Vector3(0.1f, 0.012f, 0.003f), Mt(Chrome));

            m.Box(new Vector3(-0.16f, 0.375f, 0.09f), new Vector3(0.02f, 0.03f, 0.03f), M(black));
            m.Box(new Vector3(0.16f, 0.375f, 0.09f), new Vector3(0.02f, 0.03f, 0.03f), M(black));
            m.Box(new Vector3(0f, 0.395f, 0.09f), new Vector3(0.34f, 0.018f, 0.035f), M(black));

            var ears = new Vector3(0f, 0.36f, -0.02f);
            m.Ellipsoid(ears + new Vector3(0f, 0.012f, 0f), new Vector3(0.08f, 0.03f, 0.05f), M(black), 6, 3);
            m.Rod(ears + Vector3.up * 0.02f, ears + new Vector3(-0.2f, 0.36f, -0.05f), 0.007f, Mt(Chrome));
            m.Rod(ears + Vector3.up * 0.02f, ears + new Vector3(0.22f, 0.33f, -0.03f), 0.007f, Mt(Chrome));
            return centre;
        }

        /// <summary>The lit picture over a <see cref="PortableTv"/>'s screen (drawn white: the set tints it per channel).</summary>
        public static void TvScreenGlow(RaidMesh m, Vector3 centre, Vector2 size)
            => m.Upright(centre + new Vector3(0f, 0f, 0.002f), Vector3.forward, size.x - 0.01f, size.y - 0.01f, Gl(Color.white));

        /// <summary>A compact kitchen radio (0.3 wide, 0.12 deep, 0.2 high to its handle; front +Z) standing at the origin:
        /// a plastic case, a speaker grille, a tuning dial window with its needle, tuning and volume knobs, a chrome carrying
        /// handle and a telescopic aerial. Returns the dial window's centre (local, on the glass); <paramref name="dial"/>
        /// is its size.</summary>
        public static Vector3 KitchenRadio(RaidMesh m, System.Random rng, out Vector2 dial)
        {
            int kind = rng.Next(3);
            Color body = kind == 0 ? new Color(0.86f, 0.82f, 0.70f) : kind == 1 ? new Color(0.45f, 0.30f, 0.17f) : new Color(0.62f, 0.17f, 0.13f);
            body = V(body, rng, 0.05f);
            Color dark = new Color(0.10f, 0.09f, 0.08f);

            m.Box(new Vector3(0f, 0.005f, 0f), new Vector3(0.26f, 0.01f, 0.1f), M(dark));
            m.Box(new Vector3(0f, 0.085f, 0f), new Vector3(0.3f, 0.15f, 0.12f), G(body));
            for (int s = -1; s <= 1; s += 2)
                m.Box(new Vector3(s * 0.151f, 0.085f, 0f), new Vector3(0.006f, 0.152f, 0.122f), G(body * 0.8f));

            m.Box(new Vector3(-0.07f, 0.085f, 0.0615f), new Vector3(0.13f, 0.11f, 0.003f), M(dark));
            for (int i = 0; i < 6; i++)
                m.Box(new Vector3(-0.07f, 0.04f + i * 0.018f, 0.0635f), new Vector3(0.12f, 0.006f, 0.003f), Mt(Chrome * 0.8f));

            var centre = new Vector3(0.07f, 0.118f, 0.064f);
            dial = new Vector2(0.11f, 0.035f);
            m.Box(new Vector3(centre.x, centre.y, 0.0615f), new Vector3(dial.x + 0.01f, dial.y + 0.01f, 0.003f), Mt(Chrome));
            m.Box(new Vector3(centre.x, centre.y, 0.0625f), new Vector3(dial.x, dial.y, 0.003f), Gs(new Color(0.80f, 0.74f, 0.58f)));
            for (int i = 0; i < 7; i++)
                m.Box(new Vector3(centre.x - 0.045f + i * 0.015f, centre.y - 0.008f, 0.0652f), new Vector3(0.002f, 0.008f, 0.001f), M(dark));
            m.Box(new Vector3(centre.x + R(rng, -0.03f, 0.03f), centre.y, 0.0665f), new Vector3(0.003f, dial.y - 0.004f, 0.002f), M(new Color(0.75f, 0.12f, 0.08f)));

            m.Cylinder(new Vector3(0.045f, 0.05f, 0.06f), 0.016f, 0.018f, 8, M(dark), Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(new Vector3(0.105f, 0.05f, 0.06f), 0.02f, 0.02f, 8, M(dark), Quaternion.Euler(90f, 0f, 0f));

            m.Box(new Vector3(-0.12f, 0.175f, 0f), new Vector3(0.012f, 0.03f, 0.012f), Mt(Chrome));
            m.Box(new Vector3(0.12f, 0.175f, 0f), new Vector3(0.012f, 0.03f, 0.012f), Mt(Chrome));
            m.Box(new Vector3(0f, 0.192f, 0f), new Vector3(0.252f, 0.012f, 0.014f), Mt(Chrome));

            m.Rod(new Vector3(0.13f, 0.16f, -0.045f), new Vector3(0.19f, 0.46f, -0.13f), 0.006f, Mt(Chrome));
            m.Ellipsoid(new Vector3(0.19f, 0.46f, -0.13f), new Vector3(0.012f, 0.012f, 0.012f), Mt(Chrome), 5, 3);
            return centre;
        }

        /// <summary>The warm glow of a <see cref="KitchenRadio"/>'s dial while it's on (under its ticks and needle).</summary>
        public static void RadioDialGlow(RaidMesh m, Vector3 centre, Vector2 size)
            => m.Upright(centre + new Vector3(0f, 0f, 0.0003f), Vector3.forward, size.x - 0.004f, size.y - 0.004f, Gl(new Color(1f, 0.74f, 0.38f)));

        /// <summary>A bed's frame and mattress (width along X, length along Z, head at -Z) with its headboard, footboard
        /// and pillows: everything but the bedding, which <see cref="BedQuilt"/> draws apart so it can be made or rumpled.</summary>
        public static void BedFrame(RaidMesh m, System.Random rng, float w, float l, Color wood)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(0f, 0.2f, 0f), new Vector3(w, 0.22f, l), M(c));
            m.Box(new Vector3(0f, 0.55f, -l * 0.5f + 0.03f), new Vector3(w + 0.06f, 1.1f, 0.06f), M(c * 1.05f));
            m.Box(new Vector3(0f, 1.08f, -l * 0.5f + 0.03f), new Vector3(w + 0.1f, 0.06f, 0.09f), M(c * 0.9f));
            m.Box(new Vector3(0f, 0.3f, l * 0.5f - 0.025f), new Vector3(w + 0.06f, 0.6f, 0.05f), M(c * 1.05f));
            m.Box(new Vector3(0f, 0.41f, -0.01f), new Vector3(w - 0.06f, 0.2f, l - 0.1f), M(BedSheet));
            int pillows = w > 1.2f ? 2 : 1;
            for (int i = 0; i < pillows; i++)
            {
                float x = pillows == 1 ? 0f : (i == 0 ? -w * 0.24f : w * 0.24f);
                m.Ellipsoid(new Vector3(x, 0.56f, -l * 0.5f + 0.3f), new Vector3(Mathf.Min(0.55f, w * 0.42f), 0.13f, 0.34f), Yaw(R(rng, -8f, 8f)),
                    M(new Color(0.84f, 0.83f, 0.78f)), 8, 4);
            }
        }

        /// <summary>The bedding over a <see cref="BedFrame"/>: made (the quilt pulled up smooth to just below the pillows,
        /// the sheet turned down over its top edge, the sides hanging over the mattress) or rumpled (thrown back toward
        /// the foot in a heap, the sheet creased where someone lay).</summary>
        public static void BedQuilt(RaidMesh m, System.Random rng, float w, float l, Color blanket, bool made)
        {
            Color b = V(blanket, rng, 0.05f);
            if (made)
            {
                float head = -l * 0.5f + 0.52f, foot = l * 0.5f - 0.06f;
                float len = foot - head, zc = (head + foot) * 0.5f;
                m.Box(new Vector3(0f, 0.535f, zc), new Vector3(w + 0.02f, 0.05f, len), M(b));
                m.Box(new Vector3(-w * 0.5f - 0.01f, 0.39f, zc), new Vector3(0.03f, 0.32f, len), M(b * 0.9f));
                m.Box(new Vector3(w * 0.5f + 0.01f, 0.39f, zc), new Vector3(0.03f, 0.32f, len), M(b * 0.9f));
                m.Box(new Vector3(0f, 0.565f, head + 0.09f), new Vector3(w + 0.03f, 0.03f, 0.18f), M(BedSheet * 1.06f));
                return;
            }

            float bl = l * 0.45f;
            float bz = l * 0.5f - 0.06f - bl * 0.5f;
            float yaw = R(rng, -6f, 6f);
            m.Box(new Vector3(0f, 0.535f, bz), new Vector3(w + 0.02f, 0.05f, bl), Yaw(yaw), M(b));
            m.Box(new Vector3(-w * 0.5f - 0.01f, 0.39f, bz), new Vector3(0.03f, 0.3f, bl), M(b * 0.9f));
            m.Box(new Vector3(w * 0.5f + 0.01f, 0.39f, bz), new Vector3(0.03f, 0.3f, bl), M(b * 0.9f));
            m.CylinderX(new Vector3(0.03f, 0.6f, bz - bl * 0.5f + 0.06f), 0.08f, w * 0.95f, 8, M(b * 1.06f), yaw);
            m.Ellipsoid(new Vector3(R(rng, -0.25f, 0.25f), 0.6f, bz + 0.05f), new Vector3(w * 0.5f, 0.12f, 0.4f), M(b * 1.04f), 8, 4);
            m.Ellipsoid(new Vector3(R(rng, -0.2f, 0.2f), 0.51f, -0.2f), new Vector3(0.5f, 0.03f, 0.9f), Yaw(R(rng, -10f, 10f)), M(BedSheet * 0.95f), 8, 3);
        }
        #endregion
    }
}
