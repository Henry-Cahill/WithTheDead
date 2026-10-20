using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        #region Bathroom
        // The starter home's bathroom, drawn for its working fixtures: each piece's static body, and apart from it the
        // parts that move (seat, lids, lever, tap handle, door) and the water the fixtures show and hide. Furniture is
        // drawn in its local frame (front toward +Z, origin on the floor at the piece's centre, the wall behind at -Z);
        // the wall fittings (mirror cabinet, towel rail) in world space off the wall face. Curved china is lofted from
        // oval rings (BathRing), faceted like the rest of the house's low-poly props.

        /// <summary>Glazed white china: the toilet, the basin, the shower tray.</summary>
        public static readonly Color BathChina = new Color(0.93f, 0.93f, 0.91f);
        private static readonly Color BathSeatWhite = new Color(0.96f, 0.95f, 0.92f);
        private static readonly Color BathCabinetWhite = new Color(0.92f, 0.92f, 0.90f);
        private static readonly Color BathMirror = new Color(0.84f, 0.89f, 0.91f);

        // ---- Toilet (local frame; the wall at z -0.35) ----
        /// <summary>The toilet's seat and lid hinge: both turn about the same line, behind the seat.</summary>
        internal static readonly Vector3 ToiletSeatHinge = new Vector3(0f, 0.404f, -0.135f);
        /// <summary>The cistern lid's top back edge, on which it's stood up against the wall when lifted off.</summary>
        internal static readonly Vector3 ToiletCisternLidHinge = new Vector3(0f, 0.80f, -0.346f);
        /// <summary>The flush lever's spindle, on the cistern's +X side.</summary>
        internal static readonly Vector3 ToiletLeverPivot = new Vector3(0.205f, 0.70f, -0.225f);
        /// <summary>The middle of a full cistern's water; it sinks <see cref="ToiletCisternDepth"/> as it empties.</summary>
        internal static readonly Vector3 ToiletCisternWater = new Vector3(0f, 0.74f, -0.2675f);
        internal const float ToiletCisternDepth = 0.30f;
        /// <summary>Where the hips rest sitting on the closed lid.</summary>
        internal static readonly Vector3 ToiletSitPoint = new Vector3(0f, 0.44f, 0.04f);
        /// <summary>Where the survivor reaches: the front of the seat.</summary>
        internal static readonly Vector3 ToiletReach = new Vector3(0f, 0.46f, 0.22f);

        // ---- Basin (local frame; the wall at z -0.225) ----
        /// <summary>The mouth of the mixer's spout, where the stream starts.</summary>
        internal static readonly Vector3 BasinSpout = new Vector3(0f, 0.918f, -0.045f);
        /// <summary>The mixer lever's pivot, on top of the tap's pillar.</summary>
        internal static readonly Vector3 BasinLeverPivot = new Vector3(0f, 0.965f, -0.165f);
        /// <summary>The middle of a full basin's water surface; it sinks <see cref="BasinWaterDepth"/> to the bowl's floor,
        /// narrowing with the hollow (<see cref="BasinWaterScale"/>).</summary>
        internal static readonly Vector3 BasinWaterTop = new Vector3(0f, 0.845f, 0.028f);
        internal const float BasinWaterDepth = 0.068f;
        /// <summary>Where the survivor reaches: over the front of the bowl.</summary>
        internal static readonly Vector3 BasinReach = new Vector3(0f, 0.90f, 0.06f);
        // The basin's hollow, from the rim down to the floor of the bowl, and the size of a full basin's water sheet.
        private static readonly BathRing BasinRim = new BathRing(0.86f, 0.17f, 0.125f, 0.025f);
        private static readonly BathRing BasinMid = new BathRing(0.82f, 0.155f, 0.11f, 0.03f);
        private static readonly BathRing BasinFloor = new BathRing(0.772f, 0.10f, 0.07f, 0.035f);
        private const float BasinSheetRx = 0.158f, BasinSheetRz = 0.114f;

        // ---- Shower (local frame; the walls at z -0.45 and x +0.45, open toward +Z) ----
        /// <summary>The glass door's hinge line, at its edge by the corner post (the door spans x -0.43..+0.02).</summary>
        internal static readonly Vector3 ShowerDoorHinge = new Vector3(-0.43f, 1.0f, 0.44f);
        /// <summary>The mixer lever's pivot on the back wall.</summary>
        internal static readonly Vector3 ShowerLeverPivot = new Vector3(-0.12f, 1.10f, -0.40f);
        /// <summary>The middle of the shower rose's face.</summary>
        internal static readonly Vector3 ShowerRose = new Vector3(0.10f, 1.95f, -0.27f);
        /// <summary>Where the survivor stands under the water (the middle of the stall, inside the glass and tiles).</summary>
        internal static readonly Vector3 ShowerStandIn = new Vector3(-0.01f, 0f, 0.01f);
        /// <summary>A clear spot in front of the stall to step out onto when there's nowhere better.</summary>
        internal static readonly Vector3 ShowerStepOut = new Vector3(-0.45f, 0f, 0.90f);
        /// <summary>Where the survivor reaches: the door's opening, at about waist height.</summary>
        internal static readonly Vector3 ShowerReach = new Vector3(-0.22f, 1.10f, 0.47f);
        /// <summary>Half the stall's inside (glass to tiles), each way.</summary>
        internal const float ShowerInsideHalf = 0.43f;

        private static Material s_BathGlass, s_BathWater;

        /// <summary>Clear glass for the shower's panels and door: a faint aqua sheen you see straight through.</summary>
        public static Surf BathGlass => new Surf(BathClear(ref s_BathGlass, "BathGlass", new Color(0.80f, 0.92f, 0.94f, 0.2f), 0.96f), Vector2.zero, 0f);

        /// <summary>Water — a running tap, the shower's spray, a basin or cistern full: pale and see-through.</summary>
        public static Surf BathWater => new Surf(BathClear(ref s_BathWater, "BathWater", new Color(0.66f, 0.82f, 0.90f, 0.45f), 0.92f), Vector2.zero, 0f);

        // A see-through lit material (alpha-blended; it casts no shadow and writes no depth), made once.
        private static Material BathClear(ref Material slot, string name, Color tint, float smoothness)
        {
            if (slot != null) return slot;
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var mat = new Material(sh) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset };
            BathSet(mat, "_Surface", 1f);
            BathSet(mat, "_Blend", 0f);
            BathSet(mat, "_BlendModePreserveSpecular", 0f);
            BathSet(mat, "_SrcBlend", (float)BlendMode.SrcAlpha);
            BathSet(mat, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            BathSet(mat, "_SrcBlendAlpha", (float)BlendMode.One);
            BathSet(mat, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            BathSet(mat, "_ZWrite", 0f);
            BathSet(mat, "_Smoothness", smoothness);
            BathSet(mat, "_Metallic", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.SetShaderPassEnabled("DepthOnly", false);
            mat.SetShaderPassEnabled("DepthNormals", false);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            slot = mat;
            return mat;
        }

        private static void BathSet(Material mat, string property, float value)
        {
            if (mat.HasProperty(property)) mat.SetFloat(property, value);
        }

        #region Lofted china
        private enum BathFacing { Out, In, Up, Down }

        // One oval ring of a lofted shape: its height, half-widths across (X) and front to back (Z), the centre's offset
        // along Z, and how square its outline is (1 an ellipse; lower is squarer — a basin's rounded oblong).
        private readonly struct BathRing
        {
            public readonly float Y, Rx, Rz, Z, Square;

            public BathRing(float y, float rx, float rz, float z = 0f, float square = 1f)
            {
                Y = y;
                Rx = rx;
                Rz = rz;
                Z = z;
                Square = square;
            }

            /// <summary>The ring shrunk to its middle, to close a disc.</summary>
            public BathRing Centre => new BathRing(Y, 0f, 0f, Z);

            public Vector3 At(float a)
            {
                float s = Mathf.Sin(a), c = Mathf.Cos(a);
                if (Square < 0.999f)
                {
                    s = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), Square);
                    c = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), Square);
                }
                return new Vector3(s * Rx, Y, Z + c * Rz);
            }
        }

        // A triangle shown from the side `facing` points to, whichever way round its corners come.
        private static void BathTri(RaidMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 facing, in Surf s)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, facing) >= 0f) m.Tri(a, b, c, s);
            else m.Tri(a, c, b, s);
        }

        // The band between two rings, seen from outside (or from inside — a bowl's hollow), or a flat ring seen from above
        // or below. Both rings are sampled at the same angles, so a band can run from an oblong to an ellipse.
        private static void BathBand(RaidMesh m, in BathRing a, in BathRing b, int sides, in Surf s, BathFacing facing)
        {
            float step = Mathf.PI * 2f / sides;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * step, a1 = a0 + step, mid = a0 + step * 0.5f;
                Vector3 p0 = a.At(a0), p1 = a.At(a1), q0 = b.At(a0), q1 = b.At(a1);
                Vector3 f = facing switch
                {
                    BathFacing.Up => Vector3.up,
                    BathFacing.Down => Vector3.down,
                    BathFacing.In => new Vector3(-Mathf.Sin(mid), 0f, -Mathf.Cos(mid)),
                    _ => new Vector3(Mathf.Sin(mid), 0f, Mathf.Cos(mid)),
                };
                BathTri(m, p0, p1, q1, f, s);
                BathTri(m, p0, q1, q0, f, s);
            }
        }

        private static void BathLoft(RaidMesh m, BathRing[] rings, int sides, in Surf s, BathFacing facing)
        {
            for (int i = 0; i + 1 < rings.Length; i++) BathBand(m, rings[i], rings[i + 1], sides, s, facing);
        }
        #endregion

        #region Toilet
        /// <summary>A close-coupled toilet's china (0.42 × 0.80 × 0.70, its back to the wall at -Z): a waisted pan with a
        /// flat rim round the bowl's hollow and its water, the boxed trap under a cistern that's open at the top (its float
        /// and pipes show when the lid's off), the seat's chrome hinges, the lever's rose and the cold feed from the wall.
        /// The seat, lid, cistern lid, lever and the cistern's water are drawn apart (<see cref="ToiletSeat"/>,
        /// <see cref="ToiletLid"/>, <see cref="ToiletCisternLid"/>, <see cref="ToiletLever"/>, <see cref="ToiletCisternSheet"/>).</summary>
        public static void ToiletBody(RaidMesh m)
        {
            Surf china = G(BathChina), hollow = G(BathChina * 0.9f), chrome = Mt(Chrome);

            // The pan, from its foot up to the rim, the rim's flat top and the hollow down to the water.
            var rim = new BathRing(0.39f, 0.18f, 0.245f, 0.08f);
            BathLoft(m, new[]
            {
                new BathRing(0f, 0.115f, 0.16f, 0.03f),
                new BathRing(0.14f, 0.12f, 0.17f, 0.035f),
                new BathRing(0.26f, 0.17f, 0.23f, 0.07f),
                new BathRing(0.35f, 0.185f, 0.25f, 0.08f),
                rim,
            }, 24, china, BathFacing.Out);
            var lip = new BathRing(0.39f, 0.125f, 0.17f, 0.095f);
            var water = new BathRing(0.29f, 0.08f, 0.10f, 0.105f);
            BathBand(m, rim, lip, 24, china, BathFacing.Up);
            BathLoft(m, new[] { lip, new BathRing(0.335f, 0.10f, 0.135f, 0.10f), water }, 24, hollow, BathFacing.In);
            BathBand(m, water, water.Centre, 24, G(new Color(0.58f, 0.70f, 0.72f)), BathFacing.Up);

            // The trap's boxed back under the cistern, and the neck joining it to the pan.
            m.Box(new Vector3(0f, 0.2f, -0.265f), new Vector3(0.26f, 0.4f, 0.15f), china);
            m.Box(new Vector3(0f, 0.3f, -0.19f), new Vector3(0.22f, 0.18f, 0.07f), china);

            // The cistern, open at the top under its lid.
            const float cx = 0.19f, z0 = -0.345f, z1 = -0.19f, y0 = 0.40f, y1 = 0.775f, t = 0.016f;
            float d = z1 - z0, h = y1 - y0, zm = (z0 + z1) * 0.5f, ym = (y0 + y1) * 0.5f;
            m.Box(new Vector3(0f, y0 + t * 0.5f, zm), new Vector3(cx * 2f, t, d), china);
            m.Box(new Vector3(0f, ym, z0 + t * 0.5f), new Vector3(cx * 2f, h, t), china);
            m.Box(new Vector3(0f, ym, z1 - t * 0.5f), new Vector3(cx * 2f, h, t), china);
            for (int s = -1; s <= 1; s += 2)
                m.Box(new Vector3(s * (cx - t * 0.5f), ym, zm), new Vector3(t, h, d - t * 2f), china);

            // Its innards: the fill valve with its float, and the overflow pipe.
            Surf plastic = M(new Color(0.42f, 0.44f, 0.46f));
            m.Cylinder(new Vector3(-0.12f, y0 + t, -0.27f), 0.017f, 0.29f, 8, plastic);
            m.Rod(new Vector3(-0.12f, 0.67f, -0.27f), new Vector3(-0.04f, 0.67f, -0.25f), 0.008f, plastic);
            m.Ellipsoid(new Vector3(-0.02f, 0.67f, -0.25f), new Vector3(0.06f, 0.05f, 0.06f), M(new Color(0.20f, 0.22f, 0.24f)), 8, 4);
            m.Cylinder(new Vector3(0.09f, y0 + t, -0.28f), 0.014f, 0.27f, 8, G(new Color(0.86f, 0.86f, 0.84f)));

            // The seat's hinges, the lever's rose on the cistern's side, and the cold feed from the wall with its stop tap.
            for (int s = -1; s <= 1; s += 2)
                m.Box(new Vector3(s * 0.075f, 0.398f, -0.15f), new Vector3(0.03f, 0.014f, 0.034f), chrome);
            m.Cylinder(new Vector3(cx, 0.70f, -0.225f), 0.022f, 0.006f, 12, chrome, Quaternion.Euler(0f, 0f, -90f));
            var feed0 = new Vector3(-0.15f, y0 + 0.01f, -0.31f);
            var feed1 = new Vector3(-0.15f, 0.2f, -0.31f);
            var feed2 = new Vector3(-0.15f, 0.2f, -0.349f);
            m.Rod(feed0, feed1, 0.014f, chrome, 6);
            m.Rod(feed1, feed2, 0.014f, chrome, 6);
            m.Box(new Vector3(-0.15f, 0.2f, -0.326f), new Vector3(0.026f, 0.026f, 0.02f), chrome);
            m.Box(new Vector3(-0.15f, 0.226f, -0.326f), new Vector3(0.008f, 0.03f, 0.008f), chrome);
        }

        /// <summary>The toilet seat: a white oval ring resting on the rim on two buffers, its back edge on the hinge line.</summary>
        public static void ToiletSeat(RaidMesh m)
        {
            Surf seat = G(BathSeatWhite);
            var outTop = new BathRing(0.415f, 0.18f, 0.235f, 0.10f);
            var inTop = new BathRing(0.415f, 0.115f, 0.155f, 0.11f);
            var outBottom = new BathRing(0.393f, 0.176f, 0.231f, 0.10f);
            var inBottom = new BathRing(0.393f, 0.119f, 0.159f, 0.11f);
            BathBand(m, outTop, inTop, 24, seat, BathFacing.Up);
            BathBand(m, outBottom, inBottom, 24, seat, BathFacing.Down);
            BathBand(m, outBottom, outTop, 24, seat, BathFacing.Out);
            BathBand(m, inBottom, inTop, 24, seat, BathFacing.In);
            for (int s = -1; s <= 1; s += 2)
                m.Box(new Vector3(s * 0.12f, 0.3915f, 0.2f), new Vector3(0.025f, 0.003f, 0.035f), M(new Color(0.75f, 0.75f, 0.73f)));
        }

        /// <summary>The toilet lid: a shallow oval cover over the seat, hinged on the same line.</summary>
        public static void ToiletLid(RaidMesh m)
        {
            Surf lid = G(BathSeatWhite);
            var top = new BathRing(0.436f, 0.176f, 0.231f, 0.10f);
            var bottom = new BathRing(0.416f, 0.182f, 0.237f, 0.10f);
            BathBand(m, top, top.Centre, 24, lid, BathFacing.Up);
            BathBand(m, bottom, bottom.Centre, 24, lid, BathFacing.Down);
            BathBand(m, bottom, top, 24, lid, BathFacing.Out);
        }

        /// <summary>The cistern's heavy china lid.</summary>
        public static void ToiletCisternLid(RaidMesh m)
            => m.Box(new Vector3(0f, 0.7875f, -0.265f), new Vector3(0.40f, 0.025f, 0.162f), G(BathChina));

        /// <summary>The flush lever on the cistern's side: a spindle boss and a short arm reaching forward past the cistern.</summary>
        public static void ToiletLever(RaidMesh m)
        {
            Surf chrome = Mt(Chrome);
            m.Box(new Vector3(0.204f, 0.70f, -0.225f), new Vector3(0.016f, 0.016f, 0.016f), chrome);
            m.Box(new Vector3(0.209f, 0.70f, -0.185f), new Vector3(0.008f, 0.012f, 0.07f), chrome);
            m.Box(new Vector3(0.209f, 0.70f, -0.148f), new Vector3(0.011f, 0.02f, 0.016f), chrome);
            m.Cylinder(ToiletLeverPivot, 0.008f, 0.004f, 12, chrome);
        }

        /// <summary>A full cistern's water: a sheet round <see cref="ToiletCisternWater"/>.</summary>
        public static void ToiletCisternSheet(RaidMesh m)
            => m.Box(ToiletCisternWater, new Vector3(0.342f, 0.004f, 0.118f), BathWater);
        #endregion

        #region Basin
        /// <summary>A pedestal wash basin (0.52 × 0.90 × 0.45, its back to the wall at -Z): an oval column under a rounded
        /// oblong of china whose flat rim runs round a deep hollow with a chrome drain, a mixer tap on the back deck, a bar
        /// of soap and a tumbler of toothbrushes. The tap's lever, the plug, the stream and the water are drawn apart
        /// (<see cref="BasinLever"/>, <see cref="BasinPlug"/>, <see cref="BasinStream"/>, <see cref="BasinWaterSheet"/>).</summary>
        public static void PedestalBasin(RaidMesh m, System.Random rng)
        {
            Surf china = G(BathChina), hollow = G(BathChina * 0.92f), chrome = Mt(Chrome);

            BathLoft(m, new[]
            {
                new BathRing(0f, 0.105f, 0.085f, -0.085f),
                new BathRing(0.06f, 0.085f, 0.07f, -0.09f),
                new BathRing(0.5f, 0.075f, 0.062f, -0.1f),
                new BathRing(0.71f, 0.1f, 0.085f, -0.1f),
            }, 16, china, BathFacing.Out);

            // The bowl: its rounded underside, the flat rim and the hollow down to the drain.
            var under = new BathRing(0.70f, 0.20f, 0.165f, -0.06f, 0.6f);
            var belly = new BathRing(0.80f, 0.245f, 0.195f, -0.03f, 0.6f);
            var lip = new BathRing(0.86f, 0.25f, 0.20f, -0.025f, 0.6f);
            BathBand(m, under, under.Centre, 20, china, BathFacing.Down);
            BathBand(m, under, belly, 20, china, BathFacing.Out);
            BathBand(m, belly, lip, 20, china, BathFacing.Out);
            BathBand(m, lip, BasinRim, 20, china, BathFacing.Up);
            BathBand(m, BasinRim, BasinMid, 20, hollow, BathFacing.In);
            BathBand(m, BasinMid, BasinFloor, 20, hollow, BathFacing.In);
            BathBand(m, BasinFloor, BasinFloor.Centre, 20, hollow, BathFacing.Up);
            m.Cylinder(new Vector3(0f, 0.772f, 0.035f), 0.022f, 0.002f, 12, chrome);
            m.Cylinder(new Vector3(0f, 0.7735f, 0.035f), 0.011f, 0.001f, 10, M(new Color(0.12f, 0.12f, 0.13f)));

            // The mixer: a chrome pillar on the back deck, its spout reaching out over the bowl.
            var tap = new Vector3(0f, 0.86f, -0.165f);
            m.Cylinder(tap, 0.026f, 0.008f, 14, chrome);
            m.Cylinder(tap, 0.019f, 0.10f, 14, chrome);
            m.Rod(new Vector3(0f, 0.935f, -0.158f), new Vector3(0f, 0.935f, -0.05f), 0.02f, chrome, 10);
            m.Cylinder(BasinSpout, 0.012f, 0.022f, 10, chrome);

            // A bar of soap on one side of the tap, a tumbler of toothbrushes on the other.
            Color[] soaps = { new Color(0.90f, 0.72f, 0.70f), new Color(0.74f, 0.86f, 0.70f), new Color(0.92f, 0.88f, 0.70f) };
            m.Ellipsoid(new Vector3(-0.175f, 0.872f, -0.165f), new Vector3(0.075f, 0.026f, 0.048f), G(soaps[rng.Next(soaps.Length)]), 10, 4);
            var cup = new Vector3(0.175f, 0.86f, -0.165f);
            m.Cylinder(cup, 0.027f, 0.085f, 12, G(V(new Color(0.56f, 0.74f, 0.80f), rng, 0.1f)), Quaternion.identity, 1.12f);
            Color[] brushes = { new Color(0.80f, 0.20f, 0.18f), new Color(0.20f, 0.42f, 0.78f), new Color(0.24f, 0.62f, 0.34f) };
            int first = rng.Next(brushes.Length);
            for (int i = 0; i < 2; i++)
            {
                Vector3 b0 = cup + new Vector3(i == 0 ? -0.008f : 0.009f, 0.03f, i == 0 ? 0.005f : -0.006f);
                Vector3 b1 = b0 + new Vector3(i == 0 ? -0.03f : 0.025f, 0.15f, i == 0 ? 0.01f : -0.012f);
                m.Rod(b0, b1, 0.009f, G(brushes[(first + i) % brushes.Length]));
                m.Box(b1 + new Vector3(0f, -0.012f, 0.007f), new Vector3(0.012f, 0.026f, 0.012f), M(new Color(0.92f, 0.92f, 0.90f)));
            }
        }

        /// <summary>The mixer's lever, lying flat over the pillar when off (it lifts at the front to run), with its
        /// hot and cold dots.</summary>
        public static void BasinLever(RaidMesh m)
        {
            Surf chrome = Mt(Chrome);
            m.Cylinder(new Vector3(0f, 0.958f, -0.165f), 0.017f, 0.014f, 12, chrome);
            m.Box(new Vector3(0f, 0.968f, -0.128f), new Vector3(0.012f, 0.009f, 0.075f), chrome);
            m.Box(new Vector3(-0.005f, 0.9725f, -0.165f), new Vector3(0.006f, 0.002f, 0.006f), M(new Color(0.75f, 0.12f, 0.10f)));
            m.Box(new Vector3(0.005f, 0.9725f, -0.165f), new Vector3(0.006f, 0.002f, 0.006f), M(new Color(0.15f, 0.30f, 0.75f)));
        }

        /// <summary>The basin's rubber plug on its chain from beside the tap: pushed into the drain (<paramref name="inDrain"/>),
        /// the chain running over the back of the rim, or lying out on the deck.</summary>
        public static void BasinPlug(RaidMesh m, bool inDrain)
        {
            Surf rubber = M(new Color(0.10f, 0.10f, 0.11f)), chain = Mt(Chrome);
            var anchor = new Vector3(0.03f, 0.86f, -0.14f);
            Vector3 plug = inDrain ? new Vector3(0f, 0.774f, 0.035f) : new Vector3(-0.08f, 0.8605f, -0.15f);
            m.Cylinder(plug, 0.02f, 0.009f, 12, rubber);
            Vector3 loop = plug + new Vector3(0f, 0.013f, 0f);
            m.Box(loop, new Vector3(0.01f, 0.008f, 0.003f), chain);
            m.Cylinder(anchor, 0.005f, 0.012f, 8, chain);
            Vector3 top = anchor + new Vector3(0f, 0.012f, 0f);
            if (inDrain)
            {
                var over = new Vector3(0.024f, 0.866f, -0.097f);
                var down = new Vector3(0.012f, 0.8f, -0.058f);
                m.Rod(top, over, 0.004f, chain);
                m.Rod(over, down, 0.004f, chain);
                m.Rod(down, loop, 0.004f, chain);
            }
            else m.Rod(top, loop, 0.004f, chain);
        }

        /// <summary>The tap running: a thin stream from the spout down into the bowl, and its splash.</summary>
        public static void BasinStream(RaidMesh m)
        {
            Surf water = BathWater;
            var foot = new Vector3(BasinSpout.x, 0.785f, BasinSpout.z);
            m.Cylinder(foot, 0.0055f, BasinSpout.y - foot.y, 8, water);
            m.Ellipsoid(foot, new Vector3(0.04f, 0.008f, 0.04f), water, 8, 3);
        }

        /// <summary>A full basin's water: an oval sheet round <see cref="BasinWaterTop"/> (the basin sinks and narrows it).</summary>
        public static void BasinWaterSheet(RaidMesh m)
        {
            var top = new BathRing(BasinWaterTop.y, BasinSheetRx, BasinSheetRz, BasinWaterTop.z);
            BathBand(m, top, top.Centre, 20, BathWater, BathFacing.Up);
        }

        /// <summary>How much the basin's water sheet is scaled across at <paramref name="level"/> (0 empty .. 1 full) to fit
        /// the hollow, which narrows toward the drain.</summary>
        internal static float BasinWaterScale(float level)
        {
            float y = BasinWaterTop.y - (1f - Mathf.Clamp01(level)) * BasinWaterDepth;
            bool low = y < BasinMid.Y;
            BathRing a = low ? BasinFloor : BasinMid, b = low ? BasinMid : BasinRim;
            float t = Mathf.InverseLerp(a.Y, b.Y, y);
            float rx = Mathf.Lerp(a.Rx, b.Rx, t), rz = Mathf.Lerp(a.Rz, b.Rz, t);
            return Mathf.Min(rx / BasinSheetRx, rz / BasinSheetRz) * 0.96f;
        }
        #endregion

        #region Shower
        /// <summary>A corner shower stall (0.90 × 2.05 × 0.90; its back and +X side stand against the room's walls, its
        /// front open toward +Z): a low white tray round a mosaic floor and a chrome drain, tile boards up the two walls,
        /// a glass side panel on -X and a fixed glass panel on the front beside the door's opening, chrome channels,
        /// rails and a corner post, a riser rail with a wide rose on its arm, the mixer's plate and a wire corner shelf
        /// of bottles. The door, the mixer's lever and the spray are drawn apart (<see cref="ShowerDoor"/>,
        /// <see cref="ShowerLever"/>, <see cref="ShowerSpray"/>).</summary>
        public static void ShowerStall(RaidMesh m, System.Random rng)
        {
            Surf tray = G(BathChina), chrome = Mt(Chrome), glass = BathGlass;
            Surf tiles = DecorMaterials.Planar(DecorTextures.Linoleum(new Color(0.86f, 0.93f, 0.93f), new Color(0.66f, 0.80f, 0.82f), false), 0.6f, 0.55f);
            Surf mosaic = DecorMaterials.Planar(DecorTextures.Linoleum(new Color(0.88f, 0.90f, 0.88f), new Color(0.46f, 0.64f, 0.68f), true), 0.4f, 0.45f);
            const float half = 0.45f, lip = 0.05f, rimH = 0.05f, top = 2.0f, board = 0.03f;

            // The tray: a low lip all round a mosaic floor with a round chrome drain in the middle.
            m.Box(new Vector3(0f, rimH * 0.5f, half - lip * 0.5f), new Vector3(half * 2f, rimH, lip), tray);
            m.Box(new Vector3(0f, rimH * 0.5f, -half + lip * 0.5f), new Vector3(half * 2f, rimH, lip), tray);
            m.Box(new Vector3(-half + lip * 0.5f, rimH * 0.5f, 0f), new Vector3(lip, rimH, half * 2f - lip * 2f), tray);
            m.Box(new Vector3(half - lip * 0.5f, rimH * 0.5f, 0f), new Vector3(lip, rimH, half * 2f - lip * 2f), tray);
            m.Flat(new Vector3(0f, 0.015f, 0f), half * 2f - lip * 2f, half * 2f - lip * 2f, 0f, mosaic);
            m.Cylinder(new Vector3(0f, 0.015f, 0f), 0.045f, 0.003f, 14, chrome);
            for (int i = -1; i <= 1; i++)
                m.Box(new Vector3(i * 0.014f, 0.0185f, 0f), new Vector3(0.006f, 0.001f, 0.05f), M(new Color(0.15f, 0.15f, 0.16f)));

            // Tile boards up the two walls, standing proud of the rooms' wainscot rails, with a white edging along the top.
            float tileH = top - rimH, tileY = (top + rimH) * 0.5f;
            m.Box(new Vector3(0f, tileY, -half + board * 0.5f), new Vector3(half * 2f, tileH, board), tiles);
            m.Box(new Vector3(half - board * 0.5f, tileY, 0f), new Vector3(board, tileH, half * 2f), tiles);
            m.Box(new Vector3(0f, top + 0.01f, -half + board * 0.5f), new Vector3(half * 2f, 0.02f, board + 0.008f), tray);
            m.Box(new Vector3(half - board * 0.5f, top + 0.01f, 0f), new Vector3(board + 0.008f, 0.02f, half * 2f), tray);

            // The glass: a side panel on -X, and a fixed panel on the front beside the door's opening, each in a chrome
            // channel on the tiles under a chrome top rail, meeting at the corner post.
            const float g0 = 0.06f, g1 = 1.95f;
            float gy = (g0 + g1) * 0.5f, gh = g1 - g0;
            m.Box(new Vector3(-0.44f, gy, 0.005f), new Vector3(0.008f, gh, 0.85f), glass);
            m.Box(new Vector3(-0.44f, gy, -0.413f), new Vector3(0.022f, gh + 0.02f, 0.014f), chrome);
            m.Box(new Vector3(-0.44f, 1.958f, 0f), new Vector3(0.02f, 0.016f, 0.88f), chrome);
            m.Box(new Vector3(0.226f, gy, 0.44f), new Vector3(0.388f, gh, 0.008f), glass);
            m.Box(new Vector3(0.413f, gy, 0.44f), new Vector3(0.014f, gh + 0.02f, 0.022f), chrome);
            m.Box(new Vector3(0.026f, gy, 0.44f), new Vector3(0.012f, gh, 0.016f), chrome);
            m.Box(new Vector3(0f, 1.958f, 0.44f), new Vector3(0.88f, 0.016f, 0.02f), chrome);
            m.Box(new Vector3(-0.44f, (rimH + 1.966f) * 0.5f, 0.44f), new Vector3(0.024f, 1.966f - rimH, 0.024f), chrome);

            // The riser rail on the back wall and the rose on its arm, its face turned down into the stall.
            m.Rod(new Vector3(0.10f, 0.95f, -0.405f), new Vector3(0.10f, 2.0f, -0.405f), 0.022f, chrome, 10);
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(0.10f, i == 0 ? 0.96f : 1.99f, -0.413f), new Vector3(0.036f, 0.03f, 0.018f), chrome);
            m.Rod(new Vector3(0.10f, 2.0f, -0.405f), new Vector3(0.10f, 1.975f, -0.295f), 0.018f, chrome, 8);
            Vector3 axis = new Vector3(0f, 1f, -0.35f).normalized;
            Quaternion tilt = Quaternion.FromToRotation(Vector3.up, axis);
            m.Cylinder(ShowerRose - axis * 0.012f, 0.075f, 0.026f, 16, chrome, tilt);
            m.Cylinder(ShowerRose - axis * 0.0135f, 0.066f, 0.002f, 16, M(new Color(0.30f, 0.32f, 0.34f)), tilt);

            // The mixer's chrome plate beside the riser.
            m.Cylinder(new Vector3(ShowerLeverPivot.x, ShowerLeverPivot.y, -0.42f), 0.06f, 0.008f, 16, chrome, Quaternion.Euler(90f, 0f, 0f));

            // A wire corner shelf of bottles, by the back wall.
            var shelf = new Vector3(0.36f, 1.28f, -0.36f);
            m.Box(shelf, new Vector3(0.11f, 0.008f, 0.11f), chrome);
            Color[] bottles = { new Color(0.20f, 0.50f, 0.66f), new Color(0.84f, 0.52f, 0.20f), new Color(0.56f, 0.30f, 0.56f), new Color(0.30f, 0.60f, 0.36f) };
            m.Cylinder(shelf + new Vector3(0.02f, 0.004f, -0.02f), 0.024f, 0.17f, 10, G(bottles[rng.Next(bottles.Length)]));
            m.Cylinder(shelf + new Vector3(0.02f, 0.174f, -0.02f), 0.011f, 0.022f, 8, G(new Color(0.92f, 0.92f, 0.90f)));
            m.Cylinder(shelf + new Vector3(-0.025f, 0.004f, 0.02f), 0.021f, 0.12f, 10, G(bottles[rng.Next(bottles.Length)]));
        }

        /// <summary>The shower's glass door, hinged at its -X edge by the corner post: a clear pane between chrome stiles,
        /// a bottom sweep and a bar handle on each face by its closing edge.</summary>
        public static void ShowerDoor(RaidMesh m)
        {
            Surf glass = BathGlass, chrome = Mt(Chrome);
            const float y0 = 0.065f, y1 = 1.948f, z = 0.44f;
            float h = y1 - y0, ym = (y0 + y1) * 0.5f;
            m.Box(new Vector3(-0.205f, ym, z), new Vector3(0.426f, h, 0.008f), glass);
            // Thin matte‑black rubber gasket strips on each side of the shower door
            m.Box(new Vector3(-0.210f, ym, z), new Vector3(0.002f, h, 0.01f), M(new Color(0.07f, 0.07f, 0.07f)));
            m.Box(new Vector3(0.013f, ym, z), new Vector3(0.002f, h, 0.01f), M(new Color(0.07f, 0.07f, 0.07f)));

            m.Box(new Vector3(-0.424f, ym, z), new Vector3(0.012f, h, 0.016f), chrome);
            m.Box(new Vector3(0.014f, ym, z), new Vector3(0.012f, h, 0.016f), chrome);
            m.Box(new Vector3(-0.205f, y0 + 0.006f, z), new Vector3(0.426f, 0.012f, 0.014f), chrome);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(-0.04f, 1.05f, z + s * 0.024f), new Vector3(0.014f, 0.24f, 0.012f), chrome);
                for (int e = -1; e <= 1; e += 2)
                    m.Box(new Vector3(-0.04f, 1.05f + e * 0.1f, z + s * 0.013f), new Vector3(0.008f, 0.008f, 0.014f), chrome);
            }
            for (int e = -1; e <= 1; e += 2)
                m.Box(new Vector3(-0.43f, ym + e * 0.7f, z), new Vector3(0.022f, 0.06f, 0.02f), chrome);
        }

        /// <summary>The shower mixer's lever: a round boss with a handle hanging down when off; it turns a quarter round to run.</summary>
        public static void ShowerLever(RaidMesh m)
        {
            Surf chrome = Mt(Chrome);
            m.Cylinder(new Vector3(ShowerLeverPivot.x, ShowerLeverPivot.y, -0.412f), 0.022f, 0.026f, 12, chrome, Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(ShowerLeverPivot.x, ShowerLeverPivot.y - 0.045f, -0.392f), new Vector3(0.018f, 0.085f, 0.014f), chrome);
        }

        /// <summary>One look of the running shower: a cone of thin streaks from the rose to the tray, some breaking into
        /// drops on the way down, and the splash on the tray. Two draws with different streams alternate to make it run.</summary>
        public static void ShowerSpray(RaidMesh m, System.Random rng)
        {
            Surf water = BathWater;
            Vector3 face = ShowerRose - new Vector3(0f, 0.02f, 0f);
            var land = new Vector3(ShowerRose.x - 0.02f, 0.03f, ShowerRose.z + 0.24f);
            for (int i = 0; i < 28; i++)
            {
                float a = R(rng, 0f, Mathf.PI * 2f), r0 = R(rng, 0.005f, 0.06f);
                float b = a + R(rng, -0.35f, 0.35f), r1 = Mathf.Lerp(0.06f, 0.28f, Mathf.Sqrt((float)rng.NextDouble()));
                Vector3 from = face + new Vector3(Mathf.Sin(a) * r0, 0f, Mathf.Cos(a) * r0);
                Vector3 to = land + new Vector3(Mathf.Sin(b) * r1, 0f, Mathf.Cos(b) * r1 * 0.9f);
                float cut = Chance(rng, 0.35) ? R(rng, 0.4f, 0.85f) : 1f;
                m.Rod(from, Vector3.Lerp(from, to, cut), R(rng, 0.0035f, 0.0065f), water);
                if (cut >= 1f) continue;
                for (int d = 1; d <= 2; d++)
                    m.Ellipsoid(Vector3.Lerp(from, to, Mathf.Min(0.98f, cut + d * 0.07f)), new Vector3(0.01f, 0.016f, 0.01f), water, 5, 3);
            }
            m.Ellipsoid(land - new Vector3(0f, 0.012f, 0f), new Vector3(0.5f, 0.025f, 0.5f), water, 12, 3);
        }
        #endregion

        #region Wall fittings
        /// <summary>The bathroom's mirror cabinet, its back centred on <paramref name="back"/> (on the wall face) and facing
        /// <paramref name="normal"/>: a white carcass <paramref name="w"/> × <paramref name="h"/>, <paramref name="d"/> deep,
        /// open at the front, with two glass shelves. Its mirrored door is drawn apart (<see cref="MirrorCabinetDoor"/>).</summary>
        public static void MirrorCabinetBody(RaidMesh m, Vector3 back, Vector3 normal, float w, float h, float d)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized, up = Vector3.up;
            Surf white = G(BathCabinetWhite), inside = M(new Color(0.84f, 0.85f, 0.83f));
            const float t = 0.016f;
            m.Box(back + normal * (t * 0.5f), new Vector3(w, h, t), q, inside);
            Vector3 mid = back + normal * (d * 0.5f);
            m.Box(mid + up * (h * 0.5f - t * 0.5f), new Vector3(w, t, d), q, white);
            m.Box(mid - up * (h * 0.5f - t * 0.5f), new Vector3(w, t, d), q, white);
            m.Box(mid + right * (w * 0.5f - t * 0.5f), new Vector3(t, h - t * 2f, d), q, white);
            m.Box(mid - right * (w * 0.5f - t * 0.5f), new Vector3(t, h - t * 2f, d), q, white);
            Surf shelf = Gs(new Color(0.62f, 0.72f, 0.74f));
            for (int i = -1; i <= 1; i += 2)
                m.Box(back + normal * (d * 0.45f) + up * (i * h / 6f), new Vector3(w - t * 2f, 0.006f, d * 0.75f), q, shelf);
        }

        /// <summary>The mirror cabinet's door: a white board faced with a mirror, its back on <paramref name="front"/> (the
        /// carcass's open front), with a small chrome pull on the closing edge (away from the hinge at <paramref name="hingeSide"/>).</summary>
        public static void MirrorCabinetDoor(RaidMesh m, Vector3 front, Vector3 normal, Vector3 hingeSide, float w, float h)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            const float t = 0.016f;
            m.Box(front + normal * (t * 0.5f), new Vector3(w, h, t), q, G(BathCabinetWhite));
            m.Box(front + normal * (t + 0.0015f), new Vector3(w - 0.024f, h - 0.024f, 0.003f), q, Mt(BathMirror));
            // Thin chrome frame around mirror pane (≈2 mm thick)
            Surf chrome = Mt(Chrome);
            // Top frame
            m.Box(front + normal * (t + 0.0015f) + Vector3.up * ((h - 0.024f) * 0.5f - 0.001f), new Vector3(w - 0.024f, 0.002f, 0.003f), q, chrome);
            // Bottom frame
            m.Box(front + normal * (t + 0.0015f) - Vector3.up * ((h - 0.024f) * 0.5f - 0.001f), new Vector3(w - 0.024f, 0.002f, 0.003f), q, chrome);
            // Left frame
            m.Box(front + normal * (t + 0.0015f) - hingeSide * ((w - 0.024f) * 0.5f - 0.001f), new Vector3(0.002f, h - 0.028f, 0.003f), q, chrome);
            // Right frame
            m.Box(front + normal * (t + 0.0015f) + hingeSide * ((w - 0.024f) * 0.5f - 0.001f), new Vector3(0.002f, h - 0.028f, 0.003f), q, chrome);
            m.Box(front + normal * (t + 0.006f) - hingeSide * (w * 0.5f - 0.016f), new Vector3(0.008f, 0.07f, 0.008f), q, Mt(Chrome));
        }

        /// <summary>A chrome towel rail on the wall: a round rose and stand-off post at each end of a bar <paramref name="len"/>
        /// long, centred on <paramref name="at"/> (on the wall face) and running <paramref name="along"/> it. Returns the bar's centre.</summary>
        public static Vector3 TowelRailBar(RaidMesh m, Vector3 at, Vector3 normal, Vector3 along, float len)
        {
            Surf chrome = Mt(Chrome);
            const float off = 0.065f;
            Quaternion toRoom = Quaternion.FromToRotation(Vector3.up, normal);
            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 foot = at + along * (e * len * 0.5f);
                m.Cylinder(foot, 0.024f, 0.008f, 12, chrome, toRoom);
                m.Rod(foot, foot + normal * off, 0.016f, chrome, 8);
            }
            Vector3 bar = at + normal * off;
            float end = len * 0.5f + 0.012f;
            m.Rod(bar - along * end, bar + along * end, 0.019f, chrome, 10);
            for (int e = -1; e <= 1; e += 2)
                m.Ellipsoid(bar + along * (e * end), new Vector3(0.022f, 0.022f, 0.022f), chrome, 8, 4);
            return bar;
        }

        /// <summary>A towel folded over a rail's bar (centred on <paramref name="bar"/>, <paramref name="width"/> along it): a
        /// rounded fold over the top, a longer flap hanging toward the room and a shorter one behind, a woven band near the hem.</summary>
        public static void TowelOnRail(RaidMesh m, Vector3 bar, Vector3 normal, Vector3 along, float width, Color c)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Surf cloth = M(c), band = M(Color.Lerp(c, Color.white, 0.35f));
            const float r = 0.017f, t = 0.012f, front = 0.27f, back = 0.24f;
            m.Cylinder(bar - along * (width * 0.5f), r + t, width, 12, cloth, Quaternion.FromToRotation(Vector3.up, along));
            Vector3 fc = bar + normal * (r + t * 0.5f) - Vector3.up * (front * 0.5f);
            Vector3 bc = bar - normal * (r + t * 0.5f) - Vector3.up * (back * 0.5f);
            m.Box(fc, new Vector3(width, front, t), q, cloth);
            m.Box(bc, new Vector3(width, back, t), q, cloth);
            m.Box(fc + normal * (t * 0.5f + 0.0012f) - Vector3.up * (front * 0.5f - 0.05f), new Vector3(width, 0.03f, 0.002f), q, band);
        }
        #endregion
        #endregion
    }
}
