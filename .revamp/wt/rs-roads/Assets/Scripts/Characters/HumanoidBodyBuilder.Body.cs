using UnityEngine;

namespace CloneZ
{
    /// <summary>Head, torso, arms and hands. Heights are metres above the soles of a figure facing +Z.</summary>
    internal sealed partial class HumanoidBodyBuilder
    {
        private static readonly Quaternion I = Quaternion.identity;
        private static readonly Vector3 Fwd = Vector3.forward;

        // The short-hair shell every fuller hairstyle starts from; hats are sized to sit over it.
        private static readonly Vector3 HairCentre = new Vector3(0f, 1.718f, -0.024f);
        private static readonly Vector3 HairRadii = new Vector3(0.089f, 0.098f, 0.104f);

        private float _topInflate;

        private bool HatOn =>
            _s.Headwear == HeadwearStyle.Cap || _s.Headwear == HeadwearStyle.CapBackward || _s.Headwear == HeadwearStyle.Beanie
            || _s.Headwear == HeadwearStyle.Helmet || _s.Headwear == HeadwearStyle.WideBrim;

        // ── Head ─────────────────────────────────────────────────────────────────────────────────────

        private void BuildHead()
        {
            const int head = (int)HumanBone.Head, neck = (int)HumanBone.Neck;
            const HumanoidSlot skin = HumanoidSlot.Skin;
            float n = _p.Neck;

            Limb(skin, neck, new Vector3(0f, 1.425f, -0.016f), new Vector3(0f, 1.625f, -0.004f), Fwd,
                new[] { new Key(0f, n + 0.004f, n + 0.008f), new Key(0.5f, n, n + 0.003f, 0.002f), new Key(1f, n * 0.96f, n) }, seg: 14);

            Ellipsoid(skin, head, new Vector3(0f, 1.695f, -0.008f), new Vector3(0.079f, 0.104f, 0.097f), I, 24, 16);   // cranium
            Ellipsoid(skin, head, new Vector3(0f, 1.733f, 0.024f), new Vector3(0.066f, 0.048f, 0.060f), I, 16, 10);    // forehead
            Ellipsoid(skin, head, new Vector3(0f, 1.630f, 0.012f), new Vector3(0.062f, 0.058f, 0.070f), I, 18, 10);    // jaw and cheeks
            Ellipsoid(skin, head, new Vector3(0f, 1.670f, 0.088f), new Vector3(0.011f, 0.017f, 0.012f), Quaternion.Euler(-12f, 0f, 0f), 10, 6);

            for (int side = -1; side <= 1; side += 2)
            {
                Ellipsoid(skin, head, new Vector3(side * 0.079f, 1.685f, -0.010f), new Vector3(0.012f, 0.028f, 0.019f), Quaternion.Euler(0f, side * 12f, 0f), 10, 6);
                Ellipsoid(HumanoidSlot.Eyes, head, new Vector3(side * 0.031f, 1.704f, 0.081f), new Vector3(0.011f, 0.008f, 0.007f), I, 10, 6);
                Ellipsoid(HumanoidSlot.Hair, head, new Vector3(side * 0.032f, 1.723f, 0.083f), new Vector3(0.017f, 0.0045f, 0.007f), Quaternion.Euler(0f, 0f, side * -8f), 8, 4);
            }

            if (_s.Zombie) Ellipsoid(HumanoidSlot.Mouth, head, new Vector3(0f, 1.624f, 0.077f), new Vector3(0.022f, 0.014f, 0.012f), I, 12, 6);
            else Ellipsoid(HumanoidSlot.Mouth, head, new Vector3(0f, 1.628f, 0.083f), new Vector3(0.018f, 0.0045f, 0.006f), I, 10, 4);

            BuildHair();
        }

        private void BuildHair()
        {
            const int head = (int)HumanBone.Head;
            const HumanoidSlot hair = HumanoidSlot.Hair;
            HairStyle style = _s.Hair;
            if (style == HairStyle.Bald) return;

            // Hair is a dome cut on a tilted plane, so the hairline is one clean curve: high on the
            // forehead, over the tops of the ears, down to the nape.
            Quaternion hairline = Quaternion.Euler(-32f, 0f, 0f);
            if (style == HairStyle.Buzz)
            {
                Ellipsoid(hair, head, new Vector3(0f, 1.712f, -0.018f), new Vector3(0.0835f, 0.0985f, 0.099f), hairline, 28, 14, 2f, 2f, 0f, 96f);
                return;
            }

            Ellipsoid(hair, head, HairCentre, HairRadii, hairline, 28, 14, 2f, 2f, 0f, 96f);
            bool hat = HatOn;

            if (style == HairStyle.Messy && !hat)
                Ellipsoid(hair, head, new Vector3(0f, 1.780f, 0.010f), new Vector3(0.062f, 0.018f, 0.046f), I, 14, 8);

            if (style == HairStyle.Messy && !hat)
            {
                Ellipsoid(hair, head, new Vector3(0.042f, 1.800f, -0.020f), new Vector3(0.032f, 0.022f, 0.036f), Quaternion.Euler(10f, 20f, -15f), 10, 6);
                Ellipsoid(hair, head, new Vector3(-0.045f, 1.795f, -0.035f), new Vector3(0.030f, 0.022f, 0.034f), Quaternion.Euler(-12f, -25f, 18f), 10, 6);
                Ellipsoid(hair, head, new Vector3(0.010f, 1.805f, 0.030f), new Vector3(0.034f, 0.020f, 0.030f), Quaternion.Euler(18f, 0f, 8f), 10, 6);
                Ellipsoid(hair, head, new Vector3(0.050f, 1.765f, -0.085f), new Vector3(0.030f, 0.026f, 0.030f), Quaternion.Euler(-20f, 30f, 0f), 10, 6);
                Ellipsoid(hair, head, new Vector3(-0.040f, 1.770f, -0.095f), new Vector3(0.032f, 0.024f, 0.028f), Quaternion.Euler(-25f, -20f, 0f), 10, 6);
            }

            if (style == HairStyle.Long)
            {
                Ellipsoid(hair, head, new Vector3(0f, 1.620f, -0.072f), new Vector3(0.090f, 0.125f, 0.052f), I, 16, 10);
                for (int side = -1; side <= 1; side += 2)
                    Ellipsoid(hair, head, new Vector3(side * 0.074f, 1.655f, -0.025f), new Vector3(0.024f, 0.085f, 0.050f), I, 10, 8);
            }

            if (style == HairStyle.Ponytail)
            {
                Ellipsoid(hair, head, new Vector3(0f, 1.700f, -0.132f), new Vector3(0.024f, 0.024f, 0.024f), I, 10, 6);
                Limb(hair, head, new Vector3(0f, 1.690f, -0.145f), new Vector3(0f, 1.535f, -0.160f), Fwd,
                    new[] { new Key(0f, 0.028f, 0.026f), new Key(0.6f, 0.024f, 0.022f), new Key(1f, 0.012f, 0.012f) }, seg: 10);
            }
        }

        // ── Torso ────────────────────────────────────────────────────────────────────────────────────

        private static float TopInflate(TopStyle top)
        {
            switch (top)
            {
                case TopStyle.Tank: return 0.011f;
                case TopStyle.Dress: return 0.012f;
                case TopStyle.TShirt: return 0.013f;
                case TopStyle.LongSleeve: return 0.015f;
                case TopStyle.Hoodie: return 0.022f;
                case TopStyle.Jacket: return 0.026f;
                default: return 0.03f;   // coat, vest
            }
        }

        private static float SleeveInflate(TopStyle top)
        {
            switch (top)
            {
                case TopStyle.Hoodie: return 0.018f;
                case TopStyle.Jacket: return 0.020f;
                case TopStyle.Coat: return 0.022f;
                default: return 0.012f;
            }
        }

        private void BuildTorso()
        {
            HumanoidShape s = _s;
            bool underwear = s.Bottom == BottomStyle.Underwear;
            float pelvisInflate = underwear ? 0.005f : 0.009f;
            float pelvisTop = underwear ? 0.985f : 1.035f;

            // Hips: trousers, shorts, the top of a skirt, or underwear — with a waistband or belt.
            TorsoSection(HumanoidSlot.Bottom, 0.800f, pelvisTop, pelvisInflate, false, true);
            TorsoSection(HumanoidSlot.BottomTrim, pelvisTop - (underwear ? 0.016f : 0.03f), pelvisTop + 0.003f, pelvisInflate + 0.004f, true, true);
            if (s.Bottom >= BottomStyle.SkirtShort)
            {
                float hem = s.Bottom == BottomStyle.SkirtShort ? 0.72f : s.Bottom == BottomStyle.SkirtLong ? 0.30f : 0.50f;
                Skirt(HumanoidSlot.Bottom, 1.02f, hem, pelvisInflate + 0.004f, s.Bottom == BottomStyle.SkirtLong ? 0.17f : 0.21f);
            }

            TopStyle top = s.Top;
            if (top == TopStyle.None)
            {
                TorsoSection(HumanoidSlot.Skin, 0.95f, 1.512f, 0f, true, false);
                return;
            }

            float inf = TopInflate(top);
            _topInflate = inf;
            const HumanoidSlot cloth = HumanoidSlot.Top, trim = HumanoidSlot.TopTrim;
            float hemY = top == TopStyle.Hoodie ? 0.90f : top == TopStyle.Jacket ? 0.89f : top == TopStyle.Coat ? 0.97f
                       : top == TopStyle.Dress ? 1.00f : top == TopStyle.Vest ? 0.92f : 0.93f;

            bool sleeveless = top == TopStyle.Tank || top == TopStyle.Vest || top == TopStyle.Dress;
            if (sleeveless)
            {
                TorsoSection(HumanoidSlot.Skin, 1.30f, 1.512f, 0f, true, false);
                TorsoSection(cloth, hemY, top == TopStyle.Vest ? 1.425f : 1.405f, inf, true, true);
            }
            else
            {
                TorsoSection(cloth, hemY, 1.50f, inf, true, true);   // the cap at the top reads as the neckline
            }

            switch (top)
            {
                case TopStyle.TShirt:
                    Collar(trim, 0.020f, 0.012f);
                    break;

                case TopStyle.LongSleeve:
                    Collar(trim, 0.022f, 0.014f);
                    Buttons(new[] { 1.40f, 1.32f, 1.24f, 1.16f, 1.08f, 1.00f }, inf);
                    break;

                case TopStyle.Hoodie:
                    TorsoSection(trim, hemY - 0.004f, hemY + 0.035f, inf + 0.004f, true, true);
                    Ellipsoid(cloth, (int)HumanBone.Chest, new Vector3(0f, 1.475f, -0.098f), new Vector3(0.128f, 0.074f, 0.072f), I, 14, 8);
                    Ellipsoid(trim, (int)HumanBone.Hips, new Vector3(0f, 1.0f, SurfaceZ(1.0f, 0f, inf, true)), new Vector3(0.10f, 0.052f, 0.012f), I, 12, 6, 4f, 4f);
                    for (int side = -1; side <= 1; side += 2)
                        Limb(trim, (int)HumanBone.Chest, new Vector3(side * 0.03f, 1.475f, SurfaceZ(1.475f, 0.03f, inf, true)),
                            new Vector3(side * 0.034f, 1.34f, SurfaceZ(1.34f, 0.034f, inf, true) + 0.004f), Fwd,
                            new[] { new Key(0f, 0.0045f, 0.0045f), new Key(1f, 0.0045f, 0.0045f) }, seg: 6);
                    break;

                case TopStyle.Jacket:
                case TopStyle.Coat:
                    BigCollar(cloth);
                    if (top == TopStyle.Jacket) Zipper(hemY, inf);
                    else
                    {
                        Skirt(cloth, 1.0f, 0.62f, inf, 0.12f);
                        Buttons(new[] { 1.36f, 1.24f, 1.12f, 1.00f, 0.88f }, inf);
                    }
                    Pockets(top == TopStyle.Jacket ? 0.98f : 0.86f, inf, top == TopStyle.Coat);
                    break;

                case TopStyle.Vest:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Ellipsoid(trim, (int)HumanBone.Chest, new Vector3(side * 0.075f, 1.31f, SurfaceZ(1.31f, 0.075f, inf, true)), new Vector3(0.045f, 0.04f, 0.012f), I, 10, 6, 4f, 4f);
                        Ellipsoid(trim, (int)HumanBone.Hips, new Vector3(side * 0.085f, 1.02f, SurfaceZ(1.02f, 0.085f, inf, true)), new Vector3(0.05f, 0.045f, 0.012f), I, 10, 6, 4f, 4f);
                    }
                    break;

                case TopStyle.Tank:
                    Straps(cloth, inf);
                    break;

                case TopStyle.Dress:
                    Straps(cloth, inf);
                    Skirt(cloth, 1.02f, 0.52f, inf, 0.24f);
                    TorsoSection(trim, 0.995f, 1.025f, inf + 0.003f, true, true);
                    break;
            }
        }

        private void Collar(HumanoidSlot slot, float extraW, float extraD)
        {
            float n = _p.Neck;
            Limb(slot, (int)HumanBone.Chest, new Vector3(0f, 1.476f, -0.014f), new Vector3(0f, 1.508f, -0.014f), Fwd,
                new[] { new Key(0f, n + extraW, n + extraD), new Key(1f, n + extraW, n + extraD) }, 0f, 0f, 1f, false, false, 16);
        }

        private void BigCollar(HumanoidSlot slot)
        {
            float n = _p.Neck;
            Limb(slot, (int)HumanBone.Chest, new Vector3(0f, 1.462f, -0.016f), new Vector3(0f, 1.528f, -0.022f), Fwd,
                new[] { new Key(0f, n + 0.036f, n + 0.030f), new Key(1f, n + 0.030f, n + 0.022f) }, 0f, 0f, 1f, false, false, 16);
        }

        private void Buttons(float[] heights, float inflate)
        {
            for (int i = 0; i < heights.Length; i++)
            {
                float y = heights[i];
                Ellipsoid(HumanoidSlot.TopTrim, TorsoBone(y), new Vector3(0f, y, SurfaceZ(y, 0f, inflate, true) + 0.001f),
                    new Vector3(0.0065f, 0.0065f, 0.004f), I, 8, 4);
            }
        }

        private void Zipper(float hemY, float inflate)
        {
            float[] ys = { 1.48f, 1.36f, 1.22f, 1.10f, 0.98f, hemY + 0.006f };
            for (int i = 0; i < ys.Length - 1; i++)
            {
                float y0 = ys[i], y1 = ys[i + 1];
                Limb(HumanoidSlot.TopTrim, TorsoBone((y0 + y1) * 0.5f),
                    new Vector3(0f, y0, SurfaceZ(y0, 0f, inflate, true)), new Vector3(0f, y1, SurfaceZ(y1, 0f, inflate, true)), Fwd,
                    new[] { new Key(0f, 0.0055f, 0.0035f), new Key(1f, 0.0055f, 0.0035f) }, seg: 6);
            }
        }

        private void Pockets(float y, float inflate, bool large)
        {
            Vector3 size = large ? new Vector3(0.055f, 0.06f, 0.012f) : new Vector3(0.05f, 0.045f, 0.011f);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.095f;
                Ellipsoid(HumanoidSlot.TopTrim, TorsoBone(y), new Vector3(x, y, SurfaceZ(y, x, inflate, true) - 0.004f), size,
                    Quaternion.Euler(0f, side * 22f, 0f), 10, 6, 4f, 4f);
            }
        }

        private void Straps(HumanoidSlot slot, float inflate)
        {
            var ribbon = new[] { new Key(0f, 0.013f, 0.0045f), new Key(1f, 0.013f, 0.0045f) };
            for (int side = -1; side <= 1; side += 2)
            {
                var top = new Vector3(side * 0.092f, 1.497f, -0.012f);
                Limb(slot, (int)HumanBone.Chest, new Vector3(side * 0.085f, 1.395f, SurfaceZ(1.395f, 0.085f, inflate, true)), top, Fwd, ribbon, seg: 8);
                Limb(slot, (int)HumanBone.Chest, top, new Vector3(side * 0.085f, 1.395f, SurfaceZ(1.395f, 0.085f, inflate, false)), Fwd, ribbon, seg: 8);
            }
        }

        /// <summary>A skirt (or the tails of a coat or dress) hanging from the waist on the hips,
        /// flaring outward below the hips so the legs can stride inside it.</summary>
        private void Skirt(HumanoidSlot slot, float waistY, float hemY, float inflate, float flare)
        {
            _rings.Clear();
            int steps = Mathf.Max(3, Mathf.CeilToInt((waistY - hemY) / 0.04f));
            for (int i = 0; i <= steps; i++)
            {
                float y = Mathf.Lerp(waistY, hemY, (float)i / steps);
                Ring r = TorsoRing(Mathf.Max(y, 0.93f), inflate);
                float drop = Mathf.Max(0f, 0.93f - y);
                r.C = new Vector3(0f, y, r.C.z - 0.004f);
                r.Rx += drop * flare;
                r.Rz += drop * flare * 1.15f;
                r.Exp = 2.3f;
                r.B0 = r.B1 = (int)HumanBone.Hips;
                r.W1 = 0f;
                _rings.Add(r);
            }
            Loft(slot, 20, true, true);
        }

        // ── Arms ─────────────────────────────────────────────────────────────────────────────────────

        private void BuildArm(float sx)
        {
            bool right = sx > 0f;
            int upper = right ? (int)HumanBone.UpperArmR : (int)HumanBone.UpperArmL, fore = upper + 1, hand = upper + 2;
            HumanoidProportions p = _p;
            Vector3 sh = p.Bind[upper], el = p.Bind[fore], wr = p.Bind[hand];
            TopStyle top = _s.Top;

            // 0 = bare arm, 1 = short sleeve, 2 = long sleeve.
            int sleeve = top == TopStyle.TShirt ? 1
                : top == TopStyle.LongSleeve || top == TopStyle.Hoodie || top == TopStyle.Jacket || top == TopStyle.Coat ? 2 : 0;
            if ((_s.TornSleeves & (right ? 2 : 1)) != 0) sleeve = 0;
            float si = SleeveInflate(top);
            const HumanoidSlot skin = HumanoidSlot.Skin, cloth = HumanoidSlot.Top;

            var upperKeys = new[]
            {
                new Key(0f, p.UpperArm, p.UpperArm), new Key(0.35f, p.UpperArm * 1.05f, p.UpperArm * 1.1f, 0.004f),
                new Key(0.78f, p.Elbow * 1.08f, p.Elbow * 1.1f, 0.002f), new Key(1f, p.Elbow, p.Elbow),
            };
            var foreKeys = new[]
            {
                new Key(0f, p.Elbow * 1.02f, p.Elbow * 1.05f), new Key(0.25f, p.Forearm * 0.95f, p.Forearm * 1.08f, 0.004f),
                new Key(0.75f, p.Wrist * 1.05f, p.Wrist * 1.3f), new Key(1f, p.Wrist * 0.9f, p.Wrist * 1.22f),
            };

            // The shoulder cap is the top of the upper arm, only a little fuller; the arm itself starts
            // just under the joint so its rounded end stays inside the shoulder instead of riding above it.
            float shoulderGrow = sleeve > 0 ? si * 0.85f : 0f;
            Ellipsoid(sleeve > 0 ? cloth : skin, upper, sh + new Vector3(sx * 0.004f, -0.028f, -0.004f),
                new Vector3(p.Deltoid + shoulderGrow, p.Deltoid * 1.05f + shoulderGrow, p.Deltoid * 1.02f + shoulderGrow), I, 14, 9);

            const float armTop = 0.1f;
            if (sleeve == 2) Limb(cloth, upper, sh, el, Fwd, upperKeys, si, armTop);
            else if (sleeve == 1)
            {
                Limb(cloth, upper, sh, el, Fwd, upperKeys, si, armTop, 0.46f, true, false, flare: 0.006f);
                Limb(skin, upper, sh, el, Fwd, upperKeys, 0f, 0.38f, 1f);
            }
            else Limb(skin, upper, sh, el, Fwd, upperKeys, 0f, armTop);
            Remember(upper, sh, el, upperKeys, sleeve == 2 ? 1f : sleeve == 1 ? 0.46f : 0f, si);

            Ellipsoid(sleeve == 2 ? cloth : skin, fore, el, Vector3.one * (p.Elbow + (sleeve == 2 ? si : 0f)), I, 12, 8);
            if (sleeve == 2)
            {
                Limb(cloth, fore, el, wr, Fwd, foreKeys, si, 0f, 0.9f, true, false);
                Limb(HumanoidSlot.TopTrim, fore, el, wr, Fwd, foreKeys, si + 0.004f, 0.84f, 0.92f, false, false);
                Limb(skin, fore, el, wr, Fwd, foreKeys, 0f, 0.8f, 1f);
            }
            else Limb(skin, fore, el, wr, Fwd, foreKeys);
            Remember(fore, el, wr, foreKeys, sleeve == 2 ? 0.9f : 0f, si);

            BuildHand(sx, hand, wr);
        }

        private void Remember(int bone, Vector3 a, Vector3 b, Key[] keys, float coverTo, float cover) =>
            _limbs.Add(new LimbSurface { Bone = bone, A = a, B = b, Keys = keys, CoverTo = coverTo, Cover = cover });

        /// <summary>A relaxed, half-closed hand — palm facing the thigh, fingers curled — that also reads
        /// as a grip when something is held in it.</summary>
        private void BuildHand(float sx, int hand, Vector3 wr)
        {
            HandwearStyle gear = _s.Hands;
            HumanoidSlot palm = gear != HandwearStyle.Bare ? HumanoidSlot.Gloves : HumanoidSlot.Skin;
            HumanoidSlot fingers = gear == HandwearStyle.Gloves ? HumanoidSlot.Gloves : HumanoidSlot.Skin;
            float g = gear != HandwearStyle.Bare ? 0.004f : 0f;
            float hs = _p.Hand;

            Ellipsoid(palm, hand, wr + new Vector3(sx * -0.004f, -0.048f, 0.006f) * hs,
                new Vector3(0.0155f + g, 0.046f + g, 0.040f + g) * hs, I, 12, 8, 3.2f, 3f);
            Ellipsoid(fingers, hand, wr + new Vector3(sx * -0.012f, -0.106f, 0.012f) * hs,
                new Vector3(0.0145f + g, 0.033f + g, 0.036f + g) * hs, Quaternion.Euler(0f, 0f, sx * -16f), 12, 8, 3f, 2.6f);
            Limb(fingers, hand, wr + new Vector3(sx * -0.006f, -0.028f, 0.036f) * hs, wr + new Vector3(sx * -0.020f, -0.068f, 0.050f) * hs, Vector3.right,
                new[] { new Key(0f, (0.012f + g) * hs, (0.012f + g) * hs), new Key(1f, (0.0095f + g) * hs, (0.0095f + g) * hs) }, seg: 8);

            if (gear != HandwearStyle.Bare)
            {
                float w = _p.Wrist;
                Limb(HumanoidSlot.Gloves, hand, wr + new Vector3(0f, 0.022f, 0f), wr + new Vector3(0f, -0.022f, 0f), Fwd,
                    new[] { new Key(0f, w * 0.95f + 0.012f, w * 1.22f + 0.012f), new Key(1f, w * 0.95f + 0.012f, w * 1.22f + 0.012f) },
                    0f, 0f, 1f, false, false, 12);
            }
        }
    }
}
