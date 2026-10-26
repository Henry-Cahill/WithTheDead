using System;
using UnityEngine;

namespace CloneZ
{
    // The dead, animals, mutants and the uncanny voices.
    public sealed partial class SfxSynth
    {
        partial void RegisterCreatures()
        {
            // ── The dead ────────────────────────────────────────────────────────────────────────────
            R(SfxIds.ZombieGroan, g => Dsp.Voice(g, g.Range(1.1f, 1.7f), new VoiceParams
            {
                F0 = u => g.Vary(85f, 0.15f) + 18f * MathF.Sin(u * 7f), Formants = u => Vowel.Lerp(Vowel.Uh, Vowel.O, u),
                Amp = u => Dsp.Bell(u, 1f, 0.35f), Rough = 0.75f, Gurgle = 0.45f, Breath = 0.4f, Jitter = 0.03f,
            }), 4);
            R(SfxIds.ZombieAttack, g => Dsp.Voice(g, 0.6f, new VoiceParams
            {
                F0 = u => 150f - 40f * u, Formants = u => Vowel.A, Amp = u => Dsp.AD(u, 0.08f, 0.35f), Rough = 0.95f, Gurgle = 0.3f, Breath = 0.5f,
            }), 3);
            R(SfxIds.ZombieDie, g => Dsp.Voice(g, 1.2f, new VoiceParams
            {
                F0 = u => Dsp.Glide(120f, 55f, u), Formants = u => Vowel.Lerp(Vowel.Aw, Vowel.U, u), Amp = u => Dsp.Bell(u, 1f, 0.2f), Rough = 0.8f, Gurgle = 0.8f, Breath = 0.45f,
            }), 2);

            // ── Psi ─────────────────────────────────────────────────────────────────────────────────
            R(SfxIds.PsiWhisper, g => Whisper(g, 1.6f), 3);

            // ── Animals and mutants ─────────────────────────────────────────────────────────────────
            R(SfxIds.WolfHowl, g =>
            {
                float len = 2.6f;
                float[] x = g.Buffer(len + 1f);
                float ph = 0f;
                for (int i = 0; i < g.N(len); i++)
                {
                    float u = i / (float)g.N(len);
                    float f = 380f + 260f * Dsp.Bell(u, 1f, 0.3f) - 80f * u;
                    ph += f * (1f + 0.01f * MathF.Sin(i * 0.002f)) / Rate;
                    x[i] = (MathF.Sin(Dsp.TwoPi * ph) + 0.2f * MathF.Sin(Dsp.TwoPi * ph * 2f)) * Dsp.Bell(u, 1f, 0.25f);
                }
                Dsp.Reverb(x, Rate, 0.8f, 0.4f, 0.45f);
                return x;
            }, 2);
            R(SfxIds.BearRoar, g => Dsp.Voice(g, 1.6f, new VoiceParams { F0 = u => 75f + 25f * Dsp.Bell(u, 1f, 0.3f), Formants = u => Vowel.A, Amp = u => Dsp.Bell(u, 1f, 0.25f), Rough = 1f, Breath = 0.6f }), 2);
            R(SfxIds.MutantScreech, g =>
            {
                float[] x = Dsp.Voice(g, 1f, new VoiceParams { F0 = u => 700f + 400f * u, Formants = u => Vowel.I, Amp = u => Dsp.AD(u, 0.05f, 0.35f), Rough = 0.9f, Breath = 0.6f });
                float[] n = Dsp.White(g, x.Length, 0.5f);
                Dsp.SweepBP(n, Rate, 3f, u => 1500f + 3000f * u);
                Dsp.Shape(n, Rate, t => Dsp.AD(t, 0.02f, 0.3f));
                Dsp.Mix(x, n, 0);
                return x;
            }, 2);
            R(SfxIds.MutantGrowl, g => Dsp.Voice(g, 1.3f, new VoiceParams { F0 = u => 55f + 10f * MathF.Sin(u * 12f), Formants = u => Vowel.U, Amp = u => Dsp.Bell(u, 1f, 0.4f), Rough = 1f, Gurgle = 0.6f, Breath = 0.4f }), 3);
            R(SfxIds.DeerCall, g => Dsp.Voice(g, 0.5f, new VoiceParams { F0 = u => 520f - 120f * u, Formants = u => Vowel.E, Amp = u => Dsp.Bell(u, 1f, 0.3f), Breath = 0.3f }));

            // ── The uncanny ─────────────────────────────────────────────────────────────────────────
            R(SfxIds.UncannyWhisper, g => Whisper(g, 2.2f), 3, peak: 0.5f);
            R(SfxIds.RadioStatic, g =>
            {
                float[] x = Dsp.White(g, g.N(1.6f), 0.6f);
                Dsp.BP(x, 1900f, 1f, Rate);
                Dsp.Grains(x, g, 0f, 1.5f, 60, 0.6f, 0.001f, u => 1f);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 0.05f, 1.3f, 0.2f));
                return x;
            }, 2, peak: 0.55f);
            R(SfxIds.Stinger, g =>
            {
                float len = 2.6f;
                float[] x = g.Buffer(len + 0.8f);
                float[] hz = { 220f, 233.1f, 311.1f, 466.2f };
                for (int i = 0; i < g.N(len); i++)
                {
                    float t = i / (float)Rate;
                    float env = MathF.Pow(t / len, 2.2f);
                    float s = 0f;
                    for (int k = 0; k < hz.Length; k++) s += MathF.Sin(Dsp.TwoPi * hz[k] * t * (1f + 0.002f * k * t));
                    x[i] = s * 0.25f * env;
                }
                Dsp.AddBurst(x, g, len - 0.05f, 1f, 0.002f, 0.25f, 3000f);
                Dsp.Reverb(x, Rate, 0.9f, 0.3f, 0.5f);
                return x;
            }, 2, peak: 0.7f);
        }
    }
}
