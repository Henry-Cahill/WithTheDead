using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The raid map's furniture, built from boxes, prisms and quads into a <see cref="RaidMesh"/>.
    /// Every piece is drawn around the current local frame origin, standing on y = 0 with its front
    /// facing +Z (its back against a wall at -Z), so a caller sets the frame and draws. Silhouettes
    /// and colours follow the reference photos: steel counter and sink, cream fridge, maroon lockers,
    /// wooden desks, CRT monitors, articulated lamps, stools, filing cabinets, barrels, crates.
    /// </summary>
    internal static partial class FurnitureBuilder
    {
        #region Shared colours and helpers
        public static readonly Color Steel = new Color(0.66f, 0.68f, 0.70f);
        public static readonly Color DarkSteel = new Color(0.22f, 0.23f, 0.25f);
        public static readonly Color Chrome = new Color(0.78f, 0.79f, 0.80f);
        public static readonly Color Black = new Color(0.07f, 0.07f, 0.08f);
        public static readonly Color Oak = new Color(0.46f, 0.31f, 0.18f);
        public static readonly Color Walnut = new Color(0.30f, 0.19f, 0.11f);
        public static readonly Color Pine = new Color(0.62f, 0.49f, 0.32f);
        public static readonly Color Cream = new Color(0.86f, 0.82f, 0.68f);
        public static readonly Color Beige = new Color(0.74f, 0.70f, 0.60f);
        public static readonly Color Maroon = new Color(0.40f, 0.09f, 0.09f);
        public static readonly Color Olive = new Color(0.30f, 0.33f, 0.20f);
        public static readonly Color OliveDark = new Color(0.20f, 0.23f, 0.14f);
        public static readonly Color Cardboard = new Color(0.58f, 0.44f, 0.28f);
        public static readonly Color Paper = new Color(0.86f, 0.85f, 0.80f);
        public static readonly Color Brass = new Color(0.62f, 0.50f, 0.22f);
        public static readonly Color WarmGlow = new Color(1f, 0.86f, 0.62f);
        public static readonly Color ScreenOff = new Color(0.09f, 0.12f, 0.11f);
        public static readonly Color ScreenOn = new Color(0.16f, 0.34f, 0.22f);

        private static Surf M(Color c) => DecorPalette.Matte(c);
        private static Surf G(Color c) => DecorPalette.Gloss(c);
        private static Surf Mt(Color c) => DecorPalette.Metal(c);
        private static Surf Gs(Color c) => DecorPalette.Glass(c);
        private static Surf Gl(Color c) => DecorPalette.Glow(c);

        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        private static bool Chance(System.Random rng, double p) => rng.NextDouble() < p;
        private static Color V(Color c, System.Random rng, float amount = 0.08f) => DecorPalette.Vary(c, amount, rng);

        private static Quaternion Yaw(float deg) => Quaternion.Euler(0f, deg, 0f);
        #endregion

        #region Photo 1: kitchenette, fridge, lockers, desk, radio
        /// <summary>A stainless-steel counter run (length along X, 0.65 deep, 0.92 high), optionally
        /// with a sink and faucet, cupboard doors and a toe kick.</summary>
        public static void SteelCounter(RaidMesh m, System.Random rng, float len, bool sink)
        {
            Color steel = V(Steel, rng, 0.04f);
            Color dark = new Color(0.16f, 0.17f, 0.18f);
            m.Box(new Vector3(0f, 0.05f, 0.02f), new Vector3(len - 0.04f, 0.1f, 0.54f), M(dark));
            m.Box(new Vector3(0f, 0.51f, -0.02f), new Vector3(len, 0.82f, 0.6f), Mt(steel * 0.92f));
            m.Box(new Vector3(0f, 0.94f, 0f), new Vector3(len + 0.02f, 0.04f, 0.66f), Mt(steel));
            m.Box(new Vector3(0f, 1.03f, -0.315f), new Vector3(len + 0.02f, 0.14f, 0.03f), Mt(steel));

            // Cupboard doors: seams and pull handles across the front.
            int doors = Mathf.Max(1, Mathf.RoundToInt(len / 0.5f));
            float dw = len / doors;
            for (int i = 0; i < doors; i++)
            {
                float x = -len * 0.5f + dw * (i + 0.5f);
                if (i > 0) m.Box(new Vector3(-len * 0.5f + dw * i, 0.5f, 0.285f), new Vector3(0.012f, 0.7f, 0.01f), M(dark));
                m.Box(new Vector3(x + (i % 2 == 0 ? dw * 0.3f : -dw * 0.3f), 0.78f, 0.29f), new Vector3(0.025f, 0.12f, 0.025f), Mt(Chrome));
            }
            m.Box(new Vector3(0f, 0.86f, 0.285f), new Vector3(len - 0.02f, 0.012f, 0.01f), M(dark));

            if (sink)
            {
                float sx = len > 1.6f ? -len * 0.25f : 0f;
                // The basin reads as a dark recess in the top with a raised rim.
                m.Box(new Vector3(sx, 0.962f, 0.02f), new Vector3(0.56f, 0.004f, 0.42f), G(new Color(0.20f, 0.21f, 0.22f)));
                m.Box(new Vector3(sx, 0.968f, 0.245f), new Vector3(0.62f, 0.016f, 0.03f), Mt(steel));
                m.Box(new Vector3(sx, 0.968f, -0.205f), new Vector3(0.62f, 0.016f, 0.03f), Mt(steel));
                m.Box(new Vector3(sx - 0.295f, 0.968f, 0.02f), new Vector3(0.03f, 0.016f, 0.44f), Mt(steel));
                m.Box(new Vector3(sx + 0.295f, 0.968f, 0.02f), new Vector3(0.03f, 0.016f, 0.44f), Mt(steel));
                m.Box(new Vector3(sx, 0.963f, 0.03f), new Vector3(0.06f, 0.004f, 0.06f), M(Black));
                // Gooseneck faucet and two taps.
                m.Cylinder(new Vector3(sx, 0.96f, -0.27f), 0.018f, 0.3f, 6, Mt(Chrome));
                m.Box(new Vector3(sx, 1.25f, -0.17f), new Vector3(0.03f, 0.03f, 0.22f), Mt(Chrome));
                m.Box(new Vector3(sx, 1.22f, -0.07f), new Vector3(0.025f, 0.06f, 0.025f), Mt(Chrome));
                m.Box(new Vector3(sx - 0.12f, 1.0f, -0.27f), new Vector3(0.05f, 0.04f, 0.05f), Mt(Chrome));
                m.Box(new Vector3(sx + 0.12f, 1.0f, -0.27f), new Vector3(0.05f, 0.04f, 0.05f), Mt(Chrome));
                if (Chance(rng, 0.6)) m.Cylinder(new Vector3(sx + 0.1f, 0.965f, 0.05f), 0.045f, 0.1f, 8, G(new Color(0.75f, 0.74f, 0.70f)));
            }
        }

        /// <summary>A rounded-off 1990s fridge-freezer, cream by default (0.76 wide, 0.72 deep, 1.72 high).</summary>
        public static void Fridge(RaidMesh m, System.Random rng, Color body)
        {
            Color c = V(body, rng, 0.03f);
            m.Box(new Vector3(0f, 0.04f, -0.02f), new Vector3(0.7f, 0.08f, 0.62f), M(new Color(0.12f, 0.12f, 0.12f)));
            m.Box(new Vector3(0f, 0.9f, -0.04f), new Vector3(0.76f, 1.64f, 0.64f), G(c * 0.95f));
            m.Box(new Vector3(0f, 1.725f, -0.04f), new Vector3(0.72f, 0.03f, 0.6f), G(c));
            // Freezer above, fridge below, a dark seam between.
            m.Box(new Vector3(0f, 1.47f, 0.3f), new Vector3(0.75f, 0.46f, 0.05f), G(c));
            m.Box(new Vector3(0f, 0.66f, 0.3f), new Vector3(0.75f, 1.12f, 0.05f), G(c));
            m.Box(new Vector3(0f, 1.235f, 0.31f), new Vector3(0.74f, 0.012f, 0.04f), M(new Color(0.2f, 0.19f, 0.17f)));
            m.Box(new Vector3(0.31f, 1.4f, 0.345f), new Vector3(0.035f, 0.26f, 0.04f), Mt(Chrome));
            m.Box(new Vector3(0.31f, 1.0f, 0.345f), new Vector3(0.035f, 0.34f, 0.04f), Mt(Chrome));
            // A few magnets and a curling note.
            int notes = rng.Next(1, 4);
            for (int i = 0; i < notes; i++)
            {
                float x = R(rng, -0.25f, 0.15f), y = R(rng, 0.8f, 1.5f);
                bool paper = Chance(rng, 0.6);
                Color nc = paper ? Paper : new Color(R(rng, 0.2f, 0.8f), R(rng, 0.2f, 0.6f), R(rng, 0.1f, 0.5f));
                m.Box(new Vector3(x, y, 0.327f), paper ? new Vector3(0.1f, 0.13f, 0.004f) : new Vector3(0.04f, 0.04f, 0.012f), Yaw(0f) * Quaternion.Euler(0f, 0f, R(rng, -12f, 12f)), M(nc));
            }
            // Add a small metal pull‑handle to the fridge door for period‑correct grip.
            float hx = R(rng, -0.15f, 0.15f);
            float hy = R(rng, 0.9f, 1.2f);
            m.Box(new Vector3(hx, hy, 0.31f), new Vector3(0.02f, 0.07f, 0.005f), Mt(Chrome));
        }

        /// <summary>A bank of tall steel lockers (0.5 m each, 0.5 deep, 1.85 high) with vents, handles
        /// and number plates; one door may hang ajar.</summary>
        public static void LockerBank(RaidMesh m, System.Random rng, int count, Color body)
        {
            float w = count * 0.5f;
            Color c = V(body, rng, 0.05f);
            Color dark = c * 0.45f;
            m.Box(new Vector3(0f, 0.04f, -0.02f), new Vector3(w, 0.08f, 0.44f), M(new Color(0.1f, 0.1f, 0.1f)));
            m.Box(new Vector3(0f, 0.96f, -0.02f), new Vector3(w, 1.76f, 0.46f), M(c * 0.9f));
            m.Box(new Vector3(0f, 1.855f, -0.02f), new Vector3(w + 0.02f, 0.03f, 0.48f), M(c * 0.8f));
            int ajar = Chance(rng, 0.45) ? rng.Next(count) : -1;
            for (int i = 0; i < count; i++)
            {
                float x = -w * 0.5f + 0.25f + i * 0.5f;
                if (i == ajar)
                {
                    // The open door swings out on its left hinge, showing the dark inside.
                    m.Box(new Vector3(x, 0.96f, 0.205f), new Vector3(0.44f, 1.66f, 0.01f), M(new Color(0.05f, 0.05f, 0.05f)));
                    Quaternion open = Yaw(-65f);
                    Vector3 hinge = new Vector3(x - 0.23f, 0f, 0.22f);
                    m.Box(hinge + open * new Vector3(0.23f, 0.96f, 0.012f), new Vector3(0.46f, 1.68f, 0.022f), open, M(c));
                    // handle on the open door (mirrors the closed‑door handle)
                    m.Box(hinge + open * new Vector3(0.23f, 1.25f, 0.022f), new Vector3(0.02f, 0.12f, 0.005f), open, Mt(Chrome));
                    m.Box(new Vector3(x, 1.25f, -0.02f), new Vector3(0.42f, 0.015f, 0.4f), M(c * 0.7f));
                    if (Chance(rng, 0.5)) m.Box(new Vector3(x, 1.32f, -0.05f), new Vector3(0.3f, 0.12f, 0.25f), M(OliveDark));
                    continue;
                }
                m.Box(new Vector3(x, 0.96f, 0.215f), new Vector3(0.46f, 1.68f, 0.02f), M(c));
                // Add a small metal pull-handle on the front of each locker door for visual grip.
                m.Box(new Vector3(x, 1.25f, 0.235f), new Vector3(0.02f, 0.12f, 0.005f), Mt(Chrome));
                for (int v = 0; v < 4; v++)
                {
                    m.Box(new Vector3(x, 1.52f + v * 0.045f, 0.227f), new Vector3(0.26f, 0.014f, 0.006f), M(dark));
                    m.Box(new Vector3(x, 0.24f + v * 0.045f, 0.227f), new Vector3(0.26f, 0.014f, 0.006f), M(dark));
                }
                m.Box(new Vector3(x + 0.16f, 1.0f, 0.235f), new Vector3(0.035f, 0.13f, 0.025f), Mt(Chrome));
                m.Box(new Vector3(x, 1.74f, 0.228f), new Vector3(0.07f, 0.035f, 0.006f), M(Paper));
                if (i > 0) m.Box(new Vector3(x - 0.25f, 0.96f, 0.222f), new Vector3(0.012f, 1.7f, 0.02f), M(dark));
            }
            // Something stowed on top: a helmet, a box or a rolled blanket.
            if (Chance(rng, 0.7))
            {
                float x = R(rng, -w * 0.3f, w * 0.3f);
                int k = rng.Next(3);
                if (k == 0) m.Ellipsoid(new Vector3(x, 1.94f, 0f), new Vector3(0.3f, 0.16f, 0.34f), M(OliveDark), 8, 4);
                else if (k == 1) CardboardBox(m, rng, new Vector3(x, 1.87f, -0.02f), new Vector3(0.42f, 0.28f, 0.36f), R(rng, -10f, 10f));
                else m.CylinderX(new Vector3(x, 1.95f, -0.02f), 0.09f, 0.5f, 8, M(Olive));
            }
        }

        /// <summary>A heavy gun safe (0.8 wide, 0.6 deep, 1.9 high) with a wheel handle and dial.</summary>
        public static void GunSafe(RaidMesh m, System.Random rng)
        {
            Color c = V(new Color(0.20f, 0.23f, 0.21f), rng, 0.05f);
            m.Box(new Vector3(0f, 0.95f, -0.02f), new Vector3(0.8f, 1.9f, 0.56f), M(c));
            m.Box(new Vector3(0f, 0.95f, 0.27f), new Vector3(0.72f, 1.8f, 0.04f), G(c * 1.15f));
            m.Box(new Vector3(0f, 1.8f, 0.3f), new Vector3(0.5f, 0.05f, 0.01f), M(Brass));
            m.Cylinder(new Vector3(0.12f, 1.05f, 0.29f), 0.09f, 0.03f, 10, Mt(Chrome), Quaternion.Euler(90f, 0f, 0f));
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(0.12f, 1.05f, 0.33f), new Vector3(0.24f, 0.02f, 0.02f), Quaternion.Euler(0f, 0f, i * 60f), Mt(Chrome));
            m.Cylinder(new Vector3(-0.12f, 1.25f, 0.29f), 0.05f, 0.03f, 10, M(Black), Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(-0.33f, 0.95f, 0.3f), new Vector3(0.04f, 1.6f, 0.03f), M(c * 0.7f));
        }

        /// <summary>A wooden office desk (width along X, 0.76 high) with a drawer pedestal on one side,
        /// a solid end panel on the other and a modesty panel at the back.</summary>
        public static void Desk(RaidMesh m, System.Random rng, float w, float d, Color wood, bool pedestalRight)
        {
            Color c = V(wood, rng, 0.06f);
            Color top = c * 1.08f;
            float side = pedestalRight ? 1f : -1f;
            m.Box(new Vector3(0f, 0.74f, 0f), new Vector3(w, 0.04f, d), G(top));
            float px = side * (w * 0.5f - 0.22f);
            m.Box(new Vector3(px, 0.36f, -0.01f), new Vector3(0.42f, 0.72f, d - 0.06f), M(c));
            for (int i = 0; i < 3; i++)
            {
                float y = 0.6f - i * 0.23f;
                m.Box(new Vector3(px, y, d * 0.5f - 0.025f), new Vector3(0.38f, 0.2f, 0.02f), M(c * 1.12f));
                m.Box(new Vector3(px, y + 0.03f, d * 0.5f - 0.005f), new Vector3(0.1f, 0.018f, 0.02f), Mt(Brass));
            }
            m.Box(new Vector3(-side * (w * 0.5f - 0.03f), 0.36f, -0.01f), new Vector3(0.04f, 0.72f, d - 0.06f), M(c));
            m.Box(new Vector3(-side * 0.21f, 0.5f, -d * 0.5f + 0.05f), new Vector3(w - 0.5f, 0.42f, 0.02f), M(c * 0.9f));
            // A shallow centre drawer under the top.
            m.Box(new Vector3(-side * 0.21f, 0.68f, d * 0.5f - 0.03f), new Vector3(w - 0.6f, 0.06f, 0.02f), M(c * 1.1f));
        }

        /// <summary>An olive field radio (green "ammo box" radio from the photo) with dials, a handle and
        /// a whip antenna. Sits on a surface at local y = <paramref name="y"/>.</summary>
        public static void FieldRadio(RaidMesh m, System.Random rng, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(new Color(0.26f, 0.32f, 0.18f), rng, 0.05f);
            m.Box(at + q * new Vector3(0f, 0.11f, 0f), new Vector3(0.38f, 0.22f, 0.24f), q, M(c));
            m.Box(at + q * new Vector3(0f, 0.12f, 0.121f), new Vector3(0.32f, 0.16f, 0.004f), q, M(new Color(0.12f, 0.13f, 0.12f)));
            m.Box(at + q * new Vector3(-0.07f, 0.16f, 0.125f), new Vector3(0.12f, 0.04f, 0.004f), q, Gl(new Color(0.55f, 0.45f, 0.20f)));
            for (int i = 0; i < 3; i++)
                m.Cylinder(at + q * new Vector3(0.04f + i * 0.045f, 0.09f, 0.123f), 0.015f, 0.025f, 6, M(Black), q * Quaternion.Euler(90f, 0f, 0f));
            m.Box(at + q * new Vector3(-0.12f, 0.25f, 0f), new Vector3(0.02f, 0.06f, 0.02f), q, M(c * 0.7f));
            m.Box(at + q * new Vector3(0.12f, 0.25f, 0f), new Vector3(0.02f, 0.06f, 0.02f), q, M(c * 0.7f));
            m.Box(at + q * new Vector3(0f, 0.28f, 0f), new Vector3(0.26f, 0.02f, 0.025f), q, M(c * 0.7f));
            m.Rod(at + q * new Vector3(0.15f, 0.22f, -0.08f), at + q * new Vector3(0.2f, 0.85f, -0.16f), 0.008f, M(Black));
            m.Box(at + q * new Vector3(-0.3f, 0.06f, 0.02f), new Vector3(0.12f, 0.04f, 0.2f), q, M(new Color(0.1f, 0.1f, 0.1f)));
        }

        /// <summary>An olive ammo can with a stencilled band and latch handle, on y = at.y.</summary>
        public static void AmmoCan(RaidMesh m, System.Random rng, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(Olive, rng, 0.06f);
            m.Box(at + q * new Vector3(0f, 0.09f, 0f), new Vector3(0.3f, 0.18f, 0.15f), q, M(c));
            m.Box(at + q * new Vector3(0f, 0.185f, 0f), new Vector3(0.31f, 0.012f, 0.16f), q, M(c * 0.8f));
            m.Box(at + q * new Vector3(0f, 0.2f, 0f), new Vector3(0.12f, 0.015f, 0.02f), q, M(c * 0.6f));
            m.Box(at + q * new Vector3(0f, 0.1f, 0.077f), new Vector3(0.2f, 0.03f, 0.004f), q, M(new Color(0.75f, 0.66f, 0.30f)));
        }
        #endregion
    }
}
