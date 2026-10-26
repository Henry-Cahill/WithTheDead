using System;
using System.Collections.Generic;

namespace CloneZ
{
    // The dead, the beasts, the mutants and the uncanny voices. Every creature here is a throat (CreatureKit.Voice): a
    // glottal source with per-period jitter and shimmer, vocal fry, period-doubling growl, rasp, gurgle and rattle, through
    // a five-formant tract sized to the animal; then breath, liquid, teeth and, for the far-carrying calls, space.
    public sealed partial class SfxSynth
    {
        partial void RegisterCreatures()
        {
            // ── The dead ────────────────────────────────────────────────────────────────────────────
            // Every zombie vocal is laid out throat by throat (see ZombieThroats): ZombieVoices asks each zombie's own.
            const int Throats = ZombieThroats.Count;
            Add(SfxIds.ZombieIdle, g => CreatureKit.ZombieIdle(g), Throats * ZombieThroats.Idles, BedRate).Peak(0.7f).Priority(150);
            Add(SfxIds.ZombieGroan, g => CreatureKit.ZombieGroan(g), Throats * ZombieThroats.Groans, BedRate).Peak(0.8f).Priority(120);
            Add(SfxIds.ZombieAlert, g => CreatureKit.ZombieAlert(g), Throats, BedRate).Peak(0.9f).Priority(80).Duck(-2f, 20f, 0.3f, 1.5f);
            Add(SfxIds.ZombieChase, g => CreatureKit.ZombieChase(g), Throats, BedRate).Peak(0.9f).Priority(90).Duck(-2f, 25f, 0.3f, 1.5f);
            Add(SfxIds.ZombieCrawl, g => CreatureKit.ZombieCrawl(g), Throats * ZombieThroats.Crawls, BedRate).Peak(0.75f).Priority(120);
            Add(SfxIds.ZombieAttack, g => CreatureKit.ZombieAttack(g), Throats, BedRate).Peak(0.9f).Priority(70);
            Add(SfxIds.ZombieHurt, g => CreatureKit.ZombieHurt(g), Throats, BedRate).Peak(0.85f).Priority(110);
            Add(SfxIds.ZombieDie, g => CreatureKit.ZombieDie(g), Throats, BedRate).Peak(0.85f).Priority(80).Then(SfxIds.BodyFall, 0.78f, 0.05f, 0.85f);
            Add(SfxIds.BodyFall, g => CreatureKit.BodyFall(g, 1f), 4, BedRate).Peak(0.9f).Priority(110);
            Add(SfxIds.BodyFallHeavy, g => CreatureKit.BodyFall(g, 2.4f), 2, BedRate).Peak(0.95f).Priority(90);

            // ── Wildlife ────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.WolfHowl, g => CreatureKit.WolfHowl(g), 3, BedRate).Peak(0.8f).Priority(90).Range(70f).Reverb(0.4f);
            Add(SfxIds.WolfGrowl, g => CreatureKit.WolfGrowl(g), 3, BedRate).Peak(0.85f).Priority(90);
            Add(SfxIds.WolfBark, g => CreatureKit.WolfBark(g), 3).Peak(0.9f).Priority(90);
            Add(SfxIds.WolfYelp, g => CreatureKit.WolfYelp(g), 3).Peak(0.85f).Priority(100);
            Add(SfxIds.WolfDie, g => CreatureKit.WolfDie(g), 2).Peak(0.85f).Priority(80).Then(SfxIds.BodyFall, 0.42f, 0.05f, 0.7f);
            Add(SfxIds.BearRoar, g => CreatureKit.BearRoar(g), 3, BedRate).Peak(0.9f).Priority(60).Duck(-3f, 35f, 0.5f, 2f);
            Add(SfxIds.BearHuff, g => CreatureKit.BearHuff(g), 3, BedRate).Peak(0.85f).Priority(90);
            Add(SfxIds.BearDie, g => CreatureKit.BearDie(g), 2, BedRate).Peak(0.9f).Priority(70).Then(SfxIds.BodyFallHeavy, 0.45f, 0.05f, 0.9f);
            Add(SfxIds.DeerCall, g => CreatureKit.DeerCall(g), 3).Peak(0.8f).Priority(130);
            Add(SfxIds.DeerSnort, g => CreatureKit.DeerSnort(g), 3).Peak(0.85f).Priority(120);
            Add(SfxIds.DeerDie, g => CreatureKit.DeerDie(g), 2).Peak(0.85f).Priority(90).Then(SfxIds.BodyFall, 0.45f, 0.05f, 0.75f);

            // ── Mutants ─────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.MutantScreech, g => CreatureKit.MutantScreech(g), 3).Peak(0.85f).Priority(70);
            Add(SfxIds.MutantGrowl, g => CreatureKit.MutantGrowl(g), 4, BedRate).Peak(0.85f).Priority(80);
            Add(SfxIds.MutantDie, g => CreatureKit.MutantDie(g), 3, BedRate).Peak(0.85f).Priority(70).Then(SfxIds.BodyFall, 0.45f, 0.05f, 0.8f);
            Add(SfxIds.HoundBark, g => CreatureKit.HoundBark(g), 3).Peak(0.9f).Priority(80);
            Add(SfxIds.HoundSnarl, g => CreatureKit.HoundSnarl(g), 3).Peak(0.9f).Priority(80);
            Add(SfxIds.ShadeShriek, g => CreatureKit.ShadeShriek(g), 3, BedRate).Peak(0.8f).Priority(70).Reverb(0.5f);
            Add(SfxIds.ThumperBellow, g => CreatureKit.ThumperBellow(g), 2, BedRate).Peak(0.9f).Priority(60).Range(50f).Duck(-3f, 50f, 0.6f, 2.5f);
            Add(SfxIds.ThumperSlam, g => CreatureKit.ThumperSlam(g), 3, BedRate).Peak(0.95f).Priority(50).Range(60f).Duck(-4f, 45f, 0.4f, 1.8f);
            Add(SfxIds.ThumperDie, g => CreatureKit.ThumperDie(g), 1, BedRate).Peak(0.9f).Priority(60).Then(SfxIds.BodyFallHeavy, 0.5f, 0.05f, 1f);
            Add(SfxIds.PuppeteerMoan, g => CreatureKit.PuppeteerMoan(g), 2, BedRate).Peak(0.75f).Priority(70).Spread(90f).Reverb(0.5f);

            // ── The uncanny ─────────────────────────────────────────────────────────────────────────
            Add(SfxIds.PsiWhisper, g => CreatureKit.PsiWhisper(g), 3, BedRate).Peak(0.8f).Priority(60).Spread(120f).Reverb(0.4f);
            Add(SfxIds.UncannyWhisper, g => CreatureKit.UncannyWhisper(g), 3, BedRate).Peak(0.5f).Priority(80).Reverb(0.5f);
            Add(SfxIds.RadioStatic, g => CreatureKit.RadioStatic(g), 2, BedRate).Peak(0.6f).Priority(70);
            AddStereo(SfxIds.Stinger, g => CreatureKit.Stinger(g), 2, BedRate).Peak(0.75f).Priority(20).Duck(-3f, 10f, 1f, 3f);
        }

        private static class CreatureKit
        {
            #region Throat
            private static readonly Vowel Mm = new Vowel(260f, 1000f, 2200f, 120f, 260f, 360f);   // lips shut: a hum
            private static readonly Vowel Oo = new Vowel(340f, 720f, 2300f, 80f, 110f, 170f);     // a howl's round mouth
            private static readonly Vowel Nn = new Vowel(290f, 1650f, 2500f, 150f, 300f, 400f);   // through the nose: a whine, a bleat

            // One formant's coefficients: a two-pole resonator with unity gain at DC (Klatt), y = a·x + b·y1 + c·y2.
            private static void Klatt(float hz, float bw, int rate, out float a, out float b, out float c)
            {
                hz = Math.Max(30f, Math.Min(hz, rate * 0.45f));
                float r = MathF.Exp(-Dsp.Pi * Math.Max(15f, bw) / rate);
                c = -r * r;
                b = 2f * r * MathF.Cos(Dsp.TwoPi * hz / rate);
                a = 1f - b - c;
            }

            // A one-pole's feedback coefficient (z = x + (z - x)·k), and the factor that brings its output, fed white
            // noise in [-1, 1), to unit deviation.
            private static float Pole(float hz, int rate) => MathF.Exp(-Dsp.TwoPi * Math.Min(Math.Max(hz, 0.01f), rate * 0.45f) / rate);
            private static float PoleNorm(float k) => 1f / MathF.Sqrt(Math.Max(1e-12f, (1f / 3f) * (1f - k) / (1f + k)));

            /// <summary>One throat. Curves take u = 0..1 over the sound.</summary>
            internal sealed class Throat
            {
                public Func<float, float> F0 = u => 110f, Amp = u => 1f;
                public Func<float, Vowel> Shape = u => Vowel.Uh;
                /// <summary>Voiced share 0..1 (null: fully voiced); below it, breath alone.</summary>
                public Func<float, float> Voicing;
                /// <summary>Creak 0..1: the pitch sinks toward <see cref="FryHz"/> and the pulses scatter.</summary>
                public Func<float, float> Fry;
                public float BreathLevel = 0.1f, Hiss = 0.2f, Tract = 1f, Widen = 1f;
                public float FryHz = 42f, Jitter = 0.012f, Shimmer = 0.08f, Drift = 0.015f, DriftHz = 2.5f;
                /// <summary>Period doubling (tripling with <see cref="SubRatio"/> 3): the growl.</summary>
                public float Sub;
                public int SubRatio = 2;
                /// <summary>Turbulence riding each pulse (false folds, a torn throat).</summary>
                public float Rasp;
                /// <summary>Fluid in the throat: a gargling flutter of the airflow and a wobble of the formants.</summary>
                public float Wet, WetHz = 24f;
                /// <summary>A periodic flap of loose tissue or phlegm (a death rattle, a snore).</summary>
                public float Rattle, RattleHz = 17f;
                /// <summary>The source's low-pass: darker when quiet, brighter as it swells.</summary>
                public float Tilt = 2600f;
                /// <summary>Extra excitation at the glottis (gurgle pops), sample for sample.</summary>
                public float[] Excite;
            }

            /// <summary>
            /// A throat speaking: a band-limited glottal pulse train whose every period is scattered in length and level
            /// (jitter, shimmer, creak, period doubling), with breath and rasp, gargled and rattled, through a cascade of
            /// five formants (F1-F3 from the vowel, F4-F5 fixed; all scaled by the tract).
            /// </summary>
            internal static float[] Voice(SynthGen g, float seconds, Throat p)
            {
                const int Block = 16;
                int rate = g.Rate, n = g.N(seconds);
                var y = new float[n];
                // The loudness and pitch lines at a control rate (interpolated), the slower controls every other block.
                int nc = n / Block + 2;
                var ampC = new float[nc];
                var f0C = new float[nc];
                for (int c = 0; c < nc; c++)
                {
                    float u = Math.Min(1f, c * Block / (float)n);
                    ampC[c] = p.Amp(u);
                    f0C[c] = p.F0(u);
                }
                float dt = 1f / rate, drift = p.Drift, fryHz = p.FryHz, jitter = p.Jitter, shimmer = p.Shimmer;
                float subAmt = p.Sub, rasp = p.Rasp, wetAmt = p.Wet, rattleAmt = p.Rattle, rattleHz = p.RattleHz, hiss = p.Hiss;
                float a1 = 0f, b1 = 0f, c1 = 0f, a2 = 0f, b2 = 0f, c2 = 0f, a3 = 0f, b3 = 0f, c3 = 0f, a4 = 0f, b4 = 0f, c4 = 0f, a5 = 0f, b5 = 0f, c5 = 0f;
                float y11 = 0f, y12 = 0f, y21 = 0f, y22 = 0f, y31 = 0f, y32 = 0f, y41 = 0f, y42 = 0f, y51 = 0f, y52 = 0f;
                float tiltK = 0f, tiltZ = 0f, driftK = Pole(p.DriftHz, rate), driftZ = 0f, driftN = PoleNorm(driftK);
                float wetK = Pole(p.WetHz, rate), wetZ = 0f, wetN = PoleNorm(wetK), sloshK = Pole(5f, rate), sloshZ = 0f, sloshN = PoleNorm(sloshK);
                float airK = Pole(2500f, rate), airZ = 0f;
                float phase = g.Next(), periodK = 1f, pulse = 1f, rattlePh = g.Next(), rattleK = 1f, flap = 0f, flapK = 1f, sloshV = 0f;
                float fry = 0f, breath = p.BreathLevel, voicing = 1f;
                int count = 0, ratio = Math.Max(2, p.SubRatio);
                bool wet = wetAmt > 0f, rattle = rattleAmt > 0f;
                float[] excite = p.Excite;
                int exciteN = excite != null ? Math.Min(excite.Length, n) : 0;
                uint s = g.NextUInt() | 1u;
                for (int c = 0, i = 0; i < n; c++)
                {
                    float amp0 = ampC[c], ampD = (ampC[c + 1] - amp0) * (1f / Block);
                    float f00 = f0C[c], f0D = (f0C[c + 1] - f00) * (1f / Block);
                    if ((c & 1) == 0)
                    {
                        float u = i / (float)n;
                        fry = p.Fry != null ? Dsp.Clamp01(p.Fry(u)) : 0f;
                        voicing = p.Voicing != null ? Dsp.Clamp01(p.Voicing(u)) : 1f;
                        Vowel v = p.Shape(u);
                        float k = p.Tract * (1f + 0.07f * wetAmt * sloshV), w = p.Widen;
                        Klatt(v.F1 * k, v.B1 * w, rate, out a1, out b1, out c1);
                        Klatt(v.F2 * k, v.B2 * w, rate, out a2, out b2, out c2);
                        Klatt(v.F3 * k, v.B3 * w, rate, out a3, out b3, out c3);
                        Klatt(3300f * k, 260f * w, rate, out a4, out b4, out c4);
                        Klatt(4250f * k, 330f * w, rate, out a5, out b5, out c5);
                        tiltK = Pole(p.Tilt * (0.35f + 0.65f * Math.Min(1f, Math.Abs(amp0))), rate);
                    }
                    int end = Math.Min(n, i + Block);
                    for (int j = 0; i < end; i++, j++)
                    {
                        // Uniform draws in [-1, 1) from an inline xorshift (seeded from the generator).
                        s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                        float rDrift = (int)s * 4.656613e-10f;
                        s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                        float rRasp = (int)s * 4.656613e-10f;
                        s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                        float rBreath = (int)s * 4.656613e-10f;
                        s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                        float rAir = (int)s * 4.656613e-10f;

                        driftZ = rDrift + (driftZ - rDrift) * driftK;
                        float wander = drift * driftN * driftZ;
                        wander = wander < -0.4f ? -0.4f : wander > 0.4f ? 0.4f : wander;
                        float f0 = (f00 + f0D * j) * (1f + wander);
                        if (fry > 0f) f0 += (fryHz * (1f + wander) - f0) * fry;
                        float step = (f0 > 4f ? f0 : 4f) * periodK * dt;
                        phase += step;
                        if (phase >= 1f)
                        {
                            phase -= 1f;
                            count = (count + 1) % ratio;
                            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                            periodK = 1f + (int)s * 4.656613e-10f * (jitter + 0.12f * fry);
                            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                            pulse = 1f + (int)s * 4.656613e-10f * (shimmer + 0.35f * fry);
                            if (pulse < 0.05f) pulse = 0.05f;
                            if (subAmt > 0f && count != 0)
                            {
                                pulse *= 1f - 0.6f * subAmt;
                                periodK *= 1f + 0.05f * subAmt;
                            }
                        }
                        // A band-limited sawtooth (polyBLEP at the reset), tilted by a one-pole.
                        float dtc = step < 0.5f ? step : 0.5f, saw = 2f * phase - 1f;
                        if (phase < dtc)
                        {
                            float t = phase / dtc;
                            saw -= t + t - t * t - 1f;
                        }
                        else if (phase > 1f - dtc)
                        {
                            float t = (phase - 1f) / dtc;
                            saw -= t * t + t + t + 1f;
                        }
                        tiltZ = saw + (tiltZ - saw) * tiltK;
                        float open = phase < 0.35f ? 1f - phase * 2.857143f : 0f;
                        float src = tiltZ * pulse * voicing
                            + rasp * rRasp * (0.2f + open) * pulse * (0.3f + 0.7f * voicing)
                            + breath * rBreath * (voicing > 0.5f && phase >= 0.5f ? 0.45f : 1f);
                        if (i < exciteN) src += excite[i];
                        if (wet)
                        {
                            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                            float rw = (int)s * 4.656613e-10f;
                            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                            float rs = (int)s * 4.656613e-10f;
                            wetZ = rw + (wetZ - rw) * wetK;
                            sloshZ = rs + (sloshZ - rs) * sloshK;
                            sloshV = sloshZ * sloshN;
                            float wv = wetZ * wetN, gate = 1f - wetAmt + wetAmt * wv * wv;
                            src *= gate > 0f ? gate : 0f;
                        }
                        if (rattle)
                        {
                            flap *= flapK;
                            rattlePh += rattleHz * rattleK * dt;
                            if (rattlePh >= 1f)
                            {
                                rattlePh -= 1f;
                                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                                rattleK = 0.7f + 0.3f * ((int)s * 4.656613e-10f + 1f);
                                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                                flap = 0.35f + 0.325f * ((int)s * 4.656613e-10f + 1f);
                                flapK = MathF.Exp(-6f * rattleHz * rattleK * dt);
                            }
                            src *= 1f - rattleAmt + rattleAmt * 2.2f * flap;
                        }
                        float o1 = a1 * src + b1 * y11 + c1 * y12; y12 = y11; y11 = o1;
                        float o2 = a2 * o1 + b2 * y21 + c2 * y22; y22 = y21; y21 = o2;
                        float o3 = a3 * o2 + b3 * y31 + c3 * y32; y32 = y31; y31 = o3;
                        float o4 = a4 * o3 + b4 * y41 + c4 * y42; y42 = y41; y41 = o4;
                        float o5 = a5 * o4 + b5 * y51 + c5 * y52; y52 = y51; y51 = o5;
                        airZ = rAir + (airZ - rAir) * airK;
                        y[i] = (o5 + (rAir - airZ) * breath * hiss) * (amp0 + ampD * j);
                    }
                }
                return y;
            }
            #endregion

            #region Shapes
            // A loudness line: a quick onset, a few slow swells of `depth`, an easy release (attack and release in seconds).
            internal static Func<float, float> Swells(SynthGen g, float seconds, float attack, float release, float depth, int minCycles = 1, int maxCycles = 3)
            {
                Func<float, float> gust = Dsp.Gusts(g, 1f, minCycles, maxCycles, 3);
                float a = Math.Max(1e-3f, attack / seconds), r = Math.Max(1e-3f, release / seconds);
                return u => (u < a ? Dsp.SmoothStep(u / a) : 1f) * (u > 1f - r ? 1f - Dsp.SmoothStep((u - 1f + r) / r) : 1f) * (1f - depth + depth * gust(u));
            }

            // Vowels at times (u, rising) with smooth glides between them.
            internal static Func<float, Vowel> Path(Vowel[] vs, float[] at)
            {
                return u =>
                {
                    if (u <= at[0]) return vs[0];
                    for (int i = 1; i < at.Length; i++)
                        if (u < at[i]) return Vowel.Lerp(vs[i - 1], vs[i], Dsp.SmoothStep((u - at[i - 1]) / (at[i] - at[i - 1])));
                    return vs[vs.Length - 1];
                };
            }

            // Where u is between a and b, 0..1 (clamped).
            internal static float Span(float u, float a, float b) => Dsp.Clamp01((u - a) / Math.Max(1e-5f, b - a));

            // 1 between a and b with smooth edges `rise` and `fall` wide, 0 outside.
            internal static float Gate(float u, float a, float b, float rise, float fall)
            {
                if (u <= a || u >= b) return 0f;
                float on = rise > 0f ? Dsp.SmoothStep((u - a) / rise) : 1f;
                float off = fall > 0f ? Dsp.SmoothStep((b - u) / fall) : 1f;
                return on * off;
            }

            // Separate bursts: each [a, b] at its level, soft edged.
            internal static Func<float, float> Pulses(float[] a, float[] b, float[] level, float rise, float fall)
            {
                return u =>
                {
                    float s = 0f;
                    for (int i = 0; i < a.Length; i++) s = Math.Max(s, level[i] * Gate(u, a[i], b[i], rise, fall));
                    return s;
                };
            }

            // A pitch line that sinks slowly and cracks upward `count` times: a sob, a yodel of the dead.
            internal static Func<float, float> Breaks(SynthGen g, int count, float jump, float fall)
            {
                var at = new float[count];
                var w = new float[count];
                for (int i = 0; i < count; i++)
                {
                    at[i] = g.Range(0.1f + 0.75f * i / count, 0.1f + 0.75f * (i + 1) / count - 0.08f);
                    w[i] = g.Range(0.05f, 0.1f);
                }
                return u =>
                {
                    float p = 1f - fall * u;
                    for (int i = 0; i < at.Length; i++) p += jump * Gate(u, at[i], at[i] + w[i], 0.012f, 0.03f);
                    return p;
                };
            }
            #endregion

            #region Breath, liquid and teeth
            // Bubbles of fluid at the glottis, to feed a throat (Throat.Excite) so the tract colours them.
            internal static float[] Bubbles(SynthGen g, float seconds, float perSecond, float loHz, float hiHz, float level, Func<float, float> density)
            {
                int rate = g.Rate, n = g.N(seconds);
                var x = new float[n];
                int count = (int)(seconds * perSecond);
                for (int k = 0; k < count; k++)
                {
                    float u = g.Next();
                    float d = density != null ? Dsp.Clamp01(density(u)) : 1f;
                    float hz = loHz * MathF.Pow(hiHz / loHz, g.Next());
                    float life = g.Range(0.008f, 0.03f), rise = g.Range(0.4f, 1.6f), gain = level * g.Range(0.25f, 1f);
                    if (g.Next() > d) continue;
                    Dsp.Mix(x, Dsp.Bubble(rate, hz, life, rise), (int)(u * n), gain);
                }
                return x;
            }

            // Wet clicks of saliva and lips (made outside the tract).
            internal static void Saliva(float[] x, SynthGen g, float t0, float t1, int count, float gain)
            {
                for (int i = 0; i < count; i++)
                    Dsp.AddBurstBP(x, g, g.Range(t0, t1), gain * g.Range(0.3f, 1f), 0.0002f, g.Range(0.0006f, 0.002f), g.Range(1800f, 5500f), g.Range(1.5f, 4f));
            }

            // Teeth meeting: a hard click, a very short bright ring, the knock of the jaw.
            internal static void Snap(float[] x, SynthGen g, float at, float gain)
            {
                int rate = g.Rate;
                Dsp.AddBurst(x, g, at, gain * 0.8f, 0.0002f, 0.0009f, 0f, 1800f);
                Dsp.Modes(x, rate, at, gain * 0.25f, new[] { g.Vary(2300f, 0.1f), g.Vary(3700f, 0.1f), g.Vary(5400f, 0.1f) },
                    new[] { 1f, 0.6f, 0.35f }, new[] { 0.003f, 0.0022f, 0.0015f });
                Dsp.Thump(x, rate, at, gain * 0.5f, g.Vary(380f, 0.1f), 240f, 0.008f, 0.0004f, 0.012f);
            }

            // A breath in or out through `vowel`, with the throat's rasp, wet and rattle and (voice above 0) a creak on it.
            internal static float[] Breath(SynthGen g, float seconds, bool inhale, float tract, float rasp, float wet, float rattle, float voice, Vowel vowel)
            {
                float att = inhale ? 0.4f : 0.1f, rel = inhale ? 0.3f : 0.5f;
                return Voice(g, seconds, new Throat
                {
                    F0 = u => inhale ? 58f : 48f, Fry = u => 1f, FryHz = inhale ? 62f : 46f, Voicing = u => voice,
                    BreathLevel = 1f, Hiss = inhale ? 0.6f : 0.35f, Tract = tract * (inhale ? 1.12f : 1f), Widen = 1.6f,
                    Shape = u => vowel, Rasp = rasp, Wet = wet, Rattle = rattle, RattleHz = inhale ? 22f : 15f,
                    Amp = u => (u < att ? Dsp.SmoothStep(u / att) : 1f) * (u > 1f - rel ? 1f - Dsp.SmoothStep((u - 1f + rel) / rel) : 1f),
                });
            }

            // A fricative made at the teeth or lips (s, sh, f): band noise swelling and falling.
            internal static void Fricative(float[] x, SynthGen g, float at, float seconds, float hz, float q, float gain)
            {
                int rate = g.Rate, n = g.N(seconds);
                float[] f = Dsp.White(g, n, 1f);
                Dsp.BP(f, Math.Min(hz, rate * 0.4f), q, rate);
                Dsp.Norm(f);
                for (int i = 0; i < n; i++) f[i] *= Dsp.Bell(i / (float)n, 1f, 0.6f);
                Dsp.Mix(x, f, (int)(at * rate), gain);
            }
            #endregion

            #region Finishing
            // `a` then `b`, each brought to the same loudness and scaled, overlapping by `overlap` seconds (a gap when negative).
            internal static float[] Join(float[] a, float gainA, float[] b, float gainB, int rate, float overlap)
            {
                int ov = Math.Min((int)(overlap * rate), Math.Min(a.Length, b.Length));
                int at = a.Length - ov;
                var y = new float[at + b.Length];
                float ka = gainA / Math.Max(1e-9f, Loudness(a, rate)), kb = gainB / Math.Max(1e-9f, Loudness(b, rate));
                for (int i = 0; i < a.Length; i++) y[i] += a[i] * ka;
                for (int i = 0; i < b.Length; i++) y[at + i] += b[i] * kb;
                return y;
            }

            // How loud a sound is to the ear rather than its loudest click: the RMS of its loudest tenth of a second.
            internal static float Loudness(float[] x, int rate)
            {
                int w = Math.Max(1, Math.Min(x.Length, rate / 10));
                double s = 0, best = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    s += x[i] * x[i];
                    if (i >= w) s -= x[i - w] * x[i - w];
                    if (s > best) best = s;
                }
                return (float)Math.Sqrt(best / w);
            }

            // A copy with `seconds` of silence after it (room for a tail).
            internal static float[] Pad(float[] x, int rate, float seconds)
            {
                var y = new float[x.Length + (int)(seconds * rate)];
                Array.Copy(x, y, x.Length);
                return y;
            }

            // Eases the last `seconds` down to silence.
            internal static void Tail(float[] x, int rate, float seconds)
            {
                int n = Math.Min(x.Length, (int)(seconds * rate));
                for (int i = 0; i < n; i++) x[x.Length - 1 - i] *= Dsp.SmoothStep(i / (float)Math.Max(1, n - 1));
            }

            // Saturation that leaves quiet parts clean: unit peak first, then tanh.
            internal static void Grit(float[] x, float drive)
            {
                Dsp.Norm(x);
                Dsp.Saturate(x, drive);
            }

            // Rumble out, the harsh band tamed, the level evened (a voice that carries), back to unit peak.
            internal static float[] Polish(float[] x, int rate, float hp = 45f, float harsh = -2f, float ratio = 3f, float threshold = -14f)
            {
                Dsp.HP(x, hp, rate);
                if (harsh != 0f) Dsp.PeakEq(x, 3300f, 0.9f, harsh, rate);
                Dsp.Norm(x);
                if (ratio > 1f)
                {
                    Dsp.Compress(x, rate, threshold, ratio, 3f, 120f);
                    Dsp.Norm(x);
                }
                return x;
            }

            // Evens out loudness across variations: the loudest tenth of a second set to `loud`, peaks above it rounded off
            // (a wet click or a bubble no longer decides how loud the whole groan plays).
            internal static float[] Level(float[] x, int rate, float loud = 0.25f)
            {
                float l = Loudness(x, rate);
                if (l > 1e-9f) Dsp.Gain(x, loud / l);
                for (int i = 0; i < x.Length; i++) x[i] = MathF.Tanh(x[i]);
                return x;
            }

            // Space for the far-carrying calls: a few scattered early reflections, then a soft room.
            internal static void Space(float[] x, SynthGen g, float size, float damp, float wet)
            {
                int rate = g.Rate;
                var dry = (float[])x.Clone();
                Dsp.LP1(dry, 5000f, rate);
                for (int i = 0; i < 5; i++)
                {
                    int d = (int)(g.Range(0.011f, 0.07f) * rate);
                    float k = g.Range(0.06f, 0.16f) * (1f - 0.12f * i);
                    for (int s = 0; s + d < x.Length; s++) x[s + d] += dry[s] * k;
                }
                Dsp.Reverb(x, rate, size, damp, wet);
            }
            #endregion

            #region The dead
            // One of the dead: the size of its throat and how far gone it is.
            private struct Dead
            {
                public float Tract, Hz, Fry, Sub, Rasp, Wet, Rattle, Breath, Widen, Jitter, Shimmer, Tilt;
            }

            private static Dead Make(float tract, float hz, float fry, float sub, float rasp, float wet, float rattle, float breath, float widen, float jitter, float shimmer, float tilt)
                => new Dead { Tract = tract, Hz = hz, Fry = fry, Sub = sub, Rasp = rasp, Wet = wet, Rattle = rattle, Breath = breath, Widen = widen, Jitter = jitter, Shimmer = shimmer, Tilt = tilt };

            private static Dead Person(SynthGen g, int kind, float spread = 1f)
            {
                Dead d;
                switch (kind)
                {
                    case 0: d = Make(0.84f, 82f, 0.25f, 0.35f, 0.30f, 0.30f, 0.00f, 0.22f, 1.4f, 0.030f, 0.15f, 2400f); break;   // a big man
                    case 1: d = Make(0.90f, 96f, 0.15f, 0.20f, 0.15f, 0.85f, 0.15f, 0.25f, 1.7f, 0.030f, 0.20f, 2000f); break;   // bloated, waterlogged
                    case 2: d = Make(1.00f, 74f, 0.65f, 0.10f, 0.40f, 0.05f, 0.00f, 0.45f, 1.3f, 0.050f, 0.30f, 3000f); break;   // a dry husk, all creak
                    case 3: d = Make(1.17f, 172f, 0.15f, 0.25f, 0.30f, 0.35f, 0.00f, 0.32f, 1.4f, 0.030f, 0.18f, 3200f); break;  // a woman
                    case 4: d = Make(0.95f, 86f, 0.35f, 0.20f, 0.20f, 0.40f, 0.65f, 0.40f, 1.5f, 0.040f, 0.20f, 2400f); break;   // a rattle in the chest
                    case 5: d = Make(0.92f, 112f, 0.10f, 0.60f, 0.55f, 0.20f, 0.00f, 0.30f, 1.4f, 0.040f, 0.25f, 3400f); break;  // a snarler
                    case 6: d = Make(1.02f, 128f, 0.30f, 0.15f, 0.45f, 0.30f, 0.10f, 0.55f, 1.5f, 0.080f, 0.30f, 2800f); break;  // moans on the in-breath
                    case 7: d = Make(0.88f, 72f, 0.40f, 0.30f, 0.20f, 0.95f, 0.35f, 0.30f, 1.8f, 0.040f, 0.25f, 1900f); break;   // drowning in it
                    case 8: d = Make(1.05f, 102f, 0.25f, 0.20f, 0.25f, 0.25f, 0.10f, 0.35f, 1.4f, 0.030f, 0.18f, 2600f); break;  // an old man
                    case 9: d = Make(0.78f, 60f, 0.20f, 0.70f, 0.45f, 0.25f, 0.00f, 0.20f, 1.5f, 0.040f, 0.25f, 2600f); break;   // a deep growler
                    default: d = Make(1.25f, 205f, 0.10f, 0.15f, 0.20f, 0.30f, 0.00f, 0.40f, 1.3f, 0.030f, 0.15f, 3400f); break; // small: a child, once
                }
                d.Tract *= 1f + g.Bipolar() * 0.04f * spread;
                d.Hz *= 1f + g.Bipolar() * 0.07f * spread;
                d.Rasp = d.Rasp * (1f + g.Bipolar() * 0.15f * spread) + 0.15f;   // the dead are rougher than any living throat
                d.Wet = Dsp.Clamp01(d.Wet * (1f + g.Bipolar() * 0.15f * spread));
                d.Jitter *= 1.6f;
                d.Shimmer *= 1.5f;
                d.Tilt *= 1.3f;
                return d;
            }

            // The five throats every zombie vocal is voiced by (see ZombieThroats): a big man, a waterlogged one, a dry
            // husk, a woman, a deep growler. Within a throat the lines barely differ in size, so one zombie keeps its voice.
            private static readonly int[] s_ThroatKinds = { 0, 1, 2, 3, 9 };

            private static Dead OfThroat(SynthGen g, int throat) => Person(g, s_ThroatKinds[throat % s_ThroatKinds.Length], 0.35f);

            // A moan from one of the dead: its throat, a pitch line (multiples of its voice), vowels and loudness. Unless
            // told otherwise the creak gathers as the breath runs out.
            private static float[] Moan(SynthGen g, Dead d, float seconds, Func<float, float> pitch, Func<float, Vowel> vowels, Func<float, float> amp,
                Func<float, float> fry = null, Func<float, float> voicing = null, float[] excite = null)
            {
                float hz = d.Hz, fryBase = d.Fry;
                return Voice(g, seconds, new Throat
                {
                    // Effort pushes the pitch up as the moan swells.
                    F0 = u => hz * pitch(u) * (0.88f + 0.24f * Math.Min(1f, amp(u))), Amp = amp, Shape = vowels, Voicing = voicing, Excite = excite,
                    Fry = fry ?? (u => fryBase * (0.35f + 0.65f * u * u)), FryHz = Math.Min(55f, hz * 0.5f),
                    BreathLevel = d.Breath * 0.6f, Hiss = 0.35f, Tract = d.Tract, Widen = d.Widen,
                    Jitter = d.Jitter, Shimmer = d.Shimmer, Drift = 0.04f, DriftHz = 4f,
                    Sub = d.Sub, Rasp = d.Rasp, Wet = d.Wet, Rattle = d.Rattle, RattleHz = g.Range(14f, 22f), Tilt = d.Tilt,
                });
            }

            // Groans: each of the five throats (ZombieThroats) with three lines in different manners: a long rise and fall,
            // a waterlogged waver, a dry falling creak, a sob that cracks upward, a rattling chest, a moan climbing into a
            // snarl, a moan on the in-breath, one drowning in it, a two-part moan with a catch of breath, a deep growl.
            private static readonly int[,] s_GroanManners = { { 0, 4, 8 }, { 1, 7, 6 }, { 2, 5, 4 }, { 3, 6, 0 }, { 9, 5, 8 } };

            internal static float[] ZombieGroan(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count, line = g.Variation / ZombieThroats.Count % ZombieThroats.Groans;
                Dead d = OfThroat(g, throat);
                int k = s_GroanManners[throat, line];
                if (k == 4) d.Rattle = Math.Max(d.Rattle, 0.6f);
                if (k == 5) { d.Sub = Math.Max(d.Sub, 0.55f); d.Rasp += 0.15f; }
                if (k == 6) { d.Jitter *= 1.6f; d.Breath = Math.Max(d.Breath, 0.5f); }
                if (k == 7) { d.Wet = Math.Max(d.Wet, 0.9f); d.Rattle = Math.Max(d.Rattle, 0.3f); }
                float len = g.Range(1.45f, 2f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 2, 5, 3);
                Func<float, float> amp = Swells(g, len, g.Range(0.05f, 0.09f), g.Range(0.3f, 0.55f), 0.35f);
                Func<float, float> pitch;
                Func<float, Vowel> vowels;
                float[] excite = null;
                float inhale = 0f;
                int spit = 0;
                switch (k)
                {
                    case 0:
                    {
                        float pk = g.Range(0.25f, 0.4f);
                        pitch = u => 0.86f + 0.34f * Dsp.Bell(u, 1f, pk) - 0.2f * u + 0.08f * (wob(u) - 0.5f);
                        vowels = Path(new[] { Mm, Vowel.Uh, Vowel.Aw, Vowel.O, Vowel.U }, new[] { 0f, 0.1f, 0.4f, 0.75f, 1f });
                        spit = 4;
                        break;
                    }
                    case 1:
                    {
                        float c = g.Range(1.5f, 2.6f), ph = g.Range(0f, 6.28f);
                        pitch = u => 1f + 0.12f * MathF.Sin(Dsp.TwoPi * c * u + ph) - 0.16f * u;
                        vowels = Path(new[] { Vowel.O, Vowel.Uh, Vowel.U, Mm }, new[] { 0f, 0.35f, 0.75f, 1f });
                        excite = Bubbles(g, len, 28f, 140f, 520f, 0.8f, u => 0.3f + 0.7f * wob(u));
                        break;
                    }
                    case 2:
                        pitch = u => Dsp.Glide(1.25f, 0.72f, u) * (1f + 0.05f * (wob(u) - 0.5f));
                        vowels = Path(new[] { Vowel.A, Vowel.Ae, Vowel.Er, Vowel.Er }, new[] { 0f, 0.3f, 0.7f, 1f });
                        inhale = g.Range(0.22f, 0.32f);
                        break;
                    case 3:
                        pitch = Breaks(g, 3, 0.3f, 0.16f);
                        vowels = Path(new[] { Vowel.E, Vowel.A, Vowel.Uh, Vowel.U }, new[] { 0f, 0.3f, 0.7f, 1f });
                        break;
                    case 4:
                    {
                        float c = g.Range(1.2f, 2f), ph = g.Range(0f, 6.28f);
                        pitch = u => 1f + 0.08f * MathF.Sin(Dsp.TwoPi * c * u + ph) - 0.12f * u;
                        vowels = Path(new[] { Vowel.Uh, Vowel.O, Vowel.O, Vowel.U }, new[] { 0f, 0.3f, 0.7f, 1f });
                        break;
                    }
                    case 5:
                        pitch = u => Dsp.Glide(0.82f, 1.25f, Dsp.SmoothStep(u * 1.25f)) * (1f - 0.35f * Span(u, 0.8f, 1f));
                        vowels = Path(new[] { Vowel.Uh, Vowel.A, Vowel.Ae, Vowel.Aw }, new[] { 0f, 0.35f, 0.7f, 1f });
                        break;
                    case 6:
                        pitch = u => Dsp.Glide(0.85f, 1.2f, u) + 0.12f * (wob(u) - 0.5f);
                        vowels = Path(new[] { Vowel.U, Vowel.O, Vowel.A }, new[] { 0f, 0.5f, 1f });
                        inhale = g.Range(0.12f, 0.2f);
                        break;
                    case 7:
                    {
                        float pk = g.Range(0.3f, 0.5f);
                        pitch = u => 0.9f + 0.25f * Dsp.Bell(u, 1f, pk) - 0.15f * u;
                        vowels = Path(new[] { Vowel.O, Vowel.Uh, Vowel.U, Vowel.Uh }, new[] { 0f, 0.4f, 0.75f, 1f });
                        excite = Bubbles(g, len, 45f, 110f, 420f, 1f, u => 0.5f + 0.5f * wob(u));
                        spit = 6;
                        break;
                    }
                    case 8:
                    {
                        float split = g.Range(0.42f, 0.52f);
                        pitch = u => u < split ? 1f + 0.12f * Dsp.Bell(u, split, 0.4f) - 0.1f * u / split : Dsp.Glide(0.97f, 0.7f, Span(u, split, 1f));
                        Func<float, float> whole = amp;
                        amp = u => whole(u) * (1f - 0.92f * Gate(u, split - 0.05f, split + 0.06f, 0.03f, 0.03f));
                        vowels = Path(new[] { Vowel.O, Vowel.A, Vowel.Uh, Vowel.Aw, Vowel.U }, new[] { 0f, 0.25f, 0.45f, 0.6f, 1f });
                        break;
                    }
                    default:
                    {
                        float pk = g.Range(0.3f, 0.45f);
                        pitch = u => 0.88f + 0.28f * Dsp.Bell(u, 1f, pk) - 0.18f * u + 0.06f * (wob(u) - 0.5f);
                        vowels = Path(new[] { Vowel.Aw, Vowel.A, Vowel.Aw, Vowel.U }, new[] { 0f, 0.3f, 0.65f, 1f });
                        break;
                    }
                }
                float[] x = Moan(g, d, len, pitch, vowels, amp, excite: excite);
                if (inhale > 0f) x = Join(Breath(g, inhale, true, d.Tract, d.Rasp + 0.3f, d.Wet, d.Rattle, 0.25f, Vowel.Uh), 0.4f, x, 1f, rate, 0.04f);
                Dsp.Norm(x);
                if (spit > 0) Saliva(x, g, 0.05f, x.Length / (float)rate - 0.25f, spit, 0.12f);
                Grit(x, 2f);
                Dsp.PeakEq(x, 1800f, 0.8f, 2f, rate);
                return Level(Polish(x, rate, 45f, 0f, 3f, -14f), rate);
            }

            // The idle dead, in their own throats: a wet breath, a creaking hum, a murmur that is almost words, sniffing at
            // the air, a sigh, a throat to clear.
            private static readonly int[,] s_IdleManners = { { 3, 1 }, { 5, 0 }, { 1, 4 }, { 2, 4 }, { 0, 3 } };

            internal static float[] ZombieIdle(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count, line = g.Variation / ZombieThroats.Count % ZombieThroats.Idles;
                Dead d = OfThroat(g, throat);
                int k = s_IdleManners[throat, line];
                float[] x;
                switch (k)
                {
                    case 0:
                        x = Join(Breath(g, g.Range(1f, 1.3f), false, d.Tract, 0.45f, 0.6f, 0.55f, 0.18f, Vowel.Uh), 1f,
                            Breath(g, g.Range(0.55f, 0.75f), true, d.Tract, 0.35f, 0.4f, 0.3f, 0.1f, Vowel.Ae), 0.6f, rate, 0.05f);
                        break;
                    case 1:
                    {
                        float len = g.Range(1.4f, 1.8f);
                        x = Moan(g, d, len, u => 0.9f - 0.12f * u, Path(new[] { Mm, Mm, Vowel.Uh, Vowel.O, Mm }, new[] { 0f, 0.2f, 0.45f, 0.7f, 1f }),
                            Swells(g, len, 0.06f, 0.5f, 0.3f), fry: u => 0.85f - 0.45f * Dsp.Bell(u, 1f, 0.5f));
                        break;
                    }
                    case 2:
                    {
                        float len = g.Range(1.5f, 1.9f);
                        int m = g.Int(6, 10);
                        var vs = new Vowel[m + 1];
                        var at = new float[m + 1];
                        for (int i = 0; i <= m; i++)
                        {
                            vs[i] = Vowel.Speech[g.Int(0, Vowel.Speech.Length)];
                            at[i] = i / (float)m;
                        }
                        Func<float, float> env = Swells(g, len, 0.05f, 0.4f, 0.25f);
                        float syl = g.Range(3f, 4.5f) * len, sph = g.Range(0f, 6.28f);
                        x = Moan(g, d, len, u => 1.05f - 0.25f * u, Path(vs, at),
                            u => env(u) * (0.45f + 0.55f * MathF.Pow(0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * syl * u + sph), 2f)), voicing: u => 0.75f);
                        break;
                    }
                    case 3:
                    {
                        float[] s1 = Breath(g, g.Range(0.08f, 0.12f), true, d.Tract * 1.2f, 0.2f, 0.3f, 0f, 0f, Nn);
                        float[] s2 = Breath(g, g.Range(0.08f, 0.12f), true, d.Tract * 1.2f, 0.2f, 0.3f, 0f, 0f, Nn);
                        float[] s3 = Breath(g, g.Range(0.1f, 0.16f), true, d.Tract * 1.2f, 0.2f, 0.3f, 0f, 0f, Nn);
                        float glen = g.Range(0.35f, 0.5f);
                        float[] grunt = Moan(g, d, glen, u => 1.1f - 0.3f * u, Path(new[] { Vowel.Uh, Vowel.U }, new[] { 0f, 1f }), Swells(g, glen, 0.03f, 0.2f, 0.1f));
                        x = Join(s1, 0.8f, s2, 0.9f, rate, -g.Range(0.08f, 0.16f));
                        x = Join(x, 1f, s3, 1f, rate, -g.Range(0.1f, 0.18f));
                        x = Join(x, 0.75f, grunt, 1f, rate, -g.Range(0.15f, 0.3f));
                        break;
                    }
                    case 4:
                    {
                        float len = g.Range(1.5f, 1.9f);
                        x = Moan(g, d, len, u => Dsp.Glide(1.1f, 0.72f, u), Path(new[] { Vowel.Ae, Vowel.Uh, Vowel.O, Vowel.U }, new[] { 0f, 0.3f, 0.65f, 1f }),
                            Swells(g, len, 0.05f, 0.6f, 0.2f), voicing: u => 0.15f + 0.5f * Dsp.Bell(u, 1f, 0.45f));
                        break;
                    }
                    default:
                    {
                        float len = g.Range(1.2f, 1.6f);
                        d.Wet = Math.Max(d.Wet, 0.75f);
                        d.Rattle = Math.Max(d.Rattle, 0.4f);
                        float[] bub = Bubbles(g, len, 35f, 120f, 480f, 0.9f, u => Dsp.Bell(u, 1f, 0.4f));
                        x = Moan(g, d, len, u => 0.8f - 0.1f * u, Path(new[] { Vowel.Uh, Vowel.O, Vowel.U }, new[] { 0f, 0.5f, 1f }),
                            Swells(g, len, 0.05f, 0.4f, 0.4f), voicing: u => 0.55f, excite: bub);
                        break;
                    }
                }
                Grit(x, 1.3f);
                return Level(Polish(x, rate, 50f, -2f, 2.5f, -16f), rate, 0.22f);
            }

            // A Crawler, in its own throat: gargling as it strains, choking, hauling itself along, breath bubbling through
            // blood, a wet growl.
            private static readonly int[,] s_CrawlManners = { { 2, 1 }, { 0, 3 }, { 1, 2 }, { 3, 0 }, { 4, 2 } };

            internal static float[] ZombieCrawl(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count, line = g.Variation / ZombieThroats.Count % ZombieThroats.Crawls;
                Dead d = OfThroat(g, throat);
                int k = s_CrawlManners[throat, line];
                d.Wet = Math.Max(d.Wet, 0.7f);
                float[] x;
                switch (k)
                {
                    case 0:
                    {
                        float len = g.Range(1.5f, 1.9f);
                        float[] bub = Bubbles(g, len, 50f, 100f, 400f, 1.1f, u => 0.4f + 0.6f * Dsp.Bell(u, 1f, 0.6f));
                        x = Moan(g, d, len, u => 0.92f + 0.18f * Dsp.Bell(u, 1f, 0.35f) - 0.15f * u, Path(new[] { Vowel.O, Vowel.Uh, Vowel.U, Vowel.Uh }, new[] { 0f, 0.4f, 0.75f, 1f }),
                            Swells(g, len, 0.05f, 0.45f, 0.45f, 2, 5), excite: bub);
                        break;
                    }
                    case 1:
                    {
                        float len = g.Range(1.3f, 1.6f);
                        int c = g.Int(3, 6);
                        var at = new float[c];
                        for (int i = 0; i < c; i++) at[i] = g.Range(0.1f + 0.8f * i / c, 0.1f + 0.8f * (i + 1) / c - 0.06f);
                        Func<float, float> env = Swells(g, len, 0.04f, 0.3f, 0.3f);
                        x = Moan(g, d, len, u => 1.15f - 0.3f * u, Path(new[] { Vowel.Uh, Vowel.Er, Vowel.Uh, Vowel.U }, new[] { 0f, 0.3f, 0.6f, 1f }),
                            u =>
                            {
                                float e = env(u);
                                for (int j = 0; j < at.Length; j++) e *= 1f - 0.95f * Gate(u, at[j], at[j] + 0.045f, 0.004f, 0.012f);
                                return e;
                            });
                        break;
                    }
                    case 2:
                    {
                        int c = g.Int(2, 4);
                        float len = 0.45f * c + g.Range(0.2f, 0.4f);
                        var a = new float[c];
                        var b = new float[c];
                        var lv = new float[c];
                        for (int i = 0; i < c; i++)
                        {
                            a[i] = (0.02f + 0.45f * i) / len;
                            b[i] = a[i] + g.Range(0.22f, 0.32f) / len;
                            lv[i] = g.Range(0.7f, 1f);
                        }
                        Func<float, float> grunts = Pulses(a, b, lv, 0.02f / len, 0.08f / len);
                        x = Moan(g, d, len,
                            u =>
                            {
                                for (int j = 0; j < a.Length; j++) if (u >= a[j] && u <= b[j]) return 1.25f - 0.35f * Span(u, a[j], b[j]);
                                return 0.95f;
                            },
                            Path(new[] { Vowel.Uh, Vowel.Er, Vowel.Uh }, new[] { 0f, 0.5f, 1f }),
                            u => Math.Max(grunts(u), 0.22f * Gate(u, 0f, 1f, 0.03f, 0.15f)),
                            voicing: u => 0.1f + 0.9f * grunts(u));
                        break;
                    }
                    case 3:
                    {
                        float len = g.Range(1.3f, 1.6f);
                        float[] bub = Bubbles(g, len, 70f, 150f, 650f, 1.3f, u => 1f - 0.6f * u);
                        x = Moan(g, d, len, u => 0.85f - 0.1f * u, Path(new[] { Vowel.Uh, Vowel.Uh, Vowel.O, Vowel.U }, new[] { 0f, 0.45f, 0.75f, 1f }),
                            Swells(g, len, 0.04f, 0.35f, 0.25f), voicing: u => 0.8f * Span(u, 0.35f, 0.65f), excite: bub);
                        break;
                    }
                    default:
                    {
                        float len = g.Range(1.5f, 1.9f);
                        d.Rattle = Math.Max(d.Rattle, 0.55f);
                        d.Sub = Math.Max(d.Sub, 0.5f);
                        x = Moan(g, d, len, u => 0.78f + 0.1f * Dsp.Bell(u, 1f, 0.4f), Path(new[] { Vowel.Uh, Vowel.O, Vowel.U }, new[] { 0f, 0.5f, 1f }),
                            Swells(g, len, 0.05f, 0.4f, 0.35f));
                        break;
                    }
                }
                Dsp.Norm(x);
                Saliva(x, g, 0.05f, x.Length / (float)rate - 0.2f, g.Int(3, 7), 0.1f);
                Grit(x, 1.5f);
                return Level(Polish(x, rate, 45f, -2f, 3f, -15f), rate, 0.22f);
            }

            // It has seen you: a sharp breath in through the teeth, then a ragged snarl climbing as it fixes on you.
            internal static float[] ZombieAlert(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count;
                Dead d = OfThroat(g, throat);
                float hz = Math.Max(95f, d.Hz * g.Range(1.15f, 1.4f));
                float len = g.Range(0.8f, 1.1f), pk = g.Range(0.3f, 0.45f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 4, 9, 3);
                Func<float, float> amp = Swells(g, len, 0.025f, 0.28f, 0.2f);
                if (throat == 4)
                {
                    float cut = g.Range(0.35f, 0.45f);
                    Func<float, float> whole = amp;
                    amp = u => whole(u) * (1f - 0.9f * Gate(u, cut, cut + 0.08f, 0.02f, 0.02f));
                }
                float[] snarl = Voice(g, len, new Throat
                {
                    F0 = u => hz * (0.78f + 0.5f * Dsp.SmoothStep(u / pk) - 0.32f * Span(u, 0.7f, 1f)) * (1f + 0.06f * (wob(u) - 0.5f)),
                    Amp = amp,
                    Shape = Path(new[] { Vowel.Uh, Vowel.A, Vowel.Ae, Vowel.A, Vowel.Aw }, new[] { 0f, 0.15f, 0.45f, 0.75f, 1f }),
                    Tract = d.Tract, Widen = 1.3f, Jitter = 0.05f, Shimmer = 0.28f, Drift = 0.03f, DriftHz = 5f,
                    Sub = 0.45f + 0.4f * d.Sub, SubRatio = throat == 2 ? 3 : 2, Rasp = 0.75f, BreathLevel = 0.35f, Hiss = 0.45f,
                    Wet = d.Wet * 0.5f, Tilt = 3800f,
                });
                float[] gasp = Breath(g, g.Range(0.1f, 0.16f), true, d.Tract, 0.5f, d.Wet * 0.5f, 0f, 0.2f, Vowel.Ae);
                float[] x = Join(gasp, 0.45f, snarl, 1f, rate, 0.03f);
                x = Pad(x, rate, 0.06f);
                Grit(x, 2.4f);
                Dsp.PeakEq(x, 1600f, 0.8f, 2.5f, rate);
                return Level(Polish(x, rate, 60f, -2f, 3f, -14f), rate, 0.3f);
            }

            // A Sprinter running you down: a high, torn, distorted scream out of its own throat (a man's long one, a
            // gurgled one, one in two breaths, a woman's, a low one).
            internal static float[] ZombieChase(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count;
                Dead d = OfThroat(g, throat);
                float len = throat == 0 ? g.Range(1.2f, 1.45f) : g.Range(0.95f, 1.2f);
                float hz = Math.Max(270f, Math.Min(520f, d.Hz * (throat == 4 ? 4.8f : throat == 3 ? 2.8f : 4.2f))) * g.Range(0.95f, 1.05f);
                float pk = g.Range(0.12f, 0.22f), top = g.Range(1.25f, 1.4f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 5, 12, 3);
                Func<float, float> amp = Swells(g, len, 0.03f, 0.3f, 0.15f);
                if (throat == 2)
                {
                    float cut = g.Range(0.38f, 0.48f);
                    Func<float, float> whole = amp;
                    amp = u => whole(u) * (1f - 0.95f * Gate(u, cut, cut + 0.09f, 0.02f, 0.02f));
                }
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * (0.7f + (top - 0.7f) * Dsp.SmoothStep(u / pk) - 0.35f * Span(u, 0.75f, 1f)) * (1f + 0.08f * (wob(u) - 0.5f)),
                    Amp = amp,
                    Shape = Path(new[] { Vowel.Uh, Vowel.A, Vowel.Ae, Vowel.A, Vowel.Aw }, new[] { 0f, 0.1f, 0.4f, 0.75f, 1f }),
                    Tract = d.Tract * 1.08f, Widen = 1.2f, Jitter = 0.03f, Shimmer = 0.25f, Drift = 0.04f, DriftHz = 6f,
                    Sub = 0.25f + 0.3f * d.Sub, SubRatio = throat == 2 ? 3 : 2, Rasp = 0.85f, BreathLevel = 0.5f, Hiss = 0.5f,
                    Wet = d.Wet * 0.45f, Tilt = 7000f,
                });
                x = Pad(x, rate, 0.06f);
                Grit(x, 3.2f);
                Dsp.PeakEq(x, 2400f, 0.8f, 3f, rate);
                return Level(Polish(x, rate, 150f, -1f, 3f, -12f), rate, 0.32f);
            }

            // The lunge: a gasp, a forced snarl thrown at you, the teeth snapping shut, a wet grunt through them.
            internal static float[] ZombieAttack(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count, k = throat;
                Dead d = OfThroat(g, throat);
                float hz = Math.Max(110f, d.Hz * g.Range(1.3f, 1.55f));
                float len = g.Range(0.45f, 0.6f);
                float[] lunge = Voice(g, len, new Throat
                {
                    F0 = u => hz * (0.85f + 0.4f * Dsp.SmoothStep(u * 2.5f) - 0.3f * Span(u, 0.6f, 1f)),
                    Amp = u => (u < 0.06f ? Dsp.SmoothStep(u / 0.06f) : 1f) * (1f - 0.85f * Dsp.SmoothStep(Span(u, 0.55f, 1f))),
                    Shape = Path(new[] { Vowel.Uh, Vowel.A, Vowel.Ae, Vowel.A }, new[] { 0f, 0.2f, 0.6f, 1f }),
                    Tract = d.Tract, Widen = 1.3f, Jitter = 0.05f, Shimmer = 0.3f, Drift = 0.03f, DriftHz = 6f,
                    Sub = 0.5f + 0.3f * d.Sub, Rasp = 0.8f, BreathLevel = 0.35f, Hiss = 0.45f, Wet = d.Wet * 0.6f, Tilt = 4200f,
                });
                var x = new float[g.N(len + g.Range(0.32f, 0.42f))];
                float[] gasp = Breath(g, 0.09f, true, d.Tract, 0.5f, 0.2f, 0f, 0.15f, Vowel.Ae);
                Dsp.Norm(gasp);
                Dsp.Norm(lunge);
                Dsp.Mix(x, gasp, 0, 0.35f);
                Dsp.Mix(x, lunge, g.N(0.05f), 1f);
                float bite = 0.05f + len * g.Range(0.82f, 0.95f);
                Snap(x, g, bite, 0.9f);
                if (k % 2 == 1) Snap(x, g, bite + g.Range(0.07f, 0.11f), 0.6f);
                float[] after = Breath(g, g.Range(0.2f, 0.28f), false, d.Tract, 0.6f, d.Wet, 0f, 0.3f, Vowel.Uh);
                Dsp.Norm(after);
                Dsp.Mix(x, after, g.N(bite + 0.03f), 0.2f);
                Grit(x, 2.4f);
                return Level(Polish(x, rate, 60f, -1.5f, 3f, -14f), rate, 0.28f);
            }

            // A bullet or a shove taken: a short strained grunt in its own throat ("urgh", a wet cough, a snarled yelp,
            // "ngh", a low grunt).
            internal static float[] ZombieHurt(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count;
                Dead d = OfThroat(g, throat);
                float hz = Math.Max(100f, d.Hz * g.Range(1.3f, 1.6f));
                float len = g.Range(0.28f, 0.45f);
                Vowel v0 = throat == 3 ? Nn : throat == 2 ? Vowel.Ae : Vowel.A, v1 = throat == 3 ? Mm : Vowel.Uh;
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * (1.1f + 0.2f * Dsp.Bell(u, 1f, 0.15f) - 0.4f * u),
                    Amp = u => (u < 0.05f ? Dsp.SmoothStep(u / 0.05f) : 1f) * MathF.Exp(-2.2f * u) * (1f - Dsp.SmoothStep(Span(u, 0.6f, 0.95f))),
                    Shape = u => Vowel.Lerp(v0, v1, Dsp.SmoothStep(u)),
                    Tract = d.Tract, Widen = 1.3f, Jitter = 0.05f, Shimmer = 0.3f,
                    Sub = 0.3f + 0.4f * d.Sub, Rasp = 0.6f + 0.3f * d.Rasp, BreathLevel = 0.4f, Hiss = 0.4f, Wet = d.Wet, Tilt = 3600f,
                });
                if (throat == 1)
                {
                    float[] cough = Breath(g, g.Range(0.2f, 0.26f), false, d.Tract, 0.9f, 0.8f, 0.3f, 0.35f, Vowel.Uh);
                    x = Join(cough, 1f, x, 0.7f, rate, 0.05f);
                }
                x = Pad(x, rate, 0.05f);
                Grit(x, 2f);
                return Level(Polish(x, rate, 70f, -1.5f, 2.5f, -14f), rate, 0.28f);
            }

            // The end, in its own throat: a last cry choked off (the dry one only creaks), a moan sinking into the chest,
            // the death rattle, a last breath out.
            internal static float[] ZombieDie(SynthGen g)
            {
                int rate = g.Rate, throat = g.Variation % ZombieThroats.Count;
                Dead d = OfThroat(g, throat);
                d.Wet = Math.Max(d.Wet, 0.5f);
                float cry = throat == 2 ? 0f : g.Range(0.22f, 0.3f);
                float len = cry + g.Range(0.7f, 0.9f);
                float split = cry / len, hz = d.Hz;
                float[] voice = Voice(g, len, new Throat
                {
                    F0 = u => hz * (u < split ? 1.6f + 0.3f * Dsp.Bell(u, split, 0.4f) : Dsp.Glide(1.35f, 0.62f, Span(u, split, 1f))),
                    Amp = u => (u < 0.03f ? Dsp.SmoothStep(u / 0.03f) : 1f)
                        * (split > 0f ? 1f - 0.75f * Gate(u, split - 0.02f, split + 0.04f, 0.01f, 0.02f) : 1f)
                        * (1f - Dsp.SmoothStep(Span(u, 0.6f, 1f))),
                    Shape = Path(new[] { Vowel.A, Vowel.Aw, Vowel.O, Vowel.U, Mm }, new[] { 0f, split, split + 0.3f * (1f - split), 0.85f, 1f }),
                    Fry = u => Span(u, 0.5f, 1f), FryHz = 40f, Tract = d.Tract, Widen = d.Widen,
                    Jitter = d.Jitter + 0.02f, Shimmer = d.Shimmer + 0.1f, Sub = d.Sub + 0.2f, Rasp = d.Rasp + 0.2f,
                    BreathLevel = d.Breath * 0.6f, Hiss = 0.25f, Wet = d.Wet, Rattle = d.Rattle * 0.5f, Tilt = d.Tilt,
                    Excite = Bubbles(g, len, 30f, 120f, 450f, 0.5f, u => Span(u, 0.3f, 0.9f)),
                });
                float[] rattle = Breath(g, g.Range(0.55f, 0.75f), false, d.Tract, 0.5f, 0.85f, 0.85f, 0.25f, Vowel.Uh);
                float[] last = Breath(g, g.Range(0.35f, 0.5f), false, d.Tract, 0.2f, 0.3f, 0f, 0f, Vowel.Uh);
                float[] x = Join(voice, 1f, rattle, 0.45f, rate, 0.1f);
                x = Join(x, 1f, last, 0.2f, rate, -g.Range(0.05f, 0.2f));
                Grit(x, 1.8f);
                return Level(Polish(x, rate, 45f, -2f, 2.5f, -16f), rate, 0.24f);
            }

            // A body coming down: knees or hands first, the torso's dull blow, head and limbs after, clothes and gear
            // settling. `weight` 1 is a person; more is a bear or a Thumper (lower, longer, heavier).
            internal static float[] BodyFall(SynthGen g, float weight)
            {
                int rate = g.Rate;
                float w = weight, sw = MathF.Sqrt(w), lo = 1f / MathF.Pow(w, 0.45f);
                var x = new float[g.N(0.9f + 0.35f * (w - 1f))];
                float t0 = 0.004f;
                Dsp.Thump(x, rate, t0, 0.4f, g.Vary(140f, 0.1f) * lo, 80f * lo, 0.015f, 0.001f, 0.025f * sw);
                Dsp.AddBurst(x, g, t0, 0.3f, 0.0008f, 0.012f, 2200f * lo, 80f);
                float tt = t0 + g.Range(0.06f, 0.12f) * sw;
                Dsp.Thump(x, rate, tt, 1f, g.Vary(95f, 0.1f) * lo, 50f * lo, 0.02f, 0.002f, 0.045f * sw);
                Dsp.AddBurst(x, g, tt, 0.7f, 0.0015f, 0.025f * sw, 1500f * lo, 150f);   // flesh and cloth slapping down
                Dsp.AddBurst(x, g, tt + 0.002f, 0.6f, 0.002f, 0.05f * sw, 400f * lo, 40f);   // the ground taking the weight
                int knocks = g.Int(1, 4);
                for (int i = 0; i < knocks; i++)
                {
                    float tk = tt + g.Range(0.05f, 0.28f) * sw, gk = g.Range(0.25f, 0.5f);
                    Dsp.Thump(x, rate, tk, gk, g.Range(160f, 260f) * lo, 90f * lo, 0.015f, 0.001f, 0.03f);
                    Dsp.AddBurst(x, g, tk, gk * 0.5f, 0.0005f, 0.008f, 3000f * lo, 200f);
                }
                var cloth = new float[x.Length];
                Dsp.Grains(cloth, g, tt, tt + 0.35f * w, (int)(50 * w), 0.5f, 0.0015f, u => 1f - u);
                Dsp.BP(cloth, 2200f, 0.7f, rate);
                Dsp.Mix(x, cloth, 0, 0.12f);
                float[] slide = Dsp.White(g, g.N(0.25f * sw), 1f);
                Dsp.LP(slide, 900f * lo, rate);
                for (int i = 0; i < slide.Length; i++) slide[i] *= Dsp.Bell(i / (float)slide.Length, 1f, 0.3f);
                Dsp.Mix(x, slide, g.N(tt + g.Range(0.15f, 0.25f) * sw), 0.06f);
                Dsp.HP(x, 28f, rate);
                Dsp.Norm(x);
                Dsp.Compress(x, rate, -12f, 2.5f, 2f, 80f);
                Tail(x, rate, 0.1f);
                return x;
            }
            #endregion

            #region Wildlife
            // A wolf's howl: a smooth glide up, a long held note with a slow waver and the mouth opening into it, then
            // the fall; one breaks upward, one is low and rough, a short call and then the long one. Far-carrying.
            internal static float[] WolfHowl(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                float len = k == 0 ? g.Range(3.6f, 4.1f) : g.Range(4f, 4.5f);
                float hz = k == 1 ? g.Range(430f, 480f) : k == 2 ? g.Range(300f, 340f) : g.Range(360f, 410f);
                float rise = g.Range(0.07f, 0.12f), fallAt = g.Range(0.72f, 0.8f);
                float start = g.Range(0.68f, 0.78f), end = g.Range(0.55f, 0.66f), drift = g.Range(-0.02f, 0.05f);
                float vib = g.Range(3.5f, 5.5f), vibD = g.Range(0.004f, 0.008f), ph = g.Range(0f, 6.28f);
                float brk = k == 1 ? g.Range(0.38f, 0.5f) : -1f;
                float split = k == 2 ? g.Range(0.24f, 0.3f) : -1f;
                Func<float, float> wob = Dsp.Gusts(g, 1f, 2, 4, 3);
                float climb = g.Range(0.03f, 0.08f);
                Func<float, float> pitch = u =>
                {
                    float v = u, r = rise, f = fallAt;
                    if (split > 0f)
                    {
                        if (u < split) { v = u / split; r = 0.3f; f = 0.55f; }
                        else v = Span(u, split, 1f);
                    }
                    // the glide up, a slow climb and wander while it holds, the fall as the breath gives out
                    float p = Dsp.Lerp(start, 1f, Dsp.SmoothStep(v / r)) + drift * v + climb * Dsp.Bell(v, 1f, 0.45f) + 0.05f * (wob(u) - 0.5f);
                    if (v > f) p *= Dsp.Lerp(1f, end, Dsp.SmoothStep((v - f) / (1f - f)));
                    if (brk > 0f) p *= 1f + 0.33f * Gate(u, brk, brk + 0.13f, 0.035f, 0.05f);
                    return p * (1f + vibD * (0.5f + 1.5f * v) * MathF.Sin(Dsp.TwoPi * vib * u * len + ph));
                };
                Func<float, float> amp = u =>
                {
                    float v = u, e = 1f;
                    if (split > 0f)
                    {
                        if (u < split) { v = u / split; e = 0.7f; }
                        else v = Span(u, split, 1f);
                    }
                    return e * (v < 0.04f ? Dsp.SmoothStep(v / 0.04f) : 1f) * (0.85f + 0.15f * Dsp.Bell(v, 1f, 0.45f)) * (1f - Dsp.SmoothStep(Span(v, 0.8f, 1f)));
                };
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * pitch(u), Amp = amp,
                    Shape = u => Vowel.Lerp(Oo, Vowel.Aw, Dsp.SmoothStep(amp(u) * 1.1f - 0.15f)),
                    Tract = g.Range(1.15f, 1.3f), Widen = 1f, Jitter = 0.002f, Shimmer = 0.02f, Drift = 0.004f, DriftHz = 1.5f,
                    BreathLevel = 0.03f, Hiss = 0.1f, Rasp = k == 2 ? 0.08f : 0.02f, Tilt = 1600f,
                });
                x = Pad(x, rate, 1.15f);
                Space(x, g, 0.88f, 0.5f, 0.45f);
                Dsp.HP(x, 140f, rate);
                Tail(x, rate, 0.4f);
                return x;
            }

            // A wolf growling on its breath: out long and low, in short and higher, out again.
            internal static float[] WolfGrowl(SynthGen g)
            {
                int rate = g.Rate;
                float o1 = g.Range(0.55f, 0.8f), i1 = g.Range(0.22f, 0.32f), o2 = g.Range(0.45f, 0.7f);
                float len = o1 + i1 + o2 + 0.12f;
                float a1 = 0.005f / len, b1 = a1 + o1 / len, a2 = b1 + 0.03f / len, b2 = a2 + i1 / len, a3 = b2 + 0.03f / len, b3 = Math.Min(0.98f, a3 + o2 / len);
                Func<float, float> amp = Pulses(new[] { a1, a2, a3 }, new[] { b1, b2, b3 }, new[] { 1f, 0.55f, 0.9f }, 0.03f / len, 0.08f / len);
                float hz = g.Range(88f, 115f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 5, 11, 3);
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * (u >= a2 && u <= b2 ? 1.25f : 1f) * (1f + 0.08f * (wob(u) - 0.5f)),
                    Amp = amp, Shape = u => Vowel.Lerp(Vowel.Er, Vowel.Uh, wob(u)),
                    Tract = g.Range(0.92f, 1.02f), Widen = 1.6f, Jitter = 0.07f, Shimmer = 0.4f, Drift = 0.03f, DriftHz = 6f,
                    Sub = 0.5f, Rasp = 0.7f, BreathLevel = 0.3f, Hiss = 0.35f, Wet = 0.15f, Tilt = 2400f,
                });
                Grit(x, 2f);
                return Polish(x, rate, 50f, -1.5f, 3f, -14f);
            }

            // One bark: a puff of air, a short loud call falling as the jaw closes.
            private static void Bark(float[] x, SynthGen g, float at, float hz, float tract, float rasp, float sub, float wet, float gain)
            {
                float len = g.Range(0.13f, 0.2f);
                float[] b = Voice(g, len, new Throat
                {
                    F0 = u => hz * (1.08f - 0.3f * u),
                    Amp = u => (u < 0.06f ? Dsp.SmoothStep(u / 0.06f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.35f, 1f))),
                    Shape = u => Vowel.Lerp(Vowel.A, Oo, Dsp.SmoothStep(u * 1.2f)),
                    Tract = tract, Widen = 1.3f, Jitter = 0.03f, Shimmer = 0.2f, Rasp = rasp, Sub = sub, Wet = wet,
                    BreathLevel = 0.35f, Hiss = 0.35f, Tilt = 4200f,
                });
                Dsp.Norm(b);
                Dsp.AddBurst(b, g, 0f, 0.35f, 0.0005f, 0.008f, 3500f, 300f);
                Dsp.Mix(x, b, (int)(at * g.Rate), gain);
            }

            // A wolf's bark: one, two, or one that turns into a short howl.
            internal static float[] WolfBark(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                float hz = g.Range(330f, 420f), tract = g.Range(1.05f, 1.2f);
                var x = new float[g.N(k == 2 ? 1.5f : 0.9f)];
                Bark(x, g, 0.005f, hz, tract, 0.45f, 0.2f, 0.05f, 1f);
                if (k == 1) Bark(x, g, g.Range(0.24f, 0.32f), hz * g.Range(0.95f, 1.05f), tract, 0.45f, 0.2f, 0.05f, 0.85f);
                if (k == 2)
                {
                    float[] h = Voice(g, g.Range(0.7f, 0.9f), new Throat
                    {
                        F0 = u => hz * 0.9f * (0.85f + 0.2f * Dsp.SmoothStep(u * 4f) - 0.25f * Span(u, 0.6f, 1f)),
                        Amp = u => (u < 0.08f ? Dsp.SmoothStep(u / 0.08f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.55f, 1f))),
                        Shape = u => Vowel.Lerp(Oo, Vowel.O, Dsp.Bell(u, 1f, 0.4f)),
                        Tract = tract, Jitter = 0.004f, Shimmer = 0.03f, BreathLevel = 0.05f, Rasp = 0.05f, Tilt = 1800f,
                    });
                    Dsp.Norm(h);
                    Dsp.Mix(x, h, g.N(0.22f), 0.7f);
                }
                Space(x, g, 0.5f, 0.6f, 0.15f);
                Grit(x, 1.6f);
                return Polish(x, rate, 120f, -1.5f, 2.5f, -12f);
            }

            // A yelp of pain: the voice jumps high and falls.
            private static void Yelp(float[] x, SynthGen g, float at, float hz, float tract, float gain)
            {
                float[] y = Voice(g, g.Range(0.14f, 0.22f), new Throat
                {
                    F0 = u => hz * (1f + 0.45f * Dsp.Bell(u, 1f, 0.25f) - 0.2f * u),
                    Amp = u => (u < 0.08f ? Dsp.SmoothStep(u / 0.08f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.4f, 1f))),
                    Shape = u => Vowel.Lerp(Vowel.Ae, Vowel.I, Dsp.Bell(u, 1f, 0.3f)),
                    Tract = tract, Widen = 1.1f, Jitter = 0.01f, Shimmer = 0.1f, Rasp = 0.2f, Sub = 0.1f, BreathLevel = 0.2f, Hiss = 0.3f, Tilt = 5000f,
                });
                Dsp.Norm(y);
                Dsp.Mix(x, y, (int)(at * g.Rate), gain);
            }

            // A whine through the nose: high, wavering, weak.
            private static void Whine(float[] x, SynthGen g, float at, float hz, float tract, float gain, float len)
            {
                float vib = g.Range(5f, 8f), ph = g.Range(0f, 6.28f);
                float[] w = Voice(g, len, new Throat
                {
                    F0 = u => hz * (1.05f - 0.25f * u) * (1f + 0.03f * MathF.Sin(Dsp.TwoPi * vib * u * len + ph)),
                    Amp = u => (u < 0.1f ? Dsp.SmoothStep(u / 0.1f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.5f, 1f))) * (0.75f + 0.25f * MathF.Sin(Dsp.TwoPi * 3f * u + ph)),
                    Shape = u => Nn, Tract = tract, Widen = 1.2f, Jitter = 0.015f, Shimmer = 0.15f, BreathLevel = 0.25f, Hiss = 0.25f, Rasp = 0.1f, Tilt = 3000f,
                });
                Dsp.Norm(w);
                Dsp.Mix(x, w, (int)(at * g.Rate), gain);
            }

            // A wolf hurt: a yelp, a double yelp, a yelp and a whimper.
            internal static float[] WolfYelp(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                float hz = g.Range(650f, 820f), tract = g.Range(1.15f, 1.3f);
                var x = new float[g.N(k == 2 ? 0.95f : 0.6f)];
                Yelp(x, g, 0.005f, hz, tract, 1f);
                if (k == 1) Yelp(x, g, g.Range(0.2f, 0.28f), hz * 0.92f, tract, 0.75f);
                if (k == 2) Whine(x, g, g.Range(0.22f, 0.28f), hz * 0.9f, tract, 0.45f, g.Range(0.45f, 0.55f));
                Grit(x, 1.4f);
                return Polish(x, rate, 200f, -2f, 2.5f, -12f);
            }

            // A wolf's death: a yelp, a falling whine, a last rattling breath.
            internal static float[] WolfDie(SynthGen g)
            {
                int rate = g.Rate;
                float hz = g.Range(640f, 780f), tract = g.Range(1.15f, 1.3f);
                var x = new float[g.N(1.6f)];
                Yelp(x, g, 0.005f, hz * 1.05f, tract, 1f);
                Whine(x, g, g.Range(0.2f, 0.26f), hz * 0.85f, tract, 0.5f, g.Range(0.55f, 0.75f));
                float[] last = Breath(g, g.Range(0.35f, 0.5f), false, tract * 1.1f, 0.3f, 0.5f, 0.4f, 0.1f, Vowel.Uh);
                Dsp.Norm(last);
                Dsp.Mix(x, last, g.N(g.Range(0.85f, 1f)), 0.18f);
                Grit(x, 1.3f);
                return Polish(x, rate, 120f, -2f, 2.5f, -14f);
            }

            // A bear's roar: a huge, rough, period-doubled bellow from a long tract, the breath roaring through it.
            internal static float[] BearRoar(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                float len = g.Range(1.8f, 2.4f), hz = g.Range(68f, 92f), pk = g.Range(0.2f, 0.35f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 3, 7, 3);
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * (0.82f + 0.45f * Dsp.Bell(u, 1f, pk) - 0.15f * u) * (1f + 0.06f * (wob(u) - 0.5f)),
                    Amp = Swells(g, len, 0.05f, 0.55f, 0.25f),
                    Shape = Path(new[] { Vowel.Uh, Vowel.A, Vowel.Aw, Vowel.O }, new[] { 0f, 0.25f, 0.6f, 1f }),
                    Tract = g.Range(0.58f, 0.66f), Widen = 1.8f, Jitter = 0.08f, Shimmer = 0.35f, Drift = 0.04f, DriftHz = 5f,
                    Sub = 0.7f, SubRatio = k == 2 ? 3 : 2, Rasp = 1.1f, BreathLevel = 0.6f, Hiss = 0.45f, Wet = 0.25f, Tilt = 3800f,
                });
                Grit(x, 2.6f);
                Dsp.PeakEq(x, 180f, 0.8f, 3f, rate);
                Dsp.PeakEq(x, 2000f, 0.8f, 3f, rate);
                return Polish(x, rate, 35f, -1.5f, 3f, -14f);
            }

            // A bear warning you off: forceful huffs, and the jaw popping (teeth clacked together).
            internal static float[] BearHuff(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                int huffs = k == 0 ? 2 : k == 1 ? 3 : 1;
                var x = new float[g.N(k == 2 ? 1.6f : 0.45f + 0.45f * huffs + 0.35f)];
                float t = 0.005f, tract = g.Range(0.6f, 0.7f);
                for (int i = 0; i < huffs; i++)
                {
                    float len = g.Range(0.25f, 0.35f), hz = g.Range(55f, 75f);
                    float[] h = Voice(g, len, new Throat
                    {
                        F0 = u => hz, Voicing = u => 0.35f * (1f - u),
                        Amp = u => (u < 0.03f ? Dsp.SmoothStep(u / 0.03f) : 1f) * MathF.Exp(-3.5f * u) * (1f - Dsp.SmoothStep(Span(u, 0.7f, 1f))),
                        Shape = u => Vowel.Lerp(Vowel.O, Vowel.Uh, u), Tract = tract, Widen = 2f, BreathLevel = 1f, Hiss = 0.12f,
                        Rasp = 0.6f, Sub = 0.4f, Wet = 0.3f, Tilt = 1800f,
                    });
                    Dsp.LP(h, 3200f, rate);
                    Dsp.Norm(h);
                    Dsp.Mix(x, h, g.N(t), i == 0 ? 1f : g.Range(0.7f, 0.95f));
                    t += len + g.Range(0.08f, 0.16f);
                }
                int pops = k == 2 ? g.Int(4, 7) : g.Int(1, 3);
                float pt = k == 2 ? 0.38f : t;
                for (int i = 0; i < pops; i++)
                {
                    Snap(x, g, pt, g.Range(0.4f, 0.7f));
                    pt += g.Range(0.07f, 0.13f);
                }
                Dsp.Norm(x);
                return Polish(x, rate, 35f, -1f, 2.5f, -14f);
            }

            // A bear's death: a long groan sinking into a gurgle and a rattle, a last heavy breath.
            internal static float[] BearDie(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(2.2f, 2.6f), hz = g.Range(70f, 85f);
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * Dsp.Glide(1.15f, 0.6f, u),
                    Amp = u => (u < 0.03f ? Dsp.SmoothStep(u / 0.03f) : 1f) * (1f - 0.3f * u) * (1f - Dsp.SmoothStep(Span(u, 0.55f, 1f))),
                    Shape = Path(new[] { Vowel.A, Vowel.Aw, Vowel.O, Vowel.U }, new[] { 0f, 0.3f, 0.6f, 1f }),
                    Fry = u => Span(u, 0.4f, 0.9f), FryHz = 32f,
                    Tract = g.Range(0.58f, 0.66f), Widen = 1.9f, Jitter = 0.07f, Shimmer = 0.35f,
                    Sub = 0.6f, Rasp = 0.8f, BreathLevel = 0.45f, Hiss = 0.3f, Wet = 0.6f, Rattle = 0.35f, RattleHz = 13f, Tilt = 2400f,
                    Excite = Bubbles(g, len, 25f, 80f, 300f, 0.7f, u => Span(u, 0.3f, 1f)),
                });
                float[] last = Breath(g, g.Range(0.5f, 0.7f), false, 0.62f, 0.4f, 0.6f, 0.5f, 0.15f, Vowel.Uh);
                x = Join(x, 1f, last, 0.35f, rate, 0.1f);
                Grit(x, 2f);
                Dsp.PeakEq(x, 170f, 0.8f, 3f, rate);
                return Polish(x, rate, 30f, -1.5f, 2.5f, -15f);
            }

            // Deer in the woods: a doe's nasal, trembling bleat, a buck's two short grunts, a fawn's high wavering call.
            internal static float[] DeerCall(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                if (k == 1)
                {
                    var x = new float[g.N(0.95f)];
                    float bhz = g.Range(110f, 140f), tract = g.Range(0.85f, 0.95f), t = 0.005f;
                    for (int i = 0; i < 2; i++)
                    {
                        float len = g.Range(0.14f, 0.22f);
                        float[] b = Voice(g, len, new Throat
                        {
                            F0 = u => bhz * (1.1f - 0.25f * u),
                            Amp = u => (u < 0.1f ? Dsp.SmoothStep(u / 0.1f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.4f, 1f))),
                            Shape = u => Vowel.Lerp(Vowel.Uh, Vowel.U, u), Tract = tract, Widen = 1.4f, Rasp = 0.35f, Sub = 0.2f,
                            Jitter = 0.03f, Shimmer = 0.2f, BreathLevel = 0.3f, Tilt = 2200f,
                        });
                        Dsp.Norm(b);
                        Dsp.Mix(x, b, g.N(t), i == 0 ? 1f : 0.85f);
                        t += len + g.Range(0.18f, 0.3f);
                    }
                    return Level(Polish(x, rate, 60f, -1f, 2.5f, -12f), rate, 0.3f);
                }
                float hz = k == 0 ? g.Range(480f, 600f) : g.Range(780f, 920f);
                float blen = k == 0 ? g.Range(0.38f, 0.5f) : g.Range(0.5f, 0.68f);
                float trem = g.Range(6f, 9f), ph = g.Range(0f, 6.28f);
                float[] y = Voice(g, blen, new Throat
                {
                    F0 = u => hz * (0.94f + 0.14f * Dsp.Bell(u, 1f, 0.25f) - 0.12f * u) * (1f + 0.025f * MathF.Sin(Dsp.TwoPi * trem * u * blen + ph)),
                    Amp = u => (u < 0.06f ? Dsp.SmoothStep(u / 0.06f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.6f, 1f))),
                    Shape = Path(new[] { Nn, Vowel.Ae, Vowel.E, Nn }, new[] { 0f, 0.2f, 0.7f, 1f }),
                    Tract = g.Range(1.3f, 1.45f), Widen = 1.2f, Jitter = 0.02f, Shimmer = 0.12f, BreathLevel = 0.2f, Hiss = 0.3f,
                    Rasp = 0.25f, Sub = k == 0 ? 0.15f : 0.05f, Tilt = 3600f,
                });
                y = Pad(y, rate, 0.08f);
                return Level(Polish(y, rate, 150f, -2f, 2f, -12f), rate, 0.3f);
            }

            // A deer's alarm blow: air forced out through the nose, sharp and then fading.
            private static void Blow(float[] x, SynthGen g, float at, float seconds, float gain)
            {
                float[] b = Voice(g, seconds, new Throat
                {
                    F0 = u => 280f, Voicing = u => 0.08f * (1f - u), BreathLevel = 1f, Hiss = 0.35f,
                    Shape = u => Vowel.Lerp(Nn, Vowel.Uh, u), Tract = 1.3f, Widen = 1.4f, Rasp = 0.25f, Wet = 0.15f,
                    Amp = u => (u < 0.04f ? Dsp.SmoothStep(u / 0.04f) : 1f) * MathF.Exp(-3f * u) * (1f - Dsp.SmoothStep(Span(u, 0.7f, 1f))),
                });
                Dsp.LP(b, 6500f, g.Rate);
                Dsp.Norm(b);
                Dsp.Mix(x, b, (int)(at * g.Rate), gain);
            }

            // A deer that has seen you: a blow, a double blow, a blow and a forehoof stamped down.
            internal static float[] DeerSnort(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                var x = new float[g.N(k == 0 ? 0.55f : 0.95f)];
                Blow(x, g, 0.003f, g.Range(0.22f, 0.32f), 1f);
                if (k == 1) Blow(x, g, g.Range(0.36f, 0.46f), g.Range(0.18f, 0.26f), 0.8f);
                if (k == 2)
                {
                    float at = g.Range(0.38f, 0.5f);
                    Dsp.Thump(x, rate, at, 0.7f, g.Vary(150f, 0.1f), 70f, 0.012f, 0.001f, 0.04f);
                    Dsp.AddBurst(x, g, at, 0.35f, 0.0008f, 0.015f, 1800f, 60f);
                }
                return Polish(x, rate, 80f, -1f, 2f, -12f);
            }

            // A deer's death: a loud, shaking distress bleat, then a last breath.
            internal static float[] DeerDie(SynthGen g)
            {
                int rate = g.Rate;
                float hz = g.Range(520f, 640f), len = g.Range(0.55f, 0.7f), trem = g.Range(9f, 13f), ph = g.Range(0f, 6.28f);
                float[] y = Voice(g, len, new Throat
                {
                    F0 = u => hz * (1f + 0.35f * Dsp.Bell(u, 1f, 0.3f) - 0.3f * u) * (1f + 0.05f * MathF.Sin(Dsp.TwoPi * trem * u * len + ph)),
                    Amp = u => (u < 0.04f ? Dsp.SmoothStep(u / 0.04f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.5f, 1f))),
                    Shape = Path(new[] { Vowel.Ae, Vowel.A, Nn }, new[] { 0f, 0.4f, 1f }),
                    Tract = g.Range(1.25f, 1.4f), Widen = 1.3f, Jitter = 0.03f, Shimmer = 0.2f, BreathLevel = 0.25f, Hiss = 0.3f,
                    Rasp = 0.35f, Sub = 0.3f, Tilt = 3600f,
                });
                float[] last = Breath(g, g.Range(0.35f, 0.5f), false, 1.3f, 0.2f, 0.4f, 0.3f, 0f, Nn);
                y = Join(y, 1f, last, 0.3f, rate, 0.05f);
                Grit(y, 1.5f);
                return Polish(y, rate, 120f, -2f, 2.5f, -13f);
            }
            #endregion

            #region Mutants
            // Two throats out of one mouth: a high, wobbling, torn shriek against a lower rough voice a wrong interval
            // below, half ring-modulated.
            internal static float[] MutantScreech(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(0.9f, 1.2f), hz = g.Range(850f, 1100f), ratio = g.Range(0.68f, 0.74f);
                float fm = g.Range(7f, 12f), pk = g.Range(0.15f, 0.25f);
                Func<float, float> amp = Swells(g, len, 0.02f, 0.35f, 0.2f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 6, 15, 3);
                Func<float, float> line = u => (0.7f + 0.5f * Dsp.SmoothStep(u / pk) - 0.3f * Span(u, 0.65f, 1f)) * (1f + 0.12f * (wob(u) - 0.5f));
                float[] a = Voice(g, len, new Throat
                {
                    F0 = u => hz * line(u) * (1f + 0.025f * MathF.Sin(Dsp.TwoPi * fm * u * len)),
                    Amp = amp, Shape = Path(new[] { Vowel.E, Vowel.I, Vowel.E }, new[] { 0f, 0.5f, 1f }),
                    Tract = 1.35f, Widen = 1.2f, Jitter = 0.05f, Shimmer = 0.3f, Drift = 0.04f, DriftHz = 7f, Sub = 0.4f, SubRatio = 3, Rasp = 0.8f,
                    BreathLevel = 0.5f, Hiss = 0.5f, Tilt = 8000f,
                });
                float[] b = Voice(g, len, new Throat
                {
                    F0 = u => hz * ratio * line(u) * (1f - 0.02f * MathF.Sin(Dsp.TwoPi * fm * 0.73f * u * len)),
                    Amp = amp, Shape = Path(new[] { Vowel.A, Vowel.Ae, Vowel.A }, new[] { 0f, 0.5f, 1f }),
                    Tract = 1.1f, Widen = 1.4f, Jitter = 0.07f, Shimmer = 0.35f, Drift = 0.05f, DriftHz = 5f, Sub = 0.5f, Rasp = 0.9f,
                    BreathLevel = 0.3f, Hiss = 0.3f, Tilt = 5000f,
                });
                Dsp.Norm(a);
                Dsp.Norm(b);
                var x = new float[a.Length];
                float ring = g.Range(55f, 90f);
                for (int i = 0; i < x.Length; i++) x[i] = a[i] + 0.7f * b[i] * (0.6f + 0.4f * MathF.Sin(Dsp.TwoPi * ring * i / rate));
                Grit(x, 3f);
                Dsp.PeakEq(x, 3000f, 0.7f, 2f, rate);
                return Polish(x, rate, 200f, 0f, 3f, -12f);
            }

            // A growl from something too big inside: two rough, gargling throats a tritone apart over a throb felt more
            // than heard.
            internal static float[] MutantGrowl(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(1.3f, 1.8f), hz = g.Range(48f, 58f), ratio = g.Range(1.38f, 1.45f), tract = g.Range(0.6f, 0.7f);
                Func<float, float> amp = Swells(g, len, 0.06f, 0.45f, 0.35f, 2, 5);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 3, 8, 3);
                var x = new float[g.N(len)];
                for (int v = 0; v < 2; v++)
                {
                    float r = v == 0 ? 1f : ratio;
                    float[] y = Voice(g, len, new Throat
                    {
                        F0 = u => hz * r * (0.9f + 0.2f * wob(u)), Amp = amp, Shape = u => Vowel.Lerp(Vowel.U, Vowel.Aw, wob(u)),
                        Tract = tract * (v == 0 ? 1f : 1.08f), Widen = 2f, Jitter = 0.06f, Shimmer = 0.35f,
                        Sub = 0.8f, SubRatio = 3, Rasp = 0.8f, BreathLevel = 0.4f, Hiss = 0.3f, Wet = 0.6f, Rattle = 0.3f, RattleHz = 11f, Tilt = 2800f,
                    });
                    Dsp.Norm(y);
                    Dsp.Mix(x, y, 0, v == 0 ? 1f : 0.6f);
                }
                float th = g.Range(32f, 42f);
                Func<float, float> throb = Dsp.Gusts(g, 1f, 5, 11, 3);
                for (int i = 0; i < x.Length; i++)
                {
                    float u = i / (float)x.Length;
                    x[i] += 0.15f * amp(u) * MathF.Sin(Dsp.TwoPi * th * i / rate) * throb(u);
                }
                Grit(x, 2.2f);
                Dsp.PeakEq(x, 1200f, 0.8f, 3f, rate);
                return Polish(x, rate, 40f, -1.5f, 3f, -14f);
            }

            // A mutant dying: a gurgled shriek collapsing into a rough groan, a rattle, a last wet breath.
            internal static float[] MutantDie(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(1.3f, 1.6f), hz = g.Range(500f, 650f);
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * (u < 0.2f ? 1f + 0.3f * Dsp.Bell(u, 0.2f, 0.4f) : Dsp.Glide(1f, 0.16f, Span(u, 0.2f, 1f))),
                    Amp = u => (u < 0.02f ? Dsp.SmoothStep(u / 0.02f) : 1f) * (1f - Dsp.SmoothStep(Span(u, 0.55f, 1f))),
                    Shape = Path(new[] { Vowel.I, Vowel.E, Vowel.A, Vowel.O, Vowel.U }, new[] { 0f, 0.18f, 0.35f, 0.7f, 1f }),
                    Fry = u => Span(u, 0.5f, 1f), FryHz = 38f,
                    Tract = g.Range(0.95f, 1.1f), Widen = 1.5f, Jitter = 0.05f, Shimmer = 0.3f, Sub = 0.5f, SubRatio = 3, Rasp = 0.7f,
                    BreathLevel = 0.35f, Hiss = 0.35f, Wet = 0.6f, Rattle = 0.3f, Tilt = 5000f,
                    Excite = Bubbles(g, len, 30f, 100f, 400f, 0.6f, u => Span(u, 0.3f, 1f)),
                });
                float[] last = Breath(g, g.Range(0.4f, 0.55f), false, 0.9f, 0.4f, 0.7f, 0.6f, 0.15f, Vowel.Uh);
                x = Join(x, 1f, last, 0.35f, rate, 0.08f);
                Grit(x, 2.4f);
                return Polish(x, rate, 50f, -1.5f, 2.5f, -14f);
            }

            // A hound giving tongue: one to three harsh, wet, diseased barks.
            internal static float[] HoundBark(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                float hz = g.Range(210f, 290f), tract = g.Range(0.9f, 1f), t = 0.005f;
                var x = new float[g.N(1f)];
                for (int i = 0; i <= k; i++)
                {
                    Bark(x, g, t, hz * g.Range(0.95f, 1.05f), tract, 0.85f, 0.6f, 0.35f, i == 0 ? 1f : 0.85f);
                    t += g.Range(0.2f, 0.27f);
                }
                Grit(x, 3f);
                return Polish(x, rate, 80f, -1f, 3f, -12f);
            }

            // A hound going for you: a vicious snarl with the lips back, the teeth snapping.
            internal static float[] HoundSnarl(SynthGen g)
            {
                int k = g.Variation, rate = g.Rate;
                float len = g.Range(0.45f, 0.65f), hz = g.Range(150f, 200f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 6, 14, 3);
                float[] s = Voice(g, len, new Throat
                {
                    F0 = u => hz * (0.85f + 0.4f * Dsp.SmoothStep(u * 3f) - 0.2f * Span(u, 0.6f, 1f)) * (1f + 0.1f * (wob(u) - 0.5f)),
                    Amp = u => (u < 0.05f ? Dsp.SmoothStep(u / 0.05f) : 1f) * (1f - 0.8f * Dsp.SmoothStep(Span(u, 0.55f, 1f))),
                    Shape = Path(new[] { Vowel.Er, Vowel.E, Vowel.Ae }, new[] { 0f, 0.4f, 1f }),
                    Tract = g.Range(0.95f, 1.05f), Widen = 1.4f, Jitter = 0.08f, Shimmer = 0.4f, Sub = 0.6f, Rasp = 0.95f,
                    BreathLevel = 0.4f, Hiss = 0.5f, Wet = 0.3f, Tilt = 4500f,
                });
                var x = new float[g.N(len + 0.3f)];
                Dsp.Norm(s);
                Dsp.Mix(x, s, 0, 1f);
                float bite = len * g.Range(0.8f, 0.92f);
                Snap(x, g, bite, 1f);
                if (k != 1) Snap(x, g, bite + g.Range(0.06f, 0.1f), 0.7f);
                Grit(x, 2.8f);
                return Polish(x, rate, 80f, -1f, 3f, -12f);
            }

            // The Shade: a high voice that is mostly air, a whistle through a crack gliding with it, and a smear of its
            // own echo run backwards into it.
            internal static float[] ShadeShriek(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(1.1f, 1.4f), hz = g.Range(1100f, 1450f), pk = g.Range(0.3f, 0.45f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 3, 7, 3);
                Func<float, float> amp = Swells(g, len, 0.04f, 0.45f, 0.3f);
                float[] v = Voice(g, len, new Throat
                {
                    F0 = u => hz * (0.8f + 0.35f * Dsp.Bell(u, 1f, pk) - 0.2f * u) * (1f + 0.04f * (wob(u) - 0.5f)),
                    Amp = amp, Voicing = u => 0.35f, Shape = Path(new[] { Vowel.E, Vowel.I, Vowel.E, Vowel.Ae }, new[] { 0f, 0.35f, 0.7f, 1f }),
                    Tract = 1.3f, Widen = 1.1f, Jitter = 0.01f, Shimmer = 0.1f, BreathLevel = 0.9f, Hiss = 0.8f, Rasp = 0.2f, Tilt = 6000f,
                });
                Dsp.Norm(v);
                float[] w = Dsp.White(g, v.Length, 1f);
                float wh = g.Range(2400f, 3200f);
                Dsp.SweepBP(w, rate, 18f, t => wh * (0.8f + 0.4f * Dsp.Bell(t / len, 1f, pk)));
                Dsp.Norm(w);
                for (int i = 0; i < v.Length; i++) v[i] += 0.5f * w[i] * amp(i / (float)v.Length);
                var x = new float[g.N(len + 0.9f)];
                Dsp.Mix(x, v, 0, 1f);
                float[] smear = Pad(v, rate, 0.6f);
                Dsp.Reverb(smear, rate, 0.8f, 0.4f, 1.5f, 0f);
                Array.Reverse(smear);
                Dsp.Norm(smear);
                int peakAt = (int)(pk * v.Length);
                Dsp.Mix(x, smear, peakAt - smear.Length + g.N(0.05f), 0.35f);
                Space(x, g, 0.75f, 0.4f, 0.35f);
                Tail(x, rate, 0.3f);
                return Polish(x, rate, 300f, -1f, 2f, -14f);
            }

            // A Thumper: a bellow below the voice of anything living, the ground humming with it.
            internal static float[] ThumperBellow(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(2.1f, 2.7f), hz = g.Range(34f, 44f), pk = g.Range(0.3f, 0.45f);
                Func<float, float> wob = Dsp.Gusts(g, 1f, 2, 5, 3);
                Func<float, float> amp = Swells(g, len, 0.08f, 0.7f, 0.25f);
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * (0.85f + 0.3f * Dsp.Bell(u, 1f, pk) - 0.1f * u) * (1f + 0.04f * (wob(u) - 0.5f)),
                    Amp = amp, Shape = Path(new[] { Vowel.O, Vowel.A, Vowel.Aw, Vowel.U }, new[] { 0f, 0.3f, 0.65f, 1f }),
                    Tract = g.Range(0.42f, 0.48f), Widen = 2.2f, Jitter = 0.05f, Shimmer = 0.3f, Drift = 0.02f,
                    Sub = 0.5f, Rasp = 0.6f, BreathLevel = 0.4f, Hiss = 0.2f, Wet = 0.3f, Tilt = 1500f,
                });
                Ground(x, g, amp, 0.35f);
                Grit(x, 1.8f);
                Dsp.PeakEq(x, 120f, 0.7f, 3f, rate);
                return Polish(x, rate, 22f, -1f, 3f, -14f);
            }

            // The rumble of the earth under something huge, following its loudness.
            private static void Ground(float[] x, SynthGen g, Func<float, float> amp, float level)
            {
                float[] r = Dsp.Brown(g, x.Length);
                Dsp.LP(r, 90f, g.Rate);
                Dsp.Norm(r);
                Dsp.Norm(x);
                for (int i = 0; i < x.Length; i++) x[i] += level * r[i] * amp(i / (float)x.Length);
            }

            // A Thumper's slam: the whole weight of it coming down. A huge dull blow and the thwack of its body, the
            // ground cracking outward, dirt and stones raining back, the earth rumbling after. No blast: nothing deafens.
            internal static float[] ThumperSlam(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(1.6f)];
                Dsp.Thump(x, rate, 0f, 1f, g.Vary(62f, 0.08f), 28f, 0.05f, 0.002f, 0.2f);
                Dsp.Thump(x, rate, 0.004f, 0.7f, g.Vary(140f, 0.1f), 60f, 0.02f, 0.001f, 0.05f);
                Dsp.AddBurst(x, g, 0f, 0.8f, 0.001f, 0.06f, 900f, 40f);
                Dsp.AddBurst(x, g, 0.002f, 0.8f, 0.0005f, 0.025f, 0f, 400f);
                var crack = new float[x.Length];
                Dsp.Grains(crack, g, 0.01f, g.Range(0.3f, 0.45f), 120, 0.6f, 0.0012f, u => 1f - u);
                Dsp.HP(crack, 500f, rate);
                Dsp.LP(crack, 5000f, rate);
                Dsp.Mix(x, crack, 0, 0.5f);
                var dirt = new float[x.Length];
                Dsp.Grains(dirt, g, 0.15f, 1.3f, 160, 0.4f, 0.0008f, u => MathF.Exp(-3f * u));
                Dsp.HP(dirt, 1200f, rate);
                Dsp.Mix(x, dirt, 0, 0.25f);
                float[] rumble = Dsp.Brown(g, x.Length);
                Dsp.LP(rumble, 140f, rate);
                for (int i = 0; i < rumble.Length; i++) rumble[i] *= Dsp.AD(i / (float)rate, 0.03f, 0.35f);
                Dsp.Norm(rumble);
                Dsp.Mix(x, rumble, 0, 0.5f);
                Dsp.HP(x, 24f, rate);
                Dsp.PeakEq(x, 250f, 0.8f, 3f, rate);
                Dsp.Norm(x);
                Dsp.Compress(x, rate, -10f, 3f, 1f, 120f);
                Tail(x, rate, 0.2f);
                return x;
            }

            // A Thumper dying: the bellow sinking, gargling and rattling away, and the long last breath of it.
            internal static float[] ThumperDie(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(2.3f, 2.7f), hz = g.Range(36f, 42f);
                Func<float, float> amp = u => (u < 0.03f ? Dsp.SmoothStep(u / 0.03f) : 1f) * (1f - 0.3f * u) * (1f - Dsp.SmoothStep(Span(u, 0.5f, 1f)));
                float[] x = Voice(g, len, new Throat
                {
                    F0 = u => hz * Dsp.Glide(1.3f, 0.65f, u), Amp = amp,
                    Shape = Path(new[] { Vowel.A, Vowel.Aw, Vowel.O, Vowel.U }, new[] { 0f, 0.3f, 0.65f, 1f }),
                    Tract = g.Range(0.42f, 0.48f), Widen = 2.3f, Jitter = 0.07f, Shimmer = 0.35f,
                    Sub = 0.6f, Rasp = 0.7f, BreathLevel = 0.45f, Hiss = 0.2f, Wet = 0.6f, Rattle = 0.4f, RattleHz = 9f, Tilt = 1500f,
                    Excite = Bubbles(g, len, 20f, 60f, 220f, 0.7f, u => Span(u, 0.35f, 1f)),
                });
                Ground(x, g, amp, 0.3f);
                float[] last = Breath(g, g.Range(0.7f, 0.9f), false, 0.45f, 0.5f, 0.6f, 0.5f, 0.2f, Vowel.O);
                x = Join(x, 1f, last, 0.4f, rate, 0.15f);
                Grit(x, 1.8f);
                Dsp.PeakEq(x, 120f, 0.7f, 3f, rate);
                return Polish(x, rate, 22f, -1f, 2.5f, -15f);
            }

            // The Puppeteer reaching for your mind: a choir of one, moaning in beating unisons, a fifth and an octave,
            // ring-modulated just enough to be wrong, with whispers riding on it.
            internal static float[] PuppeteerMoan(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(2.5f, 3.1f), hz = g.Range(95f, 115f);
                var x = new float[g.N(len + 0.9f)];
                float[] ratios = { 1f, 1.013f, 1.495f, 0.5f };
                float[] gains = { 1f, 0.8f, 0.5f, 0.4f };
                Func<float, float> amp = Swells(g, len, 0.12f, 0.8f, 0.4f, 1, 2);
                for (int v = 0; v < ratios.Length; v++)
                {
                    float r = ratios[v] * g.Range(0.996f, 1.004f), vib = g.Range(4f, 6.5f), vd = g.Range(0.006f, 0.014f), ph = g.Range(0f, 6.28f);
                    float delay = v == 0 ? 0f : g.Range(0f, 0.15f);
                    float[] y = Voice(g, len, new Throat
                    {
                        F0 = u => hz * r * Dsp.Glide(0.93f, 1.06f, u) * (1f + vd * MathF.Sin(Dsp.TwoPi * vib * u * len + ph)),
                        Amp = amp, Shape = Path(new[] { Oo, Vowel.O, Vowel.A, Vowel.O, Oo }, new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }),
                        Tract = g.Range(0.95f, 1.1f), Widen = 1.2f, Jitter = 0.004f, Shimmer = 0.03f, BreathLevel = 0.12f, Hiss = 0.15f, Tilt = 2200f,
                    });
                    Dsp.Norm(y);
                    Dsp.Mix(x, y, (int)(delay * rate), gains[v]);
                }
                float ring = g.Range(28f, 40f);
                for (int i = 0; i < x.Length; i++) x[i] *= 0.75f + 0.25f * MathF.Sin(Dsp.TwoPi * ring * i / rate);
                float[] wh = WhisperPhrase(g, len * 0.8f, 1.05f, 1.1f);
                Dsp.Norm(wh);
                Dsp.Norm(x);
                Dsp.Mix(x, wh, g.N(0.3f), 0.25f);
                Space(x, g, 0.8f, 0.45f, 0.4f);
                Tail(x, rate, 0.4f);
                return Polish(x, rate, 50f, -1f, 2f, -16f);
            }
            #endregion

            #region Whispers, the radio, the stinger
            // A whispered phrase in no language: words of one to three syllables, vowels of breath through a moving tract,
            // with s, sh, f, t and k made at the teeth and lips between them.
            internal static float[] WhisperPhrase(SynthGen g, float seconds, float tract, float pace)
            {
                var at = new List<float>();
                var dur = new List<float>();
                var lvl = new List<float>();
                var vow = new List<Vowel>();
                var con = new List<int>();
                float t = g.Range(0.005f, 0.03f);
                while (t < seconds - 0.3f)
                {
                    int syl = g.Int(1, 4);
                    for (int s = 0; s < syl && t < seconds - 0.25f; s++)
                    {
                        float d = g.Range(0.1f, 0.22f) / pace;
                        at.Add(t);
                        dur.Add(d);
                        lvl.Add(g.Range(0.45f, 1f));
                        vow.Add(Vowel.Speech[g.Int(0, Vowel.Speech.Length)]);
                        con.Add(g.Int(0, 7));
                        t += d * g.Range(0.85f, 1.05f);
                    }
                    t += g.Range(0.06f, 0.24f) / pace;
                }
                if (at.Count == 0)
                {
                    at.Add(0.01f);
                    dur.Add(seconds * 0.5f);
                    lvl.Add(1f);
                    vow.Add(Vowel.Uh);
                    con.Add(0);
                }
                float[] A = at.ToArray(), D = dur.ToArray(), L = lvl.ToArray();
                Vowel[] V = vow.ToArray();
                int m = A.Length;
                Func<float, float> amp = u =>
                {
                    float tt = u * seconds, s = 0f;
                    for (int i = 0; i < m; i++)
                    {
                        float w = (tt - A[i]) / D[i];
                        if (w <= 0f || w >= 1f) continue;
                        float e = w < 0.18f ? Dsp.SmoothStep(w / 0.18f) : 1f - 0.75f * Dsp.SmoothStep((w - 0.18f) / 0.82f);
                        if (w > 0.85f) e *= 1f - Dsp.SmoothStep((w - 0.85f) / 0.15f);
                        s = Math.Max(s, L[i] * e);
                    }
                    return s;
                };
                Func<float, Vowel> shape = u =>
                {
                    float tt = u * seconds;
                    int i = 0;
                    while (i + 1 < m && A[i + 1] <= tt) i++;
                    if (i == 0) return V[0];
                    return Vowel.Lerp(V[i - 1], V[i], Dsp.SmoothStep((tt - A[i]) / (0.35f * D[i])));
                };
                float[] x = Voice(g, seconds, new Throat
                {
                    F0 = u => 100f, Voicing = u => 0f, BreathLevel = 1f, Hiss = 0.35f, Tract = tract, Widen = 1.5f, Amp = amp, Shape = shape,
                });
                Dsp.Norm(x);
                for (int i = 0; i < m; i++)
                {
                    float t0 = A[i], gain = L[i];
                    switch (con[i])
                    {
                        case 1: { float d = g.Range(0.06f, 0.1f); Fricative(x, g, t0 - d * 0.8f, d, g.Range(5200f, 7000f), 2.2f, 0.5f * gain); break; }
                        case 2: { float d = g.Range(0.07f, 0.12f); Fricative(x, g, t0 - d * 0.8f, d, g.Range(2600f, 3300f), 1.6f, 0.5f * gain); break; }
                        case 3: { float d = g.Range(0.05f, 0.09f); Fricative(x, g, t0 - d * 0.8f, d, g.Range(3500f, 5000f), 0.7f, 0.25f * gain); break; }
                        case 4: Dsp.AddBurstBP(x, g, Math.Max(0f, t0 - 0.004f), 0.45f * gain, 0.0003f, 0.006f, g.Range(3800f, 5000f), 1.2f); break;
                        case 5: Dsp.AddBurstBP(x, g, Math.Max(0f, t0 - 0.006f), 0.4f * gain, 0.0004f, 0.01f, g.Range(1800f, 2600f), 1.5f); break;
                    }
                }
                return x;
            }

            // Whispers close to the ear: a near voice, a smaller one behind it, one run backwards out of its own echo and
            // one slowed far down, in a little space of their own.
            internal static float[] UncannyWhisper(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(2.6f, 3.2f);
                var x = new float[g.N(len + 0.6f)];
                float[] a = WhisperPhrase(g, len * g.Range(0.75f, 0.95f), g.Range(0.95f, 1.08f), g.Range(0.9f, 1.15f));
                Dsp.Norm(a);
                Dsp.Mix(x, a, 0, 1f);
                float[] b = WhisperPhrase(g, len * 0.7f, g.Range(1.15f, 1.3f), g.Range(1.1f, 1.4f));
                Dsp.Norm(b);
                Dsp.Mix(x, b, g.N(g.Range(0.3f, 0.7f)), 0.45f);
                float[] c = Pad(WhisperPhrase(g, g.Range(1f, 1.4f), g.Range(0.9f, 1.1f), 1f), rate, 0.5f);
                Dsp.Reverb(c, rate, 0.85f, 0.4f, 1.2f);
                Array.Reverse(c);
                Dsp.Norm(c);
                Dsp.Mix(x, c, g.N(g.Range(0.6f, 1.1f)), 0.5f);
                float[] d = Dsp.Varispeed(WhisperPhrase(g, 1.4f, 1f, 1f), g.Range(0.55f, 0.7f));
                Dsp.Norm(d);
                Dsp.Mix(x, d, g.N(g.Range(0.1f, 0.5f)), 0.35f);
                Dsp.HP(x, 120f, rate);
                Space(x, g, 0.7f, 0.5f, 0.35f);
                Tail(x, rate, 0.35f);
                return Polish(x, rate, 120f, -1f, 2f, -18f);
            }

            // One lub-dub of a heartbeat at phase p (one beat per unit).
            private static float Heart(float p)
            {
                p -= MathF.Floor(p);
                return Dsp.Bell(p, 0.12f, 0.35f) + 0.7f * Dsp.Bell(p - 0.22f, 0.11f, 0.35f);
            }

            // Something in your head: whispers from every side (one backwards, one deep), tones ringing and beating high
            // in the skull, a pressure throbbing like a pulse.
            internal static float[] PsiWhisper(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(2.2f, 2.7f);
                var x = new float[g.N(len + 0.55f)];
                for (int v = 0; v < 3; v++)
                {
                    float[] p = WhisperPhrase(g, len * g.Range(0.6f, 0.9f), g.Range(0.85f, 1.25f), g.Range(1f, 1.5f));
                    Dsp.Norm(p);
                    float tr = g.Range(3f, 9f), ph = g.Range(0f, 6.28f);
                    for (int i = 0; i < p.Length; i++) p[i] *= 0.6f + 0.4f * MathF.Sin(Dsp.TwoPi * tr * i / rate + ph);
                    Dsp.Mix(x, p, v == 0 ? 0 : g.N(g.Range(0.05f, 0.5f)), v == 0 ? 1f : 0.6f);
                }
                float[] back = Pad(WhisperPhrase(g, g.Range(0.9f, 1.3f), 1f, 1.2f), rate, 0.5f);
                Dsp.Reverb(back, rate, 0.85f, 0.4f, 1.2f);
                Array.Reverse(back);
                Dsp.Norm(back);
                Dsp.Mix(x, back, g.N(g.Range(0.5f, 1f)), 0.6f);
                float[] deep = Dsp.Varispeed(WhisperPhrase(g, 1.2f, 1f, 1f), g.Range(0.5f, 0.62f));
                Dsp.Norm(deep);
                Dsp.Mix(x, deep, g.N(g.Range(0.05f, 0.3f)), 0.5f);
                Dsp.Norm(x);
                int n = x.Length;
                float t1 = g.Range(3100f, 3600f), t2 = g.Range(4200f, 4800f), t3 = g.Range(5400f, 6000f);
                float beat = g.Range(3f, 7f), bpm = g.Range(1.1f, 1.5f), thud = g.Range(44f, 52f);
                float m1 = 0f, m2 = 0f, m3 = 0f, pulseAmp = 0f, sw = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    if ((i & 15) == 0)
                    {
                        sw = Dsp.Bell(i / (float)n, 1f, 0.5f);
                        m1 = 0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * beat * 0.37f * t);
                        m2 = 0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * beat * 0.74f * t);
                        m3 = 0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * beat * 1.11f * t);
                        pulseAmp = Heart(t * bpm);
                    }
                    float ring = MathF.Sin(Dsp.TwoPi * t1 * t) * m1 + MathF.Sin(Dsp.TwoPi * t2 * t + 1f) * m2 + MathF.Sin(Dsp.TwoPi * t3 * t + 2f) * m3;
                    x[i] += (0.025f * ring + 0.35f * pulseAmp * MathF.Sin(Dsp.TwoPi * thud * t)) * sw;
                }
                Space(x, g, 0.8f, 0.45f, 0.4f);
                Tail(x, rate, 0.35f);
                return Polish(x, rate, 35f, -1f, 2f, -16f);
            }

            // A radio switching itself on: shortwave hiss fading in and out, crackle, carriers squealing as the dial
            // drifts, and broken voices in no language on a mistuned carrier.
            internal static float[] RadioStatic(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(3.2f, 3.8f);
                int n = g.N(len);
                float[] x = Dsp.White(g, n, 1f);
                Dsp.HP(x, 300f, rate);
                Dsp.LP(x, 4200f, rate);
                float[] fade = Dsp.Wander(g, n, rate, g.Range(0.8f, 1.6f));
                for (int i = 0; i < n; i++) x[i] *= 0.3f * (0.3f + 0.7f * fade[i]);
                Dsp.Grains(x, g, 0f, len, (int)(len * 45f), 0.5f, 0.0005f, u => 1f);
                int pops = g.Int(3, 7);
                for (int i = 0; i < pops; i++) Dsp.AddBurst(x, g, g.Range(0.05f, len - 0.3f), g.Range(0.5f, 1f), 0.0002f, 0.002f, 5000f, 300f);
                int squeals = g.Int(2, 4);
                for (int s = 0; s < squeals; s++)
                {
                    float t0 = g.Range(0.05f, len - 0.9f), dur = g.Range(0.35f, 0.9f);
                    float f0 = g.Range(300f, 3200f), f1 = g.Range(300f, 3200f), wHz = g.Range(4f, 8f), wD = g.Range(0.01f, 0.04f), lv = g.Range(0.15f, 0.35f);
                    int i0 = (int)(t0 * rate), i1 = Math.Min(n, i0 + (int)(dur * rate));
                    float ph = 0f;
                    for (int i = i0; i < i1; i++)
                    {
                        float u = (i - i0) / (float)(i1 - i0);
                        float f = Dsp.Glide(f0, f1, Dsp.SmoothStep(u)) * (1f + wD * MathF.Sin(Dsp.TwoPi * wHz * (i - i0) / rate));
                        ph += f / rate;
                        ph -= MathF.Floor(ph);
                        x[i] += lv * Dsp.Bell(u, 1f, 0.5f) * MathF.Sin(Dsp.TwoPi * ph);
                    }
                }
                int frags = g.Int(2, 4);
                for (int f = 0; f < frags; f++)
                {
                    float t0 = g.Range(0.1f, len - 0.9f), dur = g.Range(0.4f, 0.8f), hz = g.Range(105f, 210f);
                    float syl = g.Range(5f, 8f) * dur, sph = g.Range(0f, 6.28f), into = g.Range(0f, 6.28f);
                    var vs = new Vowel[16];
                    for (int i = 0; i < vs.Length; i++) vs[i] = Vowel.Speech[g.Int(0, Vowel.Speech.Length)];
                    float[] v = Voice(g, dur, new Throat
                    {
                        F0 = u => hz * (1f + 0.12f * MathF.Sin(Dsp.TwoPi * 1.3f * u + into)),
                        Amp = u => Gate(u, 0f, 1f, 0.05f, 0.1f) * (0.25f + 0.75f * MathF.Pow(Math.Max(0f, MathF.Sin(Dsp.TwoPi * syl * u + sph)), 0.6f)),
                        Shape = u => vs[(int)(u * syl * 1.5f) % vs.Length],
                        Tract = g.Range(0.95f, 1.15f), BreathLevel = 0.08f, Jitter = 0.01f, Tilt = 3000f,
                    });
                    Dsp.HP(v, 350f, rate);
                    Dsp.LP(v, 2800f, rate);
                    float fc = g.Range(40f, 140f);
                    for (int i = 0; i < v.Length; i++) v[i] *= 0.45f + 0.55f * MathF.Sin(Dsp.TwoPi * fc * i / rate);
                    Dsp.Norm(v);
                    Dsp.Mix(x, v, (int)(t0 * rate), g.Range(0.35f, 0.6f));
                }
                Dsp.HP(x, 220f, rate);
                Dsp.PeakEq(x, 1800f, 1f, 4f, rate);
                Dsp.LP(x, 4600f, rate);
                Dsp.Norm(x);
                Dsp.Compress(x, rate, -18f, 4f, 2f, 60f);
                Level(x, rate, 0.3f);
                Tail(x, rate, 0.25f);
                return x;
            }

            // The stinger: a low stir, then a cluster of semitones and tritones bending upward with a quickening tremolo,
            // a bowed plate screeching through it, a sub swelling, a breath sucked in, and the hit (a deep blow, a crack,
            // struck metal, a dissonant stab) reversed into itself and ringing out wide.
            internal static StereoBuf Stinger(SynthGen g)
            {
                int rate = g.Rate;
                float swell = g.Range(2.4f, 2.8f), len = swell + 1.85f;
                int n = g.N(len), hit = g.N(swell);
                var s = new StereoBuf(n);
                var stir = new float[g.N(0.6f)];
                Dsp.Thump(stir, rate, 0f, 1f, 70f, 45f, 0.05f, 0.004f, 0.12f);
                Dsp.MixPan(s, stir, 0, 0.45f, 0f);
                float root = g.Range(98f, 125f);
                float[] semis = { 0f, 1f, 6f, 7f, 11f, 13f };
                var cluster = new StereoBuf(hit + g.N(0.02f));
                for (int k = 0; k < semis.Length; k++)
                {
                    float hz = root * MathF.Pow(2f, semis[k] / 12f) * g.Range(0.997f, 1.003f);
                    float bend = g.Range(1.5f, 3f), pan = g.Range(-0.8f, 0.8f), ph = g.Next(), tph = g.Range(0f, 6.28f);
                    var tone = new float[cluster.Length];
                    float step = 0f, gain = 0f;
                    for (int i = 0; i < tone.Length; i++)
                    {
                        if ((i & 15) == 0)
                        {
                            float t = i / (float)rate, u = Math.Min(1f, t / swell);
                            step = hz * MathF.Pow(2f, bend * u * u / 12f) / rate;
                            float trem = 1f - 0.4f * (0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * (2f * t + 10f * t * t * t / (3f * swell * swell)) + tph));
                            gain = trem * (0.12f + 0.88f * MathF.Pow(u, 2.2f));
                        }
                        ph += step;
                        if (ph >= 1f) ph -= 1f;
                        tone[i] = Dsp.Saw(ph, step) * gain;
                    }
                    Dsp.MixPan(cluster, tone, 0, 0.28f, pan);
                }
                // One opening filter for the whole cluster (each side), rather than one per tone.
                Dsp.SweepLP(cluster.L, rate, 0.9f, t => 250f + 3800f * MathF.Pow(Math.Min(1f, t / swell), 2f));
                Dsp.SweepLP(cluster.R, rate, 0.9f, t => 250f + 3800f * MathF.Pow(Math.Min(1f, t / swell), 2f));
                Dsp.Mix(s.L, cluster.L, 0);
                Dsp.Mix(s.R, cluster.R, 0);
                float plate = g.Range(560f, 700f);
                float[] partials = { 1f, 2.32f, 4.25f };
                for (int k = 0; k < partials.Length; k++)
                {
                    float[] b = Dsp.White(g, hit, 1f);
                    Dsp.BP(b, plate * partials[k], 60f, rate);
                    Dsp.Norm(b);
                    for (int i = 0; i < b.Length; i++)
                    {
                        float u = i / (float)b.Length;
                        b[i] *= u * u * u;
                    }
                    Dsp.MixPan(s, b, 0, 0.2f / (1f + k * 0.5f), g.Range(-0.6f, 0.6f));
                }
                var sub = new float[hit];
                for (int i = 0; i < sub.Length; i++)
                {
                    float u = i / (float)sub.Length;
                    sub[i] = MathF.Sin(Dsp.TwoPi * (38f + 5f * u) * i / rate) * u * u;
                }
                Dsp.MixPan(s, sub, 0, 0.4f, 0f);
                float[] gasp = Breath(g, 0.32f, true, 1f, 0.3f, 0f, 0f, 0f, Vowel.Ae);
                Dsp.Norm(gasp);
                Dsp.MixPan(s, gasp, hit - gasp.Length, 0.25f, 0f);

                var h = new float[n - hit];
                Dsp.Thump(h, rate, 0f, 1f, 72f, 32f, 0.06f, 0.001f, 0.45f);
                Dsp.AddBurst(h, g, 0f, 0.8f, 0.0003f, 0.015f, 0f, 900f);
                Dsp.Ring(h, rate, 0f, 0.35f, g.Range(150f, 180f), new[] { 1f, 2.76f, 5.4f, 8.93f, 13.3f }, 1f, 0.7f, 0.75f);
                for (int k = 0; k < semis.Length; k += 2)
                {
                    float step = root * 2f * MathF.Pow(2f, semis[k] / 12f) / rate, ph = 0f, env = 0.16f, fall = MathF.Exp(-1f / (0.35f * rate));
                    for (int i = 0; i < h.Length; i++)
                    {
                        ph += step;
                        if (ph >= 1f) ph -= 1f;
                        h[i] += env * Dsp.Saw(ph, step);
                        env *= fall;
                    }
                }
                Dsp.LP(h, 5000f, rate);
                var rv = new float[g.N(1f)];
                Array.Copy(h, rv, Math.Min(h.Length, g.N(0.5f)));
                Dsp.Reverb(rv, rate, 0.85f, 0.35f, 2f, 0f);
                Array.Reverse(rv);
                Dsp.Norm(rv);
                Dsp.MixPan(s, rv, hit - rv.Length, 0.3f, 0f);
                StereoBuf hs = Dsp.Decorrelate(h, g, 0.4f);
                Dsp.Mix(s.L, hs.L, hit, 1f);
                Dsp.Mix(s.R, hs.R, hit, 1f);
                Dsp.HP(s.L, 25f, rate);
                Dsp.HP(s.R, 25f, rate);
                // The room only from just before the hit (the swell carries its own width; this halves the cost).
                int from = Math.Max(0, hit - g.N(0.6f));
                var room = new StereoBuf(n - from);
                Array.Copy(s.L, from, room.L, 0, room.Length);
                Array.Copy(s.R, from, room.R, 0, room.Length);
                Dsp.StereoReverb(room, rate, 0.7f, 0.45f, 0.45f);
                Array.Copy(room.L, 0, s.L, from, room.Length);
                Array.Copy(room.R, 0, s.R, from, room.Length);
                Tail(s.L, rate, 0.4f);
                Tail(s.R, rate, 0.4f);
                return s;
            }
            #endregion
        }
    }

    public static partial class SfxIds
    {
        public const string ZombieIdle = "zombie_idle", ZombieAlert = "zombie_alert", ZombieChase = "zombie_chase",
            ZombieCrawl = "zombie_crawl", ZombieHurt = "zombie_hurt", BodyFall = "body_fall", BodyFallHeavy = "body_fall_heavy",
            WolfGrowl = "wolf_growl", WolfBark = "wolf_bark", WolfYelp = "wolf_yelp", WolfDie = "wolf_die",
            BearHuff = "bear_huff", BearDie = "bear_die", DeerSnort = "deer_snort", DeerDie = "deer_die",
            MutantDie = "mutant_die", HoundBark = "hound_bark", HoundSnarl = "hound_snarl", ShadeShriek = "shade_shriek",
            ThumperBellow = "thumper_bellow", ThumperSlam = "thumper_slam", ThumperDie = "thumper_die", PuppeteerMoan = "puppeteer_moan";
    }

    /// <summary>
    /// How the dead's vocals are laid out: variation v of zombie_groan, zombie_idle, zombie_alert, zombie_chase,
    /// zombie_crawl, zombie_attack, zombie_hurt and zombie_die is throat v % <see cref="Count"/> saying its
    /// (v / Count)-th line. A zombie that always asks for its own throat (as ZombieVoices does) keeps one voice,
    /// from its first groan to its death rattle.
    /// </summary>
    public static class ZombieThroats
    {
        /// <summary>Distinct throats: a big man, a waterlogged one, a dry husk, a woman, a deep growler.</summary>
        public const int Count = 5;
        /// <summary>Lines per throat (the other vocals have one each).</summary>
        public const int Groans = 3, Idles = 2, Crawls = 2;
    }
}
