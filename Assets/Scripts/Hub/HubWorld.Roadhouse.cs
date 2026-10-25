using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The Rusty Kettle as a Kentucky roadhouse in December 1993: a pool table under its lamp, a jukebox by the stage, a
    // dartboard, the TV over the end of the bar, neon in the windows and on the walls, the Christmas lights strung round
    // the room and a tree in the corner, and over every station a board that says what it is.
    public static partial class HubWorld
    {
        private static readonly Dictionary<string, Material> s_Neon = new Dictionary<string, Material>();

        /// <summary>The wall piece a decoration hangs on, so the camera's cutaway takes it with the wall.</summary>
        private static Transform WallOf(Transform root, string name)
        {
            Transform t = root.Find(name);
            return t != null ? t : root;
        }

        // Text that lights itself (unlit, so it reads in a dim room and at night): neon is bright letters on black.
        private static Surf SelfLit(string text, Color bg, Color fg, out float aspect)
        {
            Texture2D tex = DecorTextures.Sign(text, bg, fg, out aspect);
            string key = text + "|" + ColorUtility.ToHtmlStringRGB(bg) + "|" + ColorUtility.ToHtmlStringRGB(fg);
            if (!s_Neon.TryGetValue(key, out Material m) || m == null || m.mainTexture != tex)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Texture");
                m = new Material(sh) { name = "SelfLit " + text, hideFlags = HideFlags.DontUnloadUnusedAsset };
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
                s_Neon[key] = m;
            }
            return new Surf(m, Vector2.zero, 0f);
        }

        private static Surf Neon(string text, Color color, out float aspect) => SelfLit(text, new Color(0.03f, 0.025f, 0.03f), color, out aspect);

        private static readonly Rect FullUv = new Rect(0f, 0f, 1f, 1f);

        private static void PointLight(Transform parent, string name, Vector3 at, Color color, float intensity, float range, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void BuildRoadhouse(Transform root, Transform overhead, Style st)
        {
            BuildPoolTable(root, st);
            BuildJukebox(root, st);
            BuildDarts(root, st);
            BuildTv(root);
            BuildNeon(root);
            BuildChristmas(root);
            BuildAntlers(root, st);
        }

        #region Games and music
        private static void BuildPoolTable(Transform root, Style st)
        {
            var p = new Vector3(5.4f, 0f, -0.8f);
            Surf felt = DecorPalette.Matte(new Color(0.10f, 0.36f, 0.22f));
            Surf cushion = DecorPalette.Matte(new Color(0.08f, 0.28f, 0.17f));
            Prop(root, "PoolTable", p + new Vector3(0f, 0.42f, 0f), new Vector3(2.3f, 0.84f, 1.3f), m =>
            {
                foreach (Vector2 l in new[] { new Vector2(-0.95f, -0.45f), new Vector2(0.95f, -0.45f), new Vector2(-0.95f, 0.45f), new Vector2(0.95f, 0.45f) })
                    m.Box(p + new Vector3(l.x, 0.33f, l.y), new Vector3(0.14f, 0.66f, 0.14f), st.DarkWood);
                m.Box(p + new Vector3(0f, 0.74f, 0f), new Vector3(2.3f, 0.16f, 1.3f), st.DarkWood);
                m.Box(p + new Vector3(0f, 0.83f, 0f), new Vector3(2.04f, 0.02f, 1.04f), felt);
                m.Box(p + new Vector3(0f, 0.86f, 0.585f), new Vector3(2.3f, 0.06f, 0.13f), st.Wood);
                m.Box(p + new Vector3(0f, 0.86f, -0.585f), new Vector3(2.3f, 0.06f, 0.13f), st.Wood);
                m.Box(p + new Vector3(1.085f, 0.86f, 0f), new Vector3(0.13f, 0.06f, 1.04f), st.Wood);
                m.Box(p + new Vector3(-1.085f, 0.86f, 0f), new Vector3(0.13f, 0.06f, 1.04f), st.Wood);
                m.Box(p + new Vector3(0f, 0.85f, 0.51f), new Vector3(2.0f, 0.04f, 0.03f), cushion);
                m.Box(p + new Vector3(0f, 0.85f, -0.51f), new Vector3(2.0f, 0.04f, 0.03f), cushion);
                Surf pocket = DecorPalette.Matte(new Color(0.03f, 0.03f, 0.03f));
                foreach (Vector2 k in new[] { new Vector2(-1.02f, -0.52f), new Vector2(0f, -0.53f), new Vector2(1.02f, -0.52f),
                             new Vector2(-1.02f, 0.52f), new Vector2(0f, 0.53f), new Vector2(1.02f, 0.52f) })
                    m.Cylinder(p + new Vector3(k.x, 0.835f, k.y), 0.055f, 0.012f, 10, pocket);
                // A racked game waiting for someone, and the cue ball.
                Color[] balls =
                {
                    new Color(0.95f, 0.80f, 0.15f), new Color(0.15f, 0.25f, 0.75f), new Color(0.80f, 0.12f, 0.10f), new Color(0.40f, 0.15f, 0.55f),
                    new Color(0.95f, 0.45f, 0.10f), new Color(0.10f, 0.50f, 0.25f), new Color(0.45f, 0.10f, 0.10f), new Color(0.04f, 0.04f, 0.04f),
                };
                int b = 0;
                for (int row = 0; row < 5; row++)
                    for (int i = 0; i <= row; i++)
                        m.Sphere(p + new Vector3(0.45f + row * 0.05f, 0.868f, (i - row * 0.5f) * 0.058f), 0.028f, DecorPalette.Gloss(balls[b++ % balls.Length]));
                m.Sphere(p + new Vector3(-0.55f, 0.868f, 0.05f), 0.028f, DecorPalette.Gloss(new Color(0.93f, 0.92f, 0.86f)));
                m.Rod(p + new Vector3(-0.95f, 0.875f, 0.32f), p + new Vector3(0.25f, 0.875f, 0.42f), 0.012f, st.Wood);
            });
            // The billiard lamp over it, on two chains.
            Deco(root, "PoolLamp", m =>
            {
                Surf shade = DecorPalette.Gloss(new Color(0.10f, 0.26f, 0.16f));
                m.Box(p + new Vector3(0f, 2.22f, 0f), new Vector3(1.3f, 0.16f, 0.36f), shade);
                m.Box(p + new Vector3(0f, 2.135f, 0f), new Vector3(1.2f, 0.01f, 0.3f), DecorPalette.Glow(new Color(1f, 0.86f, 0.60f)));
                m.Box(p + new Vector3(0f, 2.31f, 0f), new Vector3(1.34f, 0.03f, 0.08f), DecorPalette.Metal(new Color(0.78f, 0.62f, 0.30f)));
                foreach (float x in new[] { -0.5f, 0.5f })
                    m.Box(p + new Vector3(x, 2.76f, 0f), new Vector3(0.012f, 0.9f, 0.012f), st.Metal);
            }, p + new Vector3(0f, 2.2f, 0f));
            PointLight(root, "PoolLight", p + new Vector3(0f, 1.95f, 0f), new Color(1f, 0.86f, 0.62f), 1.6f, 3.6f);
        }

        private static void BuildJukebox(Transform root, Style st)
        {
            var j = new Vector3(12.45f, 0f, -2.4f);
            Prop(root, "Jukebox", j + new Vector3(0f, 0.8f, 0f), new Vector3(0.62f, 1.6f, 0.96f), m =>
            {
                Surf cabinet = DecorPalette.Gloss(new Color(0.30f, 0.14f, 0.08f));
                Surf chrome = DecorPalette.Metal(new Color(0.80f, 0.80f, 0.82f));
                m.Box(j + new Vector3(0f, 0.65f, 0f), new Vector3(0.6f, 1.3f, 0.94f), cabinet);
                m.CylinderX(j + new Vector3(0f, 1.3f, 0f), 0.47f, 0.6f, 16, cabinet);
                float x = j.x - 0.305f;
                // Bubble tubes up the sides, the glowing band, the record window and the grille.
                m.Box(new Vector3(x, 0.75f, j.z - 0.4f), new Vector3(0.02f, 1.1f, 0.07f), DecorPalette.Glow(new Color(1f, 0.62f, 0.15f)));
                m.Box(new Vector3(x, 0.75f, j.z + 0.4f), new Vector3(0.02f, 1.1f, 0.07f), DecorPalette.Glow(new Color(1f, 0.62f, 0.15f)));
                m.Box(new Vector3(x, 1.06f, j.z), new Vector3(0.02f, 0.06f, 0.74f), DecorPalette.Glow(new Color(1f, 0.25f, 0.20f)));
                m.Box(new Vector3(x, 1.32f, j.z), new Vector3(0.02f, 0.3f, 0.6f), DecorPalette.Glow(new Color(1f, 0.92f, 0.65f)));
                m.Box(new Vector3(x, 0.45f, j.z), new Vector3(0.02f, 0.42f, 0.62f), DecorPalette.Matte(new Color(0.12f, 0.08f, 0.06f)));
                for (int i = 0; i < 6; i++)
                    m.Box(new Vector3(x - 0.012f, 0.28f + i * 0.07f, j.z), new Vector3(0.012f, 0.018f, 0.64f), chrome);
                m.Box(new Vector3(x - 0.01f, 0.86f, j.z), new Vector3(0.03f, 0.12f, 0.8f), chrome);
            });
            PointLight(root, "JukeboxGlow", j + new Vector3(-0.7f, 1.1f, 0f), new Color(1f, 0.52f, 0.35f), 1.1f, 3.4f);
        }

        private static void BuildDarts(Transform root, Style st)
        {
            float z = -HalfZ + WallT * 0.5f;
            Quaternion facing = Quaternion.Euler(90f, 0f, 0f);
            var board = new Vector3(-5.6f, 1.73f, z);
            Deco(WallOf(root, "Wall_S1"), "Darts", m =>
            {
                m.Cylinder(board, 0.25f, 0.035f, 20, DecorPalette.Matte(new Color(0.08f, 0.07f, 0.06f)), facing);
                Color[] rings = { new Color(0.85f, 0.80f, 0.66f), new Color(0.70f, 0.14f, 0.10f), new Color(0.14f, 0.40f, 0.20f), new Color(0.85f, 0.80f, 0.66f), new Color(0.75f, 0.12f, 0.10f) };
                float[] radii = { 0.205f, 0.17f, 0.12f, 0.08f, 0.022f };
                for (int i = 0; i < rings.Length; i++)
                    m.Cylinder(board + new Vector3(0f, 0f, 0.036f + i * 0.002f), radii[i], 0.002f, 20, DecorPalette.Matte(rings[i]), facing);
                // The chalk scoreboard beside it.
                m.Box(new Vector3(-4.75f, 1.75f, z + 0.012f), new Vector3(0.6f, 0.45f, 0.024f), DecorPalette.Matte(new Color(0.10f, 0.16f, 0.12f)));
                for (int i = 0; i < 4; i++)
                    m.Box(new Vector3(-4.75f, 1.88f - i * 0.09f, z + 0.026f), new Vector3(0.36f - i * 0.06f, 0.012f, 0.004f), DecorPalette.Matte(new Color(0.85f, 0.86f, 0.84f)));
            }, board);
        }

        private static void BuildTv(Transform root)
        {
            var at = new Vector3(4.7f, 2.42f, HalfZ - 0.45f);
            Deco(WallOf(root, "Wall_N"), "Tv", m =>
            {
                Surf plastic = DecorPalette.Matte(new Color(0.12f, 0.12f, 0.13f));
                m.Box(at, new Vector3(0.62f, 0.48f, 0.46f), plastic);
                m.Upright(at + new Vector3(0f, 0.02f, -0.235f), Vector3.back, 0.5f, 0.36f, DecorPalette.Glow(new Color(0.46f, 0.60f, 0.78f)));
                m.Box(at + new Vector3(0f, -0.3f, 0.12f), new Vector3(0.5f, 0.04f, 0.36f), DecorPalette.Metal(new Color(0.30f, 0.30f, 0.32f)));
                m.Rod(at + new Vector3(0f, -0.32f, 0.2f), new Vector3(at.x, at.y - 0.32f, HalfZ - WallT * 0.5f), 0.03f, DecorPalette.Metal(new Color(0.30f, 0.30f, 0.32f)));
            }, at);
            PointLight(WallOf(root, "Wall_N"), "TvGlow", at + new Vector3(0f, -0.2f, -0.7f), new Color(0.6f, 0.75f, 1f), 0.6f, 2.6f);
        }

        // Neon on the walls, each with a little light of its own colour on the panelling.
        private static void BuildNeon(Transform root)
        {
            NeonOnWall(root, "Wall_N", "COLD BEER", new Color(0.35f, 0.65f, 1f), new Vector3(7.4f, 2.25f, HalfZ), Vector3.back, 0.34f);
            NeonOnWall(root, "Wall_N", "BOURBON", new Color(1f, 0.62f, 0.2f), new Vector3(-9.4f, 2.35f, HalfZ), Vector3.back, 0.36f);
            NeonOnWall(root, "Wall_E", "KENTUCKY", new Color(1f, 0.25f, 0.2f), new Vector3(HalfX, 2.72f, 4.3f), Vector3.left, 0.26f);
        }

        private static void NeonOnWall(Transform root, string wall, string text, Color color, Vector3 onWall, Vector3 normal, float height)
        {
            Surf neon = Neon(text, color, out float aspect);
            Vector3 face = onWall + normal * (WallT * 0.5f + 0.03f);
            Transform parent = WallOf(root, wall);
            Deco(parent, "Neon " + text, m =>
            {
                Vector3 side = Vector3.Cross(normal, Vector3.up);
                Vector3 size = new Vector3(Mathf.Abs(side.x) * (height * aspect + 0.1f) + Mathf.Abs(normal.x) * 0.03f, height + 0.1f,
                    Mathf.Abs(side.z) * (height * aspect + 0.1f) + Mathf.Abs(normal.z) * 0.03f);
                m.Box(face - normal * 0.012f, size, DecorPalette.Matte(new Color(0.05f, 0.05f, 0.06f)));
                m.Upright(face + normal * 0.004f, normal, height * aspect, height, neon, FullUv);
            }, face);
            PointLight(parent, "NeonGlow " + text, face + normal * 0.6f, color, 0.8f, 2.6f);
        }

        private static void BuildChristmas(Transform root)
        {
            Color[] colors = { new Color(1f, 0.15f, 0.1f), new Color(0.2f, 0.9f, 0.3f), new Color(0.25f, 0.45f, 1f), new Color(1f, 0.7f, 0.2f), new Color(1f, 0.95f, 0.85f) };
            Surf wire = DecorPalette.Matte(new Color(0.05f, 0.12f, 0.06f));
            float y = WallH - 0.2f, inset = WallT * 0.5f + 0.05f;
            int k = 0;
            // A string of bulbs along a wall, scalloped between hooks every 1.8 m.
            void Strand(string wall, Vector3 a, Vector3 b)
            {
                Deco(WallOf(root, wall), "ChristmasLights", m =>
                {
                    float len = Vector3.Distance(a, b);
                    int n = Mathf.Max(2, Mathf.FloorToInt(len / 0.45f));
                    Vector3 prev = a;
                    for (int i = 0; i <= n; i++)
                    {
                        float t = i / (float)n;
                        float along = t * len;
                        Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (0.09f * Mathf.Sin(Mathf.PI * Mathf.Repeat(along / 1.8f, 1f)));
                        if (i > 0) m.Rod(prev, p, 0.008f, wire);
                        m.Box(p + Vector3.down * 0.03f, new Vector3(0.045f, 0.06f, 0.045f), DecorPalette.Glow(colors[k++ % colors.Length]));
                        prev = p;
                    }
                }, (a + b) * 0.5f);
            }
            Strand("Wall_N", new Vector3(-HalfX + 0.3f, y, HalfZ - inset), new Vector3(HalfX - 0.3f, y, HalfZ - inset));
            Strand("Wall_E", new Vector3(HalfX - inset, y, HalfZ - 0.3f), new Vector3(HalfX - inset, y, -HalfZ + 0.3f));
            Strand("Wall_S2", new Vector3(HalfX - 0.3f, y, -HalfZ + inset), new Vector3(1.4f, y, -HalfZ + inset));
            Strand("Wall_S1", new Vector3(-1.4f, y, -HalfZ + inset), new Vector3(-HalfX + 0.3f, y, -HalfZ + inset));
            Strand("Wall_W", new Vector3(-HalfX + inset, y, -HalfZ + 0.3f), new Vector3(-HalfX + inset, y, HalfZ - 0.3f));

            // The tree in the corner by the window, lit, with a star on top.
            var t0 = new Vector3(11.9f, 0f, -8.0f);
            Prop(root, "ChristmasTree", t0 + new Vector3(0f, 1.1f, 0f), new Vector3(1.3f, 2.2f, 1.3f), m =>
            {
                m.Box(t0 + new Vector3(0f, 0.06f, 0f), new Vector3(0.5f, 0.12f, 0.5f), DecorPalette.Matte(new Color(0.60f, 0.10f, 0.08f)));
                m.Cylinder(t0 + new Vector3(0f, 0.12f, 0f), 0.07f, 0.3f, 8, DecorPalette.Matte(new Color(0.35f, 0.22f, 0.12f)));
                Surf green = DecorPalette.Matte(new Color(0.08f, 0.26f, 0.12f));
                float[] bases = { 0.35f, 0.95f, 1.5f }, radii = { 0.72f, 0.56f, 0.40f }, heights = { 0.85f, 0.75f, 0.7f }, tops = { 0.15f, 0.12f, 0.05f };
                for (int i = 0; i < 3; i++)
                    m.Cylinder(t0 + new Vector3(0f, bases[i], 0f), radii[i], heights[i], 12, green, Quaternion.identity, tops[i]);
                m.Sphere(t0 + new Vector3(0f, 2.24f, 0f), 0.08f, DecorPalette.Glow(new Color(1f, 0.88f, 0.35f)));
                var rng = new System.Random(1217);
                for (int i = 0; i < 30; i++)
                {
                    int tier = i % 3;
                    float h = (float)rng.NextDouble() * heights[tier] * 0.8f;
                    float r = radii[tier] * (1f - (1f - tops[tier]) * h / heights[tier]) + 0.01f;
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                    Vector3 at = t0 + new Vector3(Mathf.Cos(a) * r, bases[tier] + h, Mathf.Sin(a) * r);
                    m.Sphere(at, i % 2 == 0 ? 0.035f : 0.025f, DecorPalette.Glow(colors[i % colors.Length]));
                }
            });
            PointLight(root, "TreeGlow", t0 + new Vector3(-0.8f, 1.2f, 0.6f), new Color(1f, 0.75f, 0.55f), 0.7f, 2.8f);
        }

        private static void BuildAntlers(Transform root, Style st)
        {
            var at = new Vector3(HalfX - WallT * 0.5f - 0.03f, 2.45f, -5.6f);
            Deco(WallOf(root, "Wall_E"), "Antlers", m =>
            {
                Surf bone = DecorPalette.Matte(new Color(0.78f, 0.72f, 0.60f));
                m.Box(at, new Vector3(0.05f, 0.36f, 0.26f), st.Wood);
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 b = at + new Vector3(-0.05f, 0.08f, side * 0.06f);
                    Vector3 main = b + new Vector3(-0.12f, 0.22f, side * 0.28f);
                    m.Rod(b, main, 0.018f, bone);
                    m.Rod(main, main + new Vector3(-0.04f, 0.12f, side * 0.1f), 0.014f, bone);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 on = Vector3.Lerp(b, main, 0.35f + i * 0.25f);
                        m.Rod(on, on + new Vector3(-0.03f, 0.13f, side * 0.02f), 0.011f, bone);
                    }
                }
            }, at);
        }
        #endregion

        #region Windows
        // The windows: in every hub the night outside behind frosted glass; in the town, from the street, the warm light
        // of the room through them (a pane's two faces are separate decals on the wall's two sides).
        private static readonly (string Wall, Vector3 At, Vector3 Out)[] Windows =
        {
            ("Wall_S1", new Vector3(-10.6f, 1.6f, -HalfZ), Vector3.back),
            ("Wall_S2", new Vector3(6.0f, 1.6f, -HalfZ), Vector3.back),
            ("Wall_E", new Vector3(HalfX, 1.6f, 4.3f), Vector3.right),
            ("Wall_E", new Vector3(HalfX, 1.6f, -7.6f), Vector3.right),
            ("Wall_W", new Vector3(-HalfX, 1.6f, 7.3f), Vector3.left),
            ("Wall_W", new Vector3(-HalfX, 1.6f, -3.9f), Vector3.left),
        };

        private static void BuildWindows(Transform root, Style st, bool inTown)
        {
            const float W = 1.8f, H = 1.3f;
            Surf night = DecorPalette.Glass(new Color(0.06f, 0.09f, 0.16f));
            Surf frost = DecorPalette.Matte(new Color(0.80f, 0.86f, 0.92f));
            Surf warm = DecorPalette.Glow(new Color(0.86f, 0.62f, 0.36f));
            Surf shutter = DecorPalette.Matte(new Color(0.10f, 0.20f, 0.14f));
            foreach ((string wall, Vector3 at, Vector3 outward) in Windows)
            {
                Vector3 side = Vector3.Cross(outward, Vector3.up);   // along the wall
                Transform parent = WallOf(root, wall);
                Vector3 inner = at - outward * (WallT * 0.5f);
                Deco(parent, "Window", m =>
                {
                    Vector3 face = inner - outward * 0.006f;
                    m.Upright(face, -outward, W, H, night);
                    Frame(m, inner - outward * 0.02f, side, outward, W, H, st.Trim, 0.04f);
                    m.Box(inner - outward * 0.09f + Vector3.down * (H * 0.5f + 0.03f), Along(side, W + 0.2f, 0.05f, 0.18f, outward), st.Wood);
                    // Frost creeping up the bottom corners.
                    m.Box(face - outward * 0.004f + Vector3.down * (H * 0.5f - 0.06f) - side * (W * 0.5f - 0.18f), Along(side, 0.34f, 0.12f, 0.004f, outward), frost);
                    m.Box(face - outward * 0.004f + Vector3.down * (H * 0.5f - 0.05f) + side * (W * 0.5f - 0.14f), Along(side, 0.26f, 0.1f, 0.004f, outward), frost);
                }, inner);
                if (!inTown) continue;
                Vector3 outer = at + outward * (WallT * 0.5f + 0.05f);
                Deco(parent, "WindowOutside", m =>
                {
                    m.Upright(outer + outward * 0.006f, outward, W, H, warm);
                    Frame(m, outer + outward * 0.02f, side, outward, W, H, st.SidingTrim, 0.045f);
                    m.Box(outer + outward * 0.08f + Vector3.down * (H * 0.5f + 0.04f), Along(side, W + 0.24f, 0.06f, 0.16f, outward), st.SidingTrim);
                    m.Box(outer + outward * 0.08f + Vector3.down * (H * 0.5f - 0.02f), Along(side, W + 0.16f, 0.07f, 0.14f, outward), DecorMaterials.Snow);
                    foreach (float s in new[] { -1f, 1f })
                        m.Box(outer + outward * 0.03f + side * s * (W * 0.5f + 0.32f), Along(side, 0.5f, H + 0.1f, 0.04f, outward), shutter);
                }, outer);
            }
        }

        // A box sized along the wall (<paramref name="along"/>), up and through it (<paramref name="through"/>).
        private static Vector3 Along(Vector3 side, float along, float up, float through, Vector3 outward)
            => new Vector3(Mathf.Abs(side.x) * along + Mathf.Abs(outward.x) * through, up, Mathf.Abs(side.z) * along + Mathf.Abs(outward.z) * through);

        // A window's frame and its cross bars, on a face of the wall.
        private static void Frame(RaidMesh m, Vector3 c, Vector3 side, Vector3 outward, float w, float h, in Surf s, float t)
        {
            m.Box(c + Vector3.up * (h * 0.5f), Along(side, w + t * 2f, t, 0.03f, outward), s);
            m.Box(c + Vector3.down * (h * 0.5f), Along(side, w + t * 2f, t, 0.03f, outward), s);
            m.Box(c + side * (w * 0.5f), Along(side, t, h, 0.03f, outward), s);
            m.Box(c - side * (w * 0.5f), Along(side, t, h, 0.03f, outward), s);
            m.Box(c, Along(side, w, t * 0.6f, 0.025f, outward), s);
            m.Box(c, Along(side, t * 0.6f, h, 0.025f, outward), s);
        }
        #endregion

        #region Signs over the stations
        // A board over every station saying what it is, hung from the ceiling and turned to face the camera, so the room
        // reads at a glance from any view, however the building stands.
        private static void BuildSigns(Transform root, Transform overhead, bool inTown)
        {
            HangSign(root, "MOTT - BAR & TRADE", new Vector3(-2f, 2.5f, 6.2f));
            HangSign(root, "VASKO - FENCE", new Vector3(-10.8f, 2.45f, 6.2f));
            HangSign(root, "INSTITUTE", new Vector3(10.4f, 2.45f, 6.4f));
            HangSign(root, "GUNSMITH & REPAIRS", new Vector3(11.4f, 2.45f, 1.0f));
            HangSign(root, "MEDIC", new Vector3(10.9f, 2.45f, -5.5f));
            HangSign(root, "GUIDE - THE ROADS", new Vector3(4.6f, 2.45f, -6.6f));
            HangSign(root, "HIRED GUNS", new Vector3(-6.5f, 2.45f, -6.0f));
            HangSign(root, "NOTICES & JOBS", new Vector3(-11.5f, 2.5f, -1.5f));
            HangSign(root, "FLEA MARKET", new Vector3(-11.3f, 2.45f, -6.0f));
            HangSign(root, "YOUR STASH", new Vector3(-11.5f, 2.5f, 2.5f));
            HangSign(root, "HIDEOUT", new Vector3(-11.6f, 2.5f, 5.0f));
            HangSign(root, "LANDLORD", new Vector3(7.5f, 2.45f, -7.5f));
            HangSign(root, inTown ? "FRONT DOOR - BARDSTOWN" : "INTO THE ZONE", new Vector3(0f, 2.55f, -7.7f), true);
            if (inTown)
            {
                // The exit sign over the door, as the fire code wants.
                Surf exit = Neon("EXIT", new Color(1f, 0.2f, 0.15f), out float aspect);
                var at = new Vector3(0f, 2.62f, -HalfZ + WallT * 0.5f + 0.06f);
                Deco(WallOf(root, "Lintel"), "ExitSign", m =>
                {
                    m.Box(at - Vector3.forward * 0.02f, new Vector3(0.18f * aspect + 0.08f, 0.26f, 0.06f), DecorPalette.Matte(new Color(0.85f, 0.85f, 0.82f)));
                    m.Upright(at + Vector3.forward * 0.012f, Vector3.forward, 0.18f * aspect, 0.18f, exit, FullUv);
                }, at);
            }
        }

        private static void HangSign(Transform root, string text, Vector3 at, bool accent = false)
        {
            const float H = 0.3f;
            // Painted boards, but self-lit: the room is dim and the board is there to be read at a glance.
            Surf face = SelfLit(text, accent ? new Color(0.32f, 0.08f, 0.05f) : new Color(0.13f, 0.08f, 0.05f),
                new Color(0.95f, 0.85f, 0.62f), out float aspect);
            float w = H * aspect;
            GameObject go = Deco(root, "Sign " + text, m =>
            {
                m.Box(at + new Vector3(0f, 0f, 0.012f), new Vector3(w + 0.1f, H + 0.1f, 0.035f), DecorPalette.Matte(new Color(0.20f, 0.12f, 0.07f)));
                m.Upright(at - new Vector3(0f, 0f, 0.008f), Vector3.back, w, H, face, FullUv);
                Surf chain = DecorPalette.Metal(new Color(0.40f, 0.38f, 0.35f));
                foreach (float x in new[] { -w * 0.4f, w * 0.4f })
                    m.Box(at + new Vector3(x, (WallH - at.y) * 0.5f + H * 0.25f, 0.012f), new Vector3(0.012f, WallH - at.y - H * 0.5f, 0.012f), chain);
            }, at);
            if (go != null) go.AddComponent<FaceCameraYaw>();
        }
        #endregion
    }

    /// <summary>Turns a hanging sign about the vertical to face the camera, so it reads in every view and however its
    /// room is turned.</summary>
    public sealed class FaceCameraYaw : MonoBehaviour
    {
        private Camera _cam;

        private void LateUpdate()
        {
            if (_cam == null || !_cam.isActiveAndEnabled)
            {
                _cam = Camera.main;
                if (_cam == null) return;
            }
            Vector3 f = _cam.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
        }
    }
}
