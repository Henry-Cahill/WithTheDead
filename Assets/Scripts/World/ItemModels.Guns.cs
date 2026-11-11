using UnityEngine;

namespace CloneZ
{
    // The guns of 1993 Kentucky and of the Zone, built to their real sizes the way the pistol and the shotgun are: lying on
    // their left side, the muzzle at +X, the top of the gun (the sights) up +Z, the right side (+Y) to the camera. Each one's
    // hold (GunStats.Hold) is measured off these models, and the asset pipeline fits their generated meshes onto them.
    public static partial class ItemModels
    {
        private static readonly PartMat GunBlued = PartMat.Of(new Color(0.13f, 0.13f, 0.15f), 0.5f, 0.85f);
        private static readonly PartMat GunBluedHi = PartMat.Of(new Color(0.22f, 0.23f, 0.26f), 0.55f, 0.85f);
        private static readonly PartMat GunMatte = PartMat.Of(new Color(0.11f, 0.11f, 0.12f), 0.3f, 0.55f);
        private static readonly PartMat GunSteel = PartMat.Of(new Color(0.42f, 0.44f, 0.48f), 0.6f, 0.9f);
        private static readonly PartMat GunDark = PartMat.Of(new Color(0.05f, 0.05f, 0.06f), 0.3f, 0.35f);
        private static readonly PartMat GunPolymer = PartMat.Of(new Color(0.09f, 0.09f, 0.1f), 0.25f);
        private static readonly PartMat GunRibs = PartMat.Of(new Color(0.05f, 0.05f, 0.055f), 0.2f);
        private static readonly PartMat GunWalnut = PartMat.Of(new Color(0.42f, 0.25f, 0.13f), 0.3f);
        private static readonly PartMat GunWalnutDark = PartMat.Of(new Color(0.27f, 0.15f, 0.08f), 0.26f);
        private static readonly PartMat GunBirch = PartMat.Of(new Color(0.6f, 0.43f, 0.26f), 0.3f);
        private static readonly PartMat GunLaminate = PartMat.Of(new Color(0.4f, 0.2f, 0.11f), 0.32f);
        private static readonly PartMat GunPlum = PartMat.Of(new Color(0.33f, 0.15f, 0.12f), 0.35f);
        private static readonly PartMat GunOlive = PartMat.Of(new Color(0.24f, 0.26f, 0.19f), 0.3f, 0.4f);
        private static readonly PartMat GunBrass = PartMat.Of(new Color(0.78f, 0.6f, 0.3f), 0.55f, 0.8f);

        // A thin object lying on its side hovers its centre this high, so the bob never dips it through the floor.
        private const float GunBase = 0.12f;

        private static PartNode GunRoot(PartNode parent, out float baseHeight)
        {
            baseHeight = GunBase;
            var root = new PartNode("Visual");
            root.SetParent(parent, false);
            return root;
        }

        // A block from x0 to x1 along the bore and z0 to z1 up the gun, `thick` across it, its middle `y` off the centre line.
        private static PartRef Span(PartNode p, string name, float x0, float x1, float z0, float z1, float thick, PartMat mat, float y = 0f)
            => Block(p, name, new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f), new Vector3(x1 - x0, thick, z1 - z0), mat);

        // A round bar along the bore from x0 to x1, its axis at height z.
        private static PartRef Bore(PartNode p, string name, float x0, float x1, float z, float diameter, PartMat mat, float y = 0f)
            => Rod(p, name, new Vector3((x0 + x1) * 0.5f, y, z), new Vector3(0f, 0f, 90f), diameter, x1 - x0, mat);

        // A block centred at (x, z) whose rear end drops `rearDown` degrees: the toe line of a buttstock.
        private static PartRef Tilt(PartNode p, string name, float x, float z, float len, float thick, float height, float rearDown, PartMat mat)
            => p.Add(new Part(PartShape.Cube, new Vector3(x, 0f, z), Quaternion.Euler(0f, -rearDown, 0f), new Vector3(len, thick, height), mat));

        // A frame pivoting at (x, z), turned so whatever hangs below it (down its -Z) slopes back toward the butt by `degrees`
        // (a pistol grip); negative slopes it forward (a curved magazine).
        private static PartNode Raked(PartNode p, string name, float x, float z, float degrees)
        {
            var n = new PartNode(name);
            n.SetParent(p, false);
            n.localPosition = new Vector3(x, 0f, z);
            n.localEulerAngles = new Vector3(0f, degrees, 0f);
            return n;
        }

        #region Kentucky
        /// <summary>A four-inch .38 Special service revolver (a K-frame): a slim barrel with the ejector rod under it, the
        /// fluted six-shot cylinder in its window under the top strap, a spurred hammer, and walnut stocks on a round butt.</summary>
        private static PartNode BuildRevolver38(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.04f;

            Bore(root, "Barrel", 0.02f, 0.118f, bore, 0.017f, GunBlued);
            Bore(root, "Crown", 0.116f, 0.12f, bore, 0.018f, GunBluedHi);
            Bore(root, "BoreHole", 0.119f, 0.1205f, bore, 0.009f, GunDark);
            Span(root, "FrontSight", 0.098f, 0.114f, 0.047f, 0.057f, 0.004f, GunBlued);
            Bore(root, "EjectorRod", 0.02f, 0.09f, 0.027f, 0.0075f, GunSteel);
            Bore(root, "EjectorTip", 0.086f, 0.095f, 0.027f, 0.009f, GunBluedHi);
            Span(root, "Lug", 0.092f, 0.102f, 0.027f, 0.036f, 0.008f, GunBlued);

            // The frame round the cylinder window: the barrel shank in front, the top strap, the standing breech, the bottom.
            Span(root, "Shank", 0.012f, 0.026f, 0.014f, 0.054f, 0.022f, GunBlued);
            Span(root, "TopStrap", -0.042f, 0.012f, 0.0475f, 0.055f, 0.02f, GunBlued);
            Span(root, "RearSight", -0.044f, -0.036f, 0.055f, 0.058f, 0.006f, GunDark);
            Span(root, "Breech", -0.05f, -0.032f, 0.008f, 0.055f, 0.024f, GunBlued);
            Span(root, "FrameBottom", -0.05f, 0.026f, 0.003f, 0.011f, 0.02f, GunBlued);
            Bore(root, "Cylinder", -0.032f, 0.012f, 0.029f, 0.037f, GunBlued);
            Span(root, "FluteUpper", -0.027f, 0.007f, 0.033f, 0.038f, 0.002f, GunDark, 0.0182f);
            Span(root, "FluteLower", -0.027f, 0.007f, 0.019f, 0.024f, 0.002f, GunDark, 0.0182f);
            Span(root, "Latch", -0.05f, -0.04f, 0.034f, 0.042f, 0.004f, GunSteel, 0.012f);

            Span(root, "Hammer", -0.058f, -0.048f, 0.04f, 0.058f, 0.008f, GunBluedHi);
            Sphere(root, "HammerSpur", new Vector3(-0.062f, 0f, 0.06f), Vector3.zero, new Vector3(0.014f, 0.008f, 0.008f), GunBluedHi);

            Span(root, "GuardBottom", -0.03f, 0.008f, -0.012f, -0.007f, 0.008f, GunBlued);
            Span(root, "GuardFront", 0.004f, 0.01f, -0.012f, 0.004f, 0.008f, GunBlued);
            Span(root, "Trigger", -0.018f, -0.012f, -0.006f, 0.004f, 0.006f, GunSteel);

            PartNode grip = Raked(root, "Grip", -0.044f, 0.006f, 16f);
            Span(grip, "GripFrame", -0.012f, 0.01f, -0.07f, 0f, 0.016f, GunBlued);
            Span(grip, "Stocks", -0.018f, 0.014f, -0.072f, -0.004f, 0.032f, GunWalnut);
            Span(grip, "Checkering", -0.012f, 0.008f, -0.06f, -0.015f, 0.001f, GunWalnutDark, 0.0162f);
            Sphere(grip, "Medallion", new Vector3(-0.002f, 0.0165f, -0.012f), Vector3.zero, new Vector3(0.008f, 0.002f, 0.008f), GunSteel);
            Sphere(grip, "Butt", new Vector3(-0.002f, 0f, -0.072f), Vector3.zero, new Vector3(0.034f, 0.032f, 0.012f), GunWalnut);
            return root;
        }

        /// <summary>The Government-model .45 automatic: a long, flat-topped slide with its barrel bushing and rear
        /// serrations, the exposed spur hammer over the beavertail grip safety, the thumb safety and slide stop on the frame,
        /// and checkered walnut grips on a steep, raked grip.</summary>
        private static PartNode BuildM1911(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.044f, side = 0.012f;

            Span(root, "Slide", -0.098f, 0.104f, 0.032f, 0.058f, 0.024f, GunBlued);
            Span(root, "SlideTop", -0.096f, 0.1f, 0.0575f, 0.0595f, 0.016f, GunBluedHi);
            Bore(root, "Bushing", 0.104f, 0.11f, bore, 0.018f, GunBluedHi);
            Bore(root, "BoreHole", 0.109f, 0.1105f, bore, 0.009f, GunDark);
            Span(root, "FrontSight", 0.092f, 0.098f, 0.058f, 0.064f, 0.004f, GunBlued);
            Span(root, "RearSight", -0.086f, -0.076f, 0.058f, 0.064f, 0.016f, GunBlued);
            Span(root, "RearNotch", -0.084f, -0.078f, 0.061f, 0.065f, 0.004f, GunDark);
            Span(root, "EjectionPort", -0.005f, 0.03f, 0.042f, 0.056f, 0.002f, GunDark, side);
            for (int i = 0; i < 5; i++)
                Span(root, "Serration", -0.092f + i * 0.006f, -0.089f + i * 0.006f, 0.034f, 0.056f, 0.002f, GunDark, side);

            Span(root, "DustCover", 0.01f, 0.104f, 0.022f, 0.032f, 0.022f, GunBlued);
            Span(root, "Frame", -0.09f, 0.01f, 0.018f, 0.032f, 0.022f, GunBlued);
            Span(root, "SlideStop", -0.02f, 0.012f, 0.028f, 0.034f, 0.003f, GunSteel, side + 0.0005f);
            Sphere(root, "SlideStopPin", new Vector3(0.018f, side, 0.026f), Vector3.zero, new Vector3(0.006f, 0.004f, 0.006f), GunSteel);
            Span(root, "ThumbSafety", -0.078f, -0.06f, 0.03f, 0.036f, 0.003f, GunSteel, side + 0.0005f);
            Span(root, "Hammer", -0.104f, -0.096f, 0.038f, 0.058f, 0.008f, GunBluedHi);
            Sphere(root, "HammerSpur", new Vector3(-0.104f, 0f, 0.057f), Vector3.zero, new Vector3(0.01f, 0.008f, 0.01f), GunBluedHi);
            Span(root, "GripSafety", -0.108f, -0.092f, 0.014f, 0.03f, 0.02f, GunBlued);

            Span(root, "GuardBottom", -0.012f, 0.035f, -0.008f, -0.003f, 0.008f, GunBlued);
            Span(root, "GuardFront", 0.03f, 0.036f, -0.008f, 0.018f, 0.008f, GunBlued);
            Span(root, "Trigger", -0.004f, 0.004f, -0.002f, 0.016f, 0.006f, GunSteel);
            Span(root, "MagRelease", -0.016f, -0.008f, 0.008f, 0.016f, 0.003f, GunSteel, side + 0.0005f);

            PartNode grip = Raked(root, "Grip", -0.068f, 0.018f, 18f);
            Span(grip, "GripFrame", -0.02f, 0.015f, -0.088f, 0f, 0.022f, GunBlued);
            Span(grip, "Panels", -0.017f, 0.012f, -0.08f, -0.006f, 0.03f, GunWalnut);
            Span(grip, "Checkering", -0.014f, 0.009f, -0.074f, -0.012f, 0.001f, GunWalnutDark, 0.0151f);
            Sphere(grip, "ScrewTop", new Vector3(-0.002f, 0.0152f, -0.014f), Vector3.zero, new Vector3(0.005f, 0.002f, 0.005f), GunSteel);
            Sphere(grip, "ScrewBottom", new Vector3(-0.002f, 0.0152f, -0.07f), Vector3.zero, new Vector3(0.005f, 0.002f, 0.005f), GunSteel);
            Span(grip, "Housing", -0.024f, -0.017f, -0.09f, -0.004f, 0.02f, GunBluedHi);
            Span(grip, "MagBase", -0.018f, 0.013f, -0.095f, -0.088f, 0.02f, GunBlued);
            return root;
        }

        /// <summary>A semi-automatic .22 carbine: a light barrel tied to a one-piece hardwood stock by a barrel band, a
        /// round-topped receiver with the charging handle on the right, the flush rotary magazine, and a bead front sight.</summary>
        private static PartNode BuildRifle22(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.032f, side = 0.013f;

            Bore(root, "Barrel", 0.03f, 0.46f, bore, 0.017f, GunBlued);
            Bore(root, "Crown", 0.455f, 0.462f, bore, 0.018f, GunBluedHi);
            Bore(root, "BoreHole", 0.461f, 0.4625f, bore, 0.008f, GunDark);
            Span(root, "BarrelBand", 0.18f, 0.19f, 0.004f, 0.043f, 0.039f, GunBlued);
            Span(root, "FrontSight", 0.44f, 0.448f, 0.04f, 0.052f, 0.004f, GunBlued);
            Sphere(root, "Bead", new Vector3(0.444f, 0f, 0.053f), Vector3.zero, new Vector3(0.005f, 0.005f, 0.005f), GunBrass);
            Span(root, "RearSight", 0.15f, 0.162f, 0.039f, 0.05f, 0.012f, GunBlued);

            Span(root, "Receiver", -0.14f, 0.03f, 0.014f, 0.052f, 0.026f, GunBlued);
            Bore(root, "ReceiverTop", -0.14f, 0.03f, 0.05f, 0.026f, GunBlued);
            Span(root, "EjectionPort", -0.06f, -0.02f, 0.03f, 0.048f, 0.002f, GunDark, side + 0.0005f);
            Rod(root, "ChargingHandle", new Vector3(-0.025f, 0.02f, 0.028f), Vector3.zero, 0.007f, 0.016f, GunSteel);
            Span(root, "Magazine", -0.074f, -0.042f, -0.012f, 0.016f, 0.022f, GunPolymer);

            Span(root, "GuardFront", -0.104f, -0.098f, -0.022f, 0.012f, 0.012f, GunBlued);
            Span(root, "GuardRear", -0.148f, -0.142f, -0.02f, 0.012f, 0.012f, GunBlued);
            Span(root, "GuardBottom", -0.148f, -0.098f, -0.024f, -0.018f, 0.012f, GunBlued);
            Span(root, "Trigger", -0.128f, -0.122f, -0.012f, 0.008f, 0.006f, GunSteel);

            // One-piece stock: the forend under the barrel, the action, a pistol-grip wrist and the buttstock.
            Span(root, "Forend", -0.1f, 0.195f, 0f, 0.03f, 0.036f, GunBirch);
            Sphere(root, "ForendTip", new Vector3(0.195f, 0f, 0.015f), Vector3.zero, new Vector3(0.02f, 0.036f, 0.03f), GunBirch);
            Span(root, "Action", -0.16f, 0.03f, -0.006f, 0.016f, 0.036f, GunBirch);
            Span(root, "Wrist", -0.25f, -0.15f, -0.02f, 0.038f, 0.034f, GunBirch);
            Sphere(root, "GripSwell", new Vector3(-0.2f, 0f, -0.028f), Vector3.zero, new Vector3(0.06f, 0.034f, 0.04f), GunBirch);
            Span(root, "Comb", -0.468f, -0.24f, -0.008f, 0.04f, 0.036f, GunBirch);
            Tilt(root, "Toe", -0.37f, -0.025f, 0.2f, 0.036f, 0.05f, 10f, GunBirch);
            Span(root, "ButtPlate", -0.48f, -0.466f, -0.07f, 0.042f, 0.037f, GunDark);
            return root;
        }

        /// <summary>A police pump shotgun: an 18-inch cylinder-bore barrel with rifle sights, the magazine tube extended to
        /// the muzzle and clamped to it, a ribbed black pump, a matte receiver with a ghost-ring sight, and a black synthetic
        /// stock with a thick recoil pad.</summary>
        private static PartNode BuildRiotShotgun(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.036f, side = 0.016f;

            Bore(root, "Barrel", 0f, 0.49f, bore, 0.026f, GunMatte);
            Bore(root, "Muzzle", 0.482f, 0.492f, bore, 0.028f, GunMatte);
            Bore(root, "BoreHole", 0.491f, 0.4925f, bore, 0.019f, GunDark);
            Span(root, "FrontSight", 0.46f, 0.474f, 0.049f, 0.06f, 0.006f, GunMatte);
            Bore(root, "MagTube", 0f, 0.47f, 0.008f, 0.024f, GunMatte);
            Bore(root, "MagCap", 0.47f, 0.485f, 0.008f, 0.027f, GunSteel);
            Span(root, "Clamp", 0.43f, 0.445f, -0.006f, 0.05f, 0.03f, GunMatte);
            Span(root, "BarrelRing", 0.31f, 0.325f, -0.004f, 0.05f, 0.028f, GunMatte);

            Bore(root, "Pump", 0.07f, 0.25f, 0.012f, 0.05f, GunPolymer);
            for (int i = 0; i < 6; i++) Bore(root, "PumpRib", 0.085f + i * 0.028f, 0.091f + i * 0.028f, 0.012f, 0.053f, GunRibs);
            Span(root, "ActionBars", -0.03f, 0.08f, 0.002f, 0.012f, 0.03f, GunMatte);

            Span(root, "Receiver", -0.17f, 0f, 0f, 0.05f, 0.032f, GunMatte);
            Bore(root, "ReceiverTop", -0.17f, 0f, 0.048f, 0.03f, GunMatte);
            Span(root, "EjectionPort", -0.09f, -0.035f, 0.02f, 0.046f, 0.002f, GunDark, side + 0.0005f);
            Span(root, "GhostRing", -0.04f, -0.025f, 0.06f, 0.074f, 0.012f, GunMatte);
            Rod(root, "Aperture", new Vector3(-0.032f, 0f, 0.068f), new Vector3(0f, 0f, 90f), 0.006f, 0.016f, GunDark);

            Span(root, "GuardBottom", -0.15f, -0.09f, -0.025f, -0.019f, 0.012f, GunMatte);
            Span(root, "GuardFront", -0.096f, -0.09f, -0.025f, 0.002f, 0.012f, GunMatte);
            Span(root, "GuardRear", -0.15f, -0.144f, -0.022f, 0.002f, 0.012f, GunMatte);
            Span(root, "Trigger", -0.124f, -0.118f, -0.017f, 0f, 0.006f, GunSteel);
            Sphere(root, "Safety", new Vector3(-0.148f, side, -0.006f), Vector3.zero, new Vector3(0.008f, 0.004f, 0.008f), GunSteel);

            Span(root, "Wrist", -0.285f, -0.165f, -0.012f, 0.044f, 0.036f, GunPolymer);
            Sphere(root, "GripSwell", new Vector3(-0.23f, 0f, -0.024f), Vector3.zero, new Vector3(0.07f, 0.036f, 0.035f), GunPolymer);
            Span(root, "Comb", -0.478f, -0.28f, -0.01f, 0.044f, 0.038f, GunPolymer);
            Tilt(root, "Toe", -0.38f, -0.03f, 0.2f, 0.038f, 0.05f, 9f, GunPolymer);
            Span(root, "RecoilPad", -0.492f, -0.476f, -0.075f, 0.048f, 0.04f, GunDark);
            return root;
        }

        /// <summary>A walnut-stocked sporting bolt action in .30-06: a tapered barrel with a ramp front sight, the round
        /// receiver and bolt shroud, the bolt handle swept down on the right, and a Monte Carlo stock with a checkered pistol
        /// grip, sling swivels and a recoil pad.</summary>
        private static PartNode BuildHuntingRifle(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.035f, side = 0.017f;

            Bore(root, "BarrelShank", 0f, 0.26f, bore, 0.024f, GunBlued);
            Bore(root, "Barrel", 0.26f, 0.53f, bore, 0.019f, GunBlued);
            Bore(root, "Crown", 0.524f, 0.531f, bore, 0.0195f, GunBluedHi);
            Bore(root, "BoreHole", 0.53f, 0.5315f, bore, 0.008f, GunDark);
            Span(root, "SightRamp", 0.495f, 0.52f, 0.044f, 0.051f, 0.008f, GunBlued);
            Span(root, "FrontBlade", 0.508f, 0.512f, 0.051f, 0.058f, 0.003f, GunSteel);
            Span(root, "RearSight", 0.2f, 0.215f, 0.046f, 0.054f, 0.012f, GunBlued);

            Bore(root, "Receiver", -0.2f, 0f, bore, 0.034f, GunBlued);
            Bore(root, "RecoilLug", -0.005f, 0.005f, bore, 0.036f, GunBluedHi);
            Bore(root, "BoltShroud", -0.235f, -0.2f, bore, 0.026f, GunBlued);
            Bore(root, "CockingPiece", -0.245f, -0.235f, 0.032f, 0.012f, GunSteel);
            Span(root, "EjectionPort", -0.11f, -0.04f, 0.027f, 0.047f, 0.002f, GunDark, side);
            Rod(root, "BoltHandle", new Vector3(-0.17f, 0.026f, 0.0275f), new Vector3(-28f, 0f, 0f), 0.007f, 0.034f, GunSteel);
            Sphere(root, "BoltKnob", new Vector3(-0.175f, 0.042f, 0.019f), Vector3.zero, new Vector3(0.016f, 0.016f, 0.016f), GunSteel);
            Span(root, "Safety", -0.215f, -0.205f, 0.022f, 0.028f, 0.004f, GunSteel, side - 0.001f);

            Span(root, "GuardBottom", -0.168f, -0.115f, -0.03f, -0.025f, 0.012f, GunBlued);
            Span(root, "GuardFront", -0.121f, -0.115f, -0.03f, -0.004f, 0.012f, GunBlued);
            Span(root, "GuardRear", -0.17f, -0.163f, -0.028f, -0.004f, 0.012f, GunBlued);
            Span(root, "Trigger", -0.142f, -0.136f, -0.022f, -0.004f, 0.006f, GunSteel);

            // The walnut stock: forend, action, a pistol grip with its cap, the Monte Carlo comb and the toe.
            Span(root, "Forend", -0.01f, 0.31f, 0.003f, 0.03f, 0.044f, GunWalnut);
            Bore(root, "ForendBelly", -0.01f, 0.31f, 0.008f, 0.04f, GunWalnut);
            Sphere(root, "ForendTip", new Vector3(0.31f, 0f, 0.012f), Vector3.zero, new Vector3(0.03f, 0.04f, 0.036f), GunWalnut);
            Span(root, "Action", -0.21f, -0.01f, -0.012f, 0.028f, 0.046f, GunWalnut);
            Span(root, "Wrist", -0.31f, -0.2f, -0.02f, 0.04f, 0.04f, GunWalnut);
            PartNode grip = Raked(root, "PistolGrip", -0.23f, -0.02f, 25f);
            Span(grip, "Grip", -0.022f, 0.02f, -0.045f, 0f, 0.04f, GunWalnut);
            Span(grip, "GripCap", -0.024f, 0.022f, -0.05f, -0.045f, 0.041f, GunDark);
            Span(grip, "Checkering", -0.016f, 0.014f, -0.04f, -0.005f, 0.001f, GunWalnutDark, 0.0205f);
            Span(root, "Comb", -0.52f, -0.3f, 0f, 0.05f, 0.042f, GunWalnut);
            Tilt(root, "Toe", -0.42f, -0.03f, 0.22f, 0.042f, 0.05f, 12f, GunWalnut);
            Span(root, "RecoilPad", -0.535f, -0.52f, -0.085f, 0.052f, 0.044f, GunDark);
            Sphere(root, "SwivelFront", new Vector3(0.24f, 0f, -0.013f), Vector3.zero, new Vector3(0.008f, 0.008f, 0.008f), GunSteel);
            Sphere(root, "SwivelRear", new Vector3(-0.44f, 0f, -0.068f), Vector3.zero, new Vector3(0.008f, 0.008f, 0.008f), GunSteel);
            return root;
        }

        /// <summary>A lever-action .30-30 carbine: a round barrel over the full-length magazine tube, a walnut forend under
        /// a barrel band, the flat-sided receiver with its loading gate and exposed hammer, the finger lever's loop under a
        /// straight-gripped walnut stock, and a steel butt plate.</summary>
        private static PartNode BuildLeverAction(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.04f, side = 0.015f;

            Bore(root, "Barrel", -0.05f, 0.48f, bore, 0.02f, GunBlued);
            Bore(root, "Crown", 0.474f, 0.481f, bore, 0.021f, GunBluedHi);
            Bore(root, "BoreHole", 0.48f, 0.4815f, bore, 0.008f, GunDark);
            Bore(root, "MagTube", -0.05f, 0.43f, 0.019f, 0.017f, GunBlued);
            Bore(root, "TubeCap", 0.43f, 0.442f, 0.019f, 0.018f, GunSteel);
            Span(root, "BarrelBand", 0.4f, 0.412f, 0.008f, 0.052f, 0.026f, GunBlued);
            Span(root, "ForendCap", 0.165f, 0.178f, 0.006f, 0.054f, 0.04f, GunBlued);
            Span(root, "SightRamp", 0.455f, 0.475f, 0.048f, 0.053f, 0.01f, GunBlued);
            Span(root, "FrontSight", 0.462f, 0.47f, 0.053f, 0.062f, 0.004f, GunBlued);
            Sphere(root, "Bead", new Vector3(0.466f, 0f, 0.062f), Vector3.zero, new Vector3(0.004f, 0.004f, 0.004f), GunBrass);
            Span(root, "RearSight", 0.11f, 0.122f, 0.048f, 0.058f, 0.014f, GunBlued);
            Span(root, "RearNotch", 0.113f, 0.119f, 0.054f, 0.059f, 0.004f, GunDark);

            Span(root, "Forend", -0.05f, 0.168f, 0.004f, 0.048f, 0.038f, GunWalnut);

            Span(root, "Receiver", -0.205f, -0.05f, -0.006f, 0.056f, 0.03f, GunBlued);
            Span(root, "Bolt", -0.17f, -0.06f, 0.056f, 0.063f, 0.012f, GunSteel);
            Span(root, "LoadingGate", -0.1f, -0.065f, 0.004f, 0.018f, 0.002f, GunBluedHi, side + 0.0005f);
            Span(root, "Hammer", -0.215f, -0.203f, 0.046f, 0.07f, 0.008f, GunBluedHi);
            Sphere(root, "HammerSpur", new Vector3(-0.218f, 0f, 0.071f), Vector3.zero, new Vector3(0.016f, 0.008f, 0.008f), GunBluedHi);

            // The finger lever: forward under the receiver, down round the trigger and back under the wrist.
            Span(root, "LeverForward", -0.135f, -0.05f, -0.016f, -0.006f, 0.01f, GunBlued);
            Span(root, "LoopFront", -0.14f, -0.13f, -0.058f, -0.006f, 0.01f, GunBlued);
            Span(root, "LoopBottom", -0.245f, -0.13f, -0.062f, -0.053f, 0.01f, GunBlued);
            Span(root, "LoopRear", -0.25f, -0.24f, -0.06f, -0.026f, 0.01f, GunBlued);
            Span(root, "Trigger", -0.162f, -0.156f, -0.022f, -0.004f, 0.006f, GunSteel);

            Span(root, "Wrist", -0.3f, -0.205f, -0.024f, 0.045f, 0.036f, GunWalnut);
            Span(root, "Comb", -0.476f, -0.295f, -0.014f, 0.046f, 0.038f, GunWalnut);
            Tilt(root, "Toe", -0.39f, -0.035f, 0.19f, 0.038f, 0.05f, 11f, GunWalnut);
            Span(root, "ButtPlate", -0.486f, -0.474f, -0.08f, 0.05f, 0.04f, GunBluedHi);
            return root;
        }

        /// <summary>A first-generation AR-15 sporter: a 20-inch barrel with a three-prong flash hider and the triangular
        /// front sight base, round ribbed handguards, the flat upper with its carry handle and rear sight, a curved 30-round
        /// magazine, a raked pistol grip and the long fixed stock.</summary>
        private static PartNode BuildAr15(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.03f, side = 0.015f;

            Bore(root, "Barrel", -0.02f, 0.47f, bore, 0.016f, GunMatte);
            Bore(root, "FlashHider", 0.47f, 0.495f, bore, 0.022f, GunMatte);
            Bore(root, "BoreHole", 0.494f, 0.4955f, bore, 0.01f, GunDark);
            Span(root, "SightBase", 0.3f, 0.33f, 0.012f, 0.058f, 0.022f, GunMatte);
            Span(root, "SightPost", 0.312f, 0.318f, 0.058f, 0.098f, 0.006f, GunMatte);
            Span(root, "EarRight", 0.308f, 0.322f, 0.058f, 0.09f, 0.002f, GunMatte, 0.007f);
            Span(root, "EarLeft", 0.308f, 0.322f, 0.058f, 0.09f, 0.002f, GunMatte, -0.007f);
            Span(root, "BayonetLug", 0.302f, 0.316f, 0f, 0.012f, 0.01f, GunMatte);

            Bore(root, "Handguard", 0f, 0.27f, bore, 0.05f, GunPolymer);
            for (int i = 0; i < 4; i++) Span(root, "Vent", 0.03f + i * 0.06f, 0.06f + i * 0.06f, 0.022f, 0.038f, 0.002f, GunDark, 0.025f);
            Bore(root, "DeltaRing", -0.012f, 0f, bore, 0.058f, GunMatte);
            Bore(root, "HandguardCap", 0.27f, 0.28f, bore, 0.044f, GunMatte);

            Span(root, "Upper", -0.245f, -0.012f, 0.012f, 0.05f, 0.03f, GunMatte);
            Span(root, "CarryHandle", -0.235f, -0.07f, 0.074f, 0.088f, 0.016f, GunMatte);
            Span(root, "HandleFront", -0.09f, -0.07f, 0.05f, 0.074f, 0.016f, GunMatte);
            Span(root, "HandleRear", -0.235f, -0.205f, 0.05f, 0.074f, 0.016f, GunMatte);
            Span(root, "RearSight", -0.235f, -0.22f, 0.088f, 0.1f, 0.012f, GunMatte);
            Span(root, "Peep", -0.236f, -0.219f, 0.092f, 0.097f, 0.004f, GunDark);
            Span(root, "ChargingHandle", -0.26f, -0.24f, 0.055f, 0.068f, 0.02f, GunMatte);
            Span(root, "EjectionPort", -0.16f, -0.09f, 0.022f, 0.044f, 0.002f, GunDark, side + 0.0005f);

            Span(root, "Lower", -0.245f, -0.02f, -0.014f, 0.012f, 0.03f, GunMatte);
            Span(root, "Magwell", -0.12f, -0.05f, -0.04f, -0.014f, 0.03f, GunMatte);
            Sphere(root, "MagRelease", new Vector3(-0.045f, side + 0.001f, -0.022f), Vector3.zero, new Vector3(0.01f, 0.004f, 0.01f), GunSteel);
            Sphere(root, "Selector", new Vector3(-0.2f, side + 0.001f, -0.002f), Vector3.zero, new Vector3(0.012f, 0.004f, 0.012f), GunSteel);
            PartNode mag = Raked(root, "Magazine", -0.085f, -0.04f, -10f);
            Span(mag, "Body", -0.032f, 0.032f, -0.15f, 0f, 0.024f, GunMatte);
            Span(mag, "Floorplate", -0.036f, 0.036f, -0.156f, -0.15f, 0.026f, GunPolymer);

            Span(root, "GuardBottom", -0.17f, -0.11f, -0.04f, -0.035f, 0.012f, GunMatte);
            Span(root, "GuardFront", -0.115f, -0.11f, -0.04f, -0.014f, 0.012f, GunMatte);
            Span(root, "Trigger", -0.146f, -0.14f, -0.034f, -0.014f, 0.006f, GunSteel);
            PartNode grip = Raked(root, "PistolGrip", -0.19f, -0.014f, 22f);
            Span(grip, "Grip", -0.018f, 0.018f, -0.09f, 0f, 0.028f, GunPolymer);

            Span(root, "Stock", -0.48f, -0.245f, -0.004f, 0.046f, 0.036f, GunPolymer);
            Tilt(root, "Toe", -0.37f, -0.03f, 0.22f, 0.036f, 0.05f, 8f, GunPolymer);
            Span(root, "ButtPlate", -0.495f, -0.478f, -0.075f, 0.05f, 0.038f, GunPolymer);
            Span(root, "Trapdoor", -0.4955f, -0.494f, -0.04f, 0.03f, 0.02f, GunDark);
            return root;
        }
        #endregion

        #region The Zone
        /// <summary>The Soviet marksman's rifle: a long barrel with a slotted flash hider, the ventilated laminate handguard
        /// over the gas tube, the AK-pattern receiver with its charging handle and safety lever, a 10-round magazine, and the
        /// skeletal thumbhole stock with its cheek rest.</summary>
        private static PartNode BuildSvd(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.038f, side = 0.016f;

            Bore(root, "Barrel", 0f, 0.56f, bore, 0.02f, GunBlued);
            Bore(root, "FlashHider", 0.56f, 0.61f, bore, 0.026f, GunBlued);
            for (int i = 0; i < 3; i++) Span(root, "Slot", 0.57f + i * 0.012f, 0.578f + i * 0.012f, 0.03f, 0.046f, 0.002f, GunDark, 0.0131f);
            Span(root, "FrontSight", 0.53f, 0.55f, 0.048f, 0.066f, 0.014f, GunBlued);
            Span(root, "GasBlock", 0.2f, 0.245f, 0.03f, 0.068f, 0.026f, GunBlued);
            Bore(root, "GasTube", 0f, 0.2f, 0.06f, 0.018f, GunBlued);
            Bore(root, "Handguard", 0f, 0.2f, 0.045f, 0.05f, GunLaminate);
            for (int i = 0; i < 3; i++) Span(root, "Vent", 0.03f + i * 0.055f, 0.055f + i * 0.055f, 0.04f, 0.05f, 0.002f, GunDark, 0.0251f);
            Bore(root, "RingRear", -0.008f, 0.002f, 0.045f, 0.054f, GunBlued);
            Bore(root, "RingFront", 0.198f, 0.208f, 0.045f, 0.054f, GunBlued);

            Span(root, "Receiver", -0.26f, 0f, 0.018f, 0.062f, 0.032f, GunBlued);
            Span(root, "DustCover", -0.26f, -0.02f, 0.062f, 0.07f, 0.03f, GunBluedHi);
            Span(root, "RearSight", -0.03f, 0f, 0.062f, 0.074f, 0.016f, GunBlued);
            Span(root, "ChargingHandle", -0.07f, -0.05f, 0.048f, 0.056f, 0.012f, GunSteel, 0.022f);
            Span(root, "SafetyLever", -0.22f, -0.1f, 0.03f, 0.036f, 0.003f, GunBlued, side + 0.0005f);
            PartNode mag = Raked(root, "Magazine", -0.14f, 0.018f, -6f);
            Span(mag, "Body", -0.035f, 0.035f, -0.105f, 0f, 0.026f, GunBlued);

            Span(root, "GuardBottom", -0.245f, -0.19f, -0.004f, 0.002f, 0.012f, GunBlued);
            Span(root, "GuardFront", -0.196f, -0.19f, -0.004f, 0.018f, 0.012f, GunBlued);
            Span(root, "Trigger", -0.222f, -0.216f, 0f, 0.016f, 0.006f, GunSteel);

            // The thumbhole stock: the grip post, the bar under the hole, the comb over it, the cheek rest and the butt.
            PartNode post = Raked(root, "GripPost", -0.27f, 0.018f, 16f);
            Span(post, "Post", -0.02f, 0.02f, -0.09f, 0f, 0.03f, GunLaminate);
            Tilt(root, "LowerBar", -0.43f, -0.075f, 0.34f, 0.03f, 0.026f, 2f, GunLaminate);
            Span(root, "Comb", -0.59f, -0.26f, 0.022f, 0.058f, 0.034f, GunLaminate);
            Span(root, "CheekRest", -0.5f, -0.36f, 0.058f, 0.072f, 0.034f, GunWalnutDark);
            Span(root, "Butt", -0.61f, -0.585f, -0.095f, 0.062f, 0.036f, GunLaminate);
            Span(root, "ButtPlate", -0.615f, -0.605f, -0.095f, 0.062f, 0.036f, GunDark);
            return root;
        }

        /// <summary>A belt-fed general-purpose machine gun: the fluted barrel with its conical flash hider, tall front sight
        /// and carrying handle, the bipod folded under the gas tube, the boxy receiver with its feed cover and the belt
        /// running up into it from a 100-round box slung underneath, a pistol grip and the skeleton stock.</summary>
        private static PartNode BuildPkm(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.035f, side = 0.021f;

            Bore(root, "Barrel", 0.05f, 0.56f, bore, 0.024f, GunMatte);
            Span(root, "Flute", 0.08f, 0.38f, bore - 0.002f, bore + 0.002f, 0.002f, GunDark, 0.0121f);
            Bore(root, "FlashHider", 0.56f, 0.595f, bore, 0.03f, GunMatte);
            Bore(root, "BoreHole", 0.594f, 0.5955f, bore, 0.012f, GunDark);
            Span(root, "FrontSight", 0.5f, 0.515f, 0.048f, 0.082f, 0.02f, GunMatte);
            Span(root, "GasBlock", 0.4f, 0.43f, 0f, 0.048f, 0.026f, GunMatte);
            Bore(root, "GasTube", 0.05f, 0.4f, 0.012f, 0.02f, GunMatte);

            Bore(root, "BipodLegRight", 0.12f, 0.39f, -0.004f, 0.009f, GunMatte, 0.006f);
            Bore(root, "BipodLegLeft", 0.12f, 0.39f, -0.004f, 0.009f, GunMatte, -0.006f);
            Span(root, "BipodFeet", 0.11f, 0.125f, -0.012f, 0.004f, 0.024f, GunMatte);
            Span(root, "BipodClamp", 0.39f, 0.4f, -0.012f, 0.02f, 0.03f, GunMatte);

            Span(root, "HandleBar", 0.07f, 0.2f, 0.085f, 0.095f, 0.014f, GunPolymer);
            Span(root, "HandleFront", 0.185f, 0.2f, 0.045f, 0.088f, 0.012f, GunMatte);
            Span(root, "HandleRear", 0.07f, 0.085f, 0.045f, 0.088f, 0.012f, GunMatte);

            Span(root, "Receiver", -0.27f, 0.05f, -0.004f, 0.064f, 0.042f, GunMatte);
            Span(root, "FeedCover", -0.21f, 0f, 0.064f, 0.08f, 0.044f, GunMatte);
            Span(root, "CoverLatch", -0.215f, -0.2f, 0.074f, 0.084f, 0.03f, GunSteel);
            Span(root, "RearSight", -0.005f, 0.012f, 0.064f, 0.084f, 0.02f, GunMatte);
            Span(root, "FeedTray", -0.13f, -0.08f, 0.03f, 0.06f, 0.006f, GunMatte, side + 0.002f);
            Span(root, "ChargingHandle", -0.02f, 0f, 0.014f, 0.026f, 0.01f, GunSteel, side + 0.005f);
            for (int i = 0; i < 4; i++)
            {
                float z = 0.04f - i * 0.014f;
                Bore(root, "Round", -0.125f, -0.06f, z, 0.011f, GunBrass, side + 0.009f);
                Span(root, "Link", -0.13f, -0.12f, z - 0.006f, z + 0.006f, 0.012f, GunDark, side + 0.009f);
            }

            Span(root, "AmmoBox", -0.115f, 0.02f, -0.12f, -0.004f, 0.052f, GunOlive);
            Span(root, "BoxLid", -0.115f, 0.02f, -0.012f, -0.004f, 0.054f, GunSteel);
            Span(root, "BoxHandle", -0.07f, -0.03f, -0.062f, -0.052f, 0.004f, GunSteel, 0.028f);

            PartNode grip = Raked(root, "PistolGrip", -0.17f, -0.004f, 16f);
            Span(grip, "Grip", -0.018f, 0.018f, -0.088f, 0f, 0.03f, GunPolymer);
            Span(root, "GuardBottom", -0.15f, -0.1f, -0.032f, -0.026f, 0.012f, GunMatte);
            Span(root, "GuardFront", -0.106f, -0.1f, -0.032f, -0.004f, 0.012f, GunMatte);
            Span(root, "Trigger", -0.13f, -0.124f, -0.026f, -0.004f, 0.006f, GunSteel);

            Span(root, "StockTop", -0.585f, -0.27f, 0.026f, 0.05f, 0.034f, GunPolymer);
            Span(root, "StockUpright", -0.3f, -0.27f, -0.04f, 0.05f, 0.034f, GunPolymer);
            Tilt(root, "StockBottom", -0.45f, -0.06f, 0.3f, 0.03f, 0.024f, 6f, GunPolymer);
            Span(root, "Butt", -0.595f, -0.575f, -0.09f, 0.055f, 0.036f, GunPolymer);
            Span(root, "ButtPlate", -0.6f, -0.595f, -0.09f, 0.055f, 0.036f, GunDark);
            return root;
        }

        /// <summary>The short AK: an eight-inch barrel ending in the big cylindrical muzzle booster, the combined gas block and
        /// front sight, plum handguards, the receiver with its hinged top cover carrying the flip rear sight, a curved
        /// 30-round magazine, a plum pistol grip and the triangular side-folding stock, unfolded.</summary>
        private static PartNode BuildAks74u(ItemDef def, PartNode parent, out float baseHeight)
        {
            PartNode root = GunRoot(parent, out baseHeight);
            const float bore = 0.036f, side = 0.015f;

            Bore(root, "Barrel", 0.11f, 0.3f, bore, 0.018f, GunBlued);
            Bore(root, "Booster", 0.3f, 0.357f, bore, 0.034f, GunBlued);
            Bore(root, "BoosterCone", 0.355f, 0.365f, bore, 0.028f, GunBluedHi);
            Bore(root, "BoreHole", 0.3645f, 0.366f, bore, 0.012f, GunDark);
            Span(root, "GasBlock", 0.21f, 0.255f, 0.026f, 0.078f, 0.026f, GunBlued);
            Span(root, "FrontSight", 0.225f, 0.24f, 0.078f, 0.09f, 0.02f, GunBlued);
            Bore(root, "UpperHandguard", 0.09f, 0.21f, 0.064f, 0.024f, GunPlum);
            Span(root, "LowerHandguard", 0.09f, 0.21f, 0.012f, 0.05f, 0.036f, GunPlum);
            Span(root, "Retainer", 0.085f, 0.095f, 0.01f, 0.054f, 0.038f, GunBlued);

            Span(root, "Receiver", -0.12f, 0.11f, 0.018f, 0.062f, 0.03f, GunBlued);
            Span(root, "TopCover", -0.12f, 0.09f, 0.062f, 0.07f, 0.028f, GunBluedHi);
            Span(root, "RearSight", 0.07f, 0.09f, 0.07f, 0.08f, 0.016f, GunBlued);
            Span(root, "ChargingHandle", 0.055f, 0.075f, 0.046f, 0.056f, 0.01f, GunSteel, 0.02f);
            Span(root, "SafetyLever", -0.06f, 0.05f, 0.032f, 0.04f, 0.003f, GunBlued, side + 0.0005f);
            Span(root, "EjectionPort", -0.02f, 0.06f, 0.042f, 0.06f, 0.002f, GunDark, side + 0.0005f);
            PartNode mag = Raked(root, "Magazine", 0.04f, 0.018f, -18f);
            Span(mag, "Body", -0.034f, 0.034f, -0.17f, 0f, 0.026f, GunPlum);

            Span(root, "GuardBottom", -0.06f, -0.005f, -0.004f, 0.002f, 0.012f, GunBlued);
            Span(root, "GuardFront", -0.011f, -0.005f, -0.004f, 0.018f, 0.012f, GunBlued);
            Span(root, "Trigger", -0.035f, -0.029f, 0f, 0.016f, 0.006f, GunSteel);
            PartNode grip = Raked(root, "PistolGrip", -0.075f, 0.018f, 18f);
            Span(grip, "Grip", -0.016f, 0.016f, -0.085f, 0f, 0.028f, GunPlum);

            Span(root, "StockHinge", -0.13f, -0.115f, 0f, 0.055f, 0.034f, GunBlued);
            Bore(root, "StockTop", -0.35f, -0.12f, 0.048f, 0.012f, GunBlued);
            Tilt(root, "StockStrut", -0.2325f, -0.013f, 0.228f, 0.012f, 0.012f, 8.6f, GunBlued);
            Span(root, "ButtPlate", -0.365f, -0.345f, -0.045f, 0.058f, 0.03f, GunBlued);
            Span(root, "ButtPad", -0.367f, -0.362f, -0.045f, 0.058f, 0.03f, GunDark);
            return root;
        }
        #endregion
    }
}
