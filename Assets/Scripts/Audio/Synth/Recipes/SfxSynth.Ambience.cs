using System;
using UnityEngine;

namespace CloneZ
{
    // Ambient beds, weather, wildlife and the diegetic music of the world.
    public sealed partial class SfxSynth
    {
        partial void RegisterAmbience()
        {
            // ── Ambience and music ──────────────────────────────────────────────────────────────────
            R(SfxIds.WindAmbience, g => Loop(g, 8f, x =>
            {
                float[] p = Dsp.Pink(g, x.Length);
                float f = 1f / 8f;
                Dsp.SweepBP(p, Rate, 0.8f, u => 380f + 700f * (0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * (u * 8f * f * 2f))));
                Dsp.Mix(x, p, 0);
            }), loop: true, peak: 0.5f);
            R(SfxIds.ForestAmbience, g => Loop(g, 10f, x =>
            {
                float[] p = Dsp.Pink(g, x.Length);
                Dsp.LP(p, 900f, Rate);
                Dsp.Mix(x, p, 0, 0.6f);
                for (int b = 0; b < 4; b++)
                {
                    var chirp = new float[g.N(0.25f)];
                    float f0 = g.Range(2500f, 4200f), ph = 0f;
                    for (int i = 0; i < chirp.Length; i++)
                    {
                        float t = i / (float)Rate;
                        ph += (f0 + 900f * MathF.Sin(t * 60f)) / Rate;
                        chirp[i] = MathF.Sin(Dsp.TwoPi * ph) * Dsp.Bell(t, 0.25f, 0.3f) * 0.25f;
                    }
                    Dsp.MixWrap(x, chirp, g.N(g.Range(0f, 9.5f)));
                }
                Dsp.MixWrap(x, Dsp.Creak(g, 1.2f, u => 9f + 5f * u, u => Dsp.Bell(u, 1f, 0.5f) * 0.4f, new[] { 300f, 700f }, new[] { 5f, 6f }, 0.4f), g.N(4f));
            }), loop: true, peak: 0.45f);
            R(SfxIds.BarAmbience, g => Loop(g, 10f, x =>
            {
                for (int v = 0; v < 14; v++)
                {
                    float[] voice = Dsp.Voice(g, g.Range(0.8f, 1.8f), new VoiceParams
                    {
                        F0 = u => g.Vary(g.Chance(0.5f) ? 120f : 200f, 0.15f) * (1f + 0.1f * MathF.Sin(u * 9f)),
                        Formants = u => Vowel.Speech[(int)(u * 6f) % Vowel.Speech.Length], Amp = u => Dsp.Bell(u, 1f, 0.5f), Breath = 0.2f,
                    });
                    Dsp.MixWrap(x, voice, g.N(g.Range(0f, 9.8f)), 0.25f);
                }
                for (int c = 0; c < 5; c++)
                {
                    var clink = new float[g.N(0.4f)];
                    Dsp.Ring(clink, Rate, 0f, 0.3f, g.Range(2600f, 3600f), new[] { 1f, 2.3f, 3.9f }, 0.1f);
                    Dsp.MixWrap(x, clink, g.N(g.Range(0f, 9.8f)));
                }
                Dsp.LoopLP(x, 2500f, Rate);
                Dsp.Reverb(x, Rate, 0.5f, 0.5f, 0.3f, 1f, true);
            }), loop: true, peak: 0.45f);
            R(SfxIds.Guitar, g => Loop(g, 12f, x =>
            {
                float[] notes = { 57, 60, 64, 69, 67, 64, 60, 62, 55, 59, 62, 67, 64, 62, 59, 57 };
                float beat = 0.75f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float[] pluck = Pluck(g, Dsp.Midi(notes[i]), 1.6f);
                    Dsp.MixWrap(x, pluck, g.N(i * beat), 0.5f);
                    if (i % 4 == 0) Dsp.MixWrap(x, Pluck(g, Dsp.Midi(notes[i] - 12f), 2.2f), g.N(i * beat), 0.35f);
                }
                Dsp.Reverb(x, Rate, 0.4f, 0.6f, 0.25f, 1f, true);
            }), loop: true, peak: 0.6f);
        }
    }
}
