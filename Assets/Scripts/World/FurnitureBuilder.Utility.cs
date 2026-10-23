using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        #region Clinic
        /// <summary>A white steel medicine cabinet (0.9 × 0.42 × 1.8): glazed upper doors over shelves
        /// of boxes and bottles, solid lower doors, a red cross.</summary>
        public static void MedCabinet(RaidMesh m, System.Random rng)
        {
            Color c = V(new Color(0.86f, 0.87f, 0.86f), rng, 0.03f);
            m.Box(new Vector3(0f, 0.9f, -0.03f), new Vector3(0.9f, 1.8f, 0.36f), G(c));
            m.Box(new Vector3(0f, 0.04f, -0.02f), new Vector3(0.86f, 0.08f, 0.34f), M(new Color(0.2f, 0.2f, 0.2f)));
            // Upper half: shelves of stock seen through the glazing frames.
            m.Box(new Vector3(0f, 1.32f, 0.12f), new Vector3(0.82f, 0.86f, 0.01f), M(new Color(0.55f, 0.58f, 0.58f)));
            for (int s = 0; s < 3; s++)
            {
                float y = 0.98f + s * 0.27f;
                m.Box(new Vector3(0f, y, 0.0f), new Vector3(0.82f, 0.015f, 0.3f), G(c));
                m.Upright(new Vector3(0f, y + 0.1f, 0.135f), Vector3.forward, 0.8f, 0.18f, DecorMaterials.Stock(2), new Rect((float)rng.NextDouble() * 0.5f, 0f, 0.5f, 0.6f));
            }
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.43f, 1.32f, 0.16f), new Vector3(0.04f, 0.9f, 0.03f), G(c));
                m.Box(new Vector3(s * 0.02f, 1.32f, 0.16f), new Vector3(0.03f, 0.9f, 0.03f), G(c));
            }
            m.Box(new Vector3(0f, 1.76f, 0.16f), new Vector3(0.9f, 0.04f, 0.03f), G(c));
            m.Box(new Vector3(0f, 0.88f, 0.16f), new Vector3(0.9f, 0.04f, 0.03f), G(c));
            // Lower doors.
            m.Box(new Vector3(-0.22f, 0.47f, 0.16f), new Vector3(0.43f, 0.78f, 0.02f), G(c * 0.97f));
            m.Box(new Vector3(0.22f, 0.47f, 0.16f), new Vector3(0.43f, 0.78f, 0.02f), G(c * 0.97f));
            m.Box(new Vector3(-0.04f, 0.6f, 0.175f), new Vector3(0.02f, 0.1f, 0.02f), Mt(Chrome));
            m.Box(new Vector3(0.04f, 0.6f, 0.175f), new Vector3(0.02f, 0.1f, 0.02f), Mt(Chrome));
            // Chrome hinge pins on the side frames
            m.Box(new Vector3(-0.45f, 0.6f, 0.175f), new Vector3(0.02f, 0.1f, 0.02f), Mt(Chrome));
            m.Box(new Vector3(0.45f, 0.6f, 0.175f), new Vector3(0.02f, 0.1f, 0.02f), Mt(Chrome));
            Color red = new Color(0.72f, 0.12f, 0.10f);
            m.Box(new Vector3(-0.22f, 0.62f, 0.172f), new Vector3(0.14f, 0.04f, 0.004f), M(red));
            m.Box(new Vector3(-0.22f, 0.62f, 0.172f), new Vector3(0.04f, 0.14f, 0.004f), M(red));
        }

        /// <summary>A hospital bed (0.9 × 2.0): white tube frame on casters, raised back section,
        /// side rails, pale sheet and pillow; head at -Z.</summary>
        public static void HospitalBed(RaidMesh m, System.Random rng)
        {
            Color white = new Color(0.82f, 0.83f, 0.82f);
            Color sheet = V(new Color(0.62f, 0.70f, 0.68f), rng, 0.05f);
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(i % 2 == 0 ? -0.4f : 0.4f, 0f, i < 2 ? -0.9f : 0.9f);
                m.Box(p + new Vector3(0f, 0.3f, 0f), new Vector3(0.04f, 0.5f, 0.04f), Mt(white));
                m.Box(p + new Vector3(0f, 0.04f, 0f), new Vector3(0.05f, 0.08f, 0.07f), M(Black));
            }
            m.Box(new Vector3(0f, 0.5f, 0f), new Vector3(0.88f, 0.06f, 1.96f), Mt(white));
            m.Box(new Vector3(0f, 0.6f, 0.35f), new Vector3(0.84f, 0.14f, 1.2f), M(sheet));
            Quaternion back = Quaternion.Euler(-35f, 0f, 0f);
            m.Box(new Vector3(0f, 0.78f, -0.6f), new Vector3(0.84f, 0.14f, 0.78f), back, M(sheet));
            m.Ellipsoid(new Vector3(0f, 1.0f, -0.82f), new Vector3(0.55f, 0.14f, 0.32f), back, M(new Color(0.88f, 0.88f, 0.86f)), 8, 4);
            m.Box(new Vector3(0f, 0.85f, -0.99f), new Vector3(0.9f, 0.7f, 0.04f), G(white));
            m.Box(new Vector3(0f, 0.75f, 0.99f), new Vector3(0.9f, 0.5f, 0.04f), G(white));
            for (int s = -1; s <= 1; s += 2)
            {
                float y = s < 0 || Chance(rng, 0.5) ? 0.8f : 0.6f;
                m.Box(new Vector3(s * 0.46f, y, -0.1f), new Vector3(0.025f, 0.025f, 1.1f), Mt(white));
                m.Box(new Vector3(s * 0.46f, y - 0.12f, -0.1f), new Vector3(0.025f, 0.025f, 1.1f), Mt(white));
            }
            // metal end‑caps on the side‑rails (tiny chrome squares)
            float railHalfLength = 0.55f; // half of the rail length (1.1 m)
            foreach (float side in new[] { -1f, 1f })
            {
                float y = side < 0 || Chance(rng, 0.5f) ? 0.8f : 0.6f;
                // front end (positive Z)
                m.Box(new Vector3(side * 0.46f, y, -0.1f + railHalfLength), new Vector3(0.03f, 0.03f, 0.02f), Mt(Chrome));
                // rear end (negative Z)
                m.Box(new Vector3(side * 0.46f, y, -0.1f - railHalfLength), new Vector3(0.03f, 0.03f, 0.02f), Mt(Chrome));
            }
            if (Chance(rng, 0.5))
                m.Box(new Vector3(0.1f, 0.69f, 0.55f), new Vector3(0.7f, 0.05f, 0.8f), Yaw(R(rng, -10f, 10f)), M(V(new Color(0.55f, 0.62f, 0.66f), rng)));
        }

        /// <summary>A drip stand: wheeled base, pole and a bag.</summary>
        public static void IvStand(RaidMesh m, System.Random rng, Vector3 at)
        {
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                m.Rod(at + new Vector3(0f, 0.08f, 0f), at + new Vector3(Mathf.Sin(a) * 0.25f, 0.04f, Mathf.Cos(a) * 0.25f), 0.025f, Mt(Chrome));
            }
            m.Cylinder(at + new Vector3(0f, 0.06f, 0f), 0.015f, 1.75f, 6, Mt(Chrome));
            m.Box(at + new Vector3(0f, 1.8f, 0f), new Vector3(0.3f, 0.015f, 0.015f), Mt(Chrome));
            m.Box(at + new Vector3(0.1f, 1.62f, 0f), new Vector3(0.1f, 0.18f, 0.04f), Gs(new Color(0.75f, 0.8f, 0.78f)));
        }

        /// <summary>A three-panel folding privacy screen of pale curtain on a chrome frame.</summary>
        public static void PrivacyScreen(RaidMesh m, System.Random rng)
        {
            Color cloth = V(new Color(0.60f, 0.70f, 0.66f), rng, 0.05f);
            float[] yaws = { -25f, 0f, 25f };
            Vector3 p = new Vector3(-0.844f, 0f, -0.127f);
            for (int i = 0; i < 3; i++)
            {
                Quaternion q = Yaw(yaws[i]);
                Vector3 c = p + q * new Vector3(0.3f, 0f, 0f);
                m.Box(c + Vector3.up * 0.95f, new Vector3(0.58f, 1.5f, 0.015f), q, M(cloth));
                m.Box(c + Vector3.up * 1.72f, new Vector3(0.6f, 0.025f, 0.025f), q, Mt(Chrome));
                m.Box(p + Vector3.up * 0.9f, new Vector3(0.025f, 1.8f, 0.025f), Mt(Chrome));
                p += q * new Vector3(0.6f, 0f, 0f);
            }
            m.Box(p + Vector3.up * 0.9f, new Vector3(0.025f, 1.8f, 0.025f), Mt(Chrome));
        }

        /// <summary>A wheelchair: two big wheels, small casters, sling seat and back.</summary>
        public static void Wheelchair(RaidMesh m, System.Random rng)
        {
            Color frame = Chrome, seat = new Color(0.12f, 0.12f, 0.14f);
            for (int s = -1; s <= 1; s += 2)
            {
                m.CylinderX(new Vector3(s * 0.3f, 0.3f, -0.05f), 0.3f, 0.03f, 14, M(Black));
                m.CylinderX(new Vector3(s * 0.28f, 0.3f, -0.05f), 0.26f, 0.015f, 14, Mt(frame));
                m.CylinderX(new Vector3(s * 0.22f, 0.07f, 0.32f), 0.07f, 0.03f, 8, M(Black));
                m.Rod(new Vector3(s * 0.22f, 0.12f, 0.32f), new Vector3(s * 0.22f, 0.5f, 0.2f), 0.025f, Mt(frame));
                m.Rod(new Vector3(s * 0.22f, 0.5f, -0.2f), new Vector3(s * 0.22f, 0.95f, -0.25f), 0.025f, Mt(frame));
            }
            m.Box(new Vector3(0f, 0.5f, 0f), new Vector3(0.44f, 0.04f, 0.42f), M(seat));
            m.Box(new Vector3(0f, 0.75f, -0.23f), new Vector3(0.44f, 0.38f, 0.03f), Quaternion.Euler(-8f, 0f, 0f), M(seat));
        }
        #endregion

        #region Stores, warehouses and garages
        /// <summary>A steel shelving unit (width along X) with <paramref name="levels"/> shelves of stock:
        /// boxes, tins and parts. <paramref name="stockKind"/> picks the printed stock strip.</summary>
        public static void MetalShelf(RaidMesh m, System.Random rng, float w, float d, float h, int levels, int stockKind, Color frame)
        {
            Color f = V(frame, rng, 0.05f);
            for (int i = 0; i < 4; i++) {
                m.Box(new Vector3((i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.02f), h * 0.5f, (i < 2 ? -1f : 1f) * (d * 0.5f - 0.02f)), new Vector3(0.04f, h, 0.04f), M(f));
                // Add subtle chrome rivets to each corner post
                float rivFraction = 0.2f + (float)rng.NextDouble() * 0.6f;
                Vector3 postPos = new Vector3((i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.02f), h * 0.5f, (i < 2 ? -1f : 1f) * (d * 0.5f - 0.02f));
                Vector3 rivPos = postPos + new Vector3(0f, -h * 0.5f + rivFraction * h, 0f);
                m.Cylinder(rivPos, 0.005f, 0.004f, 6, Mt(Chrome));
                m.Cylinder(rivPos, 0.005f, 0.004f, 6, Mt(Chrome));
            }
            for (int l = 0; l < levels; l++)
            {
                float y = 0.08f + l * (h - 0.12f) / Mathf.Max(1, levels - 1);
                m.Box(new Vector3(0f, y, 0f), new Vector3(w - 0.02f, 0.025f, d), M(f * 1.1f));
                if (l == levels - 1 && y > 1.7f) continue;
                float slot = l == levels - 1 ? 0.3f : (h - 0.12f) / Mathf.Max(1, levels - 1) - 0.05f;
                if (Chance(rng, 0.65))
                    m.Upright(new Vector3(0f, y + 0.013f + slot * 0.5f, d * 0.5f - 0.04f), Vector3.forward, w - 0.08f, slot, DecorMaterials.Stock(stockKind), new Rect((float)rng.NextDouble() * 0.4f, 0f, 0.6f, Mathf.Clamp01(slot / 0.32f)));
                int boxes = rng.Next(0, 3);
                for (int b = 0; b < boxes; b++)
                {
                    float bw = R(rng, 0.25f, 0.45f), bh = Mathf.Min(slot - 0.02f, R(rng, 0.18f, 0.32f));
                    if (bh < 0.1f) continue;
                    CardboardBox(m, rng, new Vector3(R(rng, -w * 0.35f, w * 0.35f), y + 0.013f, R(rng, -0.05f, 0.02f)), new Vector3(bw, bh, Mathf.Min(d - 0.06f, R(rng, 0.25f, 0.4f))), R(rng, -8f, 8f));
                }
            }
        }

        /// <summary>A double-sided store gondola (length along X, 0.9 deep, 1.45 high) stocked on both faces.</summary>
        public static void Gondola(RaidMesh m, System.Random rng, float len)
        {
            Color body = new Color(0.78f, 0.78f, 0.76f);
            m.Box(new Vector3(0f, 0.08f, 0f), new Vector3(len, 0.16f, 0.86f), M(body * 0.7f));
            m.Box(new Vector3(0f, 0.75f, 0f), new Vector3(len, 1.4f, 0.06f), M(body));
            for (int s = -1; s <= 1; s += 2)
            {
                for (int l = 0; l < 4; l++)
                {
                    float y = 0.2f + l * 0.33f;
                    m.Box(new Vector3(0f, y, s * 0.22f), new Vector3(len, 0.02f, 0.38f), M(body * 0.95f));
                    if (Chance(rng, 0.8))
                        m.Upright(new Vector3(0f, y + 0.14f, s * 0.38f), new Vector3(0f, 0f, s), len - 0.04f, 0.26f, DecorMaterials.Stock(0), new Rect((float)rng.NextDouble() * 0.5f, 0f, 0.5f, 0.85f));
                }
            }
            m.Box(new Vector3(0f, 1.47f, 0f), new Vector3(len, 0.04f, 0.12f), M(new Color(0.7f, 0.15f, 0.12f)));
        }

        /// <summary>A glass-door drinks cooler (0.9 × 0.75 × 2.0) lit from within.</summary>
        public static void DrinksCooler(RaidMesh m, System.Random rng, bool lit)
        {
            Color frame = new Color(0.82f, 0.82f, 0.80f);
            m.Box(new Vector3(0f, 1.0f, -0.05f), new Vector3(0.9f, 2.0f, 0.62f), M(frame));
            m.Box(new Vector3(0f, 1.9f, 0.27f), new Vector3(0.9f, 0.2f, 0.04f), M(new Color(0.70f, 0.14f, 0.12f)));
            m.Box(new Vector3(0f, 1.0f, -0.2f), new Vector3(0.78f, 1.6f, 0.02f), lit ? Gl(new Color(0.62f, 0.72f, 0.78f)) : M(new Color(0.25f, 0.27f, 0.28f)));
            for (int s = 0; s < 4; s++)
            {
                float y = 0.3f + s * 0.4f;
                m.Box(new Vector3(0f, y, 0.0f), new Vector3(0.78f, 0.015f, 0.42f), Mt(Chrome));
                if (Chance(rng, 0.85))
                    m.Upright(new Vector3(0f, y + 0.14f, 0.16f), Vector3.forward, 0.74f, 0.26f, DecorMaterials.Stock(0), new Rect((float)rng.NextDouble() * 0.5f, 0f, 0.45f, 0.85f));
            }
            m.Box(new Vector3(-0.42f, 1.0f, 0.27f), new Vector3(0.05f, 1.62f, 0.05f), Mt(Chrome));
            m.Box(new Vector3(0.42f, 1.0f, 0.27f), new Vector3(0.05f, 1.62f, 0.05f), Mt(Chrome));
            m.Box(new Vector3(0f, 0.17f, 0.27f), new Vector3(0.9f, 0.06f, 0.05f), Mt(Chrome));
            m.Box(new Vector3(0.36f, 1.05f, 0.31f), new Vector3(0.03f, 0.5f, 0.04f), Mt(Chrome));
            m.Box(new Vector3(0f, 0.08f, 0.2f), new Vector3(0.86f, 0.12f, 0.1f), M(Black));
        }

        /// <summary>A shop counter run (length along X, 0.7 deep, 1.0 high) with a cash register.</summary>
        public static void ShopCounter(RaidMesh m, System.Random rng, float len)
        {
            Color c = V(new Color(0.55f, 0.38f, 0.22f), rng, 0.05f);
            m.Box(new Vector3(0f, 0.48f, 0f), new Vector3(len, 0.96f, 0.66f), M(c));
            m.Box(new Vector3(0f, 0.98f, 0f), new Vector3(len + 0.04f, 0.04f, 0.72f), G(new Color(0.72f, 0.70f, 0.64f)));
            m.Box(new Vector3(0f, 0.5f, 0.335f), new Vector3(len - 0.1f, 0.7f, 0.01f), M(c * 0.85f));
            float rx = -len * 0.3f;
            m.Box(new Vector3(rx, 1.08f, -0.05f), new Vector3(0.4f, 0.16f, 0.4f), M(new Color(0.18f, 0.18f, 0.2f)));
            m.Box(new Vector3(rx, 1.2f, 0.1f), new Vector3(0.36f, 0.12f, 0.12f), Quaternion.Euler(30f, 0f, 0f), M(new Color(0.24f, 0.24f, 0.26f)));
            m.Box(new Vector3(rx, 1.32f, -0.15f), new Vector3(0.2f, 0.1f, 0.04f), M(new Color(0.15f, 0.15f, 0.16f)));
            m.Box(new Vector3(rx, 1.35f, -0.127f), new Vector3(0.02f, 0.04f, 0.005f), Mt(Chrome));
            m.Box(new Vector3(rx, 1.32f, -0.128f), new Vector3(0.16f, 0.05f, 0.004f), Gl(new Color(0.2f, 0.6f, 0.4f)));
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(R(rng, 0f, len * 0.4f), 1.0f + i * 0.02f, R(rng, -0.2f, 0.2f)), new Vector3(0.2f, 0.015f, 0.28f), Yaw(R(rng, -30f, 30f)), M(V(new Color(0.7f, 0.5f, 0.3f), rng, 0.3f)));
        }

        /// <summary>A heavy work bench (1.8 × 0.75) with a vice, a pegboard of tools and a lower shelf.</summary>
        public static void ToolBench(RaidMesh m, System.Random rng)
        {
            Color top = V(new Color(0.48f, 0.34f, 0.20f), rng, 0.06f);
            Color legs = new Color(0.20f, 0.24f, 0.30f);
            m.Box(new Vector3(0f, 0.88f, 0f), new Vector3(1.8f, 0.07f, 0.75f), M(top));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(i % 2 == 0 ? -0.85f : 0.85f, 0.43f, i < 2 ? -0.32f : 0.32f), new Vector3(0.05f, 0.86f, 0.05f), M(legs));
            m.Box(new Vector3(0f, 0.18f, 0f), new Vector3(1.7f, 0.03f, 0.66f), M(top * 0.8f));
            m.Box(new Vector3(0f, 1.42f, -0.35f), new Vector3(1.8f, 0.95f, 0.03f), M(new Color(0.55f, 0.42f, 0.28f)));
            for (int i = 0; i < 9; i++)
            {
                float x = -0.75f + i * 0.19f + R(rng, -0.03f, 0.03f);
                float hh = R(rng, 0.12f, 0.3f);
                m.Box(new Vector3(x, R(rng, 1.25f, 1.6f), -0.32f), new Vector3(R(rng, 0.03f, 0.06f), hh, 0.03f), Quaternion.Euler(0f, 0f, R(rng, -20f, 20f)), Mt(new Color(0.35f, 0.36f, 0.38f)));
            }
            m.Box(new Vector3(0.75f, 0.98f, 0.3f), new Vector3(0.18f, 0.14f, 0.2f), M(new Color(0.2f, 0.28f, 0.42f)));
            m.Box(new Vector3(0.75f, 0.98f, 0.45f), new Vector3(0.04f, 0.04f, 0.16f), Mt(Chrome));
            m.Box(new Vector3(-0.5f, 0.96f, 0.0f), new Vector3(0.5f, 0.1f, 0.22f), G(new Color(0.65f, 0.12f, 0.1f)));
            m.Box(new Vector3(-0.5f, 1.03f, 0.0f), new Vector3(0.3f, 0.03f, 0.03f), M(Black));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(R(rng, -0.2f, 0.6f), 0.93f, R(rng, -0.2f, 0.25f)), new Vector3(R(rng, 0.04f, 0.2f), 0.025f, 0.04f), Yaw(R(rng, 0f, 180f)), Mt(new Color(0.4f, 0.4f, 0.42f)));
            CardboardBox(m, rng, new Vector3(R(rng, -0.5f, 0.5f), 0.195f, 0f), new Vector3(0.5f, 0.3f, 0.4f), R(rng, -10f, 10f));
        }

        /// <summary>A stack of worn tyres.</summary>
        public static void TyreStack(RaidMesh m, System.Random rng, Vector3 at, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 p = at + new Vector3(R(rng, -0.04f, 0.04f), i * 0.22f, R(rng, -0.04f, 0.04f));
                m.Cylinder(p, 0.34f, 0.21f, 12, M(new Color(0.08f, 0.08f, 0.08f)));
                m.Cylinder(p + Vector3.up * 0.211f, 0.18f, 0.002f, 10, M(new Color(0.02f, 0.02f, 0.02f)));
            }
        }

        /// <summary>A wooden pallet (1.2 × 1.0), optionally loaded with boxes.</summary>
        public static void Pallet(RaidMesh m, System.Random rng, Vector3 at, float yaw, bool loaded)
        {
            Quaternion q = Yaw(yaw);
            Surf wood = DecorMaterials.Planks(V(Pine, rng, 0.08f));
            for (int i = 0; i < 3; i++) m.Box(at + q * new Vector3(0f, 0.05f, -0.42f + i * 0.42f), new Vector3(1.2f, 0.1f, 0.1f), q, wood);
            for (int i = 0; i < 6; i++) m.Box(at + q * new Vector3(-0.5f + i * 0.2f, 0.12f, 0f), new Vector3(0.14f, 0.025f, 1.0f), q, wood);
            if (!loaded) return;
            for (int i = 0; i < 4; i++)
                CardboardBox(m, rng, at + q * new Vector3(i % 2 == 0 ? -0.28f : 0.28f, 0.135f, i < 2 ? -0.22f : 0.22f), new Vector3(0.52f, 0.42f, 0.42f), yaw + R(rng, -5f, 5f));
            if (Chance(rng, 0.6))
                for (int i = 0; i < 2; i++)
                    CardboardBox(m, rng, at + q * new Vector3(i == 0 ? -0.26f : 0.26f, 0.555f, R(rng, -0.1f, 0.1f)), new Vector3(0.5f, 0.38f, 0.42f), yaw + R(rng, -8f, 8f));
        }

        /// <summary>An olive jerry can standing on the floor.</summary>
        public static void JerryCan(RaidMesh m, System.Random rng, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(Olive, rng, 0.08f);
            m.Box(at + q * new Vector3(0f, 0.24f, 0f), new Vector3(0.34f, 0.46f, 0.17f), q, M(c));
            m.Box(at + q * new Vector3(0f, 0.25f, 0.087f), new Vector3(0.3f, 0.025f, 0.006f), q * Quaternion.Euler(0f, 0f, 50f), M(c * 0.8f));
            m.Box(at + q * new Vector3(0f, 0.25f, 0.087f), new Vector3(0.3f, 0.025f, 0.006f), q * Quaternion.Euler(0f, 0f, -50f), M(c * 0.8f));
            m.Box(at + q * new Vector3(0f, 0.5f, 0f), new Vector3(0.2f, 0.03f, 0.04f), q, M(c * 0.7f));
            m.Box(at + q * new Vector3(0.13f, 0.5f, 0f), new Vector3(0.05f, 0.05f, 0.05f), q, M(c * 0.6f));
            // pull‑handle on the front lip of the can
            Vector3 handlePos = at + q * new Vector3(0f, 0.58f, 0.095f) + new Vector3((float)rng.NextDouble() * 0.004f - 0.002f, 0f, 0f);
            m.Cylinder(handlePos, 0.006f, 0.012f, 8, Mt(Chrome));
            // recessed cap (slightly indented)
            Vector3 capPos = at + q * new Vector3(0f, 0.57f, 0f);
            m.Cylinder(capPos, 0.09f, 0.008f, 12, M(c * 0.9f));
            // small rust patch (randomised position & size)
            float rustW = 0.04f + (float)rng.NextDouble() * 0.02f;
            float rustH = 0.02f + (float)rng.NextDouble() * 0.01f;
            Vector3 rustPos = at + q * new Vector3(0.12f, 0.22f, -0.08f) + new Vector3((float)rng.NextDouble() * 0.01f - 0.005f, (float)rng.NextDouble() * 0.005f - 0.0025f, 0f);
            Color rustCol = new Color(0.55f, 0.28f, 0.13f) * (0.9f + (float)rng.NextDouble() * 0.2f);
            m.Box(rustPos, new Vector3(rustW, rustH, 0.004f), M(rustCol));
            // thin darker stripe along the side seam
            Vector3 seamPos = at + q * new Vector3(0f, 0.24f, -0.085f);
            m.Box(seamPos, new Vector3(0.34f, 0.46f, 0.002f), M(c * 0.8f));
            // front label (random variant)
            int labelKind = rng.Next(0, 4);
            Vector3 labelPos = at + q * new Vector3(0f, 0.38f, 0.09f) + new Vector3((float)rng.NextDouble() * 0.006f - 0.003f, 0f, 0f);
            m.Quad(labelPos, new Vector3(0.12f, 0f, 0f), new Vector3(0f, 0.06f, 0f), DecorMaterials.Poster(labelKind));
        }

        /// <summary>A soft-drink vending machine with a glowing panel (0.9 × 0.8 × 1.85).</summary>
        public static void VendingMachine(RaidMesh m, System.Random rng, bool lit)
        {
            Color body = Chance(rng, 0.5) ? new Color(0.62f, 0.10f, 0.10f) : new Color(0.12f, 0.24f, 0.48f);
            m.Box(new Vector3(0f, 0.925f, -0.02f), new Vector3(0.9f, 1.85f, 0.76f), G(body));
            m.Box(new Vector3(-0.1f, 1.1f, 0.365f), new Vector3(0.6f, 1.2f, 0.01f), lit ? Gl(new Color(0.85f, 0.82f, 0.75f)) : M(new Color(0.6f, 0.58f, 0.55f)));
            // Coin slot – thin black rectangle with slight random offset
            Vector3 slotPos = new Vector3(-0.14f + (float)rng.NextDouble()*0.02f, 1.02f + (float)rng.NextDouble()*0.04f, 0.37f);
            m.Box(slotPos, new Vector3(0.04f, 0.02f, 0.004f), M(Black));
            // Paper‑receipt slot – light‑grey slit with random hue shift
            Vector3 receiptPos = new Vector3(0.12f + (float)rng.NextDouble()*0.01f, 1.14f + (float)rng.NextDouble()*0.02f, 0.37f);
            m.Box(receiptPos, new Vector3(0.08f, 0.004f, 0.006f), M(new Color(0.55f, 0.55f, 0.58f)));
            // Chrome knob for the service door
            Vector3 knobPos = new Vector3(0.28f + (float)rng.NextDouble()*0.005f, 1.10f, 0.36f);
            m.Cylinder(knobPos, 0.01f, 0.004f, 8, Mt(Chrome));
            // Button panel – three chrome push‑buttons with random offsets
            for (int i = 0; i < 3; i++)
            {
                float y = 1.06f + i * 0.03f + (float)rng.NextDouble()*0.005f;
                Vector3 btnPos = new Vector3(-0.2f + (float)rng.NextDouble()*0.01f, y, 0.37f);
                m.Box(btnPos, new Vector3(0.018f, 0.008f, 0.004f), Mt(Chrome));
            }
            // LED status display – subtle emissive green strip
            Vector3 dispPos = new Vector3(-0.05f + (float)rng.NextDouble()*0.02f, 1.12f, 0.368f);
            m.Box(dispPos, new Vector3(0.12f, 0.015f, 0.002f), Gl(new Color(0.25f, 0.30f, 0.12f)));
            // Brand label – small poster quad with random variant
            int labelKind = rng.Next(0, 5);
            Vector3 labelPos = new Vector3(0f + (float)rng.NextDouble()*0.01f, 1.04f, 0.369f);
            m.Quad(labelPos, new Vector3(0.18f, 0f, 0f), new Vector3(0f, 0.12f, 0f), DecorMaterials.Poster(labelKind));
            m.Box(new Vector3(0.33f, 1.1f, 0.37f), new Vector3(0.14f, 0.8f, 0.02f), M(new Color(0.2f, 0.2f, 0.22f)));
            for (int i = 0; i < 6; i++) m.Box(new Vector3(0.33f, 0.8f + i * 0.11f, 0.385f), new Vector3(0.08f, 0.06f, 0.02f), M(new Color(0.8f, 0.8f, 0.76f)));
            m.Box(new Vector3(-0.1f, 0.3f, 0.37f), new Vector3(0.5f, 0.16f, 0.02f), M(Black));
        }

        /// <summary>A bottled-water cooler.</summary>
        public static void WaterCooler(RaidMesh m, System.Random rng)
        {
            m.Box(new Vector3(0f, 0.47f, 0f), new Vector3(0.32f, 0.94f, 0.32f), G(V(Beige, rng, 0.05f)));
            m.Box(new Vector3(0f, 0.75f, 0.162f), new Vector3(0.16f, 0.1f, 0.01f), M(new Color(0.15f, 0.15f, 0.16f)));
            m.Cylinder(new Vector3(0f, 0.94f, 0f), 0.13f, 0.42f, 10, Gs(new Color(0.42f, 0.58f, 0.72f)), Quaternion.identity, 0.6f);
        }

        /// <summary>A beige photocopier.</summary>
        public static void Copier(RaidMesh m, System.Random rng)
        {
            Color c = V(Beige, rng, 0.04f);
            m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.6f), M(c * 0.92f));
            m.Box(new Vector3(0f, 0.97f, 0f), new Vector3(0.86f, 0.14f, 0.58f), M(c));
            m.Box(new Vector3(0f, 1.05f, -0.02f), new Vector3(0.8f, 0.03f, 0.52f), M(new Color(0.25f, 0.25f, 0.27f)));
            m.Box(new Vector3(-0.5f, 0.75f, 0f), new Vector3(0.14f, 0.02f, 0.36f), M(Paper));
    m.Box(new Vector3(0.3f, 1.0f, 0.3f), new Vector3(0.2f, 0.05f, 0.03f), Gl(new Color(0.2f, 0.5f, 0.3f)));
    Vector3 btnPos = new Vector3(0f, 1.0f, 0.31f) + new Vector3((float)rng.NextDouble()*0.01f-0.005f, (float)rng.NextDouble()*0.01f-0.005f, 0f);
    m.Box(btnPos, new Vector3(0.02f, 0.02f, 0.004f), Mt(Chrome));
}

        /// <summary>A dead pot plant: clay pot, dry soil and drooping brown stalks.</summary>
        public static void DeadPlant(RaidMesh m, System.Random rng, Vector3 at)
        {
            m.Cylinder(at, 0.17f, 0.36f, 10, M(new Color(0.55f, 0.30f, 0.18f)), Quaternion.identity, 1.25f);
            m.Cylinder(at + Vector3.up * 0.33f, 0.2f, 0.01f, 10, M(new Color(0.18f, 0.13f, 0.09f)));
            int stalks = rng.Next(4, 8);
            for (int i = 0; i < stalks; i++)
            {
                float a = R(rng, 0f, Mathf.PI * 2f);
                Vector3 tip = at + new Vector3(Mathf.Sin(a) * R(rng, 0.15f, 0.4f), R(rng, 0.6f, 1.1f), Mathf.Cos(a) * R(rng, 0.15f, 0.4f));
                m.Rod(at + Vector3.up * 0.34f, tip, 0.015f, M(new Color(0.36f, 0.30f, 0.16f)));
                m.Diamond(tip, new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a)) * 0.06f, new Vector3(0f, -0.12f, 0f), M(new Color(0.42f, 0.34f, 0.18f)));
            }
        }
        #endregion

        #region Bathrooms
        /// <summary>A toilet (tank against the wall at -Z).</summary>
        public static void Toilet(RaidMesh m, System.Random rng)
        {
            Color w = new Color(0.86f, 0.86f, 0.84f);
            m.Box(new Vector3(0f, 0.6f, -0.24f), new Vector3(0.44f, 0.36f, 0.18f), G(w));
            // thin rubber gasket – sits just below the tank
            m.Box(new Vector3(0f, 0.58f, -0.24f), new Vector3(0.44f, 0.02f, 0.18f), M(Color.black));
            m.Cylinder(new Vector3(0f, 0f, 0.02f), 0.15f, 0.3f, 10, G(w), Quaternion.identity, 1.25f);
            Vector3 handlePos = new Vector3(0.12f, 0.45f, 0.04f) + new Vector3((float)rng.NextDouble() * 0.01f - 0.005f, (float)rng.NextDouble() * 0.02f - 0.01f, 0f);
            Vector3 handleSize = new Vector3(0.02f, 0.04f, 0.01f);
            Quaternion handleRot = Quaternion.Euler(0f, (float)rng.NextDouble() * 10f - 5f, 0f);
            m.Box(handlePos, handleSize, handleRot, Mt(Chrome));
            // left bracket for paper roll holder
            Vector3 leftBracket = new Vector3(-0.18f, 0.55f, -0.30f) + new Vector3((float)rng.NextDouble()*0.004f-0.002f,
                (float)rng.NextDouble()*0.008f-0.004f,
                (float)rng.NextDouble()*0.004f-0.002f);
            m.Box(leftBracket, new Vector3(0.02f, 0.20f, 0.01f), Mt(Chrome));
            // right bracket for paper roll holder
            Vector3 rightBracket = new Vector3(0.18f, 0.55f, -0.30f) + new Vector3((float)rng.NextDouble()*0.004f-0.002f,
                (float)rng.NextDouble()*0.008f-0.004f,
                (float)rng.NextDouble()*0.004f-0.002f);
            m.Box(rightBracket, new Vector3(0.02f, 0.20f, 0.01f), Mt(Chrome));
            // cardboard roll – light‑grey cylinder
            Vector3 rollPos = new Vector3(0f, 0.58f, -0.32f);
            m.Cylinder(rollPos, 0.03f, 0.12f, 8, M(new Color(0.78f, 0.78f, 0.76f)));
                        // flush button plate – centred on front edge of tank
            Vector3 btnPos = new Vector3(0f, 0.44f, -0.04f) + new Vector3((float)rng.NextDouble()*0.004f-0.002f,
                (float)rng.NextDouble()*0.006f-0.003f,
                0f);
            // optional slight yaw for visual variation
            Quaternion btnRot = Quaternion.Euler(0f, (float)rng.NextDouble()*6f-3f, 0f);
            m.Box(btnPos, new Vector3(0.06f, 0.018f, 0.004f), btnRot, Mt(Chrome));
            // tiny knob on the button
            Vector3 knobPos = btnPos + new Vector3(0f, 0.01f, 0f);
            m.Cylinder(knobPos, 0.006f, 0.003f, 6, Mt(Chrome));
            m.Ellipsoid(new Vector3(0f, 0.41f, 0.06f), new Vector3(0.24f, 0.02f, 0.3f), M(new Color(0.3f, 0.32f, 0.3f)), 8, 3);
        }

        /// <summary>A pedestal wash basin with a mirror cabinet drawn above it.</summary>
        public static void WashBasin(RaidMesh m, System.Random rng)
        {
            Color w = new Color(0.86f, 0.86f, 0.84f);
            m.Cylinder(new Vector3(0f, 0f, -0.12f), 0.09f, 0.78f, 8, G(w));
            m.Box(new Vector3(0f, 0.82f, -0.06f), new Vector3(0.55f, 0.12f, 0.42f), G(w));
            m.Box(new Vector3(0f, 0.885f, -0.04f), new Vector3(0.4f, 0.004f, 0.28f), G(new Color(0.55f, 0.58f, 0.58f)));
            m.Box(new Vector3(0f, 0.95f, -0.22f), new Vector3(0.03f, 0.12f, 0.03f), Mt(Chrome));
            m.Box(new Vector3(0f, 1.45f, -0.25f), new Vector3(0.5f, 0.6f, 0.06f), Gs(new Color(0.42f, 0.46f, 0.48f)));
            // Chrome faucet with tiny red (hot) and blue (cold) lever handles
            Vector3 fp = new Vector3(-0.20f, 0.84f, -0.02f) + new Vector3((float)rng.NextDouble()*0.01f-0.005f, (float)rng.NextDouble()*0.02f-0.01f, 0f);
            Vector3 fs = new Vector3(0.04f, 0.02f, 0.02f);
            m.Box(fp, fs, Mt(Chrome));
            m.Box(fp + new Vector3(0.02f, 0.02f, 0f), new Vector3(0.008f, 0.012f, 0.008f), M(new Color(0.80f, 0.10f, 0.10f)));
            m.Box(fp + new Vector3(-0.02f, 0.02f, 0f), new Vector3(0.008f, 0.012f, 0.008f), M(new Color(0.10f, 0.30f, 0.80f)));
        }

        /// <summary>A white bath tub (0.75 × 1.6, length along X).</summary>
        public static void Bathtub(RaidMesh m, System.Random rng)
        {
            Color w = new Color(0.86f, 0.86f, 0.84f);
            m.Box(new Vector3(0f, 0.27f, 0f), new Vector3(1.6f, 0.54f, 0.75f), G(w));
            m.Box(new Vector3(0f, 0.545f, 0f), new Vector3(1.44f, 0.004f, 0.58f), G(new Color(0.6f, 0.62f, 0.6f)));
            m.Box(new Vector3(-0.7f, 0.66f, 0f), new Vector3(0.04f, 0.12f, 0.04f), Mt(Chrome));
            // Chrome faucet with hot (red) and cold (blue) levers
            m.Box(new Vector3(-0.6f, 0.66f, 0.35f), new Vector3(0.06f, 0.02f, 0.03f), Mt(Chrome));
            m.Box(new Vector3(-0.58f, 0.68f, 0.352f), new Vector3(0.008f, 0.012f, 0.008f), M(new Color(0.80f, 0.10f, 0.10f)));
            m.Box(new Vector3(-0.62f, 0.68f, 0.352f), new Vector3(0.008f, 0.012f, 0.008f), M(new Color(0.10f, 0.30f, 0.80f)));
        }
        #endregion
    }
}
