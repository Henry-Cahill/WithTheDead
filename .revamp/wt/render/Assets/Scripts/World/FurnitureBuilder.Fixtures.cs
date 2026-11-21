using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        #region Doors, wall fittings and ceiling lights
        /// <summary>A wooden panel door leaf hinged at the local origin and extending along +X
        /// (<paramref name="width"/> × <paramref name="height"/>, 4.5 cm thick), with raised panels on
        /// both faces and a brass knob. <paramref name="glass"/> gives a shop door with a dark pane.</summary>
        public static void DoorLeaf(RaidMesh m, System.Random rng, float width, float height, Color wood, bool glass)
        {
            Color c = V(wood, rng, 0.06f);
            const float t = 0.045f;
            if (glass)
            {
                Color frame = new Color(0.62f, 0.63f, 0.62f);
                m.Box(new Vector3(0.04f, height * 0.5f, 0f), new Vector3(0.08f, height, t), Mt(frame));
                m.Box(new Vector3(width - 0.04f, height * 0.5f, 0f), new Vector3(0.08f, height, t), Mt(frame));
                m.Box(new Vector3(width * 0.5f, height - 0.05f, 0f), new Vector3(width, 0.1f, t), Mt(frame));
                m.Box(new Vector3(width * 0.5f, 0.12f, 0f), new Vector3(width, 0.24f, t), Mt(frame));
                m.Box(new Vector3(width * 0.5f, height * 0.5f + 0.07f, 0f), new Vector3(width - 0.16f, height - 0.34f, 0.012f), Gs(new Color(0.14f, 0.17f, 0.18f)));
                m.Box(new Vector3(width - 0.14f, 1.0f, 0.04f), new Vector3(0.03f, 0.35f, 0.03f), Mt(Chrome));
                return;
            }
            m.Box(new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(width, height, t), M(c));
            for (int face = -1; face <= 1; face += 2)
            {
                float z = face * (t * 0.5f + 0.004f);
                for (int r = 0; r < 2; r++)
                    for (int col = 0; col < 2; col++)
                    {
                        float pw = (width - 0.3f) * 0.5f;
                        float ph = r == 0 ? height * 0.36f : height * 0.4f;
                        float py = r == 0 ? 0.15f + ph * 0.5f : height - 0.15f - ph * 0.5f;
                        float px = 0.12f + pw * 0.5f + col * (pw + 0.06f);
                        m.Box(new Vector3(px, py, z), new Vector3(pw, ph, 0.008f), M(c * 0.88f));
                    }
                m.Ellipsoid(new Vector3(width - 0.08f, 0.98f, face * (t * 0.5f + 0.035f)), new Vector3(0.055f, 0.055f, 0.05f), Mt(Brass), 6, 4);
            }
        }

        /// <summary>A framed poster on a wall: <paramref name="at"/> is its centre on the wall face,
        /// <paramref name="normal"/> points out of the wall.</summary>
        public static void FramedPoster(RaidMesh m, System.Random rng, Vector3 at, Vector3 normal, float w, float h, int kind)
        {
            float roll = R(rng, -4f, 4f);
            Color frame = Chance(rng, 0.5) ? new Color(0.16f, 0.12f, 0.08f) : new Color(0.55f, 0.55f, 0.52f);
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(0f, 0f, roll);
            m.Box(at + normal * 0.008f, new Vector3(w + 0.05f, h + 0.05f, 0.016f), q, M(frame));
            m.Upright(at + normal * 0.0175f, normal, w, h, DecorMaterials.Poster(kind), new Rect(0f, 0f, 1f, 1f), roll);
        }

        /// <summary>A round wall clock (stopped).</summary>
        public static void WallClock(RaidMesh m, System.Random rng, Vector3 at, Vector3 normal)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Cylinder(at, 0.16f, 0.04f, 14, M(new Color(0.15f, 0.15f, 0.16f)), q * Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(at + normal * 0.041f, 0.14f, 0.002f, 14, M(new Color(0.88f, 0.87f, 0.82f)), q * Quaternion.Euler(90f, 0f, 0f));
            float a1 = R(rng, 0f, 360f), a2 = R(rng, 0f, 360f);
            m.Box(at + normal * 0.046f + q * (Quaternion.Euler(0f, 0f, a1) * new Vector3(0f, 0.045f, 0f)), new Vector3(0.012f, 0.09f, 0.004f), q * Quaternion.Euler(0f, 0f, a1), M(Black));
            m.Box(at + normal * 0.047f + q * (Quaternion.Euler(0f, 0f, a2) * new Vector3(0f, 0.06f, 0f)), new Vector3(0.008f, 0.12f, 0.004f), q * Quaternion.Euler(0f, 0f, a2), M(Black));
        }

        /// <summary>A cast-iron radiator under a window (length along the wall).</summary>
        public static void Radiator(RaidMesh m, System.Random rng, Vector3 at, Vector3 normal, float len)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Color c = V(new Color(0.62f, 0.60f, 0.55f), rng, 0.06f);
            int fins = Mathf.Max(4, Mathf.RoundToInt(len / 0.07f));
            for (int i = 0; i < fins; i++)
            {
                float x = -len * 0.5f + (i + 0.5f) * len / fins;
                m.Box(at + q * new Vector3(x, 0.38f, 0.1f), new Vector3(0.045f, 0.6f, 0.14f), q, M(c));
            }
            m.Box(at + q * new Vector3(0f, 0.12f, 0.1f), new Vector3(len, 0.04f, 0.08f), q, M(c * 0.9f));
            m.Box(at + q * new Vector3(0f, 0.66f, 0.1f), new Vector3(len, 0.03f, 0.06f), q, M(c * 0.9f));
        }

        /// <summary>A fluorescent ceiling panel (0.6 × 1.2) hung just under the ceiling at
        /// <paramref name="at"/> (the ceiling's underside); lit panels glow.</summary>
        public static void CeilingPanel(RaidMesh m, Vector3 at, float yaw, bool lit)
        {
            Quaternion q = Yaw(yaw);
            m.Box(at + new Vector3(0f, -0.03f, 0f), new Vector3(0.62f, 0.06f, 1.22f), q, M(new Color(0.70f, 0.70f, 0.68f)));
            m.Box(at + new Vector3(0f, -0.062f, 0f), new Vector3(0.52f, 0.004f, 1.1f), q, lit ? Gl(new Color(1f, 0.93f, 0.78f)) : M(new Color(0.55f, 0.56f, 0.55f)));
        }

        /// <summary>A frosted dome light flush with the ceiling (houses).</summary>
        public static void CeilingDome(RaidMesh m, Vector3 at, bool lit)
        {
            m.Cylinder(at + new Vector3(0f, -0.02f, 0f), 0.2f, 0.02f, 12, M(new Color(0.75f, 0.74f, 0.7f)));
            m.Ellipsoid(at + new Vector3(0f, -0.06f, 0f), new Vector3(0.34f, 0.12f, 0.34f), lit ? Gl(new Color(1f, 0.88f, 0.66f)) : G(new Color(0.62f, 0.62f, 0.58f)), 10, 4);
        }

        /// <summary>A bare bulb hanging on its flex (garages, storerooms).</summary>
        public static void HangingBulb(RaidMesh m, Vector3 ceiling, float drop, bool lit)
        {
            m.Rod(ceiling, ceiling - Vector3.up * drop, 0.01f, M(Black));
            m.Cylinder(ceiling - Vector3.up * (drop + 0.04f), 0.025f, 0.05f, 6, M(new Color(0.2f, 0.2f, 0.2f)));
            m.Ellipsoid(ceiling - Vector3.up * (drop + 0.08f), new Vector3(0.07f, 0.09f, 0.07f), lit ? Gl(new Color(1f, 0.82f, 0.52f)) : Gs(new Color(0.5f, 0.5f, 0.46f)), 6, 4);
        }

        /// <summary>A green enamel shop lamp shade hanging from the ceiling (warehouse / tent).</summary>
        public static void ShopLamp(RaidMesh m, Vector3 ceiling, float drop, bool lit)
        {
            Vector3 head = ceiling - Vector3.up * drop;
            m.Rod(ceiling, head, 0.012f, M(Black));
            m.Cylinder(head - Vector3.up * 0.16f, 0.24f, 0.16f, 12, G(new Color(0.16f, 0.30f, 0.20f)), Quaternion.identity, 0.25f, false);
            m.Ellipsoid(head - Vector3.up * 0.13f, new Vector3(0.09f, 0.09f, 0.09f), lit ? Gl(new Color(1f, 0.85f, 0.58f)) : Gs(new Color(0.5f, 0.5f, 0.46f)), 6, 4);
        }

        // The lit look of each ceiling fitting, drawn apart as a glow shell just proud of the fitting drawn unlit at the
        // same spot (same tessellation, a few mm larger), so a working light can show it only while it's on.

        /// <summary>The lit diffuser of a <see cref="CeilingPanel"/> at <paramref name="at"/>.</summary>
        public static void CeilingPanelGlow(RaidMesh m, Vector3 at, float yaw)
            => m.Box(at + new Vector3(0f, -0.0645f, 0f), new Vector3(0.53f, 0.007f, 1.11f), Yaw(yaw), Gl(new Color(1f, 0.93f, 0.78f)));

        /// <summary>The lit dome of a <see cref="CeilingDome"/> at <paramref name="at"/>.</summary>
        public static void CeilingDomeGlow(RaidMesh m, Vector3 at)
            => m.Ellipsoid(at + new Vector3(0f, -0.06f, 0f), new Vector3(0.352f, 0.132f, 0.352f), Gl(new Color(1f, 0.88f, 0.66f)), 10, 4);

        /// <summary>The lit bulb of a <see cref="HangingBulb"/> hung <paramref name="drop"/> below <paramref name="ceiling"/>.</summary>
        public static void HangingBulbGlow(RaidMesh m, Vector3 ceiling, float drop)
            => m.Ellipsoid(ceiling - Vector3.up * (drop + 0.08f), new Vector3(0.078f, 0.098f, 0.078f), Gl(new Color(1f, 0.82f, 0.52f)), 6, 4);

        /// <summary>The lit bulb of a <see cref="ShopLamp"/> hung <paramref name="drop"/> below <paramref name="ceiling"/>.</summary>
        public static void ShopLampGlow(RaidMesh m, Vector3 ceiling, float drop)
            => m.Ellipsoid(ceiling - Vector3.up * (drop + 0.13f), new Vector3(0.098f, 0.098f, 0.098f), Gl(new Color(1f, 0.85f, 0.58f)), 6, 4);
        #endregion

        #region Overgrowth and grime
        private static readonly Color[] s_Leaf =
        {
            new Color(0.10f, 0.20f, 0.20f), new Color(0.16f, 0.24f, 0.26f), new Color(0.20f, 0.27f, 0.31f),
            new Color(0.09f, 0.16f, 0.13f), new Color(0.13f, 0.22f, 0.18f), new Color(0.24f, 0.30f, 0.33f),
        };

        /// <summary>A creeping vine cluster on a wall face: wandering stems carrying many small leaves
        /// in dark teal / blue-grey, rooted at the floor (or hanging from the top). <paramref name="at"/>
        /// is the root on the wall face; <paramref name="normal"/> points out of the wall.</summary>
        public static void Vines(RaidMesh m, System.Random rng, Vector3 at, Vector3 normal, float width, float height, bool hanging)
        {
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            Color stem = new Color(0.12f, 0.12f, 0.10f);
            int stems = Mathf.Max(2, Mathf.RoundToInt(width * R(rng, 2.5f, 4f)));
            for (int s = 0; s < stems; s++)
            {
                Vector3 p = at + right * R(rng, -width * 0.5f, width * 0.5f);
                float dir = hanging ? -1f : 1f;
                float len = height * R(rng, 0.45f, 1f);
                float step = 0.14f;
                int segs = Mathf.Max(2, Mathf.RoundToInt(len / step));
                float drift = R(rng, -0.6f, 0.6f);
                for (int i = 0; i < segs; i++)
                {
                    drift = Mathf.Clamp(drift + R(rng, -0.35f, 0.35f), -0.9f, 0.9f);
                    Vector3 q = p + (Vector3.up * dir + right * drift * 0.5f).normalized * step;
                    float off = 0.012f + 0.01f * (float)rng.NextDouble();
                    // A flat stem strip lying on the wall (4 vertices instead of a box's 24).
                    Vector3 half = (q - p) * 0.5f;
                    Vector3 side = Vector3.Cross(normal, half).normalized * 0.008f;
                    m.Quad((p + q) * 0.5f + normal * off, side, half, M(stem));
                    int leaves = rng.Next(1, 4);
                    for (int l = 0; l < leaves; l++)
                    {
                        float size = R(rng, 0.06f, 0.13f);
                        float ang = R(rng, 0f, Mathf.PI * 2f);
                        Vector3 u = (right * Mathf.Cos(ang) + Vector3.up * Mathf.Sin(ang)) * size;
                        Vector3 v = (right * -Mathf.Sin(ang) + Vector3.up * Mathf.Cos(ang)) * size * 0.55f;
                        Vector3 c = Vector3.Lerp(p, q, (float)rng.NextDouble()) + right * R(rng, -0.1f, 0.1f) + Vector3.up * R(rng, -0.05f, 0.05f);
                        c += normal * (0.02f + 0.02f * (float)rng.NextDouble());
                        m.Diamond(c, u, v + normal * R(rng, 0f, 0.03f), M(s_Leaf[rng.Next(s_Leaf.Length)]));
                    }
                    p = q;
                }
            }
        }

        /// <summary>A grimy patch on a wall: soot, a damp stain or a hole knocked through the plaster.</summary>
        public static void WallStain(RaidMesh m, System.Random rng, Vector3 at, Vector3 normal, Color wall)
        {
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            int k = rng.Next(3);
            if (k == 0)
            {
                Color soot = new Color(wall.r * 0.45f, wall.g * 0.43f, wall.b * 0.4f);
                m.Blob(at + normal * 0.006f, right, Vector3.up, R(rng, 0.25f, 0.6f), R(rng, 0.3f, 0.7f), 11, 0.35f, rng, M(soot));
            }
            else if (k == 1)
            {
                Color damp = new Color(wall.r * 0.7f, wall.g * 0.62f, wall.b * 0.48f);
                m.Blob(at + normal * 0.006f, right, Vector3.up, R(rng, 0.3f, 0.5f), R(rng, 0.2f, 0.45f), 12, 0.3f, rng, M(damp));
                m.Blob(at + normal * 0.007f, right, Vector3.up, R(rng, 0.12f, 0.25f), R(rng, 0.1f, 0.2f), 9, 0.3f, rng, M(damp * 0.85f));
            }
            else
            {
                m.Blob(at + normal * 0.006f, right, Vector3.up, R(rng, 0.2f, 0.35f), R(rng, 0.15f, 0.3f), 10, 0.45f, rng, M(new Color(0.78f, 0.76f, 0.70f)));
                m.Blob(at + normal * 0.007f, right, Vector3.up, R(rng, 0.08f, 0.16f), R(rng, 0.07f, 0.14f), 8, 0.4f, rng, M(new Color(0.06f, 0.05f, 0.05f)));
                for (int i = 0; i < 3; i++)
                    m.Box(at + normal * 0.009f + Vector3.up * R(rng, -0.08f, 0.08f), new Vector3(0.24f, 0.02f, 0.006f), Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, R(rng, -8f, 8f)), M(new Color(0.42f, 0.30f, 0.18f)));
            }
        }
        #endregion
    }
}
