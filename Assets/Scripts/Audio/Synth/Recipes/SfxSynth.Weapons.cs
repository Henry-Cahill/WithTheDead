using System;
using UnityEngine;

namespace CloneZ
{
    // Guns, reloads, bolts, impacts, melee and explosions.
    public sealed partial class SfxSynth
    {
        partial void RegisterWeapons()
        {
            // ── Guns ────────────────────────────────────────────────────────────────────────────────
            R(SfxIds.GunPistol, g => Gun(g, 0.9f, 200f, 60f, 0.07f, 5500f, 0.11f, 0.3f), 3, peak: 0.95f);
            R(SfxIds.GunRifle, g => Gun(g, 1.3f, 150f, 45f, 0.09f, 7000f, 0.16f, 0.42f), 3, peak: 0.95f);
            R(SfxIds.GunShotgun, g => Gun(g, 1.2f, 110f, 38f, 0.13f, 3600f, 0.2f, 0.4f), 3, peak: 0.95f);
            R(SfxIds.GunSniper, g => Gun(g, 2.4f, 120f, 34f, 0.12f, 8000f, 0.28f, 0.55f), 2, peak: 0.95f);
            R(SfxIds.GunSuppressed, g =>
            {
                float[] x = g.Buffer(0.45f);
                Dsp.AddBurst(x, g, 0f, 0.7f, 0.001f, 0.02f, 1800f);
                Dsp.Thump(x, Rate, 0f, 0.5f, 160f, 70f, 0.02f, 0.001f, 0.04f);
                Dsp.AddBurst(x, g, 0.005f, 0.35f, 0.0005f, 0.004f, 0f, 2500f);
                return x;
            }, 3);
            R(SfxIds.GunFlare, g =>
            {
                float[] x = g.Buffer(1.4f);
                Dsp.Thump(x, Rate, 0f, 0.6f, 160f, 60f, 0.04f, 0.001f, 0.08f);
                float[] w = Dsp.White(g, x.Length, 0.6f);
                Dsp.SweepBP(w, Rate, 2f, u => 400f + 2400f * u);
                Dsp.Shape(w, Rate, t => Dsp.AD(t, 0.05f, 0.4f));
                Dsp.Mix(x, w, 0);
                return x;
            });
            R(SfxIds.DryFire, g => Click(g, 2600f, 0.012f), 2);
            R(SfxIds.Reload, g =>
            {
                float[] x = g.Buffer(0.8f);
                AddClick(x, g, 0f, 1800f, 0.02f, 0.9f);
                Dsp.Grains(x, g, 0.2f, 0.45f, 30, 0.25f, 0.002f, u => 1f - u);
                AddClick(x, g, 0.55f, 2200f, 0.015f, 1f);
                return x;
            }, 2);
            R(SfxIds.Jam, g =>
            {
                float[] x = g.Buffer(0.5f);
                AddClick(x, g, 0f, 1400f, 0.02f, 1f);
                AddClick(x, g, 0.18f, 1200f, 0.025f, 0.8f);
                return x;
            });
            R(SfxIds.BoltThrow, g => Whoosh(g, 0.3f, 900f, 2400f, 0.5f), 2);
            R(SfxIds.BoltLand, g =>
            {
                float[] x = g.Buffer(0.6f);
                Dsp.Ring(x, Rate, 0f, 0.6f, g.Vary(3100f, 0.08f), new[] { 1f, 2.41f, 4.07f, 6.3f }, 0.09f);
                Dsp.AddBurst(x, g, 0f, 0.4f, 0.0005f, 0.004f, 0f, 2000f);
                return x;
            }, 3);

            // ── Impacts ─────────────────────────────────────────────────────────────────────────────
            R(SfxIds.HitFlesh, g => { float[] x = g.Buffer(0.3f); Dsp.Thump(x, Rate, 0f, 1f, 150f, 80f, 0.02f, 0.001f, 0.05f); Dsp.AddBurst(x, g, 0f, 0.5f, 0.001f, 0.03f, 1200f); return x; }, 3);
            R(SfxIds.HitArmor, g => { float[] x = g.Buffer(0.9f); Dsp.Ring(x, Rate, 0f, 0.7f, g.Vary(900f, 0.1f), new[] { 1f, 2.76f, 5.4f, 8.93f }, 0.25f); Dsp.AddBurst(x, g, 0f, 0.6f, 0.0005f, 0.006f, 0f, 2000f); return x; }, 3);
            R(SfxIds.Ricochet, g =>
            {
                float[] x = g.Buffer(0.5f);
                float ph = 0f;
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = Dsp.Glide(3200f, 1100f, t / 0.45f) * (1f + 0.03f * MathF.Sin(t * 90f));
                    ph += f / Rate;
                    x[i] = MathF.Sin(Dsp.TwoPi * ph) * Dsp.AD(t, 0.005f, 0.12f);
                }
                Dsp.AddBurst(x, g, 0f, 0.5f, 0.0005f, 0.003f, 0f, 2500f);
                return x;
            }, 2);
            R(SfxIds.MeleeSwing, g => Whoosh(g, 0.28f, 500f, 1500f, 0.7f), 3);
            R(SfxIds.MeleeHit, g => { float[] x = g.Buffer(0.3f); Dsp.Thump(x, Rate, 0f, 1f, 120f, 70f, 0.02f, 0.001f, 0.06f); Dsp.AddBurst(x, g, 0f, 0.45f, 0.001f, 0.02f, 1500f); return x; }, 3);
            R(SfxIds.Explosion, g =>
            {
                float[] x = g.Buffer(2.8f);
                Dsp.Thump(x, Rate, 0f, 1f, 75f, 28f, 0.15f, 0.003f, 0.6f);
                float[] b = Dsp.Brown(g, x.Length);
                Dsp.LP(b, 700f, Rate);
                Dsp.Shape(b, Rate, t => Dsp.AD(t, 0.005f, 0.7f));
                Dsp.Norm(b, 0.8f);
                Dsp.Mix(x, b, 0);
                Dsp.Grains(x, g, 0.15f, 1.6f, 120, 0.2f, 0.004f, u => 1f - u);
                Dsp.Echo(x, Rate, 0.28f, 0.35f, 1200f, 0.35f);
                return x;
            }, 2, peak: 1f);
            R(SfxIds.Grenade, g => { float[] x = g.Buffer(0.5f); AddClick(x, g, 0f, 3400f, 0.03f, 1f); AddClick(x, g, 0.2f, 2800f, 0.05f, 0.7f); return x; });

            // ── The ears ────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.EarRing, g =>
            {
                float[] x = g.Buffer(4.5f);
                float f = g.Range(3400f, 4200f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)g.Rate;
                    x[i] = (MathF.Sin(Dsp.TwoPi * f * t) + 0.3f * MathF.Sin(Dsp.TwoPi * f * 1.004f * t)) * Dsp.AD(t, 0.05f, 1.6f);
                }
                return x;
            }, 2).Peak(0.35f).Inside().Dry().Priority(8);
        }
    }
}
