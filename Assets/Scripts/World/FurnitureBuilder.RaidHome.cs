using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        // The raid town's houses, refitted to work like the starter home: the pieces the houses had drawn whole — the wall
        // cupboards, the living room's TV, the wardrobe, the laundry's washing machines and the ranch's bath — drawn as a
        // fixed body plus the parts that move (doors, taps, the plug) and the water and suds their fixtures show and hide,
        // and the yard's mailbox, doormat, outside tap and house number. Furniture is drawn in its local frame like the rest
        // (front toward +Z, origin on the floor at its middle); the layout constants are shared with the fixtures that work
        // them (RaidBuilding.Home.cs, RaidHomeBath, RaidHomeWasher).

        private static readonly Color[] WardrobeCloths =
        {
            new Color(0.22f, 0.26f, 0.38f), new Color(0.52f, 0.18f, 0.16f), new Color(0.80f, 0.78f, 0.70f), new Color(0.30f, 0.34f, 0.24f),
            new Color(0.62f, 0.52f, 0.36f), new Color(0.14f, 0.14f, 0.15f), new Color(0.58f, 0.42f, 0.56f),
        };

        #region Wall cupboards
        /// <summary>How many two-door bays a run of wall cupboards <paramref name="len"/> long is split into (each about the
        /// starter home's 1.2 m cupboard).</summary>
        internal static int WallCupBays(float len) => Mathf.Max(1, Mathf.RoundToInt(len / WallCupLength));

        /// <summary>A run of wall cupboards <paramref name="len"/> long over a raid kitchen's worktop, drawn like the starter
        /// home's (<see cref="WallCupboardBody"/>) but to any length: its <see cref="WallCupBays"/> bays share one carcass with a
        /// shelf and a cornice, a partition between each, and whatever crockery and tins were put away in them. The frame's
        /// origin is on the wall face at floor level, +Z into the room; the doors are drawn by <see cref="RaidWallCupboardDoor"/>.</summary>
        public static void RaidWallCupboardBody(RaidMesh m, System.Random rng, in KitchenTone t, float len)
        {
            int bays = WallCupBays(len);
            float h = len * 0.5f, d = WallCupDepth, bay = len / bays;
            float cy = (WallCupY0 + WallCupY1) * 0.5f, hh = WallCupY1 - WallCupY0;
            Surf cab = M(t.Cabinet), inside = M(t.Inside);
            m.Box(new Vector3(-h + 0.009f, cy, d * 0.5f), new Vector3(0.018f, hh, d), cab);
            m.Box(new Vector3(h - 0.009f, cy, d * 0.5f), new Vector3(0.018f, hh, d), cab);
            m.Box(new Vector3(0f, WallCupY1 - 0.009f, d * 0.5f), new Vector3(len, 0.018f, d), cab);
            m.Box(new Vector3(0f, WallCupY0 + 0.009f, d * 0.5f), new Vector3(len, 0.018f, d), cab);
            m.Box(new Vector3(0f, cy, 0.006f), new Vector3(len - 0.036f, hh - 0.036f, 0.012f), inside);
            float shelf = cy + 0.01f;
            m.Box(new Vector3(0f, shelf, d * 0.5f - 0.01f), new Vector3(len - 0.036f, 0.014f, d - 0.04f), inside);
            m.Box(new Vector3(0f, WallCupY1 + 0.012f, d * 0.5f + 0.012f), new Vector3(len + 0.03f, 0.024f, d + 0.044f), M(t.Cabinet * 0.94f));
            for (int i = 1; i < bays; i++)
                m.Box(new Vector3(-h + bay * i, cy, d * 0.5f), new Vector3(0.018f, hh - 0.036f, d - 0.012f), cab);

            // Each bay's crockery: plates and bowls stacked below, mugs beside them, glasses and a tin or two on the shelf.
            float floor = WallCupY0 + 0.018f, upper = shelf + 0.007f;
            Surf plate = G(KitchenPlate), glass = Gs(new Color(0.55f, 0.66f, 0.70f));
            for (int i = 0; i < bays; i++)
            {
                float c = -h + bay * (i + 0.5f), k = Mathf.Min(1f, bay / WallCupLength);
                if (Chance(rng, 0.8))
                    for (int p = 0, n = 2 + rng.Next(4); p < n; p++) m.Cylinder(new Vector3(c - 0.3f * k, floor + p * 0.014f, 0.17f), 0.11f, 0.012f, 14, plate);
                if (Chance(rng, 0.6))
                    for (int p = 0, n = 1 + rng.Next(3); p < n; p++)
                        m.Cylinder(new Vector3(c + 0.02f * k, floor + p * 0.03f, 0.17f), 0.075f, 0.03f, 12, plate, Quaternion.identity, 1.35f);
                for (int p = 0, n = rng.Next(4); p < n; p++)
                    m.Cylinder(new Vector3(c + (0.22f + p * 0.1f) * k, floor, 0.19f + (p % 2) * 0.05f), 0.04f, 0.09f, 10,
                        G(V(KitchenMugColours[p % KitchenMugColours.Length], rng, 0.06f)));
                for (int p = 0, n = rng.Next(5); p < n; p++) m.Cylinder(new Vector3(c + (-0.4f + p * 0.09f) * k, upper, 0.2f), 0.035f, 0.12f, 10, glass);
                if (Chance(rng, 0.6))
                    for (int p = 0, n = 1 + rng.Next(3); p < n; p++)
                    {
                        Color label = V(new Color(0.70f, 0.20f, 0.14f), rng, 0.3f);
                        var at = new Vector3(c + (0.12f + p * 0.1f) * k, upper, 0.16f + (p % 2) * 0.04f);
                        m.Cylinder(at, 0.038f, 0.11f, 10, Mt(new Color(0.70f, 0.70f, 0.68f)));
                        m.Cylinder(at + new Vector3(0f, 0.02f, 0f), 0.039f, 0.07f, 10, M(label), Quaternion.identity, 1f, false);
                    }
            }
        }

        /// <summary>One door of a raid run of wall cupboards, shut: <paramref name="x0"/>..<paramref name="x1"/> along the run
        /// (frame-local), hung on its -X edge when <paramref name="hingeLeft"/>, its knob by the free edge.</summary>
        public static void RaidWallCupboardDoor(RaidMesh m, in KitchenTone t, float x0, float x1, bool hingeLeft)
            => KitchenCupboardDoor(m, t, x0, x1, WallCupY0 + 0.004f, WallCupY1 - 0.004f, WallCupDepth + 0.02f,
                hingeLeft ? x1 - 0.045f : x0 + 0.045f, WallCupY0 + 0.09f);
        #endregion

        #region The living room's TV
        /// <summary>A low wooden TV stand (1.0 × 0.5 × 0.44, front +Z) with a VCR behind its smoked-glass front, its clock
        /// light glowing green — without the set, which stands on its top (<see cref="ConsoleTv"/>).</summary>
        public static void TvStandBody(RaidMesh m, System.Random rng)
        {
            Color wood = V(Walnut, rng, 0.08f);
            m.Box(new Vector3(0f, 0.25f, -0.02f), new Vector3(1.0f, 0.5f, 0.44f), M(wood));
            m.Box(new Vector3(0f, 0.2f, 0.2f), new Vector3(0.9f, 0.28f, 0.01f), M(new Color(0.05f, 0.05f, 0.05f)));
            m.Box(new Vector3(-0.15f, 0.12f, 0.1f), new Vector3(0.4f, 0.08f, 0.26f), M(new Color(0.08f, 0.08f, 0.09f)));
            m.Box(new Vector3(-0.1f, 0.125f, 0.232f), new Vector3(0.14f, 0.02f, 0.004f), Gl(new Color(0.2f, 0.6f, 0.3f)));
            // The remote, left on the end of the stand beside the set.
            m.Box(new Vector3(0.42f, 0.506f, 0.08f), new Vector3(0.05f, 0.012f, 0.16f), Yaw(R(rng, -25f, 25f)), M(new Color(0.12f, 0.12f, 0.13f)));
            // tiny chrome buttons on the remote surface (random slight offset for wear)
            for (int i = 0; i < 3; i++)
            {
                float offX = R(rng, -0.015f, 0.015f);
                float offZ = R(rng, -0.06f, 0.06f);
                // place a small chrome button cylinder slightly above the remote top
                m.Cylinder(new Vector3(0.42f + offX, 0.512f, 0.08f + offZ), 0.003f, 0.006f, 6, M(Chrome));
            }
        }

        /// <summary>A 1993 console TV (0.68 × 0.56 × 0.5, front +Z) with rabbit ears, standing on a TV stand's top (0.5 up):
        /// the cabinet, a dark screen, the control strip with its two chrome knobs. Returns the screen's centre (local, on
        /// the glass); <paramref name="screen"/> is its size (the lit picture is <see cref="TvScreenGlow"/>).</summary>
        public static Vector3 ConsoleTv(RaidMesh m, System.Random rng, out Vector2 screen)
        {
            Color tv = Chance(rng, 0.5) ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.36f, 0.24f, 0.14f);
            m.Box(new Vector3(0f, 0.78f, -0.04f), new Vector3(0.68f, 0.56f, 0.5f), M(tv));
            var centre = new Vector3(-0.07f, 0.79f, 0.22f);
            screen = new Vector2(0.48f, 0.38f);
            m.Box(new Vector3(centre.x, centre.y, 0.215f), new Vector3(screen.x, screen.y, 0.01f), Gs(ScreenOff));
            m.Box(new Vector3(0.25f, 0.79f, 0.215f), new Vector3(0.1f, 0.42f, 0.012f), M(tv * 0.6f));
            m.Cylinder(new Vector3(0.25f, 0.88f, 0.21f), 0.025f, 0.03f, 6, M(Chrome), Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(new Vector3(0.25f, 0.78f, 0.21f), 0.025f, 0.03f, 6, M(Chrome), Quaternion.Euler(90f, 0f, 0f));
            m.Ellipsoid(new Vector3(0f, 1.08f, -0.1f), new Vector3(0.12f, 0.05f, 0.1f), M(Black), 6, 3);
            m.Rod(new Vector3(0f, 1.1f, -0.1f), new Vector3(-0.28f, 1.55f, -0.16f), 0.008f, Mt(Chrome));
            m.Rod(new Vector3(0f, 1.1f, -0.1f), new Vector3(0.3f, 1.52f, -0.12f), 0.008f, Mt(Chrome));
            return centre;
        }
        #endregion

        #region Wardrobe
        // The bedroom wardrobe (1.0 wide, 0.58 deep, 1.95 high; front +Z): a carcass open at the front, and two panelled
        // doors hung on its outer edges (their fronts at WardrobeFront) meeting in the middle.
        internal const float WardrobeFront = 0.265f;
        internal const float WardrobeDoorY0 = 0.13f, WardrobeDoorY1 = 1.87f;
        internal const float WardrobeHalf = 0.49f;   // the doors' hinged edges, either side of the middle

        /// <summary>The wardrobe's carcass without its doors (see <see cref="WardrobeDoor"/>): plinth, sides, a shadowed back,
        /// floor and hat shelf, the cornice, a rail of clothes on their hangers, and maybe a folded blanket up top and a
        /// shoebox on the floor. Returns the wood colour for the doors.</summary>
        public static Color WardrobeBody(RaidMesh m, System.Random rng, Color wood)
        {
            Color c = V(wood, rng, 0.06f);
            Surf body = M(c), inside = M(c * 0.45f);
            m.Box(new Vector3(0f, 0.05f, -0.02f), new Vector3(0.98f, 0.1f, 0.54f), M(c * 0.7f));
            // subtle brass rivet strip on the top of the wardrobe carcass
            int rivetCount = 6 + rng.Next(3); // 6–8 rivets
            for (int i = 0; i < rivetCount; i++)
            {
                float x = R(rng, -0.45f, 0.45f);
                float z = R(rng, -0.02f, 0.02f);
                // small brass cylinder representing a rivet
                m.Cylinder(new Vector3(x, 1.95f, z), 0.004f, 0.008f, 8, Mt(Brass));
            }
            m.Box(new Vector3(-0.49f, 1.0f, -0.03f), new Vector3(0.02f, 1.8f, 0.54f), body);
            m.Box(new Vector3(0.49f, 1.0f, -0.03f), new Vector3(0.02f, 1.8f, 0.54f), body);
            m.Box(new Vector3(0f, 1.0f, -0.29f), new Vector3(0.96f, 1.8f, 0.02f), inside);
            m.Box(new Vector3(0f, 0.11f, -0.03f), new Vector3(0.96f, 0.02f, 0.54f), inside);
            m.Box(new Vector3(0f, 1.89f, -0.03f), new Vector3(0.96f, 0.02f, 0.54f), body);
            m.Box(new Vector3(0f, 1.93f, -0.02f), new Vector3(1.06f, 0.07f, 0.6f), M(c * 0.85f));
            m.Box(new Vector3(0f, 1.63f, -0.05f), new Vector3(0.96f, 0.016f, 0.46f), inside);

            // The rail and what hangs on it: coats, shirts, a dress, each on its hanger.
            Surf chrome = Mt(Chrome);
            m.CylinderX(new Vector3(0f, 1.53f, -0.05f), 0.012f, 0.94f, 8, chrome);
            int n = 4 + rng.Next(5);
            for (int i = 0; i < n; i++)
            {
                float x = -0.4f + i * (0.8f / (n - 1)) + R(rng, -0.015f, 0.015f);
                float len = R(rng, 0.55f, 1.1f);
                Color cloth = V(WardrobeCloths[rng.Next(WardrobeCloths.Length)], rng, 0.1f);
                m.Box(new Vector3(x, 1.47f - len * 0.5f, -0.05f), new Vector3(0.045f, len, 0.4f), Yaw(R(rng, -7f, 7f)), M(cloth));
                m.Rod(new Vector3(x, 1.47f, -0.2f), new Vector3(x, 1.5f, -0.05f), 0.008f, chrome);
                m.Rod(new Vector3(x, 1.47f, 0.1f), new Vector3(x, 1.5f, -0.05f), 0.008f, chrome);
            }
            if (Chance(rng, 0.6))
                m.Box(new Vector3(R(rng, -0.2f, 0.2f), 1.69f, -0.07f), new Vector3(0.42f, 0.1f, 0.34f), Yaw(R(rng, -5f, 5f)),
                    M(V(new Color(0.55f, 0.24f, 0.18f), rng, 0.25f)));
            if (Chance(rng, 0.7))
                CardboardBox(m, rng, new Vector3(R(rng, -0.25f, 0.25f), 0.12f, -0.06f), new Vector3(0.32f, 0.12f, 0.2f), R(rng, -10f, 10f));
            return c;
        }

        /// <summary>One of the wardrobe's doors, shut (<paramref name="left"/>: the -X one): a board with two raised panels,
        /// hung on its outer edge, and a brass knob by the edge where the doors meet.</summary>
        public static void WardrobeDoor(RaidMesh m, Color wood, bool left)
        {
            float s = left ? -1f : 1f;
            float x0 = left ? -WardrobeHalf + 0.002f : 0.003f, x1 = left ? -0.003f : WardrobeHalf - 0.002f;
            float cx = (x0 + x1) * 0.5f, w = x1 - x0;
            float cy = (WardrobeDoorY0 + WardrobeDoorY1) * 0.5f, h = WardrobeDoorY1 - WardrobeDoorY0;
            m.Box(new Vector3(cx, cy, WardrobeFront - 0.01f), new Vector3(w, h, 0.02f), M(wood * 1.06f));
            m.Box(new Vector3(cx, 1.35f, WardrobeFront + 0.003f), new Vector3(w - 0.12f, 0.6f, 0.01f), M(wood * 0.94f));
            m.Box(new Vector3(cx, 0.6f, WardrobeFront + 0.003f), new Vector3(w - 0.12f, 0.6f, 0.01f), M(wood * 0.94f));
            m.Box(new Vector3(s * 0.04f, 1.0f, WardrobeFront + 0.01f), new Vector3(0.02f, 0.08f, 0.03f), Mt(Brass));
            // top wood trim (1 cm high, spans the door width)
            m.Box(new Vector3(cx, WardrobeDoorY1 - 0.005f, WardrobeFront - 0.009f), new Vector3(w, 0.01f, 0.02f), M(wood * 0.9f));
            // bottom wood trim (1 cm high)
            m.Box(new Vector3(cx, WardrobeDoorY0 + 0.005f, WardrobeFront - 0.009f), new Vector3(w, 0.01f, 0.02f), M(wood * 0.9f));
            // side edge bevel (2 cm wide, 1 cm deep)
            m.Box(new Vector3(s * (x0 + 0.005f), (WardrobeDoorY0 + WardrobeDoorY1) * 0.5f, WardrobeFront - 0.008f), new Vector3(0.02f, WardrobeDoorY1 - WardrobeDoorY0, 0.01f), M(wood * 0.85f));
            // brass pull handle (horizontal)
            m.CylinderX(new Vector3(cx, (WardrobeDoorY0 + WardrobeDoorY1) * 0.5f, WardrobeFront + 0.015f), 0.006f, 0.12f, 8, Mt(Brass));
            // key-hole (black metal)
            m.Cylinder(new Vector3(cx + w * 0.22f, WardrobeDoorY0 + 0.25f, WardrobeFront - 0.015f), 0.003f, 0.01f, 6, M(Black));
            // wear scar (thin dark quad)
            var scarSurf = M(wood * 0.5f);
            m.Quad(new Vector3(cx - w * 0.3f, WardrobeDoorY0 + 0.12f, WardrobeFront - 0.008f), new Vector3(0.35f, 0f, 0f), new Vector3(0f, 0f, 0.001f), scarSurf);
            // Hinge pins
            if (left)
            {
                var topPin = new Vector3(x0 - 0.005f, WardrobeDoorY1 - 0.06f, WardrobeFront + 0.01f);
                var botPin = new Vector3(x0 - 0.005f, WardrobeDoorY0 + 0.06f, WardrobeFront + 0.01f);
                m.Cylinder(topPin, 0.005f, 0.02f, 8, Mt(Brass));
                m.Cylinder(botPin, 0.005f, 0.02f, 8, Mt(Brass));
            }
            else
            {
                var topPin = new Vector3(x1 + 0.005f, WardrobeDoorY1 - 0.06f, WardrobeFront + 0.01f);
                var botPin = new Vector3(x1 + 0.005f, WardrobeDoorY0 + 0.06f, WardrobeFront + 0.01f);
                m.Cylinder(topPin, 0.005f, 0.02f, 8, Mt(Brass));
                m.Cylinder(botPin, 0.005f, 0.02f, 8, Mt(Brass));
            }
        }
        #endregion

        #region Washing machine
        // A front-loading washing machine (0.6 wide, 0.9 high, 0.6 deep; front +Z): an enamel cabinet, the drum's mouth on
        // the front behind a porthole door hung on its -X side, and the controls along the back of the top.
        internal static readonly Vector3 WasherPorthole = new Vector3(0f, 0.42f, 0.29f);   // the porthole's middle, on the cabinet's front
        internal const float WasherMouth = 0.165f;                                       // the drum's mouth (radius)
        internal const float WasherRing = 0.19f;                                         // the middle of the door's chrome ring (radius)
        internal static readonly Vector3 WasherDial = new Vector3(0.17f, 0.9f, -0.145f);  // the programme dial, on the console's front

        /// <summary>The washing machine's fixed body: the cabinet on its plinth, the detergent drawer, the console with the
        /// programme dial and its buttons, and the drum's dark mouth in its rubber seal with somebody's last load showing in
        /// the bottom of it. The porthole door and the suds of a wash are drawn apart (<see cref="WasherDoor"/>,
        /// <see cref="WasherSuds"/>).</summary>
        public static void WasherBody(RaidMesh m, System.Random rng)
        {
            Color enamel = V(new Color(0.88f, 0.88f, 0.85f), rng, 0.03f);
            Surf e = G(enamel), dark = M(new Color(0.05f, 0.05f, 0.06f));
            Quaternion facing = Quaternion.Euler(90f, 0f, 0f);
            m.Box(new Vector3(0f, 0.03f, 0f), new Vector3(0.56f, 0.06f, 0.54f), M(new Color(0.16f, 0.16f, 0.17f)));
            m.Box(new Vector3(0f, 0.455f, -0.005f), new Vector3(0.6f, 0.79f, 0.59f), e);
            m.Box(new Vector3(0f, 0.86f, -0.005f), new Vector3(0.6f, 0.02f, 0.59f), G(enamel * 0.97f));
            m.Box(new Vector3(0f, 0.08f, 0.292f), new Vector3(0.54f, 0.008f, 0.004f), M(enamel * 0.8f));

            // The detergent drawer and the console along the back of the top, its dial and buttons facing the room.
            m.Box(new Vector3(-0.16f, 0.79f, 0.293f), new Vector3(0.2f, 0.07f, 0.008f), G(enamel * 0.95f));
            m.Box(new Vector3(-0.16f, 0.775f, 0.299f), new Vector3(0.08f, 0.012f, 0.006f), M(enamel * 0.7f));
            m.Box(new Vector3(0f, 0.9f, -0.22f), new Vector3(0.6f, 0.06f, 0.15f), G(enamel * 0.92f));
            m.Box(new Vector3(-0.08f, 0.9f, WasherDial.z + 0.001f), new Vector3(0.22f, 0.03f, 0.004f), M(new Color(0.25f, 0.30f, 0.38f)));
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(-0.2f + i * 0.04f, 0.9f, WasherDial.z + 0.003f), new Vector3(0.025f, 0.018f, 0.008f), M(new Color(0.75f, 0.74f, 0.70f)));
            m.Cylinder(WasherDial, 0.035f, 0.025f, 14, M(new Color(0.72f, 0.72f, 0.70f)), facing);
            m.Box(WasherDial + new Vector3(0f, 0.014f, 0.026f), new Vector3(0.006f, 0.028f, 0.003f), M(Black));

            // The drum: its grey rubber seal, the dark inside, and the last load lying in the bottom of it.
            Vector3 p = WasherPorthole;
            m.Cylinder(p, WasherMouth + 0.03f, 0.004f, 20, M(new Color(0.30f, 0.31f, 0.32f)), facing);
            m.Cylinder(p + new Vector3(0f, 0f, 0.004f), WasherMouth, 0.002f, 20, dark, facing);
            m.Cylinder(p + new Vector3(0f, 0f, 0.006f), WasherMouth * 0.55f, 0.001f, 16, Mt(new Color(0.30f, 0.31f, 0.32f)), facing);
            for (int i = 0, n = rng.Next(4); i < n; i++)
            {
                Color cloth = V(WardrobeCloths[rng.Next(WardrobeCloths.Length)], rng, 0.1f);
                var at = p + new Vector3(R(rng, -0.08f, 0.08f), -R(rng, 0.04f, 0.1f), 0.01f + i * 0.002f);
                m.Ellipsoid(at, new Vector3(R(rng, 0.12f, 0.2f), R(rng, 0.06f, 0.1f), 0.008f), Quaternion.Euler(0f, 0f, R(rng, -30f, 30f)), M(cloth), 8, 3);
            }
        }

        /// <summary>The porthole door, shut: a chrome ring round a see-through glass, and the handle on its free (+X) side.</summary>
        public static void WasherDoor(RaidMesh m)
        {
            Vector3 p = WasherPorthole + new Vector3(0f, 0f, 0.018f);
            Surf chrome = Mt(Chrome);
            const int segments = 16;
            float piece = 2f * Mathf.PI * WasherRing / segments * 1.08f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                Quaternion q = Quaternion.Euler(0f, 0f, a);
                m.Box(p + q * new Vector3(0f, WasherRing, 0f), new Vector3(piece, 0.045f, 0.03f), q, chrome);
            }
            m.Cylinder(p + new Vector3(0f, 0f, -0.006f), WasherRing - 0.02f, 0.03f, 20, BathGlass, Quaternion.Euler(90f, 0f, 0f), 0.85f);
            m.Box(p + new Vector3(WasherRing + 0.02f, 0f, 0.008f), new Vector3(0.03f, 0.09f, 0.02f), M(new Color(0.70f, 0.70f, 0.68f)));
        }

        /// <summary>A wash going round behind the glass, drawn round the porthole's middle: grey-blue water with the suds in
        /// streaks across it (the machine turns it about the porthole's axis while it runs).</summary>
        public static void WasherSuds(RaidMesh m, System.Random rng)
        {
            Quaternion facing = Quaternion.Euler(90f, 0f, 0f);
            m.Cylinder(new Vector3(0f, 0f, 0.011f), WasherMouth * 0.97f, 0.002f, 18, G(new Color(0.48f, 0.58f, 0.66f)), facing);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + R(rng, -15f, 15f);
                Quaternion q = Quaternion.Euler(0f, 0f, a);
                m.Ellipsoid(q * new Vector3(0f, R(rng, 0.04f, 0.11f), 0.014f), new Vector3(R(rng, 0.07f, 0.14f), R(rng, 0.03f, 0.05f), 0.005f), q,
                    M(new Color(0.90f, 0.92f, 0.94f)), 8, 3);
            }
        }
        #endregion
        #region Bath
        // The ranch's bath (1.6 long along X, 0.75 deep, 0.56 high; its back to the wall at -Z): a built-in enamel tub with
        // a panelled front, the taps and the spout over the drain at one end (end = +1: the +X end; -1: the -X end).
        internal const float BathLength = 1.6f, BathDepth = 0.75f, BathHeight = 0.56f;
        internal const float BathInsideX = 0.72f, BathInsideZ = 0.28f;   // half the hollow, each way
        internal const float BathFloorY = 0.1f;                         // the top of the tub's floor
        internal const float BathWaterTopY = 0.46f;                     // a full bath's water surface
        internal const float BathTapSpacing = 0.13f;                    // the taps either side of the spout

        /// <summary>The cold (or hot) tap's cross-head at the <paramref name="end"/> of the bath.</summary>
        internal static Vector3 BathTapHead(float end, bool hot) => new Vector3(end * 0.765f, BathHeight + 0.055f, hot ? BathTapSpacing : -BathTapSpacing);

        /// <summary>The mouth of the spout, where the water falls from.</summary>
        internal static Vector3 BathSpout(float end) => new Vector3(end * 0.6f, 0.585f, 0f);

        /// <summary>The drain in the tub's floor, under the spout.</summary>
        internal static Vector3 BathDrain(float end) => new Vector3(end * 0.56f, BathFloorY, 0f);

        /// <summary>The bath's fixed body: the tub's walls, floor and panelled front, the overflow, the drain, the taps'
        /// pillars and the spout, a bottle and a scrubbing brush on the back rim, and the mat on the floor in front. The
        /// taps' heads, the water, the stream and the plug are drawn apart.</summary>
        public static void BathTub(RaidMesh m, System.Random rng, float end)
        {
            Surf enamel = G(BathChina), floor = G(BathChina * 0.95f), chrome = Mt(Chrome);
            const float hx = BathLength * 0.5f, hz = BathDepth * 0.5f, h = BathHeight;
            const float wallZ = hz - BathInsideZ, wallX = hx - BathInsideX;
            m.Box(new Vector3(0f, h * 0.5f, hz - wallZ * 0.5f), new Vector3(BathLength, h, wallZ), enamel);
            m.Box(new Vector3(0f, h * 0.5f, -hz + wallZ * 0.5f), new Vector3(BathLength, h, wallZ), enamel);
            m.Box(new Vector3(hx - wallX * 0.5f, h * 0.5f, 0f), new Vector3(wallX, h, BathInsideZ * 2f), enamel);
            m.Box(new Vector3(-hx + wallX * 0.5f, h * 0.5f, 0f), new Vector3(wallX, h, BathInsideZ * 2f), enamel);
            m.Box(new Vector3(0f, BathFloorY * 0.5f, 0f), new Vector3(BathInsideX * 2f, BathFloorY, BathInsideZ * 2f), floor);
            m.Box(new Vector3(0f, h * 0.47f, hz + 0.002f), new Vector3(BathLength - 0.12f, h * 0.62f, 0.004f), G(BathChina * 0.97f));
            m.Box(new Vector3(0f, 0.03f, hz + 0.004f), new Vector3(BathLength - 0.02f, 0.06f, 0.008f), M(new Color(0.55f, 0.55f, 0.53f)));

            // The overflow, the drain, the taps' pillars and the spout reaching out over the drain.
            Quaternion inward = Quaternion.Euler(0f, 0f, end * 90f);
            m.Cylinder(new Vector3(end * BathInsideX, 0.42f, 0f), 0.028f, 0.004f, 12, chrome, inward);
            Vector3 drain = BathDrain(end);
            m.Cylinder(drain, 0.032f, 0.002f, 12, chrome);
            m.Cylinder(drain + new Vector3(0f, 0.001f, 0f), 0.022f, 0.002f, 12, M(new Color(0.12f, 0.12f, 0.13f)));
            for (int s = -1; s <= 1; s += 2)
            {
                var pillar = new Vector3(end * 0.765f, h, s * BathTapSpacing);
                m.Cylinder(pillar, 0.024f, 0.008f, 12, chrome);
                m.Cylinder(pillar, 0.016f, 0.048f, 10, chrome);
            }
            Vector3 spout = BathSpout(end);
            m.Cylinder(new Vector3(end * 0.765f, h, 0f), 0.03f, 0.008f, 12, chrome);
            m.Rod(new Vector3(end * 0.765f, h + 0.005f, 0f), new Vector3(end * 0.765f, 0.615f, 0f), 0.03f, chrome, 10);
            m.Rod(new Vector3(end * 0.765f, 0.615f, 0f), new Vector3(spout.x, 0.615f, 0f), 0.026f, chrome, 10);
            m.Cylinder(spout, 0.016f, 0.03f, 10, chrome);

            // On the back rim: a bottle of bubble bath and a scrubbing brush; on the floor in front, the bath mat.
            Color bottle = V(Chance(rng, 0.5) ? new Color(0.30f, 0.52f, 0.74f) : new Color(0.82f, 0.44f, 0.58f), rng, 0.1f);
            var b = new Vector3(-end * 0.55f, h, -hz + wallZ * 0.5f);
            m.Cylinder(b, 0.03f, 0.15f, 10, G(bottle));
            m.Cylinder(b + new Vector3(0f, 0.15f, 0f), 0.012f, 0.025f, 8, G(new Color(0.92f, 0.92f, 0.90f)));
            if (Chance(rng, 0.7))
            {
                var brush = new Vector3(-end * 0.3f, h + 0.012f, -hz + wallZ * 0.5f);
                m.Box(brush, new Vector3(0.16f, 0.024f, 0.05f), Yaw(R(rng, -10f, 10f)), M(new Color(0.55f, 0.38f, 0.20f)));
                m.Box(brush - new Vector3(0f, 0.014f, 0f), new Vector3(0.15f, 0.01f, 0.045f), Yaw(R(rng, -10f, 10f)), M(new Color(0.80f, 0.74f, 0.52f)));
            }
            Color mat = V(Chance(rng, 0.5) ? new Color(0.44f, 0.60f, 0.70f) : new Color(0.78f, 0.62f, 0.56f), rng, 0.08f);
            m.Box(new Vector3(R(rng, -0.15f, 0.15f), 0.03f, hz + 0.32f), new Vector3(0.75f, 0.012f, 0.48f), Yaw(R(rng, -6f, 6f)), M(mat));
        }

        /// <summary>A bath tap's cross-head with its red (hot) or blue (cold) cap, drawn round the origin (the bath turns it
        /// as the tap opens).</summary>
        public static void BathTapCross(RaidMesh m, bool hot) => KitchenTapHead(m, Vector3.zero, hot ? TapHotRed : TapColdBlue);

        /// <summary>The bath filling: a stream from the spout down to the tub's floor, and a splash where it lands.</summary>
        public static void BathStream(RaidMesh m, float end)
        {
            Surf water = BathWater;
            Vector3 spout = BathSpout(end);
            var foot = new Vector3(spout.x, BathFloorY + 0.004f, spout.z);
            m.Cylinder(foot, 0.009f, spout.y - foot.y, 8, water);
            m.Ellipsoid(foot, new Vector3(0.09f, 0.012f, 0.09f), water, 8, 3);
        }

        /// <summary>A full bath's water: a sheet across the whole hollow at <see cref="BathWaterTopY"/> (the bath lowers it
        /// with the level).</summary>
        public static void BathWaterSheet(RaidMesh m)
            => m.Box(new Vector3(0f, BathWaterTopY, 0f), new Vector3(BathInsideX * 2f - 0.01f, 0.004f, BathInsideZ * 2f - 0.01f), BathWater);

        /// <summary>The bath's plug on its chain from the overflow: pushed into the drain (<paramref name="inDrain"/>), or
        /// hanging over the end of the tub.</summary>
        public static void BathPlug(RaidMesh m, float end, bool inDrain)
        {
            Surf rubber = M(new Color(0.10f, 0.10f, 0.11f)), chain = Mt(Chrome);
            var anchor = new Vector3(end * (BathInsideX - 0.005f), 0.42f, 0f);
            Vector3 drain = BathDrain(end);
            Vector3 plug = inDrain ? drain + new Vector3(0f, 0.002f, 0f) : new Vector3(end * (BathInsideX - 0.02f), 0.2f, 0.06f);
            m.Cylinder(plug, 0.026f, 0.012f, 12, rubber, inDrain ? Quaternion.identity : Quaternion.Euler(0f, 0f, end * 80f));
            if (inDrain)
            {
                var mid = new Vector3(end * (BathInsideX - 0.06f), 0.2f, 0.02f);
                m.Rod(anchor, mid, 0.004f, chain);
                m.Rod(mid, plug + new Vector3(0f, 0.012f, 0f), 0.004f, chain);
            }
            else m.Rod(anchor, plug, 0.004f, chain);
        }
        #endregion

        #region The yard
        // The raid houses' yard fittings, drawn in the starter home's own frame (StarterHome.Grounds.cs) — the street toward
        // -Z, the front door toward +Z — and turned to each house's front: the rural mailbox on its post at the end of the
        // front walk, and the coir mat on the front step. The outside tap's frame puts the wall's face at the origin, the
        // way out from it toward -X.
        internal static readonly Vector3 MailboxDoorHinge = new Vector3(0f, 1.12f, -0.252f);
        internal static readonly Vector3 MailboxFlagPivot = new Vector3(0.126f, 1.18f, 0.2f);
        internal static readonly Vector3 MailboxReach = new Vector3(0f, 1.25f, -0.3f);

        /// <summary>A rural mailbox on a cedar post (its foot at the origin), the door toward -Z: post, box, the dark mouth
        /// behind the door and the boss the flag turns on. The door and the flag are drawn apart.</summary>
        public static void YardMailbox(RaidMesh m)
        {
            Surf cedar = M(new Color(0.40f, 0.28f, 0.17f));
            m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(0.09f, 1.1f, 0.09f), cedar);
            m.Box(new Vector3(0f, 1.11f, 0f), new Vector3(0.18f, 0.03f, 0.34f), cedar);
            Surf box = Mt(new Color(0.18f, 0.20f, 0.24f));
            m.Box(new Vector3(0f, 1.2f, 0f), new Vector3(0.22f, 0.16f, 0.48f), box);
            m.CylinderX(new Vector3(0f, 1.28f, 0f), 0.11f, 0.48f, 10, box, 90f);
            Surf dark = M(new Color(0.04f, 0.04f, 0.05f));
            m.Box(new Vector3(0f, 1.205f, -0.241f), new Vector3(0.19f, 0.15f, 0.002f), dark);
            m.Cylinder(new Vector3(0f, 1.28f, -0.242f), 0.095f, 0.002f, 12, dark, Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(new Vector3(0.11f, 1.18f, 0.2f), 0.014f, 0.012f, 8, box, Quaternion.Euler(0f, 0f, -90f));
            // Snow on the box's round top.
            m.Ellipsoid(new Vector3(0f, 1.385f, 0f), new Vector3(0.17f, 0.04f, 0.44f), M(DecorProps.SnowWhite), 8, 3);
        }

        /// <summary>The mailbox's door, shut: the box's profile (a square foot under a round top) with its latch.</summary>
        public static void YardMailboxDoor(RaidMesh m)
        {
            Surf face = Mt(new Color(0.24f, 0.26f, 0.30f));
            m.Box(new Vector3(0f, 1.2f, -0.252f), new Vector3(0.226f, 0.16f, 0.012f), face);
            m.Cylinder(new Vector3(0f, 1.28f, -0.258f), 0.113f, 0.012f, 12, face, Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(0f, 1.35f, -0.262f), new Vector3(0.04f, 0.018f, 0.01f), Mt(new Color(0.55f, 0.55f, 0.52f)));
        }

        /// <summary>The mailbox's red flag, down: its arm lying forward along the box's side from the boss.</summary>
        public static void YardMailboxFlag(RaidMesh m)
        {
            Surf red = M(new Color(0.72f, 0.12f, 0.10f));
            Vector3 pivot = MailboxFlagPivot;
            m.Box(pivot + new Vector3(0f, 0f, -0.12f), new Vector3(0.008f, 0.016f, 0.26f), red);
            m.Box(pivot + new Vector3(0f, 0.022f, -0.215f), new Vector3(0.008f, 0.06f, 0.07f), red);
        }

        internal const float DoormatWidth = 0.86f, DoormatDepth = 0.46f, DoormatThick = 0.012f;

        /// <summary>What lies under the doormat (its underside's middle at the origin): a square of pale, damp concrete and a
        /// dead beetle, hidden inside the mat until it's lifted.</summary>
        public static void UnderDoormat(RaidMesh m)
        {
            m.Flat(new Vector3(0f, 0.0015f, 0f), DoormatWidth - 0.02f, DoormatDepth - 0.02f, 0f, M(new Color(0.47f, 0.46f, 0.42f)));
            m.Ellipsoid(new Vector3(0.14f, 0.004f, -0.07f), new Vector3(0.024f, 0.007f, 0.016f), G(new Color(0.10f, 0.07f, 0.05f)), 6, 3);
        }

        /// <summary>The coir welcome mat (its underside's middle at the origin), dusted with snow off somebody's boots.</summary>
        public static void Doormat(RaidMesh m, System.Random rng)
        {
            const float w = DoormatWidth, d = DoormatDepth, t = DoormatThick;
            m.Box(new Vector3(0f, t * 0.5f, 0f), new Vector3(w, t, d), M(new Color(0.16f, 0.13f, 0.10f)));
            m.Flat(new Vector3(0f, t + 0.0015f, 0f), w - 0.07f, d - 0.07f, 0f, M(new Color(0.46f, 0.34f, 0.18f)));
            Surf welcome = DecorMaterials.Sign("WELCOME", new Color(0.46f, 0.34f, 0.18f), new Color(0.16f, 0.11f, 0.07f), out float aspect);
            m.FlatUV(new Vector3(0f, t + 0.003f, 0f), 0.7f, 0.7f / aspect, 0f, welcome, new Rect(0f, 0f, 1f, 1f));
            m.FlatBlob(new Vector3(R(rng, -0.2f, 0.2f), t + 0.004f, R(rng, -0.08f, 0.08f)), 0.16f, 0.1f, R(rng, 0f, 360f), 0.4f, rng,
                M(DecorProps.SnowShade));
        }

        // The outside tap (the starter home's, StarterHome.Grounds.cs): a standpipe just off the wall with its brass body,
        // and a hose reel beside it. Its frame puts the wall's face at the origin, the way out from it toward -X.
        internal static readonly Vector3 HoseTapBody = new Vector3(-0.07f, 0.63f, 0f);
        private static readonly Vector3 HoseReel = new Vector3(-0.35f, 0f, 0f);

        /// <summary>The outside tap and its hose: a standpipe with a brass body, its spout and coupling, a green hose coiled
        /// on a reel (with snow on it) and run out across the ground to a brass nozzle. Returns the coupling (where the water
        /// comes out); <paramref name="nozzle"/> and <paramref name="nozzleDir"/> are where the hose ends and which way it
        /// points (for <see cref="HoseTrickle"/>). The wheel is drawn apart (<see cref="HoseTapWheel"/>).</summary>
        public static Vector3 HoseTap(RaidMesh m, out Vector3 nozzle, out Vector3 nozzleDir)
        {
            Surf frame = Mt(new Color(0.22f, 0.24f, 0.22f));
            Surf hose = G(new Color(0.16f, 0.42f, 0.18f));
            Surf brass = Mt(Brass);
            Vector3 at = HoseReel;
            m.Box(at + new Vector3(-0.2f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f), frame);
            m.Box(at + new Vector3(0.2f, 0.25f, 0f), new Vector3(0.04f, 0.5f, 0.04f), frame);
            m.CylinderX(at + new Vector3(0f, 0.36f, 0f), 0.04f, 0.44f, 8, frame);
            m.CylinderX(at + new Vector3(0f, 0.36f, 0f), 0.2f, 0.3f, 14, hose);
            m.Ellipsoid(at + new Vector3(0f, 0.565f, 0f), new Vector3(0.28f, 0.05f, 0.3f), M(DecorProps.SnowWhite), 8, 3);
            Vector3 end = at + new Vector3(-0.6f, 0.01f, 0.45f);
            m.Rod(at + new Vector3(-0.1f, 0.2f, 0.1f), end, 0.035f, hose, 6);
            nozzleDir = new Vector3(-0.5f, 0f, 0.35f).normalized;
            nozzle = end + nozzleDir * 0.08f;
            m.Rod(end, nozzle, 0.03f, brass, 8);

            // The standpipe, its tap and spout, and the short length of hose from the spout down onto the reel.
            Vector3 body = HoseTapBody, pipe = new Vector3(body.x, 0f, body.z);
            m.Cylinder(pipe, 0.016f, 0.6f, 8, Mt(new Color(0.52f, 0.54f, 0.53f)));
            Vector3 elbow = body + new Vector3(-0.065f, 0f, 0f);
            m.Box(body, new Vector3(0.045f, 0.04f, 0.045f), brass);
            m.Rod(body, elbow, 0.022f, brass, 8);
            m.Rod(elbow, elbow - Vector3.up * 0.045f, 0.022f, brass, 8);
            Vector3 coupling = elbow - Vector3.up * 0.075f;
            m.Cylinder(coupling, 0.017f, 0.03f, 8, brass);
            m.Cylinder(coupling + Vector3.up * 0.018f, 0.018f, 0.004f, 12, brass);
            m.Rod(coupling, at + new Vector3(0.12f, 0.52f, 0f), 0.03f, hose, 6);
            m.Cylinder(body + Vector3.up * 0.018f, 0.005f, 0.016f, 6, brass);
            return coupling;
        }

        /// <summary>The outside tap's red wheel (a disc with a spoke and a knob) on the tap's body; it turns anticlockwise
        /// (seen from above) to open.</summary>
        public static void HoseTapWheel(RaidMesh m)
        {
            Surf red = M(new Color(0.66f, 0.12f, 0.09f));
            Vector3 hub = HoseTapBody + Vector3.up * 0.032f;
            m.Cylinder(hub, 0.032f, 0.01f, 12, red);
            m.Box(hub + Vector3.up * 0.012f, new Vector3(0.058f, 0.006f, 0.01f), M(new Color(0.42f, 0.07f, 0.05f)));
            m.Ellipsoid(hub + new Vector3(0.024f, 0.017f, 0f), new Vector3(0.012f, 0.014f, 0.012f), red, 6, 4);
        }

        /// <summary>Water trickling from the hose's nozzle: a glistening run, and a grey patch where it's melted the snow.</summary>
        public static void HoseTrickle(RaidMesh m, Vector3 nozzle, Vector3 dir)
        {
            var splash = new System.Random(77);
            Vector3 pool = nozzle + dir * 0.16f;
            m.FlatBlob(new Vector3(pool.x, 0.009f, pool.z), 0.26f, 0.2f, 35f, 0.3f, splash, M(new Color(0.46f, 0.50f, 0.56f)));
            m.FlatBlob(new Vector3(pool.x, 0.011f, pool.z), 0.15f, 0.11f, 35f, 0.3f, splash, G(new Color(0.56f, 0.64f, 0.70f)));
            m.Rod(nozzle, nozzle + dir * 0.12f - Vector3.up * 0.01f, 0.012f, G(new Color(0.56f, 0.66f, 0.72f)), 6);
        }
        /// <summary>An enamelled house-number plate on a wall face (<paramref name="at"/>, facing <paramref name="normal"/>).</summary>
        public static void HouseNumberPlate(RaidMesh m, Vector3 at, Vector3 normal, string number)
        {
            Surf s = DecorMaterials.Sign(number, new Color(0.86f, 0.85f, 0.80f), new Color(0.10f, 0.12f, 0.20f), out float aspect);
            float h = 0.1f, w = h * aspect;
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Box(at + normal * 0.008f, new Vector3(w + 0.03f, h + 0.03f, 0.016f), q, M(new Color(0.10f, 0.12f, 0.20f)));
            m.Upright(at + normal * 0.0175f, normal, w, h, s, new Rect(0f, 0f, 1f, 1f));
        }
        #endregion
    }
}
