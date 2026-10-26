using System;
using UnityEngine;

namespace CloneZ
{
    // The building blocks of the original recipes (22.05 kHz). Reworked recipes keep their own helpers in their family file.
    public sealed partial class SfxSynth
    {
        #region Building blocks
        // A gunshot: the supersonic crack, the bloom of the charge, and the tail rolling off the surroundings.
        private static float[] Gun(SynthGen g, float len, float bodyHz, float bodyEnd, float bodyTau, float crackLp, float tailTau, float echo)
        {
            float[] x = g.Buffer(len);
            Dsp.AddBurst(x, g, 0f, 1f, 0.0004f, 0.006f, crackLp);
            Dsp.Thump(x, Rate, 0f, 0.95f, g.Vary(bodyHz, 0.06f), bodyEnd, 0.025f, 0.0008f, bodyTau);
            Dsp.AddBurst(x, g, 0.002f, 0.55f, 0.002f, tailTau, 1800f);
            Dsp.Echo(x, Rate, g.Range(0.09f, 0.16f), echo, 2200f, 0.35f);
            return x;
        }

        private static float[] Whoosh(SynthGen g, float len, float hz0, float hz1, float q)
        {
            float[] x = Dsp.White(g, g.N(len), 0.8f);
            Dsp.SweepBP(x, Rate, 1f / Math.Max(0.1f, q), u => Dsp.Lerp(hz0, hz1, u));
            Dsp.Shape(x, Rate, t => Dsp.Bell(t, len, 0.45f));
            return x;
        }

        private static float[] Click(SynthGen g, float hz, float tau)
        {
            float[] x = g.Buffer(0.15f);
            AddClick(x, g, 0f, hz, tau, 1f);
            return x;
        }

        private static void AddClick(float[] x, SynthGen g, float at, float hz, float tau, float gain)
        {
            at = Mathf.Clamp01(at);
            Dsp.Ring(x, Rate, at, gain * 0.6f, g.Vary(hz, 0.05f), new[] { 1f, 2.7f, 5.4f }, tau);
            Dsp.AddBurst(x, g, at, gain * 0.5f, 0.0003f, 0.002f, 0f, 1500f);
        }

        private static float[] Tone(SynthGen g, float len, float hz, float attack, float tau)
        {
            float[] x = g.Buffer(len);
            MixTone(x, 0f, hz, attack, tau, 1f);
            return x;
        }

private static void MixTone(float[] x, float at, float hz, float attack, float tau, float gain)
    {
        int start = (int)(at * Rate);
        if (start < 0) start = 0;
        if (start >= x.Length) return;
        for (int i = start; i < x.Length; i++)
        {
            float t = (i - start) / (float)Rate;
            x[i] += gain * MathF.Sin(Dsp.TwoPi * hz * t) * Dsp.AD(t, attack, tau);
        }
    }

private static float[] Whisper(SynthGen g, float len)
    {
        float[] x = Dsp.Voice(g, len, new VoiceParams
        {
            Whisper = true,
            Formants = u => Vowel.Speech[(int)(u * 9f) % Vowel.Speech.Length],
            Amp = u => Dsp.Bell(u, 1f, 0.5f) * (0.6f + 0.4f * MathF.Sin(u * 40f)),
        });
        // Apply reverb directly, then blend a short faded tail without extra silence.
        var rev = new float[x.Length];
        Array.Copy(x, rev, x.Length);
        Dsp.Reverb(rev, Rate, 0.85f, 0.4f, 0.5f);
        // Mix a short 0.2 s tail (if buffer long enough) with linear fade‑out.
        int tailSamples = Math.Min(g.N(0.2f), rev.Length);
        for (int i = 0; i < tailSamples; i++)
        {
            float fade = 1f - (i / (float)tailSamples);
            int idx = rev.Length - tailSamples + i;
            rev[idx] = rev[idx] * (1f - fade) + rev[i] * fade;
        }
        return rev;
    }

        // Karplus-Strong: a plucked string from a noise burst in a damped delay line.
        private static float[] Pluck(SynthGen g, float hz, float len)
        {
            int n = g.N(len);
            int period = Math.Max(2, (int)(Rate / hz));
            var line = new float[period];
            for (int i = 0; i < period; i++) line[i] = g.Bipolar();
            var x = new float[n];
            int k = 0;
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float cur = line[k];
                float next = 0.996f * 0.5f * (cur + prev);
                prev = cur;
                line[k] = next;
                x[i] = cur;
                if (++k >= period) k = 0;
            }
            Dsp.LP(x, 3500f, Rate);
            return x;
        }
        #endregion
    }
}
