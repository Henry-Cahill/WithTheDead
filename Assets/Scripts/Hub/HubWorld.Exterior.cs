using UnityEngine;

namespace CloneZ
{
    // The Kettle as it stands in the town: barn-red clapboard with white corner boards on a concrete foundation, a low
    // gable roof of rusting tin under its snow, icicles along the eaves and the woodstove's brick chimney, neon on the
    // roof and a pole sign by the road, a covered porch with its lantern, and the front door onto the street. The roof,
    // its sign and the porch roof live under the hub's overhead root (out of the NavMesh bake), and lift away in the
    // isometric view while the survivor is inside (KettleRoof).
    public static partial class HubWorld
    {
        private const float RoofRise = 2.0f, Eave = 0.5f, Skin = 0.05f;

        // The outer faces of the walls (the siding's surface).
        private static float OuterX => HalfX + WallT * 0.5f + Skin;
        private static float OuterZ => HalfZ + WallT * 0.5f + Skin;

        private static void BuildExterior(Transform root, Transform overhead, Style st)
        {
            var roof = new GameObject("Roof").transform;
            roof.SetParent(overhead, false);
            BuildCorners(root, st);
            BuildRoof(roof, st);
            BuildRoofSign(roof);
            BuildPorch(root, roof, st);
            BuildPoleSign(root);
            BuildWindowNeon(root);
            BuildDrifts(root);
            BuildFrontDoor(root, st);
            var cutaway = overhead.gameObject.AddComponent<KettleRoof>();
            cutaway.Room = root;
            cutaway.Parts = roof;
        }

        // White corner boards where the siding meets, and a frieze board under the eaves.
        private static void BuildCorners(Transform root, Style st)
        {
            float x = OuterX, z = OuterZ;
            void Corner(string wall, float sx, float sz) => Deco(WallOf(root, wall), "CornerBoard", m =>
                m.Box(new Vector3(sx * (x - 0.03f), WallH * 0.5f, sz * (z - 0.03f)), new Vector3(0.14f, WallH, 0.14f), st.SidingTrim),
                new Vector3(sx * x, WallH * 0.5f, sz * z));
            Corner("Wall_N", -1f, 1f);
            Corner("Wall_N", 1f, 1f);
            Corner("Wall_S1", -1f, -1f);
            Corner("Wall_S2", 1f, -1f);
            Deco(WallOf(root, "Wall_N"), "Frieze", m => m.Box(new Vector3(0f, WallH - 0.12f, z + 0.02f), new Vector3(x * 2f, 0.22f, 0.04f), st.SidingTrim), new Vector3(0f, WallH, z));
            Deco(WallOf(root, "Wall_S1"), "Frieze", m => m.Box(new Vector3(-x * 0.5f, WallH - 0.12f, -z - 0.02f), new Vector3(x, 0.22f, 0.04f), st.SidingTrim), new Vector3(-x * 0.5f, WallH, -z));
            Deco(WallOf(root, "Wall_S2"), "Frieze", m => m.Box(new Vector3(x * 0.5f, WallH - 0.12f, -z - 0.02f), new Vector3(x, 0.22f, 0.04f), st.SidingTrim), new Vector3(x * 0.5f, WallH, -z));
            Deco(WallOf(root, "Wall_E"), "Frieze", m => m.Box(new Vector3(x + 0.02f, WallH - 0.12f, 0f), new Vector3(0.04f, 0.22f, z * 2f), st.SidingTrim), new Vector3(x, WallH, 0f));
            Deco(WallOf(root, "Wall_W"), "Frieze", m => m.Box(new Vector3(-x - 0.02f, WallH - 0.12f, 0f), new Vector3(0.04f, 0.22f, z * 2f), st.SidingTrim), new Vector3(-x, WallH, 0f));
            // The outside of the doorway: a white casing round it.
            Deco(WallOf(root, "Lintel"), "DoorCasingOutside", m =>
            {
                float f = -z - 0.02f;
                m.Box(new Vector3(-1.18f, 1.22f, f), new Vector3(0.14f, 2.44f, 0.04f), st.SidingTrim);
                m.Box(new Vector3(1.18f, 1.22f, f), new Vector3(0.14f, 2.44f, 0.04f), st.SidingTrim);
                m.Box(new Vector3(0f, 2.5f, f), new Vector3(2.5f, 0.16f, 0.04f), st.SidingTrim);
            }, new Vector3(0f, 1.2f, -z));
        }

        // A roof slope: a slab of tin under its snow, drawn level about its own middle and turned into place, so its
        // collider lies with it (the cutaway and the shots see the roof where it is).
        private static void Slope(Transform parent, string name, Vector3 middle, Quaternion turn, Vector3 size, float towardRidge, in Surf tin)
        {
            var m = new RaidMesh();
            m.Box(middle, size, tin);
            m.Box(middle + new Vector3(0f, size.y * 0.5f + 0.06f, towardRidge * 0.14f), new Vector3(size.x - 0.24f, 0.12f, size.z - 0.5f), DecorMaterials.Snow);
            GameObject go = m.Emit(name, parent, middle);
            go.transform.rotation = turn;
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.06f, 0f);
            col.size = new Vector3(size.x, size.y + 0.12f, size.z);
        }

        private static void BuildRoof(Transform roof, Style st)
        {
            const float T = 0.14f;
            float zw = OuterZ, run = zw + Eave;
            float pitch = Mathf.Atan2(RoofRise, zw);
            float drop = Eave * Mathf.Tan(pitch);
            float length = Mathf.Sqrt(run * run + (RoofRise + drop) * (RoofRise + drop));
            float width = (OuterX + Eave) * 2f;
            float midY = WallH + (RoofRise - drop) * 0.5f;
            float deg = pitch * Mathf.Rad2Deg;
            Vector3 nFront = new Vector3(0f, Mathf.Cos(pitch), -Mathf.Sin(pitch)), nBack = new Vector3(0f, Mathf.Cos(pitch), Mathf.Sin(pitch));
            Slope(roof, "RoofFront", new Vector3(0f, midY, -run * 0.5f) + nFront * (T * 0.5f), Quaternion.Euler(-deg, 0f, 0f), new Vector3(width, T, length), 1f, st.Roof);
            Slope(roof, "RoofBack", new Vector3(0f, midY, run * 0.5f) + nBack * (T * 0.5f), Quaternion.Euler(deg, 0f, 0f), new Vector3(width, T, length), -1f, st.Roof);

            float eaveY = WallH - drop;
            Deco(roof, "RoofTrim", m =>
            {
                // The ridge, snowed over.
                m.Box(new Vector3(0f, WallH + RoofRise + T + 0.02f, 0f), new Vector3(width, 0.14f, 0.5f), DecorMaterials.Snow);
                // The gables: siding up to the ridge, a louvred vent, and white rake boards along the eaves.
                foreach (float s in new[] { -1f, 1f })
                {
                    float x = s * OuterX;
                    var a = new Vector3(x, WallH, -zw);
                    var b = new Vector3(x, WallH, zw);
                    var c = new Vector3(x, WallH + RoofRise, 0f);
                    m.Tri(a, b, c, st.Siding);
                    m.Tri(a, c, b, st.Siding);
                    m.Box(new Vector3(x + s * 0.02f, WallH + 0.95f, 0f), new Vector3(0.04f, 0.6f, 0.9f), DecorPalette.Matte(new Color(0.12f, 0.12f, 0.13f)));
                    for (int i = 0; i < 5; i++)
                        m.Box(new Vector3(x + s * 0.045f, WallH + 0.72f + i * 0.11f, 0f), new Vector3(0.02f, 0.04f, 0.86f), st.SidingTrim);
                    float rx = s * (OuterX + Eave - 0.04f);
                    m.Rod(new Vector3(rx, eaveY - 0.02f, -run), new Vector3(rx, WallH + RoofRise + 0.05f, 0f), 0.09f, st.SidingTrim);
                    m.Rod(new Vector3(rx, eaveY - 0.02f, run), new Vector3(rx, WallH + RoofRise + 0.05f, 0f), 0.09f, st.SidingTrim);
                }
                // Fascia boards along both eaves, and icicles hanging from them.
                Surf ice = DecorPalette.Glass(new Color(0.78f, 0.88f, 0.96f));
                var rng = new System.Random(1993);
                foreach (float s in new[] { -1f, 1f })
                {
                    m.Box(new Vector3(0f, eaveY - 0.07f, s * (run + 0.02f)), new Vector3(width, 0.16f, 0.05f), st.SidingTrim);
                    for (float x = -width * 0.5f + 0.4f; x < width * 0.5f - 0.3f; x += 0.55f + (float)rng.NextDouble() * 0.9f)
                    {
                        float h = 0.12f + (float)rng.NextDouble() * 0.42f;
                        m.Cylinder(new Vector3(x, eaveY - 0.14f, s * (run - 0.02f)), 0.025f + (float)rng.NextDouble() * 0.025f, h, 6, ice,
                            Quaternion.Euler(180f, 0f, 0f), 0.05f);
                    }
                }
                // The woodstove's chimney through the front slope, capped, with snow on the cap.
                Surf brick = DecorPalette.Matte(new Color(0.46f, 0.22f, 0.16f));
                var ch = new Vector3(StoveAt.x, 0f, StoveAt.z + 0.1f);
                m.Box(new Vector3(ch.x, (3.9f + ChimneyTop) * 0.5f, ch.z), new Vector3(0.62f, ChimneyTop - 3.9f, 0.62f), brick);
                m.Box(new Vector3(ch.x, ChimneyTop + 0.04f, ch.z), new Vector3(0.74f, 0.08f, 0.74f), DecorPalette.Metal(new Color(0.30f, 0.29f, 0.28f)));
                m.Box(new Vector3(ch.x, ChimneyTop + 0.11f, ch.z), new Vector3(0.64f, 0.07f, 0.64f), DecorMaterials.Snow);
            }, new Vector3(0f, WallH + 1f, 0f));
        }

        // THE RUSTY KETTLE in orange neon on a board on the front slope, marquee bulbs round it, lit for the street.
        private static void BuildRoofSign(Transform roof)
        {
            Surf neon = Neon("THE RUSTY KETTLE", new Color(1f, 0.42f, 0.18f), out float aspect);
            const float H = 0.72f, Z = -5.6f;
            float w = H * aspect;
            float roofY = WallH + RoofRise * (1f - Mathf.Abs(Z) / OuterZ);
            var c = new Vector3(0f, roofY + 0.55f + H * 0.5f, Z);
            Deco(roof, "RoofSign", m =>
            {
                Surf board = DecorPalette.Matte(new Color(0.05f, 0.11f, 0.08f));
                Surf post = DecorPalette.Metal(new Color(0.22f, 0.22f, 0.23f));
                m.Box(c + new Vector3(0f, 0f, 0.06f), new Vector3(w + 0.5f, H + 0.42f, 0.12f), board);
                m.Upright(c + new Vector3(0f, 0f, -0.006f), Vector3.back, w, H, neon, FullUv);
                Surf bulb = DecorPalette.Glow(new Color(1f, 0.92f, 0.70f));
                float bx = w * 0.5f + 0.17f, by = H * 0.5f + 0.13f;
                for (float x = -bx; x <= bx + 0.01f; x += 0.3f)
                {
                    m.Sphere(new Vector3(x, c.y + by, c.z - 0.02f), 0.035f, bulb);
                    m.Sphere(new Vector3(x, c.y - by, c.z - 0.02f), 0.035f, bulb);
                }
                for (float y = -by + 0.3f; y < by - 0.1f; y += 0.3f)
                {
                    m.Sphere(new Vector3(-bx, c.y + y, c.z - 0.02f), 0.035f, bulb);
                    m.Sphere(new Vector3(bx, c.y + y, c.z - 0.02f), 0.035f, bulb);
                }
                foreach (float x in new[] { -w * 0.5f + 0.4f, 0f, w * 0.5f - 0.4f })
                    m.Box(new Vector3(x, (roofY - 0.2f + c.y - H * 0.5f) * 0.5f, Z + 0.15f), new Vector3(0.1f, c.y - H * 0.5f - roofY + 0.2f, 0.1f), post);
            }, c);
            PointLight(roof, "RoofSignGlow", c + new Vector3(0f, -0.4f, -1.4f), new Color(1f, 0.52f, 0.32f), 1.4f, 7f);
        }

        // A covered porch across the door: a plank deck two steps up from the snow, posts, side rails, a tin shed roof with
        // its fascia sign, a lantern by the door and a mat to stamp the snow off on.
        private static void BuildPorch(Transform root, Transform roof, Style st)
        {
            const float D = 2.6f, W = 6.4f;
            float zf = -OuterZ, zEdge = zf - D;
            Surf planks = DecorMaterials.Planks(new Color(0.46f, 0.40f, 0.33f));
            Solid(root, "PorchDeck", new Vector3(0f, -Lift * 0.5f, zf - D * 0.5f), new Vector3(W, Lift, D), planks);
            Solid(root, "PorchStep1", new Vector3(0f, -Lift + 0.1f, zEdge - 0.17f), new Vector3(3.4f, 0.2f, 0.34f), planks);
            Solid(root, "PorchStep2", new Vector3(0f, -Lift + 0.05f, zEdge - 0.51f), new Vector3(3.4f, 0.1f, 0.34f), planks);
            foreach (float s in new[] { -1f, 1f })
            {
                Solid(root, "PorchPost", new Vector3(s * (W * 0.5f - 0.12f), 1.3f, zEdge + 0.12f), new Vector3(0.14f, 2.6f, 0.14f), st.SidingTrim);
                float x = s * (W * 0.5f - 0.07f);
                Prop(root, "PorchRail", new Vector3(x, 0.47f, zf - (D - 0.12f) * 0.5f), new Vector3(0.1f, 0.95f, D - 0.12f), m =>
                {
                    m.Box(new Vector3(x, 0.92f, zf - (D - 0.12f) * 0.5f), new Vector3(0.08f, 0.06f, D - 0.12f), st.SidingTrim);
                    m.Box(new Vector3(x, 0.14f, zf - (D - 0.12f) * 0.5f), new Vector3(0.06f, 0.05f, D - 0.12f), st.SidingTrim);
                    for (float z = zf - 0.2f; z > zEdge + 0.25f; z -= 0.24f)
                        m.Box(new Vector3(x, 0.53f, z), new Vector3(0.04f, 0.75f, 0.04f), st.SidingTrim);
                });
            }
            Deco(root, "Doormat", m => m.Box(new Vector3(0f, 0.006f, zf - 0.55f), new Vector3(1.2f, 0.012f, 0.65f), DecorPalette.Matte(new Color(0.30f, 0.20f, 0.12f))), new Vector3(0f, 0f, zf - 0.55f));

            // The shed roof over it, falling from the wall to the posts.
            const float Top = 2.98f, Low = 2.62f, T = 0.1f;
            float run = D + 0.25f;
            float slope = Mathf.Atan2(Top - Low, run);
            var n = new Vector3(0f, Mathf.Cos(slope), -Mathf.Sin(slope));
            Slope(roof, "PorchRoof", new Vector3(0f, (Top + Low) * 0.5f, zf - run * 0.5f) + n * (T * 0.5f), Quaternion.Euler(-slope * Mathf.Rad2Deg, 0f, 0f),
                new Vector3(W + 0.5f, T, Mathf.Sqrt(run * run + (Top - Low) * (Top - Low))), 1f, st.Roof);
            Surf painted = DecorMaterials.Sign("BOURBON - BEER - EATS", new Color(0.07f, 0.18f, 0.12f), new Color(0.95f, 0.88f, 0.70f), out float aspect);
            var sign = new Vector3(0f, Low - 0.2f, zf - run - 0.02f);
            Deco(roof, "PorchSign", m =>
            {
                m.Box(sign + new Vector3(0f, 0f, 0.03f), new Vector3(W + 0.5f, 0.3f, 0.05f), st.SidingTrim);
                m.Upright(sign, Vector3.back, 0.22f * aspect, 0.22f, painted, FullUv);
            }, sign);

            // The lantern by the door: the porch's light, the one that casts shadows on the snow.
            var lamp = new Vector3(1.6f, 2.05f, zf - 0.12f);
            Deco(root, "PorchLantern", m =>
            {
                Surf iron = DecorPalette.Metal(new Color(0.08f, 0.08f, 0.08f));
                m.Box(lamp, new Vector3(0.2f, 0.3f, 0.2f), DecorPalette.Glow(new Color(1f, 0.80f, 0.52f)));
                m.Box(lamp + new Vector3(0f, 0.17f, 0f), new Vector3(0.26f, 0.05f, 0.26f), iron);
                m.Box(lamp + new Vector3(0f, -0.16f, 0f), new Vector3(0.22f, 0.03f, 0.22f), iron);
                m.Rod(lamp + new Vector3(0f, 0.12f, 0.1f), lamp + new Vector3(0f, 0.12f, 0.14f), 0.03f, iron);
            }, lamp);
            PointLight(root, "PorchLight", lamp + new Vector3(-0.4f, -0.1f, -0.8f), new Color(1f, 0.78f, 0.50f), 1.8f, 8f, true);
        }

        // A pole sign by the road, its faces turned up and down the street for the traffic.
        private static void BuildPoleSign(Transform root)
        {
            var b = new Vector3(-12.2f, -Lift, -12.6f);
            Solid(root, "SignPole", b + new Vector3(0f, 2.3f, 0f), new Vector3(0.18f, 4.6f, 0.18f), DecorPalette.Metal(new Color(0.42f, 0.42f, 0.44f)));
            Surf top = Neon("THE RUSTY", new Color(1f, 0.45f, 0.2f), out float a1);
            Surf bottom = Neon("KETTLE", new Color(1f, 0.45f, 0.2f), out float a2);
            Surf open = Neon("OPEN", new Color(1f, 0.2f, 0.3f), out float a3);
            var c = b + new Vector3(0f, 4.05f, 0f);
            Deco(root, "PoleSign", m =>
            {
                m.Box(c, new Vector3(0.3f, 1.1f, 2.6f), DecorPalette.Matte(new Color(0.05f, 0.11f, 0.08f)));
                m.Box(c + new Vector3(0f, 0.58f, 0f), new Vector3(0.34f, 0.06f, 2.66f), DecorPalette.Metal(new Color(0.75f, 0.75f, 0.78f)));
                m.Box(c + new Vector3(0f, -0.58f, 0f), new Vector3(0.34f, 0.06f, 2.66f), DecorPalette.Metal(new Color(0.75f, 0.75f, 0.78f)));
                // The OPEN box hangs on the pole (which runs up through it), so its faces stand proud of the pole.
                Vector3 o = c + new Vector3(0f, -1.0f, 0f);
                m.Box(o, new Vector3(0.26f, 0.42f, 0.42f * a3 + 0.12f), DecorPalette.Matte(new Color(0.04f, 0.04f, 0.05f)));
                m.Box(c + new Vector3(0f, -0.7f, 0f), new Vector3(0.02f, 0.18f, 0.02f), DecorPalette.Metal(new Color(0.3f, 0.3f, 0.3f)));
                foreach (float s in new[] { -1f, 1f })
                {
                    var face = new Vector3(s, 0f, 0f);
                    m.Upright(c + face * 0.153f + new Vector3(0f, 0.22f, 0f), face, 0.34f * a1, 0.34f, top, FullUv);
                    m.Upright(c + face * 0.153f + new Vector3(0f, -0.22f, 0f), face, 0.34f * a2, 0.34f, bottom, FullUv);
                    m.Upright(o + face * 0.133f, face, 0.3f * a3, 0.3f, open, FullUv);
                }
            }, c);
            PointLight(root, "PoleSignGlow", c + new Vector3(1.2f, -0.4f, 0f), new Color(1f, 0.45f, 0.28f), 0.9f, 4.5f);
        }

        // Neon in the front windows, for the street.
        private static void BuildWindowNeon(Transform root)
        {
            float z = -OuterZ - 0.015f;
            Surf open = Neon("OPEN", new Color(1f, 0.2f, 0.28f), out float a1);
            Deco(WallOf(root, "Wall_S1"), "WindowNeon", m => m.Upright(new Vector3(-10.6f, 1.78f, z), Vector3.back, 0.3f * a1, 0.3f, open, FullUv), new Vector3(-10.6f, 1.78f, z));
            Surf beer = Neon("COLD BEER", new Color(0.3f, 0.62f, 1f), out float a2);
            Deco(WallOf(root, "Wall_S2"), "WindowNeon", m => m.Upright(new Vector3(6.0f, 1.82f, z), Vector3.back, 0.22f * a2, 0.22f, beer, FullUv), new Vector3(6.0f, 1.82f, z));
        }

        // Snow drifted against the walls, deeper on the back and sides than by the porch.
        private static void BuildDrifts(Transform root)
        {
            var rng = new System.Random(1217);
            Deco(root, "Drifts", m =>
            {
                void Run(Vector3 from, Vector3 to, Vector3 outward, float depth)
                {
                    float len = Vector3.Distance(from, to);
                    Vector3 dir = (to - from) / len;
                    float t = 0f;
                    while (t < len - 0.4f)
                    {
                        float seg = Mathf.Min(len - t, 1.2f + (float)rng.NextDouble() * 1.6f);
                        float h = 0.1f + (float)rng.NextDouble() * 0.22f, d = depth * (0.6f + (float)rng.NextDouble() * 0.6f);
                        Vector3 mid = from + dir * (t + seg * 0.5f) + outward * (d * 0.5f) + Vector3.up * (-Lift + h * 0.5f);
                        m.Box(mid, new Vector3(Mathf.Abs(dir.x) * seg + Mathf.Abs(outward.x) * d, h, Mathf.Abs(dir.z) * seg + Mathf.Abs(outward.z) * d), DecorMaterials.Snow);
                        t += seg + 0.1f + (float)rng.NextDouble() * 0.6f;
                    }
                }
                float x = OuterX + 0.06f, z = OuterZ + 0.06f;
                Run(new Vector3(-x, 0f, z), new Vector3(x, 0f, z), Vector3.forward, 0.7f);
                Run(new Vector3(x, 0f, z), new Vector3(x, 0f, -z), Vector3.right, 0.55f);
                Run(new Vector3(-x, 0f, -z), new Vector3(-x, 0f, z), Vector3.left, 0.55f);
                Run(new Vector3(-x, 0f, -z), new Vector3(-3.5f, 0f, -z), Vector3.back, 0.4f);
                Run(new Vector3(3.5f, 0f, -z), new Vector3(x, 0f, -z), Vector3.back, 0.4f);
            }, Vector3.zero);
        }

        // The front door: two oak leaves with frosted glass, hung in the doorway on hinges, opening out onto the porch.
        private static void BuildFrontDoor(Transform root, Style st)
        {
            float z = -HalfZ;
            var door = new GameObject("FrontDoor");
            door.transform.SetParent(root, false);
            door.transform.position = new Vector3(0f, 0f, z);
            Surf oak = DecorPalette.Matte(new Color(0.38f, 0.23f, 0.12f));
            Surf brass = DecorPalette.Metal(new Color(0.78f, 0.62f, 0.30f));
            Surf glow = DecorPalette.Glow(new Color(0.80f, 0.56f, 0.32f));
            Surf night = DecorPalette.Glass(new Color(0.07f, 0.10f, 0.17f));
            var hinges = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f;
                var hinge = new GameObject(i == 0 ? "LeftHinge" : "RightHinge").transform;
                hinge.SetParent(door.transform, false);
                hinge.position = new Vector3(s * 1.08f, 0f, z);
                hinges[i] = hinge;
            // Brass door handle – a short cylinder protruding from the door face.
            {
                float handleOffset = 0.08f; // metres outward from door plane
                float handleHeight = 0.1f; // metres above ground
                float handleRadius = 0.02f; // metre radius
                float handleLength = 0.12f; // metre length
                var hPos = new Vector3(s * (1.08f - 0.53f), handleHeight, z + s * handleOffset);
                var mh = new RaidMesh();
                mh.Cylinder(hPos, handleRadius, handleLength, 8, brass, Quaternion.Euler(0f, s > 0 ? 90f : -90f, 0f));
                mh.Emit("Handle", hinge, hPos);
            }
                var mid = new Vector3(s * (1.08f - 0.53f), 1.18f, z);
                var m = new RaidMesh();
                m.Box(mid, new Vector3(1.04f, 2.34f, 0.06f), oak);
                foreach (float f in new[] { -1f, 1f })
                {
                    float face = z + f * 0.031f;
                    var glass = new Vector3(mid.x, 1.62f, face);
                    m.Upright(glass, new Vector3(0f, 0f, f), 0.62f, 0.86f, f < 0f ? glow : night);
                    m.Box(new Vector3(mid.x, 1.62f, face + f * 0.006f), new Vector3(0.03f, 0.86f, 0.012f), oak);
                    m.Box(new Vector3(mid.x, 1.62f, face + f * 0.006f), new Vector3(0.62f, 0.03f, 0.012f), oak);
                    m.Box(new Vector3(mid.x, 0.16f, face + f * 0.004f), new Vector3(0.96f, 0.26f, 0.008f), brass);
                }
                m.Box(new Vector3(mid.x, 1.05f, z + 0.09f), new Vector3(0.8f, 0.05f, 0.05f), brass);
                m.Rod(new Vector3(s * 0.12f, 0.92f, z - 0.09f), new Vector3(s * 0.12f, 1.22f, z - 0.09f), 0.025f, brass);
                GameObject leaf = m.Emit("Leaf", hinge, hinge.position);
                var col = leaf.AddComponent<BoxCollider>();
                // Hinge cap – a small cube capping the hinge bolt.
                {
                    var capSize = new Vector3(0.06f, 0.06f, 0.02f);
                    var capPos = hinge.position + new Vector3(0f, 0f, s * 0.03f);
                    var mc = new RaidMesh();
                    mc.Box(capPos, capSize, DecorPalette.Metal(new Color(0.6f,0.6f,0.6f)));
                    mc.Emit("HingeCap", hinge, capPos);
                }
                col.center = mid - hinge.position;
                col.size = new Vector3(1.04f, 2.34f, 0.08f);
            }
            // Brass keyhole on the left leaf – two orthogonal thin bars.
            {
                var kh = new RaidMesh();
                // Horizontal bar
                kh.Box(new Vector3(-1.08f + 0.12f, 1.4f, z - 0.015f), new Vector3(0.18f, 0.015f, 0.01f), brass);
                // Vertical bar
                kh.Box(new Vector3(-1.08f + 0.12f, 1.4f, z - 0.015f), new Vector3(0.015f, 0.12f, 0.01f), brass);
                kh.Emit("Keyhole", door.transform, Vector3.zero);
            }
            // Brass plaque label near the top centre of the door.
            {
                var plaque = new RaidMesh();
                var plaqueSize = new Vector3(0.48f, 0.06f, 0.02f);
                var plaquePos = new Vector3(0f, 2.0f, z - 0.02f);
                plaque.Box(plaquePos, plaqueSize, brass);
                plaque.Emit("DoorPlaque", door.transform, plaquePos);
            }
            // Randomised scratches – thin dark boxes drawn on the oak leaf.
            {
                var rng = new System.Random(1993);
                var scratch = new RaidMesh();
                for (int i = 0; i < 6; i++)
                {
                    float sx = (float)rng.NextDouble() * 1.8f - 0.9f;
                    float sz = (float)rng.NextDouble() * 0.3f - 0.15f;
                    float sy = 0.5f + (float)rng.NextDouble() * 1.0f;
                    var size = new Vector3(0.02f, 0.02f, 0.001f);
                    var pos = new Vector3(sx, sy, z + sz);
                    scratch.Box(pos, size, DecorPalette.Matte(new Color(0.1f, 0.1f, 0.1f)));
                }
                scratch.Emit("Scratches", door.transform, Vector3.zero);
            }
            // Door‑bell button – small glowing sphere with a point light.
            {
                var bellPos = new Vector3(1.08f - 0.12f, 1.2f, z + 0.06f);
                var mb = new RaidMesh();
                mb.Sphere(bellPos, 0.04f, DecorPalette.Glow(new Color(1f, 0.95f, 0.6f)));
                mb.Emit("BellButton", door.transform, bellPos);
                PointLight(door.transform, "BellLight", bellPos + new Vector3(0f, 0f, -0.1f), new Color(1f, 0.9f, 0.5f), 0.6f, 3f);
            }
            Deco(root, "Threshold", m => m.Box(new Vector3(0f, 0.01f, z), new Vector3(2.2f, 0.02f, 0.32f), DecorPalette.Metal(new Color(0.78f, 0.62f, 0.30f))), new Vector3(0f, 0f, z));
            var kd = door.AddComponent<KettleDoor>();
            kd.Room = root;
            kd.LeftHinge = hinges[0];
            kd.RightHinge = hinges[1];
        }
    }
}
