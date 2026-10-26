using System;
using UnityEngine;

namespace CloneZ
{
    // Zone gear, anomalies, emissions and machines.
    public sealed partial class SfxSynth
    {
        partial void RegisterZone()
        {
            // ── Zone gear ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.DetectorBeep, g => Tone(g, 0.07f, 1800f, 0.005f, 0.03f));
            R(SfxIds.DetectorAlert, g => { float[] x = g.Buffer(0.25f); MixTone(x, 0f, 2400f, 0.004f, 0.02f, 1f); MixTone(x, 0.11f, 2400f, 0.004f, 0.02f, 1f); return x; });
            R(SfxIds.Geiger, g => { float[] x = g.Buffer(0.03f); Dsp.AddBurst(x, g, 0f, 1f, 0.0002f, 0.0012f, 0f, 3000f); return x; }, 3);

            // ── Anomalies ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.AnomalyHum, g => Loop(g, 3f, x =>
            {
                float f1 = Dsp.Snap(55f, 3f), f2 = Dsp.Snap(110.5f, 3f), f3 = Dsp.Snap(83f, 3f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.5f * MathF.Sin(Dsp.TwoPi * f1 * t) + 0.25f * MathF.Sin(Dsp.TwoPi * f2 * t) + 0.18f * MathF.Sin(Dsp.TwoPi * f3 * t);
                }
                float[] p = Dsp.Pink(g, x.Length);
                Dsp.LP(p, 500f, Rate);
                Dsp.Mix(x, p, 0, 0.2f);
            }), loop: true, peak: 0.6f);
            R(SfxIds.AnomalyGravity, g =>
            {
                float[] x = Dsp.White(g, g.N(1.6f), 0.8f);
                Dsp.SweepLP(x, Rate, 3f, u => 150f + 1300f * Dsp.Bell(u, 1f, 0.5f));
                Dsp.Shape(x, Rate, t => Dsp.Bell(t, 1.6f, 0.5f));
                Dsp.Thump(x, Rate, 0.7f, 0.9f, 60f, 30f, 0.2f, 0.01f, 0.5f);
                return x;
            }, 2);
            R(SfxIds.AnomalyFire, g =>
            {
                float[] x = Dsp.Brown(g, g.N(1.5f));
                Dsp.LP(x, 1000f, Rate);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 0.08f, 1f, 0.4f));
                Dsp.Grains(x, g, 0f, 1.4f, 160, 0.25f, 0.002f, u => 1f);
                return x;
            }, 2);
            R(SfxIds.AnomalyElectric, g =>
            {
                float[] x = g.Buffer(0.9f);
                Dsp.Grains(x, g, 0f, 0.8f, 220, 0.6f, 0.0008f, u => 1f - u * 0.5f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] += 0.25f * Dsp.Square(Dsp.Wrap(t * 120f), 120f / Rate) * Dsp.ASR(t, 0.01f, 0.6f, 0.2f);
                }
                Dsp.HP(x, 300f, Rate);
                return x;
            }, 3);
            R(SfxIds.AnomalyAcid, g =>
            {
                float[] x = Dsp.White(g, g.N(1.2f), 0.4f);
                Dsp.HP(x, 2500f, Rate);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 0.05f, 0.8f, 0.3f));
                for (int b = 0; b < 8; b++) Dsp.Mix(x, Dsp.Bubble(Rate, g.Range(300f, 900f), 0.12f, 1.4f), g.N(g.Range(0f, 1f)), 0.6f);
                return x;
            }, 2);
            R(SfxIds.AnomalyTrigger, g => { float[] x = g.Buffer(1f); Dsp.AddBurst(x, g, 0f, 1f, 0.001f, 0.08f, 5000f); Dsp.Thump(x, Rate, 0f, 0.9f, 140f, 40f, 0.06f, 0.002f, 0.3f); return x; }, 2);

            // ── Emissions and psi ───────────────────────────────────────────────────────────────────
            R(SfxIds.EmissionSiren, g => Loop(g, 4f, x =>
            {
                float ph = 0f;
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 650f + 260f * MathF.Sin(Dsp.TwoPi * t / 4f);
                    ph += f / Rate;
                    float s = MathF.Sin(Dsp.TwoPi * ph);
                    x[i] = 0.6f * s + 0.25f * MathF.Sin(Dsp.TwoPi * ph * 2f) + 0.1f * MathF.Sin(Dsp.TwoPi * ph * 3f);
                }
                Dsp.Echo(x, Rate, 0.35f, 0.3f, 1500f, 0.4f, true);
            }), loop: true, peak: 0.7f);
            R(SfxIds.EmissionRumble, g =>
            {
                float[] x = Dsp.Brown(g, g.N(4f));
                Dsp.LP(x, 140f, Rate);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 1f, 2f, 1f));
                for (int k = 0; k < 5; k++) Dsp.Thump(x, Rate, g.Range(0.2f, 3.5f), 0.7f, 50f, 25f, 0.3f, 0.05f, 0.6f);
                return x;
            });

            // ── Machines ────────────────────────────────────────────────────────────────────────────
            R(SfxIds.HeliRotor, g => Loop(g, 2f, x =>
            {
                float blade = Dsp.Snap(4.5f, 2f);
                int beats = Mathf.RoundToInt(blade * 2f);
                for (int b = 0; b < beats; b++)
                {
                    var thump = new float[g.N(0.18f)];
                    Dsp.Thump(thump, Rate, 0f, 1f, 95f, 55f, 0.03f, 0.004f, 0.05f);
                    Dsp.AddBurst(thump, g, 0f, 0.35f, 0.003f, 0.04f, 700f);
                    Dsp.MixWrap(x, thump, g.N(b / blade));
                }
                float whine = Dsp.Snap(1180f, 2f);
                for (int i = 0; i < x.Length; i++) x[i] += 0.06f * MathF.Sin(Dsp.TwoPi * whine * i / Rate);
                float[] n = Dsp.Pink(g, x.Length);
                Dsp.LP(n, 900f, Rate);
                Dsp.Mix(x, n, 0, 0.25f);
            }), loop: true);
            R(SfxIds.FlareBurn, g => Loop(g, 2f, x =>
            {
                float[] n = Dsp.White(g, x.Length, 0.5f);
                Dsp.BP(n, 2000f, 0.7f, Rate);
                Dsp.Mix(x, n, 0);
                Dsp.Grains(x, g, 0f, 2.2f, 90, 0.3f, 0.0015f, u => 1f);
            }), loop: true, peak: 0.6f);
            R(SfxIds.EngineLoop, g => Loop(g, 1f, x =>
            {
                float fire = Dsp.Snap(28f, 1f), saw = Dsp.Snap(42f, 1f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.5f * Dsp.Saw(Dsp.Wrap(saw * t), saw / Rate) + 0.5f * MathF.Pow(MathF.Max(0f, MathF.Sin(Dsp.TwoPi * fire * t)), 6f);
                }
                Dsp.LoopLP(x, 600f, Rate);
                float[] n = Dsp.Brown(g, x.Length);
                Dsp.Mix(x, n, 0, 0.15f);
            }), loop: true, peak: 0.6f);
            R(SfxIds.EngineStartFail, g =>
            {
                float[] x = g.Buffer(1.6f);
                for (int i = 0; i < g.N(1.1f); i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.5f * Dsp.Square(Dsp.Wrap(t * (55f + 8f * MathF.Sin(t * 17f))), 60f / Rate) * Dsp.ASR(t, 0.05f, 0.9f, 0.15f);
                }
                Dsp.LP(x, 700f, Rate);
                Dsp.Thump(x, Rate, 1.15f, 0.8f, 80f, 40f, 0.05f, 0.005f, 0.12f);
                return x;
            });
            R(SfxIds.DroneBuzz, g => Loop(g, 1f, x =>
            {
                float a = Dsp.Snap(180f, 1f), b = Dsp.Snap(183f, 1f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.4f * Dsp.Saw(Dsp.Wrap(a * t), a / Rate) + 0.4f * Dsp.Saw(Dsp.Wrap(b * t), b / Rate);
                }
                Dsp.LoopBP(x, 900f, 0.8f, Rate);
            }), loop: true, peak: 0.5f);
        }
    }
}
