using System;
using UnityEngine;

namespace CloneZ
{
    // Doors, gear, the body, the interface and small actions.
    public sealed partial class SfxSynth
    {
        partial void RegisterFoley()
        {
            // ── Doors and gear ──────────────────────────────────────────────────────────────────────
            R(SfxIds.DoorOpen, g => Dsp.Creak(g, 0.9f, u => 18f + 30f * u, u => Dsp.Bell(u, 1f, 0.4f), new[] { 480f, 1150f, 2300f }, new[] { 6f, 8f, 10f }, 0.35f), 2);
            R(SfxIds.DoorClose, g => { float[] x = g.Buffer(0.6f); Dsp.Thump(x, Rate, 0f, 1f, 110f, 60f, 0.03f, 0.002f, 0.12f); Dsp.AddBurst(x, g, 0f, 0.4f, 0.001f, 0.03f, 900f); return x; }, 2);
            R(SfxIds.DoorBash, g => { float[] x = g.Buffer(0.8f); Dsp.Thump(x, Rate, 0f, 1f, 95f, 50f, 0.04f, 0.002f, 0.18f); Dsp.AddBurst(x, g, 0f, 0.7f, 0.001f, 0.06f, 2500f); Dsp.Grains(x, g, 0.02f, 0.3f, 25, 0.3f, 0.003f, u => 1f - u); return x; }, 3);
            R(SfxIds.GearRustle, g => { float[] x = g.Buffer(0.4f); Dsp.Grains(x, g, 0f, 0.35f, 60, 0.4f, 0.004f, u => Dsp.Bell(u, 1f, 0.5f)); Dsp.BP(x, 1800f, 0.7f, Rate); return x; }, 3);

            // ── The body ────────────────────────────────────────────────────────────────────────────
            R(SfxIds.BreathMask, g => Loop(g, 3.2f, x =>
            {
                float[] n = Dsp.Pink(g, x.Length);
                for (int i = 0; i < n.Length; i++)
                {
                    float t = (i / (float)Rate) % 3.2f;
                    float env = t < 1.3f ? Dsp.Bell(t, 1.3f, 0.4f) : Dsp.Bell(t - 1.5f, 1.5f, 0.3f) * 0.8f;
                    n[i] *= env;
                }
                Dsp.BP(n, 900f, 1.2f, Rate);
                Dsp.Mix(x, n, 0);
                AddClick(x, g, 1.4f, 1500f, 0.01f, 0.3f);
            }), loop: true, peak: 0.55f);
            R(SfxIds.MaskCrack, g => { float[] x = g.Buffer(0.8f); Dsp.Ring(x, Rate, 0f, 0.8f, 2600f, new[] { 1f, 1.73f, 2.9f, 4.4f, 6.1f }, 0.12f); Dsp.Grains(x, g, 0f, 0.25f, 40, 0.5f, 0.0008f, u => 1f - u); return x; });
            R(SfxIds.VisorWipe, g => { float[] x = Dsp.White(g, g.N(0.5f), 0.4f); Dsp.SweepBP(x, Rate, 4f, u => 1200f + 1800f * u); Dsp.Shape(x, Rate, t => Dsp.Bell(t, 0.5f, 0.5f)); return x; });
            R(SfxIds.Heartbeat, g => { float[] x = g.Buffer(0.9f); Dsp.Thump(x, Rate, 0f, 1f, 70f, 45f, 0.03f, 0.005f, 0.06f); Dsp.Thump(x, Rate, 0.22f, 0.7f, 65f, 45f, 0.03f, 0.005f, 0.06f); return x; });
            R(SfxIds.Sneeze, g =>
            {
                float[] x = Dsp.White(g, g.N(0.9f), 0.5f);
                Dsp.SweepBP(x, Rate, 1.5f, u => 600f + 1800f * u);
                Dsp.Shape(x, Rate, t => t < 0.55f ? Dsp.Bell(t, 0.55f, 0.8f) * 0.35f : Dsp.AD(t - 0.55f, 0.01f, 0.08f));
                return x;
            }, 2);
            R(SfxIds.Cough, g =>
            {
                float[] x = g.Buffer(0.9f);
                for (int k = 0; k < 3; k++) Dsp.AddBurstBP(x, g, k * 0.25f, 1f - k * 0.2f, 0.005f, 0.06f, 550f, 1.2f);
                return x;
            }, 2);

            // ── Interface ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.UiClick, g => Tone(g, 0.03f, 2200f, 0.001f, 0.008f), peak: 0.4f);
            R(SfxIds.UiBuy, g => { float[] x = g.Buffer(0.5f); Dsp.Ring(x, Rate, 0f, 0.5f, 1320f, new[] { 1f, 2f, 3f }, 0.15f); Dsp.Ring(x, Rate, 0.09f, 0.5f, 1760f, new[] { 1f, 2f, 3f }, 0.2f); return x; }, peak: 0.5f);
            R(SfxIds.UiError, g => { float[] x = g.Buffer(0.25f); for (int i = 0; i < x.Length; i++) x[i] = 0.4f * Dsp.Square(Dsp.Wrap(i * 180f / Rate), 180f / Rate) * Dsp.ASR(i / (float)Rate, 0.005f, 0.18f, 0.05f); Dsp.LP(x, 1500f, Rate); return x; }, peak: 0.4f);
            R(SfxIds.Cash, g => { float[] x = g.Buffer(0.7f); for (int k = 0; k < 5; k++) Dsp.Ring(x, Rate, g.Range(0f, 0.35f), 0.4f, g.Range(3000f, 4200f), new[] { 1f, 2.6f, 4.9f }, 0.08f); return x; }, 2, peak: 0.5f);

            // ── Small actions ───────────────────────────────────────────────────────────────────────
            R(SfxIds.Pickup, g => { float[] x = g.Buffer(0.25f); Dsp.Grains(x, g, 0f, 0.2f, 30, 0.4f, 0.003f, u => Dsp.Bell(u, 1f, 0.3f)); Dsp.BP(x, 2000f, 0.8f, Rate); return x; }, 2, peak: 0.45f);
            R(SfxIds.Eat, g => { float[] x = g.Buffer(0.6f); Dsp.Grains(x, g, 0f, 0.5f, 50, 0.5f, 0.002f, u => 0.5f + 0.5f * MathF.Sin(u * 30f)); Dsp.LP(x, 3000f, Rate); return x; }, 2, peak: 0.5f);
            R(SfxIds.Drink, g => { float[] x = g.Buffer(0.9f); for (int k = 0; k < 3; k++) Dsp.Mix(x, Dsp.Bubble(Rate, g.Range(250f, 420f), 0.18f, 1.6f), g.N(k * 0.28f), 0.8f); return x; }, peak: 0.5f);
            R(SfxIds.Bandage, g => { float[] x = g.Buffer(0.7f); Dsp.Grains(x, g, 0f, 0.6f, 120, 0.5f, 0.0012f, u => 1f - u * 0.5f); Dsp.BP(x, 3200f, 0.9f, Rate); return x; }, 2, peak: 0.5f);
            R(SfxIds.Injector, g => { float[] x = g.Buffer(0.6f); AddClick(x, g, 0f, 2400f, 0.01f, 0.8f); float[] h = Dsp.White(g, g.N(0.4f), 0.3f); Dsp.HP(h, 3500f, Rate); Dsp.Shape(h, Rate, t => Dsp.AD(t, 0.02f, 0.15f)); Dsp.Mix(x, h, g.N(0.05f)); return x; }, peak: 0.5f);
            R(SfxIds.PageTurn, g => { float[] x = Dsp.White(g, g.N(0.4f), 0.5f); Dsp.SweepBP(x, Rate, 1.5f, u => 1500f + 2000f * u); Dsp.Shape(x, Rate, t => Dsp.Bell(t, 0.4f, 0.4f)); return x; }, 2, peak: 0.45f);
            R(SfxIds.Splash, g => { float[] x = Dsp.White(g, g.N(0.9f), 0.7f); Dsp.LP(x, 3000f, Rate); Dsp.Shape(x, Rate, t => Dsp.AD(t, 0.01f, 0.15f)); for (int k = 0; k < 4; k++) Dsp.Mix(x, Dsp.Bubble(Rate, g.Range(400f, 900f), 0.1f, 1.5f), g.N(g.Range(0.05f, 0.6f)), 0.4f); return x; }, 2);
            R(SfxIds.Crank, g => { float[] x = g.Buffer(1.1f); for (int k = 0; k < 12; k++) AddClick(x, g, k / 12f, 1700f + (k % 2) * 300f, 0.012f, 0.6f); return x; }, peak: 0.6f);
            R(SfxIds.Lighter, g => { float[] x = g.Buffer(0.8f); AddClick(x, g, 0f, 3000f, 0.006f, 0.8f); float[] w = Dsp.White(g, g.N(0.6f), 0.3f); Dsp.BP(w, 800f, 0.7f, Rate); Dsp.Shape(w, Rate, t => Dsp.AD(t, 0.05f, 0.25f)); Dsp.Mix(x, w, g.N(0.05f)); return x; }, peak: 0.5f);
            R(SfxIds.Handcuffs, g => { float[] x = g.Buffer(0.7f); for (int k = 0; k < 6; k++) Dsp.Ring(x, Rate, k * 0.05f, 0.4f, 2800f, new[] { 1f, 2.2f, 3.7f }, 0.03f); return x; }, peak: 0.6f);
        }
    }
}
