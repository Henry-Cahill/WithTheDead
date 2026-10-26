using System;
using UnityEngine;

namespace CloneZ
{
    // Footsteps on every surface, for the survivor and everything else that walks.
    public sealed partial class SfxSynth
    {
        partial void RegisterFootsteps()
        {
            // ── Footsteps ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.StepSnow, g => { float[] x = g.Buffer(0.25f); Dsp.Grains(x, g, 0f, 0.16f, 70, 0.6f, 0.0015f, u => Dsp.Bell(u, 1f, 0.3f)); Dsp.LP(x, 4500f, Rate); return x; }, 4);
            R(SfxIds.StepWood, g => { float[] x = g.Buffer(0.25f); Dsp.Thump(x, Rate, 0f, 0.8f, g.Vary(140f, 0.1f), 90f, 0.02f, 0.002f, 0.04f); Dsp.AddBurst(x, g, 0f, 0.3f, 0.001f, 0.015f, 1200f); return x; }, 4);
            R(SfxIds.StepConcrete, g => { float[] x = g.Buffer(0.2f); Dsp.AddBurst(x, g, 0f, 0.6f, 0.0008f, 0.01f, 0f, 1400f); Dsp.Thump(x, Rate, 0f, 0.3f, 220f, 140f, 0.01f, 0.001f, 0.02f); return x; }, 4);
            R(SfxIds.StepGrass, g => { float[] x = g.Buffer(0.3f); Dsp.Grains(x, g, 0f, 0.2f, 40, 0.4f, 0.003f, u => Dsp.Bell(u, 1f, 0.4f)); Dsp.BP(x, 2500f, 0.8f, Rate); return x; }, 4);
        }
    }
}
