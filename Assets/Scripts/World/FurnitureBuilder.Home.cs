using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        #region Kitchens and dining
        /// <summary>A run of wooden base cabinets with a laminate top (0.62 deep, 0.92 high), optionally
        /// with a stainless sink.</summary>
        public static void KitchenCounter(RaidMesh m, System.Random rng, float len, Color cabinet, Color top, bool sink)
        {
            Color c = V(cabinet, rng, 0.05f);
            m.Box(new Vector3(0f, 0.05f, 0.0f), new Vector3(len - 0.02f, 0.1f, 0.52f), M(new Color(0.1f, 0.09f, 0.08f)));
            m.Box(new Vector3(0f, 0.5f, -0.02f), new Vector3(len, 0.8f, 0.58f), M(c));
            m.Box(new Vector3(0f, 0.92f, 0f), new Vector3(len + 0.02f, 0.04f, 0.63f), G(V(top, rng, 0.04f)));
            m.Box(new Vector3(0f, 1.0f, -0.3f), new Vector3(len + 0.02f, 0.12f, 0.02f), G(top * 0.9f));
            int doors = Mathf.Max(1, Mathf.RoundToInt(len / 0.45f));
            float dw = len / doors;
            for (int i = 0; i < doors; i++)
            {
                float x = -len * 0.5f + dw * (i + 0.5f);
                m.Box(new Vector3(x, 0.5f, 0.275f), new Vector3(dw - 0.04f, 0.62f, 0.02f), M(c * 1.08f));
                m.Box(new Vector3(x, 0.84f, 0.275f), new Vector3(dw - 0.04f, 0.1f, 0.02f), M(c * 1.08f));
                m.Box(new Vector3(x + dw * 0.32f * (i % 2 == 0 ? 1f : -1f), 0.7f, 0.29f), new Vector3(0.02f, 0.08f, 0.02f), Mt(Brass));
            }
            if (sink)
            {
                float sx = len > 1.6f ? len * 0.2f : 0f;
                m.Box(new Vector3(sx, 0.942f, 0.02f), new Vector3(0.6f, 0.004f, 0.44f), Mt(Steel));
                m.Box(new Vector3(sx - 0.14f, 0.945f, 0.03f), new Vector3(0.26f, 0.004f, 0.34f), G(new Color(0.22f, 0.23f, 0.24f)));
                m.Box(new Vector3(sx + 0.14f, 0.945f, 0.03f), new Vector3(0.26f, 0.004f, 0.34f), G(new Color(0.22f, 0.23f, 0.24f)));
                m.Cylinder(new Vector3(sx, 0.94f, -0.25f), 0.016f, 0.22f, 6, Mt(Chrome));
                m.Box(new Vector3(sx, 0.97f, -0.25f), new Vector3(0.02f, 0.01f, 0.02f), Yaw(R(rng, -15f, 15f)), Mt(Brass));
                m.Box(new Vector3(sx, 1.16f, -0.17f), new Vector3(0.025f, 0.025f, 0.18f), Mt(Chrome));
            }
            // Clutter: a kettle, jars, a dish rack, a stack of plates.
            int items = rng.Next(1, 4);
            for (int i = 0; i < items; i++)
            {
                float x = R(rng, -len * 0.45f, len * 0.45f);
                if (sink && Mathf.Abs(x - (len > 1.6f ? len * 0.2f : 0f)) < 0.4f) continue;
                int k = rng.Next(4);
                if (k == 0) m.Cylinder(new Vector3(x, 0.94f, -0.1f), 0.09f, 0.2f, 8, G(V(new Color(0.7f, 0.2f, 0.15f), rng)));
                else if (k == 1) m.Cylinder(new Vector3(x, 0.94f, -0.18f), 0.05f, 0.16f, 6, Gs(new Color(0.55f, 0.6f, 0.55f)));
                else if (k == 2) m.Cylinder(new Vector3(x, 0.94f, 0.05f), 0.12f, 0.05f, 10, G(new Color(0.85f, 0.85f, 0.82f)));
                else CardboardBox(m, rng, new Vector3(x, 0.94f, -0.05f), new Vector3(0.24f, 0.3f, 0.18f), R(rng, -15f, 15f));
            }
        }

        /// <summary>Wall-hung kitchen cupboards (drawn into a wall's mesh so they vanish with it). The frame
        /// origin is on the wall face with +Z pointing into the room.</summary>
        public static void UpperCabinets(RaidMesh m, System.Random rng, float len, Color cabinet)
        {
            Color c = V(cabinet, rng, 0.05f);
            m.Box(new Vector3(0f, 1.82f, 0.17f), new Vector3(len, 0.7f, 0.34f), M(c));
            int doors = Mathf.Max(1, Mathf.RoundToInt(len / 0.45f));
            float dw = len / doors;
            for (int i = 0; i < doors; i++)
            {
                float x = -len * 0.5f + dw * (i + 0.5f);
                m.Box(new Vector3(x, 1.82f, 0.345f), new Vector3(dw - 0.04f, 0.64f, 0.02f), M(c * 1.08f));
                m.Box(new Vector3(x + dw * 0.32f * (i % 2 == 0 ? 1f : -1f), 1.58f, 0.36f), new Vector3(0.02f, 0.08f, 0.02f), Mt(Brass));
                // Tiny brass hinges on each side of the upper‑cabinet door
                m.Box(new Vector3(x - dw * 0.5f + 0.02f, 1.78f, 0.345f), new Vector3(0.02f, 0.04f, 0.02f), Mt(Brass));
                m.Box(new Vector3(x + dw * 0.5f - 0.02f, 1.78f, 0.345f), new Vector3(0.02f, 0.04f, 0.02f), Mt(Brass));
            }
        }

        /// <summary>A white enamel range: four burners, a back panel of knobs and an oven door.</summary>
        public static void Stove(RaidMesh m, System.Random rng, Color enamel)
        {
            Color c = V(enamel, rng, 0.04f);
            m.Box(new Vector3(0f, 0.46f, -0.02f), new Vector3(0.76f, 0.92f, 0.62f), G(c));
            m.Box(new Vector3(0f, 0.925f, 0f), new Vector3(0.74f, 0.01f, 0.6f), M(new Color(0.18f, 0.18f, 0.19f)));
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.18f : 0.18f, z = i < 2 ? -0.13f : 0.13f;
                m.Cylinder(new Vector3(x, 0.93f, z), 0.1f, 0.012f, 10, M(Black));
                m.Cylinder(new Vector3(x, 0.94f, z), 0.06f, 0.01f, 8, M(new Color(0.3f, 0.3f, 0.3f)));
                // Tiny brass control knob on each burner
                m.Cylinder(new Vector3(x, 0.945f, z), 0.015f, 0.01f, 6, Mt(Brass), Yaw(R(rng, -10f, 10f)));
            }
            m.Box(new Vector3(0f, 1.0f, -0.3f), new Vector3(0.76f, 0.16f, 0.06f), G(c));
            for (int i = 0; i < 4; i++)
                m.Cylinder(new Vector3(-0.27f + i * 0.18f, 1.0f, -0.27f), 0.022f, 0.025f, 6, M(Black), Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(0f, 0.5f, 0.3f), new Vector3(0.66f, 0.52f, 0.02f), G(c * 0.97f));
            m.Box(new Vector3(0f, 0.52f, 0.312f), new Vector3(0.42f, 0.22f, 0.004f), Gs(new Color(0.08f, 0.08f, 0.08f)));
            m.Box(new Vector3(0f, 0.8f, 0.33f), new Vector3(0.52f, 0.025f, 0.025f), Mt(Chrome));
            m.Box(new Vector3(0f, 0.12f, 0.3f), new Vector3(0.66f, 0.14f, 0.02f), G(c * 0.95f));
            if (Chance(rng, 0.5))
            {
                m.Cylinder(new Vector3(0.18f, 0.94f, 0.13f), 0.11f, 0.13f, 10, Mt(new Color(0.45f, 0.45f, 0.46f)));
                m.Box(new Vector3(0.36f, 1.04f, 0.13f), new Vector3(0.16f, 0.02f, 0.03f), M(Black));
            }
        }

        /// <summary>A rectangular table on four legs (top at 0.75) with a few things left on it.</summary>
        public static void Table(RaidMesh m, System.Random rng, float w, float d, Color wood)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(0f, 0.735f, 0f), new Vector3(w, 0.04f, d), G(c));
            // thin wooden runner (front‑edge trim)
            m.Box(new Vector3(0f, 0.735f + 0.021f, d * 0.45f), new Vector3(w - 0.04f, 0.005f, 0.02f), M(c * 0.85f));
            m.Box(new Vector3(0f, 0.68f, 0f), new Vector3(w - 0.12f, 0.08f, d - 0.12f), M(c * 0.85f));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3((i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.06f), 0.36f, (i < 2 ? -1f : 1f) * (d * 0.5f - 0.06f)), new Vector3(0.05f, 0.72f, 0.05f), M(c * 0.8f));
            // brass candle holder base
            Vector3 candPos = new Vector3(-w * 0.2f + R(rng, -0.02f, 0.02f), 0.735f + 0.025f, d * 0.2f + R(rng, -0.02f, 0.02f));
            m.Cylinder(candPos, 0.02f, 0.05f, 6, Mt(Brass));
            // tiny emissive flame (visible only when lit)
            m.Box(candPos + new Vector3(0f, 0.05f, 0f), new Vector3(0.01f, 0.015f, 0.01f), Gl(new Color(1f, 0.94f, 0.65f)));
            // glass vase (simple translucent look using a light colour)
            Vector3 vasePos = new Vector3(w * 0.15f + R(rng, -0.02f, 0.02f), 0.735f + 0.025f, -d * 0.1f + R(rng, -0.02f, 0.02f));
            m.Cylinder(vasePos, 0.03f, 0.12f, 8, M(new Color(0.93f, 0.96f, 0.98f)));
            // plate inside vase (flat disc)
            m.Box(vasePos + new Vector3(0f, 0.12f, 0f), new Vector3(0.12f, 0.002f, 0.12f), G(new Color(0.92f, 0.92f, 0.90f)));
            // cloth napkin (simple red‑brown pattern)
            Vector3 napPos = new Vector3(-w * 0.2f + R(rng, -0.02f, 0.02f), 0.735f + 0.027f, d * 0.15f + R(rng, -0.02f, 0.02f));
            m.Box(napPos, new Vector3(0.15f, 0.001f, 0.08f), G(new Color(0.78f, 0.22f, 0.22f)));
            // occasional pair of metal shakers (left = salt, right = pepper)
            if (Chance(rng, 0.3f))
            {
                Vector3 basePos = new Vector3(R(rng, -w * 0.3f, w * 0.3f), 0.745f, R(rng, -d * 0.3f, d * 0.3f));
                // salt shaker
                m.Cylinder(basePos, 0.01f, 0.03f, 6, Mt(Brass));
                // pepper shaker (slightly offset)
                m.Cylinder(basePos + new Vector3(0.02f, 0f, 0f), 0.01f, 0.03f, 6, Mt(Brass));
            }
            int items = rng.Next(0, 4);
            for (int i = 0; i < items; i++)
            {
                var p = new Vector3(R(rng, -w * 0.4f, w * 0.4f), 0.755f, R(rng, -d * 0.35f, d * 0.35f));
                int k = rng.Next(4);
                if (k == 0) m.Cylinder(p, 0.04f, 0.1f, 8, G(new Color(0.85f, 0.84f, 0.8f)));
                else if (k == 1) m.FlatUV(p + Vector3.up * 0.002f, 0.21f, 0.28f, R(rng, 0f, 360f), DecorMaterials.Paper(rng.Next(3)), new Rect(0f, 0f, 1f, 1f));
                else if (k == 2) m.Cylinder(p, 0.12f, 0.02f, 10, G(new Color(0.9f, 0.88f, 0.82f)));
                else m.Cylinder(p, 0.033f, 0.12f, 8, Mt(new Color(0.6f, 0.55f, 0.45f)));
            }
        }
        #endregion

        #region Living rooms and bedrooms
        /// <summary>A fabric sofa (width along X, 0.88 deep) with seat and back cushions and rolled arms.</summary>
        public static void Sofa(RaidMesh m, System.Random rng, float w, Color fabric)
        {
            Color c = V(fabric, rng, 0.06f);
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3((i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.08f), 0.03f, (i < 2 ? -1f : 1f) * 0.34f), new Vector3(0.05f, 0.06f, 0.05f), M(Walnut));
            m.Box(new Vector3(0f, 0.25f, -0.02f), new Vector3(w, 0.38f, 0.82f), M(c * 0.9f));
            m.Box(new Vector3(0f, 0.62f, -0.32f), new Vector3(w - 0.36f, 0.5f, 0.2f), Quaternion.Euler(-10f, 0f, 0f), M(c));
            int n = w > 1.6f ? 3 : (w > 1.1f ? 2 : 1);
            float cw = (w - 0.4f) / n;
            for (int i = 0; i < n; i++)
            {
                float x = -w * 0.5f + 0.2f + cw * (i + 0.5f);
                m.Box(new Vector3(x, 0.5f, 0.07f), new Vector3(cw - 0.02f, 0.14f, 0.6f), Yaw(R(rng, -2f, 2f)), M(V(c, rng, 0.04f)));
            }
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * (w * 0.5f - 0.1f), 0.42f, -0.02f), new Vector3(0.2f, 0.5f, 0.82f), M(c * 0.95f));
                m.CylinderX(new Vector3(s * (w * 0.5f - 0.1f), 0.66f, -0.02f), 0.11f, 0.82f, 8, M(c * 0.95f), 90f);
            }
            if (Chance(rng, 0.6))
                m.Box(new Vector3(R(rng, -w * 0.3f, w * 0.3f), 0.68f, -0.08f), new Vector3(0.4f, 0.36f, 0.12f), Quaternion.Euler(-20f, R(rng, -20f, 20f), R(rng, -10f, 10f)), M(V(new Color(0.6f, 0.5f, 0.3f), rng, 0.2f)));
        }

        /// <summary>A 1993 tube TV with rabbit ears on a low wooden stand with a VCR.</summary>
        public static void TvStand(RaidMesh m, System.Random rng)
        {
            Color wood = V(Walnut, rng, 0.08f);
            m.Box(new Vector3(0f, 0.25f, -0.02f), new Vector3(1.0f, 0.5f, 0.44f), M(wood));
            m.Box(new Vector3(0f, 0.2f, 0.2f), new Vector3(0.9f, 0.28f, 0.01f), M(new Color(0.05f, 0.05f, 0.05f)));
            m.Box(new Vector3(-0.15f, 0.12f, 0.1f), new Vector3(0.4f, 0.08f, 0.26f), M(new Color(0.08f, 0.08f, 0.09f)));
            m.Box(new Vector3(-0.1f, 0.125f, 0.232f), new Vector3(0.14f, 0.02f, 0.004f), Gl(new Color(0.2f, 0.6f, 0.3f)));
            Color tv = Chance(rng, 0.5) ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.36f, 0.24f, 0.14f);
            m.Box(new Vector3(0f, 0.78f, -0.04f), new Vector3(0.68f, 0.56f, 0.5f), M(tv));
            m.Box(new Vector3(-0.07f, 0.79f, 0.215f), new Vector3(0.48f, 0.38f, 0.01f), Gs(ScreenOff));
            m.Box(new Vector3(0.25f, 0.79f, 0.215f), new Vector3(0.1f, 0.42f, 0.012f), M(tv * 0.6f));
            m.Cylinder(new Vector3(0.25f, 0.88f, 0.21f), 0.025f, 0.03f, 6, M(Chrome), Quaternion.Euler(90f, 0f, 0f));
            m.Cylinder(new Vector3(0.25f, 0.78f, 0.21f), 0.025f, 0.03f, 6, M(Chrome), Quaternion.Euler(90f, 0f, 0f));
            m.Ellipsoid(new Vector3(0f, 1.08f, -0.1f), new Vector3(0.12f, 0.05f, 0.1f), M(Black), 6, 3);
            m.Rod(new Vector3(0f, 1.1f, -0.1f), new Vector3(-0.28f, 1.55f, -0.16f), 0.008f, Mt(Chrome));
            m.Rod(new Vector3(0f, 1.1f, -0.1f), new Vector3(0.3f, 1.52f, -0.12f), 0.008f, Mt(Chrome));
        }

        /// <summary>A low coffee table with magazines and an ashtray.</summary>
        public static void CoffeeTable(RaidMesh m, System.Random rng, float w, float d)
        {
            Color c = V(Oak, rng, 0.08f);
            m.Box(new Vector3(0f, 0.42f, 0f), new Vector3(w, 0.04f, d), G(c));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3((i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.05f), 0.2f, (i < 2 ? -1f : 1f) * (d * 0.5f - 0.05f)), new Vector3(0.04f, 0.4f, 0.04f), M(c * 0.8f));
            for (int i = 0; i < rng.Next(1, 4); i++)
                m.Box(new Vector3(R(rng, -w * 0.3f, w * 0.3f), 0.443f + i * 0.005f, R(rng, -d * 0.25f, d * 0.25f)), new Vector3(0.22f, 0.006f, 0.28f), Yaw(R(rng, -30f, 30f)), M(V(new Color(0.6f, 0.4f, 0.3f), rng, 0.3f)));
            if (Chance(rng, 0.5)) m.Cylinder(new Vector3(R(rng, -w * 0.3f, w * 0.3f), 0.44f, R(rng, -d * 0.2f, d * 0.2f)), 0.06f, 0.025f, 8, Gs(new Color(0.35f, 0.42f, 0.38f)));
        }

        /// <summary>A bed (width along X, length along Z, head at -Z) with headboard, mattress,
        /// rumpled blanket and pillows.</summary>
        public static void Bed(RaidMesh m, System.Random rng, float w, float l, Color blanket, Color wood)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(0f, 0.2f, 0f), new Vector3(w, 0.22f, l), M(c));
            m.Box(new Vector3(0f, 0.55f, -l * 0.5f + 0.03f), new Vector3(w + 0.06f, 1.1f, 0.06f), M(c * 1.05f));
            m.Box(new Vector3(0f, 1.08f, -l * 0.5f + 0.03f), new Vector3(w + 0.1f, 0.06f, 0.09f), M(c * 0.9f));
            m.Box(new Vector3(0f, 0.3f, l * 0.5f - 0.025f), new Vector3(w + 0.06f, 0.6f, 0.05f), M(c * 1.05f));
            m.Box(new Vector3(0f, 0.41f, -0.01f), new Vector3(w - 0.06f, 0.2f, l - 0.1f), M(new Color(0.80f, 0.78f, 0.72f)));
            Color b = V(blanket, rng, 0.08f);
            float bl = l * R(rng, 0.55f, 0.72f);
            float bz = l * 0.5f - 0.06f - bl * 0.5f;
            m.Box(new Vector3(0f, 0.53f, bz), new Vector3(w + 0.02f, 0.06f, bl), Yaw(R(rng, -3f, 3f)), M(b));
            m.Box(new Vector3(-w * 0.5f - 0.01f, 0.38f, bz), new Vector3(0.03f, 0.3f, bl), M(b * 0.9f));
            m.Box(new Vector3(w * 0.5f + 0.01f, 0.38f, bz), new Vector3(0.03f, 0.3f, bl), M(b * 0.9f));
            if (Chance(rng, 0.5))
                m.Ellipsoid(new Vector3(R(rng, -0.2f, 0.2f), 0.58f, bz - bl * 0.3f), new Vector3(w * 0.7f, 0.12f, 0.35f), M(b * 1.05f), 8, 4);
            int pillows = w > 1.2f ? 2 : 1;
            for (int i = 0; i < pillows; i++)
            {
                float x = pillows == 1 ? 0f : (i == 0 ? -w * 0.24f : w * 0.24f);
                m.Ellipsoid(new Vector3(x, 0.56f, -l * 0.5f + 0.3f), new Vector3(Mathf.Min(0.55f, w * 0.42f), 0.13f, 0.34f), Yaw(R(rng, -8f, 8f)), M(new Color(0.84f, 0.83f, 0.78f)), 8, 4);
            }
        }

        /// <summary>A steel army bunk bed (0.9 × 2.0) with two mattresses, olive blankets and a ladder.</summary>
        public static void BunkBed(RaidMesh m, System.Random rng)
        {
            Color frame = V(new Color(0.28f, 0.31f, 0.24f), rng, 0.05f);
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3((i % 2 == 0 ? -0.43f : 0.43f), 0.85f, (i < 2 ? -0.97f : 0.97f)), new Vector3(0.04f, 1.7f, 0.04f), M(frame));
            float[] levels = { 0.38f, 1.28f };
            foreach (float y in levels)
            {
                m.Box(new Vector3(-0.43f, y, 0f), new Vector3(0.04f, 0.06f, 1.94f), M(frame));
                m.Box(new Vector3(0.43f, y, 0f), new Vector3(0.04f, 0.06f, 1.94f), M(frame));
                m.Box(new Vector3(0f, y + 0.08f, 0f), new Vector3(0.82f, 0.12f, 1.9f), M(new Color(0.72f, 0.70f, 0.62f)));
                Color b = V(Olive, rng, 0.08f);
                float bl = R(rng, 1.1f, 1.4f);
                m.Box(new Vector3(0f, y + 0.16f, 0.95f - bl * 0.5f), new Vector3(0.86f, 0.04f, bl), Yaw(R(rng, -3f, 3f)), M(b));
                m.Ellipsoid(new Vector3(0f, y + 0.2f, -0.72f), new Vector3(0.5f, 0.1f, 0.28f), M(new Color(0.8f, 0.79f, 0.74f)), 8, 4);
            }
            m.Box(new Vector3(0f, 1.72f, -0.97f), new Vector3(0.9f, 0.04f, 0.04f), M(frame));
            m.Box(new Vector3(0f, 1.72f, 0.97f), new Vector3(0.9f, 0.04f, 0.04f), M(frame));
            for (int r = 0; r < 4; r++) m.Box(new Vector3(0.43f, 0.6f + r * 0.25f, 0.97f), new Vector3(0.04f, 0.03f, 0.04f), M(frame));
        }

        /// <summary>A two-door wooden wardrobe (1.0 wide, 0.58 deep, 1.95 high), sometimes ajar.</summary>
        public static void Wardrobe(RaidMesh m, System.Random rng, Color wood)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(0f, 0.05f, -0.02f), new Vector3(0.98f, 0.1f, 0.54f), M(c * 0.7f));
            m.Box(new Vector3(0f, 1.0f, -0.03f), new Vector3(1.0f, 1.8f, 0.54f), M(c));
            m.Box(new Vector3(0f, 1.93f, -0.02f), new Vector3(1.06f, 0.07f, 0.6f), M(c * 0.85f));
            // thin wooden trim capping the wardrobe top (adds a worn‑look)
            m.Box(new Vector3(0f, 1.93f + 0.07f, 0f), new Vector3(0.98f, 0.02f, 0.54f), M(c * 0.95f));
            // left side wooden frame strip
            m.Box(new Vector3(-0.49f, 0.9f, 0f), new Vector3(0.02f, 1.8f, 0.54f), M(c * 0.95f));
            // right side wooden frame strip
            m.Box(new Vector3(0.49f, 0.9f, 0f), new Vector3(0.02f, 1.8f, 0.54f), M(c * 0.95f));
            // occasional brass plaque centered on the top trim
            if (Chance(rng, 0.3))
                m.Box(new Vector3(0f, 1.93f + 0.07f + 0.01f, 0f), new Vector3(0.10f, 0.004f, 0.04f), Mt(Brass));
            bool ajar = Chance(rng, 0.35);
            for (int s = -1; s <= 1; s += 2)
            {
                if (ajar && s == 1)
                {
                    m.Box(new Vector3(0.25f, 1.0f, 0.24f), new Vector3(0.46f, 1.7f, 0.01f), M(new Color(0.05f, 0.04f, 0.04f)));
                    Quaternion open = Yaw(55f);
                    Vector3 hinge = new Vector3(0.49f, 0f, 0.26f);
                    m.Box(hinge + open * new Vector3(-0.24f, 1.0f, 0.01f), new Vector3(0.48f, 1.74f, 0.022f), open, M(c * 1.06f));
                    m.Box(new Vector3(0.25f, 0.6f, 0.0f), new Vector3(0.4f, 0.3f, 0.4f), M(V(new Color(0.4f, 0.35f, 0.5f), rng, 0.3f)));
                    continue;
                }
                m.Box(new Vector3(s * 0.25f, 1.0f, 0.255f), new Vector3(0.48f, 1.74f, 0.02f), M(c * 1.06f));
                m.Box(new Vector3(s * 0.25f, 1.35f, 0.268f), new Vector3(0.36f, 0.6f, 0.01f), M(c * 0.94f));
                m.Box(new Vector3(s * 0.25f, 0.6f, 0.268f), new Vector3(0.36f, 0.6f, 0.01f), M(c * 0.94f));
                // brass pull handle (horizontal bar) centred on each door
                            m.Box(new Vector3(s * 0.25f, 1.2f, 0.30f), new Vector3(0.12f, 0.015f, 0.01f), Mt(Brass));
            }
        }

        /// <summary>A chest of drawers (1.0 × 0.48 × 0.8) with knobs and something on top.</summary>
        public static void Dresser(RaidMesh m, System.Random rng, Color wood)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(0f, 0.41f, -0.02f), new Vector3(1.0f, 0.78f, 0.46f), M(c));
            m.Box(new Vector3(0f, 0.81f, -0.01f), new Vector3(1.04f, 0.03f, 0.5f), G(c * 1.05f));
            for (int r = 0; r < 3; r++)
            {
                float y = 0.16f + r * 0.23f;
                m.Box(new Vector3(0f, y, 0.215f), new Vector3(0.94f, 0.2f, 0.02f), M(c * 1.1f));
                m.Box(new Vector3(-0.25f, y, 0.23f), new Vector3(0.04f, 0.04f, 0.02f), Mt(Brass));
                m.Box(new Vector3(0.25f, y, 0.23f), new Vector3(0.04f, 0.04f, 0.02f), Mt(Brass));
            }
            if (Chance(rng, 0.6)) m.Box(new Vector3(R(rng, -0.3f, 0.3f), 0.88f, -0.05f), new Vector3(0.2f, 0.1f, 0.14f), M(V(Walnut, rng, 0.2f)));
        }

        /// <summary>A bedside table with a fabric-shaded lamp. Returns the bulb position.</summary>
        public static Vector3 Nightstand(RaidMesh m, System.Random rng, Color wood, bool lit)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(0f, 0.3f, -0.02f), new Vector3(0.45f, 0.6f, 0.4f), M(c));
            m.Box(new Vector3(0f, 0.4f, 0.185f), new Vector3(0.39f, 0.16f, 0.02f), M(c * 1.1f));
            m.Box(new Vector3(0f, 0.4f, 0.2f), new Vector3(0.04f, 0.03f, 0.02f), Mt(Brass));
            // side‑mounted metal switch
            m.Box(new Vector3(0.18f, 0.34f, -0.03f), new Vector3(0.02f, 0.04f, 0.006f), Mt(Brass));
            // fabric rib at the front edge of the shade
            m.Box(new Vector3(0f, 0.47f, -0.06f), new Vector3(0.38f, 0.004f, 0.004f), G(new Color(0.6f, 0.5f, 0.35f)));
            // brass rivet centred on the shade top
            m.Box(new Vector3(0f, 0.48f, -0.04f), new Vector3(0.008f, 0.008f, 0.001f), Mt(Brass));
            // worn line on the wood surface
            m.Box(new Vector3(0f, 0.31f, -0.05f), new Vector3(0.36f, 0.001f, 0.01f), G(new Color(0.12f, 0.10f, 0.09f)));
            m.Cylinder(new Vector3(0f, 0.6f, -0.04f), 0.07f, 0.04f, 8, G(new Color(0.6f, 0.5f, 0.35f)));
            m.Cylinder(new Vector3(0f, 0.64f, -0.04f), 0.02f, 0.22f, 6, Mt(Brass));
            // main bulb body (unchanged)
            m.Cylinder(new Vector3(0f, 0.84f, -0.04f), 0.15f, 0.2f, 10, lit ? Gl(new Color(0.95f, 0.78f, 0.52f)) : M(new Color(0.78f, 0.70f, 0.55f)), Quaternion.identity, 0.65f, false);
            // tiny emissive spot at the centre of the bulb (visible only when lit)
            if (lit)
                m.Box(new Vector3(0f, 0.84f, -0.04f), new Vector3(0.03f, 0.03f, 0.03f), Gl(new Color(1f, 0.93f, 0.65f)));
            // occasional carved label on the wooden side
            if (Chance(rng, 0.3f))
                m.Box(new Vector3(-0.22f, 0.38f, -0.02f), new Vector3(0.04f, 0.008f, 0.001f), G(new Color(0.25f, 0.22f, 0.18f)));
            return new Vector3(0f, 0.9f, -0.04f);
        }

        /// <summary>A bookcase (0.9 × 0.32 × 1.8) with rows of spines and a few lying books.</summary>
        public static void Bookshelf(RaidMesh m, System.Random rng, Color wood, int stockKind = 1)
        {
            Color c = V(wood, rng, 0.06f);
            m.Box(new Vector3(-0.44f, 0.9f, 0f), new Vector3(0.02f, 1.8f, 0.32f), M(c));
            m.Box(new Vector3(0.44f, 0.9f, 0f), new Vector3(0.02f, 1.8f, 0.32f), M(c));
            m.Box(new Vector3(0f, 0.9f, -0.15f), new Vector3(0.88f, 1.8f, 0.02f), M(c * 0.7f));
            // thin wooden strip capping the top edge (adds a worn‑look)
            m.Box(new Vector3(0f, 0.9f + 1.8f, 0f), new Vector3(0.88f, 0.02f, 0.32f), M(c * 0.95f));
            // top‑left corner cap
            m.Box(new Vector3(-0.44f + 0.01f, 0.9f + 1.8f + 0.01f, -0.16f + 0.01f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            // top‑right corner cap
            m.Box(new Vector3( 0.44f - 0.01f, 0.9f + 1.8f + 0.01f, -0.16f + 0.01f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            // bottom‑left corner cap
            m.Box(new Vector3(-0.44f + 0.01f, 0.9f + 0.02f + 0.01f, -0.16f + 0.01f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            // bottom‑right corner cap
            m.Box(new Vector3( 0.44f - 0.01f, 0.9f + 0.02f + 0.01f, -0.16f + 0.01f), new Vector3(0.02f, 0.02f, 0.02f), Mt(Brass));
            // occasional side brass plaque centered on the right side board
            if (Chance(rng, 0.3))
            {
                m.Box(new Vector3(0.44f + 0.011f, 0.9f + 0.9f, 0f), new Vector3(0.12f, 0.004f, 0.04f), Mt(Brass));
            }
            // hanging leather cords on each side of the top trim
            // left‑hand cord
            m.Cylinder(new Vector3(-0.34f, 0.9f + 1.8f + 0.03f, -0.12f), 0.005f, 0.06f, 6, G(new Color(0.42f, 0.37f, 0.33f)), Yaw(R(rng, -8f, 8f)));
            // right‑hand cord
            m.Cylinder(new Vector3( 0.34f, 0.9f + 1.8f + 0.03f, -0.12f), 0.005f, 0.06f, 6, G(new Color(0.42f, 0.37f, 0.33f)), Yaw(R(rng, -8f, 8f)));
            // subtle wear scratches near bottom edge
            // left‑side scratch
            m.Box(new Vector3(-0.2f + R(rng, -0.02f, 0.02f), 0.9f + 0.02f + 0.005f, -0.14f + R(rng, -0.01f, 0.01f)), new Vector3(0.15f, 0.002f, 0.004f), G(new Color(0.15f, 0.13f, 0.11f)));
            // right‑side scratch
            m.Box(new Vector3( 0.2f + R(rng, -0.02f, 0.02f), 0.9f + 0.02f + 0.005f, -0.14f + R(rng, -0.01f, 0.01f)), new Vector3(0.15f, 0.002f, 0.004f), G(new Color(0.15f, 0.13f, 0.11f)));

            // occasional brass plaque centered on the trim
            if (Chance(rng, 0.4))
                m.Box(new Vector3(0f, 0.9f + 1.8f + 0.01f, 0f), new Vector3(0.12f, 0.004f, 0.04f), Mt(Brass));
            for (int s = 0; s < 5; s++)
            {
                float y = 0.9f + 0.05f + s * 0.43f;
                m.Box(new Vector3(0f, y, 0f), new Vector3(0.88f, 0.025f, 0.3f), M(c));
                if (s == 4) break;
                float u0 = (float)rng.NextDouble() * 0.5f;
                m.Upright(new Vector3(0f, y + 0.17f, 0.1f), Vector3.forward, 0.86f, 0.32f, DecorMaterials.Stock(stockKind), new Rect(u0, 0f, 0.5f, 1f));
            }
        }
        #endregion
    }
}
