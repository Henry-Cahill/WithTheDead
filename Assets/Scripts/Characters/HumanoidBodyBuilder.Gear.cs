using UnityEngine;

namespace CloneZ
{
    /// <summary>Legs, feet and footwear, headwear, and the blood on the dead.</summary>
    internal sealed partial class HumanoidBodyBuilder
    {
        // Foot sections from heel to toe along +Z: half width, half height, and centre height. Each
        // section's underside sits on the sole line, so the foot rests flat on the ground.
        private static readonly float[] FootZ = { -0.080f, -0.072f, -0.055f, -0.020f, 0.030f, 0.090f, 0.140f, 0.170f, 0.188f, 0.196f };
        private static readonly float[] FootW = { 0f, 0.024f, 0.034f, 0.040f, 0.043f, 0.047f, 0.046f, 0.040f, 0.027f, 0f };
        private static readonly float[] FootH = { 0f, 0.030f, 0.040f, 0.050f, 0.046f, 0.032f, 0.024f, 0.018f, 0.012f, 0f };
        private static readonly float[] FootY = { 0.040f, 0.040f, 0.045f, 0.052f, 0.048f, 0.035f, 0.028f, 0.022f, 0.018f, 0.018f };

        private Key[] ThighKeys()
        {
            HumanoidProportions p = _p;
            return new[]
            {
                new Key(0f, p.Thigh, p.Thigh), new Key(0.3f, p.Thigh * 0.94f, p.Thigh, 0.008f),
                new Key(0.72f, p.Knee * 1.22f, p.Knee * 1.2f, 0.004f), new Key(1f, p.Knee, p.Knee * 1.02f, 0.004f),
            };
        }

        private Key[] ShinKeys()
        {
            HumanoidProportions p = _p;
            return new[]
            {
                new Key(0f, p.Knee * 0.98f, p.Knee, 0.004f), new Key(0.22f, p.Calf * 0.96f, p.Calf * 1.04f, -0.012f),
                new Key(0.62f, p.Ankle * 1.3f, p.Ankle * 1.34f, -0.004f), new Key(1f, p.Ankle, p.Ankle * 1.05f),
            };
        }

        // ── Legs ─────────────────────────────────────────────────────────────────────────────────────

        private void BuildLeg(float sx)
        {
            int thigh = sx > 0f ? (int)HumanBone.ThighR : (int)HumanBone.ThighL, shin = thigh + 1;
            Vector3 hip = _p.Bind[thigh] + new Vector3(0f, 0.02f, 0f), knee = _p.Bind[shin], ankle = _p.Bind[shin + 1];
            Key[] tk = ThighKeys(), sk = ShinKeys();
            const HumanoidSlot skin = HumanoidSlot.Skin, cloth = HumanoidSlot.Bottom;
            Vector3 kneeCap = knee + new Vector3(0f, 0f, 0.004f);

            if (_s.Bottom == BottomStyle.Pants)
            {
                bool tucked = _s.Feet == FootwearStyle.Boots;
                float end = tucked ? 0.66f : 0.965f;
                Limb(cloth, thigh, hip, knee, Fwd, tk, 0.013f);
                Ellipsoid(cloth, shin, kneeCap, Vector3.one * (_p.Knee + 0.013f), I, 12, 8);
                Limb(cloth, shin, knee, ankle, Fwd, sk, 0.014f, 0f, end, true, false, flare: tucked ? 0f : 0.008f);
                Limb(skin, shin, knee, ankle, Fwd, sk, 0f, 0.86f, 1f);
                Remember(thigh, hip, knee, tk, 1f, 0.013f);
                Remember(shin, knee, ankle, sk, end, 0.014f);
                return;
            }

            if (_s.Bottom == BottomStyle.Shorts)
            {
                Limb(cloth, thigh, hip, knee, Fwd, tk, 0.016f, 0f, 0.5f, true, false, flare: 0.006f);
                Limb(skin, thigh, hip, knee, Fwd, tk, 0f, 0.42f, 1f);
                Remember(thigh, hip, knee, tk, 0.5f, 0.016f);
            }
            else
            {
                Limb(skin, thigh, hip, knee, Fwd, tk);
                Remember(thigh, hip, knee, tk, 0f, 0f);
            }

            Ellipsoid(skin, shin, kneeCap, Vector3.one * _p.Knee, I, 12, 8);
            Limb(skin, shin, knee, ankle, Fwd, sk);
            Remember(shin, knee, ankle, sk, 0f, 0f);
        }

        // ── Feet ─────────────────────────────────────────────────────────────────────────────────────

        private void BuildFoot(float sx)
        {
            int foot = sx > 0f ? (int)HumanBone.FootR : (int)HumanBone.FootL, shin = foot - 1;
            float x = sx * _p.HipX;
            const float heel = -0.08f, toe = 0.196f;
            Key[] sk = ShinKeys();
            Vector3 knee = _p.Bind[shin], ankle = _p.Bind[foot];

            switch (_s.Feet)
            {
                case FootwearStyle.Shoes:
                    FootLoft(HumanoidSlot.Shoes, foot, x, 0.010f, 0.008f, heel, toe);
                    Sole(foot, x, 0.010f, 0.022f);
                    break;

                case FootwearStyle.Boots:
                    FootLoft(HumanoidSlot.Shoes, foot, x, 0.013f, 0.010f, heel, toe);
                    Sole(foot, x, 0.013f, 0.03f);
                    Limb(HumanoidSlot.Shoes, shin, knee, ankle, Fwd, sk, 0.026f, 0.58f, 1f, false, true, 14);
                    Limb(HumanoidSlot.Soles, shin, knee, ankle, Fwd, sk, 0.029f, 0.58f, 0.61f, false, false, 14);
                    break;

                case FootwearStyle.Sandals:
                    FootLoft(HumanoidSlot.Skin, foot, x, 0f, 0f, heel, toe);
                    Sole(foot, x, 0.006f, 0.012f);
                    FootLoft(HumanoidSlot.Shoes, foot, x, 0.004f, 0.004f, 0.03f, 0.075f);
                    break;

                case FootwearStyle.Socks:
                    FootLoft(HumanoidSlot.Shoes, foot, x, 0.004f, 0.004f, heel, toe);
                    Limb(HumanoidSlot.Shoes, shin, knee, ankle, Fwd, sk, 0.005f, 0.80f, 1f, false, true);
                    break;

                default:
                    FootLoft(HumanoidSlot.Skin, foot, x, 0f, 0f, heel, toe);
                    break;
            }
        }

        private void FootLoft(HumanoidSlot slot, int bone, float x, float growW, float growH, float z0, float z1)
        {
            _rings.Clear();
            int last = FootZ.Length - 1;
            bool fromHeel = z0 <= FootZ[0] + 1e-4f, toToe = z1 >= FootZ[last] - 1e-4f;

            _rings.Add(FootRing(bone, x, fromHeel ? FootZ[0] : z0, growW, growH, fromHeel ? -growW : 0f));
            for (int i = 1; i < last; i++)
                if (FootZ[i] > z0 + 1e-4f && FootZ[i] < z1 - 1e-4f) _rings.Add(FootRing(bone, x, FootZ[i], growW, growH, 0f));
            _rings.Add(FootRing(bone, x, toToe ? FootZ[last] : z1, growW, growH, toToe ? growW : 0f));
            Loft(slot, 14, !fromHeel, !toToe);
        }

        private Ring FootRing(int bone, float x, float z, float growW, float growH, float shift)
        {
            int i = 0;
            while (i < FootZ.Length - 2 && z > FootZ[i + 1]) i++;
            float u = Mathf.Clamp01((z - FootZ[i]) / (FootZ[i + 1] - FootZ[i]));
            float w = Mathf.Lerp(FootW[i], FootW[i + 1], u), h = Mathf.Lerp(FootH[i], FootH[i + 1], u);
            float y = Mathf.Lerp(FootY[i], FootY[i + 1], u);
            bool pole = w < 1e-4f;
            return new Ring
            {
                // Growing the shoe keeps its underside on the sole and lifts its top.
                C = new Vector3(x, y + (pole ? 0f : growH), z + shift), R = Vector3.right, F = Vector3.up,
                Rx = pole ? 0f : w + growW, Rz = pole ? 0f : h + growH, Exp = 2.5f, B0 = bone, B1 = bone,
            };
        }

        private void Sole(int foot, float x, float grow, float thick)
        {
            Ellipsoid(HumanoidSlot.Soles, foot, new Vector3(x, thick * 0.5f, 0.058f),
                new Vector3(0.047f + grow + 0.004f, thick * 0.5f, 0.138f + grow * 0.8f + 0.006f), I, 16, 6, 2.6f, 6f);
        }

        // ── Headwear ─────────────────────────────────────────────────────────────────────────────────

        private void BuildHeadwear()
        {
            const int head = (int)HumanBone.Head;
            const HumanoidSlot hat = HumanoidSlot.Hat, trim = HumanoidSlot.HatTrim;

            switch (_s.Headwear)
            {
                case HeadwearStyle.Cap:
                case HeadwearStyle.CapBackward:
                {
                    Quaternion tilt = Quaternion.Euler(-14f, 0f, 0f);
                    Ellipsoid(hat, head, HairCentre, new Vector3(0.098f, 0.107f, 0.115f), tilt, 18, 7, 2f, 2f, 0f, 90f);
                    Vector3 top = HairCentre + tilt * new Vector3(0f, 0.107f, 0f);
                    Ellipsoid(trim, head, top, new Vector3(0.011f, 0.007f, 0.011f), tilt, 8, 4);
                    if (_s.Headwear == HeadwearStyle.Cap)
                        Ellipsoid(hat, head, new Vector3(0f, 1.742f, 0.105f), new Vector3(0.080f, 0.008f, 0.070f), Quaternion.Euler(8f, 0f, 0f), 16, 6);
                    else
                        Ellipsoid(hat, head, new Vector3(0f, 1.695f, -0.162f), new Vector3(0.080f, 0.008f, 0.066f), Quaternion.Euler(-12f, 0f, 0f), 16, 6);
                    break;
                }

                case HeadwearStyle.Beanie:
                {
                    Quaternion tilt = Quaternion.Euler(-10f, 0f, 0f);
                    Ellipsoid(hat, head, HairCentre, new Vector3(0.099f, 0.109f, 0.116f), tilt, 18, 8, 2f, 2f, 0f, 92f);
                    Ellipsoid(trim, head, HairCentre, new Vector3(0.106f, 0.115f, 0.123f), tilt, 18, 3, 2f, 2f, 70f, 93f);
                    break;
                }

                case HeadwearStyle.Helmet:
                {
                    Quaternion tilt = Quaternion.Euler(-12f, 0f, 0f);
                    Ellipsoid(hat, head, HairCentre, new Vector3(0.108f, 0.115f, 0.126f), tilt, 18, 8, 2.2f, 2f, 0f, 95f);
                    Ellipsoid(trim, head, new Vector3(0f, 1.752f, 0.100f), new Vector3(0.075f, 0.007f, 0.035f), Quaternion.Euler(12f, 0f, 0f), 14, 5);
                    for (int side = -1; side <= 1; side += 2)
                        Ellipsoid(trim, head, new Vector3(side * 0.03f, 1.832f, -0.035f), new Vector3(0.008f, 0.006f, 0.07f), Quaternion.Euler(-10f, 0f, 0f), 8, 4);
                    break;
                }

                case HeadwearStyle.WideBrim:
                {
                    Quaternion tilt = Quaternion.Euler(-6f, 0f, 0f);
                    var crown = new Vector3(0f, 1.73f, -0.03f);
                    Ellipsoid(hat, head, crown, new Vector3(0.096f, 0.115f, 0.11f), tilt, 18, 8, 2.4f, 3f, 0f, 90f);
                    Ellipsoid(trim, head, crown, new Vector3(0.1f, 0.119f, 0.114f), tilt, 18, 2, 2.4f, 3f, 74f, 88f);
                    Ellipsoid(hat, head, new Vector3(0f, 1.733f, -0.025f), new Vector3(0.19f, 0.008f, 0.20f), tilt, 24, 6);
                    break;
                }

                case HeadwearStyle.Bandana:
                {
                    Quaternion tilt = Quaternion.Euler(-14f, 0f, 0f);
                    Ellipsoid(hat, head, HairCentre, HairRadii + new Vector3(0.004f, 0.004f, 0.004f), tilt, 18, 3, 2f, 2f, 62f, 82f);
                    Ellipsoid(hat, head, new Vector3(0f, 1.71f, -0.142f), new Vector3(0.02f, 0.018f, 0.016f), I, 8, 5);
                    for (int side = -1; side <= 1; side += 2)
                        Limb(hat, head, new Vector3(0f, 1.70f, -0.150f), new Vector3(side * 0.02f, 1.62f, -0.162f), Fwd,
                            new[] { new Key(0f, 0.012f, 0.005f), new Key(1f, 0.008f, 0.004f) }, seg: 8);
                    break;
                }

                case HeadwearStyle.Mask:
                    Ellipsoid(hat, head, new Vector3(0f, 1.635f, 0.024f), new Vector3(0.072f, 0.056f, 0.090f), I, 16, 9);
                    Ellipsoid(trim, head, new Vector3(0f, 1.68f, -0.012f), new Vector3(0.084f, 0.104f, 0.102f), Quaternion.Euler(8f, 0f, 0f), 18, 2, 2f, 2f, 86f, 94f);
                    break;

                case HeadwearStyle.Glasses:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Ellipsoid(hat, head, new Vector3(side * 0.031f, 1.705f, 0.092f), new Vector3(0.022f, 0.017f, 0.003f), I, 12, 4);
                        Ellipsoid(trim, head, new Vector3(side * 0.031f, 1.705f, 0.095f), new Vector3(0.019f, 0.014f, 0.003f), I, 12, 4);
                        Limb(hat, head, new Vector3(side * 0.052f, 1.708f, 0.088f), new Vector3(side * 0.081f, 1.703f, -0.005f), Vector3.up,
                            new[] { new Key(0f, 0.003f, 0.003f), new Key(1f, 0.003f, 0.003f) }, seg: 6);
                    }
                    Ellipsoid(hat, head, new Vector3(0f, 1.708f, 0.096f), new Vector3(0.012f, 0.003f, 0.003f), I, 8, 4);
                    break;
            }
        }

        // ── Gore ─────────────────────────────────────────────────────────────────────────────────────

        private void BuildGore()
        {
            var rng = new System.Random(_s.Gore * 7919 + 101);

            // Blood down the chin from the last meal.
            Splash((int)HumanBone.Head, new Vector3(0f, 1.598f, 0.071f), new Vector3(0f, -0.45f, 1f), 0.022f, 0.032f);

            int count = 5 + rng.Next(5);
            for (int i = 0; i < count; i++)
            {
                double pick = rng.NextDouble();
                if (pick < 0.45) TorsoSplash(rng);
                else LimbSplash(rng, pick < 0.72 ? rng.Next(0, 4) : rng.Next(4, _limbs.Count));
            }
        }

        private void TorsoSplash(System.Random rng)
        {
            float y = Mathf.Lerp(0.98f, 1.40f, (float)rng.NextDouble());
            float a = Mathf.Lerp(-1.9f, 1.9f, (float)rng.NextDouble());   // around the body from the front
            bool covered = _s.Top != TopStyle.None && y > 0.95f;
            float inflate = covered ? _topInflate : 0f;

            SampleTorso(y, out float w, out float d, out float z, out float e);
            w += inflate; d += inflate;
            float s = Mathf.Sin(a), c = Mathf.Cos(a), p = 2f / e;
            float x = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), p) * w;
            float fz = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), p) * d;
            // Superellipse normal: the gradient of |x/w|^e + |z/d|^e.
            Vector3 normal = new Vector3(Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x / w), e - 1f) / w, 0f,
                                         Mathf.Sign(fz) * Mathf.Pow(Mathf.Abs(fz / d), e - 1f) / d);
            if (normal.sqrMagnitude < 1e-8f) normal = Vector3.forward;

            Splash(TorsoBone(y), new Vector3(x, y, z + fz), normal.normalized,
                Mathf.Lerp(0.03f, 0.055f, (float)rng.NextDouble()), Mathf.Lerp(0.03f, 0.065f, (float)rng.NextDouble()));
        }

        private void LimbSplash(System.Random rng, int index)
        {
            if (index < 0 || index >= _limbs.Count) return;
            LimbSurface l = _limbs[index];
            LimbFrame(l.A, l.B, Fwd, out Vector3 axis, out float len, out Vector3 r, out Vector3 f);
            float t = Mathf.Lerp(0.15f, 0.85f, (float)rng.NextDouble());
            SampleKeys(l.Keys, t, out float rx, out float rz, out float dz);
            float inflate = t <= l.CoverTo ? l.Cover : 0f;
            rx += inflate; rz += inflate;

            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector3 point = l.A + axis * (t * len) + f * dz + r * (c * rx) + f * (s * rz);
            Vector3 normal = (r * (c / rx) + f * (s / rz)).normalized;
            Splash(l.Bone, point, normal, Mathf.Lerp(0.018f, 0.032f, (float)rng.NextDouble()), Mathf.Lerp(0.025f, 0.05f, (float)rng.NextDouble()));
        }

        /// <summary>A thin, flattened blot of blood lying on the surface at <paramref name="point"/>.</summary>
        private void Splash(int bone, Vector3 point, Vector3 normal, float width, float height)
        {
            Quaternion rot = Quaternion.LookRotation(normal, Vector3.up);
            Ellipsoid(HumanoidSlot.Gore, bone, point + normal * 0.001f, new Vector3(width, height, 0.0065f), rot, 10, 6);
        }
    }
}
