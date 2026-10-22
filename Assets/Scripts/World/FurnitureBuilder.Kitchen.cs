using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        // The starter home's working kitchen: the sink unit, the counter, the wall cupboards, the gas range and the
        // fridge-freezer, each drawn as a fixed body plus the doors, drawers and fittings that move (drawn apart so
        // they can swing, slide, light up or turn). Every piece is drawn in its local frame like the rest of the
        // furniture (front toward +Z, origin on the floor at its middle); the layout constants are shared with the
        // fixtures that animate them (KitchenSink, KitchenStove, KitchenFridge, HomeFurniture.Kitchen).

        #region Kitchen colours
        private static readonly Color KitchenPlinth = new Color(0.10f, 0.09f, 0.08f);
        private static readonly Color KitchenPlate = new Color(0.90f, 0.90f, 0.86f);
        private static readonly Color OvenDark = new Color(0.10f, 0.10f, 0.11f);
        private static readonly Color OvenGlow = new Color(0.95f, 0.45f, 0.15f);
        private static readonly Color FridgeLinerWhite = new Color(0.90f, 0.93f, 0.95f);
        private static readonly Color FreezerFrost = new Color(0.86f, 0.92f, 0.97f);
        private static readonly Color TapHotRed = new Color(0.78f, 0.12f, 0.10f);
        private static readonly Color TapColdBlue = new Color(0.16f, 0.36f, 0.80f);
        private static readonly Color[] KitchenMugColours =
        {
            new Color(0.62f, 0.22f, 0.18f), new Color(0.22f, 0.36f, 0.56f), new Color(0.86f, 0.80f, 0.56f),
        };

        /// <summary>The colours of one run of kitchen units, picked once so the carcass and the doors drawn apart
        /// from it match: the paint, the doors' slightly lighter paint, the shadowed inside and the worktop.</summary>
        internal readonly struct KitchenTone
        {
            public readonly Color Cabinet, Door, Inside, Top;

            public KitchenTone(Color cabinet, Color top)
            {
                Cabinet = cabinet;
                Door = cabinet * 1.08f;
                Inside = cabinet * 0.82f;
                Top = top;
            }
        }

        /// <summary>The starter home's fitted kitchen: the sink unit, the counter and the wall cupboards share one paint
        /// and one worktop, so units built apart (and the doors drawn apart from them) match exactly.</summary>
        internal static readonly Color HomeCabinetPaint = new Color(0.78f, 0.76f, 0.66f);
        internal static readonly Color HomeWorktop = new Color(0.58f, 0.44f, 0.28f);
        internal static KitchenTone HomeKitchen => new KitchenTone(HomeCabinetPaint, HomeWorktop);
        #endregion

        #region Shared parts
        // The shell of a base unit (len along X): plinth, end panels, back and floor — the inside a shade darker, so
        // an open door shows a cupboard rather than a solid block — and the rail under the worktop down to railBottom.
        private static void KitchenCarcass(RaidMesh m, in KitchenTone t, float len, float railBottom)
        {
            float h = len * 0.5f;
            Surf cab = M(t.Cabinet), inside = M(t.Inside);
            m.Box(new Vector3(0f, 0.05f, -0.01f), new Vector3(len - 0.04f, 0.1f, 0.52f), M(KitchenPlinth));
            m.Box(new Vector3(-h + 0.009f, 0.45f, -0.02f), new Vector3(0.018f, 0.9f, 0.58f), cab);
            m.Box(new Vector3(h - 0.009f, 0.45f, -0.02f), new Vector3(0.018f, 0.9f, 0.58f), cab);
            m.Box(new Vector3(0f, 0.5f, -0.302f), new Vector3(len - 0.036f, 0.8f, 0.012f), inside);
            m.Box(new Vector3(0f, 0.109f, -0.02f), new Vector3(len - 0.036f, 0.018f, 0.56f), inside);
            m.Box(new Vector3(0f, (railBottom + 0.9f) * 0.5f, 0.262f), new Vector3(len - 0.036f, 0.9f - railBottom, 0.02f), cab);
        }

        // A full laminate worktop with its upstand against the wall.
        private static void KitchenWorktop(RaidMesh m, in KitchenTone t, float len)
        {
            m.Box(new Vector3(0f, 0.92f, 0f), new Vector3(len + 0.02f, 0.04f, 0.63f), G(t.Top));
            KitchenUpstand(m, t, len);
        }

        private static void KitchenUpstand(RaidMesh m, in KitchenTone t, float len)
            => m.Box(new Vector3(0f, 1.0f, -0.305f), new Vector3(len + 0.02f, 0.12f, 0.02f), G(t.Top * 0.9f));

        // A flat slab x0..x1 by z0..z1 (y0..y1 thick) with a rectangular hole hx0..hx1 by hz0..hz1 cut in it.
        private static void KitchenSlabAround(RaidMesh m, float x0, float x1, float z0, float z1,
            float hx0, float hx1, float hz0, float hz1, float y0, float y1, in Surf s)
        {
            float cy = (y0 + y1) * 0.5f, hy = y1 - y0, cz = (z0 + z1) * 0.5f, hcx = (hx0 + hx1) * 0.5f;
            m.Box(new Vector3((x0 + hx0) * 0.5f, cy, cz), new Vector3(hx0 - x0, hy, z1 - z0), s);
            m.Box(new Vector3((hx1 + x1) * 0.5f, cy, cz), new Vector3(x1 - hx1, hy, z1 - z0), s);
            m.Box(new Vector3(hcx, cy, (hz1 + z1) * 0.5f), new Vector3(hx1 - hx0, hy, z1 - hz1), s);
            m.Box(new Vector3(hcx, cy, (z0 + hz0) * 0.5f), new Vector3(hx1 - hx0, hy, hz0 - z0), s);
        }

        // A painted cupboard door (0.02 thick, its front face at zFront) with a raised panel and a brass knob.
        private static void KitchenCupboardDoor(RaidMesh m, in KitchenTone t, float x0, float x1, float y0, float y1, float zFront,
            float knobX, float knobY)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, w = x1 - x0, h = y1 - y0;
            m.Box(new Vector3(cx, cy, zFront - 0.01f), new Vector3(w, h, 0.02f), M(t.Door));
            m.Box(new Vector3(cx, cy, zFront + 0.0015f), new Vector3(w - 0.08f, h - 0.09f, 0.003f), M(t.Door * 0.95f));
            // Thin chrome edge strip around the painted panel for realism
            float edgeThickness = 0.001f;
            // Top edge
            m.Box(new Vector3(cx, cy + (h - 0.09f) / 2f - edgeThickness / 2f, zFront + 0.0015f), new Vector3(w - 0.08f, edgeThickness, 0.003f), Mt(Chrome));
            // Bottom edge
            m.Box(new Vector3(cx, cy - (h - 0.09f) / 2f + edgeThickness / 2f, zFront + 0.0015f), new Vector3(w - 0.08f, edgeThickness, 0.003f), Mt(Chrome));
            // Left edge
            m.Box(new Vector3(cx - (w - 0.08f) / 2f + edgeThickness / 2f, cy, zFront + 0.0015f), new Vector3(edgeThickness, h - 0.09f, 0.003f), Mt(Chrome));
            // Right edge
            m.Box(new Vector3(cx + (w - 0.08f) / 2f - edgeThickness / 2f, cy, zFront + 0.0015f), new Vector3(edgeThickness, h - 0.09f, 0.003f), Mt(Chrome));
            Quaternion outward = Quaternion.Euler(90f, 0f, 0f);
            m.Cylinder(new Vector3(knobX, knobY, zFront), 0.008f, 0.02f, 6, Mt(Brass), outward);
            m.Cylinder(new Vector3(knobX, knobY, zFront + 0.016f), 0.015f, 0.01f, 8, Mt(Brass), outward);
        }
        #endregion

        #region Sink unit
        // The sink unit (the kitchen tap's WaterSource, 1.2 long, standing unturned): a single steel bowl with a
        // drainer to its left and a mixer tap behind it; underneath, a blind corner (the counter's end hides it)
        // beside a pair of doors.
        internal const float SinkLength = 1.2f;
        internal const float SinkBowlX = 0.24f, SinkBowlZ = 0.02f;           // the bowl's middle (and its drain)
        internal const float SinkBowlW = 0.42f, SinkBowlD = 0.34f;           // its inside, along X and Z
        internal const float SinkBowlBottom = 0.784f;                        // the top of its floor
        internal const float SinkDeckY = 0.946f;                             // the top of the steel deck round it
        internal const float SinkWaterDepth = 0.13f;                         // the depth of a full basin
        internal static readonly Vector3 SinkSpout = new Vector3(0.24f, 1.135f, 0f);       // where the water leaves the tap
        internal static readonly Vector3 SinkColdTap = new Vector3(0.34f, 0.99f, -0.18f);  // the cold tap's cross-head
        internal const float SinkDoorX0 = 0.048f, SinkDoorMid = 0.314f, SinkDoorX1 = 0.58f;
        internal const float SinkDoorY0 = 0.118f, SinkDoorY1 = 0.828f, SinkDoorFront = 0.29f;

        /// <summary>The kitchen sink unit's fixed body: base cabinets under a worktop cut for the bowl, a stainless
        /// bowl and ribbed drainer, the mixer tap (all but the cold tap's cross-head, which turns — see
        /// <see cref="SinkTapHead"/>), the plumbing and a bucket in the cupboard, and a dish rack, washing-up liquid
        /// and a sponge on top. The doors under the bowl are drawn by <see cref="SinkDoor"/>.</summary>
        public static void SinkUnit(RaidMesh m, System.Random rng, in KitchenTone t)
        {
            const float len = SinkLength, h = len * 0.5f;
            KitchenCarcass(m, t, len, SinkDoorY1 + 0.002f);
            Surf inside = M(t.Inside);

            // The blind corner: two fixed panels, a stile beside the doors and a divider inside.
            float b0 = -h + 0.02f, b1 = SinkDoorX0 - 0.02f, bw = (b1 - b0) * 0.5f, doorY = (SinkDoorY0 + SinkDoorY1) * 0.5f;
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(b0 + bw * (i + 0.5f), doorY, SinkDoorFront - 0.01f), new Vector3(bw - 0.004f, SinkDoorY1 - SinkDoorY0, 0.02f), M(t.Door));
            m.Box(new Vector3(SinkDoorX0 - 0.011f, doorY, 0.262f), new Vector3(0.018f, SinkDoorY1 - SinkDoorY0, 0.02f), M(t.Cabinet));
            m.Box(new Vector3(SinkDoorX0 - 0.011f, 0.44f, -0.02f), new Vector3(0.016f, 0.64f, 0.56f), inside);

            // The worktop, cut for the bowl, and its upstand.
            float bx0 = SinkBowlX - SinkBowlW * 0.5f, bx1 = SinkBowlX + SinkBowlW * 0.5f;
            float bz0 = SinkBowlZ - SinkBowlD * 0.5f, bz1 = SinkBowlZ + SinkBowlD * 0.5f;
            KitchenSlabAround(m, -h - 0.01f, h + 0.01f, -0.315f, 0.315f, bx0, bx1, bz0, bz1, 0.90f, 0.94f, G(t.Top));
            KitchenUpstand(m, t, len);

            // The stainless deck with its ribbed drainer, and the bowl let into it.
            KitchenSlabAround(m, -0.52f, 0.52f, -0.21f, 0.25f, bx0, bx1, bz0, bz1, 0.94f, SinkDeckY, Mt(Steel));
for (int i = 0; i < 7; i++)
                 m.Box(new Vector3(-0.47f + i * 0.068f, SinkDeckY + 0.0015f, 0.02f), new Vector3(0.012f, 0.003f, 0.36f), Mt(Steel * 1.06f));
             // Occasionally add a subtle oil stain on the stainless‑steel deck
             if (Chance(rng, 0.25f))
                 m.Box(new Vector3(0f, SinkDeckY + 0.001f, 0f), new Vector3(0.08f, 0.004f, 0.02f), Mt(Steel * 0.8f));
             Surf bowl = Mt(Steel * 0.82f);
            float wallY = (SinkDeckY + SinkBowlBottom - 0.006f) * 0.5f, wallH = SinkDeckY - SinkBowlBottom + 0.006f;
            m.Box(new Vector3(bx0 + 0.003f, wallY, SinkBowlZ), new Vector3(0.006f, wallH, SinkBowlD), bowl);
            m.Box(new Vector3(bx1 - 0.003f, wallY, SinkBowlZ), new Vector3(0.006f, wallH, SinkBowlD), bowl);
            m.Box(new Vector3(SinkBowlX, wallY, bz1 - 0.003f), new Vector3(SinkBowlW, wallH, 0.006f), bowl);
            m.Box(new Vector3(SinkBowlX, wallY, bz0 + 0.003f), new Vector3(SinkBowlW, wallH, 0.006f), bowl);
            m.Box(new Vector3(SinkBowlX, SinkBowlBottom - 0.003f, SinkBowlZ), new Vector3(SinkBowlW, 0.006f, SinkBowlD), bowl);
            m.Cylinder(new Vector3(SinkBowlX, SinkBowlBottom, SinkBowlZ), 0.032f, 0.002f, 12, M(new Color(0.12f, 0.12f, 0.13f)));
            m.Cylinder(new Vector3(SinkBowlX, SinkBowlBottom + 0.002f, SinkBowlZ), 0.022f, 0.001f, 12, Mt(Chrome));
            m.Box(new Vector3(SinkBowlX, 0.915f, bz0 + 0.0075f), new Vector3(0.05f, 0.012f, 0.002f), M(new Color(0.1f, 0.1f, 0.1f)));   // the overflow

            // The mixer tap: a swan-neck spout between a hot and a cold tap.
            Surf chrome = Mt(Chrome);
            m.Cylinder(new Vector3(SinkBowlX, SinkDeckY, -0.18f), 0.026f, 0.028f, 10, chrome);
            m.Cylinder(new Vector3(SinkBowlX, SinkDeckY + 0.028f, -0.18f), 0.016f, 0.19f, 8, chrome);
            m.Box(new Vector3(SinkBowlX, 1.17f, -0.095f), new Vector3(0.028f, 0.028f, 0.2f), chrome);
            m.Box(new Vector3(SinkSpout.x, SinkSpout.y + 0.015f, SinkSpout.z), new Vector3(0.026f, 0.03f, 0.026f), chrome);
            Vector3 hot = new Vector3(SinkColdTap.x - 0.2f, SinkColdTap.y, SinkColdTap.z);
            m.Cylinder(new Vector3(hot.x, SinkDeckY, hot.z), 0.018f, 0.036f, 8, chrome);
            // Subtle rust spot on the hot tap (occasional)
            if (Chance(rng, 0.15f))
            {
                Color rust = new Color(0.55f, 0.30f, 0.10f);
                // small rust patch on the side of the tap
                m.Box(new Vector3(hot.x + 0.015f, SinkDeckY + 0.018f, hot.z), new Vector3(0.004f, 0.004f, 0.004f), M(rust));
            }
            m.Cylinder(new Vector3(SinkColdTap.x, SinkDeckY, SinkColdTap.z), 0.018f, 0.036f, 8, chrome);
            KitchenTapHead(m, hot, TapHotRed);

            // Under the bowl: the waste and its trap, the copper supply pipes with their stopcocks, a bucket and bleach.
            Surf pvc = G(new Color(0.84f, 0.84f, 0.80f));
            m.Cylinder(new Vector3(SinkBowlX, 0.6f, SinkBowlZ), 0.02f, SinkBowlBottom - 0.606f, 8, pvc);
            m.Cylinder(new Vector3(SinkBowlX, 0.52f, SinkBowlZ), 0.036f, 0.1f, 10, pvc);
            m.Rod(new Vector3(SinkBowlX, 0.585f, SinkBowlZ), new Vector3(SinkBowlX, 0.585f, -0.296f), 0.04f, pvc, 8);
            Surf copper = Mt(new Color(0.72f, 0.42f, 0.24f));
            for (int i = 0; i < 2; i++)
            {
                float x = SinkBowlX + (i == 0 ? -0.12f : 0.12f);
                m.Rod(new Vector3(x, 0.3f, -0.285f), new Vector3(x, 0.775f, -0.285f), 0.016f, copper, 6);
                m.Box(new Vector3(x, 0.42f, -0.272f), new Vector3(0.03f, 0.04f, 0.03f), Mt(Brass));
            }
            Color bucket = Chance(rng, 0.5) ? new Color(0.70f, 0.16f, 0.12f) : new Color(0.20f, 0.38f, 0.62f);
            m.Cylinder(new Vector3(0.45f, 0.118f, 0.06f), 0.085f, 0.2f, 12, G(bucket), Quaternion.identity, 1.2f, false);
            m.Cylinder(new Vector3(0.45f, 0.118f, 0.06f), 0.085f, 0.006f, 12, G(bucket * 0.8f));
            m.Cylinder(new Vector3(0.14f, 0.118f, 0.13f), 0.04f, 0.2f, 10, G(new Color(0.9f, 0.9f, 0.86f)));
            m.Cylinder(new Vector3(0.14f, 0.318f, 0.13f), 0.016f, 0.03f, 8, G(new Color(0.2f, 0.4f, 0.75f)));

            // On top: a rack of plates on the drainer, the washing-up liquid and a sponge.
            KitchenDishRack(m, rng, new Vector3(-0.28f, SinkDeckY, 0.02f));
            if (Chance(rng, 0.8))
            {
                m.Cylinder(new Vector3(-0.55f, 0.94f, -0.24f), 0.028f, 0.17f, 8, G(new Color(0.30f, 0.62f, 0.32f)));
                m.Cylinder(new Vector3(-0.55f, 1.11f, -0.24f), 0.012f, 0.025f, 6, G(new Color(0.92f, 0.92f, 0.90f)));
            }
            m.Box(new Vector3(0.55f, 0.955f, -0.255f), new Vector3(0.09f, 0.03f, 0.06f), M(new Color(0.92f, 0.80f, 0.22f)));
            m.Box(new Vector3(0.55f, 0.972f, -0.255f), new Vector3(0.09f, 0.006f, 0.06f), M(new Color(0.22f, 0.45f, 0.22f)));
        }

        /// <summary>One of the two doors under the sink's bowl, shut (<paramref name="left"/>: hinged at its left edge).</summary>
        public static void SinkDoor(RaidMesh m, in KitchenTone t, bool left)
        {
            float x0 = left ? SinkDoorX0 : SinkDoorMid + 0.002f, x1 = left ? SinkDoorMid - 0.002f : SinkDoorX1;
            KitchenCupboardDoor(m, t, x0, x1, SinkDoorY0, SinkDoorY1, SinkDoorFront, left ? x1 - 0.035f : x0 + 0.035f, SinkDoorY1 - 0.1f);
        }

        /// <summary>The cold tap's cross-head with its blue cap, drawn round the origin (the sink turns it as the tap opens).</summary>
        public static void SinkTapHead(RaidMesh m) => KitchenTapHead(m, Vector3.zero, TapColdBlue);

        private static void KitchenTapHead(RaidMesh m, Vector3 at, Color cap)
        {
            Surf chrome = Mt(Chrome);
            m.Box(at, new Vector3(0.07f, 0.014f, 0.014f), chrome);
            m.Box(at, new Vector3(0.014f, 0.014f, 0.07f), chrome);
            m.Cylinder(at + new Vector3(0f, 0.007f, 0f), 0.011f, 0.005f, 8, G(cap));
        }

        /// <summary>The running water: a unit-long, unit-radius column hanging down from the origin with a denser core
        /// (the sink scales it to the flow and the drop).</summary>
        public static void SinkWaterColumn(RaidMesh m, in Surf water)
        {
            m.Cylinder(new Vector3(0f, -1f, 0f), 1f, 1f, 10, water, Quaternion.identity, 1f, false);
            m.Cylinder(new Vector3(0f, -1f, 0f), 0.45f, 1f, 8, water, Quaternion.identity, 1f, false);
        }

        /// <summary>The little splash where the running water lands, round the origin.</summary>
        public static void SinkSplash(RaidMesh m, in Surf water)
            => m.Ellipsoid(Vector3.zero, new Vector3(0.06f, 0.012f, 0.06f), water, 10, 3);

        /// <summary>The water standing in the basin: a thin sheet the size of the bowl, round the origin.</summary>
        public static void SinkBasinWater(RaidMesh m, in Surf water)
            => m.Box(Vector3.zero, new Vector3(SinkBowlW - 0.01f, 0.004f, SinkBowlD - 0.01f), water);

        /// <summary>The sink's rubber plug on its chain, drawn round the drain: pushed in (<paramref name="inDrain"/>),
        /// or lying beside it with the chain running up the back of the bowl to the overflow.</summary>
        public static void SinkPlug(RaidMesh m, bool inDrain)
        {
            Surf rubber = M(new Color(0.08f, 0.08f, 0.09f)), chain = Mt(Chrome);
            const float back = -SinkBowlD * 0.5f + 0.008f;          // the bowl's back wall, from the drain
            const float overflow = 0.915f - SinkBowlBottom;         // the overflow's height above the bowl's floor
            Vector3 plug = inDrain ? Vector3.zero : new Vector3(0.12f, 0f, -0.09f);
            m.Cylinder(plug, 0.03f, 0.008f, 12, rubber);
            m.Box(plug + new Vector3(0f, 0.012f, 0f), new Vector3(0.012f, 0.008f, 0.004f), chain);
            var foot = new Vector3(plug.x * 0.5f, 0.002f, back + 0.004f);
            m.Rod(plug + new Vector3(0f, 0.012f, 0f), foot, 0.004f, chain);
            m.Rod(foot, new Vector3(0f, overflow - 0.008f, back), 0.004f, chain);
        }

        /// <summary>A couple of dirty plates with a fork on them and a mug on its side, left in the bowl and drawn round the
        /// drain — clear of the drain itself, the spot where the pulled plug lies (<see cref="SinkPlug"/>) and the spout's stream.</summary>
        public static void DirtyDishes(RaidMesh m, System.Random rng)
        {
            Surf plate = G(KitchenPlate);
            m.Cylinder(new Vector3(-0.12f, 0.003f, 0f), 0.083f, 0.012f, 14, plate);
            m.Cylinder(new Vector3(-0.115f, 0.015f, 0.01f), 0.078f, 0.012f, 14, plate);
            m.FlatBlob(new Vector3(-0.105f, 0.0275f, 0.025f), 0.045f, 0.03f, 30f, 0.4f, rng, M(new Color(0.45f, 0.34f, 0.22f)));
            m.Box(new Vector3(-0.125f, 0.0295f, -0.03f), new Vector3(0.016f, 0.004f, 0.13f), 70f, Mt(Chrome));
            m.CylinderX(new Vector3(0.1f, 0.036f, 0.09f), 0.036f, 0.085f, 10, G(new Color(0.85f, 0.82f, 0.62f)), -30f);
        }

        // A chrome-wire dish rack with a few plates standing in it and a mug upside down.
        private static void KitchenDishRack(RaidMesh m, System.Random rng, Vector3 at)
        {
            Surf wire = Mt(Chrome * 0.95f);
            const float w = 0.36f, d = 0.28f;
            m.Box(at + new Vector3(0f, 0.006f, -d * 0.5f), new Vector3(w, 0.008f, 0.008f), wire);
            m.Box(at + new Vector3(0f, 0.006f, d * 0.5f), new Vector3(w, 0.008f, 0.008f), wire);
            m.Box(at + new Vector3(-w * 0.5f, 0.006f, 0f), new Vector3(0.008f, 0.008f, d), wire);
            m.Box(at + new Vector3(w * 0.5f, 0.006f, 0f), new Vector3(0.008f, 0.008f, d), wire);
            for (int k = 0; k < 6; k++)
            {
                float x = -w * 0.5f + 0.04f + k * 0.056f;
                m.Box(at + new Vector3(x, 0.045f, -d * 0.5f), new Vector3(0.006f, 0.08f, 0.006f), wire);
                m.Box(at + new Vector3(x, 0.045f, d * 0.5f), new Vector3(0.006f, 0.08f, 0.006f), wire);
            }
            int plates = 2 + rng.Next(2);
            for (int k = 0; k < plates; k++)
                m.CylinderX(at + new Vector3(-w * 0.5f + 0.068f + k * 0.056f, 0.112f, 0f), 0.1f, 0.012f, 14, G(KitchenPlate));
            m.Cylinder(at + new Vector3(w * 0.5f - 0.06f, 0.008f, -0.06f), 0.04f, 0.09f, 10, G(V(KitchenMugColours[0], rng, 0.06f)));
        }
        #endregion

        #region Counter
        // The kitchen counter (HomeFurniture, 1.2 long): two drawers under the worktop over a double cupboard.
        internal const float CounterLength = 1.2f;
        internal const float CounterFront = 0.29f;                           // the doors' and drawers' front face
        internal const float CounterDoorY0 = 0.118f, CounterDoorY1 = 0.72f;
        internal const float CounterDrawerY0 = 0.747f, CounterDrawerY1 = 0.868f;
        internal const float CounterDrawerTravel = 0.32f;                    // how far a drawer pulls out

        /// <summary>The kitchen counter's fixed body: base cabinets with a shelf inside and two drawer bays over the
        /// cupboard, the worktop, and a few things on it at either end — the middle of the back is kept clear for
        /// the kitchen radio. The doors and drawers are drawn by <see cref="CounterDoor"/> / <see cref="CounterDrawer"/>.</summary>
        public static void KitchenCounterBody(RaidMesh m, System.Random rng, in KitchenTone t)
        {
            const float len = CounterLength;
            KitchenCarcass(m, t, len, CounterDrawerY1 + 0.002f);
            Surf inside = M(t.Inside), cab = M(t.Cabinet);
            m.Box(new Vector3(0f, 0.42f, -0.04f), new Vector3(len - 0.036f, 0.016f, 0.48f), inside);       // the shelf
            m.Box(new Vector3(0f, 0.735f, -0.02f), new Vector3(len - 0.036f, 0.016f, 0.56f), inside);      // the drawers' deck
            m.Box(new Vector3(0f, 0.735f, 0.262f), new Vector3(len - 0.036f, 0.02f, 0.02f), cab);
            m.Box(new Vector3(0f, 0.8075f, -0.02f), new Vector3(0.016f, 0.124f, 0.56f), inside);           // between the drawers
            m.Box(new Vector3(0f, 0.8075f, 0.262f), new Vector3(0.016f, 0.124f, 0.02f), cab);
            KitchenWorktop(m, t, len);

            // Add a small wooden chalkboard sign on the worktop with a faint chalk scribble.
            // The board uses the sign material with a dark wood background and light chalk text.
            {
                const float boardHeight = 0.04f; // thin board
                const string boardText = "Today's specials";
                // Dark wood colour for the board background
                Color boardBg = new Color(0.22f, 0.18f, 0.15f);
                // Chalk colour for the text (off‑white, slightly faded)
                Color boardFg = new Color(0.9f, 0.9f, 0.85f);
                Surf boardSurf = DecorMaterials.Sign(boardText, boardBg, boardFg, out float aspect);
                // Width based on aspect ratio of the sign texture
                float boardWidth = boardHeight * aspect;
                // Position the board near the front of the counter, on the worktop surface.
                // CounterFront constant defines the front face Z position.
                var boardPos = new Vector3(-0.4f, 0.94f, CounterFront - 0.02f);
                // Create the thin board box (back side) – colour matches the background.
                m.Box(boardPos, new Vector3(boardWidth, boardHeight, 0.01f), M(boardBg));
                // Upright the sign texture on the front face.
                m.Upright(boardPos + new Vector3(0f, boardHeight * 0.5f, 0.005f), Vector3.forward, boardWidth, boardHeight, boardSurf, new Rect(0f, 0f, 1f, 1f));
            }

            // A crock of wooden spoons at the far end, beyond the radio.
            Surf wood = M(new Color(0.55f, 0.38f, 0.20f));
            m.Cylinder(new Vector3(-0.53f, 0.94f, -0.2f), 0.05f, 0.13f, 10, G(V(new Color(0.82f, 0.80f, 0.70f), rng)));
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f;
                var b = new Vector3(-0.53f + Mathf.Sin(a) * 0.015f, 1.0f, -0.2f + Mathf.Cos(a) * 0.015f);
                Vector3 e = b + new Vector3(Mathf.Sin(a) * 0.03f, 0.2f, Mathf.Cos(a) * 0.03f);
                m.Rod(b, e, 0.014f, wood);
                m.Ellipsoid(e, new Vector3(0.035f, 0.05f, 0.02f), wood, 6, 3);
            }
            // A bread bin and the tea and sugar tins along the back, a kettle at the front.
            if (Chance(rng, 0.7))
            {
                Color bin = V(Chance(rng, 0.5) ? new Color(0.62f, 0.16f, 0.12f) : new Color(0.84f, 0.80f, 0.68f), rng, 0.05f);
                m.Box(new Vector3(0.36f, 1.02f, -0.17f), new Vector3(0.3f, 0.16f, 0.2f), G(bin));
                m.Box(new Vector3(0.36f, 1.07f, -0.069f), new Vector3(0.28f, 0.004f, 0.002f), M(bin * 0.6f));
                m.Box(new Vector3(0.36f, 1.05f, -0.064f), new Vector3(0.08f, 0.014f, 0.012f), Mt(Chrome));
            }
            if (Chance(rng, 0.6))
                for (int i = 0; i < 2; i++)
                {
                    var at = new Vector3(0.06f + i * 0.09f, 0.94f, -0.23f);
                    float th = 0.15f - i * 0.03f;
                    m.Cylinder(at, 0.04f, th, 10, Mt(new Color(0.75f, 0.74f, 0.70f)));
                    m.Cylinder(at + new Vector3(0f, th, 0f), 0.042f, 0.015f, 10, M(new Color(0.15f, 0.15f, 0.15f)));
                }
            if (Chance(rng, 0.6))
            {
                var at = new Vector3(0.3f, 0.94f, 0.12f);
                Surf steel = Mt(Chrome * 0.92f), black = M(Black);
                m.Cylinder(at, 0.08f, 0.15f, 12, steel, Quaternion.identity, 0.75f);
                m.Cylinder(at + new Vector3(0f, 0.15f, 0f), 0.025f, 0.02f, 8, black);
                m.Rod(at + new Vector3(0.06f, 0.05f, 0f), at + new Vector3(0.13f, 0.14f, 0f), 0.02f, steel, 6);
                m.Box(at + new Vector3(-0.09f, 0.11f, 0f), new Vector3(0.02f, 0.1f, 0.025f), black);
                m.Box(at + new Vector3(-0.065f, 0.165f, 0f), new Vector3(0.05f, 0.015f, 0.025f), black);
            }
        }

        /// <summary>One of the counter's two cupboard doors, shut. Both hang on their left (-X) edges — the left door at the
        /// counter's end, the right one at the middle, so in the corner by the sink it swings clear of the sink's doors —
        /// with the knob by the free edge.</summary>
        public static void CounterDoor(RaidMesh m, in KitchenTone t, bool left)
        {
            const float h = CounterLength * 0.5f;
            float x0 = left ? -h + 0.002f : 0.003f, x1 = left ? -0.003f : h - 0.002f;
            KitchenCupboardDoor(m, t, x0, x1, CounterDoorY0, CounterDoorY1, CounterFront, x1 - 0.045f, CounterDoorY1 - 0.08f);
        }

        /// <summary>One of the two drawers under the counter's worktop, shut: its front and brass pull, its box, and
        /// what's kept in it for show — a cutlery tray (<paramref name="cutlery"/>) or a kitchen drawer's odds and ends.</summary>
        public static void CounterDrawer(RaidMesh m, in KitchenTone t, bool left, bool cutlery)
        {
            const float h = CounterLength * 0.5f;
            float x0 = left ? -h + 0.002f : 0.01f, x1 = left ? -0.01f : h - 0.002f;
            float cx = (x0 + x1) * 0.5f, cy = (CounterDrawerY0 + CounterDrawerY1) * 0.5f;
            m.Box(new Vector3(cx, cy, CounterFront - 0.01f), new Vector3(x1 - x0, CounterDrawerY1 - CounterDrawerY0, 0.02f), M(t.Door));
            Surf brass = Mt(Brass);
            m.Box(new Vector3(cx, cy, CounterFront + 0.022f), new Vector3(0.12f, 0.014f, 0.012f), brass);
            m.Box(new Vector3(cx - 0.05f, cy, CounterFront + 0.008f), new Vector3(0.012f, 0.012f, 0.018f), brass);
            m.Box(new Vector3(cx + 0.05f, cy, CounterFront + 0.008f), new Vector3(0.012f, 0.012f, 0.018f), brass);

            Surf box = M(t.Inside * 1.05f);
            float bx0 = x0 + 0.03f, bx1 = x1 - 0.03f, bw = bx1 - bx0, bcx = (bx0 + bx1) * 0.5f;
            const float z0 = -0.21f, z1 = CounterFront - 0.02f, y0 = 0.752f, y1 = 0.852f;
            float bz = (z0 + z1) * 0.5f, bd = z1 - z0, by = (y0 + y1) * 0.5f;
            m.Box(new Vector3(bx0 + 0.006f, by, bz), new Vector3(0.012f, y1 - y0, bd), box);
            m.Box(new Vector3(bx1 - 0.006f, by, bz), new Vector3(0.012f, y1 - y0, bd), box);
            m.Box(new Vector3(bcx, by, z0 + 0.006f), new Vector3(bw, y1 - y0, 0.012f), box);
            m.Box(new Vector3(bcx, y0 + 0.004f, bz), new Vector3(bw, 0.008f, bd), box);
            if (cutlery) KitchenCutleryTray(m, new Vector3(bcx, y0 + 0.008f, bz), bw - 0.03f, bd - 0.04f);
            else KitchenOddments(m, new Vector3(bcx, y0 + 0.008f, bz), bw - 0.04f, bd - 0.06f);
        }

        // A grey plastic cutlery tray: knives, forks, spoons and teaspoons in their slots.
        private static void KitchenCutleryTray(RaidMesh m, Vector3 at, float w, float d)
        {
            Surf tray = M(new Color(0.62f, 0.63f, 0.64f)), steel = Mt(Chrome);
            const int slots = 4;
            float sw = w / slots;
            m.Box(at + new Vector3(0f, 0.002f, 0f), new Vector3(w, 0.004f, d), tray);
            for (int i = 1; i < slots; i++)
                m.Box(at + new Vector3(-w * 0.5f + sw * i, 0.02f, 0f), new Vector3(0.006f, 0.04f, d), tray);
            m.Box(at + new Vector3(0f, 0.02f, -d * 0.5f), new Vector3(w, 0.04f, 0.006f), tray);
            m.Box(at + new Vector3(0f, 0.02f, d * 0.5f), new Vector3(w, 0.04f, 0.006f), tray);
            for (int i = 0; i < slots; i++)
            {
                float x = -w * 0.5f + sw * (i + 0.5f);
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = at + new Vector3(x + (k - 1) * sw * 0.22f, 0.007f + k * 0.003f, 0f);
                    m.Box(p, new Vector3(0.014f, 0.004f, d * 0.6f), steel);
                    if (i == 1) m.Box(p + new Vector3(0f, 0f, d * 0.34f), new Vector3(0.024f, 0.004f, 0.06f), steel);
                    else if (i >= 2) m.Ellipsoid(p + new Vector3(0f, 0f, d * 0.33f), new Vector3(i == 2 ? 0.032f : 0.022f, 0.008f, i == 2 ? 0.046f : 0.032f), steel, 6, 3);
                }
            }
        }

        // The odds and ends a kitchen drawer collects: string, matches, a roll of tape, a pencil, a corkscrew.
        private static void KitchenOddments(RaidMesh m, Vector3 at, float w, float d)
        {
            m.Ellipsoid(at + new Vector3(-w * 0.3f, 0.03f, -d * 0.2f), new Vector3(0.07f, 0.06f, 0.07f), M(new Color(0.80f, 0.72f, 0.52f)), 7, 4);
            m.Box(at + new Vector3(w * 0.05f, 0.008f, d * 0.25f), new Vector3(0.05f, 0.016f, 0.035f), 15f, M(new Color(0.75f, 0.20f, 0.15f)));
            m.Cylinder(at + new Vector3(w * 0.3f, 0f, -d * 0.15f), 0.04f, 0.04f, 10, M(new Color(0.85f, 0.82f, 0.70f)));
            m.Box(at + new Vector3(-w * 0.05f, 0.005f, -d * 0.05f), new Vector3(0.008f, 0.008f, 0.16f), -25f, M(new Color(0.90f, 0.75f, 0.20f)));
            m.Box(at + new Vector3(w * 0.32f, 0.006f, d * 0.28f), new Vector3(0.1f, 0.012f, 0.03f), 40f, M(new Color(0.30f, 0.20f, 0.12f)));
        }
        #endregion

        #region Wall cupboards
        // The wall cupboards over the worktop (drawn into a wall's mesh, so they vanish with it): the frame's origin
        // is on the wall face at floor level, +Z pointing into the room.
        internal const float WallCupLength = 1.2f, WallCupDepth = 0.34f;
        internal const float WallCupY0 = 1.47f, WallCupY1 = 2.17f;

        /// <summary>The wall cupboards' fixed body: the carcass with a shelf and a cornice, and the plates, bowls, mugs,
        /// glasses and teapot put away inside. The doors are drawn by <see cref="WallCupboardDoor"/>.</summary>
        public static void WallCupboardBody(RaidMesh m, System.Random rng, in KitchenTone t)
        {
            const float len = WallCupLength, h = len * 0.5f, d = WallCupDepth;
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

            // The crockery: plates and bowls stacked below with the mugs, glasses and a teapot on the shelf.
            float floor = WallCupY0 + 0.018f, upper = shelf + 0.007f;
            Surf plate = G(KitchenPlate);
            for (int i = 0; i < 4; i++) m.Cylinder(new Vector3(-0.36f, floor + i * 0.014f, 0.17f), 0.11f, 0.012f, 14, plate);
            for (int i = 0; i < 3; i++) m.Cylinder(new Vector3(-0.08f, floor + i * 0.03f, 0.17f), 0.075f, 0.03f, 12, plate, Quaternion.identity, 1.35f);
            for (int i = 0; i < 3; i++)
                m.Cylinder(new Vector3(0.16f + i * 0.1f, floor, 0.19f + (i % 2) * 0.05f), 0.04f, 0.09f, 10, G(V(KitchenMugColours[i], rng, 0.06f)));
            Surf glass = Gs(new Color(0.55f, 0.66f, 0.70f));
            for (int i = 0; i < 4; i++) m.Cylinder(new Vector3(-0.42f + i * 0.09f, upper, 0.2f), 0.035f, 0.12f, 10, glass);
            Surf pot = G(V(new Color(0.86f, 0.84f, 0.78f), rng, 0.05f));
            var tp = new Vector3(0.2f, upper, 0.17f);
            m.Cylinder(tp, 0.08f, 0.11f, 12, pot, Quaternion.identity, 0.8f);
            m.Cylinder(tp + new Vector3(0f, 0.11f, 0f), 0.02f, 0.02f, 8, pot);
            m.Rod(tp + new Vector3(0.06f, 0.04f, 0f), tp + new Vector3(0.12f, 0.1f, 0f), 0.02f, pot, 6);
            m.Box(tp + new Vector3(-0.09f, 0.06f, 0f), new Vector3(0.02f, 0.07f, 0.02f), pot);
        }

        /// <summary>One of the wall cupboards' two doors, shut (<paramref name="left"/>: hinged at the left end).</summary>
        public static void WallCupboardDoor(RaidMesh m, in KitchenTone t, bool left)
        {
            const float h = WallCupLength * 0.5f;
            float x0 = left ? -h + 0.002f : 0.003f, x1 = left ? -0.003f : h - 0.002f;
            KitchenCupboardDoor(m, t, x0, x1, WallCupY0 + 0.004f, WallCupY1 - 0.004f, WallCupDepth + 0.02f, left ? -0.045f : 0.045f, WallCupY0 + 0.09f);
        }
        #endregion

        #region Gas range
        // The gas range (the CookingStation, 0.76 wide, standing unturned). Burners are listed as the cook facing it
        // sees them — their left is the range's +X.
        internal static readonly Vector3[] StoveBurners =
        {
            new Vector3(0.18f, 0.948f, 0.13f), new Vector3(-0.18f, 0.948f, 0.13f),     // front-left, front-right
            new Vector3(0.18f, 0.948f, -0.13f), new Vector3(-0.18f, 0.948f, -0.13f),   // back-left, back-right
        };
        internal static readonly float[] StoveKnobX = { 0.09f, -0.09f, 0.27f, -0.27f };   // each burner's knob on the back-guard
        internal const float StoveKnobY = 1.0f, StoveKnobZ = -0.266f;                    // .. in front of its chrome bezel
        internal static readonly Vector3 StoveOvenKnob = new Vector3(0f, 0.835f, 0.294f); // the oven's thermostat
        internal static readonly Vector3 StoveOvenLamp = new Vector3(0.09f, 0.835f, 0.29f);
        internal const float StoveDoorHingeY = 0.215f, StoveDoorFront = 0.32f;         // the oven door hinges down here

        /// <summary>A white enamel gas range's fixed body: the hob's four burners under cast grates, the back-guard
        /// with the knobs' bezels, the control strip with the oven's thermostat bezel and pilot lamp, the broiler
        /// drawer, and the oven — a dark cavity with two wire racks — but not the oven door (<see cref="OvenDoor"/>),
        /// the knobs (<see cref="StoveKnob"/>) or the flames (<see cref="BurnerFlame"/>).</summary>
        public static void GasRange(RaidMesh m, System.Random rng, Color enamel)
        {
            Surf body = G(enamel), dark = M(OvenDark), black = M(Black), chrome = Mt(Chrome);
            m.Box(new Vector3(-0.37f, 0.46f, -0.02f), new Vector3(0.02f, 0.92f, 0.62f), body);
            m.Box(new Vector3(0.37f, 0.46f, -0.02f), new Vector3(0.02f, 0.92f, 0.62f), body);
            m.Box(new Vector3(0f, 0.46f, -0.32f), new Vector3(0.72f, 0.92f, 0.02f), G(enamel * 0.9f));
            m.Box(new Vector3(0f, 0.905f, -0.02f), new Vector3(0.72f, 0.03f, 0.62f), body);
            m.Box(new Vector3(0f, 0.835f, 0.28f), new Vector3(0.72f, 0.11f, 0.02f), body);                 // the control strip
            m.Box(new Vector3(0f, 0.2025f, 0.28f), new Vector3(0.72f, 0.025f, 0.02f), body);               // the rail under the oven
            m.Box(new Vector3(-0.33f, 0.4975f, 0.28f), new Vector3(0.06f, 0.565f, 0.02f), body);
            m.Box(new Vector3(0.33f, 0.4975f, 0.28f), new Vector3(0.06f, 0.565f, 0.02f), body);
            m.Box(new Vector3(0f, 0.12f, 0.28f), new Vector3(0.72f, 0.14f, 0.02f), G(enamel * 0.97f));    // the broiler drawer
            m.Box(new Vector3(0f, 0.15f, 0.3f), new Vector3(0.3f, 0.016f, 0.016f), chrome);
            m.Box(new Vector3(0f, 0.025f, 0.2f), new Vector3(0.72f, 0.05f, 0.1f), black);

            // The oven: a dark enamel cavity with two wire racks on their runners.
            m.Box(new Vector3(-0.295f, 0.4975f, -0.01f), new Vector3(0.01f, 0.565f, 0.56f), dark);
            m.Box(new Vector3(0.295f, 0.4975f, -0.01f), new Vector3(0.01f, 0.565f, 0.56f), dark);
            m.Box(new Vector3(0f, 0.775f, -0.01f), new Vector3(0.6f, 0.01f, 0.56f), dark);
            m.Box(new Vector3(0f, 0.22f, -0.01f), new Vector3(0.6f, 0.01f, 0.56f), dark);
            m.Box(new Vector3(0f, 0.4975f, -0.285f), new Vector3(0.6f, 0.565f, 0.01f), dark);
            Surf wire = Mt(Chrome * 0.85f);
            for (int r = 0; r < 2; r++)
            {
                float y = r == 0 ? 0.36f : 0.55f;
                for (int k = 0; k < 6; k++) m.Box(new Vector3(-0.25f + k * 0.1f, y, -0.01f), new Vector3(0.006f, 0.006f, 0.5f), wire);
                m.Box(new Vector3(0f, y, 0.23f), new Vector3(0.56f, 0.006f, 0.006f), wire);
                m.Box(new Vector3(0f, y, -0.25f), new Vector3(0.56f, 0.006f, 0.006f), wire);
                m.Box(new Vector3(-0.283f, y - 0.008f, -0.01f), new Vector3(0.012f, 0.008f, 0.5f), dark);
                m.Box(new Vector3(0.283f, y - 0.008f, -0.01f), new Vector3(0.012f, 0.008f, 0.5f), dark);
            }
            if (Chance(rng, 0.4)) m.Box(new Vector3(0f, 0.383f, -0.02f), new Vector3(0.36f, 0.04f, 0.28f), M(new Color(0.18f, 0.18f, 0.19f)));

            // The hob: four burners in drip pans under cast-iron grates.
            m.Box(new Vector3(0f, 0.925f, 0f), new Vector3(0.74f, 0.01f, 0.6f), M(new Color(0.18f, 0.18f, 0.19f)));
            Surf pan = Mt(Chrome * 0.75f), cap = M(new Color(0.26f, 0.26f, 0.27f));
            for (int i = 0; i < 4; i++)
            {
                Vector3 b = StoveBurners[i];
                m.Cylinder(new Vector3(b.x, 0.93f, b.z), 0.1f, 0.006f, 14, pan);
                m.Cylinder(new Vector3(b.x, 0.936f, b.z), 0.045f, 0.012f, 12, black);
                m.Cylinder(new Vector3(b.x, 0.948f, b.z), 0.035f, 0.006f, 12, cap);
                m.Cylinder(new Vector3(b.x, 0.956f, b.z), 0.1f, 0.012f, 14, black, Quaternion.identity, 1f, false);
                for (int k = 0; k < 4; k++)
                {
                    float a = 45f + k * 90f, r = a * Mathf.Deg2Rad;
                    m.Box(new Vector3(b.x + Mathf.Sin(r) * 0.075f, 0.962f, b.z + Mathf.Cos(r) * 0.075f), new Vector3(0.012f, 0.012f, 0.05f), a, black);
                }
            }

            // The back-guard with the burner knobs' bezels; the control strip with the oven's.
            m.Box(new Vector3(0f, 1.0f, -0.3f), new Vector3(0.76f, 0.16f, 0.06f), body);
            if (Chance(rng, 0.8f)) m.Cylinder(new Vector3(-0.35f, 0.94f, -0.33f), 0.006f, 0.012f, 8, chrome);
            if (Chance(rng, 0.8f)) m.Cylinder(new Vector3(0.35f, 0.94f, -0.33f), 0.006f, 0.012f, 8, chrome);
            if (Chance(rng, 0.8f)) m.Cylinder(new Vector3(-0.35f, 0.94f, -0.27f), 0.006f, 0.012f, 8, chrome);
            if (Chance(rng, 0.8f)) m.Cylinder(new Vector3(0.35f, 0.94f, -0.27f), 0.006f, 0.012f, 8, chrome);
            Quaternion outward = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 4; i++)
                m.Cylinder(new Vector3(StoveKnobX[i], StoveKnobY, StoveKnobZ - 0.004f), 0.03f, 0.004f, 12, chrome, outward);
            m.Cylinder(StoveOvenKnob - new Vector3(0f, 0f, 0.004f), 0.03f, 0.004f, 12, chrome, outward);
            m.Cylinder(StoveOvenLamp, 0.009f, 0.004f, 8, M(new Color(0.35f, 0.08f, 0.05f)), outward);
            m.Box(new Vector3(-0.22f, 0.835f, 0.292f), new Vector3(0.1f, 0.025f, 0.004f), chrome);

            // Now and then a pan left on the back-right burner.
            if (Chance(rng, 0.5))
            {
                Vector3 b = StoveBurners[3];
                Surf steel = Mt(new Color(0.45f, 0.45f, 0.46f));
                m.Cylinder(new Vector3(b.x, 0.968f, b.z), 0.1f, 0.12f, 14, steel);
                m.Cylinder(new Vector3(b.x, 1.088f, b.z), 0.102f, 0.008f, 14, steel);
                m.Cylinder(new Vector3(b.x, 1.096f, b.z), 0.015f, 0.02f, 8, black);
                m.Box(new Vector3(b.x - 0.15f, 1.04f, b.z), new Vector3(0.1f, 0.02f, 0.025f), black);
            }
        }

        /// <summary>The oven door, shut: enamel outside with a dark window and a chrome bar handle, a dark liner on
        /// the inside. It hinges down about its bottom edge (<see cref="StoveDoorHingeY"/>, <see cref="StoveDoorFront"/>).</summary>
        public static void OvenDoor(RaidMesh m, Color enamel)
        {
            const float y0 = StoveDoorHingeY, y1 = 0.775f, z1 = StoveDoorFront, z0 = z1 - 0.035f;
            float cy = (y0 + y1) * 0.5f;
            Surf chrome = Mt(Chrome);
            m.Box(new Vector3(0f, cy, (z0 + z1) * 0.5f), new Vector3(0.66f, y1 - y0, z1 - z0), G(enamel * 0.98f));
            m.Box(new Vector3(0f, 0.52f, z1 + 0.0005f), new Vector3(0.42f, 0.22f, 0.002f), Gs(new Color(0.05f, 0.05f, 0.06f)));
            m.Box(new Vector3(0f, 0.632f, z1 + 0.002f), new Vector3(0.44f, 0.01f, 0.004f), chrome);
            m.Box(new Vector3(0f, 0.408f, z1 + 0.002f), new Vector3(0.44f, 0.01f, 0.004f), chrome);
            m.Box(new Vector3(-0.215f, 0.52f, z1 + 0.002f), new Vector3(0.01f, 0.234f, 0.004f), chrome);
            m.Box(new Vector3(0.215f, 0.52f, z1 + 0.002f), new Vector3(0.01f, 0.234f, 0.004f), chrome);
            m.Box(new Vector3(0f, 0.72f, z1 + 0.028f), new Vector3(0.52f, 0.022f, 0.022f), chrome);
            m.Box(new Vector3(-0.22f, 0.72f, z1 + 0.01f), new Vector3(0.02f, 0.02f, 0.02f), chrome);
            m.Box(new Vector3(0.22f, 0.72f, z1 + 0.01f), new Vector3(0.02f, 0.02f, 0.02f), chrome);
            m.Box(new Vector3(0f, cy, z0 - 0.003f), new Vector3(0.56f, 0.46f, 0.006f), M(OvenDark));
        }

        /// <summary>The oven's window lit from within, laid over the dark glass while the oven is on (drawn with the
        /// door shut, so it rides on the door).</summary>
        public static void OvenWindowGlow(RaidMesh m)
            => m.Box(new Vector3(0f, 0.52f, StoveDoorFront + 0.004f), new Vector3(0.41f, 0.21f, 0.002f), Gl(OvenGlow));

        /// <summary>The oven's element glowing along the cavity's roof, and its pilot lamp lit.</summary>
        public static void OvenHeatGlow(RaidMesh m)
        {
            Surf hot = Gl(new Color(1f, 0.32f, 0.08f));
            for (int i = 0; i < 3; i++) m.Box(new Vector3(0f, 0.762f, -0.2f + i * 0.15f), new Vector3(0.5f, 0.012f, 0.012f), hot);
            m.Box(new Vector3(-0.25f, 0.762f, -0.125f), new Vector3(0.012f, 0.012f, 0.15f), hot);
            m.Box(new Vector3(0.25f, 0.762f, 0.025f), new Vector3(0.012f, 0.012f, 0.15f), hot);
            m.Cylinder(StoveOvenLamp + new Vector3(0f, 0f, 0.003f), 0.0095f, 0.003f, 8, Gl(new Color(1f, 0.45f, 0.12f)), Quaternion.Euler(90f, 0f, 0f));
        }

        /// <summary>A lit gas burner's ring of blue flame, drawn round the top of the burner head (the origin).</summary>
        public static void BurnerFlame(RaidMesh m)
        {
            Surf ring = Gl(new Color(0.22f, 0.38f, 0.95f)), tongue = Gl(new Color(0.42f, 0.62f, 1f));
            m.Cylinder(Vector3.zero, 0.046f, 0.006f, 16, ring, Quaternion.identity, 1f, false);
            for (int k = 0; k < 12; k++)
            {
                float a = k * 30f, r = a * Mathf.Deg2Rad;
                m.Box(new Vector3(Mathf.Sin(r) * 0.05f, 0.008f, Mathf.Cos(r) * 0.05f), new Vector3(0.01f, 0.016f, 0.004f), a, tongue);
            }
        }

        /// <summary>A black control knob with a white pointer, drawn round the origin and standing out along +Z.</summary>
        public static void StoveKnob(RaidMesh m)
        {
            m.Cylinder(Vector3.zero, 0.022f, 0.026f, 10, M(Black), Quaternion.Euler(90f, 0f, 0f));
            m.Box(new Vector3(0f, 0.01f, 0.0265f), new Vector3(0.005f, 0.016f, 0.002f), M(new Color(0.9f, 0.9f, 0.88f)));
        }
        #endregion

        #region Fridge-freezer
        // The fridge-freezer (HomeFurniture, 0.72 wide, 0.70 deep, 1.72 high): a small freezer over a tall fridge,
        // both doors hinged on the cabinet's -X edge (on the right as you face it) with their handles on the left.
        internal const float FridgeDoorFront = 0.32f, FridgeHingeX = -0.36f;
        internal const float FridgeDoorY0 = 0.105f, FridgeDoorY1 = 1.235f, FreezerDoorY0 = 1.255f, FreezerDoorY1 = 1.70f;
        internal static readonly Vector3 FridgeLampLens = new Vector3(0f, 1.168f, -0.25f);   // under the divider, at the back

        /// <summary>A 1990s fridge-freezer's cabinet without its doors: white enamel outside over a grilled plinth, a
        /// pale liner inside, glass shelves, a crisper drawer and the lamp's housing in the fridge, an ice tray and frost
        /// in the freezer above. The doors are drawn by <see cref="FridgeDoor"/>.</summary>
        public static void FridgeCabinet(RaidMesh m, Color enamel)
        {
            Surf body = G(enamel);
            m.Box(new Vector3(0f, 0.045f, -0.02f), new Vector3(0.66f, 0.09f, 0.58f), M(new Color(0.12f, 0.12f, 0.12f)));
            m.Box(new Vector3(0f, 0.047f, 0.265f), new Vector3(0.64f, 0.066f, 0.01f), M(new Color(0.20f, 0.20f, 0.21f)));
            for (int r = 0; r < 3; r++) m.Box(new Vector3(0f, 0.027f + r * 0.02f, 0.271f), new Vector3(0.58f, 0.006f, 0.004f), M(Black));

            m.Box(new Vector3(-0.35f, 0.86f, -0.04f), new Vector3(0.02f, 1.72f, 0.62f), body);
            m.Box(new Vector3(0.35f, 0.86f, -0.04f), new Vector3(0.02f, 1.72f, 0.62f), body);
            m.Box(new Vector3(0f, 1.71f, -0.04f), new Vector3(0.68f, 0.02f, 0.62f), body);
            m.Box(new Vector3(0f, 0.905f, -0.34f), new Vector3(0.68f, 1.63f, 0.02f), G(enamel * 0.9f));
            m.Box(new Vector3(0f, 0.1f, -0.04f), new Vector3(0.68f, 0.02f, 0.62f), body);
            m.Box(new Vector3(0f, 1.245f, -0.04f), new Vector3(0.68f, 0.03f, 0.62f), body);
            m.Box(new Vector3(0f, 1.245f, 0.272f), new Vector3(0.68f, 0.014f, 0.004f), M(new Color(0.30f, 0.30f, 0.30f)));
            FridgeLinerBox(m, 0.11f, 1.23f, M(FridgeLinerWhite));
            FridgeLinerBox(m, 1.26f, 1.70f, M(FreezerFrost));

            // The fridge: two glass shelves, a crisper drawer and the lamp housing.
            Surf shelf = Gs(new Color(0.62f, 0.72f, 0.74f)), trim = Mt(Chrome);
            for (int i = 0; i < 2; i++)
            {
                float y = i == 0 ? 0.48f : 0.84f;
                m.Box(new Vector3(0f, y, -0.075f), new Vector3(0.66f, 0.008f, 0.52f), shelf);
                m.Box(new Vector3(0f, y, 0.188f), new Vector3(0.66f, 0.012f, 0.008f), trim);
            }
            m.Box(new Vector3(0f, 0.2f, -0.06f), new Vector3(0.62f, 0.17f, 0.5f), G(new Color(0.80f, 0.88f, 0.84f)));
            m.Box(new Vector3(0f, 0.255f, 0.192f), new Vector3(0.3f, 0.02f, 0.008f), M(new Color(0.70f, 0.76f, 0.74f)));
            m.Box(FridgeLampLens + new Vector3(0f, 0.022f, 0f), new Vector3(0.16f, 0.04f, 0.08f), M(FridgeLinerWhite * 0.95f));
            m.Box(FridgeLampLens, new Vector3(0.12f, 0.004f, 0.06f), M(new Color(0.78f, 0.80f, 0.82f)));

            // The freezer: an ice-cube tray and a crust of frost on the back.
            m.Box(new Vector3(0.12f, 1.275f, -0.1f), new Vector3(0.12f, 0.025f, 0.28f), M(new Color(0.92f, 0.94f, 0.96f)));
            Surf frost = M(new Color(0.96f, 0.98f, 1f));
            m.Ellipsoid(new Vector3(-0.12f, 1.5f, -0.322f), new Vector3(0.24f, 0.14f, 0.02f), frost, 8, 4);
            m.Ellipsoid(new Vector3(0.16f, 1.6f, -0.322f), new Vector3(0.16f, 0.09f, 0.016f), frost, 8, 4);
        }

        // The inside of one compartment, y0..y1: its sides, back, roof and floor.
        private static void FridgeLinerBox(RaidMesh m, float y0, float y1, in Surf s)
        {
            float cy = (y0 + y1) * 0.5f, h = y1 - y0;
            m.Box(new Vector3(-0.337f, cy, -0.04f), new Vector3(0.006f, h, 0.6f), s);
            m.Box(new Vector3(0.337f, cy, -0.04f), new Vector3(0.006f, h, 0.6f), s);
            m.Box(new Vector3(0f, cy, -0.327f), new Vector3(0.668f, h, 0.006f), s);
            m.Box(new Vector3(0f, y1 - 0.003f, -0.04f), new Vector3(0.668f, 0.006f, 0.6f), s);
            m.Box(new Vector3(0f, y0 + 0.003f, -0.04f), new Vector3(0.668f, 0.006f, 0.6f), s);
        }

        /// <summary>The fridge's door — or, with <paramref name="freezer"/>, the freezer's above it — shut: enamel with a
        /// chrome handle by its free edge (and on the fridge's, a few magnets and notes), and on its inside a liner,
        /// a gasket and door shelves. Both hinge on the cabinet's left edge (<see cref="FridgeHingeX"/>).</summary>
        public static void FridgeDoor(RaidMesh m, System.Random rng, Color enamel, bool freezer)
        {
            float y0 = freezer ? FreezerDoorY0 : FridgeDoorY0, y1 = freezer ? FreezerDoorY1 : FridgeDoorY1;
            float cy = (y0 + y1) * 0.5f, h = y1 - y0;
            const float z1 = FridgeDoorFront, z0 = z1 - 0.05f;
            m.Box(new Vector3(0f, cy, (z0 + z1) * 0.5f), new Vector3(0.72f, h, z1 - z0), G(enamel));

            Surf chrome = Mt(Chrome);
            float hy = freezer ? y0 + 0.12f : y1 - 0.24f, hl = freezer ? 0.18f : 0.34f;
            m.Box(new Vector3(0.30f, hy, z1 + 0.025f), new Vector3(0.035f, hl, 0.03f), chrome);
            m.Box(new Vector3(0.30f, hy - hl * 0.4f, z1 + 0.006f), new Vector3(0.02f, 0.02f, 0.014f), chrome);
            m.Box(new Vector3(0.30f, hy + hl * 0.4f, z1 + 0.006f), new Vector3(0.02f, 0.02f, 0.014f), chrome);

            // Helper to place a small cylindrical magnet on the door front.
            void AddMagnet(Vector3 at, Color col)
            {
                // a thin chrome disc, tinted to the magnet’s colour
                var mat = Mt(Chrome * 0.7f + col * 0.3f);
                m.Cylinder(at, 0.018f, 0.002f, 12, mat); // disc face
                // tiny raised edge to catch light
                m.Cylinder(at + new Vector3(0f, 0.001f, 0f), 0.018f, 0.0005f, 12, Mt(Chrome));
            }

            // Add 2‑4 random fridge magnets.
            int magCount = 2 + rng.Next(3);

            // Worn hinge strip – thin, slightly dulled chrome along the left edge (hinge side).
            float hingeX = -0.36f + 0.003f;
            m.Box(new Vector3(hingeX, cy, z1 + 0.001f), new Vector3(0.006f, h, 0.004f), Mt(Chrome * 0.6f));

            // Subtle surface scratches on the door front.
            int scratchCount = rng.Next(3); // 0‑2 scratches
            for (int i = 0; i < scratchCount; i++)
            {
                float sx = R(rng, -0.3f, 0.3f);
                float sy = R(rng, y0 + 0.1f, y1 - 0.1f);
                float length = R(rng, 0.02f, 0.06f);
                float angle = R(rng, -15f, 15f);
                var dark = Mt(new Color(0.2f, 0.2f, 0.2f));
                // thin cylinder as scratch
                m.Cylinder(new Vector3(sx, sy, z1 + 0.002f), 0.001f, length, 6, dark, Quaternion.Euler(0f, angle, 0f));
            }
            for (int i = 0; i < magCount; i++)
            {
                float mx = R(rng, -0.25f, 0.25f);
                float my = R(rng, y0 + 0.2f, y1 - 0.15f);
                Color col = new Color(R(rng, 0.4f, 0.9f), R(rng, 0.4f, 0.9f), R(rng, 0.4f, 0.9f));
                AddMagnet(new Vector3(mx, my, z1 + 0.003f), col);
            }

            Surf liner = M(freezer ? FreezerFrost : FridgeLinerWhite), gasket = M(new Color(0.50f, 0.50f, 0.52f));
            m.Box(new Vector3(0f, cy, z0 - 0.002f), new Vector3(0.62f, h - 0.08f, 0.004f), liner);
            m.Box(new Vector3(0f, y1 - 0.025f, z0 - 0.004f), new Vector3(0.68f, 0.016f, 0.008f), gasket);
            m.Box(new Vector3(0f, y0 + 0.025f, z0 - 0.004f), new Vector3(0.68f, 0.016f, 0.008f), gasket);
            m.Box(new Vector3(-0.332f, cy, z0 - 0.004f), new Vector3(0.016f, h - 0.05f, 0.008f), gasket);
            m.Box(new Vector3(0.332f, cy, z0 - 0.004f), new Vector3(0.016f, h - 0.05f, 0.008f), gasket);
            int bins = freezer ? 1 : 3;
            for (int i = 0; i < bins; i++) FridgeDoorBin(m, freezer ? y0 + 0.14f : y0 + 0.2f + i * 0.32f, z0, liner);

            if (freezer) return;
            int notes = 1 + rng.Next(3);
            for (int i = 0; i < notes; i++)
            {
                float x = R(rng, -0.25f, 0.15f), y = R(rng, 0.55f, 1.05f);
                bool paper = Chance(rng, 0.6);
                Color nc = paper ? Paper : new Color(R(rng, 0.2f, 0.8f), R(rng, 0.2f, 0.6f), R(rng, 0.1f, 0.5f));
                m.Box(new Vector3(x, y, z1 + 0.003f), paper ? new Vector3(0.1f, 0.13f, 0.004f) : new Vector3(0.04f, 0.04f, 0.012f),
                    Quaternion.Euler(0f, 0f, R(rng, -12f, 12f)), M(nc));
            }
        }

        // A shelf on the inside of a fridge door: a lip, a floor and two ends.
        private static void FridgeDoorBin(RaidMesh m, float y, float z0, in Surf s)
        {
            m.Box(new Vector3(0f, y, z0 - 0.068f), new Vector3(0.58f, 0.07f, 0.006f), s);
            m.Box(new Vector3(0f, y - 0.035f, z0 - 0.036f), new Vector3(0.58f, 0.006f, 0.07f), s);
            m.Box(new Vector3(-0.29f, y, z0 - 0.036f), new Vector3(0.006f, 0.07f, 0.07f), s);
            m.Box(new Vector3(0.29f, y, z0 - 0.036f), new Vector3(0.006f, 0.07f, 0.07f), s);
        }

        /// <summary>The fridge lamp's lens lit, drawn round the lens (the origin).</summary>
        public static void FridgeLampGlow(RaidMesh m)
            => m.Box(new Vector3(0f, -0.0015f, 0f), new Vector3(0.122f, 0.004f, 0.062f), Gl(new Color(0.92f, 0.96f, 1f)));
        #endregion
    }
}
