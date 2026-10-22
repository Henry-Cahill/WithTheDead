using UnityEngine;
using Surf = CloneZ.Surf;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        #region Photo 3: office desk kit, seating, cabinets, barrels
        /// <summary>A beige 1990s CRT monitor (deep tapered housing, dark glass) on a swivel foot,
        /// standing at <paramref name="at"/>. A powered screen glows a dim green.</summary>
        public static void CrtMonitor(RaidMesh m, System.Random rng, Vector3 at, float yaw, bool on)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(Beige, rng, 0.04f);
            m.Box(at + q * new Vector3(0f, 0.015f, -0.02f), new Vector3(0.24f, 0.03f, 0.22f), q, M(c * 0.9f));
            m.Box(at + q * new Vector3(0f, 0.05f, -0.02f), new Vector3(0.1f, 0.05f, 0.1f), q, M(c * 0.85f));
            m.Box(at + q * new Vector3(0f, 0.26f, 0.02f), new Vector3(0.4f, 0.36f, 0.1f), q, M(c));
            // metal grill frame (chrome)
            m.Box(at + q * new Vector3(0f, 0.32f, 0.07f), new Vector3(0.34f, 0.02f, 0.002f), q, Mt(Chrome));
            // vertical grill slats (chrome)
            float[] slatX = { -0.12f, -0.06f, 0f, 0.06f, 0.12f };
            foreach (float sx in slatX)
                m.Box(at + q * new Vector3(sx, 0.32f, 0.07f), new Vector3(0.004f, 0.018f, 0.001f), q, Mt(Chrome));
            // rubber foot pad (dark grey, non‑reflective)
            m.Cylinder(at + q * new Vector3(0f, 0.00f, -0.02f), 0.04f, 0.005f, 12, M(new Color(0.12f,0.12f,0.13f)));
            // power cord (black PVC)
            Vector3 cordBase = at + q * new Vector3(0f, 0.015f, -0.15f);
            m.Cylinder(cordBase, 0.005f, 0.30f, 8, M(Black));
            // plug at cord end
            Vector3 plugPos = cordBase + new Vector3(0f, -0.30f, 0f);
            m.Box(plugPos, new Vector3(0.02f, 0.01f, 0.015f), M(Black));
            m.Box(at + q * new Vector3(-0.15f, 0.30f, 0.07f), new Vector3(0.01f, 0.005f, 0.004f), q, Mt(Chrome));
            m.Box(at + q * new Vector3(0f, 0.25f, -0.13f), new Vector3(0.32f, 0.29f, 0.18f), q, M(c * 0.93f));
            m.Box(at + q * new Vector3(0f, 0.24f, -0.25f), new Vector3(0.2f, 0.18f, 0.08f), q, M(c * 0.88f));
            m.Box(at + q * new Vector3(0f, 0.27f, 0.072f), new Vector3(0.32f, 0.26f, 0.006f), q, on ? Gl(ScreenOn) : Gs(ScreenOff));
            m.Box(at + q * new Vector3(0.15f, 0.1f, 0.072f), new Vector3(0.03f, 0.015f, 0.006f), q, on ? Gl(new Color(0.2f, 0.8f, 0.3f)) : M(Black));
        }

        /// <summary>A beige keyboard lying in front of a monitor.</summary>
        public static void Keyboard(RaidMesh m, System.Random rng, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            m.Box(at + q * new Vector3(0f, 0.012f, 0f), new Vector3(0.44f, 0.024f, 0.16f), q, M(V(Beige, rng, 0.04f)));
            m.Box(at + q * new Vector3(-0.02f, 0.025f, 0.005f), new Vector3(0.36f, 0.004f, 0.11f), q, M(new Color(0.5f, 0.48f, 0.43f)));
        }

        /// <summary>An articulated desk lamp: weighted base, two spring-loaded arms and a conical shade
        /// tipped toward the desk. Returns the bulb position in the mesh's local frame.</summary>
        public static Vector3 DeskLamp(RaidMesh m, System.Random rng, Vector3 at, float yaw, Color shade, bool lit)
        {
            Quaternion q = Yaw(yaw);
            Color arm = new Color(0.16f, 0.16f, 0.17f);
            m.Cylinder(at, 0.075f, 0.03f, 10, M(shade * 0.7f));
            Vector3 screwPos = at + new Vector3(R(rng, -0.002f, 0.002f), 0.03f, R(rng, -0.002f, 0.002f));
            m.Cylinder(screwPos, 0.008f, 0.004f, 6, Mt(Chrome));
            Vector3 j0 = at + new Vector3(0f, 0.03f, 0f);
            Vector3 j1 = at + q * new Vector3(0f, 0.36f, -0.1f);
            Vector3 j2 = at + q * new Vector3(0f, 0.44f, 0.22f);
            m.Rod(j0, j1, 0.018f, M(arm));
            m.Rod(j0 + q * new Vector3(0.025f, 0f, 0f), j1 + q * new Vector3(0.025f, 0f, 0f), 0.006f, Mt(Chrome));
            m.Rod(j1, j2, 0.016f, M(arm));
            m.Rod(j1 + q * new Vector3(-0.025f, 0f, 0f), j2 + q * new Vector3(-0.025f, 0f, 0f), 0.006f, Mt(Chrome));
            m.Ellipsoid(j1, new Vector3(0.035f, 0.035f, 0.035f), M(arm), 6, 4);
            // The shade points down and forward from the end of the arm, open at the mouth.
            Quaternion tilt = q * Quaternion.Euler(150f, 0f, 0f);
            Vector3 axis = tilt * Vector3.up;
            Vector3 mouth = j2 + axis * 0.15f;
            m.Cylinder(mouth, 0.08f, 0.15f, 10, G(shade), tilt * Quaternion.Euler(180f, 0f, 0f), 0.4f, false);
            m.Cylinder(j2 - axis * 0.01f, 0.034f, 0.012f, 8, G(shade * 0.8f), tilt);
            m.Ellipsoid(mouth - axis * 0.045f, new Vector3(0.06f, 0.06f, 0.06f), lit ? Gl(WarmGlow) : M(new Color(0.55f, 0.55f, 0.5f)), 6, 4);
            return mouth;
        }

        /// <summary>A shop stool: round padded seat on four splayed chrome legs with a foot ring.</summary>
        public static void Stool(RaidMesh m, System.Random rng, Color seat)
        {
            float h = R(rng, 0.58f, 0.66f);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                m.Rod(dir * 0.13f + Vector3.up * (h - 0.02f), dir * 0.21f, 0.022f, Mt(Chrome));
            }
            // Small chrome foot‑ring around the padded seat
            // radius matches the stool seat, thin band placed just below it
            Vector3 ringPos = new Vector3(0f, h - 0.025f, 0f);
            m.Cylinder(ringPos, 0.165f, 0.004f, 12, Mt(Chrome), Quaternion.Euler(90f, 0f, 0f));
            float ry = h * 0.4f, rr = 0.165f;
            for (int i = 0; i < 4; i++)
            {
                float a0 = (45f + i * 90f) * Mathf.Deg2Rad, a1 = (135f + i * 90f) * Mathf.Deg2Rad;
                m.Rod(new Vector3(Mathf.Sin(a0) * rr, ry, Mathf.Cos(a0) * rr), new Vector3(Mathf.Sin(a1) * rr, ry, Mathf.Cos(a1) * rr), 0.014f, Mt(Chrome));
            }
            m.Cylinder(new Vector3(0f, h - 0.03f, 0f), 0.15f, 0.03f, 10, M(DarkSteel));
            m.Cylinder(new Vector3(0f, h, 0f), 0.17f, 0.06f, 12, G(V(seat, rng, 0.06f)));
        }

        /// <summary>A swivel office chair: cross base on casters, gas post, padded seat and backrest.</summary>
        public static void OfficeChair(RaidMesh m, System.Random rng, Color fabric)
        {
            Color f = V(fabric, rng, 0.06f);
            Color b = new Color(0.12f, 0.12f, 0.13f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                m.Rod(new Vector3(0f, 0.07f, 0f), dir * 0.3f + Vector3.up * 0.05f, 0.04f, M(b));
                m.Ellipsoid(dir * 0.3f + Vector3.up * 0.03f, new Vector3(0.05f, 0.05f, 0.05f), M(b), 6, 3);
            }
            m.Cylinder(new Vector3(0f, 0.07f, 0f), 0.025f, 0.36f, 6, Mt(Chrome));
            m.Cylinder(new Vector3(0f, 0.43f, 0f), 0.012f, 0.008f, 6, Mt(Chrome));
            m.Box(new Vector3(0f, 0.46f, 0.02f), new Vector3(0.48f, 0.08f, 0.46f), M(f));
            // subtle stitching – two thin raised lines across the seat
            m.Box(new Vector3(0f, 0.46f + 0.02f, 0.02f), new Vector3(0.48f, 0.001f, 0.46f),
                M(V(f, rng, 0.02f)));
            m.Box(new Vector3(0f, 0.46f - 0.02f, 0.02f), new Vector3(0.48f, 0.001f, 0.46f),
                M(V(f, rng, 0.02f)));
            // four rivet heads at seat corners
            float hx = 0.24f; // half seat width
            float dz = 0.23f; // half seat depth
            Vector3[] rivetPos = {
                new Vector3(-hx, 0.46f, -dz),
                new Vector3( hx, 0.46f, -dz),
                new Vector3(-hx, 0.46f,  dz),
                new Vector3( hx, 0.46f,  dz)
            };
            foreach (var p in rivetPos)
                m.Cylinder(p, 0.003f, 0.01f, 6, Mt(Chrome));
            m.Box(new Vector3(0f, 0.48f, -0.2f), new Vector3(0.06f, 0.18f, 0.04f), M(b));
            // low‑profile metal brace on backrest
            m.Rod(new Vector3(-0.22f, 0.78f, -0.25f),
                new Vector3( 0.22f, 0.78f, -0.25f),
                0.004f, Mt(Chrome));
        }

        /// <summary>A plain wooden kitchen / school chair.</summary>
        public static void WoodChair(RaidMesh m, System.Random rng, Color wood)
        {
            Color c = V(wood, rng, 0.08f);
            m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.04f, 0.4f), M(c));
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 0.18f, z = (i < 2 ? -1f : 1f) * 0.17f;
                m.Box(new Vector3(x, 0.215f, z), new Vector3(0.035f, 0.43f, 0.035f), M(c * 0.9f));
            }
            m.Box(new Vector3(-0.18f, 0.7f, -0.18f), new Vector3(0.035f, 0.5f, 0.035f), M(c * 0.9f));
            m.Box(new Vector3(0.18f, 0.7f, -0.18f), new Vector3(0.035f, 0.5f, 0.035f), M(c * 0.9f));
            m.Box(new Vector3(0f, 0.86f, -0.18f), new Vector3(0.4f, 0.1f, 0.025f), M(c));
            m.Box(new Vector3(0f, 0.66f, -0.18f), new Vector3(0.36f, 0.05f, 0.02f), M(c));
        }

        /// <summary>A steel filing cabinet (0.46 wide, 0.62 deep) with labelled drawers; one may be
        /// pulled half out with folders showing.</summary>
        public static void FilingCabinet(RaidMesh m, System.Random rng, int drawers, Color body)
        {
            Color c = V(body, rng, 0.05f);
            float h = drawers * 0.33f + 0.05f;
            m.Box(new Vector3(0f, h * 0.5f, -0.02f), new Vector3(0.46f, h, 0.58f), M(c * 0.92f));
            if (Chance(rng, 0.25f))
            {
                float rx = R(rng, -0.15f, 0.15f);
                float ry = R(rng, 0.2f, h - 0.2f);
                Vector3 rustPos = new Vector3(rx, ry, -0.04f);
                Vector3 rustSize = new Vector3(0.08f, 0.04f, 0.004f);
                m.Box(rustPos, rustSize, M(new Color(0.55f, 0.33f, 0.28f)));
            }
            // Guard against zero‑drawer cabinets – rng.Next(0) would throw.
            int open = (drawers > 0 && Chance(rng, 0.4)) ? rng.Next(drawers) : -1;
            for (int i = 0; i < drawers; i++)
            {
                float y = 0.05f + i * 0.33f + 0.165f;
                float pull = i == open ? R(rng, 0.2f, 0.32f) : 0f;
                if (pull > 0f)
                {
                    m.Box(new Vector3(0f, y - 0.02f, 0.27f + pull * 0.5f - 0.02f), new Vector3(0.4f, 0.26f, pull + 0.04f), M(c * 0.8f));
                     for (int f = 0; f < 5; f++)
                     {
                          Surf folderMat = (f % 2 == 0) ? M(new Color(0.70f, 0.62f, 0.40f)) : M(Paper);
                         m.Box(new Vector3(R(rng, -0.12f, 0.12f), y + 0.09f, 0.27f + f * pull / 5f),
                               new Vector3(0.34f, R(rng, 0.03f, 0.08f), 0.01f),
                               Quaternion.Euler(R(rng, -15f, 15f), 0f, 0f),
                               folderMat);
                     }
                }
                m.Box(new Vector3(0f, y, 0.275f + pull), new Vector3(0.43f, 0.3f, 0.02f), M(c));
                // Small chrome handle on the drawer front
                // Adjusted length to make room for lock bar
                m.Rod(new Vector3(-0.06f, y + 0.15f, 0.295f + pull), new Vector3(0.06f, y + 0.15f, 0.295f + pull), 0.004f, Mt(Chrome));
                m.Box(new Vector3(0f, y + 0.02f, 0.29f + pull), new Vector3(0.14f, 0.03f, 0.025f), Mt(Chrome));
                // ----- metal lock bar ----- (only top drawer)
                if (i == drawers - 1) {
                    Vector3 lockPos = new Vector3(-0.12f, y + 0.15f, 0.295f + pull);
                    m.Box(lockPos, new Vector3(0.035f, 0.015f, 0.004f), Mt(Chrome));
                    Vector3 knobPos = lockPos + new Vector3(0f, 0.008f, 0.002f);
                    m.Cylinder(knobPos, 0.004f, 0.008f, 6, Mt(Chrome), Quaternion.Euler(90f,0f,0f));
                    // occasional rust streak near lock (10% chance)
                    if (Chance(rng, 0.1f)) {
                        Vector3 rustPos = lockPos + new Vector3(0.01f, 0.005f, 0f);
                        Vector3 rustSize = new Vector3(0.02f, 0.003f, 0.001f);
                        m.Box(rustPos, rustSize, M(new Color(0.55f, 0.33f, 0.28f)));
                }
                }
                // ----- subtle dent / wear patch on the top edge -----
                if (i == drawers - 1) {
                    float dentX = R(rng, -0.01f, 0.01f);
                    float dentZ = R(rng, -0.01f, 0.01f);
                    Vector3 dentPos = new Vector3(dentX, h - 0.01f, dentZ);
                    Color dentCol = DecorPalette.Vary(c, 0.05f, rng); // 5% brighter
                    m.Box(dentPos, new Vector3(0.06f, 0.01f, 0.008f), M(dentCol));
                }
                // ----- metal label plate (paper‑tinted) -----
                Vector3 platePos = new Vector3(0f, y + 0.20f, 0.295f + pull);
                m.Box(platePos, new Vector3(0.18f, 0.02f, 0.001f), M(Paper));
                m.Box(new Vector3(0f, y + 0.09f, 0.287f + pull), new Vector3(0.09f, 0.04f, 0.004f), M(Paper));
            }
        }

        /// <summary>A 55-gallon drum with rolling hoops and a lid (r 0.29, 0.88 high). Painted drums
        /// are given their colour; pass <paramref name="hazard"/> for a yellow-black warning band.</summary>
        public static void Barrel(RaidMesh m, System.Random rng, Vector3 at, Color paint, bool hazard = false)
        {
            Color c = V(paint, rng, 0.08f);
            m.Cylinder(at, 0.29f, 0.88f, 12, G(c));
            m.Cylinder(at + new Vector3(0f, 0.28f, 0f), 0.298f, 0.035f, 12, G(c * 0.85f));
            m.Cylinder(at + new Vector3(0f, 0.58f, 0f), 0.298f, 0.035f, 12, G(c * 0.85f));
            m.Cylinder(at + new Vector3(0f, 0.86f, 0f), 0.296f, 0.03f, 12, G(c * 0.75f));
            // Subtle worn metal band around the barrel
            m.Cylinder(at + new Vector3(0f, 0.44f, 0f), 0.298f, 0.018f, 12, DecorPalette.Metal(new Color(0.45f, 0.44f, 0.43f)));
            m.Cylinder(at + new Vector3(0f, 0.88f, 0f), 0.26f, 0.006f, 12, M(c * 0.6f));
            m.Cylinder(at + new Vector3(0.14f, 0.88f, 0.05f), 0.03f, 0.015f, 6, M(c * 0.5f));
            if (hazard) m.Cylinder(at + new Vector3(0f, 0.36f, 0f), 0.297f, 0.2f, 12, DecorMaterials.Hazard, Quaternion.identity, 1f, false);
            if (Chance(rng, 0.35))
            {
                // A rust bloom on one side.
                float a = R(rng, 0f, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                m.Blob(at + dir * 0.296f + Vector3.up * R(rng, 0.15f, 0.6f), Vector3.Cross(Vector3.up, dir) * -1f, Vector3.up, 0.12f, 0.16f, 8, 0.4f, rng, M(new Color(0.36f, 0.20f, 0.10f)));
            }
        }

        /// <summary>A nailed wooden crate (<paramref name="size"/> centred on <paramref name="at"/> at
        /// floor level) with edge battens; military crates are olive with a stencil band.</summary>
        public static void Crate(RaidMesh m, System.Random rng, Vector3 at, Vector3 size, float yaw, bool military)
        {
            Quaternion q = Yaw(yaw);
            Color wood = military ? V(new Color(0.32f, 0.35f, 0.21f), rng, 0.06f) : V(Pine, rng, 0.08f);
            Surf body = military ? M(wood) : DecorMaterials.Planks(wood);
            Vector3 c = at + new Vector3(0f, size.y * 0.5f, 0f);
            m.Box(c, size, q, body);
            Color bat = wood * 0.78f;
            float t = 0.045f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    m.Box(c + q * new Vector3(sx * (size.x * 0.5f - t * 0.4f), 0f, sz * (size.z * 0.5f - t * 0.4f)), new Vector3(t, size.y + 0.005f, t), q, M(bat));
            m.Box(c + q * new Vector3(0f, size.y * 0.5f - t * 0.4f, size.z * 0.5f - t * 0.4f), new Vector3(size.x, t, t), q, M(bat));
            m.Box(c + q * new Vector3(0f, size.y * 0.5f - t * 0.4f, -size.z * 0.5f + t * 0.4f), new Vector3(size.x, t, t), q, M(bat));
            // Subtle metal strap on the front face of the crate
            m.Box(c + q * new Vector3(0f, size.y * 0.5f, size.z * 0.5f - 0.015f), new Vector3(size.x * 0.8f, 0.02f, 0.018f), q, DecorPalette.Metal(new Color(0.45f, 0.44f, 0.43f)));

            if (military)
            {
                m.Box(c + q * new Vector3(0f, size.y * 0.1f, size.z * 0.5f + 0.003f), new Vector3(size.x * 0.6f, size.y * 0.14f, 0.004f), q, M(new Color(0.80f, 0.74f, 0.50f)));
                m.Box(c + q * new Vector3(-size.x * 0.5f - 0.01f, 0.05f, 0f), new Vector3(0.02f, 0.04f, size.z * 0.3f), q, M(Black));
                m.Box(c + q * new Vector3(size.x * 0.5f + 0.01f, 0.05f, 0f), new Vector3(0.02f, 0.04f, size.z * 0.3f), q, M(Black));
            }
        }

        /// <summary>A taped cardboard box (centre-bottom at <paramref name="at"/>); some stand open
        /// with their flaps up.</summary>
        public static void CardboardBox(RaidMesh m, System.Random rng, Vector3 at, Vector3 size, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(Cardboard, rng, 0.1f);
            Vector3 center = at + new Vector3(0f, size.y * 0.5f, 0f);
            m.Box(center, size, q, M(c));
            float top = size.y + 0.002f;
            if (Chance(rng, 0.3))
            {
                // Open flaps along the long edges, splayed outward.
                for (int s = -1; s <= 1; s += 2)
                {
                    Quaternion fq = q * Quaternion.Euler(s * R(rng, 100f, 140f), 0f, 0f);
                    Vector3 hinge = at + q * new Vector3(0f, size.y, s * size.z * 0.5f);
                    m.Box(hinge + fq * new Vector3(0f, size.z * 0.25f, 0f), new Vector3(size.x * 0.98f, size.z * 0.5f, 0.006f), fq, M(c * 0.95f));
                }
                m.Box(at + q * new Vector3(0f, size.y - 0.004f, 0f), new Vector3(size.x * 0.94f, 0.004f, size.z * 0.94f), q, M(c * 0.35f));
            }
            else
            {
                m.Box(at + q * new Vector3(0f, top, 0f), new Vector3(size.x * 1.001f, 0.004f, 0.06f), q, M(new Color(0.78f, 0.70f, 0.50f)));
                m.Box(at + q * new Vector3(0f, size.y * 0.5f, size.z * 0.5f + 0.002f), new Vector3(0.06f, size.y * 0.7f, 0.004f), q, M(new Color(0.78f, 0.70f, 0.50f)));
            }
            if (Chance(rng, 0.5))
                m.Box(center + q * new Vector3(R(rng, -0.1f, 0.1f) * size.x, size.y * 0.1f, size.z * 0.5f + 0.003f), new Vector3(0.12f, 0.08f, 0.004f), q, M(Paper));
        }

        /// <summary>A short stack of cardboard boxes, each a little skewed.</summary>
        public static void BoxStack(RaidMesh m, System.Random rng, Vector3 at, float yaw, int count)
        {
            float y = 0f;
            float w = R(rng, 0.42f, 0.6f), d = R(rng, 0.36f, 0.48f);
            for (int i = 0; i < count; i++)
            {
                float h = R(rng, 0.26f, 0.42f);
                Vector3 jitter = new Vector3(R(rng, -0.04f, 0.04f), 0f, R(rng, -0.04f, 0.04f));
                CardboardBox(m, rng, at + jitter + Vector3.up * y, new Vector3(w, h, d), yaw + R(rng, -9f, 9f));
                y += h;
                w *= R(rng, 0.82f, 1f);
                d *= R(rng, 0.85f, 1f);
            }
        }

        /// <summary>A worn patterned rug lying flat (fringed at the short ends).</summary>
        public static void Rug(RaidMesh m, System.Random rng, Vector3 at, float w, float d, float yaw, int variant)
        {
            m.FlatUV(at + new Vector3(0f, 0.028f, 0f), w, d, yaw, DecorMaterials.Rug(variant), new Rect(0f, 0f, 1f, 1f));
            Quaternion q = Yaw(yaw);
            Color fringe = new Color(0.80f, 0.76f, 0.64f);
            m.Flat(at + q * new Vector3(w * 0.5f + 0.03f, 0.027f, 0f), 0.06f, d * 0.96f, yaw, M(fringe));
            m.Flat(at + q * new Vector3(-w * 0.5f - 0.03f, 0.027f, 0f), 0.06f, d * 0.96f, yaw, M(fringe));
        }
        #endregion
    }
}
