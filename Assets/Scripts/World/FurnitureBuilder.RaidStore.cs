using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        // The county office's, the food mart's and the quarantine site's working furniture, drawn as a fixed body plus
        // the parts that move (a copier's lid, a cash drawer, a cooler's glass door, a safe's door, a drum's lid, a
        // dumpster's lid, a guard booth's hatch) or light up (a jammed copier's warning lamp, a water heater's pilot, a
        // generator's run lamp). Each piece is drawn in its own frame like the rest of the furniture (front toward +Z,
        // origin on the floor at its middle); the constants are shared with the fixtures that work them.

        #region Office: copier
        /// <summary>The copier's lid hinge (its back edge, under the lid).</summary>
        internal static readonly Vector3 OfficeCopierLidHinge = new Vector3(0f, 1.04f, -0.28f);
        /// <summary>The control panel at the front right of the deck, where the survivor reaches.</summary>
        internal static readonly Vector3 OfficeCopierPanel = new Vector3(0.28f, 1.03f, 0.34f);
        /// <summary>The red warning lamp beside the ready lamp.</summary>
        internal static readonly Vector3 OfficeCopierJamLamp = new Vector3(0.36f, 1.035f, 0.325f);

        /// <summary>A beige photocopier's body (0.9 × 1.07 × 0.6): the cabinet with its paper trays, the scanner deck with
        /// its glass (and the last original left on it), the output tray and the control panel. The lid is
        /// <see cref="OfficeCopierLid"/>.</summary>
        public static void OfficeCopierBody(RaidMesh m, System.Random rng)
        {
            Color c = V(Beige, rng, 0.04f);
            m.Box(new Vector3(0f, 0.04f, 0f), new Vector3(0.86f, 0.08f, 0.56f), M(DarkSteel));
            m.Box(new Vector3(0f, 0.49f, 0f), new Vector3(0.9f, 0.82f, 0.6f), M(c * 0.92f));
            for (int i = 0; i < 2; i++)
            {
                float y = 0.26f + i * 0.3f;
                m.Box(new Vector3(0f, y, 0.302f), new Vector3(0.84f, 0.26f, 0.006f), M(c * 0.97f));
                m.Box(new Vector3(0f, y + 0.08f, 0.307f), new Vector3(0.2f, 0.03f, 0.006f), M(c * 0.6f));
            }
            m.Box(new Vector3(0f, 0.97f, 0f), new Vector3(0.86f, 0.14f, 0.58f), M(c));
            m.Box(new Vector3(0f, 1.0405f, -0.02f), new Vector3(0.62f, 0.002f, 0.42f), Gs(new Color(0.14f, 0.18f, 0.2f)));
            m.FlatUV(new Vector3(-0.1f, 1.0435f, -0.04f), 0.21f, 0.28f, 90f + R(rng, -6f, 6f), DecorMaterials.Paper(rng.Next(3)), new Rect(0f, 0f, 1f, 1f));
            // The output tray on the left, a sheet or two in it.
            m.Box(new Vector3(-0.49f, 0.73f, 0f), new Vector3(0.08f, 0.02f, 0.4f), M(c * 0.8f));
            m.Box(new Vector3(-0.52f, 0.75f, 0f), new Vector3(0.14f, 0.02f, 0.36f), M(Paper));
            // The control panel: a sloped strip of keys, the green ready lamp, the lamp-housing for the red one.
            m.Box(new Vector3(0.28f, 1.02f, 0.3f), new Vector3(0.26f, 0.05f, 0.04f), M(c * 0.85f));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(0.2f + i * 0.04f, 1.047f, 0.3f), new Vector3(0.025f, 0.008f, 0.02f), M(new Color(0.3f, 0.3f, 0.32f)));
            m.Box(new Vector3(0.36f, 1.035f, 0.32f), new Vector3(0.05f, 0.026f, 0.004f), M(Black));
            m.Box(new Vector3(0.22f, 1.03f, 0.321f), new Vector3(0.05f, 0.02f, 0.004f), Gl(new Color(0.2f, 0.62f, 0.32f)));
        }

        /// <summary>The copier's lid (drawn shut), hinged at its back edge (<see cref="OfficeCopierLidHinge"/>).</summary>
        public static void OfficeCopierLid(RaidMesh m)
        {
            m.Box(new Vector3(0f, 1.056f, -0.02f), new Vector3(0.8f, 0.03f, 0.52f), M(new Color(0.25f, 0.25f, 0.27f)));
            m.Box(new Vector3(0f, 1.075f, 0.22f), new Vector3(0.24f, 0.012f, 0.03f), M(new Color(0.16f, 0.16f, 0.17f)));
        }

        /// <summary>The copier's red warning lamp, lit while it's jammed (drawn round its own origin).</summary>
        public static void OfficeCopierJamGlow(RaidMesh m) => m.Box(Vector3.zero, new Vector3(0.044f, 0.022f, 0.004f), Gl(new Color(0.95f, 0.18f, 0.12f)));
        #endregion

        #region Office: water cooler
        /// <summary>Where the cooler's jug sits on the cabinet, and how tall it is.</summary>
        internal const float OfficeJugBase = 0.94f, OfficeJugHeight = 0.42f;
        /// <summary>The cooler's taps, where the survivor reaches.</summary>
        internal static readonly Vector3 OfficeCoolerTaps = new Vector3(0f, 0.79f, 0.2f);

        /// <summary>A bottled-water cooler (0.32 × 1.36 × 0.32): the cabinet, the tap recess with its blue and red taps and
        /// drip tray, a paper-cup dispenser, and the jug's clear empty shell — the water inside it is
        /// <see cref="OfficeJugWater"/>.</summary>
        public static void OfficeWaterCoolerBody(RaidMesh m, System.Random rng)
        {
            Color c = V(Beige, rng, 0.05f);
            m.Box(new Vector3(0f, 0.47f, 0f), new Vector3(0.32f, 0.94f, 0.32f), G(c));
            m.Box(new Vector3(0f, 0.75f, 0.162f), new Vector3(0.16f, 0.1f, 0.01f), M(new Color(0.15f, 0.15f, 0.16f)));
            m.Box(new Vector3(-0.04f, 0.785f, 0.175f), new Vector3(0.03f, 0.03f, 0.03f), G(new Color(0.18f, 0.32f, 0.70f)));
            m.Box(new Vector3(0.04f, 0.785f, 0.175f), new Vector3(0.03f, 0.03f, 0.03f), G(new Color(0.72f, 0.14f, 0.10f)));
            m.Box(new Vector3(0f, 0.706f, 0.18f), new Vector3(0.14f, 0.012f, 0.05f), Mt(new Color(0.45f, 0.45f, 0.46f)));
            // Add chrome tap-knobs above the taps
            m.Cylinder(new Vector3(-0.04f, 0.795f, 0.185f), 0.005f, 0.015f, 6, Mt(Chrome));
            m.Cylinder(new Vector3(0.04f, 0.795f, 0.185f), 0.005f, 0.015f, 6, Mt(Chrome));
            m.Cylinder(new Vector3(0.195f, 0.48f, 0.02f), 0.035f, 0.24f, 8, M(new Color(0.9f, 0.9f, 0.88f)));
            m.Cylinder(new Vector3(0f, 0.93f, 0f), 0.08f, 0.02f, 10, M(c * 0.8f));
            m.Cylinder(new Vector3(0f, OfficeJugBase, 0f), 0.13f, OfficeJugHeight, 10, BathGlass, Quaternion.identity, 0.6f);
        }

        /// <summary>The water standing in the cooler's jug, drawn full from its own origin (the jug's base); the fixture
        /// scales it to the level.</summary>
        public static void OfficeJugWater(RaidMesh m)
            => m.Cylinder(Vector3.zero, 0.118f, OfficeJugHeight - 0.03f, 10, Gs(new Color(0.30f, 0.46f, 0.64f)), Quaternion.identity, 0.64f);
        #endregion

        #region Office: vending machine
        /// <summary>Where the survivor reaches at the vending machine (the selection panel), and its delivery tray.</summary>
        internal static readonly Vector3 OfficeVendingPanel = new Vector3(0.33f, 1.05f, 0.42f);
        internal static readonly Vector3 OfficeVendingTray = new Vector3(-0.1f, 0.3f, 0.42f);

        /// <summary>A soft-drink and snack vending machine's body (0.9 × 1.85 × 0.8): the cabinet, the selection panel
        /// and its buttons, the coin slot and the delivery flap; the display window is <see cref="OfficeVendingWindow"/>.</summary>
        public static void OfficeVendingBody(RaidMesh m, System.Random rng)
        {
            Color body = Chance(rng, 0.5) ? new Color(0.62f, 0.10f, 0.10f) : new Color(0.12f, 0.24f, 0.48f);
            m.Box(new Vector3(0f, 0.925f, -0.02f), new Vector3(0.9f, 1.85f, 0.76f), G(body));
            m.Box(new Vector3(-0.1f, 1.1f, 0.362f), new Vector3(0.66f, 1.26f, 0.006f), M(body * 0.55f));
            m.Box(new Vector3(0.33f, 1.1f, 0.37f), new Vector3(0.14f, 0.8f, 0.02f), M(new Color(0.2f, 0.2f, 0.22f)));
            for (int i = 0; i < 6; i++) m.Box(new Vector3(0.33f, 0.8f + i * 0.11f, 0.385f), new Vector3(0.08f, 0.06f, 0.02f), M(new Color(0.8f, 0.8f, 0.76f)));
            m.Box(new Vector3(0.33f, 1.43f, 0.386f), new Vector3(0.1f, 0.05f, 0.004f), Gl(new Color(0.85f, 0.32f, 0.16f)));
            m.Box(new Vector3(0.33f, 0.66f, 0.383f), new Vector3(0.05f, 0.09f, 0.008f), Mt(Chrome));
            m.Box(new Vector3(0.33f, 0.66f, 0.388f), new Vector3(0.006f, 0.04f, 0.004f), M(Black));
            m.Box(new Vector3(-0.1f, 0.3f, 0.37f), new Vector3(0.5f, 0.16f, 0.02f), M(Black));
            m.Box(new Vector3(-0.1f, 0.37f, 0.382f), new Vector3(0.4f, 0.02f, 0.012f), Mt(Chrome));
        }

        /// <summary>The vending machine's display window: whole (glowing with the power on), or smashed in — a dark hollow
        /// of empty coils and what's left on them, a few teeth of glass round the frame.</summary>
        public static void OfficeVendingWindow(RaidMesh m, System.Random rng, bool lit, bool smashed)
        {
            if (!smashed)
            {
                m.Box(new Vector3(-0.1f, 1.1f, 0.368f), new Vector3(0.6f, 1.2f, 0.01f),
                    lit ? Gl(new Color(0.85f, 0.82f, 0.75f)) : M(new Color(0.6f, 0.58f, 0.55f)));
                return;
            }
            m.Box(new Vector3(-0.1f, 1.1f, 0.367f), new Vector3(0.6f, 1.2f, 0.008f), M(new Color(0.07f, 0.07f, 0.08f)));
            for (int row = 0; row < 4; row++)
            {
                float y = 0.62f + row * 0.29f;
                m.Box(new Vector3(-0.1f, y, 0.374f), new Vector3(0.58f, 0.012f, 0.012f), Mt(Chrome * 0.8f));
                if (Chance(rng, 0.6))
                    m.Upright(new Vector3(-0.1f, y + 0.12f, 0.374f), Vector3.forward, 0.54f, 0.2f, DecorMaterials.Stock(0),
                        new Rect((float)rng.NextDouble() * 0.5f, 0f, 0.4f, 0.7f));
            }
            Surf shard = Gs(new Color(0.42f, 0.46f, 0.48f));
            for (int i = 0; i < 6; i++)
            {
                float x = i % 2 == 0 ? -0.38f : 0.18f, y = 0.55f + (i / 2) * 0.55f;
                float k = R(rng, 0.05f, 0.11f);
                m.Diamond(new Vector3(x, y, 0.378f), new Vector3(i % 2 == 0 ? k : -k, 0f, 0f), new Vector3(0f, k * 1.4f, 0f), shard);
            }
        }
        #endregion

        #region Office: water heater
        /// <summary>The water heater's drain valve, its pilot's sight glass and its gas control.</summary>
        internal static readonly Vector3 OfficeHeaterValve = new Vector3(0.12f, 0.14f, 0.33f);
        internal static readonly Vector3 OfficeHeaterPilot = new Vector3(0f, 0.2f, 0.29f);
        internal static readonly Vector3 OfficeHeaterControl = new Vector3(0f, 0.42f, 0.34f);

        /// <summary>A gas water heater (0.6 × 2.75 × 0.6): the enamelled tank, its pipes and flue up to the ceiling, the gas
        /// control, the burner door with the pilot's little sight glass (lit by <see cref="OfficeHeaterPilotGlow"/>), the
        /// brass drain valve at its foot and the maker's plate.</summary>
        public static void OfficeWaterHeater(RaidMesh m, System.Random rng)
        {
            Color shell = V(new Color(0.78f, 0.76f, 0.70f), rng, 0.03f);
            m.Cylinder(Vector3.zero, 0.28f, 1.5f, 12, G(shell));
            m.Cylinder(new Vector3(0f, 1.5f, 0f), 0.22f, 0.05f, 12, G(shell * 0.9f));
            Surf copper = Mt(new Color(0.5f, 0.35f, 0.2f));
            m.Cylinder(new Vector3(0.12f, 1.5f, 0.06f), 0.03f, 1.2f, 6, copper);
            m.Cylinder(new Vector3(-0.12f, 1.5f, 0.06f), 0.03f, 1.2f, 6, copper);
            m.Cylinder(new Vector3(0f, 1.55f, -0.1f), 0.05f, 1.2f, 8, Mt(new Color(0.46f, 0.47f, 0.48f)));
            m.Box(new Vector3(0f, 0.42f, 0.3f), new Vector3(0.13f, 0.12f, 0.08f), M(new Color(0.12f, 0.12f, 0.13f)));
            m.Cylinder(new Vector3(0f, 0.44f, 0.34f), 0.03f, 0.03f, 8, M(new Color(0.62f, 0.12f, 0.1f)), Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(0.08f, 0.38f, 0.3f), new Vector3(0.02f, 0.04f, 0.06f), M(Black));
            m.Box(new Vector3(0f, 0.2f, 0.277f), new Vector3(0.18f, 0.12f, 0.012f), M(new Color(0.16f, 0.16f, 0.17f)));
            m.Box(new Vector3(0f, 0.2f, 0.284f), new Vector3(0.06f, 0.035f, 0.004f), Gs(new Color(0.1f, 0.1f, 0.12f)));
            m.CylinderX(OfficeHeaterValve + new Vector3(0f, 0f, -0.04f), 0.018f, 0.1f, 6, Mt(Brass), 90f);
            m.Box(OfficeHeaterValve + new Vector3(0f, 0.025f, -0.03f), new Vector3(0.05f, 0.012f, 0.012f), Mt(Brass));
            m.Box(new Vector3(0f, 0.95f, 0.279f), new Vector3(0.16f, 0.2f, 0.004f), M(Paper));
            m.Box(new Vector3(0f, 1.0f, 0.281f), new Vector3(0.12f, 0.02f, 0.002f), M(new Color(0.3f, 0.3f, 0.32f)));
        }

        /// <summary>The pilot flame seen through the sight glass (drawn round its own origin).</summary>
        public static void OfficeHeaterPilotGlow(RaidMesh m) => m.Box(Vector3.zero, new Vector3(0.04f, 0.024f, 0.004f), Gl(new Color(0.42f, 0.62f, 1f)));
        #endregion

        #region Office: drums
        /// <summary>A 55-gallon drum's open body (r 0.29, 0.88 high) with its rolling hoops and a dark slop inside, given
        /// <paramref name="paint"/> (a yellow-black band when <paramref name="hazard"/>). Returns the paint as varied, for
        /// its lid (<see cref="OfficeDrumLid"/>).</summary>
        public static Color OfficeDrumBody(RaidMesh m, System.Random rng, Color paint, bool hazard)
        {
            Color c = V(paint, rng, 0.08f);
            m.Cylinder(Vector3.zero, 0.29f, 0.88f, 12, G(c), Quaternion.identity, 1f, false);
            m.Cylinder(Vector3.zero, 0.29f, 0.01f, 12, G(c * 0.8f));
            m.Cylinder(new Vector3(0f, 0.28f, 0f), 0.298f, 0.035f, 12, G(c * 0.85f));
            m.Cylinder(new Vector3(0f, 0.58f, 0f), 0.298f, 0.035f, 12, G(c * 0.85f));
            m.Cylinder(new Vector3(0f, 0.86f, 0f), 0.296f, 0.03f, 12, G(c * 0.75f), Quaternion.identity, 1f, false);
            // Inside: the shadowed wall and whatever's settled in the bottom.
            RaidInnerWall(m, 0.284f, 0.02f, 0.86f, 12, M(c * 0.3f));
            m.Cylinder(new Vector3(0f, 0.3f, 0f), 0.28f, 0.004f, 12, M(new Color(0.1f, 0.09f, 0.08f)));
            if (hazard) m.Cylinder(new Vector3(0f, 0.36f, 0f), 0.297f, 0.2f, 12, DecorMaterials.Hazard, Quaternion.identity, 1f, false);
            if (Chance(rng, 0.35))
            {
                float a = R(rng, 0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                m.Blob(dir * 0.296f + Vector3.up * R(rng, 0.15f, 0.6f), Vector3.Cross(Vector3.up, dir) * -1f, Vector3.up, 0.12f, 0.16f, 8, 0.4f, rng, M(new Color(0.36f, 0.20f, 0.10f)));
            }
            return c;
        }

        // The inside face of a round wall (radius, from y0 up height), seen past an open top.
        private static void RaidInnerWall(RaidMesh m, float radius, float y0, float height, int sides, in Surf s)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * radius;
                var p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * radius;
                m.Quad((p0 + p1) * 0.5f + Vector3.up * (y0 + height * 0.5f), (p1 - p0) * 0.5f, Vector3.up * (height * 0.5f), s);
            }
        }

        /// <summary>A drum's lid (drawn on), with its bung: hinged on its left edge it lifts off and stands on end.</summary>
        public static void OfficeDrumLid(RaidMesh m, Color paint)
        {
            m.Cylinder(new Vector3(0f, 0.872f, 0f), 0.3f, 0.02f, 12, G(paint * 0.75f));
            m.Cylinder(new Vector3(0f, 0.89f, 0f), 0.26f, 0.004f, 12, M(paint * 0.6f));
            m.Cylinder(new Vector3(0.14f, 0.89f, 0.05f), 0.03f, 0.015f, 6, M(paint * 0.5f));
        }
        #endregion

        #region Food mart: checkout counter and till
        /// <summary>The register's cash drawer, closed: its front's middle (relative to the register's x).</summary>
        internal const float MartTillY = 1.045f, MartTillFront = 0.15f;

        /// <summary>The checkout counter (length along X, 0.72 deep, 1.0 high) with the cash register on it, all but the
        /// cash drawer (<see cref="MartTillDrawer"/>). Returns the register's x.</summary>
        public static float MartCounterBody(RaidMesh m, System.Random rng, float len)
        {
            Color c = V(new Color(0.55f, 0.38f, 0.22f), rng, 0.05f);
            m.Box(new Vector3(0f, 0.48f, 0f), new Vector3(len, 0.96f, 0.66f), M(c));
            m.Box(new Vector3(0f, 0.98f, 0f), new Vector3(len + 0.04f, 0.04f, 0.72f), G(new Color(0.72f, 0.70f, 0.64f)));
            m.Box(new Vector3(0f, 0.5f, 0.335f), new Vector3(len - 0.1f, 0.7f, 0.01f), M(c * 0.85f));
            float rx = -len * 0.3f;
            Color reg = new Color(0.18f, 0.18f, 0.2f);
            m.Box(new Vector3(rx - 0.19f, 1.045f, -0.05f), new Vector3(0.02f, 0.09f, 0.4f), M(reg));
            m.Box(new Vector3(rx + 0.19f, 1.045f, -0.05f), new Vector3(0.02f, 0.09f, 0.4f), M(reg));
            m.Box(new Vector3(rx, 1.045f, -0.24f), new Vector3(0.36f, 0.09f, 0.02f), M(reg));
            m.Box(new Vector3(rx, 1.14f, -0.05f), new Vector3(0.4f, 0.1f, 0.4f), M(reg));
            m.Box(new Vector3(rx, 1.23f, 0.06f), new Vector3(0.36f, 0.1f, 0.12f), Quaternion.Euler(30f, 0f, 0f), M(new Color(0.24f, 0.24f, 0.26f)));
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 4; j++)
                    m.Box(new Vector3(rx - 0.1f + j * 0.06f, 1.275f - i * 0.026f, 0.06f + i * 0.045f), new Vector3(0.04f, 0.012f, 0.03f),
                        Quaternion.Euler(30f, 0f, 0f), M(new Color(0.78f, 0.76f, 0.70f)));
            m.Box(new Vector3(rx, 1.33f, -0.15f), new Vector3(0.2f, 0.1f, 0.04f), M(new Color(0.15f, 0.15f, 0.16f)));
            m.Box(new Vector3(rx, 1.33f, -0.128f), new Vector3(0.16f, 0.05f, 0.004f), Gl(new Color(0.2f, 0.6f, 0.4f)));
            m.CylinderX(new Vector3(rx + 0.13f, 1.22f, -0.18f), 0.04f, 0.09f, 8, M(Paper));
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(R(rng, 0f, len * 0.4f), 1.0f + i * 0.02f, R(rng, -0.2f, 0.2f)), new Vector3(0.2f, 0.015f, 0.28f), Yaw(R(rng, -30f, 30f)), M(V(new Color(0.7f, 0.5f, 0.3f), rng, 0.3f)));
            return rx;
        }

        /// <summary>The register's cash drawer (drawn shut under the register at <paramref name="rx"/>): a shallow steel
        /// tray of bill slots and coin cups, a few bills left in it.</summary>
        public static void MartTillDrawer(RaidMesh m, System.Random rng, float rx)
        {
            Color reg = new Color(0.18f, 0.18f, 0.2f);
            m.Box(new Vector3(rx, 1.006f, -0.05f), new Vector3(0.36f, 0.012f, 0.38f), M(reg * 0.8f));
            m.Box(new Vector3(rx - 0.174f, 1.035f, -0.05f), new Vector3(0.012f, 0.05f, 0.38f), M(reg * 0.8f));
            m.Box(new Vector3(rx + 0.174f, 1.035f, -0.05f), new Vector3(0.012f, 0.05f, 0.38f), M(reg * 0.8f));
            m.Box(new Vector3(rx, 1.035f, -0.234f), new Vector3(0.36f, 0.05f, 0.012f), M(reg * 0.8f));
            m.Box(new Vector3(rx, MartTillY, MartTillFront - 0.008f), new Vector3(0.37f, 0.086f, 0.016f), M(reg));
            m.Box(new Vector3(rx, MartTillY + 0.02f, MartTillFront + 0.002f), new Vector3(0.1f, 0.012f, 0.006f), Mt(Chrome));
            for (int i = 1; i < 4; i++)
                m.Box(new Vector3(rx - 0.18f + i * 0.09f, 1.03f, -0.12f), new Vector3(0.006f, 0.04f, 0.2f), M(reg * 0.6f));
            for (int i = 0; i < 4; i++)
                if (Chance(rng, 0.5))
                    m.Box(new Vector3(rx - 0.135f + i * 0.09f, 1.016f, -0.12f), new Vector3(0.07f, 0.006f, 0.15f), M(new Color(0.52f, 0.62f, 0.46f)));
            for (int i = 0; i < 5; i++)
                m.Cylinder(new Vector3(rx - 0.14f + i * 0.07f, 1.012f, 0.06f), 0.026f, 0.02f, 8, M(reg * 0.55f));
        }
        #endregion

        #region Food mart: drinks cooler
        /// <summary>The glass door's hinge (its left stile's outer edge, at the bottom) and the door's front face.</summary>
        internal static readonly Vector3 MartCoolerHinge = new Vector3(-0.445f, 0.14f, 0.31f);
        internal const float MartCoolerFront = 0.31f;

        /// <summary>A glass-door drinks cooler's cabinet (0.9 × 2.0 × 0.75), hollow so the stock shows through the door:
        /// the lit back panel (when <paramref name="lit"/>), four chrome shelves of bottles and cans, the red header and
        /// the kick grille. The door is <see cref="MartCoolerDoor"/>.</summary>
        public static void MartCoolerBody(RaidMesh m, System.Random rng, bool lit)
        {
            Color frame = new Color(0.82f, 0.82f, 0.80f);
            m.Box(new Vector3(0f, 1.0f, -0.355f), new Vector3(0.9f, 2.0f, 0.02f), M(frame));
            m.Box(new Vector3(-0.44f, 1.0f, -0.05f), new Vector3(0.02f, 2.0f, 0.62f), M(frame));
            m.Box(new Vector3(0.44f, 1.0f, -0.05f), new Vector3(0.02f, 2.0f, 0.62f), M(frame));
            m.Box(new Vector3(0f, 1.89f, -0.05f), new Vector3(0.88f, 0.22f, 0.62f), M(frame));
            m.Box(new Vector3(0f, 1.9f, 0.27f), new Vector3(0.9f, 0.2f, 0.04f), M(new Color(0.70f, 0.14f, 0.12f)));
            m.Box(new Vector3(0f, 0.1f, -0.05f), new Vector3(0.88f, 0.2f, 0.62f), M(frame * 0.9f));
            m.Box(new Vector3(0f, 0.08f, 0.27f), new Vector3(0.86f, 0.12f, 0.04f), M(Black));
            m.Box(new Vector3(0f, 1.0f, -0.335f), new Vector3(0.84f, 1.56f, 0.01f), lit ? Gl(new Color(0.62f, 0.72f, 0.78f)) : M(new Color(0.25f, 0.27f, 0.28f)));
            for (int s = 0; s < 4; s++)
            {
                float y = 0.3f + s * 0.4f;
                m.Box(new Vector3(0f, y, -0.06f), new Vector3(0.86f, 0.015f, 0.52f), Mt(Chrome));
                if (Chance(rng, 0.85))
                    m.Upright(new Vector3(0f, y + 0.14f, 0.14f), Vector3.forward, 0.8f, 0.26f, DecorMaterials.Stock(0), new Rect((float)rng.NextDouble() * 0.5f, 0f, 0.45f, 0.85f));
            }
        }

        /// <summary>The cooler's glass door (drawn shut) in its chrome frame, with the long pull handle on its free edge.</summary>
        public static void MartCoolerDoor(RaidMesh m)
        {
            Surf chrome = Mt(Chrome);
            const float z = MartCoolerFront - 0.025f;
            m.Box(new Vector3(-0.42f, 0.98f, z), new Vector3(0.05f, 1.64f, 0.05f), chrome);
            m.Box(new Vector3(0.42f, 0.98f, z), new Vector3(0.05f, 1.64f, 0.05f), chrome);
            m.Box(new Vector3(0f, 0.17f, z), new Vector3(0.9f, 0.06f, 0.05f), chrome);
            m.Box(new Vector3(0f, 1.775f, z), new Vector3(0.9f, 0.05f, 0.05f), chrome);
            m.Box(new Vector3(0f, 0.975f, z), new Vector3(0.8f, 1.56f, 0.008f), BathGlass);
            m.Box(new Vector3(0.36f, 1.05f, MartCoolerFront + 0.02f), new Vector3(0.03f, 0.5f, 0.03f), chrome);
            m.Box(new Vector3(0.36f, 0.81f, MartCoolerFront + 0.005f), new Vector3(0.03f, 0.02f, 0.03f), chrome);
            m.Box(new Vector3(0.36f, 1.29f, MartCoolerFront + 0.005f), new Vector3(0.03f, 0.02f, 0.03f), chrome);
        }
        #endregion

        #region Food mart: the safe
        /// <summary>The safe door's hinge (the left edge of its face, at the bottom).</summary>
        internal static readonly Vector3 MartSafeHinge = new Vector3(-0.37f, 0.05f, 0.29f);
        /// <summary>The dial on the door, where the survivor works.</summary>
        internal static readonly Vector3 MartSafeDial = new Vector3(-0.12f, 1.25f, 0.33f);

        /// <summary>A heavy floor safe's carcass (0.8 × 1.9 × 0.6), hollow behind its door: two shelves with a cash box and
        /// a ledger on them. The door is <see cref="MartSafeDoor"/>; returns the steel's colour for it.</summary>
        public static Color MartSafeBody(RaidMesh m, System.Random rng)
        {
            Color c = V(new Color(0.20f, 0.23f, 0.21f), rng, 0.05f);
            Surf steel = M(c), inside = M(new Color(0.12f, 0.12f, 0.12f));
            m.Box(new Vector3(0f, 0.95f, -0.28f), new Vector3(0.8f, 1.9f, 0.04f), steel);
            m.Box(new Vector3(-0.38f, 0.95f, -0.01f), new Vector3(0.04f, 1.9f, 0.5f), steel);
            m.Box(new Vector3(0.38f, 0.95f, -0.01f), new Vector3(0.04f, 1.9f, 0.5f), steel);
            m.Box(new Vector3(0f, 1.87f, -0.01f), new Vector3(0.72f, 0.06f, 0.5f), steel);
            m.Box(new Vector3(0f, 0.04f, -0.01f), new Vector3(0.72f, 0.08f, 0.5f), steel);
            m.Box(new Vector3(0f, 0.95f, -0.255f), new Vector3(0.72f, 1.74f, 0.01f), inside);
            for (int s = 0; s < 2; s++)
                m.Box(new Vector3(0f, 0.68f + s * 0.56f, -0.02f), new Vector3(0.72f, 0.02f, 0.46f), M(c * 0.8f));
            m.Box(new Vector3(R(rng, -0.15f, 0.1f), 0.75f, -0.05f), new Vector3(0.3f, 0.12f, 0.22f), Yaw(R(rng, -8f, 8f)), Mt(new Color(0.42f, 0.44f, 0.46f)));
            m.Box(new Vector3(R(rng, -0.1f, 0.15f), 1.3f, -0.06f), new Vector3(0.26f, 0.05f, 0.34f), Yaw(R(rng, -10f, 10f)), M(new Color(0.24f, 0.30f, 0.20f)));
            return c;
        }

        /// <summary>The safe's door (drawn shut): the slab, the brass name plate, the handle wheel, the dial and the hinge bar.</summary>
        public static void MartSafeDoor(RaidMesh m, Color steel)
        {
            m.Box(new Vector3(0f, 0.95f, 0.27f), new Vector3(0.72f, 1.8f, 0.04f), G(steel * 1.15f));
            m.Box(new Vector3(0f, 1.8f, 0.293f), new Vector3(0.5f, 0.05f, 0.006f), M(Brass));
            m.Cylinder(new Vector3(0.12f, 1.05f, 0.29f), 0.09f, 0.03f, 10, Mt(Chrome), Quaternion.Euler(90f, 0f, 0f));
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(0.12f, 1.05f, 0.325f), new Vector3(0.24f, 0.02f, 0.02f), Quaternion.Euler(0f, 0f, i * 60f), Mt(Chrome));
            m.Cylinder(new Vector3(-0.12f, 1.25f, 0.29f), 0.05f, 0.03f, 10, M(Black), Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(-0.12f, 1.25f, 0.321f), new Vector3(0.006f, 0.03f, 0.004f), M(Paper));
            m.Box(new Vector3(-0.33f, 0.95f, 0.3f), new Vector3(0.04f, 1.6f, 0.03f), M(steel * 0.7f));
        }
        #endregion

        #region Quarantine tent: the cot
        /// <summary>A folding camp cot (1.9 × 0.5 × 0.8, its length along X, head at -X): a steel frame, a canvas bed, a
        /// rolled sleeping bag and a folded blanket for a pillow.</summary>
        public static void TentCot(RaidMesh m, System.Random rng, Color drab)
        {
            Surf frame = Mt(new Color(0.25f, 0.27f, 0.22f));
            Surf cloth = M(V(new Color(0.36f, 0.38f, 0.26f), rng, 0.04f));
            for (int i = 0; i < 4; i++) m.Box(new Vector3(i % 2 == 0 ? -0.85f : 0.85f, 0.2f, i < 2 ? -0.32f : 0.32f), new Vector3(0.04f, 0.4f, 0.04f), frame);
            m.Box(new Vector3(0f, 0.39f, -0.36f), new Vector3(1.9f, 0.03f, 0.03f), frame);
            m.Box(new Vector3(0f, 0.39f, 0.36f), new Vector3(1.9f, 0.03f, 0.03f), frame);
            m.Box(new Vector3(0f, 0.42f, 0f), new Vector3(1.9f, 0.04f, 0.75f), cloth);
            m.Box(new Vector3(0.2f, 0.47f, 0f), new Vector3(1.2f, 0.06f, 0.7f), M(drab));
            m.CylinderX(new Vector3(0.78f, 0.53f, 0f), 0.08f, 0.66f, 8, M(drab * 0.85f), 90f);
            m.Box(new Vector3(-0.7f, 0.48f, 0f), new Vector3(0.34f, 0.08f, 0.5f), Yaw(R(rng, -6f, 6f)), M(new Color(0.55f, 0.52f, 0.42f)));
        }
        #endregion

        #region Outdoors: dumpster and guard booth
        /// <summary>The dumpster's right-hand lid's hinge (its back edge) and where the survivor reaches to lift it.</summary>
        internal static readonly Vector3 TownDumpsterHinge = new Vector3(0.47f, 1.18f, -0.56f);
        internal static readonly Vector3 TownDumpsterReach = new Vector3(0.47f, 1.02f, 0.62f);

        /// <summary>A green steel dumpster (1.9 × 1.2 × 1.1): the bin on its casters, the left lid shut under a crust of
        /// snow, and under the right-hand lid (<see cref="TownDumpsterLid"/>) the bags and rubbish it's full of.</summary>
        public static void TownDumpsterBody(RaidMesh m, System.Random rng)
        {
            Color c = V(new Color(0.16f, 0.30f, 0.20f), rng, 0.08f);
            m.Box(new Vector3(0f, 0.65f, 0f), new Vector3(1.9f, 1.0f, 1.1f), G(c));
            m.Box(new Vector3(0f, 0.65f, 0.56f), new Vector3(1.8f, 0.9f, 0.02f), M(c * 0.85f));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.98f, 0.5f, 0f), new Vector3(0.08f, 0.16f, 1.0f), M(c * 0.6f));
                m.Cylinder(new Vector3(s * 0.75f, 0f, 0.42f), 0.08f, 0.15f, 6, M(new Color(0.07f, 0.07f, 0.07f)));
                m.Cylinder(new Vector3(s * 0.75f, 0f, -0.42f), 0.08f, 0.15f, 6, M(new Color(0.07f, 0.07f, 0.07f)));
            }
            Surf lid = M(new Color(0.12f, 0.12f, 0.12f));
            m.Box(new Vector3(-0.47f, 1.18f, 0f), new Vector3(0.92f, 0.04f, 1.12f), lid);
            m.Box(new Vector3(-0.47f, 1.23f, 0f), new Vector3(0.8f, 0.06f, 1.0f), M(DecorProps.SnowWhite));
            // The rubbish under the right-hand lid: a dark heap of bags just below the rim.
            m.Box(new Vector3(0.47f, 1.153f, 0f), new Vector3(0.86f, 0.004f, 1.04f), M(new Color(0.06f, 0.06f, 0.07f)));
            for (int i = 0; i < 3; i++)
                m.Ellipsoid(new Vector3(R(rng, 0.2f, 0.75f), 1.152f, R(rng, -0.35f, 0.35f)), new Vector3(R(rng, 0.25f, 0.4f), 0.012f, R(rng, 0.25f, 0.4f)), G(new Color(0.05f, 0.05f, 0.06f)), 8, 3);
        }

        /// <summary>The dumpster's right-hand lid (drawn shut), hinged along its back edge (<see cref="TownDumpsterHinge"/>).</summary>
        public static void TownDumpsterLid(RaidMesh m)
        {
            m.Box(new Vector3(0.47f, 1.18f, 0f), new Vector3(0.92f, 0.04f, 1.12f), M(new Color(0.12f, 0.12f, 0.12f)));
            m.Box(new Vector3(0.47f, 1.18f, 0.575f), new Vector3(0.3f, 0.03f, 0.03f), M(new Color(0.09f, 0.09f, 0.09f)));
        }

        /// <summary>The guard booth's sliding hatch, its radio on the ledge under it, and where the survivor reaches for each.</summary>
        internal static readonly Vector3 TownBoothHatchReach = new Vector3(-0.25f, 1.35f, 0.95f);
        internal static readonly Vector3 TownBoothRadioReach = new Vector3(0.42f, 1.25f, 1.05f);
        internal static readonly Vector3 TownBoothRadio = new Vector3(0.42f, 1.13f, 0.91f);
        internal const float TownBoothHatchTravel = 0.46f;

        /// <summary>The checkpoint's guard booth (1.6 × 2.3 × 1.6): olive panels, dark glazing all round, the roof under
        /// snow, a ledge outside the front window with the booth radio on it, and the frame of the sliding hatch
        /// (<see cref="TownBoothHatch"/>).</summary>
        public static void TownBoothBody(RaidMesh m, System.Random rng)
        {
            Color wall = new Color(0.34f, 0.36f, 0.26f);
            m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(1.6f, 1.1f, 1.6f), M(wall));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(i % 2 == 0 ? -0.76f : 0.76f, 1.55f, i < 2 ? -0.76f : 0.76f), new Vector3(0.08f, 0.9f, 0.08f), M(wall));
            m.Box(new Vector3(0f, 1.55f, 0f), new Vector3(1.5f, 0.85f, 1.5f), Gs(new Color(0.08f, 0.10f, 0.11f)));
            m.Box(new Vector3(0f, 2.1f, 0f), new Vector3(1.9f, 0.2f, 1.9f), M(wall * 0.8f));
            m.Box(new Vector3(0f, 2.23f, 0f), new Vector3(1.7f, 0.06f, 1.7f), M(DecorProps.SnowWhite));
            m.Box(new Vector3(0f, 1.115f, 0.9f), new Vector3(1.2f, 0.03f, 0.28f), M(wall * 0.9f));
            m.Box(new Vector3(-0.3f, 1.135f, 0.98f), new Vector3(0.5f, 0.012f, 0.1f), M(DecorProps.SnowShade));
            m.Box(new Vector3(-0.25f, 1.55f, 0.762f), new Vector3(0.58f, 0.66f, 0.016f), M(wall * 0.7f));
            // The booth radio, out on the ledge under the eaves (its lit dial is laid over it while it plays).
            TownFieldRadio(m, rng, TownBoothRadio, 0f);
        }

        /// <summary>The booth's hatch (drawn shut): a glazed sash that slides along the front window.</summary>
        public static void TownBoothHatch(RaidMesh m)
        {
            Surf frame = M(new Color(0.24f, 0.26f, 0.18f));
            m.Box(new Vector3(-0.25f, 1.55f, 0.775f), new Vector3(0.5f, 0.6f, 0.012f), Gs(new Color(0.12f, 0.15f, 0.16f)));
            m.Box(new Vector3(-0.49f, 1.55f, 0.78f), new Vector3(0.03f, 0.62f, 0.025f), frame);
            m.Box(new Vector3(-0.01f, 1.55f, 0.78f), new Vector3(0.03f, 0.62f, 0.025f), frame);
            m.Box(new Vector3(-0.25f, 1.255f, 0.78f), new Vector3(0.5f, 0.03f, 0.025f), frame);
            m.Box(new Vector3(-0.25f, 1.845f, 0.78f), new Vector3(0.5f, 0.03f, 0.025f), frame);
            m.Box(new Vector3(-0.05f, 1.5f, 0.795f), new Vector3(0.02f, 0.12f, 0.02f), Mt(Chrome));
        }

        /// <summary>A small lamp's glow (a radio's dial, a generator's run lamp), drawn round its own origin facing +Z.</summary>
        public static void TownLampGlow(RaidMesh m, Color colour, float w, float h) => m.Box(Vector3.zero, new Vector3(w, h, 0.004f), Gl(colour));

        /// <summary>An olive field radio like <see cref="FieldRadio"/> standing at <paramref name="at"/> turned by
        /// <paramref name="yaw"/>, its dial dark (the lit dial is <see cref="TownFieldRadioDial"/>, laid over it while the
        /// set is on). Returns the dial's middle (frame-local).</summary>
        public static Vector3 TownFieldRadio(RaidMesh m, System.Random rng, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(new Color(0.26f, 0.32f, 0.18f), rng, 0.05f);
            m.Box(at + q * new Vector3(0f, 0.11f, 0f), new Vector3(0.38f, 0.22f, 0.24f), q, M(c));
            m.Box(at + q * new Vector3(0f, 0.12f, 0.121f), new Vector3(0.32f, 0.16f, 0.004f), q, M(new Color(0.12f, 0.13f, 0.12f)));
            m.Box(at + q * new Vector3(-0.07f, 0.16f, 0.125f), new Vector3(0.12f, 0.04f, 0.004f), q, M(new Color(0.22f, 0.2f, 0.14f)));
            for (int i = 0; i < 3; i++)
                m.Cylinder(at + q * new Vector3(0.04f + i * 0.045f, 0.09f, 0.123f), 0.015f, 0.025f, 6, M(Black), q * Quaternion.Euler(90f, 0f, 0f));
            m.Box(at + q * new Vector3(-0.12f, 0.25f, 0f), new Vector3(0.02f, 0.06f, 0.02f), q, M(c * 0.7f));
            m.Box(at + q * new Vector3(0.12f, 0.25f, 0f), new Vector3(0.02f, 0.06f, 0.02f), q, M(c * 0.7f));
            m.Box(at + q * new Vector3(0f, 0.28f, 0f), new Vector3(0.26f, 0.02f, 0.025f), q, M(c * 0.7f));
            m.Rod(at + q * new Vector3(0.15f, 0.22f, -0.08f), at + q * new Vector3(0.2f, 0.85f, -0.16f), 0.008f, M(Black));
            m.Box(at + q * new Vector3(-0.3f, 0.06f, 0.02f), new Vector3(0.12f, 0.04f, 0.2f), q, M(new Color(0.1f, 0.1f, 0.1f)));
            return at + q * new Vector3(-0.07f, 0.16f, 0.128f);
        }

        /// <summary>A field radio's lit dial (drawn round its own origin, facing the radio's front).</summary>
        public static void TownFieldRadioDial(RaidMesh m) => m.Box(Vector3.zero, new Vector3(0.12f, 0.04f, 0.004f), Gl(new Color(0.85f, 0.66f, 0.28f)));
        #endregion
    }
}
