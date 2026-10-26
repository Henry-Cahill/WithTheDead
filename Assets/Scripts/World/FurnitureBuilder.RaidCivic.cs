using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        // The raid town's workplace furniture made usable: the one-piece drawings of the filing cabinet, the locker bank,
        // the gun safe, the medicine cabinet, the tool bench, the crate, the office desk, the CRT monitor, the field radio,
        // the steel counter and the rest, each split into a fixed body (open where a door or a drawer goes, dark inside,
        // with something in there to find) and the parts that move — drawn shut, in the piece's own frame like every other
        // piece of furniture (front toward +Z, origin on the floor at its middle). The layout constants are shared with the
        // kit that animates them (RaidBuilding.Civic.cs).

        #region Filing cabinet
        internal const float FilingPlinth = 0.05f, FilingPitch = 0.33f;
        internal const float FilingFront = 0.285f;   // the drawer fronts' face

        /// <summary>The middle of filing drawer <paramref name="i"/> (0 = the bottom one).</summary>
        internal static float FilingDrawerY(int i) => FilingPlinth + i * FilingPitch + FilingPitch * 0.5f;

        /// <summary>A steel filing cabinet's carcass (0.46 wide, 0.62 deep, 0.33 m a drawer): open at the front and dark
        /// inside, with rails between the drawers (drawn by <see cref="FilingDrawer"/>) and now and then something left on
        /// top. Returns its paint, for the drawers.</summary>
        public static Color FilingCabinetBody(RaidMesh m, System.Random rng, int drawers, Color body)
        {
            Color c = V(body, rng, 0.05f);
            float h = drawers * FilingPitch + FilingPlinth;
            Surf shell = M(c * 0.92f);
            m.Box(new Vector3(0f, h * 0.5f, -0.30f), new Vector3(0.46f, h, 0.02f), shell);
            m.Box(new Vector3(-0.22f, h * 0.5f, -0.02f), new Vector3(0.02f, h, 0.58f), shell);
            m.Box(new Vector3(0.22f, h * 0.5f, -0.02f), new Vector3(0.02f, h, 0.58f), shell);
            m.Box(new Vector3(0f, h - 0.01f, -0.02f), new Vector3(0.46f, 0.02f, 0.58f), shell);
            m.Box(new Vector3(0f, FilingPlinth * 0.5f, -0.02f), new Vector3(0.46f, FilingPlinth, 0.58f), M(c * 0.55f));
            m.Box(new Vector3(0f, (h + FilingPlinth) * 0.5f, -0.287f), new Vector3(0.42f, h - FilingPlinth - 0.02f, 0.006f), M(c * 0.3f));
            for (int i = 1; i < drawers; i++)
                m.Box(new Vector3(0f, FilingPlinth + i * FilingPitch, 0.255f), new Vector3(0.42f, 0.016f, 0.03f), shell);
            if (Chance(rng, 0.35))
                m.Box(new Vector3(R(rng, -0.08f, 0.08f), h + 0.03f, R(rng, -0.12f, 0.08f)), new Vector3(0.24f, 0.06f, 0.31f), Yaw(R(rng, -15f, 15f)), M(new Color(0.70f, 0.62f, 0.40f)));
            else if (Chance(rng, 0.3))
                m.Cylinder(new Vector3(R(rng, -0.1f, 0.1f), h, R(rng, -0.1f, 0.1f)), 0.04f, 0.1f, 8, G(new Color(R(rng, 0.5f, 0.9f), R(rng, 0.4f, 0.8f), R(rng, 0.3f, 0.7f))));
            return c;
        }

        /// <summary>Drawer <paramref name="i"/> (0 = the bottom one) of a filing cabinet, shut: its front with a pull and a
        /// card holder, and behind it the drawer itself, full of hanging files.</summary>
        public static void FilingDrawer(RaidMesh m, System.Random rng, int i, Color c)
        {
            float y = FilingDrawerY(i);
            m.Box(new Vector3(0f, y, 0.275f), new Vector3(0.43f, 0.3f, 0.02f), M(c));
            m.Box(new Vector3(0f, y + 0.02f, 0.29f), new Vector3(0.14f, 0.03f, 0.025f), Mt(Chrome));
            m.Box(new Vector3(0f, y + 0.09f, 0.287f), new Vector3(0.09f, 0.04f, 0.004f), M(Paper));
            Surf tray = M(c * 0.78f);
            const float hh = 0.22f, depth = 0.5f, zc = 0.26f - depth * 0.5f;
            float y0 = y - 0.13f;
            m.Box(new Vector3(-0.19f, y0 + hh * 0.5f, zc), new Vector3(0.01f, hh, depth), tray);
            m.Box(new Vector3(0.19f, y0 + hh * 0.5f, zc), new Vector3(0.01f, hh, depth), tray);
            m.Box(new Vector3(0f, y0 + 0.005f, zc), new Vector3(0.37f, 0.01f, depth), tray);
            m.Box(new Vector3(0f, y0 + hh * 0.5f, zc - depth * 0.5f + 0.005f), new Vector3(0.37f, hh, 0.01f), tray);
            int files = 4 + rng.Next(5);
            for (int f = 0; f < files; f++)
            {
                float z = zc - depth * 0.5f + 0.05f + (depth - 0.1f) * (f + 0.5f) / files;
                float fh = R(rng, 0.13f, 0.19f);
                Color fc = Chance(rng, 0.65) ? new Color(0.70f, 0.62f, 0.40f) : new Color(0.30f, 0.42f, 0.30f);
                m.Box(new Vector3(R(rng, -0.01f, 0.01f), y0 + 0.012f + fh * 0.5f, z), new Vector3(0.34f, fh, 0.008f), Quaternion.Euler(R(rng, -10f, 10f), 0f, 0f), M(fc));
                if (Chance(rng, 0.5)) m.Box(new Vector3(R(rng, -0.12f, 0.12f), y0 + 0.022f + fh, z), new Vector3(0.05f, 0.022f, 0.006f), M(Paper));
            }
        }
        #endregion

        #region Locker bank
        internal const float LockerPitch = 0.5f, LockerDoorY = 0.96f, LockerDoorH = 1.68f, LockerDoorFront = 0.225f;

        /// <summary>The middle of locker <paramref name="i"/> in a bank of <paramref name="count"/>.</summary>
        internal static float LockerX(int i, int count) => -count * LockerPitch * 0.5f + LockerPitch * (i + 0.5f);

        /// <summary>A bank of tall steel lockers without their doors (0.5 m a locker, 0.5 deep, 1.87 high): open compartments,
        /// dark inside, each with a hat shelf and a hook — and now and then a jacket, a pair of boots or a helmet left in
        /// one — the stiles between the doors, the cap and whatever's been stowed on top. The doors are drawn by
        /// <see cref="LockerDoor"/>. Returns the paint.</summary>
        public static Color LockerBankBody(RaidMesh m, System.Random rng, int count, Color body)
        {
            float w = count * LockerPitch;
            Color c = V(body, rng, 0.05f);
            Surf shell = M(c * 0.9f), inside = M(c * 0.32f), shelf = M(c * 0.7f);
            m.Box(new Vector3(0f, 0.04f, -0.02f), new Vector3(w, 0.08f, 0.44f), M(new Color(0.1f, 0.1f, 0.1f)));
            m.Box(new Vector3(0f, 0.96f, -0.24f), new Vector3(w, 1.76f, 0.02f), shell);
            m.Box(new Vector3(-w * 0.5f + 0.01f, 0.96f, -0.02f), new Vector3(0.02f, 1.76f, 0.46f), shell);
            m.Box(new Vector3(w * 0.5f - 0.01f, 0.96f, -0.02f), new Vector3(0.02f, 1.76f, 0.46f), shell);
            m.Box(new Vector3(0f, 0.09f, -0.02f), new Vector3(w, 0.02f, 0.46f), shell);
            m.Box(new Vector3(0f, 1.83f, -0.02f), new Vector3(w, 0.02f, 0.46f), shell);
            m.Box(new Vector3(0f, 1.855f, -0.02f), new Vector3(w + 0.02f, 0.03f, 0.48f), M(c * 0.8f));
            Color dark = c * 0.45f, boot = new Color(0.12f, 0.10f, 0.08f);
            for (int i = 0; i < count; i++)
            {
                float x = LockerX(i, count);
                if (i > 0)
                {
                    m.Box(new Vector3(x - LockerPitch * 0.5f, 0.96f, -0.02f), new Vector3(0.012f, 1.74f, 0.46f), shell);
                    m.Box(new Vector3(x - LockerPitch * 0.5f, 0.96f, 0.222f), new Vector3(0.012f, 1.7f, 0.02f), M(dark));
                }
                m.Box(new Vector3(x, 0.96f, -0.226f), new Vector3(0.46f, 1.72f, 0.006f), inside);
                m.Box(new Vector3(x, 1.5f, -0.02f), new Vector3(0.46f, 0.015f, 0.42f), shelf);
                m.Box(new Vector3(x, 1.42f, -0.19f), new Vector3(0.02f, 0.02f, 0.07f), Mt(Chrome));
                int k = rng.Next(5);
                if (k < 2)
                    m.Box(new Vector3(x + R(rng, -0.02f, 0.02f), 1.08f, -0.13f), new Vector3(0.36f, 0.66f, 0.1f), Yaw(R(rng, -4f, 4f)),
                        M(V(k == 0 ? OliveDark : new Color(0.20f, 0.22f, 0.26f), rng, 0.1f)));
                if (Chance(rng, 0.4))
                {
                    m.Box(new Vector3(x - 0.08f, 0.17f, -0.05f), new Vector3(0.1f, 0.14f, 0.26f), M(boot));
                    m.Box(new Vector3(x + 0.07f, 0.17f, -0.04f), new Vector3(0.1f, 0.14f, 0.26f), Yaw(8f), M(boot));
                }
                if (Chance(rng, 0.35))
                {
                    if (Chance(rng, 0.5)) m.Ellipsoid(new Vector3(x, 1.58f, -0.04f), new Vector3(0.26f, 0.15f, 0.3f), M(OliveDark), 8, 4);
                    else CardboardBox(m, rng, new Vector3(x, 1.508f, -0.03f), new Vector3(0.32f, 0.12f, 0.2f), R(rng, -8f, 8f));
                }
            }
            if (Chance(rng, 0.7))
            {
                float x = R(rng, -w * 0.3f, w * 0.3f);
                int k = rng.Next(3);
                if (k == 0) m.Ellipsoid(new Vector3(x, 1.94f, 0f), new Vector3(0.3f, 0.16f, 0.34f), M(OliveDark), 8, 4);
                else if (k == 1) CardboardBox(m, rng, new Vector3(x, 1.87f, -0.02f), new Vector3(0.42f, 0.28f, 0.36f), R(rng, -10f, 10f));
                else m.CylinderX(new Vector3(x, 1.95f, -0.02f), 0.09f, 0.5f, 8, M(Olive));
            }
            return c;
        }

        /// <summary>Locker <paramref name="i"/>'s door, shut: louvred top and bottom, a lift handle by its free edge, a number
        /// plate, and on its inside, now and then, a photo taped up. It hinges on its left edge.</summary>
        public static void LockerDoor(RaidMesh m, System.Random rng, int i, int count, Color c)
        {
            float x = LockerX(i, count);
            Color dark = c * 0.45f;
            m.Box(new Vector3(x, LockerDoorY, 0.215f), new Vector3(0.46f, LockerDoorH, 0.02f), M(c));
            for (int v = 0; v < 4; v++)
            {
                m.Box(new Vector3(x, 1.52f + v * 0.045f, 0.227f), new Vector3(0.26f, 0.014f, 0.006f), M(dark));
                m.Box(new Vector3(x, 0.24f + v * 0.045f, 0.227f), new Vector3(0.26f, 0.014f, 0.006f), M(dark));
            }
            m.Box(new Vector3(x + 0.16f, 1.0f, 0.235f), new Vector3(0.035f, 0.13f, 0.025f), Mt(Chrome));
            m.Box(new Vector3(x, 1.74f, 0.228f), new Vector3(0.07f, 0.035f, 0.006f), M(Paper));
            if (Chance(rng, 0.3))
                m.Box(new Vector3(x + R(rng, -0.06f, 0.06f), R(rng, 1.25f, 1.45f), 0.203f), new Vector3(0.1f, 0.14f, 0.003f),
                    Quaternion.Euler(0f, 0f, R(rng, -10f, 10f)), M(V(new Color(0.75f, 0.70f, 0.60f), rng, 0.15f)));
        }
        #endregion

        #region Gun safe
        internal const float SafeDoorFront = 0.29f, SafeHingeX = -0.36f;
        internal static readonly Vector3 SafeDial = new Vector3(-0.12f, 1.25f, 0.32f);

        /// <summary>A heavy gun safe without its door (0.8 wide, 0.6 deep, 1.9 high): thick steel walls round a felt-lined
        /// inside, long guns standing in a rack, shelves of ammunition and a pistol case, and the hinge knuckles on the left.
        /// The door is drawn by <see cref="GunSafeDoor"/>. Returns the paint.</summary>
        public static Color GunSafeBody(RaidMesh m, System.Random rng)
        {
            Color c = V(new Color(0.20f, 0.23f, 0.21f), rng, 0.05f);
            Surf steel = M(c), felt = M(new Color(0.24f, 0.08f, 0.07f));
            const float t = 0.05f, back = -0.30f, front = 0.25f;
            const float zc = (back + front) * 0.5f, d = front - back;
            m.Box(new Vector3(0f, 0.95f, back + t * 0.5f), new Vector3(0.8f, 1.9f, t), steel);
            m.Box(new Vector3(-0.4f + t * 0.5f, 0.95f, zc), new Vector3(t, 1.9f, d), steel);
            m.Box(new Vector3(0.4f - t * 0.5f, 0.95f, zc), new Vector3(t, 1.9f, d), steel);
            m.Box(new Vector3(0f, 1.9f - t * 0.5f, zc), new Vector3(0.8f, t, d), steel);
            m.Box(new Vector3(0f, t * 0.5f, zc), new Vector3(0.8f, t, d), steel);
            m.Box(new Vector3(0f, 0.95f, back + t + 0.003f), new Vector3(0.7f, 1.8f, 0.006f), felt);
            m.Box(new Vector3(0f, t + 0.003f, zc + t * 0.5f), new Vector3(0.7f, 0.006f, d - t), felt);
            for (int i = 0; i < 2; i++)
                m.Cylinder(new Vector3(-0.405f, 0.35f + i * 1.1f, 0.27f), 0.02f, 0.14f, 8, Mt(c * 1.3f));

            // The rack on the left: a notched sill for the butts, a bar for the barrels, and a few long guns in it.
            Surf wood = M(new Color(0.30f, 0.20f, 0.11f)), gun = Mt(new Color(0.12f, 0.12f, 0.13f));
            m.Box(new Vector3(-0.12f, t + 0.03f, -0.02f), new Vector3(0.44f, 0.06f, 0.12f), wood);
            m.Box(new Vector3(-0.12f, 1.2f, -0.19f), new Vector3(0.44f, 0.04f, 0.06f), wood);
            int guns = 2 + rng.Next(3);
            for (int i = 0; i < guns; i++)
            {
                float x = -0.29f + i * 0.11f;
                Quaternion lean = Quaternion.Euler(-9f, 0f, R(rng, -2f, 2f));
                m.Box(new Vector3(x, 0.24f, -0.03f), new Vector3(0.06f, 0.24f, 0.11f), lean, M(new Color(0.30f, 0.20f, 0.11f)));
                m.Box(new Vector3(x, 0.8f, -0.1f), new Vector3(0.04f, 0.9f, 0.05f), lean, gun);
                m.Box(new Vector3(x, 0.62f, -0.06f), new Vector3(0.035f, 0.16f, 0.09f), lean, gun);
            }

            // Shelves on the right with boxes of rounds and a pistol case; ammunition cans across the top shelf.
            for (int s = 0; s < 2; s++)
            {
                float y = 0.55f + s * 0.5f;
                m.Box(new Vector3(0.235f, y, zc + t * 0.5f), new Vector3(0.22f, 0.02f, d - t), M(c * 0.8f));
                int boxes = 1 + rng.Next(3);
                for (int b = 0; b < boxes; b++)
                    m.Box(new Vector3(0.18f + b * 0.06f, y + 0.045f, R(rng, -0.12f, 0.05f)), new Vector3(0.05f, 0.07f, 0.1f), Yaw(R(rng, -10f, 10f)),
                        M(V(b % 2 == 0 ? new Color(0.62f, 0.52f, 0.26f) : new Color(0.20f, 0.36f, 0.22f), rng, 0.1f)));
            }
            m.Box(new Vector3(0.235f, 0.12f, -0.06f), new Vector3(0.2f, 0.09f, 0.3f), M(Black));
            m.Box(new Vector3(0f, 1.5f, zc + t * 0.5f), new Vector3(0.7f, 0.02f, d - t), M(c * 0.8f));
            for (int i = 0; i < 2; i++)
                if (Chance(rng, 0.75)) AmmoCan(m, rng, new Vector3(-0.17f + i * 0.34f, 1.51f, -0.04f), 90f + R(rng, -6f, 6f));
            return c;
        }

/// <summary>The gun safe's door, shut (it hinges on its left edge at <see cref="SafeHingeX"/>): a thick slab with a rear strike‑plate, a front key‑hole, a tiny status LED, and an interior serial‑number label.</summary>
        public static void GunSafeDoor(RaidMesh m, Color c)
        {
            m.Box(new Vector3(0f, 0.95f, 0.27f), new Vector3(0.72f, 1.8f, 0.04f), G(c * 1.15f));
            m.Box(new Vector3(0f, 0.95f, 0.243f), new Vector3(0.6f, 1.66f, 0.014f), M(c * 0.8f));
            for (int i = 0; i < 3; i++)
                m.CylinderX(new Vector3(0.345f, 0.45f + i * 0.5f, 0.258f), 0.016f, 0.05f, 8, Mt(Chrome));
            // rear strike‑plate, just behind the bolt‑work
            m.Box(new Vector3(0f, 1.78f, 0.295f), new Vector3(0.5f, 0.05f, 0.01f), M(Brass));
            // rear strike‑plate, just behind the bolt‑work
            m.Box(new Vector3(0f, 1.78f, 0.265f), new Vector3(0.45f, 0.04f, 0.006f), Mt(Chrome));
            // front key‑hole (simple cylinder cutout)
            m.Cylinder(new Vector3(0f, 1.55f, 0.31f), 0.008f, 0.02f, 12, M(Black));
            // tiny LED status light (emissive green)
            m.Box(new Vector3(0.31f, 1.78f, 0.292f), new Vector3(0.008f, 0.008f, 0.002f), Gl(new Color(0.1f, 0.85f, 0.1f)));
            // stamped serial‑number plate (paper texture)
            m.Box(new Vector3(0f, 0.97f, 0.29f), new Vector3(0.18f, 0.015f, 0.003f), M(Paper));
            m.Cylinder(new Vector3(0.12f, 1.05f, 0.29f), 0.09f, 0.03f, 10, Mt(Chrome), Quaternion.Euler(90f, 0f, 0f));
            for (int i = 0; i < 3; i++)
                m.Box(new Vector3(0.12f, 1.05f, 0.33f), new Vector3(0.24f, 0.02f, 0.02f), Quaternion.Euler(0f, 0f, i * 60f), Mt(Chrome));
            m.Cylinder(new Vector3(SafeDial.x, SafeDial.y, 0.29f), 0.05f, 0.03f, 10, M(Black), Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(SafeDial.x, SafeDial.y + 0.055f, 0.296f), new Vector3(0.008f, 0.016f, 0.006f), M(Paper));
        }
        #endregion

        #region Medicine cabinet
        internal const float MedDoorFront = 0.17f;
        internal const float MedUpperY0 = 0.90f, MedUpperY1 = 1.78f, MedLowerY0 = 0.08f, MedLowerY1 = 0.86f;

        /// <summary>A white steel medicine cabinet without its doors (0.9 × 0.42 × 1.8): shelves of boxes and bottles above a
        /// middle shelf, a cupboard of dressings below. Its doors — glazed above, solid below — are drawn by
        /// <see cref="MedCabinetDoor"/>. Returns the enamel.</summary>
        public static Color MedCabinetBody(RaidMesh m, System.Random rng)
        {
            Color c = V(new Color(0.86f, 0.87f, 0.86f), rng, 0.03f);
            Surf shell = G(c);
            m.Box(new Vector3(0f, 0.04f, -0.02f), new Vector3(0.86f, 0.08f, 0.34f), M(new Color(0.2f, 0.2f, 0.2f)));
            m.Box(new Vector3(0f, 0.94f, -0.2f), new Vector3(0.9f, 1.72f, 0.02f), shell);
            m.Box(new Vector3(-0.44f, 0.94f, -0.03f), new Vector3(0.02f, 1.72f, 0.36f), shell);
            m.Box(new Vector3(0.44f, 0.94f, -0.03f), new Vector3(0.02f, 1.72f, 0.36f), shell);
            m.Box(new Vector3(0f, 1.79f, -0.03f), new Vector3(0.9f, 0.02f, 0.36f), shell);
            m.Box(new Vector3(0f, 0.09f, -0.03f), new Vector3(0.86f, 0.02f, 0.36f), shell);
            m.Box(new Vector3(0f, 0.88f, -0.03f), new Vector3(0.86f, 0.03f, 0.36f), shell);
            m.Box(new Vector3(0f, 1.335f, -0.187f), new Vector3(0.86f, 0.88f, 0.006f), M(new Color(0.60f, 0.64f, 0.64f)));
            m.Box(new Vector3(0f, 0.485f, -0.187f), new Vector3(0.86f, 0.76f, 0.006f), M(c * 0.78f));
            for (int s = 0; s < 3; s++)
            {
                float y = 0.98f + s * 0.27f;
                m.Box(new Vector3(0f, y, -0.04f), new Vector3(0.86f, 0.015f, 0.3f), shell);
                m.Upright(new Vector3(0f, y + 0.1f, 0.07f), Vector3.forward, 0.8f, 0.18f, DecorMaterials.Stock(2), new Rect((float)rng.NextDouble() * 0.5f, 0f, 0.5f, 0.6f));
            }
            m.Box(new Vector3(0f, 0.48f, -0.04f), new Vector3(0.86f, 0.015f, 0.3f), shell);
            for (int i = 0; i < 4; i++)
            {
                if (!Chance(rng, 0.7)) continue;
                float x = -0.3f + i * 0.2f + R(rng, -0.03f, 0.03f);
                float y = i < 2 ? 0.1f : 0.488f;
                Vector3 size = new Vector3(R(rng, 0.12f, 0.18f), R(rng, 0.1f, 0.2f), R(rng, 0.14f, 0.22f));
                m.Box(new Vector3(x, y + size.y * 0.5f, R(rng, -0.08f, 0.0f)), size, Yaw(R(rng, -12f, 12f)), M(V(i % 2 == 0 ? Paper : new Color(0.62f, 0.72f, 0.80f), rng, 0.08f)));
            }
            return c;
        }

        /// <summary>One of the medicine cabinet's four doors, shut: a glazed upper one (a white frame round clear glass) or a
        /// solid lower one (the left one with a red cross). A <paramref name="left"/> door hinges on the cabinet's left side.</summary>
        public static void MedCabinetDoor(RaidMesh m, Color c, bool upper, bool left)
        {
            float x0 = left ? -0.45f : 0.005f, x1 = left ? -0.005f : 0.45f;
            float y0 = upper ? MedUpperY0 : MedLowerY0, y1 = upper ? MedUpperY1 : MedLowerY1;
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, w = x1 - x0, h = y1 - y0;
            const float z = MedDoorFront - 0.0125f;
            float free = left ? x1 - 0.035f : x0 + 0.035f;
            if (upper)
            {
                Surf frame = G(c);
                m.Box(new Vector3(x0 + 0.02f, cy, z), new Vector3(0.04f, h, 0.025f), frame);
                m.Box(new Vector3(x1 - 0.02f, cy, z), new Vector3(0.04f, h, 0.025f), frame);
                m.Box(new Vector3(cx, y1 - 0.02f, z), new Vector3(w - 0.08f, 0.04f, 0.025f), frame);
                m.Box(new Vector3(cx, y0 + 0.02f, z), new Vector3(w - 0.08f, 0.04f, 0.025f), frame);
                m.Box(new Vector3(cx, cy, z), new Vector3(w - 0.07f, h - 0.07f, 0.004f), KitchenParts.Water);
                m.Box(new Vector3(free, y0 + 0.1f, MedDoorFront + 0.006f), new Vector3(0.02f, 0.06f, 0.012f), Mt(Chrome));
                return;
            }
            m.Box(new Vector3(cx, cy, z), new Vector3(w, h, 0.02f), G(c * 0.97f));
            m.Box(new Vector3(free, 0.6f, MedDoorFront + 0.006f), new Vector3(0.02f, 0.1f, 0.012f), Mt(Chrome));
            if (!left) return;
            Color red = new Color(0.72f, 0.12f, 0.10f);
            m.Box(new Vector3(cx, 0.62f, MedDoorFront - 0.002f + 0.004f), new Vector3(0.14f, 0.04f, 0.004f), M(red));
            m.Box(new Vector3(cx, 0.62f, MedDoorFront - 0.002f + 0.004f), new Vector3(0.04f, 0.14f, 0.004f), M(red));
        }
        #endregion

        #region Tool bench
        internal const float BenchDrawerY = 0.7425f, BenchDrawerH = 0.19f, BenchDrawerW = 0.5f, BenchDrawerFront = 0.345f;
        internal static readonly float[] BenchDrawerX = { -0.565f, -0.035f };
        internal static readonly Vector3 BenchVice = new Vector3(0.68f, 1.0f, 0.34f);   // the fixed jaw's face, at its middle

        /// <summary>A heavy work bench (1.8 × 0.75, its pegboard of tools reaching 1.9) without its drawers: the worktop on
        /// steel legs, the lower shelf with a box on it, the drawer case under the worktop (two drawers, drawn by
        /// <see cref="BenchDrawer"/>), the vice's fixed jaw at the front corner (its moving jaw drawn by
        /// <see cref="BenchViceJaw"/>), a red toolbox and the odd tool lying about.</summary>
        public static void ToolBenchBody(RaidMesh m, System.Random rng)
        {
            Color top = V(new Color(0.48f, 0.34f, 0.20f), rng, 0.06f);
            Color legs = new Color(0.20f, 0.24f, 0.30f);
            m.Box(new Vector3(0f, 0.88f, 0f), new Vector3(1.8f, 0.07f, 0.75f), M(top));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(i % 2 == 0 ? -0.85f : 0.85f, 0.43f, i < 2 ? -0.32f : 0.32f), new Vector3(0.05f, 0.86f, 0.05f), M(legs));
            m.Box(new Vector3(0f, 0.18f, 0f), new Vector3(1.7f, 0.03f, 0.66f), M(top * 0.8f));

            // The drawer case: a steel box hung under the worktop, split in two.
            Surf steel = M(new Color(0.22f, 0.27f, 0.33f)), inside = M(new Color(0.08f, 0.09f, 0.10f));
            const float x0 = -0.83f, x1 = 0.23f, y0 = 0.64f, y1 = 0.845f, cz = 0.02f, depth = 0.63f;
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
            m.Box(new Vector3(cx, cy, cz - depth * 0.5f + 0.01f), new Vector3(x1 - x0, y1 - y0, 0.02f), steel);
            m.Box(new Vector3(cx, y0 + 0.01f, cz), new Vector3(x1 - x0, 0.02f, depth), steel);
            m.Box(new Vector3(x0 + 0.01f, cy, cz), new Vector3(0.02f, y1 - y0, depth), steel);
            m.Box(new Vector3(x1 - 0.01f, cy, cz), new Vector3(0.02f, y1 - y0, depth), steel);
            m.Box(new Vector3(-0.3f, cy, cz), new Vector3(0.02f, y1 - y0, depth), steel);
            m.Box(new Vector3(cx, cy, cz - depth * 0.5f + 0.022f), new Vector3(x1 - x0 - 0.04f, y1 - y0 - 0.04f, 0.004f), inside);

            // The pegboard and its tools.
            m.Box(new Vector3(0f, 1.42f, -0.35f), new Vector3(1.8f, 0.95f, 0.03f), M(new Color(0.55f, 0.42f, 0.28f)));
            for (int i = 0; i < 9; i++)
            {
                float x = -0.75f + i * 0.19f + R(rng, -0.03f, 0.03f);
                float hh = R(rng, 0.12f, 0.3f);
                m.Box(new Vector3(x, R(rng, 1.25f, 1.6f), -0.32f), new Vector3(R(rng, 0.03f, 0.06f), hh, 0.03f), Quaternion.Euler(0f, 0f, R(rng, -20f, 20f)), Mt(new Color(0.35f, 0.36f, 0.38f)));
            }

            // The vice: its base bolted down at the corner and the fixed jaw.
            Surf vice = G(new Color(0.20f, 0.28f, 0.42f)), jaw = Mt(new Color(0.30f, 0.31f, 0.33f));
            Vector3 v = BenchVice;
            m.Box(new Vector3(v.x, 0.93f, v.z - 0.1f), new Vector3(0.16f, 0.03f, 0.22f), vice);
            m.Box(new Vector3(v.x, 0.985f, v.z - 0.12f), new Vector3(0.12f, 0.08f, 0.2f), vice);
            m.Box(new Vector3(v.x, v.y, v.z - 0.027f), new Vector3(0.18f, 0.1f, 0.046f), vice);
            m.Box(new Vector3(v.x, v.y + 0.02f, v.z - 0.002f), new Vector3(0.17f, 0.04f, 0.004f), jaw);

            // A red toolbox and a few tools on the worktop; a box on the shelf below.
            m.Box(new Vector3(-0.5f, 0.96f, 0.0f), new Vector3(0.5f, 0.1f, 0.22f), G(new Color(0.65f, 0.12f, 0.1f)));
            m.Box(new Vector3(-0.5f, 1.03f, 0.0f), new Vector3(0.3f, 0.03f, 0.03f), M(Black));
            for (int i = 0; i < 4; i++)
                m.Box(new Vector3(R(rng, -0.2f, 0.45f), 0.93f, R(rng, -0.2f, 0.2f)), new Vector3(R(rng, 0.04f, 0.2f), 0.025f, 0.04f), Yaw(R(rng, 0f, 180f)), Mt(new Color(0.4f, 0.4f, 0.42f)));
            CardboardBox(m, rng, new Vector3(R(rng, -0.5f, 0.5f), 0.195f, 0f), new Vector3(0.5f, 0.3f, 0.4f), R(rng, -10f, 10f));
        }

        /// <summary>One of the tool bench's two drawers (<paramref name="i"/> 0 the left), shut: a steel front with a pull,
        /// and the tray behind it with sockets, screwdrivers and bits rattling about in it.</summary>
        public static void BenchDrawer(RaidMesh m, System.Random rng, int i)
        {
            float x = BenchDrawerX[i];
            Color steel = new Color(0.26f, 0.32f, 0.40f);
            m.Box(new Vector3(x, BenchDrawerY, BenchDrawerFront - 0.01f), new Vector3(BenchDrawerW - 0.01f, BenchDrawerH - 0.01f, 0.02f), M(steel));
            m.Box(new Vector3(x, BenchDrawerY + 0.03f, BenchDrawerFront + 0.012f), new Vector3(0.2f, 0.025f, 0.025f), Mt(Chrome));
            Surf tray = M(steel * 0.7f);
            const float depth = 0.56f, hh = 0.12f;
            float zc = BenchDrawerFront - 0.02f - depth * 0.5f, y0 = BenchDrawerY - 0.08f;
            m.Box(new Vector3(x - 0.235f, y0 + hh * 0.5f, zc), new Vector3(0.01f, hh, depth), tray);
            m.Box(new Vector3(x + 0.235f, y0 + hh * 0.5f, zc), new Vector3(0.01f, hh, depth), tray);
            m.Box(new Vector3(x, y0 + 0.005f, zc), new Vector3(0.46f, 0.01f, depth), tray);
            m.Box(new Vector3(x, y0 + hh * 0.5f, zc - depth * 0.5f + 0.005f), new Vector3(0.46f, hh, 0.01f), tray);
            int n = 3 + rng.Next(5);
            for (int k = 0; k < n; k++)
            {
                Vector3 p = new Vector3(x + R(rng, -0.18f, 0.18f), y0 + 0.02f, zc + R(rng, -0.22f, 0.22f));
                if (Chance(rng, 0.5)) m.Box(p, new Vector3(R(rng, 0.12f, 0.22f), 0.02f, 0.025f), Yaw(R(rng, 0f, 180f)), Mt(new Color(0.42f, 0.42f, 0.44f)));
                else m.Cylinder(p - Vector3.up * 0.01f, 0.018f, 0.035f, 8, Mt(Chrome));
            }
        }

        /// <summary>The vice's moving jaw with its screw and tommy bar, shut against the fixed jaw (it slides out along +Z).</summary>
        public static void BenchViceJaw(RaidMesh m)
        {
            Surf vice = G(new Color(0.20f, 0.28f, 0.42f)), jaw = Mt(new Color(0.30f, 0.31f, 0.33f)), bar = Mt(Chrome);
            Vector3 v = BenchVice;
            m.Box(new Vector3(v.x, v.y, v.z + 0.028f), new Vector3(0.18f, 0.1f, 0.048f), vice);
            m.Box(new Vector3(v.x, v.y + 0.02f, v.z + 0.002f), new Vector3(0.17f, 0.04f, 0.004f), jaw);
            m.Box(new Vector3(v.x, 0.955f, v.z - 0.06f), new Vector3(0.05f, 0.03f, 0.2f), vice);
            m.Rod(new Vector3(v.x, v.y, v.z + 0.05f), new Vector3(v.x, v.y, v.z + 0.13f), 0.022f, bar, 8);
            m.Rod(new Vector3(v.x - 0.09f, v.y, v.z + 0.13f), new Vector3(v.x + 0.09f, v.y, v.z + 0.13f), 0.014f, bar, 6);
            m.Ellipsoid(new Vector3(v.x - 0.095f, v.y, v.z + 0.13f), new Vector3(0.025f, 0.025f, 0.025f), bar, 6, 3);
            m.Ellipsoid(new Vector3(v.x + 0.095f, v.y, v.z + 0.13f), new Vector3(0.025f, 0.025f, 0.025f), bar, 6, 3);
        }
        #endregion

        #region Crates and footlockers
        internal const float CrateLidThick = 0.03f;

        /// <summary>What's packed in an open crate: nothing to see, straw round a box or two, olive ammunition cans, or a
        /// soldier's folded kit (a footlocker).</summary>
        internal enum CrateLoad { Empty, Straw, Ammo, Kit }

        /// <summary>A nailed crate without its lid (<paramref name="size"/> standing on <paramref name="at"/>, turned
        /// <paramref name="yaw"/>): its walls and floor, the corner battens, a military crate's stencil band and rope
        /// handles, and what's packed inside, which shows once the lid's up. Returns the wood, for <see cref="CrateLid"/>.</summary>
        public static Color CrateBox(RaidMesh m, System.Random rng, Vector3 at, Vector3 size, float yaw, bool military, CrateLoad load)
        {
            Quaternion q = Yaw(yaw);
            Color wood = military ? V(new Color(0.32f, 0.35f, 0.21f), rng, 0.06f) : V(Pine, rng, 0.08f);
            Surf body = military ? M(wood) : DecorMaterials.Planks(wood);
            const float t = 0.022f;
            float h = size.y - CrateLidThick, hx = size.x * 0.5f, hz = size.z * 0.5f;
            m.Box(at + q * new Vector3(0f, h * 0.5f, hz - t * 0.5f), new Vector3(size.x, h, t), q, body);
            m.Box(at + q * new Vector3(0f, h * 0.5f, -hz + t * 0.5f), new Vector3(size.x, h, t), q, body);
            m.Box(at + q * new Vector3(-hx + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, size.z - t * 2f), q, body);
            m.Box(at + q * new Vector3(hx - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, size.z - t * 2f), q, body);
            m.Box(at + q * new Vector3(0f, t * 0.5f, 0f), new Vector3(size.x - t * 2f, t, size.z - t * 2f), q, body);
            m.Box(at + q * new Vector3(0f, t + 0.002f, 0f), new Vector3(size.x - t * 2f - 0.004f, 0.004f, size.z - t * 2f - 0.004f), q, M(wood * 0.4f));
            Color bat = wood * 0.78f;
            const float b = 0.045f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    m.Box(at + q * new Vector3(sx * (hx - b * 0.4f), h * 0.5f, sz * (hz - b * 0.4f)), new Vector3(b, h + 0.004f, b), q, M(bat));
            if (military)
            {
                m.Box(at + q * new Vector3(0f, h * 0.55f, hz + 0.003f), new Vector3(size.x * 0.6f, Mathf.Min(0.08f, h * 0.16f), 0.004f), q, M(new Color(0.80f, 0.74f, 0.50f)));
                m.Box(at + q * new Vector3(-hx - 0.01f, h * 0.6f, 0f), new Vector3(0.02f, 0.04f, size.z * 0.3f), q, M(Black));
                m.Box(at + q * new Vector3(hx + 0.01f, h * 0.6f, 0f), new Vector3(0.02f, 0.04f, size.z * 0.3f), q, M(Black));
            }

            // The load, kept inside the walls and under the lid.
            float ix = hx - t - 0.01f, iz = hz - t - 0.01f, room = h - t - 0.03f;
            Vector3 floor = at + Vector3.up * (t + 0.004f);
            switch (load)
            {
                case CrateLoad.Straw:
                    m.Box(floor + q * new Vector3(0f, Mathf.Min(room, 0.12f) * 0.5f, 0f), new Vector3(ix * 2f, Mathf.Min(room, 0.12f), iz * 2f), q, M(new Color(0.66f, 0.56f, 0.30f)));
                    if (room > 0.22f)
                        CardboardBox(m, rng, floor + q * new Vector3(R(rng, -ix * 0.4f, ix * 0.4f), 0.06f, 0f), new Vector3(Mathf.Min(0.36f, ix * 1.2f), Mathf.Min(room - 0.06f, 0.24f), Mathf.Min(0.3f, iz * 1.4f)), yaw + R(rng, -15f, 15f));
                    break;
                case CrateLoad.Ammo:
                    if (room < 0.19f || ix < 0.16f) break;
                    int cans = Mathf.Clamp(Mathf.FloorToInt(ix * 2f / 0.17f), 1, 4);
                    for (int i = 0; i < cans; i++)
                        if (Chance(rng, 0.8))
                            AmmoCan(m, rng, floor + q * new Vector3(-ix + 0.085f + i * 0.17f, 0f, R(rng, -0.02f, 0.02f)), yaw + 90f + R(rng, -4f, 4f));
                    break;
                case CrateLoad.Kit:
                    m.Box(floor + q * new Vector3(-ix * 0.45f, 0.05f, 0f), new Vector3(ix * 0.9f, 0.1f, iz * 1.6f), q, M(V(OliveDark, rng, 0.1f)));
                    m.Box(floor + q * new Vector3(ix * 0.5f, 0.04f, -iz * 0.3f), new Vector3(ix * 0.8f, 0.08f, iz * 1.0f), q, M(V(new Color(0.62f, 0.60f, 0.52f), rng, 0.1f)));
                    if (Chance(rng, 0.5)) m.Box(floor + q * new Vector3(ix * 0.5f, 0.1f, iz * 0.55f), new Vector3(0.12f, 0.04f, 0.08f), q, M(new Color(0.82f, 0.80f, 0.74f)));
                    break;
            }
            return wood;
        }

        /// <summary>A crate's lid, shut on the box <see cref="CrateBox"/> drew: boards with two battens across, and on a
        /// military one a stencilled panel.</summary>
        public static void CrateLid(RaidMesh m, Vector3 at, Vector3 size, float yaw, Color wood, bool military)
        {
            Quaternion q = Yaw(yaw);
            Surf body = military ? M(wood) : DecorMaterials.Planks(wood);
            m.Box(at + q * new Vector3(0f, size.y - CrateLidThick * 0.5f, 0f), new Vector3(size.x, CrateLidThick, size.z), q, body);
            Color bat = wood * 0.78f;
            float bz = size.z * 0.5f - 0.035f;
            m.Box(at + q * new Vector3(0f, size.y + 0.011f, bz), new Vector3(size.x, 0.022f, 0.055f), q, M(bat));
            m.Box(at + q * new Vector3(0f, size.y + 0.011f, -bz), new Vector3(size.x, 0.022f, 0.055f), q, M(bat));
            if (military)
                m.Box(at + q * new Vector3(0f, size.y + 0.0015f, 0f), new Vector3(size.x * 0.45f, 0.003f, Mathf.Max(0.05f, size.z - 0.2f) * 0.5f), q, M(new Color(0.80f, 0.74f, 0.50f)));
        }
        #endregion

        #region Office desk and computer
        /// <summary>The middle of pedestal drawer <paramref name="row"/> of an office desk (0 = the top one).</summary>
        internal static float DeskDrawerY(int row) => 0.6f - row * 0.23f;

        /// <summary>An office desk without its drawers (width along X, 0.76 high; <see cref="Desk"/>'s look): the top, a drawer
        /// pedestal on one side — open-fronted and dark inside, with rails between its three drawers — a solid end panel on
        /// the other, a modesty panel at the back and the runners of the shallow pencil drawer under the top. The drawers are
        /// drawn by <see cref="DeskDrawer"/> and <see cref="DeskPencilDrawer"/>. Returns the wood.</summary>
        public static Color DeskBody(RaidMesh m, System.Random rng, float w, float d, Color wood, bool pedestalRight)
        {
            Color c = V(wood, rng, 0.06f);
            float side = pedestalRight ? 1f : -1f, px = side * (w * 0.5f - 0.22f);
            const float pz = -0.01f;
            float pd = d - 0.06f;
            Surf carcass = M(c);
            m.Box(new Vector3(0f, 0.74f, 0f), new Vector3(w, 0.04f, d), G(c * 1.08f));
            m.Box(new Vector3(px - 0.2f, 0.36f, pz), new Vector3(0.02f, 0.72f, pd), carcass);
            m.Box(new Vector3(px + 0.2f, 0.36f, pz), new Vector3(0.02f, 0.72f, pd), carcass);
            m.Box(new Vector3(px, 0.36f, pz - pd * 0.5f + 0.01f), new Vector3(0.42f, 0.72f, 0.02f), carcass);
            m.Box(new Vector3(px, 0.02f, pz), new Vector3(0.42f, 0.04f, pd), carcass);
            m.Box(new Vector3(px, 0.38f, pz - pd * 0.5f + 0.022f), new Vector3(0.38f, 0.66f, 0.004f), M(c * 0.35f));
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(px, 0.485f - i * 0.23f, d * 0.5f - 0.05f), new Vector3(0.38f, 0.02f, 0.02f), carcass);
            m.Box(new Vector3(-side * (w * 0.5f - 0.03f), 0.36f, -0.01f), new Vector3(0.04f, 0.72f, d - 0.06f), carcass);
            m.Box(new Vector3(-side * 0.21f, 0.5f, -d * 0.5f + 0.05f), new Vector3(w - 0.5f, 0.42f, 0.02f), M(c * 0.9f));
            float cx = -side * 0.21f, cw = w - 0.6f;
            m.Box(new Vector3(cx - cw * 0.5f - 0.015f, 0.69f, 0f), new Vector3(0.02f, 0.06f, d - 0.12f), M(c * 0.85f));
            m.Box(new Vector3(cx + cw * 0.5f + 0.015f, 0.69f, 0f), new Vector3(0.02f, 0.06f, d - 0.12f), M(c * 0.85f));
            return c;
        }

        /// <summary>Pedestal drawer <paramref name="row"/> of an office desk (0 the top), shut: a front with a brass pull, and the
        /// drawer behind it with papers, pens and the like — the deep bottom one with a few hanging files.</summary>
        public static void DeskDrawer(RaidMesh m, System.Random rng, float w, float d, Color c, bool pedestalRight, int row)
        {
            float side = pedestalRight ? 1f : -1f, px = side * (w * 0.5f - 0.22f), y = DeskDrawerY(row);
            float front = d * 0.5f - 0.025f;
            m.Box(new Vector3(px, y, front), new Vector3(0.38f, 0.2f, 0.02f), M(c * 1.12f));
            m.Box(new Vector3(px, y + 0.03f, d * 0.5f - 0.005f), new Vector3(0.1f, 0.018f, 0.02f), Mt(Brass));
            Surf box = M(c * 0.8f);
            float depth = d - 0.17f, hh = row == 2 ? 0.17f : 0.14f;
            float zc = front - 0.01f - depth * 0.5f, y0 = y - 0.09f;
            m.Box(new Vector3(px - 0.175f, y0 + hh * 0.5f, zc), new Vector3(0.01f, hh, depth), box);
            m.Box(new Vector3(px + 0.175f, y0 + hh * 0.5f, zc), new Vector3(0.01f, hh, depth), box);
            m.Box(new Vector3(px, y0 + 0.005f, zc), new Vector3(0.34f, 0.01f, depth), box);
            m.Box(new Vector3(px, y0 + hh * 0.5f, zc - depth * 0.5f + 0.005f), new Vector3(0.34f, hh, 0.01f), box);
            if (row == 2)
            {
                int files = 3 + rng.Next(4);
                for (int f = 0; f < files; f++)
                {
                    float z = zc - depth * 0.5f + 0.06f + (depth - 0.12f) * (f + 0.5f) / files;
                    m.Box(new Vector3(px, y0 + 0.012f + 0.065f, z), new Vector3(0.3f, 0.13f, 0.008f), Quaternion.Euler(R(rng, -10f, 10f), 0f, 0f),
                        M(Chance(rng, 0.6) ? new Color(0.70f, 0.62f, 0.40f) : new Color(0.62f, 0.30f, 0.24f)));
                }
                return;
            }
            int sheets = rng.Next(1, 4);
            for (int s = 0; s < sheets; s++)
                m.FlatUV(new Vector3(px + R(rng, -0.03f, 0.03f), y0 + 0.011f + s * 0.002f, zc + R(rng, -0.1f, 0.1f)), 0.21f, 0.28f, R(rng, -15f, 15f),
                    DecorMaterials.Paper(rng.Next(3)), new Rect(0f, 0f, 1f, 1f));
            if (Chance(rng, 0.7))
                m.Rod(new Vector3(px + R(rng, -0.12f, 0f), y0 + 0.02f, zc + R(rng, -0.15f, 0.15f)), new Vector3(px + R(rng, 0.02f, 0.12f), y0 + 0.02f, zc + R(rng, -0.15f, 0.15f)), 0.01f,
                    M(Chance(rng, 0.5) ? new Color(0.12f, 0.20f, 0.55f) : Black));
            if (Chance(rng, 0.4))
                m.Box(new Vector3(px + R(rng, -0.08f, 0.08f), y0 + 0.035f, zc + R(rng, -0.15f, 0.15f)), new Vector3(0.05f, 0.05f, 0.15f), Yaw(R(rng, -30f, 30f)), M(V(new Color(0.20f, 0.20f, 0.22f), rng, 0.2f)));
        }

        /// <summary>An office desk's shallow pencil drawer under the top, shut: a slim front with a pull and a tray of pencils,
        /// a ruler and paper clips.</summary>
        public static void DeskPencilDrawer(RaidMesh m, System.Random rng, float w, float d, Color c, bool pedestalRight)
        {
            float side = pedestalRight ? 1f : -1f, cx = -side * 0.21f, cw = w - 0.6f;
            m.Box(new Vector3(cx, 0.68f, d * 0.5f - 0.03f), new Vector3(cw, 0.06f, 0.02f), M(c * 1.1f));
            m.Box(new Vector3(cx, 0.68f, d * 0.5f - 0.012f), new Vector3(0.08f, 0.014f, 0.016f), Mt(Brass));
            Surf box = M(c * 0.8f);
            float depth = d - 0.2f, zc = d * 0.5f - 0.04f - depth * 0.5f;
            m.Box(new Vector3(cx, 0.657f, zc), new Vector3(cw - 0.02f, 0.008f, depth), box);
            m.Box(new Vector3(cx - cw * 0.5f + 0.014f, 0.68f, zc), new Vector3(0.008f, 0.045f, depth), box);
            m.Box(new Vector3(cx + cw * 0.5f - 0.014f, 0.68f, zc), new Vector3(0.008f, 0.045f, depth), box);
            m.Box(new Vector3(cx, 0.68f, zc - depth * 0.5f + 0.004f), new Vector3(cw - 0.02f, 0.045f, 0.008f), box);
            int pencils = rng.Next(2, 6);
            for (int i = 0; i < pencils; i++)
            {
                float px = cx + R(rng, -cw * 0.35f, cw * 0.35f), pz = zc + R(rng, -depth * 0.3f, depth * 0.3f);
                Vector3 dir = Yaw(R(rng, -30f, 30f)) * new Vector3(0.08f, 0f, 0f);
                m.Rod(new Vector3(px, 0.668f, pz) - dir, new Vector3(px, 0.668f, pz) + dir, 0.008f, M(Chance(rng, 0.6) ? new Color(0.85f, 0.70f, 0.18f) : new Color(0.20f, 0.30f, 0.62f)));
            }
            if (Chance(rng, 0.5)) m.Box(new Vector3(cx + R(rng, -0.1f, 0.1f), 0.663f, zc + R(rng, -0.1f, 0.1f)), new Vector3(0.3f, 0.004f, 0.03f), Yaw(R(rng, -10f, 10f)), M(new Color(0.80f, 0.74f, 0.55f)));
        }

        /// <summary>A beige CRT monitor (<see cref="CrtMonitor"/>'s housing, foot and bezel) without its glass and power
        /// light, which <see cref="CrtScreen"/> draws lit or dark.</summary>
        public static void CrtMonitorBody(RaidMesh m, System.Random rng, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(Beige, rng, 0.04f);
            m.Box(at + q * new Vector3(0f, 0.015f, -0.02f), new Vector3(0.24f, 0.03f, 0.22f), q, M(c * 0.9f));
            m.Box(at + q * new Vector3(0f, 0.05f, -0.02f), new Vector3(0.1f, 0.05f, 0.1f), q, M(c * 0.85f));
            m.Box(at + q * new Vector3(0f, 0.26f, 0.02f), new Vector3(0.4f, 0.36f, 0.1f), q, M(c));
            m.Box(at + q * new Vector3(0f, 0.25f, -0.13f), new Vector3(0.32f, 0.29f, 0.18f), q, M(c * 0.93f));
            m.Box(at + q * new Vector3(0f, 0.24f, -0.25f), new Vector3(0.2f, 0.18f, 0.08f), q, M(c * 0.88f));
        }

        /// <summary>A CRT's glass and power light, at the monitor <see cref="CrtMonitorBody"/> drew: switched on, a dim green glow
        /// with lines of text and a green light; off, dark glass.</summary>
        public static void CrtScreen(RaidMesh m, System.Random rng, Vector3 at, float yaw, bool on)
        {
            Quaternion q = Yaw(yaw);
            m.Box(at + q * new Vector3(0f, 0.27f, 0.072f), new Vector3(0.32f, 0.26f, 0.006f), q, on ? Gl(ScreenOn) : Gs(ScreenOff));
            m.Box(at + q * new Vector3(0.15f, 0.1f, 0.072f), new Vector3(0.03f, 0.015f, 0.006f), q, on ? Gl(new Color(0.2f, 0.8f, 0.3f)) : M(Black));
            if (!on) return;
            Surf text = Gl(new Color(0.42f, 0.86f, 0.50f));
            int lines = 4 + rng.Next(4);
            for (int i = 0; i < lines; i++)
            {
                float len = R(rng, 0.06f, 0.24f);
                m.Box(at + q * new Vector3(-0.13f + len * 0.5f, 0.375f - i * 0.03f, 0.0775f), new Vector3(len, 0.008f, 0.001f), q, text);
            }
        }
        #endregion

        #region Field radio
        /// <summary>Where a field radio's knobs are, in the radio's own frame (for the hand that turns them).</summary>
        internal static readonly Vector3 FieldRadioKnobs = new Vector3(0.08f, 0.09f, 0.14f);

        /// <summary>The olive field radio (<see cref="FieldRadio"/>) with its dial window dark: the set, its knobs, the carry
        /// handle, the whip antenna and the handset beside it. <see cref="FieldRadioDial"/> lights the window.</summary>
        public static void FieldRadioBody(RaidMesh m, System.Random rng, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Color c = V(new Color(0.26f, 0.32f, 0.18f), rng, 0.05f);
            m.Box(at + q * new Vector3(0f, 0.11f, 0f), new Vector3(0.38f, 0.22f, 0.24f), q, M(c));
            m.Box(at + q * new Vector3(0f, 0.12f, 0.121f), new Vector3(0.32f, 0.16f, 0.004f), q, M(new Color(0.12f, 0.13f, 0.12f)));
            m.Box(at + q * new Vector3(-0.07f, 0.16f, 0.125f), new Vector3(0.12f, 0.04f, 0.004f), q, M(new Color(0.18f, 0.16f, 0.11f)));
            for (int i = 0; i < 3; i++)
                m.Cylinder(at + q * new Vector3(0.04f + i * 0.045f, 0.09f, 0.123f), 0.015f, 0.025f, 6, M(Black), q * Quaternion.Euler(90f, 0f, 0f));
            m.Box(at + q * new Vector3(-0.12f, 0.25f, 0f), new Vector3(0.02f, 0.06f, 0.02f), q, M(c * 0.7f));
            m.Box(at + q * new Vector3(0.12f, 0.25f, 0f), new Vector3(0.02f, 0.06f, 0.02f), q, M(c * 0.7f));
            m.Box(at + q * new Vector3(0f, 0.28f, 0f), new Vector3(0.26f, 0.02f, 0.025f), q, M(c * 0.7f));
            m.Rod(at + q * new Vector3(0.15f, 0.22f, -0.08f), at + q * new Vector3(0.2f, 0.85f, -0.16f), 0.008f, M(Black));
            m.Box(at + q * new Vector3(-0.3f, 0.03f, 0.02f), new Vector3(0.12f, 0.04f, 0.2f), q, M(new Color(0.1f, 0.1f, 0.1f)));
            m.Rod(at + q * new Vector3(-0.3f, 0.03f, -0.08f), at + q * new Vector3(-0.17f, 0.06f, -0.06f), 0.01f, M(Black));
        }

        /// <summary>A field radio's dial window, lit amber (shown while it's on).</summary>
        public static void FieldRadioDial(RaidMesh m, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            m.Box(at + q * new Vector3(-0.07f, 0.16f, 0.1285f), new Vector3(0.12f, 0.04f, 0.002f), q, Gl(new Color(0.85f, 0.62f, 0.24f)));
        }
        #endregion

        #region Steel counter and sink
        internal const float SteelDoorFront = 0.28f, SteelDoorY0 = 0.12f, SteelDoorY1 = 0.83f;
        internal const float SteelSinkW = 0.5f, SteelSinkD = 0.38f, SteelSinkZ = 0.02f, SteelSinkBottom = 0.78f, SteelDeckY = 0.96f;
        internal static readonly Vector3 SteelSpout = new Vector3(0f, 1.19f, -0.07f);     // where the water leaves the faucet (x from the sink)
        internal static readonly Vector3 SteelColdTap = new Vector3(0.12f, 1.007f, -0.27f); // the cold tap's cross-head (x from the sink)

        /// <summary>A stainless-steel counter run without its doors (length along X, 0.65 deep, 0.96 to the worktop; the look
        /// of <see cref="SteelCounter"/>): the carcass with its cupboard bays — one to a pair of doors, dark inside — the
        /// apron, the worktop and the backsplash; and with a <paramref name="sink"/> at <paramref name="sinkX"/>, a deep bowl
        /// let into the top, a gooseneck faucet between two cross-head taps (the cold one's head turns: drawn by
        /// <see cref="SinkTapHead"/>) and the trap under it. Returns the steel's shade, for the doors.</summary>
        public static Color SteelCounterBody(RaidMesh m, System.Random rng, float len, int doors, bool sink, float sinkX)
        {
            Color steel = V(Steel, rng, 0.04f);
            Surf body = Mt(steel * 0.92f), top = Mt(steel), inside = M(new Color(0.10f, 0.10f, 0.11f));
            float h = len * 0.5f;
            m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(len - 0.04f, 0.1f, 0.52f), M(new Color(0.16f, 0.17f, 0.18f)));
            m.Box(new Vector3(0f, 0.51f, -0.31f), new Vector3(len, 0.82f, 0.02f), body);
            m.Box(new Vector3(-h + 0.01f, 0.51f, -0.02f), new Vector3(0.02f, 0.82f, 0.6f), body);
            m.Box(new Vector3(h - 0.01f, 0.51f, -0.02f), new Vector3(0.02f, 0.82f, 0.6f), body);
            m.Box(new Vector3(0f, 0.11f, -0.02f), new Vector3(len - 0.04f, 0.02f, 0.58f), body);
            m.Box(new Vector3(0f, 0.88f, 0.27f), new Vector3(len, 0.08f, 0.02f), body);
            m.Box(new Vector3(0f, 0.48f, -0.296f), new Vector3(len - 0.04f, 0.7f, 0.006f), inside);
            float dw = len / Mathf.Max(1, doors);
            for (int i = 2; i < doors; i += 2)
                m.Box(new Vector3(-h + dw * i, 0.44f, -0.02f), new Vector3(0.02f, 0.64f, 0.58f), body);
            m.Box(new Vector3(0f, 1.03f, -0.315f), new Vector3(len + 0.02f, 0.14f, 0.03f), top);
            if (!sink)
            {
                m.Box(new Vector3(0f, 0.94f, 0f), new Vector3(len + 0.02f, 0.04f, 0.66f), top);
                return steel;
            }

            float bx0 = sinkX - SteelSinkW * 0.5f, bx1 = sinkX + SteelSinkW * 0.5f;
            float bz0 = SteelSinkZ - SteelSinkD * 0.5f, bz1 = SteelSinkZ + SteelSinkD * 0.5f;
            KitchenSlabAround(m, -h - 0.01f, h + 0.01f, -0.33f, 0.33f, bx0, bx1, bz0, bz1, 0.92f, SteelDeckY, top);
            Surf bowl = Mt(steel * 0.82f);
            float wallY = (SteelDeckY + SteelSinkBottom) * 0.5f, wallH = SteelDeckY - SteelSinkBottom;
            m.Box(new Vector3(bx0 + 0.003f, wallY, SteelSinkZ), new Vector3(0.006f, wallH, SteelSinkD), bowl);
            m.Box(new Vector3(bx1 - 0.003f, wallY, SteelSinkZ), new Vector3(0.006f, wallH, SteelSinkD), bowl);
            m.Box(new Vector3(sinkX, wallY, bz1 - 0.003f), new Vector3(SteelSinkW, wallH, 0.006f), bowl);
            m.Box(new Vector3(sinkX, wallY, bz0 + 0.003f), new Vector3(SteelSinkW, wallH, 0.006f), bowl);
            m.Box(new Vector3(sinkX, SteelSinkBottom - 0.003f, SteelSinkZ), new Vector3(SteelSinkW, 0.006f, SteelSinkD), bowl);
            m.Cylinder(new Vector3(sinkX, SteelSinkBottom, SteelSinkZ), 0.032f, 0.002f, 12, M(new Color(0.12f, 0.12f, 0.13f)));
            m.Cylinder(new Vector3(sinkX, SteelSinkBottom + 0.002f, SteelSinkZ), 0.022f, 0.001f, 12, Mt(Chrome));
            m.Box(new Vector3(sinkX, 0.968f, bz1 + 0.025f), new Vector3(SteelSinkW + 0.12f, 0.016f, 0.03f), top);
            m.Box(new Vector3(sinkX, 0.968f, bz0 - 0.025f), new Vector3(SteelSinkW + 0.12f, 0.016f, 0.03f), top);
            m.Box(new Vector3(bx0 - 0.045f, 0.968f, SteelSinkZ), new Vector3(0.03f, 0.016f, SteelSinkD + 0.08f), top);
            m.Box(new Vector3(bx1 + 0.045f, 0.968f, SteelSinkZ), new Vector3(0.03f, 0.016f, SteelSinkD + 0.08f), top);

            // The gooseneck faucet between the two taps; the hot tap's head stays put.
            Surf chrome = Mt(Chrome);
            m.Cylinder(new Vector3(sinkX, SteelDeckY, -0.27f), 0.018f, 0.3f, 6, chrome);
            m.Box(new Vector3(sinkX, 1.25f, -0.17f), new Vector3(0.03f, 0.03f, 0.22f), chrome);
            m.Box(new Vector3(sinkX + SteelSpout.x, 1.22f, SteelSpout.z), new Vector3(0.025f, 0.06f, 0.025f), chrome);
            m.Box(new Vector3(sinkX - SteelColdTap.x, 0.98f, SteelColdTap.z), new Vector3(0.04f, 0.04f, 0.04f), chrome);
            m.Box(new Vector3(sinkX + SteelColdTap.x, 0.98f, SteelColdTap.z), new Vector3(0.04f, 0.04f, 0.04f), chrome);
            KitchenTapHead(m, new Vector3(sinkX - SteelColdTap.x, SteelColdTap.y, SteelColdTap.z), TapHotRed);

            // Under the bowl: the waste, its trap and the supply pipes.
            Surf pvc = G(new Color(0.84f, 0.84f, 0.80f));
            m.Cylinder(new Vector3(sinkX, 0.6f, SteelSinkZ), 0.02f, SteelSinkBottom - 0.606f, 8, pvc);
            m.Cylinder(new Vector3(sinkX, 0.52f, SteelSinkZ), 0.036f, 0.1f, 10, pvc);
            m.Rod(new Vector3(sinkX, 0.585f, SteelSinkZ), new Vector3(sinkX, 0.585f, -0.296f), 0.04f, pvc, 8);
            Surf copper = Mt(new Color(0.72f, 0.42f, 0.24f));
            for (int i = 0; i < 2; i++)
            {
                float x = sinkX + (i == 0 ? -0.12f : 0.12f);
                m.Rod(new Vector3(x, 0.3f, -0.285f), new Vector3(x, 0.775f, -0.285f), 0.016f, copper, 6);
            }
            if (Chance(rng, 0.6)) m.Cylinder(new Vector3(sinkX + 0.1f, SteelSinkBottom, SteelSinkZ + 0.06f), 0.045f, 0.1f, 8, G(new Color(0.75f, 0.74f, 0.70f)));
            return steel;
        }

        /// <summary>One of the steel counter's cupboard doors (spanning <paramref name="x0"/>..<paramref name="x1"/> along the
        /// run), shut, its pull by the free edge.</summary>
        public static void SteelCounterDoor(RaidMesh m, Color steel, float x0, float x1, bool hingeLeft)
        {
            float cx = (x0 + x1) * 0.5f;
            m.Box(new Vector3(cx, (SteelDoorY0 + SteelDoorY1) * 0.5f, SteelDoorFront - 0.01f), new Vector3(x1 - x0 - 0.006f, SteelDoorY1 - SteelDoorY0, 0.02f), Mt(steel * 0.95f));
            m.Box(new Vector3(hingeLeft ? x1 - 0.06f : x0 + 0.06f, 0.74f, SteelDoorFront + 0.013f), new Vector3(0.025f, 0.12f, 0.025f), Mt(Chrome));
        }
        #endregion

        #region Ward beds, the examination couch, the privacy screen
        internal const float WardMattressTop = 0.66f, ExamCouchTop = 0.72f;

        /// <summary>A hospital bed made up to lie on (0.9 × 2.0, head at -Z): a white tube frame on casters, head- and
        /// footboards, a flat mattress in a pale sheet with a blanket folded at the foot, a pillow, side rails let down, and
        /// the patient's chart hung on the foot rail.</summary>
        public static void WardBed(RaidMesh m, System.Random rng)
        {
            Color white = new Color(0.82f, 0.83f, 0.82f);
            Color sheet = V(new Color(0.80f, 0.83f, 0.81f), rng, 0.03f);
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(i % 2 == 0 ? -0.4f : 0.4f, 0f, i < 2 ? -0.9f : 0.9f);
                m.Box(p + new Vector3(0f, 0.3f, 0f), new Vector3(0.04f, 0.5f, 0.04f), Mt(white));
                m.Box(p + new Vector3(0f, 0.04f, 0f), new Vector3(0.05f, 0.08f, 0.07f), M(Black));
            }
            m.Box(new Vector3(0f, 0.5f, 0f), new Vector3(0.88f, 0.06f, 1.96f), Mt(white));
            m.Box(new Vector3(0f, 0.595f, 0f), new Vector3(0.84f, 0.13f, 1.9f), M(sheet));
            Color blanket = V(new Color(0.55f, 0.62f, 0.66f), rng, 0.08f);
            m.Box(new Vector3(0f, WardMattressTop + 0.02f, 0.62f), new Vector3(0.86f, 0.04f, 0.56f), Yaw(R(rng, -3f, 3f)), M(blanket));
            m.Ellipsoid(new Vector3(0f, WardMattressTop + 0.06f, -0.72f), new Vector3(0.55f, 0.12f, 0.32f), M(new Color(0.88f, 0.88f, 0.86f)), 8, 4);
            m.Box(new Vector3(0f, 0.85f, -0.99f), new Vector3(0.9f, 0.7f, 0.04f), G(white));
            m.Box(new Vector3(0f, 0.75f, 0.99f), new Vector3(0.9f, 0.5f, 0.04f), G(white));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * 0.46f, 0.55f, -0.1f), new Vector3(0.025f, 0.025f, 1.1f), Mt(white));
                m.Box(new Vector3(s * 0.46f, 0.45f, -0.1f), new Vector3(0.025f, 0.025f, 1.1f), Mt(white));
            }
            m.Box(new Vector3(0.18f, 0.84f, 1.017f), new Vector3(0.2f, 0.28f, 0.01f), M(new Color(0.55f, 0.40f, 0.22f)));
            m.Box(new Vector3(0.18f, 0.83f, 1.023f), new Vector3(0.18f, 0.24f, 0.003f), M(Paper));
            m.Box(new Vector3(0.18f, 0.97f, 1.024f), new Vector3(0.07f, 0.03f, 0.008f), Mt(Chrome));
        }

        /// <summary>The clinic's examination couch (0.7 × 1.9 on a 0.95 × 2.0 footprint, head at -Z): a padded vinyl top on a
        /// steel base, a sheet of paper drawn down it from the roll at the head, and a step at its side.</summary>
        public static void ExamCouch(RaidMesh m, System.Random rng)
        {
            Color steel = new Color(0.70f, 0.71f, 0.72f);
            Color vinyl = V(new Color(0.16f, 0.32f, 0.34f), rng, 0.06f);
            m.Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 1.7f), Mt(steel));
            m.Box(new Vector3(0f, 0.04f, 0f), new Vector3(0.64f, 0.08f, 1.74f), M(new Color(0.2f, 0.2f, 0.21f)));
            m.Box(new Vector3(0f, 0.36f, 0.851f), new Vector3(0.3f, 0.12f, 0.004f), M(steel * 0.8f));
            m.Box(new Vector3(0f, 0.66f, 0f), new Vector3(0.7f, 0.12f, 1.9f), G(vinyl));
            m.Box(new Vector3(0f, ExamCouchTop + 0.002f, 0.08f), new Vector3(0.5f, 0.004f, 1.6f), M(new Color(0.92f, 0.91f, 0.86f)));
            m.CylinderX(new Vector3(0f, ExamCouchTop + 0.04f, -0.93f), 0.045f, 0.52f, 10, M(new Color(0.92f, 0.91f, 0.86f)));
            m.Box(new Vector3(-0.29f, ExamCouchTop + 0.04f, -0.93f), new Vector3(0.02f, 0.1f, 0.03f), Mt(Chrome));
            m.Box(new Vector3(0.29f, ExamCouchTop + 0.04f, -0.93f), new Vector3(0.02f, 0.1f, 0.03f), Mt(Chrome));
            m.Box(new Vector3(0.36f, 0.11f, 0.3f), new Vector3(0.18f, 0.06f, 0.4f), M(new Color(0.2f, 0.2f, 0.21f)));
            for (int s = -1; s <= 1; s += 2)
                m.Box(new Vector3(0.36f, 0.055f, 0.3f + s * 0.17f), new Vector3(0.16f, 0.11f, 0.03f), Mt(steel));
        }

        /// <summary>Where the posts of the three-panel privacy screen (<see cref="PrivacyScreen"/>) stand: the outer ends and
        /// the two hinges of the middle panel.</summary>
        internal static readonly Vector3[] ScreenPosts =
        {
            new Vector3(-0.844f, 0f, -0.127f), new Vector3(-0.3002f, 0f, 0.1266f), new Vector3(0.2998f, 0f, 0.1266f), new Vector3(0.8436f, 0f, -0.127f),
        };

        /// <summary>Panel <paramref name="i"/> (0..2, left to right) of the folding privacy screen with its posts — the middle one
        /// both of its hinges, an outer one the post at its free end — in <paramref name="cloth"/>.</summary>
        public static void ScreenPanel(RaidMesh m, Color cloth, int i)
        {
            Quaternion q = Yaw((i - 1) * 25f);
            Vector3 c = ScreenPosts[i] + q * new Vector3(0.3f, 0f, 0f);
            m.Box(c + Vector3.up * 0.95f, new Vector3(0.58f, 1.5f, 0.015f), q, M(cloth));
            m.Box(c + Vector3.up * 1.72f, new Vector3(0.6f, 0.025f, 0.025f), q, Mt(Chrome));
            for (int k = 0; k < 4; k++)
            {
                bool mine = i == 1 ? k == 1 || k == 2 : k == (i == 0 ? 0 : 3);
                if (!mine) continue;
                m.Box(ScreenPosts[k] + Vector3.up * 0.9f, new Vector3(0.025f, 1.8f, 0.025f), Mt(Chrome));
                m.Ellipsoid(ScreenPosts[k] + Vector3.up * 0.02f, new Vector3(0.05f, 0.04f, 0.05f), M(Black), 6, 3);
            }
        }
        #endregion

        #region The garage's car
        // The sedan on the garage floor (4.4 × 1.78, front at +Z): DecorProps.Car's sedan without the snow, built so its
        // trunk lid lifts and its front passenger door (on the +X side) swings open on the dim inside of the car.
        internal const float CarLen = 4.4f, CarWid = 1.78f, CarBodyY = 0.35f, CarBeltY = 0.93f;
        internal const float CarCabLen = 2.2f, CarCabZ = -0.2f;
        internal const float CarDoorZ0 = -0.17f, CarDoorZ1 = 0.82f;    // the passenger door, B-pillar to A-pillar
        internal const float CarTrunkZ0 = -2.12f, CarTrunkZ1 = -1.32f; // the trunk lid, tail to rear window
        internal const float CarSkin = 0.08f;                          // the passenger side's skin, drawn apart from the core

        /// <summary>The garage car's fixed body, in <paramref name="paint"/>: the wheels, the core of the lower body with the
        /// passenger side's skin round the door and a dark opening behind it (a seat's edge and the end of the dash showing),
        /// the trunk's well under its lid with a spare and a tool roll in it, the cabin's pillars, roof and windows (all but
        /// the passenger door's), bumpers and lights, and the odd rust bloom.</summary>
        public static Color GarageCarBody(RaidMesh m, System.Random rng, Color paint)
        {
            Color c = V(paint, rng, 0.06f);
            Color glass = new Color(0.07f, 0.09f, 0.10f), chrome = new Color(0.55f, 0.56f, 0.56f), dark = new Color(0.06f, 0.06f, 0.07f);
            const float hw = CarWid * 0.5f, hl = CarLen * 0.5f, wheelR = 0.33f;
            float lowY = CarBodyY + 0.02f, lowH = CarBeltY - lowY, lowC = (lowY + CarBeltY) * 0.5f;
            int flat = Chance(rng, 0.4) ? rng.Next(4) : -1;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (hw - 0.14f);
                float z = (i < 2 ? 1f : -1f) * (hl - 0.85f);
                float r = i == flat ? wheelR * 0.8f : wheelR;
                m.CylinderX(new Vector3(x, r, z), r, 0.24f, 12, M(new Color(0.07f, 0.07f, 0.07f)));
                m.CylinderX(new Vector3(x + Mathf.Sign(x) * 0.121f, r, z), r * 0.55f, 0.01f, 10, Mt(chrome * 0.8f));
            }

            // The lower body: the core forward of the trunk, recessed on the passenger side; the trunk's walls and floor behind.
            Surf body = G(c), inside = M(dark);
            float coreZ0 = CarTrunkZ1 - 0.04f, coreZ1 = hl - 0.05f, coreX1 = hw - CarSkin;
            m.Box(new Vector3((-hw + coreX1) * 0.5f, lowC, (coreZ0 + coreZ1) * 0.5f), new Vector3(coreX1 + hw, lowH, coreZ1 - coreZ0), body);
            m.Box(new Vector3(hw - CarSkin * 0.5f, lowC, (CarDoorZ1 + coreZ1) * 0.5f), new Vector3(CarSkin, lowH, coreZ1 - CarDoorZ1), body);
            m.Box(new Vector3(hw - CarSkin * 0.5f, lowC, (-hl + 0.05f + CarDoorZ0) * 0.5f), new Vector3(CarSkin, lowH, CarDoorZ0 + hl - 0.05f), body);
            m.Box(new Vector3(coreX1 + 0.002f, lowC + 0.02f, (CarDoorZ0 + CarDoorZ1) * 0.5f), new Vector3(0.004f, lowH - 0.04f, CarDoorZ1 - CarDoorZ0 - 0.02f), inside);
            m.Box(new Vector3(coreX1 + 0.02f, lowY + 0.03f, (CarDoorZ0 + CarDoorZ1) * 0.5f), new Vector3(0.036f, 0.06f, CarDoorZ1 - CarDoorZ0 - 0.02f), M(dark * 2f));
            float tz = (-hl + 0.05f + coreZ0) * 0.5f, tl = coreZ0 + hl - 0.05f;
            m.Box(new Vector3(-hw + 0.03f, lowC, tz), new Vector3(0.06f, lowH, tl), body);
            m.Box(new Vector3(coreX1 - 0.03f, lowC, tz), new Vector3(0.06f, lowH, tl), body);
            m.Box(new Vector3((-hw + coreX1) * 0.5f, lowC, -hl + 0.08f), new Vector3(coreX1 + hw, lowH, 0.06f), body);
            m.Box(new Vector3((-hw + coreX1) * 0.5f, lowY + 0.1f, tz), new Vector3(coreX1 + hw, 0.2f, tl), body);
            m.Box(new Vector3((-hw + coreX1) * 0.5f, lowY + 0.202f, tz), new Vector3(coreX1 + hw - 0.12f, 0.004f, tl - 0.08f), inside);
            m.Cylinder(new Vector3(-0.3f, lowY + 0.204f, tz + 0.05f), 0.27f, 0.16f, 12, M(new Color(0.07f, 0.07f, 0.07f)));
            m.Cylinder(new Vector3(-0.3f, lowY + 0.21f, tz + 0.05f), 0.15f, 0.16f, 10, Mt(chrome * 0.7f));
            if (Chance(rng, 0.7)) m.CylinderX(new Vector3(0.35f, lowY + 0.25f, tz - 0.1f), 0.05f, 0.4f, 8, M(new Color(0.30f, 0.20f, 0.12f)), R(rng, -20f, 20f));

            // Bumpers and lights, and the plate on the tail.
            m.Box(new Vector3(0f, CarBodyY + 0.06f, hl - 0.04f), new Vector3(CarWid - 0.02f, 0.14f, 0.1f), Mt(chrome));
            m.Box(new Vector3(0f, CarBodyY + 0.06f, -hl + 0.04f), new Vector3(CarWid - 0.02f, 0.14f, 0.1f), Mt(chrome));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * (hw - 0.22f), CarBodyY + 0.34f, hl - 0.045f), new Vector3(0.32f, 0.14f, 0.02f), Gs(new Color(0.75f, 0.74f, 0.66f)));
                m.Box(new Vector3(s * (hw - 0.2f), CarBodyY + 0.36f, -hl + 0.045f), new Vector3(0.3f, 0.12f, 0.02f), Gs(new Color(0.5f, 0.08f, 0.06f)));
            }
            m.Box(new Vector3(0f, CarBodyY + 0.3f, -hl + 0.046f), new Vector3(0.3f, 0.15f, 0.01f), M(new Color(0.86f, 0.84f, 0.74f)));
            m.Box(new Vector3(0f, CarBeltY - 0.06f, -hl + 0.046f), new Vector3(0.1f, 0.04f, 0.012f), Mt(chrome));

            // The cabin: a dark inside behind the glass, the roof and the pillars; the passenger door carries its own window.
            float cabY = CarBeltY, cz0 = CarCabZ - CarCabLen * 0.5f, cz1 = CarCabZ + CarCabLen * 0.5f;
            m.Box(new Vector3(0f, cabY + 0.22f, CarCabZ), new Vector3(CarWid - 0.24f, 0.44f, CarCabLen - 0.04f), M(dark));
            m.Box(new Vector3(0f, cabY + 0.47f, CarCabZ), new Vector3(CarWid - 0.14f, 0.06f, CarCabLen + 0.04f), body);
            Surf pane = Gs(glass);
            m.Box(new Vector3(0f, cabY + 0.22f, cz1 - 0.01f), new Vector3(CarWid - 0.2f, 0.44f, 0.01f), pane);
            m.Box(new Vector3(0f, cabY + 0.22f, cz0 + 0.01f), new Vector3(CarWid - 0.2f, 0.44f, 0.01f), pane);
            m.Box(new Vector3(-hw + 0.1f, cabY + 0.22f, CarCabZ), new Vector3(0.01f, 0.44f, CarCabLen - 0.04f), pane);
            m.Box(new Vector3(hw - 0.1f, cabY + 0.22f, (cz0 + CarDoorZ0) * 0.5f), new Vector3(0.01f, 0.44f, CarDoorZ0 - cz0), pane);
            for (int s = -1; s <= 1; s += 2)
            {
                m.Box(new Vector3(s * (hw - 0.1f), cabY + 0.22f, cz1 - 0.04f), new Vector3(0.06f, 0.46f, 0.08f), body);
                m.Box(new Vector3(s * (hw - 0.1f), cabY + 0.22f, cz0 + 0.04f), new Vector3(0.06f, 0.46f, 0.08f), body);
                m.Box(new Vector3(s * (hw - 0.1f), cabY + 0.22f, CarCabZ), new Vector3(0.06f, 0.46f, 0.06f), body);
            }
            // Seen through the open passenger door: the edge of the seat, the end of the dash with the glovebox in it.
            Color seat = new Color(0.30f, 0.24f, 0.18f), dash = dark * 2.5f;
            m.Box(new Vector3(coreX1 + 0.015f, 0.70f, 0.12f), new Vector3(0.03f, 0.12f, 0.5f), M(seat));
            m.Box(new Vector3(hw - 0.12f, cabY + 0.16f, -0.1f), new Vector3(0.02f, 0.32f, 0.1f), M(seat));
            m.Box(new Vector3(coreX1 + 0.015f, 0.855f, 0.67f), new Vector3(0.03f, 0.15f, 0.26f), M(dash));
            m.Box(new Vector3(hw - 0.12f, cabY + 0.035f, 0.7f), new Vector3(0.02f, 0.07f, 0.3f), M(dash));
            m.Box(new Vector3(coreX1 + 0.015f, 0.86f, 0.538f), new Vector3(0.026f, 0.08f, 0.004f), M(new Color(0.24f, 0.22f, 0.20f)));

            int rust = rng.Next(0, 3);
            for (int i = 0; i < rust; i++)
            {
                float z = R(rng, -hl * 0.8f, hl * 0.8f);
                if (z > CarDoorZ0 - 0.1f && z < CarDoorZ1 + 0.1f) z = R(rng, -hl * 0.8f, CarDoorZ0 - 0.2f);
                m.Blob(new Vector3(-hw - 0.004f, CarBodyY + R(rng, 0.15f, 0.45f), z), new Vector3(0f, 0f, -1f), Vector3.up, R(rng, 0.12f, 0.3f), R(rng, 0.08f, 0.18f), 9, 0.4f, rng,
                    M(new Color(0.34f, 0.18f, 0.09f)));
            }
            return c;
        }

        /// <summary>The garage car's trunk lid, shut over the well (it hinges at the foot of the rear window).</summary>
        public static void GarageCarTrunkLid(RaidMesh m, Color c)
        {
            m.Box(new Vector3(0f, CarBeltY + 0.015f, (CarTrunkZ0 + CarTrunkZ1) * 0.5f), new Vector3(CarWid - 0.04f, 0.03f, CarTrunkZ1 - CarTrunkZ0), G(c));
            m.Box(new Vector3(0f, CarBeltY - 0.005f, (CarTrunkZ0 + CarTrunkZ1) * 0.5f), new Vector3(CarWid - 0.2f, 0.01f, CarTrunkZ1 - CarTrunkZ0 - 0.12f), M(new Color(0.12f, 0.12f, 0.13f)));
        }

        /// <summary>The garage car's front passenger door, shut: its outer skin with a handle, the window above it, and the
        /// trim on its inside. It hinges at its front edge, by the A-pillar.</summary>
        public static void GarageCarDoor(RaidMesh m, Color c)
        {
            const float hw = CarWid * 0.5f, lowY = CarBodyY + 0.02f;
            float zc = (CarDoorZ0 + CarDoorZ1) * 0.5f, len = CarDoorZ1 - CarDoorZ0 - 0.01f;
            m.Box(new Vector3(hw - CarSkin * 0.5f, (lowY + CarBeltY) * 0.5f, zc), new Vector3(CarSkin, CarBeltY - lowY, len), G(c));
            m.Box(new Vector3(hw + 0.006f, CarBeltY - 0.12f, CarDoorZ0 + 0.18f), new Vector3(0.02f, 0.03f, 0.14f), Mt(new Color(0.55f, 0.56f, 0.56f)));
            m.Box(new Vector3(hw - CarSkin - 0.004f, (lowY + CarBeltY) * 0.5f + 0.04f, zc), new Vector3(0.008f, CarBeltY - lowY - 0.14f, len - 0.08f), M(new Color(0.22f, 0.20f, 0.18f)));
            m.Box(new Vector3(hw - 0.1f, CarBeltY + 0.215f, zc), new Vector3(0.01f, 0.43f, len - 0.06f), Gs(new Color(0.07f, 0.09f, 0.10f)));
            m.Box(new Vector3(hw - 0.1f, CarBeltY + 0.425f, zc), new Vector3(0.03f, 0.03f, len - 0.04f), G(c));
        }
        #endregion
    }
}
